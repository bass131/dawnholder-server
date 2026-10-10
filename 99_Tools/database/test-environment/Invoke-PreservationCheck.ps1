[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Record', 'Compare')][string]$Action,
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [string]$Database,
    [string]$ManifestPath,
    [Parameter(Mandatory)][string]$EvidenceDirectory,
    [Parameter(Mandatory)][string]$ResultPath,
    [Parameter(Mandatory)][string]$SnapshotPath
)
# Record after Baseline001; Compare after official Complete and before binding.
# Both files stay under EvidenceDirectory. Record commits once and journals the snapshot hash.
. (Join-Path $PSScriptRoot 'EngineCheck.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$requiredState = if ($Action -ceq 'Record') { 'Baseline001' } else { 'Installed' }
Invoke-EngineCheck -Request $PSBoundParameters -Tool 'Invoke-PreservationCheck' -RequiredState $requiredState -Work {
    param($context)
    if ($context.Report.Action -ceq 'Record') { Invoke-PreservationRecord -Context $context }
    else { Invoke-PreservationCompare -Context $context }
}
