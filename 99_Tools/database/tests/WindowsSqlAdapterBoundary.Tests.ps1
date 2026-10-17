[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    # Optional absolute windows-sql folder of an isolated same-shaped database tree (before/after product runs).
    [string]$ProductRoot = ''
)
# Independent verification tests for the Windows SQL Express adapter, CLI and certificate entry points.
# - Real entry scripts run as child processes only with rejected input or another machine's approved plan.
# - The Preflight -> Plan -> Apply -> Verify -> Cleanup path runs the real core, native dispatcher, private store,
#   registry/firewall/SQL/WSL helpers and journal. Only protected facts and effects are replaced below those
#   helpers: registry keys, the SQL connection and statements, service/port/firewall cmdlets, WSL children,
#   DPAPI files and service control. Each fake records its call and returns the external state it simulates;
#   none of them recomputes a product decision. Expected values are constants taken from the requirement.
# - Fixture names LAPTOP-FIXTURE / DHFIXTURE never name this desktop or its SQL instance.
# Nothing here opens SQL, a certificate store, the registry, a service, the firewall or WSL.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-adapter-boundary'

# The -ProductRoot parameter is this same script-scope variable; empty means the checked-out windows-sql folder.
if (-not $ProductRoot) { $ProductRoot = Join-Path $script:ToolRoot 'windows-sql' }
elseif (-not [IO.Path]::IsPathRooted($ProductRoot)) { throw 'ProductRoot must be an explicit absolute path.' }
$script:ProductRoot = [IO.Path]::GetFullPath($ProductRoot)
if (-not [IO.File]::Exists((Join-Path $script:ProductRoot 'WindowsSql.Sql.ps1'))) {
    throw ('ProductRoot has no WindowsSql.Sql.ps1: ' + $script:ProductRoot)
}
$script:PwshPath = [Diagnostics.Process]::GetCurrentProcess().Path
$script:FixtureMachine = 'LAPTOP-FIXTURE'
$script:FixtureInstance = 'DHFIXTURE'
$script:FixtureInstanceId = 'MSSQL17.DHFIXTURE'
$script:FixtureVersion = '17.0.1000.7'
$script:FixturePort = 15433
$script:FixtureThumbprint = 'AB' * 20
if ([Environment]::MachineName -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run machine-rejection tests.'
}
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
try { $script:ExecutorSid = $currentIdentity.User.Value } finally { $currentIdentity.Dispose() }

# ---- Fail-closed guards: reaching any of these real commands is a test failure, never a side effect.
$script:ForbiddenCalls = [Collections.Generic.List[string]]::new()
foreach ($name in @('Restart-Service', 'Stop-Service', 'Start-Service', 'Set-Service', 'New-SelfSignedCertificate',
        'Import-PfxCertificate', 'Set-NetFirewallRule', 'Invoke-Sqlcmd', 'sqlcmd', 'netsh', 'reg', 'certutil',
        'sc.exe', 'docker', 'wsl', 'Start-Process', 'Get-ItemProperty', 'Set-ItemProperty', 'New-ItemProperty',
        'Remove-ItemProperty', 'Get-WmiObject', 'Invoke-CimMethod')) {
    Set-Item -Path ('Function:script:' + $name) -Value ([scriptblock]::Create(
            "`$script:ForbiddenCalls.Add('$name'); throw 'VERIFIER_FORBIDDEN_$name'"))
}

# The certificate gate reads key identity through the product's Add-Type class
# Dawnholder.WindowsSql.CertificateKeyNative (CERT_KEY_PROV_INFO, CryptAcquireCertificatePrivateKey) and reopens CSP
# keys with RSACryptoServiceProvider. Both names bind to these stand-ins and Add-Type refuses that class, so section 5
# compiles no C#, makes no P/Invoke and opens no key container. Provider facts come from the case, never from CAPI.
class AdapterFakeCertificateKeyNative {
    static [Collections.Generic.List[string]]$Calls = [Collections.Generic.List[string]]::new()
    static [hashtable]$ProviderInfo = @{}
    static [object]$ExpectedCertificate = $null

    static [object] ReadProviderParameters([object]$certificate) {
        $thumbprint = [string]$certificate.Thumbprint
        $same = [object]::ReferenceEquals($certificate, [AdapterFakeCertificateKeyNative]::ExpectedCertificate)
        [AdapterFakeCertificateKeyNative]::Calls.Add('ReadProviderParameters:' + $thumbprint + ':' + $same)
        if (-not [AdapterFakeCertificateKeyNative]::ProviderInfo.ContainsKey($thumbprint)) {
            throw 'VERIFIER_UNREPRESENTED_CertificateKeyProviderInfo'
        }
        return [AdapterFakeCertificateKeyNative]::ProviderInfo[$thumbprint]
    }

    static [void] AssertKeyBinding([object]$certificate) {
        [AdapterFakeCertificateKeyNative]::Calls.Add('AssertKeyBinding:' + [string]$certificate.Thumbprint)
        throw 'VERIFIER_FORBIDDEN_CertificateKeyBinding'
    }
}

class AdapterFakeCspKey {
    AdapterFakeCspKey([object]$parameters) {
        [AdapterFakeCertificateKeyNative]::Calls.Add('CspOpen:' + [string]$parameters.KeyContainerName)
        throw 'VERIFIER_FORBIDDEN_CspKeyOpen'
    }
}

# The directory lease compiles its own class as before; only a definition of the key native class is refused.
function Add-Type {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TypeDefinition)
    if ($TypeDefinition.Contains('CertificateKeyNative')) {
        [AdapterFakeCertificateKeyNative]::Calls.Add('Add-Type')
        throw 'VERIFIER_FORBIDDEN_Add-Type'
    }
    Microsoft.PowerShell.Utility\Add-Type -TypeDefinition $TypeDefinition -ErrorAction Stop
}

# Registered before section 1 parses product files: the parser resolves and caches type literal names, so a name
# parsed first would keep the real type.
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
$keyTypeNames = [ordered]@{
    'Dawnholder.WindowsSql.CertificateKeyNative' = [AdapterFakeCertificateKeyNative]
    'Security.Cryptography.RSACryptoServiceProvider' = [AdapterFakeCspKey]
    'System.Security.Cryptography.RSACryptoServiceProvider' = [AdapterFakeCspKey]
}
foreach ($typeName in $keyTypeNames.Keys) { $accelerators::Add($typeName, $keyTypeNames[$typeName]) }
# Parsed after registration like product code: both lookup forms, the Add-Type command and its refusal.
$keyProbe = & ([scriptblock]::Create(@'
$refused = $false
try { Add-Type -TypeDefinition 'namespace AdapterKeyProbe { public static class CertificateKeyNative {} }' }
catch { $refused = $true }
[pscustomobject]@{
    Lookup = ('Dawnholder.WindowsSql.CertificateKeyNative' -as [type]).FullName
    Native = [Dawnholder.WindowsSql.CertificateKeyNative].FullName
    Csp = [Security.Cryptography.RSACryptoServiceProvider].FullName
    SystemCsp = [System.Security.Cryptography.RSACryptoServiceProvider].FullName
    AddTypeFile = (Get-Command -Name 'Add-Type').ScriptBlock.File
    Refused = $refused
    ProbeLoaded = $null -ne ('AdapterKeyProbe.CertificateKeyNative' -as [type])
}
'@))
$keyBound = $keyProbe.Lookup -ceq 'AdapterFakeCertificateKeyNative' -and
$keyProbe.Native -ceq 'AdapterFakeCertificateKeyNative' -and $keyProbe.Csp -ceq 'AdapterFakeCspKey' -and
$keyProbe.SystemCsp -ceq 'AdapterFakeCspKey' -and $keyProbe.AddTypeFile -ceq $PSCommandPath -and
$keyProbe.Refused -and -not $keyProbe.ProbeLoaded
Assert-True -Name 'IND-CERT-KEY-00 key native class and CSP key names resolve to stand-ins; Add-Type refuses' `
    -Condition $keyBound -Detail ($keyProbe | ConvertTo-Json -Compress)
if (-not $keyBound) {
    Add-TestResult -Name 'IND-FLOW-00 flows not executed: certificate key boundary cannot be replaced safely' `
        -Outcome FAIL
    Complete-TestSuite
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

function Get-CaughtMessage {
    param([scriptblock]$Action)
    try { $null = & $Action; return $null }
    catch { return $_.Exception.Message }
}

function New-ExpiryText {
    param([double]$Hours)
    return [DateTimeOffset]::UtcNow.AddHours($Hours).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'",
        [Globalization.CultureInfo]::InvariantCulture)
}

function New-PlanDocument {
    param([string]$Lifetime, [string]$SourceRoot, [object[]]$Inputs, [string]$Machine = $script:FixtureMachine,
        [string]$Expires = (New-ExpiryText -Hours 3), [bool]$RestartApproved = $true, [bool]$FirewallApproved = $true)
    return [ordered]@{
        Version = 1
        ExecutionApproved = $true
        ExpiresUtc = $Expires
        LifetimeId = $Lifetime
        MachineName = $Machine
        InstanceName = $script:FixtureInstance
        InstanceId = $script:FixtureInstanceId
        ExpectedProductVersion = $script:FixtureVersion
        Port = $script:FixturePort
        Database = 'Dawnholder_VerifierFixture'
        RuntimeLogin = 'dh_verifier_fixture'
        SchemaVersion = 4
        CertificateThumbprint = $script:FixtureThumbprint
        Encrypt = 'Mandatory'
        TrustServerCertificate = $false
        EnableSa = $false
        SwitchWslNetworkingMode = $false
        ForceEncryption = 'Preserve'
        RestartApproved = $RestartApproved
        FirewallApproved = $FirewallApproved
        SourceRoot = $SourceRoot
        EvidenceRoot = (Join-Path $SourceRoot '.backups\evidence')
        PrivateRoot = (Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $Lifetime))
        Inputs = @($Inputs)
    }
}

function New-InputEntry {
    param([string]$SourceRoot, [string]$Relative)
    return [ordered]@{ Path = $Relative; Sha256 = (Get-FileSha256 -Path (Join-Path $SourceRoot $Relative)) }
}

