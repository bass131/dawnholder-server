[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the Test-ModuleStructure.ps1 command-line contract.
# Each case runs the real entry point in a separate Windows PowerShell process against a fixture copy.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'module-structure-cli'
$checker = Join-Path $script:ToolRoot 'Test-ModuleStructure.ps1'
# Structure counterexamples that edit a module source re-derive the hash chain with Sync-FixtureHashConsumers,
# so that they stay structure-only (warning pilot exit0, Strict exit1); hash drift is tested in its own suite.

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

function Test-InputMissingCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$File,
        [Parameter(Mandatory)][string]$MessagePattern,
        [Parameter(Mandatory)][string]$MissingFile
    )
    # A registered module path that no longer exists leaves the hash inspection without an input.
    # MSSQL.md documents that as unavailable (exit2 in both modes); the placement issue must stay visible.
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $issues = @(if ($warning.Result) { $warning.Result.Issues })
    $placement = @($issues | Where-Object { $_.File -ceq $File -and $_.Message -cmatch $MessagePattern })
    $missingInput = @($issues | Where-Object {
            $null -ne $_.PSObject.Properties['Kind'] -and $_.Kind -ceq 'HashInspectionUnavailable' -and
            $_.File -ceq $MissingFile
        })
    Assert-True -Name "$Name -> unavailable with null ViolationCount" `
        -Condition ($null -ne $warning.Result -and $warning.Result.Status -ceq 'unavailable' -and
        $null -eq $warning.Result.ViolationCount -and $warning.Result.HashInspectionComplete -eq $false) `
        -Detail (Get-IssueText -Result $warning.Result)
    Assert-True -Name "$Name -> placement issue kept next to the input error" -Condition ($placement.Count -ge 1) `
        -Detail (Get-IssueText -Result $warning.Result)
    Assert-True -Name "$Name -> hash input issue names the missing registered file" `
        -Condition ($missingInput.Count -eq 1) -Detail (Get-IssueText -Result $warning.Result)
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 2 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 2 -Actual $strict.ExitCode
}

