using System.Net;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// M3.8 Phase 03 (캡스톤 1 데모 — 캐릭터 선택 핸들러 단위 테스트):
/// `C_CharacterSelect` 수신 → 검증 → `PlayerStats` 박힘 흐름 전체 커버.
///
/// **검증 invariant** (Phase 03 완료 조건 5건 1:1 정합):
///   1. happy_warrior: characterClass=0 → Warrior stats 박힘 (`Class==Warrior`, `Hp==150`)
///   2. happy_ranger: characterClass=1 → Ranger stats 박힘 (`Class==Ranger`, `Hp==80`)
///   3. invalid_2: characterClass=2 → silent drop + `_stats` null 유지
///   4. invalid_255: characterClass=255 → silent drop + `_stats` null 유지
///   5. duplicate: 이미 선택 후 재전송 → silent drop + 옛 stats 유지
///
/// **테스트 전략** (AttackHandlerTests / MoveIntentHandlerTests 패턴 정합):
///   - GameMap 직접 주입(GetMap override) → GameWorld.Instance singleton race 차단
///   - Send/Disconnect override로 I/O 차단 (socket 없이 동작)
///   - `BypassHandshake()`로 handshake 우회 (lifecycle 테스트가 아니라 handler 로직 테스트)
///   - `HasSelectedClass` / `GetStatsForTest` internal getter로 상태 검증
///
/// **헌법 #3 정합**: 테스트 3~4번이 invalid 입력 silent drop + cheat-flag 로그 검증.
/// **헌법 #1 정합**: 테스트 1~2번이 서버가 stats를 박는 흐름 검증 (클라 수치 직접 박기 X).
/// </summary>
[Collection("ConsoleSerial")]
public class CharacterSelectHandlerTests : IDisposable
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

        // CompleteHandshakeAndEnter는 protected internal — handshake 우회용.
        public void BypassHandshake() => CompleteHandshakeAndEnter();

        // HasSelectedClass는 GameSession.HasSelectedClass (internal getter) 직접 접근.
        // 같은 어셈블리 내 서브클래스이므로 가능. GameSession._stats null 여부만 표면화.
        public bool StatsSet => HasSelectedClass;
    }

    public CharacterSelectHandlerTests()
    {
        _map = new GameMap();
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    // --- 헬퍼 ---

    static ArraySegment<byte> CharacterSelectPacket(byte characterClass)
    {
        C_CharacterSelect pkt = new C_CharacterSelect { characterClass = characterClass };
        return pkt.Write();
    }

    // handshake 우회 + tick 1회 → EnterGameWorld 람다 처리 완료 상태.
    TestGameSession SetupHandshakedSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.BypassHandshake();
        _map.Tick(1);
        return s;
    }

    // --- 5건 회귀 ---

    [Fact]
    public void Happy_Warrior_StatsSet_WarriorClass()
    {
        // arrange: handshake 통과 상태.
        TestGameSession s = SetupHandshakedSession();
        Assert.False(s.StatsSet); // 선택 전 = null

        // act: characterClass=0 (Warrior) 전송.
        s.OnRecvPacket(CharacterSelectPacket(0));

        // assert: HasSelectedClass = true + 로그에 Warrior 박힘.
        Assert.True(s.StatsSet);
        string log = _consoleCapture.ToString();
        Assert.Contains("Warrior", log);
        // Warrior MaxHp=150 로그 검증 (SetCharacterClass 로그 정합).
        Assert.Contains("Hp:150", log);
    }

    [Fact]
    public void Happy_Ranger_StatsSet_RangerClass()
    {
        // arrange
        TestGameSession s = SetupHandshakedSession();
        Assert.False(s.StatsSet);

        // act: characterClass=1 (Ranger) 전송.
        s.OnRecvPacket(CharacterSelectPacket(1));

        // assert: HasSelectedClass = true + 로그에 Ranger 박힘.
        Assert.True(s.StatsSet);
        string log = _consoleCapture.ToString();
        Assert.Contains("Ranger", log);
        // Ranger MaxHp=80 로그 검증.
        Assert.Contains("Hp:80", log);
    }

    [Fact]
    public void Invalid_CharacterClass2_SilentDrop_StatsNull()
    {
        // arrange: characterClass=2 는 현재 enum에 없음 — 범위 초과.
        TestGameSession s = SetupHandshakedSession();

        // act
        s.OnRecvPacket(CharacterSelectPacket(2));

        // assert: silent drop → stats 여전히 null, [Trust] 로그 박힘.
        Assert.False(s.StatsSet);
        Assert.Equal(0, s.DisconnectCalls); // silent drop이지 disconnect 아님
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust] CharacterSelect: invalid characterClass=0x02", log);
        Assert.Contains("cheat-flag", log);
    }

    [Fact]
    public void Invalid_CharacterClass255_SilentDrop_StatsNull()
    {
        // arrange: byte 최대값 = 255 = 명백한 잘못된 입력.
        TestGameSession s = SetupHandshakedSession();

        // act
        s.OnRecvPacket(CharacterSelectPacket(255));

        // assert
        Assert.False(s.StatsSet);
        Assert.Equal(0, s.DisconnectCalls);
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust] CharacterSelect: invalid characterClass=0xFF", log);
    }

    [Fact]
    public void Duplicate_Select_SilentDrop_OldStatsPreserved()
    {
        // arrange: Warrior로 첫 선택 완료.
        TestGameSession s = SetupHandshakedSession();
        s.OnRecvPacket(CharacterSelectPacket(0)); // Warrior 선택
        Assert.True(s.StatsSet);
        _consoleCapture.GetStringBuilder().Clear(); // 로그 초기화 (첫 선택 로그 제거)

        // act: 두 번째 선택 시도 (Ranger로 바꾸려는 시도 = 중복).
        s.OnRecvPacket(CharacterSelectPacket(1));

        // assert: silent drop + [Trust] 로그 박힘 + Warrior stats 유지 (Ranger로 교체 X).
        // HasSelectedClass는 여전히 true (null → non-null 전환 X).
        Assert.True(s.StatsSet);
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust] CharacterSelect: already selected", log);
        Assert.Contains("duplicate dropped", log);
        // Ranger 선택 로그가 없어야 — 두 번째 SetCharacterClass 호출 X.
        Assert.DoesNotContain("Hp:80", log);
    }
}
