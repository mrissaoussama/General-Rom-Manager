// AvaloniaApplication1/AvaloniaUI/Utils/Settings.cs

using System.Text.Json;

namespace AvaloniaUI.Utils;

public class PluginSetting
{
    public string PluginName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class Settings
{

    // ── Column Visibility ─────────────────────────────────────────────────
    public Dictionary<string, List<string>> VisibleColumns { get; set; } = new();

    /// <summary>
    /// Raised when the column visibility list for a console changes.
    /// The argument is the console name whose columns were modified.
    /// Subscribers should update their DataGrids on the UI thread.
    /// </summary>
    public static event Action<string>? ColumnsChanged;

    /// <summary>
    /// Invokes <see cref="ColumnsChanged"/> for <paramref name="consoleName"/>.
    /// Call this from external code (e.g. SettingsWindow) after modifying column visibility.
    /// </summary>
    public static void RaiseColumnsChanged(string consoleName) =>
        ColumnsChanged?.Invoke(consoleName);

    /// <summary>Raised when compact mode is toggled.</summary>
    public static event Action? CompactModeChanged;
    public static void RaiseCompactModeChanged() => CompactModeChanged?.Invoke();

    // ── Library Paths ─────────────────────────────────────────────────────
    /// <summary>Root directory for database / metadata storage.</summary>
    public string? DatabasePath { get; set; }
    /// <summary>Root directories to scan per console (Key = console name).</summary>
    public Dictionary<string, string> ConsoleLibraryPaths { get; set; } = new();

    /// <summary>The first-run wizard was finished or closed; it is not shown again.</summary>
    public bool OnboardingCompleted { get; set; }

    /// <summary>More ROM folders per console id, scanned together with the primary one.</summary>
    public Dictionary<string, List<string>> ConsoleExtraLibraryPaths { get; set; } = new();
    /// <summary>Per console id: emulator executable and arguments template (see EmulatorLauncher).</summary>
    public Dictionary<string, EmulatorSettings> Emulators { get; set; } = new();

    /// <summary>Paths of user-supplied conversion tools by <see cref="ConversionTool.Id"/>; unset: Tools folder.</summary>
    public Dictionary<string, string> ToolPaths { get; set; } = new();

    public string ToolPath(ConversionTool tool) => tool.ResolvePath(ToolPaths.GetValueOrDefault(tool.Id));

    /// <summary>Rescan every console this often (hours); 0 = only on start, on demand and when the watchers fire.</summary>
    public int RescanIntervalHours { get; set; }

    /// <summary>Per-console scan settings, keyed by console id.</summary>
    public Dictionary<string, ConsoleScanSettings> ScanSettings { get; set; } = new();

    public ScanOptions GetScanOptions(string consoleId, string root) =>
        ScanSettings.TryGetValue(consoleId, out var s)
            ? ScanOptions.For(root, s.Parallelism, s.ExcludedFolders)
            : ScanOptions.For(root, null, null);

    // ── File Organiser ────────────────────────────────────────────────────
    /// <summary>Default rename format string, e.g. "{TitleName} [{Region}] [v{Version}]".</summary>
    public string RenameFormatString { get; set; } = "{TitleName} [{Region}] [v{Version}]";

    /// <summary>1G1R region priority, e.g. "USA, World, Europe, Japan".</summary>
    public string RegionPriority { get; set; } = "USA, World, Europe, Japan";

    // ── Plugins ───────────────────────────────────────────────────────────
    public List<PluginSetting> PluginSettings { get; set; } = [];
    /// <summary>Directory scanned for plugin DLLs.</summary>
    public string PluginsDirectory { get; set; } = "plugins";

    // ── UI preferences ────────────────────────────────────────────────────
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public bool WindowMaximized { get; set; }

    /// <summary>
    /// Application colour theme: "Default" (follows OS), "Dark", or "Light".
    /// </summary>
    public string Theme { get; set; } = "Default";

    /// <summary>
    /// When <see langword="true"/> the DataGrids use a smaller row height and
    /// tighter padding to fit more rows on screen at once.
    /// </summary>
    public bool IsCompactMode { get; set; } = false;

    /// <summary>
    /// Whether the download-manager panel at the bottom of the window is expanded.
    /// </summary>
    public bool IsDownloadPanelExpanded { get; set; } = true;

    /// <summary>
    /// When true, auto-download missing database files on startup. Default: false.
    /// </summary>
    public bool AutoDownloadMissingDBs { get; set; } = false;

    /// <summary>Ask GitHub for a newer RomManager at startup (installed copies only). Off by default: it is a network call.</summary>
    public bool CheckForAppUpdates { get; set; }

    /// <summary>Console module options (Settings → Consoles), keyed by the module's options type name.</summary>
    public Dictionary<string, Dictionary<string, string>> ConsoleOptions { get; set; } = new();

    /// <summary>Pre-module setting, moved into <see cref="ConsoleOptions"/> on load.</summary>
    public bool AllowPs4OnlineUpdateCheck { get; set; }

    /// <summary>Download box art (libretro-thumbnails, title databases) for the cover view and details.</summary>
    public bool DownloadBoxArt { get; set; } = true;
    /// <summary>Hash whole disc images for Redump matching; null = use config.json.</summary>
    public bool? HashDiscImages { get; set; }

    // ── Config URL / path overrides (user can override config.json defaults) ──
    // Pre-module settings, moved into ConsoleOptions["SwitchOptions"] on load.
    /// <summary>Override for Switch TitleDB base URL.</summary>
    public string? SwitchTitleDBUrlOverride { get; set; }
    /// <summary>Override for Switch versions URL.</summary>
    public string? SwitchVersionsUrlOverride { get; set; }
    /// <summary>Override for Switch TitleDB local path (relative to BaseFolder).</summary>
    public string? SwitchTitleDBPathOverride { get; set; }

    // ── Storage ──────────────────────────────────────────────────────────
    /// <summary>Where settings live; the app's store unless replaced (tests).</summary>
    public static ISettingsStore Store { get; set; } = new JsonSettingsStore(JsonSettingsStore.DefaultPath());

    public static Settings Instance => Store.Current;

    /// <summary>Re-reads the settings file.</summary>
    public static Settings Load() => Store.Reload();

    /// <summary>Saves this object (normally <see cref="Instance"/>) to the store.</summary>
    public void Save() => Store.Save(this);

    public bool IsPluginEnabled(string pluginName)
        => PluginSettings.FirstOrDefault(p =>
               string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase))
           ?.Enabled ?? true;

