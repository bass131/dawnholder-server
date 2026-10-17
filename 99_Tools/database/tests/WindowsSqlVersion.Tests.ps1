[CmdletBinding(DefaultParameterSetName = 'Suite')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Suite')][string]$WorkRoot,
    # Version-check card (Markdown); its first powershell block holds the probe. A revised card uses the same command.
    [Parameter(Mandatory, ParameterSetName = 'Suite')][string]$CardPath,
    # Optional absolute windows-sql folder of an isolated same-shaped tree, as in WindowsSqlAdapterBoundary.
    [Parameter(ParameterSetName = 'Suite')][string]$ProductRoot = '',
    # Internal: the suite re-runs this file once per scenario in a fresh child process.
    [Parameter(Mandatory, ParameterSetName = 'Scenario')][string]$ScenarioPath
)
# Requirement tests for the SQL engine version sources of the Windows SQL Express tool and its version-check card.
# - The engine version is Registry64 Setup/PatchLevel of the approved InstanceId. The product accepts it only when it
#   equals the approved ExpectedProductVersion and the SQL ProductVersion read on the same LPC connection; the card
#   (four inputs, no approved version) accepts it only when it equals that SQL ProductVersion. Setup/Version is install
#   metadata: it neither blocks a patched instance nor stands in for a missing or invalid PatchLevel.
# - Every scenario runs in a fresh pwsh 7 child. Before any product or card text is parsed, the child binds the type
#   names [Microsoft.Win32.RegistryKey], [Microsoft.Win32.Registry], [Data.SqlClient.SqlConnection] and, for the card,
#   [Environment] to the stand-in classes below, then proves that every type reference of the loaded product files or
#   of the card probe resolved to a stand-in. The real product functions and the card probe text run unchanged.
# - The stand-in SQL server holds fixed server facts only and evaluates the comparisons that the product or the card
#   sends, as the engine would. Expected outcomes are requirement constants, never recomputed from product code.
# - Fixture names LAPTOP-FIXTURE / DHFIXTURE never name this host or its SQL instance.
# Nothing here opens SQL, the registry, a certificate store, a service, the firewall or WSL.

$script:FixtureMachine = 'LAPTOP-FIXTURE'
$script:FixtureInstance = 'DHFIXTURE'
$script:FixtureInstanceId = 'MSSQL17.DHFIXTURE'
$script:SqlServerKey = 'SOFTWARE\Microsoft\Microsoft SQL Server'
$script:InstanceMapPath = $script:SqlServerKey + '\Instance Names\SQL'
$script:SetupPath = $script:SqlServerKey + '\' + $script:FixtureInstanceId + '\Setup'
$script:SqlTarget = 'lpc:' + $script:FixtureMachine + '\' + $script:FixtureInstance
# Requirement cases: an updated instance keeps its install version in Setup/Version while the engine moved on.
$script:InstallVersion = '17.0.1000.7'
$script:PatchedVersion = '17.0.1145.4'

# Type names whose real binding would reach the registry or SQL; the card child also guards System.Environment.
$script:ProtectedTypeNames = @(
    'Microsoft.Win32.RegistryKey',
    'Microsoft.Win32.Registry',
    'System.Data.SqlClient.SqlConnection',
    'Microsoft.Data.SqlClient.SqlConnection'
)
# Real commands that would reach a protected resource; a scenario child records and refuses each call.
$script:ForbiddenCommands = @(
    'Invoke-WindowsSqlChild', 'Invoke-WindowsSqlWslMetadata', 'Get-CimInstance', 'Get-WmiObject', 'Invoke-CimMethod',
    'Get-Service', 'Start-Service', 'Stop-Service', 'Restart-Service', 'Set-Service', 'Get-NetTCPConnection',
    'Get-NetFirewallRule', 'New-NetFirewallRule', 'Set-NetFirewallRule', 'Remove-NetFirewallRule', 'Get-Item',
    'Get-ItemProperty', 'Get-ItemPropertyValue', 'Set-ItemProperty', 'New-ItemProperty', 'Remove-ItemProperty',
    'Get-ChildItem', 'Test-Path', 'Invoke-Sqlcmd', 'sqlcmd', 'reg', 'certutil', 'netsh', 'sc.exe', 'wsl', 'wsl.exe',
    'docker', 'Start-Process', 'New-SelfSignedCertificate', 'Import-PfxCertificate', 'Export-Clixml', 'Import-Clixml'
)
# Child exit codes outside the card probe's own 0/1/2: the harness stopped before or around the target.
$script:ChildGuardFailed = 91
$script:ChildProbeMissing = 92
$script:ChildProbeReturned = 93
$script:ChildHarnessFailed = 94

# One "<server function>(<name>)" call, and the OR-joined "<server function> <> <operand>" test of a THROW guard.
$script:ServerFunctionPattern = '(?i)(?<source>CONNECTIONPROPERTY|SERVERPROPERTY|IS_SRVROLEMEMBER)' +
'\(\s*N?''(?<property>[A-Za-z_]+)''\s*\)'
$script:ComparisonPattern = $script:ServerFunctionPattern +
'\)?\s*(?:COLLATE\s+(?<collation>[A-Za-z0-9_]+)\s*)?<>\s*(?<operand>@[A-Za-z_]+|N''[^'']*''|[0-9]+)'

# =====================================================================================================
# Stand-ins bound to protected type names inside a scenario child only (Install-VersionTypeStandIns).
# =====================================================================================================
class VersionFakeRegistryKey {
    static [scriptblock]$OnOpenBaseKey

    static [object] OpenBaseKey([object]$Hive, [object]$View) {
        return & ([VersionFakeRegistryKey]::OnOpenBaseKey) $Hive $View
    }
}

# [Microsoft.Win32.Registry]::LocalMachine would bypass OpenBaseKey; this stand-in has no members at all.
class VersionFakeRegistry {
}

class VersionFakeSqlConnection {
    static [scriptblock]$OnCreate
    static [scriptblock]$OnOpen
    static [scriptblock]$OnCommand
    [string]$ConnectionString
    [int]$Id
    [bool]$Disposed

    VersionFakeSqlConnection([string]$ConnectionString) {
        $this.ConnectionString = $ConnectionString
        & ([VersionFakeSqlConnection]::OnCreate) $this
    }

    [void] Open() {
        & ([VersionFakeSqlConnection]::OnOpen) $this
    }

    [object] CreateCommand() {
        return & ([VersionFakeSqlConnection]::OnCommand) $this
    }

    [void] Dispose() {
        $this.Disposed = $true
    }

    [void] Close() {
        $this.Disposed = $true
    }
}

# The card compares its machine input with the host name; the card child reports the fixture machine instead.
class VersionFakeEnvironment {
    static [string]$MachineName
    static [string]$NewLine
}

# =====================================================================================================
# Scenario child: observation log, protected I/O stand-ins and the stand-in SQL server.
# =====================================================================================================
function Write-VersionLog {
    param([string]$Kind, [Collections.IDictionary]$Data = @{})
    $entry = [ordered]@{ Kind = $Kind }
    foreach ($key in $Data.Keys) { $entry[$key] = $Data[$key] }
    # Appended per call: the card probe ends the process with exit, so nothing is buffered.
    $line = ConvertTo-Json -InputObject $entry -Depth 8 -Compress
    [IO.File]::AppendAllText($script:LogPath, $line + "`n", [Text.UTF8Encoding]::new($false))
}

function New-VersionRegistryKey {
    param([string]$Path)
    $key = [pscustomobject]@{ Path = $Path }
    $key | Add-Member -MemberType ScriptMethod -Name OpenSubKey -Value {
        param([string]$Name, $Writable = $false)
        $full = if ($this.Path) { $this.Path + '\' + $Name } else { $Name }
        Write-VersionLog -Kind 'RegistryOpen' -Data @{ Path = $full; Writable = [bool]$Writable }
        if ([bool]$Writable) { throw 'VERSION_FAKE_REGISTRY_WRITE_REJECTED' }
        # Like the real API, a missing key opens as null.
        if (-not $script:World.Registry.ContainsKey($full)) { return $null }
        return New-VersionRegistryKey -Path $full
    }
    $key | Add-Member -MemberType ScriptMethod -Name GetValue -Value {
        param([string]$Name, $Default = $null, $Options = $null)
        Write-VersionLog -Kind 'RegistryRead' -Data @{ Path = $this.Path; Name = $Name }
        $values = $script:World.Registry[$this.Path]
        if ($null -eq $values -or -not $values.ContainsKey($Name)) { return $Default }
        return $values[$Name].Value
    }
    $key | Add-Member -MemberType ScriptMethod -Name GetValueKind -Value {
        param([string]$Name)
        Write-VersionLog -Kind 'RegistryKindRead' -Data @{ Path = $this.Path; Name = $Name }
        $values = $script:World.Registry[$this.Path]
        # Like the real API, the kind of a missing value is an IOException.
        if ($null -eq $values -or -not $values.ContainsKey($Name)) { throw [IO.IOException]::new('Value missing.') }
        return [Microsoft.Win32.RegistryValueKind]$values[$Name].Kind
    }
    $key | Add-Member -MemberType ScriptMethod -Name GetValueNames -Value {
        $values = $script:World.Registry[$this.Path]
        if ($null -eq $values) { return [string[]]@() }
        return [string[]]@($values.Keys)
    }
    $key | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
    $key | Add-Member -MemberType ScriptMethod -Name Close -Value { }
    return $key
}

function New-VersionSqlCommand {
    param([int]$ConnectionId)
    # A provider parameter collection keeps the shared New-DbCommand typing real; that provider never executes.
    $provider = [Data.SqlClient.SqlCommand]::new()
    $command = [pscustomobject]@{
        ConnectionId = $ConnectionId
        Provider = $provider
        CommandText = ''
        CommandTimeout = 0
        Transaction = $null
        Parameters = $provider.Parameters
    }
    $command | Add-Member -MemberType ScriptMethod -Name ExecuteNonQuery -Value {
        return Invoke-VersionSqlServer -Command $this -Mode 'NonQuery'
    }
    $command | Add-Member -MemberType ScriptMethod -Name ExecuteScalar -Value {
        return Invoke-VersionSqlServer -Command $this -Mode 'Scalar'
    }
    $command | Add-Member -MemberType ScriptMethod -Name ExecuteReader -Value {
        param($Behavior = $null)
        $row = Invoke-VersionSqlServer -Command $this -Mode 'Reader'
        return New-VersionSqlReader -Values $row
    }
    $command | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.Provider.Dispose() }
    return $command
}

