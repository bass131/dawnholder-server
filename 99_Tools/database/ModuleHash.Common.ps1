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
        Offset = $Offset
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

function Read-ModuleHashInputs {
    param([string]$DatabaseRoot)
    $texts = @{}
    $files = New-Object 'Collections.Generic.List[string]'
    $issues = New-Object 'Collections.Generic.List[object]'
    $pending = New-Object 'Collections.Generic.Queue[string]'
    $pending.Enqueue('')
    # Only the root-level independent evidence tree is excluded, never tests-extra or a nested namesake.
    $excludedDirectories = @('tests')
    $extensions = @('.sql', '.ps1', '.psm1', '.psd1', '.json')
    $activeFile = '.'
    while ($pending.Count -gt 0) {
        $relativeDirectory = $pending.Dequeue()
        $activeFile = if ($relativeDirectory) { $relativeDirectory } else { '.' }
        try {
            $directory = Join-Path $DatabaseRoot $relativeDirectory
            $children = @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop | Sort-Object Name)
            foreach ($child in $children) {
                $relative = if ($relativeDirectory) { $relativeDirectory + '/' + $child.Name } else { $child.Name }
                if ($child.PSIsContainer -and $relative -iin $excludedDirectories) { continue }
                $isInput = -not $child.PSIsContainer -and $child.Extension -iin $extensions
                if (-not $child.PSIsContainer -and -not $isInput) { continue }
                $activeFile = $relative
                if (($child.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Hash input reparse point cannot be inspected without following a link: $relative."
                }
                if ($child.PSIsContainer) {
                    $pending.Enqueue($relative)
                    continue
                }
                $text = if ($child.Extension -ieq '.sql') {
                    Get-MigrationText -Path $child.FullName
                } else { [IO.File]::ReadAllText($child.FullName) }
                $texts[$relative] = $text
                $files.Add($relative)
            }
        }
        catch {
            $issues.Add([pscustomobject]@{
                    File = $activeFile
                    Line = $null
                    Kind = 'HashInspectionUnavailable'
                    Source = $null
                    Observed = $null
                    Expected = 'Readable product SQL, PowerShell and manifest inputs within DatabaseRoot'
                    Message = $_.Exception.Message
                    Remediation = 'Restore local readable inputs; do not follow external links or broaden tests exclusion.'
                })
        }
    }
    [pscustomobject]@{
        Complete = $issues.Count -eq 0
        Texts = $texts
        CheckedFiles = @($files.ToArray())
        Issues = @($issues.ToArray())
    }
}

function Test-ModuleHashLiteralRegistration {
    param(
        [hashtable]$Texts,
        [object[]]$Targets
    )
    $registrations = @{}
    foreach ($target in $Targets) {
        if ($target.Observed -cmatch '^[0-9A-F]{64}$') {
            $key = $target.File + ':' + $target.Offset
            $registrations[$key] = $target.Observed
        }
    }
    foreach ($file in @($Texts.Keys | Sort-Object)) {
        $text = $Texts[$file]
        # Include comments and strings. Values alone cannot authorize a new consumer location.
        $literals = [regex]::Matches($text, '(?i)(?<![0-9a-f])[0-9a-f]{64}(?![0-9a-f])')
        foreach ($literal in $literals) {
            $key = $file + ':' + $literal.Index
            if ($registrations.ContainsKey($key) -and $registrations[$key] -ceq $literal.Value) { continue }
            $line = 1 + [regex]::Matches($text.Substring(0, $literal.Index), "`n").Count
            [pscustomobject]@{
                File = $file
                Line = $line
                Offset = $literal.Index
                Kind = 'UnregisteredHashLiteral'
                Source = $null
                Observed = $literal.Value
                Expected = 'An explicitly reviewed source/definition/manifest identity consumer at this location'
                Message = 'Unregistered 64-hex literal; an existing value at another location is not a registration'
                Remediation = "Review ${file}:$line (offset $($literal.Index)); register its meaning and exact " +
                'File/Offset via Test-ModuleHashConsumers/New-ModuleHashTarget in ModuleHash.Common.ps1, ' +
                'update the fixed target contract, independent offline tests and MSSQL.md; do not adopt the value.'
            }
        }
    }
}

