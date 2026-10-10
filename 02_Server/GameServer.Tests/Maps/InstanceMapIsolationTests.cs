using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 사냥터·보스방 복사본의 격리, 지금 맵으로 가는 입력, 공용 맵 조회.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 1, 만들 것 1·5, goal-review IM-07·IM-09·IM-11, S-05.
// 지금 있는 경계(월드 틱, 수신 경로, 세션의 지금 맵, GetMap)만 쓴다. 새 접근점이 필요한 단정은 InstanceMapRegistryTests에 있다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapIsolationTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    GameWorld World => _fixture.World;

    public void Dispose() => _fixture.Dispose();

    // T1 · 완료조건 1, IM-07·IM-11: 파티 A(a1·a2)와 파티 B(b1·b2)가 같은 틱에 마을 포탈 1로 사냥터에 들어간다.
    [Fact]
    public void TwoPartiesEnteringHuntingGround_LandInDifferentMaps_AndNeverSeeEachOther()
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        InstanceProbeSession b1 = _fixture.Join();
        InstanceProbeSession b2 = _fixture.Join();
        _fixture.FormParty(a1, a2);
        _fixture.FormParty(b1, b2);
        int aMark = a1.Packets.Count;
        int bMark = b1.Packets.Count;

        foreach (InstanceProbeSession session in new[] { a1, a2, b1, b2 })
            _fixture.EnterPortal(session, portalId: 1);
        _fixture.Settle();
        GameMap aMap = CurrentMapOf(a1);
        GameMap bMap = CurrentMapOf(b1);
        int[] aSide = PlayerIds(aMap).Concat(aMap.Enemies.Keys).ToArray();
        int[] bSide = PlayerIds(bMap).Concat(bMap.Enemies.Keys).ToArray();

        Assert.Same(aMap, a2.CurrentMap);
        Assert.Same(bMap, b2.CurrentMap);
        Assert.True(aMap.Enemies.Count > 0 && bMap.Enemies.Count > 0, "fixture: 사냥터 맵 데이터의 적이 있어야 한다");
        Assert.NotSame(aMap, bMap);
        Assert.Equal(Ids(a1, a2), PlayerIds(aMap));
        Assert.Equal(Ids(b1, b2), PlayerIds(bMap));
        Assert.Empty(aMap.Enemies.Keys.Intersect(bMap.Enemies.Keys));
        Assert.DoesNotContain(MentionedEntityIds(a1.Packets.Skip(aMark)), id => bSide.Contains(id));
        Assert.DoesNotContain(MentionedEntityIds(b1.Packets.Skip(bMark)), id => aSide.Contains(id));
    }

    // T1 · 완료조건 1, IM-07: 혼자인 c와 d가 서로 다른 틱에 마을 포탈 1로 사냥터에 들어간다.
    [Fact]
    public void TwoSoloPlayersInHuntingGround_AreInDifferentMaps_AndReceiveNothingAboutEachOther()
    {
        InstanceProbeSession c = _fixture.Join();
        InstanceProbeSession d = _fixture.Join();
        int cMark = c.Packets.Count;

        GameMap cMap = _fixture.MoveThroughPortal(c, portalId: 1);
        int dMark = d.Packets.Count;
        GameMap dMap = _fixture.MoveThroughPortal(d, portalId: 1);
        _fixture.Settle();

        Assert.NotSame(cMap, dMap);
        Assert.Null(cMap.GetPlayer(d.EntityId));
        Assert.Null(dMap.GetPlayer(c.EntityId));
        Assert.DoesNotContain(d.EntityId, MentionedEntityIds(c.Packets.Skip(cMark)));
        Assert.DoesNotContain(c.EntityId, MentionedEntityIds(d.Packets.Skip(dMark)));
    }

    // T7 · 만들 것 5, IM-09: 사냥터 안의 혼자 s가 오른쪽 이동 입력을 다섯 번 보낸다.
    [Fact]
    public void MoveInputInsideHuntingGround_MovesThePlayerInTheMapTheSessionIsIn()
    {
        InstanceProbeSession s = _fixture.Join();
        GameMap hunting = _fixture.MoveThroughPortal(s, portalId: 1);
        float startX = hunting.GetPlayer(s.EntityId)!.Position.X;

        _fixture.SendMoveRight(s, times: 5);
        PlayerEntity? moved = hunting.GetPlayer(s.EntityId);

        Assert.Same(hunting, s.CurrentMap);
        Assert.NotNull(moved);
        Assert.True(moved!.Position.X > startX, $"x {startX} → {moved.Position.X}: 입력이 그 맵의 플레이어를 움직여야 한다");
    }

    // T8 · 만들 것 1, S-05: 시작한 월드와, 혼자 s가 사냥터에 들어간 뒤의 공용 맵 조회.
    [Fact]
    public void PublicLookup_HasOneTownAndOneEnding_AndNeverReturnsInstanceKinds()
    {
        GameMap? town = World.GetMap(MapId.Town);
        GameMap? ending = World.GetMap(MapId.Ending);

        Assert.NotNull(town);
        Assert.Equal(MapId.Town, town!.MapId);
        Assert.Same(town, World.GetMap(MapId.Town));
        Assert.NotNull(ending);
        Assert.Equal(MapId.Ending, ending!.MapId);
        Assert.Null(World.GetMap(MapId.HuntingGround));
        Assert.Null(World.GetMap(MapId.BossRoom));

        InstanceProbeSession s = _fixture.Join();
        _fixture.MoveThroughPortal(s, portalId: 1);

        Assert.Null(World.GetMap(MapId.HuntingGround));
    }
}
