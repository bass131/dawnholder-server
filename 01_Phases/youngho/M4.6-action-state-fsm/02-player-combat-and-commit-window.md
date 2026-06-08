---
owner: youngho
milestone: M4.6
phase: 02
title: 플레이어 전투 상태 + commit window 규칙 + 공유 상수
status: pending
grade: 복잡
risk: trust-boundary
estimated: 3~4h
domain: server+shared
---

# Phase 02: 플레이어 전투 상태 + commit window 규칙 + 공유 상수

> **상태**: pending
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 복잡 + **trust-boundary 깃발** (이동 입력 게이트 = 신뢰 경계 변경 → reviewer 엄격 점검)
> **담당**: server + shared

---

## 🎯 목표

플레이어 **전투 상태(Attack / Hit / Death)**를 Phase 01 프레임워크 위로 이주하고, 이번 마일스톤의 **핵심 규칙**을 도입한다: **공격은 commit window 동안 이동을 잠근다**. 이 잠금은 **서버에서 강제**되며(헌법 #1), 잠금 지속(틱)은 **98_Shared 단일 상수**로 둬 클라가 같은 규칙으로 예측할 수 있게 한다(Phase 03 연결).

이 Phase가 끝나면: 플레이어가 공격 중에는 서버가 이동 입력을 무시한다. 조작된 클라로도 우회 불가.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (StateMachine + 이동 상태)

---

## 📝 작업 내용

- [ ] **선결 결정 1 (코드 첫 줄 전, plan-auditor 🟡 흡수)** — commit window 상수는 `98_Shared/GameData/`에 **신규 정의**(의미 = "공격 행동 잠금 지속"). 현 `AnimLatchTicks=8`(서버 전용 = 시각 latch)와 **별개 의미로 분리** — 같은 8틱 값이라도 시각 latch ≠ 행동 잠금이므로 합치지 않음 (drift 방지 우선)
- [ ] **선결 결정 2 (코드 첫 줄 전, plan-auditor 🟡 흡수)** — 이동 게이트 = **화이트리스트**: `AttackState`(commit window 진행 중)면 **이동 델타 0 강제 + 점프 입력도 무시**(전부 잠금). "공격 아니면 허용"이 아니라 "이동 가능 상태일 때만 허용" 방향 (부정 조건 분기 함정 회피)
- [ ] `AttackState` / `HitState` / `DeathState` 클래스화 — 기존 `AttackLatchTicks` / `HitLatchTicks` / `IsDeadAnimState` 의미를 State 지속(틱)으로 흡수
- [ ] **commit window 상수 신설** — 선결 결정 1대로 `98_Shared/GameData/`에 신규 상수
- [ ] **서버 이동 게이트** — `GameSession.SubmitMoveIntent()` (또는 `PlayerEntity` 입력 적용 지점)에서 선결 결정 2대로 이동 델타 0 강제
- [ ] State 기반 AnimState 산출 — Attack/Hit/Death가 State에서 나오게 (Phase 01 패턴 연장)
- [ ] tick 기반 통일 — ms rate-limit(500ms)와 tick latch 혼재 정리: 공격 쿨다운/지속을 어떤 축으로 통일할지 결정 후 박제 (carry-over: 두 타이밍 메커니즘 혼재)

---

## ✅ 완료 조건

- [ ] **공격 중 이동 잠금이 서버에서 강제** — 단위 테스트: AttackState 진행 중 이동 입력 주입 → 위치 변화 0 (commit window 종료 후 정상 이동)
- [ ] **우회 불가 입증** — 비정상 이동 입력(연속/대량)을 commit window 중 주입해도 서버 위치 불변
- [ ] commit window 지속이 **98_Shared 상수 단일 출처** — 서버가 그 상수를 읽어 게이트
- [ ] `dotnet test` green (신규 commit window 테스트 포함) + 기존 전투 테스트 회귀 0
- [ ] **ProtocolVersion 9 불변** (신규 패킷 없음 — 게이트는 서버 내부)
- [ ] reviewer 🔴 0 (trust-boundary 변경 = 범위 검증/소유권/우회 점검 필수)

---

## 🧪 테스트

**자동**:
- `CommitWindowTests` — 공격 중 이동 무시 / 종료 후 복귀 / 경계 틱(off-by-one)
- 기존 공격 rate-limit(500ms) 회귀 — commit window와 충돌 없는지

**수동**:
- WSL2 서버 + Play — 공격 누르면 그 자리에 "콱" 멈춰 끝까지 휘두르는 체감 확인 (직업 2종)

---

## 📚 학습 포인트

- **서버 권위 게임플레이 규칙 vs 시각 효과** — "이동 잠금"은 규칙(서버), Animator Exit Time은 거울(클라). 왜 클라에만 두면 핵 취약점인지
- **단일 진실 상수(single source of truth)** — 같은 규칙을 서버·클라가 따로 하드코딩하면 reconcile 어긋남. 공유 상수로 묶는 이유
- **신뢰 경계(trust boundary)** — 입력 게이트는 untrusted 입력 검증의 일종. 범위/우회를 테스트로 막기

---

## ⚠️ 함정 / 주의사항

- **부정 조건 분기 함정** (carry-over) — "공격 아니면 이동 허용" vs "이동 가능 상태일 때만 허용" — 새 상태 추가 시 어느 쪽이 안전한지. 화이트리스트 권장
- commit window를 클라 Animator Exit Time으로만 두는 함정 — 반드시 서버가 진실
- 잠금 지속과 애니 길이 어긋나면 "공격 끝났는데 못 움직임" 또는 반대 — 상수 단일화로 봉합 (carry-over: 애니 길이 vs 서버 latch)

---

## ➡️ 다음 Phase

- Phase 03 — 클라 미러 + 예측 게이트 (commit window 상수로 클라도 게이트 → rubber-band 방지)

---

## 📋 박제 (완료 후)

- 복잡 + trust-boundary → **-DONE.md** (규칙 결정/우회 입증 사실 박제 강조)

---

## 작업 로그

- 2026-06-08: 신설 (plan)
