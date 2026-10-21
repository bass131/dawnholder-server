#nullable enable
namespace Dawnholder.Client.UI
{
    /// <summary>Immutable guild presentation supplied by the state owner, including its next price and eligibility.</summary>
    public sealed class GuildStatusDisplay
    {
        public string GuildName { get; }
        public int MemberCount { get; }
        public int OnlineCount { get; }
        public long Treasury { get; }
        public int BaseLevel { get; }
        public int MaxBaseLevel { get; }
        public bool BlacksmithUnlocked { get; }
        public long LevelUpCost { get; }
        public GuildActionAvailability LevelUp { get; }
        public bool LevelUpPending { get; }
        public GuildActionResult LevelUpResult { get; }

        public GuildStatusDisplay(string guildName, int memberCount, int onlineCount, long treasury,
            int baseLevel, int maxBaseLevel, bool blacksmithUnlocked, long levelUpCost,
            GuildActionAvailability levelUp, bool levelUpPending, GuildActionResult levelUpResult)
        {
            GuildName = guildName;
            MemberCount = memberCount;
            OnlineCount = onlineCount;
            Treasury = treasury;
            BaseLevel = baseLevel;
            MaxBaseLevel = maxBaseLevel;
            BlacksmithUnlocked = blacksmithUnlocked;
            LevelUpCost = levelUpCost;
            LevelUp = levelUp;
            LevelUpPending = levelUpPending;
            LevelUpResult = levelUpResult;
        }
    }
}
