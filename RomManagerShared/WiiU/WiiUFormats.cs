using System.Buffers.Binary;
using System.Text;
using RomManagerShared.Utils;

namespace RomManagerShared.WiiU;

/// <summary>Key-free readers for Wii U title metadata (TMD) and disc images (WUD/WUX).</summary>
public static class WiiUFormats
{
    private const uint WuxMagic0 = 0x30585557; // "WUX0"
    private const uint WuxMagic1 = 0x1099D02E;

    /// <summary>Title ID and version from an unencrypted title.tmd.</summary>
    public static (string TitleId, ushort Version)? ReadTmd(string path)
    {
        using var fs = FileUtils.OpenRead(path);
        Span<byte> buffer = stackalloc byte[0x400];
        var read = fs.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        if (read < 4) return null;

        // Header follows the signature block, whose size depends on the signature type.
        int header = BinaryPrimitives.ReadUInt32BigEndian(buffer) switch
        {
            0x10003 => 0x240, // RSA-4096
            0x10004 => 0x140, // RSA-2048
            0x10005 => 0x80,  // ECDSA
            _ => -1,
        };
        if (header < 0 || header + 0xA0 > read) return null;

        var titleId = Convert.ToHexString(buffer.Slice(header + 0x4C, 8));
        var version = BinaryPrimitives.ReadUInt16BigEndian(buffer[(header + 0x9C)..]);
        return (titleId, version);
    }

    /// <summary>Product code (e.g. "WUP-P-ADVE") from the unencrypted first sector of a WUD or WUX.</summary>
    public static string? ReadDiscProductCode(string path)
    {
        using var fs = FileUtils.OpenRead(path);
        Span<byte> header = stackalloc byte[0x20];
        if (fs.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;

        long sectorZero = 0;
        if (BinaryPrimitives.ReadUInt32LittleEndian(header) == WuxMagic0
            && BinaryPrimitives.ReadUInt32LittleEndian(header[4..]) == WuxMagic1)
        {
            // WUX: header, sector index table, then deduplicated sectors aligned to the sector size.
            var sectorSize = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
            var uncompressedSize = BinaryPrimitives.ReadUInt64LittleEndian(header[0x10..]);
            if (sectorSize == 0) return null;
            var sectorCount = (long)((uncompressedSize + sectorSize - 1) / sectorSize);
            var dataOffset = (0x20 + sectorCount * 4 + sectorSize - 1) / sectorSize * sectorSize;
            Span<byte> index = stackalloc byte[4];
            fs.Position = 0x20;
            fs.ReadExactly(index);
            sectorZero = dataOffset + BinaryPrimitives.ReadUInt32LittleEndian(index) * (long)sectorSize;
        }

        Span<byte> code = stackalloc byte[10];
        fs.Position = sectorZero;
        if (fs.ReadAtLeast(code, code.Length, throwOnEndOfStream: false) < code.Length) return null;
        var text = Encoding.ASCII.GetString(code);
        return text.StartsWith("WUP-") ? text : null;
    }

    /// <summary>Region from the last letter of the game code (WUP-P-ADV<b>E</b>).</summary>
    public static Base.Region RegionFromProductCode(string productCode) => productCode[^1] switch
    {
        'E' => Base.Region.USA,
        'P' => Base.Region.Europe,
        'J' => Base.Region.Japan,
        _ => Base.Region.Unknown,
    };
}
