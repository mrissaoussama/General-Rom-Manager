# SamplePlugin — RAWG Metadata Fetcher

The reference RomManager plugin, shipped with the app. It looks up the selected games on the
[RAWG Video Games Database](https://rawg.io/apidocs), shows a table of matches and, after you confirm, saves the
description, publisher, developer, release date, genres and cover to the library. Undo is in the Operations panel.

## Features

- **Fetch metadata from RAWG…** (Metadata submenu): looks up up to 50 selected games, shows what was found and
  saves it. Settings choose whether RAWG's name replaces the title and whether covers are downloaded.
- **Open on RAWG**: searches RAWG.io for the selected game in the browser.

## Setup

The plugin is built with the app and copied to `<app>/BuiltInPlugins/SamplePlugin/`; bundled plugins load
without the approval prompt. Enter a free RAWG API key (<https://rawg.io/login>) in
Settings → Plugins → GameMetadataFetcher. The options appear when you right-click a ROM (table or cards).

Plugins you add yourself go in `<data folder>/Plugins/<Name>/` (or Settings → Plugins → Import a zip) and are
approved, with their permissions, before they load.

## The contract (plugin API 1)

```csharp
[assembly: PluginApiVersion(PluginApi.Version)]

[Plugin("MyPlugin", "1.0.0", Description = "Does something cool",
    Permissions = [PluginPermission.Network, PluginPermission.LibraryMetadata])]
public sealed class MyPlugin : IPlugin
{
    public string PluginName        => "MyPlugin";
    public string PluginVersion     => "1.0.0";
    public string PluginDescription => "Does something cool";
    public string PluginAuthor      => "Your Name";

    public IReadOnlyList<string>           SupportedConsoles   => ["Switch"]; // or [] for all
    public IReadOnlyList<PluginPermission> RequiredPermissions => [PluginPermission.Network, PluginPermission.LibraryMetadata];
    public IReadOnlyList<PluginOption>     Options             =>
    [
        new PluginOption
        {
            Name = "My Action",
            Description = "Shown as the tooltip",
            Category = "Metadata",        // context-menu submenu; the plugin's name when null
            IconKey = "icons/action.png", // 16×16 file next to the DLL, or an app resource key
            CanExecutePredicate = roms => roms.Any(r => r is Game),
        },
    ];

    public Task InitializeAsync(IPluginContext ctx) => Task.CompletedTask;

    // Runs in the app's operation queue: Cancel fires the token.
    public async Task ExecuteAsync(int optionIndex, IPluginContext ctx, CancellationToken ct)
    {
        foreach (var (rom, i) in ctx.SelectedRoms.Select((r, i) => (r, i)))
        {
            ct.ThrowIfCancellationRequested();
            await ctx.UpdateMetadataAsync(rom, new RomMetadata { Publisher = "…" }, ct);
            ctx.Progress.Report(100.0 * (i + 1) / ctx.SelectedRoms.Count);
        }
        await ctx.ShowMessageAsync("MyPlugin", "Done");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

What `IPluginContext` offers:

| Member | Notes |
|---|---|
| `SelectedRoms`, `ConsoleId`, `ConsoleName` | Copies of the selection: changing them changes nothing in the library |
| `UpdateMetadataAsync(rom, RomMetadata)` | Title, description, publisher, developer, release date, genres, cover. Saved apart from scan results, so rescans and moved files keep it; undoable. Needs `LibraryMetadata` |
| `Progress` | 0–100, shown in the Operations panel |
| `ShowMessageAsync`, `ShowTableAsync`, `ConfirmAsync` | Dialogs in the app's look, no Avalonia reference needed (`ShowPluginUIAsync` still takes a control) |
| `PickSaveFileAsync` + `OpenFile` | A file the user picked may be written without `FileSystem` |
| `OpenInBrowser(uri)` | http/https only |
| `CreateHttpClient()` | Needs `Network` |
| `OpenFile(path, mode, access)` | Free inside `PluginDataDirectory`, else needs `FileSystem` |
| `StartProcess(startInfo)` | Needs `ExternalProcess` |
| `Services` | Database services only with `Database` |
| `Settings`, `Logger`, `PluginDataDirectory`, `ConfigFilePath` | |

## Settings

Implement `IPluginSettings` to declare options (`PluginSettingDescriptor`: key, label, Text / Secret / Boolean,
default). Settings → Plugins shows them; the values are saved in the plugin's data folder (`settings.json`)
and handed to the plugin as `IPluginContext.Settings`. Secret values are encrypted for the current user (DPAPI)
on Windows; elsewhere the file is made readable by the user only.

## Library hooks

Implement `ILibraryHooks` to hear about the library, off the UI thread, for the consoles in `SupportedConsoles`:

- `OnScanCompletedAsync`: the console, every rom, and the roms added or removed since the library was last shown.
- `OnFilesChangedAsync` (optional, default no-op): files renamed, moved (also by an undo) or deleted from the app.

## Permissions

Declare every permission in `[Plugin(Permissions = ...)]`; the user approves them before the plugin
loads, and `IPluginContext` refuses capabilities that were not approved (`PluginPermissionException`).
Plugins run in-process, so this is not a sandbox: it keeps an approved plugin to what it declared.
`SupportedConsoles` entries that match no installed console are logged when the plugin loads.

## Starting a new plugin

```sh
dotnet new install ./templates/RomManagerPlugin
dotnet new rommanager-plugin -n MyPlugin --Abstractions ../RomManager/RomManager.Abstractions/RomManager.Abstractions.csproj
dotnet build MyPlugin
```

The project compiles against `RomManager.Abstractions` (the plugin contract and rom model) without copying
it: RomManager supplies it at runtime (a plugin that ships its own copy still loads; the host's copy is used).
Plugins built against `RomManagerShared` before the contract moved keep working through type forwarding.
