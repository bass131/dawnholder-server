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
        # 0: the failure is a plain PowerShell error. Otherwise an in-memory SqlException with this number.
        FailNumber = 0
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

# Provider-like text a real engine error could carry; no executor-facing error may repeat it.
$fakeProviderText = 'FAKE-SENTINEL-PROVIDER-8W Password=fake-not-a-secret'

function Stop-FakeEngine {
    param([Parameter(Mandatory)][string]$Text)
    # Reached through the fake command's ExecuteNonQuery/ExecuteScalar method, so PowerShell wraps it as a real
    # provider error would be wrapped before Invoke-Db* see it.
    if ($script:Engine.FailNumber) {
        throw (New-TestSqlException -Number $script:Engine.FailNumber -Message ($Text + ' ' + $fakeProviderText))
    }
    throw $Text
}

$script:Responder = {
    param([string]$Mode, [string]$Sql, $Values)
    $engine = $script:Engine
    # Module bodies and the catalog contain SELECT text of their own; classify them before content matches.
    $header = [regex]::Match($Sql, '^CREATE OR ALTER (PROCEDURE|FUNCTION) (dh\.\w+)')
    if ($header.Success) {
        $name = $header.Groups[2].Value
        $engine.Events.Add('DDL:' + $name)
        if ($engine.FailOn -ceq $name) { Stop-FakeEngine -Text "Fake engine failure at $name." }
        $stored = Get-DefinitionValues -Text (& $engine.StoreText $Sql)
        $kind = if ($header.Groups[1].Value -ceq 'FUNCTION') { 'FN' } else { 'P' }
        Set-Row -List $engine.Actual -ObjectName $name -Row (New-ActualRow -ObjectName $name -Kind $kind `
                -Bytes $stored.Bytes -Checksum $stored.Checksum)
        return -1
    }
    if ($Sql.Contains('THROW 51001')) {
        $engine.Events.Add('CATALOG')
        if ($engine.FailOn -ceq 'CATALOG') { Stop-FakeEngine -Text 'Fake catalog failure.' }
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
    $failure = $null
    try {
        $output = @(& $Action)
    }
    catch {
        $failure = $_.Exception
        $message = $failure.Message
    }
    $trace = New-Object 'Collections.Generic.List[string]'
    foreach ($entry in $script:Connection.Log) {
        if ($entry.Mode -in @('Begin', 'Commit', 'Rollback', 'Dispose')) { $trace.Add($entry.Mode) }
    }
    return [pscustomobject]@{
        Output = $output
        Error = $message
        # What the caller can consume from the thrown error: the safe SQL number and whether an inner error remains.
        ErrorSqlNumber = $(if ($null -ne $failure) { $failure.Data['DatabaseSqlNumber'] } else { $null })
        ErrorHasInner = $null -ne $failure -and $null -ne $failure.InnerException
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

# ---- Empty metadata reads ask SQL for [] (source/boundary form only).
# The fake does not evaluate ISNULL, so this is not engine acceptance of an empty row set; that remains U-01.
$wrappedReadHead = '(?s)^SELECT ISNULL\(\(\s*SELECT .+ FOR JSON PATH(?:, INCLUDE_NULL_VALUES)?'
$wrappedRead = $wrappedReadHead + '\s*\), N''\[\]''\) AS \w+Rows;\s*$'
$readSourcePattern = 'FROM (dh\.SchemaVersion|dh\.ModuleRelease|dh\.ModuleDefinition|sys\.objects)'
$run = Invoke-Complete -Engine (New-EngineState -AppliedMigrations 1)
$jsonReads = @($script:Connection.Log | Where-Object { $_.Mode -ceq 'Scalar' -and $_.Sql.Contains('FOR JSON') })
$unwrappedReads = @($jsonReads | Where-Object { $_.Sql -cnotmatch $wrappedRead })
$readSources = @($jsonReads | ForEach-Object { [regex]::Match($_.Sql, $readSourcePattern).Groups[1].Value } |
        Sort-Object -Unique)
Assert-True -Name 'first install -> every scalar FOR JSON read is sent as ISNULL(..., N''[]'')' -Condition (
    $null -eq $run.Error -and $jsonReads.Count -gt 0 -and $unwrappedReads.Count -eq 0) `
    -Detail ('reads=' + $jsonReads.Count + '; error=' + $run.Error)
Assert-Equal -Name 'first install -> wrapped reads cover history, releases, recorded and actual modules' `
    -Expected 'dh.ModuleDefinition,dh.ModuleRelease,dh.SchemaVersion,sys.objects' -Actual ($readSources -join ',')
$databaseCommon = Read-FixtureText -Path (Join-Path $script:ToolRoot 'Database.Common.ps1')
$moduleCommon = Read-FixtureText -Path (Join-Path $script:ToolRoot 'Module.Common.ps1')
$scalarSites = [regex]::Matches($databaseCommon + $moduleCommon, 'FOR JSON PATH').Count
$wrappedSitePattern = 'FOR JSON PATH(?:, INCLUDE_NULL_VALUES)?\r?\n\), N''\[\]''\) AS \w+Rows;'
$wrappedSites = [regex]::Matches($databaseCommon + $moduleCommon, $wrappedSitePattern).Count
Assert-True -Name 'runner sources -> all five scalar FOR JSON sites end in ISNULL(..., N''[]'')' `
    -Condition ($scalarSites -eq 5 -and $wrappedSites -eq 5) `
    -Detail ('sites=' + $scalarSites + '; wrapped=' + $wrappedSites)

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
# INSTALL-07 (user decision msg_6b139eecb44e; final review contract requirement 3): Invoke-Db* rethrow a safe error
# that keeps only the first real SqlException's number, 0 without one, and neither provider text nor inner error.
# The numbers below are the fixture's chosen values; the provider text is the fake engine's own failure text.
$engine = New-EngineState -AppliedMigrations 1
$engine.FailOn = 'dh.ReadAdmission'
$run = Invoke-Complete -Engine $engine
$kinds = Get-EventKinds -Run $run
Assert-True -Name 'intermediate module failure -> error propagates' -Condition (
    $null -ne $run.Error -and $run.ErrorSqlNumber -is [int] -and $run.ErrorSqlNumber -eq 0 -and
    -not $run.ErrorHasInner -and -not $run.Error.Contains('Fake engine failure')) `
    -Detail ('sqlNumber=' + $run.ErrorSqlNumber + '; inner=' + $run.ErrorHasInner + '; error=' + $run.Error)
Assert-True -Name 'intermediate module failure -> no later module, grant, metadata or catalog' -Condition (
    $run.Events[-1] -ceq 'DDL:dh.ReadAdmission' -and @($kinds | Where-Object { $_ -in @('GRANTS', 'META', 'CATALOG') }).Count -eq 0) `
    -Detail ($run.Events -join ',')
Assert-Equal -Name 'intermediate module failure -> rollback requested' -Expected 'Begin,Rollback,Dispose' -Actual ($run.Transactions -join ',')

# The transaction outcome is asserted on its own so a changed error text cannot hide it.
$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.FailOn = 'CATALOG'
$run = Invoke-Complete -Engine $engine
Assert-True -Name 'strict catalog failure -> rollback without commit' -Condition (
    $null -ne $run.Error -and ($run.Transactions -join ',') -ceq 'Begin,Rollback,Dispose') `
    -Detail ('tx=' + ($run.Transactions -join ',') + '; error=' + $run.Error)
Assert-True -Name 'strict catalog failure -> safe error without fake engine text' -Condition (
    $run.ErrorSqlNumber -is [int] -and $run.ErrorSqlNumber -eq 0 -and -not $run.ErrorHasInner -and
    -not $run.Error.Contains('Fake catalog failure')) `
    -Detail ('sqlNumber=' + $run.ErrorSqlNumber + '; inner=' + $run.ErrorHasInner + '; error=' + $run.Error)
$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.FailOn = 'CATALOG'
$engine.FailNumber = 51001
$run = Invoke-Complete -Engine $engine
Assert-True -Name 'strict catalog SqlException -> its number propagates without provider text, owned tx rolled back' `
    -Condition ($run.ErrorSqlNumber -is [int] -and $run.ErrorSqlNumber -eq 51001 -and -not $run.ErrorHasInner -and
        -not $run.Error.Contains('FAKE-SENTINEL') -and -not $run.Error.Contains('Fake catalog failure') -and
        ($run.Transactions -join ',') -ceq 'Begin,Rollback,Dispose') `
    -Detail ('sqlNumber=' + $run.ErrorSqlNumber + '; inner=' + $run.ErrorHasInner + '; tx=' +
        ($run.Transactions -join ',') + '; error=' + $run.Error)

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
# A source change whose manifest entry alone was updated is refused before any database command:
# 004 and the strict catalog still name the old bundle (SQL-STRUCTURE-06 recurrence path).
$partialRoot = New-DatabaseFixture -Name 'changed-source-manifest-only'
Edit-FixtureText -Path (Join-Path $partialRoot 'modules/procedures/read_admission.sql') -Find '    SET NOCOUNT ON;' `
    -Replace "    SET NOCOUNT ON;`n    -- reviewed wording change"
Update-FixtureManifestEntry -Root $partialRoot -EntryPath 'modules/procedures/read_admission.sql'
$partialCause = '^Module source structure violation: .*"Kind":"CatalogSourceChecksum",' +
'"Source":"modules/procedures/read_admission\.sql".*"Kind":"CatalogManifestIdentity"'
Assert-Throws -Name 'changed source with only its manifest entry updated -> refused at the source stage' `
    -Pattern $partialCause -Action { Read-ModuleBundle -DatabaseRoot $partialRoot }

# The reviewed change with every derived consumer aligned (manifest, 004, catalog) reaches the declaration check.
$fixtureRoot = New-DatabaseFixture -Name 'changed-source'
Edit-FixtureText -Path (Join-Path $fixtureRoot 'modules/procedures/read_admission.sql') -Find '    SET NOCOUNT ON;' `
    -Replace "    SET NOCOUNT ON;`n    -- reviewed wording change"
Sync-FixtureHashConsumers -Root $fixtureRoot
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
    $null -ne $run.Error -and ($run.Transactions -join ',') -ceq 'Begin') `
    -Detail ('tx=' + ($run.Transactions -join ',') + '; error=' + $run.Error)
$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.FailOn = 'CATALOG'
$engine.FailNumber = 51001
$run = Invoke-Runner -Engine $engine -Action {
    $transaction = $script:Connection.BeginTransaction()
    Invoke-Migrations -Connection $script:Connection -Transaction $transaction -Phase Complete `
        -Contract $script:Contract
}
Assert-True -Name 'caller-supplied transaction SqlException -> number propagates, provider text hidden, tx left open' `
    -Condition ($run.ErrorSqlNumber -is [int] -and $run.ErrorSqlNumber -eq 51001 -and -not $run.ErrorHasInner -and
        -not $run.Error.Contains('FAKE-SENTINEL') -and ($run.Transactions -join ',') -ceq 'Begin') `
    -Detail ('sqlNumber=' + $run.ErrorSqlNumber + '; tx=' + ($run.Transactions -join ',') + '; error=' + $run.Error)

# ---- INSTALL-07 boundary at Invoke-DbScalar/Invoke-DbNonQuery (contract requirement 3). In-memory SqlException
# objects are thrown by a recording fake command; this is not a SqlCommand, provider call or SQL engine. Each
# expected number is the one the case's fixture puts first in the chain, never derived from the product walk.
function New-RecordingConnection {
    param([Parameter(Mandatory)][scriptblock]$Execute)
    # Records command creation, execution and disposal; Execute either returns the result or throws.
    $connection = [pscustomobject]@{
        Database = 'Dawnholder_Dev_Fixture'
        Execute = $Execute
        Log = New-Object 'Collections.Generic.List[string]'
    }
    $connection | Add-Member -MemberType ScriptMethod -Name CreateCommand -Value {
        $owner = $this
        $owner.Log.Add('Create')
        $command = [pscustomobject]@{
            Owner = $owner
            CommandText = ''
            CommandTimeout = 0
            Transaction = $null
        }
        $command | Add-Member -MemberType ScriptMethod -Name ExecuteScalar -Value {
            $this.Owner.Log.Add('Scalar:tx=' + ($null -ne $this.Transaction))
            return (& $this.Owner.Execute)
        }
        $command | Add-Member -MemberType ScriptMethod -Name ExecuteNonQuery -Value {
            $this.Owner.Log.Add('NonQuery:tx=' + ($null -ne $this.Transaction))
            return (& $this.Owner.Execute)
        }
        $command | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Owner.Log.Add('Dispose') }
        return $command
    }
    return $connection
}

