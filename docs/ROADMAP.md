# Roadmap v2

Written 2026-10-27 after finishing most of [v1](ROADMAP-v1.md) and a review of the branch. v1 covered
hygiene, data safety, persistence, the AXAML UI and format coverage. v2 is about finishing the
architecture, closing correctness gaps, and the features that make the app worth using daily.

Each phase should leave the app building with green tests. Tick items off as they land.
Legend: **[P0]** data safety or correctness, **[P1]** high value, **[P2]** nice to have.

---

## Phase A: Dependencies and build

- [x] **[P1]** Clear the remaining RomManagerShared warnings and treat warnings as errors there too
      (the whole solution now builds with zero warnings).
- [x] Removed `SkyEditor.Utilities.AsyncFor`: plain loops in the vendored 3DS toolkit; the unused
      `ThreeDsRom.ExtractFiles` (its only `ProgressReportToken` user) is gone.
- [x] `ItemsRepeater` and `DataGrid` follow Avalonia 12.
- [x] Android/Browser heads deleted (unmaintained since net9; recoverable from git history).
- [x] Dependabot for NuGet (grouped) and GitHub Actions.
- [x] Code coverage in CI (`Microsoft.Testing.Extensions.CodeCoverage`, Cobertura artifact; ~43% lines).
- [x] CI builds and tests on windows, ubuntu and macos (test paths made portable).
- [x] Avalonia 12.1.4 (DataGrid 12.1.2, ItemsRepeater 12.0.0). Avalonia.Diagnostics has no v12 and was dropped.
      v12 binds the UI thread strictly: view-model brushes are immutable, the theme preview posts to the UI
      thread, and the headless test session starts before any other test.

## Phase B: Architecture

### B.1 Parser contract [P1]
- [x] `RomParserExecutor.ParseAsync(file, ct)` returns a `ParseResult` (roms, diagnostics, unidentified
      reason); `IRomParser<T>.ProcessFile(path, ct)` is a default method, so existing parsers work unchanged.
- [x] Content sniffing: `IContentSniffer.CanParse(path, header)` + `FileMagic` (PSN/PS4 packages, GameCube/Wii
      discs, PSP/PlayStation ISOs). Used by the executor and by the add-folder console detector.
- [x] Cancellation reaches the long work: title-info providers take a token (`GetTitleInfo(rom, ct)`), so a
      cancelled scan stops Redump hashing mid-file (nothing partial is cached). Parsers only read headers;
      they can override `ProcessFile(path, ct)` when that changes.

### B.2 Static state [P1]
- [x] `FileUtils.Log` → Microsoft.Extensions.Logging: `AppLog.Info/Warn/Error` (category = calling file, level
      by meaning; 138 warnings, 40 info) through the host's `ILoggerFactory`; `LogFileProvider`/`LogSink` keep the
      log file, log panel and `ParseDiagnostics`. Log writes are serialised and fall back to a per-user path.
- [x] `ISettingsStore` (`JsonSettingsStore`): atomic saves (a crash used to be able to truncate settings.json),
      a corrupt file is kept as `settings.corrupt-*.json` instead of being replaced by defaults on the next save,
      and new installs keep settings in `%AppData%/RomManager` (an update replacing the program folder would
      have wiped them; an existing settings.json next to the exe is still used).
- [ ] Fold config.json into the same options model (`IOptionsMonitor<AppOptions>`); `RomManagerConfiguration`
      is still static.
- [x] `MainViewModel.RomPaths` (static) → `ILibraryRoots` (DI singleton, reads settings on each call, no reload
      step); several folders per console scanned as one library, one watcher each; an offline folder keeps
      its cached rows; adding a folder for a configured console offers "Add as another folder".

### B.3 Domain [P2]
- [x] Replace reflection `RomUtils.CopyNonNullProperties` with `Rom.MergeFrom(Rom other)`: explicit for `Rom`
      fields (lists combined, flags OR-ed, so a second parser no longer clears `IsFolderFormat`); subclass
      properties via a cached property map.
