---
owner: youngho
milestone: M4.7
phase: 05
title: 클라 공격 입력+예측+원격 연출 — 항상 스윙 + 로컬 Attack 예측 + 원격 투사체/스윙
status: pending
grade: 복잡
risk: unity-asset(VFX 시)
estimated: 3~4h
domain: client
---

# Phase 05: 클라 공격 입력+예측+원격 연출 (공격갈래)

> 상세 설계 = `_milestone-plan.md` "기둥 2 / 클라". 의존 = P1, P3.

## 목표
허공 스윙 허용(입력 시 항상 스윙 + 로컬 Attack 예측) + 원격 플레이어 공격(Mage 투사체/근접 스윙) 연출.

## 작업
1. **항상 스윙**: `LocalPlayerInput.cs:63` `if(TryAttack)` 게이트 제거(쿨다운만). `AttackIntent.cs:32~43` 타겟 없어도 송신(targetEntityId=0 sentinel) + 항상 true + 항상 `NotifyAttack()`.
2. **로컬 Attack 예측**: `LocalPlayerMotion.cs:41~51` commit window 동안 Attack animState 선예측(서버 확인 전). 서버도 유효 시도 시 Attack 진입 → 일치(보수적, rubber-band 0).
3. **원격 연출**: `PlayerAttackHandler` 신설 + 등록 — `attackerEntityId==LocalEntityId` 가드 후 `attackType==1` 원격 Mage 투사체 / `==0` 근접 스윙 VFX. 위치=원격 attacker `EffectAnchor.ResolvePosition`, 타겟=`EnemyRegistry.TryGetTransform(targetEntityId)`(0이면 facing 폴백). `ProjectileVisual.Launch` 재사용.
4. `RemoteEntityRegistry.TryGetTransform`(+선택 TryGetFacing) public 노출.
5. 투사체 스폰 헬퍼 추출(`MageRangedAttack.cs:55~62`) — 로컬/원격 공유.

## ⚠️ 점검
- 원격 근접 스윙 VFX = **기존 에셋 재사용 1순위**. 새 VFX 필요 시 unity-asset 깃발(백업 의무) + 외관은 사용자 영역 확인.
- 로컬 중복 회피: attacker 제외 broadcast(P3) + 클라 LocalEntityId 가드 2중.

## 완료 조건 (정량)
- [ ] Unity 컴파일 0 error
- [ ] **(수동 Play)** 허공 스윙 시 로컬 모션 즉시 재생 / 2클라 원격 Mage 투사체+Knight 스윙(허공 포함) 관측 / 로컬 중복 0
- [ ] **(수동 Play)** reconcile rubber-band 0 + 쿨다운 중 연타 시 로컬 Attack 모션 깜빡임 commit window 종료로 자연 복구(잔상 잠금 없음) (plan-auditor 🟡 흡수)
