using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 복사본의 정리 수명과, 같은 틱 안의 생성·정리·생성 실패.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 4·8, 만들 것 4, goal-review IM-01~05·IM-08·IM-15·IM-16(IM-04는 불변식만).
// 지금 있는 경계만 쓴다. 정리는 「그 맵 객체가 더 이상 Tick되지 않음」으로 본다. 살아 있는 복사본 수는 InstanceMapRegistryTests에 있다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapLifecycleTests : IDisposable
{
    InstanceMapTestWorld? _fixture;

    InstanceMapTestWorld Fixture => _fixture ??= new InstanceMapTestWorld();

    GameMap Town => Fixture.Town;

    public void Dispose() => _fixture?.Dispose();

    // T4 · 완료조건 4, IM-03: 혼자 s가 사냥터에 들어갔다가 사냥터 포탈 2로 마을에 돌아온다(마지막 인원 퇴장).
    [Fact]
    public void LastPlayerLeavingByPortal_RetiresThatHuntingGroundMap()
    {
        InstanceProbeSession s = Fixture.Join();
        GameMap hunting = Fixture.MoveThroughPortal(s, portalId: 1);

        Fixture.MoveThroughPortal(s, portalId: 2);

        Assert.Null(hunting.GetPlayer(s.EntityId));
        Assert.False(Fixture.IsStillTicked(hunting), "비운 사냥터 복사본은 정리돼 더 이상 Tick되지 않아야 한다");
    }

    // T4 · 완료조건 4, IM-05: 사냥터 안의 혼자 s의 연결이 끊긴다.
    [Fact]
    public void DisconnectInsideHuntingGround_RetiresThatMap_AndLeavesNoOwner()
    {
        InstanceProbeSession s = Fixture.Join();
        GameMap hunting = Fixture.MoveThroughPortal(s, portalId: 1);
        int entityId = s.EntityId;

        s.Close();
        Fixture.Tick();

        Assert.Equal(-1, s.EntityId);
        Assert.Null(hunting.GetPlayer(entityId));
        Assert.Null(Town.GetPlayer(entityId));
        Assert.False(Fixture.IsStillTicked(hunting), "끊김으로 빈 사냥터 복사본은 정리돼야 한다");
    }

    // T4 · 완료조건 4, IM-02·IM-05: 보스방이 열린 혼자 s가 사냥터 포탈 1로 떠난 직후, 보스방 도착 전에 끊긴다.
    [Fact]
    public void DisconnectWhileMovingFromHuntingGroundToBossRoom_RetiresTheHuntingGroundMap_AndLeavesNoOwner()
    {
        InstanceProbeSession s = Fixture.Join();
        Fixture.UnlockBossRoom(s.EntityId);
        GameMap hunting = Fixture.MoveThroughPortal(s, portalId: 1);
        int entityId = s.EntityId;

        Fixture.EnterPortal(s, portalId: 1);
        // 월드 정리 전에 출발 맵 job만 돌려 「출발 맵에서 빠졌고 도착 job은 남은」 순간을 고정한다.
        Fixture.TickOnly(hunting);
        bool inTransit = s.CurrentMap == null;
        s.Close();
        Fixture.Settle();

        Assert.True(inTransit, "fixture: 포탈 job 뒤 도착 전에는 이동 중이어야 한다");
        Assert.Equal(-1, s.EntityId);
        Assert.Null(hunting.GetPlayer(entityId));
        Assert.Null(Town.GetPlayer(entityId));
        Assert.False(Fixture.IsStillTicked(hunting), "이동 중 끊김 뒤 빈 출발 복사본은 정리돼야 한다");
    }

    // T4 · 완료조건 4, IM-04(불변식만): s가 사냥터를 비운 뒤, 소켓 스레드가 정리 직전에 읽은 옛 사냥터로 이동 입력이 들어간다.
    [Fact]
    public void StaleInputIntoRetiredMap_DoesNotRun_AndDoesNotChangeTheLivePlayer()
    {
        InstanceProbeSession s = Fixture.Join();
        GameMap hunting = Fixture.MoveThroughPortal(s, portalId: 1);
        Fixture.MoveThroughPortal(s, portalId: 2);
        PlayerEntity live = Town.GetPlayer(s.EntityId)!;
        uint ackBefore = live.LastClientTick;

        s.StaleMapRead = hunting;
        s.OnRecvPacket(new C_MoveIntent { input = InputBits.Encode(1, false), clientTick = 999 }.Write());
        s.StaleMapRead = null;
        bool retiredMapStillRuns = Fixture.IsStillTicked(hunting);
        Fixture.Settle();

        Assert.Equal(ackBefore, live.LastClientTick);
        Assert.Equal(0, live.InputQueueCount);
        Assert.Single(Town.Players, p => p.EntityId == s.EntityId);
        Assert.Null(hunting.GetPlayer(s.EntityId));
        Assert.False(retiredMapStillRuns, "정리된 복사본에 들어간 job은 실행되지 않아야 한다");
    }

    // T4 · 완료조건 4, IM-02·IM-03: 파티 a1·a2가 같은 틱에 마을 포탈 1로 들어간다(둘 다 들어오는 중일 때 정리되면 안 된다).
    [Fact]
    public void TwoPartyMembersEnteringInOneTick_BothArriveInOneMap()
    {
        InstanceProbeSession a1 = Fixture.Join();
        InstanceProbeSession a2 = Fixture.Join();
        Fixture.FormParty(a1, a2);

        Fixture.EnterPortal(a1, portalId: 1);
        Fixture.EnterPortal(a2, portalId: 1);
        Fixture.Settle();
        GameMap? arrived = a1.CurrentMap;

        Assert.NotNull(arrived);
        Assert.Same(arrived, a2.CurrentMap);
        Assert.Equal(Ids(a1, a2), PlayerIds(arrived!));
    }

    // T6 · 완료조건 8, IM-16: 사냥터의 혼자 p3가 끊기는 같은 틱에 혼자 p1·p2가 마을 포탈 1로 들어간다.
    [Fact]
    public void TwoPortalsAndADisconnectInOneTick_FinishWithoutException_AndEveryoneEndsUpInPlace()
    {
        InstanceProbeSession p1 = Fixture.Join();
        InstanceProbeSession p2 = Fixture.Join();
        InstanceProbeSession p3 = Fixture.Join();
        GameMap p3Hunting = Fixture.MoveThroughPortal(p3, portalId: 1);
        int p3Id = p3.EntityId;

        Fixture.EnterPortal(p1, portalId: 1);
        Fixture.EnterPortal(p2, portalId: 1);
        p3.Close();
        Exception? sameTick = Record.Exception(() => Fixture.Tick());
        Exception? followingTicks = Record.Exception(() => Fixture.Settle());

        Assert.Null(sameTick);
        Assert.Null(followingTicks);
        Assert.True(p1.CurrentMap?.GetPlayer(p1.EntityId) != null, "p1은 사냥터 맵에 도착해야 한다");
        Assert.True(p2.CurrentMap?.GetPlayer(p2.EntityId) != null, "p2는 사냥터 맵에 도착해야 한다");
        Assert.Null(p3Hunting.GetPlayer(p3Id));
        Assert.Null(Town.GetPlayer(p3Id));
    }

    // T6 · 완료조건 8, IM-16: 혼자 s가 마을 포탈 1로 새 사냥터 복사본에 들어간다. 새 복사본은 만든 틱의 순회 목록에 없다.
    [Fact]
    public void EnteringANewInstance_FinishesArrivalOnTheNextTick()
    {
        InstanceProbeSession s = Fixture.Join();
        int entityId = s.EntityId;

        Fixture.EnterPortal(s, portalId: 1);
        Fixture.Tick();
        bool leftTownInPortalTick = Town.GetPlayer(entityId) == null;
        GameMap? afterPortalTick = s.CurrentMap;
        Fixture.Tick();
        GameMap? afterNextTick = s.CurrentMap;

        Assert.True(leftTownInPortalTick, "fixture: 포탈 job이 마을에서 빼야 한다");
        Assert.Null(afterPortalTick);
        Assert.NotNull(afterNextTick);
        Assert.Equal(MapId.HuntingGround, afterNextTick!.MapId);
        Assert.NotNull(afterNextTick.GetPlayer(entityId));
    }

    // T6 · 완료조건 8, IM-01: 사냥터 맵 데이터에 알 수 없는 적 종류가 있어 복사본을 만들 수 없는 월드에서 s가 마을 포탈 1에 들어간다.
    [Fact]
    public void BrokenHuntingGroundData_KeepsThePlayerInTown_AndTheNextInputStillWorks()
    {
        _fixture = new InstanceMapTestWorld(BrokenHuntingGroundMapData());
        InstanceProbeSession s = Fixture.Join();

        Fixture.EnterPortal(s, portalId: 1);
        Fixture.Settle();
        GameMap? stayedIn = s.CurrentMap;
        PlayerEntity? player = Town.GetPlayer(s.EntityId);
        float startX = player?.Position.X ?? float.NaN;
        Fixture.SendMoveRight(s, times: 5);

        Assert.Same(Town, stayedIn);
        Assert.NotNull(player);
        Assert.True(player!.Position.X > startX, $"x {startX} → {player.Position.X}: 마을에 남은 플레이어의 다음 입력이 들어야 한다");
    }
}
