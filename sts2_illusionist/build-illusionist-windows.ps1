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

# --- Phase 2: pack .pck with Godot (redirect user dirs into repo) ---
Write-Info "Phase 2/2: Godot pck"
$prevAppData = $env:APPDATA; $prevLocal = $env:LOCALAPPDATA; $prevTemp = $env:TEMP; $prevTmp = $env:TMP
try {
    $env:APPDATA = $GodotAppData; $env:LOCALAPPDATA = $GodotLocalAppData; $env:TEMP = $GodotTemp; $env:TMP = $GodotTemp
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
