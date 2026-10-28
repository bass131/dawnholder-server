namespace Dawnholder.Server.GameServer.Tests.Network;

// Oracle source: packet-contract-v1.md, sections 「프레임과 값」 and 「필드 순서와 길이」
// under 01_Phases/goals/2026-10-18-character-server/. Hex fragments are literal wire expectations,
// never output from the product serializer. Sizes are contract expectations, not measurements.
internal static class CharacterMenuPacketFixtures
{
    internal const uint RequestId = 0x12345678;
    internal const string FirstId = "00112233-4455-6677-8899-aabbccddeeff";
    internal const string SecondId = "ffeeddcc-bbaa-9988-7766-554433221100";
    internal const string ThirdId = "01234567-89ab-cdef-0123-456789abcdef";
    internal const string FirstName = "가나";
    internal const string SecondName = "다라";
    internal const string MaximumName = "AAAAAAAAAAAA";

    // 36 D-form characters = 72 UTF-16LE bytes, deliberately unlike Guid.ToByteArray().
    const string FirstIdHex = "300030003100310032003200330033002D0034003400350035002D0036003600370037002D0038003800390039002D00610061006200620063006300640064006500650066006600";
    const string SecondIdHex = "660066006500650064006400630063002D0062006200610061002D0039003900380038002D0037003700360036002D00350035003400340033003300320032003100310030003000";
    const string ThirdIdHex = "300031003200330034003500360037002D0038003900610062002D0063006400650066002D0030003100320033002D00340035003600370038003900610062006300640065006600";
    const string FirstNameHex = "00AC98B0";
    const string SecondNameHex = "E4B27CB7";
    const string MaximumNameHex = "4100410041004100" + "4100410041004100" + "4100410041004100";
    const string MaximumRawNameHex =
        "00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC" +
        "00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC" +
        "00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC00AC";
    const string EmptySlot = "0000000000";

    internal sealed record Schema(string Name, ushort Id, string Fields);
    internal sealed record Frame(string Label, string PacketName, ushort PacketId, int ExpectedSize,
        byte[] Bytes, (string Name, object Value)[] Fields);

