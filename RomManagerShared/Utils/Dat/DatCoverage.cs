using RomManagerShared.Base;

namespace RomManagerShared.Utils.Dat;

/// <summary>A title info provider backed by DAT files; used for have/miss reports.</summary>
public interface IDatSource
{
    /// <summary>The loaded DATs for this console, or null when there are none.</summary>
    DatIndex? Index { get; }
}

/// <summary>Which games of a DAT set the library has (matched by verified hash) and which are missing.</summary>
public sealed record DatCoverage(int Total, IReadOnlyList<string> Have, IReadOnlyList<string> Missing)
{
    public static DatCoverage Compute(DatIndex index, IEnumerable<Rom> library)
    {
        // DatTitleInfoProvider.Apply stores the DAT game name on the verified hashes.
        var have = library
            .SelectMany(r => r.Hashes ?? [])
            .Where(h => h.IsVerified && h.Description is not null && index.Games.Contains(h.Description))
            .Select(h => h.Description!)
            .ToHashSet(StringComparer.Ordinal);
        var missing = index.Games.Where(g => !have.Contains(g)).Order(StringComparer.OrdinalIgnoreCase).ToList();
        return new DatCoverage(index.Games.Count, [.. have.Order(StringComparer.OrdinalIgnoreCase)], missing);
    }

    /// <summary>Several DAT sources for one console (e.g. No-Intro and Redump) merged into one report.</summary>
    public static DatCoverage? Compute(IEnumerable<DatIndex> indexes, IReadOnlyList<Rom> library)
    {
        var parts = indexes.Select(i => Compute(i, library)).ToList();
        if (parts.Count == 0) return null;
        return new DatCoverage(parts.Sum(p => p.Total),
            [.. parts.SelectMany(p => p.Have).Distinct()], [.. parts.SelectMany(p => p.Missing).Distinct()]);
    }
}
