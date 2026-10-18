using System.Buffers.Binary;
using System.Reflection;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuPacketFixtures;

namespace Dawnholder.Server.GameServer.Tests.Network;

/// <summary>
/// C7 and packet-contract-v1.md (01_Phases/goals/2026-10-18-character-server):
/// protocol22, appended IDs49..54, frozen IDs1..48, and independent wire fixtures.
/// New Shared types are required through reflection so their absence is an assertion failure,
/// rather than a compile dependency. These codec tests do not establish handler validation,
/// handshake equality, Core/DB behavior, or Unity/UI acceptance of a result.
/// </summary>
public sealed class CharacterMenuPacketContractTests
{
    const string Requirement = "C7 / packet-contract-v1.md";

    public static TheoryData<string> ExistingPacketNames => Names(ExistingPackets.Select(packet => packet.Name));
    public static TheoryData<string> MenuPacketNames => Names(MenuPackets.Select(packet => packet.Name));
    public static TheoryData<string> FrameNames => Names(Frames.Select(frame => frame.Label));

    [Fact]
    public void ProtocolVersion_IsTwentyTwo()
    {
        Type type = RequireSharedType("ProtocolVersion");
        FieldInfo field = RequireField(type, "Current", BindingFlags.Public | BindingFlags.Static);
        Assert.True(field.FieldType == typeof(ushort), $"{Requirement}: ProtocolVersion.Current must be ushort");
        Assert.True(Equals((ushort)22, field.GetValue(null)),
            $"{Requirement}: ProtocolVersion.Current must be22, actual={field.GetValue(null)}");
    }

    [Fact]
    public void PublicMenuEnums_KeepTheirByteNamesAndValues()
    {
        AssertPublicByteEnum("CharacterMenuResult", Results);
        AssertPublicByteEnum("CharacterMenuInvalidField", InvalidFields);
    }

    [Fact]
    public void PacketIds_HaveNoAliasesOrReusedNumbers()
    {
        ushort[] values = Enum.GetNames<PacketID>()
            .Select(name => Convert.ToUInt16(Enum.Parse<PacketID>(name))).ToArray();
        Assert.True(values.Length == values.Distinct().Count(),
            $"{Requirement}: PacketID aliases would reuse a wire number");
    }

    [Theory]
    [MemberData(nameof(ExistingPacketNames))]
    public void ExistingPacket_PreservesItsNumberAndExactOrderedFields(string packetName)
    {
        AssertSchema(ExistingPackets.Single(packet => packet.Name == packetName));
    }

    [Theory]
    [MemberData(nameof(MenuPacketNames))]
    public void MenuPacket_HasItsAllocatedNumberAndExactOrderedFields(string packetName)
    {
        AssertSchema(MenuPackets.Single(packet => packet.Name == packetName));
    }

    [Theory]
    [MemberData(nameof(FrameNames))]
    public void Write_MatchesTheIndependentLiteralFrame(string label)
    {
        Frame frame = Frames.Single(candidate => candidate.Label == label);
        IPacket packet = PacketWithFields(frame);

        byte[] actual = Write(packet, label);

        Assert.True(actual.Length == frame.ExpectedSize,
            $"{Requirement}: {label} ID{frame.PacketId} size must be {frame.ExpectedSize}, actual={actual.Length}");
        Assert.True(actual.Length >= 4, $"{Requirement}: {label} requires size/id header");
        Assert.True(BinaryPrimitives.ReadUInt16LittleEndian(actual) == frame.ExpectedSize,
            $"{Requirement}: {label} size header must include the four header bytes, in LE");
        Assert.True(BinaryPrimitives.ReadUInt16LittleEndian(actual.AsSpan(2)) == frame.PacketId,
            $"{Requirement}: {label} packetId header at offset2 must be ID{frame.PacketId}, in LE");
        AssertBytes(frame.Bytes, actual, label);
    }

