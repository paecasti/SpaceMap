using System.Diagnostics;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Scanning;

public sealed class FileSystemScanner(OmissionClassifier omissionClassifier)
{
    private const int DefaultBatchSize = 4096;

    public async Task<ScanExecutionResult> ScanAsync(
        string scanId,
        ScanScope scope,
        CancellationToken cancellationToken,
        Action<long>? onEntriesProcessed,
        Action<OmittedItem>? onOmission,
        Func<IReadOnlyList<PathNode>, CancellationToken, Task> onNodesBatchPersisted)
    {
        var stopwatch = Stopwatch.StartNew();
        var normalizedRoots = ResolveRoots(scope);
        if (normalizedRoots.Count == 0)
        {
            throw new InvalidOperationException("At least one readable root path is required.");
        }

        var pendingNodes = new List<PathNode>(DefaultBatchSize);
        var omissions = new List<OmittedItem>();
        var scanCounter = new ScanCounter();

        async Task FlushPendingNodesAsync()
        {
            if (pendingNodes.Count == 0)
            {
                return;
            }

            var batch = pendingNodes.ToArray();
            pendingNodes.Clear();
            await onNodesBatchPersisted(batch, cancellationToken);
        }

        async ValueTask EmitNodeAsync(PathNode node)
        {
            pendingNodes.Add(node);
            if (pendingNodes.Count >= DefaultBatchSize)
            {
                await FlushPendingNodesAsync();
            }
        }

        foreach (var root in normalizedRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(root))
            {
                var omitted = omissionClassifier.PathUnavailable(scanId, root, "The configured root path does not exist.");
                omissions.Add(omitted);
                onOmission?.Invoke(omitted);
                continue;
            }

            await ProcessDirectoryAsync(
                scanId,
                root,
                null,
                0,
                scanCounter,
                cancellationToken,
                omissions,
                onEntriesProcessed,
                onOmission,
                EmitNodeAsync);
        }

        await FlushPendingNodesAsync();
        stopwatch.Stop();

        return new ScanExecutionResult(
            omissions,
            scanCounter.EntriesProcessed,
            stopwatch.ElapsedMilliseconds,
            omissions.Any(x => x.AffectsPartialResult));
    }

    private async ValueTask<DirectoryAggregate> ProcessDirectoryAsync(
        string scanId,
        string directoryPath,
        string? parentNodeId,
        int depth,
        ScanCounter scanCounter,
        CancellationToken cancellationToken,
        List<OmittedItem> omissions,
        Action<long>? onEntriesProcessed,
        Action<OmittedItem>? onOmission,
        Func<PathNode, ValueTask> emitNodeAsync)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPath = NormalizePath(directoryPath);
        var nodeId = Guid.NewGuid().ToString("N");
        long realSize = 0;
        long logicalSize = 0;
        int directChildren = 0;
        int descendants = 0;
        bool partial = false;

        IEnumerable<FileSystemInfo> entries;
        try
        {
            entries = new DirectoryInfo(normalizedPath).EnumerateFileSystemInfos();
        }
        catch (UnauthorizedAccessException ex)
        {
            var omitted = omissionClassifier.PermissionDenied(scanId, normalizedPath, ex.Message);
            omissions.Add(omitted);
            onOmission?.Invoke(omitted);
            partial = true;
            entries = Array.Empty<FileSystemInfo>();
        }
        catch (IOException ex)
        {
            var omitted = omissionClassifier.IoError(scanId, normalizedPath, ex.Message);
            omissions.Add(omitted);
            onOmission?.Invoke(omitted);
            partial = true;
            entries = Array.Empty<FileSystemInfo>();
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                var omitted = omissionClassifier.SymlinkSkipped(scanId, entry.FullName);
                omissions.Add(omitted);
                onOmission?.Invoke(omitted);
                partial = true;
                continue;
            }

            directChildren++;
            try
            {
                if (entry is DirectoryInfo directoryInfo)
                {
                    var childDirectory = await ProcessDirectoryAsync(
                        scanId,
                        directoryInfo.FullName,
                        nodeId,
                        depth + 1,
                        scanCounter,
                        cancellationToken,
                        omissions,
                        onEntriesProcessed,
                        onOmission,
                        emitNodeAsync);
                    realSize += childDirectory.RealSizeBytes;
                    logicalSize += childDirectory.LogicalSizeBytes;
                    descendants += 1 + childDirectory.DescendantCount;
                    partial |= childDirectory.Partial;
                }
                else if (entry is FileInfo fileInfo)
                {
                    long size = fileInfo.Exists ? fileInfo.Length : 0;
                    scanCounter.EntriesProcessed++;
                    onEntriesProcessed?.Invoke(scanCounter.EntriesProcessed);
                    await emitNodeAsync(
                        new PathNode(
                        Guid.NewGuid().ToString("N"),
                        scanId,
                        nodeId,
                        NormalizePath(fileInfo.FullName),
                        fileInfo.Name,
                        NodeKind.File,
                        depth + 1,
                        size,
                        size,
                        0,
                        0,
                        false));
                    realSize += size;
                    logicalSize += size;
                    descendants++;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                var omitted = omissionClassifier.PermissionDenied(scanId, entry.FullName, ex.Message);
                omissions.Add(omitted);
                onOmission?.Invoke(omitted);
                partial = true;
            }
            catch (IOException ex)
            {
                var omitted = omissionClassifier.IoError(scanId, entry.FullName, ex.Message);
                omissions.Add(omitted);
                onOmission?.Invoke(omitted);
                partial = true;
            }
        }

        scanCounter.EntriesProcessed++;
        onEntriesProcessed?.Invoke(scanCounter.EntriesProcessed);

        var directoryNode = new PathNode(
            nodeId,
            scanId,
            parentNodeId,
            normalizedPath,
            GetLabel(normalizedPath),
            NodeKind.Directory,
            depth,
            realSize,
            logicalSize,
            directChildren,
            descendants,
            partial);
        await emitNodeAsync(directoryNode);

        return new DirectoryAggregate(
            directoryNode.NodeId,
            directoryNode.RealSizeBytes,
            directoryNode.LogicalSizeBytes,
            directoryNode.DescendantCount,
            directoryNode.Partial);
    }

    private static IReadOnlyList<string> ResolveRoots(ScanScope scope)
    {
        if (scope.Mode != ScopeMode.AllLocalDrives)
        {
            return scope.RootPaths.Select(NormalizePath).ToArray();
        }

        return DriveInfo.GetDrives()
            .Where(drive => drive.DriveType is DriveType.Fixed or DriveType.Removable)
            .Select(drive => NormalizePath(drive.RootDirectory.FullName))
            .ToArray();
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar) && fullPath.Length > Path.GetPathRoot(fullPath)!.Length
            ? fullPath.TrimEnd(Path.DirectorySeparatorChar)
            : fullPath;
    }

    private static string GetLabel(string path)
    {
        var root = Path.GetPathRoot(path);
        if (string.Equals(root, path, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return Path.GetFileName(path);
    }

    private sealed record DirectoryAggregate(
        string NodeId,
        long RealSizeBytes,
        long LogicalSizeBytes,
        int DescendantCount,
        bool Partial);

    private sealed class ScanCounter
    {
        public long EntriesProcessed { get; set; }
    }
}

public sealed record ScanExecutionResult(
    IReadOnlyList<OmittedItem> OmittedItems,
    long EntriesProcessed,
    long ElapsedMs,
    bool IsPartial);
