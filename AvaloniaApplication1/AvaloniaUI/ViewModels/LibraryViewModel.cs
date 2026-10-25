using System.ComponentModel;

namespace AvaloniaUI.ViewModels;

/// <summary>"All consoles" page: the rows of every console page in one list, with a console column.</summary>
public sealed partial class LibraryViewModel : ConsoleViewModelBase
{
    private readonly ObservableCollection<ConsoleViewModelBase> _consoles;
    private readonly Func<bool, Task> _scanAll;
    private bool _pending;

    public LibraryViewModel(ObservableCollection<ConsoleViewModelBase> consoles, Func<bool, Task> scanAll, ImageCache? images)
        : base("All consoles", images)
    {
        _consoles = consoles;
        _scanAll = scanAll;
        foreach (var c in consoles) c.PropertyChanged += OnConsoleChanged;
        consoles.CollectionChanged += (_, e) =>
        {
            foreach (ConsoleViewModelBase c in e.NewItems ?? Array.Empty<object>()) c.PropertyChanged += OnConsoleChanged;
            foreach (ConsoleViewModelBase c in e.OldItems ?? Array.Empty<object>()) c.PropertyChanged -= OnConsoleChanged;
            Schedule();
        };
        Rebuild();
    }

    private void OnConsoleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RomCount) or nameof(Roms)) Schedule();
        if (e.PropertyName == nameof(IsLoading)) IsLoading = _consoles.Any(c => c.IsLoading);
    }

    private void Schedule()
    {
        if (_pending) return;
        _pending = true;
        Dispatcher.UIThread.Post(Rebuild, DispatcherPriority.Background);
    }

    private void Rebuild()
    {
        _pending = false;
        var selected = SelectedRom;
        Roms = new ObservableCollection<RomViewModel>(_consoles.SelectMany(c => c.Roms));
        if (selected is not null && Roms.Contains(selected)) SelectedRom = selected;
        StatusMessage = $"{Roms.Count:N0} ROMs in {_consoles.Count} consoles";
    }

    private ConsoleViewModelBase? Owner(RomViewModel row) => _consoles.FirstOrDefault(c => c.ConsoleName == row.Console);

    protected override Task LoadAsync(bool fullRescan) => _scanAll(fullRescan);

    public override void RemoveRow(RomViewModel row) => (Owner(row) ?? (ConsoleViewModelBase)this).Roms.Remove(row);

    public override void ReplaceRow(RomViewModel row) => Owner(row)?.ReplaceRow(row);

    public override IReadOnlyList<string> DefaultColumns => ["Console", "Title", "TitleID", "RomType", "Region"];

    public override IReadOnlyList<(string PropName, string Header, bool UsesDictionaryLookup)> GetColumnDescriptors() =>
    [
        ("Console", "Console", false),
        ("TitleID", "Title ID", false),
        ("Title", "Title", false),
        ("RomType", "Type", false),
        ("Region", "Region", false),
        ("Version", "Version", false),
        ("FileName", "File Name", true),
        ("Size", "Size", true),
        ("Publisher", "Publisher", true),
        ("File Path", "File Path", true),
    ];

    // Console-specific actions only apply when the whole selection is from one console.
    public override IEnumerable<IConsoleRomAction> GetContextActionsForSelection(IEnumerable<Rom> selectedRoms)
    {
        var roms = selectedRoms.ToList();
        var owners = Roms.Where(r => roms.Contains(r.RomSource)).Select(Owner).Distinct().ToList();
        return owners is [{ } owner] ? owner.GetContextActionsForSelection(roms) : [];
    }

    public override IReadOnlyList<ConsoleRequirement> GetAllRequirements() => [];
}