    // Preservation snapshot of PDL.xml at 2bf22066, SHA256 6265AECE…E90F633.
    // This table stays frozen: runtime discovery must not derive its own expected schema from PDL.
    internal static readonly Schema[] ExistingPackets =
    {
        new("C_Ping", 1, "long clientTimestampMs"),
        new("S_Pong", 2, "long clientTimestampMs, long serverTimestampMs"),
        new("S_EnterMap", 3, "int entityId, float spawnX, float spawnY"),
        new("S_LeaveMap", 4, "int entityId"),
        new("C_MoveIntent", 5, "byte input, uint clientTick"),
        new("S_Snapshot", 6, "int entityId, float x, float y, float vx, float vy, int serverTick, uint lastAckedClientTick, byte animState"),
        new("C_Handshake", 7, "ushort clientVersion"),
        new("S_HandshakeResult", 8, "bool ok, ushort serverVersion, string reason"),
        new("S_PlayerJoin", 9, "int entityId, float spawnX, float spawnY, byte characterClass"),
        new("S_PlayerLeave", 10, "int entityId"),
        new("C_Attack", 11, "int targetEntityId, int attackerClientTick"),
        new("S_EntitySpawn", 12, "int entityId, byte entityKind, float x, float y, int currentHp, int maxHp"),
        new("S_HitResult", 13, "int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp, byte hitEffect"),
        new("S_EntityDeath", 14, "int entityId"),
        new("S_StageClear", 15, "int bossEntityId"),
        new("C_CharacterSelect", 16, "byte characterClass"),
        new("C_EnterPortal", 17, "int portalId"),
        new("S_MapTransition", 18, "byte destMapId, float spawnX, float spawnY"),
        new("S_EntityState", 19, "int entityId, float x, float y, byte state, byte animState, int serverTick"),
        new("S_EnemyAttack", 20, "int attackerId, int targetId, int damage, int targetCurrentHp, byte attackPattern"),
        new("S_PlayerHp", 21, "int entityId, int currentHp, int maxHp"),
        new("S_PlayerAttack", 22, "int attackerEntityId, byte attackType, int targetEntityId, byte facing"),
        new("S_ProjectileLaunch", 23, "int attackerEntityId, int targetEntityId, byte projectileType, int travelTicks"),
        new("C_SkillUse", 24, "byte skillId, int attackerClientTick, byte facing, byte verticalDir"),
        new("S_SkillCast", 25, "int casterEntityId, byte skillId, int strikeDelayTicks, byte facing"),
        new("C_PartyInvite", 26, "int targetEntityId"),
        new("C_PartyRespond", 27, "int inviterEntityId, byte accept"),
        new("C_PartyLeave", 28, "byte reserved"),
        new("S_PartyInviteRecv", 29, "int inviterEntityId, byte inviterClass"),
        new("S_PartyUpdate", 30, "int partyId, int leaderEntityId, int member0EntityId, int member1EntityId, byte member0Class, byte member1Class"),
        new("S_PartyError", 31, "byte reason"),
        new("S_QuestUpdate", 32, "int currentCount, int targetCount"),
        new("S_PortalLocked", 33, "int requiredCount, int currentCount"),
        new("C_CheatCommand", 34, "byte cheatType"),
        new("C_InventoryRequest", 35, "byte reserved"),
        new("S_InventorySnapshot", 36, "uint revision, int currency, int slot0ItemId, int slot0Count, int slot1ItemId, int slot1Count, int slot2ItemId, int slot2Count, int slot3ItemId, int slot3Count, int slot4ItemId, int slot4Count, int slot5ItemId, int slot5Count, int slot6ItemId, int slot6Count, int slot7ItemId, int slot7Count"),
        new("C_ItemUse", 37, "int itemId, uint expectedRevision"),
        new("S_ItemUseResult", 38, "byte result, int itemId, uint revision"),
        new("S_DungeonClearResult", 39, "byte result, int currency, int reward0ItemId, int reward0Count, int reward1ItemId, int reward1Count, int reward2ItemId, int reward2Count"),
        new("C_DungeonClearConfirm", 40, "byte reserved"),
        new("C_Login", 41, "string accountName"),
        new("S_LoginResult", 42, "byte result"),
        new("C_GuildRequest", 43, "byte reserved"),
        new("S_GuildSnapshot", 44, "long snapshotSequence, byte changeKind, uint guildId, uint guildRevision, int treasury, byte strongholdLevel, byte maxStrongholdLevel, byte blacksmithUnlocked, int memberCount, int onlineMemberCount, uint inventoryRevision, byte weaponLevel, byte maxWeaponLevel, int attackBonus, int strongholdCost, int weaponCurrencyCost, int weaponMaterialId, int weaponMaterialCount, byte strongholdAvailability, byte weaponAvailability, string guildName"),
        new("C_StrongholdUpgrade", 45, "uint expectedGuildRevision"),
        new("C_WeaponEnhance", 46, "uint expectedInventoryRevision"),
        new("S_GuildActionResult", 47, "byte action, byte result, uint requestedRevision, uint guildRevision, uint inventoryRevision, long snapshotSequence"),
        new("S_CharacterLoadResult", 48, "byte result, byte characterClass"),
    };

    internal static readonly Schema[] MenuPackets =
    {
        new("C_CharacterListRequest", 49, "uint requestId, byte reserved"),
        new("S_CharacterListResult", 50, "uint requestId, byte result, byte invalidField, byte occupiedMask, string slot1CharacterIdD, string slot1Name, byte slot1ClassId, string slot2CharacterIdD, string slot2Name, byte slot2ClassId, string slot3CharacterIdD, string slot3Name, byte slot3ClassId"),
        new("C_CharacterCreateRequest", 51, "uint requestId, byte slot, byte classId, byte reserved, string rawName"),
        new("S_CharacterCreateResult", 52, "uint requestId, byte result, byte invalidField, byte slot, string characterIdD, string name, byte classId"),
        new("C_CharacterSelectRequest", 53, "uint requestId, byte reserved, string characterIdD"),
        new("S_CharacterSelectResult", 54, "uint requestId, byte result, byte invalidField, string selectedCharacterIdD"),
    };

