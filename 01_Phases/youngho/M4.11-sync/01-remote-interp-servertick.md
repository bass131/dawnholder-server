---
owner: youngho
milestone: M4.11
phase: 01
title: 원격 보간 serverTick 전환 — 창드래그 desync(백로그 #5) 봉합
status: in_progress
grade: 대규모
slug: 01-remote-interp-servertick
created: 2026-06-11
domains: [shared, server, client]
prior_phases: []
depends_on: []
---

# M4.11 Phase 01 — 원격 보간 serverTick 전환 (창드래그 desync 봉합)

> 마일스톤 계획서 = `_milestone-plan.md` (도미노 2 = 이 Phase). 증거 사슬은 거기 + 아래 실측표.
> 이 Phase는 마일스톤 위험 오름차순의 **첫 칸(저위험)** — `RemoteEntity` 보간 시간축 안에 갇힌 국소 봉합.

---

## Context (왜)

원격 엔티티(타인 플레이어/몬스터)는 서버 스냅샷 사이를 보간해 부드럽게 그린다. 그런데 보간의 **시간축**이 잘못됐다 — 서버가 스냅샷마다 실어 보내는 `serverTick`(권위 시각)을 **버리고**, 클라 벽시계(`Time.realtimeSinceStartup`)로 타임스탬프를 *재도장*한다.

평상시엔 괜찮지만, 창 드래그·포커스 상실로 `Update`가 멈추면: 소켓 워커는 계속 수신해 디스패치 큐에 스냅샷을 쌓고 → 드래그가 끝나 `Update`가 재개되면 쌓인 N개를 한 프레임에 몰아 드레인 → N개 스냅샷이 거의 **같은 벽시계 값**으로 버퍼에 적재 → 보간이 시간상 뭉친 점들을 옛 위치부터 재생 → **천천히 회복되는 desync (백로그 #5)**.

서버는 이미 정확한 시각(`serverTick`)을 보내고 있다. 클라가 그걸 *버리지 않고 쓰면* 뭉침이 풀린다 — N개가 몰려와도 각자 고유한 serverTick을 가지므로 보간 버퍼에 시간상 *제대로 펼쳐져* 적재된다.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-11, 핸들러 분리 PR #98 반영)

> 계획서 증거표는 `ClientPacketHandlers.cs:125,131` 등 옛 단일 파일 줄번호를 가리킨다. PR #98(핸들러 폴더 분리)로 스냅샷 핸들러가 `Handlers/Sync/SnapshotHandler.cs`로 이동 → 아래는 **현재 파일·줄번호로 갱신한 실측**.

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. RemoteEntity 벽시계 재도장 | `State/RemoteEntity.cs:43,45-46` | `EnqueueSnapshot(float x, float y)` — serverTick 파라미터 *없음*. 본문 `float now = Time.realtimeSinceStartup; _buffer.Add(new Snapshot(now, x, y));` |
| 1b. 보간 소비도 벽시계 | `State/RemoteEntity.cs:89` | `float target = Time.realtimeSinceStartup - InterpolationDelay;` — 적재·소비 둘 다 벽시계 |
| 1c. Snapshot struct 시간 필드 | `State/RemoteEntity.cs:125-136` | `readonly struct Snapshot { float Time, X, Y }` — `Time`이 벽시계 |
| 2. 디스패치 일괄 드레인 | `Network/MainThreadDispatcher.cs` | 소켓 워커 `Enqueue` → `Update`가 `while(TryDequeue) action()` 일괄 드레인. freeze 후 재개 시 N개 `EnqueueSnapshot`이 거의 동일 벽시계로 몰림 |
| 3. 핸들러가 serverTick 버림 | `Network/Handlers/Sync/SnapshotHandler.cs:32,39` | `int sTick = pkt.serverTick;`로 *읽지만* `session.SetLastReceivedServerTick(sTick)`(lag-comp 기준점)에만 사용 |
| 3b. UpdateSnapshot에 안 넘김 | `Network/Handlers/Sync/SnapshotHandler.cs:67,72` + `State/RemoteEntityRegistry.cs:128,140` | `UpdateSnapshot(eid, x, y, animState)` — tick 인자 없음 → `entity.EnqueueSnapshot(x, y)` |
| 3c. RosterBuffer 캐싱 클로저도 누락 | `Network/Handlers/Sync/SnapshotHandler.cs:60-68` | 전환 중 캐싱 클로저가 `capturedEid/X/Y/animState`만 캡처 — serverTick 누락 (봉합 시 동반 전달 필요) |

---

## 변경 대상 (파일별)

1. **`State/RemoteEntity.cs`** — 핵심.
   - `EnqueueSnapshot(float x, float y)` → `EnqueueSnapshot(int serverTick, float x, float y)`.
   - 버퍼 타임스탬프를 벽시계가 아닌 **서버 시간축**(`serverTick * SecondsPerTick`, 20 TPS → 0.05f)으로 적재.
   - `Update` 보간 소비(`:89`)의 `target`도 같은 서버 시간축으로 — **설계 포인트** 참조.
   - `Snapshot.Time`의 의미가 "벽시계 수신 시각" → "서버 시각"으로 바뀜 (주석 갱신).

2. **`State/RemoteEntityRegistry.cs`** — 전달 통로.
   - `UpdateSnapshot(int entityId, float x, float y, byte animState)` → `serverTick` 인자 추가.
   - `entity.EnqueueSnapshot(serverTick, x, y)` 전달.

3. **`Network/Handlers/Sync/SnapshotHandler.cs`** — 시작점.
   - 이미 읽고 있는 `sTick`을 `UpdateSnapshot(... serverTick ...)`로 전달.
   - RosterBuffer 캐싱 클로저(`:60-68`)에도 `capturedTick` 캡처 추가.

4. **틱 길이 상수** — `Shared.GameData.Constants.TickDuration`(= `1.0f / ServerTickRate`, `ServerTickRate=20` → 0.05f)이 **이미 존재 → 재사용**(`Constants.cs:13,22` 실측 확인, plan-auditor 교차검증). 하드코딩 금지(클라 CLAUDE.md "밸런스 숫자는 shared에서"). **shared 무변경 확정** — 클라가 *읽기만* 함(STOP 가지 닫힘).

---

## 설계 결정 포인트 (client Worker 착수 시 확정 → plan-auditor/메인 검증)

버퍼 타임스탬프를 `serverTick * 0.05f`로 바꾸면, 보간 소비 시각(`target`)도 **같은 서버 시간축**이어야 한다 (둘이 다른 축이면 보간이 깨진다). "지금 서버는 몇 틱쯤일까"를 클라가 추정해야 한다:

- **추정 서버 시각** = `(마지막 수신 serverTick) * 0.05f + (그 후 경과한 realtime)`.
  `target = 추정 서버 시각 - InterpolationDelay(0.15f)`.
- 이 추정 시계를 **어디에 둘지**가 결정 포인트: `RemoteEntity` 내부(엔티티마다 lastTick+stopwatch) vs 세션 레벨(`UnityClientSession`이 단일 서버 시계 보유 → 모든 엔티티 공유). 후자가 "공유 시계" 정신에 맞고 Phase 4(고정스텝)와도 정합하나, Phase 1 범위(저위험·국소)를 넘어설 수 있음.
- **권고**: Phase 1은 *국소 봉합* 유지 — `RemoteEntity` 내부 추정으로 시작. 세션 레벨 단일 시계는 Phase 4에서 통합 검토(계획서 도미노 3). 단 착수 후 국소 추정이 지저분하면 메인과 재논의.
- **plan-auditor 보강(2026-06-11)**: 서버 기준점은 *이미 단일*이다 — `SnapshotHandler:39`의 `session.SetLastReceivedServerTick(sTick)`이 본인/타인 무관 단일 값으로 갱신됨. 따라서 RemoteEntity 국소 추정도 "동일 프레임 내 모든 원격 엔티티가 같은 기준점"을 보므로 사실상 공유 시계다 → Phase 4에서 세션 단일 시계로 *승격*할 때 호출부 치환이 국소적(재작업 비용 낮음).

---

## 적 경로 확대 (v12 bump — 영호 GO 2026-06-11)

`RemoteEntity`가 플레이어/적 **공용** 보간 컴포넌트임이 착수 중 드러남(client Worker가 적 경로를 놓쳐 컴파일 에러 `EnemyRegistry.cs:97` → BuildPlayer 거부 → Editor.log 역추적으로 발견). 적도 같은 desync를 가지므로 같은 serverTick 보간으로 통일. **wire 변경 = 의존 순서 엄수**:

1. **shared** — `99_Tools/PacketGenerator/PDL.xml`의 `S_EntityState`(ID 19)에 `<int name="serverTick"/>` **append-only**(맨 끝, `animState` 뒤). `ProtocolVersion.Current` 11→12 bump. PacketGenerator 재생성 → `98_Shared` Shared.dll 재빌드 → `03_Client/Assets/Plugins/Shared.dll` 갱신. 헌법 §4(양쪽 컴파일) + §2(은퇴 ID 재사용 금지, append-only).
2. **server** — `S_EntityState` 송신부에서 `serverTick` = 현재 서버 틱 채우기(서버는 tick 보유). 송신 경로 = 적 AI broadcast(SnapshotTickInterval=2틱=100ms).
3. **client** — `Handlers/Sync/EntityStateHandler.cs`가 `pkt.serverTick` 읽어 `EnemyRegistry.UpdatePosition(eid, serverTick, x, y, animState)` 전달. `EnemyRegistry.UpdatePosition` 시그니처에 serverTick 추가 → `entry.Interp.EnqueueSnapshot(serverTick, x, y + footOffset)`. **이게 컴파일 에러(`EnemyRegistry.cs:97`) 봉합.**

PacketRoundTrip 테스트에 `S_EntityState.serverTick` 케이스 추가. 봇 회귀 desync 0 유지.

## 완료 조건 / 게이트

- [ ] `EnqueueSnapshot`/`UpdateSnapshot`/`SnapshotHandler` serverTick 경로 연결 — 벽시계 재도장 제거.
- [ ] RosterBuffer 캐싱 클로저도 serverTick 동반 (전환 중 경로 누락 없음).
- [ ] Unity 콘솔 **error 0** (핸들러 분리 때처럼 타입 resolve 확인).
- [ ] **2클라 실측 게이트 (백로그 #5 봉합 증명)**: 2개 클라 띄워 한쪽 창 드래그 → 다른 클라에서 보이는 원격 캐릭터가 **드래그 후 즉시 제자리 회복**(봉합 전엔 천천히 회복 = desync). 봉합 *전후* 비교로 동시 확인:
  - 봉합 전 = 천천히 회복(현 거동) 재현 → 봉합 후 = 즉시 회복.
  - 런타임 게이트 2개 동시 확인(계획서): freeze 시 realtimeSinceStartup 정지 여부(뭉침 폭) / 회복이 InterpolationDelay(150ms)로 수렴하는지.
- [ ] 봇 회귀 — 기존 시나리오 desync 0 유지(원격 보간 변경이 봇 측정에 영향 없는지).

---

## 위험 / 헌법 약속

- **§2 프로토콜 — 플레이어 wire 무변경 / 적 v12 bump (영호 GO 2026-06-11)**: 플레이어 `serverTick`은 이미 `S_Snapshot`(PDL `:71`)에 있어 무변경. **그러나 착수 중 `RemoteEntity`가 적(`EnemyRegistry`)과 공용임이 드러남** — 적은 `S_EntityState`(ID 19)로 위치 갱신하는데 거기엔 serverTick *없음* → 컴파일 에러(`EnemyRegistry.cs:97`). 적도 봉합하려면 `S_EntityState`에 serverTick append(헌법 §2 append-only) + **ProtocolVersion 11→12 bump** + 서버 송신부. 영호 명시 GO 받음(irreversible 깃발). 상세 = 아래 "적 경로 확대" 섹션.
- **§1 서버 권위 불변**: 보간은 순수 시각 표현. 서버 권위 상태 변경 없음. 클라 CLAUDE.md "원격 = 서버 broadcast 순수 미러, 보간만" 정합.
- **⚠️ LocalPlayerMovement/PlayerPredictor 절대 금지(이 Phase)**: 이 Phase는 *원격* 엔티티 보간만. 로컬 예측 심장부(force-adopt 등)는 **안 건드린다**. SnapshotHandler의 본인 path(`:48-53` `OnServerSnapshot`)는 **무변경**.
- **Teleport 보간 끊기 정합**: `SnapInterpolation`/`SetTeleportArriveCallback` 경로(스킬 텔레포트)는 버퍼 clear 기반 — 시간축 변경과 독립이지만, 새 시간축에서도 첫 EnqueueSnapshot 콜백이 정상 발동하는지 확인.

---

> Phase 완료 시 `01-...-DONE.md` 박제(복잡 등급). 게이트 통과 후 Phase 02(force-adopt 덤불 정리) 착수.
