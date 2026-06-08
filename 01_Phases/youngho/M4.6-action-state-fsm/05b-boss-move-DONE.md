---
owner: youngho
milestone: M4.6
phase: 05b
title: 보스 고정 포탑 → 이동/탐지 구동 (BossMoveState 신설)
status: done
grade: 복잡
risk: trust-boundary (보스 데미지 판정 경로 — 탐지구동으로 트리거 변경)
summary: 제자리 고정 포탑(blind-timer — 쿨다운만으로 telegraph 반복)이던 보스를 이동/탐지 구동으로 전환. Phase 05의 3-State(Idle/Telegraph/Attack)에 BossMoveState를 더한 4-State FSM. Idle(dwell+탐지)→Move(접근/배회)→{사거리→Telegraph→Attack→Idle | 배회종료→Idle}. 몬스터 AI 부품(MoveChase/MovePatrol/FindClosestInAggro) 재사용. BossDefault stat(MoveSpeed/AggroRange/PatrolRange) 비영(非零)화 + AggroOnSight. 순수 서버 — wire State=Idle 고정 유지로 클라 0줄·프로토콜 v9 불변(걷는 시각은 animState=Walk 한 byte로만). build 0/0 · test 471 passed · reviewer 🔴0.
---

# Phase 05b DONE — 보스 이동/탐지 구동

> 브랜치 `feature/m4.6-boss-move` · 커밋 `db0d9a3`(server+shared+tests+Shared.dll) · 2026-06-08 (세션27)
> 설계 plan 저장: `~/.claude/plans/delegated-prancing-gray.md` (Stage 2). 사용자 승인 후 착수.

## TL;DR

Phase 05까지 보스는 **제자리 고정 포탑**이었다 — 탐지도 이동도 없이 쿨다운 타이머(blind-timer)만으로
"예고→자기 주변 AoE"를 반복. 사용자가 **살아 움직이는 보스**를 원해, Phase 05의 3-State에
`BossMoveState`를 더한 **4-State FSM**으로 확장했다.

- **기본(타겟 없음)**: `Idle → Move(배회) → Idle` 반복.
- **탐지 시**: `Idle → Move(타겟 접근) → Telegraph → Attack → Idle(재탐지)`.

옛 blind-timer 공격은 **폐기**되고 공격이 **탐지+사거리 도달 구동**으로 바뀌었다(타겟 없으면 배회만, 공격 X).
몬스터(Phase 04)의 AI 부품 `MoveChase`/`MovePatrol`/`FindClosestInAggro`를 그대로 빌려 재사용 90%.

핵심은 **순수 서버 작업**이라는 점 — 보스가 움직여도 이미 쏘던 `S_EntityState`(x/y + animState)에 실리고,
클라는 보스를 일반 몬스터와 동일 경로(EnemyRegistry→RemoteEntity 150ms 보간→AnimatorDriver)로 렌더한다.
**클라 0줄, 프로토콜 v9 불변.** (옛 "v10급/protocol bump" 추정은 탐색으로 오판 정정됨.)

## 설계 — 4-State 그래프

| State | 역할 | 전환 |
|---|---|---|
| **BossIdleState** | `AttackCooldownTicks` dwell 카운트다운 → 0 도달 시 탐지(FindClosestInAggro) 후 Move | → Move (항상) |
| **BossMoveState**(신설) | 타겟 有: MoveChase 접근, 사거리(BossAttackTriggerRange) 안 → BeginTelegraph. de-aggro(>AggroRange×1.5) → 배회.<br>타겟 無: MovePatrol 배회(BossWanderTicks) 소진 → Idle. 매 틱 재탐지. | → Telegraph / Idle |
| **BossTelegraphState** | (Phase 05 그대로) 예고 카운트다운 | → Attack |
| **BossAttackState** | (Enter) ApplyBossAttack + 쿨다운 리셋. (Tick) Idle 복귀 | → Idle |

- **쿨다운 = Idle dwell 통합**: 별도 필드 없이 `AttackCooldownTicks`가 Idle dwell을 겸함.
  Attack 후 = 40/24틱(긴 리듬), 배회 종료 후 = `BossIdlePauseTicks` 10틱(짧은 숨). Idle이 카운트다운 소유.
  → Phase 05의 `BossAttackState.Tick` off-by-one `cooldown--`은 **제거**(blind-timer 폐기로 불필요).
- **wire State = Idle 고정 유지(v9 안전 핵심)**: `BossMoveState`는 `enemy.State`를 *건드리지 않음*.
  EnemyState enum 신규값 0. 걷는 *시각*은 `ComputeBossAnimState`가 `boss.Fsm.AnimState`(Move→Walk) 위임으로만.
