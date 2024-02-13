using System;
using LibHac.Common;
using LibHac.Fs.Impl;
using LibHac.Fs.Shim;
using LibHac.FsSrv.FsCreator;
using LibHac.FsSrv.Impl;
using LibHac.FsSrv.Sf;
using LibHac.FsSrv.Storage;
using LibHac.FsSystem;
using LibHac.Sm;

namespace LibHac.FsSrv;

public static class FileSystemServerInitializer
{
    private const ulong SpeedEmulationProgramIdMinimum = 0x100000000000000;
    private const ulong SpeedEmulationProgramIdMaximum = 0x100000000001FFF;

    private const int BufferManagerHeapSize = 1024 * 1024 * 14;
    private const int BufferManagerCacheSize = 1024;
    private const int BufferManagerBlockSize = 0x4000;

    /// <summary>
    /// Initializes a <see cref="FileSystemServer"/> with the provided <see cref="FileSystemServerConfig"/>.
    /// </summary>
    /// <param name="client">The <see cref="HorizonClient"/> that <paramref name="server"/> was created with.</param>
    /// <param name="server">The <see cref="FileSystemServer"/> to initialize.</param>
    /// <param name="config">The config for initializing <paramref name="server"/>.</param>
    public static void InitializeWithConfig(HorizonClient client, FileSystemServer server, FileSystemServerConfig config)
    {
        if (config.FsCreators == null)
            throw new ArgumentException("FsCreators must not be null");

        if (config.StorageDeviceManagerFactory == null)
            throw new ArgumentException("StorageDeviceManagerFactory must not be null");

        server.SetDebugFlagEnabled(false);
        server.Storage.InitializeStorageDeviceManagerFactory(config.StorageDeviceManagerFactory);

        FileSystemProxyConfiguration fspConfig = InitializeFileSystemProxy(server, config);

        using SharedRef<IFileSystemProxy> fileSystemProxy = server.Impl.GetFileSystemProxyServiceObject();
        ulong processId = client.Os.GetCurrentProcessId().Value;
        fileSystemProxy.Get.SetCurrentProcess(processId).IgnoreResult();

        client.Fs.Impl.InitializeDfcFileSystemProxyServiceObject(ref fileSystemProxy.Ref);

        InitializeFileSystemProxyServer(client, server);

        var saveService = new SaveDataFileSystemService(fspConfig.SaveDataFileSystemService, processId);

        saveService.CleanUpTemporaryStorage().IgnoreResult();
        saveService.CleanUpSaveData().IgnoreResult();
        saveService.CompleteSaveDataExtension().IgnoreResult();
        SaveDataFileSystemService.FixSaveData().IgnoreResult();
        saveService.RecoverMultiCommit().IgnoreResult();

        config.StorageDeviceManagerFactory.SetReady(StorageDevicePortId.SdCard, null);
        config.StorageDeviceManagerFactory.SetReady(StorageDevicePortId.GameCard, null);

        // NS usually takes care of this
        if (client.Fs.IsSdCardInserted())
            client.Fs.SetSdCardAccessibility(true);
    }

