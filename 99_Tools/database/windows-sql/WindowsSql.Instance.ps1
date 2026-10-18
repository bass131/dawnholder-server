# Exact named-instance Windows resources. No instance/DB/certificate discovery or fallback transports.
function Open-WindowsSqlRegistryKey {
    param($Adapter, [string]$Suffix, [switch]$Writable)
    $root = [Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64')
    try {
        $path = 'SOFTWARE\Microsoft\Microsoft SQL Server\' + $Adapter.Plan.InstanceId + $Suffix
        $key = $root.OpenSubKey($path, $Writable.IsPresent)
        if ($null -eq $key) { throw 'WINDOWS_SQL_INSTANCE_MISSING' }
        return $key
    }
    finally { $root.Dispose() }
}

function Read-WindowsSqlInstanceIdentity {
    param($Adapter)
    $root = [Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64')
    $map = $null
    $setup = $null
    try {
        $map = $root.OpenSubKey('SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL')
        if ($null -eq $map -or $map.GetValue($Adapter.Plan.InstanceName) -cne $Adapter.Plan.InstanceId) {
            throw 'WINDOWS_SQL_IDENTITY_MISMATCH'
        }
        $setup = Open-WindowsSqlRegistryKey -Adapter $Adapter -Suffix '\Setup'
        # PatchLevel follows the engine; Setup/Version can remain at the initial installation version.
        $version = $setup.GetValue('PatchLevel', $null, 'DoNotExpandEnvironmentNames')
        if ($version -isnot [string] -or $version.Length -gt 64 -or
            $version -cnotmatch '\A(?:0|[1-9][0-9]*)(?:\.(?:0|[1-9][0-9]*)){3}\z' -or
            $version -cne $Adapter.Plan.ExpectedProductVersion) {
            throw 'WINDOWS_SQL_IDENTITY_MISMATCH'
        }
        # Open validates the SQL-reported identity and this connection's Shared memory transport.
        [void](Get-WindowsSqlMaster -Adapter $Adapter)
        Assert-WindowsSqlCertificate -Adapter $Adapter
        [void](Get-WindowsSqlVerifyHost -Adapter $Adapter)
        return [pscustomobject]@{
            InstanceName = $Adapter.Plan.InstanceName
            InstanceId = $Adapter.Plan.InstanceId
            ProductVersion = $version
        }
    }
    finally {
        if ($null -ne $map) { $map.Dispose() }
        if ($null -ne $setup) { $setup.Dispose() }
        $root.Dispose()
    }
}

function Read-WindowsSqlRegistry {
    param($Adapter)
    $values = [ordered]@{}
    $sections = @{
        '\MSSQLServer' = @('LoginMode')
        '\MSSQLServer\SuperSocketNetLib' = @('ForceEncryption', 'Certificate')
        '\MSSQLServer\SuperSocketNetLib\Tcp' = @('Enabled', 'ListenOnAllIPs')
        '\MSSQLServer\SuperSocketNetLib\Tcp\IPAll' = @('TcpPort', 'TcpDynamicPorts')
    }
    foreach ($suffix in $sections.Keys) {
        $key = Open-WindowsSqlRegistryKey -Adapter $Adapter -Suffix $suffix
        try {
            foreach ($name in $sections[$suffix]) {
                $value = $key.GetValue($name, $null, 'DoNotExpandEnvironmentNames')
                $kind = $key.GetValueKind($name).ToString()
                $expected = if ($name -cin @('Certificate', 'TcpPort', 'TcpDynamicPorts')) { 'String' } else { 'DWord' }
                if ($kind -cne $expected -or $null -eq $value) { throw 'WINDOWS_SQL_CONFIGURATION_UNSUPPORTED' }
                $values[$name] = $value
            }
        }
        finally { $key.Dispose() }
    }
    if ($values.LoginMode -notin @(1, 2) -or $values.Enabled -notin @(0, 1) -or
        $values.ListenOnAllIPs -ne 1 -or $values.ForceEncryption -notin @(0, 1) -or
        $values.TcpPort -cnotmatch '^([0-9]{1,5})?$' -or
        $values.TcpDynamicPorts -cnotmatch '^([0-9]{1,5})?$' -or
        $values.Certificate -cnotmatch '^([A-Fa-f0-9]{40})?$') {
        throw 'WINDOWS_SQL_CONFIGURATION_UNSUPPORTED'
    }
    return [pscustomobject]$values
}

function ConvertTo-WindowsSqlConfiguration {
    param($Registry)
    return [pscustomobject]@{
        LoginMode = if ($Registry.LoginMode -eq 1) { 'Windows' } else { 'Mixed' }
        TcpEnabled = $Registry.Enabled -eq 1
        TcpPort = if ($Registry.TcpPort -eq '') { $null } else { [int]$Registry.TcpPort }
        DynamicPorts = $Registry.TcpDynamicPorts -ne ''
        ForceEncryption = $Registry.ForceEncryption -eq 1
        CertificateThumbprint = if ($Registry.Certificate -eq '') {
            $null
        } else { $Registry.Certificate.ToUpperInvariant() }
    }
}

function Assert-WindowsSqlRegistryUnchanged {
    param($Adapter)
    $current = Read-WindowsSqlRegistry -Adapter $Adapter
    if ($null -eq $Adapter.ExpectedRegistry) { throw 'WINDOWS_SQL_CONFIGURATION_CHANGED' }
    foreach ($name in $current.PSObject.Properties.Name) {
        if ($current.$name -cne $Adapter.ExpectedRegistry.$name) { throw 'WINDOWS_SQL_CONFIGURATION_CHANGED' }
    }
    [void](Get-WindowsSqlService -Adapter $Adapter)
}

function Assert-WindowsSqlDependentServices {
    param($Controller)
    $dependents = $null
    try {
        # Each check owns a fresh list; cached enumeration/status must not survive the preflight or a prior write.
        $Controller.Refresh()
        $dependents = $Controller.DependentServices
        if ($null -eq $dependents) { throw 'WINDOWS_SQL_DEPENDENT_SERVICE' }
        foreach ($dependent in $dependents) {
            $dependent.Refresh()
            if ([string]$dependent.Status -cne 'Stopped' -or [string]$dependent.StartType -cne 'Disabled') {
                throw 'WINDOWS_SQL_DEPENDENT_SERVICE'
            }
        }
    }
    catch { throw 'WINDOWS_SQL_DEPENDENT_SERVICE' }
    finally {
        # The caller owns the instance controller; this helper owns every enumerated dependent, even on failure.
        foreach ($dependent in $dependents) {
            if ($null -ne $dependent -and $null -ne $dependent.PSObject.Methods['Dispose']) {
                $dependent.Dispose()
            }
        }
    }
}

function Get-WindowsSqlService {
    param($Adapter)
    $name = 'MSSQL$' + $Adapter.Plan.InstanceName
    $service = Get-CimInstance -ClassName Win32_Service -Filter ("Name='" + $name + "'") -ErrorAction Stop
    if ($null -eq $service -or $service.Name -cne $name -or
        $service.StartName -ine $Adapter.Runtime.ServiceAccount -or $service.State -cne 'Running') {
        throw 'WINDOWS_SQL_SERVICE_CHANGED'
    }
    $controller = [ServiceProcess.ServiceController]::new($name)
    try { Assert-WindowsSqlDependentServices -Controller $controller }
    finally { $controller.Dispose() }
    return $service
}

function Read-WindowsSqlPortRanges {
    param(
        [ValidateSet('ipv4', 'ipv6')][string]$AddressFamily,
        [ValidateSet('excludedportrange', 'dynamicport')][string]$RangeKind
    )
    $protocol = if ($RangeKind -ceq 'excludedportrange') { 'protocol=tcp' } else { 'tcp' }
    try {
        $output = Invoke-WindowsSqlChild -FileName 'C:\Windows\System32\netsh.exe' `
            -Arguments @('int', $AddressFamily, 'show', $RangeKind, $protocol) -TimeoutSeconds 15
    }
    catch { throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED' }
    if ($output -isnot [string] -or [string]::IsNullOrWhiteSpace($output)) {
        throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
    }
    $lines = @($output -split '\r?\n' | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 })
    if ($RangeKind -ceq 'excludedportrange') {
        # netsh emits two headings, a two-column separator, start/end rows (optional *), then a * - footnote.
        # Only this complete table is accepted; an empty/malformed response cannot mean no exclusions.
        if ($lines.Count -lt 4 -or $lines[2] -cnotmatch '^-+\s+-+$' -or
            $lines[-1] -cnotmatch '^\*\s+-\s+\S.*$') {
            throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
        }
        $ranges = [Collections.Generic.List[object]]::new()
        for ($index = 3; $index -lt $lines.Count - 1; $index++) {
            if ($lines[$index] -cnotmatch '^([0-9]{1,5})\s+([0-9]{1,5})(?:\s+\*)?$') {
                throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
            }
            $start = [int]$Matches[1]
            $end = [int]$Matches[2]
            if ($start -lt 1 -or $end -lt $start -or $end -gt 65535) {
                throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
            }
            $ranges.Add([pscustomobject]@{ Start = $start; End = $end })
        }
        return $ranges.ToArray()
    }

    # Dynamic output has a heading, separator, start and count in that order; labels are localized.
    if ($lines.Count -ne 4 -or $lines[1] -cnotmatch '^-+$' -or
        $lines[2] -cnotmatch '^[^:0-9]+:\s*([0-9]{1,5})$') {
        throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
    }
    $start = [int]$Matches[1]
    if ($lines[3] -cnotmatch '^[^:0-9]+:\s*([0-9]{1,5})$') {
        throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
    }
    $count = [int]$Matches[1]
    $end = $start + $count - 1
    if ($start -lt 1 -or $count -lt 1 -or $end -gt 65535) {
        throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
    }
    return [pscustomobject]@{ Start = $start; End = $end }
}

function Read-WindowsSqlPort {
    param($Adapter)
    $ranges = @(foreach ($family in @('ipv4', 'ipv6')) {
            Read-WindowsSqlPortRanges -AddressFamily $family -RangeKind excludedportrange
            Read-WindowsSqlPortRanges -AddressFamily $family -RangeKind dynamicport
        })
    foreach ($range in $ranges) {
        if ($Adapter.Plan.Port -ge $range.Start -and $Adapter.Plan.Port -le $range.End) {
            throw 'WINDOWS_SQL_PORT_CONFLICT'
        }
    }
    $errors = @()
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $Adapter.Plan.Port `
            -ErrorAction SilentlyContinue -ErrorVariable errors)
    foreach ($errorRecord in $errors) {
        if ($errorRecord.FullyQualifiedErrorId -notlike '*CmdletizationQuery_NotFound*') {
            throw 'WINDOWS_SQL_PORT_OBSERVATION_FAILED'
        }
    }
    $service = Get-WindowsSqlService -Adapter $Adapter
    $owned = $listeners.Count -gt 0 -and @($listeners | Where-Object OwningProcess -NE $service.ProcessId).Count -eq 0
    return [pscustomobject]@{ Port = $Adapter.Plan.Port; InUse = $listeners.Count -gt 0; OwnedByInstance = $owned }
}

function Invoke-WindowsSqlWslMetadata {
    param([string[]]$Arguments)
    return Invoke-WindowsSqlChild -FileName 'C:\Windows\System32\wsl.exe' `
        -Arguments (@('-d', 'Ubuntu', '--exec') + $Arguments) -TimeoutSeconds 15
}

function Read-WindowsSqlFirewall {
    param($Adapter)
    $errors = @()
    $name = 'Dawnholder.WindowsSql.' + $Adapter.Plan.LifetimeId
    $rules = @(Get-NetFirewallRule -PolicyStore PersistentStore -Name $name `
            -ErrorAction SilentlyContinue -ErrorVariable errors)
    foreach ($errorRecord in $errors) {
        if ($errorRecord.FullyQualifiedErrorId -notlike '*CmdletizationQuery_NotFound*') {
            throw 'WINDOWS_SQL_FIREWALL_OBSERVATION_FAILED'
        }
    }
    if ($rules.Count -eq 0) { return $null }
    if ($rules.Count -ne 1) { throw 'WINDOWS_SQL_FIREWALL_CHANGED' }
    $rule = $rules[0]
    $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
    $ports = $rule | Get-NetFirewallPortFilter -ErrorAction Stop
    $addresses = $rule | Get-NetFirewallAddressFilter -ErrorAction Stop
    $service = $rule | Get-NetFirewallServiceFilter -ErrorAction Stop
    $description = 'Dawnholder WindowsSql ' + $Adapter.Plan.PlanHash
    if ($null -eq $receipt.Firewall -or $receipt.Firewall -cne $name -or
        $rule.Description -cne $description -or $rule.Direction -ne 'Inbound' -or
        $rule.Action -ne 'Allow' -or $rule.Enabled -ne 'True' -or $ports.Protocol -ne 'TCP' -or
        [string]$ports.LocalPort -cne [string]$Adapter.Plan.Port -or
        [string]$addresses.RemoteAddress -cne $Adapter.Runtime.WslSubnet -or
        [string]$addresses.LocalAddress -cne $Adapter.Runtime.EndpointAddress -or
        $service.Service -cne ('MSSQL$' + $Adapter.Plan.InstanceName)) {
        throw 'WINDOWS_SQL_FIREWALL_CHANGED'
    }
    return [pscustomobject]@{ RemoteAddress = $Adapter.Runtime.WslSubnet; Port = $Adapter.Plan.Port }
}

function Read-WindowsSqlNetwork {
    param($Adapter)
    $runtime = $Adapter.Runtime
    $mode = Invoke-WindowsSqlWslMetadata -Arguments @('wslinfo', '--networking-mode')
    if ($mode -cne $runtime.NetworkMode.ToLowerInvariant()) { throw 'WINDOWS_SQL_NETWORK_CHANGED' }
    if ($runtime.NetworkMode -ceq 'Nat') {
        $routes = @(ConvertFrom-Json (Invoke-WindowsSqlWslMetadata -Arguments @('ip', '-j', '-4', 'route', 'show')))
        $default = @($routes | Where-Object dst -EQ 'default')
        if ($default.Count -ne 1 -or $default[0].gateway -cne $runtime.EndpointAddress) {
            throw 'WINDOWS_SQL_NETWORK_CHANGED'
        }
        $subnet = @($routes | Where-Object {
                (Get-WindowsSqlField -Object $_ -Name 'dst') -ceq $runtime.WslSubnet -and
                (Get-WindowsSqlField -Object $_ -Name 'dev') -ceq $default[0].dev -and
                (Get-WindowsSqlField -Object $_ -Name 'scope') -ceq 'link'
            })
        if ($subnet.Count -ne 1) { throw 'WINDOWS_SQL_NETWORK_CHANGED' }
    }
    return [pscustomobject]@{
        Mode = $runtime.NetworkMode
        WslSubnet = $runtime.WslSubnet
        OwnedFirewallRule = Read-WindowsSqlFirewall -Adapter $Adapter
    }
}

function Set-WindowsSqlRegistryValue {
    param($Adapter, [string]$Suffix, [string]$Name, $Value, [string]$Kind)
    Assert-WindowsSqlRegistryUnchanged -Adapter $Adapter
    $key = Open-WindowsSqlRegistryKey -Adapter $Adapter -Suffix $Suffix -Writable
    try {
        $key.SetValue($Name, $Value, [Microsoft.Win32.RegistryValueKind]::$Kind)
        $key.Flush()
    }
    finally { $key.Dispose() }
    $Adapter.ExpectedRegistry.$Name = $Value
    Assert-WindowsSqlRegistryUnchanged -Adapter $Adapter
}

function Invoke-WindowsSqlInstanceMutation {
    param($Adapter, [string]$Operation, $Request)
    $tcp = '\MSSQLServer\SuperSocketNetLib\Tcp'
    $network = '\MSSQLServer\SuperSocketNetLib'
    switch -CaseSensitive ($Operation) {
        'SetMixedAuthentication' {
            Set-WindowsSqlRegistryValue -Adapter $Adapter -Suffix '\MSSQLServer' `
                -Name 'LoginMode' -Value 2 -Kind DWord
        }
        'SetTcpPort' {
            $port = Read-WindowsSqlPort -Adapter $Adapter
            if ($port.InUse -and -not $port.OwnedByInstance) { throw 'WINDOWS_SQL_PORT_CONFLICT' }
            Set-WindowsSqlRegistryValue -Adapter $Adapter -Suffix ($tcp + '\IPAll') `
                -Name 'TcpDynamicPorts' -Value '' -Kind String
            Set-WindowsSqlRegistryValue -Adapter $Adapter -Suffix ($tcp + '\IPAll') `
                -Name 'TcpPort' -Value ([string]$Adapter.Plan.Port) -Kind String
            Set-WindowsSqlRegistryValue -Adapter $Adapter -Suffix $tcp -Name Enabled -Value 1 -Kind DWord
        }
        'SetServerCertificate' {
            if ($Request.Thumbprint -cne $Adapter.Plan.CertificateThumbprint) { throw 'WINDOWS_SQL_IDENTITY_MISMATCH' }
            Assert-WindowsSqlCertificate -Adapter $Adapter
            Set-WindowsSqlRegistryValue -Adapter $Adapter -Suffix $network -Name Certificate `
                -Value $Adapter.Plan.CertificateThumbprint.ToLowerInvariant() -Kind String
        }
        'RestartService' {
            if (-not $Adapter.Plan.RestartApproved) { throw 'WINDOWS_SQL_RESTART_NOT_APPROVED' }
            if ($null -ne $Adapter.Master) {
                $Adapter.Master.Dispose()
                $Adapter.Master = $null
            }
            $controller = [ServiceProcess.ServiceController]::new('MSSQL$' + $Adapter.Plan.InstanceName)
            try {
                Assert-WindowsSqlDependentServices -Controller $controller
                # SCM must refuse active dependents even if one starts after the check; never cascade their stop.
                $controller.Stop($false)
                $controller.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(45))
                $controller.Start()
                $controller.WaitForStatus('Running', [TimeSpan]::FromSeconds(45))
                [void](Get-WindowsSqlMaster -Adapter $Adapter)
            }
            finally { $controller.Dispose() }
        }
        'AddFirewallRule' {
            if (-not $Adapter.Plan.FirewallApproved -or $Adapter.Runtime.NetworkMode -cne 'Nat' -or
                $Request.RemoteAddress -cne $Adapter.Runtime.WslSubnet) { throw 'WINDOWS_SQL_FIREWALL_SCOPE_INVALID' }
            $observed = Read-WindowsSqlNetwork -Adapter $Adapter
            if ($null -ne $observed.OwnedFirewallRule) { throw 'WINDOWS_SQL_FIREWALL_CHANGED' }
            $name = 'Dawnholder.WindowsSql.' + $Adapter.Plan.LifetimeId
            $parameters = @{
                Name = $name
                DisplayName = $name
                Description = 'Dawnholder WindowsSql ' + $Adapter.Plan.PlanHash
                PolicyStore = 'PersistentStore'
                Enabled = 'True'
                Direction = 'Inbound'
                Action = 'Allow'
                Protocol = 'TCP'
                LocalPort = $Adapter.Plan.Port
                LocalAddress = $Adapter.Runtime.EndpointAddress
                RemoteAddress = $Adapter.Runtime.WslSubnet
                Service = 'MSSQL$' + $Adapter.Plan.InstanceName
                EdgeTraversalPolicy = 'Block'
                ErrorAction = 'Stop'
            }
            New-NetFirewallRule @parameters | Out-Null
            $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
            $receipt.Firewall = $name
            Save-WindowsSqlResourceReceipt -Adapter $Adapter -Receipt $receipt
            [void](Read-WindowsSqlFirewall -Adapter $Adapter)
        }
        'RemoveFirewallRule' {
            if ($null -eq (Read-WindowsSqlFirewall -Adapter $Adapter)) { throw 'WINDOWS_SQL_FIREWALL_CHANGED' }
            Remove-NetFirewallRule -PolicyStore PersistentStore `
                -Name ('Dawnholder.WindowsSql.' + $Adapter.Plan.LifetimeId) -ErrorAction Stop
        }
        'RestoreConfiguration' {
            $snapshot = Read-WindowsSqlPrivateJson -Adapter $Adapter -Name 'snapshot.json'
            if (($snapshot | ConvertTo-Json -Depth 20 -Compress) -cne
                ($Request.Snapshot | ConvertTo-Json -Depth 20 -Compress)) { throw 'WINDOWS_SQL_SNAPSHOT_CHANGED' }
            if ($snapshot.Registry.ForceEncryption -ne $Adapter.ExpectedRegistry.ForceEncryption) {
                throw 'WINDOWS_SQL_CONFIGURATION_CHANGED'
            }
            foreach ($item in @(
                    @{ Suffix = '\MSSQLServer'; Name = 'LoginMode'; Kind = 'DWord' },
                    @{ Suffix = $network; Name = 'Certificate'; Kind = 'String' },
                    @{ Suffix = $tcp; Name = 'Enabled'; Kind = 'DWord' },
                    @{ Suffix = $tcp + '\IPAll'; Name = 'TcpPort'; Kind = 'String' },
                    @{ Suffix = $tcp + '\IPAll'; Name = 'TcpDynamicPorts'; Kind = 'String' }
                )) {
                Set-WindowsSqlRegistryValue -Adapter $Adapter @item -Value $snapshot.Registry.($item.Name)
            }
        }
    }
}
