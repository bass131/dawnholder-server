# Cross-Review — 2026-05-30 — PR #56 (feature/m4.3 → main) 머지 전

> γ 방식 (α=Claude reviewer / β=Codex / γ=비교). `/cross-review` 산출물.
> 호출 경로: **Claude 직접 호출** (사용자 명시 — γ 9회차 예외). `codex exec -s read-only -C <dir>`.
> 사전 자료: `2026-05-30-claude-pre-review-pr56-m43-merge.md`

## 변경 범위
- `feature/m4.3` vs `main` (origin/main=`1078fec`), 119 파일 / +9094 -1422 / 13 커밋
- 등급: 대규모 (위험 깃발: irreversible — ProtocolVersion 6→7 + trust-boundary + unity-asset)
- 3 덩어리: Code Convention(ADR-028) / M4.3 Phase07 적AI 서버(Protocol 6→7) / M4.3R 리팩토링 7 Phase

## α — Claude reviewer 결과
**GO** 🟢 (🔴 0 / 🟡 2). 5축 전부 통과:
- 헌법 #2 Protocol: PDL append-only, ID 재사용 0, Current=7 정합
- 헌법 #3 trust-boundary: IntentRateLimiter/MapMigration 추출 후 invariant byte-for-byte 보존
- 동작 보존: God class 분리 = 순수 추출 (EnemyAiTests 12 + 315 회귀 0)
- cross-phase: 클라 S_EntityState 핸들러 0건 = **🟢 위반 아님 (Phase 08 의도 분할 + forward-compatible drop)**
- ADR-012: m_ → _camelCase 서버+ClientNet 자매 대칭
- 🟡: `BroadcastTests.cs:226` 주석 stale / `ClientPacketHandlers.cs:13` "12개"→11개

## β — Codex 결과
**NO-GO** (S_EntityState 수신 경로 누락):
- 가닥1 [🟡 확신] `RosterTransitionBuffer.cs:43` — `sceneLoaded +=` 해제 경로 없음 → reconnect/disconnect 시 stale buffer 생존
- 가닥2 [🟡 추측] `MapMigration.cs:123` — closing 검사 1회뿐 → 검사 직후 disconnect 시 ghost owner 잔존 가능
- 가닥3a [🔴 확신] `UnityClientSession.cs:83` — S_EntityState(19) 핸들러 없음 → broadcast Unknown drop, 적 위치 stale + 경고 로그 반복
- 가닥3b [🟡 확신] `EnemyAISystem.cs:130` — chase clamp 없음 → 진동 + de-aggro 후 patrol bound 스냅
- 가닥4: 통과

## γ 비교 분석

### 양쪽 다 잡음 (사실 일치, 심각도 불일치) — 최우선 판단
- **S_EntityState 클라 핸들러 누락**: α=🟢(Phase 08 의도 분할), β=🔴(NO-GO). **같은 사실, 정반대 심각도.**
  - 사실 확정(코드 검증): `SnapshotTickInterval=2` → 100ms마다 적 1마리당 초당 10회 broadcast. 클라 미등록 처리 = 패킷당 `Debug.LogWarning`(스로틀 0) → **초당 10×N 경고 스팸 + 적 클라 동결**.
  - 판정: **β가 더 정확**. α의 "안전한 degradation"은 crash 부재만 맞고, 실질 콘솔 스팸/desync 회귀를 과소평가. 단 β의 🔴도 과함 — crash/보안/프로토콜 위반 아님 + 계획된 Phase 08 갭. **진실 = 🟠 (실질 품질 회귀, 하드 블로커는 아님).**

### β만 잡음
- 가닥1 `RosterTransitionBuffer` 미해제 [확신]: **확정 실재** (grep `-=`/`OnDisable`/`OnDestroy` 0건). M4.2 γ9 "sceneLoaded unsubscribe 누락" 동일 패턴 재발. 격리된 클라 버그, 봉합 저위험.
- 가닥2 `MapMigration` ghost [추측]: Codex 자가 "추측" 표기. trust-boundary 인접하나 α는 invariant 보존 판정. 미검증 — 별도 확인 권장.
- 가닥3b chase clamp [확신]: **신규 아님** — work-pin 기지 reviewer 🟡 (Phase07, M4.4 backlog 이미 박힘).

### α만 잡음
- 주석 stale 2건 (`BroadcastTests.cs:226` / `ClientPacketHandlers.cs:13`) — 동작 무관, β는 안 봄.

### 양쪽 통과
- 헌법 #2 Protocol append-only / #5 tick 블로킹 0 / God class 추출 동작 보존(테스트 회귀 0)

## 결정 권유

🟠 **조건부** — 하드 블로커는 없으나 β가 실질 품질 회귀 1건(S_EntityState 스팸/desync) + 격리 버그 1건(RosterTransitionBuffer) 발견. α 단독 GO는 낙관적이었음.

- **권장: 봉합 먼저 → β 재실측(Step 4-A) → 머지.**
  - (a) S_EntityState 최소 핸들러 등록 (dispatch 테이블 한 줄 + 적 위치 갱신 — 스팸 제거 + 적 클라 이동) — Phase 08 핵심을 당겨오나 작음
  - (b) `RosterTransitionBuffer` sceneLoaded 해제 (`OnDisconnected`/teardown 경로)
  - (가닥2는 봉합 시 함께 확인, 가닥3b/주석 stale은 M4.4/Phase08 흡수)
- **대안**: GO + Phase 08 즉시 후속 (main에 transient 스팸 상태 허용) / 보류 후 Phase 08 완성

## 옛 학습 정합
- 가닥1 = `sceneLoaded unsubscribe 누락` (M4.2 γ9 학습 자산) 재발 → 패턴 재교육 가치
- β > α 영역: "코드 직접 접근 + 동작 추론"이 정적 헌법 점검이 못 본 런타임 회귀 포착 (γ 가치 재증명)
- 실측 우선(Step 4-B): 사용자 Play-test "이상 무"는 적 이동/콘솔 스팸을 *특정 점검 안 함* → β 코드 분석이 그 갭 메움

## 봉합 (사용자 결정: 봉합 먼저 → 재실측 → 머지)
client SubAgent 위임, additive only (Protocol/PDL/서버/prefab 변경 0):
- (a) S_EntityState 클라 핸들러: `EntityStateHandler`(ClientPacketHandlers.cs) + dispatch 테이블 1줄(UnityClientSession.cs) + `EnemyRegistry.UpdatePosition`(직접 transform 세팅, 보간은 Phase 08 — §0.3). 워커 파싱→primitive 캡처→MainThreadDispatcher 적용.
- (b) `RosterTransitionBuffer.Teardown()` + UnityClientSession.OnDisconnected(main thread) 호출. sceneLoaded 해제 + 상태 clear.

## β 재실측 (Step 4-A) — CLEAN
`codex exec -s read-only` 2차. 봉합 4파일만 점검:
- 스레드 안전 🟢 / Teardown -= 안전 🟢 / spawn 전 race silent skip 🟢 / 기존 경로 회귀 0 🟢
- **봉합 새 결함 0 / 재실측 결론: CLEAN**

## 최종 상태
α GO → β NO-GO(2건) → 봉합 → β 재실측 CLEAN. **남은 게이트: 사용자 Play-test 실측(Step 4-B) → commit → admin bypass 머지(사용자 GO).**
