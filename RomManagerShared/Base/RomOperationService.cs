using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base.Database;
using RomManagerShared.Utils;

namespace RomManagerShared.Base;

/// <summary>
/// Core service that creates <see cref="QueuedOperation"/> instances for all
/// ROM management tasks. Keeps operation creation logic testable and out of the UI.
/// </summary>
public sealed class RomOperationService
{
    private readonly OperationQueue _queue;
    private readonly IServiceProvider _services;

    public OperationQueue Queue => _queue;

    public RomOperationService(OperationQueue queue, IServiceProvider services)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    // ── Rename ────────────────────────────────────────────────────────────

    /// <summary>
    /// Enqueue a rename operation for a single ROM using the given format template.
    /// Returns the queued operation. Supports undo (reverts to original filename).
    /// </summary>
    public QueuedOperation EnqueueRename(Rom rom, string formatString)
    {
        var originalPath = rom.Path;
        var fileName = Path.GetFileName(originalPath);

        return _queue.Enqueue(new QueuedOperation
        {
            Name = $"Rename: {fileName}",
            Description = $"Rename using template: {formatString}",
            Category = "Rename",
            ExecuteAsync = async (ct, progress) =>
            {
                var results = await FileRenamer.RenameFilesAsync(new[] { rom }, formatString, ct: ct);
                var result = results.FirstOrDefault();
                if (result is not null && !result.Success)
                    throw new InvalidOperationException(result.ErrorMessage ?? "Rename failed");
                progress.Report(100);
            },
            UndoAsync = async ct =>
            {
                if (rom.Path != originalPath && File.Exists(rom.Path))
                {
                    File.Move(rom.Path, originalPath!);
                    rom.Path = originalPath;
                }
                await Task.CompletedTask;
            },
        });
    }

    /// <summary>
    /// Enqueue a bulk rename operation for multiple ROMs.
    /// Supports undo (reverts all renames).
    /// </summary>
    public QueuedOperation EnqueueBulkRename(IReadOnlyList<Rom> roms, string formatString)
    {
        // Capture original paths for undo
        var originalPaths = roms.ToDictionary(r => r, r => r.Path);

        return _queue.Enqueue(new QueuedOperation
        {
            Name = $"Rename {roms.Count} ROM(s)",
            Description = $"Rename using template: {formatString}",
            Category = "Rename",
            ExecuteAsync = async (ct, progress) =>
            {
                var results = await FileRenamer.RenameFilesAsync(roms, formatString, ct: ct);
                int ok = results.Count(r => r.Success && r.Changed);
                int fail = results.Count(r => !r.Success);
                progress.Report(100);
                FileUtils.Log($"[Rename] Done: {ok} renamed, {fail} failed.");
                if (fail > 0 && ok == 0)
                    throw new InvalidOperationException($"All {fail} rename(s) failed.");
            },
            UndoAsync = async ct =>
            {
                foreach (var (rom, origPath) in originalPaths)
                {
                    if (rom.Path != origPath && !string.IsNullOrEmpty(rom.Path) && File.Exists(rom.Path))
                    {
                        File.Move(rom.Path, origPath!);
                        rom.Path = origPath;
                    }
                }
                await Task.CompletedTask;
            },
        });
    }

