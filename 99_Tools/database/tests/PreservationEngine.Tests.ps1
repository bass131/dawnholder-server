[CmdletBinding()]
param([Parameter(Mandatory)][string]$WorkRoot)
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'preservation-engine'
. (Join-Path $script:ToolRoot 'test-environment/EngineCheck.Common.ps1')
Set-OfflineStubs

$sites = @(Get-EngineJsonSites -DatabaseRoot $script:ToolRoot)
Assert-Equal -Expected 5 -Actual $sites.Count -Name 'source discovery finds exactly five scalar sites'
Assert-Equal -Expected 'HistoryBefore|HistoryAfter|ModuleRegistration|ActualModules|Release' `
    -Actual ($sites.Kind -join '|') -Name 'source discovery preserves the two distinct history call sites'
$sourceFixture = New-DatabaseFixture -Name 'source-discovery'
Copy-Item -LiteralPath (Join-Path $script:ToolRoot 'Database.Common.ps1') -Destination $sourceFixture
$sourcePath = Join-Path $sourceFixture 'Database.Common.ps1'
$sourceText = [IO.File]::ReadAllText($sourcePath)
Write-FixtureText -Path $sourcePath -Text ($sourceText.Replace('SELECT ISNULL((', 'SELECT COALESCE(('))
Assert-Throws -Action { Get-EngineJsonSites -DatabaseRoot $sourceFixture } -Pattern 'Exactly five' `
    -Name 'changed product copy with missing boundaries fails before observation'
Write-FixtureText -Path $sourcePath -Text ($sourceText + "`n" + @'
function Extra-Query { Invoke-DbScalar -Sql "SELECT ISNULL((SELECT 1 FOR JSON PATH), N'[]')" }
'@)
Assert-Throws -Action { Get-EngineJsonSites -DatabaseRoot $sourceFixture } -Pattern 'Exactly five' `
    -Name 'sixth source boundary is refused'
Assert-Equal -Expected 'SELECT ((SELECT 1 FOR JSON PATH))' `
    -Actual (ConvertTo-UnwrappedEngineSql -Sql "SELECT ISNULL((SELECT 1 FOR JSON PATH), N'[]')") `
    -Name 'unwrapped SQL is derived by the two exact substitutions'
foreach ($text in @("SELECT ISNULL((SELECT 1)", "ISNULL((ISNULL((SELECT 1), N'[]')", "ISNULL((1), N'[]') ), N'[]')")) {
    Assert-Throws -Action { ConvertTo-UnwrappedEngineSql -Sql $text } -Pattern 'exactly one' `
        -Name ('wrapper multiplicity rejects ' + $text)
}

$cases = @(
    @{ Name = 'empty DBNull'; Wrapped = '[]'; Raw = [DBNull]::Value; Count = 0; Want = 'PASS'; RawWant = 'PASS' },
    @{ Name = 'empty raw array'; Wrapped = '[]'; Raw = '[]'; Count = 0; Want = 'PASS'; RawWant = 'OBSERVED' },
    @{ Name = 'one row'; Wrapped = '[{"v":1}]'; Raw = '[{"v":1}]'; Count = 1; Want = 'PASS'; RawWant = 'PASS' },
    @{ Name = 'row count'; Wrapped = '[{"v":1}]'; Raw = '[{"v":1}]'; Count = 2; Want = 'FAIL'; RawWant = 'PASS' },
    @{ Name = 'unequal text'; Wrapped = '[{"v":1}]'; Raw = '[{"v":2}]'; Count = 1; Want = 'FAIL'; RawWant = 'PASS' },
    @{ Name = 'wrapped DBNull'; Wrapped = [DBNull]::Value; Raw = [DBNull]::Value;
        Count = 0; Want = 'FAIL'; RawWant = 'PASS' },
    @{ Name = 'wrapped whitespace'; Wrapped = ' []'; Raw = '[]'; Count = 0; Want = 'FAIL'; RawWant = 'OBSERVED' }
)
foreach ($case in $cases) {
    $verdict = Get-EngineJsonVerdict -Wrapped $case.Wrapped -Unwrapped $case.Raw -Count $case.Count
    Assert-Equal -Expected $case.Want -Actual $verdict.Outcome -Name ('decision table ' + $case.Name)
    Assert-Equal -Expected $case.RawWant -Actual $verdict.UnwrappedOutcome -Name ('raw decision ' + $case.Name)
}
$summary = Get-EngineRawSummary -Value ('x' * 220)
Assert-Equal -Expected 220 -Actual $summary.Length -Name 'raw length is measured before prefix truncation'
Assert-Equal -Expected 200 -Actual $summary.Prefix.Length -Name 'raw prefix is bounded to 200 characters'
Assert-Equal -Expected 'DBNull' -Actual (Get-EngineRawSummary -Value ([DBNull]::Value)).Type `
    -Name 'raw DBNull type is not coerced to text'

$fixedSnapshot = @'
{"Account":[{"AccountId":"a","CreatedUtc":"2026-01-01T00:00:00.000"}],
"Character":[{"CharacterId":"c","AccountId":"a","Class":0,"Version":"0000000000000001"}],
"CharacterProgress":[{"CharacterId":"c","Hp":0,"PositionX":"-1.2500000000000000e+000"}]}
'@
$before = $fixedSnapshot | ConvertFrom-Json
Assert-Equal -Expected 0 -Actual @(Compare-PreservationSnapshot -Before $before -After $before).Count `
    -Name 'unchanged snapshot has no differences'
