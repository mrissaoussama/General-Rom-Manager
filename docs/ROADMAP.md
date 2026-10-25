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
- [ ] `.gitignore`: `settings.json`, `*.db`, `Logs/`, `Cache/`, `*.keys`, `.vscode/` (or commit a
      shared `.vscode` on purpose), `tools/ParserHarness/paths.json`.
- [ ] Move to the `.slnx` format (`dotnet sln migrate`) and list only the live projects.
- [ ] Add `Directory.Build.props` at the root with the shared settings: `Nullable`,
      `LangVersion latest`, `TreatWarningsAsErrors` for new projects, and `AnalysisLevel latest`.
      Add `Directory.Packages.props` for central package management.
- [ ] CI: a GitHub Actions workflow on windows-latest that builds Desktop and runs the tests.

## Phase 1: Dependencies and frameworks (1–2 days)

| Package | Now | Target | Notes |
|---|---|---|---|
| Avalonia (all) | 11.2.6 | **11.3.22** now, 12.x later | 11.3 is a drop-in upgrade. 12 is a major version: do it after the UI moves to AXAML (phase 5) |
| Avalonia.Controls.DataGrid | (transitive) | match Avalonia | |
| CommunityToolkit.Mvvm | 8.4.0 | 8.4.2 | |
| EF Core (Sqlite/Proxies/Design/InMemory) | 10.0.0 | 10.0.13 | Drop `Proxies` once lazy loading is removed (phase 2) |
| Microsoft.Extensions.* | 10.0.0 | latest 10.0.x | |
| System.IO.Hashing / System.Drawing.Common | 10.0.0 | latest 10.0.x | Replace `System.Drawing.Common` (Windows-only) with SkiaSharp (already shipped with Avalonia) |
| DiscUtils | 0.16.13 (abandoned) | **LTRData.DiscUtils.\*** 1.0.x | Maintained fork. Adds UDF (PS2 DVD9) and better ISO handling |
| HtmlAgilityPack | 1.11.72 | 1.13.1 | |
| Syroot.BinaryData | 6.0.0-alpha.13 | remove | Replace with `BinaryPrimitives`/`BinaryReader` |
| LibHac (vendored) | 0.20.0 source | **`Ryujinx.LibHac` 0.20.0** | See [ADR 0001](decisions/0001-libhac.md). Tag the vendored tree first |
| SkyEditor.IO (vendored) | netstandard2.0, LangVersion 8 | net10.0 or remove | It pulls **FluentAssertions 7 alpha as a runtime dependency**, which must go |
| xunit / runner | 2.9.2 / 2.8.2 | xunit.v3 | |
| Microsoft.NET.Test.Sdk | 17.12.0 | 18.x | |
| FluentAssertions | 8.8.0 | **Shouldly** or AwesomeAssertions | FA 8 needs a paid license for commercial use |
| coverlet.collector | 6.0.2 | latest | |

- [ ] **[P0] Security:** `Microsoft.EntityFrameworkCore.Design` 10.0.0 flows transitively into the
      app (no `PrivateAssets="all"`), which pulls `System.Security.Cryptography.Xml` 9.0.0 with
      known high-severity advisories (NU1903, e.g. GHSA-cvvh-rhrc-wg4q). Set `PrivateAssets="all"`
      on the Design reference in `RomManagerShared.csproj` (it is design-time only) and bump to
      10.0.x. Confirm with `dotnet nuget why <proj> System.Security.Cryptography.Xml`.
- [ ] Fix the existing warnings: move `OpenFolderDialog`/`OpenFileDialog` to
      `TopLevel.StorageProvider`, and fix the nullability warnings in `ConsoleViewModel.cs:371-427`.
