using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base.Database;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.Base.Library;

public sealed record ScanProgress(int Processed, int Total, int Parsed, string? CurrentFile);

/// <summary>Incremental scan: unchanged files come from the cache, the rest are parsed and stored.</summary>
public sealed class LibraryScanner(AppDbContext db)
{
    /// <summary>Bump when parser output changes so cached entries get re-parsed.</summary>
    public const int ScanVersion = 4;
    private const int SaveBatchSize = 25;

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
        var files = await Task.Run(() => FileUtils.GetFilesInDirectoryWithExtensions(root, extensions), ct);
        var cached = await db.LibraryFiles
            .Where(f => f.Console == console)
            .ToDictionaryAsync(f => f.Path, StringComparer.OrdinalIgnoreCase, ct);

        var result = new List<Rom>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int processed = 0, parsed = 0;
        foreach (var path in files)
        {
            ct.ThrowIfCancellationRequested();
            seen.Add(path);
            var info = new FileInfo(path);
            cached.TryGetValue(path, out var entry);

            if (!fullRescan && entry is not null && IsUnchanged(entry, info))
            {
                result.AddRange(RomSnapshot.Deserialize(entry.RomsJson));
            }
            else
            {
                progress?.Report(new ScanProgress(processed, files.Count, parsed, path));
                var roms = await Task.Run(() => manager.ParseFile(path), ct);
                result.AddRange(roms);

                if (entry is null)
                {
                    entry = new LibraryFile { Console = console, Path = path };
                    db.LibraryFiles.Add(entry);
                    cached[path] = entry;
                }
                entry.Size = info.Length;
                entry.LastWriteUtc = info.LastWriteTimeUtc;
                entry.ScanVersion = ScanVersion;
                entry.ScannedUtc = DateTime.UtcNow;
                entry.RomsJson = RomSnapshot.Serialize(roms);
                if (++parsed % SaveBatchSize == 0)
                    await db.SaveChangesAsync(ct);
            }
            progress?.Report(new ScanProgress(++processed, files.Count, parsed, null));
        }

        db.LibraryFiles.RemoveRange(cached.Values.Where(f => !seen.Contains(f.Path)));
        await db.SaveChangesAsync(ct);
        return result;
    }

    private static bool IsUnchanged(LibraryFile entry, FileInfo info) =>
        entry.ScanVersion == ScanVersion
        && entry.Size == info.Length
        && entry.LastWriteUtc == info.LastWriteTimeUtc;
}
