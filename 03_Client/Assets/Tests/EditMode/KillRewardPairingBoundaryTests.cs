using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // The one-second requirement is tested on both sides; expected rewards are literal snapshot differences.
    public class KillRewardPairingBoundaryTests
    {
        [TestCase(0.999f, true)]
        [TestCase(1.001f, false)]
        public void OneSecondWindow_AcceptsInsideAndRejectsOutside(float elapsed, bool expected)
        {
            var pairing = new KillRewardPairing();
            pairing.SetBaseline(25, new InventorySlot[0]);
            pairing.RecordDeath(new Vector3(5, 0, 0), 0);

            bool paired = pairing.TryTakeReward(35, new InventorySlot[0], elapsed, Vector3.zero, out var reward);

            Assert.AreEqual(expected, paired);
            if (paired) Assert.AreEqual(10, reward.Gold);
        }

        [Test]
        public void FallingGoldAndRisingItem_ProducesOnlyTheItemIncrease()
        {
            var pairing = new KillRewardPairing();
            pairing.SetBaseline(25, new[] { new InventorySlot(ItemId.Material, 2) });
            pairing.RecordDeath(new Vector3(5, 0, 0), 0);

            bool paired = pairing.TryTakeReward(15, new[] { new InventorySlot(ItemId.Material, 3) }, 0.1f,
                Vector3.zero, out var reward);

            Assert.IsTrue(paired);
            Assert.AreEqual(0, reward.Gold);
            CollectionAssert.AreEqual(new[] { new InventorySlot(ItemId.Material, 1) }, reward.Items);
        }

        [Test]
        public void NearestDeathIsConsumed_LeavingTheOtherDeathForTheNextSnapshot()
        {
            var pairing = new KillRewardPairing();
            pairing.SetBaseline(25, new InventorySlot[0]);
            pairing.RecordDeath(new Vector3(12, 0, 0), 0);
            pairing.RecordDeath(new Vector3(5, 0, 0), 0.05f);

            bool firstPaired = pairing.TryTakeReward(35, new InventorySlot[0], 0.1f, Vector3.zero, out var first);
            bool secondPaired = pairing.TryTakeReward(45, new InventorySlot[0], 0.2f, Vector3.zero, out var second);
            bool thirdPaired = pairing.TryTakeReward(55, new InventorySlot[0], 0.3f, Vector3.zero, out _);

            Assert.IsTrue(firstPaired);
            Assert.AreEqual(new Vector3(5, 0, 0), first.Spot);
            Assert.AreEqual(10, first.Gold);
            Assert.IsTrue(secondPaired);
            Assert.AreEqual(new Vector3(12, 0, 0), second.Spot);
            Assert.AreEqual(10, second.Gold);
            Assert.IsFalse(thirdPaired, "Each death can account for only one reward snapshot.");
        }

        [Test]
        public void ResetDiscardsPendingClearShare_BeforeTheNewConnectionsFirstKill()
        {
            var pairing = new KillRewardPairing();
            pairing.SetBaseline(25, new InventorySlot[0]);
            pairing.ExpectDungeonClearShare(50, new[] { new InventorySlot(ItemId.CoinPouch, 1) });
            pairing.Reset();
            bool baselinePaired = pairing.TryTakeReward(100, new InventorySlot[0], 0.1f, Vector3.zero, out _);
            pairing.RecordDeath(new Vector3(5, 0, 0), 0.2f);
            bool killPaired = pairing.TryTakeReward(110, new[] { new InventorySlot(ItemId.CoinPouch, 1) }, 0.3f,
                Vector3.zero, out var reward);

            Assert.IsFalse(baselinePaired);
            Assert.IsTrue(killPaired);
            Assert.AreEqual(10, reward.Gold);
            CollectionAssert.AreEqual(new[] { new InventorySlot(ItemId.CoinPouch, 1) }, reward.Items);
        }
    }
}
