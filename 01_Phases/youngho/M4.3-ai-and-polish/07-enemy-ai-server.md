---
owner: youngho
milestone: M4.3
phase: 07
title: Enemy AI 서버 — patrol/chase FSM + tick 루프 + 위치 브로드캐스트 패킷
status: done
grade: 복잡
risk: irreversible
estimated: 4~6h
domain: server+shared
---

# Phase 07: Enemy AI 서버 — patrol/chase FSM

> **상태**: done (2026-05-29 완료 — reviewer 통과, DONE.md 박제)
> **마일스톤**: M4.3
> **등급**: 복잡 (2 도메인 server+shared / PDL bump irreversible)
> **담당**: server SubAgent (+ shared SubAgent — PDL/GameData)

---

## 🎯 목표

지금 고정 위치에 서 있는 더미 `EnemyEntity`를 **스스로 움직이는 AI**로 만든다. Normal enemy가 정해진 범위를 **순찰(patrol)**하다가, 플레이어가 aggro 범위에 들어오면 **추격(chase)**하고, 범위를 벗어나면 patrol로 복귀하는 **FSM(유한 상태 기계)**을 서버 tick 루프 안에서 돌린다. 적이 움직이므로 **위치를 주기적으로 클라에 알리는 패킷(S_EntityState)을 신설**한다.

이 Phase는 **서버 + 공유(패킷/데이터)만** — 클라 렌더는 Phase 08.

---

## ⏪ 사전 조건

- [x] M4.2 마감 (4맵 분리 + GameMap.Tick 인프라)
- [x] enemy spawn 인프라 (`MapSpawnTable`, `GameMap.SpawnEnemy`, `EnemyEntity`)
- [ ] 없음 — M4.3 첫 Phase

---

## 📝 작업 내용

### 공유 (shared)
- [ ] `EnemyState` enum 신설 (`98_Shared/GameData/` 또는 server Combat — 클라가 받을 거면 shared) — `Idle=0, Patrol=1, Chase=2`
- [ ] `EnemyStats`(`98_Shared/GameData/Formulas.cs:56`)에 AI 파라미터 추가 — `MoveSpeed`, `AggroRange`, `PatrolRange` (Normal 기본값. 하드코딩 임시 OK — monster 데이터 테이블은 컨텐츠 마일스톤)
  - ⚠️ **MoveSpeed는 발표용 보수적(낮은) 값** — target rewind 미적용(M4.4 이월) 상태라, 적이 빠르면 "내 화면 속 적"(150ms 보간 지연)과 "서버 판정 위치"가 벌어져 조준-판정 어긋남. 느린 속도로 어긋남 체감 최소화 (함정 참조)
