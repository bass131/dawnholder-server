[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the Test-ModuleStructure.ps1 command-line contract.
# Each case runs the real entry point in a separate Windows PowerShell process against a fixture copy.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'module-structure-cli'
$checker = Join-Path $script:ToolRoot 'Test-ModuleStructure.ps1'

function Invoke-StructureCheck {
    param(
        [Parameter(Mandatory)][string]$Root,
        [switch]$Strict,
        [switch]$Text
    )
    $arguments = @('-DatabaseRoot', $Root)
    if (-not $Text) { $arguments += '-Json' }
    if ($Strict) { $arguments += '-Strict' }
    $run = Invoke-PowerShellFile -File $checker -Arguments $arguments
    $result = $null
    if (-not $Text -and $run.StdOut.Trim().StartsWith('{')) {
        $result = $run.StdOut | ConvertFrom-Json
    }
    return [pscustomobject]@{
        ExitCode = $run.ExitCode
        Result = $result
        StdOut = $run.StdOut
        StdErr = $run.StdErr
    }
}

function Get-IssueText {
    param($Result)
    if ($null -eq $Result) { return '' }
    return (@($Result.Issues) | ForEach-Object { '{0}:{1}|{2}|{3}|{4}' -f $_.File, $_.Line, $_.Message, $_.Expected, $_.Remediation }) -join ' || '
}

function Test-ViolationCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$File,
        [Parameter(Mandatory)][string]$MessagePattern,
        [string]$ExpectedPattern = '.',
        [string]$RemediationPattern = '.'
    )
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $issues = @(if ($warning.Result) { $warning.Result.Issues })
    $match = @($issues | Where-Object {
        $_.File -ceq $File -and $_.Message -cmatch $MessagePattern -and
        $_.Expected -cmatch $ExpectedPattern -and $_.Remediation -cmatch $RemediationPattern
    })
    Assert-True -Name "$Name -> violation status" -Condition ($null -ne $warning.Result -and $warning.Result.Status -ceq 'violation') `
        -Detail (Get-IssueText -Result $warning.Result)
    Assert-True -Name "$Name -> intended issue (file/message/expected/remediation)" -Condition ($match.Count -ge 1) `
        -Detail (Get-IssueText -Result $warning.Result)
    Assert-True -Name "$Name -> ViolationCount equals issue count" `
        -Condition ($null -ne $warning.Result -and $warning.Result.ViolationCount -eq $issues.Count -and $issues.Count -gt 0)
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 0 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 1 -Actual $strict.ExitCode
}

function Test-UnavailableCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$MessagePattern
    )
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $issue = @(if ($warning.Result) { $warning.Result.Issues })
    Assert-True -Name "$Name -> unavailable with null ViolationCount" `
        -Condition ($null -ne $warning.Result -and $warning.Result.Status -ceq 'unavailable' -and
            $null -eq $warning.Result.ViolationCount) -Detail (Get-IssueText -Result $warning.Result)
    Assert-True -Name "$Name -> intended unavailable cause" `
        -Condition (@($issue | Where-Object Message -CMatch $MessagePattern).Count -eq 1) -Detail (Get-IssueText -Result $warning.Result)
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 2 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 2 -Actual $strict.ExitCode
}

# Positive control: the reviewed tree copy is compliant in both modes and lists every module and migration file.
$compliantRoot = New-DatabaseFixture -Name 'compliant'
$compliant = Invoke-StructureCheck -Root $compliantRoot
$compliantStrict = Invoke-StructureCheck -Root $compliantRoot -Strict
Assert-Equal -Name 'compliant tree -> status' -Expected 'compliant' -Actual $compliant.Result.Status
Assert-Equal -Name 'compliant tree -> ViolationCount' -Expected 0 -Actual $compliant.Result.ViolationCount
Assert-Equal -Name 'compliant tree -> checked files (19 modules + 4 migrations)' -Expected 23 -Actual @($compliant.Result.CheckedFiles).Count
Assert-Equal -Name 'compliant tree -> warning exit' -Expected 0 -Actual $compliant.ExitCode
Assert-Equal -Name 'compliant tree -> Strict exit' -Expected 0 -Actual $compliantStrict.ExitCode
Assert-True -Name 'compliant tree -> machine fields' -Condition (
    @('Status', 'Scope', 'CheckedFiles', 'Issues', 'ViolationCount' | Where-Object { $null -eq $compliant.Result.PSObject.Properties[$_] }).Count -eq 0)
$text = Invoke-StructureCheck -Root $compliantRoot -Text
Assert-True -Name 'compliant tree -> text mode status line' -Condition ($text.StdOut -cmatch 'Module structure: compliant\.') -Detail $text.StdOut.Trim()

# Real tree in place (read-only) with the default root.
$default = Invoke-PowerShellFile -File $checker -Arguments @('-Json', '-Strict')
Assert-True -Name 'repository tree default root -> compliant Strict exit0' `
    -Condition ($default.ExitCode -eq 0 -and ($default.StdOut | ConvertFrom-Json).Status -ceq 'compliant')

