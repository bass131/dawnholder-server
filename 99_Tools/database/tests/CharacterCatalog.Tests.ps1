[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    [ValidateSet('Offline', 'FreshInstalled', 'UpgradeFromRelease4')][string]$Case = 'Offline',
    [string]$Endpoint,
    [string]$Database,
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash
)
# Requirement tests for the character catalog schema: goal 2026-10-17 character storage foundation
# ("make" item 4, "touch" migrations 005+ only with 001~004 unchanged, completion conditions 2 and 3) and
# character storage API v1 1.0.0 (test acceptance: empty install, empty release-4 upgrade, populated refusal).
# Offline (default) reads files only and never opens SQL: 001~004 keep their base-commit content, new migrations
# continue the numbering, and the product source list and module release move past release 4.
# FreshInstalled and UpgradeFromRelease4 run only inside an approved test environment, in the Test-Database.ps1
# entry order: reviewed plan hash -> exact target -> execution approval -> ready manifest -> owner connection.
# FreshInstalled inspects a database the product installer created from nothing.
# UpgradeFromRelease4 receives a database installed by the base-commit tool (release 4, no game rows). It proves
# that each populated fixture is refused with schema, history, release, permissions and rows unchanged, that a
# writer holding a row at check time or writing during the upgrade cannot slip through, then upgrades and replays.
# Expected values are base-commit hashes and the goal's table names, never values recomputed by the installer.
# A refusal that rolls back is not distinguishable here from a refusal before the first change; that order is a
# review item of the independent verifier.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite ('character-catalog-' + $Case.ToLowerInvariant())

# git show 48aabbd5d23c8ec7021ff22282c90f295db64c67:99_Tools/database/migrations/<name> | sha256sum
# (the base commit of the goal; LF content without BOM, the same text Get-MigrationText reads).
$baseMigrations = [ordered]@{
    '001_initial.sql' = 'F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC'
    '002_persistence_metadata.sql' = '867F8CC52350029EE15E753EC9134DDB7F7FAD91D5A9383D0F2209EAAD2C3599'
    '003_module_metadata.sql' = 'A4F271547EC0E2E700413F51D8281A361D8654D383996E73F9476E4540A40F9B'
    '004_module_release.sql' = '2B9480C378AFBFFD7FA09E45BC2C416977D70B3BFD5436D5DDD3A1C24CB207B0'
}
# goal "make" item 1: the whole-flow tables. Column sets are implementation decisions and are not asserted.
$flowTables = @(
    'Account', 'Character', 'CharacterProgress', 'CharacterWallet', 'CharacterWeapon', 'Guild', 'GuildMember'
)
# Rows of these tables make a release-4 database "populated" (goal completion condition 3 and the no-migration rule).
# Each table maps to its release-4 key so row snapshots compare in a stable order.
$gameTables = [ordered]@{
    Account = 'AccountId'
    Character = 'CharacterId'
    CharacterProgress = 'CharacterId'
    CharacterAuthority = 'SlotId'
    CharacterOperation = 'OperationId'
}

function Get-NormalizedFileHash {
    param([Parameter(Mandatory)][string]$Path)
    # Content identity across CRLF/LF checkouts: UTF-8 without BOM, CRLF folded to LF.
    $text = [IO.File]::ReadAllText($Path, (New-Object Text.UTF8Encoding($false))).TrimStart([char]0xFEFF)
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($text.Replace("`r`n", "`n"))
    return Get-Sha256Hex -Bytes $bytes
}

