[CmdletBinding()]
param([Parameter(Mandatory)][string]$WorkRoot)
# P02 requirements (msg_b6138c0ea19d): excluded/dynamic ranges block inclusively for both address families.
# Reuse only the existing dependent-service fixture definitions, ending before its first test cases.
# No product decision is replaced: actual Step -> native dispatcher -> ReadPort and SetTcpPort run below.
# All service/registry/SQL/WSL boundaries remain the existing recorded, fail-closed stand-ins.
$fixturePath = Join-Path $PSScriptRoot 'WindowsSqlDependentService.Tests.ps1'
$fixtureText = [IO.File]::ReadAllText($fixturePath)
$stop = $fixtureText.IndexOf('$allowCases = @(')
if ($stop -lt 0) { throw 'DEPENDENT_FIXTURE_BOUNDARY_NOT_FOUND' }
$prefix = $fixtureText.Substring(0, $stop).
Replace(". (Join-Path `$PSScriptRoot 'TestSupport.ps1')", ". '$(Join-Path $PSScriptRoot 'TestSupport.ps1')'").
Replace("'windows-sql-dependent-service'", "'windows-sql-port-ranges'")
. ([scriptblock]::Create($prefix)) -WorkRoot $WorkRoot
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'reused-fixture-prefix.ps1'), $prefix)
$script:FixturePort = 15433
$script:NetshCalls = [Collections.Generic.List[object]]::new()
$script:PortCases = [Collections.Generic.List[object]]::new()
$script:EmptyTable = "TCP excluded ranges`nStart Port    End Port`n----------    --------`n* - Administered exclusions."
$script:HighDynamic = "TCP dynamic range`n-----------------`nStart Port: 49152`nNumber of Ports: 16384"
$script:LowDynamic = "TCP dynamic range`n-----------------`nStart Port: 60000`nNumber of Ports: 100"
$script:Responses = @{}
$script:Listeners = @()
$script:ChildFault = ''

function Reset-PortInputs {
    Reset-FakeWorld -Dependents @()
    $script:Responses = @{
        'ipv4/excludedportrange' = $script:EmptyTable
        'ipv6/excludedportrange' = $script:EmptyTable
        'ipv4/dynamicport' = $script:LowDynamic
        'ipv6/dynamicport' = $script:LowDynamic
    }
    $script:NetshCalls.Clear()
    $script:Listeners = @()
    $script:ChildFault = ''
}

function Invoke-WindowsSqlChild {
    param([string]$FileName, [string[]]$Arguments, [string]$InputText = '', [hashtable]$Environment = @{},
        [int]$TimeoutSeconds = 45)
    if ($FileName -ceq 'C:\Windows\System32\netsh.exe') {
        $script:NetshCalls.Add([pscustomobject]@{
                File = $FileName
                Argv = $Arguments
                Timeout = $TimeoutSeconds
            })
        $argText = $Arguments -join ' '
        $valid = $argText -cin @('int ipv4 show excludedportrange protocol=tcp',
            'int ipv6 show excludedportrange protocol=tcp', 'int ipv4 show dynamicport tcp',
            'int ipv6 show dynamicport tcp')
        if (-not $valid -or $TimeoutSeconds -ne 15 -or $InputText.Length -ne 0 -or $Environment.Count -ne 0) {
            Add-WorldViolation -Text ('unexpected netsh invocation ' + $argText)
            throw 'FAKE_NETSH_UNEXPECTED'
        }
        $key = $Arguments[1] + '/' + $Arguments[3]
        if ($key -ceq $script:ChildFault) { throw 'WINDOWS_SQL_CHILD_FAILED' }
        return $script:Responses[$key]
    }
    if ($FileName -ceq 'C:\Windows\System32\wsl.exe' -and
        ($Arguments -join ' ') -ceq '-d Ubuntu --exec wslinfo --networking-mode') {
        return 'mirrored'
    }
    Add-WorldViolation -Text ('unexpected child ' + $FileName)
    throw 'FAKE_CHILD_UNEXPECTED'
}

function Get-NetTCPConnection {
    [CmdletBinding()]
    param([string]$State, [int]$LocalPort)
    if ($State -cne 'Listen') { throw 'FAKE_LISTENER_QUERY_UNEXPECTED' }
    return $script:Listeners
}

