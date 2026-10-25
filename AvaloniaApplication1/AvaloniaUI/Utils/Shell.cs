using Avalonia.Controls.ApplicationLifetimes;

namespace AvaloniaUI.Utils;

/// <summary>Window-level services (clipboard, dialogs, shell open) for view models.</summary>
internal static class Shell
{
    public static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static async Task CopyAsync(string text)
    {
        if (MainWindow?.Clipboard is { } clipboard && !string.IsNullOrEmpty(text))
            await clipboard.SetTextAsync(text);
    }

    public static Task<string?> PickFolderAsync(string title) =>
        MainWindow is { } owner ? Dialogs.PickFolderAsync(owner, title) : Task.FromResult<string?>(null);

    public static async Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK")
    {
        if (MainWindow is not { } owner) return false;
        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var ok = new Button { Content = confirmText, IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        ok.Click += (_, _) => { result = true; dialog.Close(); };
        cancel.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { cancel, ok },
                },
            },
        };
        await dialog.ShowDialog(owner);
        return result;
    }

    public static Task ShowDialogAsync(Window window) =>
        MainWindow is { } owner ? window.ShowDialog(owner) : Task.CompletedTask;

    public static void RevealInExplorer(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return;
        try
        {
            if (OperatingSystem.IsWindows())
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            else
                OpenWithDefaultApp(Path.GetDirectoryName(path)!);
        }
        catch (Exception ex) { FileUtils.Log($"Cannot reveal '{path}': {ex.Message}"); }
    }

    public static void OpenWithDefaultApp(string path)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { FileUtils.Log($"Cannot open '{path}': {ex.Message}"); }
    }
}