- [x] ~~Narrow `Rom`~~ — decided against after measuring: every field is set by two or more consoles or is
      generic metadata (Banner/IsDemo are Switch-only today but generic); moving them would change the DB
      schema and every stored snapshot for no gain.
- [x] Namespaces match folders (`NamespaceTests`). Mostly files moved (3DS → ThreeDS, N64 → Nintendo64,
      Configuration/Parsers sub-folders) so stored rom type names stay valid; only parsers/readers changed namespace.
- [ ] Split heavy consoles into module assemblies (ADR 0003 step 2). **Blocked:** `AppDbContext` maps Switch and
      Wii U title-DB entities, so modules first need to contribute their own EF configuration. → v3.

### B.4 Scanning
- [x] Polling fallback for ROM folders on network drives/UNC paths (every 2 minutes: file count, total
      size and newest write time of matching files).
- [x] Per-console scan settings (`ScanOptions`, Settings → Scanning): parallelism (automatic: 1 on network
      drives, else 4) and excluded sub-folders.
- [x] Hash cache (`Cache/hashes.json`, keyed by path, size and mtime) so Redump matching never re-hashes
      an unchanged disc image when DATs change.

## Phase C: Correctness and data safety

- [x] **[P0]** Moving folder-format roms (Wii U, PS3, Xbox) moves the whole folder (rename on one volume,
      length-checked copy then delete across volumes); roms sharing a file move once. Undo restores whole
      folders and every rom of a shared file (it used to move only the marker file back).
- [x] **[P0]** Delete moves files to the Recycle Bin (fixed drives) / Trash (macOS, freedesktop on Linux);
      files that cannot be trashed are only deleted after a second, explicit confirmation.
- [x] **[P1]** Persist the operation journal so undo survives a restart (`OperationJournal`, `journal.json`
      next to the database; clearing finished operations forgets their entries). Single-rom rename undo
      now restores folder-format roms too.
- [x] **[P1]** Redump for multi-track discs: PS1/PS2 `.cue` and Dreamcast `.gdi` sets match by their first
      track. (CHD's internal SHA-1 covers the whole compressed image and cannot match Redump's per-track
      hashes; matching CHDs needs decompression.)
- [ ] XCI parsing test. **Needs a real file:** an XCI cannot be identified without keys, and building an
      encrypted XCI fixture is not practical; run ParserHarness on one of yours.
- [x] Switch missing-DLC report (`SwitchDlcChecker`): DLC IDs in the already-downloaded titledb whose base game
      (DLC ID − 0x1000, low 13 bits cleared) is in the library; no extra download (`cnmts.json` not needed).
- [x] PS4 missing updates from Sony's update XML (`PS4UpdateChecker`; HMAC-keyed URL, TLS pinned to Sony's
      private root), opt-in per session or in Settings; never run for the dashboard total.

## Phase D: UI and UX

- [x] **[P1]** `IDialogService` (`DialogService.Current`, faked in tests); confirm, preview, properties, rename and
      choose-console dialogs are AXAML views hosted by `DialogWindow`.
- [x] Settings as an AXAML view (`SettingsViewModel` + `SettingsView`). It edits a draft: Cancel used to keep
      every edit in memory (saved by the next unrelated save); plugin reload refreshes the list in place.
- [x] Onboarding as an AXAML view (`OnboardingViewModel`). It no longer reappears on every start after being
      skipped, and is not shown when config.json already has ROM folders.
- [x] Settings: toggle Redump hashing (settings.json overrides config.json).
- [x] Settings: scan parallelism and excluded folders.
- [x] Settings: DAT overview per console (sets and game counts, on request) and "Open DAT folder".
- [x] Plugins page: the enable/disable toggle is now enforced (it was saved but ignored), permissions
      shown, "Revoke approval".
