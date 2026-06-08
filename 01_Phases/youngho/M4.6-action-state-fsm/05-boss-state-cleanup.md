---
owner: youngho
milestone: M4.6
phase: 05
title: 보스를 명시적 State로 정리 + P2 telegraph 상수 단일화
status: pending
grade: 복잡
estimated: 3~4h
domain: server+shared
---

# Phase 05: 보스를 명시적 State로 정리 + P2 telegraph 상수 단일화

> **상태**: pending
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 복잡 (server + shared — 보스 FSM 재구성, 회귀 민감)
> **담당**: server + shared

---

## 🎯 목표

현재 **필드 조건 분기**로 된 `BossBehaviorSystem`(208줄: IsPhase2 / Telegraph / Cooldown)을 **명시적 State**(`BossIdleState` / `BossTelegraphState` / `BossAttackState` + 페이즈 전환)로 정리한다. 이월 건인 **P2 0.5s telegraph 상수**를 98_Shared로 옮겨 P1/P2가 단일 출처에서 정합되게 한다. 보스 행동(페이즈 1/2, 예고, 쿨다운, 범위 데미지)은 **회귀 없이** 보존한다.

이 Phase가 끝나면: 보스가 읽기 쉬운 State 머신으로 동작하고, telegraph 타이밍이 한 곳에서 관리된다 = 셋(플레이어/몬스터/보스) 통일 구조 완성.

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (State 베이스가 AI actor에 적용 검증됨)

---

## 📝 작업 내용

- [ ] 보스 State 클래스화 — `BossIdleState`(쿨다운 대기) / `BossTelegraphState`(예고 카운트다운) / `BossAttackState`(데미지 판정 + 쿨다운 리셋)
- [ ] 페이즈 전환 — HP ≤ 50% 1회성 전이를 명시적 상태/플래그 정리 (idempotent 보존)
- [ ] **P2 telegraph 상수(0.5s/10틱)를 98_Shared로** — 현재 서버 상수에 흩어진 P1(16틱)/P2(10틱) 예고 지속을 단일 정의로. 동적 배율이면 P1 기준 + 배율로 P2 자동 산출 검토
- [ ] `ApplyBossAttack()` 범위 다인 데미지 = `BossAttackState` 내부로 캡슐화
- [ ] `GameMap.Tick()`의 BossBehaviorSystem 호출 지점 정합 (틱 순서 보존)
- [ ] **trust-boundary 인접** — 보스 데미지는 서버 권위 유지 (범위 내 플레이어만)

---

## ✅ 완료 조건

- [ ] 보스가 명시적 State로 동작 — 페이즈 1/2 + telegraph + 쿨다운 봇 시나리오(BossFightSmoke) 회귀 0
- [ ] P2 telegraph 상수가 **98_Shared 단일 출처** — 서버가 그 값을 읽음, 중복 정의 0
- [ ] HP 50% 페이즈 전환이 정확히 1회 (idempotent) — 단위 테스트
- [ ] 범위 내 플레이어만 데미지 (서버 권위) — 기존 `BossBehaviorTests` 회귀 0
- [ ] `dotnet test` green + **ProtocolVersion 9 불변**
- [ ] reviewer 🔴 0

---

## 🧪 테스트

**자동**:
- `BossStateTests` — Idle→Telegraph→Attack→Idle 순환, P1/P2 쿨다운·예고 틱, 페이즈 전환 1회
- 기존 `BossBehaviorTests` 전부 회귀

**수동**:
- WSL2 서버 + Play — 보스방 양방향 전투: 예고 모션 → 공격 → 페이즈 2 전환 체감 (M4.5 데모 루프 재현). ⚠️ 보스 봇 시나리오는 **서버당 1회**(무리스폰 carry-over)

---

## 📚 학습 포인트

- **필드 분기 → 명시적 State** — "지금 무슨 상태?"가 코드에 드러남. telegraph/cooldown이 상태로 명명
- **상수 단일화** — P1/P2가 한 출처에서 파생되면 밸런스 조정이 한 곳. 98_Shared가 양쪽 진실
- **idempotent 전이** — 1회성 페이즈 전환을 틱 루프에서 안전하게

---

## ⚠️ 함정 / 주의사항

- telegraph 진행 중 페이즈 전환 시 예고 유지 = 예고 공정성 의도 (M4.5 주석 정정 학습) — 정리하다 깨지 말 것
- 보스 봇 시나리오 무리스폰 → **서버당 1회** (carry-over). 재실행 시 서버 재기동
- telegraph 상수 98_Shared 이동 = 공유 코드 규율(헌법 #4) — 서버 컴파일 재확인, v9 불변 입증

---

## ➡️ 다음 Phase

- Phase 06 — 회귀 + 마감

---

## 📋 박제 (완료 후)

- 복잡 → **-DONE.md**

---

## 작업 로그

- 2026-06-08: 신설 (plan)
