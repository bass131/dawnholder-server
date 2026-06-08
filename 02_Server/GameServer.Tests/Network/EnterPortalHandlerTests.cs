using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// C_EnterPortal 핸들러 단위 테스트.
///
/// **검증 invariant**:
///   1. happy — portal 근처에서 올바른 portalId 전송 → 맵 이동 실행 (S_MapTransition 수신)
///   2. reject_far — portal에서 멀리 떨어진 위치에서 전송 → silent drop (맵 이동 없음)
///   3. reject_invalid — 현재 맵에 없는 portalId 전송 → silent drop
///   4. auth_failure — handshake 미완료 상태에서 전송 → first-packet 게이트 Disconnect
///   5. class_not_selected — handshake 완료지만 캐릭터 선택 전 → silent drop
///
/// **테스트 전략**:
///   - TestGameSession이 GetMap(currentMap) + GetDestMap(destMap) 양쪽 override
///     → GameWorld singleton 없이 두 맵 주입 가능 (격리 보장)
///   - portal 근접 검증 = tick thread 안에서 실행 → map.Tick() 1회로 처리
///   - 헌법 #3 (Trust Boundary): 근접 실패 / invalid portalId = silent drop (disconnect X)
/// </summary>
[Collection("ConsoleSerial")]
public class EnterPortalHandlerTests : IDisposable
{
    // Town 맵: portal x=20 (PortalTable 정의 — Town → HuntingGround, portalId=1)
    // HuntingGround 맵: portal x=25 (HuntingGround → BossRoom, portalId=1)
    //
    // TestGameSession은 두 맵 모두 주입받아 migration 흐름 시뮬.
    readonly GameMap _townMap;
    readonly GameMap _destMap;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // Town portal: portalId=1, position x=20, dest=HuntingGround, destSpawn x=2
    const int ValidPortalId = 1;
    const int InvalidPortalId = 999;
    // Town portal 위치 x=20 — PortalTable.TownPortals 정의와 일치
    const float PortalX = 20f;
    const float PortalY = 0f;

    // 근접 임계 = 2 unit (SubmitEnterPortal 내부 ProximityThreshold)
    // portal (20, 0)에서 1.5 unit 안: (18.5, 0) → 통과
    static readonly Vector2 NearPortalPos = new Vector2(PortalX - 1.5f, PortalY);
    // portal (20, 0)에서 10 unit: (10, 0) → 실패 (dist²=100 > 4)
    static readonly Vector2 FarFromPortalPos = new Vector2(10f, 0f);

    // TestGameSession: 두 맵 모두 주입 (GetMap=currentMap, GetDestMap=destMap)
    class TestGameSession : GameSession
    {
        readonly GameMap _currentMap;
        readonly GameMap? _destMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap currentMap, GameMap? destMap = null)
        {
            _currentMap = currentMap;
            _destMap = destMap;
        }

        protected override GameMap? GetMap()
        {
            // _migrating 체크는 base.GetMap()이 하지만 여기서는 항상 _currentMap 반환.
            // migration 중 null이어야 하는 케이스는 별도 테스트에서 다룸.
            return _currentMap;
        }