function Test-OfflineMigrationFiles {
    $migrationRoot = Join-Path $script:ToolRoot 'migrations'
    $names = @(Get-ChildItem -LiteralPath $migrationRoot -File -Filter '*.sql' | Sort-Object Name |
            ForEach-Object { $_.Name })
    foreach ($name in $baseMigrations.Keys) {
        $path = Join-Path $migrationRoot $name
        $exists = [IO.File]::Exists($path)
        Assert-True -Condition $exists -Name "base migration $name still exists"
        if ($exists) {
            Assert-Equal `
                -Expected $baseMigrations[$name] `
                -Actual (Get-NormalizedFileHash -Path $path) `
                -Name "base migration $name keeps its base-commit content"
        }
    }
    Assert-Equal `
        -Expected (@($baseMigrations.Keys) -join '|') `
        -Actual (@($names | Select-Object -First 4) -join '|') `
        -Name 'migrations 001~004 keep their names and order'
    $added = @($names | Select-Object -Skip 4)
    Assert-True -Condition ($added.Count -ge 1) -Name 'a new migration follows 004' -Detail ($added -join ', ')
    for ($index = 0; $index -lt $added.Count; $index++) {
        $prefix = '{0:D3}_' -f ($index + 5)
        $continues = $added[$index].StartsWith($prefix) -and $added[$index] -cmatch '^\d{3}_[a-z0-9_]+\.sql$'
        Assert-True -Condition $continues -Name "new migration $($added[$index]) continues the numbering as $prefix"
    }
}

function Test-OfflineProductSources {
    # The product installer's own source list and module bundle are the real entry; they are read, not executed.
    $sources = @()
    try {
        $sources = @(Get-DatabaseMigrationSources -Phase Complete)
    }
    catch {
        Add-TestResult -Name 'installer source list is readable' -Outcome FAIL -Detail $_.Exception.Message
    }
    Assert-True -Condition ($sources.Count -gt 4) -Name 'installer source list includes migrations after 004' `
        -Detail ('sources=' + $sources.Count)
    $index = 0
    foreach ($name in $baseMigrations.Keys) {
        if ($index -lt $sources.Count) {
            Assert-Equal -Expected ($name + '|' + $baseMigrations[$name]) `
                -Actual ($sources[$index].Name + '|' + $sources[$index].Checksum) `
                -Name "installer source $($index + 1) is the unchanged $name"
        }
        $index++
    }
    $release = 0
    try {
        $release = (Read-ModuleBundle -DatabaseRoot $script:ToolRoot).ReleaseVersion
    }
    catch {
        Add-TestResult -Name 'module bundle is readable' -Outcome FAIL -Detail $_.Exception.Message
    }
    Assert-True `
        -Condition ($release -gt 4) `
        -Name 'module bundle declares a release after 4' `
        -Detail ('release=' + $release)
}

function Get-JsonPart {
    param([Parameter(Mandatory)]$Connection, [Parameter(Mandatory)][string]$Sql)
    # FOR JSON inside ISNULL((...)) is one nvarchar(max) value, never split across result rows.
    return [string](Invoke-DbScalar -Connection $Connection -Sql ("SELECT ISNULL(({0}), N'[]');" -f $Sql))
}

function Get-CatalogSnapshot {
    param([Parameter(Mandatory)]$Connection)
    $rowParts = @($gameTables.Keys | ForEach-Object {
            "(SELECT * FROM dh.[$_] ORDER BY [$($gameTables[$_])] FOR JSON PATH, INCLUDE_NULL_VALUES) AS [$_]"
        }) -join ",`n    "
    return [ordered]@{
        History = Get-JsonPart -Connection $Connection -Sql @'
SELECT Version, Name, Checksum FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH
'@
        Release = Get-JsonPart -Connection $Connection -Sql @'
SELECT Version, ManifestChecksum FROM dh.ModuleRelease ORDER BY Version FOR JSON PATH
'@
        Definitions = Get-JsonPart -Connection $Connection -Sql @'
SELECT ObjectName, Kind, SourceChecksum, DefinitionBytes, DefinitionChecksum
FROM dh.ModuleDefinition ORDER BY ObjectName FOR JSON PATH
'@
        Objects = Get-JsonPart -Connection $Connection -Sql @'
SELECT o.name AS Name, o.type AS Type, OBJECT_DEFINITION(o.object_id) AS Definition,
    (SELECT c.name AS Name, TYPE_NAME(c.user_type_id) AS Type, c.max_length AS Length, c.is_nullable AS Nullable
     FROM sys.columns AS c WHERE c.object_id = o.object_id ORDER BY c.column_id FOR JSON PATH) AS Columns
FROM sys.objects AS o
WHERE o.schema_id = SCHEMA_ID(N'dh')
ORDER BY o.name
FOR JSON PATH
'@
        Permissions = Get-JsonPart -Connection $Connection -Sql @'
SELECT p.class_desc AS Class, OBJECT_SCHEMA_NAME(p.major_id) AS SchemaName, OBJECT_NAME(p.major_id) AS ObjectName,
    USER_NAME(p.grantee_principal_id) AS Grantee, p.permission_name AS Permission, p.state_desc AS State
FROM sys.database_permissions AS p
ORDER BY Class, SchemaName, ObjectName, Grantee, Permission
FOR JSON PATH
'@
        Rows = Get-JsonPart -Connection $Connection -Sql ("SELECT`n    " + $rowParts + "`nFOR JSON PATH")
    }
}

function Assert-SnapshotUnchanged {
    param(
        [Parameter(Mandatory)]$Before,
        [Parameter(Mandatory)]$After,
        [Parameter(Mandatory)][string]$Label,
        [string[]]$Parts = @('History', 'Release', 'Definitions', 'Objects', 'Permissions', 'Rows')
    )
    foreach ($part in $Parts) {
        Assert-True -Condition ($Before[$part] -ceq $After[$part]) -Name "$Label leaves $part unchanged"
    }
}

function Get-GameRowCount {
    param([Parameter(Mandatory)]$Connection)
    $sum = @($gameTables.Keys | ForEach-Object { "(SELECT COUNT_BIG(*) FROM dh.[$_])" }) -join ' + '
    return [long](Invoke-DbScalar -Connection $Connection -Sql ('SELECT ' + $sum + ';'))
}

function Get-SqlNumber {
    param([Parameter(Mandatory)][scriptblock]$Action)
    # Invoke-Db* keep only the CLR Int32 SQL number in Data['DatabaseSqlNumber'] (SqlError.Common.ps1).
    try {
        $null = & $Action
    }
    catch {
        $errorObject = $_.Exception
        while ($null -ne $errorObject) {
            if ($errorObject.Data['DatabaseSqlNumber'] -is [int]) { return $errorObject.Data['DatabaseSqlNumber'] }
            $errorObject = $errorObject.InnerException
        }
        return -1
    }
    return 0
}

function Test-InstalledCatalog {
    param([Parameter(Mandatory)]$Connection, [Parameter(Mandatory)][string]$Label)
    $sources = @(Get-DatabaseMigrationSources -Phase Complete)
    $history = @(ConvertFrom-DatabaseJsonRows -Json (Get-CatalogSnapshot -Connection $Connection).History)
    Assert-Equal -Expected $sources.Count -Actual $history.Count -Name "$Label records every installer migration once"
    $index = 0
    foreach ($name in $baseMigrations.Keys) {
        $row = if ($index -lt $history.Count) { $history[$index] } else { $null }
        Assert-Equal -Expected ('{0}|{1}|{2}' -f ($index + 1), $name, $baseMigrations[$name]) `
            -Actual $(if ($row) { '{0}|{1}|{2}' -f $row.Version, $row.Name, $row.Checksum } else { 'missing' }) `
            -Name "$Label history row $($index + 1) is the unchanged $name"
        $index++
    }
    foreach ($table in $flowTables) {
        $lookup = @{
            Connection = $Connection
            Sql = 'SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N''dh'') AND name = @name;'
            Parameters = @{ name = $table }
        }
        $exists = [int](Invoke-DbScalar @lookup)
        Assert-Equal -Expected 1 -Actual $exists -Name "$Label has table dh.$table"
    }
    Assert-Equal `
        -Expected 0 `
        -Actual (Get-GameRowCount -Connection $Connection) `
        -Name "$Label creates or adopts no game rows"
}

function Test-ReplayIsIdempotent {
    param([Parameter(Mandatory)]$Connection, [Parameter(Mandatory)]$Contract, [Parameter(Mandatory)][string]$Label)
    $before = Get-CatalogSnapshot -Connection $Connection
    $message = Get-ThrownMessage -Action {
        Invoke-Migrations -Connection $Connection -Phase Complete -Contract $Contract
    }
    $after = Get-CatalogSnapshot -Connection $Connection
    Assert-True -Condition ($null -eq $message) -Name "$Label replay succeeds" -Detail ([string]$message)
    Assert-SnapshotUnchanged -Before $before -After $after -Label "$Label replay"
}

function Test-PopulatedFixtureIsRefused {
    param(
        [Parameter(Mandatory)]$Connection,
        [Parameter(Mandatory)]$Contract,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$InsertSql,
        [Parameter(Mandatory)][string]$DeleteSql
    )
    $values = @{ account = [Guid]::NewGuid(); character = [Guid]::NewGuid(); operation = [Guid]::NewGuid() }
    [void](Invoke-DbNonQuery -Connection $Connection -Sql $InsertSql -Parameters $values)
    try {
        $before = Get-CatalogSnapshot -Connection $Connection
        $message = Get-ThrownMessage -Action {
            Invoke-Migrations -Connection $Connection -Phase Complete -Contract $Contract
        }
        $after = Get-CatalogSnapshot -Connection $Connection
        Assert-True -Condition ($null -ne $message) -Name "$Label is refused" -Detail ([string]$message)
        Assert-SnapshotUnchanged -Before $before -After $after -Label "$Label refusal"
    }
    finally {
        # The fixture rows belong to this test; the next case starts from the same empty release-4 database.
        [void](Invoke-DbNonQuery -Connection $Connection -Sql $DeleteSql -Parameters $values)
    }
    Assert-Equal -Expected 0 -Actual (Get-GameRowCount -Connection $Connection) -Name "$Label fixture removed"
}

function Start-InstallerRunspace {
    param([Parameter(Mandatory)]$Contract, [Parameter(Mandatory)]$Manifest, [Parameter(Mandatory)][string]$Database)
    # A second owner connection in this process runs the product installer while the test holds a writer open.
    $shell = [powershell]::Create()
    [void]$shell.AddScript({
            param($ToolRoot, $Contract, $Manifest, $Database)
            Set-StrictMode -Version Latest
            $ErrorActionPreference = 'Stop'
            . (Join-Path $ToolRoot 'test-environment/Environment.Common.ps1')
            . (Join-Path $ToolRoot 'Database.Common.ps1')
            $connection = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $Manifest -Database $Database
            try {
                $null = Invoke-Migrations -Connection $connection -Phase Complete -Contract $Contract
                'Applied'
            }
            catch {
                'Refused: ' + $_.Exception.Message
            }
            finally {
                $connection.Dispose()
            }
        })
    [void]$shell.AddParameter('ToolRoot', $script:ToolRoot)
    [void]$shell.AddParameter('Contract', $Contract)
    [void]$shell.AddParameter('Manifest', $Manifest)
    [void]$shell.AddParameter('Database', $Database)
    return [pscustomobject]@{ Shell = $shell; Async = $shell.BeginInvoke() }
}

function Test-WriterAtCheckTimeIsNotAdopted {
    param($Connection, $Contract, $Manifest, [string]$Database)
    $writer = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $Manifest -Database $Database
    $values = @{ account = [Guid]::NewGuid(); character = [Guid]::NewGuid() }
    $baseline = Get-CatalogSnapshot -Connection $Connection
    $writeTransaction = $writer.BeginTransaction()
    $installer = $null
    try {
        [void](Invoke-DbNonQuery -Connection $writer -Transaction $writeTransaction -Parameters $values -Sql @'
INSERT dh.Account(AccountId) VALUES(@account);
INSERT dh.Character(CharacterId, AccountId, Class) VALUES(@character, @account, 0);
'@)
        $installer = Start-InstallerRunspace -Contract $Contract -Manifest $Manifest -Database $Database
        # Give the installer time to reach its check while the row is uncommitted, then commit the row.
        $finishedFirst = $installer.Async.AsyncWaitHandle.WaitOne([TimeSpan]::FromSeconds(3))
        $writeTransaction.Commit()
        $settled = $installer.Async.AsyncWaitHandle.WaitOne([TimeSpan]::FromSeconds(60))
        $result = 'NotSettled'
        if ($settled) {
            try {
                $result = [string](@($installer.Shell.EndInvoke($installer.Async)) -join '')
            }
            catch {
                $result = 'InstallerError: ' + $_.Exception.Message
            }
        }
        $after = Get-CatalogSnapshot -Connection $Connection
        Add-TestResult -Name 'writer at check time: installer finished before the writer committed' -Outcome OBSERVED `
            -Detail ([string]$finishedFirst)
        Assert-True `
            -Condition ($result.StartsWith('Refused')) `
            -Name 'writer at check time makes the upgrade refuse' `
            -Detail $result
        Assert-SnapshotUnchanged -Before $baseline -After $after -Label 'writer at check time' `
            -Parts @('History', 'Release', 'Definitions', 'Objects', 'Permissions')
    }
    finally {
        if ($null -ne $writeTransaction.Connection) { $writeTransaction.Rollback() }
        $writeTransaction.Dispose()
        if ($null -ne $installer) { $installer.Shell.Dispose() }
        [void](Invoke-DbNonQuery -Connection $writer -Parameters $values -Sql @'
DELETE dh.Character WHERE CharacterId = @character;
DELETE dh.Account WHERE AccountId = @account;
'@)
        $writer.Dispose()
    }
    Assert-Equal `
        -Expected 0 `
        -Actual (Get-GameRowCount -Connection $Connection) `
        -Name 'writer at check time fixture removed'
}

function Test-WriterDuringUpgradeWaits {
    param($Connection, $Contract, $Manifest, [string]$Database)
    $baseline = Get-CatalogSnapshot -Connection $Connection
    $writer = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $Manifest -Database $Database
    $values = @{ account = [Guid]::NewGuid(); character = [Guid]::NewGuid() }
    $upgrade = $Connection.BeginTransaction()
    try {
        $message = Get-ThrownMessage -Action {
            Invoke-Migrations -Connection $Connection -Transaction $upgrade -Phase Complete -Contract $Contract
        }
        Assert-True `
            -Condition ($null -eq $message) `
            -Name 'upgrade applies inside the held transaction' `
            -Detail ([string]$message)
        $accountWait = Get-SqlNumber -Action {
            Invoke-DbNonQuery -Connection $writer -Parameters $values -Sql @'
SET LOCK_TIMEOUT 1000;
INSERT dh.Account(AccountId) VALUES(@account);
'@
        }
        $authorityWait = Get-SqlNumber -Action {
            Invoke-DbNonQuery -Connection $writer -Parameters $values -Sql @'
SET LOCK_TIMEOUT 1000;
INSERT dh.CharacterAuthority(SlotId, AccountId, CharacterId, Fence, OwnerKind, OwnerId, LastSequence, ChangedUtc)
VALUES(1, @account, @character, 0, 0, NULL, 0, SYSUTCDATETIME());
'@
        }
        # 1222: lock request time out. Any other result means the writer was not held by the upgrade.
        Assert-Equal -Expected 1222 -Actual $accountWait -Name 'Account writer waits for the in-flight upgrade'
        Assert-Equal `
            -Expected 1222 `
            -Actual $authorityWait `
            -Name 'CharacterAuthority writer waits for the in-flight upgrade'
    }
    finally {
        if ($null -ne $upgrade.Connection) { $upgrade.Rollback() }
        $upgrade.Dispose()
        # A writer that was not held committed its row; remove it so the failure stays local to this case.
        [void](Invoke-DbNonQuery -Connection $writer -Parameters $values -Sql @'
DELETE dh.CharacterAuthority WHERE CharacterId = @character;
DELETE dh.Account WHERE AccountId = @account;
'@)
        $writer.Dispose()
    }
    $after = Get-CatalogSnapshot -Connection $Connection
    Assert-SnapshotUnchanged -Before $baseline -After $after -Label 'rolled-back upgrade with a waiting writer'
}

function Test-ReleaseFourPrecondition {
    param([Parameter(Mandatory)]$Connection)
    $history = @(ConvertFrom-DatabaseJsonRows -Json (Get-CatalogSnapshot -Connection $Connection).History)
    $observed = @($history | ForEach-Object { $_.Name + '|' + $_.Checksum }) -join ','
    $expected = @($baseMigrations.Keys | ForEach-Object { $_ + '|' + $baseMigrations[$_] }) -join ','
    $release = [int](Invoke-DbScalar `
            -Connection $Connection `
            -Sql 'SELECT ISNULL(MAX(Version), 0) FROM dh.ModuleRelease;')
    $rows = Get-GameRowCount -Connection $Connection
    $ready = $observed -ceq $expected -and $release -eq 4 -and $rows -eq 0
    Assert-True -Condition $ready -Name 'fixture is the base-commit release 4 without game rows' `
        -Detail ('history=' + $observed + ' release=' + $release + ' rows=' + $rows)
    return $ready
}

if ($Case -eq 'Offline') {
    . (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
    . (Join-Path $script:ToolRoot 'Database.Common.ps1')
    Set-OfflineStubs
    Test-OfflineMigrationFiles
    Test-OfflineProductSources
    Complete-TestSuite
}

# Database cases: the reviewed plan, exact target and execution approval precede any connection object.
. (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
$contract = Read-TestEnvironmentApprovalPlan `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
if ([string]::IsNullOrWhiteSpace($Endpoint)) {
    throw 'Supply the explicit exact approved database and container endpoint; no fallback.'
}
Assert-TestEnvironmentTarget -Contract $contract -Database $Database -Endpoint $Endpoint
Assert-TestEnvironmentExecutionApproval -Contract $contract
. (Join-Path $script:ToolRoot 'Database.Common.ps1')
$manifest = Read-TestEnvironmentManifest -Contract $contract -Database $Database -ManifestPath $contract.ManifestPath
Assert-TestEnvironmentReadyForStep -Manifest $manifest
$connection = Open-TestEnvironmentDatabase -Contract $contract -Manifest $manifest -Database $Database
try {
    Assert-DatabaseOwner -Connection $connection
    if ($Case -eq 'FreshInstalled') {
        Test-InstalledCatalog -Connection $connection -Label 'fresh install'
        Test-ReplayIsIdempotent -Connection $connection -Contract $contract -Label 'fresh install'
    }
    elseif (Test-ReleaseFourPrecondition -Connection $connection) {
        $populated = @(
            @{
                Label = 'release 4 with a Character row'
                Insert = @'
INSERT dh.Account(AccountId) VALUES(@account);
INSERT dh.Character(CharacterId, AccountId, Class) VALUES(@character, @account, 1);
INSERT dh.CharacterProgress(CharacterId, MapId, PositionX, PositionY, Hp, MaxHp)
VALUES(@character, 0, 3.5, 0, 80, 80);
'@
                Delete = @'
DELETE dh.CharacterProgress WHERE CharacterId = @character;
DELETE dh.Character WHERE CharacterId = @character;
DELETE dh.Account WHERE AccountId = @account;
'@
            },
            @{
                Label = 'release 4 with a CharacterAuthority row'
                Insert = @'
INSERT dh.CharacterAuthority(SlotId, AccountId, CharacterId, Fence, OwnerKind, OwnerId, LastSequence, ChangedUtc)
VALUES(1, @account, @character, 0, 0, NULL, 0, SYSUTCDATETIME());
'@
                Delete = 'DELETE dh.CharacterAuthority WHERE CharacterId = @character;'
            },
            @{
                # 002 ties every receipt to the authority row (FK on SlotId), so a receipt cannot exist alone.
                Label = 'release 4 with a CharacterOperation receipt'
                Insert = @'
INSERT dh.CharacterAuthority(SlotId, AccountId, CharacterId, Fence, OwnerKind, OwnerId, LastSequence, ChangedUtc)
VALUES(1, @account, @character, 0, 0, NULL, 0, SYSUTCDATETIME());
INSERT dh.CharacterOperation(OperationId, SlotId, Kind, PayloadVersion, Payload, Outcome, ResultCode, ResultSnapshot,
    RecordedUtc)
VALUES(@operation, 1, 1, 1, 0x01, 2, 200, NULL, SYSUTCDATETIME());
'@
                Delete = @'
DELETE dh.CharacterOperation WHERE OperationId = @operation;
DELETE dh.CharacterAuthority WHERE CharacterId = @character;
'@
            },
            @{
                # goal completion condition 3: unmapped Account-only data is not adopted silently either.
                Label = 'release 4 with an Account row only'
                Insert = 'INSERT dh.Account(AccountId) VALUES(@account);'
                Delete = 'DELETE dh.Account WHERE AccountId = @account;'
            }
        )
        foreach ($fixture in $populated) {
            $refusal = @{
                Connection = $connection
                Contract = $contract
                Label = $fixture.Label
                InsertSql = $fixture.Insert
                DeleteSql = $fixture.Delete
            }
            Test-PopulatedFixtureIsRefused @refusal
        }
        $concurrency = @{ Connection = $connection; Contract = $contract; Manifest = $manifest; Database = $Database }
        Test-WriterAtCheckTimeIsNotAdopted @concurrency
        Test-WriterDuringUpgradeWaits @concurrency
        # The same database and tool now succeed: the refusals above were caused by the rows, not by the target.
        $upgradeMessage = Get-ThrownMessage -Action {
            Invoke-Migrations -Connection $connection -Phase Complete -Contract $contract
        }
        Assert-True `
            -Condition ($null -eq $upgradeMessage) `
            -Name 'empty release 4 upgrades' `
            -Detail ([string]$upgradeMessage)
        Test-InstalledCatalog -Connection $connection -Label 'release 4 upgrade'
        Test-ReplayIsIdempotent -Connection $connection -Contract $contract -Label 'release 4 upgrade'
    }
}
finally {
    $connection.Dispose()
}
Complete-TestSuite
