using SpaceMap.Core.Domain;

namespace SpaceMap.Core.Application.Scanning;

public sealed record ScanProgressEvent(
    string ScanId,
    ScanStatus State,
    long ElapsedMs,
    long EntriesProcessed,
    bool IsPartial,
    string CurrentRootPath);

public sealed record PartialBreakdownEvent(
    string ScanId,
    string Scope,
    IReadOnlyList<BreakdownItem> TopContributors,
    long AsOfProcessedEntries);

public sealed record BreakdownItem(
    string Path,
    NodeKind Kind,
    long RealSizeBytes,
    long LogicalSizeBytes,
    bool Partial);

public sealed record ScanIssueEvent(
    string ScanId,
    string Path,
    string ReasonCode,
    string? ReasonDetail,
    bool AffectsPartialResult);
