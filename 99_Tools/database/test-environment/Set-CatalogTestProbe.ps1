[CmdletBinding()]
param(
    [ValidateSet('Apply', 'Remove')][string]$Action,
    [string]$ApprovalPath,
    [string]$ExpectedApprovalHash
)

# This entry point owns only the separately approved catalog-test probe. Importing its definitions is inert.
# ExpectedApprovalHash binds the action document; possession of a draft is not human authorization to run it.
function Stop-CatalogProbe {
    param([string]$Code)
    $failure = [InvalidOperationException]::new('CatalogProbe:' + $Code)
    $failure.Data['CatalogProbeCode'] = $Code
    throw $failure
}

function Assert-CatalogProbeKeys {
    param($Value, [string[]]$Keys)
    if ($Value -isnot [pscustomobject]) {
        Stop-CatalogProbe -Code 'ObjectRequired'
    }
    $actual = @($Value.PSObject.Properties.Name | Sort-Object -CaseSensitive)
    $expected = @($Keys | Sort-Object -CaseSensitive)
    if (($actual -join '|') -cne ($expected -join '|')) {
        Stop-CatalogProbe -Code 'DocumentFields'
    }
}

function Get-CatalogProbeFullPath {
    param([string]$Path)
    if ([string]::IsNullOrEmpty($Path) -or $Path -cnotmatch '^[A-Za-z]:[\\/]' -or
        $Path.Substring(2).Contains(':')) {
        Stop-CatalogProbe -Code 'AbsoluteLocalPathRequired'
    }
    $full = [IO.Path]::GetFullPath($Path)
    if ($full -cne $Path.Replace('/', '\') -or $full.EndsWith('\')) {
        Stop-CatalogProbe -Code 'NoncanonicalPath'
    }
    return $full
}

function Assert-CatalogProbeNoReparse {
    param([string]$Path)
    $full = Get-CatalogProbeFullPath -Path $Path
    $itemPath = $full
    while (-not [string]::IsNullOrEmpty($itemPath)) {
        if (Test-Path -LiteralPath $itemPath) {
            $item = Get-Item -LiteralPath $itemPath -Force -ErrorAction Stop
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                Stop-CatalogProbe -Code 'ReparsePath'
            }
        }
        $itemPath = [IO.Path]::GetDirectoryName($itemPath)
    }
}

function Read-CatalogProbePublicJson {
    param([string]$Path, [string]$ExpectedHash)
    $full = Get-CatalogProbeFullPath -Path $Path
    Assert-CatalogProbeNoReparse -Path $full
    if ([IO.Path]::GetExtension($full) -cne '.json') { Stop-CatalogProbe -Code 'PublicJsonPathRequired' }
    if ($ExpectedHash -cnotmatch '^[0-9A-F]{64}$') {
        Stop-CatalogProbe -Code 'ApprovalHashRequired'
    }
    $file = [IO.FileStream]::new($full, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        if ($file.Length -lt 2 -or $file.Length -gt 32768) {
            Stop-CatalogProbe -Code 'PublicDocumentSize'
        }
        $bytes = [byte[]]::new([int]$file.Length)
        $read = 0
        while ($read -lt $bytes.Length) {
            $count = $file.Read($bytes, $read, $bytes.Length - $read)
            if ($count -eq 0) { Stop-CatalogProbe -Code 'PublicDocumentRead' }
            $read += $count
        }
        $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
        if ($hash -cne $ExpectedHash) { Stop-CatalogProbe -Code 'PublicDocumentHash' }
        $json = [Text.UTF8Encoding]::new($false, $true).GetString($bytes)
        # PS5.1 keeps JSON times as strings. Newer hosts must opt out of automatic DateTime conversion.
        if ((Get-Command -Name ConvertFrom-Json).Parameters.ContainsKey('DateKind')) {
            return ConvertFrom-Json -InputObject $json -DateKind String -ErrorAction Stop
        }
        return ConvertFrom-Json -InputObject $json -ErrorAction Stop
    }
    finally {
        $sha.Dispose()
        $file.Dispose()
    }
}

