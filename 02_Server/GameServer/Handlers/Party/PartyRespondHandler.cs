using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_PartyRespond 핸들러: decode inviterEntityId+accept + auth 게이트 + session 캡슐 메서드 호출만.
//   파티 결성/통보는 session.SubmitPartyRespond → PartyRegistry actor 안에서.
//
// **헌법 #3 (Trust Boundary)**:
//   - 응답자(행위자)는 패킷에 없음 — session._entityId에서 강제(SubmitPartyRespond 안). 위장 차단.
//   - inviterEntityId(패킷)는 untrusted "주장" — 서버는 보류 초대 기록을 진실로 삼음.
//     claimedInviter 일치 검증은 A4. 이번엔 보류 초대 존재 매칭만.
//   - accept byte 정규화: SubmitPartyRespond에서 ==1만 수락으로 판정(whitelist). 그 외 = 거절.
//   - class 선택 전 파티 입력은 silent drop + cheat-flag 후보 로그.
internal sealed class PartyRespondHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!session.HasSelectedClass)
        {
            Console.WriteLine(
                "[Trust] C_PartyRespond before CharacterSelect — silent drop (cheat-flag candidate)");
            return;
        }

        C_PartyRespond pkt = new C_PartyRespond();
        pkt.Read(buffer);

        session.SubmitPartyRespond(pkt.inviterEntityId, pkt.accept);
    }
}
