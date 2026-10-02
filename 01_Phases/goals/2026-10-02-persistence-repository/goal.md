# P3 / D1b — 영속성 저장소와 격리 SQL 검증

상태: **G0 기술 범위 승인, 신규 Sol의 A1 SQL 구현 진행; G1 외부 변경은 사용자 응답 대기**. 메인 `msg_78c5c1647b7c`로 독립 저장소·schema·수명 도구·최소 recovery 콘솔/launcher 범위가 승인됐다. DB 접속·생성·변경과 계정 생성은 아직 금지다. 구현 완료나 독립 검증 판정은 아직 없다.

## 착수 근거와 기준점

- 메인 Claude `msg_31a50ce2b7ab`(2026-10-02 04:31:28 UTC): D1b 초안과 사용자 안건 2~3개를 요청했다. 메인 전달 사용자 결정은 **전용 시험 DB를 만들어 시험 후 폐기하며 기존 Dawnholder_Dev는 사용하지 않음**이다. 저장 연동·아키텍처 뷰·운영툴 시스템카드는 병행하고 Q-1B 및 그 시작 시 Windows 실검증(3A)은 뒤로 미뤘다. 전달 결정을 사용자 직접 입력으로 격상하지 않는다.
- 작업 경로 `C:/Dev/DawnHolder_Project`, branch `feat/persistence-repository-d1b-20261002`, base `27f1f57374c91b723127830cf16f6613ea32ebf1`. `git fetch origin main` 뒤 clean 상태에서 새 branch를 만들었다. 이전 Q-1A branch·archive·stash 2개와 Unity 사용자 설정을 보존한다.
- 출발점은 [D1a goal의 D1b 인계](../2026-10-01-persistence-technical-design/goal.md), [기술 명세](../2026-10-01-persistence-technical-design/technical-spec.md), [SQL 반증 계획](../2026-10-01-persistence-technical-design/verification-plan.md), [D0 결정](../2026-09-29-persistence-design/goal.md), [P0 DB·소유 계약](../2026-09-30-contracts-baseline/contracts.md)이다. 이전 정적 PASS와 과거 ODBC 성공을 D1b 실행 근거로 재사용하지 않는다.
- 새 GameDev Astra handle `term_20caf529-b751-406a-b539-3cbcacf7a8b7`, incarnation `654eee24-028f-4c10-ad1e-7faf93879b09`, runtime `8a673084-6819-45b9-a551-347226cdce9b`. 메인 지정 역할은 Astra, 화면은 `GPT-6-Astra xhigh`, 최초 기동 argv는 이 세션에서 미확인, backend 실제 모델은 `unknown`이다. READY는 `msg_a6c75282a312`로 보고했다. 이전 Run/Task/Dispatch는 실행 권한으로 쓰지 않는다.

## 목표와 보존 경계

**목표:** D1a의 authority/fence·두 rowversion·operation 증빙·unknown 조정 계약을 신규 migration과 .NET 저장소로 구현하고, 정확히 승인된 시험 DB에서 독립 SQL 반증 근거를 남긴다. 다음 D2가 게임 actor와 분리된 API를 받아 연결할 수 있어야 한다.

- 고정 계정/캐릭터 1개, 최초 저장 class 유지, 재접속 Town 안전 spawn·기본 풀HP, quest/보스 해금 비영속을 유지한다. legacy Progress를 acquire/정상 close에서 자동 UPDATE하지 않는다.
- 모든 writer·resolver·recovery는 동일 slot lock과 DB transaction을 사용한다. runtime은 recovery 권한·직접 테이블 DML을 갖지 않는다. timeout·프로세스 부재·취소를 rollback/권위 해제 증거로 삼지 않는다.
- `001_initial.sql`과 적용 checksum은 고정한다. 기존 `Dawnholder_Dev` 및 선택한 시험 DB 밖 모든 DB는 접속·변경·fixture·폐기 대상에서 제외한다. 단, 대상 존재 확인과 승인된 login 생성/정리에 필요한 로컬 instance의 `master` 메타데이터 작업은 별도 승인 범위로 명시한다.
- D2 bounded worker/Host 종료 연결, D3 Loading/packet/Unity 적용, D4 실제 재접속·플레이는 구현하지 않는다. 정상 close의 `NoWriteNeeded`는 checkpoint RPC 0인 소비 계약으로 인계하며 SQL `CheckpointNoChange`와 구분한다.
- GameServer/Unity/공유 프로토콜의 기존 동작과 사용자 Unity 3파일·`.meta`·GUID·stash를 보존한다. Management 복구 command·운영 UI, 정식 인증·경제 데이터, 주기 저장·매 logout UPDATE, 자동 ledger purge/TTL takeover는 범위 밖이다.

