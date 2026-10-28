using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 세 맵 재설계(goal 2026-10-24-level-redesign) PR1의 포탈 위치·도착 요구.
// 기대 좌표는 승인 좌표표의 리터럴이다. 포탈 표에서 읽는 값은 fixture가 플레이어를 세울 자리(입력)뿐이다.
// 도착은 실제 수신 경로가 보낸 S_MapTransition으로 본다. 지형·씬·화면의 바닥 지지는 이 시험이 보지 않는다.
[Collection("GameWorldRegistryTests")]
public sealed class MapLayoutPortalContractTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Theory]
    [InlineData(MapId.Town, 1, 25f, 0f, MapId.HuntingGround, 2f, 0f)]
    [InlineData(MapId.HuntingGround, 1, 56f, 1f, MapId.BossRoom, 30f, 0f)]
    [InlineData(MapId.HuntingGround, 2, -2f, 0f, MapId.Town, 21f, 0f)]
    [InlineData(MapId.BossRoom, 1, -9.5f, 1f, MapId.Ending, 0f, 0f)]
    [InlineData(MapId.BossRoom, 2, 34f, 0f, MapId.HuntingGround, 50f, 0f)]
    [InlineData(MapId.Ending, 1, 5f, 0f, MapId.Town, 0f, 0f)]
    public void PortalTable_DeclaresLayoutPositionAndArrival(
        MapId source, int portalId, float x, float y, MapId destination, float spawnX, float spawnY)
    {
        Portal portal = Assert.Single(PortalTable.GetPortalsFor(source), p => p.PortalId == portalId);

        Assert.Equal(new Portal(portalId, new Vector2(x, y), destination, new Vector2(spawnX, spawnY)), portal);
    }

    [Fact]
    public void PortalIds_StayOnePerPublicMapAndTwoPerCopiedMap()
    {
        Assert.Equal(new[] { 1 }, PortalIdsOf(MapId.Town));
        Assert.Equal(new[] { 1, 2 }, PortalIdsOf(MapId.HuntingGround));
        Assert.Equal(new[] { 1, 2 }, PortalIdsOf(MapId.BossRoom));
        Assert.Equal(new[] { 1 }, PortalIdsOf(MapId.Ending));
    }

    [Fact]
    public void TownPortal_ArrivesInSoloAndPartyHuntingGroundCopies_AtLayoutSpawn()
    {
        InstanceProbeSession solo = _fixture.Join();
        InstanceProbeSession leader = _fixture.Join();
        InstanceProbeSession member = _fixture.Join();
        int partyId = _fixture.FormParty(leader, member);
        int soloMark = solo.Packets.Count;
        int leaderMark = leader.Packets.Count;

        GameMap soloCopy = _fixture.MoveThroughPortal(solo, portalId: 1);
        GameMap partyCopy = _fixture.MoveThroughPortal(leader, portalId: 1);

        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), soloCopy.InstanceKey);
        Assert.Equal(InstanceKey.ForParty(partyId), partyCopy.InstanceKey);
        AssertArrival(solo, soloMark, MapId.HuntingGround, new Vector2(2f, 0f));
        AssertArrival(leader, leaderMark, MapId.HuntingGround, new Vector2(2f, 0f));
    }

    [Fact]
    public void HuntingGroundReturnPortal_ArrivesInTown_AtLayoutSpawn()
    {
        InstanceProbeSession solo = _fixture.Join();
        _fixture.MoveThroughPortal(solo, portalId: 1);
        int mark = solo.Packets.Count;

        GameMap town = _fixture.MoveThroughPortal(solo, portalId: 2);

        Assert.Same(_fixture.Town, town);
        AssertArrival(solo, mark, MapId.Town, new Vector2(21f, 0f));
    }

    [Fact]
    public void HuntingGroundForwardPortal_AfterTwentyKills_ArrivesInBossRoom_AtLayoutSpawn()
    {
        InstanceProbeSession solo = _fixture.Join();
        _fixture.MoveThroughPortal(solo, portalId: 1);
        _fixture.UnlockBossRoom(solo.EntityId);
        int mark = solo.Packets.Count;

        GameMap bossRoom = _fixture.MoveThroughPortal(solo, portalId: 1);

        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), bossRoom.InstanceKey);
        AssertArrival(solo, mark, MapId.BossRoom, new Vector2(30f, 0f));
    }

    [Fact]
    public void BossRoomReturnPortal_ArrivesInHuntingGround_AtLayoutSpawn()
    {
        InstanceProbeSession solo = _fixture.Join();
        _fixture.MoveThroughPortal(solo, portalId: 1);
        _fixture.UnlockBossRoom(solo.EntityId);
        _fixture.MoveThroughPortal(solo, portalId: 1);
        int mark = solo.Packets.Count;

        GameMap huntingGround = _fixture.MoveThroughPortal(solo, portalId: 2);

        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), huntingGround.InstanceKey);
        AssertArrival(solo, mark, MapId.HuntingGround, new Vector2(50f, 0f));
    }

    // 보존: 20킬 전에는 보스방 방향만 잠기고 플레이어는 사냥터에 남는다.
    [Fact]
    public void HuntingGroundForwardPortal_BeforeTwentyKills_StaysLocked()
    {
        InstanceProbeSession solo = _fixture.Join();
        GameMap huntingGround = _fixture.MoveThroughPortal(solo, portalId: 1);
        int mark = solo.Packets.Count;

        _fixture.EnterPortal(solo, portalId: 1);
        _fixture.Settle();

        Assert.Same(huntingGround, solo.CurrentMap);
        byte[] lockedPacket = Assert.Single(solo.Packets.Skip(mark), p => IdOf(p) == PacketID.S_PortalLocked);
        Assert.Equal(20, Read<S_PortalLocked>(lockedPacket).requiredCount);
        Assert.DoesNotContain(solo.Packets.Skip(mark), p => IdOf(p) == PacketID.S_MapTransition);
    }

    // 보존: 근접 범위 밖이거나 이 맵에 없는 portalId면 전환하지 않는다.
    [Theory]
    [InlineData(1, 3f)]
    [InlineData(2, 0f)]
    public void TownPortal_FromOutsideProximityOrWithForeignId_DoesNotTransition(int requestedPortalId, float offsetX)
    {
        InstanceProbeSession solo = _fixture.Join();
        GameMap town = _fixture.Town;
        Vector2 portalPosition = Assert.Single(town.Portals).Position;
        town.GetPlayer(solo.EntityId)!.Position = portalPosition + new Vector2(offsetX, 0f);
        int mark = solo.Packets.Count;

        solo.OnRecvPacket(new C_EnterPortal { portalId = requestedPortalId }.Write());
        _fixture.Settle();

        Assert.Same(town, solo.CurrentMap);
        Assert.DoesNotContain(solo.Packets.Skip(mark), p => IdOf(p) == PacketID.S_MapTransition);
    }

    static int[] PortalIdsOf(MapId mapId)
        => PortalTable.GetPortalsFor(mapId).Select(p => p.PortalId).OrderBy(id => id).ToArray();

    // 도착 좌표의 권위는 서버가 보낸 전환 패킷이다. 도착 뒤 물리 틱이 움직인 현재 위치와 구분한다.
    static void AssertArrival(InstanceProbeSession session, int mark, MapId destination, Vector2 spawn)
    {
        byte[] packet = Assert.Single(session.Packets.Skip(mark), p => IdOf(p) == PacketID.S_MapTransition);
        S_MapTransition transition = Read<S_MapTransition>(packet);
        Assert.Equal((byte)destination, transition.destMapId);
        Assert.Equal(spawn.X, transition.spawnX);
        Assert.Equal(spawn.Y, transition.spawnY);
    }
}
