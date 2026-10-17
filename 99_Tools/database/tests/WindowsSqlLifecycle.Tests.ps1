[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Requirement tests for Invoke-WindowsSqlStep -Plan -Step -Adapter, written before the Windows SQL Express tool exists.
# Until windows-sql/WindowsSql.Common.ps1 provides it every requirement case below is expected to FAIL.
# Every side effect goes through one narrow adapter seam: $Adapter.InvokeOperation(<operation>, <request>).
# The recording fake below returns fixed observations, records each operation with its request, and can be forced
# to fail one operation. Expected traces and states are constants of the test contract; nothing recomputes the
# product decision. Covered steps: Preflight/Plan (reads only), Apply (approval, snapshot, exact identity),
# Verify (TLS and target catalog through the adapter) and Cleanup (owned resources only, configuration restore).
# A fake TLS success is a substitute result: the product must report the adapter kind instead of a live success.
# Nothing here opens SQL, Docker or WSL or reads services, the registry, certificate stores or the firewall.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-lifecycle'

# The private parent is %LOCALAPPDATA%\Dawnholder; it points into the suite so no real profile path is named.
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))

$productRelative = 'windows-sql/WindowsSql.Common.ps1'
$productPath = Join-Path $script:ToolRoot $productRelative
$entryRelative = 'windows-sql/Invoke-WindowsSqlEnvironment.ps1'
$entryPath = Join-Path $script:ToolRoot $entryRelative
$productLoadError = $null
if ([IO.File]::Exists($productPath)) {
    try {
        . $productPath
    }
    catch {
        $productLoadError = $_.Exception.Message
    }
}
else {
    $productLoadError = 'missing product file: ' + $productRelative
}
Assert-True -Name ('PRODUCT-02 {0} loads its definitions' -f $productRelative) `
    -Condition ($null -eq $productLoadError) -Detail ([string]$productLoadError)

Set-OfflineStubs

# ---- External command stubs. Each records its name and throws, so reaching one fails the case and is reported.
$script:ExternalCalls = New-Object 'Collections.Generic.List[string]'
$externalCommands = @(
    'Get-Service', 'Restart-Service', 'Start-Service', 'Stop-Service', 'Set-Service',
    'Get-ItemProperty', 'Set-ItemProperty', 'New-ItemProperty', 'Remove-ItemProperty',
    'Get-CimInstance', 'Invoke-CimMethod', 'Set-CimInstance', 'Get-WmiObject',
    'Get-NetFirewallRule', 'New-NetFirewallRule', 'Set-NetFirewallRule', 'Remove-NetFirewallRule',
    'Invoke-Sqlcmd', 'sqlcmd', 'sqlcmd.exe', 'netsh', 'netsh.exe', 'reg', 'reg.exe', 'sc.exe',
    'certutil', 'certutil.exe', 'New-SelfSignedCertificate', 'Import-PfxCertificate', 'Export-PfxCertificate',
    'Get-PfxCertificate', 'Get-NetIPAddress', 'Test-NetConnection', 'Invoke-Expression',
    'docker', 'wsl', 'wsl.exe', 'Start-Process', 'Get-NetTCPConnection'
)
foreach ($name in $externalCommands) {
    $stub = [scriptblock]::Create(
        "`$script:ExternalCalls.Add('$name'); throw 'Offline test stub: $name must not run.'")
    Set-Item -Path ('Function:script:' + $name) -Value $stub
}

$protectedProviderPattern = '^(?i)(HKLM:|HKCU:|HKEY_|Registry::|Microsoft\.PowerShell\.Core\\Registry::|Cert:)'
function Assert-NoProtectedProviderPath {
    param(
        [Parameter(Mandatory)][string]$Command,
        [object[]]$Arguments
    )
    # File system reads pass through; registry and certificate provider paths are external state.
    foreach ($argument in @($Arguments)) {
        if ([string]$argument -match $protectedProviderPattern) {
            $script:ExternalCalls.Add($Command + ' ' + [string]$argument)
            throw ('Offline test stub: {0} on a registry or certificate path must not run.' -f $Command)
        }
    }
}
function Get-Item {
    Assert-NoProtectedProviderPath -Command 'Get-Item' -Arguments $args
    Microsoft.PowerShell.Management\Get-Item @args
}
function Get-ChildItem {
    Assert-NoProtectedProviderPath -Command 'Get-ChildItem' -Arguments $args
    Microsoft.PowerShell.Management\Get-ChildItem @args
}
function Test-Path {
    Assert-NoProtectedProviderPath -Command 'Test-Path' -Arguments $args
    Microsoft.PowerShell.Management\Test-Path @args
}

# ---- Small helpers, kept in step with WindowsSqlPlan.Tests.ps1 (TestSupport.ps1 is outside this test's ownership).
function Get-FieldValue {
    param(
        $Object,
        [Parameter(Mandatory)][string]$Name
    )
    if ($null -eq $Object) { return $null }
    if ($Object -is [Collections.IDictionary]) {
        if ($Object.Contains($Name)) { return $Object[$Name] }
        return $null
    }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-FieldNames {
    param($Object)
    if ($null -eq $Object) { return , @() }
    if ($Object -is [Collections.IDictionary]) { return , @([string[]]@($Object.Keys)) }
    return , @($Object.PSObject.Properties | ForEach-Object { $_.Name })
}

function ConvertTo-CanonicalValue {
    param($Value, [int]$Depth = 0)
    # Key-sorted plain values so two objects compare by content, independent of property order or container type.
    if ($null -eq $Value) { return $null }
    if ($Value -is [string] -or $Value -is [ValueType]) { return $Value }
    # Deeper graphs are not plan, observation or state data; their text form is enough to compare and to scan.
    if ($Depth -ge 8) { return [string]$Value }
    if ($Value -is [Collections.IDictionary]) {
        $sorted = [ordered]@{}
        foreach ($key in @([string[]]@($Value.Keys) | Sort-Object)) {
            $sorted[$key] = ConvertTo-CanonicalValue -Value $Value[$key] -Depth ($Depth + 1)
        }
        return $sorted
    }
    if ($Value -is [Collections.IEnumerable]) {
        return , @($Value | ForEach-Object { ConvertTo-CanonicalValue -Value $_ -Depth ($Depth + 1) })
    }
    $sorted = [ordered]@{}
    foreach ($property in @($Value.PSObject.Properties | Sort-Object -Property Name)) {
        $sorted[$property.Name] = ConvertTo-CanonicalValue -Value $property.Value -Depth ($Depth + 1)
    }
    return $sorted
}

function ConvertTo-CanonicalJson {
    param($Value)
    return ConvertTo-Json -InputObject (ConvertTo-CanonicalValue -Value $Value) -Depth 12 -Compress
}

function ConvertTo-FixtureObject {
    param($Value)
    # A fresh object graph per call, so a product that edits a response cannot change the next read.
    if ($null -eq $Value) { return $null }
    if ($Value -is [Collections.IDictionary]) {
        $properties = [ordered]@{}
        foreach ($key in $Value.Keys) { $properties[$key] = ConvertTo-FixtureObject -Value $Value[$key] }
        return [pscustomobject]$properties
    }
    if ($Value -is [string] -or $Value -is [ValueType]) { return $Value }
    if ($Value -is [Collections.IEnumerable]) {
        return , @($Value | ForEach-Object { ConvertTo-FixtureObject -Value $_ })
    }
    return $Value
}

function Assert-EqualInCase {
    param(
        $Expected,
        $Actual,
        [Parameter(Mandatory)][string]$Name,
        [string]$Context = ''
    )
    # Assert-Equal plus the case's own error and trace, so a dependent failure shows what the step did.
    $passed = [string]$Expected -ceq [string]$Actual
    Add-TestResult -Name $Name -Outcome $(if ($passed) { 'PASS' } else { 'FAIL' }) `
        -Detail ('expected <{0}> actual <{1}> | {2}' -f $Expected, $Actual, $Context)
}

function Join-SortedNames {
    param([object[]]$Names)
    $list = [string[]]@($Names | Where-Object { $null -ne $_ } | ForEach-Object { [string]$_ })
    [Array]::Sort($list, [StringComparer]::Ordinal)
    return $list -join ','
}

function Invoke-Captured {
    param([Parameter(Mandatory)][scriptblock]$CapturedAction)
    # All streams are kept so a secret sentinel in Write-Host/Verbose/Warning output is also detected.
    $capturedRecords = New-Object 'Collections.Generic.List[object]'
    $capturedError = $null
    $capturedErrorType = $null
    try {
        & $CapturedAction *>&1 | ForEach-Object { $capturedRecords.Add($_) }
    }
    catch {
        $capturedError = $_.Exception.Message
        $capturedErrorType = $_.Exception.GetType().FullName
    }
    return [pscustomobject]@{
        Records = $capturedRecords.ToArray()
        Error = $capturedError
        ErrorType = $capturedErrorType
    }
}

function Get-ReturnedObject {
    param([object[]]$Records)
    # The product return value is the last pipeline object that is not a stream record or plain text.
    $streamTypes = @(
        [Management.Automation.InformationRecord], [Management.Automation.WarningRecord],
        [Management.Automation.VerboseRecord], [Management.Automation.DebugRecord],
        [Management.Automation.ErrorRecord]
    )
    $returned = @($Records | Where-Object {
            $record = $_
            $null -ne $record -and $record -isnot [string] -and
            @($streamTypes | Where-Object { $record -is $_ }).Count -eq 0
        })
    if ($returned.Count -eq 0) { return $null }
    return $returned[$returned.Count - 1]
}

function Get-CaptureText {
    param($Capture)
    $parts = New-Object 'Collections.Generic.List[string]'
    $parts.Add([string]$Capture.Error)
    foreach ($record in @($Capture.Records)) {
        if ($record -is [Management.Automation.InformationRecord]) {
            $parts.Add([string]$record.MessageData)
        }
        elseif ($record -is [string] -or $record -is [Management.Automation.ErrorRecord]) {
            $parts.Add([string]$record)
        }
        elseif ($record -is [Management.Automation.InformationalRecord]) {
            # Warning, verbose and debug records.
            $parts.Add([string]$record.Message)
        }
        else {
            $parts.Add((ConvertTo-CanonicalJson -Value $record))
        }
    }
    return $parts -join "`n"
}

# ---- Fixed fixture values (constants of this test contract, shared with WindowsSqlPlan.Tests.ps1).
$fixtureLifetime = '0123456789abcdef0123456789abcdef'
$otherLifetime = 'fedcba9876543210fedcba9876543210'
$fixtureMachine = 'LAPTOP-FIXTURE'
$fixtureInstance = 'SQLEXPRESS'
$fixtureInstanceId = 'MSSQL17.SQLEXPRESS'
$fixtureVersion = '17.0.1000.7'
$fixturePort = 14333
$fixtureDatabase = 'Dawnholder_Demo_Fixture'
$fixtureLogin = 'dh_demo_fixture'
$fixtureThumbprint = 'A1B2C3D4E5F60718293A4B5C6D7E8F9012345678'
$fixtureWslSubnet = '172.29.48.0/20'
$secretSentinel = 'WinSqlLifecycleSentinel-9c42e1'

$sourceRoot = Join-Path $script:SuiteRoot 'source'
$evidenceRoot = Join-Path $sourceRoot '.backups\windows-sql-evidence'
[void][IO.Directory]::CreateDirectory($evidenceRoot)
$privateRoot = Join-Path $env:LOCALAPPDATA 'Dawnholder\windows-sql-fixture'
$sourceInputs = foreach ($index in 1..3) {
    $relative = 'inputs/file{0:D2}.txt' -f $index
    $path = Join-Path $sourceRoot $relative
    [void][IO.Directory]::CreateDirectory((Split-Path -Path $path -Parent))
    [IO.File]::WriteAllText($path, "windows sql fixture $index")
    [ordered]@{ Path = $relative; Sha256 = Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($path)) }
}

