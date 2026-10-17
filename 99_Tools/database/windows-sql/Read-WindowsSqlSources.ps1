# The existing module parser's Int32 contract is owned by Windows PowerShell 5.1.
# This child reads only reviewed source files; it never connects to SQL or accepts caller SQL.
param()
if ($MyInvocation.InvocationName -eq '.') { return }
$ErrorActionPreference = 'Stop'
try {
    if ($PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -ne 1) { throw 'version' }
    $databaseRoot = Split-Path -Parent $PSScriptRoot
    . (Join-Path $databaseRoot 'Module.Common.ps1')
    $result = [ordered]@{
        Version = 1
        PowerShellVersion = $PSVersionTable.PSVersion.ToString()
        Sources = @(Get-DatabaseMigrationSources -Phase Complete -DatabaseRoot $databaseRoot)
        Bundle = Read-ModuleBundle -DatabaseRoot $databaseRoot
        Catalog = Get-MigrationText -Path (Join-Path $databaseRoot 'verify-schema.sql')
    }
    $result | ConvertTo-Json -Depth 20 -Compress
    exit 0
}
catch {
    [Console]::Error.WriteLine('WINDOWS_SQL_SOURCE_READER_FAILED')
    exit 1
}
