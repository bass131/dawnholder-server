[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the registered source hash consumers checked by Test-ModuleStructure.ps1.
# Expected values come from TestSupport's explicit-encoding helpers (pinned by the Node crypto known answer in
# ModuleBundle.Tests.ps1) and from this file's own literal enumeration; ModuleHash.Common.ps1 is never the oracle.
# Every CLI case runs the real entry point in a separate Windows PowerShell process against a fixture copy.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'module-hash-consumers'

$manifestFile = 'modules/manifest.json'
$catalogFile = 'verify-schema.sql'
$declarationFile = 'migrations/004_module_release.sql'
$guardFile = 'Module.Common.ps1'
$consumerFiles = @($manifestFile, $catalogFile, $declarationFile, $guardFile)
$expectedKinds = [ordered]@{
    ManifestSourceChecksum = 19
    ManifestDefinitionChecksum = 18
    ManifestDefinitionBytes = 18
    CatalogSourceChecksum = 18
    CatalogDefinitionChecksum = 18
    CatalogDefinitionBytes = 18
    CatalogMigrationChecksum = 4
    CatalogManifestIdentity = 1
    ReleaseManifestIdentity = 1
    ImmutableMigrationChecksum = 1
}
$identitySource = 'modules/manifest.json (raw UTF-8 bytes)'
$immutableSource = 'migrations/001_initial.sql (CRLF->LF UTF-8)'

function Get-LineNumber {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][int]$Index
    )
    return 1 + ([regex]::Matches($Text.Substring(0, $Index), "`n")).Count
}

function New-ConsumerRow {
    param(
        [string]$File,
        [string]$Text,
        [int]$Index,
        [string]$Kind,
        [string]$Source,
        [string]$Value,
        [string]$Expected
    )
    return [pscustomobject]@{
        File = $File
        Line = Get-LineNumber -Text $Text -Index $Index
        Kind = $Kind
        Source = $Source
        Value = $Value
        Expected = $Expected
    }
}

function Get-ConsumerTable {
    param([Parameter(Mandatory)][string]$Root)
    # The verifier's own enumeration of every hash-like literal in the four consumer files and its reviewed value.
    # Any 64-hex token or entry hash field that is not a documented consumer is kept as an UNREGISTERED row.
    $rows = New-Object 'Collections.Generic.List[object]'
    $manifestBytes = [IO.File]::ReadAllBytes((Join-Path $Root $manifestFile))
    $identity = Get-Sha256Hex -Bytes $manifestBytes
    $manifestText = (New-Object Text.UTF8Encoding($false, $true)).GetString($manifestBytes)
    $objects = @{}
    $fieldPattern = '"(?<Field>\w+)"\s*:\s*(?:"(?<Value>[0-9A-Fa-f]{64})"|(?<Value>-?\d[\d.eE+-]*))'
    foreach ($entry in [regex]::Matches($manifestText, '"Path"\s*:\s*"(?<Path>[^"]+)"[^}]*\}')) {
        $path = $entry.Groups['Path'].Value
        $values = Get-ExpectedModuleValues -Path (Join-Path $Root $path)
        $objectName = [regex]::Match($entry.Value, '"ObjectName"\s*:\s*"dh\.(?<Name>\w+)"')
        if ($objectName.Success) { $objects[$objectName.Groups['Name'].Value] = $path }
        foreach ($field in [regex]::Matches($entry.Value, $fieldPattern)) {
            $name = $field.Groups['Field'].Value
            $literal = $field.Groups['Value']
            $expected = ''
            if ($name -ceq 'SourceChecksum') {
                $kind = 'ManifestSourceChecksum'
                $expected = $values.SourceChecksum
            }
            elseif ($name -ceq 'DefinitionChecksum') {
                $kind = 'ManifestDefinitionChecksum'
                $expected = $values.DefinitionChecksum
            }
            elseif ($name -ceq 'DefinitionBytes') {
                # The permissions entry registers 0 engine bytes; it is not a hash consumer.
                if ($path -ceq 'modules/permissions.sql') { continue }
                $kind = 'ManifestDefinitionBytes'
                $expected = [string]$values.DefinitionBytes
            }
            else {
                $kind = 'UNREGISTERED:' + $name
            }
            $row = @{
                File = $manifestFile
                Text = $manifestText
                Index = $entry.Index + $literal.Index
                Kind = $kind
                Source = $path
                Value = $literal.Value
                Expected = $expected
            }
            $rows.Add((New-ConsumerRow @row))
        }
    }

    $catalog = Read-FixtureText -Path (Join-Path $Root $catalogFile)
    $known = @{}
    $modulePattern = "\('(?<Name>\w+)',\s*'(?:FN|P)',\s*(?<Bytes>\d+),\s*" +
    "0x(?<Definition>[0-9A-Fa-f]{64}),\s*'(?<Source>[0-9A-Fa-f]{64})'\)"
    foreach ($match in [regex]::Matches($catalog, $modulePattern)) {
        $path = $objects[$match.Groups['Name'].Value]
        $values = Get-ExpectedModuleValues -Path (Join-Path $Root $path)
        $fields = @(
            @{ Group = 'Bytes'; Kind = 'CatalogDefinitionBytes'; Expected = [string]$values.DefinitionBytes },
            @{ Group = 'Definition'; Kind = 'CatalogDefinitionChecksum'; Expected = $values.DefinitionChecksum },
            @{ Group = 'Source'; Kind = 'CatalogSourceChecksum'; Expected = $values.SourceChecksum }
        )
        foreach ($field in $fields) {
            $literal = $match.Groups[$field.Group]
            $known[$literal.Index] = $true
            $row = @{
                File = $catalogFile
                Text = $catalog
                Index = $literal.Index
                Kind = $field.Kind
                Source = $path
                Value = $literal.Value
                Expected = $field.Expected
            }
            $rows.Add((New-ConsumerRow @row))
        }
    }
    $migrationPattern = "\((?<Version>\d+),\s*'(?<Name>\d{3}_[a-z_]+\.sql)',\s*'(?<Value>[0-9A-Fa-f]{64})'\)"
    foreach ($match in [regex]::Matches($catalog, $migrationPattern)) {
        $literal = $match.Groups['Value']
        $known[$literal.Index] = $true
        $source = 'migrations/' + $match.Groups['Name'].Value
        $row = @{
            File = $catalogFile
            Text = $catalog
            Index = $literal.Index
            Kind = 'CatalogMigrationChecksum'
            Source = $source
            Value = $literal.Value
            Expected = (Get-ExpectedModuleValues -Path (Join-Path $Root $source)).SourceChecksum
        }
        $rows.Add((New-ConsumerRow @row))
    }
    $releasePattern = "ManifestChecksum\s+COLLATE\s+\w+\s*=\s*'(?<Value>[0-9A-Fa-f]{64})'"
    foreach ($match in [regex]::Matches($catalog, $releasePattern)) {
        $literal = $match.Groups['Value']
        $known[$literal.Index] = $true
        $row = @{
            File = $catalogFile
            Text = $catalog
            Index = $literal.Index
            Kind = 'CatalogManifestIdentity'
            Source = $identitySource
            Value = $literal.Value
            Expected = $identity
        }
        $rows.Add((New-ConsumerRow @row))
    }
    foreach ($match in [regex]::Matches($catalog, '[0-9A-Fa-f]{64}')) {
        if ($known.ContainsKey($match.Index)) { continue }
        $row = @{
            File = $catalogFile
            Text = $catalog
            Index = $match.Index
            Kind = 'UNREGISTERED'
            Source = ''
            Value = $match.Value
            Expected = ''
        }
        $rows.Add((New-ConsumerRow @row))
    }

    $baselineSource = Join-Path $Root 'migrations/001_initial.sql'
    $singleValueFiles = @(
        @{
            File = $declarationFile
            Kind = 'ReleaseManifestIdentity'
            Source = $identitySource
            Expected = $identity
        },
        @{
            File = $guardFile
            Kind = 'ImmutableMigrationChecksum'
            Source = $immutableSource
            Expected = (Get-ExpectedModuleValues -Path $baselineSource).SourceChecksum
        }
    )
    foreach ($consumer in $singleValueFiles) {
        $text = Read-FixtureText -Path (Join-Path $Root $consumer.File)
        foreach ($match in [regex]::Matches($text, '[0-9A-Fa-f]{64}')) {
            $row = @{
                File = $consumer.File
                Text = $text
                Index = $match.Index
                Kind = $consumer.Kind
                Source = $consumer.Source
                Value = $match.Value
                Expected = $consumer.Expected
            }
            $rows.Add((New-ConsumerRow @row))
        }
    }
    return @($rows.ToArray())
}

