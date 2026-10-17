# Plan parsing owns only explicit local file reads. It never observes SQL or the host identity.
function Get-WindowsSqlField {
    param($Object, [string]$Name)
    if ($Object -is [Collections.IDictionary]) {
        if (@($Object.Keys) -ccontains $Name) { return , $Object[$Name] }
    }
    elseif ($null -ne $Object -and @($Object.PSObject.Properties.Name) -ccontains $Name) {
        return , $Object.PSObject.Properties[$Name].Value
    }
    return $null
}

function Get-WindowsSqlKeys {
    param($Object)
    if ($Object -is [Collections.IDictionary]) { return @($Object.Keys) }
    if ($Object -is [pscustomobject]) { return @($Object.PSObject.Properties.Name) }
    return @()
}

function Test-WindowsSqlShape {
    param($Object, [string[]]$Fields)
    if ($Object -isnot [Collections.IDictionary] -and $Object -isnot [pscustomobject]) { return $false }
    $keys = @(Get-WindowsSqlKeys -Object $Object)
    if ($keys.Count -ne $Fields.Count) { return $false }
    foreach ($field in $Fields) {
        if ($keys -cnotcontains $field) { return $false }
    }
    return $true
}

function Test-WindowsSqlInteger {
    param($Value)
    return $Value -is [int] -or $Value -is [long]
}

function Test-WindowsSqlText {
    param($Value)
    return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value)
}

function Read-WindowsSqlPlanBytes {
    param($Path)
    if ($Path -isnot [string] -or $Path -notmatch '^[A-Za-z]:[\\/]') {
        throw 'WINDOWS_SQL_PLAN_INVALID'
    }
    $stream = $null
    try {
        $stream = [IO.File]::Open($Path, 'Open', 'Read', 'Read')
        # Bound allocation and decoding, even if a caller supplies a very large file.
        if ($stream.Length -gt 65536) { throw 'WINDOWS_SQL_PLAN_TOO_LARGE' }
        $bytes = [byte[]]::new([int]$stream.Length)
        $stream.ReadExactly($bytes)
        return , $bytes
    }
    catch {
        if ($_.Exception.Message -ceq 'WINDOWS_SQL_PLAN_TOO_LARGE') { throw 'WINDOWS_SQL_PLAN_TOO_LARGE' }
        throw 'WINDOWS_SQL_PLAN_INVALID'
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
    }
}

function Assert-WindowsSqlJsonProperties {
    param([Text.Json.JsonElement]$Element)
    if ($Element.ValueKind -eq [Text.Json.JsonValueKind]::Object) {
        $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($property in $Element.EnumerateObject()) {
            if (-not $names.Add($property.Name)) { throw 'WINDOWS_SQL_PLAN_INVALID' }
            Assert-WindowsSqlJsonProperties -Element $property.Value
        }
    }
    elseif ($Element.ValueKind -eq [Text.Json.JsonValueKind]::Array) {
        foreach ($value in $Element.EnumerateArray()) { Assert-WindowsSqlJsonProperties -Element $value }
    }
}

function ConvertFrom-WindowsSqlPlanBytes {
    param([byte[]]$Bytes)
    $document = $null
    try {
        $text = [Text.UTF8Encoding]::new($false, $true).GetString($Bytes)
        $document = [Text.Json.JsonDocument]::Parse($text)
        if ($document.RootElement.ValueKind -ne [Text.Json.JsonValueKind]::Object) {
            throw 'WINDOWS_SQL_PLAN_INVALID'
        }
        Assert-WindowsSqlJsonProperties -Element $document.RootElement
        $plan = ConvertFrom-Json -InputObject $text -AsHashtable -ErrorAction Stop
        # Preserve the original offset text independently of PowerShell JSON date coercion.
        $expiry = [Text.Json.JsonElement]::new()
        if ($document.RootElement.TryGetProperty('ExpiresUtc', [ref]$expiry)) {
            $plan['ExpiresUtc'] = if ($expiry.ValueKind -eq [Text.Json.JsonValueKind]::String) {
                $expiry.GetString()
            } else { $null }
        }
        return $plan
    }
    catch { throw 'WINDOWS_SQL_PLAN_INVALID' }
    finally {
        if ($null -ne $document) { $document.Dispose() }
    }
}

