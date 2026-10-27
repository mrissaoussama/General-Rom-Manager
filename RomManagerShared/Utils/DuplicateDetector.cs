using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>
/// Strategy to use when comparing ROMs for duplicates.
/// </summary>
[Flags]
public enum DuplicateStrategy
{
    None      = 0,
    /// <summary>Match by TitleID (recommended for Nintendo / PlayStation).</summary>
    TitleId   = 1 << 0,
    /// <summary>Match by file hash stored on the ROM entity.</summary>
    Hash      = 1 << 1,
    /// <summary>Match by normalised title name (case-insensitive, strip articles).</summary>
    TitleName = 1 << 2,
    /// <summary>Default: TitleID + Hash.</summary>
    Default   = TitleId | Hash
}

/// <summary>A group of ROMs that are considered duplicates of each other.</summary>
public sealed record DuplicateGroup(
    string GroupKey,
    DuplicateStrategy MatchedBy,
    IReadOnlyList<Rom> Roms)
{
    /// <summary>
    /// Returns all but the "best" ROM — the one with the highest version, or
    /// most regions, or simply the first when all else is equal.
    /// </summary>
    public IReadOnlyList<Rom> Redundant => Roms.Skip(1).ToList();

    /// <summary>
    /// The preferred ROM to keep (highest version > most regions > first).
    /// </summary>
    public Rom Preferred => Roms[0];
}

/// <summary>
/// Detects duplicate ROMs using configurable strategies.
/// </summary>
public sealed class DuplicateDetector
{
    private readonly DuplicateStrategy _strategy;
    private readonly ILogger? _logger;

    public DuplicateDetector(DuplicateStrategy strategy = DuplicateStrategy.Default, ILogger? logger = null)
    {
        _strategy = strategy;
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Find all duplicate groups in the supplied list.
    /// </summary>
    public IReadOnlyList<DuplicateGroup> FindDuplicates(IEnumerable<Rom> roms)
    {
        var romList = roms?.ToList() ?? [];
        var groups = new List<DuplicateGroup>();

        if (_strategy.HasFlag(DuplicateStrategy.TitleId))
            groups.AddRange(FindByTitleId(romList));

        if (_strategy.HasFlag(DuplicateStrategy.Hash))
            groups.AddRange(FindByHash(romList));

        if (_strategy.HasFlag(DuplicateStrategy.TitleName))
            groups.AddRange(FindByTitleName(romList));

        _logger?.LogInformation(
            "Duplicate detection found {Count} groups from {Total} ROMs",
            groups.Count, romList.Count);

        return groups;
    }

    /// <summary>
    /// Convenience: return every ROM that is NOT the preferred copy.
    /// </summary>
    public IReadOnlyList<Rom> FindRedundantRoms(IEnumerable<Rom> roms)
        => FindDuplicates(roms)
            .SelectMany(g => g.Redundant)
            .Distinct()
            .ToList();

    /// <summary>
    /// Files holding the same rom: same title ID, rom type and version, in different files. (Grouping by title ID
    /// alone would pair a PS4 game with its update, which share the ID.) The copy to keep comes first: a
    /// DAT-verified dump, then the larger file, then the shorter path.
    /// </summary>
    public static IReadOnlyList<DuplicateGroup> FindExactCopies(IEnumerable<Rom> roms) =>
        [.. roms
            .Where(r => !string.IsNullOrEmpty(r.TitleID) && !string.IsNullOrEmpty(r.Path))
            .GroupBy(r => (Id: r.TitleID!.ToUpperInvariant(), Type: r.GetType().Name, Version: r.Version ?? ""))
            .Select(g => g.DistinctBy(r => r.Path!, FileUtils.PathComparer).ToList())
            .Where(g => g.Count > 1)
            .Select(g => new DuplicateGroup(g[0].TitleID!, DuplicateStrategy.TitleId,
                [.. g.OrderByDescending(r => r.Hashes?.Any(h => h.IsVerified) == true)
                     .ThenByDescending(r => r.Size ?? TryGetFileSize(r.Path))
                     .ThenBy(r => r.Path!.Length)]))];

    // ── Strategy implementations ──────────────────────────────────────────

    private static IEnumerable<DuplicateGroup> FindByTitleId(IList<Rom> roms)
    {
        return roms
            .Where(r => !string.IsNullOrEmpty(r.TitleID))
            .GroupBy(r => r.TitleID!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup(
                g.Key,
                DuplicateStrategy.TitleId,
                RankGroup(g.ToList())));
    }

    private static IEnumerable<DuplicateGroup> FindByHash(IList<Rom> roms)
    {
        return roms
            .Where(r => r.Hashes?.Count > 0)
            .SelectMany(r => r.Hashes!.Select(h => (Hash: h.Value, Rom: r)))
            .Where(x => !string.IsNullOrEmpty(x.Hash))
            .GroupBy(x => x.Hash!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup(
                g.Key,
                DuplicateStrategy.Hash,
                RankGroup(g.Select(x => x.Rom).Distinct().ToList())));
    }

    private static IEnumerable<DuplicateGroup> FindByTitleName(IList<Rom> roms)
    {
        return roms
            .Where(r => r.Titles?.Count > 0)
            .GroupBy(
                r => NormalizeTitleName(r.Titles!.FirstOrDefault()?.Value ?? ""),
                StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0 && g.Count() > 1)
            .Select(g => new DuplicateGroup(
                g.Key,
                DuplicateStrategy.TitleName,
                RankGroup(g.ToList())));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Orders roms so index 0 is the "best" copy:
    ///   1. Highest numeric version (null = 0)
    ///   2. Most regions
    ///   3. Largest file size (higher quality dump)
    /// </summary>
    private static IReadOnlyList<Rom> RankGroup(IList<Rom> roms)
    {
        return roms
            .OrderByDescending(r => ParseVersion(r.Version))
            .ThenByDescending(r => r.Regions?.Count ?? 0)
            .ThenByDescending(r => TryGetFileSize(r.Path))
            .ToList();
    }

    private static double ParseVersion(string? version)
    {
        if (string.IsNullOrEmpty(version)) return 0;
        // Handle "v1.2.3" style
        string trimmed = version.TrimStart('v', 'V');
        return double.TryParse(trimmed, out double v) ? v : 0;
    }

    private static long TryGetFileSize(string? path)
    {
        try { return path != null ? new FileInfo(path).Length : 0; }
        catch { return 0; }
    }

    private static string NormalizeTitleName(string name)
    {
        // Strip leading articles
        string lower = name.ToLowerInvariant().Trim();
        foreach (var article in new[] { "the ", "a ", "an " })
            if (lower.StartsWith(article)) return lower[article.Length..].Trim();
        return lower;
    }
}
