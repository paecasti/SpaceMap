using SpaceMap.Core.Application.Actions;
using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;
using SpaceMap.Infrastructure.NativeShell;
using SpaceMap.Infrastructure.Persistence;
using SpaceMap.Infrastructure.Telemetry;

namespace SpaceMap.Infrastructure.Scanning;

public sealed class ScanExecutionService(
    SchemaInitializer schemaInitializer,
    ScanOrchestrator scanOrchestrator,
    FileSystemScanner fileSystemScanner,
    ScanSessionRepository scanSessionRepository,
    PathNodeRepository pathNodeRepository,
    ViewStateRepository viewStateRepository,
    ChildListingQueryService childListingQueryService,
    RestoreSnapshotQueryService restoreSnapshotQueryService,
    RestoreManifestStore restoreManifestStore,
    ExplorerService explorerService,
    ClipboardService clipboardService,
    ScanEventStream eventStream,
    PartialBreakdownPublisher partialBreakdownPublisher,
    ScanLogger scanLogger) : IDiskScanService
{
    public event EventHandler<ScanProgressEvent>? ScanProgressChanged
    {
        add => eventStream.ScanProgressChanged += value;
        remove => eventStream.ScanProgressChanged -= value;
    }

    public event EventHandler<PartialBreakdownEvent>? PartialBreakdownPublished
    {
        add => eventStream.PartialBreakdownPublished += value;
        remove => eventStream.PartialBreakdownPublished -= value;
    }

    public event EventHandler<ScanIssueEvent>? ScanIssueReported
    {
        add => eventStream.ScanIssueReported += value;
        remove => eventStream.ScanIssueReported -= value;
    }

    public async Task<StartScanResponse> StartScanAsync(ScanScope scope, CancellationToken cancellationToken = default)
    {
        await schemaInitializer.InitializeAsync(cancellationToken);

        var (scanId, scanToken) = scanOrchestrator.Start(scope);
        var acceptedAt = DateTimeOffset.UtcNow;
        var session = new ScanSession(scanId, scope, acceptedAt, null, ScanStatus.Running, 0, false, false, null);
        await scanSessionRepository.UpsertAsync(session, cancellationToken);
        eventStream.PublishProgress(new ScanProgressEvent(scanId, ScanStatus.Running, 0, 0, false, scope.RootPaths.FirstOrDefault() ?? string.Empty));

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await scanLogger.LogAsync($"Scan started: {scanId}");
                    long lastPublishedEntries = 0;
                    await pathNodeRepository.DeleteNodesAsync(scanId, CancellationToken.None);

                    var result = await fileSystemScanner.ScanAsync(
                        scanId,
                        scope,
                        scanToken,
                        entriesProcessed =>
                        {
                            if (entriesProcessed - lastPublishedEntries < 250 && entriesProcessed != 1)
                            {
                                return;
                            }

                            lastPublishedEntries = entriesProcessed;
                            eventStream.PublishProgress(
                                new ScanProgressEvent(
                                    scanId,
                                    ScanStatus.Running,
                                    0,
                                    entriesProcessed,
                                    false,
                                    scope.RootPaths.FirstOrDefault() ?? string.Empty));
                        },
                        omission =>
                        {
                            eventStream.PublishIssue(
                                new ScanIssueEvent(
                                    omission.ScanId,
                                    omission.FullPath,
                                    omission.ReasonCode,
                                    omission.ReasonDetail,
                                    omission.AffectsPartialResult));
                        },
                        async (nodes, token) => await pathNodeRepository.AppendNodesAsync(nodes, token));

                    var finalStatus = result.IsPartial ? ScanStatus.PartialCompleted : ScanStatus.Completed;
                    var completedSession = session with
                    {
                        CompletedAt = DateTimeOffset.UtcNow,
                        Status = finalStatus,
                        EntriesProcessed = result.EntriesProcessed,
                        IsPartial = result.IsPartial
                    };

                    await scanSessionRepository.ReplaceOmittedItemsAsync(scanId, result.OmittedItems, CancellationToken.None);
                    await scanSessionRepository.UpsertAsync(completedSession, CancellationToken.None);

                    var restorePath = scope.Mode == ScopeMode.AllLocalDrives ? "Local drives" : scope.RootPaths.First();
                    await viewStateRepository.SaveAsync(
                        new ViewState(scanId, restorePath, [restorePath], SortMode.RealDesc, null, null, DateTimeOffset.UtcNow),
                        CancellationToken.None);
                    await restoreManifestStore.SaveAsync(scanId, CancellationToken.None);

                    var rootListing = await childListingQueryService.ListAsync(
                        scanId,
                        restorePath,
                        SortMode.RealDesc,
                        null,
                        8,
                        0,
                        CancellationToken.None);
                    partialBreakdownPublisher.Publish(scanId, restorePath, rootListing.Items, result.EntriesProcessed);
                    eventStream.PublishProgress(
                        new ScanProgressEvent(
                            scanId,
                            finalStatus,
                            result.ElapsedMs,
                            result.EntriesProcessed,
                            result.IsPartial,
                            restorePath));
                    await scanLogger.LogAsync($"Scan completed: {scanId} [{finalStatus}]");
                }
                catch (OperationCanceledException)
                {
                    var cancelled = session with
                    {
                        CompletedAt = DateTimeOffset.UtcNow,
                        Status = ScanStatus.Cancelled
                    };
                    await scanSessionRepository.UpsertAsync(cancelled, CancellationToken.None);
                    eventStream.PublishProgress(
                        new ScanProgressEvent(scanId, ScanStatus.Cancelled, 0, cancelled.EntriesProcessed, cancelled.IsPartial, scope.RootPaths.FirstOrDefault() ?? string.Empty));
                    await scanLogger.LogAsync($"Scan cancelled: {scanId}");
                }
                catch (Exception ex)
                {
                    var failed = session with
                    {
                        CompletedAt = DateTimeOffset.UtcNow,
                        Status = ScanStatus.Failed,
                        LastErrorSummary = ex.Message
                    };
                    await scanSessionRepository.UpsertAsync(failed, CancellationToken.None);
                    eventStream.PublishProgress(
                        new ScanProgressEvent(scanId, ScanStatus.Failed, 0, failed.EntriesProcessed, false, scope.RootPaths.FirstOrDefault() ?? string.Empty));
                    await scanLogger.LogAsync($"Scan failed: {scanId} [{ex.Message}]");
                }
                finally
                {
                    scanOrchestrator.Complete(scanId);
                }
            },
            CancellationToken.None);

        return new StartScanResponse(scanId, acceptedAt);
    }

    public Task<CancelScanResponse> CancelScanAsync(string scanId, CancellationToken cancellationToken = default)
    {
        var cancelled = scanOrchestrator.TryCancel(scanId);
        return Task.FromResult(new CancelScanResponse(cancelled ? ScanStatus.Cancelling : ScanStatus.Cancelled));
    }

    public Task<ChildListingResult> ListChildrenAsync(
        string scanId,
        string path,
        SortMode sort,
        long? minimumSizeBytes,
        int limit,
        int offset,
        CancellationToken cancellationToken = default) =>
        childListingQueryService.ListAsync(scanId, path, sort, minimumSizeBytes, limit, offset, cancellationToken);

    public Task<RestoreSnapshotResult?> RestoreLastSnapshotAsync(CancellationToken cancellationToken = default) =>
        restoreSnapshotQueryService.RestoreAsync(cancellationToken);

    public Task<NativeActionResult> OpenPathAsync(string path, CancellationToken cancellationToken = default) =>
        explorerService.OpenAsync(path, cancellationToken);

    public Task<NativeActionResult> CopyPathAsync(string path, CancellationToken cancellationToken = default) =>
        clipboardService.CopyAsync(path, cancellationToken);
}
