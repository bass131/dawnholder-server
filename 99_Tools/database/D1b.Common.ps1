# Definitions only. Importing this file does not inspect SQL, Windows identities or secrets.
function Get-D1bContract {
    $repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    [pscustomobject]@{
        SchemaVersion = 1
        Goal = '01_Phases/goals/2026-10-02-persistence-repository/goal.md'
        GoalMarker = 'persistence-repository-d1b-20261002'
        G0 = 'msg_78c5c1647b7c'; G1 = 'msg_21ae101a52db'
        Machine = 'YYH_DESKTOP'; Instance = '.\SQLEXPRESS'
        Endpoint = 'tcp:127.0.0.1,14330'; Database = 'Dawnholder_Dev_D1b_20261002'
        SlotId = 1
        AccountId = '828e39df-ba5d-4209-86ea-4e9ec1a43ed5'
        CharacterId = '686e8071-daa1-404d-af7d-d4bb2442748c'
        RuntimeLogin = 'dh_d1b_runtime_20261002'
        RecoveryPrincipal = 'YYH_DESKTOP\dh_d1b_recovery'
        RecoveryLocalName = 'dh_d1b_recovery'
        ExecutorSid = 'S-1-5-21-3452356722-1322878128-3924384381-1001'
        ManifestPath = Join-Path $repository '.backups/verification/2026-10-02-persistence-repository/fixture-manifest.json'
        PrivateDirectory = 'C:\ProgramData\Dawnholder-D1b-20261002'
        IdentityDirectory = 'C:\ProgramData\Dawnholder-D1b-20261002-identity'
        RuntimeCredentialPath = 'C:\ProgramData\Dawnholder-D1b-20261002\runtime.clixml'
        RecoveryCredentialPath = 'C:\ProgramData\Dawnholder-D1b-20261002\recovery.clixml'
        IdentityPath = 'C:\ProgramData\Dawnholder-D1b-20261002-identity\identity.json'
    }
}

function Assert-D1bTarget([string]$Database, [string]$Instance = '.\SQLEXPRESS') {
    $c = Get-D1bContract
    if ([string]::IsNullOrWhiteSpace($Database) -or $Database -cne $c.Database -or $Instance -cne $c.Instance) {
        throw 'D1b requires the explicit exact approved database and local instance.'
    }
}