function New-VersionSqlReader {
    param([object[]]$Values)
    $reader = [pscustomobject]@{ Values = $Values; Position = -1; FieldCount = $Values.Count }
    $reader | Add-Member -MemberType ScriptMethod -Name Read -Value {
        $this.Position++
        return $this.Position -eq 0
    }
    $reader | Add-Member -MemberType ScriptMethod -Name IsDBNull -Value {
        param([int]$Ordinal)
        return $null -eq $this.Values[$Ordinal]
    }
    $reader | Add-Member -MemberType ScriptMethod -Name GetValue -Value {
        param([int]$Ordinal)
        if ($null -eq $this.Values[$Ordinal]) { return [DBNull]::Value }
        return $this.Values[$Ordinal]
    }
    $reader | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
    $reader | Add-Member -MemberType ScriptMethod -Name Close -Value { }
    return $reader
}

function Get-VersionServerValue {
    param([string]$Source, [string]$Property)
    $facts = $script:World.Sql
    switch -CaseSensitive ($Source.ToUpperInvariant() + ':' + $Property.ToLowerInvariant()) {
        'CONNECTIONPROPERTY:net_transport' { return [string]$facts.NetTransport }
        'SERVERPROPERTY:machinename' { return [string]$facts.MachineName }
        'SERVERPROPERTY:instancename' { return [string]$facts.InstanceName }
        'SERVERPROPERTY:productversion' { return [string]$facts.ProductVersion }
        'IS_SRVROLEMEMBER:sysadmin' { return [int]$facts.IsSysadmin }
    }
    # Any other property reads as NULL, as SERVERPROPERTY does for an unknown name.
    return $null
}

function Test-VersionServerComparison {
    param([Text.RegularExpressions.Match]$Comparison, [hashtable]$Parameters)
    $source = $Comparison.Groups['source'].Value.ToUpperInvariant()
    $property = $Comparison.Groups['property'].Value
    $collation = $Comparison.Groups['collation'].Value
    $operand = $Comparison.Groups['operand'].Value
    $server = Get-VersionServerValue -Source $source -Property $property
    if ($operand.StartsWith('@')) {
        $name = $operand.Substring(1)
        if (-not $Parameters.ContainsKey($name)) { throw 'VERSION_FAKE_SQL_PARAMETER_MISSING' }
        $compared = $Parameters[$name]
    }
    elseif ($operand.StartsWith("N'")) { $compared = $operand.Substring(2, $operand.Length - 3) }
    else { $compared = [int]$operand }
    # Engine semantics: NULL <> x is UNKNOWN, so IF does not THROW; <> ignores trailing spaces; only *_BIN/*_BIN2
    # collations compare case exactly (the instance default collation is case-insensitive).
    if ($null -eq $server -or $null -eq $compared) {
        $differs = $false
    }
    elseif ($server -is [int] -or $compared -is [int]) {
        $differs = [long]$server -ne [long]$compared
    }
    else {
        $mode = [StringComparison]::OrdinalIgnoreCase
        if ($collation -match '_BIN2?$') { $mode = [StringComparison]::Ordinal }
        $differs = -not [string]::Equals(([string]$server).TrimEnd(' '), ([string]$compared).TrimEnd(' '), $mode)
    }
    return [ordered]@{
        Key = $source + ':' + $property.ToLowerInvariant()
        Collation = $collation
        Operand = $operand
        Server = $server
        Compared = $compared
        Differs = $differs
    }
}

function Invoke-VersionSqlServer {
    param($Command, [string]$Mode)
    $sql = [string]$Command.CommandText
    $parameters = @{}
    foreach ($parameter in $Command.Parameters) {
        $parameters[$parameter.ParameterName.TrimStart('@')] = $parameter.Value
    }
    $entry = [ordered]@{
        ConnectionId = $Command.ConnectionId
        Mode = $Mode
        Head = ($sql.Trim() -split "`n")[0].Trim()
        Parameters = $parameters
        Columns = @()
        Comparisons = @()
        Outcome = 'Unsupported'
    }
    try {
        # Table or catalog reads would reach existing databases; the version boundary reads server functions only.
        if ($sql -match '\bFROM\b') {
            $entry.Outcome = 'TableRead'
            throw 'VERSION_FAKE_SQL_TABLE_READ'
        }
        if ($sql.Contains('sp_getapplock')) {
            $entry.Outcome = 'LockGranted'
            return 0
        }
        if ($Mode -ceq 'Reader') {
            $columns = [regex]::Matches($sql, $script:ServerFunctionPattern)
            if ($columns.Count -eq 0) { throw 'VERSION_FAKE_SQL_UNSUPPORTED' }
            $keys = [Collections.Generic.List[string]]::new()
            $values = [Collections.Generic.List[object]]::new()
            foreach ($column in $columns) {
                $source = $column.Groups['source'].Value
                $property = $column.Groups['property'].Value
                $keys.Add($source.ToUpperInvariant() + ':' + $property.ToLowerInvariant())
                $values.Add((Get-VersionServerValue -Source $source -Property $property))
            }
            $entry.Columns = $keys.ToArray()
            $entry.Outcome = 'Row'
            return , $values.ToArray()
        }
        $statement = [regex]::Match($sql, '(?is)\bIF\b(?<predicate>.*?)\bTHROW\s+(?<number>[0-9]+)')
        if (-not $statement.Success) { throw 'VERSION_FAKE_SQL_UNSUPPORTED' }
        $predicate = $statement.Groups['predicate'].Value
        $found = [regex]::Matches($predicate, $script:ComparisonPattern)
        $remainder = [regex]::Replace($predicate, $script:ComparisonPattern, ' ')
        $remainder = [regex]::Replace($remainder, '(?i)CONVERT\(\s*nvarchar\(\s*128\s*\)\s*,', ' ')
        # Only OR-joined comparisons of server functions are simulated; any other predicate stops the scenario.
        if ($found.Count -eq 0 -or $remainder -notmatch '^(\s|\bOR\b)*$') { throw 'VERSION_FAKE_SQL_UNSUPPORTED' }
        $evaluated = [Collections.Generic.List[object]]::new()
        foreach ($comparison in $found) {
            $evaluated.Add((Test-VersionServerComparison -Comparison $comparison -Parameters $parameters))
        }
        $entry.Comparisons = $evaluated.ToArray()
        if (@($evaluated | Where-Object { $_.Differs }).Count -gt 0) {
            $entry.Outcome = 'Thrown'
            throw ('VERSION_FAKE_SQL_THROW_' + $statement.Groups['number'].Value)
        }
        $entry.Outcome = 'Passed'
        return -1
    }
    finally {
        Write-VersionLog -Kind 'SqlStatement' -Data $entry
    }
}

function ConvertTo-VersionWorld {
    param([Collections.IDictionary]$Scenario)
    # Registry key and value names are case-insensitive, as in Windows.
    $registry = [hashtable]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $Scenario.Registry.Keys) {
        $values = [hashtable]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($name in $Scenario.Registry[$path].Keys) {
            $entry = $Scenario.Registry[$path][$name]
            $value = if ($entry.Kind -ceq 'DWord') { [int]$entry.Value } else { [string]$entry.Value }
            $values[$name] = [pscustomobject]@{ Kind = [string]$entry.Kind; Value = $value }
        }
        $registry[$path] = $values
    }
    return [pscustomobject]@{ Registry = $registry; Sql = [pscustomobject]$Scenario.Sql }
}

