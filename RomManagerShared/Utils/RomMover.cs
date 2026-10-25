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
    public bool Changed => !string.Equals(SourcePath, DestinationPath, StringComparison.OrdinalIgnoreCase);
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

        // Infer common source root from first file's directory when not supplied
        string sourceRoot = commonSourceRoot
            ?? FindCommonRoot(romList.Select(r => Path.GetDirectoryName(r.Path)!));

        // Validate disk space (skip in dry-run)
        if (!dryRun && !HasEnoughSpace(_targetRoot, romList))
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

        foreach (var rom in romList)
        {
            ct.ThrowIfCancellationRequested();
            var result = MoveOne(rom, sourceRoot, dryRun);
            results.Add(result);
        }

        return results;
    }

    private MoveResult MoveOne(Rom rom, string sourceRoot, bool dryRun)
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
        if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase))
            return new MoveResult(src, dest, true, false);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Move(src, dest);
            rom.Path = dest;
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
        if (!File.Exists(dest) ||
            string.Equals(dest, sourcePath, StringComparison.OrdinalIgnoreCase))
            return dest;

        string dir = Path.GetDirectoryName(dest) ?? "";
        string name = Path.GetFileNameWithoutExtension(dest);
        string ext = Path.GetExtension(dest);
        int i = 1;
        string candidate;
        do { candidate = Path.Combine(dir, $"{name} ({i++}){ext}"); }
        while (File.Exists(candidate));
        return candidate;
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
                   string.Equals(parts1[i], parts2[i], StringComparison.OrdinalIgnoreCase)) i++;
            common = string.Join(Path.DirectorySeparatorChar, parts1[..i]);
        }
        return common ?? "";
    }

    private static bool HasEnoughSpace(string targetRoot, IList<Rom> roms)
    {
        try
        {
            string root = Path.GetPathRoot(targetRoot) ?? targetRoot;
            var drive = new DriveInfo(root);
            long needed = roms.Sum(r =>
            {
                try { return new FileInfo(r.Path!).Length; }
                catch { return 0L; }
            });
            return drive.AvailableFreeSpace > needed;
        }
        catch { return true; /* assume ok if detection fails */ }
    }
}
