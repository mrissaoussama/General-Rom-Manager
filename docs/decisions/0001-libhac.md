# ADR 0001 — Switch container parsing: keep LibHac (C#), switch to the `Ryujinx.LibHac` package

- Status: accepted (vendored tree kept at git tag `libhac-vendored`)
- Date: 2026-10-25

## Context

Switch NSP/XCI parsing uses a vendored copy of LibHac 0.20.0 (`LibHac/`, ~770 files).

- `github.com/Thealexbarney/LibHac` no longer exists (`git ls-remote` → "Repository not found").
- The last upstream NuGet `LibHac` release is 0.19.0 (Oct 2023).
- The Ryubing (Ryujinx fork) project publishes **`Ryujinx.LibHac` 0.20.0** on nuget.org (built for
  net8.0; source repo `git.ryujinx.app/ryubing/libhac`, which was unreachable from here at the time of
  writing).
- Our local edits to the vendored copy (8 files) only fix C# 14 "first-class span" overload
  ambiguities (`MemoryMarshal.Cast(...)` → explicit `.AsSpan()` / casts) and retarget to net10.0.
  The package's compiled net8.0 IL needs none of them and runs on net10.
- The real bug seen in testing ("Missing NCA title key" on every titlekey-encrypted NSP) is **ours**,
  not LibHac's: we never import the `.tik` tickets inside the NSP into the `KeySet`.

## Options

| Option | Pros | Cons |
|---|---|---|
| **A. `Ryujinx.LibHac` NuGet** | Drops ~770 vendored files; same API/version we already use; maintained by an active emulator project | Depends on a third party feed staying on nuget.org; net8 build (fine on net10) |
| B. Keep vendored copy | Zero external risk; we can patch freely | We own ~770 files of crypto/FS code, must hand-fix every new compiler break; no upstream fixes |
| C. Shell out to `hactool` / `nstool` and parse stdout | Small binaries, well-known tools | Process per file (slow on 1k+ libs), text-output parsing is brittle, per-OS binaries to ship, hactool is unmaintained, no icon/NACP-language access without extracting to disk. Contradicts the plan to *remove* external tools (Python, cdecrypt) |
| D. Use Ryujinx/Ryubing emulator code | — | Emulator code is LibHac underneath; much bigger dependency for no gain |

## Decision

**A**, with **B as the fallback**: reference `Ryujinx.LibHac` 0.20.0, delete `LibHac/` from the solution,
keep the vendored tree on a git tag (`libhac-vendored`) so we can restore it if the package disappears.
Do not use hactool/nstool binaries. If ever needed, nstool (MIT, maintained) is the better CLI fallback
for a diagnostic "verify with external tool" action only — not for scanning.

## Follow-ups

1. Import tickets before opening NCAs (fixes all "Missing NCA title key" failures):
   ```csharp
   foreach (var entry in pfs.EnumerateEntries("/", "*.tik"))
   {
       using var tikFile = new UniqueRef<IFile>();
       pfs.OpenFile(ref tikFile.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
       var ticket = new Ticket(tikFile.Get.AsStream());
       var rightsId = new RightsId(ticket.RightsId);
       var accessKey = new AccessKey(ticket.GetTitleKey(keySet));
       keySet.ExternalKeySet.Add(rightsId, accessKey).ThrowIfFailure();
   }
   ```
   (same approach as hactoolnet's `ImportTickets`).
2. NSZ/XCZ: LibHac cannot read zstd-compressed NCZ sections. Short term keep the header-scan parser
   (`SwitchRomNSPNSZParser`) for IDs/versions; medium term read NCZ section headers + `ZstdSharp` to
   decompress only the control NCA (NACP + icon).
3. Add a fixture test with a tiny homebrew NSP so a future LibHac swap is caught by CI.
