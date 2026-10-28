
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

/// <summary>A question with Cancel and a confirm button; without Cancel, a message.</summary>
public sealed class ConfirmDialogViewModel(string title, string message, string confirmText, bool showCancel = true) : DialogViewModel
{
    public override string Title => title;
    public override double Width => 420;
    public string Message => message;
    public string ConfirmText => confirmText;
    public bool ShowCancel => showCancel;
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

/// <summary>Rows of text under column headers (plugin results); Copy puts them on the clipboard as tab-separated text.</summary>
public sealed partial class TableDialogViewModel(string title, string summary, IReadOnlyList<string> columns,
    IReadOnlyList<IReadOnlyList<string>> rows) : DialogViewModel
{
    public override string Title => title;
    public override double Width => 820;
    public override double Height => 480;
    public override bool CanResize => true;
    public string Summary => summary;
    public bool HasSummary => summary.Length > 0;
    public IReadOnlyList<string> Columns => columns;
    // Short rows are padded so every column binds.
    public IReadOnlyList<string[]> Rows { get; } = [.. rows.Select(r => Enumerable.Range(0, columns.Count).Select(i => i < r.Count ? r[i] : "").ToArray())];

    public string AsText() => string.Join(Environment.NewLine,
        new[] { string.Join('\t', Columns) }.Concat(Rows.Select(r => string.Join('\t', r))));

    [RelayCommand]
    private Task Copy() => Shell.CopyAsync(AsText());
}

/// <summary>Read-only list of every property of a rom.</summary>
public sealed class PropertiesDialogViewModel : DialogViewModel
{
    /// <param name="navigate">Selects a related rom in the page; the dialog closes first. Null hides nothing, clicks do nothing.</param>
    public PropertiesDialogViewModel(RomViewModel rom, ImageCache? images = null, Action<Rom>? navigate = null)
    {
        Rom = rom;
        Details = new RomDetailsViewModel(rom, images);
        Details.NavigateRequested += related =>
        {
            CancelCommand.Execute(null);
            navigate?.Invoke(related);
        };
    }

    public RomViewModel Rom { get; }
    public override string Title => string.Format(Strings.Dialogs_Properties, Rom.Title);
    public override double Width => 600;
    public override double Height => 620;
    public override bool CanResize => true;
    /// <summary>The same content as the side panel.</summary>
    public RomDetailsViewModel Details { get; }

    public override void OnClosed() => Details.Dispose();
}

/// <summary>Where to move roms: one of the console's library folders, or another folder (<see cref="PickOther"/>).</summary>
public sealed partial class ChooseFolderDialogViewModel : DialogViewModel
{
    public ChooseFolderDialogViewModel(string title, string message, IReadOnlyList<string> folders)
    {
        Title = title;
        Message = message;
        Folders = folders;
        _selected = folders.FirstOrDefault();
    }

    public override string Title { get; }
    public override double Width => 560;
    public string Message { get; }
    public IReadOnlyList<string> Folders { get; }
    /// <summary>The user chose "Other folder…": the caller shows a folder picker.</summary>
    public bool PickOther { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _selected;

    protected override bool CanConfirm() => Selected is not null;

    [RelayCommand]
    private void Other()
    {
        PickOther = true;
        Selected = null;
        CancelCommand.Execute(null);
    }
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
    public const int PreviewLimit = 50;
    private readonly IReadOnlyList<Rom> _roms;
    private bool _valid = true;

    /// <param name="consolePlaceholders">The console's own placeholders, e.g. "{Firmware}" (<see cref="FileRenamer.ConsolePlaceholders"/>).</param>
    public RenameDialogViewModel(IReadOnlyList<Rom> roms, IEnumerable<string> consolePlaceholders, string consoleName, string template)
    {
        _roms = roms;
        ConsoleName = consoleName;
        ConsolePlaceholders = [.. consolePlaceholders.Except(FileRenamer.GeneralPlaceholders, StringComparer.OrdinalIgnoreCase)];
        _template = template;
        UpdatePreview();
    }

    public override string Title => _roms.Count == 1 ? Strings.Dialogs_Rename : string.Format(Strings.Dialogs_RenameROMs, _roms.Count);
    public override double Width => 760;
    public string ConsoleName { get; }
    public IReadOnlyList<string> General => FileRenamer.GeneralPlaceholders;
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
                PreviewRows.Add(!r.Success
                    ? string.Format(Strings.Dialogs_NotRenamed, Path.GetFileName(r.SourcePath), r.ErrorMessage)
                    : r.SourcePath == r.DestinationPath
                    ? string.Format(Strings.Dialogs_Unchanged, Path.GetFileName(r.SourcePath))
                    : $"{Path.GetFileName(r.SourcePath)}  →  {Path.GetFileName(r.DestinationPath)}");
            var changed = results.Count(r => r.Success && r.SourcePath != r.DestinationPath);
            Summary = string.Format(Strings.Dialogs_OfFileSChange, changed, results.Count) + (_roms.Count > PreviewLimit ? string.Format(Strings.Dialogs_FirstShown, PreviewLimit) : "");
            // A typo would otherwise end up in every file name.
            var unknown = _roms.SelectMany(r => FileRenamer.UnknownPlaceholders(r, Template)).Distinct().ToList();
            if (unknown.Count > 0)
                Summary = string.Format(Strings.Dialogs_UnknownPlaceholders, string.Join(", ", unknown));
            _valid = unknown.Count == 0;
        }
        catch (Exception ex)
        {
            Summary = string.Format(Strings.Dialogs_InvalidTemplate, ex.Message);
            _valid = false;
        }
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
