[CmdletBinding()]
param([Parameter(Mandatory)][string]$WorkRoot)
# Container tool boundaries in PS5.1. Every connection, token/ACL read and credential import is a recording fake.
# SQL responses are fixed fixtures, not computed from the plan or from product calculations.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'container-connection'
. (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')

function New-ConnectionContract {
    param([string]$Name)
    $root = Join-Path $script:SuiteRoot $Name
    $contract = New-OfflineContainerPlan -Root $root -Approved $true -G2 'offline-g2'
    $contract | Add-Member -NotePropertyName ApprovalPlanPath -NotePropertyValue (Join-Path $root 'plan.json')
    $contract | Add-Member -NotePropertyName ApprovalPlanHash -NotePropertyValue ('A' * 64)
    return $contract
}

function Invoke-ConnectionCase {
    param(
        [hashtable]$Changes = @{},
        [int]$OpenNumber = 0,
        [switch]$First,
        [switch]$NoCapture,
        [switch]$EngineDrift,
        [switch]$BadCredential
    )
    & {
        param($Changes, $OpenNumber, $First, $NoCapture, $EngineDrift, $BadCredential)
        . (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
        $contract = New-ConnectionContract -Name 'connection'
        $manifest = New-TestEnvironmentManifest -Contract $contract -Database $contract.Database
        $manifest.AdminCredentialHash = 'B' * 64
        if (-not $First) {
            $manifest.MasterFamilyGuid = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
            $manifest.Engine = [pscustomobject]@{
                ProductVersion = $(if ($EngineDrift) { '15.0.2000.1' } else { '16.0.1000.6' })
                ServerCollation = 'Latin1_General_CI_AS'
                OriginalLogin = 'sa'
            }
        }
        $row = [ordered]@{
            Machine = 'fixture-host'
            InstanceName = [DBNull]::Value
            ProductVersion = '16.0.1000.6'
            ServerCollation = 'Latin1_General_CI_AS'
            HostPlatform = 'Linux'
            MasterFamilyGuid = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
            IsSysadmin = 1
            OriginalLogin = 'sa'
        }
        foreach ($key in $Changes.Keys) { $row[$key] = $Changes[$key] }
        $context = [pscustomobject]@{
            Calls = [Collections.Generic.List[string]]::new()
            Row = $row
            OpenNumber = $OpenNumber
            BadCredential = $BadCredential
            Connection = $null
            Settings = $null
            CredentialName = ''
            ReadOnly = $false
            Writes = 0
            IdentitySql = ''
        }
        function New-ContainerFakeObject {
            param([string]$TypeName, [object[]]$ArgumentList)
            if ($TypeName -cne 'System.Data.SqlClient.SqlConnection') {
                throw 'Unexpected New-Object at the container connection boundary.'
            }
            $context.Calls.Add('Construct')
            $context.Settings = [Data.SqlClient.SqlConnectionStringBuilder]::new([string]$ArgumentList[0])
            $context.CredentialName = $ArgumentList[1].UserId
            $context.ReadOnly = $ArgumentList[1].Password.IsReadOnly()
            $fake = [pscustomobject]@{ Context = $context; Disposed = $false; Database = $context.Settings.InitialCatalog }
            $fake | Add-Member -MemberType ScriptMethod -Name Open -Value {
                $this.Context.Calls.Add('Open')
                if ($this.Context.OpenNumber) {
                    throw (New-TestSqlException -Number $this.Context.OpenNumber -Message 'SENTINEL provider text')
                }
            }
            $fake | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Disposed = $true }
            $context.Connection = $fake
            return $fake
        }
        function Test-ContainerFakeExecutor {
            param($Contract)
            $context.Calls.Add('Executor')
            Assert-TestEnvironmentExecutionApproval -Contract $Contract
        }
        function Test-ContainerFakeSecret {
            param($Contract, $Manifest, $Path, $ExpectedHash)
            $context.Calls.Add('Secret')
            if ($Path -cne $Contract.AdminCredentialPath -or $ExpectedHash -cne ('B' * 64)) {
                throw 'Fake credential identity mismatch.'
            }
        }
        function Import-ContainerFakeCredential {
            param([string]$LiteralPath)
            $context.Calls.Add('Import')
            if ($context.BadCredential) { throw 'SENTINEL secret import failure' }
            $secret = [Security.SecureString]::new()
            $secret.AppendChar('x')
            $secret.MakeReadOnly()
            return [Management.Automation.PSCredential]::new('sa', $secret)
        }
        function Invoke-ContainerFakeSql {
            param($Connection, [string]$Sql, [string]$Result)
            $context.Calls.Add('Identity')
            $context.IdentitySql = $Sql
            $table = [Data.DataTable]::new()
            foreach ($key in $context.Row.Keys) {
                $type = $(if ($key -ceq 'IsSysadmin') { [int] } else { [string] })
                [void]$table.Columns.Add($key, $type)
            }
            $dataRow = $table.NewRow()
            foreach ($key in $context.Row.Keys) { $dataRow[$key] = $context.Row[$key] }
            $table.Rows.Add($dataRow)
            return , $table
        }
        function Write-ContainerFakeManifest {
            param($Contract, $Manifest)
            $context.Calls.Add('Record')
            $context.Writes++
        }
        $shadows = @{
            'New-Object' = 'New-ContainerFakeObject'
            'Assert-TestEnvironmentExecutor' = 'Test-ContainerFakeExecutor'
            'Assert-TestEnvironmentSecretFile' = 'Test-ContainerFakeSecret'
            'Import-Clixml' = 'Import-ContainerFakeCredential'
            'Invoke-DatabaseSql' = 'Invoke-ContainerFakeSql'
            'Write-TestEnvironmentManifest' = 'Write-ContainerFakeManifest'
        }
        foreach ($name in $shadows.Keys) { Set-Alias -Name $name -Value $shadows[$name] -Scope Local }
        foreach ($name in $shadows.Keys) {
            if ((Get-Command $name).ResolvedCommandName -cne $shadows[$name]) {
                throw 'Container fake boundary did not resolve; product was not executed.'
            }
        }
        $failure = $null
        $connection = $null
        try {
            $arguments = @{
                Contract = $contract
                Manifest = $manifest
                Database = $contract.Database
                Master = [bool]$First
                RecordIdentity = [bool]($First -and -not $NoCapture)
            }
            $connection = Open-TestEnvironmentDatabase @arguments
        } catch {
            $failure = $_.Exception
        }
        [pscustomobject]@{ Context = $context; Failure = $failure; Manifest = $manifest; Connection = $connection }
    } $Changes $OpenNumber $First $NoCapture $EngineDrift $BadCredential
}

$first = Invoke-ConnectionCase -First
Assert-True -Name 'Create preflight accepts fixed Linux/default-instance/sysadmin identity and records master once' -Condition (
    $null -eq $first.Failure -and $first.Context.Writes -eq 1 -and
    $first.Manifest.MasterFamilyGuid -ceq 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee')
Assert-Equal -Name 'connection preflight order verifies executor and secret before construction and Open' -Expected (
    'Executor,Secret,Import,Construct,Open,Identity,Record'
) -Actual ($first.Context.Calls -join ',')
Assert-True -Name 'executed identity query selects master family_guid and never master database_guid or ServerName' -Condition (
    $first.Context.IdentitySql -cmatch '(?s)\bfamily_guid\b.*FROM sys.database_recovery_status\s+WHERE database_id = 1' -and
    $first.Context.IdentitySql -cnotmatch '\bdatabase_guid\b|@@SERVERNAME|SERVERPROPERTY\(''ServerName''\)' -and
    $first.Context.IdentitySql.Contains("SERVERPROPERTY('MachineName')") -and
    $first.Context.IdentitySql.Contains("SERVERPROPERTY('InstanceName')") -and
    $first.Context.IdentitySql.Contains('FROM sys.dm_os_host_info') -and
    $first.Context.IdentitySql.Contains("IS_SRVROLEMEMBER('sysadmin')")
)
$again = Invoke-ConnectionCase
Assert-True -Name 'a subsequent matching connection compares identity without rewriting it' -Condition (
    $null -eq $again.Failure -and $again.Context.Writes -eq 0)
if ($null -ne $first.Context.Settings) {
    $expected = [ordered]@{
        'Data Source' = 'tcp:127.0.0.1,14330'
        'Initial Catalog' = 'master'
        'Integrated Security' = 'False'
        'Encrypt' = 'True'
        'TrustServerCertificate' = 'True'
        'Persist Security Info' = 'False'
        'Pooling' = 'False'
        'Connect Timeout' = '7'
        'ConnectRetryCount' = '0'
    }
    foreach ($key in $expected.Keys) {
        Assert-Equal -Name ('container connection setting ' + $key) -Expected $expected[$key] -Actual (
            $first.Context.Settings[$key]
        )
    }
    Assert-True -Name 'SqlCredential has sa and a read-only SecureString; connection string has no user or password' -Condition (
        $first.Context.CredentialName -ceq 'sa' -and $first.Context.ReadOnly -and
        -not $first.Context.Settings.ShouldSerialize('Password') -and
        -not $first.Context.Settings.ShouldSerialize('User ID'))
}
foreach ($case in @(
        @{ Name = 'other hostname'; Changes = @{ Machine = 'other-host' } },
        @{ Name = 'hostname case differs'; Changes = @{ Machine = 'Fixture-Host' } },
        @{ Name = 'hostname whitespace'; Changes = @{ Machine = 'fixture-host ' } },
        @{ Name = 'SQL NULL hostname'; Changes = @{ Machine = [DBNull]::Value } },
        @{ Name = 'named instance'; Changes = @{ InstanceName = 'named' } },
        @{ Name = 'empty non-NULL instance'; Changes = @{ InstanceName = '' } },
        @{ Name = 'Windows platform'; Changes = @{ HostPlatform = 'Windows' } },
        @{ Name = 'non-sysadmin'; Changes = @{ IsSysadmin = 0 } },
        @{ Name = 'different admin'; Changes = @{ OriginalLogin = 'other' } },
        @{ Name = 'empty master guid'; Changes = @{ MasterFamilyGuid = '00000000-0000-0000-0000-000000000000' } },
        @{ Name = 'malformed master guid'; Changes = @{ MasterFamilyGuid = 'invalid' } },
        @{ Name = 'replaced master volume'; Changes = @{ MasterFamilyGuid = '11111111-2222-3333-4444-555555555555' } }
    )) {
    $run = Invoke-ConnectionCase -Changes $case.Changes
    Assert-True -Name ('connection rejects ' + $case.Name + ' without adoption and disposes connection') -Condition (
        $null -ne $run.Failure -and
        $run.Failure.Message -ceq 'Container identity mismatch; preserve resources without adoption.' -and
        $run.Context.Writes -eq 0 -and $run.Context.Connection.Disposed)
}
foreach ($case in @(
        @{ Name = 'version'; Changes = @{ ProductVersion = '17.0.1234.5' } },
        @{ Name = 'collation'; Changes = @{ ServerCollation = 'Other_CI_AS' } }
    )) {
    foreach ($isFirst in @($true, $false)) {
        $run = Invoke-ConnectionCase -Changes $case.Changes -First:$isFirst
        Assert-True -Name ("engine $($case.Name) mismatch rejected; first=$isFirst; observed value never adopted") -Condition (
            $null -ne $run.Failure -and $run.Failure.Message -ceq
            'Engine changed; a new golden-vector decision is required.' -and $run.Context.Writes -eq 0)
    }
}
$drift = Invoke-ConnectionCase -EngineDrift
Assert-True -Name 'matching plan values do not bypass the recorded-engine comparison' -Condition (
    $null -ne $drift.Failure -and $drift.Failure.Message -cmatch '^Engine changed')
$missing = Invoke-ConnectionCase -First -NoCapture
Assert-True -Name 'only explicit Create master preflight may establish a missing master identity' -Condition (
    $null -ne $missing.Failure -and $missing.Failure.Message -cmatch '^Container identity mismatch' -and
    $missing.Context.Writes -eq 0)
foreach ($number in @(18456, 10054, 64, -2)) {
    $run = Invoke-ConnectionCase -OpenNumber $number
    $prefix = $(if ($number -eq 18456) { '^Container login failed' } else { '^Container endpoint unreachable' })
    Assert-True -Name ("Open SQL $number is classified, preserved numerically, disposed, and never retried") -Condition (
        $null -ne $run.Failure -and $run.Failure.Message -cmatch $prefix -and
        $run.Failure.Data['DatabaseSqlNumber'] -eq $number -and -not $run.Failure.Message.Contains('SENTINEL') -and
        @($run.Context.Calls | Where-Object { $_ -ceq 'Open' }).Count -eq 1 -and $run.Context.Connection.Disposed)
    Assert-True -Name ("Open SQL $number remains classified in lifecycle summary") -Condition (
        (Get-TestEnvironmentFailureSummary -Exception $run.Failure) -cmatch $prefix)
}
$import = Invoke-ConnectionCase -BadCredential
Assert-True -Name 'DPAPI import failure stops as login failure before connection creation without leaking text' -Condition (
    $null -ne $import.Failure -and $import.Failure.Message -ceq
    'Container login failed; provider and credential text suppressed.' -and $null -eq $import.Context.Connection)

# Plan guards: legacy resources, finite approval values and names, all before any I/O or connection.
$plan = New-ConnectionContract -Name 'plan'
foreach ($case in @(
        @{ Name = 'old Windows plan'; Field = 'PlanVersion'; Value = 1; Reason = '^Unsupported approval-plan version' },
        @{ Name = 'old manifest schema'; Field = 'SchemaVersion'; Value = 1; Reason = '^Unsupported approval-plan version' },
        @{ Name = 'historical Windows database'; Field = 'Database'; Value = 'Dawnholder_Dev_D1b_20261002';
            Reason = '^Resource names must describe purpose' },
        @{ Name = 'game server port'; Field = 'Endpoint'; Value = 'tcp:127.0.0.1,7777'; Reason = '^Invalid explicit container' },
        @{ Name = 'zero connection timeout'; Field = 'ConnectTimeoutSeconds'; Value = 0; Reason = '^Cost and attempt' },
        @{ Name = 'fractional timeout'; Field = 'ReadyTimeoutSeconds'; Value = 1.5; Reason = '^Cost and attempt' },
        @{ Name = 'short stop grace'; Field = 'StopTimeoutSeconds'; Value = 29; Reason = '^Approved cost or attempt' },
        @{ Name = 'fourth attempt'; Field = 'Attempt'; Value = 4; Reason = '^Approved cost or attempt' },
        @{ Name = 'less than memory floor'; Field = 'MinFreeMemoryMb'; Value = 2047; Reason = '^Approved cost or attempt' },
        @{ Name = 'ceiling below SQL memory'; Field = 'ContainerMemoryLimitMb'; Value = 1024;
            Reason = '^Approved cost or attempt' },
        @{ Name = 'mutable image tag'; Field = 'ImageDigest'; Value = 'latest'; Reason = '^Invalid approved image' },
        @{ Name = 'ProgramData'; Field = 'ManifestPath'; Value = 'C:\ProgramData\Dawnholder-test\manifest.json';
            Reason = '^Protected Windows lifecycle path' },
        @{ Name = 'old lifecycle journal'; Field = 'ManifestPath';
            Value = 'C:\evidence\2026-10-02-persistence-repository\fixture-manifest.json';
            Reason = '^Protected Windows lifecycle path' },
        @{ Name = 'old WSL access directory'; Field = 'PrivateDirectory';
            Value = (Join-Path $env:LOCALAPPDATA 'Dawnholder\MssqlWsl-SQLEXPRESS');
            Reason = '^Protected Windows lifecycle path' }
    )) {
    $mutated = $plan.PSObject.Copy()
    $mutated.($case.Field) = $case.Value
    Assert-Throws -Name ('plan rejects ' + $case.Name) -Pattern $case.Reason -Action {
        Assert-TestEnvironmentApprovalPlan -Plan $mutated
    }
}
$alternate = $plan.PSObject.Copy()
$alternate.ExpectedProductVersion = '19.2.3456.7'
$alternate.ExpectedCollation = 'Fixture_Other_CS_AS'
$alternate.ConnectTimeoutSeconds = 11
Assert-NoThrow -Name 'engine and timeout values belong to the plan rather than current deployment constants' -Action {
    Assert-TestEnvironmentApprovalPlan -Plan $alternate
}
$extra = $plan.PSObject.Copy()
$extra | Add-Member -NotePropertyName RecoveryLocalName -NotePropertyValue 'legacy'
Assert-Throws -Name 'plan rejects obsolete Windows resource fields even with version 2' -Pattern '^Unexpected approval-plan' -Action {
    Assert-TestEnvironmentApprovalPlan -Plan $extra
}
$oldManifest = New-TestEnvironmentManifest -Contract $plan -Database $plan.Database
$oldManifest.SchemaVersion = 1
Assert-Throws -Name 'v1 manifest is rejected before its old fields are examined' -Pattern '^Unsupported lifecycle manifest' -Action {
    Assert-TestEnvironmentManifest -Manifest $oldManifest -Database $plan.Database -ManifestPath $plan.ManifestPath -Contract $plan
}

# Every literal throw in product PS and the two fixed Open categories must have exactly one safe reason.
function Get-ConstantStopText {
    param($Statement)
    $pipeline = $Statement.Pipeline
    while ($pipeline -is [Management.Automation.Language.PipelineAst] -and
        @($pipeline.PipelineElements).Count -eq 1 -and
        $pipeline.PipelineElements[0] -is [Management.Automation.Language.CommandExpressionAst]) {
        $expression = $pipeline.PipelineElements[0].Expression
        if ($expression -is [Management.Automation.Language.StringConstantExpressionAst]) { return $expression.Value }
        if ($expression -isnot [Management.Automation.Language.ParenExpressionAst]) { break }
        $pipeline = $expression.Pipeline
    }
    return $null
}
$files = @(Get-ChildItem -LiteralPath $script:ToolRoot -File -Filter '*.ps1') +
    @(Get-ChildItem -LiteralPath (Join-Path $script:ToolRoot 'test-environment') -File -Filter '*.ps1')
$reasonSites = @()
$engineStopSites = @()
$forbidden = @()
$parseProblems = @()
$safe = @()
foreach ($file in $files) {
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
    $parseProblems += @($errors)
    foreach ($throw in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.ThrowStatementAst] }, $true)) {
        $constant = Get-ConstantStopText -Statement $throw
        if ($null -ne $constant) { $reasonSites += $constant }
        if ($constant -ceq 'Engine changed; a new golden-vector decision is required.') {
            $owner = $throw.Parent
            while ($null -ne $owner -and
                $owner -isnot [Management.Automation.Language.FunctionDefinitionAst]) {
                $owner = $owner.Parent
            }
            $engineStopSites += [pscustomobject]@{
                Function = $(if ($null -eq $owner) { '<script>' } else { $owner.Name })
                Location = $file.Name + ':' + $throw.Extent.StartLineNumber
            }
        }
    }
    foreach ($call in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.CommandAst] }, $true)) {
        if ($call.GetCommandName() -in @('docker', 'wsl', 'wsl.exe', 'Start-Process', 'New-LocalUser',
                'Remove-LocalUser', 'Get-LocalUser', 'Get-LocalGroupMember')) {
            $forbidden += $file.Name + ':' + $call.Extent.StartLineNumber
        }
    }
    if ($file.Name -ceq 'Environment.Common.ps1') {
        $list = $ast.Find({
                param($node)
                $node -is [Management.Automation.Language.AssignmentStatementAst] -and
                $node.Left.Extent.Text -ceq '$safeReasons'
            }, $true)
        # PS5.1 SafeGetValue rejects a newline-delimited @() statement block. Read its literal AST leaves;
        # each resulting value is also checked through the real classifier below.
        $safe = @($list.Right.FindAll({
                param($node)
                $node -is [Management.Automation.Language.StringConstantExpressionAst]
            }, $true) | ForEach-Object { $_.Value })
    }
}
$reasonSites += @('Container endpoint unreachable; no fallback or retry, provider text suppressed.',
    'Container login failed; provider and credential text suppressed.')
