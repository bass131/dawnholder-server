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
$VerbosePreference = 'SilentlyContinue'
$DebugPreference = 'SilentlyContinue'

function New-TestEnvironmentSecret {
    # Generated only in the approved external executor. No plaintext password pipeline.
    $bytes = [byte[]]::new(32)
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $secret = [Security.SecureString]::new()
    try {
        $rng.GetBytes($bytes)
        foreach ($char in 'Dh1!'.ToCharArray()) {
            $secret.AppendChar($char)
        }
        $alphabet = '0123456789ABCDEF'
        foreach ($b in $bytes) {
            $secret.AppendChar($alphabet[$b -shr 4])
            $secret.AppendChar($alphabet[$b -band 15])
        }
        $secret.MakeReadOnly()
        return $secret
    } finally {
        $rng.Dispose()
        [Array]::Clear($bytes, 0, $bytes.Length)
    }
}

function Get-TestEnvironmentLoginSid(
    $Connection,
    [string]$Name
) {
    $sid = Invoke-DatabaseSql `
        -Connection $Connection `
        -Sql 'SELECT CONVERT(varchar(170),sid,1) FROM sys.server_principals WHERE name=@name' `
        -Parameters @{
            name = (New-DatabaseSqlParameter -Type NVarChar -Value $Name -Size 128)
        }
    if ($sid -isnot [string] -or $sid -cnotmatch '^0x[0-9A-F]+$') {
        throw 'Created SQL login SID was not observed.'
    }
    return $sid
}

$Contract = Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $ApprovalPlanPath -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
Assert-TestEnvironmentExecutor -Contract $Contract -Administrator
if ($PSVersionTable.PSEdition -ne 'Desktop' -or -not [Environment]::Is64BitProcess) {
    throw 'Use the approved 64-bit Windows PowerShell 5.1 executor; no module or runtime installation.'
}
$guard = Lock-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
$manifest = $null
$master = $null
$connection = $null
$step = $null
$runtimeSecret = $null
$recoverySecret = $null
$runtimeCredential = $null
$recoveryCredential = $null
try {
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    Assert-TestEnvironmentReadyForStep -Manifest $manifest
    if ($manifest.State -cne 'Bound') {
        throw 'Provision requires complete install and the fixed binding.'
    }
    Assert-TestEnvironmentLocalAccountAbsent -Contract $Contract
    foreach ($path in @($manifest.PrivateDirectory, $manifest.IdentityDirectory, $manifest.RuntimeCredentialPath, $manifest.RecoveryCredentialPath)) {
        Assert-TestEnvironmentNoReparse -Path $path
        if (Test-Path -LiteralPath $path) {
            throw 'Lifecycle path already exists; never read or rotate existing secrets.'
        }
    }
    $master = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database -Master
    Assert-TestEnvironmentDatabaseIdentity -Master $master -Manifest $manifest
    $connection = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database
    Assert-TestEnvironmentMarkers -Connection $connection -Manifest $manifest
    $names = @{
        runtime = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.RuntimeLogin -Size 128)
        recovery = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.RecoveryPrincipal -Size 128)
    }
    if ((Invoke-DatabaseSql `
        -Connection $master `
        -Sql 'SELECT COUNT(*) FROM sys.server_principals WHERE name IN (@runtime,@recovery)' `
        -Parameters $names) -ne 0 -or
        (Invoke-DatabaseSql `
        -Connection $connection `
        -Sql 'SELECT COUNT(*) FROM sys.database_principals WHERE name IN (@runtime,@recovery)' `
        -Parameters $names) -ne 0) {
            throw 'An exact SQL login/user name exists; no adoption.'
        }
    if ((Invoke-DatabaseSql `
        -Connection $connection `
        -Sql "SELECT COUNT(*) FROM sys.database_principals WHERE name IN (N'dh_runtime',N'dh_recovery') AND type='R'") -ne 2) {
            throw 'Migration-owned execute-only roles are missing.'
        }
    $step = 'CreatePrivateDirectory'
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            Path = $manifest.PrivateDirectory
            FullControl = @($manifest.ExecutorSid, 'S-1-5-18')
        })
    Set-TestEnvironmentDirectoryAcl -Contract $Contract -Path $manifest.PrivateDirectory -ExecutorSid $manifest.ExecutorSid
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Path = $manifest.PrivateDirectory
            OwnerSid = $manifest.ExecutorSid
        })

    $runtimeSecret = New-TestEnvironmentSecret
    $recoverySecret = New-TestEnvironmentSecret
    $runtimeCredential = [Management.Automation.PSCredential]::new($manifest.RuntimeLogin, $runtimeSecret)
    $recoveryCredential = [Management.Automation.PSCredential]::new($manifest.RecoveryPrincipal, $recoverySecret)
    foreach ($kind in @('Runtime', 'Recovery')) {
        $path = $manifest.($kind+'CredentialPath')
        $credential = $(if ($kind -eq 'Runtime') {
            $runtimeCredential
        } else {
            $recoveryCredential
        })
        $step = 'Save'+$kind+'Credential'
        Start-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $step `
            -Plan ([pscustomobject]@{
                Path = $path
                Format = 'PSCredential CLIXML / current-executor Windows DPAPI'
            })
        if (Test-Path -LiteralPath $path) {
            throw 'Credential path appeared; preserve it without reading.'
        }
        Assert-TestEnvironmentDirectoryAcl -Path $manifest.PrivateDirectory -ExecutorSid $manifest.ExecutorSid
        $serialized = [Management.Automation.PSSerializer]::Serialize($credential)
        $file = New-TestEnvironmentOwnedFile -Contract $Contract -Path $path -ExecutorSid $manifest.ExecutorSid
        try {
            $bytes = [Text.UTF8Encoding]::new($false).GetBytes($serialized)
            $file.Write($bytes, 0, $bytes.Length)
            $file.Flush($true)
        } finally {
            $file.Dispose()
            $serialized = $null
            $bytes = $null
        }
        $manifest.($kind+'CredentialHash') = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        Assert-TestEnvironmentSecretFile `
            -Contract $Contract `
            -Path $path `
            -ExpectedHash $manifest.($kind+'CredentialHash') `
            -Manifest $manifest
        Complete-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $step `
            -Identity ([pscustomobject]@{
                Path = $path
                Hash = $manifest.($kind+'CredentialHash')
                ExecutorSid = $manifest.ExecutorSid
            })
    }
    $step = 'CreateWindowsAccount'
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            Name = $manifest.RecoveryPrincipal
            Administrator = $false
        })
    Assert-TestEnvironmentLocalAccountAbsent -Contract $Contract
    # No group, password-policy, service or registry changes. New account only.
    $user = New-LocalUser `
        -Name $Contract.RecoveryLocalName `
        -Password $recoverySecret `
        -Description 'Dawnholder disposable recovery test principal' `
        -ErrorAction Stop
    $manifest.WindowsAccountSid = $user.SID.Value
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    $adminMembers = @(Get-LocalGroupMember -SID ([Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')) -ErrorAction Stop)
    if (@($adminMembers | Where-Object {
        $_.SID.Value -ceq $manifest.WindowsAccountSid
    }).Count) {
        throw 'New recovery account unexpectedly belongs to Administrators.'
    }
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Name = $manifest.RecoveryPrincipal
            Sid = $manifest.WindowsAccountSid
        })

    $step = 'CreateRuntimeLogin'
    $sid = [byte[]]::new(16)
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($sid)
    } finally {
        $rng.Dispose()
    }
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            Name = $manifest.RuntimeLogin
            PlannedSid = (ConvertTo-DatabaseSidHex -Bytes $sid)
            DefaultDatabase = $Database
        })
    # The only plaintext boundary is an in-memory typed SQL parameter, never argv or output.
    $password = $null
    $parameters = $null
    try {
        $password = $runtimeCredential.GetNetworkCredential().Password
        $parameters = @{
            name = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.RuntimeLogin -Size 128)
            password = (New-DatabaseSqlParameter -Type NVarChar -Value $password -Size 128)
            sid = (New-DatabaseSqlParameter -Type VarBinary -Value $sid -Size 16)
            database = (New-DatabaseSqlParameter -Type NVarChar -Value $Database -Size 128)
        }
        [void](Invoke-DatabaseSql `
            -Connection $master `
            -Sql @'
IF EXISTS(SELECT 1 FROM sys.server_principals WHERE name = @name)
    THROW 51120, 'Login name occupied.', 1;
DECLARE @sql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@name) + N' WITH PASSWORD=' + QUOTENAME(@password, '''') +
    N',SID=' + CONVERT(varchar(34), @sid, 1) + N',CHECK_POLICY=ON,CHECK_EXPIRATION=OFF,DEFAULT_DATABASE=' + QUOTENAME(@database) + N';';
EXEC(@sql);
'@ `
            -Parameters $parameters `
            -Result NonQuery)
    } finally {
        $password = $null
        if ($null -ne $parameters) {
            $parameters.Clear()
        }
        $parameters = $null
    }
    $manifest.RuntimeLoginSid = Get-TestEnvironmentLoginSid -Connection $master -Name $manifest.RuntimeLogin
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    if ($manifest.RuntimeLoginSid -cne (ConvertTo-DatabaseSidHex -Bytes $sid)) {
        throw 'New SQL login SID differs from the recorded plan.'
    }
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Name = $manifest.RuntimeLogin
            Sid = $manifest.RuntimeLoginSid
        })
    $step = 'CreateRecoveryLogin'
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            Name = $manifest.RecoveryPrincipal
            WindowsSid = $manifest.WindowsAccountSid
        })
    [void](Invoke-DatabaseSql `
        -Connection $master `
        -Sql @'
IF EXISTS(SELECT 1 FROM sys.server_principals WHERE name = @name)
    THROW 51121, 'Recovery login occupied.', 1;
DECLARE @sql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@name) + N' FROM WINDOWS WITH DEFAULT_DATABASE=' + QUOTENAME(@database) + N';';
EXEC sys.sp_executesql @stmt = @sql;
'@ `
        -Parameters @{
            name = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.RecoveryPrincipal -Size 128)
            database = (New-DatabaseSqlParameter -Type NVarChar -Value $Database -Size 128)
        } `
        -Result NonQuery)
    $manifest.RecoveryLoginSid = Get-TestEnvironmentLoginSid -Connection $master -Name $manifest.RecoveryPrincipal
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
    $windowsSid = [Security.Principal.SecurityIdentifier]::new($manifest.WindowsAccountSid)
    $windowsBytes = [byte[]]::new($windowsSid.BinaryLength)
    $windowsSid.GetBinaryForm($windowsBytes, 0)
    if ($manifest.RecoveryLoginSid -cne (ConvertTo-DatabaseSidHex -Bytes $windowsBytes)) {
        throw 'Recovery login does not match the newly created Windows SID.'
    }
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Name = $manifest.RecoveryPrincipal
            Sid = $manifest.RecoveryLoginSid
        })

    foreach ($kind in @('Runtime', 'Recovery')) {
        $name = $(if ($kind -eq 'Runtime') {
            $manifest.RuntimeLogin
        } else {
            $manifest.RecoveryPrincipal
        })
        $role = $(if ($kind -eq 'Runtime') {
            'dh_runtime'
        } else {
            'dh_recovery'
        })
        $step = 'Create'+$kind+'User'
        Start-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $step `
            -Plan ([pscustomobject]@{
                Name = $name
                Database = $Database
                OnlyExplicitRole = $role
            })
        # The role is migration-owned; the principal is the exact reviewed environment input.
        $sql = @'
