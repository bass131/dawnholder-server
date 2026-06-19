# M7.7 마이그레이션 명세 (파일 단위 from → to)

> **영호 "더 세부적으로" 요청 (2026-06-20)** — `FOLDER_STRUCTURE_DRAFT.md`의 폴더 레벨을 *파일 하나하나* + NS before→after + 참조 영향 + Phase로 분해. 실측 인벤토리 기준(2026-06-20).
> 불변: behavior-invariant, 이동마다 frozen 참조 grep 0. 각 행 = 독립 commit 가능 단위.
> 범례: **MOVE**=파일 이동, **NS**=네임스페이스 텍스트만(파일 유지), **NEW**=신설, **EXTRACT**=코드 추출, **REFS**=참조 갱신 필요처.

---

## P1 — NS 정합 (파일 이동 0, NS 텍스트만)

### P1a. PacketGenerator NS (서버측 도구)
| 파일 | op | NS before | NS after |
|---|---|---|---|
| `99_Tools/PacketGenerator/Program.cs` | NS | `PacketGenerator` | `Dawnholder.Tools.PacketGenerator` |
| `99_Tools/PacketGenerator/PacketFormat.cs` | NS | `PacketGenerator` | `Dawnholder.Tools.PacketGenerator` |
- REFS: PacketGenerator는 독립 실행(entry=Program), 외부 참조 없음 → 컴파일만 확인. csproj RootNamespace와 정합.

### P1b. 클라 핸들러 NS = 폴더 (23개, 파일 이동 0 → .meta 무관)
`Dawnholder.Client.Network` → `Dawnholder.Client.Network.Handlers.{Sub}`:
- `Handlers/Combat/*` (6: EnemyAttack/EntityDeath/HitResult/PlayerAttack/PlayerHp/StageClear) → `...Handlers.Combat`
- `Handlers/Party/*` (3) → `...Handlers.Party` · `Handlers/Quest/*` (1) → `...Handlers.Quest`
- `Handlers/Roster/*` (3: EntitySpawn/PlayerJoin/PlayerLeave) → `...Handlers.Roster`
- `Handlers/Session/*` (3: EnterMap/HandshakeResult/Pong) → `...Handlers.Session`
- `Handlers/Skill/*` (2: ProjectileLaunch/SkillCast) → `...Handlers.Skill`
- `Handlers/Sync/*` (2: EntityState/Snapshot) → `...Handlers.Sync`
- `Handlers/Zone/*` (2: MapTransition/PortalLocked) → `...Handlers.Zone`
- `Handlers/IClientPacketHandler.cs` → `...Handlers`
- REFS: `UnityClientSession.cs`(등록 테이블)가 서브 NS `using` 8개 추가. MCP 재컴파일 0err 게이트.
- ⚠️ **세부 결정 P1-①**: 서버 핸들러도 동일하게 평면 NS(`...Handlers`)임(실측). 클라만 할지 / 서버도 대칭으로 `...Handlers.{Sub}` 할지. (권장: **둘 다 정합** — 대칭 + 폴더=NS. 단 HandlerRegistry usings 늘어남)
- ⚠️ **세부 결정 P1-②**: `IClientPacketHandler` vs `IPacketHandler` 이름 비대칭 — 통일할지(예: 둘 다 `IPacketHandler`)? (권장: 보류 — 이름 통일은 가치 낮음, 별도)

---

## P2 — 봇 하니스 (ProbeBase + 서브폴더 + 네이밍)

### P2a. ProbeBase 추출
| op | 대상 |
|---|---|
| NEW | `99_Tools/headless-bot/Scenarios/ProbeBase.cs` — 17 Probe 공통(Connector + connected/handshake/enterMap ManualResetEvent + HandlePacket 핸드셰이크 시퀀스 + WaitUntil) |
| 수정 | 17 Probe가 `ProbeBase` 상속, 시나리오 고유 로직만 override |
- REFS: 각 시나리오 파일 내부 Probe만 — 외부 참조 없음. 회귀 봇 16/16 동치 게이트.

