using System.Reflection;
using System.Text.RegularExpressions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// Dungeon goal P5 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「던전 패킷·창 PR의 요구」), written before
// the table exists. Shared.GameData.DungeonClearRewards is read by name through reflection: a direct reference would
// stop this whole test assembly from compiling until it is added. Gold 50, Material 1 and CoinPouch 1 are the goal's
// fixed values (10-08 decision 2 A), and MaxItemKinds 3 is its fixed public name; none is a product calculation.
public sealed class DungeonClearRewardTableTests
{
    const string TableName = "Shared.GameData.DungeonClearRewards";
    const int GoalCurrency = 50;
    const int GoalMaxItemKinds = 3;

    [Fact]
    public void RewardTable_IsTheGoalStaticClass_WithFiftyGoldOneMaterialAndOneCoinPouch()
    {
        Type table = RequireTable();
        int currency = ReadStatic<int>(table, "Currency");
        (ItemId ItemId, int Count)[] items = ReadStatic<IReadOnlyList<InventorySlot>>(table, "Items")
            .Select(slot => (slot.ItemId, slot.Count))
            .OrderBy(slot => slot.ItemId)
            .ToArray();

        Assert.True(table.IsAbstract && table.IsSealed, "P5 DungeonClearRewards is a static class");
        Assert.Equal(GoalCurrency, currency);
        Assert.Equal(new[] { (ItemId.Material, 1), (ItemId.CoinPouch, 1) }, items);
    }

    [Fact]
    public void MaxItemKinds_IsConstThree_AndEqualsTheRewardSlotsOfTheClearResultPacket()
    {
        Type table = RequireTable();
        int maxItemKinds = ReadMaxItemKinds(table);
        Type? packet = typeof(PacketID).Assembly.GetType("Shared.Protocol.S_DungeonClearResult");
        Assert.True(packet is not null, "goal P1/P2: the generated packet Shared.Protocol.S_DungeonClearResult does not exist yet");
        int rewardSlots = packet!.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Count(field => Regex.IsMatch(field.Name, @"^reward\d+ItemId$"));

        Assert.Equal(GoalMaxItemKinds, maxItemKinds);
        Assert.Equal(maxItemKinds, rewardSlots);
    }

    [Fact]
    public void EveryReward_FitsTheCatalogAndTheInventoryLimits()
    {
        Type table = RequireTable();
        int currency = ReadStatic<int>(table, "Currency");
        IReadOnlyList<InventorySlot> items = ReadStatic<IReadOnlyList<InventorySlot>>(table, "Items");
        int maxItemKinds = ReadMaxItemKinds(table);

        Assert.InRange(currency, 0, InventoryLimits.MaxCurrency);
        // An empty table would make the per-item checks below pass with nothing to check.
        Assert.NotEmpty(items);
        Assert.True(items.Count <= maxItemKinds, $"P5 {items.Count} item kinds must fit the {maxItemKinds} packet slots");
        foreach (InventorySlot slot in items)
        {
            Assert.True(ItemCatalog.IsDefined(slot.ItemId), $"P5 reward item {slot.ItemId} is in ItemCatalog");
            Assert.InRange(slot.Count, 1, InventoryLimits.MaxStack);
        }
    }

    static Type RequireTable()
    {
        Type? table = typeof(InventoryLimits).Assembly.GetType(TableName);
        Assert.True(table is not null, $"goal P5: {TableName} does not exist yet (98_Shared/GameData/Dungeon/DungeonClearRewards.cs)");
        return table!;
    }

    // Field or property: the goal fixes the names and types of Currency and Items, not how they are declared.
    static T ReadStatic<T>(Type table, string name)
    {
        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        object? value = table.GetField(name, PublicStatic)?.GetValue(null) ?? table.GetProperty(name, PublicStatic)?.GetValue(null);
        Assert.True(value is T, $"goal public name: {table.Name}.{name} is a public static {typeof(T).Name}");
        return (T)value!;
    }

    // The goal fixes MaxItemKinds as a const int.
    static int ReadMaxItemKinds(Type table)
    {
        FieldInfo? field = table.GetField("MaxItemKinds", BindingFlags.Public | BindingFlags.Static);
        Assert.True(field is { IsLiteral: true } && field.FieldType == typeof(int), "goal public name: MaxItemKinds is a public const int");
        return (int)field!.GetRawConstantValue()!;
    }
}