- **BeginTelegraph 헬퍼 추출**: 옛 BossIdleState의 telegraph 셋업(예고틱+latch+broadcast)을 정적 헬퍼로
  빼 Move가 "사거리 도달" 트리거로 호출(Rule of three 정합).

### BossDefault stat (98_Shared, 데이터만 — v9 불변)
MoveSpeed=1.5(Golem 1.2~Normal 2.0 사이, 느리고 위압적) / AggroRange=7.0(넓은 감지) /
PatrolRange=4.0(배회 폭, invariant PatrolRange<AggroRange ✓) / AggroOnSight=true(능동 탐지). **전부 tunable**.

### 신규 서버 상수 (CombatConstants — 서버 전용)
BossAttackTriggerRange=2.5(≈BossAttackHalfExtent) / BossIdlePauseTicks=10 / BossWanderTicks=20.

## AC 검증 결과

| 항목 | 결과 |
|---|---|
| WSL2 빌드 | **0 Error / 0 Warning** (`dotnet build Dawnholder.slnx -c Debug`) |
| 테스트 | **471 passed / 0 failed / 4 skipped** (`dotnet test GameServer.Tests --no-build`) — Phase 05 대비 +2(신규 5 추가, blind-timer 테스트 폐기/재작성) |
| reviewer 5축 | **🔴 0 / 🟡 3**(전부 테스트 보강 권고 — 3건 모두 반영) / 🟢 헌법#1·#2·#5 통과 |
| 프로토콜 | v9 불변 — PDL/ProtocolVersion 무변경, EnemyState enum 신규값 0 (`ProtocolVersion_Is9` 통과) |
| 클라 영향 | 0줄. Shared.dll API 표면 불변(BossDefault 메서드 *본문* 값만 변경) → 클라 재컴파일 불필요 |
| 메인 검수 | 생산 6파일 스펙 1:1 일치 확인(일탈 0 — Phase 05 일탈 학습 반영해 정밀 스펙 선제) |

검증 명령(독립 재실행):
```
wsl -e bash -lc 'cd /mnt/c/Dev/ClaudeDev && ~/.dotnet/dotnet build Dawnholder.slnx -c Debug'
  → Build succeeded. 0 Warning(s) 0 Error(s)
wsl -e bash -lc 'cd /mnt/c/Dev/ClaudeDev && ~/.dotnet/dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --no-build -c Debug'
  → Passed! Failed: 0, Passed: 471, Skipped: 4, Total: 475
```

## Play 실측 (완료, 2026-06-08)

사용자 직접(Unity) 실측 — 이동형 보스 **체감 OK**: 배회(걷는 Walk 애니) → 접근 시 탐지→추격→예고→공격.
클라 무수정인데 보스가 *걷는 모습* + Walk 애니 바로 보임 = **렌더 경로 재사용 실증**.

**Play 봉합 2건** (커밋 `2a67308`, 순수 클라, v9 불변):
1. **공격 시 플레이어 바라보기(facing)** — 정지 상태로 공격 중엔 이동 dx가 없어 facing이 옛 추격 방향에
   멈춤(공격 끝→즉시 재공격 시 플레이어 반대편 봄). `EnemyMotion`이 animState=Attack·정지일 때 로컬 플레이어
   쪽으로 보정. facing은 시각 전용(판정 AABB는 서버 좌우 대칭이라 데미지 무영향). client 결정(A안: v9 불변 vs B안 v10 서버권위 facing 중 A).
2. **찌르기↔히트 동기** — 옛 Animator가 Stabbing_Start→End를 exit-time 자동전환(클립-시계)이라 서버 히트와
   jitter로 어긋남("가끔 판정이 찌르기보다 먼저"). **Strike 트리거**로 교체 → S_EnemyAttack(권위 타격) 도착
   순간 찌르기 발동 → 모션이 데미지와 동기. 경로: EnemyAttackHandler→EnemyRegistry.NotifyStrike→
   AnimatorDriver.FireStrike→SetTrigger. Boss_Animator.controller = netcode 동기 기능 wiring(MCP, 백업 떠둠).
   핵심 교훈: **애니메이션 시계(클라 클립) ≠ 권위 시계(서버 히트)** — 둘을 묶어야 동기. (이건 Phase 05 고정 보스
   때부터 있던 잠복 이슈, 첫 Play에서 발현. 이동 로직과 무관.)

