# Definitions only. Import performs no database, identity, account, ACL or credential I/O.
function Read-TestEnvironmentApprovalPlan {
    param(
        [string]$ApprovalPlanPath,
        [string]$ExpectedApprovalPlanHash
    )
    if ([string]::IsNullOrWhiteSpace($ApprovalPlanPath) -or -not [IO.Path]::IsPathRooted($ApprovalPlanPath) -or
        $ExpectedApprovalPlanHash -cnotmatch '^[0-9A-F]{64}$') {
        throw 'Supply the absolute nonsecret plan path and separately coordinator-reviewed SHA256.'
    }
    Assert-TestEnvironmentNoReparse -Path $ApprovalPlanPath
    # Hash the same byte array that is parsed: file replacement cannot switch the reviewed input.
    $bytes = [IO.File]::ReadAllBytes($ApprovalPlanPath)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $actualHash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
    if ($actualHash -cne $ExpectedApprovalPlanHash) {
        throw 'Approval plan changed after coordinator review.'
    }
    $plan = [Text.UTF8Encoding]::new($false, $true).GetString($bytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
    Assert-TestEnvironmentApprovalPlan -Plan $plan
    $plan | Add-Member `
        -MemberType NoteProperty `
        -Name ApprovalPlanPath `
        -Value ([IO.Path]::GetFullPath($ApprovalPlanPath))
    $plan | Add-Member -MemberType NoteProperty -Name ApprovalPlanHash -Value $actualHash
    return $plan
}

function Assert-TestEnvironmentApprovalPlan {
    param(
        $Plan
    )
    $required = @(
        'PlanVersion',
        'SchemaVersion',
        'ExecutionApproved',
        'Goal',
        'GoalMarker',
        'G0',
        'G1',
        'G2',
        'Machine',
        'Instance',
        'InstanceName',
        'Endpoint',
        'Database',
        'SlotId',
        'AccountId',
        'CharacterId',
        'RuntimeLogin',
        'RecoveryPrincipal',
        'RecoveryLocalName',
        'ExecutorSid',
        'Encrypt',
        'TrustServerCertificate',
        'ManifestPath',
        'SettlementPath',
        'PrivateDirectory',
        'IdentityDirectory',
        'IdentityPath',
        'RuntimeCredentialPath',
        'RecoveryCredentialPath'
    )
    foreach ($key in $required) {
        if ($null -eq $Plan.PSObject.Properties[$key]) {
            throw "Missing approval-plan field: $key."
        }
    }
    if ($Plan.PlanVersion -isnot [int] -or $Plan.PlanVersion -ne 1 -or
        $Plan.SchemaVersion -isnot [int] -or $Plan.SchemaVersion -ne 1 -or
        $Plan.SlotId -isnot [int] -or $Plan.SlotId -ne 1 -or $Plan.ExecutionApproved -isnot [bool] -or
        $Plan.Encrypt -isnot [bool] -or -not $Plan.Encrypt -or
        $Plan.TrustServerCertificate -isnot [bool] -or -not $Plan.TrustServerCertificate) {
        throw 'Unsupported approval-plan version, slot or fixture encryption contract.'
    }
    foreach ($key in @(
            'Goal',
            'GoalMarker',
            'G0',
            'G1',
            'Machine',
            'InstanceName',
            'RuntimeLogin',
            'RecoveryLocalName'
        )) {
        if ($Plan.$key -isnot [string] -or [string]::IsNullOrWhiteSpace($Plan.$key)) {
            throw "Missing explicit approval value: $key."
        }
    }
    if ($Plan.Database -isnot [string] -or $Plan.Database -cnotmatch '^Dawnholder_Dev_[A-Za-z0-9_]+$' -or
        $Plan.Database.Length -gt 128 -or $Plan.RuntimeLogin.Length -gt 128 -or
        $Plan.Instance -cne ('.\' + $Plan.InstanceName) -or $Plan.InstanceName -cnotmatch '^[A-Za-z0-9_]+$' -or
        $Plan.Endpoint -cnotmatch '^tcp:127\.0\.0\.1,[0-9]{1,5}$' -or
        [int]$Plan.Endpoint.Split(',')[1] -notin 1..65535 -or
        $Plan.RecoveryLocalName -cnotmatch '^[A-Za-z0-9_]{1,20}$' -or
        $Plan.RecoveryPrincipal -cne ($Plan.Machine + '\' + $Plan.RecoveryLocalName) -or
        $Plan.RecoveryPrincipal.Length -gt 128 -or $Plan.RuntimeLogin -ceq $Plan.RecoveryPrincipal -or
        $Plan.ExecutorSid -cnotmatch '^S-1-[0-9-]+$') {
        throw 'Invalid explicit local target/principal; no game DB, system DB or endpoint fallback.'
    }
    foreach ($key in @('AccountId', 'CharacterId')) {
        $guid = [Guid]::Empty
        if ($Plan.$key -isnot [string] -or -not [Guid]::TryParseExact($Plan.$key, 'D', [ref]$guid) -or
            $guid -eq [Guid]::Empty) {
            throw "Invalid fixed binding: $key."
        }
    }
    Assert-TestEnvironmentPlanPaths -Plan $Plan
    if ($Plan.ExecutionApproved -and ($Plan.G2 -isnot [string] -or [string]::IsNullOrWhiteSpace($Plan.G2) -or
            $Plan.G2 -cin @($Plan.G0, $Plan.G1))) {
        throw 'Execution needs the separately reviewed G2 record.'
    }
}

function Assert-TestEnvironmentPlanPaths {
    param($Plan)
    # Pure shape validation: private credentials and child identity must retain separate ACL owners/readers.
    $paths = @(
        'ManifestPath',
        'SettlementPath',
        'PrivateDirectory',
        'IdentityDirectory',
        'IdentityPath',
        'RuntimeCredentialPath',
        'RecoveryCredentialPath'
    )
    $seen = @()
    foreach ($key in $paths) {
        $value = $Plan.$key
        if ($value -isnot [string] -or [string]::IsNullOrWhiteSpace($value) -or
            -not [IO.Path]::IsPathRooted($value) -or $value.StartsWith('\\')) {
            throw "Approval paths must be explicit local absolute paths: $key."
        }
        $resolved = [IO.Path]::GetFullPath($value)
        if ($resolved -cne $value -or $resolved -ieq [IO.Path]::GetPathRoot($resolved) -or $resolved -in $seen) {
            throw 'Approval paths must be distinct, normalized and below a directory root.'
        }
        $seen += $resolved
    }
    if ([IO.Path]::GetDirectoryName($Plan.RuntimeCredentialPath) -ine $Plan.PrivateDirectory -or
        [IO.Path]::GetDirectoryName($Plan.RecoveryCredentialPath) -ine $Plan.PrivateDirectory -or
        [IO.Path]::GetDirectoryName($Plan.IdentityPath) -ine $Plan.IdentityDirectory -or
        [IO.Path]::GetDirectoryName($Plan.SettlementPath) -ine [IO.Path]::GetDirectoryName($Plan.ManifestPath)) {
        throw 'Credential, child identity and settlement locations do not match their approved responsibilities.'
    }
    foreach ($directory in @($Plan.PrivateDirectory, $Plan.IdentityDirectory)) {
        $prefix = $directory + [IO.Path]::DirectorySeparatorChar
        foreach ($key in @('ManifestPath', 'SettlementPath')) {
            if ($Plan.$key.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Nonsecret lifecycle evidence must remain outside the credential and child directories.'
            }
        }
    }
    if ($Plan.PrivateDirectory.StartsWith($Plan.IdentityDirectory + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $Plan.IdentityDirectory.StartsWith($Plan.PrivateDirectory + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Private and child directories must have separate ACL boundaries.'
    }
}

function Assert-TestEnvironmentExecutionApproval {
    param(
        $Contract
    )
    # A reviewed hash pins the input, not the truth of user approval. Coordinator/executor must read that original.
    if ($Contract.ExecutionApproved -isnot [bool] -or -not $Contract.ExecutionApproved -or
        [string]::IsNullOrWhiteSpace($Contract.G2)) {
        throw 'Draft plan cannot execute. Coordinator must review actual G2 approval and deliver its exact plan hash.'
    }
}

function Assert-TestEnvironmentTarget(
    [string]$Database,
    [string]$Instance = '',
    $Contract
) {
    if ($null -eq $Contract -or [string]::IsNullOrWhiteSpace($Database) -or
        $Database -cne $Contract.Database -or $Database -cnotmatch '^Dawnholder_Dev_[A-Za-z0-9_]+$' -or
        ($Instance -and $Instance -cne $Contract.Instance)) {
        throw 'Supply the explicit exact approved database and local instance; no fallback.'
    }
}

function Assert-TestEnvironmentPath(
    [string]$Path,
    [string]$Expected
) {
    if ([string]::IsNullOrWhiteSpace($Path) -or -not [IO.Path]::IsPathRooted($Path) -or
        -not [string]::Equals([IO.Path]::GetFullPath($Path), $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Test environment path is not the exact approved absolute path.'
    }
}

function Assert-TestEnvironmentNoReparse(
    [string]$Path
) {
    # Inspect only ancestors of an exact authorized path; never enumerate sibling files.
    $itemPath = [IO.Path]::GetFullPath($Path)
    while ($itemPath) {
        if (Test-Path -LiteralPath $itemPath) {
            if ((Get-Item -LiteralPath $itemPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw 'Test environment paths must not traverse reparse points.'
            }
        }
        $itemPath = [IO.Path]::GetDirectoryName($itemPath)
    }
}

function New-TestEnvironmentManifest(
    [string]$Database,
    $Contract
) {
    Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
    $c = $Contract
    Assert-TestEnvironmentExecutionApproval -Contract $Contract
    [pscustomobject]@{
        SchemaVersion = 1
        Goal = $c.Goal
        GoalMarker = $c.GoalMarker
        ApprovalPlanPath = $c.ApprovalPlanPath
        ApprovalPlanHash = $c.ApprovalPlanHash
        InstanceName = $c.InstanceName
        RecoveryLocalName = $c.RecoveryLocalName
        SettlementPath = $c.SettlementPath
        G0 = $c.G0
        G1 = $c.G1
        G2 = $c.G2
        Machine = $c.Machine
        Instance = $c.Instance
        Endpoint = $c.Endpoint
        Database = $c.Database
        SlotId = 1
        AccountId = $c.AccountId
        CharacterId = $c.CharacterId
        RuntimeLogin = $c.RuntimeLogin
        RecoveryPrincipal = $c.RecoveryPrincipal
        ExecutorSid = $c.ExecutorSid
        Encrypt = $true
        TrustServerCertificate = $true
        ManifestPath = $c.ManifestPath
        PrivateDirectory = $c.PrivateDirectory
        IdentityDirectory = $c.IdentityDirectory
        IdentityPath = $c.IdentityPath
        RuntimeCredentialPath = $c.RuntimeCredentialPath
        RecoveryCredentialPath = $c.RecoveryCredentialPath
        CreatedUtc = [DateTime]::UtcNow.ToString('o')
        UpdatedUtc = [DateTime]::UtcNow.ToString('o')
        State = 'Planned'
        Engine = $null
        DatabaseIdentity = $null
        MigrationManifest = @()
        WindowsAccountSid = $null
        RuntimeLoginSid = $null
        RecoveryLoginSid = $null
        RuntimeUserSid = $null
        RecoveryUserSid = $null
        RuntimeCredentialHash = $null
        RecoveryCredentialHash = $null
        IdentityHash = $null
        Steps = @()
        Cleanup = $null
    }
}

function Assert-TestEnvironmentManifest(
    $Manifest,
    [string]$Database,
    [string]$ManifestPath,
    $Contract
) {
    Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
    $c = $Contract
    Assert-TestEnvironmentPath -Path $ManifestPath -Expected $c.ManifestPath
    foreach ($key in @(
            'SchemaVersion',
            'Goal',
            'GoalMarker',
            'G0',
            'G1',
            'G2',
            'Machine',
            'Instance',
            'InstanceName',
            'Endpoint',
            'Database',
            'SlotId',
            'AccountId',
            'CharacterId',
            'RuntimeLogin',
            'RecoveryPrincipal',
            'RecoveryLocalName',
            'ExecutorSid',
            'Encrypt',
            'TrustServerCertificate',
            'ManifestPath',
            'SettlementPath',
            'PrivateDirectory',
            'IdentityDirectory',
            'IdentityPath',
            'RuntimeCredentialPath',
            'RecoveryCredentialPath',
            'ApprovalPlanPath',
            'ApprovalPlanHash'
        )) {
        if ($null -eq $Manifest.PSObject.Properties[$key] -or [string]$Manifest.$key -cne [string]$Contract.$key) {
            throw "Lifecycle manifest differs from the independently supplied approval plan: $key."
        }
    }
    Assert-TestEnvironmentExecutionApproval -Contract $Contract
    if ($Manifest.Encrypt -isnot [bool] -or -not $Manifest.Encrypt -or
        $Manifest.TrustServerCertificate -isnot [bool] -or -not $Manifest.TrustServerCertificate) {
        throw 'Lifecycle encryption values must retain their boolean contract.'
    }
    foreach ($key in @(
            'State',
            'Engine',
            'DatabaseIdentity',
            'MigrationManifest',
            'WindowsAccountSid',
            'RuntimeLoginSid',
            'RecoveryLoginSid',
            'RuntimeUserSid',
            'RecoveryUserSid',
            'RuntimeCredentialHash',
            'RecoveryCredentialHash',
            'IdentityHash',
            'Steps',
            'Cleanup'
        )) {
        if ($null -eq $Manifest.PSObject.Properties[$key]) {
            throw "Missing lifecycle field: $key."
        }
    }
    if ($Manifest.SchemaVersion -isnot [int] -or $Manifest.SlotId -isnot [int] -or
        $Manifest.State -cnotin @(
            'Planned',
            'Created',
            'Baseline001',
            'Installed',
            'Bound',
            'PrincipalsReady',
            'CleanupStarted',
            'Removed'
        )) {
        throw 'Invalid manifest version/slot/state type.'
    }
    if ($null -ne $Manifest.Engine -and ($Manifest.Engine.ProductVersion -cnotmatch '^\d+\.\d+\.\d+\.\d+$' -or
            [string]::IsNullOrWhiteSpace($Manifest.Engine.ServerCollation) -or
            [string]::IsNullOrWhiteSpace($Manifest.Engine.OriginalLogin))) {
        throw 'Invalid recorded engine observation.'
    }
    if ($null -ne $Manifest.DatabaseIdentity) {
        $id = $Manifest.DatabaseIdentity
        if ($id.DatabaseId -isnot [int] -or
            $id.DatabaseId -le 4 -or $id.CreationTime -cnotmatch '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?$' -or
            $id.OwnerSid -cnotmatch '^0x(?:[0-9A-F]{2})+$' -or $id.DatabaseGuid -cnotmatch '^[a-fA-F0-9-]{36}$' -or
            [Guid]$id.DatabaseGuid -eq [Guid]::Empty -or
            $id.Rcsi -isnot [bool] -or [string]::IsNullOrWhiteSpace($id.Collation)) {
            throw 'Invalid recorded created database identity.'
        }
    }
    $migrationNames = @('001_initial.sql', '002_persistence_metadata.sql',
        '003_module_metadata.sql', '004_module_release.sql')
    $versions = @()
    foreach ($migration in @($Manifest.MigrationManifest)) {
        if ($migration.Version -isnot [int] -or $migration.Version -lt 1 -or $migration.Version -gt 4 -or
            $migration.Version -in $versions -or $migration.Name -cnotmatch '^\d{3}_[a-z0-9_]+\.sql$' -or
            [int]$migration.Name.Substring(0, 3) -ne $migration.Version -or
            $migration.Checksum -cnotmatch '^[0-9A-F]{64}$') {
            throw 'Invalid recorded migration identity.'
        }
        if ($migration.Version -ne $versions.Count + 1 -or
            $migration.Name -cne $migrationNames[$versions.Count]) {
            throw 'Recorded migrations must retain the reviewed ordered contiguous version/name contract.'
        }
        $versions += $migration.Version
    }
    if (($Manifest.State -ceq 'Baseline001' -and $versions.Count -ne 1) -or
        ($Manifest.State -cin @('Installed', 'Bound', 'PrincipalsReady') -and $versions.Count -ne 4)) {
        throw (
            'Lifecycle state does not match the recorded SQL migration boundary; lifecycle schema version remains one.'
        )
    }
    foreach ($sidKey in @(
            'WindowsAccountSid',
            'RuntimeLoginSid',
            'RecoveryLoginSid',
            'RuntimeUserSid',
            'RecoveryUserSid'
        )) {
        if ($null -ne $Manifest.$sidKey -and [string]$Manifest.$sidKey -cnotmatch '^0x[0-9A-F]+$|^S-1-[0-9-]+$') {
            throw 'Invalid recorded SID.'
        }
    }
    foreach ($hashKey in @('RuntimeCredentialHash', 'RecoveryCredentialHash', 'IdentityHash')) {
        if ($null -ne $Manifest.$hashKey -and [string]$Manifest.$hashKey -cnotmatch '^[0-9A-F]{64}$') {
            throw 'Invalid recorded hash.'
        }
    }
    $names = @()
    foreach ($step in @($Manifest.Steps)) {
        if ($step.Name -in $names -or $step.Status -cnotin @('Pending', 'Done', 'Failed')) {
            throw 'Invalid lifecycle step history.'
        }
        $names += $step.Name
    }
}

function Read-TestEnvironmentManifest(
    [string]$Database,
    [string]$ManifestPath,
    $Contract
) {
    Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
    Assert-TestEnvironmentPath -Path $ManifestPath -Expected ($Contract).ManifestPath
    Assert-TestEnvironmentNoReparse -Path $ManifestPath
    $m = [IO.File]::ReadAllText($ManifestPath) | ConvertFrom-Json
    Assert-TestEnvironmentManifest -Contract $Contract -Manifest $m -Database $Database -ManifestPath $ManifestPath
    return $m
}

function Write-TestEnvironmentManifest(
    $Manifest,
    $Contract
) {
    Assert-TestEnvironmentManifest `
        -Contract $Contract `
        -Manifest $Manifest `
        -Database $Manifest.Database `
        -ManifestPath $Manifest.ManifestPath
    Assert-TestEnvironmentNoReparse -Path $Manifest.ManifestPath
    $Manifest.UpdatedUtc = [DateTime]::UtcNow.ToString('o')
    $temporary = $Manifest.ManifestPath + '.pending'
    if (Test-Path -LiteralPath $temporary) {
        throw 'Pending manifest remains; preserve it for coordinator inspection.'
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Manifest | ConvertTo-Json -Depth 20))
    $file = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $file.Write($bytes, 0, $bytes.Length)
        $file.Flush($true)
    } finally {
        $file.Dispose()
    }
    if ([IO.File]::Exists($Manifest.ManifestPath)) {
        # Preserve a null backup path through the PowerShell string-argument binder.
        [IO.File]::Replace($temporary, $Manifest.ManifestPath, [NullString]::Value)
    }
    else {
        [IO.File]::Move($temporary, $Manifest.ManifestPath)
    }
}

function Lock-TestEnvironmentManifest(
    [string]$Database,
    [string]$ManifestPath,
    $Contract
) {
    # Draft plans and an unapproved identity must stop before even the journal lock is created.
    Assert-TestEnvironmentExecutor -Contract $Contract
    Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
    Assert-TestEnvironmentPath -Path $ManifestPath -Expected ($Contract).ManifestPath
    Assert-TestEnvironmentNoReparse -Path ($ManifestPath + '.lock')
    return [IO.File]::Open(
        ($ManifestPath + '.lock'),
        [IO.FileMode]::OpenOrCreate,
        [IO.FileAccess]::ReadWrite,
        [IO.FileShare]::None
    )
}

function Assert-TestEnvironmentExecutor(
    [switch]$Administrator,
    $Contract
) {
    $c = $Contract
    Assert-TestEnvironmentExecutionApproval -Contract $Contract
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        if ($env:COMPUTERNAME -cne $c.Machine -or $identity.User.Value -cne $c.ExecutorSid) {
            throw 'Run as the separately approved machine/SID; no alternate identity is adopted.'
        }
        if ($Administrator -and -not ([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole(
                [Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'Use an approved elevated Windows PowerShell with the same executor SID; no automatic UAC.'
        }
    } finally {
        $identity.Dispose()
    }
}

function Assert-TestEnvironmentLocalAccountAbsent(
    $Contract
) {
    $existing = $null
    try {
        $existing = Get-LocalUser -Name $Contract.RecoveryLocalName -ErrorAction Stop
    } catch {
        if ($_.FullyQualifiedErrorId -notlike 'UserNotFound*') {
            throw 'Cannot prove the exact Windows account name is absent.'
        }
    }
    if ($null -ne $existing) {
        throw 'Test environment Windows account name occupied; no adoption.'
    }
}

function Assert-TestEnvironmentReadyForStep(
    $Manifest
) {
    if ($Manifest.State -in @('CleanupStarted', 'Removed') -or
        @($Manifest.Steps | Where-Object Status -ne 'Done').Count -gt 0) {
        throw 'An incomplete attempt or cleanup exists; preserve resources and request coordinator reconciliation.'
    }
}

function Start-TestEnvironmentStep(
    $Manifest,
    [string]$Name,
    $Plan,
    $Contract
) {
    Assert-TestEnvironmentReadyForStep -Manifest $Manifest
    if (@($Manifest.Steps | Where-Object Name -ceq $Name).Count) {
        throw 'This one-time step has already been attempted.'
    }
    $Manifest.Steps = @($Manifest.Steps) + [pscustomobject]@{
        Name = $Name
        Status = 'Pending'
        PlannedUtc = [DateTime]::UtcNow.ToString('o')
        Plan = $Plan
        CompletedUtc = $null
        Identity = $null
        Failure = $null
    }
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $Manifest
}

function Complete-TestEnvironmentStep(
    $Manifest,
    [string]$Name,
    $Identity,
    $Contract
) {
    $step = @($Manifest.Steps | Where-Object Name -ceq $Name)
    if ($step.Count -ne 1 -or $step[0].Status -cne 'Pending') {
        throw 'Invalid step transition.'
    }
    $step[0].Status = 'Done'
    $step[0].Identity = $Identity
    $step[0].CompletedUtc = [DateTime]::UtcNow.ToString('o')
    Write-TestEnvironmentManifest -Contract $Contract -Manifest $Manifest
}

function Get-DatabaseFailureCode(
    [Exception]$Exception
) {
    [pscustomobject]@{
        ErrorType = $Exception.GetType().FullName
        HResult = $Exception.HResult
        SqlNumber = $Exception.Data['DatabaseSqlNumber']
        Detail = 'Provider/native text suppressed; inspect the last planned and recorded identity.'
    }
}

function Get-TestEnvironmentStopReason(
    [Exception]$Exception
) {
    # Exact product literals are returned from this list, never copied from an exception or its inner text.
    $safeReasons = @(
        'Manifest already exists; this one-time lifetime cannot be restarted.'
        'Database creation is one-time and create-only.'
        'A target lifecycle directory already exists; no adoption.'
        'An exact database/login name is occupied; no adoption or rotation.'
        'Test environment Windows account name occupied; no adoption.'
        'Cannot prove the exact Windows account name is absent.'
        'Pending manifest remains; preserve it for coordinator inspection.'
        'An incomplete attempt or cleanup exists; preserve resources and request coordinator reconciliation.'
        'This one-time step has already been attempted.'
        'Invalid step transition.'
        'Install 001 first, leave its fixture to the independent verifier, then install 002+ in the same database.'
        'Installer core requires the lifecycle-owned manifest lock and exact connection.'
        'Installer requires the recorded active test-environment installation step.'
        'Test environment shared-memory connection failed; no fallback, provider text suppressed.'
        'Exact local instance and privileged SQL executor required.'
        'Engine changed; a new golden-vector decision is required.'
        'Exact database identity missing or ambiguous.'
        'No recorded created database identity; no adoption.'
        'Database is not online; preserve it.'
        'Owner/goal marker mismatch; no adoption or cleanup.'
        'Cleanup settlement is incomplete; no deletion is authorized by it.'
        'Evidence must be durable, nonsecret files within this goal evidence directory.'
        'Preserved evidence hash mismatch.'
        'A task-owned connection is not settled.'
        'Invalid settled request.'
        'Terminal request lacks payload/outcome evidence.'
        'SQL login identity missing/unknown/changed; no deletion.'
        'Cannot inspect exact Windows account identity.'
        'Windows account identity missing/unknown/changed; no deletion.'
        'Unknown credential file exists; do not read/delete it.'
        'Child identity manifest changed.'
        'Unknown child identity manifest exists.'
        'Remaining target connections/requests/transactions/locks; stop and preserve resources.'
        'Recorded binding disappeared; preserve resources.'
        'Held/mismatched binding or temporary trigger remains; cleanup does not release/drop it.'
        'Archived ledger count changed.'
        'Terminal operation evidence is absent.'
        'Terminal operation evidence differs from settlement.'
        'Manifest changed after review.'
        'Cleanup was already attempted; explicit reconciliation is required.'
        'Windows SID changed; preserve account.'
        'Unexpected credential file ACL.'
        'Credential hash changed; preserve it.'
        # These checks can also be reached after the lifecycle entry point has entered its guarded try.
        'Draft plan cannot execute. Coordinator must review actual G2 approval and deliver its exact plan hash.'
        'Supply the explicit exact approved database and local instance; no fallback.'
        'Test environment path is not the exact approved absolute path.'
        'Test environment paths must not traverse reparse points.'
        'Lifecycle encryption values must retain their boolean contract.'
        'Invalid manifest version/slot/state type.'
        'Invalid recorded engine observation.'
        'Invalid recorded created database identity.'
        'Invalid recorded migration identity.'
        'Recorded migrations must retain the reviewed ordered contiguous version/name contract.'
        'Lifecycle state does not match the recorded SQL migration boundary; lifecycle schema version remains one.'
        'Invalid recorded SID.'
        'Invalid recorded hash.'
        'Invalid lifecycle step history.'
        'Run as the separately approved machine/SID; no alternate identity is adopted.'
        'Use an approved elevated Windows PowerShell with the same executor SID; no automatic UAC.'
        'Unsupported Test environment SQL parameter type.'
        'Test environment SQL parameter type/length mismatch.'
        'SQL size is required.'
        'Invalid SQL parameter name.'
        'Typed SQL parameters required.'
        # The installer keeps its own throw contract; only its fixed product reasons cross this reporting boundary.
        'SQL tool parameters need a supported explicit CLR value; no provider inference.'
        'Complete installation needs an explicit reviewed test-environment contract.'
        'Incomplete migration history after application.'
        'Module manifest must be UTF-8 without BOM, LF, and a final newline; do not normalize its identity.'
        'Module manifest version/encoding differs from the reviewed current release.'
        'Module SQL file set differs from the exact reviewed bundle; remove extras or restore missing files.'
        'Invalid module path/checksum/dependency list; use the reviewed exact paths.'
        'Permissions must be the final single bundle entry without an engine definition.'
        'Permissions source must contain only the nine reviewed individual EXECUTE grants.'
        'Incomplete module bundle; no module may be silently omitted.'
        'Migration files must be the reviewed contiguous version/name set; reject holes, duplicates and extras.'
        'Immutable 001 source checksum drift; restore the reviewed baseline.'
        'Database has unknown/newer migrations; use the matching tool revision without downgrading.'
        'Migration history has a hole, unknown name/version or checksum drift; do not rewrite applied history.'
        'Expected the complete database JSON row array, including [] for no rows.'
        'Existing module registration/object set is incomplete or unexpected; refuse adoption or overwrite.'
        'Unregistered/missing/unexpected module; restore or review it before deploying a new declaration.'
        'Module deployment requires the caller-owned migration transaction.'
        'Current bundle differs from its migration declaration; a different bundle needs a new release version.'
        'Module release history has an unknown, missing-schema or malformed declaration; refuse deployment.'
    )
    foreach ($reason in $safeReasons) {
        if ([string]::Equals($Exception.Message, $reason, [StringComparison]::Ordinal)) {
            return $reason
        }
    }
    # Interpolated throws retain their direct-call contract. Match complete, bounded templates and return constants:
    # keys, paths, object names and structure/provider details are never part of the reported reason.
    $manifestKey = '(?:SchemaVersion|Goal|GoalMarker|G0|G1|G2|Machine|Instance|InstanceName|Endpoint|' +
        'Database|SlotId|AccountId|CharacterId|RuntimeLogin|RecoveryPrincipal|RecoveryLocalName|ExecutorSid|' +
        'Encrypt|TrustServerCertificate|ManifestPath|SettlementPath|PrivateDirectory|IdentityDirectory|' +
        'IdentityPath|RuntimeCredentialPath|RecoveryCredentialPath|ApprovalPlanPath|ApprovalPlanHash)'
    $lifecycleKey = '(?:State|Engine|DatabaseIdentity|MigrationManifest|WindowsAccountSid|RuntimeLoginSid|' +
        'RecoveryLoginSid|RuntimeUserSid|RecoveryUserSid|RuntimeCredentialHash|RecoveryCredentialHash|' +
        'IdentityHash|Steps|Cleanup)'
    $identityKey = '(?:DatabaseId|CreationTime|OwnerSid|DatabaseGuid|Collation|Rcsi)'
    $modulePath = '(?:modules/(?:functions|procedures(?:/internal)?)/[a-z_]{1,80}\.sql|modules/permissions\.sql)'
    $moduleName = 'dh\.[A-Za-z][A-Za-z0-9_]{0,127}'
    switch -CaseSensitive -Regex ($Exception.Message) {
        ('\ALifecycle manifest differs from the independently supplied approval plan: ' + $manifestKey + '\.\z') {
            return 'Lifecycle manifest differs from the independently supplied approval plan; preserve it.'
        }
        ('\AMissing lifecycle field: ' + $lifecycleKey + '\.\z') {
            return 'A required lifecycle manifest field is missing; preserve it.'
        }
        ('\ADatabase identity changed: ' + $identityKey + '\.\z') {
            return 'Database identity changed; preserve resources and request coordinator reconciliation.'
        }
        '\AExpected an object at [^\r\n]{1,1024}\.\z' {
            return 'Module manifest requires an object at the reviewed bundle location.'
        }
        '\AUnexpected manifest fields at [^\r\n]{1,1024}; use the reviewed bundle format\.\z' {
            return 'Module manifest fields differ from the reviewed bundle format.'
        }
        '\AModule source structure (?:violation|unavailable): [^\r\n]{1,16384}\z' {
            return 'Module source structure is not compliant or could not be inspected; preserve resources.'
        }
        ('\AModule source checksum mismatch: ' + $modulePath +
            '; update the reviewed bundle and declaration together\.\z') {
            return 'Module source checksum differs from the reviewed bundle and declaration.'
        }
        ('\AModule files are one batch and cannot contain GO: ' + $modulePath + '\.\z') {
            return 'Module SQL must remain a single batch without GO.'
        }
        ('\AModule object/kind/expected definition mismatch: ' + $modulePath + '\.\z') {
            return 'Module object, kind or expected definition differs from the reviewed bundle.'
        }
        ('\AExpected one CREATE OR ALTER (?:FUNCTION|PROCEDURE) batch at ' + $modulePath + '\.\z') {
            return 'Module SQL requires one reviewed CREATE OR ALTER batch.'
        }
        ('\ADependency must precede ' + $moduleName + ': [^\r\n]{0,1024}\.\z') {
            return 'Module dependency order differs from the reviewed bundle.'
        }
        ('\ADependency contract mismatch: ' + $modulePath +
            '; declare the operation''s actual helper responsibilities\.\z') {
            return 'Module dependency declarations differ from their reviewed responsibilities.'
        }
        ('\ARegistered/actual module drift: ' + $moduleName +
            '; refuse overwrite, including unchanged source\.\z') {
            return 'Registered and actual module definitions differ; refuse overwrite.'
        }
        ('\AUnchanged source has different reviewed definition metadata: ' + $moduleName + '\.\z') {
            return 'Unchanged module source has different reviewed definition metadata.'
        }
        ('\AAlready-declared release has different source registration: ' + $moduleName + '; refuse repair\.\z') {
            return 'Already-declared module release has different source registration; refuse repair.'
        }
        ('\ATest environment SQL command failed \(provider number -?\d{1,10}\); ' +
            'raw SQL and provider text suppressed\. Preserve manifest\.\z') {
            return 'Test environment SQL command failed; raw SQL and provider text suppressed. Preserve manifest.'
        }
    }
    return 'Unclassified failure; provider/native text suppressed.'
}

function Get-TestEnvironmentFailureSummary(
    [Exception]$Exception
) {
    $reason = Get-TestEnvironmentStopReason -Exception $Exception
    $failureCode = Get-DatabaseFailureCode -Exception $Exception
    $safeSqlNumber = 'unavailable'
    if ($failureCode.SqlNumber -is [int]) {
        $safeSqlNumber = [string]$failureCode.SqlNumber
    }
    return ('{0} FailureCode={1}; HResult={2}; SqlNumber={3}.' -f
        $reason, $failureCode.ErrorType, $failureCode.HResult, $safeSqlNumber)
}

function Fail-TestEnvironmentStep(
    $Manifest,
    [string]$Name,
    $FailureCode = $null,
    $Contract
) {
    $step = @($Manifest.Steps | Where-Object Name -ceq $Name)
    if ($step.Count -eq 1 -and $step[0].Status -ceq 'Pending') {
        $step[0].Status = 'Failed'
        $step[0].Failure = $FailureCode
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $Manifest
    }
}

function New-DatabaseSqlParameter(
    [System.Data.SqlDbType]$Type,
    $Value,
    [int]$Size = 0
) {
    # No AddWithValue, coercion from arbitrary strings, or provider-inferred lengths.
    if ($Type.ToString() -cnotin @('Int', 'BigInt', 'TinyInt', 'Bit', 'UniqueIdentifier', 'NVarChar', 'VarBinary')) {
        throw 'Unsupported Test environment SQL parameter type.'
    }
    if ($null -ne $Value -and $Value -isnot [DBNull]) {
        $valid = switch ($Type.ToString()) {
            'Int' {
                $Value -is [int]
            }
            'BigInt' {
                $Value -is [long]
            }
            'TinyInt' {
                $Value -is [byte]
            }
            'Bit' {
                $Value -is [bool]
            }
            'UniqueIdentifier' {
                $Value -is [Guid]
            }
            'NVarChar' {
                $Value -is [string] -and $Size -gt 0 -and $Value.Length -le $Size
            }
            'VarBinary' {
                $Value -is [byte[]] -and $Size -gt 0 -and $Value.Length -le $Size
            }
            default {
                $false
            }
        }
        if (-not $valid) {
            throw 'Test environment SQL parameter type/length mismatch.'
        }
    }
    if ($Type -in @([Data.SqlDbType]::NVarChar, [Data.SqlDbType]::VarBinary) -and $Size -le 0) {
        throw 'SQL size is required.'
    }
    [pscustomobject]@{
        Type = $Type
        Size = $Size
        Value = $Value
    }
}

function New-DatabaseSqlCommand(
    [Data.SqlClient.SqlConnection]$Connection,
    [string]$Sql,
    [hashtable]$Parameters = @{},
    [Data.SqlClient.SqlTransaction]$Transaction = $null
) {
    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        $command.CommandTimeout = 30
        $command.Transaction = $Transaction
        foreach ($name in $Parameters.Keys) {
            if ($name -cnotmatch '^[A-Za-z][A-Za-z0-9]*$') {
                throw 'Invalid SQL parameter name.'
            }
            $inputParameter = $Parameters[$name]
            if ($null -eq $inputParameter -or $null -eq $inputParameter.PSObject.Properties['Type']) {
                throw 'Typed SQL parameters required.'
            }
            $checked = New-DatabaseSqlParameter `
                -Type $inputParameter.Type `
                -Value $inputParameter.Value `
                -Size $inputParameter.Size
            $parameter = $command.Parameters.Add(('@' + $name), $checked.Type)
            if ($checked.Size) {
                $parameter.Size = $checked.Size
            }
            $parameter.Value = $(if ($null -eq $checked.Value) {
                    [DBNull]::Value
                } else {
                    $checked.Value
                })
        }
        return $command
    } catch {
        # Validation may fail after allocating a command; callers must never inherit that resource.
        $command.Dispose()
        throw
    }
}

