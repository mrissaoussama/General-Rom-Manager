# RomManager

A desktop ROM library manager for Windows, Linux and macOS (.NET 10 + Avalonia). Point it at one
folder per console and it reads each file's header or container to find the title ID, name, version,
region and icon, keeps the library in a local database, and lets you rename, move, de-duplicate and
hash files.

## Features

- **Library**: incremental scans (only new or changed files are parsed), a file watcher per ROM folder,
  progress and cancel, a list of unrecognised files with the reason each one failed.
- **Dashboard**: totals, duplicates, missing updates, unrecognised files and per-console scan status.
- **Browse**: table or cover cards, an "All consoles" view, grouping of updates/DLC under their game,
  filters (type, region, language, format, has/no update, duplicates, unnamed) and search.
- **Organise**: rename with a preview of old → new names, move with a preview, delete, hash
  (CRC32/MD5/SHA1), duplicate detection, 1G1R (keep one version per game by region priority); operations
  run in a queue with cancel and undo (undo survives a restart).
- **Identify**: No-Intro DAT matching for cartridge consoles (by hash), title databases for Switch,
  3DS and Wii U, libretro box art for DAT-matched games, embedded icons and banners.
- **Switch missing updates**: compares the library against `versions.json` from titledb.
- **Plugins**: add context-menu actions and hear about scans; each plugin declares its permissions, needs
  your approval before it runs, and only gets network, files outside its folder, processes and the
  database when it declared them. See [SamplePlugin](SamplePlugin/README.md).

## Supported formats

| Console | Formats | Notes |
|---|---|---|
| Nintendo Switch | nsp, nsz, xci | Full metadata needs your `prod.keys`/`title.keys`; a header scan works without them. XCI untested |
| Nintendo 3DS | cia, 3ds/cci, cxi | Encrypted cartridge dumps give the title ID; names come from the title DB |
| Nintendo DS | nds | Banner title and icon |
| Wii U | wux, wud, tmd, folders | Title ID/version without keys; names from the title DB |
| Wii / GameCube | wbfs, wad, iso, gcm, ciso, rvz, wia | |
| GB / GBC / GBA / NES / SNES / N64 | header formats | Names from No-Intro DATs you add |
| Mega Drive, Master System, Game Gear, PC Engine | header formats | Names from No-Intro DATs you add |
| PlayStation | cue/bin, iso, chd, pbp | Serial from SYSTEM.CNF |
| PlayStation 2 | iso, cso, zso, chd | ISO9660 and UDF |
| PSP | iso, cso, pbp, pkg | Icon from ICON0.PNG |
| PS3 / PS Vita | pkg, folders, vpk | |
| PlayStation 4 | pkg | Title, version, icon and pictures |
| Dreamcast | gdi, chd | |
| Xbox / Xbox 360 | iso, xbe, xex | Xbox 360 names and icons need a user-supplied `xextool.exe` |

## Install

Download the archive for your platform from the releases page and unpack it anywhere. It contains
one self-contained executable and `config.json`; no .NET install is needed.

To build from source you need the .NET 10 SDK:

```sh
dotnet run --project AvaloniaApplication1/AvaloniaUI.Desktop
```

## Setup

1. On first start, the setup wizard asks for a base folder and your ROM folders. You can also drop a
   folder onto the window; RomManager guesses the console from the file extensions.
2. **Switch keys** (optional, for full Switch metadata): put `prod.keys` and `title.keys` in
   `%USERPROFILE%\.switch\` (Windows) or `~/.switch/`. Settings shows whether they were found but never
   displays key values.
3. **Title databases** (optional): Settings → "Download title databases" fetches the Switch and Wii U
   title lists and the Switch `versions.json`.
4. **No-Intro DATs** (optional): copy DAT files into the folder shown on the dashboard
   (`Data/DAT` by default). They are applied on the next scan. Redump DATs for PlayStation, PS2, PSP, GameCube,
   Wii and Dreamcast match `.iso`/`.gcm` images and `.cue`/`.gdi` sets when `"Dats": { "HashDiscImages": true }` is set in
   `config.json` (off by default: hashing DVD images is slow).

Paths and tool locations live in `config.json`; your own choices are saved to `settings.json` next to
the executable and take precedence.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Design decisions](docs/decisions/)

## Legal

RomManager does not include or download games, keys or firmware. Use it only with files you own.
