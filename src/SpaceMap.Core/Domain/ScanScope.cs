namespace SpaceMap.Core.Domain;

public sealed record ScanScope(ScopeMode Mode, IReadOnlyList<string> RootPaths);
