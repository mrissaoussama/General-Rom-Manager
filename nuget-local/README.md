# nuget-local

Local NuGet feed (see `nuget.config`). It holds copies of packages that could disappear from nuget.org,
so restore keeps working without them:

- `Ryujinx.LibHac` 0.20.0: unlisted since Ryujinx ended (2024). Unlisted packages still restore by exact
  version, but nothing guarantees they stay.

Fill it with `powershell -File tools/pin-packages.ps1` (copies the packages from the NuGet cache, verifying their
SHA-512). The `.nupkg` files are git-ignored; keep a backup of this folder.
