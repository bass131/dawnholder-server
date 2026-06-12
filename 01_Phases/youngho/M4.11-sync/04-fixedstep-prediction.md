---
owner: youngho
milestone: M4.11
phase: 04-fixedstep-prediction
title: 로컬 예측 고정스텝 전환 — 가변 dt 뿌리 제거 + 시각 보간
status: planned
grade: 복잡
slug: 04-fixedstep-prediction
created: 2026-06-12
domains: [client, shared]
prior_phases: [01-remote-interp-servertick, 02-force-adopt-decouple, 03-regression-safety-net]
depends_on: [03-regression-safety-net]
risk_flags: [심장부 고위험, 비가역]
---

# M4.11 Phase 04 — 로컬 예측 고정스텝 전환 (가변 dt 뿌리 제거 + 시각 보간)

> 마일스톤 계획서 = `_milestone-plan.md` P4 행 — **이 마일스톤의 칼날(고위험 심장부)**. 위험 오름차순 게이트식의 마지막 코드 매듭. P3(`03-regression-safety-net.md`)이 친 그물이 통과한 *뒤에만* 착수한다.
> 이 Phase가 도미노의 **뿌리**를 뽑는다 — 마일스톤 Context의 "(뿌리) 클라 가변 dt vs 서버 고정 틱". 뿌리를 뽑아야 P1~P2가 봉합한 잎사귀(SnapThreshold dead-zone, force-adopt 게이트)가 *구조적으로* 줄어든다.

---

## Context (왜)

지금 클라는 예측(prediction, 서버 확인 전 먼저 움직여 반응성을 확보하는 기법)을 **매 프레임 가변 dt**로 굴린다. `Time.deltaTime`(delta-time, 직전 프레임에서 지금까지 흐른 시간)은 프레임마다 다르다 — 144fps면 ~7ms, 30fps면 ~33ms. 반면 서버는 **고정 50ms 틱**(20 TPS)으로 시뮬레이션한다. 둘은 *적분(integration, 속도를 시간으로 곱해 위치를 누적하는 계산) 박자가 달라서* 정확히 안 맞는다. 같은 입력을 줘도 누적 위치가 미세하게 어긋나는 **drift(누적 오차)**가 구조적으로 생긴다 (`PlayerPredictor.cs:24-25`, `:33`의 주석이 이 현상을 자인한다).

이 drift를 덮으려고 P0 이전에 `SnapThreshold`를 1.5f로 키웠다(`PlayerPredictor.cs:33-35`). 그 위에 force-adopt 게이트(P2가 공유 상수로 정련)가 얹혔다. 즉 **dead-zone과 force-adopt 덤불은 전부 "가변 dt drift"라는 한 뿌리의 잎사귀**다. P4는 그 뿌리를 뽑는다 — 예측 박자를 서버와 똑같은 50ms 고정 서브스텝으로 맞춰서, 같은 `Physics.Step` 함수에 같은 dt를 먹인다. 그러면 drift가 *구조적으로* 사라진다.

"고정스텝으로 바꾸면 화면이 끊기지 않나?"라는 직관은 틀렸다 — **계산 박자(고정스텝)와 그림 부드러움(시각 보간)은 분리**된다. 물리는 50ms 고정으로 *결정론적으로* 돌리고, 화면에는 두 스텝 *사이*를 보간해 프레임 Hz로 부드럽게 그린다. 이게 게임 루프 정석 "Fix Your Timestep"이자 Unity의 FixedUpdate(고정 물리)↔Update(가변 렌더) 철학이다. `PlayerPredictor.cs:30`에 박힌 "over-engineering(과설계)" 판정은, 고정스텝이 *dt-drift 덤불 전체를 제거*하는 값어치 앞에서 재평가한다 — 마일스톤 계획서 위험 섹션이 선언한 정정이다.

---

## 현재 형상 실측 (2026-06-12, HEAD 0774695, Explore 에이전트 확정)

> 이 절은 P4가 *무엇을 어디서* 바꾸는지를 코드로 못 박은 것이다. 재탐색 불필요 — 이 좌표에서 바로 수술한다.

