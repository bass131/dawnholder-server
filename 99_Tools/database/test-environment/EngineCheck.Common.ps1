# Shared evidence, preservation and SQL observation boundaries for the two engine check entry points.
. (Join-Path $PSScriptRoot 'Environment.Common.ps1')
. (Join-Path $PSScriptRoot '../Database.Common.ps1')
$script:EngineProductScalar = (Get-Command Invoke-DbScalar -CommandType Function).ScriptBlock
$script:EngineProductNonQuery = (Get-Command Invoke-DbNonQuery -CommandType Function).ScriptBlock
$script:EngineProductDatabaseSql = (Get-Command Invoke-DatabaseSql -CommandType Function).ScriptBlock

function Stop-EngineCheck([string]$Message) {
    $failure = [InvalidOperationException]::new($Message)
    $failure.Data['EngineCheckMessage'] = $Message
    throw $failure
}

function Get-EngineCheckFailure($Exception) {
    $current = $Exception
    while ($null -ne $current) {
        if ($current.Data.Contains('EngineCheckMessage')) {
            return [pscustomobject]@{
                Message = [string]$current.Data['EngineCheckMessage']
                Code = Get-DatabaseFailureCode -Exception $Exception
            }
        }
        $current = $current.InnerException
    }
    return [pscustomobject]@{
        Message = Get-TestEnvironmentFailureSummary -Exception $Exception
        Code = Get-DatabaseFailureCode -Exception $Exception
    }
}

function Assert-EngineEvidencePath([string]$Directory, [string]$Path, [switch]$Existing) {
    if (-not [IO.Path]::IsPathRooted($Directory) -or -not [IO.Directory]::Exists($Directory) -or
        -not [IO.Path]::IsPathRooted($Path)) {
        Stop-EngineCheck -Message 'Evidence needs an existing absolute directory and absolute file paths.'
    }
    $root = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or
        -not [IO.Directory]::Exists([IO.Path]::GetDirectoryName($full))) {
        Stop-EngineCheck -Message 'Evidence file must be below the explicit evidence directory.'
    }
    Assert-TestEnvironmentNoReparse -Path $full
    if ($Existing) {
        if (-not [IO.File]::Exists($full)) {
            Stop-EngineCheck -Message 'The recorded preservation snapshot is required.'
        }
    } elseif (Test-Path -LiteralPath $full) {
        Stop-EngineCheck -Message 'Evidence already exists; overwriting or automatic retry is forbidden.'
    }
    return $full
}

function Write-EngineJson($Stream, $Value) {
    $json = (ConvertTo-Json -InputObject $Value -Depth 30) + "`n"
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($json)
    $Stream.Write($bytes, 0, $bytes.Length)
    $Stream.Flush()
}

function Write-NewEngineJson([string]$Path, $Value) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        Write-EngineJson -Stream $stream -Value $Value
    } finally {
        $stream.Dispose()
    }
}