# Comment and literal mentions are not calls; real qualified calls are.
$acquire = 'modules/procedures/acquire_and_load.sql'
$receiptCall = 'EXEC dh.ReadOperationReceipt'
$root = New-DatabaseFixture -Name 'receipt-in-line-comment'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ('-- ' + $receiptCall)
Test-ViolationCase -Name 'receipt call only in a line comment' -Root $root -File $acquire `
    -MessagePattern '^dh\.AcquireAndLoad does not call its ReadOperationReceipt responsibility$' `
    -ExpectedPattern 'Direct EXEC dh\.ReadOperationReceipt in modules/procedures/acquire_and_load\.sql' `
    -RemediationPattern 'dh\.ReadOperationReceipt'

$root = New-DatabaseFixture -Name 'receipt-in-nested-block-comment'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ('/* outer /* ' + $receiptCall + ' */ still comment */ PRINT 1;')
Test-ViolationCase -Name 'receipt call only in a nested block comment' -Root $root -File $acquire `
    -MessagePattern 'does not call its ReadOperationReceipt responsibility'

$root = New-DatabaseFixture -Name 'receipt-in-string'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ("PRINT N'it''s " + $receiptCall + "';")
Test-ViolationCase -Name 'receipt call only inside a string literal with an escaped quote' -Root $root -File $acquire `
    -MessagePattern 'does not call its ReadOperationReceipt responsibility'

$root = New-DatabaseFixture -Name 'receipt-in-dynamic-sql'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ("EXEC sys.sp_executesql N'" + $receiptCall + "';")
Test-ViolationCase -Name 'receipt call only as dynamic SQL text' -Root $root -File $acquire `
    -MessagePattern 'does not call its ReadOperationReceipt responsibility'

$root = New-DatabaseFixture -Name 'receipt-bracket-call'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace 'EXECUTE [dh].[ReadOperationReceipt]'
$bracket = Invoke-StructureCheck -Root $root -Strict
Assert-True -Name 'bracket-qualified EXECUTE call is recognized -> compliant' `
    -Condition ($bracket.ExitCode -eq 0 -and $bracket.Result.Status -ceq 'compliant') -Detail (Get-IssueText -Result $bracket.Result)

# Placement: moved, missing, migrated-back and unregistered modules.
$root = New-DatabaseFixture -Name 'public-moved-to-internal'
Move-Item -LiteralPath (Join-Path $root 'modules/procedures/read_admission.sql') `
    -Destination (Join-Path $root 'modules/procedures/internal/read_admission.sql')
Test-ViolationCase -Name 'public RPC moved to internal folder' -Root $root -File 'modules/procedures/internal/read_admission.sql' `
    -MessagePattern '^Unknown path or non-single module definition$' `
    -ExpectedPattern '^dh\.ReadAdmission at modules/procedures/read_admission\.sql$' -RemediationPattern 'Move the known object'
$moved = Invoke-StructureCheck -Root $root
Assert-True -Name 'public RPC moved -> missing contract path also reported' -Condition (
    @($moved.Result.Issues | Where-Object { $_.File -ceq 'modules/procedures/read_admission.sql' -and
        $_.Message -cmatch '^Required module missing' }).Count -eq 1)

$root = New-DatabaseFixture -Name 'helper-missing'
Remove-Item -LiteralPath (Join-Path $root 'modules/procedures/internal/read_character_state.sql')
Test-ViolationCase -Name 'helper file missing' -Root $root -File 'modules/procedures/internal/read_character_state.sql' `
    -MessagePattern '^Required module missing or registered under the wrong definition$' `
    -RemediationPattern 'Restore the authoritative modules/procedures/internal/read_character_state\.sql'

$root = New-DatabaseFixture -Name 'procedure-back-in-migrations'
Copy-Item -LiteralPath (Join-Path $root 'modules/procedures/read_admission.sql') `
    -Destination (Join-Path $root 'migrations/005_read_admission.sql')
Test-ViolationCase -Name 'procedure definition placed back in migrations' -Root $root -File 'migrations/005_read_admission.sql' `
    -MessagePattern '^Unknown path or non-single module definition$' -ExpectedPattern 'dh\.ReadAdmission at modules/procedures/read_admission\.sql'

$root = New-DatabaseFixture -Name 'unregistered-module'
Write-FixtureText -Path (Join-Path $root 'modules/procedures/extra_lookup.sql') `
    -Text "CREATE OR ALTER PROCEDURE dh.ExtraLookup`nAS`nBEGIN`n    SET NOCOUNT ON;`nEND;`n"
