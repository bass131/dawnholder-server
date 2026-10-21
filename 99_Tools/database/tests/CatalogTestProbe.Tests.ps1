[CmdletBinding()]
param([Parameter(Mandatory)][string]$WorkRoot)

# Run only under a separate offline execution contract; all files stay below the explicit WorkRoot.
# Real file entry is tested only for refusals before SQL; in-process orchestration uses verified I/O stand-ins.
if ($PSVersionTable.PSEdition -cne 'Desktop' -or $PSVersionTable.PSVersion.ToString() -notlike '5.1.*') {
    throw 'Run the separately approved offline suite in Windows PowerShell 5.1.'
}
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'catalog-test-probe'
$probeSource = Join-Path $script:ToolRoot 'test-environment/Set-CatalogTestProbe.ps1'
$importOutput = . $probeSource
. (Join-Path $script:ToolRoot 'SqlError.Common.ps1')
Assert-True -Name 'Dot-source import is inert and produces no output' -Condition ($null -eq $importOutput)

function New-ProbeFixturePlan {
    param([string]$Root)
    $created = [DateTimeOffset]::UtcNow.AddMinutes(-1)
    $sources = @('test-environment/Set-CatalogTestProbe.ps1', 'SqlError.Common.ps1',
        'migrations/005_character_catalog.sql', 'verify-schema.sql')
    return [pscustomobject][ordered]@{
        version = 1
        purpose = 'character-catalog-probe'
        documentState = 'DraftRequiresExplicitActionHashApproval'
        action = 'Apply'
        step = 'CreateCatalogProbe'
        createdUtc = $created.ToString('yyyy-MM-ddTHH:mm:ssZ')
        expiresUtc = $created.AddHours(24).ToString('yyyy-MM-ddTHH:mm:ssZ')
        machine = [Environment]::MachineName
        executorSid = 'S-1-5-21-1-2-3-1001'
        instance = [Environment]::MachineName + '\CATALOGFIXTURE'
        adminEndpoint = 'lpc:.\CATALOGFIXTURE'
        sqlMajor = 1
        database = 'Dawnholder_Char_0123456789ab'
        databaseGuid = '11223344-5566-7788-99AA-BBCCDDEEFF00'
        runtimeName = 'dh_char_0123456789ab'
        runtimeSid = '0x102030405060708090A0B0C0D0E0F001'
        schemaVersion = 5
        moduleRelease = 5
        moduleManifestChecksum = 'AC6D31EA3C28C21FD01D150F2828BD9AA9FCAF8384FEB9B9B2A171F4E718A571'
        probeName = 'dh_probe_0123456789ab'
        plannedSid = '0x112233445566778899AABBCCDDEEFF11'
        credentialPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) `
            'Dawnholder\0123456789abcdef0123456789abcdef\catalog-probe.clixml'
        evidenceDirectory = $Root
        receiptPath = Join-Path $Root 'probe-apply-receipt.json'
        journalPath = Join-Path $Root 'probe-apply-journal.jsonl'
        removeReceiptPath = Join-Path $Root 'probe-remove-receipt.json'
        removeJournalPath = Join-Path $Root 'probe-remove-journal.jsonl'
        serverPermissions = @('CONNECT SQL', 'VIEW SERVER PERFORMANCE STATE')
        databasePermissions = @('CONNECT', 'SELECT ON OBJECT::dh.Account')
        deleteCredential = $false
        originalReceiptHash = ''
        originalApprovalHash = ''
        credentialHash = ''
        sourceHashes = @($sources | ForEach-Object {
            [pscustomobject]@{ path = $_; sha256 = (Get-FileHash -LiteralPath (Join-Path $script:ToolRoot $_)).Hash }
        })
    }
}

function Save-ProbeFixtureJson {
    param($Value, [string]$Path)
    [IO.File]::WriteAllText($Path, (ConvertTo-Json -InputObject $Value -Depth 10), [Text.UTF8Encoding]::new($false))
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

$plan = New-ProbeFixturePlan -Root $script:SuiteRoot
Assert-NoThrow -Name 'Exact Apply scope admitted' -Action {
    Assert-CatalogProbeApproval -Plan $plan -Action Apply -Now ([DateTimeOffset]::UtcNow)
}
Assert-Throws -Name 'Action cannot reuse Apply document for Remove' -Pattern 'CatalogProbe:ActionScope' -Action {
    Assert-CatalogProbeApproval -Plan $plan -Action Remove -Now ([DateTimeOffset]::UtcNow)
}
$counterexamples = @(
    @{ Name = 'endpoint'; Field = 'adminEndpoint'; Value = 'tcp:127.0.0.1,1433'; Code = 'TargetScope' }
    @{ Name = 'remote instance'; Field = 'instance'; Value = 'REMOTE\CATALOGFIXTURE'; Code = 'TargetScope' }
    @{ Name = 'default instance'; Field = 'instance'; Value = $plan.machine; Code = 'TargetScope' }
    @{ Name = 'instance suffix'; Field = 'instance'; Value = ($plan.machine + '\BAD;SQL'); Code = 'TargetScope' }
    @{ Name = 'endpoint suffix mismatch'; Field = 'adminEndpoint'; Value = 'lpc:.\OTHER'; Code = 'TargetScope' }
    @{ Name = 'SQL major type'; Field = 'sqlMajor'; Value = '1'; Code = 'TargetScope' }
    @{ Name = 'SQL major range'; Field = 'sqlMajor'; Value = 0; Code = 'TargetScope' }
    @{ Name = 'database identifier'; Field = 'database'; Value = 'Dawnholder_Char_bad]'; Code = 'TargetScope' }
    @{ Name = 'runtime name relation'; Field = 'runtimeName'; Value = 'dh_char_ffffffffffff'; Code = 'TargetScope' }
    @{ Name = 'probe name relation'; Field = 'probeName'; Value = 'dh_probe_ffffffffffff'; Code = 'TargetScope' }
    @{ Name = 'runtime SID'; Field = 'runtimeSid'; Value = '0x112233445566778899AABBCCDDEEFF11'; Code = 'TargetScope' }
    @{ Name = 'zero runtime SID'; Field = 'runtimeSid'; Value = ('0x' + ('0' * 32)); Code = 'TargetScope' }
    @{ Name = 'runtime SID format'; Field = 'runtimeSid'; Value = '0xZZ'; Code = 'TargetScope' }
    @{ Name = 'database GUID'; Field = 'databaseGuid'; Value = [Guid]::Empty.ToString(); Code = 'TargetScope' }
    @{ Name = 'noncanonical GUID'; Field = 'databaseGuid'; Value = $plan.databaseGuid.ToLower(); Code = 'TargetScope' }
    @{ Name = 'GUID type'; Field = 'databaseGuid'; Value = [Guid]::Empty; Code = 'StringRequired' }
    @{ Name = 'executor SID overflow'; Field = 'executorSid'; Value = 'S-1-5-21-4294967296-2-3-1001'
        Code = 'TargetScope' }
    @{ Name = 'planned SID equals runtime'; Field = 'plannedSid'; Value = $plan.runtimeSid; Code = 'TargetScope' }
    @{ Name = 'zero planned SID'; Field = 'plannedSid'; Value = ('0x' + ('0' * 32)); Code = 'TargetScope' }
    @{ Name = 'expired'; Field = 'expiresUtc'; Value = '2000-01-01T00:00:00Z'; Code = 'ApprovalWindow' }
    @{ Name = 'implicit timezone'; Field = 'expiresUtc'; Value = '2099-01-01T00:00:00'; Code = 'UtcStringRequired' }
    @{ Name = 'converted DateTime'; Field = 'expiresUtc'; Value = [DateTime]::UtcNow; Code = 'UtcStringRequired' }
    @{ Name = 'wrong step'; Field = 'step'; Value = 'RemoveCatalogProbe'; Code = 'ActionStep' }
    @{
        Name = 'broad grant'; Field = 'serverPermissions'
        Value = @('CONNECT SQL', 'VIEW SERVER STATE'); Code = 'PermissionScope'
    }
    @{ Name = 'delete during Apply'; Field = 'deleteCredential'; Value = $true; Code = 'CredentialDeletionScope' }
    @{
        Name = 'credential overwrite target'; Field = 'credentialPath'
        Value = 'C:\runtime.clixml'; Code = 'CredentialScope'
    }
    @{
        Name = 'credential lifetime relation'; Field = 'credentialPath'
        Value = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) `
            'Dawnholder\ffffffffffffffffffffffffffffffff\catalog-probe.clixml'; Code = 'CredentialScope'
    }
    @{
        Name = 'credential filename'; Field = 'credentialPath'
        Value = Join-Path ([IO.Path]::GetDirectoryName($plan.credentialPath)) 'runtime.clixml'; Code = 'CredentialScope'
    }
)
foreach ($example in $counterexamples) {
    $bad = $plan.PSObject.Copy()
    $bad.($example.Field) = $example.Value
    Assert-Throws -Name ('Reject ' + $example.Name) -Pattern ('CatalogProbe:' + $example.Code) -Action {
        Assert-CatalogProbeApproval -Plan $bad -Action Apply -Now ([DateTimeOffset]::UtcNow)
    }
}
$tooLong = $plan.PSObject.Copy()
$tooLong.expiresUtc = ([DateTimeOffset]::Parse($plan.createdUtc)).AddHours(25).ToString('yyyy-MM-ddTHH:mm:ssZ')
Assert-Throws -Name 'Window cannot exceed 24 hours' -Pattern 'CatalogProbe:ApprovalWindow' -Action {
    Assert-CatalogProbeApproval -Plan $tooLong -Action Apply -Now ([DateTimeOffset]::UtcNow)
}
$extra = $plan.PSObject.Copy()
$extra | Add-Member -NotePropertyName password -NotePropertyValue 'fixture-only-sentinel'
Assert-Throws -Name 'Secret or unknown approval field rejected' -Pattern 'CatalogProbe:DocumentFields' -Action {
    Assert-CatalogProbeApproval -Plan $extra -Action Apply -Now ([DateTimeOffset]::UtcNow)
}
$publicPath = Join-Path $script:SuiteRoot 'time-fields.json'
$publicHash = Save-ProbeFixtureJson -Value $plan -Path $publicPath
$roundTrip = Read-CatalogProbePublicJson -Path $publicPath -ExpectedHash $publicHash
Assert-True -Name 'JSON UTC remains a string' -Condition ($roundTrip.createdUtc -is [string])
Assert-Equal -Name 'JSON UTC bytes retain approved text' -Expected $plan.createdUtc -Actual $roundTrip.createdUtc
Assert-Throws -Name 'Exact public hash required' -Pattern 'CatalogProbe:PublicDocumentHash' -Action {
    Read-CatalogProbePublicJson -Path $publicPath -ExpectedHash ('F' * 64)
}
$drifted = $plan.PSObject.Copy()
$drifted.sourceHashes = @($plan.sourceHashes | ForEach-Object { $_.PSObject.Copy() })
$drifted.sourceHashes[0].sha256 = 'F' * 64
Assert-Throws -Name 'Changed source stops before I/O' -Pattern 'CatalogProbe:SourceDrift' -Action {
    Assert-CatalogProbeSources -Plan $drifted -SourcePath $probeSource
}

