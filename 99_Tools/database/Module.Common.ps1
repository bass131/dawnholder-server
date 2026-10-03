Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import definitions in a child scope: the executable entry point's parameters must not overwrite caller variables.
$moduleStructureDefinitions = & {
    . (Join-Path $PSScriptRoot 'Test-ModuleStructure.ps1')
    foreach ($name in @('Get-ModuleStructureContract', 'Get-ModuleExecutableText', 'Test-ModuleStructure')) {
        Get-Command -Name $name -CommandType Function
    }
}
foreach ($definition in $moduleStructureDefinitions) {
    Set-Item -Path ('Function:' + $definition.Name) -Value $definition.ScriptBlock
}

function Get-ModuleDefinitionHash([string]$Sql) {
    # Reviewed source, UTF-16LE without BOM. Never derive the expected hash from the database.
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::Unicode.GetBytes($Sql)))).Replace('-', '')
    } finally {
        $sha.Dispose()
    }
}

function Assert-ModuleManifestFields($Value, [string[]]$Expected, [string]$Location) {
    if ($null -eq $Value -or $Value -isnot [pscustomobject]) {
        throw "Expected an object at $Location."
    }
    $actual = @($Value.PSObject.Properties.Name | Sort-Object)
    if (($actual -join '|') -cne (($Expected | Sort-Object) -join '|')) {
        throw "Unexpected manifest fields at $Location; use the reviewed bundle format."
    }
}

