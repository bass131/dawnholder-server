# Windows SQL Express 실행 카드

이미 설치된 **정확한 Windows 명명 인스턴스**에 새 시연 DB와 runtime login만 준비한다.
PR1의 오프라인 검사·빌드 결과는 노트북 실행 성공이 아니다. 아래 실제 단계는 사용자가 노트북으로
옮긴 뒤 정확한 도구 commit, 계획 hash, 관리자 실행과 중단 시간을 승인한 PR2에서 수행한다.
데스크톱 Windows SQL Express와 보호 컨테이너에는 이 카드를 실행하지 않는다.

진입점은 `Invoke-WindowsSqlEnvironment.ps1`이다. `WindowsSql.Common.ps1`은 순수 판단과 수명,
`WindowsSql.Adapter.ps1`은 실제 연산의 닫힌 목록, `Instance/Sql/Certificate/Verify`는 해당 I/O를 소유한다.
`WindowsSql.CertificateKey.ps1`은 Prepare와 Certificate(Verify·Instance·Remove에서 사용)의
키 취득·지원 조건·정확 키 파일·서비스 ACL 판정을 함께 소유한다.
`Runtime/Store/PathLease`는 공개 입력과 파일·잠금을 담당한다. dot-source는 정의만 읽는다.
CLI에는 adapter, SQL, script, 관측값, 머신 우회, 비밀, TLS 완화 인자가 없다.

## 1. 실행 전에 확정할 사실과 승인

노트북 담당자는 다음 값을 비밀 없는 `facts.json`에 기록하고 사용자에게 보여 준다.
계획에 적힌 값을 이 데스크톱에서 추정하거나 승인 정보로 승격하지 않는다.

| 항목 | 필요한 관측/승인 |
|---|---|
| 실행 위치 | 노트북의 실제 MachineName, 실행 사용자 SID, 관리자 토큰, checkout 절대 경로와 정확 commit |
| 설치 | 정확 InstanceName/InstanceId/ProductVersion, 서비스 실행 계정과 Running 상태 |
| 의존 서비스 | 정확 SQL 인스턴스의 의존 서비스 전체가 없거나 모두 `Stopped`이면서 시작 유형 `Disabled`인지 확인 |
| 기존 자원 | 신규 DB `Dawnholder_...`, 신규 login `dh_...`, 신규 lifetime 32자리 소문자 hex |
| 보호 기준 | 사용자/노트북 담당자가 고른 기존 백업·메타데이터 기준과 전후 비교 방법. 기존 DB 목록/내용 조회를 추가하지 않음 |
| 설정 | 기존 LoginMode/TCP/고정·동적 포트/cert/ForceEncryption, 충돌 없는 신규 포트 |
| 네트워크 | 현재 NAT 또는 mirrored 유지, Windows endpoint IPv4, WSL에서 그 주소로 해석되는 TLS DNS 이름 |
| 중단 | Apply 설정 변경 후 서비스 재시작 1회. 같은 인스턴스의 다른 연결도 끊김. Cleanup 복원에도 재시작이 필요할 수 있음 |
| 인증서 | 별도 Prepare 단계의 새 인증서·개인키 서비스 읽기 권한 부여 승인. OS 전역 Root 저장소 변경 없음 |
| 저장 identity | 기존 서버 규칙으로 정한 persistedAccountName/AccountId, 새 CharacterId, slot 1 |
| 출력 | SourceRoot 아래 공개 파일/근거, 현재 사용자 LOCALAPPDATA 아래 전용 PrivateRoot |

인증서 키 검사는 Windows의 `crypt32`/CAPI 및 해당 SChannel CSP에 접근할 수 있는 실행 권한이 필요하다.
지원 키는 인증서의 `CERT_KEY_PROV_INFO`에 **Microsoft RSA SChannel Cryptographic Provider**,
`PROV_RSA_SCHANNEL`(12), 머신 키, `AT_KEYEXCHANGE`(1)가 기록된 기존 컨테이너의
**비내보내기 RSA 2048비트 이상** 개인키다. 관리 코드에서 `RSACng` 또는
`RSACryptoServiceProvider`로 보이는지만으로 지원 여부를 판정하지 않는다.
CNG 전용 KSP·사용자 키·서명 전용/내보내기 가능 키와 증명할 수 없는 메타데이터는 거부한다.
X509 KeyUsage의 KeyEncipherment나 CNG KeyUsage는 KeySpec의 대체 근거가 아니다.

도구는 인증서의 공급자/컨테이너/KeySpec으로 **기존 키만** 열고, CAPI에서 인증서 공개키와 연결된 키를
대조한다. 키 생성·내보내기·누락된 연결 복구·다른 컨테이너 검색은 하지 않는다. 키 파일은 CSP가 반환한
고유 컨테이너 이름 한 개로 `CommonApplicationData\Microsoft\Crypto\RSA\MachineKeys` 바로 아래의
실제 파일을 확인한다. 빈 이름·경로 이탈·대체 데이터 스트림·폴더·reparse/junction은 거부한다.
정확 서비스 SID의 Allow Read가 필요하며 Deny·읽기 부족은 거부한다(Prepare는 검증 뒤 Read를 부여).