## 원천 조사 결과

아래는 base 소스 대조이며 현재 SQL instance·설치·권한을 조회한 결과가 아니다.

| 원천 | 확인한 사실과 D1b 영향 |
|---|---|
| `99_Tools/database/Install-Database.ps1:4`, `Test-Database.ps1:4` | `-Database` 생략 시 환경변수 또는 `Dawnholder_Dev` 선택. D1b는 모든 DB 도구에 승인된 정확 이름을 명시하고 실행 전 그 값과 대상 identity를 기록해야 한다 |
| `Database.Common.ps1:4–24`, `Install-Database.ps1` | 기존 도구는 로컬 shared memory·Windows 통합 인증·MachineName/owner marker 검사·Encrypt/TrustServerCertificate=true를 사용. 새 DB 생성은 migration transaction 밖이며 생성/marker 사이 실패는 자동 채택하지 않음 |
| `Database.Common.ps1:71–101`, `verify-schema.sql:22` | 파일 하나=SQL batch 하나, 전체 신규 migration+catalog 단일 transaction. 기존 catalog 열 20개 등 고정 검사는 신규 metadata를 추가하면 갱신해야 함 |
| `Configure-WslAccess.ps1:7,76–88,94`, `Test-WslAccess.ps1:9,46` | 기존 경로는 Dawnholder_Dev/14330에 고정되고 legacy login에 직접 SELECT/INSERT/UPDATE를 부여함. Enable은 registry 변경·SQL service restart도 포함. D1b 전용 principal 도구로 그대로 재사용하지 않음 |
| `rg --files 99_Tools/database`와 `rg -n 'DROP\s+DATABASE'` (`02_Server`, `99_Tools`의 SQL/PS1/SH/프로젝트 파일) | 해당 검색 범위에 DB DROP 도구 없음. 시험 DB 폐기 도구/절차의 작성·검토·실행 소유를 새로 배정해야 함 |
| `00_Document/FEATURE_MAP.md:13`, `02_Server/GameServer/Maps/Actions/` | `Maps/States/Actions/` 오기를 확인. D1b 기능 완료와 무관하므로 이 branch에서는 수정하지 않고 별도 작은 문서 PR 후보로 메인에 보고 |

## 사용자에게 올릴 세 가지 안건 — 모두 제안, 미승인

전용 DB 사용·시험 후 폐기라는 방향은 다시 묻지 않는다. 아래는 정확한 외부 변경 대상과 승인 단위다. 메인이 사용자 응답과 원문 message를 이 절에 반영한 뒤 실행 gate를 연다.

