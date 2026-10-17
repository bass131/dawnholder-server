# Definitions only. The separate v1 Linux plan does not change the legacy G2 v2 contract.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Environment.Common.ps1')

function Get-LinuxSqlHash {
    param([byte[]]$Bytes)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes))
}

function Read-LinuxSqlPlan {
    param([string]$Path, [string]$ExpectedHash, [switch]$Closing)
    if (-not $IsWindows -or $ExpectedHash -cnotmatch '^[A-F0-9]{64}$') {
        throw 'LINUX_SQL_PLAN_IDENTITY_REQUIRED'
    }
    Assert-TestEnvironmentNoReparse -Path $Path
    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -gt 131072 -or (Get-LinuxSqlHash -Bytes $bytes) -cne $ExpectedHash) {
        throw 'LINUX_SQL_PLAN_CHANGED'
    }
    $planText = [Text.UTF8Encoding]::new($false, $true).GetString($bytes)
    $plan = $planText | ConvertFrom-Json
    if ($plan.Version -ne 1 -or $plan.ExecutionApproved -isnot [bool] -or -not $plan.ExecutionApproved -or
        $plan.Encrypt -cne 'Mandatory' -or $plan.TrustServerCertificate -isnot [bool] -or
        $plan.TrustServerCertificate -or $plan.SaContainerEnvironmentException -ne $true -or
        $plan.RemoveNewResourcesAfterAudit -ne $true) {
        throw 'LINUX_SQL_EXECUTION_NOT_APPROVED'
    }
    $document = $null
    try {
        # Read the original offset with the Linux host's JSON API, avoiding PowerShell's DateTime coercion.
        $document = [Text.Json.JsonDocument]::Parse($planText)
        $expiry = $document.RootElement.GetProperty('ExpiresUtc')
        $expiryText = $expiry.GetString()
        if ($expiryText -cnotmatch '(Z|[+-][0-9]{2}:[0-9]{2})$') {
            throw 'LINUX_SQL_PLAN_EXPIRY_INVALID'
        }
        $expiresUtc = $expiry.GetDateTimeOffset()
        $plan.ExpiresUtc = $expiryText
    } catch {
        throw 'LINUX_SQL_PLAN_EXPIRY_INVALID'
    } finally {
        if ($null -ne $document) { $document.Dispose() }
    }
    $now = [DateTimeOffset]::UtcNow
    $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    if ($plan.ExecutorSid -cne $sid -or $plan.Machine -cne [Environment]::MachineName -or
        (-not $Closing -and $expiresUtc -le $now) -or $expiresUtc -gt $now.AddDays(1)) {
        throw 'LINUX_SQL_EXECUTOR_OR_APPROVAL_EXPIRED'
    }
    if ($plan.Database -cnotmatch '^Dawnholder_Test_[A-Za-z0-9_]{1,80}$' -or
        $plan.RuntimeLogin -cnotmatch '^dh_[a-z0-9_]{1,80}$' -or
        $plan.ContainerName -cnotmatch '^[a-z][a-z0-9-]{1,62}$' -or
        $plan.ContainerHostname -cne $plan.ContainerName -or
        $plan.VolumeName -cne ($plan.ContainerName + '-data') -or
        $plan.Image -cnotmatch '^mcr\.microsoft\.com/mssql/server@sha256:[a-f0-9]{64}$' -or
        $plan.Endpoint -cne ('127.0.0.1,' + $plan.Port) -or $plan.Port -lt 1024 -or $plan.Port -gt 65535 -or
        $plan.SqlMemoryMb -ne 3072 -or $plan.ContainerMemoryMb -ne 4096 -or
        $plan.MinPhysicalBytes -lt 6GB -or $plan.MinCommitBytes -lt 4GB -or $plan.MinDiskBytes -lt 10GB -or
        $plan.MaxEditors -ne 1 -or $plan.ConnectSeconds -ne 5 -or $plan.CommandSeconds -ne 30 -or
        $plan.HostSeconds -ne 180 -or $plan.ReadyAttempts -ne 12 -or $plan.StopSeconds -ne 30 -or
        $plan.SlotId -ne 1 -or [guid]$plan.AccountId -eq [guid]::Empty -or
        [guid]$plan.CharacterId -eq [guid]::Empty -or $plan.LifetimeId -cnotmatch '^[a-f0-9]{32}$') {
        throw 'LINUX_SQL_TARGET_OR_LIMIT_INVALID'
    }
    $private = [IO.Path]::GetFullPath($plan.PrivateRoot).TrimEnd('\')
    $privateParent = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Dawnholder')).TrimEnd('\')
    if ([IO.Path]::GetDirectoryName($private) -cne $privateParent -or
        $plan.ProtectedResources -isnot [array] -or $plan.ProtectedResources.Count -lt 3) {
        throw 'LINUX_SQL_PRIVATE_BOUNDARY_INVALID'
    }
    foreach ($protected in $plan.ProtectedResources) {
        if ($plan.ContainerName -ieq $protected.Container -or $plan.VolumeName -ieq $protected.Volume -or
            ($protected.PrivateRoot -and $private -ieq [IO.Path]::GetFullPath($protected.PrivateRoot))) {
            throw 'LINUX_SQL_PROTECTED_RESOURCE'
        }
    }
    foreach ($pathToCheck in @($private, $plan.EvidenceRoot, $plan.SourceRoot)) {
        Assert-TestEnvironmentNoReparse -Path $pathToCheck
        if (-not [IO.Path]::IsPathFullyQualified($pathToCheck)) { throw 'LINUX_SQL_ABSOLUTE_PATH_REQUIRED' }
    }
    if (-not $plan.EvidenceRoot.StartsWith($plan.SourceRoot.TrimEnd('\', '/') + '\.backups\',
            [StringComparison]::OrdinalIgnoreCase) -or
        $plan.LinuxHostDll -cnotmatch '^/home/[^\s]+/LinuxSqlHost\.dll$' -or
        $plan.DotnetPath -cnotmatch '^/home/[^\s]+/dotnet$' -or
        $plan.LinuxHostSha256 -cnotmatch '^[A-F0-9]{64}$' -or
        $plan.DotnetVersion -cne '10.0.301' -or
        $plan.ImageDefaultUser -cnotin @('mssql', '10001:0') -or
        $plan.LinuxBuildRoot -cnotmatch '^/home/[A-Za-z0-9_./-]+$' -or
        $plan.LinuxHostDll -cne ($plan.LinuxBuildRoot + '/publish/LinuxSqlHost.dll')) {
        throw 'LINUX_SQL_EXECUTION_PATH_INVALID'
    }
    $plan | Add-Member NoteProperty PlanPath ([IO.Path]::GetFullPath($Path))
    $plan | Add-Member NoteProperty PlanHash $ExpectedHash
    if (-not $Closing) { Assert-LinuxSqlSources -Plan $plan }
    $script:LinuxSqlEvidenceRoot = $plan.EvidenceRoot
    return $plan
}

function Assert-LinuxSqlSources {
    param($Plan)
    if ($Plan.Inputs.Count -lt 25 -or $Plan.Inputs.Count -gt 128) { throw 'LINUX_SQL_INPUT_SET_INCOMPLETE' }
    $seen = @{}
    foreach ($inputFile in $Plan.Inputs) {
        $relative = [string]$inputFile.Path
        if ($relative -cnotmatch '^[A-Za-z0-9_.\-/]+$' -or $relative.Split('/') -contains '..' -or
            $relative.StartsWith('/') -or $seen.ContainsKey($relative) -or
            $inputFile.Sha256 -cnotmatch '^[A-F0-9]{64}$') { throw 'LINUX_SQL_INPUT_PATH_INVALID' }
        $seen[$relative] = $true
        $path = Join-Path $Plan.SourceRoot $relative
        Assert-TestEnvironmentNoReparse -Path $path
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $inputFile.Sha256) {
            throw 'LINUX_SQL_INPUT_CHANGED'
        }
    }
}

