using System.Globalization;
using Avalonia.Data.Converters;


namespace AvaloniaUI.ViewModels;

/// <summary>What the settings window and shell need from a console page.</summary>
public interface IConsoleViewModel
{
    string ConsoleName { get; }
    ObservableCollection<RomViewModel> Roms { get; }
    bool IsLoading { get; }
    Task LoadData();
    Task Rescan();
    void CancelScan();
    /// <summary>
    /// PropName is either a direct <see cref="RomViewModel"/> property (UsesDictionaryLookup=false)
    /// or a key in <see cref="RomViewModel.PropertiesDictionary"/>.
    /// </summary>
    IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors();
    IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms);
    IReadOnlyList<ConsoleRequirement> GetAllRequirements();
    IReadOnlyList<ConsoleRequirement> GetUnmetRequirements();
}

public sealed partial class ConsoleViewModel<T> : ConsoleViewModelBase where T : GamingConsole
{
    private readonly Type _consoleType = typeof(T);
    private readonly IServiceProvider _serviceProvider;
    private readonly List<LibraryWatcher> _watchers = [];
    private readonly ILibraryRoots _roots;

    public ConsoleViewModel(string consoleName, IServiceProvider serviceProvider)
        : base(consoleName, serviceProvider.GetService<ImageCache>())
    {
        _serviceProvider = serviceProvider;
        _roots = serviceProvider.GetService<ILibraryRoots>() ?? new LibraryRoots();
        RefreshRequirements();
        using var scope = serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetService<ConsoleManager<T>>();
        CanCheckUpdates = manager is IMissingUpdateReporter;
        CanCheckDlc = manager is IMissingDlcReporter;
    }

    public override IReadOnlyList<RomManagerShared.Utils.Dat.DatIndex> GetDatIndexes()
    {
        using var scope = _serviceProvider.CreateScope();
        return [.. scope.ServiceProvider.GetServices<ITitleInfoProvider<T>>()
            .OfType<RomManagerShared.Utils.Dat.IDatSource>()
            .Select(s => s.Index).OfType<RomManagerShared.Utils.Dat.DatIndex>()];
    }

    public override async Task<RomManagerShared.Utils.Dat.DatCoverage?> GetDatCoverageAsync()
    {
        var indexes = GetDatIndexes();
        if (indexes.Count == 0) return null;
        var library = Roms.Select(r => r.RomSource).ToList();
        return await Task.Run(() => RomManagerShared.Utils.Dat.DatCoverage.Compute(indexes, library));
    }

