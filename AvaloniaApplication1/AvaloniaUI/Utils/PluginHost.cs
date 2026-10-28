namespace AvaloniaUI.Utils;

/// <summary>Plugins' dialogs, shown with the app's own dialog views on the UI thread.</summary>
public sealed class PluginHost(Func<IDialogService> dialogs) : IPluginHost
{
    public Task ShowUIAsync(object control, string title) => OnUi(() => ShowWindow(control, title));

    public Task ShowMessageAsync(string title, string message) =>
        OnUi(() => dialogs().ShowAsync(new ConfirmDialogViewModel(title, message, Strings.Console_Close, showCancel: false)));

    public Task ShowTableAsync(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows) =>
        OnUi(() => dialogs().ShowAsync(new TableDialogViewModel(title, "", columns, rows)));

    public Task<bool> ConfirmAsync(string title, string message, string? confirmText) =>
        OnUi(() => dialogs().ConfirmAsync(title, message, confirmText));

    public Task<string?> PickSaveFileAsync(string title, string suggestedName, string extension) =>
        OnUi(() => dialogs().PickSaveFileAsync(title, suggestedName, (extension.ToUpperInvariant(), extension)));

    public void OpenInBrowser(Uri url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { AppLog.Warn($"[Plugins] cannot open {url}: {ex.Message}"); }
    }

    private static Task<T> OnUi<T>(Func<Task<T>> show) =>
        Dispatcher.UIThread.CheckAccess() ? show() : Dispatcher.UIThread.InvokeAsync(show);

    private static Task OnUi(Func<Task> show) =>
        Dispatcher.UIThread.CheckAccess() ? show() : Dispatcher.UIThread.InvokeAsync(show);

    // A plugin's own Avalonia control (or a string) in a window owned by the main window.
    private static Task ShowWindow(object control, string title)
    {
        Control content = control switch
        {
            Control c => c,
            _ => new ScrollViewer
            {
                Content = new SelectableTextBlock
                {
                    Text = control?.ToString() ?? Strings.App_Empty,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(16),
                    FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
                    FontSize = 13,
                },
            },
        };
        var win = new Window
        {
            Title = title,
            Content = content,
            Width = 640,
            Height = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        if (Shell.MainWindow is { } owner) win.Show(owner);
        else win.Show();
        return Task.CompletedTask;
    }
}