# =====================================================================================================
# 1. Static boundary of the public entry points and the connection settings (source structure only).
# =====================================================================================================
function Get-ParameterNames {
    param([string]$Path)
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors)
    return @($ast.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
}
$entryParameters = @(
    @{ File = 'Invoke-WindowsSqlEnvironment.ps1'; Expected = 'ExpectedPlanHash|PlanPath|RuntimePath|Step' },
    @{ File = 'New-WindowsSqlCertificate.ps1'; Expected = 'ExpectedPreparationHash|PreparationPath' },
    @{ File = 'Remove-WindowsSqlCertificate.ps1'; Expected = 'ExpectedPlanHash|PlanPath|RuntimePath' }
)
foreach ($entry in $entryParameters) {
    $names = (@(Get-ParameterNames -Path (Join-Path $script:ProductRoot $entry.File)) | Sort-Object) -join '|'
    Assert-Equal -Name ('IND-STATIC-01 public parameters are closed: ' + $entry.File) -Expected $entry.Expected `
        -Actual $names
}

$sqlText = [IO.File]::ReadAllText((Join-Path $script:ProductRoot 'WindowsSql.Sql.ps1'))
$hostText = [IO.File]::ReadAllText((Join-Path $script:ProductRoot 'WindowsSqlVerifyHost/Program.cs'))
$productTexts = @(Get-ChildItem -LiteralPath $script:ProductRoot -Recurse -File -Include '*.ps1', '*.cs' |
        ForEach-Object { [pscustomobject]@{ Name = $_.Name; Text = [IO.File]::ReadAllText($_.FullName) } })
Assert-True -Name 'IND-STATIC-02 admin connection checks Shared memory on the same connection' `
    -Condition ($sqlText.Contains("CONNECTIONPROPERTY('net_transport')") -and $sqlText.Contains("N'Shared memory'"))
Assert-True -Name 'IND-STATIC-03 no TCP/Named Pipes admin data source in the SQL helper' `
    -Condition ($sqlText -notmatch "(?i)'(tcp|np):")
Assert-True -Name 'IND-STATIC-04 WSL verify host keeps Mandatory encryption and certificate validation' `
    -Condition ($hostText.Contains('Encrypt = SqlConnectionEncryptOption.Mandatory') -and
    $hostText.Contains('TrustServerCertificate = false') -and
    $hostText -notmatch 'TrustServerCertificate\s*=\s*true')
$rootStorePattern = '(?i)(\\Root\b|StoreName\]::Root|X509Store|Import-Certificate)'
$rootStoreHits = @($productTexts | Where-Object { $_.Text -match $rootStorePattern })
Assert-True -Name 'IND-STATIC-05 no OS trust store (Root) write path in product sources' `
    -Condition ($rootStoreHits.Count -eq 0) -Detail (@($rootStoreHits | ForEach-Object Name) -join ',')
$saHits = @($productTexts | Where-Object { $_.Text -match "(?i)(ALTER\s+LOGIN\s+\[?sa\b|N'sa'|=\s*'sa')" })
Assert-True -Name 'IND-STATIC-06 no sa login reference in product SQL' -Condition ($saHits.Count -eq 0) `
    -Detail (@($saHits | ForEach-Object Name) -join ',')
$listHits = @($productTexts | Where-Object { $_.Text -match '(?i)sys\.(databases|sql_logins|master_files)\b' })
Assert-True -Name 'IND-STATIC-07 no database/login catalog enumeration' -Condition ($listHits.Count -eq 0) `
    -Detail (@($listHits | ForEach-Object Name) -join ',')

# The admin connection builder runs here verbatim (statements before SqlConnection construction only).
# PowerShell routes property assignment on IDictionary objects to the keyword indexer; the repository
# convention is $builder['Data Source'] (test-environment/Environment.Common.ps1).
$sqlTokens = $null
$sqlErrors = $null
$sqlAst = [Management.Automation.Language.Parser]::ParseFile((Join-Path $script:ProductRoot 'WindowsSql.Sql.ps1'),
    [ref]$sqlTokens, [ref]$sqlErrors)
$openFunction = $sqlAst.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -ceq 'Open-WindowsSqlAdminConnection' }, $true)
$builderStatements = [Collections.Generic.List[string]]::new()
foreach ($statement in @($openFunction.Body.EndBlock.Statements)) {
    if ($statement.Extent.Text -match '^\$connection\s*=') { break }
    $builderStatements.Add($statement.Extent.Text)
}
$builderError = $null
$builderConnectionString = ''
& {
    $Adapter = [pscustomobject]@{ Plan = [pscustomobject]@{
            MachineName = $script:FixtureMachine; InstanceName = $script:FixtureInstance } }
    $Database = 'master'
    try {
        . ([scriptblock]::Create(($builderStatements -join "`n")))
        $script:BuilderConnectionString = $builder.ConnectionString
    }
    catch { $script:BuilderError = $_.Exception.Message }
}
$builderError = Get-Variable -Name BuilderError -Scope Script -ValueOnly -ErrorAction SilentlyContinue
$builderConnectionString = [string](Get-Variable -Name BuilderConnectionString -Scope Script -ValueOnly `
        -ErrorAction SilentlyContinue)
Assert-True -Name 'IND-ADMIN-01 admin connection builder statements execute under pwsh 7' `
    -Condition ($null -eq $builderError) -Detail ([string]$builderError)
# Parse through the constructor: `$builderKeys.ConnectionString = ...` would also go to the keyword indexer.
$builderKeys = [Data.SqlClient.SqlConnectionStringBuilder]::new($builderConnectionString)
Assert-True -Name 'IND-ADMIN-02 admin connection is exact lpc + integrated, Encrypt=false, TSC=false' -Condition (
    [string]$builderKeys['Data Source'] -ceq ('lpc:' + $script:FixtureMachine + '\' + $script:FixtureInstance) -and
    $builderKeys['Integrated Security'] -eq $true -and $builderKeys['Encrypt'] -eq $false -and
    $builderKeys['TrustServerCertificate'] -eq $false) -Detail $builderConnectionString

# =====================================================================================================
# 2. Real entry scripts as child processes: rejected input and another machine stop before any adapter.
# =====================================================================================================
$cliRoot = Join-Path $script:SuiteRoot 'cli'
$cliSource = Join-Path $cliRoot 'source'
$cliTemp = Join-Path $cliRoot 'temp'
foreach ($directory in @((Join-Path $cliSource '.backups\evidence'), $cliTemp)) {
    [void][IO.Directory]::CreateDirectory($directory)
}
[IO.File]::WriteAllText((Join-Path $cliSource 'input.txt'), 'cli fixture input, not an approval')
$cliSentinel = 'VERIFIER_SECRET_SENTINEL_5F3A'
[IO.File]::WriteAllText((Join-Path $cliSource 'runtime.json'), '{"Password":"' + $cliSentinel + '"}')
$cliLifetime = [Guid]::NewGuid().ToString('N')
$cliPlan = New-PlanDocument -Lifetime $cliLifetime -SourceRoot $cliSource -Inputs @(
    (New-InputEntry -SourceRoot $cliSource -Relative 'input.txt'),
    (New-InputEntry -SourceRoot $cliSource -Relative 'runtime.json'))
$cliPlanPath = Join-Path $cliRoot 'plan.json'
Write-JsonFile -Path $cliPlanPath -Value $cliPlan
$cliPlanHash = Get-FileSha256 -Path $cliPlanPath

function Invoke-EntryScript {
    param([string]$Name, [string]$Script, [string[]]$Arguments)
    $start = [Diagnostics.ProcessStartInfo]::new($script:PwshPath)
    foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File',
            (Join-Path $script:ProductRoot $Script)) + $Arguments) {
        $start.ArgumentList.Add($argument)
    }
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.WorkingDirectory = $cliRoot
    $start.Environment['LOCALAPPDATA'] = $env:LOCALAPPDATA
    $start.Environment['TEMP'] = $cliTemp
    $start.Environment['TMP'] = $cliTemp
    $process = [Diagnostics.Process]::Start($start)
    $process.StandardInput.Close()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(120000)) {
        $process.Kill($true)
        $process.WaitForExit()
        throw ('entry child timed out: ' + $Name)
    }
    $result = [pscustomobject]@{
        Name = $Name
        Script = $Script
        Arguments = $Arguments
        Exit = $process.ExitCode
        Stdout = $stdout.Result
        Stderr = $stderr.Result
    }
    $process.Dispose()
    Write-JsonFile -Path (Join-Path $cliRoot ($Name + '.json')) -Value $result
    return $result
}

function Assert-EntryRejected {
    param([string]$Id, $Result, [string]$Code)
    $stdoutEmpty = $Result.Stdout.Length -eq 0
    $codeExact = $Result.Stderr.Trim() -ceq $Code
    $noSentinel = -not ($Result.Stdout + $Result.Stderr).Contains($cliSentinel)
    Assert-True -Name ($Id + ' exit 1 for ' + $Result.Name) -Condition ($Result.Exit -eq 1) `
        -Detail ([string]$Result.Exit)
    Assert-True -Name ($Id + ' stderr is exactly ' + $Code + ' for ' + $Result.Name) -Condition $codeExact `
        -Detail $Result.Stderr.Trim()
    Assert-True -Name ($Id + ' no stdout result for ' + $Result.Name) -Condition $stdoutEmpty
    Assert-True -Name ($Id + ' no runtime secret sentinel in output for ' + $Result.Name) -Condition $noSentinel
}

$commonCli = @('-PlanPath', $cliPlanPath, '-ExpectedPlanHash', $cliPlanHash, '-RuntimePath', 'runtime.json')
foreach ($step in @('Preflight', 'Plan', 'Apply', 'Verify', 'Cleanup')) {
    $result = Invoke-EntryScript -Name ('cli-other-machine-' + $step) -Script 'Invoke-WindowsSqlEnvironment.ps1' `
        -Arguments ($commonCli + @('-Step', $step))
    Assert-EntryRejected -Id 'IND-CLI-01' -Result $result -Code 'WINDOWS_SQL_IDENTITY_MISMATCH'
}
$result = Invoke-EntryScript -Name 'cli-step-case' -Script 'Invoke-WindowsSqlEnvironment.ps1' `
    -Arguments ($commonCli + @('-Step', 'apply'))
Assert-EntryRejected -Id 'IND-CLI-02' -Result $result -Code 'WINDOWS_SQL_STEP_INVALID'
$result = Invoke-EntryScript -Name 'cli-lowercase-hash' -Script 'Invoke-WindowsSqlEnvironment.ps1' -Arguments @(
    '-PlanPath', $cliPlanPath, '-ExpectedPlanHash', $cliPlanHash.ToLowerInvariant(), '-RuntimePath', 'runtime.json',
    '-Step', 'Preflight')
Assert-EntryRejected -Id 'IND-CLI-03' -Result $result -Code 'WINDOWS_SQL_PLAN_IDENTITY_REQUIRED'
$result = Invoke-EntryScript -Name 'cli-other-hash' -Script 'Invoke-WindowsSqlEnvironment.ps1' -Arguments @(
    '-PlanPath', $cliPlanPath, '-ExpectedPlanHash', ('0' * 64), '-RuntimePath', 'runtime.json', '-Step', 'Preflight')
Assert-EntryRejected -Id 'IND-CLI-04' -Result $result -Code 'WINDOWS_SQL_PLAN_CHANGED'
$result = Invoke-EntryScript -Name 'cli-relative-plan' -Script 'Invoke-WindowsSqlEnvironment.ps1' -Arguments @(
    '-PlanPath', 'plan.json', '-ExpectedPlanHash', $cliPlanHash, '-RuntimePath', 'runtime.json', '-Step', 'Preflight')
Assert-EntryRejected -Id 'IND-CLI-05' -Result $result -Code 'WINDOWS_SQL_PLAN_INVALID'

$expiredPlan = New-PlanDocument -Lifetime $cliLifetime -SourceRoot $cliSource -Inputs $cliPlan.Inputs `
    -Expires (New-ExpiryText -Hours -1)
$expiredPath = Join-Path $cliRoot 'plan-expired.json'
Write-JsonFile -Path $expiredPath -Value $expiredPlan
$result = Invoke-EntryScript -Name 'cli-expired' -Script 'Invoke-WindowsSqlEnvironment.ps1' -Arguments @(
    '-PlanPath', $expiredPath, '-ExpectedPlanHash', (Get-FileSha256 -Path $expiredPath), '-RuntimePath',
    'runtime.json', '-Step', 'Apply')
Assert-EntryRejected -Id 'IND-CLI-06' -Result $result -Code 'WINDOWS_SQL_APPROVAL_EXPIRED'

# This desktop's own name: v1 must not be able to target it, so no adapter can be constructed here.
$hostPlan = New-PlanDocument -Lifetime $cliLifetime -SourceRoot $cliSource -Inputs $cliPlan.Inputs `
    -Machine ([Environment]::MachineName)
$hostPlanPath = Join-Path $cliRoot 'plan-this-host.json'
Write-JsonFile -Path $hostPlanPath -Value $hostPlan
if ([Environment]::MachineName -cnotmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,14}$') {
    $result = Invoke-EntryScript -Name 'cli-this-host' -Script 'Invoke-WindowsSqlEnvironment.ps1' -Arguments @(
        '-PlanPath', $hostPlanPath, '-ExpectedPlanHash', (Get-FileSha256 -Path $hostPlanPath), '-RuntimePath',
        'runtime.json', '-Step', 'Preflight')
    Assert-EntryRejected -Id 'IND-CLI-07' -Result $result -Code 'WINDOWS_SQL_NAME_INVALID'
}
else {
    Add-TestResult -Name 'IND-CLI-07 this-host plan not executed (host name is v1-valid; would reach adapter)' `
        -Outcome OBSERVED
}

foreach ($forbidden in @(
        @{ Name = 'Adapter'; Value = 'Fake' }, @{ Name = 'TrustServerCertificate'; Value = 'true' },
        @{ Name = 'Sql'; Value = 'SELECT 1' }, @{ Name = 'MachineName'; Value = $script:FixtureMachine })) {
    $result = Invoke-EntryScript -Name ('cli-forbidden-' + $forbidden.Name) -Script 'Invoke-WindowsSqlEnvironment.ps1' `
        -Arguments ($commonCli + @('-Step', 'Preflight', ('-' + $forbidden.Name), $forbidden.Value))
    Assert-True -Name ('IND-CLI-08 unknown parameter -' + $forbidden.Name + ' fails before execution') `
        -Condition ($result.Exit -ne 0 -and $result.Stdout.Length -eq 0 -and
        -not $result.Stderr.Contains('WINDOWS_SQL_IDENTITY_MISMATCH')) -Detail ([string]$result.Exit)
}

# Certificate preparation for another machine: identity stops before PKI, private folder and public CA output.
$prepPrivate = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $cliLifetime)
$prepCa = Join-Path $cliSource '.backups\public-ca.pem'
$preparation = [ordered]@{
    Version = 1
    ExecutionApproved = $true
    ExpiresUtc = (New-ExpiryText -Hours 3)
    LifetimeId = $cliLifetime
    MachineName = $script:FixtureMachine
    InstanceName = $script:FixtureInstance
    ExecutorSid = $script:ExecutorSid
    ServiceAccount = ('NT Service\MSSQL$' + $script:FixtureInstance)
    TlsServerName = 'laptop-fixture.internal'
    PrivateRoot = $prepPrivate
    PublicCaPath = $prepCa
}
$preparationPath = Join-Path $cliRoot 'preparation.json'
Write-JsonFile -Path $preparationPath -Value $preparation
$preparationHash = Get-FileSha256 -Path $preparationPath
$result = Invoke-EntryScript -Name 'cert-prepare-other-machine' -Script 'New-WindowsSqlCertificate.ps1' -Arguments @(
    '-PreparationPath', $preparationPath, '-ExpectedPreparationHash', $preparationHash)
Assert-EntryRejected -Id 'IND-CERT-CLI-01' -Result $result -Code 'WINDOWS_SQL_IDENTITY_MISMATCH'
$result = Invoke-EntryScript -Name 'cert-prepare-hash' -Script 'New-WindowsSqlCertificate.ps1' -Arguments @(
    '-PreparationPath', $preparationPath, '-ExpectedPreparationHash', ('1' * 64))
Assert-EntryRejected -Id 'IND-CERT-CLI-02' -Result $result -Code 'WINDOWS_SQL_PREPARATION_CHANGED'
Assert-True -Name 'IND-CERT-CLI-03 rejected preparation created no private folder or public CA' `
    -Condition (-not [IO.Directory]::Exists($prepPrivate) -and -not [IO.File]::Exists($prepCa))
$result = Invoke-EntryScript -Name 'cert-remove-other-machine' -Script 'Remove-WindowsSqlCertificate.ps1' `
    -Arguments $commonCli
Assert-EntryRejected -Id 'IND-CERT-CLI-04' -Result $result -Code 'WINDOWS_SQL_IDENTITY_MISMATCH'

# Dot-sourcing an entry script returns before definitions or effects.
foreach ($entry in @('Invoke-WindowsSqlEnvironment.ps1', 'New-WindowsSqlCertificate.ps1',
        'Remove-WindowsSqlCertificate.ps1')) {
    $start = [Diagnostics.ProcessStartInfo]::new($script:PwshPath)
    $probe = ". '" + (Join-Path $script:ProductRoot $entry) + "'; " +
    "@(Get-Command -CommandType Function | Where-Object Name -like '*WindowsSql*').Count"
    foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-Command', $probe)) {
        $start.ArgumentList.Add($argument)
    }
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['LOCALAPPDATA'] = $env:LOCALAPPDATA
    $process = [Diagnostics.Process]::Start($start)
    $out = $process.StandardOutput.ReadToEnd().Trim()
    $err = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    Assert-True -Name ('IND-CLI-09 dot-source of ' + $entry + ' defines and runs nothing') `
        -Condition ($process.ExitCode -eq 0 -and $out -ceq '0' -and $err.Length -eq 0) -Detail ($out + '|' + $err)
    $process.Dispose()
}
$cliPrivateEntries = @(Get-ChildItem -LiteralPath (Join-Path $env:LOCALAPPDATA 'Dawnholder') -Force)
Assert-True -Name 'IND-CLI-10 no private lifetime folder exists after all rejected entries' `
    -Condition ($cliPrivateEntries.Count -eq 0) -Detail (@($cliPrivateEntries | ForEach-Object Name) -join ',')

# =====================================================================================================
# 3. Product definitions, the real PS5.1 source reader child and child-process lifetime.
# =====================================================================================================
# The service observation opens the exact instance service through the ServiceController type literal to read its
# dependent services. The fixture instance has none. Service control is replaced at the RestartService seam below,
# so any Stop/Start/Wait reaching this stand-in is recorded and refused.
class AdapterFakeServiceController {
    static [Collections.Generic.List[string]]$Calls = [Collections.Generic.List[string]]::new()
    [string]$ServiceName
    [object[]]$DependentServices = @()

    AdapterFakeServiceController([string]$name) {
        $this.ServiceName = $name
        [AdapterFakeServiceController]::Calls.Add('Open:' + $name)
    }

    [void] Refresh() { [AdapterFakeServiceController]::Calls.Add('Refresh:' + $this.ServiceName) }
    [void] Stop() { [AdapterFakeServiceController]::Refuse('Stop()') }
    [void] Stop([bool]$stopDependentServices) {
        [AdapterFakeServiceController]::Refuse('Stop(' + $stopDependentServices + ')')
    }
    [void] Start() { [AdapterFakeServiceController]::Refuse('Start') }
    [void] WaitForStatus([object]$desired, [TimeSpan]$timeout) {
        [AdapterFakeServiceController]::Refuse('WaitForStatus')
    }
    [void] Dispose() { [AdapterFakeServiceController]::Calls.Add('Dispose:' + $this.ServiceName) }

    static [void] Refuse([string]$operation) {
        [AdapterFakeServiceController]::Calls.Add('Control:' + $operation)
        throw 'VERIFIER_FORBIDDEN_ServiceControl'
    }
}

$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
$controllerTypeNames = @('ServiceProcess.ServiceController', 'System.ServiceProcess.ServiceController')
foreach ($typeName in $controllerTypeNames) { $accelerators::Add($typeName, [AdapterFakeServiceController]) }
$boundControllerNames = @(foreach ($typeName in $controllerTypeNames) {
        (& ([scriptblock]::Create('[' + $typeName + ']'))).FullName
    })
$controllerBound = @($boundControllerNames | Where-Object { $_ -cne 'AdapterFakeServiceController' }).Count -eq 0
Assert-True -Name 'IND-SCM-00 ServiceController type literals resolve to the offline stand-in' `
    -Condition $controllerBound -Detail ($boundControllerNames -join ',')
