# 후속 구현 분할과 검증

[goal](goal.md)의 확정 작은 범위와 [design](design.md)을 따른다. 아래는 후속 계획이며 구현/SQL/테스트 미실행이다. 각 단계는 최신 main 별 branch에서 설계→정확 파일/작업자 배정→구현 쓰기 종료→다른 작업자의 요구사항 TestCode 작성·실행→리뷰/결과→PR로 진행한다. 메인이 Git/실행권·승인 경계를 관리하고 실제 상태는 단계별 goal 하나에 기록한다.

**D1–D4는 이번 로드맵의 조건부 병합 예외 범위 밖이다. 각 PR 병합 직전에 사용자 명시 승인을 받아야 하며 자동 병합 예약은 금지한다.**

## 단계·의존성

| 단계 | 완료 경계 | 선행 |
|---|---|---|
| D1a 기술 명세 | driver/version·authority/operation DDL·acquire/create/mutation/release의 동일 DB 경계·신뢰 recovery 권한·token/증빙·timeout과 격리 반증 방법 고정 | D0 독립 리뷰/통합, D1 목표·실행 범위 인계 |
| D1b repository/DB | 원자 최초생성/safe Progress, acquire+load, unknown 조정/release, 새 migration/catalog·실제 SQL 반증 | D1a 설계·정확 DB/파일 소유 확정. 001 고정 |
| D2 작업 수명 | 한 캐릭터 bounded lane, EOF/Host/0owner safe 인계, NoWriteNeeded·미확정 종결·전체 deadline | D1 계약 뒤 fake 작성 가능, 실제 DB 통합은 D1b 완료 뒤 |
| D3 권위 입장 | Loading/Active gate·새 결과·권위 class 전 spawn 보류·HUD rebind·Town풀HP/기존 Ready순서 | D2 수명/D1 결과 계약, protocol writer의 선행 동결 |
| D4 실제 복구 | commit→같은GUID의 새 runtime entity/session 매핑·저장class·Town풀HP. 같은 process 재접속은 새 ID, process 재시작은 ID 숫자 재사용 허용. crash unknown·fence·관리 recovery·정리 | D1–3 독립 TestCode/리뷰, 격리 데이터/포트/Unity 실행권 |

## 파일 소유 후보

신규명은 계약 수준 후보이며 지금 생성/쓰기 권한이 아니다. 기존 DB catalog 경로는 `verify-schema.sql`로 확인했다. 정확 증분·agent와 추가 접점은 단계 kickoff에서 좁게 확정한다. 표의 `Tests/`는 `02_Server/GameServer.Tests/`, 서버 축약 경로는 `02_Server/GameServer/` 아래다.

| 단일 구현 writer | 파일 후보 | 별도 독립 TestCode writer |
|---|---|---|
| D1 repository | 신규 `Persistence/PersistenceContracts.cs`, `SqlCharacterRepository.cs`; 기존 `GameServer.csproj` driver dependency. 설정 주입 접점은 D1a에서 확정 | 신규 `Tests/Persistence/RepositoryOwnershipTests.cs`, `RepositoryConcurrencyTests.cs`, `RepositoryCreationTests.cs`; test project dependency도 단일 writer 인계 |
| D1 DB | 신규 `99_Tools/database/migrations/002_character_authority.sql` 가칭, 기존 `verify-schema.sql`; 필요할 때 `Database.Common.ps1` gate만 | 기존 `99_Tools/database/Test-Database.ps1` 새 catalog/재실행/음성 사례. 구현 종료 후 테스트 writer에게 인계. 001·서비스/계정 설정 script 불변 |
| D2 lifecycle | 신규 `Persistence/CharacterPersistenceOwner.cs`, `PersistenceWorker.cs`, `CloseCaptureTicket.cs`; 기존 `Sessions/GameSession.cs`, `Loop/GameWorld.cs`, `Hosting/ServerHost.cs` | 신규 `Tests/Persistence/PersistenceWorkerTests.cs`, `PersistenceCloseCaptureTests.cs`; 기존 `Network/ServerHostLifecycleTests.cs`, `GameSessionLifecycleTests.cs`, `Maps/MapMigrationTests.cs`의 필요한 보존 범위 |
| D3 protocol | `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/ProtocolVersion.cs`, `98_Shared/Protocol/Generated/GenPackets.cs` 및 실제 generator 산출물/등록. 정확 목록은 시작 시 확정 | 새 packet roundtrip/status/invalid/version/기존ID 안정성. `Tests/Protocol/PersistencePacketTests.cs` 가칭, 서버/봇/Unity 소비자 검증 |
| D3 서버 | 기존 `Handlers/Session/CharacterSelectHandler.cs`, `Sessions/GameSession.cs` admission·입력·entry | 신규 `Tests/Persistence/CharacterAdmissionTests.cs`: Loading 차단·중복·create 취소·late 적용·정상 입력 양성 |
| D3 Unity | `03_Client/Assets/Scripts/Network/UnityClientSession.cs`, 새 `Network/Handlers/Session/CharacterLoadResultHandler.cs`+meta, 기존 `Bootstrap/ClassLoadout.cs`, `LocalPlayerSpawner.cs`, `UI/SkillHudController.cs`, `Network/Handlers/Session/EnterMapHandler.cs`. 등록/Loading안내 UI 접점 추가는 사전 확인 | 기존 asmdef의 EditMode에 `PersistenceEntryTests.cs`, PlayMode에 `PersistenceEntryPlayTests.cs`+meta 후보. **정확 테스트 루트는 D3 시작 때 확인**. 기존 entry/class/HP 회귀 유지 |
| D4 시나리오 | `99_Tools/headless-bot/Scenarios/Persistence/` 아래 단일 시나리오 후보와 ignored verification launcher | 신규 `Tests/Integration/PersistenceRecoveryTests.cs`, Unity 실제 GameServer lane. 실행권은 메인/명시 단일 executor |