function Get-ConsumerRow {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Kind,
        [string]$Source
    )
    $found = @(Get-ConsumerTable -Root $Root | Where-Object {
            $_.Kind -ceq $Kind -and (-not $Source -or $_.Source -ceq $Source)
        })
    if ($found.Count -lt 1) { throw "No consumer row for $Kind $Source." }
    return $found[0]
}

function Set-ConsumerLiteral {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)]$Consumer,
        [Parameter(Mandatory)][string]$Value
    )
    # Deliberate stimulus: replace one literal on its line in place; every other byte stays as reviewed.
    $path = Join-Path $Root $Consumer.File
    $lines = (Read-FixtureText -Path $path).Split("`n")
    $token = '(?<![0-9A-Fa-f])' + [regex]::Escape($Consumer.Value) + '(?![0-9A-Fa-f])'
    $line = $lines[$Consumer.Line - 1]
    if ([regex]::Matches($line, $token).Count -ne 1) {
        throw "Stimulus target is not unique on $($Consumer.File):$($Consumer.Line)."
    }
    $lines[$Consumer.Line - 1] = [regex]::Replace($line, $token, $Value)
    Write-FixtureText -Path $path -Text ($lines -join "`n")
}

function Add-FixtureText {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Text
    )
    Write-FixtureText -Path $Path -Text ((Read-FixtureText -Path $Path) + $Text)
}

function Get-OtherHex {
    param([Parameter(Mandatory)][string]$Value)
    # Same registered format, different value: change only the last hexadecimal digit.
    $last = if ($Value.EndsWith('A')) { 'B' } else { 'A' }
    return $Value.Substring(0, $Value.Length - 1) + $last
}

function Get-InputIssue {
    param($Result)
    if ($null -eq $Result) { return @() }
    return @($Result.Issues | Where-Object {
            $null -ne $_.PSObject.Properties['Kind'] -and $_.Kind -ceq 'HashInspectionUnavailable'
        })
}

function Get-DriftIssue {
    param($Result)
    if ($null -eq $Result) { return @() }
    return @($Result.Issues | Where-Object {
            $null -ne $_.PSObject.Properties['Kind'] -and $_.Kind -cne 'HashInspectionUnavailable'
        })
}

function Get-DriftKinds {
    param($Result)
    return @(Get-DriftIssue -Result $Result | ForEach-Object { $_.Kind } | Sort-Object) -join ','
}

function Test-SingleDriftCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)]$Consumer,
        [Parameter(Mandatory)][string]$Observed,
        [Parameter(Mandatory)][string]$Expected
    )
    # One drifted consumer fails both modes with exactly that target and a concrete replacement.
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $result = $warning.Result
    $drift = @(Get-DriftIssue -Result $result)
    $issue = if ($drift.Count -eq 1) { $drift[0] } else { $null }
    $detail = Get-IssueText -Result $result
    Assert-True -Name "$Name -> violation with one hash drift of 116 complete targets" -Condition (
        $null -ne $result -and $result.Status -ceq 'violation' -and
        $result.HashViolationCount -eq 1 -and $result.ViolationCount -eq 1 -and
        $result.HashTargetCount -eq 116 -and $result.HashInspectionComplete -eq $true) -Detail $detail
    Assert-True -Name "$Name -> drift names the consumer file, line, kind and source" -Condition (
        $null -ne $issue -and $issue.File -ceq $Consumer.File -and $issue.Line -eq $Consumer.Line -and
        $issue.Kind -ceq $Consumer.Kind -and $issue.Source -ceq $Consumer.Source) -Detail $detail
    Assert-True -Name "$Name -> observed and independently recomputed expected values" -Condition (
        $null -ne $issue -and $issue.Observed -ceq $Observed -and $issue.Expected -ceq $Expected) -Detail $detail
    $replacement = "Replace '$Observed' with '$Expected' at $($Consumer.File):$($Consumer.Line)"
    Assert-True -Name "$Name -> remediation gives the exact replacement" -Condition (
        $null -ne $issue -and $issue.Remediation.Contains($replacement)) -Detail $detail
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 1 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 1 -Actual $strict.ExitCode
}

function Test-HashUnavailableCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$MessagePattern,
        [Parameter(Mandatory)][string]$File
    )
    # Missing input, unknown registration or unknown literal format is unavailable in both modes (MSSQL.md).
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $result = $warning.Result
    $inputIssues = @(Get-InputIssue -Result $result)
    $detail = Get-IssueText -Result $result
    Assert-True -Name "$Name -> unavailable, null counts, incomplete inspection" -Condition (
        $null -ne $result -and $result.Status -ceq 'unavailable' -and $null -eq $result.ViolationCount -and
        $null -eq $result.HashViolationCount -and $result.HashInspectionComplete -eq $false) -Detail $detail
    Assert-True -Name "$Name -> one input issue with the intended cause" -Condition (
        $inputIssues.Count -eq 1 -and $inputIssues[0].Message -cmatch $MessagePattern) -Detail $detail
    Assert-Equal -Name "$Name -> input issue names the file to fix" -Expected $File `
        -Actual $(if ($inputIssues.Count -eq 1) { $inputIssues[0].File })
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 2 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 2 -Actual $strict.ExitCode
    # Kept for follow-up assertions; the function output stays the PASS/FAIL lines.
    $script:LastHashResult = $result
}

function Get-ConsumerHashes {
    param([Parameter(Mandatory)][string]$Root)
    return @($consumerFiles | ForEach-Object {
            $_ + '=' + (Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes((Join-Path $Root $_))))
        }) -join '|'
}

function Invoke-ChildScript {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Lines
    )
    # Import-boundary probes run in their own Windows PowerShell process and print one JSON object.
    $path = Join-Path $script:SuiteRoot ($Name + '.ps1')
    Write-FixtureText -Path $path -Text (($Lines -join "`n") + "`n")
    $run = Invoke-PowerShellFile -File $path -WorkingDirectory $script:SuiteRoot
    $result = $null
    if ($run.StdOut.Trim().StartsWith('{')) { $result = $run.StdOut | ConvertFrom-Json }
    return [pscustomobject]@{
        ExitCode = $run.ExitCode
        Result = $result
        Output = ($run.StdOut + $run.StdErr).Trim()
    }
}

# ---- Positive controls: the reviewed tree and its byte copy.
$realArguments = @('-Json', '-Strict')
$real = Invoke-PowerShellFile -File (Join-Path $script:ToolRoot 'Test-ModuleStructure.ps1') -Arguments $realArguments
$realResult = if ($real.StdOut.Trim().StartsWith('{')) { $real.StdOut | ConvertFrom-Json } else { $null }
Assert-True -Name 'repository tree -> compliant, 116 complete targets, no drift, Strict exit0' -Condition (
    $real.ExitCode -eq 0 -and $null -ne $realResult -and $realResult.Status -ceq 'compliant' -and
    $realResult.HashTargetCount -eq 116 -and $realResult.HashInspectionComplete -eq $true -and
    $realResult.HashViolationCount -eq 0 -and $realResult.ViolationCount -eq 0) `
    -Detail ('exit=' + $real.ExitCode + '; ' + (Get-IssueText -Result $realResult))

