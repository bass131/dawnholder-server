Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Shared offline support for database tool tests.
# Tests never open SQL: product connection/identity/ACL entry functions are replaced by throwing stubs,
# and deployment paths run against the narrow ADO.NET fake below.

$script:ToolRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$script:TestResults = New-Object 'Collections.Generic.List[object]'

function Initialize-TestSuite {
    param(
        [Parameter(Mandatory)][string]$WorkRoot,
        [Parameter(Mandatory)][string]$Suite
    )
    # Fixtures live only below the explicit work root; there is no TEMP or repository fallback.
    if (-not [IO.Path]::IsPathRooted($WorkRoot)) {
        throw 'WorkRoot must be an explicit absolute path.'
    }
    $root = [IO.Path]::GetFullPath($WorkRoot)
    if (-not [IO.Directory]::Exists($root)) {
        throw "WorkRoot does not exist: $root"
    }
    $suiteRoot = Join-Path $root $Suite
    if ([IO.Directory]::Exists($suiteRoot)) {
        throw "Suite directory already exists; use a new run directory instead of deleting: $suiteRoot"
    }
    [void][IO.Directory]::CreateDirectory($suiteRoot)
    $script:SuiteName = $Suite
    $script:SuiteRoot = $suiteRoot
    return $suiteRoot
}

function Add-TestResult {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][ValidateSet('PASS', 'FAIL', 'OBSERVED')][string]$Outcome,
        [string]$Detail = ''
    )
    $script:TestResults.Add([pscustomobject]@{
            Suite = $script:SuiteName
            Name = $Name
            Outcome = $Outcome
            Detail = $Detail
        })
    Write-Output ('{0}: {1}{2}' -f $Outcome, $Name, $(if ($Detail) { ' -- ' + $Detail } else { '' }))
}

function Assert-True {
    param(
        [bool]$Condition,
        [Parameter(Mandatory)][string]$Name,
        [string]$Detail = ''
    )
    Add-TestResult -Name $Name -Outcome $(if ($Condition) { 'PASS' } else { 'FAIL' }) -Detail $Detail
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [Parameter(Mandatory)][string]$Name
    )
    $passed = [string]$Expected -ceq [string]$Actual
    Add-TestResult -Name $Name -Outcome $(if ($passed) { 'PASS' } else { 'FAIL' }) `
        -Detail ('expected <{0}> actual <{1}>' -f $Expected, $Actual)
}

function Get-ThrownMessage {
    param([Parameter(Mandatory)][scriptblock]$Action)
    try {
        $null = & $Action
    }
    catch {
        return $_.Exception.Message
    }
    return $null
}

function Assert-Throws {
    param(
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Name
    )
    # The exact cause is asserted so that an earlier unrelated failure cannot pass a counterexample.
    $message = Get-ThrownMessage -Action $Action
    if ($null -eq $message) {
        Add-TestResult -Name $Name -Outcome FAIL -Detail 'no exception'
    }
    elseif ($message -cmatch $Pattern) {
        Add-TestResult -Name $Name -Outcome PASS -Detail $message
    }
    else {
        Add-TestResult -Name $Name -Outcome FAIL -Detail ('unexpected cause: ' + $message)
    }
}

function Assert-NoThrow {
    param(
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][string]$Name
    )
    $message = Get-ThrownMessage -Action $Action
    Add-TestResult -Name $Name -Outcome $(if ($null -eq $message) { 'PASS' } else { 'FAIL' }) -Detail ([string]$message)
}

function Complete-TestSuite {
    $path = Join-Path $script:SuiteRoot 'results.json'
    $json = ConvertTo-Json -InputObject @($script:TestResults.ToArray()) -Depth 6
    [IO.File]::WriteAllText($path, $json + "`n", (New-Object Text.UTF8Encoding($false)))
    $failed = @($script:TestResults | Where-Object Outcome -CEQ 'FAIL').Count
    $passed = @($script:TestResults | Where-Object Outcome -CEQ 'PASS').Count
    $observed = @($script:TestResults | Where-Object Outcome -CEQ 'OBSERVED').Count
    Write-Output ('SUMMARY {0}: pass={1} fail={2} observed={3}' -f $script:SuiteName, $passed, $failed, $observed)
    if ($failed -gt 0) { exit 1 }
    exit 0
}

