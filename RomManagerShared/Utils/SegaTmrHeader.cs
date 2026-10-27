using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerShared.Utils;

/// <summary>"TMR SEGA" header shared by Master System and Game Gear carts (no title inside).</summary>
public sealed record SegaTmrHeader(int Offset, string ProductCode, int Version, int RegionCode, ushort Checksum)
{
    private static readonly int[] Offsets = [0x7FF0, 0x3FF0, 0x1FF0];

    public string RegionName => RegionCode switch
    {
        3 => "SMS Japan",
        4 => "SMS Export",
        5 => "GG Japan",
        6 => "GG Export",
        7 => "GG International",
        _ => $"Unknown ({RegionCode})",
    };

    public List<Region> Regions => RegionCode switch
    {
        3 or 5 => [Region.Japan],
        7 => [Region.World],
        _ => [],
    };

    /// <summary>Copier dumps carry a 512-byte header before the ROM.</summary>
    public static bool HasCopierHeader(long length) => length % 1024 == 512;

    public static SegaTmrHeader? Read(string path)
    {
        var length = new FileInfo(path).Length;
        var skip = HasCopierHeader(length) ? 512 : 0;
        var data = new byte[Math.Min(length - skip, 0x8000)];
        using (var fs = FileUtils.OpenRead(path))
        {
            fs.Seek(skip, SeekOrigin.Begin);
            fs.ReadExactly(data);
        }
        return Parse(data);
    }

    public static SegaTmrHeader? Parse(ReadOnlySpan<byte> rom)
    {
        foreach (var offset in Offsets)
        {
            if (rom.Length < offset + 16 || !rom.Slice(offset, 8).SequenceEqual("TMR SEGA"u8)) continue;
            var h = rom.Slice(offset, 16);
            // Product code: BCD in 0xC-0xD (little-endian) plus an extra leading digit in the high nibble of 0xE.
            var lead = h[0xE] >> 4;
            var code = $"{(lead > 0 ? lead.ToString() : "")}{h[0xD]:X2}{h[0xC]:X2}";
            return new SegaTmrHeader(offset, code, h[0xE] & 0x0F, h[0xF] >> 4, (ushort)(h[0xA] | h[0xB] << 8));
        }
        return null;
    }
}

/// <summary>Fields read from the TMR SEGA header (Master System and Game Gear).</summary>
public abstract class SegaTmrGame : Game
{
    [UiProperty(DisplayName = "Region Code")]
    public string? RegionCode { get; set; }

    [UiProperty(DisplayName = "Header")]
    public string? HeaderFormat { get; set; }

    public void Apply(SegaTmrHeader? header)
    {
        if (header is null) { HeaderFormat = "None"; return; }
        HeaderFormat = $"TMR SEGA @ 0x{header.Offset:X4}";
        RegionCode = header.RegionName;
        ProductCode = header.ProductCode;
        Version = header.Version.ToString();
        if (header.Regions.Count > 0) Regions = header.Regions;
    }
}
