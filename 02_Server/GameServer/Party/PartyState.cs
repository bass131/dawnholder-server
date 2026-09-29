namespace Dawnholder.Server.GameServer.Party;

// 파티 1개의 데이터. 변경은 PartyRegistry tick thread에서만 (actor 불변식).
// 멤버 식별 = entityId만 — session 참조 X (disconnect race 회피, ADR-026).
public sealed class PartyState
{
    public PartyState(int partyId, int leaderEntityId)
    {
        PartyId = partyId;
        LeaderEntityId = leaderEntityId;
    }

    public int PartyId { get; }
    public int LeaderEntityId { get; }

    // 정원 2 고정. PDL member0/member1 슬롯과 정합.
    public List<int> Members { get; } = new();
}
