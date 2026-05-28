---
owner: youngho
milestone: M4.2
phase: milestone-closeout
title: Map Transition — 마일스톤 마감
status: done
grade: 복잡
summary: M3 응급 단일 맵 3-zone trick을 진짜 4맵 분리로 승격. GameWorld 다맵 레지스트리 + portal handoff(서버 권위 player migration) + 클라 scene 전환까지 풀세트. ADR-026(entity id 전역 풀) + ADR-027(client bootstrap persistent services) 박제. ProtocolVersion 5→6 bump. reviewer Tier 2-A 통과(🔴0/🟡3) + Codex β cross-review γ 9회차(β 1차 4건 + 봉합 + β 2차 4건 + 봉합 + 본인 실측 검증). 캡스톤 1 발표 데모 준비 완료(single + multi player 시각 확인 ✅). 다음 = M4.3 AI + Polish + cheat-flag/Serilog 이월분.
---

# M4.2 — Map Transition (마일스톤 마감)

**마감 일자**: 2026-05-28
**Phase 수**: 5 (확정 분해 — Phase 01 골격 / 02 portal+packet / 03 migration / 04 client dispatch / 05 통합+마감)
**등급**: 복잡 (마일스톤 마감 의례 + PDL bump irreversible 깃발)

---

## TL;DR

M3에서 응급 데모용으로 박았던 "한 GameMap 안의 3-zone 좌표 분할" 트릭을 진짜 4맵 분리로 승격했다. `GameWorld._map` 단일 참조가 `Dictionary<MapId, GameMap>` 레지스트리가 되고, 맵 사이는 portal handoff로 잇는다. **핵심 설계 결정 두 개**: (1) **ADR-026 — entity id 전역 풀**: 맵 이동 시 entity id가 유지된다. `S_MapTransition` 패킷에 entityId 필드를 *박지 않은* 게 정합 — 박는 순간 약속이 중복되고 클라가 둘이 다를 때 누가 옳은지 모호해진다. (2) **ADR-027 — Client Bootstrap Persistent Services**: 옛 `NetworkBootstrap`의 DontDestroyOnLoad + 중복 가드 + sceneLoaded 자동 teardown이라는 *암묵적 lifecycle*을 코드 주도 부트스트래퍼 + 명시 Connect()/Disconnect() API로 승격. Phase 03 migration 로직은 race window 4종(transient drop / disconnect during migration / GetDestMap null / broadcast skip)을 결정론적 단위 테스트로 봉합했고, Phase 04 클라 dispatch는 Play 검증에서 발견한 *위치가 아니라 생명주기* 결함을 봉합(옛 LocalPlayer가 페이드 동안 살아 reconcile snap → enabled=false로 입력 자격 창 강제). Phase 05에서 헤드리스 봇 portal 이동 흐름 + 통합 테스트 + 옛 Skip smoke 2건 복구로 회귀 안전망 완성.

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 등급 | 마감 |
|---|---|---|---|---|
| 01 | 맵 레지스트리 + MapId enum 골격 | `Dictionary<MapId, GameMap>` 다맵 tick + `MapSpawnTable` 단일 진실 + smoke 2건 Phase 05 Skip 박제 | 보통 | ✅ `dad760b` |
| 02 | portal 정의 + S_MapTransition 패킷 + PDL bump | `PortalTable` static + PDL `C_EnterPortal`/`S_MapTransition` append-only + ProtocolVersion 5→6 + Shared.dll 동반 commit | 복잡 | ✅ `8363425` |
| 03 | 맵 간 player migration 로직 | `SubmitEnterPortal` 4단 trust-boundary + entity id 유지(ADR-026) + race window 4종 결정론 봉합 + 케이스 스터디 HTML 박제 | 복잡 | ✅ `41f224d` |
| 04 | 클라 4 scene dispatch + portal UX | ADR-027 Bootstrap + 동적 spawn + Play 검증 봉합 3건(카메라 미추적 / spawn 튐 d=2 / 첫 snapshot race) | 복잡 | ✅ `a59cb1b` + `cc532d5` (-DONE.md `8ccedd8`) |
| 05 | 통합 검증 + 봇 맵 이동 시나리오 + 마감 | 봇 portal 흐름 추가(CombatProbe/BossProbe) + 새 `MapTransitionScenario` 4맵 루프 + 통합 테스트 + Skip smoke 2건 복구 + ARCHITECTURE.md 갱신 | 보통 | ✅ 본 commit |

