using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Combat;

// One read boundary for melee (including Mage projectiles), Dash, and Thunderbolt. Inventory
// validates the current connection owner. The resulting damage is captured by deferred hits,
// so an enhancement committed later cannot retroactively change a projectile already in flight.
internal static class PlayerDamage
{
    internal static int Compute(PlayerEntity attacker, EnemyStats target,
        Func<GameSession, int, int>? readAttackBonus)
    {
        int bonus = attacker.Owner == null ? 0 : readAttackBonus?.Invoke(attacker.Owner, attacker.EntityId) ?? 0;
        // Add before the existing minimum-damage clamp; adding after it would change the formula
        // against high defense. Basic PlayerStats and enemy-to-player damage remain unchanged.
        return Formulas.ComputeDamage(attacker.Stats, target, CombatConstants.BaseDamage + bonus);
    }
}
