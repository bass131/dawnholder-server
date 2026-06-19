using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Party;

// C_PartyInvite 핸들러: decode targetEntityId + auth 게이트 + session 캡슐 메서드 호출만.
//   pending invite 기록 / 피초대자 통보는 session.SubmitPartyInvite → PartyRegistry actor 안에서.
//
// **헌법 #3 (Trust Boundary)**:
//   - 초대자(행위자)는 패킷에 없음 — session._entityId에서 강제(SubmitPartyInvite 안). 위장 차단.
//   - class 선택(=월드 진입) 전 파티 입력은 신뢰 경계 위반 → silent drop + cheat-flag 후보 로그.
//   - 거절 4종(자기초대/이미파티/정원/만료)은 A4(Phase 05). 이번엔 happy + auth 게이트.
internal sealed class PartyInviteHandler : IPacketHandler
{
    // 파티 입력 — class 선택(=월드 진입) 전 = 신뢰 경계 위반. dispatch 일괄 게이트가 silent drop.
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_PartyInvite pkt = new C_PartyInvite();
        pkt.Read(buffer);

        // targetEntityId는 untrusted — 존재/자기초대/이미파티 검증은 A4. 이번엔 그대로 전달.
        session.SubmitPartyInvite(pkt.targetEntityId);
    }
}