function Invoke-DatabaseSql(
    [Data.SqlClient.SqlConnection]$Connection,
    [string]$Sql,
    [hashtable]$Parameters = @{},
    [Data.SqlClient.SqlTransaction]$Transaction = $null,
    [ValidateSet('Scalar', 'NonQuery', 'Rows')][string]$Result = 'Scalar'
) {
    $command = New-DatabaseSqlCommand `
        -Connection $Connection `
        -Sql $Sql `
        -Parameters $Parameters `
        -Transaction $Transaction
    try {
        switch ($Result) {
            'Scalar' {
                return $command.ExecuteScalar()
            }
            'NonQuery' {
                return $command.ExecuteNonQuery()
            }
            'Rows' {
                $reader = $command.ExecuteReader()
                try {
                    $table = [Data.DataTable]::new()
                    $table.Load($reader)
                    return , $table
                } finally {
                    $reader.Dispose()
                }
            }
        }
    } catch {
        # Do not rethrow a provider error: CREATE LOGIN errors can contain generated SQL/passwords.
        $number = $(if ($_.Exception -is [Data.SqlClient.SqlException]) {
                $_.Exception.Number
            } else {
                0
            })
        $safeError = [InvalidOperationException]::new("Test environment SQL command failed (provider number $number); raw SQL and provider text suppressed. Preserve manifest.")
        $safeError.Data['DatabaseSqlNumber'] = $number
        throw $safeError
    } finally {
        $command.Dispose()
    }
}

