using System;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;

namespace Dawnholder.Client.Tests
{
    // C6: real session dispatch, independently authored v20 wire. Unity, sockets and the main queue
    // are real; server, scene loading and Art are outside this EditMode fixture.
    public sealed class GuildClientPacketTests
    {
        GuildClientHarness _h;

        [SetUp] public void Setup() => _h = new GuildClientHarness();
        [TearDown] public void Teardown() => _h?.Dispose();

        [Test]
        public void Snapshot_UsesCurrentMainQueue_AndCapturesTheReceiveBufferBeforeReuse()
        {
            object state = GuildClientProduct.State(_h.Session);
            Assert.IsFalse(GuildClientProduct.Read<bool>(state, "HasSnapshot"));
            byte[] raw = GuildClientWire.Snapshot(inventoryRevision: 0);
            byte[] offset = Enumerable.Repeat((byte)0xCC, 90).ToArray();
            Buffer.BlockCopy(raw, 0, offset, 7, raw.Length);
            _h.Session.OnRecvPacket(new ArraySegment<byte>(offset, 7, raw.Length));
            Assert.IsFalse(GuildClientProduct.Read<bool>(state, "HasSnapshot"), "receive thread cannot publish");
            Array.Clear(offset, 0, offset.Length);
            _h.Queue.Drain();
            Assert.IsTrue(GuildClientProduct.Read<bool>(state, "HasSnapshot"));
            Assert.AreEqual(1L, GuildClientProduct.Read<long>(state, "SnapshotSequence"));
            Assert.AreEqual("새벽 길드", GuildClientProduct.Read<string>(state, "GuildName"));
            Assert.AreEqual(7u, GuildClientProduct.Read<uint>(state, "GuildRevision"));
            Assert.AreEqual(40, GuildClientProduct.Read<int>(state, "Treasury"));
            Assert.AreEqual(7, GuildClientProduct.Read<int>(state, "MemberCount"));
            Assert.AreEqual(3, GuildClientProduct.Read<int>(state, "OnlineMemberCount"));
            Assert.AreEqual(0u, GuildClientProduct.Read<uint>(state, "InventoryRevision"));
            Assert.AreEqual(0, GuildClientProduct.Read<int>(state, "WeaponLevel"));
            Assert.AreEqual(30, GuildClientProduct.Read<int>(state, "StrongholdCost"));
            Assert.AreEqual(20, GuildClientProduct.Read<int>(state, "WeaponCurrencyCost"));
        }

        [TestCase("가")]
        [TestCase("1234567890123456")]
        [TestCase("\uD83D\uDE00")]
        [TestCase("12345678901234\uD83D\uDE00")]
        public void ValidUtf16NameBoundaries_AreAcceptedWithoutReplacement(string name)
        {
            GuildClientProduct.State(_h.Session);
            _h.Deliver(GuildClientWire.Name(GuildClientWire.Snapshot(), GuildClientWire.Utf16(name)));
            Assert.AreEqual(name, GuildClientProduct.Value<string>(_h.Session, "GuildName"));
        }

