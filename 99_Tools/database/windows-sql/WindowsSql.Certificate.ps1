function Read-WindowsSqlCertificateReceipt {
    param($Adapter)
    $path = Join-Path $Adapter.Plan.PrivateRoot 'certificate.json'
    Assert-WindowsSqlPrivateAcl -Path $path -Sid $Adapter.Runtime.ExecutorSid
    $bytes = Read-WindowsSqlPlanBytes -Path $path
    if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne
        $Adapter.Runtime.CertificateReceiptHash) { throw 'WINDOWS_SQL_CERTIFICATE_RECEIPT_CHANGED' }
    $receipt = ConvertFrom-WindowsSqlPlanBytes -Bytes $bytes
    if (-not (Test-WindowsSqlShape -Object $receipt -Fields @('Version', 'PreparationHash', 'LifetimeId',
                'MachineName', 'InstanceName', 'ExecutorSid', 'Thumbprint', 'PublicCaSha256',
                'KeyFile', 'ServiceSid')) -or
        $receipt.Version -ne 1 -or $receipt.LifetimeId -cne $Adapter.Plan.LifetimeId -or
        $receipt.MachineName -cne $Adapter.Plan.MachineName -or
        $receipt.InstanceName -cne $Adapter.Plan.InstanceName -or
        $receipt.ExecutorSid -cne $Adapter.Runtime.ExecutorSid -or
        $receipt.Thumbprint -cne $Adapter.Plan.CertificateThumbprint) {
        throw 'WINDOWS_SQL_CERTIFICATE_RECEIPT_CHANGED'
    }
    return $receipt
}

function Assert-WindowsSqlCertificate {
    param($Adapter)
    $receipt = Read-WindowsSqlCertificateReceipt -Adapter $Adapter
    $certificate = Get-Item -LiteralPath ('Cert:\LocalMachine\My\' + $receipt.Thumbprint) -ErrorAction Stop
    try {
        $now = [DateTime]::UtcNow
        $eku = @($certificate.Extensions | Where-Object { $_.Oid.Value -ceq '2.5.29.37' })
        if (-not $certificate.HasPrivateKey -or $certificate.NotBefore.ToUniversalTime() -gt $now -or
            $certificate.NotAfter.ToUniversalTime() -lt $now.AddHours(24) -or
            $eku.Count -ne 1 -or @($eku[0].EnhancedKeyUsages.Value) -cnotcontains '1.3.6.1.5.5.7.3.1') {
            throw 'WINDOWS_SQL_CERTIFICATE_INVALID'
        }
        $san = @($certificate.Extensions | Where-Object { $_.Oid.Value -ceq '2.5.29.17' })
        $usage = @($certificate.Extensions | Where-Object { $_.Oid.Value -ceq '2.5.29.15' })
        $requiredUsage = [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature -bor
        [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment
        if ($usage.Count -ne 1 -or ($usage[0].KeyUsages -band $requiredUsage) -ne $requiredUsage) {
            throw 'WINDOWS_SQL_CERTIFICATE_INVALID'
        }
        if ($san.Count -ne 1) { throw 'WINDOWS_SQL_CERTIFICATE_INVALID' }
        $extension = [Security.Cryptography.X509Certificates.X509SubjectAlternativeNameExtension]::new(
            $san[0].RawData, $san[0].Critical)
        $names = @($extension.EnumerateDnsNames())
        if ($names -inotcontains $Adapter.Runtime.TlsServerName -or $names -inotcontains $Adapter.Plan.MachineName) {
            throw 'WINDOWS_SQL_CERTIFICATE_NAME_MISMATCH'
        }
        $privateKey = $certificate.PrivateKey
        try {
            if ($privateKey -isnot [Security.Cryptography.RSACryptoServiceProvider] -or
                $privateKey.KeySize -lt 2048 -or $privateKey.CspKeyContainerInfo.KeyNumber -ne 'Exchange' -or
                -not $privateKey.CspKeyContainerInfo.MachineKeyStore -or $privateKey.CspKeyContainerInfo.Exportable) {
                throw 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED'
            }
            $keyFile = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) `
            ('Microsoft\Crypto\RSA\MachineKeys\' + $privateKey.CspKeyContainerInfo.UniqueKeyContainerName)
            if ($keyFile -cne $receipt.KeyFile) { throw 'WINDOWS_SQL_CERTIFICATE_KEY_CHANGED' }
            [void](Assert-WindowsSqlLocalPath -Path $keyFile)
            $serviceSid = [Security.Principal.NTAccount]::new($Adapter.Runtime.ServiceAccount).Translate(
                [Security.Principal.SecurityIdentifier]).Value
            if ($serviceSid -cne $receipt.ServiceSid) { throw 'WINDOWS_SQL_SERVICE_CHANGED' }
            $acl = Get-Acl -LiteralPath $keyFile
            $rules = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) | Where-Object {
                    $_.IdentityReference.Value -ceq $serviceSid
                })
            if (@($rules | Where-Object AccessControlType -EQ 'Deny').Count -ne 0 -or
                @($rules | Where-Object {
                        $_.AccessControlType -eq 'Allow' -and
                        ($_.FileSystemRights -band [Security.AccessControl.FileSystemRights]::Read) -eq
                        [Security.AccessControl.FileSystemRights]::Read
                    }).Count -ne 1) { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID' }
        }
        finally { if ($null -ne $privateKey) { $privateKey.Dispose() } }
        $caPath = Get-WindowsSqlInputPath -Plan $Adapter.Plan -Relative $Adapter.Runtime.PublicCaPath
        $caBytes = [IO.File]::ReadAllBytes($caPath)
        if ($caBytes.Length -gt 16384 -or
            [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($caBytes)) -cne $receipt.PublicCaSha256) {
            throw 'WINDOWS_SQL_PUBLIC_CA_CHANGED'
        }
        $caText = [Text.UTF8Encoding]::new($false, $true).GetString($caBytes)
        if ($caText.Contains('PRIVATE KEY')) { throw 'WINDOWS_SQL_PUBLIC_CA_CHANGED' }
        $public = [Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPem($caText)
        try {
            if ($public.Thumbprint -cne $certificate.Thumbprint) { throw 'WINDOWS_SQL_PUBLIC_CA_CHANGED' }
        }
        finally { $public.Dispose() }
    }
    finally { $certificate.Dispose() }
}
