namespace SpaceMap.Infrastructure.Telemetry;

public sealed class ScanLogger(LoggerConfigurationFactory loggerConfigurationFactory)
{
    public Task LogAsync(string message, CancellationToken cancellationToken = default)
    {
        var line = $"[{DateTimeOffset.UtcNow:O}] {message}{Environment.NewLine}";
        return File.AppendAllTextAsync(loggerConfigurationFactory.EnsureLogFile(), line, cancellationToken);
    }
}
