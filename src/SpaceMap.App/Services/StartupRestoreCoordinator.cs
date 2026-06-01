using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Domain;
using SpaceMap.Infrastructure.Persistence;

namespace SpaceMap.App.Services;

public sealed class StartupRestoreCoordinator(
    IDiskScanService diskScanService,
    ViewStateRepository viewStateRepository)
{
    public Task<RestoreSnapshotResult?> RestoreAsync(CancellationToken cancellationToken = default) =>
        diskScanService.RestoreLastSnapshotAsync(cancellationToken);

    public Task SaveViewStateAsync(
        string scanId,
        string currentPath,
        IReadOnlyList<string> breadcrumbPaths,
        SortMode sortMode,
        long? minimumSizeBytes,
        string? selectedPath,
        CancellationToken cancellationToken = default) =>
        viewStateRepository.SaveAsync(
            new ViewState(
                scanId,
                currentPath,
                breadcrumbPaths,
                sortMode,
                minimumSizeBytes,
                selectedPath,
                DateTimeOffset.UtcNow),
            cancellationToken);
}
