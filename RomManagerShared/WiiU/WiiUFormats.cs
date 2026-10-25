using System.Buffers.Binary;
using System.Text;
using RomManagerShared.Utils;

namespace RomManagerShared.WiiU;

/// <summary>Key-free readers for Wii U title metadata (TMD) and disc images (WUD/WUX).</summary>
public static class WiiUFormats
{
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
        using var disc = WiiUDiscImage.Open(path);
        if (disc is null) return null;
        Span<byte> code = stackalloc byte[10];
        if (disc.Read(0, code) < code.Length) return null;
        var text = Encoding.ASCII.GetString(code);
        return text.StartsWith("WUP-") ? text : null;
    }

    /// <summary>
    /// Title IDs of the game partitions ("GM&lt;titleid&gt;") from the partition table, which is encrypted
    /// with the per-disc key only (no common key needed).
    /// </summary>
    public static List<string> ReadDiscTitleIds(string path, byte[] discKey)
    {
        const long PartitionTableOffset = 0x18000;
        const uint PartitionTableMagic = 0xCCA6E67B;
        using var disc = WiiUDiscImage.Open(path);
        if (disc is null) return [];

        var encrypted = new byte[0x8000];
        if (disc.Read(PartitionTableOffset, encrypted) < encrypted.Length) return [];
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = discKey;
        var table = aes.DecryptCbc(encrypted, new byte[16], System.Security.Cryptography.PaddingMode.None);
        if (BinaryPrimitives.ReadUInt32BigEndian(table) != PartitionTableMagic) return [];

        var count = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(0x1C)), 0x80);
        var ids = new List<string>();
        for (int i = 0; i < count && 0x800 + (i + 1) * 0x80 <= table.Length; i++)
        {
            var name = Encoding.ASCII.GetString(table, 0x800 + i * 0x80, 0x1F).Split('\0')[0];
            // GM partitions hold titles; skip system titles (00050010...).
            if (name.Length >= 18 && name.StartsWith("GM") && WiiUUtils.GetRomMetadataClass(name[2..18]) is not null)
                ids.Add(name[2..18]);
        }
        return ids;
    }

    /// <summary>
    /// Disc key from a sibling key file (&lt;name&gt;.key, game.key, disc.key; 16 raw bytes or 32 hex chars)
    /// or a [32-hex] tag in the file name.
    /// </summary>
    public static byte[]? FindDiscKey(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        foreach (var candidate in new[] { Path.ChangeExtension(path, ".key"), Path.Combine(dir, "game.key"), Path.Combine(dir, "disc.key") })
        {
            if (!File.Exists(candidate)) continue;
            var bytes = File.ReadAllBytes(candidate);
            if (bytes.Length == 16) return bytes;
            var text = Encoding.ASCII.GetString(bytes).Trim();
            if (IsHexKey(text)) return Convert.FromHexString(text);
        }
        var match = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(path), @"\[([0-9A-Fa-f]{32})\]");
        return match.Success ? Convert.FromHexString(match.Groups[1].Value) : null;
    }

    private static bool IsHexKey(string text) => text.Length == 32 && text.All(char.IsAsciiHexDigit);

    /// <summary>Region from the last letter of the game code (WUP-P-ADV<b>E</b>).</summary>
    public static Base.Region RegionFromProductCode(string productCode) => productCode[^1] switch
    {
        'E' => Base.Region.USA,
        'P' => Base.Region.Europe,
        'J' => Base.Region.Japan,
        _ => Base.Region.Unknown,
    };
}

/// <summary>Random access over a raw WUD or a sector-deduplicated WUX image.</summary>
internal sealed class WiiUDiscImage : IDisposable
{
    private const uint WuxMagic0 = 0x30585557; // "WUX0"
    private const uint WuxMagic1 = 0x1099D02E;
    private readonly FileStream _stream;
    private readonly uint _sectorSize;
    private readonly long _dataOffset;
    private readonly bool _isWux;

    private WiiUDiscImage(FileStream stream, bool isWux, uint sectorSize, long dataOffset)
    {
        _stream = stream;
        _isWux = isWux;
        _sectorSize = sectorSize;
        _dataOffset = dataOffset;
    }

    public static WiiUDiscImage? Open(string path)
    {
        var fs = FileUtils.OpenRead(path);
        Span<byte> header = stackalloc byte[0x20];
        if (fs.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length)
        {
            fs.Dispose();
            return null;
        }
        if (BinaryPrimitives.ReadUInt32LittleEndian(header) != WuxMagic0
            || BinaryPrimitives.ReadUInt32LittleEndian(header[4..]) != WuxMagic1)
            return new WiiUDiscImage(fs, false, 0, 0);

        // WUX: header, u32 sector index per sector, then sectors aligned to the sector size.
        var sectorSize = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        var uncompressedSize = BinaryPrimitives.ReadUInt64LittleEndian(header[0x10..]);
        if (sectorSize == 0)
        {
            fs.Dispose();
            return null;
        }
        var sectorCount = (long)((uncompressedSize + sectorSize - 1) / sectorSize);
        var dataOffset = (0x20 + sectorCount * 4 + sectorSize - 1) / sectorSize * sectorSize;
        return new WiiUDiscImage(fs, true, sectorSize, dataOffset);
    }

    public int Read(long offset, Span<byte> buffer)
    {
        if (!_isWux)
        {
            _stream.Position = offset;
            return _stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        }
        int total = 0;
        Span<byte> index = stackalloc byte[4];
        while (total < buffer.Length)
        {
            var sector = offset / _sectorSize;
            var within = (int)(offset % _sectorSize);
            _stream.Position = 0x20 + sector * 4;
            if (_stream.ReadAtLeast(index, 4, throwOnEndOfStream: false) < 4) break;
            var stored = BinaryPrimitives.ReadUInt32LittleEndian(index);
            var take = (int)Math.Min(buffer.Length - total, _sectorSize - within);
            _stream.Position = _dataOffset + stored * (long)_sectorSize + within;
            var read = _stream.ReadAtLeast(buffer.Slice(total, take), take, throwOnEndOfStream: false);
            total += read;
            offset += read;
            if (read < take) break;
        }
        return total;
    }

    public void Dispose() => _stream.Dispose();
}
