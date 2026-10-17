[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Requirement tests for the Windows SQL Express demo tool, written before the tool exists.
# Until windows-sql/WindowsSql.Common.ps1 provides these functions every requirement case below is expected to FAIL.
# - Read-WindowsSqlPlan -Path -ExpectedHash: bounded JSON, exact hash, version, expiry window, explicit
#   machine/instance identity, port/database/login names, LifetimeId, source input hashes, PrivateRoot/EvidenceRoot
#   and the fixed TLS/sa/global-change values. The reader checks the plan only; the machine itself is compared later
#   through the adapter, so a plan naming another machine is still a readable plan.
# - Get-WindowsSqlChangePlan -Plan -Observation: a pure decision over a caller-provided observation. It returns
#   Decision/Changes/Blockers/TargetConfiguration and never claims that the observation came from a live instance.
# Field names, error codes and change names are fixed in the pretest test contract for the implementer.
# Nothing here opens SQL, Docker or WSL or reads services, the registry, certificate stores or the firewall:
# those commands are replaced by recording stubs, and a case that reaches one fails. Fixture values are not secrets.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-plan'

# The private parent is %LOCALAPPDATA%\Dawnholder; it points into the suite so no real profile path is named.
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))

$productRelative = 'windows-sql/WindowsSql.Common.ps1'
$productPath = Join-Path $script:ToolRoot $productRelative
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
Assert-True -Name ('PRODUCT-01 {0} loads its definitions' -f $productRelative) `
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

# ---- Small helpers. Expected values below are constants; nothing re-derives a product decision.
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
    # A fresh object graph per call, so a product that edits its input cannot change the next case's fixture.
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
    # Assert-Equal plus the case's own error and result, so a dependent failure shows what the call did.
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

# ---- Fixed fixture values (constants of this test contract).
$fixtureLifetime = '0123456789abcdef0123456789abcdef'
$fixtureMachine = 'LAPTOP-FIXTURE'
$fixtureInstance = 'SQLEXPRESS'
$fixtureInstanceId = 'MSSQL17.SQLEXPRESS'
$fixtureVersion = '17.0.1000.7'
$fixturePort = 14333
$fixtureDatabase = 'Dawnholder_Demo_Fixture'
$fixtureLogin = 'dh_demo_fixture'
$fixtureThumbprint = 'A1B2C3D4E5F60718293A4B5C6D7E8F9012345678'
$fixtureWslSubnet = '172.29.48.0/20'
$secretSentinel = 'WinSqlPlanSentinel-5d1e8b'

$sourceRoot = Join-Path $script:SuiteRoot 'source'
$evidenceRoot = Join-Path $sourceRoot '.backups\windows-sql-evidence'
[void][IO.Directory]::CreateDirectory($evidenceRoot)
# Named only; the reader does not need the private directory to exist yet.
$privateRoot = Join-Path $env:LOCALAPPDATA 'Dawnholder\windows-sql-fixture'

function New-SourceInputs {
    param([Parameter(Mandatory)][string]$Root)
    $entries = foreach ($index in 1..3) {
        $relative = 'inputs/file{0:D2}.txt' -f $index
        $path = Join-Path $Root $relative
        [void][IO.Directory]::CreateDirectory((Split-Path -Path $path -Parent))
        [IO.File]::WriteAllText($path, "windows sql fixture $index")
        [ordered]@{
            Path = $relative
            Sha256 = Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($path))
        }
    }
    return , @($entries)
}
$sourceInputs = New-SourceInputs -Root $sourceRoot

function New-ValidPlanValues {
    param([string]$ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(2).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"))
    return [ordered]@{
        Version = 1
        ExecutionApproved = $true
        ExpiresUtc = $ExpiresUtc
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

$script:PlanFileIndex = 0
function Write-PlanBytes {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][byte[]]$Bytes
    )
    $script:PlanFileIndex++
    $path = Join-Path $script:SuiteRoot ('plan-{0:D3}-{1}.json' -f $script:PlanFileIndex, $Name)
    [IO.File]::WriteAllBytes($path, $Bytes)
    return [pscustomobject]@{
        Path = $path
        Hash = Get-Sha256Hex -Bytes $Bytes
    }
}

function Write-PlanFile {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)]$Values
    )
    $json = ConvertTo-Json -InputObject $Values -Depth 8
    return Write-PlanBytes -Name $Name -Bytes ([Text.UTF8Encoding]::new($false).GetBytes($json))
}

function Invoke-PlanRead {
    param(
        [Parameter(Mandatory)]$File,
        [string]$ExpectedHash
    )
    $readPath = $File.Path
    $readHash = if ($PSBoundParameters.ContainsKey('ExpectedHash')) { $ExpectedHash } else { $File.Hash }
    $script:ExternalCalls.Clear()
    $capture = Invoke-Captured -CapturedAction { Read-WindowsSqlPlan -Path $readPath -ExpectedHash $readHash }
    return [pscustomobject]@{
        Capture = $capture
        Plan = Get-ReturnedObject -Records $capture.Records
        External = @($script:ExternalCalls.ToArray())
    }
}

function Test-PlanRejected {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Code,
        [Parameter(Mandatory)]$File,
        [string]$ExpectedHash
    )
    $readArguments = @{ File = $File }
    if ($PSBoundParameters.ContainsKey('ExpectedHash')) { $readArguments.ExpectedHash = $ExpectedHash }
    $read = Invoke-PlanRead @readArguments
    $rightCause = $read.Capture.Error -ceq $Code
    $noExternal = $read.External.Count -eq 0
    Assert-True -Name ('{0}: rejected as {1} before any external call' -f $Name, $Code) `
        -Condition ($rightCause -and $noExternal) `
        -Detail ('error <{0}> external <{1}>' -f $read.Capture.Error, ($read.External -join ','))
}

function Test-PlanVariantRejected {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Code,
        [Parameter(Mandatory)][scriptblock]$Change
    )
    # One field changes from the valid plan, so the asserted code belongs to that field alone.
    $values = New-ValidPlanValues
    & $Change $values
    $file = Write-PlanFile -Name 'variant' -Values $values
    Test-PlanRejected -Name $Name -Code $Code -File $file
}

# ==== Read-WindowsSqlPlan: accepted plans =====================================================================
$now = [DateTimeOffset]::UtcNow
$inTwoHours = $now.AddHours(2)
$acceptedExpiries = @(
    @{ Name = 'Z offset two hours ahead'; Text = $inTwoHours.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") },
    @{ Name = '+09:00 offset of the same instant'
        Text = $inTwoHours.ToOffset([TimeSpan]::FromHours(9)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
    # Read as local wall time this instant would look hours earlier; the original offset keeps it in the future.
    @{ Name = '-07:00 offset of the same instant'
        Text = $inTwoHours.ToOffset([TimeSpan]::FromHours(-7)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") }
)
foreach ($case in $acceptedExpiries) {
    $file = Write-PlanFile -Name 'valid' -Values (New-ValidPlanValues -ExpiresUtc $case.Text)
    $read = Invoke-PlanRead -File $file
    $accepted = $null -eq $read.Capture.Error -and $null -ne $read.Plan
    $hashRecorded = (Get-FieldValue -Object $read.Plan -Name 'PlanHash') -ceq $file.Hash
    $noExternal = $read.External.Count -eq 0
    Assert-True -Name ('PLAN-01 valid approved plan is accepted with PlanHash recorded: ' + $case.Name) `
        -Condition ($accepted -and $hashRecorded -and $noExternal) `
        -Detail ('error <{0}> external <{1}>' -f $read.Capture.Error, ($read.External -join ','))
}

# The plan names another machine; comparing it with the real host belongs to the adapter at step time.
$otherMachineFile = Write-PlanFile -Name 'other-machine' -Values (New-ValidPlanValues)
$otherMachineRead = Invoke-PlanRead -File $otherMachineFile
$namesOtherMachine = $fixtureMachine -cne [Environment]::MachineName
$otherMachineAccepted = $null -eq $otherMachineRead.Capture.Error -and $null -ne $otherMachineRead.Plan
Assert-True -Name 'PLAN-02 reader does not compare MachineName with this host (machine check is adapter-owned)' `
    -Condition ($namesOtherMachine -and $otherMachineAccepted) `
    -Detail ('fixture machine differs from host: {0}; error <{1}>' -f
    $namesOtherMachine, $otherMachineRead.Capture.Error)

# ==== Read-WindowsSqlPlan: identity of the plan file =========================================================
$identityFile = Write-PlanFile -Name 'identity' -Values (New-ValidPlanValues)
Test-PlanRejected -Name 'PLAN-03 lower-case expected hash' -Code 'WINDOWS_SQL_PLAN_IDENTITY_REQUIRED' `
    -File $identityFile -ExpectedHash $identityFile.Hash.ToLowerInvariant()