### P2b. Scenarios 서브폴더 (18파일 MOVE)
| 서브폴더 | 시나리오 (Probe) |
|---|---|
| `Combat/` | EmergencyCombatSmoke(Combat) · RemoteAttackSmoke(Attack) · WhiffSwingSmoke(Whiff) · RangedHitSmoke(Ranged) · RangedWhiffSmoke(RangedWhiff) · HpSyncSmoke(Hp) |
| `Boss/` | BossFightSmoke(Fight) · BossGateSmoke(Gate) · BossStageClearSmoke(Boss) |
| `Skill/` | DashSmokeScenario(Dash) · TeleportSmokeScenario(Teleport) · ThunderboltAoeSmoke(Aoe) · FreezeSmoke(Freeze) |
| `Enemy/` | EnemyAiSmoke(Ai) |
| `Party/` | PartyQuestSmoke(Party) |
| `Movement/` | M2BasicMovement(–) · MapTransitionScenario(Transition) |
| `Roster/` | MultiRosterSmoke(Bot) |
- REFS: `Program.cs`(시나리오 디스패치)가 새 경로 참조 — 단 같은 어셈블리라 NS만 맞으면 됨(서브폴더 NS 유지 or `.Combat` 등). 결정 P2-③: 서브폴더 NS 부여 vs 유지(권장 유지).
- 네이밍 P2-④: `*Smoke` vs `*Scenario` 혼재(DashSmokeScenario는 둘 다) → 하나로(권장 `*Smoke` 통일, 봇=smoke test).

---

## P3 — 98_Shared 그룹화 (MOVE, NS 유지 `Shared.GameData`)

| op | 파일 → 목적지 |
|---|---|
| MOVE | `GameData/{ActionKind,AnimState,EnemyKind,HitEffect,SkillId}.cs` → `GameData/Enums/` |
| MOVE | `GameData/{MapDataFile,MapContent,Terrain}.cs` → `GameData/Map/` |
| MOVE | `GameData/{Formulas,PlayerStats,SkillCatalog}.cs` → `GameData/Combat/` |
| 유지 | `GameData/{Constants,Physics,InputBits}.cs` (루트 cross-cutting 코어) |
| 보류 | `Protocol/CharacterClass.cs` (D9 — 24참조 sweep, append-only) |
- NS 전부 `Shared.GameData` 유지(폴더만) → **using 변경 0, 참조 0 영향**. Shared.dll 출력 불변 = Unity 무영향. 빌드 게이트만.

---

## P4 — GameMap 분해 + PlayerEntity 경계 (EXTRACT, M8 토대 — 이동 아님)

| op | 내용 |
|---|---|
| EXTRACT | `Maps/GameMap.cs`에서 wire 조립 → **NEW `Maps/MapPacketPublisher.cs`**: S_Snapshot 조립(:302-313)·SendPlayerHp(:531)·SendInitialRosterTo(:552). GameMap은 publisher 호출만 |
| EXTRACT | tick step 보조 → **NEW** helper(들): player physics(:234-291)·enemy gravity(:651)·death/respawn 위임. GameMap.Tick=순서 orchestration |
| EXTRACT | `Maps/PlayerEntity.cs` 저장/휘발 경계 → **NEW `PlayerSnapshot` DTO**(Hp/Position/Stats). 휘발(_inputQueue/_posHistory/ActionFsm/jumpBuffer)은 제외 명시 |
- 검증: **WSL2 회귀 663 비감소 + S_Snapshot.Write() byte 동치 테스트**(wire 불변 계약). reviewer. trust-boundary 인접=Opus worker.
- ⚠️ 세부 결정 P4-⑤: MapPacketPublisher 위치 — `Maps/`(맵 소관) vs 신설 `Net/Presentation/`(직렬화 소관). (권장 Maps/ — 맵이 자기 상태를 publish)

---

## P5 — 데이터화 (EXTRACT/데이터, shotgun 제거)

