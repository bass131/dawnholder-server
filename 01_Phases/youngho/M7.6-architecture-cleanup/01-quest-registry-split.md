---
phase: P01
title: QuestRegistry 분리 — 퀘스트 상태를 Party 도메인에서 독립
milestone: M7.6
owner: youngho
grade: 복잡
risk: trust-boundary (보스 해금 = 서버 권위 퀘스트 상태)
depends_on: []
blocks: [P02, P03]
status: in_progress
---

# P01 — QuestRegistry 분리 (preM8★)

> 근거: 감사 #1 (`../../../00_Document/reviews/2026-06-19-architecture-logic-audit.html`).
> 퀘스트(보스 해금) 진행 상태가 **Party 도메인에 굳어 있음** → M8 영속화 시 퀘스트가 파티 테이블로 새는 오염.
> 본 Phase가 깨끗한 퀘스트 경계를 확보(M8 진입 전 토대).

## 🎯 목표

`PartyRegistry`에 섞여 있는 **퀘스트 진행 상태·로직**을 새 `02_Server/GameServer/Quest/QuestRegistry.cs` (actor)로 분리한다. 게임 *동작은 불변* — 순수 구조 이동(외부 행동 0 변경, 와이어 포맷 0 변경).

## 📏 현황 실측 (2026-06-19, file:line)

퀘스트 상태가 **두 군데**로 흩어져 있음:
- 솔로 진행: `PartyRegistry._soloProgress` (PartyRegistry.cs:43)
- 파티 공유 진행: `PartyState.KillCount` (PartyState.cs:22) — 주석이 직접 "퀘스트 Q2에서 증가"라 명시
- 보스 영구 해금 latch: `PartyRegistry._bossUnlocked` (PartyRegistry.cs:48)

분리 대상 심볼 (`PartyRegistry`):
| 심볼 | 줄 | 종류 |
|---|---|---|
| `_soloProgress` | 43 | 필드 (per-entity 퀘스트) |
| `_bossUnlocked` | 48 | 필드 (per-entity latch) |
| `OnKill(int, GameWorld)` | 215 | 킬 적립 |
| `DebugCompleteQuest(int, GameWorld)` | 248 | 치트 즉시완료 ★P02 대상 |
| `ResetAllQuestProgress()` | 274 | 보스 킬 시 리셋 |
| `GetKillCount(int)` | 288 | 게이트 권위 조회 |
| `GetSoloProgress(int)` | 299 | 테스트 관측 |
| `IsBossUnlocked(int)` | 303 | 테스트 관측 |

호출처 (전수):
- `GameWorld.MakeMap` onKill 콜백 (GameWorld.cs:164–171): `_party.ResetAllQuestProgress()` / `_party.OnKill(killerId, this)`
- `GameSession.SubmitCheatCommand` (GameSession.cs:491–495): `world.Party.EnqueueJob(() => world.Party.DebugCompleteQuest(entityId, world))`
- `GameSession` getKillCount delegate (GameSession.cs:369, 644–645): `GameWorld.Instance?.Party.GetKillCount(entityId)`
- 테스트: `QuestKillCountTests`, `BossGateSmokeTests`, `MapTransitionIntegrationTests`, `PartyQuestSmokeTests`, `PartyRegistry` 직접 호출하는 `_world.Party.OnKill/...`

## 🧭 설계 결정 (depth-B + actor)

### 왜 depth-B (파티 공유 KillCount는 PartyState 잔류)

- **영속 대상 = per-entity 퀘스트 상태** (`_soloProgress` + `_bossUnlocked` latch). 파티는 로그아웃 시 소멸(MVP 비영속)이라 `PartyState.KillCount`는 *런타임 일시 집계*. M8이 필요로 하는 건 per-entity 상태 → QuestRegistry가 그것만 소유하면 깨끗한 영속 경계 확보.
- **depth-A(KillCount까지 QuestRegistry로 이동) 기각**: partyId 키로 옮기면 disband 시 QuestRegistry가 파티 소멸을 *관찰*해 청소해야 함 → 새 생명주기 결합 + 동작 불변 위험 ↑. M8에 불필요(파티 카운트 비영속). **미래 과제로 박제** (파티 진행도 영속이 필요해지면 그때).
- **★ depth-B의 명시적 영속 귀결** (plan-auditor 권고 1): 임계(20) *미달* 상태에서 파티로만 그라인드한 플레이어는 per-entity 영속 상태가 **완전히 0**(`PartyRegistry.cs:222` — latch는 임계 도달 시에만 add, 그 전엔 `party.KillCount`만 누적=비영속). 즉 **해금 *완료자*는 latch로 복원 충분, 해금 *진행 중* 파티원의 진행분은 M8 재로그인 시 0부터**. MVP 허용(파티=세션 한정 비영속). 진행분 영속이 필요해지면 depth-A 미래 과제.

### QuestRegistry 형태 (actor 패턴, PartyRegistry mirror)

