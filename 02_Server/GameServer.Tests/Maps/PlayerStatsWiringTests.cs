using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// Cross-review γ10 (β2 봉합 회귀 방어, 2026-05-29):
/// 클래스별 권위 전투 HP(전사 150 / 원거리 80)가 <see cref="PlayerEntity"/>의
/// 실제 전투 HP(Hp/MaxHp)에 반영되는지 검증.
///
/// **옛 결함**: PlayerEntity.Hp/MaxHp가 `= 100` 하드코딩이고 생성자가 Stats를 무시 →
///   클래스 선택이 권위 전투 HP에 미반영(이중 진실: Stats.MaxHp=150인데 전투 MaxHp=100).
///   cross-review β가 발견, α는 놓침. PlayerEntity 생성자에서 Stats.MaxHp/Hp로 초기화하도록 봉합.
/// </summary>
public class PlayerStatsWiringTests
{
    [Fact]
    public void PlayerEntity_Warrior_UsesClassHp()
    {
        var e = new PlayerEntity(1, Vector2.Zero, owner: null, stats: PlayerStats.Warrior());

        Assert.Equal(150, e.MaxHp);
        Assert.Equal(150, e.Hp);
    }

    [Fact]
    public void PlayerEntity_Ranger_UsesClassHp()
    {
        var e = new PlayerEntity(1, Vector2.Zero, owner: null, stats: PlayerStats.Ranger());

        Assert.Equal(80, e.MaxHp);
        Assert.Equal(80, e.Hp);
    }

    [Fact]
    public void GameMap_AddPlayer_AppliesClassMaxHp()
    {
        var map = new GameMap(MapId.Town);

        PlayerEntity e = map.AddPlayer(owner: null, spawnPos: Vector2.Zero, stats: PlayerStats.Ranger());

        Assert.Equal(80, e.MaxHp);
        Assert.Equal(80, e.Hp);
    }
}
