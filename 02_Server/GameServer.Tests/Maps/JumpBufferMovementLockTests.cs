using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Maps.Systems;
using Shared.GameData;
using Xunit.Abstractions;

namespace GameServer.Tests.Maps;

// Airborne jump input may buffer until landing, but cannot launch during
// an attack or hit lock. Observe the real physics/FSM output, not a copied formula.
public sealed class JumpBufferMovementLockTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("attack")]
    [InlineData("hit")]
    public void BufferedJump_LandingDuringLockedState_DoesNotLaunch(string state)
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = BufferAirborneJump(map, physics);
        switch (state)
        {
            case "attack":
                player.EnterAttackState();
                break;
            case "hit":
                player.EnterHitState(dirX: 1);
                break;
        }
        long groundedTick = Land(map, physics, player);
        string stateAtLanding = player.ActionFsm.CurrentState.GetType().Name;
        bool declaredMovementLock = player.ActionFsm.CurrentState.LocksMovement;
        physics.Step(map, groundedTick + 1);
        output.WriteLine($"requestedState={state}, landingState={stateAtLanding}, "
            + $"declaredLock={declaredMovementLock}, groundedTick={groundedTick}, "
            + $"velocityY={player.Velocity.Y}, onGround={player.OnGround}, positionY={player.Position.Y}");

        Assert.True(player.Velocity.Y <= 0, $"Buffered jump launched upward during {state}: vy={player.Velocity.Y}");
        Assert.True(player.OnGround, $"Buffered jump left the ground during {state}");
    }

    [Fact]
    public void BufferedJump_LandingWithoutLock_StillLaunches()
    {
        var map = new GameMap(MapId.Town);
        var physics = new PlayerPhysicsSystem();
        PlayerEntity player = BufferAirborneJump(map, physics);
        long groundedTick = Land(map, physics, player);
        Assert.False(player.ActionFsm.CurrentState.LocksMovement);

        physics.Step(map, groundedTick + 1);

        Assert.True(player.Velocity.Y > 0, "Buffered airborne input must launch once ground is available");
        Assert.False(player.OnGround);
    }

    private static PlayerEntity BufferAirborneJump(GameMap map, PlayerPhysicsSystem physics)
    {
        PlayerEntity player = map.AddPlayer(null, new Vector2(0, 0.2f));
        player.OnGround = false;
        player.Velocity = new Vector2(0, -1);
        player.EnqueueInput(inputX: 0, jumpPressed: true, clientTick: 1);
        physics.Step(map, 1);

        Assert.False(player.OnGround, "Fixture must buffer while still airborne");
        Assert.True(player.Velocity.Y < 0, "Airborne input must not itself jump");
        Assert.Equal(0, player.InputQueueCount);
        return player;
    }

    private static long Land(GameMap map, PlayerPhysicsSystem physics, PlayerEntity player)
    {
        long tick = 1;
        while (!player.OnGround && tick < 4)
            physics.Step(map, ++tick);
        Assert.True(player.OnGround, "Fixture must reach ground before the buffered input expires");
        Assert.Equal(0, player.Velocity.Y);
        return tick;
    }
}
