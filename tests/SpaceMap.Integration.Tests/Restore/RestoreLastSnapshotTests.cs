using Fixtures;
using FluentAssertions;
using SpaceMap.Core.Domain;

namespace SpaceMap.Integration.Tests.Restore;

public sealed class RestoreLastSnapshotTests
{
    [Fact]
    public async Task RestoreLastSnapshotAsync_ReturnsLastConfirmedSession()
    {
        using var fixture = new FilesystemFixtureBuilder().WithSampleTree();
        var host = TestHostBuilder.Build(Path.Combine(fixture.RootPath, ".state"));
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        host.DiskScanService.ScanProgressChanged += (_, args) =>
        {
            if (args.State is ScanStatus.Completed or ScanStatus.PartialCompleted)
            {
                completion.TrySetResult();
            }
        };

        var started = await host.DiskScanService.StartScanAsync(new ScanScope(ScopeMode.SinglePath, [fixture.RootPath]));
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var restored = await host.DiskScanService.RestoreLastSnapshotAsync();
        restored.Should().NotBeNull();
        restored!.ScanId.Should().Be(started.ScanId);
        restored.Outdated.Should().BeTrue();
        restored.ViewState.CurrentPath.Should().Be(fixture.RootPath);
    }
}
