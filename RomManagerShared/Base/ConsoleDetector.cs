using RomManagerShared.Utils;

namespace RomManagerShared.Base;

/// <summary>Guesses which console a folder belongs to from the file extensions inside it (and, for shared
/// extensions, the files' first bytes).</summary>
public static class ConsoleDetector
{
    public sealed record Guess(Type Console, double Score, int Files);

    /// <summary>Reads the header of a file with a shared extension and says whether a console may own it.</summary>
    public delegate bool HeaderCheck(Type console, string path, ReadOnlySpan<byte> header);

    /// <summary>
    /// Scores each console by the files whose extension it parses. Extensions shared by several consoles
    /// (iso, pkg) count for each of them proportionally less, so unique formats decide. With
    /// <paramref name="check"/>, up to <paramref name="sniffLimit"/> such files are opened and only the consoles
    /// whose parsers accept the header share them.
    /// </summary>
    public static IReadOnlyList<Guess> Rank(IEnumerable<string> files, IReadOnlyDictionary<Type, IReadOnlyCollection<string>> extensionsByConsole,
        HeaderCheck? check = null, int sniffLimit = 50)
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
            if (consoles.Count > 1 && check is not null && sniffLimit-- > 0)
                consoles = Sniff(file, consoles, check);
            foreach (var console in consoles)
            {
                var (score, count) = scores.GetValueOrDefault(console);
                scores[console] = (score + 1.0 / consoles.Count, count + 1);
            }
        }
        return [.. scores.Select(kv => new Guess(kv.Key, kv.Value.Score, kv.Value.Files)).OrderByDescending(g => g.Score)];
    }

    private static List<Type> Sniff(string file, List<Type> consoles, HeaderCheck check)
    {
        byte[] header;
        try { header = FileMagic.ReadHeader(file); }
        catch (IOException) { return consoles; }
        catch (UnauthorizedAccessException) { return consoles; }
        var accepted = consoles.Where(c => check(c, file, header)).ToList();
        return accepted.Count > 0 ? accepted : consoles;
    }

    /// <summary>Files under <paramref name="root"/>, two levels deep and capped, so big shares stay fast.</summary>
    public static IEnumerable<string> SampleFiles(string root, int limit = 5000)
    {
        var options = new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 2, IgnoreInaccessible = true };
        return Directory.EnumerateFiles(root, "*", options).Take(limit);
    }
}
