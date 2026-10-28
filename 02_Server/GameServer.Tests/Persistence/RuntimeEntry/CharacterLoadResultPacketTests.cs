using System.Buffers.Binary;
using System.Reflection;
using Dawnholder.Server.GameServer.Handlers;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 4 (runtime-entry pretest contract; api-contract v2 「패킷과 공개 결과」「ID 43~47과 판 번호」):
/// S_CharacterLoadResult is ID 48 and six bytes (ushort size, ushort id, byte result, byte characterClass), the public
/// CharacterLoadStatus is Success 0 / Busy 1 / RecoveryRequired 2 / Unavailable 3, and IDs 1..42 plus the frozen World
/// definitions 43..47 keep their numbers and fields. Expected bytes come from the literal frames and the independent
/// <see cref="Wire"/> encoder, never from the generator. The ID and field tables are copied from PDL.xml at
/// 6e398abe (SHA256 EDF7ED5F…0B1C) and the frozen World snapshot (SHA256 0EA439BC…1C99). The generated types are
/// looked up by name, as in DungeonClearPacketContractTests, so this assembly compiles before the generator runs.
/// The complete ID table and protocol 22 follow packet-contract-v1.md
/// (01_Phases/goals/2026-10-18-character-server), preserving the original IDs 1..48.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterLoadResultPacketTests
{
    static readonly Dictionary<string, Type> PdlTypes = new()
    {
        ["bool"] = typeof(bool),
        ["byte"] = typeof(byte),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["float"] = typeof(float),
        ["string"] = typeof(string),
    };

    // Core PDL at 6e398abe: IDs 1..42 in file order, fields as "type name" in declaration order.
    static readonly (string Name, int Id, string Fields)[] CorePackets =
    {
        ("C_Ping", 1, "long clientTimestampMs"),
        ("S_Pong", 2, "long clientTimestampMs, long serverTimestampMs"),
        ("S_EnterMap", 3, "int entityId, float spawnX, float spawnY"),
        ("S_LeaveMap", 4, "int entityId"),
        ("C_MoveIntent", 5, "byte input, uint clientTick"),
        ("S_Snapshot", 6, "int entityId, float x, float y, float vx, float vy, int serverTick, uint lastAckedClientTick, byte animState"),
        ("C_Handshake", 7, "ushort clientVersion"),
        ("S_HandshakeResult", 8, "bool ok, ushort serverVersion, string reason"),
        ("S_PlayerJoin", 9, "int entityId, float spawnX, float spawnY, byte characterClass"),
        ("S_PlayerLeave", 10, "int entityId"),
        ("C_Attack", 11, "int targetEntityId, int attackerClientTick"),
        ("S_EntitySpawn", 12, "int entityId, byte entityKind, float x, float y, int currentHp, int maxHp"),
        ("S_HitResult", 13, "int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp, byte hitEffect"),
        ("S_EntityDeath", 14, "int entityId"),
        ("S_StageClear", 15, "int bossEntityId"),
        ("C_CharacterSelect", 16, "byte characterClass"),
        ("C_EnterPortal", 17, "int portalId"),
        ("S_MapTransition", 18, "byte destMapId, float spawnX, float spawnY"),
        ("S_EntityState", 19, "int entityId, float x, float y, byte state, byte animState, int serverTick"),
        ("S_EnemyAttack", 20, "int attackerId, int targetId, int damage, int targetCurrentHp, byte attackPattern"),
        ("S_PlayerHp", 21, "int entityId, int currentHp, int maxHp"),
        ("S_PlayerAttack", 22, "int attackerEntityId, byte attackType, int targetEntityId, byte facing"),
        ("S_ProjectileLaunch", 23, "int attackerEntityId, int targetEntityId, byte projectileType, int travelTicks"),
        ("C_SkillUse", 24, "byte skillId, int attackerClientTick, byte facing, byte verticalDir"),
        ("S_SkillCast", 25, "int casterEntityId, byte skillId, int strikeDelayTicks, byte facing"),
        ("C_PartyInvite", 26, "int targetEntityId"),
        ("C_PartyRespond", 27, "int inviterEntityId, byte accept"),
        ("C_PartyLeave", 28, "byte reserved"),
        ("S_PartyInviteRecv", 29, "int inviterEntityId, byte inviterClass"),
        ("S_PartyUpdate", 30, "int partyId, int leaderEntityId, int member0EntityId, int member1EntityId, byte member0Class, byte member1Class"),
        ("S_PartyError", 31, "byte reason"),
        ("S_QuestUpdate", 32, "int currentCount, int targetCount"),
        ("S_PortalLocked", 33, "int requiredCount, int currentCount"),
        ("C_CheatCommand", 34, "byte cheatType"),
        ("C_InventoryRequest", 35, "byte reserved"),
        ("S_InventorySnapshot", 36, "uint revision, int currency, int slot0ItemId, int slot0Count, int slot1ItemId, int slot1Count, int slot2ItemId, int slot2Count, int slot3ItemId, int slot3Count, int slot4ItemId, int slot4Count, int slot5ItemId, int slot5Count, int slot6ItemId, int slot6Count, int slot7ItemId, int slot7Count"),
        ("C_ItemUse", 37, "int itemId, uint expectedRevision"),
        ("S_ItemUseResult", 38, "byte result, int itemId, uint revision"),
        ("S_DungeonClearResult", 39, "byte result, int currency, int reward0ItemId, int reward0Count, int reward1ItemId, int reward1Count, int reward2ItemId, int reward2Count"),
        ("C_DungeonClearConfirm", 40, "byte reserved"),
        ("C_Login", 41, "string accountName"),
        ("S_LoginResult", 42, "byte result"),
    };

    // Frozen World working-tree snapshot (msg_c48ebefa9ccb), IDs 43..47 kept field for field before Core's 48.
    static readonly (string Name, int Id, string Fields)[] FrozenWorldPackets =
    {
        ("C_GuildRequest", 43, "byte reserved"),
        ("S_GuildSnapshot", 44, "long snapshotSequence, byte changeKind, uint guildId, uint guildRevision, int treasury, byte strongholdLevel, byte maxStrongholdLevel, byte blacksmithUnlocked, int memberCount, int onlineMemberCount, uint inventoryRevision, byte weaponLevel, byte maxWeaponLevel, int attackBonus, int strongholdCost, int weaponCurrencyCost, int weaponMaterialId, int weaponMaterialCount, byte strongholdAvailability, byte weaponAvailability, string guildName"),
        ("C_StrongholdUpgrade", 45, "uint expectedGuildRevision"),
        ("C_WeaponEnhance", 46, "uint expectedInventoryRevision"),
        ("S_GuildActionResult", 47, "byte action, byte result, uint requestedRevision, uint guildRevision, uint inventoryRevision, long snapshotSequence"),
    };

    public static TheoryData<string> CorePacketNames => Names(CorePackets);

    public static TheoryData<string> FrozenWorldPacketNames => Names(FrozenWorldPackets);

    // Success carries the authoritative class Knight 0 / Mage 1; every failure carries 255 (no class).
    public static TheoryData<byte, byte> LoadResultCases => new()
    {
        { Success, (byte)CharacterClass.Knight },
        { Success, (byte)CharacterClass.Mage },
        { Busy, NoClass },
        { RecoveryRequired, NoClass },
        { Unavailable, NoClass },
    };

    [Fact]
    public void LoadStatus_IsTheFourPublicValues()
    {
        Type status = RuntimeEntryApi.SharedType("Shared.Protocol.CharacterLoadStatus");
        Assert.True(status.IsEnum, "api-contract v2: CharacterLoadStatus is a public enum");

        (string Name, long Value)[] values = Enum.GetNames(status)
            .Select(name => (name, Convert.ToInt64(Enum.Parse(status, name))))
            .OrderBy(entry => entry.Item2)
            .ToArray();

        Assert.True(status.IsPublic, "api-contract v2: CharacterLoadStatus is public");
        Assert.Equal(new[] { ("Success", 0L), ("Busy", 1L), ("RecoveryRequired", 2L), ("Unavailable", 3L) }, values);
    }

    [Fact]
    public void CharacterSelectIntentValues_StayKnightZeroAndMageOne()
    {
        Assert.Equal(0, (int)CharacterClass.Knight);
        Assert.Equal(1, (int)CharacterClass.Mage);
        Assert.Equal(new[] { "Knight", "Mage" }, Enum.GetNames<CharacterClass>());
    }

    [Fact]
    public void LoadResult_IsPacketFortyEight_WithByteResultAndByteClass()
    {
        bool named = Enum.TryParse("S_CharacterLoadResult", out PacketID id);
        Type type = RuntimeEntryApi.SharedType("Shared.Protocol.S_CharacterLoadResult");

        Assert.True(named, $"{RuntimeEntryApi.Missing}PacketID.S_CharacterLoadResult does not exist yet");
        Assert.Equal(LoadResultId, (ushort)id);
        Assert.Equal(LoadResultId, NewPacket(type).Protocol);
        Assert.Equal(new[] { ("result", typeof(byte)), ("characterClass", typeof(byte)) }, PublicFields(type));
    }

    // Checked against literals first, so the oracle is confirmed even while the generated type is missing.
    [Fact]
    public void LoadResult_SuccessMageAndUnavailable_AreTheseSixBytes()
    {
        byte[] successMage = { 0x06, 0x00, 0x30, 0x00, 0x00, 0x01 };
        byte[] unavailable = { 0x06, 0x00, 0x30, 0x00, 0x03, 0xFF };
        Assert.Equal(successMage, Wire.LoadResult(Success, (byte)CharacterClass.Mage));
        Assert.Equal(unavailable, Wire.LoadResult(Unavailable, NoClass));
        Type type = RequireLoadResultType();

        byte[] writtenSuccess = Write(type, Success, (byte)CharacterClass.Mage);
        byte[] writtenUnavailable = Write(type, Unavailable, NoClass);

        Assert.Equal(successMage, writtenSuccess);
        Assert.Equal(unavailable, writtenUnavailable);
    }

    [Theory]
    [MemberData(nameof(LoadResultCases))]
    public void LoadResult_WritesAndReadsTheGoldenFrame(byte result, byte characterClass)
    {
        byte[] golden = Wire.LoadResult(result, characterClass);
        Type type = RequireLoadResultType();

        byte[] written = Write(type, result, characterClass);
        IPacket decoded = NewPacket(type);
        decoded.Read(new ArraySegment<byte>(golden));

        Assert.Equal(golden, written);
        Assert.Equal(6, written.Length);
        Assert.Equal(result, GetField(decoded, "result"));
        Assert.Equal(characterClass, GetField(decoded, "characterClass"));
    }

    // Protective: holds before the change and must keep holding after it.
    [Theory]
    [MemberData(nameof(CorePacketNames))]
    public void ExistingPacket_KeepsItsIdAndFields(string name)
    {
        (string Name, int Id, string Fields) source = CorePackets.Single(packet => packet.Name == name);

        bool named = Enum.TryParse(name, out PacketID id);
        Type? type = typeof(PacketID).Assembly.GetType("Shared.Protocol." + name);

        Assert.True(named, $"PacketID.{name} disappeared");
        Assert.Equal(source.Id, (int)id);
        Assert.NotNull(type);
        Assert.Equal(Expected(source.Fields), PublicFields(type!));
    }

    [Theory]
    [MemberData(nameof(FrozenWorldPacketNames))]
    public void FrozenWorldPacket_KeepsItsIdAndFieldsBeforeTheLoadResult(string name)
    {
        (string Name, int Id, string Fields) source = FrozenWorldPackets.Single(packet => packet.Name == name);

        bool named = Enum.TryParse(name, out PacketID id);
        Assert.True(named, $"{RuntimeEntryApi.Missing}PacketID.{name} (frozen World definition) does not exist yet");
        Type type = RuntimeEntryApi.SharedType("Shared.Protocol." + name);

        Assert.Equal(source.Id, (int)id);
        Assert.Equal(Expected(source.Fields), PublicFields(type));
    }

    // packet-contract-v1.md: IDs 1..48 stay frozen; the six menu packets append at 49..54 with protocol 22.
    [Fact]
    public void PacketIds_AreOneToFiftyFour_WithTheLoadResultStillFortyEight()
    {
        int[] ids = Enum.GetValues<PacketID>().Select(id => (int)id).Order().ToArray();
        string[] names = CorePackets.Concat(FrozenWorldPackets).Select(packet => packet.Name)
            .Append("S_CharacterLoadResult")
            .Concat(new[]
            {
                "C_CharacterListRequest",
                "S_CharacterListResult",
                "C_CharacterCreateRequest",
                "S_CharacterCreateResult",
                "C_CharacterSelectRequest",
                "S_CharacterSelectResult",
            }).ToArray();
        string[] actualNames = Enum.GetValues<PacketID>().OrderBy(id => (int)id).Select(id => id.ToString()).ToArray();

        Assert.Equal(Enumerable.Range(1, 54), ids);
        Assert.Equal(names, actualNames);
        Assert.Equal((ushort)22, ProtocolVersion.Current);
    }

    // Protective, holds today: 48 is a server-to-client packet; the server registers no handler for it, and a client
    // that sends one anyway is dropped without a reply, a close or a class change.
    [Fact]
    public async Task LoadResultSentByAClient_IsDroppedByTheServer()
    {
        using AdmissionTestWorld fixture = new(attachPersistence: false);
        EntrySession session = await fixture.EnterAsync(OtherName, CharacterClass.Knight);
        bool registered = HandlerRegistry.TryGet((PacketID)LoadResultId, out _);
        int mark = session.Mark;

        Exception? thrown = Record.Exception(() => session.OnRecvPacket(Wire.LoadResult(Success, (byte)CharacterClass.Mage)));
        await fixture.TicksAsync(3);
        bool classKnown = fixture.World.TryGetEntityClass(session.EntityId, out byte entityClass);

        Assert.False(registered, "no server handler may exist for the server-to-client load result");
        Assert.Null(thrown);
        Assert.False(session.IsClosing);
        Assert.DoesNotContain(LoadResultId, session.IdsSince(mark));
        Assert.True(classKnown);
        Assert.Equal((byte)CharacterClass.Knight, entityClass);
    }

    static TheoryData<string> Names((string Name, int Id, string Fields)[] packets)
    {
        TheoryData<string> names = new();
        foreach ((string Name, int Id, string Fields) packet in packets)
            names.Add(packet.Name);
        return names;
    }

    static Type RequireLoadResultType() => RuntimeEntryApi.SharedType("Shared.Protocol.S_CharacterLoadResult");

    static IPacket NewPacket(Type type) => (IPacket)Activator.CreateInstance(type)!;

    static byte[] Write(Type type, byte result, byte characterClass)
    {
        IPacket packet = NewPacket(type);
        SetField(packet, "result", result);
        SetField(packet, "characterClass", characterClass);
        return packet.Write().ToArray();
    }

    static void SetField(IPacket packet, string name, byte value)
    {
        FieldInfo? field = packet.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(field is not null, $"{RuntimeEntryApi.Missing}{packet.GetType().Name}.{name} is not a public field");
        Assert.Equal(typeof(byte), field!.FieldType);
        field.SetValue(packet, value);
    }

    static object? GetField(IPacket packet, string name)
        => packet.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(packet);

    // Metadata order is declaration order, which the generator emits in PDL order.
    static (string Name, Type Type)[] PublicFields(Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(field => field.MetadataToken)
            .Select(field => (field.Name, field.FieldType))
            .ToArray();

    static (string Name, Type Type)[] Expected(string fields)
        => fields.Split(", ")
            .Select(field => field.Split(' '))
            .Select(parts => (parts[1], PdlTypes[parts[0]]))
            .ToArray();

    /// <summary>Independent oracle: [ushort size incl. header][ushort id 48][result][characterClass], LittleEndian.</summary>
    static class Wire
    {
        internal static byte[] LoadResult(byte result, byte characterClass)
        {
            byte[] packet = new byte[6];
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), LoadResultId);
            packet[4] = result;
            packet[5] = characterClass;
            return packet;
        }
    }
}
