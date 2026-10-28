namespace AvaloniaUI.ViewModels;

/// <summary>A line of the "Add to library" preview.</summary>
public sealed record ImportRow(string Source, string Console, string Outcome, bool WillChange);

/// <summary>
/// File → Add to library: pick files or a folder, a library folder and what to do (move, copy, keep in place),
/// and see where each file goes before anything happens. The plan is rebuilt in the background on every change.
/// </summary>
public sealed partial class AddToLibraryViewModel : DialogViewModel
{
    private readonly LibraryImport _import;
    private int _planRun;

    public AddToLibraryViewModel(LibraryImport import, IEnumerable<string> libraryFolders)
    {
        _import = import;
        LibraryFolders = [.. libraryFolders];
        _libraryFolder = LibraryFolders.FirstOrDefault();
    }

    public override string Title => Strings.Import_Title;
    public override double Width => 860;
    public override double Height => 600;
    public override bool CanResize => true;

    /// <summary>The picked files and folders; a picked folder's entries are imported one by one.</summary>
    public ObservableCollection<string> Sources { get; } = [];
    public ObservableCollection<string> LibraryFolders { get; }
    public ObservableCollection<ImportRow> Rows { get; } = [];
    public IReadOnlyList<ImportItem> Plan { get; private set; } = [];

    [ObservableProperty] private string? _libraryFolder;
    [ObservableProperty] private ImportMode _mode = ImportMode.Move;
    [ObservableProperty] private string _summary = Strings.Import_PickSomething;
    [ObservableProperty] private bool _isPlanning;
    /// <summary>The folder picked as source, when one was (Keep in place adds it to the library).</summary>
    [ObservableProperty] private string? _sourceFolder;

    public bool IsMove { get => Mode == ImportMode.Move; set { if (value) Mode = ImportMode.Move; } }
    public bool IsCopy { get => Mode == ImportMode.Copy; set { if (value) Mode = ImportMode.Copy; } }
    public bool IsKeep { get => Mode == ImportMode.KeepInPlace; set { if (value) Mode = ImportMode.KeepInPlace; } }
    public bool NeedsLibraryFolder => Mode != ImportMode.KeepInPlace;

    partial void OnModeChanged(ImportMode value)
    {
        OnPropertyChanged(nameof(IsMove));
        OnPropertyChanged(nameof(IsCopy));
        OnPropertyChanged(nameof(IsKeep));
        OnPropertyChanged(nameof(NeedsLibraryFolder));
        _ = RebuildAsync();
    }

    partial void OnLibraryFolderChanged(string? value) => _ = RebuildAsync();

    protected override bool CanConfirm() => !IsPlanning && Plan.Any(i => i.WillChange)
        && (Mode == ImportMode.KeepInPlace || !string.IsNullOrWhiteSpace(LibraryFolder));

    [RelayCommand]
    private async Task PickFolder()
    {
        if (await Dialogs.PickFolderAsync(Strings.Import_PickFolder) is not { } folder) return;
        SourceFolder = folder;
        Sources.Clear();
        try { foreach (var entry in LibraryImport.Entries(folder)) Sources.Add(entry); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Summary = string.Format(Strings.Main_CannotRead, folder, ex.Message);
            return;
        }
        await RebuildAsync();
    }

    [RelayCommand]
    private async Task PickFiles()
    {
        var files = await Dialogs.PickFilesAsync(Strings.Import_PickFiles);
        if (files.Count == 0) return;
        SourceFolder = null;
        Sources.Clear();
        foreach (var file in files) Sources.Add(file);
        await RebuildAsync();
    }

    [RelayCommand]
    private async Task ChooseLibraryFolder()
    {
        if (await Dialogs.PickFolderAsync(Strings.Settings_ChooseLibraryFolder) is not { } folder) return;
        if (!LibraryFolders.Contains(folder, FileUtils.PathComparer)) LibraryFolders.Add(folder);
        LibraryFolder = folder;
    }

    /// <summary>Re-plans (headers are read, so off the UI thread); a newer run discards an older one.</summary>
    public async Task RebuildAsync()
    {
        var run = ++_planRun;
        var sources = Sources.ToList();
        var root = LibraryFolder;
        var mode = Mode;
        if (sources.Count == 0)
        {
            Plan = [];
            Rows.Clear();
            Summary = Strings.Import_PickSomething;
            ConfirmCommand.NotifyCanExecuteChanged();
            return;
        }
        IsPlanning = true;
        ConfirmCommand.NotifyCanExecuteChanged();
        Summary = Strings.Import_Planning;
        try
        {
            var plan = await Task.Run(() => _import.Plan(sources, root, mode));
            if (run != _planRun) return;
            Plan = plan;
            Rows.Clear();
            foreach (var item in plan) Rows.Add(Row(item, mode));
            var changing = plan.Count(i => i.WillChange);
            Summary = mode == ImportMode.KeepInPlace
                ? string.Format(Strings.Import_SummaryKeep, changing, plan.Count, string.Join(", ", plan.Where(i => i.WillChange).Select(i => i.Console!.DisplayName).Distinct()))
                : string.Format(mode == ImportMode.Move ? Strings.Import_SummaryMove : Strings.Import_SummaryCopy, changing, plan.Count, root);
        }
        catch (Exception ex)
        {
            if (run != _planRun) return;
            Plan = [];
            Summary = string.Format(Strings.Console_Error, ex.Message);
            AppLog.Warn($"[Import] Planning failed: {ex}");
        }
        finally
        {
            if (run == _planRun) IsPlanning = false;
            ConfirmCommand.NotifyCanExecuteChanged();
        }
    }

    private static ImportRow Row(ImportItem item, ImportMode mode)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(item.Source))
            + (item.Companions.Count > 0 ? string.Format(Strings.Import_AndCompanions, item.Companions.Count) : "");
        var outcome = item.Skip is { } skip ? string.Format(Strings.Import_Skipped, skip)
            : mode == ImportMode.KeepInPlace ? Strings.Import_StaysHere
            : "→ " + item.Destination;
        return new ImportRow(name, item.Console?.DisplayName ?? "?", outcome, item.WillChange);
    }
}

/// <summary>Skipped rows of the import preview are dimmed.</summary>
public sealed class ImportRowOpacity : Avalonia.Data.Converters.IValueConverter
{
    public static readonly ImportRowOpacity Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        value is true ? 1.0 : 0.55;

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}
