[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("snapshot", "diff", "selftest")]
    [string] $Command,

    [string] $OutputPath,
    [string] $BaselinePath,
    [string] $CurrentSnapshotPath,
    [string] $JsonOutputPath,
    [string] $MarkdownOutputPath,
    [string] $BaseRef,
    [string] $ProjectRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Resolve-ProjectRoot {
    param([string] $RequestedRoot)

    $candidate = if ([string]::IsNullOrWhiteSpace($RequestedRoot)) {
        Join-Path $PSScriptRoot ".."
    } else {
        $RequestedRoot
    }

    $resolved = (Resolve-Path -LiteralPath $candidate).Path
    if (-not (Test-Path -LiteralPath (Join-Path $resolved "mod_manifest.json"))) {
        throw "Project root does not contain mod_manifest.json: $resolved"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $resolved "Scripts"))) {
        throw "Project root does not contain Scripts/: $resolved"
    }
    return $resolved
}

function Get-RepoRoot {
    param([string] $ResolvedProjectRoot)

    $output = & git -C $ResolvedProjectRoot rev-parse --show-toplevel 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to resolve Git repository root: $output"
    }
    return ([string]$output).Trim()
}

function Write-Utf8File {
    param(
        [string] $Path,
        [string] $Content
    )

    $absolute = [System.IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $absolute
    if (-not (Test-Path -LiteralPath $parent)) {
        [void](New-Item -ItemType Directory -Path $parent -Force)
    }
    [System.IO.File]::WriteAllText($absolute, $Content, $Utf8NoBom)
}

function Get-RelativeUnixPath {
    param(
        [string] $BasePath,
        [string] $TargetPath
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [Uri]::new($baseFull)
    $targetUri = [Uri]::new($targetFull)
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
}

function Remove-CSharpComments {
    param([string] $Text)

    $builder = [System.Text.StringBuilder]::new()
    $state = "normal"
    $i = 0
    while ($i -lt $Text.Length) {
        $c = $Text[$i]
        $next = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }

        switch ($state) {
            "normal" {
                if ($c -eq '/' -and $next -eq '/') {
                    $state = "lineComment"
                    $i += 2
                    continue
                }
                if ($c -eq '/' -and $next -eq '*') {
                    $state = "blockComment"
                    $i += 2
                    continue
                }
                if ($c -eq '"') {
                    $isVerbatim = $i -gt 0 -and $Text[$i - 1] -eq '@'
                    $state = if ($isVerbatim) { "verbatimString" } else { "string" }
                } elseif ($c -eq "'") {
                    $state = "char"
                }
                [void]$builder.Append($c)
                $i++
            }
            "lineComment" {
                if ($c -eq "`r" -or $c -eq "`n") {
                    [void]$builder.Append($c)
                    $state = "normal"
                }
                $i++
            }
            "blockComment" {
                if ($c -eq '*' -and $next -eq '/') {
                    $state = "normal"
                    $i += 2
                    continue
                }
                if ($c -eq "`r" -or $c -eq "`n") {
                    [void]$builder.Append($c)
                }
                $i++
            }
            "string" {
                [void]$builder.Append($c)
                if ($c -eq '\' -and $i + 1 -lt $Text.Length) {
                    [void]$builder.Append($next)
                    $i += 2
                    continue
                }
                if ($c -eq '"') {
                    $state = "normal"
                }
                $i++
            }
            "verbatimString" {
                [void]$builder.Append($c)
                if ($c -eq '"') {
                    if ($next -eq '"') {
                        [void]$builder.Append($next)
                        $i += 2
                        continue
                    }
                    $state = "normal"
                }
                $i++
            }
            "char" {
                [void]$builder.Append($c)
                if ($c -eq '\' -and $i + 1 -lt $Text.Length) {
                    [void]$builder.Append($next)
                    $i += 2
                    continue
                }
                if ($c -eq "'") {
                    $state = "normal"
                }
                $i++
            }
        }
    }
    return $builder.ToString()
}

function Get-BalancedRange {
    param(
        [string] $Text,
        [int] $OpenIndex,
        [char] $OpenCharacter,
        [char] $CloseCharacter
    )

    if ($OpenIndex -lt 0 -or $OpenIndex -ge $Text.Length -or $Text[$OpenIndex] -ne $OpenCharacter) {
        throw "Invalid balanced-range start at index $OpenIndex for '$OpenCharacter'."
    }

    $depth = 0
    $state = "normal"
    $i = $OpenIndex
    while ($i -lt $Text.Length) {
        $c = $Text[$i]
        $next = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }
        switch ($state) {
            "normal" {
                if ($c -eq '"') {
                    $state = if ($i -gt 0 -and $Text[$i - 1] -eq '@') { "verbatimString" } else { "string" }
                } elseif ($c -eq "'") {
                    $state = "char"
                } elseif ($c -eq $OpenCharacter) {
                    $depth++
                } elseif ($c -eq $CloseCharacter) {
                    $depth--
                    if ($depth -eq 0) {
                        return [pscustomobject][ordered]@{
                            Inner = $Text.Substring($OpenIndex + 1, $i - $OpenIndex - 1)
                            EndIndex = $i
                        }
                    }
                }
                $i++
            }
            "string" {
                if ($c -eq '\') {
                    $i += 2
                    continue
                }
                if ($c -eq '"') { $state = "normal" }
                $i++
            }
            "verbatimString" {
                if ($c -eq '"') {
                    if ($next -eq '"') {
                        $i += 2
                        continue
                    }
                    $state = "normal"
                }
                $i++
            }
            "char" {
                if ($c -eq '\') {
                    $i += 2
                    continue
                }
                if ($c -eq "'") { $state = "normal" }
                $i++
            }
        }
    }
    throw "Unbalanced '$OpenCharacter$CloseCharacter' segment starting at index $OpenIndex."
}

