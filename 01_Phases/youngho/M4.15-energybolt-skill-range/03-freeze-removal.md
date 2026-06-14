---
owner: youngho
milestone: M4.15
phase: 03
title: freeze 적용 제거 (인프라 보존)
status: pending
grade: 보통
domain: server
summary: ApplyFreeze 호출 2곳(Melee 평타 + Thunderbolt) 제거, freeze 인프라는 미래 빙결 스킬용 보존
---

# Phase 03: freeze 적용 제거 (인프라 보존)

> **상태**: pending
> **마일스톤**: M4.15
> **등급**: 보통
> **담당**: server (Sonnet Worker)

---

## 🎯 목표

Mage 평타와 Thunderbolt가 적을 얼리는 `ApplyFreeze` 호출을 제거해 메이플 에너지 볼트/번개의 거동(데미지만, stun 없음)에 맞춘다. 단, freeze **인프라**(`FrozenUntilTick`/`ApplyFreeze`/`EnemyAISystem` 가드/Boss 면역)는 미래 빙결 계열 스킬이 재사용하도록 **보존**한다 — 호출 *지점*만 걷어낸다.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (`MeleeAction.cs`/`CombatConstants.cs` 동일 파일 직렬화 — 02 먼저).

---

## 📝 작업 내용

- [ ] `MeleeAction.cs:79` — Mage 평타 `target.ApplyFreeze(...)` 호출 제거. (투사체/데미지 지연은 유지.)
- [ ] `ThunderboltAction.cs:43` — `target.ApplyFreeze(...)` 호출 제거 (Boss 제외 분기도 함께 정리).
- [ ] `CombatConstants.cs:50` `StunTicks` — 호출자 소멸로 dead. *제거하되* 주석으로 "빙결 스킬 도입 시 부활 (도착 후 추가 정지 틱)" 의도 보존.
- [ ] **인프라 보존 확인** (변경 X): `EnemyEntity.FrozenUntilTick`(L106)/`ApplyFreeze`(L142-143), `EnemyAISystem` freeze 가드(L24-29), Boss 면역(`EnemyKind.Boss continue`).
- [ ] 테스트 갱신:
  - `MageRangedCombatTests` — freeze assertion(L182-184 `FrozenUntilTick > 0`)을 "freeze 안 됨"(`== 0`)으로 전환.
  - `ThunderboltSkillTests` — freeze 관련 assertion 갱신.
  - `DeferredDamageSystemTests` — **인프라 테스트(freeze 가드/Boss 면역)는 유지** (인프라 살아있음). 단, 직접 `ApplyFreeze` 호출로 가드 동작을 검증하는 단위 테스트라 그대로 green.

---

## ✅ 완료 조건

- [ ] `ApplyFreeze` 호출 site = 0 (production grep). 인프라 정의(메서드/필드/가드)는 잔존.
- [ ] 적이 Mage 평타/Thunderbolt 맞아도 `FrozenUntilTick == 0` (테스트).
- [ ] `DeferredDamageSystemTests` freeze 인프라 테스트 green (인프라 보존 증명).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).

---

## 🧪 테스트

**자동**:
- `MageRangedCombatTests` — 평타 후 `FrozenUntilTick == 0`.
- `DeferredDamageSystemTests` — `ApplyFreeze` 직접 호출 시 가드 정상(인프라 무손상).

**수동**: 영호 Play — 적 연타해도 안 굳고 정상 이동/반격.

---

## 📚 학습 포인트

- **호출 제거 ≠ 기능 삭제** — 능력(infra)을 남기고 *사용*만 끊으면 미래 재활성화가 한 줄(ApplyFreeze 호출 추가). 메이플 빙결 스킬 대비 = YAGNI와 "재사용 가능 인프라 보존"의 균형.
- **dead 상수 처리** — 호출자 사라진 상수는 그냥 두면 혼란, 지우면 의도 소실. *주석으로 의도 박제 후 제거*가 정석.

---

## ⚠️ 함정 / 주의사항

- 인프라(`EnemyAISystem` 가드/`FrozenUntilTick`)를 *실수로 같이 지우지 말 것* — 보존이 명시 결정. 가드를 지우면 미래 빙결 스킬이 작동 안 함.
- Boss 면역 메커니즘(`EnemyAISystem`이 Boss를 freeze 가드 전에 skip)은 freeze 호출이 사라져도 그대로 — 인프라 일부라 유지.
- freeze 제거로 `travelTicks`와 freeze의 결합이 끊김 → Phase 04(투사체 모델)에서 `travelTicks`를 자유롭게 손봐도 freeze 부작용 없음 (의존 해소).

---

## ➡️ 다음 Phase

- Phase 04 — 투사체 일정 속도 (서버 모델).

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