Test-ViolationCase -Name 'unregistered module file' -Root $root -File 'modules/procedures/extra_lookup.sql' `
    -MessagePattern '^Unknown path or non-single module definition$' -RemediationPattern 'remove unregistered SQL'

$root = New-DatabaseFixture -Name 'two-definitions'
$path = Join-Path $root 'modules/procedures/internal/serialize_progress.sql'
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + "GO`nCREATE PROCEDURE dh.Second AS SELECT 1;`n")
Test-ViolationCase -Name 'two definitions in one module file' -Root $root -File 'modules/procedures/internal/serialize_progress.sql' `
    -MessagePattern '^Unknown path or non-single module definition$'

$root = New-DatabaseFixture -Name 'renamed-object'
Edit-FixtureText -Path (Join-Path $root 'modules/procedures/read_admission.sql') `
    -Find 'CREATE OR ALTER PROCEDURE dh.ReadAdmission' -Replace 'CREATE OR ALTER PROCEDURE dh.ReadAdmissionV2'
Test-ViolationCase -Name 'wrong object name at contract path' -Root $root -File 'modules/procedures/read_admission.sql' `
    -MessagePattern '^Unexpected module dh\.ReadAdmissionV2$'

$root = New-DatabaseFixture -Name 'wrong-kind'
Edit-FixtureText -Path (Join-Path $root 'modules/procedures/internal/serialize_progress.sql') `
    -Find 'CREATE OR ALTER PROCEDURE dh.SerializeProgress' -Replace 'CREATE OR ALTER FUNCTION dh.SerializeProgress'
Test-ViolationCase -Name 'wrong module kind at contract path' -Root $root -File 'modules/procedures/internal/serialize_progress.sql' `
    -MessagePattern '^Unexpected module dh\.SerializeProgress$' -ExpectedPattern '\(P\)'

# Operation-specific exceptions stay explicit: admission/inspection/resolvers must not gain responsibilities.
$admission = 'modules/procedures/read_admission.sql'
$root = New-DatabaseFixture -Name 'admission-reads-receipt'
Edit-FixtureText -Path (Join-Path $root $admission) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.ReadOperationReceipt @OperationId = @OperationId;`n        EXEC dh.AssertPersistenceContract"
Test-ViolationCase -Name 'ReadAdmission adds a receipt read' -Root $root -File $admission `
    -MessagePattern '^Unexpected helper/RPC call dh\.ReadOperationReceipt$' -RemediationPattern 'preserve admission/inspection/resolver exceptions'

$inspect = 'modules/procedures/inspect_recovery.sql'
$root = New-DatabaseFixture -Name 'inspection-records-receipt'
Edit-FixtureText -Path (Join-Path $root $inspect) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.RecordOperationReceipt @OperationId = @OperationId;`n        EXEC dh.AssertPersistenceContract"
Test-ViolationCase -Name 'InspectRecovery adds a receipt write' -Root $root -File $inspect `
    -MessagePattern '^Unexpected helper/RPC call dh\.RecordOperationReceipt$'

$resolver = 'modules/procedures/resolve_runtime_operation.sql'
$root = New-DatabaseFixture -Name 'resolver-serializes-snapshot'
Edit-FixtureText -Path (Join-Path $root $resolver) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.SerializePersistenceSnapshot @Kind = @Kind;`n        EXEC dh.AssertPersistenceContract"
Test-ViolationCase -Name 'resolver adds a fresh snapshot serializer' -Root $root -File $resolver `
    -MessagePattern '^Unexpected helper/RPC call dh\.SerializePersistenceSnapshot$'

$root = New-DatabaseFixture -Name 'public-calls-public'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.ReadAdmission @SlotId = 1;`n        EXEC dh.AssertPersistenceContract"
Test-ViolationCase -Name 'public RPC calls another public RPC' -Root $root -File $acquire `
    -MessagePattern '^Unexpected helper/RPC call dh\.ReadAdmission$'

# Inputs that cannot be inspected are unavailable in both modes, never compliant or violation.
$root = New-DatabaseFixture -Name 'unterminated-comment'
$path = Join-Path $root 'modules/procedures/internal/emit_persistence_result.sql'
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + "/* unfinished`n")
Test-UnavailableCase -Name 'unterminated block comment' -Root $root -MessagePattern '^Unterminated SQL block comment'

$root = New-DatabaseFixture -Name 'unterminated-string'
$path = Join-Path $root 'modules/procedures/internal/emit_persistence_result.sql'
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + "PRINT 'unfinished`n")
Test-UnavailableCase -Name 'unterminated string literal' -Root $root -MessagePattern '^Unterminated SQL string'

$root = New-DatabaseFixture -Name 'unterminated-identifier'
$path = Join-Path $root 'modules/procedures/internal/emit_persistence_result.sql'
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path) + "SELECT [unfinished`n")
Test-UnavailableCase -Name 'unterminated bracket identifier' -Root $root -MessagePattern '^Unterminated SQL identifier'