$sqlFailureCases = @(
    @{
        Name = 'a wrapped real SqlException'
        Expected = 547
        Execute = { throw (New-TestSqlException -Number 547 -Message ('Provider text ' + $fakeProviderText)) }
    },
    @{
        Name = 'the first real SqlException before a deeper one'
        Expected = 2627
        Execute = {
            $deeper = New-TestSqlException -Number 51123 -Message ('Deeper provider text ' + $fakeProviderText)
            throw (New-TestSqlException -Number 2627 -Message ('Provider text ' + $fakeProviderText) `
                    -InnerException $deeper)
        }
    },
    @{
        Name = 'a real SqlException below a non-SQL wrapper'
        Expected = 1222
        Execute = {
            $sql = New-TestSqlException -Number 1222 -Message ('Provider text ' + $fakeProviderText)
            throw ([InvalidOperationException]::new('Wrapper text ' + $fakeProviderText, $sql))
        }
    },
    @{
        Name = 'a real SqlException that also carries a same-name Number property'
        Expected = 515
        Execute = {
            $sql = New-TestSqlException -Number 515 -Message ('Provider text ' + $fakeProviderText)
            $sql | Add-Member -MemberType NoteProperty -Name Number -Value 999 -Force
            throw $sql
        }
    },
    @{
        Name = 'a non-SQL error whose message, Data and Number look like a SQL number'
        Expected = 0
        Execute = {
            $lookalike = [InvalidOperationException]::new(
                'Test environment SQL command failed (provider number 547); ' + $fakeProviderText,
                [Exception]::new('Inner text ' + $fakeProviderText))
            $lookalike.Data['DatabaseSqlNumber'] = 547
            $lookalike | Add-Member -MemberType NoteProperty -Name Number -Value 547
            throw $lookalike
        }
    },
    @{
        Name = 'a plain non-SQL error'
        Expected = 0
        Execute = { throw ('Native text ' + $fakeProviderText) }
    }
)
# Precondition of the same-name case: PowerShell shows the added Number to a property read, so a reader that does
# not use the CLR value would see 999.
$shadowedSql = New-TestSqlException -Number 515 -Message 'shadow probe'
$shadowedSql | Add-Member -MemberType NoteProperty -Name Number -Value 999 -Force
$shadowVisible = $shadowedSql.Number -eq 999 -and $shadowedSql.PSBase.Number -eq 515
Assert-True -Name 'fixture: an added Number property shadows the SqlException number for ordinary reads' `
    -Condition $shadowVisible -Detail ('visible=' + $shadowedSql.Number + '; clr=' + $shadowedSql.PSBase.Number)

$callerTransaction = [pscustomobject]@{ Name = 'caller-owned fixture transaction' }
foreach ($entryPoint in @('Invoke-DbScalar', 'Invoke-DbNonQuery')) {
    $mode = $(if ($entryPoint -ceq 'Invoke-DbScalar') { 'Scalar' } else { 'NonQuery' })
    foreach ($case in $sqlFailureCases) {
        $connection = New-RecordingConnection -Execute $case.Execute
        $failure = $null
        try {
            $null = & $entryPoint -Connection $connection -Sql 'SELECT 1' -Transaction $callerTransaction
        }
        catch {
            $failure = $_.Exception
        }
        $number = $(if ($null -ne $failure) { $failure.Data['DatabaseSqlNumber'] } else { $null })
        $text = $(if ($null -ne $failure) { $failure.ToString() } else { '' })
        $numbered = $number -is [int] -and $number -eq $case.Expected
        $safe = $failure -is [InvalidOperationException] -and $null -eq $failure.InnerException -and
            -not $text.Contains('FAKE-SENTINEL') -and -not $text.Contains('Password=')
        $order = ($connection.Log -join ',') -ceq ('Create,' + $mode + ':tx=True,Dispose')
        Assert-True -Name ("$entryPoint boundary: $($case.Name) -> number $($case.Expected), no provider text") `
            -Condition ($numbered -and $safe -and $order) `
            -Detail ('number=' + $number + '; safe=' + $safe + '; calls=' + ($connection.Log -join ',') + '; error=' +
                $(if ($null -ne $failure) { $failure.Message } else { 'none' }))
    }
    $successValue = @{ Scalar = 'scalar-fixture'; NonQuery = 3 }[$mode]
    $connection = New-RecordingConnection -Execute { $successValue }
    $value = & $entryPoint -Connection $connection -Sql 'SELECT 1' -Transaction $callerTransaction
    $calls = $connection.Log -join ','
    Assert-True -Name ("$entryPoint success -> provider value returned unchanged, command disposed after it") `
        -Condition ($value -ceq $successValue -and $calls -ceq ('Create,' + $mode + ':tx=True,Dispose')) `
        -Detail ('value=' + $value + '; calls=' + $calls)
}

# Definitions-only import: the shared boundary file declares one function, and a fresh runspace that loads only
# Database.Common.ps1 resolves the boundary to that file (this suite's own scope already has it from both imports).
$sqlErrorFile = [IO.Path]::GetFullPath((Join-Path $script:ToolRoot 'SqlError.Common.ps1'))
$parseTokens = $null
$parseErrors = $null
$sqlErrorAst = [Management.Automation.Language.Parser]::ParseFile($sqlErrorFile, [ref]$parseTokens,
    [ref]$parseErrors)
$topLevel = @($sqlErrorAst.EndBlock.Statements)
$onlyDefinitions = $parseErrors.Count -eq 0 -and $null -eq $sqlErrorAst.ParamBlock -and
    $null -eq $sqlErrorAst.BeginBlock -and $null -eq $sqlErrorAst.ProcessBlock -and $topLevel.Count -eq 1 -and
    $topLevel[0] -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $topLevel[0].Name -ceq 'New-DatabaseSqlFailure'
$fresh = [PowerShell]::Create()
$resolvedBoundary = ''
$freshErrors = 0
try {
    $databaseFile = Join-Path $script:ToolRoot 'Database.Common.ps1'
    $null = $fresh.AddScript('param($File) . $File; ' +
        '[string](Get-Command -Name New-DatabaseSqlFailure -CommandType Function).ScriptBlock.File').
        AddArgument($databaseFile)
    $resolvedBoundary = [string](@($fresh.Invoke()) -join '')
    $freshErrors = $fresh.Streams.Error.Count
}
catch {
    # A missing boundary stops the probe; record it as this assertion's failure instead of ending the suite.
    $resolvedBoundary = 'probe stopped: ' + $_.Exception.Message
    $freshErrors = $freshErrors + 1
}
finally {
    $fresh.Dispose()
}
Assert-True -Name 'SqlError.Common.ps1 only defines New-DatabaseSqlFailure; Database.Common.ps1 resolves to that file' `
    -Condition ($onlyDefinitions -and $freshErrors -eq 0 -and
        [string]::Equals($resolvedBoundary, $sqlErrorFile, [StringComparison]::OrdinalIgnoreCase)) `
    -Detail ('onlyDefinitions=' + $onlyDefinitions + '; resolved=' + $resolvedBoundary + '; errors=' + $freshErrors)

# ---- Test-Database.ps1 Assert-SqlError consumes that safe number (INSTALL-07-C1). Only the function definition is
# taken from the script's AST; the script body opens SQL and never runs here. Accepted: an Int32 Data number from the
# product boundary that is listed. Rejected (rethrown): another number, 0 from a non-SQL failure, a raw provider
# exception that bypassed the boundary, SQL-like message text, a missing or non-Int32 Data value, and success.
$testDatabaseFile = Join-Path $script:ToolRoot 'Test-Database.ps1'
$parseErrors = $null
$testDatabaseAst = [Management.Automation.Language.Parser]::ParseFile($testDatabaseFile, [ref]$parseTokens,
    [ref]$parseErrors)
$assertSqlError = $testDatabaseAst.Find({
        param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Assert-SqlError'
    }, $true)

function Invoke-AssertSqlError {
    param([Parameter(Mandatory)][scriptblock]$Action, [Parameter(Mandatory)][int[]]$Numbers)
    # The extracted definition runs in a child scope; its output and any rethrown error are returned.
    return & {
        param($Definition, $Act, $Expected)
        . ([scriptblock]::Create($Definition))
        $output = @()
        $failure = $null
        try {
            $output = @(Assert-SqlError -Action $Act -Numbers $Expected -Label 'offline consumer case')
        }
        catch {
            $failure = $_.Exception
        }
        [pscustomobject]@{ Output = $output; Failure = $failure }
    } $assertSqlError.Extent.Text $Action $Numbers
}

$consumerReady = $parseErrors.Count -eq 0 -and $null -ne $assertSqlError
$script:ConsumerConnection = New-RecordingConnection -Execute $sqlFailureCases[0].Execute
$listed = Invoke-AssertSqlError -Numbers @(547) -Action {
    Invoke-DbNonQuery -Connection $script:ConsumerConnection -Sql 'INSERT dh.Character(CharacterId) VALUES(@id)'
}
$listedText = ($listed.Output -join "`n")
Assert-True -Name 'Assert-SqlError accepts the listed SQL number kept by Invoke-DbNonQuery, without provider text' `
    -Condition ($consumerReady -and $null -eq $listed.Failure -and
        $listedText -ceq 'PASS: offline consumer case (SQL 547)') `
    -Detail ('output=' + $listedText + '; error=' + $(if ($listed.Failure) { $listed.Failure.Message } else { 'none' }))

$script:ConsumerConnection = New-RecordingConnection -Execute $sqlFailureCases[0].Execute
$unlisted = Invoke-AssertSqlError -Numbers @(2627, 2601) -Action {
    Invoke-DbNonQuery -Connection $script:ConsumerConnection -Sql 'INSERT dh.Account(AccountId) VALUES(@id)'
}
$rethrownNumber = $(if ($unlisted.Failure) { $unlisted.Failure.Data['DatabaseSqlNumber'] } else { $null })
Assert-True -Name 'Assert-SqlError rethrows the safe error for an unlisted SQL number' -Condition (
    $consumerReady -and $unlisted.Output.Count -eq 0 -and $rethrownNumber -is [int] -and $rethrownNumber -eq 547 -and
    -not $unlisted.Failure.Message.Contains('FAKE-SENTINEL')) `
    -Detail ('rethrownNumber=' + $rethrownNumber + '; output=' + ($unlisted.Output -join ' | '))

$script:ConsumerConnection = New-RecordingConnection -Execute $sqlFailureCases[5].Execute
$fallback = Invoke-AssertSqlError -Numbers @(547) -Action {
    Invoke-DbNonQuery -Connection $script:ConsumerConnection -Sql 'UPDATE dh.Character SET Class=2'
}
$fallbackNumber = $(if ($fallback.Failure) { $fallback.Failure.Data['DatabaseSqlNumber'] } else { $null })
Assert-True -Name 'Assert-SqlError rethrows a non-SQL failure that the boundary reported as number 0' -Condition (
    $consumerReady -and $fallback.Output.Count -eq 0 -and $fallbackNumber -is [int] -and $fallbackNumber -eq 0) `
    -Detail ('rethrownNumber=' + $fallbackNumber)

$script:ConsumerConnection = New-RecordingConnection -Execute { 1 }
$succeeded = Invoke-AssertSqlError -Numbers @(547) -Action {
    Invoke-DbNonQuery -Connection $script:ConsumerConnection -Sql 'UPDATE dh.Character SET Class=0'
}
Assert-True -Name 'Assert-SqlError fails when the action unexpectedly succeeds' -Condition (
    $consumerReady -and $null -ne $succeeded.Failure -and
    $succeeded.Failure.Message -ceq 'offline consumer case unexpectedly succeeded.') `
    -Detail $(if ($succeeded.Failure) { $succeeded.Failure.Message } else { 'no failure' })

$rejectedShapes = [ordered]@{
    'a raw SqlException that bypassed the product boundary' = {
        throw (New-TestSqlException -Number 547 -Message ('Provider text ' + $fakeProviderText))
    }
    'SQL-like message text without a Data number' = {
        throw ([InvalidOperationException]::new(
                'Test environment SQL command failed (provider number 547); raw SQL and provider text suppressed.'))
    }
    'a string Data number' = {
        $shape = [InvalidOperationException]::new('Safe-looking error')
        $shape.Data['DatabaseSqlNumber'] = '547'
        throw $shape
    }
    'an Int64 Data number' = {
        $shape = [InvalidOperationException]::new('Safe-looking error')
        $shape.Data['DatabaseSqlNumber'] = [long]547
        throw $shape
    }
}
foreach ($shape in $rejectedShapes.Keys) {
    $result = Invoke-AssertSqlError -Numbers @(547) -Action $rejectedShapes[$shape]
    Assert-True -Name ("Assert-SqlError rejects $shape") -Condition (
        $consumerReady -and $result.Output.Count -eq 0 -and $null -ne $result.Failure -and
        $result.Failure.Message -cne 'offline consumer case unexpectedly succeeded.') `
        -Detail ('rethrown=' + $(if ($result.Failure) { $result.Failure.GetType().FullName } else { 'none' }) +
            '; output=' + ($result.Output -join ' | '))
}

# The catalog drift checks of Test-Database run Invoke-Migrations inside the test's own transaction. The fake catalog
# fails with the fixture's 51004; the Contract is supplied here so the offline runner reaches the catalog.
$engine = New-EngineState -AppliedMigrations 4 -Installed
$engine.FailOn = 'CATALOG'
$engine.FailNumber = 51004
$script:Engine = $engine
$script:Connection = New-FakeSqlConnection -Responder $script:Responder
$script:ConsumerTransaction = $script:Connection.BeginTransaction()
$drift = Invoke-AssertSqlError -Numbers @(51004) -Action {
    Invoke-Migrations -Connection $script:Connection -Transaction $script:ConsumerTransaction -Phase Complete `
        -Contract $script:Contract
}
$driftTransactions = @($script:Connection.Log |
        Where-Object { $_.Mode -in @('Begin', 'Commit', 'Rollback', 'Dispose') } | ForEach-Object Mode)
Assert-True -Name 'Assert-SqlError accepts a catalog SQL number from Invoke-Migrations, leaving the test transaction' `
    -Condition ($consumerReady -and $null -eq $drift.Failure -and
        ($drift.Output -join '') -ceq 'PASS: offline consumer case (SQL 51004)' -and
        ($driftTransactions -join ',') -ceq 'Begin') `
    -Detail ('output=' + ($drift.Output -join '') + '; tx=' + ($driftTransactions -join ',') + '; error=' +
        $(if ($drift.Failure) { $drift.Failure.Message } else { 'none' }))

Complete-TestSuite
