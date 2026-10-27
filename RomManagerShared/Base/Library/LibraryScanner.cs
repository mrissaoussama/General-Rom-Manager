using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.Base.Library;

public sealed record ScanProgress(int Processed, int Total, int Parsed, string? CurrentFile);

/// <summary>A file with a ROM extension that parsed to nothing, and why.</summary>
public sealed record UnrecognizedFile(string Path, string Reason);

/// <summary>Incremental scan: unchanged files come from the cache, the rest are parsed and stored.</summary>
public sealed class LibraryScanner(AppDbContext db)
{
    /// <summary>Bump when parser output changes so cached entries get re-parsed.</summary>
    public const int ScanVersion = 17;
    private const int SaveBatchSize = 25;

    /// <summary>Files from the last <see cref="ScanAsync{T}"/> that matched an extension but produced no roms.</summary>
    public IReadOnlyList<UnrecognizedFile> Unrecognized { get; private set; } = [];

    public async Task<List<Rom>> LoadCachedAsync(string console, CancellationToken ct = default)
    {
        var files = await db.LibraryFiles.AsNoTracking()
            .Where(f => f.Console == console)
            .Select(f => f.RomsJson)
            .ToListAsync(ct);
        return files.SelectMany(RomSnapshot.Deserialize).ToList();
    }

    public Task<List<Rom>> ScanAsync<T>(ConsoleManager<T> manager, string console, string root,
        bool fullRescan = false, IProgress<ScanProgress>? progress = null, CancellationToken ct = default,
        ScanOptions? options = null)
        where T : GamingConsole =>
        ScanAsync(manager, console, [root], fullRescan, progress, ct, _ => options ?? ScanOptions.Default);

    /// <summary>
    /// Scans every folder in <paramref name="roots"/> as one library (missing folders are skipped). Each folder
    /// uses its own exclusions; files are parsed with the lowest parallelism of the folders.
    /// </summary>
    public async Task<List<Rom>> ScanAsync<T>(ConsoleManager<T> manager, string console, IReadOnlyList<string> roots,
        bool fullRescan, IProgress<ScanProgress>? progress, CancellationToken ct, Func<string, ScanOptions> optionsFor)
        where T : GamingConsole
    {
        var extensions = manager.RomParserExecutor.GetSupportedExtensions();
        var fingerprint = manager.TitleInfoProviderManager?.DataFingerprint ?? "";
        var present = roots.Where(Directory.Exists).Select(r => (Root: r, Options: optionsFor(r))).ToList();
        var parallelism = present.Count == 0 ? ScanOptions.DefaultParallelism : present.Min(p => p.Options.MaxParallelism);
        var files = await Task.Run(() => present
            .SelectMany(p => FileUtils.GetFilesInDirectoryWithExtensions(p.Root, extensions).Where(f => !p.Options.IsExcluded(p.Root, f)))
            .Distinct(FileUtils.PathComparer)
            .ToList(), ct);
        // Keyed the way the OS compares paths: on Linux, a.nsp and A.nsp are two files.
        var cached = new Dictionary<string, LibraryFile>(FileUtils.PathComparer);
        foreach (var f in await db.LibraryFiles.Where(f => f.Console == console).ToListAsync(ct))
            if (!cached.TryAdd(f.Path, f)) db.LibraryFiles.Remove(f); // a duplicate row from an older build


        // Per-file slots keep results in enumeration order.
        var slots = new List<Rom>[files.Count];
        var errors = new string?[files.Count];
        var gone = new bool[files.Count];
        var pending = new List<int>();
        for (int i = 0; i < files.Count; i++)
        {
            cached.TryGetValue(files[i], out var entry);
            if (!fullRescan && entry is not null && IsUnchanged(entry, files[i], fingerprint))
            {
                slots[i] = RomSnapshot.Deserialize(entry.RomsJson);
                errors[i] = entry.Error;
            }
            else
                pending.Add(i);
        }

        int processed = files.Count - pending.Count, parsed = 0;
        progress?.Report(new ScanProgress(processed, files.Count, parsed, null));
        foreach (var batch in pending.Chunk(parallelism))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new ScanProgress(processed, files.Count, parsed, files[batch[0]]));

            // Parsing is I/O bound and parallel; enrichment and saving share the DbContext, so stay sequential.
            var parsedBatch = await Task.WhenAll(batch.Select(i =>
                Task.Run(() => manager.RomParserExecutor.ParseAsync(files[i], ct), ct)));
            for (int b = 0; b < batch.Length; b++)
            {
                var path = files[batch[b]];
                // Deleted or moved since the folder was listed (e.g. a copy in progress): the next scan sees it.
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    slots[batch[b]] = [];
                    gone[batch[b]] = true;
                    processed++;
                    continue;
                }
                var roms = await manager.EnrichAsync(parsedBatch[b].Roms, ct);
                slots[batch[b]] = roms;
                errors[batch[b]] = roms.Count == 0 ? parsedBatch[b].UnidentifiedReason : null;

                if (!cached.TryGetValue(path, out var entry))
                {
                    entry = new LibraryFile { Console = console, Path = path };
                    db.LibraryFiles.Add(entry);
                    cached[path] = entry;
                }
                entry.Size = info.Length;
                entry.LastWriteUtc = info.LastWriteTimeUtc;
                entry.ScanVersion = ScanVersion;
                entry.ScannedUtc = DateTime.UtcNow;
                entry.DataFingerprint = fingerprint.Length > 0 ? fingerprint : null;
                entry.RomsJson = RomSnapshot.Serialize(roms);
                entry.Error = errors[batch[b]];
                if (++parsed % SaveBatchSize == 0)
                    await db.SaveChangesAsync(ct);
                processed++;
            }
            progress?.Report(new ScanProgress(processed, files.Count, parsed, null));
        }

        var seen = new HashSet<string>(files.Where((_, i) => !gone[i]), FileUtils.PathComparer);
        var result = slots.SelectMany(r => r).ToList();

        // A folder that is offline (unplugged drive, share down) keeps its cached rows until it is back.
        var offline = roots.Where(r => !Directory.Exists(r))
            .Select(r => Path.TrimEndingDirectorySeparator(Path.GetFullPath(r)) + Path.DirectorySeparatorChar).ToList();
        bool UnderOffline(string path) => offline.Any(o => path.StartsWith(o, FileUtils.PathComparison));
        foreach (var kept in cached.Values.Where(f => !seen.Contains(f.Path) && UnderOffline(f.Path)))
        {
            seen.Add(kept.Path);
            result.AddRange(RomSnapshot.Deserialize(kept.RomsJson));
        }
        Unrecognized = [.. files.Select((f, i) => (f, i)).Where(x => slots[x.i].Count == 0 && seen.Contains(x.f))
            .Select(x => new UnrecognizedFile(x.f, errors[x.i] ?? "No parser recognised the file"))];
        db.LibraryFiles.RemoveRange(cached.Values.Where(f => !seen.Contains(f.Path)));
        await db.SaveChangesAsync(ct);
        return result;
    }

    private static bool IsUnchanged(LibraryFile entry, string path, string fingerprint)
    {
        var info = new FileInfo(path);
        return info.Exists
            && entry.ScanVersion == ScanVersion
            && (entry.DataFingerprint ?? "") == fingerprint
            && entry.Size == info.Length
            && entry.LastWriteUtc == info.LastWriteTimeUtc;
    }
}
