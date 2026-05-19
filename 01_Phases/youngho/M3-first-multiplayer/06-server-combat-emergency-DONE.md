---
summary: DRAFT — M3 Phase 06 서버 응급 전투 인프라 완료 박제 예정. C_Attack/S_EntitySpawn/S_HitResult 중심의 서버 권위 전투, enemy HP/death, rate-limit silent drop, ProtocolVersion v3 반영 결과를 Claude가 최종 채움.
phase: 06-server-combat-emergency
work-id: phase06-emergency-combat
status: draft
completed_at: TBD
commit: TBD
---

# Phase 06 — 서버 응급 전투 인프라 완료 박제 DRAFT

> **DRAFT**: Claude가 Step 5/6 구현과 검증을 끝낸 뒤 실제 수치, commit, 테스트 결과로 교체한다.  
> 본 문서는 사실 박제 골격과 학습 키워드 선반이다. 아직 `status: done`이 아니다.

**소요 시간**: TBD

## TL;DR

Phase 06은 M3 면담 데모용 서버 권위 전투 골격을 박는 작업이다. 클라는 `C_Attack { targetEntityId }`로 공격 의도만 보내고, 서버가 attacker 강제, range 검증, damage 적용, HP/death broadcast를 책임진다.

완료 시 enemy spawn → attack → HP 감소 → death/despawn 흐름이 end-to-end로 보이고, Phase 07 boss/stage clear가 같은 combat 흐름 위에 올라갈 수 있어야 한다.

## 5단계 보고

- **무엇을 만들었나** — TBD. 예상 산출물: `Combat/EnemyEntity`, `EnemyKind`, player HP 필드, `C_Attack`, `S_EntitySpawn`, `S_HitResult`, `S_EntityDeath` 또는 Option B HP 0 death-equivalent, `ProtocolVersion.Current = 3`, `AttackHandler`, combat tests.
- **왜 필요한가** — M3 5/20 면담 데모에서 "서버 권위로 적을 때리고 HP가 줄고 사라진다"를 보여주기 위한 최소 전투 인프라다. Phase 07 보스와 StageClear는 이 흐름을 재사용한다.
- **어떻게 만들었나** — TBD. 원칙: handler는 decode + `session.SubmitAttack(...)`만 수행, 실제 combat mutation은 `GameMap.EnqueueJob` 이후 map/tick 쪽에서 처리. attacker는 packet이 아니라 session entity id로 강제.
- **테스트 결과** — TBD. `dotnet build Dawnholder.slnx --nologo`, `AttackHandlerTests` 6건, 수동 Unity + bot smoke 결과를 채운다.
- **다음 스텝** — Phase 07 서버 보스 + `S_StageClear` 1회 broadcast. Codex spec: `99_Tools/headless-bot/Scenarios/BossStageClearSmoke.md`.

## AC 검증 결과

> Claude가 실제 실행 후 채운다. 실패하면 이 Phase는 아직 done이 아니다.

### 1. Spawn 흐름

Expected:

```text
클라 접속 → S_EntitySpawn 수신 → enemy 1마리 화면 표시
```

Result:

```text
TBD
```

### 2. 공격 → HP 감소 → broadcast

Expected:

```text
C_Attack(targetEntityId) → 서버 range/cooldown/damage 판정 → S_HitResult broadcast
```

Result:

```text
TBD
```

### 3. enemy death

Expected:

```text
HP 0 → S_EntityDeath 1회 broadcast → 클라에서 사라짐
Option B면 S_HitResult.currentHp == 0으로 despawn
```

Result:

```text
TBD
```

### 4. rate-limit silent drop

Expected:

```text
500ms 안 2회 공격 → 2회차는 no HP change + no broadcast
```

Result:

```text
TBD
```

### 5. out-of-range silent no-op

Expected:

```text
range 밖 공격 → no HP change + no broadcast
```

Result:

```text
TBD
```

### 6. handler/unit tests

Expected:

```bash
dotnet test ... AttackHandlerTests ...
```

Result:

```text
TBD
```

### 7. build

Expected:

```bash
dotnet build Dawnholder.slnx --nologo
```

Result:

