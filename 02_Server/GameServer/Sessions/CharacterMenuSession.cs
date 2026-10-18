using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

// Composed by one GameSession. Its World queue owns this lifetime and emits Legacy-only
// guidance. This adapter has no Stored/Catalog connection; old admission persistence is not
// a menu mode. A Stored host must not route its failures through this Legacy policy.
internal sealed class CharacterMenuSession
{
    readonly CharacterMenuRequestLifetime _requests = new();

    internal void HandleLegacyRequest(
        GameSession session, uint requestId, PacketID requestPacketId,
        PersistenceDeadline deadline, bool wasSelecting)
    {
        if (session.IsClosing)
        {
            Close();
            return;
        }

        CharacterMenuRequestBeginResult begin = _requests.Begin(requestId, (ushort)requestPacketId, deadline);
        if (begin.Status is CharacterMenuRequestBeginStatus.Ignored or CharacterMenuRequestBeginStatus.Closed)
            return;
        if (begin.Status == CharacterMenuRequestBeginStatus.InProgress)
        {
            SendFailure(session, requestId, requestPacketId, CharacterMenuResult.RequestInProgress);
            return;
        }

        CharacterMenuRequestTicket ticket = begin.Ticket!;
        try
        {
            // Recheck the owner after queuing, while preserving a rejection at receipt
            // (a later login must not authorize an earlier menu request).
            CharacterMenuResult rejection = wasSelecting && session.CanUseCharacterMenu
                ? CharacterMenuResult.LegacyFlowOnly
                : CharacterMenuResult.InvalidState;
            if (_requests.IsCurrent(ticket)) SendFailure(session, requestId, requestPacketId, rejection);
        }
        finally
        {
            // A failed send still consumes the ID; it does not retry or leave synchronous
            // guidance pending. Only this original token can release the pending request.
            _requests.Complete(ticket);
        }
    }

    internal void Close() => _requests.Close();

    static void SendFailure(GameSession session, uint requestId, PacketID requestPacketId, CharacterMenuResult result)
    {
        // Empty fields are explicit: generated writers require non-null strings. These
        // packets never represent a successful list/create/select and never register a player.
        IPacket reply = requestPacketId switch
        {
            PacketID.C_CharacterListRequest => new S_CharacterListResult
            {
                requestId = requestId,
                result = (byte)result,
                invalidField = (byte)CharacterMenuInvalidField.None,
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
            },
            PacketID.C_CharacterCreateRequest => new S_CharacterCreateResult
            {
                requestId = requestId,
                result = (byte)result,
                invalidField = (byte)CharacterMenuInvalidField.None,
                slot = 0,
                characterIdD = string.Empty,
                name = string.Empty,
                classId = 0,
            },
            PacketID.C_CharacterSelectRequest => new S_CharacterSelectResult
            {
                requestId = requestId,
                result = (byte)result,
                invalidField = (byte)CharacterMenuInvalidField.None,
                selectedCharacterIdD = string.Empty,
            },
            _ => throw new InvalidOperationException("A menu ticket must identify a menu request."),
        };
        ArraySegment<byte> payload = reply.Write();
        if (!session.IsClosing) session.Send(payload);
    }
}