# Positive control: the reviewed tree copy is compliant in both modes and lists every file it read.
$compliantRoot = New-DatabaseFixture -Name 'compliant'
$compliant = Invoke-StructureCheck -Root $compliantRoot
$compliantStrict = Invoke-StructureCheck -Root $compliantRoot -Strict
Assert-Equal -Name 'compliant tree -> status' -Expected 'compliant' -Actual $compliant.Result.Status
Assert-Equal -Name 'compliant tree -> ViolationCount' -Expected 0 -Actual $compliant.Result.ViolationCount
# 19 module/permission files and 4 migrations for placement, plus the three hash consumers MSSQL.md requires.
$structureFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $compliantRoot 'modules') -Filter '*.sql' -File -Recurse
    Get-ChildItem -LiteralPath (Join-Path $compliantRoot 'migrations') -Filter '*.sql' -File
) | ForEach-Object { $_.FullName.Substring($compliantRoot.Length + 1).Replace('\', '/') }
$expectedChecked = @($structureFiles) + @('modules/manifest.json', 'verify-schema.sql', 'Module.Common.ps1')
Assert-Equal -Name 'compliant tree -> checked files (23 structure files + manifest, catalog, Module.Common)' `
    -Expected ((@($expectedChecked) | Sort-Object) -join '|') `
    -Actual ((@($compliant.Result.CheckedFiles) | Sort-Object) -join '|')
Assert-Equal -Name 'compliant tree -> checked file count' -Expected 26 -Actual @($compliant.Result.CheckedFiles).Count
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
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'receipt call only in a line comment' -Root $root -File $acquire `
    -MessagePattern '^dh\.AcquireAndLoad does not call its ReadOperationReceipt responsibility$' `
    -ExpectedPattern 'Direct EXEC dh\.ReadOperationReceipt in modules/procedures/acquire_and_load\.sql' `
    -RemediationPattern 'dh\.ReadOperationReceipt'

$root = New-DatabaseFixture -Name 'receipt-in-nested-block-comment'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ('/* outer /* ' + $receiptCall + ' */ still comment */ PRINT 1;')
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'receipt call only in a nested block comment' -Root $root -File $acquire `
    -MessagePattern 'does not call its ReadOperationReceipt responsibility'

$root = New-DatabaseFixture -Name 'receipt-in-string'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ("PRINT N'it''s " + $receiptCall + "';")
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'receipt call only inside a string literal with an escaped quote' -Root $root -File $acquire `
    -MessagePattern 'does not call its ReadOperationReceipt responsibility'

$root = New-DatabaseFixture -Name 'receipt-in-dynamic-sql'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace ("EXEC sys.sp_executesql N'" + $receiptCall + "';")
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'receipt call only as dynamic SQL text' -Root $root -File $acquire `
    -MessagePattern 'does not call its ReadOperationReceipt responsibility'

$root = New-DatabaseFixture -Name 'receipt-bracket-call'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find $receiptCall -Replace 'EXECUTE [dh].[ReadOperationReceipt]'
Sync-FixtureHashConsumers -Root $root
$bracket = Invoke-StructureCheck -Root $root -Strict
Assert-True -Name 'bracket-qualified EXECUTE call is recognized -> compliant' `
    -Condition ($bracket.ExitCode -eq 0 -and $bracket.Result.Status -ceq 'compliant') -Detail (Get-IssueText -Result $bracket.Result)

# Placement: moved, missing, migrated-back and unregistered modules.
$root = New-DatabaseFixture -Name 'public-moved-to-internal'
Move-Item -LiteralPath (Join-Path $root 'modules/procedures/read_admission.sql') `
    -Destination (Join-Path $root 'modules/procedures/internal/read_admission.sql')
$movedCase = @{
    Name = 'public RPC moved to internal folder'
    Root = $root
    File = 'modules/procedures/internal/read_admission.sql'
    MessagePattern = '^Unknown path or non-single module definition$'
    MissingFile = 'modules/procedures/read_admission.sql'
}
Test-InputMissingCase @movedCase
$moved = Invoke-StructureCheck -Root $root
Assert-True -Name 'public RPC moved -> intended issue (file/message/expected/remediation)' -Condition (
    @($moved.Result.Issues | Where-Object {
            $_.File -ceq 'modules/procedures/internal/read_admission.sql' -and
            $_.Expected -cmatch '^dh\.ReadAdmission at modules/procedures/read_admission\.sql$' -and
            $_.Remediation -cmatch 'Move the known object' }).Count -eq 1) -Detail (Get-IssueText -Result $moved.Result)
Assert-True -Name 'public RPC moved -> missing contract path also reported' -Condition (
    @($moved.Result.Issues | Where-Object { $_.File -ceq 'modules/procedures/read_admission.sql' -and
            $_.Message -cmatch '^Required module missing' }).Count -eq 1)

$root = New-DatabaseFixture -Name 'helper-missing'
Remove-Item -LiteralPath (Join-Path $root 'modules/procedures/internal/read_character_state.sql')
$missingCase = @{
    Name = 'helper file missing'
    Root = $root
    File = 'modules/procedures/internal/read_character_state.sql'
    MessagePattern = '^Required module missing or registered under the wrong definition$'
    MissingFile = 'modules/procedures/internal/read_character_state.sql'
}
Test-InputMissingCase @missingCase
$missing = Invoke-StructureCheck -Root $root
$restore = 'Restore the authoritative modules/procedures/internal/read_character_state\.sql'
Assert-True -Name 'helper file missing -> restore remediation kept' -Condition (
    @($missing.Result.Issues | Where-Object { $_.Remediation -cmatch $restore }).Count -eq 1) `
    -Detail (Get-IssueText -Result $missing.Result)

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
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'two definitions in one module file' -Root $root -File 'modules/procedures/internal/serialize_progress.sql' `
    -MessagePattern '^Unknown path or non-single module definition$'

$root = New-DatabaseFixture -Name 'renamed-object'
Edit-FixtureText -Path (Join-Path $root 'modules/procedures/read_admission.sql') `
    -Find 'CREATE OR ALTER PROCEDURE dh.ReadAdmission' -Replace 'CREATE OR ALTER PROCEDURE dh.ReadAdmissionV2'
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'wrong object name at contract path' -Root $root -File 'modules/procedures/read_admission.sql' `
    -MessagePattern '^Unexpected module dh\.ReadAdmissionV2$'

$root = New-DatabaseFixture -Name 'wrong-kind'
Edit-FixtureText -Path (Join-Path $root 'modules/procedures/internal/serialize_progress.sql') `
    -Find 'CREATE OR ALTER PROCEDURE dh.SerializeProgress' -Replace 'CREATE OR ALTER FUNCTION dh.SerializeProgress'
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'wrong module kind at contract path' -Root $root -File 'modules/procedures/internal/serialize_progress.sql' `
    -MessagePattern '^Unexpected module dh\.SerializeProgress$' -ExpectedPattern '\(P\)'

# Operation-specific exceptions stay explicit: admission/inspection/resolvers must not gain responsibilities.
$admission = 'modules/procedures/read_admission.sql'
$root = New-DatabaseFixture -Name 'admission-reads-receipt'
Edit-FixtureText -Path (Join-Path $root $admission) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.ReadOperationReceipt @OperationId = @OperationId;`n        EXEC dh.AssertPersistenceContract"
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'ReadAdmission adds a receipt read' -Root $root -File $admission `
    -MessagePattern '^Unexpected helper/RPC call dh\.ReadOperationReceipt$' -RemediationPattern 'preserve admission/inspection/resolver exceptions'

$inspect = 'modules/procedures/inspect_recovery.sql'
$root = New-DatabaseFixture -Name 'inspection-records-receipt'
Edit-FixtureText -Path (Join-Path $root $inspect) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.RecordOperationReceipt @OperationId = @OperationId;`n        EXEC dh.AssertPersistenceContract"
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'InspectRecovery adds a receipt write' -Root $root -File $inspect `
    -MessagePattern '^Unexpected helper/RPC call dh\.RecordOperationReceipt$'

$resolver = 'modules/procedures/resolve_runtime_operation.sql'
$root = New-DatabaseFixture -Name 'resolver-serializes-snapshot'
Edit-FixtureText -Path (Join-Path $root $resolver) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.SerializePersistenceSnapshot @Kind = @Kind;`n        EXEC dh.AssertPersistenceContract"
Sync-FixtureHashConsumers -Root $root
Test-ViolationCase -Name 'resolver adds a fresh snapshot serializer' -Root $root -File $resolver `
    -MessagePattern '^Unexpected helper/RPC call dh\.SerializePersistenceSnapshot$'

$root = New-DatabaseFixture -Name 'public-calls-public'
Edit-FixtureText -Path (Join-Path $root $acquire) -Find '        EXEC dh.AssertPersistenceContract' `
    -Replace "        EXEC dh.ReadAdmission @SlotId = 1;`n        EXEC dh.AssertPersistenceContract"
Sync-FixtureHashConsumers -Root $root
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
Sync-FixtureHashConsumers -Root $root
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

# Root resolution: the reported DatabaseRoot must name the tree that was actually inspected.
function Get-ReportedRoot {
    param($Result)
    # An output without the field is reported as absent instead of stopping the suite in strict mode.
    if ($null -eq $Result -or $null -eq $Result.PSObject.Properties['DatabaseRoot']) { return '<absent>' }
    return $Result.DatabaseRoot
}

function Invoke-StructureScript {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Lines,
        [Parameter(Mandatory)][string]$ProcessDirectory
    )
    # A runner script lets the session location or a session-only PSDrive differ from the child's process directory.
    $runnerPath = Join-Path $script:SuiteRoot ($Name + '.ps1')
    Write-FixtureText -Path $runnerPath -Text (($Lines -join "`n") + "`nexit `$LASTEXITCODE`n")
    $run = Invoke-PowerShellFile -File $runnerPath -WorkingDirectory $ProcessDirectory
    $result = $null
    if ($run.StdOut.Trim().StartsWith('{')) {
        $result = $run.StdOut | ConvertFrom-Json
    }
    return [pscustomobject]@{
        ExitCode = $run.ExitCode
        Result = $result
        StdOut = $run.StdOut
    }
}

function New-TwoTreeCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][bool]$SessionTreeCompliant
    )
    # Same relative name in both directories; exactly one tree has an unregistered module file.
    # That placement violation leaves every registered hash input intact (warning pilot, Strict exit1).
    $sessionTree = New-DatabaseFixture -Name ($Name + '\session\db')
    $processTree = New-DatabaseFixture -Name ($Name + '\process\db')
    $broken = if ($SessionTreeCompliant) { $processTree } else { $sessionTree }
    Write-FixtureText -Path (Join-Path $broken 'modules/procedures/extra_lookup.sql') `
        -Text "CREATE OR ALTER PROCEDURE dh.ExtraLookup`nAS`nBEGIN`n    SET NOCOUNT ON;`nEND;`n"
    $runner = @{
        Name = $Name + '-runner'
        ProcessDirectory = Split-Path -Parent $processTree
        Lines = @(
            "Set-Location -LiteralPath '$(Split-Path -Parent $sessionTree)'",
            "& '$checker' -DatabaseRoot './db' -Json -Strict"
        )
    }
    return [pscustomobject]@{
        Run = Invoke-StructureScript @runner
        SessionTree = [IO.Path]::GetFullPath($sessionTree)
    }
}

