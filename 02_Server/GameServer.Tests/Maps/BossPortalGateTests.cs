using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

/// <summary>
/// 보스 포탈 잠금 게이트 테스트 (M5 Q3 trust-boundary).
///
/// <b>trust-boundary 4불변식 검증:</b>
///   1. 게이트는 RemovePlayer/SetMigrating *전*: 거부 시 원래 맵 잔류 (ghost 0).
///   2. killCount는 서버 권위: getKillCount delegate(stub) — 클라 주장 X.
///   3. entityId는 서버 _entityId: Execute에 캡처된 값 사용 (패킷 값 X).
///   4. Dest==BossRoom 진입 방향만 게이트: 역방향(Boss→HG)은 게이트 X.
///
/// <b>테스트 케이스:</b>
///   1. killCount &lt; 20 → 거부 + S_PortalLocked 송신, 원래 맵 잔류 (ghost 미발생).
///   2. killCount >= 20 → BossRoom 진입 성공 (RemovePlayer + AddPlayer 정상).
///   3. ghost 미발생: 거부 후 currentMap.GetPlayer(entityId) != null.
///   4. S_PortalLocked 필드 검증: requiredCount=20, currentCount=N.
///   5. 역방향/비-BossRoom Dest: killCount=0이어도 게이트 X (통과).
/// </summary>
[Collection("ConsoleSerial")]
public class BossPortalGateTests : IDisposable
{
    // HuntingGround → BossRoom portal: portalId=1, position x=25, destSpawn (22, 0)
    // PortalTable.HuntingGroundPortals 정합.
    const int HgToBossPortalId = 1;
    static readonly Vector2 HgPortalPos = new Vector2(25f, 0f);
    static readonly Vector2 NearHgPortal = new Vector2(25f - 1.5f, 0f); // 1.5 unit 안 — 근접검증 통과

    // Town → HuntingGround portal: portalId=1, position x=20
    // 역방향 게이트 미적용 검증용 (Town portal Dest=HuntingGround).
    static readonly Vector2 NearTownPortal = new Vector2(20f - 1.5f, 0f);

    readonly GameMap _huntingGround;
    readonly GameMap _bossRoom;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // ── 테스트 세션 헬퍼 ─────────────────────────────────────────────────────

    // 킬카운트 stub 주입 가능한 migration 전용 세션.
    // GetMap(현재 맵) + GetDestMap(목적지 맵) + GetKillCount(stub) override.
    class GateTestSession : GameSession
    {
        GameMap _currentMap;
        readonly GameMap _destMap;
        readonly int _stubKillCount;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public GateTestSession(GameMap currentMap, GameMap destMap, int stubKillCount)
        {
            _currentMap = currentMap;
            _destMap = destMap;
            _stubKillCount = stubKillCount;
        }

