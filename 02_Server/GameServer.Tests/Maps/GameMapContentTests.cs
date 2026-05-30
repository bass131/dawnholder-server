using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 맵별 콘텐츠(enemy spawn) 단위 테스트.
// spawn 정의 확인은 아래 헬퍼를 통해 MapSpawnTable 단일 진실 공급원에서 가져옴.
//
// **검증 범위**:
//   - Town:          Enemies.Count == 0 (빈 맵)
//   - HuntingGround: Enemies.Count == 1, 첫 enemy = EnemyKind.Normal
//   - BossRoom:      Enemies.Count == 1, 첫 enemy = EnemyKind.Boss
//   - Ending:        Enemies.Count == 0 (빈 맵)
//
// GameMap은 GameWorld 없이 독립 사용 가능 (actor 패턴) → 싱글톤 의존 X.
public class GameMapContentTests
{
    // MapSpawnTable 단일 진실 공급원에서 spawn 정의 추출 (헬퍼).
    static readonly EnemySpawnDef NormalDef = MapSpawnTable.GetSpawnsFor(MapId.HuntingGround)[0];
    static readonly EnemySpawnDef BossDef   = MapSpawnTable.GetSpawnsFor(MapId.BossRoom)[0];
    [Fact]
    public void Town_HasNoEnemies()
    {
        GameMap map = new GameMap(MapId.Town);
        Assert.Empty(map.Enemies);
    }

    [Fact]
    public void HuntingGround_HasOneNormalEnemy()
    {
        GameMap map = new GameMap(MapId.HuntingGround);
        EnemyEntity enemy = Assert.Single(map.Enemies.Values);

        Assert.Equal(EnemyKind.Normal, enemy.Kind);
        Assert.Equal(NormalDef.X, enemy.X);
        Assert.Equal(NormalDef.Y, enemy.Y);
        Assert.Equal(NormalDef.MaxHp, enemy.MaxHp);
    }

    [Fact]
    public void BossRoom_HasOneBossEnemy()
    {
        GameMap map = new GameMap(MapId.BossRoom);
        EnemyEntity enemy = Assert.Single(map.Enemies.Values);

        Assert.Equal(EnemyKind.Boss, enemy.Kind);
        Assert.Equal(BossDef.X, enemy.X);
        Assert.Equal(BossDef.Y, enemy.Y);
        Assert.Equal(BossDef.MaxHp, enemy.MaxHp);
    }

    [Fact]
    public void Ending_HasNoEnemies()
    {
        GameMap map = new GameMap(MapId.Ending);
        Assert.Empty(map.Enemies);
    }

    [Fact]
    public void HuntingGround_NormalEnemy_EntityId_Is_One()
    {
        // entity id 풀은 맵별 독립 (_nextEntityId=1부터). Normal = 첫 번째 발급 = 1.
        GameMap map = new GameMap(MapId.HuntingGround);
        EnemyEntity normal = map.Enemies.Values.Single();
        Assert.Equal(1, normal.EntityId);
    }

    [Fact]
    public void BossRoom_Boss_EntityId_Is_One()
    {
        // BossRoom도 독립 풀 → Boss = 첫 번째 발급 = 1.
        GameMap map = new GameMap(MapId.BossRoom);
        EnemyEntity boss = map.Enemies.Values.Single();
        Assert.Equal(1, boss.EntityId);
    }
}
