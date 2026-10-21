[CmdletBinding()]
param([Parameter(Mandatory)][string]$WorkRoot)

# Independent verification of the catalog-test probe source: the real file entry, and the real Invoke-CatalogProbe
# orchestration with its SQL, host, ACL and credential boundaries replaced in process.
# - The file entry runs only with refusals that stop before any SQL or private path: a wrong approval hash and a
#   fixture executor SID that the real host check rejects. Its success path needs the real SQL host; not run here.
# - In process, host/ACL/connection/command/credential save/credential hash are recording stand-ins set through
#   Function:script:, as Set-OfflineStubs does. No-reparse and public-file creation keep the real functions; the
#   wrappers skip only the private credential folder and inject one receipt failure. Remove-Item is fail-closed and
#   admits only the exact credential path.
# - Test-Path is not shadowed: in Windows PowerShell 5.1 a script-scope Test-Path stand-in is also seen by
#   Get-FileHash inside its module. The only private path it reaches is whether the new catalog-probe.clixml exists.
# - The probe's in-memory password and PSCredential are synthetic. Stand-ins read the user name and parameter names,
#   never parameter values. SQL engine semantics, real ACLs and DPAPI files are not verified here.
if ($PSVersionTable.PSEdition -cne 'Desktop' -or $PSVersionTable.PSVersion.ToString() -notlike '5.1.*') {
    throw 'Run this independent suite in Windows PowerShell 5.1.'
}
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'catalog-test-probe-independent'
$script:ProbeSource = Join-Path $script:ToolRoot 'test-environment/Set-CatalogTestProbe.ps1'
$script:CredentialPath = 'C:\Users\bass1\AppData\Local\Dawnholder\8256bcde8c80478494bcefb9cdfaf4f6\catalog-probe.clixml'
$script:PrivateFolder = 'C:\Users\bass1\AppData\Local\Dawnholder\'
$script:FixtureSid = 'S-1-5-21-1-2-3-1001'
$script:FixtureCredentialHash = 'C' * 64

function New-IndependentEvidence {
    param([string]$Name, [switch]$Missing)
    $root = [IO.Path]::GetFullPath((Join-Path $script:SuiteRoot $Name))
    if (-not $Missing) { [void][IO.Directory]::CreateDirectory($root) }
    return $root
}

function New-IndependentPlan {
    param([string]$Evidence, [string]$Kind = 'Apply', [string]$ApprovalHash = '', [string]$ReceiptHash = '',
        [string]$CredentialHash = '')
    $created = [DateTimeOffset]::UtcNow.AddMinutes(-1)
    $sources = @('test-environment/Set-CatalogTestProbe.ps1', 'SqlError.Common.ps1',
        'migrations/005_character_catalog.sql', 'verify-schema.sql')
    return [pscustomobject][ordered]@{
        version = 1
        purpose = 'character-catalog-probe'
        documentState = 'DraftRequiresExplicitActionHashApproval'
        action = $Kind
        step = $(if ($Kind -ceq 'Apply') { 'CreateCatalogProbe' } else { 'RemoveCatalogProbe' })
        createdUtc = $created.ToString('yyyy-MM-ddTHH:mm:ssZ')
        expiresUtc = $created.AddHours(23).ToString('yyyy-MM-ddTHH:mm:ssZ')
        machine = 'DESKTOP-88BJKQH'
        executorSid = $script:FixtureSid
        instance = 'DESKTOP-88BJKQH\SQLEXPRESS'
        adminEndpoint = 'lpc:.\SQLEXPRESS'
        sqlMajor = 17
        database = 'Dawnholder_Char_8256bcde8c80'
        databaseGuid = '7C6E662B-9C39-4410-B256-868742E6619B'
        runtimeName = 'dh_char_8256bcde8c80'
        runtimeSid = '0x043491844159D3A10FA504F68F4B8425'
        schemaVersion = 5
        moduleRelease = 5
        moduleManifestChecksum = 'AC6D31EA3C28C21FD01D150F2828BD9AA9FCAF8384FEB9B9B2A171F4E718A571'
        probeName = 'dh_probe_8256bcde8c80'
        plannedSid = '0x112233445566778899AABBCCDDEEFF11'
        credentialPath = $script:CredentialPath
        evidenceDirectory = $Evidence
        receiptPath = Join-Path $Evidence 'probe-apply-receipt.json'
        journalPath = Join-Path $Evidence 'probe-apply-journal.jsonl'
        removeReceiptPath = Join-Path $Evidence 'probe-remove-receipt.json'
        removeJournalPath = Join-Path $Evidence 'probe-remove-journal.jsonl'
        serverPermissions = @('CONNECT SQL', 'VIEW SERVER PERFORMANCE STATE')
        databasePermissions = @('CONNECT', 'SELECT ON OBJECT::dh.Account')
        deleteCredential = ($Kind -ceq 'Remove')
        originalReceiptHash = $ReceiptHash
        originalApprovalHash = $ApprovalHash
        credentialHash = $CredentialHash
        sourceHashes = @($sources | ForEach-Object {
                [pscustomobject][ordered]@{
                    path = $_
                    sha256 = (Get-FileHash -LiteralPath (Join-Path $script:ToolRoot $_) -Algorithm SHA256).Hash
                }
            })
    }
}

