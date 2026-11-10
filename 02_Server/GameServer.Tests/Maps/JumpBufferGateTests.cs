using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Shared.GameData;

namespace GameServer.Tests.Maps;

public sealed class JumpBufferGateTests
{
    [Theory]
    [InlineData("attack")]
    [InlineData("hit")]
    public void BufferedJump_LockOnLanding_ClearsBeforeUnlock(string state)
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = BufferJump(map, physics, 0.2f);
        LockForOneTick(player, state);

        physics.Step(map, 2);

        Assert.True(player.OnGround);
        Assert.Equal(0, player.Velocity.Y);
        Assert.False(player.HasBufferedJump);
        Assert.False(player.ActionFsm.CurrentState.LocksMovement);
        physics.Step(map, 3);
        Assert.True(player.OnGround);
        Assert.Equal(0, player.Velocity.Y);
        AssertFreshJumpWorks(map, physics, player, 4);
    }

    [Theory]
    [InlineData("attack")]
    [InlineData("hit")]
    public void BufferedJump_AirborneLock_DiscardsBeforeAnyLanding(string state)
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = BufferJump(map, physics, 10);
        LockForOneTick(player, state);

        physics.Step(map, 2);

        Assert.False(player.OnGround);
        Assert.False(player.HasBufferedJump);
        Assert.False(player.ActionFsm.CurrentState.LocksMovement);
        PutOnGround(player);
        physics.Step(map, 3);
        Assert.True(player.OnGround);
        Assert.Equal(0, player.Velocity.Y);
    }

    [Theory]
    [InlineData("attack")]
    [InlineData("hit")]
    public void FreshJump_LastLockedTick_IsConsumedWithoutCreatingBuffer(string state)
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = map.AddPlayer(null, Vector2.Zero);
        PutOnGround(player);
        LockForOneTick(player, state);
        player.EnqueueInput(0, true, 1);

        physics.Step(map, 1);

        Assert.True(player.OnGround);
        Assert.Equal(0, player.Velocity.Y);
        Assert.False(player.HasBufferedJump);
        Assert.Equal(0, player.InputQueueCount);
        Assert.Equal(1u, player.LastClientTick);
        Assert.False(player.ActionFsm.CurrentState.LocksMovement);
        physics.Step(map, 2);
        Assert.True(player.OnGround);
        AssertFreshJumpWorks(map, physics, player, 3);
    }

    [Fact]
    public void Unlocked_AirborneBuffer_FiresOnceOnLanding()
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = BufferJump(map, physics, 0.2f);

        physics.Step(map, 2);
        Assert.True(player.OnGround);
        Assert.True(player.HasBufferedJump);
        physics.Step(map, 3);
        Assert.False(player.OnGround);
        Assert.True(player.Velocity.Y > 0);
        Assert.False(player.HasBufferedJump);
        PutOnGround(player);
        physics.Step(map, 4);
        Assert.True(player.OnGround);
        Assert.Equal(0, player.Velocity.Y);
    }

    [Fact]
    public void Unlocked_BufferExpiresOnThirdNeutralAirborneTick()
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = BufferJump(map, physics, 10);

        physics.Step(map, 2);
        Assert.True(player.HasBufferedJump);
        physics.Step(map, 3);
        Assert.True(player.HasBufferedJump);
        physics.Step(map, 4);
        Assert.False(player.OnGround);
        Assert.False(player.HasBufferedJump);
        PutOnGround(player);
        physics.Step(map, 5);
        Assert.True(player.OnGround);
        Assert.Equal(0, player.Velocity.Y);
    }

    private static PlayerEntity BufferJump(GameMap map, PlayerPhysicsSystem physics, float height)
    {
        PlayerEntity player = map.AddPlayer(null, new Vector2(0, height));
        player.OnGround = false;
        player.Velocity = new Vector2(0, -1);
        player.EnqueueInput(0, true, 1);
        physics.Step(map, 1);
        Assert.False(player.OnGround);
        Assert.True(player.HasBufferedJump);
        Assert.True(player.Velocity.Y < 0);
        return player;
    }

    private static void LockForOneTick(PlayerEntity player, string state)
    {
        if (state == "attack") player.EnterAttackState();
        else player.EnterHitState(dirX: 1);
        // Make this the final locked tick, so deferred old input cannot hide behind a longer lock.
        player.StateTicksRemaining = 1;
        Assert.True(player.ActionFsm.CurrentState.LocksMovement);
    }

    private static void PutOnGround(PlayerEntity player)
    {
        player.Position = Vector2.Zero;
        player.Velocity = Vector2.Zero;
        player.OnGround = true;
    }

    private static void AssertFreshJumpWorks(GameMap map, PlayerPhysicsSystem physics, PlayerEntity player, uint tick)
    {
        player.EnqueueInput(0, true, tick);
        physics.Step(map, tick);
        Assert.False(player.OnGround);
        Assert.True(player.Velocity.Y > 0);
        Assert.False(player.HasBufferedJump);
    }
}
