---
owner: youngho
milestone: M4.7
phase: 02
title: 서버 HP 송신 — GameMap.SendPlayerHp + 3트리거(진입/피격/부활)
status: in-progress
grade: 보통
risk: —
estimated: 2~3h
domain: server
---

# Phase 02: 서버 HP 송신 (HP갈래)

> 상세 설계 = `_milestone-plan.md` "변경점 요약 / 서버". 의존 = P1(S_PlayerHp 패킷).

## 목표
플레이어 HP가 변할 때만 권위 이벤트 `S_PlayerHp`를 송신 — 표시 미러(M4.5 임시) 제거의 **서버 본체**.

## 작업
1. `GameMap.SendPlayerHp(PlayerEntity p)` 신설 — `S_PlayerHp{entityId=p.EntityId, currentHp=Max(0,p.Hp), maxHp=p.MaxHp}`를 `p.Owner?.Send`로 **본인에게만 1:1**. 틱스레드 논블로킹(헌법 #5).
2. 트리거 3곳:
   - **진입**: `GameMap.AddPlayer`(초기 1회 — 진입 즉시 권위 HP로 HUD 초기화).
   - **피격**: `BossStates.cs:49` `player.Hp -= damage` 직후 `map.SendPlayerHp(player)`.
   - **부활**: `BossStates.cs:70~71` `Hp=MaxHp; Revive()` 직후 — **표시 미러 제거의 핵심**.
3. 테스트: `BossBehaviorTests`에 S_PlayerHp 송신 단언(피격=감소 HP / 부활=currentHp==maxHp). 송신 관측 인프라는 기존 S_EnemyAttack 관측 패턴 재사용.

## 완료 조건 (정량)
- [ ] `SendPlayerHp` 논블로킹 송신(`p.Owner?.Send`), 동기 DB/await/Thread.Sleep 0
- [ ] 3트리거 모두 배선(진입/피격/부활)
- [ ] `dotnet test` green (신규 S_PlayerHp 단언 포함)
- [ ] 빌드 0W/0E

## 범위 밖
회복 아이템/스킬(트리거 소스 없음), 원격/파티 HP 바(entityId 미래용), S_EnemyAttack 필드 정리.
