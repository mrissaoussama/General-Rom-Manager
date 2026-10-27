using Avalonia.Styling;
using RomManagerShared.Switch.Configuration;

namespace AvaloniaUI.ViewModels;

/// <summary>
/// The Settings dialog. Edits a draft: nothing reaches <see cref="Settings"/> until Save, and Cancel
/// discards everything (the theme preview is reverted too).
/// </summary>
public sealed partial class SettingsViewModel : DialogViewModel
{
    public static readonly IReadOnlyList<string> ThemeNames = ["Default (system)", "Light", "Dark"];
    public static readonly IReadOnlyList<string> RenamePlaceholders =
        ["{TitleName}", "{TitleID}", "{Region}", "{Version}", "{Publisher}", "{Developer}", "{ReleaseDate}", "{Platform}"];

    private readonly Settings _settings;
    private readonly Action<Settings> _persist;
    private readonly PluginManager? _plugins;
    private readonly IReadOnlyList<ConsoleViewModelBase> _consoles;
    private readonly Func<Task<int>>? _downloadTitleDatabases;
    private readonly string _initialTheme;

    public SettingsViewModel(Settings settings, PluginManager? plugins = null, IEnumerable<ConsoleViewModelBase>? consoles = null,
        Func<Task<int>>? downloadTitleDatabases = null, Action<Settings>? persist = null)
    {
        _settings = settings;
        _persist = persist ?? (s => s.Save());
        _plugins = plugins;
        _consoles = [.. consoles ?? []];
        _downloadTitleDatabases = downloadTitleDatabases;
        _initialTheme = settings.Theme;

        _themeIndex = settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        _isCompactMode = settings.IsCompactMode;
        DatabasePath = new PathField(settings.DatabasePath ?? "", "Not set (default)");
        PluginsDirectory = new PathField(settings.PluginsDirectory, "Not set");
        _regionPriority = settings.RegionPriority;
        _renameFormat = settings.RenameFormatString;
        _autoDownloadMissingDbs = settings.AutoDownloadMissingDBs;
        _hashDiscImages = RomManagerShared.Utils.Dat.DiscHashing.Enabled;
        _switchTitleDbUrl = settings.SwitchTitleDBUrlOverride ?? "";
        _switchVersionsUrl = settings.SwitchVersionsUrlOverride ?? "";
        SwitchTitleDbPath = new PathField(settings.SwitchTitleDBPathOverride ?? "", "Not set (default)");

        Consoles = [.. ConsoleModules.Descriptors.Select(d => new ConsoleSettingsRow(d, settings))];
        Tools = [.. ConversionTools.All.Select(t => new ToolPathRow(t, settings.ToolPaths.GetValueOrDefault(t.Id, "")))];
        ColumnGroups = [.. BuildColumnGroups()];
        LoadPlugins();
        DatDirectory = SafeDatDirectory();
        KeysDirectory = RomManagerShared.Switch.SwitchKeyStatus.DefaultKeysDirectory;
        _ = RefreshKeysAsync();
    }

    public override string Title => "Settings";
    public override double Width => 820;
    public override double Height => 600;
    public override bool CanResize => true;

    // ── General ───────────────────────────────────────────────────────────

    [ObservableProperty] private int _themeIndex;
    [ObservableProperty] private bool _isCompactMode;
    public PathField DatabasePath { get; }
    public PathField PluginsDirectory { get; }
    public IReadOnlyList<ConsoleSettingsRow> Consoles { get; }
    public IReadOnlyList<ToolPathRow> Tools { get; }

    // Preview the theme while the dialog is open; Cancel puts the old one back.
    partial void OnThemeIndexChanged(int value) => PreviewTheme(value);

    /// <summary>Applies the theme at <paramref name="index"/> of <see cref="ThemeNames"/> to the running app.</summary>
    public static void PreviewTheme(int index) => ApplyTheme(ThemeKey(index));

    public static string ThemeKey(int index) => index switch { 1 => "Light", 2 => "Dark", _ => "Default" };

