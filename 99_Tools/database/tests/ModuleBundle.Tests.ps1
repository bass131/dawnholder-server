[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of reviewed bundle, migration source and history validation.
# Product functions are dot-sourced as definitions only; fixtures are byte copies below WorkRoot.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'module-bundle'
. (Join-Path $script:ToolRoot 'Database.Common.ps1')
Set-OfflineStubs

function Set-ManifestEntryValue {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$EntryPath,
        [Parameter(Mandatory)][string]$Property,
        $Value
    )
    $manifest = Read-FixtureManifest -Root $Root
    $entry = @($manifest.Entries | Where-Object Path -CEQ $EntryPath)[0]
    $entry.$Property = $Value
    Write-FixtureManifest -Root $Root -Manifest $manifest
}

function Move-ManifestEntry {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$EntryPath,
        [Parameter(Mandatory)][string]$BeforePath
    )
    $manifest = Read-FixtureManifest -Root $Root
    $moving = @($manifest.Entries | Where-Object Path -CEQ $EntryPath)[0]
    $rest = @($manifest.Entries | Where-Object Path -CNE $EntryPath)
    $ordered = foreach ($entry in $rest) {
        if ($entry.Path -ceq $BeforePath) { $moving }
        $entry
    }
    $manifest.Entries = @($ordered)
    Write-FixtureManifest -Root $Root -Manifest $manifest
}

$manifestPath = Join-Path $script:ToolRoot 'modules/manifest.json'
$progress = 'modules/procedures/internal/serialize_progress.sql'
$admission = 'modules/procedures/read_admission.sql'
$acquire = 'modules/procedures/acquire_and_load.sql'
$permissions = 'modules/permissions.sql'

# ---- Positive control on the reviewed tree and independent recomputation of every declared value.
$real = Read-ModuleBundle -DatabaseRoot $script:ToolRoot
$manifest = Read-FixtureManifest -Root $script:ToolRoot
$manifestHash = Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($manifestPath))
$declaration = Read-FixtureText -Path (Join-Path $script:ToolRoot 'migrations/004_module_release.sql')
Assert-Equal -Name 'reviewed bundle -> 18 modules' -Expected 18 -Actual @($real.Modules).Count
Assert-Equal -Name 'reviewed bundle -> release version 4' -Expected 4 -Actual $real.ReleaseVersion
Assert-Equal -Name 'reviewed bundle -> manifest checksum equals raw file SHA256' -Expected $manifestHash -Actual $real.ManifestChecksum
Assert-True -Name '004 declaration carries the raw manifest SHA256 (no self-hash cycle)' `
    -Condition ($declaration -cmatch "VALUES\(4, '$manifestHash'\)" -and
    @($manifest.Entries | Where-Object { $_.Path -cmatch 'manifest|004_' }).Count -eq 0)
$kinds = @($manifest.Entries | ForEach-Object { if ($_.Kind -ceq 'FN') { 'codec' } elseif ($_.Path -cmatch '/internal/') { 'helper' } `
            elseif ($_.Kind -ceq 'Permissions') { 'permissions' } else { 'public' } })
