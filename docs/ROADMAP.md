# Roadmap: fix plan

Written 2026-10-25 from a code read, a full build and test run, and a parser run against a real
library (`Y:\roms`, `Y:\nsp`). The phases are in order. Each phase should leave the app building with
green tests. Tick items off here as they land.

Legend: **[P0]** = data-safety or correctness, **[P1]** = high value, **[P2]** = nice to have.

---

## Phase 0: Hygiene (½ day)

- [x] **[P0]** Commit the current working tree. 79 files are modified or untracked, including whole
      features (OperationQueue, plugins, duplicate detector, mover, settings/onboarding windows,
      tests), and the last commit was April 2025. Use small logical commits.
- [x] Remove or archive the dead projects: root `RomManager.csproj` + `App.axaml`, `MainWindow.axaml`,
      `Program.cs`, `app.manifest`, `Avalonia/` at the root, `RomManagerMaui/`, `ConsoleTest2/`,
      `testconsole/`. Decide whether Android and Browser are kept. If they are, retarget them to
      `net10.0-android` / `net10.0-browser`; if not, delete them.
- [x] Delete junk from the repo root (zip removed; local logs/tools git-ignored): `RomManager.zip`, `hs_err_pid5052.log`, `output.txt`, `es.exe`,
      `RomManager.sln.DotSettings.user`, `AvaloniaUI.Desktop/{err,out}.txt`, `AvaloniaUI.Desktop/US.en.json`.
- [x] `.gitignore`: `settings.json`, `*.db`, `Logs/`, `Cache/`, `*.keys`, `.vscode/` (or commit a
      shared `.vscode` on purpose), `tools/ParserHarness/paths.json`.
- [x] Move to the `.slnx` format (`dotnet sln migrate`) and list only the live projects (Android/Browser heads left out; ParserHarness added).
- [x] Root `Directory.Build.props` with the shared settings (`Nullable`, `LangVersion latest`) and
      `Directory.Packages.props` for central package management (Android/Browser heads opt out).
- [x] `TreatWarningsAsErrors` for every project except RomManagerShared (warning backlog) and the
      unmaintained Android/Browser heads.
- [ ] `AnalysisLevel latest` and warnings-as-errors for RomManagerShared once its ~1,200 warnings
      (mostly CS8618 in vendored 3DS/ISO2GOD code) are cleared.
- [x] CI: a GitHub Actions workflow on windows-latest that builds Desktop and runs the tests.

## Phase 1: Dependencies and frameworks (1–2 days)

| Package | Now | Target | Notes |
|---|---|---|---|
| Avalonia (all) | 11.2.6 | **11.3.22** now (done), 12.x later | 11.3 is a drop-in upgrade. 12 is a major version: do it after the UI moves to AXAML (phase 5) |
| Avalonia.Controls.DataGrid | (transitive) | match Avalonia | |
| CommunityToolkit.Mvvm | 8.4.0 | 8.4.2 | |
| EF Core (Sqlite/Proxies/Design/InMemory) | 10.0.0 | 10.0.13 (done) | Drop `Proxies` once lazy loading is removed (phase 2) |
| Microsoft.Extensions.* | 10.0.0 | latest 10.0.x | |
| System.IO.Hashing / System.Drawing.Common | 10.0.0 | latest 10.0.x | Replace `System.Drawing.Common` (Windows-only) with SkiaSharp (already shipped with Avalonia) |
| DiscUtils | 0.16.13 (abandoned) | **LTRData.DiscUtils.\*** 1.0.x | Maintained fork. Adds UDF (PS2 DVD9) and better ISO handling |
| HtmlAgilityPack | 1.11.72 | 1.13.1 | |
| Syroot.BinaryData | 6.0.0-alpha.13 | remove | Replace with `BinaryPrimitives`/`BinaryReader` |
| LibHac (vendored) | 0.20.0 source | **`Ryujinx.LibHac` 0.20.0** (done) | See [ADR 0001](decisions/0001-libhac.md). Tag the vendored tree first |
| SkyEditor.IO (vendored) | netstandard2.0, LangVersion 8 | net10.0 or remove | It pulls **FluentAssertions 7 alpha as a runtime dependency**, which must go |
| xunit / runner | 2.9.2 / 2.8.2 | xunit.v3 | |
| Microsoft.NET.Test.Sdk | 17.12.0 | 18.x | |
| FluentAssertions | 8.8.0 | **Shouldly** or AwesomeAssertions | FA 8 needs a paid license for commercial use |
| coverlet.collector | 6.0.2 | latest | |

