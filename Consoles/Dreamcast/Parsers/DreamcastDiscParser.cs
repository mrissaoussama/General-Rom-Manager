using CHDSharp;
using CHDSharp.Models;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.Dreamcast.Parsers;

/// <summary>Reads IP.BIN from the high-density data track (LBA 45000) of GDI sets and GD-ROM CHDs.</summary>
public class DreamcastDiscParser : IRomParser<DreamcastConsole>
{
    private const int HighDensityLba = 45000;
    public List<string> Extensions { get; set; } = ["gdi", "chd"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var (ip, size) = path.EndsWith(".gdi", StringComparison.OrdinalIgnoreCase)
                ? ReadGdi(path)
                : (ReadChd(path), FileUtils.GetFileSize(path));
            if (ip is null)
                return Task.FromResult<List<Rom>>([]);

            var game = new DreamcastGame
            {
                TitleID = ip.ProductNumber,
                ProductCode = ip.ProductNumber,
                Version = ip.Version,
                Publisher = ip.Publisher,
                ReleaseDate = ip.ReleaseDate,
                DiscNumber = ip.DiscNumber,
                Path = path,
                Size = size,
            };
            game.AddTitleName(string.IsNullOrEmpty(ip.Title) ? Path.GetFileNameWithoutExtension(path) : ip.Title);
            foreach (var region in ip.Regions) game.AddRegion(region);
            if (ip.Regions.Count == 0) game.AddRegion(Region.Unknown);
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Dreamcast] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }

    // Lines after the track count: "<track> <lba> <type 4=data/0=audio> <sector size> <file> <offset>"; file may be quoted.
    private static (DreamcastIpBin? Ip, long Size) ReadGdi(string gdiPath)
    {
        var dir = Path.GetDirectoryName(gdiPath)!;
        List<(long Lba, int Type, int SectorSize, string File)> tracks = [];
        foreach (var line in File.ReadLines(gdiPath).Skip(1))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            string file;
            string[] fields;
            var quote = trimmed.IndexOf('"');
            if (quote >= 0)
            {
                var end = trimmed.IndexOf('"', quote + 1);
                if (end < 0) continue;
                file = trimmed[(quote + 1)..end];
                fields = trimmed[..quote].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                fields = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 5) continue;
                file = fields[4];
            }
            if (fields.Length < 4 || !long.TryParse(fields[1], out var lba) || !int.TryParse(fields[2], out var type)
                || !int.TryParse(fields[3], out var sectorSize)) continue;
            tracks.Add((lba, type, sectorSize, Path.Combine(dir, file)));
        }

        var size = tracks.Sum(t => Math.Max(0, FileUtils.GetFileSize(t.File)));
        foreach (var track in tracks.Where(t => t.Type == 4).OrderBy(t => t.Lba >= HighDensityLba ? 0 : 1))
        {
            if (!File.Exists(track.File)) continue;
            using var fs = FileUtils.OpenRead(track.File);
            if (DreamcastIpBin.Read(fs, track.SectorSize, 0) is { } ip)
                return (ip, size);
        }
        return (null, size);
    }

    // CHD CD frames are 2448 bytes; check the first frame of each data track, high-density area first.
    private static DreamcastIpBin? ReadChd(string path)
    {
        var error = ChdFile.Open(path, out var chd, CancellationToken.None);
        if (chd is null)
            throw new InvalidDataException($"CHD open failed: {error}");
        ChdFile.OpenAsStream(chd, true, out var stream);
        using (stream)
        {
            var frameSize = chd.UnitBytes > 0 ? (int)chd.UnitBytes : 2448;
            List<long> frames = [.. (chd.Tracks ?? [])
                .Where(t => t.TrackType != ChdTrackType.Audio)
                .OrderBy(t => t.PhysFrameOfs >= HighDensityLba ? 0 : 1)
                .SelectMany(t => new[] { (long)t.StartFrame + t.PreGap, (long)t.StartFrame })
                .Distinct()];
            if (frames.Count == 0) frames.Add(0);
            foreach (var frame in frames)
                if (DreamcastIpBin.Read(stream, frameSize, frame) is { } ip)
                    return ip;
            return null;
        }
    }
}