Assert-Equal -Name 'manifest order codec -> 8 helpers -> 9 public -> permissions' `
    -Expected ((@('codec') + @('helper') * 8 + @('public') * 9 + @('permissions')) -join ',') -Actual ($kinds -join ',')
$valueMismatches = @()
$dependencyMismatches = @()
foreach ($entry in $manifest.Entries) {
    $values = Get-ExpectedModuleValues -Path (Join-Path $script:ToolRoot $entry.Path)
    if ($values.SourceChecksum -cne $entry.SourceChecksum) { $valueMismatches += "$($entry.Path) source" }
    if ($null -ne $entry.ObjectName -and ($values.DefinitionBytes -ne $entry.DefinitionBytes -or
            $values.DefinitionChecksum -cne $entry.DefinitionChecksum)) {
        $valueMismatches += "$($entry.Path) definition"
    }
    if ($null -ne $entry.ObjectName) {
        # Independent call extraction: drop line comments, then collect direct qualified EXEC targets.
        $code = ((Read-FixtureText -Path (Join-Path $script:ToolRoot $entry.Path)) -split "`n" |
                ForEach-Object { ($_ -replace '--.*$', '') }) -join "`n"
        $calls = @([regex]::Matches($code, '(?i)\bEXEC(?:UTE)?\s+(?:@\w+\s*=\s*)?dh\.(\w+)') |
                ForEach-Object { 'dh.' + $_.Groups[1].Value } | Sort-Object -Unique)
        if (($calls -join '|') -cne ((@($entry.DependsOn) | Sort-Object) -join '|')) {
            $dependencyMismatches += "$($entry.Path): calls=$($calls -join ',') declared=$(@($entry.DependsOn) -join ',')"
        }
    }
}
Assert-True -Name 'every source/definition value is re-derived from source (UTF-8 LF / UTF-16LE)' `
    -Condition ($valueMismatches.Count -eq 0) -Detail ($valueMismatches -join '; ')
Assert-True -Name 'every declared dependency set equals the module''s actual direct calls' `
    -Condition ($dependencyMismatches.Count -eq 0) -Detail ($dependencyMismatches -join '; ')
$catalog = Read-FixtureText -Path (Join-Path $script:ToolRoot 'verify-schema.sql')
$catalogRows = @([regex]::Matches($catalog, "\('(\w+)', '(FN|P)', (\d+), 0x([0-9A-F]{64}),\s*'([0-9A-F]{64})'\)") | ForEach-Object {
        'dh.{0}|{1}|{2}|{3}|{4}' -f $_.Groups[1].Value, $_.Groups[2].Value, $_.Groups[3].Value, $_.Groups[4].Value, $_.Groups[5].Value
    })
$manifestRows = @($manifest.Entries | Where-Object ObjectName | ForEach-Object {
        '{0}|{1}|{2}|{3}|{4}' -f $_.ObjectName, $_.Kind, $_.DefinitionBytes, $_.DefinitionChecksum, $_.SourceChecksum
    })
Assert-Equal -Name 'strict catalog module expectations equal manifest (18 rows)' `
    -Expected (($manifestRows | Sort-Object) -join ';') -Actual (($catalogRows | Sort-Object) -join ';')
# The catalog runs after 004 is recorded; its release literal must name the same reviewed bundle as 004.
$releasePattern = "ModuleRelease r JOIN dh\.SchemaVersion s[\s\S]*?ManifestChecksum[^']*'([0-9A-F]{64})'"
$releaseLiteral = [regex]::Match($catalog, $releasePattern).Groups[1].Value
Assert-Equal -Name 'strict catalog release declaration expects the raw manifest SHA256 declared by 004' `
    -Expected $manifestHash -Actual $releaseLiteral
$permissionsText = Read-FixtureText -Path (Join-Path $script:ToolRoot $permissions)
$grants = @([regex]::Matches($permissionsText, '(?m)^GRANT EXECUTE ON OBJECT::dh\.(\w+) TO (dh_runtime|dh_recovery);$') |
        ForEach-Object { $_.Groups[1].Value + '>' + $_.Groups[2].Value })
Assert-Equal -Name 'permissions: exactly nine individual public EXECUTE grants (runtime5/recovery4)' `
    -Expected 'ReadAdmission>dh_runtime,AcquireAndLoad>dh_runtime,WriteSafeCheckpoint>dh_runtime,ReleaseRuntime>dh_runtime,ResolveRuntimeOperation>dh_runtime,InspectRecovery>dh_recovery,RecoverAndLoad>dh_recovery,ReleaseRecovery>dh_recovery,ResolveRecoveryOperation>dh_recovery' `
    -Actual ($grants -join ',')