GameSession은 D2 종료 후 D3로 인계하고 동시 쓰기를 금지한다. MapMigration production 변경은 기존 close 경계로 부족할 때만 정확 범위를 추가 배정한다. PDL/Shared/등록·DLL 복사도 단일 writer/executor다. UI scene/prefab 재저장을 자동 포함하지 않는다. 독립 검증자는 production 결함을 원 구현자에게 반환한다.

## 반증 TestCode·완료조건

| 경계 | 요구 관측 |
|---|---|
| restart authority | old 요청을 DB 진입 전/직렬화 경계 획득 뒤 UPDATE 전/commit 뒤 응답 전 정지. new acquire와 교차시켜 old-first이면 new load가 확정값, new-first이면 old mutation/release 거부. cancel/process 종료만으로 추정 금지 |
| create/operation/token | 동일 고정 주체 동시 create1개·foreign ownership 거부·전체transaction rollback·동일ID다른payload 거부·응답유실 증빙조회. Character 읽기 의존 conflict와 복사 token alias mutation 반증 |
| duplicate/recovery | 신규중복 거부/기존Ready 유지, 정상release 후 재접속/재시작 양성. crash/unknown은 RecoveryRequired, 무권한 recovery 거부·허용된 recovery는 atomic fence+load |
| close/0owner | EOF/명시close/Host/source제거전후/destcloseskip에서 safe DTO·진행요청 인계와 cleanup 유한 종결. 변경 필드·작업 없음은 NoWriteNeeded이며 Character/Progress 게임 데이터 UPDATE0을 확인한다. authority release의 조건부 transaction은 수행한다. 도착/Quest capture 대기 없음 |
| bounded/deadline | full/DB지연/lateack를 제어 task/clock으로 주입. tick 비차단·한 전체deadline·unknown을 Durable/Released로 위장하지 않음 |
| admission | load 차단 중 raw move/attack/skill/portal/party/cheat 거부, 완료 뒤 정상양성. class 선택만으로 gate 통과·중복DB작업·2회entry 금지. create commit 후 UI취소는 row삭제/새캐릭터생성 아님 |
| 권위 class/Unity | Knight/Mage 최초생성과 메뉴반대·저장class 조합. 결과 전 spawn/예측/입력0, 이후 Stats·이동/평타·Q/E·HUD 일치. scene/result/HP 선후·옛Town객체·oldgeneration·상충/invalidresult·성공없는EnterMap 반증 |
| 실제 policy/durability | commit 뒤 같은GUID에 새 runtime entity/session 매핑·저장class·Town풀HP를 확인. 같은 process 재접속은 새 EntityId, process 재시작은 allocator 초기화로 ID 숫자 재사용 허용. 이전quest/unlock복원없음, 기존 Party/Quest정책 회귀. SQL rollbackCRUD를 durable E2E로 대체하지 않음 |

pure fake는 순서/상태 반증이고 SQL lock/driver 검증을 대신하지 않는다. 실제 두 연결 시험은 row/operation/token을 각각 관측한다. 구현 후 별도 작업자가 TestCode를 작성하며 실패 원문·수정·재검증을 보존한다.

## 실행 전제·부작용

| 실행 | 부작용·보존/정리 |
|---|---|
| pure tests/build | DB 없음. package cache/생성물/빌드 출력 부작용은 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md) 확인. Windows 자동 DLL복사 피하고 검증본만 메인 선택복사/hash |
| migration/catalog | 승인한 suffix DB/격리 GUID·관리자 DDL/잠금. 001/이력 보존, 재실행·기존행보존·실패rollback. rollback도 rowversion 카운터 증가 가능 |
| repository SQL | 앱 최소권한의 별도 연결·lock timeout/commit. 비밀은 제한된 process 주입, 명령행/로그/Unity에 금지. 종료 후 DB 실행권 인계 |
| durability/crash | 실제 commit한 시험 행·task-owned process/연결만 fault. 생성ID 목록·승인된 관리 cleanup, 앱DELETE/DDL 확대 금지. 사용자 SQL서비스/타process 종료 금지 |
| Unity/bot | port/WSL clone/Unity 단독 소유, 새 protocol/DLL hash·양쪽 소비자 검증, meta/GUID/asset 보존·임시잔여 확인. 자동가상입력과 수동화면/물리키보드/청취 구분 |
| recovery/restore | 신뢰 복구 principal·허용·격리 시험을 먼저 확정. 실제 backup restore/서비스 변경은 별도 범위 |

각 목표가 정확 명령/환경/시각/통과·실패·skip/commit 여부/자원 종료/미실행을 기록한다. 로컬 성공과 PR CI/병합·정식 인증·DB 운영 복구 완료는 별개다. 현재 이 계획의 실제 실행 결과는 없다.
