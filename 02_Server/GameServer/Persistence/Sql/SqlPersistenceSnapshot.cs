using System.Globalization;
using System.Text.Json;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

/// <summary>SQL v1 historical proof만 해석한다. 현재 권위와의 일치는 owner가 별도로 확인한다.</summary>
internal static class SqlPersistenceSnapshot
{
    internal static OperationReceipt Parse(string json, OperationIdentity operation, PersistenceResultCode code)
    {
        if (json.Length is < 1 or > 2048)
            throw new InvalidDataException("Invalid persistence snapshot.");
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Keys(root, "version", "kind", "resultCode", "slotId", "accountId", "characterId", "ownerKind", "ownerId",
            "fence", "sequence", "characterPresent", "class", "characterVersionHex", "progressPresent",
            "progressVersionHex", "safe", "storedProgress");
        PersistenceIdentity identity = operation.Identity;
        if (root.GetProperty("version").GetInt32() != 1 ||
            root.GetProperty("kind").GetInt32() != (int)operation.Kind ||
            root.GetProperty("resultCode").GetInt16() != (short)code ||
            root.GetProperty("slotId").GetByte() != identity.SlotId ||
            GuidValue(root.GetProperty("accountId")) != identity.AccountId ||
            GuidValue(root.GetProperty("characterId")) != identity.CharacterId)
            throw new InvalidDataException("Snapshot identity mismatch.");

        PersistenceOwnerKind kind = (PersistenceOwnerKind)root.GetProperty("ownerKind").GetByte();
        JsonElement owner = root.GetProperty("ownerId");
        PersistenceAuthority authority = new(identity, kind,
            owner.ValueKind == JsonValueKind.Null ? null : GuidValue(owner),
            Counter(root.GetProperty("fence")), Counter(root.GetProperty("sequence")));
        if (operation.Kind == PersistenceOperationKind.ReleaseRuntime)
        {
            foreach (string field in new[] { "characterPresent", "class", "characterVersionHex", "progressPresent",
                "progressVersionHex", "safe", "storedProgress" })
                RequireNull(root.GetProperty(field));
            if (operation.ExpectedFence == long.MaxValue || authority.Fence != operation.ExpectedFence + 1)
                throw new InvalidDataException("Invalid release fence.");
            return new OperationReceipt(operation.OperationId, operation.Kind, code, authority, null, null, null, null);
        }

        bool characterPresent = root.GetProperty("characterPresent").GetBoolean();
        bool progressPresent = root.GetProperty("progressPresent").GetBoolean();
        if (!characterPresent || !progressPresent || authority.OwnerId != operation.OwnerId ||
            authority.LastSequence != operation.Sequence ||
            authority.Fence != (operation.Kind == PersistenceOperationKind.Acquire
                ? checked(operation.ExpectedFence + 1) : operation.ExpectedFence))
            throw new InvalidDataException("Incomplete runtime snapshot.");
        CharacterClass characterClass = CharacterClassValue(root.GetProperty("class").GetByte());
        PersistenceToken character = HexToken(root.GetProperty("characterVersionHex"));
        PersistenceToken progress = HexToken(root.GetProperty("progressVersionHex"));
        JsonElement safe = root.GetProperty("safe");
        ValidateProgress(safe, safeProjection: true);
        ValidateProgress(root.GetProperty("storedProgress"), safeProjection: false);
        SafeCheckpoint checkpoint = new(characterClass, Real(safe.GetProperty("x")), Real(safe.GetProperty("y")),
            safe.GetProperty("maxHp").GetInt32());
        SafeCheckpoint expected = operation.Kind == PersistenceOperationKind.Acquire
            ? new SafeCheckpoint(characterClass, operation.SafeDefaults!.TownX, operation.SafeDefaults.TownY,
                characterClass == CharacterClass.Knight ? operation.SafeDefaults.KnightMaxHp : operation.SafeDefaults.MageMaxHp)
            : operation.Safe!;
        if (!checkpoint.Equals(expected))
            throw new InvalidDataException("Snapshot safe projection mismatch.");
        return new OperationReceipt(operation.OperationId, operation.Kind, code, authority,
            characterClass, checkpoint, character, progress);
    }

    internal static void ValidateProgress(JsonElement value, bool safeProjection)
    {
        Keys(value, "mapId", "x", "y", "hp", "maxHp", "bossUnlocked");
        byte map = value.GetProperty("mapId").GetByte();
        _ = Real(value.GetProperty("x"));
        _ = Real(value.GetProperty("y"));
        int hp = value.GetProperty("hp").GetInt32();
        int maxHp = value.GetProperty("maxHp").GetInt32();
        bool boss = value.GetProperty("bossUnlocked").GetBoolean();
        if (maxHp <= 0 || hp < 0 || hp > maxHp || map > 3 || safeProjection && (map != 0 || hp != maxHp || boss))
            throw new InvalidDataException("Invalid progress projection.");
    }

    internal static void Keys(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Expected a persistence object.");
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
            if (!names.Add(property.Name) || !expected.Contains(property.Name, StringComparer.Ordinal))
                throw new InvalidDataException("Unexpected or duplicate persistence field.");
        if (names.Count != expected.Length)
            throw new InvalidDataException("Incomplete persistence object.");
    }

    internal static CharacterClass CharacterClassValue(byte value) => value switch
    {
        0 => CharacterClass.Knight,
        1 => CharacterClass.Mage,
        _ => throw new InvalidDataException("Invalid stored class.")
    };

    static Guid GuidValue(JsonElement value)
    {
        string? text = value.GetString();
        if (!Guid.TryParseExact(text, "D", out Guid guid) || guid == Guid.Empty || text != guid.ToString("D"))
            throw new InvalidDataException("Invalid persistence identity.");
        return guid;
    }

    static long Counter(JsonElement value)
    {
        string? text = value.GetString();
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long result) ||
            result < 0 || result.ToString(CultureInfo.InvariantCulture) != text)
            throw new InvalidDataException("Invalid persistence counter.");
        return result;
    }

    static PersistenceToken HexToken(JsonElement value)
    {
        string? text = value.GetString();
        if (text is null || text.Length != 16 || text.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidDataException("Invalid persistence token.");
        return new PersistenceToken(Convert.FromHexString(text));
    }

    static float Real(JsonElement value)
    {
        string? text = value.GetString();
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ||
            !float.IsFinite(result))
            throw new InvalidDataException("Invalid persistence coordinate.");
        return result;
    }

    internal static void RequireNull(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Null)
            throw new InvalidDataException("Unexpected persistence value.");
    }
}
