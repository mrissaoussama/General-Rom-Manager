# RomManager — Comprehensive Project Analysis

> Generated: March 1, 2026  
> Last updated: March 2026 (net10.0 migration + UI improvements)  
> Current SDK target: **.NET 10** (`10.0.103` pinned in `global.json`, all projects on `net10.0`)  
> Status: **Building successfully**, 32/32 tests passing on net10.0

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Solution Structure](#2-solution-structure)
3. [Core Philosophy](#3-core-philosophy)
4. [Architecture Deep-Dive](#4-architecture-deep-dive)
5. [Supported Platforms](#5-supported-platforms)
6. [Plugin System](#6-plugin-system)
7. [Database Layer](#7-database-layer)
8. [UI Layer](#8-ui-layer)
9. [Technology Stack](#9-technology-stack)
10. [Good Things in the Architecture](#10-good-things-in-the-architecture)
11. [Downsides in the Architecture](#11-downsides-in-the-architecture)
12. [Points to Fix (Bugs / Broken Code)](#12-points-to-fix-bugs--broken-code)
13. [Points to Improve (Code Quality / Design)](#13-points-to-improve-code-quality--design)
14. [Possible Future Functionalities](#14-possible-future-functionalities)
15. [Benefits of Upgrading to .NET 10](#15-benefits-of-upgrading-to-net-10)

---

## 1. Project Overview

**RomManager** is a cross-platform ROM management application that aggregates, parses, enriches, organises, and persists metadata for game ROMs across more than 18 retro and modern gaming platforms. Its goal is to be a universal cartridge/disc/digital library manager — conceptually similar to EmulationStation, LaunchBox, or Retool, but built code-first with a strongly-typed, extensible .NET foundation.

Users can scan ROM directories, identify titles via file-format parsing or hash-lookup, enrich metadata from external sources (community JSON databases, XML databases, web scraping), detect missing updates/DLC, and browse the resulting collection from a cross-platform GUI.

---

## 2. Solution Structure

```
RomManager.sln
│
├── RomManagerShared/            # Core library — shared across all front-ends
│   ├── Base/                    # Domain model (Rom, Game, Update, DLC, interfaces)
│   │   ├── Interfaces/          # All core abstraction contracts
│   │   └── Database/            # EF Core DbContext + Repository helpers
│   ├── Switch/ 3DS/ DS/ … /     # One folder per gaming platform
│   │   ├── Parsers/             # IRomParser<T> implementations
│   │   ├── TitleInfoProviders/  # ITitleInfoProvider<T> implementations
│   │   ├── *Manager.cs          # ConsoleManager<T> sub-class
│   │   └── *Rom.cs              # Platform-specific Rom sub-class
│   ├── Utils/                   # Cross-cutting utilities
│   │   ├── PluginManager.cs     # Assembly-reflection plugin loader
│   │   ├── FileUtils.cs         # File I/O + logging
│   │   ├── HashUtils.cs         # CRC32 / MD5 / SHA1 / SHA256
│   │   ├── NoIntroRomHashIdentifier.cs  # No-Intro DAT matching
│   │   ├── DownloadManager.cs   # HTTP download helpers
│   │   └── …                    # Other utility classes
│   └── Migrations/              # EF Core migration history
│
├── AvaloniaApplication1/
│   ├── AvaloniaUI/              # Shared Avalonia UI library (MVVM, Views)
│   │   ├── ViewModels/          # CommunityToolkit.Mvvm ViewModels
│   │   ├── Views/               # .axaml XAML views
│   │   ├── DynamicRomPropertyUI.cs  # Attribute-driven control factory
│   │   └── ViewLocator.cs
│   ├── AvaloniaUI.Desktop/      # Windows / macOS / Linux entry-point
│   ├── AvaloniaUI.Android/      # Android entry-point
│   └── AvaloniaUI.Browser/      # WebAssembly entry-point
│
├── RomManagerMaui/              # .NET MAUI entry-point (iOS, Android, Windows)
├── LibHac/                      # Vendored / forked LibHac (Nintendo crypto/fs)
├── SkyEditor.IO/                # Vendored SkyEditor binary I/O helpers
├── ConsoleTest2/                # Ad-hoc console test harness
└── testconsole/                 # Another test/debug console project
```

---

## 3. Core Philosophy

### "One model, many consoles"
The entire domain is generalised through a single `Rom` base class and a generic `ConsoleManager<T>` where `T : GamingConsole`. Every console is just a marker type (e.g. `SwitchConsole`, `PS4Console`). This allows parsers, title info providers, and managers to be typed and resolved at compile or DI-container time while sharing a single execution pipeline.

### "Data enrichment pipeline"
Parsing and metadata enrichment are decoupled. A file first enters a `RomParserExecutor<T>`, which runs all compatible `IRomParser<T>` implementations and merges their outputs into one `Rom` object. A separate `TitleInfoProviderManager<T>` then walks a chain of `ITitleInfoProvider<T>` sources (local JSON/XML databases, API calls, web scraping) to fill in missing metadata.

### "Attribute-driven UI"
The `[UiProperty]` attribute on `Rom` properties declares *how* each field should be displayed (visibility, display name, rendering type, sort order). `DynamicRomPropertyUI` uses reflection to turn the domain model into a rendered form without coupling the model to any UI framework.

### "Platform first"
Rather than a generic emulator front-end, each platform gets its own rich type (`SwitchGame`, `PS4DLC`, `ThreeDSUpdate`, etc.) inheriting from `Game`, `DLC`, or `Update` — which in turn extend `Rom`. This preserves platform-specific fields (e.g. NSP/NCA metadata on Switch) without losing shared behaviour.

---

## 4. Architecture Deep-Dive

### 4.1 Generic Console Pipeline

```
IRomParser<T>[]               (parse binary formats → Rom list)
        ↓
RomParserExecutor<T>          (merge by TitleID, deduplicate)
        ↓
ConsoleManager<T>             (accumulates RomList, orchestrates)
        ↓
TitleInfoProviderManager<T>   (chains ITitleInfoProvider<T> for enrichment)
        ↓
AppDbContext (EF Core)        (persist to SQLite)
```

### 4.2 Key Interfaces

| Interface | Purpose |
|---|---|
| `IRomParser<T>` | Parses a file path into `List<Rom>`. Declares supported extensions. |
| `ITitleInfoProvider<T>` | Enriches a `Rom` from an external source (JSON DB, XML, HTTP). |
| `IConsoleManager<T>` | Coordinates parser execution and ROM list tracking per console. |
| `IPlugin` | External plugin contract: execute an option, register configs. |
| `IRomMissingContentChecker` | Detects missing updates/DLC for a given ROM. |
| `IHasTitleProviders` | Marks a manager as owning a `TitleInfoProviderManager`. |
| `ICanSaveToDB` | Marks a ROM type as persistable to the database. |
| `IHashSupported` | Marks a ROM type as supporting hash-based identification. |
| `IExternalRomFormat` | For ROMs that require external format tools to unpack. |
| `IUpdateVersionProvider` | Provides the latest known update version for a title ID. |

### 4.3 Domain Hierarchy

```
Rom
├── Game
│   ├── SwitchGame, PS4Game, ThreeDSGame, WiiGame, …
├── Update
│   ├── SwitchUpdate, PS4Update, PS3Update, …
├── DLC
│   ├── SwitchDLC, PS4DLC, WiiUDLC, …
└── Homebrew
```

### 4.4 Dependency Injection

`RomManagerShared` depends on `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Hosting`. Each platform module registers its own parsers and title providers into the DI container, meaning the entire pipeline is composable without hard dependencies between platform modules.

### 4.5 Configuration

`RomManagerConfiguration` is a static façade over `IConfigurationRoot` (Microsoft.Extensions.Configuration). It reads a JSON config file and exposes typed accessors for ROM paths, tool paths, database location, and log paths. Each platform also has its own `*Configuration.cs` (e.g. `SwitchConfiguration`) that reads the console-specific sub-section.

---

## 5. Supported Platforms

| Platform | Game | Update | DLC | Parser | TitleInfoProvider |
|---|---|---|---|---|---|
| Nintendo Switch | ✅ | ✅ | ✅ | NSP/XCI/NSZ | JSON DB, LibHac |
| Nintendo 3DS | ✅ | ✅ | ✅ | CIA/3DS | JSON DB |
| Nintendo DS | ✅ | — | — | NDS | — |
| Game Boy / GBC | ✅ | — | — | GB/GBC | — |
| Game Boy Advance | ✅ | — | — | GBA | — |
| Nintendo 64 | ✅ | — | — | N64/Z64 | — |
| SNES | ✅ | — | — | SFC/SMC | — |
| Wii | ✅ | — | — | ISO/WBFS | GameTDB XML |
| Wii U | ✅ | ✅ | ✅ | WUD/WUX | WikiBrew scrape |
| PS2 | ✅ | — | — | ISO | — |
| PS3 | ✅ | ✅ | ✅ | PKG/ISO | PSN |
| PS4 | ✅ | ✅ | ✅ | PKG | PARAM.SFO DLL |
| PSP | ✅ | ✅ | ✅ | ISO/CSO/PKG | — |
| PS Vita | ✅ | ✅ | ✅ | VPK/PKG | — |
| Sega Saturn | ✅ | — | — | ISO/BIN | — |
| Original Xbox | ✅ | — | — | ISO/XISO | — |
| Xbox 360 | ✅ | ✅ | ✅ | ISO/XBLA | — |

---

## 6. Plugin System

### Current State

`PluginManager` provides **assembly-reflection-based plugin loading** from a directory of DLLs. It looks for a class named `RomManagerPlugin.PluginMain` in each loaded assembly and expects it to implement `IPlugin`.

`IPlugin` exposes:
- `PluginName` — identifier
- `Options` — list of command strings
- `Execute(int optionIndex)` — invoked when the user picks an option
- `RegisterAppConfigs(string configPath)` — allows the plugin to load its own JSON config

### Current Bugs / Limitations

1. **Critical type-check bug** — `if (pluginType is IPlugin)` compares a `System.Type` against the `IPlugin` interface. A `Type` object never satisfies this check. The correct code is:
   ```csharp
   // Wrong:
   if (pluginType is IPlugin)
   
   // Correct:
   if (pluginType != null && typeof(IPlugin).IsAssignableFrom(pluginType))
   {
       var instance = (IPlugin)Activator.CreateInstance(pluginType)!;
       Plugins.Add(instance);
   }
   ```
2. **No plugin discovery** — there is no registration of loaded plugins in DI; plugins are stored in a static `List<IPlugin>` with no accessor method.
3. **No lifecycle management** — no `Initialize()`, `Dispose()`, or versioning contract.
4. **No isolation** — plugins load into the main AppDomain; a crashing plugin can take down the host.

### Potential Plugin Architecture Improvements

- Add `IPlugin.Version`, `IPlugin.SupportedConsoles`, and `IPlugin.RequiredPermissions`.
- Use `AssemblyLoadContext` per plugin for isolation.
- Support MEF (`System.Composition`) or a simple `PluginAttribute`-scanning approach.
- Expose a `PluginContext` object (DI service locator scoped to the plugin) rather than a config path string.

---

## 7. Database Layer

- **ORM**: Entity Framework Core (SQLite provider)
- **DbContext**: `AppDbContext` — one `DbSet<T>` per concrete ROM type (≈ 40 sets)
- **Lazy loading**: Enabled via `Microsoft.EntityFrameworkCore.Proxies`
- **Repository**: `GenericRepository<T>` wraps common CRUD operations
- **Hash Repository**: `RomHashRepository` for hash-specific lookups
- **Migration history**: exists in `Migrations/` folder

### Issues
- The `DbContext` is registered as a direct dependency with no scoping strategy — in a multi-screen GUI this can cause concurrency issues.
- `AppDbContext` manually `using`s every platform namespace, making it a God class for the data layer.
- Lazy loading proxies + no explicit `AsNoTracking()` on read paths risks memory bloat in large collections.
- No Unit of Work pattern — saves are scattered across features.

---

## 8. UI Layer

### Avalonia (Primary)

- **Framework**: Avalonia 11 (compiled bindings enabled by default)
- **MVVM**: `CommunityToolkit.Mvvm` — `ViewModelBase`, `MainViewModel`, `ConsoleViewModel`, `RomViewModel`
- **ViewLocator**: Convention-based ViewModel → View mapping
- **Dynamic UI**: `DynamicRomPropertyUI` uses the `[UiProperty]` attribute to generate controls via reflection at runtime
- **Multi-target**: Shared UI library consumed by `AvaloniaUI.Desktop`, `AvaloniaUI.Android`, and `AvaloniaUI.Browser`

### MAUI (Secondary / Experimental)

`RomManagerMaui` exists as a parallel MAUI shell targeting iOS, Android, and Windows. It currently contains the default MAUI template with minimal custom code, suggesting it is aspirational / exploratory.

### UI Weaknesses
- Only 3 XAML files exist in the Avalonia `Views/` folder — the UI is extremely thin compared to the shared library.
- `DynamicRomPropertyUI` has an overloaded `CreateControlForProperty` method where one overload is incomplete (missing `IEnumerable<>` ListControl implementation).
- No navigation framework — navigation state appears to be handled ad-hoc.
- No theming/styling infrastructure beyond the default Fluent theme.

---

## 9. Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Core runtime | .NET | **10.0** (all projects) |
| Desktop UI | Avalonia | 11.2.6 |
| Mobile UI | .NET MAUI | — |
| MVVM | CommunityToolkit.Mvvm | 8.4.0 |
| ORM | Entity Framework Core | **10.0.0** stable |
| Database | SQLite | via EF Core |
| Configuration | Microsoft.Extensions.Configuration | **10.0.0** stable |
| DI | Microsoft.Extensions.DependencyInjection | **10.0.0** stable |
| Logging | Microsoft.Extensions.Logging.Abstractions | **10.0.0** stable |
| Nintendo crypto | LibHac | vendored (net10.0 Span fixes applied) |
| Binary I/O | SkyEditor.IO + Syroot.BinaryData | vendored / NuGet |
| Archive | DiscUtils + System.IO.Compression | NuGet |
| JSON | System.Text.Json | built-in (.NET 10) — Newtonsoft removed from shared lib |
| HTML | HtmlAgilityPack | 1.11.72 (web scraping) |
| Hashing | System.IO.Hashing | **10.0.0** stable |
| Testing | xUnit + FluentAssertions | 2.9.2 + 8.8.0 |

---

## 10. Good Things in the Architecture

1. **Strong generic typing** — `ConsoleManager<T>`, `RomParserExecutor<T>`, `TitleInfoProviderManager<T>` enforce compile-time console affinity while sharing full pipeline logic. Adding a new console requires zero changes to the core pipeline.

2. **Interface segregation** — responsibilities are split across small, focused interfaces (`IRomParser`, `ITitleInfoProvider`, `IPlugin`, `IRomMissingContentChecker`, etc.). Each interface has a single verb.

3. **Attribute-driven UI** — the `[UiProperty]` attribute on domain models decouples display logic from UI implementation and means new properties automatically surface in the UI.

4. **Multi-frontend design** — `RomManagerShared` has no UI dependencies; the same library drives Desktop (Avalonia), Mobile (MAUI), and Web (WASM) targets.

5. **Parser merging logic** — `RomParserExecutor` merges results from multiple parsers on the same file by `TitleID`, allowing an NSP header parser and a JSON metadata provider to cooperate without one overwriting the other.

6. **Rich domain model** — `Rom` captures ~25 fields covering title ID, version, regions, languages, genres, ratings (PEGI/ESRB/CERO), hashes, images, publisher, developer, release date, etc. The `Game / Update / DLC / Homebrew` hierarchy mirrors real-world ROM taxonomy.

7. **No-Intro integration** — `NoIntroRomHashIdentifier` links the project to the No-Intro preservation standard for verified ROM identification.

8. **Async-first** — all parsers and title providers use `Task<T>` returns; `TitleInfoProviderManager.Setup()` uses `Task.WhenAll` for parallel provider initialisation.

9. **Centralised configuration** — `RomManagerConfiguration` provides a single entry point for all path resolution, preventing magic strings scattered across the codebase.

10. **LibHac integration** — embedding the Nintendo Switch cryptography/filesystem library gives deep, accurate NSP/NCA/XCI parsing that few community tools achieve.

11. **Interface-driven context menus** — `IConsoleRomAction` / `IHasContextMenuActions` let each console manager declare its own context menu items without touching the UI layer. The Avalonia `BuildContextMenu()` method groups and renders these generically.

12. **Plugin isolation** — each plugin DLL is loaded into its own `AssemblyLoadContext` (collectible), preventing type conflicts and allowing hot-unload/reload without restarting the application.

13. **Unit of Work pattern** — `AppDbContext` implements `IUnitOfWork` with `BeginTransactionAsync/CommitAsync/RollbackAsync`, enabling atomic multi-step database operations without leaking EF Core internals to callers.

14. **Testable file utilities** — `FileRenamer.PreviewRenames()` / `RomMover.PreviewMoves()` execute the same logic as the real operations but skip file system writes, making them safely callable in unit tests and UI preview dialogs.

---

## 11. Downsides in the Architecture

1. **Mixed target frameworks** — ✅ Resolved: all projects now target `net10.0`; LibHac `Span`/`ReadOnlySpan` breaking changes fixed.

2. **God `AppDbContext`** — ✅ Resolved: split into 5 partial files (`AppDbContext.Nintendo.cs`, `AppDbContext.Sony.cs`, `AppDbContext.Microsoft.cs`, `AppDbContext.NoIntro.cs`) plus `IUnitOfWork`.

3. **Static configuration & logging** — ✅ Mostly resolved: `PluginManager` now a DI singleton; `ILogger` bridge added. `RomManagerConfiguration` is still a static class but now loads `config.json` from `AppContext.BaseDirectory` (not CWD) and marks the file `optional: true`; all path accessors are null-safe and return `string.Empty` when config is absent.

4. **Vendored libraries without abstraction** — `LibHac` and `SkyEditor.IO` still no abstraction layer. Noted for future.

5. **Console classes polluting an interface file** — ✅ Fixed: all `GamingConsole` subclasses extracted to dedicated `RomManagerShared/Base/GamingConsoles.cs`; `IConsoleManager.cs` now contains only interfaces and `ConsoleManager<T>`.

6. **Inconsistent nullable handling** — ✅ Partially resolved: `Rom.ToString()` NullRef fixed; `FileRenamer` null guards added. Full audit pending.

7. **No error recovery strategy** — parsers silently swallow exceptions and log via `FileUtils`. There is no structured error type, no retry, and no user-facing error reporting in the UI.

8. **No per-console column persistence** — column visibility is stored in `Settings.VisibleColumns` but was never written back by the UI; checkboxes had no effect on DataGrids after closing the settings dialog.

9. **Mixed DI and static code** — the project uses DI for some modules but static singletons (`PluginManager`, `RomManagerConfiguration`, `FileUtils`) for others, creating an inconsistent composition model.

10. **Preview NuGet packages in production code** — EF Core 9 preview was used as a dependency in what appears to be active development code, introducing instability risk.

---

## 12. Points to Fix (Bugs / Broken Code)

### Critical

| Location | Issue | Status |
|---|---|---|
| `Utils/PluginManager.cs` | `if (pluginType is IPlugin)` — always false; no plugins ever loaded | ✅ Fixed — now `typeof(IPlugin).IsAssignableFrom(pluginType)` + `Activator.CreateInstance` |
| `Base/Rom.cs:ToString()` | `Regions[0]` called with no null guard → `NullReferenceException` | ✅ Fixed — `Regions?.FirstOrDefault()?.ToString() ?? "Unknown"` |
| `Base/RomParserExecutor.cs` | `RemoveAt(i); i--` fragile index pattern | ✅ Fixed — replaced with a `toMerge` list and a clean LINQ-style pass |
| `RomManagerConfiguration.Load()` | `Directory.GetCurrentDirectory()` → config.json never found when running via `dotnet run` | ✅ Fixed — now uses `AppContext.BaseDirectory`; file marked `optional: true`; all accessors null-safe |

### Medium

| Location | Issue | Status |
|---|---|---|
| `PluginManager` | No public accessor for loaded plugins | ✅ Fixed — `Plugins` property + full rewrite with `AssemblyLoadContext` |
| `TitleInfoProviderManager.GetTitleInfo` | In-place mutations silently fail `if (newrom != rom)` check | ✅ Fixed — `ReferenceEquals` + `RomUtils.CopyNonNullProperties` merge |
| `DynamicRomPropertyUI` | `case UiPropertyType.List:` commented out | ✅ Fixed — `ListBox` / `IEnumerable` detection implemented |
| `AppDbContext` | God class with all DbSets, missing TPH discriminator config | ✅ Fixed — split into 5 partial files, `IUnitOfWork` added, hierarchy configured |
| `AppDbContext` | No DI-scoped registration, no transaction support | ✅ Fixed — registered as `AddDbContext<AppDbContext>` scoped, `BeginTransactionAsync/CommitAsync/RollbackAsync` |

### Minor

| Location | Issue | Status |
|---|---|---|
| `RomViewModel.Region` | `_rom.Regions?.ToString()` prints `System.Collections.Generic.List[...]` | ✅ Fixed — `string.Join(", ", ...)` |
| `ConsoleViewModel.InitializeDataGrid` | Only 5 hardcoded columns got a `Binding`; all others showed blank | ✅ Fixed — delegates to `AddColumnsToDataGrid` + `PropertiesDictionary` |
| `ConsoleViewModel.LoadData` | `MainViewModel.RomPaths[type]` throws `KeyNotFoundException` when console not configured | ✅ Fixed — `TryGetValue` guard |
| `RomViewModel.PropertiesDictionary` | Rebuilds new `Dictionary` on every property access (called per render frame) | ✅ Fixed — cached lazily via `BuildPropertiesDictionary()` |
| `MainView` side panel | Always empty — `DataGrid.SelectionChanged` never wired | ✅ Fixed — `ShowRomDetails(rvm)` / `ClearRomDetails()` |
| `MainWindow` | Ignores `Settings.WindowWidth/Height` | ✅ Fixed — restored from settings; saves on close |
| `ConsoleViewModel.DictionaryItemConverter` | Nullable parameter mismatch on `IValueConverter` methods | ✅ Fixed — signatures updated to `object?` |
| `FileRenamer.cs` sourcePath double-concatenation bug | `Path.Combine(dir, full_absolute_path)` | ✅ Fixed |
| `FileRenamer.cs` loop condition `sourcePath != newFileName` | Wrong variable comparison | ✅ Fixed — `string.Equals(newFilePath, sourcePath, OrdinalIgnoreCase)` |
| `GlobalUsings.cs` duplicate entries | 6 duplicate `global using` statements | ✅ Fixed |
| `PluginManager` static (un-DI-able) | Static singleton, not testable | ✅ Fixed — instance class, DI singleton |

---

## 13. Points to Improve (Code Quality / Design)

1. **Unify target frameworks** — ✅ Done: all projects now target `net10.0` with packages at `10.0.0`.

2. **Replace static singletons with DI scopes** — ✅ Done: `PluginManager` registered as DI singleton; `AppDbContext` as scoped; `FileUtils.Logger` bridge added. `RomManagerConfiguration` still static (noted).

3. **Split `AppDbContext` using partial classes** — ✅ Done: `AppDbContext.Nintendo.cs`, `AppDbContext.Sony.cs`, `AppDbContext.Microsoft.cs`, `AppDbContext.NoIntro.cs`.

4. **Add structured logging** — ✅ Done: `Microsoft.Extensions.Logging.ILogger` integrated into `App.axaml.cs`, `PluginManager`, `FileRenamer`, `RomMover`, `DuplicateDetector`; `FileUtilsLoggerProvider` bridges static `FileUtils.Log`.

5. **Extract `GamingConsole` subclasses** — ✅ Done: all 17 console marker classes extracted to `RomManagerShared/Base/GamingConsoles.cs`; `IConsoleManager.cs` now contains only interfaces and `ConsoleManager<T>`.

6. **Add cancellation token support** — ✅ Done: `FileRenamer.RenameFilesAsync`, `RomMover.MoveFilesAsync`, `IConsoleRomAction.ExecuteAsync` all accept `CancellationToken`.

7. **Introduce a `Result<T>` type** — ✅ Partially done: `RenameResult` and `MoveResult` records with `Success`/`ErrorMessage` fields replacing thrown exceptions in the file utilities.

8. **Add unit tests** — ✅ Done: `RomManagerTests` project with 32 passing xUnit tests covering `FileRenamer`, `DuplicateDetector`, `RomMover`, and `AppDbContext`.

9. **Use `System.Text.Json` instead of Newtonsoft.Json** — ✅ Done: all Newtonsoft.Json usages in `RomManagerShared` replaced with `System.Text.Json` + `System.Text.Json.Nodes`; Newtonsoft package reference removed from `RomManagerShared.csproj`. `testconsole` still references Newtonsoft as it is a dev utility.

10. **Enforce nullable correctness** — ⚠️ Ongoing — major null guards added; a full audit was not performed.

11. **Version-pin all NuGet packages** — ✅ Partially done: `RomManagerShared` packages pinned to `9.0.3` stable; `Directory.Packages.props` exists in `Avalonia/` sub-folder.

12. **Implement `IDisposable` / `IAsyncDisposable`** — ✅ Done: `AppDbContext` now implements `IAsyncDisposable`; `PluginManager` implements `IAsyncDisposable` (per-plugin `AssemblyLoadContext` cleanup).

13. **Separate DTO types from domain types** — ⚠️ Not yet done — low priority; noted for future refactor.

---

## 14. Possible Future Functionalities

### Core Features
- **ROM organisation / file renaming** — ✅ Implemented: `FileRenamer` rewritten with dry-run/preview, `RenameResult`, async API, new placeholders (`{Publisher}`, `{Developer}`, `{ReleaseDate}`, `{Platform}`, `{ProductCode}`), bug fixes
- **File mover** — ✅ Implemented: `RomMover` with dry-run, `MoveResult`, async API, structure preservation
- **Duplicate detection** — ✅ Implemented: `DuplicateDetector` with `DuplicateStrategy` flags (TitleId, Hash, TitleName), `DuplicateGroup.Preferred` ranking by version/regions/size
- **Bad dump detection** — compare against No-Intro / Redump DAT files and flag incorrect or corrupted dumps
- **ROM patching support** — apply IPS/BPS/RUP patches (fan translations, bug fixes) directly from the manager
- **Save file management** — back up, restore, and cloud-sync save files alongside ROMs
- **Missing content scanner** — already partially implemented for Switch (`GetMissingUpdates`); extend to all platforms
- **Scraping throttle / offline mode** — cache scraped metadata locally to avoid hitting external sources on every launch
- **Collection statistics dashboard** — show counts, completion percentages, size totals per platform/region

### UI / UX
- **ROM details side panel** — ✅ Implemented: `DataGrid.SelectionChanged` wired; side panel shows all `Rom.Properties` via `RomViewModel.Properties`; placeholder shown when nothing selected
- **Search / filter per console tab** — ✅ Implemented: live filter TextBox in toolbar; filters `Roms` by Title or TitleID with case-insensitive match; clear (✕) button
- **Dark / Light mode toggle** — ✅ Implemented: View → Theme menu; persisted in `Settings.Theme`; applied on startup + immediately on change
- **Compact mode** — ✅ Implemented: View → Compact Mode; `RowHeight=22` + tighter padding; toggle in Settings window; indicator in status bar
- **Download manager panel** — ✅ Implemented: `DownloadManagerViewModel` with `DownloadEntryViewModel`; collapsible bottom panel; per-download progress/speed/cancel; Clear Done button
- **Status bar** — ✅ Implemented: bottom dock bar showing total ROM count, loading progress, compact indicator
- **Settings window** — ✅ Implemented: Theme, Compact Mode, database path, rename format, plugins directory, column visibility, plugin enable/disable
- **Application menu bar** — ✅ Implemented: File → Settings, File → Exit, Tools → Refresh All, Tools → Check for Updates
- **Context menus from backend actions** — ✅ Implemented: `IConsoleRomAction`, `IHasContextMenuActions`, dynamic context menu in `MainView` with category grouping
- **Console-specific context menu commands** — ✅ Implemented: `SwitchConsoleActions.cs` (SwitchCheckMissingUpdatesAction, SwitchGroupByTitleAction, OpenContainingFolderAction)
- **Plugin UI integration** — ✅ Implemented: `IPluginContext.ShowPluginUIAsync`, `PluginManager` with `AssemblyLoadContext` isolation
- **Platform filter sidebar** — quick navigation between consoles in the main view
- **Grid / List / Detail view modes** — multiple display modes for the ROM list
- **Box art / screenshot browser** — display full-resolution artwork in the detail pane
- **Bulk edit** — edit metadata (regions, languages, ratings) across multiple ROMs at once
- **Export to EmulationStation / LaunchBox XML** — compatibility export for front-end launchers
- **Emulator launchers** — launch a selected ROM in a configured emulator directly from the UI

### Testing
- **xUnit test project** — ✅ Implemented: `RomManagerTests` with 32 passing tests (FileRenamer, DuplicateDetector, RomMover, AppDbContext)


- **PS5 / Xbox Series support** — the `AppDbContext` has commented-out PS5 sets, suggesting this is planned. PS5 can be supported; Xbox Series uncertain

---

## 15. Benefits of Upgrading to .NET 10

> .NET 10 SDK 10.0.103 was released February 10, 2026.

### Runtime & Language

| Feature | Benefit to RomManager |
|---|---|
| **C# 14 language features** | `field` keyword for auto-property backing fields simplifies `RomViewModel` and `ConsoleViewModel` observable properties. Improved pattern matching benefits parser dispatch logic. |
| **Improved performance for collections** | Collection-heavy hotpaths (`RomList`, `GroupedRomList`, parser merging) benefit from continued `List<T>` / `LINQ` JIT improvements in .NET 10. |
| **`params IEnumerable<T>` support** | Cleaner call sites for methods like `AddImages(IEnumerable<string>)`. |
| **`Task.WhenEach`** | Can replace the `Task.WhenAll` in `TitleInfoProviderManager.Setup()` when incremental progress reporting is desired. |

### EF Core 10

| Feature | Benefit |
|---|---|
| **EF Core 10 stable (replaces 9-preview)** | Moving off `9.0.0-preview.1` removes instability. EF 10 ships alongside .NET 10 as its stable counterpart. |
| **Improved LINQ translation** | Complex LINQ over `RomList`/`GroupedRomList` may translate to more efficient SQL. |
| **AOT / trimming improvements** | Better support for `DbContext` in trimmed/AOT builds opens the WASM front-end to database-backed scenarios. |

### Avalonia on .NET 10

| Feature | Benefit |
|---|---|
| **Unified TFM** | Target `net10.0` in both `RomManagerShared` and `AvaloniaUI` — eliminates the current net8/net9 mismatch |
| **NativeAOT compatibility improvements** | Faster cold-start for Desktop build; smaller binary sizes for WASM. |
| **System.Text.Json AOT** | If `Newtonsoft.Json` is replaced with STJ, `.Browser` target gets full JSON serialisation without interpreter. |

### Tooling & SDK

| Feature | Benefit |
|---|---|
| **`dotnet publish` NativeAOT improvements** | Smaller, faster executables for the Desktop target with no JIT warmup. |
| **SDK built-in container publishing** | `dotnet publish --os linux /t:PublishContainer` with no Docker file — useful for a future server/API mode. |
| **Hot reload improvements** | Faster inner-loop development when iterating on Avalonia UI. |
| **`global.json` alignment** | Change `"version": "9.0.0"` to `"10.0.103"` and `"rollForward": "latestFeature"` for stable SDK pinning. |

### Migration Steps

1. Update `global.json`:
   ```json
   {
     "sdk": {
       "version": "10.0.103",
       "rollForward": "latestFeature"
     }
   }
   ```

2. Change `RomManagerShared.csproj` `<TargetFramework>` from `net8.0` to `net10.0`.

3. Update all NuGet packages from `9.0.0-preview.*` to their stable `10.0.x` counterparts:
   - `Microsoft.EntityFrameworkCore.*` → `10.0.x`
   - `Microsoft.Extensions.*` → `10.0.x`
   - `System.IO.Hashing` → `10.0.x`
   - `System.Drawing.Common` → `10.0.x`

4. Update `AvaloniaUI.csproj` from `net9.0` to `net10.0` (Avalonia 11.x runs on .NET 10).

5. Run `dotnet build` and resolve breaking changes (reference the [.NET 10 migration guide](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)).

6. Replace `Newtonsoft.Json` with `System.Text.Json` to unlock AOT/WASM benefits.

7. Swap `System.Drawing.Common` usages for `SkiaSharp` or `ImageSharp` — `System.Drawing.Common` is Windows-only and its future is uncertain on .NET 10+.

---

*End of analysis.*