function ConvertFrom-CatalogProbeUtc {
    param($Value)
    if ($Value -isnot [string] -or $Value -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$') {
        Stop-CatalogProbe -Code 'UtcStringRequired'
    }
    $parsed = [DateTimeOffset]::MinValue
    $valid = [DateTimeOffset]::TryParseExact(
        $Value, 'yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$parsed)
    if (-not $valid -or $parsed.Offset -ne [TimeSpan]::Zero) {
        Stop-CatalogProbe -Code 'UtcStringRequired'
    }
    return $parsed
}

function Assert-CatalogProbeApproval {
    param($Plan, [string]$Action, [DateTimeOffset]$Now)
    $keys = @(
        'version', 'purpose', 'documentState', 'action', 'step', 'createdUtc', 'expiresUtc',
        'machine', 'executorSid', 'instance', 'adminEndpoint', 'sqlMajor', 'database', 'databaseGuid',
        'runtimeName', 'runtimeSid', 'schemaVersion', 'moduleRelease', 'moduleManifestChecksum',
        'probeName', 'plannedSid', 'credentialPath', 'evidenceDirectory', 'receiptPath', 'journalPath',
        'removeReceiptPath', 'removeJournalPath', 'serverPermissions', 'databasePermissions',
        'deleteCredential', 'originalReceiptHash', 'originalApprovalHash', 'credentialHash', 'sourceHashes'
    )
    Assert-CatalogProbeKeys -Value $Plan -Keys $keys
    foreach ($name in $keys) {
        if ($name -cnotin @('version', 'sqlMajor', 'schemaVersion', 'moduleRelease', 'deleteCredential',
                'createdUtc', 'expiresUtc',
                'serverPermissions', 'databasePermissions', 'sourceHashes') -and $Plan.$name -isnot [string]) {
            Stop-CatalogProbe -Code 'StringRequired'
        }
    }
    if ($Action -cnotin @('Apply', 'Remove') -or $Plan.action -cne $Action -or
        $Plan.version -isnot [int] -or $Plan.version -ne 1 -or
        $Plan.purpose -cne 'character-catalog-probe' -or
        $Plan.documentState -cne 'DraftRequiresExplicitActionHashApproval') {
        Stop-CatalogProbe -Code 'ActionScope'
    }
    $expectedStep = if ($Action -ceq 'Apply') { 'CreateCatalogProbe' } else { 'RemoveCatalogProbe' }
    if ($Plan.step -cne $expectedStep) { Stop-CatalogProbe -Code 'ActionStep' }
    $created = ConvertFrom-CatalogProbeUtc -Value $Plan.createdUtc
    $expires = ConvertFrom-CatalogProbeUtc -Value $Plan.expiresUtc
    if ($created -gt $Now -or $expires -le $Now -or $expires -le $created -or
        ($expires - $created) -gt [TimeSpan]::FromHours(24)) {
        Stop-CatalogProbe -Code 'ApprovalWindow'
    }
    # The exact target is hash-bound by the public manifest; host and SQL gates compare actual identities.
    $instanceParts = $Plan.instance.Split('\')
    $databaseName = [regex]::Match($Plan.database, '^Dawnholder_Char_([0-9a-f]{12})$')
    $databaseGuid = [Guid]::Empty
    if ($Plan.machine -cnotmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,62}$' -or
        $instanceParts.Count -ne 2 -or $instanceParts[0] -cne $Plan.machine -or
        $instanceParts[1] -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,15}$' -or
        $Plan.adminEndpoint -cne ('lpc:.\' + $instanceParts[1]) -or
        $Plan.sqlMajor -isnot [int] -or $Plan.sqlMajor -lt 1 -or $Plan.sqlMajor -gt 255 -or
        -not $databaseName.Success -or
        -not [Guid]::TryParseExact($Plan.databaseGuid, 'D', [ref]$databaseGuid) -or
        $databaseGuid -eq [Guid]::Empty -or $Plan.databaseGuid -cne $databaseGuid.ToString('D').ToUpperInvariant() -or
        $Plan.runtimeName -cne ('dh_char_' + $databaseName.Groups[1].Value) -or
        $Plan.probeName -cne ('dh_probe_' + $databaseName.Groups[1].Value) -or
        $Plan.runtimeSid -cnotmatch '^0x[0-9A-F]{32}$' -or $Plan.runtimeSid -ceq ('0x' + ('0' * 32)) -or
        $Plan.schemaVersion -isnot [int] -or $Plan.schemaVersion -ne 5 -or
        $Plan.moduleRelease -isnot [int] -or $Plan.moduleRelease -ne 5 -or
        $Plan.moduleManifestChecksum -cnotmatch '^[0-9A-F]{64}$' -or
        $Plan.executorSid -cnotmatch '^S-1-5-21-\d+-\d+-\d+-\d+$' -or
        $Plan.plannedSid -cnotmatch '^0x[0-9A-F]{32}$' -or
        $Plan.plannedSid -ceq $Plan.runtimeSid -or $Plan.plannedSid -ceq ('0x' + ('0' * 32))) {
        Stop-CatalogProbe -Code 'TargetScope'
    }
    try { $executor = [Security.Principal.SecurityIdentifier]::new($Plan.executorSid) }
    catch { Stop-CatalogProbe -Code 'TargetScope' }
    if ($executor.Value -cne $Plan.executorSid) { Stop-CatalogProbe -Code 'TargetScope' }
    if (($Plan.serverPermissions -join '|') -cne 'CONNECT SQL|VIEW SERVER PERFORMANCE STATE' -or
        ($Plan.databasePermissions -join '|') -cne 'CONNECT|SELECT ON OBJECT::dh.Account') {
        Stop-CatalogProbe -Code 'PermissionScope'
    }
    foreach ($name in @('serverPermissions', 'databasePermissions', 'sourceHashes')) {
        if ($Plan.$name -isnot [array]) { Stop-CatalogProbe -Code 'ArrayRequired' }
    }
    foreach ($permission in @($Plan.serverPermissions) + @($Plan.databasePermissions)) {
        if ($permission -isnot [string]) { Stop-CatalogProbe -Code 'PermissionScope' }
    }
    $credentialPath = Get-CatalogProbeFullPath -Path $Plan.credentialPath
    $local = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $credentialRoot = Join-Path $local 'Dawnholder'
    $credentialFolder = [IO.Path]::GetDirectoryName($credentialPath)
    $lifetime = [IO.Path]::GetFileName($credentialFolder)
    # Keep the existing lifetime/name relation inside the current profile, without embedding an environment.
    if ($lifetime -cnotmatch '^[0-9a-f]{32}$' -or
        -not $lifetime.StartsWith($databaseName.Groups[1].Value, [StringComparison]::Ordinal) -or
        [IO.Path]::GetDirectoryName($credentialFolder) -cne $credentialRoot -or
        [IO.Path]::GetFileName($credentialPath) -cne 'catalog-probe.clixml') {
        Stop-CatalogProbe -Code 'CredentialScope'
    }
    $evidence = Get-CatalogProbeFullPath -Path $Plan.evidenceDirectory
    foreach ($entry in @{
            receiptPath = 'probe-apply-receipt.json'
            journalPath = 'probe-apply-journal.jsonl'
            removeReceiptPath = 'probe-remove-receipt.json'
            removeJournalPath = 'probe-remove-journal.jsonl'
        }.GetEnumerator()) {
        if ((Get-CatalogProbeFullPath -Path $Plan.($entry.Key)) -cne (Join-Path $evidence $entry.Value)) {
            Stop-CatalogProbe -Code 'EvidenceScope'
        }
    }
    if ($Plan.deleteCredential -isnot [bool] -or $Plan.deleteCredential -ne ($Action -ceq 'Remove')) {
        Stop-CatalogProbe -Code 'CredentialDeletionScope'
    }
    foreach ($name in @('originalReceiptHash', 'originalApprovalHash', 'credentialHash')) {
        if (($Action -ceq 'Apply' -and $Plan.$name -cne '') -or
            ($Action -ceq 'Remove' -and $Plan.$name -cnotmatch '^[0-9A-F]{64}$')) {
            Stop-CatalogProbe -Code 'CreationAttribution'
        }
    }
}

function Assert-CatalogProbeHost {
    param($Plan)
    if ($PSVersionTable.PSEdition -cne 'Desktop' -or $PSVersionTable.PSVersion.Major -ne 5 -or
        $PSVersionTable.PSVersion.Minor -ne 1 -or -not [Environment]::Is64BitProcess -or
        [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        Stop-CatalogProbe -Code 'WindowsPowerShell51Required'
    }
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        if ($identity.User.Value -cne $Plan.executorSid -or
            [Environment]::MachineName -cne $Plan.machine -or $identity.ImpersonationLevel -eq 'Impersonation') {
            Stop-CatalogProbe -Code 'ExecutorIdentity'
        }
    }
    finally { $identity.Dispose() }
    $local = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $credentialPath = Get-CatalogProbeFullPath -Path $Plan.credentialPath
    $lifetime = [IO.Path]::GetFileName([IO.Path]::GetDirectoryName($credentialPath))
    $expected = Join-Path $local ('Dawnholder\' + $lifetime + '\catalog-probe.clixml')
    if ($lifetime -cnotmatch '^[0-9a-f]{32}$' -or $credentialPath -cne $expected) {
        Stop-CatalogProbe -Code 'ExecutorProfile'
    }
}

function Assert-CatalogProbeSources {
    param($Plan, [string]$SourcePath)
    $root = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($SourcePath)) '..'))
    $paths = @('test-environment/Set-CatalogTestProbe.ps1', 'SqlError.Common.ps1',
        'migrations/005_character_catalog.sql', 'verify-schema.sql')
    if ($Plan.sourceHashes.Count -ne $paths.Count) { Stop-CatalogProbe -Code 'SourceSet' }
    for ($index = 0; $index -lt $paths.Count; $index++) {
        $entry = $Plan.sourceHashes[$index]
        Assert-CatalogProbeKeys -Value $entry -Keys @('path', 'sha256')
        if ($entry.path -cne $paths[$index] -or $entry.sha256 -cnotmatch '^[0-9A-F]{64}$') {
            Stop-CatalogProbe -Code 'SourceSet'
        }
        $path = Join-Path $root $entry.path
        Assert-CatalogProbeNoReparse -Path $path
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $entry.sha256) {
            Stop-CatalogProbe -Code 'SourceDrift'
        }
    }
    $migration = [IO.File]::ReadAllText((Join-Path $root 'migrations/005_character_catalog.sql'))
    $declaration = [regex]::Match($migration, "VALUES\(5, '([0-9A-F]{64})'\);")
    if (-not $declaration.Success -or $declaration.Groups[1].Value -cne $Plan.moduleManifestChecksum) {
        Stop-CatalogProbe -Code 'ModuleSourceIdentity'
    }
    return $root
}

