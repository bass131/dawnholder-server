[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of test-environment input guards at their function and entry-point boundaries.
# Only a draft (ExecutionApproved=false) fixture plan is written; approved contracts exist in memory only.
# No lifecycle action that reads Windows identity, takes the journal lock or opens SQL is invoked.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'environment-guards'
. (Join-Path $script:ToolRoot 'test-environment/Environment.Common.ps1')
. (Join-Path $script:ToolRoot 'Database.Common.ps1')
Set-OfflineStubs

$fixtureRoot = $script:SuiteRoot
$database = 'Dawnholder_Dev_Fixture'

function New-PlanObject {
    $root = $fixtureRoot
    return [pscustomobject][ordered]@{
        PlanVersion = 1
        SchemaVersion = 1
        ExecutionApproved = $false
        Goal = 'offline-fixture'
        GoalMarker = 'offline-fixture-marker'
        G0 = 'fixture-g0'
        G1 = 'fixture-g1'
        G2 = ''
        Machine = 'FIXTUREHOST'
        Instance = '.\FIXTURE'
        InstanceName = 'FIXTURE'
        Endpoint = 'tcp:127.0.0.1,14330'
        Database = $database
        SlotId = 1
        AccountId = '11111111-1111-1111-1111-111111111111'
        CharacterId = '22222222-2222-2222-2222-222222222222'
        RuntimeLogin = 'dh_fixture_runtime'
        RecoveryPrincipal = 'FIXTUREHOST\dhrecovery'
        RecoveryLocalName = 'dhrecovery'
        ExecutorSid = 'S-1-5-21-1-2-3-1001'
        Encrypt = $true
        TrustServerCertificate = $true
        ManifestPath = Join-Path $root 'lifecycle\manifest.json'
        SettlementPath = Join-Path $root 'lifecycle\settlement.json'
        PrivateDirectory = Join-Path $root 'private'
        IdentityDirectory = Join-Path $root 'identity'
        IdentityPath = Join-Path $root 'identity\identity.json'
        RuntimeCredentialPath = Join-Path $root 'private\runtime.cred'
        RecoveryCredentialPath = Join-Path $root 'private\recovery.cred'
    }
}

function Write-PlanFile {
    param([Parameter(Mandatory)]$Plan, [Parameter(Mandatory)][string]$Name, [switch]$Bom)
    $path = Join-Path $fixtureRoot $Name
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes(($Plan | ConvertTo-Json -Depth 5))
    if ($Bom) { $bytes = [byte[]](@(0xEF, 0xBB, 0xBF) + $bytes) }
    [IO.File]::WriteAllBytes($path, $bytes)
    return [pscustomobject]@{ Path = $path; Hash = Get-Sha256Hex -Bytes $bytes }
}

function Get-ApprovedContract {
    # In-memory only: never written as a plan file and never passed to an executing entry point.
    $contract = New-PlanObject
    $contract.ExecutionApproved = $true
    $contract.G2 = 'fixture-g2-in-memory'
    $contract | Add-Member -MemberType NoteProperty -Name ApprovalPlanPath -Value (Join-Path $fixtureRoot 'draft-plan.json')
    $contract | Add-Member -MemberType NoteProperty -Name ApprovalPlanHash -Value ('A' * 64)
    return $contract
}

# ---- Approval plan input: absolute path, reviewed hash over the parsed bytes.
$draft = Write-PlanFile -Plan (New-PlanObject) -Name 'draft-plan.json'
$read = $null
Assert-NoThrow -Name 'draft plan with its reviewed hash is read' -Action {
    $script:read = Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $draft.Path -ExpectedApprovalPlanHash $draft.Hash
}
Assert-True -Name 'read plan records its own hash and stays draft' -Condition (
    $script:read.ApprovalPlanHash -ceq $draft.Hash -and $script:read.ExecutionApproved -eq $false)
Assert-Throws -Name 'relative plan path rejected' -Pattern '^Supply the absolute nonsecret plan path' -Action {
    Read-TestEnvironmentApprovalPlan -ApprovalPlanPath 'draft-plan.json' -ExpectedApprovalPlanHash $draft.Hash
}
Assert-Throws -Name 'lowercase reviewed hash rejected' -Pattern '^Supply the absolute nonsecret plan path' -Action {
    Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $draft.Path -ExpectedApprovalPlanHash $draft.Hash.ToLowerInvariant()
}
Assert-Throws -Name 'different reviewed hash rejected' -Pattern '^Approval plan changed after coordinator review' -Action {
    Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $draft.Path -ExpectedApprovalPlanHash ('B' * 64)
}
$edited = Write-PlanFile -Plan (New-PlanObject) -Name 'edited-plan.json'
[IO.File]::AppendAllText($edited.Path, ' ')
Assert-Throws -Name 'plan bytes changed after review rejected' -Pattern '^Approval plan changed after coordinator review' -Action {
    Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $edited.Path -ExpectedApprovalPlanHash $edited.Hash
}
$bom = Write-PlanFile -Plan (New-PlanObject) -Name 'bom-plan.json' -Bom
Assert-NoThrow -Name 'BOM-prefixed plan hashed as stored bytes and parsed' -Action {
    Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $bom.Path -ExpectedApprovalPlanHash $bom.Hash
}

# ---- Approval plan shape: exact local target, principals and separated paths.
$invalidTarget = '^Invalid explicit local target/principal'
foreach ($case in @(
    @{ Name = 'default game database name'; Property = 'Database'; Value = 'Dawnholder_Dev'; Pattern = $invalidTarget },
    @{ Name = 'unrelated database'; Property = 'Database'; Value = 'GameDB'; Pattern = $invalidTarget },
    @{ Name = 'instance not matching instance name'; Property = 'Instance'; Value = '.\OTHER'; Pattern = $invalidTarget },
    @{ Name = 'non-loopback endpoint'; Property = 'Endpoint'; Value = 'tcp:10.0.0.5,14330'; Pattern = $invalidTarget },
    @{ Name = 'endpoint port out of range'; Property = 'Endpoint'; Value = 'tcp:127.0.0.1,70000'; Pattern = $invalidTarget },
    @{ Name = 'recovery principal on another machine'; Property = 'RecoveryPrincipal'; Value = 'OTHERHOST\dhrecovery'; Pattern = $invalidTarget },
    @{ Name = 'runtime login equals recovery principal'; Property = 'RuntimeLogin'; Value = 'FIXTUREHOST\dhrecovery'; Pattern = $invalidTarget },
    @{ Name = 'malformed executor SID'; Property = 'ExecutorSid'; Value = 'S-1-x'; Pattern = $invalidTarget },
    @{ Name = 'empty binding GUID'; Property = 'AccountId'; Value = '00000000-0000-0000-0000-000000000000'; Pattern = '^Invalid fixed binding: AccountId' },
    @{ Name = 'plan version 2'; Property = 'PlanVersion'; Value = 2; Pattern = '^Unsupported approval-plan version' },
    @{ Name = 'encryption disabled'; Property = 'Encrypt'; Value = $false; Pattern = '^Unsupported approval-plan version' },
    @{ Name = 'slot 2'; Property = 'SlotId'; Value = 2; Pattern = '^Unsupported approval-plan version' },
    @{ Name = 'UNC manifest path'; Property = 'ManifestPath'; Value = '\\server\share\manifest.json'; Pattern = '^Approval paths must be explicit local absolute paths: ManifestPath' },
    @{ Name = 'non-normalized identity path'; Property = 'IdentityPath'; Value = (Join-Path $fixtureRoot 'identity\..\identity\identity.json');
        Pattern = '^Approval paths must be distinct, normalized' },
    @{ Name = 'settlement path equal to manifest path'; Property = 'SettlementPath'; Value = (Join-Path $fixtureRoot 'lifecycle\manifest.json');
        Pattern = '^Approval paths must be distinct, normalized' },
    @{ Name = 'credential outside private directory'; Property = 'RuntimeCredentialPath'; Value = (Join-Path $fixtureRoot 'lifecycle\runtime.cred');
        Pattern = '^Credential, child identity and settlement locations do not match' },
    @{ Name = 'drive root path'; Property = 'PrivateDirectory'; Value = 'C:\'; Pattern = '^Approval paths must be distinct, normalized' }
)) {
    $plan = New-PlanObject
    $plan.($case.Property) = $case.Value
    Assert-Throws -Name ("plan $($case.Name) rejected") -Pattern $case.Pattern -Action { Assert-TestEnvironmentApprovalPlan -Plan $plan }
}

$plan = New-PlanObject
$plan.PSObject.Properties.Remove('RuntimeLogin')
Assert-Throws -Name 'plan missing a required field rejected' -Pattern '^Missing approval-plan field: RuntimeLogin' `
    -Action { Assert-TestEnvironmentApprovalPlan -Plan $plan }

$plan = New-PlanObject
$plan.ManifestPath = Join-Path $fixtureRoot 'private\manifest.json'
$plan.SettlementPath = Join-Path $fixtureRoot 'private\settlement.json'
Assert-Throws -Name 'nonsecret lifecycle evidence inside the private directory rejected' `
    -Pattern '^Nonsecret lifecycle evidence must remain outside' -Action { Assert-TestEnvironmentApprovalPlan -Plan $plan }

$plan = New-PlanObject
$plan.IdentityDirectory = Join-Path $fixtureRoot 'private\identity'
$plan.IdentityPath = Join-Path $fixtureRoot 'private\identity\identity.json'
Assert-Throws -Name 'child identity directory nested in private directory rejected' `
    -Pattern '^Private and child directories must have separate ACL boundaries' -Action { Assert-TestEnvironmentApprovalPlan -Plan $plan }

foreach ($case in @(
    @{ Name = 'approved without G2'; G2 = '' },
    @{ Name = 'approved with G2 copied from G0'; G2 = 'fixture-g0' }
)) {
    $plan = New-PlanObject
    $plan.ExecutionApproved = $true
    $plan.G2 = $case.G2
    Assert-Throws -Name ("plan $($case.Name) rejected") -Pattern '^Execution needs the separately reviewed G2 record' `
        -Action { Assert-TestEnvironmentApprovalPlan -Plan $plan }
}

# ---- Execution approval and exact target/path.
Assert-Throws -Name 'draft contract cannot execute' -Pattern '^Draft plan cannot execute' `
    -Action { Assert-TestEnvironmentExecutionApproval -Contract $script:read }
Assert-NoThrow -Name 'in-memory approved contract with G2 passes the execution gate' `
    -Action { Assert-TestEnvironmentExecutionApproval -Contract (Get-ApprovedContract) }
Assert-NoThrow -Name 'exact database and instance accepted' `
    -Action { Assert-TestEnvironmentTarget -Contract $script:read -Database $database -Instance '.\FIXTURE' }
foreach ($case in @(
    @{ Name = 'database differing only by case'; Database = 'dawnholder_Dev_Fixture'; Instance = '' },
    @{ Name = 'another approved-looking database'; Database = 'Dawnholder_Dev_Other'; Instance = '' },
    @{ Name = 'other instance'; Database = $database; Instance = '.\SQLEXPRESS' },
    @{ Name = 'empty database'; Database = ''; Instance = '' }
)) {
    Assert-Throws -Name ("target $($case.Name) rejected") -Pattern '^Supply the explicit exact approved database' `
        -Action { Assert-TestEnvironmentTarget -Contract $script:read -Database $case.Database -Instance $case.Instance }
}
Assert-Throws -Name 'relative lifecycle path rejected' -Pattern '^Test environment path is not the exact approved absolute path' `
    -Action { Assert-TestEnvironmentPath -Path 'lifecycle\manifest.json' -Expected $script:read.ManifestPath }
Assert-Throws -Name 'other lifecycle path rejected' -Pattern '^Test environment path is not the exact approved absolute path' `
    -Action { Assert-TestEnvironmentPath -Path (Join-Path $fixtureRoot 'other.json') -Expected $script:read.ManifestPath }

