param([ValidateSet('prepare','measure','check','path')][string]$Action = 'check')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Architecture.Common.ps1')
$settings = Get-ArchitectureSettings
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repoRoot
try {
    $goal = Resolve-ArchitectureInputPath -Root $repoRoot -RelativePath $settings.goalPath
    $manifest = Get-Content "$goal/input-manifest.json" -Raw | ConvertFrom-Json
    $evidence = Resolve-ArchitectureInputPath -Root $repoRoot -RelativePath $settings.evidencePath
    New-Item -ItemType Directory -Force -Path $evidence | Out-Null
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
        if ((Get-FileHash $item.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $item.sha256) {
            throw "Frozen file changed: $($item.path)"
        }
    }
    $metadata = [ordered]@{
        sourceCommit = $manifest.sourceCommit
        implementationHead = (git rev-parse HEAD)
        capturedUtc = [DateTime]::UtcNow.ToString('o')
        manifestHash = (Get-FileHash "$goal/input-manifest.json" -Algorithm SHA256).Hash.ToLowerInvariant()
        tree = $tree
        frozenFiles = $freeze.files
        freezeCommit = $freeze.commit
        freezeCommittedUtc = [DateTimeOffset]::Parse($freeze.committedAt).UtcDateTime.ToString('o')
        gitStatus = @(git status --short)
    }
    $metadata | ConvertTo-Json -Depth 8 | Set-Content "$evidence/source-git.json" -Encoding utf8NoBOM
    wsl -d Ubuntu -- bash 99_Tools/Architecture/run-wsl.sh $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Architecture WSL $Action failed: $LASTEXITCODE"
    }
} finally {
    Pop-Location
}