# File execution redefines its own functions, so no inherited stand-in can safely authorize a success path.
# A deliberately different executor must be refused before SQL/credential I/O; no product test seam is added.
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
try {
    if ($identity.User.Value -ceq $plan.executorSid) { throw 'Fixture executor unexpectedly matches the real user.' }
    $currentExecutorSid = $identity.User.Value
}
finally { $identity.Dispose() }
$hostPlan = $plan.PSObject.Copy()
$hostPlan.executorSid = $currentExecutorSid
Assert-NoThrow -Name 'Real host accepts its current machine user and profile' -Action {
    Assert-CatalogProbeHost -Plan $hostPlan
}
$hostPlan.machine = $plan.machine + '-OTHER'
Assert-Throws -Name 'Real host refuses a different machine' -Pattern 'CatalogProbe:ExecutorIdentity' -Action {
    Assert-CatalogProbeHost -Plan $hostPlan
}
$hostPlan.machine = $plan.machine
$hostPlan.credentialPath = 'C:\runtime.clixml'
Assert-Throws -Name 'Real host refuses a credential outside its profile' `
    -Pattern 'CatalogProbe:ExecutorProfile' -Action {
    Assert-CatalogProbeHost -Plan $hostPlan
}
Assert-True -Name 'Private credential absent before offline run' -Condition (
    -not (Test-Path -LiteralPath $plan.credentialPath))
$global:LASTEXITCODE = 0
$entryOutput = & $probeSource -Action Apply -ApprovalPath $publicPath -ExpectedApprovalHash ('0' * 64)
$entryExit = $LASTEXITCODE
$entryResult = ConvertFrom-Json -InputObject ($entryOutput -join "`n")
Assert-Equal -Name 'Real file entry refuses wrong public hash' -Expected PublicDocumentHash `
    -Actual $entryResult.failure.category
Assert-Equal -Name 'Wrong hash file entry exits 1' -Expected 1 -Actual $entryExit
$global:LASTEXITCODE = 0
$entryOutput = & $probeSource -Action Apply -ApprovalPath $publicPath -ExpectedApprovalHash $publicHash
$entryExit = $LASTEXITCODE
$entryResult = ConvertFrom-Json -InputObject ($entryOutput -join "`n")
Assert-Equal -Name 'Real file entry preserves current executor guard' -Expected ExecutorIdentity `
    -Actual $entryResult.failure.category
Assert-Equal -Name 'Identity refusal exits 1' -Expected 1 -Actual $entryExit
Assert-Equal -Name 'Identity refusal never starts SQL' -Expected NotStarted -Actual $entryResult.sqlOutcome
Assert-True -Name 'Identity refusal writes no intent journal' -Condition (-not [IO.File]::Exists($plan.journalPath))

# Exercise the real typed-command boundary with an in-memory command/parameter collection, without Open.
$backingCommand = [Data.SqlClient.SqlCommand]::new()
$fakeCommand = [pscustomobject]@{
    Parameters = $backingCommand.Parameters
    CommandText = ''
    CommandTimeout = 0
    CapturedType = ''
    CapturedSize = 0
    Disposed = $false
    ThrowProviderText = $false
}
$fakeCommand | Add-Member -MemberType ScriptMethod -Name ExecuteScalar -Value {
    if ($this.ThrowProviderText) { throw 'fixture-provider-text Password=fixture-only-sentinel' }
    $this.CapturedType = $this.Parameters['@password'].SqlDbType.ToString()
    $this.CapturedSize = $this.Parameters['@password'].Size
    return 'typed'
}
$fakeCommand | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Disposed = $true }
$fakeConnection = [pscustomobject]@{ Command = $fakeCommand }
$fakeConnection | Add-Member -MemberType ScriptMethod -Name CreateCommand -Value { return $this.Command }
$typed = Invoke-CatalogProbeSql -Connection $fakeConnection -Sql 'SELECT @password' -Parameters @{
    password = @{ Type = 'NVarChar'; Size = 128; Value = 'fixture-only-sentinel' }
}
Assert-Equal -Name 'Password uses explicit NVarChar' -Expected NVarChar -Actual $fakeCommand.CapturedType
Assert-Equal -Name 'Password uses explicit size' -Expected 128 -Actual $fakeCommand.CapturedSize
Assert-Equal -Name 'Command parameters cleared' -Expected 0 -Actual $fakeCommand.Parameters.Count
Assert-True -Name 'Command disposed' -Condition $fakeCommand.Disposed
$fakeCommand.ThrowProviderText = $true
$safeFailure = Get-ThrownMessage -Action {
    Invoke-CatalogProbeSql -Connection $fakeConnection -Sql 'fixture' -Parameters @{}
}
Assert-True -Name 'Provider text suppressed' -Condition (
    $safeFailure -notmatch 'fixture-provider-text|fixture-only-sentinel' -and $safeFailure -match 'provider number 0')
$backingCommand.Dispose()

function Assert-OfflineProbeHost {
    param($Plan)
    $script:ProbeFixture.HostChecked = $true
}

function Assert-OfflineProbeAcl {
    param([string]$Path, [string]$ExecutorSid, [switch]$Directory)
    if ($ExecutorSid -cne 'S-1-5-21-1-2-3-1001') { throw 'Fixture SID mismatch.' }
    if ($script:ProbeFixture.Behavior -ceq 'AclRefused') { Stop-CatalogProbe -Code 'OwnerOrAcl' }
}

function Assert-OfflineProbePath {
    param([string]$Path)
    if ($script:ProbeFixture.Behavior -ceq 'ReparseRefused') { Stop-CatalogProbe -Code 'ReparsePath' }
}

function Open-OfflineProbeMaster {
    param($Plan)
    $script:ProbeFixture.OpenCount++
    $connection = [pscustomobject]@{ Closed = $false }
    $connection | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Closed = $true }
    return $connection
}

function Invoke-OfflineProbeSql {
    param($Connection, [string]$Sql, [hashtable]$Parameters)
    $state = $script:ProbeFixture
    if ($Sql -match 'BEGIN TRANSACTION;') {
        $state.MutationCount++
        $journalPath = if ($state.Plan.action -ceq 'Apply') {
            $state.Plan.journalPath
        } else {
            $state.Plan.removeJournalPath
        }
        $events = @(Get-Content -LiteralPath $journalPath | ForEach-Object { ConvertFrom-Json -InputObject $_ })
        if ($events[-1].stage -cne 'SqlAttempt' -or -not $state.CredentialExists) {
            throw 'Fixture: mutation did not follow durable intent and credential.'
        }
        if ($state.Behavior -ceq 'SqlUnknown') { Stop-CatalogProbe -Code 'FixtureSqlUnknown' }
        $state.SqlCommitted = $true
        return 'Committed'
    }
    if ($Sql -match 'sp_getapplock') { return 'Locked' }
    $state.GateCount++
    if ($state.Behavior -ceq 'TransportRefused') { Stop-CatalogProbe -Code 'FixtureTransportRefused' }
    return 'Verified'
}

function Save-OfflineProbeCredential {
    param($Plan, [Management.Automation.PSCredential]$Credential)
    $state = $script:ProbeFixture
    $events = @(Get-Content -LiteralPath $Plan.journalPath | ForEach-Object { ConvertFrom-Json -InputObject $_ })
    if ($events[-1].stage -cne 'IntentFixed' -or $Credential.UserName -cne $Plan.probeName) {
        throw 'Fixture: credential did not follow intent.'
    }
    $state.CredentialExists = $true
    if ($state.Behavior -ceq 'CredentialPartial') { Stop-CatalogProbe -Code 'FixtureCredentialPartial' }
    return $state.CredentialHash
}

function Get-OfflineProbeCredentialHash {
    param($Plan)
    if (-not $script:ProbeFixture.CredentialExists) { Stop-CatalogProbe -Code 'FixtureCredentialMissing' }
    return $script:ProbeFixture.CredentialHash
}

$script:RealProbePublicFile = (Get-Command -Name New-CatalogProbePublicFile -CommandType Function).ScriptBlock
function New-OfflineProbePublicFile {
    param([string]$Path)
    if ($script:ProbeFixture.Behavior -ceq 'ReceiptPartial' -and $Path -ceq $script:ProbeFixture.Plan.receiptPath) {
        [IO.File]::WriteAllText($Path, '{', [Text.UTF8Encoding]::new($false))
        Stop-CatalogProbe -Code 'FixtureReceiptPartial'
    }
    return & $script:RealProbePublicFile -Path $Path
}

function Remove-OfflineProbeCredential {
    [CmdletBinding()]
    param([string]$LiteralPath)
    $state = $script:ProbeFixture
    if ($LiteralPath -cne $state.Plan.credentialPath -or -not $state.SqlCommitted) {
        throw 'Fixture: deletion outside the exact post-commit credential scope.'
    }
    $state.CredentialExists = $false
    $state.DeleteCount++
}

function New-ProbeScenario {
    param([string]$Name, [string]$Behavior = '')
    $root = Join-Path $script:SuiteRoot $Name
    [void][IO.Directory]::CreateDirectory($root)
    $fixturePlan = New-ProbeFixturePlan -Root $root
    $path = Join-Path $root 'approval.json'
    $hash = Save-ProbeFixtureJson -Value $fixturePlan -Path $path
    $script:ProbeFixture = [pscustomobject]@{
        Plan = $fixturePlan; ApprovalPath = $path; ApprovalHash = $hash; Behavior = $Behavior
        CredentialExists = $false; CredentialHash = ('A' * 64); HostChecked = $false
        OpenCount = 0; GateCount = 0; MutationCount = 0; SqlCommitted = $false; DeleteCount = 0
    }
}

function Invoke-ProbeScenario {
    return Invoke-CatalogProbe -Action $script:ProbeFixture.Plan.action `
        -ApprovalPath $script:ProbeFixture.ApprovalPath -ExpectedApprovalHash $script:ProbeFixture.ApprovalHash `
        -SourcePath $probeSource
}

$boundaries = [ordered]@{
    'Assert-CatalogProbeHost' = 'Assert-OfflineProbeHost'
    'Assert-CatalogProbeAcl' = 'Assert-OfflineProbeAcl'
    'Assert-CatalogProbeNoReparse' = 'Assert-OfflineProbePath'
    'Open-CatalogProbeMaster' = 'Open-OfflineProbeMaster'
    'Invoke-CatalogProbeSql' = 'Invoke-OfflineProbeSql'
    'Save-CatalogProbeCredential' = 'Save-OfflineProbeCredential'
    'Get-CatalogProbeCredentialHash' = 'Get-OfflineProbeCredentialHash'
    'New-CatalogProbePublicFile' = 'New-OfflineProbePublicFile'
    'Remove-Item' = 'Remove-OfflineProbeCredential'
}
$originalBoundaries = @{}
try {
    foreach ($name in $boundaries.Keys) {
        if (Get-Alias -Name $name -ErrorAction SilentlyContinue) { throw 'Fixture alias collision.' }
        $original = Get-Command -Name $name -CommandType Function -ErrorAction SilentlyContinue
        $originalBoundaries[$name] = if ($null -eq $original) { $null } else { $original.ScriptBlock }
        $body = (Get-Command -Name $boundaries[$name] -CommandType Function).ScriptBlock
        Set-Item -Path ('Function:script:' + $name) -Value $body
    }
    foreach ($name in $boundaries.Keys) {
        $resolved = Get-Command -Name $name
        if ($resolved.CommandType -ne 'Function' -or
            [IO.Path]::GetFullPath($resolved.ScriptBlock.File) -cne [IO.Path]::GetFullPath($PSCommandPath)) {
            throw ('Fixture boundary was not replaced by this test file: ' + $name)
        }
    }
    Assert-Equal -Name 'Test-Path remains a cmdlet after stand-in installation' -Expected Cmdlet `
        -Actual (Get-Command -Name Test-Path).CommandType
    $hashAfterStubs = (Get-FileHash -LiteralPath $publicPath -Algorithm SHA256).Hash
    Assert-Equal -Name 'PS5.1 Get-FileHash still handles its internal PathType call' -Expected $publicHash `
        -Actual $hashAfterStubs
    New-ProbeScenario -Name 'success'
    $applied = Invoke-ProbeScenario
    Assert-True -Name 'Apply completes through real orchestration with stubbed I/O' -Condition $applied.succeeded
    Assert-Equal -Name 'Only one SQL mutation attempt' -Expected 1 -Actual $script:ProbeFixture.MutationCount
    Assert-True -Name 'Fresh credential retained' -Condition $script:ProbeFixture.CredentialExists
    $journalBefore = (Get-FileHash -LiteralPath $script:ProbeFixture.Plan.journalPath).Hash
    $repeated = Invoke-ProbeScenario
    Assert-Equal -Name 'Apply rerun refuses adoption' -Expected ApplyAlreadyStarted -Actual $repeated.failure.category
    Assert-Equal -Name 'Rerun preserves raw intent' -Expected $journalBefore `
        -Actual (Get-FileHash -LiteralPath $script:ProbeFixture.Plan.journalPath).Hash
    $applyPlan = $script:ProbeFixture.Plan
    $removePlan = $applyPlan.PSObject.Copy()
    $removePlan.action = 'Remove'
    $removePlan.step = 'RemoveCatalogProbe'
    $removePlan.deleteCredential = $true
    $removePlan.originalApprovalHash = $script:ProbeFixture.ApprovalHash
    $removePlan.originalReceiptHash = (Get-FileHash -LiteralPath $applyPlan.receiptPath).Hash
    $removePlan.credentialHash = $script:ProbeFixture.CredentialHash
    $script:ProbeFixture.Plan = $removePlan
    $script:ProbeFixture.ApprovalPath = Join-Path $removePlan.evidenceDirectory 'remove-approval.json'
    $script:ProbeFixture.ApprovalHash = Save-ProbeFixtureJson -Value $removePlan `
        -Path $script:ProbeFixture.ApprovalPath
    $validReceiptHash = $removePlan.originalReceiptHash
    $removePlan.originalReceiptHash = 'F' * 64
    $script:ProbeFixture.ApprovalHash = Save-ProbeFixtureJson -Value $removePlan `
        -Path $script:ProbeFixture.ApprovalPath
    $badReceipt = Invoke-ProbeScenario
    Assert-Equal -Name 'Remove cannot invent creation attribution' -Expected PublicDocumentHash `
        -Actual $badReceipt.failure.category
    Assert-Equal -Name 'Wrong creation receipt cannot delete credential' -Expected 0 `
        -Actual $script:ProbeFixture.DeleteCount
    Assert-Equal -Name 'Wrong creation receipt cannot reach SQL mutation' -Expected 1 `
        -Actual $script:ProbeFixture.MutationCount
    $removePlan.originalReceiptHash = $validReceiptHash
    $script:ProbeFixture.ApprovalHash = Save-ProbeFixtureJson -Value $removePlan `
        -Path $script:ProbeFixture.ApprovalPath
    $createdSid = $removePlan.plannedSid
    $removePlan.plannedSid = '0x2233445566778899AABBCCDDEEFF1122'
    $script:ProbeFixture.ApprovalHash = Save-ProbeFixtureJson -Value $removePlan `
        -Path $script:ProbeFixture.ApprovalPath
    $wrongSid = Invoke-ProbeScenario
    Assert-Equal -Name 'Remove preserves original SID attribution' -Expected CreationReceiptTarget `
        -Actual $wrongSid.failure.category
    Assert-Equal -Name 'Changed SID cannot delete credential' -Expected 0 -Actual $script:ProbeFixture.DeleteCount
    $removePlan.plannedSid = $createdSid
    $script:ProbeFixture.ApprovalHash = Save-ProbeFixtureJson -Value $removePlan `
        -Path $script:ProbeFixture.ApprovalPath
    $script:ProbeFixture.SqlCommitted = $false
    $removed = Invoke-ProbeScenario
    Assert-True -Name 'Fresh Remove action uses original creation receipt' -Condition $removed.succeeded
    Assert-Equal -Name 'Only exact credential removed after commit' -Expected 1 -Actual $script:ProbeFixture.DeleteCount
    Assert-True -Name 'Original creation receipt preserved' -Condition (
        Microsoft.PowerShell.Management\Test-Path -LiteralPath $applyPlan.receiptPath)

    foreach ($behavior in @('AclRefused', 'ReparseRefused', 'TransportRefused', 'CredentialPartial',
            'SqlUnknown', 'ReceiptPartial')) {
        New-ProbeScenario -Name $behavior -Behavior $behavior
        $result = Invoke-ProbeScenario
        Assert-True -Name ($behavior + ' does not report success') -Condition (-not $result.succeeded)
        Assert-Equal -Name ($behavior + ' never auto-deletes credentials') -Expected 0 `
            -Actual $script:ProbeFixture.DeleteCount
        if ($behavior -cin @('CredentialPartial', 'SqlUnknown', 'ReceiptPartial')) {
            Assert-True -Name ($behavior + ' retains partial credential') `
                -Condition $script:ProbeFixture.CredentialExists
            $hashBefore = (Get-FileHash -LiteralPath $script:ProbeFixture.Plan.journalPath).Hash
            $script:ProbeFixture.Behavior = ''
            $retry = Invoke-ProbeScenario
            Assert-Equal -Name ($behavior + ' retry refuses overwrite') -Expected ApplyAlreadyStarted `
                -Actual $retry.failure.category
            Assert-Equal -Name ($behavior + ' retry preserves journal') -Expected $hashBefore `
                -Actual (Get-FileHash -LiteralPath $script:ProbeFixture.Plan.journalPath).Hash
        }
        if ($behavior -ceq 'SqlUnknown') {
            Assert-Equal -Name 'Unconfirmed SQL remains Unknown' -Expected Unknown -Actual $result.sqlOutcome
            $events = @(Get-Content -LiteralPath $script:ProbeFixture.Plan.journalPath |
                ForEach-Object { ConvertFrom-Json -InputObject $_ })
            Assert-Equal -Name 'Unknown SQL state is durable' -Expected Unknown -Actual $events[-1].sqlOutcome
        }
        if ($behavior -ceq 'ReceiptPartial') {
            Assert-Equal -Name 'Receipt failure retains known SQL commit fact' -Expected Committed `
                -Actual $result.sqlOutcome
            Assert-Equal -Name 'Partial receipt not replaced' -Expected '{' `
                -Actual ([IO.File]::ReadAllText($script:ProbeFixture.Plan.receiptPath))
        }
        if ($behavior -cin @('AclRefused', 'ReparseRefused', 'TransportRefused')) {
            Assert-Equal -Name ($behavior + ' no SQL mutation') -Expected 0 -Actual $script:ProbeFixture.MutationCount
            Assert-True -Name ($behavior + ' no credential') -Condition (-not $script:ProbeFixture.CredentialExists)
        }
    }
    Assert-True -Name 'Private credential absent after all orchestration scenarios' -Condition (
        -not (Test-Path -LiteralPath $plan.credentialPath))
}
finally {
    foreach ($name in $originalBoundaries.Keys) {
        if ($null -ne $originalBoundaries[$name]) {
            Set-Item -Path ('Function:script:' + $name) -Value $originalBoundaries[$name]
        }
        else { Microsoft.PowerShell.Management\Remove-Item -LiteralPath ('Function:script:' + $name) }
    }
}
Complete-TestSuite