$reviewedRoot = New-DatabaseFixture -Name 'reviewed'
$before = Get-ConsumerHashes -Root $reviewedRoot
Sync-FixtureHashConsumers -Root $reviewedRoot
Assert-Equal -Name 'independent hash-chain resync of the reviewed copy changes no consumer byte' `
    -Expected $before -Actual (Get-ConsumerHashes -Root $reviewedRoot)
$reviewed = Invoke-StructureCheck -Root $reviewedRoot
$reviewedStrict = Invoke-StructureCheck -Root $reviewedRoot -Strict
$reviewedResult = $reviewed.Result
$targets = @(if ($reviewedResult) { $reviewedResult.HashTargets })
Assert-True -Name 'reviewed copy -> compliant with 116 complete targets and zero drift' -Condition (
    $null -ne $reviewedResult -and $reviewedResult.Status -ceq 'compliant' -and
    $reviewedResult.HashTargetCount -eq 116 -and $targets.Count -eq 116 -and
    $reviewedResult.HashInspectionComplete -eq $true -and $reviewedResult.HashViolationCount -eq 0) `
    -Detail (Get-IssueText -Result $reviewedResult)
Assert-Equal -Name 'reviewed copy -> warning exit' -Expected 0 -Actual $reviewed.ExitCode
Assert-Equal -Name 'reviewed copy -> Strict exit' -Expected 0 -Actual $reviewedStrict.ExitCode
$kindCounts = @($expectedKinds.Keys | ForEach-Object {
        $kind = $_
        $kind + '=' + @($targets | Where-Object Kind -CEQ $kind).Count
    }) -join ','
$documentedCounts = @($expectedKinds.Keys | ForEach-Object { $_ + '=' + $expectedKinds[$_] }) -join ','
Assert-Equal -Name 'reviewed copy -> target count per documented kind' -Expected $documentedCounts -Actual $kindCounts
Assert-True -Name 'reviewed copy -> every consumer input is listed as checked' -Condition (
    @($consumerFiles | Where-Object { $_ -cnotin @($reviewedResult.CheckedFiles) }).Count -eq 0)

# ---- Independent enumeration: the CLI targets are exactly the verifier's consumer table.
$table = @(Get-ConsumerTable -Root $reviewedRoot)
$unregistered = @($table | Where-Object { $_.Kind -like 'UNREGISTERED*' })
Assert-True -Name 'reviewed copy -> no hash-like literal outside the documented consumers' `
    -Condition ($unregistered.Count -eq 0) `
    -Detail (@($unregistered | ForEach-Object { $_.File + ':' + $_.Line }) -join ', ')
$keyFormat = '{0}|{1}|{2}|{3}|{4}|{5}'
$tableKeys = @($table | ForEach-Object {
        $keyFormat -f $_.File, $_.Line, $_.Kind, $_.Source, $_.Value, $_.Expected
    } | Sort-Object)
$targetKeys = @($targets | ForEach-Object {
        $keyFormat -f $_.File, $_.Line, $_.Kind, $_.Source, $_.Observed, $_.Expected
    } | Sort-Object)
$missing = @($tableKeys | Where-Object { $_ -cnotin $targetKeys })
$extra = @($targetKeys | Where-Object { $_ -cnotin $tableKeys })
Assert-True -Name 'CLI targets equal the independent table (file, line, kind, source, observed, expected)' `
    -Condition ($tableKeys.Count -eq 116 -and $missing.Count -eq 0 -and $extra.Count -eq 0) `
    -Detail ('table=' + $tableKeys.Count + '; missing=' + ($missing -join ' ; ') + '; extra=' + ($extra -join ' ; '))
$misplaced = @($targets | Where-Object {
        $line = (Read-FixtureText -Path (Join-Path $reviewedRoot $_.File)).Split("`n")[$_.Line - 1]
        -not $line.Contains($_.Observed)
    })
Assert-True -Name 'every target Observed literal is present on its reported File:Line' `
    -Condition ($misplaced.Count -eq 0) `
    -Detail (@($misplaced | ForEach-Object { $_.File + ':' + $_.Line }) -join ', ')

# ---- One drifted consumer of each documented meaning (default and Strict both exit1).
$admissionPath = 'modules/procedures/read_admission.sql'
$driftCases = @(
    @{ Kind = 'ManifestSourceChecksum'; Source = $admissionPath; Chain = 'manifest' },
    @{ Kind = 'ManifestDefinitionChecksum'; Source = $admissionPath; Chain = 'manifest' },
    @{ Kind = 'ManifestDefinitionBytes'; Source = $admissionPath; Chain = 'manifest' },
    @{ Kind = 'CatalogSourceChecksum'; Source = $admissionPath; Chain = 'none' },
    @{ Kind = 'CatalogDefinitionChecksum'; Source = $admissionPath; Chain = 'none' },
    @{ Kind = 'CatalogDefinitionBytes'; Source = $admissionPath; Chain = 'none' },
    @{ Kind = 'CatalogMigrationChecksum'; Source = 'migrations/002_persistence_metadata.sql'; Chain = 'none' },
    @{ Kind = 'CatalogManifestIdentity'; Source = $identitySource; Chain = 'none' },
    @{ Kind = 'ReleaseManifestIdentity'; Source = $identitySource; Chain = 'release' },
    @{ Kind = 'ImmutableMigrationChecksum'; Source = $immutableSource; Chain = 'none' }
)
foreach ($case in $driftCases) {
    $root = New-DatabaseFixture -Name ('drift-' + $case.Kind)
    $consumer = Get-ConsumerRow -Root $root -Kind $case.Kind -Source $case.Source
    if ($case.Kind -cmatch 'Bytes$') {
        $stimulus = [string]([int]$consumer.Value + 2)
    }
    else {
        $stimulus = Get-OtherHex -Value $consumer.Value
    }
    Set-ConsumerLiteral -Root $root -Consumer $consumer -Value $stimulus
    # Keep the stimulus and align only the consumers that hash the edited file (manifest identity or 004).
    if ($case.Chain -ceq 'manifest') {
        Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
    }
    if ($case.Chain -ceq 'release') {
        Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries -PreserveReleaseDeclaration
    }
    $driftCase = @{
        Name = 'single drift ' + $case.Kind
        Root = $root
        Consumer = $consumer
        Observed = $stimulus
        Expected = (Get-ConsumerRow -Root $root -Kind $case.Kind -Source $case.Source).Expected
    }
    Test-SingleDriftCase @driftCase
}

