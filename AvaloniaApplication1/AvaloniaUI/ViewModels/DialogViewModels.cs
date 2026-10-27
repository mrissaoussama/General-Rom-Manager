
namespace AvaloniaUI.ViewModels;

/// <summary>
/// Base for modal dialogs shown by <see cref="IDialogService"/> in a <see cref="DialogWindow"/>.
/// <see cref="CloseRequested"/> asks the hosting window to close; <see cref="Confirmed"/> is the answer.
/// </summary>
public abstract partial class DialogViewModel : ViewModelBase
{
    public abstract string Title { get; }
    public virtual double Width => 520;
    /// <summary>Fixed height, or NaN to size to content.</summary>
    public virtual double Height => double.NaN;
    public virtual bool CanResize => false;

    public bool Confirmed { get; private set; }
    public event Action? CloseRequested;

    protected virtual bool CanConfirm() => true;

    /// <summary>Called by the hosting window once it has closed (by a button, Escape or the title bar).</summary>
    public virtual void OnClosed() { }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        Confirmed = true;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}

public sealed class ConfirmDialogViewModel(string title, string message, string confirmText) : DialogViewModel
{
    public override string Title => title;
    public override double Width => 420;
    public string Message => message;
    public string ConfirmText => confirmText;
}

/// <summary>Lists planned changes (one line each) and asks to go ahead.</summary>
public sealed class PreviewDialogViewModel(string title, string summary, IEnumerable<string> lines,
    string confirmText, bool showCancel = true) : DialogViewModel
{
    public override string Title => title;
    public override double Width => 820;
    public override double Height => 480;
    public override bool CanResize => true;
    public string Summary => summary;
    public IReadOnlyList<string> Lines { get; } = [.. lines];
    public string ConfirmText => confirmText;
    public bool ShowCancel => showCancel;
}

/// <summary>Read-only list of every property of a rom.</summary>
public sealed class PropertiesDialogViewModel(RomViewModel rom) : DialogViewModel
{
    public override string Title => string.Format(Strings.Dialogs_Properties, rom.Title);
    public override double Width => 560;
    public override double Height => 520;
    public override bool CanResize => true;
    public IReadOnlyList<DetailRow> Rows { get; } = [.. rom.Properties.Select(kv => new DetailRow(Labels.Property(kv.Key), kv.Value))];
}

/// <summary>Asks which console a dropped folder holds, preselecting the detected one.</summary>
public sealed partial class ChooseConsoleDialogViewModel : DialogViewModel
{
    public ChooseConsoleDialogViewModel(string folder, IReadOnlyList<ConsoleDescriptor> consoles, ConsoleDescriptor? guess, string detail)
    {
        Folder = folder;
        Detail = detail;
        Consoles = consoles;
        _selected = guess is null ? null : consoles.FirstOrDefault(c => c.ConsoleType == guess.ConsoleType);
    }

    public override string Title => Strings.Dialogs_AddROMFolder;
    public string Folder { get; }
    public string Detail { get; }
    public IReadOnlyList<ConsoleDescriptor> Consoles { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private ConsoleDescriptor? _selected;

    protected override bool CanConfirm() => Selected is not null;
}

/// <summary>Rename template editor with a live dry run of the real renamer.</summary>
public sealed partial class RenameDialogViewModel : DialogViewModel
{
    private static readonly string[] GeneralPlaceholders =
    [
        "{TitleName}", "{TitleID}", "{Region}", "{Version}", "{Publisher}", "{Developer}", "{ReleaseDate}",
        "{Platform}", "{ProductCode}", "{Size}", "{Genres}", "{Players}", "{Type}", "{MinFirmware}", "{DLCCount}",
    ];

    public const int PreviewLimit = 50;
    private readonly IReadOnlyList<Rom> _roms;
    private bool _valid = true;

    public RenameDialogViewModel(IReadOnlyList<Rom> roms, IEnumerable<string> consoleProperties, string consoleName, string template)
    {
        _roms = roms;
        ConsoleName = consoleName;
        ConsolePlaceholders = [.. consoleProperties.Take(12).Select(p => "{" + p + "}")];
        _template = template;
        UpdatePreview();
    }

    public override string Title => _roms.Count == 1 ? Strings.Dialogs_Rename : string.Format(Strings.Dialogs_RenameROMs, _roms.Count);
    public override double Width => 760;
    public string ConsoleName { get; }
    public IReadOnlyList<string> General => GeneralPlaceholders;
    public IReadOnlyList<string> ConsolePlaceholders { get; }
    public bool HasConsolePlaceholders => ConsolePlaceholders.Count > 0;
    public ObservableCollection<string> PreviewRows { get; } = [];

    [ObservableProperty] private string _template;
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private int _caretIndex;

    partial void OnTemplateChanged(string value) => UpdatePreview();

    protected override bool CanConfirm() => _valid;

    /// <summary>Inserts a placeholder at the caret.</summary>
    [RelayCommand]
    private void Insert(string placeholder)
    {
        var pos = Math.Clamp(CaretIndex, 0, Template.Length);
        Template = Template.Insert(pos, placeholder);
        CaretIndex = pos + placeholder.Length;
    }

    private void UpdatePreview()
    {
        PreviewRows.Clear();
        try
        {
            var results = FileRenamer.PreviewRename(_roms.Take(PreviewLimit), Template);
            foreach (var r in results)
                PreviewRows.Add(r.SourcePath == r.DestinationPath
                    ? string.Format(Strings.Dialogs_Unchanged, Path.GetFileName(r.SourcePath))
                    : $"{Path.GetFileName(r.SourcePath)}  →  {Path.GetFileName(r.DestinationPath)}");
            var changed = results.Count(r => r.SourcePath != r.DestinationPath);
            Summary = string.Format(Strings.Dialogs_OfFileSChange, changed, results.Count) + (_roms.Count > PreviewLimit ? string.Format(Strings.Dialogs_FirstShown, PreviewLimit) : "");
            _valid = true;
        }
        catch (Exception ex)
        {
            Summary = string.Format(Strings.Dialogs_InvalidTemplate, ex.Message);
            _valid = false;
        }
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
