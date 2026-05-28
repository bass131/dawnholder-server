using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

// M4.2 Phase 01 (결정 2 모듈화 갱신): GameMap.NormalEnemyMaxHp 제거 → MapSpawnTable 조회로 대체.

namespace GameServer.Tests.Network;

/// <summary>
/// M4.1 Phase 02 (P0-1 + P0-2 봉합 — 세션 상태 머신 회귀 안전망):
/// handshake → class 선택 → 월드 진입 순서 강제 + 선택 전 입력 silent drop 검증.
///
/// **P0-1 봉합**: handshake 후 class 선택 없이 월드 진입 불가 (옛 결함 = handshake만 해도 입장됨).
/// **P0-2 봉합**: C_CharacterSelect 없이 C_MoveIntent/C_Attack 보내면 서버가 silent drop.
///
/// **검증 invariant** (Phase 02 완료 조건 6건 1:1 정합):
///   1. EnterGameWorld_WithoutHandshake_Rejected — handshake 안 박힘 + class 선택 시도 = silent reject
///   2. EnterGameWorld_WithoutCharacterSelect_Rejected — handshake OK 후 월드 미진입 (P0-1 핵심 회귀 방어)
///   3. EnterGameWorld_AfterCharacterSelect_Success — handshake → CharacterSelect → 월드 진입 정상 transition
///   4. MoveIntent_BeforeCharacterSelect_Dropped — class 선택 전 C_MoveIntent silent drop
///   5. Attack_BeforeCharacterSelect_Dropped — class 선택 전 C_Attack silent drop
///   6. CharacterSelect_DuplicateAfterEnter_Rejected — class 선택 후 두 번째 C_CharacterSelect 차단
///
/// **테스트 전략** (AttackHandlerTests / CharacterSelectHandlerTests 패턴 정합):
///   - GameMap 직접 주입(GetMap override) → singleton race 차단
///   - Send/Disconnect override로 I/O 차단
///   - handshake는 OnRecvPacket으로 실제 핸들러 경유 (state machine 검증이 목적이라 우회 X)
///
/// **헌법 정합**:
///   - 헌법 #1 (Server Authority): 월드 진입 조건은 서버가 결정
///   - 헌법 #3 (Trust Boundary): class 선택 전 입력 = silent drop + [Trust] 경고
///   - 헌법 #5 (No Blocking): 상태 검사는 sync, 틱 마샬링은 EnqueueJob
/// </summary>
[Collection("ConsoleSerial")]
public class SessionStateMachineTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap? GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        // class 선택 완료 여부 노출 (protected internal 접근).
        public bool ClassSelected => HasSelectedClass;
    }

    public SessionStateMachineTests()
    {
        _map = new GameMap();
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    // --- 헬퍼 ---

    static ArraySegment<byte> HandshakePacket()
    {
        C_Handshake pkt = new() { clientVersion = ProtocolVersion.Current };
        return pkt.Write();
    }

    static ArraySegment<byte> CharacterSelectPacket(byte characterClass = 0)
    {
        C_CharacterSelect pkt = new() { characterClass = characterClass };
        return pkt.Write();
    }

    static ArraySegment<byte> MoveIntentPacket()
    {
        C_MoveIntent pkt = new() { input = 0b00000010, clientTick = 1 }; // +1 input
        return pkt.Write();
    }

    static ArraySegment<byte> AttackPacket(int targetEntityId = 1)
    {
        C_Attack pkt = new() { targetEntityId = targetEntityId };
        return pkt.Write();
    }

    // 테스트 세션 연결 + OnConnected 호출 (handshake 대기 상태).
    TestGameSession CreateConnectedSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        return s;
    }

    // PacketID 추출 헬퍼 (BroadcastTests / AttackHandlerTests 패턴 정합).
    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    // --- 6건 회귀 ---

    /// <summary>
    /// 테스트 1: handshake 없이 class 선택 시도 — first-packet 게이트가 차단.
    /// handshake 미완료 상태에서 C_CharacterSelect 보내면 Disconnect됨 (헌법 #3 정합).
    /// </summary>
    [Fact]
    public void EnterGameWorld_WithoutHandshake_Rejected()
    {
        // arrange: handshake 안 한 상태.
        TestGameSession s = CreateConnectedSession();

        // act: 첫 패킷으로 C_CharacterSelect 시도 — handshake 게이트에서 차단.
        s.OnRecvPacket(CharacterSelectPacket(0));
        _map.Tick(1);

        // assert: first-packet 게이트가 Disconnect 처리 + player 맵에 없음.
        Assert.Equal(1, s.DisconnectCalls);
        Assert.Empty(_map.Players);
        // class 선택 상태 X.
        Assert.False(s.ClassSelected);
    }

    /// <summary>
    /// 테스트 2: handshake OK 후 class 선택 없이 — 월드 진입 안 됨 (P0-1 핵심 회귀 방어).
    /// 옛 코드 = handshake 후 바로 EnterGameWorld 호출 → 클래스 없이 입장.
    /// 새 코드 = handshake는 상태 전이만, EnterGameWorld는 CharacterSelect 후.
    /// </summary>
    [Fact]
    public void EnterGameWorld_WithoutCharacterSelect_Rejected()
    {
        // arrange + act: handshake 정상 처리.
        TestGameSession s = CreateConnectedSession();
        s.OnRecvPacket(HandshakePacket());

        // S_HandshakeResult(ok=true) 회신 확인.
        Assert.Contains(s.SentPackets, p => PacketIdOf(p) == PacketID.S_HandshakeResult);

        // tick 후에도 player=0 — class 선택 없이 EnterGameWorld 안 불림.
        _map.Tick(1);
        Assert.Empty(_map.Players); // P0-1 봉합 핵심 검증

        // S_EnterMap 패킷도 없어야 함 (EnterGameWorld 안 불렸으니).
        Assert.DoesNotContain(s.SentPackets, p => PacketIdOf(p) == PacketID.S_EnterMap);
        Assert.Equal(0, s.DisconnectCalls);
    }

    /// <summary>
    /// 테스트 3: handshake → CharacterSelect → 월드 진입 정상 transition.
    /// 두 조건 모두 충족 시 EnterGameWorldIfReady()가 EnterGameWorld()를 호출하는 경로 검증.
    /// </summary>
    [Fact]
    public void EnterGameWorld_AfterCharacterSelect_Success()
    {
        // arrange + act: handshake → class 선택 (Warrior).
        TestGameSession s = CreateConnectedSession();
        s.OnRecvPacket(HandshakePacket());

        // 아직 class 선택 전 — 월드 미진입 확인.
        _map.Tick(1);
        Assert.Empty(_map.Players);

        // class 선택 → EnterGameWorldIfReady() 호출 → EnterGameWorld() 실행.
        s.OnRecvPacket(CharacterSelectPacket(0)); // Warrior

        // tick 후 player=1 — 정상 월드 진입.
        _map.Tick(2);
        Assert.Single(_map.Players);

        // S_EnterMap 패킷 회신 확인 (EnterGameWorld 실행 증거).
        Assert.Contains(s.SentPackets, p => PacketIdOf(p) == PacketID.S_EnterMap);
        Assert.True(s.ClassSelected);
        Assert.Equal(0, s.DisconnectCalls);
    }

    /// <summary>
    /// 테스트 4: class 선택 전 C_MoveIntent silent drop (P0-2 봉합).
    /// handshake 완료 후 class 선택 전 상태에서 C_MoveIntent 수신 시 [Trust] 로그 + drop.
    /// </summary>
    [Fact]
    public void MoveIntent_BeforeCharacterSelect_Dropped()
    {
        // arrange: handshake 완료 + class 선택 전.
        TestGameSession s = CreateConnectedSession();
        s.OnRecvPacket(HandshakePacket());
        _map.Tick(1);
        Assert.Empty(_map.Players); // class 선택 전 = 월드 미진입

        // act: class 선택 전 C_MoveIntent 송신.
        s.OnRecvPacket(MoveIntentPacket());
        _map.Tick(2);

        // assert: [Trust] 경고 로그 박힘 + player 맵에 없음 (intent 처리 X).
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust] C_MoveIntent before CharacterSelect", log);
        Assert.Contains("silent drop", log);

        // player가 맵에 없으므로 intent 처리가 없었음을 간접 확인.
        Assert.Empty(_map.Players);
        Assert.Equal(0, s.DisconnectCalls);
    }

    /// <summary>
    /// 테스트 5: class 선택 전 C_Attack silent drop (P0-2 봉합, MoveIntent 정합 패턴).
    /// handshake 완료 후 class 선택 전 상태에서 C_Attack 수신 시 [Trust] 로그 + drop.
    /// </summary>
    [Fact]
    public void Attack_BeforeCharacterSelect_Dropped()
    {
        // arrange: handshake 완료 + class 선택 전.
        TestGameSession s = CreateConnectedSession();
        s.OnRecvPacket(HandshakePacket());
        _map.Tick(1);
        Assert.Empty(_map.Players);

        // act: class 선택 전 C_Attack 송신.
        s.OnRecvPacket(AttackPacket(targetEntityId: 1));
        _map.Tick(2);

        // assert: [Trust] 경고 로그 박힘.
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust] C_Attack before CharacterSelect", log);
        Assert.Contains("silent drop", log);

        // attack이 처리되지 않았으므로 enemy HP 변동 없음 (Normal enemy=1 기본 HP 확인).
        Assert.True(_map.Enemies.ContainsKey(1)); // enemy 여전히 살아있음
        // Normal enemy 기본 HP: MapSpawnTable 단일 진실 공급원에서 조회.
        int normalMaxHp = MapSpawnTable.GetSpawnsFor(MapId.HuntingGround)[0].MaxHp;
        Assert.Equal(normalMaxHp, _map.Enemies[1].Hp); // HP 변동 없음
        Assert.Equal(0, s.DisconnectCalls);
    }

    /// <summary>
    /// 테스트 6: class 선택 후 두 번째 C_CharacterSelect 차단 (M3.8 Phase 03 정신 회귀 방어).
    /// 첫 선택 완료 후 두 번째 선택 시도 = silent drop + 기존 stats 유지.
    /// idempotent 보장 — 두 번째 EnterGameWorldIfReady()도 _enteredWorld=true라 no-op.
    /// </summary>
    [Fact]
    public void CharacterSelect_DuplicateAfterEnter_Rejected()
    {
        // arrange: 정상 handshake + Warrior 선택 + 월드 진입.
        TestGameSession s = CreateConnectedSession();
        s.OnRecvPacket(HandshakePacket());
        s.OnRecvPacket(CharacterSelectPacket(0)); // Warrior
        _map.Tick(1);
        Assert.Single(_map.Players);
        Assert.True(s.ClassSelected);

        // 첫 선택 로그 제거 (두 번째 시도 로그만 검증).
        _consoleCapture.GetStringBuilder().Clear();
        int playersBefore = _map.Players.Count;

        // act: 두 번째 선택 시도 (Ranger로 교체 의도).
        s.OnRecvPacket(CharacterSelectPacket(1)); // Ranger
        _map.Tick(2);

        // assert: duplicate drop 로그 박힘 + player 수 변동 없음 + 두 번째 EnterGameWorld no-op.
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust] CharacterSelect: already selected", log);
        Assert.Contains("duplicate dropped", log);

        // 두 번째 EnterGameWorldIfReady()가 _enteredWorld=true로 no-op이므로 roster 수 그대로.
        Assert.Equal(playersBefore, _map.Players.Count);
        Assert.Equal(0, s.DisconnectCalls);
    }
}
