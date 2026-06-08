---
owner: youngho
milestone: M4.6
phase: 04
title: 몬스터 AI(Patrol/Chase/Attack)를 검증된 State 베이스로 확장
status: done
grade: 복잡
estimated: 2~3h
domain: server
---

# Phase 04: 몬스터 AI를 검증된 State 베이스로 확장

> **상태**: done (스코프 확장 — 아래 ⚠️ 참조)
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 복잡 (server — AI 리팩토링, 행동 회귀 안전 요구)
> **담당**: server

---

## 🎯 목표

플레이어로 **검증된** State 프레임워크를 일반/골렘 몬스터 AI에 확장한다. 현재 `EnemyAISystem`(enum+switch, Patrol/Chase 193줄)을 State 클래스(`PatrolState` / `ChaseState`)로 옮기되, **기존 행동(aggro 히스테리시스, 순찰 경계, Chase 대상 교체)은 비트 단위로 보존**한다 (순수 구조 이주, 행동 변경 없음). 여기서부터 State 클래스의 확장성 값어치가 실제로 발생한다(상태가 늘기 시작).

이 Phase가 끝나면: 몬스터가 동일 베이스 위에서 기존과 똑같이 순찰/추격한다.

---

## ⚠️ 실제 진행 = 스코프 확장 (Play 피드백, 2026-06-08)

> **정직 박제 (reviewer 지적 반영)**: 본 계획은 "순수 구조 이주만"으로 못박았으나, Play 실측 피드백을 반복하며 적 행동 전반으로 확장됐다. 아래는 *계획 대비 실제* 차이 — 상세 사실은 [`04-monster-ai-state-DONE.md`](04-monster-ai-state-DONE.md).

- **계획대로 지킨 것**: 제네릭 State 베이스 이주(Patrol/Chase) + 행동보존(전환 틱 같은-틱 이동 = desync 0) + Boss 불간섭 + AnimState↔EnemyState 분리.
- **계획 밖 추가(Play 피드백)**:
  - **적 HitState = *신규 기능***(AI 멈춤 + 넉백). ← *"순수 이주(행동 100% 보존)" 아님*. 옛 적은 피격 시 latch만, 넉백 없었음. reviewer가 false-promise로 짚어 본 줄로 정정.
  - **선공/후공**(EnemyStats.AggroOnSight: 슬라임=후공/골렘=선공) + aggro 6→4 튜닝(Normal PatrolRange 4→3).
  - **클라 거울**(피격 시 공격자 바라보기 + 죽음 VFX) — Phase 03 클라 트랙의 연장.
  - **죽음 연출**: 서버 DeathState 시도 → 0.8s 지연이 테스트 5개 깨짐 → 클라 코스메틱 VFX로 전환(헌법#1 유지).
- **여전히 마일스톤 밖(연기 확정)**: 공격이 타겟 근접에 결합된 구조(허공 스윙 + 서버 AABB 별도 판정) = 다음 phase/마일스톤. (memory `future-attack-decouple-swing-from-hitdetection`)

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
>
> **(2026-06-08 갱신)** 위 "commit window 밖" 제외는 *유지*됐으나(공격 중 추격 멈춤 미구현), **"순수 구조 이주만"은 Play 피드백으로 무효화**(HitState/넉백/선공후공 추가) → 위 ⚠️ 스코프 확장 참조.
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
- 2026-06-08: 완료 (세션26, **스코프 확장** — 제네릭 이주 + HitState/넉백 + 선공후공 + 클라거울). 커밋 `5a18242`(서버+Shared) / `8a67bcf`(클라) / `4018f2f`(봇). reviewer 🔴0/🟡0(7불변식), 풀 테스트 461/0/4skip, 봇 EnemyAiSmoke GREEN(Option A), Play 실측 ✅. 상세=`04-monster-ai-state-DONE.md`
