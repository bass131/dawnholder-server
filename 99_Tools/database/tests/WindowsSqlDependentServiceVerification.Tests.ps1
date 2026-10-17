[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    # Optional absolute windows-sql folder of an isolated same-shaped tree (HEAD or mutant product runs).
    [string]$ProductRoot = ''
)
# Independent verification of the dependent-service boundary of the exact SQL instance service, for boundaries the
# requirement suite WindowsSqlDependentService.Tests.ps1 leaves open (requirement: lead contract DS-1 v1 from the main
# scope decisions msg_87d420e8d4fb / msg_b5227e44a175):
# - The restart branch's own fresh check: the change lands at the restart controller itself, after the registry
#   guard already read a safe world, so only the check next to Stop can keep Stop from running.
# - Check-to-Stop race: a dependent starts after the last check. Stop must leave the refusal to SCM, never cascade.
# - The window between the Apply observation and the snapshot, snapshot storage failures before/after the file,
#   a failing first journal and a partial Apply: no first write on refusal, recovery state kept otherwise, and a
#   refused rerun or Cleanup never turns into success.
# - Verify and Cleanup observe the dependents too; their public codes are recorded.
# - A null list, a null entry and a failing controller refresh with provider text; that text never becomes public.
# - Every instance controller the product opens and every dependent it enumerates is disposed.
# The real path runs: Invoke-WindowsSqlStep -> observation/Apply/Verify/Cleanup core -> adapter InvokeOperation ->
# native dispatcher -> instance helpers including the real RestartService branch, journal and private store.
# Only protected facts and effects are replaced below those helpers: ServiceController (type accelerator), the
# Win32_Service CIM read, registry keys, TCP listeners, firewall rules, the WSL child and TLS verification, the SQL
# connection, database/login reads and mutations, executor/certificate gates and the fixed instance identity read.
# Each fake records its call and returns the simulated state; none recomputes a product decision. Expected outcomes
# are requirement constants. A refusal counts only where the accepted safe control of the same step followed the
# same recorded path up to the point where the dependent input differs.
# Nothing here opens SQL, the registry, a service, the firewall, a certificate store or WSL.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-dependent-service-verification'

# The -ProductRoot parameter is this same script-scope variable; empty means the checked-out windows-sql folder.
if (-not $ProductRoot) { $ProductRoot = Join-Path $script:ToolRoot 'windows-sql' }
elseif (-not [IO.Path]::IsPathRooted($ProductRoot)) { throw 'ProductRoot must be an explicit absolute path.' }
$script:ProductRoot = [IO.Path]::GetFullPath($ProductRoot)
if (-not [IO.File]::Exists((Join-Path $script:ProductRoot 'WindowsSql.Instance.ps1'))) {
    throw ('ProductRoot has no WindowsSql.Instance.ps1: ' + $script:ProductRoot)
}
$script:FixtureMachine = 'LAPTOP-FIXTURE'
$script:FixtureInstance = 'DHFIXTURE'
$script:FixtureInstanceId = 'MSSQL17.DHFIXTURE'
$script:FixtureVersion = '17.0.1000.7'
$script:FixturePort = 15435
$script:FixtureThumbprint = 'EF' * 20
$script:InstanceService = 'MSSQL$' + $script:FixtureInstance
$script:AgentService = 'SQLAgent$' + $script:FixtureInstance
$script:PublicCodePattern = '^WINDOWS_SQL_[A-Z0-9_]+$'
# Provider-like text with a secret marker; it may appear only inside this suite's own fakes.
$script:SecretMarker = 'DSV-SECRET-7f3c'
$script:ProviderText = 'Provider failure: Server=lpc:LAPTOP-FIXTURE;Password=' + $script:SecretMarker
if ([Environment]::MachineName -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run.'
}
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
try { $script:ExecutorSid = $currentIdentity.User.Value } finally { $currentIdentity.Dispose() }

# ---- Fail-closed guards: reaching any of these real commands is a test failure, never a side effect.
$script:ForbiddenCalls = [Collections.Generic.List[string]]::new()
foreach ($name in @('Get-Service', 'Restart-Service', 'Stop-Service', 'Start-Service', 'Set-Service',
        'Suspend-Service', 'Resume-Service', 'New-Service', 'Remove-Service', 'Invoke-CimMethod', 'Set-CimInstance',
        'Remove-CimInstance', 'New-CimSession', 'Get-CimAssociatedInstance', 'Get-WmiObject', 'sc', 'sc.exe', 'net',
        'net.exe', 'Start-Process', 'Invoke-Sqlcmd', 'sqlcmd', 'reg', 'reg.exe', 'netsh', 'wsl', 'wsl.exe',
        'Get-ItemProperty', 'Set-ItemProperty', 'New-ItemProperty', 'Remove-ItemProperty', 'New-NetFirewallRule',
        'Set-NetFirewallRule', 'Remove-NetFirewallRule', 'New-SelfSignedCertificate', 'Import-PfxCertificate',
        'certutil', 'docker')) {
    Set-Item -Path ('Function:script:' + $name) -Value ([scriptblock]::Create(
            "`$script:ForbiddenCalls.Add('$name'); throw 'DEPENDENT_VERIFY_FORBIDDEN_$name'"))
}

function Write-JsonFile {
    param([string]$Path, $Value)
    $text = $Value | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($Path, $text + "`n", [Text.UTF8Encoding]::new($false))
}

function Get-FileSha256 {
    param([string]$Path)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($Path)))
}