if (-not $controllerBound) {
    Add-TestResult -Name 'IND-FLOW-00 flows not executed: service observation cannot be replaced safely' -Outcome FAIL
    Complete-TestSuite
}
. (Join-Path $script:ProductRoot 'WindowsSql.Common.ps1')
. (Join-Path $script:ProductRoot 'WindowsSql.Adapter.ps1')
$script:RealInvokeChild = ${function:Invoke-WindowsSqlChild}
$script:RealAssertExecutor = ${function:Assert-WindowsSqlExecutor}
$script:RealAssertCertificate = ${function:Assert-WindowsSqlCertificate}

$readerTemp = Join-Path $script:SuiteRoot 'reader-temp'
[void][IO.Directory]::CreateDirectory($readerTemp)
$inheritedModulePath = [string]$env:PSModulePath
Add-TestResult -Name 'IND-READER-00 inherited PSModulePath includes a PowerShell 7 module path' -Outcome OBSERVED `
    -Detail ([string]($inheritedModulePath -match '(?i)\\PowerShell\\7\\Modules'))
$readerError = $null
$readerText = $null
try {
    # Same executable, arguments and environment keys as New-WindowsSqlAdapter.
    $readerText = & $script:RealInvokeChild -FileName 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
        -Arguments @('-NoLogo', '-NoProfile', '-NonInteractive', '-File',
        (Join-Path $script:ProductRoot 'Read-WindowsSqlSources.ps1')) `
        -Environment @{ TEMP = $readerTemp; TMP = $readerTemp; LOCALAPPDATA = $env:LOCALAPPDATA }
}
catch { $readerError = $_.Exception.Message }
Assert-True -Name 'IND-READER-01 product child runs the PS5.1 source reader from pwsh 7' `
    -Condition ($null -eq $readerError) -Detail ([string]$readerError)
$script:Sources = $null
if ($null -ne $readerText) {
    $script:Sources = ConvertFrom-Json -InputObject $readerText -Depth 30
    Assert-True -Name 'IND-READER-02 reader returns four migrations, release 4 bundle and catalog' -Condition (
        $script:Sources.Version -eq 1 -and $script:Sources.PowerShellVersion -match '^5\.1\.' -and
        @($script:Sources.Sources).Count -eq 4 -and $script:Sources.Bundle.ReleaseVersion -eq 4 -and
        [string]$script:Sources.Catalog -ne '') -Detail $script:Sources.PowerShellVersion
    Assert-Equal -Name 'IND-READER-03 reader migration names are 001..004 in order' `
        -Expected '001_initial.sql|002_persistence_metadata.sql|003_module_metadata.sql|004_module_release.sql' `
        -Actual (@($script:Sources.Sources | ForEach-Object Name) -join '|')
}
$pwshReaderError = Get-CaughtMessage {
    & $script:RealInvokeChild -FileName $script:PwshPath -Arguments @('-NoLogo', '-NoProfile', '-NonInteractive',
        '-File', (Join-Path $script:ProductRoot 'Read-WindowsSqlSources.ps1')) `
        -Environment @{ TEMP = $readerTemp; TMP = $readerTemp }
}
Assert-True -Name 'IND-READER-04 source reader refuses a non-5.1 host without output detail' `
    -Condition ($pwshReaderError -ceq 'WINDOWS_SQL_CHILD_FAILED') -Detail ([string]$pwshReaderError)

# Timeout kills the whole child tree; stderr with exit 0 is still a failure.
$treeRoot = Join-Path $script:SuiteRoot 'child-tree'
[void][IO.Directory]::CreateDirectory($treeRoot)
$grandchildPidPath = Join-Path $treeRoot 'grandchild.pid'
$grandchildScript = Join-Path $treeRoot 'grandchild.ps1'
$parentScript = Join-Path $treeRoot 'parent.ps1'
[IO.File]::WriteAllText($grandchildScript,
    "[IO.File]::WriteAllText('$grandchildPidPath', [string]`$PID); [Threading.Thread]::Sleep(60000)")
[IO.File]::WriteAllText($parentScript, (
        "`$p = [Diagnostics.Process]::Start('$($script:PwshPath)', '-NoProfile -File ""$grandchildScript""'); " +
        "while (-not [IO.File]::Exists('$grandchildPidPath')) { [Threading.Thread]::Sleep(100) }; " +
        "[Threading.Thread]::Sleep(60000)"))
$timeoutError = Get-CaughtMessage {
    & $script:RealInvokeChild -FileName $script:PwshPath -Arguments @('-NoProfile', '-File', $parentScript) `
        -TimeoutSeconds 15
}
Assert-True -Name 'IND-CHILD-01 child timeout is reported as WINDOWS_SQL_CHILD_TIMEOUT' `
    -Condition ($timeoutError -ceq 'WINDOWS_SQL_CHILD_TIMEOUT') -Detail ([string]$timeoutError)
$grandchildAlive = $null
if ([IO.File]::Exists($grandchildPidPath)) {
    $grandchildPid = [int][IO.File]::ReadAllText($grandchildPidPath)
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        $grandchildAlive = $null -ne (Get-Process -Id $grandchildPid -ErrorAction SilentlyContinue)
        if ($grandchildAlive) { [Threading.Thread]::Sleep(250) }
    } while ($grandchildAlive -and [DateTime]::UtcNow -lt $deadline)
    if ($grandchildAlive) { Stop-Process -Id $grandchildPid -Force -ErrorAction SilentlyContinue }
}
Assert-True -Name 'IND-CHILD-02 timeout also ends the grandchild process' -Condition ($grandchildAlive -eq $false) `
    -Detail ([string]$grandchildAlive)
$stderrScript = Join-Path $treeRoot 'stderr-exit0.ps1'
[IO.File]::WriteAllText($stderrScript,
    '[Console]::Out.Write("ok"); [Console]::Error.Write("VERIFIER_CHILD_DETAIL"); exit 0')
$stderrError = Get-CaughtMessage {
    & $script:RealInvokeChild -FileName $script:PwshPath -Arguments @('-NoProfile', '-File', $stderrScript)
}
Assert-True -Name 'IND-CHILD-03 stderr output with exit 0 fails without exposing the child text' `
    -Condition ($stderrError -ceq 'WINDOWS_SQL_CHILD_FAILED') -Detail ([string]$stderrError)

# Real executor gate with this session's own identity (read-only).
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Add-TestResult -Name 'IND-EXEC-00 verifier session administrator token' -Outcome OBSERVED `
    -Detail ([string]$isAdministrator)
$executorError = Get-CaughtMessage {
    & $script:RealAssertExecutor -Plan ([pscustomobject]@{ MachineName = $script:FixtureMachine }) `
        -Runtime ([pscustomobject]@{ ExecutorSid = $script:ExecutorSid })
}
Assert-True -Name 'IND-EXEC-01 executor gate rejects another machine first' `
    -Condition ($executorError -ceq 'WINDOWS_SQL_IDENTITY_MISMATCH') -Detail ([string]$executorError)
$executorError = Get-CaughtMessage {
    & $script:RealAssertExecutor -Plan ([pscustomobject]@{ MachineName = [Environment]::MachineName }) `
        -Runtime ([pscustomobject]@{ ExecutorSid = 'S-1-5-21-1-2-3-1001' })
}
Assert-True -Name 'IND-EXEC-02 executor gate rejects another SID on this machine' `
    -Condition ($executorError -ceq 'WINDOWS_SQL_EXECUTOR_MISMATCH') -Detail ([string]$executorError)
if (-not $isAdministrator) {
    $executorError = Get-CaughtMessage {
        & $script:RealAssertExecutor -Plan ([pscustomobject]@{ MachineName = [Environment]::MachineName }) `
            -Runtime ([pscustomobject]@{ ExecutorSid = $script:ExecutorSid })
    }
    Assert-True -Name 'IND-EXEC-03 executor gate rejects a non-administrator token' `
        -Condition ($executorError -ceq 'WINDOWS_SQL_EXECUTOR_MISMATCH') -Detail ([string]$executorError)
}

# =====================================================================================================
# 4. Protected I/O fakes below the adapter helpers, then the full step sequence through the dispatcher.
# =====================================================================================================
$script:GeneratedPasswords = [Collections.Generic.List[string]]::new()
$script:CommandFactory = [pscustomobject]@{}
$script:CommandFactory | Add-Member -MemberType ScriptMethod -Name CreateCommand -Value {
    # A provider command object only for the real New-DbCommand parameter typing; it never executes.
    return [Data.SqlClient.SqlCommand]::new()
}

function Reset-FakeWorld {
    param([ValidateSet('Nat', 'Mirrored')][string]$Mode)
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
    $script:Service = @{ State = 'Running'; ProcessId = 4242 }
    $script:Listeners = [Collections.Generic.List[object]]::new()
    $script:Firewall = @{}
    $script:Sql = @{
        DbExists = $false; DbGuid = $null; DbOwner = $null; Versions = [Collections.Generic.List[object]]::new()
        Binding = $null; LoginExists = $false; LoginSid = $null; UserExists = $false; Password = $null
        FailMarker = $null; BindingMismatch = $false
    }
    $script:Wsl = @{
        Mode = $Mode.ToLowerInvariant()
        Routes = '[{"dst":"default","gateway":"172.28.16.1","dev":"eth0","flags":[]},' +
        '{"dst":"172.28.16.0/20","dev":"eth0","protocol":"kernel","scope":"link",' +
        '"prefsrc":"172.28.20.5","flags":[]}]'
        CloneRoot = '/home/fixture/dawnholder-clone'
        Dotnet = '/home/fixture/.dotnet/dotnet'
        HostOutput = '{"Status":"Verified","Transport":"TCP","Encrypted":true,"CertificateValidated":true,' +
        '"RuntimeExecuteGrants":5,"DirectDmlDenied":true}'
        HostFails = $false
    }
    $script:Faults = @{ RegistryWrite = $null; AfterWrite = $null }
    foreach ($name in @('Mutations', 'RegistryWrites', 'SqlLog', 'ConnectionOpens', 'FirewallCalls', 'WslCalls',
            'VerifyCalls', 'ModuleCalls', 'JournalWrites', 'Violations', 'Restarts')) {
        Set-Variable -Scope Script -Name $name -Value ([Collections.Generic.List[object]]::new())
    }
    $script:ExecutorChecks = 0
    $script:CertificateChecks = 0
}

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
        $script:RegistryWrites.Add([pscustomobject]@{ Suffix = $this.Suffix; Name = $Name; Value = [string]$Value
                Kind = [string]$Kind })
        if ($script:Faults.RegistryWrite -ceq $Name) {
            $script:Faults.RegistryWrite = $null
            throw 'FAKE_REGISTRY_WRITE_FAILED'
        }
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

function Update-FakeListeners {
    $ownedByService = [Predicate[object]] { param($item) $item.OwningProcess -eq $script:Service.ProcessId }
    $script:Listeners.RemoveAll($ownedByService) | Out-Null
    $tcp = $script:Registry['\MSSQLServer\SuperSocketNetLib\Tcp']['Enabled'].Value
    $port = $script:Registry['\MSSQLServer\SuperSocketNetLib\Tcp\IPAll']['TcpPort'].Value
    if ($tcp -eq 1 -and $port -ne '') {
        $script:Listeners.Add([pscustomobject]@{ LocalPort = [int]$port; OwningProcess = $script:Service.ProcessId })
    }
}

function Get-CimInstance {
    [CmdletBinding()]
    param([string]$ClassName, [string]$Filter)
    $expected = "Name='MSSQL`$" + $script:FixtureInstance + "'"
    if ($ClassName -cne 'Win32_Service' -or $Filter -cne $expected) { throw 'FAKE_CIM_UNEXPECTED' }
    return [pscustomobject]@{
        Name = 'MSSQL$' + $script:FixtureInstance
        StartName = 'NT Service\MSSQL$' + $script:FixtureInstance
        State = $script:Service.State
        ProcessId = $script:Service.ProcessId
    }
}

