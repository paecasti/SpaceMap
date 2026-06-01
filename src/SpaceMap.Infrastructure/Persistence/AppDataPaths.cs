namespace SpaceMap.Infrastructure.Persistence;

public sealed class AppDataPaths
{
    public AppDataPaths(string? baseDirectory = null)
    {
        BaseDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpaceMap");
        DatabasePath = Path.Combine(BaseDirectory, "scan-index.db");
        ManifestPath = Path.Combine(BaseDirectory, "restore-manifest.json");
        LogDirectory = Path.Combine(BaseDirectory, "logs");
    }

    public string BaseDirectory { get; }

    public string DatabasePath { get; }

    public string ManifestPath { get; }

    public string LogDirectory { get; }
}