    public void SetPluginEnabled(string pluginName, bool enabled)
    {
        var existing = PluginSettings.FirstOrDefault(p =>
            string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            existing.Enabled = enabled;
        else
            PluginSettings.Add(new PluginSetting { PluginName = pluginName, Enabled = enabled });
        Save();
    }
}

public sealed class EmulatorSettings
{
    public string Path { get; set; } = "";
    public string Arguments { get; set; } = RomManagerShared.Utils.EmulatorLauncher.DefaultArguments;
}

/// <summary>Scan settings for one console's ROM folder.</summary>
public class ConsoleScanSettings
{
    /// <summary>Files parsed at once; null: automatic (1 on network drives, otherwise 4).</summary>
    public int? Parallelism { get; set; }
    /// <summary>Folder names or relative paths to skip.</summary>
    public List<string> ExcludedFolders { get; set; } = [];
}

/// <summary>Stores and applies <see cref="IConsoleOptions"/> values (Settings → Consoles).</summary>
public static class ConsoleOptionsStore
{
    public static string Key(IConsoleOptions options) => options.GetType().Name;

    /// <summary>Stored values with defaults for anything unset.</summary>
    public static IReadOnlyDictionary<string, string> Values(IConsoleOptions options, Settings settings)
    {
        var stored = settings.ConsoleOptions.GetValueOrDefault(Key(options));
        return options.Options.ToDictionary(d => d.Key, d => stored?.GetValueOrDefault(d.Key) ?? d.Default);
    }

    public static void ApplyAll(IEnumerable<IConsoleOptions> all, Settings settings)
    {
        foreach (var options in all)
        {
            try { options.Apply(Values(options, settings)); }
            catch (Exception ex) { AppLog.Warn($"[{Key(options)}] {ex.Message}"); }
        }
    }

    /// <summary>Moves settings from before console modules (Switch overrides, PS4 online check) into ConsoleOptions.</summary>
    public static bool MigrateLegacy(Settings s)
    {
        var changed = false;
        if (s.SwitchTitleDBUrlOverride is not null || s.SwitchVersionsUrlOverride is not null || s.SwitchTitleDBPathOverride is not null)
        {
            var sw = s.ConsoleOptions.TryGetValue("SwitchOptions", out var v) ? v : s.ConsoleOptions["SwitchOptions"] = [];
            if (s.SwitchTitleDBUrlOverride is { } url) sw.TryAdd("TitleDbUrl", url);
            if (s.SwitchVersionsUrlOverride is { } versions) sw.TryAdd("VersionsUrl", versions);
            if (s.SwitchTitleDBPathOverride is { } path) sw.TryAdd("TitleDbPath", path);
            s.SwitchTitleDBUrlOverride = s.SwitchVersionsUrlOverride = s.SwitchTitleDBPathOverride = null;
            changed = true;
        }
        if (s.AllowPs4OnlineUpdateCheck)
        {
            (s.ConsoleOptions.TryGetValue("PS4Options", out var v) ? v : s.ConsoleOptions["PS4Options"] = []).TryAdd("OnlineUpdateCheck", "true");
            s.AllowPs4OnlineUpdateCheck = false;
            changed = true;
        }
        return changed;
    }
}