| 좌표 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. **가변 dt 진입점 (유일)** | `03_Client/.../Prediction/PlayerPredictor.cs:96-99` | `Predict(inputX, jumpPressed, dt)`가 가변 dt를 `SharedPhysics.Step`에 그대로 전달. **가변 dt가 들어가는 곳은 여기 한 곳뿐.** |
| 2. **replay는 이미 고정** | `PlayerPredictor.cs:135` | reconcile replay는 이미 `Constants.TickDuration`(50ms) 고정 dt — **P4가 안 건드린다** (이미 결정론적). |
| 3. dead-zone 임계 | `PlayerPredictor.cs:35` | `SnapThreshold = 1.5f`. mispredict 판정 `:119-120`. **P4에서 값 변경 금지** (축소는 별도 후속, STOP 포인트). |
| 4. over-engineering 주석 | `PlayerPredictor.cs:30` | "fixed-step + visual lerp는 격투/콘솔 RTS 패턴이라 우리 게임에 over-engineering." — 마일스톤 플랜이 재평가 선언. |
| 5. **Update 단일 경로** | `03_Client/.../Prediction/LocalPlayerMovement.cs:230` | 5단계: ①시간 감쇠(`:234-244`) ②source-gating(`:250-252`) ③Predict 호출(`:258-261`, `MaxPredictStep=0.1f` clamp `:260`) ④`transform.position` 직접 대입(`:262` — **시각 보간 없음**) ⑤송신 throttle(`:266-268`). |
| 6. 송신 cadence | `LocalPlayerMovement.cs:266-268` | `_sendAccumulator`가 *실제 dt* 누적 → 50ms마다 C_MoveIntent 1발 + `NotifySent`(`:290`)로 InputHistory 기록. **이미 50ms cadence — P4의 박자 모태.** |
| 7. **점프 엣지 latch** | `LocalPlayerMovement.cs:252,275` | `_jumpEdgeThisTick`을 송신 cycle까지 보관, 송신 후 `:275`에서 reset. **프레임 단위가 아니라 송신 cadence 단위.** |
| 8. 서버 거울 | `02_Server/.../Maps/GameMap.cs:221-278` | 틱당 입력 큐에서 **정확히 1개 소비**(없으면 neutral) → movement-gate → `Physics.Step(dt=TickDuration)` 1회. |
| 9. Physics.Step dt 위치 | `98_Shared/GameData/Physics.cs:122` | dt는 `PhysicsInput` 안 — **호출자 책임**. 순수 static 함수(상태 없음). |
| 10. 공유 상수 | `98_Shared/GameData/Constants.cs:22,87` | `TickDuration=0.05f` / `ExternalImpulseEpsilon=0.05f`(P2 산물). |
| 11. 시각 보간 선례 | `03_Client/.../State/RemoteEntity.cs:107-128` | 서버 시간축 버퍼 + `_renderTime` 연속 전진 + drift*0.1 흡수 + 0.5s 초과 시 snap (P1 산물 — 로컬 보간 *참고*용). |

**핵심 관찰**: 서버는 "틱당 입력 1개 소비 → Step 1회"(좌표 8)다. 클라가 이걸 *정확히 거울*로 만들려면 `_sendAccumulator`(이미 50ms cadence, 좌표 6)를 *예측의 박자*로 승격하면 된다 — 입력 샘플링·Predict·송신·NotifySent를 한 묶음으로 묶어 50ms마다 1:1로 돌린다. 그러면 클라의 N번째 Predict와 서버의 N번째 Step이 같은 입력 × 같은 dt를 먹는다 → drift 0.

---

## 작업 항목 (client 본체 + 98_Shared 영향 가능)

핵심 = **accumulator(누적기) 기반 고정 서브스텝 + 시각 보간** ("Fix Your Timestep" 정석). `LocalPlayerMovement.Update`를 재구조화한다.

**1. 고정 서브스텝 전환** — `_sendAccumulator`를 *예측의 박자*로 승격.
- accumulator가 `TickDuration`을 넘을 때마다 다음 **4종 세트를 1:1로 묶어** 실행: ①입력 샘플링(source-gating 거친 `moveX`/`jumpEdge`) → ②`Predict(moveX, jumpEdge, TickDuration)` *고정 dt* → ③`C_MoveIntent` 송신 → ④`NotifySent`(InputHistory 기록).
- 프레임이 길면(fps<20) 한 프레임에 서브스텝 여러 번, 짧으면(고fps) 0번. 서버의 "틱당 1 입력 소비"(좌표 8)와 정확한 거울.
- 결과: Predict에 들어가는 dt가 항상 `TickDuration` → 좌표 1의 가변 dt 진입점 제거.

**2. 시각 보간** — substep의 prev/curr 두 상태(Predict 직전/직후 위치)를 들고:
- `transform.position = Vector3.Lerp(prev, curr, accumulator / TickDuration)` (좌표 4 `:262`의 직접 대입 교체).
- `RemoteEntity` 패턴(좌표 11) 참고하되 **로컬은 버퍼 불요** — 인접한 두 점이면 충분(원격은 네트워크 지연 버퍼가 필요하지만 로컬 예측은 방금 만든 두 점이라 즉시 보간).

