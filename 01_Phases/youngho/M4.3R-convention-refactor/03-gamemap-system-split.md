---
owner: youngho
milestone: M4.3R
phase: 03
title: GameMap System 분리 (CombatSystem / EnemyAISystem / RespawnSystem)
status: done
grade: 복잡
domain: server
estimated: 3~4h
---

# Phase 03: GameMap System 분리 (rank 2)

> **상태**: pending
> **마일스톤**: M4.3R
> **등급**: 복잡 (665줄 4도메인 God class — 부록 A 핵심)
> **담당**: server SubAgent

---

## 🎯 목표

`GameMap`(665줄)이 동시에 쥔 4개 도메인을 §2.2 **컨테이너 + System** 구조로 분리한다. 컨테이너(`GameMap`)는 **상태 + tick 엔진 + actor 경계**만 남기고, 전투/AI/respawn 로직을 3개 System으로 추출한다. **동작 완전 보존** — `ProcessAttack`/`UpdateEnemies`/`ProcessRespawns` 로직을 *옮기기만*, 판정 결과는 한 틱도 달라지지 않는다.

---

## ⏪ 사전 조건

- [ ] Phase 01 베이스라인 (회귀 비교 기준) — 권장
- [x] Phase 07(M4.3) enemy AI 서버 완료 (UpdateEnemies/ProcessRespawns 존재)
- [ ] 없음 — 독립 추출 (Phase 02와 병렬 가능, 도메인 다름)

---

## 📝 작업 내용

- [ ] **CombatSystem** 추출 — `ProcessAttack(GameMap map, int attackerId, int targetId, long clientTick)` + static `GetAttackHitbox`(L382). target.Hp 변경·BroadcastToAll은 map을 인자로 받아 호출
- [ ] **EnemyAISystem** 추출 — `Update(GameMap map, long tick)` ← `UpdateEnemies` 본문(aggro 탐색·Patrol↔Chase 전이·히스테리시스·이동·S_EntityState broadcast). `_players`/`_enemies`는 기존 `Players`/`Enemies` 프로퍼티로 읽기 접근
- [ ] **RespawnSystem** 추출 — `Process(GameMap map, long tick)` ← `ProcessRespawns` + `_respawnQueue` + `NormalEnemyRespawnTicks`
- [ ] `GameMap.Tick`은 **System 호출 순서 명문화**만: physics → CombatSystem(큐된 attack job 경유) → EnemyAISystem → RespawnSystem
- [ ] `SubmitAttack`의 `EnqueueJob` 람다가 `map.ProcessAttack` 대신 `combatSystem.ProcessAttack(map, ...)`을 부르도록 조정
- [ ] System이 `_enemies`/`_respawnQueue`를 변경하려면 **internal mutator** 노출 — **최소 surface**로 (§0.3)

### ⚠️ 분리 금지 / 보존 (§0.3)
- [ ] invariant 주석("살아있는 적만 `_enemies`")을 **컨테이너 1곳**에 박아 "컨테이너+3System 4파일 다 열어야 이해" 함정 회피
- [ ] 컨테이너 상태(`_players`/`_enemies`/`_pendingJobs`/`AllocId`/`AddPlayer`/`RemovePlayer`/`SpawnEnemy`) = actor 경계라 잔류

---

## ✅ 완료 조건

- [ ] `GameMap.cs` < 600줄 (size-guard 경고 해소)
- [ ] CombatSystem/EnemyAISystem/RespawnSystem 3 클래스 분리, 각 단일 도메인
- [ ] `dotnet build Dawnholder.slnx` green (경고/오류 0)
- [ ] **동작 보존**: `dotnet test --no-incremental` 회귀 0 — 기존 `EnemyAiTests`(12)/`AttackHandlerTests`/`BossStageClearTests`/`LagCompensationTests` 전부 통과 (Phase 01 baseline 카운트 유지)
- [ ] 헤드리스 봇 `EnemyAiSmoke`/`BossStageClearSmoke` 통과
- [ ] tick System 호출 순서 명문화 (physics→Combat→AI→Respawn)
- [ ] reviewer 헌법 hard 위반 0 (§2.2 컨테이너+System + §1.1 tick thread + §0.3 = 축6)

---

## 🧪 테스트

**자동**: 기존 EnemyAiTests/AttackHandlerTests/BossStageClearTests/LagCompensationTests — *변경 없이* 전부 통과 = 동작 보존 증명.
**수동**: 서버+봇 → 사냥터 patrol→chase, 공격 6단계, 5초 respawn 관찰.

---

## 📚 학습 포인트

- **§2.2 컨테이너 + System 패턴 (GPP Component)**: 데이터 소유 = 컨테이너/엔티티, 로직 = System(변경만). System끼리 직접 호출 X — 공유 상태 경유. God class를 깨는 정석.
- **tick thread 규율 보존(§1.1)**: System을 추출해도 *호출은 여전히 tick thread 안*에서만(EnqueueJob 경유). 외부 스레드가 System을 직접 부르면 맵=actor 단일 스레드 불변식 붕괴.
- **순수 리팩토링의 증명**: 로직을 옮기기만 했으면 기존 테스트가 *수정 없이* 통과해야 함. 테스트를 고쳐야 한다면 동작이 바뀐 것 = 리팩토링 아님.

---

## ⚠️ 함정 / 주의사항

- **동작 보존 절대** — 판정 로직(rewind/AABB/aggro 거리/respawn tick) 한 줄도 의미 변경 금지. 메서드 위치만 이동.
- **internal mutator 과다 노출 경계(§0.3)**: mutator를 너무 잘게 쪼개면 invariant가 4파일로 흩어짐. 최소 surface + 컨테이너 주석.
- **target rewind 비대칭은 이 Phase 범위 아님**: CombatSystem 추출 시 기존 `ProcessAttack`의 attacker-only rewind를 *그대로* 옮김. 근본 봉합은 M4.4 (work-pin 🔴).
- **size-guard hook 경고**: GameMap 편집 중 600줄+ 경고가 뜸 (의도된 신호 — 분리 완료 시 사라짐).

---

## ➡️ 다음 Phase

- Phase 04 (GameSession trust-boundary 추출) — server 도메인 순차

---

## 📋 박제 (완료 후)

- **복잡 등급** — `03-gamemap-system-split-DONE.md` 박음.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
