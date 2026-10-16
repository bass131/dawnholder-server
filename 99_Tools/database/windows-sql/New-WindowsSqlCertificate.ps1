#requires -Version 7.0
[CmdletBinding()]
param([string]$PreparationPath, [string]$ExpectedPreparationHash)
if ($MyInvocation.InvocationName -eq '.') { return }
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WindowsSql.Common.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Runtime.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Store.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.PathLease.ps1')
$mutex = $null
$owned = $false
$lease = $null
$privateLease = $null
try {
    $bytes = Read-WindowsSqlPlanBytes -Path $PreparationPath
    if ($ExpectedPreparationHash -cnotmatch '^[A-F0-9]{64}$' -or
        [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne $ExpectedPreparationHash) {
        throw 'WINDOWS_SQL_PREPARATION_CHANGED'
    }
    $preparation = ConvertFrom-WindowsSqlPlanBytes -Bytes $bytes
    if (-not (Test-WindowsSqlShape -Object $preparation -Fields @('Version', 'ExecutionApproved', 'ExpiresUtc',
                'LifetimeId', 'MachineName', 'InstanceName', 'ExecutorSid', 'ServiceAccount', 'TlsServerName',
                'PrivateRoot', 'PublicCaPath')) -or -not (Test-WindowsSqlInteger -Value $preparation.Version) -or
        $preparation.Version -ne 1 -or
        $preparation.ExecutionApproved -isnot [bool] -or -not $preparation.ExecutionApproved -or
        $preparation.LifetimeId -cnotmatch '^[a-f0-9]{32}$' -or
        $preparation.MachineName -cnotmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,14}$' -or
        $preparation.InstanceName -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,15}$' -or
        $preparation.TlsServerName -cnotmatch '^(?=.{1,253}$)[a-zA-Z0-9]+([.-][a-zA-Z0-9]+)*$' -or
        [Uri]::CheckHostName($preparation.TlsServerName) -ne [UriHostNameType]::Dns -or
        $preparation.ServiceAccount -cne ('NT Service\MSSQL$' + $preparation.InstanceName)) {
        throw 'WINDOWS_SQL_PREPARATION_INVALID'
    }
    Assert-WindowsSqlApproval -Plan $preparation
    Assert-WindowsSqlExecutor -Plan $preparation -Runtime $preparation
    $private = Get-WindowsSqlFullPath -Path $preparation.PrivateRoot
    if ([IO.Path]::GetDirectoryName($private) -ine (Join-Path $env:LOCALAPPDATA 'Dawnholder')) {
        throw 'WINDOWS_SQL_PATH_INVALID'
    }
    $ca = Assert-WindowsSqlLocalPath -Path $preparation.PublicCaPath -AllowMissing
    if ([IO.File]::Exists($ca) -or [IO.Directory]::Exists($private) -or
        (Test-WindowsSqlPathWithin -Path $ca -Parent $private)) { throw 'WINDOWS_SQL_PREPARATION_TARGET_EXISTS' }
    [void](Assert-WindowsSqlLocalPath -Path ([IO.Path]::GetDirectoryName($ca)))
    $serviceSid = [Security.Principal.NTAccount]::new($preparation.ServiceAccount).Translate(
        [Security.Principal.SecurityIdentifier])
    $mutex = [Threading.Mutex]::new($false,
        ('Global\Dawnholder.WindowsSql.' + $preparation.MachineName + '.' + $preparation.InstanceName))
    try { $owned = $mutex.WaitOne(0) }
    catch [Threading.AbandonedMutexException] { $owned = $true }
    if (-not $owned) { throw 'WINDOWS_SQL_BUSY' }
    $lease = Open-WindowsSqlDirectoryLease -Paths @($private, $ca)
    Import-Module PKI -ErrorAction Stop
    New-WindowsSqlPrivateDirectory -Path $private -Sid $preparation.ExecutorSid
    $privateLease = Open-WindowsSqlDirectoryLease -Paths @($private)
    [void][IO.Directory]::CreateDirectory((Join-Path $private 'empty-ca'))
    Set-WindowsSqlPrivateFileOwner -Path (Join-Path $private 'empty-ca') -Sid $preparation.ExecutorSid
    [void][IO.Directory]::CreateDirectory((Join-Path $private 'execution'))
    Set-WindowsSqlPrivateFileOwner -Path (Join-Path $private 'execution') -Sid $preparation.ExecutorSid
    # Intent survives a process termination even before a certificate thumbprint becomes available.
    [IO.File]::WriteAllBytes((Join-Path $private 'preparation.json'), $bytes)
    Set-WindowsSqlPrivateFileOwner -Path (Join-Path $private 'preparation.json') -Sid $preparation.ExecutorSid
    $certificate = New-SelfSignedCertificate -Type SSLServerAuthentication `
        -Subject ('CN=' + $preparation.MachineName) -DnsName @($preparation.MachineName, $preparation.TlsServerName) `
        -CertStoreLocation 'Cert:\LocalMachine\My' -Provider 'Microsoft RSA SChannel Cryptographic Provider' `
        -KeyAlgorithm RSA -KeyLength 2048 -KeySpec KeyExchange -KeyExportPolicy NonExportable `
        -HashAlgorithm SHA256 -KeyUsage DigitalSignature, KeyEncipherment `
        -NotAfter ([DateTime]::Now.AddDays(30)) -ErrorAction Stop
    try {
        # Record identity immediately. Failure after this point is manual recovery, never automatic deletion.
        [IO.File]::WriteAllText((Join-Path $private 'certificate-created.txt'), $certificate.Thumbprint)
        Set-WindowsSqlPrivateFileOwner -Path (Join-Path $private 'certificate-created.txt') `
            -Sid $preparation.ExecutorSid
        $key = $certificate.PrivateKey
        try {
            if ($key -isnot [Security.Cryptography.RSACryptoServiceProvider]) {
                throw 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED'
            }
            $keyFile = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) `
            ('Microsoft\Crypto\RSA\MachineKeys\' + $key.CspKeyContainerInfo.UniqueKeyContainerName)
        }
        finally { if ($null -ne $key) { $key.Dispose() } }
        [void](Assert-WindowsSqlLocalPath -Path $keyFile)
        $acl = Get-Acl -LiteralPath $keyFile
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($serviceSid, 'Read', 'Allow'))
        Set-Acl -LiteralPath $keyFile -AclObject $acl -ErrorAction Stop
        $pem = $certificate.ExportCertificatePem() + "`n"
        $caStream = [IO.FileStream]::new($ca, 'CreateNew', 'Write', 'None', 4096, 'WriteThrough')
        try {
            $caStream.Write([Text.UTF8Encoding]::new($false).GetBytes($pem))
            $caStream.Flush($true)
        }
        finally { $caStream.Dispose() }
        $receipt = [ordered]@{
            Version = 1
            PreparationHash = $ExpectedPreparationHash
            LifetimeId = $preparation.LifetimeId
            MachineName = $preparation.MachineName
            InstanceName = $preparation.InstanceName
            ExecutorSid = $preparation.ExecutorSid
            Thumbprint = $certificate.Thumbprint
            PublicCaSha256 = (Get-FileHash -LiteralPath $ca -Algorithm SHA256).Hash
            KeyFile = $keyFile
            ServiceSid = $serviceSid.Value
        }
        $receiptPath = Join-Path $private 'certificate.json'
        $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $receiptPath -Encoding utf8NoBOM
        Set-WindowsSqlPrivateFileOwner -Path $receiptPath -Sid $preparation.ExecutorSid
        [pscustomobject]@{ Status = 'Prepared'; Thumbprint = $certificate.Thumbprint;
            ReceiptHash = (Get-FileHash -LiteralPath $receiptPath).Hash } | ConvertTo-Json -Compress
    }
    finally { $certificate.Dispose() }
    exit 0
}
catch {
    $code = $_.Exception.Message
    if ($code -cnotmatch '^WINDOWS_SQL_[A-Z0-9_]+$') { $code = 'WINDOWS_SQL_CERTIFICATE_PREPARATION_FAILED' }
    [Console]::Error.WriteLine($code)
    exit 1
}
finally {
    if ($null -ne $privateLease) { $privateLease.Dispose() }
    if ($null -ne $lease) { $lease.Dispose() }
    if ($owned) { $mutex.ReleaseMutex() }
    if ($null -ne $mutex) { $mutex.Dispose() }
}