- [x] **[P0] Security:** `Microsoft.EntityFrameworkCore.Design` 10.0.0 flows transitively into the
      app (no `PrivateAssets="all"`), which pulls `System.Security.Cryptography.Xml` 9.0.0 with
      known high-severity advisories (NU1903, e.g. GHSA-cvvh-rhrc-wg4q). Set `PrivateAssets="all"`
      on the Design reference in `RomManagerShared.csproj` (it is design-time only) and bump to
      10.0.x. Confirm with `dotnet nuget why <proj> System.Security.Cryptography.Xml`.
- [x] Fix the existing warnings (the UI projects build clean): move `OpenFolderDialog`/`OpenFileDialog` to
      `TopLevel.StorageProvider`, and fix the nullability warnings in `ConsoleViewModel.cs:371-427`.
- [x] Remove `Microsoft.NETCore.Platforms` from the root `Directory.Build.props` (DiscUtils replaced).

## Phase 2: Data safety and correctness bugs (2–3 days)

These were all reproduced against real files with `tools/ParserHarness`.

- [x] **[P0] Open ROM files read-only everywhere.** A bare `new FileStream(path, FileMode.Open)`
      requests write access and an exclusive lock. It fails on read-only shares and can lock out
      emulators. Affected: `PS2/Parsers/PS2RomParser.cs:38`, `3DS/3DSReader.cs:15,27`,
      `Wii/Parsers/WadInfo.cs:124,639,680`, `PSVita/PSVitaUtils.cs:69,85`. Add a single
      `FileUtils.OpenRead(path)` helper and an analyzer or grep check in CI.
- [x] **[P0]** `3DS/3DSReader.cs` never disposes its `BinaryReader`/`FileStream`, so handles leak.
- [x] **[P0]** The Wii WAD parser writes to the base directory path (`Access to the path 'D:\RomManager'
      is denied`). Send extracted files to `GetCachePath()/<console>/<titleId>/`.
- [x] **[P0]** The OG Xbox parser writes an icon folder (`54540082/`) straight into `BaseDirectory`.
      Send it to the cache as well.
- [x] **[P0]** (Also found: it deleted pre-existing `code/` and `meta/` folders.) `WiiUTMDTIKParser` runs `cdecrypt.exe`, which decrypts the title in place. That is
      slow (2–3 s per title) and writes into the ROM folder. Make it opt-in until phase 4 replaces it.
- [x] **[P0]** Remove the runtime `pip install -r` in `FileUtils` (around line 225). Installing
      packages silently is a supply-chain risk.
- [x] 3DS CIA: `NullReferenceException` on update CIAs (MK7 v3.5.0). Add a fixture test.
- [x] Switch: import the `.tik` tickets from the NSP before opening NCAs (snippet in ADR 0001).
      At the moment every titlekey-encrypted NSP fails with `Missing NCA title key`, and the results
      only come from the header-scan fallback, so icons, multi-language titles and sizes are lost.
- [x] `SwitchRomNSPXCIParser`: the instance fields (`localStorage`, `NSPPartitionFileSystem`,
      `RomList`) make it stateful and grow without bound. Make them locals and dispose them with
      `using`.
- [x] PS2: one ISO yields 2 games (`SLES_500.09` and `SLUS_200.90`), because every `SLxx` filename on
      the disc is treated as a game. Read `SYSTEM.CNF` → `BOOT2` instead.
- [x] PSP ISO: two parsers (`PSPISOSFOParser`, `PSPRomParser`) both match `iso`, and one of them
      returns an empty `PSPGame`. Merge them and drop roms with an empty TitleID in the executor.
