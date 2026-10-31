using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // KillRewardPairing rules (01_Phases/goals/2026-10-30-kill-reward-float-text/goal.md 「만들 것」 2~5) without Unity objects.
    // Expected numbers and lines are written out; none is derived from the product's own arithmetic or the catalog.
    public class KillRewardPairingTests
    {
        static readonly Vector3 Near = new Vector3(5f, 0f, 0f);
        static readonly Vector3 Far = new Vector3(12f, 0f, 0f);
        static readonly Vector3 Player = new Vector3(2f, 0f, 0f);

        [Test]
        public void FirstSnapshot_OnlySetsTheBaseline_ThenTheNextKillShowsItsIncrease()
        {
            var pairing = new KillRewardPairing();
            pairing.RecordDeath(Near, 0f);
            Assert.IsFalse(pairing.TryTakeReward(25, Slots(Materials(2)), 0.1f, Player, out _));

            pairing.RecordDeath(Far, 1f);
            Assert.IsTrue(pairing.TryTakeReward(35, Slots(Materials(3)), 1.1f, Player, out KillRewardPairing.Reward reward));
            Assert.AreEqual(Far, reward.Spot);
            Assert.AreEqual(10, reward.Gold);
            CollectionAssert.AreEqual(new[] { Materials(1) }, reward.Items);
        }

        [Test]
        public void TheBaselineMovesWithEverySnapshot_SoASecondKillShowsOnlyItsOwnIncrease()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            Assert.IsTrue(pairing.TryTakeReward(35, Slots(Materials(3)), 0.1f, Player, out _));

            pairing.RecordDeath(Far, 5f);
            Assert.IsTrue(pairing.TryTakeReward(45, Slots(Materials(3)), 5.1f, Player, out KillRewardPairing.Reward reward));
            Assert.AreEqual(10, reward.Gold);
            CollectionAssert.IsEmpty(reward.Items);
        }

        [Test]
        public void DecreasedValues_AreIgnored()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            Assert.IsFalse(pairing.TryTakeReward(15, Slots(Materials(1)), 0.1f, Player, out _));
        }

        [Test]
        public void AnIncreaseWithoutADeath_ShowsNothing_AndBecomesTheBaseline()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            Assert.IsFalse(pairing.TryTakeReward(35, Slots(Materials(3)), 0.1f, Player, out _));

            pairing.RecordDeath(Near, 1f);
            Assert.IsTrue(pairing.TryTakeReward(45, Slots(Materials(3)), 1.1f, Player, out KillRewardPairing.Reward reward));
            Assert.AreEqual(10, reward.Gold);
        }

        [Test]
        public void ADeathOlderThanTheOneSecondWindow_DoesNotPair()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            Assert.IsFalse(pairing.TryTakeReward(35, Slots(Materials(3)), 1.3f, Player, out _));
        }

        [Test]
        public void OfTwoDeathsInTheWindow_TheOneNearestThePlayerIsUsed_InEitherOrder()
        {
            KillRewardPairing nearFirst = WithBaseline(25, Materials(2));
            nearFirst.RecordDeath(Near, 0f);
            nearFirst.RecordDeath(Far, 0.05f);
            Assert.IsTrue(nearFirst.TryTakeReward(35, Slots(Materials(3)), 0.1f, Player, out KillRewardPairing.Reward first));
            Assert.AreEqual(Near, first.Spot);

            KillRewardPairing farFirst = WithBaseline(25, Materials(2));
            farFirst.RecordDeath(Far, 0f);
            farFirst.RecordDeath(Near, 0.05f);
            Assert.IsTrue(farFirst.TryTakeReward(35, Slots(Materials(3)), 0.1f, Player, out KillRewardPairing.Reward second));
            Assert.AreEqual(Near, second.Spot);
        }

        [Test]
        public void AUsedDeath_IsNotPairedTwice()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            Assert.IsTrue(pairing.TryTakeReward(35, Slots(Materials(3)), 0.1f, Player, out _));
            Assert.IsFalse(pairing.TryTakeReward(45, Slots(Materials(3)), 0.2f, Player, out _));
        }

        [Test]
        public void ADungeonClearSuccessShare_IsTakenOutOfTheNextSnapshotOnly()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            pairing.ExpectDungeonClearShare(50, Slots(Materials(1), CoinPouches(1)));
            // 25 → 85 with the clear's 50, one material and one coin pouch: only the kill's 10 gold is left.
            Assert.IsTrue(pairing.TryTakeReward(85, Slots(Materials(3), CoinPouches(1)), 0.1f, Player, out KillRewardPairing.Reward reward));
            Assert.AreEqual(10, reward.Gold);
            CollectionAssert.IsEmpty(reward.Items);

            pairing.RecordDeath(Far, 5f);
            Assert.IsTrue(pairing.TryTakeReward(95, Slots(Materials(4), CoinPouches(1)), 5.1f, Player, out KillRewardPairing.Reward next));
            Assert.AreEqual(10, next.Gold);
            CollectionAssert.AreEqual(new[] { Materials(1) }, next.Items);
        }

        [Test]
        public void ASnapshotThatHoldsOnlyTheClearShare_ShowsNothing()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            pairing.ExpectDungeonClearShare(50, Slots(Materials(1), CoinPouches(1)));
            Assert.IsFalse(pairing.TryTakeReward(75, Slots(Materials(3), CoinPouches(1)), 0.1f, Player, out _));
        }

        [Test]
        public void AfterReset_TheNextSnapshotIsOnlyABaseline_AndDeathsBeforeItDoNotPairLater()
        {
            KillRewardPairing pairing = WithBaseline(25, Materials(2));
            pairing.RecordDeath(Near, 0f);
            pairing.Reset();
            pairing.RecordDeath(Near, 0.05f);
            Assert.IsFalse(pairing.TryTakeReward(35, Slots(Materials(3)), 0.1f, Player, out _));
            Assert.IsFalse(pairing.TryTakeReward(45, Slots(Materials(3)), 0.2f, Player, out _));
        }

        [Test]
        public void LinesUseTheGoldWordAndTheCatalogNameWithOneSpaceBeforeTheNumber()
        {
            Assert.AreEqual("+금화 10", KillRewardPairing.GoldLine(10));
            Assert.AreEqual("+재료 1", KillRewardPairing.ItemLine(Materials(1)));
            Assert.AreEqual("+재화 주머니 1", KillRewardPairing.ItemLine(CoinPouches(1)));
        }

        static KillRewardPairing WithBaseline(int currency, params InventorySlot[] slots)
        {
            var pairing = new KillRewardPairing();
            pairing.SetBaseline(currency, Slots(slots));
            return pairing;
        }

        static InventorySlot[] Slots(params InventorySlot[] slots) => slots;

        static InventorySlot Materials(int count) => new InventorySlot(ItemId.Material, count);

        static InventorySlot CoinPouches(int count) => new InventorySlot(ItemId.CoinPouch, count);
    }
}
