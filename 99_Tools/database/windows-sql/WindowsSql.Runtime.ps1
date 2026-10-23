# Production-only input binding. The v1 decision core remains usable with offline observations.
function Get-WindowsSqlExecutionPlan {
    param($Adapter)
    $execution = Get-WindowsSqlField -Object $Adapter -Name 'ExecutionPlan'
    if ($null -ne $execution) { return $execution }
    return $Adapter.Plan
}

function Assert-WindowsSqlLocalPath {
    param([string]$Path, [switch]$AllowMissing)
    $full = Get-WindowsSqlFullPath -Path $Path
    if ($full -match ':' -and $full.Substring(2).Contains(':')) { throw 'WINDOWS_SQL_PATH_INVALID' }
    foreach ($segment in $full.Substring(3).Split('\')) {
        if ($segment.EndsWith('.') -or $segment.EndsWith(' ') -or
            $segment -match '^(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|$)') { throw 'WINDOWS_SQL_PATH_INVALID' }
    }
    $ancestor = $full
    while ($ancestor) {
        if ([IO.File]::Exists($ancestor) -or [IO.Directory]::Exists($ancestor)) {
            if (([IO.File]::GetAttributes($ancestor) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'WINDOWS_SQL_PATH_REPARSE'
            }
        }
        elseif (-not $AllowMissing) { throw 'WINDOWS_SQL_PATH_INVALID' }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
    return $full
}

function Get-WindowsSqlInputPath {
    param($Plan, [string]$Relative)
    $entries = @($Plan.Inputs | Where-Object Path -CEQ $Relative)
    if ($entries.Count -ne 1) { throw 'WINDOWS_SQL_REQUIRED_INPUT_MISSING' }
    return Assert-WindowsSqlLocalPath -Path (Join-Path $Plan.SourceRoot $Relative)
}

function Read-WindowsSqlRuntime {
    param($Plan, [string]$Relative)
    $runtime = ConvertFrom-WindowsSqlPlanBytes -Bytes (Read-WindowsSqlInputBytes -Plan $Plan -Relative $Relative)
    $fields = @('Version', 'LifetimeId', 'ExecutorSid', 'ServiceAccount', 'EndpointAddress', 'TlsServerName',
        'NetworkMode', 'WslSubnet', 'WslDistribution', 'PublicCaPath',
        'CredentialPath', 'CertificateReceiptHash', 'ProfilePath', 'VerifyHostFiles')
    if (-not (Test-WindowsSqlShape -Object $runtime -Fields $fields) -or
        -not (Test-WindowsSqlInteger -Value $runtime.Version) -or $runtime.Version -ne 2 -or
        $runtime.LifetimeId -cne $Plan.LifetimeId -or $Plan.SchemaVersion -ne 5) {
        throw 'WINDOWS_SQL_RUNTIME_INVALID'
    }
    foreach ($name in @('ExecutorSid', 'ServiceAccount', 'EndpointAddress', 'TlsServerName', 'NetworkMode',
            'WslDistribution', 'PublicCaPath', 'CredentialPath',
            'CertificateReceiptHash', 'ProfilePath')) {
        if (-not (Test-WindowsSqlText -Value $runtime[$name])) { throw 'WINDOWS_SQL_RUNTIME_INVALID' }
    }
    if ($runtime.ExecutorSid -cnotmatch '^S-1-5-21-([0-9]+-){3}[0-9]+$' -or
        $runtime.ServiceAccount -cne ('NT Service\MSSQL$' + $Plan.InstanceName) -or
        $runtime.WslDistribution -cne 'Ubuntu' -or $runtime.NetworkMode -cnotin @('Nat', 'Mirrored') -or
        $runtime.TlsServerName -cnotmatch '^(?=.{1,253}$)[a-zA-Z0-9]+([.-][a-zA-Z0-9]+)*$' -or
        [Uri]::CheckHostName($runtime.TlsServerName) -ne [UriHostNameType]::Dns -or
        $runtime.CertificateReceiptHash -cnotmatch '^[A-F0-9]{64}$') {
        throw 'WINDOWS_SQL_RUNTIME_INVALID'
    }
    $address = $null
    if (-not [Net.IPAddress]::TryParse($runtime.EndpointAddress, [ref]$address) -or
        $address.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork -or
        $address.ToString() -cne $runtime.EndpointAddress -or $runtime.EndpointAddress -eq '0.0.0.0') {
        throw 'WINDOWS_SQL_RUNTIME_INVALID'
    }
    if (($runtime.NetworkMode -ceq 'Nat' -and
            (-not (Test-WindowsSqlFirewallScope -Scope $runtime.WslSubnet) -or
            [Net.IPAddress]::IsLoopback($address))) -or
        ($runtime.NetworkMode -ceq 'Mirrored' -and
        ($runtime.EndpointAddress -cne '127.0.0.1' -or $null -ne $runtime.WslSubnet))) {
        throw 'WINDOWS_SQL_NETWORK_CHANGED'
    }
    $credential = Get-WindowsSqlFullPath -Path $runtime.CredentialPath
    if ($credential -cne (Join-Path $Plan.PrivateRoot 'runtime.clixml')) {
        throw 'WINDOWS_SQL_PATH_INVALID'
    }
    foreach ($relativePath in @($runtime.PublicCaPath, $runtime.ProfilePath)) {
        [void](Get-WindowsSqlInputPath -Plan $Plan -Relative $relativePath)
    }
    if ($runtime.VerifyHostFiles -isnot [array] -or $runtime.VerifyHostFiles.Count -lt 3 -or
        $runtime.VerifyHostFiles.Count -gt 128) { throw 'WINDOWS_SQL_RUNTIME_INVALID' }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in $runtime.VerifyHostFiles) {
        if (-not (Test-WindowsSqlShape -Object $file -Fields @('Path', 'Sha256')) -or
            $file.Path -isnot [string] -or $file.Path -cnotmatch '^[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)*$' -or
            $file.Path.Split('/') -contains '..' -or $file.Path.Split('/') -contains '.' -or
            $file.Sha256 -isnot [string] -or $file.Sha256 -cnotmatch '^[A-F0-9]{64}$' -or
            -not $seen.Add($file.Path)) { throw 'WINDOWS_SQL_RUNTIME_INVALID' }
    }
    foreach ($required in @('WindowsSqlVerifyHost.dll', 'WindowsSqlVerifyHost.deps.json',
            'WindowsSqlVerifyHost.runtimeconfig.json', 'Microsoft.Data.SqlClient.dll')) {
        if (-not $seen.Contains($required)) { throw 'WINDOWS_SQL_REQUIRED_INPUT_MISSING' }
    }
    return [pscustomobject]$runtime
}

function Read-WindowsSqlInputBytes {
    param($Plan, [string]$Relative)
    $path = Get-WindowsSqlInputPath -Plan $Plan -Relative $Relative
    $bytes = Read-WindowsSqlPlanBytes -Path $path
    $entry = @($Plan.Inputs | Where-Object Path -CEQ $Relative)[0]
    if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne $entry.Sha256) {
        throw 'WINDOWS_SQL_INPUT_CHANGED'
    }
    return , $bytes
}

function Assert-WindowsSqlExecutor {
    param($Plan, $Runtime)
    # This gate precedes service, registry, certificate, network and SQL access, including Preflight.
    if (-not [OperatingSystem]::IsWindows() -or [Environment]::MachineName -cne $Plan.MachineName) {
        throw 'WINDOWS_SQL_IDENTITY_MISMATCH'
    }
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        if ($identity.User.Value -cne $Runtime.ExecutorSid -or
            -not [Security.Principal.WindowsPrincipal]::new($identity).IsInRole(
                [Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'WINDOWS_SQL_EXECUTOR_MISMATCH'
        }
    }
    finally { $identity.Dispose() }
}

function Get-WindowsSqlRequiredInputs {
    param($Plan, [string]$RuntimePath)
    $relativeRoot = '99_Tools/database/'
    $paths = [Collections.Generic.List[string]]::new()
    foreach ($name in @('Database.Common.ps1', 'SqlError.Common.ps1', 'Module.Common.ps1',
            'ModuleHash.Common.ps1', 'Test-ModuleStructure.ps1', 'verify-schema.sql', 'modules/manifest.json')) {
        $paths.Add($relativeRoot + $name)
    }
    foreach ($name in @('001_initial.sql', '002_persistence_metadata.sql',
            '003_module_metadata.sql', '004_module_release.sql')) { $paths.Add($relativeRoot + 'migrations/' + $name) }
    $manifest = ConvertFrom-WindowsSqlPlanBytes -Bytes (Read-WindowsSqlInputBytes -Plan $Plan `
            -Relative ($relativeRoot + 'modules/manifest.json'))
    foreach ($entry in $manifest.Entries) { $paths.Add($relativeRoot + [string]$entry.Path) }
    foreach ($file in @(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1')) {
        $paths.Add($relativeRoot + 'windows-sql/' + $file.Name)
    }
    $paths.Add($RuntimePath)
    foreach ($name in @('Program.cs', 'WindowsSqlVerifyHost.csproj')) {
        $paths.Add($relativeRoot + 'windows-sql/WindowsSqlVerifyHost/' + $name)
    }
    foreach ($name in @('99_Tools/sync-wsl.sh', '99_Tools/Formatting/sdk.sh', 'global.json')) { $paths.Add($name) }
    foreach ($path in $paths) { [void](Get-WindowsSqlInputPath -Plan $Plan -Relative $path) }
    return $paths.ToArray()
}

function Open-WindowsSqlInputLease {
    param($Plan, [string]$RuntimePath)
    [void](Get-WindowsSqlRequiredInputs -Plan $Plan -RuntimePath $RuntimePath)
    $streams = [Collections.Generic.List[IDisposable]]::new()
    try {
        foreach ($entry in $Plan.Inputs) {
            $path = Get-WindowsSqlInputPath -Plan $Plan -Relative $entry.Path
            $stream = [IO.File]::Open($path, 'Open', 'Read', 'Read')
            $streams.Add($stream)
            if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) -cne $entry.Sha256) {
                throw 'WINDOWS_SQL_INPUT_CHANGED'
            }
        }
        return , $streams
    }
    catch {
        foreach ($stream in $streams) { $stream.Dispose() }
        throw 'WINDOWS_SQL_INPUT_CHANGED'
    }
}

function Invoke-WindowsSqlChild {
    param([string]$FileName, [string[]]$Arguments, [string]$InputText = '', [hashtable]$Environment = @{},
        [int]$TimeoutSeconds = 45)
    $process = [Diagnostics.Process]::new()
    $started = $false
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $FileName
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    # netsh redirects UTF-8; an inherited ANSI reader can consume punctuation after localized labels.
    if ($FileName -ieq 'C:\Windows\System32\netsh.exe') {
        $info.StandardOutputEncoding = [Text.UTF8Encoding]::new($false, $true)
    }
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    foreach ($name in $Environment.Keys) { $info.Environment[$name] = $Environment[$name] }
    $process.StartInfo = $info
    try {
        # Windows PowerShell must rebuild its module path without inheriting PowerShell 7 module paths.
        if ($info.FileName -ieq 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe') {
            [void]$info.Environment.Remove('PSModulePath')
        }
        if (-not $process.Start()) { throw 'WINDOWS_SQL_CHILD_FAILED' }
        $started = $true
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if ($InputText) { $process.StandardInput.WriteLine($InputText) }
        $process.StandardInput.Close()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw 'WINDOWS_SQL_CHILD_TIMEOUT'
        }
        $output = $stdout.GetAwaiter().GetResult()
        $errorOutput = $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0 -or $errorOutput.Length -ne 0 -or $output.Length -gt 2097152) {
            throw 'WINDOWS_SQL_CHILD_FAILED'
        }
        return $output.TrimEnd("`r", "`n")
    }
    finally {
        if ($started -and -not $process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $process.Dispose()
    }
}
