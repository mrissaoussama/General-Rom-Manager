using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base.Database;

namespace RomManagerShared.Base.Library;

/// <summary>
/// Reads, applies and writes <see cref="RomMetadataOverride"/>s. Scans and cache loads call
/// <see cref="ApplyAsync"/>; plugins write through <see cref="SetAsync"/> (see <c>IPluginContext.UpdateMetadataAsync</c>).
/// </summary>
public sealed class MetadataOverrideStore(AppDbContext db)
{
    private const int MaxCoverBytes = 16 * 1024 * 1024;

    /// <summary>The title ID (upper case), or <c>path:&lt;file&gt;</c> for roms without one.</summary>
    public static string RomKey(Rom rom) =>
        !string.IsNullOrWhiteSpace(rom.TitleID) ? rom.TitleID.Trim().ToUpperInvariant() : "path:" + rom.Path;

    /// <summary>The console a rom belongs to, as stored in <see cref="LibraryFile.Console"/>; empty when unknown.</summary>
    public static string ConsoleOf(Rom rom) => ConsoleModules.ConsoleOf(rom.GetType())?.Name ?? "";

    /// <summary>Applies the console's overrides to <paramref name="roms"/> in place.</summary>
    public async Task ApplyAsync(string console, IEnumerable<Rom> roms, CancellationToken ct = default)
    {
        var overrides = await db.RomMetadataOverrides.AsNoTracking().Where(o => o.Console == console).ToListAsync(ct);
        if (overrides.Count == 0) return;
        var byKey = overrides.ToDictionary(o => (o.RomKey, o.RomType));
        foreach (var rom in roms)
            if (byKey.TryGetValue((RomKey(rom), rom.GetType().FullName!), out var o))
                Apply(rom, o);
    }

    /// <summary>Puts the override's values on the rom (the title and description go first in their lists).</summary>
    public static void Apply(Rom rom, RomMetadataOverride o)
    {
        if (!string.IsNullOrWhiteSpace(o.Title))
            rom.Titles = [new RomTitle { Value = o.Title }, .. (rom.Titles ?? []).Where(t => t.Value != o.Title)];
        if (!string.IsNullOrWhiteSpace(o.Description))
            rom.Descriptions = [new RomDescription { Value = o.Description }, .. (rom.Descriptions ?? []).Where(d => d.Value != o.Description)];
        if (o.Publisher is not null) rom.Publisher = o.Publisher;
        if (o.Developer is not null) rom.Developer = o.Developer;
        if (o.ReleaseDate is not null) rom.ReleaseDate = o.ReleaseDate;
        if (!string.IsNullOrEmpty(o.Genres))
            rom.Genres = [.. o.Genres.Split(',').Select(g => Enum.TryParse<GenreEnum>(g, out var e) ? e : GenreEnum.Unknown).Where(g => g != GenreEnum.Unknown)];
        if (o.CoverPath is not null && File.Exists(o.CoverPath)) rom.Icon = o.CoverPath;
    }

    /// <summary>
    /// Merges <paramref name="metadata"/> into the rom's override (creating it) and applies it to the rom.
    /// Returns the override before (null when there was none) and after, for <see cref="RestoreAsync"/>.
    /// </summary>
    public async Task<(RomMetadataOverride? Before, RomMetadataOverride After)> SetAsync(Rom rom, RomMetadata metadata,
        string? source, string coversDirectory, CancellationToken ct = default)
    {
        var console = ConsoleOf(rom);
        var key = RomKey(rom);
        var type = rom.GetType().FullName!;
        var row = await db.RomMetadataOverrides.FirstOrDefaultAsync(o => o.Console == console && o.RomKey == key && o.RomType == type, ct);
        var before = row?.Copy();
        if (row is null)
        {
            row = new RomMetadataOverride { Console = console, RomKey = key, RomType = type };
            db.RomMetadataOverrides.Add(row);
        }

        if (metadata.Title is not null) row.Title = metadata.Title.Trim();
        if (metadata.Description is not null) row.Description = metadata.Description.Trim();
        if (metadata.Publisher is not null) row.Publisher = metadata.Publisher.Trim();
        if (metadata.Developer is not null) row.Developer = metadata.Developer.Trim();
        if (metadata.ReleaseDate is not null) row.ReleaseDate = metadata.ReleaseDate;
        if (metadata.Genres is not null) row.Genres = string.Join(",", GenreNames.Parse(metadata.Genres));
        if (metadata.CoverImage is not null) row.CoverPath = await SaveCoverAsync(metadata.CoverImage, coversDirectory, ct);
        row.Source = source;
        row.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        Apply(rom, row);
        return (before, row.Copy());
    }