function Open-TestEnvironmentDatabase(
    $Manifest,
    [string]$Database,
    [switch]$Master,
    $Contract
) {
    Assert-TestEnvironmentManifest `
        -Contract $Contract `
        -Manifest $Manifest `
        -Database $Database `
        -ManifestPath $Manifest.ManifestPath
    Assert-TestEnvironmentExecutor -Contract $Contract
    $builder = [Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder['Data Source'] = 'lpc:' + $Manifest.Instance
    $builder['Initial Catalog'] = $(if ($Master) {
            'master'
        } else {
            $Database
        })
    $builder['Integrated Security'] = $true
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = $true
    $builder['Pooling'] = $false
    $builder['Connect Timeout'] = 5
    $builder['Application Name'] = 'Dawnholder.TestEnvironment'
    $connection = [Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        try {
            $connection.Open()
        } catch {
            throw 'Test environment shared-memory connection failed; no fallback, provider text suppressed.'
        }
        $row = (Invoke-DatabaseSql `
                -Connection $connection `
                -Sql @'
SELECT CONVERT(nvarchar(128), SERVERPROPERTY('MachineName')) Machine,
    CONVERT(nvarchar(128), SERVERPROPERTY('InstanceName')) InstanceName,
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) ProductVersion,
    CONVERT(nvarchar(128), SERVERPROPERTY('Collation')) ServerCollation,
    IS_SRVROLEMEMBER('sysadmin') IsSysadmin, ORIGINAL_LOGIN() OriginalLogin;
'@ `
                -Result Rows).Rows[0]
        if ($row.Machine -cne $Manifest.Machine -or
            $row.InstanceName -cne $Contract.InstanceName -or $row.IsSysadmin -ne 1) {
            throw 'Exact local instance and privileged SQL executor required.'
        }
        if ($null -ne $Manifest.Engine -and ($row.ProductVersion -cne $Manifest.Engine.ProductVersion -or
                $row.ServerCollation -cne $Manifest.Engine.ServerCollation)) {
            throw 'Engine changed; a new golden-vector decision is required.'
        }
        return $connection
    } catch {
        $connection.Dispose()
        throw
    }
}

