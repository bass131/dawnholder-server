using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuReplyFactoryFixtures;
using Summary = (System.Guid CharacterId, byte Slot, string Name, Shared.Protocol.CharacterClass Class);

namespace Dawnholder.Server.GameServer.Tests.Network;

// World reply assembly only: packet-contract-v1.md and the fixed reply-factory task contract.
// These tests establish no Core calls, DB ownership, Stored behavior, 54->48 admission or guild entry.
public sealed class CharacterMenuReplyFactoryTests
{
    public static TheoryData<string> DamagedSummaries => new()
    {
        "slot-zero", "slot-four", "slot-byte-max", "empty-guid", "class-two", "class-byte-max",
        "null-name", "empty-name", "name-26-bytes", "lone-high-surrogate", "lone-low-surrogate",
        "reversed-surrogates", "non-bmp",
    };

    [Fact]
    public void ProductApi_UsesTheFixedInternalStaticValueSignatures()
        => _ = new CharacterMenuReplyFactoryProbe();

    [Fact]
    public void EmptyList_HasZeroMaskAndThreeEmptySlotsInTheLiteral26ByteFrame()
    {
        CharacterMenuReplyFactoryProbe factory = new();
        S_CharacterListResult reply = factory.ListSuccess(RequestId, Array.Empty<Summary>());

        AssertEmptyList(reply);
        AssertFrame(EmptyList, 26, 50, RequestId, reply);
    }

