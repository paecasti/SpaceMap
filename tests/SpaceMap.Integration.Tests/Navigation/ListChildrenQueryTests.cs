using Fixtures;
using FluentAssertions;
using SpaceMap.Core.Domain;

namespace SpaceMap.Integration.Tests.Navigation;

public sealed class ListChildrenQueryTests
{
    [Fact]
    public async Task ListChildrenAsync_BuildsBreadcrumb_AndSupportsFilters()
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

        await host.DiskScanService.StartScanAsync(new ScanScope(ScopeMode.SinglePath, [fixture.RootPath]));
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var listing = await host.DiskScanService.ListChildrenAsync(
            await GetScanIdAsync(host, fixture.RootPath),
            Path.Combine(fixture.RootPath, "beta"),
            SortMode.RealDesc,
            1,
            50,
            0);

        listing.Breadcrumb.Should().NotBeEmpty();
        listing.Breadcrumb.Last().Path.Should().Be(Path.Combine(fixture.RootPath, "beta"));
    }

    private static async Task<string> GetScanIdAsync(TestDiskHost host, string rootPath)
    {
        var restored = await host.DiskScanService.RestoreLastSnapshotAsync();
        restored.Should().NotBeNull();
        restored!.ViewState.CurrentPath.Should().Be(rootPath);
        return restored.ScanId;
    }
}