function Get-TestEnvironmentDatabaseIdentity(
    [Data.SqlClient.SqlConnection]$Master,
    $Manifest
) {
    $table = Invoke-DatabaseSql `
        -Connection $Master `
        -Sql @'
SELECT d.database_id DatabaseId,
    CONVERT(nvarchar(33), d.create_date, 126) CreationTime,
    CONVERT(varchar(170), d.owner_sid, 1) OwnerSid,
    CONVERT(nvarchar(36), r.database_guid) DatabaseGuid,
    d.collation_name Collation, d.is_read_committed_snapshot_on Rcsi, d.state_desc State
FROM sys.databases d JOIN sys.database_recovery_status r ON r.database_id = d.database_id
WHERE d.name = @name;
'@ `
        -Parameters @{
        name = (New-DatabaseSqlParameter -Type NVarChar -Value $Manifest.Database -Size 128)
    } `
        -Result Rows
    if ($table.Rows.Count -ne 1) {
        throw 'Exact database identity missing or ambiguous.'
    }
    $r = $table.Rows[0]
    [pscustomobject]@{
        DatabaseId = [int]$r.DatabaseId
        CreationTime = [string]$r.CreationTime
        OwnerSid = [string]$r.OwnerSid
        DatabaseGuid = [string]$r.DatabaseGuid
        Collation = [string]$r.Collation
        Rcsi = [bool]$r.Rcsi
        State = [string]$r.State
    }
}

function Assert-TestEnvironmentDatabaseIdentity(
    [Data.SqlClient.SqlConnection]$Master,
    $Manifest
) {
    if ($null -eq $Manifest.DatabaseIdentity) {
        throw 'No recorded created database identity; no adoption.'
    }
    $current = Get-TestEnvironmentDatabaseIdentity -Master $Master -Manifest $Manifest
    foreach ($key in @('DatabaseId', 'CreationTime', 'OwnerSid', 'DatabaseGuid', 'Collation', 'Rcsi')) {
        if ([string]$current.$key -cne [string]$Manifest.DatabaseIdentity.$key) {
            throw "Database identity changed: $key."
        }
    }
    if ($current.State -cne 'ONLINE') {
        throw 'Database is not online; preserve it.'
    }
}

function Assert-TestEnvironmentMarkers(
    [Data.SqlClient.SqlConnection]$Connection,
    $Manifest
) {
    $table = Invoke-DatabaseSql `
        -Connection $Connection `
        -Sql @'
SELECT name,
    CONVERT(nvarchar(128), value) Value FROM sys.extended_properties
WHERE class = 0 AND name IN (N'Dawnholder.DatabaseTool', N'Dawnholder.D1bGoal');
'@ `
        -Result Rows
    $markers = @{
    }
    foreach ($row in $table.Rows) {
        $markers[$row.name] = $row.Value
    }
    if ($markers['Dawnholder.DatabaseTool'] -cne 'development-v1' -or
        $markers['Dawnholder.D1bGoal'] -cne $Manifest.GoalMarker) {
        throw 'Owner/goal marker mismatch; no adoption or cleanup.'
    }
}

