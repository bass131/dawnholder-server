---
owner: youngho
milestone: M4.8
phase: 04
title: 서버 스킬 시스템 + 썬더볼트 AoE — C_SkillUse + 박스 스캔 + 광역 지연 데미지
status: done
grade: 복잡
risk: trust-boundary(쿨다운·박스 판정 서버 권위)
estimated: 3~4h
domain: server
---

# Phase 04: 서버 스킬 시스템 + 썬더볼트 AoE

> 의존 = P2(인프라)·P3(평타 헬퍼). 최소 스킬 시스템 + 썬더볼트 광역.

## 목표
평타와 분리된 스킬 발동(C_SkillUse)을 받아 쿨다운 검증 → 공격자 중심 X,Y 박스 스캔으로 타격 적 즉발 확정 → 각 적 광역 지연 데미지 + freeze(Normal만) + S_SkillCast 연출.

## 작업
1. **`C_SkillUseHandler.cs` 신설** (`02_Server/GameServer/Handlers/`) — decode + skillId 범위 검증(1=Thunderbolt만, 그 외 silent drop=cheat 후보) + session 캡슐화 메서드 호출. `HandlerRegistry`에 한 줄 등록.
2. **쿨다운(서버 권위)**: `PlayerEntity`에 스킬 쿨다운 필드(`LastSkillTickMs` 또는 스킬별 맵 — 썬더볼트 1개라 단일 필드로 시작). `ThunderboltCooldownTicks`(≈40) 미경과 시 silent drop(헌법 #3).
3. **`SkillSystem.cs` 신설**(또는 CombatSystem 확장) — 썬더볼트 처리:
   - `ResolveImpactTargets(map, casterOrigin, box)` = 공격자 중심 X,Y 박스(`ThunderboltBoxHalfX/HalfY`, facing 전방 우선) ∩ 살아있는 적 목록(P3 헬퍼의 범위 분기).
   - 각 적: `map.EnqueueDeferredDamage(new DeferredImpact{enemy, damage, impactTick=CurrentTick+LightningDelayTicks, hitEffect=2, caster})` + `enemy.FrozenUntilTick = CurrentTick + LightningDelayTicks` **단 Normal/Golem만**(Boss는 freeze 생략, 데미지만).
   - `S_SkillCast{caster, skillId, strikeDelayTicks=LightningDelayTicks, facing}` broadcast(목록 없음 — 캐스팅 연출).
   - 빈 박스(타격 적 0): S_SkillCast만(캐스팅 모션), deferred 0.
4. **`CombatConstants.cs` 썬더볼트 상수**: `ThunderboltBoxHalfX`(≈6.0f) / `ThunderboltBoxHalfY`(≈3.0f) / `LightningDelayTicks`(≈4) / `ThunderboltCooldownTicks`(≈40) / 데미지 베이스(단일 히트).
5. **단위 테스트** (handler happy/invalid skillId/auth 3종 + 시스템): 박스 ∩ 적 목록 정확(경계 적 포함/제외) · 각 적 낙뢰딜레이 후 데미지+S_HitResult(hitEffect=2) · Boss 데미지 적용·freeze 안 됨 · 쿨다운 중 재발동 silent drop · 빈 박스=S_SkillCast만.

## 완료 조건 (정량)
- [ ] `dotnet test` green (기존 회귀 0)
- [ ] C_SkillUse handler: happy 1 + invalid skillId 1 + auth(handshake 미완료) 1
- [ ] 박스 스캔: 박스 내 적 N개 전원 deferred enqueue, 박스 밖 0
- [ ] 각 적 도착틱: 데미지 + S_HitResult.hitEffect == 2
- [ ] Boss: 데미지 적용 O, FrozenUntilTick 세팅 X(면역) → 이동 유지
- [ ] 쿨다운(ThunderboltCooldownTicks) 미경과 재발동 silent drop
- [ ] 빈 박스: S_SkillCast 1회, deferred 0

## 주의
- trust-boundary: 쿨다운·박스 판정 서버 단독. 클라는 skillId + attackerClientTick만.
- AoE-ready: ResolveImpactTargets가 단일(P3)/박스(P4) 공용 → 미래 원형 등 모양만 추가.
- Boss freeze 면역은 enqueue 시점 Kind 분기(EnemyKind.Boss면 FrozenUntilTick 세팅 skip).
