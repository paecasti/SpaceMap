namespace SpaceMap.Core.Domain;

public enum ScopeMode
{
    SinglePath,
    SingleDrive,
    AllLocalDrives
}

public enum ScanStatus
{
    Running,
    PartialCompleted,
    Completed,
    Failed,
    Cancelled,
    Cancelling
}

public enum NodeKind
{
    Directory,
    File
}

public enum SortMode
{
    RealDesc,
    LogicalDesc,
    NameAsc
}

public enum NativeActionType
{
    OpenLocation,
    CopyPath
}