function Install-VersionTypeStandIns {
    param([switch]$HostIdentity)
    [VersionFakeRegistryKey]::OnOpenBaseKey = {
        param($Hive, $View)
        Write-VersionLog -Kind 'RegistryBase' -Data @{ Hive = [string]$Hive; View = [string]$View }
        # Only the 64-bit local machine view is simulated; any other hive or view stops the scenario.
        if ([string]$Hive -cne 'LocalMachine' -or [string]$View -cne 'Registry64') {
            throw 'VERSION_FAKE_REGISTRY_VIEW_UNSUPPORTED'
        }
        return New-VersionRegistryKey -Path ''
    }
    [VersionFakeSqlConnection]::OnCreate = {
        param($Connection)
        $script:ConnectionCount++
        $Connection.Id = $script:ConnectionCount
        Write-VersionLog -Kind 'SqlConnection' -Data @{
            Id = $Connection.Id
            ConnectionString = $Connection.ConnectionString
        }
    }
    [VersionFakeSqlConnection]::OnOpen = {
        param($Connection)
        Write-VersionLog -Kind 'SqlOpen' -Data @{ Id = $Connection.Id }
        if ($script:World.Sql.OpenFails) { throw 'VERSION_FAKE_LPC_UNAVAILABLE' }
    }
    [VersionFakeSqlConnection]::OnCommand = {
        param($Connection)
        return New-VersionSqlCommand -ConnectionId $Connection.Id
    }
    $standIns = [ordered]@{
        'Microsoft.Win32.RegistryKey' = [VersionFakeRegistryKey]
        'Microsoft.Win32.Registry' = [VersionFakeRegistry]
        'Data.SqlClient.SqlConnection' = [VersionFakeSqlConnection]
        'System.Data.SqlClient.SqlConnection' = [VersionFakeSqlConnection]
        'Microsoft.Data.SqlClient.SqlConnection' = [VersionFakeSqlConnection]
    }
    if ($HostIdentity) {
        [VersionFakeEnvironment]::MachineName = $script:FixtureMachine
        [VersionFakeEnvironment]::NewLine = [System.Environment]::NewLine
        $standIns['Environment'] = [VersionFakeEnvironment]
        $standIns['System.Environment'] = [VersionFakeEnvironment]
    }
    $accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
    foreach ($name in $standIns.Keys) {
        $accelerators::Add($name, $standIns[$name])
        # Newly parsed text must bind the name to the stand-in before any target text is parsed.
        $bound = & ([scriptblock]::Create('[' + $name + ']'))
        if ($bound.FullName -cne $standIns[$name].FullName) { throw ('VERSION_STANDIN_NOT_BOUND ' + $name) }
    }
}

function Test-VersionTypeReferences {
    param([object[]]$Roots, [string[]]$Protected)
    $violations = [Collections.Generic.List[string]]::new()
    $standIns = [Collections.Generic.HashSet[string]]::new()
    foreach ($root in $Roots) {
        $typeNodes = $root.FindAll({
                param($node)
                $node -is [Management.Automation.Language.TypeExpressionAst] -or
                $node -is [Management.Automation.Language.TypeConstraintAst]
            }, $true)
        foreach ($node in $typeNodes) {
            $type = $node.TypeName.GetReflectionType()
            if ($null -eq $type) { continue }
            $where = [string]$node.Extent.File + ':' + $node.Extent.StartLineNumber
            if ($type.FullName -cin $Protected) { $violations.Add($where + ' ' + $type.FullName) }
            elseif ($type.FullName -clike 'VersionFake*') { [void]$standIns.Add($type.FullName) }
        }
        # Provider paths reach the registry without any guarded type name.
        if ($root.Extent.Text -match '\bHKLM:|HKEY_LOCAL_MACHINE\\|Registry::') {
            $violations.Add([string]$root.Extent.File + ' registry provider path')
        }
    }
    $bothBound = $standIns.Contains('VersionFakeRegistryKey') -and $standIns.Contains('VersionFakeSqlConnection')
    return [ordered]@{
        Passed = $violations.Count -eq 0 -and $bothBound
        StandIns = @($standIns)
        Violations = $violations.ToArray()
    }
}

function Test-VersionProductTypes {
    param([string]$ProductRoot)
    $databaseRoot = [IO.Path]::GetFullPath((Join-Path $ProductRoot '..'))
    # Every file the product can dot-source: the windows-sql scripts and the database tool scripts beside them.
    $files = @([IO.Directory]::GetFiles($ProductRoot, '*.ps1')) +
    @([IO.Directory]::GetFiles($databaseRoot, '*.ps1'))
    $roots = [Collections.Generic.List[object]]::new()
    $parseErrors = 0
    foreach ($file in $files) {
        $tokens = $null
        $errors = $null
        $roots.Add([Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$errors))
        $parseErrors += @($errors).Count
    }
    $result = Test-VersionTypeReferences -Roots $roots.ToArray() -Protected $script:ProtectedTypeNames
    $result.Paths = @($files | ForEach-Object { [IO.Path]::GetFullPath($_) })
    $result.ParseErrors = $parseErrors
    if ($parseErrors -gt 0) { $result.Passed = $false }
    return $result
}

function Test-VersionLoadedFunctions {
    param([string[]]$GuardedPaths)
    # Every function the product defined must come from a file the type guard inspected before loading.
    $unguarded = @(Get-Command -CommandType Function | Where-Object {
            $file = $_.ScriptBlock.File
            $file -and $file -cne $PSCommandPath -and [IO.Path]::GetFullPath($file) -notin $GuardedPaths
        } | ForEach-Object { $_.Name + '@' + $_.ScriptBlock.File })
    return [ordered]@{ Passed = $unguarded.Count -eq 0; Unguarded = $unguarded }
}

function Install-VersionCommandStubs {
    param([switch]$ProductGates)
    foreach ($name in $script:ForbiddenCommands) {
        Set-Item -Path ('Function:script:' + $name) -Value ([scriptblock]::Create(
                "Write-VersionLog -Kind 'Forbidden' -Data @{ Name = '$name' }; throw 'VERSION_FORBIDDEN_$name'"))
    }
    if ($ProductGates) {
        # OS identity, certificate store and WSL host facts cannot be produced for the fixture. Their real rejections
        # are covered by WindowsSqlAdapterBoundary (IND-EXEC-*, IND-CERT-*); here each call is recorded only.
        foreach ($name in @('Assert-WindowsSqlExecutor', 'Assert-WindowsSqlCertificate', 'Get-WindowsSqlVerifyHost')) {
            Set-Item -Path ('Function:script:' + $name) -Value ([scriptblock]::Create(
                    "Write-VersionLog -Kind 'Gate' -Data @{ Name = '$name' }"))
        }
    }
}

function New-VersionPlan {
    param([string]$ExpectedProductVersion, [string]$SourceRoot)
    # Closed plan fields as Assert-WindowsSqlPlanFields reads them; only the version varies between scenarios.
    return [pscustomobject][ordered]@{
        Version = 1
        ExecutionApproved = $true
        ExpiresUtc = [DateTimeOffset]::UtcNow.AddHours(3).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'",
            [Globalization.CultureInfo]::InvariantCulture)
        LifetimeId = [Guid]::NewGuid().ToString('N')
        MachineName = $script:FixtureMachine
        InstanceName = $script:FixtureInstance
        InstanceId = $script:FixtureInstanceId
        ExpectedProductVersion = $ExpectedProductVersion
        Port = 15433
        Database = 'Dawnholder_VersionFixture'
        RuntimeLogin = 'dh_version_fixture'
        SchemaVersion = 4
        CertificateThumbprint = 'AB' * 20
        Encrypt = 'Mandatory'
        TrustServerCertificate = $false
        EnableSa = $false
        SwitchWslNetworkingMode = $false
        ForceEncryption = 'Preserve'
        RestartApproved = $false
        FirewallApproved = $false
        SourceRoot = $SourceRoot
        EvidenceRoot = Join-Path $SourceRoot '.backups\evidence'
        PrivateRoot = Join-Path $SourceRoot 'private-unused'
        Inputs = @()
        PlanHash = 'A' * 64
    }
}

function New-VersionAdapter {
    param($Plan)
    $adapter = [pscustomobject]@{
        Kind = 'VersionProtectedIoStandIn'
        Plan = $Plan
        Runtime = [pscustomobject]@{ ExecutorSid = 'S-1-5-21-1-2-3-1001' }
        RuntimePath = 'runtime.json'
        Master = $null
        ExpectedRegistry = $null
        LastTls = $null
    }
    $adapter | Add-Member -MemberType ScriptMethod -Name InvokeOperation -Value {
        param([string]$Operation, $Request)
        Write-VersionLog -Kind 'AdapterOperation' -Data @{ Operation = $Operation }
        switch -CaseSensitive ($Operation) {
            # The host name read is the one dispatcher branch replaced, as in WindowsSqlAdapterBoundary.
            'ReadMachineIdentity' { return [pscustomobject]@{ MachineName = $script:FixtureMachine } }
            'ReadInstanceIdentity' {
                try { return Invoke-WindowsSqlNativeOperation -Adapter $this -Operation $Operation -Request $Request }
                catch {
                    # Preflight replaces the native code with its own; the cause is kept for the assertions.
                    Write-VersionLog -Kind 'NativeError' -Data @{ Message = $_.Exception.Message }
                    throw
                }
            }
        }
        # Sections after the identity belong to other suites; a canned value lets Preflight finish.
        return [pscustomobject]@{ StandInFor = $Operation }
    }
    return $adapter
}

