using System.Buffers.Binary;
using System.Text;
using RomManagerShared.Utils;

namespace RomManagerShared.DS;

/// <summary>
/// DS icon/title block (offset at header 0x68): 32x32 4bpp icon as 8x8 tiles (+0x20), 16-colour BGR555
/// palette (+0x220, colour 0 transparent), then UTF-16 titles per language (English at +0x340).
/// </summary>
public sealed record DSBanner(string? Title, string? Publisher, byte[]? IconRgba)
{
    public const int IconSize = 32;

    public static DSBanner? Read(Stream stream)
    {
        Span<byte> head = stackalloc byte[4];
        stream.Position = 0x68;
        if (stream.ReadAtLeast(head, 4, throwOnEndOfStream: false) < 4) return null;
        var offset = BinaryPrimitives.ReadUInt32LittleEndian(head);
        if (offset == 0 || offset + 0x840 > stream.Length) return null;

        var block = new byte[0x840];
        stream.Position = offset;
        stream.ReadExactly(block);
        // CRC16 at +2 covers icon, palette and the six original titles; rejects trimmed or garbled banners.
        if (Crc16(block.AsSpan(0x20, 0x820)) != BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(2))) return null;
        var (title, publisher) = ParseTitle(block.AsSpan(0x340, 0x100));
        return new DSBanner(title, publisher, DecodeIcon(block.AsSpan(0x20, 0x200), block.AsSpan(0x220, 0x20)));
    }

    private static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++) crc = (ushort)((crc & 1) != 0 ? crc >> 1 ^ 0xA001 : crc >> 1);
        }
        return crc;
    }

    // "Title\nSubtitle\nPublisher": last line is the publisher when there are two or more.
    private static (string?, string?) ParseTitle(ReadOnlySpan<byte> utf16)
    {
        var text = Encoding.Unicode.GetString(utf16).Split('\0')[0].Replace("\r", "");
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0 || lines.Any(l => l.Any(char.IsControl))) return (null, null);
        return lines.Length == 1 ? (lines[0], null) : (string.Join(" ", lines[..^1]), lines[^1]);
    }

    private static byte[]? DecodeIcon(ReadOnlySpan<byte> tiles, ReadOnlySpan<byte> palette)
    {
        var colors = new uint[16];
        for (int i = 1; i < 16; i++)
        {
            var c = BinaryPrimitives.ReadUInt16LittleEndian(palette[(i * 2)..]);
            int r = (c & 0x1F) * 255 / 31, g = (c >> 5 & 0x1F) * 255 / 31, b = (c >> 10 & 0x1F) * 255 / 31;
            colors[i] = (uint)(r | g << 8 | b << 16 | 0xFF << 24);
        }
        if (colors.Skip(1).All(c => c == colors[1])) return null; // blank icon

        var rgba = new byte[IconSize * IconSize * 4];
        for (int tile = 0; tile < 16; tile++)
            for (int p = 0; p < 64; p++)
            {
                var b = tiles[tile * 32 + p / 2];
                var index = p % 2 == 0 ? b & 0x0F : b >> 4;
                int x = tile % 4 * 8 + p % 8, y = tile / 4 * 8 + p / 8;
                BinaryPrimitives.WriteUInt32LittleEndian(rgba.AsSpan((y * IconSize + x) * 4), colors[index]);
            }
        return rgba;
    }

    public byte[]? IconPng(int scale = 4) => IconRgba is null
        ? null
        : PngWriter.Encode(PngWriter.Upscale(IconRgba, IconSize, IconSize, scale), IconSize * scale, IconSize * scale);
}