$expectedReasons = @($reasonSites | Sort-Object -Unique)
$missingReasons = @($expectedReasons | Where-Object { $_ -cnotin $safe })
$obsoleteReasons = @($safe | Where-Object { $_ -cnotin $expectedReasons })
Assert-True -Name 'AST exhaustive constant stop reasons match the safe list 1:1 without stale entries' -Condition (
    $parseProblems.Count -eq 0 -and $expectedReasons.Count -gt 0 -and $missingReasons.Count -eq 0 -and
    $obsoleteReasons.Count -eq 0 -and @($safe | Sort-Object -Unique).Count -eq $safe.Count) -Detail (
    'expected=' + $expectedReasons.Count + '; missing=' + ($missingReasons -join '|') +
    '; obsolete=' + ($obsoleteReasons -join '|')
)
$outsideEngineOwner = @($engineStopSites | Where-Object { $_.Function -cne 'Open-TestEnvironmentDatabase' })
$outsideEngineLocations = @($outsideEngineOwner | ForEach-Object { $_.Location })
Assert-True -Name 'AST engine-change throws belong only to Open-TestEnvironmentDatabase' -Condition (
    $parseProblems.Count -eq 0 -and $engineStopSites.Count -ge 1 -and $outsideEngineOwner.Count -eq 0
) -Detail (
    'sites=' + $engineStopSites.Count + '; outside owner=' + ($outsideEngineLocations -join '|') +
    '; engine comparison must be owned by Open-TestEnvironmentDatabase'
)
foreach ($reason in $expectedReasons) {
    Assert-Equal -Name ('safe literal remains classified: ' + $reason) -Expected $reason -Actual (
        Get-TestEnvironmentStopReason -Exception ([InvalidOperationException]::new($reason))
    )
}
Assert-True -Name 'product AST has no external container/process or Windows-account calls' -Condition (
    $parseProblems.Count -eq 0 -and $files.Count -gt 0 -and $forbidden.Count -eq 0
) -Detail ($forbidden -join '|')
Assert-True -Name 'obsolete WSL access tools are absent after their Restore route was documented' -Condition (
    -not [IO.File]::Exists((Join-Path $script:ToolRoot 'Configure-WslAccess.ps1')) -and
    -not [IO.File]::Exists((Join-Path $script:ToolRoot 'Test-WslAccess.ps1'))
)

