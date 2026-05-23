#nullable enable
using Shared.GameData;
using UnityEngine;
using SysVector2 = System.Numerics.Vector2;
using SharedPhysics = Shared.GameData.Physics; // UnityEngine.Physics와 이름 충돌 회피

namespace Dawnholder.Client.Prediction
{
    // Phase 05 (M2): Client-side prediction + snap reconcile.
    // Phase 06 (M2): Input replay reconcile (서버 위치 + 미-ack 입력 재시뮬, snap 텔레포트 X).
    // Phase 07 (M2): Physics.Step 통합 — 양쪽 단일 출처 (헌법 #1). Y축 prediction + 점프 도입.
    //
    // **순수 C# 클래스 (MonoBehaviour 아님)** — 미래 EditMode 테스트 가능성 보존.
    // Unity 의존은 UnityEngine.Vector2 + Mathf.Abs 두 가지로 한정.
    //
    // **흐름** (Phase 07 갱신, 매 frame Predict 패턴 — Phase 06 정합):
    //   1. spawn 시점: LocalPlayerController가 SetInitialPosition(spawnPos)
    //   2. *매 frame* (Time.deltaTime 가변): LocalPlayerController가 Predict(inputX, jumpPressed, dt)
    //      → Physics.Step 호출 → Position/Velocity/OnGround 갱신. 시뮬 자체가 부드러움.
    //   3. *50ms cadence* (송신 throttle): C_MoveIntent 송신 + InputHistory push (정의 파일 #82
    //      "fps 의존 차단" = *송신 cadence* 의미. Predict 시뮬은 가변 dt OK).
    //   4. S_Snapshot 도착 시: UnityClientSession이 OnSnapshot(serverX/Y, serverVx/Vy, ackedTick)
    //      → mispredict (X 또는 Y > SnapThreshold) 시 서버 권위 상태에서 미-ack 입력 replay
    //      → 부드러운 정정 (Phase 06 패턴 그대로, snap 텔레포트 X)
    //
    // **양쪽 공식 일치 (헌법 #1 / ADR-010)**:
    //   Physics.Step은 Shared.GameData. 클라/서버 같은 함수 호출 → drift 0.
    //   *클라 가변 dt + 서버 fixed dt* 차이는 누적 결과상 근사 → 미세 drift는 reconcile로 흡수.
    //   타입은 UnityEngine.Vector2 ↔ System.Numerics.Vector2 변환 (PDL 주석 패턴).
    //
    // **장르 결정 — Phase 07 Step 4 사후 정정 (2026-05-17)**:
    //   초기 Step 4는 *fixed cadence Predict*로 박았으나 240Hz에서 *뚜두두둑* 끊김 발생
    //   (정의 파일 완료 조건 4 "200ms lag 부드러움"과 충돌). MMORPG/캐주얼 RPG 장르
    //   (ADR-006/009)는 fairness보다 부드러움 우선 — Source/Quake/Overwatch 패턴 정합.
    //   fixed-step + visual lerp (C 옵션)는 격투/콘솔 RTS 패턴이라 우리 게임에 over-engineering.
    //
    // **비교 축** (Phase 07): X + Y 둘 다 SnapThreshold 비교. Phase 06은 X만 — Y prediction
    //   도입으로 mispredict 가능성 양축에 박힘.
    public class PlayerPredictor
    {
        // Phase 05 튜닝 1.0f → M3.8 Phase 05 검증 시점 1.5f 갱신.
        // 사유: SnapshotTickInterval 5→2 (250ms→100ms broadcast 10Hz)로 박은 후 mispredict 검사
        // 빈도 4배 ↑ → 점프 중 클라 가변 dt vs 서버 fixed dt drift 누적이 1.0f 초과 빈도 ↑ →
        // 점프할 때 reconcile snap 자주 → 끊김 결함. 1.5f로 작은 drift 흡수.
        // 헌법 #1 영향 X — 큰 cheat (텔레포트)은 여전히 reconcile, 서버는 권위 그대로.
        public const float SnapThreshold = 1.5f;

        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }     // Phase 07: Y 속도 추가
        public bool OnGround { get; private set; } = true;
        public int SnapCount { get; private set; }

