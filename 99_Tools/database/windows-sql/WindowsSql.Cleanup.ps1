# Cleanup compares current observations with the journal before issuing any destructive operation.
# Snapshot stays opaque: the core records the original observation separately for partial Apply recovery.
function Assert-WindowsSqlCleanupState {
    param($Plan, $Observation, $State)
    if ($null -eq $State) { throw 'WINDOWS_SQL_CLEANUP_STATE_MISSING' }
    if ((Get-WindowsSqlField -Object $State -Name 'LifetimeId') -cne $Plan.LifetimeId) {
        throw 'WINDOWS_SQL_TARGET_NOT_OWNED'
    }
    if (-not (Test-WindowsSqlObservation -Observation $Observation)) {
        throw 'WINDOWS_SQL_OBSERVATION_UNCLEAR'
    }
    if ($Observation.Database.Name -cne $Plan.Database -or $Observation.Login.Name -cne $Plan.RuntimeLogin -or
        $Observation.Port.Port -ne $Plan.Port) {
        throw 'WINDOWS_SQL_IDENTITY_MISMATCH'
    }
    foreach ($resource in @($Observation.Database, $Observation.Login)) {
        if ($resource.Exists -and $resource.OwnedLifetimeId -cne $Plan.LifetimeId) {
            throw 'WINDOWS_SQL_TARGET_NOT_OWNED'
        }
    }
    $status = Get-WindowsSqlField -Object $State -Name 'Status'
    $completed = Get-WindowsSqlField -Object $State -Name 'CompletedChanges'
    $target = Get-WindowsSqlField -Object $State -Name 'TargetConfiguration'
    $snapshot = Get-WindowsSqlField -Object $State -Name 'Snapshot'
    $known = @('SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService',
        'AddFirewallRule', 'CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin')
    if ($status -isnot [string] -or $status -cnotin @('Applied', 'Failed', 'Cleaned') -or
        $completed -isnot [array] -or $null -eq $snapshot -or
        -not (Test-WindowsSqlConfiguration -Configuration $target)) {
        throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
    }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $lastIndex = -1
    foreach ($change in $completed) {
        if ($change -isnot [string] -or $change -cnotin $known -or -not $seen.Add($change)) {
            throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
        }
        $index = [Array]::IndexOf($known, $change)
        if ($index -le $lastIndex) { throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' }
        $lastIndex = $index
    }
    if (($completed -ccontains 'ApplyMigrations') -and ($completed -cnotcontains 'CreateDatabase')) {
        throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
    }
    if ($target.LoginMode -cne 'Mixed' -or -not $target.TcpEnabled -or $target.TcpPort -ne $Plan.Port -or
        $target.DynamicPorts -or $target.CertificateThumbprint -cne $Plan.CertificateThumbprint) {
        throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
    }
    $expected = $target
    $original = Get-WindowsSqlField -Object $State -Name 'OriginalConfiguration'
    if ($status -ceq 'Failed') {
        $failedOperation = Get-WindowsSqlField -Object $State -Name 'FailedOperation'
        if ((Get-WindowsSqlField -Object $State -Name 'CleanupStarted') -eq $true -or
            $failedOperation -isnot [string] -or
            $failedOperation -cnotin ($known + @('WriteLifecycleState')) -or
            -not (Test-WindowsSqlConfiguration -Configuration $original)) {
            throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
        }
        # Migration/restart can partly commit without an observable receipt in this v1 adapter contract.
        # Such failures require explicit investigation, not inferred deletion or an automatic retry.
        if ($failedOperation -cin @('ApplyMigrations', 'RestartService')) {
            throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
        }
        $expected = Copy-WindowsSqlConfiguration -Configuration $original
        foreach ($change in $completed) {
            Set-WindowsSqlExpectedConfiguration -Configuration $expected -Operation $change -Plan $Plan
        }
        $recordedExpected = Get-WindowsSqlField -Object $State -Name 'ExpectedConfiguration'
        if (-not (Test-WindowsSqlConfigurationEqual -Left $expected -Right $recordedExpected)) {
            throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
        }
        # An unconfirmed create must still be absent; an owner mark does not prove that it completed.
        if (($failedOperation -ceq 'CreateDatabase' -and $Observation.Database.Exists) -or
            ($failedOperation -ceq 'CreateRuntimeLogin' -and $Observation.Login.Exists) -or
            ($failedOperation -ceq 'AddFirewallRule' -and $null -ne $Observation.Network.OwnedFirewallRule)) {
            throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
        }
    }
    elseif ($status -ceq 'Cleaned') {
        if (-not (Test-WindowsSqlConfiguration -Configuration $original)) {
            throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
        }
        $expected = $original
    }
    if (-not (Test-WindowsSqlConfigurationEqual -Left $Observation.Configuration -Right $expected)) {
        throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
    }
    foreach ($binding in @(
            @{ Change = 'CreateDatabase'; Resource = $Observation.Database },
            @{ Change = 'CreateRuntimeLogin'; Resource = $Observation.Login }
        )) {
        if ($completed -ccontains $binding.Change) {
            $shouldExist = $status -cne 'Cleaned'
            if ($binding.Resource.Exists -ne $shouldExist) { throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' }
        }
    }
    if ($Observation.Database.Exists -and $completed -ccontains 'ApplyMigrations' -and
        $Observation.Database.SchemaVersion -ne $Plan.SchemaVersion) {
        throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
    }
    if ($completed -ccontains 'AddFirewallRule') {
        $rule = $Observation.Network.OwnedFirewallRule
        if ($status -ceq 'Cleaned') {
            if ($null -ne $rule) { throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' }
        }
        else {
            if ($null -eq $rule -or $rule.Port -ne $Plan.Port -or
                -not (Test-WindowsSqlFirewallScope -Scope $rule.RemoteAddress)) {
                throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
            }
            $recordedRule = Get-WindowsSqlField -Object $State -Name 'FirewallRule'
            # Full Applied states from the initial contract have no extra partial-recovery fields.
            $scope = if ($null -ne $recordedRule) {
                Get-WindowsSqlField -Object $recordedRule -Name 'RemoteAddress'
            } else { $Observation.Network.WslSubnet }
            if ($rule.RemoteAddress -cne $scope) { throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED' }
        }
    }
    $restore = @($completed | Where-Object {
            $_ -cin @('SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate')
        }).Count -gt 0
    if ($status -cne 'Cleaned' -and $restore -and -not $Plan.RestartApproved) {
        throw 'WINDOWS_SQL_RESTART_NOT_APPROVED'
    }
    if ((Get-WindowsSqlField -Object $State -Name 'PlanHash') -cne $Plan.PlanHash) {
        throw 'WINDOWS_SQL_CLEANUP_STATE_CHANGED'
    }
    return $restore
}

function Copy-WindowsSqlCleanupState {
    param($State, [string]$Status, $PendingOperation, $FailedOperation)
    $copy = [ordered]@{}
    foreach ($key in @(Get-WindowsSqlKeys -Object $State)) {
        $copy[$key] = Get-WindowsSqlField -Object $State -Name $key
    }
    $copy.Status = $Status
    $copy.PendingOperation = $PendingOperation
    $copy.FailedOperation = $FailedOperation
    $copy.CleanupStarted = $true
    return [pscustomobject]$copy
}

function Invoke-WindowsSqlCleanup {
    param($Plan, $Observation, $Adapter)
    $state = Invoke-WindowsSqlOperation -Adapter $Adapter -Operation 'ReadLifecycleState' `
        -Request (New-WindowsSqlRequest -Plan $Plan) -FailureCode 'WINDOWS_SQL_CLEANUP_FAILED'
    $restore = Assert-WindowsSqlCleanupState -Plan $Plan -Observation $Observation -State $state
    if ($state.Status -ceq 'Cleaned') { return 'Cleaned' }
    $operations = [Collections.Generic.List[string]]::new()
    if ($state.CompletedChanges -ccontains 'CreateRuntimeLogin') { $operations.Add('RemoveRuntimeLogin') }
    if ($state.CompletedChanges -ccontains 'CreateDatabase') { $operations.Add('RemoveDatabase') }
    if ($state.CompletedChanges -ccontains 'AddFirewallRule') { $operations.Add('RemoveFirewallRule') }
    if ($restore) {
        $operations.Add('RestoreConfiguration')
        $operations.Add('RestartService')
    }
    $operation = 'WriteLifecycleState'
    $pending = $null
    try {
        foreach ($change in $operations) {
            $pending = $change
            $operation = 'WriteLifecycleState'
            $journal = Copy-WindowsSqlCleanupState -State $state -Status 'Applying' `
                -PendingOperation $pending -FailedOperation $null
            Write-WindowsSqlState -Adapter $Adapter -State $journal -FailureCode 'WINDOWS_SQL_CLEANUP_FAILED'
            $operation = $change
            $request = New-WindowsSqlRequest -Plan $Plan
            if ($change -ceq 'RestoreConfiguration') { $request.Snapshot = $state.Snapshot }
            Invoke-WindowsSqlMutation -Adapter $Adapter -Operation $change -Request $request `
                -FailureCode 'WINDOWS_SQL_CLEANUP_FAILED'
            $pending = $null
        }
        $operation = 'WriteLifecycleState'
        $journal = Copy-WindowsSqlCleanupState -State $state -Status 'Cleaned' `
            -PendingOperation $null -FailedOperation $null
        Write-WindowsSqlState -Adapter $Adapter -State $journal -FailureCode 'WINDOWS_SQL_CLEANUP_FAILED'
        return 'Cleaned'
    }
    catch {
        $journal = Copy-WindowsSqlCleanupState -State $state -Status 'Failed' `
            -PendingOperation $pending -FailedOperation $operation
        try { Write-WindowsSqlState -Adapter $Adapter -State $journal -FailureCode 'WINDOWS_SQL_CLEANUP_FAILED' }
        catch { }
        throw 'WINDOWS_SQL_CLEANUP_FAILED'
    }
}