- [x] Unrecognised files: "Move to folder…" from the list (preview, keeps sub-folders, rescans after).
- [x] Profile 5k rows (`HeadlessUiTests.LargeLibrary_StaysResponsive`, synthetic): load 371 ms, filter 158 ms,
      switch to cards 80 ms with 25 cards realized. Cover loading on a real library is still unmeasured.
- [x] Keyboard navigation in the card view: arrows, Home/End, PageUp/PageDown (`CardNavigation`); Delete,
      F2 and Alt+Enter act on the focused card.
- [x] README screenshots, rendered by `HeadlessUiTests.Screenshots` (Skia, the app's own styles from
      `AppStyles.axaml`) with `SCREENSHOTS=1`. The dashboard now counts consoles that already have roms.
- [ ] Localisation (resx). Prerequisite done: every window is AXAML now. → v3.

## Phase E: Features

- [x] **[P1]** Have/miss report per DAT (dashboard → "Have/miss report…"; `DatCoverage`, counts verified
      hash matches against every game in the console's DATs).
- [x] **[P1]** Export the library (CSV/JSON): Library → Export library…, and "Export…" on a console page
      for the filtered rows (`LibraryExporter`; CSV is formula-safe).
- [x] 1G1R ("one game, one ROM", `OneGameOneRom`): groups versions by DAT parent (parent/clone DATs) or clean
      title, keeps the best by region priority (Settings), good dumps and newest revision; "1G1R…" on a console
      page previews and moves the others (undoable).
- [x] Per-console emulator launch: Settings → Emulators (path + arguments with {rom}/{dir}/{name}),
      "Launch in emulator" in the rom context menu.
- [x] Convert/compress through user-supplied tools (`ConversionTools`: chdman CD/DVD → CHD, DolphinTool → RVZ,
      maxcso → CSO, nsz → NSZ) as queued, cancellable operations: the tool runs in `Cache/Temp/<guid>`, the
      result is moved next to the original (kept; never overwritten), undo deletes the outputs. Tool paths in
      Settings → Conversion tools; "Convert" appears in the rom menu when a tool is present.

## Phase F: Plugins

- [x] **[P1]** Capability-scoped context: `IPluginContext.CreateHttpClient` needs `Network`, `OpenFile` outside
      the plugin data folder needs `FileSystem`, `StartProcess` needs `ExternalProcess`, and `Services` hides
      database services without `Database` (`PluginPermissionException`). Cooperative, not a sandbox.
- [x] `RomManager.Abstractions` assembly: the plugin contract and the rom model (same namespaces). Plugins and
      the template reference only it; RomManagerShared forwards the moved types, so stored library entries and
      plugins built against RomManagerShared still resolve. `PluginApi.Version` stays 1 (binary compatible).
- [x] Plugin hooks: `ILibraryHooks.OnScanCompletedAsync` with the roms added/removed by each scan.
- [x] `dotnet new` template for plugins (`templates/RomManagerPlugin`, short name `rommanager-plugin`). Plugin
      load contexts now share assemblies the host already loaded, so a plugin shipping `RomManagerShared.dll`
      loads (it was silently skipped: its `IPlugin` was a different type).

## Phase G: Testing and release

- [x] Golden JSON for parser fixtures (`RomManagerTests/Golden`, `UPDATE_GOLDEN=1` to refresh);
      `tools/ParserHarness --json`.
- [x] Headless UI tests (Avalonia.Headless, `HeadlessUiTests`): dialogs clicked with a real pointer, the
      dashboard, and the console page's grid.
- [x] Velopack: the release workflow builds Setup.exe + portable zip (Windows), AppImage (Linux), .app/.pkg
      (macOS) and the update feed; Help → Check for app updates (and an opt-in startup check) installs newer
      releases. Code signing runs when the `WIN_SIGN_PARAMS` / `MAC_SIGN_*` secrets are set (none yet).

---

## Done in v2

(Move items here as they land.)
