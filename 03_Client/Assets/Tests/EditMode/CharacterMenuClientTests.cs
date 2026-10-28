using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // Pure client cases remain below; Session_/Facade_ cases also enter the real receive table and main queue.
    // Menu replies and authority48 use independent contract bytes, never a product writer as the oracle.
    public sealed class CharacterMenuClientTests
    {
        [Test]
        public void PublicClientContract_IsSealedDisposable_WithRequiredReadOnlyStateAndEvent()
        {
            using var client = new MenuClientProbe();
            Type type = client.Instance.GetType();
            Assert.IsTrue(type.IsPublic && type.IsSealed);
            Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(type));
            foreach (string name in new[] { "HasPending", "PendingRequestId", "LastReply" })
            {
                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(property, name);
                Assert.IsNull(property.GetSetMethod(false), name + " has no public setter");
            }
            Assert.IsFalse(client.Pending);
            Assert.AreEqual(0u, client.PendingId);
            Assert.IsNull(client.LastReply);
            client.Require("TryList", Type.EmptyTypes);
            client.Require("TryCreate", new[] { typeof(byte), typeof(byte), typeof(string) });
            client.Require("TrySelect", new[] { typeof(string) });
            client.Require("TryReceive", new[] { CharacterMenuPacketContractTests.RequireType("CharacterMenuReply") });
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Constructor_RejectsEachNullDelegate(int missing)
        {
            Type type = CharacterMenuPacketContractTests.RequireType("CharacterMenuClient");
            ConstructorInfo constructor = MenuClientProbe.Constructor(type);
            object[] args = { (Func<bool>)(() => true), (Action<ArraySegment<byte>>)(_ => { }),
                (Action)(() => { }), (Action<string>)(_ => { }) };
            args[missing] = null;
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(args));
            Assert.IsInstanceOf<ArgumentNullException>(error.InnerException);
        }

        [Test]
        public void UnsolicitedReplyWithoutPending_CannotPublishOrSelect()
        {
            using var client = new MenuClientProbe();
            foreach (int kind in new[] { 50, 52, 54 })
                Assert.IsFalse(client.Receive(client.Reply(kind, 1)));
            AssertNoRequest(client);
            Assert.AreEqual(0, client.Changes);
        }

        [TestCase(49)]
        [TestCase(51)]
        [TestCase(53)]
        public void CanSendFalse_RejectsEveryRequestWithoutAllocatingPendingOrSending(int requestKind)
        {
            using var client = new MenuClientProbe { CanSend = false };
            Assert.IsFalse(client.Start(requestKind));
            Assert.IsFalse(client.Pending);
            Assert.AreEqual(0u, client.PendingId);
            Assert.IsNull(client.LastReply);
            Assert.IsEmpty(client.Sent);
            Assert.IsEmpty(client.Selections);
            Assert.AreEqual(0, client.Disconnects);
        }

        [TestCase(49)]
        [TestCase(51)]
        [TestCase(53)]
        public void AnyPendingKind_BlocksAllAdditionalKinds_WithoutChangingOriginalRequest(int requestKind)
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.Start(requestKind));
            uint requestId = client.PendingId;
            Assert.Greater(requestId, 0u);
            byte[] original = (byte[])client.Sent.Single().Clone();
            Assert.IsTrue(client.Pending);
            Assert.IsFalse(client.List());
            Assert.IsFalse(client.Create(3, 0, "다른이름"));
            Assert.IsFalse(client.Select(CharacterMenuWire.Ids[1]));
            Assert.AreEqual(requestId, client.PendingId);
            Assert.AreEqual(1, client.Sent.Count);
            CollectionAssert.AreEqual(original, client.Sent.Single());
            Assert.IsNull(client.LastReply);
            object reply = client.Reply(requestKind + 1, requestId);
            Assert.IsTrue(client.Receive(reply));
            Assert.IsFalse(client.Pending);
            Assert.AreSame(reply, client.LastReply);
        }

        [Test]
        public void ListCreateSelect_SendExactReservedZeroWire_WithIncreasingNonzeroIds()
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.List());
            uint listId = client.PendingId;
            Assert.Greater(listId, 0u);
            AssertRequest(client.Sent[0], listId, new byte[] { 9, 0, 49, 0 }, new byte[] { 0 });
            Assert.IsTrue(client.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.List(listId, 0))));

            const string rawName = " e\u0301 ";
            Assert.IsTrue(client.Create(3, 0, rawName));
            uint createId = client.PendingId;
            Assert.Greater(createId, listId);
            AssertRequest(client.Sent[1], createId, new byte[] { 21, 0, 51, 0 },
                new byte[] { 3, 0, 0, 8, 0, 0x20, 0, 0x65, 0, 1, 3, 0x20, 0 });
            Assert.IsTrue(client.Receive(CharacterMenuPacketContractTests.Read(
                CharacterMenuWire.Create(createId, 3, 0))));

            Assert.IsTrue(client.Select(CharacterMenuWire.Id.ToUpperInvariant()));
            uint selectId = client.PendingId;
            Assert.Greater(selectId, createId);
            byte[] select = client.Sent[2];
            CollectionAssert.AreEqual(new byte[] { 83, 0, 53, 0 }, select.Take(4).ToArray());
            Assert.AreEqual(selectId, UInt32At(select, 4));
            Assert.AreEqual(0, select[8], "reserved; no class field is present");
            CollectionAssert.AreEqual(new byte[] { 72, 0 }, select.Skip(9).Take(2).ToArray());
            string sentId = Encoding.Unicode.GetString(select, 11, 72);
            Assert.AreEqual(Guid.Parse(CharacterMenuWire.Id), Guid.Parse(sentId));
            Assert.AreEqual(83, select.Length, "exact ID request, no class or trailing bytes");
        }

        [TestCase(0, 0, "가나")]
        [TestCase(4, 0, "가나")]
        [TestCase(1, 2, "가나")]
        [TestCase(1, 255, "가나")]
        [TestCase(1, 0, null)]
        public void InvalidLocalCreate_DoesNotSendOrConsumePending(int slot, int classId, string rawName)
        {
            using var client = new MenuClientProbe();
            Assert.IsFalse(client.Create((byte)slot, (byte)classId, rawName));
            AssertNoRequest(client);
        }

        [Test]
        public void UnpairedRawNameSurrogates_AreRejectedBeforeSend()
        {
            using var client = new MenuClientProbe();
            // Keep invalid UTF-16 in runtime literals, outside custom-attribute UTF-8 serialization.
            foreach (string name in new[] { "\ud800", "\udc00", "x\ud800y" })
            {
                Assert.IsFalse(client.Create(1, 0, name));
                AssertNoRequest(client);
            }
        }

        [Test]
        public void RawName_OverThirtySixUnitsIsRejected_ButServerNamePolicyIsNotReimplemented()
        {
            using var client = new MenuClientProbe();
            Assert.IsFalse(client.Create(1, 0, new string('가', 37)));
            AssertNoRequest(client);
            foreach (string rawName in new[] { "", "a", " ", " e\u0301 ", new string('가', 36), "가\ud83d\ude00" })
            {
                Assert.IsTrue(client.Create(1, 0, rawName), "only local wire structure, not Core name policy");
                byte[] frame = client.Sent.Last();
                int byteCount = frame[11] | frame[12] << 8;
                Assert.AreEqual(rawName.Length * 2, byteCount);
                Assert.AreEqual(rawName, new UnicodeEncoding(false, false, true).GetString(frame, 13, byteCount));
                Assert.AreEqual(13 + byteCount, frame.Length);
                Assert.IsTrue(client.Receive(CharacterMenuPacketContractTests.Read(
                    CharacterMenuWire.Failure(52, client.PendingId, 1, 2))));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-guid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        [TestCase("00112233445566778899aabbccddeeff")]
        [TestCase("{00112233-4455-6677-8899-aabbccddeeff}")]
        [TestCase(" 00112233-4455-6677-8899-aabbccddeeff")]
        public void InvalidSelectId_DoesNotSend(string id)
        {
            using var client = new MenuClientProbe();
            Assert.IsFalse(client.Select(id));
            AssertNoRequest(client);
        }

        [TestCase(49)]
        [TestCase(51)]
        [TestCase(53)]
        public void WrongIdKindOrDuplicate_CannotCompletePendingOrOverwriteLastReply(int requestKind)
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.List());
            object previous = CharacterMenuPacketContractTests.Read(CharacterMenuWire.List(client.PendingId, 0));
            Assert.IsTrue(client.Receive(previous));
            Assert.IsTrue(client.Start(requestKind));
            uint requestId = client.PendingId;
            int changedBefore = client.Changes;
            object wrongId = client.Reply(requestKind + 1, requestId + 1);
            int otherKind = requestKind == 49 ? 52 : 50;
            Assert.IsFalse(client.Receive(wrongId));
            Assert.IsFalse(client.Receive(client.Reply(otherKind, requestId)));
            Assert.IsFalse(client.Receive(previous));
            Assert.IsFalse(client.Receive(null));
            Assert.IsTrue(client.Pending);
            Assert.AreEqual(requestId, client.PendingId);
            Assert.AreSame(previous, client.LastReply);
            Assert.AreEqual(changedBefore, client.Changes);
            Assert.IsEmpty(client.Selections);

            object matching = client.Reply(requestKind + 1, requestId);
            Assert.IsTrue(client.Receive(matching));
            Assert.IsFalse(client.Pending);
            Assert.AreSame(matching, client.LastReply);
            Assert.AreEqual(changedBefore + 1, client.Changes, "one matched result notification");
            int after = client.Changes;
            Assert.IsFalse(client.Receive(matching));
            Assert.AreSame(matching, client.LastReply);
            Assert.AreEqual(after, client.Changes);
            Assert.AreEqual(requestKind == 53 ? 1 : 0, client.Selections.Count);
        }

        [Test]
        public void SuccessfulCreate_MustMatchOriginalSlot()
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.Create(2, 1, "가나"));
            uint requestId = client.PendingId;
            int changes = client.Changes;
            Assert.IsFalse(client.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.Create(requestId, 1))));
            Assert.IsTrue(client.Pending);
            Assert.AreEqual(requestId, client.PendingId);
            Assert.IsNull(client.LastReply);
            Assert.AreEqual(changes, client.Changes);
            Assert.IsTrue(client.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.Create(requestId, 2))));
            Assert.IsEmpty(client.Selections);
        }

        [Test]
        public void OnlyMatchingSelectSuccess_NotPreviewOrFailure_CallsCanonicalSelectionOnce()
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.List());
            Assert.IsTrue(client.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.List(client.PendingId))));
            Assert.IsEmpty(client.Selections, "list preview is not selection authority");
            Assert.IsTrue(client.Select(CharacterMenuWire.Id));
            uint failedId = client.PendingId;
            object failed = CharacterMenuPacketContractTests.Read(CharacterMenuWire.Failure(54, failedId, 7));
            Assert.IsTrue(client.Receive(failed));
            Assert.IsEmpty(client.Selections);
            Assert.AreSame(failed, client.LastReply);
            Assert.IsTrue(client.Select(CharacterMenuWire.Id.ToUpperInvariant()));
            uint requestId = client.PendingId;
            int changes = client.Changes;
            Assert.IsFalse(client.Receive(CharacterMenuPacketContractTests.Read(
                CharacterMenuWire.Select(requestId, CharacterMenuWire.Ids[1]))));
            Assert.IsFalse(client.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.Select(failedId))));
            Assert.IsTrue(client.Pending);
            Assert.AreSame(failed, client.LastReply);
            Assert.AreEqual(changes, client.Changes);
            object accepted = CharacterMenuPacketContractTests.Read(CharacterMenuWire.Select(requestId));
            Assert.IsTrue(client.Receive(accepted));
            CollectionAssert.AreEqual(new[] { CharacterMenuWire.Id }, client.Selections);
            Assert.IsFalse(client.Receive(accepted));
            Assert.IsFalse(client.List());
            Assert.IsFalse(client.Create(1, 0, "가나"));
            Assert.IsFalse(client.Select(CharacterMenuWire.Id));
            Assert.AreEqual(3, client.Sent.Count);
            Assert.AreEqual(0, client.Disconnects, "54 success waits for existing 48/entry flow");
            Assert.AreEqual(1, client.Selections.Count);
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void OrdinaryFailure_PreservesResultAndAllowsNextExplicitRequest(int packetId)
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.Start(packetId - 1));
            object reply = CharacterMenuPacketContractTests.Read(CharacterMenuWire.Failure(packetId, client.PendingId, 11));
            Assert.IsTrue(client.Receive(reply));
            Assert.AreSame(reply, client.LastReply);
            Assert.AreEqual((byte)11, CharacterMenuPacketContractTests.Get<byte>(client.LastReply, "Result"));
            Assert.IsFalse(client.Pending);
            Assert.IsEmpty(client.Selections);
            Assert.AreEqual(0, client.Disconnects);
            Assert.IsTrue(client.List());
        }

        [TestCase(49)]
        [TestCase(51)]
        [TestCase(53)]
        public void SendException_ClosesOnceAndNeverRetriesOrAcceptsSuccess(int requestKind)
        {
            using var client = new MenuClientProbe { ThrowOnSend = true };
            Assert.IsFalse(client.Start(requestKind));
            Assert.AreEqual(1, client.Sent.Count, "one attempted send, no retry");
            uint sentId = UInt32At(client.Sent[0], 4);
            Assert.Greater(sentId, 0u);
            Assert.AreEqual(1, client.Disconnects);
            int changes = client.Changes;
            Assert.IsFalse(client.Receive(client.Reply(requestKind + 1, sentId)));
            Assert.IsNull(client.LastReply);
            Assert.IsEmpty(client.Selections);
            Assert.IsFalse(client.List());
            Assert.IsFalse(client.Create(1, 0, "가나"));
            Assert.IsFalse(client.Select(CharacterMenuWire.Id));
            client.Dispose();
            Assert.AreEqual(1, client.Sent.Count);
            Assert.AreEqual(1, client.Disconnects);
            Assert.AreEqual(changes, client.Changes);
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void OutcomeUnknown_PublishesOriginalResultBeforeDisconnect_AndNeverRetries(int packetId)
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.Start(packetId - 1));
            uint requestId = client.PendingId;
            object reply = CharacterMenuPacketContractTests.Read(CharacterMenuWire.Failure(packetId, requestId, 12));
            client.OnDisconnect = () => Assert.AreSame(reply, client.LastReply, "publish the unknown outcome before close");
            Assert.IsTrue(client.Receive(reply));
            Assert.AreSame(reply, client.LastReply);
            Assert.AreEqual((byte)12, CharacterMenuPacketContractTests.Get<byte>(client.LastReply, "Result"));
            Assert.IsFalse(client.Pending);
            Assert.AreEqual(1, client.Disconnects);
            Assert.IsFalse(client.List());
            Assert.IsFalse(client.Create(1, 0, "가나"));
            Assert.IsFalse(client.Select(CharacterMenuWire.Id));
            Assert.IsFalse(client.Receive(reply));
            Assert.AreEqual(1, client.Sent.Count);
            Assert.IsEmpty(client.Selections);
            client.Dispose();
            Assert.AreEqual(1, client.Disconnects);
        }

        [Test]
        public void PendingRequest_IsCapturedBeforeSend_AndCanCompleteSynchronously()
        {
            using var client = new MenuClientProbe();
            client.OnSend = frame =>
            {
                Assert.IsTrue(client.Pending, "pending must already exist at send boundary");
                uint sentId = UInt32At(frame, 4);
                Assert.AreEqual(sentId, client.PendingId);
                Assert.IsFalse(client.Create(2, 1, "나나"));
                Assert.IsTrue(client.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.List(sentId, 0))));
            };
            Assert.IsTrue(client.List());
            Assert.IsFalse(client.Pending);
            Assert.IsNotNull(client.LastReply);
            Assert.AreEqual(1, client.Sent.Count);
        }

        [TestCase(49)]
        [TestCase(51)]
        [TestCase(53)]
        public void LostConnectionOwnership_RejectsLateReplyAndAllSendsWithoutTouchingNewClient(int requestKind)
        {
            using var old = new MenuClientProbe();
            using var current = new MenuClientProbe();
            Assert.IsTrue(old.Start(requestKind));
            object late = old.Reply(requestKind + 1, old.PendingId);
            uint oldId = old.PendingId;
            int oldChanges = old.Changes;
            old.CanSend = false;
            Assert.IsTrue(current.List());
            uint currentId = current.PendingId;
            Assert.IsFalse(old.Receive(late));
            Assert.IsFalse(old.List());
            Assert.IsFalse(old.Create(1, 0, "가나"));
            Assert.IsFalse(old.Select(CharacterMenuWire.Id));
            Assert.AreEqual(oldId, old.PendingId);
            Assert.IsNull(old.LastReply);
            Assert.AreEqual(oldChanges, old.Changes);
            Assert.IsEmpty(old.Selections);
            Assert.IsTrue(current.Pending);
            Assert.AreEqual(currentId, current.PendingId);
            Assert.IsNull(current.LastReply);
            Assert.AreEqual(1, current.Sent.Count);
            Assert.IsTrue(current.Receive(CharacterMenuPacketContractTests.Read(CharacterMenuWire.List(currentId, 0))));
        }

        [TestCase(49)]
        [TestCase(51)]
        [TestCase(53)]
        public void Dispose_IsIdempotent_AndPreventsLateNotificationsWithoutDisconnectingGlobalOwner(int requestKind)
        {
            using var client = new MenuClientProbe();
            Assert.IsTrue(client.Start(requestKind));
            object reply = client.Reply(requestKind + 1, client.PendingId);
            client.Dispose();
            client.Dispose();
            Assert.IsFalse(client.Pending);
            int changes = client.Changes;
            Assert.IsFalse(client.Receive(reply));
            Assert.IsFalse(client.List());
            Assert.IsFalse(client.Create(1, 0, "가나"));
            Assert.IsFalse(client.Select(CharacterMenuWire.Id));
            Assert.AreEqual(changes, client.Changes);
            Assert.IsEmpty(client.Selections);
            Assert.IsNull(client.LastReply);
            Assert.AreEqual(0, client.Disconnects, "session cleanup owns transport disconnect");
            Assert.AreEqual(1, client.Sent.Count);
        }

        [Test]
        public void Session_PublicMenuGetterAndInternalBegin_HaveTheContractShape()
        {
            PropertyInfo property = RequireMenuGetter(typeof(UnityClientSession));
            Assert.IsNull(property.GetSetMethod(false), "session menu has no public setter");
            MethodInfo begin = RequireBegin();
            Assert.IsTrue(begin.IsAssembly, "BeginCharacterMenu belongs to the internal session boundary");
            Assert.AreEqual(typeof(bool), begin.ReturnType);
            using var h = new SessionMenuProbe();
            Assert.IsNull(h.Menu, "no implicit menu on construction");
        }

        [Test]
        public void Session_BeginRequiresAppliedHandshakeAndLaterAccepted_AndSendsNothingItself()
        {
            using var h = new SessionMenuProbe();
            Assert.IsFalse(h.Begin());
            h.Receive(LoginAccepted);
            h.Queue.Drain();
            Assert.IsFalse(h.Begin(), "Accepted before handshake must not be cached for later use");
            h.Handshake();
            Assert.IsFalse(h.Begin(), "handshake cannot reuse the earlier Accepted");
            h.Receive(LoginAccepted);
            Assert.IsFalse(h.Begin(), "receive thread cannot open the context before main drain");
            h.Queue.Drain();
            Assert.IsTrue(h.Begin());
            object first = h.Menu;
            Assert.IsNotNull(first);
            Assert.IsFalse(h.Begin());
            Assert.AreSame(first, h.Menu);
            Assert.IsFalse(MenuPending(first));
            h.AssertNoWire("Begin itself sends neither list nor legacy select");
        }

        [TestCase("superseded")]
        [TestCase("closed")]
        [TestCase("cleaned")]
        [TestCase("legacy-selected")]
        public void Session_BeginRejectsUnavailableOrLegacySelectedSession(string state)
        {
            using var h = new SessionMenuProbe();
            h.Handshake();
            h.Receive(LoginAccepted);
            h.Queue.Drain();
            if (state == "superseded") h.Current = false;
            else if (state == "closed") h.Session.Disconnect();
            else if (state == "cleaned") h.Session.Cleanup();
            else
            {
                h.Session.SendCharacterSelect(1);
                CollectionAssert.AreEqual(new byte[] { 5, 0, 16, 0, 1 }, h.Sockets.ReadFrame());
            }
            Assert.IsFalse(h.Begin());
            Assert.IsNull(h.Menu);
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void Session_ActualReceiveTable_QueuesAndCorrelatesEachMenuReply(int packetId)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            object menu = h.Menu;
            int changes = 0;
            SubscribeMenu(menu, () => changes++);
            StartMenuRequest(menu, packetId - 1);
            int changesAfterSend = changes;
            uint id = MenuPendingId(menu);
            byte[] sent = h.Sockets.ReadFrame();
            Assert.AreEqual(packetId - 1, sent[2]);
            Assert.AreEqual(id, UInt32At(sent, 4));
            h.Receive(MenuReply(packetId, id + 1));
            h.Receive(MenuReply(packetId == 50 ? 52 : 50, id));
            h.Queue.Drain();
            Assert.IsTrue(MenuPending(menu));
            Assert.AreEqual(id, MenuPendingId(menu));
            Assert.IsNull(MenuLastReply(menu));
            Assert.AreEqual(changesAfterSend, changes, "unmatched replies add no notification");
            byte[] matching = MenuReply(packetId, id);
            // Nonzero offset forces dispatch and the reader to honor the actual ArraySegment.
            byte[] padded = new byte[matching.Length + 9];
            Buffer.BlockCopy(matching, 0, padded, 4, matching.Length);
            h.Session.OnRecvPacket(new ArraySegment<byte>(padded, 4, matching.Length));
            Assert.IsTrue(MenuPending(menu));
            Assert.IsNull(MenuLastReply(menu), "no reader result applied on receipt");
            Assert.AreEqual(changesAfterSend, changes, "receipt alone adds no main-thread notification");
            h.Queue.Drain();
            Assert.IsFalse(MenuPending(menu));
            object reply = MenuLastReply(menu);
            Assert.IsNotNull(reply, "the registered handler must reach this session's client");
            Assert.AreEqual((ushort)packetId, CharacterMenuPacketContractTests.Get<ushort>(reply, "PacketId"));
            Assert.AreEqual(id, CharacterMenuPacketContractTests.Get<uint>(reply, "RequestId"));
            Assert.AreEqual(changesAfterSend + 1, changes, "one matched reply notification after the send baseline");
            h.Receive(matching);
            h.Queue.Drain();
            Assert.AreSame(reply, MenuLastReply(menu));
            Assert.AreEqual(changesAfterSend + 1, changes, "duplicate reply adds no notification");
            Assert.IsNull(ClassLoadout.ServerClass, "menu preview and 54 do not own class authority");
            Assert.IsFalse(h.Session.CanSendGameplay);
            h.AssertNoWire("response dispatch does not send legacy selection");
        }

        [TestCase(52)]
        [TestCase(54)]
        public void Session_ReplyPayloadMustMatchPendingSlotOrSelectedId(int packetId)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            object menu = h.Menu;
            StartMenuRequest(menu, packetId - 1);
            h.Sockets.ReadFrame();
            uint id = MenuPendingId(menu);
            h.Receive(packetId == 52 ? CharacterMenuWire.Create(id, 2) :
                CharacterMenuWire.Select(id, CharacterMenuWire.Ids[1]));
            h.Receive(LoadAuthority(0, 1));
            h.Queue.Drain();
            Assert.IsTrue(MenuPending(menu));
            Assert.IsNull(MenuLastReply(menu));
            Assert.IsNull(h.Session.CharacterLoad.Status);
            Assert.IsNull(ClassLoadout.ServerClass);
            h.Receive(MenuReply(packetId, id));
            h.Queue.Drain();
            Assert.IsFalse(MenuPending(menu));
            Assert.IsNotNull(MenuLastReply(menu));
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void Session_OrdinaryFailurePublishesOriginalResult_AndPermitsNextRequest(int packetId)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            StartMenuRequest(h.Menu, packetId - 1);
            h.Sockets.ReadFrame();
            h.Receive(CharacterMenuWire.Failure(packetId, MenuPendingId(h.Menu), 11));
            h.Queue.Drain();
            Assert.AreEqual((byte)11, CharacterMenuPacketContractTests.Get<byte>(MenuLastReply(h.Menu), "Result"));
            Assert.IsFalse(MenuPending(h.Menu));
            Assert.IsFalse(h.Session.IsClosed);
            h.Receive(LoadAuthority(0, 1));
            h.Queue.Drain();
            Assert.IsNull(h.Session.CharacterLoad.Status, "a failure never opens 48 context");
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.IsFalse(h.Session.CanSendGameplay);
            Assert.IsTrue(MenuCall(h.Menu, "TryList"));
            Assert.AreEqual(49, h.Sockets.ReadFrame()[2]);
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void Session_OutcomeUnknownIsVisibleBeforeClose_WithoutRetryOrAuthority(int packetId)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            object retained = h.Menu;
            StartMenuRequest(retained, packetId - 1);
            h.Sockets.ReadFrame();
            byte? published = null;
            byte? atClose = null;
            int closes = 0;
            SubscribeMenu(retained, () =>
            {
                object reply = MenuLastReply(retained);
                if (reply != null) published = CharacterMenuPacketContractTests.Get<byte>(reply, "Result");
            });
            h.Session.Closed += () => { closes++; atClose = published; };
            h.Receive(CharacterMenuWire.Failure(packetId, MenuPendingId(retained), 12));
            Assert.IsFalse(h.Session.IsClosed);
            h.Queue.Drain();
            Assert.AreEqual((byte)12, published);
            Assert.AreEqual((byte)12, atClose, "original Unknown result must be published before close");
            Assert.IsTrue(h.Session.IsClosed);
            Assert.AreEqual(1, closes);
            Assert.IsFalse(MenuCall(retained, "TryList"));
            Assert.IsFalse(h.Session.CanSendGameplay);
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.AreEqual(0, h.Sockets.Peer.Available, "no retry or legacy bytes after Unknown");
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void Session_CurrentMalformedMenuFrame_ClosesOnlyAtMainDrain(int packetId)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            int closes = 0;
            h.Session.Closed += () => closes++;
            // Recognized ID with a short fixed body; malformed independently of request matching.
            h.Receive(new byte[] { 4, 0, (byte)packetId, 0 });
            Assert.IsFalse(h.Session.IsClosed, "malformed handling must use the main queue");
            Assert.AreEqual(0, closes);
            h.Queue.Drain();
            Assert.IsTrue(h.Session.IsClosed, "registered strict reader failure terminates this connection");
            Assert.AreEqual(1, closes);
        }

        [TestCase(50, false)]
        [TestCase(52, false)]
        [TestCase(54, false)]
        [TestCase(50, true)]
        [TestCase(52, true)]
        [TestCase(54, true)]
        public void Session_SupersededQueuedReplyOrMalformedFrame_CannotAffectNewSession(int packetId, bool malformed)
        {
            using var old = new SessionMenuProbe();
            old.Open();
            object oldMenu = old.Menu;
            StartMenuRequest(oldMenu, packetId - 1);
            old.Sockets.ReadFrame();
            uint oldId = MenuPendingId(oldMenu);
            int oldChanges = 0;
            SubscribeMenu(oldMenu, () => oldChanges++);
            old.Receive(malformed ? new byte[] { 4, 0, (byte)packetId, 0 } : MenuReply(packetId, oldId));
            old.Current = false;
            using var current = new SessionMenuProbe();
            current.Open();
            Assert.IsTrue(MenuCall(current.Menu, "TryList"));
            current.Sockets.ReadFrame();
            uint currentId = MenuPendingId(current.Menu);
            int unrelated = 0;
            old.Queue.Post(() => unrelated++);
            old.Queue.Drain();
            Assert.AreEqual(1, unrelated, "a stale session cannot clear the shared dispatcher");
            Assert.AreEqual(0, oldChanges);
            Assert.IsNull(MenuLastReply(oldMenu));
            Assert.IsTrue(MenuPending(current.Menu));
            Assert.AreEqual(currentId, MenuPendingId(current.Menu));
            Assert.IsNull(MenuLastReply(current.Menu));
            Assert.IsFalse(current.Session.IsClosed);
            current.Receive(CharacterMenuWire.List(currentId, 0));
            current.Queue.Drain();
            Assert.IsFalse(MenuPending(current.Menu));
            Assert.IsNotNull(MenuLastReply(current.Menu));
            current.AssertNoWire("old reply cannot trigger a request in the new generation");
        }

        [Test]
        public void Session_OnlyMatched54Opens48Context_WithoutLegacyWireOrEarlyGameplay()
        {
            using var h = new SessionMenuProbe();
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            h.Open();
            object menu = h.Menu;
            Assert.IsTrue(MenuCall(menu, "TryList"));
            h.Sockets.ReadFrame();
            h.Receive(CharacterMenuWire.List(MenuPendingId(menu), 1, firstClass: 1));
            h.Receive(LoadAuthority(0, 1));
            h.Queue.Drain();
            Assert.IsNull(ClassLoadout.ServerClass, "preview is not authoritative");
            Assert.IsNull(h.Session.CharacterLoad.Status, "48 before successful selection is ignored");
            Assert.IsTrue(MenuCall(menu, "TrySelect", CharacterMenuWire.Id));
            byte[] selection = h.Sockets.ReadFrame();
            Assert.AreEqual(53, selection[2]);
            uint id = MenuPendingId(menu);
            h.Receive(CharacterMenuWire.Select(id + 1));
            h.Receive(CharacterMenuWire.Select(id, CharacterMenuWire.Ids[1]));
            h.Receive(LoadAuthority(0, 1));
            h.Queue.Drain();
            Assert.IsNull(h.Session.CharacterLoad.Status);
            Assert.IsTrue(MenuPending(menu));
            h.Session.SendCharacterSelect(0);
            h.AssertNoWire("direct legacy request is suppressed throughout menu context");
            int authorityChanges = 0;
            Action changed = () => authorityChanges++;
            ClassLoadout.AuthorityChanged += changed;
            try
            {
                h.Receive(CharacterMenuWire.Select(id));
                h.Receive(LoadAuthority(0, 1));
                Assert.IsNull(ClassLoadout.ServerClass, "neither callback runs on the receive thread");
                h.Queue.Drain();
                Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass);
                Assert.AreEqual(CharacterClass.Mage, h.Session.CharacterLoad.LoadedClass);
                Assert.AreEqual(1, authorityChanges);
                Assert.IsFalse(h.Session.Entry.IsGameplayReady);
                Assert.IsFalse(h.Session.CanSendGameplay, "54 and 48 cannot bypass the entry barrier");
                h.Receive(CharacterMenuWire.Select(id));
                h.Receive(LoadAuthority(0, 0));
                h.Receive(LoadAuthority(1, 255));
                h.Queue.Drain();
                Assert.AreEqual(CharacterLoadStatus.Success, h.Session.CharacterLoad.Status);
                Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass, "first valid 48 wins");
                Assert.AreEqual(1, authorityChanges);
                Assert.IsFalse(MenuCall(menu, "TryList"));
                h.AssertNoWire("matched 54 opens the existing context with zero C_CharacterSelect bytes");
            }
            finally { ClassLoadout.AuthorityChanged -= changed; }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Session_Matched54ThenFailed48_PreservesExistingRejectionAndClosedEntry(int status)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            var rejected = new List<CharacterLoadStatus>();
            h.Session.CharacterLoadRejected += rejected.Add;
            Assert.IsTrue(MenuCall(h.Menu, "TrySelect", CharacterMenuWire.Id));
            h.Sockets.ReadFrame();
            h.Receive(CharacterMenuWire.Select(MenuPendingId(h.Menu)));
            h.Receive(LoadAuthority((byte)status, 255));
            h.Queue.Drain();
            CollectionAssert.AreEqual(new[] { (CharacterLoadStatus)status }, rejected);
            Assert.AreEqual((CharacterLoadStatus)status, h.Session.CharacterLoad.Status);
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.IsFalse(h.Session.CanSendGameplay);
            h.AssertNoWire("failed 48 cannot fall back to legacy selection");
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void Session_CleanupDisposesExternallyRetainedClient_AndLateFramesStayLocal(int packetId)
        {
            using var h = new SessionMenuProbe();
            h.Open();
            object retained = h.Menu;
            StartMenuRequest(retained, packetId - 1);
            h.Sockets.ReadFrame();
            byte[] reply = MenuReply(packetId, MenuPendingId(retained));
            int changes = 0;
            SubscribeMenu(retained, () => changes++);
            h.Receive(reply);
            h.Session.Cleanup();
            h.Session.Cleanup();
            int afterCleanup = changes;
            Assert.IsNull(h.Menu);
            Assert.IsFalse(MenuPending(retained));
            Assert.IsFalse(MenuCall(retained, "TryList"));
            Assert.IsFalse(MenuCall(retained, "TryCreate", (byte)1, (byte)1, "가나"));
            Assert.IsFalse(MenuCall(retained, "TrySelect", CharacterMenuWire.Id));
            h.Receive(reply);
            h.Receive(new byte[] { 4, 0, (byte)packetId, 0 });
            h.Receive(LoadAuthority(0, 1));
            h.Queue.Drain();
            Assert.AreEqual(afterCleanup, changes);
            Assert.IsNull(MenuLastReply(retained));
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.IsFalse(h.Begin());
            h.AssertNoWire("disposed old client cannot send after cleanup");
        }

        [Test]
        public void Facade_PublicMenuApi_IsOptionalHostWithoutLocalClassParameter()
        {
            MethodInfo connect = RequireFacadeConnect();
            ParameterInfo host = connect.GetParameters().Single();
            Assert.IsTrue(host.IsOptional);
            Assert.IsNull(host.DefaultValue);
            RequireMenuGetter(typeof(NetworkService));
        }

        [Test]
        public void Facade_ActualAwakeSubscription_ConnectsMenuAndRequestsFirstListOnce()
        {
            using var h = new MenuFacadeProbe();
            Assert.IsNull(h.Menu);
            // Invalid process-local class proves this path does not consult the legacy class requirement.
            ClassLoadout.SessionSelectedClass = (CharacterClass)255;
            AccountLoadout.SessionAccountName = "MENU_ACCOUNT";
            UnityClientSession session = h.Connect();
            Assert.AreEqual(IPAddress.Parse("127.0.0.2"), h.Endpoint.Address);
            Assert.AreEqual(7777, h.Endpoint.Port);
            Assert.IsTrue(h.Service.IsConnected);
            Assert.IsNull(h.Menu);
            h.Accept(session);
            byte[] first = h.Sockets.Last().ReadFrame();
            Assert.AreEqual(49, first[2]);
            Assert.Greater(UInt32At(first, 4), 0u);
            object menu = h.Menu;
            Assert.AreSame(SessionMenu(session), menu);
            Assert.IsTrue(MenuPending(menu));
            Assert.AreEqual(1, h.BeginCalls);
            h.Accept(session);
            Assert.IsFalse(BeginSessionMenu(session));
            Assert.AreSame(menu, h.Menu);
            AssertNoWire(h.Sockets.Last(), "duplicate Accepted causes no list or legacy bytes");
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.IsFalse(session.CanSendGameplay);
        }

        [Test]
        public void Facade_MenuGetterAndLateAcceptedFollowCurrentLifetimeAcrossReconnect()
        {
            using var h = new MenuFacadeProbe();
            UnityClientSession old = h.Connect();
            h.Accept(old);
            h.Sockets.Last().ReadFrame();
            object retained = h.Menu;
            uint oldId = MenuPendingId(retained);
            h.Service.Disconnect();
            h.Queue.Drain();
            Assert.IsNull(h.Menu);
            Assert.IsFalse(MenuPending(retained));
            UnityClientSession current = h.Connect();
            h.Accept(current);
            h.Sockets.Last().ReadFrame();
            object currentMenu = h.Menu;
            Assert.AreNotSame(retained, currentMenu);
            uint id = MenuPendingId(currentMenu);
            old.OnRecvPacket(new ArraySegment<byte>(LoginAccepted));
            old.OnRecvPacket(new ArraySegment<byte>(CharacterMenuWire.List(oldId)));
            old.OnRecvPacket(new ArraySegment<byte>(new byte[] { 4, 0, 50, 0 }));
            old.OnRecvPacket(new ArraySegment<byte>(LoadAuthority(0, 1)));
            h.Queue.Drain();
            Assert.AreSame(currentMenu, h.Menu);
            Assert.IsTrue(MenuPending(currentMenu));
            Assert.AreEqual(id, MenuPendingId(currentMenu));
            Assert.IsNull(MenuLastReply(currentMenu));
            Assert.IsFalse(current.IsClosed);
            Assert.IsFalse(MenuCall(retained, "TryList"));
            AssertNoWire(h.Sockets.Last(), "late old session cannot send through new facade");
        }

        [Test]
        public void Facade_MenuThenLegacyThenMenu_PreservesModeAnd48Authority()
        {
            using var h = new MenuFacadeProbe();
            UnityClientSession menuSession = h.Connect();
            h.Accept(menuSession);
            h.Sockets.Last().ReadFrame();
            h.Service.Disconnect();
            h.Queue.Drain();
            UnityClientSession legacy = h.Connect(legacy: true);
            h.Accept(legacy);
            CollectionAssert.AreEqual(new byte[] { 5, 0, 16, 0, 1 }, h.Sockets.Last().ReadFrame());
            Assert.IsNull(h.Menu, "legacy login does not create a menu");
            legacy.OnRecvPacket(new ArraySegment<byte>(LoadAuthority(0, 0)));
            h.Queue.Drain();
            Assert.AreEqual(CharacterClass.Knight, ClassLoadout.ServerClass);
            Assert.IsFalse(legacy.CanSendGameplay);
            h.Service.Disconnect();
            h.Queue.Drain();
            UnityClientSession next = h.Connect();
            h.Accept(next);
            Assert.AreEqual(49, h.Sockets.Last().ReadFrame()[2]);
            Assert.IsNotNull(h.Menu);
            Assert.IsNull(ClassLoadout.ServerClass);
            AssertNoWire(h.Sockets.Last(), "menu reconnect sends no inherited legacy selection");
        }

        [Test]
        public void Facade_DestroyUnsubscribesActualMenuHandler_AndDisposesRetainedClient()
        {
            using var h = new MenuFacadeProbe();
            UnityClientSession session = h.Connect();
            h.Accept(session);
            h.Sockets.Last().ReadFrame();
            object retained = h.Menu;
            h.DestroyService();
            Assert.IsNull(MenuFacadeProbe.MenuSubscription(h.Owner), "OnDestroy must detach the facade handler");
            Assert.IsNull(h.Owner.CurrentSession);
            Assert.IsFalse(MenuPending(retained));
            Assert.IsFalse(MenuCall(retained, "TryList"));
            session.OnRecvPacket(new ArraySegment<byte>(LoginAccepted));
            h.Queue.Drain();
            Assert.IsNull(NetworkService.Instance);
        }

        // Reflection is local to each new test's call path: missing session/facade API is an assertion RED,
        // never a fixture-wide setup failure or a compile error that hides the existing pure client cases.
        static readonly byte[] LoginAccepted = { 5, 0, 42, 0, 0 };
        static readonly BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static PropertyInfo RequireMenuGetter(Type owner)
        {
            PropertyInfo property = owner.GetProperty("CharacterMenu", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, "session-v1 missing public CharacterMenu getter: " + owner.Name);
            Assert.IsTrue(property.GetMethod != null && property.GetMethod.IsPublic);
            Assert.AreEqual(CharacterMenuPacketContractTests.RequireType("CharacterMenuClient"), property.PropertyType);
            Assert.IsNull(property.GetSetMethod(false));
            return property;
        }

        static MethodInfo RequireBegin()
        {
            MethodInfo method = typeof(UnityClientSession).GetMethod("BeginCharacterMenu", InstanceMembers,
                null, Type.EmptyTypes, null);
            Assert.IsNotNull(method, "session-v1 missing internal bool BeginCharacterMenu()");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            Assert.IsTrue(method.IsAssembly);
            return method;
        }

        static MethodInfo RequireFacadeConnect()
        {
            MethodInfo method = typeof(NetworkService).GetMethod("ConnectCharacterMenu", BindingFlags.Public |
                BindingFlags.Instance, null, new[] { typeof(string) }, null);
            Assert.IsNotNull(method, "session-v1 missing public NetworkService.ConnectCharacterMenu(string)");
            Assert.AreEqual(typeof(void), method.ReturnType);
            return method;
        }

        static object SessionMenu(UnityClientSession session) => RequireMenuGetter(typeof(UnityClientSession)).GetValue(session);
        static bool BeginSessionMenu(UnityClientSession session) => (bool)RequireBegin().Invoke(session, null);
        static bool MenuPending(object menu) => CharacterMenuPacketContractTests.Get<bool>(menu, "HasPending");
        static uint MenuPendingId(object menu) => CharacterMenuPacketContractTests.Get<uint>(menu, "PendingRequestId");
        static object MenuLastReply(object menu) => CharacterMenuPacketContractTests.Get<object>(menu, "LastReply");
        static bool MenuCall(object menu, string method, params object[] args) =>
            (bool)menu.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance).Invoke(menu, args);
        static void SubscribeMenu(object menu, Action handler) => menu.GetType().GetEvent("Changed").AddEventHandler(menu, handler);
        static void StartMenuRequest(object menu, int kind)
        {
            bool sent = kind == 49 ? MenuCall(menu, "TryList") : kind == 51 ?
                MenuCall(menu, "TryCreate", (byte)1, (byte)1, "가나") :
                MenuCall(menu, "TrySelect", CharacterMenuWire.Id.ToUpperInvariant());
            Assert.IsTrue(sent, "session-bound client request " + kind);
        }
        static byte[] MenuReply(int kind, uint id) => kind == 50 ? CharacterMenuWire.List(id) :
            kind == 52 ? CharacterMenuWire.Create(id) : CharacterMenuWire.Select(id);
        static byte[] LoadAuthority(byte result, byte characterClass) => new byte[] { 6, 0, 48, 0, result, characterClass };
        static void AssertNoWire(TestSocketPair sockets, string reason) =>
            Assert.IsFalse(sockets.Peer.Poll(30000, SelectMode.SelectRead), reason);

        sealed class SessionMenuProbe : IDisposable
        {
            internal readonly ManualConnectionQueue Queue = new();
            internal readonly TestSocketPair Sockets;
            internal readonly UnityClientSession Session;
            internal bool Current = true;
            readonly CharacterClass? _previousIntent;
            readonly string _previousNotice;

            internal SessionMenuProbe()
            {
                RequireMenuGetter(typeof(UnityClientSession));
                RequireBegin();
                _previousIntent = ClassLoadout.SessionSelectedClass;
                MainMenuNotice.TryTake(out _previousNotice);
                Sockets = new TestSocketPair();
                Session = new UnityClientSession(() => Current, Queue.Post, (_, __) => { }, (_, __) => { });
                Session.Start(Sockets.Client);
            }
            internal object Menu => SessionMenu(Session);
            internal bool Begin() => BeginSessionMenu(Session);
            internal void Receive(byte[] frame) => Session.OnRecvPacket(new ArraySegment<byte>(frame));
            internal void Handshake() => SessionTestTools.Handshake(Session, Queue);
            internal void Open()
            {
                Handshake();
                Receive(LoginAccepted);
                Queue.Drain();
                Assert.IsTrue(Begin());
                Assert.IsNotNull(Menu);
                AssertNoWire("opening the session does not request the first list itself");
            }
            internal void AssertNoWire(string reason) => CharacterMenuClientTests.AssertNoWire(Sockets, reason);
            public void Dispose()
            {
                Current = false;
                Session.Disconnect();
                Queue.Drain();
                Session.Cleanup();
                Sockets.Dispose();
                ClassLoadout.ResetServerClass();
                ClassLoadout.SessionSelectedClass = _previousIntent;
                MainMenuNotice.TryTake(out _);
                MainMenuNotice.Set(_previousNotice);
            }
        }

        // Awake's real event delegate is retained when replacing only the connection lifetime with a controlled
        // connector/queue. The real facade Connect/handler/getter/OnDestroy and real sessions still run; this does
        // not exercise Connector.BeginConnect or NetworkService.CreateSession's default Unity dispatcher.
        sealed class MenuFacadeProbe : IDisposable
        {
            internal readonly ManualConnectionQueue Queue = new();
            internal readonly List<TestSocketPair> Sockets = new();
            internal readonly ClientConnectionLifetime Owner;
            internal readonly NetworkService Service;
            internal IPEndPoint Endpoint;
            internal int BeginCalls;
            readonly GameObject _object;
            readonly string _previousAccount;
            readonly CharacterClass? _previousIntent;
            readonly string _previousNotice;
            bool _destroyed;

            internal MenuFacadeProbe()
            {
                RequireFacadeConnect();
                RequireMenuGetter(typeof(NetworkService));
                RequireMenuGetter(typeof(UnityClientSession));
                RequireBegin();
                Assert.IsNull(NetworkService.Instance, "facade probe requires no existing service owner");
                _previousAccount = AccountLoadout.SessionAccountName;
                _previousIntent = ClassLoadout.SessionSelectedClass;
                MainMenuNotice.TryTake(out _previousNotice);
                _object = new GameObject("character menu facade contract");
                try
                {
                    // Keep the component inactive so EditMode cannot also deliver automatic lifecycle callbacks.
                    _object.SetActive(false);
                    Service = _object.AddComponent<NetworkService>();
                    FieldInfo field = typeof(NetworkService).GetField("_lifetime", InstanceMembers);
                    if (field.GetValue(Service) == null) SessionTestTools.Call(Service, "Awake");
                    var original = (ClientConnectionLifetime)field.GetValue(Service);
                    Action<IClientConnectionSession> subscription = MenuSubscription(original);
                    Assert.IsNotNull(subscription, "actual Awake must subscribe CharacterMenuLoginAccepted");
                    Assert.IsTrue(subscription.GetInvocationList().All(handler => ReferenceEquals(handler.Target, Service)),
                        "the subscription must belong to the actual facade instance");
                    Owner = new ClientConnectionLifetime(endpoint =>
                    {
                        BeginCalls++;
                        Endpoint = endpoint;
                        var sockets = new TestSocketPair();
                        Sockets.Add(sockets);
                        return new ConnectedMenuAttempt(sockets.Client);
                    }, Queue.Post, current => new UnityClientSession(current, Queue.Post, (_, __) => { }, (_, __) => { }),
                        ClassLoadout.ResetServerClass);
                    Owner.CharacterMenuLoginAccepted += subscription;
                    original.Dispose();
                    field.SetValue(Service, Owner);
                    AccountLoadout.SessionAccountName = "menu_account";
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
            internal object Menu => RequireMenuGetter(typeof(NetworkService)).GetValue(Service);
            internal static Action<IClientConnectionSession> MenuSubscription(ClientConnectionLifetime owner) =>
                (Action<IClientConnectionSession>)typeof(ClientConnectionLifetime)
                    .GetField("CharacterMenuLoginAccepted", InstanceMembers).GetValue(owner);
            internal UnityClientSession Connect(bool legacy = false)
            {
                if (legacy) Service.Connect("127.0.0.2", 1);
                else RequireFacadeConnect().Invoke(Service, new object[] { "127.0.0.2" });
                Queue.WaitThenDrain();
                Assert.IsInstanceOf<UnityClientSession>(Owner.CurrentSession);
                var session = (UnityClientSession)Owner.CurrentSession;
                Assert.AreEqual(7, Sockets.Last().ReadFrame()[2], "real Activate sends the handshake first");
                SessionTestTools.Handshake(session, Queue);
                byte[] login = Sockets.Last().ReadFrame();
                Assert.AreEqual(41, login[2]);
                Assert.AreEqual("menu_account", Encoding.Unicode.GetString(login, 6, login[4] | login[5] << 8));
                return session;
            }
            internal void Accept(UnityClientSession session)
            {
                session.OnRecvPacket(new ArraySegment<byte>(LoginAccepted));
                Queue.Drain();
            }
            internal void DestroyService()
            {
                if (_destroyed) return;
                _destroyed = true;
                try
                {
                    // This never-active component needs the real product teardown before its Unity object is removed.
                    if (Service != null) SessionTestTools.Call(Service, "OnDestroy");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(_object);
                    Queue.Drain();
                }
            }
            public void Dispose()
            {
                try { DestroyService(); }
                finally
                {
                    try
                    {
                        Owner?.Dispose();
                        Queue.Drain();
                    }
                    finally
                    {
                        foreach (TestSocketPair sockets in Sockets) sockets.Dispose();
                        AccountLoadout.SessionAccountName = _previousAccount;
                        ClassLoadout.ResetServerClass();
                        ClassLoadout.SessionSelectedClass = _previousIntent;
                        MainMenuNotice.TryTake(out _);
                        MainMenuNotice.Set(_previousNotice);
                    }
                }
            }
        }

        sealed class ConnectedMenuAttempt : IConnectionAttempt
        {
            Socket _socket;
            internal ConnectedMenuAttempt(Socket socket) => _socket = socket;
            public Task<ConnectionOutcome> Completion => Task.FromResult(new ConnectionOutcome(ConnectionStatus.Success));
            public bool TryTakeConnectedSocket(out Socket socket)
            {
                socket = _socket;
                _socket = null;
                return socket != null;
            }
            public void Cancel() { _socket?.Dispose(); _socket = null; }
            public void Dispose() => Cancel();
        }

        static void AssertNoRequest(MenuClientProbe client)
        {
            Assert.IsEmpty(client.Sent);
            Assert.IsFalse(client.Pending);
            Assert.AreEqual(0u, client.PendingId);
            Assert.IsNull(client.LastReply);
            Assert.AreEqual(0, client.Disconnects);
            Assert.IsEmpty(client.Selections);
        }

        static void AssertRequest(byte[] actual, uint id, byte[] header, byte[] tail)
        {
            byte[] expected = header.Concat(new[] { (byte)id, (byte)(id >> 8), (byte)(id >> 16), (byte)(id >> 24) })
                .Concat(tail).ToArray();
            CollectionAssert.AreEqual(expected, actual, "independent request layout");
        }

        static uint UInt32At(byte[] frame, int offset) => (uint)(frame[offset] | frame[offset + 1] << 8 |
            frame[offset + 2] << 16 | frame[offset + 3] << 24);

        sealed class MenuClientProbe : IDisposable
        {
            internal readonly object Instance;
            internal readonly List<byte[]> Sent = new();
            internal readonly List<string> Selections = new();
            internal bool CanSend = true;
            internal bool ThrowOnSend;
            internal int Changes;
            internal int Disconnects;
            internal Action<byte[]> OnSend;
            internal Action OnDisconnect;

            internal MenuClientProbe()
            {
                Type type = CharacterMenuPacketContractTests.RequireType("CharacterMenuClient");
                Instance = Constructor(type).Invoke(new object[]
                {
                    (Func<bool>)(() => CanSend), (Action<ArraySegment<byte>>)(frame =>
                    {
                        byte[] copy = frame.ToArray();
                        Sent.Add(copy);
                        OnSend?.Invoke(copy);
                        if (ThrowOnSend) throw new InvalidOperationException("injected send failure");
                    }),
                    (Action)(() => { Disconnects++; OnDisconnect?.Invoke(); }),
                    (Action<string>)Selections.Add,
                });
                EventInfo changed = type.GetEvent("Changed", BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(changed, "v1 Changed event is absent");
                Assert.AreEqual(typeof(Action), changed.EventHandlerType);
                changed.AddEventHandler(Instance, (Action)(() => Changes++));
            }

            internal bool Pending => CharacterMenuPacketContractTests.Get<bool>(Instance, "HasPending");
            internal uint PendingId => CharacterMenuPacketContractTests.Get<uint>(Instance, "PendingRequestId");
            internal object LastReply => CharacterMenuPacketContractTests.Get<object>(Instance, "LastReply");

            internal static ConstructorInfo Constructor(Type type)
            {
                ConstructorInfo constructor = type.GetConstructor(new[] { typeof(Func<bool>),
                    typeof(Action<ArraySegment<byte>>), typeof(Action), typeof(Action<string>) });
                Assert.IsNotNull(constructor, "v1 delegate constructor is absent");
                return constructor;
            }

            internal MethodInfo Require(string name, Type[] parameters)
            {
                MethodInfo method = Instance.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance,
                    null, parameters, null);
                Assert.IsNotNull(method, "v1 required method is absent: " + name);
                Assert.AreEqual(typeof(bool), method.ReturnType);
                return method;
            }

            internal bool List() => (bool)Require("TryList", Type.EmptyTypes).Invoke(Instance, null);
            internal bool Create(byte slot, byte classId, string name) => (bool)Require("TryCreate",
                new[] { typeof(byte), typeof(byte), typeof(string) }).Invoke(Instance, new object[] { slot, classId, name });
            internal bool Select(string id) => (bool)Require("TrySelect", new[] { typeof(string) })
                .Invoke(Instance, new object[] { id });
            internal bool Receive(object reply) => (bool)Require("TryReceive",
                new[] { CharacterMenuPacketContractTests.RequireType("CharacterMenuReply") })
                .Invoke(Instance, new[] { reply });
            internal bool Start(int kind) => kind == 49 ? List() : kind == 51 ? Create(1, 1, "가나") : Select(CharacterMenuWire.Id);
            internal object Reply(int kind, uint id) => CharacterMenuPacketContractTests.Read(
                kind == 50 ? CharacterMenuWire.List(id) : kind == 52 ? CharacterMenuWire.Create(id) : CharacterMenuWire.Select(id));
            public void Dispose()
            {
                Assert.IsInstanceOf<IDisposable>(Instance, "v1 client is IDisposable");
                ((IDisposable)Instance).Dispose();
            }
        }
    }
}
