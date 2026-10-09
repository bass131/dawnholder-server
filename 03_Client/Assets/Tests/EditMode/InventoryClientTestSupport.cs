using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Observes the PR2 inventory mirror through its agreed public surface only: a MonoBehaviour facade
    // Dawnholder.Client.State.InventoryState with `static Instance { get; private set; }` and the
    // HasSnapshot/Revision/Currency/GetSlot getters (pr2-acceptance.md design choice,
    // pr2-instance-clarification-v1.1.md). The type is found by name so this test assembly keeps
    // compiling before the product exists; each missing piece is an explicit assertion. Internal fields
    // are never read. Like the existing singleton fixtures, the component stays on an inactive object,
    // so EditMode publication is not evidence of the real bootstrap/lifecycle path.
    internal sealed class InventoryMirrorProbe : IDisposable
    {
        internal const string TypeName = "Dawnholder.Client.State.InventoryState";
        const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        readonly Type _type;
        readonly PropertyInfo _instance;
        readonly PropertyInfo _hasSnapshot;
        readonly PropertyInfo _revision;
        readonly PropertyInfo _currency;
        readonly MethodInfo _getSlot;
        GameObject _owner;
        Component _mirror;

        InventoryMirrorProbe()
        {
            _type = typeof(UnityClientSession).Assembly.GetType(TypeName);
            if (_type == null) return;
            _instance = _type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            _hasSnapshot = _type.GetProperty("HasSnapshot", PublicInstance);
            _revision = _type.GetProperty("Revision", PublicInstance);
            _currency = _type.GetProperty("Currency", PublicInstance);
            _getSlot = _type.GetMethod("GetSlot", PublicInstance, null, new[] { typeof(int) }, null);
        }

        internal bool HasSnapshot => (bool)_hasSnapshot.GetValue(_mirror);

        internal uint Revision => (uint)_revision.GetValue(_mirror);

        internal int Currency => (int)_currency.GetValue(_mirror);

        bool TypeFound => _type != null;

        bool IsMonoBehaviourFacade => TypeFound && typeof(MonoBehaviour).IsAssignableFrom(_type);

        bool HasPublishableInstance => _instance != null && _instance.PropertyType == _type &&
            _instance.GetSetMethod(true) != null;

        public void Dispose()
        {
            if (_mirror != null && ReferenceEquals(_instance.GetValue(null), _mirror))
            {
                _instance.GetSetMethod(true).Invoke(null, new object[] { null });
            }
            if (_owner != null) UnityEngine.Object.DestroyImmediate(_owner);
            _owner = null;
            _mirror = null;
        }

        // Publishes a mirror owner when the facade exists, so product callbacks find the same owner the
        // real scene would install. Absence is not asserted here; Require() reports it at observation time.
        internal static InventoryMirrorProbe TryPublish()
        {
            var probe = new InventoryMirrorProbe();
            if (!probe.IsMonoBehaviourFacade || !probe.HasPublishableInstance) return probe;
            Assert.IsNull(probe._instance.GetValue(null), "fixture must not replace an existing InventoryState owner");
            probe._owner = new GameObject("Inventory contract fixture");
            probe._owner.SetActive(false);
            probe._mirror = probe._owner.AddComponent(probe._type);
            probe._instance.GetSetMethod(true).Invoke(null, new object[] { probe._mirror });
            return probe;
        }

        internal InventoryMirrorProbe Require()
        {
            Assert.IsTrue(TypeFound, $"{TypeName} is not implemented (PR2 mirror missing)");
            Assert.IsTrue(IsMonoBehaviourFacade, $"{TypeName} must be a MonoBehaviour facade");
            Assert.IsTrue(HasPublishableInstance, "InventoryState needs `public static InventoryState Instance { get; private set; }`");
            Assert.IsTrue(IsGetter(_hasSnapshot, typeof(bool)), "InventoryState.HasSnapshot must be a public bool getter");
            Assert.IsTrue(IsGetter(_revision, typeof(uint)), "InventoryState.Revision must be a public uint getter");
            Assert.IsTrue(IsGetter(_currency, typeof(int)), "InventoryState.Currency must be a public int getter");
            bool slotReader = _getSlot != null && _getSlot.ReturnType == typeof(InventorySlot);
            Assert.IsTrue(slotReader, "InventoryState.GetSlot(int) must return Shared.GameData.InventorySlot");
            Assert.AreSame(_mirror, _instance.GetValue(null), "the observed owner is no longer InventoryState.Instance");
            return this;
        }

        internal InventorySlot Slot(int index) => (InventorySlot)_getSlot.Invoke(_mirror, new object[] { index });

        static bool IsGetter(PropertyInfo property, Type type) =>
            property != null && property.PropertyType == type && property.GetGetMethod() != null;
    }

    // A server-side inventory value as the test's stand-in server would send it. Expected mirror values
    // are these literals, never something decoded or computed by product or generated code.
    internal sealed class ServerInventory
    {
        internal ServerInventory(uint revision, int currency, params (ItemId Item, int Count)[] stacks)
        {
            Revision = revision;
            Currency = currency;
            Stacks = stacks;
        }

        internal uint Revision { get; }

        internal int Currency { get; }

        internal (ItemId Item, int Count)[] Stacks { get; }

        internal byte[] Wire()
        {
            var slots = new (int, int)[Stacks.Length];
            for (int i = 0; i < Stacks.Length; i++) slots[i] = ((int)Stacks[i].Item, Stacks[i].Count);
            return InventoryWire.Snapshot(Revision, Currency, slots);
        }

        // Every value is compared separately so a failure names the field that diverged.
        internal void AssertShownBy(InventoryMirrorProbe mirror, string requirement)
        {
            bool synced = mirror.HasSnapshot;
            Assert.IsTrue(synced, $"{requirement}: mirror must be synchronized");
            Assert.AreEqual(Revision, mirror.Revision, $"{requirement}: revision");
            Assert.AreEqual(Currency, mirror.Currency, $"{requirement}: currency");
            for (int slot = 0; slot < InventoryWire.SlotCount; slot++)
            {
                ItemId expectedItem = slot < Stacks.Length ? Stacks[slot].Item : ItemId.None;
                int expectedCount = slot < Stacks.Length ? Stacks[slot].Count : 0;
                InventorySlot shown = mirror.Slot(slot);
                Assert.AreEqual(expectedItem, shown.ItemId, $"{requirement}: slot {slot} item");
                Assert.AreEqual(expectedCount, shown.Count, $"{requirement}: slot {slot} count");
            }
        }
    }

    // Byte encoders written from the fixed PR1 wire table (pr1-acceptance.md): ushort size including the
    // 4-byte header, ushort packet id, then LittleEndian fields. Generated Write() is deliberately unused
    // so a generator or product change cannot redefine the expected bytes.
    internal static class InventoryWire
    {
        internal const int SlotCount = 8;
        internal const ushort InventoryRequestId = 35;
        internal const ushort SnapshotId = 36;
        internal const ushort ItemUseResultId = 38;
        internal const int SnapshotLength = 76;
        internal const int ItemUseResultLength = 13;

        internal static byte[] InventoryRequest => new byte[] { 0x05, 0x00, 0x23, 0x00, 0x00 };

        internal static byte[] Snapshot(uint revision, int currency, params (int ItemId, int Count)[] slots)
        {
            Assert.LessOrEqual(slots.Length, SlotCount, "fixture snapshot has at most eight slots");
            byte[] wire = Header(SnapshotLength, SnapshotId);
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(4), revision);
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(8), currency);
            for (int i = 0; i < slots.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(12 + (i * 8)), slots[i].ItemId);
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(16 + (i * 8)), slots[i].Count);
            }
            return wire;
        }

        internal static byte[] ItemUseResult(byte result, int itemId, uint revision)
        {
            byte[] wire = Header(ItemUseResultLength, ItemUseResultId);
            wire[4] = result;
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(5), itemId);
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(9), revision);
            return wire;
        }

        // A truncated or padded frame whose header agrees with its new length, as framing would deliver it.
        internal static byte[] Resized(byte[] wire, int length)
        {
            byte[] resized = new byte[length];
            Buffer.BlockCopy(wire, 0, resized, 0, Math.Min(length, wire.Length));
            BinaryPrimitives.WriteUInt16LittleEndian(resized.AsSpan(0), (ushort)length);
            return resized;
        }

        internal static byte[] WithHeaderSize(byte[] wire, ushort size)
        {
            byte[] copy = (byte[])wire.Clone();
            BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(0), size);
            return copy;
        }

        internal static ushort Id(byte[] frame) => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2));

        static byte[] Header(int length, ushort id)
        {
            byte[] wire = new byte[length];
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(0), (ushort)length);
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(2), id);
            return wire;
        }
    }

    // Reads what the client actually put on the loopback socket. TestSocketPair.ReadFrame blocks for one
    // frame and fails on EOF; these tests also need "nothing was sent" and must tolerate a closed client.
    internal static class PeerFrames
    {
        internal const int QuietMilliseconds = 300;

        internal static List<byte[]> Take(TestSocketPair sockets)
        {
            var frames = new List<byte[]>();
            var pending = new List<byte>();
            byte[] chunk = new byte[1024];
            while (sockets.Peer.Poll(QuietMilliseconds * 1000, SelectMode.SelectRead))
            {
                int received;
                try { received = sockets.Peer.Receive(chunk); }
                catch (SocketException) { break; }
                if (received == 0) break; // the client closed its socket
                for (int i = 0; i < received; i++) pending.Add(chunk[i]);
                while (pending.Count >= 4)
                {
                    int size = pending[0] | (pending[1] << 8);
                    Assert.GreaterOrEqual(size, 4, "client sent a frame shorter than its header");
                    if (pending.Count < size) break;
                    frames.Add(pending.GetRange(0, size).ToArray());
                    pending.RemoveRange(0, size);
                }
            }
            Assert.IsEmpty(pending, "client left a partial frame on the socket");
            return frames;
        }

        internal static int Count(List<byte[]> frames, ushort packetId)
        {
            int count = 0;
            foreach (byte[] frame in frames)
            {
                if (InventoryWire.Id(frame) == packetId) count++;
            }
            return count;
        }
    }

    // A published MonoBehaviour singleton on an inactive object, as ClientSessionMirrorResetTests does,
    // without the player/registry singletons that EntryBindingFixture would also install.
    internal sealed class PublishedSingleton<T> : IDisposable where T : Component
    {
        readonly GameObject _owner;

        internal PublishedSingleton()
        {
            PropertyInfo instance = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNull(instance.GetValue(null), $"fixture must not replace an existing {typeof(T).Name}");
            _owner = new GameObject(typeof(T).Name + " inventory fixture");
            _owner.SetActive(false);
            Value = _owner.AddComponent<T>();
            instance.GetSetMethod(true).Invoke(null, new object[] { Value });
        }

        internal T Value { get; }

        public void Dispose()
        {
            PropertyInfo instance = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (ReferenceEquals(instance.GetValue(null), Value)) instance.GetSetMethod(true).Invoke(null, new object[] { null });
            UnityEngine.Object.DestroyImmediate(_owner);
        }
    }

    // R4/R5 (pr2-acceptance.md; client.md "인벤토리 표시와 요청 수명"): a gameplay Ready sends one
    // C_InventoryRequest and registers one response deadline. The deadline has its own session boundary
    // (postInventoryTimeout) and never enters the send channel, where a 5 s entry would hold back
    // latency-delayed intents (opus-pr2-review #1; client.md "응답 기한은 편집기 송신 지연 FIFO와 분리").
    // Fixtures that predate PR2 and own the send channel or the peer consume exactly these by role: the query
    // by its literal bytes (run first when SimulatedLatencyMs delays intents) and the one deadline by its
    // recorded wait, which is never run. Without a socket the delayed query is identified by its latency
    // delay only and is not run, because a send without a transport closes the session.
    internal static class ReadyInventoryQuery
    {
        internal static void Consume(TestSocketPair sockets, List<Action> delayed, List<float> delays, int readyFrom,
            List<float> deadlines)
        {
            float latency = UnityClientSession.SimulatedLatencyMs / 1000f;
            bool queryDelayed = UnityClientSession.SimulatedLatencyMs > 0;
            int added = delayed.Count - readyFrom;
            Assert.AreEqual(delayed.Count, delays.Count, "fixture records the delay of every scheduled entry");
            Assert.AreEqual(queryDelayed ? 1 : 0, added, "#1 Ready puts only the latency-delayed query on the send channel");
            if (queryDelayed)
            {
                Assert.AreEqual(latency, delays[readyFrom], "R4 the Ready query is a latency-delayed intent");
                if (sockets != null) delayed[readyFrom]();
            }
            if (sockets != null)
                CollectionAssert.AreEqual(InventoryWire.InventoryRequest, sockets.ReadFrame(), "R4 Ready sends C_InventoryRequest(reserved 0)");
            Assert.AreEqual(1, deadlines.Count, "R5 Ready registers exactly one response deadline");
            Assert.Greater(deadlines[0], latency, "R5 the deadline is a response wait, not a send");
            delayed.RemoveRange(readyFrom, added);
            delays.RemoveRange(readyFrom, added);
            deadlines.Clear();
        }
    }
}
