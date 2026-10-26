using System.ComponentModel;

namespace AvaloniaUI.ViewModels;

/// <summary>Library overview: totals across consoles plus one tile per console.</summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly Action<ConsoleViewModelBase> _open;
    private bool _pending;

    public ObservableCollection<ConsoleViewModelBase> Consoles { get; }

    [ObservableProperty] private int _totalRoms;
    [ObservableProperty] private int _games;
    [ObservableProperty] private int _updates;
    [ObservableProperty] private int _dlcs;
    [ObservableProperty] private int _unidentified;
    [ObservableProperty] private string _totalSize = "0 B";
    [ObservableProperty] private int _duplicates;
    [ObservableProperty] private int _notRecognised;
    /// <summary>Null until a console with a version database reports a count.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingUpdates))]
    private int? _missingUpdates;
    public bool HasMissingUpdates => MissingUpdates is not null;
    private int _missingUpdatesRun;

    public bool HasConsoles => Consoles.Count > 0;

    public DashboardViewModel(ObservableCollection<ConsoleViewModelBase> consoles, Action<ConsoleViewModelBase> open)
    {
        Consoles = consoles;
        _open = open;
        foreach (var c in consoles) c.PropertyChanged += OnConsoleChanged;
        consoles.CollectionChanged += (_, e) =>
        {
            foreach (ConsoleViewModelBase c in e.NewItems ?? Array.Empty<object>()) c.PropertyChanged += OnConsoleChanged;
            foreach (ConsoleViewModelBase c in e.OldItems ?? Array.Empty<object>()) c.PropertyChanged -= OnConsoleChanged;
            OnPropertyChanged(nameof(HasConsoles));
            ScheduleRecalculate();
        };
        RefreshDats();
    }

    private void OnConsoleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConsoleViewModelBase.RomCount) or nameof(ConsoleViewModelBase.Roms)
            or nameof(ConsoleViewModelBase.DuplicateCount) or nameof(ConsoleViewModelBase.Unrecognized))
            ScheduleRecalculate();
    }

    // Coalesce bursts (a scan replaces the list, then rows change) into one pass.
    private void ScheduleRecalculate()
    {
        if (_pending) return;
        _pending = true;
        Dispatcher.UIThread.Post(Recalculate, DispatcherPriority.Background);
    }

    public void Recalculate()
    {
        _pending = false;
        var roms = Consoles.SelectMany(c => c.Roms).ToList();
        TotalRoms = roms.Count;
        Games = roms.Count(r => r.RomSource is Game);
        Updates = roms.Count(r => r.RomSource is Update);
        Dlcs = roms.Count(r => r.RomSource is DLC);
        Unidentified = roms.Count(r => r.Title == "Unknown" || string.IsNullOrEmpty(r.TitleID));
        TotalSize = FileUtils.FormatFileSize(roms.Sum(r => r.RomSource.Size ?? 0));
        Duplicates = Consoles.Sum(c => c.DuplicateCount);
        NotRecognised = Consoles.Sum(c => c.Unrecognized.Count);
        _ = CountMissingUpdatesAsync();
    }

    // Runs after each recalculation; a newer run discards the result of an older one.
    private async Task CountMissingUpdatesAsync()
    {
        var run = ++_missingUpdatesRun;
        int? total = null;
        foreach (var console in Consoles.ToList())
            if (await console.CountMissingUpdatesAsync() is { } n)
                total = (total ?? 0) + n;
        if (run == _missingUpdatesRun) MissingUpdates = total;
    }

    [RelayCommand]
    private void Open(ConsoleViewModelBase console) => _open(console);

    // ── DAT files (No-Intro / Redump) ─────────────────────────────────────

    [ObservableProperty] private string _datSummary = "";

    public string DatDirectory => RomManagerShared.Utils.Dat.DatIndex.DatDirectory;

    [RelayCommand]
    public void RefreshDats()
    {
        try
        {
            var count = Directory.Exists(DatDirectory)
                ? Directory.EnumerateFiles(DatDirectory, "*.*", SearchOption.AllDirectories)
                    .Count(f => f.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                : 0;
            DatSummary = count == 0
                ? "No DAT files. Add No-Intro DATs to name cartridge ROMs by hash; the next scan applies them."
                : $"{count} DAT file(s). Added or changed DATs are applied on the next scan.";
        }
        catch (Exception ex) { DatSummary = $"Cannot read DAT folder: {ex.Message}"; }
    }

    [RelayCommand]
    private void OpenDatFolder()
    {
        Directory.CreateDirectory(DatDirectory);
        Shell.OpenWithDefaultApp(DatDirectory);
    }
}