function Save-IndependentJson {
    param($Value, [string]$Path)
    [IO.File]::WriteAllText($Path, (ConvertTo-Json -InputObject $Value -Depth 10), [Text.UTF8Encoding]::new($false))
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Read-IndependentJournal {
    param([string]$Path)
    if (-not [IO.File]::Exists($Path)) { return @() }
    # The probe may still hold its journal open with FileShare.Read.
    $stream = [IO.FileStream]::new($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false))
    try { $text = $reader.ReadToEnd() }
    finally { $reader.Dispose() }
    return @($text.Split([char]10) | Where-Object { $_ } | ForEach-Object { ConvertFrom-Json -InputObject $_ })
}

function Get-JournalStages {
    param([string]$Path)
    return (@(Read-IndependentJournal -Path $Path | ForEach-Object { $_.stage }) -join '|')
}

function Get-JournalTail {
    param([string]$Path)
    $entries = @(Read-IndependentJournal -Path $Path)
    if ($entries.Count -eq 0) { return '' }
    return $entries[-1].stage + '/' + $entries[-1].sqlOutcome
}

function Get-BoundaryOrder {
    # Credential hash reads are asserted separately, so a changed number of rechecks does not hide the order.
    return (@($script:ProbeWorld.Events | Where-Object { $_ -cne 'Hash' }) -join '|')
}

function Test-HashReadBetween {
    param([string]$After, [string]$Before)
    $events = @($script:ProbeWorld.Events)
    $start = [Array]::IndexOf($events, $After)
    $end = [Array]::IndexOf($events, $Before)
    if ($start -lt 0 -or $end -le $start + 1) { return $false }
    return @($events[($start + 1)..($end - 1)] | Where-Object { $_ -ceq 'Hash' }).Count -gt 0
}

function New-IndependentSqlFailure {
    param([int]$Number)
    return New-DatabaseSqlFailure -Exception (New-TestSqlException -Number $Number -Message 'independent fixture')
}

# 1. Import and the real file entry, before any stand-in exists.
$importOutput = . $script:ProbeSource
. (Join-Path $script:ToolRoot 'SqlError.Common.ps1')
Assert-True -Name 'Dot-source import writes no output' -Condition ($null -eq $importOutput)
Assert-True -Name 'Fixture credential file is absent before the suite' -Condition (
    -not (Test-Path -LiteralPath $script:CredentialPath))
