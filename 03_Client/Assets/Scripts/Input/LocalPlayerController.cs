#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // Phase 01: 오프라인 로컬 좌우 이동 (transform 직접).
    // Phase 03: Instance singleton + SetServerPosition — S_EnterMap이 spawn 좌표 적용.
    // Phase 04: transform 직접 조작 *완전 제거* (헌법 #1 강제). 입력은 C_MoveIntent로만 전송.
    // Phase 05: **prediction 도입** — PlayerPredictor가 매 frame 누적, transform이 따라감.
    //   - S_EnterMap   → SetServerPosition → predictor.SetInitialPosition
    //   - 매 frame     → predictor.Predict(input, dt) + transform = predictor.Position
    //   - S_Snapshot   → OnServerSnapshot → predictor.OnSnapshot (threshold 비교 후 snap or 무시)
    //
    // 헌법 #1: prediction은 *예상*일 뿐. 서버 snapshot이 도착하면 항상 서버가 정답 — snap.
    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayerController : MonoBehaviour
    {
        public static LocalPlayerController? Instance { get; private set; }

        readonly PlayerPredictor _predictor = new PlayerPredictor();

        Vector2 _moveInput;
        uint _localTickCounter; // Phase 06 replay reconcile 대비 (지금은 임의 누적)

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        void Update()
        {
            sbyte encoded = EncodeInputX(_moveInput.x);
            _localTickCounter++;

            // Phase 05: prediction 즉시 적용 → 입력→화면 lag 0 (반응성).
            // Time.deltaTime은 가변, 서버는 50ms 고정 → 미세 drift 필연 (snap이 가끔 발생, 의도).
            _predictor.Predict(encoded, Time.deltaTime);
            Vector2 predicted = _predictor.Position;
            transform.position = new Vector3(predicted.x, predicted.y, 0f);

            // C_MoveIntent 송신 (Phase 04 그대로 — 매 frame).
            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            C_MoveIntent pkt = new C_MoveIntent
            {
                inputX = encoded,
                clientTick = (int)_localTickCounter
            };
            // Phase 05: SendIntent 경유 — Editor에서 SimulatedLatencyMs 적용 가능.
            session.SendIntent(pkt.Write());
        }

        // Phase 03 S_EnterMap → 서버가 정한 spawn 좌표 적용.
        // Phase 05: transform 직접 갱신 대신 predictor 초기화 — 다음 Update에서 transform이 자동 동기.
        // 단 spawn 첫 frame 깜빡임 방지를 위해 즉시 transform도 한 번 설정.
        public void SetServerPosition(Vector3 worldPos)
        {
            _predictor.SetInitialPosition(new Vector2(worldPos.x, worldPos.y));
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        // Phase 05: S_Snapshot → predictor의 reconcile 판단에 위임.
        // threshold 안이면 prediction 신뢰(아무 일도 안 일어남), 밖이면 강제 덮어쓰기 + snap 로그.
        public void OnServerSnapshot(float serverX, float serverY, int serverTick)
        {
            float prevX = _predictor.Position.x;
            bool snapped = _predictor.OnSnapshot(serverX, serverY);
            if (snapped)
            {
                float dx = serverX - prevX;
                Debug.Log($"[Snap] dx={dx:F2} at serverTick={serverTick} (count={_predictor.SnapCount})");
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
