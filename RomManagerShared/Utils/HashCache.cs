using System.Text.Json;
using RomManagerShared.Configuration;

namespace RomManagerShared.Utils;

/// <summary>
/// Remembers SHA-1/CRC32 of large files by path, size and modification time, so disc images are hashed
/// once even when DATs change and their console is re-scanned. Stored as JSON in the cache folder.
/// </summary>
public sealed class HashCache(string path)
{
    public sealed record Entry(long Size, long MTimeTicks, string Sha1, string Crc);

    private readonly object _lock = new();
    private Dictionary<string, Entry>? _entries;

    private static HashCache? _default;

    /// <summary><c>&lt;Cache&gt;/hashes.json</c>; null when no cache folder is configured.</summary>
    public static HashCache? Default
    {
        get
        {
            if (_default is not null) return _default;
            var cache = RomManagerConfiguration.GetCachePath();
            return string.IsNullOrEmpty(cache) ? null : _default = new HashCache(Path.Combine(cache, "hashes.json"));
        }
    }

    public async Task<(string Sha1, string Crc, long Size)> GetOrComputeAsync(string file,
        Func<string, Task<(string Sha1, string Crc, long Size)>> compute)
    {
        var info = new FileInfo(file);
        var key = Path.GetFullPath(file);
        lock (_lock)
        {
            if (Load().TryGetValue(key, out var hit) && hit.Size == info.Length && hit.MTimeTicks == info.LastWriteTimeUtc.Ticks)
                return (hit.Sha1, hit.Crc, hit.Size);
        }
        var result = await compute(file);
        lock (_lock)
        {
            Load()[key] = new Entry(info.Length, info.LastWriteTimeUtc.Ticks, result.Sha1, result.Crc);
            Save();
        }
        return result;
    }

    private Dictionary<string, Entry> Load()
    {
        if (_entries is not null) return _entries;
        try
        {
            _entries = File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path)) ?? []
                : [];
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[HashCache] {path}: {ex.Message}");
            _entries = [];
        }
        _entries = new Dictionary<string, Entry>(_entries, StringComparer.OrdinalIgnoreCase);
        return _entries;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_entries));
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex) { AppLog.Warn($"[HashCache] save failed: {ex.Message}"); }
    }
}