Test-PlanRejected -Name 'PLAN-04 expected hash of 63 characters' -Code 'WINDOWS_SQL_PLAN_IDENTITY_REQUIRED' `
    -File $identityFile -ExpectedHash $identityFile.Hash.Substring(1)
Test-PlanRejected -Name 'PLAN-05 expected hash of other content' -Code 'WINDOWS_SQL_PLAN_CHANGED' `
    -File $identityFile -ExpectedHash ('0' * 64)

# Trailing JSON whitespace keeps the document valid, so only the size bound can reject it.
$validJson = ConvertTo-Json -InputObject (New-ValidPlanValues) -Depth 8
$oversizeBytes = [Text.UTF8Encoding]::new($false).GetBytes($validJson + (' ' * 70000))
$oversizeFile = Write-PlanBytes -Name 'oversize' -Bytes $oversizeBytes
Test-PlanRejected -Name 'PLAN-06 plan larger than 65536 bytes' -Code 'WINDOWS_SQL_PLAN_TOO_LARGE' -File $oversizeFile
$invalidUtf8File = Write-PlanBytes -Name 'invalid-utf8' -Bytes ([byte[]](0xFF, 0xFE, 0x7B, 0x00, 0x7D, 0x00))
Test-PlanRejected -Name 'PLAN-07 bytes that are not strict UTF-8' -Code 'WINDOWS_SQL_PLAN_INVALID' `
    -File $invalidUtf8File
$arrayFile = Write-PlanBytes -Name 'array' -Bytes ([Text.UTF8Encoding]::new($false).GetBytes('[1]'))
Test-PlanRejected -Name 'PLAN-08 JSON that is not an object' -Code 'WINDOWS_SQL_PLAN_INVALID' -File $arrayFile

# ==== Read-WindowsSqlPlan: version, approval and expiry ======================================================
Test-PlanVariantRejected -Name 'PLAN-09 version 2' -Code 'WINDOWS_SQL_PLAN_VERSION_UNSUPPORTED' `
    -Change { param($p) $p.Version = 2 }
Test-PlanVariantRejected -Name 'PLAN-10 version as the string "1"' -Code 'WINDOWS_SQL_PLAN_VERSION_UNSUPPORTED' `
    -Change { param($p) $p.Version = '1' }
Test-PlanVariantRejected -Name 'PLAN-11 ExecutionApproved false' -Code 'WINDOWS_SQL_EXECUTION_NOT_APPROVED' `
    -Change { param($p) $p.ExecutionApproved = $false }
