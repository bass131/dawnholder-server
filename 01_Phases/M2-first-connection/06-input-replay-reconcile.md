# Phase 06: Input replay 기반 reconcile

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 2~3시간
> **담당 에이전트**: client (+ shared)

---

## 🎯 목표

Phase 05의 *snap* 대신, 서버가 **어디까지 처리했는지(LastAckedClientTick)** 를 알려주면 클라가 그 시점의 서버 위치에서 출발해 **그 이후 보낸 입력들을 재실행(replay)** 한다. snap의 순간 점프가 사라지고 부드러운 보정.

이 패턴이 본인의 면접 무기 1순위. 현업 MMO/FPS 표준이고, 본인이 직접 구현했다고 말할 수 있는 영역.

---

## ⏪ 사전 조건

- [ ] Phase 05 완료 (prediction + snap 동작)

---

## 📝 작업 내용

- [ ] **PDL 수정** — `S_Snapshot`에 `uint lastAckedClientTick` 필드 추가. PacketGenerator 재실행.
- [ ] **서버 측**:
    - `PlayerEntity._lastClientTick`을 Phase 04에서 받아둔 값으로 매 intent 시 갱신.
    - `S_Snapshot` 브로드캐스트 시 `_lastClientTick`을 함께 전송.
- [ ] **클라 측**:
    - `InputHistory` 큐 — `(clientTick, inputX)` pairs. 최근 ~5초(=100 tick) 보관, 그 이상 은 ack 받았으면 정리.
    - `C_MoveIntent` 전송 시 `clientTick`을 함께 보내고, 같은 값으로 InputHistory에 push.
    - `OnSnapshot`: snap 대신 다음 알고리즘:
        ```
        if (|snapshot.X - _predictedAtTick(snapshot.lastAckedClientTick).X| > Threshold):
            // mispredict 발생
            position = (snapshot.X, snapshot.Y)
            for each input in InputHistory where input.tick > snapshot.lastAckedClientTick:
                position += input.inputX * MoveSpeed * TickDuration
            _predictedPosition = position
        ```
    - 즉, "서버가 인정한 위치"에서 출발해 "아직 ack 못 받은 입력만" 재실행.
    - InputHistory에서 ack된 입력은 정리.
- [ ] (옵션) prediction을 매 frame이 아니라 **tick aligned**로 변경 — 클라도 50ms 고정 simulation step. fps 무관, replay 결정론 확보.

---

## ✅ 완료 조건

- [ ] SimulatedLatencyMs = 200 → snap-점프 거의 사라지고 부드러운 위치 유지 (Phase 05 대비 명확한 개선)
- [ ] Mispredict 발생 빈도는 Phase 05와 비슷하지만 **시각적 점프가 안 보임**
- [ ] 인위적 cheat(클라 강제 점프)는 여전히 즉시 보정됨 (Server Authority 유지)
- [ ] 30초 연속 입력 시 InputHistory 메모리 무한 증가 X (ack로 정리됨 확인)

---

## 🧪 테스트

**자동 테스트:**
- `InputHistoryTests` — push N개 → ack k → 큐 길이 N-k 검증
- `ReconcileTests` — mispredict 시나리오 모킹 → 재계산 위치가 단순 snap이 아닌 replay 결과와 일치

**수동 테스트:**
- Latency 200ms로 5분 플레이 → 점프 발생 카운트 (시각 + 로그)
- Phase 05 대비 같은 환경에서 체감 차이 비교 (이게 면접 서사)

---

## 📚 학습 포인트

- **Input prediction/replay**: Source 엔진 / Quake / 대부분의 FPS·MMO에서 쓰는 표준 패턴.
- **결정론적 simulation의 중요성**: 양쪽이 같은 결과를 내야 replay가 의미 있음. float은 위험하지만 단순 좌우 이동은 충분히 안전.
- **ack 의미**: 서버가 "여기까지 처리했다"고 알려주는 게 client/server 합의의 핵심.
- **tick aligned client**: 클라가 50ms 단위로 simulation하면 결정론 확보가 쉬워짐. 단, 렌더링은 별도 interp 필요 (M3에서).
- **메모리 위생**: 히스토리 큐는 반드시 정리. 안 그러면 누수.

---

## ⚠️ 함정 / 주의사항

- 클라 prediction step과 서버 tick step이 다르면 replay 누적 오차 → tick aligned 가는 게 정공법.
- float 누적은 매 tick 미세 오차 → 100 tick 후 0.001 정도 drift, threshold 안으로 흡수됨. 안 되면 fixed-point 고려(지금은 X).
- ack 받기 전에 InputHistory가 비면 위험 — 항상 보낸 직후 push, 정리는 snapshot 받은 후만.
- `_predictedAtTick`는 별도 보관 필요 또는 매번 큐 재시뮬. 단순화하려면 매번 재시뮬 (성능 부담 미미).
- replay 중 사이드 이펙트(이벤트 발생 등) 없도록 — pure 위치 계산만.

---

## ➡️ 다음 Phase

- Phase 07: 중력 + 점프 — 사이드스크롤다운 점프 동작 (서버 권위 + prediction)

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