function Get-NetTCPConnection {
    [CmdletBinding()]
    param([string]$State, [int]$LocalPort)
    return @($script:Listeners | Where-Object { $_.LocalPort -eq $LocalPort })
}

function Get-NetFirewallRule {
    [CmdletBinding()]
    param([string]$PolicyStore, [string]$Name)
    $script:FirewallCalls.Add('Get:' + $Name)
    if ($script:Firewall.ContainsKey($Name)) { return $script:Firewall[$Name] }
}

function Get-NetFirewallPortFilter {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline)]$InputObject)
    process { [pscustomobject]@{ Protocol = $InputObject.Protocol; LocalPort = $InputObject.LocalPort } }
}

function Get-NetFirewallAddressFilter {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline)]$InputObject)
    process {
        [pscustomobject]@{ RemoteAddress = $InputObject.RemoteAddress; LocalAddress = $InputObject.LocalAddress }
    }
}

function Get-NetFirewallServiceFilter {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline)]$InputObject)
    process { [pscustomobject]@{ Service = $InputObject.Service } }
}

function New-NetFirewallRule {
    [CmdletBinding()]
    param($Name, $DisplayName, $Description, $PolicyStore, $Enabled, $Direction, $Action, $Protocol, $LocalPort,
        $LocalAddress, $RemoteAddress, $Service, $EdgeTraversalPolicy)
    $script:FirewallCalls.Add('New:' + $Name)
    $rule = [pscustomobject]@{
        Name = $Name; DisplayName = $DisplayName; Description = $Description; PolicyStore = $PolicyStore
        Enabled = [string]$Enabled; Direction = [string]$Direction; Action = [string]$Action
        Protocol = [string]$Protocol
        LocalPort = [string]$LocalPort; LocalAddress = [string]$LocalAddress; RemoteAddress = [string]$RemoteAddress
        Service = [string]$Service; EdgeTraversalPolicy = [string]$EdgeTraversalPolicy
    }
    $script:Firewall[$Name] = $rule
    return $rule
}

function Remove-NetFirewallRule {
    [CmdletBinding()]
    param([string]$PolicyStore, [string]$Name)
    $script:FirewallCalls.Add('Remove:' + $Name)
    if (-not $script:Firewall.ContainsKey($Name)) { throw 'FAKE_FIREWALL_RULE_MISSING' }
    $script:Firewall.Remove($Name)
}

function ConvertTo-FakeWslPath {
    param([string]$Path)
    $full = [IO.Path]::GetFullPath($Path)
    return '/mnt/' + $full.Substring(0, 1).ToLowerInvariant() + $full.Substring(2).Replace('\', '/')
}

function Invoke-WindowsSqlChild {
    param([string]$FileName, [string[]]$Arguments, [string]$InputText = '', [hashtable]$Environment = @{},
        [int]$TimeoutSeconds = 45)
    if ($FileName -cne 'C:\Windows\System32\wsl.exe') {
        $script:Violations.Add('unexpected child ' + $FileName)
        throw 'FAKE_CHILD_UNEXPECTED'
    }
    $a = @($Arguments)
    if ($a.Count -ge 4 -and $a[0] -ceq '-d' -and $a[1] -ceq 'Ubuntu' -and $a[2] -ceq '--exec') {
        $tail = @($a[3..($a.Count - 1)])
        $script:WslCalls.Add([pscustomobject]@{
                Command = $tail[0]; Input = $InputText.Length; Timeout = $TimeoutSeconds })
        switch -CaseSensitive ($tail[0]) {
            'wslinfo' { return $script:Wsl.Mode }
            'ip' { return $script:Wsl.Routes }
            'wslpath' { return ConvertTo-FakeWslPath -Path $tail[3] }
            'bash' {
                if ($tail[1] -ceq '-c') { return $script:Wsl.Dotnet }
                return $script:Wsl.CloneRoot
            }
        }
    }
    if ($a.Count -ge 5 -and $a[0] -ceq '-d' -and $a[1] -ceq 'Ubuntu' -and $a[2] -ceq '--cd') {
        $script:VerifyCalls.Add([pscustomobject]@{ Arguments = $a; InputText = $InputText; Timeout = $TimeoutSeconds })
        if ($script:Wsl.HostFails) { throw 'WINDOWS_SQL_CHILD_FAILED' }
        return $script:Wsl.HostOutput
    }
    $script:Violations.Add('unexpected wsl arguments')
    throw 'FAKE_CHILD_UNEXPECTED'
}

function New-FakeConnection {
    param([string]$Database)
    $connection = [pscustomobject]@{ Database = $Database; Disposed = $false }
    $connection | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Disposed = $true }
    $connection | Add-Member -MemberType ScriptMethod -Name BeginTransaction -Value {
        $transaction = [pscustomobject]@{ Connection = $this; Rows = [Collections.Generic.List[object]]::new()
            Binding = $null; Committed = $false }
        $transaction | Add-Member -MemberType ScriptMethod -Name Commit -Value {
            foreach ($row in $this.Rows) { $script:Sql.Versions.Add($row) }
            if ($null -ne $this.Binding) { $script:Sql.Binding = $this.Binding }
            $this.Committed = $true
            $this.Connection = $null
        }
        $transaction | Add-Member -MemberType ScriptMethod -Name Rollback -Value { $this.Connection = $null }
        $transaction | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
        return $transaction
    }
    return $connection
}

function Open-WindowsSqlAdminConnection {
    param($Adapter, [string]$Database = 'master')
    # Replaces only the provider open; the real builder statements are executed in IND-ADMIN-01.
    $script:ConnectionOpens.Add($Database)
    if ($Database -cne 'master' -and $Database -cne $Adapter.Plan.Database) {
        $script:Violations.Add('connection to another database')
    }
    if ($Database -cne 'master' -and -not $script:Sql.DbExists) { throw 'WINDOWS_SQL_LOCAL_CONNECTION_FAILED' }
    return New-FakeConnection -Database $Database
}

function Invoke-FakeSql {
    param($Connection, $Command, $Transaction)
    $sql = $Command.CommandText
    $p = @{}
    foreach ($parameter in $Command.Parameters) { $p[$parameter.ParameterName.TrimStart('@')] = $parameter.Value }
    if ($Connection.Disposed) { throw 'FAKE_SQL_DISPOSED_CONNECTION' }
    if ($null -ne $Transaction -and -not [object]::ReferenceEquals($Transaction.Connection, $Connection)) {
        throw 'FAKE_SQL_FOREIGN_TRANSACTION'
    }
    $script:SqlLog.Add([pscustomobject]@{ Database = $Connection.Database; InTransaction = $null -ne $Transaction
            Head = ($sql.Trim() -split "`n")[0]; Parameters = @($p.Keys | Sort-Object) })
    if ($null -ne $script:Sql.FailMarker -and $sql.Contains($script:Sql.FailMarker)) {
        $script:Sql.FailMarker = $null
        throw 'FAKE_SQL_INJECTED_FAILURE'
    }
    $target = $Connection.Database -ceq 'Dawnholder_VerifierFixture'
    # Reviewed source texts are matched first so their contents never reach a statement marker below.
    if (@($script:Sources.Sources | ForEach-Object Sql) -ccontains $sql) {
        if ($null -eq $Transaction -or -not $target) { throw 'FAKE_SQL_MIGRATION_OUTSIDE_TRANSACTION' }
        return -1
    }
    if ($sql -ceq $script:Sources.Catalog) {
        if (-not $target) { throw 'FAKE_SQL_WRONG_DATABASE' }
        return -1
    }
    if ($sql.Contains('sp_getapplock') -and $sql.Contains("@LockOwner = 'Session'")) { return 0 }
    if ($sql.Contains('CASE WHEN DB_ID(@name) IS NULL')) { return [int]$script:Sql.DbExists }
    if ($sql.Contains('FROM sys.database_recovery_status WHERE database_id = DB_ID(@name)')) {
        if ($script:Sql.DbExists) { return $script:Sql.DbGuid.ToString().ToUpperInvariant() }
        return [DBNull]::Value
    }
    if ($sql.Contains('FROM sys.extended_properties')) {
        if (-not $target) { throw 'FAKE_SQL_WRONG_DATABASE' }
        if ($null -eq $script:Sql.DbOwner) { return [DBNull]::Value }
        return $script:Sql.DbOwner
    }
    if ($sql.Contains('COALESCE(MAX(Version), 0) FROM dh.SchemaVersion')) { return [int]$script:Sql.Versions.Count }
    if ($sql.Contains('FROM sys.server_principals WHERE name = @name')) {
        if ($script:Sql.LoginExists) { return [Convert]::ToHexString($script:Sql.LoginSid) }
        return [DBNull]::Value
    }
    if ($sql.Contains('CASE WHEN SUSER_ID(@name) IS NULL')) { return [int]$script:Sql.LoginExists }
    if ($sql.Contains("'Exact targets must be new.'")) {
        if ($script:Sql.DbExists -or $script:Sql.LoginExists) { throw 'FAKE_SQL_51202' }
        $script:Sql.DbExists = $true
        $script:Sql.DbGuid = [Guid]::NewGuid()
        return -1
    }
    if ($sql.Contains('CREATE SCHEMA dh AUTHORIZATION dbo')) {
        if (-not $target) { throw 'FAKE_SQL_WRONG_DATABASE' }
        $script:Sql.DbOwner = [string]$p.lifetime
        return -1
    }
    if ($sql.Contains("'Fresh owned schema required; no repair.'")) {
        if ($null -eq $Transaction -or $script:Sql.Versions.Count -ne 0) { throw 'FAKE_SQL_51203' }
        return -1
    }
    if ($sql.Contains('INSERT dh.SchemaVersion(Version, Name, Checksum)')) {
        if ($null -eq $Transaction) { throw 'FAKE_SQL_NO_TRANSACTION' }
        $Transaction.Rows.Add([pscustomobject]@{ Version = [int]$p.version; Name = [string]$p.name
                Checksum = [string]$p.hash })
        return 1
    }
    if ($sql.Contains("N'Dawnholder.Persistence.Slot.1'")) {
        if ($null -eq $Transaction) { throw 'FAKE_SQL_NO_TRANSACTION' }
        $Transaction.Binding = [pscustomobject]@{ Account = [Guid]$p.account; Character = [Guid]$p.character }
        return 1
    }
    if ($sql.Contains("N'CREATE LOGIN '")) {
        if ($script:Sql.LoginExists) { throw 'FAKE_SQL_51205' }
        if ($p.password -isnot [string] -or $p.sid -isnot [byte[]] -or $p.database -cne 'Dawnholder_VerifierFixture') {
            throw 'FAKE_SQL_LOGIN_PARAMETERS'
        }
        $script:Sql.Password = $p.password
        $script:GeneratedPasswords.Add($p.password)
        $script:Sql.LoginExists = $true
        $script:Sql.LoginSid = [byte[]]$p.sid
        return -1
    }
    if ($sql.Contains("N'CREATE USER '")) {
        if (-not $target -or -not $script:Sql.LoginExists -or
            [Convert]::ToHexString([byte[]]$p.sid) -cne [Convert]::ToHexString($script:Sql.LoginSid)) {
            throw 'FAKE_SQL_51206'
        }
        $script:Sql.UserExists = $true
        return -1
    }
    if ($sql.Contains('SELECT Version, Name, Checksum FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH')) {
        if ($script:Sql.Versions.Count -eq 0) { return '[]' }
        return ConvertTo-Json -InputObject @($script:Sql.Versions.ToArray()) -Compress
    }
    if ($sql.Contains("'Binding mismatch.'")) {
        if ($script:Sql.BindingMismatch -or $null -eq $script:Sql.Binding -or
            $script:Sql.Binding.Account -ne [Guid]$p.account -or
            $script:Sql.Binding.Character -ne [Guid]$p.character -or
            -not $script:Sql.UserExists) {
            throw 'FAKE_SQL_51207'
        }
        return -1
    }
    if ($sql.Contains("'Unexpected login authority.'")) { return -1 }
    if ($sql.Contains("N'DROP USER '")) {
        if (-not $target) { throw 'FAKE_SQL_WRONG_DATABASE' }
        $script:Sql.UserExists = $false
        return -1
    }
    if ($sql.Contains("'Login changed.'")) {
        if (-not $script:Sql.LoginExists -or [Convert]::ToHexString($script:Sql.LoginSid) -cne [string]$p.sid) {
            throw 'FAKE_SQL_51210'
        }
        $script:Sql.LoginExists = $false
        $script:Sql.LoginSid = $null
        return -1
    }
    if ($sql.Contains("'Database changed.'")) {
        if (-not $script:Sql.DbExists -or [Guid]$p.guid -ne $script:Sql.DbGuid) { throw 'FAKE_SQL_51211' }
        $script:Sql.DbExists = $false
        $script:Sql.DbGuid = $null
        $script:Sql.DbOwner = $null
        $script:Sql.Versions.Clear()
        $script:Sql.Binding = $null
        $script:Sql.UserExists = $false
        return -1
    }
    $script:Violations.Add('unexpected SQL: ' + ($sql.Trim() -split "`n")[0])
    throw 'FAKE_SQL_UNEXPECTED'
}

function Invoke-DbScalar {
    param($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null)
    # Real New-DbCommand keeps the shared parameter typing contract; the fake replaces execution only.
    $command = New-DbCommand -Connection $script:CommandFactory -Sql $Sql -Parameters $Parameters
    try { return Invoke-FakeSql -Connection $Connection -Command $command -Transaction $Transaction }
    finally { $command.Dispose() }
}

function Invoke-DbNonQuery {
    param($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null)
    $command = New-DbCommand -Connection $script:CommandFactory -Sql $Sql -Parameters $Parameters
    try { return Invoke-FakeSql -Connection $Connection -Command $command -Transaction $Transaction }
    finally { $command.Dispose() }
}

