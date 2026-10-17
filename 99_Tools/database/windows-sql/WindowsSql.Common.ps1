# Definitions only: file reads belong to the reader; machine effects belong to the adapter.
. (Join-Path $PSScriptRoot 'WindowsSql.Plan.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Observation.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Lifecycle.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Cleanup.ps1')

function Read-WindowsSqlPlan {
    param($Path, $ExpectedHash)
    if ($ExpectedHash -isnot [string] -or $ExpectedHash -cnotmatch '^[A-F0-9]{64}$') {
        throw 'WINDOWS_SQL_PLAN_IDENTITY_REQUIRED'
    }
    $bytes = Read-WindowsSqlPlanBytes -Path $Path
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
    if ($hash -cne $ExpectedHash) { throw 'WINDOWS_SQL_PLAN_CHANGED' }
    $plan = ConvertFrom-WindowsSqlPlanBytes -Bytes $bytes
    Assert-WindowsSqlPlanFields -Plan $plan
    Assert-WindowsSqlApproval -Plan $plan
    Assert-WindowsSqlPlanPaths -Plan $plan
    Assert-WindowsSqlInputs -Plan $plan
    $plan['PlanHash'] = $ExpectedHash
    return [pscustomobject]$plan
}

function Get-WindowsSqlChangePlan {
    param($Plan, $Observation)
    # This decision reads only its arguments, including the caller-provided observation.
    Assert-WindowsSqlPlanFields -Plan $Plan -ReadPlan
    return New-WindowsSqlChangePlan -Plan $Plan -Observation $Observation
}

function Invoke-WindowsSqlStep {
    param($Plan, $Step, $Adapter)
    if ($Step -isnot [string] -or $Step -cnotin @('Preflight', 'Plan', 'Apply', 'Verify', 'Cleanup')) {
        throw 'WINDOWS_SQL_STEP_INVALID'
    }
    Assert-WindowsSqlPlanFields -Plan $Plan -ReadPlan
    # Recheck time at use, so an in-memory plan cannot outlive its approval.
    Assert-WindowsSqlApproval -Plan $Plan
    if (-not (Test-WindowsSqlText -Value (Get-WindowsSqlField -Object $Adapter -Name 'Kind')) -or
        $null -eq $Adapter.PSObject.Methods['InvokeOperation']) {
        throw 'WINDOWS_SQL_ADAPTER_INVALID'
    }
    $failureCode = switch -CaseSensitive ($Step) {
        'Apply' { 'WINDOWS_SQL_APPLY_FAILED' }
        'Cleanup' { 'WINDOWS_SQL_CLEANUP_FAILED' }
        default { 'WINDOWS_SQL_ADAPTER_FAILED' }
    }
    $observation = Read-WindowsSqlObservation -Plan $Plan -Adapter $Adapter -FailureCode $failureCode
    $result = [ordered]@{ Step = $Step; AdapterKind = $Adapter.Kind }
    switch -CaseSensitive ($Step) {
        'Preflight' { $result.Observation = $observation }
        'Plan' { $result.ChangePlan = Get-WindowsSqlChangePlan -Plan $Plan -Observation $observation }
        'Apply' {
            $result.Status = Invoke-WindowsSqlApply -Plan $Plan -Observation $observation -Adapter $Adapter
        }
        'Verify' {
            Assert-WindowsSqlVerification -Plan $Plan -Observation $observation -Adapter $Adapter
            $result.Status = 'Verified'
        }
        'Cleanup' {
            $result.Status = Invoke-WindowsSqlCleanup -Plan $Plan -Observation $observation -Adapter $Adapter
        }
    }
    return [pscustomobject]$result
}
