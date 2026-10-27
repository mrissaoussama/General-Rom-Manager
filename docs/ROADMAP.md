# Roadmap v4

Written 2026-10-28 after a full review of the code, docs, dependencies and CI, with [v3](ROADMAP-v3.md) finished
(earlier: [v2](ROADMAP-v2.md), [v1](ROADMAP-v1.md)). Baseline: the solution builds with zero warnings and
325 of 326 tests pass (one is Windows-recycle-bin only).

Legend: **[P0]** data safety or correctness, **[P1]** high value, **[P2]** nice to have.

---

## Phase A: Dependencies, build and CI

Every direct NuGet package is current and none is vulnerable or deprecated (`dotnet list package
--outdated/--vulnerable/--deprecated`). Transitive SkiaSharp 4 and SQLitePCLRaw 3 are new majors pinned by
Avalonia and EF Core; leave them to those packages.

- [ ] **[P1]** GitHub Actions on Node 20 (`checkout@v4`, `setup-dotnet@v4`, `upload-artifact@v4`) move to the
      current majors; CI cancels runs superseded by a newer push.
- [ ] **[P1]** `HashUtils` carries a copy of a third-party CRC32 class although `System.IO.Hashing` (already a
      dependency) has one. Use the package.
- [ ] **[P2]** About a dozen files have several statements merged on one line (`byte[] x;        using (...)`),
      left over from an old mass edit. Split them.
- [ ] **[P2]** WiiU imports `LibHac.Tools.Fs` without using it; drop the import (one less module tied to LibHac).
- [ ] **[P1]** Culture analyzers: `.editorconfig` turns CA1304/CA1311 (culture-sensitive `ToLower`/`ToUpper`)
      into errors outside vendored code, so the bugs in C1 cannot come back.

## Phase B: Architecture

- [ ] **[P1]** `IMissingUpdateReporter` is async-first. Today the sync method is the contract and PS4 implements
      it by blocking on network calls (`GetAwaiter().GetResult()`, one request per game).
- [ ] **[P1]** One hashing path: `HashUtils` computes every requested algorithm in a single read (it read the
      file once per algorithm: three passes over a 4 GB ISO), takes a cancellation token and progress, hashes
      folder roms recursively, and does not append duplicates to `Rom.Hashes`. `RomOperationService` and
      `UniversalRomActions` both use it instead of two copies of the same loop.
- [ ] **[P2]** A console's id comes from one place (`ConsoleModules.IdOf`) instead of three
      `Name.Replace("Console", "")` copies.
- [ ] **[P1]** Scanner pipeline: files were parsed in fixed chunks of `MaxParallelism` and each chunk waited for
      its slowest file (one 40 GB PS4 pkg held up three small files). Parse with a bounded worker pool feeding
      the sequential enrich/save step.
- [ ] **[P2]** `RomParserExecutor.GetSupportedExtensions` returns each extension once.

## Phase C: Correctness and data safety

- [ ] **[P0]** Culture bugs. `ReleaseDate.ToString("yyyy-MM-dd")` uses the current calendar, so with an Arabic
      (Hijri), Thai or Japanese-era locale renamed files get the wrong year. `ToLower()` on extensions breaks with
      Turkish casing (`"ISO"` → `"ıso"`, so no parser matches). Use the invariant culture.
- [ ] **[P0]** Rename: a rom with no title and the template `{TitleName}` was renamed to `.nsp` (a hidden file on
      Linux/macOS). Empty names are refused. Names are made portable for the SD cards and shares ROMs end up on:
      characters invalid on Windows/FAT are replaced on every OS, trailing dots and spaces are trimmed,
      reserved device names (`CON`, `NUL`, `COM1`…) are escaped, and names are capped at 255 characters.
- [ ] **[P0]** Conversion tools got their arguments as one string with paths pasted in; a file name with a quote
      (legal on Linux/macOS) could inject arguments. Arguments are tokenised once and passed through
      `ProcessStartInfo.ArgumentList`.
- [ ] **[P1]** Move: "already there" compared paths ignoring case on every OS, and a target folder with the
      file's name was not treated as a collision.
- [ ] **[P1]** Console-specific grid columns were found by type-name prefix, so the Game Boy page took Game Boy
      Advance rom types and Wii took Wii U ones. Match on the console's module assembly too.
- [ ] **[P1]** A failed database migration was logged and ignored; every later query then failed. The library
      database is a cache: move the broken file aside (`*.broken-<time>`), create a new one and tell the user.
- [ ] **[P1]** Rename/move that partly fail ended "Completed" with no sign of the failures. The operation keeps
      a warning ("3 of 10 failed: …") that the Operations panel shows.
- [ ] **[P2]** Plugin loading errors went to `Console.WriteLine` (invisible in a GUI app); log them.
- [ ] **[P2]** `OperationQueue` disposes each operation's `CancellationTokenSource` when it finishes.

## Phase D: UI and UX

- [ ] **[P1]** Operations panel shows the warning of an operation that finished with errors (C).
- [ ] **[P1]** Rename preview marks names that will be refused (empty) instead of letting them through.
- [ ] **[P2]** The shell's initial "Ready" status comes from `Strings.resx`.

## Phase E: Features

- [ ] **[P1]** Tool pinning (ADR 0002 todo): the SHA-256 of each user-supplied conversion tool is recorded on
      first use; a changed binary needs confirmation before it runs again.
- [ ] **[P2]** Rename placeholders `{Year}` and `{Languages}`.

## Phase F: Plugins

- [ ] **[P2]** Plugins that fail to load are listed in Settings → Plugins with the reason (today only the log
      says why a plugin is missing).

## Phase G: Testing and release

- [ ] Tests for every fix above (rename sanitising, culture, move collisions, single-pass hashing, column type
      matching, DB recovery, argument tokenising, tool pinning, scanner ordering with uneven parse times).

## Not in this round (blocked or needs a decision)

- Linux and macOS CI: runs on the next push to GitHub (not verifiable locally on Windows).
- Code signing certificates (Windows Authenticode, Apple Developer ID); the release workflow already uses them
  when the secrets exist.
- XCI parsing test: needs a real XCI.
- Coverage gate in CI: once Linux/macOS are green.
- Module-private dependencies (LibHac only in the Switch module, SkyEditor in 3DS, DiscUtils in PS2/PSP,
  HtmlAgilityPack in Wii U). Modules would ship their own dependency DLLs next to them and the loader would
  resolve them; this changes the published layout and single-file publishing, so it gets its own ADR first.
