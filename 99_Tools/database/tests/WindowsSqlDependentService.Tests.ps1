[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    # Optional absolute windows-sql folder of an isolated same-shaped database tree (before/after product runs).
    [string]$ProductRoot = ''
)
# Requirement tests for the dependent services of the exact SQL instance service, written before the fix.
# Requirement (lead contract DS-1 v1 from the main scope decisions msg_87d420e8d4fb / msg_b5227e44a175):
# - Preflight, Plan and Apply observe the dependent services before the first write. They continue only when there
#   is none or every dependent is Stopped with start type Disabled. Any other state or start type, a null or
#   missing value and an observation error are explicit rejections. Names or counts alone never allow.
# - Immediately before the restart the same condition is observed again; a change since the earlier observation
#   stops Apply before Stop. Dependents are never stopped, deleted or reconfigured.
# The real path runs: Invoke-WindowsSqlStep -> observation/Apply core -> adapter InvokeOperation -> native
# dispatcher -> instance helpers including the real RestartService branch, journal and private store.
# Only protected facts and effects are replaced below those helpers: registry keys, service facts and control
# (ServiceController via a type accelerator, Get-Service, Win32_Service CIM reads), TCP listeners, firewall rules,
# the WSL child, SQL connection/statements, database mutations, executor/certificate gates and the fixed instance
# identity read (direct OpenBaseKey, as in WindowsSqlAdapterBoundary.Tests.ps1). Each fake records its call and
# returns the simulated state; none recomputes a product decision. Expected outcomes are requirement constants.
# A rejection counts only where the one-safe-agent control (DS-ALLOW-02) is accepted and the case followed the same
# recorded path up to its first dependent-service read: only the dependent input explains the difference.
# Nothing here opens SQL, the registry, a service, the firewall, a certificate store or WSL.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-dependent-service'

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
$script:FixturePort = 15434
$script:FixtureThumbprint = 'CD' * 20
$script:InstanceService = 'MSSQL$' + $script:FixtureInstance
$script:AgentService = 'SQLAgent$' + $script:FixtureInstance
$script:PublicCodePattern = '^WINDOWS_SQL_[A-Z0-9_]+$'
if ([Environment]::MachineName -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run.'
}
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
try { $script:ExecutorSid = $currentIdentity.User.Value } finally { $currentIdentity.Dispose() }

