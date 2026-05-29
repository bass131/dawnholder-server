---
owner: youngho
milestone: M4.3R
phase: 04
title: GameSession trust-boundary 추출 (IntentRateLimiter + MapMigration)
status: pending
grade: 복잡
risk: trust-boundary
domain: server
estimated: 3~5h
---

# Phase 04: GameSession trust-boundary 추출 (rank 4)

> **상태**: pending
> **마일스톤**: M4.3R
> **등급**: 복잡 + **trust-boundary 자동 상향** (헌법 #3 신뢰 경계 코드)
> **담당**: server SubAgent (+ reviewer 강화 — trust-boundary)

---

## 🎯 목표

`GameSession`(700줄)에서 분리 이득이 명확한 메커니즘 둘을 추출한다: rate-limit 윈도우 로직 → `IntentRateLimiter`(socket 없이 단독 테스트 가능), 160줄짜리 단일 migration 람다 → `MapMigration.Execute` 헬퍼(검증/transfer 단계 가독 분리). **handshake state는 §0.3에 따라 컨테이너 잔류**(socket lifecycle 강결합 — 억지 분리 금지). **헌법 #3 신뢰 경계 invariant가 추출 과정에서 흩어지지 않는 게 최우선** — 동작·검증 결과 완전 보존.

---

## ⏪ 사전 조건

- [ ] Phase 03 완료 (server 도메인 순차 — **같은 server 영역 God class 동시편집 충돌 회피**가 주 사유. migration이 쓰는 `map.RemovePlayer`/`AddPlayerWithId`는 Phase 03이 *추출 않고 컨테이너에 잔류*하므로 surface는 이미 안정 — 순차는 충돌 회피 목적)
- [ ] Phase 01 베이스라인

---

## 📝 작업 내용

- [ ] **IntentRateLimiter** 추출 — `_rateLimitWindow`/`_intentCountInWindow`/`_rateLimitLoggedThisWindow` + `bool TryConsume(out bool firstWarn)`. `SubmitMoveIntent`(L307~325)는 `limiter.TryConsume()` 호출만
- [ ] **IntentRateLimiter xUnit 단독 테스트** 신설 — 윈도우 갱신/임계 초과/첫 경고 1회 로직을 socket 없이 검증 (현재는 GameSession 전체를 세워야 테스트 가능 = 이 추출의 핵심 이득)
- [ ] **MapMigration.Execute(GameSession self, GameMap from, Portal portal, ...)** 헬퍼 — `SubmitEnterPortal`(L428~586) `EnqueueJob` 람다 본문 추출. 검증 단계(portal lookup·근접)와 transfer 단계(맵A RemovePlayer·맵B AddPlayerWithId·roster/enemy 재전송)로 가독 분리
- [ ] `GameSession`은 socket 진입점(OnConnected/OnDisconnected/OnRecvPacket/Send) + 세션 state 소유만 잔류

### ⚠️ 분리 금지 (§0.3 overSplitWarning)
- [ ] **handshake state**(`_handshakeCompleted`/`_enteredWorld`/`CompleteHandshakeAndEnter`/`RejectHandshake`/`EnterGameWorldIfReady`)는 socket lifecycle 강결합 → **컨테이너 잔류**. 빼면 socket 콜백과 두 파일을 둘 다 열어야 흐름이 보임

---

## ✅ 완료 조건

- [ ] `GameSession.cs` < 600줄 (size-guard 경고 해소)
- [ ] IntentRateLimiter 추출 + 단독 xUnit 테스트 N개 통과
- [ ] MapMigration.Execute 추출 (검증/transfer 가독 분리)
- [ ] **trust-boundary 검증 invariant 보존 확인**: rate-limit 임계·handshake 게이트·portal 근접 검증이 추출 후에도 *같은 입력에 같은 거부/허용* (헌법 #3)
- [ ] **동작 보존**: `dotnet test --no-incremental` 회귀 0 — 기존 `GameSessionRateLimitTests`/`SessionStateMachineTests`/`EnterPortalHandlerTests`/`HandshakeHandlerTests`/`GameSessionLifecycleTests` 전부 통과
- [ ] 헤드리스 봇 `MapTransitionScenario`/`MultiRosterSmoke` 통과
- [ ] reviewer **trust-boundary 강화 점검** 통과 (헌법 #3 + §2.2 + §0.3 = 축6)

---

## 🧪 테스트

**자동**: IntentRateLimiter 단독 테스트(신규) + 기존 rate-limit/handshake/portal 테스트 회귀 0.
**수동**: 봇 다수로 rate-limit 임계 초과 시도 → 거부 + 첫 경고 1회 로그 확인. portal 근접 검증 우회 시도 → 거부 확인.

---

## 📚 학습 포인트

- **trust-boundary 리팩토링 위험**: 검증 코드를 옮길 때 가장 흔한 사고 = invariant가 두 곳으로 쪼개져 한쪽만 검사. 신뢰 경계(헌법 #3)는 "옮긴 후에도 같은 입력에 같은 판정"을 테스트로 박제.
- **테스트 가능성을 위한 추출**: rate-limit 로직이 socket과 묶여 있으면 단독 테스트 불가 → 작은 클래스로 떼면 시간 윈도우 로직만 격리 검증. 추출의 진짜 이득은 "줄 수"가 아니라 "테스트 가능성".
- **§0.3 균형**: 같은 God class라도 rate-limit(추출 이득 큼)과 handshake(socket 강결합, 추출 손해)는 다르게 판단. "항상 쪼개라"가 아니라 균형.

---

## ⚠️ 함정 / 주의사항

- **🔴 trust-boundary — 발표 직전 리스크**: rate-limit/handshake/portal 근접검증은 핵 취약점·동기화 버그 직결. 회귀 안전망(신규 + 기존 테스트 + 봇 스모크)을 *추출 전후 모두* 돌려 같음을 증명. 의심되면 Phase를 발표 후로 미룰 수 있음 (마일스톤 plan 타이밍 판단).
- **handshake 억지 분리 금지(§0.3)** — overSplitWarning 박힘. rate-limiter + migration만.
- **migration 람다는 tick thread(EnqueueJob) 안 실행** — 헬퍼 추출해도 호출 위치는 그대로 (§1.1·헌법 #5).
- **DLL stale 함정**: GameSession은 서버 전용이라 Shared 영향 적지만, 빌드 1회 후 테스트.

---

## ➡️ 다음 Phase

- Phase 05 (클라 기회성) / Phase 06 (클라 네이밍) / Phase 07 (네트워크 prefix) — cleanup 묶음

---

## 📋 박제 (완료 후)

- **복잡 + trust-boundary** — `04-gamesession-trust-boundary-extract-DONE.md` 박음 (검증 invariant 보존 사유 명시).

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