function Invoke-VersionProductTarget {
    param($Adapter, [string]$Target)
    try {
        if ($Target -ceq 'Preflight') {
            $step = Invoke-WindowsSqlStep -Plan $Adapter.Plan -Step 'Preflight' -Adapter $Adapter
            $instance = $step.Observation.Instance
        }
        else {
            $instance = Invoke-WindowsSqlNativeOperation -Adapter $Adapter -Operation 'ReadInstanceIdentity' `
                -Request (New-WindowsSqlRequest -Plan $Adapter.Plan)
        }
        $fields = if ($instance -is [Collections.IDictionary]) {
            @($instance.Keys)
        } else { @($instance.PSObject.Properties.Name) }
        $masterId = if ($null -ne $Adapter.Master) { $Adapter.Master.Id } else { $null }
        Write-VersionLog -Kind 'Result' -Data @{
            Instance = $instance
            Fields = $fields
            MasterConnectionId = $masterId
        }
    }
    catch {
        Write-VersionLog -Kind 'Error' -Data @{ Message = $_.Exception.Message }
    }
    finally {
        if ($null -ne $Adapter.Master) { $Adapter.Master.Dispose() }
    }
}

function Get-VersionCardProbe {
    param([string]$Path)
    # The probe is the one script block in the card's first powershell block that declares OutputDirectory.
    $text = [IO.File]::ReadAllText($Path)
    $block = [regex]::Match($text, '(?s)```powershell\r?\n(?<code>.*?)\r?\n```')
    if (-not $block.Success) { return $null }
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseInput($block.Groups['code'].Value, [ref]$tokens, [ref]$errors)
    if (@($errors).Count -gt 0) { return $null }
    $probes = @($ast.FindAll({
                param($node)
                $node -is [Management.Automation.Language.ScriptBlockExpressionAst] -and
                $null -ne $node.ScriptBlock.ParamBlock -and
                @($node.ScriptBlock.ParamBlock.Parameters | ForEach-Object {
                        $_.Name.VariablePath.UserPath }) -ccontains 'OutputDirectory'
            }, $true))
    if ($probes.Count -ne 1) { return $null }
    return $probes[0]
}

if ($PSCmdlet.ParameterSetName -ceq 'Scenario') {
    Set-StrictMode -Version Latest
    $ErrorActionPreference = 'Stop'
    $scenario = [IO.File]::ReadAllText($ScenarioPath) | ConvertFrom-Json -AsHashtable
    $script:LogPath = [string]$scenario.LogPath
    $script:World = ConvertTo-VersionWorld -Scenario $scenario
    $script:ConnectionCount = 0
    try {
        if ($scenario.Kind -ceq 'Card') {
            Install-VersionTypeStandIns -HostIdentity
            $probe = Get-VersionCardProbe -Path ([string]$scenario.CardPath)
            if ($null -eq $probe) {
                Write-VersionLog -Kind 'CardProbe' -Data @{ Found = $false }
                exit $script:ChildProbeMissing
            }
            $guard = Test-VersionTypeReferences -Roots @($probe) `
                -Protected ($script:ProtectedTypeNames + 'System.Environment')
            Write-VersionLog -Kind 'Guard' -Data $guard
            if (-not $guard.Passed) { exit $script:ChildGuardFailed }
            Install-VersionCommandStubs
            $probeInputs = @{
                MachineName = $script:FixtureMachine
                InstanceName = $script:FixtureInstance
                InstanceId = $script:FixtureInstanceId
                OutputDirectory = [string]$scenario.OutputDirectory
            }
            $probeBlock = $probe.ScriptBlock.GetScriptBlock()
            # The probe ends this process with its own exit code; returning instead is a harness finding.
            & $probeBlock @probeInputs
            Write-VersionLog -Kind 'CardProbe' -Data @{ Found = $true; Returned = $true }
            exit $script:ChildProbeReturned
        }
        Install-VersionTypeStandIns
        $productRoot = [string]$scenario.ProductRoot
        $guard = Test-VersionProductTypes -ProductRoot $productRoot
        Write-VersionLog -Kind 'Guard' -Data $guard
        if (-not $guard.Passed) { exit $script:ChildGuardFailed }
        . (Join-Path $productRoot 'WindowsSql.Common.ps1')
        . (Join-Path $productRoot 'WindowsSql.Adapter.ps1')
        $loaded = Test-VersionLoadedFunctions -GuardedPaths $guard.Paths
        Write-VersionLog -Kind 'GuardLoaded' -Data $loaded
        if (-not $loaded.Passed) { exit $script:ChildGuardFailed }
        Install-VersionCommandStubs -ProductGates
        $plan = New-VersionPlan -ExpectedProductVersion ([string]$scenario.ExpectedProductVersion) `
            -SourceRoot ([IO.Path]::GetDirectoryName($ScenarioPath))
        Invoke-VersionProductTarget -Adapter (New-VersionAdapter -Plan $plan) -Target ([string]$scenario.Target)
        exit 0
    }
    catch {
        Write-VersionLog -Kind 'HarnessError' -Data @{ Message = $_.Exception.Message }
        exit $script:ChildHarnessFailed
    }
}

# =====================================================================================================
# Suite: scenario builders, child runner and requirement assertions.
# =====================================================================================================
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-version'

# The -ProductRoot parameter is this same script-scope variable; empty means the checked-out windows-sql folder.
if (-not $ProductRoot) { $ProductRoot = Join-Path $script:ToolRoot 'windows-sql' }
elseif (-not [IO.Path]::IsPathRooted($ProductRoot)) { throw 'ProductRoot must be an explicit absolute path.' }
$script:ProductRoot = [IO.Path]::GetFullPath($ProductRoot)
if (-not [IO.File]::Exists((Join-Path $script:ProductRoot 'WindowsSql.Instance.ps1'))) {
    throw ('ProductRoot has no WindowsSql.Instance.ps1: ' + $script:ProductRoot)
}
if (-not [IO.Path]::IsPathRooted($CardPath)) { throw 'CardPath must be an explicit absolute path.' }
$script:CardPath = [IO.Path]::GetFullPath($CardPath)
if (-not [IO.File]::Exists($script:CardPath)) { throw ('CardPath does not exist: ' + $script:CardPath) }
if ([Environment]::MachineName -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run.'
}
$script:PwshPath = [Diagnostics.Process]::GetCurrentProcess().Path

function Get-VersionFileSha256 {
    param([string]$Path)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($Path)))
}

function Get-VersionReparseAncestors {
    param([string]$Path)
    for ($directory = [IO.DirectoryInfo]::new($Path); $null -ne $directory; $directory = $directory.Parent) {
        if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { $directory.FullName }
    }
}

function Get-VersionProperty {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    if ($Object -is [Collections.IDictionary]) { return $Object[$Name] }
    $member = $Object.PSObject.Properties[$Name]
    if ($null -eq $member) { return $null }
    return $member.Value
}

function New-VersionRegistry {
    param([string]$SetupVersion, $PatchLevel, [string]$PatchLevelKind = 'String',
        [string]$MappedInstanceId = $script:FixtureInstanceId)
    $setup = [ordered]@{ Version = @{ Kind = 'String'; Value = $SetupVersion } }
    # A null PatchLevel means the value is absent; an empty string is a present, empty value.
    if ($null -ne $PatchLevel) { $setup.PatchLevel = @{ Kind = $PatchLevelKind; Value = $PatchLevel } }
    return [ordered]@{
        $script:InstanceMapPath = [ordered]@{
            $script:FixtureInstance = @{ Kind = 'String'; Value = $MappedInstanceId }
        }
        $script:SetupPath = $setup
    }
}

function New-VersionSqlFacts {
    param([string]$ProductVersion, [hashtable]$Overrides = @{})
    $facts = [ordered]@{
        NetTransport = 'Shared memory'
        MachineName = $script:FixtureMachine
        InstanceName = $script:FixtureInstance
        ProductVersion = $ProductVersion
        IsSysadmin = 1
        OpenFails = $false
    }
    foreach ($key in $Overrides.Keys) { $facts[$key] = $Overrides[$key] }
    return $facts
}

function New-VersionCase {
    param(
        [string]$Id,
        [string]$Title,
        [ValidateSet('Pass', 'Block')][string]$Expect,
        [string]$SetupVersion,
        $PatchLevel,
        [string]$SqlVersion,
        [string]$Approved = '',
        [string]$PatchLevelKind = 'String',
        [string]$Target = 'Native',
        [hashtable]$Sql = @{},
        [string]$MappedInstanceId = $script:FixtureInstanceId,
        [string]$Cause = '',
        [string]$Control = '',
        [string[]]$Recorded = @(),
        [switch]$Stale,
        [switch]$Detailed
    )
    return [pscustomobject]@{
        Id = $Id
        Title = $Title
        Expect = $Expect
        Target = $Target
        Approved = $Approved
        Cause = $Cause
        Control = $Control
        Recorded = $Recorded
        Stale = $Stale.IsPresent
        Detailed = $Detailed.IsPresent
        Registry = New-VersionRegistry -SetupVersion $SetupVersion -PatchLevel $PatchLevel `
            -PatchLevelKind $PatchLevelKind -MappedInstanceId $MappedInstanceId
        Sql = New-VersionSqlFacts -ProductVersion $SqlVersion -Overrides $Sql
    }
}