function Get-EngineCodeIdentity([string]$DatabaseRoot) {
    $bundle = Read-ModuleBundle -DatabaseRoot $DatabaseRoot
    $files = @(Get-ChildItem -LiteralPath $DatabaseRoot -File -Filter '*.ps1') +
    @(Get-ChildItem -LiteralPath (Join-Path $DatabaseRoot 'test-environment') -File -Filter '*.ps1') +
    @(Get-ChildItem -LiteralPath (Join-Path $DatabaseRoot 'migrations') -File -Filter '*.sql') +
    @(Get-Item -LiteralPath (Join-Path $DatabaseRoot 'verify-schema.sql'))
    foreach ($relativePath in @($bundle.Modules.Path) + @($bundle.Permissions.Path, 'modules/manifest.json')) {
        $files += Get-Item -LiteralPath (Join-Path $DatabaseRoot $relativePath)
    }
    $hashes = @(foreach ($file in ($files | Sort-Object FullName)) {
            [pscustomobject]@{
                File = $file.FullName.Substring($DatabaseRoot.Length).TrimStart('\', '/').Replace('\', '/')
                SHA256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            }
        })
    return [pscustomobject]@{ Files = $hashes; BundleManifestChecksum = $bundle.ManifestChecksum }
}

function Open-EngineObservedConnection($Context, [switch]$Master) {
    # Observe the existing helper's identity row, without another query or a second identity policy.
    function Invoke-DatabaseSql(
        $Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null, [string]$Result = 'Scalar'
    ) {
        $value = & $script:EngineProductDatabaseSql @PSBoundParameters
        if ($Result -ceq 'Rows' -and $Sql.Contains("SERVERPROPERTY('MachineName')")) {
            $row = $value.Rows[0]
            $Context.Report.EngineIdentity = [ordered]@{
                MachineName = [string]$row.Machine
                ProductVersion = [string]$row.ProductVersion
                Collation = [string]$row.ServerCollation
                host_platform = [string]$row.HostPlatform
                MasterFamilyGuid = [string]$row.MasterFamilyGuid
                Database = $Context.Manifest.Database
                DatabaseIdentity = $Context.Manifest.DatabaseIdentity
                Source = 'Identity row observed inside Open-TestEnvironmentDatabase'
            }
        }
        if ($value -is [Data.DataTable]) { return , $value }
        return $value
    }
    return (Open-TestEnvironmentDatabase -Contract $Context.Contract -Manifest $Context.Manifest `
            -Database $Context.Manifest.Database -Master:$Master)
}

function Invoke-EngineCheck {
    param(
        $Request,
        [string]$Tool,
        [string]$RequiredState,
        [scriptblock]$Work
    )
    $resultPath = Assert-EngineEvidencePath -Directory $Request.EvidenceDirectory -Path $Request.ResultPath
    if ($resultPath -ieq [IO.Path]::GetFullPath($Request.SnapshotPath)) {
        Stop-EngineCheck -Message 'Result and snapshot must be different files.'
    }
    $stream = [IO.File]::Open($resultPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $report = [ordered]@{
        Tool = $Tool
        Action = $Request.Action
        StartedUtc = [DateTime]::UtcNow.ToString('o')
        FinishedUtc = $null
        StateBefore = $null
        StateAfter = $null
        EngineIdentity = $null
        CodeIdentity = $null
        Outcome = 'FAIL'
        Committed = $false
        Failure = $null
        Preservation = $null
        ScalarCalls = [Collections.Generic.List[object]]::new()
        Observations = [Collections.Generic.List[object]]::new()
        Rejections = [Collections.Generic.List[object]]::new()
        Atomicity = [Collections.Generic.List[object]]::new()
        StateComparisons = [Collections.Generic.List[object]]::new()
    }
    $guard = $null
    $master = $null
    $connection = $null
    $manifest = $null
    try {
        $contract = Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $Request.ApprovalPlanPath `
            -ExpectedApprovalPlanHash $Request.ExpectedApprovalPlanHash
        Assert-TestEnvironmentTarget -Contract $contract -Database $Request.Database
        Assert-TestEnvironmentExecutionApproval -Contract $contract
        $recording = $Tool -ceq 'Invoke-PreservationCheck' -and $Request.Action -ceq 'Record'
        $snapshotPath = Assert-EngineEvidencePath -Directory $Request.EvidenceDirectory `
            -Path $Request.SnapshotPath -Existing:(-not $recording)
        $guard = Lock-TestEnvironmentManifest -Contract $contract -Database $Request.Database `
            -ManifestPath $Request.ManifestPath
        $manifest = Read-TestEnvironmentManifest -Contract $contract -Database $Request.Database `
            -ManifestPath $Request.ManifestPath
        $report.StateBefore = $manifest.State
        Assert-TestEnvironmentReadyForStep -Manifest $manifest
        if ($manifest.State -cne $RequiredState) {
            Stop-EngineCheck -Message "This action requires manifest State $RequiredState."
        }
        $databaseRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
        $report.CodeIdentity = Get-EngineCodeIdentity -DatabaseRoot $databaseRoot
        $context = [pscustomobject]@{
            Contract = $contract
            Manifest = $manifest
            Connection = $null
            DatabaseRoot = $databaseRoot
            SnapshotPath = $snapshotPath
            Report = $report
            Sites = @()
        }
        # Resolve every source boundary before connecting or allowing a mutating observation.
        if ($Tool -ceq 'Test-EngineBoundary') {
            $context.Sites = @(Get-EngineJsonSites -DatabaseRoot $databaseRoot)
        }
        $master = Open-EngineObservedConnection -Context $context -Master
        Assert-TestEnvironmentDatabaseIdentity -Master $master -Manifest $manifest
        $connection = Open-EngineObservedConnection -Context $context
        Assert-TestEnvironmentMarkers -Connection $connection -Manifest $manifest
        $context.Connection = $connection
        $observer = New-EngineObserver -Context $context -Scenario $Request.Action
        Invoke-WithEngineObserver -Observer $observer -Action {
            Assert-DatabaseOwner -Connection $connection
            & $Work $context
        }
        if (@($report.Rejections | Where-Object Outcome -CEQ 'UNOBSERVED').Count) {
            Stop-EngineCheck -Message 'Required stimuli were unobserved; an engine verdict remains incomplete.'
        }
        $report.Outcome = 'PASS'
    } catch {
        $report.Failure = Get-EngineCheckFailure -Exception $_.Exception
        $report.Failure | Add-Member -MemberType NoteProperty -Name ScriptStack -Value $_.ScriptStackTrace
    } finally {
        if ($null -ne $manifest) { $report.StateAfter = $manifest.State }
        if ($null -ne $connection) { $connection.Dispose() }
        if ($null -ne $master) { $master.Dispose() }
        if ($null -ne $guard) { $guard.Dispose() }
        $report.FinishedUtc = [DateTime]::UtcNow.ToString('o')
        try {
            Write-EngineJson -Stream $stream -Value $report
        } finally {
            $stream.Dispose()
        }
    }
    if ($report.Outcome -cne 'PASS') {
        Stop-EngineCheck -Message 'Engine check failed; preserve evidence and resources. No automatic retry or cleanup.'
    }
    Write-Output "PASS: $resultPath"
}

function Get-PreservationSnapshot($Connection, $Transaction = $null) {
    # SQL style 3 is lossless for real/float. Dates and rowversions are text to avoid JSON/parser coercion.
    $queries = [ordered]@{
        Account = @'
SELECT ISNULL((SELECT AccountId, CONVERT(varchar(33), CreatedUtc, 126) AS CreatedUtc
FROM dh.Account ORDER BY AccountId FOR JSON PATH), N'[]');
'@
        Character = @'
SELECT ISNULL((SELECT CharacterId, AccountId, Class,
    CONVERT(varchar(33), CreatedUtc, 126) AS CreatedUtc, CONVERT(varchar(16), Version, 2) AS Version
FROM dh.Character ORDER BY CharacterId FOR JSON PATH), N'[]');
'@
        CharacterProgress = @'
SELECT ISNULL((SELECT CharacterId, MapId, CONVERT(varchar(30), PositionX, 3) AS PositionX,
    CONVERT(varchar(30), PositionY, 3) AS PositionY, Hp, MaxHp, BossUnlocked,
    CONVERT(varchar(33), SavedUtc, 126) AS SavedUtc, CONVERT(varchar(16), Version, 2) AS Version
FROM dh.CharacterProgress ORDER BY CharacterId FOR JSON PATH), N'[]');
'@
    }
    $tables = [ordered]@{}
    foreach ($name in $queries.Keys) {
        $json = Invoke-DbScalar -Connection $Connection -Transaction $Transaction -Sql $queries[$name]
        $tables[$name] = @(ConvertFrom-DatabaseJsonRows -Json $json)
    }
    return [pscustomobject]$tables
}

function Compare-PreservationSnapshot($Before, $After) {
    foreach ($table in @('Account', 'Character', 'CharacterProgress')) {
        $key = if ($table -ceq 'Account') { 'AccountId' } else { 'CharacterId' }
        $previous = @{}
        $current = @{}
        foreach ($row in @($Before.$table)) {
            if ($previous.ContainsKey([string]$row.$key)) {
                Stop-EngineCheck -Message 'Snapshot contains duplicate primary keys.'
            }
            $previous[[string]$row.$key] = $row
        }
        foreach ($row in @($After.$table)) {
            if ($current.ContainsKey([string]$row.$key)) {
                Stop-EngineCheck -Message 'Snapshot contains duplicate primary keys.'
            }
            $current[[string]$row.$key] = $row
        }
        foreach ($id in @($previous.Keys | Sort-Object)) {
            if (-not $current.ContainsKey($id)) {
                [pscustomobject]@{ Table = $table; Key = $id; Kind = 'Missing'; Column = $null }
                continue
            }
            $old = $previous[$id]
            $new = $current[$id]
            $columns = @(@($old.PSObject.Properties.Name) + @($new.PSObject.Properties.Name) | Sort-Object -Unique)
            foreach ($column in $columns) {
                $oldProperty = $old.PSObject.Properties[$column]
                $newProperty = $new.PSObject.Properties[$column]
                $sameValue = $false
                if ($null -ne $oldProperty -and $null -ne $newProperty) {
                    $oldValue = ConvertTo-Json -InputObject $oldProperty.Value -Compress
                    $newValue = ConvertTo-Json -InputObject $newProperty.Value -Compress
                    $sameValue = $oldValue -ceq $newValue
                }
                if (-not $sameValue) {
                    $kind = if ($column -ceq 'Version') { 'RowVersionChanged' } else { 'ValueChanged' }
                    [pscustomobject]@{ Table = $table; Key = $id; Kind = $kind; Column = $column }
                }
            }
        }
        foreach ($id in @($current.Keys | Sort-Object)) {
            if (-not $previous.ContainsKey($id)) {
                [pscustomobject]@{ Table = $table; Key = $id; Kind = 'Added'; Column = $null }
            }
        }
    }
}

function New-PreservationIds($Contract) {
    $ids = @(1..6 | ForEach-Object { [Guid]::NewGuid() })
    Assert-PreservationIds -Ids $ids -Contract $Contract
    return $ids
}

function Assert-PreservationIds($Ids, $Contract) {
    if ($Ids.Count -ne 6 -or @($Ids | Select-Object -Unique).Count -ne 6 -or
        $Ids -contains [Guid]::Empty -or $Ids -contains [Guid]$Contract.AccountId -or
        $Ids -contains [Guid]$Contract.CharacterId) {
        Stop-EngineCheck -Message 'Preservation GUIDs collide with each other or the approved binding.'
    }
}

function Invoke-PreservationRecord($Context) {
    $transaction = $null
    $started = $false
    $step = 'RecordPreservationData'
    try {
        Start-TestEnvironmentStep -Contract $Context.Contract -Manifest $Context.Manifest -Name $step `
            -Plan ([pscustomobject]@{ SnapshotPath = $Context.SnapshotPath })
        $started = $true
        $ids = @(New-PreservationIds -Contract $Context.Contract)
        Assert-PreservationIds -Ids $ids -Contract $Context.Contract
        $transaction = $Context.Connection.BeginTransaction()
        $count = Invoke-DbScalar -Connection $Context.Connection -Transaction $transaction -Sql @'
SELECT (SELECT COUNT(*) FROM dh.Account WITH (UPDLOCK, HOLDLOCK)) +
    (SELECT COUNT(*) FROM dh.Character WITH (UPDLOCK, HOLDLOCK)) +
    (SELECT COUNT(*) FROM dh.CharacterProgress WITH (UPDLOCK, HOLDLOCK));
'@
        if ($count -ne 0) {
            Stop-EngineCheck -Message 'Preservation tables are not empty; refuse adoption or writing.'
        }
        $parameters = @{}
        for ($index = 0; $index -lt $ids.Count; $index++) { $parameters['id' + $index] = $ids[$index] }
        [void](Invoke-DbNonQuery -Connection $Context.Connection -Transaction $transaction -Parameters $parameters `
                -Sql @'
SET XACT_ABORT ON;
INSERT dh.Account(AccountId) VALUES(@id0), (@id1), (@id2);
INSERT dh.Character(CharacterId, AccountId, Class)
VALUES(@id3, @id0, 0), (@id4, @id0, 1), (@id5, @id1, 0);
INSERT dh.CharacterProgress(CharacterId, MapId, PositionX, PositionY, Hp, MaxHp, BossUnlocked)
VALUES(@id3, 0, -12.5, 3.25, 120, 120, 1), (@id4, 3, 0.125, -0.5, 0, 75, 0);
'@)
        $transaction.Commit()
        $Context.Report.Committed = $true
        $snapshot = Get-PreservationSnapshot -Connection $Context.Connection
        Write-NewEngineJson -Path $Context.SnapshotPath -Value $snapshot
        $identity = [pscustomobject]@{
            SnapshotPath = $Context.SnapshotPath
            SHA256 = (Get-FileHash -LiteralPath $Context.SnapshotPath -Algorithm SHA256).Hash
            RowCounts = [pscustomobject]@{
                Account = @($snapshot.Account).Count
                Character = @($snapshot.Character).Count
                CharacterProgress = @($snapshot.CharacterProgress).Count
            }
        }
        $Context.Report.Preservation = $identity
        Complete-TestEnvironmentStep -Contract $Context.Contract -Manifest $Context.Manifest `
            -Name $step -Identity $identity
    } catch {
        $original = $_.Exception
        if ($null -ne $transaction -and $null -ne $transaction.Connection) { $transaction.Rollback() }
        if ($started) {
            Fail-TestEnvironmentStep -Contract $Context.Contract -Manifest $Context.Manifest -Name $step `
                -FailureCode (Get-DatabaseFailureCode -Exception $original)
        }
        throw $original
    } finally {
        if ($null -ne $transaction) { $transaction.Dispose() }
    }
}

function Read-PreservationSnapshot($Context) {
    $steps = @($Context.Manifest.Steps | Where-Object Name -CEQ 'RecordPreservationData')
    if ($steps.Count -ne 1 -or $steps[0].Status -cne 'Done' -or
        $steps[0].Identity.SnapshotPath -ine $Context.SnapshotPath) {
        Stop-EngineCheck -Message 'A completed preservation record and its exact snapshot path are required.'
    }
    # Hash and parse the same bytes, so a replacement between reads cannot change the compared evidence.
    $bytes = [IO.File]::ReadAllBytes($Context.SnapshotPath)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '') }
    finally { $sha.Dispose() }
    if ($hash -cne $steps[0].Identity.SHA256) {
        Stop-EngineCheck -Message 'Preservation snapshot checksum differs from the journal.'
    }
    return ([Text.UTF8Encoding]::new($false, $true).GetString($bytes).TrimStart([char]0xFEFF) | ConvertFrom-Json)
}

function Get-PreservationSchemaDifferences($Context, $History) {
    $source = @(Get-DatabaseMigrationSources -Phase Baseline001 -DatabaseRoot $Context.DatabaseRoot)[0]
    $steps = @($Context.Manifest.Steps | Where-Object Name -CEQ 'InstallBaseline001')
    $baseline = @()
    if ($steps.Count -eq 1 -and $steps[0].Status -ceq 'Done') {
        $baseline = @($steps[0].Identity.MigrationManifest)
    }
    $origins = [ordered]@{
        Database = @($History | Where-Object Version -EQ 1)
        Manifest = @($Context.Manifest.MigrationManifest | Select-Object -First 1)
        BaselineJournal = $baseline
    }
    foreach ($origin in $origins.Keys) {
        $rows = @($origins[$origin])
        if ($rows.Count -ne 1) {
            [pscustomobject]@{ Source = $origin; Kind = 'BaselineRowCount'; Column = $null }
            continue
        }
        foreach ($column in @('Version', 'Name', 'Checksum')) {
            if ([string]$rows[0].$column -cne [string]$source.$column) {
                [pscustomobject]@{ Source = $origin; Kind = 'BaselineMismatch'; Column = $column }
            }
        }
    }
}

function Get-EngineHistory($Connection) {
    $json = Invoke-DbScalar -Connection $Connection -Sql @'
SELECT ISNULL((SELECT Version, Name, Checksum FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH), N'[]');
'@
    return @(ConvertFrom-DatabaseJsonRows -Json $json)
}

function Invoke-PreservationCompare($Context) {
    $before = Read-PreservationSnapshot -Context $Context
    $after = Get-PreservationSnapshot -Connection $Context.Connection
    $history = @(Get-EngineHistory -Connection $Context.Connection)
    $differences = @(Compare-PreservationSnapshot -Before $before -After $after) +
    @(Get-PreservationSchemaDifferences -Context $Context -History $history)
    $Context.Report.Preservation = [ordered]@{
        Outcome = $(if ($differences.Count) { 'FAIL' } else { 'PASS' })
        Differences = $differences
        Before = $before
        After = $after
        History = $history
    }
    if ($differences.Count) { Stop-EngineCheck -Message 'Preservation comparison failed.' }
}

function ConvertTo-UnwrappedEngineSql([string]$Sql) {
    $open = 'ISNULL(('
    $close = "), N'[]')"
    if ([regex]::Matches($Sql, [regex]::Escape($open)).Count -ne 1 -or
        [regex]::Matches($Sql, [regex]::Escape($close)).Count -ne 1) {
        Stop-EngineCheck -Message 'JSON boundary requires exactly one reviewed opening and closing wrapper.'
    }
    return $Sql.Replace($open, '((').Replace($close, '))')
}

function Get-EngineJsonSites([string]$DatabaseRoot) {
    $sites = [Collections.Generic.List[object]]::new()
    foreach ($file in @('Database.Common.ps1', 'Module.Common.ps1')) {
        $tokens = $null
        $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile(
            (Join-Path $DatabaseRoot $file), [ref]$tokens, [ref]$errors
        )
        if ($errors.Count) { Stop-EngineCheck -Message 'Product source cannot be parsed for observation.' }
        $calls = @($ast.FindAll({
                    param($node)
                    $node -is [Management.Automation.Language.CommandAst] -and
                    $node.GetCommandName() -ceq 'Invoke-DbScalar'
                }, $true))
        foreach ($call in $calls) {
            $elements = $call.CommandElements
            for ($index = 0; $index -lt ($elements.Count - 1); $index++) {
                if ($elements[$index] -isnot [Management.Automation.Language.CommandParameterAst] -or
                    $elements[$index].ParameterName -cne 'Sql') { continue }
                $argument = $elements[$index + 1]
                if ($argument -isnot [Management.Automation.Language.StringConstantExpressionAst] -or
                    -not $argument.Value.StartsWith('SELECT ISNULL((')) { continue }
                $sql = $argument.Value
                $unwrapped = ConvertTo-UnwrappedEngineSql -Sql $sql
                $sites.Add([pscustomobject]@{
                        Site = $file + ':' + $argument.Extent.StartLineNumber
                        Sql = $sql
                        Unwrapped = $unwrapped
                        SqlSHA256 = Get-MigrationHash -Sql $sql
                        Kind = $null
                        CountSql = $null
                    })
            }
        }
    }
    if ($sites.Count -ne 5) { Stop-EngineCheck -Message 'Exactly five product JSON scalar sites are required.' }
    $kinds = @('HistoryBefore', 'HistoryAfter', 'ModuleRegistration', 'ActualModules', 'Release')
    $sources = @('FROM dh.SchemaVersion ', 'FROM dh.SchemaVersion ', 'FROM dh.ModuleDefinition ',
        'FROM sys.objects o JOIN sys.schemas ', 'FROM dh.ModuleRelease r LEFT JOIN dh.SchemaVersion ')
    $counts = @('SELECT COUNT(*) FROM dh.SchemaVersion;', 'SELECT COUNT(*) FROM dh.SchemaVersion;',
        'SELECT COUNT(*) FROM dh.ModuleDefinition;', @'
SELECT COUNT(*) FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = N'dh' AND o.type IN ('P', 'FN', 'IF', 'TF', 'FS', 'FT', 'PC');
'@, 'SELECT COUNT(*) FROM dh.ModuleRelease;')
    for ($index = 0; $index -lt 5; $index++) {
        if (-not $sites[$index].Sql.Contains($sources[$index])) {
            Stop-EngineCheck -Message 'Product JSON source mapping changed; review before observation.'
        }
        $sites[$index].Kind = $kinds[$index]
        $sites[$index].CountSql = $counts[$index]
    }
    return $sites.ToArray()
}

function Get-EngineRawSummary($Value) {
    $kind = 'Other'
    if ($Value -is [DBNull]) { $kind = 'DBNull' }
    elseif ($Value -is [string]) { $kind = 'String' }
    $length = $null
    $hash = $null
    $prefix = $null
    if ($kind -ceq 'String') {
        $length = $Value.Length
        $hash = Get-MigrationHash -Sql $Value
        $prefix = $Value.Substring(0, [Math]::Min(200, $length))
    }
    return [pscustomobject]@{ Type = $kind; Length = $length; SHA256 = $hash; Prefix = $prefix }
}

function Get-EngineJsonVerdict($Wrapped, $Unwrapped, [int]$Count) {
    # Fixed decision table: product normalization is required; raw empty behavior is an observation.
    $issues = [Collections.Generic.List[string]]::new()
    $rawOutcome = 'PASS'
    $readerMessage = $null
    $wrappedRows = @()
    if ($Wrapped -isnot [string]) {
        $issues.Add('Wrapped value is not a string.')
    } else {
        try { $wrappedRows = @(ConvertFrom-DatabaseJsonRows -Json $Wrapped) }
        catch { $issues.Add('Wrapped value is not a complete JSON row array.') }
        if ($Count -eq 0 -and $Wrapped -cne '[]') { $issues.Add('Wrapped empty value must be exactly [].') }
        if ($Count -gt 0 -and -not $Wrapped.StartsWith('[')) { $issues.Add('Wrapped rows must start with [.') }
        if ($wrappedRows.Count -ne $Count) { $issues.Add('JSON rows differ from independent COUNT(*).') }
    }
    if ($Count -gt 0) {
        if ($Unwrapped -isnot [string] -or $Unwrapped -cne $Wrapped) {
            $issues.Add('Unwrapped nonempty value differs from wrapped value.')
        }
    } elseif ($Unwrapped -is [DBNull]) {
        try { $null = ConvertFrom-DatabaseJsonRows -Json $Unwrapped }
        catch { $readerMessage = $_.Exception.Message }
        if ($null -eq $readerMessage -or
            -not $readerMessage.StartsWith('Expected the complete database JSON row array')) {
            $issues.Add('Product JSON reader did not reject DBNull with the fixed boundary message.')
        }
    } else {
        $rawOutcome = 'OBSERVED'
    }
    return [pscustomobject]@{
        Outcome = $(if ($issues.Count) { 'FAIL' } else { 'PASS' })
        UnwrappedOutcome = $rawOutcome
        WrappedRows = $wrappedRows.Count
        IndependentCount = $Count
        ReaderRejection = $readerMessage
        Issues = @($issues.ToArray())
    }
}

function New-EngineObserver($Context, [string]$Scenario, [switch]$Observe, [switch]$Fault) {
    return [pscustomobject]@{
        Context = $Context
        Scenario = $Scenario
        Observe = [bool]$Observe
        Fault = [bool]$Fault
        Armed = $false
        Fired = $false
        HistoryCalls = 0
        ProductCalls = 0
        ArmedAfterCall = $null
        FiredAtCall = $null
        ArmedSqlSHA256 = $null
        FaultSqlSHA256 = $null
        ReturnedNormally = $false
        OriginalScalar = $script:EngineProductScalar
        OriginalNonQuery = $script:EngineProductNonQuery
    }
}

function Assert-EngineFaultBoundary($Observer, [string]$Sql) {
    $Observer.ProductCalls++
    if ($Observer.Fault -and $Observer.Armed) {
        $Observer.Armed = $false
        $Observer.Fired = $true
        $Observer.FiredAtCall = $Observer.ProductCalls
        $Observer.FaultSqlSHA256 = Get-MigrationHash -Sql $Sql
        Stop-EngineCheck -Message 'Engine check injected failure after migration metadata write.'
    }
}

function Add-EngineScalarCall($Observer, [string]$Sql, $Value, [string]$Origin, $Failure = $null) {
    $log = $Observer.Context.Report.ScalarCalls
    $log.Add([pscustomobject]@{
            Sequence = $log.Count + 1
            Scenario = $Observer.Scenario
            Origin = $Origin
            SqlSHA256 = Get-MigrationHash -Sql $Sql
            Raw = $(if ($null -eq $Failure) { Get-EngineRawSummary -Value $Value } else { $null })
            Failure = $Failure
        })
}

function Invoke-EngineObservedScalar(
    $Observer, $Connection, [string]$Sql, $Transaction = $null,
    [hashtable]$Parameters = @{}, [string]$Origin
) {
    try {
        $value = & $Observer.OriginalScalar -Connection $Connection -Sql $Sql `
            -Transaction $Transaction -Parameters $Parameters
    } catch {
        Add-EngineScalarCall -Observer $Observer -Sql $Sql -Origin $Origin `
            -Failure (Get-EngineCheckFailure -Exception $_.Exception)
        throw
    }
    Add-EngineScalarCall -Observer $Observer -Sql $Sql -Value $value -Origin $Origin
    return $value
}

function Add-EngineJsonObservation($Observer, $Site, $Connection, $Transaction, $Wrapped) {
    $unwrapped = Invoke-EngineObservedScalar -Observer $Observer -Connection $Connection `
        -Transaction $Transaction -Sql $Site.Unwrapped -Origin 'Unwrapped probe'
    $count = Invoke-EngineObservedScalar -Observer $Observer -Connection $Connection `
        -Transaction $Transaction -Sql $Site.CountSql -Origin 'Independent count'
    $verdict = Get-EngineJsonVerdict -Wrapped $Wrapped -Unwrapped $unwrapped -Count $count
    $Observer.Context.Report.Observations.Add([pscustomobject]@{
            Site = $Site.Site
            Kind = $Site.Kind
            Scenario = $Observer.Scenario
            SqlSHA256 = $Site.SqlSHA256
            Wrapped = Get-EngineRawSummary -Value $Wrapped
            Unwrapped = Get-EngineRawSummary -Value $unwrapped
            Verdict = $verdict
        })
    if ($verdict.Outcome -cne 'PASS') { Stop-EngineCheck -Message 'JSON boundary decision table failed.' }
}

function Invoke-WithEngineObserver(
    [Alias('Observer')]$ActiveEngineObserver,
    [Alias('Action')][scriptblock]$ObservedAction
) {
    # Local dynamic scope restores both product functions automatically on success or failure.
    # Original scriptblocks still create commands through the unchanged product parameter/timeout boundary.
    function Invoke-DbScalar($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null) {
        try { Assert-EngineFaultBoundary -Observer $ActiveEngineObserver -Sql $Sql }
        catch {
            Add-EngineScalarCall -Observer $ActiveEngineObserver -Sql $Sql -Origin 'Product fault injection' `
                -Failure (Get-EngineCheckFailure -Exception $_.Exception)
            throw
        }
        $value = Invoke-EngineObservedScalar -Observer $ActiveEngineObserver @PSBoundParameters -Origin 'Product'
        if ($ActiveEngineObserver.Observe) {
            $matches = @($ActiveEngineObserver.Context.Sites | Where-Object Sql -CEQ $Sql)
            if ($matches.Count) {
                $site = $matches[0]
                if ($matches.Count -eq 2) {
                    $site = $matches[[Math]::Min(1, $ActiveEngineObserver.HistoryCalls)]
                    $ActiveEngineObserver.HistoryCalls++
                }
                Add-EngineJsonObservation -Observer $ActiveEngineObserver -Site $site -Connection $Connection `
                    -Transaction $Transaction -Wrapped $value
            }
        }
        return $value
    }
    function Invoke-DbNonQuery($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null) {
        Assert-EngineFaultBoundary -Observer $ActiveEngineObserver -Sql $Sql
        $value = & $ActiveEngineObserver.OriginalNonQuery @PSBoundParameters
        if ($ActiveEngineObserver.Fault -and $Sql -ceq
            'INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(@version,@name,@hash)' -and
            $Parameters.ContainsKey('version') -and $Parameters.version -eq 2) {
            $ActiveEngineObserver.Armed = $true
            $ActiveEngineObserver.ArmedAfterCall = $ActiveEngineObserver.ProductCalls
            $ActiveEngineObserver.ArmedSqlSHA256 = Get-MigrationHash -Sql $Sql
        }
        return $value
    }
    & $ObservedAction
}

function Get-EngineDatabaseState($Context) {
    $objects = [ordered]@{}
    foreach ($name in @('CharacterAuthority', 'CharacterOperation', 'ModuleDefinition', 'ModuleRelease')) {
        $value = Invoke-DbScalar -Connection $Context.Connection -Sql 'SELECT OBJECT_ID(@name);' `
            -Parameters @{ name = 'dh.' + $name }
        $objects[$name] = $(if ($value -is [DBNull]) { $null } else { $value })
    }
    $metadata = [ordered]@{}
    if ($null -ne $objects.ModuleDefinition -and $null -ne $objects.ModuleRelease) {
        $metadataKinds = @('ModuleRegistration', 'ActualModules', 'Release')
        foreach ($site in @($Context.Sites | Where-Object Kind -In $metadataKinds)) {
            $metadata[$site.Kind] = Invoke-DbScalar -Connection $Context.Connection -Sql $site.Sql
        }
    }
    return [pscustomobject]@{
        Snapshot = Get-PreservationSnapshot -Connection $Context.Connection
        History = @(Get-EngineHistory -Connection $Context.Connection)
        Objects = [pscustomobject]$objects
        Metadata = [pscustomobject]$metadata
        TransactionCount = Invoke-DbScalar -Connection $Context.Connection -Sql 'SELECT @@TRANCOUNT;'
    }
}

function Assert-EngineRestoredState($Context, $Before, [string]$Scenario) {
    $after = Get-EngineDatabaseState -Context $Context
    $differences = @(Compare-PreservationSnapshot -Before $Before.Snapshot -After $after.Snapshot)
    foreach ($part in @('History', 'Objects', 'Metadata', 'TransactionCount')) {
        $old = ConvertTo-Json -InputObject $Before.$part -Depth 15 -Compress
        $new = ConvertTo-Json -InputObject $after.$part -Depth 15 -Compress
        if ($old -cne $new) { $differences += [pscustomobject]@{ Kind = 'StateChanged'; Part = $part } }
    }
    if ($after.TransactionCount -ne 0) {
        $differences += [pscustomobject]@{ Kind = 'OpenTransaction'; Part = 'TransactionCount' }
    }
    $Context.Report.StateComparisons.Add([pscustomobject]@{
            Scenario = $Scenario
            Before = $Before
            After = $after
            Differences = $differences
            Outcome = $(if ($differences.Count) { 'FAIL' } else { 'PASS' })
        })
    if ($differences.Count) { Stop-EngineCheck -Message 'Rollback state differs; stop all later observations.' }
}

function Invoke-EngineRollback($Context, [string]$Scenario, [scriptblock]$Action) {
    $before = Get-EngineDatabaseState -Context $Context
    $transaction = $Context.Connection.BeginTransaction()
    try {
        & $Action $transaction
    } finally {
        try {
            if ($null -ne $transaction.Connection) { $transaction.Rollback() }
        } finally {
            $transaction.Dispose()
        }
        Assert-EngineRestoredState -Context $Context -Before $before -Scenario $Scenario
    }
}

function Get-EngineRejectionPrefix([string]$Case) {
    # Expected prefixes are independent, fixed contracts, never extracted from the implementation under test.
    switch ($Case) {
        'EmptyRelease' { return 'Current bundle differs from its migration declaration' }
        'EmptyRegistration' { return 'Existing module registration/object set is incomplete or unexpected' }
        'MissingRegistration' { return 'Existing module registration/object set is incomplete or unexpected' }
        'DefinitionDrift' { return 'Registered/actual module drift' }
        'MalformedRelease' { return 'Module release history has an unknown, missing-schema or malformed declaration' }
        default { Stop-EngineCheck -Message 'Unknown rejection case.' }
    }
}

function Invoke-EngineRejection($Context, [string]$Case, [string]$Stimulus, [scriptblock]$Action) {
    $prefix = Get-EngineRejectionPrefix -Case $Case
    $actual = $null
    $code = $null
    try { $null = & $Action }
    catch {
        $code = Get-DatabaseFailureCode -Exception $_.Exception
        # Only these product exceptions are allowed to expose their text; provider messages remain suppressed.
        if ($_.Exception.Message.StartsWith($prefix) -and
            $_.Exception.Message -cnotmatch '(?i)password|pwd\s*=|connectionstring|secret') {
            $actual = $_.Exception.Message
        } else {
            $actual = (Get-EngineCheckFailure -Exception $_.Exception).Message
        }
    }
    $passed = $null -ne $actual -and $actual.StartsWith($prefix)
    $Context.Report.Rejections.Add([pscustomobject]@{
            Case = $Case
            Stimulus = $Stimulus
            ExpectedPrefix = $prefix
            ActualMessage = $actual
            SqlFailure = $code
            Outcome = $(if ($passed) { 'PASS' } else { 'FAIL' })
        })
    if (-not $passed) { Stop-EngineCheck -Message 'Expected product rejection was not observed.' }
}

function Invoke-EngineStimulus($Context, $Transaction, [string]$Case, [string]$Sql) {
    try {
        [void](Invoke-DbNonQuery -Connection $Context.Connection -Transaction $Transaction -Sql $Sql)
        return $true
    } catch {
        $failure = Get-EngineCheckFailure -Exception $_.Exception
        $Context.Report.Rejections.Add([pscustomobject]@{
                Case = $Case
                Stimulus = $Sql
                ActualMessage = $failure.Message
                SqlFailure = $failure.Code
                Outcome = 'UNOBSERVED'
            })
        if ($null -eq $failure.Code.SqlNumber -or $failure.Code.SqlNumber -le 0) { throw }
        return $false
    }
}

function Invoke-EngineSiteProbe($Context, $Transaction, [string]$Kind, [string]$Scenario) {
    $site = @($Context.Sites | Where-Object Kind -CEQ $Kind)[0]
    $observer = New-EngineObserver -Context $Context -Scenario $Scenario
    $wrapped = Invoke-EngineObservedScalar -Observer $observer -Connection $Context.Connection `
        -Transaction $Transaction -Sql $site.Sql -Origin 'Wrapped probe'
    Add-EngineJsonObservation -Observer $observer -Site $site -Connection $Context.Connection `
        -Transaction $Transaction -Wrapped $wrapped
}

function Invoke-EngineAtomicity($Context) {
    $before = Get-EngineDatabaseState -Context $Context
    foreach ($owner in @('ToolRehearsal', 'Product')) {
        $observer = New-EngineObserver -Context $Context -Scenario ('Atomicity' + $owner) -Fault
        $transaction = $null
        $failure = $null
        if ($owner -ceq 'ToolRehearsal') { $transaction = $Context.Connection.BeginTransaction() }
        try {
            Invoke-WithEngineObserver -Observer $observer -Action {
                $parameters = @{
                    Connection = $Context.Connection
                    Phase = 'Complete'
                    Contract = $Context.Contract
                }
                if ($null -ne $transaction) { $parameters.Transaction = $transaction }
                $null = Invoke-Migrations @parameters
                if ($owner -ceq 'Product') { $Context.Report.Committed = $true }
                $observer.ReturnedNormally = $true
            }
        } catch {
            $failure = Get-EngineCheckFailure -Exception $_.Exception
        } finally {
            if ($null -ne $transaction) {
                try {
                    if ($null -ne $transaction.Connection) { $transaction.Rollback() }
                } finally { $transaction.Dispose() }
            }
        }
        $passed = $observer.Fired -and $observer.FiredAtCall -eq ($observer.ArmedAfterCall + 1) -and
        $null -ne $failure -and
        $failure.Message -ceq 'Engine check injected failure after migration metadata write.'
        $Context.Report.Atomicity.Add([pscustomobject]@{
                Owner = $owner
                ArmedAfterCall = $observer.ArmedAfterCall
                FiredAtCall = $observer.FiredAtCall
                ArmedSqlSHA256 = $observer.ArmedSqlSHA256
                FaultSqlSHA256 = $observer.FaultSqlSHA256
                ReturnedNormally = $observer.ReturnedNormally
                Failure = $failure
                Outcome = $(if ($passed) { 'PASS' } else { 'FAIL' })
            })
        Assert-EngineRestoredState -Context $Context -Before $before -Scenario ('Atomicity' + $owner)
        if (-not $passed) {
            Stop-EngineCheck -Message 'Fault rehearsal did not confirm the required boundary; stop before next owner.'
        }
    }
}
