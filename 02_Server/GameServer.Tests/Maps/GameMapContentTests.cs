using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 맵별 콘텐츠(enemy spawn) 단위 테스트.
// content 주입 방식 — MapSpawnTable 은퇴 (M4.4 Phase 03).
//
// **검증 범위**:
//   - Town:          content null → Enemies.Count == 0 (빈 맵)
//   - HuntingGround: content 주입 → Normal 1마리
//   - BossRoom:      content 주입 → Boss 1마리
//   - Ending:        content null → Enemies.Count == 0 (빈 맵)
//
// GameMap은 GameWorld 없이 독립 사용 가능 (actor 패턴) → 싱글톤 의존 X.
public class GameMapContentTests
{
    // 옛 MapSpawnTable 값 보존 — inlined.
    const float NormalX    = 10f;
    const float NormalY    = 0f;
    const int   NormalMaxHp = 30;
    const float BossX      = 30f;
    const float BossY      = 0f;
    const int   BossMaxHp   = 150;

    [Fact]
    public void Town_HasNoEnemies()
    {
        GameMap map = new GameMap(MapId.Town);
        Assert.Empty(map.Enemies);
    }

    [Fact]
    public void HuntingGround_HasOneNormalEnemy()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        EnemyEntity enemy = Assert.Single(map.Enemies.Values);

        Assert.Equal(EnemyKind.Normal, enemy.Kind);
        Assert.Equal(NormalX, enemy.X);
        Assert.Equal(NormalY, enemy.Y);
        Assert.Equal(NormalMaxHp, enemy.MaxHp);
    }

    [Fact]
    public void BossRoom_HasOneBossEnemy()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, BossX, BossY),
        });
        GameMap map = new GameMap(MapId.BossRoom, content: content);
        EnemyEntity enemy = Assert.Single(map.Enemies.Values);

        Assert.Equal(EnemyKind.Boss, enemy.Kind);
        Assert.Equal(BossX, enemy.X);
        Assert.Equal(BossY, enemy.Y);
        Assert.Equal(BossMaxHp, enemy.MaxHp);
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
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        EnemyEntity normal = map.Enemies.Values.Single();
        Assert.Equal(1, normal.EntityId);
    }

    [Fact]
    public void BossRoom_Boss_EntityId_Is_One()
    {
        // BossRoom도 독립 풀 → Boss = 첫 번째 발급 = 1.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, BossX, BossY),
        });
        GameMap map = new GameMap(MapId.BossRoom, content: content);
        EnemyEntity boss = map.Enemies.Values.Single();
        Assert.Equal(1, boss.EntityId);
    }
}
