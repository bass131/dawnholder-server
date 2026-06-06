#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dawnholder.Client.Gameplay
{
    /// <summary>
    /// Portal GameObject에 붙이는 컴포넌트. 로컬 플레이어가 트리거에 닿으면
    /// <see cref="C_EnterPortal"/> 의도 패킷을 서버에 송신.
    ///
    /// **헌법 #1 (Server Authority)**:
    ///   클라이언트는 "이동하겠다"는 *의도*만 보냄. 실제 맵 이동 판정(근접 검증, 포탈 유효성)과
    ///   S_MapTransition 전송은 서버 전용. 클라 스스로 scene 전환 판정 X.
    ///
    /// **중복 송신 방지**:
    ///   OnTriggerEnter2D는 Trigger 안에 머무는 동안 매 frame 호출되지 않고 *진입 시 1회*만 호출.
    ///   단 Collider2D 이탈 후 재진입 시 재호출됨 (정상 — 재시도 허용).
    ///   추가 안전망으로 쿨다운(_cooldownSec) 두어 빠른 재진입 스팸 방지.
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

        // 중복 송신 방지 쿨다운 (초). 빠른 재진입 스팸 차단.
        // 서버에서 근접 검증 실패 시 S_MapTransition이 안 오므로 클라 스팸 차단이 주 목적.
        [FormerlySerializedAs("cooldownSeconds")]
        [SerializeField] float _cooldownSeconds = 2.0f;

        float _lastSentTime = -999f;

        // OnTriggerEnter2D: 트리거 진입 시 1회 호출 (매 frame X).
        // 로컬 플레이어만 처리 — 다른 플레이어/적의 충돌은 무시.
        void OnTriggerEnter2D(Collider2D other)
        {
            // 로컬 플레이어 확인 — LocalPlayerMovement 컴포넌트 보유 여부로 판별.
            // 타인 플레이어는 RemoteEntity 컴포넌트를 가짐 (LocalPlayerMovement X).
            if (other.GetComponent<LocalPlayerMovement>() == null) return;

            // 쿨다운 가드 — 빠른 재진입 스팸 차단.
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