# Shared module-bundle helpers are covered by their own suites; here only the call contract is recorded.
function Invoke-ModuleBundle {
    param($Connection, $Transaction, $Bundle, [bool]$AllowFirstInstall = $false, [bool]$ReleaseAlreadyApplied = $true)
    $script:ModuleCalls.Add([pscustomobject]@{ Kind = 'Invoke'; Transaction = $null -ne $Transaction
            SameConnection = ($null -ne $Transaction -and
                [object]::ReferenceEquals($Transaction.Connection, $Connection))
            AllowFirstInstall = $AllowFirstInstall; ReleaseAlreadyApplied = $ReleaseAlreadyApplied
            ReleaseVersion = $Bundle.ReleaseVersion; Modules = @($Bundle.Modules).Count })
    foreach ($module in @($Bundle.Modules)) { "Module $($module.ObjectName) applied." }
}

function Get-DatabaseModuleState {
    param($Connection, $Transaction)
    $script:ModuleCalls.Add([pscustomobject]@{ Kind = 'State'; Database = $Connection.Database })
    return [pscustomobject]@{ Recorded = @(); Actual = @() }
}

function Assert-DatabaseModuleState {
    param($State, $Bundle, [bool]$AllowFirstInstall = $false, [bool]$RequireCurrentSource = $false)
    $script:ModuleCalls.Add([pscustomobject]@{ Kind = 'AssertState'; RequireCurrentSource = $RequireCurrentSource })
}

function Export-Clixml {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline)]$InputObject, [string]$LiteralPath, [switch]$NoClobber)
    process {
        # DPAPI is replaced: the file holds only the user name marker, never the password.
        if ($InputObject -isnot [Management.Automation.PSCredential]) { throw 'FAKE_CLIXML_TYPE' }
        if ($NoClobber -and [IO.File]::Exists($LiteralPath)) { throw 'FAKE_CLIXML_EXISTS' }
        [IO.File]::WriteAllText($LiteralPath, 'FAKE-DPAPI-CLIXML;' + $InputObject.UserName)
    }
}

function Import-Clixml {
    [CmdletBinding()]
    param([string]$LiteralPath)
    $text = [IO.File]::ReadAllText($LiteralPath)
    if (-not $text.StartsWith('FAKE-DPAPI-CLIXML;') -or $null -eq $script:Sql.Password) { throw 'FAKE_CLIXML_INVALID' }
    $secure = ConvertTo-SecureString -String $script:Sql.Password -AsPlainText -Force
    return [Management.Automation.PSCredential]::new($text.Substring(18), $secure)
}

# OS identity facts (machine/SID/admin) cannot be produced for the fixture: the gate is counted, and the real
# gate is exercised above (IND-EXEC-*).
function Assert-WindowsSqlExecutor {
    param($Plan, $Runtime)
    $script:ExecutorChecks++
}

# Certificate store and CSP machine key cannot be faked safely; the real check's rejections run in section 5.
function Assert-WindowsSqlCertificate {
    param($Adapter)
    $script:CertificateChecks++
}

# Instance map/Setup registry are read through OpenBaseKey directly; the fixture identity replaces that read.
# The remaining calls (SQL identity connection, certificate gate, WSL host check) stay the product's.
function Read-WindowsSqlInstanceIdentity {
    param($Adapter)
    [void](Get-WindowsSqlMaster -Adapter $Adapter)
    Assert-WindowsSqlCertificate -Adapter $Adapter
    [void](Get-WindowsSqlVerifyHost -Adapter $Adapter)
    return [pscustomobject]@{
        InstanceName = $script:FixtureInstance
        InstanceId = $script:FixtureInstanceId
        ProductVersion = $script:FixtureVersion
    }
}

function Add-MutationTrace {
    param([string]$Kind, [string]$Operation, $Adapter)
    $path = Join-Path $Adapter.Plan.PrivateRoot 'lifecycle.json'
    $status = $null
    $pending = $null
    if ([IO.File]::Exists($path)) {
        $journal = [IO.File]::ReadAllText($path) | ConvertFrom-Json
        $status = $journal.Status
        $pending = $journal.PendingOperation
    }
    $script:Mutations.Add([pscustomobject]@{ Kind = $Kind; Operation = $Operation; JournalStatus = $status
            JournalPending = $pending })
}

$script:RealInstanceMutation = ${function:Invoke-WindowsSqlInstanceMutation}
function Invoke-WindowsSqlInstanceMutation {
    param($Adapter, [string]$Operation, $Request)
    Add-MutationTrace -Kind 'Instance' -Operation $Operation -Adapter $Adapter
    if ($Operation -ceq 'RestartService') {
        # ServiceController is protected service control; only its observable effects are simulated.
        $script:Restarts.Add($Adapter.Plan.RestartApproved)
        if ($null -ne $Adapter.Master) {
            $Adapter.Master.Dispose()
            $Adapter.Master = $null
        }
        Update-FakeListeners
        return
    }
    & $script:RealInstanceMutation -Adapter $Adapter -Operation $Operation -Request $Request
}

$script:RealDatabaseMutation = ${function:Invoke-WindowsSqlDatabaseMutation}
function Invoke-WindowsSqlDatabaseMutation {
    param($Adapter, [string]$Operation)
    Add-MutationTrace -Kind 'Database' -Operation $Operation -Adapter $Adapter
    & $script:RealDatabaseMutation -Adapter $Adapter -Operation $Operation
}

$script:RealWritePrivateJson = ${function:Write-WindowsSqlPrivateJson}
function Write-WindowsSqlPrivateJson {
    param($Adapter, [string]$Name, $Value)
    & $script:RealWritePrivateJson -Adapter $Adapter -Name $Name -Value $Value
    $pending = Get-WindowsSqlField -Object $Value -Name 'PendingOperation'
    $script:JournalWrites.Add([pscustomobject]@{
            Name = $Name; Status = (Get-WindowsSqlField -Object $Value -Name 'Status'); Pending = $pending })
    $hook = $script:Faults.AfterWrite
    if ($null -ne $hook -and $Name -ceq $hook.Name -and $pending -ceq $hook.Pending) {
        $script:Faults.AfterWrite = $null
        & $hook.Action
    }
}

