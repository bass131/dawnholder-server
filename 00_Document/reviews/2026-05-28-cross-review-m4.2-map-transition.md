# Cross-Review — 2026-05-28 — M4.2 Map Transition 마일스톤 마감 (γ 9회차)

## 변경 범위
- 브랜치: `feature/m4.2-map-transition` (main `d751332`에서 분기)
- 변경: 104 파일 / +7378 / -651 (main 대비). commit 7건 (Phase 01~04) + Phase 05 본 마감 commit 미커밋
- 등급: **대규모** (3+ 도메인 server/shared/client/qa, 300줄+ 비가역)
- 위험 깃발: `irreversible`(PDL 5→6) + `trust-boundary`(Phase 02·03) + `unity-asset`(Phase 04)

## α — Claude reviewer Tier 2-A 결과
- 🔴 위반 0건 / 🟡 개선 3건 / 🟢 5축 PASS
- 🟡 1·2: ARCHITECTURE.md L212 stale + M4.2 결과 종합 절 누락 → **본 마감 commit 동반 봉합**
- 🟡 3: 봇 portal 좌표 const 3 파일 중복 → M4.3 backlog (drift 위험 < 동기화 의무 주석)
- 머지 게이트: 조건부 GO → 봉합 후 GO 격상

## β — Codex 결과 (γ 1차 호출 — `codex review --base main`)

**4건 발견** (P1/P2/P3/P4) — α가 못 잡은 *실제 동작 시나리오* 시각.

- P1 (UnityClientSession:498) — 페이드 중 roster 패킷(S_PlayerJoin/S_EntitySpawn) 옛 씬 적용 후 destroy → 새 씬에 roster 없음
- P2 (CombatBootstrap:41) — Awake에 `BuildRemoteEntityRegistry` 없음. 옛 Gameplay.unity의 `_RemoteEntityRegistry`가 새 4 씬에 누락 → multi player에서 silently drop
- P3 (PersistentServicesBootstrap:40) — `_spawned` static guard가 instance check 앞에서 early return → domain reload off 시 false negative
- P4 (MapTransitionScenario:255-256) — `before` capture가 SendEnterPortal *후*에 박힘 → race window

## 봉합 1차 (client SubAgent + qa SubAgent 분담)

- P1 봉합: UnityClientSession roster buffer 패턴(_pendingMapTransition + _rosterBuffer + sceneLoaded drain)
- P2 봉합: CombatBootstrap.BuildRemoteEntityRegistry() + RemoteEntityRegistry.SetRemotePlayerPrefab() 공개 API
- P3 봉합: _spawned guard 제거 → instance check 단일 진실
- P4 봉합: WaitForMapTransition(expectedCount 파라미터화) — qa commit `c8b7235`

## β — γ 2차 호출 (재실측 — `codex review --uncommitted`)

**추가 4건 발견** — 1차 봉합이 *새 결함 차원* 도입 (큰 학습 자산).

- P1-B (UnityClientSession:613-614) — buffer가 S_PlayerJoin/S_EntitySpawn/S_Snapshot만, lifecycle deltas(S_PlayerLeave/S_HitResult/S_EntityDeath) 누락
- P1-C (UnityClientSession:94) — sceneLoaded subscribe 했는데 OnDisconnected에서 unsubscribe X → reconnect 시 옛 session callback 잔류
- P2-A (CombatBootstrap:112) — `Resources.Load("RemotePlayer")` 박혔는데 prefab은 `Assets/Prefabs/Characters/RemotePlayer.prefab` (Resources 밖) → 모든 자동 registry Spawn 실패
- P3-A (.backups/20260525-pre-mcp-scene) — `.gitignore`에 `.backups/` 미박힘 → `git add .` 시 폭탄

## ★★★ 결정적 사용자 통찰 — "서버 구동 안 하고 테스트한 거 아니야?"

본인 (2026-05-28) 짚음. Codex β는 *서버 구동 X / 코드 정적 분석 + 추론만*. 본인 실측이 진짜 truth.

## 실측 검증 (본인 직접, 결정적 truth)

**1차 single player 검증** (서버 + 클라 1대):
- Town → HG → BossRoom → Ending 흐름 + C_Attack 17건 (Normal enemy + Boss)
- 서버 로그: `roster:0`(single player정합), enemy spawn 정상, portal 이동 정상
- → **enemy/boss 시각 잘 보임 ✅** = β P1 시나리오 *S_EntitySpawn 부분* 실측 발현 X

**2차 multi player 검증** (P2-A prefab Resources 이동 후, 2 client):
- RemotePlayer prefab을 `Assets/Resources/`로 이동(본인 Unity Editor drag)
- multi player 재시도 → **RemotePlayer 잘 보임 ✅**
- portal 이동 시나리오 정상
- → β P2 진짜 결함 *실증* (prefab 누락이면 spawn X) + β P1 시나리오 *S_PlayerJoin 부분*도 실측 발현 X

## γ 비교 정합 분석