function New-ValidPlanValues {
    return [ordered]@{
        Version = 1
        ExecutionApproved = $true
        ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(2).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
        LifetimeId = $fixtureLifetime
        MachineName = $fixtureMachine
        InstanceName = $fixtureInstance
        InstanceId = $fixtureInstanceId
        ExpectedProductVersion = $fixtureVersion
        Port = $fixturePort
        Database = $fixtureDatabase
        RuntimeLogin = $fixtureLogin
        SchemaVersion = 4
        CertificateThumbprint = $fixtureThumbprint
        Encrypt = 'Mandatory'
        TrustServerCertificate = $false
        EnableSa = $false
        SwitchWslNetworkingMode = $false
        ForceEncryption = 'Preserve'
        RestartApproved = $true
        FirewallApproved = $true
        SourceRoot = $sourceRoot
        EvidenceRoot = $evidenceRoot
        PrivateRoot = $privateRoot
        Inputs = @($sourceInputs | ForEach-Object { [ordered]@{ Path = $_.Path; Sha256 = $_.Sha256 } })
    }
}

function Read-CasePlan {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Change
    )
    # Plans come from the product reader, as the entry point would pass them to Invoke-WindowsSqlStep.
    $values = New-ValidPlanValues
    & $Change $values
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes((ConvertTo-Json -InputObject $values -Depth 8))
    $path = Join-Path $script:SuiteRoot ('plan-' + $Name + '.json')
    [IO.File]::WriteAllBytes($path, $bytes)
    $readPath = $path
    $readHash = Get-Sha256Hex -Bytes $bytes
    $capture = Invoke-Captured -CapturedAction { Read-WindowsSqlPlan -Path $readPath -ExpectedHash $readHash }
    return [pscustomobject]@{
        Plan = Get-ReturnedObject -Records $capture.Records
        Error = $capture.Error
        Hash = $readHash
    }
}
$plans = @{
    Approved = Read-CasePlan -Name 'approved' -Change { param($p) }
    NoRestart = Read-CasePlan -Name 'no-restart' -Change { param($p) $p.RestartApproved = $false }
    NoFirewall = Read-CasePlan -Name 'no-firewall' -Change { param($p) $p.FirewallApproved = $false }
    NoApprovals = Read-CasePlan -Name 'no-approvals' -Change {
        param($p)
        $p.RestartApproved = $false
        $p.FirewallApproved = $false
    }
}

# ---- Adapter operations. Reads observe, the record operation journals lifecycle state, the rest change state.
$script:ReadOperations = @(
    'ReadMachineIdentity', 'ReadInstanceIdentity', 'ReadInstanceConfiguration', 'ReadPortOwner', 'ReadNetworkMode',
    'ReadTargetDatabase', 'ReadTargetLogin', 'ReadLifecycleState', 'VerifyTlsConnection', 'VerifyTargetCatalog'
)
$script:RecordOperations = @('WriteLifecycleState')
$script:MutationOperations = @(
    'SaveConfigurationSnapshot', 'SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService',
    'AddFirewallRule', 'CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin',
    'RemoveRuntimeLogin', 'RemoveDatabase', 'RemoveFirewallRule', 'RestoreConfiguration'
)
$script:KnownOperations = @($script:ReadOperations) + @($script:RecordOperations) + @($script:MutationOperations)
$cleanupOperations = @('RemoveRuntimeLogin', 'RemoveDatabase', 'RemoveFirewallRule', 'RestoreConfiguration')
$instanceChanges = @(
    'SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService', 'AddFirewallRule',
    'CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin'
)

function New-FakeAdapter {
    param(
        [Parameter(Mandatory)][hashtable]$Responses,
        [string]$FailOperation = '',
        [string]$FailMessage = ''
    )
    # Test-only seam double. Unknown operations are recorded and refused, so a new side effect cannot hide here.
    $adapter = [pscustomobject]@{
        Kind = 'Fake'
        Responses = $Responses
        FailOperation = $FailOperation
        FailMessage = $FailMessage
        Trace = New-Object 'Collections.Generic.List[object]'
    }
    $adapter | Add-Member -MemberType ScriptMethod -Name InvokeOperation -Value {
        param([string]$Operation, $Request)
        $this.Trace.Add([pscustomobject]@{
                Operation = $Operation
                Request = $Request
                RequestJson = ConvertTo-CanonicalJson -Value $Request
            })
        if ($Operation -cnotin $script:KnownOperations) { throw ('FAKE_UNKNOWN_OPERATION ' + $Operation) }
        if ($Operation -ceq $this.FailOperation) { throw $this.FailMessage }
        if ($this.Responses.ContainsKey($Operation)) {
            return ConvertTo-FixtureObject -Value $this.Responses[$Operation]
        }
        return [pscustomobject]@{ Completed = $true }
    }
    return $adapter
}