    private static void ApplyTheme(string key)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = key switch { "Dark" => ThemeVariant.Dark, "Light" => ThemeVariant.Light, _ => ThemeVariant.Default };
    }

    [RelayCommand]
    private async Task BrowseDatabase()
    {
        if (await Dialogs.PickFolderAsync("Select database directory") is { } path) DatabasePath.Value = path;
    }

    [RelayCommand]
    private async Task BrowsePluginsDirectory()
    {
        if (await Dialogs.PickFolderAsync("Select plugins directory") is { } path) PluginsDirectory.Value = path;
    }

    [RelayCommand]
    private async Task BrowseRomFolder(ConsoleSettingsRow row)
    {
        if (await Dialogs.PickFolderAsync($"Select {row.DisplayName} ROM folder") is { } path) row.Primary.Value = path;
    }

    [RelayCommand]
    private async Task AddRomFolder(ConsoleSettingsRow row)
    {
        if (await Dialogs.PickFolderAsync($"Add a {row.DisplayName} ROM folder") is { } path)
            row.ExtraFolders = string.IsNullOrWhiteSpace(row.ExtraFolders) ? path : $"{row.ExtraFolders}; {path}";
    }

    [RelayCommand]
    private async Task BrowseEmulator(ConsoleSettingsRow row)
    {
        if (await Dialogs.PickFileAsync($"Emulator for {row.DisplayName}", "Programs", "exe", "*") is { } path) row.EmulatorPath = path;
    }

    [RelayCommand]
    private async Task BrowseTool(ToolPathRow row)
    {
        if (await Dialogs.PickFileAsync(row.Tool.DisplayName, "Programs", "exe", "*") is { } path) row.Path = path;
    }

    // ── File organiser ────────────────────────────────────────────────────

    [ObservableProperty] private string _regionPriority;
    [ObservableProperty] private string _renameFormat;
    [ObservableProperty] private int _renameCaret;

    [RelayCommand]
    private void InsertPlaceholder(string placeholder)
    {
        var pos = Math.Clamp(RenameCaret, 0, RenameFormat.Length);
        RenameFormat = RenameFormat.Insert(pos, placeholder);
        RenameCaret = pos + placeholder.Length;
    }

    // ── Downloads, DATs ───────────────────────────────────────────────────

    [ObservableProperty] private bool _autoDownloadMissingDbs;
    [ObservableProperty] private bool _hashDiscImages;
    [ObservableProperty] private string _downloadStatus = "";
    [ObservableProperty] private string _datOverview = "";
    public string DatDirectory { get; }
    public bool CanDownloadTitleDatabases => _downloadTitleDatabases is not null;

    private static string SafeDatDirectory()
    {
        try { return RomManagerShared.Utils.Dat.DatIndex.DatDirectory; }
        catch { return ""; }
    }

    [RelayCommand]
    private void OpenDatFolder()
    {
        Directory.CreateDirectory(DatDirectory);
        Shell.OpenWithDefaultApp(DatDirectory);
    }

    // Loading DATs parses their XML, so only on request.
    [RelayCommand]
    private async Task ShowDatOverview()
    {
        DatOverview = "Loading DATs...";
        var lines = await Task.Run(() => _consoles.Select(c =>
        {
            var indexes = c.GetDatIndexes();
            return indexes.Count == 0
                ? $"{c.DisplayName}: no DAT"
                : $"{c.DisplayName}: {indexes.Count} DAT set(s), {indexes.Sum(i => i.Games.Count):N0} games";
        }).ToList());
        DatOverview = lines.Count == 0 ? "No console folders configured." : string.Join(Environment.NewLine, lines);
    }

    [RelayCommand]
    private async Task DownloadTitleDatabases()
    {
        if (_downloadTitleDatabases is null) return;
        DownloadStatus = "Checking…";
        try
        {
            var queued = await _downloadTitleDatabases();
            DownloadStatus = queued switch { < 0 => "Check failed (see log)", 0 => "All present", _ => $"Queued {queued} download(s)" };
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Settings] Title DB download failed: {ex.Message}");
            DownloadStatus = "Check failed (see log)";
        }
    }

    // ── Switch keys and overrides ─────────────────────────────────────────

    public string KeysDirectory { get; }
    public ObservableCollection<StatusLine> KeyStatus { get; } = [];
    [ObservableProperty] private string _switchTitleDbUrl;
    [ObservableProperty] private string _switchVersionsUrl;
    public PathField SwitchTitleDbPath { get; }

    // Only counts are shown, never key values.
    [RelayCommand]
    private async Task RefreshKeys()
    {
        RomManagerShared.Switch.SwitchKeyStatus status;
        try { status = await Task.Run(RomManagerShared.Switch.SwitchKeyStatus.ReadDefault); }
        catch (Exception ex)
        {
            AppLog.Warn($"[Settings] Key status check failed: {ex.GetType().Name}");
            return;
        }
        KeyStatus.Clear();
        var prod = status.ProdKeys;
        if (!prod.Exists) KeyStatus.Add(StatusLine.Bad("prod.keys: Not found (required for NSP/XCI)"));
        else if (prod.Error is not null) KeyStatus.Add(StatusLine.Bad($"prod.keys: Found, could not be read ({prod.Error})"));
        else
        {
            var master = status.HighestMasterKey is { } mk
                ? $", highest master_key_{mk:x2}" + (status.FirmwareHint is { } fw ? $" (firmware {fw}+)" : "")
                : ", no master keys";
            var text = $"prod.keys: Found, {prod.KeyCount} keys{master}";
            KeyStatus.Add(prod.KeyCount > 0 ? StatusLine.Ok(text) : StatusLine.Bad(text));
        }
        var title = status.TitleKeys;
        if (!title.Exists) KeyStatus.Add(StatusLine.Neutral("title.keys: Not found (optional)"));
        else if (title.Error is not null) KeyStatus.Add(StatusLine.Bad($"title.keys: Found, could not be read ({title.Error})"));
        else KeyStatus.Add(StatusLine.Ok($"title.keys: Found, {title.KeyCount} title keys"));
    }

    private Task RefreshKeysAsync() => RefreshKeysCommand.ExecuteAsync(null);

    // ── Columns ───────────────────────────────────────────────────────────

    public IReadOnlyList<ColumnGroup> ColumnGroups { get; }
    public bool HasColumnGroups => ColumnGroups.Count > 0;

    private IEnumerable<ColumnGroup> BuildColumnGroups()
    {
        foreach (var console in _consoles.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var visible = _settings.VisibleColumns.GetValueOrDefault(console.ConsoleName) ?? [.. console.DefaultColumns];
            yield return new ColumnGroup(console.ConsoleName, console.DisplayName, console.DefaultColumns,
                [.. console.GetColumnDescriptors().Select(d => new ColumnToggle(d.PropName, d.Header, visible.Contains(d.PropName)))]);
        }
    }

    // ── Plugins ───────────────────────────────────────────────────────────

    public ObservableCollection<PluginRow> Plugins { get; } = [];
    public bool HasPlugins => Plugins.Count > 0;
    [ObservableProperty] private string _pluginStatus = "";

    private void LoadPlugins()
    {
        Plugins.Clear();
        foreach (var p in _plugins?.Plugins ?? [])
            Plugins.Add(new PluginRow(p, _settings.IsPluginEnabled(p.PluginName)));
        OnPropertyChanged(nameof(HasPlugins));
    }

    private static string PluginsFolder() => RomManagerConfiguration.GetGlobalPath("Plugins");

    [RelayCommand]
    private async Task ImportPlugin()
    {
        if (_plugins is null || await Dialogs.PickFileAsync("Select plugin ZIP", "Plugin zip", "zip") is not { } zip) return;
        try
        {
            var name = await _plugins.ImportZipAsync(zip);
            PluginStatus = $"Imported {name}";
        }
        catch (Exception ex) { PluginStatus = $"Import failed: {ex.Message}"; }
        LoadPlugins();
    }

    [RelayCommand]
    private void OpenPluginsFolder()
    {
        var dir = PluginsFolder();
        Directory.CreateDirectory(dir);
        Shell.OpenWithDefaultApp(dir);
    }

    [RelayCommand]
    private async Task ReloadPlugins()
    {
        if (_plugins is null) return;
        try
        {
            await _plugins.LoadPluginsAsync(PluginsFolder(), askForApproval: true);
            PluginStatus = $"{_plugins.Plugins.Count} plugin(s) loaded";
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Settings] Plugin reload failed: {ex}");
            PluginStatus = $"Reload failed: {ex.Message}";
        }
        LoadPlugins();
    }

    // Takes effect at once: the plugin must not load on the next start even if Settings is cancelled.
    [RelayCommand]
    private void RevokePlugin(PluginRow row)
    {
        _plugins?.Approvals.Revoke(row.Name);
        row.IsEnabled = false;
        row.IsRevoked = true;
        _settings.SetPluginEnabled(row.Name, false);
        _persist(_settings);
    }

    // ── Save / cancel ─────────────────────────────────────────────────────

    /// <summary>Writes the draft into <see cref="Settings"/>, saves it and applies what is live (theme, compact
    /// mode, columns, Redump hashing, Switch overrides).</summary>
    public void Apply()
    {
        var s = _settings;
        s.Theme = ThemeKey(ThemeIndex);
        s.IsCompactMode = IsCompactMode;
        s.DatabasePath = NullIfBlank(DatabasePath.Value);
        s.PluginsDirectory = string.IsNullOrWhiteSpace(PluginsDirectory.Value) ? "plugins" : PluginsDirectory.Value.Trim();
        foreach (var row in Consoles) row.ApplyTo(s);
        s.ToolPaths = Tools.Where(t => !string.IsNullOrWhiteSpace(t.Path)).ToDictionary(t => t.Tool.Id, t => t.Path.Trim());
        s.RegionPriority = RegionPriority;
        s.RenameFormatString = RenameFormat;
        s.AutoDownloadMissingDBs = AutoDownloadMissingDbs;
        s.HashDiscImages = HashDiscImages;
        s.SwitchTitleDBUrlOverride = NullIfBlank(SwitchTitleDbUrl);
        s.SwitchVersionsUrlOverride = NullIfBlank(SwitchVersionsUrl);
        s.SwitchTitleDBPathOverride = NullIfBlank(SwitchTitleDbPath.Value);
        foreach (var group in ColumnGroups)
            s.VisibleColumns[group.ConsoleName] = [.. group.Columns.Where(c => c.IsVisible).Select(c => c.PropName)];
        foreach (var plugin in Plugins.Where(p => !p.IsRevoked))
            s.SetPluginEnabled(plugin.Name, plugin.IsEnabled);
        _persist(s);

        RomManagerShared.Utils.Dat.DiscHashing.UserOverride = s.HashDiscImages;
        SwitchConfiguration.TitleDBUrlOverride = s.SwitchTitleDBUrlOverride;
        SwitchConfiguration.VersionsUrlOverride = s.SwitchVersionsUrlOverride;
        SwitchConfiguration.TitleDBPathOverride = s.SwitchTitleDBPathOverride;
        Settings.RaiseCompactModeChanged();
        foreach (var group in ColumnGroups) Settings.RaiseColumnsChanged(group.ConsoleName);
    }

    /// <summary>Saves on confirm, otherwise reverts the theme preview.</summary>
    public override void OnClosed()
    {
        if (Confirmed) Apply();
        else ApplyTheme(_initialTheme);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static List<string> SplitList(string? text) =>
        [.. (text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}

/// <summary>A folder text box with a live "Found" / "Not found" / "Not set" status (checked off the UI thread).</summary>
public sealed partial class PathField : ObservableObject
{
    private readonly string _emptyText;
    private int _version;

    public PathField(string value, string emptyText = "Not set")
    {
        _emptyText = emptyText;
        _value = value;
        _ = CheckAsync(debounce: false);
    }

    [ObservableProperty] private string _value;
    [ObservableProperty] private StatusLine _status = StatusLine.Neutral("");

    partial void OnValueChanged(string value) => _ = CheckAsync(debounce: true);

    /// <summary>Resolves the latest check; for tests.</summary>
    public Task Checked { get; private set; } = Task.CompletedTask;

    private Task CheckAsync(bool debounce)
    {
        var my = ++_version;
        return Checked = Run();

        async Task Run()
        {
            var text = Value.Trim();
            if (text.Length == 0)
            {
                Status = StatusLine.Neutral(_emptyText);
                return;
            }
            if (debounce) await Task.Delay(300);
            if (my != _version) return;
            var exists = await Task.Run(() =>
            {
                try { return Directory.Exists(Resolve(text)); }
                catch { return false; }
            });
            if (my == _version) Status = exists ? StatusLine.Ok("Found") : StatusLine.Bad("Not found");
        }
    }

    // Relative paths are resolved against the app base folder (as the rest of the app does).
    private static string Resolve(string text)
    {
        var expanded = Environment.ExpandEnvironmentVariables(text);
        if (Path.IsPathRooted(expanded)) return expanded;
        var baseDir = string.IsNullOrEmpty(RomManagerConfiguration.BaseFolder) ? AppContext.BaseDirectory : RomManagerConfiguration.BaseFolder;
        return Path.Combine(baseDir, expanded);
    }
}

/// <summary>A line of status text with its colour.</summary>
public sealed record StatusLine(string Text, IBrush Brush)
{
    private static readonly IBrush OkBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x44));
    private static readonly IBrush BadBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0x3A, 0x2F));

    public static StatusLine Ok(string text) => new(text, OkBrush);
    public static StatusLine Bad(string text) => new(text, BadBrush);
    public static StatusLine Neutral(string text) => new(text, Brushes.Gray);
}

