[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the shared migration/module runner at its ADO.NET boundary.
# The fake engine below records commands; it assumes rather than proves SQL Server behavior:
# a module's stored definition equals the submitted batch text, and an empty set is returned as the
# selected representation (`[]` or DBNull). Results based on those assumptions are labelled OBSERVED.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'module-deployment'
. (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
. (Join-Path $script:ToolRoot 'Database.Common.ps1')
Set-OfflineStubs

$script:Bundle = Read-ModuleBundle -DatabaseRoot $script:ToolRoot
$script:Sources = @(Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $script:ToolRoot)
$script:Contract = [pscustomobject]@{
    Database = 'Dawnholder_Dev_Fixture'
    Instance = '.\FIXTURE'
    ExecutionApproved = $true
    G2 = 'in-memory offline fixture; not an approval record'
}

function Get-DefinitionValues {
    param([string]$Text)
    $utf16 = (New-Object Text.UnicodeEncoding($false, $false)).GetBytes($Text.Replace("`r", ''))
    return [pscustomobject]@{ Bytes = $utf16.Length; Checksum = Get-Sha256Hex -Bytes $utf16 }
}

function New-EngineState {
    param(
        [ValidateSet('EmptyArray', 'DBNull')][string]$Empty = 'EmptyArray',
        [int]$AppliedMigrations = 0,
        [switch]$Installed
    )
    $state = [pscustomobject]@{
        Empty = $Empty
        History = New-Object 'Collections.Generic.List[object]'
        Releases = New-Object 'Collections.Generic.List[object]'
        Recorded = New-Object 'Collections.Generic.List[object]'
        Actual = New-Object 'Collections.Generic.List[object]'
        Events = New-Object 'Collections.Generic.List[string]'
        FailOn = $null
        StoreText = { param($sql) $sql }
    }
    foreach ($source in @($script:Sources | Select-Object -First $AppliedMigrations)) {
        $state.History.Add([pscustomobject]@{ Version = $source.Version; Name = $source.Name; Checksum = $source.Checksum })
    }
    if ($Installed) {
        $state.Releases.Add([pscustomobject]@{ Version = 4; ManifestChecksum = $script:Bundle.ManifestChecksum })
        foreach ($module in $script:Bundle.Modules) {
            $state.Recorded.Add([pscustomobject]@{
                ObjectName = $module.ObjectName
                Kind = $module.Kind.PadRight(2)
                SourceChecksum = $module.SourceChecksum
                DefinitionBytes = $module.DefinitionBytes
                DefinitionChecksum = $module.DefinitionChecksum
            })
            $state.Actual.Add((New-ActualRow -ObjectName $module.ObjectName -Kind $module.Kind `
                -Bytes $module.DefinitionBytes -Checksum $module.DefinitionChecksum))
        }
    }
    return $state
}

function New-ActualRow {
    param([string]$ObjectName, [string]$Kind, [int]$Bytes, [string]$Checksum)
    return [pscustomobject]@{
        ObjectName = $ObjectName
        Kind = $Kind.PadRight(2)
        DefinitionBytes = $Bytes
        DefinitionChecksum = $Checksum
        ExecuteAsPrincipalId = $null
        AnsiNulls = $true
        QuotedIdentifier = $true
        SchemaBound = ($Kind -ceq 'FN')
        OwnerPrincipalId = 1
        ExpectedOwnerPrincipalId = 1
    }
}

function ConvertTo-EngineJson {
    param($Rows)
    # Sorting an empty list yields $null; never serialize that as one null row.
    $array = @($Rows | Where-Object { $null -ne $_ })
    if ($array.Count -eq 0) {
        if ($script:Engine.Empty -ceq 'DBNull') { return [DBNull]::Value }
        return '[]'
    }
    return ConvertTo-Json -InputObject $array -Depth 5 -Compress
}

function Set-Row {
    param($List, [string]$ObjectName, $Row)
    $existing = @($List | Where-Object ObjectName -CEQ $ObjectName)
    foreach ($item in $existing) { [void]$List.Remove($item) }
    $List.Add($Row)
}

$script:Responder = {
    param([string]$Mode, [string]$Sql, $Values)
    $engine = $script:Engine
    # Module bodies and the catalog contain SELECT text of their own; classify them before content matches.
    $header = [regex]::Match($Sql, '^CREATE OR ALTER (PROCEDURE|FUNCTION) (dh\.\w+)')
    if ($header.Success) {
        $name = $header.Groups[2].Value
        $engine.Events.Add('DDL:' + $name)
        if ($engine.FailOn -ceq $name) { throw "Fake engine failure at $name." }
        $stored = Get-DefinitionValues -Text (& $engine.StoreText $Sql)
        $kind = if ($header.Groups[1].Value -ceq 'FUNCTION') { 'FN' } else { 'P' }
        Set-Row -List $engine.Actual -ObjectName $name -Row (New-ActualRow -ObjectName $name -Kind $kind `
            -Bytes $stored.Bytes -Checksum $stored.Checksum)
        return -1
    }
    if ($Sql.Contains('THROW 51001')) {
        $engine.Events.Add('CATALOG')
        if ($engine.FailOn -ceq 'CATALOG') { throw 'Fake catalog failure.' }
        return -1
    }
    if ($Sql.Contains("N'Dawnholder.SchemaMigration'")) { $engine.Events.Add('LOCK'); return -1 }
    if ($Sql.Contains('FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH')) {
        $engine.Events.Add('HISTORY')
        return (ConvertTo-EngineJson -Rows ($engine.History | Sort-Object Version))
    }
    if ($Sql.StartsWith('INSERT dh.SchemaVersion')) {
        $engine.Events.Add('SV:' + $Values.version)
        $engine.History.Add([pscustomobject]@{ Version = $Values.version; Name = $Values.name; Checksum = $Values.hash })
        return 1
    }
    if ($Sql.Contains('CREATE TABLE dh.Account')) { $engine.Events.Add('MIGRATION:001'); return -1 }
    if ($Sql.Contains('CREATE TABLE dh.CharacterAuthority')) { $engine.Events.Add('MIGRATION:002'); return -1 }
    if ($Sql.Contains('CREATE TABLE dh.ModuleRelease')) { $engine.Events.Add('MIGRATION:003'); return -1 }
    if ($Sql.Contains('INSERT dh.ModuleRelease')) {
        $engine.Events.Add('MIGRATION:004')
        $declared = [regex]::Match($Sql, "VALUES\((\d+), '([0-9A-F]{64})'\)")
        $engine.Releases.Add([pscustomobject]@{ Version = [int]$declared.Groups[1].Value; ManifestChecksum = $declared.Groups[2].Value })
        return 1
    }
    if ($Sql.Contains('FROM dh.ModuleRelease r LEFT JOIN dh.SchemaVersion')) {
        $engine.Events.Add('RELEASES')
        $rows = foreach ($release in ($engine.Releases | Sort-Object Version)) {
            $schema = @($engine.History | Where-Object Version -EQ $release.Version)
            [pscustomobject]@{
                Version = $release.Version
                ManifestChecksum = $release.ManifestChecksum
                SchemaVersion = $(if ($schema.Count) { $release.Version } else { $null })
            }
        }
        return (ConvertTo-EngineJson -Rows $rows)
    }
    if ($Sql.Contains('FROM dh.ModuleDefinition ORDER BY ObjectName')) {
        $engine.Events.Add('RECORDED')
        return (ConvertTo-EngineJson -Rows ($engine.Recorded | Sort-Object ObjectName))
    }
    if ($Sql.Contains('FROM sys.objects o JOIN sys.schemas')) {
        $engine.Events.Add('ACTUAL')
        return (ConvertTo-EngineJson -Rows ($engine.Actual | Sort-Object ObjectName))
    }
    if ($Sql.StartsWith('SET ANSI_NULLS ON')) { $engine.Events.Add('SESSION'); return -1 }
    if ($Sql.Contains('GRANT EXECUTE ON OBJECT::')) { $engine.Events.Add('GRANTS'); return -1 }
    if ($Sql.StartsWith('UPDATE dh.ModuleDefinition')) {
        $engine.Events.Add('META:' + $Values.name)
        Set-Row -List $engine.Recorded -ObjectName $Values.name -Row ([pscustomobject]@{
            ObjectName = $Values.name
            Kind = ([string]$Values.kind).PadRight(2)
            SourceChecksum = $Values.source
            DefinitionBytes = $Values.bytes
            DefinitionChecksum = $Values.definition
        })
        return 1
    }
    if ($Sql.Contains('001 phase boundary mismatch')) { $engine.Events.Add('BOUNDARY'); return -1 }
    throw ('Unexpected SQL in fake engine: ' + $Sql.Substring(0, [Math]::Min(80, $Sql.Length)))
}

function Invoke-Runner {
    param(
        [Parameter(Mandatory)]$Engine,
        [Parameter(Mandatory)][scriptblock]$Action
    )
    # Returns the product output, thrown message and the full ordered command/transaction trace.
    $script:Engine = $Engine
    $script:Connection = New-FakeSqlConnection -Responder $script:Responder
    $output = @()
    $message = $null
    try {
        $output = @(& $Action)
    }
    catch {
        $message = $_.Exception.Message
    }
    $trace = New-Object 'Collections.Generic.List[string]'
    foreach ($entry in $script:Connection.Log) {
        if ($entry.Mode -in @('Begin', 'Commit', 'Rollback', 'Dispose')) { $trace.Add($entry.Mode) }
    }
    return [pscustomobject]@{
        Output = $output
        Error = $message
        Events = @($Engine.Events)
        Transactions = @($trace)
        AllInTransaction = (@($script:Connection.Log | Where-Object { $_.Mode -in @('Scalar', 'NonQuery') -and -not $_.InTransaction }).Count -eq 0)
        CommandCount = @($script:Connection.Log | Where-Object { $_.Mode -in @('Scalar', 'NonQuery') }).Count
    }
}

function Invoke-Complete {
    param($Engine, $Contract = $script:Contract)
    return Invoke-Runner -Engine $Engine -Action { Invoke-Migrations -Connection $script:Connection -Phase Complete -Contract $Contract }
}

function Get-EventKinds {
    param($Run)
    return @($Run.Events | ForEach-Object { ($_ -split ':')[0] })
}

$moduleNames = @($script:Bundle.Modules | ForEach-Object ObjectName)
$ddlOrder = @($moduleNames | ForEach-Object { 'DDL:' + $_ })
$metaOrder = @($moduleNames | ForEach-Object { 'META:' + $_ })

# ---- First Complete install after the Baseline001 lifecycle step, with `[]` for empty sets.
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 1)
$expected = @('LOCK', 'HISTORY', 'MIGRATION:002', 'SV:2', 'MIGRATION:003', 'SV:3', 'MIGRATION:004', 'SV:4', 'HISTORY',
    'RELEASES', 'RECORDED', 'ACTUAL', 'SESSION') + $ddlOrder + @('GRANTS', 'RECORDED', 'ACTUAL') + $metaOrder + @('CATALOG')
Assert-True -Name 'first Complete install (empty sets as []) -> succeeds' -Condition ($null -eq $run.Error) -Detail ([string]$run.Error)
Assert-Equal -Name 'first install order: lock, history, 002-004, recheck, declaration, state, 18 DDL in manifest order, grants, recheck, metadata, catalog' `
    -Expected ($expected -join ',') -Actual ($run.Events -join ',')
Assert-Equal -Name 'first install -> single owned transaction committed' -Expected 'Begin,Commit,Dispose' -Actual ($run.Transactions -join ',')
Assert-True -Name 'first install -> every command inside the migration transaction' -Condition $run.AllInTransaction
Assert-Equal -Name 'first install -> 18 modules reported applied' -Expected 18 `
    -Actual @($run.Output | Where-Object { $_ -cmatch '^Module dh\.\w+ applied\.$' }).Count

# ---- The same first install when the engine returns NULL for an empty scalar FOR JSON subquery.
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 1 -Empty DBNull)
Add-TestResult -Name 'first Complete install with DBNull for empty sets (engine form unverified)' -Outcome OBSERVED `
    -Detail ('error=' + $run.Error + '; events=' + ($run.Events -join ',') + '; tx=' + ($run.Transactions -join ','))

# ---- Baseline001 on the freshly created (empty) SchemaVersion table.
$run = Invoke-Runner -Engine (New-EngineState) -Action { Invoke-Migrations -Connection $script:Connection -Phase Baseline001 }
Assert-True -Name 'Baseline001 on empty history (empty set as []) -> applies only 001 and boundary check' `
    -Condition ($null -eq $run.Error -and ($run.Events -join ',') -ceq 'LOCK,HISTORY,MIGRATION:001,SV:1,HISTORY,BOUNDARY' -and
        ($run.Transactions -join ',') -ceq 'Begin,Commit,Dispose') -Detail ('error=' + $run.Error + '; events=' + ($run.Events -join ','))
$run = Invoke-Runner -Engine (New-EngineState -Empty DBNull) -Action { Invoke-Migrations -Connection $script:Connection -Phase Baseline001 }
Add-TestResult -Name 'Baseline001 on empty history with DBNull for the empty set (engine form unverified)' -Outcome OBSERVED `
    -Detail ('error=' + $run.Error + '; events=' + ($run.Events -join ',') + '; tx=' + ($run.Transactions -join ','))

# ---- Re-running the same reviewed bundle on an installed database.
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 4 -Installed)
$expected = @('LOCK', 'HISTORY', 'HISTORY', 'RELEASES', 'RECORDED', 'ACTUAL', 'SESSION', 'GRANTS', 'RECORDED', 'ACTUAL') +
    $metaOrder + @('CATALOG')
Assert-Equal -Name 'same bundle rerun -> no migration body or DDL, grants and verification repeated' `
    -Expected ($expected -join ',') -Actual ($run.Events -join ',')
Assert-Equal -Name 'same bundle rerun -> committed once' -Expected 'Begin,Commit,Dispose' -Actual ($run.Transactions -join ',')
Assert-Equal -Name 'same bundle rerun -> 18 unchanged modules verified' -Expected 18 `
    -Actual @($run.Output | Where-Object { $_ -cmatch 'unchanged; actual definition verified\.$' }).Count

# ---- Rejections before any DDL on an installed database.
function Test-RejectedBeforeDdl {
    param([string]$Name, $Engine, [string]$Pattern)
    $run = Invoke-Complete -Engine $Engine
    $kinds = Get-EventKinds -Run $run
    Assert-True -Name "$Name -> intended rejection" -Condition ($null -ne $run.Error -and $run.Error -cmatch $Pattern) -Detail ([string]$run.Error)
    Assert-True -Name "$Name -> no DDL, grant, metadata or catalog" -Condition (
        @($kinds | Where-Object { $_ -in @('DDL', 'GRANTS', 'META', 'CATALOG', 'SESSION') }).Count -eq 0) -Detail ($run.Events -join ',')
    Assert-Equal -Name "$Name -> owned transaction rolled back, not committed" -Expected 'Begin,Rollback,Dispose' -Actual ($run.Transactions -join ',')
}

$engine = New-EngineState -AppliedMigrations 4 -Installed
@($engine.Actual | Where-Object ObjectName -CEQ 'dh.ReadAdmission')[0].DefinitionChecksum = ('0' * 64)
Test-RejectedBeforeDdl -Name 'actual definition drift' -Engine $engine -Pattern '^Registered/actual module drift: dh\.ReadAdmission'

$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.Actual.Add((New-ActualRow -ObjectName 'dh.Unreviewed' -Kind 'P' -Bytes 2 -Checksum ('A' * 64)))
Test-RejectedBeforeDdl -Name 'unregistered dh module present' -Engine $engine -Pattern '^Existing module registration/object set is incomplete or unexpected'

$engine = New-EngineState -AppliedMigrations 4 -Installed
[void]$engine.Recorded.Remove(@($engine.Recorded | Where-Object ObjectName -CEQ 'dh.SerializeProgress')[0])
Test-RejectedBeforeDdl -Name 'registration missing for one module' -Engine $engine -Pattern '^Existing module registration/object set is incomplete or unexpected'

$engine = New-EngineState -AppliedMigrations 4 -Installed
[void]$engine.Actual.Remove(@($engine.Actual | Where-Object ObjectName -CEQ 'dh.SerializeProgress')[0])
$engine.Actual.Add((New-ActualRow -ObjectName 'dh.SerializeProgressCopy' -Kind 'P' -Bytes 2 -Checksum ('A' * 64)))
Test-RejectedBeforeDdl -Name 'same count but different object name' -Engine $engine -Pattern '^Unregistered/missing/unexpected module'

$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.Recorded.Clear()
$engine.Actual.Clear()
Test-RejectedBeforeDdl -Name 'registrations and objects erased after install' -Engine $engine `
    -Pattern '^Existing module registration/object set is incomplete or unexpected'

foreach ($case in @(
    @{ Name = 'owner differs from dbo'; Object = 'dh.ReadAdmission'; Property = 'OwnerPrincipalId'; Value = 5 },
    @{ Name = 'EXECUTE AS context'; Object = 'dh.ReadAdmission'; Property = 'ExecuteAsPrincipalId'; Value = 1 },
    @{ Name = 'ANSI_NULLS off'; Object = 'dh.ReadAdmission'; Property = 'AnsiNulls'; Value = $false },
    @{ Name = 'QUOTED_IDENTIFIER off'; Object = 'dh.ReadAdmission'; Property = 'QuotedIdentifier'; Value = $false },
    @{ Name = 'codec not schema bound'; Object = 'dh.PersistencePayloadV1'; Property = 'SchemaBound'; Value = $false }
)) {
    $engine = New-EngineState -AppliedMigrations 4 -Installed
    @($engine.Actual | Where-Object ObjectName -CEQ $case.Object)[0].($case.Property) = $case.Value
    Test-RejectedBeforeDdl -Name ("actual module $($case.Name)") -Engine $engine `
        -Pattern ('^Registered/actual module drift: ' + [regex]::Escape($case.Object))
}

$engine = New-EngineState -AppliedMigrations 4 -Installed
@($engine.Recorded | Where-Object ObjectName -CEQ 'dh.ReadAdmission')[0].SourceChecksum = ('B' * 64)
Test-RejectedBeforeDdl -Name 'already-declared release with a different source registration' -Engine $engine `
    -Pattern '^Already-declared release has different source registration: dh\.ReadAdmission'

$engine = New-EngineState -AppliedMigrations 4 -Installed
foreach ($list in @($engine.Recorded, $engine.Actual)) {
    @($list | Where-Object ObjectName -CEQ 'dh.ReadAdmission')[0].DefinitionChecksum = ('C' * 64)
}
Test-RejectedBeforeDdl -Name 'unchanged source with different reviewed definition metadata' -Engine $engine `
    -Pattern '^Unchanged source has different reviewed definition metadata: dh\.ReadAdmission'

$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.History[1] = [pscustomobject]@{ Version = 2; Name = $engine.History[1].Name; Checksum = $engine.History[1].Checksum.ToLowerInvariant() }
$run = Invoke-Complete -Engine $engine
Assert-True -Name 'applied history drift -> rejected before any migration body' -Condition (
    $run.Error -cmatch '^Migration history has a hole' -and ($run.Events -join ',') -ceq 'LOCK,HISTORY' -and
    ($run.Transactions -join ',') -ceq 'Begin,Rollback,Dispose') -Detail ('error=' + $run.Error + '; events=' + ($run.Events -join ','))

$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.History.Add([pscustomobject]@{ Version = 5; Name = '005_future.sql'; Checksum = ('D' * 64) })
$run = Invoke-Complete -Engine $engine
Assert-True -Name 'newer database history -> rejected without downgrade' -Condition (
    $run.Error -cmatch '^Database has unknown/newer migrations' -and ($run.Events -join ',') -ceq 'LOCK,HISTORY') -Detail ([string]$run.Error)

# ---- Failure inside the transaction propagates and nothing after the failing step runs.
$engine = New-EngineState -AppliedMigrations 1
$engine.FailOn = 'dh.ReadAdmission'
$run = Invoke-Complete -Engine $engine
$kinds = Get-EventKinds -Run $run
Assert-True -Name 'intermediate module failure -> error propagates' -Condition ($run.Error -cmatch 'Fake engine failure at dh\.ReadAdmission') -Detail ([string]$run.Error)
Assert-True -Name 'intermediate module failure -> no later module, grant, metadata or catalog' -Condition (
    $run.Events[-1] -ceq 'DDL:dh.ReadAdmission' -and @($kinds | Where-Object { $_ -in @('GRANTS', 'META', 'CATALOG') }).Count -eq 0) `
    -Detail ($run.Events -join ',')
Assert-Equal -Name 'intermediate module failure -> rollback requested' -Expected 'Begin,Rollback,Dispose' -Actual ($run.Transactions -join ',')

$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.FailOn = 'CATALOG'
$run = Invoke-Complete -Engine $engine
Assert-True -Name 'strict catalog failure -> rollback without commit' -Condition (
    $run.Error -cmatch 'Fake catalog failure' -and ($run.Transactions -join ',') -ceq 'Begin,Rollback,Dispose') -Detail ([string]$run.Error)

# Engine definition text different from the reviewed source stays fail-closed before metadata is written.
$engine = New-EngineState -AppliedMigrations 1
$engine.StoreText = { param($sql) $sql.Replace('CREATE OR ALTER ', 'CREATE ') }
$run = Invoke-Complete -Engine $engine
$kinds = Get-EventKinds -Run $run
Assert-True -Name 'stored definition differs from source expectation -> rejected before metadata' -Condition (
    $run.Error -cmatch '^Registered/actual module drift' -and @($kinds | Where-Object { $_ -in @('META', 'CATALOG') }).Count -eq 0 -and
    ($run.Transactions -join ',') -ceq 'Begin,Rollback,Dispose') -Detail ('error=' + $run.Error)
Add-TestResult -Name 'engine canonical text for CREATE OR ALTER is unobserved; fake assumes the submitted batch is stored' -Outcome OBSERVED

# ---- Declaration and release history.
$fixtureRoot = New-DatabaseFixture -Name 'changed-source'
Edit-FixtureText -Path (Join-Path $fixtureRoot 'modules/procedures/read_admission.sql') -Find '    SET NOCOUNT ON;' `
    -Replace "    SET NOCOUNT ON;`n    -- reviewed wording change"
Update-FixtureManifestEntry -Root $fixtureRoot -EntryPath 'modules/procedures/read_admission.sql'
$changedBundle = Read-ModuleBundle -DatabaseRoot $fixtureRoot
$run = Invoke-Runner -Engine (New-EngineState -AppliedMigrations 4 -Installed) -Action {
    $transaction = $script:Connection.BeginTransaction()
    Invoke-ModuleBundle -Connection $script:Connection -Transaction $transaction -Bundle $changedBundle `
        -AllowFirstInstall $false -ReleaseAlreadyApplied $true
}
Assert-True -Name 'changed source bundle without a new declaration -> rejected before DDL' -Condition (
    $run.Error -cmatch '^Current bundle differs from its migration declaration' -and ($run.Events -join ',') -ceq 'RELEASES') `
    -Detail ('error=' + $run.Error + '; events=' + ($run.Events -join ','))

$futureBundle = [pscustomobject]@{
    ReleaseVersion = 5
    ManifestChecksum = $changedBundle.ManifestChecksum
    Modules = $changedBundle.Modules
    Permissions = $changedBundle.Permissions
}
$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.History.Add([pscustomobject]@{ Version = 5; Name = '005_module_release.sql'; Checksum = ('E' * 64) })
$engine.Releases.Add([pscustomobject]@{ Version = 5; ManifestChecksum = $changedBundle.ManifestChecksum })
$run = Invoke-Runner -Engine $engine -Action {
    $transaction = $script:Connection.BeginTransaction()
    Invoke-ModuleBundle -Connection $script:Connection -Transaction $transaction -Bundle $futureBundle `
        -AllowFirstInstall $false -ReleaseAlreadyApplied $false
}
$changed = @(@($engine.Recorded | Where-Object ObjectName -CEQ 'dh.ReadAdmission')[0].SourceChecksum)
Assert-True -Name 'new declaration -> only the changed module is re-applied, grants and all metadata follow' -Condition (
    $null -eq $run.Error -and (@($run.Events | Where-Object { $_ -like 'DDL:*' }) -join ',') -ceq 'DDL:dh.ReadAdmission' -and
    @($run.Events | Where-Object { $_ -ceq 'GRANTS' }).Count -eq 1 -and @($run.Events | Where-Object { $_ -like 'META:*' }).Count -eq 18 -and
    $changed[0] -ceq @($futureBundle.Modules | Where-Object ObjectName -CEQ 'dh.ReadAdmission')[0].SourceChecksum) `
    -Detail ('error=' + $run.Error + '; events=' + ($run.Events -join ','))
# ToArray avoids a Windows PowerShell 5.1 binder failure when @() wraps this generic list.
$releaseRows = $engine.Releases.ToArray()
Assert-True -Name 'new declaration -> previous release row retained' -Condition ($releaseRows.Count -eq 2 -and $releaseRows[0].Version -eq 4)

$engine = New-EngineState -AppliedMigrations 3
$engine.Releases.Add([pscustomobject]@{ Version = 4; ManifestChecksum = $script:Bundle.ManifestChecksum })
$run = Invoke-Runner -Engine $engine -Action {
    $transaction = $script:Connection.BeginTransaction()
    Invoke-ModuleBundle -Connection $script:Connection -Transaction $transaction -Bundle $script:Bundle -AllowFirstInstall $true -ReleaseAlreadyApplied $false
}
Assert-True -Name 'release declaration without its SchemaVersion row -> rejected' -Condition (
    $run.Error -cmatch '^Module release history has an unknown, missing-schema or malformed declaration' -and
    ($run.Events -join ',') -ceq 'RELEASES') -Detail ('error=' + $run.Error)

$run = Invoke-Runner -Engine (New-EngineState -AppliedMigrations 4 -Installed) -Action {
    Invoke-ModuleBundle -Connection $script:Connection -Transaction $null -Bundle $script:Bundle
}
Assert-True -Name 'module deployment without the caller transaction -> rejected before any command' `
    -Condition ($run.Error -cmatch '^Module deployment requires the caller-owned migration transaction' -and $run.CommandCount -eq 0)

# ---- Entry guards before any command and caller-owned transaction.
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 1) -Contract $null
Assert-True -Name 'Complete without reviewed contract -> rejected before any command' -Condition (
    $run.Error -cmatch '^Complete installation needs an explicit reviewed test-environment contract' -and $run.CommandCount -eq 0 -and
    $run.Transactions.Count -eq 0)
$draft = [pscustomobject]@{ Database = 'Dawnholder_Dev_Fixture'; Instance = '.\FIXTURE'; ExecutionApproved = $false; G2 = '' }
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 1) -Contract $draft
Assert-True -Name 'Complete with draft contract -> rejected before any command' -Condition (
    $run.Error -cmatch '^Draft plan cannot execute' -and $run.CommandCount -eq 0 -and $run.Transactions.Count -eq 0)
$other = [pscustomobject]@{ Database = 'Dawnholder_Dev_Other'; Instance = '.\FIXTURE'; ExecutionApproved = $true; G2 = 'fixture' }
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 1) -Contract $other
Assert-True -Name 'Complete for a different database -> rejected before any command' -Condition (
    $run.Error -cmatch '^Supply the explicit exact approved database' -and $run.CommandCount -eq 0)

$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.FailOn = 'CATALOG'
$run = Invoke-Runner -Engine $engine -Action {
    $transaction = $script:Connection.BeginTransaction()
    Invoke-Migrations -Connection $script:Connection -Transaction $transaction -Phase Complete -Contract $script:Contract
}
Assert-True -Name 'caller-supplied transaction -> runner neither commits nor rolls back it' -Condition (
    $run.Error -cmatch 'Fake catalog failure' -and ($run.Transactions -join ',') -ceq 'Begin') -Detail ($run.Transactions -join ',')

Complete-TestSuite
