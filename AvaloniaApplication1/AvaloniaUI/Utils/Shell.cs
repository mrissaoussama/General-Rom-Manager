using Avalonia.Controls.ApplicationLifetimes;

namespace AvaloniaUI.Utils;

/// <summary>Window-level services (clipboard, shell open, export) for view models. Dialogs: <see cref="IDialogService"/>.</summary>
internal static class Shell
{
    public static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static async Task CopyAsync(string text)
    {
        if (MainWindow?.Clipboard is { } clipboard && !string.IsNullOrEmpty(text))
            await clipboard.SetTextAsync(text);
    }

    /// <summary>Asks for a CSV or JSON path and writes the rows there; returns the path or null when cancelled.</summary>
    public static async Task<string?> ExportAsync(IDialogService dialogs, string suggestedName, IReadOnlyList<RomManagerShared.Utils.ExportRow> rows)
    {
        var path = await dialogs.PickSaveFileAsync("Export", suggestedName, ("CSV", "csv"), ("JSON", "json"));
        if (path is null) return null;
        await Task.Run(() => RomManagerShared.Utils.LibraryExporter.Export(rows, path));
        return path;
    }

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
        catch (Exception ex) { AppLog.Warn($"Cannot reveal '{path}': {ex.Message}"); }
    }

    public static void OpenWithDefaultApp(string path)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { AppLog.Warn($"Cannot open '{path}': {ex.Message}"); }
    }
}
