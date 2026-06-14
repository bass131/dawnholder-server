---
owner: youngho
milestone: M4.14
phase: 01b-attackstate-flyweight-fix
title: AttackState flyweight 공유 mutable 채널 제거 (병렬 테스트 race 근본수정)
status: done
grade: 보통
slug: 01b-attackstate-flyweight-fix
created: 2026-06-14
completed: 2026-06-14
domains: [server]
prior_phases: [01-baseline-and-prep]
depends_on: [01-baseline-and-prep]
risk_flags: []
---

# M4.14 Phase 01b — AttackState flyweight Pending* 제거

> **계획 외 파생 Phase**. Phase 01 베이스라인 측정 중 발견된 flaky(`CommitWindowTests.AttackState_BlocksMovement_MultipleInputs`)의 근본 원인을 추적해 수정한다. 영호 결정(2026-06-14): 근본 수정(안 B)을 Phase 01.5(=01b)로, 별도 파일.
> **순수 구조 리팩토링 — 거동 절대 보존.** server-only (98_Shared/클라/테스트 무변경).

---

## Context (왜) — flaky 추적

베이스라인 풀 스위트에서 `CommitWindowTests.AttackState_BlocksMovement_MultipleInputs` 실패 (공격 잠금 중 플레이어가 -0.41 좌측 드리프트). **격리 실행 = 6/6 통과, 풀 병렬 = 실패** → xUnit 병렬 실행 중 **공유 정적 상태 race** (실제 게임 코드 버그 아님 — 런타임은 맵당 단일 스레드라 안전). 현재 main에 이미 있던 조건(M4.14는 docs만 변경).

---

## 근본 원인 (file:line 확정 — 2026-06-14)

| 증거 | `파일:줄` | 내용 |
|---|---|---|
| flyweight 싱글톤 | `PlayerCombatStates.cs:16` | `static readonly AttackState Attack = new()` — 공유 인스턴스. |
| mutable 필드 3개 | `PlayerCombatStates.cs:34-37` (수정 전) | `PendingImpulseVx`/`PendingDecayPerTick`/`PendingDurationTicks` — flyweight에 가변 상태. |
| 공유 채널 write | `PlayerEntity.cs:240` (수정 전) | `EnterAttackState`가 `Attack.PendingImpulseVx = impulseVx`로 싱글톤에 씀. |
| 공유 채널 read | `PlayerCombatStates.cs:45` (수정 전) | `AttackState.Enter`가 `player.ExternalImpulseVx = PendingImpulseVx`로 싱글톤에서 읽음. |
| 자백 주석 | `PlayerCombatStates.cs:33` (수정 전) | "Flyweight 정적 인스턴스이므로 tick thread invariant(단일 스레드) 내에서만 유효." |
| 오염원 | `MeleeAction.cs:48-50` | lunge `AttackLungeInitialVx * FacingDir` — 좌향이면 음수. 병렬 melee 테스트가 `PendingImpulseVx` 덮어씀. |

**메커니즘**: write(`:240`)→read(`:45`) 사이에 다른 스레드(병렬 melee 테스트)가 `PendingImpulseVx`를 음수로 덮음 → 잠긴 플레이어가 엉뚱한 -임펄스 read → 매 틱 `DecayImpulse` 좌측 누적 → -0.41. **`HitState`는 이미 올바른 방식**(`EnterHitState`가 임펄스를 엔티티에 직접 세팅, `PlayerEntity.cs:259`) — Attack만 옛 방식이라 불일치.

> 흥미로운 연결: 이 "공유 mutable 상태로 파라미터 흘려보내기"는 Phase 02 `ActionContext`가 제거하는 안티패턴과 **같은 결**.

---

## 설계 (안 B — HitState 패턴 통일)

flyweight의 `Pending*` 3필드 완전 제거 → `EnterAttackState`가 impulse/decay/duration을 **PlayerEntity에 직접** 세팅. flyweight 진짜 무상태 복원.

- **세팅 시점 = `ChangeState` 이후** (핵심): `ChangeState`는 Exit(이전)→Enter(신규) 순서(`StateMachine.cs:29-35`). Exit가 `ExternalImpulseVx=0`으로 덮을 수 있으므로 그 *후* 세팅해야 안전.
- `AttackState.Enter` 메서드 제거 (base `ActorState.Enter` no-op). `Exit`는 `player.*` 리셋 2줄 유지, `Pending*` 리셋 3줄 제거.

## 변경 대상 (2파일 — server-only)

1. `02_Server/GameServer/Maps/PlayerEntity.cs` — `EnterAttackState`(`:236-245`): ChangeState 먼저, 이후 `StateTicksRemaining`/`ExternalImpulseVx`/`ImpulseDecayPerTick` 엔티티 직접 세팅.
2. `02_Server/GameServer/Maps/States/PlayerCombatStates.cs` — `Pending*` 3필드 + 주석 삭제, `Enter` 삭제, `Exit`에서 `Pending*` 리셋 제거.

---

## 완료 조건 / 게이트 (정량) — ✅ 통과 (2026-06-14)

- [x] `Pending*` 참조 0 (grep: 전투 상태 코드 잔존 0).
- [x] flyweight `AttackState` 무상태 (mutable instance 필드 0) — HitState와 패턴 통일.
- [x] **거동 보존**: WSL2 build 0/0 + **풀 병렬 스위트 3x green (568/0/5)** — 수정 전 567/1(flaky) → 후 568/0(안정). 전투 테스트(KnightDash/CombatSwing/AnimState/HitKnockback) 전부 green.
- [x] race 해소: 격리·병렬 결과 일치 (간헐성 0).
- [x] wire 무변경: server-only, 98_Shared/Protocol 미접촉, Shared.dll 무변경.
- [x] 테스트 파일 무변경 (거동 보존 증거 — assert가 그대로 통과).

**검증 흐름**: server Worker(Sonnet) 구현 → 메인 file:line 실측 대조 → WSL2 build 0/0 + 3x 병렬 568/0/5 → spurious Shared.dll 재빌드 아티팩트 복원(소스 무변경).

---

## 위험 / 헌법 게이트

- **§1 서버 권위**: 임펄스 값·계산 무변경 — 저장 위치만 flyweight→엔티티. 권위 불변.
- **헌법 "정적 mutable 게임 상태 금지"**: 이 수정이 정확히 그 위반(flyweight Pending*)을 제거 — 헌법 정합 강화.
- **§5 틱 루프**: 틱 new 0 유지 (엔티티 필드 = 기존, 신규 할당 없음).

---

## 학습 (캡스톤 자산)

- **flaky ≠ 코드 버그**: 격리 통과/병렬 실패 = 테스트 격리 문제. "코드가 맞나"가 아니라 "공유 상태가 새나"를 잼 (M4.13 BossSmoke flaky 교훈의 변주 — 그건 'CI가 빠른가', 이건 '병렬 안전한가').
- **flyweight는 무상태여야**: GPP Flyweight의 핵심은 상태 없음. mutable 필드를 파라미터 채널로 쓰면 단일 스레드 가정에 묶임 = 숨은 결합.

> Phase 02(IGameAction 계약 통일)로 진행 — 같은 안티패턴(공유 mutable 파라미터 전달)을 Action 계층에서 `ActionContext`로 제거.
