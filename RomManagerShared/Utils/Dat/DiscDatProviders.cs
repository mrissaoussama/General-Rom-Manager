using System.Buffers;
using System.Security.Cryptography;
using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;

namespace RomManagerShared.Utils.Dat;

/// <summary>
/// Matches whole disc images against Redump DATs. Hashing a DVD image takes a while, so this is
/// opt-in: set <c>"Dats": { "HashDiscImages": true }</c> in config.json. Only single-file images
/// (.iso/.gcm) are hashed; compressed and multi-track formats never match Redump hashes.
/// </summary>
public abstract class DiscDatTitleInfoProvider<T> : ITitleInfoProvider<T>, IHasDataFingerprint where T : GamingConsole
{
    private static readonly string[] HashedExtensions = [".iso", ".gcm"];

    public string Source { get; set; } = "Redump DAT";

    public static bool Enabled =>
        bool.TryParse(RomManagerConfiguration.Configuration.GetSection("Dats:HashDiscImages").Value, out var v) && v;

    /// <summary>True for DAT file names that belong to this console.</summary>
    protected abstract bool MatchesDat(string fileName);

    // Turning hashing on or off, or changing the DATs, re-parses the console's cached files.
    public string DataFingerprint => Enabled ? DatIndex.Fingerprint(MatchesDat) : "";

    public Task LoadTitleDatabaseAsync() => Task.CompletedTask;

    public async Task<Rom> GetTitleInfo(Rom rom)
    {
        if (!Enabled || rom.Path is not { } path || !File.Exists(path)) return rom;
        if (!HashedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) return rom;
        if (DatIndex.ForConsole(MatchesDat) is not { } index) return rom;

        var (sha1, crc, size) = await HashAsync(path);
        if (index.Find(sha1, crc, size) is { } hit)
            DatTitleInfoProvider<T>.Apply(rom, hit, sha1, crc);
        return rom;
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

// Redump DAT file names: "<Company> - <System> - Datfile (...)".

public class PS2RedumpProvider : DiscDatTitleInfoProvider<PS2Console>
{
    protected override bool MatchesDat(string f) => f.Contains("Sony - PlayStation 2", StringComparison.OrdinalIgnoreCase);
}

public class PSPRedumpProvider : DiscDatTitleInfoProvider<PSPConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Sony - PlayStation Portable", StringComparison.OrdinalIgnoreCase);
}

public class GameCubeRedumpProvider : DiscDatTitleInfoProvider<GameCubeConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Nintendo - GameCube", StringComparison.OrdinalIgnoreCase);
}

public class WiiRedumpProvider : DiscDatTitleInfoProvider<WiiConsole>
{
    protected override bool MatchesDat(string f) =>
        f.Contains("Nintendo - Wii", StringComparison.OrdinalIgnoreCase) && !f.Contains("Wii U", StringComparison.OrdinalIgnoreCase);
}