function New-FlowScenario {
    param([string]$Name, [ValidateSet('Nat', 'Mirrored')][string]$Mode = 'Nat', [bool]$RestartApproved = $true,
        [bool]$FirewallApproved = $true)
    Reset-FakeWorld -Mode $Mode
    $lifetime = [Guid]::NewGuid().ToString('N')
    $root = Join-Path $script:SuiteRoot ('flow-' + $Name)
    $source = Join-Path $root 'source'
    [void][IO.Directory]::CreateDirectory((Join-Path $source '.backups\evidence'))
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    New-WindowsSqlPrivateDirectory -Path $private -Sid $script:ExecutorSid
    foreach ($child in @('execution', 'empty-ca')) {
        [void][IO.Directory]::CreateDirectory((Join-Path $private $child))
        Set-WindowsSqlPrivateFileOwner -Path (Join-Path $private $child) -Sid $script:ExecutorSid
    }
    $tlsName = if ($Mode -ceq 'Nat') { 'laptop-fixture.internal' } else { 'localhost' }
    $accountId = [Guid]::NewGuid().ToString('D')
    $characterId = [Guid]::NewGuid().ToString('D')
    Write-JsonFile -Path (Join-Path $source 'profile.json') -Value ([ordered]@{
            persistedAccountName = 'fixture_user'
            database = 'Dawnholder_VerifierFixture'
            serverEndpoint = ('tcp:' + $tlsName + ',' + $script:FixturePort)
            slotId = 1
            accountId = $accountId
            characterId = $characterId
        })
    [IO.File]::WriteAllText((Join-Path $source 'public-ca.pem'), "verifier public fixture, not a certificate`n")
    Write-JsonFile -Path (Join-Path $source 'runtime.json') -Value ([ordered]@{
            Version = 1
            LifetimeId = $lifetime
            ExecutorSid = $script:ExecutorSid
            ServiceAccount = ('NT Service\MSSQL$' + $script:FixtureInstance)
            EndpointAddress = $(if ($Mode -ceq 'Nat') { '172.28.16.1' } else { '127.0.0.1' })
            TlsServerName = $tlsName
            NetworkMode = $Mode
            WslSubnet = $(if ($Mode -ceq 'Nat') { '172.28.16.0/20' } else { $null })
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
    $inputs = @('runtime.json', 'profile.json', 'public-ca.pem' | ForEach-Object {
            New-InputEntry -SourceRoot $source -Relative $_ })
    $planPath = Join-Path $root 'plan.json'
    Write-JsonFile -Path $planPath -Value (New-PlanDocument -Lifetime $lifetime -SourceRoot $source -Inputs $inputs `
            -RestartApproved $RestartApproved -FirewallApproved $FirewallApproved)
    $plan = Read-WindowsSqlPlan -Path $planPath -ExpectedHash (Get-FileSha256 -Path $planPath)
    $runtime = Read-WindowsSqlRuntime -Plan $plan -Relative 'runtime.json'
    $adapter = [pscustomobject]@{
        Kind = 'VerifierProtectedIoFake'
        Plan = $plan
        Runtime = $runtime
        RuntimePath = 'runtime.json'
        Lease = $null
        DirectoryLease = $null
        Mutex = $null
        MutexOwned = $false
        Master = $null
        Sources = $script:Sources
        ExpectedRegistry = $null
        LastTls = $null
    }
    $adapter | Add-Member -MemberType ScriptMethod -Name InvokeOperation -Value {
        param([string]$Operation, $Request)
        # Machine identity is the only dispatcher branch replaced: it reads this host's real name.
        if ($Operation -ceq 'ReadMachineIdentity') { return [pscustomobject]@{ MachineName = $script:FixtureMachine } }
        Invoke-WindowsSqlNativeOperation -Adapter $this -Operation $Operation -Request $Request
    }
    return [pscustomobject]@{ Plan = $plan; Runtime = $runtime; Adapter = $adapter; Private = $private; Root = $root
        AccountId = $accountId; CharacterId = $characterId }
}

function Invoke-Step {
    param($Scenario, [string]$Step)
    return Invoke-WindowsSqlStep -Plan $Scenario.Plan -Step $Step -Adapter $Scenario.Adapter
}

function Read-PrivateFile {
    param($Scenario, [string]$Name)
    $path = Join-Path $Scenario.Private $Name
    if (-not [IO.File]::Exists($path)) { return $null }
    return [IO.File]::ReadAllText($path) | ConvertFrom-Json
}

function Get-RegistryValue {
    param([string]$Suffix, [string]$Name)
    return [string]$script:Registry[$Suffix][$Name].Value
}

if ($null -eq $script:Sources) {
    Add-TestResult -Name 'IND-FLOW-00 flows need reader sources; not executed' -Outcome FAIL `
        -Detail 'reader output unavailable'
    Complete-TestSuite
}

# ---- Main NAT lifecycle with the real store and real helpers.
$main = New-FlowScenario -Name 'main-nat'
$preflight = Invoke-Step -Scenario $main -Step 'Preflight'
$preflightWrites = $script:RegistryWrites.Count + $script:Mutations.Count + @($script:FirewallCalls |
        Where-Object { $_ -notlike 'Get:*' }).Count
Assert-True -Name 'IND-FLOW-01 Preflight performs reads only (no registry/SQL/firewall mutation)' `
    -Condition ($preflightWrites -eq 0 -and -not [IO.File]::Exists((Join-Path $main.Private 'lifecycle.json')) -and
    -not [IO.File]::Exists((Join-Path $main.Private 'snapshot.json'))) -Detail ([string]$preflightWrites)
$observed = $preflight.Observation.Configuration
Assert-True -Name 'IND-FLOW-02 Preflight observation reports the fixture original configuration' -Condition (
    $observed.LoginMode -ceq 'Windows' -and -not $observed.TcpEnabled -and
    $observed.DynamicPorts -and $observed.ForceEncryption -and $null -eq $observed.CertificateThumbprint -and
    $preflight.Observation.Network.Mode -ceq 'Nat') -Detail ($observed | ConvertTo-Json -Compress)
Assert-Equal -Name 'IND-FLOW-03 Preflight result names the adapter kind' -Expected 'VerifierProtectedIoFake' `
    -Actual $preflight.AdapterKind

$planResult = Invoke-Step -Scenario $main -Step 'Plan'
$expectedChanges = @('AddFirewallRule', 'ApplyMigrations', 'CreateDatabase', 'CreateRuntimeLogin', 'RestartService',
    'SetMixedAuthentication', 'SetServerCertificate', 'SetTcpPort')
Assert-Equal -Name 'IND-FLOW-04 Plan decision for the fresh NAT laptop fixture' -Expected 'Apply' `
    -Actual $planResult.ChangePlan.Decision
Assert-Equal -Name 'IND-FLOW-05 Plan change set (requirement table)' -Expected ($expectedChanges -join '|') `
    -Actual ((@($planResult.ChangePlan.Changes) | Sort-Object) -join '|')

$script:Mutations.Clear()
$script:RegistryWrites.Clear()
$applyResult = Invoke-Step -Scenario $main -Step 'Apply'
Assert-Equal -Name 'IND-FLOW-06 Apply result' -Expected 'Applied' -Actual $applyResult.Status
$order = @($script:Mutations | ForEach-Object Operation)
$restartIndex = [Array]::IndexOf($order, 'RestartService')
$settingsBeforeRestart = @('SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate' | Where-Object {
        $index = [Array]::IndexOf($order, $_)
        $index -ge 0 -and $index -lt $restartIndex
    }).Count -eq 3
Assert-True -Name 'IND-FLOW-07 restart follows the three setting changes, migrations follow database creation' `
    -Condition ($settingsBeforeRestart -and
    [Array]::IndexOf($order, 'CreateDatabase') -lt [Array]::IndexOf($order, 'ApplyMigrations')) `
    -Detail ($order -join ',')
$journalBefore = @($script:Mutations | Where-Object {
        $_.JournalStatus -cne 'Applying' -or $_.JournalPending -cne $_.Operation })
$journalTrace = @($script:Mutations | ForEach-Object {
        $_.Operation + ':' + $_.JournalStatus + '/' + $_.JournalPending })
Assert-True -Name 'IND-FLOW-08 every mutation runs after a durable Applying journal naming it' `
    -Condition ($journalBefore.Count -eq 0 -and $script:Mutations.Count -eq 8) -Detail ($journalTrace -join ',')
Assert-True -Name 'IND-FLOW-09 dispatcher re-ran the executor gate for every native operation' `
    -Condition ($script:ExecutorChecks -gt 0) -Detail ([string]$script:ExecutorChecks)
$ipAll = '\MSSQLServer\SuperSocketNetLib\Tcp\IPAll'
$netLib = '\MSSQLServer\SuperSocketNetLib'
Assert-True -Name 'IND-FLOW-10 Apply sets mixed auth, fixed TCP port, no dynamic port, exact certificate' -Condition (
    (Get-RegistryValue '\MSSQLServer' 'LoginMode') -ceq '2' -and
    (Get-RegistryValue '\MSSQLServer\SuperSocketNetLib\Tcp' 'Enabled') -ceq '1' -and
    (Get-RegistryValue $ipAll 'TcpPort') -ceq [string]$script:FixturePort -and
    (Get-RegistryValue $ipAll 'TcpDynamicPorts') -ceq '' -and
    (Get-RegistryValue $netLib 'Certificate') -ieq $script:FixtureThumbprint) `
    -Detail (($script:RegistryWrites | ForEach-Object { $_.Name + '=' + $_.Value }) -join ',')
$forceWrites = @($script:RegistryWrites | Where-Object Name -CEQ 'ForceEncryption')
Assert-True -Name 'IND-FLOW-11 ForceEncryption is never written and keeps its original value' `
    -Condition ($forceWrites.Count -eq 0 -and (Get-RegistryValue $netLib 'ForceEncryption') -ceq '1')
$rule = @($script:Firewall.Values)
Assert-True -Name 'IND-FLOW-12 firewall rule is exact: inbound TCP port, WSL subnet, endpoint, SQL service only' `
    -Condition (
    $rule.Count -eq 1 -and $rule[0].RemoteAddress -ceq '172.28.16.0/20' -and
    $rule[0].LocalAddress -ceq '172.28.16.1' -and
    $rule[0].LocalPort -ceq [string]$script:FixturePort -and $rule[0].Protocol -ceq 'TCP' -and
    $rule[0].Direction -ceq 'Inbound' -and $rule[0].Service -ceq ('MSSQL$' + $script:FixtureInstance) -and
    $rule[0].EdgeTraversalPolicy -ceq 'Block' -and $rule[0].Description.Contains($main.Plan.PlanHash)) `
    -Detail ($rule | ConvertTo-Json -Compress)
$moduleInvoke = @($script:ModuleCalls | Where-Object Kind -CEQ 'Invoke')
Assert-True -Name 'IND-FLOW-13 migrations 001..004, module bundle and binding commit in one owned transaction' `
    -Condition (
    (@($script:Sql.Versions | ForEach-Object Name) -join '|') -ceq
    '001_initial.sql|002_persistence_metadata.sql|003_module_metadata.sql|004_module_release.sql' -and
    $moduleInvoke.Count -eq 1 -and $moduleInvoke[0].Transaction -and $moduleInvoke[0].SameConnection -and
    $moduleInvoke[0].AllowFirstInstall -and -not $moduleInvoke[0].ReleaseAlreadyApplied -and
    $null -ne $script:Sql.Binding -and $script:Sql.Binding.Account -eq [Guid]$main.AccountId -and
    $script:Sql.Binding.Character -eq [Guid]$main.CharacterId -and $script:Sql.DbOwner -ceq $main.Plan.LifetimeId) `
    -Detail ($moduleInvoke | ConvertTo-Json -Compress)
$password = [string]$script:Sql.Password
Assert-True -Name 'IND-FLOW-14 runtime login password is generated (>=16, policy shape) and not reused' `
    -Condition ($password.Length -ge 16 -and $password -cmatch '[A-Z]' -and $password -cmatch '[a-z]' -and
    $password -cmatch '[0-9]' -and $password -match '[^A-Za-z0-9]') -Detail ([string]$password.Length)
$resources = Read-PrivateFile -Scenario $main -Name 'resources.json'
$credentialPath = Join-Path $main.Private 'runtime.clixml'
Assert-True -Name 'IND-FLOW-15 owned receipt binds exact DB GUID, login SID, credential hash and firewall rule' `
    -Condition (
    $null -ne $resources -and $resources.DatabaseGuid -ceq $script:Sql.DbGuid.ToString().ToUpperInvariant() -and
    $resources.LoginSid -ceq [Convert]::ToHexString($script:Sql.LoginSid) -and
    $resources.CredentialHash -ceq (Get-FileSha256 -Path $credentialPath) -and
    $resources.Firewall -ceq ('Dawnholder.WindowsSql.' + $main.Plan.LifetimeId)) `
    -Detail ($resources | ConvertTo-Json -Compress)
$credentialAcl = Get-Acl -LiteralPath $credentialPath
$foreignAllow = @($credentialAcl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) | Where-Object {
        $_.AccessControlType -eq 'Allow' -and $_.IdentityReference.Value -cnotin @($script:ExecutorSid, 'S-1-5-18') })
Assert-True -Name 'IND-FLOW-16 credential file is owned by the executor with no other allowed principal' -Condition (
    $credentialAcl.GetOwner([Security.Principal.SecurityIdentifier]).Value -ceq $script:ExecutorSid -and
    $foreignAllow.Count -eq 0)
$journal = Read-PrivateFile -Scenario $main -Name 'lifecycle.json'
$snapshot = Read-PrivateFile -Scenario $main -Name 'snapshot.json'
Assert-True -Name 'IND-FLOW-17 journal Applied with plan hash; snapshot holds the original registry values' -Condition (
    $journal.Status -ceq 'Applied' -and $journal.PlanHash -ceq $main.Plan.PlanHash -and
    @($journal.CompletedChanges).Count -eq 8 -and $snapshot.Registry.LoginMode -eq 1 -and
    $snapshot.Registry.Enabled -eq 0 -and
    $snapshot.Registry.TcpPort -ceq '' -and $snapshot.Registry.TcpDynamicPorts -ceq '0' -and
    $snapshot.Registry.Certificate -ceq '' -and $snapshot.Registry.ForceEncryption -eq 1)
$foreignConnections = @($script:ConnectionOpens | Where-Object { $_ -cnotin @('master', 'Dawnholder_VerifierFixture') })
Assert-True -Name 'IND-FLOW-18 only master and the exact new database were connected' `
    -Condition ($foreignConnections.Count -eq 0 -and $script:Violations.Count -eq 0) `
    -Detail ((@($script:Violations) + @($foreignConnections)) -join ',')

$mutationCount = $script:Mutations.Count
$journalText = [IO.File]::ReadAllText((Join-Path $main.Private 'lifecycle.json'))
$rerun = Invoke-Step -Scenario $main -Step 'Apply'
Assert-True -Name 'IND-FLOW-19 re-running Apply is Unchanged without any mutation or journal rewrite' -Condition (
    $rerun.Status -ceq 'Unchanged' -and $script:Mutations.Count -eq $mutationCount -and
    [IO.File]::ReadAllText((Join-Path $main.Private 'lifecycle.json')) -ceq $journalText) -Detail $rerun.Status

$verify = Invoke-Step -Scenario $main -Step 'Verify'
$verifyCall = @($script:VerifyCalls)
Assert-Equal -Name 'IND-FLOW-20 Verify result' -Expected 'Verified' -Actual $verify.Status
$payload = if ($verifyCall.Count -eq 1) { $verifyCall[0].InputText | ConvertFrom-Json } else { $null }
$argvText = if ($verifyCall.Count -eq 1) { $verifyCall[0].Arguments -join ' ' } else { '' }
Assert-True -Name 'IND-FLOW-21 runtime secret reaches the WSL host only through stdin' -Condition (
    $null -ne $payload -and $payload.Password -ceq $password -and -not $argvText.Contains($password) -and
    $payload.RuntimeLogin -ceq 'dh_verifier_fixture' -and $payload.ServerName -ceq 'laptop-fixture.internal')
Assert-True -Name 'IND-FLOW-22 WSL host gets only the child CA file and an empty CA directory, with a hard timeout' `
    -Condition ($argvText -match 'SSL_CERT_FILE=/mnt/[a-z]/.+/public-ca\.pem' -and
    $argvText -match 'SSL_CERT_DIR=/mnt/[a-z]/.+/empty-ca' -and $argvText -match '\btimeout\b.*\b40s\b' -and
    $verifyCall[0].Timeout -le 45) -Detail $argvText

foreach ($case in @(
        @{ Name = 'named pipes transport'; Output = '{"Status":"Verified","Transport":"Named Pipes","Encrypted":true,' +
            '"CertificateValidated":true,"RuntimeExecuteGrants":5,"DirectDmlDenied":true}'; Fails = $false },
        @{ Name = 'certificate not validated'; Output = '{"Status":"Verified","Transport":"TCP","Encrypted":true,' +
            '"CertificateValidated":false,"RuntimeExecuteGrants":5,"DirectDmlDenied":true}'; Fails = $false },
        @{ Name = 'host failure exit'; Output = ''; Fails = $true })) {
    $script:Wsl.HostOutput = $case.Output
    $script:Wsl.HostFails = $case.Fails
    $message = Get-CaughtMessage { Invoke-Step -Scenario $main -Step 'Verify' }
    Assert-True -Name ('IND-FLOW-23 Verify rejects ' + $case.Name) `
        -Condition ($message -ceq 'WINDOWS_SQL_TLS_VERIFY_FAILED') -Detail ([string]$message)
}
$script:Wsl.HostOutput = '{"Status":"Verified","Transport":"TCP","Encrypted":true,"CertificateValidated":true,' +
'"RuntimeExecuteGrants":5,"DirectDmlDenied":true}'
$script:Wsl.HostFails = $false
$script:Sql.BindingMismatch = $true
$message = Get-CaughtMessage { Invoke-Step -Scenario $main -Step 'Verify' }
Assert-True -Name 'IND-FLOW-24 Verify rejects a catalog/binding mismatch on the owned database' `
    -Condition ($message -ceq 'WINDOWS_SQL_CATALOG_VERIFY_FAILED') -Detail ([string]$message)
$script:Sql.BindingMismatch = $false

$script:Mutations.Clear()
$script:RegistryWrites.Clear()
$cleanup = Invoke-Step -Scenario $main -Step 'Cleanup'
Assert-Equal -Name 'IND-FLOW-25 Cleanup result' -Expected 'Cleaned' -Actual $cleanup.Status
Assert-Equal -Name 'IND-FLOW-26 Cleanup order: login, database, firewall, restore, restart' `
    -Expected 'RemoveRuntimeLogin|RemoveDatabase|RemoveFirewallRule|RestoreConfiguration|RestartService' `
    -Actual ((@($script:Mutations | ForEach-Object Operation)) -join '|')
Assert-True -Name 'IND-FLOW-27 Cleanup restores every original value and leaves ForceEncryption untouched' -Condition (
    (Get-RegistryValue '\MSSQLServer' 'LoginMode') -ceq '1' -and
    (Get-RegistryValue '\MSSQLServer\SuperSocketNetLib\Tcp' 'Enabled') -ceq '0' -and
    (Get-RegistryValue $ipAll 'TcpPort') -ceq '' -and (Get-RegistryValue $ipAll 'TcpDynamicPorts') -ceq '0' -and
    (Get-RegistryValue $netLib 'Certificate') -ceq '' -and (Get-RegistryValue $netLib 'ForceEncryption') -ceq '1' -and
    @($script:RegistryWrites | Where-Object Name -CEQ 'ForceEncryption').Count -eq 0) `
    -Detail (($script:RegistryWrites | ForEach-Object { $_.Name + '=' + $_.Value }) -join ',')
Assert-True -Name 'IND-FLOW-28 Cleanup removed exactly the owned login, database and firewall rule' -Condition (
    -not $script:Sql.DbExists -and -not $script:Sql.LoginExists -and $script:Firewall.Count -eq 0 -and
    (Read-PrivateFile -Scenario $main -Name 'lifecycle.json').Status -ceq 'Cleaned')
Assert-True `
    -Name 'IND-FLOW-29 Cleanup keeps snapshot, receipt and credential evidence for the separate certificate step' `
    -Condition ([IO.File]::Exists((Join-Path $main.Private 'snapshot.json')) -and
    [IO.File]::Exists((Join-Path $main.Private 'resources.json')) -and [IO.File]::Exists($credentialPath))
$script:Mutations.Clear()
$again = Invoke-Step -Scenario $main -Step 'Cleanup'
Assert-True -Name 'IND-FLOW-30 repeated Cleanup is Cleaned without any mutation' `
    -Condition ($again.Status -ceq 'Cleaned' -and $script:Mutations.Count -eq 0)
$message = Get-CaughtMessage { Invoke-Step -Scenario $main -Step 'Apply' }
Assert-True -Name 'IND-FLOW-31 a cleaned lifetime cannot be re-applied under the same plan' `
    -Condition ($message -ceq 'WINDOWS_SQL_LIFECYCLE_STATE_CHANGED' -and $script:Mutations.Count -eq 0) `
    -Detail ([string]$message)

# ---- Partial failure: a registry write fails after another write of the same operation already happened.
$partial = New-FlowScenario -Name 'partial-tcp'
$script:Faults.RegistryWrite = 'TcpPort'
$message = Get-CaughtMessage { Invoke-Step -Scenario $partial -Step 'Apply' }
$journal = Read-PrivateFile -Scenario $partial -Name 'lifecycle.json'
Assert-True -Name 'IND-FAIL-01 interrupted SetTcpPort records Failed with the pending operation and no rollback' `
    -Condition (
    $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and $journal.Status -ceq 'Failed' -and
    $journal.FailedOperation -ceq 'SetTcpPort' -and $journal.PendingOperation -ceq 'SetTcpPort' -and
    (@($journal.CompletedChanges) -join '|') -ceq 'SetMixedAuthentication' -and
    (Get-RegistryValue $ipAll 'TcpDynamicPorts') -ceq '') -Detail ([string]$message)
$script:Mutations.Clear()
$script:RegistryWrites.Clear()
$message = Get-CaughtMessage { Invoke-Step -Scenario $partial -Step 'Cleanup' }
Assert-True -Name 'IND-FAIL-02 Cleanup refuses the uncertain partial state before any removal or restore' -Condition (
    $message -ceq 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' -and $script:Mutations.Count -eq 0 -and
    $script:RegistryWrites.Count -eq 0) -Detail ([string]$message)
$message = Get-CaughtMessage { Invoke-Step -Scenario $partial -Step 'Apply' }
Assert-True -Name 'IND-FAIL-03 Apply after a failed journal is refused instead of re-snapshotting' `
    -Condition ($message -ceq 'WINDOWS_SQL_LIFECYCLE_STATE_CHANGED' -and $script:Mutations.Count -eq 0) `
    -Detail ([string]$message)

# ---- Database created, then its ownership transaction fails: ownership is not inferred.
$dbFail = New-FlowScenario -Name 'db-schema-fail'
$script:Sql.FailMarker = 'CREATE SCHEMA dh AUTHORIZATION dbo'
$message = Get-CaughtMessage { Invoke-Step -Scenario $dbFail -Step 'Apply' }
$journal = Read-PrivateFile -Scenario $dbFail -Name 'lifecycle.json'
Assert-True -Name 'IND-FAIL-04 failed database ownership step is journaled as CreateDatabase failure' -Condition (
    $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and $journal.FailedOperation -ceq 'CreateDatabase' -and
    $script:Sql.DbExists -and $null -eq $script:Sql.DbOwner) -Detail ([string]$message)
$script:Mutations.Clear()
$message = Get-CaughtMessage { Invoke-Step -Scenario $dbFail -Step 'Cleanup' }
Assert-True -Name 'IND-FAIL-05 Cleanup does not drop a database whose ownership was never confirmed' -Condition (
    $message -cin @('WINDOWS_SQL_TARGET_NOT_OWNED', 'WINDOWS_SQL_CLEANUP_STATE_CHANGED') -and
    $script:Mutations.Count -eq 0 -and $script:Sql.DbExists) -Detail ([string]$message)

# ---- Login creation fails after the DPAPI credential was persisted.
$loginFail = New-FlowScenario -Name 'login-fail'
$script:Sql.FailMarker = "N'CREATE LOGIN '"
$message = Get-CaughtMessage { Invoke-Step -Scenario $loginFail -Step 'Apply' }
$loginCredential = Join-Path $loginFail.Private 'runtime.clixml'
Assert-True -Name 'IND-FAIL-06 login failure after credential persistence keeps the orphan credential' -Condition (
    $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and -not $script:Sql.LoginExists -and
    [IO.File]::Exists($loginCredential)) -Detail ([string]$message)
$cleanupLogin = Get-CaughtMessage { $script:LoginCleanup = Invoke-Step -Scenario $loginFail -Step 'Cleanup' }
$loginReceipt = Read-PrivateFile -Scenario $loginFail -Name 'resources.json'
Assert-True -Name 'IND-FAIL-07 Cleanup after a confirmed-absent login removes the owned database and restores' `
    -Condition (
    $null -eq $cleanupLogin -and $script:LoginCleanup.Status -ceq 'Cleaned' -and -not $script:Sql.DbExists -and
    (Get-RegistryValue '\MSSQLServer' 'LoginMode') -ceq '1') -Detail ([string]$cleanupLogin)
Add-TestResult -Name 'IND-FAIL-08 orphan credential after login failure has no receipt hash for certificate cleanup' `
    -Outcome OBSERVED -Detail ('credential exists=' + [IO.File]::Exists($loginCredential) +
    '; receipt CredentialHash=' + [string]$loginReceipt.CredentialHash)

# ---- Another actor changes ForceEncryption after the journal: the change is detected before any write.
$foreign = New-FlowScenario -Name 'foreign-change'
$script:Faults.AfterWrite = @{ Name = 'lifecycle.json'; Pending = 'SetMixedAuthentication'; Action = {
        $script:Registry['\MSSQLServer\SuperSocketNetLib']['ForceEncryption'] = @{ Value = [int]0; Kind = 'DWord' }
    } }
$message = Get-CaughtMessage { Invoke-Step -Scenario $foreign -Step 'Apply' }
Assert-True -Name 'IND-FAIL-09 concurrent configuration change stops Apply before the first registry write' -Condition (
    $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and $script:RegistryWrites.Count -eq 0 -and
    (Read-PrivateFile -Scenario $foreign -Name 'lifecycle.json').Status -ceq 'Failed') -Detail ([string]$message)
$script:Mutations.Clear()
$message = Get-CaughtMessage { Invoke-Step -Scenario $foreign -Step 'Cleanup' }
Assert-True -Name 'IND-FAIL-10 Cleanup refuses to overwrite another actor''s ForceEncryption change' -Condition (
    $message -ceq 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' -and $script:Mutations.Count -eq 0 -and
    $script:RegistryWrites.Count -eq 0) -Detail ([string]$message)

# ---- The private store becomes permissive after the snapshot: the journal write fails, no mutation follows.
$acl = New-FlowScenario -Name 'unsafe-acl'
$usersRule = [Security.AccessControl.FileSystemAccessRule]::new(
    [Security.Principal.SecurityIdentifier]::new('S-1-5-32-545'), 'ReadAndExecute', 'ContainerInherit, ObjectInherit',
    'None', 'Allow')
function Set-TestDirectoryRule {
    param([string]$Path, [switch]$Remove)
    # DACL-only persistence on this suite's own folder (Set-Acl also requests the SACL here).
    $directory = [IO.DirectoryInfo]::new($Path)
    $security = [IO.FileSystemAclExtensions]::GetAccessControl($directory,
        [Security.AccessControl.AccessControlSections]::Access)
    if ($Remove) { [void]$security.RemoveAccessRule($usersRule) } else { $security.AddAccessRule($usersRule) }
    [IO.FileSystemAclExtensions]::SetAccessControl($directory, $security)
}
$script:AclHookApplied = $false
$script:Faults.AfterWrite = @{ Name = 'snapshot.json'; Pending = $null; Action = {
        Set-TestDirectoryRule -Path $acl.Private
        $script:AclHookApplied = $true
    } }
$message = Get-CaughtMessage { Invoke-Step -Scenario $acl -Step 'Apply' }
Assert-True -Name 'IND-FAIL-11 permissive private store blocks the journal and every mutation' -Condition (
    $script:AclHookApplied -and $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and
    $script:Mutations.Count -eq 0 -and $script:RegistryWrites.Count -eq 0 -and
    -not [IO.File]::Exists((Join-Path $acl.Private 'lifecycle.json'))) `
    -Detail ([string]$message + '; hook=' + $script:AclHookApplied)
Set-TestDirectoryRule -Path $acl.Private -Remove
$message = Get-CaughtMessage { Invoke-Step -Scenario $acl -Step 'Apply' }
Assert-True -Name 'IND-FAIL-12 retry after the store is repaired refuses to replace the existing snapshot' -Condition (
    $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and $script:Mutations.Count -eq 0 -and
    $script:RegistryWrites.Count -eq 0) -Detail ([string]$message)

# ---- A foreign process takes the port between the plan and SetTcpPort.
$port = New-FlowScenario -Name 'port-race'
$script:Faults.AfterWrite = @{ Name = 'lifecycle.json'; Pending = 'SetTcpPort'; Action = {
        $script:Listeners.Add([pscustomobject]@{ LocalPort = $script:FixturePort; OwningProcess = 9999 })
    } }
$message = Get-CaughtMessage { Invoke-Step -Scenario $port -Step 'Apply' }
Assert-True -Name 'IND-FAIL-13 port conflict at mutation time stops before the TCP port is written' -Condition (
    $message -ceq 'WINDOWS_SQL_APPLY_FAILED' -and
    @($script:RegistryWrites | Where-Object Name -CIn @('TcpPort', 'TcpDynamicPorts', 'Enabled')).Count -eq 0 -and
    (Read-PrivateFile -Scenario $port -Name 'lifecycle.json').FailedOperation -ceq 'SetTcpPort') `
    -Detail ([string]$message)

# ---- Approval expiry is re-checked at mutation time.
$expiry = New-FlowScenario -Name 'expiry'
$state = [pscustomobject]@{ LifetimeId = $expiry.Plan.LifetimeId; PlanHash = $expiry.Plan.PlanHash; Status = 'Applying'
    PendingOperation = 'SetMixedAuthentication' }
Write-WindowsSqlPrivateJson -Adapter $expiry.Adapter -Name 'lifecycle.json' -Value $state
$expiry.Plan.ExpiresUtc = '2020-01-01T00:00:00Z'
$message = Get-CaughtMessage {
    Invoke-WindowsSqlNativeOperation -Adapter $expiry.Adapter -Operation 'SetMixedAuthentication' `
        -Request (New-WindowsSqlRequest -Plan $expiry.Plan)
}
Assert-True -Name 'IND-FAIL-14 an expired approval stops a journaled mutation before it runs' -Condition (
    $message -ceq 'WINDOWS_SQL_APPROVAL_EXPIRED' -and $script:RegistryWrites.Count -eq 0) -Detail ([string]$message)

# ---- A rule with this lifetime's name but no owned receipt is never adopted.
$stale = New-FlowScenario -Name 'stale-rule'
$staleName = 'Dawnholder.WindowsSql.' + $stale.Plan.LifetimeId
$script:Firewall[$staleName] = [pscustomobject]@{
    Name = $staleName; Description = 'Dawnholder WindowsSql ' + $stale.Plan.PlanHash
    Direction = 'Inbound'; Action = 'Allow'; Enabled = 'True'; Protocol = 'TCP'; LocalPort = [string]$script:FixturePort
    LocalAddress = '172.28.16.1'; RemoteAddress = '172.28.16.0/20'; Service = ('MSSQL$' + $script:FixtureInstance) }
$message = Get-CaughtMessage { Invoke-Step -Scenario $stale -Step 'Preflight' }
Assert-True -Name 'IND-FAIL-15 an unowned same-name firewall rule stops Preflight without adoption' -Condition (
    $message -ceq 'WINDOWS_SQL_ADAPTER_FAILED' -and $script:Firewall.Count -eq 1 -and
    @($script:FirewallCalls | Where-Object { $_ -notlike 'Get:*' }).Count -eq 0) -Detail ([string]$message)

# ---- An existing database with the target name but no receipt: never opened, never read, never changed.
$existing = New-FlowScenario -Name 'existing-db'
$script:Sql.DbExists = $true
$script:Sql.DbGuid = [Guid]::NewGuid()
$planExisting = Invoke-Step -Scenario $existing -Step 'Plan'
Assert-True -Name 'IND-PROT-01 an existing unowned target database blocks the plan' -Condition (
    $planExisting.ChangePlan.Decision -ceq 'Blocked' -and
    @($planExisting.ChangePlan.Blockers) -ccontains 'WINDOWS_SQL_TARGET_NOT_OWNED') `
    -Detail (@($planExisting.ChangePlan.Blockers) -join ',')
$message = Get-CaughtMessage { Invoke-Step -Scenario $existing -Step 'Apply' }
Assert-True -Name 'IND-PROT-02 the unowned database is never connected to, and Apply mutates nothing' -Condition (
    $message -ceq 'WINDOWS_SQL_TARGET_NOT_OWNED' -and $script:Mutations.Count -eq 0 -and
    @($script:ConnectionOpens | Where-Object { $_ -cne 'master' }).Count -eq 0 -and
    -not [IO.File]::Exists((Join-Path $existing.Private 'lifecycle.json'))) -Detail ([string]$message)

# ---- Mirrored networking: no firewall work; Verify before Apply never opens the WSL host.
$mirror = New-FlowScenario -Name 'mirrored' -Mode 'Mirrored'
$message = Get-CaughtMessage { Invoke-Step -Scenario $mirror -Step 'Verify' }
Assert-True -Name 'IND-NET-01 Verify before Apply fails on the catalog without starting the TLS host' -Condition (
    $message -ceq 'WINDOWS_SQL_CATALOG_VERIFY_FAILED' -and $script:VerifyCalls.Count -eq 0) -Detail ([string]$message)
$mirrorPlan = Invoke-Step -Scenario $mirror -Step 'Plan'
Assert-True -Name 'IND-NET-02 mirrored plan has no firewall change' `
    -Condition (@($mirrorPlan.ChangePlan.Changes) -cnotcontains 'AddFirewallRule' -and
    $mirrorPlan.ChangePlan.Decision -ceq 'Apply')
$mirrorApply = Invoke-Step -Scenario $mirror -Step 'Apply'
Assert-True -Name 'IND-NET-03 mirrored Apply creates no firewall rule and reads no NAT route' -Condition (
    $mirrorApply.Status -ceq 'Applied' -and $script:Firewall.Count -eq 0 -and
    @($script:FirewallCalls | Where-Object { $_ -like 'New:*' }).Count -eq 0 -and
    @($script:WslCalls | Where-Object Command -CEQ 'ip').Count -eq 0)

# ---- No restart approval: a configuration change needs it, so Apply is refused before any effect.
$noRestart = New-FlowScenario -Name 'no-restart' -RestartApproved $false
$message = Get-CaughtMessage { Invoke-Step -Scenario $noRestart -Step 'Apply' }
Assert-True -Name 'IND-NET-04 required restart without approval refuses Apply with no effect' -Condition (
    $message -ceq 'WINDOWS_SQL_RESTART_NOT_APPROVED' -and $script:Mutations.Count -eq 0 -and
    -not [IO.File]::Exists((Join-Path $noRestart.Private 'snapshot.json'))) -Detail ([string]$message)

# ---- Adapter construction refuses a plan whose source root is not this tool checkout (before mutex/lease).
$sourceRootError = Get-CaughtMessage { New-WindowsSqlAdapter -Plan $mirror.Plan -RuntimePath 'runtime.json' }
Assert-True -Name 'IND-ADAPTER-01 adapter refuses a plan for another source root' `
    -Condition ($sourceRootError -ceq 'WINDOWS_SQL_SOURCE_ROOT_MISMATCH') -Detail ([string]$sourceRootError)

# ---- Secret hygiene across every file this suite wrote.
$generated = @($script:GeneratedPasswords | Where-Object { $_ } | Select-Object -Unique)
Add-TestResult -Name 'IND-SECRET-00 generated runtime passwords captured for the scan' -Outcome OBSERVED `
    -Detail ([string]$generated.Count)
$leaks = @(Get-ChildItem -LiteralPath $script:SuiteRoot -Recurse -File -Force | Where-Object {
        $text = [IO.File]::ReadAllText($_.FullName)
        @($generated | Where-Object { $text.Contains($_) }).Count -gt 0
    })
Assert-True -Name 'IND-SECRET-01 generated runtime passwords appear in no file, journal, receipt or credential' `
    -Condition ($leaks.Count -eq 0) -Detail (@($leaks | ForEach-Object Name) -join ',')
Assert-True -Name 'IND-SECRET-02 no forbidden real command was reached' `
    -Condition ($script:ForbiddenCalls.Count -eq 0) -Detail (@($script:ForbiddenCalls) -join ',')
$serviceCalls = @([AdapterFakeServiceController]::Calls)
$instanceService = 'MSSQL$' + $script:FixtureInstance
$controlCalls = @($serviceCalls | Where-Object { $_ -clike 'Control:*' })
$otherServices = @($serviceCalls | Where-Object { $_ -clike 'Open:*' -and $_ -cne ('Open:' + $instanceService) })
$opened = @($serviceCalls | Where-Object { $_ -clike 'Open:*' }).Count
Assert-True -Name 'IND-SCM-01 service observation opened only the exact instance and never controlled a service' `
    -Condition ($opened -gt 0 -and $controlCalls.Count -eq 0 -and $otherServices.Count -eq 0) `
    -Detail ('opened={0}; control={1}; other={2}' -f $opened, ($controlCalls -join ','), ($otherServices -join ','))

# =====================================================================================================
# 5. Real certificate gate rejections with in-memory certificates (no store, no persisted key).
# =====================================================================================================
$certScenario = New-FlowScenario -Name 'certificate' -Mode 'Mirrored'
$script:CertificateFixtures = @{}
function Get-Item {
    [CmdletBinding()]
    param([string]$LiteralPath, [switch]$Force)
    if ($LiteralPath -like 'Cert:*') {
        $script:CertificateReads++
        $thumbprint = Split-Path -Leaf $LiteralPath
        if (-not $script:CertificateFixtures.ContainsKey($thumbprint)) { throw 'FAKE_CERTIFICATE_MISSING' }
        return $script:CertificateFixtures[$thumbprint]
    }
    return Microsoft.PowerShell.Management\Get-Item -LiteralPath $LiteralPath -Force:$Force
}

function New-TestCertificate {
    param([string[]]$DnsNames, [string]$Eku = '1.3.6.1.5.5.7.3.1', [double]$Days = 30, [switch]$PublicOnly)
    $rsa = [Security.Cryptography.RSA]::Create(2048)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            ('CN=' + $script:FixtureMachine), $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        foreach ($name in $DnsNames) { $san.AddDnsName($name) }
        $request.CertificateExtensions.Add($san.Build())
        $oids = [Security.Cryptography.OidCollection]::new()
        [void]$oids.Add([Security.Cryptography.Oid]::new($Eku))
        $request.CertificateExtensions.Add(
            [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids, $false))
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                [Security.Cryptography.X509Certificates.X509KeyUsageFlags]'DigitalSignature, KeyEncipherment', $true))
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1),
            [DateTimeOffset]::UtcNow.AddDays($Days))
        if ($PublicOnly) {
            $public = [Security.Cryptography.X509Certificates.X509Certificate2]::new($certificate.RawData)
            $certificate.Dispose()
            return $public
        }
        return $certificate
    }
    finally { $rsa.Dispose() }
}

