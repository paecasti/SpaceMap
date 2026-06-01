namespace SpaceMap.Core.Domain;

public sealed record ScanSession(
    string ScanId,
    ScanScope Scope,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    ScanStatus Status,
    long EntriesProcessed,
    bool IsPartial,
    bool Outdated,
    string? LastErrorSummary);
