# SamplePlugin — RAWG Metadata Fetcher

A demo RomManager plugin that queries the [RAWG Video Games Database API](https://rawg.io/apidocs)
to fetch game metadata for selected ROMs.

## Features

- **Fetch Metadata** — Looks up the selected ROM on RAWG.io and shows rating, genres, release date, etc.
- **Open RAWG Page** — Opens the RAWG.io search page for the selected ROM in your default browser.

## Setup

1. Register for a **free** RAWG API key at <https://rawg.io/login>.
2. Build the plugin: `dotnet build SamplePlugin/SamplePlugin.csproj`.
3. Copy the output DLL and its dependencies to the RomManager `plugins/` folder
   (default: `plugins/SamplePlugin/SamplePlugin.dll`).
4. Edit the auto-created config file at:
   ```
   %LOCALAPPDATA%\RomManager\plugins\GameMetadataFetcher\config.json
   ```
   Replace `YOUR_RAWG_API_KEY_HERE` with your actual key.
5. Restart RomManager. The options will appear when you right-click a ROM.

## Library hooks

Also implement `ILibraryHooks` to hear about scans: `OnScanCompletedAsync` gets the console, every rom, and
the roms added or removed since the library was last shown. It runs off the UI thread after each scan.

## Permissions

Declare every permission in `[Plugin(Permissions = ...)]`; the user approves them before the plugin
loads, and `IPluginContext` refuses capabilities that were not approved (`PluginPermissionException`).
Plugins run in-process, so this is not a sandbox: it keeps an approved plugin to what it declared.

## Building a Plugin

```csharp
[assembly: PluginApiVersion(PluginApi.Version)]

[Plugin("MyPlugin", "1.0.0", Description = "Does something cool", Permissions = [PluginPermission.Network])]
public sealed class MyPlugin : IPlugin
{
    public string PluginName        => "MyPlugin";
    public string PluginVersion     => "1.0.0";
    public string PluginDescription => "Does something cool";
    public string PluginAuthor      => "Your Name";

    public IReadOnlyList<string>           SupportedConsoles    => ["Switch"]; // or [] for all
    public IReadOnlyList<PluginPermission> RequiredPermissions  => [PluginPermission.Network];
    public IReadOnlyList<PluginOption>     Options              =>
    [
        new PluginOption { Name = "My Action", Description = "Describe what it does" }
    ];

    public Task InitializeAsync(IPluginContext ctx) => Task.CompletedTask;

    public async Task ExecuteAsync(int optionIndex, IPluginContext ctx)
    {
        // ctx.SelectedRoms — the ROMs the user right-clicked
        // ctx.ShowPluginUIAsync(avaloniaControl, "Window Title") — show UI
        // ctx.Services — DI service locator (database services only with Database)
        // ctx.Logger — MEL logger pre-scoped to this plugin
        // ctx.CreateHttpClient() — needs Network
        // ctx.OpenFile(path, mode, access) — free inside ctx.PluginDataDirectory, else needs FileSystem
        // ctx.StartProcess(startInfo) — needs ExternalProcess
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Starting a new plugin

```sh
dotnet new install ./templates/RomManagerPlugin
dotnet new rommanager-plugin -n MyPlugin --RomManagerShared ../RomManager/RomManagerShared/RomManagerShared.csproj
dotnet build MyPlugin
```

The project compiles against `RomManagerShared` without copying it: RomManager supplies it at runtime
(a plugin that ships its own copy still loads; the host's copy is used).
