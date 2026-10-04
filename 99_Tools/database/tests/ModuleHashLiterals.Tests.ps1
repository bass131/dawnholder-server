[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Independent offline tests of the product 64-hex literal registration check (N-14) of Test-ModuleStructure.ps1.
# Expected File/Line/Offset values come from the stimulus text this file writes and from its own BOM/CRLF
# normalization (MSSQL.md offline check); ModuleHash.Common.ps1 is never the oracle.
# Every CLI case runs the real entry point in a separate Windows PowerShell process against a fixture copy.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'module-hash-literals'

$checker = Join-Path $script:ToolRoot 'Test-ModuleStructure.ps1'
$inputExtensions = @('.sql', '.ps1', '.psm1', '.psd1', '.json')
$literalPattern = '(?<![0-9A-Fa-f])[0-9A-Fa-f]{64}(?![0-9A-Fa-f])'
$admissionPath = 'modules/procedures/read_admission.sql'
$manifestFile = 'modules/manifest.json'
$catalogFile = 'verify-schema.sql'
$declarationFile = 'migrations/004_module_release.sql'
$guardFile = 'Module.Common.ps1'

function New-StimulusHex {
    param([Parameter(Mandatory)][string]$Seed)
    # A distinct well-formed 64-hex value per case that no product file contains.
    return Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes('module-hash-literals:' + $Seed))
}

function Get-LiteralCoordinateText {
    param([Parameter(Mandatory)][string]$Path)
    # Documented coordinates: decoded text without the UTF-8 BOM; SQL additionally CRLF -> LF.
    $bytes = [IO.File]::ReadAllBytes($Path)
    $start = 0
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { $start = 3 }
    $text = (New-Object Text.UTF8Encoding($false, $true)).GetString($bytes, $start, $bytes.Length - $start)
    if ([IO.Path]::GetExtension($Path) -ieq '.sql') { $text = $text.Replace("`r`n", "`n") }
    return $text
}

function Get-StimulusLocation {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$File,
        [Parameter(Mandatory)][string]$Value,
        [int]$Occurrence = 1
    )
    # Location of the n-th occurrence of a value this suite wrote, in the documented coordinates.
    $text = Get-LiteralCoordinateText -Path (Join-Path $Root $File)
    $index = -1
    for ($count = 0; $count -lt $Occurrence; $count++) {
        $index = $text.IndexOf($Value, $index + 1, [StringComparison]::Ordinal)
        if ($index -lt 0) { throw "Occurrence $Occurrence of the stimulus is not in ${File}: $Value" }
    }
    return [pscustomobject]@{
        File = $File
        Line = 1 + ([regex]::Matches($text.Substring(0, $index), "`n")).Count
        Offset = $index
        Observed = $Value
    }
}

function Format-StimulusLocation {
    param($Location)
    if ($null -eq $Location) { return '<none>' }
    return '{0}:{1}@{2}={3}' -f $Location.File, $Location.Line, $Location.Offset, $Location.Observed
}

function Add-StimulusFile {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$File,
        [Parameter(Mandatory)][string]$Text
    )
    $path = Join-Path $Root $File
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
    Write-FixtureText -Path $path -Text $Text
}

function Add-StimulusText {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Text
    )
    Write-FixtureText -Path $Path -Text ((Read-FixtureText -Path $Path) + $Text)
}

function Edit-UniqueFixtureText {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Find,
        [Parameter(Mandatory)][string]$Replace
    )
    # The stimulus anchor must exist exactly once, so one edit cannot silently become several literals.
    $found = [regex]::Matches((Read-FixtureText -Path $Path), [regex]::Escape($Find)).Count
    if ($found -ne 1) { throw "Stimulus anchor occurs $found times in ${Path}: $Find" }
    Edit-FixtureText -Path $Path -Find $Find -Replace $Replace
}

function Add-LiteralScript {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Seed
    )
    # A new plain product script holding one unregistered literal; returns its expected location.
    $value = New-StimulusHex -Seed $Seed
    Add-StimulusFile -Root $Root -File 'extra.ps1' -Text ("`$previous = '" + $value + "'`n")
    return Get-StimulusLocation -Root $Root -File 'extra.ps1' -Value $value
}

function New-DirectoryJunction {
    param(
        [Parameter(Mandatory)][string]$Link,
        [Parameter(Mandatory)][string]$Target
    )
    # Junctions need no elevation. Callers remove only the link with [IO.Directory]::Delete, never the target.
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Link))
    $output = & cmd.exe /c mklink /J "$Link" "$Target" 2>&1
    if (-not [IO.Directory]::Exists($Link)) { throw "Junction was not created: $Link ($output)" }
}

function Get-FixtureTreeHash {
    param([Parameter(Mandatory)][string]$Root)
    return @(Get-ChildItem -LiteralPath $Root -Recurse -File -Force | Sort-Object FullName | ForEach-Object {
            $_.FullName.Substring($Root.Length) + '=' + (Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($_.FullName)))
        }) -join '|'
}

