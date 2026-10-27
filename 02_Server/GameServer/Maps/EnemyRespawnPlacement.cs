using System.Numerics;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>Respawn coordinates only; each map's system owns its queue and alternating side.</summary>
internal readonly record struct EnemyRespawnPlacement(Vector2 GolemLeft, Vector2 GolemRight)
{
    // 기본 배치는 당시 1층 좌/우 교차 스폰용 튜닝 값이다.
    // 첫 재출현을 왼쪽으로 정한 이유는 원본 골렘이 중앙 오른쪽에 있어 반대편부터 시작하기 위해서였다.
    internal static EnemyRespawnPlacement Default => new(new Vector2(-8.5f, 0f), new Vector2(9.5f, 0f));

    // World가 만드는 사냥터에만 주입한다. 초기 적 출현 위치는 content가 소유한다.
    internal static EnemyRespawnPlacement HuntingGround => new(new Vector2(26f, 0f), new Vector2(32f, 0f));
}