$case = New-TwoTreeCase -Name 'two-tree-session-compliant' -SessionTreeCompliant $true
$caseResult = $case.Run.Result
Assert-True -Name 'relative root: compliant session tree, violating process tree -> compliant Strict exit0' -Condition (
    $case.Run.ExitCode -eq 0 -and $null -ne $caseResult -and $caseResult.Status -ceq 'compliant') `
    -Detail ('exit=' + $case.Run.ExitCode + '; ' + (Get-IssueText -Result $caseResult))
Assert-Equal -Name 'relative root: compliant session tree -> DatabaseRoot is the session tree' `
    -Expected $case.SessionTree -Actual (Get-ReportedRoot -Result $caseResult)

$case = New-TwoTreeCase -Name 'two-tree-session-violation' -SessionTreeCompliant $false
$caseResult = $case.Run.Result
Assert-True -Name 'relative root: violating session tree, compliant process tree -> violation Strict exit1' -Condition (
    $case.Run.ExitCode -eq 1 -and $null -ne $caseResult -and $caseResult.Status -ceq 'violation') `
    -Detail ('exit=' + $case.Run.ExitCode + '; ' + (Get-IssueText -Result $caseResult))
Assert-Equal -Name 'relative root: violating session tree -> DatabaseRoot is the session tree' `
    -Expected $case.SessionTree -Actual (Get-ReportedRoot -Result $caseResult)