function Get-UnregisteredIssue {
    param($Result)
    if ($null -eq $Result) { return @() }
    return @($Result.Issues | Where-Object {
            $null -ne $_.PSObject.Properties['Kind'] -and $_.Kind -ceq 'UnregisteredHashLiteral'
        })
}

function Test-UnregisteredLiteralCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)]$Expected
    )
    # One unregistered literal fails both modes after a complete inspection, at its exact location.
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $result = $warning.Result
    $detail = Get-IssueText -Result $result
    $literals = @(if ($result) { $result.HashUnregisteredLiterals })
    $literal = if ($literals.Count -eq 1) { $literals[0] } else { $null }
    $issues = @(Get-UnregisteredIssue -Result $result)
    $drift = @(if ($result) { $result.HashTargets | Where-Object IsDrift })
    Assert-True -Name "$Name -> violation with one unregistered literal after a complete inspection" -Condition (
        $null -ne $result -and $result.Status -ceq 'violation' -and $result.ViolationCount -eq 1 -and
        $result.HashViolationCount -eq 1 -and $result.HashUnregisteredLiteralCount -eq 1 -and
        $result.HashTargetCount -eq 116 -and $result.HashInspectionComplete -eq $true -and
        $result.HashLiteralInspectionComplete -eq $true -and $drift.Count -eq 0) -Detail $detail
    Assert-Equal -Name "$Name -> literal file, line, offset and observed text" `
        -Expected (Format-StimulusLocation -Location $Expected) -Actual (Format-StimulusLocation -Location $literal)
    $place = $Expected.File + ':' + $Expected.Line
    Assert-True -Name "$Name -> issue names the location with registration guidance, no replacement value" `
        -Condition ($issues.Count -eq 1 -and $issues[0].File -ceq $Expected.File -and
        $issues[0].Line -eq $Expected.Line -and $issues[0].Remediation.Contains($place) -and
        $issues[0].Remediation -cmatch 'register' -and $issues[0].Remediation -cnotmatch "Replace '") `
        -Detail $detail
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 1 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 1 -Actual $strict.ExitCode
    $script:LastLiteralRun = $warning
}

function Test-LiteralFreeCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root
    )
    # Nothing to register: compliant in both modes after a complete literal inspection.
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $result = $warning.Result
    Assert-True -Name "$Name -> compliant with zero unregistered literals" -Condition (
        $null -ne $result -and $result.Status -ceq 'compliant' -and $result.HashViolationCount -eq 0 -and
        $result.HashUnregisteredLiteralCount -eq 0 -and $result.HashInspectionComplete -eq $true -and
        $result.HashLiteralInspectionComplete -eq $true) -Detail (Get-IssueText -Result $result)
    Assert-Equal -Name "$Name -> warning exit" -Expected 0 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 0 -Actual $strict.ExitCode
    $script:LastLiteralRun = $warning
}

function Test-LiteralInputUnavailableCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$File,
        [string]$MessagePattern = '.'
    )
    # An input that cannot be read, or only through a link, leaves the check unavailable, never complete or zero.
    $warning = Invoke-StructureCheck -Root $Root
    $strict = Invoke-StructureCheck -Root $Root -Strict
    $result = $warning.Result
    $detail = Get-IssueText -Result $result
    $inputIssues = @(if ($result) {
            $result.Issues | Where-Object {
                $null -ne $_.PSObject.Properties['Kind'] -and $_.Kind -ceq 'HashInspectionUnavailable'
            }
        })
    Assert-True -Name "$Name -> unavailable with null counts and both inspections incomplete" -Condition (
        $null -ne $result -and $result.Status -ceq 'unavailable' -and $null -eq $result.ViolationCount -and
        $null -eq $result.HashViolationCount -and $null -eq $result.HashUnregisteredLiteralCount -and
        $result.HashInspectionComplete -eq $false -and $result.HashLiteralInspectionComplete -eq $false -and
        @($result.HashUnregisteredLiterals).Count -eq 0) -Detail $detail
    Assert-True -Name "$Name -> one input issue names the file to fix" -Condition (
        $inputIssues.Count -eq 1 -and $inputIssues[0].File -ceq $File -and
        $inputIssues[0].Message -cmatch $MessagePattern) -Detail $detail
    Assert-Equal -Name "$Name -> warning pilot exit" -Expected 2 -Actual $warning.ExitCode
    Assert-Equal -Name "$Name -> Strict exit" -Expected 2 -Actual $strict.ExitCode
    $script:LastLiteralRun = $warning
}

function Invoke-SessionRelativeCheck {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][bool]$LiteralInSession
    )
    # Same relative name './db' below the session location and below the child's process directory.
    $sessionTree = New-DatabaseFixture -Name ($Name + '\session\db')
    $processTree = New-DatabaseFixture -Name ($Name + '\process\db')
    $literalTree = if ($LiteralInSession) { $sessionTree } else { $processTree }
    $null = Add-LiteralScript -Root $literalTree -Seed $Name
    $runner = Join-Path $script:SuiteRoot ($Name + '-runner.ps1')
    Write-FixtureText -Path $runner -Text (
        "Set-Location -LiteralPath '" + (Split-Path -Parent $sessionTree) + "'`n" +
        "& '" + $checker + "' -DatabaseRoot './db' -Json -Strict`n" +
        "exit `$LASTEXITCODE`n")
    $run = Invoke-PowerShellFile -File $runner -WorkingDirectory (Split-Path -Parent $processTree)
    $result = if ($run.StdOut.Trim().StartsWith('{')) { $run.StdOut | ConvertFrom-Json } else { $null }
    return [pscustomobject]@{
        ExitCode = $run.ExitCode
        Result = $result
        SessionTree = [IO.Path]::GetFullPath($sessionTree)
    }
}

