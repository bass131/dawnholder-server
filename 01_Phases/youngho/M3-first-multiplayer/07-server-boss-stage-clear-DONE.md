---
summary: M3 Phase 07 서버 보스 + Stage Clear를 완료했다. Boss는 EnemyKind.Boss 특수 케이스로 우측 zone에 spawn되고, HP 0에서 S_EntityDeath 뒤 S_StageClear를 1회 broadcast하며, BossStageClearSmoke fresh server 실측까지 PASS했다.
phase: 07-server-boss-stage-clear
work-id: phase07-boss-stage-clear
status: done
completed_at: 2026-05-19
commit: "35358fb / TBD (BossStageClearSmoke follow-up commit)"
---

# Phase 07 — 서버 보스 + Stage Clear 트리거 완료 박제

**소요 시간**: server 1.5h 예상 + smoke 구현/실측 후속.

## TL;DR

Phase 07은 Phase 06 combat 흐름 위에 우측 zone Boss와 server-authoritative StageClear broadcast를 얹은 작업이다. Boss는 별도 `BossEntity`가 아니라 `EnemyKind.Boss`를 가진 `EnemyEntity`로 처리하고, HP 0 시 `S_EntityDeath` 뒤에 `S_StageClear { bossEntityId }`를 1회 broadcast한다.

결과적으로 Boss spawn -> 10회 attack -> HP 0 -> death -> StageClear -> dead boss re-attack no-op까지 headless-bot smoke로 검증됐다.

## 5단계 보고

- **무엇을 만들었나** — `GameMap.SpawnBoss(30, 0, 100)`, `EnemyKind.Boss`, `S_StageClear`, `_stageCleared` flag, boss death 시 StageClear 1회 broadcast, `BossStageClearTests` 3건, `BossStageClearSmoke`.
- **왜 필요한가** — 2026-05-20 면담 데모에서 "보스를 잡으면 스테이지 클리어가 뜬다"는 완결 흐름을 서버 권위로 보여주기 위해 필요했다.
- **어떻게 만들었나** — Phase 06의 `EnemyEntity`와 `ProcessAttack`을 그대로 재사용하고, target kind가 `Boss`이며 아직 `_stageCleared == false`인 경우에만 `S_StageClear`를 보낸다. death는 entity lifecycle, StageClear는 game event로 분리했다.
- **테스트 결과** — `dotnet build Dawnholder.slnx --nologo` PASS, `dotnet test --no-build --nologo` 170 PASS / 1 Skip, `BossStageClearTests` 3건 PASS, `BossStageClearSmoke` fresh server PASS.
- **다음 스텝** — Unity Phase 08b/08c에서 `S_StageClear` dispatch + UI 표시 연결, 이후 M4에서 보스 AI/패턴/정밀 판정 확장.

## AC 검증 결과

### 1. Boss spawn 우측 zone

```text
PASS
GameMap ctor에서 SpawnBoss(30, 0, 100) 호출.
Normal=entityId 1, Boss=entityId 2, Player=entityId 3부터 발급.

BossStageClearSmoke:
entity=3 boss=2 moveIntents=113
```

### 2. Phase 06 흐름 그대로 attack -> HP 감소

```text
PASS
Boss도 EnemyEntity이므로 C_Attack -> ProcessAttack -> S_HitResult 경로를 그대로 사용.
Boss HP 100, BaseDamage 10 기준 10회 hit로 HP 0.

[Bot] BossStageClearSmoke: success=True entity=3 boss=2 hits=10 stageClear=True
      boss hp: 100 -> 0 moveIntents=113 death=True stageClearCount=1 duplicateSuppressed=True
```

### 3. Boss HP 0 -> S_StageClear broadcast 1회

```text
PASS
BossStageClearTests.Boss_Death_BroadcastsStageClearOnce
BossStageClearSmoke: stageClear=True, stageClearCount=1
```

### 4. 중복 방지

```text
PASS
BossStageClearTests.BossDuplicateAttack_NoExtraStageClear
BossStageClearSmoke: duplicateSuppressed=True

dead boss re-attack 3회 후 추가 S_HitResult/S_EntityDeath/S_StageClear 없음.
```

### 5. handler/entity 단위 테스트

