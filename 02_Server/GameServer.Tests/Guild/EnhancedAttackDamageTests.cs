using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// I6 / G5 (technical contract v1, D6·D7): after a real enhance, the server-confirmed damage of the
// same attack on the same target grows by exactly the approved +5 for Knight melee, Dash, Mage melee
// (projectile) and Thunderbolt. An unenhanced player's damage does not change, a projectile or
// lightning created before the commit keeps the damage fixed at creation, and a map transfer or a
// death/revive keeps the bonus. Damage is read from the S_HitResult the server sends; each check is a
// difference between two observations of the same attack on the same kind of target, so no expected
// damage is computed with Formulas or PlayerStats.
[Collection("GameWorldRegistryTests")]
public sealed class EnhancedAttackDamageTests : IDisposable
{
    const int Bonus = 5;
    const int DummyHp = 100_000;

    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    [Fact]
    public void KnightMelee_AfterEnhance_IsFiveMore_AndAnUnenhancedKnightIsUnchanged()
    {
        (GuildClient knight, GuildClient[] others) = EnhanceReadyMember(CharacterClass.Knight);
        GuildClient unenhanced = others[2];
        int before = Melee(knight);
        int otherBefore = Melee(unenhanced);

        _guild.EnhanceSucceeds(knight);
        int after = Melee(knight);
        int otherAfter = Melee(unenhanced);

        Assert.Equal(Bonus, after - before);
        Assert.Equal(otherBefore, otherAfter);
    }

    [Fact]
    public void KnightDash_AfterEnhance_IsFiveMore()
    {
        (GuildClient knight, _) = EnhanceReadyMember(CharacterClass.Knight);
        int before = Dash(knight);

        _guild.EnhanceSucceeds(knight);
        int after = Dash(knight);

        Assert.Equal(Bonus, after - before);
    }

    [Fact]
    public void MageMeleeProjectile_AfterEnhance_IsFiveMore()
    {
        (GuildClient mage, _) = EnhanceReadyMember(CharacterClass.Mage);
        int before = Melee(mage);

        _guild.EnhanceSucceeds(mage);
        int after = Melee(mage);

        Assert.Equal(Bonus, after - before);
    }

    [Fact]
    public void MageThunderbolt_AfterEnhance_IsFiveMore()
    {
        (GuildClient mage, _) = EnhanceReadyMember(CharacterClass.Mage);
        int before = Thunderbolt(mage);

        _guild.EnhanceSucceeds(mage);
        int after = Thunderbolt(mage);

        Assert.Equal(Bonus, after - before);
    }

    // D3/D6: the attack and the enhance arrive before the same tick. The map step creates the deferred
    // impact with the old damage, then the guild queue commits after Inventory in that tick; the impact
    // that lands later must keep its creation damage, and only the next attack gets +5.
    [Theory]
    [InlineData("melee-projectile")]
    [InlineData("thunderbolt")]
    public void DeferredDamageCreatedBeforeTheCommit_KeepsItsCreationDamage(string attack)
    {
        (GuildClient mage, _) = EnhanceReadyMember(CharacterClass.Mage);
        int before = attack == "thunderbolt" ? Thunderbolt(mage) : Melee(mage);
        uint inventoryRevision = _guild.QueryInventory(mage).Revision;
        EnemyEntity dummy = SpawnDummy(mage, offsetX: 2f);
        int mark = mage.Mark;

        mage.OnRecvPacket(attack == "thunderbolt" ? ThunderboltBytes() : MeleeBytes(dummy));
        mage.OnRecvPacket(GuildWire.WeaponEnhance(inventoryRevision));
        _guild.Tick();
        Assert.DoesNotContain(Received<S_HitResult>(mage, mark), hit => hit.targetEntityId == dummy.EntityId);
        _guild.Settle(GuildTestWorld.DeferredSettleTicks);

        Assert.Equal(GuildCode.Success, GuildTestWorld.SingleResult(mage.Since(mark, GuildWire.GuildActionResultId)).Result);
        Assert.Equal(before, DamageTo(mage, mark, dummy));
        _guild.Settle(attack == "thunderbolt" ? Constants.ThunderboltCooldownTicks : CombatConstants.MeleeCooldownTicks);
        Assert.Equal(before + Bonus, attack == "thunderbolt" ? Thunderbolt(mage) : Melee(mage));
    }

    // D7: the personal state survives the gap of a portal move, so the new map reads the same bonus.
    [Fact]
    public void EnhancedKnight_KeepsTheBonus_AfterAMapTransfer()
    {
        (GuildClient knight, _) = EnhanceReadyMember(CharacterClass.Knight);
        int before = Melee(knight);
        _guild.EnhanceSucceeds(knight);

        GameMap hunting = _guild.MoveToHuntingGround(knight);
        _guild.Settle(CombatConstants.MeleeCooldownTicks);

        Assert.Same(hunting, _guild.MapOf(knight));
        Assert.Equal(before + Bonus, Melee(knight));
    }

