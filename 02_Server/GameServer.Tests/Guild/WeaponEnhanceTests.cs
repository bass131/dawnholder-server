using System.Collections;
using System.Reflection;
using Dawnholder.Server.GameServer.Items;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// I5 / G4 and the personal part of I8 (technical contract v1): with the blacksmith unlocked, a living
// member in Town spends personal currency 20 + Material 1 once to raise the weapon 0 → 1 and the
// attack bonus 0 → 5, against the expected inventory revision. Refusals follow D4 and consume nothing;
// replies follow D8 (success 47 → 36 → 44, stale 47 → current 36 → 44, other refusals 47 → current 44).
// Preparation (GuildTestWorld): treasury from 6 seeds, the real 45 upgrade, and personal funds from
// real kills. Expected numbers are approved literals (20, 1, +5) applied to the kill-table literals
// (boss 50 + material 1 + pouch 1), never recomputed by product code.
[Collection("GameWorldRegistryTests")]
public sealed class WeaponEnhanceTests : IDisposable
{
    const int BossCurrency = 50;
    const int EnhanceCurrency = 20;

    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    [Fact]
    public void EnhanceOnce_SpendsTwentyAndOneMaterial_AndRaisesLevelAndBonusTogether()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.GainBossReward(smith);
        InventorySnapshot inventory = _guild.QueryInventory(smith);
        GuildSnapshot before = _guild.Query(smith);
        Assert.Equal(inventory.Revision, before.InventoryRevision);
        Assert.Equal(GuildCode.Success, before.WeaponAvailability);

        List<byte[]> replies = _guild.Act(smith, GuildWire.WeaponEnhance(inventory.Revision));

        Assert.Equal(
            new[] { GuildWire.GuildActionResultId, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId },
            GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        InventorySnapshot spent = GuildWire.ReadInventorySnapshot(replies[1]);
        GuildSnapshot after = GuildTestWorld.ReadSnapshot(replies[2]);
        Assert.Equal(GuildAction.WeaponEnhance, result.Action);
        Assert.Equal(GuildCode.Success, result.Result);
        Assert.Equal(inventory.Revision, result.RequestedRevision);
        Assert.True(result.InventoryRevision > inventory.Revision, "a committed enhance advances the inventory revision");
        Assert.Equal(before.GuildRevision, result.GuildRevision);
        Assert.Equal(after.Sequence, result.Sequence);

        Assert.Equal(result.InventoryRevision, spent.Revision);
        Assert.Equal(BossCurrency - EnhanceCurrency, spent.Currency);
        Assert.Equal(0, spent.CountOf(GuildWire.MaterialItemId));
        Assert.Equal(1, spent.CountOf(GuildWire.CoinPouchItemId));

        Assert.Equal(ChangeKind.WeaponEnhance, after.ChangeKind);
        Assert.Equal(result.InventoryRevision, after.InventoryRevision);
        Assert.Equal(1, after.WeaponLevel);
        Assert.Equal(5, after.AttackBonus);
        Assert.Equal(GuildCode.MaxLevel, after.WeaponAvailability);
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(before.Treasury, after.Treasury);
    }

    [Fact]
    public void NotEnoughCurrency_IsRefused_WithoutConsumingTheMaterial()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.KillOneHit(smith, EnemyKind.Normal); // currency 10 + material 1

        GuildActionResult result = AssertRefused(smith);