# Text mode shows the counts and the exact replacement for a drift and still exits 1.
$root = New-DatabaseFixture -Name 'drift-text-mode'
$release = Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity'
$staleText = Get-OtherHex -Value $release.Value
Set-ConsumerLiteral -Root $root -Consumer $release -Value $staleText
$text = Invoke-StructureCheck -Root $root -Text
$textOutput = ($text.StdOut + $text.StdErr) -replace '\s+', ''
Assert-True -Name 'text mode drift -> status, counts and the file:line replacement, exit1' -Condition (
    $text.ExitCode -eq 1 -and $textOutput.Contains('Modulestructure:violation.') -and
    $textOutput.Contains('Hashconsumers:116;complete:True;drift:1.') -and
    $textOutput.Contains(('verify-schema.sql:' + $release.Line + ':')) -and
    $textOutput.Contains(("Fix:Replace'" + $staleText + "'with'" + $release.Value + "'"))) `
    -Detail ('exit=' + $text.ExitCode + '; ' + ($text.StdOut + $text.StdErr).Trim())

# ---- SQL-STRUCTURE-06: the catalog release check is a separate consumer of the raw manifest identity.
$staleRelease = 'A8D8DC923CD472F1113DD1A1B1FC92A9A02E4034C5AF20489DEA87867ADE1070'
$root = New-DatabaseFixture -Name 'sql06-stale-release'
$release = Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity'
Set-ConsumerLiteral -Root $root -Consumer $release -Value $staleRelease
$sql06 = @{
    Name = 'SQL-06 stale catalog release literal'
    Root = $root
    Consumer = $release
    Observed = $staleRelease
    Expected = $release.Expected
}
Test-SingleDriftCase @sql06

# Realistic update order after a module edit: nothing, then the manifest entry only, then all but the release check.
$root = New-DatabaseFixture -Name 'sql06-update-order'
$edit = @{
    Path = Join-Path $root $admissionPath
    Find = '    SET NOCOUNT ON;'
    Replace = "    SET NOCOUNT ON;`n    -- reviewed change"
}
Edit-FixtureText @edit
$stage = Invoke-StructureCheck -Root $root
$moduleDrifts = @(
    'CatalogDefinitionBytes', 'CatalogDefinitionChecksum', 'CatalogSourceChecksum',
    'ManifestDefinitionBytes', 'ManifestDefinitionChecksum', 'ManifestSourceChecksum'
) -join ','
Assert-Equal -Name 'module edited only -> six drifts of that module (manifest and catalog source/definition/bytes)' `
    -Expected $moduleDrifts -Actual (Get-DriftKinds -Result $stage.Result)
Assert-Equal -Name 'module edited only -> warning exit' -Expected 1 -Actual $stage.ExitCode
Update-FixtureManifestEntry -Root $root -EntryPath $admissionPath
$stage = Invoke-StructureCheck -Root $root
$entryOnlyDrifts = @(
    'CatalogDefinitionBytes', 'CatalogDefinitionChecksum', 'CatalogManifestIdentity',
    'CatalogSourceChecksum', 'ReleaseManifestIdentity'
) -join ','
Assert-Equal -Name 'manifest entry updated only -> catalog module drifts and both identity consumers' `
    -Expected $entryOnlyDrifts -Actual (Get-DriftKinds -Result $stage.Result)
