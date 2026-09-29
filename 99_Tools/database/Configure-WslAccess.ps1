# Optional administrator stage. NOT run by Install-Database.ps1.
# Plan is read-only. Enable/Restore require a separately approved maintenance window.
[CmdletBinding()]
param([ValidateSet('Plan','Enable','Restore')][string]$Action = 'Plan')
. (Join-Path $PSScriptRoot 'Database.Common.ps1')
$instance = '.\SQLEXPRESS'
$database = 'Dawnholder_Dev'
$port = 14330
$serviceName = 'MSSQL$SQLEXPRESS'
$instanceId = (Get-ItemProperty 'HKLM:/SOFTWARE/Microsoft/Microsoft SQL Server/Instance Names/SQL').SQLEXPRESS
if ($instanceId -notmatch '^MSSQL[0-9]+\.SQLEXPRESS$') { throw 'Unexpected SQL instance registry mapping.' }
$serverKey = "HKLM:/SOFTWARE/Microsoft/Microsoft SQL Server/$instanceId/MSSQLServer"
$tcpKey = "$serverKey/SuperSocketNetLib/Tcp"
$stateDirectory = Join-Path $env:LOCALAPPDATA 'Dawnholder/MssqlWsl-SQLEXPRESS'
$statePath = Join-Path $stateDirectory 'state.clixml'
$credentialPath = Join-Path $stateDirectory 'credential.clixml'
$changes = @(
    [pscustomobject]@{Path=$serverKey;Name='LoginMode';Before=(Get-ItemProperty $serverKey).LoginMode;After=2},
    [pscustomobject]@{Path=$tcpKey;Name='Enabled';Before=(Get-ItemProperty $tcpKey).Enabled;After=1},
    [pscustomobject]@{Path=$tcpKey;Name='ListenOnAllIPs';Before=(Get-ItemProperty $tcpKey).ListenOnAllIPs;After=0}
)
$loopbacks = @()
foreach ($key in Get-ChildItem $tcpKey) {
    if ($key.PSChildName -eq 'IPAll') { continue }
    $ip = Get-ItemProperty $key.PSPath
    $isLoopback = $ip.IpAddress -in @('127.0.0.1','::1')
    $changes += [pscustomobject]@{Path=$key.PSPath;Name='Enabled';Before=$ip.Enabled;After=[int]$isLoopback}
    if ($isLoopback) {
        $loopbacks += $ip.IpAddress
        $changes += [pscustomobject]@{Path=$key.PSPath;Name='TcpPort';Before=$ip.TcpPort;After=[string]$port}
        $changes += [pscustomobject]@{Path=$key.PSPath;Name='TcpDynamicPorts';Before=$ip.TcpDynamicPorts;After=''}
    }
}
if ($Action -eq 'Plan') {
    $changes | Select-Object Path,Name,Before,After | Format-List
    Write-Output "Additional changes: new random SQL login; user in $database; SELECT/INSERT/UPDATE on the three game tables; SELECT on SchemaVersion."
    Write-Output "One service restart for Enable, one for Restore. No SQL Browser or firewall changes. Port: $port, loopback only."
    Write-Output "State and Windows DPAPI credential: $stateDirectory (created only by Enable)."
    Write-Output 'Restore reverts saved registry values and disables only the newly created login; it preserves all data, user and credential files.'
    return
}
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal $identity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run from an approved elevated PowerShell; this script never opens UAC.' }
if ($Action -eq 'Enable') {
    if (Test-Path -LiteralPath $stateDirectory) { throw 'State directory already exists. Inspect/restore the prior attempt; never rotate an existing credential implicitly.' }
    if ((Get-Service $serviceName).Status -ne 'Running') { throw 'SQL service must already be running.' }
    if ((Get-ItemProperty $tcpKey).Enabled -ne 0) { throw 'This plan requires initially disabled TCP; review an already networked instance separately.' }
    if ((Get-ItemProperty $serverKey).LoginMode -ne 1) { throw 'This plan requires initial Windows-only authentication.' }
    if ('127.0.0.1' -notin $loopbacks -or '::1' -notin $loopbacks) { throw 'Both loopback registry entries are required.' }
    if (@(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue).Count -gt 0) { throw 'Target TCP port is already occupied.' }
    $connection = Open-LocalDatabase $instance $database
    try {
        Assert-DatabaseOwner $connection
        if ((Invoke-DbScalar $connection "SELECT IS_SRVROLEMEMBER('sysadmin')") -ne 1) { throw 'SQL sysadmin required for this optional administrator stage.' }
        if ((Invoke-DbScalar $connection 'SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process=1 AND session_id<>@@SPID') -ne 0) { throw 'Other SQL sessions exist. Arrange a quiet maintenance window first.' }
        [void](New-Item -ItemType Directory -Path $stateDirectory)
        $acl = Get-Acl -LiteralPath $stateDirectory
        $acl.SetAccessRuleProtection($true,$false)
        foreach ($sid in @($identity.User, [Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
            $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($sid,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
        }
        Set-Acl -LiteralPath $stateDirectory -AclObject $acl
        $login = 'Dawnholder_Dev_Wsl_' + [Guid]::NewGuid().ToString('N').Substring(0,12)
        $sidBytes = [Guid]::NewGuid().ToByteArray()
        $state = [pscustomobject]@{InstanceId=$instanceId;Changes=$changes;Login=$login;Sid=$sidBytes;Database=$database;Port=$port;Restored=$false}
        $state | Export-Clixml -LiteralPath $statePath
        $random = New-Object byte[] 32
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($random) } finally { $rng.Dispose() }
        $password = 'Dh!' + ([BitConverter]::ToString($random)).Replace('-','')
        $credential = [Management.Automation.PSCredential]::new($login,(ConvertTo-SecureString $password -AsPlainText -Force))
        $credential | Export-Clixml -LiteralPath $credentialPath
        # Secret is a SQL parameter, never a CLI argument or printed SQL string.
        [void](Invoke-DbNonQuery $connection @'
DECLARE @sql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@login)
    + N' WITH PASSWORD=' + QUOTENAME(@password,'''') + N', SID=' + CONVERT(varchar(34),@sid,1)
    + N', CHECK_POLICY=ON, CHECK_EXPIRATION=OFF, DEFAULT_DATABASE=[Dawnholder_Dev];';
EXEC(@sql);
SET @sql = N'CREATE USER ' + QUOTENAME(@login) + N' FOR LOGIN ' + QUOTENAME(@login) + N';'
    + N'GRANT SELECT,INSERT,UPDATE ON OBJECT::dh.Account TO ' + QUOTENAME(@login) + N';'
    + N'GRANT SELECT,INSERT,UPDATE ON OBJECT::dh.Character TO ' + QUOTENAME(@login) + N';'
    + N'GRANT SELECT,INSERT,UPDATE ON OBJECT::dh.CharacterProgress TO ' + QUOTENAME(@login) + N';'
    + N'GRANT SELECT ON OBJECT::dh.SchemaVersion TO ' + QUOTENAME(@login) + N';';
EXEC(@sql);
'@ @{login=$login;password=$password;sid=$sidBytes})
        $password = $null
        foreach ($change in $changes) { Set-ItemProperty -LiteralPath $change.Path -Name $change.Name -Value $change.After }
    } finally { $connection.Dispose(); $password = $null }
    # A failure leaves the saved state available for explicit Restore, never blind retries.
    Restart-Service -Name $serviceName -ErrorAction Stop
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction Stop)
    if ($listeners.Count -eq 0 -or @($listeners | Where-Object LocalAddress -NotIn @('127.0.0.1','::1')).Count -gt 0) {
        throw 'Unexpected listener state. Run Restore immediately in the approved maintenance window.'
    }
    Write-Output 'Enable completed. Run Test-WslAccess.ps1 to verify native WSL authentication and runtime permissions.'
    return
}

if (-not (Test-Path -LiteralPath $statePath)) { throw 'No saved state to restore.' }
$state = Import-Clixml -LiteralPath $statePath
if ($state.InstanceId -ne $instanceId -or $state.Database -ne $database) { throw 'Backup target mismatch.' }
if ($state.Restored) { Write-Output 'Already restored.'; return }
# Do not overwrite later administrator changes silently.
foreach ($change in $state.Changes) {
    $current = (Get-ItemProperty -LiteralPath $change.Path).($change.Name)
    if ([string]$current -ne [string]$change.Before -and [string]$current -ne [string]$change.After) { throw 'Configuration changed since Enable. Review saved state manually before restoring.' }
}
if ((Get-Service $serviceName).Status -eq 'Running') {
    $connection = Open-LocalDatabase $instance 'master'
    try {
        if ((Invoke-DbScalar $connection 'SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process=1 AND session_id<>@@SPID') -ne 0) { throw 'Other SQL sessions exist. Arrange a quiet maintenance window first.' }
        [void](Invoke-DbNonQuery $connection @'
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name=@login)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name=@login AND sid=@sid)
        THROW 51006, 'Saved login SID mismatch; refusing to disable an unrelated login.', 1;
    DECLARE @sql nvarchar(max)=N'ALTER LOGIN '+QUOTENAME(@login)+N' DISABLE;'; EXEC(@sql);
END
'@ @{login=$state.Login;sid=[byte[]]$state.Sid})
    } finally { $connection.Dispose() }
}
foreach ($change in $state.Changes) { Set-ItemProperty -LiteralPath $change.Path -Name $change.Name -Value $change.Before }
if ((Get-Service $serviceName).Status -eq 'Running') { Restart-Service -Name $serviceName -ErrorAction Stop }
else { Start-Service -Name $serviceName -ErrorAction Stop }
# Also covers restoring after a failed service start, when the pre-restart disable was unavailable.
$connection = Open-LocalDatabase $instance 'master'
try {
    [void](Invoke-DbNonQuery $connection @'
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name=@login AND sid=@sid)
BEGIN
    DECLARE @sql nvarchar(max)=N'ALTER LOGIN '+QUOTENAME(@login)+N' DISABLE;'; EXEC(@sql);
END
'@ @{login=$state.Login;sid=[byte[]]$state.Sid})
} finally { $connection.Dispose() }
$state.Restored = $true
$state | Export-Clixml -LiteralPath $statePath
Write-Output 'Original registry configuration restored and new login disabled. Database data and DPAPI backup retained.'
