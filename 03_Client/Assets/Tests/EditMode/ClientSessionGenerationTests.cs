using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    internal static class SessionTestTools
    {
        internal static void Set(object target, string property, object value) =>
            target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetSetMethod(true).Invoke(target, new[] { value });
        internal static object Roster(UnityClientSession session) => typeof(UnityClientSession)
            .GetProperty("RosterBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
        internal static object Call(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
        internal static void Handshake(UnityClientSession session, ManualConnectionQueue queue)
        {
            session.OnRecvPacket(new S_HandshakeResult { ok = true, serverVersion = ProtocolVersion.Current, reason = "" }.Write());
            queue.Drain();
            Assert.IsTrue(session.HandshakeOk);
        }
    }

    internal sealed class TestSocketPair : IDisposable
    {
        internal readonly Socket Client;
        internal readonly Socket Peer;
        internal TestSocketPair()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client.Connect(listener.LocalEndpoint);
                Peer = listener.AcceptSocket();
                Peer.ReceiveTimeout = 3000;
            }
            finally { listener.Stop(); }
        }
        internal byte[] ReadFrame()
        {
            byte[] header = Read(4);
            int length = BitConverter.ToUInt16(header, 0);
            Assert.GreaterOrEqual(length, 4);
            byte[] result = new byte[length];
            Buffer.BlockCopy(header, 0, result, 0, 4);
            Buffer.BlockCopy(Read(length - 4), 0, result, 4, length - 4);
            return result;
        }
        byte[] Read(int count)
        {
            byte[] bytes = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int received = Peer.Receive(bytes, offset, count - offset, SocketFlags.None);
                Assert.Greater(received, 0, "unexpected EOF");
                offset += received;
            }
            return bytes;
        }
        public void Dispose() { Client.Dispose(); Peer.Dispose(); }
    }

    public sealed class ClientSessionGenerationTests
    {
        readonly List<UnityClientSession> _sessions = new();
        int _latency;
        [SetUp] public void Setup() => _latency = UnityClientSession.SimulatedLatencyMs;
        [TearDown] public void Teardown()
        {
            foreach (var session in _sessions) session.Cleanup();
            _sessions.Clear();
            UnityClientSession.SimulatedLatencyMs = _latency;
        }
        UnityClientSession New(Func<bool> current, ManualConnectionQueue queue, Action<Action, float> delayed = null,
            Action<Action, float> deadlines = null)
        {
            var session = new UnityClientSession(current, queue.Post, delayed, deadlines);
            _sessions.Add(session);
            return session;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PacketApplication_RechecksGenerationAtDrain(bool replaceBeforeDrain)
        {
            var queue = new ManualConnectionQueue();
            bool current = true;
            var session = New(() => current, queue);
            using var views = new EntryBindingFixture(session);
            views.Begin();
            views.Ready();
            session.OnRecvPacket(new S_HandshakeResult { ok = true, serverVersion = ProtocolVersion.Current, reason = "" }.Write());
            session.OnRecvPacket(new S_Snapshot { entityId = 7, serverTick = 123 }.Write());
            int unrelated = 0;
            queue.Post(() => unrelated++);
            if (replaceBeforeDrain) current = false;
            queue.Drain();
            Assert.AreEqual(!replaceBeforeDrain, session.HandshakeOk);
            Assert.AreEqual(replaceBeforeDrain ? 0 : 123, session.LastReceivedServerTick);
            Assert.AreEqual(1, unrelated, "generation change must not clear the entire dispatcher");
        }

        [Test]
        public void NaturalClose_NotifiesDespiteClosedGate_AndCannotClearNewInstance()
        {
            var queue = new ManualConnectionQueue();
            bool oldCurrent = true;
            var old = New(() => oldCurrent, queue);
            old.Publish();
            int closes = 0, applies = 0;
            old.Closed += () => closes++;
            old.EnqueueApply(() => applies++);
            old.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 1));
            Assert.IsTrue(old.IsClosed);
            Assert.AreEqual(1, closes);
            oldCurrent = false;
            var current = New(() => true, queue);
            current.Publish();
            queue.Drain();
            Assert.AreEqual(0, applies);
            Assert.AreSame(current, UnityClientSession.Instance);
            Assert.IsFalse(current.IsClosed);
        }

        [Test]
        public void HandshakeRejection_ClosesSessionAndSendsMandatoryNotification()
        {
            var queue = new ManualConnectionQueue();
            var session = New(() => true, queue);
            int closed = 0;
            session.Closed += () => closed++;
            LogAssert.Expect(LogType.Error, "[Unity] Handshake FAILED — rejected (server version=16). Disconnecting.");
            session.OnRecvPacket(new S_HandshakeResult { ok = false, serverVersion = 16, reason = "rejected" }.Write());
            queue.Drain();
            Assert.AreEqual(1, closed);
            Assert.IsTrue(session.IsClosed);
            Assert.IsFalse(session.HandshakeOk);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeferredRoster_RechecksSessionBeforeSceneDrain(bool invalidate)
        {
            var queue = new ManualConnectionQueue();
            bool current = true;
            var session = New(() => current, queue);
            object roster = SessionTestTools.Roster(session);
            Scene scene = SceneManager.GetActiveScene();
            SessionTestTools.Call(roster, "BeginTransition", scene.name);
            int applied = 0;
            Assert.IsTrue((bool)SessionTestTools.Call(roster, "TryBuffer", "test roster", (Action)(() => applied++)));
            if (invalidate) current = false;
            SessionTestTools.Call(roster, "Drain");
            Assert.AreEqual(invalidate ? 0 : 1, applied);
        }

        [Test]
        public void RosterCloseDuringDrain_DropsRemainingCallbacks()
        {
            var queue = new ManualConnectionQueue();
            var session = New(() => true, queue);
            object roster = SessionTestTools.Roster(session);
            Scene scene = SceneManager.GetActiveScene();
            SessionTestTools.Call(roster, "BeginTransition", scene.name);
            int applied = 0;
            SessionTestTools.Call(roster, "TryBuffer", "first", (Action)(() => { applied++; session.Cleanup(); }));
            SessionTestTools.Call(roster, "TryBuffer", "second", (Action)(() => applied++));
            SessionTestTools.Call(roster, "Drain");
            Assert.AreEqual(1, applied);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DelayedIntent_OnlyCurrentLiveSessionSends(bool invalidate)
        {
            using var sockets = new TestSocketPair();
            var queue = new ManualConnectionQueue();
            var delayed = new List<Action>();
            var delays = new List<float>();
            var deadlines = new List<float>();
            bool current = true;
            var session = New(() => current, queue, (action, delay) => { delayed.Add(action); delays.Add(delay); },
                (action, seconds) => deadlines.Add(seconds));
            using var views = new EntryBindingFixture(session);
            session.Start(sockets.Client);
            UnityClientSession.SimulatedLatencyMs = 100;
            try
            {
                session.SendIntent(new C_Ping { clientTimestampMs = 10 }.Write());
                Assert.IsEmpty(delayed, "pre-handshake intent must not even be scheduled");
                SessionTestTools.Handshake(session, queue);
                session.SendIntent(new C_Ping { clientTimestampMs = 10 }.Write());
                Assert.IsEmpty(delayed, "handshake alone is not gameplay readiness");
                views.Begin();
                views.Ready();
                ReadyInventoryQuery.Consume(sockets, delayed, delays, 0, deadlines);
                session.SendIntent(new C_Ping { clientTimestampMs = 10 }.Write());
                Assert.AreEqual(1, delayed.Count);
                if (invalidate) current = false;
                delayed[0]();
                if (invalidate) Assert.IsFalse(sockets.Peer.Poll(100000, SelectMode.SelectRead));
                else Assert.AreEqual(PacketID.C_Ping, (PacketID)BitConverter.ToUInt16(sockets.ReadFrame(), 2));
            }
            finally { session.Disconnect(); queue.Drain(); }
        }

        [Test]
        public void ActivationSendsHandshakeBeforeCharacterSelection()
        {
            using var sockets = new TestSocketPair();
            var queue = new ManualConnectionQueue();
            var session = New(() => true, queue);
            try
            {
                session.Activate(sockets.Client);
                Assert.AreEqual(PacketID.C_Handshake, (PacketID)BitConverter.ToUInt16(sockets.ReadFrame(), 2));
                Assert.IsNull(UnityClientSession.Instance);
                session.Publish();
                session.SendCharacterSelect(1);
                Assert.IsFalse(sockets.Peer.Poll(100000, SelectMode.SelectRead));
                SessionTestTools.Handshake(session, queue);
                session.SendCharacterSelect(1);
                byte[] selection = sockets.ReadFrame();
                Assert.AreEqual(PacketID.C_CharacterSelect, (PacketID)BitConverter.ToUInt16(selection, 2));
                C_CharacterSelect packet = new();
                packet.Read(new ArraySegment<byte>(selection));
                Assert.AreEqual(1, packet.characterClass);
            }
            finally { session.Disconnect(); queue.Drain(); }
        }
    }
}