function Assert-WindowsSqlPlanFields {
    param($Plan, [switch]$ReadPlan)
    $fields = @(
        'Version', 'ExecutionApproved', 'ExpiresUtc', 'LifetimeId', 'MachineName', 'InstanceName', 'InstanceId',
        'ExpectedProductVersion', 'Port', 'Database', 'RuntimeLogin', 'SchemaVersion', 'CertificateThumbprint',
        'Encrypt', 'TrustServerCertificate', 'EnableSa', 'SwitchWslNetworkingMode', 'ForceEncryption',
        'RestartApproved', 'FirewallApproved', 'SourceRoot', 'EvidenceRoot', 'PrivateRoot', 'Inputs'
    )
    if ($ReadPlan) { $fields += 'PlanHash' }
    if ($Plan -isnot [Collections.IDictionary] -and $Plan -isnot [pscustomobject]) {
        throw 'WINDOWS_SQL_PLAN_INVALID'
    }
    foreach ($key in @(Get-WindowsSqlKeys -Object $Plan)) {
        if ($key -cnotin $fields) { throw 'WINDOWS_SQL_PLAN_FIELD_UNKNOWN' }
    }
    $version = Get-WindowsSqlField -Object $Plan -Name 'Version'
    if (-not (Test-WindowsSqlInteger -Value $version) -or $version -ne 1) {
        throw 'WINDOWS_SQL_PLAN_VERSION_UNSUPPORTED'
    }
    $approved = Get-WindowsSqlField -Object $Plan -Name 'ExecutionApproved'
    if ($approved -isnot [bool] -or -not $approved) { throw 'WINDOWS_SQL_EXECUTION_NOT_APPROVED' }
    foreach ($field in @('MachineName', 'InstanceName', 'InstanceId', 'ExpectedProductVersion', 'Database',
            'RuntimeLogin', 'LifetimeId', 'CertificateThumbprint')) {
        if (-not (Test-WindowsSqlText -Value (Get-WindowsSqlField -Object $Plan -Name $field))) {
            throw 'WINDOWS_SQL_PLAN_FIELD_INVALID'
        }
    }
    foreach ($field in @('Port', 'SchemaVersion')) {
        if (-not (Test-WindowsSqlInteger -Value (Get-WindowsSqlField -Object $Plan -Name $field))) {
            throw 'WINDOWS_SQL_PLAN_FIELD_INVALID'
        }
    }
    if ($Plan.ExpectedProductVersion -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' -or
        $Plan.LifetimeId -cnotmatch '^[a-f0-9]{32}$' -or $Plan.SchemaVersion -lt 1 -or
        $Plan.CertificateThumbprint -cnotmatch '^[A-F0-9]{40}$') {
        throw 'WINDOWS_SQL_PLAN_FIELD_INVALID'
    }
    $instancePattern = '^MSSQL[0-9]{2}\.' + [regex]::Escape($Plan.InstanceName) + '$'
    if ($Plan.MachineName -cnotmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,14}$' -or
        $Plan.InstanceName -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,15}$' -or
        $Plan.InstanceId -cnotmatch $instancePattern -or
        $Plan.Database -cnotmatch '^Dawnholder_[A-Za-z0-9_]{1,100}$' -or
        $Plan.RuntimeLogin -cnotmatch '^dh_[a-z0-9_]{1,80}$') {
        throw 'WINDOWS_SQL_NAME_INVALID'
    }
    if ($Plan.Port -lt 1024 -or $Plan.Port -gt 65535) { throw 'WINDOWS_SQL_PORT_INVALID' }
    $encrypt = Get-WindowsSqlField -Object $Plan -Name 'Encrypt'
    $trust = Get-WindowsSqlField -Object $Plan -Name 'TrustServerCertificate'
    if ($encrypt -isnot [string] -or $encrypt -cne 'Mandatory' -or $trust -isnot [bool] -or $trust) {
        throw 'WINDOWS_SQL_TLS_RELAXATION_REJECTED'
    }
    $enableSa = Get-WindowsSqlField -Object $Plan -Name 'EnableSa'
    if ($enableSa -isnot [bool] -or $enableSa) { throw 'WINDOWS_SQL_SA_REJECTED' }
    $switchMode = Get-WindowsSqlField -Object $Plan -Name 'SwitchWslNetworkingMode'
    $force = Get-WindowsSqlField -Object $Plan -Name 'ForceEncryption'
    if ($switchMode -isnot [bool] -or $switchMode -or $force -isnot [string] -or $force -cne 'Preserve') {
        throw 'WINDOWS_SQL_GLOBAL_CHANGE_REJECTED'
    }
    foreach ($field in @('RestartApproved', 'FirewallApproved')) {
        if ((Get-WindowsSqlField -Object $Plan -Name $field) -isnot [bool]) {
            throw 'WINDOWS_SQL_PLAN_FIELD_INVALID'
        }
    }
    if ($ReadPlan) {
        $hash = Get-WindowsSqlField -Object $Plan -Name 'PlanHash'
        if ($hash -isnot [string] -or $hash -cnotmatch '^[A-F0-9]{64}$') {
            throw 'WINDOWS_SQL_PLAN_IDENTITY_REQUIRED'
        }
    }
}