Test-PlanVariantRejected -Name 'PLAN-12 ExecutionApproved as the string "true"' `
    -Code 'WINDOWS_SQL_EXECUTION_NOT_APPROVED' -Change { param($p) $p.ExecutionApproved = 'true' }
Test-PlanVariantRejected -Name 'PLAN-13 expiry two minutes past' -Code 'WINDOWS_SQL_APPROVAL_EXPIRED' `
    -Change { param($p) $p.ExpiresUtc = [DateTimeOffset]::UtcNow.AddMinutes(-2).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") }
Test-PlanVariantRejected -Name 'PLAN-14 expiry more than 24 hours ahead' -Code 'WINDOWS_SQL_APPROVAL_EXPIRED' `
    -Change { param($p) $p.ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(25).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") }
Test-PlanVariantRejected -Name 'PLAN-15 expiry without Z or offset' -Code 'WINDOWS_SQL_PLAN_EXPIRY_INVALID' `
    -Change { param($p) $p.ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(2).ToString("yyyy-MM-dd'T'HH:mm:ss") }
Test-PlanVariantRejected -Name 'PLAN-16 expiry missing' -Code 'WINDOWS_SQL_PLAN_EXPIRY_INVALID' `
    -Change { param($p) $p.Remove('ExpiresUtc') }

# ==== Read-WindowsSqlPlan: presence and type of identity and target fields ===================================
$fieldCases = @(
    @{ Name = 'PLAN-17 MachineName missing'; Change = { param($p) $p.Remove('MachineName') } },
    @{ Name = 'PLAN-18 MachineName empty'; Change = { param($p) $p.MachineName = '' } },
    @{ Name = 'PLAN-19 InstanceName missing'; Change = { param($p) $p.Remove('InstanceName') } },
    @{ Name = 'PLAN-20 InstanceId as a number'; Change = { param($p) $p.InstanceId = 17 } },
    @{ Name = 'PLAN-21 ExpectedProductVersion with two parts'
        Change = { param($p) $p.ExpectedProductVersion = '17.0' } },
    @{ Name = 'PLAN-22 Port as a string'; Change = { param($p) $p.Port = '14333' } },
    @{ Name = 'PLAN-23 Database null'; Change = { param($p) $p.Database = $null } },
    @{ Name = 'PLAN-24 RuntimeLogin missing'; Change = { param($p) $p.Remove('RuntimeLogin') } },
    @{ Name = 'PLAN-25 LifetimeId upper-case'
        Change = { param($p) $p.LifetimeId = $fixtureLifetime.ToUpperInvariant() } },
    @{ Name = 'PLAN-26 LifetimeId of 31 characters'
        Change = { param($p) $p.LifetimeId = $fixtureLifetime.Substring(1) } },
    @{ Name = 'PLAN-27 SchemaVersion as a string'; Change = { param($p) $p.SchemaVersion = '4' } },
    @{ Name = 'PLAN-28 CertificateThumbprint lower-case'
        Change = { param($p) $p.CertificateThumbprint = $fixtureThumbprint.ToLowerInvariant() } },
    @{ Name = 'PLAN-29 CertificateThumbprint of 39 characters'
        Change = { param($p) $p.CertificateThumbprint = $fixtureThumbprint.Substring(1) } },
    @{ Name = 'PLAN-30 RestartApproved as the string "true"'; Change = { param($p) $p.RestartApproved = 'true' } },
    @{ Name = 'PLAN-31 FirewallApproved missing'; Change = { param($p) $p.Remove('FirewallApproved') } }
)
foreach ($case in $fieldCases) {
    Test-PlanVariantRejected -Name $case.Name -Code 'WINDOWS_SQL_PLAN_FIELD_INVALID' -Change $case.Change
}

# ==== Read-WindowsSqlPlan: names that would carry SQL or shell syntax ========================================
$nameCases = @(
    @{ Name = 'PLAN-32 Database with a closing bracket and statement'
        Change = { param($p) $p.Database = 'Dawnholder_Demo]; DROP DATABASE x;--' } },
    @{ Name = 'PLAN-33 Database with a quote'; Change = { param($p) $p.Database = "Dawnholder_Demo'" } },
    @{ Name = 'PLAN-34 Database without the Dawnholder_ prefix'; Change = { param($p) $p.Database = 'master' } },
    @{ Name = 'PLAN-35 RuntimeLogin with a semicolon'; Change = { param($p) $p.RuntimeLogin = 'dh_demo;calc' } },
    @{ Name = 'PLAN-36 RuntimeLogin with a subexpression'; Change = { param($p) $p.RuntimeLogin = 'dh_$(whoami)' } },
    @{ Name = 'PLAN-37 RuntimeLogin sa'; Change = { param($p) $p.RuntimeLogin = 'sa' } },
    @{ Name = 'PLAN-38 InstanceName with a semicolon'; Change = { param($p) $p.InstanceName = 'SQLEXPRESS;calc' } },
    @{ Name = 'PLAN-39 InstanceId for another instance name'; Change = { param($p) $p.InstanceId = 'MSSQL17.OTHER' } },
    @{ Name = 'PLAN-40 InstanceId with an ampersand'
        Change = { param($p) $p.InstanceId = 'MSSQL17.SQLEXPRESS&calc' } },
    @{ Name = 'PLAN-41 MachineName with a pipe'; Change = { param($p) $p.MachineName = 'LAPTOP|calc' } }
)
foreach ($case in $nameCases) {
    Test-PlanVariantRejected -Name $case.Name -Code 'WINDOWS_SQL_NAME_INVALID' -Change $case.Change
}

# ==== Read-WindowsSqlPlan: port range ========================================================================
foreach ($port in @(0, 1023, 65536)) {
    $portValue = $port
    Test-PlanVariantRejected -Name ('PLAN-42 Port {0} outside 1024..65535' -f $portValue) `
        -Code 'WINDOWS_SQL_PORT_INVALID' -Change ({ param($p) $p.Port = $portValue }.GetNewClosure())
}

