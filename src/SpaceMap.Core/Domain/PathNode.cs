namespace SpaceMap.Core.Domain;

public sealed record PathNode(
    string NodeId,
    string ScanId,
    string? ParentNodeId,
    string FullPath,
    string DisplayName,
    NodeKind Kind,
    int Depth,
    long RealSizeBytes,
    long LogicalSizeBytes,
    int DirectChildCount,
    int DescendantCount,
    bool Partial);
