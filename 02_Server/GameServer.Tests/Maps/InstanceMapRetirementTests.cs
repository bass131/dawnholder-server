using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 복사본이 정리될 때 함께 사라지는 것과, 도착 job이 예외로 끝나도 입장 대기가 풀리는지의 독립 검증.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 4, 「현재 결과」의 「구현」 절 확인 항목 5와
//   server.md 「맵 이동과 복사본 수명」의 마지막 문장(리드 답 msg_c38bcd08583e 조건 A), goal-review IM-02·IM-03.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapRetirementTests : IDisposable
{
    // 사냥터 포탈 2(마을행) 옆이다. Mage가 x=5에서 x=6의 적에게 쏜 투사체는 두 틱 이상 날아간다.
    const float MageX = 5f;
    const float TargetX = 6f;

    readonly InstanceMapTestWorld _fixture = new();

    GameWorld World => _fixture.World;

    public void Dispose() => _fixture.Dispose();

    // 확인 항목 5: 혼자 Mage가 자기 사냥터 복사본에서 투사체를 쏘고, 맞기 전 틱에 포탈 2로 마을에 간다. 들어오는 사람은 없다.
    [Fact]
    public void LastPlayerLeavingWithAProjectileInFlight_RetiresTheInstance_AndTheProjectileNeverKillsOrRewards()
    {
        InstanceProbeSession mage = JoinAs(CharacterClass.Mage);
        GameMap hunting = _fixture.MoveThroughPortal(mage, portalId: 1);
        EnemyEntity target = PlaceMageBesideATarget(hunting, mage);
        int travelTicks = LaunchProjectile(mage, target);
        int mark = mage.Packets.Count;

        _fixture.EnterPortal(mage, portalId: 2);
        _fixture.Tick();
        bool inFlightAtTheLeaveTick = hunting.Enemies.ContainsKey(target.EntityId);
        int liveInstancesAfterTheLeaveTick = World.LiveInstanceCount;
        _fixture.Settle(travelTicks + 3);
        bool targetStillPresent = hunting.Enemies.ContainsKey(target.EntityId);
        int economyPushes = mage.Packets.Skip(mark).Count(p => IdOf(p) == PacketID.S_InventorySnapshot);
        (uint revision, int currency) = QueryInventory(mage);

        Assert.True(inFlightAtTheLeaveTick, "fixture: 떠나는 틱에는 투사체가 아직 날아가는 중이어야 한다");
        Assert.Equal(0, liveInstancesAfterTheLeaveTick);
        Assert.True(targetStillPresent);
        Assert.Same(_fixture.Town, mage.CurrentMap);
        Assert.Equal(0, economyPushes);
        Assert.Equal(0, World.Quest.GetSoloProgress(mage.EntityId));
        Assert.Equal(0u, revision);
        Assert.Equal(0, currency);
    }

    // 확인 항목 5의 대조군: 같은 입력에서 같은 파티 관찰자가 그 복사본에 남으면 투사체가 맞아 처치·보상이 생긴다.
    [Fact]
    public void ProjectileInFlight_StillKillsAndRewards_WhenAPartyMemberKeepsTheInstanceAlive()
    {
        InstanceProbeSession mage = JoinAs(CharacterClass.Mage);
        InstanceProbeSession observer = _fixture.Join();
        int party = _fixture.FormParty(mage, observer);
        GameMap hunting = _fixture.MoveThroughPortal(mage, portalId: 1);
        GameMap observerHunting = _fixture.MoveThroughPortal(observer, portalId: 1);
        EnemyEntity target = PlaceMageBesideATarget(hunting, mage);
        int travelTicks = LaunchProjectile(mage, target);
        int mark = mage.Packets.Count;

        _fixture.EnterPortal(mage, portalId: 2);
        _fixture.Tick();
        bool inFlightAtTheLeaveTick = hunting.Enemies.ContainsKey(target.EntityId);
        _fixture.Settle(travelTicks + 3);
        bool targetStillPresent = hunting.Enemies.ContainsKey(target.EntityId);
        int economyPushes = mage.Packets.Skip(mark).Count(p => IdOf(p) == PacketID.S_InventorySnapshot);

        Assert.Same(hunting, observerHunting);
        Assert.True(inFlightAtTheLeaveTick, "fixture: 떠나는 틱에는 투사체가 아직 날아가는 중이어야 한다");
        Assert.Equal(1, World.LiveInstanceCount);
        Assert.False(targetStillPresent);
        Assert.Same(_fixture.Town, mage.CurrentMap);
        Assert.Equal(1, economyPushes);
        Assert.Equal(1, World.Quest.GetPartyProgress(party));
    }

    // IM-02: 혼자 t가 마을 포탈 1로 새 사냥터 복사본에 들어가고, 다음 틱의 도착 job이 S_MapTransition을 보내다 예외를 낸다.
    [Fact]
    public void ArrivalThatThrowsWhileSending_ReleasesTheIncomingCount_AndTheInstanceIsRetired()
    {
        TransitionSendFailingSession t = JoinFailingOnTransition();
        int tId = t.EntityId;
        t.FailMapTransitionSend = true;
        Portal townPortal = PortalTable.GetPortalsFor(MapId.Town).Single(p => p.PortalId == 1);
        _fixture.Town.GetPlayer(tId)!.Position = townPortal.Position;

        t.OnRecvPacket(new C_EnterPortal { portalId = (byte)townPortal.PortalId }.Write());
        _fixture.Tick();
        bool openedAndKeptWhileIncoming = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(tId), out GameMap? hunting);
        _fixture.Tick();
        bool arrivalThrew = t.MapTransitionSendFailed;
        _fixture.Settle();
        bool instanceStillRegistered = World.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(tId), out _);

        Assert.True(openedAndKeptWhileIncoming, "fixture: 포탈 job이 혼자 열쇠 복사본을 만들고 입장 대기 동안 남겨야 한다");
        Assert.True(arrivalThrew, "fixture: 도착 job이 S_MapTransition 송신에서 예외를 내야 한다");
        Assert.False(instanceStillRegistered);
        Assert.Equal(0, World.LiveInstanceCount);
        Assert.True(World.AllLiveMaps.All(map => map.GetPlayer(tId) == null));
        Assert.Equal(-1, t.EntityId);
        Assert.False(_fixture.IsStillTicked(hunting!));
    }

    InstanceProbeSession JoinAs(CharacterClass characterClass)
    {
        InstanceProbeSession session = new(World);
        session.OnConnected(InstanceProbeSession.Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)characterClass }.Write());
        _fixture.Tick();
        Assert.True(session.EntityId >= 0, "fixture: 실제 핸드셰이크·클래스 선택으로 마을에 들어가야 한다");
        return session;
    }

    TransitionSendFailingSession JoinFailingOnTransition()
    {
        TransitionSendFailingSession session = new(World);
        session.OnConnected(InstanceProbeSession.Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());
        _fixture.Tick();
        Assert.True(session.EntityId >= 0, "fixture: 실제 핸드셰이크·클래스 선택으로 마을에 들어가야 한다");
        return session;
    }

    // 자리를 옮긴 뒤 몇 틱을 돌려 되감기 공격 판정이 새 위치를 보게 한다(InventoryMigrationLifetimeTests와 같은 서기).
    EnemyEntity PlaceMageBesideATarget(GameMap hunting, InstanceProbeSession mage)
    {
        hunting.GetPlayer(mage.EntityId)!.Position = new Vector2(MageX, 0f);
        _fixture.Settle();
        return hunting.SpawnEnemy(EnemyKind.Normal, TargetX, 0f, maxHp: 1);
    }

    // 실제 Mage 공격 입력이 지연 피해 투사체를 큐에 넣고 비행 틱 수를 알린다.
    int LaunchProjectile(InstanceProbeSession mage, EnemyEntity target)
    {
        int mark = mage.Packets.Count;
        mage.OnRecvPacket(new C_Attack { targetEntityId = target.EntityId, attackerClientTick = (int)(_fixture.LastTick + 1) }.Write());
        _fixture.Tick();
        byte[] launch = Assert.Single(mage.Packets.Skip(mark), p => IdOf(p) == PacketID.S_ProjectileLaunch);
        int travelTicks = Read<S_ProjectileLaunch>(launch).travelTicks;
        Assert.True(travelTicks >= 2, "fixture: 투사체가 두 틱 이상 날아가야 떠나는 틱에 아직 맞지 않는다");
        return travelTicks;
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

    // 실제 GameSession 수신·도착 경로를 쓰고, 켜 두면 S_MapTransition 송신만 실패시킨다.
    sealed class TransitionSendFailingSession : GameSession
    {
        internal TransitionSendFailingSession(GameWorld world)
            : base(world)
        {
        }

        internal bool FailMapTransitionSend { get; set; }

        internal bool MapTransitionSendFailed { get; private set; }

        public override void Send(ArraySegment<byte> packet)
        {
            if (FailMapTransitionSend && IdOf(packet.ToArray()) == PacketID.S_MapTransition)
            {
                MapTransitionSendFailed = true;
                throw new InvalidOperationException("시험 입력: S_MapTransition 송신 실패");
            }
        }

        public override void Disconnect() => OnDisconnected(InstanceProbeSession.Endpoint);
    }
}
