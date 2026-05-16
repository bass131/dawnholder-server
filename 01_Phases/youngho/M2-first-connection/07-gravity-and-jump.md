# Phase 07: 중력 + 점프

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 2.5~3시간
> **담당 에이전트**: gameplay + client

---

## 🎯 목표

사이드스크롤 게임답게 **점프**가 된다. 중력 + 지면 collision은 서버 권위. Space 키 → C_MoveIntent에 jump bit → 서버가 ground일 때만 vy 부여 → 자연스러운 포물선. 클라 prediction도 같은 물리 공식을 써서 lag 환경에서도 부드럽게.

이 Phase가 끝나면 옵션 B(1인 movement) **데모 가능**.

---

## ⏪ 사전 조건

- [ ] Phase 06 완료 (prediction + replay reconcile)

---

## 📝 작업 내용

- [ ] **공유 물리 공식** (`98_Shared/GameData/Physics.cs`):
    - `Gravity = -20.0f` (units/s²)
    - `JumpSpeed = 8.0f` (units/s)
    - `GroundY = 0.0f`
    - 단일 step 함수: `static (Vector2 pos, Vector2 vel, bool onGround) Step(Vector2 pos, Vector2 vel, sbyte inputX, bool jumpPressed, float dt)`
    - 함수 안에서 horizontal 이동 + 중력 + ground clamp + jump 처리. **결정론**.
- [ ] **PDL 수정**:
    - `C_MoveIntent`에 `byte jumpPressed` 비트 추가 (또는 inputX와 합쳐 1바이트 비트필드).
    - `S_Snapshot`에 `float vx, float vy` 추가 (replay/prediction에 필요).
- [ ] **서버 측**:
    - `PlayerEntity`에 `Velocity` 추가, `OnGround` 캐시.
    - `GameMap.Tick` 안에서 각 player의 `Physics.Step` 호출 → 결과 적용.
    - jump 검증: `OnGround == true`일 때만 vy 부여 (공중 더블점프 차단 — 헌법 #3 trust boundary).
- [ ] **클라 측**:
    - `PlayerPredictor`에서 `Physics.Step`을 똑같이 사용 (양쪽 단일 출처).
    - 입력 모듈에 Space 키 → jumpPressed bit.
    - InputHistory에 jumpPressed도 함께 저장 (replay 시 필요).
    - Snapshot에서 velocity도 받아 prediction state 동기화.

---

## ✅ 완료 조건

- [ ] Unity Play → A/D + Space로 좌우 이동 + 점프. 자연스러운 포물선.
- [ ] 공중에서 Space 두 번째 → 두 번째 점프 안 됨 (서버 차단)
- [ ] Cheat: 클라가 Space를 강제로 매 frame 전송 → 서버는 ground일 때만 1회만 적용
- [ ] 인위적 lag 200ms에서도 prediction 부드러움 유지 (Phase 06 reconcile 패턴 그대로 작동)
- [ ] 회귀: 좌우 이동만 (점프 없이) Phase 06 시나리오와 동일 동작

---

## 🧪 테스트

**자동 테스트:**
- `PhysicsTests` (Shared) — Step 함수 입력별 예상 결과 테이블 검증.
- `JumpValidationTests` (서버) — 공중 점프 시도 → 무시.
- 클라 prediction이 서버 결과와 일치하는지 mock 테스트.

**수동 테스트:**
- 영상 캡처용 시나리오 확정: 좌우 + 점프 + 연속 점프(ground 닿자마자) 사이클.

---

## 📚 학습 포인트

- **공식 = Shared, 실행 = 서버** (헌법 #1): Physics.cs를 양쪽이 *읽기만* 함. 한 곳에서 결과를 *적용*하는 건 서버.
- **결정론**: 같은 입력 → 같은 출력. float은 동일 플랫폼 + 같은 컴파일 옵션이면 충분히 결정론적.
- **State 동기화**: position만 보내면 velocity 재계산 필요 → 같이 보내는 게 안전.
- **Anti-cheat 패턴**: jump 검증은 *서버가 가진 ground 상태*로만. 클라 보고 받지 않음.
- **fixed timestep의 위력**: dt가 항상 0.05면 결과 재현. 변수면 결정론 깨짐.

---

## ⚠️ 함정 / 주의사항

- Unity 측에서 `Rigidbody2D` 쓰면 자체 물리 → Shared 공식과 결과 달라짐. **Rigidbody2D 금지**, 순수 transform + 직접 계산.
- `dt = Time.deltaTime` 그대로 쓰면 fps 의존 → Phase 06에서 tick aligned로 갔다면 dt = TickDuration 고정.
- 중력 부호 헷갈림(2D는 Y up이면 +가 위, gravity는 -). 일관성 유지.
- 점프 직후 같은 tick 안에서 ground 판정이 true로 남으면 즉시 재점프 가능 → jump 적용 후 OnGround = false로 먼저 변경.
- jumpPressed가 "에지(눌린 순간)"인지 "상태(누르고 있는 동안)"인지 합의 — 보통 에지. 클라가 키 down 1tick만 true 보내야 의도와 맞음.

---

## ➡️ 다음 Phase

- Phase 08: 회귀 안전망 + 데모 영상 + p99 측정 — M2 완료 증명

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
