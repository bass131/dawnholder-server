using System.Numerics;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>Respawn coordinates only; each map's system owns its queue and alternating side.</summary>
internal readonly record struct EnemyRespawnPlacement(Vector2 GolemLeft, Vector2 GolemRight)
{
    internal static EnemyRespawnPlacement Default => new(new Vector2(-8.5f, 0f), new Vector2(9.5f, 0f));
}
