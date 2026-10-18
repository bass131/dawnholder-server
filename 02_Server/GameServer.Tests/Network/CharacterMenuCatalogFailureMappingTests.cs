using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuReplyFactoryFixtures;

namespace Dawnholder.Server.GameServer.Tests.Network;

// core-consumer-contract-v1.md and packet-contract-v1.md own these meaning/wire expectations.
// Wrapper extraction, receipt reconciliation, Core storage and admission lifetimes remain outside this seam.
public sealed class CharacterMenuCatalogFailureMappingTests
{
    // Literal public wire codes, not Core ordinals. Keep the two selection failures named separately.
    public static TheoryData<string, byte> DirectFailures => new()
    {
        { "RequestInProgress", 2 },
        { "InvalidState", 3 },
        { "SlotOccupied", 4 },
        { "SlotsFull", 5 },
        { "NameTaken", 6 },
        { "CharacterNotFound", 7 },
        { "NotOwned", 7 },
        { "Busy", 8 },
        { "RecoveryRequired", 9 },
        { "CancelledBeforeDispatch", 10 },
        { "Unavailable", 11 },
        { "OutcomeUnknown", 12 },
        { "StillUnknown", 12 },
        { "SchemaMismatch", 13 },
        { "IntegrityFailure", 14 },
        { "OperationConflict", 15 },
    };

    public static IEnumerable<object[]> NonInputFailureFields
    {
        get
        {
            string[] fields = { "Slot", "Name", "Class", "CharacterId", "Account", "OperationId", "Request",
                CharacterMenuCatalogFailureMappingProbe.UndefinedField };
            foreach (object[] failure in DirectFailures)
            {
                foreach (string field in fields)
                {
                    yield return new[] { failure[0], field, failure[1] };
                }
            }
        }
    }

    [Fact]
    public void ProductApi_RequiresTheRealCoreTypedInternalStaticMapper()
        => _ = new CharacterMenuCatalogFailureMappingProbe();

    [Theory]
    [MemberData(nameof(DirectFailures))]
    public void DirectFailure_PreservesItsNamedMeaningAsALiteralWireCode(string coreStatus, byte expectedResult)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();

        var actual = mapper.MapFailure(coreStatus, "None");

