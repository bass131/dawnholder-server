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
    //   2. *50ms 고정 서브스텝* (accumulator 기반): Predict(inputX, jumpPressed)
    //      → Physics.Step(dt=TickDuration) 호출 → Position/Velocity/OnGround 갱신.
    //   3. *50ms cadence* (서브스텝과 1:1): C_MoveIntent 송신 + InputHistory push.
    //   4. S_Snapshot 도착 시: OnSnapshot(serverX/Y, serverVx/Vy, ackedTick)
    //      → mispredict (X 또는 Y > SnapThreshold) 시 서버 권위 상태에서 미-ack 입력 replay
    //      → 부드러운 정정 (snap 텔레포트 X)
    //
    // **양쪽 공식 일치 (헌법 #1 / ADR-010)**:
    //   Physics.Step은 Shared.GameData. 클라/서버 같은 함수 호출 + 같은 TickDuration → drift 0.
    //   고정 서브스텝이 "dt-drift 덤불(SnapThreshold 확장 + force-adopt 게이트)"의 뿌리를 제거.
    public class PlayerPredictor
    {
        // P4 고정 서브스텝으로 dt-drift가 구조적으로 제거됨. 1.5f는 Dash lunge 등 서버 임펄스 오차 흡수 여지 보존 —
        // 축소는 고정스텝 효과 실측 후 별도 결정(STOP 포인트). 헌법 #1 영향 X.
        public const float SnapThreshold = 1.5f;

        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public bool OnGround { get; private set; } = true;
        public int SnapCount { get; private set; }

        // 직업별 이동 파라미터 — PlayerStats factory 단일 출처(헌법 #4). fail-loud: 기본값 없음.
        readonly MoveParams _move;

        // 송신된 입력의 (clientTick, inputX, jumpPressed, externalVelX) 보관 → snapshot의 ackedTick
        // 받으면 미-ack 입력만 replay.
        readonly InputHistory _history = new InputHistory();

        MapTerrain? _terrain;

        // === 임펄스 예측 상태 (M4.13 P5a — 서버 AttackState.Tick 클라 거울) ===
        // 서버는 대쉬/lunge 진입 틱에 EnterAttackState(startVx, decay, durationTicks) → 매 틱 vx를
        // Physics.Step ExternalVelX로 합성하고 DecayImpulse로 감쇠, durationTicks 후 0 정리(Exit).
        // 클라 live Predict가 같은 P4 공식(Physics.DecayImpulse)으로 같은 궤적을 *직접 예측* → forceAdopt 불요.
        float _impulseVx;            // 이번 틱 ExternalVelX로 주입할 임펄스 vx (0이면 임펄스 없음).
        float _impulseDecay;         // 틱당 감쇠 계수 (대쉬 1.0=등속, lunge 0.75).
        int _impulseTicksRemaining;  // 남은 임펄스 틱 — 0 도달 시 _impulseVx=0 정리 (서버 Exit 거울).

        // 직전 Predict가 이번 틱 Physics.Step ExternalVelX로 *실제 주입한* 임펄스 vx.
        // NotifySent가 이 값을 InputHistory에 저장 → "저장값 = live 적용값"(재계산 금지) 보장 단일 경로.
        public float LastAppliedImpulseVx { get; private set; }

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
            ClearImpulse();
            LastAppliedImpulseVx = 0f;
            _history.Clear();
        }

        // 임펄스 시작 — 대쉬/lunge 진입 시 LocalPlayerMovement가 호출 (서버 EnterAttackState 거울).
        //
        // **★시작 틱 정렬** (정의서 §확정 설계 plan-auditor 🟡 봉합):
        //   서버는 입력을 *소비한 틱* 같은 Step에 임펄스를 합성한다. 클라는 이 호출 *직후 첫 Predict*
        //   서브스텝부터 _impulseVx를 ExternalVelX로 주입 → "임펄스 시작 = 그 입력을 담아 보내는 C_MoveIntent
        //   서브스텝(= _localTickCounter 증가 틱)". 그 _localTickCounter가 곧 reconcile ack 기준이므로,
        //   서버가 같은 입력 틱에 임펄스를 시작하면 클라/서버 임펄스 위상이 *같은 틱 번호*에 정렬된다.
        //   호출 박자를 latch로 흡수(LocalPlayerMovement._pendingImpulse) → Update 컴포넌트 실행 순서 의존 제거.
        // startVx: 부호 포함 초기 vx (DashSpeed×facing 등). decayPerTick: 1.0(대쉬)/0.75(lunge).
        // durationTicks: 임펄스 지속 (DashTravelTicks / AttackCommitWindowTicks) — 서버 Exit 틱 거울.
        public void StartImpulse(float startVx, float decayPerTick, int durationTicks)
        {
            _impulseVx = startVx;
            _impulseDecay = decayPerTick;
            _impulseTicksRemaining = durationTicks;
        }

        void ClearImpulse()
        {
            _impulseVx = 0f;
            _impulseDecay = 0f;
            _impulseTicksRemaining = 0;
        }

        // LocalPlayerMovement가 C_MoveIntent 송신 직후 호출. jumpPressed 동봉 — replay 시 점프 시도 재현.
        // externalVelX = 그 서브스텝 Predict가 실제 쓴 임펄스 vx (LastAppliedImpulseVx) — 재계산 금지.
        public void NotifySent(uint clientTick, sbyte inputX, bool jumpPressed, float externalVelX)
        {
            _history.Push(clientTick, inputX, jumpPressed, externalVelX);
        }

        // 임펄스 없는 평지 입력용 3-arg 오버로드 — externalVelX=0 위임. 기존 reconcile 의미론 테스트 불변.
        public void NotifySent(uint clientTick, sbyte inputX, bool jumpPressed)
        {
            _history.Push(clientTick, inputX, jumpPressed, 0f);
        }

        // 50ms 고정 서브스텝 호출 — dt = Constants.TickDuration 고정.
        // 가변 dt 진입 원천 차단 ("illegal state unrepresentable"). 헌법 #1: 서버와 동일 dt → drift 0.
        //
        // **임펄스 전진** (서버 AttackState.Tick 거울):
        //   (a) 이번 틱 ExternalVelX = _impulseVx 를 Physics.Step 4-arg에 주입 (live 예측).
        //   (b) Step 후 _impulseVx = DecayImpulse(_impulseVx, _impulseDecay) + ticksRemaining-- ;
        //       0 도달 시 임펄스 정리(서버 Exit 거울). DecayImpulse는 P4 공유 공식 단일 출처.
        //   이번 틱 적용한 vx를 LastAppliedImpulseVx에 보관 → NotifySent가 저장 (저장=적용값).
        public void Predict(sbyte inputX, bool jumpPressed)
        {
            float appliedImpulseVx = _impulseVx;
            LastAppliedImpulseVx = appliedImpulseVx;

            PhysicsState after = SharedPhysics.Step(ToPhysicsState(),
                new PhysicsInput(inputX, jumpPressed, Constants.TickDuration, appliedImpulseVx), _terrain, _move);
            ApplyPhysicsState(after);

            if (_impulseTicksRemaining > 0)
            {
                _impulseVx = SharedPhysics.DecayImpulse(_impulseVx, _impulseDecay);
                _impulseTicksRemaining--;
                if (_impulseTicksRemaining <= 0)
                    ClearImpulse();
            }
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
                // 서버 권위 상태에서 출발 — 위치 + 속도 + ground (위치로 추정)
                Position = new Vector2(serverX, serverY);
                Velocity = new Vector2(serverVx, serverVy);
                OnGround = IsGroundedAt(serverX, serverY, serverVy);

                // 미-ack 입력 재시뮬 (서버 권위 → 클라 현재까지).
                // terrain 오버로드 동일 — replay가 평지로 돌면 서버-클라 대칭이 깨져 reconcile 자체가 어긋남.
                // 피격 중 입력은 source-gating으로 0이라 replay는 물리(중력)만 적용.
                //
                // **임펄스 replay** (M4.13 P5a): rec.ExternalVelX = 그 틱 live가 실제 쓴 vx를 그대로 재생
                //   (4-arg PhysicsInput). replay 중 _impulseVx를 *재전진시키지 않음* — live 전진과 replay
                //   재생은 분리. 임펄스 위상은 이미 live가 InputRecord에 박았으므로 이중 적용 금지.
                foreach (InputRecord rec in _history.ReplayFrom(ackedClientTick))
                {
                    PhysicsState after = SharedPhysics.Step(ToPhysicsState(),
                        new PhysicsInput(rec.InputX, rec.JumpPressed, Constants.TickDuration, rec.ExternalVelX),
                        _terrain, _move);
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