```text
PASS
BossStageClearTests 3/3 PASS:
- Boss_Death_BroadcastsStageClearOnce
- BossDuplicateAttack_NoExtraStageClear
- NormalEnemy_Death_NoStageClear

전체 회귀:
dotnet test --no-build --nologo
통과 170 / 건너뜀 1 / 실패 0
```

### 6. smoke 실측

```text
PASS
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario BossStageClearSmoke

[Bot] BossStageClearSmoke: success=True entity=3 boss=2 hits=10 stageClear=True
      boss hp: 100 -> 0 moveIntents=113 death=True stageClearCount=1 duplicateSuppressed=True
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **Boss 모델**: 별도 `BossEntity` vs `EnemyKind.Boss` -> `EnemyKind` 채택. 응급 단계에서는 AI/state machine이 없어 model 분리보다 combat 재사용 가치가 컸다.
- **StageClear 표현**: death packet flag vs 별도 `S_StageClear` -> 별도 packet 채택. entity lifecycle과 game event 의미를 분리했다.
- **중복 방지**: enemy remove만 vs `_stageCleared` flag 추가 -> 둘 다 사용. remove는 현 시나리오 중복을 막고, flag는 미래 boss 다중화에도 1회 broadcast 안전망이 된다.
- **ProtocolVersion**: Phase 06 v3에서 추가 bump vs v3 유지 -> v3 유지. Phase 06에서 stale client cutoff가 이미 박혔고, Phase 07은 같은 emergency PR 안의 additive 1패킷이다.
- **broadcast 순서**: StageClear 먼저 vs death 먼저 -> `S_EntityDeath` 후 `S_StageClear`. 클라가 entity lifecycle 처리 후 stage clear UI를 띄우는 흐름이 자연스럽다.
- **Boss smoke 이동 방식**: 위치 강제/테스트 훅 vs `C_MoveIntent` 실제 이동 -> 실제 이동 채택. bot도 클라처럼 input intent만 보내고 서버 권위 좌표로 range 안에 들어간다.

## 막혔던 지점 (있다면)

- **entity id pool shift** — Normal=1만 있던 상태에서 Boss=2가 추가되며 Player id가 3으로 밀렸다. 기존 6개 테스트 파일의 기대값을 갱신했다.
- **Normal death와 StageClear 분리** — Phase 06 death 경로를 그대로 쓰면 Normal enemy 처치에도 stage clear가 뜰 수 있어 `NormalEnemy_Death_NoStageClear` 테스트를 추가했다.
- **Unity dispatch 공백** — server-only Phase라 Unity는 아직 `S_StageClear` dispatch가 없다. Phase 08b/08c 후속으로 남긴다.
- **Boss smoke 이동 시간** — Boss가 `(30,0)`이고 range가 3이라 bot이 x≈28까지 실제 이동해야 한다. `moveIntents=113`으로 검증 시간이 약 16초까지 늘어났지만 면담 전 smoke 안정성이 더 중요했다.

## 학습 일지 후보 키워드

- **★★★ EnemyKind 통합 보스 모델** (`boss-as-enemy-kind`) — 별도 entity class 없이 combat 흐름 재사용.
- **★★★ StageClear 권위 broadcast** (`server-authoritative-stage-clear`) — 클라가 보스 사망을 자체 판정하지 않고 서버 broadcast를 단일 진실로 사용.
- **★★★ death vs stage clear 분리** (`entity-death-vs-game-event`) — lifecycle packet과 game event packet을 분리한 의미 설계.
- **★★ 중복 방지 flag** (`stage-clear-idempotency-flag`) — `_stageCleared`로 StageClear 1회 보장.
- **★★ entity id pool shift 회귀** (`entity-id-pool-shift-regression`) — server-owned entity 추가가 player id fixture를 밀어내는 테스트 회귀.
- **★★ headless boss route smoke** (`headless-boss-route-smoke`) — 실제 이동 intent로 boss range까지 접근한 뒤 combat packet을 검증.
- **★ gamma 7회차 사전 명세** (`boss-stage-clear-smoke-pre-spec`) — 구현 전 smoke spec을 먼저 박아 Phase 07 검증 경로를 선명하게 만든 흐름.

## Codex 사전/병렬 산출물

- `99_Tools/headless-bot/Scenarios/BossStageClearSmoke.md` — Phase 07 smoke scenario spec.
- `99_Tools/headless-bot/Scenarios/BossStageClearSmoke.cs` — Phase 07 smoke implementation + fresh server PASS.
