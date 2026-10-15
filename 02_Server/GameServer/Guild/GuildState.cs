namespace Dawnholder.Server.GameServer.Guild;

// Only communal economy belongs here. Membership, online presence, and connection sequence
// have different lifetimes and must not advance this revision when they alone change.
internal sealed class GuildState
{
    internal GuildState(uint revision, int treasury, byte strongholdLevel)
    {
        if (treasury < 0 || treasury > GuildRules.TreasuryCap || strongholdLevel < 1 ||
            strongholdLevel > GuildRules.MaxStrongholdLevel)
        {
            throw new InvalidOperationException("Guild economy invariant violated.");
        }
        Revision = revision;
        Treasury = treasury;
        StrongholdLevel = strongholdLevel;
    }

    internal uint Revision { get; }

    internal int Treasury { get; }

    internal byte StrongholdLevel { get; }

    internal bool BlacksmithUnlocked => StrongholdLevel == GuildRules.MaxStrongholdLevel;

    internal GuildState WithJoinSeed()
    {
        int gain = Math.Min(GuildRules.JoinSeed, GuildRules.TreasuryCap - Treasury);
        if (gain == 0) return this;
        if (Revision == uint.MaxValue) throw new InvalidOperationException("Guild revision exhausted during admission.");
        return new GuildState(Revision + 1, Treasury + gain, StrongholdLevel);
    }

    internal GuildState WithStrongholdUpgrade()
        => new(checked(Revision + 1), Treasury - GuildRules.StrongholdCost, GuildRules.MaxStrongholdLevel);
}
