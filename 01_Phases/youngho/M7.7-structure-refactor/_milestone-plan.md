---
owner: youngho
milestone: M7.7
title: 구조 리팩토링 (Structure Refactor — reader-friendly + M8 persistence boundary)
grade: 대규모
status: planning
date: 2026-06-20
inputs:
  - _diagnosis.md (병렬 진단 3영역, file:line)
  - Codex 검토 9항목 (Architecture Visualizer refresh 후 보강)
  - Architecture Atlas SOLID/결합도 136 신호 (Schema 1.1, Files 194 / Types 265 / Relations 1444)
  - Rookiss 레퍼런스 비교 (M8 이월 메모)
---

# M7.7 — 구조 리팩토링 (Milestone Plan)

## 1. Executive Summary

M7.7은 **behavior-invariant 리팩토링** 마일스톤이다. 게임 동작을 1bit도 바꾸지 않고, **(a) 사람이 기능 위치를 예측할 수 있는 구조**, **(b) M8 DB 영속화가 깨끗한 상태 경계 위에 올라갈 토대**, **(c) shotgun surgery(한 기능 추가에 여러 파일 산발 수정) 감소**를 만든다.

전제 인식: **큰 뼈대는 이미 건강하다** — 의존 방향 단방향(저수준 Network가 게임 로직 0참조), 헌법 "서버 권위·클라=렌더러" 위반 0, Party/Quest는 M7.6에서 이미 PartyFlow/QuestRegistry/PartyNotifier로 분리됨. 따라서 이 마일스톤은 *대수술이 아니라 정돈*이다. 핵심 전략은 **"이동(move)보다 지도(map)와 경계(boundary)를 먼저"** — Codex 검토의 1순위.

목표는 **warning count 감소가 아니다.** Atlas의 136 신호는 *자동 위반 판정이 아니라 검토 신호*로 다룬다(§11에서 재분류). 거대 순환(53타입 SCC)을 "없애는 것"도 목표가 아니다 — 대부분 의도된 actor/registry/state-machine 패턴이다.

**M8 차단 최소선(이것만은 M8 전에 필수)**: P0(FEATURE_MAP) + P4(GameMap 분해 + PlayerEntity 저장/휘발 경계). 나머지(P1~P3, P5~P6)는 가독성·확장성 개선이며 M8과 병행하거나 후속으로 미뤄도 안전하다.

## 2. Non-Goals (이번에 하지 않는 것)

- **게임 동작 변경 금지** — 밸런스/판정/타이밍/네트워크 wire shape 불변. `Protocol.Version` bump 0.
- **거대 순환(SCC) 전면 해소 금지** — 단일 어셈블리 상호참조는 C#에서 정상. 인터페이스를 전 seam에 박는 과잉 추상화는 학부생 단독 규모에 부채. M8 관련 seam만 손대고 나머지는 §11에서 "검증/보류".
- **warning/diagnostic count 0 만들기 금지** — false-positive·deliberate 패턴은 그대로 둔다.
- **엔티티 공통 베이스(GameObject류) 도입 금지(이번엔)** — Codex 경고: 이른 base class는 추상화 부채. *저장 가능 상태 분리가 먼저*, 공통 베이스는 M8 이후 판단(Deferred §12).
- **DB/영속화 구현 금지** — M8 소관. 이번엔 *경계만* 긋는다(저장 후보 vs 휘발 식별).
- **대량 파일 이동 금지(P6 외)** — 이동은 ADR로 기준 확정(P0) 후 마지막에 최소로(P6). frozen `-DONE`·CODEOWNERS 참조를 깨므로(memory `project-reorg`).
- **UI 접미사 컨벤션 일괄 sweep 보류** — 클래스/파일 대량 rename은 Unity serialized ref 위험. Deferred(§12).

## 3. Phase List

| Phase | 제목 | 도메인 | 위험 | M8 차단? |
|---|---|---|---|---|
| **P0** | FEATURE_MAP + 네이밍/경계 ADR 초안 | 문서 | 최저(코드 0) | ✅ (지도) |
| **P1** | 저위험 정합 (doc drift + NS 정합, 이동 없음) | 문서+서버+클라 | 낮음 | — |
| **P2** | 봇 하니스 정리 (ProbeBase + 서브폴더) | 99_Tools | 최저 | — |
| **P3** | 98_Shared 개념 그룹화 (GameData 하위폴더) | 98_Shared | 낮음 | — |
| **P4** | M8 토대 핵심 (GameMap 분해 + PlayerEntity 경계) | 서버 | 중 | ✅ **필수** |
| **P5** | 데이터화 (EnemyKind·스킬·CombatBootstrap) | 서버+클라 | 중 | — |
| **P6** | 폴더·NS 재편 이동 (ADR 적용, 마지막) | 서버(+클라) | **높음** | — |

