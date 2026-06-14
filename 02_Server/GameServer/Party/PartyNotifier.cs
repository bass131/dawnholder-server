using Dawnholder.Server.GameServer.Loop;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Party;

// 파티 상태 → S_Party* 패킷 빌드 + cross-map 1:1 송신만 담당하는 순수 헬퍼.
//
// **책임 분리 (§2.2)**: PartyRegistry = 순수 데이터 actor(파티 dict 조작).
//   "누구에게 무슨 패킷을" = 송신 관심사라 별도 단위로 분리. 이 클래스는 상태를 *읽기만* 하고
//   GameWorld.SendToEntity(대상 맵 EnqueueJob 경유)로 라우팅.
//
// **호출 invariant**: PartyRegistry tick thread(EnqueueJob 람다) 안에서만 호출.
//   PartyState 읽기 + GameWorld.TryGetEntityClass가 tick thread 직렬화에 의존.
//   실제 socket Send는 GameWorld.SendToEntity가 대상 맵 thread로 다시 마샬링하므로 안전(헌법 §5).
//
// **헌법 #1 (Server Authority)**: memberNClass는 서버 권위 PlayerStats에서만 — GameWorld.TryGetEntityClass.
internal static class PartyNotifier
{
    // 초대 거절을 행위자(초대자)에게 통보. reason byte는 PartyRegistry.Error* 상수와 정합.
    //   거절은 *서버 판정*(헌법 §3) — 클라가 자기 자신을 거절 처리하지 않음.
    public static void SendPartyError(GameWorld world, int targetEntityId, byte reason)
    {
        S_PartyError pkt = new S_PartyError { reason = reason };
        world.SendToEntity(targetEntityId, pkt.Write());
    }

    // 피초대자에게 초대 도착 통보. inviterClass는 서버 권위 조회(클라 신뢰 X).
    public static void SendInviteRecv(GameWorld world, int inviterEntityId, int targetEntityId)
    {
        world.TryGetEntityClass(inviterEntityId, out byte inviterClass);
        S_PartyInviteRecv pkt = new S_PartyInviteRecv
        {
            inviterEntityId = inviterEntityId,
            inviterClass = inviterClass,
        };
        world.SendToEntity(targetEntityId, pkt.Write());
    }

    // 파티 결성/갱신을 양 멤버 전원에게 통보. member0/1 슬롯은 Members 순서대로.
    public static void SendPartyUpdate(GameWorld world, PartyState party)
    {
        int member0 = party.Members.Count > 0 ? party.Members[0] : 0;
        int member1 = party.Members.Count > 1 ? party.Members[1] : 0;
        world.TryGetEntityClass(member0, out byte member0Class);
        world.TryGetEntityClass(member1, out byte member1Class);

        S_PartyUpdate pkt = new S_PartyUpdate
        {
            partyId = party.PartyId,
            leaderEntityId = party.LeaderEntityId,
            member0EntityId = member0,
            member1EntityId = member1,
            member0Class = member0Class,
            member1Class = member1Class,
        };

        foreach (int memberId in party.Members)
            world.SendToEntity(memberId, pkt.Write());
    }

    // 해산 통보: partyId=0 빈 상태로 약속(클라는 partyId==0 = 파티 없음으로 해석).
    //   해산 전 캡처한 멤버 목록에 각각 1:1 송신. 멤버 entity가 떠났으면 SendToEntity가 silent 무시.
    public static void SendDisband(GameWorld world, IReadOnlyList<int> formerMembers)
    {
        foreach (int memberId in formerMembers)
        {
            S_PartyUpdate pkt = new S_PartyUpdate
            {
                partyId = 0,
                leaderEntityId = 0,
                member0EntityId = 0,
                member1EntityId = 0,
                member0Class = 0,
                member1Class = 0,
            };
            world.SendToEntity(memberId, pkt.Write());
        }
    }
}
