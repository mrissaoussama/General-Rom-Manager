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

    public string ConsoleName { get; }
    public int RomCount => Roms.Count;
    public bool ShowEmptyState => !IsLoading && Roms.Count == 0;

    /// <summary>Set by the shell; rename/move/actions go through it.</summary>
    public RomOperationService? Operations { get; set; }
    /// <summary>Raised when an operation for this console was queued (the shell shows the operations panel).</summary>
    public event Action? OperationQueued;

    protected ConsoleViewModelBase(string consoleName, ImageCache? images)
    {
        ConsoleName = consoleName;
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
    /// <summary>Only rows whose title is missing or just the file name (unidentified).</summary>
    [ObservableProperty] private bool _unnamedOnly;

    public bool IsFiltered => SearchText.Length > 0 || TypeFilter != AnyOption || RegionFilter != AnyOption || UnnamedOnly;

    private bool Matches(RomViewModel r)
    {
        if (TypeFilter != AnyOption && r.RomType != TypeFilter) return false;
        if (RegionFilter != AnyOption && !r.Region.Split(", ").Contains(RegionFilter)) return false;
        if (UnnamedOnly && !IsUnnamed(r)) return false;
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
    partial void OnUnnamedOnlyChanged(bool value) => RefreshFilter();

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
        UnnamedOnly = false;
    }

    private void UpdateRegionOptions()
    {
        var regions = Roms.SelectMany(r => r.Region.Split(", ")).Distinct().Order().ToList();
        if (!RegionOptions.Skip(1).SequenceEqual(regions))
            RegionOptions = [AnyOption, .. regions];
        if (!RegionOptions.Contains(RegionFilter)) RegionFilter = AnyOption;
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
        var dialog = new FormatNameWindow(roms[0].RomSource, GetColumnDescriptors()
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
        Track(Operations.EnqueueMove([.. roms.Select(r => r.RomSource)], target), roms);
    }

    [RelayCommand]
    private async Task Delete(IReadOnlyList<RomViewModel> roms)
    {
        if (roms.Count == 0) return;
        var plural = roms.Count == 1 ? "" : "s";
        if (!await Shell.ConfirmAsync("Delete", $"Delete {roms.Count} ROM file{plural} from disk? This cannot be undone.", "Delete"))
            return;
        foreach (var r in roms.ToList())
        {
            try
            {
                if (File.Exists(r.Path)) File.Delete(r.Path);
                RemoveRow(r);
            }
            catch (Exception ex) { StatusMessage = $"Delete failed: {ex.Message}"; }
        }
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
