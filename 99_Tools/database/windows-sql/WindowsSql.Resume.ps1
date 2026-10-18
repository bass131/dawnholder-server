# Explicit two-plan handoff only. Ordinary Apply never enters this path.
. (Join-Path $PSScriptRoot 'WindowsSql.Common.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Adapter.ps1')

function Read-WindowsSqlResumeArtifact {
    param([string]$Path, [string]$Sha256)
    if ($Sha256 -cnotmatch '^[A-F0-9]{64}$') { throw 'WINDOWS_SQL_RESUME_BINDING_INVALID' }
    [void](Assert-WindowsSqlLocalPath -Path $Path)
    $bytes = Read-WindowsSqlPlanBytes -Path $Path
    if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne $Sha256) {
        throw 'WINDOWS_SQL_RESUME_ARTIFACT_CHANGED'
    }
    return , $bytes
}

function Read-WindowsSqlResumeContext {
    param([string]$BindingPath, [string]$BindingHash, [string]$EntryPath,
        [string]$Mode, [string]$Approval)
    if ($Mode -cnotin @('Resume', 'Verify', 'Cleanup')) { throw 'WINDOWS_SQL_RESUME_MODE_INVALID' }
    $binding = ConvertFrom-WindowsSqlPlanBytes -Bytes (
        Read-WindowsSqlResumeArtifact -Path $BindingPath -Sha256 $BindingHash)
    $fields = @('Version', 'LifetimeId', 'ExpiresUtc', 'RuntimePath', 'LegacyPlan', 'ExecutionPlan',
        'Entry', 'InitialLifecycleHash', 'SnapshotHash', 'ApprovalRequired')
    if ($binding.Version -eq 2) { $fields += @('PreviousBinding', 'PreviousAttempt', 'InputChanges', 'SourceEvidence') }
    if (-not (Test-WindowsSqlShape -Object $binding -Fields $fields) -or
        -not (Test-WindowsSqlInteger -Value $binding.Version) -or $binding.Version -notin @(1, 2) -or
        $binding.ApprovalRequired -isnot [bool] -or -not $binding.ApprovalRequired -or
        $binding.InitialLifecycleHash -cnotmatch '^[A-F0-9]{64}$' -or
        $binding.SnapshotHash -cnotmatch '^[A-F0-9]{64}$') { throw 'WINDOWS_SQL_RESUME_BINDING_INVALID' }
    foreach ($artifact in @($binding.LegacyPlan, $binding.ExecutionPlan, $binding.Entry)) {
        if (-not (Test-WindowsSqlShape -Object $artifact -Fields @('Path', 'Sha256'))) {
            throw 'WINDOWS_SQL_RESUME_BINDING_INVALID'
        }
        [void](Read-WindowsSqlResumeArtifact -Path $artifact.Path -Sha256 $artifact.Sha256)
    }
    if ((Get-WindowsSqlFullPath -Path $EntryPath) -cne $binding.Entry.Path -or
        $Approval -cne ($Mode + ' ' + $binding.LifetimeId + ' binding ' + $BindingHash)) {
        throw 'WINDOWS_SQL_RESUME_APPROVAL_REQUIRED'
    }
    # The old plan is historical ownership, never a waiver for executing its stale Inputs.
    $legacy = ConvertFrom-WindowsSqlPlanBytes -Bytes (
        Read-WindowsSqlResumeArtifact -Path $binding.LegacyPlan.Path -Sha256 $binding.LegacyPlan.Sha256)
    Assert-WindowsSqlPlanFields -Plan $legacy
    Assert-WindowsSqlPlanPaths -Plan $legacy
    Assert-WindowsSqlApproval -Plan $legacy
    $legacy['PlanHash'] = $binding.LegacyPlan.Sha256
    $execution = Read-WindowsSqlPlan -Path $binding.ExecutionPlan.Path -ExpectedHash $binding.ExecutionPlan.Sha256
    foreach ($key in @(Get-WindowsSqlKeys -Object $legacy)) {
        if ($key -cin @('Inputs', 'PlanHash')) { continue }
        if ((Get-WindowsSqlField -Object $execution -Name $key) -cne $legacy[$key]) {
            throw 'WINDOWS_SQL_RESUME_IDENTITY_CHANGED'
        }
    }
    if ($binding.LifetimeId -cne $legacy.LifetimeId -or $binding.ExpiresUtc -cne $legacy.ExpiresUtc -or
        $legacy.PlanHash -ceq $execution.PlanHash) { throw 'WINDOWS_SQL_RESUME_IDENTITY_CHANGED' }
    $runtime = Read-WindowsSqlRuntime -Plan $execution -Relative $binding.RuntimePath
    $previous = $null
    $unchangedPlan = $execution
    if ($binding.Version -eq 2) {
        $previous = Read-WindowsSqlPreviousResume -Binding $binding -OwnershipPlan $legacy
        Assert-WindowsSqlSourceHandoff -Binding $binding -PreviousPlan $previous.ExecutionPlan -ExecutionPlan $execution
        $unchangedPlan = $previous.ExecutionPlan
    }
    # Runtime, public CA, profile and SQL sources remain the original approval's exact bytes.
    $unchanged = @($binding.RuntimePath, $runtime.PublicCaPath, $runtime.ProfilePath) + @(
        $legacy.Inputs | Where-Object { $_.Path -match '/migrations/|/modules/' } | ForEach-Object Path)
    foreach ($path in $unchanged) {
        $old = @($legacy.Inputs | Where-Object Path -CEQ $path)
        $new = @($unchangedPlan.Inputs | Where-Object Path -CEQ $path)
        if ($old.Count -ne 1 -or $new.Count -ne 1 -or $old[0].Sha256 -cne $new[0].Sha256) {
            throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED'
        }
    }
    $artifacts = @($binding.LegacyPlan, $binding.ExecutionPlan, $binding.Entry,
        [pscustomobject]@{ Path = $BindingPath; Sha256 = $BindingHash })
    if ($null -ne $previous) { $artifacts += $previous.Artifacts }
    return [pscustomobject]@{
        BindingPath = $BindingPath
        BindingHash = $BindingHash
        EntryPath = $EntryPath
        Mode = $Mode
        Approval = $Approval
        Binding = $binding
        OwnershipPlan = [pscustomobject]$legacy
        ExecutionPlan = $execution
        PreviousContext = $previous
        Artifacts = $artifacts
    }
}

