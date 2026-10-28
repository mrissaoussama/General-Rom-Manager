using RomManagerShared.Base;

namespace RomManagerShared.Utils.Dat;

/// <summary>How a file's own hashes compare with the console's DATs.</summary>
public enum DatVerdict
{
    /// <summary>The file has no computed hashes yet.</summary>
    NotHashed,
    /// <summary>A DAT lists exactly this file (a good dump).</summary>
    Verified,
    /// <summary>No DAT lists this file: a bad or modified dump, a hack, or a title the DAT lacks.</summary>
    NotInDat,
}

public sealed record DatCheck(Rom Rom, DatVerdict Verdict, DatEntry? Match);

/// <summary>Checks roms against DATs by the hashes computed from their files ("Local" hashes).</summary>
public static class DatVerifier
{
    public static DatCheck Verify(Rom rom, IReadOnlyList<DatIndex> indexes)
    {
        var local = rom.Hashes?.Where(h => h.Description == "Local").ToList() ?? [];
        string? Hash(HashTypeEnum type) => local.LastOrDefault(h => h.Type == type)?.Value?.ToLowerInvariant();
        var (sha1, crc) = (Hash(HashTypeEnum.SHA1), Hash(HashTypeEnum.CRC32));
        if (sha1 is null && crc is null) return new(rom, DatVerdict.NotHashed, null);
        var size = rom.Path is { } path && File.Exists(path) ? new FileInfo(path).Length : rom.Size ?? 0;
        foreach (var index in indexes)
            if (index.Find(sha1, crc, size) is { } match)
                return new(rom, DatVerdict.Verified, match);
        return new(rom, DatVerdict.NotInDat, null);
    }
}