foreach ($mutation in @('Added', 'Missing', 'ValueChanged', 'RowVersionChanged')) {
    $after = $fixedSnapshot | ConvertFrom-Json
    switch ($mutation) {
        'Added' { $after.Account += [pscustomobject]@{ AccountId = 'b'; CreatedUtc = 'fixed' } }
        'Missing' { $after.CharacterProgress = @() }
        'ValueChanged' { $after.CharacterProgress[0].Hp = 1 }
        'RowVersionChanged' { $after.Character[0].Version = '0000000000000002' }
    }
    $differences = @(Compare-PreservationSnapshot -Before $before -After $after)
    Assert-Equal -Expected 1 -Actual $differences.Count -Name ('snapshot detects only ' + $mutation)
    Assert-Equal -Expected $mutation -Actual $differences[0].Kind -Name ('snapshot classifies ' + $mutation)
}
$binding = [pscustomobject]@{
    AccountId = '11111111-1111-1111-1111-111111111111'
    CharacterId = '22222222-2222-2222-2222-222222222222'
}
$ids = @(1..6 | ForEach-Object { [Guid]::NewGuid() })
$ids[3] = [Guid]$binding.AccountId
Assert-Throws -Action { Assert-PreservationIds -Ids $ids -Contract $binding } -Pattern 'collide' `
    -Name 'binding account GUID collision stops without adoption'
$ids[3] = [Guid]$binding.CharacterId
Assert-Throws -Action { Assert-PreservationIds -Ids $ids -Contract $binding } -Pattern 'collide' `
    -Name 'binding character GUID collision stops without adoption'

$outputPath = Join-Path $script:SuiteRoot 'output.json'
Write-NewEngineJson -Path $outputPath -Value @{ Fixed = 'first' }
Assert-Throws -Action { Write-NewEngineJson -Path $outputPath -Value @{ Fixed = 'second' } } -Pattern 'exist' `
    -Name 'CreateNew refuses an existing output at write time'
Assert-Throws -Action {
    Assert-EngineEvidencePath -Directory $script:SuiteRoot -Path $outputPath
} -Pattern 'already exists' -Name 'existing output is refused before connecting'
Assert-Throws -Action {
    Assert-EngineEvidencePath -Directory $script:SuiteRoot -Path (Join-Path $WorkRoot 'outside.json')
} -Pattern 'below the explicit' -Name 'evidence cannot escape its explicit directory'
Assert-Throws -Action {
    Assert-EngineEvidencePath -Directory $script:SuiteRoot -Path 'relative.json'
} -Pattern 'absolute' -Name 'relative evidence paths are refused'

$baselineRow = [pscustomobject]@{
    Version = 1
    Name = '001_initial.sql'
    Checksum = 'F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC'
}
foreach ($origin in @('Unchanged', 'Database', 'Manifest', 'BaselineJournal')) {
    $rowText = ConvertTo-Json -InputObject $baselineRow
    $history = @($rowText | ConvertFrom-Json)
    $manifestRow = $rowText | ConvertFrom-Json
    $journalRow = $rowText | ConvertFrom-Json
    switch ($origin) {
        'Database' { $history[0].Checksum = '0' * 64 }
        'Manifest' { $manifestRow.Name = '001_changed.sql' }
        'BaselineJournal' { $journalRow.Version = 2 }
    }
    $schemaContext = [pscustomobject]@{
        DatabaseRoot = $script:ToolRoot
        Manifest = [pscustomobject]@{
            MigrationManifest = @($manifestRow)
            Steps = @([pscustomobject]@{
                    Name = 'InstallBaseline001'
                    Status = 'Done'
                    Identity = [pscustomobject]@{ MigrationManifest = @($journalRow) }
                })
        }
    }
    $differences = @(Get-PreservationSchemaDifferences -Context $schemaContext -History $history)
    $want = if ($origin -ceq 'Unchanged') { 0 } else { 1 }
    Assert-Equal -Expected $want -Actual $differences.Count -Name ('baseline three-source comparison ' + $origin)
    if ($want) {
        Assert-Equal -Expected $origin -Actual $differences[0].Source -Name ('baseline drift attribution ' + $origin)
    }
}

$savedDatabaseSql = $script:EngineProductDatabaseSql
try {
    $script:EngineProductDatabaseSql = {
        $table = [Data.DataTable]::new()
        foreach ($name in @('Machine', 'ProductVersion', 'ServerCollation', 'HostPlatform', 'MasterFamilyGuid')) {
            [void]$table.Columns.Add($name, [string])
        }
        [void]$table.Rows.Add('observed-host', '17.0.1.2', 'observed-collation', 'Linux',
            '33333333-3333-3333-3333-333333333333')
        return , $table
    }
    & {
        function Open-TestEnvironmentDatabase {
            param($Contract, $Manifest, $Database, [switch]$Master)
            $null = Invoke-DatabaseSql -Sql "SELECT SERVERPROPERTY('MachineName')" -Result Rows
            return [pscustomobject]@{ Offline = $true }
        }
        $identityContext = [pscustomobject]@{
            Contract = [pscustomobject]@{}
            Manifest = [pscustomobject]@{ Database = 'Dawnholder_Dev_Fixture'; DatabaseIdentity = 'input' }
            Report = [pscustomobject]@{ EngineIdentity = $null }
        }
        $null = Open-EngineObservedConnection -Context $identityContext
        Assert-Equal -Expected 'observed-host' -Actual $identityContext.Report.EngineIdentity.MachineName `
            -Name 'engine identity records the existing helper row rather than an expected hostname constant'
        Assert-Equal -Expected '17.0.1.2' -Actual $identityContext.Report.EngineIdentity.ProductVersion `
            -Name 'engine identity preserves observed product version'
    }
} finally {
    $script:EngineProductDatabaseSql = $savedDatabaseSql
}

function New-ObservationContext {
    return [pscustomobject]@{
        Connection = $null
        Contract = [pscustomobject]@{ Database = 'Dawnholder_Dev_Fixture'; ExecutionApproved = $true; G2 = 'fixture' }
        Sites = $sites
        Report = [pscustomobject]@{
            ScalarCalls = [Collections.Generic.List[object]]::new()
            Observations = [Collections.Generic.List[object]]::new()
            Atomicity = [Collections.Generic.List[object]]::new()
            StateComparisons = [Collections.Generic.List[object]]::new()
        }
    }
}
$context = New-ObservationContext
$context.Connection = New-FakeSqlConnection -Responder { param($mode, $sql, $values) return 1 }
$observer = New-EngineObserver -Context $context -Scenario 'unit' -Fault
Invoke-WithEngineObserver -Observer $observer -Action {
    $null = Invoke-DbNonQuery -Connection $context.Connection `
        -Sql 'INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(@version,@name,@hash)' -Parameters @{ version = 1 }
    $null = Invoke-DbScalar -Connection $context.Connection -Sql 'SELECT 1;'
}
Assert-True -Condition (-not $observer.Fired -and -not $observer.Armed) `
    -Name 'fault ignores version 1 metadata and ordinary scalar calls'
Assert-Throws -Action {
    Invoke-WithEngineObserver -Observer $observer -Action {
        $null = Invoke-DbNonQuery -Connection $context.Connection `
            -Sql 'INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(@version,@name,@hash)' `
            -Parameters @{ version = 2 }
        $null = Invoke-DbScalar -Connection $context.Connection -Sql 'SELECT 2;'
    }
} -Pattern 'injected failure' -Name 'fault fires on the next SQL call after successful version 2 metadata'
Assert-Equal -Expected 3 -Actual $context.Connection.Log.Count -Name 'faulted next call never reaches the provider'
Assert-Equal -Expected 4 -Actual $observer.FiredAtCall -Name 'fault records exact SQL call sequence'
Assert-Equal -Expected 2 -Actual $context.Report.ScalarCalls.Count `
    -Name 'scalar log includes both successful and injected failed scalar calls'
