# Phase 05: Client prediction + snap reconcile

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 2시간
> **담당 에이전트**: client

---

## 🎯 목표

키 입력 즉시 클라가 자기 캐릭터를 *미리* 움직인다(prediction). 서버 snapshot이 도착하면 비교 — 일치하면 그대로, 어긋나면 서버 위치로 즉시 **snap** (보간/replay 없이). lag 체감이 사라지지만, snap 발생 시 *순간 점프*가 보이는 게 의도된 결과 — 다음 Phase의 동기 부여.

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (MoveIntent + Snapshot, MoveSpeed가 Shared에 박혀있음)

---

## 📝 작업 내용

- [ ] **클라 측** (`03_Client/Assets/Scripts/Prediction/`):
    - `PlayerPredictor.cs` 신작 — 매 frame 입력값 × `Shared.GameData.Constants.MoveSpeed` × `Time.deltaTime` 누적해 `_predictedPosition` 갱신.
    - PlayerView GameObject의 `transform.position`은 매 frame `_predictedPosition`을 따라감 (보간 없이 직접).
    - C_MoveIntent 전송은 Phase 04 그대로 (50ms 주기 또는 매 frame).
    - `OnSnapshot(S_Snapshot)`: `|snapshot.X - _predictedPosition.X| > SnapThreshold` 면 `_predictedPosition = (snapshot.X, snapshot.Y)` 로 강제 덮어쓰기 + 로그.
    - `SnapThreshold = 0.5f` (튜닝 가능, 우선 0.5 유닛).
- [ ] **공유 상수 정합 확인** — `Shared.GameData.Constants.MoveSpeed`가 양쪽에서 같은 값. 절대 클라 측에서 별도 const 박지 말 것.
- [ ] **인위적 lag 시뮬레이션 옵션** (옵션이지만 강추):
    - `04_ClientNet/` 또는 클라 송수신 큐에 `SimulatedLatencyMs` (Editor only) — 송신 패킷을 N ms 지연 후 실제 전송. 200ms 정도로 테스트.
- [ ] snap 발생 카운터 — 콘솔에 "[Snap] dx=0.83 at tick 142" 로깅.

---

## ✅ 완료 조건

- [ ] Unity Play → A/D 누르는 순간 캐릭터가 **즉시** 움직임 (Phase 04 lag 사라짐)
- [ ] SimulatedLatencyMs = 0일 때 snap 거의 발생 안 함 (분당 5회 미만 정도)
- [ ] SimulatedLatencyMs = 200일 때 snap 빈도 증가, 화면에서 가끔 *점프*가 보임 — 이게 의도
- [ ] 클라가 cheat 시뮬: `_predictedPosition`을 강제로 (1000, 0)으로 점프시키면 snapshot 도착하자마자 원위치로 snap (Server Authority 시연)

---

## 🧪 테스트

**자동 테스트:**
- `PlayerPredictorTests` (Unity 측 — EditMode test): 입력 시퀀스 + snapshot 시퀀스 모킹 → 예상 위치/snap 카운트 확인.
- 서버 측은 변경 없음 (Phase 04 회귀 그대로 통과해야 함).

**수동 테스트:**
- Play → 즉시 반응 체감
- Latency 200ms 토글 → snap 빈도 시각 확인
- 디버그 키로 강제 cheat → 원위치 snap

---

## 📚 학습 포인트

- **Client-side prediction**: 본인 캐릭터에만 적용. 다른 플레이어는 서버 snapshot 기반 보간 (M3).
- **Authoritative server의 실전 의미**: prediction은 *예상*일 뿐, 서버가 항상 정답. 다르면 서버 따름.
- **양쪽 공식 일치의 무게**: MoveSpeed 한 글자 다르면 매 tick drift → snap 폭주.
- **Snap vs interp vs replay**: 가장 단순한 reconcile 전략이 snap. Phase 06에서 replay 도입, 그 전엔 snap의 단점을 본인 눈으로 봐야 함.
- **Threshold 의미**: 너무 작으면 미세 drift도 점프, 너무 크면 cheat 통과. 0.1~1.0 사이 튜닝.

---

## ⚠️ 함정 / 주의사항

- 클라가 자체 MoveSpeed를 박으면 서버와 다른 값일 때 **반드시** drift → 무한 snap. Shared 상수 강제.
- snapshot이 도착하기 전에 prediction을 너무 멀리 끌고가면 snap 거리 폭증 → 어차피 부자연스러움. Phase 06이 해결.
- Time.deltaTime이 큰 프레임(스파이크)에 큰 점프 → 서버는 50ms 고정이라 prediction 결과 다를 수 있음. 일단 무시, M3 이후 fixed simulation 도입 고려.
- SimulatedLatencyMs 코드를 Release 빌드에 포함시키면 안 됨 — `#if UNITY_EDITOR`로 감싸기.
- snap 시 카메라가 같이 점프하면 어지러움 — 카메라는 부드러운 follow로 (Phase 01 CameraFollow가 lerp면 자동 완화).

---

## ➡️ 다음 Phase

- Phase 06: Input replay 기반 reconcile — snap 대신 last-ack 이후 입력 재실행으로 부드럽게

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
