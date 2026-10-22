using System;
using System.Linq;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // The committed Shared/ClientNet consumers see raw v21 frames in one receive chunk.
    // Authority belongs to 48 and the current connection; guild pushes cannot grant gameplay readiness.
    public sealed class GuildAdmissionPacketCompatibilityTests
    {
        UnityClientSession _session;
        ManualConnectionQueue _queue;
        TestSocketPair _sockets;
        CharacterClass? _menuIntent;
        int _latency;
        bool _current;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance);
            _menuIntent = ClassLoadout.SessionSelectedClass;
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            ClassLoadout.ResetServerClass();
            _current = true;
            _queue = new ManualConnectionQueue();
            _sockets = new TestSocketPair();
            _session = new UnityClientSession(() => _current, _queue.Post);
            _session.Start(_sockets.Client);
            _session.Publish();
            SessionTestTools.Handshake(_session, _queue);
        }

        [TearDown]
        public void Teardown()
        {
            _session?.Disconnect();
            _queue?.Drain();
            _session?.Cleanup();
            _sockets?.Dispose();
            ClassLoadout.ResetServerClass();
            ClassLoadout.SessionSelectedClass = _menuIntent;
            UnityClientSession.SimulatedLatencyMs = _latency;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CoalescedAdmissionAndGuildFrames_ApplyAuthorityOnlyForTheCurrentConnection(bool replaceBeforeDrain)
        {
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            _session.SendCharacterSelect(0);
            CollectionAssert.AreEqual(new byte[] { 5, 0, 16, 0, 0 }, _sockets.ReadFrame());
            byte[] admission = { 6, 0, 48, 0, 0, 1 };
            byte[] snapshot =
            {
                76, 0, 44, 0,
                8, 7, 6, 5, 4, 3, 2, 1,
                4, 1, 0, 0, 0,
                212, 195, 178, 161,
                0, 202, 154, 59,
                2, 2, 1,
                7, 0, 0, 0, 3, 0, 0, 0,
                68, 51, 34, 17,
                1, 1, 5, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                1, 0, 0, 0, 0, 0, 0, 0,
                6, 6, 10, 0,
                200, 192, 189, 188, 32, 0, 56, 174, 220, 180,
            };
            byte[] result =
            {
                26, 0, 47, 0, 2, 1, 4, 3, 2, 1,
                212, 195, 178, 161, 68, 51, 34, 17, 8, 7, 6, 5, 4, 3, 2, 1,
            };
            Assert.AreEqual(21, ProtocolVersion.Current);
            Assert.AreEqual(76, snapshot.Length);
            byte[] chunk = admission.Concat(snapshot).Concat(result).ToArray();
            object guild = GuildClientProduct.State(_session);

            int consumed = _session.OnRecv(new ArraySegment<byte>(chunk));
            Assert.AreEqual(108, consumed);
            Assert.IsNull(_session.CharacterLoad.Status);
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.IsFalse(_session.CanSendGameplay);
            Assert.IsFalse(GuildClientProduct.Read<bool>(guild, "HasSnapshot"));
            if (replaceBeforeDrain) _current = false;
            _queue.Drain();

            Assert.IsFalse(_session.IsClosed);
            Assert.IsFalse(_session.CanSendGameplay, "class authority still requires actual map/view readiness");
            if (replaceBeforeDrain)
            {
                Assert.IsNull(_session.CharacterLoad.Status);
                Assert.IsNull(ClassLoadout.ServerClass);
                Assert.IsFalse(GuildClientProduct.Read<bool>(guild, "HasSnapshot"));
            }
            else
            {
                Assert.AreEqual(CharacterLoadStatus.Success, _session.CharacterLoad.Status);
                Assert.AreEqual(CharacterClass.Mage, _session.CharacterLoad.LoadedClass);
                Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass);
                Assert.AreEqual((int)CharacterClass.Knight, ClassLoadout.GetMenuSelectedClassValue(-1));
                Assert.IsTrue(GuildClientProduct.Read<bool>(guild, "HasSnapshot"));
                Assert.AreEqual(0x0102030405060708L, GuildClientProduct.Read<long>(guild, "SnapshotSequence"));
            }
            Assert.IsFalse(GuildClientProduct.Pending(_session));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