**의존성 그래프**: `P0 → {P1, P4, P6}` (FEATURE_MAP/ADR이 입력) · `P1 ∥ P2 ∥ P3` (상호 독립) · `P4 → P5(권장 순)` · `P6는 항상 마지막`(P0 ADR 승인 + P4/P5 정착 후, 이동 churn 방지).

**권장 실행 순서 (Codex §9 정합)**: P0 → P1 → (P2 ∥ P3) → **P4(M8 차단 핵심)** → P5 → P6.

## 4. Phase별 목표

- **P0** — 미래 독자가 기능을 헤매지 않게 하는 **FEATURE_MAP.md** 작성(기능별 entry/trust gate/orchestration/state owner/notification/client mirror/UI/persistence candidate/volatile state). 동시에 문서 드리프트 수정, 그리고 P6 이동의 *기준*이 될 **네이밍/경계 ADR 초안**(아직 적용 X, 결정만).
- **P1** — "이름이 거짓말하는" 것 중 *이동 없이* 고칠 수 있는 정합: PacketGenerator NS(csproj↔source), 클라 핸들러 NS=폴더(파일 이동 X, NS 텍스트만). 헌법/CLAUDE.md 문서 드리프트.
- **P2** — 봇 시나리오 17개의 `*Probe` 중복(connect→handshake→CharacterSelect→EnterMap 재구현)을 `ProbeBase`로 추출 + Scenarios 개념 서브폴더 + `*Smoke`/`*Scenario` 네이밍 통일.
- **P3** — `98_Shared/GameData` 14파일 평면을 개념 하위폴더(Enums/Map/Combat + 루트 코어)로 그룹화. NS 유지(폴더만) 우선.
- **P4** — **M8이 붙을 곳을 깨끗이.** GameMap(7책임)에서 wire/broadcast 조립과 tick step을 추출, PlayerEntity의 *저장 가능 상태*와 *휘발 런타임 상태* 경계를 그어 M8 스냅샷 DTO 청사진 확보.
- **P5** — 추가 시 산탄총 수술 나는 곳을 데이터/레지스트리로: EnemyKind catalog, 클라 스킬 정의, CombatBootstrap installer화. OCP.
- **P6** — P0 ADR을 적용한 폴더·NS *이동*: "Network" 3중 오버로드 해소, MapMigration 재배치, Combat/Maps 정합. frozen 참조 점검 동반. 가장 위험 → 마지막·최소.

## 5. Phase별 수정 범위

### P0 (코드 0, 문서만)
- 신설: `00_Document/FEATURE_MAP.md` (또는 `01_Phases/youngho/M7.7-structure-refactor/FEATURE_MAP.md`). 기능 ≥8개: Session/Auth, Movement, Combat(melee/hit), Skill, Enemy/AI, Party, Quest, MapTransition, (Respawn/Death).
- 신설: `00_Document/ADR/harness/ADR-033-structure-naming-boundaries.md` 초안 (Network/Combat/Maps.Systems/handler NS 기준 — *결정 제안*, 적용은 P6).
- 수정: `98_Shared/CLAUDE.md`(ProtocolVersion 표기 정정), 헌법 Repo Layout 다이어그램(+04_ClientNet), Party/Quest 흐름 서술(Atlas 최신 반영).
  - ⚠️ **명확화(plan-auditor 권고①)**: `ProtocolVersion.cs:81 = const Current = 16`이 *진실*, `CLAUDE.md:19 = Current=15`가 stale. → **CLAUDE.md만 16으로 수정**(문서를 실제값에 맞춤). 이는 `Protocol.Version` *bump이 아님* — behavior-invariant·헌법 §2 무관.

### P1 (NS 텍스트, 이동 0)
- `99_Tools/PacketGenerator/*.cs`: `namespace PacketGenerator` → `Dawnholder.Tools.PacketGenerator`(csproj RootNamespace 정합).
- `03_Client/Assets/Scripts/Network/Handlers/**/*.cs` (23): `Dawnholder.Client.Network` → 폴더별(`...Network.Handlers.Combat` 등). using 갱신. **Unity 컴파일 회귀 확인(MCP)**. ※파일 이동 0 → .meta/GUID 무영향.

### P2 (99_Tools, 격리)
- 신설: `99_Tools/headless-bot/Scenarios/ProbeBase.cs` (connect/handshake/CharacterSelect/EnterMap/WaitUntil 공통).
- 수정: 17 시나리오가 ProbeBase 상속하도록 + `Scenarios/{Combat,Boss,Skill,Party,Movement}/` 서브폴더 이동.
- 선택: `Program.cs` if-체인 → `Dictionary<string,Func>` 레지스트리.