| 안건 | 추천하는 구체 범위 | 다른 선택의 영향 |
|---|---|---|
| **1. 시험 DB의 한 번 수명** | 로컬 `.\SQLEXPRESS`의 **`Dawnholder_Dev_D1b_20261002` 하나**를 신규 생성→001 및 002+ 적용→반복 fixture/실제 commit/임시 시험 trigger 설치·제거→최종 폐기까지 한 번 승인. 기존 동명 DB가 있으면 중단하며 자동 채택·DROP·재생성하지 않음. 동일 DB 안 시험 전용 행 reset은 허용하되 binding/fence/ledger 보존. 마지막 DROP은 증거 보존·모든 연결/operation 정산·정확 identity 재확인 뒤에만 실행 | 생성·시험까지만 승인하고 DROP은 최종 상태를 보고 별도 승인받을 수도 있음. 어느 방식이든 새 이름/두 번째 DB·폐기 후 재생성은 승인 범위 밖 |
| **2. 시험 전용 계정 두 개와 자격증명 수명** | 로컬 Windows **`YYH_DESKTOP\dh_d1b_recovery`**(비관리자, SQL `dh_recovery`만)와 SQL login **`dh_d1b_runtime_20261002`**(해당 DB `dh_runtime`만)을 생성·매핑. 동명 계정은 채택하지 않음. 관리자만 승인된 provision/launcher를 실행하며 runtime 비밀은 목표 전용 ACL+DPAPI 저장소에서 자식 stdin/pipe로 전달. recovery는 그 Windows 계정의 실제 통합 인증 실행. 시험 종료 후 이번에 만든 SQL login/Windows 계정과 목표 전용 credential만 SID·manifest 대조 후 정리하는 수명까지 함께 승인 | 사용자가 제공하는 기존 전용 계정을 쓸 경우 정확 이름·상속 권한을 새로 확인하며 기존 계정 삭제/권한 확대는 하지 않음. 관리자 sysadmin을 recovery 양성 시험의 대용으로 쓰지 않음 |
| **3. 격리 로컬 시험의 TLS 예외** | 위 DB에 대한 로컬 shared memory 관리 연결 및 **`tcp:127.0.0.1,14330`**의 Windows/WSL 시험 연결에 한해 `Encrypt=Mandatory`(기존 관리 도구는 Encrypt=true), **`TrustServerCertificate=true`**를 명시 허용. SQL instance/endpoint를 실행 직전 확인하고 자동 fallback 없이 기록. 기본 저장소 설정은 false, 시험 예외는 D1b 종료와 함께 끝남 | 인증서 검증을 유지할 수도 있으나 검증 가능한 인증서가 없으면 연결 시험이 차단됨. 인증서 설치·신뢰 저장소 변경은 이 초안의 허용 작업에 포함하지 않음 |

TLS 예외는 암호화를 유지하지만 서버 인증서의 신원을 검증하지 않는다. 위 계정의 비밀번호는 사용자 답변·채팅·goal·명령 인수·로그에 적지 않는다. DB 제거가 server login/Windows 계정·credential 제거를 대신하지 않으므로 각각 기록한다. 계정 profile의 임의 재귀 삭제·다른 DPAPI 파일 읽기/변경은 포함하지 않는다. SQL service·registry·방화벽·인증 모드·SAC·Claude 권한 설정의 변경, 기존 principal의 revoke/disable은 세 안건의 범위 밖이다.

## G0 승인과 Windows recovery 실행 설계

- 메인 `msg_78c5c1647b7c`(2026-10-02 04:46:08 UTC)는 기술 항목 1~5를 승인했다. `Microsoft.Data.SqlClient 6.1.7`의 추가/restore는 D1a 결정의 이행이며 별도 사용자 재승인 대상이 아니다. 공식 지원/patch를 다시 확인하고 다른 version/패키지가 필요하면 메인에 올린다. R-7 Fable 시범은 이 목표에 적용하지 않는다.
- G1 응답 전에도 **DB 접속 없는 A 구현**을 진행한다. A를 A1(SQL schema·RPC·catalog)→A2(승인 대상/identity/계정·DB 수명 도구)로 좁혀 신규 Sol 세션을 순차 배정한다. G0가 DB/계정/비밀 실행권을 열지는 않는다.
- [ADR-029](../../../00_Document/ADR/harness/ADR-029-wsl2-dotnet-execution-standard.md)의 SAC `0x800711C7` 과거 관측과 현재 DEVELOPMENT를 따른다. Windows 최소 콘솔의 실제 실행이 막힐 수 있으므로 B의 launcher에 **Windows PowerShell + 기존 System.Data.SqlClient**로 동일 4개 RPC를 호출하는 제한된 script 경로를 구현한다. 새 DLL/Add-Type 컴파일 없이 `CommandType.StoredProcedure`와 명시 SqlDbType/길이를 쓰고 .NET 콘솔과 SQL 계약을 공유한다.
- 복구 script는 전용 recovery Windows 계정의 실제 실행 컨텍스트에서만 통합 인증한다. 정확 manifest의 DB/slot/GUID와 expected owner/fence/reason을 받으며 임의 SQL/범용 RPC/임의 endpoint·credential을 받지 않는다. parent 관리자와 child recovery identity를 혼동하지 않고 실제 Windows SID·ORIGINAL_LOGIN·서버/DB role 및 상속 권한을 검증한다. 자격증명은 argv/보고에 넣지 않으며 기존 runtime credential을 사용하지 않는다.
- G2에서 launcher 및 script의 실제 diff/경로/ACL·입력·권한 검사를 먼저 실사한다. signed Windows PowerShell 경로를 사용한다고 script 실행 성공을 가정하지 않는다. PowerShell 경로도 정책에 막히면 정확 오류·미실행을 보고하며 SAC 끄기·정책 변경·Unblock·신뢰 DLL 복사로 우회하지 않는다. 이 경로로 S06이 성공해도 Microsoft.Data.SqlClient/net10 Windows 실행(S07) 성공으로 합치지 않는다.
- FEATURE_MAP 오기는 메인의 별도 작은 문서 PR 지시를 받았다. D1b 구현의 독립 대기 구간에 Architecture 파일 소유를 확인한 뒤 별도 branch·외부 작성/실사로 처리하며 D1b diff에 섞지 않는다.

