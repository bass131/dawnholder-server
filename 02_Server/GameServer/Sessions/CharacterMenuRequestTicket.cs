using Dawnholder.Server.GameServer.Persistence;

namespace Dawnholder.Server.GameServer.Sessions;

// Only CharacterMenuRequestLifetime issues tickets. Identity, not matching field values,
// authorizes completion; a ticket from another connection can never clear this pending request.
internal sealed class CharacterMenuRequestTicket
{
    internal CharacterMenuRequestTicket(
        uint requestId, ushort requestPacketId, long generation, PersistenceDeadline deadline)
    {
        RequestId = requestId;
        RequestPacketId = requestPacketId;
        Generation = generation;
        Deadline = deadline;
    }

    internal uint RequestId { get; }
    internal ushort RequestPacketId { get; }
    internal long Generation { get; }
    internal PersistenceDeadline Deadline { get; }
}