# ---- Manifest identity and shape.
$root = New-DatabaseFixture -Name 'manifest-bom'
$bytes = [IO.File]::ReadAllBytes((Join-Path $root 'modules/manifest.json'))
[IO.File]::WriteAllBytes((Join-Path $root 'modules/manifest.json'), [byte[]](@(0xEF, 0xBB, 0xBF) + $bytes))
Assert-Throws -Name 'manifest with UTF-8 BOM rejected' -Pattern 'UTF-8 without BOM, LF, and a final newline' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'manifest-crlf'
Write-FixtureText -Path (Join-Path $root 'modules/manifest.json') `
    -Text ((Read-FixtureText -Path (Join-Path $root 'modules/manifest.json')).Replace("`n", "`r`n"))
Assert-Throws -Name 'manifest with CRLF rejected' -Pattern 'UTF-8 without BOM, LF, and a final newline' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'manifest-no-final-newline'
Write-FixtureText -Path (Join-Path $root 'modules/manifest.json') `
    -Text (Read-FixtureText -Path (Join-Path $root 'modules/manifest.json')).TrimEnd("`n")
Assert-Throws -Name 'manifest without final newline rejected' -Pattern 'UTF-8 without BOM, LF, and a final newline' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'manifest-extra-field'
$fixture = Read-FixtureManifest -Root $root
$fixture | Add-Member -MemberType NoteProperty -Name Approved -Value $true
Write-FixtureManifest -Root $root -Manifest $fixture
Assert-Throws -Name 'manifest with an extra top-level field rejected' -Pattern '^Unexpected manifest fields at ' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

foreach ($case in @(
        @{ Name = 'release version 5'; Property = 'ReleaseVersion'; Value = 5 },
        @{ Name = 'release version as string'; Property = 'ReleaseVersion'; Value = '4' },
        @{ Name = 'format version 2'; Property = 'FormatVersion'; Value = 2 },
        @{ Name = 'source encoding text changed'; Property = 'SourceEncoding'; Value = 'UTF-8' }
    )) {
    $root = New-DatabaseFixture -Name ('manifest-' + ($case.Name -replace '\W+', '-'))
    $fixture = Read-FixtureManifest -Root $root
    $fixture.($case.Property) = $case.Value
    Write-FixtureManifest -Root $root -Manifest $fixture
    Assert-Throws -Name ("manifest $($case.Name) rejected") -Pattern 'version/encoding differs from the reviewed current release' `
        -Action { Read-ModuleBundle -DatabaseRoot $root }
}

$root = New-DatabaseFixture -Name 'entry-extra-field'
$fixture = Read-FixtureManifest -Root $root
$fixture.Entries[3] | Add-Member -MemberType NoteProperty -Name Approved -Value $true
Write-FixtureManifest -Root $root -Manifest $fixture
Assert-Throws -Name 'entry with an extra field rejected' -Pattern '^Unexpected manifest fields at modules/' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

foreach ($case in @(
        @{ Name = 'path escape through ..'; Value = 'modules/procedures/../procedures/read_admission.sql' },
        @{ Name = 'absolute path'; Value = 'C:/Windows/win.ini' },
        @{ Name = 'backslash path'; Value = 'modules\procedures\read_admission.sql' }
    )) {
    $root = New-DatabaseFixture -Name ('entry-' + ($case.Name -replace '\W+', '-'))
    Set-ManifestEntryValue -Root $root -EntryPath $admission -Property Path -Value $case.Value
    Assert-Throws -Name ("entry $($case.Name) rejected") -Pattern '^Invalid module path/checksum/dependency list' `
        -Action { Read-ModuleBundle -DatabaseRoot $root }
}