    [Theory]
    [InlineData((byte)1, "66003200785634120000014800" + FirstIdHex + "0400410061000000000000000000000000")]
    [InlineData((byte)2, "660032007856341200000200000000004800" + FirstIdHex + "040041006100000000000000")]
    [InlineData((byte)3, "6600320078563412000004000000000000000000004800" + FirstIdHex + "04004100610000")]
    public void OneKnight_UsesItsSlotMaskEvenThoughClassZeroAlsoEncodesAnEmptySlot(byte slot, string frame)
    {
        Summary character = First;
        character.Slot = slot;
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterListResult reply = factory.ListSuccess(RequestId, new[] { character });

        AssertFrame(frame, 102, 50, RequestId, reply);
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(2, 0, 1)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 1, 0)]
    public void ThreeCharacters_ArePlacedBySlotAndDoNotReorderTheCallerList(int a, int b, int c)
    {
        Summary[] ordered = { First, Second, Third };
        List<Summary> caller = new() { ordered[a], ordered[b], ordered[c] };
        Summary[] before = caller.ToArray();
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterListResult reply = factory.ListSuccess(RequestId, caller);

        Assert.Equal(before, caller.ToArray());
        AssertFrame(FullList, 254, 50, RequestId, reply);
        caller.Clear();
        caller.Add((Guid.Empty, 255, "changed after return", (CharacterClass)255));
        AssertFrame(FullList, 254, 50, RequestId, reply);
    }

    [Fact]
    public void MaximumList_HasThree72ByteIdsAndThree24ByteNamesInTheLiteral314ByteFrame()
    {
        Summary first = First;
        first.Name = "ABCDEFGHIJKL";
        Summary second = Second;
        second.Name = "MNOPQRSTUVWX";
        Summary third = Third;
        third.Name = "abcdefghijkl";
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterListResult reply = factory.ListSuccess(RequestId, new[] { third, first, second });

        AssertFrame(MaximumList, 314, 50, RequestId, reply);
    }

    [Fact]
    public void NullList_IsAnEmptyIntegrityFailureInsteadOfAnEmptySuccess()
    {
        CharacterMenuReplyFactoryProbe factory = new();
        S_CharacterListResult reply = factory.ListSuccess(RequestId, null);

        AssertEmptyList(reply);
        AssertFrame(IntegrityList, 26, 50, RequestId, reply);
    }

    [Theory]
    [InlineData("four-characters")]
    [InlineData("duplicate-slot")]
    [InlineData("duplicate-id")]
    public void CorruptCollection_IsRejectedWholeWithoutRetainingValidRowsOrMutatingInput(string damage)
    {
        Summary extra = Second;
        if (damage == "duplicate-slot") extra.Slot = First.Slot;
        if (damage == "duplicate-id") extra.CharacterId = First.CharacterId;
        List<Summary> caller = damage == "four-characters"
            ? new() { Third, First, Second, (Guid.Parse("12345678-1234-4567-89ab-123456789abc"), 1, "Dd", CharacterClass.Mage) }
            : new() { First, extra };
        Summary[] before = caller.ToArray();
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterListResult reply = factory.ListSuccess(RequestId, caller);

        Assert.Equal(before, caller.ToArray());
        AssertEmptyList(reply);
        AssertFrame(IntegrityList, 26, 50, RequestId, reply);
    }

    [Theory]
    [MemberData(nameof(DamagedSummaries))]
    public void CorruptSummaryInList_RejectsEverySlotAndPreservesTheCallerValues(string damage)
    {
        Summary[] caller = { First, DamagedSummary(damage) };
        Summary[] before = caller.ToArray();
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterListResult reply = factory.ListSuccess(RequestId, caller);

        Assert.Equal(before, caller);
        AssertEmptyList(reply);
        AssertFrame(IntegrityList, 26, 50, RequestId, reply);
    }

    [Theory]
    [InlineData((byte)1, CharacterClass.Knight, "0100")]
    [InlineData((byte)2, CharacterClass.Mage, "0201")]
    [InlineData((byte)3, CharacterClass.Knight, "0300")]
    public void CreateSuccess_PreservesSlotNameClassAndCanonicalGuid(byte slot, CharacterClass characterClass,
        string literalSlotClass)
    {
        Summary character = First;
        character.Slot = slot;
        character.Class = characterClass;
        Summary before = character;
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterCreateResult reply = factory.CreateSuccess(RequestId, character);

        Assert.Equal(before, character);
        Assert.Equal(FirstId, reply.characterIdD);
        Assert.Equal("Aa", reply.name);
        // The two bytes are independent test data, not calculated from the input enum/slot.
        AssertFrame("5C003400785634120000" + literalSlotClass[..2] + "4800" + FirstIdHex
            + "040041006100" + literalSlotClass[2..], 92, 52, RequestId, reply);
    }

    [Fact]
    public void CreateSuccess_PreservesNfcBmpNameAndItsUtf16ByteLengthPrefix()
    {
        Summary character = First;
        character.Name = "가나";
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterCreateResult reply = factory.CreateSuccess(RequestId, character);

        Assert.Equal("가나", reply.name);
        AssertFrame("5C003400785634120000014800" + FirstIdHex + "040000AC98B000",
            92, 52, RequestId, reply);
    }

    [Fact]
    public void MaximumCreateSuccess_HasTheLiteral112ByteFrameAndDoesNotTruncateTheName()
    {
        Summary character = Second;
        character.Name = "ABCDEFGHIJKL";
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterCreateResult reply = factory.CreateSuccess(RequestId, character);

        Assert.Equal("ABCDEFGHIJKL", reply.name);
        AssertFrame("70003400785634120000024800" + SecondIdHex
            + "18004100420043004400450046004700480049004A004B004C0001", 112, 52, RequestId, reply);
    }

    [Theory]
    [MemberData(nameof(DamagedSummaries))]
    public void CorruptCreateSummary_HasNoSuccessFieldsAndDoesNotMutateTheInput(string damage)
    {
        Summary character = DamagedSummary(damage);
        Summary before = character;
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterCreateResult reply = factory.CreateSuccess(RequestId, character);

        Assert.Equal(before, character);
        AssertEmptyCreate(reply);
        AssertFrame(IntegrityCreate, 16, 52, RequestId, reply);
    }

    [Fact]
    public void SelectSuccess_ReturnsTheSameCanonicalIdInTheLiteral84ByteFrame()
    {
        Guid id = Guid.Parse("AABBCCDD-EEFF-4011-8233-445566778899");
        CharacterMenuReplyFactoryProbe factory = new();

        S_CharacterSelectResult reply = factory.SelectSuccess(RequestId, id);

        Assert.Equal(FirstId, reply.selectedCharacterIdD);
        AssertFrame("540036007856341200004800" + FirstIdHex, 84, 54, RequestId, reply);
    }

    [Fact]
    public void EmptySelectionId_IsAnIntegrityFailureWithNoSelectedId()
    {
        CharacterMenuReplyFactoryProbe factory = new();
        S_CharacterSelectResult reply = factory.SelectSuccess(RequestId, Guid.Empty);

        Assert.Equal("", reply.selectedCharacterIdD);
        AssertFrame(IntegritySelect, 12, 54, RequestId, reply);
    }

    [Theory]
    [InlineData((byte)1, "0102")]
    [InlineData((byte)2, "0200")]
    [InlineData((byte)3, "0300")]
    [InlineData((byte)4, "0400")]
    [InlineData((byte)5, "0500")]
    [InlineData((byte)6, "0600")]
    [InlineData((byte)7, "0700")]
    [InlineData((byte)8, "0800")]
    [InlineData((byte)9, "0900")]
    [InlineData((byte)10, "0A00")]
    [InlineData((byte)11, "0B00")]
    [InlineData((byte)12, "0C00")]
    [InlineData((byte)13, "0D00")]
    [InlineData((byte)14, "0E00")]
    [InlineData((byte)15, "0F00")]
    [InlineData((byte)16, "1000")]
    public void EveryPublicRejection_PreservesItsMeaningAndEmptiesAllSuccessFields(byte result, string outcomeHex)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        AssertFailureTriplet(factory, (CharacterMenuResult)result, CharacterMenuInvalidField.Name, outcomeHex);
    }

    [Theory]
    [InlineData((byte)1, "0101")]
    [InlineData((byte)2, "0102")]
    [InlineData((byte)3, "0103")]
    [InlineData((byte)4, "0104")]
    [InlineData((byte)0, "0E00")]
    [InlineData((byte)5, "0E00")]
    [InlineData((byte)255, "0E00")]
    public void InvalidInput_PreservesOnlyKnownUserFieldsAndDoesNotGuessUnknownMeaning(byte field, string outcomeHex)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        AssertFailureTriplet(factory, CharacterMenuResult.InvalidInput, (CharacterMenuInvalidField)field, outcomeHex);
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)17)]
    [InlineData((byte)255)]
    public void SuccessOrUndefinedFailureResult_BecomesAnEmptyIntegrityFailure(byte result)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        AssertFailureTriplet(factory, (CharacterMenuResult)result, CharacterMenuInvalidField.Slot, "0E00");
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData((byte)4)]
    [InlineData((byte)255)]
    public void NonInputRejection_ClearsEvenAnUndefinedInvalidField(byte field)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        AssertFailureTriplet(factory, CharacterMenuResult.OutcomeUnknown, (CharacterMenuInvalidField)field, "0C00");
    }

    [Theory]
    [InlineData(1u, "0C003600010000000E000000")]
    [InlineData(uint.MaxValue, "0C003600FFFFFFFF0E000000")]
    public void UnsignedRequestId_IsPreservedWithoutNarrowingOrSignChanges(uint requestId, string frame)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        S_CharacterSelectResult reply = factory.SelectSuccess(requestId, Guid.Empty);

        AssertFrame(frame, 12, 54, requestId, reply);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("create")]
    [InlineData("select")]
    [InlineData("failure-list")]
    [InlineData("failure-create")]
    [InlineData("failure-select")]
    public void ZeroInternalRequestId_ThrowsInsteadOfManufacturingAnUncorrelatedReply(string operation)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = operation switch
            {
                "list" => (IPacket)factory.ListSuccess(0, Array.Empty<Summary>()),
                "create" => factory.CreateSuccess(0, First),
                "select" => factory.SelectSuccess(0, First.CharacterId),
                "failure-list" => factory.Failure(0, (PacketID)49, CharacterMenuResult.Busy, CharacterMenuInvalidField.None),
                "failure-create" => factory.Failure(0, (PacketID)51, CharacterMenuResult.Busy, CharacterMenuInvalidField.None),
                "failure-select" => factory.Failure(0, (PacketID)53, CharacterMenuResult.Busy, CharacterMenuInvalidField.None),
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };
        });
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)48)]
    [InlineData((ushort)50)]
    [InlineData((ushort)52)]
    [InlineData((ushort)54)]
    [InlineData(ushort.MaxValue)]
    public void FailureForNonMenuRequest_ThrowsInsteadOfGuessingAResponseKind(ushort requestPacketId)
    {
        CharacterMenuReplyFactoryProbe factory = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Failure(RequestId, (PacketID)requestPacketId,
            CharacterMenuResult.InvalidState, CharacterMenuInvalidField.None));
    }

    static void AssertFailureTriplet(CharacterMenuReplyFactoryProbe factory, CharacterMenuResult result,
        CharacterMenuInvalidField field, string literalOutcomeHex)
    {
        S_CharacterListResult list = Assert.IsType<S_CharacterListResult>(
            factory.Failure(RequestId, (PacketID)49, result, field));
        S_CharacterCreateResult create = Assert.IsType<S_CharacterCreateResult>(
            factory.Failure(RequestId, (PacketID)51, result, field));
        S_CharacterSelectResult select = Assert.IsType<S_CharacterSelectResult>(
            factory.Failure(RequestId, (PacketID)53, result, field));

        AssertEmptyList(list);
        AssertEmptyCreate(create);
        Assert.Equal("", select.selectedCharacterIdD);
        // Fixed packet-v1 failure skeletons; result/field literals come from independent theory data.
        AssertFrame("1A00320078563412" + literalOutcomeHex + "00000000000000000000000000000000",
            26, 50, RequestId, list);
        AssertFrame("1000340078563412" + literalOutcomeHex + "000000000000", 16, 52, RequestId, create);
        AssertFrame("0C00360078563412" + literalOutcomeHex + "0000", 12, 54, RequestId, select);
    }
}
