﻿using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using System.Security.Cryptography;
using System.Text;
namespace DotNet3dsToolkit.Ctr;

public class NcchPartition : IDisposable
{
    private const int MediaUnitSize = 0x200;
    {
        try
        {
            return file.Length >= 0x104 && await file.ReadStringAsync(0x100, 4, Encoding.ASCII) == "NCCH";
        catch (Exception)
        {
            return false;
        }
    }
    {
        NcchHeader? header = null;
        if (data.Length > 0)
        {
            header = new NcchHeader(await data.ReadArrayAsync(0, 0x200));
        }
        await partition.Initialize(data);
        return partition;
    }
    /// Builds a new NCCH partition from the given directory
    /// </summary>
    /// <param name="fileSystem">File system from which to load the files</param>
    /// <returns>A newly built NCCH partition</returns>
    public static async Task<NcchPartition> Build(string headerFilename, string exHeaderFilename, string? exeFsDirectory, string? romFsDiretory, string? plainRegionFilename, string? logoFilename, IFileSystem fileSystem, ProcessingProgressedToken? progressToken = null)
    {
        ProcessingProgressedToken? exefsToken = null;
        ProcessingProgressedToken? romfsToken = null;
        void ReportProgress()
        {
            if (progressToken != null)
            {
                progressToken.TotalFileCount = (exefsToken?.TotalFileCount + romfsToken?.TotalFileCount).GetValueOrDefault();
                progressToken.ProcessedFileCount = (exefsToken?.ProcessedFileCount + romfsToken?.ProcessedFileCount).GetValueOrDefault();
            }
        };
        if (!string.IsNullOrEmpty(exeFsDirectory))
        {
            if (progressToken != null)
            {
                exefsToken = new ProcessingProgressedToken();
                exefsToken.FileCountChanged += (sender, e) => ReportProgress();
            }
            exeFsTask = Task.Run<ExeFs?>(async () => await ExeFs.Build(exeFsDirectory, fileSystem, exefsToken).ConfigureAwait(false));
        }
        else
        {
            exeFsTask = Task.FromResult<ExeFs?>(null);
        }
        if (!string.IsNullOrEmpty(exHeaderFilename))
        {
            using var exHeaderData = new BinaryFile(fileSystem.ReadAllBytes(exHeaderFilename));
            exHeader = await NcchExtendedHeader.Load(exHeaderData);
        }
        if (!string.IsNullOrEmpty(plainRegionFilename))
        {
            plainRegion = fileSystem.ReadAllText(plainRegionFilename);
        }
        if (!string.IsNullOrEmpty(logoFilename))
        {
            logo = fileSystem.ReadAllBytes(logoFilename);
        }
    }
    {
        Header = header;
    }
    {
        ExeFs = exefs;
        Header = header;
        ExHeader = exheader;
        PlainRegion = plainRegion;
        Logo = logo;
    }
    {
        this.RawData = data;
        if (Header != null && Header.RomFsSize > 0)
        {
            if (Header.ExeFsOffset > 0 && Header.ExeFsSize > 0)
            {
                long offset = (long)Header.ExeFsOffset * MediaUnitSize;
            }
            {
                ExHeader = await NcchExtendedHeader.Load(data.Slice(0x200, Header.ExHeaderSize));
            }
            Logo = await data.ReadArrayAsync(Header.LogoRegionOffset * MediaUnitSize, Header.LogoRegionSize * MediaUnitSize);
        }
    }
    public ExeFs? ExeFs { get; private set; } // Could be null if not applicable
    public NcchExtendedHeader? ExHeader { get; private set; } // Could be null if not applicable
    public string? PlainRegion { get; private set; }
    /// Writes the current state of the NCCH partition to the given binary data accessor
    /// </summary>
    /// <param name="data">Data accessor to receive the binary data</param>
    /// <returns>A long representing the total length of data written</returns>
    public async Task<long> WriteBinary(IWriteOnlyBinaryDataAccessor data)
    {
        // Get the data
        var exheader = ExHeader?.ToByteArray();
        var plainRegionOffset = 0;
        var logoRegionOffset = 0;
        var exeFsOffset = 0;
        var offset = 0x200; // Skip the header, write it last
        if (exheader != null)
        {
            await data.WriteAsync(offset, exheader);
            offset += exheader.Length;
        }
        if (plainRegion != null)
        {
            plainRegionOffset = offset;
            await data.WriteAsync(offset, plainRegion);
            offset += plainRegion.Length;
            await data.WriteAsync(offset, padding);
            offset += padding.Length;
        }
        if (Logo != null)
        {
            logoRegionOffset = offset;
            await data.WriteAsync(offset, Logo);
            offset += Logo.Length;
            await data.WriteAsync(offset, padding);
            offset += padding.Length;
        }
        if (exeFs != null)
        {
            exeFsOffset = offset;
            await data.WriteAsync(offset, exeFs);
            offset += exeFs.Length;
            await data.WriteAsync(offset, padding);
            offset += padding.Length;
        }
        using var sha = SHA256.Create();
        header.Signature = new byte[0x100]; // We lack the 3DS's private key, so leave out the signature
        header.ContentSize = (offset + MediaUnitSize - 1) / MediaUnitSize; // offset/MediaUnitSize, but rounding up
        header.ContentLockSeedHash = 0; // Unknown, left blank by SciresM's 3DS Builder
        header.LogoRegionHash = Logo != null ? sha.ComputeHash(Logo) : (new byte[0x20]);
        {
            header.ExHeaderHash = NcchExtendedHeader.GetSuperblockHash(sha, exheader);
            header.ExHeaderSize = NcchExtendedHeader.ExHeaderDataSize;
        }
        else
        {
            header.ExHeaderHash = new byte[0x20];
            header.ExHeaderSize = 0;
        }
        header.PlainRegionSize = ((plainRegion?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
        header.LogoRegionOffset = (logoRegionOffset + MediaUnitSize - 1) / MediaUnitSize;
        header.LogoRegionSize = ((Logo?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
        header.ExeFsOffset = (exeFsOffset + MediaUnitSize - 1) / MediaUnitSize;
        header.ExeFsSize = ((exeFs?.Length ?? 0) + MediaUnitSize - 1) / MediaUnitSize;
        header.ExeFsHashRegionSize = 1; // Static 0x200 for exefs superblock
        header.ExeFsSuperblockHash = ExeFs?.GetSuperblockHash() ?? new byte[0x20];
        await data.WriteAsync(0, headerData);
    }
    {
    }
    public class NcchHeader
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private NcchHeader()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            {
                throw new ArgumentException(Properties.Resources.NcchHeader_ConstructorDataTooSmall, nameof(header));
            }
            Array.Copy(header, 0, Signature, 0, 0x100);
            ContentSize = BitConverter.ToInt32(header, 0x104);
            PartitionId = BitConverter.ToInt64(header, 0x108);
            MakerCode = BitConverter.ToInt16(header, 0x110);
            Version = BitConverter.ToInt16(header, 0x112);
            ContentLockSeedHash = BitConverter.ToInt32(header, 0x114);
            ProgramId = BitConverter.ToInt64(header, 0x118);
            Array.Copy(header, 0x120, Reserved1, 0, 0x10);
            Array.Copy(header, 0x130, LogoRegionHash, 0, 0x20);
            Array.Copy(header, 0x160, ExHeaderHash, 0, 0x20);
            Reserved2 = BitConverter.ToInt32(header, 0x184);
            Array.Copy(header, 0x188, Flags, 0, 8);
            PlainRegionSize = BitConverter.ToInt32(header, 0x194);
            LogoRegionOffset = BitConverter.ToInt32(header, 0x198);
            LogoRegionSize = BitConverter.ToInt32(header, 0x19C);
            ExeFsOffset = BitConverter.ToInt32(header, 0x1A0);
            ExeFsSize = BitConverter.ToInt32(header, 0x1A4);
            ExeFsHashRegionSize = BitConverter.ToInt32(header, 0x1A8);
            Reserved3 = BitConverter.ToInt32(header, 0x1AC);
            RomFsOffset = BitConverter.ToInt32(header, 0x1B0);
            RomFsSize = BitConverter.ToInt32(header, 0x1B4);
            RomFsHashRegionSize = BitConverter.ToInt32(header, 0x1B8);
            Reserved4 = BitConverter.ToInt32(header, 0x1BC);
            Array.Copy(header, 0x1C0, ExeFsSuperblockHash, 0, 0x20);
            Array.Copy(header, 0x1E0, RomFsSuperblockHash, 0, 0x20);
        }
        /// Creates a new <see cref="NcchHeader"/> based on the given header
        /// </summary>
        /// <remarks>
        /// The following properties need to be updated manually:
        /// - <see cref="Signature" />
        /// - <see cref="ContentSize" />
        /// - <see cref="ContentLockSeedHash" />
        /// - <see cref="LogoRegionHash" />
        /// - <see cref="ExHeaderHash" />
        /// - <see cref="ExHeaderSize" />
        /// - <see cref="PlainRegionOffset" />
        /// - <see cref="PlainRegionSize" />
        /// - <see cref="LogoRegionOffset" />
        /// - <see cref="LogoRegionSize" />
        /// - <see cref="ExeFsOffset" />
        /// - <see cref="ExeFsSize" />
        /// - <see cref="ExeFsHashRegionSize" />
        /// - <see cref="RomFsOffset" />
        /// - <see cref="RomFsSize" />
        /// - <see cref="RomFsHashRegionSize" />
        /// - <see cref="ExeFsSuperblockHash" />
        /// - <see cref="RomFsSuperblockHash" />
        /// </remarks>
        public static NcchHeader Copy(NcchHeader other)
        {
            return other == null
                ? throw new ArgumentNullException(nameof(other))
                : new NcchHeader
                {
                    Magic = "NCCH",
                    PartitionId = other.PartitionId,
                    MakerCode = other.MakerCode,
                    Version = other.Version,
                    ProgramId = other.ProgramId,
                    Reserved1 = other.Reserved1,
                    ProductCode = other.ProductCode,
                    Reserved2 = other.Reserved2,
                    Flags = other.Flags,
                    Reserved3 = other.Reserved3,
                    Reserved4 = other.Reserved4
                };
        {
            var binary = new BinaryFile(new byte[0x200]);
            binary.Write(0, 0x100, Signature);
            binary.WriteString(0x100, Encoding.ASCII, Magic);
            binary.WriteInt32(0x104, ContentSize);
            binary.WriteInt64(0x108, PartitionId);
            binary.WriteInt16(0x110, MakerCode);
            binary.WriteInt16(0x112, Version);
            binary.WriteInt32(0x114, ContentLockSeedHash);
            binary.WriteInt64(0x118, ProgramId);
            binary.Write(0x120, 0x10, Reserved1);
            binary.Write(0x130, 0x20, LogoRegionHash);
            binary.WriteString(0x150, Encoding.ASCII, ProductCode);
            binary.Write(0x160, 0x20, ExHeaderHash);
            binary.WriteInt32(0x180, ExHeaderSize);
            binary.WriteInt32(0x184, Reserved2);
            binary.Write(0x188, 0x8, Flags);
            binary.WriteInt32(0x190, PlainRegionOffset);
            binary.WriteInt32(0x194, PlainRegionSize);
            binary.WriteInt32(0x198, LogoRegionOffset);
            binary.WriteInt32(0x19C, LogoRegionSize);
            binary.WriteInt32(0x1A0, ExeFsOffset);
            binary.WriteInt32(0x1A4, ExeFsSize);
            binary.WriteInt32(0x1A8, ExeFsHashRegionSize);
            binary.WriteInt32(0x1AC, Reserved3);
            binary.WriteInt32(0x1B0, RomFsOffset);
            binary.WriteInt32(0x1B4, RomFsSize);
            binary.WriteInt32(0x1B8, RomFsHashRegionSize);
            binary.WriteInt32(0x1BC, Reserved4);
            binary.Write(0x1C0, 0x20, ExeFsSuperblockHash);
            binary.Write(0x1E0, 0x20, RomFsSuperblockHash);
            return binary;
        }
        /// RSA-2048 signature of the NCCH header, using SHA-256.
        /// </summary>
        public byte[] Signature { get; set; } // Offset: 0x0, size: 0x100
        /// Magic ID, always 'NCCH'
        /// </summary>
        public string Magic { get; set; } // Offset: 0x100, size: : 0x4
        /// Content size, in media units (1 media unit = 0x200 bytes)
        /// </summary>
        public int ContentSize { get; set; } // Offset: 0x104, size: 0x4
        public long PartitionId { get; set; } // Offset: 0x108, size: 0x8
        public short MakerCode { get; set; } // Offset: 0x110, size: 0x2
        public short Version { get; set; } // Offset: 0x112, size: 0x2
        /// When ncchflag[7] = 0x20 starting with FIRM 9.6.0-X, this is compared with the first output u32 from a SHA256 hash.
        /// The data used for that hash is 0x18-bytes: (0x10-long title-unique content lock seed) (programID from NCCH+0x118). 
        /// This hash is only used for verification of the content lock seed, and is not the actual keyY.
        /// </summary>
        public int ContentLockSeedHash { get; set; } // Offset: 0x114, size: 4
        /// The Program ID, also known as the Title ID
        /// </summary>
        public long ProgramId { get; set; } // Offset: 0x118, size: 8
        public byte[] Reserved1 { get; set; } // Offset: 0x120, size: 0x10
        /// Logo Region SHA-256 hash. (For applications built with SDK 5+) (Supported from firmware: 5.0.0-11)
        /// </summary>
        public byte[] LogoRegionHash { get; set; } // Offset: 0x130, size: 0x20
        public string ProductCode { get; set; } // Offset: 0x150, size: 0x10
        /// Extended header SHA-256 hash (SHA256 of 2x Alignment Size, beginning at 0x0 of ExHeader)
        /// </summary>
        public byte[] ExHeaderHash { get; set; } // Offset: 0x160, size: 0x20
        /// Extended header size, in bytes
        /// </summary>
        public int ExHeaderSize { get; set; } // Offset: 0x180, size: 4
        public int Reserved2 { get; set; } // Offset: 0x184, size: 4
        /// 3	Crypto Method: When this is non-zero, a NCCH crypto method using two keyslots is used(see above).
        /// 4	Content Platform: 1 = CTR, 2 = snake (New 3DS).
        /// 5	Content Type Bit-masks: Data = 0x1, Executable = 0x2, SystemUpdate = 0x4, Manual = 0x8, Child = (0x4|0x8), Trial = 0x10. When 'Data' is set, but not 'Executable', NCCH is a CFA.Otherwise when 'Executable' is set, NCCH is a CXI.
        /// 6	Content Unit Size i.e.u32 ContentUnitSize = 0x200 * 2 ^ flags[6];
        /// 7	Bit-masks: FixedCryptoKey = 0x1, NoMountRomFs = 0x2, NoCrypto = 0x4, using a new keyY generator = 0x20(starting with FIRM 9.6.0 - X).
        /// </summary>
        public byte[] Flags { get; set; } // Offset: 0x188, size: 8
        /// Plain region offset, in media units
        /// </summary>
        public int PlainRegionOffset { get; set; } // Offset: 0x190, size: 4
        /// Plain region size, in media units
        /// </summary>
        public int PlainRegionSize { get; set; } // Offset: 0x194, size: 4
        /// Logo Region offset, in media units (For applications built with SDK 5+) (Supported from firmware: 5.0.0-11)
        /// </summary>
        public int LogoRegionOffset { get; set; } // Offset: 0x198, size: 4
        /// Logo Region size, in media units (For applications built with SDK 5+) (Supported from firmware: 5.0.0-11)
        /// </summary>
        public int LogoRegionSize { get; set; } // Offset: 0x19C, size: 4
        /// ExeFS offset, in media units
        /// </summary>
        public int ExeFsOffset { get; set; } // Offset: 0x1A0, size: 4
        /// ExeFS size, in media units
        /// </summary>
        public int ExeFsSize { get; set; } // Offset: 0x1A4, size: 4
        /// ExeFS hash region size, in media units
        /// </summary>
        public int ExeFsHashRegionSize { get; set; } // Offset: 0x1A8, size: 4
        public int Reserved3 { get; set; } // Offset: 0x1AC, size: 4
        /// RomFS offset, in media units
        /// </summary>
        public int RomFsOffset { get; set; } // Offset: 0x1B0, size: 4
        /// RomFS size, in media units
        /// </summary>
        public int RomFsSize { get; set; } // Offset: 0x1B4, size: 4
        /// RomFS hash region size, in media units
        /// </summary>
        public int RomFsHashRegionSize { get; set; } // Offset: 0x1B8, size: 4
        public int Reserved4 { get; set; } // Offset: 0x1BC, size: 4
        /// ExeFS superblock SHA-256 hash - (SHA-256 hash, starting at 0x0 of the ExeFS over the number of media units specified in the ExeFS hash region size)
        /// </summary>
        public byte[] ExeFsSuperblockHash { get; set; } // Offset: 0x1C0, size: 0x20
        /// RomFS superblock SHA-256 hash - (SHA-256 hash, starting at 0x0 of the RomFS over the number of media units specified in the RomFS hash region size)
        /// </summary>
        public byte[] RomFsSuperblockHash { get; set; } // Offset: 0x1E0, size: 0x20
    }
    #endregion
}