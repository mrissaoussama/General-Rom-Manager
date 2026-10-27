using System.Buffers.Binary;
using System.Text;
using RomManagerShared.Utils;

namespace RomManagerShared.PS4;

/// <summary>The plaintext metadata entries of a PS4 PKG (game, patch or add-on).</summary>
public sealed record PS4Pkg(string ContentId, ParamSfo? Sfo, byte[]? Icon0, byte[]? Pic0, byte[]? Pic1);

/// <summary>
/// Reads the PS4 PKG header and entry table (big-endian). param.sfo and the images are stored
/// unencrypted, so no keys are needed.
/// </summary>
public static class PS4PkgReader
{
    private const uint Magic = 0x7F434E54; // "\x7FCNT"
    private const uint ParamSfoId = 0x1000, Icon0Id = 0x1200, Pic0Id = 0x1220, Pic1Id = 0x1241;
    private const uint EncryptedFlag = 0x80000000;
    private const int MaxEntrySize = 16 * 1024 * 1024;

    public static bool IsPS4Pkg(Stream stream)
    {
        Span<byte> magic = stackalloc byte[4];
        stream.Position = 0;
        return stream.Length >= 0x1000 && stream.ReadAtLeast(magic, 4, throwOnEndOfStream: false) == 4
            && BinaryPrimitives.ReadUInt32BigEndian(magic) == Magic;
    }

    public static PS4Pkg Read(Stream stream)
    {
        var header = new byte[0x80];
        stream.Position = 0;
        stream.ReadExactly(header);
        if (BinaryPrimitives.ReadUInt32BigEndian(header) != Magic)
            throw new InvalidDataException("Not a PS4 PKG");

        var entryCount = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x10));
        var tableOffset = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x18));
        var contentId = Encoding.ASCII.GetString(header, 0x40, 0x24).TrimEnd('\0');
        if (entryCount > 10_000 || tableOffset + entryCount * 32L > stream.Length)
            throw new InvalidDataException("PS4 PKG entry table is out of range");

        var table = new byte[entryCount * 32];
        stream.Position = tableOffset;
        stream.ReadExactly(table);

        ParamSfo? sfo = null;
        byte[]? icon0 = null, pic0 = null, pic1 = null;
        for (int i = 0; i < entryCount; i++)
        {
            var entry = table.AsSpan(i * 32, 32);
            var id = BinaryPrimitives.ReadUInt32BigEndian(entry);
            if (id is not (ParamSfoId or Icon0Id or Pic0Id or Pic1Id)) continue;
            if ((BinaryPrimitives.ReadUInt32BigEndian(entry[8..]) & EncryptedFlag) != 0) continue;
            var data = ReadEntry(stream, BinaryPrimitives.ReadUInt32BigEndian(entry[16..]), BinaryPrimitives.ReadUInt32BigEndian(entry[20..]));
            if (data is null) continue;
            switch (id)
            {
                case ParamSfoId: sfo = ParamSfo.Read(data); break;
                case Icon0Id: icon0 = data; break;
                case Pic0Id: pic0 = data; break;
                case Pic1Id: pic1 = data; break;
            }
        }
        return new PS4Pkg(contentId, sfo, icon0, pic0, pic1);
    }

    private static byte[]? ReadEntry(Stream stream, uint offset, uint size)
    {
        if (size == 0 || size > MaxEntrySize || offset + (long)size > stream.Length) return null;
        var data = new byte[size];
        stream.Position = offset;
        stream.ReadExactly(data);
        return data;
    }
}