- 자체 `ConcurrentQueue<Action> _pendingJobs` + `EnqueueJob` + `Tick(long)` 보유 (PartyRegistry와 동일 actor 골격).
- 소유 상태: `_soloProgress`, `_bossUnlocked`.
- **PartyRegistry 단방향 의존**: 생성자 주입(`QuestRegistry(PartyRegistry party)`). 파티 멤버십/공유 카운트가 필요한 지점(`GetPartyByEntity`, `PartyState.KillCount` 읽기/쓰기)만 호출. **PartyRegistry는 QuestRegistry를 모름**(역방향 의존 0 — 사이클 금지).
- **스레드 안전 불변식**: `Quest.Tick()`과 `Party.Tick()`은 **같은 GameWorld 틱 스레드**에서 순차 실행(`GameWorld.OnTick`). QuestRegistry가 PartyState를 **읽고/쓰는**(★`party.KillCount++`·`= target`·`= 0` 변경 포함 — plan-auditor 권고 3) 건 이 동일-스레드 보장 하에서만 안전 — 주석으로 박는다. *Quest가 Party 소유 데이터를 변경*하는 교차-actor 쓰기라, 미래 맵 멀티스레드화 시 재검토 1순위.

### GameWorld 배선

- `GameWorld`에 `_quest`/`Quest` 추가 (`_party`/`Party` 패턴 mirror). 생성 순서: `_party` 먼저 → `_quest = new QuestRegistry(_party)`.
- `OnTick`: `Party.Tick(tickNumber)` **다음에** `Quest.Tick(tickNumber)` 호출 (퀘스트가 파티 상태를 읽으므로 파티 드레인 후).
- `MakeMap` onKill 콜백: 마샬링 큐를 **Quest 큐로 이동** — `_quest.EnqueueJob(() => { if Boss → _quest.ResetAllQuestProgress(); else → _quest.OnKill(killerId, this); })`.

### 호출처 갱신

- `GameSession.SubmitCheatCommand`: `world.Quest.EnqueueJob(() => world.Quest.DebugCompleteQuest(entityId, world))`.
- `GameSession` getKillCount delegate: `GameWorld.Instance?.Quest.GetKillCount(entityId)`.
- 테스트: `_world.Party.OnKill/GetSoloProgress/IsBossUnlocked/GetKillCount/ResetAllQuestProgress` → `_world.Quest.*`. **단, 파티 공유 카운트 직접 검증**(`GetPartyByEntity(x)!.KillCount`)은 PartyState에 남으므로 *그대로 유지*.

## 🔗 P01 → P02 핸드오프 (hard 의존)

- 치트 즉시완료의 **새 위치 = `QuestRegistry.DebugCompleteQuest`** (호출: `GameSession.SubmitCheatCommand` → `world.Quest.EnqueueJob`).
- 등록 경로는 불변: `HandlerRegistry.C_CheatCommand → CheatCommandHandler → session.SubmitCheatCommand`. **P02는 이 등록(HandlerRegistry.cs:28)을 `#if DEBUG`로 감싼다** — DebugCompleteQuest 자체는 건드리지 않음.
- 본 Phase done 보고에 "DebugCompleteQuest 신규 위치 = QuestRegistry" **명시 박제** (P02가 참조).

## ✅ 완료 조건 (done 판사 = 외부 기계 게이트, ADR-029)

- [ ] 빌드 0 error / 0 warning (server + 98_Shared).
- [ ] WSL2 회귀 green — **테스트 수 657 비감소** (동작 불변 증명).
- [ ] 봇 시나리오 통과 (퀘스트 그라인드 → 보스 해금 경로 회귀 0).
- [ ] `reviewer` 🔴 0.
- [ ] **`Protocol.Version` 불변** (와이어 포맷 0 변경 — 순수 서버 내부 구조).
- [ ] `git diff` = 순수 이동 (퀘스트 *로직* 변경 0 — 동치 보존).
- [ ] **★ trust-boundary 동치 = 명시적 테스트 연결** (plan-auditor 권고 2): `QuestKillCountTests.BossUnlock_Persists_AfterReset_NoRegrind`(latch 동치) + `BossGateSmokeTests`/`MapTransitionIntegrationTests`(게이트 권위 경로)가 이동 후 PASS 유지 — **테스트 본문 0 수정, 호출 대상만 `Party.*`→`Quest.*`**. (git diff 순수이동만으론 "값의 *의미* 보존"을 증명 못 함 → 테스트 PASS로 동치 박제.)
- [ ] dangling 참조 0 (`02_Server/CLAUDE.md` Layout 표에 `Quest/QuestRegistry.cs` 반영 = 동일 commit) + **이동 후 PartyRegistry 미사용 using/심볼 0** (★특히 line 3 `using ...Quest;` — 전부 이동 시 죽을 수 있음, plan-auditor 권고 4).
- [ ] P01→P02 핸드오프 박제 (DebugCompleteQuest 신규 위치).

## ⚠️ 함정

- **trust-boundary 약화 0**: 보스 해금은 서버 권위. 검증 로직(임계 비교, latch, getKillCount delegate)을 *동치 그대로* 옮길 것 — 게이트가 읽는 값의 의미가 바뀌면 안 됨.
- **latch 의미 보존**: `_bossUnlocked`는 `ResetAllQuestProgress`에서 의도적으로 안 비움(영구 해금, 재그라인드 방지). 이 의미 보존 필수.
- **솔로 vs 파티 분기 보존**: `OnKill`/`DebugCompleteQuest`/`GetKillCount`의 "파티면 공유, 솔로면 _soloProgress" 분기를 동치 유지.
- **actor 경계**: QuestRegistry 메서드 직접 호출 금지 — `EnqueueJob` 마샬링(헌법 §5). 테스트는 `EnqueueJob` 후 `Tick()` 드레인 패턴.
- **동일-스레드 의존 명시**: QuestRegistry가 PartyState를 읽는 안전성은 Quest.Tick/Party.Tick 동일 스레드 보장에 의존 — 주석 박제(미래 맵 멀티스레드화 시 재검토 포인트).