$oldIdentity = (Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity').Value
Sync-FixtureHashConsumers -Root $root
$release = Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity'
Set-ConsumerLiteral -Root $root -Consumer $release -Value $oldIdentity
$lastStage = @{
    Name = 'everything updated except the catalog release check'
    Root = $root
    Consumer = $release
    Observed = $oldIdentity
    Expected = $release.Expected
}
Test-SingleDriftCase @lastStage

# ---- Inputs, registrations and literal formats that cannot be inspected (unavailable, exit2 in both modes).
$absentCases = @(
    @{ Name = 'catalog verify-schema.sql absent'; File = $catalogFile; Count = 55 },
    @{ Name = 'Module.Common.ps1 absent'; File = $guardFile; Count = 115 },
    @{ Name = 'manifest absent'; File = $manifestFile; Count = 0 },
    @{ Name = '004 declaration absent'; File = $declarationFile; Count = 112 }
)
foreach ($case in $absentCases) {
    $root = New-DatabaseFixture -Name ('absent-' + ($case.File -replace '\W+', '-'))
    Remove-Item -LiteralPath (Join-Path $root $case.File)
    $absentCause = [regex]::Escape(($case.File -split '/')[-1])
    Test-HashUnavailableCase -Name $case.Name -Root $root -MessagePattern $absentCause -File $case.File
    # Targets observed before the error are reported; an incomplete count is never the registered 116.
    Assert-Equal -Name ($case.Name + ' -> targets observed before the error') -Expected $case.Count `
        -Actual $(if ($script:LastHashResult) { $script:LastHashResult.HashTargetCount })
}

# SQL-STRUCTURE-10: a migration that cannot be read is the file to restore, not the catalog that names it.
# The partial target list is reported as observed; the internal order of checks is not part of the contract.
foreach ($migration in @(
        'migrations/001_initial.sql', 'migrations/002_persistence_metadata.sql', 'migrations/003_module_metadata.sql'
    )) {
    $root = New-DatabaseFixture -Name ('absent-' + ($migration -replace '\W+', '-'))
    Remove-Item -LiteralPath (Join-Path $root $migration)
    $absentName = $migration + ' absent'
    $absentCause = [regex]::Escape(($migration -split '/')[-1])
    Test-HashUnavailableCase -Name $absentName -Root $root -MessagePattern $absentCause -File $migration
    $partial = $script:LastHashResult
    Assert-True -Name ($absentName + ' -> partial target list below the registered 116') -Condition (
        $null -ne $partial -and $partial.HashTargetCount -lt 116 -and
        $partial.HashTargetCount -eq @($partial.HashTargets).Count) `
        -Detail $(if ($partial) { 'targets=' + $partial.HashTargetCount })
}

$manifestCause = '^Manifest identity requires UTF-8 without BOM, LF and a final newline\.$'
$root = New-DatabaseFixture -Name 'manifest-bom'
$path = Join-Path $root $manifestFile
[IO.File]::WriteAllBytes($path, [byte[]](@(0xEF, 0xBB, 0xBF) + [IO.File]::ReadAllBytes($path)))
Test-HashUnavailableCase -Name 'manifest with UTF-8 BOM' -Root $root -MessagePattern $manifestCause -File $manifestFile
$root = New-DatabaseFixture -Name 'manifest-crlf'
$path = Join-Path $root $manifestFile
Write-FixtureText -Path $path -Text ((Read-FixtureText -Path $path).Replace("`n", "`r`n"))
Test-HashUnavailableCase -Name 'manifest with CRLF' -Root $root -MessagePattern $manifestCause -File $manifestFile
$root = New-DatabaseFixture -Name 'manifest-no-final-newline'
$path = Join-Path $root $manifestFile
Write-FixtureText -Path $path -Text (Read-FixtureText -Path $path).TrimEnd("`n")
Test-HashUnavailableCase -Name 'manifest without final newline' -Root $root -MessagePattern $manifestCause `
    -File $manifestFile

$registration = '^Manifest hash target registration has missing, duplicate or unknown paths'
$progressPath = 'modules/procedures/internal/serialize_progress.sql'
$root = New-DatabaseFixture -Name 'registration-missing-entry'
$manifest = Read-FixtureManifest -Root $root
$manifest.Entries = @($manifest.Entries | Where-Object Path -CNE $progressPath)
Write-FixtureManifest -Root $root -Manifest $manifest
Test-HashUnavailableCase -Name 'manifest entry missing' -Root $root -MessagePattern $registration -File $manifestFile
$root = New-DatabaseFixture -Name 'registration-duplicate-entry'
$manifest = Read-FixtureManifest -Root $root
$manifest.Entries = @($manifest.Entries) + @($manifest.Entries[1])
Write-FixtureManifest -Root $root -Manifest $manifest
Test-HashUnavailableCase -Name 'manifest entry duplicated' -Root $root -MessagePattern $registration -File $manifestFile
$root = New-DatabaseFixture -Name 'registration-unknown-path'
$manifest = Read-FixtureManifest -Root $root
$progressEntry = @($manifest.Entries | Where-Object Path -CEQ $progressPath)[0]
$progressEntry.Path = 'modules/procedures/internal/serialize_progress_v2.sql'
Write-FixtureManifest -Root $root -Manifest $manifest
Test-HashUnavailableCase -Name 'manifest entry with an unknown path' -Root $root -MessagePattern $registration `
    -File $manifestFile
$root = New-DatabaseFixture -Name 'registration-unknown-object'
$manifest = Read-FixtureManifest -Root $root
$progressEntry = @($manifest.Entries | Where-Object Path -CEQ $progressPath)[0]
$progressEntry.ObjectName = 'dh.SerializeProgressV2'
Write-FixtureManifest -Root $root -Manifest $manifest
Test-HashUnavailableCase -Name 'manifest entry with an unknown object name' -Root $root `
    -MessagePattern '^Unknown manifest object/kind hash registration: ' -File $manifestFile

# Literal formats outside the registered ones; the identity consumers are re-aligned so only the format differs.
$root = New-DatabaseFixture -Name 'format-lowercase-source'
$consumer = Get-ConsumerRow -Root $root -Kind 'ManifestSourceChecksum' -Source $admissionPath
Set-ConsumerLiteral -Root $root -Consumer $consumer -Value $consumer.Value.ToLowerInvariant()
Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
Test-HashUnavailableCase -Name 'manifest SourceChecksum in lowercase' -Root $root `
    -MessagePattern '^Unknown or duplicate SourceChecksum literal: ' -File $manifestFile
$bytesCases = @(
    @{ Name = 'integral value written with a decimal point'; Suffix = '.0' },
    @{ Name = 'fractional value'; Suffix = '.5' }
)
foreach ($case in $bytesCases) {
    $root = New-DatabaseFixture -Name ('format-bytes-' + ($case.Suffix -replace '\W+', ''))
    $consumer = Get-ConsumerRow -Root $root -Kind 'ManifestDefinitionBytes' -Source $admissionPath
    Set-ConsumerLiteral -Root $root -Consumer $consumer -Value ($consumer.Value + $case.Suffix)
    Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
    Test-HashUnavailableCase -Name ('manifest DefinitionBytes ' + $case.Name) -Root $root `
        -MessagePattern 'DefinitionBytes' -File $manifestFile
}

# SQL-STRUCTURE-09: the whole JSON number token must be a canonical nonnegative int32 (MSSQL.md offline check).
# Windows PowerShell 5.1 ConvertFrom-Json reads 012072 and +12072 as Int32, so a typed reader alone cannot reject
# them. The cause must name the whole token from the file, never an adopted integer prefix.
$bytesTokenCases = @(
    @{ Name = 'exponent form'; Token = { param($value) $value.Substring(0, 1) + '.' + $value.Substring(1) +
            'E' + ($value.Length - 1) } },
    @{ Name = 'negative value'; Token = { param($value) '-' + $value } },
    @{ Name = 'string value'; Token = { param($value) '"' + $value + '"' } },
    @{ Name = 'int32 overflow'; Token = { param($value) '2147483648' } },
    @{ Name = 'leading zero'; Token = { param($value) '0' + $value } },
    @{ Name = 'plus sign'; Token = { param($value) '+' + $value } }
)
foreach ($case in $bytesTokenCases) {
    $root = New-DatabaseFixture -Name ('format-bytes-' + ($case.Name -replace '\W+', '-'))
    $consumer = Get-ConsumerRow -Root $root -Kind 'ManifestDefinitionBytes' -Source $admissionPath
    $token = & $case.Token $consumer.Value
    Set-ConsumerLiteral -Root $root -Consumer $consumer -Value $token
    Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
    $unavailableCase = @{
        Name = 'manifest DefinitionBytes ' + $case.Name + ' ' + $token
        Root = $root
        MessagePattern = 'DefinitionBytes.*' + [regex]::Escape($token)
        File = $manifestFile
    }
    Test-HashUnavailableCase @unavailableCase
}

# A canonical int32 token is the registered format: another value is drift and Observed is the whole token.
foreach ($token in @('0', '2147483647')) {
    $root = New-DatabaseFixture -Name ('bytes-canonical-' + $token)
    $consumer = Get-ConsumerRow -Root $root -Kind 'ManifestDefinitionBytes' -Source $admissionPath
    Set-ConsumerLiteral -Root $root -Consumer $consumer -Value $token
    Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
    $canonicalCase = @{
        Name = 'manifest DefinitionBytes canonical ' + $token + ' with another value'
        Root = $root
        Consumer = $consumer
        Observed = $token
        Expected = $consumer.Expected
    }
    Test-SingleDriftCase @canonicalCase
}

# JSON whitespace around a canonical token is layout, not another format; the target still starts at the token.
$root = New-DatabaseFixture -Name 'bytes-whitespace'
$consumer = Get-ConsumerRow -Root $root -Kind 'ManifestDefinitionBytes' -Source $admissionPath
Set-ConsumerLiteral -Root $root -Consumer $consumer -Value ('   ' + $consumer.Value + "`n            ")
Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
$spaced = Invoke-StructureCheck -Root $root -Strict
$spacedTargets = @(if ($spaced.Result) {
        $spaced.Result.HashTargets | Where-Object {
            $_.Kind -ceq 'ManifestDefinitionBytes' -and $_.Source -ceq $admissionPath
        }
    })
$spacedText = Read-FixtureText -Path (Join-Path $root $manifestFile)
Assert-True -Name 'manifest DefinitionBytes with spaces and a newline around the token -> compliant, Strict exit0' `
    -Condition ($spaced.ExitCode -eq 0 -and $null -ne $spaced.Result -and $spaced.Result.Status -ceq 'compliant') `
    -Detail ('exit=' + $spaced.ExitCode + '; ' + (Get-IssueText -Result $spaced.Result))
Assert-True -Name 'manifest DefinitionBytes with surrounding whitespace -> target offset and line at the token' `
    -Condition ($spacedTargets.Count -eq 1 -and $spacedTargets[0].Observed -ceq $consumer.Value -and
    $spacedTargets[0].Line -eq $consumer.Line -and
    $spacedText.Substring($spacedTargets[0].Offset, $consumer.Value.Length) -ceq $consumer.Value) `
    -Detail $(if ($spacedTargets.Count -eq 1) {
        'offset=' + $spacedTargets[0].Offset + '; line=' + $spacedTargets[0].Line
    })

# The permissions entry registers no engine definition; its own field formats stay exact (manifest is the file).
$permissionsCause = '^Permissions entry must not register an engine definition hash\.$'
$permissionsCases = @(
    @{ Name = 'DefinitionBytes written as 0.0'; Find = '"DefinitionBytes": 0,'; Replace = '"DefinitionBytes": 0.0,' },
    @{
        Name = 'DefinitionChecksum registered'
        Find = '"DefinitionChecksum": null'
        Replace = '"DefinitionChecksum": "' + ('C' * 64) + '"'
    }
)
foreach ($case in $permissionsCases) {
    $root = New-DatabaseFixture -Name ('permissions-' + ($case.Name -replace '\W+', '-'))
    $path = Join-Path $root $manifestFile
    if (([regex]::Matches((Read-FixtureText -Path $path), [regex]::Escape($case.Find))).Count -ne 1) {
        throw "Permissions fixture anchor is not unique: $($case.Find)"
    }
    Edit-FixtureText -Path $path -Find $case.Find -Replace $case.Replace
    Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
    Test-HashUnavailableCase -Name ('permissions entry ' + $case.Name) -Root $root `
        -MessagePattern $permissionsCause -File $manifestFile
}

$catalogRow = "    ('SerializeProgress', 'P', "
$rowPattern = "(?m)^    \('SerializeProgress', 'P', \d+, 0x[0-9A-F]{64},\r?\n\s+'[0-9A-F]{64}'\),\r?\n"
$rowCount = '^Expected exactly 18 known @modules rows'
$catalogCases = @(
    @{ Name = 'catalog module row missing'; Cause = $rowCount; Edit = 'remove' },
    @{ Name = 'catalog module row duplicated'; Cause = $rowCount; Edit = 'duplicate' },
    @{
        Name = 'catalog module row with an unknown name'
        Cause = '^Missing, duplicate or unknown catalog module hash registration'
        Edit = 'rename'
    },
    @{ Name = 'catalog module definition hash in lowercase'; Cause = $rowCount; Edit = 'lowercase' },
    @{ Name = 'second catalog module block'; Cause = '^Expected one @modules literal block'; Edit = 'block' }
)
foreach ($case in $catalogCases) {
    $root = New-DatabaseFixture -Name ('catalog-' + $case.Edit)
    $path = Join-Path $root $catalogFile
    $catalog = Read-FixtureText -Path $path
    $row = [regex]::Match($catalog, $rowPattern)
    if (-not $row.Success) { throw 'Catalog fixture row for SerializeProgress not found.' }
    $edited = switch ($case.Edit) {
        'remove' { $catalog.Remove($row.Index, $row.Length) }
        'duplicate' { $catalog.Insert($row.Index, $row.Value) }
        'rename' { $catalog.Replace($catalogRow, "    ('SerializeProgressV2', 'P', ") }
        'lowercase' {
            $lower = [regex]::Replace($row.Value, '0x[0-9A-F]{64}', { param($hex) $hex.Value.ToLowerInvariant() })
            $catalog.Remove($row.Index, $row.Length).Insert($row.Index, $lower)
        }
        'block' { $catalog + "INSERT @modules VALUES`n" + $row.Value.TrimEnd().TrimEnd(',') + ";`n" }
    }
    Write-FixtureText -Path $path -Text $edited
    Test-HashUnavailableCase -Name $case.Name -Root $root -MessagePattern $case.Cause -File $catalogFile
}

$migrationCause = '^Missing, duplicate or unknown catalog migration hash registration\.$'
$migrationCases = @(
    @{
        Name = 'catalog migration row duplicated'
        Find = "(3, '003_module_metadata.sql'"
        Replace = "(2, '002_persistence_metadata.sql'"
    },
    @{
        Name = 'catalog migration row with an unknown name'
        Find = "(2, '002_persistence_metadata.sql'"
        Replace = "(2, '002_persistence_other.sql'"
    }
)
foreach ($case in $migrationCases) {
    $root = New-DatabaseFixture -Name ('migration-' + ($case.Name -replace '\W+', '-'))
    Edit-FixtureText -Path (Join-Path $root $catalogFile) -Find $case.Find -Replace $case.Replace
    Test-HashUnavailableCase -Name $case.Name -Root $root -MessagePattern $migrationCause -File $catalogFile
}

$identityCount = '^Expected one registered manifest identity literal in '
$root = New-DatabaseFixture -Name 'release-literal-absent'
Edit-FixtureText -Path (Join-Path $root $catalogFile) -Find 'r.ManifestChecksum COLLATE Latin1_General_100_BIN2 =' `
    -Replace 'r.ManifestChecksum ='
Test-HashUnavailableCase -Name 'catalog release identity check absent' -Root $root `
    -MessagePattern ($identityCount + 'verify-schema\.sql\.$') -File $catalogFile
$root = New-DatabaseFixture -Name 'release-literal-duplicate'
$release = Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity'
$extraCheck = "IF NOT EXISTS (SELECT 1 FROM dh.ModuleRelease r`n" +
"    WHERE r.ManifestChecksum COLLATE Latin1_General_100_BIN2 = '" + $release.Value + "')`n" +
"    THROW 51010, 'Module release declaration drift.', 1;`n"
Add-FixtureText -Path (Join-Path $root $catalogFile) -Text $extraCheck
Test-HashUnavailableCase -Name 'catalog release identity check duplicated' -Root $root `
    -MessagePattern ($identityCount + 'verify-schema\.sql\.$') -File $catalogFile
$root = New-DatabaseFixture -Name 'declaration-literal-absent'
Edit-FixtureText -Path (Join-Path $root $declarationFile) -Find 'VALUES(4, ' -Replace 'VALUES(5, '
Test-HashUnavailableCase -Name '004 declaration literal absent' -Root $root `
    -MessagePattern ($identityCount + 'migrations/004_module_release\.sql\.$') -File $declarationFile

$guardCause = '^Expected one immutable 001 source checksum guard\.$'
$guardEdit = @{
    Find = '$index -eq 0 -and $hash -cne'
    Replace = '$index -eq 0 -and $hash -ne'
}
$root = New-DatabaseFixture -Name 'guard-absent'
Edit-FixtureText -Path (Join-Path $root $guardFile) @guardEdit
Test-HashUnavailableCase -Name 'immutable 001 guard absent' -Root $root -MessagePattern $guardCause -File $guardFile
$root = New-DatabaseFixture -Name 'guard-duplicate'
$guard = Get-ConsumerRow -Root $root -Kind 'ImmutableMigrationChecksum'
$guardCopy = "# duplicated guard: if (`$index -eq 0 -and `$hash -cne '" + $guard.Value + "') { }`n"
Add-FixtureText -Path (Join-Path $root $guardFile) -Text $guardCopy
Test-HashUnavailableCase -Name 'immutable 001 guard duplicated' -Root $root -MessagePattern $guardCause -File $guardFile

# A drift observed before an inspection error does not turn the result into a violation or a pass.
$root = New-DatabaseFixture -Name 'drift-then-unavailable'
$release = Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity'
Set-ConsumerLiteral -Root $root -Consumer $release -Value (Get-OtherHex -Value $release.Value)
Edit-FixtureText -Path (Join-Path $root $guardFile) @guardEdit
Test-HashUnavailableCase -Name 'drift plus missing 001 guard' -Root $root -MessagePattern $guardCause -File $guardFile
$mixed = $script:LastHashResult
$observedDrift = @(if ($mixed) {
        $mixed.HashTargets | Where-Object { $_.Kind -ceq 'CatalogManifestIdentity' -and $_.IsDrift }
    })
Assert-True -Name 'drift plus missing 001 guard -> partial targets keep the observed drift, count below 116' `
    -Condition ($null -ne $mixed -and $mixed.HashTargetCount -eq 115 -and $observedDrift.Count -eq 1) `
    -Detail $(if ($mixed) { 'targets=' + $mixed.HashTargetCount })

# ---- Structure and hash results stay distinguishable.
$extraModulePath = 'modules/procedures/extra_lookup.sql'
$extraModule = "CREATE OR ALTER PROCEDURE dh.ExtraLookup`nAS`nBEGIN`n    SET NOCOUNT ON;`nEND;`n"
$root = New-DatabaseFixture -Name 'structure-only'
Write-FixtureText -Path (Join-Path $root $extraModulePath) -Text $extraModule
$structure = Invoke-StructureCheck -Root $root
$structureStrict = Invoke-StructureCheck -Root $root -Strict
$structureResult = $structure.Result
Assert-True -Name 'structure-only violation -> complete hash inspection with zero drift' -Condition (
    $null -ne $structureResult -and $structureResult.Status -ceq 'violation' -and
    $structureResult.ViolationCount -eq 1 -and $structureResult.HashViolationCount -eq 0 -and
    $structureResult.HashTargetCount -eq 116 -and $structureResult.HashInspectionComplete -eq $true) `
    -Detail (Get-IssueText -Result $structureResult)
Assert-Equal -Name 'structure-only violation -> warning pilot exit' -Expected 0 -Actual $structure.ExitCode
Assert-Equal -Name 'structure-only violation -> Strict exit' -Expected 1 -Actual $structureStrict.ExitCode

$root = New-DatabaseFixture -Name 'structure-edit-resynced'
$edit = @{
    Path = Join-Path $root $admissionPath
    Find = '        EXEC dh.AssertPersistenceContract'
    Replace = "        EXEC dh.ReadOperationReceipt @OperationId = @OperationId;`n" +
    '        EXEC dh.AssertPersistenceContract'
}
Edit-FixtureText @edit
Sync-FixtureHashConsumers -Root $root
$edited = Invoke-StructureCheck -Root $root
$receiptCall = '^Unexpected helper/RPC call dh\.ReadOperationReceipt$'
$editedIssues = @(if ($edited.Result) { $edited.Result.Issues | Where-Object { $_.Message -cmatch $receiptCall } })
Assert-True -Name 'edited module with an aligned hash chain -> structure issue only, warning exit0' -Condition (
    $edited.ExitCode -eq 0 -and $null -ne $edited.Result -and $edited.Result.Status -ceq 'violation' -and
    $edited.Result.HashViolationCount -eq 0 -and @(Get-DriftIssue -Result $edited.Result).Count -eq 0 -and
    $editedIssues.Count -eq 1) -Detail (Get-IssueText -Result $edited.Result)

$root = New-DatabaseFixture -Name 'structure-and-drift'
Write-FixtureText -Path (Join-Path $root $extraModulePath) -Text $extraModule
$release = Get-ConsumerRow -Root $root -Kind 'CatalogManifestIdentity'
Set-ConsumerLiteral -Root $root -Consumer $release -Value (Get-OtherHex -Value $release.Value)
$both = Invoke-StructureCheck -Root $root
$bothStrict = Invoke-StructureCheck -Root $root -Strict
$bothResult = $both.Result
$placementIssues = @(if ($bothResult) { $bothResult.Issues | Where-Object { $_.File -ceq $extraModulePath } })
Assert-True -Name 'structure violation plus drift -> both issues, two violations, one drift' -Condition (
    $null -ne $bothResult -and $bothResult.Status -ceq 'violation' -and $bothResult.ViolationCount -eq 2 -and
    $bothResult.HashViolationCount -eq 1 -and @(Get-DriftIssue -Result $bothResult).Count -eq 1 -and
    $placementIssues.Count -eq 1) -Detail (Get-IssueText -Result $bothResult)
Assert-Equal -Name 'structure violation plus drift -> warning pilot exit' -Expected 1 -Actual $both.ExitCode
Assert-Equal -Name 'structure violation plus drift -> Strict exit' -Expected 1 -Actual $bothStrict.ExitCode

# ---- Import boundary of ModuleHash.Common.ps1 (Windows PowerShell 5.1 parser and child processes).
$hashCommon = Join-Path $script:ToolRoot 'ModuleHash.Common.ps1'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($hashCommon, [ref]$tokens, [ref]$parseErrors)
$topLevel = @($ast.EndBlock.Statements)
$definitions = @($topLevel | Where-Object { $_ -is [Management.Automation.Language.FunctionDefinitionAst] })
$hashFunctions = @(
    'Get-MigrationText', 'Get-MigrationHash', 'Get-ModuleDefinitionHash',
    'New-ModuleHashTarget', 'Get-ModuleHashRows', 'Test-ModuleHashConsumers'
)
# Reviewed definition set: fix 3 split input reading, occurrence registration and manifest token checks into
# helpers. Callers dot-source this file into their own scope, so every added name is a reviewed import change.
$reviewedHashFunctions = $hashFunctions + @(
    'Read-ModuleHashInputs', 'Test-ModuleHashLiteralRegistration', 'Get-ModuleManifestHashTargets'
)
$sortedHashFunctions = ($reviewedHashFunctions | Sort-Object) -join ','
$definedNames = (@($definitions | ForEach-Object Name) | Sort-Object) -join ','
Assert-True -Name 'ModuleHash.Common.ps1 -> definitions only (nine reviewed functions, no top-level statement)' `
    -Condition (@($parseErrors).Count -eq 0 -and $topLevel.Count -eq $definitions.Count -and
    $definedNames -ceq $sortedHashFunctions) `
    -Detail ((@($topLevel | ForEach-Object { $_.GetType().Name }) -join ',') + '; names=' + $definedNames)
# A reviewed name must not replace a function that another product script defines in the same caller scope.
$productScripts = @(Get-ChildItem -LiteralPath $script:ToolRoot -Filter '*.ps1' -File -Recurse | Where-Object {
        $_.Name -cne 'ModuleHash.Common.ps1' -and
        -not $_.FullName.StartsWith((Join-Path $script:ToolRoot 'tests') + '\')
    })
$callerFunctions = foreach ($productScript in $productScripts) {
    $callerTokens = $null
    $callerErrors = $null
    $callerAst = [Management.Automation.Language.Parser]::ParseFile(
        $productScript.FullName, [ref]$callerTokens, [ref]$callerErrors
    )
    $callerAst.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $true) |
        ForEach-Object Name
}
$collisions = @($reviewedHashFunctions | Where-Object { $_ -in @($callerFunctions) })
Assert-True -Name 'ModuleHash.Common.ps1 functions do not redefine a function of another product script' `
    -Condition ($productScripts.Count -gt 0 -and $collisions.Count -eq 0) `
    -Detail ('scripts=' + $productScripts.Count + '; collisions=' + ($collisions -join ','))
$forbidden = @(
    'Read-ModuleBundle', 'Test-ModuleStructure', 'Invoke-Migrations', 'Invoke-ModuleBundle', 'Invoke-DbScalar',
    'Invoke-DbNonQuery', 'New-DbCommand', 'Open-LocalDatabase', 'Invoke-Sqlcmd', 'sqlcmd', 'Invoke-Expression',
    'Set-Content', 'Add-Content', 'Out-File', 'New-Item', 'Remove-Item', 'Set-Item', 'Copy-Item', 'Move-Item'
)
$commandNodes = $ast.FindAll({ param($node) $node -is [Management.Automation.Language.CommandAst] }, $true)
$commands = @($commandNodes | ForEach-Object { $_.GetCommandName() } | Where-Object { $_ } | Sort-Object -Unique)
$typeNodes = $ast.FindAll({ param($node) $node -is [Management.Automation.Language.TypeExpressionAst] }, $true)
$types = @($typeNodes | ForEach-Object { $_.TypeName.FullName })
# File system calls are limited to static [IO.*] reads (ReadAllBytes/ReadAllText).
$memberNodes = $ast.FindAll({
        param($node) $node -is [Management.Automation.Language.InvokeMemberExpressionAst]
    }, $true)
$writes = @($memberNodes | Where-Object {
        $_.Expression -is [Management.Automation.Language.TypeExpressionAst] -and
        $_.Expression.TypeName.FullName -match '^(System\.)?IO\.' -and $_.Member.Extent.Text -cnotmatch '^Read'
    })
Assert-True -Name 'ModuleHash.Common.ps1 -> no bundle reader, structure CLI, SQL or write command' -Condition (
    @($commands | Where-Object { $_ -in $forbidden }).Count -eq 0 -and
    @($types | Where-Object { $_ -match 'Sql|Data\.' }).Count -eq 0 -and $writes.Count -eq 0) `
    -Detail ('commands=' + ($commands -join ',') + '; types=' + ($types -join ',') + '; writes=' +
    (@($writes | ForEach-Object { $_.Extent.Text }) -join ','))

$sentinels = @('DatabaseRoot', 'Json', 'Strict', 'result', 'targets', 'files', 'manifest', 'catalog', 'values', 'entry')
$sentinelLines = @($sentinels | ForEach-Object { "`$$_ = 'sentinel-$_'" })
# Each element is parenthesized: the array comma binds tighter than string concatenation.
$sentinelList = (@($sentinels | ForEach-Object { "'$_'" })) -join ', '
# Script-defined functions only; module auto-loading (Utility/Management) adds module functions on first use.
$scriptFunctions = '@(Get-ChildItem Function: | Where-Object { -not $_.ModuleName } | ForEach-Object Name)'
$report = @(
    ('$after = @(' + $scriptFunctions + ' | Where-Object { $_ -notin $before } | Sort-Object)'),
    ('$kept = @(@(' + $sentinelList + ') | Where-Object {'),
    '        (Get-Variable -Name $_ -Scope Script -ValueOnly) -ceq (''sentinel-'' + $_) })',
    '[pscustomobject]@{ Added = $after; Kept = $kept.Count } | ConvertTo-Json -Compress'
)
$importCases = @(
    @{ Name = 'ModuleHash.Common.ps1'; Exact = $true },
    @{ Name = 'Module.Common.ps1'; Exact = $false },
    @{ Name = 'Database.Common.ps1'; Exact = $false }
)
foreach ($case in $importCases) {
    $import = ". '" + (Join-Path $script:ToolRoot $case.Name) + "'"
    $lines = @('$before = ' + $scriptFunctions) + $sentinelLines + @($import) + $report
    $probe = Invoke-ChildScript -Name ('import-' + ($case.Name -replace '\W+', '-')) -Lines $lines
    $added = @(if ($probe.Result) { $probe.Result.Added })
    Assert-True -Name ("dot-sourcing $($case.Name) keeps the caller's variables") -Condition (
        $probe.ExitCode -eq 0 -and $null -ne $probe.Result -and $probe.Result.Kept -eq $sentinels.Count) `
        -Detail $probe.Output
    if ($case.Exact) {
        Assert-Equal -Name ("dot-sourcing $($case.Name) adds exactly its nine reviewed hash functions") `
            -Expected $sortedHashFunctions -Actual ($added -join ',')
    }
    else {
        Assert-True -Name ("dot-sourcing $($case.Name) brings the same six hash functions") -Condition (
            @($hashFunctions | Where-Object { $_ -cnotin $added }).Count -eq 0) -Detail ($added -join ',')
    }
}

$fixtureForStub = New-DatabaseFixture -Name 'stubbed-callers'
$stubNames = @(
    'Read-ModuleBundle', 'Test-ModuleStructure', 'Get-ModuleExecutableText',
    'Invoke-DbScalar', 'Invoke-DbNonQuery', 'New-DbCommand', 'Open-LocalDatabase'
)
$stubList = (@($stubNames | ForEach-Object { "'$_'" })) -join ', '
$stubbed = Invoke-ChildScript -Name 'hash-without-bundle-reader' -Lines @(
    'Set-StrictMode -Version Latest',
    '$ErrorActionPreference = ''Stop''',
    (". '" + (Join-Path $script:ToolRoot 'Test-ModuleStructure.ps1') + "'"),
    ('foreach ($name in @(' + $stubList + ')) {'),
    "    Set-Item -Path ('Function:' + `$name) -Value ([scriptblock]::Create(`"throw 'stub `$name called'`"))",
    '}',
    '$contract = @(Get-ModuleStructureContract)',
    ("`$inspection = Test-ModuleHashConsumers -DatabaseRoot '" + $fixtureForStub + "' -Contract `$contract"),
    '[pscustomobject]@{',
    '    Complete = $inspection.Complete',
    '    Targets = @($inspection.Targets).Count',
    '    Issues = @($inspection.Issues).Count',
    '} | ConvertTo-Json -Compress'
)
Assert-True -Name 'hash inspection completes with the bundle reader, structure CLI and SQL entry points stubbed' `
    -Condition ($stubbed.ExitCode -eq 0 -and $null -ne $stubbed.Result -and $stubbed.Result.Complete -eq $true -and
    $stubbed.Result.Targets -eq 116 -and $stubbed.Result.Issues -eq 0) -Detail $stubbed.Output

# Unregistered 64-hex literals (N-14), including the two former scope observations of this suite, are asserted in
# ModuleHashLiterals.Tests.ps1 with the same stimuli.

Complete-TestSuite