DECLARE @sql nvarchar(max) = N'CREATE USER ' + QUOTENAME(@name) + N' FOR LOGIN ' + QUOTENAME(@name) +
    N'; ALTER ROLE ' + QUOTENAME(@role) + N' ADD MEMBER ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt = @sql;
'@
        $transaction = $connection.BeginTransaction()
        try {
            [void](Invoke-DatabaseSql `
                -Connection $connection `
                -Sql $sql `
                -Parameters @{
                    name = (New-DatabaseSqlParameter -Type NVarChar -Value $name -Size 128)
                    role = (New-DatabaseSqlParameter -Type NVarChar -Value $role -Size 128)
                } `
                -Transaction $transaction `
                -Result NonQuery)
            $userSid = Invoke-DatabaseSql `
                -Connection $connection `
                -Sql 'SELECT CONVERT(varchar(170),sid,1) FROM sys.database_principals WHERE name=@name' `
                -Parameters @{
                    name = (New-DatabaseSqlParameter -Type NVarChar -Value $name -Size 128)
                } `
                -Transaction $transaction
            if ($userSid -cne $manifest.($kind+'LoginSid')) {
                throw 'User/login SID mismatch.'
            }
            $transaction.Commit()
            $manifest.($kind+'UserSid') = $userSid
        } finally {
            $transaction.Dispose()
        }
        Complete-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $step `
            -Identity ([pscustomobject]@{
                Name = $name
                Sid = $userSid
                Role = $role
            })
    }
    $step = 'ValidatePrincipalMetadata'
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            ServerRoles = 0
            OnlyDatabaseRoles = @('dh_runtime', 'dh_recovery')
            EffectiveIdentityTests = 'B/C pending'
        })
    # Actual group/public/server inheritance must also be measured in B/C as each principal.
    if ((Invoke-DatabaseSql `
        -Connection $master `
        -Sql @'
SELECT COUNT( * ) FROM sys.server_role_members m JOIN sys.server_principals p ON p.principal_id = m.member_principal_id
WHERE p.name IN (@runtime, @recovery);
'@ `
        -Parameters $names) -ne 0 -or (Invoke-DatabaseSql `
        -Connection $master `
        -Sql @'
SELECT COUNT( * ) FROM sys.server_permissions x JOIN sys.server_principals p ON p.principal_id = x.grantee_principal_id
WHERE p.name IN (@runtime, @recovery) AND NOT(x.permission_name = N'CONNECT SQL' AND x.state = 'G');
'@ `
        -Parameters $names) -ne 0) {
            throw 'Unexpected explicit server authority on new principals.'
        }
    if ((Invoke-DatabaseSql `
        -Connection $connection `
        -Sql @'
SELECT COUNT( * ) FROM sys.database_principals p WHERE p.name IN (@runtime, @recovery) AND
    ((SELECT COUNT( * ) FROM sys.database_role_members m WHERE m.member_principal_id = p.principal_id) <> 1 OR
    NOT EXISTS(SELECT 1 FROM sys.database_role_members m JOIN sys.database_principals role ON role.principal_id = m.role_principal_id
    WHERE m.member_principal_id = p.principal_id AND ((p.name = @runtime AND role.name = N'dh_runtime') OR
    (p.name = @recovery AND role.name = N'dh_recovery'))) OR
    EXISTS(SELECT 1 FROM sys.database_permissions x WHERE x.grantee_principal_id = p.principal_id AND
    NOT(x.class = 0 AND x.permission_name = N'CONNECT' AND x.state = 'G')) OR
    EXISTS(SELECT 1 FROM sys.schemas s WHERE s.principal_id = p.principal_id));
'@ `
        -Parameters $names) -ne 0) {
            throw 'Unexpected explicit database role/grant/ownership on new users.'
        }
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            ExplicitServerRoles = 0
            DirectDatabaseDmlGrants = 0
            EffectiveIdentityTests = 'Not executed'
        })
    $step = 'CreateIdentityDirectory'
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            Path = $manifest.IdentityDirectory
            ReaderSid = $manifest.WindowsAccountSid
            Secret = $false
        })
    Set-TestEnvironmentDirectoryAcl `
        -Contract $Contract `
        -Path $manifest.IdentityDirectory `
        -ExecutorSid $manifest.ExecutorSid `
        -ReaderSid $manifest.WindowsAccountSid
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Path = $manifest.IdentityDirectory
            ReaderSid = $manifest.WindowsAccountSid
        })
    $step = 'PublishIdentityManifest'
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Plan ([pscustomobject]@{
            Path = $manifest.IdentityPath
            Secret = $false
        })
    $identity = New-TestEnvironmentChildIdentity -Manifest $manifest
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($identity | ConvertTo-Json -Depth 12))
    $file = New-TestEnvironmentOwnedFile `
        -Contract $Contract `
        -Path $manifest.IdentityPath `
        -ExecutorSid $manifest.ExecutorSid `
        -ReaderSid $manifest.WindowsAccountSid
    try {
        $file.Write($bytes, 0, $bytes.Length)
        $file.Flush($true)
    } finally {
        $file.Dispose()
    }
    $manifest.IdentityHash = (Get-FileHash -LiteralPath $manifest.IdentityPath -Algorithm SHA256).Hash
    $manifest.State = 'PrincipalsReady'
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Path = $manifest.IdentityPath
            Hash = $manifest.IdentityHash
        })
    Write-Output 'New principals, DPAPI files and separate nonsecret child manifest recorded; effective permission and launcher tests remain pending.'
} catch {
    if ($null -ne $manifest -and $step) {
        Fail-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $step `
            -FailureCode (Get-DatabaseFailureCode -Exception $_.Exception)
    }
    # Neither the native Windows error nor SQL text may leave the secret execution boundary.
    throw 'Test environment principal provisioning stopped; preserve every partial resource and manifest. No rotation, adoption or automatic cleanup.'
} finally {
    if ($null -ne $runtimeSecret) {
        $runtimeSecret.Dispose()
    }
    if ($null -ne $recoverySecret) {
        $recoverySecret.Dispose()
    }
    $runtimeCredential = $null
    $recoveryCredential = $null
    if ($null -ne $connection) {
        $connection.Dispose()
    }
    if ($null -ne $master) {
        $master.Dispose()
    }
    $guard.Dispose()
}
