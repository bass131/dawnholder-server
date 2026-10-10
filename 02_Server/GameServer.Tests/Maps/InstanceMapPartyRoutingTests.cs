using Dawnholder.Server.GameServer.Maps;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 파티원이 같은 복사본으로 모이는 경로와, 복사본 안에서 바뀌지 않는 열쇠.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 2, 만들 것 3, goal-review IM-07·IM-10.
// 지금 있는 경계만 쓴다. 열쇠 값 단정은 InstanceMapRegistryTests에 있다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapPartyRoutingTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    public void Dispose() => _fixture.Dispose();

    // T2 · 완료조건 2, IM-07·IM-10: 파티 a1·a2가 서로 다른 틱에 마을 포탈 1로 들어가고, 그 사이에 혼자 c가 들어간다.
    [Fact]
    public void PartyMembersEnteringOnDifferentTicks_MeetInOneMap_WithoutTheSoloPlayerBetweenThem()
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        InstanceProbeSession c = _fixture.Join();
        _fixture.FormParty(a1, a2);

        GameMap firstMember = _fixture.MoveThroughPortal(a1, portalId: 1);
        GameMap solo = _fixture.MoveThroughPortal(c, portalId: 1);
        GameMap secondMember = _fixture.MoveThroughPortal(a2, portalId: 1);

        Assert.Same(firstMember, secondMember);
        Assert.NotSame(firstMember, solo);
        Assert.Equal(Ids(a1, a2), PlayerIds(firstMember));
    }

    // T2 · 완료조건 2, IM-10: 파티 진행으로 보스방이 열린 a1이 사냥터→보스방→사냥터로 돌아온다. a2와 혼자 c는 사냥터에 있다.
    [Fact]
    public void MemberGoingToBossRoomAndBack_ReturnsToThePartyHuntingGround()
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        InstanceProbeSession c = _fixture.Join();
        _fixture.FormParty(a1, a2);
        _fixture.UnlockBossRoom(a1.EntityId);
        GameMap partyHunting = _fixture.MoveThroughPortal(a1, portalId: 1);
        _fixture.MoveThroughPortal(a2, portalId: 1);
        _fixture.MoveThroughPortal(c, portalId: 1);

        _fixture.MoveThroughPortal(a1, portalId: 1);
        GameMap back = _fixture.MoveThroughPortal(a1, portalId: 2);

        Assert.Same(partyHunting, back);
        Assert.Equal(Ids(a1, a2), PlayerIds(back));
    }

    // T2 · 완료조건 2, IM-10(열쇠는 입장 때 정해짐): 사냥터 안에서 a1이 탈퇴 패킷을 보내 2인 파티가 해산된다.
    [Fact]
    public void MemberLeavingThePartyInsideHuntingGround_BothStayInThatMap()
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        _fixture.FormParty(a1, a2);
        GameMap partyHunting = _fixture.MoveThroughPortal(a1, portalId: 1);
        _fixture.MoveThroughPortal(a2, portalId: 1);

        _fixture.LeaveParty(a1);

        Assert.Same(partyHunting, a1.CurrentMap);
        Assert.Same(partyHunting, a2.CurrentMap);
        Assert.Equal(Ids(a1, a2), PlayerIds(partyHunting));
    }
}
