namespace SpaceMap.Infrastructure.NativeShell;

public sealed class WindowCloseGuard
{
    public bool ShouldPromptForClose(bool hasActiveScan) => hasActiveScan;
}