function Assert-CatalogProbeAcl {
    param([string]$Path, [string]$ExecutorSid, [switch]$Directory)
    Assert-CatalogProbeNoReparse -Path $Path
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -ne [bool]$Directory) { Stop-CatalogProbe -Code 'FileKind' }
    $acl = Get-Acl -LiteralPath $Path -ErrorAction Stop
    $rules = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]))
    $expected = if ($Directory) { @($ExecutorSid, 'S-1-5-18') } else { @($ExecutorSid) }
    if (-not $acl.AreAccessRulesProtected -or
        $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $ExecutorSid -or
        $rules.Count -ne $expected.Count) {
        Stop-CatalogProbe -Code 'OwnerOrAcl'
    }
    foreach ($sid in $expected) {
        $rule = @($rules | Where-Object { $_.IdentityReference.Value -ceq $sid })
        $inheritance = if ($Directory) { 'ContainerInherit,ObjectInherit' } else { 'None' }
        $expectedRule = [Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl', $inheritance, 'None', 'Allow')
        if ($rule.Count -ne 1 -or $rule[0].IsInherited -or $rule[0].AccessControlType -ne 'Allow' -or
            $rule[0].FileSystemRights -ne $expectedRule.FileSystemRights -or
            $rule[0].InheritanceFlags -ne $expectedRule.InheritanceFlags -or $rule[0].PropagationFlags -ne 'None') {
            Stop-CatalogProbe -Code 'OwnerOrAcl'
        }
    }
}

function New-CatalogProbeOwnedFile {
    param([string]$Path, [string]$ExecutorSid)
    Assert-CatalogProbeAcl -Path ([IO.Path]::GetDirectoryName($Path)) -ExecutorSid $ExecutorSid -Directory
    Assert-CatalogProbeNoReparse -Path $Path
    if (Test-Path -LiteralPath $Path) { Stop-CatalogProbe -Code 'CredentialAlreadyExists' }
    $acl = [Security.AccessControl.FileSecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($ExecutorSid))
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($ExecutorSid), 'FullControl', 'Allow'))
    # Owner and executor-only ACL are present at CreateNew, rather than corrected after writing the secret.
    return [IO.FileStream]::new(
        $Path, [IO.FileMode]::CreateNew, [Security.AccessControl.FileSystemRights]::FullControl,
        [IO.FileShare]::None, 4096, [IO.FileOptions]::WriteThrough, $acl)
}

