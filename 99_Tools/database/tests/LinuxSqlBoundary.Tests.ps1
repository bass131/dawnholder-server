[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the Linux SQL tool boundaries that live runs cannot force:
# - Invoke-LinuxSqlProcess keeps child stdout/stderr text exactly and journals stdin presence, never its content.
# - Read-LinuxSqlPlan reads the approval expiry instant from its original offset text.
# - Invoke-EnvironmentDocker redacts a non-empty SA value (and generated secret shapes) before any stderr record.
# Children are this PowerShell executable only. Nothing opens SQL, Docker or WSL; secrets below are fixture shapes.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'linux-sql-boundary'
. (Join-Path $script:ToolRoot 'test-environment/LinuxSql.Common.ps1')
Set-OfflineStubs

$fixtureRoot = $script:SuiteRoot
$shell = [Diagnostics.Process]::GetCurrentProcess().Path

function Invoke-ChildText {
    # The child writes exactly the given strings; expected values are the literals passed here.
    param([string]$Stdout, [string]$Stderr, [int]$ExitCode = 0)
    $encode = { param($text) [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($text)) }
    $command = (
        '$o = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(''{0}''));' +
        '$e = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(''{1}''));' +
        '[Console]::Out.Write($o); [Console]::Out.Flush(); [Console]::Error.Write($e); [Console]::Error.Flush(); exit {2}'
    ) -f (& $encode $Stdout), (& $encode $Stderr), $ExitCode
    return Invoke-LinuxSqlProcess -FileName $shell -Arguments @('-NoProfile', '-NonInteractive', '-Command', $command) -Seconds 60
}

# ---- Child output text is preserved byte-for-byte (v2 output buffer change, msg_41f0d0ce6ae7 counterexamples).
$cases = @(
    @{ Name = 'quotes and repeated spaces'; Stdout = 'a "b"  c   d'; Stderr = "x  'y'  z" },
    @{ Name = 'multiple lines with indentation'; Stdout = "line1`nline2`n    indented`n"; Stderr = "e1`n`ne3" },
    @{ Name = 'empty stdout and stderr'; Stdout = ''; Stderr = '' },
    # Longer than one 2048-char read: a reused buffer or enumerated char[] would drop or space-pad characters.
    @{ Name = 'char runs across several 2048-char reads'; Stdout = ('0123456789ABCDEF' * 400); Stderr = ('xy' * 1500) }
)
foreach ($case in $cases) {
    $result = Invoke-ChildText -Stdout $case.Stdout -Stderr $case.Stderr -ExitCode 0
    $stdoutExact = $result.Output -ceq $case.Stdout
    $stderrExact = $result.Error -ceq $case.Stderr
    Assert-True -Name ('child stdout preserved exactly: ' + $case.Name) -Condition $stdoutExact `
        -Detail ('expected {0} chars, actual {1} chars' -f $case.Stdout.Length, $result.Output.Length)
    Assert-True -Name ('child stderr preserved exactly: ' + $case.Name) -Condition $stderrExact `
        -Detail ('expected {0} chars, actual {1} chars' -f $case.Stderr.Length, $result.Error.Length)
}
$exitResult = Invoke-ChildText -Stdout 'done' -Stderr '' -ExitCode 7
Assert-Equal -Name 'child exit code is returned unchanged' -Expected 7 -Actual $exitResult.ExitCode

# ---- stdin: omitted/null means EOF only, '' means one empty line, other types are rejected; journal has presence only.
$journalRoot = Join-Path $fixtureRoot 'journal'
[void][IO.Directory]::CreateDirectory($journalRoot)
$script:LinuxSqlEvidenceRoot = $journalRoot
$lengthCommand = '$t = [Console]::In.ReadToEnd(); [Console]::Out.Write($t.Length)'
$stdinCases = @(
    @{ Name = 'omitted'; Splat = @{}; ExpectedLength = '0'; ExpectedPresent = $false },
    @{ Name = 'explicit null'; Splat = @{ InputText = $null }; ExpectedLength = '0'; ExpectedPresent = $false },
    # WriteLineAsync appends Environment.NewLine (CRLF on Windows) after the value.
    @{ Name = 'empty string'; Splat = @{ InputText = '' }; ExpectedLength = '2'; ExpectedPresent = $true },
    @{ Name = 'value'; Splat = @{ InputText = 'stdin-sentinel-4711' }; ExpectedLength = '21'; ExpectedPresent = $true }
)
foreach ($case in $stdinCases) {
    $before = @(Get-ChildItem -LiteralPath $journalRoot -File | ForEach-Object FullName)
    $splat = $case.Splat
    $result = Invoke-LinuxSqlProcess -FileName $shell -Arguments @('-NoProfile', '-NonInteractive', '-Command', $lengthCommand) `
        -Seconds 60 @splat
    $journals = @(Get-ChildItem -LiteralPath $journalRoot -File | Where-Object FullName -notin $before)
    $journalText = if ($journals.Count -eq 1) { [IO.File]::ReadAllText($journals[0].FullName) } else { '{}' }
    $journal = $journalText | ConvertFrom-Json
    Assert-Equal -Name ('stdin ' + $case.Name + ': child read length') -Expected $case.ExpectedLength -Actual $result.Output
    Assert-Equal -Name ('stdin ' + $case.Name + ': one new journal') -Expected 1 -Actual $journals.Count
    Assert-Equal -Name ('stdin ' + $case.Name + ': journal StdinPresent') -Expected $case.ExpectedPresent -Actual $journal.StdinPresent
    Assert-True -Name ('stdin ' + $case.Name + ': journal never contains stdin text') `
        -Condition (-not $journalText.Contains('stdin-sentinel-4711'))
}
Assert-Throws -Name 'stdin of a non-string type is rejected before a child starts' -Pattern '^LINUX_SQL_STDIN_TYPE_INVALID$' `
    -Action { Invoke-LinuxSqlProcess -FileName $shell -Arguments @('-NoProfile', '-Command', 'exit 0') -InputText 42 }
Remove-Variable -Name LinuxSqlEvidenceRoot -Scope Script

# ---- Plan expiry is the original offset instant (static fix #2), with the 24-hour cap even while closing.
$sourceRoot = Join-Path $fixtureRoot 'source'
$evidenceRoot = Join-Path $sourceRoot '.backups\evidence'
[void][IO.Directory]::CreateDirectory($evidenceRoot)
$inputs = foreach ($index in 1..25) {
    $relative = 'inputs/file{0:D2}.txt' -f $index
    $path = Join-Path $sourceRoot $relative
    [void][IO.Directory]::CreateDirectory((Split-Path $path))
    [IO.File]::WriteAllText($path, "fixture $index")
    [ordered]@{ Path = $relative; Sha256 = (Get-FileHash -LiteralPath $path).Hash }
}
# Never created: the plan only names a private directory below the approved parent.
$privateRoot = Join-Path $env:LOCALAPPDATA 'Dawnholder\linux-sql-boundary-fixture-unused'

function New-PlanFile {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$ExpiresText)
    $plan = [ordered]@{
        Version = 1; ExecutionApproved = $true; ExpiresUtc = $ExpiresText
        ExecutorSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value; Machine = [Environment]::MachineName
        LifetimeId = ('a' * 32); SourceRoot = $sourceRoot; EvidenceRoot = $evidenceRoot; PrivateRoot = $privateRoot
        ContainerName = 'fixture-sql'; ContainerHostname = 'fixture-sql'; VolumeName = 'fixture-sql-data'
        Database = 'Dawnholder_Test_Fixture'; RuntimeLogin = 'dh_fixture'; Port = 14399; Endpoint = '127.0.0.1,14399'
        Image = 'mcr.microsoft.com/mssql/server@sha256:' + ('b' * 64)
        Encrypt = 'Mandatory'; TrustServerCertificate = $false; SaContainerEnvironmentException = $true
        RemoveNewResourcesAfterAudit = $true; SqlMemoryMb = 3072; ContainerMemoryMb = 4096
        MinPhysicalBytes = 6GB; MinCommitBytes = 4GB; MinDiskBytes = 10GB; MaxEditors = 1
        ConnectSeconds = 5; CommandSeconds = 30; HostSeconds = 180; ReadyAttempts = 12; StopSeconds = 30
        SlotId = 1; AccountId = [guid]::NewGuid().ToString(); CharacterId = [guid]::NewGuid().ToString()
        DotnetPath = '/home/fixture/dotnet'; DotnetVersion = '10.0.301'; ImageDefaultUser = 'mssql'
        LinuxBuildRoot = '/home/fixture/build'; LinuxHostDll = '/home/fixture/build/publish/LinuxSqlHost.dll'
        LinuxHostSha256 = ('C' * 64); Inputs = @($inputs)
        ProtectedResources = @(
            @{ Container = 'p1'; Volume = 'p1-data'; PrivateRoot = $null },
            @{ Container = 'p2'; Volume = 'p2-data'; PrivateRoot = $null },
            @{ Container = 'p3'; Volume = 'p3-data'; PrivateRoot = $null })
    }
    $path = Join-Path $fixtureRoot ($Name + '.json')
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($plan | ConvertTo-Json -Depth 6))
    [IO.File]::WriteAllBytes($path, $bytes)
    return [pscustomobject]@{ Path = $path; Hash = (Get-FileHash -LiteralPath $path).Hash }
}

$now = [DateTimeOffset]::UtcNow
$inTwoHours = $now.AddHours(2)
$planCases = @(
    @{ Name = 'Z offset two hours ahead'; Text = $inTwoHours.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"); Closing = $false; Accept = $true },
    @{ Name = '+09:00 offset of the same instant'; Text = $inTwoHours.ToOffset([TimeSpan]::FromHours(9)).ToString("yyyy-MM-dd'T'HH:mm:sszzz");
        Closing = $false; Accept = $true },
    # Read as local time this would be 9 hours earlier on a KST machine; the original offset says it is in the future.
    @{ Name = '-07:00 offset two hours ahead'; Text = $inTwoHours.ToOffset([TimeSpan]::FromHours(-7)).ToString("yyyy-MM-dd'T'HH:mm:sszzz");
        Closing = $false; Accept = $true },
    @{ Name = 'two minutes past, open step'; Text = $now.AddMinutes(-2).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"); Closing = $false; Accept = $false },
    @{ Name = 'two minutes past, closing step'; Text = $now.AddMinutes(-2).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"); Closing = $true; Accept = $true },
    @{ Name = '25 hours ahead even while closing'; Text = $now.AddHours(25).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"); Closing = $true; Accept = $false }
)
$index = 0
foreach ($case in $planCases) {
    $index++
    $file = New-PlanFile -Name ('plan-expiry-{0}' -f $index) -ExpiresText $case.Text
    $message = Get-ThrownMessage -Action { Read-LinuxSqlPlan -Path $file.Path -ExpectedHash $file.Hash -Closing:$case.Closing }
    $accepted = $null -eq $message
    $rejectedForExpiry = $message -ceq 'LINUX_SQL_EXECUTOR_OR_APPROVAL_EXPIRED'
    Assert-True -Name ('plan expiry ' + $case.Name + ': ' + $(if ($case.Accept) { 'accepted' } else { 'rejected as expired' })) `
        -Condition $(if ($case.Accept) { $accepted } else { $rejectedForExpiry }) -Detail ('{0} -> {1}' -f $case.Text, [string]$message)
}
$noOffset = New-PlanFile -Name 'plan-expiry-no-offset' -ExpiresText $inTwoHours.ToString("yyyy-MM-dd'T'HH:mm:ss")
Assert-Throws -Name 'plan expiry without Z or offset is invalid' -Pattern '^LINUX_SQL_PLAN_EXPIRY_INVALID$' `
    -Action { Read-LinuxSqlPlan -Path $noOffset.Path -ExpectedHash $noOffset.Hash }

# ---- Docker stderr records redact a non-empty SA value (v3 filter). The product function is parsed from source.
$environmentPath = Join-Path $script:ToolRoot 'test-environment/Invoke-LinuxSqlEnvironment.ps1'
$ast = [Management.Automation.Language.Parser]::ParseFile($environmentPath, [ref]$null, [ref]$null)
$dockerFunction = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -ceq 'Invoke-EnvironmentDocker' }, $true)
. ([scriptblock]::Create($dockerFunction.Extent.Text))

$dockerPrivate = Join-Path $fixtureRoot 'docker-private'
$dockerEvidence = Join-Path $fixtureRoot 'docker-evidence'
[void][IO.Directory]::CreateDirectory($dockerPrivate)
[void][IO.Directory]::CreateDirectory($dockerEvidence)
$fixtureSa = 'Dh1!' + ('0123456789ABCDEF' * 4)
$fixtureRuntime = 'Dh1!' + ('FEDCBA9876543210' * 4)
[IO.File]::WriteAllText((Join-Path $dockerPrivate 'sa.txt'), $fixtureSa)
$plan = [pscustomobject]@{ PrivateRoot = $dockerPrivate; EvidenceRoot = $dockerEvidence }
$script:PreparedChecks = 0
function Assert-LinuxSqlPrepared { param($Plan) $script:PreparedChecks++ }
$script:NextDockerResult = $null
function Invoke-LinuxSqlProcess { param([string]$FileName, [string[]]$Arguments, [int]$Seconds, [int]$OutputLimit) return $script:NextDockerResult }

function Invoke-DockerCase {
    param([string]$Output, [string]$Stderr, [int]$ExitCode, [switch]$IncludeError)
    $script:NextDockerResult = [pscustomobject]@{ ExitCode = $ExitCode; Output = $Output; Error = $Stderr; Pid = 1; BirthUtc = 'fixture' }
    $before = @(Get-ChildItem -LiteralPath $dockerEvidence -File | ForEach-Object FullName)
    $message = Get-ThrownMessage -Action { Invoke-EnvironmentDocker -Arguments @('fixture') -IncludeError:$IncludeError }
    $new = @(Get-ChildItem -LiteralPath $dockerEvidence -File | Where-Object FullName -notin $before)
    $text = if ($new.Count -eq 1) { [IO.File]::ReadAllText($new[0].FullName) } else { $null }
    return [pscustomobject]@{ Message = $message; NewRecords = $new.Count; Text = $text
        Record = $(if ($text) { $text | ConvertFrom-Json } else { $null }) }
}

$saTwice = Invoke-DockerCase -Output '' -Stderr ("before $fixtureSa middle $fixtureSa after $fixtureRuntime end") -ExitCode 0
Assert-Equal -Name 'stderr with SA: exactly one record' -Expected 1 -Actual $saTwice.NewRecords
Assert-True -Name 'stderr with SA: record has no SA, runtime-shaped or Dh1! text' `
    -Condition ($saTwice.Text -and -not $saTwice.Text.Contains($fixtureSa) -and -not $saTwice.Text.Contains($fixtureRuntime) -and
    -not $saTwice.Text.Contains('Dh1!'))
