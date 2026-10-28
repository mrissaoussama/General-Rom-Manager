namespace RomManagerShared.Utils;

/// <summary>
/// What the app provides to plugins' dialogs (implemented by the UI; tests use a fake). Calls may come from any
/// thread; the implementation moves to the UI thread itself.
/// </summary>
public interface IPluginHost
{
    Task ShowUIAsync(object control, string title);
    Task ShowMessageAsync(string title, string message);
    Task ShowTableAsync(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows);
    Task<bool> ConfirmAsync(string title, string message, string? confirmText);
    Task<string?> PickSaveFileAsync(string title, string suggestedName, string extension);
    void OpenInBrowser(Uri url);
}
