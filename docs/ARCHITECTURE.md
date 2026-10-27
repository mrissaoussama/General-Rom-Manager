# Architecture

State as of 2026-10-27. Target architecture and migration steps are in [ROADMAP.md](ROADMAP.md).

## Projects

```
AvaloniaUI.Desktop ──► AvaloniaUI ──► RomManagerShared ──► Ryujinx.LibHac, LTRData.DiscUtils, CHDSharp (NuGet)
                                                       └─► SkyEditor.IO (vendored, net10.0; 3DS)
RomManagerTests ──► RomManagerShared, AvaloniaUI (view-model tests), SamplePlugin (built, loaded at runtime)
SamplePlugin    ──► RomManagerShared
tools/ParserHarness ──► RomManagerShared
```

Solution: `RomManager.slnx`. Package versions: `Directory.Packages.props`. The desktop head is the only one
(the unmaintained Android and Browser heads were removed; they are in git history).

## Domain model (`RomManagerShared/Base`)

- `GamingConsole` — empty marker base; one subtype per console (`SwitchConsole`, `PS4Console`, …). Used
  only as a generic type argument.
- `Rom` — wide base entity (TitleID, Version, Titles[], Regions[], Languages[], Hashes[], Path, Size,
  Icon, Images…). Subclasses per console and content kind: `Game`, `Update`, `DLC` and
  `<Console>Game/Update/DLC`. `[UiProperty]` drives grid columns.
- `Rom.Equals` = (TitleID, Path).

## Pipeline

```
ConsoleViewModel<T>.LoadAsync
  └─ LibraryScanner.LoadCachedAsync           rows from LibraryFile.RomsJson (instant)
  └─ ConsoleManager<T>.Setup()                title info providers load their databases
  └─ LibraryScanner.ScanAsync
        files under the ROM folder matching parser extensions
        unchanged (size, mtime, ScanVersion, DataFingerprint) → cached roms
        changed/new → parallel batches (MaxParallelism 4):
            ParseDiagnostics.CaptureAsync(RomParserExecutor<T>.ExecuteParsers(file))
            → ConsoleManager<T>.EnrichAsync (title providers, DAT matching)  sequential
            → LibraryFile { RomsJson, Error } saved in batches
        deleted → LibraryFile removed
  └─ RomViewModel per Rom → DataGrid / cards; LibraryWatcher triggers debounced rescans
```

`RomParserExecutor<T>` runs every parser whose extensions match and merges results by TitleID; roms
with no TitleID are dropped. Files that parse to nothing are reported with what the parsers logged.

## Composition (`AvaloniaUI/App.axaml.cs`)

Generic Host + MS DI. `ConsoleModules.Register` (ADR 0003) reflects over `RomManagerShared` and
`Modules/*.dll` and registers, per console type, parsers, title info providers, organizers and the
console manager, all **scoped**. `ConsoleModules.Describe` gives each console its display name,
manufacturer and badge. `AppDbContext` is scoped; migrations run at startup. `PluginManager` is a
singleton. Console view models create a scope per load or action.

## Persistence

SQLite via EF Core (no lazy loading). `LibraryFiles` holds one row per scanned file (path, size, mtime,
scan version, data fingerprint, error, roms as polymorphic JSON from `RomSnapshot`). Title-database
DTOs and hashes have their own tables. `MigrationTests` and CI check for pending model changes.

## Cross-cutting

