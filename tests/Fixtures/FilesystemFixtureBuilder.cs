namespace Fixtures;

public sealed class FilesystemFixtureBuilder : IDisposable
{
    private readonly string _rootPath;

    public FilesystemFixtureBuilder()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "SpaceMap.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    public string RootPath => _rootPath;

    public FilesystemFixtureBuilder WithSampleTree()
    {
        TestTreeSeeder.CreateSampleTree(_rootPath);
        return this;
    }

    public FilesystemFixtureBuilder WithGeneratedFiles(int directories, int filesPerDirectory, int bytesPerFile = 64)
    {
        for (var directoryIndex = 0; directoryIndex < directories; directoryIndex++)
        {
            var directory = Path.Combine(_rootPath, $"dir-{directoryIndex:D3}");
            Directory.CreateDirectory(directory);
            for (var fileIndex = 0; fileIndex < filesPerDirectory; fileIndex++)
            {
                var file = Path.Combine(directory, $"file-{fileIndex:D4}.bin");
                File.WriteAllBytes(file, Enumerable.Repeat((byte)(fileIndex % 255), bytesPerFile).ToArray());
            }
        }

        return this;
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