## 메인과 Astra가 정할 기술 항목

사용자 안건과 분리한다. 아래 제안은 메인 goal 확인 뒤 세부 manifest/spec으로 고정하며, 구현자가 사용자 정책을 추측해 바꾸지 않는다.

1. **독립 라이브러리:** `02_Server/Persistence/`의 net10 저장소 프로젝트와 별도 `Persistence.Tests`를 둔다. GameSession/GameWorld/Host를 참조하지 않는다. immutable options/request/token/result·typed SQL adapter·별도 recovery interface를 나눈다. GameServer composition 연결은 D2다. `Microsoft.Data.SqlClient 6.1.7`은 D1a 선택값이며 구현 착수 시 공식 지원/patch를 다시 확인하고 고정 version을 기록한다. 아직 restore/설치하지 않는다.
2. **배포 단위:** 002 `persistence_metadata`(Authority/Operation/role), 003 `persistence_payload`(codec 함수 하나), 004 `read_admission`, 005 `acquire_and_load`, 006 `write_safe_checkpoint`, 007 `release_runtime`, 008 `resolve_runtime_operation`, 009 `inspect_recovery`, 010 `recover_and_load`, 011 `release_recovery`, 012 `resolve_recovery_operation`, 013 `persistence_grants`를 제안한다. 실제 파일은 `NNN_name.sql`. 각 function/procedure/시험 trigger 파일은 CREATE 하나만 둔다. 추가 함수가 필요하면 적용 전에 Astra가 번호·의존 목록을 다시 고정한다. GO 분할기·동적 runtime SQL·login/password를 migration에 넣지 않는다.
3. **대상 강제와 정리:** D1b 진입 도구는 필수 `-Database`·목표 manifest를 받고 빈 값/기본 게임 DB/불일치를 SQL 접속 전에 거부한다. 기존 Install/Test 호출에도 값을 명시하며 환경변수 fallback으로 실행하지 않는다. 폐기는 preview→정확 대상 확인→실행 경로로 분리하고 owner marker에 더해 instance/DB 식별 정보·생성 기록을 대조한다. prefix 일괄 DROP, 강제 SINGLE_USER/ROLLBACK IMMEDIATE, 다른 세션 kill은 사용하지 않는다. 남은 연결·Unresolved가 있으면 보존 후 보고한다.
4. **설정·권한·실행자:** endpoint/SQL ProductVersion·collation·RCSI·migration hash, slot1/고정 시험 GUID, 두 principal의 SID·실제 ORIGINAL_LOGIN/role/group, legacy writer 부재를 승인 후 읽기 gate에서 기록한다. 시작 시 ProductVersion까지 비교하며 patch가 달라도 golden vector 재검증 없이는 재개하지 않는다. 시험 관측/fixture용 관리자 연결과 runtime/recovery 연결을 분리한다. GUID·operation/owner manifest·실행 시간 창은 Astra가 고정하며 승인 DB 밖으로 확장하지 않는다.
5. **관리 도구의 이번 범위:** D1a S06을 실행할 수 있도록 `99_Tools/PersistenceRecovery/`와 전용 Windows launcher에 4개 RPC(Inspect/Recover/Release/Resolve), 정확 DB/slot/GUID, reason·관찰 fence/owner만 노출하는 최소 콘솔 도구를 배정한다. 임의 SQL/다른 DB 선택/서버 앱 credential을 받지 않는다. 정식 배포·Management 화면은 후속이다. 실제 Windows principal로 실행할 수 없으면 S06을 미실행으로 둔다.
6. **실행 환경 판단:** Windows+WSL .NET driver RPC와 실제 Windows recovery principal 시험을 목표로 한다. Windows SAC 제약은 이 D1b에도 영향을 줄 수 있으므로 정확 차단 오류를 기록하고 메인에게 허용 실행 환경/범위 판단을 올린다. Q-1B의 3A 연기를 D1b Windows 시험 통과·면제로 해석하지 않는다. WSL 성공으로 Windows PASS를 대신하지 않는다.

