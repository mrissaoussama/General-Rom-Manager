using System.Buffers.Binary;
using System.Text;

namespace RomManagerShared.ThreeDS;

/// <summary>
/// The unencrypted NCCH header fields of a cartridge image (.3ds/.cci, NCSD) or a single partition
/// (.cxi, NCCH). Readable even when the ExeFS/RomFS are encrypted.
/// </summary>
public sealed record ThreeDSCartridgeHeader(long ProgramId, string ProductCode, bool Encrypted)
{
    private const int MediaUnit = 0x200;
    private const int NoCryptoFlag = 0x4;

    public string TitleId => ProgramId.ToString("X16");

    public static ThreeDSCartridgeHeader? TryRead(Stream stream)
    {
        Span<byte> header = stackalloc byte[0x200];
        if (!ReadAt(stream, 0, header)) return null;

        long ncchOffset = 0;
        if (Magic(header) == "NCSD")
        {
            // Partition table at 0x120: (offset, length) pairs in media units; partition 0 is the game.
            ncchOffset = BinaryPrimitives.ReadUInt32LittleEndian(header[0x120..]) * (long)MediaUnit;
            if (ncchOffset == 0 || !ReadAt(stream, ncchOffset, header)) return null;
        }
        if (Magic(header) != "NCCH") return null;

        var programId = BinaryPrimitives.ReadInt64LittleEndian(header[0x118..]);
        var productCode = Encoding.ASCII.GetString(header.Slice(0x150, 0x10)).TrimEnd('\0', ' ');
        var encrypted = (header[0x188 + 7] & NoCryptoFlag) == 0;
        return new ThreeDSCartridgeHeader(programId, productCode, encrypted);
    }

    private static string Magic(ReadOnlySpan<byte> header) => Encoding.ASCII.GetString(header.Slice(0x100, 4));

    private static bool ReadAt(Stream stream, long offset, Span<byte> buffer)
    {
        if (offset + buffer.Length > stream.Length) return false;
        stream.Position = offset;
        stream.ReadExactly(buffer);
        return true;
    }
}
