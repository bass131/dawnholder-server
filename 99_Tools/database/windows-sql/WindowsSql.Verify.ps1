function Assert-WindowsSqlProfile {
    param($Adapter)
    $profile = ConvertFrom-WindowsSqlPlanBytes -Bytes (Read-WindowsSqlInputBytes -Plan $Adapter.Plan `
            -Relative $Adapter.Runtime.ProfilePath)
    if (-not (Test-WindowsSqlShape -Object $profile -Fields @('persistedAccountName', 'database',
                'serverEndpoint', 'slotId', 'accountId', 'characterId')) -or
        $profile.database -cne $Adapter.Plan.Database -or
        $profile.serverEndpoint -cne ('tcp:' + $Adapter.Runtime.TlsServerName + ',' + $Adapter.Plan.Port) -or
        $profile.slotId -ne 1 -or $profile.accountId -cne $Adapter.Runtime.AccountId -or
        $profile.characterId -cne $Adapter.Runtime.CharacterId -or
        $profile.persistedAccountName -isnot [string] -or
        $profile.persistedAccountName -cnotmatch '^[a-z0-9_]{3,16}$') {
        throw 'WINDOWS_SQL_PROFILE_CHANGED'
    }
}

function Get-WindowsSqlVerifyHost {
    param($Adapter)
    $execution = Join-Path $Adapter.Plan.PrivateRoot 'execution'
    Assert-WindowsSqlPrivateAcl -Path $execution -Sid $Adapter.Runtime.ExecutorSid
    $executionWsl = Invoke-WindowsSqlWslMetadata -Arguments @('wslpath', '-a', '-u', $execution)
    $source = Invoke-WindowsSqlWslMetadata -Arguments @('wslpath', '-a', '-u', $Adapter.Plan.SourceRoot)
    $runtime = Invoke-WindowsSqlWslMetadata -Arguments @('wslpath', '-a', '-u',
        (Join-Path $Adapter.Plan.SourceRoot $Adapter.RuntimePath))
    $root = Invoke-WindowsSqlWslMetadata -Arguments @('bash', ($source + '/99_Tools/sync-wsl.sh'), 'path')
    $script = @'
set -euo pipefail
fail() { exit 1; }
export DOTNET_CLI_HOME="$4/cli-home" DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
[[ "$1" == /* && "$1" != /mnt/* && "$(realpath -e -- "$1")" == "$1" ]]
[[ "$(cat -- "$1/.dawnholder-source")" == "$2" ]]
python3 - "$1/99_Tools/database/windows-sql/WindowsSqlVerifyHost/bin/Debug/net10.0" "$3" <<'PY'
import hashlib,json,pathlib,sys
root=pathlib.Path(sys.argv[1]); cfg=json.load(open(sys.argv[2],encoding='utf-8'))
expected={x['Path']:x['Sha256'] for x in cfg['VerifyHostFiles']}
actual={p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}
if actual != set(expected): raise SystemExit(1)
for name,digest in expected.items():
 p=root/name
 if any(q.is_symlink() for q in [p,*p.parents]): raise SystemExit(1)
 if hashlib.sha256(p.read_bytes()).hexdigest().upper()!=digest: raise SystemExit(1)
PY
source "$1/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$1" >/dev/null
printf '%s\n' "$DOTNET"
'@
    $dotnet = Invoke-WindowsSqlWslMetadata -Arguments @('bash', '-c', $script, 'windows-sql-host',
        $root, $source, $runtime, $executionWsl)
    if ($dotnet -notmatch '^/[A-Za-z0-9_./-]+$') { throw 'WINDOWS_SQL_VERIFY_HOST_INVALID' }
    return [pscustomobject]@{
        Root = $root
        Dotnet = $dotnet
        ExecutionRoot = $executionWsl
        Dll = $root + '/99_Tools/database/windows-sql/WindowsSqlVerifyHost/bin/Debug/net10.0/WindowsSqlVerifyHost.dll'
    }
}

function Invoke-WindowsSqlWslVerification {
    param($Adapter)
    Assert-WindowsSqlProfile -Adapter $Adapter
    Assert-WindowsSqlCertificate -Adapter $Adapter
    [void](Read-WindowsSqlNetwork -Adapter $Adapter)
    $hostInfo = Get-WindowsSqlVerifyHost -Adapter $Adapter
    $caPath = Get-WindowsSqlInputPath -Plan $Adapter.Plan -Relative $Adapter.Runtime.PublicCaPath
    $caWsl = Invoke-WindowsSqlWslMetadata -Arguments @('wslpath', '-a', '-u', $caPath)
    $empty = Join-Path $Adapter.Plan.PrivateRoot 'empty-ca'
    Assert-WindowsSqlPrivateAcl -Path $empty -Sid $Adapter.Runtime.ExecutorSid
    if (@(Get-ChildItem -LiteralPath $empty -Force).Count -ne 0) { throw 'WINDOWS_SQL_CA_DIRECTORY_NOT_EMPTY' }
    $emptyWsl = Invoke-WindowsSqlWslMetadata -Arguments @('wslpath', '-a', '-u', $empty)
    Assert-WindowsSqlPrivateAcl -Path $Adapter.Runtime.CredentialPath -Sid $Adapter.Runtime.ExecutorSid
    $credential = $null
    $pointer = [IntPtr]::Zero
    $payload = $null
    try {
        $credential = Import-Clixml -LiteralPath $Adapter.Runtime.CredentialPath -ErrorAction Stop
        if ($credential -isnot [Management.Automation.PSCredential] -or
            $credential.UserName -cne $Adapter.Plan.RuntimeLogin -or
            $credential.Password.Length -lt 16 -or $credential.Password.Length -gt 128) {
            throw 'WINDOWS_SQL_CREDENTIAL_INVALID'
        }
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($credential.Password)
        $envelope = [ordered]@{
            Version = 1
            EndpointAddress = $Adapter.Runtime.EndpointAddress
            ServerName = $Adapter.Runtime.TlsServerName
            Port = $Adapter.Plan.Port
            Database = $Adapter.Plan.Database
            RuntimeLogin = $Adapter.Plan.RuntimeLogin
            MachineName = $Adapter.Plan.MachineName
            InstanceName = $Adapter.Plan.InstanceName
            ProductVersion = $Adapter.Plan.ExpectedProductVersion
            PublicCaHash = (Get-FileHash -LiteralPath $caPath).Hash
            Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
        }
        $payload = $envelope | ConvertTo-Json -Compress
        $arguments = @('-d', 'Ubuntu', '--cd', $hostInfo.Root, '--exec',
            'timeout', '--signal=TERM', '--kill-after=5s', '40s', 'env',
            "SSL_CERT_FILE=$caWsl", "SSL_CERT_DIR=$emptyWsl", 'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0',
            'DOTNET_GENERATE_ASPNET_CERTIFICATE=false', ('DOTNET_CLI_HOME=' + $hostInfo.ExecutionRoot + '/cli-home'),
            'DOTNET_CLI_TELEMETRY_OPTOUT=1', 'DOTNET_NOLOGO=1', $hostInfo.Dotnet, $hostInfo.Dll)
        $text = Invoke-WindowsSqlChild -FileName 'C:\Windows\System32\wsl.exe' -Arguments $arguments `
            -InputText $payload -TimeoutSeconds 45
        $result = ConvertFrom-Json -InputObject $text
        if (-not (Test-WindowsSqlShape -Object $result -Fields @('Status', 'Transport', 'Encrypted',
                    'CertificateValidated', 'RuntimeExecuteGrants', 'DirectDmlDenied')) -or
            $result.Status -cne 'Verified' -or $result.Transport -cne 'TCP' -or
            $result.Encrypted -ne $true -or $result.CertificateValidated -ne $true -or
            $result.RuntimeExecuteGrants -ne 5 -or $result.DirectDmlDenied -ne $true) {
            throw 'WINDOWS_SQL_TLS_VERIFY_FAILED'
        }
        return $result
    }
    finally {
        if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
        if ($null -ne $credential) { $credential.Password.Dispose() }
        $payload = $null
        $envelope = $null
    }
}
