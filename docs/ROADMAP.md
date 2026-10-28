# Roadmap v5

Written 2026-10-28 after [v4](ROADMAP-v4.md), from a review of the context menu, the plugin system and the UI.
The app is not public yet, so the plugin contract changes in place: it stays plugin API 1.

Legend: **[P0]** data safety or correctness, **[P1]** high value, **[P2]** nice to have.

---

## Phase A: Context menu

- [x] **[P0]** "✏️ Rename Files" (Organise submenu) renamed every selected file with a hard-coded template: no
      preview, no confirmation, no undo, next to the real Rename (F2). Remove it.
- [x] **[P1]** "🔍 Find Duplicates" only wrote to the log. Remove it; "Duplicates…" on the page already shows a
      preview and resolves them.
- [x] **[P1]** "#️⃣ Calculate Hashes" only logged its results. It becomes a built-in menu item that shows the
      hashes when done and keeps them: they are written into the file's cached scan result, so the grid's Hashes
      column and Properties show them (until the file changes).
- [x] **[P2]** Switch "Group ROMs by Title" grouped a list on a throwaway manager and changed nothing on screen
      (the page has its own grouping toggle). Remove it.
- [x] **[P1]** The cards view had no context menu; it gets the table's menu (right-click selects the card).
- [x] **[P2]** Action names lose their emoji and come from `Strings.resx`; action and plugin option icons
      (`IconKey`: an image file next to the plugin or an app resource) are shown.
- [x] **[P2]** Plugin options are grouped under the plugin's name (or the option's own `Category`) instead of
      one generic "Plugins" submenu.
- [x] **[P2]** The column picker's "Reset to defaults" and the plugin switches' "Enabled/Disabled" come from
      `Strings.resx`.

## Phase B: Library metadata that survives rescans

- [x] **[P1]** `RomMetadataOverrides` table (console, title ID, rom type → title, description, publisher,
      developer, release date, genres, cover): applied to roms after enrichment and when the cache loads, so a
      rescan or a moved file keeps them. Written by plugins (C) with undo.

## Phase C: Plugin API (still version 1)

- [x] **[P1]** `IPlugin.ExecuteAsync(optionIndex, context, cancellationToken)`; plugin actions run in the
      operation queue, so Cancel works, and report progress through `IPluginContext.Progress`.
- [x] **[P1]** `IPluginContext.UpdateMetadataAsync(rom, RomMetadata)`: titles, descriptions, publisher,
      developer, release date, genres and a cover image, saved as overrides (B). Needs the new
      `PluginPermission.LibraryMetadata`. Undoable from the Operations panel.
- [x] **[P1]** Results without Avalonia: `ShowMessageAsync`, `ShowTableAsync`, `ConfirmAsync`,
      `PickSaveFileAsync` (a file the user picks may be written without the FileSystem permission) and
      `OpenInBrowser` (http/https only).
- [x] **[P1]** `SelectedRoms` is a copy: a plugin that changes it no longer changes the library behind the app's
      back; changes go through `UpdateMetadataAsync`.
- [x] **[P1]** `ILibraryHooks.OnFilesChangedAsync`: renamed, moved and deleted files (default no-op).
- [x] **[P1]** Secret settings are encrypted at rest: DPAPI (current user) on Windows; elsewhere the file is
      readable by the user only (0600).
- [x] **[P2]** `SupportedConsoles` entries that match no console are logged when the plugin loads.
- [x] **[P2]** Template and sample plugin follow the contract; README documents it.

## Phase D: Plugins that ship with the app

Bundled plugins live in `<app>/BuiltInPlugins/<Name>/` (sources in `Plugins/<Name>/`), are built with the app and are trusted like the app itself (no
approval prompt). Plugins the user adds still need approval.

- [x] **[P1]** RAWG metadata (the sample, now shipped): looks up the selection, shows a table of matches and saves
      the chosen metadata and cover to the library (with undo).
- [x] **[P1]** Web search: search the selection on the web (search engine URL in the plugin's settings;
      YouTube and GameFAQs options).
- [x] **[P1]** Playlists: export the selection as a RetroArch playlist (`.lpl`) or an EmulationStation
      `gamelist.xml`.

## Phase E: UX pass

- [x] Render every screen (headless screenshots) and fix what looks wrong: plugin messages used the preview dialog with an
      empty list; they now use the message dialog (no Cancel, scrolls when long). New shots: plugin results and message,
      Settings → Plugins.

## Phase F: Tests and docs

- [x] Tests for each change; ARCHITECTURE, README, SamplePlugin README and the template updated.
