---
owner: youngho
milestone: M4.8
phase: 05
title: 클라 서버확정 투사체 + 스킬 입력/연출 — 선예측 제거 + 핸들러 + 낙뢰 VFX
status: done
grade: 복잡
risk: unity-asset(VFX는 사용자/유현 placeholder)
estimated: 3~4h
domain: client
---

# Phase 05: 클라 서버확정 투사체 + 스킬 입력/연출

> 의존 = P1(프로토콜)·P3(S_ProjectileLaunch)·P4(C_SkillUse/S_SkillCast). 클라를 서버 확정 기반으로 전환 + 스킬 입력.

## 목표
로컬 선예측 투사체 스폰을 제거하고 서버 통보(S_ProjectileLaunch) 후 스폰으로 통일. 스킬 키 입력(C_SkillUse) + 썬더볼트 낙뢰 연출(S_SkillCast 캐스팅 + S_HitResult hitEffect 분기).

## 작업
1. **선예측 스폰 제거** (`MageRangedAttack.cs:22-55`): `ProjectileSpawner.Spawn` 제거, commit window 알림(`NotifyAttack`)은 유지. 평타 입력은 여전히 C_Attack 송신.
2. **`ProjectileLaunchHandler` 신설** (`ClientPacketHandlers.cs`, S_ProjectileLaunch) — 로컬/원격 공통: 캐스터 위치(Local=LocalPlayer, 원격=RemoteEntityRegistry) + 타겟 위치(EnemyRegistry.TryGetTransform, 0이면 facing 폴백) → `ProjectileVisual.Launch`. travelTicks로 비행 속도/lifetime 보정(도착 ≈ 서버 도착틱).
3. **`PlayerAttackHandler`에서 Mage(attackType==1) 투사체 스폰 제거** — 근접 스윙(attackType==0)만 유지(평타 투사체는 ProjectileLaunchHandler로 이관).
4. **스킬 키 입력** (`LocalPlayerInput.cs`): 스킬 키(임시 Q) → `C_SkillUse{skillId=Thunderbolt, attackerClientTick}` 송신 + 로컬 캐스팅 commit window(이동 잠금).
5. **`SkillCastHandler` 신설** (S_SkillCast) — 캐스터(Local/Remote) 위치에 캐스팅 모션/연출(placeholder VFX). 로컬 중복 가드(caster==LocalEntityId여도 캐스팅 모션은 재생, 데미지/낙뢰는 S_HitResult).
6. **`HitResultHandler` hitEffect 분기** — 0=기본/근접(기존), 1=투사체 도착 임팩트 VFX, 2=낙뢰 VFX(target 위치, EnemyRegistry로 해결). 데미지 텍스트/적 HP 갱신(`EnemyRegistry.ApplyHit`)은 공통.
7. **`UnityClientSession` dispatch 등록**: ProjectileLaunchHandler / SkillCastHandler.
8. **freeze 시각** = 적 position 정지로 자동(S_EntityState 갱신 안 됨). 별도 이펙트는 범위 밖.
9. **Unity 컴파일 검증** — MCP RunCommand(AssetDatabase.Refresh + CompilationPipeline.RequestScriptCompilation 완전한정명) + ReadConsole Error 0.

## 완료 조건 (정량)
- [ ] Unity 컴파일 0 error (MCP RunCommand + ReadConsole)
- [ ] 2클라 평타: 투사체 1발(선예측 중복 0)·도착 ≈ 서버 도착틱·적 정지(freeze)·도착 순간 HP 감소
- [ ] 사거리 밖 평타: 캐스팅 스윙만(투사체 X)
- [ ] 썬더볼트: 박스 내 적들 각자 위치 낙뢰 VFX + 동시 HP 감소, Normal 정지·Boss 안 멈춤
- [ ] 쿨다운 중 스킬 키 재입력: 발동 X(서버 silent drop)
- [ ] reconcile rubber-band 0 (캐스팅 commit window 자연 복구)

## 주의
- unity-asset 깃발: 새 prefab/VFX는 백업 의무(Phase 08 사고 학습). 썬더볼트/투사체 VFX는 **사용자·유현 placeholder** — AI는 wiring/코드만, 외관 에셋은 사용자 직접.
- 로컬도 S_ProjectileLaunch로 스폰(선예측 제거) → 로컬/원격 1발 일치.