function ConvertTo-DatabaseSidHex(
    [byte[]]$Bytes
) {
    '0x' + [BitConverter]::ToString($Bytes).Replace('-', '')
}

function ConvertFrom-DatabaseSidHex(
    [string]$Hex
) {
    if ($Hex -cnotmatch '^0x(?:[0-9A-F]{2})+$') {
        throw 'Invalid SID hex.'
    }
    $bytes = [byte[]]::new(($Hex.Length - 2) / 2)
    for ($index = 0; $index -lt $bytes.Length; $index++) {
        $bytes[$index] = [Convert]::ToByte($Hex.Substring(2 + $index * 2, 2), 16)
    }
    return , $bytes
}

function Set-TestEnvironmentDirectoryAcl(
    [string]$Path,
    [string]$ExecutorSid,
    [string]$ReaderSid = '',
    $Contract
) {
    $c = $Contract
    if ($Path -cnotin @($c.PrivateDirectory, $c.IdentityDirectory) -or $ExecutorSid -cne $c.ExecutorSid -or
        ($Path -ceq $c.PrivateDirectory -and $ReaderSid)) {
        throw 'Directory ACL change is outside the exact lifecycle contract.'
    }
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($ExecutorSid))
    foreach ($sid in @($ExecutorSid, 'S-1-5-18')) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($sid),
                'FullControl',
                'ContainerInherit,ObjectInherit',
                'None',
                'Allow'))
    }
    if ($ReaderSid) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($ReaderSid),
                'ReadAndExecute',
                'ContainerInherit,ObjectInherit',
                'None',
                'Allow'))
    }
    # Directory.CreateDirectory with ACL is available in Windows PowerShell/.NET Framework.
    [void][IO.Directory]::CreateDirectory($Path, $acl)
    Assert-TestEnvironmentDirectoryAcl -Path $Path -ExecutorSid $ExecutorSid -ReaderSid $ReaderSid
}