**머지 이력**: M4.2 전체가 단일 PR(예정) — Phase 01~05 풀세트 한 묶음. 사용자 GO 게이트 의무.

---

## AC 검증 결과

마일스톤 plan `_milestone-plan.md` 완료 조건 대조:

| 마일스톤 AC | 결과 | 검증 |
|---|---|---|
| `MapId` enum 4맵 정의 (Town / HuntingGround / BossRoom / Ending) | ✅ | `02_Server/GameServer/Maps/MapId.cs` stable id 0/1/2/3 |
| `GameWorld`가 `Dictionary<MapId, GameMap>` 레지스트리로 다맵 tick | ✅ | `Loop/GameWorld.cs` + `GameWorldRegistryTests` 6건 PASS |
| portal entity + 근접 검증 (헌법 #3) | ✅ | `Maps/PortalTable.cs` + `SubmitEnterPortal` 근접 ≤ 2 unit 게이트 + `PortalTableTests` 16건 PASS |
| `C_EnterPortal` + `S_MapTransition` PDL + ProtocolVersion 5→6 | ✅ | `PDL.xml` append-only IDs 17/18 + `Generated/GenPackets.cs` + Shared.dll 동반 commit + `ProtocolVersion.Current = 6` |
| 맵 간 player state 이전 (HP / PlayerStats / 위치) — 왕복 보존 | ✅ | `MapMigrationTests.RoundTrip_*` + `EntityId_Preserved` 13건 PASS |
| 클라 4 scene 전환 + portal 트리거 입력 | ✅ | Unity Play 모드 4맵 전환 + reviewer Tier 2-A Phase 04 통과 |
| `dotnet test` green (회귀 0 + Phase별 신규) | ✅ | 300통과 / 0실패 / 4Skip (M4.1 baseline 221 → +79) |
| headless-bot 맵 이동 왕복 시나리오 PASS | ✅ | `MapTransitionScenario` 4맵 루프 + `MapTransitionIntegrationTests.MapTransition_FullLoop_Succeeds` PASS |
| M4.2-마감 `_milestone-DONE.md` (복잡 등급) | ✅ | 본 파일 |
| CHANGELOG entry ([M] — PDL bump + 모든 팀원 빌드 영향) | ✅ | 본 commit 동반 |
| 캡스톤 1 발표 데모 영상 가능 상태 (M4.1 + M4.2 종합) | ✅ | Unity Play 4맵 전환 + 정밀 전투(M4.1) 풀세트 |

**빌드**: `dotnet build Dawnholder.slnx` 오류 0 / 경고 0
**테스트**: 300통과 / 0실패 / 4Skip (Skip 4건 모두 LongRunning 사유 명시, 동등 deterministic 단위 테스트로 대체 경로 박힘)

---

## 결정 흐름

1. **scope = 데모 핵심 우선** (2026-05-25 scope 결정) — 4맵 분리 + portal handoff + 클라 scene 전환까지. cheat-flag table + Serilog 도입은 M4.3로 이월(캡스톤 발표 데모 화면에 안 보이는 인프라 + 1주 일정 안전 마진).
2. **ADR-026 entity id 전역 풀** — 옛 패턴은 맵별 entity id 재발급이지만, *맵 이동 시 id 유지*가 클라 RemoteEntityRegistry 인덱싱/네트워크 추적 비용 모두 단순화. `S_MapTransition`에 entityId 필드 *없음* — 패킷에 박는 순간 ADR-026의 약속이 중복되어 클라가 둘 다 다를 때 모호해진다. wire-level 검증 불가 trade-off는 봇 *간접* 검증(최초 LocalEntityId 보존) + `MapMigrationTests.EntityId_Preserved`로 흡수.
3. **ADR-027 Client Bootstrap Persistent Services** — 옛 `NetworkBootstrap`의 DontDestroyOnLoad + 중복 가드 + OnSceneLoaded 자동 teardown이라는 *암묵적 lifecycle*을 코드 주도 부트스트래퍼 + 명시 `Connect()/Disconnect()` API로 승격. `PersistentServicesBootstrap`가 `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`로 1회 spawn. 맵 전환 시 네트워크 세션은 *그대로 유지*.
4. **Phase 03 동시성 설계 결정 과정 박제** — `_migrating` Volatile 플래그를 lock vs Interlocked vs Volatile 3 옵션 비교 + race window 4종(transient drop / disconnect during migration / GetDestMap null / broadcast skip) 발견 과정을 `00_Document/case-studies/2026-05-25-ai-orchestrator-concurrency.html`에 케이스 스터디로 박음 (commit `8c11e91`). AI 조율자 동시성 설계 과정 자산화 = 캡스톤 평가 자산.
5. **Phase 04 위치가 아니라 생명주기 결함 재정의** — Play 검증에서 발견한 맵 전환 직후 spawn 튐(d=2)을 처음엔 "predictor 위치 리셋" 문제로 봤지만, 실제 원인은 "곧 파괴될 옛 LocalPlayer가 페이드 동안 살아 snapshot 계속 받음". 해법을 위치 리셋 → `Instance=null + enabled=false`로 전환 + HandleSnapshot의 `Instance != null` 가드가 "입력 자격 창" 단일 지점에서 강제. 이런 *현상 vs 본질* 재정의가 학부생→실무 도약 패턴.
6. **Phase 05 ARCHITECTURE 갱신 동반** (reviewer Tier 2-A 🟡 응답) — 옛 M4.1 마감 직후 발견된 *역방향* false-promise(코드는 됐는데 문서 stale) 재발 패턴을 본 마감 commit에 ARCHITECTURE.md `## M4.2 결과 종합` 절 신설 + L212 `ProtocolVersion 5→6` 본문 정정으로 동시 봉합. ADR-024 cadence 정합 — *마감 직후 발견*되지 않도록 *마감 시점 봉합*.

---

## 학습 일지 후보 키워드

1. **`static-analysis-vs-empirical-truth-gap` (★★★)** — 본 마감 시점 *최대 학습 자산*. 정적 분석(α reviewer + β Codex)은 *논리적 정합* 추론을 박지만 *실측 동작 truth*와 격차 가능. β P1 = TCP 순서 + main thread queue + 페이드 비동기 = *논리적*으로 페이드 중 roster 옛 씬 적용. 실측: enemy/RemotePlayer 잘 보임. 사용자가 "서버 구동 안 하고 테스트한 거 아니야?" 한 마디로 γ 함정 짚음 → 실측 검증 진행 → β P1 false positive 가능성 ↑↑ 확인. β P2는 multi player 실측에서 *진짜 결함 실증*. 즉 β의 가치는 *불완전 시나리오*에서도 결함을 추론할 수 있다는 것 + 한계는 실측 truth와 격차 가능. 한국 게임 회사 실무 정합 — 면접에서 "외부 cross-review 결과를 어떻게 검증하셨나요?" 답변 결정타 키워드.

2. **`entity-id-global-pool-not-in-packet` (★★★)** — ADR-026의 핵심 trade-off. 맵 이동 시 entity id를 *유지*하기로 결정한 순간, `S_MapTransition` 패킷에 entityId 필드를 *박지 않는* 게 정합이 된다. 박으면 약속이 중복되고 클라가 둘이 다를 때 누가 옳은지 모호해진다. 한국 게임 회사가 흔히 쓰는 패턴 — 면접에서 "맵 전환 시 entity id를 어떻게 관리하세요?" 답변 어필 키워드.
2. **`map-migration-race-deterministic-coverage` (★★★)** — 단일 race가 아니라 *4 차원 race window*가 있다 (transient drop / disconnect during migration / GetDestMap null / broadcast skip). 각 차원을 결정론적 단위 테스트로 봉합 = "한 차원 race 봉합은 다른 차원 race 안전 보장 X"(M3.8 통찰)의 정통 적용. Phase 03 케이스 스터디 HTML로 박제.
3. **`lifecycle-not-position` (★★★)** — Phase 04 spawn 튐 봉합. 표면(위치 튐)을 보고 위치 리셋으로 해결하려다 실패, 본질(생명주기 — 곧 파괴될 객체가 메시지 받음)로 재정의 후 `enabled=false`로 "수신 자격 창" 단일 지점 강제. 학부생→실무 도약 패턴: *현상 vs 본질* 재정의 능력.
4. **`client-bootstrap-explicit-lifecycle` (★★)** — ADR-027. DontDestroyOnLoad + 중복 가드 + 자동 teardown이라는 Unity의 *암묵적* 패턴 누적을 *명시* 부트스트래퍼 + 명시 Connect/Disconnect API로 승격. 응급 단계 패턴(M3)에서 본 마감 패턴(M4)으로 자연 진화.
5. **`reverse-false-promise-architecture-stale` (★★)** — 코드는 박혔는데 문서가 stale인 *역방향* false-promise. M4.1 마감 직후 발견된 이력 + M4.2 reviewer Tier 2-A 통합 점검에서 동일 패턴 재발 — 마감 시점 ARCHITECTURE 갱신 동반이 표준 절차. ADR-024 cadence가 *예방적*으로 작동한 사례.

6. **`cross-review-followup-finding-pattern` (★★)** — β 1차 봉합 → β 2차 → 추가 4건 발견. 봉합 자체가 *새 결함 차원* 도입(P1-B lifecycle deltas + P1-C unsubscribe). M3.7 옵션 C 게이트 5번째 실측 패턴과 정합 = "봉합 후 재실측 의무" cadence. M4.3 자동 재실측 ADR 후보.

7. **`user-as-final-truth-arbiter` (★★)** — 사용자 "서버 구동 안 하고 테스트한 거 아니야?" 한 마디로 γ 함정 짚음. α/β 두 정적 시각 모두 *체크리스트 시야* 한계. 본인 *실측 동작* 시각이 final arbiter. 학부생 → 실무 도약 핵심 패턴.

---

## false-promise 점검 결과 (ADR-024 cadence — 마일스톤 마감 의무 섹션)

M4.2 누적 false-promise 발본 **1건** (마일스톤 마감 시점 집계):

- **ARCHITECTURE.md L212 ProtocolVersion stale + M4.2 결과 종합 절 누락** (reviewer Tier 2-A 🟡 1·2) — 코드는 v6로 박혔는데 문서는 v5 그대로 + M3 결과 종합 절은 박혀있지만 M4.2 결과 절 없음. *역방향* false-promise (M4.1 동일 패턴 재발 후보). 본 마감 commit에 ARCHITECTURE.md `## M4.2 결과 종합` 절 신설 + L212 본문 정정으로 동시 봉합.

**판정**: M4.2는 *forward* false-promise(약속만 박고 미구현) **0건**, *역방향* 1건 — reviewer Tier 2-A 통합 점검이 *마감 직후 발견*되지 않도록 *마감 시점 봉합*을 강제한 게 누적 차단 효과. ad-hoc +5건 트리거 미도달.

M4.2 plan에 명시한 forward 약속 풀세트는 모두 실현:
- "smoke 2건 복구"(Phase 01 Skip) → Phase 05 복구 + 주석 갱신 ✅
- "맵 이동 통합 테스트" → `MapTransitionIntegrationTests` 신설 ✅
- "헤드리스 봇 맵 이동 시나리오" → `MapTransitionScenario` + smoke 봇 흐름 추가 ✅
- "`_milestone-DONE.md` 박제" → 본 파일 ✅
- "CHANGELOG [M] entry" → 본 commit ✅

**M4.3 이월 명시** (옛 약속 정합):
- cheat-flag table (헌법 #3 강화) — M4.2 scope 결정에서 명시 이월
- Serilog 도입 — 동일
- 봇 portal 좌표 const 공유 헬퍼 — reviewer 🟡 3 (3 파일 drift 위험)
- reconcile drift 튜닝 — 평상시 이동 중 d≈1.5 snap (가변 dt vs 고정 tick)
- 맵 간 enemy respawn 정책 — 현재 맵 인스턴스 평생 1회 spawn (Phase 03 결정)
- `GameSession/GameMap` enemy spawn 종속성 분리 — 사용자 지적 (응집/종속성 개선)
- `PendingSpawn` EditMode 테스트 — Phase 04 reviewer 🟡
- **β P1 buffer 패턴 재실측 + 봉합 롤백 검토** — 본 γ 9회차에서 false positive 가능성 ↑ 확인. M4.3에서 정확한 timing 시나리오(페이드 정확히 그 시점에 패킷 도착) multi player 실측 후 false positive 확정 시 UnityClientSession buffer 코드 + P1-B/P1-C 봉합 함께 롤백. *방어적 안전망*으로 일단 유지(무해, ~50줄).
- **RemotePlayer Animator 미작동** — 본 γ 검증 시 본인 추가 발견. 외관 backlog, 본인 분담.
- **3 RemotePlayer prefab 동시 박힘 정합 검토** — `RemotePlayer.prefab` / `RemotePlayer.backup.prefab` / 새 `Resources/RemotePlayer.prefab` 동시 존재. cleanup 의무 (어느 게 진짜 사용 중인지 본인 확인).
- **cross-review 자동 재실측 cadence ADR** — β 1차 봉합 → β 2차 추가 결함 패턴 정합화 (Rule of Three 통과 시).

---

## reviewer Tier 2-A 통합 점검 결과 (마감 의무 — plan-auditor 2026-05-25 🔴 봉합)

- 🔴 0건 — 5축 점검 통과 (헌법 §1~§5 / ADR-002·010·012·026·027 / ARCHITECTURE 구조 / 테스트 커버리지 / 도메인 패턴 모두 정합)
- 🟡 3건 — 본 마감 commit에 2건 동반 봉합, 1건 M4.3 backlog
  - 🟡 1: ARCHITECTURE.md L212 ProtocolVersion stale → 본 commit 봉합
  - 🟡 2: ARCHITECTURE.md M4.2 결과 종합 절 누락 → 본 commit 봉합
  - 🟡 3: 봇 portal 좌표 const 3 파일 중복 → M4.3 backlog (drift 위험은 동기화 의무 주석으로 흡수)
- 🟢 5축 PASS — 헌법 §1 portal 목적지/spawn 100% 서버 권위 / §2 PDL append-only + 산출물 정렬 / §3 4단 검증 silent drop / §4 양쪽 빌드 정합 / §5 EnqueueJob 동기 코드 0 await

머지 게이트: **조건부 GO → 본 commit으로 GO 격상** (🟡 1·2 동반 봉합 완료).

## Codex β cross-review γ 9회차 결과 (본 마감 직전 추가 진행)

상세 산출물: `00_Document/reviews/2026-05-28-cross-review-m4.2-map-transition.md`

**γ 진행 흐름**:
1. **β 1차** (`codex review --base main`) → 4건 발견 (P1 buffer / P2 RemoteEntityRegistry / P3 _spawned guard / P4 race)
2. **봉합 1차** (client SubAgent P1+P2+P3 / qa SubAgent P4 commit `c8b7235`)
3. **β 2차** (`codex review --uncommitted`) → 추가 4건 (P1-B lifecycle deltas / P1-C unsubscribe / P2-A prefab 위치 / P3-A .gitignore)
4. **사용자 통찰** "서버 구동 안 하고 테스트한 거 아니야?" → 실측 검증 의무 진입
5. **실측 1차 single player** — Town→HG→BossRoom→Ending + C_Attack 17건 → enemy/boss 잘 보임 ✅
6. **본인 prefab Resources 이동** (P2-A 봉합) → multi player 재검증 → RemotePlayer 잘 보임 ✅
7. **β P1 false positive 가능성 ↑↑ 확인** — 실측에서 페이드 중 roster 옛 씬 적용 시나리오 발현 X

**판정**:
- β P1 (+ P1-B + P1-C) — **봉합 유지 (방어적 안전망)** + M4.3 재실측 backlog (false positive 확정 시 롤백)
- β P2 (+ P2-A) — **진짜 결함 실증, 봉합 유지** + prefab Resources 이동 완료
- β P3 — 미검증(domain reload off) 봉합 무해 유지
- β P4 — 진짜 결함, `c8b7235` 박힘
- β P3-A — `.gitignore` `.backups/` 추가 본 commit 봉합

**머지 게이트**: GO (실측 검증 통과 + 본인 final truth arbiter 통찰).

---

## ➡️ 다음

- **M4.2 완전 마감** = 본 commit + PR 머지 시점 (사용자 GO 게이트 의무 — 헌법 + ADR-022)
- **M4.3 — AI + Polish** 진입 (캡스톤 1 발표 후 7~10월). enemy AI + boss behavior + jump Y mispredict 봉합 + **cheat-flag + Serilog 이월분** + PvP ADR + 봇 portal 좌표 공유 헬퍼 + reconcile drift 튜닝 + `GameSession/GameMap` spawn 종속성 분리 + 맵 간 enemy respawn 정책 ADR
- **별 시점 backlog** (work-pin 박제 이월):
  - `PendingSpawn` EditMode 테스트 — Phase 04 reviewer 🟡 backlog
  - target도 rewind (M4.3) / capsule hitbox (M4.3) — M4.1 이월
  - `04_ClientNet.Tests` — M4.1 이월
  - 봇 lag 종단간 실측 (timing harness) — M4.1 이월
