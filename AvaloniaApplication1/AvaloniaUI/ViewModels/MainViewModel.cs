namespace AvaloniaUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    /// <summary>Exposes the root service provider for the context menu handler.</summary>
    public IServiceProvider ServiceProvider => _serviceProvider;
    private bool _configLoaded = false;
    public static Dictionary<Type, string> RomPaths = LoadConsolePaths();
    public List<Type>? ConsoleTypes { get; set; }

    // ── Global download manager ────────────────────────────────────────────
    public DownloadManagerViewModel DownloadManager { get; } = new();

    // ── Operation queue for bulk operations ─────────────────────────────────
    public OperationQueue Operations { get; } = new();

    // ── Core operation service — all logic lives here, UI just calls ────────
    public RomOperationService OperationService { get; }

    // ── Total ROM count for status bar ────────────────────────────────────
    [ObservableProperty] private int _totalRomCount;
    [ObservableProperty] private string _statusMessage = "Ready";
    /// <summary>Rebuilds RomPaths from config.json and Settings (Settings wins).</summary>
    public static void ReloadRomPaths() => RomPaths = LoadConsolePaths();

    private static Dictionary<Type, string> LoadConsolePaths()
    {
        var consolePaths = new Dictionary<Type, string>();
        var assembly = Assembly.Load("RomManagerShared");

        // Source 1: config.json
        try
        {
            foreach (var consoleSection in RomManagerConfiguration.Configuration
                         .GetSection("Consoles").GetChildren())
            {
                var consoleName = consoleSection.Key + "Console";
                var consoleType = assembly.GetTypes().FirstOrDefault(t =>
                    t.Name.Equals(consoleName, StringComparison.OrdinalIgnoreCase));
                if (consoleType != null)
                {
                    var romPath = consoleSection["RomPath"];
                    if (!string.IsNullOrEmpty(romPath))
                        consolePaths.TryAdd(consoleType, romPath);
                }
            }
        }
        catch { /* config missing/malformed */ }

        // Source 2: Settings.ConsoleLibraryPaths overrides config.json
        foreach (var kv in Settings.Instance.ConsoleLibraryPaths)
        {
            var consoleName = kv.Key + "Console";
            var consoleType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals(consoleName, StringComparison.OrdinalIgnoreCase));
            if (consoleType != null && !string.IsNullOrEmpty(kv.Value))
                consolePaths[consoleType] = kv.Value;
        }

        return consolePaths;
    }
    [ObservableProperty] private IConsoleViewModel? _selectedConsole;

    public ObservableCollection<IConsoleViewModel> ConsoleViewModels { get; } = new();
    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        OperationService = new RomOperationService(Operations, serviceProvider);
        InitializeConsoles();
        // Load console tabs from config
        Task.Run(LoadConfigAsync);
    }
    private void InitializeConsoles()
    {
        var consoleTypes = GetAvailableConsoleTypes();
        foreach (var consoleType in consoleTypes)
        {
            // Create appropriate generic instance using reflection
            var viewModelType = typeof(ConsoleViewModel<>).MakeGenericType(consoleType);
            var consoleName = consoleType.Name.Replace("Console", "");
        
            var viewModel = (IConsoleViewModel)Activator.CreateInstance(
                viewModelType, 
                new object[] { consoleName, consoleType, _serviceProvider })!;
            
            ConsoleViewModels.Add(viewModel);
        }
    }
    [RelayCommand]
    private async Task ReloadConfig()
    {
        await LoadConfigAsync();
    }

    [RelayCommand]
    private void Exit()
    {
        Environment.Exit(0);
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        StatusMessage = "Checking for missing database files…";
        DownloadManager.IsPanelExpanded = true;

        DownloadMethodInfo[] missing;
        try
        {
            missing = await Task.Run(() => FileDownloader.GetMissingResources());
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB check failed: {ex.Message}";
            return;
        }

        if (missing.Length == 0)
        {
            StatusMessage = "All database files are present.";
            return;
        }

        StatusMessage = $"Queuing {missing.Length} missing database file(s)…";

        foreach (var resource in missing)
        {
            if (resource.Url is null || resource.FilePaths is null) continue;
            for (int i = 0; i < resource.Url.Length; i++)
            {
                var url  = resource.Url[i];
                var path = i < resource.FilePaths.Length ? resource.FilePaths[i] : resource.FilePaths[0];
                _ = DownloadManager.DownloadAsync(url, path, resource.DownloadName);
            }
        }

        StatusMessage = $"Queued {missing.Length} missing database download(s).";
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        // theme: "Default" | "Dark" | "Light"
        var variant = theme switch
        {
            "Dark"  => Avalonia.Styling.ThemeVariant.Dark,
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            _       => Avalonia.Styling.ThemeVariant.Default,
        };

        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = variant;

        Settings.Instance.Theme = theme;
        Settings.Instance.Save();
    }

    [RelayCommand]
    private void ToggleCompactMode()
    {
        Settings.Instance.IsCompactMode = !Settings.Instance.IsCompactMode;
        Settings.Instance.Save();
    }

    public void RecalculateTotalRomCount()
    {
        TotalRomCount = ConsoleViewModels.Sum(c => c.Roms?.Count ?? 0);
        StatusMessage = TotalRomCount > 0 ? $"{TotalRomCount:N0} ROMs loaded" : "Ready";
    }

    [RelayCommand]
    private void RenameAllRoms()
    {
        // Implementation
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        var plugins = _serviceProvider.GetService<PluginManager>();
        var win = new SettingsWindow(plugins, ConsoleViewModels);

        // Find the owner window
        var owner = Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (owner is not null)
            await win.ShowDialog(owner);
        else
            win.Show();
    }


    [RelayCommand]
    private async Task Refresh()
    {
        _configLoaded = false;
        ReloadRomPaths();          // pick up any path changes from Settings
        await LoadConfigAsync();

        // Load each console in sequence (parsers share I/O; parallel opens too many files)
        foreach (var console in ConsoleViewModels)
            await console.LoadData();

        RecalculateTotalRomCount();
    }
    public List<Type> GetAvailableConsoleTypes()
    {
        var assembly = Assembly.Load("RomManagerShared");
        return assembly.GetTypes()
            .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();
    }
