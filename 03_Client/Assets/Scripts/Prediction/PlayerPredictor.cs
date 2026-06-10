#nullable enable
using Shared.GameData;
using UnityEngine;
using SysVector2 = System.Numerics.Vector2;
using SharedPhysics = Shared.GameData.Physics; // UnityEngine.Physics와 이름 충돌 회피

namespace Dawnholder.Client.Prediction
{
    // Client-side prediction + input replay reconcile (서버 위치 + 미-ack 입력 재시뮬, snap 텔레포트 X).
    //
    // **순수 C# 클래스 (MonoBehaviour 아님)** — 미래 EditMode 테스트 가능성 보존.
    // Unity 의존은 UnityEngine.Vector2 + Mathf.Abs 두 가지로 한정.
    //
    // **흐름**:
    //   1. spawn 시점: SetInitialPosition(spawnPos)
    //   2. *매 frame* (Time.deltaTime 가변): Predict(inputX, jumpPressed, dt)
    //      → Physics.Step 호출 → Position/Velocity/OnGround 갱신. 시뮬 자체가 부드러움.
    //   3. *50ms cadence* (송신 throttle): C_MoveIntent 송신 + InputHistory push.
    //   4. S_Snapshot 도착 시: OnSnapshot(serverX/Y, serverVx/Vy, ackedTick)
    //      → mispredict (X 또는 Y > SnapThreshold) 시 서버 권위 상태에서 미-ack 입력 replay
    //      → 부드러운 정정 (snap 텔레포트 X)
    //
    // **양쪽 공식 일치 (헌법 #1 / ADR-010)**:
    //   Physics.Step은 Shared.GameData. 클라/서버 같은 함수 호출 → drift 0.
    //   *클라 가변 dt + 서버 fixed dt* 차이는 누적 결과상 근사 → 미세 drift는 reconcile로 흡수.
    //
    // **장르 결정 (매 frame 가변 Predict + reconcile, fixed cadence X)**:
    //   fixed cadence Predict는 고프레임에서 끊김 발생. MMORPG/캐주얼 RPG 장르
    //   (ADR-006/009)는 fairness보다 부드러움 우선 — Source/Quake/Overwatch 패턴 정합.
    //   fixed-step + visual lerp는 격투/콘솔 RTS 패턴이라 우리 게임에 over-engineering.
    public class PlayerPredictor
    {
        // 점프 중 클라 가변 dt vs 서버 fixed dt drift 누적이 1.0f를 자주 초과 → reconcile snap 끊김.
        // 1.5f로 작은 drift 흡수. 헌법 #1 영향 X — 큰 cheat (텔레포트)은 여전히 reconcile, 서버 권위 그대로.
        public const float SnapThreshold = 1.5f;

        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public bool OnGround { get; private set; } = true;
        public int SnapCount { get; private set; }

        // 직업별 이동 파라미터 — PlayerStats factory 단일 출처(헌법 #4). fail-loud: 기본값 없음.
        readonly MoveParams _move;

        // 송신된 입력의 (clientTick, inputX, jumpPressed) 보관 → snapshot의 ackedTick
        // 받으면 미-ack 입력만 replay.
        readonly InputHistory _history = new InputHistory();

        MapTerrain? _terrain;

        public PlayerPredictor(MoveParams move)
        {
            _move = move;
        }

        // 맵 전환 시 새 맵 terrain 주입. null 전달 시 평지 fallback (Physics.Step null 경로).
        public void SetTerrain(MapTerrain? terrain) => _terrain = terrain;

        // 서버 권위 상태 리셋 시점의 접지 판정 — 옛 평지 가정(y<=0)은 지형 단차 위에서 오판 →
        // replay 점프 입력이 서버와 어긋남. 착지 스냅이 face 정확값을 주므로 등호 포함 eps 비교
        // (등호 경계 = 일상 도달 상태 — 벽점프 사례 carry-over). 의미론 출처 = Physics.StepWithTerrain
        // 착지 스냅. 세 번째 유사 구현 등장 시 Shared 추출 (Rule of Three).
        bool IsGroundedAt(float x, float y, float vy)
        {
            if (vy > 0f) return false;
            if (_terrain == null) return y <= 0.0001f;

            const float Eps = 0.0001f;
            foreach (TerrainAabb s in _terrain.Solids)
                if (x >= s.MinX && x <= s.MaxX && y >= s.MaxY - Eps && y <= s.MaxY + Eps)
                    return true;
            foreach (TerrainPlatform p in _terrain.Platforms)
                if (x >= p.MinX && x <= p.MaxX && y >= p.Y - Eps && y <= p.Y + Eps)
                    return true;
            return false;
        }

