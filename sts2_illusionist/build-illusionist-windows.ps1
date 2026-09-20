param(
    [string]$Sts2Root = "D:\Program Files\Steam\steamapps\common\Slay the Spire 2",
    [string]$GodotBin,
    [string]$DotnetBin = "dotnet",
    [string]$RitsuLibVersion,
    [switch]$NoRitsuLibUpdate,
    [switch]$SkipInstallCopy,
    [switch]$Help
)

if ($Help) {
    Write-Host @"
Usage: build-illusionist-windows.ps1 [OPTIONS]

Builds the Illusionist mod (C# dll + Godot .pck) and installs it to <Sts2Root>\mods\illusionist.

Before building, it upgrades the RitsuLib dependency to the newest release on nuget.org and keeps
illusionist.csproj, mod_manifest.json and the STS2-RitsuLib source checkout on that same version.

Options:
  -Sts2Root <path>       Slay the Spire 2 install dir (default: D:\Program Files\Steam\steamapps\common\Slay the Spire 2)
  -GodotBin <path>       Godot 4.5.1 exe (default: searches repo root + PATH for Godot_v4.5.1-stable_win64*.exe)
  -DotnetBin <path>      dotnet executable (default: dotnet)
  -RitsuLibVersion <ver> Build against this exact RitsuLib version instead of the newest one
  -NoRitsuLibUpdate      Keep the version already pinned in illusionist.csproj (offline / reproducible builds)
  -SkipInstallCopy       Build only; do not copy into the game's mods dir
  -Help                  Show this help
"@
    exit 0
}

$ErrorActionPreference = "Stop"

$ProjectDir   = $PSScriptRoot
$RepoRoot     = Split-Path $ProjectDir -Parent
$AssemblyName = "illusionist"
$ModId        = "illusionist"
$ProjectFile  = Join-Path $ProjectDir "$AssemblyName.csproj"
$PckSource    = Join-Path $ProjectDir "build\$AssemblyName.pck"
$DllSource    = Join-Path $ProjectDir ".godot\mono\temp\bin\Debug\$AssemblyName.dll"
$ManifestSrc  = Join-Path $ProjectDir "mod_manifest.json"

# Sandboxed dirs so headless Godot can write logs inside the repo.
$GodotAppData      = Join-Path $ProjectDir ".build_output\appdata"
$GodotLocalAppData = Join-Path $ProjectDir ".build_output\localappdata"
$GodotTemp         = Join-Path $ProjectDir ".build_output\tmp"

function Write-Info([string]$m) { Write-Host "[illusionist] $m" -ForegroundColor Cyan }

# --- Resolve game dir ---
if (-not (Test-Path (Join-Path $Sts2Root "data_sts2_windows_x86_64\sts2.dll"))) {
    Write-Error "sts2.dll not found under -Sts2Root '$Sts2Root'. Pass the correct -Sts2Root."
    exit 1
}

# --- Resolve Godot ---
function Resolve-Godot([string]$candidate) {
    $cands = @(
        $candidate,
        $env:GODOT_BIN,
        (Join-Path $RepoRoot "Godot_v4.5.1-stable_win64_console.exe"),
        (Join-Path $RepoRoot "Godot_v4.5.1-stable_win64.exe"),
        "Godot_v4.5.1-stable_win64_console.exe",
        "Godot_v4.5.1-stable_win64.exe"
    )
    foreach ($c in $cands) {
        if (-not $c) { continue }
        if (Test-Path $c) { return (Resolve-Path $c).Path }
        $cmd = Get-Command $c -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
    }
    Write-Error "Could not find Godot 4.5.1. Pass -GodotBin or set GODOT_BIN."
    exit 1
}
$GodotExe = Resolve-Godot $GodotBin

$verText = (& $GodotExe --version | Select-Object -First 1).Trim()
if (-not $verText.StartsWith("4.5.1")) {
    Write-Error "Godot must be 4.5.1 (found: $verText). STS2 cannot load .pck from 4.6.x."
    exit 1
}

Write-Info "Game dir: $Sts2Root"
Write-Info "Godot:    $GodotExe ($verText)"

New-Item -ItemType Directory -Force -Path $GodotAppData, $GodotLocalAppData, $GodotTemp | Out-Null

# --- Phase 0: keep the RitsuLib dependency current ---------------------------------------------
# The mod compiles against the STS2.RitsuLib NuGet package and ships a manifest dependency on the
# RitsuLib mod. Both used to be hand-pinned and silently rotted (and the package's own deploy target
# then downgraded the RitsuLib installed in the game). This phase resolves one version and writes it
# to every place that records it, so the compiled DLL, the manifest floor, the copy deployed into
# the game, and the source checkout we read the API from can never disagree.

$RitsuLibPkgId  = "STS2.RitsuLib"
$RitsuLibSrcDir = Join-Path $RepoRoot "STS2-RitsuLib"
$RitsuLibVerRe  = '(?<pre><RitsuLibVersion\s+Condition="[^"]*">)(?<ver>[^<]+)(?<post></RitsuLibVersion>)'
$ManifestDepRe  = '(?<pre>"id"\s*:\s*"STS2-RitsuLib"\s*,\s*"min_version"\s*:\s*")(?<ver>[^"]+)(?<post>")'
$MinGameRe      = '(?<pre>"min_game_version"\s*:\s*")(?<ver>[^"]+)(?<post>")'
$Sts2AppId      = "2868840"
$RitsuLibWsId   = "3747602295"   # Steam Workshop item players install RitsuLib from

function Write-Warn([string]$m) { Write-Host "[illusionist] $m" -ForegroundColor Yellow }

# Rewrites the single captured version inside a file, preserving its formatting and encoding.
function Set-VersionInFile([string]$path, [string]$pattern, [string]$version) {
    $text = [System.IO.File]::ReadAllText($path)
    $m = [regex]::Match($text, $pattern)
    if (-not $m.Success) { Write-Error "Could not find the RitsuLib version marker in $path."; exit 1 }
    if ($m.Groups['ver'].Value -eq $version) { return $false }
    $updated = [regex]::Replace($text, $pattern, "`${pre}$version`${post}")
    [System.IO.File]::WriteAllText($path, $updated, (New-Object System.Text.UTF8Encoding($false)))
    return $true
}

function Get-VersionInFile([string]$path, [string]$pattern) {
    $m = [regex]::Match([System.IO.File]::ReadAllText($path), $pattern)
    if (-not $m.Success) { Write-Error "Could not find the RitsuLib version marker in $path."; exit 1 }
    return $m.Groups['ver'].Value
}

# Newest stable release on nuget.org, or $null when offline / the feed is unreachable.
function Get-LatestRitsuLibVersion {
    $url = "https://api.nuget.org/v3-flatcontainer/$($RitsuLibPkgId.ToLowerInvariant())/index.json"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $json = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 20).Content | ConvertFrom-Json
    } catch {
        Write-Warn "nuget.org unreachable ($($_.Exception.Message.Trim())); keeping the pinned version."
        return $null
    }
    $stable = @($json.versions | Where-Object { $_ -notmatch '-' })
    if ($stable.Count -eq 0) { Write-Warn "No stable $RitsuLibPkgId releases found; keeping the pinned version."; return $null }
    return ($stable | Sort-Object { [version]$_ } | Select-Object -Last 1)
}