# ==== Read-WindowsSqlPlan: TLS, sa and global changes stay fixed =============================================
$fixedValueCases = @(
    @{ Name = 'PLAN-43 Encrypt Optional'; Code = 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
        Change = { param($p) $p.Encrypt = 'Optional' } },
    @{ Name = 'PLAN-44 Encrypt as boolean true'; Code = 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
        Change = { param($p) $p.Encrypt = $true } },
    @{ Name = 'PLAN-45 Encrypt missing'; Code = 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
        Change = { param($p) $p.Remove('Encrypt') } },
    @{ Name = 'PLAN-46 TrustServerCertificate true'; Code = 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
        Change = { param($p) $p.TrustServerCertificate = $true } },
    @{ Name = 'PLAN-47 TrustServerCertificate as the string "false"'; Code = 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
        Change = { param($p) $p.TrustServerCertificate = 'false' } },
    @{ Name = 'PLAN-48 TrustServerCertificate missing'; Code = 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
        Change = { param($p) $p.Remove('TrustServerCertificate') } },
    @{ Name = 'PLAN-49 EnableSa true'; Code = 'WINDOWS_SQL_SA_REJECTED'
        Change = { param($p) $p.EnableSa = $true } },
    @{ Name = 'PLAN-50 EnableSa missing'; Code = 'WINDOWS_SQL_SA_REJECTED'
        Change = { param($p) $p.Remove('EnableSa') } },
    @{ Name = 'PLAN-51 SwitchWslNetworkingMode true'; Code = 'WINDOWS_SQL_GLOBAL_CHANGE_REJECTED'
        Change = { param($p) $p.SwitchWslNetworkingMode = $true } },
    @{ Name = 'PLAN-52 SwitchWslNetworkingMode missing'; Code = 'WINDOWS_SQL_GLOBAL_CHANGE_REJECTED'
        Change = { param($p) $p.Remove('SwitchWslNetworkingMode') } },
    @{ Name = 'PLAN-53 ForceEncryption Enable'; Code = 'WINDOWS_SQL_GLOBAL_CHANGE_REJECTED'
        Change = { param($p) $p.ForceEncryption = 'Enable' } },
    @{ Name = 'PLAN-54 ForceEncryption Disable'; Code = 'WINDOWS_SQL_GLOBAL_CHANGE_REJECTED'
        Change = { param($p) $p.ForceEncryption = 'Disable' } },
    @{ Name = 'PLAN-55 ForceEncryption missing'; Code = 'WINDOWS_SQL_GLOBAL_CHANGE_REJECTED'
        Change = { param($p) $p.Remove('ForceEncryption') } }
)
foreach ($case in $fixedValueCases) {
    Test-PlanVariantRejected -Name $case.Name -Code $case.Code -Change $case.Change
}