# Spellings of the same compliant tree must inspect the same files and report one normalized root.
$expectedRoot = [IO.Path]::GetFullPath($compliantRoot)
$expectedFiles = @($compliant.Result.CheckedFiles) -join '|'
Assert-Equal -Name 'compliant tree -> DatabaseRoot is the normalized absolute root' `
    -Expected $expectedRoot -Actual (Get-ReportedRoot -Result $compliant.Result)
$bracketTree = Join-Path $script:SuiteRoot 'path with [brackets]'
[IO.Directory]::Move((New-DatabaseFixture -Name 'bracket-source'), $bracketTree)
$rootLink = Join-Path $script:SuiteRoot 'root-junction'
$mklink = & cmd.exe /c mklink /J "$rootLink" "$compliantRoot" 2>&1
Assert-True -Name 'fixture junction to the compliant tree created inside the work root' `
    -Condition ([IO.Directory]::Exists($rootLink)) -Detail ([string]$mklink)
$forms = @(
    @{ Name = 'trailing backslash'; Root = $compliantRoot + '\'; Expected = $expectedRoot },
    @{ Name = 'trailing slash'; Root = $compliantRoot + '/'; Expected = $expectedRoot },
    @{ Name = 'doubled trailing separators'; Root = $compliantRoot + '\\'; Expected = $expectedRoot },
    @{ Name = 'forward-slash absolute'; Root = $compliantRoot.Replace('\', '/'); Expected = $expectedRoot },
    @{ Name = 'dot segments'; Root = (Join-Path $compliantRoot 'modules\..\.'); Expected = $expectedRoot },
    @{
        Name = 'provider-qualified FileSystem path'
        Root = 'Microsoft.PowerShell.Core\FileSystem::' + $compliantRoot
        Expected = $expectedRoot
    },
    @{ Name = 'spaces and wildcard brackets'; Root = $bracketTree; Expected = $bracketTree },
    @{ Name = 'junction to the tree'; Root = $rootLink; Expected = $rootLink }
)
foreach ($form in $forms) {
    $run = Invoke-StructureCheck -Root $form.Root -Strict
    Assert-True -Name ('root form ' + $form.Name + ' -> compliant Strict exit0') `
        -Condition ($run.ExitCode -eq 0 -and $null -ne $run.Result -and $run.Result.Status -ceq 'compliant') `
        -Detail ('exit=' + $run.ExitCode + '; ' + (Get-IssueText -Result $run.Result) + $run.StdErr)
    Assert-Equal -Name ('root form ' + $form.Name + ' -> DatabaseRoot') `
        -Expected $form.Expected -Actual (Get-ReportedRoot -Result $run.Result)
    Assert-Equal -Name ('root form ' + $form.Name + ' -> same CheckedFiles') `
        -Expected $expectedFiles -Actual $(if ($run.Result) { @($run.Result.CheckedFiles) -join '|' })
}
[IO.Directory]::Delete($rootLink)
$linkRemoved = -not [IO.Directory]::Exists($rootLink)
$targetKept = [IO.File]::Exists((Join-Path $compliantRoot 'modules/manifest.json'))
Assert-True -Name 'root junction removed and its target tree preserved' -Condition ($linkRemoved -and $targetKept)

$relativeForms = @(
    @{ Name = 'bare relative name'; Root = 'compliant' },
    @{ Name = 'dot-relative with trailing backslash'; Root = '.\compliant\' }
)
foreach ($form in $relativeForms) {
    $run = Invoke-PowerShellFile -File $checker -Arguments @('-DatabaseRoot', $form.Root, '-Json', '-Strict') `
        -WorkingDirectory $script:SuiteRoot
    $result = if ($run.StdOut.Trim().StartsWith('{')) { $run.StdOut | ConvertFrom-Json } else { $null }
    Assert-True -Name ('root form ' + $form.Name + ' -> compliant at the normalized root') -Condition (
        $run.ExitCode -eq 0 -and $null -ne $result -and $result.Status -ceq 'compliant' -and
        (Get-ReportedRoot -Result $result) -ceq $expectedRoot -and
        (@($result.CheckedFiles) -join '|') -ceq $expectedFiles) `
        -Detail ('exit=' + $run.ExitCode + '; root=' + (Get-ReportedRoot -Result $result))
}

$drive = Invoke-StructureScript -Name 'psdrive-root-runner' -ProcessDirectory $script:SuiteRoot -Lines @(
    "`$null = New-PSDrive -Name DhFixture -PSProvider FileSystem -Root '$compliantRoot'",
    "& '$checker' -DatabaseRoot 'DhFixture:\' -Json -Strict"
)
Assert-True -Name 'root form session PSDrive root -> compliant at the provider path' -Condition (
    $drive.ExitCode -eq 0 -and $null -ne $drive.Result -and $drive.Result.Status -ceq 'compliant' -and
    (Get-ReportedRoot -Result $drive.Result) -ceq $expectedRoot -and
    (@($drive.Result.CheckedFiles) -join '|') -ceq $expectedFiles) `
    -Detail ('exit=' + $drive.ExitCode + '; root=' + (Get-ReportedRoot -Result $drive.Result))

$textRoot = Invoke-StructureCheck -Root ($compliantRoot + '\') -Text
# Redirected host output may wrap; the fixture path has no whitespace, so compare with whitespace removed.
Assert-True -Name 'text mode first line names the normalized DatabaseRoot' `
    -Condition (($textRoot.StdOut -replace '\s', '').StartsWith(
        'Modulestructure:compliant.DatabaseRoot:' + $expectedRoot + '.')) -Detail $textRoot.StdOut.Trim()
Assert-True -Name 'compliant tree -> DatabaseRoot machine field present' `
    -Condition ($null -ne $compliant.Result.PSObject.Properties['DatabaseRoot'])

# Roots that cannot name a FileSystem directory are unavailable with the intended cause, not compliant.
$providerCause = '^DatabaseRoot must use the FileSystem provider\.$'
foreach ($case in @(
        @{ Name = 'Variable provider root'; Root = 'Variable:\'; Pattern = $providerCause },
        @{ Name = 'Environment provider root'; Root = 'Env:\'; Pattern = $providerCause },
        @{ Name = 'unknown drive'; Root = 'NoSuchDrive9:\db'; Pattern = 'NoSuchDrive9' }
    )) {
    Test-UnavailableCase -Name ('root ' + $case.Name) -Root $case.Root -MessagePattern $case.Pattern
    $run = Invoke-StructureCheck -Root $case.Root
    Assert-True -Name ('root ' + $case.Name + ' -> DatabaseRoot null') `
        -Condition ($null -ne $run.Result -and $null -eq (Get-ReportedRoot -Result $run.Result))
}
foreach ($case in @(
        @{ Name = 'empty'; Literal = "''" },
        @{ Name = 'whitespace'; Literal = "'   '" }
    )) {
    $blank = Invoke-StructureScript -Name ('blank-root-' + $case.Name) -ProcessDirectory $compliantRoot -Lines @(
        "& '$checker' -DatabaseRoot $($case.Literal) -Json -Strict"
    )
    $blankResult = $blank.Result
    $blankCauses = @(if ($blankResult) {
            $blankResult.Issues | Where-Object Message -CMatch '^DatabaseRoot must be a nonempty FileSystem path\.$'
        })
    Assert-True -Name ('root ' + $case.Name + ' string -> unavailable exit2 with the nonempty-path cause') -Condition (
        $blank.ExitCode -eq 2 -and $null -ne $blankResult -and $blankResult.Status -ceq 'unavailable' -and
        $null -eq $blankResult.ViolationCount -and $null -eq (Get-ReportedRoot -Result $blankResult) -and
        $blankCauses.Count -eq 1) `
        -Detail ('exit=' + $blank.ExitCode + '; ' + (Get-IssueText -Result $blankResult))
}
$driveRoot = [IO.Path]::GetPathRoot($script:SuiteRoot)
$driveModules = Join-Path $driveRoot 'modules'
if ([IO.Directory]::Exists($driveModules)) {
    Add-TestResult -Name 'physical drive root without modules' -Outcome OBSERVED `
        -Detail ($driveModules + ' exists; not run')
}
else {
    $missingModules = '^Module directory cannot be inspected: ' + [regex]::Escape($driveModules) + '\.'
    Test-UnavailableCase -Name 'physical drive root without modules' -Root $driveRoot -MessagePattern $missingModules
    $run = Invoke-StructureCheck -Root $driveRoot
    Assert-Equal -Name 'physical drive root -> DatabaseRoot keeps the drive root separator' `
        -Expected $driveRoot -Actual (Get-ReportedRoot -Result $run.Result)
}

# Scope observation: an extra SQL file reachable only through a junction inside modules/ is not enumerated.
$root = New-DatabaseFixture -Name 'junction-inside-modules'
$outside = Join-Path $script:SuiteRoot 'outside-sql'
[void][IO.Directory]::CreateDirectory($outside)
Write-FixtureText -Path (Join-Path $outside 'extra.sql') -Text "CREATE OR ALTER PROCEDURE dh.Extra AS SELECT 1;`n"
$innerLink = Join-Path $root 'modules/procedures/linked'
$null = & cmd.exe /c mklink /J "$innerLink" "$outside" 2>&1
$inner = Invoke-StructureCheck -Root $root
$innerChecked = @(if ($inner.Result) { $inner.Result.CheckedFiles })
$linkedChecked = @($innerChecked | Where-Object { $_ -like '*linked*' }).Count
Add-TestResult -Name 'scope: SQL behind a junction inside modules/ is not inspected by the CLI' -Outcome OBSERVED `
    -Detail ('status=' + $(if ($inner.Result) { $inner.Result.Status }) + '; linked files checked=' + $linkedChecked)
[IO.Directory]::Delete($innerLink)

# Text mode exposes a structure-only violation even when the warning pilot exits 0.
$root = New-DatabaseFixture -Name 'text-mode-violation'
Write-FixtureText -Path (Join-Path $root 'modules/procedures/extra_lookup.sql') `
    -Text "CREATE OR ALTER PROCEDURE dh.ExtraLookup`nAS`nBEGIN`n    SET NOCOUNT ON;`nEND;`n"
$textViolation = Invoke-StructureCheck -Root $root -Text
# The host may wrap long warning lines when output is redirected; compare whitespace-normalized text.
$textOutput = ($textViolation.StdOut + $textViolation.StdErr).Trim() -replace '\s+', ' '
Assert-True -Name 'text mode violation -> status line and file:line warning with fix' -Condition (
    $textViolation.ExitCode -eq 0 -and $textOutput -cmatch 'Module structure: violation\.' -and
    $textOutput -cmatch 'WARNING: modules/procedures/extra_lookup\.sql:1: .*Fix: Move the known object') `
    -Detail $textOutput

Complete-TestSuite
