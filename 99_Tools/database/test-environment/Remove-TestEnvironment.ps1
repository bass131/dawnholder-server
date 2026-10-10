[CmdletBinding()]
param(
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [ValidateSet('OfflinePlan', 'OnlinePreview', 'Execute')][string]$Mode = 'OfflinePlan',
    [string]$Database,
    [string]$ManifestPath,
    [string]$SettlementPath,
    [string]$ConfirmDatabase,
    [string]$ExpectedManifestHash
)
if ($MyInvocation.InvocationName -eq '.') {
    return
}
. (Join-Path $PSScriptRoot 'Environment.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$VerbosePreference = 'SilentlyContinue'
$DebugPreference = 'SilentlyContinue'

function Read-TestEnvironmentSettlement(
    $Manifest,
    [string]$Path,
    $Contract
) {
    $expected = $Contract.SettlementPath
    Assert-TestEnvironmentPath -Path $Path -Expected $expected
    Assert-TestEnvironmentNoReparse -Path $Path
    $s = [IO.File]::ReadAllText($Path) | ConvertFrom-Json
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
    $root = [IO.Path]::GetDirectoryName($Manifest.ManifestPath) + [IO.Path]::DirectorySeparatorChar
    foreach ($e in @($s.Evidence)) {
        $resolved = [IO.Path]::GetFullPath($e.Path)
        if (-not [IO.Path]::IsPathRooted($e.Path) -or
            -not $resolved.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or
            $e.Hash -cnotmatch '^[0-9A-F]{64}$' -or $resolved -ieq $Path -or $resolved -ieq $Manifest.ManifestPath) {
            throw 'Evidence must be durable, nonsecret files within this goal evidence directory.'
        }
        Assert-TestEnvironmentNoReparse -Path $resolved
        if ((Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash -cne $e.Hash) {
            throw 'Preserved evidence hash mismatch.'
        }
    }
    foreach ($c in @($s.Connections)) {
        if ($c.Closed -isnot [bool] -or -not $c.Closed -or $c.ClientConnectionId -cnotmatch '^[a-fA-F0-9-]{36}$') {
            throw 'A task-owned connection is not settled.'
        }
    }
    $ids = @()
    foreach ($r in @($s.Requests)) {
        if ($r.OperationId -in $ids -or
            [Guid]$r.OperationId -eq [Guid]::Empty -or [Guid]$r.OwnerId -eq [Guid]::Empty -or
            $r.Disposition -cnotin @('Terminal', 'NotDispatched')) {
            throw 'Invalid settled request.'
        }
        if ($r.Disposition -ceq 'Terminal' -and ($r.PayloadHash -cnotmatch '^[0-9A-F]{64}$' -or
                $r.Kind -notin 1..5 -or $r.Outcome -notin @(1, 2))) {
            throw 'Terminal request lacks payload/outcome evidence.'
        }
        $ids += $r.OperationId
    }
    return $s
}

function Assert-TestEnvironmentResourceIdentities(
    $Master,
    $Manifest,
    $Contract
) {
    foreach ($kind in @('Runtime')) {
        $name = $Manifest.RuntimeLogin
        $observed = Invoke-DatabaseSql `
            -Connection $Master `
            -Sql 'SELECT CONVERT(varchar(170),sid,1) FROM sys.server_principals WHERE name=@name' `
            -Parameters @{
            name = (New-DatabaseSqlParameter -Type NVarChar -Value $name -Size 128)
        }
        $expected = $Manifest.($kind + 'LoginSid')
        if (($null -eq $expected -and $null -ne $observed -and $observed -isnot [DBNull]) -or
            ($null -ne $expected -and $observed -cne $expected)) {
            throw 'SQL login identity missing/unknown/changed; no deletion.'
        }
    }
    foreach ($kind in @('Runtime', 'Admin')) {
        $path = $Manifest.($kind + 'CredentialPath')
        $hash = $Manifest.($kind + 'CredentialHash')
        if ($null -eq $hash) {
            if (Test-Path -LiteralPath $path) {
                throw 'Unknown credential file exists; do not read/delete it.'
            }
        } else {
            Assert-TestEnvironmentSecretFile -Contract $Contract -Path $path -ExpectedHash $hash -Manifest $Manifest
        }
    }
    if ($null -ne $Manifest.IdentityHash) {
        Assert-TestEnvironmentDirectoryAcl `
            -Path $Manifest.IdentityDirectory `
            -ExecutorSid $Manifest.ExecutorSid
        Assert-TestEnvironmentNoReparse -Path $Manifest.IdentityPath
        if ((Get-FileHash -LiteralPath $Manifest.IdentityPath -Algorithm SHA256).Hash -cne $Manifest.IdentityHash) {
            throw 'Child identity manifest changed.'
        }
    } elseif (Test-Path -LiteralPath $Manifest.IdentityPath) {
        throw 'Unknown child identity manifest exists.'
    }
}

function Assert-TestEnvironmentQuiescent(
    $Master,
    $Connection,
    $Manifest,
    $Settlement,
    $Contract
) {
    Assert-TestEnvironmentDatabaseIdentity -Master $Master -Manifest $Manifest
    Assert-TestEnvironmentMarkers -Connection $Connection -Manifest $Manifest
    $dbid = [int]$Manifest.DatabaseIdentity.DatabaseId
    $targetSpid = [int](Invoke-DatabaseSql -Connection $Connection -Sql 'SELECT @@SPID')
    $params = @{
        dbid = (New-DatabaseSqlParameter -Type Int -Value $dbid)
        tool = (New-DatabaseSqlParameter -Type Int -Value $targetSpid)
    }
    $active = Invoke-DatabaseSql `
        -Connection $Master `
        -Sql @'
SELECT COUNT( * ) FROM sys.dm_exec_sessions s WHERE s.session_id <> @@SPID AND s.session_id <> @tool AND
    (s.database_id = @dbid OR EXISTS(SELECT 1 FROM sys.dm_exec_requests r WHERE r.session_id = s.session_id AND r.database_id = @dbid) OR
    EXISTS(SELECT 1 FROM sys.dm_tran_locks l WHERE l.request_session_id = s.session_id AND l.resource_database_id = @dbid) OR
    EXISTS(SELECT 1 FROM sys.dm_tran_session_transactions st JOIN sys.dm_tran_database_transactions dt ON dt.transaction_id = st.transaction_id
    WHERE st.session_id = s.session_id AND dt.database_id = @dbid));
'@ `
        -Parameters $params
    if ($active -ne 0) {
        throw 'Remaining target connections/requests/transactions/locks; stop and preserve resources.'
    }
    $bound = Invoke-DatabaseSql `
        -Connection $Connection `
        -Sql @'
IF OBJECT_ID(N'dh.CharacterAuthority', N'U') IS NOT NULL
    SELECT COUNT( * ) FROM dh.CharacterAuthority WHERE SlotId <> 1 OR AccountId <> @account OR CharacterId <> @character OR
    OwnerKind <> 0 OR OwnerId IS NOT NULL OR LastSequence <> 0;
    ELSE SELECT 0;
'@ `
        -Parameters @{
        account = (New-DatabaseSqlParameter -Type UniqueIdentifier -Value ([Guid]$Manifest.AccountId))
        character = (New-DatabaseSqlParameter -Type UniqueIdentifier -Value ([Guid]$Manifest.CharacterId))
    }
    if (@($Manifest.Steps | Where-Object {
                $_.Name -ceq 'InitializeBinding' -and $_.Status -ceq 'Done'
            }).Count -and
        (Invoke-DatabaseSql -Connection $Connection -Sql 'SELECT COUNT(*) FROM dh.CharacterAuthority') -ne 1) {
        throw 'Recorded binding disappeared; preserve resources.'
    }
    if ($bound -ne 0 -or
        (Invoke-DatabaseSql -Connection $Connection `
            -Sql 'SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped=0') -ne 0) {
        throw 'Held/mismatched binding or temporary trigger remains; cleanup does not release/drop it.'
    }
    $ledgerCount = Invoke-DatabaseSql `
        -Connection $Connection `
        -Sql "IF OBJECT_ID(N'dh.CharacterOperation',N'U') IS NULL SELECT 0; ELSE SELECT COUNT(*) FROM dh.CharacterOperation;"
    if ([long]$Settlement.LedgerCount -ne [long]$ledgerCount) {
        throw 'Archived ledger count changed.'
    }
    foreach ($r in @($Settlement.Requests)) {
        if ($r.Disposition -ceq 'Terminal') {
            $rowset = Invoke-DatabaseSql `
                -Connection $Connection `
                -Sql @'
SELECT Kind,
    Outcome,
    ResultCode,
    CONVERT(varchar(64), HASHBYTES('SHA2_256', Payload), 2) PayloadHash
FROM dh.CharacterOperation WHERE OperationId = @id;
'@ `
                -Parameters @{
                id = (New-DatabaseSqlParameter -Type UniqueIdentifier -Value ([Guid]$r.OperationId))
            } `
                -Result Rows
            if ($rowset.Rows.Count -ne 1) {
                throw 'Terminal operation evidence is absent.'
            }
            $row = $rowset.Rows[0]
            if ($row.Kind -ne $r.Kind -or $row.Outcome -ne $r.Outcome -or $row.ResultCode -ne $r.ResultCode -or
                $row.PayloadHash -cne $r.PayloadHash) {
                throw 'Terminal operation evidence differs from settlement.'
            }
        }
    }
    Assert-TestEnvironmentResourceIdentities -Contract $Contract -Master $Master -Manifest $Manifest
}

$Contract = Read-TestEnvironmentApprovalPlan `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
$manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
if ($Mode -eq 'OfflinePlan') {
    [pscustomobject]@{
        Mode = 'OfflinePlan'
        Database = $Database
        State = $manifest.State
        RecordedIdentity = $manifest.DatabaseIdentity
        Order = @(
            'Stop producers and settle requests/connections/owners',
            'Preserve evidence and remove test triggers',
            'Online identity and settlement preflight',
            'DROP exact DB without force',
            'SID-matched runtime SQL login',
            'Hash-matched exact credential files'
        )
        PrivateDirectoryRetained = $true
        ProfileDeletion = $false
    }
    return
}
Assert-TestEnvironmentExecutor -Contract $Contract
if ($Mode -eq 'Execute') {
    if ($ConfirmDatabase -cne $Database -or $ExpectedManifestHash -cnotmatch '^[0-9A-F]{64}$') {
        throw 'Execute requires an exact database confirmation and reviewed manifest hash.'
    }
    $guard = Lock-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
} else {
    $guard = $null
}
$master = $null
$connection = $null
$cleanupStep = $null
$cleanupStarted = $false
$cleanupResourceAttempted = $false
try {
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    if ($Mode -eq 'Execute' -and
        (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash -cne $ExpectedManifestHash) {
        throw 'Manifest changed after review.'
    }
    if ($null -ne $manifest.Cleanup -or $manifest.State -in @('CleanupStarted', 'Removed')) {
        throw 'Cleanup was already attempted; explicit reconciliation is required.'
    }
    $settlement = Read-TestEnvironmentSettlement -Contract $Contract -Manifest $manifest -Path $SettlementPath
    $master = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database -Master
    Assert-TestEnvironmentDatabaseIdentity -Master $master -Manifest $manifest
    $connection = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database
    Assert-TestEnvironmentQuiescent `
        -Contract $Contract `
        -Master $master `
        -Connection $connection `
        -Manifest $manifest `
        -Settlement $settlement
    if ($Mode -eq 'OnlinePreview') {
        Write-Output 'Online identity/settlement preflight passed at this observation; no resource was changed.'
        return
    }
    # Record all cleanup planning before destructive work. Failed provisioning is never retried here.
    $manifest.Cleanup = [pscustomobject]@{
        StartedUtc = [DateTime]::UtcNow.ToString('o')
        SettlementPath = $SettlementPath
        SettlementHash = (Get-FileHash -LiteralPath $SettlementPath -Algorithm SHA256).Hash
        State = 'Pending'
        Steps = @()
    }
    $cleanupStarted = $true
    $manifest.State = 'CleanupStarted'
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    $connection.Dispose()
    $connection = $null
    $cleanupStep = 'DropDatabase'
    $manifest.Cleanup.Steps = @([pscustomobject]@{
            Name = $cleanupStep
            Status = 'Pending'
            Identity = $manifest.DatabaseIdentity
        })
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    $dropParameters = @{
        id = (New-DatabaseSqlParameter -Type Int -Value ([int]$manifest.DatabaseIdentity.DatabaseId))
        created = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.DatabaseIdentity.CreationTime -Size 33)
        owner = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.DatabaseIdentity.OwnerSid -Size 170)
        goal = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.GoalMarker -Size 128)
        guid = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.DatabaseIdentity.DatabaseGuid -Size 36)
        database = (New-DatabaseSqlParameter -Type NVarChar -Value $Database -Size 128)
    }
    # Parameter preparation can fail before any destructive call is attempted.
    $cleanupResourceAttempted = $true
    [void](Invoke-DatabaseSql `
            -Connection $master `
            -Sql @'
SET LOCK_TIMEOUT 1000;
IF NOT EXISTS(SELECT 1 FROM sys.databases d JOIN sys.database_recovery_status r ON r.database_id = d.database_id
    WHERE d.name = @database AND d.database_id = @id AND CONVERT(nvarchar(33), d.create_date, 126) = @created
    AND CONVERT(varchar(170), d.owner_sid, 1) = @owner AND CONVERT(nvarchar(36), r.database_guid) = @guid)
    THROW 51130, 'Exact database identity changed.', 1;
-- Only the reviewed identifier enters dynamic lifecycle SQL; marker values remain typed parameters.
DECLARE @markerSql nvarchar(max) = N'IF NOT EXISTS(SELECT 1 FROM ' + QUOTENAME(@database) + N'.sys.extended_properties
 WHERE class=0 AND name=N''Dawnholder.DatabaseTool'' AND CONVERT(nvarchar(128),value)=N''development-v1'') OR
 NOT EXISTS(SELECT 1 FROM ' + QUOTENAME(@database) + N'.sys.extended_properties WHERE class=0 AND
 name=N''Dawnholder.D1bGoal'' AND CONVERT(nvarchar(128),value)=@goal)
 THROW 51134, ''Exact owner/goal marker changed.'', 1;';
EXEC sys.sp_executesql @stmt = @markerSql,
    @params = N'@goal nvarchar(128)',
    @goal = @goal;
IF EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE session_id <> @@SPID AND database_id = @id) OR
    EXISTS(SELECT 1 FROM sys.dm_exec_requests WHERE session_id <> @@SPID AND database_id = @id) OR
    EXISTS(SELECT 1 FROM sys.dm_tran_locks WHERE request_session_id <> @@SPID AND resource_database_id = @id)
    THROW 51131, 'New or remaining target connection; preserve database.', 1;
DECLARE @dropSql nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@database) + N';';
EXEC sys.sp_executesql @stmt = @dropSql;
'@ `
            -Parameters $dropParameters `
            -Result NonQuery)
    $manifest.Cleanup.Steps[0].Status = 'Done'
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    foreach ($kind in @('Runtime')) {
        $sid = $manifest.($kind + 'LoginSid')
        if ($null -eq $sid) {
            continue
        }
        $name = $Manifest.RuntimeLogin
        $cleanupStep = 'Drop' + $kind + 'Login'
        $record = [pscustomobject]@{
            Name = $cleanupStep
            Status = 'Pending'
            Identity = [pscustomobject]@{
                Name = $name
                Sid = $sid
            }
        }
        $manifest.Cleanup.Steps = @($manifest.Cleanup.Steps) + $record
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
        $sql = @'
DECLARE @dropSql nvarchar(max) = N'DROP LOGIN ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt = @dropSql;
'@
        $prefix = @'
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name = @name AND sid = @sid)
    THROW 51132, 'Exact login SID changed.', 1;
IF EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE security_id = @sid OR original_security_id = @sid)
    THROW 51133, 'Principal still has a SQL session.', 1;
'@
        [void](Invoke-DatabaseSql `
                -Connection $master `
                -Sql ($prefix + "`n" + $sql) `
                -Parameters @{
                name = (New-DatabaseSqlParameter -Type NVarChar -Value $name -Size 128)
                sid = (New-DatabaseSqlParameter -Type VarBinary -Value (ConvertFrom-DatabaseSidHex -Hex $sid) -Size 85)
            } `
                -Result NonQuery)
        $record.Status = 'Done'
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    }
    foreach ($kind in @('Runtime', 'Admin')) {
        $hash = $manifest.($kind + 'CredentialHash')
        if ($null -eq $hash) {
            continue
        }
        $path = $manifest.($kind + 'CredentialPath')
        $cleanupStep = 'Remove' + $kind + 'Credential'
        $record = [pscustomobject]@{
            Name = $cleanupStep
            Status = 'Pending'
            Identity = [pscustomobject]@{
                Path = $path
                Hash = $hash
            }
        }
        $manifest.Cleanup.Steps = @($manifest.Cleanup.Steps) + $record
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
        Assert-TestEnvironmentSecretFile -Contract $Contract -Path $path -ExpectedHash $hash -Manifest $manifest
        Remove-Item -LiteralPath $path -ErrorAction Stop
        $record.Status = 'Done'
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    }
    $manifest.Cleanup.State = 'Done'
    $manifest.State = 'Removed'
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    Write-Output 'Exact DB and recorded principal/credential resources removed. Nonsecret manifests, evidence, empty directories retained.'
} catch {
    $failure = $_.Exception
    $failureCode = Get-DatabaseFailureCode -Exception $failure
    $failureSummary = Get-TestEnvironmentFailureSummary -Exception $failure
    $journalStatus = 'No cleanup failure journal write was attempted.'
    $executionStatus = $(if ($cleanupResourceAttempted) {
            'Resource deletion was attempted; cleanup may be partial.'
        } else {
            'Stopped before resource deletion was attempted.'
        })
    if ($cleanupStarted) {
        # An in-memory failure state is durable only after this write succeeds.
        try {
            $manifest.Cleanup.State = 'Failed'
            $manifest.Cleanup | Add-Member `
                -MemberType NoteProperty `
                -Name Failure `
                -Value $failureCode `
                -Force
            foreach ($record in @($manifest.Cleanup.Steps)) {
                if ($record.Status -ceq 'Pending') {
                    $record.Status = 'Failed'
                }
            }
            Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
            $journalStatus = 'Cleanup failure journal write succeeded.'
            if ($cleanupResourceAttempted) {
                $journalStatus = 'Cleanup failure journal write succeeded. ' +
                'Attempted partial cleanup is recorded as failed; deletion outcomes remain unconfirmed.'
            }
        } catch {
            $journalStatus = 'Cleanup failure journal write failed; durable state is unconfirmed. {0}' -f (
                Get-TestEnvironmentFailureSummary -Exception $_.Exception
            )
        }
    }
    $message = 'Test environment cleanup stopped. {0} Original failure: {1} Journal: {2} ' +
    'Preserve remaining resources and ask the coordinator; no forced cleanup or automatic retry.'
    throw ($message -f $executionStatus, $failureSummary, $journalStatus)
} finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
    if ($null -ne $master) {
        $master.Dispose()
    }
    if ($null -ne $guard) {
        $guard.Dispose()
    }
}
