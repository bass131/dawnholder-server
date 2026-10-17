# PR2 Preflight 전 버전 두 원천 조회

노트북 담당자가 **노트북에서만**, 아래 전체 명령과 정확한 4개 입력을 사용자에게 보여 주고 읽기 전용 조회 승인을 받은 뒤 실행한다. 이 문서 작성과 오프라인 검사는 실행 승인이 아니다. 데스크톱 SQL·보호 자원에는 실행하지 않는다.

기준은 같은 발행 commit의 [README](README.md) §1~2와 `WindowsSql.Sql.ps1`의 로컬 관리 경계다. 실행 전 발행자가 고정한 이 카드의 SHA256과 실제 파일 hash가 같은지 확인하고, 다르거나 승인한 발행본을 확인할 수 없으면 중단한다. 기존 ForceEncryption은 바꾸지 않는다. 이 조회는 TLS 검증이나 PR2 전체 성공을 뜻하지 않는다.

PowerShell 7의 Windows 터미널에서 첫 4개 문자열만 승인값으로 바꾸고 **블록 전체를 한 번** 붙여 넣는다. machine은 실제 로컬 이름과 대소문자까지 일치해야 한다. instance는 기본 인스턴스가 아닌 명명 인스턴스이며, InstanceId는 제품과 같은 `MSSQL<숫자 2자리>.<정확 instance>` 형식이다. 출력은 비밀 없는 근거를 둘 **이미 존재하는 로컬 절대 디렉터리**이고, 그 조상에도 junction/symlink가 없어야 한다. 입력값에 비밀을 넣지 않는다. 입력 외 다른 줄은 바꾸지 않는다.

명령은 새 자식 `pwsh -NoProfile -NonInteractive`에서 실행되므로 현재 터미널을 종료하지 않는다. 자식이 정상적으로 시작·종료한 경우 마지막 `PR2_VERSION_CHECK_EXIT=...`와 `$LASTEXITCODE`에 자식 종료 코드가 표시된다. 자식 시작 실패 때는 이전 값이 남을 수 있으므로 이 줄만으로 성공을 판정하지 않는다. 출력 JSON의 경로도 표시한다. 아래 블록에는 SQL 접속이 있으므로 데스크톱에서 블록 전체·내부 루틴을 시험 실행하지 않는다.

