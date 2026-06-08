---
owner: youngho
milestone: M4.6
phase: 03
title: 클라 미러 정합 — 예측 게이트(reconcile 방지) + 시각 상태 검증
status: pending
grade: 복잡
risk: unity-asset
estimated: 2~3h
domain: client
---

# Phase 03: 클라 미러 정합 — 예측 게이트 + 시각 상태 검증

> **상태**: pending
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 복잡 (client — 예측/reconcile 로직 + Animator 검증)
> **담당**: client (Unity Play 검증 시 메인 세션/unity-bridge)

---

## 🎯 목표

플레이어 슬라이스를 **완성**한다. 서버가 commit window 동안 이동을 잠그면, **클라 예측도 같은 상수로 이동을 게이트**해야 reconcile rubber-band(서버가 위치를 되돌려 튕기는 현상)가 안 생긴다. 클라의 시각 FSM(Animator)이 서버 AnimState를 **깔끔히 거울**하는지 검증한다.

이 Phase가 끝나면: 공격 중 클라가 안 움직이고(예측), 서버와 어긋나 튕기지도 않으며, Attack/Hit/Death 애니가 정확히 재생된다 = 플레이어 ActionState 수직 슬라이스 완료.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (서버 commit window + 98_Shared 상수)

---

## 📝 작업 내용

- [ ] 클라 예측(`LocalPlayerController` 이동 예측)에서 **공격 중이면 이동 예측을 중단** — 98_Shared commit window 상수 + 로컬 공격 시작 시점으로 역산
- [ ] `LocalPlayerMotion`(hybrid) 정합 — Attack/Hit/Death = 서버 AnimState 우선 로직이 commit window와 시각적으로 일치하는지
- [ ] reconcile 경로 점검 — 공격 시작~종료 구간 S_Snapshot 위치와 예측 위치 오차가 임계 이하(rubber-band 없음)
- [ ] (필요 시) Animator 전이/Exit Time을 commit window 지속과 시각 정합 — **단 권위는 서버**, Exit Time은 거울만 (unity-asset: Animator controller 편집 시 백업 의무)

---

## ✅ 완료 조건

- [ ] **공격 중 reconcile rubber-band 0** — Play 실측 + (가능하면) 예측-서버 위치 오차 로깅으로 임계 이하 확인
- [ ] Attack/Hit/Death 애니가 서버 상태와 동기 — 시각 누락/지연 없음 (carry-over: 애니 길이 vs 서버 latch 어긋남 0)
- [ ] 원격 플레이어도 공격 중 미끄러지지 않음 (RemotePlayerMotion = 서버 신뢰)
- [ ] EditMode 테스트 green (해당 시) + 기존 클라 테스트 회귀 0
- [ ] reviewer 🔴 0

---

## 🧪 테스트

**자동**:
- 예측 게이트 순수 함수 테스트 (있으면) — 공격 시작 후 N틱 이동 예측 0

**수동**:
- WSL2 서버 + Play — 공격 연타 중 캐릭터가 제자리 + 튕김 없음 (직업 2종 × 두 씬)
- 2클라 — 원격 측에서도 공격 중 안 미끄러짐

---

## 📚 학습 포인트

- **클라 예측 + reconciliation** — 예측이 서버 규칙과 어긋나면 튕김. 규칙을 공유 상수로 양쪽에 심어 일치시키는 패턴
- **hybrid 애니** — 무엇은 서버 신뢰(전투), 무엇은 로컬 예측(이동)인지 경계
- **거울의 의미** — Animator Exit Time은 시각 타이밍일 뿐, 게임 규칙 아님

---

## ⚠️ 함정 / 주의사항

- 클라에서 commit window 지속을 **하드코딩 재입력**하는 함정 — 반드시 98_Shared 상수 참조 (drift = 튕김의 원인)
- Animator controller/prefab 편집 = unity-asset 깃발 — **편집 전 백업 의무** (Phase 08 사고 학습)
- 예측 게이트를 너무 세게 걸어 입력 손실 — 잠금 종료 직후 첫 입력이 살아야 함 (jump buffer 정신)

---

## ➡️ 다음 Phase

- Phase 04 — 몬스터 AI를 검증된 State 베이스로 (03과 병렬 가능)

---

## 📋 박제 (완료 후)

- 복잡 → **-DONE.md**

---

## 작업 로그

- 2026-06-08: 신설 (plan)
