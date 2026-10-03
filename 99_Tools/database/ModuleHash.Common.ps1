# Definitions only. Source identity inspection never opens SQL or adopts database observations.
function Get-MigrationText {
    param([string]$Path)
    # Stable across Git CRLF/LF checkouts; BOM excluded by ReadAllText.
    return [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
}

function Get-MigrationHash {
    param([string]$Sql)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Sql)))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Get-ModuleDefinitionHash {
    param([string]$Sql)
    # Reviewed source, UTF-16LE without BOM. Never derive the expected hash from the database.
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::Unicode.GetBytes($Sql)))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function New-ModuleHashTarget {
    param(
        [string]$File,
        [string]$Text,
        [int]$Offset,
        [string]$Kind,
        [string]$Source,
        [string]$Observed,
        [string]$Expected
    )
    $line = 1 + [regex]::Matches($Text.Substring(0, $Offset), "`n").Count
    [pscustomobject]@{
        File = $File
        Line = $line
        Kind = $Kind
        Source = $Source
        Observed = $Observed
        Expected = $Expected
        IsDrift = $Observed -cne $Expected
        Message = "$Kind consumer differs from reviewed source: $Source"
        Remediation = "Replace '$Observed' with '$Expected' at ${File}:$line; update derived consumers together."
    }
}

function Get-ModuleHashRows {
    param(
        [string]$Text,
        [string]$Table,
        [string]$Pattern,
        [int]$Count
    )
    # Accept only the explicitly registered literal row format, including every row in the block.
    $blocks = [regex]::Matches($Text, '(?is)\bINSERT\s+@' + $Table + '\s+VALUES(?<Rows>.*?)\s*;')
    if ($blocks.Count -ne 1) { throw "Expected one @$Table literal block; hash inspection is unavailable." }
    $block = $blocks[0].Groups['Rows']
    $rows = [regex]::Matches($block.Value, $Pattern)
    $remainder = [regex]::Replace($block.Value, $Pattern, '').Trim()
    if ($rows.Count -ne $Count -or $remainder -notmatch '^[\s,]*$') {
        throw "Expected exactly $Count known @$Table rows; missing, duplicate or unknown rows cannot be inspected."
    }
    [pscustomobject]@{ Offset = $block.Index; Rows = $rows }
}