Assert-True -Condition ($null -ne $context.Report.ScalarCalls[1].Failure) `
    -Name 'failed scalar records no invented raw value'

# Fixed responses test scheduling and rollback calls, not SQL Server rollback semantics.
$script:StateReads = 0
$script:ChangeRestoredState = $false
function Get-EngineDatabaseState($Context) {
    $script:StateReads++
    $count = if ($script:ChangeRestoredState -and $script:StateReads -gt 1) { 1 } else { 0 }
    return [pscustomobject]@{
        Snapshot = ($fixedSnapshot | ConvertFrom-Json)
        History = @([pscustomobject]@{ Version = 1 })
        Objects = [pscustomobject]@{ CharacterAuthority = $null }
        Metadata = [pscustomobject]@{}
        TransactionCount = $count
    }
}
$baselineJson = '[{"Version":1,"Name":"001_initial.sql",' +
'"Checksum":"F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC"}]'
$script:FailBeforeArm = $false
$atomicResponder = {
    param($mode, $sql, $values)
    if ($script:FailBeforeArm) { throw 'fixture stops before arming' }
    if ($mode -ceq 'Scalar') { return $baselineJson }
    return 1
}
$context = New-ObservationContext
$context.Connection = New-FakeSqlConnection -Responder $atomicResponder
Assert-NoThrow -Action { Invoke-EngineAtomicity -Context $context } `
    -Name 'confirmed rehearsal permits product-owned run'
Assert-Equal -Expected 'ToolRehearsal|Product' -Actual ($context.Report.Atomicity.Owner -join '|') `
    -Name 'atomicity runs tool rehearsal before product ownership'
Assert-Equal -Expected 2 -Actual @($context.Connection.Log | Where-Object Mode -CEQ 'Rollback').Count `
    -Name 'both owners roll back on the injected failure'
Assert-Equal -Expected 0 -Actual @($context.Connection.Log | Where-Object Mode -CEQ 'Commit').Count `
    -Name 'atomicity path never commits'
$script:FailBeforeArm = $true
$context = New-ObservationContext
$context.Connection = New-FakeSqlConnection -Responder $atomicResponder
Assert-Throws -Action { Invoke-EngineAtomicity -Context $context } -Pattern 'rehearsal did not confirm' `
    -Name 'unconfirmed rehearsal stops before product-owned execution'
