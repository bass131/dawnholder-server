using System;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.Network.CharacterMenu;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.Tests
{
    // Goal 2026-11-10-code-audit-client-fixes item 7: a valid menu result received before the close is delivered once. The
    // receive thread runs OnDisconnected right after the reply, so both are queued before the main thread drains.
    public sealed class CharacterMenuFinalReplyTests
    {
        const int ListResultId = 50;
        const int CreateResultId = 52;
        static readonly EndPoint s_remote = new IPEndPoint(IPAddress.Loopback, 1);

        sealed class Probe : IDisposable
        {
            internal readonly ManualConnectionQueue Queue = new();
            internal readonly TestSocketPair Sockets = new();
            internal readonly UnityClientSession Session;
            internal bool Current = true;
            readonly CharacterClass? _previousClass = ClassLoadout.SessionSelectedClass;
            readonly string _previousNotice;

            internal Probe()
            {
                MainMenuNotice.TryTake(out _previousNotice);
                Session = new UnityClientSession(() => Current, Queue.Post, (_, __) => { }, (_, __) => { });
                Session.Start(Sockets.Client);
                SessionTestTools.Handshake(Session, Queue);
                Session.OnRecvPacket(new S_LoginResult { result = (byte)LoginResult.Accepted }.Write());
                Queue.Drain();
                Assert.IsTrue(Session.BeginCharacterMenu(), "fixture: the accepted login opens the menu");
            }

            internal CharacterMenuClient Menu => Session.CharacterMenu;

            internal void Receive(byte[] frame, int packetId) =>
                Session.ReceiveCharacterMenuResult(new ArraySegment<byte>(frame), (PacketID)packetId);

            internal bool WireIsQuiet() => !Sockets.Peer.Poll(30000, SelectMode.SelectRead);

            public void Dispose()
            {
                Current = false;
                Session.Disconnect();
                Queue.Drain();
                Session.Cleanup();
                Sockets.Dispose();
                ClassLoadout.ResetServerClass();
                ClassLoadout.SessionSelectedClass = _previousClass;
                MainMenuNotice.TryTake(out _);
                MainMenuNotice.Set(_previousNotice);
            }
        }

        static CharacterMenuClient ListRequested(Probe probe, out uint requestId, out Counter delivered)
        {
            CharacterMenuClient menu = probe.Menu;
            Assert.IsTrue(menu.TryList(), "fixture: the list request is sent");
            probe.Sockets.ReadFrame();
            requestId = menu.PendingRequestId;
            delivered = new Counter(menu);
            return menu;
        }

        sealed class Counter
        {
            internal int Value;
            internal Counter(CharacterMenuClient menu) => menu.Changed += () => Value++;
        }

        // Control for the two close-overlap cases below: the same frame delivers when no close is queued, so their failure
        // cannot come from the fixture's frame or packet id.
        [Test]
        public void ListReplyWithoutClose_IsDeliveredOnce()
        {
            using var probe = new Probe();
            CharacterMenuClient menu = ListRequested(probe, out uint requestId, out Counter delivered);

            probe.Receive(CharacterMenuWire.List(requestId), ListResultId);
            probe.Queue.Drain();

            bool replyDelivered = menu.LastReply != null;
            Assert.IsTrue(replyDelivered, "control: the reply is delivered without a close");
            Assert.AreEqual(requestId, menu.LastReply.RequestId, "control: the delivered reply is this request's");
            Assert.AreEqual(1, delivered.Value, "control: the reply is delivered exactly once");
        }

        [Test]
        public void ListReplyThenClose_BeforeTheDrain_IsDeliveredOnce()
        {
            using var probe = new Probe();
            CharacterMenuClient menu = ListRequested(probe, out uint requestId, out Counter delivered);

            probe.Receive(CharacterMenuWire.List(requestId), ListResultId);
            probe.Session.OnDisconnected(s_remote);
            probe.Queue.Drain();

            bool replyDelivered = menu.LastReply != null;
            Assert.IsTrue(replyDelivered, "item 7: the reply received before the close is delivered");
            Assert.AreEqual(requestId, menu.LastReply.RequestId, "item 7: the delivered reply is this request's");
            Assert.AreEqual(1, delivered.Value, "item 7: the reply is delivered exactly once");
            Assert.IsTrue(probe.WireIsQuiet(), "item 7: delivering the result sends nothing again");
        }

        [Test]
        public void OutcomeUnknownCreateThenClose_BeforeTheDrain_IsDeliveredOnce()
        {
            using var probe = new Probe();
            CharacterMenuClient menu = probe.Menu;
            Assert.IsTrue(menu.TryCreate(1, 0, "가나"), "fixture: the create request is sent");
            probe.Sockets.ReadFrame();
            uint requestId = menu.PendingRequestId;
            var delivered = new Counter(menu);

            probe.Receive(CharacterMenuWire.Failure(CreateResultId, requestId, (byte)CharacterMenuResult.OutcomeUnknown), CreateResultId);
            probe.Session.OnDisconnected(s_remote);
            probe.Queue.Drain();

            bool replyDelivered = menu.LastReply != null;
            Assert.IsTrue(replyDelivered, "item 7: the final OutcomeUnknown received before the close is delivered");
            Assert.AreEqual((byte)CharacterMenuResult.OutcomeUnknown, menu.LastReply.Result, "item 7: the original result is delivered");
            Assert.AreEqual(1, delivered.Value, "item 7: the result is delivered exactly once");
            Assert.IsTrue(probe.WireIsQuiet(), "item 7: delivering the result sends nothing again");
        }

        [Test]
        public void ReplyAfterCleanup_IsNotDelivered()
        {
            using var probe = new Probe();
            CharacterMenuClient menu = ListRequested(probe, out uint requestId, out Counter delivered);

            probe.Session.OnDisconnected(s_remote);
            probe.Queue.Drain();
            probe.Receive(CharacterMenuWire.List(requestId), ListResultId);
            probe.Queue.Drain();

            bool replyDelivered = menu.LastReply != null;
            Assert.IsFalse(replyDelivered, "item 7: a reply after cleanup is not delivered");
            Assert.AreEqual(0, delivered.Value, "item 7: nothing is announced after cleanup");
        }

        [Test]
        public void ReplyForAReplacedSession_IsNotDelivered()
        {
            using var probe = new Probe();
            CharacterMenuClient menu = ListRequested(probe, out uint requestId, out Counter delivered);

            probe.Current = false;
            probe.Receive(CharacterMenuWire.List(requestId), ListResultId);
            probe.Session.OnDisconnected(s_remote);
            probe.Queue.Drain();

            bool replyDelivered = menu.LastReply != null;
            Assert.IsFalse(replyDelivered, "item 7: another session's result is not delivered");
            Assert.AreEqual(0, delivered.Value, "item 7: nothing is announced for another session");
            Assert.IsTrue(probe.WireIsQuiet(), "item 7: an undelivered result sends nothing again");
        }
    }
}