        protected override GameMap? GetDestMap(MapId destMapId) => _destMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        // handshake + class 선택 완료 + 월드 진입 mock (플레이어 entity 등록).
        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0); // Knight
            EnterGameWorldIfReady();
        }
    }

    public EnterPortalHandlerTests()
    {
        _townMap = new GameMap(MapId.Town);
        _destMap = new GameMap(MapId.HuntingGround);

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

    // C_EnterPortal 패킷 직렬화 헬퍼
    static ArraySegment<byte> EnterPortalPacket(int portalId)
    {
        C_EnterPortal pkt = new C_EnterPortal { portalId = portalId };
        return pkt.Write();
    }

    // handshake + tick으로 player entity 등록 → entityId 세팅된 상태 반환
    TestGameSession SetupHandshakedSession()
    {
        TestGameSession s = new(_townMap, _destMap);
        s.OnConnected(Ep());
        s.BypassHandshake();
        _townMap.Tick(1); // AddPlayer 람다 실행 → _entityId 세팅
        return s;
    }

    // --- 테스트 ---

    [Fact]
    public void Happy_NearPortal_ValidId_Triggers_MapTransition()
    {
        // arrange: player가 portal 근처에 위치
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _townMap.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = NearPortalPos;

        s.SentPackets.Clear(); // 입장 패킷 제거 후 portal 관련 패킷만 검증

        // act: C_EnterPortal 전송 → 맵 A tick에서 검증 + migration 시작
        s.OnRecvPacket(EnterPortalPacket(ValidPortalId));
        _townMap.Tick(2); // 맵 A job 처리 (검증 + RemovePlayer + destMap.EnqueueJob)
        _destMap.Tick(2); // 맵 B job 처리 (AddPlayerWithId + S_MapTransition Send)

        // S_MapTransition 본인에게 전송됐는지 검증
        bool hasTransition = s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition);
        Assert.True(hasTransition, "S_MapTransition이 전송돼야 함 (portal 통과 성공)");

        // S_MapTransition 내용 검증: destMapId=HuntingGround(1), spawnX=2, spawnY=0
        byte[] transitionPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_MapTransition);
        S_MapTransition parsed = new S_MapTransition();
        parsed.Read(new ArraySegment<byte>(transitionPkt));
        Assert.Equal((byte)MapId.HuntingGround, parsed.destMapId);
        Assert.Equal(2f, parsed.spawnX);  // PortalTable TownPortals.DestSpawn.X
        Assert.Equal(0f, parsed.spawnY);

        // 맵 A에서 제거됐는지 확인
        Assert.Empty(_townMap.Players);
        // 맵 B에 추가됐는지 확인
        Assert.Single(_destMap.Players);

        // disconnect 없음
        Assert.Equal(0, s.DisconnectCalls);
    }

    [Fact]
    public void Reject_FarFromPortal_SilentDrop()
    {
        // arrange: player가 portal에서 멀리 위치 (텔레포트 핵 시뮬)
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _townMap.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = FarFromPortalPos; // (10, 0) — portal (20, 0)에서 dist=10

        s.SentPackets.Clear();

        // act
        s.OnRecvPacket(EnterPortalPacket(ValidPortalId));
        _townMap.Tick(2);
        _destMap.Tick(2);

        // S_MapTransition 없음 — 거리 초과 → silent drop
        Assert.False(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition),
            "거리 초과 시 S_MapTransition이 전송되면 안 됨 (텔레포트 핵 차단)");

        // 맵 A에 그대로 존재
        Assert.Single(_townMap.Players);
        // 맵 B에 없음
        Assert.Empty(_destMap.Players);

        // [Trust] 로그 확인 — 근접 실패 로그가 박혔는지
        string log = _consoleCapture.ToString();
        Assert.Contains("portal proximity fail", log);

        // disconnect 없음
        Assert.Equal(0, s.DisconnectCalls);
    }

    [Fact]
    public void Reject_InvalidPortalId_SilentDrop()
    {
        // arrange: player가 portal 근처에 있지만 portalId가 현재 맵에 없는 값
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _townMap.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = NearPortalPos; // 근처에 있어도 portalId 검증 먼저

        s.SentPackets.Clear();

        // act: 없는 portalId=999 전송
        s.OnRecvPacket(EnterPortalPacket(InvalidPortalId));
        _townMap.Tick(2);
        _destMap.Tick(2);

        // S_MapTransition 없음 — invalid portalId → silent drop
        Assert.False(s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition),
            "invalid portalId 시 S_MapTransition이 전송되면 안 됨");

        // 맵 A에 그대로 존재
        Assert.Single(_townMap.Players);
        Assert.Empty(_destMap.Players);

        // [Trust] 로그 확인
        string log = _consoleCapture.ToString();
        Assert.Contains("invalid portalId", log);

        Assert.Equal(0, s.DisconnectCalls);
    }

    [Fact]
    public void AuthFailure_HandshakeIncomplete_Rejected()
    {
        // arrange: handshake 완료 안 한 상태
        TestGameSession s = new(_townMap, _destMap);
        s.OnConnected(Ep());
        // BypassHandshake 호출 안 함

        // act: 첫 패킷으로 C_EnterPortal → first-packet 게이트 차단
        s.OnRecvPacket(EnterPortalPacket(ValidPortalId));
        _townMap.Tick(1);

        // first-packet 게이트: C_EnterPortal은 C_Handshake가 아님 → Disconnect
        Assert.Equal(1, s.DisconnectCalls);
        Assert.Contains("[Trust] First packet was C_EnterPortal", _consoleCapture.ToString());
    }

    [Fact]
    public void ClassNotSelected_BeforeCharacterSelect_SilentDrop()
    {
        // arrange: handshake 완료했지만 캐릭터 미선택 (CharacterSelectHandler 패턴 정합)
        TestGameSession s = new(_townMap, _destMap);
        s.OnConnected(Ep());
        s.CompleteHandshakeAndEnter(); // handshake만 완료, class 선택 X

        // act: C_EnterPortal 전송 → EnterPortalHandler의 HasSelectedClass 게이트에서 drop
        s.OnRecvPacket(EnterPortalPacket(ValidPortalId));
        _townMap.Tick(1);

        // [Trust] 로그 + disconnect 없음 (silent drop)
        string log = _consoleCapture.ToString();
        Assert.Contains("C_EnterPortal before CharacterSelect", log);
        Assert.Equal(0, s.DisconnectCalls);
        Assert.Empty(_townMap.Players); // 맵 진입 자체가 안 된 상태
    }
}
