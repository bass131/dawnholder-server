---
owner: youngho
milestone: M4.11
title: 클라-서버 동기화 정돈 — 공유 시계 일치 + force-adopt 덤불 정리
status: planned
grade: 대규모
slug: M4.11-sync
created: 2026-06-11
domains: [client, shared, qa]
---

# M4.11 — 클라-서버 동기화 정돈 (공유 시계 일치 + force-adopt 덤불 정리)

> 직전 = M4.10(컨벤션 v6 + 저위험 중복 정리, ProtocolVersion 11). M4.10이 만든 `ENTRY_POINTS.md` 진입점 맵(특히 동기화 항목)이 이 마일스톤의 디버깅 자산이다.
> 이 마일스톤은 동기화를 **갈아엎지 않는다.** 토대는 건강하다 — *시계(clock)를 일치*시켜 누적된 덤불을 뿌리부터 정리한다.

---

## Context (왜)

동기화 토대(서버 권위 · 클라 예측 · reconcile · 보간)는 **건강하다.** 골격은 헌법대로 잘 서 있다. 문제는 그 위에 누적된 패치들이 **덤불(thicket)**을 이뤘다는 것이다. 그리고 그 덤불의 **근본 원인은 단 하나** — **"클라-서버 공유 시계(clock)가 없다."**

도미노를 뿌리부터 끝까지 따라가 보면 이렇다:

- **(뿌리) 클라 가변 dt vs 서버 고정 틱.** 클라는 프레임마다 달라지는 `Time.deltaTime`(가변 dt, delta-time = 프레임 간 경과 시간)으로 예측을 굴리고, 서버는 50ms 고정 틱(20 TPS)으로 시뮬레이션한다. 둘은 *계산 박자가 달라서* 정확히 안 맞는다 — 미세한 drift(누적 오차)가 구조적으로 생긴다.
- **(도미노 1) drift를 덮으려 SnapThreshold를 키웠다.** `PlayerPredictor.SnapThreshold`를 1.5f로 올려서, 서버 위치와 예측 위치가 1.5유닛 이내로 벌어지는 동안은 "맞다고 친다." 결과는 **1.5유닛짜리 dead-zone**(불감대 — 그 안에선 reconcile을 안 함). 이 dead-zone 위에 다시 `ShouldForceAdopt`/`IsMovementLocked` 같은 *특수 처리*가 얹혔고, 그중 `serverVx`가 `0.0001f`인지 보는 가드가 서버의 lunge(돌진) 임펄스 매직넘버 `0.05f`에 *묶여* 있다 — 한쪽 숫자를 바꾸면 다른 쪽이 조용히 깨진다.
- **(도미노 2, 가장 구체적) 원격 보간이 serverTick을 버린다.** `RemoteEntity.EnqueueSnapshot`이 `S_Snapshot`에 들어 있는 **serverTick을 버리고** `Time.realtimeSinceStartup`(클라 벽시계)으로 타임스탬프를 *재도장*한다. 평상시엔 괜찮지만, freeze(창 드래그 등으로 프레임 멈춤)나 스파이크가 나서 네트워크 수신 → 메인 스레드 디스패치가 밀리면, 밀려 있던 스냅샷들이 한 프레임에 *몰려서* 같은 벽시계 타임스탬프를 받는다. 그러면 보간이 시간상 뭉친 점들을 옛 위치부터 다시 재생 → **백로그 #5("창 드래그 후 desync가 천천히 회복")의 강한 가설 범인.**
- **(도미노 3) 클라가 시계를 둘 쓴다.** 로컬 플레이어는 `ackedClientTick`(틱 기반 시계)으로 reconcile하고, 원격 엔티티는 벽시계(`realtimeSinceStartup`)로 보간한다. *같은 화면 안의 두 객체가 서로 다른 시간 축*에서 논다 — 일관성이 구조적으로 없다.

**개선의 방향 = 갈아엎기가 아니라 *시계 일치*다.** 서버가 이미 모든 스냅샷에 serverTick을 실어 보내고 있으니, 클라가 그걸 *버리지 않고 쓰면* 공유 시계가 선다. 그 위에서 로컬 예측을 고정스텝으로 바꾸면 dt-drift의 뿌리가 뽑히고, drift가 사라지면 임계를 줄일 수 있고, 임계가 줄면 dead-zone과 특수처리 덤불이 해소된다. 도미노를 *역순으로* 무너뜨리는 게 이 마일스톤이다.

---

## 증거 사슬 (코드 실측 확정 — 2026-06-11)