# ---- Positive controls: the reviewed product tree in place (read-only) and its byte copy.
$real = Invoke-PowerShellFile -File $checker -Arguments @('-Json', '-Strict')
$realResult = if ($real.StdOut.Trim().StartsWith('{')) { $real.StdOut | ConvertFrom-Json } else { $null }
Assert-True -Name 'repository tree -> compliant, literal inspection complete, zero unregistered, Strict exit0' `
    -Condition ($real.ExitCode -eq 0 -and $null -ne $realResult -and $realResult.Status -ceq 'compliant' -and
    $realResult.HashLiteralInspectionComplete -eq $true -and $realResult.HashUnregisteredLiteralCount -eq 0 -and
    @($realResult.HashUnregisteredLiterals).Count -eq 0 -and $realResult.HashViolationCount -eq 0) `
    -Detail ('exit=' + $real.ExitCode + '; ' + (Get-IssueText -Result $realResult))

# The documented scan: every .sql/.ps1/.psm1/.psd1/.json below the root except the root-level tests/ tree.
$productInputs = @(Get-ChildItem -LiteralPath $script:ToolRoot -Recurse -File -Force | Where-Object {
        $_.Extension -in $inputExtensions
    } | ForEach-Object { $_.FullName.Substring($script:ToolRoot.Length + 1).Replace('\', '/') } | Where-Object {
        -not $_.StartsWith('tests/', [StringComparison]::OrdinalIgnoreCase)
    } | Sort-Object)
$realChecked = @(@(if ($realResult) { $realResult.CheckedFiles }) | Sort-Object)
Assert-Equal -Name 'repository tree -> CheckedFiles are exactly the product inputs outside root tests/' `
    -Expected ($productInputs -join '|') -Actual ($realChecked -join '|')

# Every exact 64-hex literal of those inputs is one registered hex target at the same File+Offset.
# 80 = manifest source 19 + definition 18, catalog source 18 + definition 18, migrations 4, identity 2, 001 guard 1.
$occurrences = @(@(foreach ($file in $productInputs) {
            $text = Get-LiteralCoordinateText -Path (Join-Path $script:ToolRoot $file)
            foreach ($match in [regex]::Matches($text, $literalPattern)) {
                '{0}@{1}={2}' -f $file, $match.Index, $match.Value
            }
        }) | Sort-Object)
$registeredHex = @(@(if ($realResult) {
            $realResult.HashTargets | Where-Object { $_.Observed -cmatch '^[0-9A-F]{64}$' } | ForEach-Object {
                '{0}@{1}={2}' -f $_.File, $_.Offset, $_.Observed
            }
        }) | Sort-Object)
Assert-True -Name 'repository tree -> the 80 product 64-hex literals are exactly the registered hex target locations' `
    -Condition ($occurrences.Count -eq 80 -and ($occurrences -join '|') -ceq ($registeredHex -join '|')) `
    -Detail ('literals=' + $occurrences.Count + '; registered=' + $registeredHex.Count)
$misplaced = @(if ($realResult) {
        $realResult.HashTargets | Where-Object {
            $text = Get-LiteralCoordinateText -Path (Join-Path $script:ToolRoot $_.File)
            $_.Offset + $_.Observed.Length -gt $text.Length -or
            $text.Substring($_.Offset, $_.Observed.Length) -cne $_.Observed -or
            (1 + ([regex]::Matches($text.Substring(0, $_.Offset), "`n")).Count) -ne $_.Line
        }
    })
Assert-True -Name 'repository tree -> each of the 116 target offsets starts its observed value on the reported line' `
    -Condition ($null -ne $realResult -and $realResult.HashTargetCount -eq 116 -and $misplaced.Count -eq 0) `
    -Detail (@($misplaced | ForEach-Object { $_.File + ':' + $_.Line + '@' + $_.Offset }) -join ', ')

Test-LiteralFreeCase -Name 'reviewed copy' -Root (New-DatabaseFixture -Name 'reviewed')

