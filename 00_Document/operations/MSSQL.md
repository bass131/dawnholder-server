# 로컬 MSSQL 개발 DB

`99_Tools/database`는 Windows SQL Express의 별도 개발 DB를 설치하고 검증하는 도구다. **게임 서버의 로그인·저장·재접속 복구는 아직 연결하지 않았다.** 서버가 실행 중이어도 이 테이블에는 자동 저장되지 않는다.

## 현재 SQL 구조와 실행 경계

현재 database 설치·수명 도구와 오프라인 구조 검사·tests의 실행 host는 **Windows PowerShell 5.1 (`powershell.exe`)**이다. PowerShell 7에서 소스를 분석하는 CodeRules와 구분하며, 그 분석 결과를 제품 도구의 PowerShell 7 실행 지원으로 해석하지 않는다.

현재 [SQL 구조·오프라인 검증 목표](../../01_Phases/goals/2026-10-02-persistence-repository/goal.md)의 첫 PR 범위는 SQL/PowerShell 구조·배포·구조 검사와 이 안내, 신규 Opus의 합동 오프라인 검증이다. `001_initial.sql`을 보존하고 `001~004`/SchemaVersion4와 현재 `modules/` 구조를 구현했으며 독립 검증은 대기 중이다. 설계와 구현 경계는 [SQL 구조·배포 설계](../../01_Phases/goals/2026-10-02-persistence-repository/sql-structure-design.md)에 있다. **실제 새 DB 설치·SQL 접속·G2·계정/ACL/DPAPI·Windows recovery 실행과 저장소/GameServer 연결은 미완료**이며 첫 PR 병합 뒤 후속 goal로 이어간다.

