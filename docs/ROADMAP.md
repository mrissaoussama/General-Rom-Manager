# Roadmap v2

Written 2026-10-27 after finishing most of [v1](ROADMAP-v1.md) and a review of the branch. v1 covered
hygiene, data safety, persistence, the AXAML UI and format coverage. v2 is about finishing the
architecture, closing correctness gaps, and the features that make the app worth using daily.

Each phase should leave the app building with green tests. Tick items off as they land.
Legend: **[P0]** data safety or correctness, **[P1]** high value, **[P2]** nice to have.

---

## Phase A: Dependencies and build

- [ ] **[P1]** Clear the remaining RomManagerShared warnings (121 nullable warnings in first-party code:
      Switch/3DS title providers, PS Vita organizer, repositories), then turn on
      `TreatWarningsAsErrors` and `AnalysisLevel latest` for it.
- [ ] Remove `SkyEditor.Utilities.AsyncFor` (old netstandard package used by the vendored 3DS toolkit for
      `ProgressReportToken` and parallel loops). Replace with plain loops and a small progress type.
- [ ] Align `Avalonia.Controls.ItemsRepeater` (11.1.5) and `Avalonia.Controls.DataGrid` with Avalonia.
- [ ] Decide on the Android/Browser heads: delete them (recommended; unmaintained since net9) or
      retarget and add them to the solution.
- [ ] Dependabot for NuGet and GitHub Actions.
- [ ] Code coverage in CI (`Microsoft.Testing.Extensions.CodeCoverage`), reported on PRs.
- [ ] CI builds on ubuntu-latest and macos-latest as well (the release workflow already publishes there).
- [ ] Avalonia 12 (after phase D's dialog work).

## Phase B: Architecture

### B.1 Parser contract [P1]
- [ ] `ValueTask<ParseResult> ParseAsync(RomFileContext ctx, CancellationToken ct)` with roms, diagnostics
      and an "unidentified" reason. Adapter keeps old `IRomParser<T>` parsers working while they move.
- [ ] Content sniffing: `CanParse(ReadOnlySpan<byte> header)` so `.iso`/`.pkg`/`.bin` pick the right
      console in mixed folders.
- [ ] Parsers honour cancellation (long CHD/ISO reads).

### B.2 Static state [P1]
- [ ] `FileUtils.Log` → `ILogger<T>` (keep `ParseDiagnostics` by capturing through a logger scope).
- [ ] One `IOptionsMonitor<AppOptions>` from config.json + settings.json, saved through `ISettingsStore`.
- [ ] `MainViewModel.RomPaths` (static) → `ILibraryRootsService`; several roots per console.

### B.3 Domain [P2]
- [ ] Replace reflection `RomUtils.CopyNonNullProperties` with an explicit `Rom.MergeFrom(Rom other)`.
- [ ] Narrow `Rom`: platform-only fields move to subclasses.
- [ ] Align namespaces with folders (mechanical, one PR, no behaviour change).
- [ ] Split heavy consoles into module assemblies (ADR 0003 step 2: Switch/LibHac, Xbox/ISO2GOD).

### B.4 Scanning
- [ ] Polling fallback for ROM folders on network shares, where `FileSystemWatcher` misses changes.
- [ ] Per-root scan settings: parallelism (1 for network/HDD), excluded sub-folders.
- [ ] Hash cache: store file hashes in `LibraryFile` so DAT/Redump matching never re-hashes unchanged files.

## Phase C: Correctness and data safety

- [ ] **[P0]** Moving folder-format roms (Wii U, PS3, Xbox 360 GOD) fails today; implement a verified
      copy-then-delete for folders, with free-space checks.
- [ ] **[P0]** Delete to the recycle bin / trash where the OS has one; permanent delete only on request.
- [ ] **[P1]** Persist the operation journal so undo survives a restart.
- [ ] **[P1]** Redump for multi-track and compressed discs: CHD header SHA-1 (no decompression needed),
      cue/bin track hashing, GDI.
- [ ] XCI parsing test with a homebrew XCI fixture.
- [ ] Switch missing-DLC report (titledb `cnmts.json`).
- [ ] PS4 update check without PS4_Tools (Sony's public update XML), behind a network opt-in.

## Phase D: UI and UX

- [ ] **[P1]** `IDialogService`; Settings, Onboarding and the preview/properties windows as AXAML views.
- [ ] Settings: toggle Redump hashing, scan parallelism, manage DAT files (which consoles are covered).
- [ ] Plugins page: enable/disable, revoke approval, show declared permissions.
- [ ] Unrecognised files: "Open folder" and "Move to quarantine" actions.
- [ ] Profile the card view with 5k+ rows on a real library.
- [ ] Keyboard navigation in the card view.
- [ ] README screenshots.
- [ ] Localisation (resx) once strings are in AXAML.

## Phase E: Features

- [ ] **[P1]** Have/miss report per DAT: which titles in a No-Intro/Redump set are missing.
- [ ] **[P1]** Export the library (CSV/JSON).
- [ ] 1G1R ("one game, one ROM") selection from DATs with a region priority list, feeding the move/delete
      previews.
- [ ] Per-console emulator launch (path + arguments), beyond "Open with default app".
- [ ] Convert/compress through user-supplied tools (chdman, DolphinTool, nsz) as queued operations
      (ADR 0002: optional tools, never bundled).

## Phase F: Plugins

- [ ] **[P1]** Capability-scoped context: `IPluginContext` hands out an `HttpClient` only with `Network`,
      file access only inside the plugin data folder unless `FileSystem` was approved.
- [ ] Separate `RomManager.Plugins.Abstractions` assembly (contract only) so plugins stop referencing
      all of RomManagerShared; version it with `PluginApi.Version`.
- [ ] Plugin hooks: scan finished, rom added/removed.
- [ ] `dotnet new` template for plugins.

## Phase G: Testing and release

- [ ] Golden JSON for parser fixtures; `tools/ParserHarness --json`.
- [ ] Headless UI tests (Avalonia.Headless) for the console page and dashboard.
- [ ] Velopack installers and auto-update; macOS `.app` bundle; code signing.

---

## Done in v2

(Move items here as they land.)
