namespace RomManagerShared.Base;

/// <summary>Guesses which console a folder belongs to from the file extensions inside it.</summary>
public static class ConsoleDetector
{
    public sealed record Guess(Type Console, double Score, int Files);

    /// <summary>
    /// Scores each console by the files whose extension it parses. Extensions shared by several consoles
    /// (iso, chd) count for each of them proportionally less, so unique formats decide.
    /// </summary>
    public static IReadOnlyList<Guess> Rank(IEnumerable<string> files, IReadOnlyDictionary<Type, IReadOnlyCollection<string>> extensionsByConsole)
    {
        var owners = extensionsByConsole
            .SelectMany(kv => kv.Value.Select(e => (Ext: e.TrimStart('.').ToLowerInvariant(), Console: kv.Key)))
            .GroupBy(x => x.Ext)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Console).Distinct().ToList());

        var scores = new Dictionary<Type, (double Score, int Files)>();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
            if (!owners.TryGetValue(ext, out var consoles)) continue;
            foreach (var console in consoles)
            {
                var (score, count) = scores.GetValueOrDefault(console);
                scores[console] = (score + 1.0 / consoles.Count, count + 1);
            }
        }
        return [.. scores.Select(kv => new Guess(kv.Key, kv.Value.Score, kv.Value.Files)).OrderByDescending(g => g.Score)];
    }

    /// <summary>Files under <paramref name="root"/>, two levels deep and capped, so big shares stay fast.</summary>
    public static IEnumerable<string> SampleFiles(string root, int limit = 5000)
    {
        var options = new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 2, IgnoreInaccessible = true };
        return Directory.EnumerateFiles(root, "*", options).Take(limit);
    }
}