### P3 (98_Shared 폴더 이동, NS 유지)
- `98_Shared/GameData/Enums/`(ActionKind,AnimState,EnemyKind,HitEffect,SkillId), `Map/`(MapDataFile,MapContent,Terrain), `Combat/`(Formulas,PlayerStats,SkillCatalog). 루트=Constants,Physics,InputBits.
- CharacterClass(현 Protocol/, 24 사용처)는 **이동 보류**(append-only stability + 넓은 sweep) — ADR에 사유 기록.

### P4 (서버, 순수 추출) — **M8 차단 핵심**
- **P4a**: GameMap에서 `S_Snapshot`(:302-313)/`SendPlayerHp`(:531)/`SendInitialRosterTo`(:552) 조립·송신 → `MapPacketPublisher`(또는 `SnapshotBroadcaster`)로 추출.
- **P4b**: tick step(player physics :234-291 / enemy gravity :651 / death-respawn :477,502,691 / stage clear)을 보조 클래스로. GameMap=순서 orchestration만.
- **P4c**: PlayerEntity 저장 후보(Hp/Position/Stats)와 휘발(`_inputQueue`/`_posHistory`/`ActionFsm`/`_jumpBufferRemaining` :28,47,134) 경계 — `PlayerSnapshot` DTO 청사진(저장은 M8). `MapMigration` capturedStats/capturedHp(:107-108) 패턴 재사용.
- (P4d 엔티티 공통 베이스 = **Deferred**, §12)

### P5 (서버+클라, 데이터화)
- **P5a**: EnemyKind 분기 산발(GameMap maxHp 중복 :89-95/:427-433, 사망/리스폰 :439,482,490,631,695, EnemyEntity :33,38,169, EnemyAISystem:22) → catalog/factory 테이블.
- **P5b**: 클라 스킬 3 switch(LocalPlayerInput SkillKeyMap :42-47 + TrySendSkill :141-205, SkillCastHandler :65-91) → 스킬 카탈로그.
- **P5c**: CombatBootstrap(:53-64 10종 wiring) → installer/registry화.

### P6 (이동, 마지막, 최고 위험)
- ADR(P0) 적용: `GameServer/Network/GameSession.cs` → `Sessions/`(NS 정합), `MapMigration.cs` → `Maps/Transitions/`(또는 `Maps/Migration/`), `Maps/Systems/*` NS=폴더, Combat 폴더 정합.
- **frozen 참조 점검 필수**: grep `-DONE`·CODEOWNERS·문서 링크에서 이동 경로 → 깨지면 수정 or 보류.

## 6. Phase별 위험도

- **P0**: 최저 (코드 0). 리스크=ADR 결정이 미숙하면 P6이 흔들림 → plan-auditor + 영호 승인으로 게이트.
- **P1**: 낮음. NS 변경은 컴파일이 즉시 검증. 클라 NS는 Unity 재컴파일 회귀(MCP)로 확인.
- **P2**: 최저. 99_Tools, trust-boundary 무관. 회귀=봇 16/16 유지.
- **P3**: 낮음. 98_Shared 소스 이동은 DLL 출력 불변(Unity는 DLL 참조, 소스 .meta 없음). frozen ref 위험 낮음(소스 경로는 문서에 거의 미참조).
- **P4**: 중. 순수 추출(거동 불변)이라 기존 테스트 + WSL2 회귀가 안전망. trust-boundary 인접(GameMap/GameSession) → **Opus worker**.
- **P5**: 중. 데이터화는 동작 동치 증명 필요(스킬·적 전부 회귀 통과). 클라 P5b/P5c는 Play 육안(bucket-b).
- **P6**: **높음**. 파일 이동 = frozen 참조·Unity serialized ref 위험. → ADR 승인 후만, 최소 범위, 이동마다 grep 검증.

## 7. Phase별 테스트/검증

