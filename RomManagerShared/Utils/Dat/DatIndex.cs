using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml;
using RomManagerShared.Base;

namespace RomManagerShared.Utils.Dat;

public sealed record DatEntry(string Game, string RomName, long Size, string? Crc, string? Sha1, string? Serial);

/// <summary>
/// Hash lookup over Logiqx XML DATs (No-Intro, Redump). DATs are user-supplied: No-Intro requires a
/// login, so nothing is downloaded automatically.
/// </summary>
public sealed partial class DatIndex
{
    private readonly Dictionary<string, DatEntry> _bySha1 = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Crc, long Size), DatEntry> _byCrc = [];

    public int Count => _byCrc.Count + _bySha1.Count(kv => kv.Value.Crc is null);

    public static DatIndex Load(IEnumerable<string> datFiles)
    {
        var index = new DatIndex();
        foreach (var file in datFiles)
        {
            try { index.Add(file); }
            catch (Exception ex) { FileUtils.Log($"[DAT] {file}: {ex.Message}"); }
        }
        return index;
    }

    public void Add(string datPath)
    {
        using var stream = FileUtils.OpenRead(datPath);
        Add(stream);
    }

    public void Add(Stream stream)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true });
        string? game = null;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.Name is "game" or "machine")
                game = reader.GetAttribute("name");
            else if (reader.Name == "rom" && game is not null)
            {
                long.TryParse(reader.GetAttribute("size"), out var size);
                var entry = new DatEntry(game, reader.GetAttribute("name") ?? "", size,
                    reader.GetAttribute("crc")?.ToLowerInvariant(), reader.GetAttribute("sha1")?.ToLowerInvariant(),
                    reader.GetAttribute("serial"));
                if (entry.Sha1 is not null) _bySha1.TryAdd(entry.Sha1, entry);
                if (entry.Crc is not null) _byCrc.TryAdd((entry.Crc, size), entry);
            }
        }
    }

    public DatEntry? Find(string? sha1, string? crc, long size)
    {
        if (sha1 is not null && _bySha1.TryGetValue(sha1, out var hit)) return hit;
        return crc is not null && _byCrc.TryGetValue((crc.ToLowerInvariant(), size), out hit) ? hit : null;
    }

    // ── No-Intro naming: "Title (Region, Region) (Rev 1) [b]" ──────────────

    [GeneratedRegex(@"\s*(\([^)]*\)|\[[^\]]*\])")]
    private static partial Regex TagRegex();

    public static string CleanTitle(string datName) => TagRegex().Replace(datName, "").Trim();

    public static List<Region> RegionsFromName(string datName)
    {
        var first = Regex.Match(datName, @"\(([^)]*)\)");
        if (!first.Success) return [];
        var regions = new List<Region>();
        foreach (var part in first.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries))
        {
            var region = part switch
            {
                "UK" => Region.UnitedKingdom,
                "Hong Kong" => Region.HongKong,
                "Norway" => Region.Norwegia,
                _ => Enum.TryParse<Region>(part.Replace(" ", ""), true, out var r) ? r : Region.Unknown,
            };
            if (region != Region.Unknown && !regions.Contains(region)) regions.Add(region);
        }
        return regions;
    }

    // ── Catalog: DATs on disk, matched to a console by file name ──────────

    private static readonly ConcurrentDictionary<string, DatIndex> Cache = new();

    /// <summary>Folder holding user DATs (config key <c>GlobalPaths:Dats</c>, default Data/DAT).</summary>
    public static string DatDirectory =>
        Configuration.RomManagerConfiguration.GetPathOrDefault("GlobalPaths:Dats", "Data/DAT");

    /// <summary>Index of every DAT whose file name passes <paramref name="match"/>; cached until the files change.</summary>
    public static DatIndex? ForConsole(Func<string, bool> match)
    {
        var dir = DatDirectory;
        if (!Directory.Exists(dir)) return null;
        var files = Directory.EnumerateFiles(dir, "*.dat", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories))
            .Where(f => match(Path.GetFileName(f)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0) return null;
        var key = string.Join("|", files.Select(f => $"{f}:{File.GetLastWriteTimeUtc(f).Ticks}"));
        return Cache.GetOrAdd(key, _ => Load(files));
    }
}