# ==== Read-WindowsSqlPlan: no secret field, and the rejected value is never echoed ===========================
$secretValues = New-ValidPlanValues
$secretValues.RuntimePassword = $secretSentinel
$secretFile = Write-PlanFile -Name 'secret-field' -Values $secretValues
$secretRead = Invoke-PlanRead -File $secretFile
$secretRejected = $secretRead.Capture.Error -ceq 'WINDOWS_SQL_PLAN_FIELD_UNKNOWN'
$secretNotEchoed = -not (Get-CaptureText -Capture $secretRead.Capture).Contains($secretSentinel)
Assert-True -Name 'PLAN-56 plan with a RuntimePassword field: rejected as WINDOWS_SQL_PLAN_FIELD_UNKNOWN' `
    -Condition $secretRejected -Detail ('error <{0}>' -f $secretRead.Capture.Error)
Assert-True -Name 'PLAN-57 plan with a RuntimePassword field: error and every output stream omit the value' `
    -Condition ($secretRejected -and $secretNotEchoed) `
    -Detail ('rejected {0}; not echoed {1}; error <{2}>' -f
    $secretRejected, $secretNotEchoed, $secretRead.Capture.Error)

# ==== Read-WindowsSqlPlan: PrivateRoot, EvidenceRoot and SourceRoot ==========================================
$pathCases = @(
    @{ Name = 'PLAN-58 EvidenceRoot outside SourceRoot\.backups'; Code = 'WINDOWS_SQL_PATH_INVALID'
        Change = { param($p) $p.EvidenceRoot = Join-Path $script:SuiteRoot 'evidence-outside' } },
    @{ Name = 'PLAN-59 EvidenceRoot relative'; Code = 'WINDOWS_SQL_PATH_INVALID'
        Change = { param($p) $p.EvidenceRoot = '.backups\windows-sql-evidence' } },
    @{ Name = 'PLAN-60 PrivateRoot outside %LOCALAPPDATA%\Dawnholder'; Code = 'WINDOWS_SQL_PATH_INVALID'
        Change = { param($p) $p.PrivateRoot = Join-Path $script:SuiteRoot 'private-outside' } },
    @{ Name = 'PLAN-61 PrivateRoot two levels below %LOCALAPPDATA%\Dawnholder'; Code = 'WINDOWS_SQL_PATH_INVALID'
        Change = { param($p) $p.PrivateRoot = Join-Path $env:LOCALAPPDATA 'Dawnholder\windows-sql-fixture\nested' } },
    @{ Name = 'PLAN-62 PrivateRoot relative'; Code = 'WINDOWS_SQL_PATH_INVALID'
        Change = { param($p) $p.PrivateRoot = 'Dawnholder\windows-sql-fixture' } }
)
foreach ($case in $pathCases) {
    Test-PlanVariantRejected -Name $case.Name -Code $case.Code -Change $case.Change
}

# A source tree inside the private directory puts EvidenceRoot inside PrivateRoot; real inputs keep the
# input check satisfied so the conflict alone decides the result.
$conflictPrivate = Join-Path $env:LOCALAPPDATA 'Dawnholder\windows-sql-conflict'
$conflictSource = Join-Path $conflictPrivate 'source'
$conflictEvidence = Join-Path $conflictSource '.backups\windows-sql-evidence'
[void][IO.Directory]::CreateDirectory($conflictEvidence)
$conflictInputs = New-SourceInputs -Root $conflictSource
$conflictValues = New-ValidPlanValues
$conflictValues.PrivateRoot = $conflictPrivate
$conflictValues.SourceRoot = $conflictSource
$conflictValues.EvidenceRoot = $conflictEvidence
$conflictValues.Inputs = @($conflictInputs | ForEach-Object { [ordered]@{ Path = $_.Path; Sha256 = $_.Sha256 } })
$conflictFile = Write-PlanFile -Name 'path-conflict' -Values $conflictValues
Test-PlanRejected -Name 'PLAN-63 EvidenceRoot inside PrivateRoot' -Code 'WINDOWS_SQL_PATH_CONFLICT' -File $conflictFile

# ==== Read-WindowsSqlPlan: reviewed source inputs ============================================================
$inputCases = @(
    @{ Name = 'PLAN-64 input path with ..'; Code = 'WINDOWS_SQL_INPUT_PATH_INVALID'
        Change = { param($p) $p.Inputs[0].Path = '../outside.txt' } },
    @{ Name = 'PLAN-65 absolute input path'; Code = 'WINDOWS_SQL_INPUT_PATH_INVALID'
        Change = { param($p) $p.Inputs[0].Path = 'C:/outside.txt' } },
    @{ Name = 'PLAN-66 duplicate input path'; Code = 'WINDOWS_SQL_INPUT_PATH_INVALID'
        Change = { param($p) $p.Inputs[1].Path = $p.Inputs[0].Path; $p.Inputs[1].Sha256 = $p.Inputs[0].Sha256 } },
    @{ Name = 'PLAN-67 input hash in lower case'; Code = 'WINDOWS_SQL_INPUT_PATH_INVALID'
        Change = { param($p) $p.Inputs[0].Sha256 = $p.Inputs[0].Sha256.ToLowerInvariant() } },
    @{ Name = 'PLAN-68 empty input list'; Code = 'WINDOWS_SQL_INPUT_SET_INVALID'
        Change = { param($p) $p.Inputs = @() } },
    @{ Name = 'PLAN-69 input whose file content changed'; Code = 'WINDOWS_SQL_INPUT_CHANGED'
        Change = { param($p) $p.Inputs[2].Sha256 = 'F' * 64 } }
)
foreach ($case in $inputCases) {
    Test-PlanVariantRejected -Name $case.Name -Code $case.Code -Change $case.Change
}

# ==== Get-WindowsSqlChangePlan ===============================================================================
# Observation sections match what Invoke-WindowsSqlStep assembles from adapter reads (see the lifecycle suite).
function New-FreshObservation {
    return [ordered]@{
        Machine = [ordered]@{ MachineName = $fixtureMachine }
        Instance = [ordered]@{
            InstanceName = $fixtureInstance
            InstanceId = $fixtureInstanceId
            ProductVersion = $fixtureVersion
        }
        Configuration = [ordered]@{
            LoginMode = 'Windows'
            TcpEnabled = $false
            TcpPort = $null
            DynamicPorts = $false
            ForceEncryption = $false
            CertificateThumbprint = $null
        }
        Port = [ordered]@{ Port = $fixturePort; InUse = $false; OwnedByInstance = $false }
        Network = [ordered]@{ Mode = 'Nat'; WslSubnet = $fixtureWslSubnet; OwnedFirewallRule = $null }
        Database = [ordered]@{
            Name = $fixtureDatabase
            Exists = $false
            OwnedLifetimeId = $null
            SchemaVersion = $null
        }
        Login = [ordered]@{ Name = $fixtureLogin; Exists = $false; OwnedLifetimeId = $null }
    }
}

function Set-TargetConfiguration {
    param([Parameter(Mandatory)]$Observation)
    $Observation.Configuration = [ordered]@{
        LoginMode = 'Mixed'
        TcpEnabled = $true
        TcpPort = $fixturePort
        DynamicPorts = $false
        ForceEncryption = $false
        CertificateThumbprint = $fixtureThumbprint
    }
    $Observation.Port = [ordered]@{ Port = $fixturePort; InUse = $true; OwnedByInstance = $true }
}

$expectedFreshNatChanges = @(
    'SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService', 'AddFirewallRule',
    'CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin'
)
$expectedFreshMirroredChanges = @(
    'SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService',
    'CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin'
)
$expectedTargetConfiguration = [ordered]@{
    LoginMode = 'Mixed'
    TcpEnabled = $true
    TcpPort = $fixturePort
    DynamicPorts = $false
    ForceEncryption = $false
    CertificateThumbprint = $fixtureThumbprint
}

function Read-CasePlan {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Change
    )
    $values = New-ValidPlanValues
    & $Change $values
    $file = Write-PlanFile -Name $Name -Values $values
    $read = Invoke-PlanRead -File $file
    return [pscustomobject]@{ Plan = $read.Plan; Error = $read.Capture.Error }
}
$plans = @{
    Approved = Read-CasePlan -Name 'change-approved' -Change { param($p) }
    NoRestart = Read-CasePlan -Name 'change-no-restart' -Change { param($p) $p.RestartApproved = $false }
    NoFirewall = Read-CasePlan -Name 'change-no-firewall' -Change { param($p) $p.FirewallApproved = $false }
    NoApprovals = Read-CasePlan -Name 'change-no-approvals' -Change {
        param($p)
        $p.RestartApproved = $false
        $p.FirewallApproved = $false
    }
}

function Invoke-ChangePlanCase {
    param(
        [Parameter(Mandatory)][string]$PlanName,
        [Parameter(Mandatory)]$Observation
    )
    $planCase = $plans[$PlanName]
    if ($null -eq $planCase.Plan) {
        return [pscustomobject]@{
            Result = $null
            Error = 'plan fixture not readable: ' + [string]$planCase.Error
            External = @()
        }
    }
    $changePlan = $planCase.Plan
    $changeObservation = ConvertTo-FixtureObject -Value $Observation
    $script:ExternalCalls.Clear()
    $capture = Invoke-Captured -CapturedAction {
        Get-WindowsSqlChangePlan -Plan $changePlan -Observation $changeObservation
    }
    return [pscustomobject]@{
        Result = Get-ReturnedObject -Records $capture.Records
        Error = $capture.Error
        External = @($script:ExternalCalls.ToArray())
    }
}

function Get-CaseDetail {
    param($Case)
    return ('error <{0}> decision <{1}> changes <{2}> blockers <{3}> external <{4}>' -f
        $Case.Error,
        (Get-FieldValue -Object $Case.Result -Name 'Decision'),
        (Join-SortedNames -Names @(Get-FieldValue -Object $Case.Result -Name 'Changes')),
        (Join-SortedNames -Names @(Get-FieldValue -Object $Case.Result -Name 'Blockers')),
        ($Case.External -join ','))
}

function Test-ChangePlanBlocked {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Code,
        [Parameter(Mandatory)][string]$PlanName,
        [Parameter(Mandatory)]$Observation
    )
    $case = Invoke-ChangePlanCase -PlanName $PlanName -Observation $Observation
    $returned = $null -eq $case.Error -and $null -ne $case.Result
    $blocked = (Get-FieldValue -Object $case.Result -Name 'Decision') -ceq 'Blocked'
    $hasCode = @(Get-FieldValue -Object $case.Result -Name 'Blockers') -ccontains $Code
    $noExternal = $case.External.Count -eq 0
    Assert-True -Name ('{0}: returns Decision Blocked with {1} and no external call' -f $Name, $Code) `
        -Condition ($returned -and $blocked -and $hasCode -and $noExternal) -Detail (Get-CaseDetail -Case $case)
}

