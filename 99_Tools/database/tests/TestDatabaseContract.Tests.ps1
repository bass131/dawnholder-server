[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the Test-Database.ps1 entry contract (TESTDB-01; user decision msg_6a82c1c724ad,
# main scope msg_4bcc54fa9ef0): an explicit reviewed plan, exact Instance/Database and execution approval are
# checked before any SQL connection object exists; all six Complete runner calls receive that one reader-produced
# Contract; and the two migration rejection patterns accept only the current runner throws.
# 1. Static checks read the script AST, so a reverted call argument or a widened pattern fails here again.
# 2. The patterns run in their own Assert-Rejected definition against the real Invoke-Migrations over the narrow
#    fake connection of TestSupport.ps1. Fake responses (lock -1, history rows) are fixture data, not engine results.
# 3. The real Test-Database.ps1 file runs in a separate Windows PowerShell process up to its first SqlConnection,
#    which the harness refuses to create. It never runs the database checks after that point.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'test-database-contract'
. (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
. (Join-Path $script:ToolRoot 'Database.Common.ps1')
Set-OfflineStubs

$testDatabasePath = [IO.Path]::GetFullPath((Join-Path $script:ToolRoot 'Test-Database.ps1'))
$fixtureDatabase = 'Dawnholder_Dev_Fixture'
$fixtureInstance = '.\FIXTURE'
$parseTokens = $null
$parseErrors = $null
$scriptAst = [Management.Automation.Language.Parser]::ParseFile($testDatabasePath, [ref]$parseTokens,
    [ref]$parseErrors)

function Find-Ast {
    param([Parameter(Mandatory)]$Root, [Parameter(Mandatory)][scriptblock]$Predicate)
    return @($Root.FindAll($Predicate, $true))
}

function Get-CommandArguments {
    param([Parameter(Mandatory)][Management.Automation.Language.CommandAst]$Command)
    # Named parameter -> argument AST (case-insensitive like PowerShell binding), plus positional arguments.
    $named = [Collections.Specialized.OrderedDictionary]::new([StringComparer]::OrdinalIgnoreCase)
    $positional = @()
    $elements = @($Command.CommandElements | Select-Object -Skip 1)
    for ($index = 0; $index -lt $elements.Count; $index++) {
        $element = $elements[$index]
        if ($element -is [Management.Automation.Language.CommandParameterAst]) {
            $argument = $element.Argument
            $next = $index + 1
            if ($null -eq $argument -and $next -lt $elements.Count -and
                $elements[$next] -isnot [Management.Automation.Language.CommandParameterAst]) {
                $argument = $elements[$next]
                $index = $next
            }
            $named[$element.ParameterName] = $argument
        } else {
            $positional += $element
        }
    }
    return [pscustomobject]@{ Named = $named; Positional = $positional }
}

function Test-VariableArgument {
    param($Argument, [Parameter(Mandatory)][string]$Name)
    return $Argument -is [Management.Automation.Language.VariableExpressionAst] -and
        $Argument.VariablePath.UserPath -ieq $Name
}

function Get-AssignedRoots {
    param([Parameter(Mandatory)]$Left)
    # The variables an assignment writes, including $x.Member, $x[0], [type]$x and $a, $b targets.
    $targets = @($Left)
    if ($Left -is [Management.Automation.Language.ArrayLiteralAst]) {
        $targets = @($Left.Elements)
    }
    foreach ($target in $targets) {
        $node = $target
        while ($null -ne $node) {
            if ($node -is [Management.Automation.Language.VariableExpressionAst]) {
                $node.VariablePath.UserPath
                break
            }
            if ($node -is [Management.Automation.Language.MemberExpressionAst]) {
                $node = $node.Expression
            } elseif ($node -is [Management.Automation.Language.IndexExpressionAst]) {
                $node = $node.Target
            } elseif ($node -is [Management.Automation.Language.ConvertExpressionAst]) {
                $node = $node.Child
            } else {
                '<unresolved:' + $node.GetType().Name + '>'
                break
            }
        }
    }
}

function Get-CommandCalls {
    param([Parameter(Mandatory)]$Root, [Parameter(Mandatory)][string]$Name)
    $predicate = {
        param($node)
        $node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -ieq $Name
    }.GetNewClosure()
    return @(Find-Ast -Root $Root -Predicate $predicate)
}

# ---- 1. Static recurrence checks of the entry and the six runner calls.
Assert-Equal -Name 'Test-Database.ps1 parses without errors' -Expected 0 -Actual $parseErrors.Count

$parameters = @($(if ($null -ne $scriptAst.ParamBlock) { $scriptAst.ParamBlock.Parameters }))
$parameterNames = @($parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
$requiredInputs = @('Instance', 'Database', 'ApprovalPlanPath', 'ExpectedApprovalPlanHash')
$missingInputs = @($requiredInputs | Where-Object { $_ -notin $parameterNames })
$defaulted = @($parameters | Where-Object { $null -ne $_.DefaultValue } | ForEach-Object {
        $_.Name.VariablePath.UserPath + '=' + $_.DefaultValue.Extent.Text
    })
Assert-True -Name 'entry takes explicit Instance/Database and reviewed plan path/hash with no default value' `
    -Condition ($missingInputs.Count -eq 0 -and $defaulted.Count -eq 0) `
    -Detail ('parameters=' + ($parameterNames -join ',') + '; missing=' + ($missingInputs -join ',') +
        '; defaults=' + ($defaulted -join ' | '))

$environmentReads = @(Find-Ast -Root $scriptAst -Predicate {
        param($node)
        ($node -is [Management.Automation.Language.VariableExpressionAst] -and
            $node.VariablePath.DriveName -ieq 'env') -or
        ($node -is [Management.Automation.Language.StringConstantExpressionAst] -and
            $node.Value -match 'DAWNHOLDER_SQL_') -or
        ($node -is [Management.Automation.Language.InvokeMemberExpressionAst] -and
            $node.Member.Extent.Text -ieq 'GetEnvironmentVariable')
    } | ForEach-Object { $_.Extent.StartLineNumber.ToString() + ':' + $_.Extent.Text })
Assert-True -Name 'Test-Database.ps1 reads no environment value (no instance/database fallback)' `
    -Condition ($environmentReads.Count -eq 0) -Detail ($environmentReads -join ' | ')

$contractAssignments = @(Find-Ast -Root $scriptAst -Predicate {
        param($node)
        $node -is [Management.Automation.Language.AssignmentStatementAst] -and
            @(Get-AssignedRoots -Left $node.Left) -icontains 'Contract'
    })
$readerBinding = $false
$readerStatement = $null
if ($contractAssignments.Count -eq 1) {
    $assignment = $contractAssignments[0]
    $readerCalls = @(Get-CommandCalls -Root $assignment.Right -Name 'Read-TestEnvironmentApprovalPlan')
    $whole = $assignment.Left -is [Management.Automation.Language.VariableExpressionAst] -and
        $assignment.Right -is [Management.Automation.Language.PipelineAst] -and
        @($assignment.Right.PipelineElements).Count -eq 1 -and $readerCalls.Count -eq 1
    if ($whole) {
        $binding = Get-CommandArguments -Command $readerCalls[0]
        $readerBinding = $binding.Positional.Count -eq 0 -and $binding.Named.Count -eq 2 -and
            (Test-VariableArgument -Argument $binding.Named['ApprovalPlanPath'] -Name 'ApprovalPlanPath') -and
            (Test-VariableArgument -Argument $binding.Named['ExpectedApprovalPlanHash'] `
                    -Name 'ExpectedApprovalPlanHash')
        $readerStatement = $assignment
    }
}
Assert-True -Name 'the Contract is assigned once, from the reviewed plan reader with the entry path and hash' `
    -Condition $readerBinding `
    -Detail ('assignments=' + (@($contractAssignments | ForEach-Object {
                    $_.Extent.StartLineNumber.ToString() + ':' + ($_.Extent.Text -replace '\s+', ' ')
                }) -join ' | '))

# No later write may synthesize or relax approval: no member/index write on any variable for the approval fields, no
# variable cmdlets and no Contract argument other than the reader result.
$approvalWrites = @(Find-Ast -Root $scriptAst -Predicate {
        param($node)
        $node -is [Management.Automation.Language.AssignmentStatementAst] -and
            $node.Left -isnot [Management.Automation.Language.VariableExpressionAst] -and
            ($node.Left.Extent.Text -match '(?i)ExecutionApproved|\bG2\b' -or
                @(Get-AssignedRoots -Left $node.Left) -icontains 'Contract')
    })
$variableCommands = @('Add-Member', 'Set-Variable', 'New-Variable', 'Remove-Variable', 'Clear-Variable') |
    ForEach-Object { Get-CommandCalls -Root $scriptAst -Name $_ }
$contractArguments = @(Find-Ast -Root $scriptAst -Predicate {
        param($node)
        $node -is [Management.Automation.Language.CommandParameterAst] -and $node.ParameterName -ieq 'Contract'
    } | ForEach-Object {
        $call = $_.Parent
        $binding = Get-CommandArguments -Command $call
        [pscustomobject]@{
            Line = $_.Extent.StartLineNumber
            Ok = Test-VariableArgument -Argument $binding.Named['Contract'] -Name 'Contract'
        }
    })
$foreignContract = @($contractArguments | Where-Object { -not $_.Ok } | ForEach-Object Line)
Assert-True -Name 'nothing rewrites the Contract or approval fields, and every -Contract is that reader result' `
    -Condition ($approvalWrites.Count -eq 0 -and @($variableCommands).Count -eq 0 -and $foreignContract.Count -eq 0 -and
        $contractArguments.Count -ge 8) `
    -Detail ('approvalWrites=' + (@($approvalWrites | ForEach-Object { $_.Extent.Text }) -join ' | ') +
        '; variableCommands=' + @($variableCommands).Count + '; contractArguments=' + $contractArguments.Count +
        '; foreign=' + ($foreignContract -join ','))

$topLevel = @($scriptAst.EndBlock.Statements)
function Get-TopLevelIndex {
    param($Node)
    for ($index = 0; $index -lt $topLevel.Count; $index++) {
        if ($Node.Extent.StartOffset -ge $topLevel[$index].Extent.StartOffset -and
            $Node.Extent.EndOffset -le $topLevel[$index].Extent.EndOffset) {
            return $index
        }
    }
    return -1
}
$targetChecks = @(Get-CommandCalls -Root $scriptAst -Name 'Assert-TestEnvironmentTarget' | Where-Object {
        $binding = Get-CommandArguments -Command $_
        $binding.Positional.Count -eq 0 -and
            (Test-VariableArgument -Argument $binding.Named['Contract'] -Name 'Contract') -and
            (Test-VariableArgument -Argument $binding.Named['Database'] -Name 'Database') -and
            (Test-VariableArgument -Argument $binding.Named['Instance'] -Name 'Instance')
    })
$approvalChecks = @(Get-CommandCalls -Root $scriptAst -Name 'Assert-TestEnvironmentExecutionApproval' | Where-Object {
        $binding = Get-CommandArguments -Command $_
        $binding.Positional.Count -eq 0 -and
            (Test-VariableArgument -Argument $binding.Named['Contract'] -Name 'Contract')
    })
$connectionStatements = @(Get-CommandCalls -Root $scriptAst -Name 'Open-LocalDatabase' | ForEach-Object {
        Get-TopLevelIndex -Node $_
    } | Sort-Object)
$firstConnection = $(if ($connectionStatements.Count) { $connectionStatements[0] } else { -1 })
$gateIndexes = @(
    $(if ($null -ne $readerStatement) { Get-TopLevelIndex -Node $readerStatement } else { -1 }),
    $(if ($targetChecks.Count -eq 1) { Get-TopLevelIndex -Node $targetChecks[0] } else { -1 }),
    $(if ($approvalChecks.Count -eq 1) { Get-TopLevelIndex -Node $approvalChecks[0] } else { -1 })
)
$lateGates = @($gateIndexes | Where-Object { $_ -lt 0 -or $_ -ge $firstConnection })
$gatesFirst = $firstConnection -ge 0 -and $lateGates.Count -eq 0
Assert-True -Name 'reader, exact target (with Instance) and execution approval run before any Open-LocalDatabase' `
    -Condition $gatesFirst `
    -Detail ('gateStatements=' + ($gateIndexes -join ',') + '; firstConnectionStatement=' + $firstConnection +
        '; targetChecks=' + $targetChecks.Count + '; approvalChecks=' + $approvalChecks.Count)

$runnerCalls = @(Get-CommandCalls -Root $scriptAst -Name 'Invoke-Migrations')
Assert-Equal -Name 'Test-Database.ps1 calls Invoke-Migrations six times (msg_4bcc54fa9ef0)' -Expected 6 `
    -Actual $runnerCalls.Count
$runnerBindings = @($runnerCalls | ForEach-Object {
        $binding = Get-CommandArguments -Command $_
        $phase = $binding.Named['Phase']
        $keys = @($binding.Named.Keys | Sort-Object) -join ','
        [pscustomobject]@{
            Line = $_.Extent.StartLineNumber
            Ok = $binding.Positional.Count -eq 0 -and $keys -ieq 'Connection,Contract,Phase,Transaction' -and
                (Test-VariableArgument -Argument $binding.Named['Connection'] -Name 'connection') -and
                (Test-VariableArgument -Argument $binding.Named['Transaction'] -Name 'transaction') -and
                $phase -is [Management.Automation.Language.StringConstantExpressionAst] -and
                $phase.Value -ceq 'Complete' -and
                (Test-VariableArgument -Argument $binding.Named['Contract'] -Name 'Contract')
            Text = $_.Extent.Text -replace '\s+', ' '
        }
    })
Assert-True `
    -Name 'every Invoke-Migrations call names -Connection/-Transaction, -Phase Complete and -Contract $Contract' `
    -Condition ($runnerBindings.Count -eq 6 -and @($runnerBindings | Where-Object { -not $_.Ok }).Count -eq 0) `
    -Detail (@($runnerBindings | ForEach-Object { $_.Line.ToString() + ':' + $_.Ok + ':' + $_.Text }) -join ' | ')

# The two migration rejection checks: Assert-Rejected around Invoke-Migrations, each after its own history stimulus.
$rejectionChecks = @(Get-CommandCalls -Root $scriptAst -Name 'Assert-Rejected' | Where-Object {
        $binding = Get-CommandArguments -Command $_
        $action = $binding.Named['Action']
        $null -ne $action -and @(Get-CommandCalls -Root $action -Name 'Invoke-Migrations').Count -eq 1
    } | ForEach-Object {
        $binding = Get-CommandArguments -Command $_
        $pattern = $binding.Named['Pattern']
        # The stimulus is the statement just before this check inside the same block.
        $block = $_.Parent
        while ($null -ne $block -and $block -isnot [Management.Automation.Language.NamedBlockAst] -and
            $block -isnot [Management.Automation.Language.StatementBlockAst]) {
            $block = $block.Parent
        }
        $own = $_
        $siblings = @($block.Statements)
        $position = @(0..($siblings.Count - 1) | Where-Object {
                $own.Extent.StartOffset -ge $siblings[$_].Extent.StartOffset -and
                $own.Extent.EndOffset -le $siblings[$_].Extent.EndOffset
            })
        $stimulus = ''
        if ($position.Count -eq 1 -and $position[0] -gt 0) {
            $stimulus = $siblings[$position[0] - 1].Extent.Text
        }
        [pscustomobject]@{
            Line = $_.Extent.StartLineNumber
            Constant = $pattern -is [Management.Automation.Language.StringConstantExpressionAst]
            Pattern = $(if ($pattern -is [Management.Automation.Language.StringConstantExpressionAst]) {
                    $pattern.Value
                } else {
                    $null
                })
            Stimulus = $(if ($stimulus -match 'UPDATE dh\.SchemaVersion SET Checksum') {
                    'ChecksumDrift'
                } elseif ($stimulus -match 'INSERT dh\.SchemaVersion') {
                    'UnknownVersion'
                } else {
                    'unknown stimulus'
                })
        }
    })
Assert-True -Name 'two migration rejection checks use constant patterns after a drift and an unknown-version stimulus' `
    -Condition ($rejectionChecks.Count -eq 2 -and
        @($rejectionChecks | Where-Object { -not $_.Constant }).Count -eq 0 -and
        ((@($rejectionChecks | ForEach-Object Stimulus) | Sort-Object) -join ',') -ceq 'ChecksumDrift,UnknownVersion') `
    -Detail (@($rejectionChecks | ForEach-Object {
                $_.Line.ToString() + ':' + $_.Stimulus + ':' + $_.Pattern
            }) -join ' | ')

# ---- 2. The extracted Assert-Rejected and both patterns against the real runner's rejections.
$assertRejected = $scriptAst.Find({
        param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Assert-Rejected'
    }, $true)
$script:FixtureContract = [pscustomobject]@{
    Database = $fixtureDatabase
    Instance = $fixtureInstance
    ExecutionApproved = $true
    G2 = 'in-memory offline fixture; not an approval record'
}
# Applied history recomputed from the migration files by TestSupport (UTF-8, LF), independent of the product hash.
$migrationNames = @(
    '001_initial.sql', '002_persistence_metadata.sql', '003_module_metadata.sql', '004_module_release.sql'
)
$appliedRows = @(for ($index = 0; $index -lt $migrationNames.Count; $index++) {
        $path = Join-Path $script:ToolRoot ('migrations/' + $migrationNames[$index])
        [pscustomobject][ordered]@{
            Version = $index + 1
            Name = $migrationNames[$index]
            Checksum = (Get-ExpectedModuleValues -Path $path).SourceChecksum
        }
    })
$driftRows = @($appliedRows | ForEach-Object { $_.PSObject.Copy() })
$driftRows[0].Checksum = '0' * 64
$unknownRows = @($appliedRows) + [pscustomobject][ordered]@{ Version = 999999; Name = 'unknown'; Checksum = '0' * 64 }

# Answers only the runner's lock and history reads; any later command is recorded and refused. One fixture is used
# at a time, so the answers live in script state read by the fake command (as in ModuleDeployment).
$script:RunnerResponder = {
    param([string]$Mode, [string]$Sql, $Values)
    if ($Sql.Contains("N'Dawnholder.SchemaMigration'")) {
        if ($script:RunnerLockFails) {
            throw (New-TestSqlException -Number 51000 -Message 'Could not acquire migration lock.')
        }
        return -1
    }
    if ($Mode -ceq 'Scalar' -and $Sql.Contains('FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH')) {
        return $script:RunnerHistory
    }
    throw 'Offline fixture: the runner continued past the history check.'
}

function New-RunnerFixture {
    param(
        [Parameter(Mandatory)][object[]]$History,
        [string]$Database = $fixtureDatabase,
        [switch]$LockFails
    )
    $script:RunnerHistory = ConvertTo-Json -InputObject @($History) -Compress
    $script:RunnerLockFails = [bool]$LockFails
    return New-FakeSqlConnection -Responder $script:RunnerResponder -Database $Database
}

function Invoke-RunnerCase {
    param([Parameter(Mandatory)]$Connection, $Contract)
    $transaction = $Connection.BeginTransaction()
    $failure = $null
    try {
        $null = Invoke-Migrations -Connection $Connection -Transaction $transaction -Phase Complete -Contract $Contract
    } catch {
        $failure = $_.Exception
    }
    return [pscustomobject]@{
        Failure = $failure
        Commands = @($Connection.Log | Where-Object { $_.Mode -in @('Scalar', 'NonQuery') }).Count
        Transactions = (@($Connection.Log | Where-Object { $_.Mode -in @('Begin', 'Commit', 'Rollback', 'Dispose') } |
                    ForEach-Object Mode) -join ',')
    }
}

function Invoke-ExtractedAssertRejected {
    param([Parameter(Mandatory)][scriptblock]$Action, [Parameter(Mandatory)][string]$Pattern)
    # Runs the extracted definition in a child scope; returns its output and any rethrown error.
    return & {
        param($Definition, $Act, $Expected)
        . ([scriptblock]::Create($Definition))
        $output = @()
        $failure = $null
        try {
            $output = @(Assert-Rejected -Action $Act -Pattern $Expected -Label 'offline rejection case')
        } catch {
            $failure = $_.Exception
        }
        [pscustomobject]@{ Output = $output; Failure = $failure }
    } $assertRejected.Extent.Text $Action $Pattern
}

$control = Invoke-RunnerCase -Connection (New-RunnerFixture -History $appliedRows) -Contract $script:FixtureContract
Assert-True -Name 'fixture: the recomputed applied history passes the runner history check (control)' `
    -Condition ($control.Commands -gt 2) `
    -Detail ('commandsBeforeStop=' + $control.Commands + '; error=' +
        $(if ($control.Failure) { $control.Failure.Message }))

$drift = Invoke-RunnerCase -Connection (New-RunnerFixture -History $driftRows) -Contract $script:FixtureContract
$unknown = Invoke-RunnerCase -Connection (New-RunnerFixture -History $unknownRows) -Contract $script:FixtureContract
Assert-True -Name 'the runner leaves the caller transaction open on both history rejections' `
    -Condition ($null -ne $drift.Failure -and $null -ne $unknown.Failure -and
        $drift.Transactions -ceq 'Begin' -and $unknown.Transactions -ceq 'Begin' -and
        $drift.Commands -eq 2 -and $unknown.Commands -eq 2) `
    -Detail ('drift=' + $drift.Transactions + '/' + $drift.Commands + '; unknown=' + $unknown.Transactions + '/' +
        $unknown.Commands)
$driftMessage = $(if ($drift.Failure) { $drift.Failure.Message } else { '' })
$unknownMessage = $(if ($unknown.Failure) { $unknown.Failure.Message } else { '' })

# Each case is an action whose rejection the Test-Database check may see; only its own runner throw may pass.
$script:Cases = [ordered]@{
    ChecksumDrift = {
        Invoke-Migrations -Connection (New-RunnerFixture -History $driftRows) `
            -Transaction ([pscustomobject]@{ Connection = 'caller' }) -Phase Complete -Contract $script:FixtureContract
    }
    UnknownVersion = {
        Invoke-Migrations -Connection (New-RunnerFixture -History $unknownRows) `
            -Transaction ([pscustomobject]@{ Connection = 'caller' }) -Phase Complete -Contract $script:FixtureContract
    }
    ContractTargetMismatch = {
        Invoke-Migrations -Connection (New-RunnerFixture -History $driftRows -Database 'Dawnholder_Dev_Other') `
            -Transaction ([pscustomobject]@{ Connection = 'caller' }) -Phase Complete -Contract $script:FixtureContract
    }
    ContractMissing = {
        Invoke-Migrations -Connection (New-RunnerFixture -History $driftRows) `
            -Transaction ([pscustomobject]@{ Connection = 'caller' }) -Phase Complete -Contract $null
    }
    LockSqlFailure = {
        Invoke-Migrations -Connection (New-RunnerFixture -History $driftRows -LockFails) `
            -Transaction ([pscustomobject]@{ Connection = 'caller' }) -Phase Complete -Contract $script:FixtureContract
    }
    DriftWithAppendedText = { throw ($driftMessage + ' Provider text FAKE-SENTINEL-TDB') }
    DriftWithPrefix = { throw ('Wrapped: ' + $driftMessage) }
    UnknownWithAppendedText = { throw ($unknownMessage + ' Provider text FAKE-SENTINEL-TDB') }
    UnknownWithPrefix = { throw ('Wrapped: ' + $unknownMessage) }
    PreviousPatternText = { throw 'checksum mismatch; unknown migrations' }
}
$matrix = foreach ($check in $rejectionChecks) {
    foreach ($case in $script:Cases.Keys) {
        $result = $(if ($null -ne $check.Pattern -and $null -ne $assertRejected) {
                Invoke-ExtractedAssertRejected -Action $script:Cases[$case] -Pattern $check.Pattern
            } else {
                [pscustomobject]@{ Output = @(); Failure = [Exception]::new('pattern or definition missing') }
            })
        [pscustomobject]@{
            Check = $check.Stimulus
            Case = $case
            Accepted = $null -eq $result.Failure -and ($result.Output -join '') -ceq 'PASS: offline rejection case'
            Error = $(if ($result.Failure) { $result.Failure.Message } else { '' })
        }
    }
}
$matrix = @($matrix)
$ownAccepted = @($matrix | Where-Object { $_.Check -ceq $_.Case -and $_.Accepted }).Count
$othersAccepted = @($matrix | Where-Object { $_.Check -cne $_.Case -and $_.Accepted })
Assert-True -Name 'each pattern accepts the real runner rejection that follows its own stimulus' `
    -Condition ($driftMessage -and $unknownMessage -and $ownAccepted -eq 2) `
    -Detail ('drift=<' + $driftMessage + '>; unknown=<' + $unknownMessage + '>; ' + (@($matrix | Where-Object {
                    $_.Check -ceq $_.Case
                } | ForEach-Object { $_.Check + ':' + $_.Accepted + ':' + $_.Error }) -join ' | '))
Assert-True -Name 'no pattern accepts the other rejection, Contract gate, lock SQL failure or extended/old text' `
    -Condition ($matrix.Count -eq 2 * $script:Cases.Count -and $othersAccepted.Count -eq 0) `
    -Detail ('accepted=' + (@($othersAccepted | ForEach-Object { $_.Check + '<-' + $_.Case }) -join ',') +
        '; rethrown=' + (@($matrix | Where-Object { -not $_.Accepted } | ForEach-Object {
                    $_.Check + '<-' + $_.Case
                }) -join ','))

# ---- 3. The real Test-Database.ps1 file up to its first SqlConnection, in a separate Windows PowerShell process.
# Fail-closed inspectability: every way the entry and the files it dot-sources could construct a SqlConnection must be
# one the harness blocks (unqualified New-Object, or a body inside a function the harness replaces). Module-qualified
# New-Object, Activator, Add-Type, Invoke-Expression or an unknown dynamic call stop the runs before the product runs.
$blockedFunctions = @(
    'Invoke-DbScalar', 'Invoke-DbNonQuery', 'Invoke-Migrations', 'Assert-DatabaseOwner', 'Invoke-DatabaseSql',
    'Open-TestEnvironmentDatabase', 'Start-Process', 'Invoke-Expression', 'Invoke-Command', 'Add-Type',
    'Get-LocalUser', 'New-LocalUser', 'Remove-LocalUser', 'Get-Acl', 'Set-Acl'
)
function Get-DotSourcedFiles {
    param([Parameter(Mandatory)][string]$Entry)
    $files = New-Object 'Collections.Generic.List[string]'
    $pending = New-Object 'Collections.Generic.Queue[string]'
    $pending.Enqueue([IO.Path]::GetFullPath($Entry))
    $issues = @()
    while ($pending.Count -gt 0) {
        $file = $pending.Dequeue()
        if ($files.Contains($file)) {
            continue
        }
        $files.Add($file)
        $tokens = $null
        $errors = $null
        $fileAst = [Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$errors)
        $commands = @($fileAst.FindAll({ param($n) $n -is [Management.Automation.Language.CommandAst] }, $true))
        foreach ($command in $commands) {
            if ($command.InvocationOperator -ne 'Dot') {
                continue
            }
            $target = $command.CommandElements[0]
            $join = $null
            if ($target -is [Management.Automation.Language.ParenExpressionAst]) {
                $join = @($target.Pipeline.PipelineElements)[0]
            }
            $resolved = $false
            if ($join -is [Management.Automation.Language.CommandAst] -and $join.GetCommandName() -ceq 'Join-Path') {
                $parts = @($join.CommandElements | Select-Object -Skip 1)
                if ($parts.Count -eq 2 -and (Test-VariableArgument -Argument $parts[0] -Name 'PSScriptRoot') -and
                    $parts[1] -is [Management.Automation.Language.StringConstantExpressionAst]) {
                    $pending.Enqueue([IO.Path]::GetFullPath((Join-Path (Split-Path $file) $parts[1].Value)))
                    $resolved = $true
                }
            }
            if (-not $resolved) {
                $issues += 'unresolved dot-source ' + $file + ':' + $command.Extent.StartLineNumber
            }
        }
    }
    return [pscustomobject]@{ Files = @($files.ToArray()); Issues = $issues }
}

function Test-EntryInspectable {
    param([Parameter(Mandatory)][string]$Entry)
    $set = Get-DotSourcedFiles -Entry $Entry
    $issues = @($set.Issues)
    $sites = @()
    foreach ($file in $set.Files) {
        $tokens = $null
        $errors = $null
        $fileAst = [Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$errors)
        $name = Split-Path $file -Leaf
        if ($errors.Count) {
            $issues += 'parse errors in ' + $name
        }
        foreach ($node in @($fileAst.FindAll({ param($n) $true }, $true))) {
            $function = $node
            while ($null -ne $function -and $function -isnot [Management.Automation.Language.FunctionDefinitionAst]) {
                $function = $function.Parent
            }
            $owner = $(if ($null -ne $function) { $function.Name } else { '<top>' })
            $where = $name + ':' + $node.Extent.StartLineNumber + ' ' + $owner
            if ($node -is [Management.Automation.Language.InvokeMemberExpressionAst] -and $node.Static) {
                $typeName = $node.Expression.Extent.Text
                if ($typeName -match '(?i)Activator') {
                    $issues += 'Activator at ' + $where
                } elseif ($typeName -match '(?i)SqlConnection\]$' -and $node.Member.Extent.Text -ieq 'new') {
                    $sites += 'new at ' + $where
                    if ($owner -notin $blockedFunctions) {
                        $issues += 'direct SqlConnection::new outside a blocked function at ' + $where
                    }
                }
            }
            if ($node -is [Management.Automation.Language.CommandAst]) {
                $commandName = $node.GetCommandName()
                if ($null -eq $commandName) {
                    $head = $node.CommandElements[0]
                    $allowed = ($node.InvocationOperator -eq 'Dot' -and
                        $head -is [Management.Automation.Language.ParenExpressionAst]) -or
                        $head -is [Management.Automation.Language.ScriptBlockExpressionAst] -or
                        ((Test-VariableArgument -Argument $head -Name 'Action') -and $null -ne $function -and
                            @($function.Parameters |
                                    Where-Object { $_.Name.VariablePath.UserPath -ieq 'Action' }).Count)
                    if (-not $allowed) {
                        $issues += 'unknown dynamic invocation at ' + $where + ': ' + $node.Extent.Text
                    }
                } elseif ($commandName -match '\\') {
                    $issues += 'module-qualified command at ' + $where + ': ' + $commandName
                } elseif ($commandName -in @('Add-Type', 'Invoke-Expression', 'iex', 'Invoke-Command')) {
                    $issues += $commandName + ' at ' + $where
                } elseif ($commandName -ieq 'New-Object') {
                    $type = @($node.CommandElements | Select-Object -Skip 1 | Where-Object {
                            $_ -isnot [Management.Automation.Language.CommandParameterAst]
                        })[0]
                    if ($type -isnot [Management.Automation.Language.StringConstantExpressionAst]) {
                        $issues += 'New-Object with a non-constant type at ' + $where
                    } elseif ($type.Value -match '(?i)SqlConnection$') {
                        $sites += 'New-Object at ' + $where
                    }
                }
            }
        }
    }
    return [pscustomobject]@{
        Files = @($set.Files | ForEach-Object { Split-Path $_ -Leaf })
        Sites = $sites
        Issues = $issues
        Ready = $issues.Count -eq 0 -and @($sites | Where-Object { $_ -like 'New-Object at *' }).Count -gt 0
    }
}

$inspection = Test-EntryInspectable -Entry $testDatabasePath
Assert-True -Name 'entry boundary is inspectable: every SqlConnection construction is a form the harness blocks' `
    -Condition $inspection.Ready `
    -Detail ('files=' + ($inspection.Files -join ',') + '; sites=' + ($inspection.Sites -join ' | ') + '; issues=' +
        ($inspection.Issues -join ' | '))

$harnessSource = @'
param(
    [Parameter(Mandatory)][string]$CaseRoot,
    [Parameter(Mandatory)][string]$EntryPath
)
# Generated by TestDatabaseContract.Tests.ps1. Runs the real Test-Database.ps1 up to its first SqlConnection.
# New-Object is shadowed: a SqlConnection is recorded and refused before it exists, a SqlConnectionStringBuilder
# (text only, it opens nothing) is created by the qualified cmdlet, and every other type is refused. SQL, identity,
# ACL and process functions are refused. Exit 3: a shadow did not resolve, so the product was not started.
$ErrorActionPreference = 'Stop'
$request = [IO.File]::ReadAllText((Join-Path $CaseRoot 'request.json')) | ConvertFrom-Json
$global:HarnessEventsPath = Join-Path $CaseRoot 'events.jsonl'
$global:HarnessSentinel = 'Offline harness stopped before creating the first SqlConnection.'

function global:Write-HarnessEvent {
    param([string]$Name, [string]$Detail = '')
    $line = (ConvertTo-Json -InputObject ([pscustomobject]@{ Name = $Name; Detail = $Detail }) -Compress) + "`n"
    [IO.File]::AppendAllText($global:HarnessEventsPath, $line)
}

function global:New-HarnessObject {
    param(
        [Parameter(Position = 0)][string]$TypeName,
        [Parameter(Position = 1)][object[]]$ArgumentList
    )
    $type = $TypeName -as [type]
    if ($type -eq [Data.SqlClient.SqlConnection]) {
        $builder = [Data.SqlClient.SqlConnectionStringBuilder]::new([string]@($ArgumentList)[0])
        Write-HarnessEvent -Name 'SqlConnection' -Detail ($builder['Data Source'] + '|' + $builder['Initial Catalog'])
        throw $global:HarnessSentinel
    }
    $noArguments = $null -eq $ArgumentList -or $ArgumentList.Count -eq 0
    if ($type -eq [Data.SqlClient.SqlConnectionStringBuilder] -and $noArguments) {
        return Microsoft.PowerShell.Utility\New-Object -TypeName $TypeName
    }
    Write-HarnessEvent -Name 'Blocked.New-Object' -Detail $TypeName
    throw ('Offline harness refused New-Object ' + $TypeName)
}

function global:Stop-HarnessBoundary {
    Write-HarnessEvent -Name ('Blocked.' + $MyInvocation.InvocationName)
    throw ('Offline harness blocked ' + $MyInvocation.InvocationName + '; it must not run.')
}

$shadow = [ordered]@{ 'New-Object' = 'New-HarnessObject' }
foreach ($name in $request.Blocked) {
    $shadow[$name] = 'Stop-HarnessBoundary'
}
foreach ($name in $shadow.Keys) {
    Set-Alias -Scope Global -Name $name -Value $shadow[$name]
}
# Check the shadowing with the product definitions loaded the way the entry loads them.
$toolRoot = Split-Path $EntryPath
$resolved = @(& {
        . (Join-Path $toolRoot 'test-environment/Environment.Common.ps1')
        . (Join-Path $toolRoot 'Database.Common.ps1')
        foreach ($name in $shadow.Keys) {
            $command = Get-Command -Name $name -ErrorAction SilentlyContinue
            [pscustomobject]@{
                Name = $name
                CommandType = $(if ($null -eq $command) { 'Missing' } else { [string]$command.CommandType })
                Target = $(if ($null -ne $command -and $command.CommandType -eq 'Alias') {
                        $command.ResolvedCommandName
                    } else {
                        ''
                    })
            }
        }
    })
[IO.File]::WriteAllText((Join-Path $CaseRoot 'boundary.json'), (ConvertTo-Json -InputObject $resolved -Depth 3))
if (@($resolved | Where-Object { $_.CommandType -cne 'Alias' -or $_.Target -cne $shadow[$_.Name] }).Count) {
    exit 3
}
foreach ($property in @($request.Environment.PSObject.Properties)) {
    [Environment]::SetEnvironmentVariable($property.Name, [string]$property.Value, 'Process')
}
$arguments = @{}
foreach ($property in @($request.Arguments.PSObject.Properties)) {
    $arguments[$property.Name] = [string]$property.Value
}
# The entry starts with the preferences a -File run has; only the harness itself stops on errors. Output is kept line
# by line so the lines written before the stop at the first SqlConnection are not lost with the throw. The list uses
# ::new() because New-Object is the shadowed boundary here.
$ErrorActionPreference = 'Continue'
$output = [Collections.Generic.List[string]]::new()
$message = $null
try {
    & $EntryPath @arguments | ForEach-Object { $output.Add([string]$_) }
} catch {
    $message = $_.Exception.Message
}
$ErrorActionPreference = 'Stop'
$outcome = [pscustomobject]@{ Threw = $null -ne $message; Message = [string]$message; Output = @($output.ToArray()) }
[IO.File]::WriteAllText((Join-Path $CaseRoot 'outcome.json'), (ConvertTo-Json -InputObject $outcome -Depth 4))
exit 0
'@
$harnessPath = Join-Path $script:SuiteRoot 'test-database-entry-harness.ps1'
Write-FixtureText -Path $harnessPath -Text $harnessSource
$harnessSentinel = 'Offline harness stopped before creating the first SqlConnection.'

function New-PlanFile {
    param([Parameter(Mandatory)][string]$Root, [bool]$Approved, [string]$G2)
    # Offline sample of the reviewed plan shape (as in EnvironmentGuards/Lifecycle). It is not an approval record;
    # every path stays below this case root.
    $plan = [pscustomobject][ordered]@{
        PlanVersion = 1
        SchemaVersion = 1
        ExecutionApproved = $Approved
        Goal = 'offline-fixture'
        GoalMarker = 'offline-fixture-marker'
        G0 = 'fixture-g0'
        G1 = 'fixture-g1'
        G2 = $G2
        Machine = 'FIXTUREHOST'
        Instance = $fixtureInstance
        InstanceName = 'FIXTURE'
        Endpoint = 'tcp:127.0.0.1,14330'
        Database = $fixtureDatabase
        SlotId = 1
        AccountId = '11111111-1111-1111-1111-111111111111'
        CharacterId = '22222222-2222-2222-2222-222222222222'
        RuntimeLogin = 'dh_fixture_runtime'
        RecoveryPrincipal = 'FIXTUREHOST\dhrecovery'
        RecoveryLocalName = 'dhrecovery'
        ExecutorSid = 'S-1-5-21-1-2-3-1001'
        Encrypt = $true
        TrustServerCertificate = $true
        ManifestPath = Join-Path $Root 'lifecycle\manifest.json'
        SettlementPath = Join-Path $Root 'lifecycle\settlement.json'
        PrivateDirectory = Join-Path $Root 'private'
        IdentityDirectory = Join-Path $Root 'identity'
        IdentityPath = Join-Path $Root 'identity\identity.json'
        RuntimeCredentialPath = Join-Path $Root 'private\runtime.cred'
        RecoveryCredentialPath = Join-Path $Root 'private\recovery.cred'
    }
    $path = Join-Path $Root 'reviewed-plan.json'
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes(($plan | ConvertTo-Json -Depth 5))
    [IO.File]::WriteAllBytes($path, $bytes)
    return [pscustomobject]@{ Path = $path; Hash = Get-Sha256Hex -Bytes $bytes }
}

$script:EntryRuns = New-Object 'Collections.Generic.List[object]'
function Invoke-EntryCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [ValidateSet('Approved', 'Draft', 'ApprovedWithoutG2')][string]$Plan = 'Approved',
        [Parameter(Mandatory)][scriptblock]$Arguments,
        [hashtable]$Environment = @{}
    )
    $caseRoot = Join-Path $script:SuiteRoot ('entry-' + $Name)
    [void][IO.Directory]::CreateDirectory($caseRoot)
    $planFile = switch ($Plan) {
        'Approved' { New-PlanFile -Root $caseRoot -Approved $true -G2 'offline fixture G2; not an approval record' }
        'Draft' { New-PlanFile -Root $caseRoot -Approved $false -G2 '' }
        'ApprovedWithoutG2' { New-PlanFile -Root $caseRoot -Approved $true -G2 '' }
    }
    $entryArguments = & $Arguments $planFile
    $request = [pscustomobject]@{
        Arguments = [pscustomobject]$entryArguments
        Environment = [pscustomobject]$Environment
        Blocked = $blockedFunctions
    }
    Write-FixtureText -Path (Join-Path $caseRoot 'request.json') -Text (ConvertTo-Json -InputObject $request -Depth 4)
    $process = Invoke-PowerShellFile -File $harnessPath `
        -Arguments @('-CaseRoot', $caseRoot, '-EntryPath', $testDatabasePath)
    $outcomePath = Join-Path $caseRoot 'outcome.json'
    $eventsPath = Join-Path $caseRoot 'events.jsonl'
    $outcome = $(if ([IO.File]::Exists($outcomePath)) { Read-FixtureText -Path $outcomePath | ConvertFrom-Json })
    $events = @($(if ([IO.File]::Exists($eventsPath)) {
                [IO.File]::ReadAllLines($eventsPath) | Where-Object { $_ } | ForEach-Object { $_ | ConvertFrom-Json }
            }))
    $run = [pscustomobject]@{
        Name = $Name
        Exit = $process.ExitCode
        Completed = $process.ExitCode -eq 0 -and $null -ne $outcome
        Threw = $null -ne $outcome -and $outcome.Threw
        Message = $(if ($null -ne $outcome) { [string]$outcome.Message } else { '' })
        Output = @($(if ($null -ne $outcome) { $outcome.Output }))
        Connections = @($events | Where-Object Name -CEQ 'SqlConnection' | ForEach-Object Detail)
        Blocked = @($events | Where-Object { $_.Name -like 'Blocked.*' } | ForEach-Object Name)
        StdErr = $process.StdErr.Trim()
    }
    $script:EntryRuns.Add($run)
    return $run
}

function Format-EntryRun {
    param([Parameter(Mandatory)]$Run)
    return ('{0}: exit={1} threw={2} message=<{3}> connections={4} blocked={5} output={6} stderr=<{7}>' -f
        $Run.Name, $Run.Exit, $Run.Threw, $Run.Message, ($Run.Connections -join ','), ($Run.Blocked -join ','),
        ($Run.Output -join ' / '), $Run.StdErr)
}

# Expected causes are the product gates the requirement names (reader input/hash, exact target, execution approval);
# the message identifies which gate stopped the run so an unrelated earlier failure cannot pass a refusal case.
$planInputRefused = 'Supply the absolute nonsecret plan path and separately coordinator-reviewed SHA256.'
$planChanged = 'Approval plan changed after coordinator review.'
$targetRefused = 'Supply the explicit exact approved database and local instance; no fallback.'
$draftRefused = 'Draft plan cannot execute. Coordinator must review actual G2 approval and deliver its exact plan hash.'
$g2Missing = 'Execution needs the separately reviewed G2 record.'
$refusals = @(
    @{ Name = 'no-arguments'; Expected = $planInputRefused; Arguments = { param($p) @{} } },
    @{ Name = 'missing-plan-path'; Expected = $planInputRefused; Arguments = {
            param($p) @{ ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance; Database = $fixtureDatabase }
        }
    },
    @{ Name = 'relative-plan-path'; Expected = $planInputRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = 'reviewed-plan.json'; ExpectedApprovalPlanHash = $p.Hash
                Instance = $fixtureInstance; Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'blank-plan-path'; Expected = $planInputRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = '   '; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'missing-hash'; Expected = $planInputRefused; Arguments = {
            param($p) @{ ApprovalPlanPath = $p.Path; Instance = $fixtureInstance; Database = $fixtureDatabase }
        }
    },
    @{ Name = 'lowercase-hash'; Expected = $planInputRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash.ToLowerInvariant()
                Instance = $fixtureInstance; Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'other-hash'; Expected = $planChanged; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = ('A' * 64); Instance = $fixtureInstance
                Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'draft-plan'; Plan = 'Draft'; Expected = $draftRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'approved-without-g2'; Plan = 'ApprovedWithoutG2'; Expected = $g2Missing; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'missing-instance'; Expected = $targetRefused; Arguments = {
            param($p) @{ ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Database = $fixtureDatabase }
        }
    },
    @{ Name = 'blank-instance'; Expected = $targetRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = '  '
                Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'other-instance'; Expected = $targetRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = '.\OTHER'
                Database = $fixtureDatabase
            }
        }
    },
    @{ Name = 'missing-database'; Expected = $targetRefused; Arguments = {
            param($p) @{ ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance }
        }
    },
    @{ Name = 'other-database'; Expected = $targetRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = 'Dawnholder_Dev_Other'
            }
        }
    },
    @{ Name = 'default-game-database'; Expected = $targetRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = 'Dawnholder_Dev'
            }
        }
    },
    @{ Name = 'database-case-differs'; Expected = $targetRefused; Arguments = {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = $fixtureDatabase.ToLowerInvariant()
            }
        }
    },
    @{ Name = 'environment-target-only'; Expected = $targetRefused
        Environment = @{ DAWNHOLDER_SQL_INSTANCE = $fixtureInstance; DAWNHOLDER_SQL_DATABASE = $fixtureDatabase }
        Arguments = { param($p) @{ ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash } }
    },
    # One explicit value with the other only in the environment: a fallback would complete the exact target.
    @{ Name = 'instance-from-environment-only'; Expected = $targetRefused
        Environment = @{ DAWNHOLDER_SQL_INSTANCE = $fixtureInstance }
        Arguments = {
            param($p) @{ ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Database = $fixtureDatabase }
        }
    },
    @{ Name = 'database-from-environment-only'; Expected = $targetRefused
        Environment = @{ DAWNHOLDER_SQL_DATABASE = $fixtureDatabase }
        Arguments = {
            param($p) @{ ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance }
        }
    }
)
if ($inspection.Ready) {
    foreach ($case in $refusals) {
        $plan = $(if ($case.ContainsKey('Plan')) { $case.Plan } else { 'Approved' })
        $environment = $(if ($case.ContainsKey('Environment')) { $case.Environment } else { @{} })
        $run = Invoke-EntryCase -Name $case.Name -Plan $plan -Arguments $case.Arguments -Environment $environment
        Assert-True -Name ("Test-Database refuses $($case.Name) before any SqlConnection, at the expected gate") `
            -Condition ($run.Completed -and $run.Threw -and $run.Connections.Count -eq 0 -and
                $run.Blocked.Count -eq 0 -and $run.Output.Count -eq 0 -and
                [string]::Equals($run.Message, $case.Expected, [StringComparison]::Ordinal)) `
            -Detail (Format-EntryRun -Run $run)
    }

    $exactTarget = 'lpc:' + $fixtureInstance + '|' + $fixtureDatabase
    $preConnectionRefusals = @(
        'PASS: remote target rejected before connection',
        'PASS: existing unrelated database rejected before connection'
    )
    $approvedCases = @(
        @{ Name = 'approved-exact-input'; Environment = @{} },
        @{ Name = 'approved-input-with-other-environment'
            Environment = @{ DAWNHOLDER_SQL_INSTANCE = '.\OTHER'; DAWNHOLDER_SQL_DATABASE = 'Dawnholder_Dev_Other' }
        }
    )
    foreach ($case in $approvedCases) {
        $run = Invoke-EntryCase -Name $case.Name -Environment $case.Environment -Arguments {
            param($p) @{
                ApprovalPlanPath = $p.Path; ExpectedApprovalPlanHash = $p.Hash; Instance = $fixtureInstance
                Database = $fixtureDatabase
            }
        }
        Assert-True -Name ("$($case.Name) reaches the first SqlConnection only for the exact instance and database") `
            -Condition ($run.Completed -and $run.Threw -and $run.Message -ceq $harnessSentinel -and
                $run.Connections.Count -eq 1 -and $run.Connections[0] -ceq $exactTarget -and
                $run.Blocked.Count -eq 0 -and
                ($run.Output -join '|') -ceq ($preConnectionRefusals -join '|')) `
            -Detail (Format-EntryRun -Run $run)
    }
}
$unshadowed = @($script:EntryRuns | Where-Object { $_.Exit -ne 0 -or -not $_.Completed })
Assert-True -Name 'every entry run resolved its shadowed boundaries and completed in the harness' `
    -Condition ($inspection.Ready -and $script:EntryRuns.Count -eq ($refusals.Count + 2) -and $unshadowed.Count -eq 0) `
    -Detail ('runs=' + $script:EntryRuns.Count + '; failed=' + (@($unshadowed | ForEach-Object {
                    $_.Name + ':exit=' + $_.Exit + ':' + $_.StdErr
                }) -join ' | '))

Complete-TestSuite
