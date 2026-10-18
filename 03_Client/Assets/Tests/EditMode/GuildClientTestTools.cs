using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Reflection only bridges the not-yet-implemented session.Guild boundary. No decoder, request state
    // machine or expected-value calculation lives here. The agreed surface is in tdd/api-contract.md.
    internal static class GuildClientProduct
    {
        internal static object Controller(UnityClientSession session) => Read<object>(session, "Guild");
        internal static object State(UnityClientSession session) => Read<object>(Controller(session), "State");
        internal static T Value<T>(UnityClientSession session, string name) => Read<T>(State(session), name);
        internal static bool Pending(UnityClientSession session) => Read<bool>(Controller(session), "IsPending");

        internal static T Read<T>(object owner, string name)
        {
            Assert.IsNotNull(owner, "RED unimplemented: product owner for " + name);
            PropertyInfo property = owner.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(property, "RED unimplemented: public " + owner.GetType().Name + "." + name);
            object value = property.GetValue(owner);
            if (typeof(T) == typeof(object)) Assert.IsNotNull(value, "product " + name + " owner must exist");
            else Assert.IsTrue(value is T || (value == null && default(T) == null), "product getter type: " + name);
            return (T)value;
        }

        internal static bool Request(UnityClientSession session, string name)
        {
            object owner = Controller(session);
            MethodInfo method = owner.GetType().GetMethod(name, Type.EmptyTypes);
            Assert.IsNotNull(method, "RED unimplemented: public " + owner.GetType().Name + "." + name + "()");
            Assert.AreEqual(typeof(bool), method.ReturnType, "request reports attempt, never server success");
            return (bool)Invoke(() => method.Invoke(owner, null));
        }

        internal static IDisposable ObserveGrowth(UnityClientSession session, List<GuildChangeKind> observed)
        {
            object owner = Controller(session);
            EventInfo signal = owner.GetType().GetEvent("GrowthConfirmed");
            Assert.IsNotNull(signal, "RED unimplemented: Guild.GrowthConfirmed server event (no Art API)");
            Assert.AreEqual(typeof(Action<GuildChangeKind>), signal.EventHandlerType);
            Action<GuildChangeKind> callback = observed.Add;
            signal.AddEventHandler(owner, callback);
            return new Subscription(() => signal.RemoveEventHandler(owner, callback));
        }

        // Select the actual registered handler only for its header-validation unit boundary. Normal tests
        // below enter OnRecvPacket/OnRecv. Wrong-ID input cannot reach handler 44 through normal routing.
        internal static void HandleAs(UnityClientSession session, int id, ArraySegment<byte> wire)
        {
            FieldInfo registry = typeof(UnityClientSession).GetField("_handlers", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(registry, "fixture: existing session handler registry");
            var handlers = (IDictionary)registry.GetValue(null);
            object key = Enum.ToObject(typeof(Shared.Protocol.PacketID), id);
            Assert.IsTrue(handlers.Contains(key), "RED unimplemented: registered guild handler " + id);
            object handler = handlers[key];
            MethodInfo handle = handler.GetType().GetMethod("Handle");
            Invoke(() => handle.Invoke(handler, new object[] { session, wire }));
        }

        static object Invoke(Func<object> action)
        {
            try { return action(); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        sealed class Subscription : IDisposable
        {
            readonly Action _remove;
            internal Subscription(Action remove) => _remove = remove;
            public void Dispose() => _remove();
        }
    }

    // Independent v20 feature-table wire: the complete ordinary 76-byte example is literal. Mutations
    // write named input fields only; expectations in the fixtures stay literal and do not call Read/Write.
    internal static class GuildClientWire
    {
        internal static byte[] Snapshot(long sequence = 1, uint guildRevision = 7, uint inventoryRevision = 3,
            byte kind = 0, int treasury = 40, bool unlocked = false, bool enhanced = false)
        {
            byte[] wire =
            {
                76, 0, 44, 0,
                1, 0, 0, 0, 0, 0, 0, 0, 0,
                1, 0, 0, 0, 7, 0, 0, 0, 40, 0, 0, 0,
                1, 2, 0, 7, 0, 0, 0, 3, 0, 0, 0,
                3, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                30, 0, 0, 0, 20, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0,
                0, 5, 10, 0, 200, 192, 189, 188, 32, 0, 56, 174, 220, 180,
            };
            I64(wire, 4, sequence);
            wire[12] = kind;
            U32(wire, 17, guildRevision);
            I32(wire, 21, treasury);
            U32(wire, 36, inventoryRevision);
            if (unlocked)
            {
                wire[25] = 2;
                wire[27] = 1;
                I32(wire, 46, 0);
                wire[62] = 6;
                wire[63] = 0;
            }
            if (enhanced)
            {
                wire[40] = 1;
                I32(wire, 42, 5);
                I32(wire, 50, 0);
                I32(wire, 58, 0);
                wire[63] = 6;
            }
            return wire;
        }

        internal static byte[] Result(byte action = 1, byte result = 0, uint requested = 7,
            uint guildRevision = 8, uint inventoryRevision = 3, long sequence = 2)
        {
            byte[] wire = { 26, 0, 47, 0, action, result, 7, 0, 0, 0, 8, 0, 0, 0, 3, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            U32(wire, 6, requested);
            U32(wire, 10, guildRevision);
            U32(wire, 14, inventoryRevision);
            I64(wire, 18, sequence);
            return wire;
        }

        internal static byte[] Name(byte[] wire, params byte[] name)
        {
            byte[] result = new byte[66 + name.Length];
            Buffer.BlockCopy(wire, 0, result, 0, 66);
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(), (ushort)result.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(64), (ushort)name.Length);
            Buffer.BlockCopy(name, 0, result, 66, name.Length);
            return result;
        }

        internal static void U32(byte[] wire, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(offset), value);
        internal static void I32(byte[] wire, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(offset), value);
        internal static void I64(byte[] wire, int offset, long value) => BinaryPrimitives.WriteInt64LittleEndian(wire.AsSpan(offset), value);
        internal static byte[] Utf16(string name) => Encoding.Unicode.GetBytes(name);
    }

    internal sealed class GuildClientHarness : IDisposable
    {
        readonly int _latency;
        readonly Shared.Protocol.CharacterClass? _menu;
        readonly string _notice;
        readonly InventoryMirrorProbe _inventory;
        EntryBindingFixture _views;
        internal readonly ManualConnectionQueue Queue = new ManualConnectionQueue();
        internal readonly TestSocketPair Sockets;
        internal readonly UnityClientSession Session;
        internal readonly List<Action> Delayed = new List<Action>();
        internal readonly List<float> Deadlines = new List<float>();
        internal bool Current = true;
        internal bool ThrowOnSchedule;

        internal GuildClientHarness()
        {
            Assert.IsNull(UnityClientSession.Instance, "fixture cannot replace an existing connection");
            _latency = UnityClientSession.SimulatedLatencyMs;
            _menu = ClassLoadout.SessionSelectedClass;
            MainMenuNotice.TryTake(out _notice);
            ClassLoadout.ResetServerClass();
            UnityClientSession.SimulatedLatencyMs = 0;
            _inventory = InventoryMirrorProbe.TryPublish();
            Sockets = new TestSocketPair();
            Session = new UnityClientSession(() => Current, Queue.Post,
                (action, delay) =>
                {
                    if (ThrowOnSchedule) throw new InvalidOperationException("injected send scheduling failure");
                    Delayed.Add(action);
                }, (action, delay) => Deadlines.Add(delay));
            Session.Start(Sockets.Client);
            Session.Publish();
            SessionTestTools.Handshake(Session, Queue);
        }

        internal void Ready()
        {
            _views = new EntryBindingFixture(Session);
            _views.Admit(Queue, Sockets);
            _views.Begin();
            _views.Ready();
            Assert.IsTrue(Session.CanSendGameplay, "fixture passes actual character/map/view/HP gates");
            List<byte[]> automatic = PeerFrames.Take(Sockets);
            Assert.AreEqual(1, PeerFrames.Count(automatic, 35), "existing Ready inventory query is preserved");
            Assert.IsTrue(automatic.All(frame => InventoryWire.Id(frame) == 35 || InventoryWire.Id(frame) == 43),
                "Ready cannot initiate purchases");
        }

        internal void Deliver(byte[] wire)
        {
            Session.OnRecvPacket(new ArraySegment<byte>(wire));
            Queue.Drain();
        }

        internal void Synchronize(bool unlocked = false)
        {
            Ready();
            Deliver(InventoryWire.Snapshot(3, 71, (1, 4)));
            Deliver(GuildClientWire.Snapshot(unlocked: unlocked));
            Assert.IsTrue(GuildClientProduct.Value<bool>(Session, "HasSnapshot"));
            Assert.IsTrue(GuildClientProduct.Read<bool>(GuildClientProduct.Controller(Session), "IsSynchronized"));
            PeerFrames.Take(Sockets);
        }

        internal void RunDelayed()
        {
            Action[] captured = Delayed.ToArray();
            Delayed.Clear();
            foreach (Action action in captured) action();
            Queue.Drain();
        }

        public void Dispose()
        {
            Session?.Disconnect();
            Queue.Drain();
            Session?.Cleanup();
            _views?.Dispose();
            Sockets?.Dispose();
            _inventory?.Dispose();
            ClassLoadout.ResetServerClass();
            ClassLoadout.SessionSelectedClass = _menu;
            MainMenuNotice.Set(_notice);
            UnityClientSession.SimulatedLatencyMs = _latency;
        }
    }
}
