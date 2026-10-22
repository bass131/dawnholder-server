using System.Collections.Generic;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;

namespace Dawnholder.Client.Tests
{
    /// <summary>Independent review: server-race and refusal boundaries the PR2 suites do not combine.</summary>
    public sealed class GuildClientIndependentReviewTests
    {
        GuildClientHarness _h;

        [SetUp]
        public void SetUp() => _h = new GuildClientHarness();

        [TearDown]
        public void TearDown() => _h?.Dispose();

        [Test]
        public void StrongholdRace_OtherMembersBroadcastAndOwnStale_EachNeedThePointedSnapshot()
        {
            _h.Synchronize();
            var growth = new List<GuildChangeKind>();
            using var subscription = GuildClientProduct.ObserveGrowth(_h.Session, growth);
            Assert.IsTrue(GuildClientProduct.Request(_h.Session, "TryUpgradeStronghold"));
            List<byte[]> sent = PeerFrames.Take(_h.Sockets);
            Assert.AreEqual(1, sent.Count);
            CollectionAssert.AreEqual(new byte[] { 8, 0, 45, 0, 7, 0, 0, 0 }, sent[0]);

            // Another member's upgrade wins the race; the broadcast is applied but is not our answer.
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "a broadcast cannot close our purchase");
            Assert.AreEqual(2, GuildClientProduct.Value<int>(_h.Session, "StrongholdLevel"));
            Assert.AreEqual(10, GuildClientProduct.Value<int>(_h.Session, "Treasury"));

            _h.Deliver(GuildClientWire.Result(action: 1, result: 1, requested: 7, guildRevision: 8, sequence: 3));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "Stale alone waits for the pointed 44");
            _h.Deliver(GuildClientWire.Snapshot(sequence: 3, guildRevision: 8, treasury: 10, unlocked: true));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(GuildResult.Stale,
                GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.AreEqual(10, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
            CollectionAssert.AreEqual(new[] { GuildChangeKind.StrongholdUpgrade }, growth);

            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "TryUpgradeStronghold"), "server reports MaxLevel");
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets), "no retry or resend after the race");
        }

        [Test]
        public void RefusedEnhancement_KeepsServerBalance_AndFollowsTheServerAvailability()
        {
            _h.Synchronize(unlocked: true);
            Assert.IsTrue(GuildClientProduct.Request(_h.Session, "TryEnhanceWeapon"));
            CollectionAssert.AreEqual(new byte[] { 8, 0, 46, 0, 3, 0, 0, 0 }, _h.Sockets.ReadFrame());

            byte[] pointed = GuildClientWire.Snapshot(sequence: 2, unlocked: true);
            pointed[63] = 8;
            _h.Deliver(GuildClientWire.Result(action: 2, result: 8, requested: 3, guildRevision: 7, sequence: 2));
            _h.Deliver(pointed);

            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(GuildResult.InsufficientMaterial,
                GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "AttackBonus"));
            Assert.AreEqual(71, InventoryState.Instance.Currency);
            Assert.AreEqual(4, InventoryState.Instance.GetSlot(0).Count);
            Assert.AreEqual(GuildResult.InsufficientMaterial, GuildClientProduct.Value<GuildResult>(_h.Session, "WeaponAvailability"));
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "TryEnhanceWeapon"), "server refusal is not retried locally");
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [Test]
        public void EnhancementStaleAfterNewer36_NextRequestCarriesTheLatestInventoryRevision()
        {
            _h.Synchronize(unlocked: true);
            Assert.IsTrue(GuildClientProduct.Request(_h.Session, "TryEnhanceWeapon"));
            CollectionAssert.AreEqual(new byte[] { 8, 0, 46, 0, 3, 0, 0, 0 }, _h.Sockets.ReadFrame());

            _h.Deliver(InventoryWire.Snapshot(4, 71, (1, 5)));
            _h.Deliver(GuildClientWire.Result(action: 2, result: 1, requested: 3, guildRevision: 7, inventoryRevision: 4, sequence: 2));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, inventoryRevision: 4, unlocked: true));

            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(GuildResult.Stale,
                GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"));
            Assert.AreEqual(5, InventoryState.Instance.GetSlot(0).Count);
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets), "Stale is reported, not resent");

            Assert.IsTrue(GuildClientProduct.Request(_h.Session, "TryEnhanceWeapon"));
            CollectionAssert.AreEqual(new byte[] { 8, 0, 46, 0, 4, 0, 0, 0 }, _h.Sockets.ReadFrame());
        }
    }
}
