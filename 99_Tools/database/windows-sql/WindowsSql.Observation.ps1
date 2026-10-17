# Pure validation and change decisions over the seven explicitly supplied observation sections.
function Test-WindowsSqlConfiguration {
    param($Configuration)
    $fields = @('LoginMode', 'TcpEnabled', 'TcpPort', 'DynamicPorts', 'ForceEncryption', 'CertificateThumbprint')
    if (-not (Test-WindowsSqlShape -Object $Configuration -Fields $fields)) { return $false }
    if ($Configuration.LoginMode -isnot [string] -or $Configuration.LoginMode -cnotin @('Windows', 'Mixed')) {
        return $false
    }
    foreach ($field in @('TcpEnabled', 'DynamicPorts', 'ForceEncryption')) {
        if ($Configuration.$field -isnot [bool]) { return $false }
    }
    if ($null -ne $Configuration.TcpPort -and
        (-not (Test-WindowsSqlInteger -Value $Configuration.TcpPort) -or
        $Configuration.TcpPort -lt 1 -or $Configuration.TcpPort -gt 65535)) {
        return $false
    }
    $thumbprint = $Configuration.CertificateThumbprint
    return $null -eq $thumbprint -or ($thumbprint -is [string] -and $thumbprint -cmatch '^[A-F0-9]{40}$')
}

function Copy-WindowsSqlConfiguration {
    param($Configuration)
    return [pscustomobject][ordered]@{
        LoginMode = $Configuration.LoginMode
        TcpEnabled = $Configuration.TcpEnabled
        TcpPort = $Configuration.TcpPort
        DynamicPorts = $Configuration.DynamicPorts
        ForceEncryption = $Configuration.ForceEncryption
        CertificateThumbprint = $Configuration.CertificateThumbprint
    }
}

function Test-WindowsSqlConfigurationEqual {
    param($Left, $Right)
    if (-not (Test-WindowsSqlConfiguration -Configuration $Left) -or
        -not (Test-WindowsSqlConfiguration -Configuration $Right)) { return $false }
    foreach ($field in @(Get-WindowsSqlKeys -Object $Left)) {
        if ($Left.$field -cne $Right.$field) { return $false }
    }
    return $true
}

function Test-WindowsSqlMachine {
    param($Machine)
    return (Test-WindowsSqlShape -Object $Machine -Fields @('MachineName')) -and
    (Test-WindowsSqlText -Value $Machine.MachineName)
}

function Test-WindowsSqlInstance {
    param($Instance)
    if (-not (Test-WindowsSqlShape -Object $Instance -Fields @('InstanceName', 'InstanceId', 'ProductVersion'))) {
        return $false
    }
    return (Test-WindowsSqlText -Value $Instance.InstanceName) -and
    (Test-WindowsSqlText -Value $Instance.InstanceId) -and
    (Test-WindowsSqlText -Value $Instance.ProductVersion)
}

function Test-WindowsSqlObservation {
    param($Observation)
    if (-not (Test-WindowsSqlShape -Object $Observation -Fields @(
                'Machine', 'Instance', 'Configuration', 'Port', 'Network', 'Database', 'Login'))) {
        return $false
    }
    if (-not (Test-WindowsSqlMachine -Machine $Observation.Machine) -or
        -not (Test-WindowsSqlInstance -Instance $Observation.Instance) -or
        -not (Test-WindowsSqlConfiguration -Configuration $Observation.Configuration)) {
        return $false
    }
    $port = $Observation.Port
    if (-not (Test-WindowsSqlShape -Object $port -Fields @('Port', 'InUse', 'OwnedByInstance')) -or
        -not (Test-WindowsSqlInteger -Value $port.Port) -or $port.Port -lt 1024 -or $port.Port -gt 65535 -or
        $port.InUse -isnot [bool] -or $port.OwnedByInstance -isnot [bool] -or
        (-not $port.InUse -and $port.OwnedByInstance)) {
        return $false
    }
    $network = $Observation.Network
    if (-not (Test-WindowsSqlShape -Object $network -Fields @('Mode', 'WslSubnet', 'OwnedFirewallRule')) -or
        $network.Mode -isnot [string] -or $network.Mode -cnotin @('Nat', 'Mirrored') -or
        ($null -ne $network.WslSubnet -and $network.WslSubnet -isnot [string])) {
        return $false
    }
    $rule = $network.OwnedFirewallRule
    if ($null -ne $rule -and
        (-not (Test-WindowsSqlShape -Object $rule -Fields @('RemoteAddress', 'Port')) -or
        -not (Test-WindowsSqlText -Value $rule.RemoteAddress) -or
        -not (Test-WindowsSqlInteger -Value $rule.Port) -or $rule.Port -lt 1024 -or $rule.Port -gt 65535)) {
        return $false
    }
    foreach ($section in @('Database', 'Login')) {
        $resource = $Observation.$section
        $fields = @('Name', 'Exists', 'OwnedLifetimeId')
        if ($section -ceq 'Database') { $fields += 'SchemaVersion' }
        if (-not (Test-WindowsSqlShape -Object $resource -Fields $fields) -or
            -not (Test-WindowsSqlText -Value $resource.Name) -or $resource.Exists -isnot [bool]) {
            return $false
        }
        $owner = $resource.OwnedLifetimeId
        if ($null -ne $owner -and ($owner -isnot [string] -or $owner -cnotmatch '^[a-f0-9]{32}$')) {
            return $false
        }
        if (-not $resource.Exists -and $null -ne $owner) { return $false }
        if ($section -ceq 'Database') {
            $version = $resource.SchemaVersion
            if ($null -ne $version -and
                (-not (Test-WindowsSqlInteger -Value $version) -or $version -lt 0)) {
                return $false
            }
            if (-not $resource.Exists -and $null -ne $version) { return $false }
        }
    }
    return $true
}

