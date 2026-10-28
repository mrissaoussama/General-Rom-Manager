using RomManagerShared.Utils;

namespace RomManagerShared.Base.Library;

/// <summary>What "Add to library" does with the files it is given.</summary>
public enum ImportMode
{
    /// <summary>Move each file into <c>&lt;library folder&gt;/&lt;console&gt;/</c>.</summary>
    Move,
    /// <summary>Copy each file there; the originals stay.</summary>
    Copy,
    /// <summary>Leave the files where they are and add their folder to each console found in it.</summary>
    KeepInPlace,
}

/// <summary>
/// One unit of an import: a file with its companions (the tracks of a .cue, the discs of an .m3u) or a whole game
/// folder. <see cref="Destination"/> is where <see cref="Source"/> goes; companions go next to it.
/// <see cref="Skip"/> says why nothing happens to it.
/// </summary>
public sealed record ImportItem(string Source, bool IsDirectory, IReadOnlyList<string> Companions, ConsoleDescriptor? Console,
    string? Destination, string? Skip)
{
    public bool WillChange => Skip is null && Console is not null;
}

/// <summary>What an import did: the moves to undo it and the files that failed.</summary>
public sealed record ImportResult(IReadOnlyList<PathMove> Undo, IReadOnlyList<string> Created, IReadOnlyList<(string Path, string Error)> Failed, int Done);

