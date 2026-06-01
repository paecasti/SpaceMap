using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Domain;

namespace SpaceMap.Core.Application.Navigation;

public sealed class ListChildrenQuery(IDiskScanService diskScanService)
{
    public Task<ChildListingResult> ExecuteAsync(
        string scanId,
        string path,
        SortMode sort,
        long? minimumSizeBytes,
        int limit = 250,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return diskScanService.ListChildrenAsync(scanId, path, sort, minimumSizeBytes, limit, offset, cancellationToken);
    }
}
