[CmdletBinding(DefaultParameterSetName = 'Extract')]
param(
    [Parameter(ParameterSetName = 'Extract')]
    [string]$Sts2Root = 'D:\Program Files\Steam\steamapps\common\Slay the Spire 2',

    [Parameter(ParameterSetName = 'Extract')]
    [string]$OutputRoot,

    [Parameter(ParameterSetName = 'Extract')]
    [string]$WorkRoot,

    [Parameter(ParameterSetName = 'Extract')]
    [string]$GodotBin,

    [Parameter(ParameterSetName = 'Extract')]
    [string]$IlspyCmd = 'ilspycmd',

    [string]$DotnetBin = 'dotnet',

    [Parameter(ParameterSetName = 'Extract')]
    [switch]$ForceDecompile,

    [Parameter(ParameterSetName = 'Extract')]
    [switch]$Strict,

    [Parameter(Mandatory, ParameterSetName = 'Diff')]
    [switch]$Diff,

    [Parameter(Mandatory, ParameterSetName = 'Diff')]
    [string]$Before,

    [Parameter(Mandatory, ParameterSetName = 'Diff')]
    [string]$After,

    [Parameter(ParameterSetName = 'Diff')]
    [string]$DiffOutput,

    [Parameter(Mandatory, ParameterSetName = 'SelfTest')]
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'
$ToolRoot = $PSScriptRoot
$RepoRoot = Split-Path (Split-Path $ToolRoot -Parent) -Parent
$AnalyzerProject = Join-Path $ToolRoot 'src\Sts2DataExtractor.csproj'
$FixtureRoot = Join-Path $ToolRoot 'fixtures'

function Write-Step([string]$Message) {
    Write-Host "[sts2-data] $Message" -ForegroundColor Cyan
}

function Resolve-Executable([string]$Candidate, [string]$Label) {
    if ($Candidate -and (Test-Path -LiteralPath $Candidate)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }
    if ($Candidate) {
        $command = Get-Command $Candidate -ErrorAction SilentlyContinue
        if ($command) { return $command.Source }
    }
    throw "$Label executable was not found: '$Candidate'"
}

function Resolve-Godot([string]$Candidate) {
    $candidates = @(
        $Candidate,
        $env:GODOT_BIN,
        (Join-Path $RepoRoot 'Godot_v4.5.1-stable_win64_console.exe'),
        (Join-Path $RepoRoot 'Godot_v4.5.1-stable_win64.exe'),
        'Godot_v4.5.1-stable_win64_console.exe',
        'Godot_v4.5.1-stable_win64.exe'
    )
    foreach ($candidatePath in $candidates) {
        if (-not $candidatePath) { continue }
        try { return Resolve-Executable $candidatePath 'Godot' } catch { }
    }
    throw 'Godot 4.5.1 was not found. Pass -GodotBin or set GODOT_BIN.'
}

function Invoke-Checked([string]$Label, [scriptblock]$Action) {
    Write-Step $Label
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE"
    }
}

$dotnet = Resolve-Executable $DotnetBin '.NET'

function Resolve-AnalyzerSdk {
    $sdkCandidates = @()
    foreach ($line in (& $dotnet --list-sdks)) {
        if ($line -match '^(?<version>[0-9]+\.[^ ]+) \[(?<root>.+)\]$') {
            $parsedVersion = [version]$Matches['version']
            if ($parsedVersion.Major -ge 9) {
                $sdkCandidates += [pscustomobject]@{ Version = $parsedVersion; Root = $Matches['root'] }
            }
        }
    }
    $sdk = $sdkCandidates | Sort-Object Version -Descending | Select-Object -First 1
    if (-not $sdk) { throw '.NET SDK 9 or later is required.' }
    $path = Join-Path (Join-Path $sdk.Root $sdk.Version.ToString()) 'Roslyn\bincore'
    if (-not (Test-Path -LiteralPath (Join-Path $path 'Microsoft.CodeAnalysis.CSharp.dll'))) {
        throw "Roslyn binaries were not found under .NET SDK $($sdk.Version): $path"
    }
    return [pscustomobject]@{
        Version = $sdk.Version
        Roslyn = $path
        Framework = "net$($sdk.Version.Major).0"
    }
}

