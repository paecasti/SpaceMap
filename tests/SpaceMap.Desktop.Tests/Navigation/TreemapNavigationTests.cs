using System.IO;
using FluentAssertions;
using SpaceMap.App.Services;
using SpaceMap.App.ViewModels;
using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;
using SpaceMap.Infrastructure.Persistence;

namespace SpaceMap.Desktop.Tests.Navigation;

#pragma warning disable CS0067

public sealed class TreemapNavigationTests
{
    [Fact]
    public async Task NavigateCommand_LoadsListingIntoViewModel()
    {
        var service = new FakeNavigationDiskScanService();
        var viewModel = new MainWindowViewModel(service, CreateCoordinator(service));

        await viewModel.InitializeAsync();
        viewModel.NavigateCommand.Execute(@"C:\demo\folder");
        await Task.Delay(300);

        viewModel.DirectoryBreakdown.Items.Should().NotBeEmpty();
        viewModel.Navigation.BreadcrumbItems.Should().HaveCountGreaterThan(0);
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

internal sealed class FakeNavigationDiskScanService : IDiskScanService
{
    public event EventHandler<ScanProgressEvent>? ScanProgressChanged;
    public event EventHandler<PartialBreakdownEvent>? PartialBreakdownPublished;
    public event EventHandler<ScanIssueEvent>? ScanIssueReported;

    public Task<CancelScanResponse> CancelScanAsync(string scanId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CancelScanResponse(ScanStatus.Cancelled));

    public Task<SpaceMap.Core.Application.Actions.NativeActionResult> CopyPathAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpaceMap.Core.Application.Actions.NativeActionResult(true, null, "ok"));

    public Task<ChildListingResult> ListChildrenAsync(string scanId, string path, SortMode sort, long? minimumSizeBytes, int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChildListingResult(path, [new ListChildItem(path + "\\child", NodeKind.Directory, 42, 42, false)], 1, false, 0, [new BreadcrumbItem(@"C:\demo", "demo"), new BreadcrumbItem(path, "child")]));

    public Task<SpaceMap.Core.Application.Actions.NativeActionResult> OpenPathAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpaceMap.Core.Application.Actions.NativeActionResult(true, null, "ok"));

    public Task<RestoreSnapshotResult?> RestoreLastSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<RestoreSnapshotResult?>(new RestoreSnapshotResult("scan-nav", true, ScanStatus.Completed, [@"C:\demo"], new ViewStateDto("scan-nav", @"C:\demo", [@"C:\demo"], SortMode.RealDesc, null, null, DateTimeOffset.UtcNow), []));

    public Task<StartScanResponse> StartScanAsync(ScanScope scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(new StartScanResponse("scan-nav", DateTimeOffset.UtcNow));
}

#pragma warning restore CS0067
