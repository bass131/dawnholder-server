namespace Shared.GameData;

/// <summary>
/// 맵 콘텐츠 데이터 단위 — 서버 전용. 클라 빌드에는 content.bin 자체가 배포되지 않음 (D1).
/// kindId는 raw byte — EnemyKind 해석은 서버 권위 (헌법 #1).
/// </summary>
public readonly struct EnemySpawnPoint
{
    public readonly byte KindId;
    public readonly float X;
    public readonly float Y;

    public EnemySpawnPoint(byte kindId, float x, float y)
    {
        KindId = kindId;
        X = x;
        Y = y;
    }
}

/// <summary>
/// 맵 1개의 콘텐츠 데이터 — 플레이어 스폰 좌표 + 적 스폰 목록.
/// 방어 복사: 외부 배열 참조를 보관하지 않아 외부 변조를 차단.
/// </summary>
public sealed class MapContent
{
    public readonly float PlayerSpawnX;
    public readonly float PlayerSpawnY;

    public static readonly MapContent Empty
        = new MapContent(0f, 0f, System.Array.Empty<EnemySpawnPoint>());

    private readonly EnemySpawnPoint[] _enemies;

    public MapContent(float playerSpawnX, float playerSpawnY, EnemySpawnPoint[] enemies)
    {
        PlayerSpawnX = playerSpawnX;
        PlayerSpawnY = playerSpawnY;
        // 방어 복사 — 호출자가 배열을 나중에 수정해도 이 객체 상태는 불변.
        _enemies = enemies == null || enemies.Length == 0
            ? System.Array.Empty<EnemySpawnPoint>()
            : (EnemySpawnPoint[])enemies.Clone();
    }

    public System.ReadOnlySpan<EnemySpawnPoint> Enemies => _enemies;
}
