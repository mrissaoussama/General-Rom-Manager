using Avalonia.Collections;

namespace AvaloniaUI.ViewModels;

/// <summary>Console-independent page state: rom list, filtering, grouping, selection and rom commands.</summary>
public abstract partial class ConsoleViewModelBase : ViewModelBase, IConsoleViewModel
{
    private readonly ImageCache? _images;

    [ObservableProperty] private ObservableCollection<RomViewModel> _roms = [];
    [ObservableProperty] private DataGridCollectionView _romsView;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isIndeterminate = true;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private RomViewModel? _selectedRom;
    [ObservableProperty] private RomDetailsViewModel? _details;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isGrouped;
    [ObservableProperty] private string? _requirementsWarning;
    [ObservableProperty] private bool _isCardView;
    /// <summary>Files from the last scan that matched an extension but parsed to nothing.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnrecognizedText))]
    private IReadOnlyList<UnrecognizedFile> _unrecognized = [];

    public string? UnrecognizedText => Unrecognized.Count == 0 ? null : $"{Unrecognized.Count} file(s) not recognised";

    /// <summary>When the last scan of this console finished (this session).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastScanText))]
    private DateTime? _lastScanned;

    public string LastScanText => LastScanned is not { } t ? "Not scanned yet"
        : t.Date == DateTime.Today ? $"Scanned at {t:t}" : $"Scanned {t:g}";

    /// <summary>Files sharing title ID, type and version with another file.</summary>
    public int DuplicateCount => _duplicates.Count;
    public bool HasDuplicates => _duplicates.Count > 0;

    [RelayCommand]
    private async Task ShowUnrecognized()
    {
        var list = new PreviewWindow("Not recognised",
            "These files have a matching extension but no parser could read them (wrong console, corrupt, encrypted or missing keys). " +
            "The reason is what the parsers logged. You can move them out of the ROM folder so they stop showing up here.",
            Unrecognized.Select(u => $"{u.Path}  —  {u.Reason}"), "Move to folder…");
        await Shell.ShowDialogAsync(list);
        if (list.Confirmed) await MoveUnrecognizedAsync();
    }

    // Moves the unrecognised files elsewhere (keeping their sub-folders) after a preview, then rescans.
    private async Task MoveUnrecognizedAsync()
    {
        if (Unrecognized.Count == 0 || await Shell.PickFolderAsync("Move unrecognised files to") is not { } target) return;
        var files = Unrecognized.Select(u => new Rom { Path = u.Path }).ToList();
        var mover = new RomMover(target);
        var preview = new PreviewWindow("Move unrecognised files", $"{files.Count} file(s) will be moved to {target}.",
            mover.PreviewMoves(files).Select(m => $"{m.SourcePath}  →  {m.DestinationPath}"), "Move");
        await Shell.ShowDialogAsync(preview);
        if (!preview.Confirmed) return;
        var results = await mover.MoveFilesAsync(files);
        var failed = results.Count(r => !r.Success);
        StatusMessage = failed == 0 ? $"Moved {results.Count} file(s) to {target}"
            : $"Moved {results.Count - failed}, {failed} failed: {results.First(r => !r.Success).ErrorMessage}";
        await LoadData();
    }

    /// <summary>True for consoles whose manager can report missing updates (shows the "Missing updates" button).</summary>
    [ObservableProperty] private bool _canCheckUpdates;

    [RelayCommand]
    private Task CheckMissingUpdates() => CheckMissingUpdatesAsync();

    protected virtual Task CheckMissingUpdatesAsync() => Task.CompletedTask;

    /// <summary>Games with a newer update than the library has; null when unsupported or no version database.</summary>
    public virtual Task<int?> CountMissingUpdatesAsync() => Task.FromResult<int?>(null);

    /// <summary>Have/miss against this console's DATs; null when it has no DAT files.</summary>
    public virtual Task<RomManagerShared.Utils.Dat.DatCoverage?> GetDatCoverageAsync() =>
        Task.FromResult<RomManagerShared.Utils.Dat.DatCoverage?>(null);

    /// <summary>Stable console id (settings keys, plugin matching).</summary>
    public string ConsoleName { get; }
    /// <summary>Name shown in the UI; defaults to <see cref="ConsoleName"/>.</summary>
    public string DisplayName { get; private set; } = "";
    public string Badge { get; private set; } = "";
    public string Manufacturer { get; private set; } = "";
    public IBrush BadgeBrush => ManufacturerBrush(Manufacturer);

    /// <summary>Applies the console's display data (set once by the shell after construction).</summary>
    public void Describe(ConsoleDescriptor descriptor)
    {
        DisplayName = descriptor.DisplayName;
        Badge = descriptor.Badge;
        Manufacturer = descriptor.Manufacturer;
    }