function Get-CatalogProbeCredentialHash {
    param($Plan)
    Assert-CatalogProbeAcl -Path ([IO.Path]::GetDirectoryName($Plan.credentialPath)) `
        -ExecutorSid $Plan.executorSid -Directory
    Assert-CatalogProbeAcl -Path $Plan.credentialPath -ExecutorSid $Plan.executorSid
    # Hash encrypted bytes only. There is no Import-Clixml or success-report decryption.
    return (Get-FileHash -LiteralPath $Plan.credentialPath -Algorithm SHA256).Hash
}

function Write-CatalogProbeJsonLine {
    param($Stream, $Value)
    $line = (ConvertTo-Json -InputObject $Value -Depth 10 -Compress) + "`n"
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($line)
    $Stream.Write($bytes, 0, $bytes.Length)
    $Stream.Flush($true)
}

function New-CatalogProbePublicFile {
    param([string]$Path)
    Assert-CatalogProbeNoReparse -Path $Path
    if (-not [IO.Directory]::Exists([IO.Path]::GetDirectoryName($Path))) {
        Stop-CatalogProbe -Code 'EvidenceParentMissing'
    }
    return [IO.FileStream]::new(
        $Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read,
        4096, [IO.FileOptions]::WriteThrough)
}

function Write-CatalogProbeJournal {
    param(
        $Stream,
        $Plan,
        [string]$ApprovalHash,
        [string]$Stage,
        [string]$CredentialHash = '',
        [ValidateSet('NotStarted', 'Unknown', 'Committed')][string]$SqlOutcome = 'NotStarted',
        $Failure = $null
    )
    $entry = [ordered]@{
        version = 1
        action = $Plan.action
        approvalHash = $ApprovalHash
        databaseGuid = $Plan.databaseGuid
        runtimeSid = $Plan.runtimeSid
        plannedSid = $Plan.plannedSid
        stage = $Stage
        utc = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        credentialHash = $CredentialHash
        sqlOutcome = $SqlOutcome
        failure = $Failure
    }
    Write-CatalogProbeJsonLine -Stream $Stream -Value $entry
}

function ConvertFrom-CatalogProbeSidHex {
    param([string]$Value)
    if ($Value -cnotmatch '^0x[0-9A-F]{32}$') { Stop-CatalogProbe -Code 'SidFormat' }
    $bytes = [byte[]]::new(16)
    for ($index = 0; $index -lt 16; $index++) {
        $bytes[$index] = [Convert]::ToByte($Value.Substring(2 + $index * 2, 2), 16)
    }
    return , $bytes
}

function Get-CatalogProbeParameters {
    param($Plan)
    $executor = [Security.Principal.SecurityIdentifier]::new($Plan.executorSid)
    $executorBytes = [byte[]]::new($executor.BinaryLength)
    $executor.GetBinaryForm($executorBytes, 0)
    return @{
        database = @{
            Type = 'NVarChar'
            Size = 128
            Value = $Plan.database
        }
        databaseGuid = @{
            Type = 'UniqueIdentifier'
            Size = 0
            Value = [Guid]$Plan.databaseGuid
        }
        runtime = @{
            Type = 'NVarChar'
            Size = 128
            Value = $Plan.runtimeName
        }
        runtimeSid = @{
            Type = 'VarBinary'
            Size = 16
            Value = (ConvertFrom-CatalogProbeSidHex -Value $Plan.runtimeSid)
        }
        probe = @{
            Type = 'NVarChar'
            Size = 128
            Value = $Plan.probeName
        }
        sid = @{
            Type = 'VarBinary'
            Size = 16
            Value = (ConvertFrom-CatalogProbeSidHex -Value $Plan.plannedSid)
        }
        executorSid = @{
            Type = 'VarBinary'
            Size = 85
            Value = $executorBytes
        }
        instance = @{
            Type = 'NVarChar'
            Size = 128
            Value = $Plan.instance
        }
        major = @{
            Type = 'Int'
            Size = 0
            Value = [int]$Plan.sqlMajor
        }
        moduleHash = @{
            Type = 'Char'
            Size = 64
            Value = $Plan.moduleManifestChecksum
        }
        action = @{
            Type = 'NVarChar'
            Size = 6
            Value = $Plan.action
        }
        resource = @{
            Type = 'NVarChar'
            Size = 255
            Value = ('CatalogProbe:' + $Plan.databaseGuid)
        }
        postGate = @{
            Type = 'NVarChar'
            Size = -1
            Value = (Get-CatalogProbeGateSql -Plan $Plan)
        }
    }
}

function Get-CatalogProbeGateSql {
    param($Plan)
    # Identifiers cannot be SQL parameters. Only the validated manifest database name enters this quoted token.
    $databaseIdentifier = '[' + $Plan.database.Replace(']', ']]') + ']'
    $sql = @'
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 2000;
IF DB_NAME() <> N'master'
    THROW 51400, 'Probe gate.', 1;
IF ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')), N'') COLLATE Latin1_General_100_BIN2 <> @instance
    OR ISNULL(TRY_CONVERT(int, SERVERPROPERTY('ProductMajorVersion')), -1) <> @major
    OR ISNULL(CONVERT(nvarchar(40), CONNECTIONPROPERTY('net_transport')), N'') <> N'Shared memory'
    OR ISNULL(IS_SRVROLEMEMBER(N'sysadmin'), 0) <> 1
    OR NOT EXISTS (SELECT 1 FROM sys.dm_exec_connections
        WHERE session_id = @@SPID AND auth_scheme IN (N'NTLM', N'KERBEROS'))
    OR ISNULL(SUSER_SID(ORIGINAL_LOGIN()), 0x) <> @executorSid
    THROW 51401, 'Probe gate.', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.databases d JOIN sys.database_recovery_status r ON r.database_id = d.database_id
    WHERE d.name = @database AND r.database_guid = @databaseGuid
        AND d.state_desc = N'ONLINE' AND d.user_access_desc = N'MULTI_USER' AND d.is_read_only = 0
        AND d.owner_sid <> @sid
) OR NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = @runtime AND sid = @runtimeSid)
    THROW 51402, 'Probe gate.', 1;
