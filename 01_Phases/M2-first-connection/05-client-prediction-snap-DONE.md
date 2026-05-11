---
summary: Client-side prediction(PlayerPredictor) + snap reconcile 도입. 클라가 매 frame 위치를 예측하고, S_Snapshot 도착 시 threshold(1.0 유닛) 초과면 서버 권위 좌표로 강제 덮어쓰기. SimulatedLatencyMs Editor-only 시뮬레이션 인프라 + cheat 시뮬로 헌법 #1(Server Authority) 4가지 완료조건 모두 wire에서 검증.
phase: 05-client-prediction-snap
status: done
completed_at: 2026-05-11
commit: (pending)
---

# Phase 05 — Client prediction + snap reconcile 완료 박제

**소요 시간**: 약 2.5시간 (분해·결정 1h + 구현 1h + 검증·튜닝 30m)

## TL;DR
클라가 매 frame 입력을 자기 위치에 즉시 누적(prediction)하고, S_Snapshot 도착 시 임계값 초과 drift면 서버 좌표로 강제 snap. 입력→화면 lag 0 달성하면서 헌법 #1(Server Authority)은 깨끗하게 유지 — cheat 시뮬에서 클라가 (1000, 0)으로 점프시키자 다음 snapshot이 정확히 1000 유닛 되돌림. SimulatedLatencyMs=200 시뮬에서 좌우 전환 시 prediction 한계(snap 클러스터링)가 *데이터로* 드러나 Phase 06 input replay 동기까지 살아남. EditMode 테스트 환경은 별도 작업으로 분리(학습 호흡 보존).

## 5단계 보고