function Get-Sha256Hex {
    param([Parameter(Mandatory)][byte[]]$Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Get-ExpectedModuleValues {
    param([Parameter(Mandatory)][string]$Path)
    # Independent recomputation from the source file: UTF-8 source identity and UTF-16LE engine expectation.
    $bytes = [IO.File]::ReadAllBytes($Path)
    $offset = 0
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { $offset = 3 }
    $text = (New-Object Text.UTF8Encoding($false, $true)).GetString($bytes, $offset, $bytes.Length - $offset)
    $text = $text.Replace("`r`n", "`n")
    $utf16 = (New-Object Text.UnicodeEncoding($false, $false)).GetBytes($text)
    return [pscustomobject]@{
        SourceChecksum = Get-Sha256Hex -Bytes ((New-Object Text.UTF8Encoding($false)).GetBytes($text))
        DefinitionBytes = $utf16.Length
        DefinitionChecksum = Get-Sha256Hex -Bytes $utf16
    }
}

function New-DatabaseFixture {
    param(
        [Parameter(Mandatory)][string]$Name,
        [switch]$WithoutMigrations
    )
    # Byte-exact copy of the reviewed module and migration sources; product files stay read-only.
    $root = Join-Path $script:SuiteRoot $Name
    [void][IO.Directory]::CreateDirectory($root)
    Copy-Item -LiteralPath (Join-Path $script:ToolRoot 'modules') -Destination $root -Recurse
    if (-not $WithoutMigrations) {
        Copy-Item -LiteralPath (Join-Path $script:ToolRoot 'migrations') -Destination $root -Recurse
    }
    return $root
}

function Read-FixtureText {
    param([Parameter(Mandatory)][string]$Path)
    return [IO.File]::ReadAllText($Path)
}

function Write-FixtureText {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Text
    )
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding($false)))
}

function Edit-FixtureText {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Find,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Replace
    )
    $text = Read-FixtureText -Path $Path
    if (-not $text.Contains($Find)) {
        throw "Fixture edit target not found in ${Path}: $Find"
    }
    Write-FixtureText -Path $Path -Text $text.Replace($Find, $Replace)
}

function Read-FixtureManifest {
    param([Parameter(Mandatory)][string]$Root)
    return (Read-FixtureText -Path (Join-Path $Root 'modules/manifest.json')) | ConvertFrom-Json
}

function Write-FixtureManifest {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)]$Manifest
    )
    # Canonical reviewed manifest bytes: UTF-8 without BOM, LF, final newline.
    $json = ($Manifest | ConvertTo-Json -Depth 10).Replace("`r`n", "`n") + "`n"
    Write-FixtureText -Path (Join-Path $Root 'modules/manifest.json') -Text $json
}

function Update-FixtureManifestEntry {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$EntryPath
    )
    # Re-derive one entry from its edited source so the counterexample reaches the intended check.
    $manifest = Read-FixtureManifest -Root $Root
    $entry = @($manifest.Entries | Where-Object Path -CEQ $EntryPath)[0]
    $values = Get-ExpectedModuleValues -Path (Join-Path $Root $EntryPath)
    $entry.SourceChecksum = $values.SourceChecksum
    if ($null -ne $entry.ObjectName) {
        $entry.DefinitionBytes = $values.DefinitionBytes
        $entry.DefinitionChecksum = $values.DefinitionChecksum
    }
    Write-FixtureManifest -Root $Root -Manifest $manifest
}

function Set-OfflineStubs {
    # Call after the test script dot-sources the product definitions into its own script scope.
    # Connection/identity/ACL/secret entry points are replaced there by fail-closed stubs.
    foreach ($name in @(
            'Open-LocalDatabase',
            'Open-TestEnvironmentDatabase',
            'Invoke-DatabaseSql',
            'Assert-TestEnvironmentExecutor',
            'Assert-TestEnvironmentLocalAccountAbsent',
            'Lock-TestEnvironmentManifest',
            'Set-TestEnvironmentDirectoryAcl',
            'New-TestEnvironmentOwnedFile',
            'New-TestEnvironmentChildIdentity'
        )) {
        $stub = [scriptblock]::Create("throw 'Offline test stub: $name must not run.'")
        Set-Item -Path ('Function:script:' + $name) -Value $stub
    }
}

