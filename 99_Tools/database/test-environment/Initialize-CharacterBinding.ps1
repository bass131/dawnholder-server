[CmdletBinding()]
param(
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [string]$Database,
    [string]$ManifestPath
)
if ($MyInvocation.InvocationName -eq '.') {
    return
}
. (Join-Path $PSScriptRoot 'Environment.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Contract = Read-TestEnvironmentApprovalPlan `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
$guard = Lock-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
$master = $null
$connection = $null
$transaction = $null
$manifest = $null
$started = $false
try {
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    Assert-TestEnvironmentReadyForStep -Manifest $manifest
    if ($manifest.State -cnotin @('Installed', 'Bound', 'PrincipalsReady')) {
        throw 'Complete installation is required before binding.'
    }
    $master = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database -Master
    Assert-TestEnvironmentDatabaseIdentity -Master $master -Manifest $manifest
    $connection = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database
    Assert-TestEnvironmentMarkers -Connection $connection -Manifest $manifest
    $transaction = $connection.BeginTransaction()
    $parameters = @{
        account = (New-DatabaseSqlParameter -Type UniqueIdentifier -Value ([Guid]$manifest.AccountId))
        character = (New-DatabaseSqlParameter -Type UniqueIdentifier -Value ([Guid]$manifest.CharacterId))
    }
    $rowset = Invoke-DatabaseSql `
        -Connection $connection `
        -Sql @'
SET XACT_ABORT ON;
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock @Resource = N'Dawnholder.Persistence.Slot.1',
    @DbPrincipal = 'public',
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 2000;
IF @lockResult < 0
    THROW 51110, 'Slot lock unavailable.', 1;
IF (SELECT COUNT( * ) FROM dh.CharacterAuthority WITH (UPDLOCK, HOLDLOCK)) > 1 OR
    EXISTS(SELECT 1 FROM dh.CharacterAuthority WHERE SlotId <> 1 OR AccountId <> @account OR CharacterId <> @character)
    THROW 51112, 'Existing binding mismatch; no replacement.', 1;
IF EXISTS(SELECT 1 FROM dh.Character WITH (UPDLOCK, HOLDLOCK) WHERE CharacterId = @character AND AccountId <> @account)
    THROW 51111, 'Fixed character has a different account.', 1;
SELECT SlotId,
    AccountId,
    CharacterId,
    Fence,
    OwnerKind,
    OwnerId,
    LastSequence FROM dh.CharacterAuthority;
'@ `
        -Parameters $parameters `
        -Transaction $transaction `
        -Result Rows
    if ($rowset.Rows.Count -eq 1) {
        $transaction.Commit()
        $r = $rowset.Rows[0]
        Write-Output "Exact binding already exists; no DML. Fence=$($r.Fence), OwnerKind=$($r.OwnerKind), LastSequence=$($r.LastSequence)."
        return
    }
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name 'InitializeBinding' `
        -Plan ([pscustomobject]@{
            Slot = 1
            AccountId = $manifest.AccountId
            CharacterId = $manifest.CharacterId
            Fence = 0
            OwnerKind = 0
        })
    $started = $true
    [void](Invoke-DatabaseSql `
            -Connection $connection `
            -Sql @'
IF EXISTS(SELECT 1 FROM dh.CharacterAuthority WITH (UPDLOCK, HOLDLOCK))
    THROW 51113, 'Binding appeared.', 1;
INSERT dh.CharacterAuthority
(
    SlotId,
    AccountId,
    CharacterId,
    Fence,
    OwnerKind,
    OwnerId,
    LastSequence,
    ChangedUtc
)
VALUES
(
    1,
    @account,
    @character,
    0,
    0,
    NULL,
    0,
    SYSUTCDATETIME()
);
'@ `
            -Parameters $parameters `
            -Transaction $transaction `
            -Result NonQuery)
    $transaction.Commit()
    $manifest.State = 'Bound'
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name 'InitializeBinding' `
        -Identity ([pscustomobject]@{
            SlotId = 1
            AccountId = $manifest.AccountId
            CharacterId = $manifest.CharacterId
            Fence = 0
            OwnerKind = 0
        })
    Write-Output 'Fixed slot1 binding INSERT committed; no game row was created or adopted.'
} catch {
    $failure = $_.Exception
    $failureCode = Get-DatabaseFailureCode -Exception $failure
    if ($null -ne $transaction -and $null -ne $transaction.Connection) {
        try {
            $transaction.Rollback()
        } catch {
        }
    }
    if ($started) {
        Fail-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name 'InitializeBinding' `
            -FailureCode $failureCode
    }
    $summary = Get-TestEnvironmentFailureSummary -Exception $failure
    throw (('Test environment binding stopped; ' +
        'preserve manifest and investigate commit state before retry. {0}') -f $summary)
} finally {
    if ($null -ne $transaction) {
        $transaction.Dispose()
    }
    if ($null -ne $connection) {
        $connection.Dispose()
    }
    if ($null -ne $master) {
        $master.Dispose()
    }
    $guard.Dispose()
}
