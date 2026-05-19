# PR #38 Body Draft — M3 Phase 06+07 Emergency Combat + Boss Stage Clear

Suggested title:

```text
M3 Phase 06+07: 서버 응급 전투 + 보스 + Stage Clear (server 7+5/5 + smoke PASS)
```

## Summary

This PR completes the server-side emergency demo combat path for M3:

- Phase 06: server-authoritative emergency combat, Normal enemy spawn, attack intent, HP/death broadcast, rate-limit silent drop, headless smoke PASS.
- Phase 07: Boss spawn in the right zone, Boss death -> `S_StageClear` broadcast exactly once, duplicate suppression, server tests PASS.

Scope is intentionally demo-grade for the 2026-05-20 professor meeting. Lag compensation, precision hitboxes, full damage formulas, cheat-flag persistence, richer enemy AI, and PvP are deferred to M4.

## Commit Chain

- `986a042` — docs: MessagePack 잔재 정정
- `de1031a` — feat(M3 Phase 06 WIP): 서버 응급 전투 인프라 Step 1~4 + Codex 사전 검증 봉합
- `e197ae9` — Merge remote-tracking branch `origin/main` into Phase 06 branch
- `ee88b41` — feat(M3 Phase 06): Step 5+6 완성 + Codex 병렬 산출물 (server 7/7)
- `5beb0ec` — feat(M3 Phase 06 smoke): EmergencyCombatSmoke 헤드리스 봇 구현
- `eb5e7f0` — fix(M3 Phase 06 smoke): rate-limit burst 100 -> 50ms 보정 + 실측 로그 박제
- `35358fb` — feat(M3 Phase 07): 보스 + Stage Clear 응급
- `TBD` — feat(M3 Phase 07 smoke): BossStageClearSmoke 구현 + 실측
- `TBD` — docs(M3 Phase 06+07): DONE/PR body finalization

## Changes

### Phase 06 — Server Combat Emergency

- `02_Server/GameServer/Combat/` 추가
- `EnemyEntity`, `EnemyKind`, player HP/rate-limit state 추가
- `C_Attack`, `S_EntitySpawn`, `S_HitResult`, `S_EntityDeath` 추가
- `ProtocolVersion.Current = 3`
- `AttackHandler` + `GameSession.SubmitAttack(...)`
- `GameMap.ProcessAttack(...)`
- server-side checks:
  - attacker는 session entity id에서 강제
  - target exists/alive 검증
  - server-authoritative position 기반 range check
  - 500ms cooldown rate-limit silent drop
  - fixed emergency damage 10
- `EmergencyCombatSmoke` headless scenario PASS

### Phase 07 — Boss + Stage Clear

- Boss spawn: `EnemyKind.Boss`, `(30, 0)`, HP 100
- `S_StageClear { bossEntityId }` 추가, packet ID 15
- Boss death 시 `S_EntityDeath` 뒤에 `S_StageClear` broadcast
- `_stageCleared` flag로 duplicate stage clear 차단
- Normal enemy death는 StageClear를 트리거하지 않음
- entity id pool shift 회귀 갱신:
  - Normal=1
  - Boss=2
  - Player=3부터
- `BossStageClearTests` 3건 추가
- `BossStageClearSmoke` headless scenario PASS

## Codex Review / Pre-Spec

### Gamma 6 — Phase 06 Precommit Review

`00_Document/reviews/2026-05-19-m3-phase-06-codex-precommit-review.md`

| Finding | Risk | Resolution |
|---|---:|---|
| enemy spawn/identity packet 누락 | HIGH | `S_EntitySpawn` 추가 |
| attack 입력 모델 미확정 | HIGH | `C_Attack { targetEntityId }`, attacker는 session에서 강제 |
| `S_HitResult` / `S_EntityHpUpdate` 중복 | MEDIUM | `S_HitResult`에 `currentHp/maxHp` 포함 |
| `ProtocolVersion` bump 누락 | MEDIUM | `Current = 3` |
| rate-limit "reject" vs silent drop 표현 불일치 | MEDIUM | 기대값을 `no HP change + no broadcast`로 고정 |

### Gamma 7 — Phase 07 Smoke Pre-Spec

`99_Tools/headless-bot/Scenarios/BossStageClearSmoke.md`

- Boss spawn -> repeated `C_Attack`
- Boss HP 0 -> `S_EntityDeath` + `S_StageClear`
- dead boss re-attack -> no duplicate hit/death/stage clear