    public override async Task<int?> CountMissingUpdatesAsync()
    {
        if (!CanCheckUpdates || Roms.Count == 0) return null;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            // Online checks (PS4) run only when asked for, never for the dashboard total.
            if (scope.ServiceProvider.GetService<ConsoleManager<T>>() is not IMissingUpdateReporter { HasVersionDatabase: true, IsOnline: false } reporter)
                return null;
            var library = Roms.Select(r => r.RomSource).ToList();
            return (await reporter.FindMissingUpdatesAsync(library)).Count;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[{ConsoleName}] Missing update count failed: {ex.Message}");
            return null;
        }
    }

    protected override async Task CheckMissingDlcAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        if (scope.ServiceProvider.GetService<ConsoleManager<T>>() is not IMissingDlcReporter reporter) return;
        try
        {
            StatusMessage = Strings.Console_CheckingForMissingDLC;
            var library = Roms.Select(r => r.RomSource).ToList();
            var missing = await Task.Run(() => reporter.FindMissingDlcAsync(library));
            StatusMessage = missing.Count == 0 ? Strings.Console_NoMissingDLCFound : string.Format(Strings.Console_DLCMissing, missing.Count);
            var summary = missing.Count == 0
                ? Strings.Console_TheTitleDatabaseListsNoDLCForYourGamesThatTheLib
                : string.Format(Strings.Console_DLCListedInTheTitleDatabaseForGamesInTheLibraryA, missing.Count);
            await Dialogs.PreviewAsync(Strings.Console_MissingDLC2, summary,
                missing.Select(m => $"{m.Game}: {m.Dlc}  [{m.TitleId}]"), Strings.Console_Close, showCancel: false);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[{ConsoleName}] Missing DLC check failed: {ex}");
            StatusMessage = string.Format(Strings.Console_MissingDLCCheckFailed, ex.Message);
        }
    }

    protected override async Task CheckMissingUpdatesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        if (scope.ServiceProvider.GetService<ConsoleManager<T>>() is not IMissingUpdateReporter reporter) return;
        try
        {
            if (!reporter.HasVersionDatabase)
            {
                if (!await Dialogs.ConfirmAsync(Strings.Console_MissingUpdates, reporter.SetupPrompt, reporter.IsOnline ? Strings.Console_Allow : Strings.Console_Download))
                    return;
                StatusMessage = Strings.Console_DownloadingVersionDatabase;
                await reporter.DownloadVersionDatabaseAsync();
                if (!reporter.HasVersionDatabase)
                {
                    StatusMessage = Strings.Console_VersionDatabaseDownloadFailedSeeLog;
                    return;
                }
            }

            StatusMessage = Strings.Console_CheckingForMissingUpdates;
            var library = Roms.Select(r => r.RomSource).ToList();
            var missing = await reporter.FindMissingUpdatesAsync(library);
            StatusMessage = missing.Count == 0 ? Strings.Console_AllGamesAreUpToDate : string.Format(Strings.Console_GameSHaveNewerUpdates, missing.Count);
            var summary = missing.Count == 0
                ? Strings.Console_EveryGameInTheLibraryHasItsLatestUpdate
                : string.Format(Strings.Console_GameSHaveANewerUpdateThanTheNewestOneInTheLibrar, missing.Count);
            await Dialogs.PreviewAsync(Strings.Console_MissingUpdates, summary,
                missing.Select(m => $"{m.Title}  [{m.Game.TitleID}]  v{m.InstalledVersion} → v{m.LatestVersion}"),
                Strings.Console_Close, showCancel: false);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[{ConsoleName}] Missing update check failed: {ex}");
            StatusMessage = string.Format(Strings.Console_MissingUpdateCheckFailed, ex.Message);
        }
    }

    public override IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms)
    {
        using var scope = _serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetService<ConsoleManager<T>>();
        var backendActions = (manager as IHasContextMenuActions)?.GetContextMenuActions() ?? [];

        var pluginManager = scope.ServiceProvider.GetService<PluginManager>();
        var pluginActions = pluginManager is null
            ? []
            : pluginManager.GetPluginsForConsole(ConsoleName)
                .Where(p => Settings.Instance.IsPluginEnabled(p.PluginName))
                .SelectMany(p => p.Options.Select((opt, i) => (IConsoleRomAction)new PluginRomAction(p, i, opt)));

        var roms = selectedRoms.ToList();
        return backendActions.Concat(pluginActions).Where(a => a.CanExecute(roms)).ToList();
    }

    public override IReadOnlyList<ConsoleRequirement> GetAllRequirements()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService<ConsoleManager<T>>()?.GetAllRequirements() ?? [];
        }
        catch { return []; }
    }

    // A new scan cancels the running one and waits for it to stop, so two scans never write the same rows.
    private readonly SemaphoreSlim _scanGate = new(1, 1);

    protected override async Task LoadAsync(bool fullRescan)
    {
        ScanCts?.Cancel();
        using var cts = ScanCts = new CancellationTokenSource();
        var ct = cts.Token;
        var libraryKey = _consoleType.Name;
        await _scanGate.WaitAsync();
        try
        {
            IsLoading = true;
            IsIndeterminate = true;
            StatusMessage = Strings.Console_LoadingLibrary;
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<ConsoleManager<T>>();
            var scanner = scope.ServiceProvider.GetRequiredService<LibraryScanner>();

            var cached = await scanner.LoadCachedAsync(libraryKey, ct);
            ShowRoms(cached);

            var roots = _roots.GetRoots(_consoleType);
            var reachable = await Task.Run(() => roots.Where(Directory.Exists).ToList(), ct); // a share can be slow
            OfflineFolders = [.. roots.Except(reachable)];
            if (reachable.Count == 0)
            {
                StatusMessage = cached.Count > 0
                    ? string.Format(Strings.Console_ROMsCachedROMFolderNotFound, cached.Count)
                    : string.Format(Strings.Console_ROMPathNotConfiguredOrNotFoundFor, ConsoleName);
                return;
            }

            StatusMessage = cached.Count > 0 ? string.Format(Strings.Console_ROMsCheckingForChanges, cached.Count) : Strings.Console_Scanning;
            await manager.Setup();

            var progress = new Progress<ScanProgress>(p =>
            {
                IsIndeterminate = p.Total == 0;
                ProgressPercent = p.Total > 0 ? p.Processed * 100.0 / p.Total : 0;
                if (p.CurrentFile is not null)
                    StatusMessage = string.Format(Strings.Console_Scanning2, p.Processed + 1, p.Total, Path.GetFileName(p.CurrentFile));
            });
            var roms = await scanner.ScanAsync(manager, libraryKey, roots, fullRescan, progress, ct,
                root => Settings.Instance.GetScanOptions(ConsoleName, root));
            ShowRoms(roms);
            StatusMessage = scanner.Unrecognized.Count == 0
                ? string.Format(Strings.Console_LoadedROMs, Roms.Count)
                : string.Format(Strings.Console_LoadedROMsFileSNotRecognised, Roms.Count, scanner.Unrecognized.Count);
            Unrecognized = scanner.Unrecognized;
            LastScanned = DateTime.Now;
            if (scanner.Unrecognized.Count > 0)
                AppLog.Info($"[{ConsoleName}] Not recognised:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", scanner.Unrecognized.Select(u => $"{u.Path}: {u.Reason}")));
            if (_serviceProvider.GetService<PluginManager>() is { Plugins.Count: > 0 } plugins)
            {
                var scan = LibraryScanCompleted.Diff(ConsoleName, cached, roms);
                _ = Task.Run(() => plugins.NotifyScanCompletedAsync(scan, p => Settings.Instance.IsPluginEnabled(p.PluginName)), CancellationToken.None);
            }
            EnsureWatchers(roots, manager.RomParserExecutor.GetSupportedExtensions());
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Strings.Console_ScanCancelled;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[{ConsoleName}] LoadData failed: {ex}");
            StatusMessage = string.Format(Strings.Console_Error, ex.Message);
        }
        finally
        {
            _scanGate.Release();
            if (ScanCts == cts) ScanCts = null;
            ProgressPercent = 100;
            IsIndeterminate = false;
            IsLoading = false;
        }
    }

    // Rescan (incrementally) when files change under any of the ROM folders.
    private void EnsureWatchers(IReadOnlyList<string> roots, IEnumerable<string> extensions)
    {
        var wanted = roots.Where(Directory.Exists).ToList();
        if (_watchers.Select(w => w.Root).SequenceEqual(wanted)) return;
        foreach (var w in _watchers) w.Dispose();
        _watchers.Clear();
        var exts = extensions.ToList();
        foreach (var root in wanted)
        {
            try
            {
                var watcher = new LibraryWatcher(root, exts);
                watcher.Changed += () => Dispatcher.UIThread.Post(() =>
                {
                    if (!IsLoading) _ = LoadData();
                });
                _watchers.Add(watcher);
            }
            catch (Exception ex)
            {
                AppLog.Warn($"[{ConsoleName}] Folder watcher unavailable for {root}: {ex.Message}");
            }
        }
    }

    public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors()
    {
        var result = new List<(string, string, bool)>
        {
            // Direct RomViewModel properties
            ("TitleID",      "Title ID",     false),
            ("Title",        "Title",        false),
            ("RomType",      "Type",         false),
            ("Region",       "Region",       false),
            ("Version",      "Version",      false),
            // PropertiesDictionary keys
            ("FileName",     "File Name",    true),
            ("Publisher",    "Publisher",    true),
            ("Developer",    "Developer",    true),
            ("Languages",    "Languages",    true),
            ("Size",         "Size",         true),
            ("Release Date", "Release Date", true),
            ("Product Code", "Product Code", true),
            ("Players",      "Players",      true),
            ("Genres",       "Genres",       true),
            ("Description",  "Description",  true),
            ("Alt Titles",   "Alt Titles",   true),
            ("Min Firmware", "Min Firmware", true),
            ("Rating",       "Rating",       true),
            ("Hashes",       "Hashes",       true),
            ("IsDemo",       "Demo",         true),
            ("File Path",    "File Path",    true),
        };

        foreach (var prop in GetConsoleSpecificProperties())
        {
            var attr = prop.GetCustomAttribute<UiPropertyAttribute>(true)!;
            if (attr.Visible)
                result.Add((prop.Name, string.IsNullOrEmpty(attr.DisplayName) ? prop.Name : attr.DisplayName, true));
        }
        return result;
    }

    // [UiProperty] members declared on this console's rom types (e.g. SwitchGame, SwitchUpdate).
    private static List<PropertyInfo> GetConsoleSpecificProperties()
    {
        var baseRomProps = typeof(Rom).GetProperties().Select(p => p.Name).ToHashSet();
        var prefix = typeof(T).Name.Replace("Console", "");
        return ConsoleModules.AllTypes
            .Where(t => typeof(Rom).IsAssignableFrom(t) && t.Name.StartsWith(prefix) && !t.IsAbstract)
            .SelectMany(t => t.GetProperties()
                .Where(p => !baseRomProps.Contains(p.Name) && p.GetCustomAttribute<UiPropertyAttribute>(true) is not null)
                .OrderBy(p => p.GetCustomAttribute<UiPropertyAttribute>(true)!.Order))
            .GroupBy(p => p.Name)
            .Select(g => g.First())
            .ToList();
    }
}

// Adapts an IPlugin option into an IConsoleRomAction for the context menu.
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
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Dictionary<string, string> dict && parameter is string key && dict.TryGetValue(key, out var val) ? val : "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}
