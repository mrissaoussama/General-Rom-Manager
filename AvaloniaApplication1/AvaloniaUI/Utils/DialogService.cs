namespace AvaloniaUI.Utils;

/// <summary>
/// Dialogs and pickers for view models, so they never construct windows themselves and tests can
/// answer them. <see cref="DialogService.Current"/> is the instance view models use.
/// </summary>
public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string? confirmText = null);

    /// <summary>Shows a one-line-per-change list; true when the user confirmed.</summary>
    Task<bool> PreviewAsync(string title, string summary, IEnumerable<string> lines, string confirmText, bool showCancel = true);

    /// <summary>The rom's details; clicking a related rom closes the dialog and calls <paramref name="navigate"/>.</summary>
    Task ShowPropertiesAsync(RomViewModel rom, ImageCache? images = null, Action<Rom>? navigate = null);

    Task<ConsoleDescriptor?> ChooseConsoleAsync(string folder, IReadOnlyList<ConsoleDescriptor> consoles, ConsoleDescriptor? guess, string detail);

    /// <summary>Asks for a rename template (with a live preview); null when cancelled.</summary>
    Task<string?> AskRenameTemplateAsync(IReadOnlyList<Rom> roms, IEnumerable<string> consoleProperties, string consoleName, string template);

    Task<string?> PickFolderAsync(string title);

    Task<string?> PickFileAsync(string title, string filterName, params string[] extensions);

    /// <summary>Any number of files; empty when cancelled.</summary>
    Task<IReadOnlyList<string>> PickFilesAsync(string title);

    /// <summary>Shows any <see cref="DialogViewModel"/>; true when the user confirmed.</summary>
    Task<bool> ShowAsync(DialogViewModel dialog);

    /// <summary>Save dialog offering each (name, extension) pair as a file type; null when cancelled.</summary>
    Task<string?> PickSaveFileAsync(string title, string suggestedName, params (string Name, string Extension)[] types);

}

public static class DialogService
{
    /// <summary>The service view models use; tests replace it.</summary>
    public static IDialogService Current { get; set; } = new AvaloniaDialogService();
}

/// <summary>Shows dialogs modally over the main window.</summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private static Window? Owner => Shell.MainWindow;

    /// <summary>Shows <paramref name="vm"/> in a <see cref="DialogWindow"/>; false when there is no main window.</summary>
    public async Task<bool> ShowAsync(DialogViewModel vm)
    {
        if (Owner is not { } owner) return false;
        await new DialogWindow(vm).ShowDialog(owner);
        return vm.Confirmed;
    }

    public Task<bool> ConfirmAsync(string title, string message, string? confirmText = null) =>
        ShowAsync(new ConfirmDialogViewModel(title, message, confirmText ?? Strings.Common_OK));

    public Task<bool> PreviewAsync(string title, string summary, IEnumerable<string> lines, string confirmText, bool showCancel = true) =>
        ShowAsync(new PreviewDialogViewModel(title, summary, lines, confirmText, showCancel));

    public Task ShowPropertiesAsync(RomViewModel rom, ImageCache? images = null, Action<Rom>? navigate = null) =>
        ShowAsync(new PropertiesDialogViewModel(rom, images, navigate));

    public async Task<ConsoleDescriptor?> ChooseConsoleAsync(string folder, IReadOnlyList<ConsoleDescriptor> consoles,
        ConsoleDescriptor? guess, string detail)
    {
        var vm = new ChooseConsoleDialogViewModel(folder, consoles, guess, detail);
        return await ShowAsync(vm) ? vm.Selected : null;
    }

    public async Task<string?> AskRenameTemplateAsync(IReadOnlyList<Rom> roms, IEnumerable<string> consoleProperties,
        string consoleName, string template)
    {
        var vm = new RenameDialogViewModel(roms, consoleProperties, consoleName, template);
        return await ShowAsync(vm) ? vm.Template : null;
    }

    public Task<string?> PickFolderAsync(string title) =>
        Owner is { } owner ? Dialogs.PickFolderAsync(owner, title) : Task.FromResult<string?>(null);

    public Task<string?> PickFileAsync(string title, string filterName, params string[] extensions) =>
        Owner is { } owner ? Dialogs.PickFileAsync(owner, title, filterName, extensions) : Task.FromResult<string?>(null);

    public Task<IReadOnlyList<string>> PickFilesAsync(string title) =>
        Owner is { } owner ? Dialogs.PickFilesAsync(owner, title) : Task.FromResult<IReadOnlyList<string>>([]);

    public Task<string?> PickSaveFileAsync(string title, string suggestedName, params (string Name, string Extension)[] types) =>
        Owner is { } owner ? Dialogs.SaveFileAsync(owner, title, suggestedName, types) : Task.FromResult<string?>(null);
}