$fixtureFailure = New-IndependentSqlFailure -Number 51403
Assert-Equal -Name 'Fixture SQL failure carries the provider number' -Expected 51403 `
    -Actual $fixtureFailure.Data['DatabaseSqlNumber']

$entryRoot = New-IndependentEvidence -Name 'file-entry'
$entryPlan = New-IndependentPlan -Evidence $entryRoot
$entryApproval = Join-Path $entryRoot 'approval.json'
$entryHash = Save-IndependentJson -Value $entryPlan -Path $entryApproval
$global:LASTEXITCODE = 0
$entryOutput = & $script:ProbeSource -Action Apply -ApprovalPath $entryApproval -ExpectedApprovalHash ('0' * 64)
$entryExit = $LASTEXITCODE
$entryResult = ConvertFrom-Json -InputObject ($entryOutput -join "`n")
Assert-Equal -Name 'File entry: wrong approval hash is refused' -Expected PublicDocumentHash `
    -Actual $entryResult.failure.category
Assert-Equal -Name 'File entry: refusal exits 1' -Expected 1 -Actual $entryExit
$global:LASTEXITCODE = 0
$entryOutput = & $script:ProbeSource -Action Apply -ApprovalPath $entryApproval -ExpectedApprovalHash $entryHash
$entryExit = $LASTEXITCODE
$entryResult = ConvertFrom-Json -InputObject ($entryOutput -join "`n")
Assert-Equal -Name 'File entry: fixture executor is refused by the real host check' -Expected ExecutorIdentity `
    -Actual $entryResult.failure.category
Assert-Equal -Name 'File entry: identity refusal reports no SQL start' -Expected NotStarted `
    -Actual $entryResult.sqlOutcome
Assert-Equal -Name 'File entry: identity refusal exits 1' -Expected 1 -Actual $entryExit
Assert-True -Name 'File entry: identity refusal writes no journal' -Condition (
    -not [IO.File]::Exists($entryPlan.journalPath))

# 2. Stand-ins for the in-process orchestration.
function Assert-IndependentHost {
    param($Plan)
}

function Assert-IndependentAcl {
    param([string]$Path, [string]$ExecutorSid, [switch]$Directory)
    if ($ExecutorSid -cne $script:FixtureSid) { throw 'Independent stand-in: unexpected executor SID.' }
}

$script:RealNoReparse = (Get-Command -Name Assert-CatalogProbeNoReparse -CommandType Function).ScriptBlock
function Assert-IndependentNoReparse {
    param([string]$Path)
    if ($Path.StartsWith($script:PrivateFolder, [StringComparison]::OrdinalIgnoreCase)) { return }
    & $script:RealNoReparse -Path $Path
}

$script:RealPublicFile = (Get-Command -Name New-CatalogProbePublicFile -CommandType Function).ScriptBlock
function New-IndependentPublicFile {
    param([string]$Path)
    if ($script:ProbeWorld.Behavior -ceq 'ReceiptBlocked' -and $Path -ceq $script:ProbeWorld.ReceiptPath) {
        Stop-CatalogProbe -Code 'IndependentReceiptBlocked'
    }
    return & $script:RealPublicFile -Path $Path
}

function Open-IndependentMaster {
    param($Plan)
    $script:ProbeWorld.Events.Add('Open')
    $connection = [pscustomobject]@{ World = $script:ProbeWorld }
    $connection | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.World.Events.Add('Dispose') }
    return $connection
}

function Invoke-IndependentSql {
    param($Connection, [string]$Sql, [hashtable]$Parameters)
    $world = $script:ProbeWorld
    # The table reference and its key names are kept; values, including the password, are never read.
    $world.Tables.Add($Parameters)
    if ($Sql.Contains('BEGIN TRANSACTION;')) {
        $world.Events.Add('Mutation')
        $world.MutationJournalTail = Get-JournalTail -Path $world.JournalPath
        $world.MutationHadPassword = $Parameters.ContainsKey('password')
        $world.MutationCreatesLogin = $Sql.Contains('CREATE LOGIN')
        $world.MutationDropsLogin = $Sql.Contains('DROP LOGIN')
        if ($world.Behavior -ceq 'MutationTimeout') { throw (New-IndependentSqlFailure -Number -2) }
        return 'Committed'
    }
    if ($Sql.Contains('sp_getapplock')) {
        $world.Events.Add('Lock')
        if ($world.Behavior -ceq 'LockBusy') { throw (New-IndependentSqlFailure -Number 51416) }
        return 'Locked'
    }
    $world.Events.Add('Gate')
    if ($world.Behavior -ceq 'GateRefused') { throw (New-IndependentSqlFailure -Number 51403) }
    return 'Verified'
}