function Invoke-VersionChild {
    param([string]$Id, [Collections.IDictionary]$Scenario)
    $directory = Join-Path $script:SuiteRoot ('scenarios\' + $Id)
    [void][IO.Directory]::CreateDirectory($directory)
    $Scenario['LogPath'] = Join-Path $directory 'observations.jsonl'
    $scenarioPath = Join-Path $directory 'scenario.json'
    $utf8 = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText($scenarioPath, (ConvertTo-Json -InputObject $Scenario -Depth 10) + "`n", $utf8)
    $start = [Diagnostics.ProcessStartInfo]::new($script:PwshPath)
    foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $PSCommandPath,
            '-ScenarioPath', $scenarioPath)) {
        $start.ArgumentList.Add($argument)
    }
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $startedUtc = [DateTime]::UtcNow
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $timedOut = -not $process.WaitForExit(60000)
        if ($timedOut) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $exitCode = $process.ExitCode
    }
    finally { $process.Dispose() }
    $finishedUtc = [DateTime]::UtcNow
    [IO.File]::WriteAllText((Join-Path $directory 'stdout.txt'), $stdout.Result, $utf8)
    [IO.File]::WriteAllText((Join-Path $directory 'stderr.txt'), $stderr.Result, $utf8)
    [IO.File]::WriteAllText((Join-Path $directory 'exit.txt'), [string]$exitCode + "`n", $utf8)
    $entries = @()
    if ([IO.File]::Exists($Scenario['LogPath'])) {
        $entries = @([IO.File]::ReadAllLines($Scenario['LogPath']) | Where-Object { $_ } | ForEach-Object {
                ConvertFrom-Json -InputObject $_ -DateKind String })
    }
    return [pscustomobject]@{
        Id = $Id
        Exit = $exitCode
        TimedOut = $timedOut
        StdErr = $stderr.Result
        Entries = $entries
        StartedUtc = $startedUtc
        FinishedUtc = $finishedUtc
    }
}

function Get-VersionEntries {
    param($Run, [string]$Kind)
    return @($Run.Entries | Where-Object { $_.Kind -ceq $Kind })
}

function Test-VersionPatchLevelConsulted {
    param($Run)
    $reads = @($Run.Entries | Where-Object {
            $_.Kind -cin @('RegistryRead', 'RegistryKindRead') -and [string]$_.Name -ieq 'PatchLevel' -and
            [string]::Equals([string]$_.Path, $script:SetupPath, [StringComparison]::OrdinalIgnoreCase)
        })
    return $reads.Count -gt 0
}

function Get-VersionCheckedKeys {
    param($Run, $ConnectionId)
    $statements = @(Get-VersionEntries -Run $Run -Kind 'SqlStatement' | Where-Object {
            $null -ne $ConnectionId -and $_.ConnectionId -eq $ConnectionId
        })
    $keys = @($statements | ForEach-Object { @($_.Comparisons) } | ForEach-Object { [string]$_.Key }) +
    @($statements | ForEach-Object { @($_.Columns) } | ForEach-Object { [string]$_ })
    return @($keys | Sort-Object -Unique)
}

$script:SameConnectionKeys = @(
    'CONNECTIONPROPERTY:net_transport',
    'IS_SRVROLEMEMBER:sysadmin',
    'SERVERPROPERTY:instancename',
    'SERVERPROPERTY:machinename',
    'SERVERPROPERTY:productversion'
)

function Assert-VersionHarnessBoundary {
    param([string]$Id, $Run, [int[]]$AllowedExit)
    $guards = @(Get-VersionEntries -Run $Run -Kind 'Guard') + @(Get-VersionEntries -Run $Run -Kind 'GuardLoaded')
    $guardPassed = $guards.Count -gt 0 -and @($guards | Where-Object { -not $_.Passed }).Count -eq 0
    $forbidden = @(Get-VersionEntries -Run $Run -Kind 'Forbidden' | ForEach-Object { [string]$_.Name })
    $registryWrites = @(Get-VersionEntries -Run $Run -Kind 'RegistryOpen' | Where-Object { $_.Writable }).Count
    $otherViews = @(Get-VersionEntries -Run $Run -Kind 'RegistryBase' | Where-Object {
            $_.Hive -cne 'LocalMachine' -or $_.View -cne 'Registry64' }).Count
    $statements = @(Get-VersionEntries -Run $Run -Kind 'SqlStatement')
    $tableReads = @($statements | Where-Object { $_.Outcome -ceq 'TableRead' }).Count
    # A statement the stand-in server cannot evaluate would make a refusal pass for the wrong reason.
    $unsupported = @($statements | Where-Object { $_.Outcome -ceq 'Unsupported' }).Count
    $harnessErrors = @(Get-VersionEntries -Run $Run -Kind 'HarnessError' | ForEach-Object { [string]$_.Message })
    $exitAllowed = $Run.Exit -in $AllowedExit
    $held = $guardPassed -and -not $Run.TimedOut -and $forbidden.Count -eq 0 -and $registryWrites -eq 0 -and
    $otherViews -eq 0 -and $tableReads -eq 0 -and $unsupported -eq 0 -and $harnessErrors.Count -eq 0 -and $exitAllowed
    $detail = ('guard={0} timeout={1} forbidden={2} registryWrites={3} otherViews={4} tableReads={5} ' +
        'unsupportedSql={6} harness={7} exit={8} stderr={9}') -f $guardPassed, $Run.TimedOut, ($forbidden -join ','),
    $registryWrites, $otherViews, $tableReads, $unsupported, ($harnessErrors -join ','), $Run.Exit,
    ([string]$Run.StdErr).Trim()
    Assert-True -Name ($Id + ' protected I/O stayed inside the stand-ins') -Condition $held -Detail $detail
}

function Assert-VersionAdminConnection {
    param([string]$Id, $Run)
    $connections = @(Get-VersionEntries -Run $Run -Kind 'SqlConnection')
    $opens = @(Get-VersionEntries -Run $Run -Kind 'SqlOpen').Count
    Assert-Equal -Name ($Id + ' one LPC connection is built and opened once (no fallback reconnect)') `
        -Expected '1/1' -Actual ('{0}/{1}' -f $connections.Count, $opens)
    $exactSource = $false
    $integrated = $false
    $plainLocal = $false
    $noSecret = $false
    if ($connections.Count -ge 1) {
        # Parse through the constructor; a property assignment would go to the keyword indexer.
        $builder = [Data.SqlClient.SqlConnectionStringBuilder]::new([string]$connections[0].ConnectionString)
        $exactSource = [string]$builder['Data Source'] -ceq $script:SqlTarget
        $integrated = $builder['Integrated Security'] -eq $true
        $plainLocal = $builder['Encrypt'] -eq $false -and $builder['TrustServerCertificate'] -eq $false
        $noSecret = [string]$builder['User ID'] -ceq '' -and [string]$builder['Password'] -ceq ''
    }
    $detail = 'source={0} integrated={1} plain={2} noSecret={3}' -f $exactSource, $integrated, $plainLocal, $noSecret
    Assert-True -Name ($Id + ' connection is exact lpc + integrated, Encrypt=false, TSC=false, no SQL credential') `
        -Condition ($exactSource -and $integrated -and $plainLocal -and $noSecret) -Detail $detail
}

