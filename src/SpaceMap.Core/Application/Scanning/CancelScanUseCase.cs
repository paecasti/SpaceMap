using SpaceMap.Core.Application.Contracts;

namespace SpaceMap.Core.Application.Scanning;

public sealed class CancelScanUseCase(IDiskScanService diskScanService)
{
    public Task<CancelScanResponse> ExecuteAsync(string scanId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scanId))
        {
            throw new ArgumentException("scanId is required.", nameof(scanId));
        }

        return diskScanService.CancelScanAsync(scanId, cancellationToken);
    }
}
