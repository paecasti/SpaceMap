using FluentAssertions;
using SpaceMap.Infrastructure.NativeShell;

namespace SpaceMap.Integration.Tests.Shell;

public sealed class CloseGuardTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ShouldPromptForClose_ReflectsActiveScan(bool activeScan, bool expected)
    {
        var guard = new WindowCloseGuard();
        guard.ShouldPromptForClose(activeScan).Should().Be(expected);
    }
}
