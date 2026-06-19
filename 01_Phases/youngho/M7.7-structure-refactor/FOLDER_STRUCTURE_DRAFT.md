# M7.7 폴더 구조 초안 (Folder Structure — before → after)

> **영호 승인 대상.** 이 초안이 승인되면 **이후 전부 AI-driven**(자율 실행). (영호 방향 2026-06-20: "MD로 읽게 정리하는 마인드면 쓸모없어 — 실제 폴더 정리가 돼야 함" → plan의 보수적 *이동 defer* 입장을 덮고 **실제 재구성**으로.)
>
> 원칙: **폴더 = 개념 = 네임스페이스 일치** / **정의는 데이터로**. 불변: behavior-invariant(게임 동작 0), 이동마다 frozen 참조(`-DONE`·CODEOWNERS) grep 0.
> 근거: `_diagnosis.md` + ADR-033(D1~D9) + `00_Document/FEATURE_MAP.md`.

---

## A. 02_Server — 서버 (핵심)

### Before (현재)
```
02_Server/
├── Network/                  Session·Listener·Connector·RecvBuffer·SendBuffer·JobQueue·FrameValidator  [저수준 전송, 깨끗]
└── GameServer/
    ├── Combat/               CombatConstants · EnemyEntity · Hitbox          ← "Combat인데 전투 로직 없음"
    ├── Network/              GameSession · MapMigration · IntentRateLimiter   ← "Network 오버로드" (NS=Sessions/Network 혼재)
    ├── Maps/                 GameMap · PlayerEntity · PortalTable · MapDataLoader
    │   ├── Actions/          Melee·Dash·Teleport·Thunderbolt·ActionRegistry·IGameAction
    │   ├── States/           Player(Movement/Combat)·Enemy·Boss FSM
    │   └── Systems/          Combat·EnemyAI·BossBehavior·Respawn·DeferredDamage·Skill·ActionGate   ← NS=...Maps (Systems 아님)
    ├── Handlers/             Combat/ Debug/ Movement/ Party/ Session/ Skill/ Zone/   [NS=폴더 정합, 유지]
    ├── Party/  Quest/  Loop/  Debug/   [유지]
```

### After (제안)
```
02_Server/
├── Network/                  [D1 유지] 저수준 전송 ONLY — 게임로직 0참조 (규칙 명문화)
└── GameServer/
    ├── Sessions/             [D2] ← GameSession(was Network/) + IntentRateLimiter   NS=...Sessions
    ├── Entities/             [D6] ← PlayerEntity(was Maps/) + EnemyEntity(was Combat/)   NS=...Entities  ※공통 베이스는 보류(Codex: 이른 추상화 부채)
    ├── Combat/               [D5] CombatConstants · Hitbox (전투 데이터·판정만, EnemyEntity 빠짐)
    ├── Maps/                 GameMap · PortalTable · MapDataLoader   (※ World/ 리네임은 보류 — 아래 결정 #1)
    │   ├── Transitions/      [D3] ← MapMigration(was Network/)   NS=...Maps.Transitions  ("네트워크 아님 = 존 이동")
    │   ├── Systems/          [D4] NS=...Maps.Systems 로 정합 (파일 이동 0, NS 텍스트만)
    │   ├── Actions/          [유지]
    │   └── States/           [유지]
    ├── Handlers/  Party/  Quest/  Loop/  Debug/   [유지]
```

**이동 요약**: GameSession→Sessions/ · MapMigration→Maps/Transitions/ · IntentRateLimiter→Sessions/ · PlayerEntity→Entities/ · EnemyEntity→Entities/ · Maps/Systems NS 정합.

---

## B. 98_Shared — 공유 (D8)

