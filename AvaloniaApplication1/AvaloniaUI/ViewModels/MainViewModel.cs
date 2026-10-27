
namespace AvaloniaUI.ViewModels;

/// <summary>Shell: navigation (dashboard + one page per console), menus and the bottom panels.</summary>
public partial class MainViewModel : ViewModelBase
{
    public const int LogTab = 0, OperationsTab = 1, DownloadsTab = 2;

    private readonly IServiceProvider _serviceProvider;
    // Startup and Refresh can both trigger a load; serialize them.
    private readonly SemaphoreSlim _configLock = new(1, 1);
    private readonly ILibraryRoots _roots;

    public IServiceProvider ServiceProvider => _serviceProvider;
    public DownloadManagerViewModel DownloadManager { get; } = new();
    public OperationQueue Operations { get; } = new();
    public RomOperationService OperationService { get; }

    public ObservableCollection<ConsoleViewModelBase> ConsoleViewModels { get; } = [];
    /// <summary>Sidebar entries: the dashboard followed by the consoles.</summary>
    public ObservableCollection<ViewModelBase> Pages { get; } = [];
    public DashboardViewModel Dashboard { get; }
    public LibraryViewModel Library { get; }
    public LogViewModel Log { get; } = new();
    public OperationsViewModel OperationsPanel { get; }