# ---- One unregistered literal in each documented input kind and kind of location (fails both modes).
$kindCases = @(
    @{ File = 'extra.sql'; Format = "-- previously reviewed release {0}`nSELECT 1;`n" },
    @{ File = 'extra.ps1'; Format = "`$previous = '{0}'`n" },
    @{ File = 'extra.psm1'; Format = "# {0}`n" },
    @{ File = 'extra.psd1'; Format = "@{{ Hash = '{0}' }}`n" },
    @{ File = 'extra.json'; Format = "{{ `"Hash`": `"{0}`" }}`n" },
    @{ File = 'test-environment/extra.ps1'; Format = "`$previous = '{0}'`n" }
)
foreach ($case in $kindCases) {
    $root = New-DatabaseFixture -Name ('kind-' + ($case.File -replace '\W+', '-'))
    $value = New-StimulusHex -Seed $case.File
    Add-StimulusFile -Root $root -File $case.File -Text ($case.Format -f $value)
    $kindExpected = Get-StimulusLocation -Root $root -File $case.File -Value $value
    Test-UnregisteredLiteralCase -Name ('new product file ' + $case.File) -Root $root -Expected $kindExpected
}

# A comment in a registered module source; the hash chain is re-derived so the literal is the only finding.
$root = New-DatabaseFixture -Name 'module-comment'
$value = New-StimulusHex -Seed 'module-comment'
Edit-UniqueFixtureText -Path (Join-Path $root $admissionPath) -Find '    SET NOCOUNT ON;' `
    -Replace ("    SET NOCOUNT ON;`n    -- previous definition " + $value)
Sync-FixtureHashConsumers -Root $root
Test-UnregisteredLiteralCase -Name 'comment in a registered module source' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File $admissionPath -Value $value)

$root = New-DatabaseFixture -Name 'catalog-string'
$value = New-StimulusHex -Seed 'catalog-string'
Add-StimulusText -Path (Join-Path $root $catalogFile) -Text ("PRINT N'retired release " + $value + "';`n")
Test-UnregisteredLiteralCase -Name 'string literal in the catalog' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File $catalogFile -Value $value)

$root = New-DatabaseFixture -Name 'release-comment'
$value = New-StimulusHex -Seed 'release-comment'
Add-StimulusText -Path (Join-Path $root $declarationFile) -Text ('/* superseded release ' + $value + " */`n")
Sync-FixtureHashConsumers -Root $root
Test-UnregisteredLiteralCase -Name 'block comment in the 004 release migration' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File $declarationFile -Value $value)

# ---- The two former scope observations of ModuleHashConsumers.Tests.ps1 with their original stimuli.
# Historical stale release identity of SQL-STRUCTURE-06 in a catalog check of another form.
$staleRelease = 'A8D8DC923CD472F1113DD1A1B1FC92A9A02E4034C5AF20489DEA87867ADE1070'
$root = New-DatabaseFixture -Name 'n14-catalog-check-other-form'
$staleCheck = "IF NOT EXISTS (SELECT 1 FROM dh.ModuleRelease WHERE ManifestChecksum = '" + $staleRelease + "')`n" +
"    THROW 51010, 'Module release declaration drift.', 1;`n"
Add-StimulusText -Path (Join-Path $root $catalogFile) -Text $staleCheck
Test-UnregisteredLiteralCase -Name 'added catalog check with an unregistered stale identity literal' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File $catalogFile -Value $staleRelease)

$root = New-DatabaseFixture -Name 'n14-manifest-extra-hash-field'
$admissionEntry = @((Read-FixtureManifest -Root $root).Entries | Where-Object Path -CEQ $admissionPath)[0]
$admissionSource = $admissionEntry.SourceChecksum
$extraValue = 'A' * 64
$extraField = '"SourceChecksum": "' + $admissionSource + '",' + "`n" +
'            "PreviousSourceChecksum": "' + $extraValue + '"'
Edit-UniqueFixtureText -Path (Join-Path $root $manifestFile) -Find ('"SourceChecksum": "' + $admissionSource + '"') `
    -Replace $extraField
Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries
Test-UnregisteredLiteralCase -Name 'manifest entry with an extra hash field' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File $manifestFile -Value $extraValue)

# ---- Registration is the consumer location, not the value: copies of registered values are unregistered.
$identity = Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes((Join-Path $script:ToolRoot $manifestFile)))
$guard = (Get-ExpectedModuleValues -Path (Join-Path $script:ToolRoot 'migrations/001_initial.sql')).SourceChecksum
$root = New-DatabaseFixture -Name 'copy-identity-other-line'
Add-StimulusText -Path (Join-Path $root $catalogFile) -Text ('-- reviewed release ' + $identity + "`n")
Test-UnregisteredLiteralCase -Name 'manifest identity copied to another catalog line' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File $catalogFile -Value $identity -Occurrence 2)

