using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dawnholder.Client.Network;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // The v1 wire document owns these bytes. No generated packet or product writer builds an oracle.
    // This fixture covers only the pure reader/reply contract, not handler registration or session entry.
    public sealed class CharacterMenuPacketContractTests
    {
        const string MenuNamespace = "Dawnholder.Client.Network.CharacterMenu.";

        [Test]
        public void ReplyAndCharacter_ExposeSealedReadOnlyContract()
        {
            Type replyType = RequireType("CharacterMenuReply");
            Type characterType = RequireType("CharacterMenuCharacter");
            Assert.IsTrue(replyType.IsPublic && replyType.IsSealed);
            Assert.IsTrue(characterType.IsPublic && characterType.IsSealed);
            AssertGetter(replyType, "PacketId", typeof(ushort));
            AssertGetter(replyType, "RequestId", typeof(uint));
            AssertGetter(replyType, "Result", typeof(byte));
            AssertGetter(replyType, "InvalidField", typeof(byte));
            AssertGetter(replyType, "OccupiedMask", typeof(byte));
            AssertGetter(replyType, "Slots", typeof(IReadOnlyList<>).MakeGenericType(characterType));
            AssertGetter(replyType, "Character", characterType);
            AssertGetter(replyType, "SelectedCharacterIdD", typeof(string));
            AssertGetter(characterType, "Slot", typeof(byte));
            AssertGetter(characterType, "CharacterIdD", typeof(string));
            AssertGetter(characterType, "Name", typeof(string));
            AssertGetter(characterType, "ClassId", typeof(byte));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(7)]
        public void ListLiteral_ZeroOneOrThreeOccupants_PreservesSlotAndKnightZero(int mask)
        {
            object reply = Read(CharacterMenuWire.List(mask: (byte)mask));
            AssertCommon(reply, 50, 0x04030201, 0, 0);
            Assert.AreEqual((byte)mask, Get<byte>(reply, "OccupiedMask"));
            object[] slots = Slots(reply);
            Assert.AreEqual(3, slots.Length);
            for (int index = 0; index < 3; index++)
            {
                if ((mask & (1 << index)) == 0)
                    Assert.IsNull(slots[index], "unoccupied slot " + (index + 1));
                else
                    AssertCharacter(slots[index], (byte)(index + 1), CharacterMenuWire.Ids[index],
                        "가나", index == 1 ? (byte)0 : (byte)1);
            }
            Assert.IsNull(Get<object>(reply, "Character"));
            Assert.AreEqual(string.Empty, Get<string>(reply, "SelectedCharacterIdD"));
        }

        [Test]
        public void EmptyListLiteral_HasExactTwentySixBytesAndByteOrder()
        {
            byte[] frame = CharacterMenuWire.List(mask: 0);
            CollectionAssert.AreEqual(new byte[]
            {
                0x1a, 0x00, 0x32, 0x00, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00,
            }, frame);
            AssertCommon(Read(frame), 50, 0x04030201, 0, 0);
        }

        [Test]
        public void CreateLiteral_UsesFixedOffsets_AndPreservesNameWithoutNormalization()
        {
            byte[] frame = CharacterMenuWire.Create(slot: 3, classId: 0, name: "e\u0301");
            Assert.AreEqual(92, frame.Length);
            CollectionAssert.AreEqual(new byte[] { 92, 0, 52, 0, 1, 2, 3, 4, 0, 0, 3, 72, 0 },
                frame.Take(13).ToArray());
            CollectionAssert.AreEqual(new byte[] { 4, 0, 0x65, 0, 1, 3, 0 }, frame.Skip(85).ToArray());
            object reply = Read(frame);
            AssertCommon(reply, 52, 0x04030201, 0, 0);
            AssertCharacter(Get<object>(reply, "Character"), 3, CharacterMenuWire.Id, "e\u0301", 0);
            Assert.IsEmpty(Slots(reply));
            Assert.AreEqual((byte)0, Get<byte>(reply, "OccupiedMask"));
            Assert.AreEqual(string.Empty, Get<string>(reply, "SelectedCharacterIdD"));
        }

        [Test]
        public void SelectLiteral_UsesEightyFourBytesAndCanonicalId()
        {
            byte[] frame = CharacterMenuWire.Select();
            Assert.AreEqual(84, frame.Length);
            CollectionAssert.AreEqual(new byte[] { 84, 0, 54, 0, 1, 2, 3, 4, 0, 0, 72, 0 },
                frame.Take(12).ToArray());
            object reply = Read(frame);
            AssertCommon(reply, 54, 0x04030201, 0, 0);
            Assert.AreEqual(CharacterMenuWire.Id, Get<string>(reply, "SelectedCharacterIdD"));
            Assert.IsNull(Get<object>(reply, "Character"));
            Assert.IsEmpty(Slots(reply));
        }

        [TestCase(50, 314)]
        [TestCase(52, 112)]
        [TestCase(54, 84)]
        public void ExactMaximumLiteral_IsAccepted_AndLargerFrameIsRejected(int packetId, int maximum)
        {
            byte[] frame = CharacterMenuWire.Valid(packetId, "가나다라마바사아자차카타");
            Assert.AreEqual(maximum, frame.Length, "documented maximum, not a measured product formula");
            Read(frame);
            byte[] larger = new byte[maximum + 1];
            Array.Copy(frame, larger, frame.Length);
            CharacterMenuWire.Size(larger);
            Reject(new ArraySegment<byte>(larger));
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void SegmentOffsetAndCount_AreTheOnlyReadableFrameBounds(int packetId)
        {
            byte[] frame = CharacterMenuWire.Valid(packetId);
            byte[] padded = Enumerable.Repeat((byte)0xff, frame.Length + 14).ToArray();
            Array.Copy(frame, 0, padded, 5, frame.Length);
            object reply = Read(new ArraySegment<byte>(padded, 5, frame.Length));
            AssertCommon(reply, (ushort)packetId, 0x04030201, 0, 0);
            Reject(new ArraySegment<byte>(padded, 5, frame.Length - 1));
            Reject(new ArraySegment<byte>(padded, 4, frame.Length));
            Reject(new ArraySegment<byte>(padded, 5, frame.Length + 1));
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void EveryTruncation_AndTrailingData_AreRejectedWithoutException(int packetId)
        {
            byte[] frame = CharacterMenuWire.Valid(packetId);
            for (int count = 0; count < frame.Length; count++)
                Reject(new ArraySegment<byte>(frame, 0, count), "truncated count=" + count);
            byte[] trailing = frame.Concat(new byte[] { 0x7f }).ToArray();
            Reject(new ArraySegment<byte>(trailing), "unchanged header");
            CharacterMenuWire.Size(trailing);
            Reject(new ArraySegment<byte>(trailing), "header includes trailing byte");
        }

        [Test]
        public void DefaultSegment_IsRejectedWithNullReply()
        {
            Reject(default);
        }

        [TestCaseSource(nameof(MalformedFrames))]
        public void MalformedLiteral_IsRejectedWithNullReply(byte[] frame)
        {
            Reject(new ArraySegment<byte>(frame));
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void EveryDefinedFailure_PreservesResultAndBlankMeaning(int packetId)
        {
            for (byte result = 1; result <= 16; result++)
            {
                byte field = result == 1 ? (byte)2 : (byte)0;
                object reply = Read(CharacterMenuWire.Failure(packetId, 9, result, field));
                AssertCommon(reply, (ushort)packetId, 9, result, field);
                Assert.AreEqual((byte)0, Get<byte>(reply, "OccupiedMask"));
                object[] slots = Slots(reply);
                Assert.AreEqual(packetId == 50 ? 3 : 0, slots.Length);
                Assert.IsTrue(slots.All(slot => slot == null));
                Assert.IsNull(Get<object>(reply, "Character"));
                Assert.AreEqual(string.Empty, Get<string>(reply, "SelectedCharacterIdD"));
            }
        }

        [TestCase(50)]
        [TestCase(52)]
        [TestCase(54)]
        public void InvalidInput_AllPublicFieldsIncludingNone_ArePreserved(int packetId)
        {
            for (byte field = 0; field <= 4; field++)
            {
                object reply = Read(CharacterMenuWire.Failure(packetId, 7, 1, field));
                AssertCommon(reply, (ushort)packetId, 7, 1, field);
            }
        }

        [Test]
        public void Reply_DoesNotExposeMutableSlotArray_OrRetainSourceBuffer()
        {
            byte[] frame = CharacterMenuWire.List(mask: 2);
            object reply = Read(frame);
            object slots = Get<object>(reply, "Slots");
            Assert.IsFalse(slots.GetType().IsArray, "no externally mutable array");
            if (slots is IList mutable)
            {
                Assert.Throws<NotSupportedException>(() => mutable[1] = null);
                Assert.Throws<NotSupportedException>(() => mutable.Clear());
            }
            AssertSlotsHaveNoMutableGenericSurface(slots);
            Array.Clear(frame, 0, frame.Length);
            AssertCharacter(Slots(reply)[1], 2, CharacterMenuWire.Ids[1], "가나", 0);
            Assert.AreEqual((byte)2, Get<byte>(reply, "OccupiedMask"));
        }

        static void AssertSlotsHaveNoMutableGenericSurface(object slots)
        {
            Type character = RequireType("CharacterMenuCharacter");
            MethodInfo assertion = typeof(CharacterMenuPacketContractTests).GetMethod("AssertGenericReadOnlySlots",
                BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(character);
            assertion.Invoke(null, new[] { slots });
        }

        static void AssertGenericReadOnlySlots<T>(object slots)
        {
            if (slots is IList<T> mutable)
            {
                Assert.Throws<NotSupportedException>(() => mutable[1] = default);
                Assert.Throws<NotSupportedException>(() => mutable.Add(default));
                Assert.Throws<NotSupportedException>(() => mutable.Clear());
            }
        }

        static IEnumerable<TestCaseData> MalformedFrames()
        {
            foreach (int packetId in new[] { 50, 52, 54 })
            {
                byte[] valid = CharacterMenuWire.Valid(packetId);
                yield return Bad("size zero " + packetId, CharacterMenuWire.Change(valid, 0, 0));
                yield return Bad("size short " + packetId, CharacterMenuWire.Change(valid, 0, 4));
                yield return Bad("size too large " + packetId, CharacterMenuWire.Change(valid, 1, 1));
                yield return Bad("zero request " + packetId, CharacterMenuWire.Request(valid, 0));
                foreach (byte wrong in new byte[] { 48, 49, 51, 53, 55 })
                    yield return Bad("wrong packet " + packetId + " to " + wrong,
                        CharacterMenuWire.Change(valid, 2, wrong));
                yield return Bad("unknown result " + packetId, CharacterMenuWire.Change(valid, 8, 17));
                yield return Bad("result 255 " + packetId, CharacterMenuWire.Change(valid, 8, 255));
                yield return Bad("success invalidField " + packetId, CharacterMenuWire.Change(valid, 9, 1));
                yield return Bad("unknown invalidField " + packetId,
                    CharacterMenuWire.Change(CharacterMenuWire.Failure(packetId, 1, 1), 9, 5));
                for (byte result = 2; result <= 16; result++)
                    yield return Bad("noninput field " + packetId + " result " + result,
                        CharacterMenuWire.Failure(packetId, 1, result, 1));
                yield return Bad("failure carrying success payload " + packetId,
                    CharacterMenuWire.Change(valid, 8, 11));
                int idLengthOffset = packetId == 54 ? 10 : 11;
                yield return Bad("odd id bytes " + packetId,
                    CharacterMenuWire.Change(valid, idLengthOffset, 71));
                yield return Bad("oversized id bytes " + packetId,
                    CharacterMenuWire.Change(valid, idLengthOffset, 74));
                yield return Bad("unbounded id prefix " + packetId,
                    CharacterMenuWire.Change(CharacterMenuWire.Change(valid, idLengthOffset, 254),
                        idLengthOffset + 1, 255));
                foreach (string id in new[] { CharacterMenuWire.Id.ToUpperInvariant(),
                    "00000000-0000-0000-0000-000000000000", "not-a-guid", CharacterMenuWire.Id.Replace("-", "") })
                    yield return Bad("invalid id " + packetId + " " + id,
                        CharacterMenuWire.WithId(packetId, id));
                byte[] badHigh = CharacterMenuWire.Change(valid, idLengthOffset + 2, 0);
                yield return Bad("surrogate in id " + packetId,
                    CharacterMenuWire.Change(badHigh, idLengthOffset + 3, 0xd8));
            }
            yield return Bad("mask bit 3", CharacterMenuWire.Change(CharacterMenuWire.List(mask: 0), 10, 8));
            yield return Bad("occupied empty slot", CharacterMenuWire.Change(CharacterMenuWire.List(mask: 0), 10, 1));
            yield return Bad("unoccupied nonempty slot", CharacterMenuWire.Change(CharacterMenuWire.List(mask: 1), 10, 0));
            yield return Bad("empty slot nonzero class", CharacterMenuWire.Change(CharacterMenuWire.List(mask: 0), 15, 1));
            yield return Bad("duplicate occupied ids", CharacterMenuWire.List(mask: 7, duplicateId: true));
            yield return Bad("list invalid class", CharacterMenuWire.List(mask: 1, firstClass: 2));
            yield return Bad("create invalid slot zero", CharacterMenuWire.Create(slot: 0));
            yield return Bad("create invalid slot four", CharacterMenuWire.Create(slot: 4));
            yield return Bad("create invalid class", CharacterMenuWire.Create(classId: 2));
            foreach (int packetId in new[] { 50, 52 })
            {
                foreach (string name in new[] { "", "가", "가나다라마바사아자차카타파" })
                    yield return Bad("name units " + packetId + " " + name.Length,
                        CharacterMenuWire.Valid(packetId, name));
                byte[] valid = CharacterMenuWire.Valid(packetId);
                yield return Bad("odd name byte prefix " + packetId, CharacterMenuWire.Change(valid, 85, 3));
                yield return Bad("oversize name byte prefix " + packetId, CharacterMenuWire.Change(valid, 85, 26));
                yield return Bad("unpaired high name " + packetId,
                    CharacterMenuWire.Change(CharacterMenuWire.Change(valid, 87, 0), 88, 0xd8));
                yield return Bad("unpaired low name " + packetId,
                    CharacterMenuWire.Change(CharacterMenuWire.Change(valid, 87, 0), 88, 0xdc));
                yield return Bad("failure nonzero class " + packetId,
                    CharacterMenuWire.Change(CharacterMenuWire.Failure(packetId, 1, 4), 15, 1));
            }
            yield return Bad("failure list nonzero mask",
                CharacterMenuWire.Change(CharacterMenuWire.Failure(50, 1, 6), 10, 1));
            yield return Bad("failure create nonzero slot",
                CharacterMenuWire.Change(CharacterMenuWire.Failure(52, 1, 6), 10, 1));
        }

        static TestCaseData Bad(string name, byte[] frame) => new TestCaseData(frame).SetName("Malformed_" + name);

        internal static Type RequireType(string shortName)
        {
            Type type = typeof(ClientConnectionLifetime).Assembly.GetType(MenuNamespace + shortName, false);
            Assert.IsNotNull(type, "v1 required public type is absent: " + MenuNamespace + shortName);
            return type;
        }

        internal static object Read(byte[] frame) => Read(new ArraySegment<byte>(frame));

        internal static object Read(ArraySegment<byte> frame)
        {
            MethodInfo reader = Reader();
            object[] args = { frame, null };
            bool accepted = false;
            Assert.DoesNotThrow(() => accepted = (bool)reader.Invoke(null, args), "reader must not throw");
            Assert.IsTrue(accepted, "valid independent literal rejected");
            Assert.IsNotNull(args[1], "accepted reply");
            return args[1];
        }

        internal static T Get<T>(object target, string name)
        {
            Assert.IsNotNull(target, "target for " + name);
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(property, "required getter " + name);
            return (T)property.GetValue(target);
        }

        static MethodInfo Reader()
        {
            Type type = RequireType("CharacterMenuReplyReader");
            Assert.IsTrue(type.IsPublic && type.IsAbstract && type.IsSealed, "public static reader");
            Type replyType = RequireType("CharacterMenuReply");
            MethodInfo method = type.GetMethod("TryRead", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(ArraySegment<byte>), replyType.MakeByRefType() }, null);
            Assert.IsNotNull(method, "v1 TryRead(ArraySegment<byte>, out CharacterMenuReply) is absent");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            Assert.IsTrue(method.GetParameters()[1].IsOut);
            return method;
        }

        static void Reject(ArraySegment<byte> frame, string because = null)
        {
            MethodInfo reader = Reader();
            object sentinel = Read(CharacterMenuWire.Select());
            object[] args = { frame, sentinel };
            bool accepted = true;
            Assert.DoesNotThrow(() => accepted = (bool)reader.Invoke(null, args), because);
            Assert.IsFalse(accepted, because);
            Assert.IsNull(args[1], "failure must clear reply: " + because);
        }

        static void AssertGetter(Type owner, string name, Type expected)
        {
            PropertyInfo property = owner.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, owner.Name + "." + name);
            Assert.AreEqual(expected, property.PropertyType);
            Assert.IsTrue(property.GetMethod != null && property.GetMethod.IsPublic);
            Assert.IsNull(property.GetSetMethod(false), "no public setter for " + name);
        }

        static void AssertCommon(object reply, ushort packetId, uint requestId, byte result, byte field)
        {
            Assert.AreEqual(packetId, Get<ushort>(reply, "PacketId"));
            Assert.AreEqual(requestId, Get<uint>(reply, "RequestId"));
            Assert.AreEqual(result, Get<byte>(reply, "Result"));
            Assert.AreEqual(field, Get<byte>(reply, "InvalidField"));
        }

        static object[] Slots(object reply) => ((IEnumerable)Get<object>(reply, "Slots")).Cast<object>().ToArray();

        static void AssertCharacter(object character, byte slot, string id, string name, byte classId)
        {
            Assert.IsNotNull(character, "occupied character");
            Assert.AreEqual(slot, Get<byte>(character, "Slot"));
            Assert.AreEqual(id, Get<string>(character, "CharacterIdD"));
            Assert.AreEqual(name, Get<string>(character, "Name"));
            Assert.AreEqual(classId, Get<byte>(character, "ClassId"));
        }
    }

    // Only the two menu test files consume this three-packet literal fixture; it has no product dependencies.
    internal static class CharacterMenuWire
    {
        internal const string Id = "00112233-4455-6677-8899-aabbccddeeff";
        internal static readonly string[] Ids =
        {
            Id, "11112233-4455-6677-8899-aabbccddeeff", "22112233-4455-6677-8899-aabbccddeeff",
        };

        internal static byte[] List(uint request = 0x04030201, byte mask = 7, string name = "가나",
            bool duplicateId = false, byte firstClass = 1)
        {
            var bytes = Header(50, request, 0, 0);
            bytes.Add(mask);
            for (int index = 0; index < 3; index++)
            {
                bool occupied = (mask & (1 << index)) != 0;
                Text(bytes, occupied ? Ids[duplicateId ? 0 : index] : "");
                Text(bytes, occupied ? name : "");
                bytes.Add(occupied ? (index == 1 ? (byte)0 : firstClass) : (byte)0);
            }
            return Finish(bytes);
        }

        internal static byte[] Create(uint request = 0x04030201, byte slot = 1, byte classId = 1,
            string name = "가나", string id = Id)
        {
            var bytes = Header(52, request, 0, 0);
            bytes.Add(slot);
            Text(bytes, id);
            Text(bytes, name);
            bytes.Add(classId);
            return Finish(bytes);
        }

        internal static byte[] Select(uint request = 0x04030201, string id = Id)
        {
            var bytes = Header(54, request, 0, 0);
            Text(bytes, id);
            return Finish(bytes);
        }

        internal static byte[] Failure(int packetId, uint request, byte result, byte field = 0)
        {
            var bytes = Header((byte)packetId, request, result, field);
            // Fixed empty tails from v1: list mask + 3*(empty id/name + class), create slot/id/name/class, select id.
            bytes.AddRange(new byte[packetId == 50 ? 16 : packetId == 52 ? 6 : 2]);
            return Finish(bytes);
        }

        internal static byte[] Valid(int packetId, string name = "가나") =>
            packetId == 50 ? List(name: name) : packetId == 52 ? Create(name: name) : Select();

        internal static byte[] WithId(int packetId, string id)
        {
            if (packetId == 52) return Create(id: id);
            if (packetId == 54) return Select(id: id);
            var bytes = Header(50, 0x04030201, 0, 0);
            bytes.Add(1);
            Text(bytes, id);
            Text(bytes, "가나");
            bytes.Add(0);
            bytes.AddRange(new byte[10]);
            return Finish(bytes);
        }

        internal static byte[] Change(byte[] frame, int offset, byte value)
        {
            byte[] changed = (byte[])frame.Clone();
            changed[offset] = value;
            return changed;
        }

        internal static byte[] Request(byte[] frame, uint request)
        {
            byte[] changed = (byte[])frame.Clone();
            for (int index = 0; index < 4; index++) changed[4 + index] = (byte)(request >> (8 * index));
            return changed;
        }

        internal static void Size(byte[] frame)
        {
            frame[0] = (byte)frame.Length;
            frame[1] = (byte)(frame.Length >> 8);
        }

        static List<byte> Header(byte packetId, uint request, byte result, byte field) => new List<byte>
        {
            0, 0, packetId, 0, (byte)request, (byte)(request >> 8), (byte)(request >> 16),
            (byte)(request >> 24), result, field,
        };

        static void Text(List<byte> bytes, string value)
        {
            byte[] encoded = new UnicodeEncoding(false, false, true).GetBytes(value);
            bytes.Add((byte)encoded.Length);
            bytes.Add((byte)(encoded.Length >> 8));
            bytes.AddRange(encoded);
        }

        static byte[] Finish(List<byte> bytes)
        {
            byte[] frame = bytes.ToArray();
            Size(frame);
            return frame;
        }
    }
}