| Concern | Implementation | Notes |
|---|---|---|
| Logging | `AppLog` over `ILoggerFactory` (the host's in the app); `LogFileProvider` → `LogSink` (file, log panel) | `ParseDiagnostics` captures per-file messages during a scan; `FileUtils.Log` stays as a shim for plugins |
| Config | static `RomManagerConfiguration` (config.json) + `Settings.Instance` (settings.json) | Two sources, static access (ROADMAP 3.3) |
| Operations | `OperationQueue` / `QueuedOperation` with cancel + undo, `RomOperationService` | Rename/move have dry-run previews |
| Requirements | `IHasRequirements` → `ConsoleRequirement` (keys, dirs, DB files) | Surfaced in UI/onboarding |
| Context actions | `IHasContextMenuActions`, `IConsoleRomAction`, `UniversalRomActions` | |
| Missing updates | `IMissingUpdateReporter` (Switch: `SwitchUpdateChecker` + versions.json) | Console page report + dashboard total |
| Plugins | `PluginManager`: collectible `AssemblyLoadContext`, `[Plugin(Permissions = ...)]`, `PluginApiVersion`, approvals pinned to SHA-256 | Permissions are approved, not sandboxed; `IPluginContext` hands out HTTP, files outside the data folder, processes and DB services only for approved permissions |
| Downloads | `FileDownloader` (`[ResourceDownload]` methods) | |
| External tools | Optional, user-supplied (cdecrypt, xextool) under `<BaseDirectory>/Tools` | ADR 0002: no binaries in the repo |

## UI (`AvaloniaApplication1/AvaloniaUI`)

- AXAML views with compiled bindings: `MainView` (menu, sidebar with console badges, bottom panel),
  `DashboardView`, `ConsoleView` (table or cards, filters, context menu), `RomDetailsView`.
  `ViewLocator` maps view models to views and caches them.
- `ConsoleViewModelBase` holds page state (filters, grouping, selection, commands); `ConsoleViewModel<T>`
  adds scanning; `LibraryViewModel` is the all-consoles view.
- View models open dialogs through `IDialogService` (`DialogService.Current`; tests swap in a fake). Dialogs are
  `DialogViewModel`s (confirm, preview, properties, rename, choose console) shown in `DialogWindow`, with views
  in `Views/DialogViews/` resolved by `ViewLocator`. Settings is `SettingsViewModel` (a draft, applied on Save) and the first-run wizard is `OnboardingViewModel`;
  no window is built in code any more.

## Console support matrix (real-file runs 2026-10-25; later formats covered by fixture tests)

| Console | Formats | Status |
|---|---|---|
| Switch | nsp, nsz, xci | NSP/NSZ: LibHac with ticket import (names from NACP; DLC names from titledb). Header scan is the fallback without keys. XCI untested |
| 3DS | cia, 3ds/cci, cxi | CIA: TMD + SMDH. Cartridge images: title ID/product code from the NCCH header (works encrypted); SMDH title/icon only for decrypted dumps. Updates/DLC names from title DB |
| DS / GB / GBC / GBA / SNES / N64 / Saturn(mdf) | header | OK, raw header titles (DS falls back to file name for invalid headers) |
| PS2 | iso, cso, zso, chd | OK via SYSTEM.CNF, ISO9660 + UDF (LTRData.DiscUtils; CHD via CHDSharp) |
| PS3 | PARAM.SFO folder, pkg | OK (managed PKG reader) |
| PS4 | pkg | OK (managed reader: plaintext param.sfo, icon0, pic0/pic1) |
| PSP | iso, cso, pkg | OK (managed SFO + PKG readers) |
| PS Vita | vpk, sfo | vpk ok |
| Wii | wbfs, wad, iso, ciso | OK (region from game ID) |
| GameCube | iso, gcm, ciso | OK |
| Wii U | wux/wud, tmd, xml | TMD: ID/version without keys. WUD/WUX: product code + region; with the disc key (sibling `.key` or `[32-hex]` in the file name) exact title IDs from the partition table. Names via title DB. Decrypted folders: full metadata. cdecrypt optional, user-supplied |
| OG Xbox | iso, xbe | OK; icons cached under Cache/OriginalXbox |
| Xbox 360 | iso, xex | Title ID from XEX header; name/icon only with user-supplied `xextool.exe` |
| PS1 | cue, iso, chd, pbp | Serial from SYSTEM.CNF (2048/2352/2448-byte frames); PBP via the PSP parser |
| NES, SNES, GB/GBC/GBA, N64, Genesis, Master System, Game Gear, PC Engine | header | Header parsers + No-Intro DAT names (user-supplied DATs) |
| Dreamcast | gdi, chd | IP.BIN (GD-ROM). No CDI |
| GameCube/Wii | rvz, wia | Disc header from the uncompressed copy |
