using Microsoft.Extensions.Logging;
using RomManagerShared.Base;
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
    public bool Changed => !string.Equals(SourcePath, DestinationPath, StringComparison.OrdinalIgnoreCase);
}

public partial class FileRenamer
{
    private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars();

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

        var romGroups = romList
            .Where(r => !string.IsNullOrEmpty(r.Path))
            .GroupBy(r => r.Path);

        foreach (var group in romGroups)
        {
            ct.ThrowIfCancellationRequested();
            var result = ProcessGroup(group.ToList(), formatString, dryRun, logger);
            results.Add(result);
        }

        return results;
    }

    private static RenameResult ProcessGroup(
        IList<Rom> romGroup,
        string formatString,
        bool dryRun,
        ILogger? logger)
    {
        // BUG FIX: romGroup.First().Path is already absolute — do NOT combine with directory
        string sourcePath = romGroup.First().Path!;
        string newFileName = GenerateFileName(romGroup, formatString);

        // Nothing to do
        if (string.Equals(newFileName, Path.GetFileName(sourcePath), StringComparison.OrdinalIgnoreCase))
            return new RenameResult(sourcePath, sourcePath, true, dryRun);

        string directory = Path.GetDirectoryName(sourcePath) ?? "";
        string newFileNameSanitized = SanitizeFileName(newFileName);
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
    ///   {Size}, {Genres}, {Players}, {Type}, {MinFirmware}, {Description}
    /// Bracket variants [v{Version}], [d{DLCCount}], [{Region}] are stripped when empty.
    /// </summary>
    public static string GenerateFileName(IEnumerable<Rom> romGroup, string formatString)
    {
        var romList = romGroup as IList<Rom> ?? romGroup.ToList();
        Rom rom = romList.First();
        string fn = formatString;

        // Core fields
        ReplacePlaceholder(ref fn, "{TitleName}",    rom.Titles?.FirstOrDefault()?.Value ?? "");
        ReplacePlaceholder(ref fn, "{TitleID}",      rom.TitleID ?? "");
        ReplacePlaceholder(ref fn, "{ProductCode}",  rom.ProductCode ?? "");
        ReplacePlaceholder(ref fn, "{Publisher}",    rom.Publisher ?? "");
        ReplacePlaceholder(ref fn, "{Developer}",    rom.Developer ?? "");
        ReplacePlaceholder(ref fn, "{Platform}",     GetPlatformName(rom));

        // Release date — use year only when full date unavailable
        string relDate = rom.ReleaseDate.HasValue
            ? rom.ReleaseDate.Value.ToString("yyyy-MM-dd")
            : "";
        ReplacePlaceholder(ref fn, "{ReleaseDate}", relDate);

        // Size — formatted human-readable
        string sizeStr = rom.Size.HasValue ? FileUtils.FormatFileSize(rom.Size.Value) : "";
        ReplacePlaceholders(ref fn, ["[{Size}]", "{Size}"], sizeStr);

        // Genres — comma-separated
        string genresStr = (rom.Genres?.Any() == true) ? string.Join(", ", rom.Genres) : "";
        ReplacePlaceholders(ref fn, ["[{Genres}]", "{Genres}"], genresStr);

        // Players
        string playersStr = rom.NumberOfPlayers > 0 ? rom.NumberOfPlayers.ToString() : "";
        ReplacePlaceholders(ref fn, ["[{Players}]", "{Players}"], playersStr);

        // ROM type (Game, Update, DLC, Homebrew)
        string typeStr = rom switch { Game => "Game", Update => "Update", DLC => "DLC", Homebrew => "Homebrew", _ => "" };
        ReplacePlaceholders(ref fn, ["[{Type}]", "{Type}"], typeStr);

        // Min firmware
        string fwStr = rom.MinimumFirmware ?? "";
        ReplacePlaceholders(ref fn, ["[{MinFirmware}]", "{MinFirmware}"], fwStr);

        // Description (first available, truncated for file name safety)
        string descStr = rom.Descriptions?.FirstOrDefault()?.Value ?? "";
        if (descStr.Length > 80) descStr = descStr[..80];
        ReplacePlaceholders(ref fn, ["[{Description}]", "{Description}"], descStr);

        // Region — strip bracket variant when empty / unknown
        string regionStr = (rom.Regions?.Count > 0 && rom.Regions.First() != Region.Unknown)
            ? string.Join(",", rom.Regions)
            : "";
        ReplacePlaceholders(ref fn, ["[{Region}]", "{Region}"], regionStr);

        // Version
        string version = GetVersion(romList) ?? "";
        if (version.Length == 0)
            ReplacePlaceholders(ref fn, ["[v{Version}]", "v{Version}", "[{Version}]"], "");
        else
            ReplacePlaceholders(ref fn, _versionPlaceholders, version);

        // DLC count
        int dlcCount = GetDLCCount(romList);
        if (rom is not Game || dlcCount == 0)
            ReplacePlaceholders(ref fn, ["[d{DLCCount}]", "[{DLCCount}]", "d{DLCCount}"], "");
        else
            ReplacePlaceholders(ref fn, _dlcPlaceholders, dlcCount.ToString());

        fn += Path.GetExtension(romList.First().Path ?? "");
        return fn;
    }

    // ── Helper: placeholder replacement ──────────────────────────────────

    private static void ReplacePlaceholder(ref string input, string placeholder, string replacement)
    {
        input = input.Replace(placeholder, replacement, StringComparison.Ordinal).Trim();
        // Collapse double spaces introduced by empty replacements
        input = CollapseSpacesRegex().Replace(input, " ").Trim();
    }

    private static void ReplacePlaceholders(ref string input, string[] placeholders, string value)
    {
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
               !string.Equals(candidate, sourcePath, StringComparison.OrdinalIgnoreCase))
        {
            candidate = Path.Combine(directory, $"{nameNoExt} ({counter}){ext}");
            counter++;
        }

        return candidate;
    }

    // ── Helper: sanitize file name ────────────────────────────────────────

    private static string SanitizeFileName(string fileName)
    {
        string result = new(fileName.Select(c => _invalidChars.Contains(c) ? '_' : c).ToArray());
        return CollapseSpacesRegex().Replace(result, " ").Trim();
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex CollapseSpacesRegex();
}