function Save-IndependentCredential {
    param($Plan, [Management.Automation.PSCredential]$Credential)
    $world = $script:ProbeWorld
    $world.Events.Add('Save')
    $world.SaveJournalTail = Get-JournalTail -Path $world.JournalPath
    $world.SavedUserName = $Credential.UserName
    $world.CredentialExists = $true
    return $script:FixtureCredentialHash
}

function Get-IndependentCredentialHash {
    param($Plan)
    $world = $script:ProbeWorld
    $world.Events.Add('Hash')
    if (-not $world.CredentialExists) { Stop-CatalogProbe -Code 'IndependentCredentialMissing' }
    if ($world.Behavior -ceq 'HashChangedAfterSave' -and $world.Events.Contains('Save')) { return 'D' * 64 }
    return $script:FixtureCredentialHash
}

function Remove-IndependentCredential {
    [CmdletBinding()]
    param([string]$LiteralPath)
    if ($LiteralPath -cne $script:CredentialPath) {
        throw 'Independent guard: Remove-Item outside the exact credential path.'
    }
    $script:ProbeWorld.Events.Add('Delete')
    $script:ProbeWorld.CredentialExists = $false
}

$standIns = [ordered]@{
    'Assert-CatalogProbeHost' = 'Assert-IndependentHost'
    'Assert-CatalogProbeAcl' = 'Assert-IndependentAcl'
    'Assert-CatalogProbeNoReparse' = 'Assert-IndependentNoReparse'
    'New-CatalogProbePublicFile' = 'New-IndependentPublicFile'
    'Open-CatalogProbeMaster' = 'Open-IndependentMaster'
    'Invoke-CatalogProbeSql' = 'Invoke-IndependentSql'
    'Save-CatalogProbeCredential' = 'Save-IndependentCredential'
    'Get-CatalogProbeCredentialHash' = 'Get-IndependentCredentialHash'
    'Remove-Item' = 'Remove-IndependentCredential'
}
foreach ($name in $standIns.Keys) {
    $body = (Get-Command -Name $standIns[$name] -CommandType Function).ScriptBlock
    Set-Item -Path ('Function:script:' + $name) -Value $body
}
# No scenario runs unless every boundary now resolves, without an alias, to a stand-in from this file.
$thisFile = [IO.Path]::GetFullPath($PSCommandPath)
foreach ($name in $standIns.Keys) {
    $resolved = Get-Command -Name $name
    if ($resolved.CommandType -ne 'Function' -or [IO.Path]::GetFullPath($resolved.ScriptBlock.File) -ne $thisFile) {
        throw ('Independent stand-in is not in effect: ' + $name)
    }
}