    [Theory]
    [MemberData(nameof(FrameNames))]
    public void Read_ConsumesLiteralFieldsFromANonzeroSegmentOffset(string label)
    {
        Frame frame = Frames.Single(candidate => candidate.Label == label);
        IPacket packet = NewPacket(frame.PacketName, frame.PacketId);
        byte[] buffer = Enumerable.Repeat((byte)0xCC, frame.Bytes.Length + 7).ToArray();
        frame.Bytes.CopyTo(buffer, 3);

        // No Write result is fed into Read. Prefix/suffix sentinels expose segment-offset mistakes.
        packet.Read(new ArraySegment<byte>(buffer, 3, frame.Bytes.Length));

        foreach ((string name, object expected) in frame.Fields)
        {
            object? actual = RequireField(packet.GetType(), name).GetValue(packet);
            Assert.True(Equals(expected, actual),
                $"{Requirement}: {label} ID{frame.PacketId}.{name}: expected='{expected}', actual='{actual}'");
        }
        // IPacket.Read exposes no consumed-count result. Exact framing and malformed rejection
        // therefore still need real handler tests; this call alone cannot prove either boundary.
    }

    [Theory]
    [InlineData("list.request")]
    [InlineData("create.request")]
    [InlineData("select.request")]
    public void Requests_PreserveUnsignedRequestIdAtItsFixedOffset(string label)
    {
        Frame frame = Frames.Single(candidate => candidate.Label == label);
        IPacket packet = PacketWithFields(frame);
        RequireField(packet.GetType(), "requestId").SetValue(packet, uint.MaxValue);
        byte[] expected = frame.Bytes.ToArray();
        // Explicit fixture offsets, not a second implementation of uint serialization.
        expected[4] = 0xFF;
        expected[5] = 0xFF;
        expected[6] = 0xFF;
        expected[7] = 0xFF;

        AssertBytes(expected, Write(packet, label), label + ".requestId-max");
        IPacket decoded = NewPacket(frame.PacketName, frame.PacketId);
        decoded.Read(new ArraySegment<byte>(expected));
        Assert.True(Equals(uint.MaxValue, RequireField(decoded.GetType(), "requestId").GetValue(decoded)),
            $"{Requirement}: {label}.requestId must preserve nonzero uint.MaxValue at offset4");
    }

    [Theory]
    [InlineData("create.request.maximum", "rawName", 11, 72)]
    [InlineData("select.request", "characterIdD", 9, 72)]
    [InlineData("create.success.maximum", "characterIdD", 11, 72)]
    [InlineData("create.success.maximum", "name", 85, 24)]
    [InlineData("select.success", "selectedCharacterIdD", 10, 72)]
    [InlineData("list.maximum", "slot1CharacterIdD", 11, 72)]
    [InlineData("list.maximum", "slot1Name", 85, 24)]
    [InlineData("list.maximum", "slot2CharacterIdD", 112, 72)]
    [InlineData("list.maximum", "slot2Name", 186, 24)]
    [InlineData("list.maximum", "slot3CharacterIdD", 213, 72)]
    [InlineData("list.maximum", "slot3Name", 287, 24)]
    public void StringPrefixes_UseUtf16ByteCountsAtContractOffsets(string label, string field, int offset, int bytes)
    {
        Frame frame = Frames.Single(candidate => candidate.Label == label);
        byte[] actual = Write(PacketWithFields(frame), label);

        Assert.True(actual.Length >= offset + 2, $"{Requirement}: {label}.{field} prefix missing at offset{offset}");
        Assert.True(actual[offset] == bytes && actual[offset + 1] == 0,
            $"{Requirement}: {label}.{field} offset{offset} must carry UInt16LE byte count {bytes}, not characters");
    }

    static TheoryData<string> Names(IEnumerable<string> names)
    {
        var data = new TheoryData<string>();
        foreach (string name in names)
        {
            data.Add(name);
        }
        return data;
    }

    static Type RequireSharedType(string name)
    {
        Type? type = typeof(PacketID).Assembly.GetType("Shared.Protocol." + name);
        Assert.True(type != null, $"{Requirement}: required Shared.Protocol.{name} is missing (not a skip)");
        Assert.True(type!.IsPublic, $"{Requirement}: Shared.Protocol.{name} must be public");
        return type!;
    }

