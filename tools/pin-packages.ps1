# Copies packages that may leave nuget.org from the NuGet cache into nuget-local/ (the fallback feed in nuget.config).
# Run after a successful restore: powershell -File tools/pin-packages.ps1 (5.1 or 7)
$ErrorActionPreference = 'Stop'
$pinned = @(@{ Id = 'Ryujinx.LibHac'; Version = '0.20.0' })

$cache = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
$feed = [IO.Path]::Combine($PSScriptRoot, '..', 'nuget-local')
foreach ($p in $pinned) {
    $id = $p.Id.ToLowerInvariant()
    $dir = [IO.Path]::Combine($cache, $id, $p.Version)
    $nupkg = Join-Path $dir "$id.$($p.Version).nupkg"
    if (-not (Test-Path $nupkg)) { throw "$($p.Id) $($p.Version) is not in the NuGet cache ($dir); run dotnet restore first." }
    $expected = (Get-Content "$nupkg.sha512" -Raw).Trim()
    $actual = [Convert]::ToBase64String([Security.Cryptography.SHA512]::Create().ComputeHash([IO.File]::ReadAllBytes($nupkg)))
    if ($actual -ne $expected) { throw "$nupkg does not match its .sha512" }
    Copy-Item $nupkg (Join-Path $feed "$id.$($p.Version).nupkg") -Force
    Write-Host "Pinned $($p.Id) $($p.Version)"
}
