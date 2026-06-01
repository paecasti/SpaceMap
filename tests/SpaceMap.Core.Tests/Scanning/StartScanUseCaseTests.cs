using FluentAssertions;
using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;

namespace SpaceMap.Core.Tests.Scanning;

#pragma warning disable CS0067

public sealed class StartScanUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Throws_WhenRootPathsAreEmpty()
    {
        var useCase = new StartScanUseCase(new StubDiskScanService());
        var act = async () => await useCase.ExecuteAsync(new ScanScope(ScopeMode.SinglePath, Array.Empty<string>()));
        await act.Should().ThrowAsync<ArgumentException>();
    }
}

internal sealed class StubDiskScanService : IDiskScanService
{
    public event EventHandler<ScanProgressEvent>? ScanProgressChanged;
    public event EventHandler<PartialBreakdownEvent>? PartialBreakdownPublished;
    public event EventHandler<ScanIssueEvent>? ScanIssueReported;

    public Task<CancelScanResponse> CancelScanAsync(string scanId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CancelScanResponse(ScanStatus.Cancelled));

    public Task<SpaceMap.Core.Application.Actions.NativeActionResult> CopyPathAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpaceMap.Core.Application.Actions.NativeActionResult(true, null, "ok"));

    public Task<ChildListingResult> ListChildrenAsync(string scanId, string path, SortMode sort, long? minimumSizeBytes, int limit, int offset, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChildListingResult(path, [], 0, false, 0, []));

    public Task<SpaceMap.Core.Application.Actions.NativeActionResult> OpenPathAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpaceMap.Core.Application.Actions.NativeActionResult(true, null, "ok"));

    public Task<RestoreSnapshotResult?> RestoreLastSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<RestoreSnapshotResult?>(null);

    public Task<StartScanResponse> StartScanAsync(ScanScope scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(new StartScanResponse("scan", DateTimeOffset.UtcNow));
}

#pragma warning restore CS0067
