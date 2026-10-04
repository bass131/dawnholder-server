[CmdletBinding()]
param(
    [string]$Instance,
    [string]$Database,
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash
)
# The reviewed plan, exact target and execution approval must all precede any connection attempt.
. (Join-Path $PSScriptRoot 'test-environment/Environment.Common.ps1')
$Contract = Read-TestEnvironmentApprovalPlan `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
if ([string]::IsNullOrWhiteSpace($Instance)) {
    throw 'Supply the explicit exact approved database and local instance; no fallback.'
}
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database -Instance $Instance
Assert-TestEnvironmentExecutionApproval -Contract $Contract
. (Join-Path $PSScriptRoot 'Database.Common.ps1')

function Assert-Equal($Expected, $Actual, [string]$Label) {
    if ($Expected -ne $Actual) { throw "$Label failed (expected $Expected, actual $Actual)." }
    Write-Output "PASS: $Label"
}
function Assert-SqlError([scriptblock]$Action, [int[]]$Numbers, [string]$Label) {
    try { & $Action | Out-Null }
    catch {
        # Invoke-Db* replace provider errors with a safe error whose Data keeps only the CLR Int32 SQL number
        # (SqlError.Common.ps1). Raw provider exceptions, message text and non-Int32 values are not SQL numbers here.
        $number = $null
        $errorObject = $_.Exception
        while ($null -ne $errorObject -and $null -eq $number) {
            if ($errorObject.Data['DatabaseSqlNumber'] -is [int]) { $number = $errorObject.Data['DatabaseSqlNumber'] }
            $errorObject = $errorObject.InnerException
        }
        if ($null -eq $number -or $number -notin $Numbers) { throw }
        Write-Output "PASS: $Label (SQL $number)"
        return
    }
    throw "$Label unexpectedly succeeded."
}
function Assert-Rejected([scriptblock]$Action, [string]$Pattern, [string]$Label) {
    try { & $Action | Out-Null }
    catch {
        if ($_.Exception.Message -notmatch $Pattern) { throw }
        Write-Output "PASS: $Label"
        return
    }
    throw "$Label unexpectedly succeeded."
}

Assert-Rejected { Open-LocalDatabase 'remote\SQLEXPRESS' $Database } 'Instance must' 'remote target rejected before connection'
Assert-Rejected { Open-LocalDatabase $Instance 'GameDB' } 'Only Dawnholder_Dev' 'existing unrelated database rejected before connection'

$connection = Open-LocalDatabase $Instance $Database
$other = $null
$transaction = $null
$accountId = [Guid]::NewGuid()
$characterId = [Guid]::NewGuid()
$argsSql = @{account = $accountId; character = $characterId; missing = [Guid]::NewGuid() }
try {
    Assert-DatabaseOwner $connection
    [void](Invoke-DbNonQuery $connection (Get-MigrationText (Join-Path $PSScriptRoot 'verify-schema.sql')))
    Write-Output 'PASS: column types/nullability, PK/unique/index, FK and trusted constraints catalog'
    $transaction = $connection.BeginTransaction()
    # Also verifies checksums, unknown versions and application locking; never commit test data.
    Invoke-Migrations -Connection $connection -Transaction $transaction -Phase Complete -Contract $Contract
    [void](Invoke-DbNonQuery $connection 'SET XACT_ABORT OFF;' @{} $transaction)
    [void](Invoke-DbNonQuery $connection @'
INSERT dh.Account(AccountId) VALUES(@account);
INSERT dh.Character(CharacterId,AccountId,Class) VALUES(@character,@account,0);
INSERT dh.CharacterProgress(CharacterId,MapId,PositionX,PositionY,Hp,MaxHp)
VALUES(@character,1,12.5,-2.25,140,150);
'@ $argsSql $transaction)
    Assert-Equal 1 (Invoke-DbScalar $connection @'
SELECT COUNT(*) FROM dh.CharacterProgress p JOIN dh.Character c ON c.CharacterId=p.CharacterId
JOIN dh.Account a ON a.AccountId=c.AccountId
WHERE a.AccountId=@account AND p.CharacterId=@character AND c.Class=0 AND p.MapId=1
AND p.PositionX=12.5 AND p.PositionY=-2.25 AND p.Hp=140 AND p.MaxHp=150 AND p.BossUnlocked=0
AND p.SavedUtc IS NOT NULL AND c.CreatedUtc IS NOT NULL AND a.CreatedUtc IS NOT NULL
'@ $argsSql $transaction) 'insert/read joins, coordinates, HP and defaults'

    $beforeReplay = Invoke-DbScalar $connection 'SELECT CONVERT(varchar(18),Version,1) FROM dh.CharacterProgress WHERE CharacterId=@character' $argsSql $transaction
    Invoke-Migrations -Connection $connection -Transaction $transaction -Phase Complete -Contract $Contract
    Invoke-Migrations -Connection $connection -Transaction $transaction -Phase Complete -Contract $Contract
    Assert-Equal $beforeReplay (Invoke-DbScalar $connection 'SELECT CONVERT(varchar(18),Version,1) FROM dh.CharacterProgress WHERE CharacterId=@character AND Hp=140' $argsSql $transaction) 'migration replay preserves populated row and rowversion'
    Assert-Equal 1 (Invoke-DbScalar $connection 'SELECT COUNT(*) FROM dh.SchemaVersion WHERE Version=1' @{} $transaction) 'migration replay has one version row'
    $transaction.Save('ChecksumGuard')
    [void](Invoke-DbNonQuery $connection "UPDATE dh.SchemaVersion SET Checksum=REPLICATE('0',64) WHERE Version=1" @{} $transaction)
    Assert-Rejected -Action {
        Invoke-Migrations -Connection $connection -Transaction $transaction -Phase Complete -Contract $Contract
    } -Pattern '^Migration history has a hole, unknown name/version or checksum drift; do not rewrite applied history\.$' `
        -Label 'changed migration history rejected'
    $transaction.Rollback('ChecksumGuard')
    $transaction.Save('UnknownVersionGuard')
    [void](Invoke-DbNonQuery $connection "INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(999999,N'unknown',REPLICATE('0',64))" @{} $transaction)
    Assert-Rejected -Action {
        Invoke-Migrations -Connection $connection -Transaction $transaction -Phase Complete -Contract $Contract
    } -Pattern '^Database has unknown/newer migrations; use the matching tool revision without downgrading\.$' `
        -Label 'unknown database migration rejected'
    $transaction.Rollback('UnknownVersionGuard')
    [void](Invoke-DbNonQuery $connection 'SET XACT_ABORT OFF;' @{} $transaction)

    Assert-SqlError { Invoke-DbNonQuery $connection 'INSERT dh.Account(AccountId) VALUES(@account)' $argsSql $transaction } @(2627, 2601) 'account PK duplicate rejected'
    Assert-SqlError { Invoke-DbNonQuery $connection 'INSERT dh.Character(CharacterId,AccountId,Class) VALUES(@character,@account,1)' $argsSql $transaction } @(2627, 2601) 'character PK duplicate rejected'
    Assert-SqlError { Invoke-DbNonQuery $connection 'INSERT dh.CharacterProgress(CharacterId,MapId,PositionX,PositionY,Hp,MaxHp) VALUES(@character,0,0,0,80,80)' $argsSql $transaction } @(2627, 2601) 'progress PK duplicate rejected'
    Assert-SqlError { Invoke-DbNonQuery $connection "INSERT dh.SchemaVersion(Version,Name,Checksum) SELECT 999999,Name,Checksum FROM dh.SchemaVersion WHERE Version=1" @{} $transaction } @(2627, 2601) 'migration name unique constraint'
    Assert-SqlError { Invoke-DbNonQuery $connection 'INSERT dh.Character(CharacterId,AccountId,Class) VALUES(@missing,@missing,0)' $argsSql $transaction } @(547) 'orphan character rejected'
    Assert-SqlError { Invoke-DbNonQuery $connection 'INSERT dh.CharacterProgress(CharacterId,MapId,PositionX,PositionY,Hp,MaxHp) VALUES(@missing,0,0,0,80,80)' $argsSql $transaction } @(547) 'orphan progress rejected'
    Assert-SqlError { Invoke-DbNonQuery $connection 'DELETE dh.Account WHERE AccountId=@account' $argsSql $transaction } @(547) 'account deletion does not cascade'
    Assert-SqlError { Invoke-DbNonQuery $connection 'DELETE dh.Character WHERE CharacterId=@character' $argsSql $transaction } @(547) 'character deletion does not cascade'
    Assert-SqlError { Invoke-DbNonQuery $connection 'UPDATE dh.Character SET Class=2 WHERE CharacterId=@character' $argsSql $transaction } @(547) 'invalid class rejected'
    Assert-SqlError { Invoke-DbNonQuery $connection 'UPDATE dh.CharacterProgress SET MapId=4 WHERE CharacterId=@character' $argsSql $transaction } @(547) 'invalid map rejected'
    foreach ($assignment in @('Hp=-1', 'Hp=151', 'MaxHp=0')) {
        Assert-SqlError { Invoke-DbNonQuery $connection "UPDATE dh.CharacterProgress SET $assignment WHERE CharacterId=@character" $argsSql $transaction } @(547) "invalid HP rejected: $assignment"
    }
    Assert-SqlError { Invoke-DbNonQuery $connection 'UPDATE dh.CharacterProgress SET PositionX=NULL WHERE CharacterId=@character' $argsSql $transaction } @(515) 'missing coordinate rejected'
    Assert-Equal 1 (Invoke-DbScalar $connection 'SELECT XACT_STATE()' @{} $transaction) 'expected failures leave test transaction usable'

    # Two logical readers captured the same version. First save wins; stale save must affect zero.
    $argsSql.version = [byte[]](Invoke-DbScalar $connection 'SELECT Version FROM dh.CharacterProgress WHERE CharacterId=@character' $argsSql $transaction)
    Assert-Equal 1 (Invoke-DbScalar $connection 'UPDATE dh.CharacterProgress SET Hp=139,BossUnlocked=1,SavedUtc=SYSUTCDATETIME() WHERE CharacterId=@character AND Version=@version; SELECT @@ROWCOUNT;' $argsSql $transaction) 'current rowversion update succeeds'
    Assert-Equal 0 (Invoke-DbScalar $connection 'UPDATE dh.CharacterProgress SET Hp=1 WHERE CharacterId=@character AND Version=@version; SELECT @@ROWCOUNT;' $argsSql $transaction) 'stale save rejected without lost update'
    Assert-Equal 139 (Invoke-DbScalar $connection 'SELECT Hp FROM dh.CharacterProgress WHERE CharacterId=@character' $argsSql $transaction) 'winning save retained'
    # Both valid classes, all map IDs, alive/dead HP bounds.
    [void](Invoke-DbNonQuery $connection 'UPDATE dh.Character SET Class=1 WHERE CharacterId=@character; UPDATE dh.CharacterProgress SET Hp=0,MaxHp=80 WHERE CharacterId=@character;' $argsSql $transaction)
    foreach ($map in 0..3) {
        [void](Invoke-DbNonQuery $connection 'UPDATE dh.CharacterProgress SET MapId=@map WHERE CharacterId=@character' @{map = $map; character = $characterId } $transaction)
    }
    Write-Output 'PASS: both classes, all maps and zero HP accepted'

    $other = Open-LocalDatabase $Instance $Database
    Assert-Equal -1 (Invoke-DbScalar $other "DECLARE @r int; BEGIN TRAN; EXEC @r=sys.sp_getapplock @Resource=N'Dawnholder.SchemaMigration',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0; ROLLBACK; SELECT @r;") 'second connection cannot acquire held migration lock'
    Assert-SqlError { Invoke-DbNonQuery $other 'SET LOCK_TIMEOUT 1000; UPDATE dh.CharacterProgress SET Hp=1 WHERE CharacterId=@character;' $argsSql } @(1222) 'second writer blocked by uncommitted save'

    Assert-Equal 1 (Invoke-DbScalar $connection 'DELETE dh.CharacterProgress WHERE CharacterId=@character; SELECT @@ROWCOUNT;' $argsSql $transaction) 'delete progress test row'
    Assert-Equal 1 (Invoke-DbScalar $connection 'DELETE dh.Character WHERE CharacterId=@character; SELECT @@ROWCOUNT;' $argsSql $transaction) 'delete character test row'
    Assert-Equal 1 (Invoke-DbScalar $connection 'DELETE dh.Account WHERE AccountId=@account; SELECT @@ROWCOUNT;' $argsSql $transaction) 'delete account test row'
    [void](Invoke-DbNonQuery $connection 'INSERT dh.Account(AccountId) VALUES(@account)' $argsSql $transaction)
    Assert-Equal 1 (Invoke-DbScalar $connection 'SELECT COUNT(*) FROM dh.Account WHERE AccountId=@account' $argsSql $transaction) 'rollback witness exists before rollback'
    $transaction.Rollback()
    $transaction.Dispose()
    $transaction = $null
    Assert-Equal 0 (Invoke-DbScalar $connection @'
SELECT (SELECT COUNT(*) FROM dh.Account WHERE AccountId=@account)
+ (SELECT COUNT(*) FROM dh.Character WHERE CharacterId=@character)
+ (SELECT COUNT(*) FROM dh.CharacterProgress WHERE CharacterId=@character)
'@ $argsSql) 'rollback leaves no test rows'
    Assert-Equal 0 (Invoke-DbScalar $other "DECLARE @r int; BEGIN TRAN; EXEC @r=sys.sp_getapplock @Resource=N'Dawnholder.SchemaMigration',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0; ROLLBACK; SELECT @r;") 'migration lock released on rollback'
    # Isolated DDL transactions prove Install's catalog gate rejects semantic drift.
    # THROW with XACT_ABORT ON may doom the transaction; always fully roll it back.
    $driftCases = @(
        @{Label = 'loosened class CHECK rejected'; Number = 51004; Sql = 'ALTER TABLE dh.Character DROP CONSTRAINT CK_Character_Class; ALTER TABLE dh.Character WITH CHECK ADD CONSTRAINT CK_Character_Class CHECK (Class IN (0,1,2));' },
        @{Label = 'changed unlock DEFAULT rejected'; Number = 51005; Sql = 'ALTER TABLE dh.CharacterProgress DROP CONSTRAINT DF_CharacterProgress_BossUnlocked; ALTER TABLE dh.CharacterProgress ADD CONSTRAINT DF_CharacterProgress_BossUnlocked DEFAULT 1 FOR BossUnlocked;' },
        @{Label = 'default attached to wrong column rejected'; Number = 51005; Sql = 'ALTER TABLE dh.CharacterProgress DROP CONSTRAINT DF_CharacterProgress_BossUnlocked; ALTER TABLE dh.CharacterProgress ADD CONSTRAINT DF_CharacterProgress_BossUnlocked DEFAULT 0 FOR Hp;' }
    )
    foreach ($case in $driftCases) {
        $transaction = $connection.BeginTransaction()
        try {
            [void](Invoke-DbNonQuery $connection $case.Sql @{} $transaction)
            Assert-SqlError {
                Invoke-Migrations -Connection $connection -Transaction $transaction -Phase Complete -Contract $Contract
            } @($case.Number) $case.Label
        } finally {
            if ($null -ne $transaction.Connection) { $transaction.Rollback() }
            $transaction.Dispose()
            $transaction = $null
        }
        [void](Invoke-DbNonQuery $connection (Get-MigrationText (Join-Path $PSScriptRoot 'verify-schema.sql')))
    }
    Write-Output 'PASS: original CHECK/default definitions restored after each negative test'
    Write-Output "PASS: all database checks on $Instance / $Database. No test rows committed."
} finally {
    if ($null -ne $transaction) {
        if ($null -ne $transaction.Connection) { $transaction.Rollback() }
        $transaction.Dispose()
    }
    if ($null -ne $other) { $other.Dispose() }
    $connection.Dispose()
}
