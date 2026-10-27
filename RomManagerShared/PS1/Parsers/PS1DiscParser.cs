using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS2;
using RomManagerShared.Utils;
namespace RomManagerShared.PS1.Parsers;

/// <summary>Reads the serial from SYSTEM.CNF. Cue sheets point at the data track; loose track bins are not indexed.</summary>
public class PS1DiscParser : IRomParser<PS1Console>, IContentSniffer
{
    // A 2352-byte-sector image renamed to .iso has no PVD at 0x8000 and is still tried.
    public bool CanParse(string path, ReadOnlySpan<byte> header) =>
        !FileMagic.HasExtension(path, "iso") || FileMagic.IsoSystemId(header) is not { } id || id.StartsWith("PLAYSTATION", StringComparison.Ordinal);

    public List<string> Extensions { get; set; } = ["cue", "iso", "chd"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var image = path.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ? FirstTrackFile(path) : path;
            if (image is null || !File.Exists(image))
                return Task.FromResult<List<Rom>>([]);

            using var fs = FileUtils.OpenDiscImage(image);
            var serial = new RawCdReader(fs).ReadBootExecutable();
            if (serial is null)
                return Task.FromResult<List<Rom>>([]);

            var game = new PS1Game { TitleID = serial, Path = path, Size = FileUtils.GetFileSize(image) };
            game.AddTitleName(Path.GetFileNameWithoutExtension(path));
            game.AddRegion(PS2Utils.GetRegionFile(serial) ?? Region.Unknown);
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[PS1] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }

    // FILE "Game (Track 01).bin" BINARY
    private static string? FirstTrackFile(string cuePath)
    {
        foreach (var line in File.ReadLines(cuePath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("FILE", StringComparison.OrdinalIgnoreCase)) continue;
            var start = trimmed.IndexOf('"');
            var end = trimmed.LastIndexOf('"');
            if (start < 0 || end <= start) continue;
            return Path.Combine(Path.GetDirectoryName(cuePath)!, trimmed[(start + 1)..end]);
        }
        return null;
    }
}
