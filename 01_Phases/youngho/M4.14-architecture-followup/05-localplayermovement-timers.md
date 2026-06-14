---
owner: youngho
milestone: M4.14
phase: 05-localplayermovement-timers
title: (선택) LocalPlayerMovement 타이머 상태 추출 — PlayerAbilityTimers
status: planned
grade: 보통
slug: 05-localplayermovement-timers
created: 2026-06-14
domains: [client]
prior_phases: [01-baseline-and-prep]
depends_on: []
risk_flags: []
optional: true
---

# M4.14 Phase 05 — (선택) LocalPlayerMovement 타이머 추출

> 계획서 = `_milestone-plan.md` Phase #5 (**선택**). 근거 = `_architecture-review` §3(발견 #2, 대부분 false positive). **EditMode 테스트가 값을 만들 때만** 진행, 아니면 명시적 skip + 사유 박음(§0.3).

---

## Context (왜)

`LocalPlayerMovement.cs`(**482줄**)는 §3.1을 이미 상당 부분 지킴: 예측 물리 본체는 `PlayerPredictor`(plain C#) 위임, 순수 함수 4개 static 추출 + EditMode 테스트됨(`IsMovementLocked:278`/`ResolveGatedInput:290`/`ShouldForceAdopt:306`/`ResolveClassMoveParams:473`). 482줄의 정체 = 주석 40%+ (실코드 ~250). 나머지 = §2.2 컨테이너(타이머 상태 + 서브스텝 루프 + Notify 콜백 + reconcile) = 한 호흡으로 읽어야 의미.

**유일하게 방어 가능한 추출** = 쿨다운/window 타이머 상태 → plain C# `PlayerAbilityTimers` (EditMode 테스트 부착 + 필드 수↓). "해야 한다"가 아니라 "원하면" = 선택.

---

## 설계 (착수 시 + 영호 재확인)

- 쿨다운 4 + commit window + hit-gate 타이머 상태를 `PlayerAbilityTimers`(plain C#)로 추출.
- EditMode 테스트 신규 (타이머 전이 단위 검증).
- 기존 static 순수함수 4개는 **손대지 않음**.

## 변경 대상 (선택 진행 시)

1. `03_Client/Assets/Scripts/Prediction/PlayerAbilityTimers.cs` (신설, plain C#).
2. `03_Client/Assets/Scripts/Prediction/LocalPlayerMovement.cs` — 타이머 필드 → `PlayerAbilityTimers` 위임.
3. `03_Client/Assets/Tests/EditMode/Prediction/PlayerAbilityTimersTests.cs` (신설).

---

## 완료 조건 / 게이트 (정량) — 또는 명시적 skip

- [ ] (진행 시) `PlayerAbilityTimers` plain C# 추출 + EditMode 테스트 신규 + 회귀 0.
- [ ] (진행 시) 거동 보존: 타이머 전이 시점·값 불변 (Play 실측 또는 EditMode 대조).
- [ ] (skip 시) **명시적 skip 사유 박음** — "EditMode 테스트가 값을 못 만들면 현 상태 유지(§0.3 과한 추상화 회피)".

**검증 흐름**: 시작 시점에 영호와 진행/skip 재확인 → (진행) client Worker(Sonnet) + EditMode 회귀 → (skip) 사유 1줄.

---

## 위험 / 헌법 게이트

- **§0.3 과한 추상화 금지**: 타이머 1건 외 분리는 "두 파일 동시에 열어야 이해" 트랩. 타이머만 추출, 나머지 현 상태 유지.
- **§1 서버 권위**: 클라 예측 타이머 = 시각 즉응. 권위는 서버 — reconcile 불변.
- **Unity 외관 무관**: 코드 추출만(prefab/scene 미접촉). prefab 건드릴 일 생기면 영호.

> Phase 06(마감)으로 진행.
