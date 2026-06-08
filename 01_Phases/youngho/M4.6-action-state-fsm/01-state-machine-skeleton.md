---
owner: youngho
milestone: M4.6
phase: 01
title: State 머신 골격 + 플레이어 이동 상태 이주 (행동 불변)
status: done
grade: 복잡
estimated: 2~3h
domain: server
---

# Phase 01: State 머신 골격 + 플레이어 이동 상태 이주 (행동 불변)

> **상태**: pending
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 복잡 (server 단일 도메인, 신규 framework지만 **행동 불변** → 회귀 안전)
> **담당**: server

---

## 🎯 목표

서버에 **공통 행동 State 프레임워크**(추상 베이스 `ActorState` + `StateMachine` 드라이버)를 신설하고, 플레이어의 **이동 계열 상태(Idle / Move / Jump)**만 먼저 그 위로 이주한다. **행동은 1도 안 바뀐다** — 같은 입력에 같은 결과. 오직 구조만 enum/암묵 분기 → State 클래스로.

이 Phase가 끝나면: 플레이어가 *기존과 동일하게* 움직이되, 내부적으로는 State 객체가 전환을 관장한다. 프레임워크가 가장 단순한 상태로 end-to-end 검증됨.

---

## ⏪ 사전 조건

- [ ] M4.5 완전 마감 (main `5613cfa`)
- [ ] 실측 자료 숙지 (`_fsm-design-discussion.html`) — 현재 패턴 = latch 카운터 + Physics 직결

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Actors/States/` (가칭) 신설 — `ActorState` 추상 베이스: `Enter()` / `Tick()` / `Exit()` + 전환 요청 인터페이스
- [ ] `StateMachine` 드라이버 — 현재 상태 보유 + `ChangeState()` (Exit→Enter 순서 보장) + 틱당 `Tick()` 위임
- [ ] 플레이어 이동 상태 3종 클래스화: `IdleState` / `MoveState` / `JumpState` — 기존 `LocalPlayer` 예측이 아닌 **서버 측** 상태 (현 AnimState 산출 로직 = Idle/Walk/Jump 분기를 State 전환으로 이전)
- [ ] `PlayerEntity`에 `StateMachine` 필드 장착 — 기존 latch 필드는 **건드리지 않음**(Phase 02에서 통합)
- [ ] `GameMap.Tick()` 순서 안에 StateMachine.Tick 삽입 (Physics.Step 결과를 보고 상태 산출하는 현 위치 정합)
- [ ] AnimState 산출을 State에서 노출 (`CurrentState.AnimState`) — wire 값은 **완전 동일**해야 함

---

## ✅ 완료 조건

- [ ] `dotnet build` 경고 0 / 오류 0
- [ ] `dotnet test` 기존 통과 수 **그대로 유지** (회귀 0) — 이동/점프 관련 테스트 전부 green
- [ ] 헤드리스 봇 이동 시나리오 desync **0.00** (행동 불변 입증 — 이주 전후 동일 궤적)
- [ ] S_Snapshot의 animState 바이트가 이주 전과 **비트 동일** (Idle/Walk/Jump 케이스) — 캡처 비교
- [ ] reviewer 🔴 0

---

## 🧪 테스트

**자동**:
- `StateMachineTests` 신설 — Enter/Exit 호출 순서, ChangeState 자기전이 가드
- 기존 이동/점프 테스트 회귀 확인 (jump buffer 포함)

**수동**:
- WSL2 서버(ADR-029) + Unity Play — 걷기/점프/정지 애니가 이주 전과 똑같은지 눈으로

---

## 📚 학습 포인트

- **State 패턴** — switch 비대화 대신 상태별 클래스로 행동 분기 캡슐화. Enter/Tick/Exit 라이프사이클
- **행동 불변 리팩토링(behavior-preserving refactor)** — 구조만 바꾸고 결과는 비트 동일하게. 회귀 안전망(테스트+desync+바이트 비교)으로 입증
- **설익은 추상화 회피** — 베이스를 *플레이어 이동*이라는 실제 케이스로 먼저 굳힘 (몬스터/보스는 아직 안 봄)

---

## ⚠️ 함정 / 주의사항

- **행동을 "개선"하고 싶은 유혹** — 이 Phase는 구조만. commit window/이동 잠금은 Phase 02. 여기서 손대면 회귀 입증 불가
- 추상 베이스를 너무 일반화하지 말 것 — 지금은 플레이어 이동만 담으면 됨. 몬스터/보스 요구를 미리 추측해 넣으면 설익은 추상화
- `GameMap.Tick()` 호출 순서 깨지면 latch/snapshot 타이밍 어긋남 — 기존 §2.2 순서 보존

---

## ➡️ 다음 Phase

- Phase 02 — 전투 상태(Attack/Hit/Death) + commit window 규칙 (이동 잠금)

---

## 📋 박제 (완료 후)

- 복잡 등급 → **-DONE.md** (요약 + 사실 박제 + 학습 키워드). HTML 페어 불필요(대규모 아님)

---

## 작업 로그

- 2026-06-08: 신설 (plan)
