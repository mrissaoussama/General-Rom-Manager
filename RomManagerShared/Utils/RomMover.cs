using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>Result of a single ROM move operation.</summary>
public sealed record MoveResult(
    string SourcePath,
    string DestinationPath,
    bool Success,
    bool WasDryRun,
    string? ErrorMessage = null)
{
    public bool Changed => !string.Equals(SourcePath, DestinationPath, FileUtils.PathComparison);
}

/// <summary>
/// Moves ROM files to a structured target directory.
/// Preserves the relative sub-structure inside targetRoot by default.
/// </summary>
public sealed class RomMover
{
    private readonly string _targetRoot;
    private readonly ILogger? _logger;

    public RomMover(string targetRoot, ILogger? logger = null)
    {
        _targetRoot = targetRoot ?? throw new ArgumentNullException(nameof(targetRoot));
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Preview moves without touching any files.</summary>
    public IReadOnlyList<MoveResult> PreviewMoves(IEnumerable<Rom> roms, string? commonSourceRoot = null)
        => MoveCore(roms, commonSourceRoot, dryRun: true);

    /// <summary>Move files synchronously.</summary>
    public IReadOnlyList<MoveResult> MoveFiles(IEnumerable<Rom> roms, string? commonSourceRoot = null)
        => MoveCore(roms, commonSourceRoot, dryRun: false);

    /// <summary>Move files asynchronously with cancellation support.</summary>
    public Task<IReadOnlyList<MoveResult>> MoveFilesAsync(
        IEnumerable<Rom> roms,
        string? commonSourceRoot = null,
        CancellationToken ct = default)
        => Task.Run(() => MoveCore(roms, commonSourceRoot, dryRun: false, ct), ct);

    // ── Core ──────────────────────────────────────────────────────────────

    private IReadOnlyList<MoveResult> MoveCore(
        IEnumerable<Rom> roms,
        string? commonSourceRoot,
        bool dryRun,
        CancellationToken ct = default)
    {
        var results = new List<MoveResult>();
        var romList = roms?.Where(r => !string.IsNullOrEmpty(r.Path)).ToList();
        if (romList is null || romList.Count == 0) return results;

        // Folder-format roms (Wii U, PS3, Xbox) move as their whole folder; roms sharing a file or folder move once.
        var units = romList.GroupBy(UnitPath, FileUtils.PathComparer).ToList();

        // Infer common source root from the units' parent directories when not supplied
        string sourceRoot = commonSourceRoot
            ?? FindCommonRoot(units.Select(u => Path.GetDirectoryName(u.Key)!));

        // Validate disk space (skip in dry-run)
        if (!dryRun && !HasEnoughSpace(_targetRoot, units.Select(u => u.Key)))
        {
            _logger?.LogError("Not enough disk space at '{Target}'", _targetRoot);
            return results;
        }

        if (!dryRun)
        {
            try { Directory.CreateDirectory(_targetRoot); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cannot create target directory '{Target}'", _targetRoot);
                return results;
            }
        }

        foreach (var unit in units)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(Directory.Exists(unit.Key)
                ? MoveFolder(unit.Key, [.. unit], sourceRoot, dryRun, ct)
                : MoveOne(unit.First(), sourceRoot, dryRun, [.. unit.Skip(1)]));
        }

        return results;
    }

    private static string UnitPath(Rom rom) =>
        rom.IsFolderFormat ? RomUtils.GetRomBaseFolder(rom) : rom.Path!;

    private MoveResult MoveFolder(string folder, IReadOnlyList<Rom> roms, string sourceRoot, bool dryRun, CancellationToken ct)
    {
        string relative;
        try { relative = Path.GetRelativePath(sourceRoot, folder); }
        catch { relative = Path.GetFileName(folder); }
        var dest = Path.Combine(_targetRoot, relative);
        if (string.Equals(Path.GetFullPath(folder), Path.GetFullPath(dest), FileUtils.PathComparison))
            return new MoveResult(folder, dest, true, dryRun);
        for (int i = 1; Directory.Exists(dest) || File.Exists(dest); i++)
            dest = Path.Combine(_targetRoot, $"{relative} ({i})");
        if (dryRun)
            return new MoveResult(folder, dest, true, WasDryRun: true);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            MoveDirectoryVerified(folder, dest, ct);
            foreach (var rom in roms)
                rom.Path = Path.Combine(dest, Path.GetRelativePath(folder, rom.Path!));
            _logger?.LogInformation("Moved folder '{Folder}' → '{Dest}'", folder, dest);
            return new MoveResult(folder, dest, true, false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to move folder '{Src}'", folder);
            return new MoveResult(folder, dest, false, false, ex.Message);
        }
    }

