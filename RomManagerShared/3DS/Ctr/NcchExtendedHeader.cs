﻿using SkyEditor.IO.Binary;
using SkyEditor.Utilities.AsyncFor;
using System.Security.Cryptography;
using System.Text;
namespace DotNet3dsToolkit.Ctr;

public class NcchExtendedHeader
{
    /// <summary>
    /// Size of the data being hashed
    /// </summary>
    public static readonly int ExHeaderDataSize = 0x400;
    {
        var header = new NcchExtendedHeader
        {
            ApplicationTitle = await data.ReadStringAsync(0, 8, Encoding.ASCII),
            Reserved1 = await data.ReadArrayAsync(8, 5),
            Flag = await data.ReadByteAsync(0xD),
            RemasterVersion = await data.ReadInt16Async(0xE),
            TextCodeSetInfo = await CodeSetInfo.Load(data.Slice(0x10, 0xC)),
            StackSize = await data.ReadInt32Async(0x1C),
            ReadOnlyCodeSetInfo = await CodeSetInfo.Load(data.Slice(0x20, 0xC)),
            Reserved2 = await data.ReadInt32Async(0x2C),
            DataCodeSetInfo = await CodeSetInfo.Load(data.Slice(0x30, 0xC)),
            BssSize = await data.ReadInt32Async(0x3C)
        };
        await AsyncFor.For(0, 48 - 1, async i =>
        {
            moduleIds[i] = await data.ReadInt64Async(0x40 + (i * 8));
        });
        header.DependencyModuleIds = moduleIds;
        header.LocalSystemCapabilities = await Arm11LocalSystemCapabilities.Load(data.Slice(0x200, 0x170));
        header.KernelCapabilities = await Arm11KernelCapabilities.Load(data.Slice(0x370, 0x80));
        header.AccessControl = await Arm9AccessControl.Load(data.Slice(0x3F0, 0x10));
        header.AccessDescSignature = await data.ReadArrayAsync(0x400, 0x100);
        header.NcchHdrPublicKey = await data.ReadArrayAsync(0x500, 0x100);
        header.Aci = await data.ReadArrayAsync(0x600, 0x200);
        return header;
    }
    public string ApplicationTitle { get; set; } // Offset: 0x0, Size: 0x8
    public byte[] Reserved1 { get; set; } // Offset: 0x8, Size: 0x5
    /// Bit 0: Compress ExefsCode
    /// Bit 1: SDApplication
    /// </remarks>
    public byte Flag { get; set; } // Offset: 0xD, Size: 0x1
    public short RemasterVersion { get; set; } // Offset: 0xE, Size: 0x2
    public CodeSetInfo TextCodeSetInfo { get; set; } // Offset: 0x10, Size: 0xC
    public int StackSize { get; set; } // Offset: 0x1C, Size: 0x4
    public CodeSetInfo ReadOnlyCodeSetInfo { get; set; } // Offset: 0x20, Size: 0xC
    public int Reserved2 { get; set; } // Offset: 0x2C, Size: 0x4
    public CodeSetInfo DataCodeSetInfo { get; set; } // Offset: 0x30, Size: 0xC
    public int BssSize { get; set; } // Offset: 0x3C, 0x4
    public long[] DependencyModuleIds { get; set; } // Offset: 0x40, 0x180 (48x8)
    public SystemInfo SystemInformation { get; set; } // Offset: 0x1C0, 0x40
    public Arm11LocalSystemCapabilities LocalSystemCapabilities { get; set; } // Offset: 0x200, Size: 0x170
    public Arm11KernelCapabilities KernelCapabilities { get; set; } // Offset: 0x370, Size: 0x80
    public Arm9AccessControl AccessControl { get; set; } // Offset: 0x3F0, Size: 0x10
    /// AccessDesc signature (RSA-2048-SHA256)
    /// </summary>
    public byte[] AccessDescSignature { get; set; } // Offset: 0x400, Size: 0x100
    /// NCCH HDR RSA-2048 public key
    /// </summary>
    public byte[] NcchHdrPublicKey { get; set; } // Offset: 0x500, Size: 0x100
    /// ACI (for limitation of first ACI, which consists of <see cref="LocalSystemCapabilities"/>, <see cref="KernelCapabilities"/>, and <see cref=" AccessControl"/>)
    /// </summary>
    public byte[] Aci { get; set; } // Offset: 0x600, Size: 0x200
    /// Builds a new byte array from the current values
    /// </summary>
    public byte[] ToByteArray()
    {
        var buffer = new byte[0x800];
        Array.Copy(Encoding.ASCII.GetBytes(ApplicationTitle), 0, buffer, 0, 8);
        Array.Copy(Reserved1, 0, buffer, 8, 5);
        buffer[0xD] = Flag;
        BitConverter.GetBytes(RemasterVersion).CopyTo(buffer, 0xE);
        Array.Copy(TextCodeSetInfo.ToByteArray(), 0, buffer, 0x10, 0xC);
        BitConverter.GetBytes(StackSize).CopyTo(buffer, 0x1C);
        Array.Copy(ReadOnlyCodeSetInfo.ToByteArray(), 0, buffer, 0x20, 0xC);
        BitConverter.GetBytes(Reserved2).CopyTo(buffer, 0x2C);
        Array.Copy(DataCodeSetInfo.ToByteArray(), 0, buffer, 0x30, 0xC);
        BitConverter.GetBytes(BssSize).CopyTo(buffer, 0x3C);
        for (int i = 0; i < 48; i++)
        {
            BitConverter.GetBytes(DependencyModuleIds[i]).CopyTo(buffer, 0x40 + (i * 8));
        }
        Array.Copy(SystemInformation.ToByteArray(), 0, buffer, 0x1C0, 0x40);
        Array.Copy(LocalSystemCapabilities.ToByteArray(), 0, buffer, 0x200, 0x170);
        Array.Copy(KernelCapabilities.ToByteArray(), 0, buffer, 0x370, 0x80);
        Array.Copy(AccessControl.ToByteArray(), 0, buffer, 0x3F0, 0x10);
        Array.Copy(NcchHdrPublicKey, 0, buffer, 0x500, 0x100);
        Array.Copy(Aci, 0, buffer, 0x600, 0x200);
        return buffer;
    }
    {
        return sha.ComputeHash(data, 0, ExHeaderDataSize);
    }
    {
        var data = ToByteArray();
        return GetSuperblockHash(sha, data);
    }
    {
        using var sha = SHA256.Create();
        return GetSuperblockHash(sha);
    }
    public class CodeSetInfo
    {
        public static async Task<CodeSetInfo> Load(IReadOnlyBinaryDataAccessor data)
        {
            return new CodeSetInfo
            {
                Address = await data.ReadInt32Async(0),
                PhysicalRegionSize = await data.ReadInt32Async(4),
                Size = await data.ReadInt32Async(8)
            };
        }
        /// Physical region size, in page-multiples
        /// </summary>
        public int PhysicalRegionSize { get; set; } // Offset: 0x4
        /// Size, in bytes
        /// </summary>
        public int Size { get; set; } // Offset: 0x8
        public byte[] ToByteArray()
        {
            var buffer = new byte[12];
            BitConverter.GetBytes(Address).CopyTo(buffer, 0);
            BitConverter.GetBytes(PhysicalRegionSize).CopyTo(buffer, 4);
            BitConverter.GetBytes(Size).CopyTo(buffer, 8);
            return buffer;
        }
    }
    {
        public static async Task<SystemInfo> Load(IReadOnlyBinaryDataAccessor data)
        {
            return new SystemInfo
            {
                SaveDataSize = await data.ReadInt64Async(0),
                JumpId = await data.ReadInt64Async(8),
                Reserved = await data.ReadArrayAsync(0x10, 0x30)
            };
        }
        public long JumpId { get; set; } // Offset: 0x8, Size: 0x8
        public byte[] Reserved { get; set; } // Offset: 0x10, Size: 0x30
        public byte[] ToByteArray()
        {
            var buffer = new byte[0x40];
            BitConverter.GetBytes(SaveDataSize).CopyTo(buffer, 0);
            BitConverter.GetBytes(JumpId).CopyTo(buffer, 8);
            Array.Copy(Reserved, 0, buffer, 0x10, 0x30);
            return buffer;
        }
    }
    {
        public static async Task<Arm11LocalSystemCapabilities> Load(IReadOnlyBinaryDataAccessor data)
        {
            var capabilities = new Arm11LocalSystemCapabilities
            {
                ProgramId = await data.ReadInt64Async(0),
                CoreVersion = await data.ReadInt32Async(0x8),
                Flag1 = await data.ReadByteAsync(0xC),
                Flag2 = await data.ReadByteAsync(0xD),
                Flag0 = await data.ReadByteAsync(0xE),
                Priority = await data.ReadByteAsync(0xF),
                ResourceLimitDescriptors = await data.ReadArrayAsync(0x10, 0x20),
                StorageInformation = await StorageInfo.Load(data.Slice(0x30, 0x20)),
            await AsyncFor.For(0, 32 - 1, async i =>
            {
                accessControl[i] = await data.ReadInt64Async(0x50 + (8 * i));
            });
            capabilities.ServiceAccessControl = accessControl;
            capabilities.ExtendedServiceAccessControl = [await data.ReadInt64Async(0x150), await data.ReadInt64Async(0x158)];
            capabilities.Reserved = await data.ReadArrayAsync(0x160, 0xF);
            capabilities.ResourceLimitCategory = await data.ReadByteAsync(0x16F);
            return capabilities;
        }
        /// The title ID low of the required FIRM
        /// </summary>
        public int CoreVersion { get; set; } // Offset: 0x8, Size: 0x4
        /// Bit 0: Enable L2 Cache (New 3DS Only)
        /// Bit 1: CPU Speed 804 MHz
        /// Bits 2-7: Unused
        /// </summary>
        public byte Flag1 { get; set; } // Offset: 0xC, Size: 0x1
        /// Bits 0-3: New 3DS system mode (refer to <see cref="New3dsSystemMode"/>)
        /// Bits 4-7: Unused
        /// </summary>
        public byte Flag2 { get; set; } // Offset: 0xD, Size: 0x1
        /// Bits 0-1: Ideal processor
        /// Bits 2-3: Affinity mask
        /// Bits 4-7: Old3DS system mode (refer to <see cref="Old3dsSystemMode"/>)
        /// </summary>
        public byte Flag0 { get; set; } // Offset: 0xE, Size: 0x1
        public byte Priority { get; set; } // Offset: 0xF, Size: 0x1
        public byte[] ResourceLimitDescriptors { get; set; } // Offset: 0x10, Size: 0x20
        public StorageInfo StorageInformation { get; set; } // Offset: 0x30, Size: 0x20
        public long[] ServiceAccessControl { get; set; } // Offset: 0x50, Size: 0x100 (32* 8)
        public long[] ExtendedServiceAccessControl { get; set; } // Offset: 0x150, Size: 0x10 (2*8)
        public byte[] Reserved { get; set; } // Offset: 0x160, Size: 0xF
        /// Resource limit category. (0 = APPLICATION, 1 = SYS_APPLET, 2 = LIB_APPLET, 3 = OTHER (sysmodules running under the BASE memregion))
        /// </summary>
        public byte ResourceLimitCategory { get; set; } // Offset: 0x16F, Size: 1
        public byte[] ToByteArray()
        {
            var buffer = new byte[0x170];
            BitConverter.GetBytes(ProgramId).CopyTo(buffer, 0);
            BitConverter.GetBytes(CoreVersion).CopyTo(buffer, 8);
            buffer[0xC] = Flag1;
            buffer[0xD] = Flag2;
            buffer[0xE] = Flag0;
            buffer[0xF] = Priority;
            Array.Copy(ResourceLimitDescriptors, 0, buffer, 0x10, 0x20);
            Array.Copy(StorageInformation.ToByteArray(), 0, buffer, 0x30, 0x20);
            for (int i = 0; i < 32; i++)
            {
                BitConverter.GetBytes(ServiceAccessControl[i]).CopyTo(buffer, 0x50 + (i * 8));
            }
            BitConverter.GetBytes(ExtendedServiceAccessControl[0]).CopyTo(buffer, 0x150);
            BitConverter.GetBytes(ExtendedServiceAccessControl[1]).CopyTo(buffer, 0x158);
            Array.Copy(Reserved, 0, buffer, 0x160, 0xF);
            buffer[0x16F] = ResourceLimitCategory;
            return buffer;
        }
        {
            /// <summary>
            /// 64 MB of usable application memory
            /// </summary>
            Prod = 0,
            /// Undefined/unusable
            /// </summary>
            Undefined = 1,
            /// 96MB of usable application memory
            /// </summary>
            Dev1 = 2,
            /// 80MB of usable application memory
            /// </summary>
            Dev2 = 3,
            /// 72MB of usable application memory
            /// </summary>
            Dev3 = 4,
            /// 32MB of usable application memory
            /// </summary>
            Dev4 = 5,
            /// Unknown, appears same as <see cref="Prod"/>
            /// </summary>
            Undefined2 = 6,
            /// Unknown, appears same as <see cref="Prod"/>
            /// </summary>
            Undefined3 = 7
        }
        {
            /// <summary>
            /// Use Old 3DS mode
            /// </summary>
            Legacy = 0,
            /// 124MB of usable application memory
            /// </summary>
            Prod = 1,
            /// 178MB of usable application memory
            /// </summary>
            Dev1 = 2,
            /// 124MB of usable application memoryu=
            /// </summary>
            Dev2 = 3,
            /// Unknown, appears same as <see cref="Prod"/>
            /// </summary>
            /// 
            Unknown1 = 4,
            /// Unknown, appears same as <see cref="Prod"/>
            /// </summary>
            /// 
            Unknown2 = 5,
            /// Unknown, appears same as <see cref="Prod"/>
            /// </summary>
            Unknown3 = 6,
            /// Unknown, appears same as <see cref="Prod"/>
            /// </summary>
            Unknown7 = 7
        }
        {
            public static async Task<StorageInfo> Load(IReadOnlyBinaryDataAccessor data)
            {
                return new StorageInfo
                {
                    ExtdataId = await data.ReadInt64Async(0),
                    SystemSaveDataIds = await data.ReadInt64Async(0x8),
                    StorageAccessibleUniqueIds = await data.ReadInt64Async(0x10),
                    FilesystemAccessInfo = (FilesystemAccessAttributes)await data.ReadInt64Async(0x18)
                };
            }
            public long SystemSaveDataIds { get; set; } // Offset: 0x8, Size: 8
            public long StorageAccessibleUniqueIds { get; set; } // Offset: 0x10, Size: 8
            public FilesystemAccessAttributes FilesystemAccessInfo { get; set; } // Offset: 0x18, Size: 8
            public byte[] ToByteArray()
            {
                var buffer = new byte[0x20];
                BitConverter.GetBytes(ExtdataId).CopyTo(buffer, 0);
                BitConverter.GetBytes(SystemSaveDataIds).CopyTo(buffer, 8);
                BitConverter.GetBytes(StorageAccessibleUniqueIds).CopyTo(buffer, 0x10);
                BitConverter.GetBytes((long)FilesystemAccessInfo).CopyTo(buffer, 0x18);
                return buffer;
            }
            {
                CategorySystemApplication = 1,
                CateogryHardwareCheck = 2,
                CategoryFilesystemTool = 4,
                Debug = 8,
                TwlCardBackup = 0x10,
                TwlNandData = 0x20,
                Boss = 0x40,
                /// sdmc:/
                /// </summary>
                Sdmc = 0x80,
                Core = 0x100,
                /// nand:/ro/
                /// </summary>
                NandRoRead = 0x200,
                /// nand:/rw/
                /// </summary>
                NandRw = 0x400,
                /// nand:/ro/
                /// </summary>
                NandRoWrite = 0x800,
                Cardboard = 0x2000,
                ImportExportIvs = 0x4000,
                SdmcWriteOnly = 0x8000,
                SwitchCleanup = 0x10000,
                SavedataMove = 0x20000,
                Shop = 0x40000,
                Shell = 0x80000,
                CategoryHomeMenu = 0x100000,
                SeedDb = 0x200000,
                NotUseRomFs = 0x400000,
                UseExtendedSaveDataAccess = 0x800000
            }
        }
    }
    {
        public static async Task<Arm11KernelCapabilities> Load(IReadOnlyBinaryDataAccessor data)
        {
            var capabilities = new Arm11KernelCapabilities();
            var descriptors = new int[28];
            await AsyncFor.For(0, 28 - 1, async i =>
            {
                descriptors[i] = await data.ReadInt32Async(i * 4);
            });
            capabilities.Descriptors = descriptors;
            capabilities.Reserved = await data.ReadArrayAsync(0x70, 0x10);
            return capabilities;
        }
        public byte[] Reserved { get; set; } // Offset: 0x70, Size: 0x10
        public byte[] ToByteArray()
        {
            var buffer = new byte[0x80];
            for (int i = 0; i < 28; i++)
            {
                BitConverter.GetBytes(Descriptors[i]).CopyTo(buffer, i * 4);
            }
            Array.Copy(Reserved, 0, buffer, 0x70, 0x10);
            return buffer;
        }
    }
    {
        public static async Task<Arm9AccessControl> Load(IReadOnlyBinaryDataAccessor data)
        {
            return new Arm9AccessControl
            {
                Descriptors = (Descriptor)await data.ReadInt64Async(0),
                Descriptors2 = await data.ReadArrayAsync(0x8, 0x7),
                Version = await data.ReadByteAsync(0xF)
            };
        }
        public byte[] Descriptors2 { get; set; } // Offset: 0x8, Size: 7
        /// ARM9 Descriptor Version. Originally this value had to be ≥ 2. Starting with 9.3.0-X this value has to be either value 2 or value 3.
        /// </summary>
        public byte Version { get; set; } // Offset: 0xF, Size: 1
        public byte[] ToByteArray()
        {
            var buffer = new byte[0x10];
            BitConverter.GetBytes((long)Descriptors).CopyTo(buffer, 0);
            Array.Copy(Descriptors2, 0, buffer, 8, 7);
            buffer[0xF] = Version;
            return buffer;
        }
        {
            /// <summary>
            /// Mount nand:/
            /// </summary>
            MountNand = 1,
            /// Mount nand:/ro/ (Write Access)
            /// </summary>
            MountNandRoWrite = 2,
            /// Mount twln:/
            /// </summary>
            MountTwln = 4,
            /// Mount wnand:/
            /// </summary>
            MountWnand = 8,
            /// Mount card SPI
            /// </summary>
            MountCardSpi = 16,
            UseSdif3 = 32,
            CreateSeed = 64,
            UseCardSpi = 128,
            /// SD application (Not checked)
            /// </summary>
            SdApplication = 256,
            /// Mount sdmc:/ (Write Access)
            /// </summary>
            MoundSdmcWrite = 512
        }
    }
    #endregion
}