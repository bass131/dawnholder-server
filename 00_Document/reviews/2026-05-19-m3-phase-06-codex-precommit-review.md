# M3 Phase 06 Emergency Combat — Codex Precommit Review (β/γ 6회차)

- 검토 대상:
  - `01_Phases/youngho/M3-first-multiplayer/06-server-combat-emergency.md`
  - `.claude/state/current-pin.txt`
  - `01_Phases/youngho/M3-first-multiplayer/07-server-boss-stage-clear.md`
  - `01_Phases/youngho/M3-first-multiplayer/05-client-remote-entity-registry-DONE.md`
- 참조:
  - `CLAUDE.md`
  - `00_Document/ADR/INDEX.md`
  - `00_Document/ADR/tech-stack/ADR-002-tcp-pdl.md`
  - `00_Document/ADR/tech-stack/ADR-004-tickrate.md`
  - `00_Document/ADR/harness/ADR-019-reviewer-agent.md`
  - `02_Server/CLAUDE.md`
  - `98_Shared/CLAUDE.md`
  - `99_Tools/PacketGenerator/PDL.xml`
  - `98_Shared/Protocol/Generated/GenPackets.cs`
  - `02_Server/GameServer/{Maps,Network,Handlers}`
- 검토 일시: 2026-05-19

주의: 요청 프롬프트의 `98_Shared/Protocol/PDL.xml`은 현재 존재하지 않는다. 실제 PDL 단일 소스는 `99_Tools/PacketGenerator/PDL.xml`이다.

## 결론

최종 추천은 **"이거 박고 진입"**이다.

Phase 06의 서버 권위 방향은 맞다. 다만 현재 정의 그대로 코드에 들어가면 end-to-end 데모가 막힐 가능성이 높다. 가장 큰 누락은 **enemy spawn/identity를 클라에 알려주는 패킷이 없다는 점**이다. `C_Attack { targetEntityId }`를 선택하려면 클라가 target entity id를 알아야 하는데, 현재 4패킷 정의에는 그 경로가 없다.

응급 모드 기준 권장 PDL은 아래 4개다.

```text
C_Attack { int targetEntityId }
S_EntitySpawn { int entityId, byte entityKind, float x, float y, int currentHp, int maxHp }
S_HitResult { int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp }
S_EntityDeath { int entityId }
```

Phase 07은 `S_StageClear { int bossEntityId }`를 별도 패킷으로 추가한다. `S_EntityDeath.isStageBoss` 플래그로 stage clear를 겸하지 않는다.

## 실행 결과

### 현재 build

```text
dotnet build Dawnholder.slnx --nologo

빌드했습니다.
경고 0개
오류 0개
경과 시간: 00:00:03.77
```

판정: PASS.

주의: build 과정에서 `03_Client/Assets/Plugins/Shared/Shared.dll` binary가 재작성되는 side effect가 발생했다. 검증 side effect였으므로 정리했고, 리뷰 문서 작성 전 worktree는 clean 상태였다.

### PDL ID 현황

현재 generated enum은 `S_PlayerLeave = 10`까지다.

근거:

- `98_Shared/Protocol/Generated/GenPackets.cs:18`
- `98_Shared/Protocol/Generated/GenPackets.cs:29`
- `99_Tools/PacketGenerator/PDL.xml:13`
- `99_Tools/PacketGenerator/Program.cs:123`

현 생성기는 PDL 정의 순서대로 `++packetID`를 부여한다. 따라서 Phase 06 패킷 4개를 맨 아래에 append하면 `11~14`, Phase 07 `S_StageClear`는 `15`가 된다. 충돌은 없다.

단, `98_Shared/CLAUDE.md`의 "3000~3999 combat" 예약 규칙은 현재 생성기와 맞지 않는다. 응급 데모는 append-only `11~15`로 가고, M4에서 explicit ID 지원 또는 문서 정정을 해야 한다.

## Findings

