---
owner: youngho
milestone: M4.6
phase: 04
title: 몬스터 AI(Patrol/Chase/Attack)를 검증된 State 베이스로 확장
status: pending
grade: 복잡
estimated: 2~3h
domain: server
---

# Phase 04: 몬스터 AI를 검증된 State 베이스로 확장

> **상태**: pending
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 복잡 (server — AI 리팩토링, 행동 회귀 안전 요구)
> **담당**: server

---

## 🎯 목표

플레이어로 **검증된** State 프레임워크를 일반/골렘 몬스터 AI에 확장한다. 현재 `EnemyAISystem`(enum+switch, Patrol/Chase 193줄)을 State 클래스(`PatrolState` / `ChaseState`)로 옮기되, **기존 행동(aggro 히스테리시스, 순찰 경계, Chase 대상 교체)은 비트 단위로 보존**한다 (순수 구조 이주, 행동 변경 없음). 여기서부터 State 클래스의 확장성 값어치가 실제로 발생한다(상태가 늘기 시작).

이 Phase가 끝나면: 몬스터가 동일 베이스 위에서 기존과 똑같이 순찰/추격한다.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (프레임워크 + commit window 패턴 — 몬스터 공격도 동일 패턴 참고)
- [ ] (병렬 시) Phase 03과 무관하게 진행 가능 (도메인 분리)

---

## 📝 작업 내용

- [ ] `PatrolState` / `ChaseState` 클래스화 — `EnemyAISystem.Update`의 분기를 State Tick으로 이전
- [ ] aggro 진입(`|dx| ≤ AggroRange`) / de-aggro 히스테리시스(`> AggroRange × 1.5`)를 상태 전환 조건으로 명시
- [ ] 순찰 경계(`SpawnX ± PatrolRange`) + `PatrolDir` 반전 = `PatrolState` 내부로 캡슐화
- [ ] **AnimState(시각) ↔ EnemyState(AI) 분리 유지** — State 전환은 서버 내부, 클라엔 여전히 animState만 노출

> **범위 못박음 (plan-auditor 🟡 흡수)**: 이 Phase는 **순수 구조 이주만**(Patrol/Chase). 몬스터 commit window(공격 중 추격 멈춤) = *행동 변경*이라 **본 마일스톤 밖**으로 명시 제외 — 행동 보존 리팩토링(완료 조건 desync 0.00)과 섞으면 회귀 입증 면적이 흐려짐. 몬스터 행동 디테일은 통일 베이스가 굳은 뒤 별도 마일스톤.
- [ ] `EnemyEntity`에 StateMachine 장착, `GameMap.Tick()`의 EnemyAISystem 호출 지점 정합

---

## ✅ 완료 조건

- [ ] 몬스터가 기존과 동일하게 순찰/추격 — 봇 시나리오(EnemyAi) desync **0.00**
- [ ] aggro/de-aggro 경계값 회귀 0 — 단위 테스트로 진입/이탈 틱 동일
- [ ] 골렘(EnemyKind=2)도 동일 베이스에서 동작 (종류별 분기 0 유지)
- [ ] `dotnet test` green + 기존 AI 테스트 회귀 0
- [ ] reviewer 🔴 0

---

## 🧪 테스트

**자동**:
- `EnemyStateTests` — Patrol↔Chase 전환 조건, 순찰 경계 반전, 대상 교체
- 기존 `EnemyAiTests` 회귀

**수동**:
- WSL2 서버 + Play — HuntingGround에서 슬라임/골렘 순찰·추격이 이주 전과 동일

---

## 📚 학습 포인트

- **추상화의 값어치가 드러나는 순간** — 플레이어와 다른 actor(몬스터)가 같은 베이스를 재사용. 베이스가 현실 케이스 2개로 검증됨
- **행동 보존 리팩토링 2회차** — 회귀 안전망(desync/테스트)을 다시 적용

---

## ⚠️ 함정 / 주의사항

- 히스테리시스(진입 ≠ 이탈 임계)를 한 조건으로 합치는 함정 — 채터링(상태 떨림) 유발. 두 임계 보존
- Boss는 건드리지 말 것 — 별 System(Phase 05). 여기서 섞으면 회귀 범위 폭발
- State 전환 시 `TargetEntityId` 등 컨텍스트 누락 — Enter/Exit에서 정리

---

## ➡️ 다음 Phase

- Phase 05 — 보스를 명시적 State로 정리

---

## 📋 박제 (완료 후)

- 복잡 → **-DONE.md**

---

## 작업 로그

- 2026-06-08: 신설 (plan)
