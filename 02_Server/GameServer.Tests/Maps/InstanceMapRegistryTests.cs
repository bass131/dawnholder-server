using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 인스턴스 등록부의 관찰면으로 보는 요구: 열쇠 타입, 살아 있는 복사본 수·찾기, 공용 맵과 복사본 열거, 맵이 가진 열쇠.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 1·2·4·8, 만들 것 1·2·4·5·8,
//       goal-review IM-01~03·05·07~10·15·16, 채택 제안 P-02·P-05·P-13.
// 새 타입·멤버를 이름으로 직접 부르므로 구현 전에는 컴파일되지 않는다. 이름은 goal 「설계 검토 반영」이 채택한 제안을 따른다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapRegistryTests : IDisposable
{
    InstanceMapTestWorld? _fixture;

    InstanceMapTestWorld Fixture => _fixture ??= new InstanceMapTestWorld();

    GameWorld World => Fixture.World;

    public void Dispose() => _fixture?.Dispose();

    // T8 · 만들 것 1·8, P-05·P-13: 시작한 월드.
    [Fact]
    public void NewWorld_HasNoLiveInstance_AndItsPublicMapsCarryNoKey()
    {
        MapId[] liveMapIds = World.AllLiveMaps.Select(map => map.MapId).OrderBy(id => id).ToArray();

        Assert.Equal(0, World.LiveInstanceCount);
        Assert.Equal(new[] { MapId.Town, MapId.Ending }, liveMapIds);
        Assert.Null(World.GetMap(MapId.Town)!.InstanceKey);
        Assert.Null(World.GetMap(MapId.Ending)!.InstanceKey);
    }

    // T8 · 만들 것 2, IM-07·IM-10, P-02: 파티 번호와 entity 번호가 같은 값 7인 두 열쇠로 사냥터·보스방 복사본을 연다.
    [Fact]
    public void PartyAndSoloKeysWithTheSameNumber_AreDifferentKeys_AndOpenDifferentInstances()
    {
        InstanceKey party7 = InstanceKey.ForParty(7);
        InstanceKey solo7 = InstanceKey.ForSolo(7);

        GameMap partyHunting = World.GetOrCreateInstance(MapId.HuntingGround, party7);
        GameMap soloHunting = World.GetOrCreateInstance(MapId.HuntingGround, solo7);
        GameMap partyBossRoom = World.GetOrCreateInstance(MapId.BossRoom, party7);
        GameMap partyHuntingAgain = World.GetOrCreateInstance(MapId.HuntingGround, party7);
        bool soloFound = World.TryGetInstance(MapId.HuntingGround, solo7, out GameMap? soloLookup);
        bool unopenedFound = World.TryGetInstance(MapId.BossRoom, solo7, out _);

        Assert.NotEqual(party7, solo7);
        Assert.Equal(InstanceOwner.Party, party7.Owner);
        Assert.Equal(7, party7.Id);
        Assert.Equal(InstanceOwner.Solo, solo7.Owner);
        Assert.Equal(7, solo7.Id);
        Assert.NotSame(partyHunting, soloHunting);
        Assert.NotSame(partyHunting, partyBossRoom);
        Assert.Same(partyHunting, partyHuntingAgain);
        Assert.True(soloFound);
        Assert.Same(soloHunting, soloLookup);
        Assert.False(unopenedFound);
        Assert.Equal(MapId.HuntingGround, partyHunting.MapId);
        Assert.Equal(MapId.BossRoom, partyBossRoom.MapId);
        Assert.Equal<InstanceKey?>(party7, partyHunting.InstanceKey);
        Assert.Equal<InstanceKey?>(solo7, soloHunting.InstanceKey);
        Assert.Equal(3, World.LiveInstanceCount);
    }

    // T1 · 완료조건 1, IM-07·IM-10·IM-11: 파티 A(a1·a2)와 파티 B(b1·b2)가 같은 틱에 마을 포탈 1로 사냥터에 들어간다.
    [Fact]
    public void TwoPartiesInHuntingGround_HaveOneInstanceEach_KeyedByTheirParty()
    {
        InstanceProbeSession a1 = Fixture.Join();
        InstanceProbeSession a2 = Fixture.Join();
        InstanceProbeSession b1 = Fixture.Join();
        InstanceProbeSession b2 = Fixture.Join();
        int partyA = Fixture.FormParty(a1, a2);
        int partyB = Fixture.FormParty(b1, b2);

        foreach (InstanceProbeSession session in new[] { a1, a2, b1, b2 })
            Fixture.EnterPortal(session, portalId: 1);
        Fixture.Settle();
        bool aFound = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForParty(partyA), out GameMap? aInstance);
        bool bFound = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForParty(partyB), out GameMap? bInstance);

        Assert.Equal(2, World.LiveInstanceCount);
        Assert.True(aFound);
        Assert.True(bFound);
        Assert.Same(aInstance, a1.CurrentMap);
        Assert.Same(aInstance, a2.CurrentMap);
        Assert.Same(bInstance, b1.CurrentMap);
        Assert.Same(bInstance, b2.CurrentMap);
        Assert.Equal<InstanceKey?>(InstanceKey.ForParty(partyA), aInstance!.InstanceKey);
        Assert.Equal<InstanceKey?>(InstanceKey.ForParty(partyB), bInstance!.InstanceKey);
        AssertEachInExactlyOneLiveMap(a1, a2, b1, b2);
    }

    // T2 · 완료조건 2, IM-10: 파티(a1·a2, 진행 20)가 사냥터에 들어간 뒤 a1의 탈퇴로 해산되고, a2가 사냥터 포탈 1로 보스방에 간다.
    [Fact]
    public void PartyKey_StaysAfterThePartyDisbandsInside_AndFollowsIntoTheBossRoom()
    {
        InstanceProbeSession a1 = Fixture.Join();
        InstanceProbeSession a2 = Fixture.Join();
        int party = Fixture.FormParty(a1, a2);
        Fixture.UnlockBossRoom(a1.EntityId);
        Fixture.MoveThroughPortal(a1, portalId: 1);
        Fixture.MoveThroughPortal(a2, portalId: 1);

        Fixture.LeaveParty(a1);
        bool partyInstanceKept = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForParty(party), out GameMap? hunting);
        int[] playersAfterDisband = hunting == null ? Array.Empty<int>() : PlayerIds(hunting);
        GameMap bossRoom = Fixture.MoveThroughPortal(a2, portalId: 1);
        bool soloBossRoomOpened = World.TryGetInstance(MapId.BossRoom, InstanceKey.ForSolo(a2.EntityId), out _);

        Assert.True(partyInstanceKept);
        Assert.Equal<InstanceKey?>(InstanceKey.ForParty(party), hunting!.InstanceKey);
        Assert.Equal(Ids(a1, a2), playersAfterDisband);
        Assert.Equal<InstanceKey?>(InstanceKey.ForParty(party), bossRoom.InstanceKey);
        Assert.False(soloBossRoomOpened);
    }

    // T4 · 완료조건 4, IM-03·IM-15: 네 세션(파티 a1·a2, 혼자 c·d)이 사냥터에 들어갔다가 같은 틱에 사냥터 포탈 2로 마을에 돌아온다.
    [Fact]
    public void InstanceCount_IsThreeForFourSessions_AndZeroAfterEveryoneLeaves()
    {
        InstanceProbeSession a1 = Fixture.Join();
        InstanceProbeSession a2 = Fixture.Join();
        InstanceProbeSession c = Fixture.Join();
        InstanceProbeSession d = Fixture.Join();
        Fixture.FormParty(a1, a2);
        InstanceProbeSession[] everyone = { a1, a2, c, d };

        foreach (InstanceProbeSession session in everyone)
            Fixture.EnterPortal(session, portalId: 1);
        Fixture.Settle();
        int whileInside = World.LiveInstanceCount;
        foreach (InstanceProbeSession session in everyone)
            Fixture.EnterPortal(session, portalId: 2);
        Fixture.Settle();
        int afterLeaving = World.LiveInstanceCount;

        // 파티 하나 + 혼자 둘. 세션 4개보다 많아질 수 없다.
        Assert.Equal(3, whileInside);
        Assert.Equal(0, afterLeaving);
        Assert.Equal(Ids(a1, a2, c, d), PlayerIds(Fixture.Town));
        AssertEachInExactlyOneLiveMap(everyone);
    }

    // T4 · 완료조건 4, IM-05·IM-08: 사냥터 안의 혼자 s의 연결이 끊긴다.
    [Fact]
    public void DisconnectInsideAnInstance_RemovesIt_AndNoLiveMapHoldsTheEntity()
    {
        InstanceProbeSession s = Fixture.Join();
        Fixture.MoveThroughPortal(s, portalId: 1);
        int entityId = s.EntityId;
        int beforeClose = World.LiveInstanceCount;

        s.Close();
        Fixture.Tick();

        Assert.Equal(1, beforeClose);
        Assert.Equal(0, World.LiveInstanceCount);
        Assert.DoesNotContain(entityId, AllLivePlayerIds());
    }

    // T4 · 완료조건 4, IM-02·IM-05·IM-07: 혼자 s가 마을 포탈 1로 떠난 직후, 새 사냥터 복사본에 도착하기 전에 끊긴다.
    [Fact]
    public void DisconnectWhileMovingIntoANewInstance_LeavesNoInstance_AndNoGhost()
    {
        InstanceProbeSession s = Fixture.Join();
        int entityId = s.EntityId;

        Fixture.EnterPortal(s, portalId: 1);
        // 월드 정리 전에 출발 맵 job만 돌려 「복사본은 생겼고 도착 job은 남은」 순간을 고정한다.
        Fixture.TickOnly(Fixture.Town);
        bool inTransit = s.CurrentMap == null;
        int whileIncoming = World.LiveInstanceCount;
        s.Close();
        Fixture.Settle();

        Assert.True(inTransit, "fixture: 포탈 job 뒤 도착 전에는 이동 중이어야 한다");
        Assert.Equal(1, whileIncoming);
        Assert.Equal(0, World.LiveInstanceCount);
        Assert.DoesNotContain(entityId, AllLivePlayerIds());
    }

    // T4·T6 · 완료조건 4·8, IM-02·IM-03·IM-07·IM-16: 파티 a1·a2가 같은 틱에 마을 포탈 1로 들어간다.
    [Fact]
    public void InstanceEnteredByTwoInOneTick_IsKeptWhileBothAreIncoming_AndBothArriveOnTheNextTick()
    {
        InstanceProbeSession a1 = Fixture.Join();
        InstanceProbeSession a2 = Fixture.Join();
        int party = Fixture.FormParty(a1, a2);

        Fixture.EnterPortal(a1, portalId: 1);
        Fixture.EnterPortal(a2, portalId: 1);
        Fixture.Tick();
        int countAfterPortalTick = World.LiveInstanceCount;
        bool found = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForParty(party), out GameMap? instance);
        int arrivedInPortalTick = instance?.Players.Count ?? -1;
        Fixture.Tick();
        int[] arrivedOnNextTick = instance == null ? Array.Empty<int>() : PlayerIds(instance);

        Assert.Equal(1, countAfterPortalTick);
        Assert.True(found);
        Assert.Equal(0, arrivedInPortalTick);
        Assert.Equal(Ids(a1, a2), arrivedOnNextTick);
    }

    // T6 · 완료조건 8, IM-08·IM-16: 사냥터의 혼자 p3가 끊기는 같은 틱에 혼자 p1·p2가 마을 포탈 1로 들어간다.
    [Fact]
    public void TwoPortalsAndADisconnectInOneTick_LeaveTwoInstances_AndEachPlayerInExactlyOneLiveMap()
    {
        InstanceProbeSession p1 = Fixture.Join();
        InstanceProbeSession p2 = Fixture.Join();
        InstanceProbeSession p3 = Fixture.Join();
        Fixture.MoveThroughPortal(p3, portalId: 1);
        int p3Id = p3.EntityId;

        Fixture.EnterPortal(p1, portalId: 1);
        Fixture.EnterPortal(p2, portalId: 1);
        p3.Close();
        Fixture.Tick();
        Fixture.Settle();
        bool p1Found = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(p1.EntityId), out GameMap? p1Instance);
        bool p2Found = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(p2.EntityId), out GameMap? p2Instance);
        bool p3Found = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(p3Id), out _);

        Assert.Equal(2, World.LiveInstanceCount);
        Assert.True(p1Found);
        Assert.Same(p1Instance, p1.CurrentMap);
        Assert.True(p2Found);
        Assert.Same(p2Instance, p2.CurrentMap);
        Assert.False(p3Found);
        Assert.DoesNotContain(p3Id, AllLivePlayerIds());
        AssertEachInExactlyOneLiveMap(p1, p2);
    }

    // T6 · 완료조건 8, IM-01: 사냥터 맵 데이터에 알 수 없는 적 종류가 있는 월드에서 혼자 s가 마을 포탈 1에 들어간다.
    [Fact]
    public void BrokenHuntingGroundData_OpensNoInstance_AndKeepsThePlayerOnlyInTown()
    {
        _fixture = new InstanceMapTestWorld(BrokenHuntingGroundMapData());
        InstanceProbeSession s = Fixture.Join();

        Fixture.EnterPortal(s, portalId: 1);
        Fixture.Settle();

        Assert.Equal(0, World.LiveInstanceCount);
        Assert.Equal(Ids(s), AllLivePlayerIds());
        Assert.Same(Fixture.Town, s.CurrentMap);
    }

    // T7 · 만들 것 5, IM-09: 사냥터 안의 혼자 s가 오른쪽 이동 입력을 다섯 번 보낸다.
    [Fact]
    public void MoveInputInsideAnInstance_MovesThePlayerInItsInstance()
    {
        InstanceProbeSession s = Fixture.Join();
        Fixture.MoveThroughPortal(s, portalId: 1);
        bool found = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(s.EntityId), out GameMap? instance);
        float startX = instance?.GetPlayer(s.EntityId)?.Position.X ?? float.NaN;

        Fixture.SendMoveRight(s, times: 5);
        float endX = instance?.GetPlayer(s.EntityId)?.Position.X ?? float.NaN;

        Assert.True(found);
        Assert.Same(instance, s.CurrentMap);
        Assert.True(endX > startX, $"x {startX} → {endX}: 입력이 그 복사본의 플레이어를 움직여야 한다");
    }

    int[] AllLivePlayerIds()
        => World.AllLiveMaps.SelectMany(map => map.Players).Select(player => player.EntityId).OrderBy(id => id).ToArray();

    // IM-08: 틱 경계에서 살아 있는 플레이어는 살아 있는 맵 가운데 정확히 한 곳에 있다.
    void AssertEachInExactlyOneLiveMap(params InstanceProbeSession[] sessions)
    {
        int[] live = AllLivePlayerIds();
        foreach (InstanceProbeSession session in sessions)
            Assert.Equal(1, live.Count(id => id == session.EntityId));
    }
}
