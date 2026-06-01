using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Scanning;

public sealed class OmissionClassifier
{
    public OmittedItem PermissionDenied(string scanId, string path, string? detail = null) =>
        Create(scanId, path, "permission_denied", detail ?? "The path could not be read.", true);

    public OmittedItem SymlinkSkipped(string scanId, string path, string? detail = null) =>
        Create(scanId, path, "symlink_skipped", detail ?? "Links and junctions are skipped for safety.", true);

    public OmittedItem PathUnavailable(string scanId, string path, string? detail = null) =>
        Create(scanId, path, "path_unavailable", detail ?? "The path is unavailable.", true);

    public OmittedItem IoError(string scanId, string path, string? detail = null) =>
        Create(scanId, path, "io_error", detail ?? "A filesystem error occurred.", true);

    private static OmittedItem Create(string scanId, string path, string code, string? detail, bool affectsPartial) =>
        new(Guid.NewGuid().ToString("N"), scanId, path, code, detail, affectsPartial, DateTimeOffset.UtcNow);
}
