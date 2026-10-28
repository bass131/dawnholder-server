using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

// One connection's replay boundary and pending token. The session's World queue owns every
// operation, including Close; expiration is settled by the owner, never by replacing its budget.
internal sealed class CharacterMenuRequestLifetime
{
    uint _highWater;
    long _generation;
    CharacterMenuRequestTicket? _pending;
    bool _closed;

    internal CharacterMenuRequestBeginResult Begin(
        uint requestId, ushort requestPacketId, PersistenceDeadline deadline)
    {
        ArgumentNullException.ThrowIfNull(deadline);
        if (_closed) return new(CharacterMenuRequestBeginStatus.Closed);
        if (requestPacketId != (ushort)PacketID.C_CharacterListRequest &&
            requestPacketId != (ushort)PacketID.C_CharacterCreateRequest &&
            requestPacketId != (ushort)PacketID.C_CharacterSelectRequest)
        {
            return new(CharacterMenuRequestBeginStatus.Ignored);
        }
        if (requestId == 0 || requestId <= _highWater)
            return new(CharacterMenuRequestBeginStatus.Ignored);

        // A new rejected request is still observed. It must not become replayable when the
        // original pending request completes, nor may it replace that request's token/deadline.
        _highWater = requestId;
        if (_pending is not null) return new(CharacterMenuRequestBeginStatus.InProgress);

        // Strictly increasing uint IDs bound the number of generations below long.MaxValue.
        _pending = new CharacterMenuRequestTicket(requestId, requestPacketId, ++_generation, deadline);
        return new(CharacterMenuRequestBeginStatus.Started, _pending);
    }

    internal bool IsCurrent(CharacterMenuRequestTicket ticket)
        => !_closed && _pending is not null && ReferenceEquals(_pending, ticket);

    internal bool Complete(CharacterMenuRequestTicket ticket)
    {
        if (!IsCurrent(ticket)) return false;
        _pending = null;
        return true;
    }

    internal void Close()
    {
        _closed = true;
        _pending = null;
    }
}
