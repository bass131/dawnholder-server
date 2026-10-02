# V3 verifier cleanup for a TEMP root this verifier created.
# Deletes only when every guard holds:
#   1. Root is a direct child of the OS TEMP directory named dawnholder-v3-*.
#   2. Root holds .v3-owner.json whose task matches -Task.
#   3. Every -Junction is inside Root and is a reparse point; it is unlinked
#      first (RemoveDirectory on the link, never its target).
#   4. No other reparse point remains under Root (walk never follows links).
# -Sub limits deletion to one relative child of Root (the marker is kept).
param(
  [Parameter(Mandatory = $true)][string]$Root,
  [Parameter(Mandatory = $true)][string]$Task,
  [string[]]$Junction = @(),
  [string]$Sub = ''
)
$ErrorActionPreference = 'Stop'

$temp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\')
$full = [System.IO.Path]::GetFullPath($Root).TrimEnd('\')
if ([System.IO.Path]::GetDirectoryName($full) -ne $temp) { throw "refused: not a direct TEMP child: $full" }
if (-not ((Split-Path -Leaf $full) -like 'dawnholder-v3-*')) { throw "refused: unexpected root name: $full" }
$ownerPath = Join-Path $full '.v3-owner.json'
if (-not (Test-Path -LiteralPath $ownerPath -PathType Leaf)) { throw "refused: owner marker missing: $ownerPath" }
$owner = Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
if ($owner.task -ne $Task) { throw "refused: owner task mismatch" }

foreach ($link in $Junction) {
  $linkFull = [System.IO.Path]::GetFullPath($link).TrimEnd('\')
  if (-not $linkFull.StartsWith($full + '\')) { throw "refused: junction outside root: $linkFull" }
  if (-not (Test-Path -LiteralPath $linkFull)) { continue }
  $info = New-Object System.IO.DirectoryInfo $linkFull
  if (-not ($info.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) { throw "refused: not a reparse point: $linkFull" }
  [System.IO.Directory]::Delete($linkFull, $false)
  if (Test-Path -LiteralPath $linkFull) { throw "junction still present: $linkFull" }
}

$target = $full
if ($Sub) {
  $target = [System.IO.Path]::GetFullPath((Join-Path $full $Sub)).TrimEnd('\')
  if (-not $target.StartsWith($full + '\')) { throw "refused: sub outside root: $target" }
}
if (-not (Test-Path -LiteralPath $target)) { Write-Output "absent: $target"; exit 0 }

# Walk without following reparse points; any leftover link aborts the delete.
$pending = New-Object System.Collections.Stack
$pending.Push((New-Object System.IO.DirectoryInfo $target))
while ($pending.Count -gt 0) {
  $dir = $pending.Pop()
  foreach ($entry in $dir.EnumerateFileSystemInfos()) {
    if ($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) { throw "refused: unexpected reparse point: $($entry.FullName)" }
    if ($entry -is [System.IO.DirectoryInfo]) { $pending.Push($entry) }
  }
}
Remove-Item -LiteralPath $target -Recurse -Force
if (Test-Path -LiteralPath $target) { throw "delete incomplete: $target" }
Write-Output "removed: $target"