IF EXISTS (SELECT 1 FROM sys.databases WHERE state <> 0 OR ISNULL(HAS_DBACCESS(name), 0) <> 1)
    THROW 51403, 'Probe gate.', 1;
IF EXISTS (SELECT 1 FROM sys.databases WHERE owner_sid = @sid)
    THROW 51404, 'Probe gate.', 1;
IF @action = N'Apply'
BEGIN
    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @probe OR sid = @sid)
        THROW 51405, 'Probe gate.', 1;
END
ELSE
BEGIN
    IF (SELECT COUNT(*) FROM sys.server_principals WHERE name = @probe OR sid = @sid) <> 1
        OR NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = @probe AND sid = @sid
            AND is_disabled = 0 AND default_database_name = @database
            AND is_policy_checked = 1 AND is_expiration_checked = 0 AND credential_id IS NULL)
        THROW 51406, 'Probe gate.', 1;
    IF EXISTS (SELECT 1 FROM sys.dm_exec_sessions
        WHERE security_id = @sid OR original_security_id = @sid OR original_login_name = @probe)
        THROW 51407, 'Probe gate.', 1;
    IF EXISTS (SELECT 1 FROM sys.server_role_members
        WHERE member_principal_id = SUSER_ID(@probe) OR role_principal_id = SUSER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.server_principals WHERE owning_principal_id = SUSER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.endpoints WHERE principal_id = SUSER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.server_principal_credentials WHERE principal_id = SUSER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.linked_logins WHERE local_principal_id = SUSER_ID(@probe))
        OR EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE owner_sid = @sid)
        OR EXISTS (SELECT 1 FROM sys.server_permissions WHERE grantor_principal_id = SUSER_ID(@probe))
        THROW 51408, 'Probe gate.', 1;
    IF (SELECT COUNT(*) FROM sys.server_permissions WHERE grantee_principal_id = SUSER_ID(@probe)) <> 2
        OR (SELECT COUNT(*) FROM sys.server_permissions
            WHERE grantee_principal_id = SUSER_ID(@probe) AND class = 100 AND major_id = 0 AND state = 'G'
                AND permission_name IN (N'CONNECT SQL', N'VIEW SERVER PERFORMANCE STATE')) <> 2
        THROW 51409, 'Probe gate.', 1;
END;
DECLARE @otherDatabase sysname;
DECLARE @referenceCount int;
DECLARE @referenceSql nvarchar(max);
DECLARE probe_databases CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.databases WHERE name <> @database;
OPEN probe_databases;
FETCH NEXT FROM probe_databases INTO @otherDatabase;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @referenceSql = N'SELECT @count = COUNT(*) FROM ' + QUOTENAME(@otherDatabase) +
        N'.sys.database_principals WHERE sid = @probeSid OR name = @probeName;';
    SET @referenceCount = NULL;
    EXEC sys.sp_executesql @stmt = @referenceSql,
        @params = N'@probeSid varbinary(16), @probeName nvarchar(128), @count int OUTPUT',
        @probeSid = @sid, @probeName = @probe, @count = @referenceCount OUTPUT;
    IF ISNULL(@referenceCount, -1) <> 0
        THROW 51410, 'Probe gate.', 1;
    FETCH NEXT FROM probe_databases INTO @otherDatabase;
END;
CLOSE probe_databases;
DEALLOCATE probe_databases;
USE __DATABASE__;
IF DB_NAME() <> @database
    OR (SELECT COUNT(*) FROM dh.SchemaVersion) <> 5
    OR (SELECT MAX(Version) FROM dh.SchemaVersion) <> 5
    OR NOT EXISTS (SELECT 1 FROM dh.SchemaVersion WHERE Version = 5 AND Name = N'005_character_catalog.sql')
    OR (SELECT COUNT(*) FROM dh.ModuleRelease) <> 2
    OR (SELECT MAX(Version) FROM dh.ModuleRelease) <> 5
    OR NOT EXISTS (SELECT 1 FROM dh.ModuleRelease r JOIN dh.SchemaVersion s ON s.Version = r.Version
        WHERE r.Version = 5 AND r.ManifestChecksum COLLATE Latin1_General_100_BIN2 = @moduleHash)
    OR OBJECT_ID(N'dh.Account', N'U') IS NULL
    OR NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @runtime AND sid = @runtimeSid
        AND type = 'S' AND authentication_type_desc = N'INSTANCE')
    THROW 51411, 'Probe gate.', 1;
IF @action = N'Apply'
BEGIN
    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @probe OR sid = @sid)
        THROW 51412, 'Probe gate.', 1;
END
ELSE
BEGIN
    IF (SELECT COUNT(*) FROM sys.database_principals WHERE name = @probe OR sid = @sid) <> 1
        OR NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @probe AND sid = @sid
            AND type = 'S' AND authentication_type_desc = N'INSTANCE')
        THROW 51413, 'Probe gate.', 1;
    IF EXISTS (SELECT 1 FROM sys.database_role_members
        WHERE member_principal_id = USER_ID(@probe) OR role_principal_id = USER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.schemas WHERE principal_id = USER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.objects WHERE principal_id = USER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.database_principals WHERE owning_principal_id = USER_ID(@probe))
        OR EXISTS (SELECT 1 FROM sys.database_permissions WHERE grantor_principal_id = USER_ID(@probe))
        THROW 51414, 'Probe gate.', 1;
    IF (SELECT COUNT(*) FROM sys.database_permissions WHERE grantee_principal_id = USER_ID(@probe)) <> 2
        OR (SELECT COUNT(*) FROM sys.database_permissions WHERE grantee_principal_id = USER_ID(@probe)
            AND state = 'G' AND ((class = 0 AND major_id = 0 AND minor_id = 0 AND permission_name = N'CONNECT')
                OR (class = 1 AND major_id = OBJECT_ID(N'dh.Account') AND minor_id = 0
                    AND permission_name = N'SELECT'))) <> 2
        THROW 51415, 'Probe gate.', 1;
