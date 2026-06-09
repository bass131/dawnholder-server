---
owner: youngho
milestone: M4.7
phase: 06
title: 회귀 + 마감 — 봇 신규 + xUnit 회귀 + 2클라 매트릭스 + PR + 5단계 보고
status: done
grade: 보통
risk: irreversible(PR)
estimated: 1~2h
domain: qa
---

# Phase 06: 회귀 + 마감

> 의존 = P2·P3·P4·P5 전부.

## 목표
v10 구조급(HP 동기화 + 공격 모델 정비) 전체 회귀 입증 + 마일스톤 마감.

## 작업
1. **봇 신규** (`99_Tools/headless-bot/Scenarios/`):
   - **HpSyncSmoke**: 보스룸 피격→S_PlayerHp 카운트 + 부활 후 currentHp==maxHp assert.
   - **RemoteAttackSmoke**: 봇 2개, A 공격→B가 S_PlayerAttack(attacker==A) 수신·A 미수신.
   - **WhiffSwingSmoke**: 타겟 없는 좌표서 공격→S_PlayerAttack 송신·데미지 0.
2. **회귀**: 클린빌드 0/0 + `dotnet test` green + 기존 봇 전수 PASS(전투/보스 봇 fresh 서버 단독 — 교차오염 회피).
3. **2클라 Play 매트릭스(수동)**: 허공 스윙 양방향 + 원격 투사체 + HP 사망/부활 동기화(직업 2종).
4. **마감**: `ProtocolVersion == 10` assert + CHANGELOG [M](v10 bump 재빌드 의무) + `_milestone-DONE.md` + `.html` 5단계 보고(대규모).
5. **PR**(사용자 GO).

## 완료 조건 (정량)
- [ ] 신규 봇 3종 green + 기존 봇 회귀 0
- [ ] 클린빌드 0/0 + dotnet test green + ProtocolVersion 10
- [ ] cross-review 🔴0
- [ ] 2클라 Play 매트릭스 통과
- [ ] `_milestone-DONE.md`+`.html` 5단계 보고 + CHANGELOG + PR 머지(사용자 GO)
