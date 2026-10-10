using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 인스턴스 맵 요구 시험(goal 2026-10-10-instance-map-lifecycle)의 공통 fixture.
// 실제 수신 경로·포탈 표·월드 틱(끊김 정리와 Party/Quest/Inventory 단계 포함)을 그대로 돌린다.
// 시험이 고정하는 입력은 맵 데이터와 퀘스트 적립 수뿐이고, 기대값은 이 입력의 리터럴이나 실제 질의의 전후 비교로 둔다.
internal sealed class InstanceMapTestWorld : IDisposable
{
    // 마을 기본 입장 지점. 사냥터 포탈 2의 도착점(x=17)·원점과 달라야 「좌표 생략 = 기본 입장 지점」을 구별한다.
    internal const float TownSpawnX = 7f;

    // 사냥터 적은 비선공 Normal이고 포탈·도착 지점(x 2~25)에서 멀어 시험 플레이어를 건드리지 않는다.
    internal const float HuntingGroundEnemyX = 40f;

    // 보스는 감지 반경 밖에 둔다. 처치는 각 시험이 사망 후처리로 직접 일으킨다.
    internal const float BossX = 60f;

    // GameMap 생성자가 거부하는 적 종류 값(EnemyKind는 0~2).
    const byte UnknownEnemyKindId = 99;

    const int SettleTicks = 3;

    long _tick;

    internal InstanceMapTestWorld()
        : this(StandardMapData())
    {
    }

    internal InstanceMapTestWorld(IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> mapData)
    {
        World = new GameWorld(mapData);
    }

    internal GameWorld World { get; }

    internal GameMap Town => World.GetMap(MapId.Town)!;

    // 다음 Tick이 쓸 번호의 바로 앞. 근접 공격의 clientTick을 맞출 때 쓴다.
    internal long LastTick => _tick;

    public void Dispose() => World.Stop();

    internal static IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> StandardMapData()
        => new Dictionary<MapId, (MapTerrain?, MapContent?)>
        {
            [MapId.Town] = (null, new MapContent(TownSpawnX, 0f, Array.Empty<EnemySpawnPoint>())),
            [MapId.HuntingGround] = (null, new MapContent(0f, 0f, new[]
            {
                new EnemySpawnPoint((byte)EnemyKind.Normal, HuntingGroundEnemyX, 0f),
                new EnemySpawnPoint((byte)EnemyKind.Normal, HuntingGroundEnemyX + 4f, 0f),
            })),
            [MapId.BossRoom] = (null, new MapContent(0f, 0f, new[]
            {
                new EnemySpawnPoint((byte)EnemyKind.Boss, BossX, 0f),
            })),
        };

    // 사냥터 맵을 만들 수 없는 데이터. 마을은 정상이라 플레이어는 들어올 수 있다.
    internal static IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> BrokenHuntingGroundMapData()
        => new Dictionary<MapId, (MapTerrain?, MapContent?)>
        {
            [MapId.Town] = (null, new MapContent(TownSpawnX, 0f, Array.Empty<EnemySpawnPoint>())),
            [MapId.HuntingGround] = (null, new MapContent(0f, 0f, new[]
            {
                new EnemySpawnPoint(UnknownEnemyKindId, HuntingGroundEnemyX, 0f),
            })),
        };

    internal static PacketID IdOf(byte[] packet) => (PacketID)BitConverter.ToUInt16(packet, 2);

    internal static T Read<T>(byte[] packet)
        where T : IPacket, new()
    {
        T value = new();
        value.Read(new ArraySegment<byte>(packet));
        return value;
    }

    internal static int[] Ids(params InstanceProbeSession[] sessions)
        => sessions.Select(s => s.EntityId).OrderBy(id => id).ToArray();

    internal static int[] PlayerIds(GameMap map)
        => map.Players.Select(p => p.EntityId).OrderBy(id => id).ToArray();

    internal static GameMap CurrentMapOf(InstanceProbeSession session)
    {
        GameMap? map = session.CurrentMap;
        Assert.True(map != null, "fixture: 세션이 이동 중이 아니고 맵 안에 있어야 한다");
        return map!;
    }

    // 받은 패킷이 가리키는 entity 번호: 플레이어 입장·위치와 적 등장.
    internal static IEnumerable<int> MentionedEntityIds(IEnumerable<byte[]> packets)
    {
        foreach (byte[] packet in packets)
        {
            switch (IdOf(packet))
            {
                case PacketID.S_PlayerJoin:
                    yield return Read<S_PlayerJoin>(packet).entityId;
                    break;
                case PacketID.S_Snapshot:
                    yield return Read<S_Snapshot>(packet).entityId;
                    break;
                case PacketID.S_EntitySpawn:
                    yield return Read<S_EntitySpawn>(packet).entityId;
                    break;
            }
        }
    }

    internal void Tick() => LifecycleTestWorld.Tick(World, ++_tick);

