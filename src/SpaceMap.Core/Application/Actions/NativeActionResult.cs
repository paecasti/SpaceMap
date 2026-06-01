namespace SpaceMap.Core.Application.Actions;

public sealed record NativeActionResult(bool Succeeded, string? ErrorCode, string Message);