function Assert-D1bPath([string]$Path, [string]$Expected) {
    if ([string]::IsNullOrWhiteSpace($Path) -or -not [IO.Path]::IsPathRooted($Path) -or
        -not [string]::Equals([IO.Path]::GetFullPath($Path), $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'D1b path is not the exact approved absolute path.'
    }
}

function Assert-D1bNoReparse([string]$Path) {
    # Inspect only ancestors of an exact authorized path; never enumerate sibling files.
    $itemPath = [IO.Path]::GetFullPath($Path)
    while ($itemPath) {
        if (Test-Path -LiteralPath $itemPath) {
            if ((Get-Item -LiteralPath $itemPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw 'D1b paths must not traverse reparse points.'
            }
        }
        $itemPath = [IO.Path]::GetDirectoryName($itemPath)
    }
}

function New-D1bManifest([string]$Database, [string]$G2ApprovalMessage) {
    Assert-D1bTarget $Database
    if ($G2ApprovalMessage -cnotmatch '^msg_[a-f0-9]{12}$') { throw 'Record the actual G2 approval message, not G0/G1.' }
    $c = Get-D1bContract
    if ($G2ApprovalMessage -in @($c.G0, $c.G1)) { throw 'G0/G1 do not authorize execution.' }
    [pscustomobject]@{
        SchemaVersion = 1; Goal = $c.Goal; GoalMarker = $c.GoalMarker
        G0 = $c.G0; G1 = $c.G1; G2 = $G2ApprovalMessage
        Machine = $c.Machine; Instance = $c.Instance; Endpoint = $c.Endpoint; Database = $c.Database
        SlotId = 1; AccountId = $c.AccountId; CharacterId = $c.CharacterId
        RuntimeLogin = $c.RuntimeLogin; RecoveryPrincipal = $c.RecoveryPrincipal
        ExecutorSid = $c.ExecutorSid; Encrypt = $true; TrustServerCertificate = $true
        ManifestPath = $c.ManifestPath; PrivateDirectory = $c.PrivateDirectory
        IdentityDirectory = $c.IdentityDirectory; IdentityPath = $c.IdentityPath
        RuntimeCredentialPath = $c.RuntimeCredentialPath; RecoveryCredentialPath = $c.RecoveryCredentialPath
        CreatedUtc = [DateTime]::UtcNow.ToString('o'); UpdatedUtc = [DateTime]::UtcNow.ToString('o')
        State = 'Planned'; Engine = $null; DatabaseIdentity = $null; MigrationManifest = @()
        WindowsAccountSid = $null; RuntimeLoginSid = $null; RecoveryLoginSid = $null
        RuntimeUserSid = $null; RecoveryUserSid = $null
        RuntimeCredentialHash = $null; RecoveryCredentialHash = $null; IdentityHash = $null
        Steps = @(); Cleanup = $null
    }
}

function Assert-D1bManifest($Manifest, [string]$Database, [string]$ManifestPath) {
    Assert-D1bTarget $Database
    $c = Get-D1bContract
    Assert-D1bPath $ManifestPath $c.ManifestPath
    foreach ($key in @('SchemaVersion','Goal','GoalMarker','G0','G1','Machine','Instance','Endpoint','Database',
        'SlotId','AccountId','CharacterId','RuntimeLogin','RecoveryPrincipal','ExecutorSid','ManifestPath',
        'PrivateDirectory','IdentityDirectory','IdentityPath','RuntimeCredentialPath','RecoveryCredentialPath')) {
        if ($null -eq $Manifest.PSObject.Properties[$key] -or [string]$Manifest.$key -cne [string]$c.$key) {
            throw "D1b manifest contract mismatch: $key."
        }
    }
    if ($Manifest.Encrypt -isnot [bool] -or -not $Manifest.Encrypt -or
        $Manifest.TrustServerCertificate -isnot [bool] -or -not $Manifest.TrustServerCertificate -or
        $Manifest.G2 -cnotmatch '^msg_[a-f0-9]{12}$' -or $Manifest.G2 -in @($c.G0,$c.G1)) {
        throw 'Invalid execution approval or fixture encryption contract.'
    }
    foreach ($key in @('State','Engine','DatabaseIdentity','MigrationManifest','WindowsAccountSid','RuntimeLoginSid',
        'RecoveryLoginSid','RuntimeUserSid','RecoveryUserSid','RuntimeCredentialHash','RecoveryCredentialHash','IdentityHash','Steps','Cleanup')) {
        if ($null -eq $Manifest.PSObject.Properties[$key]) { throw "Missing lifecycle field: $key." }
    }
    if ($Manifest.SchemaVersion -isnot [int] -or $Manifest.SlotId -isnot [int] -or
        $Manifest.State -cnotin @('Planned','Created','Baseline001','Installed','Bound','PrincipalsReady','CleanupStarted','Removed')) {
        throw 'Invalid manifest version/slot/state type.'
    }
    if ($null -ne $Manifest.Engine -and ($Manifest.Engine.ProductVersion -cnotmatch '^\d+\.\d+\.\d+\.\d+$' -or
        [string]::IsNullOrWhiteSpace($Manifest.Engine.ServerCollation) -or [string]::IsNullOrWhiteSpace($Manifest.Engine.OriginalLogin))) {
        throw 'Invalid recorded engine observation.'
    }
    if ($null -ne $Manifest.DatabaseIdentity) {
        $id=$Manifest.DatabaseIdentity
        if ($id.DatabaseId -isnot [int] -or $id.DatabaseId -le 4 -or $id.CreationTime -cnotmatch '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?$' -or
            $id.OwnerSid -cnotmatch '^0x(?:[0-9A-F]{2})+$' -or $id.DatabaseGuid -cnotmatch '^[a-fA-F0-9-]{36}$' -or
            [Guid]$id.DatabaseGuid -eq [Guid]::Empty -or $id.Rcsi -isnot [bool] -or [string]::IsNullOrWhiteSpace($id.Collation)) {
            throw 'Invalid recorded created database identity.'
        }
    }
    $versions=@()
    foreach ($migration in @($Manifest.MigrationManifest)) {
        if ($migration.Version -isnot [int] -or $migration.Version -lt 1 -or $migration.Version -gt 13 -or
            $migration.Version -in $versions -or $migration.Name -cnotmatch '^\d{3}_[a-z0-9_]+\.sql$' -or
            [int]$migration.Name.Substring(0,3) -ne $migration.Version -or $migration.Checksum -cnotmatch '^[0-9A-F]{64}$') {
            throw 'Invalid recorded migration identity.'
        }
        $versions += $migration.Version
    }
    foreach ($sidKey in @('WindowsAccountSid','RuntimeLoginSid','RecoveryLoginSid','RuntimeUserSid','RecoveryUserSid')) {
        if ($null -ne $Manifest.$sidKey -and [string]$Manifest.$sidKey -cnotmatch '^0x[0-9A-F]+$|^S-1-[0-9-]+$') {
            throw 'Invalid recorded SID.'
        }
    }
    foreach ($hashKey in @('RuntimeCredentialHash','RecoveryCredentialHash','IdentityHash')) {
        if ($null -ne $Manifest.$hashKey -and [string]$Manifest.$hashKey -cnotmatch '^[0-9A-F]{64}$') { throw 'Invalid recorded hash.' }
    }
    $names = @()
    foreach ($step in @($Manifest.Steps)) {
        if ($step.Name -in $names -or $step.Status -cnotin @('Pending','Done','Failed')) { throw 'Invalid lifecycle step history.' }
        $names += $step.Name
    }
}

function Read-D1bManifest([string]$Database, [string]$ManifestPath) {
    Assert-D1bTarget $Database
    Assert-D1bPath $ManifestPath (Get-D1bContract).ManifestPath
    Assert-D1bNoReparse $ManifestPath
    $m = [IO.File]::ReadAllText($ManifestPath) | ConvertFrom-Json
    Assert-D1bManifest $m $Database $ManifestPath
    return $m
}

function Write-D1bManifest($Manifest) {
    Assert-D1bManifest $Manifest $Manifest.Database $Manifest.ManifestPath
    Assert-D1bNoReparse $Manifest.ManifestPath
    $Manifest.UpdatedUtc = [DateTime]::UtcNow.ToString('o')
    $temporary = $Manifest.ManifestPath + '.pending'
    if (Test-Path -LiteralPath $temporary) { throw 'Pending manifest remains; preserve it for coordinator inspection.' }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Manifest | ConvertTo-Json -Depth 20))
    $file = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $file.Write($bytes,0,$bytes.Length); $file.Flush($true) } finally { $file.Dispose() }
    if ([IO.File]::Exists($Manifest.ManifestPath)) { [IO.File]::Replace($temporary,$Manifest.ManifestPath,$null) }
    else { [IO.File]::Move($temporary,$Manifest.ManifestPath) }
}