function Set-CertificateCase {
    param($Scenario, $Certificate, [string]$ReceiptThumbprint)
    $receiptPath = Join-Path $Scenario.Private 'certificate.json'
    if ([IO.File]::Exists($receiptPath)) { [IO.File]::Delete($receiptPath) }
    Write-JsonFile -Path $receiptPath -Value ([ordered]@{
            Version = 1; PreparationHash = ('E' * 64); LifetimeId = $Scenario.Plan.LifetimeId
            MachineName = $script:FixtureMachine; InstanceName = $script:FixtureInstance
            ExecutorSid = $script:ExecutorSid
            Thumbprint = $ReceiptThumbprint; PublicCaSha256 = ('F' * 64)
            KeyFile = 'C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\fixture'; ServiceSid = 'S-1-5-80-0'
        })
    Set-WindowsSqlPrivateFileOwner -Path $receiptPath -Sid $script:ExecutorSid
    $Scenario.Plan.CertificateThumbprint = $Certificate.Thumbprint
    $Scenario.Runtime.CertificateReceiptHash = Get-FileSha256 -Path $receiptPath
    $script:CertificateFixtures[$Certificate.Thumbprint] = $Certificate
}

$certificateCases = @(
    @{ Id = 'IND-CERT-01'; Name = 'receipt hash differs (store not read)'
        Code = 'WINDOWS_SQL_CERTIFICATE_RECEIPT_CHANGED'
        Make = { New-TestCertificate -DnsNames @('localhost', $script:FixtureMachine) }; BreakReceipt = $true },
    @{ Id = 'IND-CERT-02'; Name = 'no private key'; Code = 'WINDOWS_SQL_CERTIFICATE_INVALID'
        Make = { New-TestCertificate -DnsNames @('localhost', $script:FixtureMachine) -PublicOnly } },
    @{ Id = 'IND-CERT-03'; Name = 'client-auth EKU only'; Code = 'WINDOWS_SQL_CERTIFICATE_INVALID'
        Make = { New-TestCertificate -DnsNames @('localhost', $script:FixtureMachine) -Eku '1.3.6.1.5.5.7.3.2' } },
    @{ Id = 'IND-CERT-04'; Name = 'expires within 24 hours'; Code = 'WINDOWS_SQL_CERTIFICATE_INVALID'
        Make = { New-TestCertificate -DnsNames @('localhost', $script:FixtureMachine) -Days 0.5 } },
    @{ Id = 'IND-CERT-05'; Name = 'SAN lacks the TLS server name'; Code = 'WINDOWS_SQL_CERTIFICATE_NAME_MISMATCH'
        Make = { New-TestCertificate -DnsNames @('other.example', $script:FixtureMachine) } },
    @{ Id = 'IND-CERT-06'; Name = 'CNG key instead of a CSP AT_KEYEXCHANGE machine key'
        Code = 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED'
        Make = { New-TestCertificate -DnsNames @('localhost', $script:FixtureMachine) }
        # CERT_KEY_PROV_INFO of a machine key in a CNG key storage provider: provider type 0, KeySpec 0.
        ProviderInfo = {
            $parameters = [Security.Cryptography.CspParameters]::new(0, 'Microsoft Software Key Storage Provider',
                'adapter-fixture-ksp')
            $parameters.KeyNumber = 0
            $parameters.Flags = [Security.Cryptography.CspProviderFlags]::UseMachineKeyStore
            return $parameters
        } }
)
$keyCaseThumbprint = $null
foreach ($case in $certificateCases) {
    $certificate = & $case.Make
    Set-CertificateCase -Scenario $certScenario -Certificate $certificate -ReceiptThumbprint $certificate.Thumbprint
    if ($case.ContainsKey('BreakReceipt')) { $certScenario.Runtime.CertificateReceiptHash = ('0' * 64) }
    [AdapterFakeCertificateKeyNative]::ExpectedCertificate = $certificate
    if ($case.ContainsKey('ProviderInfo')) {
        $keyCaseThumbprint = $certificate.Thumbprint
        [AdapterFakeCertificateKeyNative]::ProviderInfo[$keyCaseThumbprint] = & $case.ProviderInfo
    }
    $script:CertificateReads = 0
    $message = Get-CaughtMessage { & $script:RealAssertCertificate -Adapter $certScenario.Adapter }
    $storeReadOk = if ($case.ContainsKey('BreakReceipt')) { $script:CertificateReads -eq 0 } else { $true }
    Assert-True -Name ($case.Id + ' certificate gate rejects ' + $case.Name) `
        -Condition ($message -ceq $case.Code -and $storeReadOk) -Detail ([string]$message)
}
[AdapterFakeCertificateKeyNative]::ExpectedCertificate = $null

# Only the IND-CERT-06 certificate may reach the key boundary, and only its provider-info read: no key binding,
# CSP open or native compile. The first record is the readiness probe of the Add-Type refusal.
$keyCalls = @([AdapterFakeCertificateKeyNative]::Calls)
$unexpectedKeyCalls = @($keyCalls | Select-Object -Skip 1 | Where-Object {
        $_ -cne ('ReadProviderParameters:' + $keyCaseThumbprint + ':True')
    })
$compiledKeyNative = @([AppDomain]::CurrentDomain.GetAssemblies() |
        ForEach-Object { $_.GetType('Dawnholder.WindowsSql.CertificateKeyNative', $false) } |
        Where-Object { $null -ne $_ })
$firstKeyCall = if ($keyCalls.Count -ge 1) { $keyCalls[0] } else { '' }
$keyBoundaryClean = $firstKeyCall -ceq 'Add-Type' -and $unexpectedKeyCalls.Count -eq 0 -and $compiledKeyNative.Count -eq 0
Assert-True -Name 'IND-CERT-KEY-01 key boundary saw only the IND-CERT-06 provider read; no binding, CSP or compile' `
    -Condition $keyBoundaryClean -Detail ($keyCalls -join ',')
Add-TestResult -Name ('IND-CERT-KEY-02 provider-info reads by the certificate gate: ' + ($keyCalls.Count - 1)) `
    -Outcome OBSERVED

# =====================================================================================================
# 6. Real path lease, reparse refusal and private store ACL on this suite's own files.
# =====================================================================================================
$leaseRoot = Join-Path $script:SuiteRoot 'lease'
$leaseParent = Join-Path $leaseRoot 'parent'
$leaseChild = Join-Path $leaseParent 'child'
[void][IO.Directory]::CreateDirectory($leaseChild)
[IO.File]::WriteAllText((Join-Path $leaseChild 'input.txt'), 'lease fixture')
$lease = Open-WindowsSqlDirectoryLease -Paths @((Join-Path $leaseChild 'input.txt'))
try {
    $moveError = Get-CaughtMessage { [IO.Directory]::Move($leaseParent, (Join-Path $leaseRoot 'moved')) }
    Assert-True -Name 'IND-LEASE-01 an ancestor of a leased input cannot be renamed while leased' `
        -Condition ($null -ne $moveError -and [IO.Directory]::Exists($leaseParent))
}
finally { $lease.Dispose() }
$moveAfter = Get-CaughtMessage { [IO.Directory]::Move($leaseParent, (Join-Path $leaseRoot 'moved')) }
Assert-True -Name 'IND-LEASE-02 the lease is released on dispose' -Condition ($null -eq $moveAfter) `
    -Detail ([string]$moveAfter)

$junctionTarget = Join-Path $leaseRoot 'real'
$junctionPath = Join-Path $leaseRoot 'link'
[void][IO.Directory]::CreateDirectory($junctionTarget)
[IO.File]::WriteAllText((Join-Path $junctionTarget 'input.txt'), 'junction fixture')
$null = New-Item -ItemType Junction -Path $junctionPath -Target $junctionTarget
$junctionLease = Get-CaughtMessage {
    $handle = Open-WindowsSqlDirectoryLease -Paths @((Join-Path $junctionPath 'input.txt'))
    $handle.Dispose()
}
Assert-True -Name 'IND-LEASE-03 a junction on the input path is refused by the lease' `
    -Condition ($null -ne $junctionLease -and $junctionLease.Contains('WINDOWS_SQL_PATH_LEASE_FAILED')) `
    -Detail ([string]$junctionLease)
$localPathError = Get-CaughtMessage { Assert-WindowsSqlLocalPath -Path (Join-Path $junctionPath 'input.txt') }
Assert-True -Name 'IND-LEASE-04 local path validation refuses a junction ancestor' `
    -Condition ($localPathError -ceq 'WINDOWS_SQL_PATH_REPARSE') -Detail ([string]$localPathError)
