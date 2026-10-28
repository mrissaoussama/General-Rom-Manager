using RomManagerShared.Utils;

namespace RomManagerTests;

/// <summary>Records what plugins showed; answers confirmations and save pickers as configured.</summary>
public sealed class FakePluginHost : IPluginHost
{
    public bool ConfirmAnswer { get; set; } = true;
    public string? SavePath { get; set; }
    public List<string> Messages { get; } = [];
    public List<(string Title, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows)> Tables { get; } = [];
    public List<Uri> Opened { get; } = [];

    public Task ShowUIAsync(object control, string title) => Task.CompletedTask;
    public Task ShowMessageAsync(string title, string message) { Messages.Add(message); return Task.CompletedTask; }
    public Task ShowTableAsync(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Tables.Add((title, columns, rows));
        return Task.CompletedTask;
    }
    public Task<bool> ConfirmAsync(string title, string message, string? confirmText) => Task.FromResult(ConfirmAnswer);
    public Task<string?> PickSaveFileAsync(string title, string suggestedName, string extension) => Task.FromResult(SavePath);
    public void OpenInBrowser(Uri url) => Opened.Add(url);
}