**3. 점프 엣지 latch 함정 처치** — `_jumpEdgeThisTick`(좌표 7).
- 현재는 송신 cycle까지 보관 후 reset(`:275`). 고정스텝에선 **고fps에서 substep이 0번 도는 프레임에 점프가 눌리면 유실** 위험.
- **다음 substep이 소비할 때까지 latch 유지 + 소비 시점에 클리어**로 변경. 즉 reset 위치를 "송신 직후"가 아니라 "substep 입력 샘플링이 jumpEdge를 먹은 직후"로 옮긴다.

**4. 스파이크 방어 (spiral of death 차단)** — `MaxPredictStep=0.1f` clamp(좌표 5 `:260`)의 역할 계승.
- 현재 역할: 가변 dt를 0.1f로 잘라 한 프레임에 200ms 이상 적분 방지.
- 고정스텝에서의 역할: **프레임당 최대 substep 횟수 cap**(예: 4스텝 = 200ms). 초과분은 버리고 *이후 reconcile에 맡긴다*. 안 하면 한 프레임이 길어질 때 substep이 폭증해 더 느려지고 → 더 길어지는 spiral of death(죽음의 나선).
- 상수 처치(은퇴 / 의미 전환 / 새 상수)는 **작업 중 결정**. 새 공유 상수가 필요해 보이면 STOP(아래 위험 섹션).

**5. 타이머 감쇠 박자 결정** — 시간 감쇠 타이머(좌표 5 `:234-244`: `_commitWindowRemaining`/`_hitGateRemaining` 등).
- 현재 frame dt(`Time.deltaTime`) 감쇠. source-gating(좌표 5 ②)이 substep의 입력 샘플링과 *같은 박자*여야 게이트 누락이 없다 — 어긋나면 한 substep에서 잠금이 풀렸다 잠겼다 깜빡일 수 있다.
- **substep 박자 정렬을 1순위 검토**(타이머도 substep마다 `TickDuration`씩 감쇠). 단 쿨다운류(`_attackCooldownRemaining` 등 송신과 무관한 UI 타이머)는 frame dt 유지가 적절할 수 있음 — **실측 후 결정**.

**6. baseline 테스트 2건 갱신** — P3가 박은 `[P3 baseline — P4 재검토 대상]` 마커.
- `PlayerPredictorTests.cs`의 baseline 슬롯(가변 dt Predict 궤적, P3 정의 작업항목 2) — 고정스텝 전환으로 *자명히* 바뀐다.
- P3가 박아 둔 사유 기입 슬롯에 **변경 전 값 → 후 값 + 사유 1줄 기입 의무**. 마커를 찾는 순간 기입하지 않으면 게이트 fail (아래 완료 조건). **갱신은 이 Phase 범위.**

**7. over-engineering 주석 정정** — `PlayerPredictor.cs:30`을 재평가 사유 1줄로 교체 (예: "P4에서 dt-drift 뿌리 제거 위해 fixed-step + visual lerp 채택 — 덤불 정리 핵심 수단").

> **STOP 후속 (이 Phase 범위 밖)**: `SnapThreshold` 1.5f dead-zone 축소는 고정스텝 *효과 실측 후* 별도 결정 — 이 Phase에서 숫자를 미리 박지 않는다(영호 의논 STOP 포인트). 고정스텝이 drift를 얼마나 줄였는지 측정해야 안전한 새 값이 나온다.

---

## 완료 조건 / 게이트 (정량)

- [ ] **WSL2 서버 테스트 561 passed 유지** — 불변식 全 green (golden 궤적 / replay 불변식 / 봇 연속성 / FacingSnap). 561 미만 = P4 STOP.
- [ ] **EditMode 119 기준**: baseline 2건은 `[P3 baseline` 마커 슬롯에 **사유 기입 후** 갱신 허용, 그 외 green.
- [ ] **baseline 마커 사유 누락 0건** — `[P3 baseline` grep 후 갱신된 케이스에 "변경 전→후 + 사유" 박힘 확인. 빈 슬롯 채 갱신하면 게이트 fail.
- [ ] **봇 M2BasicMovement desync `(0.00, 0.00)` 유지** — 서버 측 연속성 불변 (P4는 클라 예측 박자만 바꿈, 서버 무관).
- [ ] **Unity 컴파일 0 error**. BuildPlayer는 불요(P5에서 재빌드).
- [ ] **`_p4-2client-checklist.md` "P4 후 거동" 6항목 영호 실측 기입** — 전 항목 "이상 무" 또는 개선. ("P4 전 거동"은 P3에서 기입 완료.)
- [ ] **거동 비교** (P4 전과 동일 또는 개선): rubber-band / Dash lunge / 평타 lunge / 피격 넉백 / 창드래그(P1 desync) / 정지 시 떨림.
- [ ] **wire 무변경 확인**: PDL.xml / `ProtocolVersion` diff 0 — **v12 유지**. 송신 cadence 20/s 불변(좌표 6).
- [ ] **(조건부 — 98_Shared 변경 발생 시)** `dotnet build Dawnholder.slnx` 0 error + Shared.dll → `03_Client/Assets/Plugins/` 복사 후 Unity 재컴파일 0 error (헌법 §4 양쪽 컴파일 게이트). 변경이 client-only로 끝나면 본 게이트는 자동 통과. (plan-auditor D2 봉합, 2026-06-12)

