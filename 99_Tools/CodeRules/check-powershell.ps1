param(
    [Parameter(Mandatory = $true)]
    [string]$InputManifest,
    [Parameter(Mandatory = $true)]
    [string]$ModuleManifest,
    [Parameter(Mandatory = $true)]
    [string]$SettingsPath,
    [Parameter(Mandatory = $true)]
    [string]$RequiredVersion
)

$ErrorActionPreference = 'Stop'
$rulesResult = @{
    version = $null
    runtime = $PSVersionTable.PSVersion.ToString()
    files = @()
    errors = @()
}

try {
    # Import only the approved analyzer manifest; inspected scripts are never imported.
    $rulesModule = Import-Module -Name $ModuleManifest -PassThru -Force
    if ($rulesModule.Version.ToString() -ne $RequiredVersion) {
        throw "PSScriptAnalyzer version mismatch: $($rulesModule.Version), expected $RequiredVersion"
    }
    $rulesResult.version = $rulesModule.Version.ToString()
    $rulesSettings = Import-PowerShellDataFile -LiteralPath $SettingsPath
    $rulesInputs = Get-Content -LiteralPath $InputManifest -Raw | ConvertFrom-Json
    foreach ($rulesInput in $rulesInputs) {
        $rulesFile = @{
            path = $rulesInput.path
            sha256 = $null
            diagnostics = @()
            error = $null
        }
        try {
            $rulesBytes = [System.IO.File]::ReadAllBytes($rulesInput.absolutePath)
            $rulesFile.sha256 = [Convert]::ToHexString(
                [System.Security.Cryptography.SHA256]::HashData($rulesBytes)
            ).ToLowerInvariant()
            if ($rulesFile.sha256 -ne $rulesInput.sha256) {
                throw "Input changed before analysis: $($rulesInput.path)"
            }
            $rulesTokens = $null
            $rulesParseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseFile(
                $rulesInput.absolutePath,
                [ref]$rulesTokens,
                [ref]$rulesParseErrors
            )
            foreach ($rulesIssue in $rulesParseErrors) {
                $rulesFile.diagnostics += @{
                    path = $rulesInput.path
                    line = $rulesIssue.Extent.StartLineNumber
                    column = $rulesIssue.Extent.StartColumnNumber
                    rule = 'PowerShellParse'
                    message = $rulesIssue.Message
                }
            }
            if ($rulesParseErrors.Count -eq 0) {
                $rulesSource = [System.IO.File]::ReadAllText($rulesInput.absolutePath)
                # ScriptDefinition isolates settings; IncludeSuppressed keeps source attributes visible.
                $rulesIssues = Invoke-ScriptAnalyzer -ScriptDefinition $rulesSource `
                    -Settings $rulesSettings -IncludeSuppressed
                foreach ($rulesIssue in $rulesIssues) {
                    $rulesFile.diagnostics += @{
                        path = $rulesInput.path
                        line = $rulesIssue.Line
                        column = $rulesIssue.Column
                        rule = $rulesIssue.RuleName
                        message = $rulesIssue.Message
                        suppressedInSource = $rulesIssue.IsSuppressed
                    }
                }
            }
        } catch {
            $rulesFile.error = $_.Exception.Message
        }
        $rulesResult.files += $rulesFile
    }
} catch {
    $rulesResult.errors += $_.Exception.Message
}

$rulesResult | ConvertTo-Json -Depth 12 -Compress
$rulesFailedFiles = @($rulesResult.files | Where-Object { $_.error -or $_.diagnostics.Count -gt 0 })
if ($rulesResult.errors.Count -gt 0 -or $rulesFailedFiles.Count -gt 0) {
    exit 1
}
exit 0
