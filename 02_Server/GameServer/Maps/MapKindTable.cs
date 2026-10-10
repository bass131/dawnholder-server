namespace Dawnholder.Server.GameServer.Maps;

public static class MapKindTable
{
    public static bool IsInstanced(MapId mapId) => mapId switch
    {
        MapId.HuntingGround => true,
        MapId.BossRoom => true,
        _ => false,
    };
}
