using System.Globalization;
using System.Windows.Input;
using Avalonia.Data.Converters;

namespace AvaloniaUI.ViewModels;
public interface IConsoleViewModel 
{
    string ConsoleName { get; }
    Task LoadData();
    Task Rescan();
    void CancelScan();
    /// <summary>
    /// Returns column descriptors so the View can build DataGrid columns without
    /// a direct dependency on <see cref="DataGrid"/>.
    /// PropName is either a direct ViewModel property (UsesDictionaryLookup=false)
    /// or a key in <see cref="RomViewModel.PropertiesDictionary"/> (UsesDictionaryLookup=true).
    /// </summary>
    IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors();
    ObservableCollection<RomViewModel> Roms { get; }
    RomViewModel SelectedRom { get; set; }
    bool IsLoading { get; }
    bool IsIndeterminate { get; }
    double ProgressPercent { get; }
    string StatusMessage { get; set; }

    /// <summary>
    /// Returns context-menu actions available for the given ROM selection.
    /// Built-in actions come from the backend's <see cref="IHasContextMenuActions"/>;
    /// plugin actions are added by the UI host.
    /// </summary>
    IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms);

    /// <summary>
    /// Returns all requirements declared by this console's parsers and manager.
    /// </summary>
    IReadOnlyList<ConsoleRequirement> GetAllRequirements();

    /// <summary>Non-optional requirements that are currently unmet.</summary>
    IReadOnlyList<ConsoleRequirement> GetUnmetRequirements();
}

public partial class ConsoleViewModel<T> : ViewModelBase , IConsoleViewModel where T : GamingConsole
{
    private readonly Type _consoleType;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private ObservableCollection<RomViewModel> _roms = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isIndeterminate = true;
    [ObservableProperty] private double _progressPercent = 0;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private RomViewModel? _selectedRom;
    public string ConsoleName { get; }

    public ConsoleViewModel(string consoleName, Type consoleType, IServiceProvider serviceProvider)
    {
        ConsoleName = consoleName;
        _consoleType = consoleType;
        _serviceProvider =serviceProvider;
    }