Assert-Equal -Name 'stderr with SA: sanitized text keeps the surrounding diagnostic' `
    -Expected 'before [REDACTED_SA] middle [REDACTED_SA] after [REDACTED_SECRET] end' -Actual $saTwice.Record.SanitizedStderr
Assert-Equal -Name 'stderr with SA: SA replacement count' -Expected 2 -Actual $saTwice.Record.SaReplacements
Assert-Equal -Name 'stderr with SA: other generated secret replacement count' -Expected 1 -Actual $saTwice.Record.OtherSecretReplacements
Assert-Equal -Name 'stderr with SA and exit 0: diagnostic still fails closed' -Expected 'LINUX_SQL_DOCKER_DIAGNOSTIC_SUPPRESSED' `
    -Actual $saTwice.Message
Assert-True -Name 'stderr with SA: prepared private files were checked before reading sa.txt' -Condition ($script:PreparedChecks -ge 1)

$failed = Invoke-DockerCase -Output '' -Stderr ("start failed for $fixtureSa") -ExitCode 1
Assert-Equal -Name 'docker exit 1 with SA: record written then failure' -Expected 'LINUX_SQL_DOCKER_FAILED' -Actual $failed.Message
Assert-True -Name 'docker exit 1 with SA: record is redacted' `
    -Condition ($failed.NewRecords -eq 1 -and -not $failed.Text.Contains($fixtureSa) -and $failed.Record.SaReplacements -eq 1)

$key = Invoke-DockerCase -Output '' -Stderr "-----BEGIN PRIVATE KEY-----`nMIIfixture" -ExitCode 1
Assert-Equal -Name 'stderr with a private key: rejected as unsafe' -Expected 'LINUX_SQL_DOCKER_DIAGNOSTIC_UNSAFE' -Actual $key.Message
Assert-Equal -Name 'stderr with a private key: no record written' -Expected 0 -Actual $key.NewRecords

$partial = Invoke-DockerCase -Output '' -Stderr ('truncated ' + $fixtureSa.Substring(0, 20)) -ExitCode 1
Assert-Equal -Name 'stderr with a truncated secret prefix: rejected as unsafe' -Expected 'LINUX_SQL_DOCKER_DIAGNOSTIC_UNSAFE' `
    -Actual $partial.Message
Assert-Equal -Name 'stderr with a truncated secret prefix: no record written' -Expected 0 -Actual $partial.NewRecords

$clean = Invoke-DockerCase -Output "abc`n" -Stderr '' -ExitCode 0
Assert-True -Name 'empty stderr: output returned and one zero-replacement record' `
    -Condition ($null -eq $clean.Message -and $clean.NewRecords -eq 1 -and $clean.Record.SaReplacements -eq 0)

Complete-TestSuite