        Assert.Equal(expectedResult, (byte)actual.Result);
        Assert.Equal((byte)0, (byte)actual.InvalidField);
    }

    [Theory]
    [InlineData("Slot", (byte)1)]
    [InlineData("Name", (byte)2)]
    [InlineData("Class", (byte)3)]
    [InlineData("CharacterId", (byte)4)]
    public void InvalidInput_PreservesOnlyTheFourPublicUserFields(string coreField, byte expectedField)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();

        var actual = mapper.MapFailure("InvalidInput", coreField);

        Assert.Equal((byte)1, (byte)actual.Result);
        Assert.Equal(expectedField, (byte)actual.InvalidField);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Account")]
    [InlineData("OperationId")]
    [InlineData("Request")]
    [InlineData(CharacterMenuCatalogFailureMappingProbe.UndefinedField)]
    public void InvalidInput_ServerContextOrUnexplainedFieldIsIntegrityFailure(string coreField)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();

        var actual = mapper.MapFailure("InvalidInput", coreField);

        Assert.Equal((byte)14, (byte)actual.Result);
        Assert.Equal((byte)0, (byte)actual.InvalidField);
    }

    [Theory]
    [MemberData(nameof(NonInputFailureFields))]
    public void NonInputFailure_DiscardsEveryUserServerAndUndefinedField(
        string coreStatus, string coreField, byte expectedResult)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();

        var actual = mapper.MapFailure(coreStatus, coreField);

        Assert.Equal(expectedResult, (byte)actual.Result);
        Assert.Equal((byte)0, (byte)actual.InvalidField);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Slot")]
    [InlineData("Account")]
    [InlineData(CharacterMenuCatalogFailureMappingProbe.UndefinedField)]
    public void UndefinedCoreStatus_IsIntegrityFailureWithoutGuessingSuccessOrLegacyMode(string coreField)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();

        var actual = mapper.MapUndefinedStatus(coreField);

        Assert.Equal((byte)14, (byte)actual.Result);
        Assert.Equal((byte)0, (byte)actual.InvalidField);
    }

    [Theory]
    [InlineData("Prepared")]
    [InlineData("Listed")]
    [InlineData("Created")]
    [InlineData("Selected")]
    [InlineData("Applied")]
    [InlineData("NotApplied")]
    public void SuccessOrAggregateStatus_CannotBeInterpretedAsADirectFailure(string coreStatus)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();

        // This API receives no summary or Rejection; it cannot infer wrapper contents.
        var actual = mapper.MapFailure(coreStatus, "Name");

        Assert.Equal((byte)14, (byte)actual.Result);
        Assert.Equal((byte)0, (byte)actual.InvalidField);
    }

    [Theory]
    [InlineData("InvalidInput", "Name", 49, (byte)1, (byte)2,
        "1A00320078563412010200000000000000000000000000000000")]
    [InlineData("InvalidInput", "Name", 51, (byte)1, (byte)2,
        "10003400785634120102000000000000")]
    [InlineData("InvalidInput", "Name", 53, (byte)1, (byte)2,
        "0C0036007856341201020000")]
    [InlineData("InvalidInput", "Account", 49, (byte)14, (byte)0,
        "1A003200785634120E0000000000000000000000000000000000")]
    [InlineData("InvalidInput", "Account", 51, (byte)14, (byte)0,
        "10003400785634120E00000000000000")]
    [InlineData("InvalidInput", "Account", 53, (byte)14, (byte)0,
        "0C003600785634120E000000")]
    [InlineData("CharacterNotFound", "Name", 49, (byte)7, (byte)0,
        "1A00320078563412070000000000000000000000000000000000")]
    [InlineData("CharacterNotFound", "Name", 51, (byte)7, (byte)0,
        "10003400785634120700000000000000")]
    [InlineData("CharacterNotFound", "Name", 53, (byte)7, (byte)0,
        "0C0036007856341207000000")]
    [InlineData("NotOwned", "Account", 49, (byte)7, (byte)0,
        "1A00320078563412070000000000000000000000000000000000")]
    [InlineData("NotOwned", "Account", 51, (byte)7, (byte)0,
        "10003400785634120700000000000000")]
    [InlineData("NotOwned", "Account", 53, (byte)7, (byte)0,
        "0C0036007856341207000000")]
    [InlineData("OutcomeUnknown", "OperationId", 49, (byte)12, (byte)0,
        "1A003200785634120C0000000000000000000000000000000000")]
    [InlineData("OutcomeUnknown", "OperationId", 51, (byte)12, (byte)0,
        "10003400785634120C00000000000000")]
    [InlineData("OutcomeUnknown", "OperationId", 53, (byte)12, (byte)0,
        "0C003600785634120C000000")]
    [InlineData("StillUnknown", "Request", 49, (byte)12, (byte)0,
        "1A003200785634120C0000000000000000000000000000000000")]
    [InlineData("StillUnknown", "Request", 51, (byte)12, (byte)0,
        "10003400785634120C00000000000000")]
    [InlineData("StillUnknown", "Request", 53, (byte)12, (byte)0,
        "0C003600785634120C000000")]
    public void MappedFailure_UsesTheCurrentFactoryAndLiteralEmptyResponseFrame(
        string coreStatus, string coreField, int requestPacketId, byte expectedResult, byte expectedField,
        string literalFrame)
    {
        CharacterMenuCatalogFailureMappingProbe mapper = new();
        var mapped = mapper.MapFailure(coreStatus, coreField);
        // Assert before the factory, whose defensive normalization must not conceal a bad mapper.
        Assert.Equal(expectedResult, (byte)mapped.Result);
        Assert.Equal(expectedField, (byte)mapped.InvalidField);
        CharacterMenuReplyFactoryProbe factory = new();

        IPacket reply = factory.Failure(RequestId, (PacketID)requestPacketId, mapped.Result, mapped.InvalidField);

        switch (requestPacketId)
        {
            case 49:
                AssertEmptyList(Assert.IsType<S_CharacterListResult>(reply));
                AssertFrame(literalFrame, 26, 50, RequestId, reply);
                break;
            case 51:
                AssertEmptyCreate(Assert.IsType<S_CharacterCreateResult>(reply));
                AssertFrame(literalFrame, 16, 52, RequestId, reply);
                break;
            case 53:
                Assert.Equal("", Assert.IsType<S_CharacterSelectResult>(reply).selectedCharacterIdD);
                AssertFrame(literalFrame, 12, 54, RequestId, reply);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(requestPacketId));
        }
    }
}
