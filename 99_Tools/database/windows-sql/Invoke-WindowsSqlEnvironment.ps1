#requires -Version 7.0
[CmdletBinding()]
param([string]$PlanPath, [string]$ExpectedPlanHash, [string]$RuntimePath, [string]$Step)
if ($MyInvocation.InvocationName -eq '.') { return }
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WindowsSql.Common.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Adapter.ps1')
$adapter = $null
try {
    if ($Step -cnotin @('Preflight', 'Plan', 'Apply', 'Verify', 'Cleanup')) { throw 'WINDOWS_SQL_STEP_INVALID' }
    $plan = Read-WindowsSqlPlan -Path $PlanPath -ExpectedHash $ExpectedPlanHash
    # Wrong-machine safety is deliberately before runtime or provider initialization.
    if ([Environment]::MachineName -cne $plan.MachineName) { throw 'WINDOWS_SQL_IDENTITY_MISMATCH' }
    $adapter = New-WindowsSqlAdapter -Plan $plan -RuntimePath $RuntimePath
    $result = Invoke-WindowsSqlStep -Plan $plan -Step $Step -Adapter $adapter
    $result | ConvertTo-Json -Depth 20 -Compress
    if ($Step -ceq 'Plan' -and $result.ChangePlan.Decision -ceq 'Blocked') { exit 2 }
    exit 0
}
catch {
    $code = $_.Exception.Message
    if ($code -cnotmatch '^WINDOWS_SQL_[A-Z0-9_]+$') { $code = 'WINDOWS_SQL_EXECUTION_FAILED' }
    [Console]::Error.WriteLine($code)
    exit 1
}
finally { if ($null -ne $adapter) { Close-WindowsSqlAdapter -Adapter $adapter } }