        // Phase 06: 송신된 입력의 (clientTick, inputX, jumpPressed) 보관 → snapshot의 ackedTick
        // 받으면 미-ack 입력만 replay. Phase 07에서 jumpPressed 동봉.
        readonly InputHistory _history = new InputHistory();

        public void SetInitialPosition(Vector2 pos)
        {
            Position = pos;
            Velocity = Vector2.zero;
            OnGround = pos.y <= 0.0001f; // ground 가정 (Physics.GroundY = 0)
            _history.Clear();
        }

        // Phase 06 Step 4: LocalPlayerController가 C_MoveIntent 송신 직후 호출.
        // Phase 07: jumpPressed 동봉 — replay 시 점프 시도 재현.
        public void NotifySent(uint clientTick, sbyte inputX, bool jumpPressed)
        {
            _history.Push(clientTick, inputX, jumpPressed);
        }

        // Phase 07: *매 frame* Time.deltaTime 가변 호출 (Phase 06 패턴 + jumpPressed 추가).
        // 클라 가변 dt + 서버 fixed dt 차이는 누적상 근사 → reconcile 흡수.
        // Physics.Step 단일 출처 호출 → 양쪽 공식 일치 (헌법 #1).
        public void Predict(sbyte inputX, bool jumpPressed, float dt)
        {
            PhysicsState after = SharedPhysics.Step(ToPhysicsState(),
                new PhysicsInput(inputX, jumpPressed, dt));
            ApplyPhysicsState(after);
        }

        // Phase 07: serverVx/serverVy 추가. mispredict 검사도 X+Y 둘 다.
        //
        // **알고리즘 (Phase 06 패턴 + Phase 07 확장)**:
        //   1. mispredict 검사 — |dX| > threshold OR |dY| > threshold
        //   2. mispredict 시: 서버 권위 (pos + vel + ground 추정) 박고 미-ack 입력 replay
        //   3. 항상 InputHistory.EvictUpTo(ackedClientTick) — 메모리 위생
        //
        // **헌법 #1 유지**: cheat 시뮬도 서버 권위 좌표 기준 → cheat 흡수 X.
        public bool OnSnapshot(float serverX, float serverY,
                               float serverVx, float serverVy,
                               uint ackedClientTick)
        {
            float dx = serverX - Position.x;
            float dy = serverY - Position.y;
            bool mispredict = Mathf.Abs(dx) > SnapThreshold
                           || Mathf.Abs(dy) > SnapThreshold;

            if (mispredict)
            {
                // 서버 권위 상태에서 출발 — 위치 + 속도 + ground (위치로 추정)
                Position = new Vector2(serverX, serverY);
                Velocity = new Vector2(serverVx, serverVy);
                OnGround = serverY <= 0.0001f && serverVy <= 0f;

                // 미-ack 입력 재시뮬 (서버 권위 → 클라 현재까지)
                foreach (InputRecord rec in _history.ReplayFrom(ackedClientTick))
                {
                    PhysicsState after = SharedPhysics.Step(ToPhysicsState(),
                        new PhysicsInput(rec.InputX, rec.JumpPressed, Constants.TickDuration));
                    ApplyPhysicsState(after);
                }
                SnapCount++;
            }

            // 항상 ack된 입력 정리 — 메모리 위생 (Phase 06 패턴).
            _history.EvictUpTo(ackedClientTick);
            return mispredict;
        }

        // === Vector2 변환 헬퍼 (PDL 주석 패턴 — System.Numerics ↔ UnityEngine) ===

        PhysicsState ToPhysicsState() => new PhysicsState(
            new SysVector2(Position.x, Position.y),
            new SysVector2(Velocity.x, Velocity.y),
            OnGround);

        void ApplyPhysicsState(PhysicsState s)
        {
            Position = new Vector2(s.Position.X, s.Position.Y);
            Velocity = new Vector2(s.Velocity.X, s.Velocity.Y);
            OnGround = s.OnGround;
        }
    }
}
