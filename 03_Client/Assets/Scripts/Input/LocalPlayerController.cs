#nullable enable
using Dawnholder.Client.Network;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // Phase 01: 오프라인 로컬 좌우 이동 (transform 직접).
    // Phase 03: Instance singleton + SetServerPosition — S_EnterMap이 spawn 좌표 적용.
    // Phase 04: **transform 직접 조작 제거** (헌법 #1 강제). 입력은 C_MoveIntent로만 전송.
    //   - Update에서 자기 위치 갱신 X
    //   - 매 frame 입력값을 sbyte로 인코딩해 서버에 송신
    //   - 위치는 S_Snapshot 도착 시 SetServerPosition으로만 변경
    //
    // Phase 05+: prediction 도입 시 매 frame 자기 위치 예측 + 서버 snapshot과 비교(reconcile).
    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayerController : MonoBehaviour
    {
        public static LocalPlayerController? Instance { get; private set; }

        Vector2 _moveInput;
        uint _localTickCounter; // Phase 06 replay reconcile 대비 (지금은 임의 누적)

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        void Update()
        {
            // 본인 transform 직접 갱신 *금지* (Phase 04 헌법 #1 강제).
            // 매 frame 클라 intent 송신. 서버는 다음 tick에 적용 후 매 5 tick(=250ms)마다 snapshot.
            sbyte encoded = EncodeInputX(_moveInput.x);
            _localTickCounter++;

            // UnityClientSession이 아직 connect 안 됐으면 송신 skip.
            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            C_MoveIntent pkt = new C_MoveIntent
            {
                inputX = encoded,
                clientTick = (int)_localTickCounter
            };
            session.Send(pkt.Write());
        }

        // S_EnterMap (Phase 03) / S_Snapshot (Phase 04)의 좌표를 적용.
        // 헌법 #1: 위치 변경은 *오직 서버 데이터*를 통해서만.
        public void SetServerPosition(Vector3 worldPos)
        {
            transform.position = worldPos;
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
