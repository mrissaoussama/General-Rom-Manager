using System.Text.RegularExpressions;
using RomManagerShared.Base;

namespace RomManagerShared.Utils.Dat;

/// <summary>One game of a 1G1R selection: the rom to keep and the other versions of the same game.</summary>
public sealed record OneGameOneRomGroup(string Game, Rom Keep, IReadOnlyList<Rom> Others);

/// <summary>
/// "One game, one ROM": groups regional versions and revisions of the same game and keeps the best one
/// by a region priority list. Games are grouped by their DAT parent when a parent/clone DAT is loaded,
/// otherwise by title without tags ("Zelda (USA) (Rev 1)" → "Zelda"). Only base games take part; updates,
/// DLC and roms without a title are left alone.
/// </summary>
public static partial class OneGameOneRom
{
    public static readonly IReadOnlyList<Region> DefaultPriority = [Region.USA, Region.World, Region.Europe, Region.Japan];

    /// <summary>Groups with more than one version; single versions need no choice.</summary>
    public static IReadOnlyList<OneGameOneRomGroup> Select(IEnumerable<Rom> library, IReadOnlyList<Region> priority,
        IEnumerable<DatIndex>? dats = null)
    {
        var indexes = dats?.ToList() ?? [];
        return [.. library
            .Where(r => r is Game && Name(r) is not null)
            .GroupBy(r => GroupKey(r, indexes), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g =>
            {
                var ranked = g.OrderBy(r => Score(r, priority)).ThenBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();
                return new OneGameOneRomGroup(g.Key, ranked[0], ranked[1..]);
            })
            .OrderBy(g => g.Game, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Parses "USA, Europe, Japan" (names or No-Intro spellings) into a priority list.</summary>
    public static List<Region> ParsePriority(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [.. DefaultPriority];
        var regions = DatIndex.RegionsFromName($"({text})");
        return regions.Count > 0 ? regions : [.. DefaultPriority];
    }

    // The DAT name the rom was verified against, else its first title.
    private static string? Name(Rom rom) =>
        rom.Hashes?.FirstOrDefault(h => h.IsVerified && !string.IsNullOrEmpty(h.Description))?.Description
        ?? rom.Titles?.FirstOrDefault()?.Value;

    private static string GroupKey(Rom rom, List<DatIndex> dats)
    {
        var name = Name(rom)!;
        foreach (var dat in dats)
            if (dat.HasCloneInfo && dat.Games.Contains(name)) return DatIndex.CleanTitle(dat.ParentOf(name));
        return DatIndex.CleanTitle(name);
    }

    [GeneratedRegex(@"\((Beta|Proto|Prototype|Sample|Demo|Kiosk|Debug|Pirate|Unl)[^)]*\)|\[b\]", RegexOptions.IgnoreCase)]
    private static partial Regex UnwantedTag();

    [GeneratedRegex(@"\(Rev ([0-9A-Z.]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex Revision();

    // Lower is better: unwanted dumps last, then region priority, then newest revision/version.
    private static (int Unwanted, int Region, int Revision, int Version) Score(Rom rom, IReadOnlyList<Region> priority)
    {
        var name = Name(rom) ?? "";
        var regions = rom.Regions is { Count: > 0 } r ? r : DatIndex.RegionsFromName(name);
        var regionRank = regions.Select(x => IndexOf(priority, x)).Where(i => i >= 0).DefaultIfEmpty(priority.Count).Min();
        var unwanted = rom.IsDemo || UnwantedTag().IsMatch(name) ? 1 : 0;
        var rev = Revision().Match(name) is { Success: true } m ? RevisionNumber(m.Groups[1].Value) : 0;
        var version = int.TryParse(new string((rom.Version ?? "").Where(char.IsDigit).ToArray()), out var v) ? v : 0;
        return (unwanted, regionRank, -rev, -version);
    }

    private static int IndexOf(IReadOnlyList<Region> list, Region region)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == region) return i;
        return -1;
    }

    private static int RevisionNumber(string rev) =>
        int.TryParse(rev.Replace(".", ""), out var n) ? n : rev.Length == 1 && char.IsLetter(rev[0]) ? rev[0] - 'A' + 1 : 0;
}
