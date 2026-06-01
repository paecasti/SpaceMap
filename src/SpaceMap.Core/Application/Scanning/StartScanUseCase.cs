using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Domain;

namespace SpaceMap.Core.Application.Scanning;

public sealed class StartScanUseCase(IDiskScanService diskScanService)
{
    public Task<StartScanResponse> ExecuteAsync(ScanScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.RootPaths.Count == 0)
        {
            throw new ArgumentException("At least one root path is required.", nameof(scope));
        }

        return diskScanService.StartScanAsync(scope, cancellationToken);
    }
}
