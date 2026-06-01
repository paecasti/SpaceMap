using System.Diagnostics;
using SpaceMap.Core.Application.Actions;

namespace SpaceMap.Infrastructure.NativeShell;

public sealed class ClipboardService
{
    public async Task<NativeActionResult> CopyAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new NativeActionResult(false, "INVALID_PATH", "A non-empty path is required.");
        }

        try
        {
            var startInfo = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command Set-Clipboard -Value @'\n{path}\n'@")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new NativeActionResult(false, "COPY_FAILED", "Clipboard process did not start.");
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0
                ? new NativeActionResult(true, null, "Path copied to clipboard.")
                : new NativeActionResult(false, "COPY_FAILED", "Clipboard copy failed.");
        }
        catch (Exception)
        {
            return new NativeActionResult(false, "COPY_FAILED", "Clipboard copy failed.");
        }
    }
}