$root = New-DatabaseFixture -Name 'entry-lowercase-checksum'
Set-ManifestEntryValue -Root $root -EntryPath $admission -Property SourceChecksum `
    -Value (@($manifest.Entries | Where-Object Path -CEQ $admission)[0].SourceChecksum.ToLowerInvariant())
Assert-Throws -Name 'lowercase source checksum rejected' -Pattern '^Invalid module path/checksum/dependency list' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'entry-count-short'
$fixture = Read-FixtureManifest -Root $root
$fixture.Entries = @($fixture.Entries | Where-Object Path -CNE $progress)
Write-FixtureManifest -Root $root -Manifest $fixture
Assert-Throws -Name 'manifest missing one module entry rejected' -Pattern '^Module SQL file set differs from the exact reviewed bundle' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'entry-duplicate'
$fixture = Read-FixtureManifest -Root $root
$lock = @($fixture.Entries | Where-Object Path -CEQ 'modules/procedures/internal/lock_and_read_authority.sql')[0]
$fixture.Entries = @($fixture.Entries | ForEach-Object { if ($_.Path -ceq $progress) { $lock } else { $_ } })
Write-FixtureManifest -Root $root -Manifest $fixture
Assert-Throws -Name 'duplicate entry replacing another module rejected' `
    -Pattern '^Module object/kind/expected definition mismatch: modules/procedures/internal/lock_and_read_authority\.sql' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

# ---- File set and source identity.
$root = New-DatabaseFixture -Name 'extra-sql-file'
Write-FixtureText -Path (Join-Path $root 'modules/procedures/internal/notes.sql') -Text "-- not a registered module`n"
Assert-Throws -Name 'unregistered .sql file under modules rejected' -Pattern '^Module source structure violation' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'extra-non-sql-file'
Write-FixtureText -Path (Join-Path $root 'modules/README.txt') -Text "notes`n"
$message = Get-ThrownMessage -Action { Read-ModuleBundle -DatabaseRoot $root }
Add-TestResult -Name 'non-SQL file under modules is outside the bundle set' -Outcome OBSERVED -Detail ('accepted=' + ($null -eq $message))

$root = New-DatabaseFixture -Name 'missing-module-file'
Remove-Item -LiteralPath (Join-Path $root $progress)
Assert-Throws -Name 'missing module file rejected' -Pattern '^Module source structure violation' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'source-changed-without-manifest'
Edit-FixtureText -Path (Join-Path $root $admission) -Find '    SET NOCOUNT ON;' -Replace "    SET NOCOUNT ON;`n    -- changed"
Assert-Throws -Name 'changed source without reviewed manifest update rejected' `
    -Pattern '^Module source checksum mismatch: modules/procedures/read_admission\.sql' -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'source-lone-cr'
Edit-FixtureText -Path (Join-Path $root $progress) -Find '    SET NOCOUNT ON;' -Replace "    SET NOCOUNT ON;`r    -- lone carriage return"
Update-FixtureManifestEntry -Root $root -EntryPath $progress
Assert-Throws -Name 'lone CR in module source rejected even with matching checksum' `
    -Pattern '^Module source checksum mismatch: modules/procedures/internal/serialize_progress\.sql' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'source-crlf'
$path = Join-Path $root $progress
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path).Replace("`n", "`r`n"))
Assert-NoThrow -Name 'CRLF checkout of a module keeps the same reviewed identity' -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'source-bom'
$path = Join-Path $root $progress
[IO.File]::WriteAllBytes($path, [byte[]](@(0xEF, 0xBB, 0xBF) + [IO.File]::ReadAllBytes($path)))
Assert-NoThrow -Name 'BOM-prefixed module keeps the same identity (BOM excluded by contract)' -Action { Read-ModuleBundle -DatabaseRoot $root }

foreach ($case in @(
        @{ Name = 'GO batch separator'; Text = "GO`n" },
        @{ Name = 'indented lowercase go'; Text = "    go`n" }
    )) {
    $root = New-DatabaseFixture -Name ('source-' + ($case.Name -replace '\W+', '-'))
    $path = Join-Path $root $progress
    Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + $case.Text)
    Update-FixtureManifestEntry -Root $root -EntryPath $progress
    Assert-Throws -Name ("module with $($case.Name) rejected") `
        -Pattern '^Module files are one batch and cannot contain GO: modules/procedures/internal/serialize_progress\.sql' `
        -Action { Read-ModuleBundle -DatabaseRoot $root }
}

