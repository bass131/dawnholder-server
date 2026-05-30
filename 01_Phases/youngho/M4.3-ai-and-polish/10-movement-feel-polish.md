---
owner: youngho
milestone: M4.3
phase: 10
title: 서버 입력 큐 fix — rubber-band 근본 해결 (coalescing + ack-on-receive)
status: pending
grade: 복잡
risk: trust-boundary
estimated: 2~3h
domain: server
---

# Phase 10: 서버 입력 큐 fix (rubber-band 근본 해결)

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (서버 단독 변경이지만 신뢰 경계 입력 처리 → trust-boundary 자동상향)
> **담당**: server (02_Server/ 단독 — **클라/프로토콜 변경 0건, ProtocolVersion bump 없음**)

---

## 🎯 목표

장시간 이동 시 캐릭터가 뒤로 **snap(rubber-band)** 하는 결함을 **근본 원인**에서 제거한다.

핵심: 이건 클라 reconcile 결함이 아니라 **서버 입력 처리** 결함이다. (세션 3에서 클라 잔차/smooth 증상치료 2회 실패 → 측정으로 서버 지목)

---

## ⏪ 사전 조건 / 측정 결과 (2026-05-30 세션4, 코드 좌표 박제)

**Server 3 결함** (rubber-band 직접 원인):
1. **단일 슬롯 coalescing** — `PlayerEntity.PendingInputX`가 한 칸짜리 슬롯(`PlayerEntity.cs:31`). `SubmitMoveIntent`의 EnqueueJob이 매 입력마다 덮어씀(`GameSession.cs:297`) → 틱 사이 입력 2개 오면 첫 번째 영구 드롭.
2. **빈 틱 = 제자리** — `GameMap.Tick`이 물리 적용 후 `p.PendingInputX = 0` 리셋(`GameMap.cs:271`) → 다음 틱까지 새 입력 안 오면 input=0 적용(안 움직임).
3. **ack-on-receive** — `entity.LastClientTick = capturedClientTick`을 입력 **수신 시점**에 박음(`GameSession.cs:299`) → 그게 `S_Snapshot.lastAckedClientTick`으로 브로드캐스트(`GameMap.cs:300`) → 서버가 *적용 안 한* 입력까지 ack.

**메커니즘**: 클라 송신(50ms)·서버 틱(50ms) free-running. 위상 어긋남 + 지터로 어떤 틱은 입력 0개(제자리), 어떤 틱은 2개(1개 드롭). 둘 다 같은 방향 = 서버 적용 이동 < 클라 예측 이동 → **lead 단조 누적**(실측 0.3 → 0.69). ack는 최신이라 클라 `InputHistory.EvictUpTo`가 다 지움 → `ReplayFrom`이 빈 시퀀스(replayed=0) → reconcile이 lead 영영 못 따라잡음 → lead > `SnapThreshold`(1.5)면 뒤로 snap + `PlayerAnimatorSync` flip 오작동.

**Client는 이미 정확** (변경 불요):
- `LocalPlayerController`: 매 프레임 가변 dt `Predict` + 50ms마다 `C_MoveIntent`(단조 `_localTickCounter`) 송신 + `InputHistory` 1건 기록.
- `PlayerPredictor.OnSnapshot`: `ackedClientTick` 초과 입력만 서버 위치에서 replay (각 50ms 고정 스텝).
- 즉 **"입력 1개 = 50ms 이동 1덩어리 = 서버 틱 1개"** 교과서 모델 위에서 동작. 서버가 이 계약(틱당 적용 + 적용분만 ack)을 어겨서 깨진 것.

---

## 📝 작업 내용 (서버 3곳)

