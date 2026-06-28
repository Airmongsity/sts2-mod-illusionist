param(
    [string]$Sts2Root = "D:\Program Files\Steam\steamapps\common\Slay the Spire 2",
    [string]$GodotBin,
    [string]$DotnetBin = "dotnet",
    [switch]$SkipInstallCopy,
    [switch]$Help
)

if ($Help) {
    Write-Host @"
Usage: build-illusionist-windows.ps1 [OPTIONS]

Builds the Illusionist mod (C# dll + Godot .pck) and installs it to <Sts2Root>\mods\illusionist.

Options:
  -Sts2Root <path>   Slay the Spire 2 install dir (default: D:\Program Files\Steam\steamapps\common\Slay the Spire 2)
  -GodotBin <path>   Godot 4.5.1 exe (default: searches repo root + PATH for Godot_v4.5.1-stable_win64*.exe)
  -DotnetBin <path>  dotnet executable (default: dotnet)
  -SkipInstallCopy   Build only; do not copy into the game's mods dir
  -Help              Show this help
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
} else {
    Write-Info "Build complete (install skipped)."
}