function Lock-D1bManifest([string]$Database, [string]$ManifestPath) {
    Assert-D1bTarget $Database
    Assert-D1bPath $ManifestPath (Get-D1bContract).ManifestPath
    Assert-D1bNoReparse ($ManifestPath + '.lock')
    return [IO.File]::Open(($ManifestPath + '.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
}

function Assert-D1bExecutor([switch]$Administrator) {
    $c = Get-D1bContract
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    if ($env:COMPUTERNAME -cne $c.Machine -or $identity.User.Value -cne $c.ExecutorSid) {
        throw 'Run as the approved YYH_DESKTOP executor; no alternate identity is adopted.'
    }
    if ($Administrator -and -not ([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Open an approved elevated Windows PowerShell as bass1; this tool never launches UAC.'
    }
}

function Assert-D1bLocalAccountAbsent {
    try {
        $existing=Get-LocalUser -Name 'dh_d1b_recovery' -ErrorAction Stop
        if ($null -ne $existing) { throw 'D1b Windows account name occupied; no adoption.' }
    } catch {
        if ($_.FullyQualifiedErrorId -notlike 'UserNotFound*') { throw 'Cannot prove the exact Windows account name is absent.' }
    }
}

function Assert-D1bReadyForStep($Manifest) {
    if ($Manifest.State -in @('CleanupStarted','Removed') -or @($Manifest.Steps | Where-Object Status -ne 'Done').Count -gt 0) {
        throw 'An incomplete attempt or cleanup exists; preserve resources and request coordinator reconciliation.'
    }
}

function Start-D1bStep($Manifest, [string]$Name, $Plan) {
    Assert-D1bReadyForStep $Manifest
    if (@($Manifest.Steps | Where-Object Name -ceq $Name).Count) { throw 'This one-time step has already been attempted.' }
    $Manifest.Steps = @($Manifest.Steps) + [pscustomobject]@{
        Name=$Name; Status='Pending'; PlannedUtc=[DateTime]::UtcNow.ToString('o'); Plan=$Plan
        CompletedUtc=$null; Identity=$null; Failure=$null
    }
    Write-D1bManifest $Manifest
}

function Complete-D1bStep($Manifest, [string]$Name, $Identity) {
    $step = @($Manifest.Steps | Where-Object Name -ceq $Name)
    if ($step.Count -ne 1 -or $step[0].Status -cne 'Pending') { throw 'Invalid step transition.' }
    $step[0].Status='Done'; $step[0].Identity=$Identity; $step[0].CompletedUtc=[DateTime]::UtcNow.ToString('o')
    Write-D1bManifest $Manifest
}

function Get-D1bFailureCode([Exception]$Exception) {
    [pscustomobject]@{ErrorType=$Exception.GetType().FullName; HResult=$Exception.HResult
        SqlNumber=$Exception.Data['D1bSqlNumber']; Detail='Provider/native text suppressed; inspect the last planned and recorded identity.'}
}

function Fail-D1bStep($Manifest, [string]$Name, $FailureCode = $null) {
    $step = @($Manifest.Steps | Where-Object Name -ceq $Name)
    if ($step.Count -eq 1 -and $step[0].Status -ceq 'Pending') {
        $step[0].Status='Failed'; $step[0].Failure=$FailureCode
        Write-D1bManifest $Manifest
    }
}

function New-D1bParameter([System.Data.SqlDbType]$Type, $Value, [int]$Size = 0) {
    # No AddWithValue, coercion from arbitrary strings, or provider-inferred lengths.
    if ($Type.ToString() -cnotin @('Int','BigInt','TinyInt','Bit','UniqueIdentifier','NVarChar','VarBinary')) {
        throw 'Unsupported D1b SQL parameter type.'
    }
    if ($null -ne $Value -and $Value -isnot [DBNull]) {
        $valid = switch ($Type.ToString()) {
            'Int' { $Value -is [int] }
            'BigInt' { $Value -is [long] }
            'TinyInt' { $Value -is [byte] }
            'Bit' { $Value -is [bool] }
            'UniqueIdentifier' { $Value -is [Guid] }
            'NVarChar' { $Value -is [string] -and $Size -gt 0 -and $Value.Length -le $Size }
            'VarBinary' { $Value -is [byte[]] -and $Size -gt 0 -and $Value.Length -le $Size }
            default { $false }
        }
        if (-not $valid) { throw 'D1b SQL parameter type/length mismatch.' }
    }
    if ($Type -in @([Data.SqlDbType]::NVarChar,[Data.SqlDbType]::VarBinary) -and $Size -le 0) { throw 'SQL size is required.' }
    [pscustomobject]@{Type=$Type; Size=$Size; Value=$Value}
}

function New-D1bCommand([Data.SqlClient.SqlConnection]$Connection, [string]$Sql,
    [hashtable]$Parameters = @{}, [Data.SqlClient.SqlTransaction]$Transaction = $null) {
    $command = $Connection.CreateCommand()
    $command.CommandText=$Sql; $command.CommandTimeout=30; $command.Transaction=$Transaction
    foreach ($name in $Parameters.Keys) {
        if ($name -cnotmatch '^[A-Za-z][A-Za-z0-9]*$') { $command.Dispose(); throw 'Invalid SQL parameter name.' }
        $p=$Parameters[$name]
        if ($null -eq $p -or $null -eq $p.PSObject.Properties['Type']) { $command.Dispose(); throw 'Typed SQL parameters required.' }
        $checked = New-D1bParameter $p.Type $p.Value $p.Size
        $parameter=$command.Parameters.Add(('@'+$name),$checked.Type)
        if ($checked.Size) { $parameter.Size=$checked.Size }
        $parameter.Value=$(if ($null -eq $checked.Value) { [DBNull]::Value } else { $checked.Value })
    }
    return $command
}

function Invoke-D1bSql([Data.SqlClient.SqlConnection]$Connection, [string]$Sql,
    [hashtable]$Parameters = @{}, [Data.SqlClient.SqlTransaction]$Transaction = $null,
    [ValidateSet('Scalar','NonQuery','Rows')][string]$Result = 'Scalar') {
    $command=New-D1bCommand $Connection $Sql $Parameters $Transaction
    try {
        switch ($Result) {
            'Scalar' { return $command.ExecuteScalar() }
            'NonQuery' { return $command.ExecuteNonQuery() }
            'Rows' {
                $reader=$command.ExecuteReader()
                try { $table=[Data.DataTable]::new(); $table.Load($reader); return ,$table } finally { $reader.Dispose() }
            }
        }
    } catch {
        # Do not rethrow a provider error: CREATE LOGIN errors can contain generated SQL/passwords.
        $number=$(if ($_.Exception -is [Data.SqlClient.SqlException]) { $_.Exception.Number } else { 0 })
        $safeError=[InvalidOperationException]::new("D1b SQL command failed (provider number $number); raw SQL and provider text suppressed. Preserve manifest.")
        $safeError.Data['D1bSqlNumber']=$number
        throw $safeError
    } finally { $command.Dispose() }
}

function Open-D1bDatabase($Manifest, [string]$Database, [switch]$Master) {
    Assert-D1bManifest $Manifest $Database $Manifest.ManifestPath
    Assert-D1bExecutor
    $builder=[Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder.DataSource='lpc:'+$Manifest.Instance
    $builder.InitialCatalog=$(if ($Master) { 'master' } else { $Database })
    $builder.IntegratedSecurity=$true; $builder.Encrypt=$true; $builder.TrustServerCertificate=$true
    $builder.Pooling=$false; $builder.ConnectTimeout=5; $builder.ApplicationName='Dawnholder.D1b.Lifecycle'
    $connection=[Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        try { $connection.Open() } catch { throw 'D1b shared-memory connection failed; no fallback, provider text suppressed.' }
        $row=(Invoke-D1bSql $connection @'
SELECT CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')) Machine,
 CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) InstanceName,
 CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) ProductVersion,
 CONVERT(nvarchar(128),SERVERPROPERTY('Collation')) ServerCollation,
 IS_SRVROLEMEMBER('sysadmin') IsSysadmin, ORIGINAL_LOGIN() OriginalLogin;
'@ -Result Rows).Rows[0]
        if ($row.Machine -cne $Manifest.Machine -or $row.InstanceName -cne 'SQLEXPRESS' -or $row.IsSysadmin -ne 1) {
            throw 'Exact local instance and privileged SQL executor required.'
        }
        if ($null -ne $Manifest.Engine -and ($row.ProductVersion -cne $Manifest.Engine.ProductVersion -or
            $row.ServerCollation -cne $Manifest.Engine.ServerCollation)) { throw 'Engine changed; a new golden-vector decision is required.' }
        return $connection
    } catch { $connection.Dispose(); throw }
}

function Get-D1bDatabaseIdentity([Data.SqlClient.SqlConnection]$Master, $Manifest) {
    $table=Invoke-D1bSql $Master @'
SELECT d.database_id DatabaseId, CONVERT(nvarchar(33),d.create_date,126) CreationTime,
 CONVERT(varchar(170),d.owner_sid,1) OwnerSid, CONVERT(nvarchar(36),r.database_guid) DatabaseGuid,
 d.collation_name Collation, d.is_read_committed_snapshot_on Rcsi, d.state_desc State
FROM sys.databases d JOIN sys.database_recovery_status r ON r.database_id=d.database_id
WHERE d.name=@name;
'@ @{name=(New-D1bParameter NVarChar $Manifest.Database 128)} -Result Rows
    if ($table.Rows.Count -ne 1) { throw 'Exact database identity missing or ambiguous.' }
    $r=$table.Rows[0]
    [pscustomobject]@{DatabaseId=[int]$r.DatabaseId; CreationTime=[string]$r.CreationTime; OwnerSid=[string]$r.OwnerSid
        DatabaseGuid=[string]$r.DatabaseGuid; Collation=[string]$r.Collation; Rcsi=[bool]$r.Rcsi; State=[string]$r.State}
}

function Assert-D1bDatabaseIdentity([Data.SqlClient.SqlConnection]$Master, $Manifest) {
    if ($null -eq $Manifest.DatabaseIdentity) { throw 'No recorded created database identity; no adoption.' }
    $current=Get-D1bDatabaseIdentity $Master $Manifest
    foreach ($key in @('DatabaseId','CreationTime','OwnerSid','DatabaseGuid','Collation','Rcsi')) {
        if ([string]$current.$key -cne [string]$Manifest.DatabaseIdentity.$key) { throw "Database identity changed: $key." }
    }
    if ($current.State -cne 'ONLINE') { throw 'Database is not online; preserve it.' }
}

function Assert-D1bMarkers([Data.SqlClient.SqlConnection]$Connection, $Manifest) {
    $table=Invoke-D1bSql $Connection @'
SELECT name, CONVERT(nvarchar(128),value) Value FROM sys.extended_properties
WHERE class=0 AND name IN (N'Dawnholder.DatabaseTool',N'Dawnholder.D1bGoal');
'@ -Result Rows
    $markers=@{}
    foreach ($row in $table.Rows) { $markers[$row.name]=$row.Value }
    if ($markers['Dawnholder.DatabaseTool'] -cne 'development-v1' -or
        $markers['Dawnholder.D1bGoal'] -cne $Manifest.GoalMarker) { throw 'Owner/goal marker mismatch; no adoption or cleanup.' }
}

function Get-D1bHex([byte[]]$Bytes) { '0x'+[BitConverter]::ToString($Bytes).Replace('-','') }

function Get-D1bBytes([string]$Hex) {
    if ($Hex -cnotmatch '^0x(?:[0-9A-F]{2})+$') { throw 'Invalid SID hex.' }
    $bytes=[byte[]]::new(($Hex.Length-2)/2)
    for ($i=0; $i -lt $bytes.Length; $i++) { $bytes[$i]=[Convert]::ToByte($Hex.Substring(2+$i*2,2),16) }
    return ,$bytes
}

function Set-D1bDirectoryAcl([string]$Path, [string]$ExecutorSid, [string]$ReaderSid = '') {
    $c=Get-D1bContract
    if ($Path -cnotin @($c.PrivateDirectory,$c.IdentityDirectory) -or $ExecutorSid -cne $c.ExecutorSid -or
        ($Path -ceq $c.PrivateDirectory -and $ReaderSid)) { throw 'Directory ACL change is outside the exact lifecycle contract.' }
    $acl=[Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true,$false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($ExecutorSid))
    foreach ($sid in @($ExecutorSid,'S-1-5-18')) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($sid),'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
    }
    if ($ReaderSid) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($ReaderSid),'ReadAndExecute','ContainerInherit,ObjectInherit','None','Allow'))
    }
    # Directory.CreateDirectory with ACL is available in Windows PowerShell/.NET Framework.
    [void][IO.Directory]::CreateDirectory($Path,$acl)
    Assert-D1bDirectoryAcl $Path $ExecutorSid $ReaderSid
}