    // ── Move ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Enqueue a move operation that relocates ROMs to a target directory.
    /// Supports undo (moves files back to their original locations).
    /// </summary>
    public QueuedOperation EnqueueMove(IReadOnlyList<Rom> roms, string targetRoot, string? commonSourceRoot = null)
    {
        // Capture original paths for undo
        var originalPaths = roms.ToDictionary(r => r, r => r.Path);

        return _queue.Enqueue(new QueuedOperation
        {
            Name = $"Move {roms.Count} ROM(s) → {Path.GetFileName(targetRoot.TrimEnd(Path.DirectorySeparatorChar))}",
            Description = $"Move to: {targetRoot}",
            Category = "Move",
            ExecuteAsync = async (ct, progress) =>
            {
                var mover = new RomMover(targetRoot);
                var results = await mover.MoveFilesAsync(roms, commonSourceRoot, ct);
                int ok = results.Count(r => r.Success && r.Changed);
                int fail = results.Count(r => !r.Success);
                progress.Report(100);
                FileUtils.Log($"[Move] Done: {ok} moved, {fail} failed.");
                if (fail > 0 && ok == 0)
                    throw new InvalidOperationException($"All {fail} move(s) failed.");
            },
            UndoAsync = async ct =>
            {
                foreach (var (rom, origPath) in originalPaths)
                {
                    if (rom.Path != origPath && !string.IsNullOrEmpty(rom.Path) && File.Exists(rom.Path))
                    {
                        var dir = Path.GetDirectoryName(origPath);
                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir);
                        File.Move(rom.Path, origPath!);
                        rom.Path = origPath;
                    }
                }
                await Task.CompletedTask;
            },
        });
    }

    // ── Hash Calculation ──────────────────────────────────────────────────

    /// <summary>
    /// Enqueue hash calculation for selected ROMs. Computed hashes are saved
    /// to the database via <see cref="RomHashRepository"/>.
    /// </summary>
    public QueuedOperation EnqueueHashCalculation(IReadOnlyList<Rom> roms)
    {
        return _queue.Enqueue(new QueuedOperation
        {
            Name = $"Calculate Hashes ({roms.Count} ROM{(roms.Count == 1 ? "" : "s")})",
            Category = "Tools",
            ExecuteAsync = async (ct, progress) =>
            {
                var hashTypes = new[] { HashTypeEnum.CRC32, HashTypeEnum.MD5, HashTypeEnum.SHA1 };
                int done = 0;

                foreach (var rom in roms)
                {
                    ct.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(rom.Path)) continue;

                    try
                    {
                        List<RomHash> hashes;
                        if (rom.IsFolderFormat && Directory.Exists(rom.Path))
                            hashes = await HashUtils.CalculateRomFolderHashes(rom, hashTypes);
                        else if (File.Exists(rom.Path))
                            hashes = await HashUtils.CalculateRomHashes(rom, hashTypes);
                        else
                            continue;

                        // Persist to DB
                        await SaveHashesToDbAsync(hashes);

                        var hashStr = hashes.FirstOrDefault()?.Value ?? "(none)";
                        FileUtils.Log($"[Hash] {rom.Titles?.FirstOrDefault()?.Value ?? rom.TitleID}: {hashStr}");
                    }
                    catch (Exception ex)
                    {
                        FileUtils.Log($"[Hash] Error for '{rom.Path}': {ex.Message}");
                    }

                    done++;
                    progress.Report(roms.Count > 0 ? (double)done / roms.Count * 100 : 100);
                }

                FileUtils.Log($"[Hash] Finished calculating hashes for {done}/{roms.Count} ROM(s).");
            },
        });
    }

    /// <summary>Persist computed hashes to the database.</summary>
    private async Task SaveHashesToDbAsync(IEnumerable<RomHash> hashes)
    {
        try
        {
            using var scope = _services.CreateScope();
            var repo = scope.ServiceProvider.GetService<RomHashRepository>();
            if (repo is null) return;
            await repo.AddIfNewRange(hashes);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[Hash] DB save failed: {ex.Message}");
        }
    }

    // ── Duplicate Detection ───────────────────────────────────────────────

    /// <summary>
    /// Enqueue a duplicate detection scan. Results are logged and returned
    /// via the <paramref name="onResults"/> callback.
    /// </summary>
    public QueuedOperation EnqueueDuplicateDetection(
        IReadOnlyList<Rom> roms,
        Action<IReadOnlyList<DuplicateGroup>>? onResults = null)
    {
        return _queue.Enqueue(new QueuedOperation
        {
            Name = $"Find Duplicates ({roms.Count} ROM{(roms.Count == 1 ? "" : "s")})",
            Category = "Organise",
            ExecuteAsync = async (ct, progress) =>
            {
                var detector = new DuplicateDetector(DuplicateStrategy.Default);
                var groups = detector.FindDuplicates(roms);
                progress.Report(100);

                if (groups.Count == 0)
                {
                    FileUtils.Log("[FindDuplicates] No duplicates found.");
                    onResults?.Invoke(groups);
                    return;
                }

                foreach (var g in groups)
                {
                    var names = string.Join(", ", g.Roms.Select(r =>
                        r.Titles?.FirstOrDefault()?.Value ?? r.TitleID ?? "?"));
                    FileUtils.Log($"[FindDuplicates] Group '{g.GroupKey}' ({g.MatchedBy}): {names}");
                }

                FileUtils.Log($"[FindDuplicates] Found {groups.Count} duplicate group(s), " +
                             $"{groups.Sum(g => g.Redundant.Count)} redundant ROM(s).");
                onResults?.Invoke(groups);
                await Task.CompletedTask;
            },
        });
    }

    // ── Execute an IConsoleRomAction via the queue ────────────────────────

    /// <summary>
    /// Enqueue any <see cref="IConsoleRomAction"/> to run through the
    /// operation queue. The action's own ExecuteAsync is called inside.
    /// </summary>
    public QueuedOperation EnqueueAction(IConsoleRomAction action, IReadOnlyList<Rom> roms)
    {
        return _queue.Enqueue(new QueuedOperation
        {
            Name = action.Name,
            Category = action.Category,
            ExecuteAsync = async (ct, progress) =>
            {
                using var scope = _services.CreateScope();
                await action.ExecuteAsync(roms, scope.ServiceProvider, ct);
                progress.Report(100);
            },
        });
    }
}
