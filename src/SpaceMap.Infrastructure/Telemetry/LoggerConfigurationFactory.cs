using SpaceMap.Infrastructure.Persistence;

namespace SpaceMap.Infrastructure.Telemetry;

public sealed class LoggerConfigurationFactory(AppDataPaths paths)
{
    public string EnsureLogFile()
    {
        Directory.CreateDirectory(paths.LogDirectory);
        return Path.Combine(paths.LogDirectory, $"session-{DateTime.UtcNow:yyyyMMdd}.log");
    }
}
