---
owner: youngho
phase: 07
title: Enemy AI 서버 — patrol/chase FSM + tick 루프 + 위치 브로드캐스트
status: done
completed: 2026-05-29
grade: 복잡
summary: M4.3 Phase 07 enemy AI 서버 완료. 고정 더미 enemy를 patrol/chase FSM으로 살아 움직이게 + S_EntityState 패킷(PacketID 19) 신설 + ProtocolVersion 6→7 + Normal enemy 5초 respawn(tick 카운트다운) + broadcast 주석 false-promise 8곳 정정. 315 테스트 통과, reviewer 헌법 hard 위반 0.
---

# Phase 07 박제: Enemy AI 서버

## TL;DR

M3에서 응급으로 박은 **고정 위치 더미 enemy를 스스로 움직이는 AI**로 만들었다. Normal enemy가 SpawnX 중심 ±PatrolRange(4.0)를 왕복(patrol)하다가, 플레이어가 AggroRange(6.0) 안에 들어오면 추격(chase)하고, de-aggro 히스테리시스(1.5×=9.0) 밖으로 벗어나면 patrol로 복귀하는 FSM을 `GameMap.Tick` 안에서 돌린다. 적이 움직이므로 위치/상태를 알리는 `S_EntityState` 패킷(PacketID 19)을 신설하고 ProtocolVersion 6→7. Normal enemy는 죽으면 5초(100 tick) 후 respawn(Boss는 StageClear 1회성이라 respawn 없음). 클라 렌더(보간/Animator)는 Phase 08.

## 신설 / 변경 파일

**신설**:
- `02_Server/GameServer/Combat/EnemyState.cs` — FSM enum (Idle=0/Patrol=1/Chase=2), byte cast stable ID
- `02_Server/GameServer.Tests/Combat/EnemyAiTests.cs` — 12개 단위 테스트
- `99_Tools/headless-bot/Scenarios/EnemyAiSmoke.cs` — Patrol→Chase 전환 smoke

**변경**:
- `02_Server/GameServer/Combat/EnemyEntity.cs` — AI 필드 6종 (State/TargetEntityId/SpawnX/SpawnY/PatrolDir/RespawnTicksRemaining), Normal→Patrol / Boss→Idle 초기화
- `02_Server/GameServer/Maps/GameMap.cs` — SpawnEnemy Stats 주입, _respawnQueue + NormalEnemyRespawnTicks=100, ProcessAttack death→_respawnQueue 등록, UpdateEnemies/ProcessRespawns 신설, Tick에 (4)(5) 단계
- `99_Tools/headless-bot/Program.cs` — EnemyAiSmoke 등록
- `98_Shared/GameData/Formulas.cs` — EnemyStats에 MoveSpeed/AggroRange/PatrolRange + NormalDefault() (shared SubAgent, 1단계)
- `99_Tools/PacketGenerator/PDL.xml` + `98_Shared/Protocol/ProtocolVersion.cs` + Generated — S_EntityState(PacketID 19) + Version 6→7 (shared SubAgent, 1단계)

## AC 검증 결과

```
dotnet build Dawnholder.slnx --nologo — 경고 0 / 오류 0
dotnet test --no-incremental --nologo — 315 통과 / 0 실패 / 4 skip (LongRunning 기존 skip 유지, 회귀 0)
ProtocolVersion == 7, S_EntityState PacketID == 19 stable
```

EnemyAiTests 12개 전부 통과:
- Patrol_BounceAtLeftBoundary / Patrol_BounceAtRightBoundary (경계 반전)
- Aggro_TransitionsToChase / Aggro_OutOfRange_StaysPatrol (aggro 진입)
- DeAggro_BeyondHysteresis_ReturnsToPatrol / DeAggro_InsideHysteresis_StaysChase (히스테리시스 양쪽)
- Chase_MovesRight_WhenTargetIsToRight / Chase_MovesLeft_WhenTargetIsToLeft (추격 방향)
- Boss_StaysIdle_WhenPlayerNearby (Boss 제외)
- Respawn_NormalEnemy_ReappearsAfterTicks / Respawn_Boss_NeverRespawns (respawn 정책)
- EntityStateBroadcast_OnSnapshotInterval (broadcast 주기)

완료 조건 충족: 빌드 통과 / 회귀 0 + 신규 12개 / Protocol.Version 7 / spawn 종속성 GameMap 단일 책임 확인 / respawn 구현 + 사유 기재. (EnemyAiSmoke는 서버 실행 수동 확인용 — 단위 테스트로 로직 검증 완료)

