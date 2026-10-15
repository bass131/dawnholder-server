#requires -Version 7.0
# Independent secure lifetime. Legacy G2 tools and manifests keep their existing API and behavior.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ApprovalPlanPath,
    [Parameter(Mandatory)][string]$ExpectedApprovalPlanHash,
    [Parameter(Mandatory)][ValidateSet('Prepare', 'Start', 'Bootstrap', 'Runtime', 'Stop', 'Audit', 'Remove')][string]$Step
)

. (Join-Path $PSScriptRoot 'LinuxSql.Common.ps1')
. (Join-Path $PSScriptRoot 'New-LinuxSqlSecrets.ps1')
$plan = $null
$lock = $null
$started = $false

function Invoke-EnvironmentDocker {
    param([string[]]$Arguments, [int]$Seconds = 30, [int]$OutputLimit = 65536, [switch]$IncludeError)
    $result = Invoke-LinuxSqlProcess -FileName 'docker.exe' -Arguments $Arguments -Seconds $Seconds -OutputLimit $OutputLimit
    $admin = $null
    $sanitized = $null
    try {
        $sanitized = $result.Error
        $adminMatches = 0
        $adminPath = Join-Path $plan.PrivateRoot 'sa.txt'
        if (Test-Path -LiteralPath $adminPath) {
            Assert-LinuxSqlPrepared -Plan $plan
            $admin = [IO.File]::ReadAllText($adminPath)
            if ($admin -cnotmatch '^Dh1![0-9A-F]{64}$') { throw 'LINUX_SQL_ADMIN_SECRET_INVALID' }
            $adminMatches = [regex]::Matches($sanitized, [regex]::Escape($admin)).Count
            $sanitized = $sanitized.Replace($admin, '[REDACTED_SA]')
        }
        # Runtime secrets use the same generated shape; never publish a key or an unrecognized secret envelope.
        $otherMatches = [regex]::Matches($sanitized, 'Dh1![0-9A-F]{64}').Count
        $sanitized = [regex]::Replace($sanitized, 'Dh1![0-9A-F]{64}', '[REDACTED_SECRET]')
        if ($sanitized -match 'PRIVATE KEY|Dh1!|MSSQL_SA_PASSWORD|<SS>') {
            throw 'LINUX_SQL_DOCKER_DIAGNOSTIC_UNSAFE'
        }
        Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot ('docker-stderr-' + [guid]::NewGuid().ToString('N') + '.json')) `
            -Value @{
            PublicArguments = $Arguments; ExitCode = $result.ExitCode; Pid = $result.Pid; BirthUtc = $result.BirthUtc
            ObservedUtc = [DateTimeOffset]::UtcNow.ToString('o'); SanitizedStderr = $sanitized
            SaReplacements = $adminMatches; OtherSecretReplacements = $otherMatches
            OriginalCharacters = $result.Error.Length; SanitizedCharacters = $sanitized.Length
            OriginalSha256 = Get-LinuxSqlHash -Bytes ([Text.Encoding]::Unicode.GetBytes($result.Error))
            SanitizedSha256 = Get-LinuxSqlHash -Bytes ([Text.Encoding]::Unicode.GetBytes($sanitized))
            HashEncoding = 'UTF-16LE'; Policy = 'Bounded stderr copy; SA and generated runtime secrets redacted'
        }
    } finally {
        $admin = $null
        $sanitized = $null
    }
    if ($result.ExitCode -ne 0) { throw 'LINUX_SQL_DOCKER_FAILED' }
    if ($IncludeError) { return ($result.Output + $result.Error).Trim() }
    if ($result.Error) { throw 'LINUX_SQL_DOCKER_DIAGNOSTIC_SUPPRESSED' }
    return $result.Output.Trim()
}

function Read-EnvironmentRecord {
    param([string]$Name)
    $record = Get-Content -LiteralPath (Join-Path $plan.EvidenceRoot $Name) -Raw | ConvertFrom-Json
    if ($record.LifetimeId -cne $plan.LifetimeId -or $record.PlanHash -cne $plan.PlanHash) {
        throw 'LINUX_SQL_RECORD_IDENTITY_CHANGED'
    }
    return $record
}

function ConvertTo-EnvironmentBindPath {
    param([string]$Path)
    $normalized = $Path.Replace('\', '/')
    # Docker Desktop can report its internal spelling of the same Windows bind source.
    if ($normalized -cmatch '^/(run/desktop/mnt/host|host_mnt)/([a-z])/(.+)$') {
        return ($Matches[2] + ':/' + $Matches[3])
    }
    return $normalized
}

function Assert-EnvironmentOwnedContainer {
    $created = Read-EnvironmentRecord -Name 'container-created.json'
    if ($created.ContainerId -cnotmatch '^[a-f0-9]{64}$') { throw 'LINUX_SQL_CONTAINER_ID_INVALID' }
    $format = '{{.Id}}|{{.Name}}|{{index .Config.Labels "dawnholder.lifetime"}}|{{.Config.Hostname}}'
    $actual = Invoke-EnvironmentDocker -Arguments @('container', 'inspect', '--format', $format, $created.ContainerId)
    $expected = $created.ContainerId + '|/' + $plan.ContainerName + '|' + $plan.LifetimeId + '|' + $plan.ContainerHostname
    if ($actual -cne $expected) { throw 'LINUX_SQL_CONTAINER_IDENTITY_CHANGED' }
    $intent = Read-EnvironmentRecord -Name 'create-intent.json'
    $format = '{"Image":{{json .Image}},"Memory":{{json .HostConfig.Memory}},' +
    '"Restart":{{json .HostConfig.RestartPolicy.Name}},"Ports":{{json .HostConfig.PortBindings}},' +
    '"Mounts":{{json .Mounts}},"Log":{{json .HostConfig.LogConfig}},"User":{{json .Config.User}}}'
    $security = (Invoke-EnvironmentDocker -Arguments @(
            'container', 'inspect', '--format', $format, $created.ContainerId)) | ConvertFrom-Json
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot ('container-security-' + [guid]::NewGuid().ToString('N') + '.json')) `
        -Value @{ ContainerId = $created.ContainerId; ObservedUtc = [DateTimeOffset]::UtcNow.ToString('o'); Security = $security }
    $maxSize = $security.Log.Config.PSObject.Properties['max-size']
    $port = @($security.Ports.'1433/tcp')
    if ($security.Image -cne $intent.ImageId -or $security.Memory -ne ($plan.ContainerMemoryMb * 1MB) -or
        $security.Restart -cne 'no' -or $security.User -cne $plan.ImageDefaultUser -or $port.Count -ne 1 -or
        $port[0].HostIp -cne '127.0.0.1' -or $port[0].HostPort -cne [string]$plan.Port -or
        @($security.Ports.PSObject.Properties).Count -ne 1 -or $security.Log.Type -cne 'json-file' -or
        ($null -ne $maxSize -and -not [string]::IsNullOrEmpty([string]$maxSize.Value)) -or
        $security.Log.Config.'max-file' -cne '1') { throw 'LINUX_SQL_CONTAINER_SECURITY_CHANGED' }
    $mounts = @{
        '/var/opt/mssql' = $plan.VolumeName
        '/bootstrap/sa.txt' = Join-Path $plan.PrivateRoot 'sa.txt'
        '/bootstrap/server.key' = Join-Path $plan.PrivateRoot 'server.key'
        '/bootstrap/server.pem' = Join-Path $plan.PrivateRoot 'server.pem'
        '/var/opt/mssql/mssql.conf' = Join-Path $plan.PrivateRoot 'mssql.conf'
        '/bootstrap/start.sh' = Join-Path $PSScriptRoot 'start-linux-sql.sh'
    }
    foreach ($destination in $mounts.Keys) {
        $rows = @($security.Mounts | Where-Object Destination -CEQ $destination)
        if ($rows.Count -ne 1) { throw 'LINUX_SQL_CONTAINER_MOUNT_CHANGED' }
        if ($destination -ceq '/var/opt/mssql') {
            if ($rows[0].Type -cne 'volume' -or $rows[0].Name -cne $plan.VolumeName -or -not $rows[0].RW) {
                throw 'LINUX_SQL_CONTAINER_VOLUME_CHANGED'
            }
        } elseif ($rows[0].Type -cne 'bind' -or $rows[0].RW -or
            (ConvertTo-EnvironmentBindPath -Path $rows[0].Source) -ine
            (ConvertTo-EnvironmentBindPath -Path $mounts[$destination])) {
            throw 'LINUX_SQL_CONTAINER_BIND_CHANGED'
        }
    }
    if (@($security.Mounts | Where-Object {
                -not $mounts.ContainsKey($_.Destination) -and
                -not ($_.Type -ceq 'tmpfs' -and $_.Destination -ceq '/run/dawnholder')
            }).Count) { throw 'LINUX_SQL_CONTAINER_EXTRA_MOUNT' }
    return $created.ContainerId
}

