namespace Dawnholder.Server.GameServer.Party;

/// <summary>Tick-thread membership snapshot; member order matches the party's notification order.</summary>
public sealed class PartyMembership
{
    public PartyMembership(int partyId, IEnumerable<int> memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);
        PartyId = partyId;
        MemberIds = Array.AsReadOnly(memberIds.ToArray());
    }

    public int PartyId { get; }
    public IReadOnlyList<int> MemberIds { get; }
}
