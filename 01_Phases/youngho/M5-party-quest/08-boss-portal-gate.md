---
owner: youngho
milestone: M5
phase: 08
title: 보스 포탈 잠금 게이트 (40킬 미달 진입 거부)
status: pending
grade: 보통
risk: trust-boundary
domain: server
estimated: 1~2h
---

# Phase 08: 보스 포탈 잠금 게이트 (40킬 미달 진입 거부)

> **상태**: pending
> **마일스톤**: M5 (트랙 Q — 퀘스트 + 보스 게이트)
> **등급**: 보통 + **trust-boundary**(맵 전환 검증 게이트) → Sonnet 구현 + Opus 리뷰(routing)
> **담당**: server (Sonnet Worker — 메인 file:line 게이트 + reviewer 자동)

---

## 🎯 목표

보스방(BossRoom) 진입을 **킬카운트로 잠근다**. 맵 전환 검증 단계에서 `Dest == BossRoom && killCount < 40`이면 진입을 거부하고 `S_PortalLocked{requiredCount, currentCount}`를 보내며, 40 이상이면 통과시킨다. 핵심은 **검증을 RemovePlayer(transfer) *전*에** 두는 것 — 후면 플레이어가 원래 맵에서 빠진 채 보스방에도 못 들어간 *유령(ghost)*이 된다.

> Q 트랙의 결승선이다. 퀘스트(40킬)가 실제 게임플레이 게이트로 작동하는 지점. trust-boundary인 이유는 맵 전환이 클라 요청으로 시작되고, 게이트를 잘못 짜면 ghost나 무단 진입이 나기 때문.

---

## ⏪ 사전 조건

- [ ] Phase 07 완료 (killCount 적립 + `BossUnlockKillCount=40` 상수).
- [ ] (권장) Phase 09(B1 양방향 포탈 데이터) 선행 — 양방향 테이블 보고 게이트 작성하면 더 안전. 플랜상 Q3를 B1 뒤 배치 권장(다른 파일이라 병렬 안전하나 순서 권장).

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Network/MapMigration.cs` — 맵 전환 검증 단계(대략 L48~76, `Execute`의 transfer 직전)에 게이트 추가:
  - `Dest == BossRoom && killCount < BossUnlockKillCount(40)` → `S_PortalLocked{requiredCount=40, currentCount=killCount}` 송신 + **`return`**(transfer 중단).
  - 게이트 통과(≥40) 시 기존 transfer 로직 그대로.
  - killCount 조회 — 파티면 `PartyState.KillCount`, 솔로면 `SoloProgress[entityId]`(07의 적립처).
- [ ] `02_Server/GameServer/Network/GameSession.cs` — `S_PortalLocked` 송신 진입점(필요 시).
- [ ] **검증은 반드시 `RemovePlayer`(원래 맵에서 제거) 전에.** 게이트 실패 시 원래 맵에 그대로 머물러야 함(ghost 방지).

---

## ✅ 완료 조건

- [ ] xUnit: killCount<40으로 BossRoom 진입 시도 → 거부 + `S_PortalLocked{required=40, current=N}`.
- [ ] xUnit: killCount≥40 → BossRoom 진입 성공.
- [ ] xUnit: 게이트 실패 시 플레이어가 원래 맵에 남음(ghost 미발생 — 원래 맵에서 안 빠짐).
- [ ] 봇: `<40 거부 → 40 채운 뒤 진입 성공` e2e(R2에서 BossGateSmoke로 정식화, 이번엔 단위 검증).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).
- [ ] reviewer 헌법 hard 위반 0 (trust-boundary = reviewer 자동 호출).

---

## 🧪 테스트

**자동**:
- `BossPortalGateTests` — <40 거부 + S_PortalLocked(required/current) / ≥40 통과 / ghost 미발생(RemovePlayer 전 차단).

**수동**: 아침 영호 Play — 40킬 전 보스 포탈 진입 시도 시 토스트(P5) + 못 들어감, 40 후 진입.

---

## 📚 학습 포인트

> 학부생 시각.

- **검증 순서 = 안전의 핵심** — 맵 전환은 보통 (1) 원래 맵에서 제거 → (2) 새 맵에 추가 두 단계다. 게이트를 (1)과 (2) *사이*에 두면, 거부 시 플레이어가 어느 맵에도 없는 ghost가 된다. 반드시 (1) *전*에 검증하고, 실패면 `return`으로 원래 맵에 그대로 둔다. "비가역 작업 전에 검증한다"는 일반 원칙.
- **early return으로 안전 차단** — 조건 불충족 시 `return`으로 즉시 빠져나오면, 뒤따르는 transfer 로직이 아예 실행되지 않는다. 중첩 if보다 명확하고 ghost 위험이 0. 가드 절(guard clause) 패턴.
- **서버 권위 게이트** (헌법 §1) — "40킬 모았나"는 서버만 안다. 클라가 "나 40킬 했어"라고 주장해도 서버가 자기 카운트로 판정한다. 클라의 진입 요청은 untrusted, 게이트가 신뢰 경계.

---

## ⚠️ 함정 / 주의사항

- **게이트는 RemovePlayer(transfer) 전 필수** — 가장 큰 함정. transfer 후 검증하면 원래 맵에서 이미 빠진 ghost가 된다. `return`으로 transfer 자체를 막아야 안전.
- **trust-boundary** — MapMigration 검증 변경 = 위험 깃발. Sonnet 구현 + Opus reviewer 자동. 무단 진입(<40인데 통과)·ghost 둘 다 차단 검증.
- **killCount 조회처 정합** — 파티/솔로 양쪽 진행처(07)에서 정확히 읽어야 함. 파티원이면 공유 카운트, 솔로면 dict. 잘못된 카운트 읽으면 게이트 오작동.
- **양방향 포탈과의 관계** — Boss→HG 역방향(B1)은 게이트 무관(나갈 땐 자유). 게이트는 Dest==BossRoom 진입 방향만. 역방향에 게이트 걸지 말 것.

---

## ➡️ 다음 Phase

- Phase 09 — B1 양방향 포탈 데이터(트랙 B 시작) / 또는 R1 봇 PartyQuestSmoke(트랙 R). 플랜 의존 그래프상 헤드리스 1순위 블록은 R1·R2로 수렴.

---

## 📋 박제 (완료 후)

- 보통+trust-boundary → work-pin + commit message + reviewer 요약. ghost 방지 검증 순서를 commit message에 박음. 마일스톤 `-DONE.md`(R3)에서 트랙 Q 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
