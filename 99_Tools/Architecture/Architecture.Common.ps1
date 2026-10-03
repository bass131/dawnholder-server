function Resolve-ArchitectureInputPath {
    param([string]$Root, [string]$RelativePath)
    $invalidCharacters = '[\\:*?\[\]]'
    $invalidSegments = @('', '.', '..')
    if ([string]::IsNullOrWhiteSpace($RelativePath) -or $RelativePath.StartsWith('/') -or $RelativePath -match $invalidCharacters) {
        throw "Invalid relative path: $RelativePath"
    }
    foreach ($segment in $RelativePath.Split('/')) {
        if ($segment -in $invalidSegments) {
            throw "Invalid path segment: $RelativePath"
        }
    }
    $path = Join-Path $Root $RelativePath
    $current = [IO.Path]::GetFullPath($path)
    while ($current) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Linked input/destination: $current"
            }
        }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ($parent -eq $current) {
            break
        }
        $current = $parent
    }
    return $path
}

function Get-ArchitectureSelection {
    param([string]$Extractor)
    $selection = $Extractor.ToLowerInvariant()
    if ($selection -notin @('roslyn', 'codegraph', 'compare')) {
        throw "Unknown extractor '$Extractor'. Repair: use -Extractor Roslyn|CodeGraph|Compare; no fallback."
    }
    return $selection
}

function Resolve-ArchitectureEvidencePath {
    param([string]$Root, [string]$RelativePath, [object]$Settings)
    if (-not $RelativePath.StartsWith('.backups/', [StringComparison]::Ordinal)) {
        throw 'EvidencePath must be a repository-relative path below Git-excluded .backups/.'
    }
    $path = Resolve-ArchitectureInputPath -Root $Root -RelativePath $RelativePath
    $historical = $Settings.evidencePath
    $freeze = $Settings.freezeRecordPath
    $marker = Join-Path $path '.dawnholder-execution.json'
    # O1 may point at an already owned new Compare root; unmarked historical roots stay protected.
    $currentOwnedRoot = $RelativePath -eq $historical -and (Test-Path -LiteralPath $marker -PathType Leaf)
    $overlapsComparison = (
        $RelativePath -eq $historical -or $RelativePath.StartsWith("$historical/") -or
        $historical.StartsWith("$RelativePath/")
    )
    if (($overlapsComparison -and -not $currentOwnedRoot) -or $freeze.StartsWith("$RelativePath/")) {
        throw 'EvidencePath overlaps preserved comparison/freeze evidence. Repair: choose a new .backups/ root.'
    }
    git -C $Root check-ignore --quiet -- "$RelativePath/"
    if ($LASTEXITCODE -ne 0) {
        throw "EvidencePath is not confirmed Git-excluded: $RelativePath. Repair: choose a Git-excluded .backups/ root."
    }
    return $path
}

function Get-ArchitectureSettings {
    $settingsPath = Join-Path $PSScriptRoot 'comparison-settings.json'
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($settings.schemaVersion -ne 1) {
        throw 'Unsupported comparison settings schema.'
    }
    return $settings
}
