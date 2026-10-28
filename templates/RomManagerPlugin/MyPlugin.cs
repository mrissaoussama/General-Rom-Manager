using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace MyPlugin;

/// <summary>
/// Build, then copy bin/Debug/net10.0/ into &lt;RomManager data folder&gt;/Plugins/MyPlugin/ (or zip that folder and use
/// Settings → Plugins → Import). RomManager asks you to approve the plugin and its permissions on first load.
/// </summary>
[Plugin("MyPlugin", "1.0.0",
    Description = "Describe what the plugin does",
    Author = "PLUGIN_AUTHOR",
    Permissions = [])] // add Network / FileSystem / Database / ExternalProcess / LibraryMetadata as needed
public sealed class MyPlugin : IPlugin, ILibraryHooks
{
    public string PluginName => "MyPlugin";
    public string PluginVersion => "1.0.0";
    public string PluginDescription => "Describe what the plugin does";
    public string PluginAuthor => "PLUGIN_AUTHOR";

    public IReadOnlyList<string> SupportedConsoles => []; // e.g. ["Switch"]; empty: every console
    public IReadOnlyList<PluginPermission> RequiredPermissions => [];

    public IReadOnlyList<PluginOption> Options =>
    [
        new PluginOption
        {
            Name = "Count selected roms",
            Description = "Shows how many roms are selected",
            Category = "MyPlugin", // context-menu submenu; the plugin's name when null
            CanExecutePredicate = roms => roms.Any(),
        },
    ];

    public Task InitializeAsync(IPluginContext context) => Task.CompletedTask;

    // Runs in the app's operation queue: report progress with context.Progress and stop when the token fires.
    // context.SelectedRoms are copies; save metadata with context.UpdateMetadataAsync (needs LibraryMetadata).
    public Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("{Count} rom(s) selected on {Console}", context.SelectedRoms.Count, context.ConsoleName);
        return context.ShowMessageAsync(PluginName, $"{context.SelectedRoms.Count} rom(s) selected");
    }

    public Task OnScanCompletedAsync(LibraryScanCompleted scan, IPluginContext context)
    {
        context.Logger.LogInformation("{Console}: {Added} added, {Removed} removed", scan.Console, scan.Added.Count, scan.Removed.Count);
        return Task.CompletedTask;
    }

    public Task OnFilesChangedAsync(LibraryFilesChanged change, IPluginContext context)
    {
        context.Logger.LogInformation("{Kind}: {Count} file(s)", change.Kind, change.Files.Count);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
