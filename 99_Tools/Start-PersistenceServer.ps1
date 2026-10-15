#requires -Version 7.0
# Run only in a separately approved runtime window after the owned clone has been compiled.
# No sync/build/run action, server.stdin FIFO, secret environment or persistent secret output is used.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProfilePath,
    [Parameter(Mandatory)][string]$CredentialPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PersistenceWslMetadata {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $lines = @(& wsl.exe -d Ubuntu --exec @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0 -or $lines.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$lines[0])) {
        throw 'Persistence launcher metadata check failed; no credential was read.'
    }
    return ([string]$lines[0]).TrimEnd([char[]]"`r`n")
}

$runtimeProcess = $null
$credential = $null
$passwordPointer = [IntPtr]::Zero
$passwordText = $null
$payload = $null
$envelope = $null
try {
    if (-not [OperatingSystem]::IsWindows()) {
        throw 'The persistence launcher requires current-user Windows DPAPI.'
    }
    $projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $publicProfile = (Resolve-Path -LiteralPath $ProfilePath).Path
    $projectPrefix = $projectRoot.TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar
    if (-not $publicProfile.StartsWith($projectPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetExtension($publicProfile) -cne '.json') {
        throw 'The public persistence profile must be a JSON file in this project.'
    }
    $sourceWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $projectRoot)
    $profileWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $publicProfile)
    $runtimeRoot = Get-PersistenceWslMetadata -Arguments @('bash', "$sourceWsl/99_Tools/sync-wsl.sh", 'path')
    $canonicalRoot = Get-PersistenceWslMetadata -Arguments @('realpath', '-e', $runtimeRoot)
    if (-not $runtimeRoot.StartsWith('/') -or $runtimeRoot -cne $canonicalRoot -or
        $runtimeRoot -eq '/' -or $runtimeRoot.StartsWith('/mnt/')) {
        throw 'The compiled persistence clone path is invalid.'
    }
    $cloneOwner = Get-PersistenceWslMetadata -Arguments @('cat', "$runtimeRoot/.dawnholder-source")
    if ($cloneOwner -cne $sourceWsl) {
        throw 'The compiled persistence clone belongs to a different project.'
    }
    $serverDll = "$runtimeRoot/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll"
    $canonicalDll = Get-PersistenceWslMetadata -Arguments @('realpath', '-e', $serverDll)
    if ($canonicalDll -cne $serverDll) {
        throw 'The compiled persistence server is missing or linked outside its clone.'
    }

    # Import-Clixml decrypts only in the approved current-user Windows DPAPI context.
    # Keep provider/DPAPI errors out of the console, including the exception's inner text.
    try {
        $credential = Import-Clixml -LiteralPath $CredentialPath
        if ($credential -isnot [Management.Automation.PSCredential] -or
            [string]::IsNullOrWhiteSpace($credential.UserName) -or $credential.UserName.Length -gt 128 -or
            $credential.UserName -match '[\x00-\x1f\x7f]' -or
            $credential.Password.Length -lt 1 -or $credential.Password.Length -gt 128) {
            throw 'Invalid runtime credential.'
        }
        $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($credential.Password)
        $passwordText = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        $envelope = [ordered]@{
            version = 1
            userName = $credential.UserName
            password = $passwordText
        }
        $payload = ConvertTo-Json -InputObject $envelope -Compress
        $utf8 = [Text.UTF8Encoding]::new($false, $true)
        if ($utf8.GetByteCount($payload) -ge 2048) {
            throw 'Runtime credential envelope exceeds its bound.'
        }
        $startInfo = [Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = 'wsl.exe'
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardInput = $true
        $startInfo.StandardInputEncoding = $utf8
        foreach ($argument in @('-d', 'Ubuntu', '--cd', $runtimeRoot, '--exec', 'dotnet',
                $serverDll, '--persistence-profile', $profileWsl)) {
            $startInfo.ArgumentList.Add($argument)
        }
        $runtimeProcess = [Diagnostics.Process]::new()
        $runtimeProcess.StartInfo = $startInfo
        if (-not $runtimeProcess.Start()) {
            throw 'Persistence server process did not start.'
        }
        $runtimeProcess.StandardInput.NewLine = "`n"
        $runtimeProcess.StandardInput.WriteLine($payload)
        $runtimeProcess.StandardInput.Flush()
        Write-Output ('Owned WSL process PID {0}, birth UTC {1:o}.' -f
            $runtimeProcess.Id, $runtimeProcess.StartTime.ToUniversalTime())
    } catch {
        throw 'Persistence credential handoff failed; provider and credential text suppressed.'
    } finally {
        if ($passwordPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
            $passwordPointer = [IntPtr]::Zero
        }
        if ($credential -is [Management.Automation.PSCredential]) {
            $credential.Password.Dispose()
        }
        if ($null -ne $envelope) { $envelope.Clear() }
        $envelope = $null
        $credential = $null
        $passwordText = $null
        $payload = $null
    }

    # stdin stays open after the credential line for Program's normal operator stop input.
    if (-not $runtimeProcess.HasExited) {
        $null = Read-Host -Prompt 'Persistence server process launched; press Enter to request normal shutdown'
        if (-not $runtimeProcess.HasExited) {
            $runtimeProcess.StandardInput.WriteLine()
            $runtimeProcess.StandardInput.Flush()
        }
    }
    $runtimeProcess.StandardInput.Close()
    $runtimeProcess.WaitForExit()
    if ($runtimeProcess.ExitCode -ne 0) {
        throw 'Persistence server failed or shutdown is incomplete; inspect only safe server diagnostics.'
    }
} catch {
    # Do not retry or kill a child on an uncertain handoff/close. EOF lets an unstarted server refuse startup.
    if ($null -ne $runtimeProcess) {
        try { $runtimeProcess.StandardInput.Close() } catch { }
    }
    throw 'Persistence launcher did not complete; confirm child-process state before another approved attempt.'
} finally {
    if ($null -ne $runtimeProcess) { $runtimeProcess.Dispose() }
}
