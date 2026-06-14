---
owner: youngho
milestone: M5
phase: 17
title: 일반몹(Normal/Golem) 공격 로직
status: pending
grade: 복잡
domain: server
estimated: 2~3h
---

# Phase 17: 일반몹(Normal/Golem) 공격 로직

> **상태**: pending
> **마일스톤**: M5
> **등급**: 복잡 (신규 AI State + 데미지 산출 신규 — 서버 전용). **trust-boundary 아님**: 일반몹 AI는 클라 소켓 입력을 받지 않는 *서버 자율 행위*(틱 루프 구동). "서버 민감 ≠ trust-boundary" (auditor 봉합 2026-06-14)
> **담당**: server (Sonnet 구현 + Opus 리뷰 — 데미지 공식 §1 민감도 때문에 리뷰는 유지)

---

## 🎯 목표

지금 적 중에서 **보스만 플레이어를 공격**하고, 일반몹(Normal/Golem)은 맞기만 하고 반격을 못 한다. 이 Phase가 끝나면 일반몹도 사거리 안에 플레이어가 들어오면 쿨다운마다 데미지를 주는 **공격 상태(AI)**를 갖는다. 데미지·히트 판정은 전부 서버에서 굴리고, 클라엔 **기존 `S_EnemyAttack`(ID20) 패킷을 재사용**해 알린다 (신규 패킷 불필요).

---

## ⏪ 사전 조건

- [ ] 없음 (독립 Phase). C 트랙은 파티/퀘스트(A/Q 트랙)와 파일이 겹치지 않아 병렬 안전.
- [ ] 참고: `BossStates.ApplyBossAttack`(L35~84) 패턴을 그대로 따른다 — 진입 전 실측 권장.

---

## 📝 작업 내용

- [ ] 신규 `EnemyAttackState` + `02_Server/GameServer/Maps/States/EnemyStates.cs`에 `ApplyAttack` 추가
  - `BossStates.ApplyBossAttack`(L35~84) 패턴을 따라 작성: 사거리 체크 → 쿨다운 체크 → 데미지 적용 → broadcast.
- [ ] `EnemyEntity.cs` — 공격 **쿨다운 카운터**(tick 단위) + **사거리** 필드 추가.
  - 쿨다운은 **남은 tick 수 카운트다운**으로 (틱 루프 blocking 금지 — `Task.Delay`/`Thread.Sleep` 절대 X).
- [ ] `98_Shared/GameData/CombatConstants.cs` — `NormalAttack*` 상수(데미지/사거리/쿨다운 tick) 추가.
- [ ] `Formulas.cs` — `EnemyStats`에 일반몹 공격 스탯(공격력 등) 반영.
- [ ] 데미지 적용 시 **기존 `S_EnemyAttack`(ID20)** broadcast (신규 패킷 X — `EnemyAttackHandler.cs:74`가 이미 이펙트로 결선).

---

## ✅ 완료 조건 (정량)

- [ ] Normal/Golem이 사거리 내 플레이어에게 데미지를 준다 (xUnit: 사거리 안 → HP 감소, 밖 → 변화 없음).
- [ ] 공격 시 **기존 `S_EnemyAttack`(ID20)** broadcast 발생 (신규 패킷 0개 — grep으로 PDL 무변경 확인).
- [ ] 무적 게이트 정합: 플레이어 `IsInvulnerable`(Dash 중 등)이면 데미지 무효 (xUnit).
- [ ] 쿨다운이 tick 카운트로 동작 — 1회 공격 후 쿨다운 동안 재공격 없음 (xUnit).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (기존 사망/전투 회귀 0).

---

## 🧪 테스트

**자동**:
- `EnemyAttackTests` (신규) — 사거리 in/out 데미지, 쿨다운 간격, 무적 게이트 무효화.
- 봇 스모크 — 일반몹 근처에서 플레이어 HP 감소 e2e (선택, R 트랙 PartyQuestSmoke가 40킬 도는 중 자연 커버).

**수동**: 영호 Play — 일반몹에 붙으면 반격당함 (Dash로 회피 가능).

---

## 📚 학습 포인트

- **패킷 재사용 = wire 비용 0** — 일반몹 공격이 보스와 같은 `S_EnemyAttack` 모양이면 신규 패킷·`ProtocolVersion` bump 없이 클라 이펙트 흐름을 그대로 탄다 (헌법 §2 "은퇴 ID 재사용 금지"는 *재사용 의도가 명확한 동일 의미* 패킷엔 해당 X — append-only 위반 아님).
- **틱 루프 쿨다운 = 카운트다운** — 서버 게임 루프(20 TPS)에서 시간 기반 쿨다운은 `Task.Delay`가 아니라 **남은 tick 정수 감소**로 구현 (헌법 §5 블로킹 금지).
- **무적 게이트 = 신뢰 경계의 한 축** — 데미지 적용 전 `IsInvulnerable` 체크는 서버가 단일 진실. 클라가 "나 무적이야"라고 우길 수 없음.

---

## ⚠️ 함정 / 주의사항

- **신규 패킷 만들지 말 것** — `S_EnemyAttack`(ID20) 재사용이 설계 결정. PDL을 건드리면 v15 bump 충돌 + 설계 결함 신호.
- 틱 안에서 `await Task.Delay`/`Thread.Sleep`/동기 DB 호출 절대 금지 (헌법 §5). 쿨다운은 tick 카운트.
- `BossStates.ApplyBossAttack`을 *복붙*하지 말고 패턴만 따른다 — Boss 면역/특수 분기는 일반몹에 불필요. 공통화 가능하면 헬퍼로 (단, 과한 추상화 X).
- 데미지 수식은 `98_Shared/GameData/`(`Formulas`/`CombatConstants`)에만 — `03_Client/`엔 절대 안 들어감 (헌법 §1).

---

## ➡️ 다음 Phase

- Phase 18 — 일반몹 피격 이펙트 wiring (클라가 `attackPattern` 값으로 slime/golem 이펙트 분기).

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `17-...-DONE.md` + HTML 시각화 (ADR-031). 데미지 공식 §1 민감 → reviewer 점검 권장(trust-boundary 깃발은 미해당).

---

## 작업 로그

- 2026-06-14: 생성.
