using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Transitions;
using Dawnholder.Server.GameServer.Party;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 포탈 없이 서버가 시키는 이동. Content 처리기처럼 그 플레이어의 지금 맵 job 안에서 부른다.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 5, 만들 것 6, Content 계약 3, goal-review IM-06·IM-13, 채택 제안 P-08.
// 이 시험이 고정하는 진입점: MapMigration.MoveToPublicMap(session, entityId, currentMap, destination, spawn = null) → MapMoveResult.
// 거부 검사 순서(IM-13): 세션 종료 중 → 이미 이동 중 → 그 맵에 없음 → 지금 맵이 공용 맵 → 목적지가 공용 맵 아님.
[Collection("GameWorldRegistryTests")]
public sealed class ServerMapMoveTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    GameMap Town => _fixture.Town;

    public void Dispose() => _fixture.Dispose();

    // T5 · 완료조건 5, IM-06: 파티 m·n이 사냥터에 있고 관찰자 o가 마을에 있을 때 m을 좌표 없이 마을로 보낸다.
    [Fact]
    public void MoveToTownWithoutSpawn_ArrivesAtTownSpawn_WithThePortalMovePacketsOnly()
    {
        InstanceProbeSession o = _fixture.Join();
        InstanceProbeSession m = _fixture.Join();
        InstanceProbeSession n = _fixture.Join();
        _fixture.FormParty(m, n);
        GameMap hunting = _fixture.MoveThroughPortal(m, portalId: 1);
        _fixture.MoveThroughPortal(n, portalId: 1);
        foreach (InstanceProbeSession session in new[] { o, m, n })
            session.Packets.Clear();

        MapMoveResult? result = null;
        JobRecord call = RunInMapJob(hunting, () => result = MapMigration.MoveToPublicMap(m, m.EntityId, hunting, MapId.Town));
        _fixture.Settle();
        IEnumerable<byte[]> everyonesPackets = m.Packets.Concat(n.Packets).Concat(o.Packets);

        Assert.Null(call.Thrown);
        Assert.Equal(MapMoveResult.Accepted, result);
        Assert.Same(Town, m.CurrentMap);
        Assert.Single(Town.Players, p => p.EntityId == m.EntityId);
        Assert.Null(hunting.GetPlayer(m.EntityId));
        // 이동한 사람: 포탈 이동과 같은 MapTransition → PlayerHp → 기존 인원 roster(server.md 「맵 패킷 표현」).
        Assert.Equal(new[] { PacketID.S_MapTransition, PacketID.S_PlayerHp, PacketID.S_PlayerJoin }, m.Packets.Take(3).Select(IdOf));
        S_MapTransition transition = Read<S_MapTransition>(m.Packets[0]);
        Assert.Equal((byte)MapId.Town, transition.destMapId);
        Assert.Equal(TownSpawnX, transition.spawnX);
        Assert.Equal(0f, transition.spawnY);
        Assert.Equal(o.EntityId, Read<S_PlayerJoin>(m.Packets[2]).entityId);
        // 남은 파티원은 퇴장을, 마을 관찰자는 입장을 받는다. 포탈 잠금 통보는 누구도 받지 않는다.
        Assert.Contains(n.Packets, p => IdOf(p) == PacketID.S_PlayerLeave && Read<S_PlayerLeave>(p).entityId == m.EntityId);
        Assert.Contains(o.Packets, p => IdOf(p) == PacketID.S_PlayerJoin && Read<S_PlayerJoin>(p).entityId == m.EntityId);
        Assert.DoesNotContain(everyonesPackets, p => IdOf(p) == PacketID.S_PortalLocked);
    }

    // T5 · 완료조건 5: 사냥터의 혼자 s를 좌표 (3, 0)으로 마을에 보낸다.
    [Fact]
    public void MoveToTownWithSpawn_UsesTheGivenSpawn()
    {
        InstanceProbeSession s = _fixture.Join();
        GameMap hunting = _fixture.MoveThroughPortal(s, portalId: 1);
        s.Packets.Clear();

        MapMoveResult? result = null;
        RunInMapJob(hunting, () => result = MapMigration.MoveToPublicMap(s, s.EntityId, hunting, MapId.Town, new Vector2(3f, 0f)));
        _fixture.Settle();
        byte[] transitionPacket = Assert.Single(s.Packets, p => IdOf(p) == PacketID.S_MapTransition);
        S_MapTransition transition = Read<S_MapTransition>(transitionPacket);

        Assert.Equal(MapMoveResult.Accepted, result);
        Assert.Same(Town, s.CurrentMap);
        Assert.Equal(3f, transition.spawnX);
        Assert.Equal(0f, transition.spawnY);
    }

    // T5 · 완료조건 5, IM-06: 마을에서 처치 보상을 받은 파티원 m을 사냥터에서 마을로 보낸다.
    [Fact]
    public void MoveToTown_KeepsInventoryRevisionCurrencyAndParty()
    {
        InstanceProbeSession m = _fixture.Join();
        InstanceProbeSession n = _fixture.Join();
        int party = _fixture.FormParty(m, n);
        KillNormalInTown(m);
        GameMap hunting = _fixture.MoveThroughPortal(m, portalId: 1);
        _fixture.MoveThroughPortal(n, portalId: 1);
        (uint Revision, int Currency) before = QueryInventory(m);

        MapMoveResult? result = null;
        RunInMapJob(hunting, () => result = MapMigration.MoveToPublicMap(m, m.EntityId, hunting, MapId.Town));
        _fixture.Settle();
        (uint Revision, int Currency) after = QueryInventory(m);
        PartyState? partyAfter = _fixture.World.Party.GetPartyByEntity(m.EntityId);

        Assert.True(before.Revision > 0, "fixture: 처치 보상으로 경제 상태가 한 번 이상 바뀌었어야 한다");
        Assert.Equal(MapMoveResult.Accepted, result);
        Assert.Same(Town, m.CurrentMap);
        Assert.Equal(before, after);
        Assert.Equal(party, partyAfter?.PartyId);
        Assert.Equal(Ids(m, n), partyAfter!.Members.OrderBy(id => id));
    }

    // T5 · 완료조건 5, IM-13 ①: 사냥터의 파티원 m이 끊긴 직후, 월드 정리 전에 m을 마을로 보낸다.
    [Fact]
    public void ClosingSession_IsRejectedAsClosing_WithoutStateOrPacketChange()
    {
        InstanceProbeSession m = _fixture.Join();
        InstanceProbeSession n = _fixture.Join();
        _fixture.FormParty(m, n);
        GameMap hunting = _fixture.MoveThroughPortal(m, portalId: 1);
        _fixture.MoveThroughPortal(n, portalId: 1);

        m.Close();
        MapMoveResult? result = null;
        JobRecord call = RunInMapJob(hunting, () => result = MapMigration.MoveToPublicMap(m, m.EntityId, hunting, MapId.Town), m, n);
        // 월드 정리가 m을 빼기 전에 그 맵 job만 돌린다.
        _fixture.TickOnly(hunting);

        Assert.Null(call.Thrown);
        Assert.Equal(MapMoveResult.SessionClosing, result);
        Assert.Equal(new[] { 0, 0 }, call.SentDuringCall);
        Assert.NotNull(hunting.GetPlayer(m.EntityId));
        Assert.Same(hunting, m.CurrentMap);
    }

    // T5 · 완료조건 5, IM-13 ①이 ②보다 먼저: 같은 job 순서로 혼자 s가 수락된 뒤 끊기고, s에게 한 번 더 부른다.
    [Fact]
    public void ClosingSessionThatIsAlsoMoving_IsRejectedAsClosing()
    {
        InstanceProbeSession s = _fixture.Join();
        GameMap hunting = _fixture.MoveThroughPortal(s, portalId: 1);

        MapMoveResult? first = null;
        MapMoveResult? second = null;
        RunInMapJob(hunting, () => first = MapMigration.MoveToPublicMap(s, s.EntityId, hunting, MapId.Town));
        hunting.EnqueueJob(s.Close);
        JobRecord secondCall = RunInMapJob(hunting, () => second = MapMigration.MoveToPublicMap(s, s.EntityId, hunting, MapId.Town), s);
        _fixture.Tick();

        Assert.Equal(MapMoveResult.Accepted, first);
        Assert.Null(secondCall.Thrown);
        Assert.Equal(MapMoveResult.SessionClosing, second);
        Assert.Equal(new[] { 0 }, secondCall.SentDuringCall);
    }

    // T5 · 완료조건 5, IM-13 ②가 ③보다 먼저: 같은 job 순서로 혼자 s를 마을로 두 번 보낸다(두 번째 때 s는 이동 중이고 맵에도 없다).
    [Fact]
    public void SecondCallForTheSamePlayer_IsRejectedAsAlreadyMigrating_AndThePlayerArrivesOnce()
    {
        InstanceProbeSession s = _fixture.Join();
        GameMap hunting = _fixture.MoveThroughPortal(s, portalId: 1);
        s.Packets.Clear();

        MapMoveResult? first = null;
        MapMoveResult? second = null;
        RunInMapJob(hunting, () => first = MapMigration.MoveToPublicMap(s, s.EntityId, hunting, MapId.Town));
        JobRecord secondCall = RunInMapJob(hunting, () => second = MapMigration.MoveToPublicMap(s, s.EntityId, hunting, MapId.Town), s);
        _fixture.Settle();

        Assert.Equal(MapMoveResult.Accepted, first);
        Assert.Null(secondCall.Thrown);
        Assert.Equal(MapMoveResult.AlreadyMigrating, second);
        Assert.Equal(new[] { 0 }, secondCall.SentDuringCall);
        Assert.Single(Town.Players, p => p.EntityId == s.EntityId);
        Assert.Single(s.Packets, p => IdOf(p) == PacketID.S_MapTransition);
    }

    // T5 · 완료조건 5, IM-13 ③이 ④보다 먼저: 사냥터의 혼자 s를 마을 job에서 마을을 지금 맵으로 넘겨 부른다(마을은 공용이라 ④도 어긋난다).
    [Fact]
    public void PlayerNotInTheGivenMap_IsRejectedAsNotInThatMap()
    {
        InstanceProbeSession s = _fixture.Join();
        GameMap hunting = _fixture.MoveThroughPortal(s, portalId: 1);

        MapMoveResult? result = null;
        JobRecord call = RunInMapJob(Town, () => result = MapMigration.MoveToPublicMap(s, s.EntityId, Town, MapId.Town), s);
        _fixture.Tick();

        Assert.Null(call.Thrown);
        Assert.Equal(MapMoveResult.NotInThatMap, result);
        Assert.Equal(new[] { 0 }, call.SentDuringCall);
        Assert.Same(hunting, s.CurrentMap);
        Assert.NotNull(hunting.GetPlayer(s.EntityId));
        Assert.Null(Town.GetPlayer(s.EntityId));
    }

    // T5 · 완료조건 5, IM-13 ④가 ⑤보다 먼저: 마을의 o를 사냥터로 보낸다(사냥터는 공용 맵이 아니라 ⑤도 어긋난다). w는 마을 관찰자.
    [Fact]
    public void PlayerInAPublicMap_IsRejectedAsNotInInstanceMap()
    {
        InstanceProbeSession o = _fixture.Join();
        InstanceProbeSession w = _fixture.Join();

        MapMoveResult? result = null;
        JobRecord call = RunInMapJob(Town, () => result = MapMigration.MoveToPublicMap(o, o.EntityId, Town, MapId.HuntingGround), o, w);
        _fixture.Tick();

        Assert.Null(call.Thrown);
        Assert.Equal(MapMoveResult.NotInInstanceMap, result);
        Assert.Equal(new[] { 0, 0 }, call.SentDuringCall);
        Assert.Same(Town, o.CurrentMap);
        Assert.NotNull(Town.GetPlayer(o.EntityId));
    }

    // T5 · 완료조건 5, IM-13 ⑤: 사냥터의 파티원 m을 보스방으로 보낸다.
    [Fact]
    public void NonPublicDestination_IsRejectedAsDestinationNotPublic()
    {
        InstanceProbeSession m = _fixture.Join();
        InstanceProbeSession n = _fixture.Join();
        _fixture.FormParty(m, n);
        GameMap hunting = _fixture.MoveThroughPortal(m, portalId: 1);
        _fixture.MoveThroughPortal(n, portalId: 1);

        MapMoveResult? result = null;
        JobRecord call = RunInMapJob(hunting, () => result = MapMigration.MoveToPublicMap(m, m.EntityId, hunting, MapId.BossRoom), m, n);
        _fixture.Tick();

        Assert.Null(call.Thrown);
        Assert.Equal(MapMoveResult.DestinationNotPublic, result);
        Assert.Equal(new[] { 0, 0 }, call.SentDuringCall);
        Assert.Same(hunting, m.CurrentMap);
        Assert.NotNull(hunting.GetPlayer(m.EntityId));
    }

    // Content 처리기처럼 그 맵의 job 안에서 move를 실행한다. 실행 중 예외와, 지켜볼 세션들이 그동안 받은 송신 수를 잡는다.
    // 결과값은 각 시험이 지역 변수로 받는다. 그래서 각 시험에서 고정한 호출식과 인자를 그대로 읽을 수 있다.
    JobRecord RunInMapJob(GameMap currentMap, Action move, params InstanceProbeSession[] watched)
    {
        JobRecord record = new();
        currentMap.EnqueueJob(() =>
        {
            int[] before = watched.Select(session => session.Packets.Count).ToArray();
            record.Thrown = Record.Exception(move);
            record.SentDuringCall = watched.Select((session, i) => session.Packets.Count - before[i]).ToArray();
        });
        return record;
    }

    // 실제 근접 공격으로 마을 적을 처치해 보상을 만든다(InventoryLifecycleRaceTests와 같은 서기·정착 순서).
    void KillNormalInTown(InstanceProbeSession attacker)
    {
        Town.GetPlayer(attacker.EntityId)!.Position = new Vector2(10f, 0f);
        _fixture.Settle();
        EnemyEntity enemy = Town.SpawnEnemy(EnemyKind.Normal, 11f, 0f, maxHp: 1);
        attacker.OnRecvPacket(new C_Attack { targetEntityId = enemy.EntityId, attackerClientTick = (int)(_fixture.LastTick + 1) }.Write());
        _fixture.Tick();
        Assert.False(Town.Enemies.ContainsKey(enemy.EntityId), "fixture: 실제 근접 공격 경로가 적을 처치해야 한다");
        _fixture.Settle();
    }

    (uint Revision, int Currency) QueryInventory(InstanceProbeSession session)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(new C_InventoryRequest().Write());
        _fixture.Settle();
        byte[] reply = Assert.Single(session.Packets.Skip(mark), p => IdOf(p) == PacketID.S_InventorySnapshot);
        S_InventorySnapshot snapshot = Read<S_InventorySnapshot>(reply);
        return (snapshot.revision, snapshot.currency);
    }

    sealed class JobRecord
    {
        internal Exception? Thrown;
        internal int[] SentDuringCall = Array.Empty<int>();
    }
}
