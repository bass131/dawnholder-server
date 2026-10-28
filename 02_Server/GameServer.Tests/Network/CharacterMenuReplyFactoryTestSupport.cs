using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;
using Summary = (System.Guid CharacterId, byte Slot, string Name, Shared.Protocol.CharacterClass Class);

namespace Dawnholder.Server.GameServer.Tests.Network;

// Only drives the real World factory. Core catalog, ownership and admission are outside this seam.
// Missing product/signatures assert RED; no response assembly is supplied by this test helper.
internal sealed class CharacterMenuReplyFactoryProbe
{
    readonly MethodInfo _list;
    readonly MethodInfo _create;
    readonly MethodInfo _select;
    readonly MethodInfo _failure;

    internal CharacterMenuReplyFactoryProbe()
    {
        Type? factory = typeof(GameSession).Assembly.GetType(
            "Dawnholder.Server.GameServer.Sessions.CharacterMenuReplyFactory");
        Assert.True(factory is not null,
            "reply-factory contract: product CharacterMenuReplyFactory is absent; later semantics not reached");
        Assert.True(factory!.IsNotPublic && factory.IsAbstract && factory.IsSealed,
            "reply-factory contract: the World factory must be internal static");
        _list = RequireMethod(factory, "ListSuccess", typeof(S_CharacterListResult),
            typeof(uint), typeof(IReadOnlyList<Summary>));
        _create = RequireMethod(factory, "CreateSuccess", typeof(S_CharacterCreateResult),
            typeof(uint), typeof(Summary));
        _select = RequireMethod(factory, "SelectSuccess", typeof(S_CharacterSelectResult),
            typeof(uint), typeof(Guid));
        _failure = RequireMethod(factory, "Failure", typeof(IPacket), typeof(uint), typeof(PacketID),
            typeof(CharacterMenuResult), typeof(CharacterMenuInvalidField));
    }

    internal S_CharacterListResult ListSuccess(uint requestId, IReadOnlyList<Summary>? characters)
        => Invoke<S_CharacterListResult>(_list, requestId, characters);

    internal S_CharacterCreateResult CreateSuccess(uint requestId, Summary character)
        => Invoke<S_CharacterCreateResult>(_create, requestId, character);

    internal S_CharacterSelectResult SelectSuccess(uint requestId, Guid characterId)
        => Invoke<S_CharacterSelectResult>(_select, requestId, characterId);

    internal IPacket Failure(uint requestId, PacketID requestPacketId,
        CharacterMenuResult result, CharacterMenuInvalidField invalidField)
        => Invoke<IPacket>(_failure, requestId, requestPacketId, result, invalidField);

    static MethodInfo RequireMethod(Type owner, string name, Type result, params Type[] arguments)
    {
        MethodInfo? method = owner.GetMethod(name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            null, arguments, null);
        Assert.True(method is not null,
            $"reply-factory contract: {name} exact signature is absent; later semantics not reached");
        Assert.True(method!.IsStatic && method.IsAssembly, $"{name} must be internal static");
        Assert.Equal(result, method.ReturnType);
        return method;
    }

