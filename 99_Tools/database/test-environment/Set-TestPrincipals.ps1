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

$Contract = Read-TestEnvironmentApprovalPlan `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
Assert-TestEnvironmentExecutor -Contract $Contract
if ($PSVersionTable.PSEdition -ne 'Desktop' -or -not [Environment]::Is64BitProcess) {
    throw 'Use the approved 64-bit Windows PowerShell 5.1 executor; no module or runtime installation.'
}
$guard = Lock-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
$manifest = $null
$master = $null
$connection = $null
$step = $null
$runtimeSecret = $null
$runtimeCredential = $null
try {
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    Assert-TestEnvironmentReadyForStep -Manifest $manifest
    if ($manifest.State -cne 'Bound') {
        throw 'Provision requires complete install and the fixed binding.'
    }
    foreach ($path in @(
            $manifest.IdentityDirectory,
            $manifest.RuntimeCredentialPath
        )) {
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
    }
    if ((Invoke-DatabaseSql `
                -Connection $master `
                -Sql 'SELECT COUNT(*) FROM sys.server_principals WHERE name = @runtime' `
                -Parameters $names) -ne 0 -or
        (Invoke-DatabaseSql `
            -Connection $connection `
            -Sql 'SELECT COUNT(*) FROM sys.database_principals WHERE name = @runtime' `
            -Parameters $names) -ne 0) {
        throw 'An exact SQL login/user name exists; no adoption.'
    }
    if ((Invoke-DatabaseSql `
                -Connection $connection `
                -Sql "SELECT COUNT(*) FROM sys.database_principals WHERE name IN (N'dh_runtime',N'dh_recovery') AND type='R'") -ne 2) {
        throw 'Migration-owned execute-only roles are missing.'
    }
    $runtimeSecret = New-TestEnvironmentSecret
    $runtimeCredential = [Management.Automation.PSCredential]::new($manifest.RuntimeLogin, $runtimeSecret)
    $step = 'SaveRuntimeCredential'
    Save-TestEnvironmentCredential -Kind Runtime -Credential $runtimeCredential -Manifest $manifest -Contract $Contract
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
    foreach ($kind in @('Runtime')) {
        $name = $manifest.RuntimeLogin
        $role = 'dh_runtime'
        $step = 'Create' + $kind + 'User'
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
            if ($userSid -cne $manifest.($kind + 'LoginSid')) {
                throw 'User/login SID mismatch.'
            }
            $transaction.Commit()
            $manifest.($kind + 'UserSid') = $userSid
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
            OnlyDatabaseRoles = @('dh_runtime')
            EffectiveIdentityTests = 'B/C pending'
        })
    # Actual group/public/server inheritance must also be measured in B/C as each principal.
    if ((Invoke-DatabaseSql `
                -Connection $master `
                -Sql @'
SELECT COUNT( * ) FROM sys.server_role_members m JOIN sys.server_principals p ON p.principal_id = m.member_principal_id
WHERE p.name = @runtime;
'@ `
                -Parameters $names) -ne 0 -or (Invoke-DatabaseSql `
                -Connection $master `
                -Sql @'
SELECT COUNT( * ) FROM sys.server_permissions x JOIN sys.server_principals p ON p.principal_id = x.grantee_principal_id
WHERE p.name = @runtime AND NOT(x.permission_name = N'CONNECT SQL' AND x.state = 'G');
'@ `
                -Parameters $names) -ne 0) {
        throw 'Unexpected explicit server authority on new principals.'
    }
    if ((Invoke-DatabaseSql `
                -Connection $connection `
                -Sql @'
SELECT COUNT( * ) FROM sys.database_principals p WHERE p.name = @runtime AND
    ((SELECT COUNT( * ) FROM sys.database_role_members m WHERE m.member_principal_id = p.principal_id) <> 1 OR
    NOT EXISTS(SELECT 1 FROM sys.database_role_members m JOIN sys.database_principals role ON role.principal_id = m.role_principal_id
    WHERE m.member_principal_id = p.principal_id AND role.name = N'dh_runtime') OR
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
            Secret = $false
        })
    Set-TestEnvironmentDirectoryAcl `
        -Contract $Contract `
        -Path $manifest.IdentityDirectory `
        -ExecutorSid $manifest.ExecutorSid
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Path = $manifest.IdentityDirectory
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
        -ExecutorSid $manifest.ExecutorSid
    try {
        $file.Write($bytes, 0, $bytes.Length)
        $file.Flush($true)
    } finally {
        $file.Dispose()
    }
    $manifest.IdentityHash = (Get-FileHash -LiteralPath $manifest.IdentityPath -Algorithm SHA256).Hash
    # PrincipalsReady means runtime is ready; recovery roles remain migration-owned without a user.
    $manifest.State = 'PrincipalsReady'
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $step `
        -Identity ([pscustomobject]@{
            Path = $manifest.IdentityPath
            Hash = $manifest.IdentityHash
        })
    Write-Output 'Runtime principal, DPAPI file and separate nonsecret child manifest recorded; effective permission and launcher tests remain pending.'
} catch {
    $failure = $_.Exception
    if ($null -ne $manifest -and $step) {
        Fail-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $step `
            -FailureCode (Get-DatabaseFailureCode -Exception $failure)
    }
    # Keep the fixed connection category and SQL number without exposing native text or secret SQL parameters.
    $summary = Get-TestEnvironmentFailureSummary -Exception $failure
    throw (('Test environment principal provisioning stopped. {0} ' +
        'Preserve every partial resource and manifest; no rotation, adoption or automatic cleanup.') -f $summary)
} finally {
    if ($null -ne $runtimeSecret) {
        $runtimeSecret.Dispose()
    }
    $runtimeCredential = $null
    if ($null -ne $connection) {
        $connection.Dispose()
    }
    if ($null -ne $master) {
        $master.Dispose()
    }
    $guard.Dispose()
}
