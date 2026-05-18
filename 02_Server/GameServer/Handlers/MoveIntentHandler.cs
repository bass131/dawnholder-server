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
        C_MoveIntent pkt = new C_MoveIntent();
        pkt.Read(buffer);

        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(pkt.input);

        session.SubmitMoveIntent(inputX, jumpPressed, valid, pkt.input, pkt.clientTick);
    }
}
