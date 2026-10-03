[CmdletBinding()]
param(
    [string]$DatabaseRoot,
    [switch]$Json,
    [switch]$Strict
)

function Get-ModuleStructureContract {
    # These are responsibility contracts, not a rule that every RPC calls every helper.
    $shared = @('LockAndReadAuthority', 'AssertPersistenceContract', 'EmitPersistenceResult')
    $game = @('ReadCharacterState', 'SerializeProgress')
    $operation = @('PersistencePayloadV1', 'ReadOperationReceipt', 'RecordOperationReceipt')
    $snapshot = @('SerializePersistenceSnapshot')
    $public = @(
        @{
            Name = 'ReadAdmission'
            File = 'read_admission.sql'
            Calls = $shared
        }
        @{
            Name = 'AcquireAndLoad'
            File = 'acquire_and_load.sql'
            Calls = $shared + $game + $operation + $snapshot
        }
        @{
            Name = 'WriteSafeCheckpoint'
            File = 'write_safe_checkpoint.sql'
            Calls = $shared + $game + $operation + $snapshot
        }
        @{
            Name = 'ReleaseRuntime'
            File = 'release_runtime.sql'
            Calls = $shared + $game + $operation + $snapshot
        }
        @{
            Name = 'ResolveRuntimeOperation'
            File = 'resolve_runtime_operation.sql'
            Calls = $shared + $game + $operation
        }
        @{
            Name = 'InspectRecovery'
            File = 'inspect_recovery.sql'
            Calls = $shared + $game
        }
        @{
            Name = 'RecoverAndLoad'
            File = 'recover_and_load.sql'
            Calls = $shared + $game + $operation + $snapshot
        }
        @{
            Name = 'ReleaseRecovery'
            File = 'release_recovery.sql'
            Calls = $shared + $game + $operation + $snapshot
        }
        @{
            Name = 'ResolveRecoveryOperation'
            File = 'resolve_recovery_operation.sql'
            Calls = $shared + $game + $operation
        }
    )
    foreach ($entry in $public) {
        [pscustomobject]@{
            Name = $entry.Name
            Kind = 'P'
            Path = 'modules/procedures/' + $entry.File
            Public = $true
            RequiredCalls = @($entry.Calls)
        }
    }
    $helpers = [ordered]@{
        LockAndReadAuthority = 'lock_and_read_authority'
        AssertPersistenceContract = 'assert_persistence_contract'
        ReadOperationReceipt = 'read_operation_receipt'
        ReadCharacterState = 'read_character_state'
        SerializeProgress = 'serialize_progress'
        SerializePersistenceSnapshot = 'serialize_persistence_snapshot'
        RecordOperationReceipt = 'record_operation_receipt'
        EmitPersistenceResult = 'emit_persistence_result'
    }
    foreach ($name in $helpers.Keys) {
        [pscustomobject]@{
            Name = $name
            Kind = 'P'
            Path = 'modules/procedures/internal/' + $helpers[$name] + '.sql'
            Public = $false
            RequiredCalls = @()
        }
    }
    [pscustomobject]@{
        Name = 'PersistencePayloadV1'
        Kind = 'FN'
        Path = 'modules/functions/persistence_payload.sql'
        Public = $false
        RequiredCalls = @()
    }
}

function Get-ModuleExecutableText {
    param([string]$Text)
    # Bounded lexical inspection only. Preserve offsets/newlines for diagnostics.
    # Comments and quoted SQL values cannot manufacture direct EXEC calls.
    $chars = $Text.ToCharArray()
    $index = 0
    while ($index -lt $chars.Length) {
        $current = $Text[$index]
        $next = if ($index + 1 -lt $chars.Length) { $Text[$index + 1] } else { [char]0 }
        if ($current -eq '-' -and $next -eq '-') {
            while ($index -lt $chars.Length -and $Text[$index] -ne "`n") {
                $chars[$index] = ' '
                $index++
            }
            continue
        }
        if ($current -eq '/' -and $next -eq '*') {
            $depth = 1
            $chars[$index] = ' '
            $chars[$index + 1] = ' '
            $index += 2
            while ($index -lt $chars.Length -and $depth -gt 0) {
                $next = if ($index + 1 -lt $chars.Length) { $Text[$index + 1] } else { [char]0 }
                if ($Text[$index] -eq '/' -and $next -eq '*') {
                    $depth++
                    $chars[$index] = ' '
                    $chars[$index + 1] = ' '
                    $index += 2
                }
                elseif ($Text[$index] -eq '*' -and $next -eq '/') {
                    $depth--
                    $chars[$index] = ' '
                    $chars[$index + 1] = ' '
                    $index += 2
                }
                else {
                    if ($Text[$index] -ne "`n" -and $Text[$index] -ne "`r") { $chars[$index] = ' ' }
                    $index++
                }
            }
            if ($depth -ne 0) { throw 'Unterminated SQL block comment; lexical inspection is unavailable.' }
            continue
        }
        if ($current -eq "'") {
            $chars[$index] = ' '
            $index++
            $closed = $false
            while ($index -lt $chars.Length) {
                if ($Text[$index] -eq "'") {
                    $chars[$index] = ' '
                    $index++
                    if ($index -lt $chars.Length -and $Text[$index] -eq "'") {
                        $chars[$index] = ' '
                        $index++
                    }
                    else {
                        $closed = $true
                        break
                    }
                }
                else {
                    if ($Text[$index] -ne "`n" -and $Text[$index] -ne "`r") { $chars[$index] = ' ' }
                    $index++
                }
            }
            if (-not $closed) { throw 'Unterminated SQL string; lexical inspection is unavailable.' }
            continue
        }
        if ($current -eq '[' -or $current -eq '"') {
            $closing = if ($current -eq '[') { ']' } else { '"' }
            $chars[$index] = ' '
            $index++
            $closed = $false
            while ($index -lt $chars.Length) {
                if ($Text[$index] -eq $closing) {
                    if ($index + 1 -lt $chars.Length -and $Text[$index + 1] -eq $closing) {
                        # Escaped identifier delimiters cannot be one of the fixed dh object names.
                        $chars[$index] = '_'
                        $chars[$index + 1] = '_'
                        $index += 2
                    }
                    else {
                        $chars[$index] = ' '
                        $index++
                        $closed = $true
                        break
                    }
                }
                else { $index++ }
            }
            if (-not $closed) { throw 'Unterminated SQL identifier; lexical inspection is unavailable.' }
            continue
        }
        $index++
    }
    return -join $chars
}