# Admin preparation executes the real entry under file/token/ACL boundaries. Save is a recording stand-in;
# DPAPI serialization itself and real ACL installation remain integration-only.
function Invoke-AdminEntryCase {
    param([switch]$SaveFails, [switch]$AlreadyPrepared)
    & {
        param($SaveFails, $AlreadyPrepared)
        . (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
        $contract = New-ConnectionContract -Name 'admin-entry'
        $manifest = New-TestEnvironmentManifest -Contract $contract -Database $contract.Database
        if ($AlreadyPrepared) { $manifest.AdminCredentialHash = 'B' * 64 }
        $context = [pscustomobject]@{
            Contract = $contract
            Manifest = $manifest
            Calls = [Collections.Generic.List[string]]::new()
            SaveFails = $SaveFails
            SavedUser = ''
            ReadOnly = $false
        }
        function Read-AdminFakePlan { return $context.Contract }
        function Read-AdminFakeManifest { return $context.Manifest }
        function Test-AdminFakeExecutor {
            param($Contract)
            Assert-TestEnvironmentExecutionApproval -Contract $Contract
            $context.Calls.Add('Executor')
        }
        function Lock-AdminFakeManifest {
            $context.Calls.Add('Lock')
            $guard = [pscustomobject]@{ Context = $context }
            $guard | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Context.Calls.Add('Unlock') }
            return $guard
        }
        function Write-AdminFakeManifest { $context.Calls.Add('Journal') }
        function Test-AdminFakePath { return $false }
        function Set-AdminFakeAcl { $context.Calls.Add('Directory') }
        function Save-AdminFakeCredential {
            param($Kind, $Credential, $Manifest, $Contract)
            $context.Calls.Add('Save')
            $context.SavedUser = $Credential.UserName
            $context.ReadOnly = $Credential.Password.IsReadOnly()
            Start-TestEnvironmentStep -Manifest $Manifest -Contract $Contract -Name SaveAdminCredential -Plan @{}
            if ($context.SaveFails) { throw 'SENTINEL save failure' }
            $Manifest.AdminCredentialHash = 'B' * 64
            Complete-TestEnvironmentStep -Manifest $Manifest -Contract $Contract -Name SaveAdminCredential -Identity @{}
        }
        function Stop-AdminFakeBoundary { throw 'Unexpected connection/process boundary in admin preparation.' }
        $shadows = @{
            'Read-TestEnvironmentApprovalPlan' = 'Read-AdminFakePlan'
            'Read-TestEnvironmentManifest' = 'Read-AdminFakeManifest'
            'Assert-TestEnvironmentExecutor' = 'Test-AdminFakeExecutor'
            'Lock-TestEnvironmentManifest' = 'Lock-AdminFakeManifest'
            'Write-TestEnvironmentManifest' = 'Write-AdminFakeManifest'
            'Test-Path' = 'Test-AdminFakePath'
            'Set-TestEnvironmentDirectoryAcl' = 'Set-AdminFakeAcl'
            'Save-TestEnvironmentCredential' = 'Save-AdminFakeCredential'
            'Open-TestEnvironmentDatabase' = 'Stop-AdminFakeBoundary'
            'docker' = 'Stop-AdminFakeBoundary'
            'wsl' = 'Stop-AdminFakeBoundary'
            'Start-Process' = 'Stop-AdminFakeBoundary'
        }
        foreach ($name in $shadows.Keys) { Set-Alias -Name $name -Value $shadows[$name] -Scope Local }
        foreach ($name in $shadows.Keys) {
            if ((Get-Command $name).ResolvedCommandName -cne $shadows[$name]) {
                throw 'Admin fake boundary did not resolve; entry was not executed.'
            }
        }
        $failure = $null
        try {
            $entry = Join-Path $script:ToolRoot 'test-environment/New-TestAdminCredential.ps1'
            $arguments = @{
                ApprovalPlanPath = $contract.ApprovalPlanPath
                ExpectedApprovalPlanHash = $contract.ApprovalPlanHash
                Database = $contract.Database
                ManifestPath = $contract.ManifestPath
            }
            $null = & $entry @arguments
        } catch {
            $failure = $_.Exception
        }
        return [pscustomobject]@{ Context = $context; Failure = $failure }
    } $SaveFails $AlreadyPrepared
}
$prepared = Invoke-AdminEntryCase
Assert-True -Name 'real admin preparation entry journals private directory then saves a read-only sa credential once' -Condition (
    $null -eq $prepared.Failure -and $prepared.Context.SavedUser -ceq 'sa' -and $prepared.Context.ReadOnly -and
    $prepared.Context.Manifest.State -ceq 'Planned' -and $prepared.Context.Manifest.AdminCredentialHash -ceq ('B' * 64) -and
    ($prepared.Context.Manifest.Steps.Name -join ',') -ceq 'CreatePrivateDirectory,SaveAdminCredential' -and
    @($prepared.Context.Manifest.Steps | Where-Object Status -ne 'Done').Count -eq 0) -Detail (
    $(if ($null -ne $prepared.Failure) { $prepared.Failure.Message } else { $prepared.Context.Calls -join ',' })
)
$failed = Invoke-AdminEntryCase -SaveFails
Assert-True -Name 'admin preparation failure preserves the partial journal and never retries or leaks native text' -Condition (
    $null -ne $failed.Failure -and -not $failed.Failure.Message.Contains('SENTINEL') -and
    @($failed.Context.Calls | Where-Object { $_ -ceq 'Save' }).Count -eq 1 -and
    $failed.Context.Manifest.Steps[1].Status -ceq 'Failed' -and $failed.Context.Manifest.State -ceq 'Planned')