    private static FileSystemProxyConfiguration InitializeFileSystemProxy(FileSystemServer server,
        FileSystemServerConfig config)
    {
        var bufferManager = new FileSystemBufferManager();
        Memory<byte> heapBuffer = GC.AllocateArray<byte>(BufferManagerHeapSize, true);
        bufferManager.Initialize(BufferManagerCacheSize, heapBuffer, BufferManagerBlockSize);

        // Todo: Assign based on the value of "IsDevelopment"
        var debugConfigurationServiceConfig = new DebugConfigurationServiceImpl.Configuration
        {
            IsDisabled = false
        };
        var debugConfigurationService = new DebugConfigurationServiceImpl(in debugConfigurationServiceConfig);

        var saveDataIndexerManager = new SaveDataIndexerManager(server.Hos.Fs, Fs.SaveData.SaveIndexerId,
            new ArrayPoolMemoryResource(), new SdHandleManager(server), false);

        var programRegistryConfig = new ProgramRegistryServiceImpl.Configuration
        {
            FsServer = server
        };

        var programRegistryService = new ProgramRegistryServiceImpl(in programRegistryConfig);

        ProgramRegistryImpl.Initialize(server, programRegistryService);

        var baseStorageConfig = new BaseStorageServiceImpl.Configuration
        {
            BisStorageCreator = config.FsCreators.BuiltInStorageCreator,
            GameCardStorageCreator = config.FsCreators.GameCardStorageCreator,
            FsServer = server
        };
        var baseStorageService = new BaseStorageServiceImpl(in baseStorageConfig);

        var timeService = new TimeServiceImpl(server);

        var baseFsServiceConfig = new BaseFileSystemServiceImpl.Configuration
        {
            BisFileSystemCreator = config.FsCreators.BuiltInStorageFileSystemCreator,
            GameCardFileSystemCreator = config.FsCreators.GameCardFileSystemCreator,
            SdCardFileSystemCreator = config.FsCreators.SdCardFileSystemCreator,
            BisWiperCreator = BisWiper.CreateWiper,
            FsServer = server
        };
        var baseFsService = new BaseFileSystemServiceImpl(in baseFsServiceConfig);

        var accessFailureManagementServiceConfig = new AccessFailureManagementServiceImpl.Configuration
        {
            FsServer = server
        };

        var accessFailureManagementService =
            new AccessFailureManagementServiceImpl(in accessFailureManagementServiceConfig);

        var speedEmulationRange =
            new InternalProgramIdRangeForSpeedEmulation(SpeedEmulationProgramIdMinimum,
                SpeedEmulationProgramIdMaximum);

        var ncaFsServiceConfig = new NcaFileSystemServiceImpl.Configuration
        {
            BaseFsService = baseFsService,
            LocalFsCreator = config.FsCreators.LocalFileSystemCreator,
            TargetManagerFsCreator = config.FsCreators.TargetManagerFileSystemCreator,
            PartitionFsCreator = config.FsCreators.PartitionFileSystemCreator,
            RomFsCreator = config.FsCreators.RomFileSystemCreator,
            StorageOnNcaCreator = config.FsCreators.StorageOnNcaCreator,
            SubDirectoryFsCreator = config.FsCreators.SubDirectoryFileSystemCreator,
            EncryptedFsCreator = config.FsCreators.EncryptedFileSystemCreator,
            ProgramRegistryService = programRegistryService,
            AccessFailureManagementService = accessFailureManagementService,
            SpeedEmulationRange = speedEmulationRange,
            FsServer = server
        };

        var ncaFsService = new NcaFileSystemServiceImpl(in ncaFsServiceConfig, config.ExternalKeySet);

        var saveFsServiceConfig = new SaveDataFileSystemServiceImpl.Configuration
        {
            BaseFsService = baseFsService,
            TimeService = timeService,
            LocalFsCreator = config.FsCreators.LocalFileSystemCreator,
            TargetManagerFsCreator = config.FsCreators.TargetManagerFileSystemCreator,
            SaveFsCreator = config.FsCreators.SaveDataFileSystemCreator,
            EncryptedFsCreator = config.FsCreators.EncryptedFileSystemCreator,
            ProgramRegistryService = programRegistryService,
            BufferManager = bufferManager,
            GenerateRandomData = config.RandomGenerator,
            IsPseudoSaveData = () => true,
            SaveDataFileSystemCacheCount = 1,
            SaveIndexerManager = saveDataIndexerManager,
            DebugConfigService = debugConfigurationService,
            FsServer = server
        };

        var saveFsService = new SaveDataFileSystemServiceImpl(in saveFsServiceConfig);

        var statusReportServiceConfig = new StatusReportServiceImpl.Configuration
        {
            NcaFileSystemServiceImpl = ncaFsService,
            SaveDataFileSystemServiceImpl = saveFsService,
            BufferManagerMemoryReport = null,
            ExpHeapMemoryReport = null,
            BufferPoolMemoryReport = null,
            GetPatrolAllocateCounts = null,
            MainThreadStackUsageReporter = new DummyStackUsageReporter(),
            IpcWorkerThreadStackUsageReporter = new DummyStackUsageReporter(),
            PipeLineWorkerThreadStackUsageReporter = new DummyStackUsageReporter(),
            FsServer = server
        };

        var statusReportService = new StatusReportServiceImpl(in statusReportServiceConfig);

        var accessLogServiceConfig = new AccessLogServiceImpl.Configuration
        {
            MinimumProgramIdForSdCardLog = 0x0100000000003000,
            FsServer = server
        };

        var accessLogService = new AccessLogServiceImpl(in accessLogServiceConfig);

        var fspConfig = new FileSystemProxyConfiguration
        {
            FsCreatorInterfaces = config.FsCreators,
            BaseStorageService = baseStorageService,
            BaseFileSystemService = baseFsService,
            NcaFileSystemService = ncaFsService,
            SaveDataFileSystemService = saveFsService,
            AccessFailureManagementService = accessFailureManagementService,
            TimeService = timeService,
            StatusReportService = statusReportService,
            ProgramRegistryService = programRegistryService,
            AccessLogService = accessLogService,
            DebugConfigurationService = debugConfigurationService
        };

        server.InitializeFileSystemProxy(fspConfig);
        return fspConfig;
    }

