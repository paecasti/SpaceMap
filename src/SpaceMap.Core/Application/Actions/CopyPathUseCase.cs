using SpaceMap.Core.Application.Contracts;

namespace SpaceMap.Core.Application.Actions;

public sealed class CopyPathUseCase(IDiskScanService diskScanService)
{
    public Task<NativeActionResult> ExecuteAsync(string path, CancellationToken cancellationToken = default) =>
        diskScanService.CopyPathAsync(path, cancellationToken);
}