- **공통 done 판사 (게임 코드 Phase)**: WSL2 회귀 green (build 0/0 + test baseline 비감소[현 663/0/5] + 봇 16/16) + reviewer 🔴0. ADR-029.
- **P0/문서**: dangling 링크 0 + hook smoke + reviewer 🔴0 (게임 회귀 불요).
- **P1**: 서버 `dotnet build` 0err + 클라 MCP 강제 재컴파일 0 CS err + WSL2 회귀 비감소.
- **P2**: 봇 회귀 16/16 (2회 결정론) — ProbeBase 추출 전후 동치.
- **P3**: 서버+클라 빌드 0err + WSL2 회귀 비감소 + Shared.dll diff 의미 동치(공개 API 불변).
- **P4**: **WSL2 회귀 필수 green** (broadcaster/tick 추출이 wire/타이밍 불변 증명). 신규 단위 테스트(snapshot 조립 동치) — **추출 전후 `S_Snapshot.Write()` 산출 byte[] 동치 비교**(plan-auditor 권고②: 바이트 동치가 회귀보다 강한 "wire shape 불변" 계약). reviewer.
- **P5**: 회귀 green + 스킬/적별 봇·xUnit 통과. 클라=Play 육안(bucket-b).
- **P6**: 이동 후 빌드 0err + 회귀 green + **frozen 참조 grep 0 dangling** + 클라 이동 시 Play 육안.

## 8. Rollback 기준

- 각 Phase = 독립 commit(들) → `git revert`로 단일 Phase 롤백 가능. behavior-invariant라 revert 안전.
- **롤백 트리거**: WSL2 회귀 baseline 감소 / reviewer 🔴 / 클라 컴파일 err / frozen 참조 dangling 발생 / 동작 변화 의심(육안).
- **P4 특칙**: 추출 후 회귀가 한 케이스라도 깨지면 즉시 해당 sub-phase revert(추출은 거동 불변이 계약 — 깨지면 추출 오류).
- **P6 특칙**: 이동 PR은 frozen grep 통과 전 머지 금지. 깨진 참조 발견 시 이동 보류(Deferred로 강등).
- 비가역(push/PR/merge)은 Phase 단위가 아니라 마일스톤 끝/중간 게이트에서 **영호 명시 GO**.

## 9. Reader-Oriented Acceptance Criteria (Codex §8)

기존 테스트 통과 외에, *사람이 읽을 수 있음*을 AC로:
- **AC-R1**: 새 개발자가 "Quest progress 저장 위치"를 FEATURE_MAP 기준 **3파일 이내**로 추적 가능.
- **AC-R2**: "새 EnemyKind 추가" 수정 파일이 문서화되고, P5 후 **2~3곳 이하**로 감소.
- **AC-R3**: "Map transition" 서버 파일이 `Network/` 폴더에 남지 않음(P6 후). 단 이동 전이면 FEATURE_MAP에 *임시 위치 + 목표 위치* 명시. **P6가 Deferred로 강등되면(§8 특칙) AC-R3은 FEATURE_MAP 임시표기로 영구 대체**(plan-auditor 권고③).
- **AC-R4**: Party/Quest 흐름의 Entry/Trust gate/Orchestration/State actor/Notification/Client mirror가 FEATURE_MAP에 명시.
- **AC-R5**: M8 persistence candidate와 volatile runtime state가 PlayerEntity에서 구분(P4c).
- **AC-R6**: "새 스킬 추가" 시 만질 클라 파일이 문서화되고 P5b 후 감소(switch 3→데이터 1).

## 10. M8 Persistence Boundary Notes

- **저장 후보(persistence candidate)**: PlayerEntity{Hp, Position, Stats}, Quest progress(killCount/boss unlock latch), Party membership(런타임만? — 영속 여부 M8 결정). 정적 데이터(스탯/스킬/맵)는 DB 밖 유지(98_Shared, flyweight).
- **휘발(절대 저장 금지)**: input queue, prediction/pos history ring buffer, FSM transient, jump buffer, cooldown 잔여 틱.
- **쓰기 경로**: 헌법 §5(틱 내 DB 호출 금지) → M8은 큐드 라이터(Rookiss "Me→You→Me" 핑퐁 + 부분 컬럼 UPDATE 차용, `_diagnosis.md` Part4). P4c가 그 스냅샷 경계를 *미리* 그어 M8이 깨끗이 붙게 함.
- **열린 질문(Codex §6, M8에서 결정)**: ① QuestRegistry가 PartyState.KillCount를 읽고 쓰는 동일 tick-thread invariant가 M8 영속화에서도 안전한가 ② boss unlock/quest progress의 저장 위치 ③ **KillCount가 PartyState에 남아야 하나, Quest-owned state로 이동해야 하나** — P4에서 *경계 표시*만, 이동 결정은 M8.
- **계정 식별(M8 핵심 미결)**: 단일 LocalDB 계정1+캐릭터N(FK), 계정 SSOT 1곳. 레퍼런스 함정(이원화·평문·토큰 버그) 회피. P0 FEATURE_MAP의 Session/Auth 항목이 현 핸드셰이크→캐릭터선택 흐름을 박아 M8 진입점 명확화.

