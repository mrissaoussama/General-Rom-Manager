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
    public const int ScanVersion = 18;
    private const int SaveBatchSize = 25;

    /// <summary>Files from the last <see cref="ScanAsync{T}"/> that matched an extension but produced no roms.</summary>
    public IReadOnlyList<UnrecognizedFile> Unrecognized { get; private set; } = [];

    public async Task<List<Rom>> LoadCachedAsync(string console, CancellationToken ct = default)
    {
        var files = await db.LibraryFiles.AsNoTracking()
            .Where(f => f.Console == console)
            .Select(f => f.RomsJson)
            .ToListAsync(ct);
        var roms = files.SelectMany(RomSnapshot.Deserialize).ToList();
        await new MetadataOverrideStore(db).ApplyAsync(console, roms, ct);
        return roms;
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

        // Parsing is I/O bound and runs on a pool of workers; enrichment and saving share the DbContext, so they
        // take results one at a time as they come in. A slow file only holds up its own worker.
        await foreach (var (i, parse) in ParseAllAsync(manager.RomParserExecutor, files, pending, parallelism, ct))
        {
            var path = files[i];
            processed++;
            // Deleted or moved since the folder was listed (e.g. a copy in progress): the next scan sees it.
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                slots[i] = [];
                gone[i] = true;
                progress?.Report(new ScanProgress(processed, files.Count, parsed, path));
                continue;
            }
            var roms = await manager.EnrichAsync(parse.Roms, ct);
            FillSizes(roms, info.Length);
            slots[i] = roms;
            errors[i] = roms.Count == 0 ? parse.UnidentifiedReason : null;

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
            entry.Error = errors[i];
            if (++parsed % SaveBatchSize == 0)
                await db.SaveChangesAsync(ct);
            progress?.Report(new ScanProgress(processed, files.Count, parsed, path));
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
        await new MetadataOverrideStore(db).ApplyAsync(console, result, ct);
        return result;
    }

    // Size on disk for roms the parser gave none: the file, or the whole game folder for folder formats.
    private static void FillSizes(List<Rom> roms, long fileLength)
    {
        foreach (var rom in roms.Where(r => r.Size is not > 0))
        {
            try
            {
                rom.Size = rom.IsFolderFormat && Directory.Exists(RomUtils.GetRomBaseFolder(rom))
                    ? FileUtils.GetDirectorySize(RomUtils.GetRomBaseFolder(rom))
                    : fileLength;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                rom.Size = fileLength;
            }
        }
    }

    /// <summary>
    /// Parses <paramref name="pending"/> with up to <paramref name="parallelism"/> files at once and yields each
    /// result as soon as it is ready (not in file order). Stops the workers when the reader stops early.
    /// </summary>
    private static async IAsyncEnumerable<(int Index, ParseResult Result)> ParseAllAsync(IRomParserExecutor executor,
        List<string> files, List<int> pending, int parallelism,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (pending.Count == 0) yield break;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var channel = System.Threading.Channels.Channel.CreateBounded<(int, ParseResult)>(parallelism * 2);
        var workers = Task.Run(async () =>
        {
            try
            {
                await Parallel.ForEachAsync(pending, new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = stop.Token },
                    async (i, token) => await channel.Writer.WriteAsync((i, await executor.ParseAsync(files[i], token)), token));
                channel.Writer.Complete();
            }
            catch (Exception ex) { channel.Writer.Complete(ex); }
        }, CancellationToken.None);

        try
        {
            while (true)
            {
                (int, ParseResult) item;
                try
                {
                    if (!await channel.Reader.WaitToReadAsync(ct)) break;
                    if (!channel.Reader.TryRead(out item)) continue;
                }
                catch (System.Threading.Channels.ChannelClosedException ex) when (ex.InnerException is not null)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(ex.InnerException);
                    throw;
                }
                yield return item;
            }
        }
        finally
        {
            stop.Cancel();
            await workers;
        }
    }

    /// <summary>
    /// Writes the roms' hashes into their files' cached scan results, so they show on the next load. A rom is matched
    /// by type, title ID and version within its file's entry; roms whose file has no entry are skipped. Only the hashes
    /// are copied: the roms in memory carry metadata overrides, which must stay out of the cache so an undo removes them.
    /// The cache entry is dropped as usual when the file changes.
    /// </summary>
    public async Task<int> UpdateCachedHashesAsync(IEnumerable<Rom> roms, CancellationToken ct = default)
    {
        int updated = 0;
        foreach (var byFile in roms.Where(r => r.Path is not null).GroupBy(r => r.Path!))
        {
            foreach (var entry in await db.LibraryFiles.Where(f => f.Path == byFile.Key).ToListAsync(ct))
            {
                var cachedRoms = RomSnapshot.Deserialize(entry.RomsJson);
                bool changed = false;
                foreach (var rom in byFile)
                {
                    var i = cachedRoms.FindIndex(c => c.GetType() == rom.GetType() && c.TitleID == rom.TitleID && c.Version == rom.Version);
                    if (i < 0) continue;
                    cachedRoms[i].Hashes = rom.Hashes;
                    changed = true;
                    updated++;
                }
                if (changed) entry.RomsJson = RomSnapshot.Serialize(cachedRoms);
            }
        }
        await db.SaveChangesAsync(ct);
        return updated;
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
