---
owner: youngho
phase: 10
title: 서버 입력 큐 fix — rubber-band 근본 해결 (coalescing + ack-on-receive)
status: done
completed: 2026-05-30
grade: 복잡
risk: trust-boundary
domain: server
summary: M4.3 Phase 10 서버 입력 큐 fix 완료. 장시간 이동 시 뒤로 snap하던 rubber-band를 서버 입력 처리에서 근본 제거 — 단일 슬롯(coalescing 드롭) → 바운드 FIFO 큐(cap 6, drop-oldest) + 틱당 Physics.Step 정확히 1회(멀티드레인 금지) + ack를 적용 시점 clientTick으로(수신 시점 아님). 서버 단독(클라/프로토콜/Shared 0건, ProtocolVersion bump 없음). 클라는 이미 교과서대로 정확했고 서버가 계약을 어긴 것. plan-auditor 2회 GO + reviewer 🔴0 + build 0/test 345 green + Play 실측(수평 rubber-band 0 / 큐 drop 0). 점프 snap은 별개 root cause(클라 가변 dt 예측 vs 고정 step replay)라 Phase 10b로 분리.
---

# Phase 10 박제: 서버 입력 큐 fix (rubber-band 근본 해결)

**소요**: ~1 세션 (측정 → 설계 → plan-auditor 2회 → server SubAgent → reviewer → Play 실측)

## TL;DR

세션3에서 클라 reconcile에 잔차/smooth를 2번 박았지만 실패("정확한 fix 아니다" 2회 지적). 세션4 코드 측정으로 진짜 원인은 **서버 입력 처리**였음을 확정: 서버가 클라 입력을 **단일 슬롯**에 받아 (a) 새 입력이 오면 덮어써 드롭(coalescing), (b) 빈 틱엔 input=0(제자리), (c) `LastClientTick`을 *수신* 시점에 박아(ack-on-receive) 적용 안 한 입력까지 ack → 클라가 replay할 입력을 다 지워(replayed=0) reconcile 무력화. 클라 송신·서버 틱이 free-running이라 위상 어긋나면 lead 단조 누적(0.3→0.69) → snap. **fix = 단일 슬롯을 바운드 FIFO 큐로(드롭 제거) + 틱당 입력 1개 적용 + ack를 적용 시점으로.** 클라(`PlayerPredictor`/`InputHistory`/`LocalPlayerController`)는 교과서대로 정확 → 변경 0.

## 5단계 보고

