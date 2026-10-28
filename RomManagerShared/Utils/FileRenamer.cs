using Microsoft.Extensions.Logging;
using RomManagerShared.Base;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RomManagerShared.Utils;

/// <summary>Result of a single rename operation (real or dry-run).</summary>
public sealed record RenameResult(
    string SourcePath,
    string DestinationPath,
    bool Success,
    bool WasDryRun,
    string? ErrorMessage = null)
{
    public bool Changed => !string.Equals(SourcePath, DestinationPath, StringComparison.Ordinal);
}

public partial class FileRenamer
{
    // Invalid on Windows and FAT/exFAT (SD cards), whatever OS renames the file: ROMs get copied to those.
    private static readonly char[] _invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*',
        .. Enumerable.Range(0, 32).Select(i => (char)i)];

    // Windows device names: "CON.nsp" cannot be opened there.
    private static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private const int MaxFileNameLength = 255;

    /// <summary>The error a rename reports when the template gives an empty name.</summary>
    public const string EmptyNameError = "The template gives an empty file name";

    /// <summary>The error a rename reports when the template has a placeholder the rom does not know.</summary>
    public const string UnknownPlaceholderError = "Unknown placeholder";

    /// <summary>Placeholders every rom understands (the rename dialog shows these).</summary>
    public static readonly IReadOnlyList<string> GeneralPlaceholders =
    [
        "{TitleName}", "{TitleID}", "{Region}", "{Version}", "{Publisher}", "{Developer}", "{ReleaseDate}", "{Year}",
        "{Platform}", "{ProductCode}", "{Size}", "{Genres}", "{Players}", "{Languages}", "{Type}", "{MinFirmware}",
        "{DLCCount}", "{FileName}",
    ];

    // Spellings of the general placeholders that the grid uses as column names ("Product Code", "Min Firmware"...).
    private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Title"] = "TitleName", ["Name"] = "TitleName", ["MinimumFirmware"] = "MinFirmware",
        ["NumberOfPlayers"] = "Players", ["RomType"] = "Type", ["Regions"] = "Region",
    };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Preview renames without touching any files.
    /// </summary>
    public static IReadOnlyList<RenameResult> PreviewRenames(IEnumerable<Rom> romList, string formatString)
        => RenameCore(romList, formatString, dryRun: true, logger: null);

    /// <summary>
    /// Rename files synchronously.  Prefer the async overload for UI use.
    /// </summary>
    public static IReadOnlyList<RenameResult> RenameFiles(
        IEnumerable<Rom> romList,
        string formatString,
        ILogger? logger = null)
        => RenameCore(romList, formatString, dryRun: false, logger);

    /// <summary>
    /// Rename files asynchronously with cancellation support.
    /// </summary>
    public static Task<IReadOnlyList<RenameResult>> RenameFilesAsync(
        IEnumerable<Rom> romList,
        string formatString,
        ILogger? logger = null,
        CancellationToken ct = default)
        => Task.Run(() => RenameCore(romList, formatString, dryRun: false, logger, ct), ct);

    /// <summary>What <see cref="RenameFilesAsync"/> would do, without touching any file.</summary>
    public static IReadOnlyList<RenameResult> PreviewRename(IEnumerable<Rom> romList, string formatString)
        => RenameCore(romList, formatString, dryRun: true, logger: null);

    // ── Core implementation ───────────────────────────────────────────────

    private static IReadOnlyList<RenameResult> RenameCore(
        IEnumerable<Rom> romList,
        string formatString,
        bool dryRun,
        ILogger? logger,
        CancellationToken ct = default)
    {
        var results = new List<RenameResult>();

        if (romList is null)
        {
            logger?.LogWarning("RenameFiles: romList was null");
            return results;
        }

        // Folder-format roms are renamed by their game folder: renaming the marker file (code/app.xml,
        // sce_sys/param.sfo, default.xbe) would break the game.
        var romGroups = romList
            .Where(r => !string.IsNullOrEmpty(r.Path))
            .GroupBy(r => r.IsFolderFormat ? RomUtils.GetRomBaseFolder(r) : r.Path!, FileUtils.PathComparer);

        foreach (var group in romGroups)
        {
            ct.ThrowIfCancellationRequested();
            var result = Directory.Exists(group.Key)
                ? ProcessFolder(group.Key, group.ToList(), formatString, dryRun, logger)
                : ProcessGroup(group.ToList(), formatString, dryRun, logger);
            results.Add(result);
        }

        return results;
    }

    private static RenameResult ProcessFolder(string folder, IList<Rom> roms, string formatString, bool dryRun, ILogger? logger)
    {
        if (UnknownPlaceholders(roms[0], formatString) is [var unknown, ..])
            return new RenameResult(folder, folder, false, dryRun, $"{UnknownPlaceholderError} {unknown}");
        // The template's extension comes from the marker file; folders have none.
        var name = SanitizeFileName(Path.GetFileNameWithoutExtension(GenerateFileName(roms, formatString)));
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(folder)) ?? "";
        if (name.Length == 0)
            return new RenameResult(folder, folder, false, dryRun, EmptyNameError);
        if (string.Equals(name, Path.GetFileName(folder), StringComparison.Ordinal))
            return new RenameResult(folder, folder, true, dryRun);

        var target = Path.Combine(parent, name);
        for (int i = 1; Directory.Exists(target) || File.Exists(target); i++)
            target = Path.Combine(parent, $"{name} ({i})");
        if (dryRun)
            return new RenameResult(folder, target, true, WasDryRun: true);
        try
        {
            Directory.Move(folder, target);
            foreach (var rom in roms)
                rom.Path = Path.Combine(target, Path.GetRelativePath(folder, rom.Path!));
            logger?.LogInformation("Renamed folder '{Source}' → '{Dest}'", folder, target);
            return new RenameResult(folder, target, true, false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to rename folder '{Folder}'", folder);
            return new RenameResult(folder, target, false, false, ex.Message);
        }
    }

    private static RenameResult ProcessGroup(
        IList<Rom> romGroup,
        string formatString,
        bool dryRun,
        ILogger? logger)
    {
        // BUG FIX: romGroup.First().Path is already absolute — do NOT combine with directory
        string sourcePath = romGroup.First().Path!;
        if (UnknownPlaceholders(romGroup[0], formatString) is [var unknown, ..])
            return new RenameResult(sourcePath, sourcePath, false, dryRun, $"{UnknownPlaceholderError} {unknown}");
        string newFileNameSanitized = SanitizeFileName(GenerateFileName(romGroup, formatString));

        // A template whose placeholders are all empty would leave just the extension (".nsp").
        if (Path.GetFileNameWithoutExtension(newFileNameSanitized).Length == 0)
            return new RenameResult(sourcePath, sourcePath, false, dryRun, EmptyNameError);

        // Nothing to do
        if (string.Equals(newFileNameSanitized, Path.GetFileName(sourcePath), StringComparison.Ordinal))
            return new RenameResult(sourcePath, sourcePath, true, dryRun);

        string directory = Path.GetDirectoryName(sourcePath) ?? "";
        string newFilePath = BuildUniqueDestPath(directory, newFileNameSanitized, sourcePath);

        if (dryRun)
            return new RenameResult(sourcePath, newFilePath, true, WasDryRun: true);

        try
        {
            File.Move(sourcePath, newFilePath);
            logger?.LogInformation("Renamed '{Source}' → '{Dest}'", Path.GetFileName(sourcePath), Path.GetFileName(newFilePath));

            foreach (var rom in romGroup)
                rom.Path = newFilePath;

            return new RenameResult(sourcePath, newFilePath, true, false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to rename '{Source}'", sourcePath);
            return new RenameResult(sourcePath, newFilePath, false, false, ex.Message);
        }
    }

    // ── File name generation ──────────────────────────────────────────────

    private static readonly string[] _versionPlaceholders    = ["{Version}"];
    private static readonly string[] _dlcPlaceholders        = ["{DLCCount}"];

    /// <summary>
    /// Supported placeholders:
    ///   {TitleName}, {TitleID}, {Region}, {Version}, {DLCCount},
    ///   {Publisher}, {Developer}, {ReleaseDate}, {Platform}, {ProductCode},
    ///   {Size}, {Genres}, {Players}, {Type}, {MinFirmware}, {Description}, {Year}, {Languages}, {FileName}
    /// plus the console-specific properties of the rom's type ({Firmware}...). Names are case-insensitive and may
    /// have spaces ("{Product Code}"). Bracket variants [v{Version}], [d{DLCCount}], [{Region}] are stripped when empty.
    /// </summary>
    public static string GenerateFileName(IEnumerable<Rom> romGroup, string formatString)
    {
        var romList = romGroup as IList<Rom> ?? romGroup.ToList();
        Rom rom = romList.First();
        string fn = NormalizeTemplate(formatString);

        // Core fields
        ReplacePlaceholder(ref fn, "{TitleName}",    rom.Titles?.FirstOrDefault()?.Value ?? "");
        ReplacePlaceholder(ref fn, "{TitleID}",      rom.TitleID ?? "");
        ReplacePlaceholder(ref fn, "{ProductCode}",  rom.ProductCode ?? "");
        ReplacePlaceholder(ref fn, "{Publisher}",    rom.Publisher ?? "");
        ReplacePlaceholder(ref fn, "{Developer}",    rom.Developer ?? "");
        ReplacePlaceholder(ref fn, "{Platform}",     GetPlatformName(rom));

        // Release date — use year only when full date unavailable
        // Gregorian whatever the user's locale (a Hijri or Thai calendar would give another year).
        string relDate = rom.ReleaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        ReplacePlaceholders(ref fn, ["[{ReleaseDate}]", "({ReleaseDate})"], relDate.Length == 0 ? "" : null);
        ReplacePlaceholder(ref fn, "{ReleaseDate}", relDate);
        string year = rom.ReleaseDate?.ToString("yyyy", CultureInfo.InvariantCulture) ?? "";
        ReplacePlaceholders(ref fn, ["[{Year}]", "({Year})"], year.Length == 0 ? "" : null);
        ReplacePlaceholder(ref fn, "{Year}", year);

        // Languages ("English,French")
        string languages = rom.Languages is { Count: > 0 } langs
            ? string.Join(",", langs.Where(l => l != Language.Unknown).Distinct().Select(l => l.ToString()))
            : "";
        ReplacePlaceholders(ref fn, ["[{Languages}]", "({Languages})"], languages.Length == 0 ? "" : null);
        ReplacePlaceholder(ref fn, "{Languages}", languages);

        // Size — formatted human-readable
        string sizeStr = rom.Size.HasValue ? FileUtils.FormatFileSize(rom.Size.Value) : "";
        ReplaceOptional(ref fn, "Size", sizeStr);

        // Genres — comma-separated
        string genresStr = (rom.Genres?.Any() == true) ? string.Join(", ", rom.Genres) : "";
        ReplaceOptional(ref fn, "Genres", genresStr);

        // Players
        string playersStr = rom.NumberOfPlayers > 0 ? rom.NumberOfPlayers.ToString() : "";
        ReplaceOptional(ref fn, "Players", playersStr);

        // ROM type (Game, Update, DLC, Homebrew)
        string typeStr = rom switch { Game => "Game", Update => "Update", DLC => "DLC", Homebrew => "Homebrew", _ => "" };
        ReplaceOptional(ref fn, "Type", typeStr);

        // Min firmware
        string fwStr = rom.MinimumFirmware ?? "";
        ReplaceOptional(ref fn, "MinFirmware", fwStr);

        // Description (first available, truncated for file name safety)
        string descStr = rom.Descriptions?.FirstOrDefault()?.Value ?? "";
        if (descStr.Length > 80) descStr = descStr[..80];
        ReplaceOptional(ref fn, "Description", descStr);

        // Region — strip bracket variant when empty / unknown
        string regionStr = (rom.Regions?.Count > 0 && rom.Regions.First() != Region.Unknown)
            ? string.Join(",", rom.Regions)
            : "";
        ReplaceOptional(ref fn, "Region", regionStr);

        // Version
        string version = GetVersion(romList) ?? "";
        if (version.Length == 0)
            ReplacePlaceholders(ref fn, ["[v{Version}]", "v{Version}", "[{Version}]", "{Version}"], "");
        else
            ReplacePlaceholders(ref fn, _versionPlaceholders, version);

        // DLC count
        int dlcCount = GetDLCCount(romList);
        if (rom is not Game || dlcCount == 0)
            ReplacePlaceholders(ref fn, ["[d{DLCCount}]", "[{DLCCount}]", "d{DLCCount}", "{DLCCount}"], "");
        else
            ReplacePlaceholders(ref fn, _dlcPlaceholders, dlcCount.ToString());

        // Anything left is a console property ({Firmware}, {Category}...) or unknown (left as is).
        fn = PlaceholderRegex().Replace(fn, m =>
            ConsoleProperty(rom, m.Groups[1].Value) is { } p ? Format(p.GetValue(rom)) : m.Value);
        fn = CollapseSpacesRegex().Replace(EmptyBracketsRegex().Replace(fn, ""), " ").Trim();

        // Last, so braces in the current name are never read as placeholders.
        string fileName = Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(rom.Path ?? ""));
        ReplacePlaceholder(ref fn, "{FileName}", fileName);

        fn += Path.GetExtension(romList.First().Path ?? "");
        return fn;
    }

    /// <summary>Placeholders of the template that <paramref name="rom"/> has no value for, e.g. a typo.</summary>
    public static IReadOnlyList<string> UnknownPlaceholders(Rom rom, string formatString)
    {
        var known = GeneralPlaceholders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. PlaceholderRegex().Matches(NormalizeTemplate(formatString))
            .Select(m => m.Value)
            .Where(token => !known.Contains(token) && token != "{Description}" && ConsoleProperty(rom, token[1..^1]) is null)
            .Distinct()];
    }

    /// <summary>Placeholders for the console-specific properties of <paramref name="romTypes"/> (e.g. "{Firmware}").</summary>
    public static IReadOnlyList<string> ConsolePlaceholders(IEnumerable<Type> romTypes) =>
        [.. romTypes.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.DeclaringType != typeof(Rom) && IsSimple(p.PropertyType)
                && p.GetCustomAttribute<UiPropertyAttribute>(true) is { Visible: true })
            .Select(p => "{" + p.Name + "}")
            .Distinct()];

    // "{Product Code}" → "{ProductCode}", "{title}" → "{TitleName}": the grid's column names work too.
    private static string NormalizeTemplate(string formatString) =>
        PlaceholderRegex().Replace(formatString, m =>
        {
            var name = m.Groups[1].Value.Replace(" ", "", StringComparison.Ordinal);
            if (_aliases.TryGetValue(name, out var alias)) name = alias;
            var general = GeneralPlaceholders.Concat(["{Description}"])
                .FirstOrDefault(g => string.Equals(g[1..^1], name, StringComparison.OrdinalIgnoreCase));
            return general ?? "{" + name + "}";
        });

    // A public property of the rom's own type (below Rom) with a plain value, matched by name or display name.
    private static PropertyInfo? ConsoleProperty(Rom rom, string name) =>
        rom.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType != typeof(Rom) && p.CanRead && p.GetIndexParameters().Length == 0 && IsSimple(p.PropertyType))
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.GetCustomAttribute<UiPropertyAttribute>(true)?.DisplayName.Replace(" ", "", StringComparison.Ordinal), name, StringComparison.OrdinalIgnoreCase));

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateOnly) || type == typeof(DateTime);
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        bool b => b ? "Yes" : "",
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    // ── Helper: placeholder replacement ──────────────────────────────────

    private static void ReplacePlaceholder(ref string input, string placeholder, string replacement)
    {
        input = input.Replace(placeholder, replacement, StringComparison.Ordinal).Trim();
        // Collapse double spaces introduced by empty replacements
        input = CollapseSpacesRegex().Replace(input, " ").Trim();
    }

    // "[{X}]"/"({X})" disappear with their brackets when the value is empty; otherwise only "{X}" is replaced.
    private static void ReplaceOptional(ref string input, string name, string value)
    {
        var token = "{" + name + "}";
        if (value.Length == 0)
            ReplacePlaceholders(ref input, ["[" + token + "]", "(" + token + ")", token], "");
        else
            ReplacePlaceholder(ref input, token, value);
    }

    // A null value leaves the placeholders alone (used to drop a bracketed placeholder only when it is empty).
    private static void ReplacePlaceholders(ref string input, string[] placeholders, string? value)
    {
        if (value is null) return;
        foreach (var ph in placeholders)
            ReplacePlaceholder(ref input, ph, value);
    }

    // ── Helper: versioning + DLC ──────────────────────────────────────────

    private static string? GetVersion(IList<Rom> romGroup)
    {
        if (romGroup.Count == 1)
            return romGroup[0].Version;

        foreach (var r in romGroup)
            if (r is Update u)
                return u.Version;

        return romGroup[0].Version;
    }

    private static int GetDLCCount(IList<Rom> romGroup)
        => romGroup.Count(r => r is DLC);

    // ── Helper: platform name ─────────────────────────────────────────────

    private static string GetPlatformName(Rom rom)
    {
        // Attempt to extract from the ROM's actual type name for a readable label
        string typeName = rom.GetType().Name;
        // Strip common suffixes (Rom, Game, Update, DLC)
        return Regex.Replace(typeName, @"(Rom|Game|Update|DLC)$", "", RegexOptions.IgnoreCase);
    }

    // ── Helper: unique destination path ──────────────────────────────────

    private static string BuildUniqueDestPath(string directory, string fileName, string sourcePath)
    {
        string ext = Path.GetExtension(fileName);
        string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
        string candidate = Path.Combine(directory, fileName);
        int counter = 1;

        // BUG FIX: was `sourcePath != newFileName` (comparing full path to filename)
        // Correct: skip collision check when candidate == source (nothing to do)
        while (File.Exists(candidate) &&
               !string.Equals(candidate, sourcePath, FileUtils.PathComparison))
        {
            candidate = Path.Combine(directory, $"{nameNoExt} ({counter}){ext}");
            counter++;
        }

        return candidate;
    }

    // ── Helper: sanitize file name ────────────────────────────────────────

    /// <summary>
    /// A name that works on Windows, FAT/exFAT and Unix: invalid characters become '_', trailing dots and spaces
    /// go (Windows drops them silently), device names get a '_' and the name is cut to 255 characters with its
    /// extension kept.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        string result = new(fileName.Select(c => _invalidChars.Contains(c) ? '_' : c).ToArray());
        result = CollapseSpacesRegex().Replace(result, " ").Trim().TrimEnd('.', ' ');
        string ext = Path.GetExtension(result);
        string stem = result[..^ext.Length].TrimEnd('.', ' ');
        if (_reservedNames.Contains(stem)) stem += "_";
        if (stem.Length + ext.Length > MaxFileNameLength)
            stem = stem[..Math.Max(0, MaxFileNameLength - ext.Length)].TrimEnd('.', ' ');
        return stem + ext;
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex CollapseSpacesRegex();

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex PlaceholderRegex();

    // "[]" or "()" left by a console placeholder with no value.
    [GeneratedRegex(@"\[\s*\]|\(\s*\)")]
    private static partial Regex EmptyBracketsRegex();
}
