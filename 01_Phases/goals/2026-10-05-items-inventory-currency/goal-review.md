# Fable 설계 검토 — 아이템·인벤토리·재화 (구현 전 불변식)

이 문서는 [R-7 Fable goal 검토 시범](../../../00_Document/operations/ORCA.md#r7-fable-pilot)의 산출물이다. Astra의 채택/보류와 메인의 원문 확인 입력이며 구현 발행 승인이 아니다. 실행·빌드·성능·backend를 주장하지 않는다.

# 작업 전 맥락

- 역할·자기 태그 / 할당 작업 / 모델: `[Content 검증자]`, 신규 외부 Fable 설계 검토자(Opus 독립 구현 검증 아님). 할당 작업 하나 = Content 첫 goal의 보호 집합·오류 분류·중복/수명·실패 원자성·비용 상한 불변식 목록 작성. 지정(요청) 모델 `claude-fable-5-1`, 관찰 모델 Fable 5.1 (계약 전달값 xhigh), backend `unknown`. 모델 자기 보고는 실제 backend 증거가 아니다.
- 현재 goal·할당 계약의 경로 / 작업 경로·branch·base·HEAD: goal `01_Phases/goals/2026-10-05-items-inventory-currency/goal.md`, 계약 `.backups/verification/2026-10-05-items-inventory-currency/fable-contract.md`(v1), 초안 `.backups/verification/2026-10-05-items-inventory-currency/pr1-contract-draft.md`. 작업 경로 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`, branch `feat/items-inventory-currency-20261005`, base `955002a932925ff2c4ac81f4a5a99f2024a4b9b2`, HEAD `6d37e7a05cd0520cdda449ccfd7582f8a6d48e1c`(관측: `git rev-parse HEAD` 일치, `git status --porcelain` 0줄).
- 메모 작성 시점 / 대상 파일 첫 쓰기 전 여부: 2026-10-05, 이 파일의 첫 쓰기가 이 메모였다(메모만 먼저 기록 → 검토 본문을 이어 씀 → 「준수 연결」 갱신).
- 기존 사용자 변경·다른 작성자의 소유권 / 허용 쓰기·실행·금지 범위: 작업 트리 clean, 다른 작성자의 미커밋 변경 없음. 허용 쓰기는 이 파일 하나. 제품·테스트·goal·CURRENT·설정 수정, build·봇·Unity/DB 실행, 설치·외부 검색·추가 위임·commit/push/branch 변경 금지. 저장 API와 팀원 정책을 확정하지 않는다.
- 원문 요구/후속 보충의 계약 버전·시각 / 고정 입력 경로·hash / 적용 단계: 계약 v1, SHA256 `321C463680E2DB633E5CA295C94FBCB68A9768817DB0713F188EB5E4D5B88452`(관측 일치). 고정 입력 목록과 SHA256은 `.backups/verification/2026-10-05-items-inventory-currency/fable-input-manifest.json`. 적용 단계 = 구현 전 설계 검토(TDD·Sol 발행 전). 후속 보충 지시 없음(check 결과 메시지 0건, worker_done 직전 재확인).

## 읽기와 선택

모든 파일은 commit `6d37e7a` 기준이며 미커밋 변경은 없다. 줄 번호는 이 commit의 관측값이다.

| 읽은 파일·절 | commit 또는 미커밋 hash | 이 작업에서 필요한 규칙·경계 |
|---|---|---|
| `AGENTS.md`, `CLAUDE.md`(세션 지침으로 수신) | `6d37e7a` | 역할 경계, Claude 서브에이전트 읽기 전용, 공학 조건(서버 권위·패킷 ID 재사용 금지·틱 I/O 금지·공유 DLL 양쪽 검증) |
| `.agents/skills/dawnholder-task-context/SKILL.md`, `references/templates.md`(맥락 메모·검증 판정 절) | `6d37e7a` | 메모 필드·사전 기록 의무·`[소스]/[추론]/[미측정]` 구분·통과 차단 기준(측정값 위장·제품 계산 복제 테스트) |
| `.agents/skills/dawnholder-goal-loop/SKILL.md` L20(R-7 연결), `00_Document/operations/ORCA.md` R-7 절 | `6d37e7a` | 시범 범위: `goal-review.md`만 쓰기, 메인 원문 확인 → goal 보완 → 승인 → Sol 발행 |
| `01_Phases/goals/2026-10-05-items-inventory-currency/goal.md` 전부 | `6d37e7a` | 범위·완료조건·설계 초안·보존 계약·패킷 합의(35..38, GameDev 39부터)·검증 등급 강·TDD 순서 |
| `.backups/verification/.../pr1-contract-draft.md` 전부 | 미커밋 로컬 원문, manifest SHA256 `210DBE84…051CD9` | 구현 설계 초안 8항목, 패킷·공유 파일 합의, 미확정 불변식·상한·오류 코드 |
| `.backups/verification/.../gamedev-boundary-agreement.json` | 로컬 원문 | `msg_58eaccfd4a15` 합의 원문(순차 ++packetID 경계, 공유 파일 최소 연결 순차 쓰기 허용, 임의 저장 hook 금지) |
| `00_Document/FEATURE_MAP.md` 전부(처치 진행·공통 계약·상태의 수명) | `6d37e7a` | 처치 콜백 → QuestRegistry 경로, 패킷 변경 확인 지점, quest/보스 해금은 세션 한정 |
| `00_Document/domains/server.md`, `protocol.md`, `client.md` | `6d37e7a` | 수정 흐름(핸들러 → 세션 제출 → 큐 → 시스템), 즉시 피해 순서, 종료/broadcast 겹침, PDL 변경 절차·append-only·.NET Standard 2.1, Unity 메인 스레드 적용·세션 정리 |
| `00_Document/conventions/CODE_CONVENTION.md` 적용 절(계약 첨부 원문) | base `955002a`(계약 첨부), 현재 `6d37e7a`와 동일 파일 SHA(manifest) | 상태와 책임, 파일 위치와 이름, 주석과 문서, 역할별 적용, 변화 평가 |
| `02_Server/GameServer/Quest/QuestRegistry.cs` 전부 | `6d37e7a` | 월드 틱 전역 레지스트리 예시: 큐 drain·OnKill 수혜자·DEBUG 가드·해산 정리 |
| `02_Server/GameServer/Loop/GameWorld.cs` 전부 | `6d37e7a` | OnTick 순서(L247-267), MakeMap 처치 콜백(L226-245), DrainSessionCloses(L200-221), IsActiveSession/IsActiveEntity(L188-192), SendToEntity(L142-161), 전역 entity id(L29-39,106) |
| `03_Client/Assets/Scripts/State/QuestState.cs` 전부, `Network/Handlers/Quest/QuestUpdateHandler.cs`, `Network/NetworkService.cs` L89-113 | `6d37e7a` | 서버 미러 싱글톤·메인 스레드 적용(`EnqueueApply`)·세션 리셋 순서(값 리셋 → 통지) |
| `02_Server/GameServer/Maps/GameMap.cs` 전부 | `6d37e7a` | Tick 순서(L230-272), AddPlayer/AddPlayerWithId/Remove(L172-196), ApplyImmediateEnemyHit(L394-416), HandleEnemyDeath(L426-441), DespawnEnemyByFall(L532-538), OnEnemyKilled(L546-547), MaybeRespawnBoss(L555-564) |
| `02_Server/GameServer/Entities/EnemyEntity.cs` 전부 | `6d37e7a` | 적 수명: EntityId 불변·IsDead 파생·respawn 카운트다운 필드 |
| `02_Server/GameServer/Maps/Systems/RespawnSystem.cs` 전부 | `6d37e7a` | 재출현은 새 entityId의 새 entity(L20-23, L75); 죽은 객체는 큐에 잔류 |
| `02_Server/GameServer/Sessions/GameSession.cs` 전부 | `6d37e7a` | 입력 gate(OnRecvPacket L125-180), OwnsPlayer(L188-189), Submit* 패턴(L225-439), SubmitCheatCommand의 월드 job·IsActiveSession 가드(L423-439), EnterGameWorld(L479-531), GetMap 이동 중 null(L536-540), 종료(L113-118, L182-186) |
| `99_Tools/PacketGenerator/PDL.xml`(머리 L1-40, 꼬리 L330-416, packet 순서 전체) | `6d37e7a` | 마지막 패킷 `C_CheatCommand`(34, L413-415) 뒤 append 위치, `<string>` 1곳(L96), `<list>` 사용 0곳 |
| `99_Tools/PacketGenerator/Program.cs` 전부 | `6d37e7a` | ParsePacket 순서형 `++packetID`(L134), 중복 이름 차단(L124-129), ParseMembers 지원 타입(L185-237, list 포함), ParseList(L246-273) |
| `98_Shared/Protocol/ProtocolVersion.cs` 전부 | `6d37e7a` | `Current = 16`(L81), 묶음 bump 관례, "PDL 가변 list 미지원" 주석(L41, L61)은 Program.cs와 불일치하는 오래된 주석 |

- 같은 책임의 예시 1~2개(경로·심볼/절) / 따를 이름·서식·오류·주석 관례: 서버 전역 상태 레지스트리 = `Quest/QuestRegistry.cs`(`EnqueueJob`/`Tick`/월드 틱 스레드 한정/`internal` 조회자) + `Party/PartyRegistry.cs`(이름만 확인). 핸들러 = `Handlers/Debug/CheatCommandHandler.cs`(`RequiresSelectedClass`, 파싱 → `session.Submit*`) + `Handlers/IPacketHandler.cs`. 공유 카탈로그 = `98_Shared/GameData/Combat/SkillCatalog.cs`(append-only switch, 미정의는 항상 거부). 클라 미러 = `State/QuestState.cs` + `Handlers/Quest/QuestUpdateHandler.cs`. 오류 전달 = `S_PartyError.reason` byte 코드, `S_PortalLocked` 1:1 `session.Send`(MapMigration.cs L93-98).
- 재사용 helper·계약 / 재사용하지 않거나 중복이 필요한 이유: `GameWorld.IsActiveSession/IsActiveEntity`(수혜자·요청자 수명 검증), `GameWorld.SendToEntity`(cross-map 1:1), `GameMap.OnEnemyKilled` 콜백(처치 진입점, 새 hook 불필요), `FrameValidator.MaxFrameSize=4096`(패킷 상한), `PlayerSnapshot` record struct 문구(저장 접점 값 계약 모델). `IntentRateLimiter`는 이동 전용 카운터라 그대로 공유하면 예산 의미가 바뀐다(§9 선택 항목).
- 영향 파일·사용처·보존 동작: GameDev 소유 `GameSession.cs`(새 Submit 2개), `GameWorld.cs`(레지스트리 필드·생성 순서·OnTick 슬롯·MakeMap 콜백 확장·DrainSessionCloses 정리 hook), `HandlerRegistry.cs`(등록 2줄), PDL·GenPackets·ProtocolVersion(Content 단일 writer). 보존 동작: Hit → Death → StageClear → 제거/respawn → 콜백 순서, quest 보스 reset/OnKill 분기, 세션 종료 시 ghost 없음(`SessionCleanupTests`), 처치자 전파(`HandleEnemyDeathKillerTests`), 패킷 1..34 순서·오프셋, 핸드셰이크 동일 버전 gate.
- 신규 파일 위치·이름 / 기존 경로·명명 관례 근거 / 새 폴더 필요 이유: 서버 `02_Server/GameServer/Items/`(새 폴더; 같은 책임의 기존 폴더 없음, `Quest/`·`Party/`처럼 도메인 폴더 + `…Registry` 이름 관례), `Handlers/Inventory/`(기존 `Handlers/<도메인>/` 관례). 공유 `98_Shared/GameData/Items/`에 카탈로그(`Combat/SkillCatalog.cs` 관례), 아이템 ID enum은 기존 `98_Shared/GameData/Enums/`가 관례상 더 맞다(§7에서 제안). 클라 `State/InventoryState.cs`, `Network/Handlers/Inventory/`, `UI/`는 PR2. 마일스톤 코드·날짜·작업자 이름을 제품 이름에 쓰지 않는다.
- 읽기 상한을 넘긴 추가 경로·구간과 이유(계약 5·6항 허용 범위 안): `99_Tools/PacketGenerator/PacketFormat.cs` L205-263, L286-319, L375-400(list·string 생성 템플릿 — "list 지원은 현재 코드에서 확인" 요구); `98_Shared/Protocol/Generated/GenPackets.cs` `S_HandshakeResult` 구간(string 와이어 확인); `02_Server/GameServer/Maps/Transitions/MapMigration.cs` L44-160(맵 이동 중 보존·거부 경계); `Maps/PlayerSnapshot.cs` 전부, `Entities/PlayerEntity.cs` 심볼 grep(이동 시 entity 재생성 확인); `Maps/Systems/DeferredDamageSystem.cs`·`CombatSystem.cs` 심볼 grep(지연 피해 처치자·IsDead 가드); `Quest/QuestNotifier.cs`, `Party/PartyFlow.cs` L113-130(종료 정리 패턴); `Handlers/HandlerRegistry.cs`, `IPacketHandler.cs`, `Handlers/Debug/CheatCommandHandler.cs`(핸들러 등록·gate 관례); `Sessions/IntentRateLimiter.cs` 심볼(제한 범위); `02_Server/Network/FrameValidator.cs` 상수·`Session.cs` L25-67(프레임 상한·수신 예외 경계); `98_Shared/GameData/Constants.cs` 상수; `98_Shared/GameData/Combat/SkillCatalog.cs`, `02_Server/GameServer/Combat/EnemyCatalog.cs` 심볼(카탈로그 관례); `98_Shared/Shared.csproj`·`04_ClientNet/*.csproj` TargetFramework; 테스트 파일 이름·메서드 이름만(`GameServer.Tests/Maps/HandleEnemyDeathKillerTests.cs`, `SessionCleanupTests.cs`, `Party/QuestMembershipLifecycleTests.cs`, `PacketRoundTripTests.cs`, `Handlers/CheatBuildGateTests.cs`); `99_Tools/headless-bot/Scenarios/ProbeBase.cs` 시그니처만. 전체 저장소·로그·대화는 수집하지 않았다.
- 언어 규칙 부재·임시 기준과 승인 상태: 제품·테스트 코드를 쓰지 않으므로 C# 공백·빌드 설정 절은 계약대로 미적용. 해당 없음.
- 열린 질문·차단 / 질문할 담당자 / 완료 때 처리 근거: 차단 없음. 질문은 Content Astra(Run `run_add8d9f825f4`)에 ask로 보낸다. 이번 검토에서 ask를 보낼 만큼 막힌 항목은 없었고, 결정이 필요한 항목은 §9 「설계 관찰(비차단)」에 범위 안 기술 선택과 메인/팀원 정책 입력으로 구분해 적었다.

## 준수 연결

| 적용 규칙·출처 절 | 준수 계획 | 완료 뒤 실제 파일·심볼/절·근거 | 계획 변경 이유 |
|---|---|---|---|
| 작업 맥락 스킬 「파일 쓰기 전 메모와 원문 계약」 | 이 파일 첫 쓰기를 메모로 시작 | 이 파일 「작업 전 맥락」(첫 쓰기), 검토 본문은 그 뒤 §0~§10 | 없음 |
| 계약 「산출물의 요구 항목」: 번호 붙은 불변식, `[소스]/[추론]/[미측정]`, 오류 분류, 패킷/API/상한 제안, 대안 한 줄, 설계 관찰(비차단), 준수 위치·미검토 | §1~§10으로 작성 | 불변식 §2(INV-01~INV-20, 각 항목 전제 표기·대안 포함), 보호 집합 §1, 오류 분류 §3, 경로 §4·§5, 상한 §6, 고정 제안 §7, TDD 경계 §8, 비차단 관찰 §9, 준수·미검토 §10 | 없음 |
| CODE_CONVENTION 「상태와 책임」(단일 소유자·수명·틱 흐름·입력 경계 검증·늦은 결과의 영향) | 보호 집합·수명·stale 불변식에 반영 | INV-01~03, 05, 11~13, 15~17 | 없음 |
| CODE_CONVENTION 「파일 위치와 이름」 | 새 폴더·이름 근거를 §7에 적고 제품 이름에 날짜·마일스톤 금지 | §7.3 배치·이름 | 없음 |
| CODE_CONVENTION 「주석과 문서」·「변화 평가」(측정값 위장 금지, 미측정 구분) | 실행·성능 주장 없음, 미측정 표시 | 모든 수치는 §6·§7에서 자리표시로 표기, §10 미실행 | 없음 |
| AGENTS 「공학 조건」(패킷 ID 재사용 금지·양쪽 직렬화·틱 I/O 금지·공유 DLL 양쪽 검증) | 패킷·수명 불변식에 반영 | INV-13, 14, 16, 18; §7.1 | 없음 |

---

# 검토 결과

## 0. 요약

근거가 충분한 지적(계약 발행 전 고정을 권함) 8건과 설계 선택 6건, 정책 입력 3건이다. 번호는 안정적으로 유지하며, 세부는 §2의 INV 번호로 연결한다.

| # | 지적(근거 충분) | 연결 |
|---|---|---|
| F-1 | 처치 보상의 수혜자는 **적용 시점**에 살아 있는 entity여야 한다. 지연 피해(DeferredDamageSystem)는 공격자 세션이 닫힌 뒤에도 적을 죽일 수 있어, 처치 콜백의 `killerEntityId`가 이미 종료된 entity일 수 있다. `GameWorld.IsActiveEntity` 검사를 보상 job 안에 두지 않으면 종료 뒤 고아 상태가 생긴다. | INV-05, INV-15 |
| F-2 | 경제 상태를 `PlayerEntity`나 `GameMap`에 두면 맵 이동 때 소실된다. `AddPlayerWithId`는 새 `PlayerEntity`를 만들고 `PlayerTransferState`는 id·Stats·Hp만 옮긴다. World 수준 레지스트리(`QuestRegistry` 자리)에 entityId 키로 둬야 한다. | INV-02, INV-03 |
| F-3 | 실패 원자성은 "검사 후 불변 상태 객체를 계산하고 참조 하나를 교체"하는 구조로만 보장된다. 필드를 차례로 고치는 구조는 중간 예외가 부분 변경을 남기고, `QuestRegistry.Tick`식 catch-log는 그 부분 변경을 숨긴다. | INV-07, INV-08 |
| F-4 | 사용 요청 중복/stale 식별은 서버에 **무제한 ID 집합을 쌓지 않는** 키여야 한다. 소유자당 `revision` 하나에 클라가 `expectedRevision`을 실어 보내면 추가 상태 없이 재전달·낡은 화면의 소비를 모두 거부한다. | INV-10, INV-11 |
| F-5 | 서버가 수신하는 새 패킷에 가변 길이 필드를 넣지 않는다. 현재 모든 `C_` 패킷은 고정 크기이고 수신 경로에 payload 파싱 예외 처리가 없다. 생성기의 `list` 템플릿은 PDL에서 한 번도 쓰이지 않았고 count를 호스트 엔디안 `BitConverter`로 쓰며 NET_LEGACY 분기가 정의되지 않은 `Segment` 식별자를 참조한다. snapshot은 고정 크기로 4096 바이트 프레임 상한 안에 둔다. | INV-13, INV-14 |
| F-6 | 처치 콜백 확장은 `GameWorld.MakeMap`의 `onKill` 람다에서 **Quest job과 별도로** 경제 큐에 enqueue한다. `HandleEnemyDeath` 내부 호출, quest의 보스 reset/OnKill 분기 변경, `DespawnEnemyByFall` 무보상 변경은 금지한다. | INV-04, INV-17 |
| F-7 | 세션 종료 정리는 `DrainSessionCloses`에서 경제 큐로 `Forget(entityId)`를 enqueue하고, 종료 뒤 도착한 job은 `IsActiveSession/IsActiveEntity` 가드로 상태를 새로 만들지 못하게 한다. 같은 틱 안의 보상 job → Forget 순서가 FIFO로 보장되는 근거를 계약에 적는다. | INV-15 |
| F-8 | 오류 4분류의 전달·변경 보존 규칙을 §3 표대로 고정한다. 특히 **내부 불변식 실패를 성공 응답이나 "정상 거부"로 바꾸지 않는다.** 카탈로그 밖 itemId는 기존 `SkillCatalog` 관례대로 silent drop + `[Trust]` 로그다. | §3 |

설계 선택(§9.1, 각 대안 포함): S-1 snapshot 모양(고정 8슬롯 vs 종류별 필드 vs list), S-2 상한 도달 시 보상 전체 거부 vs clamp, S-3 중복 처치 방지 표식 없음 vs bounded ring, S-4 결과 전달(`session.Send` 직접 vs `SendToEntity`), S-5 요청 폭주 bound(세션당 in-flight 1 vs 공유 rate limiter), S-6 드롭 결정적 vs 확률.
정책 입력(§9.2, 메인/팀원): P-A 처치자 지급 vs 파티 분배(기본안 처치자, 팀원 명세 열린 질문), P-B 보스 처치 보상 유무, P-C 자리표시 수치의 최종 기획 여부.

## 1. 보호 집합

| ID | 보호 상태 | 런타임 소유자 | 생성 → 변경 → 정리 | 키·수명 |
|---|---|---|---|---|
| PS-1 | 재화 잔액 | World 레지스트리(Content, `Items/`) | 첫 보상/조회 때 lazily 생성 → 보상·사용 job → `Forget` | entityId, 월드 진입~세션 종료 |
| PS-2 | 인벤토리 슬롯(itemId, count) × 고정 N | 동일 | 동일 | 동일 |
| PS-3 | 소유자 revision(uint) | 동일 | 성공 변경마다 +1, 실패·거부 시 불변 | 동일 |
| PS-4 | 소유자 ↔ 연결 결합 | GameDev(`GameSession._entityId`, `GameWorld.IsActiveSession/IsActiveEntity`) | 세션이 `_entityId`를 소유, job 실행 시점에 재검증 | entityId는 전역 단조·재사용 없음 |
| PS-5 | 처치 보상 단일 발화 | GameDev(`GameMap._enemies` 불변식, `HandleEnemyDeath`) + Content(콜백 소비) | 적 인스턴스당 `OnEnemyKilled` 1회 | `EnemyEntity.EntityId`, 재출현은 새 id |
| PS-6 | 사용 요청 stale/중복 판정 | Content(레지스트리) | `expectedRevision == revision`일 때만 처리 | PS-3 재사용, 추가 저장 없음 |
| PS-7 | 카탈로그 값(ItemId, MaxStack, 사용 효과, 드롭표, 상한 상수) | Shared(`98_Shared/GameData`) + 서버 드롭표 | append-only, 한 곳 교체 | 빌드 타임 |

## 2. 불변식 목록

표기: `[소스]`는 실제 file:symbol/line, `[추론]`은 이유, `[미측정]`은 확인 방법. "소유자"는 그 불변식을 코드로 지키는 파트다.

### INV-01 경제 상태의 단일 소유자와 틱 스레드
- 보호 대상·적용 시점: PS-1~3, PS-6의 모든 읽기·쓰기는 World 틱 스레드의 레지스트리 job 안에서만 일어난다. 소켓 스레드(핸들러·`GameSession.Submit*`)는 `EnqueueJob`만 한다.
- 실패·종료·stale: job 밖에서 상태를 읽어 응답하면 종료·이동과 경쟁한다. 레지스트리 `Tick`은 `GameWorld.OnTick`의 명시 슬롯에서 drain한다.
- 깨지는 반례: 핸들러가 `GameWorld.Instance.Inventory.GetSnapshot()`을 소켓 스레드에서 직접 읽어 보내면, 같은 틱의 보상 job과 겹쳐 반쯤 바뀐 값을 보낼 수 있다.
- 관찰 테스트: `GameWorld` fixture에서 `C_InventoryRequest`를 소켓 경로로 제출하고, 응답이 다음 틱(`FlushSessionClosuresAsync` barrier 또는 수동 Tick) 뒤에만 나오는지 확인. 레지스트리의 `internal` 조회자는 테스트 전용임을 주석에 둔다.
- 전제: `[소스]` `GameWorld.OnTick` L247-267이 DrainSessionCloses → 맵 Tick → DrainSessionCloses → `Party.Tick` → `Quest.Tick` 순서로 단일 스레드에서 실행; `QuestRegistry.cs` L6 "All state access stays on the world tick thread". `[추론]` 경제 Tick은 `Quest.Tick` 뒤, barrier 완료(L260) 앞에 두면 테스트가 기존 barrier로 관측할 수 있다. `[미측정]` 틱 예산 영향은 측정하지 않았다.
- 소유자: Content(레지스트리), GameDev(`OnTick` 슬롯 한 줄).
- 대안 한 줄: 맵별 상태(`GameMap`)에 두면 cross-map 이동·파티 분배 때 다시 옮겨야 하므로 권하지 않는다.

### INV-02 소유자 키는 entityId, 수명은 월드 진입부터 세션 종료 정리까지
- 보호 대상·적용 시점: 소유자 키 = 서버 `_entityId`. 항목은 첫 보상 또는 첫 조회 때 생성되고 `DrainSessionCloses`에서 제거된다. 재접속은 새 entityId이며 빈 상태로 시작한다(영속 복원은 GameDev 저장 goal).
- 실패·종료·stale: 종료된 entityId로 도착한 늦은 job은 항목을 만들지 않는다(INV-15).
- 깨지는 반례: 키를 세션 참조로 잡으면 맵 이동 중 `GetMap()==null` 구간과 혼동되고, 키를 재사용 가능한 슬롯 번호로 잡으면 다른 플레이어가 상속한다.
- 관찰 테스트: 접속 → 보상 → 종료 → 새 접속 후 snapshot이 revision 0·빈 상태; `TrackedOwnerCount`(테스트용 internal)가 접속 수와 같다.
- 전제: `[소스]` entityId는 `GameWorld.NextEntityId` `Interlocked.Increment`로 전역 단조(L106), ADR-026 유지(L29-33); 종료 시 `CompleteWorldLeave`가 `_entityId=-1`(GameSession.cs L182-186). `[추론]` 프로세스 수명 안에서 id가 재사용되지 않으므로 entityId 단독으로 혼동이 없다. `[미측정]` 없음.
- 소유자: Content.
- 대안 한 줄: 계정/캐릭터 식별자는 저장 goal에서 도입하고, 이번 goal은 entityId만 쓴다.

### INV-03 맵 이동 동안 보존, PlayerEntity·GameMap에 두지 않음
- 보호 대상·적용 시점: PS-1~3은 `MapMigration.Execute` 전후로 바뀌지 않는다.
- 깨지는 반례: 인벤토리를 `PlayerEntity` 필드로 두면 `AddPlayerWithId`가 새 entity를 만들어 소실된다. `PlayerTransferState`에 실어 나르면 GameDev 파일 변경 범위가 커진다.
- 관찰 테스트: 보상 → `C_EnterPortal`로 이동 → 이동 완료 뒤 `C_InventoryRequest` → 같은 slots·currency·revision.
- 전제: `[소스]` `GameMap.AddPlayerWithId` L180-186이 새 `PlayerEntity` 생성; `MapMigration.cs` L109 `PlayerTransferState(entityId, Stats, Hp)`, L150 `AddPlayerWithId`; L115 `SetMigrating(1)`로 이동 중 `GetMap()` null(GameSession.cs L536-540). `[추론]` World 수준 키라 이동 코드 변경이 0이다.
- 소유자: Content.
- 대안 한 줄: 이동 시 snapshot 재전송이 필요하면 클라가 `S_MapTransition` 뒤 `C_InventoryRequest`를 다시 보낸다(서버 push 없음).

### INV-04 수혜자와 보상량은 서버 처치 콜백이 결정한다
- 보호 대상·적용 시점: 보상 진입점은 `GameMap.OnEnemyKilled(killerEntityId, target)` → `GameWorld.MakeMap`의 `onKill` 하나다. 보상 요청 패킷은 없다. 보상량은 서버 드롭표(`EnemyKind` → 묶음)에서 읽는다.
- 실패·종료·stale: 낙사 소멸(`DespawnEnemyByFall`)은 콜백을 부르지 않으므로 무보상이다. 이 동작을 보존한다.
- 깨지는 반례: 클라가 `targetEntityId`를 실은 "보상 수령" 패킷을 보내면 처치 사실 위조가 가능하다.
- 관찰 테스트: 기존 `HandleEnemyDeathKillerTests`(Melee/Dash/Deferred 처치자 전파, `KillSequence_OnEnemyKilled_CalledAfterRemoveEnemy`) 보존 + 새 테스트: 낙사 소멸 뒤 어떤 소유자도 보상 없음.
- 전제: `[소스]` `GameMap.HandleEnemyDeath` L426-441이 마지막에 `OnEnemyKilled`; `DespawnEnemyByFall` L532-538은 콜백 없음(주석 "파티 킬 크레딧 오발동 방지"); `MakeMap` L233-240. `[추론]` 드롭표는 `EnemyCatalog`가 서버 `internal`(EnemyCatalog.cs L41)이므로 서버 쪽에 둔다.
- 소유자: GameDev(콜백 호출), Content(콜백 소비·드롭표).
- 대안 한 줄: 드롭표를 Shared에 두면 클라가 미리 표시할 수 있지만 사용처가 없어 PR1에선 서버에 둔다.

### INV-05 수혜자는 적용 시점에 살아 있어야 한다
- 보호 대상·적용 시점: 보상 job 실행 시 `world.IsActiveEntity(killerEntityId)`가 false면 보상 전체를 버리고 항목을 만들지 않는다.
- 실패·종료·stale: 지연 피해는 공격자 세션이 닫힌 뒤에도 적을 죽일 수 있다. 같은 틱에서 처치 뒤 종료가 이어져도 `RemovePlayerBySession`이 먼저 실행되므로 보상 job 시점엔 비활성이다.
- 깨지는 반례: 썬더볼트 지연 피해 등록 → 공격자 Disconnect → `impactTick` 도달 → `HandleEnemyDeath(target, 닫힌 id)` → 경제 항목 생성 → 영구 잔류.
- 관찰 테스트: 지연 피해 등록 후 세션 종료 → barrier → 적 사망 틱 뒤 `TrackedOwnerCount` 불변, 해당 id 조회 null. 기존 `SessionCleanupTests.CloseBeforeEntry_BarrierRemovesSession_AndLatePacketsCannotReviveIt` 패턴을 따른다.
- 전제: `[소스]` `DeferredDamageSystem.cs` L59는 대상만 검사(`target == null || target.IsDead`), L80 `map.HandleEnemyDeath(target, impact.AttackerEntityId)`; `GameWorld.IsActiveEntity` L191-192; `DrainSessionCloses` L200-221이 맵 Tick 전후에 실행. `[추론]` grep 결과에 공격자 존재 검사가 보이지 않았으나 본문 전체를 열지 않았다. `[미측정]` 실제 지연 피해로 재현되는지는 테스트로 확인한다.
- 소유자: Content.
- 대안 한 줄: 공격자 종료 시 지연 피해를 취소하는 방법은 GameDev 전투 계약 변경이라 권하지 않는다.

### INV-06 처치 보상은 적 인스턴스당 정확히 한 번
- 보호 대상·적용 시점: 같은 `EnemyEntity`(같은 `EntityId`)에 대해 보상 job은 최대 한 번 enqueue된다. 경제 레지스트리는 **무제한 "처리한 적 id" 집합을 두지 않고** 이 보장을 기존 `GameMap` 불변식에서 받는다.
- 실패·종료·stale: 재출현은 새 `EntityId`의 새 entity이므로 "같은 보상 재지급"이 아니다.
- 깨지는 반례: 이미 죽은(Hp≤0, `_enemies`에서 제거된) 같은 객체를 다시 `ApplyImmediateEnemyHit`에 넘기면 `HandleEnemyDeath`가 두 번 불려 두 번 지급된다. `ApplyImmediateEnemyHit` 자체에는 `IsDead` 사전 가드가 없다.
- 관찰 테스트: 기존 `KillSequence_OnEnemyKilled_CalledAfterRemoveEnemy` 보존 + 새 테스트: 한 틱에 두 공격 job이 같은 적을 때릴 때 보상 1회, revision +1회; 지연 피해가 사망 뒤 도달해도 보상 없음.
- 전제: `[소스]` `CombatSystem.cs` L76 `!e.IsDead` 필터; `DeferredDamageSystem.cs` L59 `IsDead` 건너뜀; `HandleEnemyDeath` L435 `RemoveEnemy`; `RespawnSystem.cs` L20-23, L75 새 id 발급; `ApplyImmediateEnemyHit` L394-416 사전 가드 없음. `[미측정]` Dash·텔레포트·보스 액션 등 `ApplyImmediateEnemyHit`의 다른 호출자가 생존 대상만 넘기는지는 읽지 않았다. 검증자는 호출자 목록을 grep해 각 가드를 표로 남긴다.
- 소유자: GameDev(사망 경로), Content(테스트 관측).
- 대안 한 줄(S-3): 방어선을 더 원하면 월드 전역 "최근 보상 적 id" 고정 길이 ring(예 256)으로 bounded 중복 거부를 두되, 영구 집합은 금지한다.

### INV-07 보상 묶음의 원자성
- 보호 대상·적용 시점: 한 처치의 묶음(재료 n + 재화 m + …)은 슬롯 공간·스택 상한·재화 상한을 **모두** 통과할 때만 전부 적용되고 revision이 1 증가한다. 하나라도 실패하면 아무것도 바뀌지 않고 revision도 그대로다.
- 구현 구조: 소유자 상태를 불변 객체(슬롯 배열 복사 + currency + revision)로 두고, 검사·계산은 순수 함수가 새 객체를 반환하며, 성공 시 레지스트리가 참조 하나를 교체한다. 예외는 계산 단계에서만 날 수 있고 교체 전이므로 부분 변경이 없다.
- 깨지는 반례: `AddItem` 성공 뒤 `AddCurrency`가 상한 초과로 실패 → 재료만 남음. 또는 `slots[i].count++` 뒤 예외 → `QuestRegistry.Tick`식 catch가 로그만 남기고 반쯤 바뀐 상태가 전송됨.
- 관찰 테스트: 슬롯 가득(또는 스택 상한 직전)에서 "재료+재화" 묶음 보상 → snapshot 전후 동일, revision 동일, 재화 동일. 기대값은 테스트 쪽 고정 상수(예: 사전 보상 3회 뒤 재화 30)로 두고 제품 계산을 복제하지 않는다.
- 전제: `[소스]` `QuestRegistry.Tick` L27-31의 catch-log 패턴이 그대로 쓰이면 부분 변경을 숨긴다. `[추론]` 참조 교체는 단일 스레드 안에서 원자적이며 할당 비용은 변경 빈도(초당 수 회)에서 무시할 수 있다. `[미측정]` 할당·GC 영향은 측정하지 않았다.
- 소유자: Content.
- 대안 한 줄(S-2): 상한 도달 시 "재화만 상한까지 clamp, 아이템은 적용"은 원자성 설명이 복잡해져 PR1에선 전체 거부를 권한다.

### INV-08 사용 판정의 원자성
- 보호 대상·적용 시점: `C_ItemUse` 처리는 (a) 요청자 활성, (b) `expectedRevision == revision`, (c) 카탈로그 정의·사용 가능 종류, (d) 소유 수량 ≥ 1, (e) 효과 적용 후 재화 ≤ 상한을 모두 검사한 뒤 새 상태를 계산·교체한다. 수량 차감과 효과 적용은 한 교체다.
- 깨지는 반례: 주머니 1개 차감 → 재화 +50이 상한 초과 → 거부했지만 주머니는 이미 줄어듦.
- 관찰 테스트: 재화 상한 직전 + 주머니 1개 → 사용 → `S_ItemUseResult(CurrencyCap)`, 주머니 수량·재화·revision 모두 불변.
- 전제: `[추론]` INV-07과 같은 구조 재사용. `[소스]` 사용 효과는 draft 3항 "주머니 → 정해진 양의 재화" 하나다.
- 소유자: Content.
- 대안 한 줄: 효과 종류를 늘리더라도 "효과 적용은 순수 함수가 새 상태를 반환"하는 계약은 유지한다.

### INV-09 음수·overflow 금지
- 보호 대상·적용 시점: currency ∈ [0, MaxCurrency], slot count ∈ [0, MaxStack], revision은 uint 증가. 모든 증감은 사전 경계 검사(`gain <= MaxCurrency - balance`)로 수행하며 `checked` 산술은 2차 방어다.
- 깨지는 반례: `int` 잔액에 `int.MaxValue` 근처에서 보상 → 음수로 wrap; 드롭표에 음수 값을 넣음.
- 관찰 테스트: 잔액을 테스트용 internal로 `MaxCurrency - 1`에 두고 보상 10 → 전체 거부; 드롭표 값 음수/0은 카탈로그 로드 시 예외(fail loud, `GameMap` ctor의 kindId 검증과 같은 태도).
- 전제: `[소스]` `GameMap.cs` L99-105 fail-loud 선례; PDL `int`/`uint`/`long` 지원(Program.cs L200-209). `[추론]` `MaxCurrency ≤ 1_000_000_000`이고 한 번의 gain ≤ MaxCurrency면 `balance + gain`은 int 범위 안이라 사전 검사만으로 overflow가 없다. `[미측정]` 없음.
- 소유자: Content(+Shared 상수).
- 대안 한 줄: `long` 잔액은 여유가 크지만 패킷·UI 타입이 늘어 PR1은 `int` + 명시 상한을 권한다.

### INV-10 revision 단조성
- 보호 대상·적용 시점: 소유자별 revision은 성공한 상태 변경마다 정확히 1 증가하고, 거부·중복·내부 실패에서는 변하지 않는다. 모든 `S_InventorySnapshot`·`S_ItemUseResult`는 당시 revision을 싣는다.
- 깨지는 반례: 거부 응답에서 revision을 올리면 클라의 `expectedRevision`이 영원히 어긋난다.
- 관찰 테스트: 보상 k회 + 성공 사용 j회 뒤 revision == k + j; 거부 n회가 섞여도 동일.
- 전제: `[추론]` uint 2^32 변경은 한 프로세스 수명에서 도달하지 않으므로 wrap 처리는 두지 않는다(계약에 명시). `[미측정]` 없음.
- 소유자: Content.
- 대안 한 줄: `long` revision은 과잉이다.

### INV-11 사용 요청의 stale·중복 거부는 expectedRevision 하나로
- 보호 대상·적용 시점: `C_ItemUse.expectedRevision != revision`이면 `Stale`로 거부하고 현재 revision을 돌려준다. 같은 패킷의 재전달은 첫 성공 뒤 revision이 바뀌어 자동으로 Stale이 된다. 서버는 요청 ID 집합을 저장하지 않는다.
- 실패·종료·stale: 처치 보상이 끼어들어 revision이 바뀌면 사용도 Stale이 된다. 클라는 snapshot을 받고 다시 시도한다(의도된 동작, 표시 일치 우선).
- 깨지는 반례: 재전달 식별 없이 수량만 검사하면 더블 클릭이 두 번 소비된다. 요청 ID를 HashSet에 쌓으면 세션당 무제한 증가.
- 관찰 테스트: 같은 바이트의 `C_ItemUse` 2회 → 첫 번째 Success, 두 번째 Stale, 수량 -1만; 보상 사이에 끼운 사용 → Stale 후 재시도 성공.
- 전제: `[소스]` TCP 단일 스트림이라 와이어 재정렬은 없음(Session.cs L25-55 순차 처리). `[추론]` 추가 per-owner 상태 0으로 draft 5항 "경제 revision과 세션 수명의 결합"을 충족한다.
- 소유자: Content.
- 대안 한 줄: 클라 단조 `requestId` + 소유자당 `lastRequestId` watermark(uint 1개)도 bounded지만 "낡은 화면에서의 소비"는 막지 못한다.

### INV-12 요청자 소유·수명 gate
- 보호 대상·적용 시점: 행위자는 패킷이 아닌 `GameSession._entityId`다. 핸들러는 `RequiresSelectedClass = true`, 파싱·범위 검증 후 `session.SubmitItemUse/SubmitInventoryRequest`만 호출한다. Submit은 `_entityId < 0`·`_world == null`이면 drop, 아니면 entityId·session을 캡처해 경제 큐에 job을 넣고, job은 `world.IsActiveSession(session, entityId)`를 먼저 검사한다.
- 실패·종료·stale: 맵 이동 중(어느 맵에도 없음)·종료 중은 비활성 → 무응답 drop. 응답 수신자가 없으므로 결과 패킷을 만들지 않는다.
- 깨지는 반례: 핸들러가 `GameWorld.Instance`를 직접 써 세션 gate를 우회; 캡처한 entityId가 종료 뒤 `-1`로 바뀐 `_entityId`와 혼동됨.
- 관찰 테스트: 기존 `SessionCleanupTests.QueuedAttack_OnlyLiveOwnerCanDamageTarget` 패턴으로 "큐에 든 사용 요청은 종료 뒤 실행되지 않음"; 클래스 선택 전 `C_ItemUse`는 `[Trust]` drop.
- 전제: `[소스]` `GameSession.SubmitCheatCommand` L423-439(월드 job + `IsActiveSession`), `OnRecvPacket` L169-173(gate), `IPacketHandler.RequiresSelectedClass` 컴파일 강제, server.md 「수정 흐름」.
- 소유자: GameDev(세션 Submit 2개 추가 — 합의 범위), Content(핸들러·job).
- 대안 한 줄: 없음(기존 관례 그대로).

### INV-13 입력 경계 검증과 "서버 수신 패킷은 고정 크기"
- 보호 대상·적용 시점: `C_ItemUse.itemId`는 카탈로그 정의값(0=None 제외)만, 수량은 PR1에서 1 고정. 서버가 수신하는 새 패킷 2개는 고정 크기이며 `string`·`list`를 쓰지 않는다. 미정의 itemId는 `[Trust]` 로그 + silent drop(응답 없음).
- 실패·종료·stale: 프레임 크기 위반은 기존 `Session.OnRecv`가 Disconnect. 선언 크기는 맞지만 짧은 payload는 생성 `Read`가 예외를 던지며 `OnRecvPacket` 호출부에 try/catch가 없다.
- 깨지는 반례: 클라 패킷에 가변 list를 두면 서버가 처음으로 가변 길이 클라 입력을 파싱하게 되고, count 65535 선언으로 `Slice` 예외 경로가 열린다.
- 관찰 테스트: `PacketRoundTripTests` 관례로 새 4패킷 왕복·size 헤더·ID; 핸들러 테스트로 미정의 itemId → 상태 불변·응답 없음.
- 전제: `[소스]` `Session.cs` L25-65(프레임 검증 → `OnRecvPacket` L51, try/catch 없음); `FrameValidator.MaxFrameSize = 4096` L24; PDL에 `<string>`은 `S_HandshakeResult.reason` 하나(L96)뿐이고 `C_` 패킷은 모두 고정 크기; `SkillCatalog.cs` 주석 "미정의 skillId는 서버 silent drop". `[미측정]` 수신 콜백이 `Read` 예외를 어떻게 처리하는지(연결 종료인지 프로세스 영향인지)는 `Session.cs` 전체를 읽지 않아 모른다. 검증자가 짧은 payload 1건을 실제로 보내 관측한다.
- 소유자: GameDev(수신 경로), Content(핸들러 검증·PDL).
- 대안 한 줄: 핸들러가 `buffer.Count`를 기대 크기와 비교한 뒤 `Read`하면 예외 경로를 줄이지만, 기존 핸들러와 다른 관례가 되므로 Astra 판단.

### INV-14 snapshot 크기 상한과 고정 슬롯 수
- 보호 대상·적용 시점: `S_InventorySnapshot`은 고정 N 슬롯(itemId, count)으로 크기가 상수이며 4096 바이트 프레임 상한 안이다. 클라는 itemId 범위·count 범위를 적용 전에 검사하고 벗어나면 적용하지 않는다(서버 버그 방어).
- 깨지는 반례: `list` 사용 시 생성 코드가 count를 `BitConverter`(호스트 엔디안)로 쓰고 읽어 다른 필드의 `BinaryPrimitives` LittleEndian 관례와 어긋난다. NET_LEGACY 분기의 Write는 정의되지 않은 `Segment` 식별자를 참조한다.
- 관찰 테스트: 왕복 테스트에서 snapshot 바이트 길이가 상수인지, `S_PartyUpdate`처럼 빈 슬롯 = 0 규약이 지켜지는지.
- 전제: `[소스]` `PacketFormat.cs` L300·L310 `BitConverter.ToUInt16`, L389 `BitConverter.TryWriteBytes`, L395 `Segment.Array`; `GenPackets.cs` 생성 Write는 `segment`/`s`만 선언(L586-595); PDL에 `<list>` 사용 0건; `ProtocolVersion.cs` L41·L61 주석은 "미지원"이라 적혀 `Program.cs` L223-228과 불일치. `[미측정]` NET_LEGACY 정의 여부와 list 생성 코드의 컴파일·왕복은 확인하지 않았다. `[추론]` 8슬롯 × 8바이트 + revision 4 + currency 4 + 헤더 4 = 76바이트.
- 소유자: Content(PDL·양쪽 핸들러).
- 대안 한 줄(S-1): list를 쓰려면 "list 패킷 왕복 테스트 + Shared/ClientNet 두 빌드 + 엔디안 불일치 수용"을 PR1 검증 범위에 추가해야 하며, 생성기 수정은 goal 범위 밖이다.

### INV-15 세션 종료 정리와 늦은 job의 상태 생성 금지
- 보호 대상·적용 시점: `DrainSessionCloses`가 `entityId >= 0`인 세션마다 경제 큐에 `Forget(entityId)`를 enqueue한다. 종료 뒤 도착한 보상·사용 job은 INV-05·INV-12 가드로 drop된다. Forget은 멱등이다.
- 순서 근거: 같은 틱에서 "맵 Tick의 처치 → 보상 job enqueue → DrainSessionCloses의 Forget enqueue → 경제 Tick"이면 보상 job이 먼저 실행되지만 이미 `RemovePlayerBySession`이 끝나 비활성 → drop, 이어 Forget. 반대로 다음 틱에 종료되면 항목이 생겼다가 Forget된다. 두 경우 모두 잔류 0.
- 깨지는 반례: Forget을 `PartyFlow.CleanupOnDisconnect`처럼 별도 큐에만 넣고 경제 큐 순서를 보장하지 않으면 보상 job이 Forget 뒤에 실행돼 고아가 남는다(가드가 없을 때).
- 관찰 테스트: 접속·보상·종료 N회 반복 뒤 `TrackedOwnerCount == 0`; 종료 뒤 같은 entityId로 보상 job을 수동 enqueue해도 항목 없음.
- 전제: `[소스]` `DrainSessionCloses` L200-221(파티 정리 L216, `CompleteWorldLeave` L217), `OnTick`에서 맵 Tick 전후 2회 호출(L253, L256). `[추론]` 경제 큐가 FIFO `ConcurrentQueue`면 같은 큐 안의 순서는 보장된다.
- 소유자: GameDev(`DrainSessionCloses` 한 줄), Content(Forget·가드).
- 대안 한 줄: 세션 `ConnectionClosed` 이벤트 구독은 소켓 스레드 콜백이라 큐 enqueue 외의 처리를 두면 안 된다.

### INV-16 틱 I/O 금지와 저장 접점은 값 snapshot만
- 보호 대상·적용 시점: 경제 job은 DB·파일·네트워크 완료를 기다리지 않는다. 전송은 `Session.Send`(lock + enqueue)뿐이다. 저장 접점은 `PlayerSnapshot`과 같은 문구의 `readonly record struct`(entityId, revision, currency, slots 복사)로 틱 스레드에서 캡처한 **값**이며 DB 식별자·저장 큐·복원 형식을 정의하지 않는다.
- 깨지는 반례: "저장 성공" 표시나 `ISaveStore` 인터페이스를 사용처 없이 선언.
- 관찰 테스트: 레지스트리 public API 서명에 `Task`/`async`가 없음을 리뷰로 확인(실행 테스트 아님).
- 전제: `[소스]` `PlayerSnapshot.cs` L6-10 문구; `GameMap.cs` L473-474 "Owner.Send는 Session.Send → lock + queue enqueue"; GameDev 합의 "임의 hook 금지"(`msg_58eaccfd4a15`). `[추론]` 서버 전용 타입이면 `record struct`·`init`을 써도 되고, Shared로 옮길 때만 .NET Standard 2.1 `IsExternalInit` 문제를 본다(protocol.md).
- 소유자: Content, 계약은 GameDev와 합의.
- 대안 한 줄: snapshot 타입을 Shared에 두는 것은 클라 사용처가 생길 때로 미룬다.

### INV-17 기존 처치 순서·quest 분기 보존
- 보호 대상·적용 시점: `MakeMap.onKill`은 기존 `_quest.EnqueueJob(...)`을 그대로 두고, 그 **바깥**에 `_inventory.EnqueueJob(() => 보상(killerId, target.Kind, target.EntityId))`를 한 줄 추가한다. 보스 처치의 quest reset 분기와 독립이다. `GameMap`·`HandleEnemyDeath`·`EnemyEntity`는 수정하지 않는다.
- 깨지는 반례: 경제 보상을 quest job의 `else` 분기 안에 넣으면 보스 처치가 무보상이 되는 결정을 암묵적으로 내린다. `HandleEnemyDeath` 안에서 레지스트리를 직접 호출하면 actor 경계를 넘는다.
- 관찰 테스트: 기존 `QuestKillCountTests`, `QuestMembershipLifecycleTests`, `BossStageClear*`, `PartyQuestSmokeTests` 전수 통과(실패 시 전수 분류); 보스 처치 1회에 quest reset과 보상 job이 각각 한 번.
- 전제: `[소스]` `GameWorld.MakeMap` L233-240; `HandleEnemyDeath` 순서 주석 L418-425; CODE_CONVENTION "시스템끼리의 호출로 상태 소유권과 actor 경계를 우회하지 않는다".
- 소유자: GameDev 파일, Content가 합의 범위에서 작성.
- 대안 한 줄: 람다에서 `target`을 캡처해 경제 job에서 늦게 읽지 말고 `Kind`·`EntityId` 값만 넘긴다(적 객체는 respawn 큐에서 재사용되지 않지만 값 캡처가 더 단순하다).

### INV-18 카탈로그 단일 출처·append-only
- 보호 대상·적용 시점: `ItemId`(0=None), 종류별 `MaxStack`·사용 가능 여부·사용 효과, `MaxSlots`·`MaxCurrency`, 결과 코드 enum은 Shared 한 곳에 있고 append-only다. 클라의 "사용 버튼 활성" gate와 서버 거부가 같은 매핑을 읽는다.
- 깨지는 반례: 서버 핸들러에 `if (itemId == 2)` 같은 매직 숫자; 클라가 자체 표에서 사용 가능 여부를 판단.
- 관찰 테스트: 카탈로그 값 테스트(`EnemyCatalogValueTests` 관례)로 모든 ItemId가 정의·양수 상한을 갖는지; 미정의 id는 `IsDefined == false`.
- 전제: `[소스]` `SkillCatalog.cs` 머리 주석(§3·§4 실현, append-only), `98_Shared/GameData/Enums/` 기존 enum 배치. `[추론]` 드롭표는 서버에 둬도 "한 곳 교체"를 만족한다.
- 소유자: Content.
- 대안 한 줄: 데이터 파일(JSON/bin) 로드는 사용처가 늘어난 뒤로 미룬다.

### INV-19 클라 표시는 서버 확정만
- 보호 대상·적용 시점(PR2 입력): 미러는 `S_InventorySnapshot`만을 표시 원천으로 쓰고 `S_ItemUseResult`는 토스트/재시도 신호로만 쓴다. 첫 snapshot 전에는 "0"이 아니라 미동기 상태를 표시한다. 적용은 `EnqueueApply`로 메인 스레드에서, 리셋은 `ResetGlobalSessionMirrors`의 "값 리셋 → 통지" 순서에 포함한다.
- 깨지는 반례: 사용 버튼 클릭 즉시 로컬 수량을 줄여 표시; 세션 리셋 뒤 이전 세션의 큐에 남은 apply가 새 미러를 덮음.
- 관찰 테스트: EditMode 테스트로 `InventoryState.ApplyUpdate` 뒤 이벤트 1회, `ResetSessionValues` 뒤 revision 0; 실제 플레이는 PR2.
- 전제: `[소스]` `QuestState.cs` L41-71, `QuestUpdateHandler.cs` L22-26, `NetworkService.cs` L96-113. `[미측정]` `UnityClientSession`/`MainThreadDispatcher`가 이전 세션의 큐를 버리는지 읽지 않았다. 세션 generation 가드가 필요한지는 PR2 검증자가 확인한다. `[추론]` 단일 TCP 스트림 + 단일 메인 스레드 큐라 같은 세션 안에서 snapshot 순서는 보존된다.
- 소유자: Content(PR2).
- 대안 한 줄: 미러가 `revision <= current`를 무시하는 가드는 같은 세션 안에서만 유효하므로 리셋 시 반드시 0으로 되돌린다.

### INV-20 DEBUG 주입 경로 금지·격리
- 보호 대상·적용 시점: PR1은 보상·재화 직접 주입 치트를 만들지 않는다. 뒤에 추가하더라도 `CheatCommandHandler`처럼 `#if DEBUG` + Release 미등록이어야 하며, 봇·테스트 증거에서 처치 경로 완료를 대신하지 못한다.
- 깨지는 반례: 봇 시나리오가 `SendCheatCompleteQuestCore`류 주입만으로 "획득 확인"을 보고.
- 관찰 테스트: `CheatBuildGateTests.C_CheatCommand_Registration_IsBuildGated` 관례; 봇 원시 로그에 `S_HitResult`→`S_EntityDeath`→`S_InventorySnapshot` 순서가 있어야 한다.
- 전제: `[소스]` `CheatCommandHandler.cs` L1-33, `HandlerRegistry.cs` L37-42, goal 「검증 계획」 봇 행.
- 소유자: Content.
- 대안 한 줄: 없음.

## 3. 오류 분류와 전달·변경 보존

| 분류 | 예 | 클라 전달 | 상태·revision | 로그 |
|---|---|---|---|---|
| 정상 거부 | 수량 부족, 사용 불가 종류(재료), 재화 상한, 인벤토리 가득/스택 상한(보상·사용 모두) | `S_ItemUseResult{result, itemId, revision}`; 보상 거부는 전달 없음(요청자가 없음) | 변경 없음, revision 불변 | 선택(info) |
| 중복/stale | `expectedRevision` 불일치, 같은 패킷 재전달, 이동 중·비활성·종료 뒤 늦은 job | Stale은 `S_ItemUseResult(Stale, 현재 revision)`; 비활성·종료는 무응답(수신자 없음) | 변경 없음 | `[Economy]` 1줄(stale), 비활성은 `[Trust]` 없이 1줄 |
| 프로토콜 위반 | 프레임 크기, 클래스 선택 전 입력, 미정의 itemId·범위 밖 값, 짧은 payload, 핸드셰이크 전 전송 | 기존 경로 유지: Disconnect(프레임·첫 패킷) 또는 silent drop(`[Trust]`) | 변경 없음 | `[Trust]` |
| 내부 불변식 실패 | 슬롯 중복 itemId, 음수 count, revision 역행, 카탈로그 불일치, 드롭표 음수 | **성공 응답 금지**, 거부 응답도 보내지 않음(원인 은폐 방지) | 참조 교체 전 예외라 변경 없음 | `[Economy][Invariant]` + 테스트는 internal 검사로 관측 |

규칙: 내부 실패를 "정상 거부"나 기본값 상태로 바꾸지 않는다. 정상 거부와 stale은 사용자 메시지가 다르므로 결과 코드를 분리한다. 성공 시에는 `S_ItemUseResult(Success)`에 이어 `S_InventorySnapshot`을 push해 표시 원천을 하나로 둔다.

## 4. 처치 보상 경로(틱 순서 기준)

1. 맵 Tick job: 공격/지연 피해 → `ApplyImmediateEnemyHit` 또는 `DeferredDamageSystem` → `HandleEnemyDeath(target, killerId)` → Death broadcast → (보스) StageClear → `RemoveEnemy` → (비보스) `EnqueueRespawn` → `OnEnemyKilled` `[소스 GameMap.cs L394-441]`.
2. `GameWorld.MakeMap.onKill`: 기존 quest job enqueue 유지 + 경제 job enqueue(값 캡처: killerId, Kind, EnemyEntityId) `[소스 L233-240]`.
3. 같은 틱 `OnTick`: 맵 Tick 종료 → `DrainSessionCloses` → `Party.Tick` → `Quest.Tick` → **경제 Tick(신설)** `[소스 L253-259]`.
4. 경제 job: `IsActiveEntity(killerId)` → 드롭표 조회 → 묶음 전체 검사 → 새 상태 계산 → 참조 교체 → revision+1 → `world.SendToEntity(killerId, S_InventorySnapshot)`(다음 맵 틱에 송신, 0~1틱 지연) `[소스 SendToEntity L142-161]`.
5. 실패(비활성·상한)는 상태·revision 불변, 로그 1줄.

## 5. 사용 경로

1. 소켓 스레드: `OnRecvPacket` gate(`RequiresSelectedClass`) → `ItemUseHandler.Handle`: `Read` → itemId 정의 검사(미정의 → `[Trust]` drop) → `session.SubmitItemUse(itemId, expectedRevision)`.
2. `SubmitItemUse`: `_entityId < 0`·`_world == null` drop → entityId·`this` 캡처 → 경제 `EnqueueJob`.
3. 경제 job: `IsActiveSession(session, entityId)` 아니면 drop → revision 비교 → 소유·종류·효과·상한 검사 → 새 상태 교체 → `session.Send(S_ItemUseResult(Success))` → `session.Send(S_InventorySnapshot)`; 거부는 `S_ItemUseResult(reason)`만.
4. `C_InventoryRequest`: 2~3과 같되 검사 없이 현재 snapshot(없으면 빈 상태 revision 0)을 `session.Send`.

## 6. 수명·비용 상한

| 대상 | 상한·수명 | 근거 |
|---|---|---|
| 소유자 항목 수 | ≤ 현재 활성 플레이어 수(+ 같은 틱 Forget 대기) | INV-02, INV-15 |
| 항목 크기 | 고정 N 슬롯 × (int, int) + int currency + uint revision ≈ 수십~백 바이트 | INV-14 |
| 처리 ID 저장 | 0(expectedRevision 방식) | INV-11 |
| 중복 처치 표식 | 0(기본안) 또는 고정 길이 ring | INV-06, S-3 |
| 패킷 크기 | 모든 새 패킷 고정 크기, snapshot < 4096 | `FrameValidator.MaxFrameSize` |
| 요청 폭주 | 세션당 경제 큐에 동시 대기 1건(S-5 채택 시) | §9.1 |
| revision | uint, wrap 미처리(계약 명시) | INV-10 |
| 자리표시 수치(측정값 아님, catalog에서 교체) | MaxSlots 8, MaxStack 99, MaxCurrency 1,000,000,000; Normal 처치 → 재료 1 + 재화 10, Golem 처치 → 주머니 1, 주머니 사용 → 재화 50 | 예시, 최종 기획 아님 |

## 7. 고정해야 할 패킷 필드·API·상한·정책(제안)

### 7.1 패킷(ID 35..38, ProtocolVersion 16 → 17 한 묶음 bump)
- 35 `C_InventoryRequest { byte reserved }` — `C_PartyLeave` 관례의 빈 패킷 자리잡이 `[소스 PDL.xml L364-366]`.
- 36 `S_InventorySnapshot { uint revision; int currency; int slot0ItemId; int slot0Count; … int slot7ItemId; int slot7Count }` — 빈 슬롯은 itemId 0·count 0(`S_PartyUpdate` 빈 슬롯 = 0 관례 `[소스 L379-387]`). 슬롯 수 N은 `MaxSlots` 상수와 동일해야 하며 테스트가 대조한다.
- 37 `C_ItemUse { int itemId; uint expectedRevision }` — 수량 필드 없음(PR1 사용량 1 고정, 사용처 없는 필드 금지). 수량이 필요해지면 새 패킷 또는 append + bump.
- 38 `S_ItemUseResult { byte result; int itemId; uint revision }` — `result`는 Shared enum(Success=0, Stale=1, NotOwned=2, NotUsable=3, CurrencyCap=4, InventoryFull=5, 이후 append).
- 새 4패킷은 `C_CheatCommand` 아래에만 append(`[소스 L413-415]`), 기존 1..34 변경 없음. 핸드셰이크가 동일 버전 강제라 봇·Unity 재빌드가 필요하다(`[소스 ProtocolVersion.cs L68-72]`).

### 7.2 서버 API 심볼(internal, 이름은 제안)
- `Items/InventoryRegistry`: `EnqueueJob(Action)`, `Tick(long)`, `GrantKillReward(int ownerEntityId, EnemyKind kind) → RewardOutcome`, `TryUseItem(int ownerEntityId, ItemId itemId, uint expectedRevision) → ItemUseOutcome`, `GetSnapshot(int ownerEntityId) → InventorySnapshot`, `Forget(int ownerEntityId)`, 테스트용 `internal TrackedOwnerCount`.
- `Items/InventoryNotifier`(`QuestNotifier` 관례): snapshot push(`SendToEntity`)와 1:1 결과(`session.Send`).
- `Items/KillRewardTable`: `EnemyKind → 보상 묶음`(서버).
- Shared: `GameData/Enums/ItemId.cs`, `GameData/Items/ItemCatalog.cs`(MaxStack·사용 가능·효과), `GameData/Items/InventoryLimits.cs`(MaxSlots·MaxCurrency), `GameData/Items/ItemUseResult.cs`(결과 enum).
- GameDev 파일의 예정 심볼(합의 `msg_58eaccfd4a15` 범위): `GameSession.SubmitInventoryRequest()`, `GameSession.SubmitItemUse(int, uint)`; `GameWorld` 필드 `_inventory`(ctor에서 `_quest` 뒤·맵 생성 전), 프로퍼티 `Inventory`, `OnTick`의 `Quest.Tick` 뒤 `Inventory.Tick`, `MakeMap.onKill` 한 줄, `DrainSessionCloses`의 Forget enqueue 한 줄; `HandlerRegistry` 2줄.

### 7.3 배치·이름
- 서버 새 폴더 `02_Server/GameServer/Items/`(도메인 폴더 관례 `Quest/`·`Party/`), 핸들러 `Handlers/Inventory/ItemUseHandler.cs`·`InventoryRequestHandler.cs`.
- 아이템 ID enum은 `98_Shared/GameData/Enums/`(기존 `SkillId`·`EnemyKind` 자리)에, 카탈로그·상한·결과 enum은 `98_Shared/GameData/Items/`에 둔다. goal의 "Shared Items" 폴더와 기존 Enums 관례를 함께 만족한다.
- 이름에 날짜·마일스톤·작업자 금지. FEATURE_MAP에 "아이템·인벤토리·재화" 행 1개를 추가해 진입점(핸들러 → `GameSession.Submit*` → `Items/InventoryRegistry`; `GameWorld` 처치 콜백 → `Items/`)을 적는다.

### 7.4 정책 기본안(교체 지점 하나)
- 수혜자 결정은 `ResolveRewardRecipients(killerId, kind) → IReadOnlyList<int>` 한 함수로 격리하고 기본안은 `[killerId]`다. 파티 분배는 이 함수 교체로 들어온다. 사용자 확정 정책이 아니며 팀원 명세의 열린 질문이다(goal 「획득 경로」).
- 보상 묶음은 결정적(확률 없음)으로 시작한다(S-6).

## 8. TDD 공개 경계와 관찰 방법

구현 전 실패 테스트는 공개 경계(패킷·세션 Submit·레지스트리 internal 조회)로 쓴다. 부재 API로 인한 컴파일 실패와 기대 행동 실패를 구분해 원시 결과를 남긴다.

| 불변식 | 경계 | 실패를 관찰하는 방법 |
|---|---|---|
| INV-04/05/06/17 | `GameWorld` fixture + `TestGameSession`(핸드셰이크 우회 관례, 예 `HandleEnemyDeathKillerTests`) | 적을 `ApplyImmediateEnemyHit`/지연 피해로 죽인 뒤 Tick → 송신 바이트에서 `S_InventorySnapshot` 1회, revision 1; 종료 세션은 0회 |
| INV-07/08/09 | `InventoryRegistry` internal 조회 + 고정 기대값 | 상한 직전 상태를 테스트가 만들고 결과 코드·수량·재화·revision 네 값을 각각 변수로 Assert |
| INV-10/11 | `C_ItemUse` 바이트 2회 제출 | 결과 코드 순서 Success→Stale, 수량 -1 |
| INV-12/15 | `SessionCleanupTests` 관례(barrier, 종료 전/후 큐) | 종료 뒤 실행된 job이 항목을 만들지 않음, `TrackedOwnerCount` |
| INV-13/14 | `PacketRoundTripTests` 관례 | 4패킷 왕복·size·ID·고정 길이; 미정의 itemId drop |
| INV-16 | 리뷰 | API 서명에 비동기 없음 |
| INV-03 | `MapTransitionIntegrationTests` 관례 | 이동 전후 snapshot 동일 |
| 봇 실제 경로 | `99_Tools/headless-bot/Scenarios/Inventory/`(새 폴더, `Combat/EmergencyCombatSmoke.cs` 관례) | `ProbeBase.HandleExtraPacket`으로 `S_InventorySnapshot`·`S_ItemUseResult` 수집; 처치 → 획득 → 사용 → 재화 증가 순서를 원시 로그로 보존. 치트 주입 금지 |
| Unity PR1 호환 | Shared DLL 복사 뒤 컴파일 | 새 패킷이 Unity 핸들러 없이도 unknown drop으로 안전한지(PR1), 표시는 PR2 |

기존 회귀 대상: `HandleEnemyDeathKillerTests`, `SessionCleanupTests`, `QuestKillCountTests`, `QuestMembershipLifecycleTests`, `QuestNotificationContractTests`, `PartyQuestSmokeTests`, `BossGateSmokeTests`, `MapTransitionIntegrationTests`, `PacketRoundTripTests`, `CheatBuildGateTests`. 실패가 생기면 전수 분류(a/b/c/d)와 같은 명령 전후 실적을 남긴다.

## 9. 설계 관찰(비차단)

### 9.1 범위 안 기술 선택(Astra 결정, 메인 보고만)
- S-1 snapshot 모양: **권장 고정 8슬롯(itemId, count)**. 대안 A 종류별 고정 필드(종류 추가마다 PDL append + bump, catalog만 바꿔선 안 됨). 대안 B 생성기 list(INV-14의 미검증 경로·엔디안 불일치, 생성기 수정은 범위 밖).
- S-2 상한 도달 보상: **권장 묶음 전체 거부 + 로그**. 대안 재화 clamp(원자성 설명 복잡, 표시 차이).
- S-3 중복 처치 표식: **권장 없음(기존 단일 발화 불변식 + 테스트)**. 대안 고정 길이 ring.
- S-4 결과 전달: **권장 1:1 결과·요청 응답은 캡처한 `session.Send`(선례 `S_PortalLocked`), 처치 push는 `SendToEntity`**. 대안 모두 `SendToEntity`(0~1틱 지연, 패턴 하나).
- S-5 요청 폭주 bound: 기존 공격·스킬·파티도 제한이 없어 현 신뢰 태도와 일치하지만, 경제 job은 응답 2개를 만든다. **권장 세션당 경제 job in-flight 1건(Interlocked 플래그, job 종료 시 해제)**. 대안 `IntentRateLimiter` 공유(이동 500/s 예산 의미가 바뀜 `[소스 L225-237]`).
- S-6 드롭: **권장 결정적 묶음**(TDD oracle 고정). 확률은 seed 주입 가능한 RNG 경계가 생긴 뒤.

### 9.2 메인/팀원 정책 입력(기술로 닫히지 않음)
- P-A 처치자 지급 vs 파티 공동 분배 — 기본안 처치자, 교체 지점 §7.4.
- P-B 보스 처치 보상 유무와 내용 — 콜백 분기는 독립이므로 어느 쪽이든 코드 한 줄.
- P-C 자리표시 수치(§6)를 전시 빌드에 그대로 쓰는지.

### 9.3 범위 밖 관찰(수정 요구 아님, 보고용)
- `QuestRegistry._soloProgress`·`_bossUnlocked`는 세션 종료 정리가 없다(`[소스]` L12-14, 제거 경로는 `ResetAllQuestProgress`·`ForgetPartyProgress`뿐). entityId가 재사용되지 않아 혼동은 없지만 느린 누수다. GameDev 소유.
- `QuestRegistry.OnKill`은 종료된 killer에도 적립한다(`[소스]` L35-51 활성 검사 없음). 경제는 INV-05로 다르게 간다.
- `ProtocolVersion.cs` L41·L61의 "PDL 가변 list 미지원" 주석은 `Program.cs` L223-228과 다르다. 다음 bump 주석에서 "미사용·미검증"으로 고치는 편이 정확하다.
- 생성 `Write()`는 패킷마다 `new byte[ushort.MaxValue]`를 할당한다(`[소스]` GenPackets.cs L591). snapshot push도 같은 비용 모델이며 측정 전 최적화 대상이 아니다.
- `Session.OnRecv`의 `OnRecvPacket` 호출에 try/catch가 없다(`[소스]` L51). 기존 모든 `C_` 패킷에 공통인 노출이며 이번 goal은 고정 크기 유지로 노출을 늘리지 않는다.

## 10. 실제 준수 위치·미검토·해당 없음·미실행

- 준수 위치: 맥락 메모(이 파일 머리), 불변식 번호와 전제 표기 §2, 오류 분류 §3, 제안 §7, 비차단 관찰 §9, 대안 한 줄은 각 INV와 S-항목에 포함. 소스 근거는 commit `6d37e7a`의 file:line.
- 미검토(읽지 않음): `ApplyImmediateEnemyHit`의 Dash·보스·텔레포트 호출자 가드, `DeferredDamageSystem`·`PartyRegistry`·`Session.cs` 전체 본문, `UnityClientSession`/`MainThreadDispatcher` 수명, headless-bot `BotSession` 송신 API, `GenPackets.cs` 전체, 기존 테스트 본문, `DEVELOPMENT.md` 실행 전제(실행하지 않음).
- 해당 없음: C# 서식·빌드 설정 절(코드 미작성), Unity `.meta`·GUID(PR2), DB(현 goal 범위 밖).
- 미실행: 빌드·테스트·생성기·봇·Unity·DB 모두 실행하지 않았다. 성능·틱 예산·GC는 측정하지 않았다. 모델 backend는 unknown이며 이 문서의 어떤 수치도 측정값이 아니다.
- 이 검토는 Opus 독립 구현 검증을 대신하지 않는다. 결함 번호 체계는 구현 뒤 검증자가 부여한다.
