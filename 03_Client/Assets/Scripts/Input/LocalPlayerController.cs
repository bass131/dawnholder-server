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
    // Phase 07 (M2): 점프 + 비트필드 + fixed cadence Predict.
    //
    // **흐름 변경 (Phase 07)**:
    //   - **매 frame**: transform = predictor.Position (그리기만, fps 의존 0).
    //   - **50ms cadence** (Constants.TickDuration 누적기): Predict + 송신 *함께 트리거*.
    //     - 옛 Phase 05~06: 매 frame Predict (Time.deltaTime 가변 dt) — fps 의존, drift.
    //     - 새 Phase 07: 50ms fixed dt → Physics.Step 결정론 정합 (정의 파일 #82, 헌법 #1).
    //     - 시각적: 5 frame 같은 위치 → 50ms 끊김 (체감 미미, M3+ 보간 검토).
    //   - **OnJump 에지 검출** (D4 (a)): "started" phase만 캡처 → 1tick true 송신 후 reset.
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
            // 매 frame: predictor 상태 그대로 그리기 (50ms cadence 사이는 정지 시각).
            Vector3 pos = new Vector3(_predictor.Position.x, _predictor.Position.y, 0f);
            transform.position = pos;

            // 50ms 누적기 — fixed cadence (서버 20 TPS와 1:1 align).
            _sendAccumulator += Time.deltaTime;
            if (_sendAccumulator < Constants.TickDuration) return;
            _sendAccumulator -= Constants.TickDuration;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            // 50ms cadence: 입력 캡처 → Predict (fixed dt, Physics.Step 단일 출처) → 송신 → reset.
            sbyte encoded = EncodeInputX(_moveInput.x);
            bool jumpEdge = _jumpEdgeThisTick;
            _jumpEdgeThisTick = false; // 1tick 사용 후 reset — 다음 cycle은 새로 캡처.

            // Phase 07: Physics.Step 위임 — 양쪽 결과 같음 (헌법 #1).
            _predictor.Predict(encoded, jumpEdge);

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