$root = Join-Path $script:SuiteRoot 'no-modules'
[void][IO.Directory]::CreateDirectory($root)
Test-UnavailableCase -Name 'root without modules directory' -Root $root -MessagePattern '^Module directory cannot be inspected'
Test-UnavailableCase -Name 'nonexistent root' -Root (Join-Path $script:SuiteRoot 'does-not-exist') `
    -MessagePattern '^Module directory cannot be inspected'

# Scope limits observed, not judged as checker defects: permissions content and helper bodies are not inspected here.
$root = New-DatabaseFixture -Name 'helper-opens-transaction'
Edit-FixtureText -Path (Join-Path $root 'modules/procedures/internal/serialize_progress.sql') `
    -Find '    SET NOCOUNT ON;' -Replace "    SET NOCOUNT ON;`n    BEGIN TRANSACTION;"
$scope = Invoke-StructureCheck -Root $root
Add-TestResult -Name 'scope: helper body with BEGIN TRANSACTION is not inspected by the CLI' -Outcome OBSERVED `
    -Detail ('status=' + $scope.Result.Status)

# Path forms a person would pass for the same compliant tree.
$trailing = Invoke-StructureCheck -Root ($compliantRoot + '\')
Assert-True -Name 'compliant tree with trailing separator -> compliant' `
    -Condition ($trailing.ExitCode -eq 0 -and $null -ne $trailing.Result -and $trailing.Result.Status -ceq 'compliant') `
    -Detail ('status=' + $(if ($trailing.Result) { $trailing.Result.Status }) + '; first issue=' +
        $(if ($trailing.Result -and @($trailing.Result.Issues).Count) { (Get-IssueText -Result $trailing.Result).Substring(0, 160) }))
$trailingStrict = Invoke-StructureCheck -Root ($compliantRoot + '\') -Strict
Assert-Equal -Name 'compliant tree with trailing separator -> Strict exit' -Expected 0 -Actual $trailingStrict.ExitCode

# Documented relative form after Set-Location inside an existing session (process directory differs).
$fixtureParent = Split-Path -Parent $compliantRoot
$fixtureName = Split-Path -Leaf $compliantRoot
$elsewhere = Join-Path $script:SuiteRoot 'process-directory'
[void][IO.Directory]::CreateDirectory($elsewhere)
$runner = Join-Path $script:SuiteRoot 'relative-runner.ps1'
Write-FixtureText -Path $runner -Text (
    "Set-Location -LiteralPath '$fixtureParent'`n" +
    "& '$checker' -DatabaseRoot './$fixtureName' -Json -Strict`n" +
    "exit `$LASTEXITCODE`n")
$relative = Invoke-PowerShellFile -File $runner -WorkingDirectory $elsewhere
$relativeResult = if ($relative.StdOut.Trim().StartsWith('{')) { $relative.StdOut | ConvertFrom-Json } else { $null }
Assert-True -Name 'relative -DatabaseRoot after Set-Location (MSSQL.md example form) -> compliant' `
    -Condition ($relative.ExitCode -eq 0 -and $null -ne $relativeResult -and $relativeResult.Status -ceq 'compliant') `
    -Detail ('exit=' + $relative.ExitCode + '; status=' + $(if ($relativeResult) { $relativeResult.Status }) + '; ' +
        $(if ($relativeResult) { (Get-IssueText -Result $relativeResult) }))
$relativeSameDirectory = Invoke-PowerShellFile -File $checker -Arguments @('-DatabaseRoot', "./$fixtureName", '-Json', '-Strict') `
    -WorkingDirectory $fixtureParent
Assert-Equal -Name 'relative -DatabaseRoot when process directory matches -> Strict exit (control)' -Expected 0 `
    -Actual $relativeSameDirectory.ExitCode

# Text mode exposes a violation even when the warning pilot exits 0.
$root = New-DatabaseFixture -Name 'text-mode-violation'
Remove-Item -LiteralPath (Join-Path $root 'modules/procedures/internal/read_character_state.sql')
$textViolation = Invoke-StructureCheck -Root $root -Text
# The host may wrap long warning lines when output is redirected; compare whitespace-normalized text.
$textOutput = ($textViolation.StdOut + $textViolation.StdErr).Trim() -replace '\s+', ' '
Assert-True -Name 'text mode violation -> status line and file:line warning with fix' -Condition (
    $textViolation.ExitCode -eq 0 -and $textOutput -cmatch 'Module structure: violation\.' -and
    $textOutput -cmatch 'WARNING: modules/procedures/internal/read_character_state\.sql:1: .*Fix: Restore') `
    -Detail $textOutput

Complete-TestSuite