# ---- Lifecycle manifest: SQL migration boundary per state, lifecycle schema stays 1.
$contract = Get-ApprovedContract
$migrations = @(Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $script:ToolRoot | ForEach-Object {
    [pscustomobject]@{ Version = $_.Version; Name = $_.Name; Checksum = $_.Checksum }
})
function Test-ManifestBoundary {
    param([string]$State, $Rows)
    $manifest = New-TestEnvironmentManifest -Contract $contract -Database $database
    $manifest.State = $State
    $manifest.MigrationManifest = @($Rows)
    Assert-TestEnvironmentManifest -Manifest $manifest -Database $database -ManifestPath $contract.ManifestPath -Contract $contract
}
Assert-NoThrow -Name 'manifest: Planned/no migrations, Baseline001/001, Installed/001-004 accepted' -Action {
    Test-ManifestBoundary -State 'Planned' -Rows @()
    Test-ManifestBoundary -State 'Baseline001' -Rows @($migrations[0])
    Test-ManifestBoundary -State 'Installed' -Rows $migrations
}
Assert-Equal -Name 'manifest lifecycle schema remains 1' -Expected 1 `
    -Actual (New-TestEnvironmentManifest -Contract $contract -Database $database).SchemaVersion
$boundary = '^Lifecycle state does not match the recorded SQL migration boundary'
$ordered = '^Recorded migrations must retain the reviewed ordered contiguous version/name contract'
$identity = '^Invalid recorded migration identity'
$oldName = [pscustomobject]@{ Version = 3; Name = '003_persistence_payload.sql'; Checksum = $migrations[2].Checksum }
$fifth = [pscustomobject]@{ Version = 5; Name = '005_future.sql'; Checksum = ('F' * 64) }
$lower = [pscustomobject]@{ Version = 1; Name = $migrations[0].Name; Checksum = $migrations[0].Checksum.ToLowerInvariant() }
foreach ($case in @(
    @{ Name = 'Installed with three migrations'; State = 'Installed'; Rows = @($migrations[0..2]); Pattern = $boundary },
    @{ Name = 'Bound with one migration'; State = 'Bound'; Rows = @($migrations[0]); Pattern = $boundary },
    @{ Name = 'Baseline001 with two migrations'; State = 'Baseline001'; Rows = @($migrations[0..1]); Pattern = $boundary },
    @{ Name = 'hole 001,003'; State = 'Created'; Rows = @($migrations[0], $migrations[2]); Pattern = $ordered },
    @{ Name = 'previous 003 name'; State = 'Created'; Rows = @($migrations[0], $migrations[1], $oldName); Pattern = $ordered },
    @{ Name = 'fifth version'; State = 'Installed'; Rows = @($migrations) + $fifth; Pattern = $identity },
    @{ Name = 'duplicate version'; State = 'Created'; Rows = @($migrations[0], $migrations[0]); Pattern = $identity },
    @{ Name = 'lowercase checksum'; State = 'Baseline001'; Rows = @($lower); Pattern = $identity }
)) {
    Assert-Throws -Name ("manifest $($case.Name) rejected") -Pattern $case.Pattern `
        -Action { Test-ManifestBoundary -State $case.State -Rows $case.Rows }
}
$manifest = New-TestEnvironmentManifest -Contract $contract -Database $database
$manifest.Database = 'Dawnholder_Dev_Other'
Assert-Throws -Name 'manifest identity differing from the approval plan rejected' -Pattern '^Lifecycle manifest differs from the independently supplied approval plan: Database' `
    -Action { Assert-TestEnvironmentManifest -Manifest $manifest -Database $database -ManifestPath $contract.ManifestPath -Contract $contract }
$manifest = New-TestEnvironmentManifest -Contract $contract -Database $database
$manifest.State = 'Recovered'
Assert-Throws -Name 'unknown lifecycle state rejected' -Pattern '^Invalid manifest version/slot/state type' `
    -Action { Assert-TestEnvironmentManifest -Manifest $manifest -Database $database -ManifestPath $contract.ManifestPath -Contract $contract }
