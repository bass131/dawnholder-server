using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

/// <summary>
/// PlayerEntity.CaptureSnapshot() 단위 테스트.
///
/// 검증 목표:
///   (a) 저장 후보 필드(EntityId / Position / CurrentHp / MaxHp / Stats)가 스냅샷에 정확히 담기는지.
///   (b) 전투 중 HP 변화 후에도 스냅샷이 변경 시점의 값을 정확히 포착하는지.
///   (c) 휘발 상태(Velocity / OnGround)가 스냅샷에 존재하지 않음을 컴파일 타임에 보장
///       — PlayerSnapshot 타입 자체에 해당 필드가 없으므로 별도 런타임 검증 불필요.
/// </summary>
public class PlayerSnapshotTests
{
    [Theory]
    [InlineData(CharacterClass.Knight)]
    [InlineData(CharacterClass.Mage)]
    public void TwoCaptures_RemainIndependent_AfterFurtherEntityMutation(CharacterClass characterClass)
    {
        PlayerStats definition = PlayerStats.ForClass(characterClass);
        PlayerEntity entity = new(42, new Vector2(3, 4), stats: definition);
        entity.Hp = -3;
        entity.MaxHp = 213;
        PlayerSnapshot first = entity.CaptureSnapshot();
        entity.Position = new Vector2(11, 12);
        entity.Hp = 301;
        entity.MaxHp = 177;
        PlayerSnapshot second = entity.CaptureSnapshot();
        entity.Position = new Vector2(-30, -40);
        entity.Hp = 1;
        entity.MaxHp = 2;
        Assert.Equal(42, first.EntityId);
        Assert.Equal(new Vector2(3, 4), first.Position);
        Assert.Equal(-3, first.CurrentHp);
        Assert.Equal(213, first.MaxHp);
        Assert.Equal(42, second.EntityId);
        Assert.Equal(new Vector2(11, 12), second.Position);
        Assert.Equal(301, second.CurrentHp);
        Assert.Equal(177, second.MaxHp);
        Assert.Same(definition, first.Stats);
        Assert.Same(definition, second.Stats);
        Assert.Equal(characterClass, first.Stats.Class);
        Assert.Equal(characterClass == CharacterClass.Mage ? 80 : 150, first.Stats.InitialHp);
        Assert.Equal(first.Stats.InitialHp, first.Stats.MaxHp);
    }

    [Fact]
    public void CaptureSnapshot_ReflectsCurrentPersistenceState()
    {
        // Arrange: Knight(HP=150, MaxHp=150) 엔티티를 특정 위치에 생성.
        var spawnPos = new Vector2(3.5f, 0f);
        var stats = PlayerStats.Knight();
        var entity = new PlayerEntity(entityId: 42, position: spawnPos, owner: null, stats: stats);

        // 전투 시뮬레이션: HP를 서버 권위로 변경.
        entity.Hp = 80;

        // 휘발 상태도 변경 — 스냅샷에 영향 없어야 함.
        entity.Velocity = new Vector2(5f, -2f);
        entity.OnGround = false;

        // Act
        PlayerSnapshot snap = entity.CaptureSnapshot();

        // Assert — 저장 후보 필드 일치
        Assert.Equal(42, snap.EntityId);
        Assert.Equal(spawnPos, snap.Position);
        Assert.Equal(80, snap.CurrentHp);
        Assert.Equal(150, snap.MaxHp);
        Assert.Equal(CharacterClass.Knight, snap.Stats.Class);
        Assert.Equal(15, snap.Stats.Attack);
        Assert.Equal(5, snap.Stats.Defense);
    }

    [Fact]
    public void CaptureSnapshot_AfterPositionMove_ReflectsNewPosition()
    {
        // Arrange
        var entity = new PlayerEntity(entityId: 7, position: Vector2.Zero, owner: null);

        // 틱 루프가 Position을 갱신하는 상황 시뮬.
        entity.Position = new Vector2(10f, 2f);

        // Act
        PlayerSnapshot snap = entity.CaptureSnapshot();

        // Assert — 이동 후 좌표가 스냅샷에 반영됨.
        Assert.Equal(new Vector2(10f, 2f), snap.Position);
    }
}