    // Fixture trigger: the real death handler runs as a map job, as HandlePlayerDeathTests does; a lethal
    // enemy hit calls the same handler. The bonus must survive the revive.
    [Fact]
    public void EnhancedKnight_KeepsTheBonus_AfterDeathAndRevive()
    {
        (GuildClient knight, _) = EnhanceReadyMember(CharacterClass.Knight);
        int before = Melee(knight);
        _guild.EnhanceSucceeds(knight);
        GameMap map = _guild.MapOf(knight);
        PlayerEntity player = _guild.PlayerOf(knight);

        map.EnqueueJob(() =>
        {
            player.Hp = 0;
            map.HandlePlayerDeath(player);
        });
        _guild.Settle();
        Assert.False(player.IsDead, "fixture: the real death handler revives the player");
        _guild.Settle(CombatConstants.MeleeCooldownTicks);

        Assert.Equal(before + Bonus, Melee(knight));
    }

    // Six members give treasury 30 (seeds); another member buys the stronghold; the attacker gets the
    // real boss reward (currency 50 + material 1) so a later EnhanceSucceeds can pay 20 + 1.
    (GuildClient Attacker, GuildClient[] Others) EnhanceReadyMember(CharacterClass characterClass)
    {
        GuildClient attacker = _guild.JoinAndEnter(characterClass);
        GuildClient[] others = _guild.JoinMembers(5);
        _guild.UpgradeStronghold(others[0]);
        _guild.GainBossReward(attacker);
        return (attacker, others);
    }

    int Melee(GuildClient attacker)
    {
        EnemyEntity dummy = SpawnDummy(attacker, offsetX: 1f);
        int mark = attacker.Mark;
        attacker.OnRecvPacket(MeleeBytes(dummy));
        _guild.Settle(GuildTestWorld.DeferredSettleTicks);
        int damage = DamageTo(attacker, mark, dummy);
        _guild.Settle(CombatConstants.MeleeCooldownTicks);
        return damage;
    }

    // The Dash box spans the knight's next 5 units in its facing direction.
    int Dash(GuildClient knight)
    {
        EnemyEntity dummy = SpawnDummy(knight, offsetX: 1.5f);
        int mark = knight.Mark;
        knight.OnRecvPacket(new C_SkillUse
        {
            skillId = (byte)SkillId.Dash,
            attackerClientTick = (int)(_guild.CurrentTick + 1),
            facing = 1,
        }.Write());
        _guild.Settle();
        int damage = DamageTo(knight, mark, dummy);
        _guild.Settle(Constants.DashCooldownTicks + Constants.DashTravelTicks);
        return damage;
    }

    int Thunderbolt(GuildClient mage)
    {
        EnemyEntity dummy = SpawnDummy(mage, offsetX: 2f);
        int mark = mage.Mark;
        mage.OnRecvPacket(ThunderboltBytes());
        _guild.Settle(GuildTestWorld.DeferredSettleTicks);
        int damage = DamageTo(mage, mark, dummy);
        _guild.Settle(Constants.ThunderboltCooldownTicks);
        return damage;
    }

    byte[] MeleeBytes(EnemyEntity target)
        => new C_Attack { targetEntityId = target.EntityId, attackerClientTick = (int)(_guild.CurrentTick + 1) }.Write().ToArray();

    byte[] ThunderboltBytes()
        => new C_SkillUse
        {
            skillId = (byte)SkillId.Thunderbolt,
            attackerClientTick = (int)(_guild.CurrentTick + 1),
            facing = 1,
        }.Write().ToArray();

    // The same target for every sample: Normal kind and Defense, never moving or attacking back, with
    // HP far above any hit. A fresh dummy per sample keeps knockback and hit state out of the comparison.
    EnemyEntity SpawnDummy(GuildClient attacker, float offsetX)
    {
        EnemyStats stats = EnemyStats.NormalDefault();
        stats.Attack = 0;
        stats.MoveSpeed = 0f;
        stats.AggroRange = 0f;
        stats.PatrolRange = 0f;
        stats.AggroOnSight = false;
        PlayerEntity player = _guild.PlayerOf(attacker);
        return _guild.MapOf(attacker).SpawnEnemy(EnemyKind.Normal, player.Position.X + offsetX, player.Position.Y, DummyHp, stats);
    }

    static int DamageTo(GuildClient attacker, int mark, EnemyEntity dummy)
        => Assert.Single(
            Received<S_HitResult>(attacker, mark),
            hit => hit.attackerEntityId == attacker.EntityId && hit.targetEntityId == dummy.EntityId).damage;

    // Existing packets are read with their existing generated readers.
    static List<T> Received<T>(GuildClient client, int mark) where T : IPacket, new()
    {
        ushort id = new T().Protocol;
        List<T> packets = new();
        foreach (byte[] bytes in client.Packets.Skip(mark).Where(bytes => GuildWire.IdOf(bytes) == id))
        {
            T packet = new();
            packet.Read(new ArraySegment<byte>(bytes));
            packets.Add(packet);
        }
        return packets;
    }
}