### After
```
98_Shared/
├── Protocol/    Generated/GenPackets.cs · ProtocolVersion.cs · CharacterClass.cs   [D9: CharacterClass 이동 보류 — 24참조 sweep]
└── GameData/
    ├── Enums/    ActionKind · AnimState · EnemyKind · HitEffect · SkillId          [흩어진 enum 모음]
    ├── Map/      MapDataFile · MapContent · Terrain
    ├── Combat/   Formulas · PlayerStats · SkillCatalog
    └── (root)    Constants · Physics · InputBits                                    [cross-cutting 코어]
```
※ 소스 파일 이동이라 **Shared.dll 출력 불변 → Unity 무영향**. NS는 폴더 따라가거나 유지(결정 #3).

---

## C. 99_Tools — 도구 (P2)

```
99_Tools/headless-bot/
├── ProbeBase.cs                        [신설 — 17개 *Probe 중복(connect/handshake/CharacterSelect/EnterMap/WaitUntil) 공통화]
└── Scenarios/{Combat,Boss,Skill,Party,Movement}/   [평면 17개 → 개념 서브폴더, *Smoke/*Scenario 네이밍 통일]

99_Tools/PacketGenerator/   namespace PacketGenerator → Dawnholder.Tools.PacketGenerator   [csproj RootNamespace 정합]
```

---

## D. 03_Client — 클라 (⚠️ Unity 위험 동반)

```
03_Client/Assets/Scripts/
├── Network/Handlers/**     [D7] NS 평면 Dawnholder.Client.Network → 폴더별(...Handlers.Combat 등). 파일 이동 0 = .meta 무관
├── Gameplay/              재분류: Npc·Portal→world interaction / CheatSender·PartyInviteSender→Network/ 송신기
└── (P5 데이터화/컴포지션 — 폴더 이동 아님)
    ├── 스킬 정의 데이터화 (LocalPlayerInput·SkillCastHandler 3 switch → 카탈로그)
    └── CombatBootstrap → installer/registry화
```
⚠️ **Unity 함정**: 파일 *이동* 시 `.meta`(GUID)가 함께 이동하면 prefab/scene serialized 참조 보존. MonoBehaviour는 NS 변경(파일 이동 0)이 가장 안전. 클라 이동은 MCP 재컴파일 + 영호 Play 육안(bucket-b) 검증.

---

## ★ 영호 결정 포인트 (승인 시 명시)

1. **Maps vs World 리네임**: `Maps/` 유지(권장, blast radius ↓) vs `World/`로 리네임(memory `future-maps-namespace-restructure`는 post-M8 권고). → **권장: 이번엔 Maps 유지**, World 대재편은 M8 후.
2. **D5/D6 포함 확정**: Combat 폴더 재편 + Entities/ 통합을 M7.7에 *포함*(영호 "real reorg" 방향 = 포함으로 해석). 맞는지 확인.
3. **98_Shared enum NS**: 폴더 따라 `...GameData.Enums`로 바꿀지 vs 폴더만 옮기고 NS 유지(blast radius ↓). → **권장: NS 유지, 폴더만**.
4. **클라 reorg 범위**: D7(핸들러 NS, 저위험) + Gameplay 재분류까지 vs P5 데이터화(스킬 카탈로그·CombatBootstrap)도 이번에. → 데이터화는 동작 동치 검증 + Play 육안 필요.

---

## 실행 순서 (승인 후 AI-driven)

1. **P1**: PacketGenerator NS + 클라 핸들러 NS (이동 0, 저위험) → 컴파일 게이트
2. **P2**: 봇 ProbeBase + Scenarios 서브폴더 → 봇 16/16
3. **P3**: 98_Shared 그룹화 → 양쪽 빌드 + WSL2 회귀
4. **P4**: GameMap broadcaster/tick 추출 + PlayerEntity 저장/휘발 경계 (M8 토대) → WSL2 회귀 + byte 동치
5. **P5**: 데이터화 (EnemyKind·스킬·CombatBootstrap) → 회귀 + Play 육안
6. **P6**: **실제 폴더 이동** (Sessions/·Entities/·Transitions/·Combat 재편·Maps.Systems NS) — ADR-033 승인 적용, 이동마다 frozen grep 0
   - 각 이동 = 독립 commit (revert 단위). frozen 참조 깨지면 해당 이동 보류.

> behavior-invariant 계약: 매 Phase WSL2 회귀 663/0/5 비감소 + 봇 16/16 + reviewer🔴0. 깨지면 즉시 revert.
