using System.Buffers.Binary;
using System.Text;

namespace RomManagerShared.Utils;

/// <summary>Minimal PARAM.SFO reader (PSP/PS3/Vita/PS4). Values are strings or ints.</summary>
public sealed class ParamSfo
{
    private const uint Magic = 0x46535000; // "\0PSF"
    private const ushort FormatUtf8Special = 0x0004;
    private const ushort FormatUtf8 = 0x0204;
    private const ushort FormatInt32 = 0x0404;

    public IReadOnlyDictionary<string, object> Entries { get; }

    private ParamSfo(Dictionary<string, object> entries) => Entries = entries;

    public string? GetString(string key) => Entries.TryGetValue(key, out var v) ? v as string : null;
    public int? GetInt(string key) => Entries.TryGetValue(key, out var v) && v is int i ? i : null;

    public static ParamSfo Read(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Read(ms.ToArray());
    }

    public static ParamSfo Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < 0x14 || BinaryPrimitives.ReadUInt32LittleEndian(data) != Magic)
            throw new InvalidDataException("Not a PARAM.SFO file");

        var keyTable = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[0x08..]);
        var dataTable = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[0x0C..]);
        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[0x10..]);

        var entries = new Dictionary<string, object>(count, StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            var entry = data.Slice(0x14 + i * 0x10, 0x10);
            var keyOffset = keyTable + BinaryPrimitives.ReadUInt16LittleEndian(entry);
            var format = BinaryPrimitives.ReadUInt16LittleEndian(entry[0x02..]);
            var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[0x04..]);
            var valueOffset = dataTable + (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[0x0C..]);

            var keyEnd = data[keyOffset..].IndexOf((byte)0);
            var key = Encoding.ASCII.GetString(data.Slice(keyOffset, keyEnd));
            var value = data.Slice(valueOffset, length);

            entries[key] = format switch
            {
                FormatInt32 => BinaryPrimitives.ReadInt32LittleEndian(value),
                FormatUtf8 or FormatUtf8Special => Encoding.UTF8.GetString(value).TrimEnd('\0'),
                _ => Convert.ToHexString(value),
            };
        }
        return new ParamSfo(entries);
    }
}