$root = New-DatabaseFixture -Name 'source-go-in-comment'
$path = Join-Path $root $progress
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + "/*`nGO`n*/`n")
Update-FixtureManifestEntry -Root $root -EntryPath $progress
Assert-NoThrow -Name 'GO inside a block comment is not a batch separator (control)' -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'header-create-only'
Edit-FixtureText -Path (Join-Path $root $progress) -Find 'CREATE OR ALTER PROCEDURE dh.SerializeProgress' `
    -Replace 'CREATE PROCEDURE dh.SerializeProgress'
Update-FixtureManifestEntry -Root $root -EntryPath $progress
Assert-Throws -Name 'CREATE without OR ALTER header rejected' `
    -Pattern '^Expected one CREATE OR ALTER PROCEDURE batch at modules/procedures/internal/serialize_progress\.sql' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'header-after-comment'
$path = Join-Path $root $progress
Write-FixtureText -Path $path -Text ("-- leading note`n" + (Read-FixtureText -Path $path))
Update-FixtureManifestEntry -Root $root -EntryPath $progress
Assert-Throws -Name 'module must begin with its CREATE OR ALTER header' `
    -Pattern '^Expected one CREATE OR ALTER PROCEDURE batch' -Action { Read-ModuleBundle -DatabaseRoot $root }

# ---- Engine expectation fields and dependency declarations.
foreach ($case in @(
        @{ Name = 'definition checksum'; Property = 'DefinitionChecksum'; Value = ('0' * 64) },
        @{ Name = 'definition byte count'; Property = 'DefinitionBytes'; Value = 1268 },
        @{ Name = 'object name'; Property = 'ObjectName'; Value = 'dh.SerializeProgressV2' },
        @{ Name = 'kind'; Property = 'Kind'; Value = 'FN' }
    )) {
    $root = New-DatabaseFixture -Name ('expectation-' + ($case.Name -replace '\W+', '-'))
    Set-ManifestEntryValue -Root $root -EntryPath $progress -Property $case.Property -Value $case.Value
    Assert-Throws -Name ("wrong $($case.Name) rejected") `
        -Pattern '^Module object/kind/expected definition mismatch: modules/procedures/internal/serialize_progress\.sql' `
        -Action { Read-ModuleBundle -DatabaseRoot $root }
}

$root = New-DatabaseFixture -Name 'dependency-after-caller'
Move-ManifestEntry -Root $root -EntryPath 'modules/procedures/internal/read_character_state.sql' -BeforePath 'modules/procedures/write_safe_checkpoint.sql'
Assert-Throws -Name 'helper ordered after its caller rejected' `
    -Pattern '^Dependency must precede dh\.AcquireAndLoad: dh\.ReadCharacterState' -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'dependency-missing'
$acquireDependencies = @(@($manifest.Entries | Where-Object Path -CEQ $acquire)[0].DependsOn | Where-Object { $_ -cne 'dh.RecordOperationReceipt' })
Set-ManifestEntryValue -Root $root -EntryPath $acquire -Property DependsOn -Value $acquireDependencies
Assert-Throws -Name 'declared dependency missing an actual responsibility rejected' `
    -Pattern '^Dependency contract mismatch: modules/procedures/acquire_and_load\.sql' -Action { Read-ModuleBundle -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'dependency-extra'
$resolver = 'modules/procedures/resolve_runtime_operation.sql'
$resolverDependencies = @(@($manifest.Entries | Where-Object Path -CEQ $resolver)[0].DependsOn) + 'dh.SerializePersistenceSnapshot'
Set-ManifestEntryValue -Root $root -EntryPath $resolver -Property DependsOn -Value $resolverDependencies
Assert-Throws -Name 'declared dependency beyond the operation''s responsibility rejected' `
    -Pattern '^Dependency contract mismatch: modules/procedures/resolve_runtime_operation\.sql' -Action { Read-ModuleBundle -DatabaseRoot $root }

