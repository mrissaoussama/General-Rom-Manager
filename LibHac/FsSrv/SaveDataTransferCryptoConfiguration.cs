﻿using System;
using LibHac.Common.FixedArrays;
using LibHac.Fs;
using LibHac.FsSystem;

namespace LibHac.FsSrv;

public class SaveDataTransferCryptoConfiguration
{
    private Array256<byte> _tokenSigningKeyModulus;
    private Array256<byte> _keySeedPackageSigningKeyModulus;
    private Array256<byte> _kekEncryptionKeyModulus;
    private Array256<byte> _keyPackageSigningModulus;

    public Span<byte> TokenSigningKeyModulus => _tokenSigningKeyModulus;
    public Span<byte> KeySeedPackageSigningKeyModulus => _keySeedPackageSigningKeyModulus;
    public Span<byte> KekEncryptionKeyModulus => _kekEncryptionKeyModulus;
    public Span<byte> KeyPackageSigningModulus => _keyPackageSigningModulus;

    public RandomDataGenerator GenerateRandomData { get; set; }
    public SaveTransferCmacGenerator GenerateCmac { get; set; }
    public SaveTransferOpenDecryptor OpenDecryptor { get; set; }
    public SaveTransferOpenEncryptor OpenEncryptor { get; set; }
    public VerifyRsaSignature VerifySignature { get; set; }
    public Action ResetConfiguration { get; set; }
    public SaveTransferAesKeyGenerator GenerateAesKey { get; set; }

    public enum KeyIndex
    {
        SaveDataTransferToken,
        SaveDataTransfer,
        SaveDataTransferKeySeedPackage,
        CloudBackUpInitialData,
        CloudBackUpImportContext,
        CloudBackUpInitialDataMac,
        SaveDataRepairKeyPackage,
        SaveDataRepairInitialDataMacBeforeRepair,
        SaveDataRepairInitialDataMacAfterRepair
    }

    public enum Attributes
    {
        Default = 0
    }

    public interface IEncryptor : IDisposable
    {
        Result Update(Span<byte> destination, ReadOnlySpan<byte> source);
        Result GetMac(out AesMac outMac);
    }

    public interface IDecryptor : IDisposable
    {
        Result Update(Span<byte> destination, ReadOnlySpan<byte> source);
        Result Verify(out bool outIsValid);
    }
}