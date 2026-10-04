using Dawnholder.Server.GameServer.Items;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Items;

// Independent verification of the economy authority at limits that the public wire cannot reach
// with today's two item kinds and small rewards: currency next to MaxCurrency, revision next to
// uint.MaxValue, bundles that only partly fit, and corrupted internal values. These states are
// built directly as internal fixtures; the wire paths are covered by InventoryWireContractTests.
// Every expected number is a literal from the PR1 acceptance table referenced from
// 01_Phases/goals/2026-10-05-items-inventory-currency/goal.md (MaxStack 99, MaxCurrency 1,000,000,000,
// normal 10 + material 1, golem 10 + pouch 1, boss 50 + material 1 + pouch 1, pouch use +50),
// never recomputed from the product.
public sealed class InventoryAuthorityBoundaryTests
{
    const int MaxCurrency = 1_000_000_000;

    static InventoryReward NormalReward => KillRewardPolicy.Resolve(7, EnemyKind.Normal, 9).Reward;

    static InventoryReward BossReward => KillRewardPolicy.Resolve(7, EnemyKind.Boss, 9).Reward;

    [Fact]
    public void Grant_UpToMaxCurrency_Succeeds_AndTheNextRewardIsRejectedWhole()
    {
        InventoryState current = State(5, 999_999_990);

        InventoryResult reachCap = InventoryTransitions.Grant(current, NormalReward, out InventoryState atCap);
        InventoryResult passCap = InventoryTransitions.Grant(atCap, NormalReward, out InventoryState afterCap);

        Assert.Equal(InventoryResult.Success, reachCap);
        AssertState(atCap, 6, MaxCurrency, (ItemId.Material, 1));
        AssertState(current, 5, 999_999_990);
        Assert.Equal(InventoryResult.CurrencyCap, passCap);
        Assert.Same(atCap, afterCap);
        AssertState(atCap, 6, MaxCurrency, (ItemId.Material, 1));
    }

    [Fact]
    public void Grant_BundleWhoseLaterItemOverflowsItsStack_AppliesNothing()
    {
        // The boss bundle's material would fit; only the pouch stack is full. Nothing may be granted.
        InventoryState current = State(10, 100, (ItemId.CoinPouch, 99));

        InventoryResult result = InventoryTransitions.Grant(current, BossReward, out InventoryState next);

        Assert.NotEqual(InventoryResult.Success, result);
        Assert.Same(current, next);
        AssertState(current, 10, 100, (ItemId.CoinPouch, 99));
    }

    [Fact]
    public void Grant_BundleWhoseCurrencyOverflows_AppliesNoItemEither()
    {
        InventoryState current = State(10, 999_999_990, (ItemId.Material, 98));

        InventoryResult result = InventoryTransitions.Grant(current, BossReward, out InventoryState next);

        Assert.NotEqual(InventoryResult.Success, result);
        Assert.Same(current, next);
        AssertState(current, 10, 999_999_990, (ItemId.Material, 98));
    }

    [Fact]
    public void UsePouch_AtExactCap_Succeeds_ButPastTheCapReturnsCurrencyCapAndKeepsThePouch()
    {
        InventoryState exact = State(3, 999_999_950, (ItemId.CoinPouch, 2));
        InventoryState over = State(3, 999_999_951, (ItemId.CoinPouch, 1));

        InventoryResult exactResult = InventoryTransitions.Use(exact, ItemId.CoinPouch, 3, out InventoryState exactNext);
        InventoryResult overResult = InventoryTransitions.Use(over, ItemId.CoinPouch, 3, out InventoryState overNext);

        Assert.Equal(InventoryResult.Success, exactResult);
        AssertState(exactNext, 4, MaxCurrency, (ItemId.CoinPouch, 1));
        Assert.Equal(InventoryResult.CurrencyCap, overResult);
        Assert.Same(over, overNext);
        AssertState(over, 3, 999_999_951, (ItemId.CoinPouch, 1));
    }

