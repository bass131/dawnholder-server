#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Gameplay
{
    // 파티 초대 송신 컴포넌트 — 로컬 플레이어 GameObject에 부착.
    //
    // **입력 키**: P 키 down-edge → 근접 최단 원격 플레이어에게 C_PartyInvite 송신.
    //   키 선택 이유: A(공격)/E(텔레포트)/W·↑(포탈/점프)/F 등과 충돌 없음.
    //   ★ 아침 튜닝 포인트: _inviteKey [SerializeField] 또는 상수 교체.
    //
    // **헌법 §1 (Server Authority)**:
    //   클라이언트는 초대 *의도*만 보냄. 파티 구성/거절/수락 판정은 서버 전용.
    //   S_PartyUpdate/S_PartyError 수신 시 PartyState.Instance가 갱신됨.
    //
    // **근접 탐색**: RemoteEntityRegistry 순회 — 이미 씬에 박힌 entity dict를 재사용.
    //   이미 파티 중인 경우 송신은 서버가 S_PartyError로 거절 (클라가 필터링 X).
    [DisallowMultipleComponent]
    public class PartyInviteSender : MonoBehaviour
    {
        // ★ 아침 튜닝: 키 변경 시 이 상수만 교체.
        // Inspector 노출이 필요하다면 [SerializeField] Key _inviteKey = Key.P; 로 전환.
        const Key InviteKey = Key.P;

        // 연속 발송 차단 쿨다운 (초). 서버 거절 응답 전 중복 발송 방지.
        [SerializeField] float _cooldownSeconds = 1.0f;

        float _lastSentTime = -999f;

        void Update()
        {
            if (Keyboard.current == null) return;

            // down-edge 만 반응 (wasPressedThisFrame = 이번 프레임에 처음 눌림).
            if (!Keyboard.current[InviteKey].wasPressedThisFrame) return;

            // 쿨다운 가드.
            float now = Time.unscaledTime;
            if (now - _lastSentTime < _cooldownSeconds) return;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null)
            {
                Debug.LogWarning("[PartyInviteSender] UnityClientSession null — 송신 불가.");
                return;
            }
            if (!session.HandshakeOk)
            {
                Debug.LogWarning("[PartyInviteSender] Handshake 미완료 — 송신 차단.");
                return;
            }

            int targetId = FindNearestRemoteEntityId();
            if (targetId < 0)
            {
                Debug.Log("[PartyInviteSender] 주변에 원격 플레이어 없음 — 초대 취소.");
                return;
            }

            var pkt = new C_PartyInvite { targetEntityId = targetId };
            session.SendIntent(pkt.Write());

            _lastSentTime = now;
            Debug.Log($"[PartyInviteSender] C_PartyInvite 송신 → targetEntityId={targetId}. 서버 응답 대기 중.");
        }

        // RemoteEntityRegistry를 순회해 로컬 플레이어 위치 기준 최단 거리 entity를 반환.
        // 없으면 -1.
        int FindNearestRemoteEntityId()
        {
            RemoteEntityRegistry? registry = RemoteEntityRegistry.Instance;
            if (registry == null) return -1;

            Vector3 origin = transform.position;
            int bestId = -1;
            float bestSqDist = float.MaxValue;

            // TryGetTransform 공개 API를 사용해 내부 dict 직접 접근 없이 순회.
            // RemoteEntityRegistry.Instance가 노출하는 공개 메서드만 사용 — 캡슐화 보존.
            //
            // ★ 설계 메모: registry가 "모든 entityId" 열거 API를 미제공하므로
            //   UnityEngine.Object.FindObjectsOfType<RemoteEntity>()로 씬 조회.
            //   registry._entities dict 직접 접근보다 Unity native 경로가 안전 (캡슐화 보존).
            RemoteEntity[] remotes = FindObjectsByType<RemoteEntity>(FindObjectsSortMode.None);
            foreach (RemoteEntity remote in remotes)
            {
                if (remote == null) continue;
                float sqDist = (remote.transform.position - origin).sqrMagnitude;
                if (sqDist < bestSqDist)
                {
                    bestSqDist = sqDist;
                    bestId = remote.EntityId;
                }
            }

            return bestId;
        }
    }
}
