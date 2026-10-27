namespace RomManagerShared.Base.Library;

/// <summary>How one ROM folder is scanned.</summary>
/// <param name="MaxParallelism">Files parsed at once.</param>
/// <param name="ExcludedFolders">
/// Folders to skip. A plain name ("Backups") matches a folder of that name at any depth; a path with a
/// separator ("Updates/old") is relative to the ROM folder.
/// </param>
public sealed record ScanOptions(int MaxParallelism, IReadOnlyList<string> ExcludedFolders)
{
    public const int DefaultParallelism = 4;

    public static ScanOptions Default { get; } = new(DefaultParallelism, []);

    /// <summary>Options for <paramref name="root"/>: one file at a time on network drives unless set.</summary>
    public static ScanOptions For(string root, int? parallelism, IEnumerable<string>? excludedFolders) =>
        new(Math.Clamp(parallelism ?? (LibraryWatcher.IsNetworkPath(root) ? 1 : DefaultParallelism), 1, 32),
            [.. (excludedFolders ?? []).Select(Normalize).Where(e => e.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)]);

    public bool IsExcluded(string root, string file)
    {
        if (ExcludedFolders.Count == 0) return false;
        var folder = Path.GetDirectoryName(Path.GetRelativePath(root, file));
        if (string.IsNullOrEmpty(folder)) return false;
        var rel = Normalize(folder);
        var segments = rel.Split('/');
        foreach (var excluded in ExcludedFolders)
        {
            if (excluded.Contains('/'))
            {
                if (rel.Equals(excluded, StringComparison.OrdinalIgnoreCase)
                    || rel.StartsWith(excluded + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (segments.Contains(excluded, StringComparer.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string Normalize(string path) => path.Trim().Replace('\\', '/').Trim('/');
}