# CP-01 fresh laptop instance (Windows authentication, TCP off, no certificate) under WSL NAT.
$freshNat = Invoke-ChangePlanCase -PlanName 'Approved' -Observation (New-FreshObservation)
$freshNatReturned = $null -eq $freshNat.Error -and $null -ne $freshNat.Result
$freshNatDetail = Get-CaseDetail -Case $freshNat
Assert-True -Name 'CP-01 fresh instance under NAT: Decision Apply without external calls' `
    -Condition ($freshNatReturned -and (Get-FieldValue -Object $freshNat.Result -Name 'Decision') -ceq 'Apply' -and
    $freshNat.External.Count -eq 0) -Detail $freshNatDetail
Assert-EqualInCase -Name 'CP-01 fresh instance under NAT: exact change set' `
    -Expected (Join-SortedNames -Names $expectedFreshNatChanges) `
    -Actual (Join-SortedNames -Names @(Get-FieldValue -Object $freshNat.Result -Name 'Changes')) `
    -Context $freshNatDetail
Assert-Equal -Name 'CP-01 fresh NAT: TargetConfiguration keeps ForceEncryption and names the plan port/cert' `
    -Expected (ConvertTo-CanonicalJson -Value $expectedTargetConfiguration) `
    -Actual $(if ($freshNatReturned) {
        ConvertTo-CanonicalJson -Value (Get-FieldValue -Object $freshNat.Result -Name 'TargetConfiguration')
    } else { $freshNatDetail })
Assert-EqualInCase -Name 'CP-01 the decision reports a provided observation, not a live instance check' `
    -Expected 'Provided' -Actual (Get-FieldValue -Object $freshNat.Result -Name 'ObservationSource') `
    -Context $freshNatDetail

# CP-02 the same instance under mirrored networking needs no firewall rule.
$mirroredObservation = New-FreshObservation
$mirroredObservation.Network = [ordered]@{ Mode = 'Mirrored'; WslSubnet = $null; OwnedFirewallRule = $null }
$freshMirrored = Invoke-ChangePlanCase -PlanName 'Approved' -Observation $mirroredObservation
$freshMirroredDecision = Get-FieldValue -Object $freshMirrored.Result -Name 'Decision'
Assert-True -Name 'CP-02 fresh instance under mirrored networking: Decision Apply' `
    -Condition ($null -eq $freshMirrored.Error -and $freshMirroredDecision -ceq 'Apply') `
    -Detail (Get-CaseDetail -Case $freshMirrored)
Assert-EqualInCase -Name 'CP-02 fresh instance under mirrored networking: exact change set without AddFirewallRule' `
    -Expected (Join-SortedNames -Names $expectedFreshMirroredChanges) `
    -Actual (Join-SortedNames -Names @(Get-FieldValue -Object $freshMirrored.Result -Name 'Changes')) `
    -Context (Get-CaseDetail -Case $freshMirrored)

# CP-03 ForceEncryption already on stays on; no change ever touches it.
$forceOnObservation = New-FreshObservation
$forceOnObservation.Configuration.ForceEncryption = $true
$forceOn = Invoke-ChangePlanCase -PlanName 'Approved' -Observation $forceOnObservation
$forceOnTarget = Get-FieldValue -Object $forceOn.Result -Name 'TargetConfiguration'
$forceOnPreserved = (Get-FieldValue -Object $forceOnTarget -Name 'ForceEncryption') -eq $true
$forceOnChanges = @(Get-FieldValue -Object $forceOn.Result -Name 'Changes')
$forceOnUntouched = @($forceOnChanges | Where-Object { [string]$_ -match 'ForceEncryption' }).Count -eq 0
Assert-True -Name 'CP-03 ForceEncryption observed on: TargetConfiguration keeps it on and no change names it' `
    -Condition ($null -eq $forceOn.Error -and $null -ne $forceOnTarget -and $forceOnPreserved -and $forceOnUntouched) `
    -Detail (Get-CaseDetail -Case $forceOn)

# CP-04 instance already configured, mirrored networking, database absent: approvals are only needed when used.
$configuredObservation = New-FreshObservation
Set-TargetConfiguration -Observation $configuredObservation
$configuredObservation.Network = [ordered]@{ Mode = 'Mirrored'; WslSubnet = $null; OwnedFirewallRule = $null }
$configured = Invoke-ChangePlanCase -PlanName 'NoApprovals' -Observation $configuredObservation
$configuredDecision = Get-FieldValue -Object $configured.Result -Name 'Decision'
Assert-True -Name 'CP-04 configured instance with no restart/firewall approval: Decision Apply (nothing needs them)' `
    -Condition ($null -eq $configured.Error -and $configuredDecision -ceq 'Apply') `
    -Detail (Get-CaseDetail -Case $configured)
Assert-EqualInCase -Name 'CP-04 configured instance: only the new target database and login are created' `
    -Expected (Join-SortedNames -Names @('ApplyMigrations', 'CreateDatabase', 'CreateRuntimeLogin')) `
    -Actual (Join-SortedNames -Names @(Get-FieldValue -Object $configured.Result -Name 'Changes')) `
    -Context (Get-CaseDetail -Case $configured)