/// <summary>
/// "File → Add to library": sorts files into one folder per console under a library folder
/// (<c>D:\Games\Nintendo 3DS\…</c>), shown as a plan first. The console of each file comes from its extension and,
/// for extensions several consoles share, its header.
/// </summary>
public sealed class LibraryImport(IReadOnlyList<ConsoleDescriptor> consoles,
    IReadOnlyDictionary<Type, IReadOnlyCollection<string>> extensionsByConsole, ConsoleDetector.HeaderCheck? check = null)
{
    // Sheets that name other files: those files travel with them.
    private static readonly HashSet<string> SheetExtensions = new(StringComparer.OrdinalIgnoreCase) { ".cue", ".gdi", ".m3u", ".ccd", ".mds" };

    /// <summary>The folder a console's roms go in under a library folder: its display name, made file-name safe.</summary>
    public static string ConsoleFolderName(ConsoleDescriptor console) => FileRenamer.SanitizeFileName(console.DisplayName);

    /// <summary>The files and folders directly inside <paramref name="folder"/>: each is one unit of the import.</summary>
    public static IReadOnlyList<string> Entries(string folder) =>
        [.. Directory.EnumerateFileSystemEntries(folder).Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// What would happen to <paramref name="sources"/> (files or game folders). With <see cref="ImportMode.KeepInPlace"/>
    /// or no <paramref name="libraryRoot"/>, items only get their console.
    /// </summary>
    public IReadOnlyList<ImportItem> Plan(IEnumerable<string> sources, string? libraryRoot, ImportMode mode)
    {
        var list = sources.Select(Path.GetFullPath).Distinct(FileUtils.PathComparer).ToList();
        var companions = FindCompanions(list);
        var attached = companions.Values.SelectMany(c => c).ToHashSet(FileUtils.PathComparer);
        var plan = new List<ImportItem>();
        foreach (var source in list.Where(s => !attached.Contains(s)))
        {
            var isDirectory = Directory.Exists(source);
            var with = companions.GetValueOrDefault(source) ?? [];
            var console = Classify(source, isDirectory, with, out var skip);
            if (console is null)
            {
                plan.Add(new ImportItem(source, isDirectory, with, null, null, skip));
                continue;
            }
            if (mode == ImportMode.KeepInPlace || libraryRoot is null)
            {
                plan.Add(new ImportItem(source, isDirectory, with, console, null, null));
                continue;
            }
            var folder = Path.Combine(Path.GetFullPath(libraryRoot), ConsoleFolderName(console));
            var destination = Path.Combine(folder, Path.GetFileName(Path.TrimEndingDirectorySeparator(source)));
            plan.Add(new ImportItem(source, isDirectory, with, console, destination, WhyNot(source, destination, with, folder)));
        }
        return plan;
    }

    private static string? WhyNot(string source, string destination, IReadOnlyList<string> companions, string folder)
    {
        if (string.Equals(source, destination, FileUtils.PathComparison))
            return "Already in place";
        if (folder.StartsWith(Path.TrimEndingDirectorySeparator(source) + Path.DirectorySeparatorChar, FileUtils.PathComparison))
            return "The library folder is inside it";
        if (File.Exists(destination) || Directory.Exists(destination)
            || companions.Any(c => File.Exists(Path.Combine(folder, Path.GetFileName(c)))))
            return "Something with this name is already there";
        return null;
    }

    private ConsoleDescriptor? Classify(string source, bool isDirectory, IReadOnlyList<string> companions, out string? skip)
    {
        skip = null;
        IEnumerable<string> files = isDirectory ? ConsoleDetector.SampleFiles(source) : [source, .. companions];
        var guesses = ConsoleDetector.Rank(files, extensionsByConsole, check);
        if (guesses.Count == 0)
        {
            skip = isDirectory ? "No ROM files inside" : "Not a ROM format";
            return null;
        }
        // Equal scores: a shared extension the headers could not settle (e.g. an .iso of an unknown disc).
        if (guesses.Count > 1 && Math.Abs(guesses[0].Score - guesses[1].Score) < 0.001)
        {
            skip = $"Could be {Describe(guesses[0].Console).DisplayName} or {Describe(guesses[1].Console).DisplayName}; add it from that console's page";
            return null;
        }
        return Describe(guesses[0].Console);
    }

    private ConsoleDescriptor Describe(Type console) =>
        consoles.FirstOrDefault(c => c.ConsoleType == console) ?? ConsoleModules.Describe(console);

    // A sheet (.cue, .gdi, .m3u…) takes the files whose names start with its own: "Game.cue" → "Game (Track 1).bin".
    // The longest matching sheet name wins ("Game 2.cue" over "Game.cue").
    private static Dictionary<string, List<string>> FindCompanions(IReadOnlyList<string> sources)
    {
        var sheets = sources.Where(s => File.Exists(s) && SheetExtensions.Contains(Path.GetExtension(s)))
            .Select(s => (Path: s, Dir: Path.GetDirectoryName(s), Stem: Path.GetFileNameWithoutExtension(s)))
            .ToList();
        var result = new Dictionary<string, List<string>>(FileUtils.PathComparer);
        if (sheets.Count == 0) return result;
        foreach (var file in sources.Where(s => File.Exists(s) && !SheetExtensions.Contains(Path.GetExtension(s))))
        {
            var name = Path.GetFileName(file);
            var dir = Path.GetDirectoryName(file);
            var owner = sheets
                .Where(s => string.Equals(s.Dir, dir, FileUtils.PathComparison) && name.StartsWith(s.Stem, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Stem.Length)
                .Select(s => s.Path)
                .FirstOrDefault();
            if (owner is null) continue;
            if (!result.TryGetValue(owner, out var list)) result[owner] = list = [];
            list.Add(file);
        }
        return result;
    }

    /// <summary>
    /// Moves or copies the items that will change. A failed item is left where it was (a partial copy is removed);
    /// the others go on. Moves are returned as undo steps; copies as the files created.
    /// </summary>
    public static ImportResult Execute(IReadOnlyList<ImportItem> plan, ImportMode mode, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var undo = new List<PathMove>();
        var created = new List<string>();
        var failed = new List<(string, string)>();
        var items = plan.Where(i => i.WillChange && i.Destination is not null).ToList();
        int attempted = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            var folder = Path.GetDirectoryName(item.Destination!)!;
            var steps = new List<(string From, string To, bool IsDirectory)> { (item.Source, item.Destination!, item.IsDirectory) };
            steps.AddRange(item.Companions.Select(c => (c, Path.Combine(folder, Path.GetFileName(c)), false)));
            var finished = new List<(string From, string To, bool IsDirectory)>();
            try
            {
                Directory.CreateDirectory(folder);
                foreach (var step in steps)
                {
                    if (mode == ImportMode.Copy) CopyEntry(step.From, step.To, step.IsDirectory, ct);
                    else if (step.IsDirectory) RomMover.MoveDirectoryVerified(step.From, step.To, ct);
                    else File.Move(step.From, step.To);
                    finished.Add(step);
                }
                foreach (var step in finished)
                {
                    if (mode == ImportMode.Copy) created.Add(step.To);
                    else undo.Add(new PathMove(step.To, step.From, step.IsDirectory));
                }
                AppLog.Info($"[Import] {(mode == ImportMode.Copy ? "Copied" : "Moved")} '{item.Source}' → '{item.Destination}'");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Put back what this item already moved, so a game never ends up split between two folders.
                foreach (var step in finished.AsEnumerable().Reverse())
                    try
                    {
                        if (mode == ImportMode.Copy) Delete(step.To, step.IsDirectory);
                        else if (step.IsDirectory) RomMover.MoveDirectoryVerified(step.To, step.From);
                        else File.Move(step.To, step.From);
                    }
                    catch (Exception rollback) when (rollback is IOException or UnauthorizedAccessException)
                    {
                        AppLog.Warn($"[Import] Could not put back '{step.To}': {rollback.Message}");
                    }
                failed.Add((item.Source, ex.Message));
                AppLog.Warn($"[Import] {item.Source}: {ex.Message}");
            }
            progress?.Report(++attempted * 100.0 / items.Count);
        }
        return new ImportResult(undo, created, failed, items.Count - failed.Count);
    }

    /// <summary>Deletes what a copy import created (its undo).</summary>
    public static void DeleteCreated(IEnumerable<string> created)
    {
        foreach (var path in created)
            try { Delete(path, Directory.Exists(path)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AppLog.Warn($"[Import] Could not delete '{path}': {ex.Message}");
            }
    }

    private static void Delete(string path, bool isDirectory)
    {
        if (isDirectory) Directory.Delete(path, recursive: true);
        else if (File.Exists(path)) File.Delete(path);
    }

    private static void CopyEntry(string from, string to, bool isDirectory, CancellationToken ct)
    {
        if (!isDirectory)
        {
            File.Copy(from, to, overwrite: false);
            return;
        }
        try
        {
            foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var target = Path.Combine(to, Path.GetRelativePath(from, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: false);
            }
        }
        catch
        {
            if (Directory.Exists(to)) Directory.Delete(to, recursive: true);
            throw;
        }
    }
}