        protected override GameMap? GetMap() => _currentMap;
        protected override GameMap? GetDestMap(MapId destMapId) => _destMap;
        // trust-boundary 불변식 #2: killCount는 서버 권위. stub이 PartyRegistry 역할 대행.
        protected override int GetKillCount(int entityId) => _stubKillCount;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0); // Knight
            EnterGameWorldIfReady();
        }
    }

    // ── 픽스처 ────────────────────────────────────────────────────────────────

    public BossPortalGateTests()
    {
        _huntingGround = new GameMap(MapId.HuntingGround);
        _bossRoom = new GameMap(MapId.BossRoom);

        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    static IPEndPoint Ep() => new IPEndPoint(IPAddress.Loopback, 0);

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    // player를 맵에 진입시키고 portal 근처에 위치시킨 세션 반환
    GateTestSession SetupSession(GameMap current, GameMap dest, int stubKillCount, Vector2 nearPos)
    {
        GateTestSession s = new(current, dest, stubKillCount);
        s.OnConnected(Ep());
        s.BypassHandshake();
        current.Tick(1); // AddPlayer 람다 실행 → entity 등록

        PlayerEntity? player = current.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = nearPos;
        return s;
    }

    // ── 테스트 ────────────────────────────────────────────────────────────────

    // 1. killCount < 20 → 거부 + S_PortalLocked 송신
    [Fact]
    public void BossRoom_Entry_Denied_When_KillCount_Below_Threshold()
    {
        int stubKillCount = 15; // < 40

        GateTestSession s = SetupSession(_huntingGround, _bossRoom, stubKillCount, NearHgPortal);
        s.SentPackets.Clear();

        C_EnterPortal pkt = new C_EnterPortal { portalId = HgToBossPortalId };
        s.OnRecvPacket(pkt.Write());
        _huntingGround.Tick(2); // Execute 실행 — 게이트 차단
        _bossRoom.Tick(2);

        // S_PortalLocked 수신 검증
        bool hasLocked = s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_PortalLocked);
        Assert.True(hasLocked, "킬카운트 미달 시 S_PortalLocked를 받아야 함");

        // S_MapTransition 없음 — 진입 거부됨
        Assert.False(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition),
            "킬카운트 미달 시 S_MapTransition이 전송되면 안 됨");
    }

    // 2. S_PortalLocked 필드 검증: requiredCount=20, currentCount=N
    [Fact]
    public void BossRoom_Denied_S_PortalLocked_Fields_Correct()
    {
        int stubKillCount = 7; // < 40

        GateTestSession s = SetupSession(_huntingGround, _bossRoom, stubKillCount, NearHgPortal);
        s.SentPackets.Clear();

        C_EnterPortal pkt = new C_EnterPortal { portalId = HgToBossPortalId };
        s.OnRecvPacket(pkt.Write());
        _huntingGround.Tick(2);
        _bossRoom.Tick(2);

        byte[]? lockedBytes = s.SentPackets.FirstOrDefault(p => PacketIdOf(p) == PacketID.S_PortalLocked);
        Assert.NotNull(lockedBytes);

        S_PortalLocked parsed = new S_PortalLocked();
        parsed.Read(new ArraySegment<byte>(lockedBytes!));

        // SSOT: QuestConstants.BossUnlockKillCount = 20
        Assert.Equal(QuestConstants.BossUnlockKillCount, parsed.requiredCount);
        Assert.Equal(stubKillCount, parsed.currentCount);
    }

    // 3. 핵심 trust-boundary 불변식 #1 — ghost 미발생 검증
    //    거부 시 플레이어가 currentMap에 *그대로 남음* (RemovePlayer 미실행 증명)
    [Fact]
    public void BossRoom_Denied_Player_Remains_In_Current_Map_No_Ghost()
    {
        int stubKillCount = 0; // < 40

        GateTestSession s = SetupSession(_huntingGround, _bossRoom, stubKillCount, NearHgPortal);

        PlayerEntity? playerBefore = _huntingGround.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerBefore);
        int entityId = playerBefore!.EntityId;

        C_EnterPortal pkt = new C_EnterPortal { portalId = HgToBossPortalId };
        s.OnRecvPacket(pkt.Write());
        _huntingGround.Tick(2); // 게이트 차단 — RemovePlayer 미실행
        _bossRoom.Tick(2);

        // trust-boundary 불변식 #1: 거부 시 currentMap에 그대로 잔류 (ghost 방지 핵심).
        // 게이트가 SetMigrating/RemovePlayer *전*에 return했기 때문.
        PlayerEntity? playerAfter = _huntingGround.GetPlayer(entityId);
        Assert.NotNull(playerAfter); // currentMap에 그대로 있어야 함

        // BossRoom에는 아무도 없음
        Assert.Empty(_bossRoom.Players);

        // disconnect 없음 — 거부는 S_PortalLocked 송신 후 return
        Assert.Equal(0, s.DisconnectCalls);
    }

    // 4. killCount >= 20 → BossRoom 진입 성공
    [Fact]
    public void BossRoom_Entry_Allowed_When_KillCount_At_Threshold()
    {
        int stubKillCount = 20; // >= 20 (정확히 임계)

        GateTestSession s = SetupSession(_huntingGround, _bossRoom, stubKillCount, NearHgPortal);
        s.SentPackets.Clear();

        C_EnterPortal pkt = new C_EnterPortal { portalId = HgToBossPortalId };
        s.OnRecvPacket(pkt.Write());
        _huntingGround.Tick(2);
        _bossRoom.Tick(2);

        // S_MapTransition 수신 — 진입 성공
        bool hasTransition = s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition);
        Assert.True(hasTransition, "킬카운트 충족 시 BossRoom 진입이 성공해야 함");

        // HuntingGround에서 제거됨
        Assert.Empty(_huntingGround.Players);

        // BossRoom에 추가됨
        Assert.Single(_bossRoom.Players);

        // S_PortalLocked 없음 — 통과
        Assert.False(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_PortalLocked),
            "킬카운트 충족 시 S_PortalLocked가 전송되면 안 됨");
    }

    // 5. killCount 초과 (> 20)도 진입 성공
    [Fact]
    public void BossRoom_Entry_Allowed_When_KillCount_Above_Threshold()
    {
        int stubKillCount = 99; // > 20

        GateTestSession s = SetupSession(_huntingGround, _bossRoom, stubKillCount, NearHgPortal);
        s.SentPackets.Clear();

        C_EnterPortal pkt = new C_EnterPortal { portalId = HgToBossPortalId };
        s.OnRecvPacket(pkt.Write());
        _huntingGround.Tick(2);
        _bossRoom.Tick(2);

        Assert.True(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition),
            "킬카운트 초과 시도 진입 성공해야 함");
        Assert.Empty(_huntingGround.Players);
        Assert.Single(_bossRoom.Players);
    }

    // 6. trust-boundary 불변식 #4 — 역방향/비-BossRoom Dest: 게이트 X
    //    Town→HuntingGround portal (Dest=HuntingGround): killCount=0이어도 통과
    [Fact]
    public void NonBossRoom_Dest_Portal_Not_Gated_KillCount_Zero()
    {
        // Town → HuntingGround 이동. Dest=HuntingGround → 게이트 조건 false.
        GameMap town = new GameMap(MapId.Town);
        GameMap hg = new GameMap(MapId.HuntingGround);
        int stubKillCount = 0; // 킬 0이어도 게이트 X

        GateTestSession s = new(town, hg, stubKillCount);
        s.OnConnected(Ep());
        s.BypassHandshake();
        town.Tick(1);

        PlayerEntity? player = town.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = NearTownPortal; // Town portal 근처
        s.SentPackets.Clear();

        C_EnterPortal pkt = new C_EnterPortal { portalId = 1 }; // Town portal portalId=1
        s.OnRecvPacket(pkt.Write());
        town.Tick(2);
        hg.Tick(2);

        // 게이트 없이 통과
        Assert.True(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition),
            "비BossRoom Dest portal은 킬카운트 0이어도 이동해야 함");
        Assert.Empty(town.Players);
        Assert.Single(hg.Players);

        // S_PortalLocked 없음
        Assert.False(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_PortalLocked),
            "비BossRoom Dest portal은 S_PortalLocked를 보내면 안 됨");
    }

    // 7. [Gate] 로그 박힘 확인
    [Fact]
    public void BossRoom_Denied_Gate_Log_Printed()
    {
        GateTestSession s = SetupSession(_huntingGround, _bossRoom, stubKillCount: 5, NearHgPortal);

        C_EnterPortal pkt = new C_EnterPortal { portalId = HgToBossPortalId };
        s.OnRecvPacket(pkt.Write());
        _huntingGround.Tick(2);

        string log = _consoleCapture.ToString();
        Assert.Contains("[Gate]", log);
        Assert.Contains("BossRoom locked", log);
        Assert.Contains("entry denied", log);
    }
}