function Assert-EnvironmentFresh {
    param([switch]$IncludePrivate)
    # Filter by the exact new names. Do not inventory or inspect any protected container/volume.
    $containers = Invoke-EnvironmentDocker -Arguments @(
        'container', 'ls', '-a', '--filter', ('name=^/' + $plan.ContainerName + '$'), '--format', '{{.ID}}')
    $volumes = Invoke-EnvironmentDocker -Arguments @(
        'volume', 'ls', '--filter', ('name=^' + $plan.VolumeName + '$'), '--format', '{{.Name}}')
    if ($containers -or $volumes -or ($IncludePrivate -and (Test-Path -LiteralPath $plan.PrivateRoot))) {
        throw 'LINUX_SQL_FRESH_NAME_OCCUPIED'
    }
    $listeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
    if (@($listeners | Where-Object Port -EQ $plan.Port).Count) {
        throw 'LINUX_SQL_PORT_OCCUPIED'
    }
}

function Assert-EnvironmentIdle {
    # A conservative busy gate: never stop another dotnet process to make the gate pass.
    $native = @(Get-Process -Name dotnet, MSBuild -ErrorAction SilentlyContinue)
    $linux = Invoke-LinuxSqlProcess -FileName 'wsl.exe' -Arguments @(
        '-d', 'Ubuntu', '--exec', 'pgrep', '-x', '-c', 'dotnet')
    if ($linux.ExitCode -notin @(0, 1) -or $linux.Output.Trim() -notmatch '^\d+$') {
        throw 'LINUX_SQL_HEAVY_STATE_UNKNOWN'
    }
    if ($native.Count -gt 0 -or [int]$linux.Output.Trim() -gt 0) { throw 'LINUX_SQL_HEAVY_RESOURCE_BUSY' }
}