이 버전은 `ListenOnAllIPs=1`, 가상 서비스 계정 `NT Service\MSSQL$<InstanceName>`,
IPv4 NAT의 실제 route subnet `/20..32` 또는 mirrored의 `127.0.0.1`을 지원한다.
이 범위 밖의 설정·별도 서비스 계정·관측 불가 상태는 중단한다.
NAT `/20..32`는 도구 지원 제한이며 WSL의 보편적 subnet 규칙이 아니다.
LAN 전체 범위, WSL 모드 강제 변경, 인스턴스 업그레이드·재설치, sa 활성화는 하지 않는다.

**의존 서비스 경계:** Preflight/Plan/Verify/Cleanup의 관측, Apply 첫 쓰기(설정 snapshot·journal·registry·DB 변경)
전 검사와 도중 각 변경 직전 검사(mutation guard)에 같은 조건을 적용한다. 전체 의존 서비스를 관측하여
빈 집합 또는 모든 서비스가 `Stopped` + `Disabled`일 때만 허용하며,
서비스 이름에 따른 예외는 없다. Running·전환 중·Paused, Manual·Automatic, 값 누락·관측 실패는 중단한다.
Apply와 Cleanup의 재시작 직전에도 새로 관측하고 조건이 달라졌으면 Stop 호출 전에 거부한다. 도구는 의존 서비스의
강제·묵시적 정지, 삭제, 시작 유형 변경을 하지 않는다. SQL 서비스 정지는 `Stop(false)`를 사용해
검사 뒤 의존 서비스가 활성화되면 SCM이 정지를 거부하게 한다. 관측과 정지는 원자적이지 않으므로
검사 뒤 시작 유형·의존 관계 변경까지 잠그는 것은 아니다. 승인한 중단 시간 동안 다른 관리 작업을 피한다.

의존 서비스 조건 위반·관측 실패의 공개 오류 코드는 다음과 같다. 다른 원인에도 같은 코드가 쓰이므로
이 코드만으로 원인을 의존 서비스로 단정하지 않는다.

| 단계 | 의존 서비스 조건 확인 시점 | 공개 오류 코드 |
|---|---|---|
| Preflight / Plan / Verify | 각 단계의 관측 | `WINDOWS_SQL_ADAPTER_FAILED` |
| Apply | 첫 쓰기 전·도중 각 변경 직전·재시작 직전 | `WINDOWS_SQL_APPLY_FAILED` |
| Cleanup | 단계 관측·각 변경 직전·복원에 따른 재시작 직전 | `WINDOWS_SQL_CLEANUP_FAILED` |

MachineName에는 영문/숫자/하이픈 1~15자만 허용한다. 밑줄 등이 있는 머신은 v1에서 거부한다.
제품 경로·모든 입력·PrivateRoot의 조상에 junction/symlink/reparse point가 있으면 중단한다.
기존 `.backups`가 junction이면 옮기거나 지우지 말고 실행 배치를 다시 검토한다.

필요 프로그램: Windows PowerShell **5.1**(공용 module reader), Windows PowerShell **7.6.x**
(PR1 자체점검은 **7.6.6**, 노트북의 7.6.7은 별도 관측), Windows PKI/NetSecurity/NetTCPIP/CimCmdlets,
WSL2 Ubuntu의 `wslinfo`, `ip`, Bash/Python3/rsync/flock/timeout/coreutils,
고정 .NET SDK **10.0.301**, Microsoft.Data.SqlClient **6.1.7**, Pkcs **9.0.21**이다.
SQL Server 2025 Express의 정확 ProductVersion은 현장 관측값으로 계획에 고정한다.
과거 보고의 `17.0.1000.7`을 실제 확인 없이 채우지 않는다. 다른 버전·도구 설치는 별도 판단한다.

**PR2 Preflight 전 버전 대조:** 도구는 `ExpectedProductVersion` 하나를 승인 인스턴스의
64비트 레지스트리 `SOFTWARE\Microsoft\Microsoft SQL Server\<InstanceId>\Setup`의 `PatchLevel`과
같은 LPC 연결의 SQL `SERVERPROPERTY('ProductVersion')` 양쪽에 비교한다. `Setup/Version`은 패치 뒤에도
최초 설치 값을 유지할 수 있는 설치 메타데이터이며 엔진 버전 비교나 fallback에 쓰지 않는다.
`PatchLevel`은 문자열인 정규 네 부분 버전이어야 한다. 부재·빈 값·공백·불필요한 앞자리 0·잘못된 형식·
승인값 불일치는 중단한다. 정확 InstanceName→InstanceId 매핑 검사와 plan v1 schema는 유지한다.

조회는 [버전 조회 카드](version-check.md)의 승인한 발행본/hash로 수행한다. 입력은 정확한
machine/instance/InstanceId/출력 폴더 네 개이며 승인 버전을 다섯째 입력으로 넣지 않는다.
카드는 `PatchLevel`과 SQL 버전을 현장에서 조사하여 유효 형식과 정확 일치를 확인하고,
`Setup/Version`은 별도 설치 메타데이터로 기록한다. §2의 LPC/Windows 통합 인증과 같은 연결의
Shared memory·machine/instance·sysadmin 경계를 유지하며 기존 DB 목록/내용은 읽지 않는다.