- [x] Wii U WUX: empty results are now dropped by the executor (real WUX parsing still open, see phase 4).
      Was: returns a `WiiUGame` with no ID or title. Either implement it (parse the WUX header,
      then the partition table's `title.tmd`) or return `[]` and report the file as unidentified.
- [x] Xbox 360 XEX/ISO: returns 0 results and logs nothing. Cause: `xextool.exe` missing; now a declared
      requirement that logs once.
- [x] `RomParserExecutor`: sort parsers in the constructor too (currently only in `AddParser`). Treat
      a null or empty TitleID as "unidentified" rather than merging it.
- [x] Remove the stray `Console.WriteLine` debug output from the parsers (DS, GB, SNES, Xbox, Vita
      print rom dumps and binary data to stdout).
- [x] The NSZ header-scan parser decodes the header as UTF-8 and slices 32 characters before `.tik`.
      Bound-check it and validate the result as 32 hex characters.
- [x] Check EF model drift: run `dotnet ef migrations has-pending-model-changes`, then add a migration.
      (Also found: nothing ever called `Migrate()`, so the DB was never created. Now applied on startup.)

## Phase 3: Architecture (1–2 weeks)

### 3.1 Persist the library and scan incrementally [P1] — done (`LibraryScanner`, `LibraryFile`, JSON snapshots, `LibraryWatcher` debounced rescans)
- New entities: `LibraryFile` (Path, Size, MTimeUtc, Console, ScanVersion, LastScanUtc, Error?)
  1→N `Rom`. Add an index on Path.
- The scanner diffs the file system against `LibraryFile`: new or changed files are parsed, deleted
  files are removed, unchanged files are skipped. Bump `ScanVersion` when a parser changes so those
  files get re-parsed.
- The app starts by loading from the database (instant). "Refresh" becomes an incremental scan, and
  "Rescan all" stays available.
- Drop lazy-loading proxies. Use explicit `Include`/projections, and `AsNoTracking` for reads.
- A file watcher (`FileSystemWatcher`) per ROM root, debounced, with a fallback for network shares
  where watchers are unreliable.

### 3.2 Scanner service off the UI thread [P1] — partly done
- [x] Parsing runs off the UI thread in parallel batches (`LibraryScanner.MaxParallelism`, default 4); enrichment and DB writes stay sequential. Progress + cancel in the UI.
- `ILibraryScanner` (in Shared): `Channel<string>` producer → `Parallel.ForEachAsync`
  (MaxDegreeOfParallelism configurable; lower it for network drives) → batched DB writes →
  `IProgress<ScanProgress>` (file, count, errors).
- Parsers become **stateless and thread-safe**. Change the contract to
  `ValueTask<ParseResult> ParseAsync(RomFileContext ctx, CancellationToken ct)`, where `ParseResult`
  = roms + diagnostics + "unidentified" reason. Results stop being swallowed.
- Pick the console by content sniffing (`CanParse(header bytes)`), not just by extension, so one
  mixed ROM folder works and the `iso`/`pkg` collisions are resolved.

### 3.3 Remove static state [P1]
- `FileUtils.Log` → `ILogger<T>` everywhere. Add a Serilog file sink, or keep the existing provider.
- `RomManagerConfiguration` and `Settings.Instance` → one `IOptions<AppOptions>` /
  `IOptionsMonitor` built from config.json + settings.json (user layer) and saved through an
  `ISettingsStore`.
- `MainViewModel.RomPaths` (static) → `ILibraryRootsService`.

### 3.4 Domain cleanup [P2]
- [x] `ConsoleDescriptor` carries Id, DisplayName, Manufacturer and Badge; the shell builds console
  pages from it and shows DisplayName everywhere (Id stays the settings/cache key).
- Align namespaces with folders.
- Narrow `Rom`: platform-specific fields move to the subclasses.
- Replace the reflection `CopyNonNullProperties` merge with an explicit `Merge(Rom other)`.

### 3.5 Hash identification [P1]
- [x] DAT matching for NES/SNES/GB/GBC/GBA/N64: `Utils/Dat/DatIndex` (streaming Logiqx reader, SHA1 or
  CRC+size) + `DatTitleInfoProvider<T>` (strips iNES/copier headers, normalises N64 byte order) sets the
  No-Intro title, regions, serial and verified hashes. DATs go in `Data/DAT` (`GlobalPaths:Dats`).
- [x] Redump DATs for discs: PS2/PSP/GameCube/Wii `.iso`/`.gcm`, streamed SHA-1 + CRC32, opt-in via
  `Dats:HashDiscImages`. Multi-track (PS1 bin/cue, Dreamcast gdi) still open.
- [x] DAT folder status + "Open folder" on the dashboard.
- [x] Trigger a rescan of cartridge consoles when DATs change (`IHasDataFingerprint` on DAT providers,
  stored per `LibraryFile`).
- [x] Removed the unused EF-backed `NoIntroRomHashIdentifier` and its tables (migration `RemoveNoIntroTables`).

### 3.6 Plugins [P2] — done
- [x] Consent before any plugin code runs: permissions are declared on `[Plugin(Permissions = ...)]`
  and shown in an "Allow plugin?" dialog on import/reload; approvals are pinned to the DLL's SHA-256
  and permission set (`Plugins/approvals.json`). Runtime `RequiredPermissions` may not exceed the
  declaration. Permissions are still not sandboxed (plugins run with the user's rights).
- [x] `[assembly: PluginApiVersion(1)]` required; other versions are refused.
- [x] Fixed: imported plugins (in sub-folders) were not loaded on restart, and Reload loaded
  plugins twice.

## Phase 4: Format coverage (ongoing, by value to your library)

Ordered by how many files of each you actually have (Everything counts: nsz 1230, wad 1687 (mostly
game data, not Wii), gbc 367, nsp 97, pkg 56, cso 46, iso 33).

- [x] **NSZ real parsing**: control/meta NCAs stay uncompressed in NSZ, so LibHac reads names and versions directly (no zstd needed). Still open: XCZ, sizes of individual NCZ. Was: **NSZ/XCZ real parsing**: NCZ section headers + `ZstdSharp` to read the control NCA (NACP,
      icon). [P1]
- [ ] XCI via LibHac (the parser claims to support it, but it is untested: no XCI files available).
- [x] PKG (PS3/PSP/Vita) in C#: the PKG header plus PARAM.SFO is a short read. Delete the Python
      script and its `aenum` dependency. [P1]
- [x] PS2/PSP: CSO v1 (`CsoStream`). CHD via the MIT `CHDSharp` package (pure managed, all codecs). ZSO (LZ4) done in the same stream. Still open: UDF via
      LTRData.DiscUtils.
- [x] PS1: cue/bin, iso, chd via `SYSTEM.CNF`. PBP (EBOOT/PS1 classics) handled by the PSP parser.
- [x] PSP: ICON0.PNG from ISO/PBP and the DS banner icon + English title are cached under `Cache/Icons` as covers.
- [x] GameCube/Wii: ISO, CISO, RVZ and WIA (disc header read from the uncompressed `dhead` copy).
- [x] NES (iNES/NES 2.0 header + DAT names).
- [x] Genesis/MD (incl. SMD interleave), Master System, Game Gear, PC Engine: header parsers + DAT providers.
- [x] Dreamcast GDI and GD-ROM CHD (IP.BIN). Still open: CDI.
- [x] Wii U: TMD and WUD/WUX read without keys; cdecrypt is optional and no longer bundled (ADR 0002). Still open: in-process meta.xml decryption. Only decrypt `meta.xml`
      when the title key is available.
- [x] Switch: missing-update report ("Missing updates" on the console page, `SwitchUpdateChecker`,
      numeric version compare against `versions.json`; prompts to download it when absent). Still
      open: missing-DLC report (needs the titledb DLC list).

## Phase 5: UI and UX (1–2 weeks, can overlap phase 4)

### Structure
- [x] Move `MainView.axaml.cs` (1,900 lines of code-built UI) to AXAML views + ViewModels with
      compiled bindings: `MainView` (menu, sidebar, bottom panel), `DashboardView`, `ConsoleView`,
      `RomDetailsView`; `ConsoleViewModelBase` holds page state; `ViewLocator` caches views per VM.
- [ ] Dialogs as UserControls behind an `IDialogService` (now `Utils/Shell.cs` + small code-built
      windows), then upgrade to Avalonia 12.
- [x] Context menu items and actions become `ICommand`s on the VM (menu is built in `ConsoleView`, commands live on `ConsoleViewModelBase`).

### UX
- [x] **Empty states**: a tab with no path offers "Choose folder…". A tab with a path but no data
      offers "Scan now". There should be no bare "Ready" over an empty grid.
- [x] **Dashboard / home**: totals (ROMs, games, updates, DLC, unnamed, size) and a tile per console
      with scan status and missing requirements.
- [x] Dashboard: duplicates and not-recognised totals, last scan time per console, "Open log".
- [x] Dashboard: missing updates total (consoles with a version database; versions.json parse cached by mtime).
- [x] **Navigation**: a left sidebar with consoles (count + scan indicator) instead of tabs.
- [x] "All consoles" view with a console column (shown when 2+ consoles).
- [x] Console icons in the sidebar (manufacturer-coloured badges from `ConsoleDescriptor`).
- [x] Box art in the details panel: `ImageCache` downloads URLs once into `Cache/Images`.
- [x] **Cards toggle** (`ItemsRepeater` + `UniformGridLayout`, covers load lazily per realized card).
- [x] Embedded covers: Switch control-NCA icon, PS4 icon0.png, 3DS SMDH (CIA), DS banner, PSP ICON0.
- [x] libretro-thumbnails box art URLs for DAT-matched cartridges (NES/SNES/GB/GBC/GBA/N64/Sega/PCE).
- [x] 3DS .3ds/.cci/.cxi parsed (icon for decrypted dumps); PS4 pic0/pic1 cached as files (managed PKG reader).
- [x] Release cover bitmaps when cards are recycled (`ItemsRepeater.ElementClearing`).
- [ ] Profile the card view with 5k+ rows on a real library.
- [x] **Filters**: type, region, unnamed only, text search (title, TitleID, path).
- [x] Filters: has/no update, duplicates (same title ID, type and version).
- [x] Filters: language, format.
- [x] Per-file scan progress with cancel; unrecognised files listed from a button on the console page.
- [x] Per-file failure reasons: `ParseDiagnostics` captures what the parsers log per file; stored in `LibraryFile.Error` and shown in the "Not recognised" list.
- [x] Drag-and-drop a folder onto the window to add it as a ROM root (`ConsoleDetector` ranks consoles by extension).
- [x] Rename shows an old → new preview (dry run of the real renamer).
- [x] Move preview (dry run of `RomMover`).
- [x] Settings: live path status, Switch key status (`SwitchKeyStatus`, never shows key values), "Download title databases" button.
- [x] Remember window position, size and maximized state; ignore positions on disconnected monitors.
- [x] Keyboard: Ctrl+F search, F5 scan all, Ctrl+D dashboard, Ctrl+L log, Del delete (confirmed),
      F2 rename, Enter properties, Shift+F5 full rescan.
- [x] Accessibility: real `Menu` control, no emoji in menu labels.
- [x] Set `AutomationProperties.Name` on buttons without text (rom cards, console tiles).

## Phase 6: Testing and release

- [ ] **Parser fixture tests**: small or homebrew files per format under `RomManagerTests/Fixtures`
      (generate truncated headers where real dumps can't be committed), plus a golden JSON of the
      expected `Rom`s.
- [x] Keep `tools/ParserHarness` for local smoke runs against a real library (`--snapshot`/`--compare` done). Add `--json` output so
      runs can be diffed before and after a change.
- [x] Scanner integration test: temp folder → scan → modify → rescan → assert the diff (`LibraryScannerTests`).
- [x] ViewModel tests for the filters (`ConsoleViewModelFilterTests`; tests reference AvaloniaUI).
- [x] ViewModel tests for empty states, grouping, dashboard totals and commands (`ConsoleViewModelStateTests`).
- [x] Release: `dotnet publish -r <rid>` gives a self-contained single-file `RomManager`; a `v*` tag
      builds win-x64/linux-x64/osx-arm64 archives (`.github/workflows/release.yml`). Version in
      `Directory.Build.props`, overridden from the tag.
- [ ] Velopack installers and auto-update.
- [x] Update `README.md`: features, supported formats, install, keys/title DB/DAT setup.
- [ ] README screenshots.

---

## Suggested order of first PRs

1. Phase 0 commit plus junk/dead-project removal.
2. `FileUtils.OpenRead` + fix every `FileMode.Open` + the 3DS reader disposal (P0).
3. Cache path for the Wii/Xbox extraction, cdecrypt opt-in, remove the runtime `pip install`.
4. Switch ticket import + `Ryujinx.LibHac` swap (ADR 0001).
5. Package bumps (Avalonia 11.3, EF 10.0.x, LTRData.DiscUtils, test stack).
6. Library persistence + incremental scanner (biggest UX gain).
7. UI move to AXAML, then the dashboard, empty states and box art.