정확 시험 DB·두 principal·실행 주체·한 번의 생성부터 폐기까지 수명은 [goal의 G1 승인 범위](../../01_Phases/goals/2026-10-02-persistence-repository/goal.md#사용자-세-안건--추천안-승인g1)를 따른다. 후속 goal 분리는 새 DB나 두 번째 수명 승인이 아니다. 이 문서의 도구·명령 설명도 SQL 접속이나 시스템 변경 권한을 추가하지 않는다. 아래 2026-09-29 성공 기록은 당시 001 구성의 근거다.

현재 installer는 기본 DB를 자동 생성하는 진입점이 아니다. 명시한 DB·manifest·승인 계획과 별도로 검토한 계획 hash를 대조하고, 수명 도구가 만든 정확한 DB만 설치한다. 인자 없이 과거의 `Install-Database.ps1` 명령을 실행하지 않는다. 실제 명령·실행 주체·승인 입력은 최초 실행 전 G2 독립 검토 후 goal에 고정한다. 승인 계획의 문자열이나 `ExecutionApproved` 값 자체가 사용자 승인을 대신하지 않는다.

| 도구 | 책임 |
|---|---|
| [New-TestDatabase.ps1](../../99_Tools/database/test-environment/New-TestDatabase.ps1) | 승인된 이름의 부재 확인, 한 번의 시험 DB 생성과 수명 기록 |
| [Install-Database.ps1](../../99_Tools/database/Install-Database.ps1) | 동일 DB의 `Baseline001` 또는 `Complete` 설치를 수명 도구에 전달. Complete는 001~004·module·권한·catalog 단계 |
| [Initialize-CharacterBinding.ps1](../../99_Tools/database/test-environment/Initialize-CharacterBinding.ps1) | 승인된 슬롯·계정·캐릭터 식별자 바인딩 |
| [Set-TestPrincipals.ps1](../../99_Tools/database/test-environment/Set-TestPrincipals.ps1) | 승인된 시험 principal·권한·격리 credential 구성 |
| [Remove-TestEnvironment.ps1](../../99_Tools/database/test-environment/Remove-TestEnvironment.ps1) | identity·미확정 작업·owner·연결 정산 대조 후 정확한 자원 정리 |
| [Environment.Common.ps1](../../99_Tools/database/test-environment/Environment.Common.ps1) | 승인 입력·수명 manifest·identity·단계 기록의 공통 경계 |

DB명·principal·executor·endpoint·TLS·경로는 검토된 승인 계획에 둔다. 기존 DB나 같은 이름의 자원을 자동 채택하거나 다른 이름으로 재생성하지 않는다. 실패·부분 실행은 원문과 자원을 보존하고 조사하며 강제 연결 종료나 자동 정리로 넘기지 않는다. 이번 배포값 변경은 새 SQL을 아직 적용하지 않은 단계의 승인된 변경이며, 적용된 migration을 나중에 덮어쓰는 운영 규칙이 아니다.

구조 중간 commit `ad6d5cbb3432947aaf867ebc994151e9473b9d02`는 **설치 불가·소스 검토용**이며 DB 설치 `git bisect` 판정에서 제외한다. 배포 구현 commit은 `ccb71077987fb8addca552109b3de3f9a5105342`다. 설치·G2는 후속 goal의 최종 검증 tree로만 진행하며 이 커밋 자체가 실행 PASS를 뜻하지 않는다.

## 현재 저장·권위·증빙 모델

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

## 공개 RPC와 최소권한

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

runtime은 Windows/WSL 모두 전용 최소권한 **SQL principal**, recovery는 `dh_recovery`에만 매핑한 전용 **비관리자 Windows principal의 통합 인증**이 후속 연결 계약이다. 기존 Windows 관리자/sysadmin은 recovery 양성 시험의 대용이 아니다. login/password는 migration에 없고 실제 계정·role 매핑·launcher·비밀 접근 구성은 G2 이후 실행 범위다. [기술명세 §2](../../01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md#2-연결설정비밀권한)의 설정/인증/권한 계약을 따른다.

기존 `Configure-WslAccess` login의 직접 SELECT/INSERT/UPDATE 권한을 새 runtime에 재사용하면 fencing을 우회한다. 기존 DB/credential/WSL 도구의 역사 기록을 현재 RPC 사용법으로 적용하지 않는다.

## SQL 소스 위치와 배포 책임

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

현재 reader/catalog는 SchemaVersion4·테이블8·module18·정확9grant 등 고정 계약을 요구한다. [Environment.Common.ps1](../../99_Tools/database/test-environment/Environment.Common.ps1)도 수명 상태별 001/정확4개 migration을 대조하며 **수명 manifest 자체의 schema는 1**이다. 향후 코드 변경은 새 버전 선언과 그에 맞는 소스·도구·catalog 검토가 필요하다. 과거 migration checksum 변경이나 오래된 checkout의 강제 덮어쓰기로 되돌리지 않는다.

이 배포 경계는 구현 및 자체 오프라인 모의 대조까지이며 실제 SQL 문법/compiler·CREATE OR ALTER 저장 정의/hash·catalog·권한·중간 실패 atomic rollback은 아직 실행하지 않았다. 예상 engine metadata가 다르면 원문을 보존하고 검토된 수정부터 진행한다.

## 오프라인 module 구조 검사

[Test-ModuleStructure.ps1](../../99_Tools/database/Test-ModuleStructure.ps1)은 고정 module 경로·객체명/종류·파일당 정의 하나와 공개 RPC의 정본 helper 직접 호출을 검사한다. 주석/리터럴을 제외한 제한된 lexer를 사용하며 입장 조회·관리 inspection·resolver의 서로 다른 책임을 별도로 등록한다.

```powershell
# Windows PowerShell 5.1에서 소스 구조만 검사. 생략 시 스크립트의 database 폴더.
# 상대 -DatabaseRoot는 PowerShell 세션의 현재 FileSystem 위치를 기준으로 해석한다.
./99_Tools/database/Test-ModuleStructure.ps1 -DatabaseRoot ./99_Tools/database -Json
./99_Tools/database/Test-ModuleStructure.ps1 -Json -Strict
```

`-DatabaseRoot`는 FileSystem provider 경로만 허용하며, 끝 구분자 유무와 관계없이 같은 트리를 검사한다. 다른 provider나 해석할 수 없는 경로는 `unavailable`/exit2다. `-Json`은 Status/DatabaseRoot/Scope/CheckedFiles/Issues/ViolationCount를 출력한다. DatabaseRoot는 검사 대상의 정규화 절대 경로이며, 경로 해석에 실패하면 null이다. 기본 출력 첫 줄에도 DatabaseRoot를 표시하고 각 File/Line/Expected/Remediation 경고를 출력한다. `-Strict`는 구조 위반을 실패 exit로 바꾼다.

| 상태 | 뜻 | warning 파일럿 exit | Strict exit |
|---|---|---|---|
| `compliant` | 검사한 경로·정의·직접 호출 계약에 위반 없음 | 0 | 0 |
| `violation` | 읽은 소스에 누락·미등록/잘못된 경로·정의·필수 호출 위반 | 0, Issues로 위반 공개 | 1 |
| `unavailable` | 경로/접근 실패나 미종결 주석·문자열/식별자 등으로 검사 불가 | 2 | 2 |

`unavailable`의 ViolationCount는 null이며 통과가 아니다. 예를 들어 누락된 `modules/procedures/internal/read_character_state.sql`을 복구하거나 공개 RPC를 등록된 목적 경로로 옮긴다. receipt helper 누락은 원래 receipt 단계에서 `EXEC dh.ReadOperationReceipt`를 명명 인자로 호출하도록 고친다. 주석에 helper 이름을 적어도 직접 호출로 세지 않는다.

검사는 SQL 문법·중첩/들여쓰기·타입·transaction/동시성·권한·29열 실행을 판정하지 않으며 manifest/hash 배포 검사의 대용도 아니다. 기본 exit0만으로 violation을 PASS로 읽지 않는다. 현재 warning 파일럿이며 CLI·독립 tests의 CI 연결과 SQLFluff 적용성/연결은 **첫 PR 병합 뒤 Rules와 별도 조율**한다. CI 연결·SQLFluff parse·실제 엔진 PASS를 이 검사에 합치지 않는다.

## 게임 정책과 후속 소비 계약

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

## 2026-09-29 기존 구성 기록

2026-09-29 구성: `YYH_Desktop\SQLEXPRESS`, Express 17.0.1000.7, `Dawnholder_Dev`. 당시 Windows 관리 도구는 통합 인증/shared memory, native WSL은 전용 SQL 인증/`tcp:127.0.0.1,14330`을 사용했다. 사용자 승인으로 mixed 인증과 loopback TCP만 활성화했다. 기존 `GameDB`의 `dbo.accounts(playerID, playerName, playerMoney, playerDate)`와 `BaseballData`, `Northwind`의 데이터·스키마는 수정하지 않았다. 다른 프로젝트의 accounts를 게임 계정으로 재사용하지 않는다.

당시 Windows PowerShell 5.1/7 도구는 명시 인자 또는 프로세스의 `DAWNHOLDER_SQL_INSTANCE`·`DAWNHOLDER_SQL_DATABASE`를 읽고, `lpc:` 로컬 shared memory·MachineName·`Dawnholder.DatabaseTool=development-v1` 소유 표시를 확인했다. 이 변수는 GameServer 설정이 아니며 현재 시험환경 수명 도구의 승인 입력을 대신하지 않는다. 기존 DB와 계정은 현재 목표의 수정·정리 대상에서 제외한다.

## 2026-09-29 구성 검증 기록

[목표 결과](../../01_Phases/goals/2026-09-29-mssql-setup/goal.md)와 [근거 폴더](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/)를 참고한다.

아래는 당시 001 구성과 기존 도구의 실행 기록이다. 현재 modules/RPC·배포·권한·경쟁·복구 검증의 통과 근거로 재사용하지 않는다.

`Test-Database.ps1`은 GUID로 격리된 시험 행만 만들고 끝에서 반드시 rollback한다. 카탈로그, PK/unique/FK/check/nullability, 두 직업·네 맵·HP 경계, CRUD, 데이터가 있는 상태의 migration 두 번 재실행, 체크섬/미지 버전 거부, stale rowversion, 별도 연결의 migration 잠금 및 쓰기 잠금 timeout을 검사한다. CHECK/default의 실제 식과 소속 테이블·열도 비교하며, Class CHECK 완화·해금 DEFAULT1·잘못된 default 열을 잠깐 적용한 세 음성사례가 거부됨을 각각 rollback으로 확인한다. 운영 트래픽이 없는 개발 DB에서 실행한다. 삭제도 이 트랜잭션에서 만든 행만 대상으로 한다. rollback 전 남겨둔 witness 행이 rollback 후 없음을 확인한다. 실패한 경우에도 finally에서 rollback하며 실제 게임 행을 seed하거나 commit하지 않는다. rowversion 내부 카운터는 rollback 후에도 증가할 수 있고 연속 번호를 보장하지 않는다.

Windows PowerShell 5.1/7 및 독립 재검토는 통과했다. Ubuntu 26.04 mirrored 모드에 Microsoft 공식 26.04 저장소의 `mssql-tools18`/`msodbcsql18` 18.7.1.1-1을 설치했다. 승인된 관리자 Enable 후 native WSL SQL 인증·최소권한·rollback 저장이 성공했다. 초기 TCP 실패 기록은 [초기 probe](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/wsl.txt), 최종 성공은 [native 인증 검사](../../01_Phases/goals/2026-09-29-mssql-setup/evidence/wsl-auth.txt)에 구분한다. **게임 서버 저장/재접속 복구는 미구현이며 Restore 실행 검증은 미실행**이다.

## 기존 WSL 연결 및 관리자 복구 기록

당시 이 PC는 사용자 승인 및 독립 검토 후 관리자 Enable까지 적용했다. 아래는 기존 도구의 구성·복구 기록이며 현재 저장 연동 시험환경의 실행 절차가 아니다. 기존 설정·credential을 새 목표에 재사용하거나 Enable·Restore·서비스 재시작을 이번 승인 범위로 해석하지 않는다. 다른 PC의 Enable이나 이 PC의 Restore에는 별도 영향 확인·점검 시간·관리자 실행 승인이 필요하다. 스크립트 자체가 UAC를 열지는 않는다. 당시 적용은 승인된 elevated PowerShell을 한 번 실행해 SQL 서비스를 재시작했으며, 복구 테스트만을 위한 추가 재시작은 하지 않았다.

```powershell
# 역사 기록: Plan은 설정과 정확한 변경 목록만 출력하는 읽기 전용 동작.
./99_Tools/database/Configure-WslAccess.ps1 -Action Plan
```

적용된 변경 및 다른 PC에서의 승인 범위:

1. `SQLEXPRESS`의 LoginMode를 Windows-only(1)에서 mixed(2)로 변경한다. 기존 로그인/암호를 교체하지 않는다.
2. TCP Enabled=1, ListenOnAllIPs=0. 등록된 `127.0.0.1`과 `::1`만 Enabled=1, TcpPort=14330, TcpDynamicPorts=''로 바꾼다. 다른 IP는 비활성화한다. IPAll은 보존하고 ListenAll=0으로 무시된다. SQL Browser 및 방화벽 규칙은 변경하지 않는다.
3. 난수 이름의 신규 SQL login과 `Dawnholder_Dev` user를 만들고, 세 게임 테이블에 SELECT/INSERT/UPDATE, SchemaVersion에는 SELECT만 부여한다. 삭제·DDL·db_owner/sysadmin·다른 사용자 DB 권한은 부여하지 않는다.
4. 같은 인스턴스를 쓰는 모든 DB에 잠깐 영향을 주는 SQL 서비스 재시작 1회가 필요하다. 다른 사용자 SQL 세션이 있으면 스크립트는 중단한다. 검사 직후 새 접속까지 막는 유지보수 모드는 아니므로 실행 전에 다른 도구/사용자의 SQL 작업을 중지해야 한다.
5. `%LOCALAPPDATA%\Dawnholder\MssqlWsl-SQLEXPRESS`에 원래 레지스트리 값 및 신규 login SID를 `state.clixml`, 256비트 난수 기반 암호를 Windows DPAPI `credential.clixml`로 저장한다. 폴더 ACL은 실행 사용자와 SYSTEM으로 제한한다. 동일 PC/Windows 사용자만 복호화할 수 있다. 저장소에 복사하거나 암호를 출력하지 않는다. 기존 폴더가 있으면 자동 재적용/비밀 교체 대신 거부한다.

```powershell
# 역사 기록: 당시 별도 승인된 동일 Windows 계정의 관리자 Enable.
./99_Tools/database/Configure-WslAccess.ps1 -Action Enable

# 역사 기록: 같은 관리자 계정의 복구 경로. Restore 실행 검증은 미실행.
./99_Tools/database/Configure-WslAccess.ps1 -Action Restore
```

Restore는 원래 설정을 복구하고 SID가 일치하는 신규 login만 비활성화한다. 데이터·DB user·암호화된 복구 파일은 삭제하지 않는다. 적용 후 다른 관리자가 바꾼 값이 있으면 덮어쓰지 않고 수동 검토를 요구한다. Enable 도중 실패해도 백업을 보존하므로 무조건 재실행하지 말고 Restore를 사용한다. SQL 서비스가 시작 실패한 경우에도 원래 레지스트리를 먼저 복구한 뒤 시작한다. 백업 경로 생성 직후 중단되어 state.clixml이 없다면 아직 SQL/레지스트리 변경 전이다. SQL 설정이 복구된 뒤에도 데이터와 이전 시도 기록은 보존하며 새 Enable은 별도 검토한다.

당시 WSL Ubuntu에 native `mssql-tools18`의 `/opt/mssql-tools18/bin/sqlcmd`를 설치했다. 새 환경의 설치는 Ubuntu 버전에 맞는 [Microsoft 공식 절차](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup-tools)와 별도 승인 범위를 따른다. 당시 Ubuntu 26.04용 `packages-microsoft-prod.deb`로 서명된 저장소를 등록하고 `ACCEPT_EULA=Y apt-get install -y mssql-tools18`로 필요한 ODBC 의존성만 설치했으며 기존 패키지 upgrade는 하지 않았다. 기존 probe의 실행 환경은 Windows PowerShell **7**이었다.

```powershell
# 역사 기록: 당시 native WSL 인증·권한·rollback probe 진입점.
./99_Tools/database/Test-WslAccess.ps1 -Distribution Ubuntu
```

이 probe는 DPAPI 비밀을 LF로 끝나는 stdin으로 Linux child에 전달하고, child 수명 동안만 SQLCMDPASSWORD 환경변수에 둔다. Windows CRLF를 쓰면 암호에 CR이 붙어 로그인 실패하므로 LF를 명시한다. 비밀을 명령 인자·콘솔·Linux 파일에 출력하지 않는다. localhost의 self-signed 개발 인증서를 신뢰하고 SQL 로그인/권한/rollback 저장을 검사한다. 실패하면 메시지와 포트 상태를 확인하되 방화벽 전체 공개나 LAN 바인딩 확대를 자동 수행하지 않는다. 프로브 성공은 native Linux ODBC 경로 검증이며 .NET GameServer의 저장 서비스 구현을 의미하지 않는다.

## 기존 구성의 인계·서비스 기록

당시 DB 구성·접속 인계는 [인계문](../../01_Phases/goals/2026-09-29-mssql-setup/handoff.md), 현재 작업·승인 범위는 [CURRENT](CURRENT.md)의 goal에서 확인한다. 당시 DB 엔진은 Windows 서비스이며 WSL에서 별도 서버 프로세스를 띄우지 않았다. 서비스 시작 유형은 기존 Manual을 유지했다. 아래는 당시 인계한 서비스/probe 경로이며 현재 G2 실행 명령이 아니다.

```powershell
Get-Service 'MSSQL$SQLEXPRESS'
# 역사 기록: 중지 상태에서 별도 승인된 관리자 서비스 시작 경로.
Start-Service 'MSSQL$SQLEXPRESS'
./99_Tools/database/Test-WslAccess.ps1
```

설계 참고: [SQL application lock](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql), [TCP ListenAll 및 고정 포트](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-a-server-to-listen-on-a-specific-tcp-port), [Windows Export-Clixml/DPAPI](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/export-clixml).
