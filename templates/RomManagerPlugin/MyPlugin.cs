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
    Permissions = [])] // add PluginPermission.Network / FileSystem / Database / ExternalProcess as needed
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
            CanExecutePredicate = roms => roms.Any(),
        },
    ];

    public Task InitializeAsync(IPluginContext context) => Task.CompletedTask;

    public Task ExecuteAsync(int optionIndex, IPluginContext context)
    {
        context.Logger.LogInformation("{Count} rom(s) selected", context.SelectedRoms.Count);
        return context.ShowPluginUIAsync($"{context.SelectedRoms.Count} rom(s) selected", PluginName);
    }

    public Task OnScanCompletedAsync(LibraryScanCompleted scan, IPluginContext context)
    {
        context.Logger.LogInformation("{Console}: {Added} added, {Removed} removed", scan.Console, scan.Added.Count, scan.Removed.Count);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