    [ObservableProperty] private ViewModelBase? _selectedPage;
    [ObservableProperty] private int _selectedBottomTab;
    [ObservableProperty] private bool _isAnyLoading;
    [ObservableProperty] private string _statusMessage = "Ready";

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _roots = serviceProvider.GetService<ILibraryRoots>() ?? new LibraryRoots();
        OperationService = new RomOperationService(Operations, serviceProvider, CreateJournal());
        OperationsPanel = new OperationsViewModel(Operations);
        OperationService.RestoreJournal();
        Dashboard = new DashboardViewModel(ConsoleViewModels, c => SelectedPage = c);
        Library = new LibraryViewModel(ConsoleViewModels, ScanAllAsync, serviceProvider.GetService<ImageCache>())
        {
            Operations = OperationService,
        };
        Library.OperationQueued += () => SelectedBottomTab = OperationsTab;
        Pages.Add(Dashboard);
        SelectedPage = Dashboard;
    }

    // Undo history lives next to the database so it survives clearing the cache.
    private static OperationJournal? CreateJournal()
    {
        try
        {
            var db = RomManagerConfiguration.GetSqliteDBPath();
            return string.IsNullOrEmpty(db) ? null : new OperationJournal(Path.Combine(Path.GetDirectoryName(db)!, "journal.json"));
        }
        catch { return null; }
    }

    // Adds pages for newly configured consoles and drops ones whose folder is gone; existing pages keep their state.
    private async Task<List<ConsoleViewModelBase>> SyncConsolesAsync()
    {
        var added = new List<ConsoleViewModelBase>();
        await _configLock.WaitAsync();
        try
        {
            var wanted = await Task.Run(() => _roots.ConfiguredConsoles()
                .Where(t => _roots.GetRoots(t).Any(Directory.Exists))
                .ToList());

            foreach (var stale in ConsoleViewModels.Where(c => !wanted.Any(t => Name(t) == c.ConsoleName)).ToList())
                ConsoleViewModels.Remove(stale);
            foreach (var type in wanted.Where(t => ConsoleViewModels.All(c => c.ConsoleName != Name(t))))
            {
                var vm = CreateConsole(type);
                ConsoleViewModels.Add(vm);
                added.Add(vm);
            }

            var ordered = ConsoleViewModels.OrderBy(c => c.ConsoleName).ToList();
            ConsoleViewModels.Clear();
            foreach (var c in ordered) ConsoleViewModels.Add(c);

            var selected = SelectedPage;
            Pages.Clear();
            Pages.Add(Dashboard);
            if (ordered.Count > 1) Pages.Add(Library);
            foreach (var c in ordered) Pages.Add(c);
            SelectedPage = selected is not null && Pages.Contains(selected) ? selected : Dashboard;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Consoles] Failed to load console list: {ex}");
        }
        finally
        {
            _configLock.Release();
        }
        return added;

        static string Name(Type t) => t.Name.Replace("Console", "");
    }

    private ConsoleViewModelBase CreateConsole(Type consoleType)
    {
        var vmType = typeof(ConsoleViewModel<>).MakeGenericType(consoleType);
        var descriptor = ConsoleModules.Describe(consoleType);
        var vm = (ConsoleViewModelBase)Activator.CreateInstance(vmType, descriptor.Id, _serviceProvider)!;
        vm.Describe(descriptor);
        vm.Operations = OperationService;
        vm.OperationQueued += () => SelectedBottomTab = OperationsTab;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConsoleViewModelBase.IsLoading))
                IsAnyLoading = ConsoleViewModels.Any(c => c.IsLoading);
        };
        return vm;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await SyncConsolesAsync();
        foreach (var c in ConsoleViewModels) c.RefreshRequirements();
        await ScanAllAsync(false);
        if (Settings.Instance.AutoDownloadMissingDBs)
            _ = DownloadMissingDatabasesAsync(showPanel: false);
    }

    /// <summary>Full rescan of every console (Shift+F5).</summary>
    [RelayCommand]
    private Task RescanAll() => ScanAllAsync(true);

    private Timer? _rescanTimer;

    /// <summary>
    /// Periodic incremental rescans (Settings → Scanning), for changes the folder watchers miss: drives plugged in
    /// later, files edited by other machines on a share. Call again after Settings change.
    /// </summary>
    public void ScheduleRescans(int hours)
    {
        _rescanTimer?.Dispose();
        _rescanTimer = null;
        if (hours <= 0) return;
        var interval = TimeSpan.FromHours(hours);
        _rescanTimer = new Timer(_ => Dispatcher.UIThread.Post(() =>
        {
            if (!IsAnyLoading) _ = ScanAllAsync(false);
        }), null, interval, interval);
    }

    private async Task ScanAllAsync(bool fullRescan)
    {
        // One console at a time: parsers share the disk.
        foreach (var console in ConsoleViewModels.ToList())
            await (fullRescan ? console.Rescan() : console.LoadData());
        RecalculateTotalRomCount();
    }

    public void RecalculateTotalRomCount()
    {
        var total = ConsoleViewModels.Sum(c => c.RomCount);
        StatusMessage = total > 0 ? string.Format(Strings.Library_ROMsInConsoles, total, ConsoleViewModels.Count) : Strings.Main_Ready;
    }

    [RelayCommand]
    private void ShowDashboard() => SelectedPage = Dashboard;

    [RelayCommand]
    private async Task ExportLibrary()
    {
        var rows = ConsoleViewModels.SelectMany(c => c.Roms.Select(r => ExportRow.From(c.ConsoleName, r.RomSource))).ToList();
        try
        {
            if (await Shell.ExportAsync(Dialogs, "RomManager library.csv", rows) is { } path)
                StatusMessage = string.Format(Strings.Console_ExportedRowsTo, rows.Count, path);
        }
        catch (Exception ex) { StatusMessage = string.Format(Strings.Console_ExportFailed, ex.Message); }
    }

    [RelayCommand]
    private void ShowLog() => SelectedBottomTab = LogTab;

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d)
            d.Shutdown();
    }

    [RelayCommand]
    private Task CheckForUpdates() => DownloadMissingDatabasesAsync(showPanel: true);

    /// <summary>Set by the app; null in tests and design mode.</summary>
    public IAppUpdater? Updater { get; set; }

    /// <summary>Checks GitHub for a newer RomManager and offers to install it.</summary>
    [RelayCommand]
    public async Task CheckForAppUpdate() => await CheckForAppUpdateAsync(quiet: false);

    /// <param name="quiet">Startup check: say nothing unless there is an update.</param>
    public async Task CheckForAppUpdateAsync(bool quiet)
    {
        if (Updater is not { IsInstalled: true } updater)
        {
            if (!quiet) StatusMessage = Strings.Main_UpdatesAreAvailableForInstalledCopiesOnlyThisOne;
            return;
        }
        try
        {
            if (await updater.CheckAsync() is not { } version)
            {
                if (!quiet) StatusMessage = Strings.Main_RomManagerIsUpToDate;
                return;
            }
            if (!await Dialogs.ConfirmAsync(Strings.Main_UpdateAvailable, string.Format(Strings.Main_RomManagerIsAvailableDownloadItAndRestartNow, version), Strings.Main_Update))
                return;
            StatusMessage = string.Format(Strings.Main_DownloadingRomManager, version);
            await updater.DownloadAndRestartAsync(new Progress<int>(p => StatusMessage = string.Format(Strings.Main_DownloadingRomManager2, version, p)));
            // A normal exit saves window placement and settings; the update installs and restarts afterwards.
            Exit();
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Update check failed: {ex.Message}");
            if (!quiet) StatusMessage = string.Format(Strings.Main_UpdateCheckFailed, ex.Message);
        }
    }

    /// <summary>Queues downloads for missing DB files. Returns the number queued, or -1 if the check failed.</summary>
    private async Task<int> DownloadMissingDatabasesAsync(bool showPanel)
    {
        StatusMessage = Strings.Main_CheckingForMissingDatabaseFiles;
        DownloadMethodInfo[] missing;
        try
        {
            var providers = _serviceProvider.GetServices<IDownloadableResources>().ToList();
            missing = await Task.Run(() => providers.SelectMany(p => p.GetMissingResources()).ToArray());
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.Main_DBCheckFailed, ex.Message);
            return -1;
        }

        if (missing.Length == 0)
        {
            StatusMessage = Strings.Main_AllDatabaseFilesArePresent;
            return 0;
        }

        if (showPanel) SelectedBottomTab = DownloadsTab;
        foreach (var resource in missing)
        {
            if (resource.Url is null || resource.FilePaths is null) continue;
            for (int i = 0; i < resource.Url.Length; i++)
            {
                var path = i < resource.FilePaths.Length ? resource.FilePaths[i] : resource.FilePaths[0];
                _ = DownloadManager.DownloadAsync(resource.Url[i], path, resource.DownloadName);
            }
        }
        StatusMessage = string.Format(Strings.Main_QueuedMissingDatabaseDownloadS, missing.Length);
        return missing.Length;
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = theme switch
            {
                "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                "Light" => Avalonia.Styling.ThemeVariant.Light,
                _ => Avalonia.Styling.ThemeVariant.Default,
            };
        Settings.Instance.Theme = theme;
        Settings.Instance.Save();
    }

    [RelayCommand]
    private void ToggleCompactMode()
    {
        Settings.Instance.IsCompactMode = !Settings.Instance.IsCompactMode;
        Settings.Instance.Save();
        Settings.RaiseCompactModeChanged();
    }

    /// <summary>Adds a dropped folder as a console's ROM folder, guessing the console from its files.</summary>
    public async Task AddLibraryFolderAsync(string folder)
    {
        var consoles = ConsoleModules.Descriptors;
        Dictionary<Type, IReadOnlyCollection<string>> extensions = [];
        Dictionary<Type, RomManagerShared.IRomParserExecutor> executors = [];
        using var scope = _serviceProvider.CreateScope();
        foreach (var d in consoles)
            try
            {
                if (scope.ServiceProvider.GetService(typeof(RomManagerShared.RomParserExecutor<>).MakeGenericType(d.ConsoleType))
                    is RomManagerShared.IRomParserExecutor executor)
                {
                    executors[d.ConsoleType] = executor;
                    extensions[d.ConsoleType] = executor.GetSupportedExtensions();
                }
            }
            catch (Exception ex) { AppLog.Warn($"[AddFolder] {d.Id}: {ex.Message}"); }

        IReadOnlyList<ConsoleDetector.Guess> guesses;
        try { guesses = await Task.Run(() => ConsoleDetector.Rank(ConsoleDetector.SampleFiles(folder), extensions,
            (console, path, header) => executors[console].MightParse(path, header))); }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.Main_CannotRead, folder, ex.Message);
            return;
        }

        var best = guesses.FirstOrDefault();
        var guess = best is null ? null : consoles.First(c => c.ConsoleType == best.Console);
        var detail = guess is null
            ? Strings.Main_NoKnownROMFilesFoundPickTheConsoleManually
            : string.Format(Strings.Main_LooksLikeMatchingFileS, guess.DisplayName, best!.Files);
        if (await Dialogs.ChooseConsoleAsync(folder, consoles, guess, detail) is not { } chosen) return;

        var existing = _roots.GetRoots(chosen.ConsoleType);
        if (existing.Contains(folder, FileUtils.PathComparer))
        {
            StatusMessage = string.Format(Strings.Main_IsAlreadyAFolder, folder, chosen.DisplayName);
            return;
        }
        if (existing.Count == 0)
            Settings.Instance.ConsoleLibraryPaths[chosen.Id] = folder;
        else if (await Dialogs.ConfirmAsync(Strings.Main_AddFolder,
                     string.Format(Strings.Main_AlreadyUsesAddAsAnotherFolder, chosen.DisplayName, string.Join(", ", existing), folder), Strings.Main_AddFolder))
            Settings.Instance.ConsoleExtraLibraryPaths[chosen.Id] = [.. Settings.Instance.ConsoleExtraLibraryPaths.GetValueOrDefault(chosen.Id) ?? [], folder];
        else
            return;
        Settings.Instance.Save();
        await SyncConsolesAsync();
        var page = ConsoleViewModels.FirstOrDefault(c => c.ConsoleName == chosen.Id);
        if (page is null) return;
        SelectedPage = page;
        await page.LoadData();
        RecalculateTotalRomCount();
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        await Dialogs.ShowAsync(new SettingsViewModel(Settings.Instance, _serviceProvider.GetService<PluginManager>(),
            ConsoleViewModels, () => DownloadMissingDatabasesAsync(showPanel: true),
            consoleOptions: _serviceProvider.GetServices<IConsoleOptions>()));
        ScheduleRescans(Settings.Instance.RescanIntervalHours);
        // Paths may have changed (LibraryRoots reads Settings on every call).
        var added = await SyncConsolesAsync();
        foreach (var c in ConsoleViewModels) c.RefreshRequirements();
        foreach (var c in added) await c.LoadData();
        RecalculateTotalRomCount();
    }
}