> 위 "도미노 2"는 계획서 작성 시점엔 *강한 가설*이었다. 2026-06-11 client Worker가 현재 코드를 한 줄씩 떠서 **3링크 전부 코드로 확정**했다(런타임 재현 *전*, 정적 실측). 다음 세션 컨텍스트 유실 방지를 위해 박제한다.

**가설**: 창 드래그/포커스 상실 시 원격 엔티티가 어긋났다가 천천히 회복(desync). 범인 = `S_Snapshot.serverTick`을 보간 시간축으로 쓰지 않고 클라 벽시계(`Time.realtimeSinceStartup`)로 재도장.

| 링크 | 판정 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|---|
| 1. RemoteEntity 재도장 | ✅확정 | `03_Client/Assets/Scripts/State/RemoteEntity.cs:43-46` | `EnqueueSnapshot(float x, float y)` — serverTick 파라미터 *없음*. 본문 `float now = Time.realtimeSinceStartup; _buffer.Add(new Snapshot(now, x, y));` — 클라 벽시계로 도장. 보간 소비도 같은 시계(`:89` `target = realtimeSinceStartup - InterpolationDelay`, `:113` 버퍼 `Time` 비교) |
| 2. 디스패치 일괄 드레인 | ✅확정 | `03_Client/Assets/Scripts/Network/MainThreadDispatcher.cs:34,72-76` | 소켓 워커가 `ConcurrentQueue<Action>` 적체 → `Update`가 `while(TryDequeue) action();`로 한 프레임에 전부 드레인. freeze 후 재개 시 N개 `EnqueueSnapshot`이 거의 동일 벽시계 값으로 몰림 |
| 3. 핸들러가 serverTick 버림 | ✅확정 | `03_Client/Assets/Scripts/Network/ClientPacketHandlers.cs:125,131,160-165` | `int sTick = pkt.serverTick;`로 *읽지만*, `session.SetLastReceivedServerTick(sTick)`(lag-comp 기준점)에만 쓰고 `RemoteEntityRegistry.UpdateSnapshot(eid,x,y,animState)`엔 안 넘김. `UpdateSnapshot` 시그니처도 tick 없음. PDL엔 존재(`99_Tools/PacketGenerator/PDL.xml:71` `<int name="serverTick"/>`) |

**메커니즘**: 창 드래그 → `Update` 정지 → 소켓 워커는 계속 수신해 `_queue` 적체 → 드래그 끝 → `Update` 재개가 쌓인 N개를 몰아 드레인 → N개 스냅샷이 거의 같은 `realtimeSinceStartup`으로 버퍼 적재 → 보간 `target = now - 0.15f`가 시간상 뭉친 점들을 옛 위치부터 재생 → desync.

**견고성 단서**: freeze 중 `realtimeSinceStartup`이 계속 흘러도 버그는 성립한다 — `Update`가 안 도는 동안 `EnqueueSnapshot` 자체가 호출되지 않으므로, 패킷은 큐에만 쌓이고 재개 한 프레임에 몰리는 구조는 불변. **→ fix 방향(serverTick을 보간 시간축으로)이 freeze 세부 동작과 무관하게 견고.**

**코드로 못 박은 것 (Phase 1 런타임 게이트로 확인)**:
1. Windows 창 드래그 시 `realtimeSinceStartup` 실제 정지 여부 (뭉침 *폭*에만 영향, 버그 성립엔 무관)
2. desync 회복이 InterpolationDelay(150ms)로 수렴하는지 / 더 긴지

**Phase 1 fix 형상(예정, 착수 시 확정)**: 핸들러 → `UpdateSnapshot` → `EnqueueSnapshot` 경로에 serverTick(int tick)을 실어 내려, 버퍼 타임스탬프를 `tick * 0.05f`(서버 시간축, 20 TPS) 기반으로 전환. 벽시계 재도장 제거. **wire 무변경**(serverTick은 이미 패킷에 있음 → 11 유지). *[정정 2026-06-12: 플레이어 `S_Snapshot`은 예상대로 무변경이었으나, 적 `S_EntityState`엔 serverTick이 없어 P1에서 append → **ProtocolVersion 11→12 bump로 실행됨** — `01-remote-interp-servertick-DONE.md` 참조]*

---

## Phase 분해 (예정 — 개별 .md는 M4.11 착수 시 분해, stale 방지)

위험 오름차순 게이트식. 저위험(국소 봉합)부터 시작해, 안전망을 깐 뒤에야 심장부(로컬 예측)를 건드린다.

