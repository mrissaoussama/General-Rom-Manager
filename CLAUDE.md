# RomManager

Cross-platform ROM library manager (.NET 10 + Avalonia). Scans ROM folders per console, parses
headers/containers, enriches metadata from title databases, and offers rename/move/dedupe/hash
actions plus a plugin system.

Docs: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) · [docs/ROADMAP.md](docs/ROADMAP.md) (v2 plan; v1 in ROADMAP-v1.md) ·
[docs/decisions/](docs/decisions/) (ADRs). `PROJECT_ANALYSIS.md` at the root is an older snapshot
(March 2026); ROADMAP supersedes it.

## Build / run / test

```sh
dotnet build AvaloniaApplication1/AvaloniaUI.Desktop/AvaloniaUI.Desktop.csproj
dotnet run   --project AvaloniaApplication1/AvaloniaUI.Desktop
dotnet test --project RomManagerTests/RomManagerTests.csproj
# Parser smoke test against real ROM files (see tools/ParserHarness/README.md)
dotnet run --project tools/ParserHarness -- PS4Console "Y:\roms\PS4" 5
```

- SDK pinned in `global.json` (10.0.103, rollForward latestFeature).
- Package versions live in `Directory.Packages.props` (central package management); `PackageReference`
  items carry no `Version`. Solution file: `RomManager.slnx`.
- `AvaloniaUI.Android` / `AvaloniaUI.Browser` target net9.0-* and are not maintained; only the
  Desktop head matters.
- Warnings are errors in every project (the solution builds with zero warnings). Vendored code in
  `Utils/ISO2GOD` and `3DS/Ctr` is marked `#nullable disable warnings`.

## Layout

| Path | What |
|---|---|
| `RomManagerShared/` | Core library: domain model, parsers, title-info providers, EF Core, utils |
| `RomManagerShared/Base/` | `Rom`/`Game`/`Update`/`DLC`, `ConsoleManager<T>`, `RomParserExecutor<T>`, `OperationQueue`, actions |
| `RomManagerShared/<Console>/` | One folder per console: `Parsers/`, `TitleInfoProviders/`, `*Manager.cs`, `*Rom.cs` |
| `RomManagerShared/Utils/` | File/hash/download utils, `PluginManager`, renamer, mover, duplicate detector, DAT matching (`Utils/Dat`); vendored ISO2GOD |
| `AvaloniaApplication1/AvaloniaUI/` | UI library: `App.axaml.cs` (DI composition root), ViewModels, AXAML Views (`MainView` shell, `DashboardView`, `ConsoleView`, `RomDetailsView`); `ViewLocator` maps VM → view |
| `AvaloniaApplication1/AvaloniaUI.Desktop/` | Desktop head (the only head that matters) |
| `SkyEditor.IO/` | Vendored binary IO lib (net10.0) used by 3DS code |
| `RomManagerTests/` | xunit v3 + AwesomeAssertions: parsers (synthetic fixtures), scanner, DB, DI, view models, plugins |
| `SamplePlugin/` | Reference plugin implementation |
| `tools/ParserHarness/` | Console app that runs every console's parsers on real files |

## How the pipeline works (short)

1. `ConsoleModules.Register` (ADR 0003) reflects over `RomManagerShared` plus any `Modules/*.dll` and, for every `GamingConsole`
   subtype `T`, registers (scoped): all `IRomParser<T>`, `ITitleInfoProvider<T>`,
   `IUpdateVersionProvider<T>`, `IRomOrganizer<T>`, `RomParserExecutor<T>`,
   `TitleInfoProviderManager<T>`, and `ConsoleManager<T>` (or its subclass, e.g. `SwitchManager`).
   **New implementations are picked up automatically — no manual registration.**
2. `ConsoleViewModel<T>.LoadData` → `LibraryScanner.LoadCachedAsync` (instant) →
   `ConsoleManager<T>.Setup()` (loads title DBs) → `LibraryScanner.ScanAsync`: unchanged files come from
   the `LibraryFiles` table, new/changed ones are parsed in parallel and enriched.
3. `RomParserExecutor<T>.ParseAsync` runs every parser whose `Extensions` contains the file extension
   (skipping `IContentSniffer` parsers that rule out the file's header, see `FileMagic`) and merges results
   by `TitleID` (`RomUtils.CopyNonNullProperties`).
4. Results are stored per file in `LibraryFiles` (JSON snapshot + parse error). What parsers log while
   parsing a file becomes its "not recognised" reason (`ParseDiagnostics`).

## Configuration

- `AvaloniaUI/config.json` (copied to output): `GlobalPaths` (BaseDirectory, Database, logs, plugins,
  cache) and per-console `RomPath`, tool paths, TitleDB URLs. Loaded via `RomManagerConfiguration`.
- `settings.json` next to the exe (`Settings.Instance`): user overrides (paths, columns, theme). Settings
  win over config.json for ROM paths.
- Switch keys: `%USERPROFILE%\.switch\prod.keys` and `title.keys` (requirement `switch.prod_keys`).
- Errors log to `GlobalPaths.ErrorLogs` via static `FileUtils.Log` — check it first when a parser
  "returns 0 roms": exceptions are caught and swallowed per parser.

## Conventions

- C# latest, nullable enabled, file-scoped namespaces, collection expressions (`[]`).
- Parsers implement `IRomParser<T>` with lowercase extension list; return `[]` (not null) on failure
  and log via `FileUtils.Log`. Implement `IHasRequirements` if the parser needs keys/files/tools.
- Console-specific ROM properties shown in the grid need `[UiProperty(...)]`.
- **Open ROM files read-only**: `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)`.
  Never bare `FileMode.Open` (requests write + exclusive lock; fails on read-only shares, locks
  emulators). Always dispose streams.
- Parsers must not write next to the ROM or into the base folder; extracted assets go under
  `RomManagerConfiguration.GetCachePath()`.
- No external programs in the scan path and no binaries in the repo (ADR 0002). Optional tools are
  user-supplied, default to `<BaseDirectory>/Tools/`, and run in `Cache/Temp`.
- Namespaces don't always match folders (e.g. `GamingConsole` is in `RomManagerShared.Interfaces`
  but lives in `Base/`). Grep for the type before adding usings.

## Adding a console

1. `RomManagerShared/<Name>/`: `<Name>Console : GamingConsole`, `<Name>Game : Rom` (+ Update/DLC),
   `<Name>Manager : ConsoleManager<<Name>Console>` (optional), `Parsers/<X>Parser : IRomParser<<Name>Console>`.
2. Add `Consoles.<Name>.RomPath` to `config.json`.
3. Run `tools/ParserHarness` against real files (use `--snapshot`/`--compare` around parser changes)
   and add a fixture-based test with a synthetic file.
4. Bump `LibraryScanner.ScanVersion` whenever parser output changes.

## Git

- Plain commit messages, no AI attribution / `Co-Authored-By` trailers.
- Don't commit ROMs, keys, `settings.json` with personal paths, `*.db`, or tool binaries.