function Test-ModuleHashConsumers {
    param(
        [string]$DatabaseRoot,
        [object[]]$Contract
    )
    $targets = New-Object 'Collections.Generic.List[object]'
    $files = New-Object 'Collections.Generic.List[string]'
    $migrationNames = @(
        '001_initial.sql', '002_persistence_metadata.sql', '003_module_metadata.sql', '004_module_release.sql'
    )
    $activeFile = 'modules/manifest.json'
    try {
        # Hash raw decoded UTF-8 only after proving decoding did not remove/change any identity bytes.
        $manifestBytes = [IO.File]::ReadAllBytes((Join-Path $DatabaseRoot $activeFile))
        $files.Add($activeFile)
        $utf8 = New-Object Text.UTF8Encoding($false, $true)
        $manifestText = $utf8.GetString($manifestBytes)
        $hasBom = $manifestBytes.Length -ge 3 -and $manifestBytes[0] -eq 0xEF -and
        $manifestBytes[1] -eq 0xBB -and $manifestBytes[2] -eq 0xBF
        if ($hasBom -or $manifestText.Contains("`r") -or
            -not $manifestText.EndsWith("`n")) {
            throw 'Manifest identity requires UTF-8 without BOM, LF and a final newline.'
        }
        $manifestHash = Get-MigrationHash -Sql $manifestText
        $manifest = $manifestText | ConvertFrom-Json
        if ($manifest.FormatVersion -ne 1 -or $manifest.ReleaseVersion -ne 4 -or
            $manifest.SourceEncoding -cne 'UTF-8; no BOM; CRLF->LF' -or
            $manifest.DefinitionEncoding -cne 'UTF-16LE; no BOM; LF; actual CR removed' -or
            $manifest.Entries -isnot [Array]) {
            throw 'Unknown manifest version/encoding/entry format; hash inspection is unavailable.'
        }
        $expectedPaths = @($Contract.Path) + @('modules/permissions.sql')
        $registered = @($manifest.Entries.Path)
        if ($Contract.Count -ne 18 -or $registered.Count -ne $expectedPaths.Count -or
            (($registered | Sort-Object) -join '|') -cne (($expectedPaths | Sort-Object) -join '|')) {
            throw 'Manifest hash target registration has missing, duplicate or unknown paths; expected 19 entries.'
        }
        $values = @{}
        foreach ($entry in $manifest.Entries) {
            $activeFile = [string]$entry.Path
            $sql = Get-MigrationText -Path (Join-Path $DatabaseRoot $activeFile)
            $files.Add($activeFile)
            $sourceHash = Get-MigrationHash -Sql $sql
            $definitionHash = Get-ModuleDefinitionHash -Sql $sql
            $definitionBytes = [Text.Encoding]::Unicode.GetByteCount($sql)
            $values[$entry.Path] = [pscustomobject]@{
                SourceChecksum = $sourceHash
                DefinitionChecksum = $definitionHash
                DefinitionBytes = $definitionBytes
            }
            $entryPattern = '(?s)\{\s*"Path"\s*:\s*"' + [regex]::Escape($entry.Path) + '".*?\}'
            $blocks = [regex]::Matches($manifestText, $entryPattern)
            if ($blocks.Count -ne 1) { throw "Cannot locate a single literal manifest entry: $($entry.Path)." }
            $block = $blocks[0]
            $known = @($Contract | Where-Object Path -CEQ $entry.Path)
            if ($entry.Path -ceq 'modules/permissions.sql') {
                if ($entry.Kind -cne 'Permissions' -or $null -ne $entry.ObjectName -or
                    $entry.DefinitionBytes -ne 0 -or $null -ne $entry.DefinitionChecksum) {
                    throw 'Permissions entry must not register an engine definition hash.'
                }
            }
            elseif ($known.Count -ne 1 -or $entry.ObjectName -cne ('dh.' + $known[0].Name) -or
                $entry.Kind -cne $known[0].Kind) {
                throw "Unknown manifest object/kind hash registration: $($entry.Path)."
            }
            $fields = @('SourceChecksum')
            if ($entry.Path -cne 'modules/permissions.sql') { $fields += @('DefinitionChecksum', 'DefinitionBytes') }
            foreach ($field in $fields) {
                $fieldPattern = '"' + $field + '"\s*:\s*(?:"(?<Value>[0-9A-F]{64})"|(?<Value>\d+))'
                $literalMatches = [regex]::Matches($block.Value, $fieldPattern)
                if ($literalMatches.Count -ne 1 -or ($field -ne 'DefinitionBytes' -and
                        $literalMatches[0].Groups['Value'].Value -cnotmatch '^[0-9A-F]{64}$')) {
                    throw "Unknown or duplicate $field literal: $($entry.Path)."
                }
                $parameters = @{
                    File = 'modules/manifest.json'
                    Text = $manifestText
                    Offset = $block.Index + $literalMatches[0].Groups['Value'].Index
                    Kind = 'Manifest' + $field
                    Source = $entry.Path
                    Observed = $literalMatches[0].Groups['Value'].Value
                    Expected = [string]$values[$entry.Path].$field
                }
                $targets.Add((New-ModuleHashTarget @parameters))
            }
        }
        $activeFile = 'verify-schema.sql'
        $catalog = Get-MigrationText -Path (Join-Path $DatabaseRoot $activeFile)
        $files.Add($activeFile)
        $modulePattern = "\('(?<Name>\w+)',\s*'(?<Kind>FN|P)',\s*(?<Bytes>\d+),\s*" +
        "0x(?<Definition>[0-9A-F]{64}),\s*'(?<Source>[0-9A-F]{64})'\)"
        $moduleBlock = Get-ModuleHashRows -Text $catalog -Table 'modules' -Pattern $modulePattern -Count 18
        $seen = @{}
        foreach ($row in $moduleBlock.Rows) {
            $name = $row.Groups['Name'].Value
            $known = @($Contract | Where-Object Name -CEQ $name)
            if ($known.Count -ne 1 -or $seen.ContainsKey($name) -or $row.Groups['Kind'].Value -cne $known[0].Kind) {
                throw "Missing, duplicate or unknown catalog module hash registration: $name."
            }
            $seen[$name] = $true
            $fields = [ordered]@{
                Source = 'SourceChecksum'
                Definition = 'DefinitionChecksum'
                Bytes = 'DefinitionBytes'
            }
            foreach ($field in $fields.Keys) {
                $parameters = @{
                    File = $activeFile
                    Text = $catalog
                    Offset = $moduleBlock.Offset + $row.Groups[$field].Index
                    Kind = 'Catalog' + $fields[$field]
                    Source = $known[0].Path
                    Observed = $row.Groups[$field].Value
                    Expected = [string]$values[$known[0].Path].($fields[$field])
                }
                $targets.Add((New-ModuleHashTarget @parameters))
            }
        }
        $migrationPattern = "\((?<Version>\d+),\s*'(?<Name>\d{3}_[a-z_]+\.sql)',\s*'(?<Value>[0-9A-F]{64})'\)"
        $migrationBlock = Get-ModuleHashRows -Text $catalog -Table 'migrations' -Pattern $migrationPattern -Count 4
        $seen = @{}
        foreach ($row in $migrationBlock.Rows) {
            $version = [int]$row.Groups['Version'].Value
            if ($version -notin 1..4 -or $seen.ContainsKey($version) -or
                $row.Groups['Name'].Value -cne $migrationNames[$version - 1]) {
                throw 'Missing, duplicate or unknown catalog migration hash registration.'
            }
            $seen[$version] = $true
            $source = 'migrations/' + $migrationNames[$version - 1]
            $sql = Get-MigrationText -Path (Join-Path $DatabaseRoot $source)
            $files.Add($source)
            $parameters = @{
                File = $activeFile
                Text = $catalog
                Offset = $migrationBlock.Offset + $row.Groups['Value'].Index
                Kind = 'CatalogMigrationChecksum'
                Source = $source
                Observed = $row.Groups['Value'].Value
                Expected = Get-MigrationHash -Sql $sql
            }
            $targets.Add((New-ModuleHashTarget @parameters))
        }
        $releasePattern = "r\.ManifestChecksum\s+COLLATE\s+Latin1_General_100_BIN2\s*=\s*'(?<Value>[0-9A-F]{64})'"
        $consumers = @(
            @{
                File = 'verify-schema.sql'
                Text = $catalog
                Pattern = $releasePattern
                Kind = 'CatalogManifestIdentity'
            }
            @{
                File = 'migrations/004_module_release.sql'
                Text = Get-MigrationText -Path (Join-Path $DatabaseRoot 'migrations/004_module_release.sql')
                Pattern = "VALUES\s*\(4,\s*'(?<Value>[0-9A-F]{64})'\)"
                Kind = 'ReleaseManifestIdentity'
            }
        )
        foreach ($consumer in $consumers) {
            $activeFile = $consumer.File
            $literalMatches = [regex]::Matches($consumer.Text, $consumer.Pattern)
            if ($literalMatches.Count -ne 1) {
                throw "Expected one registered manifest identity literal in $activeFile."
            }
            $parameters = @{
                File = $activeFile
                Text = $consumer.Text
                Offset = $literalMatches[0].Groups['Value'].Index
                Kind = $consumer.Kind
                Source = 'modules/manifest.json (raw UTF-8 bytes)'
                Observed = $literalMatches[0].Groups['Value'].Value
                Expected = $manifestHash
            }
            $targets.Add((New-ModuleHashTarget @parameters))
        }
        $activeFile = 'Module.Common.ps1'
        $moduleCommon = [IO.File]::ReadAllText((Join-Path $DatabaseRoot $activeFile))
        $files.Add($activeFile)
        $baselinePattern = "\`$index -eq 0 -and \`$hash -cne '(?<Value>[0-9A-F]{64})'"
        $literalMatches = [regex]::Matches($moduleCommon, $baselinePattern)
        if ($literalMatches.Count -ne 1) { throw 'Expected one immutable 001 source checksum guard.' }
        $baselineSql = Get-MigrationText -Path (Join-Path $DatabaseRoot 'migrations/001_initial.sql')
        $parameters = @{
            File = $activeFile
            Text = $moduleCommon
            Offset = $literalMatches[0].Groups['Value'].Index
            Kind = 'ImmutableMigrationChecksum'
            Source = 'migrations/001_initial.sql (CRLF->LF UTF-8)'
            Observed = $literalMatches[0].Groups['Value'].Value
            Expected = Get-MigrationHash -Sql $baselineSql
        }
        $targets.Add((New-ModuleHashTarget @parameters))
        # Fixed reviewed release: permissions source + 18*3 manifest + 18*3 catalog + 4 migrations + 2 release + 001.
        if ($targets.Count -ne 116) { throw "Hash target count $($targets.Count) differs from the registered 116." }
        [pscustomobject]@{
            Complete = $true
            CheckedFiles = @($files.ToArray())
            Targets = @($targets.ToArray())
            Issues = @($targets | Where-Object IsDrift)
        }
    }
    catch {
        [pscustomobject]@{
            Complete = $false
            CheckedFiles = @($files.ToArray())
            Targets = @($targets.ToArray())
            Issues = @([pscustomobject]@{
                    File = $activeFile
                    Line = $null
                    Kind = 'HashInspectionUnavailable'
                    Source = $null
                    Observed = $null
                    Expected = 'Complete readable source/manifest/catalog and the registered literal formats'
                    Message = $_.Exception.Message
                    Remediation = 'Restore the missing input or exact registered format; rerun before accepting hashes.'
                })
        }
    }
}