$inputPlan = [pscustomobject]@{ SourceRoot = $leaseRoot; Inputs = @([ordered]@{ Path = 'link/input.txt'
            Sha256 = (Get-FileSha256 -Path (Join-Path $junctionTarget 'input.txt')) }) }
$inputError = Get-CaughtMessage { Assert-WindowsSqlInputs -Plan $inputPlan }
Assert-True -Name 'IND-LEASE-05 approved input hash checks refuse link traversal even with a matching hash' `
    -Condition ($inputError -ceq 'WINDOWS_SQL_INPUT_CHANGED') -Detail ([string]$inputError)

$storeRoot = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + [Guid]::NewGuid().ToString('N'))
New-WindowsSqlPrivateDirectory -Path $storeRoot -Sid $script:ExecutorSid
$existsError = Get-CaughtMessage { New-WindowsSqlPrivateDirectory -Path $storeRoot -Sid $script:ExecutorSid }
Assert-True -Name 'IND-STORE-01 an existing private folder is never adopted' `
    -Condition ($existsError -ceq 'WINDOWS_SQL_PRIVATE_ALREADY_EXISTS') -Detail ([string]$existsError)
$storeAdapter = [pscustomobject]@{
    Plan = [pscustomobject]@{ PrivateRoot = $storeRoot; LifetimeId = ('a' * 32); PlanHash = ('A' * 64) }
    Runtime = [pscustomobject]@{ ExecutorSid = $script:ExecutorSid }
}
& $script:RealWritePrivateJson -Adapter $storeAdapter -Name 'lifecycle.json' -Value ([pscustomobject]@{
        LifetimeId = ('a' * 32); PlanHash = ('A' * 64); Status = 'Applying' })
Set-TestDirectoryRule -Path $storeRoot
$readUnsafe = Get-CaughtMessage { Read-WindowsSqlPrivateJson -Adapter $storeAdapter -Name 'lifecycle.json' }
Assert-True -Name 'IND-STORE-02 a permissive private folder is refused on read' `
    -Condition ($readUnsafe -ceq 'WINDOWS_SQL_PRIVATE_ACL_UNSAFE') -Detail ([string]$readUnsafe)
$pendingFiles = @(Get-ChildItem -LiteralPath $storeRoot -Filter '*.pending' -Force)
Assert-True -Name 'IND-STORE-03 a completed atomic write leaves no pending file' -Condition ($pendingFiles.Count -eq 0)

Complete-TestSuite