$sameLineCases = @(
    @{
        Name = 'catalog release check'
        File = $catalogFile
        Value = $identity
        Find = "'" + $identity + "')"
        Replace = "'" + $identity + "') /* " + $identity + ' */'
    },
    @{
        Name = 'manifest SourceChecksum'
        File = $manifestFile
        Value = $admissionSource
        Find = '"SourceChecksum": "' + $admissionSource + '",'
        Replace = '"SourceChecksum": "' + $admissionSource + '", "SourceCopy": "' + $admissionSource + '",'
    },
    @{
        Name = '001 guard'
        File = $guardFile
        Value = $guard
        Find = "'" + $guard + "') {"
        Replace = "'" + $guard + "') { # " + $guard
    }
)
foreach ($case in $sameLineCases) {
    $root = New-DatabaseFixture -Name ('copy-same-line-' + ($case.Name -replace '\W+', '-'))
    Edit-UniqueFixtureText -Path (Join-Path $root $case.File) -Find $case.Find -Replace $case.Replace
    if ($case.File -ceq $manifestFile) { Sync-FixtureHashConsumers -Root $root -PreserveManifestEntries }
    $registered = Get-StimulusLocation -Root $root -File $case.File -Value $case.Value -Occurrence 1
    $copy = Get-StimulusLocation -Root $root -File $case.File -Value $case.Value -Occurrence 2
    # Stimulus check first: the copy must really share the registered line at another offset.
    Assert-True -Name ($case.Name + ' same-line copy -> stimulus is on the registered line at a later offset') `
        -Condition ($copy.Line -eq $registered.Line -and $copy.Offset -gt $registered.Offset) `
        -Detail ((Format-StimulusLocation -Location $registered) + ' / ' + (Format-StimulusLocation -Location $copy))
    Test-UnregisteredLiteralCase -Name ($case.Name + ' value copied on its own line') -Root $root -Expected $copy
}

$root = New-DatabaseFixture -Name 'copy-identity-other-file'
Add-StimulusFile -Root $root -File 'release-note.json' -Text ("{ `"ReleaseManifest`": `"" + $identity + "`" }`n")
Test-UnregisteredLiteralCase -Name 'manifest identity copied into another product file' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File 'release-note.json' -Value $identity)

# Case-insensitive detection keeps the original text; a SQL binary literal is found after its 0x prefix.
$root = New-DatabaseFixture -Name 'lowercase'
$value = (New-StimulusHex -Seed 'lowercase').ToLowerInvariant()
Add-StimulusFile -Root $root -File 'extra.ps1' -Text ("`$previous = '" + $value + "'`n")
Test-UnregisteredLiteralCase -Name 'lowercase literal' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File 'extra.ps1' -Value $value)
$root = New-DatabaseFixture -Name 'binary-prefix'
$value = New-StimulusHex -Seed 'binary-prefix'
Add-StimulusFile -Root $root -File 'extra.sql' -Text ('SELECT 0x' + $value + " AS PreviousDefinition;`n")
Test-UnregisteredLiteralCase -Name 'SQL binary literal after 0x' -Root $root `
    -Expected (Get-StimulusLocation -Root $root -File 'extra.sql' -Value $value)

# Exactly 64 digits: 63 or 65 adjacent hex digits are another format, not a registration candidate.
$root = New-DatabaseFixture -Name 'near-miss'
$value = New-StimulusHex -Seed 'near-miss'
Add-StimulusFile -Root $root -File 'extra.ps1' `
    -Text ("`$short = '" + $value.Substring(1) + "'`n`$long = '" + $value + "A'`n")
Test-LiteralFreeCase -Name '63 and 65 adjacent hex digits' -Root $root

# ---- Explicit exclusion: only the root-level tests/ tree (msg_e1aa87bb7948), never a namesake elsewhere.
$excludedFormats = @{
    '.ps1' = "# {0}`n"
    '.json' = "{{ `"Hash`": `"{0}`" }}`n"
    '.sql' = "-- {0}`n"
}
$root = New-DatabaseFixture -Name 'excluded-tests'
foreach ($file in @('tests/fixture.ps1', 'tests/data.json', 'tests/nested/sample.sql')) {
    $format = $excludedFormats[[IO.Path]::GetExtension($file)]
    Add-StimulusFile -Root $root -File $file -Text ($format -f (New-StimulusHex -Seed $file))
}
Test-LiteralFreeCase -Name 'literals under the root tests/ tree' -Root $root
$excludedChecked = @(if ($script:LastLiteralRun.Result) { $script:LastLiteralRun.Result.CheckedFiles })
Assert-True -Name 'literals under the root tests/ tree -> no tests/ path is listed as checked' `
    -Condition ($excludedChecked.Count -gt 0 -and
    @($excludedChecked | Where-Object { $_ -like 'tests/*' }).Count -eq 0) `
    -Detail ($excludedChecked -join ',')