Assert-Equal -Expected 1 -Actual @($context.Connection.Log | Where-Object Mode -CEQ 'Begin').Count `
    -Name 'failed rehearsal starts only one transaction'
$script:FailBeforeArm = $false
$script:ChangeRestoredState = $true
$script:StateReads = 0
$context = New-ObservationContext
$context.Connection = New-FakeSqlConnection -Responder $atomicResponder
Assert-Throws -Action { Invoke-EngineAtomicity -Context $context } -Pattern 'Rollback state differs' `
    -Name 'unequal restored state stops all later atomicity work'
Assert-Equal -Expected 1 -Actual @($context.Connection.Log | Where-Object Mode -CEQ 'Begin').Count `
    -Name 'restoration mismatch prevents product-owned run'

# The suite appends only boundary doubles to a copy of the common helper. Both entry scripts are byte-identical.
# All external constructors and identity operations are refused; the narrow TestSupport fake receives SQL commands.
$entryFixture = New-DatabaseFixture -Name 'entry-fixture'
foreach ($file in @(Get-ChildItem -LiteralPath $script:ToolRoot -File -Filter '*.ps1')) {
    Copy-Item -LiteralPath $file.FullName -Destination $entryFixture
}
Copy-Item -LiteralPath (Join-Path $script:ToolRoot 'test-environment') -Destination $entryFixture -Recurse
$testSupport = Join-Path $PSScriptRoot 'TestSupport.ps1'
$doubleSource = @'

# Offline boundary doubles appended by PreservationEngine.Tests.ps1, never used by an engine execution.
. $env:ENGINE_CHECK_TEST_SUPPORT
$script:Case = [IO.File]::ReadAllText($env:ENGINE_CHECK_CASE) | ConvertFrom-Json
$script:OpenCount = 0
$script:LastConnection = $null
$script:FixtureBundle = Read-ModuleBundle -DatabaseRoot ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')))
$script:FixtureState = [pscustomobject]@{
    History = @(($script:Case.History | ConvertFrom-Json) | ForEach-Object { $_ })
    Recorded = @()
    Actual = @()
    Releases = @()
}
$script:FixtureDepth = 0
function New-FixtureActual($Module) {
    return [pscustomobject]@{
        ObjectName = $Module.ObjectName
        Kind = $Module.Kind
        DefinitionBytes = $Module.DefinitionBytes
        DefinitionChecksum = $Module.DefinitionChecksum
        ExecuteAsPrincipalId = $null
        AnsiNulls = $true
        QuotedIdentifier = $true
        SchemaBound = ($Module.Kind -ceq 'FN')
        OwnerPrincipalId = 1
        ExpectedOwnerPrincipalId = 1
    }
}
if ($script:FixtureState.History.Count -eq 4) {
    $script:FixtureState.Recorded = @($script:FixtureBundle.Modules |
            Select-Object ObjectName, Kind, SourceChecksum, DefinitionBytes, DefinitionChecksum)
    $script:FixtureState.Actual = @($script:FixtureBundle.Modules | ForEach-Object { New-FixtureActual -Module $_ })
    $script:FixtureState.Releases = @([pscustomobject]@{
            Version = 4
            ManifestChecksum = $script:FixtureBundle.ManifestChecksum
        })
}
function ConvertTo-FixtureRows($Rows, [string]$Sql) {
    $array = @($Rows | Where-Object { $null -ne $_ })
    if ($array.Count -eq 0 -and $Sql.StartsWith('SELECT ((')) { return [DBNull]::Value }
    return (ConvertTo-Json -InputObject $array -Depth 10 -Compress)
}
function Assert-TestEnvironmentExecutor { param($Contract) Assert-TestEnvironmentExecutionApproval -Contract $Contract }
function Assert-TestEnvironmentDatabaseIdentity { param($Master, $Manifest) }
function Assert-TestEnvironmentMarkers { param($Connection, $Manifest) }
function Open-TestEnvironmentDatabase { throw 'Offline connection factory must not be reached.' }
function Invoke-DatabaseSql { throw 'Offline typed SQL boundary must not be reached.' }
function New-Object {
    param([string]$TypeName, [object[]]$ArgumentList)
    if ($TypeName -match 'SqlConnection|SqlCommand') { throw 'Offline SQL construction refused.' }
    if ($null -eq $ArgumentList -or $ArgumentList.Count -eq 0) {
        Microsoft.PowerShell.Utility\New-Object -TypeName $TypeName
    } else {
        Microsoft.PowerShell.Utility\New-Object -TypeName $TypeName -ArgumentList $ArgumentList
    }
}
function Open-EngineObservedConnection($Context, [switch]$Master) {
    $script:OpenCount++
    $Context.Report.EngineIdentity = [pscustomobject]@{ Source = 'OFFLINE fixed engine identity input' }
    $connection = New-FakeSqlConnection -Database $Context.Manifest.Database -Responder {
        param($mode, $sql, $values)
        $state = $script:FixtureState
        if ($sql.Contains("name=N'Dawnholder.DatabaseTool'")) { return 'development-v1' }
        if ($sql.StartsWith('SELECT (SELECT COUNT(*)')) { return $script:Case.ExistingRows }
        if ($sql -ceq 'SELECT @@TRANCOUNT;') { return $script:FixtureDepth }
        if ($sql -ceq 'SELECT OBJECT_ID(@name);') {
            $version = if ($values.name -in @('dh.CharacterAuthority', 'dh.CharacterOperation')) { 2 } else { 3 }
            if ($state.History.Count -lt $version) { return [DBNull]::Value }
            return 100 + $version
        }
        if ($mode -ceq 'NonQuery') {
            if ($script:Case.WriteFailure) {
                throw (New-TestSqlException -Number 547 -Message 'Password=OFFLINE-PROVIDER-SENTINEL')
            }
            if ($sql.StartsWith('INSERT dh.SchemaVersion')) {
                $state.History += [pscustomobject]@{
                    Version = $values.version
                    Name = $values.name
                    Checksum = $values.hash
                }
            } elseif ($sql -ceq 'DELETE FROM dh.SchemaVersion;') {
                $state.History = @()
            } elseif ($sql -ceq 'DELETE FROM dh.ModuleRelease;') {
                $state.Releases = @()
            } elseif ($sql.StartsWith('DELETE FROM dh.ModuleDefinition')) {
                if ($script:Case.UnobservedStimulus) {
                    throw (New-TestSqlException -Number 547 -Message 'offline constraint refuses stimulus')
                }
                if ($sql.Contains('WHERE')) { $state.Recorded = @($state.Recorded | Select-Object -Skip 1) }
                else { $state.Recorded = @() }
            } elseif ($sql.StartsWith('UPDATE dh.ModuleDefinition SET DefinitionChecksum')) {
                $state.Recorded[0].DefinitionChecksum = '0' * 64
            } elseif ($sql.Contains('INSERT dh.ModuleRelease')) {
                $version = if ($sql.Contains('VALUES(3,')) { 3 } else { 4 }
                $state.Releases = @([pscustomobject]@{
                        Version = $version
                        ManifestChecksum = $script:FixtureBundle.ManifestChecksum
                    }) + $state.Releases
            } elseif ($sql.StartsWith('CREATE OR ALTER')) {
                $name = [regex]::Match($sql, '^CREATE OR ALTER (PROCEDURE|FUNCTION) (dh\.\w+)').Groups[2].Value
                $module = @($script:FixtureBundle.Modules | Where-Object ObjectName -CEQ $name)[0]
                $state.Actual += New-FixtureActual -Module $module
            } elseif ($sql.StartsWith('UPDATE dh.ModuleDefinition SET Kind')) {
                $state.Recorded += [pscustomobject]@{
                    ObjectName = $values.name
                    Kind = $values.kind
                    SourceChecksum = $values.source
                    DefinitionBytes = $values.bytes
                    DefinitionChecksum = $values.definition
                }
            }
            return 8
        }
        if ($sql.Contains('FROM dh.Account ORDER BY')) { return $script:Case.Snapshot.Account }
        if ($sql.Contains('FROM dh.Character ORDER BY')) { return $script:Case.Snapshot.Character }
        if ($sql.Contains('FROM dh.CharacterProgress ORDER BY')) { return $script:Case.Snapshot.CharacterProgress }
        if ($sql.Contains('COUNT(*) FROM dh.SchemaVersion')) { return $state.History.Count }
        if ($sql.Contains('COUNT(*) FROM dh.ModuleDefinition')) { return $state.Recorded.Count }
        if ($sql.Contains('COUNT(*) FROM dh.ModuleRelease')) { return $state.Releases.Count }
        if ($sql.Contains('COUNT(*) FROM sys.objects')) { return $state.Actual.Count }
        if ($sql.Contains('FROM dh.SchemaVersion ORDER BY')) {
            return (ConvertTo-FixtureRows -Rows $state.History -Sql $sql)
        }
        if ($sql.Contains('FROM dh.ModuleDefinition ORDER BY')) {
            return (ConvertTo-FixtureRows -Rows ($state.Recorded | Sort-Object ObjectName) -Sql $sql)
        }
        if ($sql.Contains('FROM sys.objects o JOIN')) {
            return (ConvertTo-FixtureRows -Rows ($state.Actual | Sort-Object ObjectName) -Sql $sql)
        }
        if ($sql.Contains('FROM dh.ModuleRelease r LEFT JOIN')) {
            $rows = @(foreach ($release in $state.Releases) {
                    $schema = @($state.History | Where-Object Version -EQ $release.Version)
                    [pscustomobject]@{
                        Version = $release.Version
                        ManifestChecksum = $release.ManifestChecksum
                        SchemaVersion = $(if ($schema.Count) { $release.Version } else { $null })
                    }
                })
            return (ConvertTo-FixtureRows -Rows $rows -Sql $sql)
        }
        throw 'Unexpected SQL at the offline entry boundary.'
    }
    $connection | Add-Member -MemberType ScriptMethod -Name BeginTransaction -Force -Value {
        $this.Log.Add([pscustomobject]@{ Mode = 'Begin'; Sql = '' })
        $script:FixtureDepth++
        $transaction = [pscustomobject]@{
            Connection = $this
            Owner = $this
            Before = (ConvertTo-Json -InputObject $script:FixtureState -Depth 12)
        }
        $transaction | Add-Member -MemberType ScriptMethod -Name Rollback -Value {
            $this.Owner.Log.Add([pscustomobject]@{ Mode = 'Rollback'; Sql = '' })
            $script:FixtureState = $this.Before | ConvertFrom-Json
            $script:FixtureDepth = if ($script:Case.RestoreMismatch) { 1 } else { 0 }
            $this.Connection = $null
        }
        $transaction | Add-Member -MemberType ScriptMethod -Name Commit -Value {
            $this.Owner.Log.Add([pscustomobject]@{ Mode = 'Commit'; Sql = '' })
            $script:FixtureDepth = 0
            $this.Connection = $null
        }
        $transaction | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
            $this.Owner.Log.Add([pscustomobject]@{ Mode = 'Dispose'; Sql = '' })
        }
        return $transaction
    }
    $connection | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
        if (-not $script:Case.EventPath) { throw 'Event path required.' }
        [pscustomobject]@{ Opens = $script:OpenCount; Log = @($this.Log.ToArray()) } |
            ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $script:Case.EventPath -Encoding UTF8
    }
    if (-not $Master) { $script:LastConnection = $connection }
    # Master disposal follows target disposal; keep the target's command events in the final file.
    if ($Master) {
        $connection | Add-Member -MemberType ScriptMethod -Name Dispose -Force -Value { }
    }
    return $connection
}
if ($script:Case.GuidCollision) {
    function New-PreservationIds($Contract) {
        return @([Guid]$Contract.AccountId) + @(1..5 | ForEach-Object { [Guid]::NewGuid() })
    }
}
'@
$helperPath = Join-Path $entryFixture 'test-environment/EngineCheck.Common.ps1'
Write-FixtureText -Path $helperPath -Text ([IO.File]::ReadAllText($helperPath) + $doubleSource)

function Invoke-EntryCase {
    param(
        [string]$Name,
        [string]$Action = 'Record',
        [string]$State = 'Baseline001',
        [string]$Tool = 'Invoke-PreservationCheck.ps1',
        [string]$Invalid = '',
        [int]$ExistingRows = 0,
        [switch]$WriteFailure,
        [switch]$GuidCollision,
        [switch]$UnobservedStimulus,
        [switch]$RestoreMismatch
    )
    $root = Join-Path $script:SuiteRoot $Name
    [void][IO.Directory]::CreateDirectory($root)
    $plan = New-OfflineContainerPlan -Root $root -Approved $true -G2 'offline-fixture-only'
    [void][IO.Directory]::CreateDirectory((Split-Path $plan.ManifestPath -Parent))
    $planPath = Join-Path $root 'plan.json'
    Write-FixtureText -Path $planPath -Text (ConvertTo-Json -InputObject $plan -Depth 10)
    $hash = (Get-FileHash -LiteralPath $planPath -Algorithm SHA256).Hash
    $contract = Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $planPath -ExpectedApprovalPlanHash $hash
    $manifest = New-TestEnvironmentManifest -Contract $contract -Database $contract.Database
    $manifest.State = $State
    $sources = @(Get-DatabaseMigrationSources -Phase Complete | Select-Object Version, Name, Checksum)
    $manifest.MigrationManifest = @()
    if ($State -ceq 'Installed') { $manifest.MigrationManifest = $sources }
    elseif ($State -cne 'Created') { $manifest.MigrationManifest = @($sources[0]) }
    $manifest.Steps = @([pscustomobject]@{
            Name = 'InstallBaseline001'
            Status = 'Done'
            Identity = [pscustomobject]@{ MigrationManifest = @($sources[0]) }
        })
    $snapshotPath = Join-Path $root 'snapshot.json'
    $snapshotInput = [ordered]@{
        Account = '[{"AccountId":"a"},{"AccountId":"b"},{"AccountId":"d"}]'
        Character = '[{"CharacterId":"c","Version":"0000000000000001"},' +
        '{"CharacterId":"e","Version":"0000000000000002"},{"CharacterId":"f","Version":"0000000000000003"}]'
        CharacterProgress = '[{"CharacterId":"c","Hp":0},{"CharacterId":"e","Hp":120}]'
    }
    if ($Action -cne 'Record') {
        $snapshot = [ordered]@{}
        foreach ($table in $snapshotInput.Keys) {
            $snapshot[$table] = @(($snapshotInput[$table] | ConvertFrom-Json) | ForEach-Object { $_ })
        }
        Write-FixtureText -Path $snapshotPath -Text (ConvertTo-Json -InputObject $snapshot -Depth 10)
        $manifest.Steps += [pscustomobject]@{
            Name = 'RecordPreservationData'
            Status = 'Done'
            Identity = [pscustomobject]@{
                SnapshotPath = $snapshotPath
                SHA256 = (Get-FileHash -LiteralPath $snapshotPath -Algorithm SHA256).Hash
            }
        }
    }
    Write-FixtureText -Path $manifest.ManifestPath -Text (ConvertTo-Json -InputObject $manifest -Depth 12)
    $casePath = Join-Path $root 'case.json'
    $eventPath = Join-Path $root 'events.json'
    $caseData = [ordered]@{
        ExistingRows = $ExistingRows
        WriteFailure = [bool]$WriteFailure
        GuidCollision = [bool]$GuidCollision
        UnobservedStimulus = [bool]$UnobservedStimulus
        RestoreMismatch = [bool]$RestoreMismatch
        EventPath = $eventPath
        Snapshot = $snapshotInput
        History = ConvertTo-Json -InputObject $manifest.MigrationManifest -Depth 5 -Compress
    }
    Write-FixtureText -Path $casePath -Text (ConvertTo-Json -InputObject $caseData -Depth 10)
    $env:ENGINE_CHECK_TEST_SUPPORT = $testSupport
    $env:ENGINE_CHECK_CASE = $casePath
    $database = $contract.Database
    switch ($Invalid) {
        'MissingPlan' { $planPath = Join-Path $root 'absent.json' }
        'Hash' { $hash = '0' * 64 }
        'Target' { $database = 'Dawnholder_Dev_Wrong' }
    }
    $resultPath = Join-Path $root 'result.json'
    $invocation = Invoke-PowerShellFile -File (Join-Path $entryFixture ('test-environment/' + $Tool)) -Arguments @(
        '-Action', $Action, '-ApprovalPlanPath', $planPath, '-ExpectedApprovalPlanHash', $hash,
        '-Database', $database, '-ManifestPath', $manifest.ManifestPath,
        '-EvidenceDirectory', $root, '-ResultPath', $resultPath, '-SnapshotPath', $snapshotPath
    )
    Write-FixtureText -Path (Join-Path $root 'stdout.txt') -Text $invocation.StdOut
    Write-FixtureText -Path (Join-Path $root 'stderr.txt') -Text $invocation.StdErr
    $result = if (Test-Path -LiteralPath $resultPath) { [IO.File]::ReadAllText($resultPath) | ConvertFrom-Json }
    $events = if (Test-Path -LiteralPath $eventPath) { [IO.File]::ReadAllText($eventPath) | ConvertFrom-Json }
    return [pscustomobject]@{
        ExitCode = $invocation.ExitCode
        Result = $result
        Events = $events
        Root = $root
        Manifest = ([IO.File]::ReadAllText($manifest.ManifestPath) | ConvertFrom-Json)
    }
}

foreach ($tool in @('Invoke-PreservationCheck.ps1', 'Test-EngineBoundary.ps1')) {
    $action = if ($tool -ceq 'Invoke-PreservationCheck.ps1') { 'Record' } else { 'BeforeComplete' }
    foreach ($invalid in @('MissingPlan', 'Hash', 'Target')) {
        $case = Invoke-EntryCase -Name ('guard-' + $action + '-' + $invalid) `
            -Tool $tool -Action $action -Invalid $invalid
        Assert-True -Condition ($case.ExitCode -ne 0 -and $null -eq $case.Events) `
            -Name ('actual ' + $action + ' refuses ' + $invalid + ' before any connection')
        Assert-Equal -Expected 'FAIL' -Actual $case.Result.Outcome `
            -Name ($action + ' writes bounded failure evidence for ' + $invalid)
    }
}
foreach ($state in @('Created', 'Installed')) {
    $case = Invoke-EntryCase -Name ('state-' + $state) -State $state
    Assert-True -Condition ($case.ExitCode -ne 0 -and $null -eq $case.Events) `
        -Name ('actual record entry refuses state ' + $state + ' before connection')
    Assert-True -Condition ($case.Result.Failure.Message -like '*requires manifest State Baseline001*') `
        -Name ('state ' + $state + ' fails for its intended reason')
}
foreach ($tool in @('Invoke-PreservationCheck.ps1', 'Test-EngineBoundary.ps1')) {
    $action = if ($tool -ceq 'Invoke-PreservationCheck.ps1') { 'Compare' } else { 'AfterComplete' }
    $case = Invoke-EntryCase -Name ('state-' + $action) -Tool $tool -Action $action
    Assert-True -Condition ($case.ExitCode -ne 0 -and $null -eq $case.Events) `
        -Name ('actual ' + $action + ' refuses Baseline001 before connection')
    Assert-True -Condition ($case.Result.Failure.Message -like '*requires manifest State Installed*') `
        -Name ($action + ' state rejection has the intended cause')
}
$case = Invoke-EntryCase -Name 'nonempty' -ExistingRows 1
Assert-True -Condition ($case.ExitCode -ne 0) -Name 'nonempty fixture stops the actual record entry'
Assert-Equal -Expected 0 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'NonQuery').Count `
    -Name 'nonempty guard executes no write SQL'
Assert-Equal -Expected 1 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'Rollback').Count `
    -Name 'nonempty guard releases its tool transaction through rollback'