```powershell
$approvedMachineName = '<승인한 정확 로컬 machine>'
$approvedInstanceName = '<승인한 정확 명명 instance>'
$approvedInstanceId = '<승인한 정확 InstanceId>'
$approvedOutputDirectory = '<승인한 기존 공개 출력 폴더 절대 경로>'

$versionProbe = {
    param(
        [string]$MachineName,
        [string]$InstanceName,
        [string]$InstanceId,
        [string]$OutputDirectory
    )

    Set-StrictMode -Version Latest
    $ErrorActionPreference = 'Stop'
    $exitCode = 1
    $failureCode = 'INPUT_INVALID'
    $outputPath = $null
    $baseKey = $null
    $setupKey = $null
    $connection = $null
    $command = $null
    $reader = $null
    $query = @'
SELECT
    CONVERT(nvarchar(128), CONNECTIONPROPERTY('net_transport')) AS NetTransport,
    CONVERT(nvarchar(128), SERVERPROPERTY('MachineName')) AS MachineName,
    CONVERT(nvarchar(128), SERVERPROPERTY('InstanceName')) AS InstanceName,
    IS_SRVROLEMEMBER(N'sysadmin') AS IsSysadmin,
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion;
'@
    $result = [ordered]@{
        SchemaVersion = 1
        StartedUtc = [DateTime]::UtcNow.ToString('o')
        CompletedUtc = $null
        Status = 'Failed'
        ExitCode = 1
        ErrorCode = $null
        Requested = [ordered]@{
            MachineName = $MachineName
            InstanceName = $InstanceName
            InstanceId = $InstanceId
            OutputDirectory = $OutputDirectory
        }
        Registry = [ordered]@{
            Hive = 'HKEY_LOCAL_MACHINE'
            View = 'Registry64'
            SubKey = $null
            ValueName = 'PatchLevel'
            MetadataValueName = 'Version'
            Attempted = $false
            ObservedUtc = $null
            PatchLevel = $null
            Version = $null
        }
        Sql = [ordered]@{
            Target = $null
            InitialCatalog = 'master'
            IntegratedSecurity = $true
            Encrypt = $false
            TrustServerCertificate = $false
            ConnectTimeoutSeconds = 5
            CommandTimeoutSeconds = 5
            Query = $query
            Attempted = $false
            ObservedUtc = $null
            NetTransport = $null
            MachineName = $null
            InstanceName = $null
            IsSysadmin = $null
            ProductVersion = $null
        }
    }

    try {
        $failureCode = 'OUTPUT_DIRECTORY_INVALID'
        if ($OutputDirectory -notmatch '\A[A-Za-z]:\\' -or
            $OutputDirectory.Substring(2).Contains(':', [StringComparison]::Ordinal)) {
            throw 'Output directory rejected.'
        }
        $directory = [IO.DirectoryInfo]::new([IO.Path]::GetFullPath($OutputDirectory))
        if (-not $directory.Exists) {
            throw 'Output directory rejected.'
        }
        for ($ancestor = $directory; $null -ne $ancestor; $ancestor = $ancestor.Parent) {
            if (($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Output directory rejected.'
            }
        }
        $fileTimestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')
        $fileName = 'pr2-version-check-' + $fileTimestamp + '-' + [Guid]::NewGuid().ToString('N') + '.json'
        $outputPath = [IO.Path]::Combine($directory.FullName, $fileName)

        # Only the approved local named instance is addressable; no aliases or fallback.
        $failureCode = 'INPUT_INVALID'
        if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -ne 7 -or
            $MachineName -cnotmatch '\A[A-Za-z0-9][A-Za-z0-9-]{0,14}\z' -or
            $MachineName -cne [Environment]::MachineName -or
            $InstanceName -cnotmatch '\A[A-Za-z][A-Za-z0-9_]{0,15}\z' -or
            $InstanceName -ieq 'MSSQLSERVER') {
            throw 'Input rejected.'
        }
        $instanceIdPattern = '\AMSSQL[0-9]{2}\.' + [regex]::Escape($InstanceName) + '\z'
        if ($InstanceId -cnotmatch $instanceIdPattern) {
            throw 'Input rejected.'
        }
        $result.Registry.SubKey = 'SOFTWARE\Microsoft\Microsoft SQL Server\' + $InstanceId + '\Setup'
        $result.Sql.Target = 'lpc:' + $MachineName + '\' + $InstanceName

        $failureCode = 'REGISTRY_VERSION_UNAVAILABLE'
        $result.Registry.Attempted = $true
        $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey(
            [Microsoft.Win32.RegistryHive]::LocalMachine,
            [Microsoft.Win32.RegistryView]::Registry64)
        $setupKey = $baseKey.OpenSubKey($result.Registry.SubKey, $false)
        if ($null -eq $setupKey) {
            throw 'Version unavailable.'
        }
        # Setup/Version is installation metadata, never an engine-version fallback.
        $result.Registry.Version = $setupKey.GetValue(
            'Version', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $registryVersion = $setupKey.GetValue(
            'PatchLevel', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $result.Registry.PatchLevel = $registryVersion
        $result.Registry.ObservedUtc = [DateTime]::UtcNow.ToString('o')
        if ($registryVersion -isnot [string] -or $registryVersion.Length -gt 64 -or
            $registryVersion -cnotmatch '\A(?:0|[1-9][0-9]*)(?:\.(?:0|[1-9][0-9]*)){3}\z') {
            throw 'Version unavailable.'
        }

        $failureCode = 'SQL_OBSERVATION_FAILED'
        # Use key indexers, matching the repaired repository adapter.
        $builder = [Data.SqlClient.SqlConnectionStringBuilder]::new()
        $builder['Data Source'] = 'lpc:' + $MachineName + '\' + $InstanceName
        $builder['Initial Catalog'] = 'master'
        $builder['Integrated Security'] = $true
        $builder['Encrypt'] = $false
        $builder['TrustServerCertificate'] = $false
        $builder['Pooling'] = $false
        $builder['Persist Security Info'] = $false
        $builder['Connect Timeout'] = 5
        $builder['ConnectRetryCount'] = 0
        $builder['Application Name'] = 'Dawnholder.WindowsSql.VersionCheck'
        $connection = [Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
        $result.Sql.Attempted = $true
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 5
        $command.CommandText = $query
        $reader = $command.ExecuteReader([Data.CommandBehavior]::SingleRow)
        if (-not $reader.Read()) {
            throw 'Observation unavailable.'
        }
        $fields = @('NetTransport', 'MachineName', 'InstanceName', 'IsSysadmin', 'ProductVersion')
        for ($index = 0; $index -lt $fields.Count; $index++) {
            if (-not $reader.IsDBNull($index)) {
                $result.Sql[$fields[$index]] = $reader.GetValue($index)
            }
        }
        $result.Sql.ObservedUtc = [DateTime]::UtcNow.ToString('o')

        # Identity, transport, privilege and version were all read on this one connection.
        $failureCode = 'SQL_BOUNDARY_MISMATCH'
        if ($result.Sql.NetTransport -cne 'Shared memory' -or
            $result.Sql.MachineName -cne $MachineName -or
            $result.Sql.InstanceName -cne $InstanceName -or
            $null -eq $result.Sql.IsSysadmin -or $result.Sql.IsSysadmin -ne 1) {
            throw 'Boundary mismatch.'
        }
        $failureCode = 'SQL_VERSION_UNAVAILABLE'
        if ($result.Sql.ProductVersion -isnot [string] -or
            $result.Sql.ProductVersion.Length -gt 64 -or
            $result.Sql.ProductVersion -cnotmatch '\A(?:0|[1-9][0-9]*)(?:\.(?:0|[1-9][0-9]*)){3}\z') {
            throw 'Version unavailable.'
        }
        $failureCode = 'VERSION_MISMATCH'
        if ($registryVersion -cne $result.Sql.ProductVersion) {
            throw 'Version mismatch.'
        }
        $exitCode = 0
        $result.Status = 'Matched'
    }
    catch {
        # Exception messages and connection strings may contain sensitive details.
        $result.ErrorCode = $failureCode
        $exitCode = 1
    }
    finally {
        foreach ($resource in @($reader, $command, $connection, $setupKey, $baseKey)) {
            if ($null -ne $resource) {
                try {
                    $resource.Dispose()
                }
                catch {
                    $result.ErrorCode = 'RESOURCE_DISPOSE_FAILED'
                    $exitCode = 1
                }
            }
        }
    }

    if ($exitCode -ne 0) {
        $result.Status = 'Failed'
    }
    $result.ExitCode = $exitCode
    $result.CompletedUtc = [DateTime]::UtcNow.ToString('o')
    try {
        if ($null -eq $outputPath) {
            throw 'No approved output path.'
        }
        $json = $result | ConvertTo-Json -Depth 6
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($json + [Environment]::NewLine)
        # CreateNew is exclusive: never overwrite any existing evidence file.
        $stream = [IO.File]::Open($outputPath, [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write, [IO.FileShare]::None)
        try {
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush($true)
        }
        finally {
            $stream.Dispose()
        }
        [Console]::WriteLine('PR2_VERSION_CHECK_JSON=' + $outputPath)
        [Console]::WriteLine('PR2_VERSION_CHECK_STATUS=' + $result.Status)
    }
    catch {
        [Console]::Error.WriteLine('PR2_VERSION_CHECK_OUTPUT_FAILED; no complete JSON is assured.')
        exit 2
    }
    exit $exitCode
}

# Serialize data separately so quotes in the output path cannot become executable code.
$probeInputs = @{
    MachineName = $approvedMachineName
    InstanceName = $approvedInstanceName
    InstanceId = $approvedInstanceId
    OutputDirectory = $approvedOutputDirectory
} | ConvertTo-Json -Compress
$encodedInputs = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($probeInputs))
$childCommand = '$probeInputs = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('''
$childCommand += $encodedInputs + ''')) | ConvertFrom-Json -AsHashtable; & {'
$childCommand += $versionProbe.ToString() + '} @probeInputs'
$encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($childCommand))
$childPwsh = Join-Path $PSHOME 'pwsh.exe'
& $childPwsh -NoLogo -NoProfile -NonInteractive -EncodedCommand $encodedCommand
$probeProcessExit = $LASTEXITCODE
Write-Output ('PR2_VERSION_CHECK_EXIT=' + $probeProcessExit)
```