function Split-TopLevelArguments {
    param([string] $Text)

    $parts = [System.Collections.Generic.List[string]]::new()
    $start = 0
    $round = 0
    $square = 0
    $curly = 0
    $angle = 0
    $state = "normal"
    $i = 0
    while ($i -lt $Text.Length) {
        $c = $Text[$i]
        $next = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }
        if ($state -eq "string") {
            if ($c -eq '\') { $i += 2; continue }
            if ($c -eq '"') { $state = "normal" }
            $i++; continue
        }
        if ($state -eq "verbatimString") {
            if ($c -eq '"' -and $next -eq '"') { $i += 2; continue }
            if ($c -eq '"') { $state = "normal" }
            $i++; continue
        }
        if ($state -eq "char") {
            if ($c -eq '\') { $i += 2; continue }
            if ($c -eq "'") { $state = "normal" }
            $i++; continue
        }

        if ($c -eq '"') {
            $state = if ($i -gt 0 -and $Text[$i - 1] -eq '@') { "verbatimString" } else { "string" }
        } elseif ($c -eq "'") {
            $state = "char"
        } elseif ($c -eq '(') { $round++
        } elseif ($c -eq ')') { $round--
        } elseif ($c -eq '[') { $square++
        } elseif ($c -eq ']') { $square--
        } elseif ($c -eq '{') { $curly++
        } elseif ($c -eq '}') { $curly--
        } elseif ($c -eq '<') { $angle++
        } elseif ($c -eq '>') { if ($angle -gt 0) { $angle-- }
        } elseif ($c -eq ',' -and $round -eq 0 -and $square -eq 0 -and $curly -eq 0 -and $angle -eq 0) {
            $parts.Add($Text.Substring($start, $i - $start).Trim())
            $start = $i + 1
        }
        $i++
    }
    $tail = $Text.Substring($start).Trim()
    if ($tail.Length -gt 0 -or $parts.Count -gt 0) {
        $parts.Add($tail)
    }
    return @($parts)
}

function Normalize-Expression {
    param([AllowEmptyString()][string] $Text)
    if ($null -eq $Text) { return $null }
    return ([regex]::Replace($Text.Trim(), '\s+', ' '))
}

function Get-Sha256Text {
    param([string] $Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace("-", "")
    } finally {
        $sha.Dispose()
    }
}

function Convert-PascalToStem {
    param([string] $Name)
    $step1 = [regex]::Replace($Name, '([A-Z]+)([A-Z][a-z])', '$1_$2')
    $step2 = [regex]::Replace($step1, '([a-z0-9])([A-Z])', '$1_$2')
    return $step2.ToUpperInvariant()
}

function Get-ClassBlock {
    param(
        [string] $Source,
        [string] $ClassName,
        [int] $SearchStart
    )
    $classMatch = [regex]::Match($Source.Substring($SearchStart), "\bclass\s+" + [regex]::Escape($ClassName) + "\b")
    if (-not $classMatch.Success) {
        throw "Unable to locate class $ClassName."
    }
    $classIndex = $SearchStart + $classMatch.Index
    $openIndex = $Source.IndexOf('{', $classIndex)
    if ($openIndex -lt 0) { throw "Unable to locate class body for $ClassName." }
    $range = Get-BalancedRange $Source $openIndex '{' '}'
    return [pscustomobject][ordered]@{
        Block = $Source.Substring($classIndex, $range.EndIndex - $classIndex + 1)
        EndIndex = $range.EndIndex
    }
}

function Get-LocalizationFiles {
    param([string] $ResolvedProjectRoot)

    $result = [ordered]@{}
    foreach ($language in @("eng", "zhs")) {
        $byKind = [ordered]@{}
        foreach ($entry in @(
            @{ Kind = "card"; File = "cards.json" },
            @{ Kind = "relic"; File = "relics.json" },
            @{ Kind = "potion"; File = "potions.json" },
            @{ Kind = "power"; File = "powers.json" },
            @{ Kind = "affliction"; File = "afflictions.json" }
        )) {
            $path = Join-Path $ResolvedProjectRoot ("illusionist\localization\{0}\{1}" -f $language, $entry.File)
            $map = [ordered]@{}
            if (Test-Path -LiteralPath $path) {
                $parsed = Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json
                foreach ($property in @($parsed.PSObject.Properties | Sort-Object Name)) {
                    $map[$property.Name] = [string]$property.Value
                }
            }
            $byKind[$entry.Kind] = $map
        }
        $result[$language] = $byKind
    }
    return $result
}

function Get-EntityLocalization {
    param(
        [System.Collections.IDictionary] $LocalizationFiles,
        [string] $Kind,
        [string] $StableId
    )

    $localized = [ordered]@{}
    foreach ($language in @("eng", "zhs")) {
        $values = [ordered]@{}
        $prefix = "$StableId."
        $sourceMap = $LocalizationFiles[$language][$Kind]
        foreach ($key in @($sourceMap.Keys | Where-Object { $_.StartsWith($prefix, [StringComparison]::Ordinal) } | Sort-Object)) {
            $suffix = $key.Substring($prefix.Length)
            $values[$suffix] = $sourceMap[$key]
        }
        $localized[$language] = $values
    }
    return $localized
}

function Get-ConstructorBaseArguments {
    param(
        [string] $ClassBlock,
        [string] $ClassName
    )
    $constructor = [regex]::Match($ClassBlock, "(?s)\b" + [regex]::Escape($ClassName) + "\s*\([^)]*\)\s*:\s*base\s*\(")
    if (-not $constructor.Success) { return @() }
    $openIndex = $constructor.Index + $constructor.Value.LastIndexOf('(')
    $range = Get-BalancedRange $ClassBlock $openIndex '(' ')'
    return @(Split-TopLevelArguments $range.Inner | ForEach-Object { Normalize-Expression $_ })
}

function Get-DynamicVariables {
    param([string] $ClassBlock)

    $values = [ordered]@{}
    $matches = [regex]::Matches($ClassBlock, 'new\s+(?<type>(?:[A-Za-z_]\w*\.)*[A-Za-z_]\w*Var(?:<[^>]+>)?)\s*\(')
    foreach ($match in $matches) {
        $openIndex = $match.Index + $match.Value.LastIndexOf('(')
        $range = Get-BalancedRange $ClassBlock $openIndex '(' ')'
        $typeName = [string]$match.Groups['type'].Value
        $shortType = $typeName.Substring($typeName.LastIndexOf('.') + 1)
        $arguments = @(Split-TopLevelArguments $range.Inner)
        if ($shortType -eq "DynamicVar" -and $arguments.Count -gt 0 -and $arguments[0] -match '^"(?<name>[^"]+)"$') {
            $baseName = $Matches.name
        } elseif ($shortType -match '^PowerVar<(?<power>[^>]+)>$') {
            $baseName = $Matches.power
        } else {
            $baseName = $shortType -replace 'Var$', ''
        }

        $name = $baseName
        $suffix = 2
        while ($values.Contains($name)) {
            $name = "$baseName#$suffix"
            $suffix++
        }
        $values[$name] = Normalize-Expression ("{0}({1})" -f $shortType, $range.Inner)
    }
    return $values
}

function Get-UpgradeCode {
    param([string] $ClassBlock)
    $match = [regex]::Match($ClassBlock, '(?s)\bOnUpgrade\s*\([^)]*\)\s*\{')
    if (-not $match.Success) { return $null }
    $openIndex = $match.Index + $match.Value.LastIndexOf('{')
    $range = Get-BalancedRange $ClassBlock $openIndex '{' '}'
    return Normalize-Expression $range.Inner
}

function Get-PublicExpressions {
    param([string] $ClassBlock)

    $expressions = [ordered]@{}
    foreach ($name in @("Rarity", "Type", "StackType", "Potency")) {
        $pattern = '(?s)\bpublic\s+override\s+[^\{;=]+?\s+' + [regex]::Escape($name) + '\s*=>\s*(?<value>.*?);'
        $match = [regex]::Match($ClassBlock, $pattern)
        if ($match.Success) {
            $expressions[$name] = Normalize-Expression $match.Groups['value'].Value
        }
    }
    return $expressions
}

function Get-PublicEntityData {
    param(
        [string] $Kind,
        [string] $ClassName,
        [string] $ClassBlock
    )

    $public = [ordered]@{}
    if ($Kind -eq "card") {
        $baseArguments = @(Get-ConstructorBaseArguments $ClassBlock $ClassName)
        $public["costExpression"] = if ($baseArguments.Count -gt 0) { $baseArguments[0] } else { $null }
        $public["typeExpression"] = if ($baseArguments.Count -gt 1) { $baseArguments[1] } else { $null }
        $public["rarityExpression"] = if ($baseArguments.Count -gt 2) { $baseArguments[2] } else { $null }
        $public["targetExpression"] = if ($baseArguments.Count -gt 3) { $baseArguments[3] } else { $null }
        $public["dynamicVariables"] = Get-DynamicVariables $ClassBlock
        $public["upgradeCode"] = Get-UpgradeCode $ClassBlock
        $keywords = @([regex]::Matches($ClassBlock, '(?:CardKeyword|IllusionistKeywords)\.[A-Za-z_]\w*') |
            ForEach-Object { $_.Value } | Sort-Object -Unique)
        $public["keywordReferences"] = $keywords
    }
    $public["overrideExpressions"] = Get-PublicExpressions $ClassBlock
    return $public
}

function Export-PublicSurfaceObject {
    param([string] $ResolvedProjectRoot)

    $manifestPath = Join-Path $ResolvedProjectRoot "mod_manifest.json"
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $localizationFiles = Get-LocalizationFiles $ResolvedProjectRoot
    $entities = [System.Collections.Generic.List[object]]::new()
    $keys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $registrationPattern = '(?s)\[Register(?<registration>Card|Relic|Potion|Power|Affliction)(?<arguments>[^\]]*)\](?:\s*\[[^\]]+\])*\s*(?:(?:public|internal|protected|private|sealed|abstract|partial|static)\s+)*class\s+(?<class>[A-Za-z_]\w*)'

    foreach ($file in @(Get-ChildItem -LiteralPath (Join-Path $ResolvedProjectRoot "Scripts") -Filter "*.cs" -File -Recurse | Sort-Object FullName)) {
        $raw = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
        $source = Remove-CSharpComments $raw
        foreach ($match in [regex]::Matches($source, $registrationPattern)) {
            $registration = [string]$match.Groups['registration'].Value
            $kind = $registration.ToLowerInvariant()
            $className = [string]$match.Groups['class'].Value
            $arguments = [string]$match.Groups['arguments'].Value
            $stemMatch = [regex]::Match($arguments, 'StableEntryStem\s*=\s*"(?<stem>[^"]+)"')
            $stem = if ($stemMatch.Success) { $stemMatch.Groups['stem'].Value } else { Convert-PascalToStem $className }
            $stableId = "ILLUSIONIST_{0}_{1}" -f $registration.ToUpperInvariant(), $stem
            $key = "$kind`:$stableId"
            if (-not $keys.Add($key)) {
                throw "Duplicate registered entity key: $key"
            }

            $classResult = Get-ClassBlock $source $className $match.Index
            $semanticText = [regex]::Replace($classResult.Block, '\s+', '')
            $entities.Add([pscustomobject][ordered]@{
                key = $key
                kind = $kind
                stableId = $stableId
                className = $className
                sourcePath = Get-RelativeUnixPath $ResolvedProjectRoot $file.FullName
                public = Get-PublicEntityData $kind $className $classResult.Block
                localization = Get-EntityLocalization $localizationFiles $kind $stableId
                semanticFingerprint = Get-Sha256Text $semanticText
            })
        }
    }

    return [pscustomobject][ordered]@{
        schemaVersion = 1
        modId = [string]$manifest.id
        modVersion = [string]$manifest.version
        entities = @($entities | Sort-Object key)
    }
}

function Write-PublicSurfaceSnapshot {
    param(
        [string] $ResolvedProjectRoot,
        [string] $Path
    )
    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "snapshot requires -OutputPath."
    }
    $snapshot = Export-PublicSurfaceObject $ResolvedProjectRoot
    $json = $snapshot | ConvertTo-Json -Depth 30
    Write-Utf8File $Path ($json + "`n")
    return $snapshot
}

