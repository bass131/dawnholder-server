---
owner: youngho
milestone: M4.8
phase: 02
title: 서버 인프라 — DeferredDamageSystem(지연 데미지 큐) + freeze(Boss 면역)
status: pending
grade: 복잡
risk: trust-boundary(지연 데미지/freeze 서버 권위)
estimated: 2~3h
domain: server
---

# Phase 02: 서버 인프라 (지연 데미지 + freeze)

> 의존 = P1(프로토콜). 평타(P3)·썬더볼트(P4)가 공유할 토대.

## 목표
"N틱 뒤 데미지 적용"(헌법 #5 논블로킹)과 "도착까지 적 이동 봉쇄"(freeze, Boss 면역)를 서버 권위로 신설.

## 작업
1. **`DeferredDamageSystem.cs` 신설** (`02_Server/GameServer/Maps/Systems/`) — `RespawnSystem`(RespawnSystem.cs:21-73) 패턴 그대로:
   - `struct DeferredImpact { int attackerEntityId; int targetEntityId; int damage; long impactTick; byte hitEffect; }`. **attackerEntityId 처음부터 포함** — S_HitResult가 attacker를 요구(PDL 줄157)하므로 필수(plan-auditor 우려 D: 미루면 P3 struct 재수정).
   - `List<DeferredImpact> _queue` + `Enqueue(DeferredImpact)`.
   - `Process(GameMap map, long tickNumber)` — 역방향 순회, `tickNumber >= impactTick`이면: 적 존재+`!IsDead` 재확인 → `target.Hp -= damage` → `S_HitResult{attacker(미상시 0 또는 caster), target, damage, currentHp, maxHp, hitEffect}` broadcast → HP≤0이면 기존 사망 경로(S_EntityDeath + Normal respawn enqueue). 죽었거나 사라졌으면 skip.
2. **`EnemyEntity.cs` `long FrozenUntilTick` 필드 신설** (tick thread invariant — Update 안에서만 R/W).
3. **`EnemyAISystem.Update`(EnemyAISystem.cs:16) 진입부 freeze 가드**: `if (enemy.FrozenUntilTick > 0) { if (tickNumber >= enemy.FrozenUntilTick) enemy.FrozenUntilTick = 0; else continue; }` — Fsm.Tick + latch 감소 스킵.
4. **`BossBehaviorSystem`엔 가드 추가 안 함** — Boss freeze 면역(데미지 지연만, telegraph→attack FSM 유지).
5. **`GameMap.cs`**: `DeferredDamageSystem` 필드 + `Tick`에 `_deferredDamageSystem.Process(this, tickNumber)` 호출(RespawnSystem 전) + `EnqueueDeferredDamage(DeferredImpact)` 노출(P3/P4가 호출).
6. **단위 테스트** (`GameServer.Tests/`): impactTick 카운트다운 → 도달 틱에 데미지 적용 + HP≤0 처리 / frozen Normal 적 이동 0(X 불변) / Boss는 FrozenUntilTick 세팅돼도 이동 유지(면역).

## 완료 조건 (정량)
- [ ] `dotnet test` green (기존 회귀 0)
- [ ] DeferredDamageSystem: impactTick 도달 전 데미지 0, 도달 틱에 정확히 1회 적용, HP≤0 시 사망 경로
- [ ] frozen Normal/Golem: FrozenUntilTick 동안 X 좌표 불변, 만료 후 이동 재개
- [ ] Boss: FrozenUntilTick 세팅돼도 BossBehaviorSystem 이동/telegraph 정상(면역)
- [ ] 도착 시 타겟 사망/디스폰: skip (예외 없음)

## 주의
- 헌법 #5: Process는 순수 tick 카운트다운(await/Sleep/Task.Run 0).
- freeze는 HitLatchTicks와 독립 필드 — 중첩 가능(frozen 중 다른 피격 데미지O 이동만 봉쇄).
- **FrozenUntilTick 세팅 규칙 = `max(기존, 신규)`** (plan-auditor 우려 B): 평타(freeze 길게)+썬더볼트(짧게) 중첩 시 더 늦은 만료 우선 → 데미지 도착 전 freeze 조기 해제 방지. P3·P4 enqueue 코드가 이 규칙 적용.
- S_HitResult의 attacker = `DeferredImpact.attackerEntityId`(위 struct에 확정 포함) 사용 — 0이 아닌 실제 시전자.
- (우려 C 약한 폴백) deferred 도착 시 attacker 디스폰 가능성은 현 마일스톤 범위 밖(맵 이동=M4.9) — 데미지는 target 기준이라 정상, attacker stale 시 VFX 출처만 폴백.
