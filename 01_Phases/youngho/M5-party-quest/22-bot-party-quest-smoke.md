---
owner: youngho
milestone: M5
phase: 22
title: 봇 PartyQuestSmoke 시나리오
status: pending
grade: 보통
domain: qa
estimated: 1~2h
---

# Phase 22: 봇 PartyQuestSmoke 시나리오

> **상태**: pending
> **마일스톤**: M5
> **등급**: 보통
> **담당**: youngho (qa)

---

## 🎯 목표

헤드리스 봇으로 **파티 + 공유 퀘스트** 전체 흐름을 e2e 검증하는 `PartyQuestSmoke` 시나리오를 만든다. 봇2가 초대→수락으로 파티를 맺고, 공유 카운트로 Normal 40킬을 채워 `S_QuestUpdate count=40`을 받는지까지 무인으로 증명한다 (WSL2 fresh).

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (파티 핸들러 happy path — invite/respond/leave).
- [ ] Phase 05 완료 (파티 신뢰경계 + 정리).
- [ ] Phase 07 완료 (킬카운트 + `S_QuestUpdate` 송신).

---

## 📝 작업 내용

- [ ] 신규 `99_Tools/headless-bot/Scenarios/PartyQuestSmoke.cs` — 봇2 시나리오 작성.
- [ ] `BotSession.cs` — 파티 패킷 파싱 추가(`S_PartyUpdate`/`S_QuestUpdate`).
- [ ] `Program.cs` — dispatch 등록 (시나리오 1줄).
- [ ] 흐름: 봇A가 봇B 초대 → 봇B 수락 → 둘이 공유로 Normal 40킬 → `S_QuestUpdate count=40` 수신 검증.
- [ ] **disconnect-해산 e2e 1줄 추가** (auditor 🟡 봉합): 봇A·봇B 파티 결성 후 봇A가 끊김 → 봇B가 `S_PartyUpdate{partyId=0}`(해산 통보) 수신 검증. cross-map+disconnect race는 xUnit이 못 잡는 통합 race라 봇 e2e가 안전망. 비가역 v15 마일스톤이라 유령 파티는 production 디버깅 비용 큼.

---

## ✅ 완료 조건 (정량)

- [ ] 봇2 초대→수락→공유 Normal 40킬→`S_QuestUpdate count=40` e2e 통과 (WSL2 fresh).
- [ ] disconnect 시 해산 통보(`S_PartyUpdate{partyId=0}`) 봇B 수신 e2e 통과.
- [ ] `run_bot_fresh_recheck.sh` fresh 단독 재검 green (carry-over 판정).
- [ ] 기존 봇 시나리오 회귀 0.

---

## 🧪 테스트

**자동**: `PartyQuestSmoke` (WSL2 fresh) — 파티 결성 + 공유 40킬 + `S_QuestUpdate=40`.
**수동**: 없음 (헤드리스 자율).

---

## 📚 학습 포인트

- **봇 시나리오 = 살아있는 명세** — 파티/퀘스트의 "기대 거동"을 봇 코드로 박으면 회귀 시 즉시 잡힌다. 단위 테스트(xUnit)가 *부분*을 증명한다면 봇은 *연결된 전체 흐름*(초대→수락→킬→카운트)을 증명.
- **공유 카운트 검증** — 봇 둘이 나눠 죽여도 합산 40이 되는지가 핵심 (`PartyState.KillCount` 공유 invariant의 e2e 확인).

---

## ⚠️ 함정 / 주의사항

- **봇 연속 FAIL ≠ 회귀** — `run_bot_fresh_recheck.sh` fresh 단독 재검이 최종 판정 (carry-over로 한 번 실패해도 재검이 green이면 통과).
- 파티 패킷 파싱(`S_PartyUpdate`/`S_QuestUpdate`)을 `BotSession`에 추가해야 시나리오가 카운트를 읽을 수 있음 — 빠뜨리면 봇이 멈춤.
- WSL2 fresh 실행 (ADR-029) — Smart App Control 차단 회피.

---

## ➡️ 다음 Phase

- Phase 23 — 봇 BossGateSmoke 시나리오 (게이트 거부/통과).

---

## 📋 박제 (완료 후 -DONE.md)

- 보통 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