        [Test]
        public void UpperBounds_AreServerValues_AndDoNotOverflow()
        {
            GuildClientProduct.State(_h.Session);
            byte[] raw = GuildClientWire.Snapshot(long.MaxValue, uint.MaxValue, uint.MaxValue,
                treasury: 1_000_000_000, unlocked: true, enhanced: true);
            GuildClientWire.I32(raw, 28, int.MaxValue);
            GuildClientWire.I32(raw, 32, int.MaxValue);
            _h.Deliver(raw);
            Assert.AreEqual(long.MaxValue, GuildClientProduct.Value<long>(_h.Session, "SnapshotSequence"));
            Assert.AreEqual(uint.MaxValue, GuildClientProduct.Value<uint>(_h.Session, "GuildRevision"));
            Assert.AreEqual(uint.MaxValue, GuildClientProduct.Value<uint>(_h.Session, "InventoryRevision"));
            Assert.AreEqual(1_000_000_000, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
            Assert.AreEqual(2, GuildClientProduct.Value<int>(_h.Session, "StrongholdLevel"));
            Assert.AreEqual(1, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"));
            Assert.AreEqual(5, GuildClientProduct.Value<int>(_h.Session, "AttackBonus"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "StrongholdCost"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "WeaponCurrencyCost"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "WeaponMaterialCount"));
        }

        static IEnumerable<TestCaseData> InvalidSnapshots()
        {
            for (int length = 0; length < 68; length++)
                yield return new TestCaseData(GuildClientWire.Snapshot(sequence: 2).Take(length).ToArray()).SetName("Snapshot_RejectsLength_" + length);
            yield return new TestCaseData(GuildClientWire.Snapshot(sequence: 2).Concat(new byte[] { 0 }).ToArray()).SetName("Snapshot_Rejects_extra_byte");
            yield return Bad("header_size", w => w[0] = 75);
            yield return Bad("wrong_id", w => w[2] = 47);
            yield return Bad("sequence_zero", w => GuildClientWire.I64(w, 4, 0));
            yield return Bad("sequence_negative", w => GuildClientWire.I64(w, 4, -1));
            yield return Bad("change_kind", w => w[12] = 6);
            yield return Bad("treasury_negative", w => GuildClientWire.I32(w, 21, -1));
            yield return Bad("treasury_over_cap", w => GuildClientWire.I32(w, 21, 1_000_000_001));
            yield return Bad("stronghold_zero", w => w[25] = 0);
            yield return Bad("stronghold_three", w => w[25] = 3);
            yield return Bad("max_stronghold", w => w[26] = 3);
            yield return Bad("unlocked_outside_bool", w => w[27] = 2);
            yield return Bad("unlocked_at_one", w => w[27] = 1);
            yield return Bad("locked_at_two", w => w[25] = 2);
            yield return Bad("member_negative", w => GuildClientWire.I32(w, 28, -1));
            yield return Bad("online_negative", w => GuildClientWire.I32(w, 32, -1));
            yield return Bad("online_exceeds_members", w => GuildClientWire.I32(w, 32, 8));
            yield return Bad("weapon_two", w => w[40] = 2);
            yield return Bad("max_weapon", w => w[41] = 2);
            yield return Bad("bonus_at_zero", w => GuildClientWire.I32(w, 42, 5));
            yield return Bad("bonus_mismatch", w => GuildClientWire.I32(w, 42, 4));
            yield return Bad("material_id", w => GuildClientWire.I32(w, 54, 2));
            yield return Bad("stronghold_availability", w => w[62] = 11);
            yield return Bad("weapon_availability", w => w[63] = 255);
            yield return Bad("negative_stronghold_cost", w => GuildClientWire.I32(w, 46, -1));
            yield return Bad("negative_weapon_cost", w => GuildClientWire.I32(w, 50, -1));
            yield return Bad("negative_material_count", w => GuildClientWire.I32(w, 58, -1));
            yield return Bad("max_stronghold_has_cost", w => GuildClientWire.I32(w, 46, 30), GuildClientWire.Snapshot(sequence: 2, unlocked: true));
            yield return Bad("max_weapon_has_cost", w => GuildClientWire.I32(w, 50, 20), GuildClientWire.Snapshot(sequence: 2, unlocked: true, enhanced: true));
            yield return Bad("max_weapon_has_material", w => GuildClientWire.I32(w, 58, 1), GuildClientWire.Snapshot(sequence: 2, unlocked: true, enhanced: true));
            yield return Bad("max_weapon_bonus_zero", w => GuildClientWire.I32(w, 42, 0), GuildClientWire.Snapshot(sequence: 2, unlocked: true, enhanced: true));
            yield return Bad("name_empty", w => w[64] = 0);
            yield return Bad("name_odd", w => w[64] = 9);
            yield return Bad("name_too_long", w => w[64] = 34);
            yield return Bad("name_length_mismatch", w => w[64] = 8);
            yield return new TestCaseData(GuildClientWire.Name(GuildClientWire.Snapshot(sequence: 2))).SetName("Snapshot_RejectsExactEmptyNameFrame");
            yield return new TestCaseData(GuildClientWire.Name(GuildClientWire.Snapshot(sequence: 2), 65)).SetName("Snapshot_RejectsExactOddNameFrame");
            yield return new TestCaseData(GuildClientWire.Name(GuildClientWire.Snapshot(sequence: 2),
                GuildClientWire.Utf16("12345678901234567"))).SetName("Snapshot_RejectsExactSeventeenCodeUnitNameFrame");
            yield return new TestCaseData(GuildClientWire.Name(GuildClientWire.Snapshot(sequence: 2), 0, 0xD8)).SetName("Snapshot_RejectsUnpairedHighSurrogate");
            yield return new TestCaseData(GuildClientWire.Name(GuildClientWire.Snapshot(sequence: 2), 0, 0xDC)).SetName("Snapshot_RejectsUnpairedLowSurrogate");
            yield return new TestCaseData(GuildClientWire.Name(GuildClientWire.Snapshot(sequence: 2), 0, 0xD8, 65, 0)).SetName("Snapshot_RejectsHighSurrogateFollowedByLetter");
        }

        static TestCaseData Bad(string name, Action<byte[]> mutate, byte[] wire = null)
        {
            wire = wire ?? GuildClientWire.Snapshot(sequence: 2);
            mutate(wire);
            return new TestCaseData(wire).SetName("Snapshot_Rejects_" + name);
        }

        [TestCaseSource(nameof(InvalidSnapshots))]
        public void MalformedSnapshot_IsRejectedBeforeRead_WithoutChangingState(byte[] raw)
        {
            object state = GuildClientProduct.State(_h.Session);
            _h.Deliver(GuildClientWire.Snapshot());
            Assert.DoesNotThrow(() => GuildClientProduct.HandleAs(_h.Session, 44, new ArraySegment<byte>(raw)));
            _h.Queue.Drain();
            Assert.AreEqual(1L, GuildClientProduct.Read<long>(state, "SnapshotSequence"));
            Assert.AreEqual(40, GuildClientProduct.Read<int>(state, "Treasury"));
            Assert.AreEqual("새벽 길드", GuildClientProduct.Read<string>(state, "GuildName"));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.IsFalse(_h.Session.IsClosed);
        }

        [Test]
        public void NullSegments_AreRejectedByBothHandlers()
        {
            object state = GuildClientProduct.State(_h.Session);
            Assert.DoesNotThrow(() => GuildClientProduct.HandleAs(_h.Session, 44, default));
            Assert.DoesNotThrow(() => GuildClientProduct.HandleAs(_h.Session, 47, default));
            _h.Queue.Drain();
            Assert.IsFalse(GuildClientProduct.Read<bool>(state, "HasSnapshot"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QueuedSnapshot_AfterCloseOrReplacement_CannotPublish(bool replace)
        {
            GuildClientProduct.State(_h.Session);
            _h.Session.OnRecvPacket(new ArraySegment<byte>(GuildClientWire.Snapshot()));
            if (replace) _h.Current = false;
            else _h.Session.OnDisconnected(ConnectionHarness.Endpoint);
            _h.Queue.Drain();
            Assert.IsFalse(GuildClientProduct.Value<bool>(_h.Session, "HasSnapshot"));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [Test]
        public void OnlyLargerSequenceApplies_EvenWhenOldPacketHasLargerEconomicRevisions()
        {
            GuildClientProduct.State(_h.Session);
            _h.Deliver(GuildClientWire.Snapshot(sequence: 9, treasury: 40));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 9, guildRevision: 99, inventoryRevision: 99, treasury: 999));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 8, guildRevision: 100, inventoryRevision: 100, treasury: 888));
            Assert.AreEqual(9L, GuildClientProduct.Value<long>(_h.Session, "SnapshotSequence"));
            Assert.AreEqual(40, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
            Assert.AreEqual(3u, GuildClientProduct.Value<uint>(_h.Session, "InventoryRevision"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Revision36And44_MergeMonotonically_WhileNewSharedSequenceStillApplies(bool inventoryFirst)
        {
            GuildClientProduct.State(_h.Session);
            _h.Deliver(GuildClientWire.Snapshot(inventoryRevision: 4, unlocked: true, enhanced: true));
            byte[] inventory = InventoryWire.Snapshot(10, 71, (1, 4));
            byte[] guild = GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, inventoryRevision: 9, treasury: 123, unlocked: true);
            _h.Deliver(inventoryFirst ? inventory : guild);
            _h.Deliver(inventoryFirst ? guild : inventory);
            Assert.AreEqual(10u, GuildClientProduct.Value<uint>(_h.Session, "InventoryRevision"));
            Assert.AreEqual(123, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
            Assert.AreEqual(2L, GuildClientProduct.Value<long>(_h.Session, "SnapshotSequence"));
            Assert.AreEqual(71, Dawnholder.Client.State.InventoryState.Instance.Currency);
            if (inventoryFirst)
            {
                Assert.AreEqual(1, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"), "lower personal 44 cannot overwrite the retained enhancement");
                Assert.AreEqual(5, GuildClientProduct.Value<int>(_h.Session, "AttackBonus"));
                Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "WeaponCurrencyCost"));
            }
            _h.Deliver(GuildClientWire.Snapshot(sequence: 3, guildRevision: 9, inventoryRevision: 10, treasury: 124, unlocked: true, enhanced: true));
            Assert.AreEqual(1, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"));
            Assert.AreEqual(124, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
        }
    }
}
