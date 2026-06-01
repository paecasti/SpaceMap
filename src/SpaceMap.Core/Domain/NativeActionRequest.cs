namespace SpaceMap.Core.Domain;

public sealed record NativeActionRequest(
    string RequestId,
    string TargetPath,
    NativeActionType ActionType,
    DateTimeOffset RequestedAt,
    string Result,
    string? ErrorCode);