## 작업 단위와 파일·실행 소유

현재 Astra 쓰기는 이 goal·CURRENT·로드맵 링크와 로컬 근거뿐이다. 아래는 **goal 확인 후 발행할 범위안**이며 동시 쓰기 권한이 아니다. 단계마다 정확 파일 목록을 task spec에 고정하고 같은 시간 외부 작업자는 하나만 둔다.

| 단위 | 작성/쓰기 담당 | 산출물·경계 |
|---|---|---|
| A. schema·배포/수명 도구 | 신규 GameDev Sol (`gpt-6.1-sol`) | `99_Tools/database/migrations/002_*.sql` 이후 명시 목록, `verify-schema.sql`, 필요한 `Database.Common.ps1`/`Install-Database.ps1` 경계 변경; 목표 전용 provision/initialize/drop 도구. 기존 WSL Enable/Restore는 변경·실행하지 않음 |
| B. repository·제한된 recovery 도구 | A 정산 후 신규 GameDev Sol | `02_Server/Persistence/**`, `99_Tools/PersistenceRecovery/**`, 목표 전용 Windows/WSL secret launcher; `Dawnholder.slnx`의 새 프로젝트 등록. 필요 시 `99_Tools/sync-wsl.sh`는 새 프로젝트 동기화만. Host/게임/프로토콜/Unity/기존 legacy credential은 쓰지 않음 |
| C. 독립 실사·테스트 | 모든 제품 쓰기 종료 후 신규 GameDev 검증자 (`claude-opus-5-5`) | `02_Server/Persistence.Tests/**`, 기존 `99_Tools/database/Test-Database.ps1`의 신규 schema 회귀, `99_Tools/database/tests/**`의 독립 fixture/trigger/fault 장치만 작성. `.backups/verification/2026-10-02-persistence-repository/verification-N/`에 판정 원문·실행 근거. 제품 결함은 D1B-01… 번호로 반환 |
| D. 승인된 외부 구성·시험 | Astra가 단일 실행 소유를 할당 | 관리자 provision/cleanup은 검토된 script를 실행할 정확 Windows 관리자와 세션을 실행 전에 기록. 독립 검증자는 할당 시간 창에만 SQL fixture/시험 실행. 구현자와 검증자의 DB 변경을 겹치지 않음. 별도 OS elevation/UI가 필요하면 메인에게 상태와 실행 절차를 전달 |
| E. 결과·Git·PR | GameDev Astra | goal에 결과·결정·근거, 필요한 MSSQL 안내·D2 인계 정리. commit/push는 Astra 하나만, PR 생성 허용·각 PR 병합은 사용자 명시 승인 후 |

