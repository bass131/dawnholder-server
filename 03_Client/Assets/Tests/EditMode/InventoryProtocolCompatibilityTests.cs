using System;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // Consumer check of the committed Shared.dll for the server economy packets. The expected bytes
    // are the fixed wire table of the items/inventory/currency goal (01_Phases/goals/
    // 2026-10-05-items-inventory-currency/goal.md and its PR1 acceptance): LittleEndian, 4-byte header
    // (ushort size including the header, ushort id), IDs 35..38 after 34, protocol version 17.
    // They are written as literals here, never computed from the generator or the server.
    // The client handler side of S_InventorySnapshot is covered in depth by InventoryClientContractTests.
    public sealed class InventoryProtocolCompatibilityTests
    {
        UnityClientSession _session;

        [TearDown]
        public void Teardown()
        {
            _session?.Cleanup();
            _session = null;
        }

        [Test]
        public void ProtocolVersion_IsSeventeen()
        {
            Assert.AreEqual(17, ProtocolVersion.Current);
        }

        [Test]
        public void EconomyPacketIds_AppendAfterTheLastExistingId()
        {
            Assert.AreEqual(34, (ushort)PacketID.C_CheatCommand, "existing IDs must not shift");
            Assert.AreEqual(35, (ushort)PacketID.C_InventoryRequest);
            Assert.AreEqual(36, (ushort)PacketID.S_InventorySnapshot);
            Assert.AreEqual(37, (ushort)PacketID.C_ItemUse);
            Assert.AreEqual(38, (ushort)PacketID.S_ItemUseResult);
        }

        [Test]
        public void InventoryRequest_WritesFiveFixedBytes()
        {
            byte[] expected = { 0x05, 0x00, 0x23, 0x00, 0x00 };

            byte[] actual = Bytes(new C_InventoryRequest { reserved = 0 }.Write());

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ItemUse_WritesTwelveLittleEndianBytes()
        {
            byte[] expected =
            {
                0x0C, 0x00, 0x25, 0x00,
                0x02, 0x00, 0x00, 0x00,
                0x04, 0x03, 0x02, 0x01,
            };

            byte[] actual = Bytes(new C_ItemUse { itemId = 2, expectedRevision = 0x01020304u }.Write());

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ItemUseResult_ReadsAndWritesThirteenFixedBytes()
        {
            byte[] wire =
            {
                0x0D, 0x00, 0x26, 0x00,
                0x06,
                0x02, 0x00, 0x00, 0x00,
                0xD4, 0xC3, 0xB2, 0xA1,
            };

            var decoded = new S_ItemUseResult();
            decoded.Read(new ArraySegment<byte>(wire));
            byte[] encoded = Bytes(new S_ItemUseResult { result = 6, itemId = 2, revision = 0xA1B2C3D4u }.Write());

            Assert.AreEqual(6, decoded.result, "result code RevisionExhausted");
            Assert.AreEqual(2, decoded.itemId);
            Assert.AreEqual(0xA1B2C3D4u, decoded.revision);
            CollectionAssert.AreEqual(wire, encoded);
        }

        [Test]
        public void InventorySnapshot_ReadsAndWritesSeventySixFixedBytes()
        {
            byte[] wire = new byte[76];
            byte[] head =
            {
                0x4C, 0x00, 0x24, 0x00,
                0x44, 0x33, 0x22, 0x11,
                0x00, 0xCA, 0x9A, 0x3B,
                0x01, 0x00, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            };
            byte[] lastSlot = { 0x04, 0x03, 0x02, 0x01, 0x0D, 0x0C, 0x0B, 0x0A };
            Buffer.BlockCopy(head, 0, wire, 0, head.Length);
            Buffer.BlockCopy(lastSlot, 0, wire, 68, lastSlot.Length);

            var decoded = new S_InventorySnapshot();
            decoded.Read(new ArraySegment<byte>(wire));
            byte[] encoded = Bytes(new S_InventorySnapshot
            {
                revision = 0x11223344u,
                currency = 1_000_000_000,
                slot0ItemId = 1,
                slot0Count = 99,
                slot1ItemId = 2,
                slot1Count = 1,
                slot7ItemId = 0x01020304,
                slot7Count = 0x0A0B0C0D,
            }.Write());

            Assert.AreEqual(0x11223344u, decoded.revision);
            Assert.AreEqual(1_000_000_000, decoded.currency);
            Assert.AreEqual(1, decoded.slot0ItemId);
            Assert.AreEqual(99, decoded.slot0Count);
            Assert.AreEqual(2, decoded.slot1ItemId);
            Assert.AreEqual(1, decoded.slot1Count);
            Assert.AreEqual(0, decoded.slot6ItemId);
            Assert.AreEqual(0, decoded.slot6Count);
            Assert.AreEqual(0x01020304, decoded.slot7ItemId);
            Assert.AreEqual(0x0A0B0C0D, decoded.slot7Count);
            CollectionAssert.AreEqual(wire, encoded);
        }

        [Test]
        public void SharedItemData_MatchesThePlaceholderTable()
        {
            Assert.AreEqual(0, (int)ItemId.None);
            Assert.AreEqual(1, (int)ItemId.Material);
            Assert.AreEqual(2, (int)ItemId.CoinPouch);
            Assert.AreEqual(8, InventoryLimits.MaxSlots);
            Assert.AreEqual(99, InventoryLimits.MaxStack);
            Assert.AreEqual(1_000_000_000, InventoryLimits.MaxCurrency);

            byte[] resultCodes =
            {
                (byte)InventoryResult.Success, (byte)InventoryResult.Stale, (byte)InventoryResult.NotOwned,
                (byte)InventoryResult.NotUsable, (byte)InventoryResult.CurrencyCap,
                (byte)InventoryResult.InventoryFull, (byte)InventoryResult.RevisionExhausted,
            };
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5, 6 }, resultCodes);

            Assert.IsFalse(ItemCatalog.IsDefined(ItemId.None));
            Assert.IsFalse(ItemCatalog.IsDefined((ItemId)3));
            Assert.IsTrue(ItemCatalog.IsDefined(ItemId.Material));
            Assert.IsTrue(ItemCatalog.IsDefined(ItemId.CoinPouch));
            Assert.IsFalse(ItemCatalog.For(ItemId.Material).IsUsable, "material cannot be used");
            Assert.AreEqual(50, ItemCatalog.For(ItemId.CoinPouch).CurrencyOnUse);
        }

        // PR1 servers push S_InventorySnapshot on every kill. PR2 registers its handler (pr2-acceptance.md
        // R2/R3), so the current client applies the push on the main drain instead of the unknown-ID drop
        // and keeps the connection open.
        [Test]
        public void CurrentClient_AppliesAnInventoryPushWithoutClosing()
        {
            var queue = new ManualConnectionQueue();
            _session = new UnityClientSession(() => true, queue.Post);
            using InventoryMirrorProbe mirror = InventoryMirrorProbe.TryPublish();
            byte[] push = Bytes(new S_InventorySnapshot { revision = 1, currency = 10, slot0ItemId = 1, slot0Count = 1 }.Write());

            _session.OnRecvPacket(new ArraySegment<byte>(push));
            queue.Drain();

            LogAssert.NoUnexpectedReceived();
            Assert.IsFalse(_session.IsClosed);
            new ServerInventory(1, 10, (ItemId.Material, 1)).AssertShownBy(mirror.Require(), "PR1 push applied by the PR2 client");
        }

        static byte[] Bytes(ArraySegment<byte> segment)
        {
            byte[] bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);
            return bytes;
        }
    }
}