| 이슈 | 위험도 | 봉합 시점 | 근거 |
|---|---:|---|---|
| enemy spawn/identity 패킷 누락. 현재 4패킷으로는 클라가 enemy entityId/위치/종류를 알 수 없어 `targetEntityId` 공격과 표시가 막힘 | HIGH | 즉시 봉합 | Phase 06은 enemy spawn과 클라 표시를 요구하지만 PDL은 attack/result/hp/death만 정의. 기존 `S_PlayerJoin`은 player 전용이고 enemy는 owner 없는 서버 entity가 될 예정 |
| `C2S_Attack { targetEntityId or direction }` 결정 미확정. direction은 서버 ray/sweep/facing 검증까지 끌고 들어와 응급 범위를 넘김 | HIGH | 즉시 봉합 | 헌법 #1/#3상 attacker는 session에서 강제해야 하고, 서버 위치로 range 검증해야 함 |
| `S_HitResult`와 `S_EntityHpUpdate`가 같은 사건을 둘로 나눔. UI 이벤트와 상태 갱신이 중복되어 client handler와 broadcast 수가 증가 | MEDIUM | 즉시 봉합 | Phase 06 PDL 4패킷 정의, `GameMap.BroadcastToAll` N² 비용 인지 주석 |
| `ProtocolVersion.Current` bump가 정의에 명시되지 않음. stale client가 handshake OK 후 신규 패킷을 unknown drop할 수 있음 | MEDIUM | 즉시 봉합 | `ProtocolVersion.Current = 2`, handshake는 exact equality, Unity는 unknown packet warning 후 drop |
| Phase 06 완료조건은 "rate-limit 거절"인데 pin/학습 보존은 "silent drop"이라고 되어 있어 테스트 기대값이 갈림 | MEDIUM | 즉시 봉합 | Phase 06 완료조건 line 38, current-pin rate-limit drop vs reject note |
| Phase 06 2.5h는 서버 코드만이면 가능하지만 client spawn/HP/death 표시까지 포함하면 낙관적 | MEDIUM | 진입 전 봉합 | UnityClientSession dispatch는 현재 `S_HandshakeResult/S_Pong/S_EnterMap/S_Snapshot/S_PlayerJoin/S_PlayerLeave`만 처리 |
| `98_Shared/CLAUDE.md`의 packet ID 범위 예약과 실제 PacketGenerator 순차 ID 정책이 불일치 | LOW | M4 후속 | 실제 `Program.cs`는 append 순서 `++packetID`만 지원 |
| `98_Shared/Protocol/PDL.xml` 문서 경로와 실제 PDL 경로가 다름 | LOW | 진입 후 봉합 | 실제 파일은 `99_Tools/PacketGenerator/PDL.xml` |
| 점프 Y축 mispredict는 보안/권위 문제는 아니지만 2D 거리 판정이면 시연 중 점프 공격 miss로 보일 수 있음 | LOW | M4 후속 | Phase 05 DONE의 known follow-up. 전투는 서버 위치만 쓰면 헌법 위반 없음 |

## 요청 항목별 판정

### (A) PDL 설계 — 4 신규 패킷

판정: 현재 정의 그대로는 진입 비권장. **4패킷 구성 자체는 유지하되 내용은 교체**한다.

`C_Attack`은 `targetEntityId` 모델을 선택한다. attacker는 packet에 넣지 말고 `GameSession._entityId`에서만 결정한다. direction 모델은 응급에서 보기엔 자연스럽지만, facing/ray/hitbox/클라 입력 조작 검증이 붙어 Phase 06 범위를 초과한다.

`S_HitResult`와 `S_EntityHpUpdate`는 통합한다. 응급은 `S_HitResult { attacker, target, damage, currentHp, maxHp }` 하나면 damage text와 HP bar를 동시에 처리할 수 있다. 대신 누락된 `S_EntitySpawn`을 4패킷 안에 넣어야 한다.

`S_EntityDeath`와 `S_StageClear`는 분리한다. death는 entity lifecycle, stage clear는 game event/UI다. boss death 시 서버가 `S_EntityDeath`와 `S_StageClear`를 각각 broadcast하되, stage clear는 서버의 1회 플래그로 막는다.

`ProtocolVersion.Current`는 `2 -> 3` bump 권장이다. 신규 패킷 추가만 보면 additive지만, 현재 handshake가 exact equality이고 데모 기능이 신규 패킷 의존이라 stale client를 빨리 끊는 편이 낫다.

### (B) 헌법 정합

판정: 방향은 맞고, 권위 검사 목록을 코드 진입 전에 고정해야 한다.

필수 검사: attacker는 session entity로 강제, target exists/type enemy/boss/alive 확인, 같은 map 확인, server authoritative position만으로 range 검증, server-side cooldown 500ms, damage 고정 10은 server only. client가 보낸 position/damage/attacker는 받지 않는다.

rate-limit 초과는 응급에서 silent drop으로 충분하다. 단 완료조건의 "거절" 표현은 "no HP change + no broadcast"로 바꾸는 편이 테스트와 일치한다. demo 시각 피드백은 local swing animation으로 처리하고, 서버 HP 변화만 권위로 둔다.

틱 블로킹 위험은 낮다. `AttackHandler`는 기존 `MoveIntentHandler`처럼 decode 후 `session.SubmitAttack(...)`만 호출하고, map/entity mutation은 `GameMap.EnqueueJob` 안에서 처리해야 한다. handler나 tick 안에 `await`, `Task.Delay`, `Thread.Sleep`, DB 호출을 넣지 않는다.

### (C) 시간 추정 — 2.5h 현실성

판정: 서버-only 2.5h, end-to-end demo는 4~5h가 더 현실적이다.

현재 범위에는 PDL 재생성, ProtocolVersion bump, Shared.dll 반영, server entity model, attack handler, tests, Unity dispatch, enemy spawn 표시, HP/death 표시가 모두 들어간다. 특히 기존 Unity는 combat packet dispatch가 전혀 없다.

