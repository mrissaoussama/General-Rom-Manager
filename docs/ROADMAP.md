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
- [x] `ItemsRepeater` 11.1.5 and `DataGrid` 11.3.13 are the newest 11.x releases (next step is Avalonia 12).
- [ ] Decide on the Android/Browser heads: delete them (recommended; unmaintained since net9) or
      retarget and add them to the solution.
- [x] Dependabot for NuGet (grouped) and GitHub Actions.
- [x] Code coverage in CI (`Microsoft.Testing.Extensions.CodeCoverage`, Cobertura artifact; ~43% lines).
- [x] CI builds and tests on windows, ubuntu and macos (test paths made portable).
- [ ] Avalonia 12 (after phase D's dialog work).

## Phase B: Architecture

### B.1 Parser contract [P1]
- [x] `RomParserExecutor.ParseAsync(file, ct)` returns a `ParseResult` (roms, diagnostics, unidentified
      reason); `IRomParser<T>.ProcessFile(path, ct)` is a default method, so existing parsers work unchanged.
- [x] Content sniffing: `IContentSniffer.CanParse(path, header)` + `FileMagic` (PSN/PS4 packages, GameCube/Wii
      discs, PSP/PlayStation ISOs). Used by the executor and by the add-folder console detector.
- [ ] Parsers honour cancellation inside long reads (CHD/ISO): the executor stops between parsers; the
      parsers themselves still need to override `ProcessFile(path, ct)`.

### B.2 Static state [P1]
- [ ] `FileUtils.Log` → `ILogger<T>` (keep `ParseDiagnostics` by capturing through a logger scope).
- [ ] One `IOptionsMonitor<AppOptions>` from config.json + settings.json, saved through `ISettingsStore`.
- [ ] `MainViewModel.RomPaths` (static) → `ILibraryRootsService`; several roots per console.

### B.3 Domain [P2]
- [x] Replace reflection `RomUtils.CopyNonNullProperties` with `Rom.MergeFrom(Rom other)`: explicit for `Rom`
      fields (lists combined, flags OR-ed, so a second parser no longer clears `IsFolderFormat`); subclass
      properties via a cached property map.
- [ ] Narrow `Rom`: platform-only fields move to subclasses.
- [ ] Align namespaces with folders (mechanical, one PR, no behaviour change).
- [ ] Split heavy consoles into module assemblies (ADR 0003 step 2: Switch/LibHac, Xbox/ISO2GOD).

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
- [ ] XCI parsing test with a homebrew XCI fixture.
- [x] Switch missing-DLC report (`SwitchDlcChecker`): DLC IDs in the already-downloaded titledb whose base game
      (DLC ID − 0x1000, low 13 bits cleared) is in the library; no extra download (`cnmts.json` not needed).
- [ ] PS4 update check without PS4_Tools (Sony's public update XML), behind a network opt-in.

## Phase D: UI and UX

- [x] **[P1]** `IDialogService` (`DialogService.Current`, faked in tests); confirm, preview, properties, rename and
      choose-console dialogs are AXAML views hosted by `DialogWindow`.
- [ ] Settings and Onboarding as AXAML views (still built in code; shown through `IDialogService.ShowWindowAsync`).
- [x] Settings: toggle Redump hashing (settings.json overrides config.json).
- [x] Settings: scan parallelism and excluded folders.
- [x] Settings: DAT overview per console (sets and game counts, on request) and "Open DAT folder".
- [x] Plugins page: the enable/disable toggle is now enforced (it was saved but ignored), permissions
      shown, "Revoke approval".
- [x] Unrecognised files: "Move to folder…" from the list (preview, keeps sub-folders, rescans after).
- [ ] Profile the card view with 5k+ rows on a real library.
- [ ] Keyboard navigation in the card view.
- [ ] README screenshots.
- [ ] Localisation (resx) once strings are in AXAML.

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
- [ ] Convert/compress through user-supplied tools (chdman, DolphinTool, nsz) as queued operations
      (ADR 0002: optional tools, never bundled).

## Phase F: Plugins

- [x] **[P1]** Capability-scoped context: `IPluginContext.CreateHttpClient` needs `Network`, `OpenFile` outside
      the plugin data folder needs `FileSystem`, `StartProcess` needs `ExternalProcess`, and `Services` hides
      database services without `Database` (`PluginPermissionException`). Cooperative, not a sandbox.
- [ ] Separate `RomManager.Plugins.Abstractions` assembly (contract only) so plugins stop referencing
      all of RomManagerShared; version it with `PluginApi.Version`.
- [x] Plugin hooks: `ILibraryHooks.OnScanCompletedAsync` with the roms added/removed by each scan.
- [ ] `dotnet new` template for plugins.

## Phase G: Testing and release

- [x] Golden JSON for parser fixtures (`RomManagerTests/Golden`, `UPDATE_GOLDEN=1` to refresh);
      `tools/ParserHarness --json`.
- [x] Headless UI tests (Avalonia.Headless, `HeadlessUiTests`): dialogs clicked with a real pointer, the
      dashboard, and the console page's grid.
- [ ] Velopack installers and auto-update; macOS `.app` bundle; code signing.

---

## Done in v2

(Move items here as they land.)
