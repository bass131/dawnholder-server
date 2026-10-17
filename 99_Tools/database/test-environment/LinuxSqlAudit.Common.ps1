# Closed, bounded inspection of only the stopped owned container's log directory. Never extract the archive.
function Get-LinuxSqlArchiveAudit {
    param([string]$ContainerId, [string[]]$Needles)
    if ($ContainerId -cnotmatch '^[a-f0-9]{64}$') { throw 'LINUX_SQL_AUDIT_ID_INVALID' }
    $archive = Invoke-LinuxSqlProcess -FileName 'docker.exe' -Arguments @(
        'container', 'cp', ($ContainerId + ':/var/opt/mssql/log'), '-') `
        -Seconds 60 -OutputLimit 33554432 -BinaryOutput
    if ($archive.ExitCode -ne 0 -or $archive.Error) { throw 'LINUX_SQL_VOLUME_AUDIT_UNAVAILABLE' }
    [void][Reflection.Assembly]::Load('System.Formats.Tar')
    $bytes = [Text.Encoding]::Latin1.GetBytes($archive.Output)
    $stream = [IO.MemoryStream]::new($bytes, $false)
    $reader = [System.Formats.Tar.TarReader]::new($stream, $false)
    $files = @()
    try {
        while ($null -ne ($entry = $reader.GetNextEntry())) {
            if ($entry.EntryType -eq [System.Formats.Tar.TarEntryType]::Directory) { continue }
            if ($entry.EntryType -notin @([System.Formats.Tar.TarEntryType]::RegularFile,
                    [System.Formats.Tar.TarEntryType]::V7RegularFile) -or
                $entry.Length -gt 8388608 -or $files.Count -ge 256 -or
                $entry.Name -match '(?i)\.(gz|zip|bz2|xz|7z|mdmp|core)$') {
                throw 'LINUX_SQL_VOLUME_AUDIT_UNINSPECTED_ENTRY'
            }
            $content = [byte[]]::new([int]$entry.Length)
            try {
                if ($entry.Length -gt 0) { $entry.DataStream.ReadExactly($content) }
                $ascii = [Text.Encoding]::Latin1.GetString($content)
                $unicode = [Text.Encoding]::Unicode.GetString($content)
                foreach ($needle in $Needles) {
                    if ($needle -and ($ascii.Contains($needle) -or $unicode.Contains($needle))) {
                        throw 'LINUX_SQL_SECRET_IN_VOLUME_LOG'
                    }
                }
                if ($ascii.Contains('PRIVATE KEY-----') -or $unicode.Contains('PRIVATE KEY-----')) {
                    throw 'LINUX_SQL_KEY_IN_VOLUME_LOG'
                }
                # Do not publish an arbitrary archive name; order/hash/length identify the inspected entry.
                $files += @{ Index = $files.Count; Bytes = $entry.Length; Sha256 = Get-LinuxSqlHash -Bytes $content }
            } finally {
                [Array]::Clear($content)
                $ascii = $null
                $unicode = $null
            }
        }
        if ($files.Count -eq 0) { throw 'LINUX_SQL_VOLUME_LOGS_UNOBSERVED' }
        return [pscustomobject]@{ ArchiveBytes = $bytes.Length; Entries = $files }
    } finally {
        $reader.Dispose()
        $stream.Dispose()
        [Array]::Clear($bytes)
        $archive = $null
    }
}