    // ── IConsoleViewModel: context menu actions ───────────────────────────
    public IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms)
    {
        using var scope = _serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetService<ConsoleManager<T>>();

        var backendActions = (manager as IHasContextMenuActions)?.GetContextMenuActions()
                             ?? Enumerable.Empty<IConsoleRomAction>();

        // Merge plugin actions (filtered to this console)
        var pluginManager = scope.ServiceProvider.GetService<PluginManager>();
        var pluginActions = pluginManager is null
            ? Enumerable.Empty<IConsoleRomAction>()
            : pluginManager.GetPluginsForConsole(ConsoleName)
                .SelectMany(p => p.Options.Select((opt, i) =>
                    (IConsoleRomAction)new PluginRomAction(p, i, opt)));

        return backendActions
            .Concat(pluginActions)
            .Where(a => a.CanExecute(selectedRoms));
    }

    // ── IConsoleViewModel: requirements ───────────────────────────────────
    public IReadOnlyList<ConsoleRequirement> GetAllRequirements()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<ConsoleManager<T>>();
            return manager?.GetAllRequirements() ?? [];
        }
        catch { return []; }
    }

    public IReadOnlyList<ConsoleRequirement> GetUnmetRequirements()
        => GetAllRequirements().Where(r => !r.IsOptional && !r.IsMet).ToList();


    private CancellationTokenSource? _scanCts;

    /// <summary>Shows the cached library, then scans for new or changed files.</summary>
    [RelayCommand]
    public Task LoadData() => LoadAsync(fullRescan: false);

    /// <summary>Re-parses every file, ignoring the cache.</summary>
    public Task Rescan() => LoadAsync(fullRescan: true);

    public void CancelScan() => _scanCts?.Cancel();

    private async Task LoadAsync(bool fullRescan)
    {
        _scanCts?.Cancel();
        using var cts = _scanCts = new CancellationTokenSource();
        var ct = cts.Token;
        var libraryKey = _consoleType.Name;
        try
        {
            IsLoading = true;
            IsIndeterminate = true;
            StatusMessage = "Loading library...";
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<ConsoleManager<T>>();
            var scanner = scope.ServiceProvider.GetRequiredService<LibraryScanner>();

            var cached = await scanner.LoadCachedAsync(libraryKey, ct);
            ShowRoms(cached);

            if (!MainViewModel.RomPaths.TryGetValue(_consoleType, out string? romPath)
                || string.IsNullOrEmpty(romPath)
                || !Directory.Exists(romPath))
            {
                StatusMessage = cached.Count > 0
                    ? $"{cached.Count} ROMs (cached) — ROM folder not found"
                    : $"ROM path not configured or not found for {ConsoleName}";
                return;
            }

            StatusMessage = cached.Count > 0 ? $"{cached.Count} ROMs — checking for changes..." : "Scanning...";
            await manager.Setup();

            var progress = new Progress<ScanProgress>(p =>
            {
                IsIndeterminate = p.Total == 0;
                ProgressPercent = p.Total > 0 ? p.Processed * 100.0 / p.Total : 0;
                if (p.CurrentFile is not null)
                    StatusMessage = $"Scanning {p.Processed + 1}/{p.Total}: {Path.GetFileName(p.CurrentFile)}";
            });
            var roms = await scanner.ScanAsync(manager, libraryKey, romPath, fullRescan, progress, ct);
            ShowRoms(roms);
            StatusMessage = $"Loaded {Roms.Count} ROMs";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            var msg = $"[{ConsoleName}] LoadData failed: {ex.Message}";
            FileUtils.Log(msg + "\n" + ex.ToString());
            StatusMessage = $"⚠️ Error: {ex.Message}";
        }
        finally
        {
            if (_scanCts == cts) _scanCts = null;
            ProgressPercent = 100;
            IsIndeterminate = false;
            IsLoading = false;
        }
    }

    private void ShowRoms(IEnumerable<Rom> roms)
    {
        Roms = new ObservableCollection<RomViewModel>(roms.Select(r => new RomViewModel(r)));
    }



  
    public IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors()
    {
        var result = new List<(string, string, bool)>
        {
            // ── Core fields (bound directly on RomViewModel) ──────────────
            ("TitleID",    "Title ID",     false),
            ("Title",      "Title",        false),
            ("RomType",    "Type",         false),
            ("Region",     "Region",       false),
            ("Version",    "Version",      false),
            // ── Extended base fields (looked up in PropertiesDictionary) ───
            ("FileName",       "File Name",      true),
            ("Publisher",      "Publisher",       true),
            ("Developer",      "Developer",       true),
            ("Languages",      "Languages",       true),
            ("Size",           "Size",             true),
            ("Release Date",   "Release Date",    true),
            ("Product Code",   "Product Code",    true),
            ("Players",        "Players",         true),
            ("Genres",         "Genres",           true),
            ("Description",    "Description",     true),
            ("Alt Titles",     "Alt Titles",       true),
            ("Min Firmware",   "Min Firmware",    true),
            ("Rating",         "Rating",          true),
            ("Hashes",         "Hashes",          true),
            ("IsDemo",         "Demo",            true),
            ("File Path",      "File Path",       true),
        };

        // ── Console-specific properties via reflected [UiProperty] ──────────
        foreach (var prop in GetConsoleSpecificProperties())
        {
            var attr = (UiPropertyAttribute)prop.GetCustomAttributes(typeof(UiPropertyAttribute), true)[0];
            if (!attr.Visible) continue;
            var header = !string.IsNullOrEmpty(attr.DisplayName) ? attr.DisplayName : prop.Name;
            result.Add((prop.Name, header, true));
        }

        return result;
    }
    

        private List<PropertyInfo> GetBaseRomProperties()
    {
        // Get properties from the base Rom class with UiPropertyAttribute
        return typeof(Rom).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(UiPropertyAttribute), true).Length > 0)
            .OrderBy(p => ((UiPropertyAttribute)p.GetCustomAttributes(typeof(UiPropertyAttribute), true)[0]).Order)
            .ToList();
    }
    
    private List<PropertyInfo> GetConsoleSpecificProperties()
    {
        var assembly = Assembly.Load("RomManagerShared");
        var baseRomProps = typeof(Rom).GetProperties().Select(p => p.Name).ToHashSet();
        var consoleName = _consoleType.Name.Replace("Console", "");
        
        // Find ROM types for this console (e.g., SwitchGame, SwitchUpdate, etc.)
        var romTypes = assembly.GetTypes()
            .Where(t => typeof(Rom).IsAssignableFrom(t) && 
                        t.Name.StartsWith(consoleName) && 
                        !t.IsAbstract)
            .ToList();
        
        var consoleSpecificProps = new List<PropertyInfo>();
        
        foreach (var romType in romTypes)
        {
            // Get properties that aren't in base Rom class
            var props = romType.GetProperties()
                .Where(p => !baseRomProps.Contains(p.Name) && 
                          p.GetCustomAttributes(typeof(UiPropertyAttribute), true).Length > 0)
                .OrderBy(p => ((UiPropertyAttribute)p.GetCustomAttributes(typeof(UiPropertyAttribute), true)[0]).Order);
                
            consoleSpecificProps.AddRange(props);
        }
        
        // Remove duplicates by name
        return consoleSpecificProps
            .GroupBy(p => p.Name)
            .Select(g => g.First())
            .ToList();
    }
    
    private void AddColumnsToDataGrid(DataGrid dataGrid, List<PropertyInfo> properties, 
        List<string> visibleColumns, string group)
    {
        foreach (var property in properties)
        {
            var attr = (UiPropertyAttribute)property.GetCustomAttributes(typeof(UiPropertyAttribute), true)[0];
            if (!attr.Visible) continue;
        
            var displayName = !string.IsNullOrEmpty(attr.DisplayName) ? attr.DisplayName : property.Name;
        
            // Create column with proper indexer binding
            var column = new DataGridTextColumn
            {
                Header = displayName,
                Binding = new Binding($"PropertiesDictionary") 
                { 
                    Converter = new DictionaryItemConverter(),
                    ConverterParameter = property.Name
                },
                IsVisible = visibleColumns.Contains(property.Name)
            };
        
            // Store metadata for column management
            column.Tag = $"{group}:{property.Name}";
        
            dataGrid.Columns.Add(column);
        }
    }
    
    private List<string> GetDefaultColumns()
    {
        // Default visible columns
        return [
            "TitleID",
            "Title", 
            "RomType",
            "Region",
            "Version"
        ];
    }
