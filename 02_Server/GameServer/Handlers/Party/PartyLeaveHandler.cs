using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Party;

// C_PartyLeave 핸들러: decode(reserved) + auth 게이트 + session 캡슐 메서드 호출만.
//   파티 해산/통보는 session.SubmitPartyLeave → PartyRegistry actor 안에서.
//
// **헌법 #3 (Trust Boundary)**:
//   - 탈퇴자(행위자)는 패킷에 없음 — session._entityId에서 강제(SubmitPartyLeave 안). 위장 차단.
//     reserved byte는 의미 없음(미래 확장 자리) — 읽되 사용 X.
//   - class 선택 전 파티 입력은 silent drop + cheat-flag 후보 로그.
internal sealed class PartyLeaveHandler : IPacketHandler
{
    // 파티 입력 — class 선택 전 = 신뢰 경계 위반. dispatch 일괄 게이트가 silent drop.
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_PartyLeave pkt = new C_PartyLeave();
        pkt.Read(buffer); // reserved 필드 — 행위자는 session에서 강제하므로 페이로드는 무의미

        session.SubmitPartyLeave();
    }
}
