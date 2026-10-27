using System.Buffers;
using System.Security.Cryptography;
using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;

namespace RomManagerShared.Utils.Dat;

/// <summary>
/// Matches disc images against Redump DATs. Hashing a DVD image takes a while, so this is opt-in:
/// set <c>"Dats": { "HashDiscImages": true }</c> in config.json. Single-file images (.iso/.gcm) are hashed
/// whole; for .cue/.gdi sets the first track is hashed (Redump lists each track, and track 1 identifies
/// the disc). Compressed formats (CHD, CSO, RVZ) never match Redump hashes.
/// </summary>
public abstract class DiscDatTitleInfoProvider<T> : ITitleInfoProvider<T>, IHasDataFingerprint, IDatSource where T : GamingConsole
{

    public string Source { get; set; } = "Redump DAT";

    public static bool Enabled => DiscHashing.Enabled;

    /// <summary>True for DAT file names that belong to this console.</summary>
    protected abstract bool MatchesDat(string fileName);

    // Turning hashing on or off, or changing the DATs, re-parses the console's cached files.
    public string DataFingerprint => Enabled ? DatIndex.Fingerprint(MatchesDat) : "";

    public DatIndex? Index => Enabled ? DatIndex.ForConsole(MatchesDat) : null;

    public Task LoadTitleDatabaseAsync() => Task.CompletedTask;

    public Task<Rom> GetTitleInfo(Rom rom) => GetTitleInfo(rom, CancellationToken.None);

    // Hashing a DVD image takes minutes: a cancelled scan stops mid-file, and nothing partial is cached.
    public async Task<Rom> GetTitleInfo(Rom rom, CancellationToken ct)
    {
        if (!Enabled || rom.Path is not { } path || !File.Exists(path)) return rom;
        if (FileToHash(path) is not { } file || !File.Exists(file)) return rom;
        if (DatIndex.ForConsole(MatchesDat) is not { } index) return rom;

        var (sha1, crc, size) = HashCache.Default is { } cache
            ? await cache.GetOrComputeAsync(file, f => HashAsync(f, ct))
            : await HashAsync(file, ct);
        if (index.Find(sha1, crc, size) is { } hit)
            DatTitleInfoProvider<T>.Apply(rom, hit, sha1, crc);
        return rom;
    }

    /// <summary>The image itself, or the first track file of a .cue/.gdi set; null for formats Redump cannot match.</summary>
    public static string? FileToHash(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? "";
        string? Track(string? name) => name is null ? null : Path.Combine(dir, name);
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".iso" or ".gcm" => path,
            ".cue" => Track(FirstCueFile(File.ReadLines(path))),
            ".gdi" => Track(FirstGdiFile(File.ReadLines(path))),
            _ => null,
        };
    }

    /// <summary>First <c>FILE "name" BINARY</c> entry of a cue sheet.</summary>
    public static string? FirstCueFile(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (!line.StartsWith("FILE ", StringComparison.OrdinalIgnoreCase)) continue;
            var rest = line[5..].Trim();
            if (rest.StartsWith('"'))
            {
                var end = rest.IndexOf('"', 1);
                return end > 1 ? rest[1..end] : null;
            }
            return rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        return null;
    }

    /// <summary>Track 1 file of a GDI: "&lt;track&gt; &lt;lba&gt; &lt;type&gt; &lt;sector size&gt; &lt;file&gt; &lt;offset&gt;", file possibly quoted.</summary>
    public static string? FirstGdiFile(IEnumerable<string> lines)
    {
        foreach (var raw in lines.Skip(1))
        {
            var line = raw.Trim();
            if (!line.StartsWith("1 ", StringComparison.Ordinal)) continue;
            var quote = line.IndexOf('"');
            if (quote >= 0)
            {
                var end = line.IndexOf('"', quote + 1);
                return end > quote ? line[(quote + 1)..end] : null;
            }
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 5 ? parts[4] : null;
        }
        return null;
    }

    /// <summary>SHA-1 and CRC32 of the whole file in one streaming pass.</summary>
    public static async Task<(string Sha1, string Crc, long Size)> HashAsync(string path, CancellationToken ct = default)
    {
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        var crc = new System.IO.Hashing.Crc32();
        var buffer = ArrayPool<byte>.Shared.Rent(1 << 20);
        try
        {
            await using var stream = FileUtils.OpenRead(path);
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                sha1.AppendData(buffer, 0, read);
                crc.Append(buffer.AsSpan(0, read));
            }
            var crcValue = crc.GetCurrentHashAsUInt32();
            return (Convert.ToHexStringLower(sha1.GetHashAndReset()), crcValue.ToString("x8"), stream.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

/// <summary>Whether disc images are hashed for Redump matching: the user setting, else config.json.</summary>
public static class DiscHashing
{
    /// <summary>Set by the app from settings.json; null falls back to <c>Dats:HashDiscImages</c>.</summary>
    public static bool? UserOverride { get; set; }

    public static bool Enabled => UserOverride
        ?? RomManagerConfiguration.Options.Dats.HashDiscImages;
}
