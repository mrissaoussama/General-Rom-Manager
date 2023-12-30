﻿// ReSharper disable UnusedMember.Local
using System;
using LibHac.Common;
using LibHac.Common.FixedArrays;
using LibHac.Crypto;
using LibHac.Fs;
using LibHac.FsSrv;

namespace LibHac.FsSystem;

/// <summary>
/// Contains the configuration used for decrypting NCAs.
/// </summary>
/// <remarks>Based on nnSdk 14.3.0 (FS 14.1.0)</remarks>
public struct NcaCryptoConfiguration
{
    public const int Rsa2048KeyModulusSize = Rsa.ModulusSize2048Pss;
    public const int Rsa2048KeyPublicExponentSize = Rsa.MaximumExponentSize2048Pss;
    public const int Rsa2048KeyPrivateExponentSize = Rsa2048KeyModulusSize;

    public const int Aes128KeySize = Aes.KeySize128;

    public const int Header1SignatureKeyGenerationMax = 1;

    public const int KeyAreaEncryptionKeyIndexCount = 3;
    public const int HeaderEncryptionKeyCount = 2;

    public const byte KeyAreaEncryptionKeyIndexZeroKey = 0xFF;

    public const int KeyGenerationMax = 32;
    public const int KeyAreaEncryptionKeyCount = KeyAreaEncryptionKeyIndexCount * KeyGenerationMax;

    public Array2<Array256<byte>> Header1SignKeyModuli;
    public Array3<byte> Header1SignKeyPublicExponent;
    public Array3<Array16<byte>> KeyAreaEncryptionKeySources;
    public Array16<byte> HeaderEncryptionKeySource;
    public Array2<Array16<byte>> HeaderEncryptedEncryptionKeys;
    public GenerateKeyFunction GenerateKey;
    public CryptAesXtsFunction EncryptAesXtsForExternalKey;
    public CryptAesXtsFunction DecryptAesXtsForExternalKey;
    public DecryptAesCtrFunction DecryptAesCtr;
    public DecryptAesCtrFunction DecryptAesCtrForExternalKey;
    public VerifySign1Function VerifySign1;
    public bool IsDev;
    public bool IsAvailableSwKey;
}

public struct NcaCompressionConfiguration
{
    public GetDecompressorFunction GetDecompressorFunc;
}

public static class NcaKeyFunctions
{
    public static bool IsInvalidKeyTypeValue(int keyType)
    {
        return keyType < 0;
    }

    public static int GetKeyTypeValue(byte keyIndex, byte keyGeneration)
    {
        if (keyIndex == NcaCryptoConfiguration.KeyAreaEncryptionKeyIndexZeroKey)
        {
            return (int)KeyType.ZeroKey;
        }

        if (keyIndex < NcaCryptoConfiguration.KeyAreaEncryptionKeyIndexCount)
        {
            return NcaCryptoConfiguration.KeyAreaEncryptionKeyIndexCount * keyGeneration + keyIndex;
        }

        return (int)KeyType.InvalidKey;
    }
}

public enum KeyType
{
    ZeroKey = -2,
    InvalidKey = -1,
    NcaHeaderKey1 = NcaCryptoConfiguration.KeyAreaEncryptionKeyCount + 0,
    NcaHeaderKey2 = NcaCryptoConfiguration.KeyAreaEncryptionKeyCount + 1,
    NcaExternalKey = NcaCryptoConfiguration.KeyAreaEncryptionKeyCount + 2,
    SaveDataDeviceUniqueMac = NcaCryptoConfiguration.KeyAreaEncryptionKeyCount + 3,
    SaveDataSeedUniqueMac = NcaCryptoConfiguration.KeyAreaEncryptionKeyCount + 4,
    SaveDataTransferMac = NcaCryptoConfiguration.KeyAreaEncryptionKeyCount + 5
}

public class NcaFileSystemDriver : IDisposable
{
    [NonCopyableDisposable]
    public struct StorageContext : IDisposable
    {
        public bool OpenRawStorage;
        public SharedRef<IStorage> BodySubStorage;
        public SharedRef<SparseStorage> CurrentSparseStorage;
        public SharedRef<IStorage> SparseStorageMetaStorage;
        public SharedRef<SparseStorage> OriginalSparseStorage;
        // Todo: externalCurrentSparseStorage, externalOriginalSparseStorage
        public SharedRef<IStorage> AesCtrExStorageMetaStorage;
        public SharedRef<IStorage> AesCtrExStorageDataStorage;
        public SharedRef<AesCtrCounterExtendedStorage> AesCtrExStorage;
        public SharedRef<IStorage> IndirectStorageMetaStorage;
        public SharedRef<IndirectStorage> IndirectStorage;
        public SharedRef<IStorage> FsDataStorage;
        public SharedRef<IStorage> CompressedStorageMetaStorage;
        public SharedRef<CompressedStorage> CompressedStorage;
        public SharedRef<IStorage> PatchLayerInfoStorage;
        public SharedRef<IStorage> SparseLayerInfoStorage;