# ---- Fail-closed guards: reaching any of these real commands is a test failure, never a side effect.
$script:ForbiddenCalls = [Collections.Generic.List[string]]::new()
foreach ($name in @('Restart-Service', 'Stop-Service', 'Start-Service', 'Set-Service', 'Suspend-Service',
        'Resume-Service', 'New-Service', 'Remove-Service', 'Invoke-CimMethod', 'Set-CimInstance', 'Remove-CimInstance',
        'New-CimSession', 'Get-WmiObject', 'sc', 'sc.exe', 'net', 'net.exe', 'Start-Process', 'Invoke-Sqlcmd',
        'sqlcmd', 'reg', 'reg.exe', 'netsh', 'wsl', 'wsl.exe', 'Get-ItemProperty', 'Set-ItemProperty',
        'New-ItemProperty', 'Remove-ItemProperty', 'New-NetFirewallRule', 'Set-NetFirewallRule',
        'Remove-NetFirewallRule', 'New-SelfSignedCertificate', 'Import-PfxCertificate', 'certutil', 'docker')) {
    Set-Item -Path ('Function:script:' + $name) -Value ([scriptblock]::Create(
            "`$script:ForbiddenCalls.Add('$name'); throw 'DEPENDENT_TEST_FORBIDDEN_$name'"))
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
# 1. Service world. One table feeds every service read API, so the product may observe through any of them.
# =====================================================================================================
# Status/StartType hold ServiceControllerStatus/ServiceStartMode names or $null. Shape 'NoStartType' returns the
# dependent without a start type field. ReadFault makes every dependent read throw (an observation error).
Add-Type -AssemblyName System.ServiceProcess.ServiceController
$script:CimStates = @{
    Stopped = 'Stopped'; StartPending = 'Start Pending'; StopPending = 'Stop Pending'; Running = 'Running'
    ContinuePending = 'Continue Pending'; PausePending = 'Pause Pending'; Paused = 'Paused'
}
$script:CimStartModes = @{
    Boot = 'Boot'; System = 'System'; Automatic = 'Auto'; Manual = 'Manual'; Disabled = 'Disabled'
}

function New-FakeDependent {
    param([string]$Name, $Status, $StartType, [ValidateSet('Full', 'NoStartType')][string]$Shape = 'Full')
    return @{ Name = $Name; Status = $Status; StartType = $StartType; Shape = $Shape }
}

function Add-WorldEvent {
    param([string]$Kind, [string]$Detail)
    $script:World.Events.Add($Kind + ':' + $Detail)
}

function Add-WorldViolation {
    param([string]$Text)
    $script:World.Violations.Add($Text)
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

function Read-FakeDependentWorld {
    param([string]$Api)
    # Every fresh dependent observation is one event; the fault models an SCM/CIM read failure.
    Add-WorldEvent -Kind 'DependentRead' -Detail $Api
    if ($script:World.ReadFault) { throw 'FAKE_DEPENDENT_OBSERVATION_FAILED' }
    return , @($script:World.Dependents.ToArray())
}

function Get-FakeDependentControllers {
    param([string]$Api)
    $items = foreach ($entry in (Read-FakeDependentWorld -Api $Api)) {
        if ($entry.Shape -ceq 'NoStartType') {
            [pscustomobject]@{
                ServiceName = $entry.Name
                Name = $entry.Name
                DisplayName = $entry.Name
                Status = ConvertTo-FakeStatus -Value $entry.Status
            }
        }
        else {
            [FakeServiceController]::new($entry.Name, (ConvertTo-FakeStatus -Value $entry.Status),
                (ConvertTo-FakeStartMode -Value $entry.StartType), $true)
        }
    }
    return , ([object[]]@($items))
}

function ConvertTo-FakeCimService {
    param($Entry)
    $properties = [ordered]@{
        Name = $Entry.Name
        DisplayName = $Entry.Name
        State = $(if ($null -eq $Entry.Status) { $null } else { $script:CimStates[$Entry.Status] })
    }
    if ($Entry.Shape -ceq 'Full') {
        $properties.StartMode = $(if ($null -eq $Entry.StartType) { $null }
            else { $script:CimStartModes[$Entry.StartType] })
    }
    return [pscustomobject]$properties
}

# Replaces System.ServiceProcess.ServiceController for the product's own type literal (accelerator in section 2).
# Behaviour lives in script-scope hooks so every effect is recorded in the shared world.
class FakeServiceController {
    static [hashtable]$Hooks
    [string]$ServiceName
    [string]$Name
    [string]$DisplayName
    [object]$Status
    [object]$StartType
    hidden [bool]$IsDependent
    hidden [object]$DependentCache

    # The product's constructor form opens the named service; only the exact instance service is allowed.
    FakeServiceController([string]$name) {
        $this.ServiceName = $name
        $this.Name = $name
        $this.DisplayName = $name
        & ([FakeServiceController]::Hooks['Open']) $this
    }

    # Dependent entries created by the fake itself; the product never calls this form.
    FakeServiceController([string]$name, [object]$status, [object]$startType, [bool]$isDependent) {
        $this.ServiceName = $name
        $this.Name = $name
        $this.DisplayName = $name
        $this.Status = $status
        $this.StartType = $startType
        $this.IsDependent = $isDependent
    }

    [void] Stop() { & ([FakeServiceController]::Hooks['Stop']) $this $true 'Stop()' }
    [void] Stop([bool]$stopDependentServices) {
        & ([FakeServiceController]::Hooks['Stop']) $this $stopDependentServices ('Stop(' + $stopDependentServices + ')')
    }
    [void] Start() { & ([FakeServiceController]::Hooks['Start']) $this }
    [void] WaitForStatus([object]$desired) { & ([FakeServiceController]::Hooks['Wait']) $this $desired }
    [void] WaitForStatus([object]$desired, [TimeSpan]$timeout) {
        & ([FakeServiceController]::Hooks['Wait']) $this $desired
    }
    [void] Refresh() { & ([FakeServiceController]::Hooks['Refresh']) $this }
    [void] Close() { }
    [void] Dispose() { }
}

$script:DependentServicesGetter = {
    # Like .NET the list is read once per controller and kept until Refresh().
    if ($null -eq $this.DependentCache) {
        $this.DependentCache = Get-FakeDependentControllers -Api 'ServiceController.DependentServices'
    }
    return , $this.DependentCache
}

[FakeServiceController]::Hooks = @{
    Open = {
        param($Controller)
        Add-WorldEvent -Kind 'ControllerOpen' -Detail $Controller.ServiceName
        if ($Controller.ServiceName -cne $script:InstanceService) {
            Add-WorldViolation -Text ('controller for another service: ' + $Controller.ServiceName)
            throw 'FAKE_CONTROLLER_UNEXPECTED'
        }
        $Controller.Status = ConvertTo-FakeStatus -Value $script:World.InstanceState
        $Controller.StartType = ConvertTo-FakeStartMode -Value 'Automatic'
        Add-Member -InputObject $Controller -MemberType ScriptProperty -Name DependentServices `
            -Value $script:DependentServicesGetter
        $change = $script:World.OnControllerOpen
        if ($null -ne $change) {
            $script:World.OnControllerOpen = $null
            Add-WorldEvent -Kind 'WorldChange' -Detail 'controller-open'
            & $change
        }
    }
    Stop = {
        param($Controller, [bool]$StopDependents, [string]$Form)
        if ($Controller.IsDependent) {
            Add-WorldViolation -Text ('dependent service control: Stop ' + $Controller.ServiceName)
            throw 'FAKE_DEPENDENT_CONTROL'
        }
        Add-WorldEvent -Kind 'Stop' -Detail $Form
        $active = @($script:World.Dependents | Where-Object { $_.Status -cne 'Stopped' })
        if ($active.Count -gt 0) {
            # SCM refuses to stop a service with active dependents when it may not stop them (error 1051).
            if (-not $StopDependents) { throw 'FAKE_SCM_DEPENDENT_SERVICES_RUNNING' }
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
            $Controller.DependentCache = $null
            $Controller.Status = ConvertTo-FakeStatus -Value $script:World.InstanceState
            return
        }
        $entries = @((Read-FakeDependentWorld -Api 'ServiceController.Refresh') | Where-Object {
                $_.Name -ceq $Controller.ServiceName })
        if ($entries.Count -ne 1) { throw 'FAKE_SERVICE_MISSING' }
        $Controller.Status = ConvertTo-FakeStatus -Value $entries[0].Status
        $Controller.StartType = ConvertTo-FakeStartMode -Value $entries[0].StartType
    }
}

function Get-Service {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string[]]$Name, [switch]$DependentServices)
    if (@($Name).Count -ne 1 -or $Name[0] -cne $script:InstanceService) {
        Add-WorldViolation -Text ('Get-Service for ' + (@($Name) -join ','))
        throw 'FAKE_GET_SERVICE_UNEXPECTED'
    }
    if (-not $DependentServices) { return [FakeServiceController]::new($script:InstanceService) }
    # A cmdlet writes each dependent to the pipeline.
    foreach ($controller in (Get-FakeDependentControllers -Api 'Get-Service -DependentServices')) { $controller }
}

function Get-CimInstance {
    [CmdletBinding()]
    param([string]$ClassName, [string]$Filter, [string]$Query, [string]$Namespace)
    if ($Query) {
        $instance = [regex]::Escape($script:InstanceService)
        $associators = '(?i)^\s*ASSOCIATORS\s+OF\s+\{\s*Win32_Service\.Name\s*=\s*[''"]' + $instance +
        '[''"]\s*\}\s+WHERE\b(?=.*\bAssocClass\s*=\s*Win32_DependentService\b)(?=.*\bRole\s*=\s*Antecedent\b)'
        if ($Query -match $associators) {
            foreach ($entry in (Read-FakeDependentWorld -Api 'CIM ASSOCIATORS Win32_DependentService')) {
                ConvertTo-FakeCimService -Entry $entry
            }
            return
        }
        Add-WorldViolation -Text ('CIM query ' + $Query)
        throw 'FAKE_CIM_UNEXPECTED'
    }
    if ($ClassName -cne 'Win32_Service') {
        Add-WorldViolation -Text ('CIM class ' + $ClassName)
        throw 'FAKE_CIM_UNEXPECTED'
    }
    if ($Filter -ceq ("Name='" + $script:InstanceService + "'")) {
        return [pscustomobject]@{
            Name = $script:InstanceService
            StartName = 'NT Service\' + $script:InstanceService
            State = $script:World.InstanceState
            StartMode = 'Auto'
            ProcessId = 4242
        }
    }
    $byName = [regex]::Match($Filter, "^Name\s*=\s*'([^']+)'$")
    if ($byName.Success) {
        $entries = @((Read-FakeDependentWorld -Api 'CIM Win32_Service by name') | Where-Object {
                $_.Name -ceq $byName.Groups[1].Value })
        if ($entries.Count -eq 1) { return ConvertTo-FakeCimService -Entry $entries[0] }
    }
    Add-WorldViolation -Text ('CIM filter ' + $Filter)
    throw 'FAKE_CIM_UNEXPECTED'
}

function Get-CimAssociatedInstance {
    [CmdletBinding()]
    param([Parameter(Position = 0, ValueFromPipeline)]$InputObject, [string]$Association, [string]$ResultClassName,
        [string]$ResultRole, [string]$Namespace)
    process {
        # The fixture instance depends on nothing, so the association yields only its dependents.
        $otherClass = $ResultClassName -and $ResultClassName -ne 'Win32_Service'
        if ((Get-PropertyValue -Object $InputObject -Name 'Name') -cne $script:InstanceService -or
            $Association -ne 'Win32_DependentService' -or $otherClass) {
            Add-WorldViolation -Text ('CIM association ' + $Association + '/' + $ResultClassName)
            throw 'FAKE_CIM_UNEXPECTED'
        }
        if ($ResultRole -ieq 'Antecedent') { return }
        foreach ($entry in (Read-FakeDependentWorld -Api 'CIM associated Win32_DependentService')) {
            ConvertTo-FakeCimService -Entry $entry
        }
    }
}

function Reset-FakeWorld {
    param([object[]]$Dependents = @(), [bool]$ReadFault = $false)
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
        ReadFault = $ReadFault
        OnRestartJournal = $null
        OnControllerOpen = $null
        Events = [Collections.Generic.List[string]]::new()
        Violations = [Collections.Generic.List[string]]::new()
    }
}

# =====================================================================================================
# 2. Guards before any product code loads: the type literal must reach the fake, and no product source may bypass
#    the fakes. If either fails, no flow runs (not executed, design needed) and no real service is touched.
# =====================================================================================================
Add-TestResult -Name 'DS-GUARD-00 PowerShell version' -Outcome OBSERVED -Detail $PSVersionTable.PSVersion.ToString()
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
$controllerTypeNames = @('ServiceProcess.ServiceController', 'System.ServiceProcess.ServiceController')
foreach ($typeName in $controllerTypeNames) { $accelerators::Add($typeName, [FakeServiceController]) }
$resolvedNames = @(foreach ($typeName in $controllerTypeNames) {
        (& ([scriptblock]::Create('[' + $typeName + ']'))).FullName
    })
$literalFaked = @($resolvedNames | Where-Object { $_ -cne 'FakeServiceController' }).Count -eq 0
Assert-True -Name 'DS-GUARD-01 ServiceController type literals resolve to the fake' -Condition $literalFaked `
    -Detail ($resolvedNames -join ',')

$bypassPattern = '(?i)(\b(CimCmdlets|Microsoft\.PowerShell\.Management)\\[A-Za-z]+-|\[wmi(searcher|class)?\]|' +
'ManagementObject|CimSession\]::)'
$bypassHits = @(Get-ChildItem -LiteralPath $script:ProductRoot -Recurse -File -Filter '*.ps1' | Where-Object {
        [IO.File]::ReadAllText($_.FullName) -match $bypassPattern
    } | ForEach-Object Name)
Assert-True -Name 'DS-GUARD-02 product sources have no module-qualified or WMI path around the service fakes' `
    -Condition ($bypassHits.Count -eq 0) -Detail ($bypassHits -join ',')

$controllerUsable = $false
if ($literalFaked) {
    Reset-FakeWorld -Dependents @()
    $probe = & ([scriptblock]::Create('[ServiceProcess.ServiceController]::new($args[0])')) $script:InstanceService
    $probeList = $probe.DependentServices
    $controllerUsable = $probe -is [FakeServiceController] -and $probeList -is [object[]] -and $probeList.Count -eq 0
}
Assert-True -Name 'DS-GUARD-03 fake controller exposes DependentServices through the product literal' `
    -Condition $controllerUsable
if (-not ($literalFaked -and $bypassHits.Count -eq 0 -and $controllerUsable)) {
    Add-TestResult -Name 'DS-FLOW-00 flows not executed: service control cannot be faked safely' -Outcome FAIL
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
    # Fresh fixture: neither the target database nor the runtime login exists.
    if ($Sql.Contains('CASE WHEN DB_ID(@name) IS NULL')) { return 0 }
    if ($Sql.Contains('FROM sys.server_principals WHERE name = @name')) { return [DBNull]::Value }
    if ($Sql.Contains('CASE WHEN SUSER_ID(@name) IS NULL')) { return 0 }
    Add-WorldViolation -Text ('unexpected SQL: ' + ($Sql.Trim() -split "`n")[0])
    throw 'FAKE_SQL_UNEXPECTED'
}

# Database creation, migrations and login are covered by WindowsSqlAdapterBoundary.Tests.ps1; here they are effects.
function Invoke-WindowsSqlDatabaseMutation {
    param($Adapter, [string]$Operation)
    Add-WorldEvent -Kind 'DatabaseMutation' -Detail $Operation
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
# A dependent-service check placed only inside this function is invisible to this suite (reported limit).
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

$script:RealWritePrivateJson = ${function:Write-WindowsSqlPrivateJson}
function Write-WindowsSqlPrivateJson {
    param($Adapter, [string]$Name, $Value)
    $pending = Get-WindowsSqlField -Object $Value -Name 'PendingOperation'
    Add-WorldEvent -Kind 'PrivateWrite' -Detail ($Name + '/' + [string]$pending)
    & $script:RealWritePrivateJson -Adapter $Adapter -Name $Name -Value $Value
    # The journal naming RestartService is the last durable point before the restart branch runs.
    $change = $script:World.OnRestartJournal
    if ($null -ne $change -and $Name -ceq 'lifecycle.json' -and $pending -ceq 'RestartService') {
        $script:World.OnRestartJournal = $null
        Add-WorldEvent -Kind 'WorldChange' -Detail 'restart-journal'
        & $change
    }
}

# =====================================================================================================
# 4. Scenario, step capture and assertions.
# =====================================================================================================
function New-DependentScenario {
    param([string]$Id, [object[]]$Dependents = @(), [bool]$ReadFault = $false)
    Reset-FakeWorld -Dependents $Dependents -ReadFault $ReadFault
    $lifetime = [Guid]::NewGuid().ToString('N')
    $source = Join-Path $script:SuiteRoot ('case-' + $Id + '\source')
    [void][IO.Directory]::CreateDirectory((Join-Path $source '.backups\evidence'))
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    New-WindowsSqlPrivateDirectory -Path $private -Sid $script:ExecutorSid
    $accountId = [Guid]::NewGuid().ToString('D')
    $characterId = [Guid]::NewGuid().ToString('D')
    Write-JsonFile -Path (Join-Path $source 'profile.json') -Value ([ordered]@{
            persistedAccountName = 'fixture_user'
            database = 'Dawnholder_DependentFixture'
            serverEndpoint = ('tcp:localhost,' + $script:FixturePort)
            slotId = 1
            accountId = $accountId
            characterId = $characterId
        })
    [IO.File]::WriteAllText((Join-Path $source 'public-ca.pem'),
        "dependent-service public fixture, not a certificate`n")
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
    $planPath = Join-Path $script:SuiteRoot ('case-' + $Id + '\plan.json')
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
            Database = 'Dawnholder_DependentFixture'
            RuntimeLogin = 'dh_dependent_fixture'
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
        InitialDependents = (ConvertTo-Json -InputObject @($script:World.Dependents.ToArray()) -Depth 4 -Compress)
    }
}

function New-StepAdapter {
    param($Scenario)
    # One adapter per step, like one CLI process per step: no connection or registry baseline carries over, so a
    # case and its control start every step from the same adapter state and the same private store.
    $adapter = [pscustomobject]@{
        Kind = 'DependentServiceFake'
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
        # Machine identity is the only dispatcher branch replaced: it reads this host's real name.
        if ($Operation -ceq 'ReadMachineIdentity') { return [pscustomobject]@{ MachineName = $script:FixtureMachine } }
        Invoke-WindowsSqlNativeOperation -Adapter $this -Operation $Operation -Request $Request
    }
    return $adapter
}

function Invoke-CaseStep {
    param($Scenario, [string]$Step)
    $adapter = New-StepAdapter -Scenario $Scenario
    $eventStart = $script:World.Events.Count
    $violationStart = $script:World.Violations.Count
    $forbiddenStart = $script:ForbiddenCalls.Count
    $output = @()
    $errorText = $null
    try { $output = @(Invoke-WindowsSqlStep -Plan $Scenario.Plan -Step $Step -Adapter $adapter) }
    catch { $errorText = $_.Exception.Message }
    return [pscustomobject]@{
        Step = $Step
        Result = $(if ($output.Count -gt 0) { $output[$output.Count - 1] } else { $null })
        Error = $errorText
        Events = [object[]]@($script:World.Events | Select-Object -Skip $eventStart)
        Violations = [object[]]@($script:World.Violations | Select-Object -Skip $violationStart)
        Forbidden = [object[]]@($script:ForbiddenCalls | Select-Object -Skip $forbiddenStart)
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

function Get-ReadApis {
    param([object[]]$Steps)
    $apis = @(foreach ($step in $Steps) {
            foreach ($read in (Get-EventsOfKind -Events $step.Events -Kind 'DependentRead')) {
                $read.Substring('DependentRead:'.Length)
            }
        })
    return (@($apis | Sort-Object -Unique) -join '|')
}

function Test-SamePathBefore {
    param([object[]]$Events, [object[]]$ControlEvents, [int]$Index)
    # The case and its accepted control did exactly the same things before the case diverges at $Index.
    if ($Index -lt 0 -or $ControlEvents.Count -lt $Index) { return $false }
    if ($Index -eq 0) { return $true }
    return ($Events[0..($Index - 1)] -join "`n") -ceq ($ControlEvents[0..($Index - 1)] -join "`n")
}

function Assert-StepRejected {
    param([string]$Id, [string]$Title, $Result)
    # A rejection counts only against the accepted one-safe-agent control of the same step: same fixture, same
    # path up to the first dependent-service read, and only the dependent input differs.
    $stepControl = $script:Control[$Result.Step]
    $reads = Get-EventsOfKind -Events $Result.Events -Kind 'DependentRead'
    $publicError = $null -ne $Result.Error -and $Result.Error -cmatch $script:PublicCodePattern
    $changePlan = Get-PropertyValue -Object $Result.Result -Name 'ChangePlan'
    $blocked = $null -eq $Result.Error -and $null -ne $changePlan -and $changePlan.Decision -ceq 'Blocked'
    $rejected = $publicError -or $blocked
    $firstRead = Get-FirstEventIndex -Events $Result.Events -Kinds @('DependentRead')
    $controlFirstRead = Get-FirstEventIndex -Events $stepControl.Events -Kinds @('DependentRead')
    $samePath = $firstRead -ge 0 -and $firstRead -eq $controlFirstRead -and
    (Test-SamePathBefore -Events $Result.Events -ControlEvents $stepControl.Events -Index $firstRead)
    $clean = $Result.Violations.Count -eq 0 -and $Result.Forbidden.Count -eq 0
    Assert-True -Name ('{0} {1} rejects {2} where the safe control is accepted' -f $Id, $Result.Step, $Title) `
        -Condition ($rejected -and $stepControl.Accepted -and $samePath -and $clean) -Detail (
        'outcome={0}; control={1}/{2}; firstRead={3}/{4}; samePath={5}; reads={6}; violations={7}' -f
        (Get-StepOutcome -Result $Result), $stepControl.Accepted, $stepControl.Outcome, $firstRead, $controlFirstRead,
        $samePath, $reads.Count, ((@($Result.Violations) + @($Result.Forbidden)) -join ','))
}

function Assert-ApplyHadNoEffect {
    param([string]$Id, $Scenario, $Apply)
    $writes = @($Apply.Events | Where-Object {
            $_ -clike 'RegistryWrite:*' -or $_ -clike 'PrivateWrite:*' -or $_ -clike 'DatabaseMutation:*' })
    $snapshotExists = [IO.File]::Exists((Join-Path $Scenario.Private 'snapshot.json'))
    $journalExists = [IO.File]::Exists((Join-Path $Scenario.Private 'lifecycle.json'))
    Assert-True -Name ($Id + ' Apply made no first write (registry, snapshot, journal, database)') `
        -Condition ($writes.Count -eq 0 -and -not $snapshotExists -and -not $journalExists) `
        -Detail ('writes={0}; snapshot={1}; journal={2}' -f ($writes -join ','), $snapshotExists, $journalExists)
    Assert-ServiceUntouched -Id $Id -Scenario $Scenario -Steps @($Apply) -AllowedStops 0
}

function Assert-ServiceUntouched {
    param([string]$Id, $Scenario, [object[]]$Steps, [int]$AllowedStops)
    $events = @($Steps | ForEach-Object { $_.Events })
    $stops = Get-EventsOfKind -Events $events -Kind 'Stop'
    $starts = Get-EventsOfKind -Events $events -Kind 'Start'
    $implicit = Get-EventsOfKind -Events $events -Kind 'ImplicitDependentStop'
    $violations = @($Steps | ForEach-Object { @($_.Violations) + @($_.Forbidden) })
    # A test-driven world change is another actor's edit; then only the product's own effects above are judged.
    $worldChanges = Get-EventsOfKind -Events $events -Kind 'WorldChange'
    $finalDependents = ConvertTo-Json -InputObject @($script:World.Dependents.ToArray()) -Depth 4 -Compress
    $unchanged = $worldChanges.Count -gt 0 -or $finalDependents -ceq $Scenario.InitialDependents
    $label = if ($AllowedStops -eq 0) { 'never stopped or started the instance' } else { 'stopped the instance once' }
    Assert-True -Name ($Id + ' ' + $label + ' and left every dependent service untouched') -Condition (
        $stops.Count -eq $AllowedStops -and $starts.Count -eq $AllowedStops -and $implicit.Count -eq 0 -and
        $violations.Count -eq 0 -and $unchanged) -Detail (
        'stops={0}; starts={1}; implicit={2}; violations={3}; worldChanges={4}; unchanged={5}' -f ($stops -join ','),
        $starts.Count, ($implicit -join ','), ($violations -join ','), $worldChanges.Count, $unchanged)
}

# =====================================================================================================
# 5. Allowed sets: none, one or several dependents, each Stopped with start type Disabled.
# =====================================================================================================
$allowCases = @(
    @{ Id = 'DS-ALLOW-01'; Title = 'no dependent service'; Dependents = @() },
    @{ Id = 'DS-ALLOW-02'; Title = 'one Stopped+Disabled agent'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled')) },
    @{ Id = 'DS-ALLOW-03'; Title = 'three Stopped+Disabled dependents'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled'),
            (New-FakeDependent -Name 'DHFixtureTelemetry' -Status 'Stopped' -StartType 'Disabled'),
            (New-FakeDependent -Name 'DHFixtureReporting' -Status 'Stopped' -StartType 'Disabled')) }
)
$script:Control = $null
foreach ($case in $allowCases) {
    $scenario = New-DependentScenario -Id $case.Id -Dependents $case.Dependents
    $preflight = Invoke-CaseStep -Scenario $scenario -Step 'Preflight'
    $planStep = Invoke-CaseStep -Scenario $scenario -Step 'Plan'
    $apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
    $id = $case.Id

    Assert-True -Name ($id + ' Preflight succeeds for ' + $case.Title) -Condition ($null -eq $preflight.Error) `
        -Detail ([string]$preflight.Error)
    $preflightReads = Get-EventsOfKind -Events $preflight.Events -Kind 'DependentRead'
    Assert-True -Name ($id + ' Preflight observes the dependent services') -Condition ($preflightReads.Count -gt 0) `
        -Detail ([string]$preflightReads.Count)
    $changePlan = Get-PropertyValue -Object $planStep.Result -Name 'ChangePlan'
    $planApplies = $null -eq $planStep.Error -and $null -ne $changePlan -and $changePlan.Decision -ceq 'Apply' -and
    @($changePlan.Changes) -ccontains 'RestartService'
    Assert-True -Name ($id + ' Plan decides Apply with RestartService') -Condition $planApplies `
        -Detail (Get-StepOutcome -Result $planStep)
    $planReads = Get-EventsOfKind -Events $planStep.Events -Kind 'DependentRead'
    Assert-True -Name ($id + ' Plan observes the dependent services') -Condition ($planReads.Count -gt 0) `
        -Detail ([string]$planReads.Count)
    Assert-Equal -Name ($id + ' Apply result') -Expected 'Applied' -Actual (Get-StepOutcome -Result $apply)
    if ($id -ceq 'DS-ALLOW-02') {
        # The one-safe-agent control of every rejected, restart and policy case below.
        $script:Control = @{
            Preflight = [pscustomobject]@{
                Accepted = $null -eq $preflight.Error
                Outcome = Get-StepOutcome -Result $preflight
                Events = $preflight.Events
            }
            Plan = [pscustomobject]@{
                Accepted = $planApplies
                Outcome = Get-StepOutcome -Result $planStep
                Events = $planStep.Events
            }
            Apply = [pscustomobject]@{
                Accepted = (Get-StepOutcome -Result $apply) -ceq 'Applied'
                Outcome = Get-StepOutcome -Result $apply
                Events = $apply.Events
            }
        }
    }

    $events = $apply.Events
    $firstRead = Get-FirstEventIndex -Events $events -Kinds @('DependentRead')
    $firstWrite = Get-FirstEventIndex -Events $events -Kinds @('RegistryWrite', 'PrivateWrite', 'DatabaseMutation',
        'Stop', 'Start')
    Assert-True -Name ($id + ' Apply observes the dependent services before its first write') `
        -Condition ($firstRead -ge 0 -and $firstWrite -ge 0 -and $firstRead -lt $firstWrite) `
        -Detail ('firstRead={0}; firstWrite={1}' -f $firstRead, $firstWrite)
    $journal = [Array]::IndexOf($events, 'PrivateWrite:lifecycle.json/RestartService')
    $stop = Get-FirstEventIndex -Events $events -Kinds @('Stop') -After $journal
    $freshRead = Get-FirstEventIndex -Events $events -Kinds @('DependentRead') -After $journal
    Assert-True -Name ($id + ' restart re-observes the dependents after its journal and before Stop') `
        -Condition ($journal -ge 0 -and $stop -gt $journal -and $freshRead -gt $journal -and $freshRead -lt $stop) `
        -Detail ('journal={0}; freshRead={1}; stop={2}' -f $journal, $freshRead, $stop)
    Assert-ServiceUntouched -Id $id -Scenario $scenario -Steps @($preflight, $planStep, $apply) -AllowedStops 1
    $forceWrites = @($events | Where-Object { $_ -ceq 'RegistryWrite:ForceEncryption' })
    $forceValue = $script:Registry['\MSSQLServer\SuperSocketNetLib']['ForceEncryption'].Value
    Assert-True -Name ($id + ' ForceEncryption is never written and keeps its original value') `
        -Condition ($forceWrites.Count -eq 0 -and $forceValue -eq 1) -Detail ([string]$forceValue)
    Add-TestResult -Name ($id + ' observed service APIs and stop form') -Outcome OBSERVED -Detail (
        'reads=' + (Get-ReadApis -Steps @($preflight, $planStep, $apply)) + '; stop=' +
        ((Get-EventsOfKind -Events $events -Kind 'Stop') -join ','))
}

# =====================================================================================================
# 6. Rejected sets: every state other than Stopped, every start type other than Disabled, null/missing values,
#    an observation error and a mixed set. Preflight, Plan and Apply reject where the DS-ALLOW-02 control of the
#    same step is accepted along the same path; Apply writes nothing and never stops. Exact codes stay OBSERVED.
# =====================================================================================================
$rejectCases = @(
    @{ Id = 'DS-REJECT-01'; Title = 'a Running dependent'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Running' -StartType 'Disabled')) },
    @{ Id = 'DS-REJECT-02'; Title = 'a StartPending dependent'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'StartPending' -StartType 'Disabled')) },
    @{ Id = 'DS-REJECT-03'; Title = 'a StopPending dependent'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'StopPending' -StartType 'Disabled')) },
    @{ Id = 'DS-REJECT-04'; Title = 'a Paused dependent'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Paused' -StartType 'Disabled')) },
    @{ Id = 'DS-REJECT-05'; Title = 'a Stopped dependent with start type Automatic'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Automatic')) },
    @{ Id = 'DS-REJECT-06'; Title = 'a Stopped dependent with start type Manual'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Manual')) },
    @{ Id = 'DS-REJECT-07'; Title = 'a dependent with a null state'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status $null -StartType 'Disabled')) },
    @{ Id = 'DS-REJECT-08'; Title = 'a dependent with a null start type'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType $null)) },
    @{ Id = 'DS-REJECT-09'; Title = 'a dependent without a start type field'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled' `
                -Shape 'NoStartType')) },
    @{ Id = 'DS-REJECT-10'; Title = 'a failing dependent-service observation'; ReadFault = $true; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled')) },
    @{ Id = 'DS-REJECT-11'; Title = 'a mixed safe and Running+Manual set'; Dependents = @(
            (New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled'),
            (New-FakeDependent -Name 'DHFixtureTelemetry' -Status 'Running' -StartType 'Manual')) }
)
foreach ($case in $rejectCases) {
    $readFault = $case.ContainsKey('ReadFault') -and $case.ReadFault
    $scenario = New-DependentScenario -Id $case.Id -Dependents $case.Dependents -ReadFault $readFault
    $steps = @(foreach ($stepName in @('Preflight', 'Plan', 'Apply')) {
            Invoke-CaseStep -Scenario $scenario -Step $stepName
        })
    foreach ($result in $steps) { Assert-StepRejected -Id $case.Id -Title $case.Title -Result $result }
    Assert-ApplyHadNoEffect -Id $case.Id -Scenario $scenario -Apply $steps[2]
    Add-TestResult -Name ($case.Id + ' public outcomes and observed service APIs') -Outcome OBSERVED -Detail (
        'Preflight=' + (Get-StepOutcome -Result $steps[0]) + '; Plan=' + (Get-StepOutcome -Result $steps[1]) +
        '; Apply=' + (Get-StepOutcome -Result $steps[2]) + '; reads=' + (Get-ReadApis -Steps $steps))
}

# =====================================================================================================
# 7. Re-check immediately before the restart: safe at the first observation, changed afterwards. Apply must fail
#    before Stop. Cases 01-03 change the world when the RestartService journal is written. Case 04 changes it at
#    the first instance controller open after that journal, so a re-check made only before any controller exists
#    (for example only through CIM) cannot keep Stop from running.
# =====================================================================================================
$safeAgent = New-FakeDependent -Name $script:AgentService -Status 'Stopped' -StartType 'Disabled'
$restartCases = @(
    @{ Id = 'DS-RESTART-01'; Title = 'the agent became Running+Manual'; At = 'OnRestartJournal'; Change = {
            $script:World.Dependents[0].Status = 'Running'
            $script:World.Dependents[0].StartType = 'Manual'
        } },
    @{ Id = 'DS-RESTART-02'; Title = 'the agent start type became Automatic'; At = 'OnRestartJournal'; Change = {
            $script:World.Dependents[0].StartType = 'Automatic'
        } },
    @{ Id = 'DS-RESTART-03'; Title = 'a new Running+Automatic dependent appeared'; At = 'OnRestartJournal'; Change = {
            $late = New-FakeDependent -Name 'DHFixtureLate' -Status 'Running' -StartType 'Automatic'
            $script:World.Dependents.Add($late)
        } },
    @{ Id = 'DS-RESTART-04'; Title = 'the agent became Running+Manual at the next controller open'
        At = 'OnControllerOpen'
        Change = {
            $script:World.Dependents[0].Status = 'Running'
            $script:World.Dependents[0].StartType = 'Manual'
        } }
)
foreach ($case in $restartCases) {
    $scenario = New-DependentScenario -Id $case.Id -Dependents @($safeAgent)
    $changeEvent = 'WorldChange:restart-journal'
    if ($case.At -ceq 'OnRestartJournal') { $script:World.OnRestartJournal = $case.Change }
    else {
        # Armed only once the RestartService journal exists, so earlier controller opens see the safe world.
        $changeEvent = 'WorldChange:controller-open'
        $script:ArmedOpenChange = $case.Change
        $script:World.OnRestartJournal = { $script:World.OnControllerOpen = $script:ArmedOpenChange }
    }
    $preflight = Invoke-CaseStep -Scenario $scenario -Step 'Preflight'
    $apply = Invoke-CaseStep -Scenario $scenario -Step 'Apply'
    $id = $case.Id
    Assert-True -Name ($id + ' Preflight accepts the still-safe agent') -Condition ($null -eq $preflight.Error) `
        -Detail ([string]$preflight.Error)
    $changeIndex = [Array]::IndexOf($apply.Events, $changeEvent)
    $readAfterChange = Get-FirstEventIndex -Events $apply.Events -Kinds @('DependentRead') -After $changeIndex
    $publicError = $null -ne $apply.Error -and $apply.Error -cmatch $script:PublicCodePattern
    # Up to the change the case must repeat the accepted control exactly; only the later dependent input differs.
    # Case 04 arms its change at the restart journal; that marker is this suite's own step, not a product action.
    $pathEvents = $apply.Events
    if ($case.At -cne 'OnRestartJournal') {
        $pathEvents = [object[]]@($apply.Events | Where-Object { $_ -cne 'WorldChange:restart-journal' })
    }
    $pathChangeIndex = [Array]::IndexOf($pathEvents, $changeEvent)
    $applyControl = $script:Control['Apply']
    $samePath = Test-SamePathBefore -Events $pathEvents -ControlEvents $applyControl.Events -Index $pathChangeIndex
    Assert-True -Name ($id + ' Apply fails after ' + $case.Title + ' where the unchanged control applies') `
        -Condition ($publicError -and $applyControl.Accepted -and $samePath -and $apply.Violations.Count -eq 0) `
        -Detail ('outcome={0}; control={1}/{2}; change={3}; samePath={4}' -f (Get-StepOutcome -Result $apply),
        $applyControl.Accepted, $applyControl.Outcome, $changeIndex, $samePath)
    Assert-True -Name ($id + ' the change reached the restart point and a fresh read observed it') `
        -Condition ($changeIndex -ge 0 -and $readAfterChange -gt $changeIndex) `
        -Detail ('change={0}; readAfterChange={1}' -f $changeIndex, $readAfterChange)
    $stops = Get-EventsOfKind -Events $apply.Events -Kind 'Stop'
    $starts = Get-EventsOfKind -Events $apply.Events -Kind 'Start'
    $implicit = Get-EventsOfKind -Events $apply.Events -Kind 'ImplicitDependentStop'
    Assert-True -Name ($id + ' Stop and Start were never called and no dependent was stopped') `
        -Condition ($stops.Count -eq 0 -and $starts.Count -eq 0 -and $implicit.Count -eq 0 -and
        $apply.Forbidden.Count -eq 0) -Detail ('stops={0}; implicit={1}; forbidden={2}' -f ($stops -join ','),
        ($implicit -join ','), ($apply.Forbidden -join ','))
    $journalPath = Join-Path $scenario.Private 'lifecycle.json'
    $journalState = if ([IO.File]::Exists($journalPath)) { [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json }
    Assert-True -Name ($id + ' the failure is journaled at RestartService without rollback') -Condition (
        $null -ne $journalState -and $journalState.Status -ceq 'Failed' -and
        $journalState.FailedOperation -ceq 'RestartService') `
        -Detail ($(if ($null -ne $journalState) { $journalState.Status + '/' + $journalState.FailedOperation }))
}

# =====================================================================================================
# 8. Policy: unsafe at the first observation and safe by restart time. The first observation is final: Apply is
#    refused before any write, and the later safe state is never reached or adopted.
# =====================================================================================================
$scenario = New-DependentScenario -Id 'DS-POLICY-01' -Dependents @(
    (New-FakeDependent -Name $script:AgentService -Status 'Running' -StartType 'Disabled'))
$script:World.OnRestartJournal = {
    $script:World.Dependents[0].Status = 'Stopped'
}
$policySteps = @(foreach ($stepName in @('Preflight', 'Plan', 'Apply')) {
        Invoke-CaseStep -Scenario $scenario -Step $stepName
    })
foreach ($result in $policySteps) {
    Assert-StepRejected -Id 'DS-POLICY-01' -Title 'a dependent that would be stopped later' -Result $result
}
Assert-ApplyHadNoEffect -Id 'DS-POLICY-01' -Scenario $scenario -Apply $policySteps[2]
$laterChanges = Get-EventsOfKind -Events $policySteps[2].Events -Kind 'WorldChange'
Assert-True -Name 'DS-POLICY-01 the later safe state is never reached: the first observation is final' `
    -Condition ($laterChanges.Count -eq 0 -and $null -ne $script:World.OnRestartJournal) `
    -Detail ($laterChanges -join ',')

Assert-True -Name 'DS-SAFETY-01 no forbidden real command was reached in any case' `
    -Condition ($script:ForbiddenCalls.Count -eq 0) -Detail ($script:ForbiddenCalls -join ',')
Complete-TestSuite