    /// <summary>
    /// Same volume: a rename. Across volumes: copy every file, check each copy's length, and only then delete
    /// the source. A failed copy removes the partial destination and leaves the source untouched.
    /// </summary>
    public static void MoveDirectoryVerified(string source, string dest, CancellationToken ct = default)
    {
        if (string.Equals(Path.GetPathRoot(Path.GetFullPath(source)), Path.GetPathRoot(Path.GetFullPath(dest)), StringComparison.OrdinalIgnoreCase))
        {
            // On Unix every path shares the root "/", so a rename can still cross devices; copy then.
            try
            {
                Directory.Move(source, dest);
                return;
            }
            catch (IOException) when (Directory.Exists(source) && !Directory.Exists(dest)) { }
        }
        try
        {
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var target = Path.Combine(dest, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: false);
                if (new FileInfo(target).Length != new FileInfo(file).Length)
                    throw new IOException($"Copy of '{file}' is incomplete");
            }
            foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(dest, Path.GetRelativePath(source, dir)));
        }
        catch
        {
            try { if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true); } catch { /* keep the error below */ }
            throw;
        }
        Directory.Delete(source, recursive: true);
    }

    private MoveResult MoveOne(Rom rom, string sourceRoot, bool dryRun, IReadOnlyList<Rom>? sameFile = null)
    {
        string src = rom.Path!;

        // Compute relative path from source root → preserve structure
        string relative;
        try
        {
            relative = Path.GetRelativePath(sourceRoot, src);
        }
        catch
        {
            relative = Path.GetFileName(src);
        }

        string dest = Path.Combine(_targetRoot, relative);
        dest = GetUniqueDestPath(dest, src);

        if (dryRun)
            return new MoveResult(src, dest, true, WasDryRun: true);

        // Skip if already there
        if (string.Equals(src, dest, FileUtils.PathComparison))
            return new MoveResult(src, dest, true, false);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Move(src, dest);
            rom.Path = dest;
            foreach (var other in sameFile ?? []) other.Path = dest;
            _logger?.LogInformation("Moved '{File}' → '{Dest}'", Path.GetFileName(src), dest);
            return new MoveResult(src, dest, true, false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to move '{Src}'", src);
            return new MoveResult(src, dest, false, false, ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string GetUniqueDestPath(string dest, string sourcePath)
    {
        if (string.Equals(dest, sourcePath, FileUtils.PathComparison) || !Exists(dest))
            return dest;

        string dir = Path.GetDirectoryName(dest) ?? "";
        string name = Path.GetFileNameWithoutExtension(dest);
        string ext = Path.GetExtension(dest);
        int i = 1;
        string candidate;
        do { candidate = Path.Combine(dir, $"{name} ({i++}){ext}"); }
        while (Exists(candidate));
        return candidate;

        // A folder with the file's name is a collision too.
        static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
    }

    private static string FindCommonRoot(IEnumerable<string> paths)
    {
        string? common = null;
        foreach (var p in paths)
        {
            if (common == null) { common = p; continue; }
            var parts1 = common.Split(Path.DirectorySeparatorChar);
            var parts2 = p.Split(Path.DirectorySeparatorChar);
            int i = 0;
            while (i < parts1.Length && i < parts2.Length &&
                   string.Equals(parts1[i], parts2[i], FileUtils.PathComparison)) i++;
            common = string.Join(Path.DirectorySeparatorChar, parts1[..i]);
        }
        return common ?? "";
    }

    private static bool HasEnoughSpace(string targetRoot, IEnumerable<string> units)
    {
        try
        {
            string root = Path.GetPathRoot(targetRoot) ?? targetRoot;
            var drive = new DriveInfo(root);
            long needed = units.Sum(u =>
            {
                try
                {
                    return Directory.Exists(u)
                        ? Directory.EnumerateFiles(u, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length)
                        : new FileInfo(u).Length;
                }
                catch { return 0L; }
            });
            return drive.AvailableFreeSpace > needed;
        }
        catch { return true; /* assume ok if detection fails */ }
    }
}