| # | Phase (예정) | 위험 | 도메인 | 핵심 | 게이트 |
|---|---|---|---|---|---|
| 1 | **원격 보간 serverTick 전환** | 저위험 (`RemoteEntity` 안에 갇힘) | client | `EnqueueSnapshot`이 벽시계 재도장 대신 `S_Snapshot.serverTick`을 보간 타임소스로 사용. **선행: 네트워크→메인스레드 디스패치 드레인을 실측**해 freeze 가설을 확정한 뒤 착수. 백로그 #5 봉합. | 백로그 #5 재현 → 봉합 확인 (창 드래그 후 즉시 회복) |
| 2 | **force-adopt 덤불 정리** | 저위험 (동작 보존) | client | `LocalPlayerMovement`의 `ShouldForceAdopt`/`IsMovementLocked`/`SnapThreshold` dead-zone을 명료화. 서버 임펄스를 **명시 플래그로 도입 검토** — `serverVx==0.0001f` 같은 매직넘버 의존을 제거(서버 lunge 0.05f 결합 끊기). | 동작 불변 (force-adopt/dash 거동 봇·실측 동일) |
| 3 | **reconcile/보간 회귀 안전망 보강** | 중위험 (테스트 인프라) | qa | reconcile·보간 회귀 테스트 + 봇 시나리오 + **2클라 실측 게이트**. ★Phase 4 착수 *전 필수* — "확실하게"의 핵심. 심장부를 건드리기 전에 "거동 불변"을 자동 증명할 그물을 먼저 친다. | 안전망이 *현재 거동*을 green으로 고정 (이후 4의 회귀 판정 기준) |
| 4 | **로컬 예측 고정스텝 전환** | **고위험** (심장부) | client + shared | `PlayerPredictor`를 가변 dt → **50ms 고정 서브스텝 + 시각 보간**으로 전환. dt-drift를 뿌리뽑아 → 임계 축소 → dead-zone·덤불 해소. 안전망(3) 통과 후만 착수. | 안전망(3) 전부 green + 2클라 실측 부드러움 동일/개선 |
| 5 | **serverTick + 고정스텝 반영 클라 재빌드** | 중위험 (빌드·통합) | client + qa | 1·2·4가 다 박힌 클라를 재빌드 → 전체 회귀(테스트 + 봇 + 2클라 + 콘솔 0에러). | 전 시나리오 green · 백로그 #5 봉합 유지 · Unity 콘솔 error 0 |

---

## 위험

- **Phase 4가 이 마일스톤의 칼날이다.** `PlayerPredictor`/`LocalPlayerMovement`는 **방금(M4.9) force-adopt·dash·reconcile을 봉합한 따끈한 심장부**다. 그래서 Phase 4는 **안전망(Phase 3) 통과 후에만** 착수한다 — 그물 없이 심장부를 건드리면 reconcile 발산을 다시 부른다.
- **"부드러움이 깨지지 않을까" 우려 해소.** 고정스텝이 화면을 끊기게 만들 거라는 직관은 틀렸다. **계산 박자(고정스텝)와 그림 부드러움(시각 보간)은 분리**된다 — 물리/예측은 50ms 고정 서브스텝으로 *결정론적으로* 돌리고, 화면에는 두 스텝 사이를 보간해 *프레임 Hz로 부드럽게* 그린다. 이게 Unity의 FixedUpdate(고정 물리)↔Update(가변 렌더) 철학이자 게임 루프 정석("Fix Your Timestep")이다. `PlayerPredictor` 주석에 박힌 "over-engineering(과설계)" 판정은, 고정스텝이 *dt-drift 덤불 전체를 제거*하는 값어치 앞에서 **재평가**한다 — 그땐 과설계였을지 몰라도, 덤불이 쌓인 지금은 토대 정리의 핵심 수단이다.
- **ProtocolVersion**: serverTick은 **이미 `S_Snapshot`에 실려 있다** → 클라가 *버리던 걸 쓰는* 변경이라 **wire 무변경 예상**(현 11 유지). 단 Phase 1/4에서 정말 새 필드가 필요해지면 그 시점에 STOP → 사용자 의논(irreversible 경로). *[정정 2026-06-12: P1에서 적 `S_EntityState` serverTick append로 **11→12 bump 완료**(사용자 GO 거침) — P2 이후는 **12 유지**가 기준]*

---

## 의존

- **M4.10 후 착수.** M4.10이 채운 `ENTRY_POINTS.md`의 **동기화 항목**(rubber-band → reconcile/force-adopt, 느린 추종 → RemoteEntity 보간 타임소스)이 이 마일스톤의 디버깅 출발점 맵이 된다 — 백지 탐색 없이 증상에서 바로 파일로 점프.

---

> **본 문서는 마일스톤 계획서.** Phase 개별 정의 `.md`는 **M4.11 착수 시점에 분해**한다(미리 박으면 stale — 위 표는 *예정* 골격일 뿐, 실측은 착수 시).