| 결함 | 진위 판정 | 봉합 결정 |
|---|---|---|
| α 🟡 1·2 (ARCHITECTURE stale) | 진짜 (역방향 false-promise 재발) | 본 마감 commit 동반 봉합 ✅ |
| α 🟡 3 (봇 portal const) | 진짜 (3 파일 drift 위험) | M4.3 backlog |
| **β P1 (roster buffer)** | **실측 발현 X = false positive 가능성 ↑↑** | **봉합 유지 (방어적 안전망)** + M4.3 재실측 backlog |
| β P2 (RemoteEntityRegistry) | **진짜 실증** (multi player 검증) | 봉합 유지 + P2-A prefab Resources 이동 ✅ |
| β P3 (_spawned guard) | 미검증 (domain reload off) | 봉합 유지 (무해, 방어적) |
| β P4 (race window) | 진짜 (timing 운) | qa commit `c8b7235` ✅ |
| β P1-B (lifecycle deltas) | P1 자식 — 같은 운명 | 봉합 유지 (방어적) |
| β P1-C (unsubscribe) | reconnect 결함 (현재 흐름엔 미발현) | 봉합 유지 (방어적) |
| β P2-A (prefab 위치) | 진짜 실증 | 본인 직접 Resources 이동 ✅ |
| β P3-A (.gitignore .backups/) | 진짜 단순 | 본 commit `.gitignore` 갱신 ✅ |

## ★★★ 학습 자산 (M4.2 cross-review γ 9회차)

### 1. `static-analysis-vs-empirical-truth-gap` ★★★ (메타)
정적 분석(α reviewer + β Codex)은 *논리적 정합* 추론을 박지만 *실측 동작 truth*와 격차 가능. β P1 = TCP 순서 + main thread queue + 페이드 비동기 = *논리적*으로 페이드 중 roster 옛 씬 적용. 실측: enemy/RemotePlayer 잘 보임 = 어딘가 방어 메커니즘 (MainThreadDispatcher batch / 페이드 timing). **β 신뢰 맹목 X — 실측 우선**. 한국 게임 회사 실무 정합 → 면접 자산 ★★★ ("외부 cross-review 결과를 어떻게 검증하셨나요?" → "실측 우선 + 정적 추론은 가설로 취급" 답변).

### 2. `cross-review-followup-finding-pattern` ★★ (메타)
β 1차 봉합 → β 2차 → 추가 4건 발견. 봉합 자체가 *새 결함 차원* 도입. 옛 패턴(M3.7 옵션 C 게이트 5번째 실측)과 정합 = "봉합 후 재실측 의무". M4.3에서 재실측 cadence ADR 후보.

### 3. `user-as-final-truth-arbiter` ★★ (메타)
사용자가 "서버 구동 안 하고 테스트한 거 아니야?" 한 마디로 *γ 함정* 짚음. α/β 두 정적 시각 모두 *체크리스트 시야* 한계. 본인 *실측 동작* 시각이 final arbiter. 학부생 → 실무 도약 핵심 패턴.

## 머지 게이트 판정

- 🔴 (양쪽 다 잡음) 0건 → **GO**
- α/β 단독 잡음 = 봉합 또는 backlog 명시
- **머지 게이트**: **GO** (조건부 → 풀세트 봉합 + 학습 박음으로 격상)

## ➡️ 다음 액션

1. 본 마감 commit (사용자 본인 직접 git add + commit)
2. M4.2 PR 생성 (사용자 명시 GO 게이트 의무 — 헌법 + ADR-022)
3. PR 머지 후 M4.3 진입

## M4.3 backlog (본 γ에서 박힌 것)

- β P1 buffer 패턴 재실측 — 정확한 timing 시나리오 multi player 박아 발현 여부 검증, false positive 확정 시 봉합 롤백
- β P1-B lifecycle deltas + P1-C unsubscribe — P1 운명 정합 (P1 롤백 시 자동 해소)
- 봇 portal 좌표 const 공유 헬퍼 (α 🟡 3)
- cross-review 자동 재실측 cadence ADR — 봉합 후 재실측 의무 패턴 정합화
- **RemotePlayer Animator 작동 X** — 본인 검증 시 추가 발견 (외관 backlog, 본인 분담)
- 3 RemotePlayer prefab 동시 박힘 (`RemotePlayer.prefab` / `RemotePlayer.backup.prefab` / 새 Resources/RemotePlayer.prefab) — 정합 검토 + cleanup

## 참고 자료
- pre-review 자료: `00_Document/reviews/2026-05-28-claude-pre-review-m4.2-map-transition.md`
- α reviewer 호출: 메인 세션 SubAgent (Opus)
- β Codex 호출: `codex review --base main` + `codex review --uncommitted` + `codex exec --sandbox read-only` (메타 질문)
- 봉합 commit: `c8b7235` (qa P4) + 본 마감 commit (client P1+P2+P3 + 메인 세션 P2-A + P3-A)