function Get-NuGetPackageDir([string]$version) {
    foreach ($root in @($env:NUGET_PACKAGES, (Join-Path $env:USERPROFILE ".nuget\packages"))) {
        if (-not $root) { continue }
        $dir = Join-Path $root "$($RitsuLibPkgId.ToLowerInvariant())\$version"
        if (Test-Path $dir) { return $dir }
    }
    return $null
}

# RitsuLib 0.6+ compiles one game-API variant per NuGet package and records which one in
# compat-target.txt. (The Workshop runtime ships several variants and picks per player; the package
# does not.) Pre-0.6 packages have no such file, so the gate below simply doesn't apply to them.
function Get-RitsuLibCompatTarget([string]$version) {
    $dir = Get-NuGetPackageDir $version
    if (-not $dir) { return $null }
    $file = Join-Path $dir "contentFiles\any\any\compat-target.txt"
    if (-not (Test-Path $file)) { return $null }
    return ([System.IO.File]::ReadAllText($file)).Trim([char]0xFEFF, ' ', "`r", "`n", "`t")
}

function Get-InstalledGameVersion {
    $file = Join-Path $Sts2Root "release_info.json"
    if (-not (Test-Path $file)) { return $null }
    try { return (((Get-Content $file -Raw | ConvertFrom-Json).version) -replace '^v', '') } catch { return $null }
}

