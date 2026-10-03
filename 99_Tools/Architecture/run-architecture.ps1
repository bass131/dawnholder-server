param(
    [string]$Action = 'check',
    [string]$Extractor = 'Roslyn',
    [string]$EvidencePath
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Architecture.Common.ps1')
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$previousOutputEncoding = $OutputEncoding
Push-Location $repoRoot
try {
    $selection = Get-ArchitectureSelection -Extractor $Extractor
    if ($Action -notin @('prepare', 'measure', 'check', 'path')) {
        throw "Unknown action '$Action'. Repair: use -Action path|prepare|measure|check."
    }
    $Action = $Action.ToLowerInvariant()
    $settings = Get-ArchitectureSettings
    if (-not $PSBoundParameters.ContainsKey('EvidencePath')) {
        $EvidencePath = ".backups/architecture/$selection"
    }
    $evidenceParameters = @{
        Root = $repoRoot
        RelativePath = $EvidencePath
        Settings = $settings
    }
    $evidence = Resolve-ArchitectureEvidencePath @evidenceParameters
    $wslArguments = @(
        '-d', 'Ubuntu', '--', 'bash', '99_Tools/Architecture/run-wsl.sh', $Action,
        '--extractor', $selection, '--evidence', $EvidencePath
    )
    if ($Action -in @('path', 'check')) {
        # A path lookup reads no frozen inputs. Check uses the captured batch provenance.
        & wsl @wslArguments
    } else {
        $goal = Resolve-ArchitectureInputPath -Root $repoRoot -RelativePath $settings.goalPath
        $manifestPath = Join-Path $goal 'input-manifest.json'
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $tree = [ordered]@{}
        git -c core.quotepath=false ls-tree -r $manifest.sourceCommit | ForEach-Object {
            if ($_ -match '^\d+ blob ([0-9a-f]{40})\t(.+)$') {
                $tree[$Matches[2]] = $Matches[1]
            }
        }
        if ($LASTEXITCODE -ne 0) {
            throw 'Cannot read frozen Git tree.'
        }
        $paths = @($manifest.files | ForEach-Object { $_.path })
        git diff --quiet $manifest.sourceCommit -- @paths
        if ($LASTEXITCODE -ne 0) {
            throw 'Product/config inputs differ from source SHA.'
        }
        $freezePath = Resolve-ArchitectureInputPath -Root $repoRoot -RelativePath $settings.freezeRecordPath
        $freeze = Get-Content -LiteralPath $freezePath -Raw | ConvertFrom-Json -DateKind String
        $actualCommitTime = git show -s --format=%cI $freeze.commit
        if ($LASTEXITCODE -ne 0 -or $actualCommitTime -ne $freeze.committedAt) {
            throw 'Freeze record commit time does not match Git.'
        }
        foreach ($item in $freeze.files) {
            $frozenPath = Resolve-ArchitectureInputPath -Root $repoRoot -RelativePath $item.path
            if ((Get-FileHash -LiteralPath $frozenPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $item.sha256) {
                throw "Frozen file changed: $($item.path)"
            }
        }
        $implementationHead = git rev-parse HEAD
        if ($LASTEXITCODE -ne 0) {
            throw 'Cannot capture implementation HEAD.'
        }
        $metadata = [ordered]@{
            sourceCommit = $manifest.sourceCommit
            implementationHead = $implementationHead
            capturedUtc = [DateTime]::UtcNow.ToString('o')
            manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
            tree = $tree
            frozenFiles = $freeze.files
            freezeCommit = $freeze.commit
            freezeCommittedUtc = [DateTimeOffset]::Parse($freeze.committedAt).UtcDateTime.ToString('o')
            gitStatus = @(git status --short)
        }
        # WSL stores this capture only after both runtime and evidence locks are held.
        $OutputEncoding = [Text.UTF8Encoding]::new($false)
        $metadata | ConvertTo-Json -Depth 8 | & wsl @wslArguments --metadata-stdin
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Architecture WSL $Action failed: $LASTEXITCODE; evidence: $evidence"
    }
} catch {
    [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
    [Console]::Error.WriteLine(
        'Repair: inspect the selected evidence/command.json and stdout/stderr, preserve installation/cache, ' +
        'correct frozen inputs or use a new unlinked Git-excluded .backups/ root, then rerun ' +
        'pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action prepare -Extractor Roslyn|CodeGraph|Compare.'
    )
    exit 1
} finally {
    $OutputEncoding = $previousOutputEncoding
    Pop-Location
}