function New-TestEnvironmentOwnedFile(
    [string]$Path,
    [string]$ExecutorSid,
    [string]$ReaderSid = '',
    $Contract
) {
    $c = $Contract
    if ($Path -cnotin @($c.RuntimeCredentialPath, $c.RecoveryCredentialPath, $c.IdentityPath) -or
        $ExecutorSid -cne $c.ExecutorSid -or ($ReaderSid -and $Path -cne $c.IdentityPath)) {
        throw 'File security is outside the exact lifecycle contract.'
    }
    Assert-TestEnvironmentNoReparse -Path $Path
    $acl = [Security.AccessControl.FileSecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($ExecutorSid))
    foreach ($sid in @($ExecutorSid, 'S-1-5-18')) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl', 'Allow'))
    }
    if ($ReaderSid) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($ReaderSid), 'ReadAndExecute', 'Allow'))
    }
    # Set owner and ACL at CreateNew, including under an elevated token. Never open an existing file.
    return [IO.FileStream]::new($Path, [IO.FileMode]::CreateNew, [Security.AccessControl.FileSystemRights]::FullControl,
        [IO.FileShare]::None, 4096, [IO.FileOptions]::WriteThrough, $acl)
}

function Assert-TestEnvironmentDirectoryAcl(
    [string]$Path,
    [string]$ExecutorSid,
    [string]$ReaderSid = ''
) {
    Assert-TestEnvironmentNoReparse -Path $Path
    $acl = Get-Acl -LiteralPath $Path
    $rules = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]))
    $expected = @($ExecutorSid, 'S-1-5-18')
    if ($ReaderSid) {
        $expected += $ReaderSid
    }
    if (-not $acl.AreAccessRulesProtected -or
        $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $ExecutorSid -or
        $rules.Count -ne $expected.Count) {
        throw 'Unexpected lifecycle directory ACL.'
    }
    foreach ($sid in $expected) {
        $rule = @($rules | Where-Object {
                $_.IdentityReference.Value -ceq $sid
            })
        $rightsName = $(if ($sid -ceq $ReaderSid) {
                'ReadAndExecute'
            } else {
                'FullControl'
            })
        # Allow rules add Synchronize on .NET Framework; compare the constructed effective mask.
        $rights = ([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($sid),
                $rightsName,
                'ContainerInherit,ObjectInherit',
                'None',
                'Allow')).FileSystemRights
        if ($rule.Count -ne 1 -or $rule[0].AccessControlType -ne 'Allow' -or $rule[0].IsInherited -or
            $rule[0].FileSystemRights -ne $rights -or
            $rule[0].InheritanceFlags -ne 'ContainerInherit, ObjectInherit' -or
            $rule[0].PropagationFlags -ne 'None') {
            throw 'Unexpected lifecycle directory access rule.'
        }
    }
}