$namesakes = @('tests-extra/fixture.ps1', 'test-environment/tests/fixture.ps1', 'modules/tests/data.json', 'tests.ps1')
foreach ($file in $namesakes) {
    $root = New-DatabaseFixture -Name ('namesake-' + ($file -replace '\W+', '-'))
    $value = New-StimulusHex -Seed $file
    $format = $excludedFormats[[IO.Path]::GetExtension($file)]
    Add-StimulusFile -Root $root -File $file -Text ($format -f $value)
    Test-UnregisteredLiteralCase -Name ('outside the root tests/ tree: ' + $file) -Root $root `
        -Expected (Get-StimulusLocation -Root $root -File $file -Value $value)
}

# A root tests/ junction is excluded before any link check: its outside target is neither followed nor read.
# A literal there would fail the check and the exclusively locked file would make any read attempt unavailable.
$root = New-DatabaseFixture -Name 'tests-junction'
$outside = Join-Path $script:SuiteRoot 'tests-junction-target'
Add-StimulusFile -Root $outside -File 'literal.ps1' -Text ('# ' + (New-StimulusHex -Seed 'tests-junction') + "`n")
Add-StimulusFile -Root $outside -File 'locked.ps1' -Text "# held open exclusively while the CLI runs`n"
$outsideBefore = Get-FixtureTreeHash -Root $outside
$testsLink = Join-Path $root 'tests'
New-DirectoryJunction -Link $testsLink -Target $outside
$lock = [IO.File]::Open((Join-Path $outside 'locked.ps1'), [IO.FileMode]::Open, [IO.FileAccess]::Read,
    [IO.FileShare]::None)
try {
    Test-LiteralFreeCase -Name 'root tests/ junction to an outside tree with a literal and a locked file' -Root $root
}
finally {
    $lock.Dispose()
}
[IO.Directory]::Delete($testsLink)
Assert-True -Name 'root tests/ junction -> link removed and the outside target bytes unchanged' -Condition (
    -not [IO.Directory]::Exists($testsLink) -and (Get-FixtureTreeHash -Root $outside) -ceq $outsideBefore)

# ---- A link below the selected root is not followed: unavailable in both modes (parent msg_33ea719b0918).
foreach ($linkFile in @('modules/procedures/linked', 'test-environment/linked')) {
    $root = New-DatabaseFixture -Name ('child-link-' + ($linkFile -replace '\W+', '-'))
    $outside = Join-Path $script:SuiteRoot ('child-link-target-' + ($linkFile -replace '\W+', '-'))
    Add-StimulusFile -Root $outside -File 'extra.sql' -Text ('-- ' + (New-StimulusHex -Seed $linkFile) + "`n")
    $outsideBefore = Get-FixtureTreeHash -Root $outside
    $link = Join-Path $root $linkFile
    New-DirectoryJunction -Link $link -Target $outside
    Test-LiteralInputUnavailableCase -Name ('junction ' + $linkFile) -Root $root -File $linkFile `
        -MessagePattern 'reparse point'
    $linkChecked = @(if ($script:LastLiteralRun.Result) { $script:LastLiteralRun.Result.CheckedFiles })
    [IO.Directory]::Delete($link)
    Assert-True -Name ('junction ' + $linkFile + ' -> no linked file read, link removed, target bytes unchanged') `
        -Condition (@($linkChecked | Where-Object { $_ -like ($linkFile + '/*') }).Count -eq 0 -and
        -not [IO.Directory]::Exists($link) -and (Get-FixtureTreeHash -Root $outside) -ceq $outsideBefore) `
        -Detail ('checked=' + ($linkChecked -join ','))
}

# The selected root itself may be a junction (existing path contract); its literals are still found, relative.
$linkTarget = New-DatabaseFixture -Name 'root-link-target'
$linkExpected = Add-LiteralScript -Root $linkTarget -Seed 'root-link'
$rootLink = Join-Path $script:SuiteRoot 'root-link'
New-DirectoryJunction -Link $rootLink -Target $linkTarget
Test-UnregisteredLiteralCase -Name 'selected root junction with an unregistered literal' -Root $rootLink `
    -Expected $linkExpected
Assert-Equal -Name 'selected root junction -> DatabaseRoot is the junction path' -Expected $rootLink `
    -Actual $(if ($script:LastLiteralRun.Result) { $script:LastLiteralRun.Result.DatabaseRoot })
[IO.Directory]::Delete($rootLink)

# ---- Relative roots: the literal scan follows the session location, not the child's process directory.
$session = Invoke-SessionRelativeCheck -Name 'relative-literal-in-session' -LiteralInSession $true
Assert-True -Name 'relative root, literal in the session tree -> violation Strict exit1 at the session tree' `
    -Condition ($session.ExitCode -eq 1 -and $null -ne $session.Result -and $session.Result.Status -ceq 'violation' -and
    $session.Result.HashUnregisteredLiteralCount -eq 1 -and
    $session.Result.DatabaseRoot -ceq $session.SessionTree) `
    -Detail ('exit=' + $session.ExitCode + '; ' + (Get-IssueText -Result $session.Result))
$process = Invoke-SessionRelativeCheck -Name 'relative-literal-in-process' -LiteralInSession $false
Assert-True -Name 'relative root, literal only in the process directory tree -> compliant Strict exit0' `
    -Condition ($process.ExitCode -eq 0 -and $null -ne $process.Result -and $process.Result.Status -ceq 'compliant' -and
    $process.Result.HashUnregisteredLiteralCount -eq 0 -and
    $process.Result.DatabaseRoot -ceq $process.SessionTree) `
    -Detail ('exit=' + $process.ExitCode + '; ' + (Get-IssueText -Result $process.Result))

# ---- Documented offsets: SQL is CRLF -> LF text; PowerShell/JSON are BOM-free decoded text with CR kept.
$root = New-DatabaseFixture -Name 'offset-crlf-sql'
$value = New-StimulusHex -Seed 'crlf-sql'
Add-StimulusFile -Root $root -File 'extra.sql' -Text ("-- one`r`n-- two`r`nSELECT '" + $value + "';`r`n")
$crlfExpected = Get-StimulusLocation -Root $root -File 'extra.sql' -Value $value
$crlfRaw = Read-FixtureText -Path (Join-Path $root 'extra.sql')
Assert-True -Name 'CRLF SQL stimulus -> written with CRLF; expected offset is the LF index on line 3' -Condition (
    $crlfRaw.Contains("`r`n") -and $crlfExpected.Line -eq 3 -and
    $crlfExpected.Offset -eq $crlfRaw.IndexOf($value, [StringComparison]::Ordinal) - 2)
Test-UnregisteredLiteralCase -Name 'CRLF SQL literal' -Root $root -Expected $crlfExpected

$bomCases = @(
    @{ File = 'extra.ps1'; Format = "# one`r`n# two`r`n`$previous = '{0}'`r`n" },
    @{ File = 'extra.json'; Format = "{{`r`n  `"One`": 1,`r`n  `"Hash`": `"{0}`"`r`n}}`r`n" }
)
foreach ($case in $bomCases) {
    $root = New-DatabaseFixture -Name ('offset-bom-crlf-' + ($case.File -replace '\W+', '-'))
    $value = New-StimulusHex -Seed ('bom-crlf-' + $case.File)
    $decoded = $case.Format -f $value
    $body = (New-Object Text.UTF8Encoding($false)).GetBytes($decoded)
    [IO.File]::WriteAllBytes((Join-Path $root $case.File), [byte[]](@(0xEF, 0xBB, 0xBF) + $body))
    $bomExpected = Get-StimulusLocation -Root $root -File $case.File -Value $value
    Assert-True -Name ('BOM and CRLF ' + $case.File + ' stimulus -> expected offset is the raw index after the BOM') `
        -Condition ($bomExpected.Line -eq 3 -and
        $bomExpected.Offset -eq $decoded.IndexOf($value, [StringComparison]::Ordinal))
    Test-UnregisteredLiteralCase -Name ('BOM and CRLF ' + $case.File + ' literal') -Root $root -Expected $bomExpected
}

# ---- Public counts and precedence with other findings.
$root = New-DatabaseFixture -Name 'mixed-drift'
$otherGuard = $guard.Substring(0, 63) + $(if ($guard.EndsWith('A')) { 'B' } else { 'A' })
$guardDrift = @{
    Path = Join-Path $root $guardFile
    Find = "'" + $guard + "')"
    Replace = "'" + $otherGuard + "')"
}
Edit-UniqueFixtureText @guardDrift
$null = Add-LiteralScript -Root $root -Seed 'mixed-drift'
$warning = Invoke-StructureCheck -Root $root
$strict = Invoke-StructureCheck -Root $root -Strict
$mixed = $warning.Result
Assert-True -Name 'registered drift plus an unregistered literal -> two hash violations, one of each kind' -Condition (
    $null -ne $mixed -and $mixed.Status -ceq 'violation' -and $mixed.ViolationCount -eq 2 -and
    $mixed.HashViolationCount -eq 2 -and $mixed.HashUnregisteredLiteralCount -eq 1 -and
    @($mixed.HashTargets | Where-Object IsDrift).Count -eq 1 -and $mixed.HashTargetCount -eq 116) `
    -Detail (Get-IssueText -Result $mixed)
Assert-Equal -Name 'registered drift plus an unregistered literal -> warning pilot exit' -Expected 1 `
    -Actual $warning.ExitCode
Assert-Equal -Name 'registered drift plus an unregistered literal -> Strict exit' -Expected 1 -Actual $strict.ExitCode

$root = New-DatabaseFixture -Name 'mixed-structure'
Add-StimulusFile -Root $root -File 'modules/procedures/extra_lookup.sql' `
    -Text "CREATE OR ALTER PROCEDURE dh.ExtraLookup`nAS`nBEGIN`n    SET NOCOUNT ON;`nEND;`n"
$null = Add-LiteralScript -Root $root -Seed 'mixed-structure'
$warning = Invoke-StructureCheck -Root $root
$strict = Invoke-StructureCheck -Root $root -Strict
$mixed = $warning.Result
Assert-True -Name 'structure violation plus an unregistered literal -> two violations, one hash violation' -Condition (
    $null -ne $mixed -and $mixed.Status -ceq 'violation' -and $mixed.ViolationCount -eq 2 -and
    $mixed.HashViolationCount -eq 1 -and $mixed.HashUnregisteredLiteralCount -eq 1 -and
    @(Get-UnregisteredIssue -Result $mixed).Count -eq 1) -Detail (Get-IssueText -Result $mixed)
Assert-Equal -Name 'structure violation plus an unregistered literal -> warning pilot exit' -Expected 1 `
    -Actual $warning.ExitCode
Assert-Equal -Name 'structure violation plus an unregistered literal -> Strict exit' -Expected 1 `
    -Actual $strict.ExitCode

$root = New-DatabaseFixture -Name 'mixed-link'
$null = Add-LiteralScript -Root $root -Seed 'mixed-link'
$outside = Join-Path $script:SuiteRoot 'mixed-link-target'
[void][IO.Directory]::CreateDirectory($outside)
$link = Join-Path $root 'test-environment/linked'
New-DirectoryJunction -Link $link -Target $outside
Test-LiteralInputUnavailableCase -Name 'unregistered literal plus a link below the root' -Root $root `
    -File 'test-environment/linked' -MessagePattern 'reparse point'
[IO.Directory]::Delete($link)

$root = New-DatabaseFixture -Name 'mixed-manifest-bom'
$null = Add-LiteralScript -Root $root -Seed 'mixed-manifest-bom'
$path = Join-Path $root $manifestFile
[IO.File]::WriteAllBytes($path, [byte[]](@(0xEF, 0xBB, 0xBF) + [IO.File]::ReadAllBytes($path)))
Test-LiteralInputUnavailableCase -Name 'unregistered literal plus a manifest with BOM' -Root $root -File $manifestFile `
    -MessagePattern '^Manifest identity requires'

# Text mode shows the registered drift count, the product literal counts and the file:line warning; exit1.
$root = New-DatabaseFixture -Name 'text-mode'
$textExpected = Add-LiteralScript -Root $root -Seed 'text-mode'
$text = Invoke-StructureCheck -Root $root -Text
$compact = ($text.StdOut + $text.StdErr) -replace '\s+', ''
Assert-True -Name 'text mode unregistered literal -> status, both count lines and file:line warning, exit1' -Condition (
    $text.ExitCode -eq 1 -and $compact.Contains('Modulestructure:violation.') -and
    $compact.Contains('Hashconsumers:116;complete:True;drift:0.') -and
    $compact.Contains('Producthashliterals:complete:True;unregistered:1;totalhashviolations:1.') -and
    $compact.Contains($textExpected.File + ':' + $textExpected.Line + ':')) `
    -Detail ('exit=' + $text.ExitCode + '; ' + ($text.StdOut + $text.StdErr).Trim())

# One finding is still a JSON array, and the check never rewrites or adopts anything in the inspected tree.
$root = New-DatabaseFixture -Name 'json-shape'
$null = Add-LiteralScript -Root $root -Seed 'json-shape'
$treeBefore = Get-FixtureTreeHash -Root $root
$json = Invoke-StructureCheck -Root $root
Assert-True -Name 'single unregistered literal -> HashUnregisteredLiterals is serialized as a JSON array' `
    -Condition ($json.StdOut -cmatch '"HashUnregisteredLiterals":\s*\[') `
    -Detail ([regex]::Match($json.StdOut, '"HashUnregisteredLiterals":[^\r\n]*').Value)
Assert-Equal -Name 'CLI run leaves every byte of the inspected tree unchanged' -Expected $treeBefore `
    -Actual (Get-FixtureTreeHash -Root $root)

# ---- SQL-STRUCTURE-10 at the input read stage: an unreadable product file is the file to fix (no ACL change).
foreach ($file in @($catalogFile, 'extra.ps1')) {
    $root = New-DatabaseFixture -Name ('locked-' + ($file -replace '\W+', '-'))
    if ($file -ceq 'extra.ps1') { $null = Add-LiteralScript -Root $root -Seed 'locked' }
    $lock = [IO.File]::Open((Join-Path $root $file), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
    try {
        Test-LiteralInputUnavailableCase -Name ('exclusively locked ' + $file) -Root $root -File $file
    }
    finally {
        $lock.Dispose()
    }
}

# ---- The deployment reader runs the same check first and refuses before any SQL (ModuleBundle.Tests pattern).
# Dot-sourcing the product definitions here, last, keeps them from replacing anything used above.
. (Join-Path $script:ToolRoot 'Database.Common.ps1')
Set-OfflineStubs
$root = New-DatabaseFixture -Name 'reader-reviewed'
Assert-NoThrow -Name 'deployment reader accepts the reviewed copy (control)' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }
$root = New-DatabaseFixture -Name 'reader-unregistered'
$null = Add-LiteralScript -Root $root -Seed 'reader-unregistered'
Assert-Throws -Name 'deployment reader refuses a source tree with an unregistered literal' `
    -Pattern '^Module source structure violation: .*UnregisteredHashLiteral' `
    -Action { Read-ModuleBundle -DatabaseRoot $root }
$root = New-DatabaseFixture -Name 'reader-link'
$outside = Join-Path $script:SuiteRoot 'reader-link-target'
[void][IO.Directory]::CreateDirectory($outside)
$link = Join-Path $root 'test-environment/linked'
New-DirectoryJunction -Link $link -Target $outside
Assert-Throws -Name 'deployment reader refuses a source tree with a link below the root' `
    -Pattern '^Module source structure unavailable: .*reparse point' -Action { Read-ModuleBundle -DatabaseRoot $root }
[IO.Directory]::Delete($link)

Complete-TestSuite