- **무엇을 만들었나** — `PlayerEntity`에 `InputCommand` struct + 바운드 `Queue`(`MaxInputQueue=6`, drop-oldest) + `EnqueueInput`/`TryDequeueInput`. `GameSession.SubmitMoveIntent`가 단일 슬롯 set → 큐 enqueue. `GameMap.Tick`이 큐서 1개 dequeue 적용(비면 neutral) + ack=적용 clientTick. 신규 `InputQueueTests` 7개 + 기존 테스트 마이그레이션.
- **왜 필요한가** — 단일 슬롯 coalescing이 입력을 영구 드롭 → 서버 적용 이동 < 클라 예측 이동 → lead 누적 → rubber-band. ack-on-receive가 클라 replay를 무력화 → reconcile이 lead를 영영 못 따라잡음. 입력 버퍼링은 client prediction의 서버측 짝 — "1입력=1틱=1덩어리" 계약을 서버가 지켜야 정합.
- **어떻게 만들었나** — **틱당 `Physics.Step` 정확히 1회**(멀티드레인 금지). 실시간 fixed-timestep 시뮬은 물리시간=벽시계라 한 틱에 2스텝 적용 시 중력 over-count + lag comp `RecordPosition` serverTick 중복으로 깨짐(plan-auditor 코드 confirm). 버퍼는 *지연*이지 *발산* 아님 — 발산은 입력 드롭이 원인이고 큐가 제거. 신뢰 경계(헌법 #3) = 큐 상한 + drop-oldest로 메모리 DoS 방어, rate limiter 별개 유지.
- **테스트 결과** — `dotnet build Dawnholder.slnx --no-incremental` 경고 0/오류 0. `dotnet test` 실패 0/통과 345/skip 4. reviewer Tier 2-A 🔴0. Play 실측: 수평 rubber-band 0(사용자 확인) / 큐 `cap drop` 0(상한 6 적정).
- **다음 스텝** — Phase 10b: 점프 snap(클라 fixed-timestep accumulator) + β10 MoveSpeed dead. 10 머지 직후 측정 1순위(발표 데모 MoveSpeed 체감 필요). (병렬) 09 boss / 11 본인 Animator.

## 신설 / 변경 파일

**신설**
- `02_Server/GameServer.Tests/InputQueueTests.cs` — 7개: coalescing 방지 / 빈 틱 ack 불변 / FIFO 순서+ack=적용 / 상한 drop(count 유지+oldest 확인) / 중력 회귀.

**수정** (서버 단독)
- `02_Server/GameServer/Maps/PlayerEntity.cs` — 단일 슬롯(`PendingInputX`/`PendingJumpPressed`) → `InputCommand` struct + 바운드 큐 + enqueue/dequeue 메서드. `LastClientTick` 적용 시점 set.
- `02_Server/GameServer/Network/GameSession.cs` — `SubmitMoveIntent` EnqueueJob: 단일 슬롯 3줄 → `EnqueueInput` 1줄, `LastClientTick` 대입 제거. rate limiter/`InputBits.Decode` 보존.
- `02_Server/GameServer/Maps/GameMap.cs` — Tick 루프: 큐서 1개 dequeue 적용, 비면 neutral(0,false), ack=적용 clientTick(neutral 틱 불변), `PendingInputX=0` 리셋 제거. animState latch/broadcast 보존.
- `02_Server/GameServer.Tests/{MoveIntentTests,AnimStateTests,Network/GameSessionRateLimitTests}.cs` — 단일 슬롯 전제 → 큐 API 마이그레이션 (커버리지 동등/강화).

## AC 검증 결과

```
$ dotnet build Dawnholder.slnx --no-incremental
  빌드했습니다. 경고 0개 / 오류 0개

$ dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --no-build
  통과! - 실패 0, 통과 345, 건너뜀 4, 전체 349

Play 실측 (서버 20TPS 가동, 사용자 Unity Play):
  - 수평 rubber-band 0 — 한 방향 장시간 이동 시 뒤로 snap 사라짐 ✅ (사용자 확인)
  - 큐 drop = 0 — Play(이동+맵전환3+점프) 동안 [InputQueue] cap drop 로그 0
    → 상한 6 적정, clock drift 오버플로 없음. 관측 후 TEMP-METRIC 제거.
  - 점프 snap 잔존 = 별개 root cause(클라 가변 dt) → Phase 10b
```

- plan-auditor Tier 2-B 2회 GO: (1) 범위 재정의(클라 polish → 서버 큐 fix) 측정 좌표 입증, (2) one-per-tick 결정(멀티드레인 금지) 코드 confirm.
- reviewer Tier 2-A 🔴0 / 🟡2: TEMP-METRIC 추적처[처리됨, 제거 완료] / `MaxInputQueue` 매직넘버[향후 Constants, 지금 불필요].

## 결정 흐름 (회고 참고용)

- **클라 fix vs 서버 fix** → 서버. 세션3 클라 잔차/smooth 2회 실패. 측정(replayed=0 항상)이 서버 ack-on-receive를 지목. 클라는 이미 정확.
- **틱당 1입력 vs full-drain(catch-up)** → 1입력. full-drain은 한 틱 다중 `Physics.Step` = 물리시간>벽시계 = 점프 Y over-count + lag comp 깨짐. plan-auditor가 `Physics.cs`(중력 dt 비례) + `RecordPosition`/`GetPositionAtTick`(serverTick 키 중복)으로 confirm.
- **drop-oldest vs drop-newest** → oldest(최신 입력 우선 = 응답성). drop 자체가 미세 lead 재유입이라 cap 6 + drop=0 계측으로 방어(주파수 drift 가설은 측정으로 확인, 단정 X).
- **starvation = neutral(0,false)** → 세계는 계속 흐름(중력/마찰). 단 ack 불변 — 적용 안 한 입력 ack하면 클라 reconcile 무력화. neutral은 vx=0이라 원격 애니 Walk→Idle 1틱 깜빡 가능(Phase 11 헛다리 방지용 기록).

## 막혔던 지점 / 이월

- **점프 snap (별개 root cause → Phase 10b)**: 수평 rubber-band 해결 후 드러남. 원인 = 클라가 `LocalPlayerController.cs:123`에서 `Time.deltaTime`(가변 dt)로 점프 예측 vs 서버/replay 고정 50ms step. 수평은 등속(선형)이라 dt 무관하지만 점프는 semi-implicit Euler 적분이 dt 의존(비선형 궤적) → reconcile이 정확 복원 못 함 → 100ms마다 Y 보정 = 보이는 snap. **버퍼/임계값 문제 아님**(둘 다 증상치료 레버). `Physics.cs` 주석이 이미 "가변 dt 금지" 경고. 진짜 fix = 클라 fixed-timestep accumulator(50ms 고정 step 예측 + 시각 보간).
- **08b DONE의 Phase 10 복선 정정**: 08b는 Phase 10을 "레버 1: replay 후 잔차 비교 + smooth blend"로 예고했으나, 그건 증상치료였음(세션3 실패 확정). 실제 fix는 서버 입력 큐. 08b의 dx/dy ±1.5~1.7 실측은 lead 누적의 증상이었고 근본은 입력 드롭.
- **β10 MoveSpeed dead**: Phase 10에서 분리(독립 root cause). Phase 10b에서 측정.

## 학습 일지 후보 키워드

입력 버퍼링(client prediction의 서버측 짝) / ack 의미론("받았다" vs "적용했다") / 틱당 1스텝 불변식(fixed-timestep, 물리시간=벽시계) / 멀티드레인 금지(over-count + lag comp 손상) / 증상 vs 근본(replayed>0 먼저 검증) / 위상(phase) ≠ 주파수(frequency)(버퍼 자연상쇄 전제 + clock drift) / 신뢰 경계 큐(상한 + drop-oldest = DoS 방어) / coalescing 드롭 / 선형 vs 비선형 적분(수평 OK, 점프 dt 의존 — Phase 10b 복선)

## 다음 Phase

- **Phase 10b** — 점프 snap(클라 fixed-timestep accumulator) + β10 MoveSpeed dead. 10 머지 직후 1순위.
- **(병렬) Phase 09** — boss behavior(+attack animState).
- **(본인 critical path) Phase 11** — 애니 외관 완성(Animator 6상태 클립).
