#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Dawnholder.Client.Gameplay
{
    /// <summary>
    /// Portal GameObject에 붙이는 컴포넌트. 로컬 플레이어가 포탈 위에 겹친 상태에서
    /// 위 방향키 down-edge(눌리는 순간) 시 <see cref="C_EnterPortal"/> 의도 패킷을 서버에 송신.
    ///
    /// **진입 방식 (MapleStory식)**:
    ///   - OnTriggerEnter2D 자동 진입 X — 겹침 플래그(_isOverlapping)만 set.
    ///   - Update()에서 위 방향키 down-edge + _isOverlapping 조합 시 송신.
    ///   - 홀드해도 down-edge(눌리는 순간) 1회만 발동 — 연속 발송 없음.
    ///   - E(Interact/Teleport) 누른 채 ↑ = 텔레포트 의도 → 포탈 진입 skip.
    ///
    /// **헌법 #1 (Server Authority)**:
    ///   클라이언트는 "이동하겠다"는 *의도*만 보냄. 실제 맵 이동 판정(근접 검증, 포탈 유효성)과
    ///   S_MapTransition 전송은 서버 전용. 클라 스스로 scene 전환 판정 X.
    ///
    /// **Unity 콜라이더 설정 (사용자 씬 작업 필요)**:
    ///   1. Portal GameObject에 Collider2D(예: CircleCollider2D / BoxCollider2D) 부착.
    ///   2. Inspector에서 "Is Trigger" 체크박스 ON (Physics 영역 관통하게).
    ///   3. 로컬 플레이어 GameObject에도 Collider2D + Rigidbody2D 필요 (트리거 이벤트 발생 조건).
    ///   4. 본 컴포넌트의 portalId Inspector 슬롯에 서버 PortalTable ID 입력 (현재 전부 1).
    ///
    /// **씬 배치 안내**:
    ///   각 맵의 portal sprite GameObject에 본 컴포넌트를 AddComponent. portalId Inspector 설정.
    ///   Town 씬: portalId=1 (Town→HuntingGround), 등 맵별 portal마다 별도 설정.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortalTrigger : MonoBehaviour
    {
        // 서버 PortalTable의 portalId. Inspector에서 설정. 정확한 id는 서버 PortalTable 참조.
        [FormerlySerializedAs("portalId")]
        [SerializeField] int _portalId = 1;

        // 중복 송신 방지 쿨다운 (초). down-edge 발동 후 재발동 차단.
        // 서버에서 근접 검증 실패 시 S_MapTransition이 안 오므로 클라 스팸 차단이 주 목적.
        [FormerlySerializedAs("cooldownSeconds")]
        [SerializeField] float _cooldownSeconds = 2.0f;

        // 위 방향키 진입 임계. 아날로그 스틱 미세 흔들림 차단용.
        // 조정 필요 시 Inspector에서 변경 가능.
        [SerializeField] float _upThreshold = 0.5f;

        float _lastSentTime = -999f;

        // 로컬 플레이어가 이 포탈 트리거 안에 있는지 여부.
        // OnTriggerEnter2D/Exit2D가 set/clear — Update()가 이 플래그로 입력 폴링 여부를 결정.
        bool _isOverlapping;

        // 이전 프레임의 위 방향키 상태 — down-edge 산출용 (현재 프레임 누름 + 직전 프레임 안 눌림).
        bool _upPrev;

        // OnTriggerEnter2D: 트리거 진입 시 1회 호출 (매 frame X).
        // 로컬 플레이어만 처리 — 즉시 송신 없이 _isOverlapping 플래그만 세팅.
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<LocalPlayerMovement>() == null) return;
            _isOverlapping = true;
            Debug.Log($"[PortalTrigger] 포탈 진입 감지 — ↑ 키로 진입 (portalId={_portalId}).");
        }

        // OnTriggerExit2D: 트리거 이탈 시 1회 호출.
        // _isOverlapping 클리어 — Update() 폴링 중단.
        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<LocalPlayerMovement>() == null) return;
            _isOverlapping = false;
            _upPrev = false; // 이탈 시 에지 리셋 — 재진입 후 홀드 상태가 즉시 발동되는 현상 방지.
            Debug.Log("[PortalTrigger] 포탈 이탈.");
        }

        void Update()
        {
            // 위 방향키 상태 읽기 — LocalPlayerInput의 텔레포트 verticalDir 읽기와 동일 소스.
            // Keyboard.current null 안전 가드: 물리 키보드 미연결 환경(빌드 서버 등) 대응.
            bool upNow = false;
            bool eHeld = false;
            if (Keyboard.current != null)
            {
                // W 또는 ↑ = "위" (LocalPlayerInput verticalDir 읽기와 동일 출처).
                upNow = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;

                // E 키 홀드 = Teleport/Interact 의도 — 텔레포트와 포탈 진입 동시 발동 방지.
                // E만 누른 채 ↑를 누르면 텔레포트 송신이 될 수 있으므로 포탈 진입을 skip.
                // (단독 ↑만 포탈 진입. 조합키 의도 존중.)
                eHeld = Keyboard.current.eKey.isPressed;
            }

            // down-edge 산출: 이번 프레임 눌림 + 직전 프레임 안 눌림.
            bool upEdge = upNow && !_upPrev;
            _upPrev = upNow;

            if (!_isOverlapping || !upEdge) return;

            // E 홀드 시 포탈 진입 skip — 텔레포트(↑+E) 조합과 충돌 방지.
            if (eHeld)
            {
                Debug.Log("[PortalTrigger] E 홀드 감지 — 텔레포트 조합으로 판단, 포탈 진입 skip.");
                return;
            }

            // 쿨다운 가드 — 빠른 재발동 차단.
            float now = Time.unscaledTime;
            if (now - _lastSentTime < _cooldownSeconds)
            {
                Debug.Log($"[PortalTrigger] 쿨다운 중 — 재송신 차단 ({_cooldownSeconds - (now - _lastSentTime):F1}초 남음).");
                return;
            }

            // 세션 가드 — 세션 없으면 송신 불가.
            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null)
            {
                Debug.LogWarning("[PortalTrigger] UnityClientSession.Instance null — 송신 불가. PersistentServices 프리팹 또는 NetworkService 누락?");
                return;
            }

            // handshake 완료 가드 — handshake 전 송신은 silent drop (서버 측 first-packet 규칙).
            if (!session.HandshakeOk)
            {
                Debug.LogWarning("[PortalTrigger] Handshake 미완료 — 송신 차단.");
                return;
            }

            // C_EnterPortal 의도 송신. 헌법 #1: 위치 판정/이동은 서버가 함.
            var pkt = new C_EnterPortal { portalId = _portalId };
            session.SendIntent(pkt.Write());

            _lastSentTime = now;
            Debug.Log($"[PortalTrigger] C_EnterPortal 송신 — portalId={_portalId}. 서버 근접 검증 대기 중.");
        }
    }
}