function Test-WindowsSqlFirewallScope {
    param($Scope)
    # /20..32 is the v1 support boundary, not an assumption about every WSL network.
    if ($Scope -isnot [string] -or $Scope -cnotmatch '^([0-9]{1,3}\.){3}[0-9]{1,3}/[0-9]{1,2}$') {
        return $false
    }
    $parts = $Scope.Split('/')
    $prefix = [int]$parts[1]
    if ($prefix -lt 20 -or $prefix -gt 32) { return $false }
    $address = [uint64]0
    foreach ($octet in $parts[0].Split('.')) {
        if ([int]$octet -gt 255 -or ([int]$octet).ToString() -cne $octet) { return $false }
        $address = ($address -shl 8) + [uint64]$octet
    }
    $hostMask = ([uint64]1 -shl (32 - $prefix)) - 1
    return ($address -band $hostMask) -eq 0
}

function New-WindowsSqlChangePlan {
    param($Plan, $Observation)
    $result = [ordered]@{
        Decision = 'Blocked'
        Changes = @()
        Blockers = @()
        TargetConfiguration = $null
        ObservationSource = 'Provided'
    }
    if (-not (Test-WindowsSqlObservation -Observation $Observation)) {
        $result.Blockers = @('WINDOWS_SQL_OBSERVATION_UNCLEAR')
        return [pscustomobject]$result
    }
    $configuration = $Observation.Configuration
    $result.TargetConfiguration = [pscustomobject][ordered]@{
        LoginMode = 'Mixed'
        TcpEnabled = $true
        TcpPort = $Plan.Port
        DynamicPorts = $false
        ForceEncryption = $configuration.ForceEncryption
        CertificateThumbprint = $Plan.CertificateThumbprint
    }
    $blockers = [Collections.Generic.List[string]]::new()
    if ($Observation.Machine.MachineName -cne $Plan.MachineName -or
        $Observation.Instance.InstanceName -cne $Plan.InstanceName -or
        $Observation.Instance.InstanceId -cne $Plan.InstanceId -or
        $Observation.Instance.ProductVersion -cne $Plan.ExpectedProductVersion -or
        $Observation.Database.Name -cne $Plan.Database -or $Observation.Login.Name -cne $Plan.RuntimeLogin) {
        $blockers.Add('WINDOWS_SQL_IDENTITY_MISMATCH')
    }
    if ($Observation.Port.Port -ne $Plan.Port) { $blockers.Add('WINDOWS_SQL_OBSERVATION_UNCLEAR') }
    if ($Observation.Port.InUse -and -not $Observation.Port.OwnedByInstance) {
        $blockers.Add('WINDOWS_SQL_PORT_CONFLICT')
    }
    foreach ($resource in @($Observation.Database, $Observation.Login)) {
        if ($resource.Exists) {
            if ($null -eq $resource.OwnedLifetimeId) { $blockers.Add('WINDOWS_SQL_TARGET_NOT_OWNED') }
            elseif ($resource.OwnedLifetimeId -cne $Plan.LifetimeId) {
                $blockers.Add('WINDOWS_SQL_OWNER_OR_VERSION_MISMATCH')
            }
        }
    }
    if ($Observation.Database.Exists -and $null -ne $Observation.Database.OwnedLifetimeId -and
        $Observation.Database.SchemaVersion -ne $Plan.SchemaVersion) {
        $blockers.Add('WINDOWS_SQL_OWNER_OR_VERSION_MISMATCH')
    }
    $changes = [Collections.Generic.List[string]]::new()
    if ($configuration.LoginMode -ceq 'Windows') { $changes.Add('SetMixedAuthentication') }
    if (-not $configuration.TcpEnabled -or $configuration.TcpPort -ne $Plan.Port -or $configuration.DynamicPorts) {
        $changes.Add('SetTcpPort')
    }
    if ($configuration.CertificateThumbprint -cne $Plan.CertificateThumbprint) {
        $changes.Add('SetServerCertificate')
    }
    if ($changes.Count -gt 0) {
        $changes.Add('RestartService')
        if (-not $Plan.RestartApproved) { $blockers.Add('WINDOWS_SQL_RESTART_NOT_APPROVED') }
    }
    $network = $Observation.Network
    if ($network.Mode -ceq 'Nat') {
        if (-not (Test-WindowsSqlFirewallScope -Scope $network.WslSubnet)) {
            $blockers.Add('WINDOWS_SQL_FIREWALL_SCOPE_INVALID')
        }
        if ($null -eq $network.OwnedFirewallRule) {
            $changes.Add('AddFirewallRule')
            if (-not $Plan.FirewallApproved) { $blockers.Add('WINDOWS_SQL_FIREWALL_NOT_APPROVED') }
        }
        elseif ($network.OwnedFirewallRule.Port -ne $Plan.Port -or
            $network.OwnedFirewallRule.RemoteAddress -cne $network.WslSubnet) {
            # Updating a stale rule is a separate approval, never an automatic scope expansion.
            $blockers.Add('WINDOWS_SQL_FIREWALL_SCOPE_INVALID')
        }
    }
    if (-not $Observation.Database.Exists) {
        $changes.Add('CreateDatabase')
        $changes.Add('ApplyMigrations')
    }
    if (-not $Observation.Login.Exists) { $changes.Add('CreateRuntimeLogin') }
    $result.Blockers = @($blockers.ToArray() | Select-Object -Unique)
    if ($blockers.Count -eq 0) {
        $result.Changes = $changes.ToArray()
        $result.Decision = if ($changes.Count -gt 0) { 'Apply' } else { 'NoChange' }
    }
    return [pscustomobject]$result
}