    internal static readonly (string Name, byte Value)[] Results =
    {
        ("Success", 0), ("InvalidInput", 1), ("RequestInProgress", 2), ("InvalidState", 3),
        ("SlotOccupied", 4), ("SlotsFull", 5), ("NameTaken", 6), ("CharacterUnavailable", 7),
        ("Busy", 8), ("RecoveryRequired", 9), ("CancelledBeforeDispatch", 10), ("Unavailable", 11),
        ("OutcomeUnknown", 12), ("SchemaMismatch", 13), ("IntegrityFailure", 14),
        ("OperationConflict", 15), ("LegacyFlowOnly", 16),
    };

    internal static readonly (string Name, byte Value)[] InvalidFields =
    {
        ("None", 0), ("Slot", 1), ("Name", 2), ("Class", 3), ("CharacterId", 4),
    };

    internal static readonly Frame[] Frames = BuildFrames();

    static Frame[] BuildFrames()
    {
        var frames = new List<Frame>
        {
            Make("list.request", "C_CharacterListRequest", 49, 9, "090031007856341200",
                ("requestId", RequestId), ("reserved", (byte)0)),
            Make("list.empty", "S_CharacterListResult", 50, 26,
                "1A00320078563412000000" + EmptySlot + EmptySlot + EmptySlot, ListFields(0)),
            Make("list.one-knight-in-slot2", "S_CharacterListResult", 50, 102,
                "6600320078563412000002" + EmptySlot + "4800" + FirstIdHex + "0400" + FirstNameHex + "00" + EmptySlot,
                ListFields(2, id2: FirstId, name2: FirstName)),
            Make("list.three", "S_CharacterListResult", 50, 254,
                "FE00320078563412000007" +
                "4800" + FirstIdHex + "0400" + FirstNameHex + "00" +
                "4800" + SecondIdHex + "0400" + SecondNameHex + "01" +
                "4800" + ThirdIdHex + "04004100420000",
                ListFields(7, id1: FirstId, name1: FirstName, id2: SecondId, name2: SecondName,
                    class2: 1, id3: ThirdId, name3: "AB")),
            Make("list.maximum", "S_CharacterListResult", 50, 314,
                "3A01320078563412000007" +
                "4800" + FirstIdHex + "1800" + MaximumNameHex + "00" +
                "4800" + SecondIdHex + "1800" + MaximumNameHex + "01" +
                "4800" + ThirdIdHex + "1800" + MaximumNameHex + "00",
                ListFields(7, id1: FirstId, name1: MaximumName, id2: SecondId, name2: MaximumName,
                    class2: 1, id3: ThirdId, name3: MaximumName)),
            Make("create.request", "C_CharacterCreateRequest", 51, 17,
                "11003300785634120201000400" + FirstNameHex,
                ("requestId", RequestId), ("slot", (byte)2), ("classId", (byte)1),
                ("reserved", (byte)0), ("rawName", FirstName)),
            Make("create.raw-spaces-preserved", "C_CharacterCreateRequest", 51, 21,
                "15003300785634120100000800200000AC98B02000",
                ("requestId", RequestId), ("slot", (byte)1), ("classId", (byte)0),
                ("reserved", (byte)0), ("rawName", " 가나 ")),
            Make("create.raw-surrogate-pair", "C_CharacterCreateRequest", 51, 21,
                "1500330078563412010000080000AC3DD800DE98B0",
                ("requestId", RequestId), ("slot", (byte)1), ("classId", (byte)0),
                ("reserved", (byte)0), ("rawName", "가\U0001F600나")),
            Make("create.request.maximum", "C_CharacterCreateRequest", 51, 85,
                "55003300785634120300004800" + MaximumRawNameHex,
                ("requestId", RequestId), ("slot", (byte)3), ("classId", (byte)0),
                ("reserved", (byte)0), ("rawName", new string('가', 36))),
            Make("create.success", "S_CharacterCreateResult", 52, 92,
                "5C003400785634120000024800" + FirstIdHex + "0400" + FirstNameHex + "01",
                CreateFields(0, 0, 2, FirstId, FirstName, 1)),
            Make("create.success.maximum", "S_CharacterCreateResult", 52, 112,
                "70003400785634120000034800" + ThirdIdHex + "1800" + MaximumNameHex + "00",
                CreateFields(0, 0, 3, ThirdId, MaximumName, 0)),
            Make("select.request", "C_CharacterSelectRequest", 53, 83,
                "5300350078563412004800" + FirstIdHex,
                ("requestId", RequestId), ("reserved", (byte)0), ("characterIdD", FirstId)),
            Make("select.success", "S_CharacterSelectResult", 54, 84,
                "540036007856341200004800" + FirstIdHex,
                ("requestId", RequestId), ("result", (byte)0), ("invalidField", (byte)0),
                ("selectedCharacterIdD", FirstId)),
        };

        // Literal failure bytes at the contract's result/invalidField offsets (8/9).
        // All public rejection codes retain the empty payload shape; handlers choose the semantic code.
        foreach ((string name, byte result) in Results.Where(entry => entry.Value != 0))
        {
            AddFailure(frames, name, result, 0);
        }
        foreach ((string name, byte invalidField) in InvalidFields.Where(entry => entry.Value != 0))
        {
            AddFailure(frames, "InvalidInput." + name, 1, invalidField);
        }
        return frames.ToArray();
    }

