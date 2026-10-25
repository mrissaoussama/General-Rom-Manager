using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AvaloniaUI.Utils;

/// <summary>StorageProvider wrappers returning local paths.</summary>
internal static class Dialogs
{
    public static async Task<string?> PickFolderAsync(TopLevel owner, string title)
    {
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public static async Task<string?> PickFileAsync(TopLevel owner, string title, string filterName, params string[] extensions)
    {
        var result = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = [.. extensions.Select(e => "*." + e)] }],
        });
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }
}
