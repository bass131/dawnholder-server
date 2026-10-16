[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    # Version-check card (Markdown); its first powershell block holds the probe and the wrapper statements.
    [Parameter(Mandatory)][string]$CardPath,
    # Optional absolute windows-sql folder of an isolated same-shaped tree, as in WindowsSqlVersion.Tests.ps1.
    [string]$ProductRoot = ''
)
# Independent verification of the engine-version source of the Windows SQL Express tool and its version-check card,
# for boundaries that the requirement suite WindowsSqlVersion.Tests.ps1 leaves open:
# - The approved value, Setup/PatchLevel and the SQL ProductVersion carry the same non-canonical text. Exact equality
#   and plan validation let it through, so only the PatchLevel format check can refuse it.
# - Setup/Version neither blocks nor stands in, whatever its presence or type.
# - The card wrapper's own composition statements carry the four inputs and the probe unchanged within the Windows
#   command-line limit. Its launch statement runs only against a child executable that does not exist.
# Scenario children reuse the guarded runner of WindowsSqlVersion.Tests.ps1 (-ScenarioPath). That runner binds the
# registry, SQL connection and Environment type names to stand-ins, proves every loaded type reference resolved to
# them, stubs forbidden commands and appends one JSON line per observation. This suite depends on its scenario fields
# (Kind, Target, ProductRoot, ExpectedProductVersion, CardPath, OutputDirectory, Registry, Sql, LogPath) and log kinds
# (Guard, GuardLoaded, Forbidden, RegistryBase, RegistryOpen, RegistryRead, SqlConnection, SqlStatement,
# AdapterOperation, NativeError, Result, Error, HarnessError). Expected outcomes are requirement constants.
# Nothing here opens SQL, the registry, a certificate store, a service, the firewall or WSL.

. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-version-verification'

# The -ProductRoot parameter is this same script-scope variable; empty means the checked-out windows-sql folder.
if (-not $ProductRoot) { $ProductRoot = Join-Path $script:ToolRoot 'windows-sql' }
elseif (-not [IO.Path]::IsPathRooted($ProductRoot)) { throw 'ProductRoot must be an explicit absolute path.' }
$script:ProductRoot = [IO.Path]::GetFullPath($ProductRoot)
if (-not [IO.Path]::IsPathRooted($CardPath)) { throw 'CardPath must be an explicit absolute path.' }
$script:CardPath = [IO.Path]::GetFullPath($CardPath)
$script:RunnerPath = Join-Path $PSScriptRoot 'WindowsSqlVersion.Tests.ps1'
$requiredFiles = @(
    (Join-Path $script:ProductRoot 'WindowsSql.Instance.ps1'),
    $script:CardPath,
    $script:RunnerPath
)
foreach ($requiredFile in $requiredFiles) {
    if (-not [IO.File]::Exists($requiredFile)) { throw ('Required file does not exist: ' + $requiredFile) }
}

# Fixture identity fixed by the scenario runner; it never names this host or its SQL instance.
$script:FixtureMachine = 'LAPTOP-FIXTURE'
$script:FixtureInstance = 'DHFIXTURE'
$script:FixtureInstanceId = 'MSSQL17.DHFIXTURE'
$script:InstanceMapPath = 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
$script:SetupPath = 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.DHFIXTURE\Setup'
if ([Environment]::MachineName -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run.'
}

# Requirement values: a patched instance keeps its install version in Setup/Version.
$script:InstallVersion = '17.0.1000.7'
$script:PatchedVersion = '17.0.1145.4'
# The same digits written with FULLWIDTH DIGIT ONE and SEVEN; only ASCII digits form an engine version.
$script:FullWidthVersion = [string][char]0xFF11 + [char]0xFF17 + '.0.1145.4'
# Windows CreateProcess accepts at most 32767 characters including the terminating null.
$script:CommandLineLimit = 32766
$script:Utf8 = [Text.UTF8Encoding]::new($false)

function Get-VerifySha256 {
    param([string]$Path)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($Path)))
}

# ---- Scenario facts and runner ----------------------------------------------------------------------------------
function New-VerifyRegistry {
    param($PatchLevel, $SetupVersion, [string]$SetupVersionKind, [switch]$WithoutSetupKey)
    # A null value is absent. The runner stores DWord values as integers and every other kind as text.
    $setup = [ordered]@{}
    if ($null -ne $SetupVersion) { $setup.Version = @{ Kind = $SetupVersionKind; Value = $SetupVersion } }
    if ($null -ne $PatchLevel) { $setup.PatchLevel = @{ Kind = 'String'; Value = $PatchLevel } }
    $map = [ordered]@{}
    $map[$script:FixtureInstance] = @{ Kind = 'String'; Value = $script:FixtureInstanceId }
    $registry = [ordered]@{}
    $registry[$script:InstanceMapPath] = $map
    if (-not $WithoutSetupKey) { $registry[$script:SetupPath] = $setup }
    return $registry
}

