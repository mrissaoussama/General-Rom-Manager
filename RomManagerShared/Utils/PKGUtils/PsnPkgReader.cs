using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RomManagerShared.Utils.PKGUtils;

public enum PsnPlatform { Unknown, PS3, PSX, PSP, PSV, PSM }
public enum PsnPkgKind { Unknown, Game, Update, DLC, Theme, Avatar }

public sealed record PsnPkgInfo(
    string ContentId,
    string TitleId,
    PsnPlatform Platform,
    PsnPkgKind Kind,
    int ContentType,
    ParamSfo? Sfo)
{
    public char RegionCode => ContentId.Length > 0 ? ContentId[0] : '?';
}

/// <summary>
/// Reads PS3/PSP/PSX/Vita ("\x7FPKG") package headers and their PARAM.SFO.
/// Port of the relevant parts of PSN_get_pkg_info.py.
/// </summary>
public static class PsnPkgReader
{
    private const uint Pkg3Magic = 0x7F504B47;
    private const uint ExtMagic = 0x7F657874;
    private const int ItemEntrySize = 0x20;

    // Public content keys (psdevwiki "gpkg-key"); 2-4 are derived from the package RIV.
    private static readonly byte[][] ContentKeys =
    [
        Convert.FromBase64String("Lntx18nJoU6jIh8YiCi4+A=="), // PS3
        Convert.FromBase64String("B/LGgpC1DSwzgY1wm2DmKw=="), // PSX/PSP
        Convert.FromBase64String("4xpwyc4d1yvzwGIpY/Lsyw=="), // PSV
        Convert.FromBase64String("QjrKOivVZJ+Whqutb9iAHw=="), // Unknown
        Convert.FromBase64String("rwf9WWUlJ7rxM4lmixfZ6g=="), // PSM
    ];

    public static bool IsPkg3(string path)
    {
        using var fs = FileUtils.OpenRead(path);
        Span<byte> magic = stackalloc byte[4];
        return fs.Read(magic) == 4 && BinaryPrimitives.ReadUInt32BigEndian(magic) == Pkg3Magic;
    }