검증자는 구현자 보고/실제 diff/실행 근거부터 대조하고 요구사항으로 독립 테스트를 작성한다. 구현자 smoke는 독립 PASS를 대체하지 않는다. 수정 Sol과 재검증 Opus는 매번 새 외부 세션이며 같은 결함이 3회 재검증에 실패하면 메인에 보고한다. 작업자는 추가 위임하지 않고 작업 하나 뒤 정산·release·동일 pane 확인 후 종료한다. 지정 모델 불가·첫 화면 모달·기동 실패는 대체/우회 없이 메인에 보고한다. 실제 Run/Task/Dispatch·요청/기동/화면 모델·backend unknown·receipt는 발행 시 기록한다. Fable goal 검토는 메인이 이 목표를 R-7 대상으로 지정한 경우에만 수행한다.

`AGENTS.md`·`ORCA.md`·`RESUME.md`는 Architecture 0단계 소유, `CLAUDE.md`는 메인 소유, `05_Management`는 Management 소유다. GameDev는 이 파일들을 수정하지 않는다. Architecture와 `msg_7bf483f3aa4a`로 비충돌 범위를 공유했다. 포트7777·서버/봇·DB 실행 소유는 GameDev지만 D1b는 게임 서버/봇 실행을 요구하지 않는다.

## 실행 gate와 관찰 가능한 완료조건

| gate | 통과 조건 |
|---|---|
| G0 범위·설계 | 메인 goal 확인, 파일/도구/독립 테스트 소유와 실행 환경 기준 확정. 라이브러리·최소 recovery tool 범위 승인 뒤 Sol 발행/패키지 추가 가능 |
| G1 외부 변경 권한 | 위 사용자 세 안건의 정확 승인 message와 대상/수명 기록. DB명·계정·TLS 값이 미정이면 해당 외부 작업은 시작하지 않음 |
| G2 최초 실행 전 | 실행할 provision/install/cleanup script의 실제 diff와 부작용을 신규 Opus가 정적 검토. 승인된 이름·manifest·비밀 경로·executor·ProductVersion/endpoint 확인 계획을 대조. 관리자 구성 후 실제 SID/권한/DB identity 확인 없이 runtime 시험하지 않음 |
| G3 독립 구현 검증 | D1a 검증 계획의 S01–S11, C01–C13, O01–O07, U01–U11, R01–R05를 아래 경계로 판정. 제품 결함 0, 미실행은 환경·영향을 적고 메인 판단 없이 완료로 합치지 않음 |
| G4 정산·인계 | 원시 증거·manifest 보존, operation/owner/connection 정산, 임시 trigger/권한/도구 프로세스 잔여 확인. 승인된 DB/계정/credential 정리 결과 및 미정리 항목 명시. D2 소비 API·미실행 D2/D3/D4 인계. PR 승인 전 최종 판정 원문과 원천 표본을 메인에게 전달 |

G2가 제품 전체 구현보다 먼저 필요하면 A 쓰기 종료 후 정적 실사 Opus를 별도 신규 세션으로 발행·종료하고 B를 진행한다. 최종 C 검증자와 재사용하지 않는다. G0로 승인된 소스 구현과 고정 driver 추가/restore는 가능하지만, G1·G2 전에는 DB 연결·비밀 읽기·관리 변경을 실행하지 않는다.

독립 검증의 핵심 관측은 다음과 같다. 자세한 자극과 기대값은 D1a 검증 계획의 ID를 정본으로 사용한다.

- **schema/권한:** 001/checksum·기존 게임 행 보존, 002+ 재실행·중간 실패 rollback·catalog 변조 거부. 두 실제 principal의 정상 RPC와 직접 DML/DDL·교차 role RPC 거부. 각 시험 trigger는 단일 CREATE 파일이며 즉시 제거 후 잔여를 조회한다.
- **원자성/codec:** 최초 생성 3행·authority·증빙 원자성, token별 conflict와 alias 방어, 종류/입력별 결과·증빙 유무·검증 순서, 저장 class와 intendedClass 구분, ProductVersion별 golden vector 및 fresh/replay JSON float roundtrip. 잘못된 입력을 거부하기 전에 Account를 INSERT하지 않는다.
- **경쟁/unknown:** 실제 독립 SqlConnection A/B와 관측/barrier 연결로 old-first/new-first·stale release·늦은 acquire·seal 경쟁을 증명한다. receipt를 버리는 시험과 실제 transport 차단을 분리하며 단계 위치를 관측하지 못했으면 미실행이다. 부분 result·cancel·timeout을 성공/rollback으로 판정하지 않는다.
- **단계 한계:** O06의 기존 Ready/게임 동작, U01의 Ready0, R02의 client Ready 미사용은 D1b에서 저장소 권위/반환 계약까지만 확인한다. 실제 actor/Host/Unity 연결은 D2/D3 검증이다. L01–L04·E01–E04 전체 통과를 D1b 결과에 포함하지 않는다.
- **실행 근거:** 명령·exit·시각·OS/SDK/package/native dependency·코드 SHA·DB identity·before/after 행/token/proof·fault 지점·실제 commit 여부를 원문에 남긴다. Windows/WSL, 정적/fake/실제 SQL, CI와 플레이를 각각 구분한다. 비밀/전체 connection string은 기록하지 않는다.

