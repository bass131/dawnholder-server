namespace Dawnholder.Server.GameServer.Maps;

public enum InstanceOwner
{
    Party,
    Solo,
}

public readonly record struct InstanceKey(InstanceOwner Owner, int Id)
{
    public static InstanceKey ForParty(int partyId) => new(InstanceOwner.Party, partyId);

    public static InstanceKey ForSolo(int entityId) => new(InstanceOwner.Solo, entityId);
}