    internal void Settle(int ticks = SettleTicks)
    {
        for (int i = 0; i < ticks; i++) Tick();
    }

    // 월드 정리 단계보다 먼저 한 맵의 job만 실행한다(SessionCleanupTests의 맵 단독 Tick과 같은 고정 방법).
    internal void TickOnly(GameMap map) => map.Tick(++_tick);

    internal InstanceProbeSession Join()
    {
        InstanceProbeSession session = new(World);
        session.OnConnected(InstanceProbeSession.Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());
        Tick();
        Assert.True(session.EntityId >= 0, "fixture: 실제 핸드셰이크·클래스 선택으로 마을에 들어가야 한다");
        return session;
    }

    // 열쇠는 입장 때의 소속으로 정해지므로 포탈 전에 부른다. 실제 초대·수락 패킷을 쓴다.
    internal int FormParty(InstanceProbeSession inviter, InstanceProbeSession invitee)
    {
        inviter.OnRecvPacket(new C_PartyInvite { targetEntityId = invitee.EntityId }.Write());
        Tick();
        invitee.OnRecvPacket(new C_PartyRespond { inviterEntityId = inviter.EntityId, accept = 1 }.Write());
        Tick();
        PartyState? party = World.Party.GetPartyByEntity(inviter.EntityId);
        Assert.True(party != null && party.Members.Contains(invitee.EntityId), "fixture: 초대·수락으로 2인 파티가 생겨야 한다");
        return party!.PartyId;
    }

    internal void LeaveParty(InstanceProbeSession member)
    {
        member.OnRecvPacket(new C_PartyLeave().Write());
        Settle();
        Assert.True(World.Party.GetPartyByEntity(member.EntityId) == null, "fixture: 탈퇴 패킷으로 파티에서 빠져야 한다");
    }

    // 처치 적립 진입점으로 진행을 채운다. 이 시험들이 보는 것은 진행이 지워지는 범위라 적립은 입력이다.
    internal void CreditKills(int entityId, int count)
    {
        for (int i = 0; i < count; i++) World.Quest.OnKill(entityId);
    }

    internal void UnlockBossRoom(int entityId) => CreditKills(entityId, QuestConstants.BossUnlockKillCount);

    // 포탈 좌표는 제품 포탈 표에서 읽는다. 기대값이 아니라 서 있을 자리라는 입력이다.
    internal void EnterPortal(InstanceProbeSession session, int portalId)
    {
        GameMap map = CurrentMapOf(session);
        map.GetPlayer(session.EntityId)!.Position = PortalOf(map, portalId).Position;
        session.OnRecvPacket(new C_EnterPortal { portalId = portalId }.Write());
    }

    internal GameMap MoveThroughPortal(InstanceProbeSession session, int portalId)
    {
        MapId destination = PortalOf(CurrentMapOf(session), portalId).Dest;
        EnterPortal(session, portalId);
        Settle();
        GameMap? arrived = session.CurrentMap;
        Assert.True(arrived != null && arrived.MapId == destination && arrived.GetPlayer(session.EntityId) != null,
            $"fixture: 포탈 {portalId}로 {destination}에 도착해야 한다");
        return arrived!;
    }

    internal void SendMoveRight(InstanceProbeSession session, int times)
    {
        for (int i = 0; i < times; i++)
        {
            session.OnRecvPacket(new C_MoveIntent { input = InputBits.Encode(1, false), clientTick = (uint)(_tick + 1) }.Write());
            Tick();
        }
    }

    // 정리된 복사본은 아무도 Tick하지 않으므로 거기 넣은 job이 돌지 않는다. 그 맵이 아직 월드 틱 안에 있는지 본다.
    internal bool IsStillTicked(GameMap map)
    {
        bool ran = false;
        map.EnqueueJob(() => ran = true);
        Tick();
        return ran;
    }

    static Portal PortalOf(GameMap map, int portalId)
        => PortalTable.GetPortalsFor(map.MapId).Single(p => p.PortalId == portalId);
}

// 실제 GameSession 수신 경로를 쓰고 송신만 모은다. 지금 맵은 제품 hook GetMap()으로 읽는다.
internal sealed class InstanceProbeSession : GameSession
{
    internal static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    internal readonly List<byte[]> Packets = new();

    internal InstanceProbeSession(GameWorld world)
        : base(world)
    {
    }

    // 소켓 스레드가 정리 직전에 옛 맵을 읽은 경우를 재현할 때만 채운다. 비어 있으면 제품 조회 그대로다.
    internal GameMap? StaleMapRead { get; set; }

    // 세션이 실제로 가리키는 맵. 이동 중이면 null이다.
    internal GameMap? CurrentMap => base.GetMap();

    public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());

    public override void Disconnect() => OnDisconnected(Endpoint);

    internal void Close() => OnDisconnected(Endpoint);

    protected override GameMap? GetMap() => StaleMapRead ?? base.GetMap();
}