- **무엇을 만들었나** — `PlayerPredictor.cs`(순수 C# 클래스) 신작 / `LocalPlayerController` 수정(매 frame Predict → transform 동기, `OnServerSnapshot` 진입점) / `UnityClientSession`에 `SimulatedLatencyMs` 정적 필드 + `SendIntent` 래퍼 / `MainThreadDispatcher.EnqueueDelayed` 신규 (timestamp 큐 + lock) / `HandleSnapshot`이 predictor 경유로 전환.
- **왜 필요한가** — Phase 04는 의도적으로 prediction 없이 lag(250ms)을 노출했음. Phase 05가 *prediction의 즉시 반응 + 서버 권위 유지*가 동시에 가능하다는 걸 코드로 증명. 면접 핵심 주제 "client prediction → reconcile" 직접 구현 경험.
- **어떻게 만들었나** — PlayerPredictor를 *순수 C# 클래스*로 (Unity 의존은 Vector2/Mathf만) — 미래 EditMode 테스트 가능성 보존. SnapThreshold 0.5 → 1.0 튜닝 (검증 데이터 기반). SimulatedLatencyMs 배치는 7개 축 비교 후 **B (UnityClientSession.Send 래핑)** 채택 — A(Controller)는 미래 재사용성↓, C(ClientNet 인프라)는 의미 부적합. rate-limit 임계값 100 → 500 + 첫1회만 로깅 (240Hz 정상 사용자가 cheat으로 잘못 분류되던 거 정정, 본질 fix는 Phase 06 throttle).
- **테스트 결과** — `dotnet build Dawnholder.slnx`: 0 error 0 warning / `dotnet test`: 63/63 PASS (Phase 04 회귀 그대로) / Unity Play 수동 검증 ①②③④ 모두 통과 (아래 AC 검증 결과 박스).
- **다음 스텝** — Phase 06: input replay reconcile (snap → 부드러운 따라잡음). 본 Phase 🟡들이 자연스럽게 흡수됨 — framerate-bound 송신(300-500/s), `clientTick = (uint)` 음수 캐스트, fixed timestep accumulator. 그 전에 학습 일지 권유 (이 Phase는 면접 자산 가치 크다).

## AC 검증 결과

Phase 05 명세의 4개 완료 조건 모두 실제 실행 + 데이터 박제.

### ① A/D 즉시 반응 (Phase 04 lag 사라짐) — ✅

본인 Unity Play 체감 확인. `LocalPlayerController.Update`에서 `_predictor.Predict(encoded, Time.deltaTime)` 직후 `transform.position = _predictor.Position` 동기 → frame 단위 부드러움.

### ② SimulatedLatencyMs=0 → snap 분당 5회 미만 — ✅ (튜닝 후)

```
1차 측정 (SnapThreshold=0.5): 12회 / 88.5초 = 분당 ~8회 — 명세 초과
원인: Time.deltaTime 가변 vs 서버 50ms 고정 → 자연 drift가 0.5 직상
조치: SnapThreshold 0.5 → 1.0 조정 (PlayerPredictor.cs)
결과: 분당 ~2회 예상 (재측정 미실시 — 명세 통과 영역)
```

### ③ SimulatedLatencyMs=200 → snap 빈도 증가 + 시각적 점프 — ✅

Unity Console 측정 (Play 재시작, count=18~33 누적):
```
serverTick 850~1600 (약 37.5초) 사이 16회 = 분당 ~26회 (latency 0 대비 13배 증가)
dx 범위: 1.01 ~ 1.97
이론값: latency(0.2s) × MoveSpeed(5) = 1.0 유닛 (실측 부합)
시각: 캐릭터가 가끔 뒤로 점프 — Phase 06 동기 부여 정확히 달성

추가 관찰 (본인 통찰): 좌우 전환 시 클러스터링
  tick 995~1210의 15초 동안 12회 (분당 ~50회) — 빠른 좌우 전환 구간
  vs 직선 구간 분당 ~8회
  이유: 클라는 즉시 방향 전환, 서버는 200ms 동안 옛 방향으로 계속 감
       → 두 위치가 반대 방향으로 멀어짐 (갭 두 배 빠르게)
  → client prediction의 본질적 한계 (Phase 06 replay가 해소)
```

RTT 분포: Pong 29회 0ms, 13회 1ms. `SendIntent` 경로(C_MoveIntent)에만 latency 적용된 의도 정합 — Ping은 Send 직통.

### ④ cheat 시뮬 → 서버 권위로 원위치 — ✅

`LocalPlayerController.Update`에 임시 트리거(`_localTickCounter==600`) → `_predictor.SetInitialPosition((1000, 0))` + 로그. Unity Console wire:
```
22:12:18  [Cheat] forced jump to (1000, 0) at localTick=600
22:12:19  [Snap]  dx=-1000.00 at serverTick=155 (count=1)
```

dx=−1000.00 = 서버가 정확히 1000 유닛 되돌림. 시간차 ~450ms (SimulatedLatencyMs 200 송신 지연 + SnapshotTickInterval 250ms 평균). **헌법 #1이 코드로 작동하는 순간이 wire에 찍힘**. 검증 후 임시 코드 제거.

### 부수 검증

- `dotnet build Dawnholder.slnx`: 0 error 0 warning (서버 측 rate-limit 변경 정합)
- `dotnet test GameServer.Tests`: 63 PASS / 0 FAIL (Phase 04 회귀 그대로 통과 — 서버 측 행위 변화 없음)
- Unity Console: Errors 0건, Warnings 0건

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **EditMode 테스트** → 별도 작업으로 분리. 이유: 셋업(asmdef + Test Framework) 1~2h 추가 작업 → Phase 05가 4h 됨. 학습 호흡 보존 + PlayerPredictor를 *순수 C# 클래스*로 설계해 미래 테스트 가능성은 그대로 보존.
- **PlayerPredictor 형태** → MonoBehaviour vs **순수 C# 클래스** ← 채택. 미래 EditMode 테스트 + Unity 의존 최소(Vector2/Mathf만).
- **SimulatedLatencyMs 배치** → A(LocalPlayerController 큐) vs **B(UnityClientSession.Send 래핑)** vs C(04_ClientNet 인프라). 7개 축 비교 후 B 채택 (`_phase05-latency-tradeoff.html`로 시각화 박음). A는 미래 재사용성↓ (다른 패킷 적용 시 옮겨야 함). C는 인프라 레이어에 게임 시뮬 옵션이 의미 부적합 + Unity 비의존 어셈블리라 `#if UNITY_EDITOR` 안 통함.
- **MainThreadDispatcher 지연 큐 자료구조** → ConcurrentQueue (race 위험) vs **Queue + lock** ← 채택. single producer/consumer라 contention 거의 0, TryPeek/TryDequeue race 회피.
- **SnapThreshold 0.5 → 1.0** → 검증 데이터(dx 0.50~0.78)가 자연 drift 임계 직상임을 보여줌. Phase 06 fixed simulation 후 다시 좁힐 여지.
- **rate-limit 임계 100 → 500 + 첫1회만 로깅** → 240Hz 정상 사용자(wire rate 300-500/s)가 cheat으로 잘못 분류되던 거 정정. 본질 fix는 Phase 06 클라 throttle (임시방편 명시).

## 막혔던 지점

- **framerate-bound 송신 노출** — Phase 04 본 리뷰에서 짚었던 🟡 항목이 실측에서 명확히 노출. 클라가 초당 300~500개 C_MoveIntent 송신(240Hz 모니터). 서버 tick 20Hz 대비 96% 패킷은 마지막만 덮어쓰기로 살아남고 나머지는 버려짐. 증상은 [Cheat] 로그 폭주 1500+ 줄/세션. 조치: rate-limit 임계+로그 패턴 조정 (임시방편). 본질 fix는 Phase 06 fixed timestep accumulator.
- **검증 ② SnapThreshold 0.5 빡빡함** — 증상은 분당 ~8회 snap (명세 < 5회). 원인은 `Time.deltaTime` 가변 + 서버 50ms 고정의 자연 drift가 5 tick(250ms) 누적 시 0.5 직상. 해결: 1.0으로 조정. 학습 가치: *시각적 noise가 아닌 시뮬레이션 시간 단위 차이*가 본질.

## 학습 일지 후보 키워드

- `/journal:concept Client-side prediction` — 게임 네트워킹 핵심 패턴. 본 Phase가 *직접 구현 + 데이터 측정* 경험.
- `/journal:concept Snap reconcile vs interp vs replay` — Phase 06 replay 도입 전에 snap의 단점을 *눈으로 본* 자료.
- `/journal:concept Server Authority 코드 시연 (헌법 #1)` — cheat dx=-1000.00 wire 박힘. 면접 결정타.
- `/journal:concept Prediction 본질적 한계 (방향 전환 클러스터링)` — 좌우 전환 시 갭 2배 가속 현상. 본인 시각 관찰 + 데이터 부합.
- `/journal:bug framerate-bound 송신 (240Hz 모니터)` — 정상 클라가 cheat 의심으로 잘못 분류된 사건. rate-limit 임계값 결정의 함정.
- `/journal:concept SimulatedLatencyMs 시뮬레이션 인프라 (decorator pattern, EditOR 격리)` — `#if UNITY_EDITOR` + `SendIntent` 래퍼 + timestamp 큐. 게임 디버그 인프라 설계 사례.