# The RitsuLib players actually get. Our manifest floor must stay at or below it, or the mod refuses
# to load for everyone until the Workshop item catches up with nuget.org.
function Get-WorkshopRitsuLibVersion {
    $steamApps = Split-Path (Split-Path $Sts2Root -Parent) -Parent
    $file = Join-Path $steamApps "workshop\content\$Sts2AppId\$RitsuLibWsId\mod_manifest.json"
    if (-not (Test-Path $file)) { return $null }
    try { return (Get-Content $file -Raw | ConvertFrom-Json).version } catch { return $null }
}

# Refuse to build against a RitsuLib that targets a newer game API than this machine runs: the
# resulting DLL would reference an API the installed game (and players on it) do not have.
function Test-RitsuLibCompatible([string]$version) {
    $compat = Get-RitsuLibCompatTarget $version
    if (-not $compat) { return $true }
    $game    = Get-InstalledGameVersion
    $minGame = Get-VersionInFile $ManifestSrc $MinGameRe
    Write-Info "RitsuLib targets game API $compat (installed game $game, manifest floor $minGame)"

    if ($game -and [version]$compat -gt [version]$game) {
        Write-Warn "RitsuLib $version targets game API $compat, but this machine runs $game."
        Write-Warn "That package tracks the game's beta branch; building against it would ship a mod"
        Write-Warn "the installed game cannot load. Update the game, or pass -RitsuLibVersion <older>"
        Write-Warn "or -NoRitsuLibUpdate to stay on the current pin."
        return $false
    }
    if ($game -and [version]$compat -lt [version]$game) {
        Write-Warn "RitsuLib $version targets game API $compat but the game is $game; RitsuLib has not caught up yet."
    }
    if ($minGame -and $compat -ne $minGame) {
        Write-Warn "mod_manifest.json min_game_version is $minGame but RitsuLib targets $compat."
        Write-Warn "Players between those two versions may fail to load the mod; bump min_game_version deliberately."
    }
    return $true
}

# Move the local RitsuLib clone onto the tag matching the package we build against, so the source we
# read APIs from matches the DLL. Never destructive: any local work or network failure aborts it.
function Test-RitsuLibTag([string]$tag) {
    & git -C $RitsuLibSrcDir rev-parse --verify --quiet "refs/tags/$tag^{commit}" | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Sync-RitsuLibSource([string]$version) {
    $tag = "v$version"
    if (-not (Test-Path (Join-Path $RitsuLibSrcDir ".git"))) {
        Write-Warn "RitsuLib source checkout not found at $RitsuLibSrcDir; skipping source sync."
        return
    }
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Warn "git not on PATH; skipping RitsuLib source sync."
        return
    }
    # git writes progress to stderr, which Windows PowerShell turns into a terminating
    # NativeCommandError under -ErrorAction Stop. Downgrade locally (the assignment is function-scoped
    # and unwinds on return) so the $LASTEXITCODE checks below actually get to run.
    $ErrorActionPreference = "Continue"
    $prevPrompt = $env:GIT_TERMINAL_PROMPT
    $env:GIT_TERMINAL_PROMPT = "0"   # fail fast instead of hanging on a credential prompt
    try {
        if (& git -C $RitsuLibSrcDir status --porcelain) {
            Write-Warn "RitsuLib source checkout has local changes; leaving it as-is."
            return
        }
        $head = @(& git -C $RitsuLibSrcDir tag --points-at HEAD)
        if ($head -contains $tag) { Write-Info "RitsuLib source: already at $tag"; return }

        # Only hit the network when the tag isn't already local, so a blocked/offline github.com
        # still lets us move between tags that were fetched earlier.
        if (-not (Test-RitsuLibTag $tag)) {
            & git -C $RitsuLibSrcDir fetch --quiet --tags origin
            if ($LASTEXITCODE -ne 0) { Write-Warn "git fetch failed; leaving the RitsuLib source checkout at its current revision."; return }
            if (-not (Test-RitsuLibTag $tag)) { Write-Warn "RitsuLib source has no tag $tag; leaving the checkout as-is."; return }
        }
        & git -C $RitsuLibSrcDir -c advice.detachedHead=false checkout --quiet $tag
        if ($LASTEXITCODE -ne 0) { Write-Warn "git checkout $tag failed; leaving the checkout as-is."; return }
        Write-Info "RitsuLib source: checked out $tag"
    }
    finally { $env:GIT_TERMINAL_PROMPT = $prevPrompt }
}

