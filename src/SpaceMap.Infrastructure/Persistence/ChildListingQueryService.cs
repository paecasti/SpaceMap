using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Persistence;

public sealed class ChildListingQueryService(
    PathNodeRepository pathNodeRepository,
    ScanSessionRepository sessionRepository)
{
    public async Task<ChildListingResult> ListAsync(
        string scanId,
        string path,
        SortMode sortMode,
        long? minimumSizeBytes,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var items = await pathNodeRepository.ListChildrenAsync(scanId, path, sortMode, minimumSizeBytes, limit, offset, cancellationToken);
        var total = await pathNodeRepository.CountChildrenAsync(scanId, path, minimumSizeBytes, cancellationToken);
        var breadcrumb = await BuildBreadcrumbAsync(scanId, path, cancellationToken);
        var omitted = await sessionRepository.GetOmittedItemsAsync(scanId, cancellationToken);
        var partial = items.Any(x => x.Partial) || omitted.Any(item => item.FullPath.StartsWith(path, StringComparison.OrdinalIgnoreCase));

        return new ChildListingResult(
            path,
            items.Select(
                node => new ListChildItem(
                    node.FullPath,
                    node.Kind,
                    node.RealSizeBytes,
                    node.LogicalSizeBytes,
                    node.Partial)).ToArray(),
            total,
            partial,
            omitted.Count,
            breadcrumb);
    }

    private async Task<IReadOnlyList<BreadcrumbItem>> BuildBreadcrumbAsync(
        string scanId,
        string path,
        CancellationToken cancellationToken)
    {
        if (string.Equals(path, "Local drives", StringComparison.OrdinalIgnoreCase))
        {
            return [new BreadcrumbItem("Local drives", "Local drives")];
        }

        var roots = await sessionRepository.GetRootPathsAsync(scanId, cancellationToken);
        var root = roots.FirstOrDefault(rootPath => path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) ?? roots.FirstOrDefault() ?? path;
        if (string.Equals(root, path, StringComparison.OrdinalIgnoreCase))
        {
            return [new BreadcrumbItem(root, LabelForPath(root))];
        }

        var parts = new List<BreadcrumbItem> { new(root, LabelForPath(root)) };
        var remaining = path[root.Length..].TrimStart(Path.DirectorySeparatorChar);
        var current = root.TrimEnd(Path.DirectorySeparatorChar);
        foreach (var segment in remaining.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = current.EndsWith(':') ? $"{current}{Path.DirectorySeparatorChar}{segment}" : $"{current}{Path.DirectorySeparatorChar}{segment}";
            parts.Add(new BreadcrumbItem(current, segment));
        }

        return parts;
    }

    internal static string LabelForPath(string path)
    {
        if (string.Equals(path, "Local drives", StringComparison.OrdinalIgnoreCase))
        {
            return "Local drives";
        }

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar);
        if (trimmed.EndsWith(':'))
        {
            return $"{trimmed}{Path.DirectorySeparatorChar}";
        }

        return Path.GetFileName(trimmed);
    }
}
