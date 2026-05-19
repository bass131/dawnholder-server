---
summary: M3 Phase 06 서버 응급 전투를 완료했다. 서버 소유 Normal enemy spawn, C_Attack intent, S_HitResult HP 감소, S_EntityDeath, 500ms rate-limit silent drop, ProtocolVersion v3, headless smoke PASS가 박혀 Phase 07 boss/stage clear가 같은 전투 흐름을 재사용할 수 있게 됐다.
phase: 06-server-combat-emergency
work-id: phase06-emergency-combat
status: done
completed_at: 2026-05-19
commit: "986a042 / de1031a / e197ae9 / ee88b41 / 5beb0ec / eb5e7f0"
---

# Phase 06 — 서버 응급 전투 인프라 완료 박제

**소요 시간**: 응급 분해 2.5h 예상에서 server + smoke 검증까지 확장.

## TL;DR

Phase 06은 2026-05-20 면담 데모를 위해 서버 권위 전투의 최소 골격을 박은 작업이다. 클라는 `C_Attack { targetEntityId }`로 공격 의도만 보내고, 서버가 attacker 강제, target/range/cooldown 검증, damage 적용, HP/death broadcast를 책임진다.

결과적으로 Normal enemy spawn -> attack -> HP 감소 -> death/despawn 흐름이 headless-bot smoke로 검증됐고, Phase 07 boss/stage clear는 같은 `EnemyEntity`/`ProcessAttack` 경로 위에 올라갈 수 있게 됐다.

## 5단계 보고

- **무엇을 만들었나** — `EnemyEntity`/`EnemyKind`, player combat HP와 `LastAttackTickMs`, `C_Attack`, `S_EntitySpawn`, `S_HitResult`, `S_EntityDeath`, `AttackHandler`, `GameSession.SubmitAttack`, `GameMap.ProcessAttack`, `ProtocolVersion.Current = 3`, `EmergencyCombatSmoke`.
- **왜 필요한가** — M3 면담 데모에서 "서버가 적 spawn과 전투 결과를 권위 있게 결정한다"는 흐름을 눈으로 보여주기 위해 필요했다. Phase 07 보스와 StageClear도 이 combat baseline이 있어야 얹을 수 있다.
- **어떻게 만들었나** — handler는 packet decode 후 `session.SubmitAttack(targetEntityId)`만 호출한다. attacker는 packet이 아니라 `GameSession`의 entity id에서 강제하고, 실제 mutation은 `GameMap.EnqueueJob` 이후 map actor/tick 경로에서 처리한다.
- **테스트 결과** — `dotnet build Dawnholder.slnx --nologo` PASS, Phase 06 handler 단위 테스트 6/6 PASS, 전체 회귀는 Phase 07 포함 170 PASS / 1 Skip, `EmergencyCombatSmoke` fresh server 실측 PASS.
- **다음 스텝** — Phase 07 서버 보스 + `S_StageClear` 1회 broadcast, 이후 Unity Phase 08b/08c에서 enemy/boss dispatch와 stage clear UI 연결.

## AC 검증 결과

### 1. Spawn 흐름

```text
PASS
S_EntitySpawn으로 Normal enemy roster를 신규 client에게 전송.
EmergencyCombatSmoke가 target entity id를 수신해 공격 대상으로 사용.
```

### 2. 공격 -> HP 감소 -> broadcast

```text
PASS
[Bot] EmergencyCombatSmoke: success=True entity=3 target=1 hits=3 death=True
      hp: 30 -> 0 moveIntents=33 rateLimitDropped=True optionB=False
```

### 3. enemy death

```text
PASS
Normal enemy HP 0에서 S_EntityDeath 1회 수신.
smoke result: death=True, optionB=False
```

### 4. rate-limit silent drop

```text
PASS
50ms 간격 burst 공격에서 추가 S_HitResult/S_EntityDeath 없음.
smoke result: rateLimitDropped=True
```

### 5. out-of-range silent no-op

```text
PASS
AttackHandlerTests OutOfRange 케이스에서 no HP change + no broadcast 검증.
서버는 client position을 받지 않고 GameMap의 권위 position만 사용.
```

### 6. handler/unit tests

```text
PASS
Phase 06 AttackHandlerTests 6/6 PASS.
검증 범위: happy hit, invalid target, cooldown silent drop, auth failure, kill broadcast, duplicate death suppression.
```

### 7. build / protocol version