function Assert-WindowsSqlApproval {
    param($Plan)
    $text = Get-WindowsSqlField -Object $Plan -Name 'ExpiresUtc'
    $expiry = [DateTimeOffset]::MinValue
    if ($text -isnot [string] -or
        $text -cnotmatch '^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9:.]+(Z|[+-][0-9]{2}:[0-9]{2})$' -or
        -not [DateTimeOffset]::TryParse($text, [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::None, [ref]$expiry)) {
        throw 'WINDOWS_SQL_PLAN_EXPIRY_INVALID'
    }
    $now = [DateTimeOffset]::UtcNow
    if ($expiry -le $now -or $expiry -gt $now.AddHours(24)) { throw 'WINDOWS_SQL_APPROVAL_EXPIRED' }
}

function Get-WindowsSqlFullPath {
    param($Path)
    if ($Path -isnot [string] -or $Path -notmatch '^[A-Za-z]:[\\/]' -or $Path -match '[\x00-\x1f]') {
        throw 'WINDOWS_SQL_PATH_INVALID'
    }
    try { return [IO.Path]::GetFullPath($Path).TrimEnd('\', '/') }
    catch { throw 'WINDOWS_SQL_PATH_INVALID' }
}

function Test-WindowsSqlPathWithin {
    param([string]$Path, [string]$Parent)
    return $Path.Equals($Parent, [StringComparison]::OrdinalIgnoreCase) -or
    $Path.StartsWith($Parent + '\', [StringComparison]::OrdinalIgnoreCase)
}

function Assert-WindowsSqlPlanPaths {
    param($Plan)
    $source = Get-WindowsSqlFullPath -Path (Get-WindowsSqlField -Object $Plan -Name 'SourceRoot')
    $evidence = Get-WindowsSqlFullPath -Path (Get-WindowsSqlField -Object $Plan -Name 'EvidenceRoot')
    $private = Get-WindowsSqlFullPath -Path (Get-WindowsSqlField -Object $Plan -Name 'PrivateRoot')
    $privateParent = Get-WindowsSqlFullPath -Path ($env:LOCALAPPDATA + '\Dawnholder')
    if ([IO.Path]::GetDirectoryName($private) -ine $privateParent -or
        -not $evidence.StartsWith($source + '\.backups\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'WINDOWS_SQL_PATH_INVALID'
    }
    foreach ($publicPath in @($source, $evidence)) {
        if ((Test-WindowsSqlPathWithin -Path $publicPath -Parent $private) -or
            (Test-WindowsSqlPathWithin -Path $private -Parent $publicPath)) {
            throw 'WINDOWS_SQL_PATH_CONFLICT'
        }
    }
}

function Assert-WindowsSqlInputs {
    param($Plan)
    $inputs = Get-WindowsSqlField -Object $Plan -Name 'Inputs'
    if ($inputs -isnot [array] -or $inputs.Count -lt 1 -or $inputs.Count -gt 128) {
        throw 'WINDOWS_SQL_INPUT_SET_INVALID'
    }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $source = Get-WindowsSqlFullPath -Path $Plan.SourceRoot
    foreach ($entry in $inputs) {
        if (-not (Test-WindowsSqlShape -Object $entry -Fields @('Path', 'Sha256'))) {
            throw 'WINDOWS_SQL_INPUT_PATH_INVALID'
        }
        $relative = $entry.Path
        $hash = $entry.Sha256
        if ($relative -isnot [string] -or $relative -cnotmatch '^[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)*$' -or
            $relative.Split('/') -contains '..' -or $relative.Split('/') -contains '.' -or
            $hash -isnot [string] -or $hash -cnotmatch '^[A-F0-9]{64}$' -or -not $seen.Add($relative)) {
            throw 'WINDOWS_SQL_INPUT_PATH_INVALID'
        }
        foreach ($segment in $relative.Split('/')) {
            if ($segment.EndsWith('.') -or $segment -match '^(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|$)') {
                throw 'WINDOWS_SQL_INPUT_PATH_INVALID'
            }
        }
        $path = [IO.Path]::GetFullPath([IO.Path]::Combine($source, $relative))
        if (-not (Test-WindowsSqlPathWithin -Path $path -Parent $source)) {
            throw 'WINDOWS_SQL_INPUT_PATH_INVALID'
        }
        $stream = $null
        try {
            # Refuse link traversal under the reviewed source root before opening an input.
            $ancestor = $path
            while ($ancestor.Length -gt $source.Length) {
                if (([IO.File]::GetAttributes($ancestor) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw 'WINDOWS_SQL_INPUT_CHANGED'
                }
                $ancestor = [IO.Path]::GetDirectoryName($ancestor)
            }
            $stream = [IO.File]::OpenRead($path)
            $actualHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream))
            if ($actualHash -cne $hash) { throw 'WINDOWS_SQL_INPUT_CHANGED' }
        }
        catch { throw 'WINDOWS_SQL_INPUT_CHANGED' }
        finally {
            if ($null -ne $stream) { $stream.Dispose() }
        }
    }
}
