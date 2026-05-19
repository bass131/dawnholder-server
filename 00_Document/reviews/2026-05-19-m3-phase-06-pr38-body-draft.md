# PR #38 Body Draft — M3 Phase 06 Emergency Combat

> GitHub PR ready 전환 시 본문에 붙여넣기 위한 draft.  
> Step 5/6 완료 후 `TBD`와 체크박스를 실제 결과로 갱신한다.

## Summary

M3 Phase 06 emergency combat infrastructure.

This PR adds the minimum server-authoritative combat loop needed for the 2026-05-20 demo:

- server-owned enemy combat state
- `C_Attack` intent packet
- enemy spawn packet so clients can know target ids
- hit result packet with HP state included
- optional death packet / HP 0 death-equivalent handling
- `ProtocolVersion.Current = 3`
- attack handler and combat validation tests

Phase 06 is intentionally emergency-scoped. Lag compensation, precision hitboxes, full damage formulas, cheat-flag persistence, and PvP are deferred to M4.

## Commits

- `986a042` — docs: MessagePack 잔재 정정
- `de1031a` — feat(M3 Phase 06 WIP): Step 1~4 + Codex 사전 검증 봉합
- `TBD` — feat(M3 Phase 06): Step 5 AttackHandler + Step 6 tests

## 변경 요약

### Step 1~4

- `02_Server/GameServer/Combat/` 신설
- `PlayerEntity` combat HP 상태 추가
- `EnemyEntity` / `EnemyKind` 추가
- `GameMap`에 server-owned enemy registry 추가
- PDL combat packet 추가
- `ProtocolVersion.Current` v2 → v3 bump
- PacketGenerator 재생성 + `Shared.dll` 갱신
- handshake test expectation v3 반영

### Step 5~6

- `AttackHandler` 추가
- `HandlerRegistry`에 `C_Attack` 등록
- `GameSession.SubmitAttack(...)` 추가
- attack mutation은 `GameMap.EnqueueJob` 경유로 처리
- server authority checks:
  - attacker는 session entity id에서 강제
  - target exists / alive
  - server position 기반 range check
  - 500ms cooldown rate-limit
  - fixed emergency damage
- `AttackHandlerTests` 추가

### Step 7 후속

- Unity 1인 + headless-bot smoke 검증
- Phase 07 boss/stage clear 진입
- `EmergencyCombatSmoke.cs` 구현 가능 상태로 전환

## Codex β 사전 검증 반영

`00_Document/reviews/2026-05-19-m3-phase-06-codex-precommit-review.md`

Codex β γ 6회차 사전 검증에서 발견된 HIGH 2건 + MEDIUM 3건을 코드 진입 전 봉합했다.

| Finding | Risk | Resolution |
|---|---:|---|
| enemy spawn/identity packet 누락 | HIGH | `S_EntitySpawn` 추가 |
| attack 입력 모델 미확정 | HIGH | `C_Attack { targetEntityId }`, attacker는 session에서 강제 |
| `S_HitResult` / `S_EntityHpUpdate` 중복 | MEDIUM | `S_HitResult`에 `currentHp/maxHp` 포함 |
| `ProtocolVersion` bump 누락 | MEDIUM | `Current = 3` |
| rate-limit "reject" vs silent drop 표현 불일치 | MEDIUM | 기대값을 `no HP change + no broadcast`로 고정 |

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
```

Version:

```text
ProtocolVersion.Current = 3
```

Notes:

- packet ids are append-only under the current generator
- stale clients should fail handshake instead of silently dropping unknown combat packets
- Phase 07 will add `S_StageClear` separately

## Acceptance Criteria

- [ ] enemy spawn is sent to newly joined clients through `S_EntitySpawn`
- [ ] client `C_Attack` reduces enemy HP only after server-side checks
- [ ] hit result is broadcast with `damage/currentHp/maxHp`
- [ ] enemy death broadcasts exactly once or HP 0 death-equivalent is handled under Option B
- [ ] rate-limit violation within 500ms is silent drop: no HP change, no broadcast
- [ ] out-of-range attack is silent no-op
- [ ] protocol version mismatch rejects stale clients

## Constitution Alignment

### #1 Server Authority

- client sends attack intent only
- client does not send attacker id, damage, HP, or authoritative position
- server computes range, hit, damage, HP, death

### #3 Trust Boundary

- attacker identity is derived from `GameSession`
- target id is validated server-side
- attack cooldown/rate-limit is enforced server-side
- invalid / too-fast / out-of-range attacks fail closed as no-op

### #5 No Blocking in Tick Loop

- handler only decodes and calls session method
- combat state mutation is marshaled into `GameMap` actor path
- no `await`, `Task.Delay`, `Thread.Sleep`, or DB call in combat tick mutation path

## Test Plan

- [ ] `dotnet build Dawnholder.slnx --nologo`
- [ ] `dotnet test Dawnholder.slnx --nologo --filter "FullyQualifiedName~AttackHandlerTests|FullyQualifiedName~HandshakeHandlerTests"`
- [ ] `dotnet test Dawnholder.slnx --nologo`
- [ ] Unity manual smoke:
  - connect
  - receive enemy spawn
  - attack on ground
  - enemy HP decreases
  - enemy disappears at HP 0
- [ ] Optional bot smoke after `.cs` implementation:
  - `EmergencyCombatSmoke`

## Known Trade-offs

- no lag compensation
- no precision hitboxes
- no full damage formula
- no enemy AI
- no PvP
- no cheat-flag persistence yet
- fixed damage is acceptable for emergency demo

## Follow-ups

### Phase 07

- boss as `EnemyKind.Boss`
- boss spawn in right zone
- `S_StageClear` separate from death packet
- stage clear broadcast exactly once
- `BossStageClearSmoke.cs` implementation from spec

### M4 Backlog

Tracked in `00_Document/M4-backlog.md`:

- shared damage formulas
- lag compensation
- precision hitboxes
- cheat-flag table
- jump Y mispredict
- packet explicit ID / doc-range alignment
- PvP support decision

## Review Notes

- PR contains emergency demo infrastructure, not final combat design.
- `02_Server` and `98_Shared` changes are expected.
- `03_Client/Assets/Plugins/Shared/Shared.dll` must be included when PDL/generated protocol changes are included.
- Phase 07 should stay a separate commit even if it lands in the same PR.