**해당 자식의 실제 exit0와 해당 실행에서 새로 생성된 JSON의 `Status=Matched`·`ExitCode=0`을 모두
확인**한 뒤 일치한 엔진 버전을 계획의 `ExpectedProductVersion`에 넣고 전체 plan/hash 승인을 받는다.
부모 PowerShell의 exit0·`PR2_VERSION_CHECK_EXIT` 한 줄·과거 Matched JSON만으로 성공을 판정하지 않는다.
기존 wrapper는 자식 시작 실패 때 이전 `$LASTEXITCODE`를 표시할 수 있고 자식 실패가 부모 exit에
반영된다고 보장하지 않는다. 시작 실패·실제 child exit 미관측·새 JSON 부재·불일치면 중단한다.
명령 전체·네 입력·대상·UTC·stdout/stderr·child exit·새 JSON을 보존하고 담당 리드에게 전달한다.

버전 오류로 중단한 실행은 수정 commit과 새로 검토·발행한 카드/hash로 재개한다. 기존 USB 카드와
과거 결과는 보존하고, 수정본의 일치한 관측값을 이후 사용자 plan/hash 승인 없이 적용하지 않는다.
오프라인 대역 시험은 실제 노트북 조회·SQL/TLS·PR2 실증 성공을 뜻하지 않는다.

## 2. 두 연결 경계를 구분한다

관리 연결은 `lpc:<MachineName>\<InstanceName>` + Windows 통합 인증이며
`Encrypt=false`, `TrustServerCertificate=false`다. 같은 연결에서
`CONNECTIONPROPERTY('net_transport') = 'Shared memory'`와 서버 identity·관리자 권한을 확인한다.
TCP/Named Pipes/SQL 인증 fallback은 없다. 원래 ForceEncryption을 낮추지 않으며 이것 때문에 실패하면 중단한다.
관리 연결로 한 일을 TLS 검증 성공으로 보고하지 않는다.

WSL Verify와 실제 서버 profile은 `tcp:<TlsServerName>,<Port>`를 사용한다.
DNS 이름은 승인 endpoint IPv4로 해석되어야 하며 인증서 SAN과 일치해야 한다.
`Encrypt=Mandatory`, `TrustServerCertificate=false`를 고정하고, 공개 PEM은 자식의
`SSL_CERT_FILE`/빈 `SSL_CERT_DIR`로만 전달한다. OS 전역 신뢰 저장소·전역 환경은 변경하지 않는다.
mirrored는 `localhost` SAN과 127.0.0.1을 사용할 수 있다. NAT는 실행 직전 route와 DNS를 다시 확인한다.
도구는 hosts/DNS 설정을 고치거나 인증서 이름 검사를 우회하지 않는다.

