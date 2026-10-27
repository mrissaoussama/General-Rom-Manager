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
    public const int ScanVersion = 15;
    private const int SaveBatchSize = 25;
    /// <summary>Files parsed concurrently. Kept low: most libraries live on HDDs or network shares.</summary>
    public int MaxParallelism { get; init; } = 4;

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

    public async Task<List<Rom>> ScanAsync<T>(ConsoleManager<T> manager, string console, string root,
        bool fullRescan = false, IProgress<ScanProgress>? progress = null, CancellationToken ct = default)
        where T : GamingConsole
    {
        var extensions = manager.RomParserExecutor.GetSupportedExtensions();
        var fingerprint = manager.TitleInfoProviderManager?.DataFingerprint ?? "";
        var files = await Task.Run(() => FileUtils.GetFilesInDirectoryWithExtensions(root, extensions), ct);
        var cached = await db.LibraryFiles
            .Where(f => f.Console == console)
            .ToDictionaryAsync(f => f.Path, StringComparer.OrdinalIgnoreCase, ct);

        // Per-file slots keep results in enumeration order.
        var slots = new List<Rom>[files.Count];
        var errors = new string?[files.Count];
        var pending = new List<int>();
        for (int i = 0; i < files.Count; i++)
        {
            cached.TryGetValue(files[i], out var entry);
            if (!fullRescan && entry is not null && IsUnchanged(entry, new FileInfo(files[i]), fingerprint))
            {
                slots[i] = RomSnapshot.Deserialize(entry.RomsJson);
                errors[i] = entry.Error;
            }
            else
                pending.Add(i);
        }

        int processed = files.Count - pending.Count, parsed = 0;
        progress?.Report(new ScanProgress(processed, files.Count, parsed, null));
        foreach (var batch in pending.Chunk(MaxParallelism))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new ScanProgress(processed, files.Count, parsed, files[batch[0]]));

            // Parsing is I/O bound and parallel; enrichment and saving share the DbContext, so stay sequential.
            var parsedBatch = await Task.WhenAll(batch.Select(i =>
                Task.Run(() => ParseDiagnostics.CaptureAsync(() => manager.RomParserExecutor.ExecuteParsers(files[i])), ct)));
            for (int b = 0; b < batch.Length; b++)
            {
                var path = files[batch[b]];
                var roms = await manager.EnrichAsync(parsedBatch[b].Result);
                slots[batch[b]] = roms;
                errors[batch[b]] = roms.Count == 0 ? ParseDiagnostics.Summarize(parsedBatch[b].Messages) : null;

                if (!cached.TryGetValue(path, out var entry))
                {
                    entry = new LibraryFile { Console = console, Path = path };
                    db.LibraryFiles.Add(entry);
                    cached[path] = entry;
                }
                var info = new FileInfo(path);
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

        var seen = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        var result = slots.SelectMany(r => r).ToList();
        Unrecognized = [.. files.Select((f, i) => (f, i)).Where(x => slots[x.i].Count == 0)
            .Select(x => new UnrecognizedFile(x.f, errors[x.i] ?? "No parser recognised the file"))];
        db.LibraryFiles.RemoveRange(cached.Values.Where(f => !seen.Contains(f.Path)));
        await db.SaveChangesAsync(ct);
        return result;
    }

    private static bool IsUnchanged(LibraryFile entry, FileInfo info, string fingerprint) =>
        entry.ScanVersion == ScanVersion
        && (entry.DataFingerprint ?? "") == fingerprint
        && entry.Size == info.Length
        && entry.LastWriteUtc == info.LastWriteTimeUtc;
}
