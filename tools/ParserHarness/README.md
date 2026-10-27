# ParserHarness

Smoke-tests the real parser pipeline (same DI wiring as the app via `App.RegisterConsoleServices`)
against files on disk. Not part of the solution build.

```sh
dotnet run --project tools/ParserHarness -- PS4Console "Y:\roms\PS4" 5
cp tools/ParserHarness/paths.example.json tools/ParserHarness/paths.json   # edit, git-ignored
dotnet run --project tools/ParserHarness -- all tools/ParserHarness/paths.json 5
```

- Uses `config.json` copied from the AvaloniaUI project (error log path, cache, title DBs).
- "0 rom(s)" usually means a parser threw and swallowed it — check the error log path printed at the end.
- Some parsers still write to stdout (binary dumps); pipe through `tr -cd '\11\12\15\40-\176'` if the
  terminal chokes. Tracked in docs/ROADMAP-v1.md phase 2.
- Find test files fast with Everything: `es.exe -n 5 ext:nsp !$RECYCLE`.

## Regression snapshots

Real ROMs can't be committed, so parser regressions are caught locally:

```sh
# before a parser change
dotnet run --project tools/ParserHarness -- all tools/ParserHarness/paths.json 20 --snapshot tools/ParserHarness/baseline.snapshot.json
# after the change: lists NEW / CHANGED / MISSING files, exit code 2 on any difference
dotnet run --project tools/ParserHarness -- all tools/ParserHarness/paths.json 20 --compare tools/ParserHarness/baseline.snapshot.json
```

Review each CHANGED entry; if the new output is correct, write a new baseline and bump
`LibraryScanner.ScanVersion` so cached libraries get re-parsed.

## JSON output

`--json` prints one JSON document on stdout (progress goes to stderr): for each file its console, time,
the roms exactly as the library stores them, and the "not recognised" reason when it parsed to nothing.

```sh
dotnet run --project tools/ParserHarness -- PS4Console "Y:\roms\PS4" 50 --json > ps4.json
```

Synthetic fixtures have golden files instead: `RomManagerTests/Golden/*.json` (see `Golden.Verify`); after an
intended parser change run the tests with `UPDATE_GOLDEN=1` and review the diff.
