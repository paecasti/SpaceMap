using Fixtures;
using FluentAssertions;
using SpaceMap.Core.Domain;

namespace SpaceMap.Integration.Tests.Performance;

public sealed class ScanPerformanceTests
{
    [Fact]
    public async Task StartScanAsync_HandlesLargeFixtureWithoutFailing()
    {
        using var fixture = new FilesystemFixtureBuilder().WithGeneratedFiles(directories: 40, filesPerDirectory: 50, bytesPerFile: 32);
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
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var restored = await host.DiskScanService.RestoreLastSnapshotAsync();
        restored.Should().NotBeNull();
        restored!.ScopeSummary.Should().NotBeEmpty();
    }
}