function New-VerifyCase {
    param(
        [string]$Id,
        [string]$Title,
        [ValidateSet('Pass', 'Block')][string]$Expect,
        [ValidateSet('Native', 'Preflight', 'Card')][string]$Target,
        $PatchLevel,
        [string]$SqlVersion,
        [string]$Approved = '',
        $SetupVersion = $script:InstallVersion,
        [string]$SetupVersionKind = 'String',
        [switch]$WithoutSetupKey,
        [ValidateSet('', 'Format', 'Sql', 'SetupKey')][string]$Cause = '',
        [string]$Control = ''
    )
    $registryArguments = @{
        PatchLevel = $PatchLevel
        SetupVersion = $SetupVersion
        SetupVersionKind = $SetupVersionKind
        WithoutSetupKey = $WithoutSetupKey
    }
    return [pscustomobject]@{
        Id = $Id
        Title = $Title
        Expect = $Expect
        Target = $Target
        Approved = $Approved
        PatchLevel = $PatchLevel
        SetupVersion = $SetupVersion
        Cause = $Cause
        Control = $Control
        Registry = New-VerifyRegistry @registryArguments
        Sql = [ordered]@{
            NetTransport = 'Shared memory'
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            ProductVersion = $SqlVersion
            IsSysadmin = 1
            OpenFails = $false
        }
    }
}