function Write-LinuxSqlJson {
    param([string]$Path, $Value)
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth 20))
    $stream = [IO.File]::Open($Path, 'CreateNew', 'Write', 'None')
    try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
}

function Invoke-LinuxSqlProcess {
    param([string]$FileName, [string[]]$Arguments, [int]$Seconds = 30,
        $InputText = $null, [int]$OutputLimit = 65536, [switch]$BinaryOutput)
    # Omitted/null means EOF only; an explicit empty string sends one empty line. Journal presence, never content.
    $stdinPresent = $null -ne $InputText
    if ($stdinPresent -and $InputText -isnot [string]) { throw 'LINUX_SQL_STDIN_TYPE_INVALID' }
    # Bounded readers keep raw provider/child output in memory only. Callers publish allowlisted facts.
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $FileName
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    if ($BinaryOutput) { $start.StandardOutputEncoding = [Text.Encoding]::Latin1 }
    foreach ($arg in $Arguments) { $start.ArgumentList.Add($arg) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $output = [Text.StringBuilder]::new()
    $errors = [Text.StringBuilder]::new()
    $outBuffer = [char[]]::new(2048)
    $errBuffer = [char[]]::new(2048)
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $childStarted = $false
    $birth = $null
    $completed = $false
    try {
        if (-not $process.Start()) { throw 'LINUX_SQL_CHILD_START_FAILED' }
        $childStarted = $true
        $birth = $process.StartTime.ToUniversalTime().ToString('o')
        $outRead = $process.StandardOutput.ReadAsync($outBuffer, 0, $outBuffer.Length)
        $errRead = $process.StandardError.ReadAsync($errBuffer, 0, $errBuffer.Length)
        if ($stdinPresent) {
            if ([Text.Encoding]::UTF8.GetByteCount($InputText) -gt 4095) { throw 'LINUX_SQL_STDIN_LIMIT' }
            $write = $process.StandardInput.WriteLineAsync($InputText)
            if (-not $write.Wait(5000)) { throw 'LINUX_SQL_STDIN_TIMEOUT_STATE_UNKNOWN' }
        }
        $process.StandardInput.Close()
        while ($null -ne $outRead -or $null -ne $errRead -or -not $process.HasExited) {
            if ($clock.Elapsed.TotalSeconds -gt $Seconds) { throw 'LINUX_SQL_CHILD_TIMEOUT_STATE_UNKNOWN' }
            foreach ($kind in @('out', 'err')) {
                $read = if ($kind -eq 'out') { $outRead } else { $errRead }
                if ($null -eq $read -or -not $read.IsCompleted) { continue }
                $count = $read.GetAwaiter().GetResult()
                # Direct assignment preserves the char[] identity for Append and the next asynchronous read.
                if ($kind -eq 'out') { $buffer = $outBuffer } else { $buffer = $errBuffer }
                $builder = if ($kind -eq 'out') { $output } else { $errors }
                if ($builder.Length + $count -gt $OutputLimit) { throw 'LINUX_SQL_CHILD_OUTPUT_LIMIT_STATE_UNKNOWN' }
                [void]$builder.Append($buffer, 0, $count)
                $next = $null
                if ($count -gt 0) {
                    $reader = if ($kind -eq 'out') { $process.StandardOutput } else { $process.StandardError }
                    $next = $reader.ReadAsync($buffer, 0, $buffer.Length)
                }
                if ($kind -eq 'out') { $outRead = $next } else { $errRead = $next }
            }
            Start-Sleep -Milliseconds 20
        }
        $completed = $true
        return [pscustomobject]@{
            ExitCode = $process.ExitCode; Output = $output.ToString(); Error = $errors.ToString()
            Pid = $process.Id; BirthUtc = $birth; ElapsedMs = $clock.ElapsedMilliseconds
        }
    } finally {
        # No kill/retry: a timed-out WSL client does not prove its Linux child is dead.
        try { $process.StandardInput.Close() } catch { }
        if ($childStarted -and (Get-Variable -Name LinuxSqlEvidenceRoot -Scope Script -ErrorAction SilentlyContinue)) {
            $exitValue = if ($process.HasExited) { $process.ExitCode } else { $null }
            Write-LinuxSqlJson -Path (Join-Path $script:LinuxSqlEvidenceRoot ('process-' + [guid]::NewGuid().ToString('N') + '.json')) -Value @{
                FileName = $FileName; PublicArguments = $Arguments; Pid = $process.Id; BirthUtc = $birth
                ExitCode = $exitValue; Completed = $completed; ElapsedMs = $clock.ElapsedMilliseconds
                StdinPresent = $stdinPresent; StdoutCharacters = $output.Length; StderrCharacters = $errors.Length
                StdoutSha256 = Get-LinuxSqlHash -Bytes ([Text.Encoding]::Unicode.GetBytes($output.ToString()))
                StderrSha256 = Get-LinuxSqlHash -Bytes ([Text.Encoding]::Unicode.GetBytes($errors.ToString()))
                RawPolicy = 'Output retained only in bounded memory; caller publishes validated observations'
            }
        }
        $process.Dispose()
        $InputText = $null
    }
}

function ConvertTo-LinuxSqlPath {
    param([string]$Path)
    $result = Invoke-LinuxSqlProcess -FileName 'wsl.exe' -Arguments @(
        '-d', 'Ubuntu', '--exec', 'wslpath', '-a', '-u', [IO.Path]::GetFullPath($Path))
    if ($result.ExitCode -ne 0 -or $result.Error -or $result.Output.Trim() -cnotmatch '^/mnt/[a-z]/[^\r\n]+$') {
        throw 'LINUX_SQL_PATH_TRANSLATION_FAILED'
    }
    return $result.Output.Trim()
}

function Assert-LinuxSqlCapacity {
    param($Plan)
    $memory = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory
    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
    $editors = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {
            $_.CommandLine -notmatch '(?i)-assetImportWorker|AssetImportWorker'
        })
    if (@($editors | Where-Object { $null -eq $_.CommandLine }).Count) { throw 'LINUX_SQL_EDITOR_STATE_UNKNOWN' }
    $sample = [ordered]@{
        ObservedUtc = [DateTimeOffset]::UtcNow.ToString('o')
        PhysicalBytes = [long]$memory.AvailableBytes
        CommitBytes = [long]$memory.CommitLimit - [long]$memory.CommittedBytes
        DiskBytes = [long]$disk.FreeSpace; Editors = $editors.Count
    }
    Write-LinuxSqlJson -Path (Join-Path $Plan.EvidenceRoot ('capacity-' + [guid]::NewGuid().ToString('N') + '.json')) -Value $sample
    if ($sample.PhysicalBytes -lt $Plan.MinPhysicalBytes -or $sample.CommitBytes -lt $Plan.MinCommitBytes -or
        $sample.DiskBytes -lt $Plan.MinDiskBytes -or $sample.Editors -gt $Plan.MaxEditors) {
        throw 'LINUX_SQL_CAPACITY_GATE_FAILED'
    }
}