Assert-Throws -Name 'creating a lifecycle manifest from a draft plan rejected' -Pattern '^Draft plan cannot execute' `
    -Action { New-TestEnvironmentManifest -Contract $script:read -Database $database }

# ---- Reparse points: the link itself is removed afterwards; its empty target is never recursively deleted.
$reparseRoot = Join-Path $fixtureRoot 'reparse'
$target = Join-Path $reparseRoot 'target'
$link = Join-Path $reparseRoot 'link'
[void][IO.Directory]::CreateDirectory($target)
$mklink = & cmd.exe /c mklink /J "$link" "$target" 2>&1
try {
    $isJunction = ((Get-Item -LiteralPath $link -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0
    Assert-True -Name 'fixture junction created inside the work root' -Condition $isJunction -Detail ([string]$mklink)
    Assert-Throws -Name 'path through a junction rejected' -Pattern '^Test environment paths must not traverse reparse points' `
        -Action { Assert-TestEnvironmentNoReparse -Path (Join-Path $link 'manifest.json') }
    Assert-NoThrow -Name 'same path through the real directory accepted (control)' `
        -Action { Assert-TestEnvironmentNoReparse -Path (Join-Path $target 'manifest.json') }
    Copy-Item -LiteralPath $draft.Path -Destination (Join-Path $target 'draft-plan.json')
    Assert-Throws -Name 'approval plan read through a junction rejected before hashing' `
        -Pattern '^Test environment paths must not traverse reparse points' `
        -Action { Read-TestEnvironmentApprovalPlan -ApprovalPlanPath (Join-Path $link 'draft-plan.json') -ExpectedApprovalPlanHash $draft.Hash }
}
finally {
    if ([IO.Directory]::Exists($link)) { [IO.Directory]::Delete($link) }
}
Assert-True -Name 'junction removed and its target preserved' `
    -Condition (-not [IO.Directory]::Exists($link) -and [IO.File]::Exists((Join-Path $target 'draft-plan.json')))

