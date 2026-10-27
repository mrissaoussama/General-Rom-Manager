using System.Globalization;
using System.Text;
using RomManagerShared.Base;
namespace RomManagerShared.Dreamcast;

/// <summary>Fields of the 0x100-byte IP.BIN header at the first sector of the GD-ROM high-density data track.</summary>
public sealed record DreamcastIpBin(string Title, string ProductNumber, string Version, string Publisher,
    DateOnly? ReleaseDate, IReadOnlyList<Region> Regions, int DiscNumber)
{
    public const int Size = 0x100;
    private static ReadOnlySpan<byte> HardwareId => "SEGA SEGAKATANA "u8;

    public static DreamcastIpBin? Parse(ReadOnlySpan<byte> header)
    {
        if (header.Length < Size || !header.StartsWith(HardwareId)) return null;
        var text = Encoding.ASCII.GetString(header[..Size]);
        string Field(int offset, int length) => text.Substring(offset, length).TrimEnd(' ', '\0');

        DateOnly? date = DateOnly.TryParseExact(Field(0x50, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
        List<Region> regions = [];
        foreach (var c in Field(0x30, 8))
        {
            var region = c switch { 'J' => Region.Japan, 'U' => Region.USA, 'E' => Region.Europe, _ => Region.Unknown };
            if (region != Region.Unknown && !regions.Contains(region)) regions.Add(region);
        }
        return new DreamcastIpBin(Field(0x80, 0x80).Trim(), Field(0x40, 10).Trim(), Field(0x4A, 6).Trim(),
            Field(0x70, 0x10).Trim(), date, regions, ParseDiscNumber(Field(0x20, 0x10)));
    }

    // Device info looks like "E6A1 GD-ROM1/1".
    private static int ParseDiscNumber(string deviceInfo)
    {
        var at = deviceInfo.IndexOf("GD-ROM", StringComparison.Ordinal);
        if (at < 0) return 1;
        var disc = deviceInfo[(at + 6)..].Split('/')[0];
        return int.TryParse(disc, out var n) && n > 0 ? n : 1;
    }

    /// <summary>User data offsets to try for a sector size (cooked, raw Mode 1/2, Mode 2 without sync, CHD frames).</summary>
    private static int[] OffsetsFor(int sectorSize) => sectorSize switch
    {
        2352 => [16, 24],
        2336 => [8],
        2448 => [16, 0, 24],
        _ => [0],
    };

    /// <summary>Reads IP.BIN from the sector at <paramref name="sector"/>, or null when it isn't there.</summary>
    public static DreamcastIpBin? Read(Stream stream, int sectorSize, long sector)
    {
        Span<byte> header = stackalloc byte[Size];
        foreach (var offset in OffsetsFor(sectorSize))
        {
            var position = sector * sectorSize + offset;
            if (position + Size > stream.Length) continue;
            stream.Position = position;
            if (stream.ReadAtLeast(header, Size, throwOnEndOfStream: false) == Size && Parse(header) is { } ip)
                return ip;
        }
        return null;
    }
}