function Invoke-IndependentScenario {
    param($Plan, [string]$ApprovalFile, [string]$ApprovalHash, [string]$Behavior = '', [bool]$CredentialExists = $false)
    $isApply = $Plan.action -ceq 'Apply'
    $script:ProbeWorld = [pscustomobject]@{
        Behavior = $Behavior
        JournalPath = $(if ($isApply) { $Plan.journalPath } else { $Plan.removeJournalPath })
        ReceiptPath = $(if ($isApply) { $Plan.receiptPath } else { $Plan.removeReceiptPath })
        Events = New-Object 'Collections.Generic.List[string]'
        Tables = New-Object 'Collections.Generic.List[object]'
        CredentialExists = $CredentialExists
        SavedUserName = ''
        SaveJournalTail = ''
        MutationJournalTail = ''
        MutationHadPassword = $false
        MutationCreatesLogin = $false
        MutationDropsLogin = $false
    }
    return Invoke-CatalogProbe -Action $Plan.action -ApprovalPath $ApprovalFile -ExpectedApprovalHash $ApprovalHash `
        -SourcePath $script:ProbeSource
}

function New-IndependentApproval {
    param($Plan, [string]$Name)
    $path = Join-Path $Plan.evidenceDirectory $Name
    return [pscustomobject]@{ Path = $path; Hash = (Save-IndependentJson -Value $Plan -Path $path) }
}

# 3. Apply, its rerun, and Remove attributed to that Apply.
$mainRoot = New-IndependentEvidence -Name 'apply-remove'
$applyPlan = New-IndependentPlan -Evidence $mainRoot
$applyApproval = New-IndependentApproval -Plan $applyPlan -Name 'apply-approval.json'
$result = Invoke-IndependentScenario -Plan $applyPlan -ApprovalFile $applyApproval.Path `
    -ApprovalHash $applyApproval.Hash
Assert-True -Name 'Apply: succeeds with every boundary stood in' -Condition $result.succeeded
Assert-Equal -Name 'Apply: SQL outcome' -Expected Committed -Actual $result.sqlOutcome
Assert-Equal -Name 'Apply: boundary order' -Expected 'Open|Gate|Lock|Save|Mutation|Dispose' -Actual (Get-BoundaryOrder)
Assert-True -Name 'Apply: credential hash rechecked between save and mutation' -Condition (
    Test-HashReadBetween -After Save -Before Mutation)
Assert-Equal -Name 'Apply: credential saved only after durable intent' -Expected 'IntentFixed/NotStarted' `
    -Actual $script:ProbeWorld.SaveJournalTail
Assert-Equal -Name 'Apply: mutation runs after durable Unknown attempt' -Expected 'SqlAttempt/Unknown' `
    -Actual $script:ProbeWorld.MutationJournalTail
Assert-Equal -Name 'Apply: credential user is the probe login' -Expected 'dh_probe_8256bcde8c80' `
    -Actual $script:ProbeWorld.SavedUserName
Assert-True -Name 'Apply: mutation creates the login with a password parameter' -Condition (
    $script:ProbeWorld.MutationCreatesLogin -and -not $script:ProbeWorld.MutationDropsLogin -and
    $script:ProbeWorld.MutationHadPassword)
Assert-Equal -Name 'Apply: journal stages' `
    -Expected 'IntentFixed|CredentialSaved|SqlAttempt|SqlCommitted|ReceiptPublished' `
    -Actual (Get-JournalStages -Path $applyPlan.journalPath)
$applyReceipt = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($applyPlan.receiptPath))
Assert-True -Name 'Apply: receipt binds status, approval and credential' -Condition (
    $applyReceipt.status -ceq 'Applied' -and $applyReceipt.sqlOutcome -ceq 'Committed' -and
    $applyReceipt.approvalHash -ceq $applyApproval.Hash -and
    $applyReceipt.credentialHash -ceq $script:FixtureCredentialHash)
Assert-Equal -Name 'Apply: every SQL parameter table is cleared after the run' -Expected 0 -Actual (
    @($script:ProbeWorld.Tables | Where-Object { $_.Count -ne 0 }).Count)

$journalBefore = (Get-FileHash -LiteralPath $applyPlan.journalPath -Algorithm SHA256).Hash
$result = Invoke-IndependentScenario -Plan $applyPlan -ApprovalFile $applyApproval.Path `
    -ApprovalHash $applyApproval.Hash
Assert-Equal -Name 'Apply rerun: refused' -Expected ApplyAlreadyStarted -Actual $result.failure.category
Assert-Equal -Name 'Apply rerun: no SQL or credential boundary reached' -Expected '' `
    -Actual ($script:ProbeWorld.Events -join '|')
Assert-Equal -Name 'Apply rerun: journal bytes unchanged' -Expected $journalBefore `
    -Actual (Get-FileHash -LiteralPath $applyPlan.journalPath -Algorithm SHA256).Hash

$receiptHash = (Get-FileHash -LiteralPath $applyPlan.receiptPath -Algorithm SHA256).Hash
$badPlan = New-IndependentPlan -Evidence $mainRoot -Kind Remove -ApprovalHash $applyApproval.Hash `
    -ReceiptHash ('F' * 64) -CredentialHash $script:FixtureCredentialHash
$badApproval = New-IndependentApproval -Plan $badPlan -Name 'remove-approval-wrong-receipt.json'
$result = Invoke-IndependentScenario -Plan $badPlan -ApprovalFile $badApproval.Path -ApprovalHash $badApproval.Hash `
    -CredentialExists $true
Assert-Equal -Name 'Remove with wrong receipt hash: refused' -Expected PublicDocumentHash `
    -Actual $result.failure.category
Assert-Equal -Name 'Remove with wrong receipt hash: no SQL or credential boundary reached' -Expected '' `
    -Actual ($script:ProbeWorld.Events -join '|')
Assert-True -Name 'Remove with wrong receipt hash: no remove journal' -Condition (
    -not [IO.File]::Exists($badPlan.removeJournalPath))

$removePlan = New-IndependentPlan -Evidence $mainRoot -Kind Remove -ApprovalHash $applyApproval.Hash `
    -ReceiptHash $receiptHash -CredentialHash $script:FixtureCredentialHash
$removeApproval = New-IndependentApproval -Plan $removePlan -Name 'remove-approval.json'
$result = Invoke-IndependentScenario -Plan $removePlan -ApprovalFile $removeApproval.Path `
    -ApprovalHash $removeApproval.Hash -CredentialExists $true
Assert-True -Name 'Remove: succeeds with the original creation receipt' -Condition $result.succeeded
Assert-Equal -Name 'Remove: boundary order' -Expected 'Open|Gate|Lock|Mutation|Delete|Dispose' `
    -Actual (Get-BoundaryOrder)
Assert-Equal -Name 'Remove: credential hash verified before SQL opens' -Expected Hash `
    -Actual $script:ProbeWorld.Events[0]
Assert-True -Name 'Remove: credential hash rechecked between commit and deletion' -Condition (
    Test-HashReadBetween -After Mutation -Before Delete)
Assert-Equal -Name 'Remove: mutation runs after durable Unknown attempt' -Expected 'SqlAttempt/Unknown' `
    -Actual $script:ProbeWorld.MutationJournalTail
Assert-True -Name 'Remove: mutation drops the login without a password parameter' -Condition (
    $script:ProbeWorld.MutationDropsLogin -and -not $script:ProbeWorld.MutationCreatesLogin -and
    -not $script:ProbeWorld.MutationHadPassword)
Assert-Equal -Name 'Remove: journal stages' `
    -Expected 'IntentFixed|SqlAttempt|SqlCommitted|CredentialRemoved|ReceiptPublished' `
    -Actual (Get-JournalStages -Path $removePlan.removeJournalPath)
$removeReceipt = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($removePlan.removeReceiptPath))
Assert-Equal -Name 'Remove: receipt status' -Expected Removed -Actual $removeReceipt.status
Assert-Equal -Name 'Remove: creation receipt bytes preserved' -Expected $receiptHash `
    -Actual (Get-FileHash -LiteralPath $applyPlan.receiptPath -Algorithm SHA256).Hash

# 4. Failures after the gate keep what is known and never delete the credential.
$cases = @(
    @{ Name = 'gate-refused'; Behavior = 'GateRefused'; Category = 'BoundaryFailure'; Number = 51403
        Outcome = 'NotStarted'; Order = 'Open|Gate|Dispose'; Stages = ''; Credential = $false }
    @{ Name = 'lock-busy'; Behavior = 'LockBusy'; Category = 'BoundaryFailure'; Number = 51416
        Outcome = 'NotStarted'; Order = 'Open|Gate|Lock|Dispose'; Stages = ''; Credential = $false }
    @{ Name = 'hash-changed'; Behavior = 'HashChangedAfterSave'; Category = 'CredentialHash'; Number = 0
        Outcome = 'NotStarted'; Order = 'Open|Gate|Lock|Save|Dispose'
        Stages = 'IntentFixed|CredentialSaved|FailedPreserved'; Credential = $true }
    @{ Name = 'mutation-timeout'; Behavior = 'MutationTimeout'; Category = 'BoundaryFailure'; Number = -2
        Outcome = 'Unknown'; Order = 'Open|Gate|Lock|Save|Mutation|Dispose'
        Stages = 'IntentFixed|CredentialSaved|SqlAttempt|FailedPreserved'; Credential = $true }
    @{ Name = 'receipt-blocked'; Behavior = 'ReceiptBlocked'; Category = 'IndependentReceiptBlocked'; Number = 0
        Outcome = 'Committed'; Order = 'Open|Gate|Lock|Save|Mutation|Dispose'
        Stages = 'IntentFixed|CredentialSaved|SqlAttempt|SqlCommitted|FailedPreserved'; Credential = $true }
)
foreach ($case in $cases) {
    $plan = New-IndependentPlan -Evidence (New-IndependentEvidence -Name $case.Name)
    $approval = New-IndependentApproval -Plan $plan -Name 'apply-approval.json'
    $result = Invoke-IndependentScenario -Plan $plan -ApprovalFile $approval.Path -ApprovalHash $approval.Hash `
        -Behavior $case.Behavior
    $label = $case.Name + ': '
    Assert-True -Name ($label + 'not reported as success') -Condition (-not $result.succeeded)
    Assert-Equal -Name ($label + 'failure category') -Expected $case.Category -Actual $result.failure.category
    Assert-Equal -Name ($label + 'SQL number') -Expected $case.Number -Actual $result.failure.sqlNumber
    Assert-Equal -Name ($label + 'SQL outcome') -Expected $case.Outcome -Actual $result.sqlOutcome
    Assert-Equal -Name ($label + 'boundary order') -Expected $case.Order -Actual (Get-BoundaryOrder)
    Assert-Equal -Name ($label + 'journal stages') -Expected $case.Stages `
        -Actual (Get-JournalStages -Path $plan.journalPath)
    if ($case.Stages) {
        Assert-Equal -Name ($label + 'last journal entry keeps the SQL outcome') `
            -Expected ('FailedPreserved/' + $case.Outcome) `
            -Actual (Get-JournalTail -Path $plan.journalPath)
    }
    Assert-Equal -Name ($label + 'credential exists only if saved, never deleted') -Expected $case.Credential `
        -Actual $script:ProbeWorld.CredentialExists
    Assert-True -Name ($label + 'no receipt') -Condition (-not [IO.File]::Exists($plan.receiptPath))
}

# The approval lives in an existing folder; only the plan's evidence folder is missing.
$missingPlan = New-IndependentPlan -Evidence (New-IndependentEvidence -Name 'evidence-missing' -Missing)
$missingApprovalPath = Join-Path (New-IndependentEvidence -Name 'evidence-approval') 'apply-approval.json'
$missingApprovalHash = Save-IndependentJson -Value $missingPlan -Path $missingApprovalPath
$result = Invoke-IndependentScenario -Plan $missingPlan -ApprovalFile $missingApprovalPath `
    -ApprovalHash $missingApprovalHash
Assert-Equal -Name 'Missing evidence folder: refused at the durable intent' -Expected EvidenceParentMissing `
    -Actual $result.failure.category
Assert-Equal -Name 'Missing evidence folder: no credential or mutation' -Expected 'Open|Gate|Lock|Dispose' `
    -Actual (Get-BoundaryOrder)

Complete-TestSuite
