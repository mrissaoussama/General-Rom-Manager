using System.Collections.Concurrent;
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

    private readonly OperationJournal? _journal;
    private readonly ConcurrentDictionary<int, Guid> _journalOps = [];

    public RomOperationService(OperationQueue queue, IServiceProvider services, OperationJournal? journal = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _journal = journal;
        _queue.OperationsCleared += ops =>
        {
            // Clearing the list forgets the undo history too.
            foreach (var op in ops)
                if (_journalOps.TryRemove(op.Id, out var id)) _journal?.Remove(id);
        };
    }

    // ── Rename ────────────────────────────────────────────────────────────

    /// <summary>
    /// Enqueue a rename operation for a single ROM using the given format template.
    /// Supports undo (reverts to the original file or folder name), also after a restart.
    /// </summary>
    public QueuedOperation EnqueueRename(Rom rom, string formatString) =>
        EnqueuePathOperation([rom], $"Rename: {Path.GetFileName(rom.Path)}", $"Rename using template: {formatString}", "Rename",
            async ct =>
            {
                var results = await FileRenamer.RenameFilesAsync([rom], formatString, ct: ct);
                var result = results.FirstOrDefault();
                if (result is not null && !result.Success)
                    throw new InvalidOperationException(result.ErrorMessage ?? "Rename failed");
            });

    /// <summary>
    /// Enqueue a bulk rename operation for multiple ROMs.
    /// Supports undo (reverts all renames).
    /// </summary>
    public QueuedOperation EnqueueBulkRename(IReadOnlyList<Rom> roms, string formatString) =>
        EnqueuePathOperation(roms, $"Rename {roms.Count} ROM(s)", $"Rename using template: {formatString}", "Rename",
            async ct =>
            {
                var results = await FileRenamer.RenameFilesAsync(roms, formatString, ct: ct);
                int ok = results.Count(r => r.Success && r.Changed);
                int fail = results.Count(r => !r.Success);
                FileUtils.Log($"[Rename] Done: {ok} renamed, {fail} failed.");
                if (fail > 0 && ok == 0)
                    throw new InvalidOperationException($"All {fail} rename(s) failed.");
            });

    // ── Move ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Enqueue a move operation that relocates ROMs to a target directory.
    /// Supports undo (moves files back to their original locations).
    /// </summary>
    public QueuedOperation EnqueueMove(IReadOnlyList<Rom> roms, string targetRoot, string? commonSourceRoot = null) =>
        EnqueuePathOperation(roms,
            $"Move {roms.Count} ROM(s) → {Path.GetFileName(targetRoot.TrimEnd(Path.DirectorySeparatorChar))}",
            $"Move to: {targetRoot}", "Move",
            async ct =>
            {
                var results = await new RomMover(targetRoot).MoveFilesAsync(roms, commonSourceRoot, ct);
                int ok = results.Count(r => r.Success && r.Changed);
                int fail = results.Count(r => !r.Success);
                FileUtils.Log($"[Move] Done: {ok} moved, {fail} failed.");
                if (fail > 0 && ok == 0)
                    throw new InvalidOperationException($"All {fail} move(s) failed.");
            });

    // Rename and move share undo: remember where every rom was, journal the moves that put it back.
    private QueuedOperation EnqueuePathOperation(IReadOnlyList<Rom> roms, string name, string description,
        string category, Func<CancellationToken, Task> execute)
    {
        var originalPaths = roms.Select(r => (r, r.Path)).ToList(); // not a dictionary: Rom equality depends on Path
        JournalEntry? entry = null;
        QueuedOperation? op = null;

        op = new QueuedOperation
        {
            Name = name,
            Description = description,
            Category = category,
            ExecuteAsync = async (ct, progress) =>
            {
                try
                {
                    await execute(ct);
                }
                finally
                {
                    // Also after a partial failure: whatever did move can be put back.
                    var plan = BuildRestorePlan(originalPaths);
                    if (plan.Count > 0 && _journal is not null)
                    {
                        entry = _journal.Add(name, category, plan);
                        _journalOps[op!.Id] = entry.Id;
                    }
                }
                progress.Report(100);
            },
            UndoAsync = ct => Task.Run(() =>
            {
                RestorePaths(originalPaths, ct);
                if (entry is not null) _journal?.Remove(entry.Id);
            }, ct),
        };
        return _queue.Enqueue(op);
    }

    /// <summary>Re-adds journaled operations from earlier sessions to the queue as undoable.</summary>
    public void RestoreJournal()
    {
        if (_journal is null) return;
        foreach (var entry in _journal.Entries)
        {
            var op = new QueuedOperation
            {
                Name = entry.Name,
                Description = "From an earlier session",
                Category = entry.Category,
                ExecuteAsync = (_, _) => Task.CompletedTask,
                UndoAsync = ct => Task.Run(() =>
                {
                    ApplyRestorePlan(entry.Undo, ct);
                    _journal.Remove(entry.Id);
                }, ct),
            };
            _journalOps[op.Id] = entry.Id;
            _queue.AddCompleted(op, entry.CompletedAt);
        }
    }

    /// <summary>
    /// The moves that put every changed rom back. Each file or folder appears once; folder-format roms
    /// move as their whole folder, never just the marker file the parser opened.
    /// </summary>
    public static List<PathMove> BuildRestorePlan(IEnumerable<(Rom Rom, string? Original)> originalPaths)
    {
        var plan = new List<PathMove>();
        var units = originalPaths
            .Where(p => p.Original is not null && p.Rom.Path is not null && p.Rom.Path != p.Original)
            .GroupBy(p => p.Rom.IsFolderFormat ? RomUtils.GetRomBaseFolder(p.Rom) : p.Rom.Path!, StringComparer.OrdinalIgnoreCase);
        foreach (var unit in units)
        {
            var (rom, original) = unit.First();
            if (rom.IsFolderFormat && !string.Equals(unit.Key, rom.Path, StringComparison.OrdinalIgnoreCase))
            {
                // The marker file's path inside the folder is the same before and after the move.
                var inside = Path.GetRelativePath(unit.Key, rom.Path!);
                var originalFolder = original![..^inside.Length].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                plan.Add(new PathMove(unit.Key, originalFolder, true));
            }
            else
            {
                plan.Add(new PathMove(unit.Key, original!, Directory.Exists(unit.Key)));
            }
        }
        return plan;
    }

    /// <summary>Moves files and folders back. Entries whose current location no longer exists are skipped.</summary>
    public static void ApplyRestorePlan(IEnumerable<PathMove> plan, CancellationToken ct = default)
    {
        foreach (var move in plan)
        {
            ct.ThrowIfCancellationRequested();
            if (move.IsDirectory ? !Directory.Exists(move.From) : !File.Exists(move.From))
            {
                FileUtils.Log($"[Undo] '{move.From}' is gone; not restoring it to '{move.To}'.");
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(move.To)!);
            if (move.IsDirectory) RomMover.MoveDirectoryVerified(move.From, move.To, ct);
            else File.Move(move.From, move.To);
        }
    }

    /// <summary>Undo for rename/move: applies <see cref="BuildRestorePlan"/> and points every rom back.</summary>
    public static void RestorePaths(IEnumerable<(Rom Rom, string? Original)> originalPaths, CancellationToken ct = default)
    {
        var list = originalPaths.ToList();
        var plan = BuildRestorePlan(list);
        ApplyRestorePlan(plan, ct);
        foreach (var (rom, original) in list)
        {
            if (rom.Path is null || original is null) continue;
            var move = plan.FirstOrDefault(m => m.IsDirectory
                ? rom.Path.StartsWith(m.From + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                : string.Equals(rom.Path, m.From, StringComparison.OrdinalIgnoreCase));
            if (move is null) continue;
            rom.Path = move.IsDirectory ? Path.Combine(move.To, Path.GetRelativePath(move.From, rom.Path)) : original;
        }
    }

    // ── Convert ───────────────────────────────────────────────────────────

    /// <summary>
    /// Converts each rom's file with a user-supplied tool (chdman, DolphinTool, …). Originals are kept; the
    /// output lands next to them. Undo deletes the files the conversion created.
    /// </summary>
    public QueuedOperation EnqueueConvert(IReadOnlyList<Rom> roms, ConversionTool tool, string exePath)
    {
        var inputs = roms.Where(r => !r.IsFolderFormat && r.Path is not null && tool.Accepts(r.Path))
            .Select(r => r.Path!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var created = new List<string>();
        return _queue.Enqueue(new QueuedOperation
        {
            Name = $"Convert {inputs.Count} file(s) → {tool.DisplayName}",
            Category = "Convert",
            ExecuteAsync = async (ct, progress) =>
            {
                int done = 0, failed = 0;
                string? firstError = null;
                foreach (var input in inputs)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var output = await ToolConverter.ConvertAsync(tool, exePath, input, ct);
                        lock (created) created.Add(output);
                        FileUtils.Log($"[Convert] {Path.GetFileName(input)} → {Path.GetFileName(output)}");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failed++;
                        firstError ??= ex.Message;
                        FileUtils.Log($"[Convert] {input}: {ex.Message}");
                    }
                    progress.Report(++done * 100.0 / inputs.Count);
                }
                if (failed > 0 && failed == inputs.Count)
                    throw new InvalidOperationException($"All {failed} conversion(s) failed: {firstError}");
            },
            UndoAsync = ct => Task.Run(() =>
            {
                lock (created)
                    foreach (var file in created.Where(File.Exists)) File.Delete(file);
            }, ct),
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
