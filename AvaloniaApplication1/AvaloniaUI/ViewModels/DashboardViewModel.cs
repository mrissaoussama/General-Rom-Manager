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
    }

    private void OnConsoleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConsoleViewModelBase.RomCount) or nameof(ConsoleViewModelBase.Roms))
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
    }

    [RelayCommand]
    private void Open(ConsoleViewModelBase console) => _open(console);
}
