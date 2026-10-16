# Only this boundary invokes the adapter. Provider streams and exception text never become public results.
function Invoke-WindowsSqlOperation {
    param($Adapter, [string]$Operation, $Request, [string]$FailureCode = 'WINDOWS_SQL_ADAPTER_FAILED')
    try {
        $records = @($Adapter.InvokeOperation($Operation, $Request) *>&1)
        $values = @($records | Where-Object {
                $null -ne $_ -and $_ -isnot [Management.Automation.InformationalRecord] -and
                $_ -isnot [Management.Automation.InformationRecord]
            })
        if ($values.Count -eq 0 -and $Operation -ceq 'ReadLifecycleState') { return $null }
        if ($values.Count -ne 1 -or
            ($values[0] -isnot [pscustomobject] -and $values[0] -isnot [Collections.IDictionary])) {
            throw 'WINDOWS_SQL_ADAPTER_FAILED'
        }
        return , $values[0]
    }
    catch { throw $FailureCode }
}

function New-WindowsSqlRequest {
    param($Plan)
    return [ordered]@{
        MachineName = $Plan.MachineName
        InstanceName = $Plan.InstanceName
        InstanceId = $Plan.InstanceId
        Database = $Plan.Database
        Login = $Plan.RuntimeLogin
        LifetimeId = $Plan.LifetimeId
        Port = $Plan.Port
    }
}

function Read-WindowsSqlObservation {
    param($Plan, $Adapter, [string]$FailureCode)
    $request = New-WindowsSqlRequest -Plan $Plan
    $readArguments = @{ Adapter = $Adapter; Request = $request; FailureCode = $FailureCode }
    $machine = Invoke-WindowsSqlOperation @readArguments -Operation 'ReadMachineIdentity'
    if (-not (Test-WindowsSqlMachine -Machine $machine)) { throw 'WINDOWS_SQL_OBSERVATION_UNCLEAR' }
    if ($machine.MachineName -cne $Plan.MachineName) { throw 'WINDOWS_SQL_IDENTITY_MISMATCH' }
    $instance = Invoke-WindowsSqlOperation @readArguments -Operation 'ReadInstanceIdentity'
    if (-not (Test-WindowsSqlInstance -Instance $instance)) { throw 'WINDOWS_SQL_OBSERVATION_UNCLEAR' }
    if ($instance.InstanceName -cne $Plan.InstanceName -or $instance.InstanceId -cne $Plan.InstanceId -or
        $instance.ProductVersion -cne $Plan.ExpectedProductVersion) {
        throw 'WINDOWS_SQL_IDENTITY_MISMATCH'
    }
    $observation = [ordered]@{ Machine = $machine; Instance = $instance }
    $operations = [ordered]@{
        Configuration = 'ReadInstanceConfiguration'
        Port = 'ReadPortOwner'
        Network = 'ReadNetworkMode'
        Database = 'ReadTargetDatabase'
        Login = 'ReadTargetLogin'
    }
    foreach ($section in $operations.Keys) {
        $readArguments.Request = New-WindowsSqlRequest -Plan $Plan
        $observation[$section] = Invoke-WindowsSqlOperation @readArguments -Operation $operations[$section]
    }
    return [pscustomobject]$observation
}

function Invoke-WindowsSqlMutation {
    param($Adapter, [string]$Operation, $Request, [string]$FailureCode)
    $response = Invoke-WindowsSqlOperation -Adapter $Adapter -Operation $Operation -Request $Request `
        -FailureCode $FailureCode
    $completed = Get-WindowsSqlField -Object $response -Name 'Completed'
    if ($completed -isnot [bool] -or -not $completed) { throw $FailureCode }
}

function New-WindowsSqlState {
    param($Plan, [string]$Status, $Snapshot, $Target, $Original, $Expected, $Completed,
        $FailedOperation, $PendingOperation, $FirewallRule)
    # Every journal request owns a new object and arrays: later progress cannot rewrite an earlier receipt.
    return [pscustomobject][ordered]@{
        LifetimeId = $Plan.LifetimeId
        PlanHash = $Plan.PlanHash
        Status = $Status
        Snapshot = $Snapshot
        TargetConfiguration = Copy-WindowsSqlConfiguration -Configuration $Target
        OriginalConfiguration = Copy-WindowsSqlConfiguration -Configuration $Original
        ExpectedConfiguration = Copy-WindowsSqlConfiguration -Configuration $Expected
        CompletedChanges = @($Completed)
        FailedOperation = $FailedOperation
        PendingOperation = $PendingOperation
        FirewallRule = $FirewallRule
    }
}

function Write-WindowsSqlState {
    param($Adapter, $State, [string]$FailureCode)
    Invoke-WindowsSqlMutation -Adapter $Adapter -Operation 'WriteLifecycleState' -Request $State `
        -FailureCode $FailureCode
}

function Set-WindowsSqlExpectedConfiguration {
    param($Configuration, [string]$Operation, $Plan)
    switch -CaseSensitive ($Operation) {
        'SetMixedAuthentication' { $Configuration.LoginMode = 'Mixed' }
        'SetTcpPort' {
            $Configuration.TcpEnabled = $true
            $Configuration.TcpPort = $Plan.Port
            $Configuration.DynamicPorts = $false
        }
        'SetServerCertificate' { $Configuration.CertificateThumbprint = $Plan.CertificateThumbprint }
    }
}

