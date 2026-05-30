---
owner: youngho
phase: 08a
title: 애니 상태머신 — 프로토콜 + 서버 (AnimState enum + animState 필드 + 서버 상태 결정 broadcast)
status: done
completed: 2026-05-30
grade: 복잡
summary: M4.3 Phase 08a 애니 상태머신 프로토콜+서버 완료. AnimState enum 신설(EnemyState AI상태와 분리된 시각 레이어) + S_Snapshot/S_EntityState에 animState byte append-only + ProtocolVersion 7→8 + 서버 권위 상태결정(player vx/grounded/이벤트, enemy AIstate/이벤트) + Attack/Hit latch 8틱(400ms, tick카운터). 338 테스트 통과, plan-auditor/reviewer 🔴0. 클라 렌더는 08b.
---

# Phase 08a 박제: 애니 상태머신 — 프로토콜 + 서버

**소요**: ~1 세션 (server SubAgent 위임 + reviewer)

## TL;DR

애니메이션 상태머신(메이플 스타일)의 **데이터 기반**을 깔았다. 서버가 각 entity(플레이어 + 적/보스)의 시각 애니 상태(Idle/Walk/Jump/Attack/Hit/Death)를 1바이트 `animState`로 **권위 결정**해, 기존 위치 스냅샷(`S_Snapshot`/`S_EntityState`)에 append-only로 동봉 broadcast한다. AI 행동상태(`EnemyState`: Patrol/Chase)와 애니 상태(`AnimState`)를 **별 레이어로 분리**(Patrol·Chase 둘 다 Walk로 매핑)해 시간적 결합을 막았다. Attack/Hit 같은 순간 이벤트는 **latch 8틱(400ms)** 으로 유지해 20TPS에서 클라가 놓치지 않게 했다. 클라 렌더(AnimatorDriver)는 Phase 08b.

## 5단계 보고

