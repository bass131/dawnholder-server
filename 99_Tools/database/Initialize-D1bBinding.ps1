[CmdletBinding()]
param([string]$Database, [string]$ManifestPath)
if ($MyInvocation.InvocationName -eq '.') { return }
. (Join-Path $PSScriptRoot 'D1b.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Assert-D1bTarget $Database
$guard=Lock-D1bManifest $Database $ManifestPath
$master=$null; $connection=$null; $transaction=$null; $manifest=$null; $started=$false
try {
    $manifest=Read-D1bManifest $Database $ManifestPath
    Assert-D1bReadyForStep $manifest
    if ($manifest.State -cnotin @('Installed','Bound','PrincipalsReady')) { throw 'Complete installation is required before binding.' }
    $master=Open-D1bDatabase $manifest $Database -Master
    Assert-D1bDatabaseIdentity $master $manifest
    $connection=Open-D1bDatabase $manifest $Database
    Assert-D1bMarkers $connection $manifest
    $transaction=$connection.BeginTransaction()
    $parameters=@{account=(New-D1bParameter UniqueIdentifier ([Guid]$manifest.AccountId))
        character=(New-D1bParameter UniqueIdentifier ([Guid]$manifest.CharacterId))}
    $rowset=Invoke-D1bSql $connection @'
SET XACT_ABORT ON;
DECLARE @r int;
EXEC @r=sys.sp_getapplock @Resource=N'Dawnholder.Persistence.Slot.1',@DbPrincipal='public',
 @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=2000;
IF @r<0 THROW 51110, 'Slot lock unavailable.', 1;
IF (SELECT COUNT(*) FROM dh.CharacterAuthority WITH(UPDLOCK,HOLDLOCK))>1 OR
 EXISTS(SELECT 1 FROM dh.CharacterAuthority WHERE SlotId<>1 OR AccountId<>@account OR CharacterId<>@character)
 THROW 51112, 'Existing binding mismatch; no replacement.', 1;
IF EXISTS(SELECT 1 FROM dh.Character WITH(UPDLOCK,HOLDLOCK) WHERE CharacterId=@character AND AccountId<>@account)
 THROW 51111, 'Fixed character has a different account.', 1;
SELECT SlotId,AccountId,CharacterId,Fence,OwnerKind,OwnerId,LastSequence FROM dh.CharacterAuthority;
'@ $parameters $transaction -Result Rows
    if ($rowset.Rows.Count -eq 1) {
        $transaction.Commit()
        $r=$rowset.Rows[0]
        Write-Output "Exact binding already exists; no DML. Fence=$($r.Fence), OwnerKind=$($r.OwnerKind), LastSequence=$($r.LastSequence)."
        return
    }
    Start-D1bStep $manifest 'InitializeBinding' ([pscustomobject]@{Slot=1; AccountId=$manifest.AccountId; CharacterId=$manifest.CharacterId; Fence=0; OwnerKind=0})
    $started=$true
    [void](Invoke-D1bSql $connection @'
IF EXISTS(SELECT 1 FROM dh.CharacterAuthority WITH(UPDLOCK,HOLDLOCK)) THROW 51113, 'Binding appeared.', 1;
INSERT dh.CharacterAuthority(SlotId,AccountId,CharacterId,Fence,OwnerKind,OwnerId,LastSequence,ChangedUtc)
 VALUES(1,@account,@character,0,0,NULL,0,SYSUTCDATETIME());
'@ $parameters $transaction -Result NonQuery)
    $transaction.Commit()
    $manifest.State='Bound'
    Complete-D1bStep $manifest 'InitializeBinding' ([pscustomobject]@{SlotId=1; AccountId=$manifest.AccountId; CharacterId=$manifest.CharacterId; Fence=0; OwnerKind=0})
    Write-Output 'Fixed slot1 binding INSERT committed; no game row was created or adopted.'
} catch {
    $failureCode=Get-D1bFailureCode $_.Exception
    if ($null -ne $transaction -and $null -ne $transaction.Connection) { try { $transaction.Rollback() } catch { } }
    if ($started) { Fail-D1bStep $manifest 'InitializeBinding' $failureCode }
    throw 'D1b binding stopped; preserve manifest and investigate commit state before retry.'
} finally {
    if ($null -ne $transaction) { $transaction.Dispose() }
    if ($null -ne $connection) { $connection.Dispose() }
    if ($null -ne $master) { $master.Dispose() }
    $guard.Dispose()
}
