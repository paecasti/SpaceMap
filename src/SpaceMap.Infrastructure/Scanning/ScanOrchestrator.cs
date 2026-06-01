using System.Collections.Concurrent;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Scanning;

public sealed class ScanOrchestrator
{
    private readonly ConcurrentDictionary<string, ActiveScan> _activeScans = new(StringComparer.OrdinalIgnoreCase);

    public (string ScanId, CancellationToken Token) Start(ScanScope scope)
    {
        var scanId = Guid.NewGuid().ToString("N");
        var cts = new CancellationTokenSource();
        _activeScans[scanId] = new ActiveScan(scope, cts);
        return (scanId, cts.Token);
    }

    public bool TryCancel(string scanId)
    {
        if (!_activeScans.TryGetValue(scanId, out var activeScan))
        {
            return false;
        }

        activeScan.CancellationTokenSource.Cancel();
        return true;
    }

    public ScanScope? GetScope(string scanId) =>
        _activeScans.TryGetValue(scanId, out var activeScan) ? activeScan.Scope : null;

    public void Complete(string scanId) => _activeScans.TryRemove(scanId, out _);

    public bool IsActive(string scanId) => _activeScans.ContainsKey(scanId);

    private sealed record ActiveScan(ScanScope Scope, CancellationTokenSource CancellationTokenSource);
}