Option B를 사전에 정의해야 한다. fallback은 `C_Attack + S_EntitySpawn + S_HitResult(currentHp/maxHp 포함)` 3패킷으로 가고, 별도 `S_EntityDeath`는 생략한다. client는 server가 보낸 `currentHp == 0` 상태를 보고 despawn만 한다. StageClear는 Phase 07의 `S_StageClear`로 반드시 별도 유지한다.

### (D) Phase 06 -> 07 연속성

판정: `BossEntity` subclass보다 `EnemyEntity.Kind` 또는 `IsBoss` 플래그가 응급에 맞다.

Phase 07 boss는 combat 로직상 normal enemy와 다른 행동이 없다. HP와 위치, stage clear trigger만 다르다. 따라서 `EnemyEntity { Kind, Hp, MaxHp, Position, IsDead }` 하나로 두고, `Kind == Boss && !_stageClearSent`일 때 `S_StageClear`를 broadcast하는 편이 빠르다.

death broadcast 패턴은 Phase 06 그대로 재사용 가능하다. 핵심은 `S_EntityDeath`를 idempotent하게 한 번만 보내는 것과, boss stage clear flag를 death와 별도로 한 번만 세팅하는 것이다.

commit은 Phase 06과 07을 분리하는 것을 권장한다. 5/20 응급이면 한 PR 안에 두 commit까지는 허용하되, `combat infra`와 `boss/stage clear`를 한 commit으로 섞지 않는 편이 회귀 원인 추적이 쉽다.

### (E) 알려진 후속

판정: Phase 05 점프 Y축 mispredict는 Phase 06 보안/권위에는 영향 없다.

전투 판정이 client position을 쓰지 않고 server authoritative position만 쓰면 헌법 #1/#3은 유지된다. 다만 2D distance에 Y를 넣으면 시연 중 점프 공격이 기대와 다르게 miss로 보일 수 있으므로, 응급 시연은 지상 공격 위주 또는 range를 넉넉하게 잡는 편이 낫다.

Phase 04 broadcast infra는 그대로 재사용한다. `GameMap.BroadcastToAll`은 closing session skip을 이미 가지고 있으므로, hit/death/stage clear도 tick job 안에서 이 통로로 보내면 된다.

enemy 제거에는 `GameSession.IsClosing` 같은 lifecycle flag는 필요 없다. session이 없는 server-owned entity라 race 축이 다르다. 대신 `IsDead` 또는 remove-before-broadcast + null no-op으로 중복 attack job을 idempotent 처리해야 한다.

### (F) 응급 trade-off

판정: trade-off는 정합하다. 다만 "보이기"를 위해 spawn/HP 표시만은 빠지면 안 된다.

`dist² < range²`는 지금도 M4에서도 맞는 패턴이다. N이 작아서 sqrt 비용은 사실상 무의미하지만, squared distance는 더 단순하고 표준적이라 유지해도 된다.

enemy AI 없음 + 고정 위치는 충분하다. 좌우 왕복은 enemy snapshot/broadcast까지 늘려야 하므로 Phase 06에는 넣지 않는 편이 낫다. 시각 임팩트는 prefab idle animation이나 HP bar로 해결하는 쪽이 싸다.

lag compensation 생략은 응급 모드와 정합하다. PRD/ARCHITECTURE를 지금 갱신할 필요는 낮고, Phase 06 DONE 또는 M4 backlog에 "position history 기반 lag compensation"으로 남기면 충분하다.

### (G) 최종 산출물 판정

판정: **이거 박고 진입**.

코드 진입 전 Phase 06 정의에 최소 반영할 내용:

1. 실제 패킷명은 `C_`/`S_` 규약으로 쓴다.
2. `C_Attack`은 `targetEntityId`로 확정한다.
3. `S_EntitySpawn`을 추가하고, `S_HitResult + S_EntityHpUpdate`는 하나로 합친다.
4. Phase 07은 `S_StageClear` 별도 패킷으로 간다.
5. `ProtocolVersion.Current` bump와 PacketGenerator 재생성/Shared.dll 반영을 완료조건에 넣는다.

## 권장 후속 조치

1. Phase 06 문서와 current-pin의 PDL 라인을 위 4패킷으로 고정한다.
2. completion의 "rate-limit 거절"을 "silent drop: no HP change, no broadcast"로 정정한다.
3. `ProtocolVersion.Current = 3` bump와 관련 round-trip/handshake 테스트 기대값 수정까지 Phase 06에 포함한다.
4. `GameMap`에는 players와 enemies를 분리 보관하고, broadcast 대상은 players만 유지한다.
5. M4에서 PacketGenerator explicit ID 또는 `98_Shared/CLAUDE.md` packet range 문서 정합을 별도 작업으로 잡는다.

## 최종 판정

Blocking runtime defect는 아직 없다. 코드 변경 전 설계 정의 단계에서 막을 수 있는 blocker가 1개 있다.

**enemy spawn/identity 패킷 누락만 즉시 봉합하면 Phase 06 진입 가능**하다. 응급 모드에서는 정밀 전투보다 end-to-end 표시가 우선이므로, 패킷 수를 늘리기보다 중복 packet을 합쳐 spawn 경로를 확보하는 쪽이 맞다.
