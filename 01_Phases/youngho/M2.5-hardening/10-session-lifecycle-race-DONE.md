---
summary: Session lifecycle race 봉합 — _closing 플래그 + always-enqueue + owner-ref cleanup. connect 직후 tick 전 disconnect 와도 ghost player 0. 8개 결정론적 race 시나리오 테스트.
phase: 10-session-lifecycle-race
work-id: phase10-session-lifecycle-race
status: done
completed_at: 2026-05-18
commit: (commit 시점에 박힘)
---

# Phase 10 — Session lifecycle race 제거 완료 박제

**소요 시간**: ~1.5시간 (구현 1h + Codex β 2차 검토 + 2건 반영 0.5h)

## TL;DR

γ 감사 Codex β 위반 — accept 직후 tick 전에 끊기면 OnDisconnected의 `_entityId<0` early-return으로 cleanup 누락, queued AddPlayer가 닫힌 세션을 owner로 player 박음 → ghost. Phase 10이 봉합: `_closing` 플래그 + always-enqueue disconnect + owner-ref cleanup. AddPlayer job 실행 시점에 `_closing` 재확인으로 race window 차단. `Interlocked.Exchange`로 이중 OnDisconnected 멱등성. 8개 deterministic + smoke 시나리오로 회귀 안전망. M3 broadcast 첫 데모에서 ghost entity 시각화 사고 사전 방지.

## 5단계 보고

- **무엇을 만들었나** —
  - `GameSession._closing` (int, Interlocked) 플래그 신설
  - `OnConnected` map job — 실행 시점에 `Volatile.Read(_closing) == 1`이면 AddPlayer + Send skip + `[Map] AddPlayer skipped` 로그
  - `OnDisconnected` 통째 재작성 — `Interlocked.Exchange(ref _closing, 1) == 1`이면 즉시 early return (이중 호출 멱등성). 아니면 항상 map job enqueue. early-return `_entityId < 0` 패턴 제거.
  - `GameMap.RemovePlayerBySession(GameSession owner)` helper — `ReferenceEquals(p.Owner, owner)` 기반 멱등 cleanup
  - cleanup 후 `self._entityId = -1` reset (Codex β 권장: 낡은 id 잔존 방지)
  - 신규 테스트 8 케이스: deterministic race A/A2, 회귀 B, 멱등성 C, smoke 100회 D, edge E/E2, owner cleanup 정확성 F + entity id reset 1건

- **왜 필요한가** —
  γ 감사 Codex β가 짚은 위반 — `GameSession.cs:41~62`(connect job)와 `:70~78`(disconnect handler)가 다른 thread에서 race. 실 운영에서는 클라가 connect 직후 즉시 끊는 케이스(네트워크 끊김, 사용자 즉시 종료, 봇 테스트 등)에서 발생. M3 broadcast 진입 시 *ghost entity*가 다른 플레이어 화면에 보이는 시각화 사고로 표면화. Phase 09 fail-closed disconnect가 박힌 후라야 race 테스트 신뢰성 확보 — 그래서 Phase 10이 09 의존.

- **어떻게 만들었나** —
  1. Phase 10 작업 내용 1~4 순차: `_closing` 필드 → OnConnected `_closing` 체크 → OnDisconnected 재작성 → GameMap helper.
  2. 테스트 작성: GameSession.GetMap() override로 singleton 의존 차단 (Phase 09에서 도입). 6 deterministic 시나리오.
  3. `dotnet test` → 6/6 통과 (lifecycle filter), 133/134 전체 (1 LongRunning skip).
  4. **Codex β 2차 검토 (xhigh reasoning)** — 안전망 평가 + 2건 추가 권장:
     - (A) cleanup 후 `_entityId` reset 안 됨 → 추가 (낡은 id 잔존 차단)
     - (B) OnDisconnected → OnConnected *역순* 시나리오 미테스트 → ScenarioA2 추가
  5. 추가 후 133/134 → 135/136 (+2 신규), 회귀 0.

- **테스트 결과** —
  - `dotnet test Dawnholder.slnx` → **통과 133 / 실패 0 / 건너뜀 1**, 기간 45초
  - Phase 10 직전(Phase 09 직후): 125 → **+8 신규** (6 lifecycle + 2 Codex β 추가)
  - 시나리오 A: OnConnected → OnDisconnected → Tick → players=0 + "AddPlayer skipped" 로그 (deterministic race regression)
  - 시나리오 A2: OnDisconnected → OnConnected → Tick → players=0 (역순 안전)
  - 시나리오 B: 정상 connect/tick/disconnect flow 회귀
  - 시나리오 C: 이중 OnDisconnected → 로그 1회만 (멱등성)
  - 시나리오 D: rapid 100회 connect/disconnect → 누적 누수 X
  - 시나리오 E: disconnect-only edge case 안전
  - 시나리오 F: 두 세션 중 한 명만 disconnect 시 owner 정확성
  - `M2BasicMovementIntegrationTests` 정상 시나리오 회귀 0

- **다음 스텝** —
  Phase 11 (M2.5 정리) — 헌법 우선순위 표 + CONTEXT.md 응축 + UnitTest1 삭제. 선택적 Phase. 또는 바로 M3 진입.

