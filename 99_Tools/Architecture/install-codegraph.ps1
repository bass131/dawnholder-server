$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Architecture.Common.ps1')
$settings = Get-ArchitectureSettings
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$evidenceRoot = Resolve-ArchitectureInputPath -Root $repoRoot -RelativePath $settings.evidencePath
$evidence = Join-Path $evidenceRoot 'install'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
Push-Location $repoRoot
try {
    $command = 'npm install --prefix 99_Tools/Architecture/CodeGraph --save-exact --ignore-scripts --no-audit --no-fund --os=linux --cpu=x64 --bin-links=false --cache 99_Tools/Architecture/CodeGraph/.npm-cache @colbymchenry/codegraph@1.6.1'
    $started = [DateTime]::UtcNow
    $timer = [Diagnostics.Stopwatch]::StartNew()
    npm install --prefix 99_Tools/Architecture/CodeGraph --save-exact --ignore-scripts --no-audit --no-fund --os=linux --cpu=x64 --bin-links=false --cache 99_Tools/Architecture/CodeGraph/.npm-cache @colbymchenry/codegraph@1.6.1 > (Join-Path $evidence 'stdout.txt') 2> (Join-Path $evidence 'stderr.txt')
    $exitCode = $LASTEXITCODE
    $timer.Stop()
    $sizes = @{}
    foreach ($name in @('node_modules', '.npm-cache')) {
        $path = Join-Path $repoRoot "99_Tools/Architecture/CodeGraph/$name"
        $sizes[$name] = (Get-ChildItem -LiteralPath $path -Recurse -File | Measure-Object -Property Length -Sum).Sum
    }
    $record = [ordered]@{
        command = $command
        startedUtc = $started.ToString('o')
        endedUtc = [DateTime]::UtcNow.ToString('o')
        exitCode = $exitCode
        elapsedSeconds = $timer.Elapsed.TotalSeconds
        peakMemoryBytes = $null
        peakMemoryReason = 'Windows npm process tree was not instrumented; N/A'
        sizesBytes = $sizes
        lockSha256 = (Get-FileHash '99_Tools/Architecture/CodeGraph/package-lock.json' -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $record | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $evidence 'measurement.json') -Encoding utf8NoBOM
    Copy-Item -LiteralPath '99_Tools/Architecture/CodeGraph/package-lock.json' -Destination (Join-Path $evidence 'package-lock.json')
    if ($exitCode -ne 0) {
        throw "Approved npm install failed: $exitCode"
    }
    foreach ($name in @('codegraph','codegraph-linux-x64')) {
        $packagePath = "99_Tools/Architecture/CodeGraph/node_modules/@colbymchenry/$name/package.json"
        $package = Get-Content -LiteralPath $packagePath -Raw | ConvertFrom-Json
        Copy-Item -LiteralPath $packagePath -Destination (Join-Path $evidence "$name-package.json")
        if ($package.version -ne $settings.codegraphVersion -or $package.dependencies -or $package.scripts) {
            throw "Installed package differs from approved dependency/script contract: $name"
        }
    }
    if ($sizes['node_modules'] -gt $settings.maxInstalledPackageBytes) {
        throw 'Installed size exceeds approved expectation; ask coordinator.'
    }
    $record | ConvertTo-Json -Depth 8
} finally {
    Pop-Location
}