function Assert-TestEnvironmentSecretFile(
    [string]$Path,
    [string]$ExpectedHash,
    $Manifest,
    $Contract
) {
    Assert-TestEnvironmentManifest `
        -Contract $Contract `
        -Manifest $Manifest `
        -Database $Manifest.Database `
        -ManifestPath $Manifest.ManifestPath
    if ($Path -cnotin @($Manifest.RuntimeCredentialPath, $Manifest.RecoveryCredentialPath)) {
        throw 'Unknown secret path.'
    }
    Assert-TestEnvironmentDirectoryAcl -Path $Manifest.PrivateDirectory -ExecutorSid $Manifest.ExecutorSid
    Assert-TestEnvironmentNoReparse -Path $Path
    $acl = Get-Acl -LiteralPath $Path
    $rules = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]))
    if (-not $acl.AreAccessRulesProtected -or $rules.Count -ne 2 -or @($rules | Where-Object {
                $_.IdentityReference.Value -cnotin @($Manifest.ExecutorSid, 'S-1-5-18') -or
                $_.AccessControlType -ne 'Allow' -or $_.FileSystemRights -ne 'FullControl'
            }).Count -or $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $Manifest.ExecutorSid) {
        throw 'Unexpected credential file ACL.'
    }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -cne $ExpectedHash) {
        throw 'Credential hash changed; preserve it.'
    }
}