END;
USE master;
'@
    return $sql.Replace('__DATABASE__', $databaseIdentifier)
}

function Get-CatalogProbeMutationSql {
    param($Plan)
    $gate = Get-CatalogProbeGateSql -Plan $Plan
    $databaseIdentifier = '[' + $Plan.database.Replace(']', ']]') + ']'
    $change = if ($Plan.action -ceq 'Apply') {
        @'
DECLARE @ddl nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@probe) + N' WITH PASSWORD = ' +
    QUOTENAME(@password, '''') + N', SID = ' + CONVERT(varchar(34), @sid, 1) +
    N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = ' + QUOTENAME(@database) + N';';
EXEC sys.sp_executesql @stmt = @ddl;
SET @ddl = N'GRANT CONNECT SQL, VIEW SERVER PERFORMANCE STATE TO ' + QUOTENAME(@probe) + N';';
EXEC sys.sp_executesql @stmt = @ddl;
USE __DATABASE__;
SET @ddl = N'CREATE USER ' + QUOTENAME(@probe) + N' FOR LOGIN ' + QUOTENAME(@probe) +
    N'; GRANT CONNECT TO ' + QUOTENAME(@probe) + N'; GRANT SELECT ON OBJECT::dh.Account TO ' +
    QUOTENAME(@probe) + N';';
EXEC sys.sp_executesql @stmt = @ddl;
USE master;
EXEC sys.sp_executesql @stmt = @postGate,
    @params = N'@database nvarchar(128), @databaseGuid uniqueidentifier, @runtime nvarchar(128),
        @runtimeSid varbinary(16), @probe nvarchar(128), @sid varbinary(16), @executorSid varbinary(85),
        @instance nvarchar(128), @major int, @moduleHash char(64), @action nvarchar(6)',
    @database = @database, @databaseGuid = @databaseGuid, @runtime = @runtime, @runtimeSid = @runtimeSid,
    @probe = @probe, @sid = @sid, @executorSid = @executorSid, @instance = @instance, @major = @major,
    @moduleHash = @moduleHash, @action = N'Remove';
'@
    }
    else {
        @'
USE __DATABASE__;
DECLARE @ddl nvarchar(max) = N'DROP USER ' + QUOTENAME(@probe) + N';';
EXEC sys.sp_executesql @stmt = @ddl;
USE master;
SET @ddl = N'DROP LOGIN ' + QUOTENAME(@probe) + N';';
EXEC sys.sp_executesql @stmt = @ddl;
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @probe OR sid = @sid)
    THROW 51417, 'Probe gate.', 1;
USE __DATABASE__;
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @probe OR sid = @sid)
    THROW 51418, 'Probe gate.', 1;
USE master;
'@
    }
    $change = $change.Replace('__DATABASE__', $databaseIdentifier)
    # CREATE LOGIN participates in SQL Server transactions. Client/commit ambiguity is still Unknown to the caller.
    # https://learn.microsoft.com/en-us/sql/t-sql/statements/create-login-transact-sql
    return @"
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
$gate
$change
    COMMIT TRANSACTION;
    SELECT N'Committed';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"@
}

function Assert-CatalogProbeCreationReceipt {
    param($Receipt, $Plan)
    Assert-CatalogProbeKeys -Value $Receipt -Keys @(
        'version', 'action', 'status', 'approvalHash', 'credentialHash', 'plan', 'completedUtc', 'sqlOutcome')
    if ($Receipt.version -ne 1 -or $Receipt.action -cne 'Apply' -or $Receipt.status -cne 'Applied' -or
        $Receipt.sqlOutcome -cne 'Committed' -or $Receipt.approvalHash -cne $Plan.originalApprovalHash -or
        $Receipt.credentialHash -cne $Plan.credentialHash) {
        Stop-CatalogProbe -Code 'CreationReceipt'
    }
    # The creation window is checked at the recorded creation time, never extended to authorize Remove.
    $completed = ConvertFrom-CatalogProbeUtc -Value $Receipt.completedUtc
    if ($completed -gt [DateTimeOffset]::UtcNow) { Stop-CatalogProbe -Code 'CreationReceiptTime' }
    Assert-CatalogProbeApproval -Plan $Receipt.plan -Action Apply -Now $completed
    foreach ($name in @(
            'machine', 'executorSid', 'instance', 'adminEndpoint', 'sqlMajor', 'database', 'databaseGuid',
            'runtimeName', 'runtimeSid', 'schemaVersion', 'moduleRelease', 'moduleManifestChecksum',
            'probeName', 'plannedSid', 'credentialPath', 'evidenceDirectory', 'receiptPath', 'journalPath',
            'removeReceiptPath', 'removeJournalPath'
        )) {
        if ([string]$Receipt.plan.$name -cne [string]$Plan.$name) {
            Stop-CatalogProbe -Code 'CreationReceiptTarget'
        }
    }
    $originalSources = ConvertTo-Json -InputObject $Receipt.plan.sourceHashes -Depth 4 -Compress
    $currentSources = ConvertTo-Json -InputObject $Plan.sourceHashes -Depth 4 -Compress
    if ($originalSources -cne $currentSources) { Stop-CatalogProbe -Code 'CreationReceiptSources' }
}

function Get-CatalogProbeFailure {
    param([Exception]$Exception)
    $code = 'BoundaryFailure'
    $number = 0
    $current = $Exception
    while ($null -ne $current) {
        if ($current.Data.Contains('CatalogProbeCode')) { $code = [string]$current.Data['CatalogProbeCode'] }
        if ($current.Data.Contains('DatabaseSqlNumber')) { $number = [int]$current.Data['DatabaseSqlNumber'] }
        if ($current -is [Data.SqlClient.SqlException]) { $number = [int]$current.Number }
        $current = $current.InnerException
    }
    # Neither provider Message/stack/connection string nor exception object crosses this boundary.
    return [pscustomobject]@{ category = $code; sqlNumber = $number }
}

function Save-CatalogProbeCredential {
    param($Plan, [Management.Automation.PSCredential]$Credential)
    $file = New-CatalogProbeOwnedFile -Path $Plan.credentialPath -ExecutorSid $Plan.executorSid
    $serialized = $null
    $bytes = $null
    try {
        $serialized = [Management.Automation.PSSerializer]::Serialize($Credential)
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($serialized)
        $file.Write($bytes, 0, $bytes.Length)
        $file.Flush($true)
    }
    finally {
        $file.Dispose()
        if ($null -ne $bytes) { [Array]::Clear($bytes, 0, $bytes.Length) }
        $serialized = $null
    }
    return Get-CatalogProbeCredentialHash -Plan $Plan
}

function Invoke-CatalogProbe {
    param([string]$Action, [string]$ApprovalPath, [string]$ExpectedApprovalHash, [string]$SourcePath)
    Set-StrictMode -Version Latest
    $ErrorActionPreference = 'Stop'
    $VerbosePreference = 'SilentlyContinue'
    $DebugPreference = 'SilentlyContinue'
    $connection = $null
    $journal = $null
    $plan = $null
    $password = $null
    $secure = $null
    $random = $null
    $credentialHash = ''
    $sqlOutcome = 'NotStarted'
    $failure = $null
    $receipt = $null
    $parameters = $null
    try {
        $plan = Read-CatalogProbePublicJson -Path $ApprovalPath -ExpectedHash $ExpectedApprovalHash
        Assert-CatalogProbeApproval -Plan $plan -Action $Action -Now ([DateTimeOffset]::UtcNow)
        Assert-CatalogProbeHost -Plan $plan
        $root = Assert-CatalogProbeSources -Plan $plan -SourcePath $SourcePath
        # This reused file defines only numeric error handling; no expired lifecycle/plan is imported.
        . (Join-Path $root 'SqlError.Common.ps1')
        Assert-CatalogProbeNoReparse -Path $plan.evidenceDirectory
        Assert-CatalogProbeAcl -Path ([IO.Path]::GetDirectoryName($plan.credentialPath)) `
            -ExecutorSid $plan.executorSid -Directory
        if ($Action -ceq 'Apply') {
            foreach ($path in @($plan.credentialPath, $plan.receiptPath, $plan.journalPath,
                    $plan.removeReceiptPath, $plan.removeJournalPath)) {
                Assert-CatalogProbeNoReparse -Path $path
                if (Test-Path -LiteralPath $path) { Stop-CatalogProbe -Code 'ApplyAlreadyStarted' }
            }
            $journalPath = $plan.journalPath
            $receiptPath = $plan.receiptPath
        }
        else {
            $original = Read-CatalogProbePublicJson -Path $plan.receiptPath -ExpectedHash $plan.originalReceiptHash
            Assert-CatalogProbeCreationReceipt -Receipt $original -Plan $plan
            $credentialHash = Get-CatalogProbeCredentialHash -Plan $plan
            if ($credentialHash -cne $plan.credentialHash) { Stop-CatalogProbe -Code 'CredentialHash' }
            foreach ($path in @($plan.removeReceiptPath, $plan.removeJournalPath)) {
                Assert-CatalogProbeNoReparse -Path $path
                if (Test-Path -LiteralPath $path) { Stop-CatalogProbe -Code 'RemoveAlreadyStarted' }
            }
            $journalPath = $plan.removeJournalPath
            $receiptPath = $plan.removeReceiptPath
        }
        $connection = Open-CatalogProbeMaster -Plan $plan
        $parameters = Get-CatalogProbeParameters -Plan $plan
        $gate = Get-CatalogProbeGateSql -Plan $plan
        $observed = Invoke-CatalogProbeSql -Connection $connection -Sql ($gate + "`nSELECT N'Verified';") `
            -Parameters $parameters
        if ($observed -cne 'Verified') { Stop-CatalogProbe -Code 'TargetNotVerified' }
        $observed = Invoke-CatalogProbeSql -Connection $connection -Parameters $parameters -Sql @'
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
    @LockOwner = 'Session', @LockTimeout = 0, @DbPrincipal = 'public';
IF @result < 0 THROW 51416, 'Probe gate.', 1;
SELECT N'Locked';
'@
        if ($observed -cne 'Locked') { Stop-CatalogProbe -Code 'ProbeBusy' }
        # No credential or SQL mutation precedes this durable, create-only intent.
        $journal = New-CatalogProbePublicFile -Path $journalPath
        Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
            -Stage IntentFixed -CredentialHash $credentialHash
        if ($Action -ceq 'Apply') {
            $random = [byte[]]::new(32)
            $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
            try { $rng.GetBytes($random) }
            finally { $rng.Dispose() }
            $password = 'Aa9!' + [Convert]::ToBase64String($random)
            $secure = ConvertTo-SecureString -String $password -AsPlainText -Force
            $credential = [Management.Automation.PSCredential]::new($plan.probeName, $secure)
            $credentialHash = Save-CatalogProbeCredential -Plan $plan -Credential $credential
            Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
                -Stage CredentialSaved -CredentialHash $credentialHash
            $parameters.password = @{
                Type = 'NVarChar'
                Size = 128
                Value = $password
            }
        }
        else {
            # Verify again immediately before removing principals; never decrypt this file.
            if ((Get-CatalogProbeCredentialHash -Plan $plan) -cne $credentialHash) {
                Stop-CatalogProbe -Code 'CredentialHash'
            }
        }
        # The approval can expire during preflight. Recheck it and reviewed sources at the write boundary.
        Assert-CatalogProbeApproval -Plan $plan -Action $Action -Now ([DateTimeOffset]::UtcNow)
        $null = Assert-CatalogProbeSources -Plan $plan -SourcePath $SourcePath
        if ((Get-CatalogProbeCredentialHash -Plan $plan) -cne $credentialHash) {
            Stop-CatalogProbe -Code 'CredentialHash'
        }
        Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
            -Stage SqlAttempt -CredentialHash $credentialHash -SqlOutcome Unknown
        $sqlOutcome = 'Unknown'
        $observed = Invoke-CatalogProbeSql -Connection $connection -Sql (Get-CatalogProbeMutationSql -Plan $plan) `
            -Parameters $parameters
        if ($observed -cne 'Committed') { Stop-CatalogProbe -Code 'CommitNotObserved' }
        $sqlOutcome = 'Committed'
        Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
            -Stage SqlCommitted -CredentialHash $credentialHash -SqlOutcome $sqlOutcome
        if ($Action -ceq 'Remove') {
            if ((Get-CatalogProbeCredentialHash -Plan $plan) -cne $credentialHash) {
                Stop-CatalogProbe -Code 'CredentialHash'
            }
            # Exact single file only, after the confirmed SQL commit and final owner/ACL/hash/no-reparse checks.
            Remove-Item -LiteralPath $plan.credentialPath -ErrorAction Stop
            if (Test-Path -LiteralPath $plan.credentialPath) { Stop-CatalogProbe -Code 'CredentialRemovalUnknown' }
            Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
                -Stage CredentialRemoved -CredentialHash $credentialHash -SqlOutcome $sqlOutcome
        }
        $receipt = [ordered]@{
            version = 1
            action = $Action
            status = $(if ($Action -ceq 'Apply') { 'Applied' } else { 'Removed' })
            approvalHash = $ExpectedApprovalHash
            credentialHash = $credentialHash
            plan = $plan
            completedUtc = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
            sqlOutcome = $sqlOutcome
        }
        $receiptFile = New-CatalogProbePublicFile -Path $receiptPath
        try { Write-CatalogProbeJsonLine -Stream $receiptFile -Value $receipt }
        finally { $receiptFile.Dispose() }
        Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
            -Stage ReceiptPublished -CredentialHash $credentialHash -SqlOutcome $sqlOutcome
    }
    catch {
        $failure = Get-CatalogProbeFailure -Exception $_.Exception
        if ($null -ne $journal) {
            try {
                Write-CatalogProbeJournal -Stream $journal -Plan $plan -ApprovalHash $ExpectedApprovalHash `
                    -Stage FailedPreserved -CredentialHash $credentialHash -SqlOutcome $sqlOutcome -Failure $failure
            }
            catch { $failure = [pscustomobject]@{ category = 'JournalFailurePreserved'; sqlNumber = 0 } }
        }
    }
    finally {
        foreach ($resource in @($connection, $journal)) {
            if ($null -ne $resource) {
                try { $resource.Dispose() }
                catch { $failure = Get-CatalogProbeFailure -Exception $_.Exception }
            }
        }
        if ($null -ne $random) { [Array]::Clear($random, 0, $random.Length) }
        if ($null -ne $secure) { $secure.Dispose() }
        if ($null -ne $parameters) { $parameters.Clear() }
        $password = $null
        $credential = $null
    }
    return [pscustomobject]@{
        succeeded = ($null -eq $failure)
        action = $Action
        sqlOutcome = $sqlOutcome
        failure = $failure
    }
}

function Invoke-CatalogProbeSql {
    param($Connection, [string]$Sql, [hashtable]$Parameters)
    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        $command.CommandTimeout = 15
        foreach ($name in $Parameters.Keys) {
            $value = $Parameters[$name]
            $type = [Data.SqlDbType]([Enum]::Parse([Data.SqlDbType], $value.Type, $false))
            $parameter = $command.Parameters.Add('@' + $name, $type)
            if ($value.Size -ne 0) { $parameter.Size = $value.Size }
            $parameter.Value = $value.Value
        }
        return $command.ExecuteScalar()
    }
    catch { throw (New-DatabaseSqlFailure -Exception $_.Exception) }
    finally {
        # In particular, do not retain a password parameter on the disposed command.
        $command.Parameters.Clear()
        $command.Dispose()
    }
}

function Open-CatalogProbeMaster {
    param($Plan)
    $builder = [Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder['Data Source'] = $Plan.adminEndpoint
    $builder['Initial Catalog'] = 'master'
    $builder['Integrated Security'] = $true
    $builder['Network Library'] = 'dbmslpcn'
    $builder['Pooling'] = $false
    $builder['Connect Timeout'] = 5
    $builder['Application Name'] = 'Dawnholder catalog probe approval'
    $connection = [Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        $connection.Open()
        return $connection
    }
    catch {
        $connection.Dispose()
        throw (New-DatabaseSqlFailure -Exception $_.Exception)
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    try {
        $result = Invoke-CatalogProbe -Action $Action -ApprovalPath $ApprovalPath `
            -ExpectedApprovalHash $ExpectedApprovalHash -SourcePath $PSCommandPath
    }
    catch {
        $result = [pscustomobject]@{
            succeeded = $false
            action = $Action
            sqlOutcome = 'Unknown'
            failure = (Get-CatalogProbeFailure -Exception $_.Exception)
        }
    }
    ConvertTo-Json -InputObject $result -Depth 4 -Compress
    if (-not $result.succeeded) { exit 1 }
}