    public static PsnPkgInfo Read(string path)
    {
        using var fs = FileUtils.OpenRead(path);
        var head = ReadAt(fs, 0, (int)Math.Min(fs.Length, 0x10000));
        if (head.Length < 0xC0 || BinaryPrimitives.ReadUInt32BigEndian(head) != Pkg3Magic)
            throw new InvalidDataException("Not a PS3/PSP/PSV package");

        var revision = BinaryPrimitives.ReadUInt16BigEndian(head.AsSpan(0x04));
        var type = BinaryPrimitives.ReadUInt16BigEndian(head.AsSpan(0x06));
        var metaOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(0x08));
        var metaCount = (int)BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(0x0C));
        var itemCount = (int)BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(0x14));
        var dataOffset = (long)BinaryPrimitives.ReadUInt64BigEndian(head.AsSpan(0x20));
        var contentId = Encoding.ASCII.GetString(head, 0x30, 0x30).TrimEnd('\0');
        var digest = head.AsSpan(0x60, 0x10).ToArray();
        var riv = head.AsSpan(0x70, 0x10).ToArray();
        bool debug = (revision & 0x8000) == 0;

        int keyIndex = type == 1 ? 0 : 1;
        if (type == 2 && head.Length >= 0xE8 && BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(0xC0)) == ExtMagic)
            keyIndex = (int)(BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(0xE4)) & 0xF);

        var meta = ReadMetadata(head, metaOffset, metaCount);
        int contentType = meta.TryGetValue(0x02, out var ct) && ct.Length >= 4 ? (int)BinaryPrimitives.ReadUInt32BigEndian(ct) : 0;
        long itemsOffset = 0;
        if (meta.TryGetValue(0x0D, out var itemsInfo) && itemsInfo.Length >= 8)
            itemsOffset = BinaryPrimitives.ReadUInt32BigEndian(itemsInfo);

        var cipher = new PkgCipher(debug, digest, riv);

        // Item entries, then the names that follow them.
        var entries = cipher.Decrypt(fs, dataOffset, itemsOffset, itemCount * ItemEntrySize, keyIndex);
        var items = new List<(string Name, long Offset, long Size, int KeyIndex)>(itemCount);
        for (int i = 0; i < itemCount; i++)
        {
            var e = entries.AsSpan(i * ItemEntrySize, ItemEntrySize);
            var nameOffset = BinaryPrimitives.ReadUInt32BigEndian(e);
            var nameSize = (int)BinaryPrimitives.ReadUInt32BigEndian(e[0x04..]);
            var offset = (long)BinaryPrimitives.ReadUInt64BigEndian(e[0x08..]);
            var size = (long)BinaryPrimitives.ReadUInt64BigEndian(e[0x10..]);
            var flags = BinaryPrimitives.ReadUInt32BigEndian(e[0x18..]);
            var itemKey = (int)((flags >> 28) & 0x7);
            var name = nameSize > 0
                ? Encoding.UTF8.GetString(cipher.Decrypt(fs, dataOffset, itemsOffset + nameOffset, nameSize, itemKey)).TrimEnd('\0')
                : string.Empty;
            items.Add((name, offset, size, itemKey));
        }

        var sfo = ReadSfo(fs, cipher, dataOffset, items);
        var platform = GetPlatform(contentType);
        var kind = GetKind(contentType, platform, meta.ContainsKey(0x0B), sfo?.GetString("CATEGORY"));
        var titleId = contentId.Length >= 16 ? contentId[7..16] : sfo?.GetString("TITLE_ID") ?? string.Empty;
        return new PsnPkgInfo(contentId, titleId, platform, kind, contentType, sfo);
    }

    private static ParamSfo? ReadSfo(FileStream fs, PkgCipher cipher, long dataOffset,
        List<(string Name, long Offset, long Size, int KeyIndex)> items)
    {
        foreach (var item in items)
        {
            if (item.Size <= 0 || item.Size > 0x100000) continue;
            if (item.Name.Equals("PARAM.SFO", StringComparison.OrdinalIgnoreCase)
                || item.Name.Equals("sce_sys/param.sfo", StringComparison.OrdinalIgnoreCase))
            {
                return TryParseSfo(cipher.Decrypt(fs, dataOffset, item.Offset, (int)item.Size, item.KeyIndex));
            }
        }
        // PSP packages keep PARAM.SFO inside EBOOT.PBP.
        foreach (var item in items)
        {
            if (!item.Name.EndsWith("EBOOT.PBP", StringComparison.OrdinalIgnoreCase) || item.Size < 0x28) continue;
            var pbpHeader = cipher.Decrypt(fs, dataOffset, item.Offset, 0x28, item.KeyIndex);
            if (BinaryPrimitives.ReadUInt32LittleEndian(pbpHeader) != 0x50425000) continue; // "\0PBP"
            var sfoStart = BinaryPrimitives.ReadUInt32LittleEndian(pbpHeader.AsSpan(0x08));
            var sfoEnd = BinaryPrimitives.ReadUInt32LittleEndian(pbpHeader.AsSpan(0x0C));
            if (sfoEnd <= sfoStart || sfoEnd - sfoStart > 0x100000) continue;
            return TryParseSfo(cipher.Decrypt(fs, dataOffset, item.Offset + sfoStart, (int)(sfoEnd - sfoStart), item.KeyIndex));
        }
        return null;
    }

    private static ParamSfo? TryParseSfo(byte[] data)
    {
        try { return ParamSfo.Read(data); }
        catch (InvalidDataException) { return null; }
    }

    private static Dictionary<int, byte[]> ReadMetadata(byte[] head, int offset, int count)
    {
        var meta = new Dictionary<int, byte[]>();
        for (int i = 0; i < count && offset + 8 <= head.Length; i++)
        {
            var id = (int)BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(offset));
            var size = (int)BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(offset + 4));
            offset += 8;
            if (offset + size > head.Length) break;
            meta[id] = head.AsSpan(offset, size).ToArray();
            offset += size;
        }
        return meta;
    }

    private static PsnPlatform GetPlatform(int contentType) => contentType switch
    {
        0x4 or 0xB or 0x5 or 0x9 or 0xD or 0x12 or 0x13 or 0x14 => PsnPlatform.PS3,
        0x1 or 0x6 => PsnPlatform.PSX,
        0x7 or 0xE or 0xF or 0x10 => PsnPlatform.PSP,
        0x15 or 0x16 or 0x1F => PsnPlatform.PSV,
        0x18 or 0x1D => PsnPlatform.PSM,
        _ => PsnPlatform.Unknown,
    };

    private static PsnPkgKind GetKind(int contentType, PsnPlatform platform, bool hasPatchMeta, string? category)
    {
        switch (contentType)
        {
            case 0x4 or 0xB: return hasPatchMeta ? PsnPkgKind.Update : PsnPkgKind.DLC;
            case 0x9 or 0x1F: return PsnPkgKind.Theme;
            case 0xD: return PsnPkgKind.Avatar;
            case 0x16: return PsnPkgKind.DLC;
        }
        return (platform, category) switch
        {
            (PsnPlatform.PSP, "PG") => PsnPkgKind.Update,
            (PsnPlatform.PSP, "MG") => PsnPkgKind.DLC,
            (PsnPlatform.PSV, "gp") => PsnPkgKind.Update,
            (PsnPlatform.Unknown, _) => PsnPkgKind.Unknown,
            _ => PsnPkgKind.Game,
        };
    }

    private static byte[] ReadAt(Stream stream, long offset, int count)
    {
        var buffer = new byte[count];
        stream.Position = offset;
        stream.ReadExactly(buffer);
        return buffer;
    }

    /// <summary>Retail packages use AES-128-CTR; debug packages XOR with SHA-1 of a counter.</summary>
    private sealed class PkgCipher(bool debug, byte[] digest, byte[] riv)
    {
        private readonly Dictionary<int, byte[]> _keys = [];

        public byte[] Decrypt(Stream stream, long dataOffset, long relOffset, int size, int keyIndex)
        {
            var aligned = relOffset & ~0xFL;
            var delta = (int)(relOffset - aligned);
            var alignedSize = (delta + size + 0xF) & ~0xF;
            var data = ReadAt(stream, dataOffset + aligned, alignedSize);
            var blockIndex = aligned / 16;
            if (debug) XorSha1(data, blockIndex);
            else AesCtr(data, GetKey(keyIndex), blockIndex);
            return data.AsSpan(delta, size).ToArray();
        }

        private byte[] GetKey(int index)
        {
            if (_keys.TryGetValue(index, out var key)) return key;
            key = ContentKeys[Math.Clamp(index, 0, ContentKeys.Length - 1)];
            if (index >= 2)
            {
                using var aes = Aes.Create();
                aes.Key = key;
                key = aes.EncryptEcb(riv, PaddingMode.None);
            }
            return _keys[index] = key;
        }

        private void AesCtr(byte[] data, byte[] key, long blockIndex)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            var counter = new BigInteger(riv, isUnsigned: true, isBigEndian: true) + blockIndex;
            var counters = new byte[data.Length];
            for (int i = 0; i < data.Length; i += 16, counter++)
                WriteBigEndian(counter, counters.AsSpan(i, 16));
            var stream = aes.EncryptEcb(counters, PaddingMode.None);
            for (int i = 0; i < data.Length; i++) data[i] ^= stream[i];
        }

        private void XorSha1(byte[] data, long blockIndex)
        {
            var iv = new byte[0x40];
            digest.AsSpan(0, 8).CopyTo(iv);
            digest.AsSpan(0, 8).CopyTo(iv.AsSpan(0x08));
            digest.AsSpan(8, 8).CopyTo(iv.AsSpan(0x10));
            digest.AsSpan(8, 8).CopyTo(iv.AsSpan(0x18));
            var counter = new BigInteger(iv, isUnsigned: true, isBigEndian: true) + blockIndex;
            var block = new byte[0x40];
            for (int i = 0; i < data.Length; i += 16, counter++)
            {
                WriteBigEndian(counter, block);
                var hash = SHA1.HashData(block);
                for (int j = 0; j < 16; j++) data[i + j] ^= hash[j];
            }
        }

        private static void WriteBigEndian(BigInteger value, Span<byte> destination)
        {
            destination.Clear();
            var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            if (bytes.Length > destination.Length) bytes = bytes[^destination.Length..];
            bytes.CopyTo(destination[(destination.Length - bytes.Length)..]);
        }
    }
}