    static T Invoke<T>(MethodInfo method, params object?[] arguments)
    {
        try
        {
            return Assert.IsAssignableFrom<T>(method.Invoke(null, arguments));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}

// packet-contract-v1.md: literal LE headers, D36 UTF-16LE IDs and byte-length prefixes.
// Concatenated pieces below are fixed wire literals, never output from a product writer/reader.
internal static class CharacterMenuReplyFactoryFixtures
{
    internal const uint RequestId = 0x12345678;
    internal const string FirstId = "aabbccdd-eeff-4011-8233-445566778899";
    internal const string SecondId = "11223344-5566-4788-99aa-bbccddeeff00";
    internal const string ThirdId = "ffeeddcc-bbaa-4988-8766-554433221100";
    internal const string FirstIdHex =
        "610061006200620063006300640064002D0065006500660066002D0034003000310031002D0038003200330033002D00340034003500350036003600370037003800380039003900";
    internal const string SecondIdHex =
        "310031003200320033003300340034002D0035003500360036002D0034003700380038002D0039003900610061002D00620062006300630064006400650065006600660030003000";
    internal const string ThirdIdHex =
        "660066006500650064006400630063002D0062006200610061002D0034003900380038002D0038003700360036002D00350035003400340033003300320032003100310030003000";
    internal const string EmptyList = "1A00320078563412000000"
        + "0000000000" + "0000000000" + "0000000000";
    internal const string IntegrityList = "1A003200785634120E0000"
        + "0000000000" + "0000000000" + "0000000000";
    internal const string IntegrityCreate = "10003400785634120E00000000000000";
    internal const string IntegritySelect = "0C003600785634120E000000";
    internal const string FullList = "FE00320078563412000007"
        + "4800" + FirstIdHex + "04004100610000"
        + "4800" + SecondIdHex + "04004200620001"
        + "4800" + ThirdIdHex + "04004300630000";
    internal const string MaximumList = "3A01320078563412000007"
        + "4800" + FirstIdHex + "18004100420043004400450046004700480049004A004B004C0000"
        + "4800" + SecondIdHex + "18004D004E004F0050005100520053005400550056005700580001"
        + "4800" + ThirdIdHex + "18006100620063006400650066006700680069006A006B006C0000";

    internal static Summary First => (Guid.Parse(FirstId), 1, "Aa", CharacterClass.Knight);
    internal static Summary Second => (Guid.Parse(SecondId), 2, "Bb", CharacterClass.Mage);
    internal static Summary Third => (Guid.Parse(ThirdId), 3, "Cc", CharacterClass.Knight);

    internal static Summary DamagedSummary(string damage)
    {
        Summary character = Second;
        switch (damage)
        {
            case "slot-zero": character.Slot = 0; break;
            case "slot-four": character.Slot = 4; break;
            case "slot-byte-max": character.Slot = 255; break;
            case "empty-guid": character.CharacterId = Guid.Empty; break;
            case "class-two": character.Class = (CharacterClass)2; break;
            case "class-byte-max": character.Class = (CharacterClass)255; break;
            case "null-name": character.Name = null!; break;
            case "empty-name": character.Name = ""; break;
            case "name-26-bytes": character.Name = "ABCDEFGHIJKLM"; break;
            case "lone-high-surrogate": character.Name = "A\uD800"; break;
            case "lone-low-surrogate": character.Name = "A\uDC00"; break;
            case "reversed-surrogates": character.Name = "\uDC00\uD800"; break;
            case "non-bmp": character.Name = "A\U0001F600"; break;
            default: throw new ArgumentOutOfRangeException(nameof(damage));
        }
        return character;
    }

    internal static void AssertFrame(string literalHex, int expectedSize, ushort expectedPacketId,
        uint expectedRequestId, IPacket packet)
    {
        byte[] expected = Convert.FromHexString(literalHex);
        Assert.Equal(expectedSize, expected.Length);
        // Write is the actual observation only. No product Read or round-trip forms the oracle.
        byte[] actual = packet.Write().ToArray();
        Assert.Equal(expectedSize, actual.Length);
        Assert.Equal((ushort)expectedSize, BinaryPrimitives.ReadUInt16LittleEndian(actual));
        Assert.Equal(expectedPacketId, BinaryPrimitives.ReadUInt16LittleEndian(actual.AsSpan(2, 2)));
        Assert.Equal(expectedRequestId, BinaryPrimitives.ReadUInt32LittleEndian(actual.AsSpan(4, 4)));
        Assert.Equal(expected, actual);
    }

    internal static void AssertEmptyList(S_CharacterListResult reply)
    {
        Assert.Equal((byte)0, reply.occupiedMask);
        Assert.Equal("", reply.slot1CharacterIdD);
        Assert.Equal("", reply.slot1Name);
        Assert.Equal((byte)0, reply.slot1ClassId);
        Assert.Equal("", reply.slot2CharacterIdD);
        Assert.Equal("", reply.slot2Name);
        Assert.Equal((byte)0, reply.slot2ClassId);
        Assert.Equal("", reply.slot3CharacterIdD);
        Assert.Equal("", reply.slot3Name);
        Assert.Equal((byte)0, reply.slot3ClassId);
    }

    internal static void AssertEmptyCreate(S_CharacterCreateResult reply)
    {
        Assert.Equal((byte)0, reply.slot);
        Assert.Equal("", reply.characterIdD);
        Assert.Equal("", reply.name);
        Assert.Equal((byte)0, reply.classId);
    }
}