function New-Configuration {
    param([switch]$Target, [bool]$ForceEncryption = $false)
    if ($Target) {
        return [ordered]@{
            LoginMode = 'Mixed'
            TcpEnabled = $true
            TcpPort = $fixturePort
            DynamicPorts = $false
            ForceEncryption = $ForceEncryption
            CertificateThumbprint = $fixtureThumbprint
        }
    }
    return [ordered]@{
        LoginMode = 'Windows'
        TcpEnabled = $false
        TcpPort = $null
        DynamicPorts = $false
        ForceEncryption = $ForceEncryption
        CertificateThumbprint = $null
    }
}

function New-Snapshot {
    param([bool]$ForceEncryption = $false)
    # The snapshot is opaque to the product; SnapshotId shows the stored value came from the adapter.
    $snapshot = New-Configuration -ForceEncryption $ForceEncryption
    $snapshot.SnapshotId = 'fixture-snapshot-1'
    return $snapshot
}
$expectedSnapshotJson = ConvertTo-CanonicalJson -Value (New-Snapshot)

function New-FreshResponses {
    param([bool]$ForceEncryption = $false)
    # A laptop instance as reported in the goal: Windows authentication only, TCP off, no server certificate.
    return @{
        ReadMachineIdentity = [ordered]@{ MachineName = $fixtureMachine }
        ReadInstanceIdentity = [ordered]@{
            InstanceName = $fixtureInstance
            InstanceId = $fixtureInstanceId
            ProductVersion = $fixtureVersion
        }
        ReadInstanceConfiguration = New-Configuration -ForceEncryption $ForceEncryption
        ReadPortOwner = [ordered]@{ Port = $fixturePort; InUse = $false; OwnedByInstance = $false }
        ReadNetworkMode = [ordered]@{ Mode = 'Nat'; WslSubnet = $fixtureWslSubnet; OwnedFirewallRule = $null }
        ReadTargetDatabase = [ordered]@{
            Name = $fixtureDatabase
            Exists = $false
            OwnedLifetimeId = $null
            SchemaVersion = $null
        }
        ReadTargetLogin = [ordered]@{ Name = $fixtureLogin; Exists = $false; OwnedLifetimeId = $null }
        ReadLifecycleState = $null
        SaveConfigurationSnapshot = New-Snapshot -ForceEncryption $ForceEncryption
        VerifyTlsConnection = [ordered]@{ Encrypted = $true; CertificateValidated = $true }
        VerifyTargetCatalog = [ordered]@{ CatalogMatches = $true; SchemaVersion = 4 }
    }
}

function New-AppliedState {
    return [ordered]@{
        LifetimeId = $fixtureLifetime
        PlanHash = $plans.Approved.Hash
        Status = 'Applied'
        Snapshot = New-Snapshot
        TargetConfiguration = New-Configuration -Target
        CompletedChanges = @($instanceChanges)
        FailedOperation = $null
    }
}

function New-AppliedResponses {
    # The instance after a complete apply: target configuration, owned database/login and the owned firewall rule.
    $responses = New-FreshResponses
    $responses.ReadInstanceConfiguration = New-Configuration -Target
    $responses.ReadPortOwner = [ordered]@{ Port = $fixturePort; InUse = $true; OwnedByInstance = $true }
    $responses.ReadNetworkMode = [ordered]@{
        Mode = 'Nat'
        WslSubnet = $fixtureWslSubnet
        OwnedFirewallRule = [ordered]@{ RemoteAddress = $fixtureWslSubnet; Port = $fixturePort }
    }
    $responses.ReadTargetDatabase = [ordered]@{
        Name = $fixtureDatabase
        Exists = $true
        OwnedLifetimeId = $fixtureLifetime
        SchemaVersion = 4
    }
    $responses.ReadTargetLogin = [ordered]@{ Name = $fixtureLogin; Exists = $true; OwnedLifetimeId = $fixtureLifetime }
    $responses.ReadLifecycleState = New-AppliedState
    return $responses
}

function Invoke-StepCase {
    param(
        [Parameter(Mandatory)][string]$PlanName,
        [Parameter(Mandatory)][string]$Step,
        [Parameter(Mandatory)][hashtable]$Responses,
        [string]$FailOperation = '',
        [string]$FailMessage = ''
    )
    $adapter = New-FakeAdapter -Responses $Responses -FailOperation $FailOperation -FailMessage $FailMessage
    $planCase = $plans[$PlanName]
    $script:ExternalCalls.Clear()
    if ($null -eq $planCase.Plan) {
        $capture = [pscustomobject]@{
            Records = @()
            Error = 'plan fixture not readable: ' + [string]$planCase.Error
            ErrorType = $null
        }
    }
    else {
        $stepPlan = $planCase.Plan
        $stepName = $Step
        $stepAdapter = $adapter
        $capture = Invoke-Captured -CapturedAction {
            Invoke-WindowsSqlStep -Plan $stepPlan -Step $stepName -Adapter $stepAdapter
        }
    }
    return [pscustomobject]@{
        Adapter = $adapter
        Capture = $capture
        Result = Get-ReturnedObject -Records $capture.Records
        Error = $capture.Error
        ErrorType = $capture.ErrorType
        External = @($script:ExternalCalls.ToArray())
        Operations = [string[]]@($adapter.Trace | ForEach-Object { $_.Operation })
    }
}

# ---- Trace readers. They only read the fake's record; none of them repeats a product decision.
function Get-MutationNames {
    param([string[]]$Operations)
    return , [string[]]@($Operations | Where-Object {
            $_ -cnotin $script:ReadOperations -and $_ -cnotin $script:RecordOperations
        })
}

function Test-OnlyReads {
    param($Case)
    # Non-empty, so a step that never ran cannot satisfy "reads only".
    $nonRead = @($Case.Operations | Where-Object { $_ -cnotin $script:ReadOperations })
    return $Case.Operations.Count -gt 0 -and $nonRead.Count -eq 0
}

function Get-Requests {
    param(
        [Parameter(Mandatory)]$Case,
        [Parameter(Mandatory)][string]$Operation
    )
    return , @($Case.Adapter.Trace | Where-Object { $_.Operation -ceq $Operation } | ForEach-Object { $_.Request })
}

function Get-FirstIndex {
    param(
        [string[]]$Operations,
        [Parameter(Mandatory)][string]$Name
    )
    if ($null -eq $Operations) { return -1 }
    return [Array]::IndexOf($Operations, $Name)
}

function Get-RequestBoundaryViolations {
    param([Parameter(Mandatory)]$Case)
    # Exact target only (no other database or login, never sa), no plaintext secret field, no unknown operation.
    $violations = New-Object 'Collections.Generic.List[string]'
    foreach ($entry in $Case.Adapter.Trace) {
        if ($entry.Operation -cnotin $script:KnownOperations) {
            $violations.Add('unknown operation ' + $entry.Operation)
        }
        $request = $entry.Request
        $database = Get-FieldValue -Object $request -Name 'Database'
        if ($null -ne $database -and [string]$database -cne $fixtureDatabase) {
            $violations.Add('{0} Database={1}' -f $entry.Operation, $database)
        }
        $login = Get-FieldValue -Object $request -Name 'Login'
        if ($null -ne $login -and [string]$login -cne $fixtureLogin) {
            $violations.Add('{0} Login={1}' -f $entry.Operation, $login)
        }
        foreach ($fieldName in (Get-FieldNames -Object $request)) {
            $fieldValue = Get-FieldValue -Object $request -Name $fieldName
            if ($fieldName -match '(?i)password|secret|pwd' -and $fieldValue -is [string]) {
                $violations.Add('{0} plaintext field {1}' -f $entry.Operation, $fieldName)
            }
        }
    }
    return , @($violations.ToArray())
}

function Test-SentinelAbsent {
    param([Parameter(Mandatory)]$Case)
    $texts = New-Object 'Collections.Generic.List[string]'
    $texts.Add((Get-CaptureText -Capture $Case.Capture))
    foreach ($entry in $Case.Adapter.Trace) { $texts.Add([string]$entry.RequestJson) }
    foreach ($value in [Environment]::GetEnvironmentVariables().Values) { $texts.Add([string]$value) }
    return -not ($texts -join "`n").Contains($secretSentinel)
}

function Get-CaseDetail {
    param([Parameter(Mandatory)]$Case)
    return ('error <{0}> status <{1}> trace <{2}> external <{3}>' -f
        $Case.Error,
        (Get-FieldValue -Object $Case.Result -Name 'Status'),
        ($Case.Operations -join ','),
        ($Case.External -join ','))
}

