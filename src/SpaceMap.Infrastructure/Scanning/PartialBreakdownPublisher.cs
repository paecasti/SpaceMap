using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;
using SpaceMap.Infrastructure.Telemetry;
using SpaceMap.Core.Application.Navigation;

namespace SpaceMap.Infrastructure.Scanning;

public sealed class PartialBreakdownPublisher(ScanEventStream eventStream)
{
    public void Publish(string scanId, string scopeLabel, IReadOnlyList<ListChildItem> items, long entriesProcessed)
    {
        var contributors = items
            .Where(node => node.Kind == NodeKind.Directory)
            .OrderByDescending(node => node.RealSizeBytes)
            .Take(8)
            .Select(node => new BreakdownItem(node.Path, node.Kind, node.RealSizeBytes, node.LogicalSizeBytes, node.Partial))
            .ToArray();

        eventStream.PublishPartialBreakdown(new PartialBreakdownEvent(scanId, scopeLabel, contributors, entriesProcessed));
    }
}