### 1. 단일 슬롯 → 바운드 FIFO 큐
- `PlayerEntity`: `PendingInputX`/`PendingJumpPressed` 한 칸 → 입력 큐(`Queue<InputCommand>` 또는 동등 구조, 각 항목 = `(sbyte inputX, bool jump, uint clientTick)`).
- `SubmitMoveIntent`의 EnqueueJob: 덮어쓰기 → **Enqueue** (들어온 입력 안 버리고 순서대로). TCP가 순서 보장 → 재정렬 걱정 없음.
- **trust boundary (헌법 #3)**: 큐 상한(6칸 — 송신/틱 위상차 최대 누적 + 여유분) + 초과 시 oldest drop = 메모리 DoS 방어. rate limiter(`SubmitMoveIntent` 진입)는 그대로 유지 — 별개 방어층.

### 2. 틱 루프: 큐에서 틱당 입력 1개 적용 (input=0 리셋 제거)
- `GameMap.Tick`: 단일 슬롯 읽기 → **큐에서 1개 dequeue해 적용**. `PendingInputX = 0` 리셋 제거.
- **틱당 정확히 `Physics.Step` 1회 (멀티 드레인 금지)**: 서버는 실시간 fixed-timestep 시뮬이라 *물리 시간 = 벽시계 시간*이어야 함(틱당 50ms 1스텝). 한 틱에 2개 적용하면 50ms 안에 100ms치 중력이 진행돼 **점프 Y가 클라와 어긋남(over-count)** + lag comp `RecordPosition`이 같은 serverTick에 여러 위치를 갖게 됨(→ `GetPositionAtTick` 깨짐). **그래서 틱당 1입력만.**
- **큐 비면(starvation) neutral 입력**: `Physics.Step(0, false)` 1회 적용(세계는 계속 흐름 — 중력/마찰). 단 **ack 불변**(아래 3) — 적용 안 한 입력이 아니므로 클라가 evict 안 함.
- **버퍼 = 지연이지 lead 누적 아님**: 입력을 *안 버리고* 1:1로 소비(상한 내)하므로 서버는 클라보다 버퍼 깊이만큼 *지연*될 뿐 *발산*하지 않음. 지연은 클라 reconcile의 replay가 흡수(로컬 표시는 항상 클라 예측). 위상(phase) 지터로 생기는 double 틱(2개 큐)과 starvation 틱(0개)은 자연 상쇄돼 버퍼가 작게(0~1) 진동 → lead < `SnapThreshold`(1.5). (원래 lead 발산은 입력 *드롭* 때문이었고 큐가 그걸 제거.)
- **⚠️ 단정 아님 — 주파수 drift 주의**: 위 "자연 상쇄"는 클라·서버 평균 *주파수가 같다*는 전제에서만 성립. 둘은 각자 타이머로 50ms를 세므로 평균조차 ppm 단위로 어긋나면(clock drift) 버퍼가 *적분*돼 단조 증가 → cap(6) drop 도달 가능. drop = 1 quantum 손실 = 미세 lead 재유입(옛 결함 축소판). **LAN + 둘 다 50ms면 수십 초 데모 창에서 확률 낮지만 "측정해서 안 터졌다"와 "안 터질 것"은 무게가 다름** → 완료 조건에 drop=0 계측(아래).
- **지터 버퍼(선택)**: starvation이 잦으면 소비 시작 전 1칸 pre-buffer(D=1, 50ms 지연 ↔ 진동 흡수). **D=0(즉시 소비)로 시작** → Play 실측 후 필요 시 D=1. LAN이라 D=0 충분 가능성↑(over-engineering 회피).

### 3. ack = 적용 시점 clientTick (받은 시점 아님)
- `LastClientTick`을 EnqueueJob에서 박지 않음 → **틱 루프에서 실제 적용한 입력의 clientTick**으로 설정.
- 빈 틱(zero-input)에는 `LastClientTick` 불변 유지 → 클라가 replay할 미-ack 입력이 정확히 남음(replayed>0).

---

## ✅ 완료 조건

- [ ] **rubber-band 0** — 장시간(수십 초) 한 방향 이동 Play 실측에서 뒤로 snap 안 보임. lead가 `SnapThreshold`(1.5) 안에서 진동.
- [ ] **타 클라 시점 RemotePlayer 부드러움 (회귀 0)** — 2 인스턴스 Play에서 상대 캐릭터 보간이 매끄러움 (틱당 1입력이라 2배 점프는 구조적으로 없음 — M3.8 SnapshotTickInterval/InterpolationDelay 표면 회귀만 확인).
- [ ] **replayed>0 관측** — reconcile이 실제로 미-ack 입력을 replay (세션3 학습 = replayed=0이 근본 신호였음 → 0 탈출 확인). **측정 수단**: 임시 계측(`OnSnapshot`에 replayed count Debug.Log, 또는 임시 필드 노출) → 검증 후 **머지 전 제거** (M2 Phase 05/06 임시계측→제거 선례 정합).
- [ ] **큐 drop = 0 (장시간 hold)** — 수십 초~분 한 방향 hold 시 큐 상한 초과 drop 카운트 0 (임시 계측, 위 인프라 재사용). drop 관측되면 clock drift 신호 → drop 정책(oldest/newest drop vs D=1 지터버퍼)을 Play 실측 후 택1로 doc 명시 (미세 lead 재유입 방어).
- [ ] `dotnet test --no-incremental` green — 신규 + 기존 회귀 0 (기존 이동/점프/보간 동작 보존).
- [ ] 서버 로그 `[Cheat]`/`[Trust]` 위반 0 (정상 이동이 거짓 플래그 안 뜸).
- [ ] **신규 단위 테스트**: (a) 큐가 입력 2개 안 잃음(coalescing 방지) (b) 빈 틱에 ack 불변 (c) 드레인 catch-up이 입력 개수 보존 (d) 큐 상한 초과 시 drop 정책.

---

## 🧪 테스트

**자동**:
- 입력 큐 단위 테스트 — FIFO 순서 적용, 상한 초과 drop, 드레인 count = enqueue count(손실 0).
- ack 의미론 테스트 — 적용한 입력의 clientTick만 `LastClientTick`에 반영, 빈 틱 불변.
- 회귀 — 기존 movement/reconcile/lag-comp 테스트 0 실패.

**수동**:
- Play — 장시간 이동(좌/우 hold) 시 snap 0, 방향 전환·점프 자연스러움.
- 서버 콘솔 — `[Cheat]`/`[Trust]` 0, tick p99 정상(틱당 멀티 적용이 50ms 예산 안).

---

## 📚 학습 포인트

- **입력 버퍼링 = client prediction의 서버측 짝**: 클라가 "1입력=1틱"으로 예측·replay하면 서버도 "틱당 정확히 그만큼" 소비해야 정합. 한쪽이 입력을 버리거나 빈 틱을 끼우면 lead가 누적.
- **ack 의미론 ("받았다" vs "적용했다")**: reconcile의 핵심. ack가 적용보다 앞서면 클라가 replay할 걸 지워버려 reconcile이 무력화. 이게 rubber-band의 진짜 엔진.
- **증상 vs 근본**: rubber-band(증상) → 클라 smooth correction(증상치료, 2회 실패) → `replayed=0` 측정이 근본(서버) 지목. **증상 쫓기 전에 replay 동작부터 검증**(세션3 학습, 사용자 2회 정확 지적).
- **지터 버퍼 trade-off**: 버퍼 깊이 ↑ = 위상 진동 흡수(부드러움) ↔ 입력 지연 ↑. 환경(LAN/WAN)에 따라 튜닝.

---

## ⚠️ 함정 / 주의사항

- **trust boundary (헌법 #3)**: 큐는 메모리 점유 → **반드시 상한 + drop 정책**. rate limiter와 별개로 큐 자체 방어. clientTick은 untrusted지만 TCP in-order라 도착 순서 = 송신 순서 → 순서 신뢰 OK, 단 값 자체로 인덱싱 금지.
- **동작 보존(헌법 #1)**: 기존 이동/점프/보간 회귀 0. 서버 권위 이동은 그대로(클라는 표시·예측만). 결정론 테스트로 가드.
- **틱당 1스텝 불변식 (멀티 적용 금지)**: `Physics.Step`/`RecordPosition` 모두 틱당 1회. 큐에 여러 개 쌓여도 한 틱에 몰아 적용 X — 물리 시간 = 벽시계 시간이어야 중력/점프 Y가 클라와 정합하고 lag comp ring buffer가 serverTick당 위치 1개를 유지. (이게 이 Phase 설계의 핵심 — full-drain 유혹을 의식적으로 거부.)
- **starvation neutral = 수평 정지(vx=0)**: 큐 빈 틱에 `(0,false)` 적용하면 그 틱 `vx=0`(Physics.cs). 이동 중 starvation이 snapshot 틱과 겹치면 *원격 시점* `ComputePlayerAnimState`(GameMap.cs:170)가 Walk→Idle 1틱 깜빡일 수 있음(로컬은 예측이라 무영향). 드물면 무시 가능하나 **Phase 11 원격 애니 디버깅 시 헛다리 방지**용으로 기록.
- **DLL**: 서버만 변경 + 프로토콜 불변 → 클라 `Shared.dll` 영향 없음. 서버 재빌드 후 재기동만(Shared.dll 잠금 시 서버 종료 후 빌드).

---

## 🔭 후속 (별도 root cause — 이 Phase 범위 아님)

원래 Phase 10에 묶여 있던 2건은 입력 큐와 **독립 root cause**라 분리(이 변경을 atomic하게 유지):
- **β10 MoveSpeed dead** — `PlayerStats.MoveSpeed`(Warrior 4 / Ranger 6)가 이동에 반영 안 됨(체감 0). `Physics.Step`이 MoveSpeed를 쓰는지 별도 측정 필요.
- **M2 jump Y mispredict** — 점프 시 Y 예측이 서버와 어긋남(가변 dt vs 고정 step Euler 오차 의심). 별도 측정 필요.

→ Phase 10 입력 큐 fix 머지 후 **Phase 10b**로 측정·분해 재평가. 발표(2026-06-10) 데모에 MoveSpeed 체감 필요하니 드롭 아님, 분리 추적.

---

## ➡️ 다음 Phase

- Phase 09 (boss) / Phase 11 (본인 Animator) — 병렬 진행.
- Phase 10b (MoveSpeed dead + jump Y) — 측정 후 분해.

---

## 📋 박제 (완료 후)

- **복잡 등급** — `10-movement-feel-polish-DONE.md` 박음 (서버 입력 큐 fix 사실 박제 + 측정 학습).
- 머지 전 `/cross-review` 권장 (trust-boundary + 입력 처리 = 신중).

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`).
- 2026-05-30 (세션4): **측정으로 root cause 재정의** — "클라 reconcile/smooth polish" → "서버 입력 큐 fix". 범위 `shared+server+client` → `server` 단독으로 좁힘(프로토콜·클라 변경 0). MoveSpeed-dead + jump-Y는 독립 root cause라 Phase 10b로 분리. 측정 좌표(`PlayerEntity.cs:31` / `GameSession.cs:297,299` / `GameMap.cs:271,300`) 박제.
