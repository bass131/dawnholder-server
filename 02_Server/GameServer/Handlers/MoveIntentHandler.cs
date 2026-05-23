using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M3 Phase 03 (헌법 #4 봉합): GameSession에서 추출된 move intent 핸들러.
//
// **이전 위치**: GameSession.HandleMoveIntent (inline).
// **변경**: decode + InputBits 디코드(헌법 #3 정합 invalid bits 정규화) →
//   session.SubmitMoveIntent(...) 호출. rate-limit / tick 마샬링은 session 안.
//
// **InputBits 단일 출처 규칙** (Codex 함정 #2): 양쪽 디코드 중복 금지 —
// 본 핸들러가 디코드하고 sbyte/bool로 전달, session은 검증된 값만 받음.
internal sealed class MoveIntentHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // M4.1 Phase 02 (P0-2 봉합 — 헌법 #3 trust boundary 강화):
        // class 선택 전 C_MoveIntent 수신 시 silent drop + [Trust] 경고 로그.
        // 이유: 캐릭터 선택 없이 movement를 처리하면 stats 없는 상태로 월드에 영향을 줄 수 있음.
        // silent drop 선택 = disconnect보다 UX 부드러움 (reconnect storm 회피, M4.2에서 cheat-flag 박을 예정).
        if (!session.HasSelectedClass)
        {
            Console.WriteLine(
                $"[Trust] C_MoveIntent before CharacterSelect — silent drop (cheat-flag candidate)");
            return;
        }

        C_MoveIntent pkt = new C_MoveIntent();
        pkt.Read(buffer);

        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(pkt.input);

        session.SubmitMoveIntent(inputX, jumpPressed, valid, pkt.input, pkt.clientTick);
    }
}
