using System.Globalization;
using System.Text.Json;
using RomManagerShared.Base;

namespace RomManagerShared.Switch;

/// <summary>Finds DLC that titledb lists for the base games in the library but the library does not have.</summary>
public static class SwitchDlcChecker
{
    /// <summary>
    /// The base game a DLC title ID belongs to: add-on content IDs are the application ID + 0x1000 + n,
    /// e.g. 0100A0D004FB1001 → 0100A0D004FB0000. Null for IDs that are not DLC.
    /// </summary>
    public static string? BaseOfDlc(string titleId)
    {
        if (titleId.Length != 16 || !ulong.TryParse(titleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
            return null;
        if ((id & 0xFFF) == 0 || (id & 0x1000) == 0) return null;
        return ((id & ~0x1FFFUL)).ToString("X16", CultureInfo.InvariantCulture);
    }

    /// <summary>(title ID, name) pairs from titledb entries (keyed by title ID or nsuId, each with an "id").</summary>
    public static IEnumerable<(string Id, string? Name)> Catalog(IReadOnlyDictionary<string, JsonElement> titledb)
    {
        foreach (var (key, value) in titledb)
        {
            string? id = key.Length == 16 ? key : null;
            string? name = null;
            if (value.ValueKind == JsonValueKind.Object)
            {
                if (value.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String) id = i.GetString();
                if (value.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String) name = n.GetString();
            }
            if (id is { Length: 16 }) yield return (id.ToUpperInvariant(), name);
        }
    }

    /// <summary>Missing DLC per base game in <paramref name="library"/>, sorted by game title then DLC name.</summary>
    public static List<MissingDlc> FindMissing(IEnumerable<Rom> library, IEnumerable<(string Id, string? Name)> catalog)
    {
        var roms = library.Where(r => r.TitleID is { Length: 16 }).ToList();
        var owned = roms.Select(r => r.TitleID!.ToUpperInvariant()).ToHashSet();
        var games = roms.OfType<SwitchGame>()
            .GroupBy(g => g.TitleID!.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        return [.. catalog
            .DistinctBy(c => c.Id)
            .Select(c => (c.Id, c.Name, Base: BaseOfDlc(c.Id)))
            .Where(c => c.Base is not null && games.ContainsKey(c.Base) && !owned.Contains(c.Id))
            .Select(c => new MissingDlc(games[c.Base!], c.Id, c.Name))
            .OrderBy(m => m.GameTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Name ?? m.TitleId, StringComparer.OrdinalIgnoreCase)];
    }
}

/// <summary>A DLC listed in titledb for a game in the library, not present in the library.</summary>
public sealed record MissingDlc(Rom Game, string TitleId, string? Name)
{
    public string GameTitle => Game.Titles?.FirstOrDefault()?.Value ?? Game.TitleID ?? "";
}