function Invoke-WindowsSqlApply {
    param($Plan, $Observation, $Adapter)
    $changePlan = Get-WindowsSqlChangePlan -Plan $Plan -Observation $Observation
    if ($changePlan.Decision -ceq 'Blocked') { throw $changePlan.Blockers[0] }
    $request = New-WindowsSqlRequest -Plan $Plan
    $previous = Invoke-WindowsSqlOperation -Adapter $Adapter -Operation 'ReadLifecycleState' -Request $request `
        -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
    if ($null -ne $previous) {
        if ((Get-WindowsSqlField -Object $previous -Name 'LifetimeId') -cne $Plan.LifetimeId) {
            throw 'WINDOWS_SQL_TARGET_NOT_OWNED'
        }
        # Never replace a recovery journal with a new snapshot after a failed or interrupted Apply.
        if ((Get-WindowsSqlField -Object $previous -Name 'Status') -cne 'Applied' -or
            (Get-WindowsSqlField -Object $previous -Name 'PlanHash') -cne $Plan.PlanHash -or
            $changePlan.Decision -cne 'NoChange') {
            throw 'WINDOWS_SQL_LIFECYCLE_STATE_CHANGED'
        }
    }
    if ($changePlan.Decision -ceq 'NoChange') { return 'Unchanged' }

    $snapshot = $null
    $completed = [Collections.Generic.List[string]]::new()
    $original = Copy-WindowsSqlConfiguration -Configuration $Observation.Configuration
    $expected = Copy-WindowsSqlConfiguration -Configuration $original
    $rule = $null
    $operation = 'SaveConfigurationSnapshot'
    $pending = $null
    $stateArguments = @{
        Plan = $Plan
        Target = $changePlan.TargetConfiguration
        Original = $original
        Expected = $expected
    }
    try {
        $snapshot = Invoke-WindowsSqlOperation -Adapter $Adapter -Operation $operation -Request $request `
            -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
        foreach ($change in $changePlan.Changes) {
            # PendingOperation distinguishes interruption from a confirmed successful change.
            $pending = $change
            $operation = 'WriteLifecycleState'
            $state = New-WindowsSqlState @stateArguments -Status 'Applying' -Snapshot $snapshot `
                -Completed $completed.ToArray() -FailedOperation $null -PendingOperation $pending -FirewallRule $rule
            Write-WindowsSqlState -Adapter $Adapter -State $state -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
            $operation = $change
            $changeRequest = New-WindowsSqlRequest -Plan $Plan
            switch -CaseSensitive ($change) {
                'SetServerCertificate' { $changeRequest.Thumbprint = $Plan.CertificateThumbprint }
                'AddFirewallRule' { $changeRequest.RemoteAddress = $Observation.Network.WslSubnet }
            }
            Invoke-WindowsSqlMutation -Adapter $Adapter -Operation $change -Request $changeRequest `
                -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
            $completed.Add($change)
            Set-WindowsSqlExpectedConfiguration -Configuration $expected -Operation $change -Plan $Plan
            if ($change -ceq 'AddFirewallRule') {
                $rule = [pscustomobject]@{ RemoteAddress = $Observation.Network.WslSubnet; Port = $Plan.Port }
            }
            $pending = $null
        }
        $operation = 'WriteLifecycleState'
        $state = New-WindowsSqlState @stateArguments -Status 'Applied' -Snapshot $snapshot `
            -Completed $completed.ToArray() -FailedOperation $null -PendingOperation $null -FirewallRule $rule
        Write-WindowsSqlState -Adapter $Adapter -State $state -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
        return 'Applied'
    }
    catch {
        # Failure never becomes rollback or success, even if journaling also fails.
        $failed = New-WindowsSqlState @stateArguments -Status 'Failed' -Snapshot $snapshot `
            -Completed $completed.ToArray() -FailedOperation $operation -PendingOperation $pending -FirewallRule $rule
        try { Write-WindowsSqlState -Adapter $Adapter -State $failed -FailureCode 'WINDOWS_SQL_APPLY_FAILED' }
        catch { }
        throw 'WINDOWS_SQL_APPLY_FAILED'
    }
}

function Assert-WindowsSqlVerification {
    param($Plan, $Observation, $Adapter)
    $changePlan = Get-WindowsSqlChangePlan -Plan $Plan -Observation $Observation
    if ($changePlan.Decision -ceq 'Blocked') { throw $changePlan.Blockers[0] }
    if (-not $Observation.Database.Exists -or -not $Observation.Login.Exists -or
        $changePlan.Decision -cne 'NoChange') {
        throw 'WINDOWS_SQL_CATALOG_VERIFY_FAILED'
    }
    $request = New-WindowsSqlRequest -Plan $Plan
    $request.Encrypt = 'Mandatory'
    $request.TrustServerCertificate = $false
    $tls = Invoke-WindowsSqlOperation -Adapter $Adapter -Operation 'VerifyTlsConnection' -Request $request `
        -FailureCode 'WINDOWS_SQL_TLS_VERIFY_FAILED'
    if (-not (Test-WindowsSqlShape -Object $tls -Fields @('Encrypted', 'CertificateValidated')) -or
        $tls.Encrypted -isnot [bool] -or -not $tls.Encrypted -or
        $tls.CertificateValidated -isnot [bool] -or -not $tls.CertificateValidated) {
        throw 'WINDOWS_SQL_TLS_VERIFY_FAILED'
    }
    $catalog = Invoke-WindowsSqlOperation -Adapter $Adapter -Operation 'VerifyTargetCatalog' -Request $request `
        -FailureCode 'WINDOWS_SQL_CATALOG_VERIFY_FAILED'
    if (-not (Test-WindowsSqlShape -Object $catalog -Fields @('CatalogMatches', 'SchemaVersion')) -or
        $catalog.CatalogMatches -isnot [bool] -or -not $catalog.CatalogMatches -or
        -not (Test-WindowsSqlInteger -Value $catalog.SchemaVersion) -or
        $catalog.SchemaVersion -ne $Plan.SchemaVersion) {
        throw 'WINDOWS_SQL_CATALOG_VERIFY_FAILED'
    }
}