$case = Invoke-EntryCase -Name 'guid-collision' -GuidCollision
Assert-True -Condition ($case.ExitCode -ne 0) -Name 'binding GUID collision stops the actual record entry'
Assert-Equal -Expected 0 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'NonQuery').Count `
    -Name 'GUID collision executes no write SQL'
$case = Invoke-EntryCase -Name 'write-failure' -WriteFailure
Assert-True -Condition ($case.ExitCode -ne 0) -Name 'provider write error fails the actual entry'
Assert-Equal -Expected 1 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'Rollback').Count `
    -Name 'write failure rolls back once'
Assert-Equal -Expected 'Failed' `
    -Actual @($case.Manifest.Steps | Where-Object Name -CEQ 'RecordPreservationData')[0].Status `
    -Name 'write failure durably journals Failed'
$output = [IO.File]::ReadAllText((Join-Path $case.Root 'result.json')) +
[IO.File]::ReadAllText((Join-Path $case.Root 'stderr.txt'))
Assert-True -Condition (-not $output.Contains('OFFLINE-PROVIDER-SENTINEL') -and -not $output.Contains('Password=')) `
    -Name 'provider secret sentinel is absent from result and executor output'
$case = Invoke-EntryCase -Name 'record-success'
Assert-Equal -Expected 0 -Actual $case.ExitCode -Name 'actual record entry completes against fake SqlClient'
Assert-Equal -Expected 'PASS' -Actual $case.Result.Outcome -Name 'record result records PASS'
Assert-Equal -Expected 'Baseline001' -Actual $case.Manifest.State -Name 'record preserves the Baseline001 state'
Assert-Equal -Expected 1 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'Commit').Count `
    -Name 'record commits exactly once'