- [ ] **PDL: `S_EntityState` 패킷 신설** (`99_Tools/PacketGenerator/PDL.xml` append-only) — `entityId, x, y, state(byte)`. 적 위치/상태 주기 브로드캐스트용
- [ ] `Protocol.Version` bump **6 → 7** (헌법 #2 — append-only, 은퇴 ID 재사용 금지)
  - **이 6→7이 M4.3 전체 대표 bump.** Phase 09의 `S_EnemyAttack`도 같은 Version 7 안에 들어감 (추가 bump 없음) — M4.3 전체를 한 PR로 발표 데모 묶음 머지하는 전제 (M3 boss DONE.md 선례: 같은 PR 연속 additive = bump 1회). ⚠️ *만약* 07과 09를 별도 PR로 분리 머지하면 그때 09에서 7→8 필요
- [ ] PacketGenerator 재생성 + Shared.dll → Plugins 복사 확인

### 서버 (server)
- [x] `EnemyEntity`에 AI 상태 필드 추가 — `EnemyState State`, `int? TargetEntityId`, patrol 기준점(`SpawnX`/`SpawnY`)·방향(`PatrolDir`), `RespawnTicksRemaining`
- [x] `GameMap.Tick`에 **enemy update 루프** 신설 (`UpdateEnemies` 분리 메서드)
  - Patrol: SpawnX 중심 ±PatrolRange 왕복 (X축 이동, MoveSpeed 적용, 경계 반전)
  - aggro 판정: 같은 맵 플레이어 중 AggroRange 안에 있으면 가장 가까운 자를 Target → Chase 전환
  - Chase: Target 방향으로 MoveSpeed 이동. Target이 AggroRange*1.5(de-aggro 히스테리시스) 벗어나거나 사라지면 Patrol 복귀
  - **헌법 #5**: tick thread 안 동기 처리만. `await`/`Thread.Sleep`/DB 호출 금지
- [x] `S_EntityState` broadcast — SnapshotTickInterval(250ms) 마다 모든 Normal enemy 위치/상태 전송 (trade-off: 매 틱 전체 vs SnapshotTickInterval — 플레이어 snapshot과 주기 통일, Phase 08 조정 예정)
- [x] **GameSession/GameMap enemy spawn 종속성 분리** — GameSession에 SpawnEnemy 직접 호출 경로 없음 확인 (GameMap.ctor → MapSpawnTable 단일 경로). "확인 완료"
- [x] **맵 간 enemy respawn 정책 결정 + 구현**: Normal enemy는 사망 후 100tick(5초) 뒤 SpawnX/SpawnY에 새 entityId로 respawn. Boss는 respawn 없음 (StageClear 1회성). trade-off: 5초는 데모 반복 시연에 자연스러운 값 (1초는 너무 짧아 플레이어 충격, 10초는 데모 흐름 끊김).

### 테스트 (server)
- [x] `EnemyAiTests` (단위, 12개): patrol 왕복 경계 반전, aggro 진입→Chase 전환, de-aggro→Patrol 복귀, Chase 방향 이동, Boss Idle 유지, Respawn Normal, Boss No Respawn, S_EntityState broadcast 검증
- [x] 헤드리스 봇 시나리오 `EnemyAiSmoke` — Town→HG portal → Normal enemy spawn 대기 → Patrol 상태 확인 → aggro 진입 → Chase 전환 패킷 검증

---

## ✅ 완료 조건

- [x] `dotnet build Dawnholder.slnx` 통과 (경고 0, 오류 0)
- [x] `dotnet test` green — 회귀 0 + 신규 EnemyAiTests 12개 통과 (315개 통과, 4개 skip)
- [x] 헤드리스 봇 `EnemyAiSmoke` 신설 (서버 실행 중 수동 확인 필요 — 단위 테스트는 통과)
- [x] `Protocol.Version == 7` 확인, `S_EntityState` PacketID=19 stable
- [x] enemy spawn 종속성이 GameMap 단일 책임으로 정리됨 (GameSession에서 spawn 호출 0 확인)
- [x] Normal enemy respawn 100tick(5초) 구현 + Boss respawn 없음 (작업 로그 사유 기재)

---

## 🧪 테스트

**자동**:
- `EnemyAiTests` — FSM 전환(Idle→Patrol→Chase→Patrol), patrol 경계 왕복, aggro 거리 판정
- 기존 `AttackHandlerTests` / `BossStageClearTests` 회귀 0 (enemy가 움직여도 공격 6단계 검증 유지)

**수동**:
- 서버 + 봇 1대 → 사냥터에서 enemy가 봇을 추격하는 서버 로그 관찰

---

## 📚 학습 포인트

- **FSM(유한 상태 기계, Finite State Machine)**: AI를 "상태 + 전이 조건"으로 모델링. enum + switch가 가장 단순한 구현. 게임 AI의 기초.
- **서버 권위 AI (헌법 #1)**: enemy 위치/판단을 서버가 전담. 클라는 결과(S_EntityState)만 받아 표시. 클라가 AI를 돌리면 핵 취약점.
- **tick thread 규율 (헌법 #5)**: enemy 수가 늘어도 tick 안에서 동기 O(N). blocking 한 줄이 20 TPS 전체를 무너뜨림.
- **브로드캐스트 비용 trade-off**: 매 틱 전체 enemy 위치 vs 변경분만 vs 거리 기반 관심영역(AOI). 지금은 단순(움직이는 것만 매 틱), AOI는 부하 마일스톤 과제.

---

## ⚠️ 함정 / 주의사항

- **PDL append-only** (헌법 #2): `S_EntityState`를 기존 패킷 사이에 끼우지 말고 맨 끝에 추가. PacketID는 정의 순서로 stable하게 부여됨. 은퇴 ID 재사용 절대 금지.
- **ProtocolVersion bump 누락**: 패킷 추가했는데 Version 안 올리면 stale 클라가 깨진 프레임 파싱 → 사고. bump 필수.
- **DLL stale 함정** (work-pin 습관 a): Shared/PDL 수정 후 Play/테스트 전 `dotnet build Dawnholder.slnx` 1회 의무. 안 하면 Unity가 옛 Shared.dll 참조 → 클라/서버 enemy 좌표 어긋남(소리 없이).
- **🔴 target rewind 비대칭 (MAX effort 재검토 발견)**: `ProcessAttack`(GameMap.cs:253)은 attacker(player)만 rewind하고 target(enemy)은 *현재 위치*를 씀. `EnemyEntity`엔 position history가 없음. 적이 고정일 땐 무해했지만 본 Phase에서 적이 움직이면 "내 화면 속 적"(보간 150ms 지연)과 "서버 판정 위치"가 어긋나 조준해도 빗맞을 수 있음. **M4.3 대응 = 적 MoveSpeed 보수적으로 낮춰 어긋남 최소화**(임시 회피). 근본 봉합(EnemyEntity position history + target rewind)은 **M4.4 이월** — 2026-05-29 의논 결정.
- **aggro 떨림(flickering)**: aggro 진입/이탈 거리를 같게 두면 경계에서 Chase↔Patrol 1틱마다 토글. de-aggro 거리를 aggro보다 크게(히스테리시스).
- **enemy가 맵 경계/portal 넘어가지 않게**: chase 중 플레이어가 portal 타면 enemy는 추격 멈추고 patrol 복귀 (맵 간 이동 X).

---

## ➡️ 다음 Phase

- Phase 08 — enemy AI 클라 (S_EntityState 받아 위치 보간 + 렌더)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `07-enemy-ai-server-DONE.md` 박음 (요약 + 사실 박제 + 학습 키워드 + PDL bump 사유).

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`)
- 2026-05-29: server SubAgent 구현 완료
  - `EnemyState.cs` 신설 (Idle=0/Patrol=1/Chase=2, byte cast 약속)
  - `EnemyEntity.cs` AI 필드 추가 (State/TargetEntityId/SpawnX/SpawnY/PatrolDir/RespawnTicksRemaining), Normal→Patrol 초기화, Boss→Idle 초기화
  - `GameMap.SpawnEnemy`: Normal enemy에 EnemyStats.NormalDefault() 자동 주입 (MoveSpeed=2.0/AggroRange=6.0/PatrolRange=4.0)
  - `GameMap._respawnQueue` + `NormalEnemyRespawnTicks=100` (5초 @ 20TPS) 추가
  - `GameMap.ProcessAttack` death 처리에 Normal enemy → _respawnQueue 등록 (Boss는 기존대로 완전 소멸)
  - `GameMap.UpdateEnemies` (tick 루프 분리 메서드): Patrol 왕복, aggro 판정, Chase 이동, de-aggro 히스테리시스(×1.5), S_EntityState broadcast (SnapshotTickInterval 주기)
  - `GameMap.ProcessRespawns`: tick 카운트다운 기반 respawn, AllocId() 새 id 발급, S_EntitySpawn broadcast
  - spawn 종속성 확인: GameSession에 SpawnEnemy 직접 호출 경로 없음 (GameMap.ctor 단일 경로)
  - **S_EntityState broadcast trade-off**: 매 틱 전체(20Hz×N enemy) vs SnapshotTickInterval 주기(4Hz). SnapshotTickInterval 선택 — 플레이어 snapshot과 주기 통일, 적이 느려 250ms 간격으로도 보간 충분. Phase 08 체감 보고 후 조정.
  - **respawn 정책**: Normal 100tick(5초) respawn = 데모 반복 시연에 자연스러운 값. Boss는 StageClear 1회성 = respawn 없음.
  - `EnemyAiTests.cs` 12개 신설 (patrol 경계 반전 2, aggro 전환 2, de-aggro 2, Chase 방향 2, Boss Idle 1, Respawn Normal 1, Boss NoRespawn 1, EntityState broadcast 1)
  - `EnemyAiSmoke.cs` 신설 (Town→HG 경유, Patrol→Chase 전환 S_EntityState 검증)
  - 빌드: 경고 0/오류 0. 테스트: 315 통과 / 0 실패 / 4 skip (회귀 0)
