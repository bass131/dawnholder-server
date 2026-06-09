---
owner: youngho
milestone: M4.8
phase: 03
title: 서버 평타 원거리 — Mage 사거리 분리 + 서버확정 투사체 + 지연 데미지
status: pending
grade: 복잡
risk: trust-boundary(명중 판정 서버 권위)
estimated: 2~3h
domain: server
---

# Phase 03: 서버 평타 원거리

> 의존 = P2(인프라). Mage 평타를 진짜 원거리로 — 서버 확정 후 발사 + 지연 데미지.

## 목표
Mage 평타(C_Attack)를 근접 AABB에서 분리해 더 긴 사거리로 즉발 명중 판정 → S_ProjectileLaunch + DeferredDamage + freeze. Knight는 기존 즉시 데미지 유지.

## 작업
1. **`CombatConstants.cs` 평타 상수**: `MageAttackHalfExtent`(≈4.0f) / `ProjectileSpeedPerTick`(≈2u/tick) / `MinTravelTicks`(2) / `MaxTravelTicks`(10). 서버 전용 주석(헌법 #1).
2. **`GetAttackHitbox`(CombatSystem.cs:140) class 분기**: Mage면 `MageAttackHalfExtent`, 그 외 기존 `AttackHalfExtent`. (origin은 rewind된 attacker 위치 유지.)
3. **`ProcessAttack`(CombatSystem.cs:36-133) Mage 분기** — rate-limit/rewind/EnterAttackState/S_PlayerAttack는 앞단 그대로:
   - AABB 명중(타겟 존재 + Intersects) **AND Mage**이면: `travelTicks = clamp(round(dist/ProjectileSpeedPerTick), Min, Max)` → `map.EnqueueDeferredDamage(new DeferredImpact{target, damage, impactTick=CurrentTick+travelTicks, hitEffect=1, attacker})` + `target.FrozenUntilTick = CurrentTick + travelTicks` + `S_ProjectileLaunch{attacker, target, projectileType=0, travelTicks}` broadcast.
   - **Knight(근접)**: 기존 즉시 `target.Hp -= damage` + `S_HitResult{..., hitEffect=0}` 경로 유지.
   - 명중 실패(타겟 없음/miss): 기존 `S_PlayerAttack` 캐스팅 스윙만(투사체 X, 데미지 0).
4. **`ResolveImpactTargets(map, origin, shape)` 헬퍼** 신설 — 단일(targetId 1개 반환) 형태. P4 박스 스캔이 같은 헬퍼 확장. (발동 시점 스캔 = 즉발 판정.)
5. **데미지 계산** = 기존 `Formulas.ComputeDamage(attacker.Stats, target.Stats, BaseDamage)` 재사용.
6. **단위 테스트**: Mage 명중→S_ProjectileLaunch 송신+deferred enqueue+FrozenUntilTick 세팅, 도착틱에 데미지+S_HitResult(hitEffect=1) / 사거리밖→S_PlayerAttack만·데미지0 / Knight→즉시 데미지(hitEffect=0) / rate-limit 스팸 차단 유지.

## 완료 조건 (정량)
- [ ] `dotnet test` green (기존 회귀 0)
- [ ] Mage 사거리 내 명중: S_ProjectileLaunch 1회 + deferred 1건 + freeze 세팅, 즉시 데미지 0(도착 전)
- [ ] travelTicks = clamp(거리/속도, 2, 10) 정확
- [ ] 도착틱: 데미지 적용 + S_HitResult.hitEffect == 1
- [ ] 사거리 밖 Mage: 투사체 0, S_PlayerAttack만, 데미지 0
- [ ] Knight: 기존 즉시 데미지 경로 불변(hitEffect=0)
- [ ] rate-limit(쿨다운) 스윙 시도 기준 유지

## 주의
- trust-boundary: 명중·travelTicks 서버 계산. 클라 targetEntityId는 힌트(0=허공).
- "발사 연출 ↔ 도착 데미지 분리" = M4.7 스윙↔명중 분리 정신 연장.