$inserts = @($case.Events.Log | Where-Object Mode -CEQ 'NonQuery')
Assert-Equal -Expected 1 -Actual $inserts.Count -Name 'record submits one parameterized fixture batch'
Assert-True -Condition ($inserts[0].Sql.Contains('VALUES(@id3, @id0, 0), (@id4, @id0, 1), (@id5, @id1, 0)')) `
    -Name 'fixture request covers both classes and leaves the third account without characters'
Assert-True -Condition ($inserts[0].Sql.Contains('VALUES(@id3, 0, -12.5, 3.25, 120, 120, 1)') -and
    $inserts[0].Sql.Contains('(@id4, 3, 0.125, -0.5, 0, 75, 0)')) `
    -Name 'fixture request covers negative fractional positions maps zero and three and both HP boundaries'
$rowCounts = @($case.Result.Preservation.RowCounts.Account, $case.Result.Preservation.RowCounts.Character,
    $case.Result.Preservation.RowCounts.CharacterProgress)
Assert-Equal -Expected '3|3|2' -Actual ($rowCounts -join '|') `
    -Name 'record snapshot journals three accounts three characters and two progress rows'
$case = Invoke-EntryCase -Name 'compare-success' -Action Compare -State Installed
Assert-Equal -Expected 0 -Actual $case.ExitCode -Name 'actual compare entry completes against fake SqlClient'
Assert-Equal -Expected 0 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'NonQuery').Count `
    -Name 'preservation compare performs no SQL writes'
