---
owner: youngho
milestone: M5
phase: 06
title: HandleEnemyDeath killerEntityId 전파
status: pending
grade: 보통
domain: server
estimated: 1~2h
---

# Phase 06: HandleEnemyDeath killerEntityId 전파

> **상태**: pending
> **마일스톤**: M5 (트랙 Q — 퀘스트 + 보스 게이트)
> **등급**: 보통 (시그니처 변경 + 호출처 3곳 전파 — 가장 넓은 코드 표면)
> **담당**: server (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

`GameMap.HandleEnemyDeath`에 **누가 죽였는지(killerEntityId)**를 전파한다. 지금은 "적이 죽었다"만 알지 누가 막타를 쳤는지 모른다. 퀘스트 킬카운트(Q2)가 "이 킬을 누구 파티에 적립할지" 결정하려면 killer 정보가 필수다. 이번 Phase는 시그니처에 killerEntityId를 추가하고, 사망을 유발하는 3개 경로(평타 / 썬더볼트 AoE / 스킬)가 정확한 killer를 전달하게 결선한다.

> 토대 작업이다. 킬카운트 증가 로직 자체는 Q2(Phase 07). 여기선 "killer가 사망 처리까지 흘러가는 파이프"만 깐다.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (v15 — 빌드 토대). **단, 06은 순수 `GameMap` 시그니처 전파라 파티 코드(트랙 A) 의존 0 → 트랙 A와 병렬 가능**. 실제 PartyRegistry.OnKill *소비*는 Phase 07에서 처음 결합(그래서 07이 05+06 의존). (auditor 봉합 2026-06-14 — 야간 1순위 병렬성 확보)

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Maps/GameMap.cs:449` — `HandleEnemyDeath(EnemyEntity target)` 시그니처에 `int killerEntityId` 파라미터 추가.
- [ ] 호출처 **3곳**에서 killer 전파 (**실측 확정 2026-06-14 — auditor + 메인 grep, 진입 시 변수명 재실측**):
  - `02_Server/GameServer/Maps/Actions/MeleeAction.cs:107` (Knight 즉시 근접 평타) → 공격자(attacker) entityId
  - `02_Server/GameServer/Maps/Actions/DashAction.cs:62` (Dash 충돌 킬) → caster entityId
  - `02_Server/GameServer/Maps/Systems/DeferredDamageSystem.cs:79` (지연 데미지 — Mage 평타·썬더볼트 등 deferred 경유) → `impact.AttackerEntityId`
- [ ] ⚠️ 옛 plan의 "`CombatSystem`/`SkillSystem`"은 **stale** — 그 클래스들은 `HandleEnemyDeath`를 호출하지 않음(grep 0건). 위 3곳이 진실.
- [ ] 지연 경로(썬더볼트 등 AoE)의 killer = **caster**(시전자) — AoE로 여럿이 죽어도 막타 주체는 시전자 1명. `impact.AttackerEntityId`가 caster를 가리키는지 확인.

---

## ✅ 완료 조건

- [ ] xUnit: 3경로(MeleeAction 즉시 평타 / DashAction 충돌 / DeferredDamageSystem 지연) 각각에서 killerEntityId가 정확히 전달됨.
- [ ] xUnit: 지연 경로(썬더볼트 AoE)로 적 사망 시 killer = caster.
- [ ] 기존 사망 처리 회귀 0 (드랍/XP/사망 브로드캐스트 등 기존 동작 그대로).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).

---

## 🧪 테스트

**자동**:
- `HandleEnemyDeathKillerTests` — 3경로 killer 정확 전달 + 썬더볼트 AoE killer=caster + 기존 사망 회귀 0.

**수동**: 없음(순수 서버 데이터 흐름 — 헤드리스로 충분).

---

## 📚 학습 포인트

> 학부생 시각.

- **시그니처 변경의 ripple(파급)** — 메서드에 파라미터 하나 추가하면 *모든 호출처*를 고쳐야 한다. `HandleEnemyDeath`는 3곳에서 호출되므로 3곳 모두 killer를 넘기게 정정해야 빌드가 통과한다. "한 줄 바꿨는데 줄줄이 따라오는" 키스톤 변경의 전형. 컴파일러가 빠진 호출처를 잡아주는 게 정적 타입 언어의 장점.
- **막타(last hit) 귀속** — 누구의 킬로 칠지 정하는 규칙. MMO에서 흔한 함정이다(막타 vs 누적 데미지 vs 파티 공유). 우리는 막타 주체(`AttackerId`)를 killer로 본다. AoE는 한 번에 여럿을 죽여도 시전자 1명이 killer.
- **AttackerId가 어디서 오나** — `hitResult.AttackerId`는 데미지를 발생시킨 entity의 id. 평타·스킬·AoE 모두 이 필드를 채우므로 단일 소스에서 killer를 뽑을 수 있다. 데이터를 한 곳에 모아두면(SSOT) 전파가 단순해진다.

---

## ⚠️ 함정 / 주의사항

- **가장 넓은 코드 표면** — 호출처 3곳(MeleeAction:107 / DashAction:62 / DeferredDamageSystem:79 — 실측 확정). 하나라도 빠뜨리면 그 경로의 킬이 적립 안 됨(또는 빌드 실패). 트랙 Q 최대 리스크 — 진입 시 변수명 재실측으로 3곳 다 확인.
- **썬더볼트 AoE killer = caster** — AoE는 여러 적을 동시에 죽인다. killer를 "맞은 적"이나 다른 무엇으로 착각하면 안 됨. caster 1명. `AttackerId`가 caster를 가리키는지 검증.
- **기존 사망 동작 보존** — killer 추가는 *정보 전파*일 뿐. 드랍/XP/사망 이펙트 등 기존 사망 처리 로직은 1줄도 안 바뀜. 회귀 0이 완료 조건.
- **killer가 없을 수 있는 경우** — 환경 데미지/자살 등 killer 미상 경로가 있으면 안전 기본값(예: 0 또는 invalid id) 처리. Q2에서 "killer 없으면 적립 안 함"으로 받음.

---

## ➡️ 다음 Phase

- Phase 07 — 킬카운트 적립(파티 공유/솔로) + S_QuestUpdate 송신.

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message로 충분(-DONE.md 박지 않음). 시그니처 변경 + 3호출처는 commit message에 박음.

---

## 작업 로그

- 2026-06-14: 생성.
