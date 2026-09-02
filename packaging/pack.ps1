#Requires -Version 5.1
<#
.SYNOPSIS
    Validates the release state, builds the mod in Release, and creates the
    Nexus/GitHub and Thunderstore zips.

.PARAMETER SkipReleaseCheck
    Skip the GitHub lookup. Use only when offline.

.PARAMETER Repo
    owner/name of the GitHub repository to read the latest release from.

.EXAMPLE
    .\packaging\pack.ps1

#>
[CmdletBinding()]
param(
    [switch]$SkipReleaseCheck,
    [string]$Repo = 'BeesQ/Romestead-BetterCarts-Mod'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Failed = $false

function Write-Step { param([string]$Message)
    Write-Host ''
    Write-Host "== $Message" -ForegroundColor Cyan
}
function Write-Ok { param([string]$Message)
    Write-Host "[ OK ]  $Message" -ForegroundColor Green
}
function Write-Warn { param([string]$Message)
    Write-Host "[WARN]  $Message" -ForegroundColor Yellow
}
function Write-Fail { param([string]$Message)
    Write-Host "[FAIL]  $Message" -ForegroundColor Red
    $script:Failed = $true
}
function Wait-ForUserThenExit { param([int]$Code)
    Write-Host ''
    Write-Host 'Press Enter to close this window...' -ForegroundColor DarkGray
    $waited = $false
    try { Read-Host | Out-Null; $waited = $true } catch { }
    if (-not $waited) {
        try { & cmd.exe /c pause | Out-Null; $waited = $true } catch { }
    }
    if (-not $waited) { Start-Sleep -Seconds 30 }
    exit $Code
}

function Stop-IfFailed { param([string]$Message)
    if ($script:Failed) {
        Write-Host ''
        Write-Host "ABORTED: $Message" -ForegroundColor Red
        Write-Host 'Nothing was written' -ForegroundColor Red
        Wait-ForUserThenExit -Code 1
    }
}

trap {
    Write-Host ''
    Write-Host 'UNEXPECTED ERROR' -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    if ($_.InvocationInfo) {
        Write-Host "  at line $($_.InvocationInfo.ScriptLineNumber): $($_.InvocationInfo.Line.Trim())" `
            -ForegroundColor DarkGray
    }
    Wait-ForUserThenExit -Code 1
}

# ---------------------------------------------------------------- paths

$Root = Split-Path -Parent $PSScriptRoot

$PluginCs     = Join-Path $Root 'Core\Plugin.cs'
$Csproj       = Join-Path $Root 'BetterCarts.csproj'
$VersionJson  = Join-Path $Root 'version.json'
$ManifestJson = Join-Path $Root 'packaging\thunderstore\manifest.json'

$Dll          = Join-Path $Root 'bin\Release\BetterCarts.dll'
$Icon         = Join-Path $Root 'packaging\assets\icon.png'
$LicenseTxt   = Join-Path $Root 'LICENSE.txt'
$LicenseBare  = Join-Path $Root 'LICENSE'
$Changelog    = Join-Path $Root 'CHANGELOG.md'
$TsReadme     = Join-Path $Root 'packaging\thunderstore\README.md'

$NexusDir     = Join-Path $Root 'packaging\nexusmods'
$ThunderDir   = Join-Path $Root 'packaging\thunderstore'

Write-Host ''
Write-Host 'Better Carts - package builder' -ForegroundColor White
Write-Host "Repo root: $Root"

# ---------------------------------------------------------------- helpers

function Get-VersionFromPattern {
    param([string]$Path, [string]$Pattern, [string]$Label)
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Fail "$Label is missing: $Path"
        return $null
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $match = [regex]::Match($text, $Pattern)
    if (-not $match.Success) {
        Write-Fail "$Label has no readable version"
        return $null
    }
    return $match.Groups[1].Value
}

function Get-VersionFromJson {
    param([string]$Path, [string]$Property, [string]$Label)
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Fail "$Label is missing: $Path"
        return $null
    }
    try {
        $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        Write-Fail "$Label is not valid JSON"
        return $null
    }
    if (-not $json.PSObject.Properties.Name.Contains($Property)) {
        Write-Fail "$Label has no '$Property' field"
        return $null
    }
    return [string]$json.$Property
}

function Get-PngSize {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24) { return $null }
    $signature = @(137, 80, 78, 71, 13, 10, 26, 10)
    for ($i = 0; $i -lt 8; $i++) {
        if ($bytes[$i] -ne $signature[$i]) { return $null }
    }
    # PNG stores width and height big-endian at offsets 16 and 20
    $width  = [System.BitConverter]::ToUInt32($bytes[19..16], 0)
    $height = [System.BitConverter]::ToUInt32($bytes[23..20], 0)
    return [pscustomobject]@{ Width = $width; Height = $height }
}

function Get-ZipFileEntries {
    param([string]$Path)
    Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName -replace '\\', '/' })
    }
    finally {
        $archive.Dispose()
    }
    return @($names | Where-Object { -not $_.EndsWith('/') })
}

function New-ZipFromMap {
    param(
        [string]$Path,
        [System.Collections.Specialized.OrderedDictionary]$Entries
    )
    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue | Out-Null

    # FileMode.Create creates or overwrites the archive.
    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Create,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive(
            $stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($entryName in $Entries.Keys) {
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                    $archive,
                    $Entries[$entryName],
                    $entryName,
                    [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Test-ZipContents {
    param([string]$Path, [string[]]$Expected, [string]$Label)
    $actual = @(Get-ZipFileEntries -Path $Path | Sort-Object)
    $want   = @($Expected | Sort-Object)

    $extra   = @($actual | Where-Object { $want -notcontains $_ })
    $missing = @($want   | Where-Object { $actual -notcontains $_ })

    if ($extra.Count -eq 0 -and $missing.Count -eq 0) {
        Write-Ok "$Label contains exactly the expected files"
        return
    }
    foreach ($item in $missing) { Write-Fail "$Label is MISSING $item" }
    foreach ($item in $extra)   { Write-Fail "$Label has UNEXPECTED $item" }
}

# ---------------------------------------------------------------- 1. versions

Write-Step 'Version fields'

$versions = [ordered]@{
    'Core\Plugin.cs' = Get-VersionFromPattern -Path $PluginCs `
        -Pattern 'PluginVersion\s*=\s*"(\d+\.\d+\.\d+)"' -Label 'Core\Plugin.cs'
    'BetterCarts.csproj' = Get-VersionFromPattern -Path $Csproj `
        -Pattern '<Version>\s*(\d+\.\d+\.\d+)\s*</Version>' -Label 'BetterCarts.csproj'
    'version.json' = Get-VersionFromJson -Path $VersionJson `
        -Property 'version' -Label 'version.json'
    'packaging\thunderstore\manifest.json' = Get-VersionFromJson -Path $ManifestJson `
        -Property 'version_number' -Label 'packaging\thunderstore\manifest.json'
}

foreach ($name in $versions.Keys) {
    if ($null -ne $versions[$name]) {
        Write-Host ("        {0,-38} {1}" -f $name, $versions[$name])
    }
}

Stop-IfFailed 'a version field could not be read'

$distinct = @($versions.Values | Sort-Object -Unique)
if ($distinct.Count -ne 1) {
    Write-Fail "the four version fields DISAGREE: $($distinct -join ', ')"
}
else {
    Write-Ok "all four version fields agree on $($distinct[0])"
}

Stop-IfFailed 'the version fields must match before anything is packaged'

$Version = $distinct[0]
$ZipName = "BetterCarts-by-BeesQ-$Version.zip"

# ---------------------------------------------------------------- 2. github

Write-Step 'Latest published release'

if ($SkipReleaseCheck) {
    Write-Warn 'release check SKIPPED by request - a duplicate version will not be caught here'
}
else {
    $published = $null
    try {
        [System.Net.ServicePointManager]::SecurityProtocol = `
            [System.Net.SecurityProtocolType]::Tls12
        $headers = @{
            'User-Agent' = 'BetterCarts-pack'
            'Accept'     = 'application/vnd.github+json'
        }
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest" `
            -Headers $headers -TimeoutSec 20
        $published = ($release.tag_name -replace '^[vV]', '')
        Write-Host "        latest release: $($release.tag_name)"
    }
    catch {
        $status = ''
        try { $status = [string]$_.Exception.Response.StatusCode } catch { $status = '' }
        if ($status -eq 'NotFound' -or "$_" -match '404') {
            Write-Warn 'the repository has no published releases yet - nothing to compare against'
        }
        else {
            Write-Fail "could not read the latest release from GitHub: $($_.Exception.Message)"
            Write-Host '        re-run with -SkipReleaseCheck if you are working offline' `
                -ForegroundColor Yellow
        }
    }

    if ($null -ne $published) {
        $local = [version]$Version
        $live  = [version]$published
        if ($local -eq $live) {
            Write-Fail "$Version is ALREADY PUBLISHED - bump the four version fields first"
        }
        elseif ($local -lt $live) {
            Write-Fail "$Version is OLDER than the published $published"
        }
        else {
            Write-Ok "$Version is newer than the published $published"
        }
    }
}

Stop-IfFailed 'the release check did not pass'

# ---------------------------------------------------------------- 3. package inputs

Write-Step 'Package contents'

if (-not (Test-Path -LiteralPath $Icon)) {
    Write-Fail 'packaging\assets\icon.png is missing'
}
else {
    $size = Get-PngSize -Path $Icon
    if ($null -eq $size) {
        Write-Fail 'packaging\assets\icon.png is not a readable PNG'
    }
    elseif ($size.Width -ne 256 -or $size.Height -ne 256) {
        Write-Fail "icon.png is $($size.Width)x$($size.Height) - Thunderstore requires exactly 256x256"
    }
    else {
        Write-Ok 'icon.png is 256x256'
    }
}

if (-not (Test-Path -LiteralPath $LicenseTxt)) {
    Write-Fail 'LICENSE.txt is missing from the repo root'
}
else {
    Write-Ok 'LICENSE.txt is present'
}

if (Test-Path -LiteralPath $LicenseBare) {
    Write-Fail 'an extensionless LICENSE sits beside LICENSE.txt - delete it, GitHub can latch onto the wrong one'
}

foreach ($required in @($TsReadme, $Changelog)) {
    if (-not (Test-Path -LiteralPath $required)) {
        Write-Fail "$required is missing"
    }
}

Stop-IfFailed 'a file that goes inside the packages is missing or wrong'

$firstHeading = (Get-Content -LiteralPath $Changelog |
    Where-Object { $_.Trim() -ne '' } |
    Select-Object -First 1).Trim()
if ($firstHeading -ne "## $Version") {
    Write-Warn "CHANGELOG.md opens with '$firstHeading' rather than '## $Version'"
}
else {
    Write-Ok "CHANGELOG.md opens with the $Version entry"
}

# ---------------------------------------------------------------- 4. release build

Write-Step 'Building Release'
Push-Location $Root
try {
    & dotnet build $Csproj --configuration Release --no-incremental | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "dotnet build exited with $LASTEXITCODE"
    }
    else {
        Write-Ok 'Release build succeeded'
    }
}
finally {
    Pop-Location
}
Stop-IfFailed 'the Release build failed'

# ---------------------------------------------------------------- 5. the DLL

Write-Step 'Built DLL'

if (-not (Test-Path -LiteralPath $Dll)) {
    Write-Fail 'the Release build did not produce bin\Release\BetterCarts.dll'
}
Stop-IfFailed 'there is no Release DLL to package'

$dllItem = Get-Item -LiteralPath $Dll
$dllTime = $dllItem.LastWriteTimeUtc
Write-Host ("        built {0}" -f $dllItem.LastWriteTime)

$versionFilePaths = [ordered]@{
    'Core\Plugin.cs'                       = $PluginCs
    'BetterCarts.csproj'                   = $Csproj
    'version.json'                         = $VersionJson
    'packaging\thunderstore\manifest.json' = $ManifestJson
}

$stale = $false
foreach ($name in $versionFilePaths.Keys) {
    $fileTime = (Get-Item -LiteralPath $versionFilePaths[$name]).LastWriteTimeUtc
    if ($dllTime -le $fileTime) {
        Write-Fail "$name was edited AFTER the DLL was built"
        $stale = $true
    }
}
if (-not $stale) {
    Write-Ok 'the Release DLL is newer than every version file'
}

$productVersion = ''
try { $productVersion = [string]$dllItem.VersionInfo.ProductVersion } catch { $productVersion = '' }
if ([string]::IsNullOrWhiteSpace($productVersion)) {
    try { $productVersion = [string]$dllItem.VersionInfo.FileVersion } catch { $productVersion = '' }
}
$dllMatch = [regex]::Match($productVersion, '^(\d+\.\d+\.\d+)')
if (-not $dllMatch.Success) {
    Write-Warn "the DLL reports no readable version ('$productVersion') - timestamp check only"
}
elseif ($dllMatch.Groups[1].Value -ne $Version) {
    Write-Fail "the Release DLL was compiled as $($dllMatch.Groups[1].Value), not $Version"
}
else {
    Write-Ok "the Release DLL is compiled as $Version"
}

Stop-IfFailed 'the built Release DLL does not match the version fields'

# ---------------------------------------------------------------- 6. zips

Write-Step "Writing $ZipName"

$nexusZip   = Join-Path $NexusDir   $ZipName
$thunderZip = Join-Path $ThunderDir $ZipName

foreach ($dir in @($NexusDir, $ThunderDir)) {
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
}
foreach ($existing in @($nexusZip, $thunderZip)) {
    if (Test-Path -LiteralPath $existing) {
        Write-Warn "overwriting $(Split-Path -Leaf $existing) in $(Split-Path -Leaf (Split-Path -Parent $existing))"
    }
}

# Nexus and GitHub use the BepInEx folder structure inside the archive.
$nexusEntries = [ordered]@{
    'BepInEx/plugins/BetterCarts/BetterCarts.dll' = $Dll
    'BepInEx/plugins/BetterCarts/icon.png'        = $Icon
    'BepInEx/plugins/BetterCarts/LICENSE.txt'     = $LicenseTxt
}
New-ZipFromMap -Path $nexusZip -Entries $nexusEntries
Write-Ok "packaging\nexusmods\$ZipName"

# Thunderstore requires flat archive contents; explicit entries prevent self-inclusion.
$thunderEntries = [ordered]@{
    'manifest.json'   = $ManifestJson
    'README.md'       = $TsReadme
    'CHANGELOG.md'    = $Changelog
    'icon.png'        = $Icon
    'BetterCarts.dll' = $Dll
    'LICENSE.txt'     = $LicenseTxt
}
New-ZipFromMap -Path $thunderZip -Entries $thunderEntries
Write-Ok "packaging\thunderstore\$ZipName"

# ---------------------------------------------------------------- 7. verify

Write-Step 'Verifying the archives'

Test-ZipContents -Path $nexusZip   -Label 'nexus zip'        -Expected @($nexusEntries.Keys)
Test-ZipContents -Path $thunderZip -Label 'thunderstore zip' -Expected @($thunderEntries.Keys)

Stop-IfFailed 'an archive does not contain what it should - do NOT upload it'

Write-Host ''
Write-Host "Ready to publish $Version" -ForegroundColor Green
Write-Host "  packaging\nexusmods\$ZipName      (Nexus + GitHub Release)"
Write-Host "  packaging\thunderstore\$ZipName   (Thunderstore)"
Wait-ForUserThenExit -Code 0