    static FieldInfo RequireField(Type type, string name,
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        FieldInfo? field = type.GetField(name, flags);
        Assert.True(field != null, $"{Requirement}: required {type.FullName}.{name} field is missing");
        return field!;
    }

    static IPacket NewPacket(string name, ushort id)
    {
        bool named = Enum.TryParse(name, out PacketID packetId);
        Assert.True(named, $"{Requirement}: required PacketID.{name}=ID{id} is missing (not a skip)");
        Assert.True((ushort)packetId == id, $"{Requirement}: PacketID.{name} must be ID{id}, actual={(ushort)packetId}");
        Type type = RequireSharedType(name);
        Assert.True(typeof(IPacket).IsAssignableFrom(type), $"{Requirement}: {name} must implement IPacket.Read/Write/Protocol");
        Assert.True(!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null,
            $"{Requirement}: {name} needs a public parameterless packet constructor");
        var packet = (IPacket)Activator.CreateInstance(type)!;
        Assert.True(packet.Protocol == id, $"{Requirement}: {name}.Protocol must return ID{id}, actual={packet.Protocol}");
        return packet;
    }

    static void AssertSchema(Schema expected)
    {
        Type type = NewPacket(expected.Name, expected.Id).GetType();
        FieldInfo[] actual = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(field => field.MetadataToken).ToArray();
        string[] fields = expected.Fields.Split(", ", StringSplitOptions.None);
        Assert.True(actual.Length == fields.Length,
            $"{Requirement}: ID{expected.Id} {expected.Name} must keep exactly [{expected.Fields}], actual fields={actual.Length}");
        for (int index = 0; index < fields.Length; index++)
        {
            string[] parts = fields[index].Split(' ');
            Type expectedType = parts[0] switch
            {
                "bool" => typeof(bool), "byte" => typeof(byte), "ushort" => typeof(ushort),
                "int" => typeof(int), "uint" => typeof(uint), "long" => typeof(long),
                "float" => typeof(float), "string" => typeof(string),
                _ => throw new InvalidOperationException("Unsupported frozen PDL type: " + parts[0]),
            };
            Assert.True(actual[index].Name == parts[1] && actual[index].FieldType == expectedType,
                $"{Requirement}: ID{expected.Id} {expected.Name} field[{index}] must be '{fields[index]}', " +
                $"actual='{actual[index].FieldType.Name} {actual[index].Name}'");
        }
    }

    static void AssertPublicByteEnum(string name, (string Name, byte Value)[] expected)
    {
        Type type = RequireSharedType(name);
        Assert.True(type.IsEnum, $"{Requirement}: {name} must be an enum");
        Assert.True(Enum.GetUnderlyingType(type) == typeof(byte), $"{Requirement}: {name} wire type must be byte");
        (string Name, byte Value)[] actual = Enum.GetNames(type)
            .Select(value => (value, Convert.ToByte(Enum.Parse(type, value))))
            .OrderBy(entry => entry.Item2).ToArray();
        Assert.True(expected.SequenceEqual(actual),
            $"{Requirement}: {name} public names/values changed; actual={string.Join(", ", actual)}");
    }

    static IPacket PacketWithFields(Frame frame)
    {
        IPacket packet = NewPacket(frame.PacketName, frame.PacketId);
        foreach ((string name, object value) in frame.Fields)
        {
            FieldInfo field = RequireField(packet.GetType(), name);
            Assert.True(field.FieldType == value.GetType(),
                $"{Requirement}: {frame.Label}.{name} must be {value.GetType().Name}, actual={field.FieldType.Name}");
            field.SetValue(packet, value);
        }
        return packet;
    }

    static byte[] Write(IPacket packet, string label)
    {
        ArraySegment<byte> segment = packet.Write();
        Assert.True(segment.Array != null && segment.Count > 0,
            $"{Requirement}: {label}.Write returned no frame");
        return segment.ToArray();
    }

    static void AssertBytes(byte[] expected, byte[] actual, string label)
    {
        Assert.True(expected.SequenceEqual(actual),
            $"{Requirement}: {label} bytes/field order/size differ; " +
            $"expected={Convert.ToHexString(expected)}, actual={Convert.ToHexString(actual)}");
    }
}