**후속 polish (deferred, 사용자: "나중에 더 다듬자")**:
- 미스(닷지) 시 허공 찌르기 — 현재는 히트 없으면 준비자세→Idle(찌르기 X). 빗나가도 찌르려면 서버 miss 신호 필요.
- de-aggro MovePatrol patrol-bound snap(reviewer #4) — Play에서 사용자 미플래그(눈에 안 띔). 관찰 종료.

## 결정 흐름

1. **시퀀싱**: Phase 05(회귀0 리팩토링)를 *먼저 닫고* 보스-이동을 별도 브랜치로(사용자 선택). 신규 행동이 Phase 05의 "회귀 0" 스토리를 흐리지 않게 분리.
2. **순수 서버 확정**: 탐색 2건으로 "보스 이동 = S_EntityState 재사용 + 클라 0줄 + v9 불변" 확인(옛 v10급 추정 오판 정정).
3. **wire State 처리**: Patrol enum 세팅 대신 *State 미변경(Idle 고정)* 선택 — Phase 05 불변식 "wire State=Idle 고정" 정확 보존 + animState로만 Walk. v9 최대 안전.
4. **쿨다운 통합**: 새 필드 추가 대신 AttackCooldownTicks를 Idle dwell로 겸용(post-attack 긴 리듬 / 배회후 짧은 숨). off-by-one cooldown-- 제거.
5. **Worker 정밀 스펙**: Phase 05의 Worker 2-State 일탈 사고 학습 → 한 줄도 해석 여지 없는 스펙 선제 작성. 결과 일탈 0.
6. **테스트 보강**: reviewer 🟡 3건(trivially-passing assert / 연속2회 / de-aggro) 즉시 반영 — 신규 행동 경로의 회귀 안전망.

## 회귀 안전망

신규/재작성 테스트(BossStateTests 5 + BossBehaviorTests 보스이동 항목):
- `BossIdle_DwellEnds_TransitionsToMove` — Idle dwell 소진 → Move(Telegraph 아님).
- `BossMove_InRange_BeginsTelegraphBroadcast` — 사거리 도달 시 S_EntityState(animState=Attack) broadcast.
- `Boss_NoTargetInAggro_WandersWithoutAttacking` — aggro 밖 player → 배회(이동) + 공격 0.
- `Boss_DetectsAndApproaches_ThenAttacks` — aggro 안·trigger 밖 player → 접근 이동(단독 assert) + 공격.
- `Boss_StaysInRange_AttacksRepeatedly` — 연속 ≥2회(Attack→Idle→Move→Telegraph→Attack 풀 루프, 쿨다운/dwell 통합 회귀망).
- `Boss_TargetFleesBeyondDeAggro_StopsChasingAndReturnsToWander` — de-aggro 시 타겟 해제 + 도주 후 공격 0(영원 추격 버그 방지).
- `Boss_InMoveState_BroadcastsWalkAnimState` — Move 중 animState=Walk(시각 경로 실증).
- 회귀 유지: Phase2 전환/데미지공식/리스폰/animState 우선순위(Attack>Hit)/직렬화 왕복/ProtocolVersion=9.

## 학습 일지 후보 키워드

- **논리 State ↔ wire enum 분리로 프로토콜 불변 유지**: 보스 "걷는다"는 *시각*을 EnemyState enum 신규값 *없이* animState=Walk 한 byte로만 표현 → 헌법 #2 "필드 추가=breaking" 함정 우회. 대가 = "latch가 항상 AnimState 분기를 먼저 잡아야" 불변식(telegraph+AnimLatchTicks 합)을 코드로 보장해야 성립. (reviewer 학습 포인트)
- **쿨다운=Idle dwell 통합**: 인터-액션 리듬과 배회 숨을 한 카운터(AttackCooldownTicks)에 reload 값만 달리해 통합 → 신규 필드 최소화. blind-timer 폐기로 off-by-one 보정도 제거(역으로 Phase 05의 보정이 *그 모델 전용*이었음).
- **탐지구동 전환 시 테스트 함정 = 배회가 만드는 동적 aggro 가장자리**: "범위 밖" 테스트가 보스 배회 가장자리(x=26)에서 player(x=32)를 탐지해 깨짐 → player를 PatrolRange+AggroRange+margin(x=37) 밖에 둬야. blind-timer→탐지구동은 정적 위치 가정을 전부 무효화.
- **Worker 정밀 스펙 = 일탈 예방**: Phase 05 2-State 일탈 사고 후, 4-State 코드를 *그대로 적을 수 있는* 수준으로 스펙 작성 → 일탈 0. "이게 더 나으면 구현은 스펙대로 + 메모만" 명시가 효과.
