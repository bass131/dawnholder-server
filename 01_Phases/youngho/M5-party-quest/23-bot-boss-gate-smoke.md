---
owner: youngho
milestone: M5
phase: 23
title: 봇 BossGateSmoke 시나리오
status: pending
grade: 보통
domain: qa
estimated: 1~2h
---

# Phase 23: 봇 BossGateSmoke 시나리오

> **상태**: pending
> **마일스톤**: M5
> **등급**: 보통
> **담당**: youngho (qa)

---

## 🎯 목표

헤드리스 봇으로 **보스 포탈 잠금 게이트**를 e2e 검증하는 `BossGateSmoke` 시나리오를 만든다. 40킬 미만에서 BossRoom 진입을 시도하면 거부(`S_PortalLocked`)당하고, 40킬을 채운 뒤엔 진입에 성공하는지 무인으로 증명한다 (WSL2 fresh).

---

## ⏪ 사전 조건

- [ ] Phase 08 완료 (보스 포탈 잠금 게이트 — `MapMigration` 검증 단계 + `S_PortalLocked`).
- [ ] Phase 22 완료 (`PartyQuestSmoke` — 봇 파티/킬 인프라 재사용).

---

## 📝 작업 내용

- [ ] 신규 `99_Tools/headless-bot/Scenarios/BossGateSmoke.cs` — 봇2 시나리오 작성.
- [ ] 흐름: 봇2가 <40 상태로 BossRoom 진입 시도 → `S_PortalLocked` 수신 + 맵 미전환 확인 → 40 채운 뒤 재시도 → BossRoom 진입 성공.
- [ ] (필요 시) `Program.cs` dispatch 등록.

---

## ✅ 완료 조건 (정량)

- [ ] 봇2 <40 BossRoom 진입 거부 확인 (`S_PortalLocked` 수신 + 맵 미전환).
- [ ] 40 채운 뒤 BossRoom 진입 성공 e2e (WSL2 fresh).
- [ ] `run_bot_fresh_recheck.sh` fresh 단독 재검 green.

---

## 🧪 테스트

**자동**: `BossGateSmoke` (WSL2 fresh) — 거부(S_PortalLocked) → 40킬 → 진입 성공.
**수동**: 없음 (헤드리스 자율).

---

## 📚 학습 포인트

- **게이트 거부의 e2e 증명 = 2가지 확인** — `S_PortalLocked` 패킷 수신만으론 부족하고, **맵이 실제로 안 바뀌었는지**(transfer 미발생)도 같이 봐야 ghost(전환은 됐는데 거부 메시지만 온) 버그를 잡는다.
- **음/양 케이스 페어** — "거부(<40)" + "통과(≥40)" 둘 다 봇이 확인해야 게이트가 양방향으로 옳음 (한쪽만 보면 항상 거부하는 버그도 통과로 보임).

---

## ⚠️ 함정 / 주의사항

- 게이트 거부 = **`S_PortalLocked` 수신 + 맵 미전환** 둘 다 확인 (RemovePlayer 전 차단 = ghost 미발생 증명).
- 봇 연속 FAIL ≠ 회귀 — fresh 재검이 판정.
- Phase 22 인프라(파티/킬 파싱) 재사용 — BossGate는 그 위에 게이트 검증만 얹음.

---

## ➡️ 다음 Phase

- Phase 24 — 전체 WSL2 회귀 + 마일스톤 마감.

---

## 📋 박제 (완료 후 -DONE.md)

- 보통 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
