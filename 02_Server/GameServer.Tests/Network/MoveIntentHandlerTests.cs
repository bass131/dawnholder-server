using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// M3 Phase 03 (헌법 #4 "Shared Code Discipline" 가짜 약속 2번째 봉합): MoveIntentHandler 단위 회귀 안전망.
///
/// **검증 invariant** (Phase 03 완료 조건 — 모든 기존 핸들러 invalid+auth 페어):
///   - happy: 정상 InputBits → tick 후 entity 이동 + LastClientTick 기록
///   - invalid: 11 reserved 비트 → [Cheat] 로그 + inputX 정규화(0) + Position 변경 X
///
/// **Auth(handshake 미완료)는 HandshakeHandlerTests.NonHandshakeFirstPacket_Rejected_NoEntry가
///   C_MoveIntent 케이스로 이미 박혀있음** — 본 파일에서 중복 X.
/// **Rate-limit drop은 GameSessionRateLimitTests가 커버**.
///
/// **테스트 전략**: dispatcher 통과 → SubmitMoveIntent 호출까지 통합 검증.
/// 핸들러 instance 격리 호출은 internal 접근 필요 → 본 마감 후 InternalsVisibleTo 박을 때 분리.
/// </summary>
[Collection("ConsoleSerial")]
public class MoveIntentHandlerTests : IDisposable
{
    readonly GameMap _map;
    readonly TestGameSession _session;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        // M4.1 Phase 02: handshake + class 선택 양쪽 우회 (월드 진입까지 mock).
        // 본 테스트는 MoveIntentHandler 로직 검증 목적 — state machine 순서는 테스트 대상 X.
        public override void OnConnected(EndPoint endPoint)
        {
            CompleteHandshakeAndEnter();   // _handshakeCompleted = true
            SetCharacterClass(0);           // HasSelectedClass = true (Warrior)
            EnterGameWorldIfReady();        // → EnterGameWorld() 호출
        }
        public override void Send(ArraySegment<byte> _) { /* socket I/O 차단 */ }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }

    public MoveIntentHandlerTests()
    {
        _map = new GameMap();
        _session = new TestGameSession(_map);
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);

        _session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        _map.Tick(1); // AddPlayer 적용 → _entityId 설정
    }

    public void Dispose() => Console.SetOut(_originalOut);

    [Fact]
    public void Happy_ValidInputBits_AppliesIntent()
    {
        // bit 0~1 = 10 (+1) + bit 2 = 1 (jump) → Encode가 박음
        byte input = InputBits.Encode(1, true);
        C_MoveIntent pkt = new C_MoveIntent { input = input, clientTick = 42 };
        _session.OnRecvPacket(pkt.Write());

        _map.Tick(2); // tick에서 PendingInputX/Jump 적용

        // Phase 06 enemy(1) + Phase 07 Boss(2) ctor spawn에 따른 player id offset 갱신 — player=entityId 3.
        PlayerEntity? entity = _map.GetPlayer(3);
        Assert.NotNull(entity);
        Assert.Equal((uint)42, entity!.LastClientTick);
        Assert.True(entity.Position.X > 0f, $"+1 input 적용 안 됨 — Position.X={entity.Position.X}");
        Assert.DoesNotContain("[Cheat]", _consoleCapture.ToString());
    }

    [Fact]
    public void Invalid_ReservedInputBits_NormalizesAndLogsCheat()
    {
        // bit 0~1 = 11 (invalid reserved) → 0b00000011 = 0x03.
        // InputBits.Encode는 sbyte ∈ {-1,0,1}만 받아 throw — 직접 raw byte 박음 (cheat 클라 모사).
        byte invalidInput = 0b00000011;
        C_MoveIntent pkt = new C_MoveIntent { input = invalidInput, clientTick = 1 };
        _session.OnRecvPacket(pkt.Write());

        _map.Tick(2);

        // [Cheat] 로그 박힘 + rawInput 0x03 명시.
        string log = _consoleCapture.ToString();
        Assert.Contains("[Cheat]", log);
        Assert.Contains("invalid input bits 0x03", log);

        // inputX 정규화 → Position 변경 X (안전 default).
        // Phase 06 enemy(1) + Phase 07 Boss(2) ctor spawn에 따른 player id offset 갱신 — player=entityId 3.
        PlayerEntity? entity = _map.GetPlayer(3);
        Assert.NotNull(entity);
        Assert.Equal(0f, entity!.Position.X);
    }
}