        public void Dispose()
        {
            BodySubStorage.Destroy();
            CurrentSparseStorage.Destroy();
            SparseStorageMetaStorage.Destroy();
            OriginalSparseStorage.Destroy();
            AesCtrExStorageMetaStorage.Destroy();
            AesCtrExStorageDataStorage.Destroy();
            AesCtrExStorage.Destroy();
            IndirectStorageMetaStorage.Destroy();
            IndirectStorage.Destroy();
            FsDataStorage.Destroy();
            CompressedStorageMetaStorage.Destroy();
            CompressedStorage.Destroy();
            PatchLayerInfoStorage.Destroy();
            SparseLayerInfoStorage.Destroy();
        }
    }

    private enum AlignmentStorageRequirement
    {
        CacheBlockSize = 0,
        None = 1
    }

    public NcaFileSystemDriver(ref SharedRef<NcaReader> ncaReader, MemoryResource allocator,
        IBufferManager bufferManager, IHash256GeneratorFactorySelector hashGeneratorFactorySelector)
    {
        throw new NotImplementedException();
    }

    public NcaFileSystemDriver(ref SharedRef<NcaReader> originalNcaReader, ref SharedRef<NcaReader> currentNcaReader,
        MemoryResource allocator, IBufferManager bufferManager,
        IHash256GeneratorFactorySelector hashGeneratorFactorySelector)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    private bool IsUsingHwAesCtrForSpeedEmulation(FileSystemServer fs)
    {
        throw new NotImplementedException();
    }

    private long GetFsOffset(NcaReader reader, int index)
    {
        return (long)reader.GetFsOffset(index);
    }

    private long GetFsEndOffset(NcaReader reader, int index)
    {
        return (long)reader.GetFsEndOffset(index);
    }