function Assert-LinuxSqlPrivateAcl {
    param($Plan)
    Assert-TestEnvironmentDirectoryAcl -Path $Plan.PrivateRoot -ExecutorSid $Plan.ExecutorSid
    $empty = Join-Path $Plan.PrivateRoot 'ca-empty'
    Assert-TestEnvironmentNoReparse -Path $empty
    if (-not (Test-Path -LiteralPath $empty -PathType Container) -or
        @(Get-ChildItem -LiteralPath $empty -Force).Count -ne 0) { throw 'LINUX_SQL_CA_DIRECTORY_CHANGED' }
    foreach ($name in @('sa.txt', 'runtime.clixml', 'server.key', 'server.pem', 'ca.pem', 'mssql.conf', 'owner.json')) {
        $path = Join-Path $Plan.PrivateRoot $name
        Assert-TestEnvironmentNoReparse -Path $path
        $acl = Get-Acl -LiteralPath $path
        $rules = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]))
        if ($acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $Plan.ExecutorSid -or
            -not $acl.AreAccessRulesProtected -or $rules.Count -ne 2 -or @($rules | Where-Object {
                    $_.IdentityReference.Value -cnotin @($Plan.ExecutorSid, 'S-1-5-18') -or
                    $_.IsInherited -or $_.AccessControlType -ne 'Allow' -or $_.FileSystemRights -ne 'FullControl'
                }).Count -gt 0) { throw 'LINUX_SQL_PRIVATE_FILE_ACL_CHANGED' }
    }
}

function Assert-LinuxSqlPrepared {
    param($Plan)
    Assert-LinuxSqlPrivateAcl -Plan $Plan
    $path = Join-Path $Plan.EvidenceRoot 'prepared.json'
    Assert-TestEnvironmentNoReparse -Path $path
    $prepared = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $names = @('sa.txt', 'runtime.clixml', 'server.key', 'server.pem', 'ca.pem', 'mssql.conf', 'owner.json')
    if ($prepared.PlanHash -cne $Plan.PlanHash -or $prepared.LifetimeId -cne $Plan.LifetimeId -or
        (($prepared.Files.Name | Sort-Object) -join '|') -cne (($names | Sort-Object) -join '|')) {
        throw 'LINUX_SQL_PREPARED_IDENTITY_CHANGED'
    }
    foreach ($file in $prepared.Files) {
        $filePath = Join-Path $Plan.PrivateRoot $file.Name
        if ($file.Bytes -le 0 -or $file.Bytes -gt 16384 -or (Get-Item -LiteralPath $filePath).Length -ne $file.Bytes -or
            (Get-FileHash -LiteralPath $filePath).Hash -cne $file.Sha256) { throw 'LINUX_SQL_PRIVATE_FILE_CHANGED' }
    }
}
