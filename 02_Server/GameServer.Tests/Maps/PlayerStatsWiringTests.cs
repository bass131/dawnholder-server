using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps.Transitions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// 클래스별 권위 전투 HP(전사 150 / 원거리 80)가 <see cref="PlayerEntity"/>의
/// 실제 전투 HP(Hp/MaxHp)에 반영되는지 검증.
///
/// **회귀 방어 대상 결함**: PlayerEntity.Hp/MaxHp가 `= 100` 하드코딩이고 생성자가 Stats를 무시하면
///   클래스 선택이 권위 전투 HP에 미반영(이중 진실: Stats.MaxHp=150인데 전투 MaxHp=100).
///   PlayerEntity 생성자에서 Stats.MaxHp/InitialHp로 초기화해야 함.
/// </summary>
public class PlayerStatsWiringTests
{
    [Theory]
    [InlineData(CharacterClass.Knight, CharacterClass.Knight, 150, 15, 5, 4f)]
    [InlineData(CharacterClass.Mage, CharacterClass.Mage, 80, 12, 2, 6f)]
    [InlineData((CharacterClass)255, CharacterClass.Knight, 150, 15, 5, 4f)]
    public void Definition_PreservesClassDefaultsAndFallback(CharacterClass requested, CharacterClass expected,
        int hp, int attack, int defense, float speed)
    {
        PlayerStats stats = PlayerStats.ForClass(requested);
        Assert.Equal(expected, stats.Class);
        Assert.Equal(hp, stats.InitialHp);
        Assert.Equal(hp, stats.MaxHp);
        Assert.Equal(attack, stats.Attack);
        Assert.Equal(defense, stats.Defense);
        Assert.Equal(speed, stats.MoveSpeed);
        Assert.Equal(8f, stats.JumpVel);
    }

    [Fact]
    public void Definition_PublicSurfaceCannotMutateSharedState()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance;
        Assert.True(typeof(PlayerStats).IsSealed);
        Assert.Null(typeof(PlayerStats).GetProperty("Hp", flags));
        Assert.All(typeof(PlayerStats).GetProperties(flags), property =>
        {
            Assert.Null(property.GetSetMethod(nonPublic: true));
            Assert.True(property.PropertyType.IsValueType);
        });
        Assert.All(typeof(PlayerStats).GetFields(flags), field =>
        {
            Assert.True(field.IsInitOnly);
            Assert.True(field.FieldType.IsValueType);
        });
    }

    [Theory]
    [InlineData(CharacterClass.Knight, 150)]
    [InlineData(CharacterClass.Mage, 80)]
    public void SharedDefinition_DoesNotShareTwoEntitiesCurrentHp(CharacterClass characterClass, int initialHp)
    {
        PlayerStats stats = PlayerStats.ForClass(characterClass);
        PlayerEntity first = new(1, Vector2.Zero, stats: stats);
        PlayerEntity second = new(2, Vector2.One, stats: stats);
        first.Hp = -5;
        first.MaxHp = 321;
        Assert.Same(first.Stats, second.Stats);
        Assert.Equal(initialHp, second.Hp);
        Assert.Equal(initialHp, second.MaxHp);
        Assert.Equal(initialHp, stats.InitialHp);
        Assert.Equal(initialHp, stats.MaxHp);
        second.Hp = 7;
        Assert.Equal(-5, first.Hp);
        Assert.Equal(321, first.MaxHp);
    }

    [Theory]
    [InlineData(false, -5)]
    [InlineData(false, 0)]
    [InlineData(false, 221)]
    [InlineData(true, -5)]
    [InlineData(true, 0)]
    [InlineData(true, 221)]
    public void Transfer_PreservesRawHp_AndNullStatsUsesKnightFallback(bool nullStats, int rawHp)
    {
        GameMap map = new(MapId.Town);
        PlayerStats? stats = nullStats ? null : PlayerStats.Mage();
        PlayerEntity player = map.AddPlayerWithId(new PlayerTransferState(42, stats, rawHp), null, new Vector2(7, 9));
        Assert.Equal(42, player.EntityId);
        Assert.Equal(new Vector2(7, 9), player.Position);
        Assert.Equal(rawHp, player.Hp);
        Assert.Equal(nullStats ? 150 : 80, player.MaxHp);
        Assert.Equal(nullStats ? CharacterClass.Knight : CharacterClass.Mage, player.Stats.Class);
        if (stats != null) Assert.Same(stats, player.Stats);
    }

    [Fact]
    public void DefaultTransfer_PreservesExistingZeroIdAndHpFallback_WithoutClaimingValidatedInput()
    {
        GameMap map = new(MapId.Town);
        PlayerEntity player = map.AddPlayerWithId(default, null, new Vector2(4, 5));
        Assert.Equal(0, player.EntityId);
        Assert.Equal(0, player.Hp);
        Assert.Equal(150, player.MaxHp);
        Assert.Equal(CharacterClass.Knight, player.Stats.Class);
        Assert.Equal(new Vector2(4, 5), player.Position);
    }

    [Theory]
    [InlineData(CharacterClass.Knight, 150)]
    [InlineData(CharacterClass.Mage, 80)]
    public void DeathRecovery_UsesDefinitionMaxHp_WithoutChangingRuntimeMaxHp(CharacterClass characterClass, int expectedHp)
    {
        GameMap map = new(MapId.Town);
        PlayerEntity player = map.AddPlayer(null, Vector2.One, PlayerStats.ForClass(characterClass));
        player.Hp = -7;
        player.MaxHp = 300;
        map.HandlePlayerDeath(player);
        Assert.Equal(expectedHp, player.Hp);
        Assert.Equal(300, player.MaxHp);
        Assert.Equal(map.PlayerSpawnPosition, player.Position);
    }

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
