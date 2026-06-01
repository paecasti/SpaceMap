using System.IO;
using FluentAssertions;
using SpaceMap.App.ViewModels;
using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;
using SpaceMap.App.Services;
using SpaceMap.Infrastructure.Persistence;

namespace SpaceMap.Desktop.Tests.Launch;

#pragma warning disable CS0067

public sealed class StartupFlowTests
{
    [Fact]
    public async Task InitializeAsync_AppliesRestoredState()
    {
        var service = new FakeDiskScanService();
        var viewModel = new MainWindowViewModel(service, CreateCoordinator(service));

        await viewModel.InitializeAsync();

        viewModel.Navigation.CurrentPath.Should().Be(@"C:\demo");
        viewModel.RestoredSnapshotBanner.IsVisible.Should().BeTrue();
    }

    private static StartupRestoreCoordinator CreateCoordinator(IDiskScanService service)
    {
        var paths = new AppDataPaths(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var factory = new SqliteConnectionFactory(paths);
        var initializer = new SchemaInitializer(factory);
        initializer.InitializeAsync().GetAwaiter().GetResult();
        return new StartupRestoreCoordinator(service, new ViewStateRepository(factory));
    }
}

internal sealed class FakeDiskScanService : IDiskScanService
{
    public event EventHandler<ScanProgressEvent>? ScanProgressChanged;
    public event EventHandler<PartialBreakdownEvent>? PartialBreakdownPublished;
    public event EventHandler<ScanIssueEvent>? ScanIssueReported;

    public Task<CancelScanResponse> CancelScanAsync(string scanId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CancelScanResponse(ScanStatus.Cancelled));

    public Task<SpaceMap.Core.Application.Actions.NativeActionResult> CopyPathAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpaceMap.Core.Application.Actions.NativeActionResult(true, null, "ok"));

    public Task<ChildListingResult> ListChildrenAsync(string scanId, string path, SortMode sort, long? minimumSizeBytes, int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChildListingResult(path, [new ListChildItem(path + "\\folder", NodeKind.Directory, 10, 10, false)], 1, false, 0, [new BreadcrumbItem(path, "demo")]));

    public Task<SpaceMap.Core.Application.Actions.NativeActionResult> OpenPathAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpaceMap.Core.Application.Actions.NativeActionResult(true, null, "ok"));

    public Task<RestoreSnapshotResult?> RestoreLastSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<RestoreSnapshotResult?>(new RestoreSnapshotResult("scan-1", true, ScanStatus.Completed, [@"C:\demo"], new ViewStateDto("scan-1", @"C:\demo", [@"C:\demo"], SortMode.RealDesc, null, null, DateTimeOffset.UtcNow), []));

    public Task<StartScanResponse> StartScanAsync(ScanScope scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(new StartScanResponse("scan-1", DateTimeOffset.UtcNow));
}

#pragma warning restore CS0067
