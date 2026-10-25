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

## Building a Plugin

```csharp
[Plugin("MyPlugin", "1.0.0", Description = "Does something cool")]
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
        // ctx.Services — DI service locator
        // ctx.Logger — MEL logger pre-scoped to this plugin
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```
