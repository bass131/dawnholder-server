using System;
using System.Buffers.Binary;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;

namespace Dawnholder.Client.Tests
{
    public sealed class InventorySnapshotBoundaryTests
    {
        ManualConnectionQueue _queue;
        InventoryMirrorProbe _mirror;
        UnityClientSession _session;
        int _previousLatency;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance);
            _previousLatency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            _queue = new ManualConnectionQueue();
            _mirror = InventoryMirrorProbe.TryPublish().Require();
            _session = new UnityClientSession(() => true, _queue.Post);
            _session.Publish();
            SessionTestTools.Handshake(_session, _queue);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                _session?.Disconnect();
                _queue?.Drain();
            }
            finally
            {
                _session?.Cleanup();
                _mirror?.Dispose();
                UnityClientSession.SimulatedLatencyMs = _previousLatency;
            }
        }

        [Test]
        public void ExactFrameInOffsetSegment_CapturesAllSlotsBeforeBufferReuse()
        {
            byte[] receiveBuffer = Embed(Snapshot());
            _session.OnRecvPacket(new ArraySegment<byte>(receiveBuffer, 11, 204));
            Assert.IsFalse(_mirror.HasSnapshot, "state changes only on the main queue");
            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
            _queue.Drain();

            AssertBaseline();
        }

        [TestCase(203, 203)]
        [TestCase(205, 205)]
        [TestCase(76, 76)]
        [TestCase(204, 203)]
        [TestCase(204, 205)]
        [TestCase(204, 76)]
        public void WrongLengthOrHeader_PreservesEveryPreviouslyAppliedValue(int length, int headerLength)
        {
            ApplyBaseline();
            byte[] malformed = Snapshot();
            Array.Resize(ref malformed, length);
            BinaryPrimitives.WriteUInt16LittleEndian(malformed.AsSpan(), (ushort)headerLength);
            BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(4), 0x11223345u);
            BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(8), 7);

            Assert.DoesNotThrow(() => _session.OnRecvPacket(new ArraySegment<byte>(Embed(malformed), 11, length)));
            Assert.DoesNotThrow(() => _queue.Drain());
            AssertBaseline();
        }

        [TestCase(0, -1)]
        [TestCase(0, 100)]
        [TestCase(2, 1)]
        [TestCase(int.MaxValue, 1)]
        public void InvalidLastSlot_RejectsTheWholeNewerSnapshot(int itemId, int count)
        {
            ApplyBaseline();
            byte[] malformed = Snapshot();
            BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(4), 0x11223345u);
            BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(8), 7);
            // Slot 23 starts at byte 196; a valid current-catalog snapshot has 0/0 here.
            BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(196), itemId);
            BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(200), count);

            Assert.DoesNotThrow(() => _session.OnRecvPacket(new ArraySegment<byte>(Embed(malformed), 11, 204)));
            Assert.DoesNotThrow(() => _queue.Drain());
            AssertBaseline();
        }

        static byte[] Snapshot()
        {
            // Independent protocol v23 fixture: 4-byte header, revision, currency, 24 item/count pairs.
            byte[] wire = new byte[204];
            new byte[]
            {
                0xCC, 0, 0x24, 0, 0x44, 0x33, 0x22, 0x11, 0x2F, 0xCB, 0x04, 0,
                1, 0, 0, 0, 63, 0, 0, 0, 2, 0, 0, 0, 41, 0, 0, 0,
            }.CopyTo(wire, 0);
            return wire;
        }

        static byte[] Embed(byte[] wire)
        {
            byte[] buffer = new byte[wire.Length + 29];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xA5;
            wire.CopyTo(buffer, 11);
            return buffer;
        }

        void ApplyBaseline()
        {
            _session.OnRecvPacket(new ArraySegment<byte>(Snapshot()));
            _queue.Drain();
            AssertBaseline();
        }

        void AssertBaseline()
        {
            Assert.IsTrue(_mirror.HasSnapshot);
            Assert.AreEqual(0x11223344u, _mirror.Revision);
            Assert.AreEqual(314159, _mirror.Currency);
            Assert.AreEqual(ItemId.Material, _mirror.Slot(0).ItemId);
            Assert.AreEqual(63, _mirror.Slot(0).Count);
            Assert.AreEqual(ItemId.CoinPouch, _mirror.Slot(1).ItemId);
            Assert.AreEqual(41, _mirror.Slot(1).Count);
            for (int slot = 2; slot < 24; slot++)
            {
                Assert.AreEqual(ItemId.None, _mirror.Slot(slot).ItemId, $"slot {slot} item");
                Assert.AreEqual(0, _mirror.Slot(slot).Count, $"slot {slot} count");
            }
        }
    }
}