// Update LoadConfigAsync method in MainViewModel.cs
    private async Task LoadConfigAsync()
    {
        // Return early if already loaded (unless refresh requested)
        if (_configLoaded)
            return;

        try
        {
            // Clear existing view models first
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ConsoleViewModels.Clear();
            });

            // Ensure configuration is loaded
            RomManagerShared.Configuration.RomManagerConfiguration.Load("config.json");

            // Create console VMs
            var consoleVMs = new List<IConsoleViewModel>();
            foreach (var romPathEntry in RomPaths)
            {
                var consoleType = romPathEntry.Key;
                var consolePath = romPathEntry.Value;
                var consoleName = consoleType.Name.Replace("Console", "");

                if (string.IsNullOrEmpty(consolePath) || !Directory.Exists(consolePath))
                {
                    FileUtils.Log($"[LoadConfigAsync] Skipping {consoleName}: path not configured or not found.");
                    continue;
                }

                var viewModelType = typeof(ConsoleViewModel<>).MakeGenericType(consoleType);

                var vm = (IConsoleViewModel?)Activator.CreateInstance(
                    viewModelType, 
                    new object[] { consoleName, consoleType, _serviceProvider });
                if (vm is null) continue;
                consoleVMs.Add(vm);
            }

            // Update on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var vm in consoleVMs.OrderBy(vm => vm.ConsoleName)) ConsoleViewModels.Add(vm);

                // Select first tab if available
                if (ConsoleViewModels.Count > 0 && SelectedConsole == null)
                {
                    SelectedConsole = ConsoleViewModels[0];
                }
            });

            _configLoaded = true;

            // Auto-check for missing metadata database files only if the user opted in
            if (Settings.Instance.AutoDownloadMissingDBs)
                _ = Task.Run(CheckForUpdates);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[LoadConfigAsync] Error loading config: {ex}");
        }
    }
}