# ---- Permissions.
$root = New-DatabaseFixture -Name 'permissions-not-last'
Move-ManifestEntry -Root $root -EntryPath $permissions -BeforePath $admission
Assert-Throws -Name 'permissions entry before public modules rejected' `
    -Pattern '^Permissions must be the final single bundle entry' -Action { Read-ModuleBundle -DatabaseRoot $root }

foreach ($case in @(
        @{ Name = 'runtime RPC granted to recovery'; Find = 'dh.ReleaseRuntime TO dh_runtime'; Replace = 'dh.ReleaseRuntime TO dh_recovery' },
        @{ Name = 'schema-wide grant added'; Find = 'GRANT EXECUTE ON OBJECT::dh.ReadAdmission TO dh_runtime;';
            Replace = "GRANT EXECUTE ON OBJECT::dh.ReadAdmission TO dh_runtime;`nGRANT EXECUTE ON SCHEMA::dh TO dh_runtime;" },
        @{ Name = 'helper grant added'; Find = 'GRANT EXECUTE ON OBJECT::dh.ReadAdmission TO dh_runtime;';
            Replace = "GRANT EXECUTE ON OBJECT::dh.ReadAdmission TO dh_runtime;`nGRANT EXECUTE ON OBJECT::dh.ReadOperationReceipt TO dh_runtime;" },
        @{ Name = 'one grant removed'; Find = "GRANT EXECUTE ON OBJECT::dh.ReleaseRecovery TO dh_recovery;`n"; Replace = '' }
    )) {
    $root = New-DatabaseFixture -Name ('permissions-' + ($case.Name -replace '\W+', '-'))
    Edit-FixtureText -Path (Join-Path $root $permissions) -Find $case.Find -Replace $case.Replace
    Update-FixtureManifestEntry -Root $root -EntryPath $permissions
    Assert-Throws -Name ("permissions $($case.Name) rejected") `
        -Pattern '^Permissions source must contain only the nine reviewed individual EXECUTE grants' -Action { Read-ModuleBundle -DatabaseRoot $root }
}

$root = New-DatabaseFixture -Name 'permissions-comment-only-change'
Edit-FixtureText -Path (Join-Path $root $permissions) -Find '-- Execute-only' -Replace "-- reviewed note`n-- Execute-only"
Update-FixtureManifestEntry -Root $root -EntryPath $permissions
Assert-NoThrow -Name 'permissions comment change keeps the grant set (control)' -Action { Read-ModuleBundle -DatabaseRoot $root }

# ---- Migration sources.
$sources = @(Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $script:ToolRoot)
$expectedNames = '001_initial.sql,002_persistence_metadata.sql,003_module_metadata.sql,004_module_release.sql'
Assert-Equal -Name 'migration sources -> exact ordered names' -Expected $expectedNames -Actual (($sources | ForEach-Object Name) -join ',')
$recomputed = @($sources | ForEach-Object { (Get-ExpectedModuleValues -Path (Join-Path $script:ToolRoot ('migrations/' + $_.Name))).SourceChecksum })
Assert-Equal -Name 'migration checksums re-derived from source' -Expected ($recomputed -join ',') -Actual (($sources | ForEach-Object Checksum) -join ',')
$catalogMigrations = @([regex]::Matches($catalog, "\((\d), '(\d{3}_[a-z_]+\.sql)', '([0-9A-F]{64})'\)") | ForEach-Object {
        '{0}|{1}|{2}' -f $_.Groups[1].Value, $_.Groups[2].Value, $_.Groups[3].Value
    })
Assert-Equal -Name 'strict catalog migration rows equal runner sources' `
    -Expected (($sources | ForEach-Object { '{0}|{1}|{2}' -f $_.Version, $_.Name, $_.Checksum }) -join ';') -Actual ($catalogMigrations -join ';')
Assert-Equal -Name '001 raw bytes unchanged (protected SHA256)' -Expected 'A831893F95C37F1194D0FE470538EACA020FFB4192710D1675ADCEE1A9C9F214' `
    -Actual (Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes((Join-Path $script:ToolRoot 'migrations/001_initial.sql'))))

