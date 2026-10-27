# Roadmap v3

Written 2026-10-27 after finishing [v2](ROADMAP-v2.md) (v1: [ROADMAP-v1.md](ROADMAP-v1.md)) and reviewing the
branch again. v2 left three items open for good reasons (below); the review found the rest.

Legend: **[P0]** data safety or correctness, **[P1]** high value, **[P2]** nice to have.

---

## Phase A: Dependencies and build

- [x] Microsoft.Extensions / EF Core / System.IO.Hashing 10.0.13 → 10.0.14.
- [ ] **[P1]** `Ryujinx.LibHac` 0.20.0 is unlisted on nuget.org (Ryujinx ended in 2024). It still restores, but
      plan a fallback: pin the package in a local feed or vendor the parts we use (NCA/PFS0/XCI, keys).
- [ ] Linux and macOS CI have not run since the headless UI tests and Avalonia 12 landed; check them on the
      next push (Skia needs fontconfig on Linux runners).

## Phase B: Architecture

- [x] **[P1]** Every console is a module assembly in `Consoles/` (ADR 0003 outcome). The EF model is core-only
      (unused rom/title-DB tables dropped), and modules reach the app only through interfaces
      (`[ConsoleInfo]`, `IFolderLayout`, `IDownloadableResources`, `IConsoleOptions`). Removing a module DLL
      leaves the app working (`MissingModuleTests`; checked with the real app).
- [ ] Fold config.json into an options model (`IOptionsMonitor<AppOptions>`); `RomManagerConfiguration` is
      still a static facade read everywhere.
- [x] **[P1]** Removed dead code: the PS4 Store scraper (`PS4PKGUpdateAndDLCChecker`, its HtmlAgilityPack use),
      the never-implemented `IRomMissingContentChecker` (PS4, 3DS stubs that only log "not implemented") and
      `RomMissingUpdates`/`RomMissingDLCs`. `PROJECT_ANALYSIS.md` is stale: delete it.

## Phase C: Correctness and data safety

- [x] **[P0]** Scanner: a file deleted or moved mid-scan aborted the scan; paths collided by case on Linux.
- [x] **[P0]** Move, undo, rename, delete and the plugin loader compare paths like the OS; case-only renames apply.
- [x] In-app update installs after a normal exit (it used to kill the process; window placement was lost).
- [x] **[P1]** A ROM folder that is offline keeps its cached rows (v2), but nothing told the user. Show
      offline folders on the console page and the dashboard, with "remove folder" for ones that are gone.
- [x] **[P1]** Box art downloads from libretro-thumbnails happened without asking. Make network use explicit:
      one "Online features" section in Settings (box art, title database downloads, PS4 updates, app updates).

## Phase D: UI and UX

- [x] **[P1]** Localisation: UI text in `AvaloniaUI/Resources/Strings.resx` (built-in strongly-typed resources, `{x:Static res:Strings.Key}` in AXAML,
  `Labels` for property names and filter values). English only for now; a language is a `Strings.<culture>.resx` file.
- [x] Duplicates…: keeps one file per identical rom (same ID, type and version; a DAT-verified dump or the largest)
      and moves the other copies after a preview (`DuplicateDetector.FindExactCopies`).
- [x] "Verify" report per console (`DatVerification`): which dumps match a DAT exactly and which do not.

## Phase E: Features

- [ ] XCI parsing test — needs a real XCI (cannot be identified without keys).
- [x] Scheduled rescans: Settings → Scanning → "Rescan every (hours)".

## Phase F: Plugins

- [x] Plugin settings: `IPluginSettings` declares options (Text, Secret, Boolean); Settings → Plugins edits them;
      plugins read `IPluginContext.Settings`. The sample plugin's API key moved there.

## Phase G: Testing and release

- [ ] Code signing certificates (Windows Authenticode, Apple Developer ID); the workflow already uses them when
      the secrets exist.
- [ ] Coverage gate in CI once Linux/macOS are green.