## 결정 흐름

- **EnemyState 위치 = server Combat/** (shared 아님): EnemyKind 패턴 정합 — 클라는 PDL state(byte)만 디코드. server-only enum으로 일관성.
- **respawn = 별도 큐 + tick 카운트다운** (헌법 #5 정석): `await Task.Delay` 대신 `RespawnTicksRemaining--` 매 tick 감소 → 0에서 spawn. 블로킹 0 + 단일 thread + 결정론. 별도 리스트(`_respawnQueue`)로 "살아있는 적만 `_enemies`" invariant 유지.
- **respawn 5초(100tick)**: 1초는 너무 짧아 플레이어 충격, 10초는 데모 흐름 끊김. 5초가 데모 반복 시연에 자연스러움. 새 entityId 발급 = 헌법 #2 "은퇴 ID 재사용 금지" 정합.
- **broadcast 주기 = SnapshotTickInterval(2, 100ms)**: player S_Snapshot과 동기. 적이 느려(MoveSpeed 2.0, 100ms에 0.2 unit) 보간 충분. Phase 08 체감 보고 후 조정 여지.
- **enemy MoveSpeed 2.0 보수적**: player(4~6) 절반 이하. target rewind 미적용(M4.4 이월)이라 빠르면 조준-판정 어긋남 → 느린 속도로 회피.
- **히스테리시스 1.5×**: aggro 진입 6.0 / 이탈 9.0 비대칭 — 경계 flickering 차단.
- **Boss AI 제외**: `if (enemy.Kind != EnemyKind.Normal) continue` — Idle 고정. Boss behavior는 Phase 09.

## reviewer Tier 2-A 결과

- **헌법/ADR hard 위반 0개** — #1(서버 권위 AI) / #2(PDL append-only + respawn 새 id) / #5(tick 동기, blocking 0) 전부 통과. Map=Actor 유지.
- 🎓 칭찬: 히스테리시스 교과서적 + respawn tick 카운트다운이 헌법 #5 정석.
- 🟡 backlog (선택 — M4.4 또는 Phase 08/09 이월):
  - aggro 타이브레이크(동률 2 player) 테스트 1개 / respawn 2회차 사이클 테스트 1개
  - **chase 경계 clamp 없음** — de-aggro 1.5× + target lost Patrol 복귀로 현재 안전하나, player가 enemy를 portal 너머로 유인 시 맵 경계 밖 좌표 가능. Phase 08/09 clamp 권장.
  - O(N·M) aggro 스캔 — 데모 무시 가능, M5+ spatial partition backlog

## broadcast 주석 false-promise 정정 (메인 세션)

reviewer가 잡음: 코드 주석 8곳(GameMap.cs 7 + PDL.xml 1)이 옛 `SnapshotTickInterval=5`(250ms)로 stale. 실제값은 2(100ms). server SubAgent 신규 코드도 옛 값 "250ms@2tick"(산수 오류)을 베껴옴. 메인 세션이 8곳 전부 `2 / 100ms`로 정정 (코드 동작 무관, 주석만). 본 DONE.md의 broadcast 결정은 처음부터 정확했음.

## M4.4 이월

- **🔴 target rewind** (정밀 전투): EnemyEntity position history + ProcessAttack target rewind. 적 이동으로 필수화 — M4.3는 적 MoveSpeed 2.0 보수적으로 회피만 (MAX effort 재검토 발견, 2026-05-29 사용자 결정).

## 학습 일지 후보 키워드

- **★★★ FSM(유한 상태 기계) enemy AI** (`enemy-fsm-patrol-chase`) — enum + switch로 Idle/Patrol/Chase 모델링.
- **★★★ 히스테리시스로 aggro flickering 차단** (`aggro-hysteresis`) — 진입/이탈 임계 비대칭(1.5×).
- **★★★ tick 카운트다운 respawn** (`tick-countdown-respawn`) — await 없이 RespawnTicksRemaining-- 로 헌법 #5 정합.
- **★★ 서버 권위 AI** (`server-authoritative-ai`) — enemy 판단 전부 서버 tick, 클라는 S_EntityState 받아 표시만.
- **★★ false-promise 주석 정정** (`stale-comment-snapshot-interval`) — 실제 상수(2)와 주석(5) 불일치 8곳. 신규 코드가 옛 stale 값을 베껴오는 함정.

## 다음

- Phase 08: client SubAgent — S_EntityState 수신 + enemy 위치 보간 + 렌더