---

## 위험 / 헌법 약속 — 금지 항목

- **⚠️ 심장부 고위험·비가역**: `PlayerPredictor`/`LocalPlayerMovement`는 M4.9가 갓 봉합한 force-adopt·dash·reconcile + M4.11 P2가 갓 봉합한 ε 게이트가 *바로 옆에 붙은* 따끈한 심장부다. 그물(P3) 없이 손대면 reconcile 발산을 다시 부른다 — P3 불변식 4벌이 절대 green이 아니면 즉시 STOP.
- **§1 서버 권위 불변**: 고정스텝은 *예측 박자*만 바꾼다 — 클라는 여전히 서버 vx/위치의 미러. reconcile은 그대로 서버 진실이 이긴다. 예측 결과를 권위 상태로 승격하지 않는다.
- **§2 프로토콜 wire 무변경**: PDL.xml / `ProtocolVersion` 손대지 않음 — **v12 유지**. C_MoveIntent 형상·송신 cadence(20/s) 불변. 신규 패킷·필드 없음.
- **source-gating 경로 구조 불변**: gated 입력(`moveX`/`jumpEdge`)이 Predict / 송신 / InputHistory **셋 모두에 동일하게** 흐르는 구조(`LocalPlayerMovement.cs:247-249` 헌법 §1 정합)를 보존. substep으로 묶을 때 이 1:1 흐름이 깨지면 reconcile replay가 서버와 어긋난다.
- **P2 산물 의미 불변**: `ShouldForceAdopt` / `IsMovementLocked`의 의미·`ExternalImpulseEpsilon` 게이트(P2 확정) 변경 금지.
- **서버 코드 무변경**: `02_Server/` 한 줄도 안 건드린다. 서버는 이미 고정 틱(좌표 8) — P4는 클라를 서버 박자에 맞출 뿐.
- **불변식 테스트 4벌 절대 green**: golden 궤적 / replay 불변식 / 봇 연속성(desync 0.00) / FacingSnap. red = 회귀 = STOP.

### 새로 식별된 함정 (이 Phase 특유)

- **점프 유실 함정**: 고fps에서 substep 0번 프레임의 점프 입력 유실 (작업항목 3). latch 미처치 시 "가끔 점프가 안 먹는" 산발 버그 — 재현 어려움. **반드시 latch 유지 + 소비 시 클리어.**
- **타이머 박자 함정**: 타이머 감쇠와 source-gating 박자 불일치 시 한 substep 내 잠금 깜빡임 (작업항목 5). substep 정렬 1순위 검토.
- **저fps spiral of death**: substep cap 미설정 시 프레임 길어짐 → substep 폭증 → 더 길어짐 (작업항목 4). cap 의무.
- **baseline 갱신 사유 누락 금지**: 마커 슬롯을 빈 채 골든 값만 바꾸면 "왜 바뀌었는지" 증거가 사라진다 — P3가 물리적 빈 슬롯으로 강제한 의도를 지킨다.

### STOP 포인트 (셋 다 영호 의논 — 자율 진행 금지)

1. **SnapThreshold 1.5f 축소 숫자** — 고정스텝 효과 *실측 후* 별도 결정. 이 Phase에서 숫자 안 박음.
2. **새 공유 상수 필요 시** — substep cap 등을 `98_Shared/Constants.cs`에 박아야 해 보이면 STOP(§4 양쪽 영향 + 값 결정은 의논).
3. **wire 변경 필요해 보이는 순간** — C_MoveIntent에 필드가 필요해 보이면 즉시 STOP(irreversible 경로 — ProtocolVersion bump는 사용자 명시 GO).

---

> Phase 완료 시 `04-...-DONE.md` 박제(복잡 등급). 게이트 통과 후 Phase 05(serverTick + 고정스텝 반영 클라 재빌드 — 전체 회귀) 착수. P3 그물 + 이 Phase의 baseline 갱신 사유가 회귀 판정의 단일 기준이 된다.