# ---- Product assertions -------------------------------------------------------------------------------------------
function Assert-VersionIdentityAccepted {
    param($Case, $Run)
    $results = @(Get-VersionEntries -Run $Run -Kind 'Result')
    $errors = @(Get-VersionEntries -Run $Run -Kind 'Error' | ForEach-Object { [string]$_.Message })
    $native = @(Get-VersionEntries -Run $Run -Kind 'NativeError' | ForEach-Object { [string]$_.Message })
    $accepted = $results.Count -eq 1 -and $errors.Count -eq 0
    Assert-True -Name ($Case.Id + ' identity is accepted: ' + $Case.Title) -Condition $accepted `
        -Detail (@($errors + $native) -join ',')
    $instance = if ($accepted) { $results[0].Instance } else { $null }
    $version = Get-VersionProperty -Object $instance -Name 'ProductVersion'
    Assert-Equal -Name ($Case.Id + ' reported engine version is the approved value') -Expected $Case.Approved `
        -Actual $(if ($null -eq $version) { '<none>' } else { [string]$version })
    $instanceName = [string](Get-VersionProperty -Object $instance -Name 'InstanceName')
    $instanceId = [string](Get-VersionProperty -Object $instance -Name 'InstanceId')
    $exactIdentity = $instanceName -ceq $script:FixtureInstance -and $instanceId -ceq $script:FixtureInstanceId
    Assert-True -Name ($Case.Id + ' reported instance name and id are the approved ones') -Condition $exactIdentity
    Assert-True -Name ($Case.Id + ' engine version source is Setup/PatchLevel of the approved InstanceId') `
        -Condition (Test-VersionPatchLevelConsulted -Run $Run)
    if ($Case.Target -ceq 'Preflight') {
        $operations = @(Get-VersionEntries -Run $Run -Kind 'AdapterOperation' | ForEach-Object { [string]$_.Operation })
        Assert-True -Name ($Case.Id + ' Preflight continues past the identity to the remaining observations') `
            -Condition ($operations -ccontains 'ReadTargetLogin') -Detail ($operations -join ',')
    }
    if ($Case.Detailed) {
        Assert-VersionAdminConnection -Id $Case.Id -Run $Run
        $masterId = if ($accepted) { $results[0].MasterConnectionId } else { $null }
        $checked = @(Get-VersionCheckedKeys -Run $Run -ConnectionId $masterId)
        $missing = @($script:SameConnectionKeys | Where-Object { $_ -cnotin $checked })
        Assert-True -Name ($Case.Id + ' transport, machine, instance, version, sysadmin run on the kept connection') `
            -Condition ($null -ne $masterId -and $missing.Count -eq 0) -Detail ('missing=' + ($missing -join ','))
        $operands = @(Get-VersionEntries -Run $Run -Kind 'SqlStatement' | ForEach-Object { @($_.Comparisons) } |
                Where-Object { $_.Key -ceq 'SERVERPROPERTY:productversion' } | ForEach-Object { [string]$_.Compared })
        Assert-True -Name ($Case.Id + ' SQL ProductVersion is compared with the approved version') -Condition (
            $operands.Count -gt 0 -and @($operands | Where-Object { $_ -cne $Case.Approved }).Count -eq 0) `
            -Detail ($operands -join ',')
        $gates = @(Get-VersionEntries -Run $Run -Kind 'Gate' | ForEach-Object { [string]$_.Name } | Sort-Object)
        Assert-Equal -Name ($Case.Id + ' executor, certificate and WSL host gates still run once each') `
            -Expected 'Assert-WindowsSqlCertificate|Assert-WindowsSqlExecutor|Get-WindowsSqlVerifyHost' `
            -Actual ($gates -join '|')
    }
}