    private static IBrush ManufacturerBrush(string manufacturer) => manufacturer switch
    {
        "Nintendo" => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
        "Sony" => new SolidColorBrush(Color.FromRgb(0x1F, 0x4E, 0xA8)),
        "Microsoft" => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
        "Sega" => new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)),
        "NEC" => new SolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00)),
        _ => new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61)),
    };
    public int RomCount => Roms.Count;
    public bool ShowEmptyState => !IsLoading && Roms.Count == 0;

    /// <summary>Set by the shell; rename/move/actions go through it.</summary>
    public RomOperationService? Operations { get; set; }
    /// <summary>Raised when an operation for this console was queued (the shell shows the operations panel).</summary>
    public event Action? OperationQueued;

    protected ConsoleViewModelBase(string consoleName, ImageCache? images)
    {
        ConsoleName = consoleName;
        DisplayName = consoleName;
        _images = images;
        _romsView = CreateView(_roms);
        _roms.CollectionChanged += OnRomsCollectionChanged;
    }

    // ── Loading ───────────────────────────────────────────────────────────

    /// <summary>Shows the cached library, then scans for new or changed files.</summary>
    [RelayCommand]
    public Task LoadData() => LoadAsync(fullRescan: false);

    /// <summary>Re-parses every file, ignoring the cache.</summary>
    [RelayCommand]
    public Task Rescan() => LoadAsync(fullRescan: true);

    protected CancellationTokenSource? ScanCts;

    [RelayCommand]
    public void CancelScan() => ScanCts?.Cancel();

    protected abstract Task LoadAsync(bool fullRescan);

    public abstract IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors();
    public abstract IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms);
    public abstract IReadOnlyList<ConsoleRequirement> GetAllRequirements();

    /// <summary>Columns shown before the user picks any.</summary>
    public virtual IReadOnlyList<string> DefaultColumns => ["TitleID", "Title", "RomType", "Region", "Version"];

    public IReadOnlyList<ConsoleRequirement> GetUnmetRequirements()
        => GetAllRequirements().Where(r => !r.IsOptional && !r.IsMet).ToList();

    public void RefreshRequirements()
    {
        var unmet = GetUnmetRequirements();
        RequirementsWarning = unmet.Count == 0
            ? null
            : $"Missing: {string.Join(", ", unmet.Select(r => r.DisplayName))}. Some features may be unavailable; check Settings.";
    }

    protected void ShowRoms(IEnumerable<Rom> roms) =>
        Roms = new ObservableCollection<RomViewModel>(roms.Select(r => new RomViewModel(r) { Console = ConsoleName }));

    /// <summary>Removes a deleted row; the all-consoles view forwards this to the owning console.</summary>
    public virtual void RemoveRow(RomViewModel row) => Roms.Remove(row);

    /// <summary>Swaps a row for a fresh one after an operation changed the rom.</summary>
    public virtual void ReplaceRow(RomViewModel row)
    {
        var i = Roms.IndexOf(row);
        if (i >= 0) Roms[i] = new RomViewModel(row.RomSource) { GroupKey = row.GroupKey, Console = row.Console };
    }

    // ── View: filter + grouping ───────────────────────────────────────────

    private DataGridCollectionView CreateView(IList<RomViewModel> roms)
    {
        AssignGroupKeys(roms);
        ComputeSets(roms);
        var view = new DataGridCollectionView(roms) { Filter = o => Matches((RomViewModel)o) };
        if (IsGrouped)
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(RomViewModel.GroupKey)));
        return view;
    }

    public const string AnyOption = "All";
    public IReadOnlyList<string> TypeOptions { get; } = [AnyOption, "Game", "Update", "DLC", "Homebrew"];
    [ObservableProperty] private IReadOnlyList<string> _regionOptions = [AnyOption];
    [ObservableProperty] private string _typeFilter = AnyOption;
    [ObservableProperty] private string _regionFilter = AnyOption;
    [ObservableProperty] private IReadOnlyList<string> _languageOptions = [AnyOption];
    [ObservableProperty] private string _languageFilter = AnyOption;
    [ObservableProperty] private IReadOnlyList<string> _formatOptions = [AnyOption];
    [ObservableProperty] private string _formatFilter = AnyOption;
    /// <summary>Only rows whose title is missing or just the file name (unidentified).</summary>
    [ObservableProperty] private bool _unnamedOnly;
    public const string HasUpdate = "Has update", NoUpdate = "No update";
    public IReadOnlyList<string> UpdateOptions { get; } = [AnyOption, HasUpdate, NoUpdate];
    /// <summary>Games with or without an update in the library (other rows are hidden when set).</summary>
    [ObservableProperty] private string _updateFilter = AnyOption;
    /// <summary>Rows sharing title ID, type and version with another row.</summary>
    [ObservableProperty] private bool _duplicatesOnly;

    private HashSet<RomViewModel> _withUpdate = [];
    private HashSet<RomViewModel> _duplicates = [];

    public bool IsFiltered => SearchText.Length > 0 || TypeFilter != AnyOption || RegionFilter != AnyOption || UnnamedOnly
        || UpdateFilter != AnyOption || DuplicatesOnly || LanguageFilter != AnyOption || FormatFilter != AnyOption;

    // Switch-style 16-char IDs share the first 13 chars across game/update/DLC; other consoles reuse the ID.
    private static string BaseKey(RomViewModel r) =>
        r.TitleID.Length == 16 ? r.TitleID[..13] : r.TitleID.Length > 0 ? r.TitleID : r.GroupKey;

    private void ComputeSets(IList<RomViewModel> roms)
    {
        var updated = roms.Where(r => r.RomType == "Update").Select(BaseKey).ToHashSet();
        _withUpdate = [.. roms.Where(r => r.RomType == "Game"
            && (updated.Contains(BaseKey(r)) || (r.RomSource is Game { Updates.Count: > 0 })))];
        _duplicates = [.. roms.Where(r => r.TitleID.Length > 0)
            .GroupBy(r => (r.TitleID, r.RomType, r.Version))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)];
        OnPropertyChanged(nameof(DuplicateCount));
        OnPropertyChanged(nameof(HasDuplicates));
    }

    private bool Matches(RomViewModel r)
    {
        if (TypeFilter != AnyOption && r.RomType != TypeFilter) return false;
        if (RegionFilter != AnyOption && !r.Region.Split(", ").Contains(RegionFilter)) return false;
        if (LanguageFilter != AnyOption && !r.LanguageNames.Contains(LanguageFilter)) return false;
        if (FormatFilter != AnyOption && r.Format != FormatFilter) return false;
        if (UnnamedOnly && !IsUnnamed(r)) return false;
        if (UpdateFilter != AnyOption && (r.RomType != "Game" || _withUpdate.Contains(r) != (UpdateFilter == HasUpdate))) return false;
        if (DuplicatesOnly && !_duplicates.Contains(r)) return false;
        var q = SearchText.Trim();
        return q.Length == 0
            || r.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || r.TitleID.Contains(q, StringComparison.OrdinalIgnoreCase)
            || r.Path.Contains(q, StringComparison.OrdinalIgnoreCase)
            || r.RomType.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUnnamed(RomViewModel r) =>
        r.Title == "Unknown" || string.Equals(r.Title, Path.GetFileNameWithoutExtension(r.Path), StringComparison.OrdinalIgnoreCase);

    partial void OnTypeFilterChanged(string value) => RefreshFilter();
    partial void OnRegionFilterChanged(string value) => RefreshFilter();
    partial void OnLanguageFilterChanged(string value) => RefreshFilter();
    partial void OnFormatFilterChanged(string value) => RefreshFilter();
    partial void OnUnnamedOnlyChanged(bool value) => RefreshFilter();
    partial void OnUpdateFilterChanged(string value) => RefreshFilter();
    partial void OnDuplicatesOnlyChanged(bool value) => RefreshFilter();

    private void RefreshFilter()
    {
        RomsView.Refresh();
        OnPropertyChanged(nameof(IsFiltered));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        TypeFilter = AnyOption;
        RegionFilter = AnyOption;
        LanguageFilter = AnyOption;
        FormatFilter = AnyOption;
        UnnamedOnly = false;
        UpdateFilter = AnyOption;
        DuplicatesOnly = false;
    }

    private void UpdateRegionOptions()
    {
        RegionOptions = Options(RegionOptions, Roms.SelectMany(r => r.Region.Split(", ")));
        if (!RegionOptions.Contains(RegionFilter)) RegionFilter = AnyOption;
        LanguageOptions = Options(LanguageOptions, Roms.SelectMany(r => r.LanguageNames));
        if (!LanguageOptions.Contains(LanguageFilter)) LanguageFilter = AnyOption;
        FormatOptions = Options(FormatOptions, Roms.Select(r => r.Format).Where(f => f.Length > 0));
        if (!FormatOptions.Contains(FormatFilter)) FormatFilter = AnyOption;
    }

    // Keeps the current list instance when nothing changed so the ComboBox selection is not reset.
    private static IReadOnlyList<string> Options(IReadOnlyList<string> current, IEnumerable<string> values)
    {
        var sorted = values.Distinct().Order().ToList();
        return current.Skip(1).SequenceEqual(sorted) ? current : [AnyOption, .. sorted];
    }

    // Updates/DLC share the game's base title ID prefix (Switch-style IDs); group them under the game's name.
    private static void AssignGroupKeys(IList<RomViewModel> roms)
    {
        static string? BaseId(string id) => id.Length == 16 ? id[..13] : null;
        var names = roms.Where(r => r.RomType == "Game" && BaseId(r.TitleID) is not null)
            .GroupBy(r => BaseId(r.TitleID)!)
            .ToDictionary(g => g.Key, g => g.First().Title);
        foreach (var r in roms)
            r.GroupKey = BaseId(r.TitleID) is { } b && names.TryGetValue(b, out var n) ? n : r.Title;
    }

    partial void OnSearchTextChanged(string value) => RefreshFilter();

    partial void OnIsGroupedChanged(bool value) => RomsView = CreateView(Roms);

    partial void OnRomsChanged(ObservableCollection<RomViewModel>? oldValue, ObservableCollection<RomViewModel> newValue)
    {
        if (oldValue is not null) oldValue.CollectionChanged -= OnRomsCollectionChanged;
        newValue.CollectionChanged += OnRomsCollectionChanged;
        RomsView = CreateView(newValue);
        OnRomsCollectionChanged(null, null);
        UpdateRegionOptions();
    }

    private void OnRomsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs? e)
    {
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(RomCount));
        if (e is not null) ComputeSets(Roms);
    }

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(ShowEmptyState));

    partial void OnSelectedRomChanged(RomViewModel? value) =>
        Details = value is null ? null : new RomDetailsViewModel(value, _images);

    partial void OnDetailsChanged(RomDetailsViewModel? oldValue, RomDetailsViewModel? newValue) => oldValue?.Dispose();

    [RelayCommand]
    private void ClearSearch() => SearchText = "";

    [RelayCommand]
    private void SelectRom(RomViewModel rom) => SelectedRom = rom;

    // ── Rom commands (parameter: the selected rows) ───────────────────────

    [RelayCommand]
    private void RevealInExplorer(IReadOnlyList<RomViewModel> roms)
    {
        if (roms.FirstOrDefault() is { } r) Shell.RevealInExplorer(r.Path);
    }

    [RelayCommand]
    private void OpenWithDefaultApp(IReadOnlyList<RomViewModel> roms)
    {
        if (roms.FirstOrDefault() is { } r && File.Exists(r.Path)) Shell.OpenWithDefaultApp(r.Path);
    }

    [RelayCommand]
    private Task CopyTitleIds(IReadOnlyList<RomViewModel> roms) => Shell.CopyAsync(string.Join(", ", roms.Select(r => r.TitleID)));

    [RelayCommand]
    private Task CopyTitles(IReadOnlyList<RomViewModel> roms) => Shell.CopyAsync(string.Join(Environment.NewLine, roms.Select(r => r.Title)));

    [RelayCommand]
    private Task CopyFileNames(IReadOnlyList<RomViewModel> roms) =>
        Shell.CopyAsync(string.Join(Environment.NewLine, roms.Select(r => Path.GetFileName(r.Path))));

    [RelayCommand]
    private async Task Rename(IReadOnlyList<RomViewModel> roms)
    {
        if (Operations is null || roms.Count == 0) return;
        var dialog = new FormatNameWindow([.. roms.Select(r => r.RomSource)], GetColumnDescriptors()
            .Where(d => d.UsesDictionaryLookup).Select(d => d.PropName), ConsoleName);
        await Shell.ShowDialogAsync(dialog);
        if (dialog.Result is not { } format) return;

        Settings.Instance.RenameFormatString = format;
        Settings.Instance.Save();
        var op = roms.Count == 1
            ? Operations.EnqueueRename(roms[0].RomSource, format)
            : Operations.EnqueueBulkRename([.. roms.Select(r => r.RomSource)], format);
        Track(op, roms);
    }

    [RelayCommand]
    private async Task Move(IReadOnlyList<RomViewModel> roms)
    {
        if (Operations is null || roms.Count == 0) return;
        var target = await Shell.PickFolderAsync("Select target directory");
        if (target is null) return;
        var sources = roms.Select(r => r.RomSource).ToList();

        // Dry run first: the user sees every destination (folder structure kept) before anything moves.
        var plan = await Task.Run(() => new RomMover(target).PreviewMoves(sources));
        var moving = plan.Where(m => m.Changed).ToList();
        if (moving.Count == 0)
        {
            StatusMessage = "Nothing to move: files are already there.";
            return;
        }
        var bytes = sources.Sum(r => r.Size ?? 0);
        var preview = new PreviewWindow("Move",
            $"Move {moving.Count} file(s) ({FileUtils.FormatFileSize(bytes)}) to {target}:",
            moving.Select(m => $"{m.SourcePath}  →  {m.DestinationPath}"), "Move");
        await Shell.ShowDialogAsync(preview);
        if (!preview.Confirmed) return;
        Track(Operations.EnqueueMove(sources, target), roms);
    }

    [RelayCommand]
    private async Task Delete(IReadOnlyList<RomViewModel> roms)
    {
        // One file can hold several roms (e.g. an NSP with a game and its update); delete per file.
        var paths = roms.Select(r => r.Path).Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var folders = paths.Where(Directory.Exists).ToList();
        var files = paths.Except(folders).ToList();
        if (files.Count == 0)
        {
            StatusMessage = folders.Count > 0 ? "Folder ROMs are not deleted from here; remove the folder in your file manager." : "Nothing to delete";
            return;
        }
        var plural = files.Count == 1 ? "" : "s";
        var skipped = folders.Count == 0 ? "" : $" {folders.Count} folder ROM(s) in the selection are skipped.";
        if (!await Shell.ConfirmAsync("Delete", $"Move {files.Count} file{plural} to the Recycle Bin / Trash?{skipped}", "Move to trash"))
            return;

        void RemoveRows(string path)
        {
            foreach (var row in Roms.Where(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase)).ToList())
                RemoveRow(row);
        }

        List<(string Path, string Reason)> notTrashed = [];
        foreach (var path in files)
        {
            try
            {
                if (File.Exists(path)) await Task.Run(() => Trash.MoveToTrash(path));
                RemoveRows(path);
            }
            catch (Exception ex) { notTrashed.Add((path, ex.Message)); }
        }
        if (notTrashed.Count == 0)
        {
            StatusMessage = $"Moved {files.Count} file{plural} to the trash";
            return;
        }

        // No trash for these (network drive, USB stick, unsupported OS): only delete permanently when asked.
        if (!await Shell.ConfirmAsync("Delete permanently?",
                $"{notTrashed.Count} file(s) could not be moved to the trash ({notTrashed[0].Reason}). Delete them permanently? This cannot be undone.",
                "Delete permanently"))
        {
            StatusMessage = $"{notTrashed.Count} file(s) kept";
            return;
        }
        foreach (var (path, _) in notTrashed)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                RemoveRows(path);
            }
            catch (Exception ex) { StatusMessage = $"Delete failed: {ex.Message}"; }
        }
    }

    /// <summary>Exports the rows that pass the current filters.</summary>
    [RelayCommand]
    private async Task ExportShown()
    {
        var rows = RomsView.Cast<RomViewModel>()
            .Select(r => ExportRow.From(r.Console.Length > 0 ? r.Console : ConsoleName, r.RomSource)).ToList();
        try
        {
            if (await Shell.ExportAsync($"{ConsoleName}.csv", rows) is { } path)
                StatusMessage = $"Exported {rows.Count:N0} rows to {path}";
        }
        catch (Exception ex) { StatusMessage = $"Export failed: {ex.Message}"; }
    }

    [RelayCommand]
    private Task ViewProperties(IReadOnlyList<RomViewModel> roms) =>
        roms.FirstOrDefault() is { } r ? Shell.ShowDialogAsync(new PropertiesWindow(r)) : Task.CompletedTask;

    public void RunAction(IConsoleRomAction action, IReadOnlyList<RomViewModel> roms)
    {
        if (Operations is null) return;
        try { Track(Operations.EnqueueAction(action, [.. roms.Select(r => r.RomSource)]), roms); }
        catch (Exception ex)
        {
            StatusMessage = $"Action failed: {ex.Message}";
            FileUtils.Log($"[Action] '{action.Name}' failed: {ex.Message}");
        }
    }

    // Rebuild the affected rows once the operation completes so paths/names refresh.
    private void Track(QueuedOperation op, IReadOnlyList<RomViewModel> roms)
    {
        OperationQueued?.Invoke();
        var rows = roms.ToList();
        Operations!.Queue.OperationStateChanged += OnChanged;

        void OnChanged(QueuedOperation changed)
        {
            if (changed.Id != op.Id || !changed.IsFinished) return;
            Operations.Queue.OperationStateChanged -= OnChanged;
            if (changed.State != OperationState.Completed) return;
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var row in rows) ReplaceRow(row);
            });
        }
    }
}
