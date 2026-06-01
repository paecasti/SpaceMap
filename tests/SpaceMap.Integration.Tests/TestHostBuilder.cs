using SpaceMap.Core.Application.Contracts;
using SpaceMap.Infrastructure.NativeShell;
using SpaceMap.Infrastructure.Persistence;
using SpaceMap.Infrastructure.Scanning;
using SpaceMap.Infrastructure.Telemetry;

namespace SpaceMap.Integration.Tests;

public static class TestHostBuilder
{
    public static TestDiskHost Build(string baseDirectory)
    {
        var paths = new AppDataPaths(baseDirectory);
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
        var orchestrator = new ScanOrchestrator();
        var eventStream = new ScanEventStream();
        var loggerFactory = new LoggerConfigurationFactory(paths);
        var scanLogger = new ScanLogger(loggerFactory);
        var breakdownPublisher = new PartialBreakdownPublisher(eventStream);
        var explorerService = new ExplorerService();
        var clipboardService = new ClipboardService();

        IDiskScanService service = new ScanExecutionService(
            schemaInitializer,
            orchestrator,
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

        return new TestDiskHost(service, viewStateRepository);
    }
}

public sealed record TestDiskHost(IDiskScanService DiskScanService, ViewStateRepository ViewStateRepository);
