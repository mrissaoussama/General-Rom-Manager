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

- [ ] **[P1]** Modules contribute their own EF configuration (`IConsoleModule.ConfigureModel`), then split
      Switch (LibHac) and Xbox (ISO2GOD) into module assemblies (ADR 0003 step 2, blocked in v2 on this).
- [ ] Fold config.json into an options model (`IOptionsMonitor<AppOptions>`); `RomManagerConfiguration` is
      still a static facade read everywhere.
- [ ] **[P1]** Remove dead code: the PS4 Store scraper (`PS4PKGUpdateAndDLCChecker`, its HtmlAgilityPack use),
      the never-implemented `IRomMissingContentChecker` (PS4, 3DS stubs that only log "not implemented") and
      `RomMissingUpdates`/`RomMissingDLCs`. `PROJECT_ANALYSIS.md` is stale: delete it.

## Phase C: Correctness and data safety

- [x] **[P0]** Scanner: a file deleted or moved mid-scan aborted the scan; paths collided by case on Linux.
- [x] **[P0]** Move, undo, rename, delete and the plugin loader compare paths like the OS; case-only renames apply.
- [x] In-app update installs after a normal exit (it used to kill the process; window placement was lost).
- [ ] **[P1]** A ROM folder that is offline keeps its cached rows (v2), but nothing tells the user. Show
      offline folders on the console page and the dashboard, with "remove folder" for ones that are gone.
- [ ] **[P1]** Box art downloads from libretro-thumbnails happen without asking. Make network use explicit:
      one "Online features" section in Settings (box art, title database downloads, PS4 updates, app updates).

## Phase D: UI and UX

- [ ] **[P1]** Localisation: resx strings with a `{l:Tr}` markup extension; English first, then French.
- [ ] Duplicates: a dialog listing each group with "keep / move the others" (as 1G1R does), instead of log lines.
- [ ] A "Verify" report per console: DAT-verified, unverified and unknown dumps.

## Phase E: Features

- [ ] XCI parsing test — needs a real XCI (cannot be identified without keys).
- [ ] Scheduled rescans (e.g. daily) for folders that are not watched (network drives already poll).

## Phase F: Plugins

- [ ] Plugin settings: let a plugin describe its options (key, label, type) and edit them in Settings → Plugins
      instead of hand-editing `config.json` in its data folder.

## Phase G: Testing and release

- [ ] Code signing certificates (Windows Authenticode, Apple Developer ID); the workflow already uses them when
      the secrets exist.
- [ ] Coverage gate in CI once Linux/macOS are green.
