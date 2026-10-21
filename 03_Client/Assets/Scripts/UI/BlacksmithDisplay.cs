using Shared.Protocol;

namespace Dawnholder.Client.UI
{
    /// <summary>Immutable personal enhancement presentation; no local pricing, spending or bonus calculation.</summary>
    public sealed class BlacksmithDisplay
    {
        public CharacterClass CharacterClass { get; }
        public int EnhanceLevel { get; }
        public int MaxEnhanceLevel { get; }
        public int AttackBonus { get; }
        public long GoldCost { get; }
        public int MaterialCost { get; }
        public GuildActionAvailability Enhance { get; }
        public bool EnhancePending { get; }
        public GuildActionResult EnhanceResult { get; }

        public BlacksmithDisplay(CharacterClass characterClass, int enhanceLevel, int maxEnhanceLevel,
            int attackBonus, long goldCost, int materialCost, GuildActionAvailability enhance,
            bool enhancePending, GuildActionResult enhanceResult)
        {
            CharacterClass = characterClass;
            EnhanceLevel = enhanceLevel;
            MaxEnhanceLevel = maxEnhanceLevel;
            AttackBonus = attackBonus;
            GoldCost = goldCost;
            MaterialCost = materialCost;
            Enhance = enhance;
            EnhancePending = enhancePending;
            EnhanceResult = enhanceResult;
        }
    }
}