function Read-PublicSurfaceSnapshot {
    param([string] $Path)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        throw "Snapshot does not exist: $Path"
    }
    try {
        $snapshot = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    } catch {
        throw "Malformed snapshot '$Path': $($_.Exception.Message)"
    }
    if ($snapshot.schemaVersion -ne 1 -or $null -eq $snapshot.entities) {
        throw "Unsupported or incomplete snapshot: $Path"
    }
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entity in @($snapshot.entities)) {
        if ([string]::IsNullOrWhiteSpace([string]$entity.key)) {
            throw "Snapshot contains an entity without a key: $Path"
        }
        if (-not $seen.Add([string]$entity.key)) {
            throw "Duplicate registered entity key in snapshot: $($entity.key)"
        }
    }
    return $snapshot
}

function Convert-ValueForDiff {
    param($Value)
    if ($null -eq $Value) { return $null }
    if ($Value -is [string] -or $Value -is [ValueType]) { return [string]$Value }
    return ($Value | ConvertTo-Json -Depth 20 -Compress)
}

function Add-FlattenedValues {
    param(
        [System.Collections.IDictionary] $Target,
        [string] $Prefix,
        $Value
    )

    if ($null -eq $Value -or $Value -is [string] -or $Value -is [ValueType]) {
        $Target[$Prefix] = Convert-ValueForDiff $Value
        return
    }
    if ($Value -is [System.Collections.IDictionary]) {
        if ($Value.Count -eq 0) {
            $Target[$Prefix] = "{}"
            return
        }
        foreach ($key in @($Value.Keys | Sort-Object)) {
            $child = if ([string]::IsNullOrEmpty($Prefix)) { [string]$key } else { "$Prefix.$key" }
            Add-FlattenedValues $Target $child $Value[$key]
        }
        return
    }
    $properties = @($Value.PSObject.Properties)
    if ($properties.Count -gt 0 -and -not ($Value -is [System.Collections.IEnumerable])) {
        foreach ($property in @($properties | Sort-Object Name)) {
            $child = if ([string]::IsNullOrEmpty($Prefix)) { $property.Name } else { "$Prefix.$($property.Name)" }
            Add-FlattenedValues $Target $child $property.Value
        }
        return
    }
    $Target[$Prefix] = Convert-ValueForDiff $Value
}

