using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// 클래스별 권위 전투 HP(전사 150 / 원거리 80)가 <see cref="PlayerEntity"/>의
/// 실제 전투 HP(Hp/MaxHp)에 반영되는지 검증.
///
/// **회귀 방어 대상 결함**: PlayerEntity.Hp/MaxHp가 `= 100` 하드코딩이고 생성자가 Stats를 무시하면
///   클래스 선택이 권위 전투 HP에 미반영(이중 진실: Stats.MaxHp=150인데 전투 MaxHp=100).
///   PlayerEntity 생성자에서 Stats.MaxHp/Hp로 초기화해야 함.
/// </summary>
public class PlayerStatsWiringTests
{
    [Fact]
    public void PlayerEntity_Knight_UsesClassHp()
    {
        var e = new PlayerEntity(1, Vector2.Zero, owner: null, stats: PlayerStats.Knight());

        Assert.Equal(150, e.MaxHp);
        Assert.Equal(150, e.Hp);
    }

    [Fact]
    public void PlayerEntity_Mage_UsesClassHp()
    {
        var e = new PlayerEntity(1, Vector2.Zero, owner: null, stats: PlayerStats.Mage());

        Assert.Equal(80, e.MaxHp);
        Assert.Equal(80, e.Hp);
    }

    [Fact]
    public void GameMap_AddPlayer_AppliesClassMaxHp()
    {
        var map = new GameMap(MapId.Town);

        PlayerEntity e = map.AddPlayer(owner: null, spawnPos: Vector2.Zero, stats: PlayerStats.Mage());

        Assert.Equal(80, e.MaxHp);
        Assert.Equal(80, e.Hp);
    }
}