function Start-EnvironmentContainer {
    Assert-EnvironmentFresh
    Assert-LinuxSqlPrepared -Plan $plan
    $format = '{{json .RepoDigests}}|{{.Id}}'
    $image = Invoke-EnvironmentDocker -Arguments @('image', 'inspect', '--format', $format, $plan.Image)
    if (-not $image.Contains($plan.Image) -or $image.Split('|')[-1] -cnotmatch '^sha256:[a-f0-9]{64}$') {
        throw 'LINUX_SQL_IMAGE_NOT_LOCAL_OR_CHANGED'
    }
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'create-intent.json') -Value @{
        PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; ImageId = $image.Split('|')[-1]
    }
    $volume = Invoke-EnvironmentDocker -Arguments @(
        'volume', 'create', '--label', ('dawnholder.lifetime=' + $plan.LifetimeId), $plan.VolumeName)
    if ($volume -cne $plan.VolumeName) { throw 'LINUX_SQL_VOLUME_CREATE_UNCONFIRMED' }
    $volumeOwner = Invoke-EnvironmentDocker -Arguments @('volume', 'inspect', '--format',
        '{{index .Labels "dawnholder.lifetime"}}', $plan.VolumeName)
    if ($volumeOwner -cne $plan.LifetimeId) { throw 'LINUX_SQL_VOLUME_OWNER_CHANGED' }
    $script = Join-Path $PSScriptRoot 'start-linux-sql.sh'
    # The fixed image's observed default is in the approved plan; do not override a different spelling.
    $userArguments = @()
    if ($plan.ImageDefaultUser -ceq '10001:0') { $userArguments = @('--user', '10001:0') }
    $args = @(
        'container', 'create', '--pull=never', '--name', $plan.ContainerName, '--hostname', $plan.ContainerHostname,
        '--label', ('dawnholder.lifetime=' + $plan.LifetimeId), '--restart=no',
        '--memory', ($plan.ContainerMemoryMb.ToString() + 'm'), '--memory-swap', ($plan.ContainerMemoryMb.ToString() + 'm'),
        '--publish', ('127.0.0.1:' + $plan.Port + ':1433'),
        '--tmpfs', '/run/dawnholder:rw,noexec,nosuid,nodev,uid=10001,gid=0,mode=0700,size=1m',
        '--mount', ('type=volume,source=' + $plan.VolumeName + ',target=/var/opt/mssql'),
        '--mount', ('type=bind,source=' + (Join-Path $plan.PrivateRoot 'sa.txt') + ',target=/bootstrap/sa.txt,readonly'),
        '--mount', ('type=bind,source=' + (Join-Path $plan.PrivateRoot 'server.key') + ',target=/bootstrap/server.key,readonly'),
        '--mount', ('type=bind,source=' + (Join-Path $plan.PrivateRoot 'server.pem') + ',target=/bootstrap/server.pem,readonly'),
        '--mount', ('type=bind,source=' + (Join-Path $plan.PrivateRoot 'mssql.conf') + ',target=/var/opt/mssql/mssql.conf,readonly'),
        '--mount', ('type=bind,source=' + $script + ',target=/bootstrap/start.sh,readonly'),
        '--env', 'ACCEPT_EULA=Y', '--env', 'MSSQL_PID=Developer',
        '--env', ('MSSQL_MEMORY_LIMIT_MB=' + $plan.SqlMemoryMb), '--env', 'MSSQL_TCP_PORT=1433',
        '--log-driver', 'json-file', '--log-opt', 'max-file=1',
        '--entrypoint', '/bin/bash'
    ) + $userArguments + @($plan.Image, '/bootstrap/start.sh')
    $id = Invoke-EnvironmentDocker -Arguments $args -Seconds 60
    if ($id -cnotmatch '^[a-f0-9]{64}$') { throw 'LINUX_SQL_CREATE_UNCONFIRMED' }
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'container-created.json') -Value @{
        PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; ContainerId = $id
        CreatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    }
    [void](Assert-EnvironmentOwnedContainer)
    [void](Invoke-EnvironmentDocker -Arguments @('container', 'start', $id) -Seconds 60)
}