$analyzerSdk = Resolve-AnalyzerSdk
$roslynBinaries = $analyzerSdk.Roslyn
$analyzerFramework = $analyzerSdk.Framework
$analyzerDll = Join-Path (Split-Path $AnalyzerProject -Parent) "bin\Release\$analyzerFramework\Sts2DataExtractor.dll"

function Build-Analyzer {
    $assetsFile = Join-Path (Split-Path $AnalyzerProject -Parent) 'obj\project.assets.json'
    $needsRestore = -not (Test-Path -LiteralPath $assetsFile)
    if (-not $needsRestore) {
        $needsRestore = (Get-Item -LiteralPath $assetsFile).LastWriteTimeUtc -lt (Get-Item -LiteralPath $AnalyzerProject).LastWriteTimeUtc
    }
    if (-not $needsRestore) {
        $needsRestore = -not ((Get-Content -LiteralPath $assetsFile -Raw).Contains('"' + $analyzerFramework + '"'))
    }
    if ($needsRestore) {
        Invoke-Checked 'Preparing analyzer project' {
            & $dotnet restore $AnalyzerProject --ignore-failed-sources -p:NuGetAudit=false "/p:RoslynBinariesPath=$roslynBinaries" "/p:AnalyzerTargetFramework=$analyzerFramework" --nologo
        }
    }
    Invoke-Checked 'Building analyzer' {
        & $dotnet build $AnalyzerProject -c Release --nologo --no-restore -p:NuGetAudit=false "/p:RoslynBinariesPath=$roslynBinaries" "/p:AnalyzerTargetFramework=$analyzerFramework"
    }
    if (-not (Test-Path -LiteralPath $analyzerDll)) { throw "Built analyzer DLL is missing: $analyzerDll" }
}

if ($PSCmdlet.ParameterSetName -eq 'SelfTest') {
    Build-Analyzer
    Invoke-Checked 'Running extractor self-test' {
        & $dotnet $analyzerDll selftest --fixture $FixtureRoot
    }
    Write-Step 'Self-test passed'
    exit 0
}

if ($PSCmdlet.ParameterSetName -eq 'Diff') {
    if (-not $DiffOutput) {
        $DiffOutput = Join-Path $RepoRoot 'game-data\diff'
    }
    $beforePath = (Resolve-Path -LiteralPath $Before).Path
    $afterPath = (Resolve-Path -LiteralPath $After).Path
    $diffPath = [System.IO.Path]::GetFullPath($DiffOutput)
    Build-Analyzer
    Invoke-Checked 'Comparing snapshots' {
        & $dotnet $analyzerDll diff --before $beforePath --after $afterPath --output $diffPath
    }
    Write-Step "Diff written to: $diffPath"
    exit 0
}

$gameRoot = [System.IO.Path]::GetFullPath($Sts2Root)
$dataRoot = Join-Path $gameRoot 'data_sts2_windows_x86_64'
$assemblyPath = Join-Path $dataRoot 'sts2.dll'
$pckPath = Join-Path $gameRoot 'SlayTheSpire2.pck'
$releaseInfoPath = Join-Path $gameRoot 'release_info.json'
foreach ($required in @($assemblyPath, $pckPath, $releaseInfoPath)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required game input is missing: $required"
    }
}

if (-not $OutputRoot) { $OutputRoot = Join-Path $RepoRoot 'game-data' }
if (-not $WorkRoot) { $WorkRoot = Join-Path $RepoRoot '.sts2-data-extractor' }
$outputRootPath = [System.IO.Path]::GetFullPath($OutputRoot)
$workRootPath = [System.IO.Path]::GetFullPath($WorkRoot)

$release = Get-Content -LiteralPath $releaseInfoPath -Raw | ConvertFrom-Json
if (-not $release.version) { throw "release_info.json has no version: $releaseInfoPath" }
$version = ([string]$release.version) -replace '[^A-Za-z0-9._-]', '_'
$assemblyHash = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash.ToLowerInvariant()
$cacheRoot = Join-Path $workRootPath $assemblyHash
$sourceRoot = Join-Path $cacheRoot 'source'
$localizationRoot = Join-Path $cacheRoot 'localization'
$cacheMarker = Join-Path $cacheRoot 'decompile.complete.json'
$snapshotRoot = Join-Path $outputRootPath $version