function Test-StepRefusedBeforeChange {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Code,
        [Parameter(Mandatory)][string]$PlanName,
        [Parameter(Mandatory)][string]$Step,
        [Parameter(Mandatory)][hashtable]$Responses
    )
    $case = Invoke-StepCase -PlanName $PlanName -Step $Step -Responses $Responses
    $rightCause = $case.Error -ceq $Code
    $readsOnly = Test-OnlyReads -Case $case
    $refusalName = '{0}: {1} refused as {2} after reads only (no mutation, no state write)' -f $Name, $Step, $Code
    Assert-True -Name $refusalName `
        -Condition ($rightCause -and $readsOnly -and $case.External.Count -eq 0) -Detail (Get-CaseDetail -Case $case)
}

$allSteps = @('Preflight', 'Plan', 'Apply', 'Verify', 'Cleanup')

# ==== LC-01..03 identity is compared before instance, SQL, service or registry access =======================
foreach ($step in $allSteps) {
    $responses = New-FreshResponses
    $responses.ReadMachineIdentity.MachineName = 'OTHER-HOST'
    $case = Invoke-StepCase -PlanName 'Approved' -Step $step -Responses $responses
    $rightCause = $case.Error -ceq 'WINDOWS_SQL_IDENTITY_MISMATCH'
    $stoppedAtMachine = ($case.Operations -join ',') -ceq 'ReadMachineIdentity'
    Assert-True -Name ('LC-01 {0}: another machine stops right after ReadMachineIdentity' -f $step) `
        -Condition ($rightCause -and $stoppedAtMachine) -Detail (Get-CaseDetail -Case $case)
}
foreach ($step in $allSteps) {
    $responses = New-FreshResponses
    $responses.ReadInstanceIdentity.InstanceId = 'MSSQL16.SQLEXPRESS'
    $case = Invoke-StepCase -PlanName 'Approved' -Step $step -Responses $responses
    $rightCause = $case.Error -ceq 'WINDOWS_SQL_IDENTITY_MISMATCH'
    $stoppedAtInstance = ($case.Operations -join ',') -ceq 'ReadMachineIdentity,ReadInstanceIdentity'
    Assert-True -Name ('LC-02 {0}: another instance id stops right after ReadInstanceIdentity' -f $step) `
        -Condition ($rightCause -and $stoppedAtInstance) -Detail (Get-CaseDetail -Case $case)
}
$versionResponses = New-FreshResponses
$versionResponses.ReadInstanceIdentity.ProductVersion = '17.0.1000.6'
$versionCase = Invoke-StepCase -PlanName 'Approved' -Step 'Apply' -Responses $versionResponses
Assert-True -Name 'LC-03 Apply: another product version stops right after ReadInstanceIdentity' `
    -Condition ($versionCase.Error -ceq 'WINDOWS_SQL_IDENTITY_MISMATCH' -and
    ($versionCase.Operations -join ',') -ceq 'ReadMachineIdentity,ReadInstanceIdentity') `
    -Detail (Get-CaseDetail -Case $versionCase)

# ==== LC-04..06 Preflight and Plan only read and report =====================================================
$preflight = Invoke-StepCase -PlanName 'Approved' -Step 'Preflight' -Responses (New-FreshResponses)
$preflightObservation = Get-FieldValue -Object $preflight.Result -Name 'Observation'
$missingSections = @('Machine', 'Instance', 'Configuration', 'Port', 'Network', 'Database', 'Login' | Where-Object {
        $null -eq (Get-FieldValue -Object $preflightObservation -Name $_)
    })
$preflightStep = Get-FieldValue -Object $preflight.Result -Name 'Step'
$preflightKind = Get-FieldValue -Object $preflight.Result -Name 'AdapterKind'
Assert-True -Name 'LC-04 Preflight: returns Step Preflight and adapter kind Fake after reads only' `
    -Condition ($null -eq $preflight.Error -and $preflightStep -ceq 'Preflight' -and $preflightKind -ceq 'Fake' -and
    (Test-OnlyReads -Case $preflight) -and $preflight.External.Count -eq 0) `
    -Detail (Get-CaseDetail -Case $preflight)
Assert-True -Name 'LC-04 Preflight: Observation carries all seven sections used by Get-WindowsSqlChangePlan' `
    -Condition ($null -ne $preflightObservation -and $missingSections.Count -eq 0) `
    -Detail ('missing <{0}> | {1}' -f ($missingSections -join ','), (Get-CaseDetail -Case $preflight))
$preflightDatabaseReads = Get-Requests -Case $preflight -Operation 'ReadTargetDatabase'
$preflightLoginReads = Get-Requests -Case $preflight -Operation 'ReadTargetLogin'
Assert-True -Name 'LC-04 Preflight: database and login reads name only the exact plan target' `
    -Condition ($preflightDatabaseReads.Count -ge 1 -and $preflightLoginReads.Count -ge 1 -and
    (Get-RequestBoundaryViolations -Case $preflight).Count -eq 0) `
    -Detail ('violations <{0}> | {1}' -f ((Get-RequestBoundaryViolations -Case $preflight) -join '; '),
    (Get-CaseDetail -Case $preflight))

$planStep = Invoke-StepCase -PlanName 'Approved' -Step 'Plan' -Responses (New-FreshResponses)
$planStepChangePlan = Get-FieldValue -Object $planStep.Result -Name 'ChangePlan'
$planStepDecision = Get-FieldValue -Object $planStepChangePlan -Name 'Decision'
Assert-True -Name 'LC-05 Plan: returns ChangePlan Decision Apply after reads only' `
    -Condition ($null -eq $planStep.Error -and $planStepDecision -ceq 'Apply' -and (Test-OnlyReads -Case $planStep)) `
    -Detail (Get-CaseDetail -Case $planStep)
Assert-EqualInCase -Name 'LC-05 Plan: ChangePlan lists the fresh NAT change set' `
    -Expected (Join-SortedNames -Names $instanceChanges) `
    -Actual (Join-SortedNames -Names @(Get-FieldValue -Object $planStepChangePlan -Name 'Changes')) `
    -Context (Get-CaseDetail -Case $planStep)

$planConflictResponses = New-FreshResponses
$planConflictResponses.ReadPortOwner = [ordered]@{ Port = $fixturePort; InUse = $true; OwnedByInstance = $false }
$planConflict = Invoke-StepCase -PlanName 'Approved' -Step 'Plan' -Responses $planConflictResponses
$planConflictChangePlan = Get-FieldValue -Object $planConflict.Result -Name 'ChangePlan'
$planConflictDecision = Get-FieldValue -Object $planConflictChangePlan -Name 'Decision'
$planConflictBlockers = @(Get-FieldValue -Object $planConflictChangePlan -Name 'Blockers')
$planConflictBlocked = $planConflictBlockers -ccontains 'WINDOWS_SQL_PORT_CONFLICT'
Assert-True -Name 'LC-06 Plan: a port conflict is reported as Blocked (not thrown) after reads only' `
    -Condition ($null -eq $planConflict.Error -and $planConflictDecision -ceq 'Blocked' -and $planConflictBlocked -and
    (Test-OnlyReads -Case $planConflict)) -Detail (Get-CaseDetail -Case $planConflict)

# ==== LC-07 Apply on a fresh instance under NAT ==============================================================
$apply = Invoke-StepCase -PlanName 'Approved' -Step 'Apply' -Responses (New-FreshResponses)
$applyOk = $null -eq $apply.Error -and (Get-FieldValue -Object $apply.Result -Name 'Status') -ceq 'Applied'
$applyDetail = Get-CaseDetail -Case $apply
Assert-True -Name 'LC-07 Apply: Status Applied, Step Apply, adapter kind Fake, no external call' `
    -Condition ($applyOk -and (Get-FieldValue -Object $apply.Result -Name 'Step') -ceq 'Apply' -and
    (Get-FieldValue -Object $apply.Result -Name 'AdapterKind') -ceq 'Fake' -and $apply.External.Count -eq 0) `
    -Detail $applyDetail
$applyMutations = Get-MutationNames -Operations $apply.Operations
Assert-EqualInCase -Name 'LC-07 Apply: exact mutation set is the snapshot plus the fresh NAT changes' `
    -Expected (Join-SortedNames -Names (@('SaveConfigurationSnapshot') + $instanceChanges)) `
    -Actual (Join-SortedNames -Names $applyMutations) -Context $applyDetail
Assert-True -Name 'LC-07 Apply: SaveConfigurationSnapshot is the first mutation' `
    -Condition ($applyOk -and $applyMutations.Count -gt 0 -and $applyMutations[0] -ceq 'SaveConfigurationSnapshot') `
    -Detail $applyDetail
$restartIndex = Get-FirstIndex -Operations $apply.Operations -Name 'RestartService'
$configIndexes = @('SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate' | ForEach-Object {
        Get-FirstIndex -Operations $apply.Operations -Name $_
    })
$lateOrMissingConfig = @($configIndexes | Where-Object { $_ -lt 0 -or $_ -gt $restartIndex })
$restartAfterConfig = $restartIndex -ge 0 -and $lateOrMissingConfig.Count -eq 0
$migrateAfterCreate = (Get-FirstIndex -Operations $apply.Operations -Name 'ApplyMigrations') -gt
(Get-FirstIndex -Operations $apply.Operations -Name 'CreateDatabase') -and
(Get-FirstIndex -Operations $apply.Operations -Name 'CreateDatabase') -ge 0
Assert-True -Name 'LC-07 Apply: RestartService follows the setting changes; ApplyMigrations follows CreateDatabase' `
    -Condition ($applyOk -and $restartAfterConfig -and $migrateAfterCreate) -Detail $applyDetail

# The journal names the snapshot before the first instance change, so a crash leaves a recoverable state.
$snapshotIndex = Get-FirstIndex -Operations $apply.Operations -Name 'SaveConfigurationSnapshot'
$firstChangeIndex = @($instanceChanges | ForEach-Object { Get-FirstIndex -Operations $apply.Operations -Name $_ } |
        Where-Object { $_ -ge 0 } | Sort-Object | Select-Object -First 1)
$applyingIndex = -1
for ($index = 0; $index -lt $apply.Adapter.Trace.Count; $index++) {
    $entry = $apply.Adapter.Trace[$index]
    $isApplyingState = $entry.Operation -ceq 'WriteLifecycleState' -and
    (Get-FieldValue -Object $entry.Request -Name 'Status') -ceq 'Applying'
    if ($isApplyingState) {
        $applyingIndex = $index
        break
    }
}
$applyingState = if ($applyingIndex -ge 0) { $apply.Adapter.Trace[$applyingIndex].Request } else { $null }
$applyingSnapshot = ConvertTo-CanonicalJson -Value (Get-FieldValue -Object $applyingState -Name 'Snapshot')
$applyingOrdered = $snapshotIndex -ge 0 -and $applyingIndex -gt $snapshotIndex -and $firstChangeIndex.Count -eq 1 -and
$applyingIndex -lt $firstChangeIndex[0]
Assert-True -Name 'LC-07 Apply: Applying state with the snapshot is written after the snapshot and before any change' `
    -Condition ($applyOk -and $applyingOrdered -and $applyingSnapshot -ceq $expectedSnapshotJson) -Detail $applyDetail

$requestChecks = @(
    @{ Operation = 'SetTcpPort'; Field = 'Port'; Expected = [string]$fixturePort },
    @{ Operation = 'SetServerCertificate'; Field = 'Thumbprint'; Expected = $fixtureThumbprint },
    @{ Operation = 'AddFirewallRule'; Field = 'Port'; Expected = [string]$fixturePort },
    # Only the observed WSL range, never a LAN-wide scope.
    @{ Operation = 'AddFirewallRule'; Field = 'RemoteAddress'; Expected = $fixtureWslSubnet },
    @{ Operation = 'CreateDatabase'; Field = 'Database'; Expected = $fixtureDatabase },
    @{ Operation = 'CreateDatabase'; Field = 'LifetimeId'; Expected = $fixtureLifetime },
    @{ Operation = 'ApplyMigrations'; Field = 'Database'; Expected = $fixtureDatabase },
    @{ Operation = 'CreateRuntimeLogin'; Field = 'Login'; Expected = $fixtureLogin },
    @{ Operation = 'CreateRuntimeLogin'; Field = 'Database'; Expected = $fixtureDatabase },
    @{ Operation = 'CreateRuntimeLogin'; Field = 'LifetimeId'; Expected = $fixtureLifetime }
)
foreach ($check in $requestChecks) {
    $requests = Get-Requests -Case $apply -Operation $check.Operation
    $values = @($requests | ForEach-Object { [string](Get-FieldValue -Object $_ -Name $check.Field) })
    $allMatch = $requests.Count -ge 1 -and @($values | Where-Object { $_ -cne $check.Expected }).Count -eq 0
    Assert-True -Name ('LC-07 Apply: {0} request {1} is {2}' -f $check.Operation, $check.Field, $check.Expected) `
        -Condition $allMatch `
        -Detail ('requests {0}, values <{1}> | {2}' -f $requests.Count, ($values -join ','), $applyDetail)
}
$applyViolations = Get-RequestBoundaryViolations -Case $apply
Assert-True -Name 'LC-07 Apply: every request names only the plan database/login, no sa and no plaintext secret field' `
    -Condition ($applyOk -and $applyViolations.Count -eq 0) `
    -Detail (($applyViolations -join '; ') + ' | ' + $applyDetail)

$applyStates = Get-Requests -Case $apply -Operation 'WriteLifecycleState'
$finalState = if ($applyStates.Count -gt 0) { $applyStates[$applyStates.Count - 1] } else { $null }
$finalStateChecks = @(
    @{ Field = 'Status'; Expected = 'Applied'; Actual = [string](Get-FieldValue -Object $finalState -Name 'Status') },
    @{ Field = 'LifetimeId'; Expected = $fixtureLifetime
        Actual = [string](Get-FieldValue -Object $finalState -Name 'LifetimeId') },
    @{ Field = 'PlanHash'; Expected = $plans.Approved.Hash
        Actual = [string](Get-FieldValue -Object $finalState -Name 'PlanHash') },
    @{ Field = 'Snapshot'; Expected = $expectedSnapshotJson
        Actual = ConvertTo-CanonicalJson -Value (Get-FieldValue -Object $finalState -Name 'Snapshot') },
    @{ Field = 'TargetConfiguration'; Expected = ConvertTo-CanonicalJson -Value (New-Configuration -Target)
        Actual = ConvertTo-CanonicalJson -Value (Get-FieldValue -Object $finalState -Name 'TargetConfiguration') },
    @{ Field = 'CompletedChanges'; Expected = Join-SortedNames -Names $instanceChanges
        Actual = Join-SortedNames -Names @(Get-FieldValue -Object $finalState -Name 'CompletedChanges') },
    @{ Field = 'FailedOperation'; Expected = ''
        Actual = [string](Get-FieldValue -Object $finalState -Name 'FailedOperation') }
)
foreach ($check in $finalStateChecks) {
    Assert-True -Name ('LC-07 Apply: final lifecycle state {0}' -f $check.Field) `
        -Condition ($applyOk -and $null -ne $finalState -and $check.Actual -ceq $check.Expected) `
        -Detail ('expected <{0}> actual <{1}> | {2}' -f $check.Expected, $check.Actual, $applyDetail)
}

# ==== LC-08..10 ForceEncryption, unused approvals and rerun ================================================
$forceOn = Invoke-StepCase -PlanName 'Approved' -Step 'Apply' -Responses (New-FreshResponses -ForceEncryption $true)
$forceOnStates = Get-Requests -Case $forceOn -Operation 'WriteLifecycleState'
$forceOnFinal = if ($forceOnStates.Count -gt 0) { $forceOnStates[$forceOnStates.Count - 1] } else { $null }
$forceOnTarget = Get-FieldValue -Object $forceOnFinal -Name 'TargetConfiguration'
$forceOnStatus = Get-FieldValue -Object $forceOn.Result -Name 'Status'
$forceOnKept = (Get-FieldValue -Object $forceOnTarget -Name 'ForceEncryption') -eq $true
$forceOnUntouched = @($forceOn.Operations | Where-Object { $_ -match 'ForceEncryption' }).Count -eq 0
Assert-True -Name 'LC-08 Apply with ForceEncryption on: applied, target keeps it on, no operation touches it' `
    -Condition ($null -eq $forceOn.Error -and $forceOnStatus -ceq 'Applied' -and $forceOnKept -and $forceOnUntouched) `
    -Detail (Get-CaseDetail -Case $forceOn)

$configuredResponses = New-FreshResponses
$configuredResponses.ReadInstanceConfiguration = New-Configuration -Target
$configuredResponses.ReadPortOwner = [ordered]@{ Port = $fixturePort; InUse = $true; OwnedByInstance = $true }
$configuredResponses.ReadNetworkMode = [ordered]@{ Mode = 'Mirrored'; WslSubnet = $null; OwnedFirewallRule = $null }
$configured = Invoke-StepCase -PlanName 'NoApprovals' -Step 'Apply' -Responses $configuredResponses
$configuredStatus = Get-FieldValue -Object $configured.Result -Name 'Status'
Assert-True -Name 'LC-09 Apply on a configured instance without restart/firewall approval: applied' `
    -Condition ($null -eq $configured.Error -and $configuredStatus -ceq 'Applied') `
    -Detail (Get-CaseDetail -Case $configured)
$configuredExpected = @('ApplyMigrations', 'CreateDatabase', 'CreateRuntimeLogin', 'SaveConfigurationSnapshot')
Assert-EqualInCase -Name 'LC-09 configured instance: no restart and no firewall change, only the new target' `
    -Expected (Join-SortedNames -Names $configuredExpected) `
    -Actual (Join-SortedNames -Names (Get-MutationNames -Operations $configured.Operations)) `
    -Context (Get-CaseDetail -Case $configured)

$rerun = Invoke-StepCase -PlanName 'Approved' -Step 'Apply' -Responses (New-AppliedResponses)
Assert-True -Name 'LC-10 Apply rerun with matching owner/version: Status Unchanged, no mutation, no reinitialisation' `
    -Condition ($null -eq $rerun.Error -and (Get-FieldValue -Object $rerun.Result -Name 'Status') -ceq 'Unchanged' -and
    $rerun.Operations.Count -gt 0 -and (Get-MutationNames -Operations $rerun.Operations).Count -eq 0) `
    -Detail (Get-CaseDetail -Case $rerun)

# ==== LC-11..17 Apply refuses before any change ============================================================
$conflictResponses = New-FreshResponses
$conflictResponses.ReadPortOwner = [ordered]@{ Port = $fixturePort; InUse = $true; OwnedByInstance = $false }
Test-StepRefusedBeforeChange -Name 'LC-11 port used by another listener' -Code 'WINDOWS_SQL_PORT_CONFLICT' `
    -PlanName 'Approved' -Step 'Apply' -Responses $conflictResponses

$foreignResponses = New-FreshResponses
$foreignResponses.ReadTargetDatabase = [ordered]@{
    Name = $fixtureDatabase
    Exists = $true
    OwnedLifetimeId = $null
    SchemaVersion = $null
}
$foreign = Invoke-StepCase -PlanName 'Approved' -Step 'Apply' -Responses $foreignResponses
$foreignViolations = Get-RequestBoundaryViolations -Case $foreign
Assert-True -Name 'LC-12 database without owner mark: Apply refused as WINDOWS_SQL_TARGET_NOT_OWNED after reads only' `
    -Condition ($foreign.Error -ceq 'WINDOWS_SQL_TARGET_NOT_OWNED' -and (Test-OnlyReads -Case $foreign) -and
    $foreign.External.Count -eq 0) -Detail (Get-CaseDetail -Case $foreign)
Assert-True -Name 'LC-12 existing database without owner mark: only the exact plan database was read, nothing listed' `
    -Condition ($foreign.Error -ceq 'WINDOWS_SQL_TARGET_NOT_OWNED' -and
    (Get-Requests -Case $foreign -Operation 'ReadTargetDatabase').Count -ge 1 -and $foreignViolations.Count -eq 0) `
    -Detail (($foreignViolations -join '; ') + ' | ' + (Get-CaseDetail -Case $foreign))

$versionMismatchResponses = New-AppliedResponses
$versionMismatchResponses.ReadTargetDatabase.SchemaVersion = 3
Test-StepRefusedBeforeChange -Name 'LC-13 owned database at another schema version' `
    -Code 'WINDOWS_SQL_OWNER_OR_VERSION_MISMATCH' -PlanName 'Approved' -Step 'Apply' `
    -Responses $versionMismatchResponses

$unclearResponses = New-FreshResponses
$unclearResponses.ReadInstanceConfiguration.LoginMode = 'Unknown'
Test-StepRefusedBeforeChange -Name 'LC-14 unclear login mode' -Code 'WINDOWS_SQL_OBSERVATION_UNCLEAR' `
    -PlanName 'Approved' -Step 'Apply' -Responses $unclearResponses

Test-StepRefusedBeforeChange -Name 'LC-15 restart needed without RestartApproved' `
    -Code 'WINDOWS_SQL_RESTART_NOT_APPROVED' -PlanName 'NoRestart' -Step 'Apply' -Responses (New-FreshResponses)
Test-StepRefusedBeforeChange -Name 'LC-16 NAT firewall rule needed without FirewallApproved' `
    -Code 'WINDOWS_SQL_FIREWALL_NOT_APPROVED' -PlanName 'NoFirewall' -Step 'Apply' -Responses (New-FreshResponses)

$scopeResponses = New-FreshResponses
$scopeResponses.ReadNetworkMode.WslSubnet = '0.0.0.0/0'
Test-StepRefusedBeforeChange -Name 'LC-17 observed WSL range 0.0.0.0/0' -Code 'WINDOWS_SQL_FIREWALL_SCOPE_INVALID' `
    -PlanName 'Approved' -Step 'Apply' -Responses $scopeResponses

# ==== LC-18 a failing adapter operation leaves a recoverable, unsuccessful state ============================
# The forced failure text carries a secret-shaped sentinel, as a provider error with a connection string might.
foreach ($failing in @('SetTcpPort', 'CreateDatabase')) {
    $failed = Invoke-StepCase -PlanName 'Approved' -Step 'Apply' -Responses (New-FreshResponses) `
        -FailOperation $failing -FailMessage ('provider failure; Password=' + $secretSentinel)
    $failIndex = Get-FirstIndex -Operations $failed.Operations -Name $failing
    $afterFailure = @($failed.Operations | Select-Object -Skip ($failIndex + 1))
    $beforeFailure = @($failed.Operations | Select-Object -First ([Math]::Max($failIndex, 0)))
    $mutationsBefore = Get-MutationNames -Operations $beforeFailure
    $completedBefore = @($mutationsBefore | Where-Object { $_ -cne 'SaveConfigurationSnapshot' })
    $nonRecordAfter = @($afterFailure | Where-Object { $_ -cnotin $script:RecordOperations })
    $cleanupDuringApply = @($failed.Operations | Where-Object { $_ -cin $cleanupOperations })
    $failedStates = Get-Requests -Case $failed -Operation 'WriteLifecycleState'
    $lastFailedState = if ($failedStates.Count -gt 0) { $failedStates[$failedStates.Count - 1] } else { $null }
    $injected = $failIndex -ge 0
    $failedDetail = Get-CaseDetail -Case $failed

    Assert-True -Name ('LC-18 failure at {0}: Apply throws exactly WINDOWS_SQL_APPLY_FAILED' -f $failing) `
        -Condition ($injected -and $failed.Error -ceq 'WINDOWS_SQL_APPLY_FAILED') -Detail $failedDetail
    Assert-True -Name ('LC-18 failure at {0}: only lifecycle state writes follow the failed operation' -f $failing) `
        -Condition ($injected -and $nonRecordAfter.Count -eq 0 -and $afterFailure.Count -ge 1) -Detail $failedDetail
    Assert-True -Name ('LC-18 failure at {0}: no cleanup, restore or removal runs on its own' -f $failing) `
        -Condition ($injected -and $cleanupDuringApply.Count -eq 0) -Detail $failedDetail
    Assert-True -Name ('LC-18 failure at {0}: no state ever records Applied' -f $failing) `
        -Condition ($injected -and @($failedStates | Where-Object {
                (Get-FieldValue -Object $_ -Name 'Status') -ceq 'Applied'
            }).Count -eq 0) -Detail $failedDetail
    $failedStateChecks = @(
        @{ Field = 'Status'; Expected = 'Failed'
            Actual = [string](Get-FieldValue -Object $lastFailedState -Name 'Status') },
        @{ Field = 'FailedOperation'; Expected = $failing
            Actual = [string](Get-FieldValue -Object $lastFailedState -Name 'FailedOperation') },
        # Derived from the fake's own record of what ran before the failure, not from the product.
        @{ Field = 'CompletedChanges'; Expected = Join-SortedNames -Names $completedBefore
            Actual = Join-SortedNames -Names @(Get-FieldValue -Object $lastFailedState -Name 'CompletedChanges') },
        @{ Field = 'Snapshot'; Expected = $expectedSnapshotJson
            Actual = ConvertTo-CanonicalJson -Value (Get-FieldValue -Object $lastFailedState -Name 'Snapshot') }
    )
    foreach ($check in $failedStateChecks) {
        Assert-True -Name ('LC-18 failure at {0}: last lifecycle state {1}' -f $failing, $check.Field) `
            -Condition ($injected -and $null -ne $lastFailedState -and $check.Actual -ceq $check.Expected) `
            -Detail ('expected <{0}> actual <{1}> | {2}' -f $check.Expected, $check.Actual, $failedDetail)
    }
    $sentinelName = 'LC-18 failure at {0}: the provider text with the secret sentinel reaches no error, output, ' +
    'state, request or environment'
    $sentinelAbsent = Test-SentinelAbsent -Case $failed
    Assert-True -Name ($sentinelName -f $failing) `
        -Condition ($injected -and $failed.Error -ceq 'WINDOWS_SQL_APPLY_FAILED' -and $sentinelAbsent) `
        -Detail $failedDetail
}

# ==== LC-19..21 Verify checks TLS and the target catalog through the adapter ===============================
$verify = Invoke-StepCase -PlanName 'Approved' -Step 'Verify' -Responses (New-AppliedResponses)
$verifyOk = $null -eq $verify.Error -and (Get-FieldValue -Object $verify.Result -Name 'Status') -ceq 'Verified'
$verifyMutations = Get-MutationNames -Operations $verify.Operations
Assert-True -Name 'LC-19 Verify: Status Verified without any mutation' `
    -Condition ($verifyOk -and $verify.Operations.Count -gt 0 -and $verifyMutations.Count -eq 0) `
    -Detail (Get-CaseDetail -Case $verify)
Assert-EqualInCase -Name 'LC-19 Verify: the fake TLS success is reported with adapter kind Fake, not as a live result' `
    -Expected 'Fake' -Actual (Get-FieldValue -Object $verify.Result -Name 'AdapterKind') `
    -Context (Get-CaseDetail -Case $verify)
$tlsRequests = Get-Requests -Case $verify -Operation 'VerifyTlsConnection'
$tlsStrict = $tlsRequests.Count -ge 1 -and @($tlsRequests | Where-Object {
        (Get-FieldValue -Object $_ -Name 'Database') -cne $fixtureDatabase -or
        (Get-FieldValue -Object $_ -Name 'Encrypt') -cne 'Mandatory' -or
        (Get-FieldValue -Object $_ -Name 'TrustServerCertificate') -isnot [bool] -or
        (Get-FieldValue -Object $_ -Name 'TrustServerCertificate')
    }).Count -eq 0
Assert-True -Name 'LC-19 Verify: TLS check asks Encrypt Mandatory, TrustServerCertificate false, plan database' `
    -Condition $tlsStrict -Detail ('requests <{0}> | {1}' -f (@($verify.Adapter.Trace |
                Where-Object Operation -CEQ 'VerifyTlsConnection' | ForEach-Object RequestJson) -join ' '),
    (Get-CaseDetail -Case $verify))
$catalogRequests = Get-Requests -Case $verify -Operation 'VerifyTargetCatalog'
$catalogOther = @($catalogRequests | Where-Object {
        (Get-FieldValue -Object $_ -Name 'Database') -cne $fixtureDatabase
    })
Assert-True -Name 'LC-19 Verify: catalog check names the plan database' `
    -Condition ($catalogRequests.Count -ge 1 -and $catalogOther.Count -eq 0) `
    -Detail ('requests {0} | {1}' -f $catalogRequests.Count, (Get-CaseDetail -Case $verify))

$verifyFailures = @(
    @{ Name = 'LC-20 certificate not validated'; Code = 'WINDOWS_SQL_TLS_VERIFY_FAILED'
        Change = { param($r) $r.VerifyTlsConnection.CertificateValidated = $false } },
    @{ Name = 'LC-20 connection not encrypted'; Code = 'WINDOWS_SQL_TLS_VERIFY_FAILED'
        Change = { param($r) $r.VerifyTlsConnection.Encrypted = $false } },
    @{ Name = 'LC-21 catalog mismatch'; Code = 'WINDOWS_SQL_CATALOG_VERIFY_FAILED'
        Change = { param($r) $r.VerifyTargetCatalog.CatalogMatches = $false } }
)
foreach ($case in $verifyFailures) {
    $responses = New-AppliedResponses
    & $case.Change $responses
    $verifyFailure = Invoke-StepCase -PlanName 'Approved' -Step 'Verify' -Responses $responses
    $verifyFailureMutations = Get-MutationNames -Operations $verifyFailure.Operations
    Assert-True -Name ('{0}: Verify fails as {1} without any mutation' -f $case.Name, $case.Code) `
        -Condition ($verifyFailure.Error -ceq $case.Code -and $verifyFailure.Operations.Count -gt 0 -and
        $verifyFailureMutations.Count -eq 0) -Detail (Get-CaseDetail -Case $verifyFailure)
}

# ==== LC-22..28 Cleanup removes only owned resources and restores the snapshot ==============================
$cleanup = Invoke-StepCase -PlanName 'Approved' -Step 'Cleanup' -Responses (New-AppliedResponses)
$cleanupOk = $null -eq $cleanup.Error -and (Get-FieldValue -Object $cleanup.Result -Name 'Status') -ceq 'Cleaned'
$cleanupDetail = Get-CaseDetail -Case $cleanup
$cleanupStep = Get-FieldValue -Object $cleanup.Result -Name 'Step'
Assert-True -Name 'LC-22 Cleanup: Status Cleaned, Step Cleanup, no external call' `
    -Condition ($cleanupOk -and $cleanupStep -ceq 'Cleanup' -and $cleanup.External.Count -eq 0) `
    -Detail $cleanupDetail
$expectedCleanupMutations = @(
    'RemoveDatabase', 'RemoveFirewallRule', 'RemoveRuntimeLogin', 'RestartService', 'RestoreConfiguration'
)
Assert-EqualInCase -Name 'LC-22 Cleanup: exact mutation set removes owned login/database/rule, restores the snapshot' `
    -Expected (Join-SortedNames -Names $expectedCleanupMutations) `
    -Actual (Join-SortedNames -Names (Get-MutationNames -Operations $cleanup.Operations)) -Context $cleanupDetail
$restoreRequests = Get-Requests -Case $cleanup -Operation 'RestoreConfiguration'
$restoredSnapshot = if ($restoreRequests.Count -eq 1) {
    ConvertTo-CanonicalJson -Value (Get-FieldValue -Object $restoreRequests[0] -Name 'Snapshot')
} else { $null }
Assert-True -Name 'LC-22 Cleanup: RestoreConfiguration gets the stored snapshot unchanged (ForceEncryption included)' `
    -Condition ($cleanupOk -and $restoredSnapshot -ceq $expectedSnapshotJson) -Detail $cleanupDetail
Assert-True -Name 'LC-22 Cleanup: RestartService follows RestoreConfiguration' `
    -Condition ($cleanupOk -and (Get-FirstIndex -Operations $cleanup.Operations -Name 'RestartService') -gt
    (Get-FirstIndex -Operations $cleanup.Operations -Name 'RestoreConfiguration') -and
    (Get-FirstIndex -Operations $cleanup.Operations -Name 'RestoreConfiguration') -ge 0) -Detail $cleanupDetail
foreach ($check in @(
        @{ Operation = 'RemoveDatabase'; Field = 'Database'; Expected = $fixtureDatabase },
        @{ Operation = 'RemoveDatabase'; Field = 'LifetimeId'; Expected = $fixtureLifetime },
        @{ Operation = 'RemoveRuntimeLogin'; Field = 'Login'; Expected = $fixtureLogin },
        @{ Operation = 'RemoveRuntimeLogin'; Field = 'LifetimeId'; Expected = $fixtureLifetime }
    )) {
    $requests = Get-Requests -Case $cleanup -Operation $check.Operation
    $values = @($requests | ForEach-Object { [string](Get-FieldValue -Object $_ -Name $check.Field) })
    Assert-True -Name ('LC-22 Cleanup: {0} request {1} is {2}' -f $check.Operation, $check.Field, $check.Expected) `
        -Condition ($requests.Count -ge 1 -and @($values | Where-Object { $_ -cne $check.Expected }).Count -eq 0) `
        -Detail ('requests {0}, values <{1}> | {2}' -f $requests.Count, ($values -join ','), $cleanupDetail)
}
$cleanupViolations = Get-RequestBoundaryViolations -Case $cleanup
Assert-True -Name 'LC-22 Cleanup: requests name only the plan database/login, no sa and no plaintext secret field' `
    -Condition ($cleanupOk -and $cleanupViolations.Count -eq 0) `
    -Detail (($cleanupViolations -join '; ') + ' | ' + $cleanupDetail)
$cleanupStates = Get-Requests -Case $cleanup -Operation 'WriteLifecycleState'
$cleanupFinal = if ($cleanupStates.Count -gt 0) { $cleanupStates[$cleanupStates.Count - 1] } else { $null }
Assert-EqualInCase -Name 'LC-22 Cleanup: final lifecycle state Status Cleaned' -Expected 'Cleaned' `
    -Actual (Get-FieldValue -Object $cleanupFinal -Name 'Status') -Context $cleanupDetail

# Someone changed the instance after Apply: Cleanup must not overwrite it.
$changedPort = New-AppliedResponses
$changedPort.ReadInstanceConfiguration.TcpPort = 15000
Test-StepRefusedBeforeChange -Name 'LC-23 TCP port changed by someone else' -Code 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' `
    -PlanName 'Approved' -Step 'Cleanup' -Responses $changedPort
$changedForce = New-AppliedResponses
$changedForce.ReadInstanceConfiguration.ForceEncryption = $true
Test-StepRefusedBeforeChange -Name 'LC-24 ForceEncryption changed by someone else' `
    -Code 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' -PlanName 'Approved' -Step 'Cleanup' -Responses $changedForce
$foreignOwner = New-AppliedResponses
$foreignOwner.ReadTargetDatabase.OwnedLifetimeId = $otherLifetime
Test-StepRefusedBeforeChange -Name 'LC-25 database owned by another lifetime' -Code 'WINDOWS_SQL_TARGET_NOT_OWNED' `
    -PlanName 'Approved' -Step 'Cleanup' -Responses $foreignOwner
$noState = New-AppliedResponses
$noState.ReadLifecycleState = $null
Test-StepRefusedBeforeChange -Name 'LC-26 no lifecycle state' -Code 'WINDOWS_SQL_CLEANUP_STATE_MISSING' `
    -PlanName 'Approved' -Step 'Cleanup' -Responses $noState
$otherState = New-AppliedResponses
$otherState.ReadLifecycleState.LifetimeId = $otherLifetime
Test-StepRefusedBeforeChange -Name 'LC-27 lifecycle state of another lifetime' -Code 'WINDOWS_SQL_TARGET_NOT_OWNED' `
    -PlanName 'Approved' -Step 'Cleanup' -Responses $otherState
Test-StepRefusedBeforeChange -Name 'LC-28 restore needs a restart without RestartApproved' `
    -Code 'WINDOWS_SQL_RESTART_NOT_APPROVED' -PlanName 'NoRestart' -Step 'Cleanup' -Responses (New-AppliedResponses)

# ==== LC-29 an unknown step reaches no adapter =============================================================
$unknownStep = Invoke-StepCase -PlanName 'Approved' -Step 'Drop' -Responses (New-FreshResponses)
$refusedStep = $unknownStep.Error -ceq 'WINDOWS_SQL_STEP_INVALID' -or
[string]$unknownStep.ErrorType -match 'ParameterBinding'
Assert-True -Name 'LC-29 unknown step Drop: refused (step code or parameter validation) before any adapter operation' `
    -Condition ($refusedStep -and $unknownStep.Operations.Count -eq 0) -Detail (Get-CaseDetail -Case $unknownStep)

# ==== Public signature, decision boundary and command-line surface =========================================
# These structural checks support the behaviour cases above; they never stand in for them.
$commonParameters = @([Management.Automation.Cmdlet]::CommonParameters) +
@([Management.Automation.Cmdlet]::OptionalCommonParameters)
$stepParameterError = Get-ThrownMessage -Action {
    $command = Get-Command -Name 'Invoke-WindowsSqlStep' -CommandType Function -ErrorAction Stop
    $publicNames = @($command.Parameters.Keys | Where-Object { $_ -notin $commonParameters })
    $script:StepParameterNames = Join-SortedNames -Names $publicNames
}
$stepParameterActual = if ($null -eq $stepParameterError) { $script:StepParameterNames } else {
    'unavailable: ' + $stepParameterError
}
Assert-Equal -Name 'STRUCT-04 Invoke-WindowsSqlStep exposes exactly Plan, Step and Adapter' `
    -Expected (Join-SortedNames -Names @('Adapter', 'Plan', 'Step')) -Actual $stepParameterActual

$forbiddenCommands = @($externalCommands)
$forbiddenTypePattern = 'ServiceController|Microsoft\.Win32\.Registry|RegistryKey|SqlConnection|SqlCommand|X509Store|' +
'Diagnostics\.Process|ProcessStartInfo|ManagementObject'
$boundaryUses = New-Object 'Collections.Generic.List[string]'
$boundaryError = if (-not [IO.File]::Exists($productPath)) { 'missing product file: ' + $productRelative } else {
    Get-ThrownMessage -Action {
        # Direct uses only: the step reaches services, registry, certificates, SQL and processes through the adapter.
        $tokens = $null
        $parseErrors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($productPath, [ref]$tokens, [ref]$parseErrors)
        $definition = $ast.Find({
                param($node)
                $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
                $node.Name -ceq 'Invoke-WindowsSqlStep'
            }, $true)
        if ($null -eq $definition) { throw 'function not defined: Invoke-WindowsSqlStep' }
        foreach ($node in $definition.Body.FindAll({ param($item) $true }, $true)) {
            $isCommand = $node -is [Management.Automation.Language.CommandAst]
            if ($isCommand -and $node.GetCommandName() -in $forbiddenCommands) {
                $boundaryUses.Add('command ' + $node.GetCommandName())
            }
            elseif (($node -is [Management.Automation.Language.TypeExpressionAst] -or
                    $node -is [Management.Automation.Language.TypeConstraintAst]) -and
                $node.TypeName.FullName -match $forbiddenTypePattern) {
                $boundaryUses.Add('type ' + $node.TypeName.FullName)
            }
            elseif ($node -is [Management.Automation.Language.StringConstantExpressionAst] -and
                $node.Value -match '^(?i)(HKLM:|HKCU:|Registry::|Cert:)') {
                $boundaryUses.Add('path ' + $node.Value)
            }
        }
    }
}
Assert-True -Name 'STRUCT-05 Invoke-WindowsSqlStep body makes no direct service/registry/certificate/SQL/process call' `
    -Condition ($null -eq $boundaryError -and $boundaryUses.Count -eq 0) `
    -Detail $(if ($null -ne $boundaryError) { $boundaryError } else { $boundaryUses -join '; ' })

# The command line takes a plan path and hash and a step; it never accepts an adapter, script, SQL, observation,
# identity override or secret, so no caller can inject behaviour or bypass the machine check.
$forbiddenParameterPattern = '^(?i)(Adapter|Script|ScriptBlock|Sql|SqlText|Query|Command|Observation|' +
'Machine|MachineName|SkipIdentity|SkipIdentityCheck|Force|AllowIdentityMismatch|TrustServerCertificate|Encrypt)$'
$forbiddenParameterTypes = '(?i)scriptblock|pscredential|securestring'
$entryFindings = New-Object 'Collections.Generic.List[string]'
$entryError = if (-not [IO.File]::Exists($entryPath)) { 'missing entry file: ' + $entryRelative } else {
    Get-ThrownMessage -Action {
        $tokens = $null
        $parseErrors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($entryPath, [ref]$tokens, [ref]$parseErrors)
        if ($parseErrors.Count -gt 0) { throw ('entry does not parse: ' + $parseErrors[0].Message) }
        if ($null -eq $ast.ParamBlock) { throw 'entry has no param block' }
        foreach ($parameter in $ast.ParamBlock.Parameters) {
            $parameterName = $parameter.Name.VariablePath.UserPath
            $secretShaped = $parameterName -match '(?i)password|secret|credential'
            if ($parameterName -match $forbiddenParameterPattern -or $secretShaped) {
                $entryFindings.Add('parameter ' + $parameterName)
            }
            foreach ($attribute in $parameter.Attributes) {
                if ($attribute -is [Management.Automation.Language.TypeConstraintAst] -and
                    $attribute.TypeName.FullName -match $forbiddenParameterTypes) {
                    $entryFindings.Add('parameter {0} typed {1}' -f $parameterName, $attribute.TypeName.FullName)
                }
            }
        }
    }
}
Assert-True -Name 'STRUCT-06 the entry takes no adapter/script/SQL/observation/identity-override/secret parameter' `
    -Condition ($null -eq $entryError -and $entryFindings.Count -eq 0) `
    -Detail $(if ($null -ne $entryError) { $entryError } else { $entryFindings -join '; ' })

Complete-TestSuite
