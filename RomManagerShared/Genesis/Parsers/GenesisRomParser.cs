using System.Text;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.Genesis.Parsers;

/// <summary>Mega Drive header at 0x100 (raw, copier-headered or SMD-interleaved dumps).</summary>
public class GenesisRomParser : IRomParser<GenesisConsole>
{
    public List<string> Extensions { get; set; } = ["md", "gen", "smd"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var length = new FileInfo(path).Length;
            var head = new byte[Math.Min(length, GenesisFormat.CopierHeaderSize + GenesisFormat.SmdBlockSize)];
            using (var fs = FileUtils.OpenRead(path))
                fs.ReadExactly(head);

            var layout = GenesisFormat.Detect(head, length);
            var rom = GenesisFormat.Normalize(head, layout);
            var game = new GenesisGame
            {
                Path = path,
                Size = length,
                HeaderFormat = layout switch
                {
                    GenesisLayout.Smd => "SMD (interleaved)",
                    GenesisLayout.CopierHeader => "Copier header",
                    _ => "Raw",
                },
            };

            if (GenesisFormat.HasSegaHeader(rom))
            {
                game.SystemType = Text(rom, 0x100, 16);
                game.Copyright = Text(rom, 0x110, 16);
                game.DomesticName = Text(rom, 0x120, 48);
                var serial = Text(rom, 0x180, 14);
                if (serial.Length > 0) game.ProductCode = serial;
                var regions = ParseRegions(Text(rom, 0x1F0, 3));
                if (regions.Count > 0) game.Regions = regions;
                game.AddTitleName(Text(rom, 0x150, 48));
            }
            else
            {
                game.HeaderFormat += " (no SEGA header)";
            }
            if (game.Titles is not { Count: > 0 })
                game.AddTitleName(Path.GetFileNameWithoutExtension(path));
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[Genesis] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }

    /// <summary>Printable ASCII with runs of spaces collapsed (non-ASCII, e.g. Shift-JIS, is dropped).</summary>
    public static string Text(ReadOnlySpan<byte> data, int offset, int length)
    {
        var sb = new StringBuilder(length);
        foreach (var b in data.Slice(offset, length))
            sb.Append(b is >= 0x20 and < 0x7F ? (char)b : ' ');
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Old style lists J/U/E letters; new style is one hex digit (bit0 Japan, bit1 Asia, bit2 USA, bit3 Europe).</summary>
    public static List<Region> ParseRegions(string code)
    {
        List<Region> regions = [];
        if (code.Length == 0) return regions;
        if (code.All(c => c is 'J' or 'U' or 'E' or ' '))
        {
            if (code.Contains('J')) regions.Add(Region.Japan);
            if (code.Contains('U')) regions.Add(Region.USA);
            if (code.Contains('E')) regions.Add(Region.Europe);
            return regions;
        }
        if (code.Length == 1 && Uri.IsHexDigit(code[0]))
        {
            var bits = Convert.ToInt32(code, 16);
            if ((bits & 1) != 0) regions.Add(Region.Japan);
            if ((bits & 2) != 0) regions.Add(Region.Asia);
            if ((bits & 4) != 0) regions.Add(Region.USA);
            if ((bits & 8) != 0) regions.Add(Region.Europe);
        }
        return regions;
    }
}