private string GetDisplayName(PropertyInfo prop)
{
    var attr = prop.GetCustomAttribute<UiPropertyAttribute>();
    return attr?.DisplayName ?? SplitCamelCase(prop.Name);
}

private string SplitCamelCase(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    
    var result = new System.Text.StringBuilder();
    result.Append(input[0]);
    
    for (int i = 1; i < input.Length; i++)
    {
        if (char.IsUpper(input[i]) && !char.IsUpper(input[i-1]))
            result.Append(' ');
        
        result.Append(input[i]);
    }
    
    return result.ToString();
}
}

// ── Adapts an IPlugin option into an IConsoleRomAction for the context menu ───

internal sealed class PluginRomAction(IPlugin plugin, int optionIndex, PluginOption option) : IConsoleRomAction
{
    public string Name => option.Name;
    public string Description => option.Description ?? option.Name;
    public string? IconKey => option.IconKey;
    public string Category => "Plugins";
    public bool SupportsBulk => true;

    public bool CanExecute(IEnumerable<Rom> selectedRoms)
        => option.CanExecutePredicate?.Invoke(selectedRoms) ?? true;

    public async Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var pm = services.GetRequiredService<PluginManager>();
        await pm.ExecuteAsync(plugin.PluginName, optionIndex, selectedRoms.ToList());
    }
}

public class DictionaryItemConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<string, string> dict && parameter is string key)
        {
            return dict.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
