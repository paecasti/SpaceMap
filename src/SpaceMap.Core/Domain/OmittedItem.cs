namespace SpaceMap.Core.Domain;

public sealed record OmittedItem(
    string OmittedItemId,
    string ScanId,
    string FullPath,
    string ReasonCode,
    string? ReasonDetail,
    bool AffectsPartialResult,
    DateTimeOffset DiscoveredAt);
