namespace SpaceMap.Core.Domain;

public sealed record ViewState(
    string ScanId,
    string CurrentPath,
    IReadOnlyList<string> BreadcrumbPaths,
    SortMode SortMode,
    long? MinimumSizeBytes,
    string? SelectedPath,
    DateTimeOffset RestoredAt);
