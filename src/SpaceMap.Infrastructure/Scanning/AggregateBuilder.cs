using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Scanning;

public static class AggregateBuilder
{
    public static IReadOnlyList<BreakdownItem> BuildTopContributors(IEnumerable<PathNode> nodes, string currentPath, int count = 8)
    {
        return nodes
            .Where(node => string.Equals(node.ParentNodeId, currentPath, StringComparison.OrdinalIgnoreCase) || node.Depth == 0)
            .OrderByDescending(node => node.RealSizeBytes)
            .ThenBy(node => node.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .Select(node => new BreakdownItem(node.FullPath, node.Kind, node.RealSizeBytes, node.LogicalSizeBytes, node.Partial))
            .ToArray();
    }
}
