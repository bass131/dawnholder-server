[CmdletBinding()]
param([string]$Database, [string]$ManifestPath)
if ($MyInvocation.InvocationName -eq '.') { return }
. (Join-Path $PSScriptRoot 'D1b.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$VerbosePreference='SilentlyContinue'; $DebugPreference='SilentlyContinue'

function New-D1bSecret {
    # Generated only in the approved external executor. No plaintext password pipeline.
    $bytes=[byte[]]::new(32)
    $rng=[Security.Cryptography.RandomNumberGenerator]::Create()
    $secret=[Security.SecureString]::new()
    try {
        $rng.GetBytes($bytes)
        foreach ($char in 'Dh1!'.ToCharArray()) { $secret.AppendChar($char) }
        $alphabet='0123456789ABCDEF'
        foreach ($b in $bytes) { $secret.AppendChar($alphabet[$b -shr 4]); $secret.AppendChar($alphabet[$b -band 15]) }
        $secret.MakeReadOnly()
        return $secret
    } finally { $rng.Dispose(); [Array]::Clear($bytes,0,$bytes.Length) }
}

function Get-D1bLoginSid($Connection, [string]$Name) {
    $sid=Invoke-D1bSql $Connection 'SELECT CONVERT(varchar(170),sid,1) FROM sys.server_principals WHERE name=@name' @{
        name=(New-D1bParameter NVarChar $Name 128)}
    if ($sid -isnot [string] -or $sid -cnotmatch '^0x[0-9A-F]+$') { throw 'Created SQL login SID was not observed.' }
    return $sid
}

Assert-D1bTarget $Database
Assert-D1bExecutor -Administrator
if ($PSVersionTable.PSEdition -ne 'Desktop' -or -not [Environment]::Is64BitProcess) {
    throw 'Use the approved 64-bit Windows PowerShell 5.1 executor; no module or runtime installation.'
}
$guard=Lock-D1bManifest $Database $ManifestPath
$manifest=$null; $master=$null; $connection=$null; $step=$null
$runtimeSecret=$null; $recoverySecret=$null; $runtimeCredential=$null; $recoveryCredential=$null
try {
    $manifest=Read-D1bManifest $Database $ManifestPath
    Assert-D1bReadyForStep $manifest
    if ($manifest.State -cne 'Bound') { throw 'Provision requires complete install and the fixed binding.' }
    Assert-D1bLocalAccountAbsent
    foreach ($path in @($manifest.PrivateDirectory,$manifest.IdentityDirectory,$manifest.RuntimeCredentialPath,$manifest.RecoveryCredentialPath)) {
        Assert-D1bNoReparse $path
        if (Test-Path -LiteralPath $path) { throw 'Lifecycle path already exists; never read or rotate existing secrets.' }
    }
    $master=Open-D1bDatabase $manifest $Database -Master
    Assert-D1bDatabaseIdentity $master $manifest
    $connection=Open-D1bDatabase $manifest $Database
    Assert-D1bMarkers $connection $manifest
    $names=@{runtime=(New-D1bParameter NVarChar $manifest.RuntimeLogin 128)
        recovery=(New-D1bParameter NVarChar $manifest.RecoveryPrincipal 128)}
    if ((Invoke-D1bSql $master 'SELECT COUNT(*) FROM sys.server_principals WHERE name IN (@runtime,@recovery)' $names) -ne 0 -or
        (Invoke-D1bSql $connection 'SELECT COUNT(*) FROM sys.database_principals WHERE name IN (@runtime,@recovery)' $names) -ne 0) {
        throw 'An exact SQL login/user name exists; no adoption.'
    }
    if ((Invoke-D1bSql $connection "SELECT COUNT(*) FROM sys.database_principals WHERE name IN (N'dh_runtime',N'dh_recovery') AND type='R'") -ne 2) {
        throw 'Migration-owned execute-only roles are missing.'
    }
    $step='CreatePrivateDirectory'
    Start-D1bStep $manifest $step ([pscustomobject]@{Path=$manifest.PrivateDirectory; FullControl=@($manifest.ExecutorSid,'S-1-5-18')})
    Set-D1bDirectoryAcl $manifest.PrivateDirectory $manifest.ExecutorSid
    Complete-D1bStep $manifest $step ([pscustomobject]@{Path=$manifest.PrivateDirectory; OwnerSid=$manifest.ExecutorSid})

    $runtimeSecret=New-D1bSecret
    $recoverySecret=New-D1bSecret
    $runtimeCredential=[Management.Automation.PSCredential]::new($manifest.RuntimeLogin,$runtimeSecret)
    $recoveryCredential=[Management.Automation.PSCredential]::new($manifest.RecoveryPrincipal,$recoverySecret)
    foreach ($kind in @('Runtime','Recovery')) {
        $path=$manifest.($kind+'CredentialPath')
        $credential=$(if ($kind -eq 'Runtime') { $runtimeCredential } else { $recoveryCredential })
        $step='Save'+$kind+'Credential'
        Start-D1bStep $manifest $step ([pscustomobject]@{Path=$path; Format='PSCredential CLIXML / current-executor Windows DPAPI'})
        if (Test-Path -LiteralPath $path) { throw 'Credential path appeared; preserve it without reading.' }
        Assert-D1bDirectoryAcl $manifest.PrivateDirectory $manifest.ExecutorSid
        $serialized=[Management.Automation.PSSerializer]::Serialize($credential)
        $file=New-D1bOwnedFile $path $manifest.ExecutorSid
        try {
            $bytes=[Text.UTF8Encoding]::new($false).GetBytes($serialized)
            $file.Write($bytes,0,$bytes.Length); $file.Flush($true)
        } finally { $file.Dispose(); $serialized=$null; $bytes=$null }
        $manifest.($kind+'CredentialHash')=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        Assert-D1bSecretFile $path $manifest.($kind+'CredentialHash') $manifest
        Complete-D1bStep $manifest $step ([pscustomobject]@{Path=$path; Hash=$manifest.($kind+'CredentialHash'); ExecutorSid=$manifest.ExecutorSid})
    }
    $step='CreateWindowsAccount'
    Start-D1bStep $manifest $step ([pscustomobject]@{Name=$manifest.RecoveryPrincipal; Administrator=$false})
    Assert-D1bLocalAccountAbsent
    # No group, password-policy, service or registry changes. New account only.
    $user=New-LocalUser -Name 'dh_d1b_recovery' -Password $recoverySecret -Description 'Dawnholder D1b 20261002 disposable recovery' -ErrorAction Stop
    $manifest.WindowsAccountSid=$user.SID.Value
    Write-D1bManifest $manifest
    $adminMembers=@(Get-LocalGroupMember -SID ([Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')) -ErrorAction Stop)
    if (@($adminMembers | Where-Object { $_.SID.Value -ceq $manifest.WindowsAccountSid }).Count) { throw 'New recovery account unexpectedly belongs to Administrators.' }
    Complete-D1bStep $manifest $step ([pscustomobject]@{Name=$manifest.RecoveryPrincipal; Sid=$manifest.WindowsAccountSid})

    $step='CreateRuntimeLogin'
    $sid=[byte[]]::new(16)
    $rng=[Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($sid) } finally { $rng.Dispose() }
    Start-D1bStep $manifest $step ([pscustomobject]@{Name=$manifest.RuntimeLogin; PlannedSid=(Get-D1bHex $sid); DefaultDatabase=$Database})
    # The only plaintext boundary is an in-memory typed SQL parameter, never argv or output.
    $password=$null; $parameters=$null
    try {
        $password=$runtimeCredential.GetNetworkCredential().Password
        $parameters=@{name=(New-D1bParameter NVarChar $manifest.RuntimeLogin 128)
            password=(New-D1bParameter NVarChar $password 128); sid=(New-D1bParameter VarBinary $sid 16)}
        [void](Invoke-D1bSql $master @'
IF EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@name) THROW 51120, 'Login name occupied.', 1;
DECLARE @sql nvarchar(max)=N'CREATE LOGIN [dh_d1b_runtime_20261002] WITH PASSWORD='+QUOTENAME(@password,'''')+
 N',SID='+CONVERT(varchar(34),@sid,1)+N',CHECK_POLICY=ON,CHECK_EXPIRATION=OFF,DEFAULT_DATABASE=[Dawnholder_Dev_D1b_20261002];';
EXEC(@sql);
'@ $parameters -Result NonQuery)
    } finally {
        $password=$null
        if ($null -ne $parameters) { $parameters.Clear() }
        $parameters=$null
    }
    $manifest.RuntimeLoginSid=Get-D1bLoginSid $master $manifest.RuntimeLogin
    Write-D1bManifest $manifest
    if ($manifest.RuntimeLoginSid -cne (Get-D1bHex $sid)) { throw 'New SQL login SID differs from the recorded plan.' }
    Complete-D1bStep $manifest $step ([pscustomobject]@{Name=$manifest.RuntimeLogin; Sid=$manifest.RuntimeLoginSid})
    $step='CreateRecoveryLogin'
    Start-D1bStep $manifest $step ([pscustomobject]@{Name=$manifest.RecoveryPrincipal; WindowsSid=$manifest.WindowsAccountSid})
    [void](Invoke-D1bSql $master @'
IF EXISTS(SELECT 1 FROM sys.server_principals WHERE name=N'YYH_DESKTOP\dh_d1b_recovery') THROW 51121, 'Recovery login occupied.', 1;
CREATE LOGIN [YYH_DESKTOP\dh_d1b_recovery] FROM WINDOWS WITH DEFAULT_DATABASE=[Dawnholder_Dev_D1b_20261002];
'@ -Result NonQuery)
    $manifest.RecoveryLoginSid=Get-D1bLoginSid $master $manifest.RecoveryPrincipal
    Write-D1bManifest $manifest
    $windowsSid=[Security.Principal.SecurityIdentifier]::new($manifest.WindowsAccountSid)
    $windowsBytes=[byte[]]::new($windowsSid.BinaryLength); $windowsSid.GetBinaryForm($windowsBytes,0)
    if ($manifest.RecoveryLoginSid -cne (Get-D1bHex $windowsBytes)) { throw 'Recovery login does not match the newly created Windows SID.' }
    Complete-D1bStep $manifest $step ([pscustomobject]@{Name=$manifest.RecoveryPrincipal; Sid=$manifest.RecoveryLoginSid})

    foreach ($kind in @('Runtime','Recovery')) {
        $name=$(if ($kind -eq 'Runtime') { $manifest.RuntimeLogin } else { $manifest.RecoveryPrincipal })
        $role=$(if ($kind -eq 'Runtime') { 'dh_runtime' } else { 'dh_recovery' })
        $step='Create'+$kind+'User'
        Start-D1bStep $manifest $step ([pscustomobject]@{Name=$name; Database=$Database; OnlyExplicitRole=$role})
        $sql=$(if ($kind -eq 'Runtime') {
            'CREATE USER [dh_d1b_runtime_20261002] FOR LOGIN [dh_d1b_runtime_20261002]; ALTER ROLE dh_runtime ADD MEMBER [dh_d1b_runtime_20261002];'
        } else {
            'CREATE USER [YYH_DESKTOP\dh_d1b_recovery] FOR LOGIN [YYH_DESKTOP\dh_d1b_recovery]; ALTER ROLE dh_recovery ADD MEMBER [YYH_DESKTOP\dh_d1b_recovery];'
        })
        $transaction=$connection.BeginTransaction()
        try {
            [void](Invoke-D1bSql $connection $sql @{} $transaction -Result NonQuery)
            $userSid=Invoke-D1bSql $connection 'SELECT CONVERT(varchar(170),sid,1) FROM sys.database_principals WHERE name=@name' @{
                name=(New-D1bParameter NVarChar $name 128)} $transaction
            if ($userSid -cne $manifest.($kind+'LoginSid')) { throw 'User/login SID mismatch.' }
            $transaction.Commit()
            $manifest.($kind+'UserSid')=$userSid
        } finally { $transaction.Dispose() }
        Complete-D1bStep $manifest $step ([pscustomobject]@{Name=$name; Sid=$userSid; Role=$role})
    }
    $step='ValidatePrincipalMetadata'
    Start-D1bStep $manifest $step ([pscustomobject]@{ServerRoles=0; OnlyDatabaseRoles=@('dh_runtime','dh_recovery'); EffectiveIdentityTests='B/C pending'})
    # Actual group/public/server inheritance must also be measured in B/C as each principal.
    if ((Invoke-D1bSql $master @'
SELECT COUNT(*) FROM sys.server_role_members m JOIN sys.server_principals p ON p.principal_id=m.member_principal_id
WHERE p.name IN (@runtime,@recovery);
'@ $names) -ne 0 -or (Invoke-D1bSql $master @'
SELECT COUNT(*) FROM sys.server_permissions x JOIN sys.server_principals p ON p.principal_id=x.grantee_principal_id
WHERE p.name IN (@runtime,@recovery) AND NOT(x.permission_name=N'CONNECT SQL' AND x.state='G');
'@ $names) -ne 0) { throw 'Unexpected explicit server authority on new principals.' }
    if ((Invoke-D1bSql $connection @'
SELECT COUNT(*) FROM sys.database_principals p WHERE p.name IN (@runtime,@recovery) AND
 ((SELECT COUNT(*) FROM sys.database_role_members m WHERE m.member_principal_id=p.principal_id)<>1 OR
 NOT EXISTS(SELECT 1 FROM sys.database_role_members m JOIN sys.database_principals role ON role.principal_id=m.role_principal_id
 WHERE m.member_principal_id=p.principal_id AND ((p.name=@runtime AND role.name=N'dh_runtime') OR
 (p.name=@recovery AND role.name=N'dh_recovery'))) OR
 EXISTS(SELECT 1 FROM sys.database_permissions x WHERE x.grantee_principal_id=p.principal_id AND
 NOT(x.class=0 AND x.permission_name=N'CONNECT' AND x.state='G')) OR
 EXISTS(SELECT 1 FROM sys.schemas s WHERE s.principal_id=p.principal_id));
'@ $names) -ne 0) { throw 'Unexpected explicit database role/grant/ownership on new users.' }
    Complete-D1bStep $manifest $step ([pscustomobject]@{ExplicitServerRoles=0; DirectDatabaseDmlGrants=0; EffectiveIdentityTests='Not executed'})
    $step='CreateIdentityDirectory'
    Start-D1bStep $manifest $step ([pscustomobject]@{Path=$manifest.IdentityDirectory; ReaderSid=$manifest.WindowsAccountSid; Secret=$false})
    Set-D1bDirectoryAcl $manifest.IdentityDirectory $manifest.ExecutorSid $manifest.WindowsAccountSid
    Complete-D1bStep $manifest $step ([pscustomobject]@{Path=$manifest.IdentityDirectory; ReaderSid=$manifest.WindowsAccountSid})
    $step='PublishIdentityManifest'
    Start-D1bStep $manifest $step ([pscustomobject]@{Path=$manifest.IdentityPath; Secret=$false})
    $identity=[pscustomobject]@{
        SchemaVersion=1; Goal=$manifest.Goal; GoalMarker=$manifest.GoalMarker; G0=$manifest.G0; G1=$manifest.G1; G2=$manifest.G2
        Machine=$manifest.Machine; Instance=$manifest.Instance; Endpoint=$manifest.Endpoint; Database=$Database
        Encrypt=$true; TrustServerCertificate=$true; SlotId=1; AccountId=$manifest.AccountId; CharacterId=$manifest.CharacterId
        DatabaseIdentity=$manifest.DatabaseIdentity; Engine=$manifest.Engine; MigrationManifest=$manifest.MigrationManifest
        RuntimeLogin=$manifest.RuntimeLogin; RuntimeLoginSid=$manifest.RuntimeLoginSid; RuntimeUserSid=$manifest.RuntimeUserSid
        RecoveryPrincipal=$manifest.RecoveryPrincipal; WindowsAccountSid=$manifest.WindowsAccountSid
        RecoveryLoginSid=$manifest.RecoveryLoginSid; RecoveryUserSid=$manifest.RecoveryUserSid
        ExecutorSid=$manifest.ExecutorSid; IdentityPath=$manifest.IdentityPath
    }
    $bytes=[Text.UTF8Encoding]::new($false).GetBytes(($identity | ConvertTo-Json -Depth 12))
    $file=New-D1bOwnedFile $manifest.IdentityPath $manifest.ExecutorSid $manifest.WindowsAccountSid
    try { $file.Write($bytes,0,$bytes.Length); $file.Flush($true) } finally { $file.Dispose() }
    $manifest.IdentityHash=(Get-FileHash -LiteralPath $manifest.IdentityPath -Algorithm SHA256).Hash
    $manifest.State='PrincipalsReady'
    Complete-D1bStep $manifest $step ([pscustomobject]@{Path=$manifest.IdentityPath; Hash=$manifest.IdentityHash})
    Write-Output 'New principals, DPAPI files and separate nonsecret child manifest recorded; effective permission and launcher tests remain pending.'
} catch {
    if ($null -ne $manifest -and $step) { Fail-D1bStep $manifest $step (Get-D1bFailureCode $_.Exception) }
    # Neither the native Windows error nor SQL text may leave the secret execution boundary.
    throw 'D1b principal provisioning stopped; preserve every partial resource and manifest. No rotation, adoption or automatic cleanup.'
} finally {
    if ($null -ne $runtimeSecret) { $runtimeSecret.Dispose() }
    if ($null -ne $recoverySecret) { $recoverySecret.Dispose() }
    $runtimeCredential=$null; $recoveryCredential=$null
    if ($null -ne $connection) { $connection.Dispose() }
    if ($null -ne $master) { $master.Dispose() }
    $guard.Dispose()
}