## 11. SOLID/결합도 신호 재분류 (Atlas 136 → Phase 매핑)

> Atlas 신호 = *검토 보조 증거*. 자동 위반 아님. high 3 / medium 133.

| 버킷 | 대표 신호 | 처리 |
|---|---|---|
| **M8 persistence blocker** | GameMap(SRP/coupling40/large683), PlayerEntity(in-coupling23), GameSession(large543/broad), MapMigration | **P4**(+MapMigration 이동 P6) |
| **Reader/onboarding risk** | Network 폴더 오버로드, Combat/Maps 혼재, 클라 핸들러 flat NS(#63 등), Party/Quest 문서 | **P0/P1/P6** |
| **Shotgun/OCP risk** | EnemyKind switch, SkillCastHandler(coupling+static), LocalPlayerInput, LocalPlayerMovement(SRP/coupling17/large433), CombatBootstrap(coupling13/broad) | **P5** |
| **Cycle — validate(대부분 의도)** | 서버 53타입 SCC + 클라 27핸들러 SCC | **목표 아님.** P4가 GameMap 허브 일부 완화. seam(예: GameSession→GameWorld 인터페이스)만 선택. 나머지 §12 보류/수용 |
| **Client composition** | CombatBootstrap, QuestIntroSequencer, QuestCompleteWatcher, AudioManager, Minimap, EffectSpawnService | P5c(CombatBootstrap)만, 나머지 보류 |
| **False-positive / deliberate (무처리)** | GenPackets(생성물), PacketFormat 19-static(codegen 도구), warn-once statics(SkillCast/HitResult/ProjectileLaunch `_warned*`), Stability 高 fan-in(AnimState28/CharacterClass24/Constants21/AudioManager24/SoundKeys25/MainThreadDispatcher24 = 안정 공유 vocab, **좋은 신호**), registry/actor(ActionRegistry/HandlerRegistry), state classes(PartyState/QuestState), dispatch tables, SendBufferHelper static, TickMetrics.Stats, CharacterSelectController↔ClassLoadout 소순환 | **수용/보류** |

**거대 순환 판단(중요)**: 53타입 SCC는 GameMap/GameSession/GameWorld 허브 상호참조 + 엔티티 back-ref(OwningMap)에서 발생. C# 단일 어셈블리에선 정상 범주이며, 전 seam 인터페이스화는 학부생 단독 규모에 과잉 부채. **"순환 제거"를 KPI로 삼지 않는다.** P4의 broadcaster 추출이 가장 굵은 허브(직렬화)를 떼어 SCC를 *부분* 완화하는 정도로 충분. 체계적 디커플링은 M8 이후 SOLID 연속 패스(§12)로.

## 12. Deferred Items (이번 마일스톤 밖)

- **엔티티 공통 베이스(GameObject류)** — Codex 경고(이른 추상화 부채). P4c 스냅샷 경계 후, M8에서 영속화 추상화가 *실제로* 요구하면 재평가.
- **거대 SCC 체계적 디커플링** (인터페이스 seam 다수) — post-M8 SOLID 연속 패스. memory `future-solid-refactor-ultracode` 잔여.
- **UI 접미사 컨벤션 일괄 sweep** (Controller/Panel/UI/Popup/Hud) — Unity serialized ref 위험. CODE_CONVENTION 명문화 후 별도.
- **Maps→World/Simulation 네임스페이스 대재편** (memory `future-maps-namespace-restructure`) — P6보다 큰 작업, post-M8.
- **CharacterClass NS 이동** (Protocol→Enums) — 24 사용처 sweep, append-only 영향. 보류(ADR 사유).
- **PartyState.KillCount 소유권 이동** (Party→Quest) — M8 영속화 결정과 묶임.
- **mutable static 정리**(warn-once 외) / **클라 추가 composition installer**(QuestIntro 등) — 가치 낮음, defer.
- **HitResultHandler→EffectSpawnService 합류** (#6 잔여, 저위험) — P5 또는 후속.

---

## 부록 — 주의 약속 (본 마일스톤)
1. **append-only** — ADR rewrite 0. ADR-033 신설(supersede 아님).
2. **behavior-invariant** — 게임 동작·wire shape 불변. `Protocol.Version` bump 0.
3. **이동은 P6에서만** — P0 ADR 승인 + frozen grep 통과 후. 그 외 Phase는 이동 0(P3 98_Shared 소스 제외, .meta 무관).
4. **Worker 수정만, commit은 메인 세션.** trust-boundary 인접 P4 = Opus worker.
5. **비가역(push/PR/merge) = 영호 명시 GO.** Phase 자동 진행, 외부행위만 게이트.