# CP-05 rerun after a complete apply: same owner and version means no reinitialisation or recreation.
$rerunObservation = New-FreshObservation
Set-TargetConfiguration -Observation $rerunObservation
$rerunObservation.Network.OwnedFirewallRule = [ordered]@{ RemoteAddress = $fixtureWslSubnet; Port = $fixturePort }
$rerunObservation.Database = [ordered]@{
    Name = $fixtureDatabase
    Exists = $true
    OwnedLifetimeId = $fixtureLifetime
    SchemaVersion = 4
}
$rerunObservation.Login = [ordered]@{ Name = $fixtureLogin; Exists = $true; OwnedLifetimeId = $fixtureLifetime }
$rerun = Invoke-ChangePlanCase -PlanName 'Approved' -Observation $rerunObservation
Assert-True -Name 'CP-05 rerun with matching owner and version: Decision NoChange and an empty change list' `
    -Condition ($null -eq $rerun.Error -and (Get-FieldValue -Object $rerun.Result -Name 'Decision') -ceq 'NoChange' -and
    @(Get-FieldValue -Object $rerun.Result -Name 'Changes').Count -eq 0) -Detail (Get-CaseDetail -Case $rerun)

# CP-06..13 identity, port and ownership conflicts.
$machineMismatch = New-FreshObservation
$machineMismatch.Machine.MachineName = 'OTHER-HOST'
Test-ChangePlanBlocked -Name 'CP-06 machine name differs from the plan' -Code 'WINDOWS_SQL_IDENTITY_MISMATCH' `
    -PlanName 'Approved' -Observation $machineMismatch
$instanceMismatch = New-FreshObservation
$instanceMismatch.Instance.InstanceId = 'MSSQL16.SQLEXPRESS'
Test-ChangePlanBlocked -Name 'CP-07 instance id differs from the plan' -Code 'WINDOWS_SQL_IDENTITY_MISMATCH' `
    -PlanName 'Approved' -Observation $instanceMismatch
$versionMismatch = New-FreshObservation
$versionMismatch.Instance.ProductVersion = '17.0.1000.6'
Test-ChangePlanBlocked -Name 'CP-08 product version differs from the plan' -Code 'WINDOWS_SQL_IDENTITY_MISMATCH' `
    -PlanName 'Approved' -Observation $versionMismatch
$portConflict = New-FreshObservation
$portConflict.Port = [ordered]@{ Port = $fixturePort; InUse = $true; OwnedByInstance = $false }
Test-ChangePlanBlocked -Name 'CP-09 plan port is used by another listener' -Code 'WINDOWS_SQL_PORT_CONFLICT' `
    -PlanName 'Approved' -Observation $portConflict
$foreignDatabase = New-FreshObservation
$foreignDatabase.Database = [ordered]@{
    Name = $fixtureDatabase
    Exists = $true
    OwnedLifetimeId = $null
    SchemaVersion = $null
}
Test-ChangePlanBlocked -Name 'CP-10 target database exists without this tool''s owner mark' `
    -Code 'WINDOWS_SQL_TARGET_NOT_OWNED' -PlanName 'Approved' -Observation $foreignDatabase
$foreignLogin = New-FreshObservation
$foreignLogin.Login = [ordered]@{ Name = $fixtureLogin; Exists = $true; OwnedLifetimeId = $null }
Test-ChangePlanBlocked -Name 'CP-11 runtime login exists without this tool''s owner mark' `
    -Code 'WINDOWS_SQL_TARGET_NOT_OWNED' -PlanName 'Approved' -Observation $foreignLogin
$otherLifetime = New-FreshObservation
$otherLifetime.Database = [ordered]@{
    Name = $fixtureDatabase
    Exists = $true
    OwnedLifetimeId = 'fedcba9876543210fedcba9876543210'
    SchemaVersion = 4
}
Test-ChangePlanBlocked -Name 'CP-12 target database owned by another lifetime' `
    -Code 'WINDOWS_SQL_OWNER_OR_VERSION_MISMATCH' -PlanName 'Approved' -Observation $otherLifetime