    [Fact]
    public void Revision_ReachesUintMaxWithoutWrapping_ThenNewChangesAreRevisionExhausted()
    {
        InventoryState nearEnd = State(4_294_967_294u, 0);
        InventoryState exhaustedWithPouch = State(4_294_967_295u, 0, (ItemId.CoinPouch, 1));

        InventoryResult lastGrant = InventoryTransitions.Grant(nearEnd, NormalReward, out InventoryState atEnd);
        InventoryResult grantAfterEnd = InventoryTransitions.Grant(atEnd, NormalReward, out InventoryState afterGrant);
        InventoryResult useAfterEnd = InventoryTransitions.Use(
            exhaustedWithPouch, ItemId.CoinPouch, 4_294_967_295u, out InventoryState afterUse);

        Assert.Equal(InventoryResult.Success, lastGrant);
        AssertState(atEnd, 4_294_967_295u, 10, (ItemId.Material, 1));
        Assert.Equal(InventoryResult.RevisionExhausted, grantAfterEnd);
        Assert.Same(atEnd, afterGrant);
        Assert.Equal(InventoryResult.RevisionExhausted, useAfterEnd);
        Assert.Same(exhaustedWithPouch, afterUse);
        AssertState(exhaustedWithPouch, 4_294_967_295u, 0, (ItemId.CoinPouch, 1));
    }

    [Fact]
    public void Construction_OrdersSlotsByItemId_AndUsingTheLastPouchLeavesAnEmptySlot()
    {
        // Input order is acquisition order; the canonical value is ItemId order padded with 0/0.
        InventoryState current = State(1, 0, (ItemId.CoinPouch, 1), (ItemId.Material, 2));

        InventoryResult result = InventoryTransitions.Use(current, ItemId.CoinPouch, 1, out InventoryState next);

        AssertState(current, 1, 0, (ItemId.Material, 2), (ItemId.CoinPouch, 1));
        Assert.Equal(InventoryResult.Success, result);
        AssertState(next, 2, 50, (ItemId.Material, 2));
    }

    [Theory]
    [InlineData("currency-negative")]
    [InlineData("currency-over-cap")]
    [InlineData("count-zero")]
    [InlineData("count-negative")]
    [InlineData("count-over-stack")]
    [InlineData("item-undefined")]
    [InlineData("item-negative")]
    [InlineData("none-with-count")]
    [InlineData("duplicate-stack")]
    public void CorruptedInternalState_IsRejected_NotNormalized(string variant)
    {
        // Two defined item kinds cannot exceed eight slots without duplicates, so the slot-capacity
        // branch is unreachable here; duplicates are rejected first.
        Exception? thrown = Record.Exception(() => CorruptedState(variant));

        Assert.NotNull(thrown);
    }

    [Theory]
    [InlineData("reward-currency-negative")]
    [InlineData("reward-empty")]
    [InlineData("reward-count-zero")]
    [InlineData("reward-count-negative")]
    [InlineData("reward-item-undefined")]
    [InlineData("reward-duplicate")]
    [InlineData("policy-killer-zero")]
    [InlineData("policy-enemy-zero")]
    [InlineData("policy-kind-unknown")]
    [InlineData("use-item-undefined")]
    public void InvalidRewardOrInternalItem_IsAnErrorRatherThanAnOrdinaryRefusal(string variant)
    {
        Exception? thrown = Record.Exception(() => InvalidInput(variant));

        Assert.NotNull(thrown);
    }

    [Fact]
    public void ASnapshot_CannotBeChangedThroughItsInputsOrItsSlotView()
    {
        InventorySlot[] source = { new(ItemId.Material, 5) };
        InventoryState state = new(1, 10, source);
        InventorySlot[] rewardItems = { new(ItemId.CoinPouch, 1) };
        InventoryReward reward = new(0, rewardItems);

        source[0] = new InventorySlot(ItemId.CoinPouch, 99);
        rewardItems[0] = new InventorySlot(ItemId.Material, 99);
        TryOverwriteFirstSlot(state.Slots);

        AssertState(state, 1, 10, (ItemId.Material, 5));
        InventorySlot rewardItem = Assert.Single(reward.Items);
        Assert.Equal(ItemId.CoinPouch, rewardItem.ItemId);
        Assert.Equal(1, rewardItem.Count);
    }

    [Fact]
    public void KillRewardPolicy_PaysTheKillerTheFixedPlaceholderTable()
    {
        (int normalRecipient, InventoryReward normal) = KillRewardPolicy.Resolve(11, EnemyKind.Normal, 21);
        (int golemRecipient, InventoryReward golem) = KillRewardPolicy.Resolve(12, EnemyKind.Golem, 22);
        (int bossRecipient, InventoryReward boss) = KillRewardPolicy.Resolve(13, EnemyKind.Boss, 23);
        EnemyKind[] unpaidKinds = Enum.GetValues<EnemyKind>()
            .Where(kind => kind is not (EnemyKind.Normal or EnemyKind.Golem or EnemyKind.Boss))
            .ToArray();

        Assert.Equal(11, normalRecipient);
        Assert.Equal(10, normal.Currency);
        Assert.Equal(new[] { (ItemId.Material, 1) }, Items(normal));
        Assert.Equal(12, golemRecipient);
        Assert.Equal(10, golem.Currency);
        Assert.Equal(new[] { (ItemId.CoinPouch, 1) }, Items(golem));
        Assert.Equal(13, bossRecipient);
        Assert.Equal(50, boss.Currency);
        Assert.Equal(new[] { (ItemId.Material, 1), (ItemId.CoinPouch, 1) }, Items(boss));
        // The acceptance asks for an explicit table before any further enemy kind is rewarded.
        Assert.Empty(unpaidKinds);
    }