빌드/서식 검사는 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)의 SDK10.0.301·원본 경로별 WSL 복제·lock·DLL 복사 부작용·환경 격리를 따른다. Windows 차단을 영구 정책 변경으로 우회하지 않는다. 프로세스 한정 PowerShell `-ExecutionPolicy Bypass`가 필요하면 실행 근거에 남긴다. Unity·서버·DB 검증을 수행하지 않았으면 PASS로 보고하지 않는다.

## 현재 결과와 다음 경계

- 원천 조사, 세션/branch 준비, 이 goal 초안과 CURRENT·로드맵 링크 작성을 수행했다. 작성자 점검에서 로컬 Markdown 링크가 모두 존재하고 Unity 사용자 3파일 SHA256이 진입 기준과 같음을 확인했다. DB/계정/서비스/패키지/비밀/제품/테스트/Unity 변경은 없고 외부 작업자도 아직 없다. 독립 판정 원문은 **아직 없음**이다.
- 로컬 근거 위치: `.backups/verification/2026-10-02-persistence-repository/`. `kickoff-messages.json`, `astra-show.json`, `unity-baseline-hashes.json`에 진입/전달/보존 근거를 둔다. 이 폴더는 Git 제외 자료다.
- 초안 commit은 `dfa9526`이며 이후 G0 승인과 실행 대안을 이 goal에 반영했다. 다음은 A1/A2의 DB 접속 없는 구현과 G1 사용자 결정 수신이다. 새 DB/계정 생성이나 실행 범위를 암묵적으로 넓히지 않는다.

## G0 이후 실행 기록

- 2026-10-02 공식 재확인: [Microsoft 지원표](https://learn.microsoft.com/en-us/sql/connect/ado-net/sqlclient-driver-support-lifecycle?view=sql-server-ver17)는 6.1 LTS 최신 patch를 **6.1.7**, 지원 종료를 2028-08-14로 표시한다. [NuGet 6.1.7](https://www.nuget.org/packages/Microsoft.Data.SqlClient/6.1.7)은 실제 패키지와 .NET8+ 및 계산된 net10 호환을 제공한다. 승인된 6.1.7을 유지한다. 아직 이 프로젝트에 package/restore를 실행한 결과는 아니다.
- 새 Run `run_5caa3033b174`, A1 Task `task_1df2b9371a5d`, Dispatch `ctx_4b0c8c812eeb`. 작업자 handle `term_46b65ef5-49f6-4d60-bb4b-c32e545b6bbb`, incarnation `d0e34ecb-8993-4086-b083-45750f640086`. 요청/최초 split 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 화면 `GPT-6.1-Sol xhigh`, backend `unknown`. Codex CLI0.160.0. 담당 Astra 아래 새 vertical pane의 빈 정상 prompt·경로·모델과 tui-idle satisfied를 확인한 뒤 최초 attach했다.
- `a1-start.json`에 state ready, `input_accepted`와 `turn_started` receipt를 보존했다. attach의 launch model=null은 모델 판정에 쓰지 않는다. `a1-spec.md`는 002~013 SQL과 catalog만 쓰기 허용하며 모든 DB 연결·비밀·설치·빌드·테스트 파일 쓰기를 금지한다. A2/검증자는 A1 정산 후 새 세션으로 발행한다.