function Test-ModuleStructure {
    param([string]$DatabaseRoot)
    $issues = New-Object 'Collections.Generic.List[object]'
    $checked = New-Object 'Collections.Generic.List[string]'
    $status = 'compliant'
    $activeFile = $DatabaseRoot
    $resolvedRoot = $null
    try {
        if ([string]::IsNullOrWhiteSpace($DatabaseRoot)) {
            throw 'DatabaseRoot must be a nonempty FileSystem path.'
        }
        # PowerShell's provider location can differ from the process working directory.
        $pathProvider = $null
        $pathDrive = $null
        $providerPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
            $DatabaseRoot, [ref]$pathProvider, [ref]$pathDrive
        )
        if ($pathProvider.Name -cne 'FileSystem') {
            throw 'DatabaseRoot must use the FileSystem provider.'
        }
        $resolvedRoot = [IO.Path]::GetFullPath($providerPath)
        $pathRoot = [IO.Path]::GetPathRoot($resolvedRoot)
        $rootSeparators = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        # Preserve drive/share roots while removing optional separators from deeper paths.
        if ($resolvedRoot.Length -gt $pathRoot.Length) {
            $resolvedRoot = $resolvedRoot.TrimEnd($rootSeparators)
        }
        $rootPrefix = $resolvedRoot
        if (-not $rootPrefix.EndsWith([string][IO.Path]::DirectorySeparatorChar)) {
            $rootPrefix += [IO.Path]::DirectorySeparatorChar
        }
        $modulesPath = Join-Path $resolvedRoot 'modules'
        if (-not [IO.Directory]::Exists($modulesPath)) {
            throw "Module directory cannot be inspected: $modulesPath. Supply the database root containing modules."
        }
        $contract = @(Get-ModuleStructureContract)
        $byPath = @{}
        foreach ($entry in $contract) { $byPath[$entry.Path] = $entry }
        $seen = @{}
        $files = @(Get-ChildItem -LiteralPath $modulesPath -Filter '*.sql' -File -Recurse)
        $migrationsPath = Join-Path $resolvedRoot 'migrations'
        if ([IO.Directory]::Exists($migrationsPath)) {
            $files += @(Get-ChildItem -LiteralPath $migrationsPath -Filter '*.sql' -File)
        }
        $files = @($files | Sort-Object FullName)
        foreach ($file in $files) {
            $activeFile = $file.FullName
            $relative = $file.FullName.Substring($rootPrefix.Length).Replace('\', '/')
            $checked.Add($relative)
            if ($relative -ceq 'modules/permissions.sql') { continue }
            $text = [IO.File]::ReadAllText($file.FullName)
            $executable = Get-ModuleExecutableText -Text $text
            $definitions = [regex]::Matches(
                $executable, '(?i)\bCREATE\s+(?:OR\s+ALTER\s+)?(PROCEDURE|PROC|FUNCTION)\s+dh\s*\.\s*(\w+)'
            )
            # Historical DDL files are not current modules. A procedure moved back there is a violation.
            if ($relative.StartsWith('migrations/') -and $definitions.Count -eq 0) { continue }
            $entry = $byPath[$relative]
            $line = 1
            if ($definitions.Count -eq 1) {
                $line = 1 + ([regex]::Matches($text.Substring(0, $definitions[0].Index), "`n")).Count
            }
            if ($null -eq $entry -or $relative -cne $entry.Path -or $definitions.Count -ne 1) {
                $knownObject = @(
                    if ($definitions.Count -eq 1) {
                        $contract | Where-Object Name -ieq $definitions[0].Groups[2].Value
                    }
                )
                $expectedLocation = if ($knownObject.Count -eq 1) {
                    'dh.' + $knownObject[0].Name + ' at ' + $knownObject[0].Path
                } else { 'One registered dh module at its exact contract path' }
                $issues.Add([pscustomobject]@{
                    File = $relative
                    Line = $line
                    Expected = $expectedLocation
                    Message = 'Unknown path or non-single module definition'
                    Remediation = 'Move the known object to its modules/functions or procedures contract path; remove unregistered SQL.'
                })
                continue
            }
            $actualName = $definitions[0].Groups[2].Value
            $actualKind = if ($definitions[0].Groups[1].Value -ieq 'FUNCTION') { 'FN' } else { 'P' }
            if ($actualName -cne $entry.Name -or $actualKind -cne $entry.Kind) {
                $issues.Add([pscustomobject]@{
                    File = $relative
                    Line = $line
                    Expected = ('dh.' + $entry.Name + ' (' + $entry.Kind + ') at ' + $entry.Path)
                    Message = ('Unexpected module dh.' + $actualName)
                    Remediation = 'Restore the expected definition/name/kind or move it to its registered path.'
                })
                continue
            }
            $seen[$entry.Name] = $true
            if (-not $entry.Public) { continue }
            $calls = [regex]::Matches(
                $executable, '(?i)\bEXEC(?:UTE)?\s+(?:@\w+\s*=\s*)?dh\s*\.\s*(\w+)\b'
            )
            $callNames = @($calls | ForEach-Object { $_.Groups[1].Value })
            foreach ($required in $entry.RequiredCalls) {
                if ($required -notin $callNames) {
                    $issues.Add([pscustomobject]@{
                        File = $relative
                        Line = $line
                        Expected = ('Direct EXEC dh.' + $required + ' in ' + $entry.Path)
                        Message = ('dh.' + $entry.Name + ' does not call its ' + $required + ' responsibility')
                        Remediation = ('Use the authoritative internal helper dh.' + $required + ' with named arguments at the original stage.')
                    })
                }
            }
            foreach ($call in $calls) {
                $called = $call.Groups[1].Value
                if ($called -notin $entry.RequiredCalls) {
                    $callLine = 1 + ([regex]::Matches($text.Substring(0, $call.Index), "`n")).Count
                    $issues.Add([pscustomobject]@{
                        File = $relative
                        Line = $callLine
                        Expected = ($entry.RequiredCalls -join ', ')
                        Message = ('Unexpected helper/RPC call dh.' + $called)
                        Remediation = 'Remove the added responsibility; preserve admission/inspection/resolver exceptions in the fixed contract.'
                    })
                }
            }
        }
        foreach ($entry in $contract) {
            if (-not $seen.ContainsKey($entry.Name)) {
                $issues.Add([pscustomobject]@{
                    File = $entry.Path
                    Line = 1
                    Expected = ('dh.' + $entry.Name + ' at ' + $entry.Path)
                    Message = 'Required module missing or registered under the wrong definition'
                    Remediation = ('Restore the authoritative ' + $entry.Path + ' file and its exact dh.' + $entry.Name + ' definition.')
                })
            }
        }
        if ($issues.Count -gt 0) { $status = 'violation' }
    }
    catch {
        $status = 'unavailable'
        $issues.Add([pscustomobject]@{
            File = $activeFile
            Line = $null
            Expected = 'Readable, lexically inspectable module sources'
            Message = $_.Exception.Message
            Remediation = 'Correct the supplied database path/access or unfinished SQL text, then rerun; unavailable is not compliant.'
        })
    }
    [pscustomobject]@{
        Status = $status
        DatabaseRoot = $resolvedRoot
        Scope = 'Source placement and direct calls only; SQL grammar, indentation and behavior are not verified.'
        CheckedFiles = @($checked.ToArray())
        Issues = @($issues.ToArray())
        ViolationCount = if ($status -eq 'unavailable') { $null } else { $issues.Count }
    }
}

if ($MyInvocation.InvocationName -eq '.') { return }
if (-not $PSBoundParameters.ContainsKey('DatabaseRoot')) { $DatabaseRoot = $PSScriptRoot }
$result = Test-ModuleStructure -DatabaseRoot $DatabaseRoot
if ($Json) { $result | ConvertTo-Json -Depth 6 }
else {
    Write-Output ("Module structure: {0}. DatabaseRoot: {1}. {2}" -f
        $result.Status, $result.DatabaseRoot, $result.Scope)
    foreach ($issue in $result.Issues) {
        Write-Warning ("{0}:{1}: {2}. Expected: {3}. Fix: {4}" -f
            $issue.File, $issue.Line, $issue.Message, $issue.Expected, $issue.Remediation)
    }
}
# Warning pilot: violations remain machine-visible even when exit0. Unavailable is always exit2.
if ($result.Status -eq 'unavailable') { exit 2 }
if ($Strict -and $result.Status -eq 'violation') { exit 1 }
exit 0
