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
        // The factory owns empty failure fields; this World-owned lifetime still owns sending.
        IPacket reply = CharacterMenuReplyFactory.Failure(
            requestId, requestPacketId, result, CharacterMenuInvalidField.None);
        ArraySegment<byte> payload = reply.Write();
        if (!session.IsClosing) session.Send(payload);
    }
}