## Protocol / PDL

Actual PDL source:

```text
99_Tools/PacketGenerator/PDL.xml
```

New packets:

```text
C_Attack { int targetEntityId }
S_EntitySpawn { int entityId, byte entityKind, float x, float y, int currentHp, int maxHp }
S_HitResult { int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp }
S_EntityDeath { int entityId }
S_StageClear { int bossEntityId }
```

Version:

```text
ProtocolVersion.Current = 3
```

Notes:

- Phase 06 bumped v2 -> v3 because stale clients must fail handshake.
- Phase 07 stayed on v3. It adds one packet inside the same emergency PR after the v3 cutoff.
- Packet ids are append-only under the current generator.

## Acceptance Criteria

### Phase 06

- [x] enemy spawn is sent to newly joined clients through `S_EntitySpawn`
- [x] client `C_Attack` reduces enemy HP only after server-side checks
- [x] hit result is broadcast with `damage/currentHp/maxHp`
- [x] enemy death broadcasts exactly once
- [x] rate-limit violation within 500ms is silent drop: no HP change, no broadcast
- [x] out-of-range attack is silent no-op
- [x] protocol version mismatch rejects stale clients

### Phase 07

- [x] Boss spawn in right zone: `(30, 0)`, HP 100
- [x] Boss uses Phase 06 attack -> HP decrease flow
- [x] Boss HP 0 broadcasts `S_StageClear` exactly once
- [x] duplicate attack after Boss death does not emit extra hit/death/stage clear
- [x] Normal enemy death does not emit `S_StageClear`
- [x] `BossStageClearSmoke` fresh server PASS

## Constitution Alignment

### #1 Server Authority

- client sends attack intent only
- client does not send attacker id, damage, HP, stage clear, or authoritative position
- server computes range, hit, damage, HP, death, and stage clear

### #3 Trust Boundary

- attacker identity is derived from `GameSession`
- target id is validated server-side
- cooldown/rate-limit is enforced server-side
- invalid / too-fast / out-of-range / dead-target attacks fail closed as no-op

### #5 No Blocking in Tick Loop

- handler only decodes and calls session method
- combat state mutation is marshaled into `GameMap` actor path
- combat/stage clear mutation has no await, `Task.Delay`, `Thread.Sleep`, or DB call

## Test Plan

- [x] `dotnet build Dawnholder.slnx --nologo`
- [x] `dotnet test Dawnholder.slnx --nologo`
  - 170 PASS / 1 Skip
  - Phase 06: `AttackHandlerTests` 6/6 PASS
  - Phase 07: `BossStageClearTests` 3/3 PASS
- [x] `EmergencyCombatSmoke`

```text
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario EmergencyCombatSmoke

[Bot] EmergencyCombatSmoke: success=True entity=3 target=1 hits=3 death=True
      hp: 30 -> 0 moveIntents=33 rateLimitDropped=True optionB=False
```

- [x] `BossStageClearSmoke`

```text
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario BossStageClearSmoke

[Bot] BossStageClearSmoke: success=True entity=3 boss=2 hits=10 stageClear=True
      boss hp: 100 -> 0 moveIntents=113 death=True stageClearCount=1 duplicateSuppressed=True
```

- [ ] Unity manual smoke after Phase 08b/08c:
  - connect
  - receive Normal + Boss spawn
  - attack Normal on ground
  - Normal HP decreases and death hides/despawns it
  - attack Boss in right zone
  - `S_StageClear` drives UI

## Known Trade-offs

- no lag compensation
- no precision hitboxes
- no full damage formula
- no enemy AI
- no PvP
- no cheat-flag persistence yet
- fixed damage is acceptable for emergency demo
- StageClear UI dispatch is Unity Phase 08b/08c follow-up

## Follow-ups

### Phase 08 / Yuhyeon

- Unity dispatch for `S_EntitySpawn`, `S_HitResult`, `S_EntityDeath`, `S_StageClear`
- enemy/boss prefab visual mapping from `entityKind`
- right-zone Boss visual placement driven by server spawn packet
- Stage Clear UI display from `S_StageClear`

### M4 Backlog

Tracked in `00_Document/M4-backlog.md`:

- shared damage formulas
- lag compensation
- precision hitboxes
- cheat-flag table
- jump Y mispredict reconcile
- packet explicit ID / doc-range alignment
- PvP support decision