# Nonsecret child projection: private paths, credential hashes and credential values never enter this object.
function New-TestEnvironmentChildIdentity {
    param($Manifest)
    return [pscustomobject]@{
        SchemaVersion = 1
        Goal = $Manifest.Goal
        GoalMarker = $Manifest.GoalMarker
        G0 = $Manifest.G0
        G1 = $Manifest.G1
        G2 = $Manifest.G2
        Machine = $Manifest.Machine
        Instance = $Manifest.Instance
        Endpoint = $Manifest.Endpoint
        Database = $Manifest.Database
        Encrypt = $true
        TrustServerCertificate = $true
        SlotId = 1
        AccountId = $Manifest.AccountId
        CharacterId = $Manifest.CharacterId
        DatabaseIdentity = $Manifest.DatabaseIdentity
        Engine = $Manifest.Engine
        MigrationManifest = $Manifest.MigrationManifest
        RuntimeLogin = $Manifest.RuntimeLogin
        RuntimeLoginSid = $Manifest.RuntimeLoginSid
        RuntimeUserSid = $Manifest.RuntimeUserSid
        RecoveryPrincipal = $Manifest.RecoveryPrincipal
        WindowsAccountSid = $Manifest.WindowsAccountSid
        RecoveryLoginSid = $Manifest.RecoveryLoginSid
        RecoveryUserSid = $Manifest.RecoveryUserSid
        ExecutorSid = $Manifest.ExecutorSid
        IdentityPath = $Manifest.IdentityPath
    }
}