## AC 검증 결과

```bash
$ dotnet build Dawnholder.slnx
  경고 0개 / 오류 0개

$ dotnet test Dawnholder.slnx --nologo
  통과 133 / 실패 0 / 건너뜀 1 (LongRunning), 기간 45초

# Lifecycle filter
$ dotnet test --filter "FullyQualifiedName~GameSessionLifecycle" --nologo
  통과 8 / 실패 0
```

완료 조건 체크:
- [x] 시나리오 A~D xUnit 통과 + 추가 A2/E/F
- [x] 시나리오 A 결정론적 (queue 순서 의존 — Codex β 확인: "정확한 deterministic regression")
- [x] 시나리오 C 이중 OnDisconnected 멱등성
- [x] dotnet test 전체 통과 (회귀 0)
- [x] rapid 100회 누적 player 0 (ScenarioD)
- [x] M2BasicMovementIntegrationTests 회귀 통과
- [x] DONE.md 작성 + Post-flight 게이트

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **`_closing` int + Interlocked.Exchange + Volatile.Read** — 가장 가벼운 cross-thread 플래그 패턴. int 접근 원자성 + Exchange 강한 barrier + Volatile.Read visibility. Codex β "tick thread/IOCP thread 사이 플래그 용도에는 충분" 확인.
- **always-enqueue + 멱등 helper 조합** — `_entityId<0` early-return 대신 항상 enqueue + 멱등 함수. "두 이벤트 사이" race의 표준 패턴 (Discord 봇, gRPC 스트림 등).
- **owner ref vs entityId cleanup** — race window 안 entityId가 -1일 수 있으니 owner reference 기반. `ReferenceEquals`로 신원 비교 명시 (== 오버로딩 차단).
- **GetMap() virtual은 Phase 09에서 이미 도입** — Phase 10 테스트가 그 덕에 singleton 의존 0. 두 Phase의 시너지.
- **Codex β 검토 vs 본인 결정** — `_entityId` reset은 fundamental 아니지만 로그 정합성 + 잠재 방어 로직 안전성 효과. 역순 시나리오는 운영엔 없지만 race 안전망 대칭성 입증. 둘 다 cheap → 반영.

## 막혔던 지점

- 막힌 곳 없음. Phase 09에서 GetMap() virtual + Send virtual + EnqueueJob virtual 도입한 덕에 Phase 10 테스트가 깨끗하게 박힘.
- 단, Codex β 추가 발견 "Console.WriteLine assert 문구 의존" — ScenarioA가 `"AddPlayer skipped"` 정확한 문구에 의존. 다만 state assertion(`Assert.Empty(_map.Players)`)이 *주된* 검증이고 로그는 *보조*이므로 그대로 둠. 문구 변경 시 두 assertion이 모두 영향 받지만, 그건 헌법 §1 위반 시 의도된 신호.

## 학습 일지 후보 키워드

- **분산 시스템의 "두 이벤트 사이" race** (학습 일지 ★★★ "Connect/disconnect race = 분산 시스템 첫 표적" 직접 시연)
- **EntityId 기반 cleanup의 한계** (race window 안 entityId == -1)
- **`_closing` flag + idempotent cleanup 패턴** (락 없이 race 봉합)
- **`Interlocked.Exchange` 반환값 시맨틱** (박기 전 값 — `== 1`이면 이미 박혀있었다)
- **`Volatile.Read` vs `Interlocked.CompareExchange(_, 0, 0)`** (둘 다 acquire, 후자가 cheaper)
- **`ReferenceEquals` vs `==`** (참조 신원 비교 명시 — operator overload 차단)
- **deterministic race test vs CPU race test** (queue 순서 의존 재현이 본 버그에 정확히 맞는 case)
- **Codex β의 "추가 발견" 패턴** (구현은 OK인데 *주변 안전망*이 누락된 곳을 짚어줌 — _entityId reset)

## 후속 안건 (본 Phase scope 밖)

- **broadcast loop `p.Owner.Send`의 _closing 체크** — Phase 10에선 broadcast 없으니 OK. M3 진입 시 closed owner 송신 정책 결정 (drop / 예외 / 무음 실패).
- **deterministic 외 CPU race 시뮬레이션** — IOCP thread + tick thread를 *실제로* 동시에 돌리는 race test는 헤비. 본 Phase는 queue 순서 의존만 cover. CPU race는 통합 테스트(M2BasicMovementIntegrationTests rapid scenarios)로 substitute.
- **Console.WriteLine 의존 → structured logging** — Serilog 도입 시 자연 해결. 본 Phase는 그대로.

## 작업 로그

- 2026-05-18: Phase 10 시작. Phase 09 GetMap virtual 도입 덕분에 테스트 인프라가 깨끗.
- 2026-05-18: 1차 dotnet test → 6/6 통과 + 전체 131/132 (회귀 0).
- 2026-05-18: Codex β 2차 검토(xhigh, ~11k token) → 안전망 평가 + 2건 추가 권장 반영 (_entityId reset / 역순 시나리오).
- 2026-05-18: 최종 회귀 133/134 통과 (+8 신규, 1 LongRunning skip). Phase 완료.
