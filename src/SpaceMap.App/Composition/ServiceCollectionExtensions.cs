using SpaceMap.App.Services;
using SpaceMap.Infrastructure.NativeShell;
using SpaceMap.Infrastructure.Persistence;
using SpaceMap.Infrastructure.Scanning;
using SpaceMap.Infrastructure.Telemetry;

namespace SpaceMap.App.Composition;

public static class ServiceCollectionExtensions
{
    public static AppServices CreateAppServices()
    {
        var paths = new AppDataPaths();
        var connectionFactory = new SqliteConnectionFactory(paths);
        var schemaInitializer = new SchemaInitializer(connectionFactory);
        var sessionRepository = new ScanSessionRepository(connectionFactory);
        var pathNodeRepository = new PathNodeRepository(connectionFactory);
        var viewStateRepository = new ViewStateRepository(connectionFactory);
        var manifestStore = new RestoreManifestStore(paths);
        var childListingQueryService = new ChildListingQueryService(pathNodeRepository, sessionRepository);
        var restoreSnapshotQueryService = new RestoreSnapshotQueryService(manifestStore, sessionRepository, pathNodeRepository, viewStateRepository);
        var omissionClassifier = new OmissionClassifier();
        var fileSystemScanner = new FileSystemScanner(omissionClassifier);
        var scanOrchestrator = new ScanOrchestrator();
        var eventStream = new ScanEventStream();
        var loggerFactory = new LoggerConfigurationFactory(paths);
        var scanLogger = new ScanLogger(loggerFactory);
        var breakdownPublisher = new PartialBreakdownPublisher(eventStream);
        var explorerService = new ExplorerService();
        var clipboardService = new ClipboardService();
        var closeGuard = new WindowCloseGuard();
        var diskScanService = new ScanExecutionService(
            schemaInitializer,
            scanOrchestrator,
            fileSystemScanner,
            sessionRepository,
            pathNodeRepository,
            viewStateRepository,
            childListingQueryService,
            restoreSnapshotQueryService,
            manifestStore,
            explorerService,
            clipboardService,
            eventStream,
            breakdownPublisher,
            scanLogger);

        var startupRestoreCoordinator = new StartupRestoreCoordinator(diskScanService, viewStateRepository);
        var windowLifecycleService = new WindowLifecycleService(closeGuard);
        return new AppServices(diskScanService, startupRestoreCoordinator, windowLifecycleService);
    }
}
