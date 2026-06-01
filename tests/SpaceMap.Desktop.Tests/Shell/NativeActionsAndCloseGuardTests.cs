using FluentAssertions;
using SpaceMap.App.Services;
using SpaceMap.Infrastructure.NativeShell;

namespace SpaceMap.Desktop.Tests.Shell;

public sealed class NativeActionsAndCloseGuardTests
{
    [Fact]
    public void WindowCloseGuard_PromptsWhenScanIsActive()
    {
        var service = new WindowLifecycleService(new WindowCloseGuard());
        service.Should().NotBeNull();
        new WindowCloseGuard().ShouldPromptForClose(true).Should().BeTrue();
    }
}
