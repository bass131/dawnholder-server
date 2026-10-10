[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('BeforeComplete', 'AfterComplete')][string]$Action,
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [string]$Database,
    [string]$ManifestPath,
    [Parameter(Mandatory)][string]$EvidenceDirectory,
    [Parameter(Mandatory)][string]$ResultPath,
    [Parameter(Mandatory)][string]$SnapshotPath
)
# A = BeforeComplete (Baseline001 with recorded data); B = AfterComplete (Installed, before binding).
# Every observation rolls back. An unequal restored state stops the remaining schedule immediately.
. (Join-Path $PSScriptRoot 'EngineCheck.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$requiredState = if ($Action -ceq 'BeforeComplete') { 'Baseline001' } else { 'Installed' }
Invoke-EngineCheck -Request $PSBoundParameters -Tool 'Test-EngineBoundary' -RequiredState $requiredState -Work {
    param($context)
    Invoke-PreservationCompare -Context $context
    $before = Get-EngineDatabaseState -Context $context
    if ($before.TransactionCount -ne 0) { Stop-EngineCheck -Message 'An existing transaction prevents observation.' }
    $bundle = Read-ModuleBundle -DatabaseRoot $context.DatabaseRoot
    if ($context.Report.Action -ceq 'BeforeComplete') {
        if ($before.History.Count -ne 1 -or $null -ne $before.Objects.CharacterAuthority -or
            $null -ne $before.Objects.CharacterOperation) {
            Stop-EngineCheck -Message 'The observation requires the unchanged baseline before migration metadata.'
        }
        Invoke-EngineRollback -Context $context -Scenario 'FirstInstall' -Action {
            param($transaction)
            $observer = New-EngineObserver -Context $context -Scenario 'FirstInstall' -Observe
            Invoke-WithEngineObserver -Observer $observer -Action {
                $null = Invoke-Migrations -Connection $context.Connection -Transaction $transaction `
                    -Phase Complete -Contract $context.Contract
            }
            $observed = @($context.Report.Observations | Where-Object Scenario -CEQ 'FirstInstall')
            $expectedCounts = [ordered]@{
                HistoryBefore = 1
                HistoryAfter = 4
                ModuleRegistration = 0
                ActualModules = 0
                Release = 1
            }
            foreach ($kind in $expectedCounts.Keys) {
                $visits = @($observed | Where-Object Kind -CEQ $kind)
                if ($visits.Count -eq 0) {
                    Stop-EngineCheck -Message 'First installation did not visit every JSON boundary.'
                }
                if ($visits[0].Verdict.IndependentCount -ne $expectedCounts[$kind]) {
                    Stop-EngineCheck -Message 'First-install natural counts differ from the required baseline path.'
                }
            }
            if (Invoke-EngineStimulus -Context $context -Transaction $transaction -Case 'EmptyRelease' `
                    -Sql 'DELETE FROM dh.ModuleRelease;') {
                Invoke-EngineSiteProbe -Context $context -Transaction $transaction `
                    -Kind Release -Scenario 'EmptyRelease'
                Invoke-EngineRejection -Context $context -Case EmptyRelease `
                    -Stimulus 'DELETE FROM dh.ModuleRelease;' -Action {
                    $null = Invoke-ModuleBundle -Connection $context.Connection `
                        -Transaction $transaction -Bundle $bundle
                }
            }
        }
        Invoke-EngineRollback -Context $context -Scenario 'EmptyHistory' -Action {
            param($transaction)
            if (Invoke-EngineStimulus -Context $context -Transaction $transaction -Case 'EmptyHistory' `
                    -Sql 'DELETE FROM dh.SchemaVersion;') {
                foreach ($kind in @('HistoryBefore', 'HistoryAfter')) {
                    Invoke-EngineSiteProbe -Context $context -Transaction $transaction `
                        -Kind $kind -Scenario 'EmptyHistory'
                }
            }
        }
        Invoke-EngineAtomicity -Context $context
    } else {
        Invoke-EngineRollback -Context $context -Scenario 'InstalledNatural' -Action {
            param($transaction)
            foreach ($site in $context.Sites) {
                Invoke-EngineSiteProbe -Context $context -Transaction $transaction `
                    -Kind $site.Kind -Scenario 'InstalledNatural'
                $last = $context.Report.Observations[$context.Report.Observations.Count - 1]
                if ($last.Verdict.IndependentCount -le 0) {
                    Stop-EngineCheck -Message 'Installed natural JSON boundaries must all contain rows.'
                }
            }
        }
        $stimuli = [ordered]@{
            EmptyRegistration = 'DELETE FROM dh.ModuleDefinition;'
            DefinitionDrift = @'
UPDATE dh.ModuleDefinition SET DefinitionChecksum =
    CASE WHEN DefinitionChecksum = REPLICATE('0', 64) THEN REPLICATE('F', 64) ELSE REPLICATE('0', 64) END
WHERE ObjectName = (SELECT MIN(ObjectName) FROM dh.ModuleDefinition);
'@
            MissingRegistration = @'
DELETE FROM dh.ModuleDefinition WHERE ObjectName = (SELECT MIN(ObjectName) FROM dh.ModuleDefinition);
'@
            MalformedRelease = "INSERT dh.ModuleRelease(Version, ManifestChecksum) VALUES(3, REPLICATE('0', 64));"
        }
        foreach ($case in $stimuli.Keys) {
            Invoke-EngineRollback -Context $context -Scenario $case -Action {
                param($transaction)
                $stimulated = Invoke-EngineStimulus -Context $context -Transaction $transaction `
                    -Case $case -Sql $stimuli[$case]
                if ($stimulated) {
                    if ($case -ceq 'EmptyRegistration') {
                        Invoke-EngineSiteProbe -Context $context -Transaction $transaction `
                            -Kind ModuleRegistration -Scenario $case
                    }
                    Invoke-EngineRejection -Context $context -Case $case -Stimulus $stimuli[$case] -Action {
                        if ($case -ceq 'MalformedRelease') {
                            $null = Invoke-ModuleBundle -Connection $context.Connection `
                                -Transaction $transaction -Bundle $bundle
                        } else {
                            $state = Get-DatabaseModuleState -Connection $context.Connection -Transaction $transaction
                            Assert-DatabaseModuleState -State $state -Bundle $bundle -AllowFirstInstall $false
                        }
                    }
                }
            }
        }
    }
}