        Assert.Equal(GuildCode.InsufficientCurrency, result.Result);
    }

    [Fact]
    public void NoMaterial_IsRefused_WithoutConsumingCurrency()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.KillOneHit(smith, EnemyKind.Golem); // currency 10 + pouch 1
        _guild.KillOneHit(smith, EnemyKind.Golem);

        GuildActionResult result = AssertRefused(smith);

        Assert.Equal(GuildCode.InsufficientMaterial, result.Result);
    }

    [Fact]
    public void LockedBlacksmith_IsFacilityLocked_EvenWithEnoughFunds()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient smith = members[0];
        _guild.GainBossReward(smith);
        GuildSnapshot shown = _guild.Query(smith);
        // Contract: level 0 shows price 20 · material 1 × 1 even while locked, and FacilityLocked.
        Assert.Equal(GuildCode.FacilityLocked, shown.WeaponAvailability);

        GuildActionResult result = AssertRefused(smith);

        Assert.Equal(GuildCode.FacilityLocked, result.Result);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(uint.MaxValue)]
    public void StaleInventoryRevision_IsRefusedWithTheCurrentInventoryThenSnapshot(uint offset)
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.GainBossReward(smith);
        InventorySnapshot inventory = _guild.QueryInventory(smith);
        GuildSnapshot before = _guild.Query(smith);
        uint stale = unchecked(inventory.Revision + offset);

        List<byte[]> replies = _guild.Act(smith, GuildWire.WeaponEnhance(stale));

        Assert.Equal(
            new[] { GuildWire.GuildActionResultId, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId },
            GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        Assert.Equal(GuildCode.Stale, result.Result);
        Assert.Equal(stale, result.RequestedRevision);
        Assert.Equal(inventory.Revision, result.InventoryRevision);
        Assert.Equal(GuildTestWorld.ReadSnapshot(replies[2]).Sequence, result.Sequence);
        AssertInventoryUnchanged(inventory, GuildWire.ReadInventorySnapshot(replies[1]));
        AssertWeaponUnchanged(before, GuildTestWorld.ReadSnapshot(replies[2]));
        AssertInventoryUnchanged(inventory, _guild.QueryInventory(smith));
    }

    // The connection's single guild pending slot drops the in-flight duplicate without a reply; the same
    // bytes after completion carry an old revision and are Stale (D4 checks the revision first).
    [Fact]
    public void DuplicateWhileInFlight_IsDropped_AndReplayAfterCompletionIsStale()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.GainBossReward(smith);
        InventorySnapshot inventory = _guild.QueryInventory(smith);
        byte[] enhance = GuildWire.WeaponEnhance(inventory.Revision);
        int mark = smith.Mark;

        smith.OnRecvPacket(enhance.ToArray());
        smith.OnRecvPacket(enhance.ToArray());
        _guild.Settle();

        GuildActionResult only = GuildTestWorld.SingleResult(smith.Since(mark, GuildWire.GuildActionResultId));
        Assert.Equal(GuildCode.Success, only.Result);
        InventorySnapshot spent = _guild.QueryInventory(smith);
        Assert.Equal(BossCurrency - EnhanceCurrency, spent.Currency);

        GuildActionResult replay = GuildTestWorld.SingleResult(_guild.Act(smith, enhance.ToArray()));

        Assert.Equal(GuildCode.Stale, replay.Result);
        AssertInventoryUnchanged(spent, _guild.QueryInventory(smith));
        Assert.Equal(1, _guild.Query(smith).WeaponLevel);
    }

    // D2: the enhancement lives with the personal economy state, so ordinary Grant and Use keep it.
    [Fact]
    public void GrantAndUseAfterEnhance_KeepTheEnhancement()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.GainBossReward(smith);
        _guild.EnhanceSucceeds(smith);

        _guild.KillOneHit(smith, EnemyKind.Normal);
        InventorySnapshot afterGrant = _guild.QueryInventory(smith);
        byte useResult = UseCoinPouch(smith, afterGrant.Revision);
        InventorySnapshot afterUse = _guild.QueryInventory(smith);
        GuildSnapshot snapshot = _guild.Query(smith);

        Assert.Equal(0, useResult); // existing 38 Success
        Assert.True(afterUse.Revision > afterGrant.Revision, "fixture: the real pouch use must commit");
        Assert.Equal(1, snapshot.WeaponLevel);
        Assert.Equal(5, snapshot.AttackBonus);
        Assert.Equal(afterUse.Revision, snapshot.InventoryRevision);
    }

    // Personal state lives for the connection; the guild lives for the server run.
    [Fact]
    public void Reconnect_StartsAtWeaponZero_ButKeepsTheGuild()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.GainBossReward(smith);
        _guild.EnhanceSucceeds(smith);
        GuildSnapshot guildBefore = _guild.Query(smith);

        smith.Close();
        _guild.Settle();
        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, smith.AccountName);
        GuildSnapshot after = _guild.Query(back);

        Assert.Equal(0, after.WeaponLevel);
        Assert.Equal(0, after.AttackBonus);
        Assert.Equal(0u, after.InventoryRevision);
        Assert.Equal(2, after.StrongholdLevel);
        Assert.Equal(1, after.BlacksmithUnlocked);
        Assert.Equal(guildBefore.Treasury, after.Treasury);
        Assert.Equal(guildBefore.GuildRevision, after.GuildRevision);
        Assert.Equal(guildBefore.MemberCount, after.MemberCount);
    }

    // Fixture limit (I8, personal revision): uint.MaxValue needs about four billion real changes, so the
    // connection's registry entry is replaced by reflection with a state built from the existing
    // InventoryState constructor (as InventoryAuthorityBoundaryTests builds limit states). Only the 46
    // request and its replies are the real path. The guild-side counterpart is out of this contract.
    [Fact]
    public void InventoryRevisionAtUintMax_IsRevisionExhausted_WithoutAnyChange()
    {
        GuildClient smith = UnlockedBlacksmithMember();
        _guild.QueryInventory(smith);
        ReplaceInventoryState(smith, new InventoryState(uint.MaxValue, BossCurrency, new[]
        {
            new InventorySlot(ItemId.Material, 1),
            new InventorySlot(ItemId.CoinPouch, 1),
        }));
        InventorySnapshot inventory = _guild.QueryInventory(smith);
        Assert.Equal(uint.MaxValue, inventory.Revision);

        GuildActionResult result = AssertRefused(smith);

        Assert.Equal(GuildCode.RevisionExhausted, result.Result);
    }

    GuildClient UnlockedBlacksmithMember()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        _guild.UpgradeStronghold(members[1]);
        return members[0];
    }

    // D8 business refusal: 47 with the request echoed and current revisions, then the current 44; no 36.
    GuildActionResult AssertRefused(GuildClient smith)
    {
        InventorySnapshot inventory = _guild.QueryInventory(smith);
        GuildSnapshot before = _guild.Query(smith);

        List<byte[]> replies = _guild.Act(smith, GuildWire.WeaponEnhance(inventory.Revision));

        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        GuildSnapshot current = GuildTestWorld.ReadSnapshot(replies[1]);
        Assert.Equal(GuildAction.WeaponEnhance, result.Action);
        Assert.NotEqual(GuildCode.Success, result.Result);
        Assert.Equal(inventory.Revision, result.RequestedRevision);
        Assert.Equal(inventory.Revision, result.InventoryRevision);
        Assert.Equal(before.GuildRevision, result.GuildRevision);
        Assert.Equal(current.Sequence, result.Sequence);
        AssertWeaponUnchanged(before, current);
        AssertInventoryUnchanged(inventory, _guild.QueryInventory(smith));
        AssertWeaponUnchanged(before, _guild.Query(smith));
        return result;
    }

    // Existing 37 → 38 (v17 table: byte result, int itemId, uint revision); returns the result byte.
    byte UseCoinPouch(GuildClient client, uint expectedRevision)
    {
        int mark = client.Mark;
        client.OnRecvPacket(GuildWire.ItemUse(GuildWire.CoinPouchItemId, expectedRevision));
        _guild.Settle();
        byte[] useResult = Assert.Single(client.Packets.Skip(mark), packet => GuildWire.IdOf(packet) == 38);
        return useResult[4];
    }

    void ReplaceInventoryState(GuildClient client, InventoryState state)
    {
        FieldInfo statesField = typeof(InventoryRegistry).GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("fixture: InventoryRegistry._states not found");
        IDictionary states = (IDictionary)statesField.GetValue(_guild.World.Inventory)!;
        object entry = states[client]
            ?? throw new InvalidOperationException("fixture: query the inventory first so the entry exists");
        PropertyInfo stateProperty = entry.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("fixture: InventoryRegistry entry has no State");
        stateProperty.SetValue(entry, state);
    }

    static void AssertInventoryUnchanged(InventorySnapshot before, InventorySnapshot after)
    {
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.Currency, after.Currency);
        Assert.Equal(before.Counts.OrderBy(pair => pair.Key), after.Counts.OrderBy(pair => pair.Key));
    }

    static void AssertWeaponUnchanged(GuildSnapshot before, GuildSnapshot after)
    {
        Assert.Equal(before.WeaponLevel, after.WeaponLevel);
        Assert.Equal(before.AttackBonus, after.AttackBonus);
        Assert.Equal(before.InventoryRevision, after.InventoryRevision);
    }
}