    static InventoryState State(uint revision, int currency, params (ItemId ItemId, int Count)[] stacks)
        => new(revision, currency, stacks.Select(stack => new InventorySlot(stack.ItemId, stack.Count)).ToArray());

    static void CorruptedState(string variant)
    {
        _ = variant switch
        {
            "currency-negative" => State(0, -1),
            "currency-over-cap" => State(0, MaxCurrency + 1),
            "count-zero" => State(0, 0, (ItemId.Material, 0)),
            "count-negative" => State(0, 0, (ItemId.Material, -1)),
            "count-over-stack" => State(0, 0, (ItemId.Material, 100)),
            "item-undefined" => State(0, 0, ((ItemId)3, 1)),
            "item-negative" => State(0, 0, ((ItemId)(-1), 1)),
            "none-with-count" => State(0, 0, (ItemId.None, 5)),
            "duplicate-stack" => State(0, 0, (ItemId.Material, 1), (ItemId.Material, 2)),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "unknown test variant"),
        };
    }

    static void InvalidInput(string variant)
    {
        switch (variant)
        {
            case "reward-currency-negative":
                _ = new InventoryReward(-1, new InventorySlot(ItemId.Material, 1));
                break;
            case "reward-empty":
                _ = new InventoryReward(0);
                break;
            case "reward-count-zero":
                _ = new InventoryReward(10, new InventorySlot(ItemId.Material, 0));
                break;
            case "reward-count-negative":
                _ = new InventoryReward(10, new InventorySlot(ItemId.Material, -1));
                break;
            case "reward-item-undefined":
                _ = new InventoryReward(10, new InventorySlot((ItemId)3, 1));
                break;
            case "reward-duplicate":
                _ = new InventoryReward(10, new InventorySlot(ItemId.Material, 1), new InventorySlot(ItemId.Material, 1));
                break;
            case "policy-killer-zero":
                _ = KillRewardPolicy.Resolve(0, EnemyKind.Normal, 9);
                break;
            case "policy-enemy-zero":
                _ = KillRewardPolicy.Resolve(7, EnemyKind.Normal, 0);
                break;
            case "policy-kind-unknown":
                _ = KillRewardPolicy.Resolve(7, (EnemyKind)99, 9);
                break;
            case "use-item-undefined":
                // The wire handler filters undefined IDs; reaching the transition with one is an
                // internal error, not a NotOwned answer.
                _ = InventoryTransitions.Use(State(0, 0), (ItemId)3, 0, out _);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(variant), variant, "unknown test variant");
        }
    }

    // Tries every write path a caller could reach from the returned view; none may reach the state.
    static void TryOverwriteFirstSlot(IReadOnlyList<InventorySlot> slots)
    {
        InventorySlot forged = new(ItemId.CoinPouch, 99);
        if (slots is InventorySlot[] array)
        {
            array[0] = forged;
        }
        if (slots is IList<InventorySlot> list)
        {
            try
            {
                list[0] = forged;
            }
            catch (NotSupportedException)
            {
            }
        }
    }

    static (ItemId ItemId, int Count)[] Items(InventoryReward reward)
        => reward.Items.Select(item => (item.ItemId, item.Count)).OrderBy(item => item.ItemId).ToArray();

    // Occupied stacks in ItemId order, then 0/0 up to the fixed eight slots.
    static void AssertState(InventoryState state, uint revision, int currency, params (ItemId ItemId, int Count)[] occupied)
    {
        (ItemId ItemId, int Count)[] expectedSlots = occupied
            .Concat(Enumerable.Repeat((ItemId.None, 0), 8 - occupied.Length))
            .ToArray();
        (ItemId ItemId, int Count)[] actualSlots = state.Slots.Select(slot => (slot.ItemId, slot.Count)).ToArray();

        Assert.Equal(revision, state.Revision);
        Assert.Equal(currency, state.Currency);
        Assert.Equal(expectedSlots, actualSlots);
    }
}
