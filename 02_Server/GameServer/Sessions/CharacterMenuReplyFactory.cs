using Shared.Protocol;
using Summary = (System.Guid CharacterId, byte Slot, string Name, Shared.Protocol.CharacterClass Class);

namespace Dawnholder.Server.GameServer.Sessions;

// Projects catalog summary values into wire replies. Core owns name policy and business
// outcomes; the caller owns request lifetime, sending and admission. No state is retained here.
internal static class CharacterMenuReplyFactory
{
    const int MaximumCharacters = 3;
    const int MaximumNameBytes = 24;

    internal static S_CharacterListResult ListSuccess(uint requestId, IReadOnlyList<Summary>? characters)
    {
        RequireRequestId(requestId);
        if (characters is null || characters.Count > MaximumCharacters)
            return EmptyList(requestId, CharacterMenuResult.IntegrityFailure);

        S_CharacterListResult reply = EmptyList(requestId, CharacterMenuResult.Success);
        HashSet<Guid> characterIds = new();
        int count = characters.Count;
        for (int index = 0; index < count; index++)
        {
            Summary character = characters[index];
            if (!IsValidSummary(character))
                return EmptyList(requestId, CharacterMenuResult.IntegrityFailure);

            byte slotMask = (byte)(1 << (character.Slot - 1));
            if ((reply.occupiedMask & slotMask) != 0 || !characterIds.Add(character.CharacterId))
                return EmptyList(requestId, CharacterMenuResult.IntegrityFailure);

            // A damaged later row discards this entire local reply, never a partial success.
            // Knight is class zero too; only the mask distinguishes occupied and empty slots.
            reply.occupiedMask |= slotMask;
            string characterIdD = character.CharacterId.ToString("D");
            switch (character.Slot)
            {
                case 1:
                    reply.slot1CharacterIdD = characterIdD;
                    reply.slot1Name = character.Name;
                    reply.slot1ClassId = (byte)character.Class;
                    break;
                case 2:
                    reply.slot2CharacterIdD = characterIdD;
                    reply.slot2Name = character.Name;
                    reply.slot2ClassId = (byte)character.Class;
                    break;
                case 3:
                    reply.slot3CharacterIdD = characterIdD;
                    reply.slot3Name = character.Name;
                    reply.slot3ClassId = (byte)character.Class;
                    break;
            }
        }

        return reply;
    }

    internal static S_CharacterCreateResult CreateSuccess(uint requestId, Summary character)
    {
        RequireRequestId(requestId);
        if (!IsValidSummary(character))
            return EmptyCreate(requestId, CharacterMenuResult.IntegrityFailure);

        S_CharacterCreateResult reply = EmptyCreate(requestId, CharacterMenuResult.Success);
        reply.slot = character.Slot;
        reply.characterIdD = character.CharacterId.ToString("D");
        reply.name = character.Name;
        reply.classId = (byte)character.Class;
        return reply;
    }

    internal static S_CharacterSelectResult SelectSuccess(uint requestId, Guid characterId)
    {
        RequireRequestId(requestId);
        if (characterId == Guid.Empty)
            return EmptySelect(requestId, CharacterMenuResult.IntegrityFailure);

        S_CharacterSelectResult reply = EmptySelect(requestId, CharacterMenuResult.Success);
        reply.selectedCharacterIdD = characterId.ToString("D");
        return reply;
    }

    internal static IPacket Failure(uint requestId, PacketID requestPacketId,
        CharacterMenuResult result, CharacterMenuInvalidField invalidField)
    {
        RequireRequestId(requestId);
        // These are already public wire results, not ordinal casts from Core status values.
        if (result == CharacterMenuResult.Success || (byte)result > (byte)CharacterMenuResult.LegacyFlowOnly)
            result = CharacterMenuResult.IntegrityFailure;

        if (result == CharacterMenuResult.InvalidInput
            && invalidField is not (CharacterMenuInvalidField.Slot or CharacterMenuInvalidField.Name
                or CharacterMenuInvalidField.Class or CharacterMenuInvalidField.CharacterId))
        {
            // An unknown/server field must not be guessed to be a user's name or slot error.
            result = CharacterMenuResult.IntegrityFailure;
        }
        if (result != CharacterMenuResult.InvalidInput) invalidField = CharacterMenuInvalidField.None;

        return requestPacketId switch
        {
            PacketID.C_CharacterListRequest => EmptyList(requestId, result, invalidField),
            PacketID.C_CharacterCreateRequest => EmptyCreate(requestId, result, invalidField),
            PacketID.C_CharacterSelectRequest => EmptySelect(requestId, result, invalidField),
            _ => throw new ArgumentOutOfRangeException(nameof(requestPacketId), requestPacketId,
                "A character menu reply requires a character menu request."),
        };
    }

    static void RequireRequestId(uint requestId)
    {
        if (requestId == 0)
            throw new ArgumentOutOfRangeException(nameof(requestId), requestId, "A reply requires a nonzero request ID.");
    }

    static bool IsValidSummary(Summary character)
    {
        if (character.CharacterId == Guid.Empty || character.Slot is < 1 or > MaximumCharacters
            || character.Class is not (CharacterClass.Knight or CharacterClass.Mage))
        {
            return false;
        }

        string? name = character.Name;
        if (string.IsNullOrEmpty(name) || name.Length > MaximumNameBytes / sizeof(char)) return false;

        // Wire names are BMP UTF-16 (two bytes per code unit). Reject malformed UTF-16
        // and non-BMP pairs without normalizing, trimming or repeating Core's name policy.
        foreach (char codeUnit in name)
        {
            if (char.IsSurrogate(codeUnit)) return false;
        }
        return true;
    }

    // Generated writers require non-null strings. Every failure starts with empty success
    // fields, including class zero; there is no independent reserved field in these replies.
    static S_CharacterListResult EmptyList(uint requestId, CharacterMenuResult result,
        CharacterMenuInvalidField invalidField = CharacterMenuInvalidField.None) => new()
    {
        requestId = requestId,
        result = (byte)result,
        invalidField = (byte)invalidField,
        occupiedMask = 0,
        slot1CharacterIdD = string.Empty,
        slot1Name = string.Empty,
        slot1ClassId = 0,
        slot2CharacterIdD = string.Empty,
        slot2Name = string.Empty,
        slot2ClassId = 0,
        slot3CharacterIdD = string.Empty,
        slot3Name = string.Empty,
        slot3ClassId = 0,
    };

    static S_CharacterCreateResult EmptyCreate(uint requestId, CharacterMenuResult result,
        CharacterMenuInvalidField invalidField = CharacterMenuInvalidField.None) => new()
    {
        requestId = requestId,
        result = (byte)result,
        invalidField = (byte)invalidField,
        slot = 0,
        characterIdD = string.Empty,
        name = string.Empty,
        classId = 0,
    };

    static S_CharacterSelectResult EmptySelect(uint requestId, CharacterMenuResult result,
        CharacterMenuInvalidField invalidField = CharacterMenuInvalidField.None) => new()
    {
        requestId = requestId,
        result = (byte)result,
        invalidField = (byte)invalidField,
        selectedCharacterIdD = string.Empty,
    };
}