    static void AddFailure(List<Frame> frames, string label, byte result, byte invalidField)
    {
        byte[] list = Convert.FromHexString("1A00320078563412000000" + EmptySlot + EmptySlot + EmptySlot);
        byte[] create = Convert.FromHexString("10003400785634120000000000000000");
        byte[] select = Convert.FromHexString("0C0036007856341200000000");
        foreach (byte[] bytes in new[] { list, create, select })
        {
            bytes[8] = result;
            bytes[9] = invalidField;
        }
        frames.Add(new("list.failure." + label, "S_CharacterListResult", 50, 26, list,
            ListFields(0, result, invalidField)));
        frames.Add(new("create.failure." + label, "S_CharacterCreateResult", 52, 16, create,
            CreateFields(result, invalidField, 0, "", "", 0)));
        frames.Add(new("select.failure." + label, "S_CharacterSelectResult", 54, 12, select,
            new (string, object)[] { ("requestId", RequestId), ("result", result),
                ("invalidField", invalidField), ("selectedCharacterIdD", "") }));
    }

    static Frame Make(string label, string packetName, ushort id, int size, string hex,
        params (string Name, object Value)[] fields) =>
        new(label, packetName, id, size, Convert.FromHexString(hex), fields);

    static (string Name, object Value)[] ListFields(byte mask, byte result = 0, byte invalidField = 0,
        string id1 = "", string name1 = "", byte class1 = 0,
        string id2 = "", string name2 = "", byte class2 = 0,
        string id3 = "", string name3 = "", byte class3 = 0) => new (string, object)[]
    {
        ("requestId", RequestId), ("result", result), ("invalidField", invalidField), ("occupiedMask", mask),
        ("slot1CharacterIdD", id1), ("slot1Name", name1), ("slot1ClassId", class1),
        ("slot2CharacterIdD", id2), ("slot2Name", name2), ("slot2ClassId", class2),
        ("slot3CharacterIdD", id3), ("slot3Name", name3), ("slot3ClassId", class3),
    };

    static (string Name, object Value)[] CreateFields(byte result, byte invalidField, byte slot,
        string id, string name, byte classId) => new (string, object)[]
    {
        ("requestId", RequestId), ("result", result), ("invalidField", invalidField),
        ("slot", slot), ("characterIdD", id), ("name", name), ("classId", classId),
    };
}
