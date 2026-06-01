using System.Diagnostics;
using SpaceMap.Core.Application.Actions;

namespace SpaceMap.Infrastructure.NativeShell;

public sealed class ExplorerService
{
    public Task<NativeActionResult> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return Task.FromResult(new NativeActionResult(false, "PATH_NOT_FOUND", "The selected path no longer exists."));
        }

        try
        {
            var startInfo = new ProcessStartInfo("explorer.exe", $"\"{path}\"")
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
            return Task.FromResult(new NativeActionResult(true, null, "Path opened in Explorer."));
        }
        catch (Exception)
        {
            return Task.FromResult(new NativeActionResult(false, "OPEN_FAILED", "Windows Explorer rejected the request."));
        }
    }
}
