#requires -Version 7.0
# Run only in a separately approved runtime window after the owned clone has been compiled.
# No sync/build/run action, server.stdin FIFO, secret environment or persistent secret output is used.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProfilePath,
    [Parameter(Mandatory)][string]$CredentialPath,
    [string]$PublicCaPath = '',
    [string]$ExpectedPublicCaHash = '',
    [string]$EmptyCaDirectory = ''
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

function Get-PersistenceDotnetPath {
    param([Parameter(Mandatory)][string]$RuntimeRoot)

    # sdk.sh owns selection and global.json validation. The clone path is data, never shell source.
    $resolveSdk = @'
set -euo pipefail
fail() { printf '%s\n' "$*" >&2; exit 1; }
source "$1/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$1" >/dev/null
printf '%s\n' "$DOTNET"
'@
    $dotnetPath = Get-PersistenceWslMetadata -Arguments @(
        'bash', '-c', $resolveSdk, 'persistence-sdk', $RuntimeRoot
    )
    if (-not $dotnetPath.StartsWith('/')) {
        throw 'The persistence SDK must resolve to an absolute executable path.'
    }
    return $dotnetPath
}

function Wait-PersistenceShutdownRequest {
    param([Parameter(Mandatory)][Diagnostics.Process]$Process)

    if ($Process.HasExited) { return $false }
    Write-Host 'Persistence server process launched; press Enter to request normal shutdown'
    if (-not [Console]::IsInputRedirected) {
        while (-not $Process.HasExited) {
            if ([Console]::KeyAvailable -and [Console]::ReadKey($true).Key -eq [ConsoleKey]::Enter) {
                Write-Host
                return $true
            }
            [void]$Process.WaitForExit(100)
        }
        return $false
    }

    # A background ReadLine cannot be cancelled reliably and can consume the next command after
    # the child dies. Peek at redirected pipe input and read only available bytes; leave no reader behind.
    if (-not ('Dawnholder.PersistenceLauncherInput' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace Dawnholder
{
    public static class PersistenceLauncherInput
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int standardHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetFileType(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekNamedPipe(
            IntPtr handle, IntPtr buffer, uint bufferSize, IntPtr bytesRead,
            out uint availableBytes, IntPtr bytesLeft);
    }
}
'@
    }
    $inputHandle = [Dawnholder.PersistenceLauncherInput]::GetStdHandle(-10) # STD_INPUT_HANDLE
    $inputType = [Dawnholder.PersistenceLauncherInput]::GetFileType($inputHandle)
    if ($inputType -notin @(1, 3)) { # FILE_TYPE_DISK or FILE_TYPE_PIPE
        throw 'The redirected operator input is not a file or pipe.'
    }
    $inputStream = [Console]::OpenStandardInput()
    try {
        while (-not $Process.HasExited) {
            $availableBytes = [uint32]0
            if ($inputType -eq 3) {
                $peeked = [Dawnholder.PersistenceLauncherInput]::PeekNamedPipe(
                    $inputHandle, [IntPtr]::Zero, 0, [IntPtr]::Zero, [ref]$availableBytes, [IntPtr]::Zero)
                if (-not $peeked) {
                    $inputError = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
                    if ($inputError -in @(109, 232)) { return $true } # Closed pipe is EOF, like Read-Host.
                    throw 'The redirected operator input could not be observed.'
                }
            }
            if ($inputType -eq 1 -or $availableBytes -gt 0) {
                $inputByte = $inputStream.ReadByte()
                if ($inputByte -lt 0 -or $inputByte -eq 10) { return $true } # EOF or completed line.
                continue
            }
            [void]$Process.WaitForExit(100)
        }
        return $false
    } finally {
        $inputStream.Dispose()
    }
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
    $dotnetPath = Get-PersistenceDotnetPath -RuntimeRoot $runtimeRoot

    $trustArguments = @()
    if ($PublicCaPath -or $ExpectedPublicCaHash -or $EmptyCaDirectory) {
        if (-not $PublicCaPath -or -not $EmptyCaDirectory -or
            $ExpectedPublicCaHash -cnotmatch '^[A-F0-9]{64}$') {
            throw 'Public CA mode needs its path, reviewed hash and an empty CA directory.'
        }
        foreach ($trustPath in @($PublicCaPath, $EmptyCaDirectory)) {
            $ancestor = [IO.Path]::GetFullPath($trustPath)
            while ($ancestor) {
                if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                    throw 'Public CA paths cannot traverse reparse points.'
                }
                $ancestor = [IO.Path]::GetDirectoryName($ancestor)
            }
        }
        $caBytes = [IO.File]::ReadAllBytes($PublicCaPath)
        if ($caBytes.Length -gt 16384 -or
            [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($caBytes)) -cne $ExpectedPublicCaHash -or
            @(Get-ChildItem -LiteralPath $EmptyCaDirectory -Force).Count -ne 0) {
            throw 'Public CA input changed or the CA directory is not empty.'
        }
        $caText = [Text.UTF8Encoding]::new($false, $true).GetString($caBytes)
        if ($caText.Contains('PRIVATE KEY') -or -not $caText.Contains('BEGIN CERTIFICATE')) {
            throw 'Only a public PEM certificate is accepted.'
        }
        $caWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $PublicCaPath)
        $caDirectoryWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $EmptyCaDirectory)
        # These values are public paths and affect only the Linux child; no global trust store or WSLENV edit.
        $trustArguments = @('env', "SSL_CERT_FILE=$caWsl", "SSL_CERT_DIR=$caDirectoryWsl")
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
        $childArguments = @('-d', 'Ubuntu', '--cd', $runtimeRoot, '--exec') + $trustArguments +
        @($dotnetPath, $serverDll, '--persistence-profile', $profileWsl)
        foreach ($argument in $childArguments) {
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
    if (Wait-PersistenceShutdownRequest -Process $runtimeProcess) {
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