function Get-PropertyValue {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

# =====================================================================================================
# 1. Service world. Dependents hold ServiceControllerStatus/ServiceStartMode names. ListMode 'Null' returns no list,
#    'NullEntry' puts a null entry before the dependents. RefreshFault makes the instance controller refresh throw.
# =====================================================================================================
Add-Type -AssemblyName System.ServiceProcess.ServiceController
# Counts across the whole suite: every product-opened instance controller and every enumerated dependent.
$script:ServiceCounts = @{ InstanceOpened = 0; InstanceDisposed = 0; DependentsCreated = 0; DependentsDisposed = 0 }
$script:AllEvents = [Collections.Generic.List[string]]::new()
$script:AllViolations = [Collections.Generic.List[string]]::new()
$script:TlsRequests = [Collections.Generic.List[string]]::new()

function New-FakeDependent {
    param([string]$Name, $Status, $StartType)
    return @{ Name = $Name; Status = $Status; StartType = $StartType }
}

function Add-WorldEvent {
    param([string]$Kind, [string]$Detail)
    $text = $Kind + ':' + $Detail
    $script:World.Events.Add($text)
    $script:AllEvents.Add($text)
}

function Add-WorldViolation {
    param([string]$Text)
    $script:World.Violations.Add($Text)
    $script:AllViolations.Add($Text)
}

function ConvertTo-FakeStatus {
    param($Value)
    if ($null -eq $Value) { return $null }
    return [System.ServiceProcess.ServiceControllerStatus]$Value
}

function ConvertTo-FakeStartMode {
    param($Value)
    if ($null -eq $Value) { return $null }
    return [System.ServiceProcess.ServiceStartMode]$Value
}

function Get-FakeDependentList {
    # One enumeration per controller until its Refresh, like .NET DependentServices.
    Add-WorldEvent -Kind 'DependentRead' -Detail 'list'
    if ($script:World.ListMode -ceq 'Null') { return $null }
    $items = [Collections.Generic.List[object]]::new()
    if ($script:World.ListMode -ceq 'NullEntry') { $items.Add($null) }
    foreach ($entry in $script:World.Dependents) {
        $items.Add([VerifyServiceController]::new($entry.Name, (ConvertTo-FakeStatus -Value $entry.Status),
                (ConvertTo-FakeStartMode -Value $entry.StartType)))
    }
    return , ([object[]]$items.ToArray())
}

# Replaces System.ServiceProcess.ServiceController for the product's own type literal (bound in section 2).
class VerifyServiceController {
    static [hashtable]$Hooks
    [string]$ServiceName
    [string]$Name
    [string]$DisplayName
    [object]$Status
    [object]$StartType
    hidden [bool]$IsDependent
    hidden [bool]$DependentListRead
    hidden [object]$DependentCache

    # The product's constructor form opens the named service; only the exact instance service is allowed.
    VerifyServiceController([string]$name) {
        $this.ServiceName = $name
        $this.Name = $name
        $this.DisplayName = $name
        & ([VerifyServiceController]::Hooks['Open']) $this
    }

    # Dependent entries created by the fake list itself; the product never calls this form.
    VerifyServiceController([string]$name, [object]$status, [object]$startType) {
        $this.ServiceName = $name
        $this.Name = $name
        $this.DisplayName = $name
        $this.Status = $status
        $this.StartType = $startType
        $this.IsDependent = $true
        & ([VerifyServiceController]::Hooks['Created']) $this
    }

    [void] Stop() { & ([VerifyServiceController]::Hooks['Stop']) $this $true 'Stop()' }
    [void] Stop([bool]$stopDependentServices) {
        $form = 'Stop(' + $stopDependentServices + ')'
        & ([VerifyServiceController]::Hooks['Stop']) $this $stopDependentServices $form
    }
    [void] Start() { & ([VerifyServiceController]::Hooks['Start']) $this }
    [void] WaitForStatus([object]$desired) { & ([VerifyServiceController]::Hooks['Wait']) $this $desired }
    [void] WaitForStatus([object]$desired, [TimeSpan]$timeout) {
        & ([VerifyServiceController]::Hooks['Wait']) $this $desired
    }
    [void] Refresh() { & ([VerifyServiceController]::Hooks['Refresh']) $this }
    [void] Close() { }
    [void] Dispose() { & ([VerifyServiceController]::Hooks['Dispose']) $this }
}

$script:DependentServicesGetter = {
    if (-not $this.DependentListRead) {
        $this.DependentCache = Get-FakeDependentList
        $this.DependentListRead = $true
    }
    if ($null -eq $this.DependentCache) { return $null }
    return , $this.DependentCache
}

[VerifyServiceController]::Hooks = @{
    Open = {
        param($Controller)
        Add-WorldEvent -Kind 'ControllerOpen' -Detail $Controller.ServiceName
        if ($Controller.ServiceName -cne $script:InstanceService) {
            Add-WorldViolation -Text ('controller for another service: ' + $Controller.ServiceName)
            throw 'FAKE_CONTROLLER_UNEXPECTED'
        }
        $script:ServiceCounts.InstanceOpened++
        $Controller.Status = ConvertTo-FakeStatus -Value $script:World.InstanceState
        $Controller.StartType = ConvertTo-FakeStartMode -Value 'Automatic'
        Add-Member -InputObject $Controller -MemberType ScriptProperty -Name DependentServices `
            -Value $script:DependentServicesGetter
        # A change armed for the Nth instance controller open after its arming point (another actor's edit).
        $openChange = $script:World.OpenChange
        if ($null -ne $openChange) {
            $openChange.Remaining--
            if ($openChange.Remaining -eq 0) {
                $script:World.OpenChange = $null
                Add-WorldEvent -Kind 'WorldChange' -Detail ('controller-open:' + $openChange.Label)
                & $openChange.Change
            }
        }
    }
    Created = {
        param($Controller)
        $script:ServiceCounts.DependentsCreated++
    }
    Stop = {
        param($Controller, [bool]$StopDependents, [string]$Form)
        if ($Controller.IsDependent) {
            Add-WorldViolation -Text ('dependent service control: Stop ' + $Controller.ServiceName)
            throw 'FAKE_DEPENDENT_CONTROL'
        }
        # Another actor's start in the gap after the product's last check, just before SCM handles the stop.
        $beforeStop = $script:World.BeforeStop
        if ($null -ne $beforeStop) {
            $script:World.BeforeStop = $null
            & $beforeStop
        }
        Add-WorldEvent -Kind 'Stop' -Detail $Form
        $active = @($script:World.Dependents | Where-Object { $_.Status -cne 'Stopped' })
        if ($active.Count -gt 0) {
            # SCM refuses to stop a service with active dependents when it may not stop them (error 1051).
            if (-not $StopDependents) {
                Add-WorldEvent -Kind 'StopRefused' -Detail 'dependent-services-running'
                throw 'FAKE_SCM_DEPENDENT_SERVICES_RUNNING'
            }
            foreach ($entry in $active) {
                # .NET Stop() first stops every active dependent: the forced stop the requirement forbids.
                Add-WorldEvent -Kind 'ImplicitDependentStop' -Detail $entry.Name
                $entry.Status = 'Stopped'
            }
        }
        $script:World.InstanceState = 'Stopped'
    }
    Start = {
        param($Controller)
        if ($Controller.IsDependent) {
            Add-WorldViolation -Text ('dependent service control: Start ' + $Controller.ServiceName)
            throw 'FAKE_DEPENDENT_CONTROL'
        }
        Add-WorldEvent -Kind 'Start' -Detail $Controller.ServiceName
        $script:World.InstanceState = 'Running'
    }
    Wait = {
        param($Controller, $Desired)
        if ($Controller.IsDependent) {
            Add-WorldViolation -Text ('dependent service control: Wait ' + $Controller.ServiceName)
            throw 'FAKE_DEPENDENT_CONTROL'
        }
        Add-WorldEvent -Kind 'Wait' -Detail ([string]$Desired)
        if ([string]$Desired -cne $script:World.InstanceState) { throw 'FAKE_SERVICE_WAIT_TIMEOUT' }
        $Controller.Status = ConvertTo-FakeStatus -Value $script:World.InstanceState
    }
    Refresh = {
        param($Controller)
        if (-not $Controller.IsDependent) {
            Add-WorldEvent -Kind 'ControllerRefresh' -Detail $Controller.ServiceName
            if ($script:World.RefreshFault) { throw $script:ProviderText }
            $Controller.DependentListRead = $false
            $Controller.DependentCache = $null
            $Controller.Status = ConvertTo-FakeStatus -Value $script:World.InstanceState
            return
        }
        Add-WorldEvent -Kind 'DependentRead' -Detail ('refresh:' + $Controller.ServiceName)
        $entries = @($script:World.Dependents | Where-Object { $_.Name -ceq $Controller.ServiceName })
        if ($entries.Count -ne 1) { throw 'FAKE_SERVICE_MISSING' }
        $Controller.Status = ConvertTo-FakeStatus -Value $entries[0].Status
        $Controller.StartType = ConvertTo-FakeStartMode -Value $entries[0].StartType
    }
    Dispose = {
        param($Controller)
        if ($Controller.IsDependent) { $script:ServiceCounts.DependentsDisposed++ }
        else { $script:ServiceCounts.InstanceDisposed++ }
    }
}

function Get-CimInstance {
    [CmdletBinding()]
    param([string]$ClassName, [string]$Filter, [string]$Query, [string]$Namespace)
    if ($ClassName -ceq 'Win32_Service' -and $Filter -ceq ("Name='" + $script:InstanceService + "'")) {
        return [pscustomobject]@{
            Name = $script:InstanceService
            StartName = 'NT Service\' + $script:InstanceService
            State = $script:World.InstanceState
            StartMode = 'Auto'
            ProcessId = 4343
        }
    }
    Add-WorldViolation -Text ('CIM ' + $ClassName + ' ' + $Filter + ' ' + $Query)
    throw 'FAKE_CIM_UNEXPECTED'
}

function Reset-FakeWorld {
    param([object[]]$Dependents = @(), [string]$ListMode = 'List', [bool]$RefreshFault = $false)
    $script:Registry = @{
        '\MSSQLServer' = @{ LoginMode = @{ Value = [int]1; Kind = 'DWord' } }
        '\MSSQLServer\SuperSocketNetLib' = @{
            ForceEncryption = @{ Value = [int]1; Kind = 'DWord' }
            Certificate = @{ Value = ''; Kind = 'String' }
        }
        '\MSSQLServer\SuperSocketNetLib\Tcp' = @{
            Enabled = @{ Value = [int]0; Kind = 'DWord' }
            ListenOnAllIPs = @{ Value = [int]1; Kind = 'DWord' }
        }
        '\MSSQLServer\SuperSocketNetLib\Tcp\IPAll' = @{
            TcpPort = @{ Value = ''; Kind = 'String' }
            TcpDynamicPorts = @{ Value = '0'; Kind = 'String' }
        }
    }
    $list = [Collections.Generic.List[hashtable]]::new()
    foreach ($entry in @($Dependents)) { $list.Add($entry.Clone()) }
    $script:World = @{
        InstanceState = 'Running'
        Dependents = $list
        ListMode = $ListMode
        RefreshFault = $RefreshFault
        Database = @{ Exists = $false; Owner = $null; SchemaVersion = $null }
        Login = @{ Exists = $false; Owner = $null }
        OpenChange = $null
        BeforeStop = $null
        OperationHook = $null
        WriteHook = $null
        WriteFault = $null
        Events = [Collections.Generic.List[string]]::new()
        Violations = [Collections.Generic.List[string]]::new()
    }
}

function Set-AgentState {
    param($Status, $StartType)
    # Another actor's edit of the existing agent entry between steps; recorded like every world change.
    Add-WorldEvent -Kind 'WorldChange' -Detail ('agent:' + [string]$Status + '+' + [string]$StartType)
    $script:World.Dependents[0].Status = $Status
    $script:World.Dependents[0].StartType = $StartType
}

# =====================================================================================================
# 2. Guards before any product code loads: the type literal must reach the fake, and no product source may bypass
#    the fakes. If either fails, no flow runs (not executed, design needed) and no real service is touched.
# =====================================================================================================
Add-TestResult -Name 'DSV-GUARD-00 PowerShell version' -Outcome OBSERVED -Detail $PSVersionTable.PSVersion.ToString()
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
$controllerTypeNames = @('ServiceProcess.ServiceController', 'System.ServiceProcess.ServiceController')
foreach ($typeName in $controllerTypeNames) { $accelerators::Add($typeName, [VerifyServiceController]) }
$resolvedNames = @(foreach ($typeName in $controllerTypeNames) {
        (& ([scriptblock]::Create('[' + $typeName + ']'))).FullName
    })
$literalFaked = @($resolvedNames | Where-Object { $_ -cne 'VerifyServiceController' }).Count -eq 0
Assert-True -Name 'DSV-GUARD-01 ServiceController type literals resolve to the fake' -Condition $literalFaked `
    -Detail ($resolvedNames -join ',')
$bypassPattern = '(?i)(\b(CimCmdlets|Microsoft\.PowerShell\.Management)\\[A-Za-z]+-|\[wmi(searcher|class)?\]|' +
'ManagementObject|CimSession\]::|Get-Service\b)'
$bypassHits = @(Get-ChildItem -LiteralPath $script:ProductRoot -Recurse -File -Filter '*.ps1' | Where-Object {
        [IO.File]::ReadAllText($_.FullName) -match $bypassPattern
    } | ForEach-Object Name)
Assert-True -Name 'DSV-GUARD-02 product sources have no module-qualified, WMI or Get-Service path around the fakes' `
    -Condition ($bypassHits.Count -eq 0) -Detail ($bypassHits -join ',')
if (-not ($literalFaked -and $bypassHits.Count -eq 0)) {
    Add-TestResult -Name 'DSV-FLOW-00 flows not executed: service observation cannot be faked safely' -Outcome FAIL
    Complete-TestSuite
}

# =====================================================================================================
# 3. Product definitions and the remaining protected I/O fakes (defined after the product, so they replace it).
# =====================================================================================================
. (Join-Path $script:ProductRoot 'WindowsSql.Common.ps1')
. (Join-Path $script:ProductRoot 'WindowsSql.Adapter.ps1')

function New-FakeRegistryKey {
    param([string]$Suffix, [bool]$Writable)
    $key = [pscustomobject]@{ Suffix = $Suffix; Writable = $Writable }
    $key | Add-Member -MemberType ScriptMethod -Name GetValue -Value {
        param($Name, $Default, $Options)
        $entry = $script:Registry[$this.Suffix][$Name]
        if ($null -eq $entry) { return $Default }
        return $entry.Value
    }
    $key | Add-Member -MemberType ScriptMethod -Name GetValueKind -Value {
        param($Name)
        $entry = $script:Registry[$this.Suffix][$Name]
        if ($null -eq $entry) { throw 'FAKE_REGISTRY_VALUE_MISSING' }
        return [Microsoft.Win32.RegistryValueKind]$entry.Kind
    }
    $key | Add-Member -MemberType ScriptMethod -Name SetValue -Value {
        param($Name, $Value, $Kind)
        if (-not $this.Writable) { throw 'FAKE_REGISTRY_READ_ONLY' }
        Add-WorldEvent -Kind 'RegistryWrite' -Detail $Name
        $stored = if ([string]$Kind -ceq 'DWord') { [int]$Value } else { [string]$Value }
        $script:Registry[$this.Suffix][$Name] = @{ Value = $stored; Kind = [string]$Kind }
    }
    $key | Add-Member -MemberType ScriptMethod -Name Flush -Value { }
    $key | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
    return $key
}

function Open-WindowsSqlRegistryKey {
    param($Adapter, [string]$Suffix, [switch]$Writable)
    if ($Adapter.Plan.InstanceId -cne $script:FixtureInstanceId) { throw 'FAKE_REGISTRY_INSTANCE_UNEXPECTED' }
    if (-not $script:Registry.ContainsKey($Suffix)) { throw 'WINDOWS_SQL_INSTANCE_MISSING' }
    return New-FakeRegistryKey -Suffix $Suffix -Writable $Writable.IsPresent
}

function Get-NetTCPConnection {
    [CmdletBinding()]
    param([string]$State, [int]$LocalPort)
    # No listener on the fixture port: the port is free for the instance.
}

function Get-NetFirewallRule {
    [CmdletBinding()]
    param([string]$PolicyStore, [string]$Name)
    # The mirrored fixture has no owned firewall rule.
}

function Invoke-WindowsSqlChild {
    param([string]$FileName, [string[]]$Arguments, [string]$InputText = '', [hashtable]$Environment = @{},
        [int]$TimeoutSeconds = 45)
    $a = @($Arguments)
    if ($FileName -ceq 'C:\Windows\System32\wsl.exe' -and $a.Count -eq 5 -and $a[3] -ceq 'wslinfo') {
        return 'mirrored'
    }
    Add-WorldViolation -Text ('unexpected child ' + $FileName + ' ' + ($a -join ' '))
    throw 'FAKE_CHILD_UNEXPECTED'
}

function Open-WindowsSqlAdminConnection {
    param($Adapter, [string]$Database = 'master')
    Add-WorldEvent -Kind 'SqlOpen' -Detail $Database
    if ($Database -cne 'master') {
        Add-WorldViolation -Text ('connection to ' + $Database)
        throw 'FAKE_SQL_DATABASE_UNEXPECTED'
    }
    $connection = [pscustomobject]@{ Database = $Database }
    $connection | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
    return $connection
}

function Invoke-DbNonQuery {
    param($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null)
    if ($Sql.Contains('sp_getapplock')) { return -1 }
    Add-WorldViolation -Text ('unexpected SQL: ' + ($Sql.Trim() -split "`n")[0])
    throw 'FAKE_SQL_UNEXPECTED'
}

function Invoke-DbScalar {
    param($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null)
    Add-WorldViolation -Text ('unexpected SQL: ' + ($Sql.Trim() -split "`n")[0])
    throw 'FAKE_SQL_UNEXPECTED'
}

# SQL reads of the target database/login: the simulated server state, in the product's observation shape.
function Read-WindowsSqlDatabase {
    param($Adapter)
    Add-WorldEvent -Kind 'SqlRead' -Detail 'Database'
    $database = $script:World.Database
    return [pscustomobject]@{
        Name = $Adapter.Plan.Database
        Exists = $database.Exists
        OwnedLifetimeId = $database.Owner
        SchemaVersion = $database.SchemaVersion
    }
}

function Read-WindowsSqlLogin {
    param($Adapter)
    Add-WorldEvent -Kind 'SqlRead' -Detail 'Login'
    $login = $script:World.Login
    return [pscustomobject]@{
        Name = $Adapter.Plan.RuntimeLogin
        Exists = $login.Exists
        OwnedLifetimeId = $login.Owner
    }
}

# Database creation, migrations and login are covered by WindowsSqlAdapterBoundary.Tests.ps1; here they are effects.
function Invoke-WindowsSqlDatabaseMutation {
    param($Adapter, [string]$Operation)
    Add-WorldEvent -Kind 'DatabaseMutation' -Detail $Operation
    $lifetime = $Adapter.Plan.LifetimeId
    switch -CaseSensitive ($Operation) {
        'CreateDatabase' { $script:World.Database = @{ Exists = $true; Owner = $lifetime; SchemaVersion = 0 } }
        'ApplyMigrations' { $script:World.Database.SchemaVersion = 4 }
        'CreateRuntimeLogin' { $script:World.Login = @{ Exists = $true; Owner = $lifetime } }
        'RemoveRuntimeLogin' { $script:World.Login = @{ Exists = $false; Owner = $null } }
        'RemoveDatabase' { $script:World.Database = @{ Exists = $false; Owner = $null; SchemaVersion = $null } }
    }
}

# OS identity facts (machine/SID/admin) cannot be produced for the fixture; the gate is counted only.
function Assert-WindowsSqlExecutor {
    param($Plan, $Runtime)
    Add-WorldEvent -Kind 'ExecutorGate' -Detail $Plan.MachineName
}

function Assert-WindowsSqlCertificate {
    param($Adapter)
    Add-WorldEvent -Kind 'CertificateGate' -Detail $Adapter.Plan.CertificateThumbprint
}

# Instance map/Setup registry are read through OpenBaseKey directly; the fixture identity replaces that read.
function Read-WindowsSqlInstanceIdentity {
    param($Adapter)
    [void](Get-WindowsSqlMaster -Adapter $Adapter)
    Assert-WindowsSqlCertificate -Adapter $Adapter
    return [pscustomobject]@{
        InstanceName = $script:FixtureInstance
        InstanceId = $script:FixtureInstanceId
        ProductVersion = $script:FixtureVersion
    }
}

# The WSL SqlClient run and the catalog query; their requests are recorded by the step adapter.
function Invoke-WindowsSqlWslVerification {
    param($Adapter)
    Add-WorldEvent -Kind 'TlsVerify' -Detail $Adapter.Plan.Database
    return [pscustomobject]@{ Encrypted = $true }
}

function Assert-WindowsSqlCatalog {
    param($Adapter)
    Add-WorldEvent -Kind 'CatalogVerify' -Detail $Adapter.Plan.Database
}

$script:RealWritePrivateJson = ${function:Write-WindowsSqlPrivateJson}
function Write-WindowsSqlPrivateJson {
    param($Adapter, [string]$Name, $Value)
    $status = [string](Get-WindowsSqlField -Object $Value -Name 'Status')
    $pending = [string](Get-WindowsSqlField -Object $Value -Name 'PendingOperation')
    # A storage fault of this one write, before the file exists or after it was written (a failed final check).
    $fault = $script:World.WriteFault
    $faultHit = $null -ne $fault -and $Name -ceq $fault.Name -and $pending -ceq $fault.Pending
    if ($faultHit) { $script:World.WriteFault = $null }
    if ($faultHit -and $fault.When -ceq 'Before') {
        Add-WorldEvent -Kind 'WriteFault' -Detail ('before:' + $Name)
        throw $script:ProviderText
    }
    & $script:RealWritePrivateJson -Adapter $Adapter -Name $Name -Value $Value
    # Recorded once the real private store finished the write.
    Add-WorldEvent -Kind 'PrivateWrite' -Detail ($Name + '/' + $status + '/' + $pending)
    if ($faultHit) {
        Add-WorldEvent -Kind 'WriteFault' -Detail ('after:' + $Name)
        throw $script:ProviderText
    }
    $hook = $script:World.WriteHook
    if ($null -ne $hook -and $Name -ceq $hook.Name -and $pending -ceq $hook.Pending) {
        $script:World.WriteHook = $null
        & $hook.Action
    }
}

# =====================================================================================================
# 4. Scenario, step capture and assertion helpers.
# =====================================================================================================
function New-VerifyScenario {
    param([string]$Id, [object[]]$Dependents = @(), [string]$ListMode = 'List', [bool]$RefreshFault = $false)
    Reset-FakeWorld -Dependents $Dependents -ListMode $ListMode -RefreshFault $RefreshFault
    $lifetime = [Guid]::NewGuid().ToString('N')
    $caseRoot = Join-Path $script:SuiteRoot ('case-' + $Id)
    $source = Join-Path $caseRoot 'source'
    [void][IO.Directory]::CreateDirectory((Join-Path $source '.backups\evidence'))
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    New-WindowsSqlPrivateDirectory -Path $private -Sid $script:ExecutorSid
    $accountId = [Guid]::NewGuid().ToString('D')
    $characterId = [Guid]::NewGuid().ToString('D')
    Write-JsonFile -Path (Join-Path $source 'profile.json') -Value ([ordered]@{
            persistedAccountName = 'fixture_user'
            database = 'Dawnholder_DependentVerify'
            serverEndpoint = ('tcp:localhost,' + $script:FixturePort)
            slotId = 1
            accountId = $accountId
            characterId = $characterId
        })
    [IO.File]::WriteAllText((Join-Path $source 'public-ca.pem'),
        "dependent-service verification public fixture, not a certificate`n")
    Write-JsonFile -Path (Join-Path $source 'runtime.json') -Value ([ordered]@{
            Version = 1
            LifetimeId = $lifetime
            ExecutorSid = $script:ExecutorSid
            ServiceAccount = ('NT Service\' + $script:InstanceService)
            EndpointAddress = '127.0.0.1'
            TlsServerName = 'localhost'
            NetworkMode = 'Mirrored'
            WslSubnet = $null
            WslDistribution = 'Ubuntu'
            AccountId = $accountId
            CharacterId = $characterId
            PublicCaPath = 'public-ca.pem'
            CredentialPath = (Join-Path $private 'runtime.clixml')
            CertificateReceiptHash = ('C' * 64)
            ProfilePath = 'profile.json'
            VerifyHostFiles = @(foreach ($file in @('WindowsSqlVerifyHost.dll', 'WindowsSqlVerifyHost.deps.json',
                        'WindowsSqlVerifyHost.runtimeconfig.json', 'Microsoft.Data.SqlClient.dll')) {
                    [ordered]@{ Path = $file; Sha256 = ('D' * 64) }
                })
        })
    $inputs = @(foreach ($relative in @('runtime.json', 'profile.json', 'public-ca.pem')) {
            [ordered]@{ Path = $relative; Sha256 = (Get-FileSha256 -Path (Join-Path $source $relative)) }
        })
    $planPath = Join-Path $caseRoot 'plan.json'
    Write-JsonFile -Path $planPath -Value ([ordered]@{
            Version = 1
            ExecutionApproved = $true
            ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(3).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'",
                [Globalization.CultureInfo]::InvariantCulture)
            LifetimeId = $lifetime
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            InstanceId = $script:FixtureInstanceId
            ExpectedProductVersion = $script:FixtureVersion
            Port = $script:FixturePort
            Database = 'Dawnholder_DependentVerify'
            RuntimeLogin = 'dh_dependent_verify'
            SchemaVersion = 4
            CertificateThumbprint = $script:FixtureThumbprint
            Encrypt = 'Mandatory'
            TrustServerCertificate = $false
            EnableSa = $false
            SwitchWslNetworkingMode = $false
            ForceEncryption = 'Preserve'
            RestartApproved = $true
            FirewallApproved = $true
            SourceRoot = $source
            EvidenceRoot = (Join-Path $source '.backups\evidence')
            PrivateRoot = $private
            Inputs = $inputs
        })
    $plan = Read-WindowsSqlPlan -Path $planPath -ExpectedHash (Get-FileSha256 -Path $planPath)
    return [pscustomobject]@{
        Id = $Id
        Plan = $plan
        Runtime = (Read-WindowsSqlRuntime -Plan $plan -Relative 'runtime.json')
        Private = $private
        CaseRoot = $caseRoot
    }
}

function New-StepAdapter {
    param($Scenario)
    # One adapter per step, like one CLI process per step: no connection or registry baseline carries over.
    $adapter = [pscustomobject]@{
        Kind = 'DependentServiceVerifyFake'
        Plan = $Scenario.Plan
        Runtime = $Scenario.Runtime
        RuntimePath = 'runtime.json'
        Lease = $null
        DirectoryLease = $null
        Mutex = $null
        MutexOwned = $false
        Master = $null
        Sources = $null
        ExpectedRegistry = $null
        LastTls = $null
    }
    $adapter | Add-Member -MemberType ScriptMethod -Name InvokeOperation -Value {
        param([string]$Operation, $Request)
        Add-WorldEvent -Kind 'Operation' -Detail $Operation
        if ($Operation -cin @('VerifyTlsConnection', 'VerifyTargetCatalog')) {
            $script:TlsRequests.Add(('{0}:Encrypt={1};TSC={2}' -f $Operation, $Request['Encrypt'],
                    $Request['TrustServerCertificate']))
        }
        # Another actor's change just before this operation reaches the native dispatcher.
        $hook = $script:World.OperationHook
        if ($null -ne $hook -and $hook.Operation -ceq $Operation) {
            $script:World.OperationHook = $null
            & $hook.Action
        }
        # Machine identity is the only dispatcher branch replaced: it reads this host's real name.
        if ($Operation -ceq 'ReadMachineIdentity') { return [pscustomobject]@{ MachineName = $script:FixtureMachine } }
        try { Invoke-WindowsSqlNativeOperation -Adapter $this -Operation $Operation -Request $Request }
        catch {
            # The inner cause stays here for the OBSERVED diagnosis; the core decides what becomes public.
            Add-WorldEvent -Kind 'NativeError' -Detail ($Operation + '=' + $_.Exception.Message)
            throw
        }
    }
    return $adapter
}

function Invoke-CaseStep {
    param($Scenario, [string]$Step)
    $adapter = New-StepAdapter -Scenario $Scenario
    $eventStart = $script:World.Events.Count
    $violationStart = $script:World.Violations.Count
    $forbiddenStart = $script:ForbiddenCalls.Count
    $countsBefore = $script:ServiceCounts.Clone()
    $output = @()
    $errorText = $null
    try { $output = @(Invoke-WindowsSqlStep -Plan $Scenario.Plan -Step $Step -Adapter $adapter) }
    catch { $errorText = $_.Exception.Message }
    $countsAfter = $script:ServiceCounts.Clone()
    return [pscustomobject]@{
        Step = $Step
        Result = $(if ($output.Count -gt 0) { $output[$output.Count - 1] } else { $null })
        Error = $errorText
        Events = [object[]]@($script:World.Events | Select-Object -Skip $eventStart)
        Violations = [object[]]@($script:World.Violations | Select-Object -Skip $violationStart)
        Forbidden = [object[]]@($script:ForbiddenCalls | Select-Object -Skip $forbiddenStart)
        InstanceOpened = $countsAfter.InstanceOpened - $countsBefore.InstanceOpened
        InstanceDisposed = $countsAfter.InstanceDisposed - $countsBefore.InstanceDisposed
        DependentsCreated = $countsAfter.DependentsCreated - $countsBefore.DependentsCreated
        DependentsDisposed = $countsAfter.DependentsDisposed - $countsBefore.DependentsDisposed
    }
}

function Get-EventsOfKind {
    param([object[]]$Events, [string]$Kind)
    return , [object[]]@($Events | Where-Object { $_ -clike ($Kind + ':*') })
}

function Get-FirstEventIndex {
    param([object[]]$Events, [string[]]$Kinds, [int]$After = -1)
    for ($index = $After + 1; $index -lt $Events.Count; $index++) {
        foreach ($kind in $Kinds) {
            if ($Events[$index] -clike ($kind + ':*')) { return $index }
        }
    }
    return -1
}

function Get-StepOutcome {
    param($Result)
    # A short public form of the step result: the error code, or the Plan decision and its blockers.
    if ($null -ne $Result.Error) { return $Result.Error }
    $changePlan = Get-PropertyValue -Object $Result.Result -Name 'ChangePlan'
    if ($null -ne $changePlan) { return $changePlan.Decision + '[' + (@($changePlan.Blockers) -join ',') + ']' }
    return [string](Get-PropertyValue -Object $Result.Result -Name 'Status')
}

function Get-NativeErrors {
    param($Result)
    $nativeErrors = Get-EventsOfKind -Events $Result.Events -Kind 'NativeError'
    return ($nativeErrors -join '|')
}

function Test-SamePrefix {
    param([object[]]$Events, [object[]]$ControlEvents, [int]$Count)
    # The case and its accepted control recorded exactly the same first $Count events.
    if ($Count -lt 1 -or $Events.Count -lt $Count -or $ControlEvents.Count -lt $Count) { return $false }
    return ($Events[0..($Count - 1)] -join "`n") -ceq ($ControlEvents[0..($Count - 1)] -join "`n")
}

function Test-PublicError {
    param($Result)
    return $null -ne $Result.Error -and $Result.Error -cmatch $script:PublicCodePattern
}

function Get-WriteEvents {
    param([object[]]$Events)
    return , [object[]]@($Events | Where-Object {
            $_ -clike 'RegistryWrite:*' -or $_ -clike 'PrivateWrite:*' -or $_ -clike 'DatabaseMutation:*' })
}

function Get-ServiceEffects {
    param([object[]]$Events)
    $stops = Get-EventsOfKind -Events $Events -Kind 'Stop'
    $starts = Get-EventsOfKind -Events $Events -Kind 'Start'
    $implicit = Get-EventsOfKind -Events $Events -Kind 'ImplicitDependentStop'
    return [pscustomobject]@{ Stops = $stops; Starts = $starts; Implicit = $implicit }
}

function Get-PrivateFileState {
    param($Scenario, [string]$Name)
    $path = Join-Path $Scenario.Private $Name
    if (-not [IO.File]::Exists($path)) { return $null }
    return [pscustomobject]@{
        Hash = Get-FileSha256 -Path $path
        Value = ([IO.File]::ReadAllText($path) | ConvertFrom-Json)
    }
}

function Assert-NoFirstWrite {
    param([string]$Id, $Scenario, $Result)
    $writes = Get-WriteEvents -Events $Result.Events
    $effects = Get-ServiceEffects -Events $Result.Events
    $snapshot = Get-PrivateFileState -Scenario $Scenario -Name 'snapshot.json'
    $journal = Get-PrivateFileState -Scenario $Scenario -Name 'lifecycle.json'
    $noWrite = $writes.Count -eq 0 -and $null -eq $snapshot -and $null -eq $journal
    $noService = $effects.Stops.Count -eq 0 -and $effects.Starts.Count -eq 0 -and $effects.Implicit.Count -eq 0
    Assert-True -Name ($Id + ' Apply made no first write and never stopped the instance') `
        -Condition ($noWrite -and $noService) -Detail ('writes={0}; snapshot={1}; journal={2}; stops={3}' -f
        ($writes -join ','), ($null -ne $snapshot), ($null -ne $journal), ($effects.Stops -join ','))
}

function Assert-RefusedLikeControl {
    param([string]$Id, [string]$Title, $Result)
    # The case diverges from the accepted control only where the product reads the dependent services: up to and
    # including the first instance controller refresh both recorded the same events.
    $control = $script:Control[$Result.Step]
    $controlRefresh = Get-FirstEventIndex -Events $control.Events -Kinds @('ControllerRefresh')
    $caseRefresh = Get-FirstEventIndex -Events $Result.Events -Kinds @('ControllerRefresh')
    $samePath = $caseRefresh -ge 0 -and $caseRefresh -eq $controlRefresh -and
    (Test-SamePrefix -Events $Result.Events -ControlEvents $control.Events -Count ($caseRefresh + 1))
    $publicError = Test-PublicError -Result $Result
    $clean = $Result.Violations.Count -eq 0 -and $Result.Forbidden.Count -eq 0
    Assert-True -Name ('{0} {1} refuses {2} where the safe control is accepted' -f $Id, $Result.Step, $Title) `
        -Condition ($publicError -and $control.Accepted -and $samePath -and $clean) -Detail (
        'outcome={0}; control={1}/{2}; firstRefresh={3}/{4}; samePath={5}; violations={6}' -f
        (Get-StepOutcome -Result $Result), $control.Accepted, $control.Outcome, $caseRefresh, $controlRefresh,
        $samePath, ((@($Result.Violations) + @($Result.Forbidden)) -join ','))
}

function Assert-ControllersDisposed {
    param([string]$Id, [object[]]$Steps)
    $opened = ($Steps | Measure-Object -Property InstanceOpened -Sum).Sum
    $disposed = ($Steps | Measure-Object -Property InstanceDisposed -Sum).Sum
    $created = ($Steps | Measure-Object -Property DependentsCreated -Sum).Sum
    $dependentsDisposed = ($Steps | Measure-Object -Property DependentsDisposed -Sum).Sum
    Assert-True -Name ($Id + ' every opened instance controller and every enumerated dependent was disposed') `
        -Condition ($opened -gt 0 -and $opened -eq $disposed -and $created -eq $dependentsDisposed) -Detail (
        'instance={0}/{1}; dependents={2}/{3}' -f $opened, $disposed, $created, $dependentsDisposed)
}

$safeAgent = New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled'

# =====================================================================================================
# 5. Safe control: one Stopped+Disabled agent through every step. Each later case is judged against it.
# =====================================================================================================
$controlScenario = New-VerifyScenario -Id 'DSV-CONTROL' -Dependents @($safeAgent)
$controlSteps = [ordered]@{}
foreach ($stepName in @('Preflight', 'Plan', 'Apply', 'Verify', 'Cleanup')) {
    $controlSteps[$stepName] = Invoke-CaseStep -Scenario $controlScenario -Step $stepName
}
$controlPlan = Get-PropertyValue -Object $controlSteps['Plan'].Result -Name 'ChangePlan'
$expectedOutcomes = @{ Preflight = ''; Apply = 'Applied'; Verify = 'Verified'; Cleanup = 'Cleaned' }
$script:Control = @{}
foreach ($stepName in $controlSteps.Keys) {
    $step = $controlSteps[$stepName]
    $outcome = Get-StepOutcome -Result $step
    $accepted = if ($stepName -ceq 'Plan') {
        $null -eq $step.Error -and $null -ne $controlPlan -and $controlPlan.Decision -ceq 'Apply'
    } else { $null -eq $step.Error -and $outcome -ceq $expectedOutcomes[$stepName] }
    $script:Control[$stepName] = [pscustomobject]@{ Accepted = $accepted; Outcome = $outcome; Events = $step.Events }
    Assert-True -Name ('DSV-CONTROL ' + $stepName + ' succeeds with one Stopped+Disabled agent') -Condition $accepted `
        -Detail $outcome
}
$controlApplyEffects = Get-ServiceEffects -Events $controlSteps['Apply'].Events
$controlCleanupEffects = Get-ServiceEffects -Events $controlSteps['Cleanup'].Events
$applySafeStop = ($controlApplyEffects.Stops -join ',') -ceq 'Stop:Stop(False)'
$cleanupSafeStop = ($controlCleanupEffects.Stops -join ',') -ceq 'Stop:Stop(False)'
$restartedOnce = $controlApplyEffects.Starts.Count -eq 1 -and $controlCleanupEffects.Starts.Count -eq 1
$noDependentStop = ($controlApplyEffects.Implicit.Count + $controlCleanupEffects.Implicit.Count) -eq 0
Assert-True -Name 'DSV-CONTROL Apply and Cleanup each restart once through Stop(False) without a dependent stop' `
    -Condition ($applySafeStop -and $cleanupSafeStop -and $restartedOnce -and $noDependentStop) -Detail (
    'apply={0}; cleanup={1}' -f ($controlApplyEffects.Stops -join ','), ($controlCleanupEffects.Stops -join ','))
Assert-ControllersDisposed -Id 'DSV-CONTROL' -Steps @($controlSteps.Values)

# =====================================================================================================
# 6. Restart branch: its own fresh check and the race after it.
# =====================================================================================================
# DSV-01: the registry guard before RestartService opens the first controller and reads a safe world; the change
# lands at the second open, the restart controller itself. Arming is this suite's step and records nothing.
$scenario = New-VerifyScenario -Id 'DSV-01' -Dependents @($safeAgent)
$script:World.WriteHook = @{ Name = 'lifecycle.json'; Pending = 'RestartService'; Action = {
        $script:World.OpenChange = @{ Remaining = 2; Label = 'restart'; Change = {
                $script:World.Dependents[0].Status = 'Running'
                $script:World.Dependents[0].StartType = 'Manual'
            } }
    } }
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$journalIndex = [Array]::IndexOf($apply.Events, 'PrivateWrite:lifecycle.json/Applying/RestartService')
$guardOpen = Get-FirstEventIndex -Events $apply.Events -Kinds @('ControllerOpen') -After $journalIndex
$changeIndex = [Array]::IndexOf($apply.Events, 'WorldChange:controller-open:restart')
$readAfterChange = Get-FirstEventIndex -Events $apply.Events -Kinds @('DependentRead') -After $changeIndex
$failureIndex = Get-FirstEventIndex -Events $apply.Events -Kinds @('NativeError') -After $changeIndex
$openAfterChange = Get-FirstEventIndex -Events $apply.Events -Kinds @('ControllerOpen') -After $changeIndex
$samePath = Test-SamePrefix -Events $apply.Events -ControlEvents $script:Control['Apply'].Events -Count $changeIndex
$effects = Get-ServiceEffects -Events $apply.Events
$journal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$refused = (Test-PublicError -Result $apply) -and $script:Control['Apply'].Accepted
Assert-True -Name 'DSV-01 Apply fails when the agent turns Running+Manual at the restart controller itself' `
    -Condition ($refused -and $samePath -and $apply.Violations.Count -eq 0) -Detail (
    'outcome={0}; change={1}; samePath={2}' -f (Get-StepOutcome -Result $apply), $changeIndex, $samePath)
$guardReadSafe = $guardOpen -ge 0 -and $guardOpen -lt $changeIndex
$restartRead = $readAfterChange -gt $changeIndex -and $failureIndex -gt $readAfterChange
$noLaterOpen = $openAfterChange -lt 0 -or $openAfterChange -gt $failureIndex
Assert-True -Name 'DSV-01 the restart branch read the change on its own controller before failing (no later open)' `
    -Condition ($guardReadSafe -and $restartRead -and $noLaterOpen) -Detail (
    'guardOpen={0}; change={1}; read={2}; failure={3}; nextOpen={4}' -f $guardOpen, $changeIndex,
    $readAfterChange, $failureIndex, $openAfterChange)
$serviceUntouched = $effects.Stops.Count -eq 0 -and $effects.Starts.Count -eq 0 -and $effects.Implicit.Count -eq 0
$agentAsSet = $script:World.Dependents[0].Status -ceq 'Running'
$instanceRunning = $script:World.InstanceState -ceq 'Running'
Assert-True -Name 'DSV-01 Stop and Start were never called and the agent was left as the other actor set it' `
    -Condition ($serviceUntouched -and $agentAsSet -and $instanceRunning) -Detail (
    'stops={0}; instance={1}' -f ($effects.Stops -join ','), $script:World.InstanceState)
$failedAtRestart = $null -ne $journal -and $journal.Value.Status -ceq 'Failed' -and
$journal.Value.FailedOperation -ceq 'RestartService'
$snapshotKept = $null -ne $journal -and $null -ne $journal.Value.Snapshot
Assert-True -Name 'DSV-01 the failure is journaled at RestartService with its snapshot, without rollback' `
    -Condition ($failedAtRestart -and $snapshotKept) -Detail ($(if ($null -ne $journal) {
            $journal.Value.Status + '/' + $journal.Value.FailedOperation }))
Assert-ControllersDisposed -Id 'DSV-01' -Steps @($apply)
Add-TestResult -Name 'DSV-01 public outcome and native cause' -Outcome OBSERVED -Detail (
    (Get-StepOutcome -Result $apply) + '; ' + (Get-NativeErrors -Result $apply))

# DSV-02: the agent starts after the restart branch's last check, just before SCM handles the stop.
$scenario = New-VerifyScenario -Id 'DSV-02' -Dependents @($safeAgent)
$script:World.BeforeStop = {
    Add-WorldEvent -Kind 'WorldChange' -Detail 'before-stop'
    $script:World.Dependents[0].Status = 'Running'
}
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$effects = Get-ServiceEffects -Events $apply.Events
$scmRefusals = Get-EventsOfKind -Events $apply.Events -Kind 'StopRefused'
$journal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$changeIndex = [Array]::IndexOf($apply.Events, 'WorldChange:before-stop')
$samePath = Test-SamePrefix -Events $apply.Events -ControlEvents $script:Control['Apply'].Events -Count $changeIndex
$singleSafeStop = ($effects.Stops -join ',') -ceq 'Stop:Stop(False)'
Assert-True -Name 'DSV-02 a dependent started after the last check makes SCM refuse the single Stop(False)' `
    -Condition ((Test-PublicError -Result $apply) -and $samePath -and $singleSafeStop -and $scmRefusals.Count -eq 1) `
    -Detail ('outcome={0}; samePath={1}; stops={2}; refused={3}' -f (Get-StepOutcome -Result $apply), $samePath,
    ($effects.Stops -join ','), $scmRefusals.Count)
$noCascade = $effects.Implicit.Count -eq 0 -and $effects.Starts.Count -eq 0
$instanceRunning = $script:World.InstanceState -ceq 'Running'
$agent = $script:World.Dependents[0]
$agentAsSet = $agent.Status -ceq 'Running' -and $agent.StartType -ceq 'Disabled'
Assert-True -Name 'DSV-02 the running dependent was never stopped and the instance kept running' `
    -Condition ($noCascade -and $instanceRunning -and $agentAsSet -and $apply.Violations.Count -eq 0) -Detail (
    'implicit={0}; instance={1}; agent={2}+{3}' -f ($effects.Implicit -join ','), $script:World.InstanceState,
    $agent.Status, $agent.StartType)
$failedAtRestart = $null -ne $journal -and $journal.Value.Status -ceq 'Failed' -and
$journal.Value.FailedOperation -ceq 'RestartService'
Assert-True -Name 'DSV-02 the refused restart is journaled as Failed RestartService' -Condition $failedAtRestart `
    -Detail ($(if ($null -ne $journal) { $journal.Value.Status + '/' + $journal.Value.FailedOperation }))
Assert-ControllersDisposed -Id 'DSV-02' -Steps @($apply)

# =====================================================================================================
# 7. Before and around the first snapshot: refusals write nothing; storage failures never become success.
# =====================================================================================================
# DSV-03: the agent turns unsafe after the Apply observation, just before the snapshot operation.
$scenario = New-VerifyScenario -Id 'DSV-03' -Dependents @($safeAgent)
$script:World.OperationHook = @{ Operation = 'SaveConfigurationSnapshot'; Action = {
        Add-WorldEvent -Kind 'WorldChange' -Detail 'before-snapshot'
        $script:World.Dependents[0].Status = 'Running'
        $script:World.Dependents[0].StartType = 'Manual'
    } }
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$changeIndex = [Array]::IndexOf($apply.Events, 'WorldChange:before-snapshot')
$readAfterChange = Get-FirstEventIndex -Events $apply.Events -Kinds @('DependentRead') -After $changeIndex
$samePath = Test-SamePrefix -Events $apply.Events -ControlEvents $script:Control['Apply'].Events -Count $changeIndex
$freshRead = $readAfterChange -gt $changeIndex
Assert-True -Name 'DSV-03 Apply refuses a dependent that turned unsafe between its observation and the snapshot' `
    -Condition ((Test-PublicError -Result $apply) -and $samePath -and $freshRead) -Detail (
    'outcome={0}; change={1}; read={2}; samePath={3}' -f (Get-StepOutcome -Result $apply), $changeIndex,
    $readAfterChange, $samePath)
Assert-NoFirstWrite -Id 'DSV-03' -Scenario $scenario -Result $apply
Set-AgentState -Status 'Stopped' -StartType 'Disabled'
$rerun = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
Assert-True -Name 'DSV-03 the refusal left no state: Apply succeeds once the agent is safe again' `
    -Condition ((Get-StepOutcome -Result $rerun) -ceq 'Applied') -Detail (Get-StepOutcome -Result $rerun)
Add-TestResult -Name 'DSV-03 public outcome and native cause' -Outcome OBSERVED -Detail (
    (Get-StepOutcome -Result $apply) + '; ' + (Get-NativeErrors -Result $apply))

# DSV-04: the snapshot storage fails before the file exists.
$scenario = New-VerifyScenario -Id 'DSV-04' -Dependents @($safeAgent)
$script:World.WriteFault = @{ Name = 'snapshot.json'; Pending = ''; When = 'Before' }
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$providerHidden = -not ([string]$apply.Error).Contains($script:SecretMarker)
Assert-True -Name 'DSV-04 a snapshot storage failure is a public Apply failure without provider text' `
    -Condition ((Test-PublicError -Result $apply) -and $providerHidden) -Detail (Get-StepOutcome -Result $apply)
Assert-NoFirstWrite -Id 'DSV-04' -Scenario $scenario -Result $apply
$rerun = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
Assert-True -Name 'DSV-04 with nothing stored, the next Apply succeeds' `
    -Condition ((Get-StepOutcome -Result $rerun) -ceq 'Applied') -Detail (Get-StepOutcome -Result $rerun)

# DSV-05: the snapshot file is written, then the write reports a failure (for example its final ACL check).
$scenario = New-VerifyScenario -Id 'DSV-05' -Dependents @($safeAgent)
$script:World.WriteFault = @{ Name = 'snapshot.json'; Pending = ''; When = 'After' }
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$snapshot = Get-PrivateFileState -Scenario $scenario -Name 'snapshot.json'
$journal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$registryWrites = Get-EventsOfKind -Events $apply.Events -Kind 'RegistryWrite'
$onlySnapshotStored = $null -ne $snapshot -and $null -eq $journal -and $registryWrites.Count -eq 0
$providerHidden = -not ([string]$apply.Error).Contains($script:SecretMarker)
Assert-True -Name 'DSV-05 a failure after the snapshot file exists fails Apply before any instance change' `
    -Condition ((Test-PublicError -Result $apply) -and $providerHidden -and $onlySnapshotStored) -Detail (
    'outcome={0}; snapshot={1}; journal={2}; registry={3}' -f (Get-StepOutcome -Result $apply),
    ($null -ne $snapshot), ($null -ne $journal), $registryWrites.Count)
$rerun = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$rerunJournal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$rerunEffects = Get-ServiceEffects -Events $rerun.Events
$rerunRegistry = Get-EventsOfKind -Events $rerun.Events -Kind 'RegistryWrite'
$rerunChangedNothing = $null -eq $rerunJournal -and $rerunRegistry.Count -eq 0 -and $rerunEffects.Stops.Count -eq 0
Assert-True -Name 'DSV-05 the rerun is refused, not reported as success, and changes nothing' `
    -Condition ((Test-PublicError -Result $rerun) -and $rerunChangedNothing) -Detail (
    'outcome={0}; journal={1}; registry={2}' -f (Get-StepOutcome -Result $rerun), ($null -ne $rerunJournal),
    $rerunRegistry.Count)
$cleanup = Invoke-CaseStep -Scenario $scenario -Step 'Cleanup'
$cleanupWrites = Get-WriteEvents -Events $cleanup.Events
Assert-True -Name 'DSV-05 Cleanup without a journal is refused and writes nothing' `
    -Condition ((Test-PublicError -Result $cleanup) -and $cleanupWrites.Count -eq 0) -Detail (
    'outcome={0}; writes={1}' -f (Get-StepOutcome -Result $cleanup), ($cleanupWrites -join ','))
# The adapter boundary still carries the fake provider text; only the core normalizes it, so it is masked here.
Add-TestResult -Name 'DSV-05 native causes of the first run and the rerun' -Outcome OBSERVED -Detail (
    ((Get-NativeErrors -Result $apply) + ' || ' + (Get-NativeErrors -Result $rerun)).Replace($script:SecretMarker,
        '<marker>'))

# DSV-06: after the snapshot the first Applying journal fails; the failure is still journaled (existing recovery).
$scenario = New-VerifyScenario -Id 'DSV-06' -Dependents @($safeAgent)
$script:World.WriteFault = @{ Name = 'lifecycle.json'; Pending = 'SetMixedAuthentication'; When = 'Before' }
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$journal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$registryWrites = Get-EventsOfKind -Events $apply.Events -Kind 'RegistryWrite'
$failedAtJournal = $null -ne $journal -and $journal.Value.Status -ceq 'Failed' -and
$journal.Value.FailedOperation -ceq 'WriteLifecycleState'
$snapshotKept = $null -ne $journal -and $null -ne $journal.Value.Snapshot
Assert-True -Name 'DSV-06 a failing first journal after the snapshot leaves a Failed journal with the snapshot' `
    -Condition ((Test-PublicError -Result $apply) -and $failedAtJournal -and $snapshotKept -and
    $registryWrites.Count -eq 0) -Detail ($(if ($null -ne $journal) {
            $journal.Value.Status + '/' + $journal.Value.FailedOperation + '; registry=' + $registryWrites.Count }))

# =====================================================================================================
# 8. Partial Apply and the later steps: Verify and Cleanup observe the dependents; recovery stays available.
# =====================================================================================================
# DSV-07: the agent's start type turns Manual after the first registry change (at the SetTcpPort journal).
$scenario = New-VerifyScenario -Id 'DSV-07' -Dependents @($safeAgent)
$script:World.WriteHook = @{ Name = 'lifecycle.json'; Pending = 'SetTcpPort'; Action = {
        Add-WorldEvent -Kind 'WorldChange' -Detail 'after-first-change'
        $script:World.Dependents[0].StartType = 'Manual'
    } }
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$journal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$registryWrites = Get-EventsOfKind -Events $apply.Events -Kind 'RegistryWrite'
$effects = Get-ServiceEffects -Events $apply.Events
$failedAtPort = $null -ne $journal -and $journal.Value.Status -ceq 'Failed' -and
$journal.Value.FailedOperation -ceq 'SetTcpPort'
$completedText = if ($null -ne $journal) { @($journal.Value.CompletedChanges) -join ',' } else { '' }
$onlyLoginMode = ($registryWrites -join ',') -ceq 'RegistryWrite:LoginMode'
Assert-True -Name 'DSV-07 a dependent turning unsafe mid-Apply stops the next change and journals the partial state' `
    -Condition ((Test-PublicError -Result $apply) -and $failedAtPort -and
    $completedText -ceq 'SetMixedAuthentication' -and $onlyLoginMode -and $effects.Stops.Count -eq 0) -Detail (
    'failedAtPort={0}; completed={1}; registry={2}; stops={3}' -f $failedAtPort, $completedText,
    ($registryWrites -join ','), ($effects.Stops -join ','))
$journalBefore = (Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json').Hash
$cleanupRefused = Invoke-CaseStep -Scenario $scenario -Step 'Cleanup'
$refusedWrites = Get-WriteEvents -Events $cleanupRefused.Events
$refusedEffects = Get-ServiceEffects -Events $cleanupRefused.Events
$journalAfter = (Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json').Hash
$refusalChangedNothing = $refusedWrites.Count -eq 0 -and $refusedEffects.Stops.Count -eq 0 -and
$journalAfter -ceq $journalBefore
Assert-True -Name 'DSV-07 Cleanup refuses while the dependent is unsafe and changes nothing' `
    -Condition ((Test-PublicError -Result $cleanupRefused) -and $refusalChangedNothing) -Detail (
    'outcome={0}; writes={1}; journalSame={2}' -f (Get-StepOutcome -Result $cleanupRefused),
    ($refusedWrites -join ','), ($journalAfter -ceq $journalBefore))
Set-AgentState -Status 'Stopped' -StartType 'Disabled'
$cleanup = Invoke-CaseStep -Scenario $scenario -Step 'Cleanup'
$cleanupEffects = Get-ServiceEffects -Events $cleanup.Events
$finalJournal = Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json'
$loginMode = $script:Registry['\MSSQLServer']['LoginMode'].Value
$cleaned = (Get-StepOutcome -Result $cleanup) -ceq 'Cleaned' -and $finalJournal.Value.Status -ceq 'Cleaned'
$singleSafeStop = ($cleanupEffects.Stops -join ',') -ceq 'Stop:Stop(False)' -and $cleanupEffects.Implicit.Count -eq 0
Assert-True -Name 'DSV-07 once the dependent is safe again Cleanup restores the original login mode' `
    -Condition ($cleaned -and $loginMode -eq 1 -and $singleSafeStop) -Detail (
    'outcome={0}; LoginMode={1}; stops={2}' -f (Get-StepOutcome -Result $cleanup), $loginMode,
    ($cleanupEffects.Stops -join ','))
Assert-ControllersDisposed -Id 'DSV-07' -Steps @($apply, $cleanupRefused, $cleanup)
Add-TestResult -Name 'DSV-07 public Cleanup outcome while unsafe' -Outcome OBSERVED -Detail (
    (Get-StepOutcome -Result $cleanupRefused) + '; ' + (Get-NativeErrors -Result $cleanupRefused))

# DSV-08: after a full Apply, an agent turning Manual blocks Verify and Cleanup until it is safe again.
$scenario = New-VerifyScenario -Id 'DSV-08' -Dependents @($safeAgent)
$apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
$verifySafe = Invoke-CaseStep -Scenario $scenario -Step 'Verify'
Set-AgentState -Status 'Stopped' -StartType 'Manual'
$verifyUnsafe = Invoke-CaseStep -Scenario $scenario -Step 'Verify'
$journalBefore = (Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json').Hash
$cleanupUnsafe = Invoke-CaseStep -Scenario $scenario -Step 'Cleanup'
$journalAfter = (Get-PrivateFileState -Scenario $scenario -Name 'lifecycle.json').Hash
# Read right after the refusal: the safe Cleanup below removes them.
$resourcesKept = $script:World.Database.Exists -and $script:World.Login.Exists
Set-AgentState -Status 'Stopped' -StartType 'Disabled'
$cleanupSafe = Invoke-CaseStep -Scenario $scenario -Step 'Cleanup'
$safeRefresh = Get-FirstEventIndex -Events $verifySafe.Events -Kinds @('ControllerRefresh')
$unsafeRefresh = Get-FirstEventIndex -Events $verifyUnsafe.Events -Kinds @('ControllerRefresh')
$verifySamePath = $safeRefresh -ge 0 -and $safeRefresh -eq $unsafeRefresh -and
(Test-SamePrefix -Events $verifyUnsafe.Events -ControlEvents $verifySafe.Events -Count ($safeRefresh + 1))
$tlsUnsafe = Get-EventsOfKind -Events $verifyUnsafe.Events -Kind 'TlsVerify'
$safeVerified = (Get-StepOutcome -Result $apply) -ceq 'Applied' -and
(Get-StepOutcome -Result $verifySafe) -ceq 'Verified'
Assert-True -Name 'DSV-08 Verify is refused before TLS when the agent turns Manual, where the safe Verify passed' `
    -Condition ($safeVerified -and (Test-PublicError -Result $verifyUnsafe) -and $verifySamePath -and
    $tlsUnsafe.Count -eq 0) -Detail ('apply={0}; safe={1}; unsafe={2}; samePath={3}; tls={4}' -f
    (Get-StepOutcome -Result $apply), (Get-StepOutcome -Result $verifySafe), (Get-StepOutcome -Result $verifyUnsafe),
    $verifySamePath, $tlsUnsafe.Count)
$unsafeWrites = Get-WriteEvents -Events $cleanupUnsafe.Events
$unsafeEffects = Get-ServiceEffects -Events $cleanupUnsafe.Events
$refusalChangedNothing = $unsafeWrites.Count -eq 0 -and $unsafeEffects.Stops.Count -eq 0 -and
$journalAfter -ceq $journalBefore
Assert-True -Name 'DSV-08 Cleanup of the applied lifetime is refused while unsafe and changes nothing' `
    -Condition ((Test-PublicError -Result $cleanupUnsafe) -and $refusalChangedNothing -and $resourcesKept) -Detail (
    'outcome={0}; writes={1}; stops={2}; journalSame={3}; resourcesKept={4}' -f
    (Get-StepOutcome -Result $cleanupUnsafe), ($unsafeWrites -join ','), ($unsafeEffects.Stops -join ','),
    ($journalAfter -ceq $journalBefore), $resourcesKept)
$resourcesRemoved = -not $script:World.Database.Exists -and -not $script:World.Login.Exists
Assert-True -Name 'DSV-08 once safe again Cleanup removes the owned database and login' `
    -Condition ((Get-StepOutcome -Result $cleanupSafe) -ceq 'Cleaned' -and $resourcesRemoved) `
    -Detail (Get-StepOutcome -Result $cleanupSafe)
Add-TestResult -Name 'DSV-08 public outcomes of the refused Verify and Cleanup' -Outcome OBSERVED -Detail (
    'Verify=' + (Get-StepOutcome -Result $verifyUnsafe) + '; Cleanup=' + (Get-StepOutcome -Result $cleanupUnsafe) +
    '; ' + (Get-NativeErrors -Result $verifyUnsafe) + '|' + (Get-NativeErrors -Result $cleanupUnsafe))

# =====================================================================================================
# 9. Observation shapes and faults: refused like the control, nothing written, provider text never public.
# =====================================================================================================
$shapeCases = @(
    @{ Id = 'DSV-09a'; Title = 'a missing dependent list (null)'; ListMode = 'Null'; Dependents = @($safeAgent) },
    @{ Id = 'DSV-09b'; Title = 'a null entry before a safe agent'; ListMode = 'NullEntry'; Dependents = @($safeAgent) },
    @{ Id = 'DSV-09c'; Title = 'a controller refresh failing with provider text'; RefreshFault = $true
        Dependents = @($safeAgent) },
    @{ Id = 'DSV-09d'; Title = 'a non-agent dependent that is Stopped+Manual'; Dependents = @($safeAgent,
            (New-FakeDependent -Name 'DHFixtureReporting' -Status 'Stopped' -StartType 'Manual')) },
    @{ Id = 'DSV-09e'; Title = 'a Stopped dependent with start type System'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'System')) }
)
foreach ($case in $shapeCases) {
    $listMode = if ($case.ContainsKey('ListMode')) { $case.ListMode } else { 'List' }
    $refreshFault = $case.ContainsKey('RefreshFault') -and $case.RefreshFault
    $scenario = New-VerifyScenario -Id $case.Id -Dependents $case.Dependents -ListMode $listMode `
        -RefreshFault $refreshFault
    $steps = @(foreach ($stepName in @('Preflight', 'Plan', 'Apply')) {
            Invoke-CaseStep -Scenario $scenario -Step $stepName
        })
    foreach ($result in $steps) { Assert-RefusedLikeControl -Id $case.Id -Title $case.Title -Result $result }
    Assert-NoFirstWrite -Id $case.Id -Scenario $scenario -Result $steps[2]
    $leakingFiles = @(Get-ChildItem -LiteralPath @($scenario.CaseRoot, $scenario.Private) -Recurse -File -Force |
            Where-Object { [IO.File]::ReadAllText($_.FullName).Contains($script:SecretMarker) } | ForEach-Object Name)
    $leakingErrors = @($steps | Where-Object { ([string]$_.Error).Contains($script:SecretMarker) })
    $leakingNative = @($steps | Where-Object { (Get-NativeErrors -Result $_).Contains($script:SecretMarker) })
    $providerHidden = $leakingFiles.Count -eq 0 -and $leakingErrors.Count -eq 0 -and $leakingNative.Count -eq 0
    Assert-True -Name ($case.Id + ' provider text reaches no public error, native cause or file') `
        -Condition $providerHidden -Detail ('files={0}; errors={1}; native={2}' -f ($leakingFiles -join ','),
        $leakingErrors.Count, $leakingNative.Count)
    Assert-ControllersDisposed -Id $case.Id -Steps $steps
    Add-TestResult -Name ($case.Id + ' public outcomes and native causes') -Outcome OBSERVED -Detail (
        'Preflight=' + (Get-StepOutcome -Result $steps[0]) + '; Plan=' + (Get-StepOutcome -Result $steps[1]) +
        '; Apply=' + (Get-StepOutcome -Result $steps[2]) + '; ' + (Get-NativeErrors -Result $steps[2]))
}

# =====================================================================================================
# 10. Suite-wide preservation and safety.
# =====================================================================================================
$forceWrites = @($script:AllEvents | Where-Object { $_ -ceq 'RegistryWrite:ForceEncryption' })
Assert-True -Name 'DSV-PRESERVE-01 ForceEncryption is never written in any step of any case' `
    -Condition ($forceWrites.Count -eq 0) -Detail ([string]$forceWrites.Count)
$tlsBad = @($script:TlsRequests | Where-Object { $_ -notmatch ':Encrypt=Mandatory;TSC=False$' })
Assert-True -Name 'DSV-PRESERVE-02 every Verify request keeps Encrypt=Mandatory and TrustServerCertificate=false' `
    -Condition ($script:TlsRequests.Count -gt 0 -and $tlsBad.Count -eq 0) -Detail (
    'requests={0}; bad={1}' -f $script:TlsRequests.Count, ($tlsBad -join ','))
$counts = $script:ServiceCounts
$instancesDisposed = $counts.InstanceOpened -gt 0 -and $counts.InstanceOpened -eq $counts.InstanceDisposed
$dependentsDisposed = $counts.DependentsCreated -eq $counts.DependentsDisposed
Assert-True -Name 'DSV-DISPOSE-01 suite-wide, every opened controller and enumerated dependent was disposed' `
    -Condition ($instancesDisposed -and $dependentsDisposed) -Detail ('instance={0}/{1}; dependents={2}/{3}' -f
    $counts.InstanceOpened, $counts.InstanceDisposed, $counts.DependentsCreated, $counts.DependentsDisposed)
Assert-True -Name 'DSV-SAFETY-01 no forbidden real command or unexpected fake call was reached in any case' `
    -Condition ($script:ForbiddenCalls.Count -eq 0 -and $script:AllViolations.Count -eq 0) -Detail (
    (@($script:ForbiddenCalls) + @($script:AllViolations)) -join ',')
Complete-TestSuite