**해당 자식의 실제 `exit 0`과 해당 실행에서 새로 만든 JSON의 `Status=Matched`, `ExitCode=0`을 모두 확인한 경우에만** 일치한 엔진 버전을 다음 계획의 후보로 사용한다. JSON의 Requested 네 입력과 StartedUtc/CompletedUtc가 이번 실행인지 확인한다. 부모 PowerShell exit0·EXIT 한 줄·이전 Matched JSON은 성공 근거가 아니다. 실행 담당자는 **수정한 명령 전체·4개 입력·stdout/stderr·실제 exit·새 JSON**을 함께 보존하고 `facts.json`의 승인 절차로 전달한다. JSON에는 정확한 registry 대상/64비트 뷰, 엔진 원천 `PatchLevel`, 설치 메타데이터 `Version`, SQL 대상/상수 조회문과 `ProductVersion`, 같은 연결의 transport/identity/sysadmin, UTC와 exit가 들어 있다. 엔진 버전은 공백·불필요한 앞자리 0이 없는 네 부분 문자열이어야 하며 두 원천의 정확 일치를 요구한다. `Version`과 SQL 버전이 달라도 `PatchLevel`이 유효하고 SQL과 같으면 일치다. `PatchLevel` 부재·잘못된 형식·불일치에서 `Version`으로 대체하지 않는다. 이 조회에는 `ExpectedProductVersion` 입력이 없다. 관측된 일치 버전을 이후 plan의 `ExpectedProductVersion`으로 넣고 사용자에게 전체 plan/hash 승인을 받아야 한다.