function Invoke-VerifyScenario {
    param([string]$Id, [Collections.IDictionary]$Scenario)
    $directory = Join-Path $script:SuiteRoot ('scenarios\' + $Id)
    [void][IO.Directory]::CreateDirectory($directory)
    $Scenario['LogPath'] = Join-Path $directory 'observations.jsonl'
    $scenarioPath = Join-Path $directory 'scenario.json'
    [IO.File]::WriteAllText($scenarioPath, (ConvertTo-Json -InputObject $Scenario -Depth 10) + "`n", $script:Utf8)
    $startedUtc = [DateTime]::UtcNow
    $process = Invoke-PowerShellFile -File $script:RunnerPath -Arguments @('-ScenarioPath', $scenarioPath)
    $finishedUtc = [DateTime]::UtcNow
    [IO.File]::WriteAllText((Join-Path $directory 'stdout.txt'), $process.StdOut, $script:Utf8)
    [IO.File]::WriteAllText((Join-Path $directory 'stderr.txt'), $process.StdErr, $script:Utf8)
    [IO.File]::WriteAllText((Join-Path $directory 'exit.txt'), [string]$process.ExitCode + "`n", $script:Utf8)
    $entries = @()
    if ([IO.File]::Exists($Scenario['LogPath'])) {
        $entries = @([IO.File]::ReadAllLines($Scenario['LogPath']) | Where-Object { $_ } | ForEach-Object {
                ConvertFrom-Json -InputObject $_ -DateKind String })
    }
    return [pscustomobject]@{
        Exit = $process.ExitCode
        StdErr = $process.StdErr
        Entries = $entries
        StartedUtc = $startedUtc
        FinishedUtc = $finishedUtc
    }
}

function Get-VerifyEntries {
    param($Run, [string]$Kind)
    return @($Run.Entries | Where-Object { $_.Kind -ceq $Kind })
}

function Assert-VerifyStandInBoundary {
    param([string]$Id, $Run, [string[]]$GuardKinds, [int[]]$AllowedExit)
    $guardsSeen = @($GuardKinds | Where-Object { (Get-VerifyEntries -Run $Run -Kind $_).Count -gt 0 })
    $guardFailures = @($GuardKinds | ForEach-Object { Get-VerifyEntries -Run $Run -Kind $_ } |
            Where-Object { -not $_.Passed }).Count
    $forbidden = @(Get-VerifyEntries -Run $Run -Kind 'Forbidden' | ForEach-Object { [string]$_.Name })
    $registryWrites = @(Get-VerifyEntries -Run $Run -Kind 'RegistryOpen' | Where-Object { $_.Writable }).Count
    $otherViews = @(Get-VerifyEntries -Run $Run -Kind 'RegistryBase' | Where-Object {
            $_.Hive -cne 'LocalMachine' -or $_.View -cne 'Registry64' }).Count
    # A statement the stand-in server cannot evaluate would let a refusal pass for the wrong reason.
    $offScope = @(Get-VerifyEntries -Run $Run -Kind 'SqlStatement' | Where-Object {
            $_.Outcome -cin @('TableRead', 'Unsupported') }).Count
    $harnessErrors = @(Get-VerifyEntries -Run $Run -Kind 'HarnessError' | ForEach-Object { [string]$_.Message })
    $held = $guardsSeen.Count -eq $GuardKinds.Count -and $guardFailures -eq 0 -and $forbidden.Count -eq 0 -and
    $registryWrites -eq 0 -and $otherViews -eq 0 -and $offScope -eq 0 -and $harnessErrors.Count -eq 0 -and
    $Run.Exit -in $AllowedExit
    $detail = ('guards={0}/{1} guardFailures={2} forbidden={3} registryWrites={4} otherViews={5} ' +
        'offScopeSql={6} harness={7} exit={8} stderr={9}') -f $guardsSeen.Count, $GuardKinds.Count, $guardFailures,
    ($forbidden -join ','), $registryWrites, $otherViews, $offScope, ($harnessErrors -join ','), $Run.Exit,
    ([string]$Run.StdErr).Trim()
    Assert-True -Name ($Id + ' protected I/O stayed inside the stand-ins') -Condition $held -Detail $detail
}

function Test-VerifyPatchLevelRead {
    param($Run)
    $reads = @(Get-VerifyEntries -Run $Run -Kind 'RegistryRead' | Where-Object {
            [string]$_.Name -ieq 'PatchLevel' -and [string]$_.Path -ieq $script:SetupPath })
    return $reads.Count -gt 0
}

function Get-VerifyVersionComparisons {
    param($Run)
    return @(Get-VerifyEntries -Run $Run -Kind 'SqlStatement' | ForEach-Object { @($_.Comparisons) } |
            Where-Object { [string]$_.Key -ceq 'SERVERPROPERTY:productversion' })
}

# ---- Product assertions -----------------------------------------------------------------------------------------
function Assert-VerifyProductAccepted {
    param($Case, $Run)
    $results = @(Get-VerifyEntries -Run $Run -Kind 'Result')
    $errors = @(Get-VerifyEntries -Run $Run -Kind 'Error' | ForEach-Object { [string]$_.Message })
    $accepted = $results.Count -eq 1 -and $errors.Count -eq 0
    Assert-True -Name ($Case.Id + ' identity is accepted: ' + $Case.Title) -Condition $accepted `
        -Detail ($errors -join ',')
    $reported = if ($accepted) { [string]$results[0].Instance.ProductVersion } else { '<none>' }
    Assert-Equal -Name ($Case.Id + ' reported engine version is the approved value') -Expected $Case.Approved `
        -Actual $reported
    Assert-True -Name ($Case.Id + ' engine version source is Setup/PatchLevel of the approved InstanceId') `
        -Condition (Test-VerifyPatchLevelRead -Run $Run)
    $operands = @(Get-VerifyVersionComparisons -Run $Run | ForEach-Object { [string]$_.Compared })
    $sameApproved = $operands.Count -gt 0 -and @($operands | Where-Object { $_ -cne $Case.Approved }).Count -eq 0
    Assert-True -Name ($Case.Id + ' SQL ProductVersion is compared with the approved value') `
        -Condition $sameApproved -Detail ($operands -join ',')
    if ($Case.Target -ceq 'Preflight') {
        $operations = @(Get-VerifyEntries -Run $Run -Kind 'AdapterOperation' | ForEach-Object { [string]$_.Operation })
        Assert-True -Name ($Case.Id + ' Preflight continues past the identity') `
            -Condition ($operations -ccontains 'ReadTargetLogin') -Detail ($operations -join ',')
    }
}

function Assert-VerifyProductRefused {
    param($Case, $Run)
    $results = @(Get-VerifyEntries -Run $Run -Kind 'Result')
    $errors = @(Get-VerifyEntries -Run $Run -Kind 'Error' | ForEach-Object { [string]$_.Message })
    $native = @(Get-VerifyEntries -Run $Run -Kind 'NativeError' | ForEach-Object { [string]$_.Message })
    $connections = @(Get-VerifyEntries -Run $Run -Kind 'SqlConnection').Count
    $refused = $results.Count -eq 0 -and $errors.Count -eq 1 -and $errors[0] -cmatch '^WINDOWS_SQL_[A-Z_]+$'
    $detail = if ($results.Count -gt 0) {
        'accepted ProductVersion=' + [string]$results[0].Instance.ProductVersion
    } else { $errors -join ',' }
    $title = $Case.Id + ' is refused with a public code: ' + $Case.Title + ' (control ' + $Case.Control + ')'
    Assert-True -Name $title -Condition $refused -Detail $detail
    switch -CaseSensitive ($Case.Cause) {
        'Format' {
            # Plan validation passed, so the refusal comes at or after the engine-version read.
            Assert-True -Name ($Case.Id + ' refusal follows a read of Setup/PatchLevel') `
                -Condition (Test-VerifyPatchLevelRead -Run $Run)
        }
        'Sql' {
            $rejected = @(Get-VerifyVersionComparisons -Run $Run | Where-Object { $_.Differs })
            Assert-True -Name ($Case.Id + ' the server-side ProductVersion comparison rejected the approved value') `
                -Condition ($rejected.Count -gt 0)
        }
        'SetupKey' {
            Assert-Equal -Name ($Case.Id + ' no SQL connection is built without the Setup key') -Expected '0' `
                -Actual ([string]$connections)
        }
    }
    if ($Case.Target -ceq 'Preflight') {
        $operations = @(Get-VerifyEntries -Run $Run -Kind 'AdapterOperation' | ForEach-Object { [string]$_.Operation })
        Assert-True -Name ($Case.Id + ' Preflight stops at the identity') `
            -Condition ($operations -cnotcontains 'ReadInstanceConfiguration') -Detail ($operations -join ',')
    }
    Add-TestResult -Name ($Case.Id + ' observed codes and SQL connections') -Outcome OBSERVED `
        -Detail ('public={0} native={1} sqlConnections={2}' -f ($errors -join ','), ($native -join ','), $connections)
}

# ---- Card assertions --------------------------------------------------------------------------------------------
function Test-VerifyUtcInRun {
    param($Text, $Run)
    $parsed = [DateTimeOffset]::MinValue
    if ($Text -isnot [string] -or -not [DateTimeOffset]::TryParse($Text,
            [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal,
            [ref]$parsed)) {
        return $false
    }
    $observed = $parsed.UtcDateTime
    return $observed -ge $Run.StartedUtc.AddSeconds(-1) -and $observed -le $Run.FinishedUtc.AddSeconds(1)
}

function Assert-VerifyCardResult {
    param($Case, $Run, [string]$OutputDirectory)
    $files = @([IO.Directory]::GetFiles($OutputDirectory, '*.json'))
    $json = $null
    if ($files.Count -eq 1) {
        $json = [IO.File]::ReadAllText($files[0]) | ConvertFrom-Json -AsHashtable -DateKind String
    }
    Assert-True -Name ($Case.Id + ' this run wrote exactly one JSON') -Condition ($null -ne $json) `
        -Detail ([string]$files.Count)
    if ($null -eq $json) { return }
    $requested = $json.Requested
    $requestedNames = @($requested.Keys | Sort-Object) -join '|'
    $requestedExact = $requestedNames -ceq 'InstanceId|InstanceName|MachineName|OutputDirectory' -and
    $requested.MachineName -ceq $script:FixtureMachine -and $requested.InstanceName -ceq $script:FixtureInstance -and
    $requested.InstanceId -ceq $script:FixtureInstanceId -and $requested.OutputDirectory -ceq $OutputDirectory
    Assert-True -Name ($Case.Id + ' JSON Requested holds exactly the four inputs of this run') `
        -Condition $requestedExact -Detail $requestedNames
    $timesInRun = (Test-VerifyUtcInRun -Text $json.StartedUtc -Run $Run) -and
    (Test-VerifyUtcInRun -Text $json.CompletedUtc -Run $Run)
    Assert-True -Name ($Case.Id + ' JSON StartedUtc/CompletedUtc fall within this run') -Condition $timesInRun `
        -Detail ('{0} {1}' -f $json.StartedUtc, $json.CompletedUtc)
    $recordedPatchLevel = $json.Registry.PatchLevel
    if ($Case.Expect -ceq 'Pass') {
        Assert-Equal -Name ($Case.Id + ' probe exits 0: ' + $Case.Title) -Expected '0' -Actual ([string]$Run.Exit)
        Assert-True -Name ($Case.Id + ' JSON says Status=Matched and ExitCode=0') -Condition (
            [string]$json.Status -ceq 'Matched' -and $json.ExitCode -eq 0) `
            -Detail ('{0}/{1}' -f $json.Status, $json.ExitCode)
        $engineBoth = [string]$recordedPatchLevel -ceq $script:PatchedVersion -and
        [string]$json.Sql.ProductVersion -ceq $script:PatchedVersion
        Assert-True -Name ($Case.Id + ' JSON records PatchLevel and SQL ProductVersion as the engine version') `
            -Condition $engineBoth -Detail ('{0}/{1}' -f $recordedPatchLevel, $json.Sql.ProductVersion)
        Assert-Equal -Name ($Case.Id + ' JSON keeps Setup/Version as observed metadata') `
            -Expected $(if ($null -eq $Case.SetupVersion) { '<null>' } else { [string]$Case.SetupVersion }) `
            -Actual $(if ($null -eq $json.Registry.Version) { '<null>' } else { [string]$json.Registry.Version })
        return
    }
    # Card contract: exit 1 is an unavailable, invalid or mismatched observation; exit 2 is an output failure.
    Assert-Equal -Name ($Case.Id + ' probe exits 1: ' + $Case.Title + ' (control ' + $Case.Control + ')') `
        -Expected '1' -Actual ([string]$Run.Exit)
    Assert-True -Name ($Case.Id + ' JSON records Status=Failed with the same exit') -Condition (
        [string]$json.Status -ceq 'Failed' -and $json.ExitCode -eq $Run.Exit) `
        -Detail ('{0}/{1}' -f $json.Status, $json.ExitCode)
    if ($Case.Cause -ceq 'Format') {
        Assert-True -Name ($Case.Id + ' JSON records the raw PatchLevel without trimming or conversion') `
            -Condition ([string]$recordedPatchLevel -ceq [string]$Case.PatchLevel) `
            -Detail ([string]$recordedPatchLevel)
    }
    Add-TestResult -Name ($Case.Id + ' observed error code and SQL attempt') -Outcome OBSERVED `
        -Detail ('ErrorCode={0} SqlAttempted={1}' -f $json.ErrorCode, $json.Sql.Attempted)
}

# ---- Wrapper composition ----------------------------------------------------------------------------------------
function Get-VerifyCardStatements {
    param([string]$Path)
    $text = [IO.File]::ReadAllText($Path)
    $block = [regex]::Match($text, '(?s)```powershell\r?\n(?<code>.*?)\r?\n```')
    if (-not $block.Success) { return @() }
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseInput($block.Groups['code'].Value, [ref]$null, [ref]$errors)
    if (@($errors).Count -gt 0) { return @() }
    return @($ast.EndBlock.Statements)
}

function Get-VerifyAssignedName {
    param($Statement)
    if ($Statement -isnot [Management.Automation.Language.AssignmentStatementAst] -or
        $Statement.Left -isnot [Management.Automation.Language.VariableExpressionAst]) {
        return $null
    }
    return $Statement.Left.VariablePath.UserPath
}

function Test-VerifyLaunchStatement {
    param($Statement)
    if ($Statement -isnot [Management.Automation.Language.PipelineAst]) { return $false }
    $command = $Statement.PipelineElements[0]
    return $command -is [Management.Automation.Language.CommandAst] -and
    $command.InvocationOperator -eq [Management.Automation.Language.TokenKind]::Ampersand -and
    $command.CommandElements[0] -is [Management.Automation.Language.VariableExpressionAst] -and
    $command.CommandElements[0].VariablePath.UserPath -ceq 'childPwsh'
}

function Test-VerifyWrapperScript {
    param([string]$Text, [string]$MissingPwsh)
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseInput($Text, [ref]$null, [ref]$errors)
    # Nested script blocks are not searched: the probe literal is only defined here, never invoked.
    $commands = @($ast.FindAll({
                param($node)
                $node -is [Management.Automation.Language.CommandAst]
            }, $false))
    $launches = @($commands | Where-Object { Test-VerifyLaunchStatement -Statement $_.Parent })
    $ampersand = [Management.Automation.Language.TokenKind]::Ampersand
    $named = @($commands | Where-Object { $_.InvocationOperator -ne $ampersand })
    $unexpected = @($named | Where-Object {
            $_.GetCommandName() -cnotin @('ConvertTo-Json', 'Join-Path', 'Write-Output') } |
            ForEach-Object { $_.Extent.Text })
    $childAssignments = @($ast.FindAll({
                param($node)
                $node -is [Management.Automation.Language.AssignmentStatementAst] -and
                $node.Left -is [Management.Automation.Language.VariableExpressionAst] -and
                $node.Left.VariablePath.UserPath -ceq 'childPwsh'
            }, $false))
    $missingTarget = $childAssignments.Count -eq 1 -and
    $childAssignments[0].Right.Expression -is [Management.Automation.Language.StringConstantExpressionAst] -and
    $childAssignments[0].Right.Expression.Value -ceq $MissingPwsh
    $passed = @($errors).Count -eq 0 -and $launches.Count -eq 1 -and $commands.Count -eq $named.Count + 1 -and
    $unexpected.Count -eq 0 -and $missingTarget -and -not [IO.File]::Exists($MissingPwsh)
    $detail = 'parseErrors={0} launches={1} unexpected={2} missingTarget={3}' -f @($errors).Count,
    $launches.Count, ($unexpected -join ','), $missingTarget
    return [pscustomobject]@{ Passed = $passed; Detail = $detail }
}

# =====================================================================================================
# 1. Run gates: fixture identity, shell, output location and input fingerprints.
# =====================================================================================================
Assert-True -Name 'IND-VER-ENV-01 fixture machine never names this host' `
    -Condition ([Environment]::MachineName -cne $script:FixtureMachine) -Detail ([Environment]::MachineName)
Assert-True -Name 'IND-VER-ENV-02 suite runs under pwsh 7 on Windows' -Condition (
    $IsWindows -and $PSVersionTable.PSVersion.Major -eq 7) -Detail $PSVersionTable.PSVersion.ToString()
# The card refuses an output folder below a junction or symlink, so the suite folder must have none.
$reparseAncestors = @()
for ($directory = [IO.DirectoryInfo]::new($script:SuiteRoot); $null -ne $directory; $directory = $directory.Parent) {
    if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        $reparseAncestors += $directory.FullName
    }
}
Assert-True -Name 'IND-VER-ENV-03 suite folder has no junction/symlink ancestor' `
    -Condition ($reparseAncestors.Count -eq 0) -Detail ($reparseAncestors -join ',')
$fingerprints = [ordered]@{
    Card = $script:CardPath
    Instance = Join-Path $script:ProductRoot 'WindowsSql.Instance.ps1'
    Sql = Join-Path $script:ProductRoot 'WindowsSql.Sql.ps1'
    Runner = $script:RunnerPath
}
$hashesBefore = [ordered]@{}
foreach ($name in $fingerprints.Keys) {
    $hashesBefore[$name] = Get-VerifySha256 -Path $fingerprints[$name]
    Add-TestResult -Name ('IND-VER-ENV-04 SHA256 ' + $name) -Outcome OBSERVED `
        -Detail ($fingerprints[$name] + ' ' + $hashesBefore[$name])
}

# The registry stand-in accepts any GetValue arguments. The real three-argument overload and the conversion of the
# option text are checked by reflection only; no key is opened.
$getValueOverload = [Microsoft.Win32.RegistryKey].GetMethod('GetValue',
    [Type[]]@([string], [object], [Microsoft.Win32.RegistryValueOptions]))
$optionValue = [Management.Automation.LanguagePrimitives]::ConvertTo('DoNotExpandEnvironmentNames',
    [Microsoft.Win32.RegistryValueOptions])
$optionBound = $null -ne $getValueOverload -and
$optionValue -eq [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames
Assert-True -Name 'IND-VER-BIND-01 real RegistryKey.GetValue(name, default, options) accepts the option text' `
    -Condition $optionBound -Detail ([string]$getValueOverload)

# =====================================================================================================
# 2. Product: the real identity read and Preflight core; only registry/SQL/host facts are stood in.
# =====================================================================================================
$install = $script:InstallVersion
$patched = $script:PatchedVersion
$productCases = @(
    (New-VerifyCase -Id 'IND-VER-PRD-01' -Title 'Preflight: approved, PatchLevel and SQL all 17.0.01145.4' `
        -Expect Block -Target Preflight -PatchLevel '17.0.01145.4' -Approved '17.0.01145.4' `
        -SqlVersion '17.0.01145.4' -Cause Format -Control IND-VER-PRD-06),
    (New-VerifyCase -Id 'IND-VER-PRD-02' -Title 'Preflight: approved, PatchLevel and SQL all end with LF' `
        -Expect Block -Target Preflight -PatchLevel ($patched + "`n") -Approved ($patched + "`n") `
        -SqlVersion ($patched + "`n") -Cause Format -Control IND-VER-PRD-06),
    (New-VerifyCase -Id 'IND-VER-PRD-03' -Title 'native: approved, PatchLevel and SQL all start with a space' `
        -Expect Block -Target Native -PatchLevel (' ' + $patched) -Approved (' ' + $patched) `
        -SqlVersion (' ' + $patched) -Cause Format -Control IND-VER-PRD-11),
    (New-VerifyCase -Id 'IND-VER-PRD-04' -Title 'native: approved, PatchLevel and SQL all have five parts' `
        -Expect Block -Target Native -PatchLevel ($patched + '.1') -Approved ($patched + '.1') `
        -SqlVersion ($patched + '.1') -Cause Format -Control IND-VER-PRD-11),
    (New-VerifyCase -Id 'IND-VER-PRD-05' -Title 'native: approved, PatchLevel and SQL all use fullwidth digits' `
        -Expect Block -Target Native -PatchLevel $script:FullWidthVersion -Approved $script:FullWidthVersion `
        -SqlVersion $script:FullWidthVersion -Cause Format -Control IND-VER-PRD-11),
    (New-VerifyCase -Id 'IND-VER-PRD-06' -Title 'Preflight: Setup/Version absent, PatchLevel/approved/SQL patched' `
        -Expect Pass -Target Preflight -PatchLevel $patched -Approved $patched -SqlVersion $patched `
        -SetupVersion $null),
    (New-VerifyCase -Id 'IND-VER-PRD-07' -Title 'Preflight: Setup/Version is REG_DWORD 1' -Expect Pass `
        -Target Preflight -PatchLevel $patched -Approved $patched -SqlVersion $patched -SetupVersion 1 `
        -SetupVersionKind DWord),
    (New-VerifyCase -Id 'IND-VER-PRD-08' -Title 'Preflight: Setup/Version 99.0.0.0 unrelated to the engine' `
        -Expect Pass -Target Preflight -PatchLevel $patched -Approved $patched -SqlVersion $patched `
        -SetupVersion '99.0.0.0'),
    (New-VerifyCase -Id 'IND-VER-PRD-09' -Title 'Preflight: PatchLevel=approved patched, SQL equals Setup/Version' `
        -Expect Block -Target Preflight -PatchLevel $patched -Approved $patched -SqlVersion $install `
        -Cause Sql -Control IND-VER-PRD-06),
    (New-VerifyCase -Id 'IND-VER-PRD-10' -Title 'native: instance map present, Setup key absent' -Expect Block `
        -Target Native -PatchLevel $patched -Approved $patched -SqlVersion $patched -WithoutSetupKey `
        -Cause SetupKey -Control IND-VER-PRD-11),
    (New-VerifyCase -Id 'IND-VER-PRD-11' -Title 'native control: Version 17.0.1000.7, PatchLevel/approved/SQL patched' `
        -Expect Pass -Target Native -PatchLevel $patched -Approved $patched -SqlVersion $patched)
)
foreach ($case in $productCases) {
    $scenario = [ordered]@{
        Kind = 'Product'
        Target = $case.Target
        ProductRoot = $script:ProductRoot
        ExpectedProductVersion = $case.Approved
        Registry = $case.Registry
        Sql = $case.Sql
    }
    $run = Invoke-VerifyScenario -Id $case.Id -Scenario $scenario
    Assert-VerifyStandInBoundary -Id $case.Id -Run $run -GuardKinds @('Guard', 'GuardLoaded') -AllowedExit @(0)
    if ($case.Expect -ceq 'Pass') { Assert-VerifyProductAccepted -Case $case -Run $run }
    else { Assert-VerifyProductRefused -Case $case -Run $run }
}

# =====================================================================================================
# 3. Card: the probe text runs unchanged in the runner with registry/SQL/host facts stood in.
# =====================================================================================================
$cardCases = @(
    (New-VerifyCase -Id 'IND-VER-CARD-01' -Title 'PatchLevel and SQL both 17.0.01145.4' -Expect Block -Target Card `
        -PatchLevel '17.0.01145.4' -SqlVersion '17.0.01145.4' -Cause Format -Control IND-VER-CARD-05),
    (New-VerifyCase -Id 'IND-VER-CARD-02' -Title 'PatchLevel and SQL both end with LF' -Expect Block -Target Card `
        -PatchLevel ($patched + "`n") -SqlVersion ($patched + "`n") -Cause Format -Control IND-VER-CARD-05),
    (New-VerifyCase -Id 'IND-VER-CARD-03' -Title 'PatchLevel and SQL both start with a space' -Expect Block `
        -Target Card -PatchLevel (' ' + $patched) -SqlVersion (' ' + $patched) -Cause Format `
        -Control IND-VER-CARD-05),
    (New-VerifyCase -Id 'IND-VER-CARD-04' -Title 'PatchLevel and SQL both use fullwidth digits' -Expect Block `
        -Target Card -PatchLevel $script:FullWidthVersion -SqlVersion $script:FullWidthVersion -Cause Format `
        -Control IND-VER-CARD-05),
    (New-VerifyCase -Id 'IND-VER-CARD-05' -Title 'Setup/Version absent, PatchLevel/SQL patched' -Expect Pass `
        -Target Card -PatchLevel $patched -SqlVersion $patched -SetupVersion $null),
    (New-VerifyCase -Id 'IND-VER-CARD-06' -Title 'Setup/Version is REG_DWORD 1' -Expect Pass -Target Card `
        -PatchLevel $patched -SqlVersion $patched -SetupVersion 1 -SetupVersionKind DWord),
    (New-VerifyCase -Id 'IND-VER-CARD-07' -Title 'Setup key absent' -Expect Block -Target Card `
        -PatchLevel $patched -SqlVersion $patched -WithoutSetupKey -Cause SetupKey -Control IND-VER-CARD-05),
    (New-VerifyCase -Id 'IND-VER-CARD-08' -Title 'SQL ProductVersion empty while PatchLevel is patched' `
        -Expect Block -Target Card -PatchLevel $patched -SqlVersion '' -Control IND-VER-CARD-05)
)
foreach ($case in $cardCases) {
    $output = Join-Path $script:SuiteRoot ('card-output\' + $case.Id)
    [void][IO.Directory]::CreateDirectory($output)
    $scenario = [ordered]@{
        Kind = 'Card'
        CardPath = $script:CardPath
        OutputDirectory = $output
        Registry = $case.Registry
        Sql = $case.Sql
    }
    $run = Invoke-VerifyScenario -Id $case.Id -Scenario $scenario
    Assert-VerifyStandInBoundary -Id $case.Id -Run $run -GuardKinds @('Guard') -AllowedExit @(0, 1, 2)
    Assert-VerifyCardResult -Case $case -Run $run -OutputDirectory $output
}

# =====================================================================================================
# 4. Wrapper: the card's own composition statements run; the launch only meets a missing child executable.
# =====================================================================================================
$statements = @(Get-VerifyCardStatements -Path $script:CardPath)
$inputNames = @('approvedMachineName', 'approvedInstanceName', 'approvedInstanceId', 'approvedOutputDirectory')
$leading = @($statements | Select-Object -First 4 | ForEach-Object { Get-VerifyAssignedName -Statement $_ })
$probeIndex = -1
$launchIndex = -1
for ($index = 0; $index -lt $statements.Count; $index++) {
    if ((Get-VerifyAssignedName -Statement $statements[$index]) -ceq 'versionProbe') { $probeIndex = $index }
    if (Test-VerifyLaunchStatement -Statement $statements[$index]) { $launchIndex = $index }
}
$shapeKnown = ($leading -join '|') -ceq ($inputNames -join '|') -and $probeIndex -eq 4 -and $launchIndex -gt $probeIndex
Assert-True -Name 'IND-VER-WRAP-01 card block: four input assignments, the probe literal, then one child launch' `
    -Condition $shapeKnown `
    -Detail ('inputs={0} probe={1} launch={2}' -f ($leading -join ','), $probeIndex, $launchIndex)

$wrapperRoot = Join-Path $script:SuiteRoot 'wrapper'
$wrapperOutput = Join-Path $wrapperRoot 'output'
[void][IO.Directory]::CreateDirectory($wrapperOutput)
$missingPwsh = Join-Path $wrapperRoot 'absent-shell\pwsh.exe'
# Quote, subexpression, ampersand and double quote must stay data; the length approaches a long Windows path.
$trickyOutput = "C:\Evidence\it's `$(Write-Output injected) & `"x`"\" + ('d' * 200)
$wrapperInputs = [ordered]@{
    approvedMachineName = $script:FixtureMachine
    approvedInstanceName = $script:FixtureInstance
    approvedInstanceId = $script:FixtureInstanceId
    approvedOutputDirectory = $trickyOutput
}
if ($shapeKnown) {
    $lines = [Collections.Generic.List[string]]::new()
    # The operator replaces only the four input strings; every other statement keeps the card text.
    foreach ($name in $inputNames) {
        $lines.Add('$' + $name + " = '" + $wrapperInputs[$name].Replace("'", "''") + "'")
    }
    for ($index = $probeIndex; $index -lt $launchIndex; $index++) {
        if ((Get-VerifyAssignedName -Statement $statements[$index]) -cne 'childPwsh') {
            $lines.Add($statements[$index].Extent.Text)
        }
    }
    $lines.Add("`$childPwsh = '" + $missingPwsh.Replace("'", "''") + "'")
    $lines.Add("[Console]::WriteLine('VERIFY_ENCODED_COMMAND=' + `$encodedCommand)")
    $lines.Add("[Console]::WriteLine('VERIFY_CARD_PWSH=' + (Join-Path `$PSHOME 'pwsh.exe'))")
    # A value left by an earlier native command in the operator's session.
    $lines.Add('$global:LASTEXITCODE = 0')
    for ($index = $launchIndex; $index -lt $statements.Count; $index++) { $lines.Add($statements[$index].Extent.Text) }
    $wrapperText = ($lines -join "`n") + "`n"
    $wrapperScript = Join-Path $wrapperRoot 'wrapper-child.ps1'
    [IO.File]::WriteAllText($wrapperScript, $wrapperText, $script:Utf8)
    $wrapperGuard = Test-VerifyWrapperScript -Text $wrapperText -MissingPwsh $missingPwsh
    Assert-True -Name 'IND-VER-WRAP-02 composed script runs card data statements and one launch of a missing file' `
        -Condition $wrapperGuard.Passed -Detail $wrapperGuard.Detail
}
if ($shapeKnown -and $wrapperGuard.Passed) {
    $wrapper = Invoke-PowerShellFile -File $wrapperScript
    [IO.File]::WriteAllText((Join-Path $wrapperRoot 'stdout.txt'), $wrapper.StdOut, $script:Utf8)
    [IO.File]::WriteAllText((Join-Path $wrapperRoot 'stderr.txt'), $wrapper.StdErr, $script:Utf8)
    [IO.File]::WriteAllText((Join-Path $wrapperRoot 'exit.txt'), [string]$wrapper.ExitCode + "`n", $script:Utf8)
    $outLines = @($wrapper.StdOut -split "`r?`n")
    $encodedLines = @($outLines | Where-Object { $_.StartsWith('VERIFY_ENCODED_COMMAND=') })
    $pwshLines = @($outLines | Where-Object { $_.StartsWith('VERIFY_CARD_PWSH=') })
    $exitLines = @($outLines | Where-Object { $_.StartsWith('PR2_VERSION_CHECK_EXIT=') })
    Assert-True -Name 'IND-VER-WRAP-03 composition produced one encoded child command' `
        -Condition ($encodedLines.Count -eq 1 -and $pwshLines.Count -eq 1) `
        -Detail ('encoded={0} pwsh={1} stderr={2}' -f $encodedLines.Count, $pwshLines.Count, $wrapper.StdErr.Trim())
}
if ($shapeKnown -and $wrapperGuard.Passed -and $encodedLines.Count -eq 1 -and $pwshLines.Count -eq 1) {
    $encoded = $encodedLines[0].Substring('VERIFY_ENCODED_COMMAND='.Length)
    $decoded = [Text.Encoding]::Unicode.GetString([Convert]::FromBase64String($encoded))
    [IO.File]::WriteAllText((Join-Path $wrapperRoot 'decoded-child-command.ps1'), $decoded, $script:Utf8)
    $decodedErrors = $null
    $decodedAst = [Management.Automation.Language.Parser]::ParseInput($decoded, [ref]$null, [ref]$decodedErrors)
    $decodedBlocks = @($decodedAst.FindAll({
                param($node)
                $node -is [Management.Automation.Language.ScriptBlockExpressionAst]
            }, $false))
    $cardProbeText = $statements[$probeIndex].Right.Expression.Extent.Text
    $sameProbe = @($decodedErrors).Count -eq 0 -and $decodedBlocks.Count -eq 1 -and
    $decodedBlocks[0].Extent.Text -ceq $cardProbeText
    Assert-True -Name 'IND-VER-WRAP-04 decoded child command parses and carries the card probe text unchanged' `
        -Condition $sameProbe -Detail ('parseErrors={0} blocks={1}' -f @($decodedErrors).Count, $decodedBlocks.Count)
    $payload = [regex]::Match($decoded, "FromBase64String\('(?<data>[A-Za-z0-9+/=]+)'\)")
    $received = $null
    if ($payload.Success) {
        $receivedText = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload.Groups['data'].Value))
        $received = $receivedText | ConvertFrom-Json -AsHashtable
    }
    $receivedNames = if ($null -ne $received) { @($received.Keys | Sort-Object) -join '|' } else { '<none>' }
    $sameInputs = $receivedNames -ceq 'InstanceId|InstanceName|MachineName|OutputDirectory' -and
    $received.MachineName -ceq $script:FixtureMachine -and $received.InstanceName -ceq $script:FixtureInstance -and
    $received.InstanceId -ceq $script:FixtureInstanceId -and $received.OutputDirectory -ceq $trickyOutput
    Assert-True -Name 'IND-VER-WRAP-05 child receives exactly the four inputs, quotes and $() kept as data' `
        -Condition $sameInputs -Detail $receivedNames
    Assert-True -Name 'IND-VER-WRAP-06 no input text appears as code in the child command' `
        -Condition (-not $decoded.Contains('injected')) -Detail ([string]$decoded.Length)
    $cardPwsh = $pwshLines[0].Substring('VERIFY_CARD_PWSH='.Length)
    $commandLine = '"' + $cardPwsh + '" -NoLogo -NoProfile -NonInteractive -EncodedCommand ' + $encoded
    $margin = $script:CommandLineLimit - $commandLine.Length
    Assert-True -Name 'IND-VER-WRAP-07 child command line fits the Windows command-line limit' `
        -Condition ($margin -ge 0) `
        -Detail ('length={0} limit={1} margin={2}' -f $commandLine.Length, $script:CommandLineLimit, $margin)
    Assert-Equal -Name 'IND-VER-WRAP-08 a launch that never starts writes no JSON' -Expected '0' `
        -Actual ([string]@([IO.Directory]::GetFiles($wrapperOutput)).Count)
    # Documented card limit: after a start failure the EXIT line shows whatever $LASTEXITCODE held before.
    Add-TestResult -Name 'IND-VER-WRAP-09 EXIT line and parent exit after a child start failure' -Outcome OBSERVED `
        -Detail ('{0} parentExit={1}' -f ($exitLines -join ','), $wrapper.ExitCode)
}

Assert-Equal -Name 'IND-VER-ENV-90 card, product and runner bytes are unchanged by the suite' `
    -Expected (@($hashesBefore.Values) -join '|') `
    -Actual (@($fingerprints.Keys | ForEach-Object { Get-VerifySha256 -Path $fingerprints[$_] }) -join '|')
Complete-TestSuite
