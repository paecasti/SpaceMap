using System.Text.Json;

namespace SpaceMap.Infrastructure.Persistence;

public sealed record RestoreManifest(string LastConfirmedScanId, DateTimeOffset SavedAt);

public sealed class RestoreManifestStore(AppDataPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task SaveAsync(string scanId, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.BaseDirectory);
        await File.WriteAllTextAsync(
            paths.ManifestPath,
            JsonSerializer.Serialize(new RestoreManifest(scanId, DateTimeOffset.UtcNow), JsonOptions),
            cancellationToken);
    }

    public async Task<RestoreManifest?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(paths.ManifestPath))
        {
            return null;
        }

        var payload = await File.ReadAllTextAsync(paths.ManifestPath, cancellationToken);
        return JsonSerializer.Deserialize<RestoreManifest>(payload, JsonOptions);
    }
}