# ---- Installer core and entry points stop before any SQL/identity work.
$installMessage = Get-ThrownMessage -Action {
    # Dot-sourcing binds the installer's own $Database/$ManifestPath parameters in this child scope,
    # so the approved values arrive as differently named block parameters.
    & {
        param($ApprovedContract, $ApprovedDatabase)
        . (Join-Path $script:ToolRoot 'Install-Database.ps1')
        Invoke-TestEnvironmentInstall -Contract $ApprovedContract -Database $ApprovedDatabase `
            -ManifestPath $ApprovedContract.ManifestPath -Phase Complete -Connection $null -ManifestLock $null
    } $contract $database
}
Assert-True -Name 'installer core without the lifecycle manifest lock rejected' `
    -Condition ($installMessage -cmatch '^Installer core requires the lifecycle-owned manifest lock') -Detail ([string]$installMessage)

$installEntry = Invoke-PowerShellFile -File (Join-Path $script:ToolRoot 'Install-Database.ps1') -Arguments @('-Database', $database)
Assert-True -Name 'Install-Database entry without a reviewed plan rejected' -Condition (
    $installEntry.ExitCode -ne 0 -and ($installEntry.StdOut + $installEntry.StdErr) -cmatch 'Supply the absolute nonsecret plan path') `
    -Detail ('exit=' + $installEntry.ExitCode)
$installWrongDb = Invoke-PowerShellFile -File (Join-Path $script:ToolRoot 'Install-Database.ps1') `
    -Arguments @('-Database', 'Dawnholder_Dev_Other', '-ApprovalPlanPath', $draft.Path, '-ExpectedApprovalPlanHash', $draft.Hash)
Assert-True -Name 'Install-Database entry for another database rejected' -Condition (
    $installWrongDb.ExitCode -ne 0 -and ($installWrongDb.StdOut + $installWrongDb.StdErr) -cmatch 'Supply the explicit exact approved database') `
    -Detail ('exit=' + $installWrongDb.ExitCode)

$newDatabase = Join-Path $script:ToolRoot 'test-environment/New-TestDatabase.ps1'
$offline = Invoke-PowerShellFile -File $newDatabase -Arguments @('-Action', 'OfflinePlan', '-Database', $database,
    '-ManifestPath', $script:read.ManifestPath, '-ApprovalPlanPath', $draft.Path, '-ExpectedApprovalPlanHash', $draft.Hash)
Assert-True -Name 'New-TestDatabase OfflinePlan returns the contract without writing lifecycle files' -Condition (
    $offline.ExitCode -eq 0 -and $offline.StdOut -cmatch 'Dawnholder_Dev_Fixture' -and
    -not [IO.Directory]::Exists((Join-Path $fixtureRoot 'lifecycle'))) -Detail ('exit=' + $offline.ExitCode + '; ' + $offline.StdErr.Trim())
$offlineWrongPath = Invoke-PowerShellFile -File $newDatabase -Arguments @('-Action', 'OfflinePlan', '-Database', $database,
    '-ManifestPath', (Join-Path $fixtureRoot 'other.json'), '-ApprovalPlanPath', $draft.Path, '-ExpectedApprovalPlanHash', $draft.Hash)
Assert-True -Name 'New-TestDatabase OfflinePlan with another manifest path rejected' -Condition (
    $offlineWrongPath.ExitCode -ne 0 -and ($offlineWrongPath.StdOut + $offlineWrongPath.StdErr) -cmatch 'not the exact approved absolute path') `
    -Detail ('exit=' + $offlineWrongPath.ExitCode)

Complete-TestSuite