function Invoke-EnvironmentBootstrap {
    $ready = $false
    for ($attempt = 1; $attempt -le $plan.ReadyAttempts; $attempt++) {
        try {
            $observation = & (Join-Path $PSScriptRoot 'Invoke-LinuxSqlHost.ps1') `
                -ApprovalPlanPath $plan.PlanPath -ExpectedApprovalPlanHash $plan.PlanHash -Operation ready
            $ready = $true
            Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'ready.json') -Value $observation
            break
        } catch {
            # Only these completed transient connect failures retry: path unavailable/refused/wait timeout/pipe closed.
            # General code 0, login, identity and TLS failures are not a readiness signal; child timeouts also stop.
            $reason = [string]$_.Exception.Message
            Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot ('ready-' + $attempt + '.json')) -Value @{
                Attempt = $attempt
                Reason = $(if ($reason -cmatch '^LINUX_SQL_(?:[A-Z0-9_]+|HOST_FAILED_-[0-9]+)$') {
                        $reason
                    } else { 'SUPPRESSED' })
            }
            if ($reason -cnotmatch '^LINUX_SQL_HOST_FAILED_(53|10061|258|233)$') { throw }
            if ($attempt -lt $plan.ReadyAttempts) { Start-Sleep -Seconds 5 }
        }
    }
    if (-not $ready) { throw 'LINUX_SQL_READY_LIMIT' }
    $bootstrap = & (Join-Path $PSScriptRoot 'Invoke-LinuxSqlHost.ps1') `
        -ApprovalPlanPath $plan.PlanPath -ExpectedApprovalPlanHash $plan.PlanHash -Operation bootstrap
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'bootstrap.json') -Value $bootstrap
    $disabled = & (Join-Path $PSScriptRoot 'Invoke-LinuxSqlHost.ps1') `
        -ApprovalPlanPath $plan.PlanPath -ExpectedApprovalPlanHash $plan.PlanHash -Operation sa-disabled
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'sa-disabled.json') -Value $disabled
}

function Stop-EnvironmentContainer {
    $id = Assert-EnvironmentOwnedContainer
    [void](Invoke-EnvironmentDocker -Arguments @('container', 'stop', '--time', $plan.StopSeconds, $id) -Seconds 60)
    $status = Invoke-EnvironmentDocker -Arguments @('container', 'inspect', '--format', '{{.State.Status}}', $id)
    if ($status -cne 'exited') { throw 'LINUX_SQL_STOP_UNCONFIRMED' }
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'stopped.json') -Value @{
        PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; ContainerId = $id
        ObservedStatus = $status; ObservedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    }
}

function Invoke-EnvironmentAudit {
    $stopped = Read-EnvironmentRecord -Name 'stopped.json'
    $id = Assert-EnvironmentOwnedContainer
    $status = Invoke-EnvironmentDocker -Arguments @('container', 'inspect', '--format', '{{.State.Status}}', $id)
    if ($id -cne $stopped.ContainerId -or $status -cne 'exited') { throw 'LINUX_SQL_AUDIT_WRITER_ACTIVE' }
    Assert-EnvironmentIdle
    Assert-LinuxSqlPrepared -Plan $plan
    $secrets = @()
    try {
        $secrets = @([IO.File]::ReadAllText((Join-Path $plan.PrivateRoot 'sa.txt')))
        if ($secrets[0] -cnotmatch '^Dh1![0-9A-F]{64}$') { throw 'LINUX_SQL_ADMIN_SECRET_INVALID' }
        # Child launches inherit this environment; secret handoffs use stdin and are never environment updates.
        foreach ($value in [Environment]::GetEnvironmentVariables('Process').Values) {
            if ([string]$value -and ([string]$value).Contains($secrets[0])) {
                throw 'LINUX_SQL_SECRET_IN_HOST_ENVIRONMENT'
            }
        }
        $logs = Invoke-EnvironmentDocker -Arguments @('container', 'logs', $id) -OutputLimit 8388608 -IncludeError
        $config = Invoke-EnvironmentDocker -Arguments @(
            'container', 'inspect', '--format', '{{json .Config.Env}}|{{json .Config.Cmd}}|{{json .Config.Entrypoint}}', $id)
        $checkedFiles = @()
        if (@(Get-ChildItem -LiteralPath $plan.EvidenceRoot -Directory).Count -ne 0) {
            throw 'LINUX_SQL_AUDIT_NESTED_EVIDENCE_UNREVIEWED'
        }
        foreach ($file in @(Get-ChildItem -LiteralPath $plan.EvidenceRoot -File)) {
            if ($file.Length -gt 8388608 -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'LINUX_SQL_AUDIT_FILE_BOUNDARY'
            }
            if ($file.Name -ceq 'lifetime.lock' -and $file.Length -eq 0) { continue }
            $text = [IO.File]::ReadAllText($file.FullName)
            foreach ($secret in $secrets) {
                if ($secret -and $text.Contains($secret)) { throw 'LINUX_SQL_SECRET_IN_EVIDENCE' }
            }
            if ($text.Contains('PRIVATE KEY-----')) { throw 'LINUX_SQL_KEY_IN_EVIDENCE' }
            $checkedFiles += @{ Name = $file.Name; Sha256 = (Get-FileHash -LiteralPath $file.FullName).Hash }
        }
        foreach ($secret in $secrets) {
            if ($secret -and ($logs.Contains($secret) -or $config.Contains($secret))) { throw 'LINUX_SQL_SECRET_IN_DOCKER_OUTPUT' }
        }
        if ($logs.Contains('PRIVATE KEY-----') -or $config.Contains('MSSQL_SA_PASSWORD')) {
            throw 'LINUX_SQL_BOOTSTRAP_ENV_OR_KEY_EXPOSED'
        }
        Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'audit.json') -Value @{
            PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; ContainerId = $id
            Closed = $true; CheckedFiles = $checkedFiles; DockerLogCharacters = $logs.Length
            HostEnvironmentChecked = $true
            Scope = 'SA only: closed own launch argv/log evidence, host environment, Docker stdout/stderr and config argv/env'
            ObservedUtc = [DateTimeOffset]::UtcNow.ToString('o')
        }
    } finally {
        $secrets = $null
        $logs = $null
        $config = $null
    }
}

function Remove-EnvironmentResources {
    $audit = Read-EnvironmentRecord -Name 'audit.json'
    $id = Assert-EnvironmentOwnedContainer
    if ($audit.Closed -ne $true -or $audit.ContainerId -cne $id) { throw 'LINUX_SQL_CLEANUP_AUDIT_REQUIRED' }
    $status = Invoke-EnvironmentDocker -Arguments @('container', 'inspect', '--format', '{{.State.Status}}', $id)
    if ($status -cne 'exited') { throw 'LINUX_SQL_CLEANUP_RUNNING' }
    $label = Invoke-EnvironmentDocker -Arguments @('volume', 'inspect', '--format',
        '{{index .Labels "dawnholder.lifetime"}}', $plan.VolumeName)
    if ($label -cne $plan.LifetimeId) { throw 'LINUX_SQL_VOLUME_OWNER_CHANGED' }
    Assert-LinuxSqlPrepared -Plan $plan
    [void](Invoke-EnvironmentDocker -Arguments @('container', 'rm', $id))
    [void](Invoke-EnvironmentDocker -Arguments @('volume', 'rm', $plan.VolumeName))
    # Exact leaf deletes only. A foreign file or link prevents removing the root; no recursive cleanup.
    foreach ($name in @('sa.txt', 'runtime.clixml', 'server.key', 'server.pem', 'ca.pem', 'mssql.conf', 'owner.json')) {
        $path = [IO.Path]::GetFullPath((Join-Path $plan.PrivateRoot $name))
        if ([IO.Path]::GetDirectoryName($path) -cne [IO.Path]::GetFullPath($plan.PrivateRoot)) {
            throw 'LINUX_SQL_CLEANUP_PATH_INVALID'
        }
        Assert-TestEnvironmentNoReparse -Path $path
        Remove-Item -LiteralPath $path
    }
    [IO.Directory]::Delete((Join-Path $plan.PrivateRoot 'ca-empty'), $false)
    [IO.Directory]::Delete($plan.PrivateRoot, $false)
}

try {
    $plan = Read-LinuxSqlPlan -Path $ApprovalPlanPath -ExpectedHash $ExpectedApprovalPlanHash `
        -Closing:($Step -in @('Stop', 'Audit', 'Remove'))
    if (-not (Test-Path -LiteralPath $plan.EvidenceRoot -PathType Container)) { throw 'LINUX_SQL_EVIDENCE_ROOT_REQUIRED' }
    Assert-TestEnvironmentNoReparse -Path (Join-Path $plan.EvidenceRoot 'lifetime.lock')
    $lock = [IO.File]::Open((Join-Path $plan.EvidenceRoot 'lifetime.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $intentPath = Join-Path $plan.EvidenceRoot ($Step + '-intent.json')
    Write-LinuxSqlJson -Path $intentPath -Value @{
        PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; Step = $Step; StartedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    }
    $started = $true
    if ($Step -in @('Prepare', 'Start', 'Bootstrap', 'Runtime')) {
        Assert-LinuxSqlCapacity -Plan $plan
        Assert-EnvironmentIdle
    }
    switch ($Step) {
        'Prepare' { Assert-EnvironmentFresh -IncludePrivate; New-LinuxSqlSecrets -Plan $plan }
        'Start' { Start-EnvironmentContainer }
        'Bootstrap' { [void](Assert-EnvironmentOwnedContainer); Invoke-EnvironmentBootstrap }
        'Runtime' {
            $bootstrap = Get-Content -LiteralPath (Join-Path $plan.EvidenceRoot 'bootstrap.json') -Raw | ConvertFrom-Json
            if ($bootstrap.Observation.SaDisabled -ne $true) { throw 'LINUX_SQL_BOOTSTRAP_NOT_COMPLETE' }
            $runtime = & (Join-Path $PSScriptRoot 'Invoke-LinuxSqlHost.ps1') `
                -ApprovalPlanPath $plan.PlanPath -ExpectedApprovalPlanHash $plan.PlanHash -Operation runtime
            Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot 'runtime.json') -Value $runtime
        }
        'Stop' { Stop-EnvironmentContainer }
        'Audit' { Invoke-EnvironmentAudit }
        'Remove' { Remove-EnvironmentResources }
    }
    Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot ($Step + '-result.json')) -Value @{
        PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; Step = $Step; Status = 'Succeeded'
        FinishedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    }
} catch {
    $safe = [string]$_.Exception.Message
    if ($safe -cnotmatch '^LINUX_SQL_(?:[A-Z0-9_]+|HOST_FAILED_-[0-9]+)$') {
        $safe = 'LINUX_SQL_STEP_FAILED_PRESERVE'
    }
    if ($started) {
        Write-LinuxSqlJson -Path (Join-Path $plan.EvidenceRoot ($Step + '-failure.json')) -Value @{
            PlanHash = $plan.PlanHash; LifetimeId = $plan.LifetimeId; Step = $Step; Reason = $safe
            FinishedUtc = [DateTimeOffset]::UtcNow.ToString('o'); ResourceState = 'InspectExactOwnerBeforeRetry'
        }
    }
    throw [InvalidOperationException]::new($safe)
} finally {
    if ($null -ne $lock) { $lock.Dispose() }
}