$ilspy = Resolve-Executable $IlspyCmd 'ilspycmd'
$godot = Resolve-Godot $GodotBin
$godotVersion = (& $godot --version | Select-Object -First 1).Trim()
if (-not $godotVersion.StartsWith('4.5.1')) {
    throw "Godot 4.5.1 is required to read this PCK safely; found '$godotVersion'."
}

Write-Step "Game: $gameRoot ($version)"
Write-Step "Assembly SHA-256: $assemblyHash"

$cacheValid = $false
if ((Test-Path -LiteralPath $cacheMarker) -and (Test-Path -LiteralPath $sourceRoot)) {
    try {
        $marker = Get-Content -LiteralPath $cacheMarker -Raw | ConvertFrom-Json
        $cacheValid = $marker.assemblySha256 -eq $assemblyHash
    } catch { $cacheValid = $false }
}

if ($ForceDecompile -and (Test-Path -LiteralPath $cacheRoot)) {
    $resolvedWork = [System.IO.Path]::GetFullPath($workRootPath).TrimEnd('\') + '\'
    $resolvedCache = [System.IO.Path]::GetFullPath($cacheRoot)
    if (-not $resolvedCache.StartsWith($resolvedWork, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove cache outside work root: $resolvedCache"
    }
    Remove-Item -LiteralPath $cacheRoot -Recurse -Force
    $cacheValid = $false
}

New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
if (-not $cacheValid) {
    if (Test-Path -LiteralPath $sourceRoot) {
        Remove-Item -LiteralPath $sourceRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $sourceRoot | Out-Null
    Invoke-Checked 'Decompiling sts2.dll with ILSpy' {
        & $ilspy -p -o $sourceRoot -r $dataRoot --nested-directories --disable-updatecheck $assemblyPath
    }
    $markerData = [ordered]@{
        assemblySha256 = $assemblyHash
        ilspyVersion = (& $ilspy --version | Select-Object -First 1)
    }
    $markerData | ConvertTo-Json | Set-Content -LiteralPath $cacheMarker -Encoding utf8
} else {
    Write-Step 'Reusing hash-matched decompile cache'
}

if (Test-Path -LiteralPath $localizationRoot) {
    Remove-Item -LiteralPath $localizationRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $localizationRoot | Out-Null
$godotAppData = Join-Path $cacheRoot 'godot-appdata'
$godotLocalAppData = Join-Path $cacheRoot 'godot-localappdata'
$godotTemp = Join-Path $cacheRoot 'godot-temp'
New-Item -ItemType Directory -Force -Path $godotAppData, $godotLocalAppData, $godotTemp | Out-Null
$oldAppData = $env:APPDATA
$oldLocalAppData = $env:LOCALAPPDATA
$oldTemp = $env:TEMP
$oldTmp = $env:TMP
try {
    $env:APPDATA = $godotAppData
    $env:LOCALAPPDATA = $godotLocalAppData
    $env:TEMP = $godotTemp
    $env:TMP = $godotTemp
    Invoke-Checked 'Extracting English localization from PCK' {
        & $godot --headless --path (Join-Path $ToolRoot 'pck') --script (Join-Path $ToolRoot 'pck\extract_localization.gd') -- $pckPath $localizationRoot
    }
} finally {
    $env:APPDATA = $oldAppData
    $env:LOCALAPPDATA = $oldLocalAppData
    $env:TEMP = $oldTemp
    $env:TMP = $oldTmp
}

Build-Analyzer
if (Test-Path -LiteralPath $snapshotRoot) {
    Remove-Item -LiteralPath $snapshotRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $snapshotRoot | Out-Null
$analyzerArgs = @(
    $analyzerDll, 'extract', '--source', $sourceRoot, '--localization', $localizationRoot,
    '--output', $snapshotRoot, '--release-info', $releaseInfoPath,
    '--assembly', $assemblyPath, '--pck', $pckPath,
    '--ilspy-version', (& $ilspy --version | Select-Object -First 1)
)
if ($Strict) { $analyzerArgs += '--strict' }
Invoke-Checked 'Extracting semantic game facts' { & $dotnet @analyzerArgs }

Write-Step "Snapshot written to: $snapshotRoot"
Get-Content -LiteralPath (Join-Path $snapshotRoot 'coverage.md')
