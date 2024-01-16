using LibHac.Boot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static LibHac.Common.Keys.KeySet;
using Type = LibHac.Common.Keys.KeyInfo.KeyType;

namespace LibHac.Common.Keys;

internal static partial class DefaultKeySet
{
    /// <summary>
    /// Creates a <see cref="KeySet"/> that contains any keys that have been embedded in the library.
    /// </summary>
    /// <returns>The created <see cref="KeySet"/>.</returns>
    public static KeySet CreateDefaultKeySet()
    {
        var keySet = new KeySet();

        // Fill the key set with any key structs included in the library.
        // This is split into multiple parts so the binary size isn't increased when providing only some keys.
        if (TsecSecrets.Length == Unsafe.SizeOf<TsecSecrets>())
        {
            keySet.KeyStruct.TsecSecrets = SpanHelpers.AsReadOnlyStruct<TsecSecrets>(TsecSecrets);
        }
        
        if (RootKeysDev.Length == Unsafe.SizeOf<RootKeys>())
        {
            keySet.KeyStruct.RootKeysDev = SpanHelpers.AsReadOnlyStruct<RootKeys>(RootKeysDev);
        }

        if (RootKeysProd.Length == Unsafe.SizeOf<RootKeys>())
        {
            keySet.KeyStruct.RootKeysProd = SpanHelpers.AsReadOnlyStruct<RootKeys>(RootKeysProd);
        }

        if (KeySeeds.Length == Unsafe.SizeOf<KeySeeds>())
        {
            keySet.KeyStruct.KeySeeds = SpanHelpers.AsReadOnlyStruct<KeySeeds>(KeySeeds);
        }

        if (StoredKeysDev.Length == Unsafe.SizeOf<StoredKeys>())
        {
            keySet.KeyStruct.StoredKeysDev = SpanHelpers.AsReadOnlyStruct<StoredKeys>(StoredKeysDev);
        }

        if (StoredKeysProd.Length == Unsafe.SizeOf<StoredKeys>())
        {
            keySet.KeyStruct.StoredKeysProd = SpanHelpers.AsReadOnlyStruct<StoredKeys>(StoredKeysProd);
        }

        if (DerivedKeysDev.Length == Unsafe.SizeOf<DerivedKeys>())
        {
            keySet.KeyStruct.DerivedKeysDev = SpanHelpers.AsReadOnlyStruct<DerivedKeys>(DerivedKeysDev);
        }

        if (DerivedKeysProd.Length == Unsafe.SizeOf<DerivedKeys>())
        {
            keySet.KeyStruct.DerivedKeysProd = SpanHelpers.AsReadOnlyStruct<DerivedKeys>(DerivedKeysProd);
        }

        if (DeviceKeys.Length == Unsafe.SizeOf<DeviceKeys>())
        {
            keySet.KeyStruct.DeviceKeys = SpanHelpers.AsReadOnlyStruct<DeviceKeys>(DeviceKeys);
        }

        if (DerivedDeviceKeysDev.Length == Unsafe.SizeOf<DerivedDeviceKeys>())
        {
            keySet.KeyStruct.DerivedDeviceKeysDev = SpanHelpers.AsReadOnlyStruct<DerivedDeviceKeys>(DerivedDeviceKeysDev);
        }

        if (DerivedDeviceKeysProd.Length == Unsafe.SizeOf<DerivedDeviceKeys>())
        {
            keySet.KeyStruct.DerivedDeviceKeysProd = SpanHelpers.AsReadOnlyStruct<DerivedDeviceKeys>(DerivedDeviceKeysProd);
        }

        if (RsaSigningKeysDev.Length == Unsafe.SizeOf<RsaSigningKeys>())
        {
            keySet.KeyStruct.RsaSigningKeysDev = SpanHelpers.AsReadOnlyStruct<RsaSigningKeys>(RsaSigningKeysDev);
        }

        if (RsaSigningKeysProd.Length == Unsafe.SizeOf<RsaSigningKeys>())
        {
            keySet.KeyStruct.RsaSigningKeysProd = SpanHelpers.AsReadOnlyStruct<RsaSigningKeys>(RsaSigningKeysProd);
        }

        if (RsaKeys.Length == Unsafe.SizeOf<RsaKeys>())
        {
            keySet.KeyStruct.RsaKeys = SpanHelpers.AsReadOnlyStruct<RsaKeys>(RsaKeys);
        }

        if (DeviceRsaKeys.Length == Unsafe.SizeOf<DeviceRsaKeys>())
        {
            keySet.KeyStruct.DeviceRsaKeys = SpanHelpers.AsReadOnlyStruct<DeviceRsaKeys>(DeviceRsaKeys);
        }

        return keySet;
    }

