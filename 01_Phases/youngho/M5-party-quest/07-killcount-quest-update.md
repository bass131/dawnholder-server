---
owner: youngho
milestone: M5
phase: 07
title: 킬카운트 적립(파티 공유/솔로) + S_QuestUpdate
status: pending
grade: 보통
domain: server
estimated: 1~2h
---

# Phase 07: 킬카운트 적립(파티 공유/솔로) + S_QuestUpdate

> **상태**: pending
> **마일스톤**: M5 (트랙 Q — 퀘스트 + 보스 게이트)
> **등급**: 보통 (PartyRegistry OnKill 훅 + 솔로 진행 dict + 패킷 송신)
> **담당**: server (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

06이 깐 killer 파이프를 받아, **킬카운트를 적립**하고 진행도를 `S_QuestUpdate`로 클라에 알린다. killer가 파티원이면 **파티 공유 카운트**(`PartyState.KillCount`)에, 솔로면 **`SoloProgress` dict**에 적립한다. 보스 해금 목표값(`BossUnlockKillCount=40`)은 서버 상수로 두고, 그 목표를 패킷의 `targetCount`로 함께 보낸다(클라 하드코딩 금지).

> 파티 공유가 핵심이다 — 파티원 2명이 각각 20마리씩 잡으면 합산 40으로 게이트가 열린다. 첫 협동 보상 메커니즘.

---

## ⏪ 사전 조건

- [ ] Phase 06 완료 (`HandleEnemyDeath`가 killerEntityId 전파).

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Party/PartyRegistry.cs`:
  - `OnKill(int killerEntityId)` 훅 — killer가 파티 보유 시 `PartyState.KillCount++`(공유), 아니면 `SoloProgress[killerEntityId]++`.
  - `SoloProgress` (`Dictionary<int,int>`) — 파티 없는 entityId의 개인 진행.
  - 적립 후 진행도를 멤버(들)에게 `S_QuestUpdate{currentCount, targetCount}` 송신(`SendToEntity` 경유, 파티면 양 멤버).
- [ ] 신규 `98_Shared/GameData/QuestConstants.cs`(또는 적절한 GameData 위치) — `BossUnlockKillCount = 40`. 서버·클라 공유 상수(SSOT).
- [ ] `targetCount`는 `BossUnlockKillCount` 서버값을 패킷에 실어 보냄(클라가 40을 하드코딩하지 않음).
- [ ] 리셋 지점 — StageClear 시 카운트 리셋. **맵 재진입 시에는 리셋 X**(게이트 동선 보존 — 헌팅장 나갔다 들어와도 진행 유지).
- [ ] **파티 결성/해산 시 솔로↔공유 전환 (야간 기본값 — 아침 영호 확인)**: 결성 시 `PartyState.KillCount`를 **0부터 시작**(리더 솔로값 승계 X). 해산 시 공유 카운트 **소멸**, 남은 멤버는 **0부터 솔로**. 가장 단순한 MVP — 야간 자율은 이 기본값으로 진행하고 아침에 영호가 "승계 원하면" 조정. (auditor 🟡 봉합 — 설계 분기 야간 기본값 명시로 stall 방지)

---

## ✅ 완료 조건

- [ ] xUnit: 파티 2명이 각각 적립 → 합산이 `PartyState.KillCount`에 누적, 40 도달 시 `S_QuestUpdate{currentCount=40, targetCount=40}`.
- [ ] xUnit: 솔로 killer → `SoloProgress[entityId]` 증가, 본인에게 S_QuestUpdate.
- [ ] xUnit: StageClear 시 카운트 리셋(공유·솔로 둘 다).
- [ ] xUnit: 맵 재진입 시 카운트 보존(리셋 안 됨).
- [ ] `targetCount`가 서버 `BossUnlockKillCount`(40)에서 옴.
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).

---

## 🧪 테스트

**자동**:
- `QuestKillCountTests` — 파티 합산 40 → S_QuestUpdate / 솔로 추적 / StageClear 리셋 / 맵 재진입 보존 / targetCount 서버값.

**수동**: 없음(카운터 정확성은 헤드리스로 망라. 봇 e2e는 R1).

---

## 📚 학습 포인트

> 학부생 시각.

- **공유 상태 vs 개인 상태** — 파티 킬은 *공유*(둘이 합쳐 40), 솔로 킬은 *개인*. 같은 "킬카운트"지만 귀속 대상이 다르다. 파티면 `PartyState.KillCount`(파티 1개당 1카운터), 솔로면 `SoloProgress` dict(entityId당 1카운터). 상태를 누구 소유로 둘지가 설계 결정.
- **SSOT(Single Source of Truth)와 하드코딩 금지** — 목표값 40을 클라가 자체적으로 들고 있으면, 서버가 40→50으로 바꿔도 클라는 모른다(불일치). 서버가 `targetCount`를 패킷에 실어 보내면 클라는 "서버가 말한 목표"만 표시한다. 헌법 §1 서버 권위의 작은 실천 — 클라는 렌더러일 뿐.
- **리셋 정책이 동선을 만든다** — "맵 재진입 시 리셋 X"는 의도된 설계다. 헌팅장에서 40킬을 모은 뒤 마을 들렀다 와도 진행이 유지돼야 보스 게이트로 가는 동선이 자연스럽다. 리셋 타이밍(StageClear만)이 게임플레이 흐름을 결정한다.

---

## ⚠️ 함정 / 주의사항

- **솔로 = 파티 없는 entityId** — `SoloProgress` dict로 추적. 공유↔솔로 전환 = **야간 기본값 박힘**(결성=0부터, 해산=공유 소멸·솔로 0부터). 아침 영호 확인 후 승계 정책 조정 가능 (작업 내용 참조).
- **맵 재진입 시 리셋 금지** — 게이트 동선 보존. StageClear에서만 리셋. 맵 이동에 카운트를 묶으면 동선이 끊긴다.
- **targetCount 서버값 강제** — 클라가 40을 하드코딩하면 SSOT 위반. 항상 `BossUnlockKillCount` 상수를 패킷으로 전달.
- **틱 루프 안전** — OnKill 적립은 PartyRegistry actor 안에서 직렬 처리(06 killer가 EnqueueJob 경유로 도달). 직접 dict를 cross-thread로 만지지 말 것.

---

## ➡️ 다음 Phase

- Phase 08 — 보스 포탈 잠금 게이트(killCount<40 진입 거부).

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message로 충분(-DONE.md 박지 않음). 리셋 정책·targetCount SSOT는 commit message에 박음.

---

## 작업 로그

- 2026-06-14: 생성.