| op | 내용 |
|---|---|
| 데이터 | **EnemyKind catalog**(서버): GameMap maxHp 중복(:89-95/:427-433) + 사망/리스폰/AI 분기 ~10곳 → `EnemyCatalog` 테이블/factory. 새 적=데이터 1행 |
| 데이터 | **스킬 정의**(클라): LocalPlayerInput SkillKeyMap(:42-47)+TrySendSkill switch(:141-205) + SkillCastHandler switch(:65-91) → 스킬 카탈로그. 새 스킬=데이터 1 |
| 리팩 | **CombatBootstrap installer화**(클라): Awake 10종 수동 wiring(:53-64) → installer/registry |
- 검증: 회귀 green + 스킬/적별 봇·xUnit + 클라 Play 육안(bucket-b).

---

## P6 — 실제 폴더 이동 (MOVE, ADR-033 승인 후, 最高위험·마지막)

| op | 파일 from → to | NS before → after |
|---|---|---|
| MOVE | `Network/GameSession.cs` → `Sessions/GameSession.cs` | `...Sessions` (이미 정합 — **파일만 이동**) |
| MOVE+NS | `Network/IntentRateLimiter.cs` → `Sessions/IntentRateLimiter.cs` | `...Network` → `...Sessions` |
| MOVE+NS | `Network/MapMigration.cs` → `Maps/Transitions/MapMigration.cs` | `...Network` → `...Maps.Transitions` |
| MOVE+NS | `Maps/PlayerEntity.cs` → `Entities/PlayerEntity.cs` | `...Maps` → `...Entities` |
| MOVE+NS | `Combat/EnemyEntity.cs` → `Entities/EnemyEntity.cs` | `...Combat` → `...Entities` |
| MOVE+NS | `Combat/EnemyState.cs` → `Entities/EnemyState.cs` | `...Combat` → `...Entities` (엔티티와 동행) |
| NS | `Maps/Systems/*.cs` (7: ActionGate·BossBehavior·Combat·DeferredDamage·EnemyAI·Respawn·Skill) | `...Maps` → `...Maps.Systems` (파일 유지) |
| 결과 | `GameServer/Network/` 폴더 **삭제**(3파일 전부 이전 → "Network 오버로드" 해소) · `Combat/`=CombatConstants+Hitbox만 잔류 |
- REFS 큰 것: **PlayerEntity 23 in-ref** + EnemyEntity·GameSession·MapMigration 참조 전부 `using` 갱신. 이동마다 **frozen 참조 grep 0**(`-DONE`·CODEOWNERS). 깨지면 해당 이동 보류.
- 각 행 = 독립 commit(revert 단위). 매 이동 후 WSL2 회귀 663 비감소.
- ⚠️ 세부 결정 P6-⑥: `Combat/` 잔류물(CombatConstants+Hitbox)이 2파일뿐 → 그냥 둘지 / `Maps/Combat/`나 다른 데 합칠지. (권장: 유지 — 전투 데이터 폴더로 의미 있음)

---

## 세부 결정 요약 (영호 확인)

| # | 결정 | 권장 |
|---|---|---|
| P1-① | 핸들러 NS=폴더를 **서버도** 대칭 적용? | 둘 다 정합 |
| P1-② | `IClientPacketHandler`/`IPacketHandler` 이름 통일? | 보류(가치 낮음) |
| P2-③ | 봇 Scenarios 서브폴더 NS 부여? | NS 유지(폴더만) |
| P2-④ | `*Smoke`/`*Scenario` 네이밍 통일? | `*Smoke`로 |
| P4-⑤ | MapPacketPublisher 위치 | `Maps/` |
| P6-⑥ | `Combat/` 잔류물(2파일) 처리 | 유지(전투 데이터) |
| (이전) | Maps vs World / D5·D6 포함 / enum NS / 클라 범위 | Maps유지 / 포함 / NS유지 / D7+데이터화 |

> 이 명세 승인 시 P1→P6 전부 AI-driven. 비가역(push/PR/merge)만 영호 GO.