function Get-ComparableEntityJson {
    param($Entity)
    $comparable = [pscustomobject][ordered]@{
        kind = $Entity.kind
        stableId = $Entity.stableId
        public = $Entity.public
        localization = $Entity.localization
        semanticFingerprint = $Entity.semanticFingerprint
    }
    return ($comparable | ConvertTo-Json -Depth 30 -Compress)
}

function Get-DisplayTitle {
    param($Entity)
    foreach ($language in @("zhs", "eng")) {
        $languageValue = if ($Entity.localization -is [System.Collections.IDictionary]) {
            $Entity.localization[$language]
        } else {
            $languageProperty = $Entity.localization.PSObject.Properties[$language]
            if ($null -eq $languageProperty) { $null } else { $languageProperty.Value }
        }
        if ($null -eq $languageValue) { continue }

        $titleValue = if ($languageValue -is [System.Collections.IDictionary]) {
            $languageValue["title"]
        } else {
            $titleProperty = $languageValue.PSObject.Properties["title"]
            if ($null -eq $titleProperty) { $null } else { $titleProperty.Value }
        }
        if (-not [string]::IsNullOrWhiteSpace([string]$titleValue)) {
            return [string]$titleValue
        }
    }
    return [string]$Entity.className
}

function Get-EntityFieldChanges {
    param(
        $Before,
        $After
    )
    $beforeFields = [ordered]@{}
    $afterFields = [ordered]@{}
    Add-FlattenedValues $beforeFields "public" $Before.public
    Add-FlattenedValues $beforeFields "localization" $Before.localization
    Add-FlattenedValues $afterFields "public" $After.public
    Add-FlattenedValues $afterFields "localization" $After.localization
    $fieldNames = @($beforeFields.Keys + $afterFields.Keys | Sort-Object -Unique)
    $changes = [System.Collections.Generic.List[object]]::new()
    foreach ($field in $fieldNames) {
        $beforeValue = if ($beforeFields.Contains($field)) { $beforeFields[$field] } else { $null }
        $afterValue = if ($afterFields.Contains($field)) { $afterFields[$field] } else { $null }
        if ($beforeValue -cne $afterValue) {
            $changes.Add([pscustomobject][ordered]@{
                field = $field
                before = $beforeValue
                after = $afterValue
            })
        }
    }
    if ($changes.Count -eq 0 -and $Before.semanticFingerprint -cne $After.semanticFingerprint) {
        $changes.Add([pscustomobject][ordered]@{
            field = "implementation"
            before = "review required"
            after = "review required"
        })
    }
    return @($changes)
}