    /// <summary>
    /// Creates a <see cref="List{T}"/> of the <see cref="KeyInfo"/> of all keys that are loadable by default.
    /// </summary>
    /// <returns>The created list.</returns>
    public static List<KeyInfo> CreateKeyList()
    {
        // Update this value if more keys are added
        var keys = new List<KeyInfo>(70)
        {
            // Keys with a group value of -1 are keys that will be read but not written.
            // This is for compatibility since some keys had other names in the past.

            // TSEC secrets aren't public yet, so the TSEC root keys will be treated as
            // root keys even though they're derived.

            new(10, Type.DeviceRoot, "secure_boot_key", (set, _) => set.SecureBootKey),
            new(11, Type.DeviceRoot, "tsec_key", (set, _) => set.TsecKey),
            new(12, Type.DeviceDrvd, "device_key", (set, _) => set.DeviceKey),
            new(15, Type.CommonRootSame, "tsec_secret", 0, TsecSecretCount, (set, i) => set.TsecSecrets[i]),
            new(20, Type.CommonRoot, "tsec_root_kek", 0, TsecKeyRevisionCount, (set, i) => set.TsecRootKeks[i]),
            new(22, Type.CommonRoot, "package1_mac_kek", 0, TsecKeyRevisionCount, (set, i) => set.Package1MacKeks[i]),
            new(24, Type.CommonRoot, "package1_kek", 0, TsecKeyRevisionCount, (set, i) => set.Package1Keks[i]),
            new(30, Type.CommonSeed, "tsec_auth_signature", 0, TsecKeyRevisionCount, (set, i) => set.TsecAuthSignatures[i]),
            new(40, Type.CommonRoot, "tsec_root_key", 0, TsecKeyRevisionCount, (set, i) => set.TsecRootKeys[i]),
            new(50, Type.CommonSeed, "keyblob_mac_key_source", (set, _) => set.KeyBlobMacKeySource),
            new(51, Type.CommonSeed, "keyblob_key_source", 0, UsedKeyBlobCount, (set, i) => set.KeyBlobKeySources[i]),
            new(55, Type.DeviceDrvd, "keyblob_key", 0, UsedKeyBlobCount, (set, i) => set.KeyBlobKeys[i]),
            new(60, Type.DeviceDrvd, "keyblob_mac_key", 0, UsedKeyBlobCount, (set, i) => set.KeyBlobMacKeys[i]),
            new(70, Type.DeviceRoot, "encrypted_keyblob", 0, 6, (set, i) =>
            {
                // Todo: Remove local variable after Roslyn issue #67697 is fixed
                ref EncryptedKeyBlob keySetKeyBlob = ref set.EncryptedKeyBlobs[i];
                return keySetKeyBlob.Bytes;
            }),
            new(80, Type.CommonRoot, "keyblob", 0, 6, (set, i) =>
            {
                // Todo: Remove local variable after Roslyn issue #67697 is fixed
                ref KeyBlob keySetKeyBlob = ref set.KeyBlobs[i];
                return keySetKeyBlob.Bytes;
            }),
            new(90, Type.CommonSeed, "master_kek_source", UsedKeyBlobCount, KeyRevisionCount, (set, i) => set.MasterKekSources[i]),
            new(100, Type.CommonRoot, "mariko_bek", (set, _) => set.MarikoBek),
            new(101, Type.CommonRoot, "mariko_kek", (set, _) => set.MarikoKek),
            new(110, Type.CommonRoot, "mariko_aes_class_key", 0, MarikoAesClassKeyCount, (set, i) => set.MarikoAesClassKeys[i]),
            new(120, Type.CommonSeedDiff, "mariko_master_kek_source", 0, KeyRevisionCount, (set, i) => set.MarikoMasterKekSources[i]),
            new(130, Type.CommonDrvd, "master_kek", 0, KeyRevisionCount, (set, i) => set.MasterKeks[i]),
            new(140, Type.CommonSeed, "master_key_source", (set, _) => set.MasterKeySource),
            new(150, Type.CommonDrvd, "master_key", 0, KeyRevisionCount, (set, i) => set.MasterKeys[i]),
            new(160, Type.CommonDrvd, "package1_key", 0, KeyRevisionCount, (set, i) => set.Package1Keys[i]),
            new(170, Type.CommonDrvd, "package1_mac_key", UsedKeyBlobCount, KeyRevisionCount, (set, i) => set.Package1MacKeys[i]),
            new(180, Type.CommonSeed, "package2_key_source", (set, _) => set.Package2KeySource),
            new(190, Type.CommonDrvd, "package2_key", 0, KeyRevisionCount, (set, i) => set.Package2Keys[i]),
            new(200, Type.CommonSeed, "bis_kek_source", (set, _) => set.BisKekSource),
            new(201, Type.CommonSeed, "bis_key_source", 0, 4, (set, i) => set.BisKeySources[i]),
            new(205, Type.DeviceDrvd, "bis_key", 0, 4, (set, i) => set.BisKeys[i]),
            new(210, Type.CommonSeed, "per_console_key_source", (set, _) => set.PerConsoleKeySource),
            new(211, Type.CommonSeed, "retail_specific_aes_key_source", (set, _) => set.RetailSpecificAesKeySource),
            new(212, Type.CommonSeed, "aes_kek_generation_source", (set, _) => set.AesKekGenerationSource),
            new(213, Type.CommonSeed, "aes_key_generation_source", (set, _) => set.AesKeyGenerationSource),
            new(214, Type.CommonSeed, "titlekek_source", (set, _) => set.TitleKekSource),
            new(220, Type.CommonDrvd, "titlekek", 0, KeyRevisionCount, (set, i) => set.TitleKeks[i]),
            new(230, Type.CommonSeed, "header_kek_source", (set, _) => set.HeaderKekSource),
            new(231, Type.CommonSeed, "header_key_source", (set, _) => set.HeaderKeySource),
            new(232, Type.CommonDrvd, "header_key", (set, _) => set.HeaderKey),
            new(240, Type.CommonSeed, "key_area_key_application_source", (set, _) => set.KeyAreaKeyApplicationSource),
            new(241, Type.CommonSeed, "key_area_key_ocean_source", (set, _) => set.KeyAreaKeyOceanSource),
            new(242, Type.CommonSeed, "key_area_key_system_source", (set, _) => set.KeyAreaKeySystemSource),
            new(250, Type.CommonSeed, "save_mac_kek_source", (set, _) => set.DeviceUniqueSaveMacKekSource),
            new(251, Type.CommonSeed, "save_mac_key_source", 0, 2, (set, i) => set.DeviceUniqueSaveMacKeySources[i]),
            new(252, Type.DeviceDrvd, "save_mac_key", 0, 2, (set, i) => set.DeviceUniqueSaveMacKeys[i]),
            new(-01, Type.CommonSeed, "save_mac_key_source", (set, _) => set.DeviceUniqueSaveMacKeySources[0]),
            new(253, Type.CommonSeed, "save_mac_sd_card_kek_source", (set, _) => set.SeedUniqueSaveMacKekSource),
            new(254, Type.CommonSeed, "save_mac_sd_card_key_source", (set, _) => set.SeedUniqueSaveMacKeySource),
            new(255, Type.DeviceDrvd, "save_mac_sd_card_key", (set, _) => set.SeedUniqueSaveMacKey),
            new(260, Type.DeviceRoot, "sd_seed", (set, _) => set.SdCardEncryptionSeed),
            new(261, Type.CommonSeed, "sd_card_kek_source", (set, _) => set.SdCardKekSource),
            new(262, Type.CommonSeed, "sd_card_save_key_source", (set, _) => set.SdCardKeySources[0]),
            new(263, Type.CommonSeed, "sd_card_nca_key_source", (set, _) => set.SdCardKeySources[1]),
            new(264, Type.CommonSeed, "sd_card_custom_storage_key_source", (set, _) => set.SdCardKeySources[2]),
            new(270, Type.CommonRoot, "xci_header_key", (set, _) => set.XciHeaderKey),
            new(271, Type.CommonRoot, "xci_t1_titlekey_kek", 0, 0x10, (set, i) => set.GcTitleKeyKeks[i]),
            new(280, Type.CommonRoot, "eticket_rsa_kek", (set, _) => set.ETicketRsaKek),
            new(281, Type.CommonRoot, "ssl_rsa_kek", (set, _) => set.SslRsaKek),
            new(282, Type.DeviceDrvd, "eticket_rsa_keypair", (set, _) => SpanHelpers.AsByteSpan(ref set.ETicketRsaKey)),
            new(290, Type.CommonDrvd, "key_area_key_application", 0, KeyRevisionCount, (set, i) => set.KeyAreaKeys[i][0]),
            new(300, Type.CommonDrvd, "key_area_key_ocean", 0, KeyRevisionCount, (set, i) => set.KeyAreaKeys[i][1]),
            new(310, Type.CommonDrvd, "key_area_key_system", 0, KeyRevisionCount, (set, i) => set.KeyAreaKeys[i][2])
        };

        return keys;
    }
}