namespace Dawnholder.Server.GameServer.Sessions;

internal readonly struct CharacterMenuRequestBeginResult
{
    internal CharacterMenuRequestBeginResult(
        CharacterMenuRequestBeginStatus status, CharacterMenuRequestTicket? ticket = null)
    {
        Status = status;
        Ticket = ticket;
    }

    internal CharacterMenuRequestBeginStatus Status { get; }
    internal CharacterMenuRequestTicket? Ticket { get; }
}