$repeat = Invoke-AdminEntryCase -AlreadyPrepared
Assert-True -Name 'admin credential entry refuses a prepared lifetime before any secret generation/save' -Condition (
    $null -ne $repeat.Failure -and $repeat.Failure.Message -cmatch 'one-time' -and
    @($repeat.Context.Calls | Where-Object { $_ -cin @('Directory', 'Save') }).Count -eq 0)



# Execute each entry up to its connection boundary. Classification must survive its outer catch.
function Invoke-EntryConnectionFailure {
    param([string]$Reason, [int]$SqlNumber, [string]$EntryFile)
    & {
        param($Reason, $SqlNumber, $EntryFile)
        . (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
        $contract = New-ConnectionContract -Name 'principal-entry'
        $manifest = New-TestEnvironmentManifest -Contract $contract -Database $contract.Database
        $manifest.State = 'Bound'
        $context = [pscustomobject]@{
            Contract = $contract
            Manifest = $manifest
            Reason = $Reason
            SqlNumber = $SqlNumber
            Calls = [Collections.Generic.List[string]]::new()
        }
        function Read-PrincipalFakePlan { return $context.Contract }
        function Read-PrincipalFakeManifest { return $context.Manifest }
        function Test-PrincipalFakeExecutor {
            param($Contract, [switch]$Administrator)
            if ($Administrator) { throw 'Unexpected elevation request.' }
            Assert-TestEnvironmentExecutionApproval -Contract $Contract
            $context.Calls.Add('Executor')
        }
        function Lock-PrincipalFakeManifest {
            $context.Calls.Add('Lock')
            $guard = [pscustomobject]@{ Context = $context }
            $guard | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Context.Calls.Add('Unlock') }
            return $guard
        }
        function Test-PrincipalFakePath { return $false }
        function Open-PrincipalFakeDatabase {
            $context.Calls.Add('Open')
            $failure = [InvalidOperationException]::new($context.Reason)
            if ($context.SqlNumber) { $failure.Data['DatabaseSqlNumber'] = [int]$context.SqlNumber }
            throw $failure
        }
        function Stop-PrincipalFakeWrite { throw 'Unexpected resource mutation after failed connection.' }
        $shadows = @{
            'Read-TestEnvironmentApprovalPlan' = 'Read-PrincipalFakePlan'
            'Read-TestEnvironmentManifest' = 'Read-PrincipalFakeManifest'
            'Assert-TestEnvironmentExecutor' = 'Test-PrincipalFakeExecutor'
            'Lock-TestEnvironmentManifest' = 'Lock-PrincipalFakeManifest'
            'Test-Path' = 'Test-PrincipalFakePath'
            'Open-TestEnvironmentDatabase' = 'Open-PrincipalFakeDatabase'
            'Write-TestEnvironmentManifest' = 'Stop-PrincipalFakeWrite'
            'Save-TestEnvironmentCredential' = 'Stop-PrincipalFakeWrite'
            'New-LocalUser' = 'Stop-PrincipalFakeWrite'
        }
        foreach ($name in $shadows.Keys) { Set-Alias -Name $name -Value $shadows[$name] -Scope Local }
        foreach ($name in $shadows.Keys) {
            if ((Get-Command $name).ResolvedCommandName -cne $shadows[$name]) {
                throw 'Principal fake boundary did not resolve; entry was not executed.'
            }
        }
        $failure = $null
        try {
            $entry = Join-Path (Join-Path $script:ToolRoot 'test-environment') $EntryFile
            $arguments = @{
                ApprovalPlanPath = $contract.ApprovalPlanPath
                ExpectedApprovalPlanHash = $contract.ApprovalPlanHash
                Database = $contract.Database
                ManifestPath = $contract.ManifestPath
            }
            $null = & $entry @arguments
        } catch {
            $failure = $_.Exception
        }
        return [pscustomobject]@{ Context = $context; Failure = $failure }
    } $Reason $SqlNumber $EntryFile
}
foreach ($entry in @(
        @{ File = 'Set-TestPrincipals.ps1'; Calls = 'Executor,Lock,Open,Unlock' },
        @{ File = 'Initialize-CharacterBinding.ps1'; Calls = 'Lock,Open,Unlock' }
    )) {
    foreach ($case in @(
        @{ Reason = 'Container endpoint unreachable; no fallback or retry, provider text suppressed.'; Number = 64 },
        @{ Reason = 'Container login failed; provider and credential text suppressed.'; Number = 18456 },
        @{ Reason = 'Container identity mismatch; preserve resources without adoption.'; Number = 0 },
        @{ Reason = 'Engine changed; a new golden-vector decision is required.'; Number = 0 }
        )) {
        $run = Invoke-EntryConnectionFailure -Reason $case.Reason -SqlNumber $case.Number -EntryFile $entry.File
        $numberText = $(if ($case.Number) { [string]$case.Number } else { 'unavailable' })
        $name = $entry.File + ' preserves connection category and SQL number: ' + $case.Reason
        Assert-True -Name $name -Condition (
            $null -ne $run.Failure -and $run.Failure.Message.Contains($case.Reason) -and
            $run.Failure.Message.Contains('SqlNumber=' + $numberText + '.') -and
            ($run.Context.Calls -join ',') -ceq $entry.Calls -and
            $run.Context.Manifest.State -ceq 'Bound' -and @($run.Context.Manifest.Steps).Count -eq 0
        ) -Detail ([string]$run.Failure)
    }
}


Complete-TestSuite