    /// <summary>Puts back the override as it was before a <see cref="SetAsync"/> (deletes it when there was none).</summary>
    public async Task RestoreAsync(RomMetadataOverride after, RomMetadataOverride? before, CancellationToken ct = default)
    {
        var row = await db.RomMetadataOverrides.FirstOrDefaultAsync(
            o => o.Console == after.Console && o.RomKey == after.RomKey && o.RomType == after.RomType, ct);
        if (row is null && before is null) return;
        if (before is null)
            db.RomMetadataOverrides.Remove(row!);
        else if (row is null)
        {
            var restored = before.Copy();
            restored.Id = 0;
            db.RomMetadataOverrides.Add(restored);
        }
        else
            db.Entry(row).CurrentValues.SetValues(before);
        await db.SaveChangesAsync(ct);
    }

    private static async Task<string> SaveCoverAsync(byte[] image, string directory, CancellationToken ct)
    {
        if (image.Length > MaxCoverBytes) throw new ArgumentException($"Cover image is larger than {MaxCoverBytes / 1024 / 1024} MB");
        var extension = ImageExtension(image) ?? throw new ArgumentException("Cover image must be PNG, JPEG, WebP or BMP");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(image))[..32].ToLowerInvariant() + extension);
        if (!File.Exists(path)) await File.WriteAllBytesAsync(path, image, ct);
        return path;
    }

    private static string? ImageExtension(ReadOnlySpan<byte> b) =>
        b.StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G']) ? ".png"
        : b.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]) ? ".jpg"
        : b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8) ? ".webp"
        : b.StartsWith("BM"u8) ? ".bmp"
        : null;
}

/// <summary>Maps genre names from metadata sources onto <see cref="GenreEnum"/>.</summary>
public static class GenreNames
{
    private static readonly Dictionary<string, GenreEnum> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rpg"] = GenreEnum.RolePlaying, ["roleplaying"] = GenreEnum.RolePlaying, ["roleplayinggamesrpg"] = GenreEnum.RolePlaying,
        ["roleplayingrpg"] = GenreEnum.RolePlaying, ["jrpg"] = GenreEnum.RolePlaying,
        ["platform"] = GenreEnum.Platformer, ["platformers"] = GenreEnum.Platformer,
        ["shootemup"] = GenreEnum.Shooter, ["fps"] = GenreEnum.Shooter, ["firstpersonshooter"] = GenreEnum.Shooter,
        ["racinggames"] = GenreEnum.Racing, ["driving"] = GenreEnum.Racing,
        ["boardgames"] = GenreEnum.Board, ["cardgames"] = GenreEnum.Card,
        ["musicrhythm"] = GenreEnum.Music, ["rhythm"] = GenreEnum.Music,
        ["beatemup"] = GenreEnum.Fighting, ["educational"] = GenreEnum.Educational, ["family"] = GenreEnum.Family,
    };

    /// <summary>The genres among <paramref name="names"/> that the app knows, each once, as enum names.</summary>
    public static IReadOnlyList<string> Parse(IEnumerable<string> names) =>
        [.. names.Select(Map).Where(g => g != GenreEnum.Unknown).Distinct().Select(g => g.ToString())];

    public static GenreEnum Map(string name)
    {
        var key = new string([.. name.Where(char.IsLetterOrDigit)]);
        if (key.Length == 0) return GenreEnum.Unknown;
        if (Aliases.TryGetValue(key, out var alias)) return alias;
        if (!key.All(char.IsDigit) && Enum.TryParse<GenreEnum>(key, ignoreCase: true, out var direct) && Enum.IsDefined(direct)) return direct;
        // "Sports games" → Sports, "Strategy RPG" → Strategy
        return Enum.GetValues<GenreEnum>().FirstOrDefault(g => g != GenreEnum.Unknown && key.StartsWith(g.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>The rom fields an override can change, captured so an undo can put them back in memory.</summary>
public sealed record RomFields(List<RomTitle>? Titles, List<RomDescription>? Descriptions, string? Publisher, string? Developer,
    DateOnly? ReleaseDate, List<GenreEnum>? Genres, string? Icon)
{
    public static RomFields Capture(Rom rom) => new(rom.Titles?.ToList(), rom.Descriptions?.ToList(), rom.Publisher, rom.Developer,
        rom.ReleaseDate, rom.Genres?.ToList(), rom.Icon);

    public void Restore(Rom rom)
    {
        rom.Titles = Titles;
        rom.Descriptions = Descriptions;
        rom.Publisher = Publisher;
        rom.Developer = Developer;
        rom.ReleaseDate = ReleaseDate;
        rom.Genres = Genres;
        rom.Icon = Icon;
    }
}

/// <summary>One metadata write, enough to undo it: the rom, its fields before, and the override before and after.</summary>
public sealed record MetadataChange(Rom Rom, RomFields FieldsBefore, RomMetadataOverride? Before, RomMetadataOverride After);
