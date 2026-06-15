#nullable enable
using Dawnholder.Client.Network;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Gameplay
{
    // [시연 디버그 치트] F8 키 → C_CheatCommand(cheatType=0=퀘스트 즉시완료) 송신.
    //   로컬 플레이어 GameObject에 부착 (PartyInviteSender 패턴 동형).
    //
    // **헌법 §1 (Server Authority)**: 클라는 치트 *의도*만 보냄. 퀘스트 카운트/보스 해금은 서버 권위.
    //   서버 DebugConfig.AllowCheats=true일 때만 처리(false면 무시) — 클라가 직접 상태 변경 X.
    //   처리되면 S_QuestUpdate 수신 → QuestState/HUD 갱신 + 보스 포탈 게이트 통과.
    //
    // 빌드 클라 포함 모든 빌드에 존재(가드 없음) — 시연 편의. 허용 여부는 서버가 결정.
    [DisallowMultipleComponent]
    public class CheatSender : MonoBehaviour
    {
        // ★ 키 변경 시 이 상수만 교체. F8 = A/E/W/P/F 등 기존 입력과 충돌 없음.
        const Key CheatKey = Key.F8;

        // C_CheatCommand.cheatType: 0 = 퀘스트 즉시완료. 미래 다른 치트는 값 추가.
        const byte CheatCompleteQuest = 0;

        [SerializeField] float _cooldownSeconds = 1.0f;

        float _lastSentTime = -999f;

        void Update()
        {
            if (Keyboard.current == null) return;

            if (!Keyboard.current[CheatKey].wasPressedThisFrame) return;

            float now = Time.unscaledTime;
            if (now - _lastSentTime < _cooldownSeconds) return;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null)
            {
                Debug.LogWarning("[CheatSender] UnityClientSession null — 송신 불가.");
                return;
            }
            if (!session.HandshakeOk)
            {
                Debug.LogWarning("[CheatSender] Handshake 미완료 — 송신 차단.");
                return;
            }

            var pkt = new C_CheatCommand { cheatType = CheatCompleteQuest };
            session.SendIntent(pkt.Write());

            _lastSentTime = now;
            Debug.Log("[CheatSender] C_CheatCommand(퀘스트 즉시완료) 송신 → 서버 AllowCheats 시 적용.");
        }
    }
}
