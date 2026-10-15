# Read-only native facts. Raw PerfOS values are bytes, not rounded Task Manager values.
# CIM CreationDate and ParentProcessId define the process identity/ancestry boundary.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$memory = Get-CimInstance -ClassName Win32_PerfRawData_PerfOS_Memory -ErrorAction Stop
if ($null -eq $memory.AvailableBytes -or $null -eq $memory.CommitLimit -or $null -eq $memory.CommittedBytes) {
    throw 'The memory provider did not return AvailableBytes, CommitLimit and CommittedBytes.'
}
$memoryObservedAt = [DateTime]::UtcNow.ToString('o')
$processes = @(Get-CimInstance -ClassName Win32_Process -ErrorAction Stop | ForEach-Object {
    $startedAt = $null
    if ($null -ne $_.CreationDate) {
        $startedAt = $_.CreationDate.ToUniversalTime().ToString('o')
    }
    [ordered]@{
        pid = [long]$_.ProcessId
        parentPid = [long]$_.ParentProcessId
        startedAt = $startedAt
        name = [string]$_.Name
    }
})

$result = [ordered]@{
    observedAt = [DateTime]::UtcNow.ToString('o')
    complete = $true
    memory = [ordered]@{
        physicalAvailableBytes = [long]$memory.AvailableBytes
        commitAvailableBytes = [long]$memory.CommitLimit - [long]$memory.CommittedBytes
    }
    processes = $processes
    provenance = [ordered]@{
        source = 'windows'
        memoryClass = 'Win32_PerfRawData_PerfOS_Memory'
        memoryObservedAt = $memoryObservedAt
        unit = 'bytes'
        availableBytes = [long]$memory.AvailableBytes
        commitLimitBytes = [long]$memory.CommitLimit
        committedBytes = [long]$memory.CommittedBytes
        processClass = 'Win32_Process'
        processIdentity = 'ProcessId, ParentProcessId, CreationDate (UTC)'
    }
}
$result | ConvertTo-Json -Depth 8 -Compress
