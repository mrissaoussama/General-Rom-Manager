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
    private static readonly string _settingsPath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

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

    // ── Library Paths ─────────────────────────────────────────────────────
    /// <summary>Root directory for database / metadata storage.</summary>
    public string? DatabasePath { get; set; }
    /// <summary>Root directories to scan per console (Key = console name).</summary>
    public Dictionary<string, string> ConsoleLibraryPaths { get; set; } = new();

    // ── File Organiser ────────────────────────────────────────────────────
    /// <summary>Default rename format string, e.g. "{TitleName} [{Region}] [v{Version}]".</summary>
    public string RenameFormatString { get; set; } = "{TitleName} [{Region}] [v{Version}]";

    // ── Plugins ───────────────────────────────────────────────────────────
    public List<PluginSetting> PluginSettings { get; set; } = [];
    /// <summary>Directory scanned for plugin DLLs.</summary>
    public string PluginsDirectory { get; set; } = "plugins";

    // ── UI preferences ────────────────────────────────────────────────────
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;

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

    // ── Config URL / path overrides (user can override config.json defaults) ──
    /// <summary>Override for Switch TitleDB base URL.</summary>
    public string? SwitchTitleDBUrlOverride { get; set; }
    /// <summary>Override for Switch versions URL.</summary>
    public string? SwitchVersionsUrlOverride { get; set; }
    /// <summary>Override for Switch TitleDB local path (relative to BaseFolder).</summary>
    public string? SwitchTitleDBPathOverride { get; set; }

    // ── Singleton ────────────────────────────────────────────────────────
    public static Settings Instance { get; private set; } = Load();

    public static Settings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                Instance = settings;
                return settings;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        Instance = new Settings();
        return Instance;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

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