$root = New-DatabaseFixture -Name 'migration-extra'
Write-FixtureText -Path (Join-Path $root 'migrations/005_extra.sql') -Text "SELECT 1;`n"
Assert-Throws -Name 'extra migration file rejected' -Pattern '^Migration files must be the reviewed contiguous version/name set' `
    -Action { Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $root }
$baseline = @(Get-DatabaseMigrationSources -Phase Baseline001 -DatabaseRoot $root)
Add-TestResult -Name 'Baseline001 phase reads only 001 even when other migration files exist' -Outcome OBSERVED `
    -Detail ('sources=' + (($baseline | ForEach-Object Name) -join ','))

$root = New-DatabaseFixture -Name 'migration-missing'
Remove-Item -LiteralPath (Join-Path $root 'migrations/003_module_metadata.sql')
Assert-Throws -Name 'missing migration (hole) rejected' -Pattern '^Migration files must be the reviewed contiguous version/name set' `
    -Action { Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'migration-renamed'
Move-Item -LiteralPath (Join-Path $root 'migrations/002_persistence_metadata.sql') -Destination (Join-Path $root 'migrations/002_persistence_metadata_v2.sql')
Assert-Throws -Name 'renamed migration rejected' -Pattern '^Migration files must be the reviewed contiguous version/name set' `
    -Action { Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'migration-001-changed'
$path = Join-Path $root 'migrations/001_initial.sql'
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + "`n")
Assert-Throws -Name 'changed 001 rejected' -Pattern '^Immutable 001 source checksum drift' `
    -Action { Get-DatabaseMigrationSources -Phase Baseline001 -DatabaseRoot $root }

$root = New-DatabaseFixture -Name 'migration-001-crlf'
$path = Join-Path $root 'migrations/001_initial.sql'
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path).Replace("`r`n", "`n").Replace("`n", "`r`n"))
Assert-NoThrow -Name 'CRLF checkout of 001 keeps the immutable checksum (control)' `
    -Action { Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $root }

# ---- Applied history.
$history = @($sources | ForEach-Object { [pscustomobject]@{ Version = $_.Version; Name = $_.Name; Checksum = $_.Checksum } })
Assert-NoThrow -Name 'history: empty, prefix and full histories accepted' -Action {
    Assert-DatabaseMigrationHistory -History @() -Sources $sources
    Assert-DatabaseMigrationHistory -History @($history[0]) -Sources $sources
    Assert-DatabaseMigrationHistory -History $history -Sources $sources
}
$historyPattern = '^Migration history has a hole, unknown name/version or checksum drift'
Assert-Throws -Name 'history hole rejected' -Pattern $historyPattern `
    -Action { Assert-DatabaseMigrationHistory -History @($history[0], $history[2]) -Sources $sources }
Assert-Throws -Name 'history starting at version 2 rejected' -Pattern $historyPattern `
    -Action { Assert-DatabaseMigrationHistory -History @($history[1]) -Sources $sources }
$renamed = [pscustomobject]@{ Version = 2; Name = '002_other.sql'; Checksum = $history[1].Checksum }
Assert-Throws -Name 'history unknown name rejected' -Pattern $historyPattern `
    -Action { Assert-DatabaseMigrationHistory -History @($history[0], $renamed) -Sources $sources }
$drift = [pscustomobject]@{ Version = 1; Name = $history[0].Name; Checksum = $history[0].Checksum.ToLowerInvariant() }
Assert-Throws -Name 'history checksum drift (case) rejected' -Pattern $historyPattern `
    -Action { Assert-DatabaseMigrationHistory -History @($drift) -Sources $sources }
