using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Transitions;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 서버가 시키는 이동이 수락된 뒤 도착 전에 연결이 끊기는 경우의 독립 검증.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 4·5, 「현재 결과」의 「구현」 절 확인 항목 3, goal-review IM-02·IM-05·IM-06.
// 목적지가 공용 맵이면 등록부가 입장 대기 수를 세지 않는다. 그래서 정리는 플레이어가 빠진 출발 복사본에서 관찰한다.
// 호출은 Content 처리기처럼 그 플레이어의 지금 맵 job 안에서 한다(Content 계약 3).
[Collection("GameWorldRegistryTests")]
public sealed class ServerMapMoveDisconnectTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    GameWorld World => _fixture.World;

    GameMap Town => _fixture.Town;

    public void Dispose() => _fixture.Dispose();

    // 확인 항목 3: 혼자 s가 사냥터 복사본에 있고 관찰자 o가 마을에 있다. s를 좌표 없이 마을로 보내는 호출이 수락된 뒤,
    // 같은 job 안(closeInTheSameJob) 또는 그 틱이 끝나고 마을의 도착 job이 돌기 전에 s의 연결을 닫는다.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClosingAfterAnAcceptedMoveToTown_LeavesTheEntityNowhere_AndRetiresTheEmptySourceInstance(bool closeInTheSameJob)
    {
        InstanceProbeSession o = _fixture.Join();
        InstanceProbeSession s = _fixture.Join();
        GameMap hunting = _fixture.MoveThroughPortal(s, portalId: 1);
        int sId = s.EntityId;
        s.Packets.Clear();
        o.Packets.Clear();

        MapMoveResult? result = null;
        Exception? thrown = null;
        hunting.EnqueueJob(() =>
        {
            thrown = Record.Exception(() => result = MapMigration.MoveToPublicMap(s, sId, hunting, MapId.Town));
            if (closeInTheSameJob) s.Close();
        });
        _fixture.Tick();
        bool inNoMapAfterTheMoveTick = NoLiveMapHolds(sId);
        if (!closeInTheSameJob) s.Close();
        _fixture.Settle();

        Assert.Null(thrown);
        Assert.Equal(MapMoveResult.Accepted, result);
        Assert.True(inNoMapAfterTheMoveTick, "fixture: 마을 도착 job은 다음 틱이라 이동 틱 끝에는 어느 맵에도 없어야 한다");
        Assert.True(NoLiveMapHolds(sId));
        Assert.DoesNotContain(Town.Players, p => ReferenceEquals(p.Owner, s));
        Assert.Equal(-1, s.EntityId);
        Assert.Equal(0, World.LiveInstanceCount);
        Assert.False(_fixture.IsStillTicked(hunting));
        Assert.DoesNotContain(s.Packets, p => IdOf(p) == PacketID.S_MapTransition);
        Assert.DoesNotContain(o.Packets, p => IdOf(p) == PacketID.S_PlayerJoin && Read<S_PlayerJoin>(p).entityId == sId);
    }

    // 확인 항목 3: 파티 m·n이 사냥터 복사본에 있다. m을 마을로 보내는 호출이 수락된 같은 job에서 m의 연결을 닫는다.
    // 복사본은 n 때문에 남고, n은 m의 퇴장을 한 번만 받는다.
    [Fact]
    public void ClosingAfterAnAcceptedMoveToTown_WithAPartnerStaying_LeavesOnlyThePartnerInTheInstance()
    {
        InstanceProbeSession m = _fixture.Join();
        InstanceProbeSession n = _fixture.Join();
        _fixture.FormParty(m, n);
        GameMap hunting = _fixture.MoveThroughPortal(m, portalId: 1);
        GameMap partnerHunting = _fixture.MoveThroughPortal(n, portalId: 1);
        int mId = m.EntityId;
        n.Packets.Clear();

        MapMoveResult? result = null;
        hunting.EnqueueJob(() =>
        {
            result = MapMigration.MoveToPublicMap(m, mId, hunting, MapId.Town);
            m.Close();
        });
        _fixture.Settle();

        Assert.Same(hunting, partnerHunting);
        Assert.Equal(MapMoveResult.Accepted, result);
        Assert.True(NoLiveMapHolds(mId));
        Assert.Equal(-1, m.EntityId);
        Assert.Equal(new[] { n.EntityId }, PlayerIds(hunting));
        Assert.Same(hunting, n.CurrentMap);
        Assert.Equal(1, World.LiveInstanceCount);
        Assert.Single(n.Packets, p => IdOf(p) == PacketID.S_PlayerLeave && Read<S_PlayerLeave>(p).entityId == mId);
    }

    bool NoLiveMapHolds(int entityId) => World.AllLiveMaps.All(map => map.GetPlayer(entityId) == null);
}
