# Definitions only; invoked after the environment owner's fresh-path/capacity/source gates.
function New-LinuxSqlPrivateDirectory {
    param($Plan)
    Assert-TestEnvironmentNoReparse -Path $Plan.PrivateRoot
    if (Test-Path -LiteralPath $Plan.PrivateRoot) { throw 'LINUX_SQL_PRIVATE_ROOT_OCCUPIED' }
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($Plan.ExecutorSid))
    foreach ($sid in @($Plan.ExecutorSid, 'S-1-5-18')) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl',
                'ContainerInherit,ObjectInherit', 'None', 'Allow'))
    }
    # .NET's ACL extension atomically creates with the final ACL; no create-then-tighten exposure window.
    $directory = [IO.DirectoryInfo]::new($Plan.PrivateRoot)
    [IO.FileSystemAclExtensions]::Create($directory, $acl)
    Assert-TestEnvironmentDirectoryAcl -Path $Plan.PrivateRoot -ExecutorSid $Plan.ExecutorSid
}

function Write-LinuxSqlPrivateText {
    param($Plan, [string]$Name, [string]$Text)
    if ($Name -cnotin @('sa.txt', 'runtime.clixml', 'server.key', 'server.pem', 'ca.pem', 'mssql.conf', 'owner.json')) {
        throw 'LINUX_SQL_PRIVATE_NAME_REFUSED'
    }
    $path = Join-Path $Plan.PrivateRoot $Name
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($Text)
    $acl = [Security.AccessControl.FileSecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($Plan.ExecutorSid))
    foreach ($sid in @($Plan.ExecutorSid, 'S-1-5-18')) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
                [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl', 'Allow'))
    }
    $file = [IO.FileSystemAclExtensions]::Create([IO.FileInfo]::new($path), [IO.FileMode]::CreateNew,
        [Security.AccessControl.FileSystemRights]::FullControl, [IO.FileShare]::None, 4096,
        [IO.FileOptions]::WriteThrough, $acl)
    try { $file.Write($bytes); $file.Flush($true) } finally { $file.Dispose(); [Array]::Clear($bytes) }
}

function New-LinuxSqlSecrets {
    param($Plan)
    New-LinuxSqlPrivateDirectory -Plan $Plan
    [void][IO.Directory]::CreateDirectory((Join-Path $Plan.PrivateRoot 'ca-empty'))
    $admin = New-TestEnvironmentSecret
    $runtime = New-TestEnvironmentSecret
    $adminCredential = [Management.Automation.PSCredential]::new('sa', $admin)
    $runtimeCredential = [Management.Automation.PSCredential]::new($Plan.RuntimeLogin, $runtime)
    $caKey = [Security.Cryptography.RSA]::Create(3072)
    $serverKey = [Security.Cryptography.RSA]::Create(3072)
    $ca = $null
    $server = $null
    try {
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'sa.txt' -Text $adminCredential.GetNetworkCredential().Password
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'runtime.clixml' `
            -Text ([Management.Automation.PSSerializer]::Serialize($runtimeCredential))
        $hash = [Security.Cryptography.HashAlgorithmName]::SHA256
        $padding = [Security.Cryptography.RSASignaturePadding]::Pkcs1
        $caRequest = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            'CN=Dawnholder isolated SQL CA', $caKey, $hash, $padding)
        $caRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
                $true, $false, 0, $true))
        $caRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyCertSign, $true))
        $now = [DateTimeOffset]::UtcNow
        $ca = $caRequest.CreateSelfSigned($now.AddMinutes(-5), $now.AddHours(24))
        $serverRequest = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            'CN=localhost', $serverKey, $hash, $padding)
        $serverRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
                $false, $false, 0, $true))
        $serverRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                ([Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature -bor
                [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment), $true))
        $purposes = [Security.Cryptography.OidCollection]::new()
        [void]$purposes.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1'))
        $serverRequest.CertificateExtensions.Add(
            [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($purposes, $true))
        $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        $san.AddDnsName('localhost')
        $san.AddIpAddress([Net.IPAddress]::Loopback)
        $serverRequest.CertificateExtensions.Add($san.Build())
        $serial = [Security.Cryptography.RandomNumberGenerator]::GetBytes(16)
        $serial[0] = $serial[0] -band 0x7f
        $serial[15] = $serial[15] -bor 1
        $server = $serverRequest.Create($ca, $now.AddMinutes(-5), $now.AddHours(12), $serial)
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'server.key' -Text $serverKey.ExportRSAPrivateKeyPem()
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'server.pem' -Text $server.ExportCertificatePem()
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'ca.pem' -Text $ca.ExportCertificatePem()
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'mssql.conf' -Text @'
[network]
forceencryption = 1
tlscert = /run/dawnholder/tls/server.pem
tlskey = /run/dawnholder/tls/server.key
tlsprotocols = 1.2
'@
        $owner = @{ PlanHash = $Plan.PlanHash; LifetimeId = $Plan.LifetimeId; ExecutorSid = $Plan.ExecutorSid }
        Write-LinuxSqlPrivateText -Plan $Plan -Name 'owner.json' -Text ($owner | ConvertTo-Json)
        Assert-LinuxSqlPrivateAcl -Plan $Plan
        $files = foreach ($name in @('sa.txt', 'runtime.clixml', 'server.key', 'server.pem', 'ca.pem', 'mssql.conf', 'owner.json')) {
            $path = Join-Path $Plan.PrivateRoot $name
            [pscustomobject]@{ Name = $name; Sha256 = (Get-FileHash -LiteralPath $path).Hash; Bytes = (Get-Item -LiteralPath $path).Length }
        }
        Write-LinuxSqlJson -Path (Join-Path $Plan.EvidenceRoot 'prepared.json') -Value @{
            PlanHash = $Plan.PlanHash; LifetimeId = $Plan.LifetimeId; Files = @($files)
            CreatedUtc = $now.ToString('o')
            ServerExpiresUtc = $server.NotAfter.ToUniversalTime().ToString('o')
            CaExpiresUtc = $ca.NotAfter.ToUniversalTime().ToString('o')
        }
    } finally {
        $admin.Dispose()
        $runtime.Dispose()
        $caKey.Dispose()
        $serverKey.Dispose()
        if ($null -ne $ca) { $ca.Dispose() }
        if ($null -ne $server) { $server.Dispose() }
        $adminCredential = $null
        $runtimeCredential = $null
    }
}
