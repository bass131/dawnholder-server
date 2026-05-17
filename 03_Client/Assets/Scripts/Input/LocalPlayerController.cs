#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // Phase 01~04: 입력 → C_MoveIntent 송신 골격.
    // Phase 05: prediction 도입 — predictor 매 frame 누적, transform 따라감.
    // Phase 06 Step 4: 50ms 송신 throttle (서버 20 TPS와 1:1 align, framerate-bound 차단).
    // Phase 07 (M2): 점프 + 비트필드 + 매 frame Predict (Phase 06 패턴 + jumpPressed).
    //
    // **흐름 (Phase 07 사후 정정 2026-05-17 — A 채택)**:
    //   - **매 frame**: Predict (Time.deltaTime 가변) + transform 갱신.
    //     - 시뮬 자체가 부드러움 (240Hz 12 frame 모두 다른 위치).
    //     - 클라 가변 dt + 서버 fixed dt 차이는 reconcile로 흡수 (Phase 06 패턴).
    //   - **50ms cadence** (송신 throttle): C_MoveIntent 송신 + InputHistory push.
    //     - 정의 파일 #82 "fps 의존 차단" = *송신 cadence* 의미. Predict 자체는 가변 OK.
    //   - **OnJump 에지 검출** (D4 (a)): "started" phase만 캡처 → 송신 cycle까지 보관 후 reset.
    //
    // **장르 정합 — MMORPG/캐주얼 RPG (ADR-006/009)**:
    //   부드러움 > 결정론 정확도 (Source/Quake/Overwatch 패턴). fixed-step + visual lerp는
    //   격투/콘솔 RTS 패턴이라 over-engineering. 사후 정정 commit (Step 4 → Step 4-fix).
    //
    // **비트필드 인코드** (D2 b 현업 정석): InputBits.Encode 단일 출처. 양쪽 같은 헬퍼 호출.
    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayerController : MonoBehaviour
    {
        public static LocalPlayerController? Instance { get; private set; }

        readonly PlayerPredictor _predictor = new PlayerPredictor();

        Vector2 _moveInput;
        bool _jumpEdgeThisTick; // Phase 07: 송신 cycle까지 jump 에지 보관. 송신 후 reset.

        uint _localTickCounter; // 송신 일련번호 (송신 시점에만 ++). Phase 06 replay reconcile 기준점.
        float _sendAccumulator; // 50ms 송신 throttle 누적기.

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        // Input System "Move" 액션 콜백 (Phase 01~ 박힘).
        void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        // Phase 07 신설: "Jump" 액션 콜백. D4 (a) 클라 에지 — "started" phase만 캡처.
        // PlayerInput component의 Behavior=Send Messages 모드에서 이 메서드명이 자동 wire.
        // value.isPressed == true: 키 down (에지), false: 키 up (무시).
        // 송신 cycle 전에 다시 누르면 같은 에지로 합쳐짐 (정상 — cadence별 1 점프).
        void OnJump(InputValue value)
        {
            if (value.isPressed) _jumpEdgeThisTick = true;
        }

        void Update()
        {
            // Phase 07: 매 frame Predict (Phase 06 패턴 + jumpPressed). 시뮬 자체가 부드러움.
            // jumpEdge는 송신 cycle까지 *보관* (송신 시점에 한 번 더 사용) — Predict는 매 frame이라
            // OnJump 이후 50ms 안 모든 frame에 jumpEdge=true 들어가면 *재점프* 시도. 단 Physics.Step의
            // OnGround 안전망이 1tick만 적용 — 점프 후 즉시 onGround=false라 자연 차단.
            sbyte encoded = EncodeInputX(_moveInput.x);
            _predictor.Predict(encoded, _jumpEdgeThisTick, Time.deltaTime);
            transform.position = new Vector3(_predictor.Position.x, _predictor.Position.y, 0f);

            // 50ms 송신 throttle — fps 의존 차단 (240Hz도 20 packet/s, Phase 06 패턴).
            _sendAccumulator += Time.deltaTime;
            if (_sendAccumulator < Constants.TickDuration) return;
            _sendAccumulator -= Constants.TickDuration;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            // 50ms cadence: 송신 + InputHistory push + jumpEdge reset.
            bool jumpEdge = _jumpEdgeThisTick;
            _jumpEdgeThisTick = false; // 송신 후 reset — 다음 cycle은 새 OnJump 캡처.

            _localTickCounter++;

            // Phase 07: 비트필드 인코드 (InputBits.Encode 단일 출처).
            byte input = InputBits.Encode(encoded, jumpEdge);
            C_MoveIntent pkt = new C_MoveIntent
            {
                input = input,
                clientTick = _localTickCounter
            };
            // Phase 05: SendIntent 경유 — Editor에서 SimulatedLatencyMs 적용 가능.
            session.SendIntent(pkt.Write());

            // Phase 06: 송신 *직후* InputHistory push (정의 파일 #83 함정 회피).
            // Phase 07: jumpEdge 함께 박음 — replay 시 점프 시도 재현.
            _predictor.NotifySent(_localTickCounter, encoded, jumpEdge);
        }

        // Phase 03 S_EnterMap → 서버가 정한 spawn 좌표 적용.
        // Phase 05: transform 직접 갱신 대신 predictor 초기화 — 다음 Update에서 transform 자동 동기.
        // 단 spawn 첫 frame 깜빡임 방지를 위해 즉시 transform도 한 번 설정.
        public void SetServerPosition(Vector3 worldPos)
        {
            _predictor.SetInitialPosition(new Vector2(worldPos.x, worldPos.y));
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        // Phase 05~06: S_Snapshot → predictor의 reconcile 판단에 위임.
        // Phase 07: vx/vy 추가 — Y축 prediction 정합. Predictor가 X+Y 둘 다 비교.
        public void OnServerSnapshot(float serverX, float serverY,
                                     float serverVx, float serverVy,
                                     int serverTick, uint ackedClientTick)
        {
            float prevX = _predictor.Position.x;
            float prevY = _predictor.Position.y;
            bool reconciled = _predictor.OnSnapshot(
                serverX, serverY, serverVx, serverVy, ackedClientTick);
            if (reconciled)
            {
                float dx = serverX - prevX;
                float dy = serverY - prevY;
                Debug.Log(
                    $"[Reconcile] d=({dx:F2}, {dy:F2}) at serverTick={serverTick} " +
                    $"ack={ackedClientTick} (count={_predictor.SnapCount})");
            }
        }

        // Vector2(아날로그 가능) → sbyte(-1/0/1) 변환.
        // 임계값 0.5 — 게임패드 아날로그 스틱 미세 흔들림 차단.
        static sbyte EncodeInputX(float x)
        {
            if (x > 0.5f) return 1;
            if (x < -0.5f) return -1;
            return 0;
        }
    }
}
