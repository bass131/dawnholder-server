[CmdletBinding()]
param(
    [ValidateSet('OfflinePlan','OnlinePreview','Execute')][string]$Mode = 'OfflinePlan',
    [string]$Database, [string]$ManifestPath, [string]$SettlementPath,
    [string]$ConfirmDatabase, [string]$ExpectedManifestHash
)
if ($MyInvocation.InvocationName -eq '.') { return }
. (Join-Path $PSScriptRoot 'D1b.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$VerbosePreference='SilentlyContinue'; $DebugPreference='SilentlyContinue'

function Read-D1bSettlement($Manifest, [string]$Path) {
    $expected=Join-Path ([IO.Path]::GetDirectoryName($Manifest.ManifestPath)) 'fixture-settlement.json'
    Assert-D1bPath $Path $expected
    Assert-D1bNoReparse $Path
    $s=[IO.File]::ReadAllText($Path) | ConvertFrom-Json
    if ($s.SchemaVersion -ne 1 -or $s.GoalMarker -cne $Manifest.GoalMarker -or $s.Database -cne $Manifest.Database -or
        $s.DatabaseGuid -cne $Manifest.DatabaseIdentity.DatabaseGuid -or $s.ExecutorSid -cne $Manifest.ExecutorSid -or
        $s.RequestsComplete -isnot [bool] -or -not $s.RequestsComplete -or
        $s.ProducersStopped -isnot [bool] -or -not $s.ProducersStopped -or
        $s.OldRequestsCannotArrive -isnot [bool] -or -not $s.OldRequestsCannotArrive -or
        $s.EvidencePreserved -isnot [bool] -or -not $s.EvidencePreserved -or
        @($s.Unresolved).Count -ne 0 -or @($s.PendingRequests).Count -ne 0 -or @($s.HeldOwners).Count -ne 0 -or
        @($s.RemainingTriggers).Count -ne 0 -or @($s.Evidence).Count -eq 0) {
        throw 'Cleanup settlement is incomplete; no deletion is authorized by it.'
    }
    $root=[IO.Path]::GetDirectoryName($Manifest.ManifestPath)+[IO.Path]::DirectorySeparatorChar
    foreach ($e in @($s.Evidence)) {
        $resolved=[IO.Path]::GetFullPath($e.Path)
        if (-not [IO.Path]::IsPathRooted($e.Path) -or -not $resolved.StartsWith($root,[StringComparison]::OrdinalIgnoreCase) -or
            $e.Hash -cnotmatch '^[0-9A-F]{64}$' -or $resolved -ieq $Path -or $resolved -ieq $Manifest.ManifestPath) {
            throw 'Evidence must be durable, nonsecret files within this goal evidence directory.'
        }
        Assert-D1bNoReparse $resolved
        if ((Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash -cne $e.Hash) { throw 'Preserved evidence hash mismatch.' }
    }
    foreach ($c in @($s.Connections)) {
        if ($c.Closed -isnot [bool] -or -not $c.Closed -or $c.ClientConnectionId -cnotmatch '^[a-fA-F0-9-]{36}$') {
            throw 'A task-owned connection is not settled.'
        }
    }
    $ids=@()
    foreach ($r in @($s.Requests)) {
        if ($r.OperationId -in $ids -or [Guid]$r.OperationId -eq [Guid]::Empty -or [Guid]$r.OwnerId -eq [Guid]::Empty -or
            $r.Disposition -cnotin @('Terminal','NotDispatched')) { throw 'Invalid settled request.' }
        if ($r.Disposition -ceq 'Terminal' -and ($r.PayloadHash -cnotmatch '^[0-9A-F]{64}$' -or
            $r.Kind -notin 1..5 -or $r.Outcome -notin @(1,2))) { throw 'Terminal request lacks payload/outcome evidence.' }
        $ids += $r.OperationId
    }
    return $s
}

function Assert-D1bResourceIdentities($Master, $Manifest) {
    foreach ($kind in @('Runtime','Recovery')) {
        $name=$(if ($kind -eq 'Runtime') { $Manifest.RuntimeLogin } else { $Manifest.RecoveryPrincipal })
        $observed=Invoke-D1bSql $Master 'SELECT CONVERT(varchar(170),sid,1) FROM sys.server_principals WHERE name=@name' @{
            name=(New-D1bParameter NVarChar $name 128)}
        $expected=$Manifest.($kind+'LoginSid')
        if (($null -eq $expected -and $null -ne $observed -and $observed -isnot [DBNull]) -or
            ($null -ne $expected -and $observed -cne $expected)) { throw 'SQL login identity missing/unknown/changed; no deletion.' }
    }
    $windows=$null
    try { $windows=Get-LocalUser -Name 'dh_d1b_recovery' -ErrorAction Stop }
    catch { if ($_.FullyQualifiedErrorId -notlike 'UserNotFound*') { throw 'Cannot inspect exact Windows account identity.' } }
    if (($null -eq $Manifest.WindowsAccountSid -and $null -ne $windows) -or
        ($null -ne $Manifest.WindowsAccountSid -and ($null -eq $windows -or $windows.SID.Value -cne $Manifest.WindowsAccountSid))) {
        throw 'Windows account identity missing/unknown/changed; no deletion.'
    }
    foreach ($kind in @('Runtime','Recovery')) {
        $path=$Manifest.($kind+'CredentialPath'); $hash=$Manifest.($kind+'CredentialHash')
        if ($null -eq $hash) {
            if (Test-Path -LiteralPath $path) { throw 'Unknown credential file exists; do not read/delete it.' }
        } else { Assert-D1bSecretFile $path $hash $Manifest }
    }
    if ($null -ne $Manifest.IdentityHash) {
        Assert-D1bDirectoryAcl $Manifest.IdentityDirectory $Manifest.ExecutorSid $Manifest.WindowsAccountSid
        Assert-D1bNoReparse $Manifest.IdentityPath
        if ((Get-FileHash -LiteralPath $Manifest.IdentityPath -Algorithm SHA256).Hash -cne $Manifest.IdentityHash) {
            throw 'Child identity manifest changed.'
        }
    } elseif (Test-Path -LiteralPath $Manifest.IdentityPath) { throw 'Unknown child identity manifest exists.' }
}

function Assert-D1bQuiescent($Master, $Connection, $Manifest, $Settlement) {
    Assert-D1bDatabaseIdentity $Master $Manifest
    Assert-D1bMarkers $Connection $Manifest
    $dbid=[int]$Manifest.DatabaseIdentity.DatabaseId
    $targetSpid=[int](Invoke-D1bSql $Connection 'SELECT @@SPID')
    $params=@{dbid=(New-D1bParameter Int $dbid); tool=(New-D1bParameter Int $targetSpid)}
    $active=Invoke-D1bSql $Master @'
SELECT COUNT(*) FROM sys.dm_exec_sessions s WHERE s.session_id<>@@SPID AND s.session_id<>@tool AND
 (s.database_id=@dbid OR EXISTS(SELECT 1 FROM sys.dm_exec_requests r WHERE r.session_id=s.session_id AND r.database_id=@dbid) OR
 EXISTS(SELECT 1 FROM sys.dm_tran_locks l WHERE l.request_session_id=s.session_id AND l.resource_database_id=@dbid) OR
 EXISTS(SELECT 1 FROM sys.dm_tran_session_transactions st JOIN sys.dm_tran_database_transactions dt ON dt.transaction_id=st.transaction_id
 WHERE st.session_id=s.session_id AND dt.database_id=@dbid));
'@ $params
    if ($active -ne 0) { throw 'Remaining target connections/requests/transactions/locks; stop and preserve resources.' }
    $bound=Invoke-D1bSql $Connection @'
IF OBJECT_ID(N'dh.CharacterAuthority',N'U') IS NOT NULL
 SELECT COUNT(*) FROM dh.CharacterAuthority WHERE SlotId<>1 OR AccountId<>@account OR CharacterId<>@character OR
 OwnerKind<>0 OR OwnerId IS NOT NULL OR LastSequence<>0;
ELSE SELECT 0;
'@ @{account=(New-D1bParameter UniqueIdentifier ([Guid]$Manifest.AccountId)); character=(New-D1bParameter UniqueIdentifier ([Guid]$Manifest.CharacterId))}
    if (@($Manifest.Steps | Where-Object { $_.Name -ceq 'InitializeBinding' -and $_.Status -ceq 'Done' }).Count -and
        (Invoke-D1bSql $Connection 'SELECT COUNT(*) FROM dh.CharacterAuthority') -ne 1) { throw 'Recorded binding disappeared; preserve resources.' }
    if ($bound -ne 0 -or (Invoke-D1bSql $Connection 'SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped=0') -ne 0) {
        throw 'Held/mismatched binding or temporary trigger remains; cleanup does not release/drop it.'
    }
    $ledgerCount=Invoke-D1bSql $Connection "IF OBJECT_ID(N'dh.CharacterOperation',N'U') IS NULL SELECT 0; ELSE SELECT COUNT(*) FROM dh.CharacterOperation;"
    if ([long]$Settlement.LedgerCount -ne [long]$ledgerCount) { throw 'Archived ledger count changed.' }
    foreach ($r in @($Settlement.Requests)) {
        if ($r.Disposition -ceq 'Terminal') {
            $rowset=Invoke-D1bSql $Connection @'
SELECT Kind,Outcome,ResultCode,CONVERT(varchar(64),HASHBYTES('SHA2_256',Payload),2) PayloadHash
FROM dh.CharacterOperation WHERE OperationId=@id;
'@ @{id=(New-D1bParameter UniqueIdentifier ([Guid]$r.OperationId))} -Result Rows
            if ($rowset.Rows.Count -ne 1) { throw 'Terminal operation evidence is absent.' }
            $row=$rowset.Rows[0]
            if ($row.Kind -ne $r.Kind -or $row.Outcome -ne $r.Outcome -or $row.ResultCode -ne $r.ResultCode -or
                $row.PayloadHash -cne $r.PayloadHash) { throw 'Terminal operation evidence differs from settlement.' }
        }
    }
    Assert-D1bResourceIdentities $Master $Manifest
}

Assert-D1bTarget $Database
$manifest=Read-D1bManifest $Database $ManifestPath
if ($Mode -eq 'OfflinePlan') {
    [pscustomobject]@{Mode='OfflinePlan'; Database=$Database; State=$manifest.State; RecordedIdentity=$manifest.DatabaseIdentity
        Order=@('Stop producers and settle requests/connections/owners','Preserve evidence and remove test triggers',
            'Online identity and settlement preflight','DROP exact DB without force','SID-matched SQL logins','SID-matched Windows account',
            'Hash-matched exact credential files'); PrivateDirectoryRetained=$true; ProfileDeletion=$false}
    return
}
Assert-D1bExecutor -Administrator:($Mode -eq 'Execute')
if ($Mode -eq 'Execute') {
    if ($ConfirmDatabase -cne $Database -or $ExpectedManifestHash -cnotmatch '^[0-9A-F]{64}$') {
        throw 'Execute requires an exact database confirmation and reviewed manifest hash.'
    }
    $guard=Lock-D1bManifest $Database $ManifestPath
} else { $guard=$null }
$master=$null; $connection=$null; $cleanupStep=$null; $cleanupStarted=$false
try {
    $manifest=Read-D1bManifest $Database $ManifestPath
    if ($Mode -eq 'Execute' -and (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash -cne $ExpectedManifestHash) {
        throw 'Manifest changed after review.'
    }
    if ($null -ne $manifest.Cleanup -or $manifest.State -in @('CleanupStarted','Removed')) { throw 'Cleanup was already attempted; explicit reconciliation is required.' }
    $settlement=Read-D1bSettlement $manifest $SettlementPath
    $master=Open-D1bDatabase $manifest $Database -Master
    Assert-D1bDatabaseIdentity $master $manifest
    $connection=Open-D1bDatabase $manifest $Database
    Assert-D1bQuiescent $master $connection $manifest $settlement
    if ($Mode -eq 'OnlinePreview') { Write-Output 'Online identity/settlement preflight passed at this observation; no resource was changed.'; return }
    # Record all cleanup planning before destructive work. Failed provisioning is never retried here.
    $manifest.Cleanup=[pscustomobject]@{StartedUtc=[DateTime]::UtcNow.ToString('o'); SettlementPath=$SettlementPath
        SettlementHash=(Get-FileHash -LiteralPath $SettlementPath -Algorithm SHA256).Hash; State='Pending'; Steps=@()}
    $cleanupStarted=$true
    $manifest.State='CleanupStarted'; Write-D1bManifest $manifest
    $connection.Dispose(); $connection=$null
    $cleanupStep='DropDatabase'
    $manifest.Cleanup.Steps=@([pscustomobject]@{Name=$cleanupStep; Status='Pending'; Identity=$manifest.DatabaseIdentity})
    Write-D1bManifest $manifest
    [void](Invoke-D1bSql $master @'
SET LOCK_TIMEOUT 1000;
IF NOT EXISTS(SELECT 1 FROM sys.databases d JOIN sys.database_recovery_status r ON r.database_id=d.database_id
 WHERE d.name=N'Dawnholder_Dev_D1b_20261002' AND d.database_id=@id AND CONVERT(nvarchar(33),d.create_date,126)=@created
 AND CONVERT(varchar(170),d.owner_sid,1)=@owner AND CONVERT(nvarchar(36),r.database_guid)=@guid)
 THROW 51130, 'Exact database identity changed.', 1;
IF NOT EXISTS(SELECT 1 FROM [Dawnholder_Dev_D1b_20261002].sys.extended_properties WHERE class=0 AND
 name=N'Dawnholder.DatabaseTool' AND CONVERT(nvarchar(128),value)=N'development-v1') OR
 NOT EXISTS(SELECT 1 FROM [Dawnholder_Dev_D1b_20261002].sys.extended_properties WHERE class=0 AND
 name=N'Dawnholder.D1bGoal' AND CONVERT(nvarchar(128),value)=@goal)
 THROW 51134, 'Exact owner/goal marker changed.', 1;
IF EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE session_id<>@@SPID AND database_id=@id) OR
 EXISTS(SELECT 1 FROM sys.dm_exec_requests WHERE session_id<>@@SPID AND database_id=@id) OR
 EXISTS(SELECT 1 FROM sys.dm_tran_locks WHERE request_session_id<>@@SPID AND resource_database_id=@id)
 THROW 51131, 'New or remaining target connection; preserve database.', 1;
DROP DATABASE [Dawnholder_Dev_D1b_20261002];
'@ @{id=(New-D1bParameter Int ([int]$manifest.DatabaseIdentity.DatabaseId))
        created=(New-D1bParameter NVarChar $manifest.DatabaseIdentity.CreationTime 33)
        owner=(New-D1bParameter NVarChar $manifest.DatabaseIdentity.OwnerSid 170)
        goal=(New-D1bParameter NVarChar $manifest.GoalMarker 128)
        guid=(New-D1bParameter NVarChar $manifest.DatabaseIdentity.DatabaseGuid 36)} -Result NonQuery)
    $manifest.Cleanup.Steps[0].Status='Done'; Write-D1bManifest $manifest
    foreach ($kind in @('Runtime','Recovery')) {
        $sid=$manifest.($kind+'LoginSid')
        if ($null -eq $sid) { continue }
        $name=$(if ($kind -eq 'Runtime') { $manifest.RuntimeLogin } else { $manifest.RecoveryPrincipal })
        $cleanupStep='Drop'+$kind+'Login'
        $record=[pscustomobject]@{Name=$cleanupStep; Status='Pending'; Identity=[pscustomobject]@{Name=$name; Sid=$sid}}
        $manifest.Cleanup.Steps=@($manifest.Cleanup.Steps)+$record; Write-D1bManifest $manifest
        $sql=$(if ($kind -eq 'Runtime') { 'DROP LOGIN [dh_d1b_runtime_20261002];' } else { 'DROP LOGIN [YYH_DESKTOP\dh_d1b_recovery];' })
        $prefix=@'
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@name AND sid=@sid) THROW 51132, 'Exact login SID changed.', 1;
IF EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE security_id=@sid OR original_security_id=@sid)
 THROW 51133, 'Principal still has a SQL session.', 1;
'@
        [void](Invoke-D1bSql $master ($prefix+"`n"+$sql) @{name=(New-D1bParameter NVarChar $name 128)
            sid=(New-D1bParameter VarBinary (Get-D1bBytes $sid) 85)} -Result NonQuery)
        $record.Status='Done'; Write-D1bManifest $manifest
    }
    if ($null -ne $manifest.WindowsAccountSid) {
        $cleanupStep='RemoveWindowsAccount'
        $record=[pscustomobject]@{Name=$cleanupStep; Status='Pending'; Identity=$manifest.WindowsAccountSid}
        $manifest.Cleanup.Steps=@($manifest.Cleanup.Steps)+$record; Write-D1bManifest $manifest
        $user=Get-LocalUser -Name 'dh_d1b_recovery' -ErrorAction Stop
        if ($user.SID.Value -cne $manifest.WindowsAccountSid) { throw 'Windows SID changed; preserve account.' }
        Remove-LocalUser -SID $user.SID -ErrorAction Stop
        $record.Status='Done'; Write-D1bManifest $manifest
    }
    foreach ($kind in @('Runtime','Recovery')) {
        $hash=$manifest.($kind+'CredentialHash')
        if ($null -eq $hash) { continue }
        $path=$manifest.($kind+'CredentialPath')
        $cleanupStep='Remove'+$kind+'Credential'
        $record=[pscustomobject]@{Name=$cleanupStep; Status='Pending'; Identity=[pscustomobject]@{Path=$path; Hash=$hash}}
        $manifest.Cleanup.Steps=@($manifest.Cleanup.Steps)+$record; Write-D1bManifest $manifest
        Assert-D1bSecretFile $path $hash $manifest
        Remove-Item -LiteralPath $path -ErrorAction Stop
        $record.Status='Done'; Write-D1bManifest $manifest
    }
    $manifest.Cleanup.State='Done'; $manifest.State='Removed'; Write-D1bManifest $manifest
    Write-Output 'Exact DB and recorded principal/credential resources removed. Nonsecret manifests, evidence, empty directories and any Windows profile retained.'
} catch {
    if ($cleanupStarted) {
        $manifest.Cleanup.State='Failed'
        $manifest.Cleanup | Add-Member -MemberType NoteProperty -Name Failure -Value (Get-D1bFailureCode $_.Exception) -Force
        foreach ($record in @($manifest.Cleanup.Steps)) { if ($record.Status -ceq 'Pending') { $record.Status='Failed' } }
        Write-D1bManifest $manifest
    }
    throw 'D1b cleanup stopped; partial cleanup is recorded. Preserve remaining resources and ask the coordinator; no forced cleanup or automatic retry.'
} finally {
    if ($null -ne $connection) { $connection.Dispose() }
    if ($null -ne $master) { $master.Dispose() }
    if ($null -ne $guard) { $guard.Dispose() }
}
