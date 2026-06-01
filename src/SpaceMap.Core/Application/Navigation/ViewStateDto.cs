using SpaceMap.Core.Domain;

namespace SpaceMap.Core.Application.Navigation;

public sealed record BreadcrumbItem(string Path, string Label);

public sealed record ListChildItem(
    string Path,
    NodeKind Kind,
    long RealSizeBytes,
    long LogicalSizeBytes,
    bool Partial);

public sealed record ChildListingResult(
    string Path,
    IReadOnlyList<ListChildItem> Items,
    int Total,
    bool Partial,
    int OmittedCount,
    IReadOnlyList<BreadcrumbItem> Breadcrumb);

public sealed record ViewStateDto(
    string ScanId,
    string CurrentPath,
    IReadOnlyList<string> BreadcrumbPaths,
    SortMode SortMode,
    long? MinimumSizeBytes,
    string? SelectedPath,
    DateTimeOffset RestoredAt);

public sealed record RestoreSnapshotResult(
    string ScanId,
    bool Outdated,
    ScanStatus Status,
    IReadOnlyList<string> ScopeSummary,
    ViewStateDto ViewState,
    IReadOnlyList<OmittedSummary> OmittedItems);

public sealed record OmittedSummary(
    string FullPath,
    string ReasonCode,
    string? ReasonDetail,
    bool AffectsPartialResult);
