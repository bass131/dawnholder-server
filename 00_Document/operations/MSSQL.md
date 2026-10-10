# 로컬 MSSQL 개발 DB

## 현재 — Linux 컨테이너의 시험 DB 도구

Windows PowerShell 5.1의 도구가 승인 계획 v2에 고정한 로컬 Linux SQL Server에 SQL 인증으로 접속한다. PowerShell 7 소스 분석과 제품 실행 지원은 구분한다. 게임 서버의 로그인·저장·재접속 연결은 후속 작업이며 이 테이블에 자동 저장되지 않는다. 전환 결정과 대안은 [ADR-035](../ADR/tech-stack/ADR-035-sqlserver-container-tools.md), 실제 실행 승인·진척·미실행은 [진행 goal](../../01_Phases/goals/2026-10-04-persistence-integration/goal.md#현재-범위pr-경계)에 있다. 이 안내와 계획의 승인 문자열 자체가 실행 승인을 대신하지 않는다.

### 진입점과 한 번 수명

| 도구 | 입력과 책임 |
|---|---|
| [New-TestDatabase.ps1](../../99_Tools/database/test-environment/New-TestDatabase.ps1) | 승인 계획 경로·별도로 검토한 SHA256·정확 DB·manifest 경로. OfflinePlan은 입력 검토, Plan은 비밀 없는 Planned manifest, Create는 사전 검사와 DB 생성, Install은 설치 단계 |
| [New-TestAdminCredential.ps1](../../99_Tools/database/test-environment/New-TestAdminCredential.ps1) | 같은 승인 입력과 Planned manifest. 첫 컨테이너 기동 **전** private 폴더와 sa DPAPI CLIXML을 한 번 생성하고 hash·저널을 기록. SQL·Docker 호출 없음 |
| [Install-Database.ps1](../../99_Tools/database/Install-Database.ps1) | 정확 수명의 Baseline001 또는 Complete(001~004·module·권한·catalog)를 수명 도구에 전달 |
| [Invoke-PreservationCheck.ps1][preservation] | `-Action Record`는 Baseline001에서 행 기록, `Compare`는 Installed에서 보존 대조 |
| [Test-EngineBoundary.ps1][engine-boundary] | `-Action BeforeComplete`는 위치 A, `AfterComplete`는 위치 B. 모든 관측 rollback |
| 위 두 도구의 승인 입력 | `-ApprovalPlanPath`, `-ExpectedApprovalPlanHash`, `-Database`, `-ManifestPath` |
| 위 두 도구의 근거 입력 | 기존 절대 `-EvidenceDirectory` 아래 `-ResultPath`, `-SnapshotPath`. Record만 신규 스냅숏, 나머지는 기록본. 덮어쓰기 금지 |
| [Initialize-CharacterBinding.ps1](../../99_Tools/database/test-environment/Initialize-CharacterBinding.ps1) | 승인된 slot1·AccountId·CharacterId 고정 바인딩 |
| [Set-TestPrincipals.ps1](../../99_Tools/database/test-environment/Set-TestPrincipals.ps1) | runtime SQL 로그인·user·dh_runtime만 구성. 무작위 SID 계획·대조, 서버 role 0·DB role 정확히 하나·추가 직접 권한/소유 0 검사 |
| [Test-Database.ps1](../../99_Tools/database/Test-Database.ps1) | 계획 경로/hash와 **-Endpoint**, **-Database** 필수. manifest를 읽고 동일 연결 helper로 두 연결을 열어 원래 rollback·잠금·계약 검사 수행 |
| [Remove-TestEnvironment.ps1](../../99_Tools/database/test-environment/Remove-TestEnvironment.ps1) | 정산·quiescent·정확 identity/hash 확인. OfflinePlan / OnlinePreview / Execute를 구분하고 명시 확인과 manifest hash 뒤에만 정리 |
| [Environment.Common.ps1](../../99_Tools/database/test-environment/Environment.Common.ps1) | v2 검증, 연결 설정·신원·오류 분류, ACL/DPAPI 파일, 저널의 단일 소유자 |

[preservation]: ../../99_Tools/database/test-environment/Invoke-PreservationCheck.ps1
[engine-boundary]: ../../99_Tools/database/test-environment/Test-EngineBoundary.ps1

순서는 OfflinePlan 검토 → Plan → New-TestAdminCredential → 승인된 카드의 컨테이너 기동/준비 확인 → Create → Baseline001 →
보존 기록(Record) → 위치 A 관측(BeforeComplete) → Complete → 보존 대조(Compare) → 위치 B 관측(AfterComplete) →
binding → runtime 주체 → 시험 → 정산/정리다.
제품 상태는 Planned → Created → Baseline001 → Installed → Bound → PrincipalsReady → CleanupStarted → Removed를 유지한다.
**PrincipalsReady는 runtime 주체 준비 완료**이며 recovery 계정은 만들지 않는다(사용자 결정 27 A).
DB의 dh_recovery role과 4개 RPC 권한은 유지하며 복구 주체 형태·실증은 후속 goal이다.

단계 저널 Pending/Done/Failed, 배타 .lock, 미완 .pending 보존을 유지한다. 실패한 단계를 자동 재시도·정리하지 않으며 같은 이름 DB·로그인을 채택하거나 회전하지 않는다. 이미지는 보존, 볼륨+시험 DB는 한 번 수명, 컨테이너는 같은 볼륨 위 프로세스 host다. 승인당 시도 3회·단계당 컨테이너 재생성 1회 상한과 직전 수명의 정리 원시를 카드가 확인한다.

### 승인 계획 v2와 manifest v2

계획은 JSON이며 아래 모든 필드를 명시한다. 누락·알 수 없는 필드·v1은 연결 전에 거부한다. PlanVersion/SchemaVersion은 모두 정수 2다. 계획 SHA256은 읽어서 파싱하는 동일 bytes에 대해 대조한다. 관측 엔진값을 기대값으로 채택하지 않는다.

| 필드 | 형식·검증 |
|---|---|
| PlanVersion, SchemaVersion | 2만 허용. SQL migration 버전 4와 별개 |
| ExecutionApproved, G0, G1, G2 | boolean 승인 표시와 검토 기록. 실제 실행은 true 및 별도 G2 기록 필요. 사용자 승인 원문은 별도로 검토 |
| Goal, GoalMarker, Machine, ExecutorSid | 비어 있지 않은 목표/표식·Windows 실행 기계·SID. 모든 실행은 같은 기계/SID, 관리자 토큰 불필요 |
| ContainerName, ContainerHostname, VolumeName | 소문자 영숫자와 중간 하이픈, 영문 시작, 최대 63자. 목적 이름을 쓰며 날짜·마일스톤·작업자 이름 거부 |
| ImageDigest | sha256: 뒤 소문자 hex 64자리. 카드에서 정확 이미지 digest 대조 |
| Endpoint, AdminLogin | tcp:127.0.0.1,포트(1~65535, 7777 제외), sa만 허용 |
| ExpectedCollation, ExpectedProductVersion | SQL 정렬 규칙 이름과 숫자 네 부분 버전. 생성 전 계획으로 고정; 승인 값은 goal 결정 28 A, 지원/실행 근거는 goal에서 확인 |
| ConnectTimeoutSeconds, ReadyTimeoutSeconds, ReadyAttemptLimit, PullTimeoutSeconds, StopTimeoutSeconds | 명시 양의 정수. StopTimeoutSeconds는 30 이상이며 강제 종료까지의 대기 값(아래 정지 측정 근거 참조). ConnectTimeout만 도구 연결에 사용, 나머지는 카드 비용 상한 |
| SqlMemoryLimitMb, ContainerMemoryLimitMb, MinFreeMemoryMb, MinFreeDiskMb | 명시 양의 정수. 컨테이너 천장 ≥ SQL 천장, Windows 여유 메모리 바닥 ≥ 2048MiB. 실제 배치값은 승인 계획 |
| Attempt | 1~3 정수. 자동 재시도 허가가 아님 |
| Database, RuntimeLogin | Dawnholder_Test_ 또는 Dawnholder_Dev_ 뒤 목적 접미, 최대 128자; runtime은 dh_ 뒤 소문자 목적명. 옛 DB·날짜/마일스톤 이름은 거부 |
| SlotId, AccountId, CharacterId | slot1, 비어 있지 않은 D형 GUID 두 개 |
| Encrypt, TrustServerCertificate | boolean true. 이 로컬 컨테이너 endpoint에서만 인증서 신뢰 |
| ManifestPath, SettlementPath | 서로 다른 정규화된 로컬 절대 파일 경로, 같은 부모. 비밀/identity 폴더 밖 |
| PrivateDirectory, IdentityDirectory | 서로 분리된 %LOCALAPPDATA%\Dawnholder\ 하위 폴더. C:\ProgramData·과거 MssqlWsl-SQLEXPRESS 경로는 읽기 전 거부 |
| AdminCredentialPath, RuntimeCredentialPath, IdentityPath | 각각 private 아래 sa/runtime 파일, identity 폴더 아래 비밀 없는 투영 파일. 경로 중복·reparse 경유 거부 |

manifest는 승인 필드와 ApprovalPlanPath/Hash를 고정하고, 도구가 MasterFamilyGuid·Engine·시험 DB identity·migration 이력·runtime SID·비밀 파일 hash·단계/정리 저널을 쓴다. sa 파일 hash는 New-TestAdminCredential의 SaveAdminCredential 단계가 manifest에 고정한다. 비밀을 만들기 전 계획에 아직 없는 hash를 요구하는 순환을 피하며, 이후 읽기와 삭제 때 실행자+SYSTEM ACL·소유자·기록 hash를 모두 대조한다. sa는 회전하지 않는다.

모든 SQL 연결은 SqlCredential와 읽기 전용 SecureString을 쓴다. 연결 문자열에는 Password/User ID가 없고 Integrated Security=false, Persist Security Info=false, Pooling=false, Encrypt/TrustServerCertificate=true, Connect Timeout=계획 값이다. ConnectRetryCount=0으로 자동 재연결도 막는다. 연결은 한 번 열어 쓰고 닫으며 재사용하지 않는다. CommandTimeout 30초, migration/create/binding lock 5000/5000/2000ms, 정리 LOCK_TIMEOUT 1000ms는 보존한다.

접속마다 MachineName=계획 hostname, InstanceName NULL, host_platform=Linux, sa/sysadmin을 확인한다. Create의 master preflight에서 database_id=1의 **family_guid**를 MasterFamilyGuid에 기록하고 이후 매 접속에서 대조한다. master database_guid는 같은 이미지의 서로 다른 두 볼륨에서 같았으므로 볼륨 판별에 쓰지 않는다(계약 보충 v1.1, 위험 확인 원시 표본 2). ServerName/@@SERVERNAME도 재생성 때 보존/변경이 달라 쓰지 않는다. 시험 DB 자체의 database_guid·create_date·owner_sid·ID·collation·RCSI 대조는 유지한다.

Open의 18456은 로그인 실패, 그 외 Open 오류는 도달 불가, 접속 후 신원 차이는 신원 불일치, 계획/manifest 엔진값 차이는 엔진 변경으로 고정 사유를 낸다. SqlException 번호는 SqlError.Common 경계를 통해 보존하고 제공자 문구는 억제한다. 도구가 Docker 원인을 추정하지 않는다. 다만 **준비 대기 성공 전의 18456은 비밀번호 오류의 증거가 아니다**. 재측정 [02-A-ready-attempts.jsonl](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-A-ready-attempts.jsonl)의 시도 4에서 18456이 나온 뒤 시도 5에서 접속에 성공했다. 위 진입 순서대로 카드가 준비 대기 성공을 기록한 뒤에만 도구를 부른다.

### 실행 카드의 Docker 규칙과 정리

도구 안에는 Docker·WSL 수명 호출이 없다. 카드 작성자는 다음을 원문으로 고정하고 실행 창의 시작·끝 UTC와 준비 대기 상한을 계획과 함께 확인한다.

- 개발·시험용 SQL Server Developer 사용권은 사용자 결정 **26 A**, 전달 msg_3a98521517fa에 근거한다. 첫 실행은 ACCEPT_EULA=Y, MSSQL_PID=Developer다.
- run은 --restart no, --hostname <계획 값>, -p 127.0.0.1:<계획 포트>:1433, --pull=never를 명시하고 사전 image digest를 대조한다. MSSQL_COLLATION, MSSQL_MEMORY_LIMIT_MB와 --memory는 계획 값이다. 시작 뒤 `127.0.0.1:<계획 포트>` TCP 연결 탐침 성공과 `docker port <정확한 이름>`의 `1433/tcp -> 127.0.0.1:<계획 포트>`를 함께 기록해 loopback 연결·바인딩을 증명하고, running 상태·준비 대기 성공도 원시로 남긴 뒤에만 도구에 넘긴다. 재측정은 [TCP 탐침 Connected: true](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/03-A-port-probe.json)와 [docker port 바인딩](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/03-A-port-stdout.txt)을 확인했다. 같은 상태에서 [Windows TCP 표에는 해당 포트의 Listen이 없었으므로](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/03-A-tcp-listeners.json) `Get-NetTCPConnection` 표를 이 증명에 쓰지 않는다.
- sa 파일은 위 고정 진입점으로 먼저 만든다. 카드는 ACL/hash를 확인한 파일을 Import-Clixml로 복호해 **실행 프로세스 환경 변수만** 사용하고 docker run -e MSSQL_SA_PASSWORD처럼 이름만 argv에 넣는다. finally에서 환경값과 평문 참조를 제거한다. 비밀을 채팅·goal·argv·로그·연결 문자열에 넣지 않는다.
- sa는 회전하지 않으며 초기 값이 컨테이너 Env에 남는다. 모든 docker inspect는 **Env를 읽지 않는 --format만** 사용한다. docker exec·cp·commit·export는 금지한다. docker logs는 이름 정한 컨테이너의 장애 분류에만 쓰고 저장 출력은 비밀 대조로 평문 부재를 확인한다.
- 존재·부재·삭제 대상은 docker inspect <정확한 이름>(컨테이너/볼륨/이미지 종류 명시)로 확인한다. --filter name= 부분 일치 목록을 부재 근거나 삭제 대상에 쓰지 않는다. 컨테이너 Id·digest·볼륨 CreatedAt·hostname·시작 UTC를 비밀 없이 기록하고 정리 때 대조한다.
- 현재 정지는 컨테이너·볼륨을 바로 지우는 정리 경로와 실패 보존 경로에 한해 `docker stop -t <계획 값, 30 이상>`을 쓴다. StopTimeoutSeconds는 **강제 종료까지의 대기**이며 정상 종료를 보장하는 유예가 아니다. `docker kill`·`rm -f`·`restart` 명령 금지는 유지한다. 실패하면 저널과 장애 원시, 정지된 컨테이너+볼륨을 보존하고 메인에 보고한다. 실패 시도 삭제는 별도 명시 승인 뒤에만 한다. 정지 뒤 같은 볼륨을 다시 쓰는 절차(SHUTDOWN 경로와 실행 주체 포함)는 1단계 카드 설계에서 정하며, 그 전에는 볼륨 재사용을 위한 정지를 하지 않는다. 해당 절차를 채택하기 전에는 SQL Server 시작 완료 뒤의 정지를 다시 잰다.

  정지 재측정의 조건: 세 정지는 모두 첫 접속 성공 뒤 0.577~3.495초에 시작했다(접속 시각 [A](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-A-ready.json)·[B](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-B-ready.json)·[C](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-C-ready.json)의 `ConnectedUtc`와 아래 정지 원시의 `CommandStartedUtc` 차이). 시작 로그는 접속 직후에 수집했고, 수집 종료부터 정지 시작까지 0.372~3.255초 구간의 로그는 없다(수집 기록 [A](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-A-startup-logs-receipt.json)·[B](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-B-startup-logs-receipt.json)·[C](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-C-startup-logs-receipt.json)의 `FinishedUtc`부터 `CommandStartedUtc`까지). 시작 중이었음이 원시로 확인되는 것은 B뿐이다. B는 SHUTDOWN 시작 뒤에도 [색인 복원 로그](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-B-stop-logs-stdout.txt)가 이어졌다. A·C의 정지 시점 시작 완료 여부는 **미측정**이다. 따라서 아래 세 값은 **시작 직후 값이며 정상 상태의 정지 값으로 쓰지 않는다**.

  이 조건에서 `docker stop -t 30`은 명령 소요 30.743초·[명령 exit 0](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-A-stop-command-receipt.json)이었고 [컨테이너는 exited·ExitCode 137](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-A-stop.json)로 강제 종료됐다. `-t 120`은 명령 소요 120.756초·[명령 exit 0](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-C-stop-command-receipt.json)이었고 [컨테이너는 exited·ExitCode 137](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-C-stop.json)로 강제 종료됐다. 관리 로그인 sa의 T-SQL `SHUTDOWN`은 명령 소요 0.219초, SHUTDOWN 시작 뒤 1.354초 안에 컨테이너의 exited·ExitCode 255를 관측했다([05-B-stop.json](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-B-stop.json)). 로그에는 `Server shut down by request from login sa`가 남았다([05-B-stop-logs-stdout.txt](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/05-B-stop-logs-stdout.txt)). 이는 측정 사실이며 SHUTDOWN 절차 채택을 뜻하지 않는다. SIGTERM 처리 원인은 미측정이다.

  정상 상태의 비교 기준은 첫 추가 측정이다. [Recovery 완료 로그](../../.backups/verification/2026-10-04-persistence-integration/r7-container-measure/run/failure-container-logs-stdout.txt)와 [정렬 규칙 변경 완료 로그](../../.backups/verification/2026-10-04-persistence-integration/r7-container-measure/run/11-final-container-logs-stdout.txt)가 나온 뒤, 시작 완료 상태에서 실행한 `docker stop -t 30`은 [명령 소요 30.75초·명령 exit 0](../../.backups/verification/2026-10-04-persistence-integration/r7-container-measure/run/11-final-container-stop-receipt.json)이었고 [컨테이너는 exited·ExitCode 137](../../.backups/verification/2026-10-04-persistence-integration/r7-container-measure/run/11-final-container-after.json)이었다. **정상 상태의 SHUTDOWN은 미측정**이다.

  master `family_guid`는 [A](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-A-identity.json)·[B](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-B-identity.json)·[C](../../.backups/verification/2026-10-04-persistence-integration/r7-container-remeasure/run/02-C-identity.json) 세 기동 표본에서 같았다. A의 30초 stop 뒤와 B의 SHUTDOWN 뒤 같은 볼륨 재기동에서 유지를 확인한 것이며, 최종 C의 120초 stop 뒤 조회는 없다.
- **정리 순서:** 도구가 정산·연결/작업/owner/trigger·identity 확인 → DB DROP → SID가 맞는 runtime 로그인 → hash가 맞는 runtime 파일·sa 파일을 지운다. sa 로그인은 지우지 않는다. 그 뒤 카드가 컨테이너 정지·삭제 → 볼륨 삭제 → 각각 정확 이름 부재 확인을 기록한다. 정산·manifest·비밀 없는 identity·빈 폴더는 보존한다.
- 매 단계 전 Windows 여유 메모리 2GiB 바닥과 계획 천장을 확인하고 Windows/WSL/Docker 메모리 원시를 남긴다. Docker Desktop 설정·.wslconfig를 바꾸지 않는다. pull 전·후, 볼륨 생성 후, 정리 후 **네 시점**의 docker system df·VM 디스크 크기·호스트 여유 공간을 기록한다. 볼륨 삭제가 VM 디스크 축소를 증명하지 않는다.



### 현재 저장·권위·증빙 모델

[001_initial.sql](../../99_Tools/database/migrations/001_initial.sql)의 게임 테이블 3개와 파일/checksum은 보존했다. `dh.SchemaVersion`은 [DB 생성 도구의 bootstrap](../../99_Tools/database/test-environment/New-TestDatabase.ps1)에서 따로 만들고 공유 runner가 적용 이력을 기록한다. [002](../../99_Tools/database/migrations/002_persistence_metadata.sql)는 authority/operation과 두 role, [003](../../99_Tools/database/migrations/003_module_metadata.sql)은 코드 배포 metadata를 추가한다.

| 테이블 | 역할·제약 |
|---|---|
| `dh.Account` | 애플리케이션이 발급하는 GUID AccountId PK, 생성 UTC. 게임 인증 계정의 소유 식별자 자리만 마련하며 비밀번호·로그인명·외부 인증 공급자는 아직 정의하지 않음. |
| `dh.Character` | GUID CharacterId PK, AccountId FK, Class 0/1 CHECK, 생성 UTC, rowversion. AccountId 인덱스. 001 자체는 여러 Character 행을 허용하며 현재 RPC는 Authority의 고정 slot1에 바인딩함. 이름 정책은 추가하지 않음. |
| `dh.CharacterProgress` | CharacterId PK/FK로 최대 한 체크포인트. MapId 0~3, real 좌표, int Hp/MaxHp, BossUnlocked bit, 저장 UTC, rowversion. `MaxHp > 0`, `0 <= Hp <= MaxHp`. FK 삭제 cascade 없음. |
| `dh.SchemaVersion` | 버전 PK, 파일명 UNIQUE, 양수 버전 CHECK, SHA-256 및 적용 UTC. 게임 계정과 무관한 설치 이력. |
| `dh.CharacterAuthority` | `SlotId=1` 하나의 사전 바인딩 AccountId/CharacterId, 현재 owner kind/ID, 영구 증가 `bigint Fence`, owner 내 `LastSequence`. 게임 행 생성과 별개이며 migration은 GUID를 만들거나 기존 행을 채택하지 않음. |
| `dh.CharacterOperation` | 고정 OperationId에 대한 terminal 증빙. Kind·PayloadVersion·typed 요청의 canonical bytes·Applied/NotApplied·code·당시 ResultSnapshot·RecordedUtc. 진행 중 상태나 나중의 UPDATE를 기록하지 않음. |
| `dh.ModuleRelease` | 버전 migration이 선언한 코드 manifest checksum 이력. 해당 SchemaVersion과의 대응은 같은 transaction에서 runner/catalog가 확인. |
| `dh.ModuleDefinition` | 객체명·종류·적용 source checksum·기대 engine definition byte 수/checksum. 실제 정의와 적용 기록을 대조하는 배포 metadata이며 SQL 본문 복사본이나 승인 기록이 아님. |

Character와 Progress의 두 `rowversion`은 각각 읽기 의존·조건부 변경의 **8-byte opaque token**이다. 숫자 순서나 시각으로 해석하지 않는다. `WriteSafeCheckpoint`는 Character token과 Progress 존재/token을 검사하고 필요한 Progress만 변경한다. 같은 safe 값이면 게임 UPDATE를 생략한다. `SavedUtc`는 진단값이며 token/fence를 대신하지 않는다.

모든 공개 RPC는 동일 `Dawnholder.Persistence.Slot.1`/public/Exclusive/Transaction application lock을 사용한다. [잠금 helper](../../99_Tools/database/modules/procedures/internal/lock_and_read_authority.sql)는 Authority를 먼저 읽고 게임 행 조회는 Account→Character→Progress 순서를 유지한다. acquire·recovery·release는 성공 시 Fence를 +1하며 감소·reset·재사용하지 않는다. owner 내 sequence와 최대값 overflow도 검사한다. 시간이 지나거나 process가 사라졌다는 이유로 권위를 빼앗지 않는다.

게임/authority 변경과 새 terminal 증빙은 같은 RPC transaction에서 확정한다. [receipt 조회](../../99_Tools/database/modules/procedures/internal/read_operation_receipt.sql)는 동일 ID의 Kind·PayloadVersion·payload 길이/bytes를 대조해 당시 결과를 replay하며, 다른 payload이면 기존 증빙을 변경하지 않는다. 응답의 `ResultSnapshot`은 과거 증빙, `Current*`는 해당 호출에서 캡처한 현재 진단이다. [결과 helper](../../99_Tools/database/modules/procedures/internal/emit_persistence_result.sql)는 public RPC의 COMMIT 뒤 추가 조회 없이 29열 한 행을 투영한다. 과거 Acquired replay만으로 현재 입장을 열 수 있는지는 후속 소비자가 owner/fence/sequence/tokens를 다시 대조해야 한다. 바인딩/fence/ledger의 자동 삭제·TTL·purge는 없으며 DB restore로 과거 상태로 돌리는 복구는 별도 승인/격리 영역이다.

이 절은 현재 SQL **소스 계약**이다. 실제 잠금·동시성·rowversion·replay·29열 provider metadata의 실행 검증은 남아 있다.

### 공개 RPC와 최소권한

[permissions.sql](../../99_Tools/database/modules/permissions.sql)은 아래 9개 객체에만 개별 EXECUTE를 부여한다. `dh_runtime`/`dh_recovery`에는 직접 테이블 DML/DDL·schema 전체 EXECUTE·내부 helper·codec 실행 권한을 부여하지 않는다. 정적 SQL과 동일 dbo 소유 체인을 전제로 하며 실제 principal의 상속 권한·직접 DML/타 역할 RPC 거부는 후속 실행 gate에서 확인한다.

| role | 공개 RPC | 책임 |
|---|---|---|
| `dh_runtime` | [ReadAdmission](../../99_Tools/database/modules/procedures/read_admission.sql) | 바인딩·배포 호환 정보·owner/fence 관측. 권위 획득/게임 load/증빙 쓰기 없음 |
| `dh_runtime` | [AcquireAndLoad](../../99_Tools/database/modules/procedures/acquire_and_load.sql) | Free와 관측 fence 대조, 없는 게임 행 생성, 저장 class 유지, runtime 권위 획득 |
| `dh_runtime` | [WriteSafeCheckpoint](../../99_Tools/database/modules/procedures/write_safe_checkpoint.sql) | owner/fence/next sequence·두 token을 검사한 제한 safe mutation |
| `dh_runtime` | [ReleaseRuntime](../../99_Tools/database/modules/procedures/release_runtime.sql) | 현재 runtime owner만 조건부 해제. 게임 데이터 변경 없음 |
| `dh_runtime` | [ResolveRuntimeOperation](../../99_Tools/database/modules/procedures/resolve_runtime_operation.sql) | runtime 종류의 같은 ID/typed payload 증빙 조회, absent이면 요청된 취소 seal 확정 |
| `dh_recovery` | [InspectRecovery](../../99_Tools/database/modules/procedures/inspect_recovery.sql) | 관리자가 정확 대상·owner/fence·게임 행 상태를 관측 |
| `dh_recovery` | [RecoverAndLoad](../../99_Tools/database/modules/procedures/recover_and_load.sql) | 관측한 owner/fence 대조와 사유 기록, recovery 권위 획득. runtime Ready나 safe load를 만들지 않음 |
| `dh_recovery` | [ReleaseRecovery](../../99_Tools/database/modules/procedures/release_recovery.sql) | 현재 recovery owner만 조건부 해제 |
| `dh_recovery` | [ResolveRecoveryOperation](../../99_Tools/database/modules/procedures/resolve_recovery_operation.sql) | recovery 종류의 증빙 조회와 absent 취소 seal |

runtime은 전용 SQL 로그인과 dh_runtime user로 만든다. recovery 주체는 사용자 결정 27 A로 후속 goal에서 정하며 이 도구는 만들지 않는다. dh_recovery 역할과 공개 권한은 유지한다. 실제 runtime의 권한·연결 시험과 recovery 실증의 미실행은 goal에서 구분한다.

기존 `Configure-WslAccess` login의 직접 SELECT/INSERT/UPDATE 권한을 새 runtime에 재사용하면 fencing을 우회한다. 기존 DB/credential/WSL 도구의 역사 기록을 현재 RPC 사용법으로 적용하지 않는다.

### SQL 소스 위치와 배포 책임

현재 정의와 immutable 적용 이력을 나눴다. [기술명세 §1.1](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md#11-migration-배포-형식)의 초기 procedure별 migration 배치는 현재 승인된 [구조·배포 설계](../../01_Phases/goals/2026-10-02-persistence-repository/sql-structure-design.md)와 아래 실제 소스로 갱신됐다.

| 위치 | 책임 |
|---|---|
| [modules/functions](../../99_Tools/database/modules/functions/) | typed 요청의 canonical payload codec `PersistencePayloadV1` 1개 |
| [modules/procedures](../../99_Tools/database/modules/procedures/) | 공개 RPC 9개. 입력·operation 고유 판정·상태 전이·transaction/오류 종결 소유 |
| [modules/procedures/internal](../../99_Tools/database/modules/procedures/internal/) | 잠금/Authority·계약 검사·receipt 조회·게임 행 조회·progress/snapshot 직렬화·receipt 기록·결과 투영의 helper 8개. 명명 scalar OUTPUT로 연결 |
| [modules/permissions.sql](../../99_Tools/database/modules/permissions.sql) | runtime5/recovery4 개별 grant의 반복 적용 |
| [modules/manifest.json](../../99_Tools/database/modules/manifest.json) | 정확 경로·객체명/종류·의존 순서·source/definition 기대값을 선언한 코드 묶음 |
| [migrations](../../99_Tools/database/migrations/) | 적용 이력: 001 initial, 002 persistence_metadata, 003 module_metadata, [004 module_release](../../99_Tools/database/migrations/004_module_release.sql)의 manifest hash 선언 |

manifest 순서는 codec 1개→내부 helper 8개→공개 RPC 9개→permissions다. DB 조회/쓰기 helper는 public RPC의 transaction 안에서 동작하고 자체 BEGIN/COMMIT/ROLLBACK을 소유하지 않는다. COMMIT 뒤 캡처 값만 투영하는 EmitPersistenceResult는 별도 결과 단계다.

source checksum은 UTF-8/BOM 제외·CRLF→LF 기준이며 module의 남은 CR은 거부한다. manifest 자체는 BOM 없는 UTF-8·LF·끝 개행을 고정한다. engine definition의 기대 byte 수/hash는 검토된 소스의 UTF-16LE/BOM 제외 값이고, 실제 `sys.sql_modules` 대조에서는 CR만 제거한다. **실제 DB hash를 읽어 정답으로 채택하지 않는다.** manifest는 자신과 004 선언을 목록에 넣지 않아 hash 순환을 피한다. `MigrationManifest`의 기존 정렬된 `{version,name,checksum}` 형식은 유지하고, 004 SQL의 checksum이 선언된 코드 묶음을 연결한다.

[Database.Common.ps1](../../99_Tools/database/Database.Common.ps1)은 Complete 승인 입력·정확 대상과 소스를 고정한 뒤 별도 migration lock 아래 전체 신규 migration·module 배포·권한·metadata·[strict catalog](../../99_Tools/database/verify-schema.sql)를 하나의 transaction으로 묶는다. 자체 소유 transaction은 오류 시 rollback/dispose하며 전달받은 transaction은 호출자가 종결한다. 파일 하나는 SQL batch 하나이며 GO 분할기를 사용하지 않는다.

[Module.Common.ps1](../../99_Tools/database/Module.Common.ps1)은 명시된 001~004/manifest/source를 검사하고 migration의 누락·미지/더 최신 버전·이름/checksum 불일치를 거부한다. 기존 module은 변경 source 적용 전에 등록 기록과 실제 객체 집합·정의·소유자/실행 컨텍스트를 대조하며, unchanged source도 검사한다. 첫 metadata 설치 외에는 비어 있거나 지워진 기록/객체를 자동 채택하지 않는다. 선언 hash가 맞는 변경 source만 적용하고 같은 묶음은 DDL을 생략하되 9grant와 최종 대조를 반복한다. 새 정의가 소스의 기대값과 맞아야 ModuleDefinition을 기록한다. drift를 덮어쓰거나 현재 선언과 다른 source를 자동 수선하지 않는다.

현재 reader/catalog는 SchemaVersion4·테이블8·module18·정확9grant 등 고정 계약을 요구한다. [Environment.Common.ps1](../../99_Tools/database/test-environment/Environment.Common.ps1)도 수명 상태별 001/정확4개 migration을 대조하며 **수명 manifest 자체의 schema는 2**이다. 향후 코드 변경은 새 버전 선언과 그에 맞는 소스·도구·catalog 검토가 필요하다. 과거 migration checksum 변경이나 오래된 checkout의 강제 덮어쓰기로 되돌리지 않는다.

이 배포 경계는 구현 및 자체 오프라인 모의 대조까지이며 실제 SQL 문법/compiler·CREATE OR ALTER 저장 정의/hash·catalog·권한·중간 실패 atomic rollback은 아직 실행하지 않았다. 예상 engine metadata가 다르면 원문을 보존하고 검토된 수정부터 진행한다.

### 오프라인 module 구조 검사

[Test-ModuleStructure.ps1](../../99_Tools/database/Test-ModuleStructure.ps1)은 고정 module 경로·객체명/종류·파일당 정의 하나와 공개 RPC의 정본 helper 직접 호출, 현재 소스의 hash 소비값을 읽기 전용으로 검사한다. 주석/리터럴을 제외한 제한된 lexer를 사용하며 입장 조회·관리 inspection·resolver의 서로 다른 책임을 별도로 등록한다. [ModuleHash.Common.ps1](../../99_Tools/database/ModuleHash.Common.ps1)이 소스 정규화·hash 계산과 소비 대조를 소유하며 bundle 읽기나 DB 연결을 호출하지 않는다.

```powershell
# Windows PowerShell 5.1에서 소스 구조·hash 소비값 검사. 생략 시 스크립트의 database 폴더.
# 상대 -DatabaseRoot는 PowerShell 세션의 현재 FileSystem 위치를 기준으로 해석한다.
./99_Tools/database/Test-ModuleStructure.ps1 -DatabaseRoot ./99_Tools/database -Json
./99_Tools/database/Test-ModuleStructure.ps1 -Json -Strict
```

`-DatabaseRoot`는 FileSystem provider 경로만 허용하며, 끝 구분자 유무와 관계없이 같은 트리를 검사한다. 다른 provider나 해석할 수 없는 경로는 `unavailable`/exit2다. 선택 root 자체가 junction이어도 그 트리를 검사하며, 하위 제품 입력의 junction/symlink는 따라 읽지 않고 검사 불가로 보고한다. `-Json`은 Status/DatabaseRoot/Scope/CheckedFiles/Issues/ViolationCount와 HashTargets/HashTargetCount/HashInspectionComplete/HashViolationCount를 출력한다. HashUnregisteredLiterals/HashUnregisteredLiteralCount와 HashLiteralInspectionComplete는 제품 전수 literal 검사 결과다. DatabaseRoot는 검사 대상의 정규화 절대 경로이며, 경로 해석에 실패하면 null이다. CheckedFiles는 실제 읽은 파일의 root 상대 경로를 중복 없이 담으며 사본에 존재하는 제품 입력에 따라 달라진다. 기본 출력은 root·등록 대상 수·완료 여부·hash 위반 수·미등록 수와 File/Line/Expected/Remediation 경고를 표시한다. `-Strict`는 배치·직접 호출 구조 위반을 실패 exit로 바꾼다. hash drift와 미등록 literal은 기본 모드에서도 실패한다.

hash 검사에는 `modules/manifest.json`, 정확 module SQL, `verify-schema.sql`, `migrations/001~004`와 `Module.Common.ps1`이 모두 필요하다. 현재 release의 명시 대상은 116개다. manifest의 source 19개·definition/bytes 각18개, catalog의 source/definition/bytes 각18개, migration checksum4개, raw manifest identity2개(004 선언·catalog release 검사), immutable001 guard1개를 대조한다. HashTargets는 대상마다 File/Line/Offset/Kind/Source/Observed/Expected/IsDrift와 구체적인 대체 안내를 제공한다. Offset은 SQL의 CRLF→LF 검사 text 또는 PS/JSON의 BOM을 제외한 decoded text에서 literal이 시작하는 0-based 문자 위치다. 등록 누락·중복·알 수 없는 형식이나 입력 부재는 검사 불가이며 대상0개를 정상으로 처리하지 않는다. DefinitionBytes는 JSON 숫자 토큰 전체가 canonical 비음수 int32(`0` 또는 0으로 시작하지 않는 정수, 최대2147483647)여야 한다. 소수·지수·문자열·음수·범위 초과를 정수 prefix로 채택하지 않는다. 유효 정수의 길이 차이는 drift다. manifest 항목의 형식/객체 오류는 `modules/manifest.json`, migration 읽기 오류는 해당 migration을 Issues.File로 보고한다.

미등록 검사는 선택 DatabaseRoot 아래 `.sql`/`.ps1`/`.psm1`/`.psd1`/`.json` 제품 파일을 읽는다. 저장소 원본이나 다른 영역으로 넘어가지 않는다. 코드의 명시 제외 목록에는 root 상대 `tests/` 하위만 있으며, 고의 실패 fixture·보호 SHA·known-answer를 제품 등록에 넣지 않는다. `tests-extra/`나 다른 경로의 `tests/`는 제외되지 않는다. 주석·문자열도 포함해 앞뒤 hex 문자가 없는 정확64자리 hex literal을 등록 target의 File/Offset/Observed와 대조한다. 이미 등록된 값의 복사라도 다른 위치는 미등록이다. HashUnregisteredLiterals는 실제 File/Line/Offset/Observed와 등록 안내를 제공한다. HashTargetCount는 등록 대상만, HashViolationCount는 등록 drift와 미등록 literal의 합이다. 파일 읽기/열거 실패나 하위 link로 필수 검사를 못 하면 검사 불가이며 완료로 보고하지 않는다.

manifest identity는 UTF-8 without BOM·LF·마지막 개행의 실물 bytes SHA256이다. module SourceChecksum과 migration checksum은 BOM 제외·CRLF→LF 원문의 UTF-8 hash이고, DefinitionChecksum/Bytes는 같은 정의의 UTF-16LE without BOM hash/byte 길이다. immutable001의 raw 보호 hash와 정규화 checksum은 의미가 다르다. manifest identity를 module source나 004 자체 checksum에 복사하지 않는다. 이 검사에서 source로 계산한 definition 기대값은 실제 엔진이 저장한 정의/hash의 증명이 아니다.

| 상태 | 뜻 | warning 파일럿 exit | Strict exit |
|---|---|---|---|
| `compliant` | 검사한 경로·정의·직접 호출 계약에 위반 없음 | 0 | 0 |
| `violation` (구조만) | hash 대조가 완료됐으며 배치·정의·직접 호출 위반이 있음 | 0, Issues로 위반 공개 | 1 |
| `violation` (hash 위반 포함) | 등록 소비값의 drift 또는 미등록64hex literal이 있음 | 1 | 1 |
| `unavailable` | 경로/접근·미종결 SQL·hash 입력 부재/형식/등록 오류 등으로 검사 불가 | 2 | 2 |

`unavailable`의 ViolationCount/HashViolationCount/HashUnregisteredLiteralCount는 null이며 통과가 아니다. HashInspectionComplete는 등록 대조와 제품 literal 검사가 모두 완료되어야 true이고, HashLiteralInspectionComplete는 전수 literal 대조가 끝났는지를 나타낸다. HashTargetCount는 오류 전까지 관측한 대상 수이고 완전한116개 검사를 뜻하지 않는다. 부분 목록이나 미등록0만으로 검사를 완료했다고 읽지 않는다. 예를 들어 누락된 `modules/procedures/internal/read_character_state.sql`을 복구하거나 공개 RPC를 등록된 목적 경로로 옮긴다. receipt helper 누락은 원래 receipt 단계에서 `EXEC dh.ReadOperationReceipt`를 명명 인자로 호출하도록 고친다. 주석에 helper 이름을 적어도 직접 호출로 세지 않는다.

hash drift는 Issues의 File/Line/Observed/Expected/Remediation을 따라 검토된 소스에 대응하는 값을 바꾼다. module 변경이면 manifest source/definition/bytes → manifest 실물 identity → 004 선언 → 004 자체 checksum과 catalog module/migration/release 소비값을 함께 갱신한다. 과거 이력이나 고의 실패 fixture를 새 값으로 덮지 않는다. 검사에는 자동 쓰기·수정·런타임 기대값 주입이 없다. 실제 DB의 unknown/drift 거부와 엄격 catalog 검사를 유지한다.

미등록 literal은 먼저 해당 파일/행과 의미를 검토한다. 필요한 소비처라면 ModuleHash.Common.ps1의 `Test-ModuleHashConsumers`에서 정본 source/definition/raw manifest identity와 연결해 `New-ModuleHashTarget`에 실제 File/Offset/Observed/Expected를 등록한다. 고정116 대상 계약과 독립 오프라인 검증 및 이 문서를 함께 검토·갱신해야 하며, 값만 허용 목록에 넣거나 관측값을 기대값으로 자동 채택하지 않는다. 불필요한 소비처라면 제거한다. 검사 대상/제외를 넓혀 위반을 숨기지 않는다.

검사는 SQL 문법·중첩/들여쓰기·타입·transaction/동시성·권한·29열 실행을 판정하지 않으며 실제 DB의 manifest/hash 배포 검사를 대신하지 않는다. 기본 exit0만으로 구조 violation을 PASS로 읽지 않는다. 배치·직접 호출은 warning 파일럿을 유지하며 CLI·독립 tests의 CI 연결과 SQLFluff 적용성/연결은 **첫 PR 병합 뒤 Rules와 별도 조율**한다. CI 연결·SQLFluff parse·실제 엔진 PASS를 이 검사에 합치지 않는다.

### 게임 정책과 후속 소비 계약

- [GameWorld](../../02_Server/GameServer/Loop/GameWorld.cs)의 `NextEntityId()`는 프로세스 메모리에서 증가하고 [GameSession](../../02_Server/GameServer/Sessions/GameSession.cs)은 재접속 때 새 entity를 만든다. 따라서 runtime EntityId를 DB PK로 쓰지 않는다. 후속 연동에는 서버가 확인한 AccountId/CharacterId와 세션 EntityId의 매핑이 필요하다. PlayerSnapshot도 현재 이 메모리 ID와 DB 계약을 구분한다.
- [CharacterClass](../../98_Shared/Protocol/CharacterClass.cs)는 Knight=0, Mage=1. [PlayerStats](../../98_Shared/GameData/Combat/PlayerStats.cs)의 공격력·방어력·이동/점프 속도는 Class로 다시 생성한다. 레벨·경험치·장비·재화·길드는 현재 코드에 저장할 계약이 없어 넣지 않았다.
- [PlayerSnapshot](../../02_Server/GameServer/Maps/PlayerSnapshot.cs)은 Position/CurrentHp/MaxHp와 불변 PlayerStats 정의를 캡처한다. [MapId](../../02_Server/GameServer/Maps/MapId.cs)는 Town=0, HuntingGround=1, BossRoom=2, Ending=3이다. 기존 컬럼은 더 넓은 checkpoint를 표현할 수 있지만, 선택된 첫 연동 범위에서는 동적 snapshot을 저장하지 않고 Town 안전 spawn과 저장 클래스의 기본 풀HP로 복귀한다.
- [QuestRegistry](../../02_Server/GameServer/Quest/QuestRegistry.cs)의 `_bossUnlocked`는 현재 **세션 한정** latch다. 기존 001의 BossUnlocked 컬럼은 보존하지만 첫 연동에서는 safe의 해금을 false로 구성하며 재접속에 latch를 복원하지 않는다. 기존 솔로 카운트와 파티 공유 카운트는 보스 처치 때 초기화되고 서로 수명이 달라 저장하지 않는다.

사용자가 선택한 첫 연동 범위는 개발 고정 계정1/캐릭터1, 최초 클래스 유지, Town 풀HP 복귀, quest/보스 해금 세션 한정이다. identity/class와 안전 checkpoint를 다루며 전투 위치/HP/해금, 파티·킬 수·적 인스턴스·입력/FSM/cooldown/위치 이력은 복원하지 않는다. 선택 범위는 [DB 설계 목표](../../01_Phases/goals/2026-09-29-persistence-design/goal.md), 상세 계약은 [저장소 기술 설계](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md)에 있다. SQL 구조는 구현됐으며 .NET 저장소와 실제 게임 저장/복원 연결은 후속 미구현이다.

DB의 좌표형은 C# float에 대응하는 real이다. SQL은 실제 지형을 모르므로 서버가 안전 spawn의 유한값/유효 위치를 검증한다. 이번 안전 projection은 Town/저장 클래스 기본 HP·MaxHp/해금false이며 runtime rawHP 전송 계약을 바꾸지 않는다. 클라이언트가 보낸 HP/좌표를 저장 권위로 쓰지 않는다.

게임 tick에서 DB 완료를 기다리지 않는다. 현재 PlayerStats는 불변 정의이며 후속 worker에는 identity/class·안전 projection과 작업 수명의 불변 값만 넘긴다. 첫 연동 범위는 create/load·권위 획득/해제·미확정 작업 종결이고 주기/매 logout 동일 값 쓰기나 Quest snapshot pipeline은 없다. 기존 [설계 계약](../../01_Phases/goals/2026-09-29-persistence-design/design.md)을 보존하며 저장소·owner lane·Host 종료·GameSession/Unity 연결은 후속 구현/검증이다.

Acquire는 없는 Progress만 안전 초기화하고 기존 전투 위치/HP/해금 값을 자동 UPDATE하지 않는다. 반환 safe와 storedProgress 진단이 달라도 자동 dirty flag를 만들지 않는다. 명시 `WriteSafeCheckpoint` 요청만 저장 class·Town 안전 좌표·기본 풀HP·해금false를 받으며 raw 전투 HP/위치·class 변경·quest 영속화를 허용하지 않는다. Character token/Progress token·owner/fence/sequence 검사를 우회하는 직접 UPDATE를 저장소 사용법으로 제공하지 않는다.

[기술명세 §6](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md#6-repository-결과와-d2d3-인계)의 `NoWriteNeeded`는 로컬 close 판정으로 **checkpoint RPC가 0**이며 필요한 조건부 release만 남는다. SQL `CheckpointNoChange`는 명시 RPC를 받은 뒤 게임 UPDATE 없이 **sequence와 terminal 증빙을 commit**한 결과다. 두 경우를 같은 저장 성공으로 처리하지 않는다.

[기술명세 §7](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md#7-timeoutcancel재시도)에 따라 dispatch 뒤 완전한 terminal receipt를 못 받으면 unknown이다. 후속 저장소는 lane을 멈추고 동일 ID/typed payload resolver로 조정하며 timeout/cancel/process 종료를 rollback·release 완료로 간주하지 않는다. 직접 ADO.NET·typed RPC와 SqlClient 버전 선택은 [기술명세 §1](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md#1-선택과-적용-경계)의 **후속 저장소 계약**이며 패키지 추가/restore·Windows/WSL .NET 실행 성공을 뜻하지 않는다.


## 역사 — Windows SQL Express와 WSL 접근

아래는 과거 실행 근거이며 현재 컨테이너 승인이나 실행 방법으로 쓰지 않는다. Windows SQL Express·옛 시험 DB는 사용자 결정(컨테이너 범위 질문 5 A)으로 보존하며 이번 전환에서 정리하지 않는다. 옛 도구·명령·설명은 모두 **commit cc20d428**의 [database 폴더 전체](https://github.com/bass131/dawnholder-server/tree/cc20d428/99_Tools/database)와 [당시 MSSQL 안내](https://github.com/bass131/dawnholder-server/blob/cc20d428/00_Document/operations/MSSQL.md)를 기준으로 읽는다.

2026-09-29 구성은 YYH_Desktop\SQLEXPRESS, Express 17.0.1000.7, Dawnholder_Dev였다. Windows 도구는 Windows 통합 인증/lpc, native WSL은 전용 SQL 인증/tcp:127.0.0.1,14330을 썼다. [당시 목표와 실행 근거](../../01_Phases/goals/2026-09-29-mssql-setup/goal.md)는 당시 001 구성·rollback·최소권한 시험의 기록이다. 현재 001~004/module/RPC·게임 저장 연결의 통과 근거가 아니다. 기존 GameDB·BaseballData·Northwind는 별도 자원이다.

### 2026-09-29 Enable의 되돌리기 경로

삭제한 Configure-WslAccess/Test-WslAccess의 Restore 경로를 이 절에 먼저 보존한다(사용자 결정 29 A). 대상 상태는 %LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS\state.clixml의 Restored=false, LoginMode **1→2**, loopback TCP **14330**, 로그인 **Dawnholder_Dev_Wsl_688ea8c6f1bd**다. 현재 도구가 이 경로를 읽거나 수정하지 않는다.

되돌릴 도구는 [cc20d428의 Configure-WslAccess.ps1](https://github.com/bass131/dawnholder-server/blob/cc20d428/99_Tools/database/Configure-WslAccess.ps1) -Action Restore다. 같은 폴더 Database.Common.ps1을 dot-source하므로 **그 commit의 99_Tools/database/ 폴더 전체**를 별도 임시 위치에 꺼낸다. 예시는 별도 승인된 점검 시간에만 수행한다.

~~~powershell
# 역사 복구용 전체 checkout 예시; 현재 goal에서 실행하지 않는다.
git worktree add --detach <임시 경로> cc20d428
# 같은 Windows 계정의 관리자 PowerShell, 영향 확인·서비스 점검 시간 별도 승인 뒤:
& '<임시 경로>\99_Tools\database\Configure-WslAccess.ps1' -Action Restore
~~~

Restore는 기록된 원래 설정을 복구하고 SID가 맞는 신규 login만 비활성화하며 데이터·DB user·암호화된 복구 파일을 삭제하지 않는다. 다른 관리자가 값을 바꿨으면 덮어쓰지 않고 검토한다. 당시 Restore의 실제 실행 검증은 미실행이다. 옛 Windows 시험 DB의 장래 정리도 cc20d428 도구와 별도 승인을 사용한다.

역사 절의 나머지 명령도 현재 파일 경로를 실행하지 않는다: Plan/Enable/Restore는 위 cc20d428 Configure-WslAccess, native WSL probe(-Distribution Ubuntu)는 [cc20d428 Test-WslAccess](https://github.com/bass131/dawnholder-server/blob/cc20d428/99_Tools/database/Test-WslAccess.ps1), Install/Test-Database 및 Get-Service/Start-Service 인계는 [cc20d428 MSSQL 안내](https://github.com/bass131/dawnholder-server/blob/cc20d428/00_Document/operations/MSSQL.md)의 해당 예시를 따른다. 서비스의 Manual 설정, D:의 옛 DB, VHDX, registry와 DPAPI 자료는 그대로 보존한다.
