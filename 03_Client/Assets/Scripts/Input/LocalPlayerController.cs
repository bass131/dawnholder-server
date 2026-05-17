#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.GameData;
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
    // Phase 06 Step 4: **framerate-bound 송신 차단** — 송신만 50ms 간격 (서버 20 TPS와 1:1 align).
    //   - prediction은 그대로 매 frame (반응성 유지)
    //   - C_MoveIntent 송신만 throttle: 240Hz 머신 240 packet/s → 20 packet/s
    //   - _localTickCounter는 *송신 시점*에만 ++ (frame 번호 X, 송신 일련번호 O)
    //   - 송신 직후 predictor.NotifySent로 InputHistory에 push (Step 5 reconcile 재료)
    //
    // 헌법 #1: prediction은 *예상*일 뿐. 서버 snapshot이 도착하면 항상 서버가 정답 — snap.
    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayerController : MonoBehaviour
    {
        public static LocalPlayerController? Instance { get; private set; }

        readonly PlayerPredictor _predictor = new PlayerPredictor();

        Vector2 _moveInput;
        uint _localTickCounter; // 송신 일련번호 (송신 시점에만 ++). Phase 06 replay reconcile의 기준점.
        float _sendAccumulator; // 50ms 송신 throttle 누적기.

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        void Update()
        {
            sbyte encoded = EncodeInputX(_moveInput.x);

            // Phase 05: prediction 즉시 적용 → 입력→화면 lag 0 (반응성).
            // Time.deltaTime은 가변, 서버는 50ms 고정 → 미세 drift 필연 (snap이 가끔 발생, 의도).
            _predictor.Predict(encoded, Time.deltaTime);
            Vector2 predicted = _predictor.Position;
            transform.position = new Vector3(predicted.x, predicted.y, 0f);

            // Phase 06 Step 4: 송신 throttle — 50ms 간격으로 *현재 inputX* 송신.
            // 서버 GameMap.Tick(20 TPS)과 1:1 align → 환경 독립(60/144/240Hz 머신 동일 cadence).
            _sendAccumulator += Time.deltaTime;
            if (_sendAccumulator < Constants.TickDuration) return;
            _sendAccumulator -= Constants.TickDuration;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            _localTickCounter++;
            C_MoveIntent pkt = new C_MoveIntent
            {
                inputX = encoded,
                // TEMP-yuhyeon-20260517: PDL의 clientTick이 int인데 _localTickCounter는 uint —
                // tick counter uint 통일 결정(learning-journal/youngho/int-vs-uint-for-tick-counters.md)
                // 후 팀장이 PDL 재생성 빼먹은 채 main push → 빌드 깨짐. PDL이 uint로 재생성되면 캐스트 제거.
                clientTick = (int)_localTickCounter
            };
            // Phase 05: SendIntent 경유 — Editor에서 SimulatedLatencyMs 적용 가능.
            session.SendIntent(pkt.Write());

            // Phase 06: 송신 *직후* InputHistory에 push (정의 파일 #83 함정 회피).
            _predictor.NotifySent(_localTickCounter, encoded);
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
        // Phase 06 Step 5: ackedClientTick 추가 → predictor가 input replay로 부드러운 정정.
        //   threshold 안: 무시 (정리만). 밖: 서버 위치 + 미-ack 입력 replay (텔레포트 X).
        public void OnServerSnapshot(float serverX, float serverY, int serverTick, uint ackedClientTick)
        {
            float prevX = _predictor.Position.x;
            bool reconciled = _predictor.OnSnapshot(serverX, serverY, ackedClientTick);
            if (reconciled)
            {
                float dx = serverX - prevX;
                Debug.Log($"[Reconcile] dx={dx:F2} at serverTick={serverTick} ack={ackedClientTick} (count={_predictor.SnapCount})");
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
