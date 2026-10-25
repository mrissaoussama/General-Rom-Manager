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
  terminal chokes. Tracked in docs/ROADMAP.md phase 2.
- Find test files fast with Everything: `es.exe -n 5 ext:nsp !$RECYCLE`.