근거: [Microsoft 인증서 요구사항](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/certificate-requirements?view=sql-server-ver17),
[Microsoft WSL 네트워킹](https://learn.microsoft.com/en-us/windows/wsl/networking),
[Microsoft 인증 모드 변경](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/change-server-authentication-mode?view=sql-server-ver17).
인증서의 유효기간, SAN, Server Authentication EKU, RSA 2048, CSP의 AT_KEYEXCHANGE,
비내보내기 개인키와 정확 서비스 SID 읽기 권한을 검사한다. 실제 SQL 로딩은 PR2에서 확인한다.

## 3. 검사 host만 준비한다

기존 `DEVELOPMENT.md`의 원본별 WSL clone/소유 marker/lock을 사용한다. 게임 서버 빌드·실행은
이 단계에 포함되지 않는다. 실행 직전 물리 여유 6GiB·commit 여유 4GiB, Unity 최대 1개,
다른 소유자의 실행과 clone lock 충돌이 없는지 확인하고 Core WSL 창을 알린다.
확인할 수 없거나 충돌하면 중단한다. 다른 프로세스를 종료하지 않는다.

다음은 WSL Ubuntu에서 실행하는 공개 명령이다. `SOURCE`는 실제 Windows checkout의 WSL 경로,
`EVIDENCE`는 그 checkout 아래 승인한 공개 근거 디렉터리로 먼저 지정한다.
두 경로에는 비밀이 없으며 `.backups` 등 링크가 없는지 확인한다.

```bash
set -euo pipefail
bash "$SOURCE/99_Tools/sync-wsl.sh" sync
root=$(bash "$SOURCE/99_Tools/sync-wsl.sh" path)
test "$(cat "$root/.dawnholder-source")" = "$SOURCE"
key=$(printf '%s' "$root" | sha256sum | cut -c1-20)
exec 8>"$HOME/.cache/dawnholder/locks/workspace-$key.lock"
flock -n 8
state=$(mktemp -d "$root/.dotnet-state-windows-sql-XXXXXXXX")
printf '%s\n' "$SOURCE" > "$state/.dawnholder-source"
export DOTNET_CLI_HOME="$state/cli-home" NUGET_PACKAGES="$state/nuget/packages"
export NUGET_HTTP_CACHE_PATH="$state/nuget/http" NUGET_PLUGINS_CACHE_PATH="$state/nuget/plugins"
export NUGET_SCRATCH="$state/nuget/scratch" TMPDIR="$state/tmp"
export DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0 DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1
mkdir -p "$TMPDIR"
fail() { printf '%s\n' "$*" >&2; exit 1; }
source "$root/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$root"
cd "$root"
project=99_Tools/database/windows-sql/WindowsSqlVerifyHost/WindowsSqlVerifyHost.csproj
"$DOTNET" restore "$project" --nologo 8>&-
"$DOTNET" build "$project" --no-restore --nologo -p:UseSharedCompilation=false 8>&-
python3 - "$root/99_Tools/database/windows-sql/WindowsSqlVerifyHost/bin/Debug/net10.0" "$EVIDENCE/host-files.json" <<'PY'
import hashlib,json,pathlib,sys
root=pathlib.Path(sys.argv[1])
files=[{'Path':p.relative_to(root).as_posix(),'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()}
       for p in sorted(root.rglob('*')) if p.is_file()]
pathlib.Path(sys.argv[2]).write_text(json.dumps(files,indent=2),encoding='utf-8')
PY
```

stdout/stderr/exit, 실제 SDK·해결된 패키지·메모리 관측을 기록한다.
빌드 실패나 의존성 경고는 담당자에게 반환한다. `host-files.json`의 전체 파일 hash를
runtime에 넣는다. Verify는 누락·추가 파일·링크·hash 차이를 거부한다.
이 단계는 SQL에 연결하지 않는다.

## 4. 별도 인증서 Prepare

다음 모든 예시의 `<...>`는 실제 승인값으로 바꿔야 한다. 예시는 실행 승인이 아니다.
공개 파일은 `<SourceRoot>\.backups\windows-sql\`처럼 정상 디렉터리에 두고,
PrivateRoot는 `%LOCALAPPDATA%\Dawnholder\<새 lifetime>`의 **직속** 하위다.
`%LOCALAPPDATA%\Dawnholder` 부모는 실행자가 확인하여 마련한다. 기존 전용 폴더는 재사용하지 않는다.

`preparation.json`의 닫힌 schema:

```json
{
  "Version": 1,
  "ExecutionApproved": true,
  "ExpiresUtc": "<승인 후 24시간 이내 UTC/offset 시각>",
  "LifetimeId": "<32자리 소문자 hex>",
  "MachineName": "<노트북 이름>",
  "InstanceName": "SQLEXPRESS",
  "ExecutorSid": "<현재 사용자 SID>",
  "ServiceAccount": "NT Service\\MSSQL$SQLEXPRESS",
  "TlsServerName": "<실제로 해석되는 DNS 이름>",
  "PrivateRoot": "<현재 사용자 LOCALAPPDATA 아래 전용 절대 경로>",
  "PublicCaPath": "<SourceRoot 아래 새 public-ca.pem 절대 경로>"
}
```

사용자가 **새 인증서 생성과 해당 키의 서비스 읽기 권한 부여**를 승인한 뒤 관리자 PowerShell에서:

```powershell
$tools = Join-Path (Get-Location) '99_Tools/database/windows-sql'
$preparationPath = (Resolve-Path '.backups/windows-sql/preparation.json').Path
$preparationHash = (Get-FileHash -LiteralPath $preparationPath).Hash
# 위 파일 전체와 hash를 사용자에게 보여 준 뒤 승인된 값 그대로 실행한다.
& "$tools/New-WindowsSqlCertificate.ps1" -PreparationPath $preparationPath `
    -ExpectedPreparationHash $preparationHash
$LASTEXITCODE
```

성공 exit0에는 thumbprint와 receipt hash가 나온다. 새 자체서명 서버 인증서를 LocalMachine/My에
만들고 공개 PEM만 내보낸다. Root 저장소에 추가하지 않는다. 개인키는 비내보내기이며 argv/env/Git에 없다.
`preparation.json`, `certificate-created.txt`, `certificate.json`은 private 폴더에 남는다.
실패 exit1이면 그 폴더와 원시를 보존하고 재실행하지 않는다. thumbprint 기록 전 중단도 자동 성공/삭제로 추정하지 않는다.

생성 직후 thumbprint를 먼저 기록하고, 공통 키 검사와 정확 키 파일의 서비스 읽기 ACL 확인이 끝난 뒤
공개 PEM과 receipt를 쓴다. 키/ACL 단계 실패 시 인증서는 이미 생겼을 수 있고,
ACL 부여 뒤 공개 PEM/receipt 기록 실패 시 읽기 권한도 남을 수 있다. 출력 파일의 부재는
인증서·권한 부재의 증거가 아니다. 자동 삭제·키 재생성 없이 §8의 부분 실패 복구 절차로 넘긴다.

| 공개 코드 | 의미와 다음 확인 |
|---|---|
| `WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED` | 기존 개인키 취득·접근 또는 인증서/키 바인딩 확인 실패. 정확 대상과 실행 권한을 확인하며 새 키로 대체하지 않는다 |
| `WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE` | 공급자·컨테이너 정보나 키 속성을 안전하게 읽어 증명할 수 없음. null을 지원 판정으로 바꾸지 않는다 |
| `WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED` | 읽은 키가 위 SChannel/머신/KeyExchange/비내보내기/RSA 조건 밖임 |
| `WINDOWS_SQL_CERTIFICATE_KEY_ACL_ACCESS_FAILED` | 정확 키 파일의 ACL 조회·부여·재확인 실패. 권한 변경 완료 여부를 추정하지 않는다 |
| `WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID` | 서비스 SID의 Allow Read 부재·부족 또는 Deny 존재 |
| `WINDOWS_SQL_CERTIFICATE_KEY_CHANGED` | 검증한 정확 키 파일과 receipt의 KeyFile 불일치 |
| `WINDOWS_SQL_PATH_INVALID` / `WINDOWS_SQL_PATH_REPARSE` | 유효한 일반 키 파일 경로를 증명할 수 없거나 reparse 경로임 |

오류에는 원래 공급자 예외나 키 내용을 출력하지 않는다. 이 검사는 실제 SQL의 인증서 로딩과 TLS 성공을
대신하지 않으며 노트북의 실제 KeySpec/CAPI 상호운용·SQL 로딩은 별도 실행 근거로 확인해야 한다.

Prepare 성공 뒤에도 Plan이 `Blocked`이거나 Apply 전에 중단하면 인증서와 서비스 계정의 개인키 읽기
권한만 남을 수 있다. Apply의 lifecycle 기록이 없으면 Cleanup은 `WINDOWS_SQL_CLEANUP_STATE_MISSING`,
인증서 Remove는 `WINDOWS_SQL_CLEANUP_REQUIRED`로 거부한다. 정리용 상태를 만들려고 Apply하지 말고
§8의 **인증서만 남은 경우**에 따라 원시를 보존하고 정확 소유 확인·별도 수동 재승인을 받는다.

## 5. runtime/profile/계획을 만들고 고정한다

`profile.json`은 기존 Start-PersistenceServer 공개 형식이다. `accountId`는 서버의
`Accounts/AccountIdResolver.cs`가 소유한 UUID v5 규칙으로 기존 승인 계정 이름과 일치시킨다.
`PersistenceOptions`의 이름 정규화/identity 검사는 실제 서버 진입에서 다시 수행된다.

```json
{
  "persistedAccountName": "<소문자 영숫자/밑줄 3~16자>",
  "database": "Dawnholder_<신규 이름>",
  "serverEndpoint": "tcp:<TLS DNS 이름>,<승인 포트>",
  "slotId": 1,
  "accountId": "<승인 계정 UUID>",
  "characterId": "<새 캐릭터 UUID>"
}
```

`runtime.json`은 아래 필드만 받는다. PublicCaPath와 ProfilePath는 SourceRoot 기준 `/` 상대 경로다.
CredentialPath는 정확히 `<PrivateRoot>\runtime.clixml`이고 존재하지 않는 새 파일이어야 한다.
runtime 비밀은 Apply에서 이 노트북 현재 사용자의 CurrentUser DPAPI로 새 생성한다.
데스크톱 credential을 복사하지 않는다.

```json
{
  "Version": 1,
  "LifetimeId": "<계획과 같은 lifetime>",
  "ExecutorSid": "<현재 사용자 SID>",
  "ServiceAccount": "NT Service\\MSSQL$SQLEXPRESS",
  "EndpointAddress": "<NAT Windows gateway 또는 mirrored 127.0.0.1>",
  "TlsServerName": "<profile과 SAN에 같은 DNS 이름>",
  "NetworkMode": "<Nat 또는 Mirrored>",
  "WslSubnet": null,
  "WslDistribution": "Ubuntu",
  "AccountId": "<profile과 같은 UUID>",
  "CharacterId": "<profile과 같은 UUID>",
  "PublicCaPath": ".backups/windows-sql/public-ca.pem",
  "CredentialPath": "<PrivateRoot>\\runtime.clixml",
  "CertificateReceiptHash": "<Prepare의 실제 certificate.json SHA256>",
  "ProfilePath": ".backups/windows-sql/profile.json",
  "VerifyHostFiles": [ "<host-files.json의 객체 배열 전체로 대체>" ]
}
```

NAT의 WslSubnet은 실제 WSL link route와 정확히 같은 IPv4 CIDR 문자열로 바꾼다.
mirrored만 null이다. 방화벽 규칙은 승인 포트·Windows local address·WSL remote subnet·정확 SQL 서비스에 한정한다.

`plan.json`은 기존 v1을 유지한다:

```json
{
  "Version": 1,
  "ExecutionApproved": true,
  "ExpiresUtc": "<현재보다 미래이며 24시간 이내>",
  "LifetimeId": "<동일 lifetime>",
  "MachineName": "<노트북 이름>",
  "InstanceName": "SQLEXPRESS",
  "InstanceId": "<실제 MSSQL17.SQLEXPRESS 등>",
  "ExpectedProductVersion": "<실제 네 부분 버전>",
  "Port": 15433,
  "Database": "Dawnholder_<신규 이름>",
  "RuntimeLogin": "dh_<신규 이름>",
  "SchemaVersion": 4,
  "CertificateThumbprint": "<Prepare의 실제 thumbprint>",
  "Encrypt": "Mandatory",
  "TrustServerCertificate": false,
  "EnableSa": false,
  "SwitchWslNetworkingMode": false,
  "ForceEncryption": "Preserve",
  "RestartApproved": true,
  "FirewallApproved": true,
  "SourceRoot": "<현재 checkout 절대 경로>",
  "EvidenceRoot": "<SourceRoot>\\.backups\\windows-sql\\evidence",
  "PrivateRoot": "<Prepare와 같은 전용 절대 경로>",
  "Inputs": [ "<아래에서 실제 계산한 Path/Sha256 배열로 대체>" ]
}
```

Port 15433은 예제다. 재시작/방화벽 bool은 실제 승인만 반영한다. 필요 단계가 미승인이면 Plan은 Blocked다.
계획과 입력 bytes를 고정하는 예시(현재 checkout 루트, 공개 파일만):

```powershell
$sourceRoot = (Get-Location).Path
$databaseRoot = Join-Path $sourceRoot '99_Tools/database'
$paths = @(
    '99_Tools/database/Database.Common.ps1', '99_Tools/database/SqlError.Common.ps1',
    '99_Tools/database/Module.Common.ps1', '99_Tools/database/ModuleHash.Common.ps1',
    '99_Tools/database/Test-ModuleStructure.ps1', '99_Tools/database/verify-schema.sql',
    '99_Tools/database/modules/manifest.json', '99_Tools/sync-wsl.sh',
    '99_Tools/Formatting/sdk.sh', 'global.json',
    '.backups/windows-sql/runtime.json', '.backups/windows-sql/profile.json',
    '.backups/windows-sql/public-ca.pem'
)
$paths += Get-ChildItem "$databaseRoot/migrations" -File -Filter '*.sql' |
    ForEach-Object { '99_Tools/database/migrations/' + $_.Name }
$manifest = Get-Content "$databaseRoot/modules/manifest.json" -Raw | ConvertFrom-Json
$paths += $manifest.Entries | ForEach-Object { '99_Tools/database/' + $_.Path }
$paths += Get-ChildItem "$databaseRoot/windows-sql" -File -Filter '*.ps1' |
    ForEach-Object { '99_Tools/database/windows-sql/' + $_.Name }
$paths += '99_Tools/database/windows-sql/WindowsSqlVerifyHost/Program.cs'
$paths += '99_Tools/database/windows-sql/WindowsSqlVerifyHost/WindowsSqlVerifyHost.csproj'
$inputs = @($paths | Sort-Object -Unique | ForEach-Object {
    [ordered]@{ Path = $_; Sha256 = (Get-FileHash -LiteralPath (Join-Path $sourceRoot $_)).Hash }
})
$planPath = (Resolve-Path '.backups/windows-sql/plan.json').Path
$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json -AsHashtable
$plan.Inputs = $inputs
$plan | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $planPath -Encoding utf8NoBOM
$planHash = (Get-FileHash -LiteralPath $planPath).Hash
```

hash를 계산한 것만으로 승인이 생기지 않는다. 파일 전체·대상·scope·예상 중단·hash를 검토하여 승인받는다.
승인 뒤 입력을 바꾸면 재승인이 필요하다. 계획 lifetime/PlanHash를 바꿔 기존 receipt를 이어받지 않는다.

## 6. 읽기 → Plan → Apply → Verify

각 명령의 stdout/stderr/exit와 관측 UTC를 EvidenceRoot의 **서로 다른 파일**에 보존한다.
아래 변수는 직전 승인 파일/정확 hash이며 자동으로 새 hash를 재계산하여 승인 대용으로 삼지 않는다.

```powershell
$runtimePath = '.backups/windows-sql/runtime.json'
$cli = Join-Path $tools 'Invoke-WindowsSqlEnvironment.ps1'
& $cli -PlanPath $planPath -ExpectedPlanHash $planHash -RuntimePath $runtimePath -Step Preflight
& $cli -PlanPath $planPath -ExpectedPlanHash $planHash -RuntimePath $runtimePath -Step Plan
```

각 exit를 확인한 뒤 다음 명령을 따로 실행한다. Preflight는 승인한 인스턴스 설정·정확 신규 이름만 읽는다.
helper의 임시 파일과 .NET CLI 상태는 전용 PrivateRoot 안에 한정하며 인스턴스 설정은 바꾸지 않는다.
Plan의 `Decision/Changes/Blockers/TargetConfiguration`을 검토한다. `ObservationSource=Provided`는
판정 코어의 입력 출처이며, 실제 adapter 결과에는 `AdapterKind=WindowsLocal`이 붙는다.
`Blocked` exit2는 실행 허가가 아니다. 정상 exit0, 입력/실행 실패 exit1이다.

설정 백업은 Apply 최초 변경 전에 PrivateRoot의 `snapshot.json`으로 저장된다.
`lifecycle.json`의 pending 연산을 durable하게 기록한 뒤 변경하고, `resources.json`은 정확
DB GUID/login SID/firewall 소유 receipt다. 다른 소유자·버전·설정과 불일치하면 중단한다.
기존 대상 DB를 초기화하거나 migration을 덮어쓰지 않는다.

사용자가 정한 중단 창과 관리자 실행 승인이 유효할 때:

```powershell
& $cli -PlanPath $planPath -ExpectedPlanHash $planHash -RuntimePath $runtimePath -Step Apply
# exit0와 Applied/Unchanged를 확인한 뒤 별도로:
& $cli -PlanPath $planPath -ExpectedPlanHash $planHash -RuntimePath $runtimePath -Step Verify
```

Verify는 WSL의 실제 SqlClient 연결에서 TLS chain/name·서버 identity·TCP·runtime 5 RPC 권한과
직접 DML 거부를 확인하고, 로컬 관리 연결에서 정확 소유 DB의 migration/catalog/계정 바인딩을 대조한다.
자격은 DPAPI→인메모리→자식 stdin으로만 전달한다. 예외 상세·연결 문자열·비밀은 출력하지 않는다.
관리 문자열의 수명을 짧게 하고 SecureString/BSTR/프로세스를 정리하지만 관리 힙의 모든 복사본에 대한
물리적 즉시 삭제를 보장한다고 주장하지 않는다. timeout/취소 시 자식 종료, 성공 fallback 없음이다.

## 7. 실제 저장·재접속·정상 종료는 별도 관측

Verify만으로 게임 저장 동작을 통과 처리하지 않는다. 기존 서버 빌드/7777 소유·메모리 관문과
사용자 승인 runtime 창을 확보한 뒤 기존 launcher를 그대로 사용한다:

```powershell
$runtime = Get-Content '.backups/windows-sql/runtime.json' -Raw | ConvertFrom-Json
$publicCa = Join-Path $sourceRoot $runtime.PublicCaPath
& './99_Tools/Start-PersistenceServer.ps1' `
    -ProfilePath (Join-Path $sourceRoot $runtime.ProfilePath) `
    -CredentialPath $runtime.CredentialPath `
    -PublicCaPath $publicCa `
    -ExpectedPublicCaHash (Get-FileHash -LiteralPath $publicCa).Hash `
    -EmptyCaDirectory (Join-Path $plan.PrivateRoot 'empty-ca')
```

공개 CA hash는 계획의 승인 input hash와 먼저 대조한다. 실행자가 파일을 바꾼 뒤 자동 계산값만
받아들이지 않는다. 실제 저장 계정으로 입장→class 선택→승인 safe checkpoint 저장→연결 해제→재접속의
저장/복원 결과를 관측한다. Enter로 정상 종료하고 shutdown 완료·프로세스 exit·남은 owned session을 확인한다.
계정/캐릭터/시각/exit/정상종료/전후 보호 근거를 반환하고 비밀은 원시에 넣지 않는다.
도구는 기존 DB 내용을 읽어 보호 증거를 대신 만들지 않는다.

## 8. 소유 자원 정리와 실패 회복 경계

게임/runtime 연결을 정상 종료하고, 같은 원래 계획/hash가 유효한 동안 사용자가 명시한 Cleanup을 실행한다.
현재 설정이 기록한 실제 사후값과 같아야 한다. 다른 사람이 바꾼 설정·소유권·불확실 부작용은
어떤 삭제/복원보다 먼저 차단한다. DROP에 강제 rollback/다른 세션 종료를 사용하지 않는다.

Cleanup에도 §1의 의존 서비스 조건(빈 집합 또는 전체 `Stopped` + `Disabled`)이 필요하다.
DB/login만 정리하고 설정 복원·재시작이 필요 없는 경우에도 같은 조건을 적용한다.
조건 미충족·관측 실패로 거부되면 정리/복원을 자동 진행하지 않고 현재 상태와 공개 원시를 보존해 담당자에게 보고한다.
조건을 맞추려고 의존 서비스를 임의로 정지·삭제하거나 시작 유형을 바꾸지 않는다.
환경 조정이 필요하면 정확 대상·조치·영향·순서를 별도로 승인받고, 조정 뒤 안전 조건을 다시 관측한 다음
원래 계획/hash·승인 유효성 및 소유·상태 조건을 충족하는 승인된 Cleanup만 수행한다.

```powershell
& $cli -PlanPath $planPath -ExpectedPlanHash $planHash -RuntimePath $runtimePath -Step Cleanup
# Cleaned 확인 후, 별도 명시 명령으로 새 인증서/개인키와 확인된 DPAPI 파일만 정리:
& "$tools/Remove-WindowsSqlCertificate.ps1" -PlanPath $planPath `
    -ExpectedPlanHash $planHash -RuntimePath $runtimePath
```

Cleanup은 해당 lifetime이 생성 완료한 login/DB/firewall만 제거하고 원래 설정을 복원한다.
원래 ForceEncryption 값은 쓰지 않고 보존·대조한다. 인증서 정리는 Cleaned/원본 설정 복원/정확
thumbprint·서비스 키/credential hash를 확인한다. 공개 PEM·snapshot·receipt·journal은 증거로 남긴다.
정리 중 실패도 재시도로 덮지 않고 `certificate-cleanup.json`의 Removing 상태를 보존한다.
Remove도 §1·§4와 같은 키/파일/서비스 ACL 검사를 통과해야 제거 의도를 기록하고 삭제한다.
접근 실패나 미지원 판정이 나면 receipt를 고치거나 키 조건을 완화하지 않고 현재 원시를 보존한다.

**인증서만 남은 경우(Apply 전 중단·Plan Blocked):** lifecycle이 없으면 위 Cleanup/Remove 명령을
사용할 수 없다. Cleanup은 상태·소유 자원·원래 설정 근거가 필요하고, Remove는 `Cleaned` 상태와
설정 복원을 요구하기 때문이다. 이 경우에도 아래 수동 복구 절차를 적용한다. Apply를 강행하거나
`lifecycle.json`에 Cleaned를 써 넣어 정리 관문을 우회하지 않는다.

수동 인증서 정리는 원래 preparation의 lifetime/machine/instance/ExecutorSid/PrivateRoot와
`certificate-created.txt`·`certificate.json`의 정확 thumbprint를 먼저 대조한다. 승인한 인증서의
개인키 식별자·서비스 SID와 그 키에 부여한 읽기 ACL을 확인하고, 현재 SQL 설정이나 다른 사용처가
그 인증서·키에 의존하지 않는지 확인한 근거를 남긴다. 기록 누락·소유 모호·다른 사용처가 있으면
보존하고 보고한다. 생성한 인증서·개인키 및 해당 서비스 읽기 권한의 정리 대상·순서를 명시하여
별도로 승인받은 작업에서만 정리한다. 기존 인증서나 다른 키의 ACL은 삭제·변경하지 않는다.
Apply 전에는 snapshot/resources/lifecycle가 없을 수 있으므로 부재를 기록하고 복원값을 추정하지 않는다.

**lifecycle 없이 snapshot 파일만 남은 경우:** snapshot 저장 실패 뒤 `lifecycle.json` 없이
`snapshot.json` 또는 `.pending` 파일만 남은 상태도 아래 수동 복구 절차를 적용한다.
lifecycle 부재나 오류 코드만으로 실제 설정 변경이 없었다고 단정하지 않는다. 공개 실패 원시와
원래 plan/runtime/hash·파일 소유 근거를 보존하고 담당자의 별도 복구 판단·구체 승인을 받는다.
잔여 파일을 자동 삭제하거나 재Apply·Cleanup 우회로 복구하지 않으며, 비밀 파일을 복사·외부 공개하지 않는다.

**실패한 login의 잔여 DPAPI 파일:** login 생성은 CurrentUser DPAPI 파일을 먼저 저장한다.
그 뒤 SQL 생성·receipt 기록이 실패하거나 중단되면 `<PrivateRoot>\runtime.clixml`이 남고
resources receipt의 `CredentialHash`는 null일 수 있다. hash가 null인 파일은 인증서 Remove가 자동 삭제하지 않는다.
오류만으로 login이 없다고 판단하거나 파일을 지워 재생성을 시도하지 않는다. 원래 plan/runtime의
lifetime·정확 CredentialPath와 실행 사용자 SID·파일 소유/ACL·현재 hash를 보존하고, journal·실패 원시와
정확 대상 login의 실제 생성/정리 상태를 대조한다. 비밀을 복호화·출력·전달하지 않는다.
경로·소유가 확정되고 더 이상 필요 없는 파일이라는 근거를 갖춘 뒤, **그 파일 하나의 수동 삭제**를
별도로 승인받는다. 경로·소유·상태가 모호하면 보존하고 보고하며 PrivateRoot 전체를 지우지 않는다.

**24시간 승인 만료 뒤 자동 Cleanup은 지원하지 않는다.** 원래 PlanHash와 snapshot을 유지한
제한이며 `ExpiresUtc`만 고쳐 재승인을 흉내 내거나 receipt를 지워 새 lifetime으로 재시작하지 않는다.
사용자는 적용 당일 정리를 계획한다. 승인 만료·불확실 실패·위 인증서/snapshot/DPAPI 잔여 상태에서는 다음
수동 복구 절차의 정확 대상과 조치를 별도로 승인한다:

1. 기존 plan/runtime/source hash, preparation/certificate receipt, snapshot/resources/lifecycle와 실패
   stdout/stderr/exit/UTC를 그대로 보존한다. 아직 생성되지 않은 기록은 부재로 남기며 새로 꾸미지 않는다.
   비밀 파일은 전달하지 않고 SID·파일 hash만 기록한다.
2. 담당자가 **정확한 원래** machine/instance와 journal의 마지막 confirmed/pending 연산을 대조한다.
   SQL 관리 조사가 필요하면 동일 LPC/Windows 통합 인증·Shared memory 확인을 유지한다.
   정확 대상의 DB GUID/login SID/소유 표식·migration 상태, 해당 firewall 규칙, 현재 설정만 확인한다.
3. 현재 값과 마지막 confirmed 사후값을 비교한다. 부작용 후 예외·migration/restart 중단·journal 기록
   실패는 성공/실패로 추정하지 않는다. 필요한 수동 조치와 영향·순서·정확 식별자를 작성해 재승인받는다.
4. 새로 승인한 복구 작업에서만 확정 소유 자원을 정리한다. snapshot이 있고 설정 복원이 승인된
   경우에만 확인된 원래 값을 복원한다. 인증서만 준비했다면 인스턴스 설정 복원을 임의 추가하지 않는다.
   GUID/SID/설정이 달라졌으면 보존하고 담당자 판단을 받는다. 다른 DB나 인스턴스 삭제/강제 세션 종료는 하지 않는다.
5. 원래 파일을 수정하지 않고 별도 복구 기록에 실제 전후값·exit·미확인을 붙인다. 기존 백업/메타데이터
   보호 기준과 비교한 뒤 사용자에게 결과를 반환한다.

이 도구에 일반 복구/새 만료 승인 우회 기능은 없다. 불확실 상태를 자동으로 Adopt/Reset하지 않는다.
노트북 실제 SQL·인증서 로딩·네트워크·TLS·재시작·복원·게임 저장/재접속은 PR2 원시와 독립 판정 전까지 미실행이다.
