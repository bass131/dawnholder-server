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
    return Join-Path $Root $RelativePath
}

function Get-ArchitectureSettings {
    $settingsPath = Join-Path $PSScriptRoot 'comparison-settings.json'
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($settings.schemaVersion -ne 1) {
        throw 'Unsupported comparison settings schema.'
    }
    return $settings
}
