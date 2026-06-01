using Fixtures;
using FluentAssertions;
using SpaceMap.Core.Domain;

namespace SpaceMap.Integration.Tests.Scanning;

public sealed class StartScanWorkflowTests
{
    [Fact]
    public async Task StartScanAsync_CompletesReadOnlyTraversal_AndPublishesResults()
    {
        using var fixture = new FilesystemFixtureBuilder().WithSampleTree();
        var host = TestHostBuilder.Build(Path.Combine(fixture.RootPath, ".state"));
        var completion = new TaskCompletionSource<ScanStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

        host.DiskScanService.ScanProgressChanged += (_, args) =>
        {
            if (args.State is ScanStatus.Completed or ScanStatus.PartialCompleted)
            {
                completion.TrySetResult(args.State);
            }
        };

        var started = await host.DiskScanService.StartScanAsync(new ScanScope(ScopeMode.SinglePath, [fixture.RootPath]));
        var state = await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));

        state.Should().Be(ScanStatus.Completed);
        var listing = await host.DiskScanService.ListChildrenAsync(started.ScanId, fixture.RootPath, SortMode.RealDesc, null, 50, 0);
        listing.Items.Should().NotBeEmpty();
        listing.Items.Select(x => x.Path).Should().Contain(path => path.Contains("alpha"));
    }
}