foreach ($action in @('BeforeComplete', 'AfterComplete')) {
    $state = if ($action -ceq 'BeforeComplete') { 'Baseline001' } else { 'Installed' }
    $case = Invoke-EntryCase -Name ('engine-' + $action) -Tool 'Test-EngineBoundary.ps1' -Action $action -State $state
    Assert-Equal -Expected 0 -Actual $case.ExitCode `
        -Name ('actual engine entry executes ' + $action + ' with fake SqlClient')
    Assert-Equal -Expected 'PASS' -Actual $case.Result.Outcome `
        -Name ($action + ' result reports completed observations')
    Assert-Equal -Expected 0 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'Commit').Count `
        -Name ($action + ' has no commits')
    if ($action -ceq 'BeforeComplete') {
        Assert-Equal -Expected 4 -Actual $case.Result.StateComparisons.Count `
            -Name 'before complete compares restored state after all four transactions'
        Assert-Equal -Expected 'ToolRehearsal|Product' -Actual ($case.Result.Atomicity.Owner -join '|') `
            -Name 'before complete reaches both atomicity owners through the actual entry'
        $natural = @($case.Result.Observations | Where-Object Scenario -CEQ 'FirstInstall')
        Assert-Equal -Expected 1 -Actual $natural[0].Verdict.IndependentCount `
            -Name 'first installation observes one baseline history row'
    } else {
        Assert-Equal -Expected 5 -Actual $case.Result.StateComparisons.Count `
            -Name 'after complete rolls back natural and four rejection transactions'
        Assert-Equal -Expected 4 -Actual @($case.Result.Rejections | Where-Object Outcome -CEQ 'PASS').Count `
            -Name 'actual entry observes all four fixed product rejections'
    }
}
$case = Invoke-EntryCase -Name 'engine-unobserved' -Tool 'Test-EngineBoundary.ps1' `
    -Action AfterComplete -State Installed -UnobservedStimulus
Assert-True -Condition ($case.ExitCode -ne 0) -Name 'unobserved stimuli cannot produce a completed engine verdict'
$unobserved = @($case.Result.Rejections | Where-Object Outcome -CEQ 'UNOBSERVED')
Assert-Equal -Expected 2 -Actual $unobserved.Count -Name 'constraint failures remain explicitly unobserved'
Assert-Equal -Expected 547 -Actual $unobserved[0].SqlFailure.SqlNumber `
    -Name 'unobserved stimulus preserves the SQL error number'
$case = Invoke-EntryCase -Name 'engine-mismatch' -Tool 'Test-EngineBoundary.ps1' `
    -Action AfterComplete -State Installed -RestoreMismatch
Assert-True -Condition ($case.ExitCode -ne 0) -Name 'actual entry stops when rollback state differs'
Assert-Equal -Expected 1 -Actual @($case.Events.Log | Where-Object Mode -CEQ 'Begin').Count `
    -Name 'actual rollback mismatch prevents every later rejection transaction'
Complete-TestSuite