Write-Info "Phase 0: RitsuLib dependency"
$pinnedVersion = Get-VersionInFile $ProjectFile $RitsuLibVerRe

if ($RitsuLibVersion) {
    $targetRitsuLib = $RitsuLibVersion
    Write-Info "RitsuLib: pinned to $targetRitsuLib by -RitsuLibVersion"
} elseif ($NoRitsuLibUpdate) {
    $targetRitsuLib = $pinnedVersion
    Write-Info "RitsuLib: update skipped (-NoRitsuLibUpdate); staying on $targetRitsuLib"
} else {
    $latest = Get-LatestRitsuLibVersion
    $targetRitsuLib = if ($latest) { $latest } else { $pinnedVersion }
    if ($latest -and ([version]$latest -lt [version]$pinnedVersion)) {
        # Someone pinned ahead of the feed (e.g. a pulled release). Never silently downgrade.
        Write-Warn "Pinned RitsuLib $pinnedVersion is newer than the latest published $latest; keeping $pinnedVersion."
        $targetRitsuLib = $pinnedVersion
    }
}

if (Set-VersionInFile $ProjectFile $RitsuLibVerRe $targetRitsuLib) {
    Write-Info "RitsuLib: $pinnedVersion -> $targetRitsuLib (illusionist.csproj)"
} else {
    Write-Info "RitsuLib: $targetRitsuLib (already current)"
}

# Restore first: the compat gate reads metadata out of the resolved package on disk.
& $DotnetBin restore $ProjectFile "/p:Sts2Root=$Sts2Root" | Out-Null
if ($LASTEXITCODE -ne 0) {
    $restoreExit = $LASTEXITCODE
    Set-VersionInFile $ProjectFile $RitsuLibVerRe $pinnedVersion | Out-Null
    # Write-Warn, not Write-Error: under -ErrorAction Stop a Write-Error terminates the script with
    # exit 1 before the explicit exit code below is ever reached.
    Write-Warn "dotnet restore failed for RitsuLib $targetRitsuLib ($restoreExit); reverted the pin to $pinnedVersion."
    exit 2
}
if (-not (Test-RitsuLibCompatible $targetRitsuLib)) {
    # Leave the tree exactly as we found it rather than half-upgraded.
    Set-VersionInFile $ProjectFile $RitsuLibVerRe $pinnedVersion | Out-Null
    Write-Warn "Refusing to build against RitsuLib $targetRitsuLib; reverted the pin to $pinnedVersion."
    exit 3
}

$workshopRitsuLib = Get-WorkshopRitsuLibVersion
if ($workshopRitsuLib -and [version]$targetRitsuLib -gt [version]$workshopRitsuLib) {
    Write-Warn "Steam Workshop RitsuLib is $workshopRitsuLib but the manifest floor is becoming $targetRitsuLib."
    Write-Warn "Do not publish until the Workshop item updates, or players cannot load the mod at all."
}
if (Set-VersionInFile $ManifestSrc $ManifestDepRe $targetRitsuLib) {
    Write-Info "RitsuLib: dependency min_version -> $targetRitsuLib (mod_manifest.json)"
}
Sync-RitsuLibSource $targetRitsuLib

# --- Phase 1: compile C# ---
Write-Info "Phase 1/2: dotnet build"
& $DotnetBin build $ProjectFile "/p:Sts2Root=$Sts2Root"
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed ($LASTEXITCODE)."; exit 2 }