    public Result OpenStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IAsynchronousAccessSplitter> outStorageAccessSplitter, out NcaFsHeaderReader outHeaderReader,
        int fsIndex)
    {
        throw new NotImplementedException();
    }

    private Result OpenStorageImpl(ref SharedRef<IStorage> outStorage, out NcaFsHeaderReader outHeaderReader,
        int fsIndex, ref StorageContext storageContext)
    {
        throw new NotImplementedException();
    }

    private Result OpenIndirectableStorageAsOriginal(ref SharedRef<IStorage> outStorage,
        in NcaFsHeaderReader headerReader, ref StorageContext storageContext)
    {
        throw new NotImplementedException();
    }

    private Result CreateBodySubStorage(ref SharedRef<IStorage> outStorage, long offset, long size)
    {
        throw new NotImplementedException();
    }

    private Result CreateAesCtrStorage(ref SharedRef<IStorage> outStorage, ref SharedRef<IStorage> baseStorage,
        long offset, in NcaAesCtrUpperIv upperIv, AlignmentStorageRequirement alignmentRequirement)
    {
        throw new NotImplementedException();
    }

    private Result CreateAesXtsStorage(ref SharedRef<IStorage> outStorage, ref SharedRef<IStorage> baseStorage,
        long offset)
    {
        throw new NotImplementedException();
    }

    private Result CreateSparseStorageMetaStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> baseStorage, long offset, in NcaAesCtrUpperIv upperIv, in NcaSparseInfo sparseInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateSparseStorageMetaStorageWithVerification(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> outLayerInfoStorage, ref SharedRef<IStorage> baseStorage, long offset,
        in NcaAesCtrUpperIv upperIv, in NcaSparseInfo sparseInfo, in NcaMetaDataHashDataInfo metaDataHashDataInfo,
        IHash256GeneratorFactory hashGeneratorFactory)
    {
        throw new NotImplementedException();
    }

    private Result CreateSparseStorageCore(ref SharedRef<SparseStorage> outStorage, ref SharedRef<IStorage> baseStorage,
        long baseStorageSize, ref SharedRef<IStorage> sparseStorageMetaStorage, in NcaSparseInfo sparseInfo,
        bool hasExternalInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateSparseStorage(ref SharedRef<IStorage> outStorage, out long outFsDataOffset,
        ref SharedRef<SparseStorage> outSparseStorage, ref SharedRef<IStorage> outSparseStorageMetaStorage, int index,
        in NcaAesCtrUpperIv upperIv, in NcaSparseInfo sparseInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateSparseStorageWithVerification(ref SharedRef<IStorage> outStorage, out long outFsDataOffset,
        out SharedRef<SparseStorage> outSparseStorage, ref SharedRef<IStorage> outSparseStorageMetaStorage,
        ref SharedRef<IStorage> outLayerInfoStorage, int index, in NcaAesCtrUpperIv upperIv,
        in NcaSparseInfo sparseInfo, in NcaMetaDataHashDataInfo metaDataHashDataInfo,
        NcaFsHeader.MetaDataHashType metaDataHashType)
    {
        throw new NotImplementedException();
    }

    private Result CreatePatchMetaStorage(ref SharedRef<IStorage> outAesCtrExMetaStorage,
        ref SharedRef<IStorage> outIndirectMetaStorage, ref SharedRef<IStorage> outLayerInfoStorage,
        ref SharedRef<IStorage> baseStorage, long offset, in NcaAesCtrUpperIv upperIv, in NcaPatchInfo patchInfo,
        in NcaMetaDataHashDataInfo metaDataHashDataInfo, IHash256GeneratorFactory hashGeneratorFactory)
    {
        throw new NotImplementedException();
    }

    private Result CreateAesCtrExStorageMetaStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> baseStorage, long offset, NcaFsHeader.EncryptionType encryptionType,
        in NcaAesCtrUpperIv upperIv, in NcaPatchInfo patchInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateAesCtrExStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<AesCtrCounterExtendedStorage> outAesCtrExStorage, ref SharedRef<IStorage> baseStorage,
        ref SharedRef<IStorage> aesCtrExMetaStorage, long counterOffset, in NcaAesCtrUpperIv upperIv,
        in NcaPatchInfo patchInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateIndirectStorageMetaStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> baseStorage, in NcaPatchInfo patchInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateIndirectStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IndirectStorage> outIndirectStorage, ref SharedRef<IStorage> baseStorage,
        ref SharedRef<IStorage> originalDataStorage, ref SharedRef<IStorage> indirectStorageMetaStorage,
        in NcaPatchInfo patchInfo)
    {
        throw new NotImplementedException();
    }

    private Result CreateSha256Storage(ref SharedRef<IStorage> outStorage, ref SharedRef<IStorage> baseStorage,
        in NcaFsHeader.HashData.HierarchicalSha256Data sha256Data, IHash256GeneratorFactory hashGeneratorFactory)
    {
        throw new NotImplementedException();
    }

    private Result CreateIntegrityVerificationStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> baseStorage, in NcaFsHeader.HashData.IntegrityMetaInfo metaInfo,
        IHash256GeneratorFactory hashGeneratorFactory)
    {
        throw new NotImplementedException();
    }

    private Result CreateIntegrityVerificationStorageImpl(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> baseStorage, in NcaFsHeader.HashData.IntegrityMetaInfo metaInfo, long layerInfoOffset,
        int maxDataCacheEntries, int maxHashCacheEntries, sbyte bufferLevel,
        IHash256GeneratorFactory hashGeneratorFactory)
    {
        throw new NotImplementedException();
    }

    private Result CreateIntegrityVerificationStorageForMeta(ref SharedRef<IStorage> outStorage,
        ref SharedRef<IStorage> outLayerInfoStorage, ref SharedRef<IStorage> baseStorage, long offset,
        in NcaMetaDataHashDataInfo metaDataHashDataInfo, IHash256GeneratorFactory hashGeneratorFactory)
    {
        throw new NotImplementedException();
    }

    public static Result CreateCompressedStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<CompressedStorage> outCompressedStorage, ref SharedRef<IStorage> outMetaStorage,
        ref SharedRef<IStorage> baseStorage, in NcaCompressionInfo compressionInfo,
        GetDecompressorFunction getDecompressor, MemoryResource allocator, IBufferManager bufferManager)
    {
        throw new NotImplementedException();
    }

    public Result CreateCompressedStorage(ref SharedRef<IStorage> outStorage,
        ref SharedRef<CompressedStorage> outCompressedStorage, ref SharedRef<IStorage> outMetaStorage,
        ref SharedRef<IStorage> baseStorage, in NcaCompressionInfo compressionInfo)
    {
        throw new NotImplementedException();
    }

    public Result CreateRegionSwitchStorage(ref SharedRef<IStorage> outStorage, NcaFsHeaderReader headerReader,
        ref SharedRef<IStorage> insideRegionStorage, ref SharedRef<IStorage> outsideRegionStorage)
    {
        throw new NotImplementedException();
    }
}