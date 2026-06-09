---
owner: youngho
milestone: M4.7
phase: 03
title: 서버 공격 모델 정비 — 스윙↔명중 분리 + S_PlayerAttack broadcast
status: pending
grade: 복잡
risk: trust-boundary
estimated: 3~4h
domain: server
---

# Phase 03: 서버 공격 모델 정비 (공격갈래)

> 상세 설계 = `_milestone-plan.md` "기둥 2 / 서버". 의존 = P1. **trust-boundary** — 검증 순서 재배치.

## 목표
공격 스윙(연출)을 명중(데미지)에서 분리 — 허공 스윙 허용. 데미지 모델(단일 타겟 AABB)은 불변, 연출/이벤트만 명중에서 뗀다.

## 작업 — `CombatSystem.ProcessAttack`
1. target null/0 sentinel이어도 early-return 안 함. **rate-limit/rewind 검증 통과**(유효 시도) 시 `attacker.EnterAttackState()`를 **AABB 판정 앞으로 이동** → 명중 무관 진입.
2. `S_PlayerAttack`(except: attacker.Owner) broadcast = EnterAttackState 직후. `attackType = Class==Mage?1:0`, `facing = attacker 방향/target 방향`.
3. AABB 명중(`attackBox.Intersects(target.Hitbox)`)은 **데미지 + S_HitResult만** 게이트하도록 재배치. 타겟 없음/miss → 데미지 스킵, 스윙·이벤트 유지.
4. **rate-limit(500ms)은 스윙 시도 기준 유지** — 스팸 차단(헌법 #3).

## ⚠️ trust-boundary 점검 (plan-auditor 확인됨)
- EnterAttackState를 AABB 앞으로 옮겨도 rate-limit(46)·rewind(48~52) 검증이 `target`을 참조 안 해 약화 0.
- 데미지 판정(68~83)·AttackHandler attacker 강제(도용 방지) 불변.
- "연출만 분리, 권위 판정 불변" — 데미지 게이트(AABB) 변경 0.

## 완료 조건 (정량)
- [ ] `dotnet test`: 허공 스윙=Attack 진입 + S_PlayerAttack 송신 + **데미지 0** / 명중=데미지 + S_HitResult / rate-limit 스팸 차단 유지
- [ ] 봇: A 허공·명중 공격 시 B 수신 + A 미수신(except)
- [ ] 빌드 0W/0E · reviewer 🔴0

## 범위 밖
근접 AABB 스윕(AoE) — 단일 타겟 데미지 유지, 스윙 연출만 분리.
