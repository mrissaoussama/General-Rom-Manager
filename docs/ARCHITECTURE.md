# Architecture

State as of 2026-10-25. Target architecture and migration steps are in [ROADMAP.md](ROADMAP.md).

## Projects

```
AvaloniaUI.Desktop ──► AvaloniaUI ──► RomManagerShared ──► Ryujinx.LibHac (NuGet, Switch)
                                                       └─► SkyEditor.IO (vendored, 3DS)
RomManagerTests ──► RomManagerShared
SamplePlugin    ──► RomManagerShared
```

Unmaintained: `AvaloniaUI.Android`, `AvaloniaUI.Browser` (net9.0-*).

## Domain model (`RomManagerShared/Base`)

- `GamingConsole` — empty marker base; one subtype per console (`SwitchConsole`, `PS4Console`, …). Used
  only as a generic type argument.
- `Rom` — wide base entity (TitleID, Version, Titles[], Regions[], Languages[], Hashes[], Path, Size,
  Icon, Images…). Subclasses per console and content kind: `Game`, `Update`, `DLC` and
  `<Console>Game/Update/DLC`. `[UiProperty]` drives grid columns.
- `Rom.Equals` = (TitleID, Path).

## Pipeline

```
ConsoleViewModel<T>.LoadData
  └─ ConsoleManager<T>.Setup()            TitleInfoProviderManager<T>.Setup → each provider loads its DB
  └─ FileUtils.GetFilesInDirectoryWithExtensions(romPath, parser extensions)   (recursive)
  └─ foreach file: ConsoleManager<T>.ProcessFile
        └─ RomParserExecutor<T>.ExecuteParsers(file)
              every parser whose Extensions contains ext → List<Rom>; merged by TitleID
        └─ RomList.AddRange
  └─ RomViewModel per Rom → DataGrid
```

Title enrichment (`TitleInfoProviderManager<T>.GetTitleInfo`) runs providers in sequence; each either
mutates the rom or returns a new one that is merged back.

## Composition (`AvaloniaUI/App.axaml.cs`)

Generic Host + MS DI. `RegisterConsoleServices` reflects over `RomManagerShared` and registers open
generics closed over every console type, all **scoped**. `AppDbContext` is scoped, configured in
`OnConfiguring` (SQLite path from config, lazy-loading proxies). `PluginManager` is a singleton.
`MainViewModel` gets one long-lived scope; `ConsoleViewModel` creates a new scope per load/action.

## Persistence

`AppDbContext` (split into partials per vendor: Nintendo, Sony, Microsoft, NoIntro) holds title-database
DTOs and hashes. Single migration `20240225212403_InitialCreate` — the model has changed since, so
there is likely pending model drift. The scanned **library is not persisted**.

## Cross-cutting

| Concern | Implementation | Notes |
|---|---|---|
| Logging | static `FileUtils.Log` + `FileUtils.Logger` bridged to MEL | Parsers swallow exceptions and log; also many stray `Console.WriteLine` |
| Config | static `RomManagerConfiguration` (config.json) + `Settings.Instance` (settings.json) | Two sources, static access |
| Operations | `OperationQueue` / `QueuedOperation` with cancel + undo, `RomOperationService` | |
| Requirements | `IHasRequirements` → `ConsoleRequirement` (keys, dirs, DB files) | Surfaced in UI/onboarding |
| Context actions | `IHasContextMenuActions`, `IConsoleRomAction`, `UniversalRomActions` (dupes, rename, hash) | |
| Plugins | `PluginManager`: collectible `AssemblyLoadContext`, `[Plugin]`, `IPlugin`, declared `PluginPermission`s, zip import | Permissions are declarative only (not enforced) |
| Downloads | `FileDownloader`, vendored Alto multi-thread downloader | |
| External tools | Python `PSN_get_pkg_info.py` (+ runtime `pip install`), `WiiU/Parsers/cdecrypt.exe` | To be removed |

## UI (`AvaloniaApplication1/AvaloniaUI`)

- `MainView.axaml.cs` (~1,900 lines) builds the whole shell in C#: menu buttons, tabs per console,
  DataGrid, details pane, bottom panel (log/downloads/operations), context menus, rename dialog.
- Other windows (`SettingsWindow`, `OnboardingWindow`, `DownloadManagerWindow`, `LogWindow`) are also
  code-built.
- ViewModels use CommunityToolkit.Mvvm source generators. Grid columns come from
  `IConsoleViewModel.GetColumnDescriptors()`.

## Console support matrix (tested against real files, 2026-10-25, after phase 2 fixes)

| Console | Formats | Status |
|---|---|---|
| Switch | nsp, xci, nsz | NSP: LibHac with ticket import (names from NACP). NSZ: IDs/versions from header scan. XCI untested |
| 3DS | cia | OK; updates/DLC have no SMDH, titles come from title DB |
| DS / GB / GBC / GBA / SNES / N64 / Saturn(mdf) | header | OK, raw header titles (DS falls back to file name for invalid headers) |
| PS2 | iso | OK via SYSTEM.CNF, ISO9660 + UDF. No CHD/CSO |
| PS3 | PARAM.SFO folder, pkg | OK (managed PKG reader) |
| PS4 | pkg | OK (PS4_Tools) |
| PSP | iso, pkg | OK (managed SFO + PKG readers) |
| PS Vita | vpk, sfo | vpk ok |
| Wii | wbfs, wad, iso, ciso | OK (region from game ID) |
| GameCube | iso, gcm, ciso | OK |
| Wii U | wux/wud, tmd, xml | TMD: ID/version without keys. WUD/WUX: product code + region; with the disc key (sibling `.key` or `[32-hex]` in the file name) exact title IDs from the partition table. Names via title DB. Decrypted folders: full metadata. cdecrypt optional, user-supplied |
| OG Xbox | iso, xbe | OK; icons cached under Cache/OriginalXbox |
| Xbox 360 | iso, xex | Title ID from XEX header; name/icon only with user-supplied `xextool.exe` |
| PS1 | cue, iso | Serial from SYSTEM.CNF (raw 2352 or 2048 sectors). No CHD/PBP |
| NES, Genesis… | — | Not supported |