/// <summary>Everything Settings knows per console: folders, emulator and scan options.</summary>
public sealed partial class ConsoleSettingsRow : ObservableObject
{
    public ConsoleSettingsRow(ConsoleDescriptor console, Settings s)
    {
        Id = console.Id;
        DisplayName = console.DisplayName;
        Primary = new PathField(s.ConsoleLibraryPaths.GetValueOrDefault(Id, ""));
        _extraFolders = string.Join("; ", s.ConsoleExtraLibraryPaths.GetValueOrDefault(Id) ?? []);
        var emulator = s.Emulators.GetValueOrDefault(Id);
        _emulatorPath = emulator?.Path ?? "";
        _emulatorArguments = emulator?.Arguments ?? EmulatorLauncher.DefaultArguments;
        var scan = s.ScanSettings.GetValueOrDefault(Id);
        _parallelism = scan?.Parallelism?.ToString() ?? "";
        _excludedFolders = string.Join("; ", scan?.ExcludedFolders ?? []);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public PathField Primary { get; }
    [ObservableProperty] private string _extraFolders;
    [ObservableProperty] private string _emulatorPath;
    [ObservableProperty] private string _emulatorArguments;
    [ObservableProperty] private string _parallelism;
    [ObservableProperty] private string _excludedFolders;

    public void ApplyTo(Settings s)
    {
        Set(s.ConsoleLibraryPaths, string.IsNullOrWhiteSpace(Primary.Value) ? null : Primary.Value.Trim());
        var extra = SettingsViewModel.SplitList(ExtraFolders);
        Set(s.ConsoleExtraLibraryPaths, extra.Count == 0 ? null : extra);
        Set(s.Emulators, string.IsNullOrWhiteSpace(EmulatorPath) ? null
            : new EmulatorSettings { Path = EmulatorPath.Trim(), Arguments = EmulatorArguments });
        int? parallel = int.TryParse(Parallelism, out var n) && n > 0 ? n : null;
        var excluded = SettingsViewModel.SplitList(ExcludedFolders);
        Set(s.ScanSettings, parallel is null && excluded.Count == 0 ? null
            : new ConsoleScanSettings { Parallelism = parallel, ExcludedFolders = excluded });
    }

    private void Set<T>(Dictionary<string, T> map, T? value) where T : class
    {
        if (value is null) map.Remove(Id);
        else map[Id] = value;
    }
}

public sealed partial class ToolPathRow(ConversionTool tool, string path) : ObservableObject
{
    public ConversionTool Tool { get; } = tool;
    public string Placeholder => Tool.ResolvePath(null);
    [ObservableProperty] private string _path = path;
}

public sealed partial class ColumnToggle(string propName, string header, bool isVisible) : ObservableObject
{
    public string PropName { get; } = propName;
    public string Header { get; } = header;
    [ObservableProperty] private bool _isVisible = isVisible;
}

public sealed partial class ColumnGroup(string consoleName, string displayName, IReadOnlyList<string> defaults,
    IReadOnlyList<ColumnToggle> columns) : ObservableObject
{
    public string ConsoleName { get; } = consoleName;
    public string DisplayName { get; } = displayName;
    public IReadOnlyList<ColumnToggle> Columns { get; } = columns;

    [RelayCommand]
    private void Reset()
    {
        foreach (var c in Columns) c.IsVisible = defaults.Contains(c.PropName);
    }
}

public sealed partial class PluginRow(IPlugin plugin, bool enabled) : ObservableObject
{
    public string Name { get; } = plugin.PluginName;
    public string Heading { get; } = $"{plugin.PluginName}  v{plugin.PluginVersion}";
    public string Description { get; } = plugin.PluginDescription;
    public string Details { get; } = string.Join(" · ", new[]
    {
        string.IsNullOrEmpty(plugin.PluginAuthor) ? null : $"Author: {plugin.PluginAuthor}",
        plugin.SupportedConsoles is { Count: > 0 } c ? $"Consoles: {string.Join(", ", c)}" : null,
        $"Permissions: {(plugin.RequiredPermissions.Count == 0 ? "none" : string.Join(", ", plugin.RequiredPermissions))}",
    }.Where(x => x is not null));
    [ObservableProperty] private bool _isEnabled = enabled;
    [ObservableProperty] private bool _isRevoked;
}
