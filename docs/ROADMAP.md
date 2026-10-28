# Roadmap v6

Written 2026-10-28 after [v5](ROADMAP-v5.md), from using the app on a small 3DS library (an encrypted cartridge dump
and its update CIA) and a list of problems and wishes.

Legend: **[P0]** data safety or correctness, **[P1]** high value, **[P2]** nice to have.

---

## Phase A: Bugs

- [x] **[P0]** Rename wrote placeholders into file names: the dialog offered the grid's column names
      (`{Product Code}`, `{FileName}`, `{Min Firmware}`) and the renamer left unknown ones as text. Placeholders
      now accept any case and spaces, `{FileName}` and the console's `[UiProperty]` values work, an unknown
      placeholder blocks the rename (and the dialog's Rename button), and `[{X}]` keeps its brackets when X has a
      value. One placeholder list (`FileRenamer.GeneralPlaceholders`) for the renamer and the dialog.
- [x] **[P0]** "On disk" showed 0 B: it summed title-database sizes, and the 3DS provider wrote the DB's size
      (0 when unknown) over the file's. The scanner now fills the size on disk (the whole folder for folder
      formats), providers only fill a missing size, and totals count each file once (`FileUtils.SizeOnDisk`).
      `ScanVersion` 18.
- [x] **[P1]** Updates and DLC were never linked to their game (`Game.Updates`, `RelatedGame` were always empty):
      `RomRelations.Link` links them after each load by base title ID (3DS/Wii U category, Switch update/DLC
      bits, shared IDs elsewhere). 3DS updates now group under their game. Links are not stored in the cache.
- [x] **[P1]** Updates and DLC take their game's icon when they have none; 3DS games whose icon cannot be read
      (encrypted dumps) show GameTDB box art.
- [x] **[P1]** Properties showed more than the side panel (and "N/A" lines for the links): both show the same
      details now; base game, updates and DLC are links to their rows; every value has a copy button.
- [x] **[P1]** Launch in emulator did nothing without an emulator: it offers to choose the program, and logs
      launches and failures.
- [x] **[P2]** Renames, moves and the organiser log through `AppLog` (they had loggers but were given none, or
      wrote to the console).

## Phase B: Library folders

- [x] **[P1]** Settings → Library folders: folders the app organises, one sub-folder per console named after it;
      scanned without listing them per console. "Create console folders" makes them.
- [x] **[P1]** File → Add to library (Ctrl+O): files or a folder; move, copy or keep in place; a preview of
      where each item goes. Console by extension and header; `.cue`/`.gdi`/`.m3u` sets stay together; game
      folders move whole; ambiguous or clashing items are skipped with the reason. Queued, undoable.
- [x] **[P1]** Move lists the console's other ROM folders first and offers to add an outside target as a ROM
      folder (otherwise the moved roms would leave the library on the next scan).

## Phase C: Batch operations

- [x] **[P1]** Verify against DATs: hash the selection (disc images too) and show which are good dumps.
- [x] **[P1]** One folder per game: a game with its updates and DLC into a folder named after it (undoable).
- [x] **[P2]** Export selection (CSV/JSON); select the selection's updates and DLC.
- [ ] **[P2]** Edit metadata of several roms at once (publisher, developer, genres) through the override store.

## Phase D: Plugins

- [x] **[P1]** GameTDB box art (Wii, GameCube, Wii U, DS, 3DS, Switch, PS3), no account.
- [x] **[P1]** IGDB metadata (Twitch client ID and secret).
- [ ] **[P2]** RetroAchievements: whether a game has achievements. Needs RetroAchievements' own hash per
      console (headers stripped, disc track rules), not the file hash.
- [ ] **[P2]** ScreenScraper metadata and media (needs developer credentials).

## Phase E: Save data

- [x] **[P1]** Find a game's saves next to the ROM and in Citra/Azahar/Lime3DS, Yuzu-based Switch emulators,
      PPSSPP, Dolphin (Wii), RPCS3 and Vita3K folders (portable or installed); shown in the details.
- [x] **[P1]** Open the save folder; back up to a dated zip; restore from one (preview, current saves backed up
      first).
- [ ] **[P2]** Ryujinx saves (its save index needs parsing), PCSX2/DuckStation memory cards, RetroArch's
      `saves` folder.
