using System.Security.Cryptography;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerShared.Utils.Dat;

/// <summary>
/// Names cartridge roms by hashing them against DATs in <see cref="DatIndex.DatDirectory"/>.
/// Subclasses pick their DAT files and how to strip copier headers. Adding or changing DATs changes
/// <see cref="DataFingerprint"/>, so the next scan re-parses this console's cached files.
/// </summary>
public abstract class DatTitleInfoProvider<T> : ITitleInfoProvider<T>, IHasDataFingerprint, IDatSource where T : GamingConsole
{
    /// <summary>Larger files are skipped (cartridge dumps are well below this).</summary>
    protected virtual long MaxSize => 128L * 1024 * 1024;

    public string Source { get; set; } = "DAT";

    /// <summary>True for DAT file names that belong to this console.</summary>
    protected abstract bool MatchesDat(string fileName);

    /// <summary>Byte ranges worth hashing; the whole file plus header-less variants.</summary>
    protected virtual IEnumerable<ReadOnlyMemory<byte>> Payloads(byte[] data) => [data];

    protected virtual DatIndex? GetIndex() => DatIndex.ForConsole(MatchesDat);

    public string DataFingerprint => DatIndex.Fingerprint(MatchesDat);

    public DatIndex? Index => GetIndex();

    /// <summary>libretro-thumbnails system folder (e.g. "Nintendo - Game Boy"); null disables box art URLs.</summary>
    protected virtual string? LibretroSystem(Rom rom) => null;

    /// <summary>Box art URL for a No-Intro name; libretro replaces &amp;*/:`&lt;&gt;?\| with '_'.</summary>
    public static string LibretroBoxartUrl(string system, string datName)
    {
        var file = string.Concat(datName.Select(c => "&*/:`<>?\\|".Contains(c) ? '_' : c));
        return $"https://thumbnails.libretro.com/{Uri.EscapeDataString(system)}/Named_Boxarts/{Uri.EscapeDataString(file)}.png";
    }

    public Task LoadTitleDatabaseAsync() => Task.CompletedTask;

    public async Task<Rom> GetTitleInfo(Rom rom)
    {
        if (rom.Path is not { } path || !File.Exists(path)) return rom;
        if (GetIndex() is not { } index) return rom;
        var info = new FileInfo(path);
        if (info.Length > MaxSize) return rom;

        var data = await File.ReadAllBytesAsync(path);
        foreach (var payload in Payloads(data))
        {
            var sha1 = Convert.ToHexStringLower(SHA1.HashData(payload.Span));
            var crc = System.IO.Hashing.Crc32.HashToUInt32(payload.Span).ToString("x8");
            if (index.Find(sha1, crc, payload.Length) is not { } hit) continue;
            Apply(rom, hit, sha1, crc);
            if (string.IsNullOrEmpty(rom.Thumbnail) && LibretroSystem(rom) is { } system)
                rom.Thumbnail = LibretroBoxartUrl(system, hit.Game);
            break;
        }
        return rom;
    }

    public static void Apply(Rom rom, DatEntry hit, string sha1, string crc)
    {
        var title = DatIndex.CleanTitle(hit.Game);
        rom.Titles ??= [];
        rom.Titles.RemoveAll(t => t.Value == title);
        rom.Titles.Insert(0, new RomTitle { Value = title });
        var regions = DatIndex.RegionsFromName(hit.Game);
        if (regions.Count > 0) rom.Regions = regions;
        if (string.IsNullOrEmpty(rom.ProductCode) && !string.IsNullOrEmpty(hit.Serial)) rom.ProductCode = hit.Serial;
        rom.Hashes ??= [];
        rom.Hashes.RemoveAll(h => h.Type is HashTypeEnum.SHA1 or HashTypeEnum.CRC32);
        rom.Hashes.Add(new RomHash { Type = HashTypeEnum.SHA1, Value = sha1, IsVerified = true, Description = hit.Game });
        rom.Hashes.Add(new RomHash { Type = HashTypeEnum.CRC32, Value = crc, IsVerified = true, Description = hit.Game });
    }
}
