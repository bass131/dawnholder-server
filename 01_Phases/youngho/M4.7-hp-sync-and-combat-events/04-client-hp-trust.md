---
owner: youngho
milestone: M4.7
phase: 04
title: 클라 HP 신뢰 경로 — PlayerHpHandler + 표시 미러 제거
status: in-progress
grade: 보통
risk: —
estimated: 2~3h
domain: client
---

# Phase 04: 클라 HP 신뢰 경로 (HP갈래)

> 상세 설계 = `_milestone-plan.md` "변경점 요약 / 클라". 의존 = P1, P2.

## 목표
서버 권위 `S_PlayerHp`를 신뢰해 HUD 갱신 + M4.5 표시 미러(PlayerStats.MaxHp 추측) 제거.

## 작업
1. `PlayerHpHandler` 신설(`ClientPacketHandlers.cs`) + `UnityClientSession.cs` dispatch 등록 — `entityId==LocalEntityId`면 `HudController.UpdateHP(currentHp, maxHp)`(MainThreadDispatcher).
2. 표시 미러 제거(`ClientPacketHandlers.cs` EnemyAttackHandler L394~428):
   - 본인 피격 HUD 갱신(L394~402) 제거 — HP 권위는 S_PlayerHp로 이관. **이펙트/피격 플래시/hit-bridge는 유지**.
   - 사망 `PlayRespawnFade` HUD 복구 콜백 제거(페이드 연출 유지). 부활 HP는 S_PlayerHp가 통지.
   - "표시 미러…v10 전 임시" 주석 삭제.
3. `HudController.Start` PlayerStats.MaxHp full 초기화 → 첫 S_PlayerHp 전 placeholder로 유지, 주석 정정.

## 완료 조건 (정량)
- [ ] Unity 컴파일 0 error (메인 세션 Unity MCP 검증)
- [ ] **(수동 Play — 봇 자동화 불가 영역)** 2클라 사망→부활 시 HUD가 서버 권위 full HP(M4.5 "0 고착" 봉합), 표시 미러 콜백 0 (plan-auditor 🟡 흡수)

## 범위 밖
원격/파티 HP 바(entityId 미래용, LocalEntityId만), 공격 이벤트(P5).
