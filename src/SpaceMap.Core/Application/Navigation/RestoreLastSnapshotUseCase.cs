using SpaceMap.Core.Application.Contracts;

namespace SpaceMap.Core.Application.Navigation;

public sealed class RestoreLastSnapshotUseCase(IDiskScanService diskScanService)
{
    public Task<RestoreSnapshotResult?> ExecuteAsync(CancellationToken cancellationToken = default) =>
        diskScanService.RestoreLastSnapshotAsync(cancellationToken);
}
