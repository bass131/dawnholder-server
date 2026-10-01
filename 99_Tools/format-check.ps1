[CmdletBinding()]
param(
    [string]$Distribution = 'Ubuntu',
    [string]$EvidenceRoot,
    [string]$WslRoot,
    [string]$StateRoot
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runId = [Guid]::NewGuid().ToString('N')
$taskRoot = if ($StateRoot) { $StateRoot } else { Join-Path ([IO.Path]::GetTempPath()) "dawnholder-format-$runId" }
if (![IO.Path]::IsPathFullyQualified($taskRoot)) { throw 'StateRoot must be an absolute task directory.' }
$taskRoot = [IO.Path]::GetFullPath($taskRoot)
if (Test-Path -LiteralPath $taskRoot) { throw 'Task state path already exists.' }
New-Item -ItemType Directory -Path $taskRoot | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $taskRoot '.dawnholder-format-owner'), "$repo`n$runId`n", $utf8)
$taskLock = [IO.File]::Open((Join-Path $taskRoot '.format.lock'), 'CreateNew', 'ReadWrite', 'None')
$saved = @{}
try {
    $environment = @{
        DOTNET_CLI_HOME="$taskRoot/cli-home"; NUGET_PACKAGES="$taskRoot/nuget/packages";
        NUGET_HTTP_CACHE_PATH="$taskRoot/nuget/http"; NUGET_PLUGINS_CACHE_PATH="$taskRoot/nuget/plugins";
        NUGET_SCRATCH="$taskRoot/nuget/scratch"; DOTNET_GENERATE_ASPNET_CERTIFICATE='false';
        DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='0';
        DOTNET_CLI_TELEMETRY_OPTOUT='1'; MSBUILDDISABLENODEREUSE='1'
    }
    foreach ($name in $environment.Keys) { $saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process'); [Environment]::SetEnvironmentVariable($name,$environment[$name],'Process') }
    if (!$EvidenceRoot) { $EvidenceRoot=Join-Path $taskRoot 'evidence' }
    $EvidenceRoot=[IO.Path]::GetFullPath($EvidenceRoot)
    if (Test-Path -LiteralPath $EvidenceRoot) { throw 'Use a new evidence directory for each run.' }
    New-Item -ItemType Directory -Path $EvidenceRoot | Out-Null
    [IO.File]::WriteAllText("$EvidenceRoot/.dawnholder-format-owner", "$repo`n$runId`n", $utf8)
    $dotnet=(Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
    function RunDotnet($name, [string[]]$arguments, [string]$workingRoot=$repo) {
        $record=[ordered]@{exe=$dotnet;cwd=$workingRoot;args=$arguments;start=(Get-Date -Format o);environment=$environment}
        Push-Location $workingRoot
        try { & $dotnet @arguments *> "$EvidenceRoot/$name.log"; $code=$LASTEXITCODE } finally { Pop-Location }
        $record.exit=$code; $record.end=Get-Date -Format o
        [IO.File]::WriteAllText("$EvidenceRoot/$name.command.json", (ConvertTo-Json -Depth 12 -InputObject $record), $utf8)
        if ($code -ne 0) { throw "$name failed with exit $code; see $EvidenceRoot/$name.log" }
    }
    RunDotnet 'sdk' @('--version')
    $observed=(Get-Content "$EvidenceRoot/sdk.log" | Select-Object -Last 1)
    if ($observed -ne '10.0.301') { throw "Required SDK10.0.301; selected $dotnet; observed $observed." }
    RunDotnet 'tool-restore' @('restore','99_Tools/Formatting/Formatting.csproj')
    RunDotnet 'tool-build' @('build','99_Tools/Formatting/Formatting.csproj','--no-restore','--nologo')
    $cli=Join-Path $repo '99_Tools/Formatting/bin/Debug/net10.0/Formatting.dll'
    $manifest=Join-Path $EvidenceRoot 'input-manifest.json'
    $snapshot=Join-Path $taskRoot 'source'
    New-Item -ItemType Directory -Path $snapshot | Out-Null
    RunDotnet 'snapshot' @($cli,'snapshot','--root',$repo,'--after',$snapshot,'--dotnet',$dotnet,'--out',"$EvidenceRoot/snapshot-copy.json")
    RunDotnet 'snapshot-product-restore' @('restore','Dawnholder.slnx') $snapshot
    RunDotnet 'snapshot-tool-restore' @('restore','99_Tools/Formatting/Formatting.csproj') $snapshot
    if (Test-Path "$snapshot/99_Tools/Formatting.Tests/Formatting.Tests.csproj") { RunDotnet 'snapshot-tests-restore' @('restore','99_Tools/Formatting.Tests/Formatting.Tests.csproj') $snapshot }
    foreach ($configuration in @('Debug','Release')) {
        RunDotnet "snapshot-shared-$configuration" @('build','98_Shared/Shared.csproj','--configuration',$configuration,'--no-restore','--nologo') $snapshot
        RunDotnet "snapshot-clientnet-$configuration" @('build','04_ClientNet/Dawnholder.Client.Net.csproj','--configuration',$configuration,'--no-restore','--nologo') $snapshot
    }
    RunDotnet 'manifest' @($cli,'manifest','--root',$snapshot,'--git-root',$repo,'--dotnet',$dotnet,'--out',$manifest)
    function WslPath([string]$path) {
        $result=& wsl -d $Distribution --exec wslpath -a -u $path.Replace('\','/')
        if ($LASTEXITCODE -ne 0) { throw "Cannot translate WSL path: $path" }
        return ($result | Select-Object -Last 1).Trim()
    }
    $linuxRepo=WslPath $repo
    $linuxManifest=WslPath $manifest
    $linuxEvidence=WslPath $EvidenceRoot
    if (!$WslRoot) { $WslRoot="/home/bass1/.cache/dawnholder/format/$runId" }
    $wslArguments=@('-d',$Distribution,'--exec','env',"DAWNHOLDER_WSL_ROOT=$WslRoot","DAWNHOLDER_FORMAT_MANIFEST=$linuxManifest",'bash',"$linuxRepo/99_Tools/sync-wsl.sh",'sync')
    & wsl @wslArguments *> "$EvidenceRoot/wsl-sync.log"
    $syncExit=$LASTEXITCODE
    [IO.File]::WriteAllText("$EvidenceRoot/wsl-sync.command.json",(ConvertTo-Json -Depth 8 -InputObject @{args=$wslArguments;exit=$syncExit}),$utf8)
    if ($syncExit -ne 0) { throw "WSL sync failed: $EvidenceRoot/wsl-sync.log" }
    $wslArguments=@('-d',$Distribution,'--exec','env',"DAWNHOLDER_FORMAT_STATE_BASE=$WslRoot/.format-states",'bash',"$WslRoot/99_Tools/format-check.sh",'--manifest',"$WslRoot/.dawnholder-format-manifest.json",'--evidence',"$linuxEvidence/wsl-check")
    & wsl @wslArguments *> "$EvidenceRoot/wsl-check.log"
    $checkExit=$LASTEXITCODE
    [IO.File]::WriteAllText("$EvidenceRoot/wsl-check.command.json",(ConvertTo-Json -Depth 8 -InputObject @{args=$wslArguments;exit=$checkExit}),$utf8)
    RunDotnet 'final-validate' @($cli,'validate','--root',$repo,'--dotnet',$dotnet,'--manifest',$manifest,'--git','--files-only')
    if ($checkExit -ne 0) { throw "Formatting check failed: $EvidenceRoot/wsl-check.log" }
    Write-Output "Formatting checks passed. Evidence: $EvidenceRoot"
} finally {
    foreach ($name in $saved.Keys) {
        if ($null -eq $saved[$name]) {
            # A typed null removes the variable; PowerShell's $null becomes an empty string.
            [Environment]::SetEnvironmentVariable($name,[NullString]::Value,'Process')
        } else {
            [Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')
        }
    }
    $taskLock.Dispose()
}
