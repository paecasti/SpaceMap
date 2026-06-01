using SpaceMap.Core.Application.Contracts;
using SpaceMap.App.Services;
using SpaceMap.Infrastructure.NativeShell;
using SpaceMap.Infrastructure.Persistence;
using SpaceMap.Infrastructure.Scanning;
using SpaceMap.Infrastructure.Telemetry;

namespace SpaceMap.App.Composition;

public sealed class AppServices(
    IDiskScanService diskScanService,
    StartupRestoreCoordinator startupRestoreCoordinator,
    WindowLifecycleService windowLifecycleService)
{
    public IDiskScanService DiskScanService { get; } = diskScanService;

    public StartupRestoreCoordinator StartupRestoreCoordinator { get; } = startupRestoreCoordinator;

    public WindowLifecycleService WindowLifecycleService { get; } = windowLifecycleService;
}