function Get-ModuleManifestHashTargets {
    param(
        [object]$Entry,
        [string]$Text,
        [object[]]$Contract,
        [object]$Values
    )
    $entryPattern = '(?s)\{\s*"Path"\s*:\s*"' + [regex]::Escape($Entry.Path) + '".*?\}'
    $blocks = [regex]::Matches($Text, $entryPattern)
    if ($blocks.Count -ne 1) { throw "Cannot locate a single literal manifest entry: $($Entry.Path)." }
    $block = $blocks[0]
    $known = @($Contract | Where-Object Path -CEQ $Entry.Path)
    if ($Entry.Path -ceq 'modules/permissions.sql') {
        if ($Entry.Kind -cne 'Permissions' -or $null -ne $Entry.ObjectName -or
            $Entry.DefinitionBytes -isnot [int] -or $Entry.DefinitionBytes -ne 0 -or
            $null -ne $Entry.DefinitionChecksum) {
            throw 'Permissions entry must not register an engine definition hash.'
        }
    }
    elseif ($known.Count -ne 1 -or $Entry.ObjectName -cne ('dh.' + $known[0].Name) -or
        $Entry.Kind -cne $known[0].Kind) {
        throw "Unknown manifest object/kind hash registration: $($Entry.Path)."
    }
    $fields = @('SourceChecksum')
    if ($Entry.Path -cne 'modules/permissions.sql') { $fields += @('DefinitionChecksum') }
    $fields += @('DefinitionBytes')
    foreach ($field in $fields) {
        # Capture the whole JSON value token. A decimal/exponent prefix is never an integer byte count.
        $fieldPattern = '"' + $field + '"\s*:\s*(?<Value>[^,}]+)(?=[,}])'
        $literalMatches = [regex]::Matches($block.Value, $fieldPattern)
        if ($literalMatches.Count -ne 1) { throw "Unknown or duplicate $field literal: $($Entry.Path)." }
        $group = $literalMatches[0].Groups['Value']
        $token = $group.Value.Trim()
        $offset = $block.Index + $group.Index + $group.Value.Length - $group.Value.TrimStart().Length
        if ($field -eq 'DefinitionBytes') {
            if ($token -cnotmatch '^(0|[1-9][0-9]*)$' -or $Entry.DefinitionBytes -isnot [int] -or
                $Entry.DefinitionBytes -lt 0) {
                throw "Unknown or duplicate DefinitionBytes literal: $($Entry.Path); token '$token' requires int32."
            }
            if ($Entry.Path -ceq 'modules/permissions.sql') { continue }
            $observed = $token
        }
        else {
            if ($token -cnotmatch '^"[0-9A-F]{64}"$') {
                throw "Unknown or duplicate $field literal: $($Entry.Path)."
            }
            $observed = $token.Substring(1, 64)
            $offset++
        }
        $parameters = @{
            File = 'modules/manifest.json'
            Text = $Text
            Offset = $offset
            Kind = 'Manifest' + $field
            Source = $Entry.Path
            Observed = $observed
            Expected = [string]$Values.$field
        }
        New-ModuleHashTarget @parameters
    }
}

function Test-ModuleHashConsumers {
    param(
        [string]$DatabaseRoot,
        [object[]]$Contract,
        [object]$Inputs
    )
    $targets = New-Object 'Collections.Generic.List[object]'
    $files = New-Object 'Collections.Generic.List[string]'
    $unregistered = @()
    $inputInspection = $Inputs
    if ($null -eq $inputInspection) { $inputInspection = Read-ModuleHashInputs -DatabaseRoot $DatabaseRoot }
    foreach ($file in $inputInspection.CheckedFiles) { $files.Add($file) }
    if (-not $inputInspection.Complete) {
        return [pscustomobject]@{
            Complete = $false
            LiteralInspectionComplete = $false
            CheckedFiles = @($files.ToArray())
            Targets = @()
            UnregisteredLiterals = @()
            Issues = $inputInspection.Issues
        }
    }
    $migrationNames = @(
        '001_initial.sql', '002_persistence_metadata.sql', '003_module_metadata.sql', '004_module_release.sql'
    )
    $activeFile = 'modules/manifest.json'
    try {
        # Hash raw decoded UTF-8 only after proving decoding did not remove/change any identity bytes.
        $manifestBytes = [IO.File]::ReadAllBytes((Join-Path $DatabaseRoot $activeFile))
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
            $sourceHash = Get-MigrationHash -Sql $sql
            $definitionHash = Get-ModuleDefinitionHash -Sql $sql
            $definitionBytes = [Text.Encoding]::Unicode.GetByteCount($sql)
            $values[$entry.Path] = [pscustomobject]@{
                SourceChecksum = $sourceHash
                DefinitionChecksum = $definitionHash
                DefinitionBytes = $definitionBytes
            }
            $activeFile = 'modules/manifest.json'
            $entryTargets = @(Get-ModuleManifestHashTargets -Entry $entry -Text $manifestText -Contract $Contract `
                    -Values $values[$entry.Path])
            foreach ($target in $entryTargets) { $targets.Add($target) }
        }
        $activeFile = 'verify-schema.sql'
        $catalog = Get-MigrationText -Path (Join-Path $DatabaseRoot $activeFile)
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
            $activeFile = $source
            $sql = Get-MigrationText -Path (Join-Path $DatabaseRoot $source)
            $activeFile = 'verify-schema.sql'
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
        $activeFile = 'migrations/004_module_release.sql'
        $releaseText = Get-MigrationText -Path (Join-Path $DatabaseRoot $activeFile)
        $consumers = @(
            @{
                File = 'verify-schema.sql'
                Text = $catalog
                Pattern = $releasePattern
                Kind = 'CatalogManifestIdentity'
            }
            @{
                File = 'migrations/004_module_release.sql'
                Text = $releaseText
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
        $baselinePattern = "\`$index -eq 0 -and \`$hash -cne '(?<Value>[0-9A-F]{64})'"
        $literalMatches = [regex]::Matches($moduleCommon, $baselinePattern)
        if ($literalMatches.Count -ne 1) { throw 'Expected one immutable 001 source checksum guard.' }
        $activeFile = 'migrations/001_initial.sql'
        $baselineSql = Get-MigrationText -Path (Join-Path $DatabaseRoot $activeFile)
        $activeFile = 'Module.Common.ps1'
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
        $unregistered = @(Test-ModuleHashLiteralRegistration -Texts $inputInspection.Texts -Targets $targets.ToArray())
        [pscustomobject]@{
            Complete = $true
            LiteralInspectionComplete = $true
            CheckedFiles = @($files.ToArray())
            Targets = @($targets.ToArray())
            UnregisteredLiterals = $unregistered
            Issues = @($targets | Where-Object IsDrift) + $unregistered
        }
    }
    catch {
        [pscustomobject]@{
            Complete = $false
            LiteralInspectionComplete = $false
            CheckedFiles = @($files.ToArray())
            Targets = @($targets.ToArray())
            UnregisteredLiterals = $unregistered
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