`exit 1`은 조회 불가·빈 값·대상/권한/전송 방식 위반·버전 불일치·자원 정리 실패다. 실패한 단계 이후에는 조회를 계속하지 않아 SQL 값이 null/Attempted=false일 수 있다. `exit 2`는 입력/출력 경로 문제 또는 JSON 기록 실패로, 완전한 JSON을 보장하지 않는다. 자식 실행 자체 실패나 exit를 관측하지 못한 경우도 중단한다. 부분 JSON을 성공 근거로 쓰거나 기존 파일을 덮어 재시도하지 않는다. wrapper가 자식 실패를 부모 종료 코드에 전파한다고 보장하지 않으므로 실제 자식 exit를 따로 보존한다.

**nonzero·불일치·미관측이면 다음 Preflight로 진행하지 않고 담당 리드에게 원시를 전달한다.** 한쪽 버전을 선택하거나 제품 검사를 낮추지 않는다. registry/SQL 쓰기, 기존 DB 목록·내용 조회, TCP/Named Pipes 재접속, SQL 인증, ForceEncryption 변경, 서비스·WSL 실행은 이 명령에 없다. 연결과 명령 timeout은 각각 5초, 연결 재시도는 0이다. 출력 파일 I/O까지 포함한 전체 실행 시간 상한을 보장하는 명령은 아니다.
