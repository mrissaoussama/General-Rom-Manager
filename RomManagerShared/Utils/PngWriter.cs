using System.Buffers.Binary;
using System.IO.Compression;

namespace RomManagerShared.Utils;

/// <summary>Minimal RGBA8 PNG encoder for small extracted icons (no System.Drawing dependency).</summary>
public static class PngWriter
{
    public static byte[] Encode(ReadOnlySpan<byte> rgba, int width, int height)
    {
        var raw = new byte[(width * 4 + 1) * height];
        for (int y = 0; y < height; y++)
            rgba.Slice(y * width * 4, width * 4).CopyTo(raw.AsSpan(y * (width * 4 + 1) + 1)); // filter 0

        var zlib = new MemoryStream();
        using (var z = new ZLibStream(zlib, CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw);

        var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // RGBA
        Chunk(png, "IHDR", ihdr);
        Chunk(png, "IDAT", zlib.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void Chunk(Stream s, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf, data.Length);
        s.Write(buf);
        var typeAndData = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) typeAndData[i] = (byte)type[i];
        data.CopyTo(typeAndData.AsSpan(4));
        s.Write(typeAndData);
        BinaryPrimitives.WriteUInt32BigEndian(buf, System.IO.Hashing.Crc32.HashToUInt32(typeAndData));
        s.Write(buf);
    }

    /// <summary>Nearest-neighbour upscale so pixel-art icons stay sharp when the UI stretches them.</summary>
    public static byte[] Upscale(ReadOnlySpan<byte> rgba, int width, int height, int factor)
    {
        var w = width * factor;
        var output = new byte[w * height * factor * 4];
        for (int y = 0; y < height * factor; y++)
            for (int x = 0; x < w; x++)
                rgba.Slice(((y / factor) * width + x / factor) * 4, 4).CopyTo(output.AsSpan((y * w + x) * 4));
        return output;
    }
}
