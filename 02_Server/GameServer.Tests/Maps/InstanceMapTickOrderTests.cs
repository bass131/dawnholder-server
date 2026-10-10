using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 맵 틱 순서 중 「복사본은 만든 순」을 복사본 사이 이동의 도착 틱으로 관찰하는 독립 검증.
// 요구: goal 2026-10-10-instance-map-lifecycle 그림 4의 2단계, goal-review IM-16
//   (공용 맵 MapId 순 → 복사본 생성 순, 복사본 → 복사본 도착은 생성 순서에 따라 같은 틱 또는 다음 틱).
// 도착 job은 목적지 맵의 job 큐에서 돈다. 목적지가 출발지보다 먼저 도는 맵이면 그 틱에는 이미 지나갔다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapTickOrderTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    public void Dispose() => _fixture.Dispose();

    // IM-16: 파티 a·b(파티 진행 20으로 둘 다 해금). a가 사냥터→보스방으로 먼저 간다.
    // bossRoomOpenedFirst면 b는 그 뒤에 사냥터로 들어가 사냥터 복사본이 보스방 뒤에 다시 만들어진다.
    // 아니면 b가 처음부터 사냥터에 남아 사냥터가 먼저다. 그다음 b가 사냥터 포탈 1로 보스방에 간다.
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void MoveBetweenInstances_ArrivesInTheMoveTickOnlyWhenTheDestinationWasCreatedLater(
        bool bossRoomOpenedFirst, bool expectedArrivalInTheMoveTick)
    {
        InstanceProbeSession a = _fixture.Join();
        InstanceProbeSession b = _fixture.Join();
        int party = _fixture.FormParty(a, b);
        _fixture.UnlockBossRoom(a.EntityId);
        GameMap firstHunting = _fixture.MoveThroughPortal(a, portalId: 1);
        GameMap? partnerHunting = bossRoomOpenedFirst ? null : _fixture.MoveThroughPortal(b, portalId: 1);
        GameMap bossRoom = _fixture.MoveThroughPortal(a, portalId: 1);
        GameMap hunting = partnerHunting ?? _fixture.MoveThroughPortal(b, portalId: 1);
        bool huntingReopened = !ReferenceEquals(hunting, firstHunting);
        int liveInstancesBeforeTheMove = _fixture.World.LiveInstanceCount;

        _fixture.EnterPortal(b, portalId: 1);
        _fixture.Tick();
        bool arrivedInTheMoveTick = bossRoom.GetPlayer(b.EntityId) != null;
        _fixture.Tick();
        bool arrivedByTheNextTick = bossRoom.GetPlayer(b.EntityId) != null;

        Assert.Equal(bossRoomOpenedFirst, huntingReopened);
        Assert.Equal(2, liveInstancesBeforeTheMove);
        Assert.Equal(InstanceKey.ForParty(party), hunting.InstanceKey);
        Assert.Equal(InstanceKey.ForParty(party), bossRoom.InstanceKey);
        Assert.Equal(expectedArrivalInTheMoveTick, arrivedInTheMoveTick);
        Assert.True(arrivedByTheNextTick);
        Assert.Same(bossRoom, b.CurrentMap);
    }
}