function Read-ModuleBundle([string]$DatabaseRoot) {
    if ([string]::IsNullOrWhiteSpace($DatabaseRoot)) { $DatabaseRoot = $PSScriptRoot }
    $DatabaseRoot = [IO.Path]::GetFullPath($DatabaseRoot).TrimEnd('\', '/')
    $manifestPath = Join-Path $DatabaseRoot 'modules/manifest.json'
    $bytes = [IO.File]::ReadAllBytes($manifestPath)
    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    $text = $utf8.GetString($bytes)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    if ($hasBom -or $text.Contains("`r") -or -not $text.EndsWith("`n")) {
        throw 'Module manifest must be UTF-8 without BOM, LF, and a final newline; do not normalize its identity.'
    }
    $manifest = $text | ConvertFrom-Json
    Assert-ModuleManifestFields -Value $manifest -Location $manifestPath -Expected @(
        'FormatVersion', 'ReleaseVersion', 'SourceEncoding', 'DefinitionEncoding', 'Entries'
    )
    if ($manifest.FormatVersion -isnot [int] -or $manifest.FormatVersion -ne 1 -or
        $manifest.ReleaseVersion -isnot [int] -or $manifest.ReleaseVersion -ne 4 -or
        $manifest.SourceEncoding -cne 'UTF-8; no BOM; CRLF->LF' -or
        $manifest.DefinitionEncoding -cne 'UTF-16LE; no BOM; LF; actual CR removed' -or
        $manifest.Entries -isnot [Array]) {
        throw 'Module manifest version/encoding differs from the reviewed current release.'
    }
    $structure = Test-ModuleStructure -DatabaseRoot $DatabaseRoot
    if ($structure.Status -cne 'compliant') {
        $detail = $structure.Issues | ConvertTo-Json -Depth 6 -Compress
        throw "Module source structure $($structure.Status): $detail"
    }
    $contract = @(Get-ModuleStructureContract)
    $expectedPaths = @($contract.Path) + @('modules/permissions.sql')
    $actualPaths = @(Get-ChildItem -LiteralPath (Join-Path $DatabaseRoot 'modules') -Recurse -File -Filter '*.sql' |
            ForEach-Object { $_.FullName.Substring($DatabaseRoot.TrimEnd('\', '/').Length + 1).Replace('\', '/') })
    if ((($actualPaths | Sort-Object) -join '|') -cne (($expectedPaths | Sort-Object) -join '|') -or
        $manifest.Entries.Count -ne $expectedPaths.Count) {
        throw 'Module SQL file set differs from the exact reviewed bundle; remove extras or restore missing files.'
    }
    $seen = @{}
    $modules = @()
    $permissions = $null
    foreach ($entry in $manifest.Entries) {
        Assert-ModuleManifestFields -Value $entry -Location ([string]$entry.Path) -Expected @(
            'Path', 'ObjectName', 'Kind', 'DependsOn', 'SourceChecksum', 'DefinitionBytes', 'DefinitionChecksum'
        )
        if ($entry.Path -isnot [string] -or $entry.Path -cnotin $expectedPaths -or
            $entry.SourceChecksum -cnotmatch '^[0-9A-F]{64}$' -or $entry.DependsOn -isnot [Array]) {
            throw 'Invalid module path/checksum/dependency list; use the reviewed exact paths.'
        }
        $sql = Get-MigrationText -Path (Join-Path $DatabaseRoot $entry.Path)
        if ($sql.Contains("`r") -or (Get-MigrationHash -Sql $sql) -cne $entry.SourceChecksum) {
            throw (
                "Module source checksum mismatch: $($entry.Path); update the reviewed bundle and declaration together."
            )
        }
        $executable = Get-ModuleExecutableText -Text $sql
        if ($executable -match '(?im)^\s*GO(?:\s|$)') {
            throw "Module files are one batch and cannot contain GO: $($entry.Path)."
        }
        $known = @($contract | Where-Object Path -CEQ $entry.Path)
        if ($entry.Path -ceq 'modules/permissions.sql') {
            $expectedDependencies = @($contract | Where-Object Public | ForEach-Object { 'dh.' + $_.Name })
            if ($null -ne $permissions -or $modules.Count -ne $contract.Count -or
                $entry.Kind -cne 'Permissions' -or $null -ne $entry.ObjectName -or
                $entry.DefinitionBytes -isnot [int] -or $entry.DefinitionBytes -ne 0 -or
                $null -ne $entry.DefinitionChecksum) {
                throw 'Permissions must be the final single bundle entry without an engine definition.'
            }
            $expectedGrants = foreach ($public in @($contract | Where-Object Public)) {
                $role = if ($public.Name -cin @('ReadAdmission', 'AcquireAndLoad', 'WriteSafeCheckpoint',
                        'ReleaseRuntime', 'ResolveRuntimeOperation')) { 'dh_runtime' } else { 'dh_recovery' }
                'GRANTEXECUTEONOBJECT::dh.' + $public.Name + 'TO' + $role + ';'
            }
            if (($executable -replace '\s', '') -cne ($expectedGrants -join '')) {
                throw 'Permissions source must contain only the nine reviewed individual EXECUTE grants.'
            }
            $permissions = [pscustomobject]@{
                Path = $entry.Path
                Sql = $sql
            }
        } else {
            if ($known.Count -ne 1 -or $entry.ObjectName -cne ('dh.' + $known[0].Name) -or
                $entry.Kind -cne $known[0].Kind -or $seen.ContainsKey($entry.ObjectName) -or
                $entry.DefinitionBytes -isnot [int] -or $entry.DefinitionBytes -ne $sql.Length * 2 -or
                $entry.DefinitionChecksum -cnotmatch '^[0-9A-F]{64}$' -or
                (Get-ModuleDefinitionHash -Sql $sql) -cne $entry.DefinitionChecksum) {
                throw "Module object/kind/expected definition mismatch: $($entry.Path)."
            }
            $ddlKind = if ($entry.Kind -ceq 'FN') { 'FUNCTION' } else { 'PROCEDURE' }
            $header = '^CREATE OR ALTER ' + $ddlKind + ' ' + [regex]::Escape($entry.ObjectName) + '\b'
            if ($sql -cnotmatch $header) {
                throw "Expected one CREATE OR ALTER $ddlKind batch at $($entry.Path)."
            }
            $expectedDependencies = @($known[0].RequiredCalls | ForEach-Object { 'dh.' + $_ })
            foreach ($dependency in $entry.DependsOn) {
                if ($dependency -isnot [string] -or -not $seen.ContainsKey($dependency)) {
                    throw "Dependency must precede $($entry.ObjectName): $dependency."
                }
            }
            $seen[$entry.ObjectName] = $true
            $modules += [pscustomobject]@{
                Path = $entry.Path
                ObjectName = $entry.ObjectName
                Kind = $entry.Kind
                SourceChecksum = $entry.SourceChecksum
                DefinitionBytes = $entry.DefinitionBytes
                DefinitionChecksum = $entry.DefinitionChecksum
                Sql = $sql
            }
        }
        if ((($entry.DependsOn | Sort-Object) -join '|') -cne (($expectedDependencies | Sort-Object) -join '|')) {
            throw (
                "Dependency contract mismatch: $($entry.Path); declare the operation's actual helper responsibilities."
            )
        }
    }
    if ($modules.Count -ne $contract.Count -or $null -eq $permissions) {
        throw 'Incomplete module bundle; no module may be silently omitted.'
    }
    return [pscustomobject]@{
        ReleaseVersion = $manifest.ReleaseVersion
        ManifestChecksum = Get-MigrationHash -Sql $text
        Modules = $modules
        Permissions = $permissions
    }
}

function Get-DatabaseMigrationSources(
    [ValidateSet('Complete', 'Baseline001')][string]$Phase = 'Complete',
    [string]$DatabaseRoot
) {
    if ([string]::IsNullOrWhiteSpace($DatabaseRoot)) { $DatabaseRoot = $PSScriptRoot }
    $names = @('001_initial.sql', '002_persistence_metadata.sql',
        '003_module_metadata.sql', '004_module_release.sql')
    $files = @(Get-ChildItem -LiteralPath (Join-Path $DatabaseRoot 'migrations') -File -Filter '*.sql' |
            Sort-Object Name)
    if ($Phase -eq 'Baseline001') {
        $names = @('001_initial.sql')
        $files = @($files | Where-Object Name -CEQ $names[0])
    }
    if (($files.Name -join '|') -cne ($names -join '|')) {
        throw 'Migration files must be the reviewed contiguous version/name set; reject holes, duplicates and extras.'
    }
    for ($index = 0; $index -lt $names.Count; $index++) {
        $sql = Get-MigrationText -Path $files[$index].FullName
        $hash = Get-MigrationHash -Sql $sql
        if ($index -eq 0 -and $hash -cne 'F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC') {
            throw 'Immutable 001 source checksum drift; restore the reviewed baseline.'
        }
        [pscustomobject]@{
            Version = $index + 1
            Name = $names[$index]
            Checksum = $hash
            Sql = $sql
        }
    }
}

function Assert-DatabaseMigrationHistory($History, $Sources) {
    $rows = @($History)
    if ($rows.Count -gt $Sources.Count) {
        throw 'Database has unknown/newer migrations; use the matching tool revision without downgrading.'
    }
    for ($index = 0; $index -lt $rows.Count; $index++) {
        $row = $rows[$index]
        $expected = $Sources[$index]
        if ($row.Version -ne $expected.Version -or $row.Name -cne $expected.Name -or
            $row.Checksum -cne $expected.Checksum) {
            throw (
                'Migration history has a hole, unknown name/version or checksum drift; do not rewrite applied history.'
            )
        }
    }
}

function ConvertFrom-DatabaseJsonRows([string]$Json) {
    if ([string]::IsNullOrWhiteSpace($Json) -or $Json -notmatch '^\s*\[') {
        throw 'Expected the complete database JSON row array, including [] for no rows.'
    }
    # Windows PowerShell 5.1 emits an entire JSON array as one pipeline object.
    # Enumerate explicitly so zero/one/many rows have identical semantics in 5.1 and newer hosts.
    $rows = $Json | ConvertFrom-Json
    foreach ($row in $rows) { $row }
}

function Get-DatabaseModuleState($Connection, $Transaction) {
    # A top-level FOR JSON can split long output across provider rows; scalar subqueries retain the full JSON value.
    # Empty metadata on first installation is normalized in SQL; the JSON reader still rejects malformed values.
    $recordedText = Invoke-DbScalar -Connection $Connection -Transaction $Transaction -Sql @'
SELECT ISNULL((
    SELECT ObjectName, Kind, SourceChecksum, DefinitionBytes, DefinitionChecksum
    FROM dh.ModuleDefinition ORDER BY ObjectName FOR JSON PATH
), N'[]') AS ModuleRows;
'@
    $actualText = Invoke-DbScalar -Connection $Connection -Transaction $Transaction -Sql @'
SELECT ISNULL((
    SELECT N'dh.' + o.name AS ObjectName, o.type AS Kind,
        DATALENGTH(REPLACE(sm.definition, NCHAR(13), N'')) AS DefinitionBytes,
        CONVERT(char(64), HASHBYTES('SHA2_256', REPLACE(sm.definition, NCHAR(13), N'')), 2) AS DefinitionChecksum,
        sm.execute_as_principal_id AS ExecuteAsPrincipalId, sm.uses_ansi_nulls AS AnsiNulls,
        sm.uses_quoted_identifier AS QuotedIdentifier, sm.is_schema_bound AS SchemaBound,
        COALESCE(o.principal_id, s.principal_id) AS OwnerPrincipalId,
        DATABASE_PRINCIPAL_ID(N'dbo') AS ExpectedOwnerPrincipalId
    FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id
    LEFT JOIN sys.sql_modules sm ON sm.object_id = o.object_id
    WHERE s.name = N'dh' AND o.type IN ('P', 'FN', 'IF', 'TF', 'FS', 'FT', 'PC')
    ORDER BY o.name FOR JSON PATH, INCLUDE_NULL_VALUES
), N'[]') AS ModuleRows;
'@
    return [pscustomobject]@{
        Recorded = @(ConvertFrom-DatabaseJsonRows -Json $recordedText)
        Actual = @(ConvertFrom-DatabaseJsonRows -Json $actualText)
    }
}

function Assert-DatabaseModuleState(
    $State,
    $Bundle,
    [bool]$AllowFirstInstall = $false,
    [bool]$RequireCurrentSource = $false
) {
    if ($AllowFirstInstall -and $State.Recorded.Count -eq 0 -and $State.Actual.Count -eq 0) { return }
    if ($State.Recorded.Count -ne $Bundle.Modules.Count -or $State.Actual.Count -ne $Bundle.Modules.Count) {
        throw 'Existing module registration/object set is incomplete or unexpected; refuse adoption or overwrite.'
    }
    $expectedNames = @($Bundle.Modules.ObjectName | Sort-Object)
    foreach ($rows in @($State.Recorded, $State.Actual)) {
        if ((($rows.ObjectName | Sort-Object) -join '|') -cne ($expectedNames -join '|')) {
            throw 'Unregistered/missing/unexpected module; restore or review it before deploying a new declaration.'
        }
    }
    foreach ($expected in $Bundle.Modules) {
        $recorded = @($State.Recorded | Where-Object ObjectName -CEQ $expected.ObjectName)[0]
        $actual = @($State.Actual | Where-Object ObjectName -CEQ $expected.ObjectName)[0]
        if ($recorded.Kind.TrimEnd() -cne $expected.Kind -or $actual.Kind.TrimEnd() -cne $expected.Kind -or
            $recorded.SourceChecksum -cnotmatch '^[0-9A-F]{64}$' -or
            $recorded.DefinitionChecksum -cnotmatch '^[0-9A-F]{64}$' -or $recorded.DefinitionBytes -le 0 -or
            $actual.DefinitionBytes -ne $recorded.DefinitionBytes -or
            $actual.DefinitionChecksum -cne $recorded.DefinitionChecksum -or
            $null -ne $actual.ExecuteAsPrincipalId -or -not $actual.AnsiNulls -or -not $actual.QuotedIdentifier -or
            ($expected.Kind -ceq 'FN' -and -not $actual.SchemaBound) -or
            $actual.OwnerPrincipalId -ne $actual.ExpectedOwnerPrincipalId) {
            throw (
                "Registered/actual module drift: $($expected.ObjectName); refuse overwrite, including unchanged source."
            )
        }
        if ($recorded.SourceChecksum -ceq $expected.SourceChecksum -and
            ($recorded.DefinitionBytes -ne $expected.DefinitionBytes -or
            $recorded.DefinitionChecksum -cne $expected.DefinitionChecksum)) {
            throw "Unchanged source has different reviewed definition metadata: $($expected.ObjectName)."
        }
        if ($RequireCurrentSource -and $recorded.SourceChecksum -cne $expected.SourceChecksum) {
            throw "Already-declared release has different source registration: $($expected.ObjectName); refuse repair."
        }
    }
}

function Invoke-ModuleBundle(
    $Connection,
    $Transaction,
    $Bundle,
    [bool]$AllowFirstInstall = $false,
    [bool]$ReleaseAlreadyApplied = $true
) {
    if ($null -eq $Transaction) { throw 'Module deployment requires the caller-owned migration transaction.' }
    $releaseText = Invoke-DbScalar -Connection $Connection -Transaction $Transaction -Sql @'
SELECT ISNULL((
    SELECT r.Version, r.ManifestChecksum, s.Version AS SchemaVersion
    FROM dh.ModuleRelease r LEFT JOIN dh.SchemaVersion s ON s.Version = r.Version
    ORDER BY r.Version FOR JSON PATH, INCLUDE_NULL_VALUES
), N'[]') AS ReleaseRows;
'@
    $releases = @(ConvertFrom-DatabaseJsonRows -Json $releaseText)
    if ($releases.Count -eq 0 -or $releases[-1].Version -ne $Bundle.ReleaseVersion -or
        $releases[-1].ManifestChecksum -cne $Bundle.ManifestChecksum) {
        throw (
            'Current bundle differs from its migration declaration; a different bundle needs a new release version.'
        )
    }
    $previousVersion = 3
    foreach ($release in $releases) {
        if ($release.Version -le $previousVersion -or $release.SchemaVersion -ne $release.Version -or
            $release.ManifestChecksum -cnotmatch '^[0-9A-F]{64}$') {
            throw 'Module release history has an unknown, missing-schema or malformed declaration; refuse deployment.'
        }
        $previousVersion = $release.Version
    }
    $state = Get-DatabaseModuleState -Connection $Connection -Transaction $Transaction
    Assert-DatabaseModuleState -State $state -Bundle $Bundle -AllowFirstInstall $AllowFirstInstall `
        -RequireCurrentSource $ReleaseAlreadyApplied
    [void](Invoke-DbNonQuery -Connection $Connection -Transaction $Transaction -Sql @'
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
'@)
    foreach ($module in $Bundle.Modules) {
        $previous = @($state.Recorded | Where-Object ObjectName -CEQ $module.ObjectName)
        if ($previous.Count -eq 0 -or $previous[0].SourceChecksum -cne $module.SourceChecksum) {
            [void](Invoke-DbNonQuery -Connection $Connection -Transaction $Transaction -Sql $module.Sql)
            Write-Output "Module $($module.ObjectName) applied."
        } else {
            Write-Output "Module $($module.ObjectName) unchanged; actual definition verified."
        }
    }
    [void](Invoke-DbNonQuery -Connection $Connection -Transaction $Transaction -Sql $Bundle.Permissions.Sql)
    # Check the new engine definitions against source-derived expectations before writing those expectations.
    $newState = Get-DatabaseModuleState -Connection $Connection -Transaction $Transaction
    $expectedRecords = @($Bundle.Modules | Select-Object ObjectName, Kind, SourceChecksum,
        DefinitionBytes, DefinitionChecksum)
    Assert-DatabaseModuleState -State ([pscustomobject]@{
            Recorded = $expectedRecords
            Actual = $newState.Actual
        }) -Bundle $Bundle
    foreach ($module in $Bundle.Modules) {
        [void](Invoke-DbNonQuery -Connection $Connection -Transaction $Transaction -Sql @'
UPDATE dh.ModuleDefinition SET Kind = @kind, SourceChecksum = @source,
    DefinitionBytes = @bytes, DefinitionChecksum = @definition WHERE ObjectName = @name;
IF @@ROWCOUNT = 0
    INSERT dh.ModuleDefinition(ObjectName, Kind, SourceChecksum, DefinitionBytes, DefinitionChecksum)
    VALUES(@name, @kind, @source, @bytes, @definition);
'@ -Parameters @{
                name = $module.ObjectName
                kind = $module.Kind
                source = $module.SourceChecksum
                bytes = [int]$module.DefinitionBytes
                definition = $module.DefinitionChecksum
            })
    }
}
