using SpaceMap.Core.Application.Actions;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;

namespace SpaceMap.Core.Application.Contracts;

public sealed record StartScanResponse(string ScanId, DateTimeOffset AcceptedAt);

public sealed record CancelScanResponse(ScanStatus Status);

public interface IDiskScanService
{
    event EventHandler<ScanProgressEvent>? ScanProgressChanged;
    event EventHandler<PartialBreakdownEvent>? PartialBreakdownPublished;
    event EventHandler<ScanIssueEvent>? ScanIssueReported;

    Task<StartScanResponse> StartScanAsync(ScanScope scope, CancellationToken cancellationToken = default);
    Task<CancelScanResponse> CancelScanAsync(string scanId, CancellationToken cancellationToken = default);
    Task<ChildListingResult> ListChildrenAsync(
        string scanId,
        string path,
        SortMode sort,
        long? minimumSizeBytes,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);
    Task<RestoreSnapshotResult?> RestoreLastSnapshotAsync(CancellationToken cancellationToken = default);
    Task<NativeActionResult> OpenPathAsync(string path, CancellationToken cancellationToken = default);
    Task<NativeActionResult> CopyPathAsync(string path, CancellationToken cancellationToken = default);
}
