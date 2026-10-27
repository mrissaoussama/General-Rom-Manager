# ADR 0002 — External tools (cdecrypt, xextool, Python)

- Status: accepted
- Date: 2026-10-25

## Context

Some formats used third-party programs: `cdecrypt.exe` (Wii U), `xextool.exe` (Xbox 360), a Python
script (PS3/PSP/Vita PKG). Problems: binaries committed to the repo can't be audited, they write next
to the ROMs, they are Windows-only, they need a runtime (Python + pip), and a scan launches one
process per file.

What each tool was really needed for:

| Tool | Used for | Needs a key/decryption? |
|---|---|---|
| Python PKG script | PKG header, item table, PARAM.SFO | Public content keys + AES-CTR (simple) |
| cdecrypt | Title ID/version, names, icons | ID/version: **no** (TMD is plain). Names/icons: yes (common key + AES-CBC) |
| xextool | Title ID, media ID, name, icon | ID: **no** (XEX header is plain). Name/icon: yes (AES + LZX decompress) |

## Decision

1. **Scanning never depends on an external program.** Everything needed to identify a title is read
   in C#: PKG (`PsnPkgReader`), TMD and WUD/WUX product code (`WiiUFormats`), XEX header (ISO2GOD
   `XexInfo`). Names come from title databases (WikiBrew, titledb, …), not from decrypting content.
2. **No binaries in the repo.** `cdecrypt.exe` was removed. Tools are user-supplied, opt-in, and
   only add extras (icons, localized names).
3. **Tool paths** default to `<BaseDirectory>/Tools/<tool>` (the app data folder, which is writable;
   the install folder may not be) and can be repointed in `config.json`
   (`WiiU:CdecryptPath`, `Xbox360:XexToolPath`) or to an absolute path. Each tool is surfaced as an
   optional `ConsoleRequirement` so the UI shows whether it is present.
4. **Tools never write next to ROMs.** They run with a working directory under
   `Cache/Temp/<guid>` that is deleted afterwards. (cdecrypt still extracts beside the TMD by design;
   it stays opt-in via `WiiU:AllowCdecrypt` and only folders it created are removed.)
5. **Python is acceptable for optional plugins**, not for core parsing (extra runtime, slower, and a
   readable script is still untrusted code).

## Replacing the remaining tool uses (later, optional)

- Wii U names/icons in C#: decrypt the title key from `title.tik` with the user's Wii U common key
  (key file, like `prod.keys`), AES-CBC decrypt content 0 (FST), locate `meta/meta.xml` and
  `meta/iconTex.tga`, decrypt only those blocks. ~300 lines; references: cdecrypt (C), Cemu
  `src/Cafe/TitleList` + `Common/FileStream`, JNUSLib (Java). Disc images (WUD/WUX) additionally need
  the per-disc key.
- Xbox 360 names/icons in C#: decrypt the PE base file (retail XEX key) and LZX-decompress it to reach
  the XDBF resource. References: xenia `src/xenia/cpu/xex_module.cc`, `xex2` docs on free60. Larger
  effort; a Title ID → name database is the cheaper route.

## Pinning (done 2026-10-28)

Conversion tools are pinned to their SHA-256 on first use (`ToolPins`, `tool-pins.json` next to the database); a
program that changed since asks for confirmation before it runs. Their arguments are passed as a list
(`ProcessStartInfo.ArgumentList`), never as one string with paths pasted in.
