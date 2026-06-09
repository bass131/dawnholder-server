---
owner: youngho
milestone: M4.8
phase: 06
title: 회귀 + 마감 — 봇 4종 + xUnit 회귀 + 2클라 매트릭스 + 5단계 보고 + PR
status: pending
grade: 보통
risk: irreversible(PR)
estimated: 2~3h
domain: qa
---

# Phase 06: 회귀 + 마감

> 의존 = P2~P5 전부.

## 목표
v11 원거리+스킬+썬더볼트 전체 회귀 입증 + 마일스톤 마감.

## 작업
1. **봇 신규** (`99_Tools/headless-bot/Scenarios/`):
   - **RangedHitSmoke**: Mage 사거리 내 타겟 공격 → S_ProjectileLaunch 수신 + travelTicks 후 S_HitResult(hitEffect=1) + 적 HP 감소 assert.
   - **FreezeSmoke**: 평타/썬더볼트 발사 후 적 position 정지 확인(Normal) + Boss는 정지 안 함(면역) assert.
   - **ThunderboltAoeSmoke**: 박스 내 다수 적 배치 → C_SkillUse → S_SkillCast 수신 + 각 적 S_HitResult(hitEffect=2) + Boss 멈춤X assert.
   - **RangedWhiffSmoke**: 사거리 밖 Mage 공격 → S_PlayerAttack만(S_ProjectileLaunch 없음) + 데미지 0 assert.
2. **회귀**: 클린빌드 0/0 + `dotnet test` green + 기존 봇 전수 PASS(전투/보스 봇 fresh 서버 단독 — 교차오염 회피). C_Attack/C_SkillUse는 attackerClientTick=최신 S_Snapshot.serverTick.
3. **2클라 Play 매트릭스(수동)**: 평타 원거리(사거리 안/밖) + 썬더볼트 박스 AoE(Normal 정지/Boss 면역) + 쿨다운 + 원격 상호 관측 + reconcile rubber-band 0.
4. **마감**: `ProtocolVersion == 11` assert + CHANGELOG[M](v11 bump 재빌드 의무) + `_milestone-DONE.md` + `.html` 5단계 보고(대규모).
5. **PR**(사용자 GO): PR1 = P1~P5(v11 정합), PR2 = P6(마감). admin 머지 예상(98_Shared→Shared.dll→CODEOWNERS).

## 완료 조건 (정량)
- [ ] 신규 봇 4종 green + 기존 봇 회귀 0
- [ ] 클린빌드 0/0 + dotnet test green + ProtocolVersion 11
- [ ] cross-review 🔴0
- [ ] 2클라 Play 매트릭스 통과
- [ ] `_milestone-DONE.md`+`.html` 5단계 보고 + CHANGELOG[M] + PR 머지(사용자 GO)
