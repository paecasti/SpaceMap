using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Persistence;

public sealed class RestoreSnapshotQueryService(
    RestoreManifestStore manifestStore,
    ScanSessionRepository scanSessionRepository,
    PathNodeRepository pathNodeRepository,
    ViewStateRepository viewStateRepository)
{
    public async Task<RestoreSnapshotResult?> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await manifestStore.LoadAsync(cancellationToken);
        if (manifest is null)
        {
            return null;
        }

        var session = await scanSessionRepository.GetAsync(manifest.LastConfirmedScanId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var viewState = await viewStateRepository.GetAsync(session.ScanId, cancellationToken)
            ?? new ViewState(
                session.ScanId,
                session.Scope.Mode == ScopeMode.AllLocalDrives ? "Local drives" : session.Scope.RootPaths.First(),
                [session.Scope.Mode == ScopeMode.AllLocalDrives ? "Local drives" : session.Scope.RootPaths.First()],
                SortMode.RealDesc,
                null,
                null,
                DateTimeOffset.UtcNow);

        var rootNodes = await pathNodeRepository.GetRootNodesAsync(session.ScanId, cancellationToken);
        var omitted = await scanSessionRepository.GetOmittedItemsAsync(session.ScanId, cancellationToken);

        return new RestoreSnapshotResult(
            session.ScanId,
            true,
            session.Status,
            rootNodes.Select(x => x.FullPath).ToArray(),
            new ViewStateDto(
                viewState.ScanId,
                viewState.CurrentPath,
                viewState.BreadcrumbPaths,
                viewState.SortMode,
                viewState.MinimumSizeBytes,
                viewState.SelectedPath,
                viewState.RestoredAt),
            omitted.Select(x => new OmittedSummary(x.FullPath, x.ReasonCode, x.ReasonDetail, x.AffectsPartialResult)).ToArray());
    }
}
