using System.Globalization;
using System.Text.Json;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerShared.Switch;

/// <summary>Compares installed Switch games and updates against blawar's titledb <c>versions.json</c>.</summary>
public static class SwitchUpdateChecker
{
    /// <summary>
    /// Reads <c>{ "&lt;base title id&gt;": { "&lt;version&gt;": "&lt;release date&gt;", ... } }</c> into the
    /// highest version per base title ID. Values are ignored, so the date format does not matter.
    /// </summary>
    public static Dictionary<string, uint> ParseLatestVersions(Stream json)
    {
        using var doc = JsonDocument.Parse(json);
        Dictionary<string, uint> latest = new(StringComparer.OrdinalIgnoreCase);
        foreach (var title in doc.RootElement.EnumerateObject())
        {
            if (title.Value.ValueKind != JsonValueKind.Object) continue;
            uint max = 0;
            foreach (var version in title.Value.EnumerateObject())
                if (uint.TryParse(version.Name, NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v > max)
                    max = v;
            if (max > 0)
                latest[title.Name] = max;
        }
        return latest;
    }

    public static Dictionary<string, uint> LoadLatestVersions(string path)
    {
        using var stream = FileUtils.OpenRead(path);
        return ParseLatestVersions(stream);
    }

    /// <summary>
    /// Base games whose newest installed version (the game itself or any update file for it) is older
    /// than the newest version in <paramref name="latest"/>. Sorted by title.
    /// </summary>
    public static List<MissingUpdate> FindMissing(IEnumerable<Rom> library, IReadOnlyDictionary<string, uint> latest)
    {
        var roms = library.Where(r => r.TitleID is { Length: 16 }).ToList();
        var installed = roms
            .Where(r => r is SwitchGame or SwitchUpdate)
            .GroupBy(r => BaseTitleId(r.TitleID!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(r => ParseVersion(r.Version)), StringComparer.OrdinalIgnoreCase);

        List<MissingUpdate> missing = [];
        foreach (var game in roms.OfType<SwitchGame>().DistinctBy(g => g.TitleID, StringComparer.OrdinalIgnoreCase))
        {
            var baseId = BaseTitleId(game.TitleID!);
            if (!game.TitleID!.Equals(baseId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!latest.TryGetValue(baseId, out var newest)) continue;
            var have = installed.GetValueOrDefault(baseId);
            if (newest > have)
                missing.Add(new MissingUpdate(game, have.ToString(CultureInfo.InvariantCulture), newest.ToString(CultureInfo.InvariantCulture)));
        }
        return [.. missing.OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Base and update IDs differ only in the last three hex digits (update = base + 0x800).</summary>
    public static string BaseTitleId(string titleId) => titleId[..^3] + "000";

    private static uint ParseVersion(string? version)
        => uint.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