```text
TBD
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **attack 입력 모델**: direction vs `targetEntityId` → `targetEntityId` 채택. 응급 데모에서 facing/ray/hitbox 검증을 피하고, 서버가 target 존재/range만 검증하면 된다.
- **attacker 표현**: packet field vs session 강제 → session 강제. 클라가 attacker id를 보내면 다른 entity 도용 공격면이 생긴다.
- **HP update 패킷**: `S_HitResult` + 별도 `S_EntityHpUpdate` vs 통합 → 통합. 응급 데모에서는 damage text와 HP bar 갱신을 한 packet으로 처리해 broadcast 비용과 client dispatch를 줄인다.
- **death 표현**: 별도 `S_EntityDeath` vs HP 0 death-equivalent → Option A는 별도 death, Option B는 HP 0. 시간 부족 시 Option B로 내려갈 수 있게 문서화했다.
- **ProtocolVersion**: additive packet이라 bump 생략 vs v3 bump → v3 bump. handshake exact equality라 stale client를 빠르게 끊는 편이 안전하다.
- **enemy 모델**: 별도 boss class vs `EnemyKind` → `EnemyKind.Normal/Boss` 재사용 방향. Phase 07에서 stage clear trigger만 얹기 쉽다.
- **rate-limit 반응**: reject packet vs silent drop → 응급은 silent drop. 기대값은 no HP change + no broadcast.

## 막혔던 지점 (있다면)

- **enemy spawn 패킷 누락 위험** — Codex γ 6회차 사전 검증에서 발견. `targetEntityId` 공격 모델을 쓰려면 클라가 target id를 알아야 하므로 `S_EntitySpawn`이 필수.
- **PDL 실제 경로 혼동** — 잘못된 경로 `98_Shared/Protocol/PDL.xml` 대신 실제 단일 소스는 `99_Tools/PacketGenerator/PDL.xml`.
- **Packet ID 예약 문서 불일치** — 문서의 3000번대 combat 예약과 현재 generator의 append-only `++packetID` 정책이 다름. 응급은 11~15 append로 진행, M4 후속 정합 후보.
- **TBD** — Claude가 실제 구현 중 막힌 내용 추가.

## 학습 일지 후보 키워드

- **★★★ 서버 권위 전투 입력 모델** (`server-authoritative-combat-intent`) — 클라는 attack intent만 보내고 서버가 hit/damage/HP/death를 결정.
- **★★★ attacker session 강제 패턴** (`attacker-from-session-not-packet`) — packet에 attacker를 넣지 않아 entity id 도용을 차단.
- **★★★ enemy spawn identity 패킷** (`entity-spawn-target-identity`) — target id를 클라에 알려주지 않으면 target-based combat이 성립하지 않는다는 사전 설계 함정.
- **★★★ ProtocolVersion bump 판단** (`protocol-version-bump-additive-demo`) — additive packet이어도 stale client cutoff가 중요하면 bump.
- **★★ HitResult + HpUpdate 통합 trade-off** (`hit-result-hp-update-collapse`) — 응급 데모에서 event/state packet을 합쳐 client dispatch와 broadcast 비용을 낮춤.
- **★★ rate-limit silent drop** (`combat-rate-limit-silent-drop`) — reject UX보다 trust boundary와 구현 단순성을 우선한 응급 판단.
- **★★ GameMap.EnqueueJob combat mutation** (`combat-map-actor-mutation`) — handler decode-only, mutation은 map actor/tick 경로에서 처리.
- **★ dist squared range check** (`distance-squared-range-check`) — sqrt 없이 range 판정. 성능보다 표준 패턴과 단순성 가치.
- **★ Option B emergency scope cut** (`emergency-option-b-combat-scope-cut`) — `S_EntityDeath` 생략 + HP 0 despawn으로 데모 성립성을 보존.
- **★ boss as EnemyKind** (`boss-as-enemy-kind`) — Phase 07 확장을 위해 boss를 enemy 특수 케이스로 두는 응급 설계.

## Codex 사전/병렬 산출물

- `00_Document/reviews/2026-05-19-m3-phase-06-codex-precommit-review.md` — Phase 06 사전 검증.
- `99_Tools/headless-bot/Scenarios/EmergencyCombatSmoke.md` — Phase 06 smoke scenario spec.
- `99_Tools/headless-bot/Scenarios/BossStageClearSmoke.md` — Phase 07 smoke scenario spec.

## Claude 최종화 체크리스트

- [ ] `status: draft` → `status: done`
- [ ] `completed_at` 실제 날짜 입력
- [ ] `commit` short hash 입력
- [ ] 산출물 list를 실제 변경 파일 기준으로 교체
- [ ] AC 검증 결과에 실제 명령과 결과 입력
- [ ] 막혔던 지점의 TBD 제거
- [ ] Phase 07 다음 스텝을 현재 진행 상태에 맞게 갱신