        public void SetInitialPosition(Vector2 pos)
        {
            Position = pos;
            Velocity = Vector2.zero;
            // 지형 모드에서 spawnY가 지형 위(예: Town y=1.39)이므로 평지 가정 불가.
            // false로 시작하면 서버도 같은 Step에서 중력을 적용해 함께 낙하 → 첫 몇 틱 drift를 reconcile이 흡수.
            OnGround = false;
            _history.Clear();
        }

        // LocalPlayerMovement가 C_MoveIntent 송신 직후 호출. jumpPressed 동봉 — replay 시 점프 시도 재현.
        public void NotifySent(uint clientTick, sbyte inputX, bool jumpPressed)
        {
            _history.Push(clientTick, inputX, jumpPressed);
        }

        // *매 frame* Time.deltaTime 가변 호출. 클라 가변 dt + 서버 fixed dt 차이는 reconcile 흡수.
        // Physics.Step 단일 출처 호출 → 양쪽 공식 일치 (헌법 #1). terrain 주입 경로 동일.
        public void Predict(sbyte inputX, bool jumpPressed, float dt)
        {
            PhysicsState after = SharedPhysics.Step(ToPhysicsState(),
                new PhysicsInput(inputX, jumpPressed, dt), _terrain, _move);
            ApplyPhysicsState(after);
        }

        // **알고리즘**:
        //   1. mispredict 검사 — |dX| > threshold OR |dY| > threshold
        //   2. mispredict 시: 서버 권위 (pos + vel + ground 추정) 박고 미-ack 입력 replay
        //   3. 항상 InputHistory.EvictUpTo(ackedClientTick) — 메모리 위생
        //
        // **헌법 #1 유지**: cheat 시뮬도 서버 권위 좌표 기준 → cheat 흡수 X.
        //
        // **forceAdopt** (HitState 넉백 표시): 클라는 서버 권위 넉백 임펄스(ExternalVelX)를 예측 못 함.
        //   피격 중엔 임계(SnapThreshold) 이내여도 서버 위치를 채택해 넉백을 시각화하고
        //   sub-threshold offset 누적(영구 어긋남)을 막는다. SnapCount는 *진짜 mispredict*에만 증가.
        public bool OnSnapshot(float serverX, float serverY,
                               float serverVx, float serverVy,
                               uint ackedClientTick, bool forceAdopt = false)
        {
            float dx = serverX - Position.x;
            float dy = serverY - Position.y;
            bool mispredict = Mathf.Abs(dx) > SnapThreshold
                           || Mathf.Abs(dy) > SnapThreshold;

            if (mispredict || forceAdopt)
            {
                // 임시 진단 — 버그 확정 후 제거 ([ReconDiag] 태그로 grep 가능)
                UnityEngine.Debug.Log(
                    $"[ReconDiag] snap dx={dx:F3} dy={dy:F3} thr={SnapThreshold}" +
                    $" mispredict={mispredict} forceAdopt={forceAdopt}" +
                    $" ackedTick={ackedClientTick} snapCount={SnapCount}");

                // 서버 권위 상태에서 출발 — 위치 + 속도 + ground (위치로 추정)
                Position = new Vector2(serverX, serverY);
                Velocity = new Vector2(serverVx, serverVy);
                OnGround = IsGroundedAt(serverX, serverY, serverVy);

                // 미-ack 입력 재시뮬 (서버 권위 → 클라 현재까지).
                // terrain 오버로드 동일 — replay가 평지로 돌면 서버-클라 대칭이 깨져 reconcile 자체가 어긋남.
                // 피격 중 입력은 source-gating으로 0이라 replay는 물리(중력)만 적용.
                foreach (InputRecord rec in _history.ReplayFrom(ackedClientTick))
                {
                    PhysicsState after = SharedPhysics.Step(ToPhysicsState(),
                        new PhysicsInput(rec.InputX, rec.JumpPressed, Constants.TickDuration), _terrain, _move);
                    ApplyPhysicsState(after);
                }
                if (mispredict) SnapCount++;
            }

            // 항상 ack된 입력 정리 — 메모리 위생.
            _history.EvictUpTo(ackedClientTick);
            return mispredict || forceAdopt;
        }

        // === Vector2 변환 헬퍼 (System.Numerics ↔ UnityEngine) ===

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
