using SpaceMap.Core.Application.Scanning;

namespace SpaceMap.Infrastructure.Telemetry;

public sealed class ScanEventStream
{
    public event EventHandler<ScanProgressEvent>? ScanProgressChanged;
    public event EventHandler<PartialBreakdownEvent>? PartialBreakdownPublished;
    public event EventHandler<ScanIssueEvent>? ScanIssueReported;

    public void PublishProgress(ScanProgressEvent progress) => ScanProgressChanged?.Invoke(this, progress);

    public void PublishPartialBreakdown(PartialBreakdownEvent breakdown) => PartialBreakdownPublished?.Invoke(this, breakdown);

    public void PublishIssue(ScanIssueEvent issue) => ScanIssueReported?.Invoke(this, issue);
}