function New-FakeSqlConnection {
    param(
        [Parameter(Mandatory)][scriptblock]$Responder,
        [string]$Database = 'Dawnholder_Dev_Fixture'
    )
    # Narrow ADO.NET boundary used by New-DbCommand/Invoke-Db*: records every command and transaction event.
    # It does not emulate SQL semantics, provider conversions, error numbers or engine atomicity.
    $connection = [pscustomobject]@{
        Database = $Database
        Responder = $Responder
        Log = New-Object 'Collections.Generic.List[object]'
    }
    $connection | Add-Member -MemberType ScriptMethod -Name CreateCommand -Value {
        $owner = $this
        $parameters = [pscustomobject]@{ Items = @{} }
        $parameters | Add-Member -MemberType ScriptMethod -Name Add -Value {
            param($name, $type)
            $parameter = [pscustomobject]@{ Name = $name; SqlDbType = $type; Size = 0; Value = $null }
            $this.Items[$name] = $parameter
            return $parameter
        }
        $command = [pscustomobject]@{
            Connection = $owner
            CommandText = ''
            CommandTimeout = 0
            Transaction = $null
            Parameters = $parameters
        }
        $command | Add-Member -MemberType ScriptMethod -Name Invoke -Value {
            param([string]$Mode)
            $values = @{}
            foreach ($key in $this.Parameters.Items.Keys) { $values[$key.TrimStart('@')] = $this.Parameters.Items[$key].Value }
            $entry = [pscustomobject]@{
                Mode = $Mode
                Sql = $this.CommandText
                Parameters = $values
                InTransaction = $null -ne $this.Transaction
            }
            $this.Connection.Log.Add($entry)
            return (& $this.Connection.Responder $Mode $this.CommandText $values)
        }
        $command | Add-Member -MemberType ScriptMethod -Name ExecuteScalar -Value { return $this.Invoke('Scalar') }
        $command | Add-Member -MemberType ScriptMethod -Name ExecuteNonQuery -Value { return $this.Invoke('NonQuery') }
        $command | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
        return $command
    }
    $connection | Add-Member -MemberType ScriptMethod -Name BeginTransaction -Value {
        $owner = $this
        $owner.Log.Add([pscustomobject]@{ Mode = 'Begin'; Sql = ''; Parameters = @{}; InTransaction = $true })
        $transaction = [pscustomobject]@{ Connection = $owner; Owner = $owner }
        $transaction | Add-Member -MemberType ScriptMethod -Name Commit -Value {
            $this.Owner.Log.Add([pscustomobject]@{ Mode = 'Commit'; Sql = ''; Parameters = @{}; InTransaction = $true })
            $this.Connection = $null
        }
        $transaction | Add-Member -MemberType ScriptMethod -Name Rollback -Value {
            $this.Owner.Log.Add([pscustomobject]@{ Mode = 'Rollback'; Sql = ''; Parameters = @{}; InTransaction = $true })
            $this.Connection = $null
        }
        $transaction | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
            $this.Owner.Log.Add([pscustomobject]@{ Mode = 'Dispose'; Sql = ''; Parameters = @{}; InTransaction = $false })
        }
        return $transaction
    }
    return $connection
}

function Invoke-PowerShellFile {
    param(
        [Parameter(Mandatory)][string]$File,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory
    )
    # Run a product entry point in a separate Windows PowerShell process and capture all output and exit.
    $exe = [Diagnostics.Process]::GetCurrentProcess().Path
    $quoted = @('-NoLogo', '-NoProfile', '-File', ('"' + $File + '"')) + @($Arguments | ForEach-Object {
            if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
        })
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $exe
    $info.Arguments = $quoted -join ' '
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    if ($WorkingDirectory) { $info.WorkingDirectory = $WorkingDirectory }
    $process = [Diagnostics.Process]::Start($info)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut = $stdout.Result
        StdErr = $stderr.Result
    }
}
