# Private files are current-user owned and never adopted from permissive or linked paths.
function New-WindowsSqlPrivateDirectory {
    param([string]$Path, [string]$Sid)
    [void](Assert-WindowsSqlLocalPath -Path $Path -AllowMissing)
    if ([IO.Directory]::Exists($Path)) { throw 'WINDOWS_SQL_PRIVATE_ALREADY_EXISTS' }
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $owner = [Security.Principal.SecurityIdentifier]::new($Sid)
    $acl.SetOwner($owner)
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($identity in @($owner, [Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
        $rule = [Security.AccessControl.FileSystemAccessRule]::new($identity, 'FullControl',
            'ContainerInherit, ObjectInherit', 'None', 'Allow')
        $acl.AddAccessRule($rule)
    }
    [IO.FileSystemAclExtensions]::Create([IO.DirectoryInfo]::new($Path), $acl)
    Assert-WindowsSqlPrivateAcl -Path $Path -Sid $Sid
}

function Assert-WindowsSqlPrivateAcl {
    param([string]$Path, [string]$Sid)
    [void](Assert-WindowsSqlLocalPath -Path $Path)
    $acl = Get-Acl -LiteralPath $Path
    if ($acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -cne $Sid) {
        throw 'WINDOWS_SQL_PRIVATE_OWNER_MISMATCH'
    }
    foreach ($rule in $acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])) {
        if ($rule.AccessControlType -eq 'Allow' -and $rule.IdentityReference.Value -cnotin @($Sid, 'S-1-5-18')) {
            throw 'WINDOWS_SQL_PRIVATE_ACL_UNSAFE'
        }
    }
}

function Set-WindowsSqlPrivateFileOwner {
    param([string]$Path, [string]$Sid)
    [void](Assert-WindowsSqlLocalPath -Path $Path)
    $acl = Get-Acl -LiteralPath $Path
    $acl.SetOwner([Security.Principal.SecurityIdentifier]::new($Sid))
    Set-Acl -LiteralPath $Path -AclObject $acl -ErrorAction Stop
    Assert-WindowsSqlPrivateAcl -Path $Path -Sid $Sid
}

function Read-WindowsSqlPrivateJson {
    param($Adapter, [string]$Name)
    Assert-WindowsSqlPrivateAcl -Path $Adapter.Plan.PrivateRoot -Sid $Adapter.Runtime.ExecutorSid
    $path = Join-Path $Adapter.Plan.PrivateRoot $Name
    if (-not [IO.File]::Exists($path)) { return $null }
    Assert-WindowsSqlPrivateAcl -Path $path -Sid $Adapter.Runtime.ExecutorSid
    $bytes = Read-WindowsSqlPlanBytes -Path $path
    $value = ConvertFrom-WindowsSqlPlanBytes -Bytes $bytes
    if ($value.LifetimeId -cne $Adapter.Plan.LifetimeId -or $value.PlanHash -cne $Adapter.Plan.PlanHash) {
        throw 'WINDOWS_SQL_RECEIPT_MISMATCH'
    }
    return [pscustomobject]$value
}

function Write-WindowsSqlPrivateJson {
    param($Adapter, [string]$Name, $Value)
    Assert-WindowsSqlPrivateAcl -Path $Adapter.Plan.PrivateRoot -Sid $Adapter.Runtime.ExecutorSid
    $path = Join-Path $Adapter.Plan.PrivateRoot $Name
    if ([IO.File]::Exists($path)) {
        Assert-WindowsSqlPrivateAcl -Path $path -Sid $Adapter.Runtime.ExecutorSid
    }
    $temporary = Join-Path $Adapter.Plan.PrivateRoot ([Guid]::NewGuid().ToString('N') + '.pending')
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth 30 -Compress))
    $stream = $null
    try {
        $stream = [IO.FileStream]::new($temporary, 'CreateNew', 'Write', 'None', 4096, 'WriteThrough')
        $stream.Write($bytes)
        $stream.Flush($true)
        $stream.Dispose()
        $stream = $null
        Set-WindowsSqlPrivateFileOwner -Path $temporary -Sid $Adapter.Runtime.ExecutorSid
        if ([IO.File]::Exists($path)) { [IO.File]::Replace($temporary, $path, [NullString]::Value) }
        else { [IO.File]::Move($temporary, $path) }
        Assert-WindowsSqlPrivateAcl -Path $path -Sid $Adapter.Runtime.ExecutorSid
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
        # A failed rename/flush leaves its .pending evidence. Never erase an uncertain journal.
    }
}

function Get-WindowsSqlResourceReceipt {
    param($Adapter)
    $receipt = Read-WindowsSqlPrivateJson -Adapter $Adapter -Name 'resources.json'
    if ($null -eq $receipt) {
        return [pscustomobject]@{
            LifetimeId = $Adapter.Plan.LifetimeId
            PlanHash = $Adapter.Plan.PlanHash
            DatabaseGuid = $null
            LoginSid = $null
            CredentialHash = $null
            Firewall = $null
        }
    }
    return $receipt
}

function Save-WindowsSqlResourceReceipt {
    param($Adapter, $Receipt)
    Write-WindowsSqlPrivateJson -Adapter $Adapter -Name 'resources.json' -Value $Receipt
}