# --- Phase 1.5: convert Spine skeletons (authored in 3.8, binary, named *.json) to the game's
#     runtime version (4.2.43). The converter detects format by extension, so binary sources are
#     copied to a temp *.skel first. Skipped (with a note) if the converter isn't present. ---
$Converter = Join-Path $RepoRoot "SpineSkeletonDataConverter\SpineSkeletonDataConverter.exe"
$ArtDir    = Join-Path $ProjectDir "illusionist\art"
$SkeletonMap = @{ "skeleton.json" = "illusionist.skel"; "skeleton_rest.json" = "illusionist_rest.skel" }
if (Test-Path $Converter) {
    Write-Info "Phase 1.5: convert Spine skeletons -> 4.2.43"
    foreach ($src in $SkeletonMap.Keys) {
        $srcPath = Join-Path $ArtDir $src
        if (-not (Test-Path $srcPath)) { continue }
        # Binary export with a .json name -> give it a .skel temp so the converter reads it as binary.
        $firstByte = [System.IO.File]::ReadAllBytes($srcPath)[0]
        $tmpExt = if ($firstByte -eq 0x7B) { ".json" } else { ".skel" }  # 0x7B = '{' => text JSON
        $tmp = Join-Path $ArtDir ("__skel_tmp" + $tmpExt)
        Copy-Item $srcPath $tmp -Force
        & $Converter $tmp (Join-Path $ArtDir $SkeletonMap[$src]) "-v" "4.2.43"
        if ($LASTEXITCODE -ne 0) { Write-Error "Skeleton conversion failed for $src ($LASTEXITCODE)."; exit 2 }
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Info "Phase 1.5: Spine converter not found; using existing .skel files as-is"
}

# --- Phase 2: pack .pck with Godot (redirect user dirs into repo) ---
Write-Info "Phase 2/2: Godot pck"
$prevAppData = $env:APPDATA; $prevLocal = $env:LOCALAPPDATA; $prevTemp = $env:TEMP; $prevTmp = $env:TMP
try {
    $env:APPDATA = $GodotAppData; $env:LOCALAPPDATA = $GodotLocalAppData; $env:TEMP = $GodotTemp; $env:TMP = $GodotTemp
    # Import pass: compiles spine textures (and other PNGs) into .ctex under .godot/imported, so
    # build_pck.gd can ship the Spine atlas texture as an imported resource (spine-godot loads it via
    # ResourceLoader). Raw-PNG assets (cards/avatars) are still packed raw and read via FileAccess.
    & $GodotExe --headless --path $ProjectDir --import
    if ($LASTEXITCODE -ne 0) { Write-Error "Godot import pass failed ($LASTEXITCODE)."; exit 2 }
    & $GodotExe --headless --path $ProjectDir --script (Join-Path $ProjectDir "tools\build_pck.gd")
    if ($LASTEXITCODE -ne 0) { Write-Error "Godot pck build failed ($LASTEXITCODE)."; exit 2 }
}
finally {
    $env:APPDATA = $prevAppData; $env:LOCALAPPDATA = $prevLocal; $env:TEMP = $prevTemp; $env:TMP = $prevTmp
}

if (-not (Test-Path $DllSource)) { Write-Error "Built DLL missing: $DllSource"; exit 2 }
if (-not (Test-Path $PckSource)) { Write-Error "Built PCK missing: $PckSource"; exit 2 }

# --- Install ---
if (-not $SkipInstallCopy) {
    $ModsDir = Join-Path $Sts2Root "mods\$ModId"
    New-Item -ItemType Directory -Force -Path $ModsDir | Out-Null
    Remove-Item (Join-Path $ModsDir "*.dll"), (Join-Path $ModsDir "*.pck") -Force -ErrorAction SilentlyContinue
    Copy-Item $DllSource     -Destination $ModsDir -Force
    Copy-Item $PckSource     -Destination $ModsDir -Force
    Copy-Item $ManifestSrc   -Destination $ModsDir -Force
    Write-Info "Installed to: $ModsDir"
    Write-Info "Files: illusionist.dll, illusionist.pck, mod_manifest.json"

    # Also refresh the Steam Workshop upload payload so it never goes stale (the art lives in the PCK).
    # This only updates local files; uploading remains a separate, manual step.
    $WorkshopContent = Join-Path $RepoRoot "illusionist-workshop\content"
    if (Test-Path $WorkshopContent) {
        Copy-Item $DllSource   -Destination $WorkshopContent -Force
        Copy-Item $PckSource   -Destination $WorkshopContent -Force
        Copy-Item $ManifestSrc -Destination $WorkshopContent -Force
        Write-Info "Synced Steam Workshop payload: $WorkshopContent"
    }
} else {
    Write-Info "Build complete (install skipped)."
}