function Read-WindowsSqlPrivateJson {
    param($Adapter, [string]$Name)
    # No pre-existing resource receipt; do not open even a fixture private directory during Preflight.
    Add-WorldEvent -Kind 'PrivateRead' -Detail $Name
    return $null
}

function New-PortScenario {
    param([int]$Port)
    # Explicit approved in-memory fixture; no production plan/private store is opened.
    $plan = [pscustomobject]@{
        Version = 1
        ExecutionApproved = $true
        ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(1).ToString('o')
        LifetimeId = ('a' * 32)
        MachineName = $script:FixtureMachine
        InstanceName = $script:FixtureInstance
        InstanceId = $script:FixtureInstanceId
        ExpectedProductVersion = $script:FixtureVersion
        Port = $Port
        Database = 'Dawnholder_PortFixture'
        RuntimeLogin = 'dh_port_fixture'
        SchemaVersion = 5
        CertificateThumbprint = $script:FixtureThumbprint
        Encrypt = 'Mandatory'
        TrustServerCertificate = $false
        EnableSa = $false
        SwitchWslNetworkingMode = $false
        ForceEncryption = 'Preserve'
        RestartApproved = $true
        FirewallApproved = $true
        SourceRoot = $script:SuiteRoot
        EvidenceRoot = (Join-Path $script:SuiteRoot '.backups/evidence')
        PrivateRoot = (Join-Path $script:SuiteRoot 'unopened-private')
        Inputs = @()
        PlanHash = ('A' * 64)
    }
    $runtime = [pscustomobject]@{
        ServiceAccount = ('NT Service\' + $script:InstanceService)
        ExecutorSid = $script:ExecutorSid
        NetworkMode = 'Mirrored'
        EndpointAddress = '127.0.0.1'
        WslSubnet = $null
    }
    return [pscustomobject]@{
        Plan = $plan
        Runtime = $runtime
    }
}

function Invoke-PortRequirement {
    param([string]$Id, [int]$Port, [string]$ExpectedError = '')
    $scenario = New-PortScenario -Port $Port
    $adapter = New-StepAdapter -Scenario $scenario
    $directError = ''
    $directPort = $null
    try { $directPort = Read-WindowsSqlPort -Adapter $adapter }
    catch { $directError = $_.Exception.Message }
    $directCalls = @($script:NetshCalls.ToArray())
    $script:NetshCalls.Clear()
    $observed = $null
    $errorText = ''
    try { $observed = Invoke-WindowsSqlStep -Plan $scenario.Plan -Step Preflight -Adapter $adapter }
    catch { $errorText = $_.Exception.Message }
    # The public dispatcher intentionally sanitizes provider exceptions; the direct boundary binds the cause.
    $entryExpected = if ($ExpectedError) { 'WINDOWS_SQL_ADAPTER_FAILED' } else { '' }
    $correct = $directError -ceq $ExpectedError -and $errorText -ceq $entryExpected
    $effects = @($script:World.Events | Where-Object {
            $_ -match '^(RegistryWrite|PrivateWrite|DatabaseMutation|Stop|Start):'
        })
    $clean = $effects.Count -eq 0 -and $script:World.Violations.Count -eq 0 -and $script:ForbiddenCalls.Count -eq 0
    $null = Assert-True -Name ($Id + ' actual Preflight outcome and no effects') -Condition ($correct -and $clean) `
        -Detail ('direct=' + $directError + '; public=' + $errorText + '; effects=' + ($effects -join ','))
    $script:PortCases.Add([pscustomobject]@{
            Id = $Id
            Port = $Port
            ExpectedError = $ExpectedError
            DirectError = $directError
            Error = $errorText
            DirectPort = $directPort
            DirectNetsh = $directCalls
            Netsh = @($script:NetshCalls.ToArray())
            Events = @($script:World.Events.ToArray())
            Violations = @($script:World.Violations.ToArray())
            Observation = $observed
        })
    $script:PortCases | ConvertTo-Json -Depth 14 |
        Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'port-cases.json')
    return $observed
}

Reset-PortInputs
$free = Invoke-PortRequirement -Id 'PORT-FREE-15433' -Port 15433
Assert-True -Name 'free listener keeps public shape and semantics' -Condition (
    $free.Observation.Port.Port -eq 15433 -and -not $free.Observation.Port.InUse -and
    -not $free.Observation.Port.OwnedByInstance)
Assert-Equal -Name 'all four exact netsh show commands are called once in order' `
    -Expected 'int ipv4 show excludedportrange protocol=tcp|int ipv4 show dynamicport tcp|int ipv6 show excludedportrange protocol=tcp|int ipv6 show dynamicport tcp' `
    -Actual (($script:NetshCalls | ForEach-Object { $_.Argv -join ' ' }) -join '|')

foreach ($family in @('ipv4', 'ipv6')) {
    foreach ($port in @(51392, 51433, 51491, 51391, 51492)) {
        Reset-PortInputs
        $script:Responses[$family + '/excludedportrange'] = "TCP exclusions`nStart End`n---- ----`n51392 51491 *`n* - Managed."
        $expected = if ($port -cin @(51392, 51433, 51491)) { 'WINDOWS_SQL_PORT_CONFLICT' } else { '' }
        $null = Invoke-PortRequirement -Id ('EXCLUDED-' + $family + '-' + $port) -Port $port -ExpectedError $expected
    }
    foreach ($port in @(49151, 49152, 51433, 65535, 15433)) {
        Reset-PortInputs
        $script:Responses[$family + '/dynamicport'] = $script:HighDynamic
        $expected = if ($port -cin @(49152, 51433, 65535)) { 'WINDOWS_SQL_PORT_CONFLICT' } else { '' }
        $null = Invoke-PortRequirement -Id ('DYNAMIC-' + $family + '-' + $port) -Port $port -ExpectedError $expected
    }
}

$badExcluded = @{
    null = $null
    blank = ''
    malformed = "head`ncolumns`n---- ----`nnot a row`n* - Managed."
    reversed = "head`ncolumns`n---- ----`n20000 19999`n* - Managed."
    zero = "head`ncolumns`n---- ----`n0 20`n* - Managed."
    overflow = "head`ncolumns`n---- ----`n65535 65536`n* - Managed."
    truncated = "head`ncolumns`n---- ----`n20000 20010"
}
$badDynamic = @{
    null = $null
    blank = ''
    malformed = "head`n----`nStart: unknown`nCount: 2"
    zeroStart = "head`n----`nStart: 0`nCount: 2"
    zeroCount = "head`n----`nStart: 49152`nCount: 0"
    overflow = "head`n----`nStart: 65535`nCount: 2"
    huge = "head`n----`nStart: 999999999999`nCount: 2"
    missing = "head`n----`nStart: 49152"
}
foreach ($family in @('ipv4', 'ipv6')) {
    foreach ($kind in @('excludedportrange', 'dynamicport')) {
        $badInputs = if ($kind -ceq 'excludedportrange') { $badExcluded } else { $badDynamic }
        foreach ($name in $badInputs.Keys) {
            Reset-PortInputs
            $script:Responses[$family + '/' + $kind] = $badInputs[$name]
            $null = Invoke-PortRequirement -Id ('BAD-' + $family + '-' + $kind + '-' + $name) -Port 15433 `
                -ExpectedError 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
        }
        Reset-PortInputs
        $script:ChildFault = $family + '/' + $kind
        $null = Invoke-PortRequirement -Id ('CHILD-FAIL-' + $family + '-' + $kind) -Port 15433 `
            -ExpectedError 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
    }
}

foreach ($owner in @('own', 'foreign')) {
    Reset-PortInputs
    $script:Listeners = @([pscustomobject]@{ OwningProcess = $(if ($owner -ceq 'own') { 4242 } else { 8888 }) })
    $value = Invoke-PortRequirement -Id ('LISTENER-' + $owner) -Port 15433
    Assert-True -Name ($owner + ' listener semantics remain explicit') -Condition (
        $value.Observation.Port.InUse -and $value.Observation.Port.OwnedByInstance -eq ($owner -ceq 'own'))
    $scenario = New-PortScenario -Port 15433
    $adapter = New-StepAdapter -Scenario $scenario
    $planResult = Invoke-WindowsSqlStep -Plan $scenario.Plan -Step Plan -Adapter $adapter
    $blocked = @($planResult.ChangePlan.Blockers) -ccontains 'WINDOWS_SQL_PORT_CONFLICT'
    Assert-True -Name ($owner + ' Plan preserves listener ownership decision') `
        -Condition ($blocked -eq ($owner -ceq 'foreign'))
}
Reset-PortInputs
$script:World.InstanceState = 'Stopped'
$null = Invoke-PortRequirement -Id 'STOPPED-SERVICE' -Port 15433 -ExpectedError 'WINDOWS_SQL_SERVICE_CHANGED'
Reset-PortInputs
$script:World.Dependents.Add((New-FakeDependent -Name $script:AgentService -Status Running -StartType Disabled))
$null = Invoke-PortRequirement -Id 'UNSAFE-DEPENDENT' -Port 15433 -ExpectedError 'WINDOWS_SQL_DEPENDENT_SERVICE'

foreach ($kind in @('excludedportrange', 'dynamicport')) {
    Reset-PortInputs
    $script:Listeners = @([pscustomobject]@{ OwningProcess = 4242 })
    if ($kind -ceq 'excludedportrange') {
        $script:Responses['ipv4/' + $kind] = "head`ncolumns`n---- ----`n51433 51433`n* - Managed."
    }
    else { $script:Responses['ipv6/' + $kind] = $script:HighDynamic }
    $null = Invoke-PortRequirement -Id ('OWN-RESERVED-' + $kind) -Port 51433 `
        -ExpectedError 'WINDOWS_SQL_PORT_CONFLICT'
}

Reset-PortInputs
$scenario = New-PortScenario -Port 15433
$adapter = New-StepAdapter -Scenario $scenario
$null = Read-WindowsSqlPort -Adapter $adapter
$script:Responses['ipv6/excludedportrange'] = "head`ncolumns`n---- ----`n15433 15433`n* - Managed."
$errorText = Get-ThrownMessage { Invoke-WindowsSqlInstanceMutation -Adapter $adapter -Operation SetTcpPort -Request @{} }
$writes = @($script:World.Events | Where-Object { $_ -clike 'RegistryWrite:*' })
Assert-True -Name 'SetTcpPort re-observes newly excluded port before any registry write' -Condition (
    $errorText -ceq 'WINDOWS_SQL_PORT_CONFLICT' -and $script:NetshCalls.Count -eq 8 -and $writes.Count -eq 0)

Reset-PortInputs
$scenario = New-PortScenario -Port 15433
$adapter = New-StepAdapter -Scenario $scenario
$null = Read-WindowsSqlPort -Adapter $adapter
$script:Responses['ipv4/dynamicport'] = "head`n----`nStart: 15433`nCount: 1"
$errorText = Get-ThrownMessage { Invoke-WindowsSqlInstanceMutation -Adapter $adapter -Operation SetTcpPort -Request @{} }
$writes = @($script:World.Events | Where-Object { $_ -clike 'RegistryWrite:*' })
Assert-True -Name 'SetTcpPort re-observes a newly dynamic port before any registry write' -Condition (
    $errorText -ceq 'WINDOWS_SQL_PORT_CONFLICT' -and $script:NetshCalls.Count -eq 8 -and $writes.Count -eq 0)

Reset-PortInputs
$scenario = New-PortScenario -Port 15433
$adapter = New-StepAdapter -Scenario $scenario
$script:Listeners = @([pscustomobject]@{ OwningProcess = 8888 })
$errorText = Get-ThrownMessage { Invoke-WindowsSqlInstanceMutation -Adapter $adapter -Operation SetTcpPort -Request @{} }
$writes = @($script:World.Events | Where-Object { $_ -clike 'RegistryWrite:*' })
Assert-True -Name 'SetTcpPort refuses a foreign listener before any registry write' -Condition (
    $errorText -ceq 'WINDOWS_SQL_PORT_CONFLICT' -and $writes.Count -eq 0)

Reset-PortInputs
$scenario = New-PortScenario -Port 15433
$adapter = New-StepAdapter -Scenario $scenario
$adapter.ExpectedRegistry = Read-WindowsSqlRegistry -Adapter $adapter
$null = Invoke-WindowsSqlInstanceMutation -Adapter $adapter -Operation SetTcpPort -Request @{}
$writes = @($script:World.Events | Where-Object { $_ -clike 'RegistryWrite:*' })
Assert-Equal -Name 'free SetTcpPort preserves exact registry write set' `
    -Expected 'RegistryWrite:TcpDynamicPorts|RegistryWrite:TcpPort|RegistryWrite:Enabled' -Actual ($writes -join '|')
Assert-Equal -Name 'ForceEncryption remains unchanged' -Expected 1 `
    -Actual $script:Registry['\MSSQLServer\SuperSocketNetLib']['ForceEncryption'].Value
$script:PortCases | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'port-cases.json')
Complete-TestSuite
