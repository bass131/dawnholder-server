---
summary: GameMap God class(665줄)를 §2.2 컨테이너+System으로 분리 — CombatSystem/EnemyAISystem/RespawnSystem 추출, GameMap 260줄. 동작 보존(test 315/0/4, 기존 테스트 무수정).
phase: 03-gamemap-system-split
work-id: m4.3r-phase03-gamemap-system
status: done
grade: 복잡
owner: youngho
completed_at: 2026-05-29
commit: 0c2b59b
---

# Phase 03 — GameMap System 분리 완료 박제

**소요 시간**: ~1.5h (server Worker + reviewer)

## TL;DR

서버의 가장 큰 God class였던 `GameMap`(665줄, 전투+AI+respawn+broadcast 4도메인)을 Code Convention §2.2(컨테이너+System)로 분리했다. 전투/AI/respawn 로직을 `CombatSystem`/`EnemyAISystem`/`RespawnSystem` 3개 클래스로 추출하고, `GameMap`은 상태 + tick 엔진 + actor 경계만 남겨 260줄로 줄였다(size-guard 600줄 임계 해소). 기존 테스트 0건 수정으로 315개 전부 통과 = 순수 리팩토링(동작 보존) 증명.

## 5단계 보고

- **무엇을 만들었나** — `Maps/Systems/CombatSystem.cs`(100줄, ProcessAttack+GetAttackHitbox), `EnemyAISystem.cs`(118줄, UpdateEnemies), `RespawnSystem.cs`(77줄, ProcessRespawns+_respawnQueue) 3개 System. GameMap 665→260줄.
- **왜 필요한가** — GameMap이 4개 도메인을 한 클래스에 쥐고 있어(부록 A §2.2 위반, 600줄+ = 거의 확실히 God class) 한 도메인만 보려 해도 전체를 읽어야 했다. §0.2(읽는 사람 뇌 부담 최소)에 정면 위배.
- **어떻게 만들었나** — 컨테이너(GameMap)는 상태(_players/_enemies/_pendingJobs/AllocId)+actor 경계(AddPlayer/RemovePlayer/EnqueueJob)+Tick 엔진만 유지. System은 `map`을 인자로 받아 공유 상태를 변경(System끼리 직접 호출 X). Tick이 호출 순서(physics→Combat→AI→Respawn)를 명문화. internal mutator 4개(CurrentTick/SetStageCleared/RemoveEnemy/EnqueueRespawn)만 노출(§0.3 최소 surface), invariant 주석은 컨테이너 1곳.
- **테스트 결과** — build 경고0/오류0, test 315통과/0실패/4skip(베이스라인 정합). 기존 EnemyAiTests(12)/AttackHandlerTests/BossStageClearTests/LagCompensationTests 무수정 통과.
- **다음 스텝** — Phase 04 (GameSession trust-boundary 추출). server 도메인 순차(같은 영역 동시편집 충돌 회피).

## AC 검증 결과

```bash
$ dotnet build Dawnholder.slnx --no-incremental
  빌드했습니다. 경고 0개 / 오류 0개

$ dotnet test Dawnholder.slnx --no-build
  통과! - 실패: 0, 통과: 315, 건너뜀: 4, 전체: 319 - GameServer.Tests.dll

$ wc -l 02_Server/GameServer/Maps/GameMap.cs
  260 02_Server/GameServer/Maps/GameMap.cs   # < 600 (size-guard 해소)
```

reviewer(Tier 2-A): 🔴 0건 (서버측). §2.2 컨테이너/System 경계, §1.1 tick thread(맵 lock 0), §0.3 최소 surface, target rewind 비대칭 무손상 모두 통과.

## 결정 흐름 (회고 참고용)

- **ProcessAttack 시그니처 보존 vs 호출처 전부 수정** → GameMap에 internal 래퍼(`ProcessAttack` → `_combatSystem.ProcessAttack(this,...)`) 유지 채택. 이유: 기존 테스트가 GameMap.ProcessAttack을 직접 호출 → 래퍼로 인터페이스 보존하면 테스트 무수정 = 동작 보존 증명이 깔끔.
- **respawn stats 전달 경로** → `SpawnEnemy(EnemyStats? stats = null)` 옵션 인자 추가. 이유: respawn 시 죽은 적의 원본 stats 유지(원본 `new EnemyEntity(..., dead.Stats)`와 동등). reviewer 동작 동등 확인.
- **internal mutator 입자** → 최소 4개로 응축(§0.3). 너무 잘게 쪼개면 invariant가 4파일로 흩어져 역효과.

## 학습 일지 후보 키워드

- §2.2 컨테이너+System (GPP Component), God class 분리, actor 모델 tick thread invariant(§1.1), 순수 리팩토링 동작 보존 증명(테스트 무수정), internal mutator 최소 surface
