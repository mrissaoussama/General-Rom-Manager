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
    [ObservableProperty] private string _statusMessage = Strings.Main_Ready;
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

    public string? UnrecognizedText => Unrecognized.Count == 0 ? null : string.Format(Strings.Console_FileSNotRecognised, Unrecognized.Count);

    /// <summary>Configured ROM folders that could not be reached at the last scan; their roms come from the cache.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OfflineText), nameof(HasOfflineFolders))]
    private IReadOnlyList<string> _offlineFolders = [];

    public bool HasOfflineFolders => OfflineFolders.Count > 0;

    public string? OfflineText => OfflineFolders.Count switch
    {
        0 => null,
        1 => string.Format(Strings.Console_FolderNotReachableItsRomsAreShownFromTheLastScan, OfflineFolders[0]),
        _ => string.Format(Strings.Console_FoldersNotReachableTheirRomsAreShownFromTheLastS, OfflineFolders.Count, string.Join(", ", OfflineFolders)),
    };

    /// <summary>Forgets offline folders that are gone for good (removes them from Settings), then rescans.</summary>
    [RelayCommand]
    private async Task RemoveOfflineFolders()
    {
        if (OfflineFolders.Count == 0 || !await Dialogs.ConfirmAsync(Strings.Console_RemoveFolders,
                string.Format(Strings.Console_StopUsingForTheirRomsLeaveTheLibraryOnTheNextSca, string.Join(", ", OfflineFolders), DisplayName),
                Strings.Console_Remove))
            return;
        var settings = Settings.Instance;
        foreach (var folder in OfflineFolders)
        {
            if (settings.ConsoleLibraryPaths.TryGetValue(ConsoleName, out var primary) && string.Equals(primary, folder, FileUtils.PathComparison))
                settings.ConsoleLibraryPaths.Remove(ConsoleName);
            if (settings.ConsoleExtraLibraryPaths.TryGetValue(ConsoleName, out var extra))
                extra.RemoveAll(p => string.Equals(p, folder, FileUtils.PathComparison));
        }
        settings.Save();
        OfflineFolders = [];
        await LoadData();
    }

    /// <summary>When the last scan of this console finished (this session).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastScanText))]
    private DateTime? _lastScanned;

    public string LastScanText => LastScanned is not { } t ? Strings.Console_NotScannedYet
        : t.Date == DateTime.Today ? string.Format(Strings.Console_ScannedAt, t) : string.Format(Strings.Console_Scanned, t);

    /// <summary>Files sharing title ID, type and version with another file.</summary>
    public int DuplicateCount => _duplicates.Count;
    public bool HasDuplicates => _duplicates.Count > 0;

    [RelayCommand]
    private async Task ShowUnrecognized()
    {
        if (await Dialogs.PreviewAsync(Strings.Console_NotRecognised,
                Strings.Console_TheseFilesHaveAMatchingExtensionButNoParserCould,
                Unrecognized.Select(u => $"{u.Path}  —  {u.Reason}"), Strings.Console_MoveToFolder))
            await MoveUnrecognizedAsync();
    }

    // Moves the unrecognised files elsewhere (keeping their sub-folders) after a preview, then rescans.
    private async Task MoveUnrecognizedAsync()
    {
        if (Unrecognized.Count == 0 || await Dialogs.PickFolderAsync(Strings.Console_MoveUnrecognisedFilesTo) is not { } target) return;
        var files = Unrecognized.Select(u => new Rom { Path = u.Path }).ToList();
        var mover = new RomMover(target);
        if (!await Dialogs.PreviewAsync(Strings.Console_MoveUnrecognisedFiles, string.Format(Strings.Console_FileSWillBeMovedTo, files.Count, target),
                mover.PreviewMoves(files).Select(m => $"{m.SourcePath}  →  {m.DestinationPath}"), Strings.Console_Move))
            return;
        var results = await mover.MoveFilesAsync(files);
        var failed = results.Count(r => !r.Success);
        StatusMessage = failed == 0 ? string.Format(Strings.Console_MovedFileSTo, results.Count, target)
            : string.Format(Strings.Console_MovedFailed, results.Count - failed, failed, results.First(r => !r.Success).ErrorMessage);
        await LoadData();
    }

    /// <summary>True for consoles whose manager can report missing updates (shows the "Missing updates" button).</summary>
    [ObservableProperty] private bool _canCheckUpdates;

    [RelayCommand]
    private Task CheckMissingUpdates() => CheckMissingUpdatesAsync();

    protected virtual Task CheckMissingUpdatesAsync() => Task.CompletedTask;

    /// <summary>True for consoles whose manager can list missing DLC (shows the "Missing DLC" button).</summary>
    [ObservableProperty] private bool _canCheckDlc;

    [RelayCommand]
    private Task CheckMissingDlc() => CheckMissingDlcAsync();

    protected virtual Task CheckMissingDlcAsync() => Task.CompletedTask;

    /// <summary>Games with a newer update than the library has; null when unsupported or no version database.</summary>
    public virtual Task<int?> CountMissingUpdatesAsync() => Task.FromResult<int?>(null);

    /// <summary>Have/miss against this console's DATs; null when it has no DAT files.</summary>
    /// <summary>The console's loaded DATs (empty when none).</summary>
    public virtual IReadOnlyList<RomManagerShared.Utils.Dat.DatIndex> GetDatIndexes() => [];

    public virtual Task<RomManagerShared.Utils.Dat.DatCoverage?> GetDatCoverageAsync() =>
        Task.FromResult<RomManagerShared.Utils.Dat.DatCoverage?>(null);

    /// <summary>Stable console id (settings keys, plugin matching).</summary>
    public string ConsoleName { get; }
    /// <summary>Name shown in the UI; defaults to <see cref="ConsoleName"/>.</summary>
    public string DisplayName { get; private set; } = "";
    public string Badge { get; private set; } = "";
    public string Manufacturer { get; private set; } = "";
    public IBrush BadgeBrush => ManufacturerBrush(Manufacturer);

    // List items (sidebar) are announced by screen readers through ToString.
    public override string ToString() => DisplayName;

    /// <summary>Applies the console's display data (set once by the shell after construction).</summary>
    public void Describe(ConsoleDescriptor descriptor)
    {
        DisplayName = descriptor.DisplayName;
        Badge = descriptor.Badge;
        Manufacturer = descriptor.Manufacturer;
    }

    private static IBrush ManufacturerBrush(string manufacturer) => manufacturer switch
    {
        "Nintendo" => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
        "Sony" => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0x1F, 0x4E, 0xA8)),
        "Microsoft" => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
        "Sega" => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)),
        "NEC" => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00)),
        _ => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61)),
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
            : string.Format(Strings.Console_MissingSomeFeaturesMayBeUnavailableCheckSettings, string.Join(", ", unmet.Select(r => r.DisplayName)));
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
        if (await Dialogs.AskRenameTemplateAsync([.. roms.Select(r => r.RomSource)], GetColumnDescriptors()
                .Where(d => d.UsesDictionaryLookup).Select(d => d.PropName), ConsoleName,
                Settings.Instance.RenameFormatString) is not { } format)
            return;

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
        var target = await Dialogs.PickFolderAsync(Strings.Console_SelectTargetDirectory);
        if (target is null) return;
        var sources = roms.Select(r => r.RomSource).ToList();

        // Dry run first: the user sees every destination (folder structure kept) before anything moves.
        var plan = await Task.Run(() => new RomMover(target).PreviewMoves(sources));
        var moving = plan.Where(m => m.Changed).ToList();
        if (moving.Count == 0)
        {
            StatusMessage = Strings.Console_NothingToMoveFilesAreAlreadyThere;
            return;
        }
        var bytes = sources.Sum(r => r.Size ?? 0);
        if (!await Dialogs.PreviewAsync(Strings.Console_Move,
                string.Format(Strings.Console_MoveFileSTo, moving.Count, FileUtils.FormatFileSize(bytes), target),
                moving.Select(m => $"{m.SourcePath}  →  {m.DestinationPath}"), Strings.Console_Move))
            return;
        Track(Operations.EnqueueMove(sources, target), roms);
    }

    [RelayCommand]
    private async Task Delete(IReadOnlyList<RomViewModel> roms)
    {
        // One file can hold several roms (e.g. an NSP with a game and its update); delete per file.
        var paths = roms.Select(r => r.Path).Where(p => p.Length > 0).Distinct(FileUtils.PathComparer).ToList();
        var folders = paths.Where(Directory.Exists).ToList();
        var files = paths.Except(folders).ToList();
        if (files.Count == 0)
        {
            StatusMessage = folders.Count > 0 ? Strings.Console_FolderROMsAreNotDeletedFromHereRemoveTheFolderIn : Strings.Console_NothingToDelete;
            return;
        }
        var plural = files.Count == 1 ? "" : "s";
        var skipped = folders.Count == 0 ? "" : string.Format(Strings.Console_FolderROMSInTheSelectionAreSkipped, folders.Count);
        if (!await Dialogs.ConfirmAsync(Strings.Console_Delete, string.Format(Strings.Console_MoveFileToTheRecycleBinTrash, files.Count, plural, skipped), Strings.Console_MoveToTrash))
            return;

        var deleted = new List<string>();
        void RemoveRows(string path)
        {
            deleted.Add(path);
            foreach (var row in Roms.Where(r => string.Equals(r.Path, path, FileUtils.PathComparison)).ToList())
                RemoveRow(row);
        }
        try
        {
            await DeleteFilesAsync(files, plural, RemoveRows);
        }
        finally
        {
            Operations?.NotifyFilesChanged("Delete", [.. roms.Select(r => r.RomSource)], [.. deleted.Select(p => new LibraryFileChange(p, null))]);
        }
    }

    private async Task DeleteFilesAsync(List<string> files, string plural, Action<string> removeRows)
    {
        List<(string Path, string Reason)> notTrashed = [];
        foreach (var path in files)
        {
            try
            {
                if (File.Exists(path)) await Task.Run(() => Trash.MoveToTrash(path));
                removeRows(path);
            }
            catch (Exception ex) { notTrashed.Add((path, ex.Message)); }
        }
        if (notTrashed.Count == 0)
        {
            StatusMessage = string.Format(Strings.Console_MovedFileToTheTrash, files.Count, plural);
            return;
        }

        // No trash for these (network drive, USB stick, unsupported OS): only delete permanently when asked.
        if (!await Dialogs.ConfirmAsync(Strings.Console_DeletePermanently,
                string.Format(Strings.Console_FileSCouldNotBeMovedToTheTrashDeleteThemPermanen, notTrashed.Count, notTrashed[0].Reason),
                Strings.Console_DeletePermanently2))
        {
            StatusMessage = string.Format(Strings.Console_FileSKept, notTrashed.Count);
            return;
        }
        foreach (var (path, _) in notTrashed)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                removeRows(path);
            }
            catch (Exception ex) { StatusMessage = string.Format(Strings.Console_DeleteFailed, ex.Message); }
        }
    }

    /// <summary>
    /// 1G1R: keeps one version of each game (Settings → region priority) and moves the others to a folder,
    /// after a preview. The move is an ordinary queued, undoable operation.
    /// </summary>
    [RelayCommand]
    private async Task OneGameOneRom()
    {
        if (Operations is null) return;
        var priority = RomManagerShared.Utils.Dat.OneGameOneRom.ParsePriority(Settings.Instance.RegionPriority);
        var library = Roms.Select(r => r.RomSource).ToList();
        var dats = GetDatIndexes();
        var groups = await Task.Run(() => RomManagerShared.Utils.Dat.OneGameOneRom.Select(library, priority, dats));
        if (groups.Count == 0)
        {
            StatusMessage = Strings.Console_1G1REveryGameHasASingleVersion;
            return;
        }
        var others = groups.SelectMany(g => g.Others).ToList();
        var lines = groups.SelectMany(g => new[] { string.Format(Strings.Console_Keep, g.Game, Path.GetFileName(g.Keep.Path)) }
            .Concat(g.Others.Select(o => string.Format(Strings.Console_Move2, Path.GetFileName(o.Path)))));
        if (!await Dialogs.PreviewAsync("1G1R",
                string.Format(Strings.Console_GameSHaveSeveralVersionsRegionPriorityOthersCoun, groups.Count, string.Join(", $", priority)),
                lines, Strings.Console_MoveOthers))
            return;
        if (await Dialogs.PickFolderAsync(Strings.Console_MoveTheOtherVersionsTo) is not { } target) return;
        var moving = new HashSet<object>(others, ReferenceEqualityComparer.Instance);
        var rows = Roms.Where(r => moving.Contains(r.RomSource)).ToList();
        Track(Operations.EnqueueMove(others, target), rows);
    }

    /// <summary>Lists which dumps a DAT confirmed and which it did not (bad dumps, hacks, or missing from the DAT).</summary>
    [RelayCommand]
    private async Task Verify()
    {
        if (GetDatIndexes().Count == 0)
        {
            StatusMessage = Strings.Console_NoDATForThisConsoleAddNoIntroOrRedumpDATsToTheDA;
            return;
        }
        var library = Roms.Select(r => r.RomSource).ToList();
        var result = await Task.Run(() => RomManagerShared.Utils.Dat.DatVerification.Compute(library));
        var total = result.Verified.Count + result.Unmatched.Count;
        await Dialogs.PreviewAsync(Strings.Console_Verify2,
            string.Format(Strings.Console_OfDumpSMatchADATEntryExactlyTheseDoNotABadOrModi, result.Verified.Count, total, result.Unmatched.Count),
            result.Unmatched.Select(r => $"{r.Titles?.FirstOrDefault()?.Value ?? "?"}  —  {r.Path}"), Strings.Console_Close, showCancel: false);
    }

    /// <summary>Keeps one file of each set of identical roms and moves the other copies, after a preview (undoable).</summary>
    [RelayCommand]
    private async Task ResolveDuplicates()
    {
        if (Operations is null) return;
        var groups = DuplicateDetector.FindExactCopies(Roms.Select(r => r.RomSource));
        if (groups.Count == 0)
        {
            StatusMessage = Strings.Console_NoDuplicateFiles;
            return;
        }
        var others = groups.SelectMany(g => g.Redundant).ToList();
        var lines = groups.SelectMany(g => new[] { string.Format(Strings.Console_Keep, g.Preferred.Titles?.FirstOrDefault()?.Value ?? g.GroupKey, g.Preferred.Path) }
            .Concat(g.Redundant.Select(o => string.Format(Strings.Console_Move2, o.Path))));
        if (!await Dialogs.PreviewAsync(Strings.Console_Duplicates2,
                string.Format(Strings.Console_RomSAreStoredMoreThanOnceSameTitleIDTypeAndVersi, groups.Count, others.Count),
                lines, Strings.Console_MoveCopies))
            return;
        if (await Dialogs.PickFolderAsync(Strings.Console_MoveTheDuplicateCopiesTo) is not { } target) return;
        var moving = new HashSet<object>(others, ReferenceEqualityComparer.Instance);
        Track(Operations.EnqueueMove(others, target), [.. Roms.Where(r => moving.Contains(r.RomSource))]);
    }

    /// <summary>Exports the rows that pass the current filters.</summary>
    [RelayCommand]
    private async Task ExportShown()
    {
        var rows = RomsView.Cast<RomViewModel>()
            .Select(r => ExportRow.From(r.Console.Length > 0 ? r.Console : ConsoleName, r.RomSource)).ToList();
        try
        {
            if (await Shell.ExportAsync(Dialogs, $"{ConsoleName}.csv", rows) is { } path)
                StatusMessage = string.Format(Strings.Console_ExportedRowsTo, rows.Count, path);
        }
        catch (Exception ex) { StatusMessage = string.Format(Strings.Console_ExportFailed, ex.Message); }
    }

    /// <summary>Conversions that apply to some of <paramref name="roms"/> and whose program is present.</summary>
    public static IReadOnlyList<ConversionTool> AvailableConversions(IReadOnlyList<RomViewModel> roms) =>
        [.. ConversionTools.All.Where(t => roms.Any(r => t.Accepts(r.Path)) && File.Exists(Settings.Instance.ToolPath(t)))];

    /// <summary>
    /// Queues a conversion of the selected files (originals are kept). The program is pinned to its SHA-256 on first
    /// use; if it changed since, the user confirms before it runs.
    /// </summary>
    public async Task ConvertAsync(ConversionTool tool, IReadOnlyList<RomViewModel> roms)
    {
        if (Operations is null) return;
        var exe = Settings.Instance.ToolPath(tool);
        try
        {
            if (ToolPins.Default is { } pins && File.Exists(exe))
            {
                if (pins.Check(exe) == ToolPinStatus.Changed
                    && !await Dialogs.ConfirmAsync(Strings.Console_ToolChangedTitle, string.Format(Strings.Console_ToolChanged, tool.DisplayName, exe), Strings.Console_RunAnyway))
                    return;
                pins.Pin(exe);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Convert] {exe}: cannot check the program: {ex.Message}");
            StatusMessage = string.Format(Strings.Console_Error, ex.Message);
            return;
        }
        Track(Operations.EnqueueConvert([.. roms.Select(r => r.RomSource)], tool, exe), roms);
    }

    /// <summary>Starts the emulator configured for the rom's console (Settings → Emulators).</summary>
    [RelayCommand]
    private void LaunchInEmulator(IReadOnlyList<RomViewModel> roms)
    {
        if (roms.FirstOrDefault() is not { } rom) return;
        var console = rom.Console.Length > 0 ? rom.Console : ConsoleName;
        if (!Settings.Instance.Emulators.TryGetValue(console, out var emulator) || string.IsNullOrWhiteSpace(emulator.Path))
        {
            StatusMessage = string.Format(Strings.Console_NoEmulatorSetForAddOneInSettingsEmulators, console);
            return;
        }
        try { EmulatorLauncher.Launch(emulator.Path, emulator.Arguments, rom.Path); }
        catch (Exception ex) { StatusMessage = string.Format(Strings.Console_CouldNotStartTheEmulator, ex.Message); }
    }

    /// <summary>CRC32, MD5 and SHA1 of the selected files, shown when done and kept with the library.</summary>
    [RelayCommand]
    private void CalculateHashes(IReadOnlyList<RomViewModel> roms)
    {
        if (Operations is null || roms.Count == 0) return;
        var op = Operations.EnqueueHashCalculation([.. roms.Select(r => r.RomSource)], hashed =>
            Dispatcher.UIThread.Post(async () =>
            {
                var lines = hashed.Where(r => r.Hashes is { Count: > 0 })
                    .Select(r => $"{Path.GetFileName(r.Path)}  " + string.Join("  ", r.Hashes!.Where(h => h.Description == "Local")
                        .GroupBy(h => h.Type).Select(g => $"{g.Key}: {g.Last().Value}")))
                    .ToList();
                await Dialogs.PreviewAsync(Strings.Console_Hashes,
                    string.Format(Strings.Console_HashedFiles, lines.Count, hashed.Count), lines, Strings.Console_Close, showCancel: false);
            }));
        Track(op, roms);
    }

    [RelayCommand]
    private Task ViewProperties(IReadOnlyList<RomViewModel> roms) =>
        roms.FirstOrDefault() is { } r ? Dialogs.ShowPropertiesAsync(r) : Task.CompletedTask;

    public void RunAction(IConsoleRomAction action, IReadOnlyList<RomViewModel> roms)
    {
        if (Operations is null) return;
        try { Track(Operations.EnqueueAction(action, [.. roms.Select(r => r.RomSource)]), roms); }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.Console_ActionFailed, ex.Message);
            AppLog.Warn($"[Action] '{action.Name}' failed: {ex.Message}");
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