function Get-ReleaseNotesFromLines {
    param([object[]] $Lines)

    $notes = [System.Collections.Generic.List[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($line in @($Lines)) {
        if ([string]$line -match '^\s*Release-Note:\s*(?<note>.+?)\s*$') {
            $note = $Matches.note
            if ($seen.Add($note)) { $notes.Add($note) }
        }
    }
    return @($notes)
}

function Get-ExplicitReleaseNotes {
    param(
        [string] $RepoRoot,
        [string] $RequestedBaseRef
    )
    if ([string]::IsNullOrWhiteSpace($RequestedBaseRef)) { return @() }

    & git -C $RepoRoot rev-parse --verify "$RequestedBaseRef^{commit}" *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Invalid Git base reference: $RequestedBaseRef"
    }
    $messages = & git -C $RepoRoot log "$RequestedBaseRef..HEAD" --reverse --format=%B 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read Git history from $RequestedBaseRef`: $messages"
    }
    return @(Get-ReleaseNotesFromLines @($messages))
}

function Compare-PublicSurfaceSnapshots {
    param(
        $Baseline,
        $Current,
        [string] $RepoRoot,
        [string] $RequestedBaseRef
    )

    $beforeByKey = @{}
    $afterByKey = @{}
    foreach ($entity in @($Baseline.entities)) { $beforeByKey[[string]$entity.key] = $entity }
    foreach ($entity in @($Current.entities)) { $afterByKey[[string]$entity.key] = $entity }
    $allKeys = @($beforeByKey.Keys + $afterByKey.Keys | Sort-Object -Unique)
    $added = [System.Collections.Generic.List[object]]::new()
    $removed = [System.Collections.Generic.List[object]]::new()
    $changed = [System.Collections.Generic.List[object]]::new()

    foreach ($key in $allKeys) {
        if (-not $beforeByKey.ContainsKey($key)) {
            $entity = $afterByKey[$key]
            $added.Add([pscustomobject][ordered]@{
                key = $key; kind = $entity.kind; stableId = $entity.stableId; title = Get-DisplayTitle $entity
            })
            continue
        }
        if (-not $afterByKey.ContainsKey($key)) {
            $entity = $beforeByKey[$key]
            $removed.Add([pscustomobject][ordered]@{
                key = $key; kind = $entity.kind; stableId = $entity.stableId; title = Get-DisplayTitle $entity
            })
            continue
        }
        $before = $beforeByKey[$key]
        $after = $afterByKey[$key]
        if ((Get-ComparableEntityJson $before) -cne (Get-ComparableEntityJson $after)) {
            $changed.Add([pscustomobject][ordered]@{
                key = $key
                kind = $after.kind
                stableId = $after.stableId
                title = Get-DisplayTitle $after
                changes = @(Get-EntityFieldChanges $before $after)
            })
        }
    }

    return [pscustomobject][ordered]@{
        schemaVersion = 1
        baselineVersion = [string]$Baseline.modVersion
        currentVersion = [string]$Current.modVersion
        baseRef = if ([string]::IsNullOrWhiteSpace($RequestedBaseRef)) { $null } else { $RequestedBaseRef }
        added = @($added)
        removed = @($removed)
        changed = @($changed)
        explicitNotes = @(Get-ExplicitReleaseNotes $RepoRoot $RequestedBaseRef)
    }
}

function Format-MarkdownValue {
    param($Value)
    if ($null -eq $Value) { return "_(none)_" }
    return "`"" + (([string]$Value) -replace "`r?`n", " / " -replace '`', "'") + "`""
}

function Convert-DiffToMarkdown {
    param($Difference)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Release Note Draft")
    $lines.Add("")
    $lines.Add("- Baseline: $($Difference.baselineVersion)")
    $lines.Add("- Current: $($Difference.currentVersion)")
    if (-not [string]::IsNullOrWhiteSpace([string]$Difference.baseRef)) {
        $lines.Add("- Git base: ``$($Difference.baseRef)``")
    }

    foreach ($section in @(
        @{ Title = "Added"; Items = @($Difference.added) },
        @{ Title = "Removed"; Items = @($Difference.removed) }
    )) {
        if ($section.Items.Count -eq 0) { continue }
        $lines.Add("")
        $lines.Add("## $($section.Title)")
        foreach ($item in @($section.Items | Sort-Object kind, stableId)) {
            $lines.Add("- [$($item.kind)] $($item.title) (``$($item.stableId)``)")
        }
    }

    if (@($Difference.changed).Count -gt 0) {
        $lines.Add("")
        $lines.Add("## Changed")
        foreach ($kindGroup in @($Difference.changed | Group-Object kind | Sort-Object Name)) {
            $lines.Add("")
            $lines.Add("### $($kindGroup.Name)")
            foreach ($item in @($kindGroup.Group | Sort-Object stableId)) {
                $lines.Add("- $($item.title) (``$($item.stableId)``)")
                foreach ($change in @($item.changes)) {
                    $before = Format-MarkdownValue $change.before
                    $after = Format-MarkdownValue $change.after
                    $lines.Add("  - ``$($change.field)``: $before -> $after")
                }
            }
        }
    }

    if (@($Difference.explicitNotes).Count -gt 0) {
        $lines.Add("")
        $lines.Add("## Explicit Release Notes")
        foreach ($note in $Difference.explicitNotes) { $lines.Add("- $note") }
    }

    if (@($Difference.added).Count + @($Difference.removed).Count + @($Difference.changed).Count + @($Difference.explicitNotes).Count -eq 0) {
        $lines.Add("")
        $lines.Add("No net player-visible changes detected.")
    }
    return (($lines -join "`n") + "`n")
}

function Invoke-DiffCommand {
    param(
        [string] $ResolvedProjectRoot,
        [string] $RepoRoot,
        [string] $RequestedBaselinePath,
        [string] $RequestedCurrentSnapshotPath,
        [string] $RequestedJsonOutputPath,
        [string] $RequestedMarkdownOutputPath,
        [string] $RequestedBaseRef
    )

    if ([string]::IsNullOrWhiteSpace($RequestedBaselinePath)) { throw "diff requires -BaselinePath." }
    if ([string]::IsNullOrWhiteSpace($RequestedJsonOutputPath)) { throw "diff requires -JsonOutputPath." }
    if ([string]::IsNullOrWhiteSpace($RequestedMarkdownOutputPath)) { throw "diff requires -MarkdownOutputPath." }
    $baseline = Read-PublicSurfaceSnapshot $RequestedBaselinePath
    $current = if ([string]::IsNullOrWhiteSpace($RequestedCurrentSnapshotPath)) {
        Export-PublicSurfaceObject $ResolvedProjectRoot
    } else {
        Read-PublicSurfaceSnapshot $RequestedCurrentSnapshotPath
    }
    $difference = Compare-PublicSurfaceSnapshots $baseline $current $RepoRoot $RequestedBaseRef
    Write-Utf8File $RequestedJsonOutputPath (($difference | ConvertTo-Json -Depth 30) + "`n")
    Write-Utf8File $RequestedMarkdownOutputPath (Convert-DiffToMarkdown $difference)
    return $difference
}

function Invoke-SelfTest {
    param(
        [string] $ResolvedProjectRoot,
        [string] $RepoRoot
    )

    $testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("illusionist-release-surface-" + [Guid]::NewGuid().ToString("N"))
    [void](New-Item -ItemType Directory -Path $testRoot -Force)
    try {
        $firstPath = Join-Path $testRoot "first.json"
        $secondPath = Join-Path $testRoot "second.json"
        [void](Write-PublicSurfaceSnapshot $ResolvedProjectRoot $firstPath)
        [void](Write-PublicSurfaceSnapshot $ResolvedProjectRoot $secondPath)
        $firstHash = (Get-FileHash -LiteralPath $firstPath -Algorithm SHA256).Hash
        $secondHash = (Get-FileHash -LiteralPath $secondPath -Algorithm SHA256).Hash
        if ($firstHash -ne $secondHash) { throw "Deterministic export test failed." }

        $baseline = Read-PublicSurfaceSnapshot $firstPath
        $same = Read-PublicSurfaceSnapshot $secondPath
        $emptyDiff = Compare-PublicSurfaceSnapshots $baseline $same $RepoRoot $null
        if (@($emptyDiff.added).Count -ne 0 -or @($emptyDiff.removed).Count -ne 0 -or @($emptyDiff.changed).Count -ne 0) {
            throw "Empty comparison test failed."
        }

        $emptyObjectFields = [ordered]@{}
        $emptyDictionaryFields = [ordered]@{}
        Add-FlattenedValues $emptyObjectFields "value" ([pscustomobject]@{})
        Add-FlattenedValues $emptyDictionaryFields "value" ([ordered]@{})
        if ($emptyObjectFields["value"] -cne $emptyDictionaryFields["value"]) {
            throw "Empty-object normalization test failed."
        }

        $displayTitleProbe = [pscustomobject]@{
            className = "FallbackTitle"
            localization = [ordered]@{
                zhs = [ordered]@{ title = "中文标题" }
                eng = [ordered]@{ title = "English Title" }
            }
        }
        if ((Get-DisplayTitle $displayTitleProbe) -cne "中文标题") {
            throw "Dictionary-backed display-title test failed."
        }

        $modifiedPath = Join-Path $testRoot "modified.json"
        $modified = (($baseline | ConvertTo-Json -Depth 30) | ConvertFrom-Json)
        $target = @($modified.entities | Where-Object { $_.kind -eq "card" })[0]
        $originalCost = [string]$target.public.costExpression
        $target.public.costExpression = $originalCost + "_SELFTEST"
        Write-Utf8File $modifiedPath (($modified | ConvertTo-Json -Depth 30) + "`n")
        $oneChange = Compare-PublicSurfaceSnapshots $baseline (Read-PublicSurfaceSnapshot $modifiedPath) $RepoRoot $null
        if (@($oneChange.changed).Count -ne 1 -or $oneChange.changed[0].stableId -ne $target.stableId) {
            throw "One-entity net change test failed."
        }

        $target.public.costExpression = $originalCost
        $revertedPath = Join-Path $testRoot "reverted.json"
        Write-Utf8File $revertedPath (($modified | ConvertTo-Json -Depth 30) + "`n")
        $revertedDiff = Compare-PublicSurfaceSnapshots $baseline (Read-PublicSurfaceSnapshot $revertedPath) $RepoRoot $null
        if (@($revertedDiff.changed).Count -ne 0) { throw "Reversion test failed." }

        $duplicate = (($baseline | ConvertTo-Json -Depth 30) | ConvertFrom-Json)
        $duplicate.entities = @($duplicate.entities) + @($duplicate.entities[0])
        $duplicatePath = Join-Path $testRoot "duplicate.json"
        Write-Utf8File $duplicatePath (($duplicate | ConvertTo-Json -Depth 30) + "`n")
        $duplicateFailed = $false
        try { [void](Read-PublicSurfaceSnapshot $duplicatePath) } catch { $duplicateFailed = $true }
        if (-not $duplicateFailed) { throw "Duplicate-key failure test failed." }

        $malformedPath = Join-Path $testRoot "malformed.json"
        Write-Utf8File $malformedPath "{not-json"
        $malformedFailed = $false
        try { [void](Read-PublicSurfaceSnapshot $malformedPath) } catch { $malformedFailed = $true }
        if (-not $malformedFailed) { throw "Malformed-input failure test failed." }

        $invalidRefFailed = $false
        try { [void](Get-ExplicitReleaseNotes $RepoRoot "refs/tags/__illusionist_missing_release_ref__") } catch { $invalidRefFailed = $true }
        if (-not $invalidRefFailed) { throw "Invalid-Git-base failure test failed." }

        $trailerNotes = @(Get-ReleaseNotesFromLines @(
            "subject",
            "Release-Note: 修复镜像结算错误",
            "Release-Note: 修复镜像结算错误",
            "Release-Note: 调整一张牌"
        ))
        if ($trailerNotes.Count -ne 2 -or $trailerNotes[0] -ne "修复镜像结算错误" -or $trailerNotes[1] -ne "调整一张牌") {
            throw "Release-Note trailer collection test failed."
        }

        return [pscustomobject][ordered]@{
            deterministicExport = $true
            entityCount = @($baseline.entities).Count
            emptyComparison = $true
            emptyObjectsNormalized = $true
            dictionaryDisplayTitle = $true
            oneEntityChange = $true
            reversionElided = $true
            duplicateRejected = $true
            malformedRejected = $true
            invalidGitBaseRejected = $true
            releaseNoteTrailersDeduplicated = $true
        }
    } finally {
        if (Test-Path -LiteralPath $testRoot) {
            Remove-Item -LiteralPath $testRoot -Recurse -Force
        }
    }
}

$resolvedProjectRoot = Resolve-ProjectRoot $ProjectRoot
$repoRoot = Get-RepoRoot $resolvedProjectRoot

switch ($Command) {
    "snapshot" {
        $snapshot = Write-PublicSurfaceSnapshot $resolvedProjectRoot $OutputPath
        Write-Host ("Exported {0} entities for v{1} to {2}" -f @($snapshot.entities).Count, $snapshot.modVersion, [System.IO.Path]::GetFullPath($OutputPath))
    }
    "diff" {
        $difference = Invoke-DiffCommand $resolvedProjectRoot $repoRoot $BaselinePath $CurrentSnapshotPath $JsonOutputPath $MarkdownOutputPath $BaseRef
        Write-Host ("Net diff: {0} added, {1} removed, {2} changed, {3} explicit notes." -f @($difference.added).Count, @($difference.removed).Count, @($difference.changed).Count, @($difference.explicitNotes).Count)
    }
    "selftest" {
        $result = Invoke-SelfTest $resolvedProjectRoot $repoRoot
        $result | Format-List
    }
}