- **무엇을 만들었나** — `AnimState` enum(98_Shared) + `S_Snapshot`/`S_EntityState`의 `animState` byte 필드 + 서버 상태결정 로직(`ComputePlayerAnimState`/`ComputeEnemyAnimState` static helper) + latch 시스템(`AnimLatchTicks=8`, attack/hit 카운터). `Protocol.Version` 7→8.
- **왜 필요한가** — jump/attack/hit/death 같은 상태는 위치(좌표)로 추론 불가. 원격 플레이어·적이 같은 모션을 같은 타이밍에 보이려면 서버가 권위 결정해 보내야 한다(헌법 #1). 위치만 보내고 애니를 빼먹으면 "미끄러짐"이 된다.
- **어떻게 만들었나** — A안(서버 권위 byte 통일) 채택. player는 vx(Walk/Idle)/grounded(Jump)/공격·피격·사망 이벤트, enemy는 AI state(Patrol·Chase→Walk)/이벤트로 우선순위 **Death>Hit>Attack>Jump>Walk>Idle** 계산. latch는 ms 타이머가 아닌 tick 카운터(헌법 #5). PDL은 각 패킷 맨끝 append-only.
- **테스트 결과** — `AnimStateTests` 17개 신규 + `dotnet build --no-incremental` 경고 0/오류 0 + `dotnet test --no-build` 통과 338/실패 0/skip 4. plan-auditor 🔴0 GO, reviewer 🔴0(모순 주석 1건 정리).
- **다음 스텝** — Phase 08b: 클라가 `animState`를 `IMotionState`+`AnimatorDriver`(전략 패턴)로 렌더 + enemy 위치 보간(기존 `RemoteEntity` 컴포넌트 재사용).

## 신설 / 변경 파일

**신설**
- `98_Shared/GameData/AnimState.cs` — 시각 애니 상태 enum (byte: Idle=0..Death=5)
- `02_Server/GameServer.Tests/AnimStateTests.cs` — animState 결정 로직 17개 테스트

**수정**
- `99_Tools/PacketGenerator/PDL.xml` — S_Snapshot + S_EntityState 맨끝 animState append, v8 주석 정정
- `98_Shared/Protocol/ProtocolVersion.cs` — Current 7→8 + v8 이력
- `98_Shared/Protocol/Generated/GenPackets.cs` — PacketGenerator 재생성 산출물
- `02_Server/GameServer/Combat/CombatConstants.cs` — `AnimLatchTicks = 8`
- `02_Server/GameServer/Maps/PlayerEntity.cs` — AttackLatchTicks / HitLatchTicks / IsDeadAnimState 필드
- `02_Server/GameServer/Combat/EnemyEntity.cs` — AttackLatchTicks / HitLatchTicks 필드
- `02_Server/GameServer/Maps/Systems/CombatSystem.cs` — 공격 성공 시 attacker.AttackLatch + target.HitLatch 설정
- `02_Server/GameServer/Maps/GameMap.cs` — Physics 루프 뒤 latch 감소 + ComputePlayerAnimState + S_Snapshot animState 주입
- `02_Server/GameServer/Maps/Systems/EnemyAISystem.cs` — latch 감소 + ComputeEnemyAnimState + S_EntityState animState 주입
- `02_Server/GameServer.Tests/PacketRoundTripTests.cs` — S_Snapshot 크기 32→33 (animState 1byte)

## AC 검증 결과

```
$ dotnet build Dawnholder.slnx --no-incremental
  빌드했습니다. 경고 0개 / 오류 0개

$ dotnet test Dawnholder.slnx --no-build
  통과! - 실패: 0, 통과: 338, 건너뜀: 4, 전체: 342 (1m 43s)
  (신규 AnimStateTests 17개 포함, LongRunning 4 skip = 기존 유지, 회귀 0)
```

- `Protocol.Version == 8` 확인 (ProtocolVersion.cs:45 Current=8)
- PDL append-only: animState가 S_Snapshot(PDL.xml:73)·S_EntityState(PDL.xml:241) *각 맨끝*에만 추가, 기존 필드 순서/PacketID 재사용 0
- PacketGenerator 산출물 3종 동반(PDL.xml + GenPackets.cs + Shared.dll)
- reviewer Tier 2-A: 헌법 #1/#2/#5 정합, blocking 위반 0

## 결정 흐름 (회고 참고용)

- **A안 vs B안** → A안(서버 권위 animState byte 통일) 채택. B안(hit=S_HitResult/death=S_EntityDeath/attack=새 패킷 클라 조합)은 클라 상태머신이 여러 신호에 흩어져 완성도↓. A안은 원격/적 클라 구현이 "byte 읽기"로 통일 → 전략 패턴 깔끔.
- **latch 8틱** → 메이플 공격/피격 모션 0.4~0.5초. 8틱×50ms=400ms 보수적 최소값. ms 타이머 아닌 tick 카운터(헌법 #5).
- **latch 감소 위치: Physics 루프(매 tick)** → broadcast(2틱 간격)에서 감소하면 8틱이 16틱=800ms로 2배 지속되는 함정. Physics.Step 직후 매 tick 감소로 정확히 400ms.
- **Compute* helper 위치** → snapshot 생성부 옆(GameMap/EnemyAISystem)에 static 순수함수. GameMap 상태 의존 0(헌법 #1).
- **IsDeadAnimState sticky flag** → IsDead와 동치이나 "한번 Death면 계속 Death" 의도를 코드로 명시.

## 막혔던 지점 (있다면)

- **플레이어 HitLatchTicks 미설정 경로**: 적→플레이어 공격이 Phase 09 전엔 없어(PvP 미지원), 필드만 두고 테스트에서만 set. Phase 09 CombatSystem 적 공격 처리 시 채워질 forward-looking 자리. (reviewer 인지 확인, false-promise 아님)
- **Boss broadcast 신설**: Boss가 이전엔 EnemyAISystem에서 `continue`로 완전 스킵 → 이제 매 snapshot tick `S_EntityState` broadcast(animState 채널 확보). 08b가 Boss S_EntityState를 idempotent하게 받는지 확인 필요.
- **모순 주석**: GameMap broadcast 섹션에 latch 위치 고민의 폐기된 사고과정(800ms 우려 등)이 14줄 남아 reviewer 🟡 → 1~2줄로 압축 정리.

## 학습 일지 후보 키워드

- `animState` / 전략 패턴(Strategy pattern) / AI상태 vs 애니상태 레이어 분리(temporal coupling 회피) / latch(순간 이벤트 유지) / append-only 프로토콜 진화 / Protocol.Version bump / 서버 권위 애니(헌법 #1) / tick 카운터 vs ms 타이머(헌법 #5) / 우선순위 상태머신(Death>Hit>Attack>Jump>Walk>Idle)

## 다음 Phase

- **Phase 08b** — 클라가 받은 animState를 `IMotionState`+`AnimatorDriver`로 렌더 (전략 패턴) + enemy 위치 보간(RemoteEntity 재사용). client SubAgent + Unity.