function Read-WindowsSqlPreviousResume {
    param($Binding, $OwnershipPlan)
    $reference = $Binding.PreviousBinding
    if (-not (Test-WindowsSqlShape -Object $reference -Fields @('Path', 'Sha256')) -or
        -not (Test-WindowsSqlShape -Object $Binding.PreviousAttempt -Fields @('Name', 'Sha256')) -or
        $Binding.PreviousAttempt.Name -cne ('resume-' + $reference.Sha256 + '.json') -or
        $Binding.PreviousAttempt.Sha256 -cnotmatch '^[A-F0-9]{64}$') {
        throw 'WINDOWS_SQL_RESUME_PREVIOUS_INVALID'
    }
    $previous = ConvertFrom-WindowsSqlPlanBytes -Bytes (
        Read-WindowsSqlResumeArtifact -Path $reference.Path -Sha256 $reference.Sha256)
    $fields = @('Version', 'LifetimeId', 'ExpiresUtc', 'RuntimePath', 'LegacyPlan', 'ExecutionPlan',
        'Entry', 'InitialLifecycleHash', 'SnapshotHash', 'ApprovalRequired')
    if (-not (Test-WindowsSqlShape -Object $previous -Fields $fields) -or $previous.Version -ne 1 -or
        $previous.ApprovalRequired -isnot [bool] -or -not $previous.ApprovalRequired -or
        $previous.InitialLifecycleHash -cnotmatch '^[A-F0-9]{64}$' -or
        $previous.InitialLifecycleHash -ceq $Binding.InitialLifecycleHash) {
        throw 'WINDOWS_SQL_RESUME_PREVIOUS_INVALID'
    }
    foreach ($key in @('LifetimeId', 'ExpiresUtc', 'RuntimePath', 'SnapshotHash')) {
        if ($previous[$key] -cne $Binding[$key]) { throw 'WINDOWS_SQL_RESUME_IDENTITY_CHANGED' }
    }
    foreach ($artifact in @($previous.LegacyPlan, $previous.ExecutionPlan, $previous.Entry,
            $Binding.SourceEvidence.Inputs, $Binding.SourceEvidence.Review)) {
        if (-not (Test-WindowsSqlShape -Object $artifact -Fields @('Path', 'Sha256'))) {
            throw 'WINDOWS_SQL_RESUME_PREVIOUS_INVALID'
        }
        [void](Read-WindowsSqlResumeArtifact -Path $artifact.Path -Sha256 $artifact.Sha256)
    }
    if ($previous.LegacyPlan.Path -cne $Binding.LegacyPlan.Path -or
        $previous.LegacyPlan.Sha256 -cne $Binding.LegacyPlan.Sha256 -or
        $previous.ExecutionPlan.Sha256 -ceq $Binding.ExecutionPlan.Sha256 -or
        $previous.ExecutionPlan.Path -ceq $Binding.ExecutionPlan.Path -or
        $previous.Entry.Path -ceq $Binding.Entry.Path -or $reference.Path -ceq $Binding.Entry.Path) {
        throw 'WINDOWS_SQL_RESUME_IDENTITY_CHANGED'
    }
    # Historical inputs describe the failed attempt. Only the NEW execution plan opens current source bytes.
    $plan = ConvertFrom-WindowsSqlPlanBytes -Bytes (
        Read-WindowsSqlResumeArtifact -Path $previous.ExecutionPlan.Path -Sha256 $previous.ExecutionPlan.Sha256)
    Assert-WindowsSqlPlanFields -Plan $plan
    Assert-WindowsSqlPlanPaths -Plan $plan
    Assert-WindowsSqlApproval -Plan $plan
    foreach ($key in @(Get-WindowsSqlKeys -Object $OwnershipPlan)) {
        if ($key -cin @('Inputs', 'PlanHash')) { continue }
        if ($plan[$key] -cne $OwnershipPlan[$key]) { throw 'WINDOWS_SQL_RESUME_IDENTITY_CHANGED' }
    }
    $plan['PlanHash'] = $previous.ExecutionPlan.Sha256
    return [pscustomobject]@{
        Binding = $previous
        BindingHash = $reference.Sha256
        ExecutionPlan = [pscustomobject]$plan
        Artifacts = @($reference, $previous.ExecutionPlan, $previous.Entry,
            $Binding.SourceEvidence.Inputs, $Binding.SourceEvidence.Review)
    }
}