$otherVersion = New-FreshObservation
$otherVersion.Database = [ordered]@{
    Name = $fixtureDatabase
    Exists = $true
    OwnedLifetimeId = $fixtureLifetime
    SchemaVersion = 3
}
Test-ChangePlanBlocked -Name 'CP-13 owned target database at another schema version' `
    -Code 'WINDOWS_SQL_OWNER_OR_VERSION_MISMATCH' -PlanName 'Approved' -Observation $otherVersion

# CP-14 observations that cannot be classified never become a plan to apply.
$unclearCases = @(
    @{ Name = 'login mode Unknown'; Change = { param($o) $o.Configuration.LoginMode = 'Unknown' } },
    @{ Name = 'TcpEnabled as the string "true"'; Change = { param($o) $o.Configuration.TcpEnabled = 'true' } },
    @{ Name = 'database existence null'; Change = { param($o) $o.Database.Exists = $null } },
    @{ Name = 'network mode missing'; Change = { param($o) $o.Network.Remove('Mode') } }
)
foreach ($case in $unclearCases) {
    $observation = New-FreshObservation
    & $case.Change $observation
    Test-ChangePlanBlocked -Name ('CP-14 unclear observation: ' + $case.Name) -Code 'WINDOWS_SQL_OBSERVATION_UNCLEAR' `
        -PlanName 'Approved' -Observation $observation
}

# CP-15..16 restart and firewall changes need their own approval in the hashed plan.
Test-ChangePlanBlocked -Name 'CP-15 restart needed without RestartApproved' -Code 'WINDOWS_SQL_RESTART_NOT_APPROVED' `
    -PlanName 'NoRestart' -Observation (New-FreshObservation)
Test-ChangePlanBlocked -Name 'CP-16 NAT firewall rule needed without FirewallApproved' `
    -Code 'WINDOWS_SQL_FIREWALL_NOT_APPROVED' -PlanName 'NoFirewall' -Observation (New-FreshObservation)

# CP-17 the rule may admit only the observed WSL range (a /20 or narrower CIDR), never a LAN-wide scope.
foreach ($scope in @('Any', 'LocalSubnet', '0.0.0.0/0', '172.16.0.0/12')) {
    $scopeObservation = New-FreshObservation
    $scopeObservation.Network.WslSubnet = $scope
    Test-ChangePlanBlocked -Name ('CP-17 observed WSL range ' + $scope) -Code 'WINDOWS_SQL_FIREWALL_SCOPE_INVALID' `
        -PlanName 'Approved' -Observation $scopeObservation
}

# ==== Public signatures and decision boundaries ==============================================================
# These structural checks support the behaviour cases above; they never stand in for them.
$commonParameters = @([Management.Automation.Cmdlet]::CommonParameters) +
@([Management.Automation.Cmdlet]::OptionalCommonParameters)
function Get-PublicParameterNames {
    param([Parameter(Mandatory)][string]$Function)
    $command = Get-Command -Name $Function -CommandType Function -ErrorAction Stop
    return Join-SortedNames -Names @($command.Parameters.Keys | Where-Object { $_ -notin $commonParameters })
}
foreach ($signature in @(
        @{ Id = 'STRUCT-01'; Function = 'Read-WindowsSqlPlan'; Expected = @('ExpectedHash', 'Path') },
        @{ Id = 'STRUCT-02'; Function = 'Get-WindowsSqlChangePlan'; Expected = @('Observation', 'Plan') }
    )) {
    $functionName = $signature.Function
    $actualNames = $null
    $signatureError = Get-ThrownMessage -Action {
        $script:SignatureNames = Get-PublicParameterNames -Function $functionName
    }
    if ($null -eq $signatureError) { $actualNames = $script:SignatureNames }
    Assert-Equal -Name ('{0} {1} exposes exactly these parameters' -f $signature.Id, $functionName) `
        -Expected (Join-SortedNames -Names $signature.Expected) `
        -Actual $(if ($null -eq $signatureError) { $actualNames } else { 'unavailable: ' + $signatureError })
}

$forbiddenCommands = @($externalCommands)
$forbiddenTypePattern = 'ServiceController|Microsoft\.Win32\.Registry|RegistryKey|SqlConnection|SqlCommand|X509Store|' +
'Diagnostics\.Process|ProcessStartInfo|ManagementObject'
function Get-DirectBoundaryUses {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Function
    )
    # Direct uses only: decisions must reach services, registry, certificates, SQL and processes through the adapter.
    $tokens = $null
    $parseErrors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$parseErrors)
    $definition = $ast.Find({
            param($node)
            $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $Function
        }, $true)
    if ($null -eq $definition) { throw ('function not defined: ' + $Function) }
    $uses = New-Object 'Collections.Generic.List[string]'
    foreach ($node in $definition.Body.FindAll({ param($item) $true }, $true)) {
        if ($node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -in $forbiddenCommands) {
            $uses.Add('command ' + $node.GetCommandName())
        }
        elseif (($node -is [Management.Automation.Language.TypeExpressionAst] -or
                $node -is [Management.Automation.Language.TypeConstraintAst]) -and
            $node.TypeName.FullName -match $forbiddenTypePattern) {
            $uses.Add('type ' + $node.TypeName.FullName)
        }
        elseif ($node -is [Management.Automation.Language.StringConstantExpressionAst] -and
            $node.Value -match '^(?i)(HKLM:|HKCU:|Registry::|Cert:)') {
            $uses.Add('path ' + $node.Value)
        }
    }
    return , @($uses.ToArray())
}
foreach ($functionName in @('Read-WindowsSqlPlan', 'Get-WindowsSqlChangePlan')) {
    $boundaryFunction = $functionName
    $uses = $null
    $boundaryError = if (-not [IO.File]::Exists($productPath)) { 'missing product file: ' + $productRelative } else {
        Get-ThrownMessage -Action {
            $script:BoundaryUses = Get-DirectBoundaryUses -Path $productPath -Function $boundaryFunction
        }
    }
    if ($null -eq $boundaryError) { $uses = @($script:BoundaryUses) }
    $boundaryName = 'STRUCT-03 {0} body makes no direct service/registry/certificate/SQL/process call' -f $functionName
    Assert-True -Name $boundaryName `
        -Condition ($null -eq $boundaryError -and $uses.Count -eq 0) `
        -Detail $(if ($null -ne $boundaryError) { $boundaryError } else { $uses -join '; ' })
}

Complete-TestSuite
