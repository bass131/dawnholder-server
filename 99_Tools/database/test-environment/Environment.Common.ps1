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
    $plan | Add-Member -MemberType NoteProperty -Name ApprovalPlanPath -Value ([IO.Path]::GetFullPath($ApprovalPlanPath))
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
    foreach ($key in @('Goal', 'GoalMarker', 'G0', 'G1', 'Machine', 'InstanceName', 'RuntimeLogin', 'RecoveryLocalName')) {
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
        [string]::IsNullOrWhiteSpace($Manifest.Engine.ServerCollation) -or [string]::IsNullOrWhiteSpace($Manifest.Engine.OriginalLogin))) {
            throw 'Invalid recorded engine observation.'
        }
    if ($null -ne $Manifest.DatabaseIdentity) {
        $id = $Manifest.DatabaseIdentity
        if ($id.DatabaseId -isnot [int] -or $id.DatabaseId -le 4 -or $id.CreationTime -cnotmatch '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?$' -or
            $id.OwnerSid -cnotmatch '^0x(?:[0-9A-F]{2})+$' -or $id.DatabaseGuid -cnotmatch '^[a-fA-F0-9-]{36}$' -or
            [Guid]$id.DatabaseGuid -eq [Guid]::Empty -or $id.Rcsi -isnot [bool] -or [string]::IsNullOrWhiteSpace($id.Collation)) {
                throw 'Invalid recorded created database identity.'
            }
    }
    $versions = @()
    foreach ($migration in @($Manifest.MigrationManifest)) {
        if ($migration.Version -isnot [int] -or $migration.Version -lt 1 -or $migration.Version -gt 14 -or
            $migration.Version -in $versions -or $migration.Name -cnotmatch '^\d{3}_[a-z0-9_]+\.sql$' -or
            [int]$migration.Name.Substring(0, 3) -ne $migration.Version -or $migration.Checksum -cnotmatch '^[0-9A-F]{64}$') {
                throw 'Invalid recorded migration identity.'
            }
        $versions += $migration.Version
    }
    foreach ($sidKey in @('WindowsAccountSid', 'RuntimeLoginSid', 'RecoveryLoginSid', 'RuntimeUserSid', 'RecoveryUserSid')) {
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
        [IO.File]::Replace($temporary, $Manifest.ManifestPath, $null)
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
    return [IO.File]::Open(($ManifestPath + '.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
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
    try {
        $existing = Get-LocalUser -Name $Contract.RecoveryLocalName -ErrorAction Stop
        if ($null -ne $existing) {
            throw 'Test environment Windows account name occupied; no adoption.'
        }
    } catch {
        if ($_.FullyQualifiedErrorId -notlike 'UserNotFound*') {
            throw 'Cannot prove the exact Windows account name is absent.'
        }
    }
}

function Assert-TestEnvironmentReadyForStep(
    $Manifest
) {
    if ($Manifest.State -in @('CleanupStarted', 'Removed') -or @($Manifest.Steps | Where-Object Status -ne 'Done').Count -gt 0) {
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
            $checked = New-DatabaseSqlParameter -Type $inputParameter.Type -Value $inputParameter.Value -Size $inputParameter.Size
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
    $command = New-DatabaseSqlCommand -Connection $Connection -Sql $Sql -Parameters $Parameters -Transaction $Transaction
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
    $builder.DataSource = 'lpc:'+$Manifest.Instance
    $builder.InitialCatalog = $(if ($Master) {
        'master'
    } else {
        $Database
    })
    $builder.IntegratedSecurity = $true
    $builder.Encrypt = $true
    $builder.TrustServerCertificate = $true
    $builder.Pooling = $false
    $builder.ConnectTimeout = 5
    $builder.ApplicationName = 'Dawnholder.TestEnvironment'
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
        if ($row.Machine -cne $Manifest.Machine -or $row.InstanceName -cne $Contract.InstanceName -or $row.IsSysadmin -ne 1) {
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
    '0x'+[BitConverter]::ToString($Bytes).Replace('-', '')
}

function ConvertFrom-DatabaseSidHex(
    [string]$Hex
) {
    if ($Hex -cnotmatch '^0x(?:[0-9A-F]{2})+$') {
        throw 'Invalid SID hex.'
    }
    $bytes = [byte[]]::new(($Hex.Length-2)/2)
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
            [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
    }
    if ($ReaderSid) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($ReaderSid), 'ReadAndExecute', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
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
    if (-not $acl.AreAccessRulesProtected -or $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $ExecutorSid -or
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
            [Security.Principal.SecurityIdentifier]::new($sid), $rightsName, 'ContainerInherit,ObjectInherit', 'None', 'Allow')).FileSystemRights
        if ($rule.Count -ne 1 -or $rule[0].AccessControlType -ne 'Allow' -or $rule[0].IsInherited -or
            $rule[0].FileSystemRights -ne $rights -or $rule[0].InheritanceFlags -ne 'ContainerInherit, ObjectInherit' -or
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
