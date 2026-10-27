using System.Buffers.Binary;
using RomManagerShared.Utils;

namespace RomManagerShared.ThreeDS;

/// <summary>
/// SMDH large icon at 0x24C0: 48x48 RGB565, stored as 6x6 row-major 8x8 tiles with pixels in Morton (Z) order.
/// </summary>
public static class SmdhIcon
{
    public const int LargeIconOffset = 0x24C0;
    public const int Size = 48;
    private const int ByteLength = Size * Size * 2;

    public static byte[]? DecodeLarge(ReadOnlySpan<byte> smdh)
    {
        if (smdh.Length < LargeIconOffset + ByteLength) return null;
        var src = smdh.Slice(LargeIconOffset, ByteLength);
        if (src.IndexOfAnyExcept((byte)0) < 0) return null; // all-zero = no icon
        var rgba = new byte[Size * Size * 4];
        int i = 0;
        for (int tileY = 0; tileY < Size; tileY += 8)
            for (int tileX = 0; tileX < Size; tileX += 8)
                for (int k = 0; k < 64; k++, i++)
                {
                    // x from even bits of k, y from odd bits
                    int x = tileX + (k & 1 | k >> 1 & 2 | k >> 2 & 4);
                    int y = tileY + (k >> 1 & 1 | k >> 2 & 2 | k >> 3 & 4);
                    var c = BinaryPrimitives.ReadUInt16LittleEndian(src[(i * 2)..]);
                    var o = (y * Size + x) * 4;
                    rgba[o] = (byte)((c >> 11 & 0x1F) * 255 / 31);
                    rgba[o + 1] = (byte)((c >> 5 & 0x3F) * 255 / 63);
                    rgba[o + 2] = (byte)((c & 0x1F) * 255 / 31);
                    rgba[o + 3] = 0xFF;
                }
        return rgba;
    }

    public static byte[]? LargePng(ReadOnlySpan<byte> smdh, int scale = 2)
    {
        var rgba = DecodeLarge(smdh);
        return rgba is null ? null : PngWriter.Encode(PngWriter.Upscale(rgba, Size, Size, scale), Size * scale, Size * scale);
    }
}
