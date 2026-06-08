---
owner: youngho
milestone: M4.6
phase: 01
title: State 머신 골격 + 플레이어 이동 상태 이주 (행동 불변)
status: done
grade: 복잡
summary: 서버 행동 State 프레임워크(ActorState/StateMachine) 신설 + 플레이어 Idle/Move/Jump를 State로 이주, 행동 비트 불변 입증
---

# Phase 01 — DONE: State 머신 골격 + 플레이어 이동 상태 이주

> **완료**: 2026-06-08 (세션24) · 커밋 `f12923c` (코드) + `0f83e35` (ADR-030) · 브랜치 `feature/m4.6-01-state-machine-skeleton`

---

## TL;DR

서버에 공통 행동 State 프레임워크(`ActorState` 추상 + `StateMachine` 드라이버)를 신설하고 플레이어 이동 계열(Idle/Move/Jump)만 그 위로 이주했다. **행동 비트 불변** — build 0/0 + test 436/0/4(회귀 0) + 봇 desync 0.00 + reviewer 🔴0으로 입증. commit window/전투 상태 State화는 Phase 02.

---

## AC 검증 결과

| 완료 조건 | 결과 |
|---|---|
| `dotnet build` 경고0/오류0 | ✅ (WSL2 / ADR-029) |
| `dotnet test` 회귀 0 | ✅ **436 Passed / 0 Failed / 4 Skipped** (M4.5 419 → +신규 StateMachineTests) |
| 봇 M2BasicMovement desync 0.00 | ✅ bot=(5.97,0.00) server=(5.97,0.00) **desync=(0.00,0.00)**, success=True, intents=1000 |
| S_Snapshot animState 바이트 동일 | ✅ 기존 `AnimStateTests`(animState 단언) 전부 통과 = 비트 동일 |
| reviewer 🔴 0 | ✅ Tier 2-A 5축 통과, 메모리리스 등가성 적대적 점검 통과 |
| ProtocolVersion 9 불변 | ✅ 98_Shared 무변경 |

검증 명령 (WSL2, ADR-029):

```
~/.dotnet/dotnet build Dawnholder.slnx              → 0 Warning / 0 Error
~/.dotnet/dotnet test Dawnholder.slnx --no-build    → 436 / 0 / 4
HeadlessBot --scenario M2BasicMovement              → desync (dx=0.00, dy=0.00)
```

---

## 결정 흐름

**무엇**: 신규 4 (`ActorState`/`StateMachine`/`PlayerMovementStates`/`StateMachineTests`) + 수정 2 (`PlayerEntity` MovementFsm 장착 / `GameMap` physics 루프 Tick 삽입 + `ComputePlayerAnimState` 이동 분기 위임).

**왜**:
- **이동 계열만 먼저 + 행동 불변** → 가장 단순한 상태로 프레임워크 end-to-end 검증. 설익은 추상화 회피(플레이어 이동이라는 실제 케이스로 베이스 굳힘 — 몬스터/보스는 Phase 04/05).
- **Flyweight 정적 인스턴스** → tick 루프 `new` 0, GC spike 0 (헌법 #5 / GPP-06).
- **폴더 `Maps/States/`** → PlayerEntity(Maps/) + System(Maps/Systems/) 계층 정합.

**어떻게 (행동 불변 등가성)**: FSM 메모리리스 — 어느 상태든 1틱 Tick이면 현재 (OnGround, vx)의 직접 매핑(`!OnGround→Jump / |vx|>0.01→Walk / else→Idle`)과 동일한 결과로 수렴. VxEpsilon `0.01f` 보존. Death/Hit/Attack 오버라이드는 이동 위임 위에 그대로 보존.

---

## 학습 일지 후보 키워드

- **State 패턴 / 메모리리스 FSM** — 상태를 들고 있어도 결과가 stateless 직접 계산과 동일할 수 있는 이유. Phase 02 commit window 도입 시 메모리리스가 깨지고, 그 지점이 State 패턴이 값을 발휘하는 자리
- **행동 보존 리팩토링(behavior-preserving refactor)** — 구조만 바꾸고 결과 비트 동일. test/desync/byte 3중 입증
- **Flyweight** — tick hot-path 무할당

---

## reviewer 🟡 (Phase 02 입구로 이월)

reviewer 선택 제안 2개 — *메모리리스가 깨지기 직전(Phase 02 입구)에 추가가 가성비 최고*:
1. 명시적 등가성 테스트 ("옛 직접 매핑 == FSM.AnimState" 표 기반)
2. epsilon 경계 테스트 (`vx == 0.01f` 경계 동작 단언)

→ Phase 02 작업 내용에 흡수 (commit window 도입으로 행동이 의도적으로 바뀌는 지점을 diff로 선명하게).

---

## 다음

- Phase 02 — 플레이어 전투 상태(Attack/Hit/Death) + commit window 규칙 + 98_Shared 상수 (trust-boundary)

---

## 작업 로그

- 2026-06-08: server SubAgent 위임 → 메인 검수(행동 불변 추적) → reviewer ✅ → WSL2 test 436/0/4 + 봇 desync 0 → 커밋 `f12923c`. ADR-030 동반 박제.
