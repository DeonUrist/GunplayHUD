param(
    [string]$GameDir = $env:APOCALYPTER_GAME_DIR,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../_releases')
)

$ErrorActionPreference = 'Stop'
if (-not $GameDir) { throw 'Set GameDir or APOCALYPTER_GAME_DIR to the installed game folder.' }

$pluginSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/Plugin.cs') -Raw
$versionMatch = [regex]::Match($pluginSource, 'VERSION\s*=\s*"(\d+\.\d+\.\d+)"')
if (-not $versionMatch.Success) { throw 'Could not read plugin version.' }
$version = $versionMatch.Groups[1].Value
$assets = @('Textures/crosshair_lines.png', 'Textures/crosshair_dot.png', 'Sounds/headshot.mp3')
foreach ($asset in $assets) {
    $source = Join-Path $PSScriptRoot $asset
    if (-not (Test-Path -LiteralPath $source -PathType Leaf) -or (Get-Item -LiteralPath $source).Length -eq 0) {
        throw "Missing or empty release asset: $asset"
    }
}

& dotnet build (Join-Path $PSScriptRoot 'GunplayHUD.csproj') -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
foreach ($asset in $assets) {
    $sourceHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $asset)).Hash
    $outputHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot "bin/Release/$asset")).Hash
    if ($sourceHash -ne $outputHash) { throw "Build asset differs from source: $asset" }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$zipPath = Join-Path (Resolve-Path -LiteralPath $OutputDirectory).Path "GunplayHUD-$version.zip"
if (Test-Path -LiteralPath $zipPath) { throw "Release ZIP already exists: $zipPath" }
$files = [ordered]@{
    'GunplayHUD/GunplayHUD.dll' = (Join-Path $PSScriptRoot 'bin/Release/GunplayHUD.dll')
    'GunplayHUD/README.md' = (Join-Path $PSScriptRoot 'README.md')
}
foreach ($asset in $assets) { $files["GunplayHUD/$asset"] = Join-Path $PSScriptRoot $asset }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $files.GetEnumerator()) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose() }

$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($zip.Entries.Count -ne $files.Count) { throw 'Unexpected release ZIP contents.' }
    foreach ($entry in $files.GetEnumerator()) {
        $archived = $zip.GetEntry($entry.Key)
        if ($null -eq $archived) { throw "Missing ZIP entry: $($entry.Key)" }
        $stream = $archived.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
        finally { $sha.Dispose(); $stream.Dispose() }
        if ($hash -ne (Get-FileHash -LiteralPath $entry.Value).Hash) { throw "ZIP entry differs from source: $($entry.Key)" }
    }
} finally { $zip.Dispose() }

Write-Output "Verified release ZIP: $zipPath"
