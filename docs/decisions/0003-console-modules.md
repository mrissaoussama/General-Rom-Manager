# ADR 0003 — Console modules and metadata providers

- Status: proposed
- Date: 2026-10-25

## Context

All 17 consoles live in `RomManagerShared` and are found by reflection over that one assembly
(`App.RegisterConsoleServices`). Heavy dependencies (LibHac, DiscUtils, PS4_Tools, System.Drawing for
ISO2GOD) are always loaded. Third parties can only add `IPlugin` menu actions, not consoles.

Questions: should each console be its own DLL? Should title databases be plugins?

## Options

| Option | Pros | Cons |
|---|---|---|
| A. Keep one assembly | Simple build, one version | Every dependency always loaded; no third-party consoles |
| B. One DLL per console, loaded like plugins | Drop-in consoles, isolate deps | 17 projects, shared-contract versioning, collectible load contexts break shared `Rom` types and EF mapping |
| **C. `IConsoleModule` contract, scan several assemblies** | Same code path for built-in and external modules; split physically only where it pays off | Small refactor now |

## Decision

**C.**

1. Add `IConsoleModule` in Shared:
   ```csharp
   public interface IConsoleModule
   {
       ConsoleDescriptor Descriptor { get; }          // id, display name, extensions, icon
       void Register(IServiceCollection services);    // parsers, providers, manager, organizers
   }
   ```
   Built-in consoles implement it in place. `RegisterConsoleServices` iterates modules from
   `RomManagerShared` **and** any `Consoles/*.dll`, loaded into the **default** load context (module
   types must share `Rom`, EF and DI types; unloading consoles at runtime is not a goal).
2. Split into separate projects only where a dependency is heavy or legally sensitive, in this
   order: `RomManager.Consoles.Switch` (LibHac), `RomManager.Consoles.Xbox` (ISO2GOD,
   System.Drawing), `RomManager.Consoles.PlayStation` (PS4_Tools, PKG). The rest can stay together.
3. Persisted data stays console-agnostic: the library cache stores ROMs as JSON snapshots
   (`RomSnapshot`), so a module's ROM types don't need EF mappings. Title-DB tables that a module
   owns move to its own `DbContext`, or to a key/value cache table.

## Title databases and other features

- `ITitleInfoProvider<T>` already is the extension point; don't wrap it in `IPlugin`.
- Built-in providers stay built-in but become **toggleable per console in Settings** (enabled, source
  URL, update interval). Scrapers that break when a website changes (WikiBrew HTML) move to a
  separate optional module.
- Good `IPlugin` candidates (opt-in, outside core): ISO→GOD conversion, cdecrypt/xextool
  integrations, custom rename schemes, exporters (EmulationStation gamelist, LaunchBox), cover
  downloaders.
- Keep in core: parsing, scanning, the library cache, hashing/No-Intro, rename/move/dedupe.