$newer = @($history) + [pscustomobject]@{ Version = 5; Name = '005_future.sql'; Checksum = ('A' * 64) }
Assert-Throws -Name 'history with newer migration rejected' -Pattern '^Database has unknown/newer migrations' `
    -Action { Assert-DatabaseMigrationHistory -History $newer -Sources $sources }

# ---- JSON row reader used for every metadata/history query.
Assert-Equal -Name 'JSON rows: [] -> zero rows' -Expected 0 -Actual @(ConvertFrom-DatabaseJsonRows -Json '[]').Count
$one = @(ConvertFrom-DatabaseJsonRows -Json '[{"Version":1,"Name":"001_initial.sql"}]')
Assert-True -Name 'JSON rows: one row stays one object (PS5.1 array nesting)' -Condition ($one.Count -eq 1 -and $one[0].Version -eq 1)
Assert-Equal -Name 'JSON rows: three rows' -Expected 3 -Actual @(ConvertFrom-DatabaseJsonRows -Json '[{"a":1},{"a":2},{"a":3}]').Count
Assert-Throws -Name 'JSON rows: empty text rejected' -Pattern '^Expected the complete database JSON row array' `
    -Action { ConvertFrom-DatabaseJsonRows -Json '' }
Assert-Throws -Name 'JSON rows: single object rejected' -Pattern '^Expected the complete database JSON row array' `
    -Action { ConvertFrom-DatabaseJsonRows -Json '{"a":1}' }
Assert-Throws -Name 'JSON rows: JSON null literal rejected' -Pattern '^Expected the complete database JSON row array' `
    -Action { ConvertFrom-DatabaseJsonRows -Json 'null' }
Assert-Throws -Name 'JSON rows: whitespace-only text rejected' `
    -Pattern '^Expected the complete database JSON row array' -Action { ConvertFrom-DatabaseJsonRows -Json '   ' }
# Truncated arrays pass the leading-bracket guard and must still fail in the JSON parser itself.
Assert-Throws -Name 'JSON rows: truncated array rejected by the parser' -Pattern '^Invalid array passed in' `
    -Action { ConvertFrom-DatabaseJsonRows -Json '[{"Version":1}' }

# ---- Known-answer source identity, fixed outside PowerShell (Node crypto, verifier evidence).
# Expected values do not come from product or TestSupport code: CRLF->LF, UTF-8 source, UTF-16LE definition.
$knownPath = Join-Path $script:SuiteRoot 'known-answer.sql'
$knownText = "CREATE PROCEDURE dh.Probe`r`nAS SELECT N'" + [char]0xAC00 + [char]0x00E9 + "';`r`n"
[IO.File]::WriteAllText($knownPath, $knownText, (New-Object Text.UTF8Encoding($false)))
$knownSql = Get-MigrationText -Path $knownPath
$knownSource = '8CE8896144B87C9159CD5623A3F672B4216D0BEA5D4C9356F4571A8C07C8F1FA'
$knownDefinition = 'D12E32D985BB1C8D43033D21925825793DC724C2527E40509795ADE2433338E9'
Assert-Equal -Name 'known answer: product source checksum' -Expected $knownSource `
    -Actual (Get-MigrationHash -Sql $knownSql)
Assert-Equal -Name 'known answer: product definition checksum' -Expected $knownDefinition `
    -Actual (Get-ModuleDefinitionHash -Sql $knownSql)
Assert-Equal -Name 'known answer: product definition bytes' -Expected 86 -Actual ($knownSql.Length * 2)
$knownValues = Get-ExpectedModuleValues -Path $knownPath
Assert-True -Name 'known answer: test recomputation helper agrees with the fixed values' -Condition (
    $knownValues.SourceChecksum -ceq $knownSource -and $knownValues.DefinitionChecksum -ceq $knownDefinition -and
    $knownValues.DefinitionBytes -eq 86)

$dbNull = Get-ThrownMessage -Action { ConvertFrom-DatabaseJsonRows -Json ([DBNull]::Value) }
Add-TestResult -Name 'JSON rows: DBNull scalar (engine form for an empty scalar FOR JSON subquery is unverified)' -Outcome OBSERVED `
    -Detail $(if ($null -eq $dbNull) { 'accepted' } else { 'rejected: ' + $dbNull })

Complete-TestSuite