function Assert-VersionIdentityRefused {
    param($Case, $Run)
    $results = @(Get-VersionEntries -Run $Run -Kind 'Result')
    $errors = @(Get-VersionEntries -Run $Run -Kind 'Error' | ForEach-Object { [string]$_.Message })
    $failedClosed = $results.Count -eq 0 -and $errors.Count -eq 1
    $publicCode = $failedClosed -and $errors[0] -cmatch '^WINDOWS_SQL_[A-Z_]+$'
    $accepted = if ($results.Count -gt 0) {
        'accepted ProductVersion=' + [string](Get-VersionProperty -Object $results[0].Instance -Name 'ProductVersion')
    } else { $errors -join ',' }
    Assert-True -Name ($Case.Id + ' is refused with a public code and no identity: ' + $Case.Title + ' (control ' +
        $Case.Control + ')') -Condition $publicCode -Detail $accepted
    switch -CaseSensitive ($Case.Cause) {
        'PatchLevel' {
            Assert-True -Name ($Case.Id + ' refusal follows a read of Setup/PatchLevel (no Version fallback)') `
                -Condition (Test-VersionPatchLevelConsulted -Run $Run)
        }
        'Map' {
            $mapReads = @(Get-VersionEntries -Run $Run -Kind 'RegistryRead' | Where-Object {
                    [string]$_.Path -ieq $script:InstanceMapPath -and [string]$_.Name -ieq $script:FixtureInstance })
            Assert-True -Name ($Case.Id + ' refusal follows the InstanceName -> InstanceId map read') `
                -Condition ($mapReads.Count -gt 0)
        }
        'Open' {
            $connections = @(Get-VersionEntries -Run $Run -Kind 'SqlConnection').Count
            $opens = @(Get-VersionEntries -Run $Run -Kind 'SqlOpen').Count
            Assert-Equal -Name ($Case.Id + ' a failed LPC open is not retried on another connection') `
                -Expected '1/1' -Actual ('{0}/{1}' -f $connections, $opens)
        }
        default {
            # Server:<key>: the stand-in server rejected exactly this comparison sent by the product.
            $key = $Case.Cause.Substring(7)
            $rejected = @(Get-VersionEntries -Run $Run -Kind 'SqlStatement' | Where-Object {
                    $_.Outcome -ceq 'Thrown' } | ForEach-Object { @($_.Comparisons) } | Where-Object {
                    $_.Differs -and $_.Key -ceq $key })
            Assert-True -Name ($Case.Id + ' the rejecting check is the server-side ' + $key + ' comparison') `
                -Condition ($rejected.Count -gt 0)
        }
    }
    if ($Case.Target -ceq 'Preflight') {
        $operations = @(Get-VersionEntries -Run $Run -Kind 'AdapterOperation' | ForEach-Object { [string]$_.Operation })
        Assert-True -Name ($Case.Id + ' Preflight stops at the identity before reading further sections') `
            -Condition ($operations -cnotcontains 'ReadInstanceConfiguration') -Detail ($operations -join ',')
    }
}

# ---- Card assertions ----------------------------------------------------------------------------------------------
# The card's JSON layout is its own; assertions look for recorded values and names, not for a fixed schema.
function Get-VersionJsonLeaves {
    param($Value)
    if ($null -eq $Value) { return }
    if ($Value -is [Collections.IDictionary]) {
        foreach ($key in $Value.Keys) { Get-VersionJsonLeaves -Value $Value[$key] }
        return
    }
    if ($Value -is [Collections.IList]) {
        foreach ($item in $Value) { Get-VersionJsonLeaves -Value $item }
        return
    }
    [string]$Value
}

function Get-VersionJsonNames {
    param($Value)
    if ($Value -is [Collections.IDictionary]) {
        foreach ($key in $Value.Keys) {
            [string]$key
            Get-VersionJsonNames -Value $Value[$key]
        }
    }
    elseif ($Value -is [Collections.IList]) {
        foreach ($item in $Value) { Get-VersionJsonNames -Value $item }
    }
}

function Test-VersionUtcInRun {
    param([string]$Text, $Run)
    if ($Text -cnotmatch '^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9:.]+Z$') { return $false }
    $parsed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse($Text, [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$parsed)) {
        return $false
    }
    $observed = $parsed.UtcDateTime
    return $observed -ge $Run.StartedUtc.AddSeconds(-1) -and $observed -le $Run.FinishedUtc.AddSeconds(1)
}

function Read-VersionCardOutput {
    param([string]$Directory, [string[]]$Existing, $Run)
    $newFiles = @([IO.Directory]::GetFiles($Directory, '*.json') | Where-Object { $_ -notin $Existing })
    $json = $null
    $leaves = @()
    $names = @()
    $utcInRun = 0
    if ($newFiles.Count -eq 1) {
        $json = [IO.File]::ReadAllText($newFiles[0]) | ConvertFrom-Json -AsHashtable -DateKind String
        $leaves = @(Get-VersionJsonLeaves -Value $json)
        $names = @(Get-VersionJsonNames -Value $json)
        $utcInRun = @($leaves | Where-Object { Test-VersionUtcInRun -Text $_ -Run $Run }).Count
    }
    return [pscustomobject]@{
        NewFiles = $newFiles
        Status = Get-VersionProperty -Object $json -Name 'Status'
        ExitCode = Get-VersionProperty -Object $json -Name 'ExitCode'
        Leaves = $leaves
        Names = $names
        UtcInRun = $utcInRun
    }
}

function Assert-VersionCardMatched {
    param($Case, $Run, $Output)
    Assert-Equal -Name ($Case.Id + ' probe exits 0: ' + $Case.Title) -Expected '0' -Actual ([string]$Run.Exit)
    Assert-True -Name ($Case.Id + ' this run wrote exactly one new JSON') -Condition ($Output.NewFiles.Count -eq 1) `
        -Detail ([string]$Output.NewFiles.Count)
    Assert-True -Name ($Case.Id + ' new JSON says Status=Matched and ExitCode=0') -Condition (
        [string]$Output.Status -ceq 'Matched' -and $Output.ExitCode -eq 0) `
        -Detail ('{0}/{1}' -f $Output.Status, $Output.ExitCode)
    Assert-True -Name ($Case.Id + ' JSON names the Setup/PatchLevel source') `
        -Condition ($Output.Leaves -ccontains 'PatchLevel' -or $Output.Names -ccontains 'PatchLevel')
    Assert-True -Name ($Case.Id + ' engine version source is Setup/PatchLevel of the approved InstanceId') `
        -Condition (Test-VersionPatchLevelConsulted -Run $Run)
    if (-not $Case.Detailed) { return }
    $registryTarget = @($Output.Leaves | Where-Object {
            $_.StartsWith($script:SetupPath, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
    Assert-True -Name ($Case.Id + ' JSON records the exact registry and SQL targets') -Condition (
        $registryTarget -and $Output.Leaves -ccontains $script:SqlTarget)
    Assert-True -Name ($Case.Id + ' JSON records UTC observation times of this run') `
        -Condition ($Output.UtcInRun -gt 0) -Detail ([string]$Output.UtcInRun)
    $installSeen = @($Output.Leaves | Where-Object { $_ -ceq $script:InstallVersion }).Count
    $engineSeen = @($Output.Leaves | Where-Object { $_ -ceq $script:PatchedVersion }).Count
    Assert-True -Name ($Case.Id + ' JSON keeps Setup/Version as metadata and both engine observations') -Condition (
        $installSeen -ge 1 -and $engineSeen -ge 2) -Detail ('install={0} engine={1}' -f $installSeen, $engineSeen)
    Assert-VersionAdminConnection -Id $Case.Id -Run $Run
    $connections = @(Get-VersionEntries -Run $Run -Kind 'SqlConnection')
    $connectionId = if ($connections.Count -eq 1) { $connections[0].Id } else { $null }
    $checked = @(Get-VersionCheckedKeys -Run $Run -ConnectionId $connectionId)
    $missing = @($script:SameConnectionKeys | Where-Object { $_ -cnotin $checked })
    Assert-True -Name ($Case.Id + ' transport, machine, instance, sysadmin and version are read on that connection') `
        -Condition ($null -ne $connectionId -and $missing.Count -eq 0) -Detail ('missing=' + ($missing -join ','))
}

function Assert-VersionCardRefused {
    param($Case, $Run, $Output)
    $probeRefused = $Run.Exit -ne 0 -and $Run.Exit -lt $script:ChildGuardFailed
    Assert-True -Name ($Case.Id + ' probe exits nonzero: ' + $Case.Title + ' (control ' + $Case.Control + ')') `
        -Condition $probeRefused -Detail ('exit=' + $Run.Exit)
    $jsonRefused = $Output.NewFiles.Count -eq 1 -and [string]$Output.Status -cne 'Matched' -and
    $null -ne $Output.ExitCode -and $Output.ExitCode -ne 0 -and $Output.ExitCode -eq $Run.Exit
    $detail = 'files={0} status={1} exit={2}' -f $Output.NewFiles.Count, $Output.Status, $Output.ExitCode
    Assert-True -Name ($Case.Id + ' new JSON records a failed status with the same nonzero exit') `
        -Condition $jsonRefused -Detail $detail
    if ($Case.Cause -ceq 'PatchLevel') {
        Assert-True -Name ($Case.Id + ' refusal follows a read of Setup/PatchLevel (no Version fallback)') `
            -Condition (Test-VersionPatchLevelConsulted -Run $Run)
    }
    foreach ($value in $Case.Recorded) {
        Assert-True -Name ($Case.Id + ' JSON records the observed value ' + $value) `
            -Condition ($Output.Leaves -ccontains $value)
    }
}

# =====================================================================================================
# 1. Run gates: fixture identity, shell, card output location and input fingerprints.
# =====================================================================================================
$hostName = [Environment]::MachineName
Assert-True -Name 'VER-ENV-01 fixture machine/instance never name this host or a real instance' -Condition (
    $hostName -cne $script:FixtureMachine -and $script:FixtureInstance -cnotin @('SQLEXPRESS', 'MSSQLSERVER')) `
    -Detail ('host=' + $hostName)
Assert-True -Name 'VER-ENV-02 suite runs under pwsh 7 on Windows' -Condition (
    $IsWindows -and $PSVersionTable.PSVersion.Major -eq 7) -Detail $PSVersionTable.PSVersion.ToString()
# The card refuses an output folder below a junction or symlink, so the suite folder must have none.
$reparseAncestors = @(Get-VersionReparseAncestors -Path $script:SuiteRoot)
Assert-True -Name 'VER-ENV-03 suite folder has no junction/symlink ancestor' `
    -Condition ($reparseAncestors.Count -eq 0) -Detail ($reparseAncestors -join ',')
$cardHashBefore = Get-VersionFileSha256 -Path $script:CardPath
Add-TestResult -Name 'VER-ENV-04 card SHA256' -Outcome OBSERVED -Detail ($script:CardPath + ' ' + $cardHashBefore)
foreach ($name in @('WindowsSql.Instance.ps1', 'WindowsSql.Sql.ps1')) {
    Add-TestResult -Name ('VER-ENV-05 product SHA256 ' + $name) -Outcome OBSERVED `
        -Detail (Get-VersionFileSha256 -Path (Join-Path $script:ProductRoot $name))
}
# Without stand-ins (this suite process) the guard must flag the real types, or a child guard would prove nothing.
$guardSampleText = '[Microsoft.Win32.RegistryKey]::OpenBaseKey(1, 2); [Data.SqlClient.SqlConnection]::new("x")'
$guardSample = [Management.Automation.Language.Parser]::ParseInput($guardSampleText, [ref]$null, [ref]$null)
$sampleGuard = Test-VersionTypeReferences -Roots @($guardSample) -Protected $script:ProtectedTypeNames
Assert-True -Name 'VER-ENV-06 type guard flags real registry and SQL connection bindings' -Condition (
    -not $sampleGuard.Passed -and @($sampleGuard.Violations).Count -eq 2) -Detail (@($sampleGuard.Violations) -join ',')

# =====================================================================================================
# 2. Product: the real identity read and Preflight core with only registry/SQL/host facts stood in.
# =====================================================================================================
$install = $script:InstallVersion
$patched = $script:PatchedVersion
$productCases = @(
    (New-VersionCase -Id 'VER-PRD-01' -Title 'Setup/Version 17.0.1000.7, PatchLevel/approved/SQL 17.0.1145.4' `
        -Expect Pass -SetupVersion $install -PatchLevel $patched -Approved $patched -SqlVersion $patched -Detailed),
    (New-VersionCase -Id 'VER-PRD-02' -Title 'unpatched: Version/PatchLevel/approved/SQL all 17.0.1000.7' `
        -Expect Pass -SetupVersion $install -PatchLevel $install -Approved $install -SqlVersion $install),
    # The control already reaches SQL with the current product, so its preservation checks run there too.
    (New-VersionCase -Id 'VER-PRD-03' -Title 'control: Version/PatchLevel/approved/SQL all 17.0.1145.4' `
        -Expect Pass -SetupVersion $patched -PatchLevel $patched -Approved $patched -SqlVersion $patched -Detailed),
    (New-VersionCase -Id 'VER-PRD-04' -Title 'Preflight with the patched instance of VER-PRD-01' -Target Preflight `
        -Expect Pass -SetupVersion $install -PatchLevel $patched -Approved $patched -SqlVersion $patched),
    (New-VersionCase -Id 'VER-PRD-05' -Title 'Preflight with the unpatched instance of VER-PRD-02' -Target Preflight `
        -Expect Pass -SetupVersion $install -PatchLevel $install -Approved $install -SqlVersion $install),
    (New-VersionCase -Id 'VER-PRD-06' -Title 'PatchLevel absent while Version equals the approved value' `
        -Expect Block -SetupVersion $patched -PatchLevel $null -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-07' -Title 'PatchLevel empty' -Expect Block -SetupVersion $patched -PatchLevel '' `
        -Approved $patched -SqlVersion $patched -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-08' -Title 'PatchLevel with three parts 17.0.1145' -Expect Block `
        -SetupVersion $patched -PatchLevel '17.0.1145' -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-09' -Title 'PatchLevel with a trailing space (no Trim)' -Expect Block `
        -SetupVersion $patched -PatchLevel ($patched + ' ') -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-10' -Title 'PatchLevel 17.0.01145.4 equal only as numbers' -Expect Block `
        -SetupVersion $patched -PatchLevel '17.0.01145.4' -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-11' -Title 'PatchLevel stored as REG_DWORD' -Expect Block -SetupVersion $patched `
        -PatchLevel 17 -PatchLevelKind DWord -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-12' -Title 'PatchLevel 17.0.1145.3 while Version equals the approved value' `
        -Expect Block -SetupVersion $patched -PatchLevel '17.0.1145.3' -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-13' -Title 'Version 17.0.1000.7, PatchLevel 17.0.1145.3, approved/SQL 17.0.1145.4' `
        -Expect Block -SetupVersion $install -PatchLevel '17.0.1145.3' -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-01),
    (New-VersionCase -Id 'VER-PRD-14' -Title 'Preflight with PatchLevel absent (VER-PRD-06)' -Target Preflight `
        -Expect Block -SetupVersion $patched -PatchLevel $null -Approved $patched -SqlVersion $patched `
        -Cause PatchLevel -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-15' -Title 'SQL ProductVersion 17.0.1145.5 differs from the approved value' `
        -Expect Block -SetupVersion $patched -PatchLevel $patched -Approved $patched -SqlVersion '17.0.1145.5' `
        -Cause 'Server:SERVERPROPERTY:productversion' -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-16' -Title 'connection transport is TCP' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -Approved $patched -SqlVersion $patched -Sql @{ NetTransport = 'TCP' } `
        -Cause 'Server:CONNECTIONPROPERTY:net_transport' -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-17' -Title 'login is not sysadmin' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -Approved $patched -SqlVersion $patched -Sql @{ IsSysadmin = 0 } `
        -Cause 'Server:IS_SRVROLEMEMBER:sysadmin' -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-18' -Title 'SQL reports another machine' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -Approved $patched -SqlVersion $patched -Sql @{ MachineName = 'OTHER-FIXTURE' } `
        -Cause 'Server:SERVERPROPERTY:machinename' -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-19' -Title 'SQL reports another instance' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -Approved $patched -SqlVersion $patched -Sql @{ InstanceName = 'OTHERFIX' } `
        -Cause 'Server:SERVERPROPERTY:instancename' -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-20' -Title 'LPC open fails' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -Approved $patched -SqlVersion $patched -Sql @{ OpenFails = $true } `
        -Cause Open -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-21' -Title 'instance map names another InstanceId' -Expect Block `
        -SetupVersion $patched -PatchLevel $patched -Approved $patched -SqlVersion $patched `
        -MappedInstanceId 'MSSQL16.DHFIXTURE' -Cause Map -Control VER-PRD-03),
    (New-VersionCase -Id 'VER-PRD-22' -Title 'instance map differs only in case' -Expect Block `
        -SetupVersion $patched -PatchLevel $patched -Approved $patched -SqlVersion $patched `
        -MappedInstanceId 'mssql17.dhfixture' -Cause Map -Control VER-PRD-03)
)
foreach ($case in $productCases) {
    $scenario = [ordered]@{
        Kind = 'Product'
        Target = $case.Target
        ProductRoot = $script:ProductRoot
        ExpectedProductVersion = $case.Approved
        Registry = $case.Registry
        Sql = $case.Sql
    }
    $run = Invoke-VersionChild -Id $case.Id -Scenario $scenario
    Assert-VersionHarnessBoundary -Id $case.Id -Run $run -AllowedExit @(0)
    if ($case.Expect -ceq 'Pass') { Assert-VersionIdentityAccepted -Case $case -Run $run }
    else { Assert-VersionIdentityRefused -Case $case -Run $run }
}

# =====================================================================================================
# 3. Card: the probe text of the given card runs unchanged with registry/SQL/host facts stood in.
# =====================================================================================================
$cardProbe = Get-VersionCardProbe -Path $script:CardPath
$probeInputs = @()
if ($null -ne $cardProbe) {
    $probeInputs = @($cardProbe.ScriptBlock.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
}
# The card observes versions before the plan exists, so it must not take an approved version.
Assert-Equal -Name 'VER-CARD-00 card probe takes exactly the four approved inputs and no approved version' `
    -Expected 'InstanceId|InstanceName|MachineName|OutputDirectory' -Actual ((@($probeInputs | Sort-Object)) -join '|')

$cardCases = @(
    (New-VersionCase -Id 'VER-CARD-01' -Title 'Setup/Version 17.0.1000.7, PatchLevel/SQL 17.0.1145.4' -Expect Pass `
        -SetupVersion $install -PatchLevel $patched -SqlVersion $patched -Detailed),
    (New-VersionCase -Id 'VER-CARD-02' -Title 'unpatched: Version/PatchLevel/SQL all 17.0.1000.7' -Expect Pass `
        -SetupVersion $install -PatchLevel $install -SqlVersion $install),
    (New-VersionCase -Id 'VER-CARD-03' -Title 'control: Version/PatchLevel/SQL all 17.0.1145.4' -Expect Pass `
        -SetupVersion $patched -PatchLevel $patched -SqlVersion $patched),
    (New-VersionCase -Id 'VER-CARD-04' -Title 'PatchLevel absent while Version equals SQL; an old Matched file exists' `
        -Expect Block -SetupVersion $patched -PatchLevel $null -SqlVersion $patched -Cause PatchLevel `
        -Control VER-CARD-03 -Stale),
    (New-VersionCase -Id 'VER-CARD-05' -Title 'PatchLevel empty' -Expect Block -SetupVersion $patched -PatchLevel '' `
        -SqlVersion $patched -Cause PatchLevel -Control VER-CARD-03),
    (New-VersionCase -Id 'VER-CARD-06' -Title 'PatchLevel with three parts 17.0.1145' -Expect Block `
        -SetupVersion $patched -PatchLevel '17.0.1145' -SqlVersion $patched -Cause PatchLevel -Control VER-CARD-03),
    (New-VersionCase -Id 'VER-CARD-07' -Title 'PatchLevel with a trailing space (no Trim)' -Expect Block `
        -SetupVersion $patched -PatchLevel ($patched + ' ') -SqlVersion $patched -Cause PatchLevel `
        -Control VER-CARD-03),
    (New-VersionCase -Id 'VER-CARD-08' -Title 'PatchLevel 17.0.01145.4 equal only as numbers' -Expect Block `
        -SetupVersion $patched -PatchLevel '17.0.01145.4' -SqlVersion $patched -Cause PatchLevel `
        -Control VER-CARD-03),
    (New-VersionCase -Id 'VER-CARD-09' -Title 'PatchLevel stored as REG_DWORD' -Expect Block -SetupVersion $patched `
        -PatchLevel 17 -PatchLevelKind DWord -SqlVersion $patched -Cause PatchLevel -Control VER-CARD-03),
    (New-VersionCase -Id 'VER-CARD-10' -Title 'PatchLevel 17.0.1145.3 while Version equals SQL 17.0.1145.4' `
        -Expect Block -SetupVersion $patched -PatchLevel '17.0.1145.3' -SqlVersion $patched -Cause PatchLevel `
        -Control VER-CARD-03 -Recorded @('17.0.1145.3', $patched)),
    (New-VersionCase -Id 'VER-CARD-11' -Title 'SQL ProductVersion 17.0.1145.5 differs from PatchLevel' -Expect Block `
        -SetupVersion $patched -PatchLevel $patched -SqlVersion '17.0.1145.5' -Control VER-CARD-03 `
        -Recorded @('17.0.1145.5')),
    (New-VersionCase -Id 'VER-CARD-12' -Title 'connection transport is TCP' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -SqlVersion $patched -Sql @{ NetTransport = 'TCP' } -Control VER-CARD-03 `
        -Recorded @('TCP')),
    (New-VersionCase -Id 'VER-CARD-13' -Title 'login is not sysadmin' -Expect Block -SetupVersion $patched `
        -PatchLevel $patched -SqlVersion $patched -Sql @{ IsSysadmin = 0 } -Control VER-CARD-03)
)
foreach ($case in $cardCases) {
    $output = Join-Path $script:SuiteRoot ('card-output\' + $case.Id)
    [void][IO.Directory]::CreateDirectory($output)
    $existing = @()
    $staleHash = $null
    if ($case.Stale) {
        # A Matched result of an earlier run must neither be overwritten nor count for this run.
        $stalePath = Join-Path $output ('pr2-version-check-20000101T000000000000Z-' + ('0' * 32) + '.json')
        $staleText = '{"Status":"Matched","ExitCode":0}' + "`n"
        [IO.File]::WriteAllText($stalePath, $staleText, [Text.UTF8Encoding]::new($false))
        $existing = @($stalePath)
        $staleHash = Get-VersionFileSha256 -Path $stalePath
    }
    $scenario = [ordered]@{
        Kind = 'Card'
        CardPath = $script:CardPath
        OutputDirectory = $output
        Registry = $case.Registry
        Sql = $case.Sql
    }
    $run = Invoke-VersionChild -Id $case.Id -Scenario $scenario
    Assert-VersionHarnessBoundary -Id $case.Id -Run $run -AllowedExit @(0, 1, 2)
    $result = Read-VersionCardOutput -Directory $output -Existing $existing -Run $run
    if ($case.Expect -ceq 'Pass') { Assert-VersionCardMatched -Case $case -Run $run -Output $result }
    else { Assert-VersionCardRefused -Case $case -Run $run -Output $result }
    if ($case.Stale) {
        Assert-Equal -Name ($case.Id + ' the old Matched file is untouched and not counted') -Expected $staleHash `
            -Actual (Get-VersionFileSha256 -Path $existing[0])
    }
}

Assert-Equal -Name 'VER-CARD-90 card bytes are unchanged by the suite' -Expected $cardHashBefore `
    -Actual (Get-VersionFileSha256 -Path $script:CardPath)
Add-TestResult -Name 'VER-CARD-91 card wrapper (child pwsh launch, EXIT line) not run: it starts an unguarded child' `
    -Outcome OBSERVED
Complete-TestSuite
