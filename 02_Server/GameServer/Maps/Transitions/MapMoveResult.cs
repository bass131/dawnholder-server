namespace Dawnholder.Server.GameServer.Maps.Transitions;

internal enum MapMoveResult
{
    Accepted,
    SessionClosing,
    AlreadyMigrating,
    NotInThatMap,
    NotInInstanceMap,
    DestinationNotPublic,
}
