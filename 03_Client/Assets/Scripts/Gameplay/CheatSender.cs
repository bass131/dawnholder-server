#nullable enable
using Dawnholder.Client.Network;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Gameplay
{
    // [시연 디버그 치트] F8 키 → C_CheatCommand(cheatType=0=퀘스트 즉시완료) 송신.
    //   F9 키 → C_CheatCommand(cheatType=1=길드 금고 +거점 비용) 송신. 거점 레벨업 확인용.
    //   로컬 플레이어 GameObject에 부착 (PartyInviteSender 패턴 동형).
    //
    // **헌법 §1 (Server Authority)**: 클라는 치트 *의도*만 보냄. 퀘스트 카운트/보스 해금/금고는 서버 권위.
    //   서버 DebugConfig.AllowCheats=true일 때만 처리(false면 무시) — 클라가 직접 상태 변경 X.
    //   처리되면 S_QuestUpdate(F8) 또는 S_GuildSnapshot(F9) 수신 → HUD·길드 창 갱신.
    //
    // 빌드 클라 포함 모든 빌드에 존재(가드 없음) — 시연 편의. 허용 여부는 서버가 결정.
    //   Release 서버(운영툴 실행본)에는 치트 처리 자체가 없다(사용자 결정 16 A).
    [DisallowMultipleComponent]
    public class CheatSender : MonoBehaviour
    {
        // ★ 키 변경 시 이 상수만 교체. F8·F9 = A/E/W/P/F/G 등 기존 입력과 충돌 없음.
        const Key CompleteQuestKey = Key.F8;
        const Key GuildTreasuryKey = Key.F9;

        // C_CheatCommand.cheatType: 0 = 퀘스트 즉시완료, 1 = 길드 금고 지급. 미래 다른 치트는 값 추가.
        const byte CheatCompleteQuest = 0;
        const byte CheatGuildTreasury = 1;

        [SerializeField] float _cooldownSeconds = 1.0f;

        float _lastSentTime = -999f;

        void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current[CompleteQuestKey].wasPressedThisFrame)
                Send(CheatCompleteQuest, "퀘스트 즉시완료");
            else if (Keyboard.current[GuildTreasuryKey].wasPressedThisFrame)
                Send(CheatGuildTreasury, "길드 금고 지급");
        }

        void Send(byte cheatType, string label)
        {
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

            var pkt = new C_CheatCommand { cheatType = cheatType };
            session.SendIntent(pkt.Write());

            _lastSentTime = now;
            Debug.Log($"[CheatSender] C_CheatCommand({label}) 송신 → 서버 AllowCheats 시 적용.");
        }
    }
}