```text
PASS
dotnet build Dawnholder.slnx --nologo
경고 0 / 오류 0

ProtocolVersion.Current = 3
PDL 신규 패킷: C_Attack / S_EntitySpawn / S_HitResult / S_EntityDeath
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **attack 입력 모델**: direction vs `targetEntityId` -> `targetEntityId` 채택. 응급 데모에서 facing/ray/hitbox 검증을 피하고, 서버가 target 존재/range만 검증하면 된다.
- **attacker 표현**: packet field vs session 강제 -> session 강제. 클라가 attacker id를 보내면 다른 entity 도용 공격면이 생긴다.
- **HP update 패킷**: `S_HitResult` + 별도 `S_EntityHpUpdate` vs 통합 -> 통합. damage text와 HP bar 갱신을 한 packet으로 처리해 broadcast 비용과 client dispatch를 줄였다.
- **death 표현**: 별도 `S_EntityDeath` vs HP 0 death-equivalent -> Option A 별도 death 채택. Option B는 시간 부족 시 대체안으로만 남겼다.
- **ProtocolVersion**: additive packet이라 bump 생략 vs v3 bump -> v3 bump. handshake exact equality라 stale client를 빠르게 끊는 편이 안전했다.
- **enemy 모델**: 별도 boss class vs `EnemyKind` -> `EnemyKind.Normal/Boss` 재사용 방향. Phase 07에서 stage clear trigger만 얹기 쉽다.
- **rate-limit 반응**: reject packet vs silent drop -> 응급은 silent drop. 기대값은 no HP change + no broadcast.

## 막혔던 지점 (있다면)

- **enemy spawn 패킷 누락 위험** — Codex gamma 6회차 사전 검증에서 발견. `targetEntityId` 공격 모델을 쓰려면 클라가 target id를 알아야 하므로 `S_EntitySpawn`이 필수였다.
- **PDL 실제 경로 혼동** — 잘못된 경로 `98_Shared/Protocol/PDL.xml` 대신 실제 단일 소스는 `99_Tools/PacketGenerator/PDL.xml`.
- **Packet ID 예약 문서 불일치** — 문서의 3000번대 combat 예약과 현재 generator의 append-only `++packetID` 정책이 다르다. 응급은 11~15 append로 진행, M4 후속 정합 후보로 남겼다.
- **smoke rate-limit flake** — 100ms x 5 burst는 서버 tick 지연까지 합치면 500ms cooldown 경계를 밟아 extra hit가 통과할 수 있었다. `RateLimitBurstIntervalMs=50`으로 줄여 500ms 안에서 silent drop을 안정 검증했다.

## 학습 일지 후보 키워드

- **★★★ 서버 권위 전투 입력 모델** (`server-authoritative-combat-intent`) — 클라는 attack intent만 보내고 서버가 hit/damage/HP/death를 결정.
- **★★★ attacker session 강제 패턴** (`attacker-from-session-not-packet`) — packet에 attacker를 넣지 않아 entity id 도용을 차단.
- **★★★ enemy spawn identity 패킷** (`entity-spawn-target-identity`) — target id를 클라에 알려주지 않으면 target-based combat이 성립하지 않는다는 사전 설계 함정.
- **★★★ ProtocolVersion bump 판단** (`protocol-version-bump-additive-demo`) — additive packet이어도 stale client cutoff가 중요하면 bump.
- **★★ HitResult + HpUpdate 통합 trade-off** (`hit-result-hp-update-collapse`) — 응급 데모에서 event/state packet을 합쳐 client dispatch와 broadcast 비용을 낮춤.
- **★★ rate-limit silent drop** (`combat-rate-limit-silent-drop`) — reject UX보다 trust boundary와 구현 단순성을 우선한 응급 판단.
- **★★ GameMap.EnqueueJob combat mutation** (`combat-map-actor-mutation`) — handler decode-only, mutation은 map actor/tick 경로에서 처리.
- **★ dist squared range check** (`distance-squared-range-check`) — sqrt 없이 range 판정. 성능보다 표준 패턴과 단순성 가치.
- **★ smoke timing vs server tick** (`smoke-rate-limit-window`) — 자동 검증의 시간창은 서버 tick jitter를 고려해 cooldown 경계에서 충분히 떨어뜨려야 한다.
- **★ boss as EnemyKind** (`boss-as-enemy-kind`) — Phase 07 확장을 위해 boss를 enemy 특수 케이스로 두는 응급 설계.

## Codex 사전/병렬 산출물

- `00_Document/reviews/2026-05-19-m3-phase-06-codex-precommit-review.md` — Phase 06 사전 검증.
- `99_Tools/headless-bot/Scenarios/EmergencyCombatSmoke.md` — Phase 06 smoke scenario spec.
- `99_Tools/headless-bot/Scenarios/EmergencyCombatSmoke.cs` — Phase 06 smoke implementation + fresh server PASS.
- `99_Tools/headless-bot/Scenarios/BossStageClearSmoke.md` — Phase 07 smoke scenario spec.