- [ ] Remove `Microsoft.NETCore.Platforms` from the root `Directory.Build.props` once DiscUtils is
      replaced (it only existed to work around DiscUtils' old transitive dependencies).

## Phase 2: Data safety and correctness bugs (2–3 days)

These were all reproduced against real files with `tools/ParserHarness`.

- [ ] **[P0] Open ROM files read-only everywhere.** A bare `new FileStream(path, FileMode.Open)`
      requests write access and an exclusive lock. It fails on read-only shares and can lock out
      emulators. Affected: `PS2/Parsers/PS2RomParser.cs:38`, `3DS/3DSReader.cs:15,27`,
      `Wii/Parsers/WadInfo.cs:124,639,680`, `PSVita/PSVitaUtils.cs:69,85`. Add a single
      `FileUtils.OpenRead(path)` helper and an analyzer or grep check in CI.
- [ ] **[P0]** `3DS/3DSReader.cs` never disposes its `BinaryReader`/`FileStream`, so handles leak.
- [ ] **[P0]** The Wii WAD parser writes to the base directory path (`Access to the path 'D:\RomManager'
      is denied`). Send extracted files to `GetCachePath()/<console>/<titleId>/`.
- [ ] **[P0]** The OG Xbox parser writes an icon folder (`54540082/`) straight into `BaseDirectory`.
      Send it to the cache as well.
- [ ] **[P0]** `WiiUTMDTIKParser` runs `cdecrypt.exe`, which decrypts the title in place. That is
      slow (2–3 s per title) and writes into the ROM folder. Make it opt-in until phase 4 replaces it.
- [ ] **[P0]** Remove the runtime `pip install -r` in `FileUtils` (around line 225). Installing
      packages silently is a supply-chain risk.
- [ ] 3DS CIA: `NullReferenceException` on update CIAs (MK7 v3.5.0). Add a fixture test.
- [ ] Switch: import the `.tik` tickets from the NSP before opening NCAs (snippet in ADR 0001).
      At the moment every titlekey-encrypted NSP fails with `Missing NCA title key`, and the results
      only come from the header-scan fallback, so icons, multi-language titles and sizes are lost.
- [ ] `SwitchRomNSPXCIParser`: the instance fields (`localStorage`, `NSPPartitionFileSystem`,
      `RomList`) make it stateful and grow without bound. Make them locals and dispose them with
      `using`.
- [ ] PS2: one ISO yields 2 games (`SLES_500.09` and `SLUS_200.90`), because every `SLxx` filename on
      the disc is treated as a game. Read `SYSTEM.CNF` → `BOOT2` instead.
- [ ] PSP ISO: two parsers (`PSPISOSFOParser`, `PSPRomParser`) both match `iso`, and one of them
      returns an empty `PSPGame`. Merge them and drop roms with an empty TitleID in the executor.
- [ ] Wii U WUX: returns a `WiiUGame` with no ID or title. Either implement it (parse the WUX header,
      then the partition table's `title.tmd`) or return `[]` and report the file as unidentified.
- [ ] Xbox 360 XEX/ISO: returns 0 results and logs nothing. Find the swallowed path and log it.
- [ ] `RomParserExecutor`: sort parsers in the constructor too (currently only in `AddParser`). Treat
      a null or empty TitleID as "unidentified" rather than merging it.
- [ ] Remove the stray `Console.WriteLine` debug output from the parsers (DS, GB, SNES, Xbox, Vita
      print rom dumps and binary data to stdout).
- [ ] The NSZ header-scan parser decodes the header as UTF-8 and slices 32 characters before `.tik`.
      Bound-check it and validate the result as 32 hex characters.
- [ ] Check EF model drift: run `dotnet ef migrations has-pending-model-changes`, then add a migration.

## Phase 3: Architecture (1–2 weeks)

### 3.1 Persist the library and scan incrementally [P1]
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

### 3.2 Scanner service off the UI thread [P1]
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
- Give `GamingConsole` content: a descriptor record (Id, DisplayName, Manufacturer, Generation,
  Extensions, Icon), registered as data, so the UI stops deriving names from type names
  (`_consoleType.Name.Replace("Console","")`).
- Align namespaces with folders.
- Narrow `Rom`: platform-specific fields move to the subclasses.
- Replace the reflection `CopyNonNullProperties` merge with an explicit `Merge(Rom other)`.

### 3.5 Hash identification [P1]
- Wire `NoIntroRomHashIdentifier` into the scan for cartridge consoles (GB/GBC/GBA/NES/SNES/N64/DS):
  CRC32 by streaming → DAT lookup → canonical title, region and languages. Support Redump DATs for
  discs.
- Download and update DATs from the settings screen, and store them in the database.

### 3.6 Plugins [P2]
- `PluginPermission` is only declared, not enforced. At minimum, show a consent dialog at import
  listing the permissions, and keep the list of plugins the user approved.
- Version the `IPlugin` contract (`[assembly: PluginApiVersion(1)]`) and refuse plugins built for a
  different version.

## Phase 4: Format coverage (ongoing, by value to your library)

Ordered by how many files of each you actually have (Everything counts: nsz 1230, wad 1687 (mostly
game data, not Wii), gbc 367, nsp 97, pkg 56, cso 46, iso 33).

- [ ] **NSZ/XCZ real parsing**: NCZ section headers + `ZstdSharp` to read the control NCA (NACP,
      icon). [P1]
- [ ] XCI via LibHac (the parser claims to support it, but it is untested: no XCI files available).
- [ ] PKG (PS3/PSP/Vita) in C#: the PKG header plus PARAM.SFO is a short read. Delete the Python
      script and its `aenum` dependency. [P1]
- [ ] PS2: CSO/ZSO (block decompression), CHD (`libchdr` P/Invoke or a managed reader), UDF via
      LTRData.DiscUtils.
- [ ] PS1: cue/bin, CHD → read `SYSTEM.CNF` from the ISO9660 track.
- [ ] GameCube/Wii: ISO, CISO, RVZ/WIA (the disc header holds ID6 and title in the first 0x440
      bytes, even for RVZ). [P1, you have GameCube files]
- [ ] NES (iNES header + No-Intro hash), Genesis/MD, Master System, PCE, Dreamcast GDI/CHD.
- [ ] Wii U: replace `cdecrypt.exe` with an in-process TMD/TIK reader. Only decrypt `meta.xml`
      when the title key is available.
- [ ] Switch: missing-update/DLC report (the backend exists in `SwitchManager.GetMissingUpdates`)
      using the downloaded `versions.json`. Today it logs "Switch version path not found" until the
      title DB is downloaded, so prompt for the download instead.

## Phase 5: UI and UX (1–2 weeks, can overlap phase 4)

### Structure
- [ ] Move `MainView.axaml.cs` (1,900 lines of code-built UI) to AXAML views + ViewModels with
      compiled bindings: `ShellView` (nav + toolbar), `LibraryView` (grid/cards), `RomDetailsView`,
      `BottomPanelView` (Log/Downloads/Operations tabs), and dialogs as UserControls hosted by an
      `IDialogService`. Then upgrade to Avalonia 12.
- [ ] Context menu items and actions become `ICommand`s on the VM, so they can be tested without UI.

### UX
- [ ] **Empty states**: a tab with no path offers "Choose folder…". A tab with a path but no data
      offers "Scan now". There should be no bare "Ready" over an empty grid.
- [ ] **Dashboard / home**: counts per console, total size, unidentified files, missing updates,
      duplicates, the last scan, and errors (linking to the log).
- [ ] **Navigation**: a left sidebar with consoles (icon + count) instead of tabs, plus an
      "All consoles" view with a console column.
- [ ] **Grid / cards toggle** with box art (Switch icon from NACP, PS4 icon0.png, 3DS SMDH,
      libretro-thumbnails for cartridges), cached on disk with thumbnails generated by SkiaSharp.
- [ ] **Virtualization**: the DataGrid is fine; the card view needs `ItemsRepeater` with
      `UniformGridLayout`. Test with 5k+ rows.
- [ ] **Filters**: unidentified only, has update or no update, region, language, format, duplicates,
      and text search across title, TitleID and filename.
- [ ] **Per-file scan progress** in the status bar with cancel. Errors are clickable and show the
      file and reason.
- [ ] Drag-and-drop a folder onto the window to add it as a ROM root (with console auto-detect).
- [ ] Rename and move: show a preview table (old → new) before running. Undo already exists in
      `OperationQueue`.
- [ ] Settings: validate paths (green/red), show the key status for Switch (`prod.keys` version,
      `title.keys` count), and add a "Download title DBs" button with progress.
- [ ] Remember the window position as well as the size, and handle DPI and multi-monitor setups.
      The window currently restores at an offset.
- [ ] Keyboard: Ctrl+F for search, F5 for an incremental scan, Shift+F5 for a full rescan, Del to
      delete with confirmation, Enter for details.
- [ ] Accessibility: use real menus (a `Menu` control, not buttons that open flyouts), set
      `AutomationProperties.Name` on the icon buttons, and keep emoji out of menu labels (they
      render inconsistently on Linux).

## Phase 6: Testing and release

- [ ] **Parser fixture tests**: small or homebrew files per format under `RomManagerTests/Fixtures`
      (generate truncated headers where real dumps can't be committed), plus a golden JSON of the
      expected `Rom`s.
- [ ] Keep `tools/ParserHarness` for local smoke runs against a real library. Add `--json` output so
      runs can be diffed before and after a change.
- [ ] Scanner integration test: temp folder → scan → modify → rescan → assert the diff.
- [ ] ViewModel tests for the filters, commands and empty states.
- [ ] Release: `dotnet publish -r win-x64/linux-x64/osx-arm64` self-contained, single-file. Add
      Velopack for installers and auto-update, and keep the version in `Directory.Build.props`.
- [ ] Update `README.md`: features, supported formats matrix (from ARCHITECTURE.md), keys setup,
      screenshots.

---

## Suggested order of first PRs

1. Phase 0 commit plus junk/dead-project removal.
2. `FileUtils.OpenRead` + fix every `FileMode.Open` + the 3DS reader disposal (P0).
3. Cache path for the Wii/Xbox extraction, cdecrypt opt-in, remove the runtime `pip install`.
4. Switch ticket import + `Ryujinx.LibHac` swap (ADR 0001).
5. Package bumps (Avalonia 11.3, EF 10.0.x, LTRData.DiscUtils, test stack).
6. Library persistence + incremental scanner (biggest UX gain).
7. UI move to AXAML, then the dashboard, empty states and box art.