    private static void InitializeFileSystemProxyServer(HorizonClient client, FileSystemServer server)
    {
        client.Sm.RegisterService(new FileSystemProxyService(server), "fsp-srv").IgnoreResult();
        client.Sm.RegisterService(new FileSystemProxyForLoaderService(server), "fsp-ldr").IgnoreResult();
        client.Sm.RegisterService(new ProgramRegistryService(server), "fsp-pr").IgnoreResult();
    }

    private class FileSystemProxyService : IServiceObject
    {
        private readonly FileSystemServer _server;

        public FileSystemProxyService(FileSystemServer server)
        {
            _server = server;
        }

        public Result GetServiceObject(ref SharedRef<IDisposable> serviceObject)
        {
            using SharedRef<IFileSystemProxy> derivedObject = _server.Impl.GetFileSystemProxyServiceObject();
            serviceObject.SetByMove(ref derivedObject.Ref);
            return Result.Success;
        }

        public void Dispose() { }
    }

    private class FileSystemProxyForLoaderService : IServiceObject
    {
        private readonly FileSystemServer _server;

        public FileSystemProxyForLoaderService(FileSystemServer server)
        {
            _server = server;
        }

        public Result GetServiceObject(ref SharedRef<IDisposable> serviceObject)
        {
            using SharedRef<IFileSystemProxyForLoader> derivedObject = _server.Impl.GetFileSystemProxyForLoaderServiceObject();
            serviceObject.SetByMove(ref derivedObject.Ref);
            return Result.Success;
        }

        public void Dispose() { }
    }

    private class ProgramRegistryService : IServiceObject
    {
        private readonly FileSystemServer _server;

        public ProgramRegistryService(FileSystemServer server)
        {
            _server = server;
        }

        public Result GetServiceObject(ref SharedRef<IDisposable> serviceObject)
        {
            using SharedRef<IProgramRegistry> derivedObject = _server.Impl.GetProgramRegistryServiceObject();
            serviceObject.SetByMove(ref derivedObject.Ref);
            return Result.Success;
        }

        public void Dispose() { }
    }

    private class DummyStackUsageReporter : IStackUsageReporter
    {
        public uint GetStackUsage() => 0;
    }
}

/// <summary>
/// Contains the configuration for creating a new <see cref="FileSystemServer"/>.
/// </summary>
public class FileSystemServerConfig
{
    /// <summary>
    /// The <see cref="FileSystemCreatorInterfaces"/> used for creating filesystems.
    /// </summary>
    public FileSystemCreatorInterfaces FsCreators { get; set; }

    /// <summary>
    /// An <see cref="IStorageDeviceManagerFactory"/> for managing the gamecard and SD card.
    /// </summary>
    public IStorageDeviceManagerFactory StorageDeviceManagerFactory { get; set; }

    /// <summary>
    /// A keyset containing rights IDs and title keys.
    /// If null, an empty set will be created.
    /// </summary>
    public ExternalKeySet ExternalKeySet { get; set; }

    /// <summary>
    /// Used for generating random data for save data.
    /// </summary>
    public RandomDataGenerator RandomGenerator { get; set; }
}