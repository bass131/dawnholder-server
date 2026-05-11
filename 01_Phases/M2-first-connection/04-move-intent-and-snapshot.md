# Phase 04: C2S_MoveIntent + S2C_Snapshot (prediction 없이)

> **상태**: done (페어: [`04-move-intent-and-snapshot-DONE.md`](04-move-intent-and-snapshot-DONE.md))
> **마일스톤**: M2 First Connection
> **예상 소요**: 2.5~3시간
> **담당 에이전트**: netcode + gameplay

---

## 🎯 목표

클라가 키를 누르면 **의도(intent)**만 서버로 보내고, 서버가 적용한 결과(snapshot)가 도착해야만 화면이 움직인다. 의도적으로 prediction 없이 만들어 "lag 체감"을 노출 — 다음 Phase에서 prediction이 왜 필요한지 본인 눈으로 보게.

이 Phase가 끝나면 **헌법 #3(Trust Boundary)**의 첫 시연이 가능: 비정상 패킷을 보내도 서버가 거른다.

---

## ⏪ 사전 조건

- [ ] Phase 03 완료 (Enter/Leave 패킷, 클라가 서버 좌표로 spawn)

---

## 📝 작업 내용

- [ ] **PDL 정의 추가**:
    - `C_MoveIntent { byte inputX; uint clientTick; }` — `inputX ∈ {-1, 0, 1}` (실제로는 `sbyte`로). `clientTick`은 다음 Phase replay 대비 미리 박음.
    - `S_Snapshot { int entityId; float x; float y; uint serverTick; }` — 본인만 우선. 다른 플레이어는 M3.
- [ ] PacketGenerator 재실행 + 양쪽 컴파일 확인.
- [ ] **클라 측**:
    - `LocalPlayerController` 수정 — 입력 읽고 매 frame 또는 매 50ms마다 `C_MoveIntent` 전송. **transform은 절대 직접 안 움직임** (snapshot으로만).
    - `S_Snapshot` 핸들러 — 받은 좌표를 Player GameObject에 적용 (MainThreadDispatcher 경유).
- [ ] **서버 측**:
    - `GameSession.OnPacket_C_MoveIntent` — 검증(`Math.Abs(inputX) <= 1`)을 통과한 intent만 `PlayerEntity._pendingIntent`에 저장. 실패 시 cheat-log + 폐기.
    - `PlayerEntity` 필드 추가: `sbyte _pendingInputX`, `uint _lastClientTick`.
    - `GameMap.Tick` 안에서 각 player의 `_pendingInputX`를 적용 → `Position.X += inputX * MoveSpeed * dt` (dt = 0.05f). 적용 후 `_pendingInputX = 0` (계속 누르고 있으면 매 tick intent 재도착).
    - `MoveSpeed` 상수를 **`98_Shared/GameData/Constants.cs`**에 정의 (다음 Phase에서 클라 prediction이 같은 값 써야 함 — 미리 박음).
    - 매 5 tick(=250ms)마다 모든 player에게 `S_Snapshot` 브로드캐스트 (지금은 자기 자신 1명).
- [ ] **Rate-limit 골격** (헌법 #3): GameSession에 "초당 intent 수신 카운터" — 100/s 초과 시 cheat-log. 차단은 안 함(다음 Phase에서).

---

## ✅ 완료 조건

- [ ] Unity Play → A 누르면 ~250ms 지연 후 캐릭터 좌측으로 움직임, D면 우측, 안 누르면 정지
- [ ] **lag 체감 명확** — 키 떼도 ~250ms는 더 움직이는 느낌 (snapshot 주기 때문)
- [ ] DummyClient로 `inputX=99` 패킷 보내면 서버 cheat-log 발생, 위치 변화 없음
- [ ] DummyClient로 초당 1000 intent → cheat-log 발생 (rate-limit 감지)
- [ ] 30초간 정상 입력 → 서버 GameMap에 좌표 변화 정상 누적

---

## 🧪 테스트

**자동 테스트:**
- `GameServer.Tests/Maps/MoveIntentTests.cs`:
    - 정상 intent → `Position.X`가 예상대로 변함
    - `inputX = 5` → 무시되고 위치 변화 없음, cheat-log 호출 검증
- DummyClient 회귀 시나리오: connect → intent 100개 → 위치 누적값 확인

**수동 테스트:**
- Unity Play로 lag 체감 (이게 Phase 05 동기 부여)
- DummyClient로 비정상 패킷 던지기 (rate-limit + 범위)

---

## 📚 학습 포인트

- **Trust Boundary (헌법 #3)**: 클라가 보내는 모든 것은 untrusted. inputX 범위 / rate-limit / 소유권은 매번 검증.
- **Intent vs State**: 클라는 "내가 뭘 하려고 한다"(intent)만 보내고, 결과 위치(state)는 서버가 정한다. 좌표를 직접 보내는 게 절대 X (그러면 텔레포트 핵).
- **Snapshot 주기와 trade-off**: 매 tick 보내면 대역폭 폭증. 너무 띄엄띄엄이면 lag 체감 끔찍. 250ms는 보통의 출발점이고 prediction 도입 후 더 늘려도 됨.
- **양쪽 공유 상수**: MoveSpeed가 클라/서버 따로 박히면 prediction 들어가는 순간 무한 drift. Shared/GameData가 단일 출처.
- **Cheat 로깅의 가치**: 차단보다 *기록*이 우선. 패턴 보고 나서 차단 정책 결정 (보안의 일반 원칙).

---

## ⚠️ 함정 / 주의사항

- `byte inputX`로 박으면 -1 표현 불가. `sbyte` 또는 명시적 인코딩(0=정지, 1=좌, 2=우) — PDL이 sbyte 지원하는지 먼저 확인, 없으면 `int8` 또는 enum.
- 클라가 transform.position을 직접 건드리는 코드가 단 한 줄이라도 남아있으면 권위 위반 — Phase 01 코드의 LocalPlayerController에서 *반드시* 제거.
- 서버 dt를 `0.05f`로 박으면 tick rate 바뀔 때 깜빡 잊음. `Constants.TickDuration = 1.0f / 20.0f`로.
- `_pendingInputX`를 매 tick 0으로 리셋해야 키 떼면 정지. 안 그러면 무한 미끄러짐.
- snapshot 브로드캐스트가 JobQueue 안에서 큰 작업이면 tick duration 폭증 — 1명일 땐 무시 가능, 다인 시 측정.

---

## ➡️ 다음 Phase

- Phase 05: Client prediction + snap reconcile — 입력 즉시 시각화 + snapshot 불일치 시 snap

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
