using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// 클래스↔스킬 게이트 단위 테스트 (M4.9 Phase 02).
///
/// 헌법 §3 (Trust Boundary) 봉합 검증:
///   1. ClassGate_KnightCastsThunderbolt_SilentDrop  — Knight가 Mage 전용 스킬 시전 시 drop + cheat-flag 로그 + 쿨다운 미소비
///   2. ClassGate_MageCastsDash_SilentDrop            — Mage가 Knight 전용 스킬 시전 시 drop + cheat-flag 로그
///   3. ClassGate_MageCastsThunderbolt_Passes         — Mage가 Thunderbolt 시전 시 정상 처리 (회귀)
///   4. ClassGate_UnknownSkillId_SilentDrop           — None(0) skillId drop (카탈로그 미등록)
///   5. ClassGate_Dash_GatePassButUnimplemented_NoCrash — Knight가 Dash 시전 시 게이트 통과 + 미구현 drop (null 참조 X)
/// </summary>
[Collection("ConsoleSerial")]
public class ClassSkillGateTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    public ClassSkillGateTests()
    {
        _originalOut = Console.Out;
        _consoleCapture = new StringWriter();
        Console.SetOut(_consoleCapture);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleCapture.Dispose();
    }

    // ── TestGameSession ────────────────────────────────────────────────────────

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }

        public void BypassHandshake(byte charClass)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(charClass);
            EnterGameWorldIfReady();
        }
    }

    // ── 헬퍼 ───────────────────────────────────────────────────────────────────

    static GameMap MakeEmptyMap() => new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>()));

    static ArraySegment<byte> SkillPacketBytes(byte skillId, int attackerClientTick = 1)
    {
        C_SkillUse pkt = new C_SkillUse { skillId = skillId, attackerClientTick = attackerClientTick };
        return pkt.Write();
    }

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    // 세션 생성 + handshake 완료 + Tick(1) → entity 등록 완료 상태.
    (TestGameSession session, GameMap map) Setup(byte charClass)
    {
        GameMap map = MakeEmptyMap();
        TestGameSession session = new(map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass);
        map.Tick(1);

        if (map.Players.Count > 0)
        {
            PlayerEntity entity = map.Players[0];
            entity.Position = new Vector2(0f, 0f);
            entity.RecordPosition(1, entity.Position);
        }

        session.SentPackets.Clear();
        _consoleCapture.GetStringBuilder().Clear();
        return (session, map);
    }

    // ── 테스트 5종 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ClassGate_KnightCastsThunderbolt_SilentDrop()
    {
        // arrange: Knight 세션
        var (session, map) = Setup(charClass: (byte)CharacterClass.Knight);

        // act: Knight가 Thunderbolt(1) 시전 시도
        session.OnRecvPacket(SkillPacketBytes(skillId: (byte)SkillId.Thunderbolt));
        map.Tick(2);

        // assert: S_SkillCast 없음 + cheat-flag 로그
        Assert.Equal(0, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust]", log);
        Assert.Contains("class mismatch", log);
        Assert.Contains("cheat-flag", log);
    }

    [Fact]
    public void ClassGate_KnightCastsThunderbolt_CooldownNotConsumed()
    {
        // Knight가 Thunderbolt를 drop 당했을 때 쿨다운이 소비되지 않아야 한다.
        // (쿨다운 소비 = ProcessThunderbolt 진입 후 통과 시점에만 발생)
        var (session, map) = Setup(charClass: (byte)CharacterClass.Knight);

        // act: 거부됐어야 할 시전
        session.OnRecvPacket(SkillPacketBytes(skillId: (byte)SkillId.Thunderbolt));
        map.Tick(2);

        // assert: entity의 Thunderbolt 쿨다운 슬롯이 초기값(long.MinValue/2)인지 확인.
        // 핸들러에서 drop됐으므로 ProcessThunderbolt까지 도달 X → 쿨다운 미소비.
        if (map.Players.Count > 0)
        {
            PlayerEntity entity = map.Players[0];
            long cooldownSlot = entity.GetLastSkillTick((byte)SkillId.Thunderbolt);
            Assert.Equal(long.MinValue / 2, cooldownSlot);
        }
    }

    [Fact]
    public void ClassGate_MageCastsDash_SilentDrop()
    {
        // arrange: Mage 세션
        var (session, map) = Setup(charClass: (byte)CharacterClass.Mage);

        // act: Mage가 Dash(2) 시전 시도
        session.OnRecvPacket(SkillPacketBytes(skillId: (byte)SkillId.Dash));
        map.Tick(2);

        // assert: 어떤 스킬 패킷도 나가지 않음 + cheat-flag 로그
        Assert.Equal(0, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust]", log);
        Assert.Contains("class mismatch", log);
    }

    [Fact]
    public void ClassGate_MageCastsThunderbolt_Passes()
    {
        // 기존 동작 회귀: Mage가 Thunderbolt 시전 → S_SkillCast 수신.
        var (session, map) = Setup(charClass: (byte)CharacterClass.Mage);

        // act: Mage → Thunderbolt
        session.OnRecvPacket(SkillPacketBytes(skillId: (byte)SkillId.Thunderbolt, attackerClientTick: 1));
        map.Tick(2);

        // assert: S_SkillCast 1건 이상
        int castCount = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.True(castCount >= 1, $"Mage Thunderbolt: S_SkillCast 없음 (got {castCount})");

        // cheat-flag 로그 없어야 (정상 시전)
        string log = _consoleCapture.ToString();
        Assert.DoesNotContain("class mismatch", log);
    }

    [Fact]
    public void ClassGate_NoneSkillId_SilentDrop()
    {
        // None(0) skillId = 카탈로그 미등록 → drop.
        var (session, map) = Setup(charClass: (byte)CharacterClass.Mage);

        session.OnRecvPacket(SkillPacketBytes(skillId: (byte)SkillId.None));
        map.Tick(2);

        Assert.Equal(0, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
        Assert.Contains("[Trust] C_SkillUse unknown skillId=0", _consoleCapture.ToString());
    }

    [Fact]
    public void ClassGate_KnightCastsDash_GatePassAndImplemented()
    {
        // Phase 03 완료: Knight가 Dash(2) 시전 시 게이트 통과 + 정상 처리 → S_SkillCast 수신.
        // (Phase 02 시점 "미구현 drop" 안전망 → Phase 03 "정상 처리" 회귀로 교체)
        var (session, map) = Setup(charClass: (byte)CharacterClass.Knight);

        Exception? caught = Record.Exception(() =>
        {
            session.OnRecvPacket(SkillPacketBytes(skillId: (byte)SkillId.Dash));
            map.Tick(2);
        });

        Assert.Null(caught); // 예외 없음

        // Dash 구현됨 → S_SkillCast 1건 이상
        Assert.True(CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast) >= 1,
            "Knight Dash: S_SkillCast 없음 — ProcessDash 미진입 의심");

        // cheat-flag 없어야 (클래스 게이트 정상 통과)
        Assert.DoesNotContain("class mismatch", _consoleCapture.ToString());
    }
}
