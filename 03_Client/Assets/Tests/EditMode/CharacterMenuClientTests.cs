using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // Pure main-thread client contract only. Actual session/handler/54->48 entry is a later stage.
    // Replies enter the real reader from independent v1 bytes; no product packet writer is used.
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
