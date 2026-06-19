using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_MoveIntent 핸들러: decode + InputBits 디코드(헌법 #3 정합 invalid bits 정규화) →
//   session.SubmitMoveIntent(...) 호출. rate-limit / tick 마샬링은 session 안.
//
// **InputBits 단일 출처 규칙**: 양쪽 디코드 중복 금지 —
// 본 핸들러가 디코드하고 sbyte/bool로 전달, session은 검증된 값만 받음.
internal sealed class MoveIntentHandler : IPacketHandler
{
    // 이동 입력 — class 선택 전 = stats 없는 상태로 월드 영향 가능. dispatch 일괄 게이트가 silent drop.
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_MoveIntent pkt = new C_MoveIntent();
        pkt.Read(buffer);

        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(pkt.input);

        session.SubmitMoveIntent(inputX, jumpPressed, valid, pkt.input, pkt.clientTick);
    }
}