function Get-WindowsSqlHistoricalInputs {
    param($Plan)
    if ($Plan.Inputs -isnot [array] -or $Plan.Inputs.Count -lt 1 -or $Plan.Inputs.Count -gt 128) {
        throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED'
    }
    $map = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $Plan.Inputs) {
        if (-not (Test-WindowsSqlShape -Object $item -Fields @('Path', 'Sha256')) -or
            $item.Path -cnotmatch '^[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)*$' -or
            $item.Path.Split('/') -contains '..' -or $item.Path.Split('/') -contains '.' -or
            $item.Sha256 -cnotmatch '^[A-F0-9]{64}$' -or -not $seen.Add($item.Path)) {
            throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED'
        }
        $map.Add($item.Path, $item.Sha256)
    }
    return , $map
}

function Assert-WindowsSqlSourceHandoff {
    param($Binding, $PreviousPlan, $ExecutionPlan)
    if (-not (Test-WindowsSqlShape -Object $Binding.SourceEvidence -Fields @('Inputs', 'Review')) -or
        $Binding.InputChanges -isnot [array] -or $Binding.InputChanges.Count -eq 0) {
        throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED'
    }
    $before = Get-WindowsSqlHistoricalInputs -Plan $PreviousPlan
    $after = Get-WindowsSqlHistoricalInputs -Plan $ExecutionPlan
    if ($before.Count -ne $after.Count) { throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED' }
    $evidence = ConvertFrom-WindowsSqlPlanBytes -Bytes (Read-WindowsSqlResumeArtifact `
            -Path $Binding.SourceEvidence.Inputs.Path -Sha256 $Binding.SourceEvidence.Inputs.Sha256)
    if ($evidence.Cwd -cne $ExecutionPlan.SourceRoot) { throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED' }
    $sqlPaths = @('migrations/005_character_catalog.sql', 'modules/manifest.json',
        'modules/procedures/character_catalog.sql', 'modules/procedures/internal/lock_and_read_authority.sql',
        'verify-schema.sql') | ForEach-Object { '99_Tools/database/' + $_ }
    $codePaths = @('Resume', 'Adapter', 'Runtime', 'Lifecycle', 'Sql') | ForEach-Object {
        '99_Tools/database/windows-sql/WindowsSql.' + $_ + '.ps1'
    }
    $changes = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    foreach ($change in $Binding.InputChanges) {
        if (-not (Test-WindowsSqlShape -Object $change -Fields @('Path', 'PreviousSha256', 'Sha256', 'Reason')) -or
            $change.Path -cnotin ($sqlPaths + $codePaths) -or -not (Test-WindowsSqlText -Value $change.Reason) -or
            $change.PreviousSha256 -cnotmatch '^[A-F0-9]{64}$' -or $change.Sha256 -cnotmatch '^[A-F0-9]{64}$' -or
            $change.PreviousSha256 -ceq $change.Sha256 -or $changes.ContainsKey($change.Path)) {
            throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED'
        }
        if ($change.Path -cin $sqlPaths) {
            $proof = @($evidence.Inputs | Where-Object Path -CEQ $change.Path)
            if ($proof.Count -ne 1 -or $proof[0].Sha256 -cne $change.Sha256) {
                throw 'WINDOWS_SQL_RESUME_SOURCE_UNVERIFIED'
            }
        }
        $changes.Add($change.Path, $change)
    }
    $changed = 0
    foreach ($path in $before.Keys) {
        if (-not $after.ContainsKey($path)) { throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED' }
        if ($before[$path] -ceq $after[$path]) {
            if ($changes.ContainsKey($path)) { throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED' }
            continue
        }
        $changed++
        if (-not $changes.ContainsKey($path) -or $changes[$path].PreviousSha256 -cne $before[$path] -or
            $changes[$path].Sha256 -cne $after[$path]) { throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED' }
    }
    if ($changed -ne $changes.Count) { throw 'WINDOWS_SQL_RESUME_INPUT_CHANGED' }
}

function Open-WindowsSqlResumeLease {
    param($Adapter)
    $artifacts = @($Adapter.ResumeContext.Artifacts)
    if ($Adapter.ResumeContext.Binding.Version -eq 2) {
        Assert-WindowsSqlPrivateAcl -Path $Adapter.Plan.PrivateRoot -Sid $Adapter.Runtime.ExecutorSid
        $previous = $Adapter.ResumeContext.Binding.PreviousAttempt
        $artifacts += [pscustomobject]@{
            Path = Join-Path $Adapter.Plan.PrivateRoot $previous.Name
            Sha256 = $previous.Sha256
        }
    }
    foreach ($artifact in $artifacts) {
        [void](Assert-WindowsSqlLocalPath -Path $artifact.Path)
        $stream = [IO.File]::Open($artifact.Path, 'Open', 'Read', 'Read')
        $Adapter.Lease.Add($stream)
        if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) -cne $artifact.Sha256) {
            throw 'WINDOWS_SQL_RESUME_ARTIFACT_CHANGED'
        }
    }
}

function Get-WindowsSqlResumeAttemptName {
    param($Adapter)
    return 'resume-' + $Adapter.ResumeContext.BindingHash + '.json'
}

function Assert-WindowsSqlResumeProvenance {
    param($Adapter, $State)
    $context = $Adapter.ResumeContext
    $attempt = Read-WindowsSqlPrivateJson -Adapter $Adapter -Name (Get-WindowsSqlResumeAttemptName -Adapter $Adapter)
    if ($null -eq $attempt -or $State.PlanHash -cne $Adapter.Plan.PlanHash -or
        $State.LifetimeId -cne $Adapter.Plan.LifetimeId -or
        (Get-WindowsSqlField -Object $State -Name 'ExecutionPlanHash') -cne $context.ExecutionPlan.PlanHash -or
        (Get-WindowsSqlField -Object $State -Name 'ExecutionBindingHash') -cne $context.BindingHash -or
        (Get-WindowsSqlField -Object $State -Name 'AttemptId') -cne $attempt.AttemptId -or
        $attempt.ExecutionPlanHash -cne $context.ExecutionPlan.PlanHash -or
        $attempt.ExecutionBindingHash -cne $context.BindingHash -or
        $attempt.InitialLifecycleHash -cne $context.Binding.InitialLifecycleHash -or
        $attempt.SnapshotHash -cne $context.Binding.SnapshotHash -or
        $attempt.EntryHash -cne $context.Binding.Entry.Sha256 -or
        $attempt.Approval -cne ('Resume ' + $Adapter.Plan.LifetimeId + ' binding ' + $context.BindingHash) -or
        $attempt.ResourceReceiptHash -cnotmatch '^[A-F0-9]{64}$' -or
        $attempt.AttemptId -cnotmatch '^[a-f0-9]{32}$') { throw 'WINDOWS_SQL_RESUME_PROVENANCE_CHANGED' }
    if ($context.Binding.Version -eq 2) {
        $previous = Read-WindowsSqlPreviousAttempt -Adapter $Adapter
        foreach ($value in @($State, $attempt)) {
            if ((Get-WindowsSqlField -Object $value -Name 'PreviousBindingHash') -cne
                    $context.Binding.PreviousBinding.Sha256 -or
                (Get-WindowsSqlField -Object $value -Name 'PreviousAttemptHash') -cne
                    $context.Binding.PreviousAttempt.Sha256 -or
                (Get-WindowsSqlField -Object $value -Name 'PreviousAttemptId') -cne $previous.AttemptId) {
                throw 'WINDOWS_SQL_RESUME_PROVENANCE_CHANGED'
            }
        }
        $archive = Read-WindowsSqlResumePrivateEvidence -Adapter $Adapter `
            -Name (Get-WindowsSqlResumeInitialName -Adapter $Adapter) `
            -ExpectedHash $context.Binding.InitialLifecycleHash
        Assert-WindowsSqlPreviousProvenance -Adapter $Adapter -State $archive.Value
    }
}

function Read-WindowsSqlPreviousAttempt {
    param($Adapter)
    $reference = $Adapter.ResumeContext.Binding.PreviousAttempt
    return (Read-WindowsSqlResumePrivateEvidence -Adapter $Adapter -Name $reference.Name `
            -ExpectedHash $reference.Sha256).Value
}

function Assert-WindowsSqlPreviousProvenance {
    param($Adapter, $State)
    [void](Read-WindowsSqlPreviousAttempt -Adapter $Adapter)
    $previousAdapter = [pscustomobject]@{
        Plan = $Adapter.Plan
        Runtime = $Adapter.Runtime
        ResumeContext = $Adapter.ResumeContext.PreviousContext
    }
    Assert-WindowsSqlResumeProvenance -Adapter $previousAdapter -State $State
}

function Get-WindowsSqlResumeInitialName {
    param($Adapter)
    return 'resume-initial-' + $Adapter.ResumeContext.BindingHash + '.json'
}

function Read-WindowsSqlResumePrivateEvidence {
    param($Adapter, [string]$Name, [string]$ExpectedHash)
    $value = Read-WindowsSqlPrivateJson -Adapter $Adapter -Name $Name
    if ($null -eq $value) { throw 'WINDOWS_SQL_RESUME_INITIAL_STATE_CHANGED' }
    $path = Join-Path $Adapter.Plan.PrivateRoot $Name
    $bytes = Read-WindowsSqlPlanBytes -Path $path
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
    if ($ExpectedHash -and $hash -cne $ExpectedHash) { throw 'WINDOWS_SQL_RESUME_INITIAL_STATE_CHANGED' }
    return [pscustomobject]@{ Value = $value; Sha256 = $hash }
}

function Assert-WindowsSqlResumeStart {
    param($Adapter, $State, $Snapshot, $Observation)
    $plan = $Adapter.Plan
    $completed = @('SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService', 'CreateDatabase')
    if ($State.Status -cne 'Failed' -or $State.FailedOperation -cne 'ApplyMigrations' -or
        $State.PendingOperation -cne 'ApplyMigrations' -or $null -ne $State.FirewallRule -or
        ($State.CompletedChanges -join '|') -cne ($completed -join '|') -or
        $State.CompletedChanges.Count -ne 5 -or
        ($State.Snapshot | ConvertTo-Json -Depth 20 -Compress) -cne
        ($Snapshot | ConvertTo-Json -Depth 20 -Compress)) { throw 'WINDOWS_SQL_RESUME_INITIAL_STATE_CHANGED' }
    if ($Adapter.ResumeContext.Binding.Version -eq 2) {
        Assert-WindowsSqlPreviousProvenance -Adapter $Adapter -State $State
    }
    elseif ($null -ne (Get-WindowsSqlField -Object $State -Name 'ExecutionPlanHash')) {
        throw 'WINDOWS_SQL_RESUME_INITIAL_STATE_CHANGED'
    }
    if (-not (Test-WindowsSqlObservation -Observation $Observation) -or
        -not (Test-WindowsSqlConfigurationEqual -Left $State.ExpectedConfiguration `
                -Right $State.TargetConfiguration) -or
        -not (Test-WindowsSqlConfigurationEqual -Left $Observation.Configuration `
                -Right $State.ExpectedConfiguration) -or
        $State.TargetConfiguration.LoginMode -cne 'Mixed' -or -not $State.TargetConfiguration.TcpEnabled -or
        $State.TargetConfiguration.DynamicPorts -or $State.TargetConfiguration.TcpPort -ne $plan.Port -or
        $State.TargetConfiguration.CertificateThumbprint -cne $plan.CertificateThumbprint -or
        $Observation.Database.Name -cne $plan.Database -or -not $Observation.Database.Exists -or
        $Observation.Database.OwnedLifetimeId -cne $plan.LifetimeId -or $Observation.Database.SchemaVersion -ne 0 -or
        $Observation.Login.Name -cne $plan.RuntimeLogin -or $Observation.Login.Exists -or
        $Observation.Port.Port -ne $plan.Port -or -not $Observation.Port.InUse -or
        -not $Observation.Port.OwnedByInstance -or $Observation.Network.Mode -cne 'Mirrored' -or
        $null -ne $Observation.Network.OwnedFirewallRule) { throw 'WINDOWS_SQL_RESUME_OBSERVATION_CHANGED' }
    if ($Snapshot.ServiceAccount -cne $Adapter.Runtime.ServiceAccount -or
        $State.OriginalConfiguration.ForceEncryption -ne $State.ExpectedConfiguration.ForceEncryption -or
        -not (Test-WindowsSqlConfigurationEqual -Left $State.OriginalConfiguration `
                -Right (ConvertTo-WindowsSqlConfiguration -Registry $Snapshot.Registry))) {
        throw 'WINDOWS_SQL_RESUME_INITIAL_STATE_CHANGED'
    }
    # Never adopt the current registry as an unexamined expected value.
    $expectedRegistry = $Snapshot.Registry | ConvertTo-Json | ConvertFrom-Json
    $expectedRegistry.LoginMode = 2
    $expectedRegistry.Enabled = 1
    $expectedRegistry.TcpPort = [string]$plan.Port
    $expectedRegistry.TcpDynamicPorts = ''
    $expectedRegistry.Certificate = $plan.CertificateThumbprint.ToLowerInvariant()
    $Adapter.ExpectedRegistry = $expectedRegistry
    Assert-WindowsSqlRegistryUnchanged -Adapter $Adapter
    $service = Get-WindowsSqlService -Adapter $Adapter
    if ($service.StartMode -cne 'Auto') { throw 'WINDOWS_SQL_SERVICE_CHANGED' }
    if ($Adapter.Sources.Sources.Count -ne 5 -or
        (($Adapter.Sources.Sources | ForEach-Object Version) -join ',') -cne '1,2,3,4,5' -or
        $Adapter.Sources.Bundle.ReleaseVersion -ne 5) { throw 'WINDOWS_SQL_SOURCE_READER_FAILED' }
}

function New-WindowsSqlResumeAttempt {
    param($Adapter, [string]$ResourceReceiptHash)
    $context = $Adapter.ResumeContext
    $attempt = [pscustomobject]@{
        LifetimeId = $Adapter.Plan.LifetimeId
        PlanHash = $Adapter.Plan.PlanHash
        ExecutionPlanHash = $context.ExecutionPlan.PlanHash
        ExecutionBindingHash = $context.BindingHash
        AttemptId = [Guid]::NewGuid().ToString('N')
        StartedUtc = [DateTimeOffset]::UtcNow.ToString('O')
        InitialLifecycleHash = $context.Binding.InitialLifecycleHash
        SnapshotHash = $context.Binding.SnapshotHash
        ResourceReceiptHash = $ResourceReceiptHash
        EntryHash = $context.Binding.Entry.Sha256
        Approval = $context.Approval
    }
    if ($context.Binding.Version -eq 2) {
        $previous = Read-WindowsSqlPreviousAttempt -Adapter $Adapter
        $attempt | Add-Member -NotePropertyMembers @{
            PreviousBindingHash = $context.Binding.PreviousBinding.Sha256
            PreviousAttemptHash = $context.Binding.PreviousAttempt.Sha256
            PreviousAttemptId = $previous.AttemptId
        }
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($attempt | ConvertTo-Json -Depth 10 -Compress))
    Write-WindowsSqlResumeOnce -Adapter $Adapter -Name (Get-WindowsSqlResumeAttemptName -Adapter $Adapter) -Bytes $bytes
    if ($context.Binding.Version -eq 2) {
        # Archive the failed journal's EXACT bytes before replacing it. A failed archive consumes the attempt too.
        $bytes = Read-WindowsSqlResumeArtifact -Path (Join-Path $Adapter.Plan.PrivateRoot 'lifecycle.json') `
            -Sha256 $context.Binding.InitialLifecycleHash
        Write-WindowsSqlResumeOnce -Adapter $Adapter -Name (Get-WindowsSqlResumeInitialName -Adapter $Adapter) -Bytes $bytes
    }
    return $attempt
}

function Write-WindowsSqlResumeOnce {
    param($Adapter, [string]$Name, [byte[]]$Bytes)
    Assert-WindowsSqlPrivateAcl -Path $Adapter.Plan.PrivateRoot -Sid $Adapter.Runtime.ExecutorSid
    $path = Join-Path $Adapter.Plan.PrivateRoot $Name
    [void](Assert-WindowsSqlLocalPath -Path $path -AllowMissing)
    $stream = $null
    try {
        # CreateNew is the one-shot boundary; even an incomplete attempt is never overwritten or retried.
        $stream = [IO.FileStream]::new($path, 'CreateNew', 'Write', 'None', 4096, 'WriteThrough')
        $stream.Write($Bytes)
        $stream.Flush($true)
    }
    finally { if ($null -ne $stream) { $stream.Dispose() } }
    Set-WindowsSqlPrivateFileOwner -Path $path -Sid $Adapter.Runtime.ExecutorSid
}

function New-WindowsSqlResumedState {
    param($Adapter, $Previous, $Attempt, [string]$Status, $Completed, $Pending, $Failed)
    $state = New-WindowsSqlState -Plan $Adapter.Plan -Status $Status -Snapshot $Previous.Snapshot `
        -Target $Previous.TargetConfiguration -Original $Previous.OriginalConfiguration `
        -Expected $Previous.ExpectedConfiguration -Completed $Completed -PendingOperation $Pending `
        -FailedOperation $Failed -FirewallRule $null
    $state | Add-Member -NotePropertyMembers @{
        ExecutionPlanHash = $Adapter.ResumeContext.ExecutionPlan.PlanHash
        ExecutionBindingHash = $Adapter.ResumeContext.BindingHash
        AttemptId = $Attempt.AttemptId
    }
    if ($Adapter.ResumeContext.Binding.Version -eq 2) {
        $state | Add-Member -NotePropertyMembers @{
            PreviousBindingHash = $Attempt.PreviousBindingHash
            PreviousAttemptHash = $Attempt.PreviousAttemptHash
            PreviousAttemptId = $Attempt.PreviousAttemptId
        }
    }
    return $state
}

function Invoke-WindowsSqlRemainingMigrations {
    param($Adapter, $Previous, $Attempt)
    $completed = [Collections.Generic.List[string]]::new()
    foreach ($change in $Previous.CompletedChanges) { $completed.Add($change) }
    $pending = $null
    $operation = 'WriteLifecycleState'
    $arguments = @{ Adapter = $Adapter; Previous = $Previous; Attempt = $Attempt }
    try {
        foreach ($change in @('ApplyMigrations', 'CreateRuntimeLogin')) {
            $pending = $change
            $operation = 'WriteLifecycleState'
            $state = New-WindowsSqlResumedState @arguments -Status 'Applying' -Completed $completed.ToArray() `
                -Pending $pending -Failed $null
            Write-WindowsSqlState -Adapter $Adapter -State $state -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
            $operation = $change
            Invoke-WindowsSqlMutation -Adapter $Adapter -Operation $change `
                -Request (New-WindowsSqlRequest -Plan $Adapter.Plan) -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
            $completed.Add($change)
            $pending = $null
        }
        $operation = 'WriteLifecycleState'
        $state = New-WindowsSqlResumedState @arguments -Status 'Applied' -Completed $completed.ToArray() `
            -Pending $null -Failed $null
        Write-WindowsSqlState -Adapter $Adapter -State $state -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
        return 'Applied'
    }
    catch {
        $code = switch -CaseSensitive ($operation) {
            'ApplyMigrations' { 'WINDOWS_SQL_APPLY_FAILED_APPLY_MIGRATIONS' }
            'CreateRuntimeLogin' { 'WINDOWS_SQL_APPLY_FAILED_CREATE_RUNTIME_LOGIN' }
            default { 'WINDOWS_SQL_APPLY_FAILED_WRITE_LIFECYCLE_STATE' }
        }
        if ($operation -ceq 'ApplyMigrations') {
            $migrationCode = Get-WindowsSqlMigrationFailureCode -Exception $_.Exception
            if ($null -ne $migrationCode) { $code = $migrationCode }
        }
        $state = New-WindowsSqlResumedState @arguments -Status 'Failed' -Completed $completed.ToArray() `
            -Pending $pending -Failed $operation
        try { Write-WindowsSqlState -Adapter $Adapter -State $state -FailureCode 'WINDOWS_SQL_APPLY_FAILED' }
        catch { throw ($code + '_WRITE_FAILURE_JOURNAL') }
        throw $code
    }
}

function Invoke-WindowsSqlResume {
    param([string]$BindingPath, [string]$BindingHash, [string]$EntryPath, [string]$Mode, [string]$Approval)
    $context = Read-WindowsSqlResumeContext -BindingPath $BindingPath -BindingHash $BindingHash `
        -EntryPath $EntryPath -Mode $Mode -Approval $Approval
    $adapter = $null
    try {
        $adapter = New-WindowsSqlAdapter -Plan $context.OwnershipPlan -RuntimePath $context.Binding.RuntimePath `
            -ResumeContext $context
        $snapshot = Read-WindowsSqlResumePrivateEvidence -Adapter $adapter -Name 'snapshot.json' `
            -ExpectedHash $context.Binding.SnapshotHash
        $initialHash = if ($Mode -ceq 'Resume') { $context.Binding.InitialLifecycleHash } else { '' }
        $journal = Read-WindowsSqlResumePrivateEvidence -Adapter $adapter -Name 'lifecycle.json' `
            -ExpectedHash $initialHash
        $observation = Read-WindowsSqlObservation -Plan $adapter.Plan -Adapter $adapter `
            -FailureCode 'WINDOWS_SQL_APPLY_FAILED'
        if ($Mode -ceq 'Resume') {
            Assert-WindowsSqlResumeStart -Adapter $adapter -State $journal.Value -Snapshot $snapshot.Value `
                -Observation $observation
            $resource = Read-WindowsSqlResumePrivateEvidence -Adapter $adapter -Name 'resources.json' -ExpectedHash ''
            $receipt = $resource.Value
            if ($context.Binding.Version -eq 2) {
                $previousAttempt = Read-WindowsSqlPreviousAttempt -Adapter $adapter
                if ($previousAttempt.ResourceReceiptHash -cne $resource.Sha256) {
                    throw 'WINDOWS_SQL_RESUME_RESOURCE_CHANGED'
                }
                Assert-WindowsSqlResumeDatabaseEmpty -Adapter $adapter
            }
            $databaseGuid = [Guid]::Empty
            if (-not [Guid]::TryParse([string]$receipt.DatabaseGuid, [ref]$databaseGuid) -or
                $databaseGuid -eq [Guid]::Empty -or $null -ne $receipt.LoginSid -or
                $null -ne $receipt.CredentialHash -or $null -ne $receipt.Firewall -or
                $receipt.DatabaseGuid -cne (Get-WindowsSqlDatabaseGuid -Adapter $adapter) -or
                (Test-Path -LiteralPath $adapter.Runtime.CredentialPath)) {
                throw 'WINDOWS_SQL_RESUME_RESOURCE_CHANGED'
            }
            Assert-WindowsSqlApproval -Plan $context.ExecutionPlan
            $attempt = New-WindowsSqlResumeAttempt -Adapter $adapter -ResourceReceiptHash $resource.Sha256
            $status = Invoke-WindowsSqlRemainingMigrations -Adapter $adapter -Previous $journal.Value -Attempt $attempt
        }
        else {
            Assert-WindowsSqlResumeProvenance -Adapter $adapter -State $journal.Value
            $completed = 'SetMixedAuthentication|SetTcpPort|SetServerCertificate|RestartService|' +
                'CreateDatabase|ApplyMigrations|CreateRuntimeLogin'
            if ($journal.Value.Status -cne 'Applied' -or
                ($journal.Value.CompletedChanges -join '|') -cne $completed -or
                $null -ne $journal.Value.PendingOperation -or $null -ne $journal.Value.FailedOperation -or
                $null -ne $journal.Value.FirewallRule -or
                ($journal.Value.Snapshot | ConvertTo-Json -Depth 20 -Compress) -cne
                ($snapshot.Value | ConvertTo-Json -Depth 20 -Compress)) {
                throw 'WINDOWS_SQL_RESUME_SUCCESS_REQUIRED'
            }
            if ($Mode -ceq 'Verify') {
                Assert-WindowsSqlVerification -Plan $adapter.Plan -Observation $observation -Adapter $adapter
                $status = 'Verified'
            }
            else { $status = Invoke-WindowsSqlCleanup -Plan $adapter.Plan -Observation $observation -Adapter $adapter }
        }
        return [pscustomobject]@{
            Step = $Mode
            Status = $status
            OwnershipPlanHash = $adapter.Plan.PlanHash
            ExecutionPlanHash = $context.ExecutionPlan.PlanHash
            ExecutionBindingHash = $context.BindingHash
        }
    }
    finally { if ($null -ne $adapter) { Close-WindowsSqlAdapter -Adapter $adapter } }
}
