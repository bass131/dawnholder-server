namespace Dawnholder.Client.UI
{
    /// <summary>Availability supplied by the state owner, never inferred from displayed balances.</summary>
    public enum GuildActionAvailability
    {
        Available,
        Locked,
        Insufficient,
        MaxLevel,
    }
}
