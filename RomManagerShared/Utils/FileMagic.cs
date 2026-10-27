using System.Buffers.Binary;
using System.Text;

namespace RomManagerShared.Utils;

/// <summary>Format checks on a file's first bytes, shared by parsers (<see cref="Base.IContentSniffer"/>) and the console detector.</summary>
public static class FileMagic
{
    /// <summary>Enough for an ISO 9660 primary volume descriptor (sector 16) and every console header used here.</summary>
    public const int HeaderLength = 0x8800;

    public static byte[] ReadHeader(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.SequentialScan);
        var buffer = new byte[Math.Min(HeaderLength, fs.Length)];
        fs.ReadExactly(buffer);
        return buffer;
    }

    /// <summary>PS3/PSP/Vita PSN package (<c>7F 'PKG'</c>).</summary>
    public static bool IsPkg3(ReadOnlySpan<byte> h) => h.Length >= 4 && h[..4].SequenceEqual("\u007FPKG"u8);

    /// <summary>PS4 package (<c>7F 'CNT'</c>).</summary>
    public static bool IsPs4Pkg(ReadOnlySpan<byte> h) => h.Length >= 4 && h[..4].SequenceEqual("\u007FCNT"u8);

    public static bool IsGameCubeDisc(ReadOnlySpan<byte> h) =>
        h.Length >= 0x20 && BinaryPrimitives.ReadUInt32BigEndian(h[0x1C..]) == 0xC2339F3D;

    public static bool IsWiiDisc(ReadOnlySpan<byte> h) =>
        h.Length >= 0x1C && BinaryPrimitives.ReadUInt32BigEndian(h[0x18..]) == 0x5D1C9EA3;

    /// <summary>System identifier of a 2048-byte-sector ISO 9660 image ("PSP GAME", "PLAYSTATION"), or null.</summary>
    public static string? IsoSystemId(ReadOnlySpan<byte> h)
    {
        const int pvd = 0x8000;
        if (h.Length < pvd + 40 || h[pvd] != 1 || !h.Slice(pvd + 1, 5).SequenceEqual("CD001"u8)) return null;
        return Encoding.ASCII.GetString(h.Slice(pvd + 8, 32)).TrimEnd(' ', '\0');
    }

    public static bool HasExtension(string path, params string[] extensions) =>
        extensions.Any(e => path.EndsWith("." + e, StringComparison.OrdinalIgnoreCase));
}
