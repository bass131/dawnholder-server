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
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // 헌법 #3 (trust boundary): class 선택 전 C_MoveIntent = silent drop.
        // 캐릭터 선택 없이 movement를 처리하면 stats 없는 상태로 월드에 영향을 줄 수 있음.
        // silent drop = disconnect보다 UX 부드러움 (reconnect storm 회피).
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
