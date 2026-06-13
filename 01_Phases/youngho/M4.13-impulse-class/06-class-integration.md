---
owner: youngho
milestone: M4.13
phase: 06-class-integration
title: 클래스 통합 + 전체 회귀 — 넉백/임펄스공격에 같은 기계 적용 + 마일스톤 마감
status: planned
grade: 복잡
slug: 06-class-integration
created: 2026-06-13
domains: [client, qa]
prior_phases: [01-action-input-gate, 02-server-impulse-model, 03-dash-behavior, 04-shared-extract, 05-client-prediction-b]
depends_on: [05-client-prediction-b]
risk_flags: []
---

# M4.13 Phase 06 — 클래스 통합 + 전체 회귀

> 계획서 = `_milestone-plan.md` Phase 분해 표 #6. 대쉬는 "예측되는 외부 임펄스" **클래스의 첫 적용 사례**였다. 이 Phase에서 **같은 기계를 넉백·임펄스공격에도 적용**해 마일스톤의 본래 목적(*클래스 통일*)을 완성하고, 전체 회귀로 마감한다.

---

## Context (왜)

마일스톤의 핵심은 *대쉬 하나*가 아니라 **"클라가 예측 못 하는 서버 임펄스 동작" 클래스(대쉬·넉백·임펄스공격)를 시스템으로 통일**하는 것이다. P1~P5가 대쉬를 첫 사례로 그 기계(상태 데이터 소유 + 서버 임펄스 모델 + 공유 공식 + 클라 예측 B)를 깔았다. P6은 **넉백·임펄스공격이 같은 기계를 타게** 해 일회성 대쉬 특수 처리가 아님을 증명하고, 전체 회귀로 회귀 안전망을 친다.

---

## 작업 (착수 시 확정 — 골격)

- **넉백을 같은 기계로** — 피격 수신 시점이 시작점. 이미 Shared 상수(`KnockbackInitialVx` 등) + ε 클램프(M4.11 P2)가 있어, P5 InputHistory 임펄스 저장·예측 경로에 넉백을 태운다. forceAdopt Hit 분기도 방식 B로 재검토.
- **임펄스공격을 같은 기계로** — 공격 시점이 시작점(AttackLungeVx). lunge가 P5 예측 채널을 타게.
- **시작점만 다름**(계획서): 대쉬=시전 시 / 넉백=피격 수신 시 / 임펄스공격=공격 시 — 동일 임펄스 예측 기계.

---

## 완료 조건 / 게이트 (정량 — 마일스톤 마감)

- [ ] 넉백·임펄스공격이 **대쉬와 같은 임펄스 예측 기계** 경유(grep으로 분기 통일 제시 — 임펄스별 특수 분기 최소화).
- [ ] 넉백/lunge 중 forceAdopt 빈도 급감(2클라 `[Reconcile]` 분석) — 대쉬와 동일 개선.
- [ ] **전체 회귀 green**:
  - WSL2 build+test 비감소(baseline 561/0).
  - EditMode 전체 green(`PlayerPredictorTests`/`MovementGateTests` 포함).
  - 봇 회귀(연속 + fresh 재검 — BossFight/HpSync 등 연속 누적 FAIL은 fresh PASS로 판정).
  - Unity 콘솔 error 0.
- [ ] **2클라 Play 종합 실측**: 대쉬·넉백·임펄스공격 모두 스터터 소멸 + 영호 손맛.
- [ ] **마일스톤 `_milestone-DONE.md` 박제**(대규모 마일스톤 5단계 보고).

---

## 위험 / 헌법 게이트

- **§1 서버 권위**: 모든 임펄스 = 서버 권위 위치. 클라는 예측 + reconcile.
- **§2 Protocol**: 통합이 wire 건드리면 STOP. 현재 v12 유지 목표.
- **회귀 비결정 주의**: 봇 연속 FAIL ≠ 회귀(서버 상태 누적 entity=0 → fresh 단독 재검 PASS가 판정). 빌드 신선도 = Managed DLL mtime.
- **§4**: 넉백/lunge 임펄스 공식도 `98_Shared` 단일 출처(P4) 정합 — 클라/서버 동일.

---

> Phase 완료 = **M4.13 마일스톤 마감**. `06-...-DONE.md` + `_milestone-DONE.md` 박제(대규모 — 5단계 보고 MD/HTML). 임펄스 동작 클래스 통일 완성.