function New-D1bOwnedFile([string]$Path, [string]$ExecutorSid, [string]$ReaderSid = '') {
    $c=Get-D1bContract
    if ($Path -cnotin @($c.RuntimeCredentialPath,$c.RecoveryCredentialPath,$c.IdentityPath) -or
        $ExecutorSid -cne $c.ExecutorSid -or ($ReaderSid -and $Path -cne $c.IdentityPath)) {
        throw 'File security is outside the exact lifecycle contract.'
    }
    Assert-D1bNoReparse $Path
    $acl=[Security.AccessControl.FileSecurity]::new()
    $acl.SetAccessRuleProtection($true,$false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($ExecutorSid))
    foreach ($sid in @($ExecutorSid,'S-1-5-18')) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($sid),'FullControl','Allow'))
    }
    if ($ReaderSid) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($ReaderSid),'ReadAndExecute','Allow'))
    }
    # Set owner and ACL at CreateNew, including under an elevated token. Never open an existing file.
    return [IO.FileStream]::new($Path,[IO.FileMode]::CreateNew,[Security.AccessControl.FileSystemRights]::FullControl,
        [IO.FileShare]::None,4096,[IO.FileOptions]::WriteThrough,$acl)
}

function Assert-D1bDirectoryAcl([string]$Path, [string]$ExecutorSid, [string]$ReaderSid = '') {
    Assert-D1bNoReparse $Path
    $acl=Get-Acl -LiteralPath $Path
    $rules=@($acl.GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier]))
    $expected=@($ExecutorSid,'S-1-5-18')
    if ($ReaderSid) { $expected += $ReaderSid }
    if (-not $acl.AreAccessRulesProtected -or $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $ExecutorSid -or
        $rules.Count -ne $expected.Count) { throw 'Unexpected lifecycle directory ACL.' }
    foreach ($sid in $expected) {
        $rule=@($rules | Where-Object { $_.IdentityReference.Value -ceq $sid })
        $rightsName=$(if ($sid -ceq $ReaderSid) { 'ReadAndExecute' } else { 'FullControl' })
        # Allow rules add Synchronize on .NET Framework; compare the constructed effective mask.
        $rights=([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($sid),$rightsName,'ContainerInherit,ObjectInherit','None','Allow')).FileSystemRights
        if ($rule.Count -ne 1 -or $rule[0].AccessControlType -ne 'Allow' -or $rule[0].IsInherited -or
            $rule[0].FileSystemRights -ne $rights -or $rule[0].InheritanceFlags -ne 'ContainerInherit, ObjectInherit' -or
            $rule[0].PropagationFlags -ne 'None') { throw 'Unexpected lifecycle directory access rule.' }
    }
}

function Assert-D1bSecretFile([string]$Path, [string]$ExpectedHash, $Manifest) {
    if ($Path -cnotin @($Manifest.RuntimeCredentialPath,$Manifest.RecoveryCredentialPath)) { throw 'Unknown secret path.' }
    Assert-D1bDirectoryAcl $Manifest.PrivateDirectory $Manifest.ExecutorSid
    Assert-D1bNoReparse $Path
    $acl=Get-Acl -LiteralPath $Path
    $rules=@($acl.GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier]))
    if (-not $acl.AreAccessRulesProtected -or $rules.Count -ne 2 -or @($rules | Where-Object {
        $_.IdentityReference.Value -cnotin @($Manifest.ExecutorSid,'S-1-5-18') -or
        $_.AccessControlType -ne 'Allow' -or $_.FileSystemRights -ne 'FullControl'
    }).Count -or $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $Manifest.ExecutorSid) {
        throw 'Unexpected credential file ACL.'
    }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -cne $ExpectedHash) { throw 'Credential hash changed; preserve it.' }
}
