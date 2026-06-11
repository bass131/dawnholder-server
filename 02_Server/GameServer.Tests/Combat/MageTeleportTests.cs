using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// Mage Teleport 스킬 단위 테스트 (M4.9 Phase 05).
///
/// 검증 대상:
///   1. Teleport_Position_FacingRight_ExpectedDest  — 오른쪽 facing 텔레포트 → Position.X = startX + TeleportDistance
///   2. Teleport_Position_FacingLeft_ExpectedDest   — 왼쪽 facing 텔레포트 → Position.X = startX - TeleportDistance
///   3. Teleport_BoundaryClamp_RightEdge            — 맵 우측 끝에서 시전 → 경계 초과 불가
///   4. Teleport_BoundaryClamp_LeftEdge             — 맵 좌측 끝에서 시전 → 경계 미만 불가
///   5. Teleport_Cooldown_SecondCastDropped         — TeleportCooldownTicks 미경과 재발동 silent drop
///   6. Teleport_Cooldown_Consumed_AfterCast        — 성공 후 쿨다운 슬롯 = currentTick
///   7. Teleport_Knight_ClassGate_SilentDrop        — Knight가 Teleport 시전 시 drop
///   8. Teleport_NoDamage_NoDeferredDamage          — S_HitResult 없음 (순수 이동 스킬)
///   9. Teleport_SkillCast_Broadcast                — S_SkillCast(skillId=Teleport) broadcast 확인
///  10. Dash_Thunderbolt_Regression                 — Dash/Thunderbolt 기존 동작 회귀 0
/// </summary>
[Collection("ConsoleSerial")]
public class MageTeleportTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    public MageTeleportTests()
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

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    static ArraySegment<byte> TeleportPacketBytes(int attackerClientTick = 1)
    {
        C_SkillUse pkt = new C_SkillUse { skillId = (byte)SkillId.Teleport, attackerClientTick = attackerClientTick };
        return pkt.Write();
    }

    static ArraySegment<byte> DashPacketBytes(int attackerClientTick = 1)
    {
        C_SkillUse pkt = new C_SkillUse { skillId = (byte)SkillId.Dash, attackerClientTick = attackerClientTick };
        return pkt.Write();
    }

    static ArraySegment<byte> ThunderboltPacketBytes(int attackerClientTick = 1)
    {
        C_SkillUse pkt = new C_SkillUse { skillId = (byte)SkillId.Thunderbolt, attackerClientTick = attackerClientTick };
        return pkt.Write();
    }

    // Mage 세션 setup. terrain null = 평지(경계 ±∞), 지형 경계 테스트 시 terrain 주입.
    (TestGameSession session, PlayerEntity caster, GameMap map) SetupMage(
        float startX = 0f, sbyte facingDir = 1, MapTerrain? terrain = null)
    {
        GameMap map = new GameMap(MapId.HuntingGround, terrain: terrain,
            content: new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>()));

        TestGameSession session = new(map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Mage);
        map.Tick(1);

        PlayerEntity caster = map.Players[0];
        caster.Position = new Vector2(startX, 0f);
        caster.FacingDir = facingDir;
        caster.RecordPosition(1, caster.Position);

        session.SentPackets.Clear();
        _consoleCapture.GetStringBuilder().Clear();

        return (session, caster, map);
    }

    // 단순 좌측 벽(minX=-100) + 우측 벽(maxX=100) terrain 구성.
    // Teleport 경계 clamp 검증용 — solid 2개로 맵 X 범위 [-100, 100] 정의.
    static MapTerrain MakeBoundedTerrain(float minX = -100f, float maxX = 100f)
    {
        TerrainAabb left  = new TerrainAabb(minX - 1f, -1f, minX, 10f);
        TerrainAabb right = new TerrainAabb(maxX,      -1f, maxX + 1f, 10f);
        TerrainAabb floor = new TerrainAabb(minX, -1f, maxX, 0f);
        return new MapTerrain(new[] { left, right, floor }, Array.Empty<TerrainPlatform>());
    }

    // ── 위치 검증 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Teleport_Position_FacingRight_ExpectedDest()
    {
        float startX = 0f;
        var (session, caster, map) = SetupMage(startX, facingDir: 1);

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        float expected = startX + CombatConstants.TeleportDistance;
        Assert.Equal(expected, caster.Position.X, precision: 3);
    }

    [Fact]
    public void Teleport_Position_FacingLeft_ExpectedDest()
    {
        float startX = 50f;
        var (session, caster, map) = SetupMage(startX, facingDir: -1);

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        float expected = startX - CombatConstants.TeleportDistance;
        Assert.Equal(expected, caster.Position.X, precision: 3);
    }

    // ── 경계 clamp ─────────────────────────────────────────────────────────────

    [Fact]
    public void Teleport_BoundaryClamp_RightEdge()
    {
        // 맵 우측 끝(maxX=100) 근처에서 오른쪽으로 텔레포트 → 100을 초과해서는 안 됨.
        float mapMax = 100f;
        MapTerrain terrain = MakeBoundedTerrain(minX: -100f, maxX: mapMax);
        float startX = mapMax - 5f; // 텔레포트 하면 100+10 초과 의도

        var (session, caster, map) = SetupMage(startX, facingDir: 1, terrain: terrain);

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        Assert.True(caster.Position.X <= mapMax + 1f, // solid MaxX까지(벽 포함) 허용
            $"우측 경계 초과: Position.X={caster.Position.X} > mapMax={mapMax}");
    }

    [Fact]
    public void Teleport_BoundaryClamp_LeftEdge()
    {
        // 맵 좌측 끝(minX=-100) 근처에서 왼쪽으로 텔레포트 → -100 미만 불가.
        float mapMin = -100f;
        MapTerrain terrain = MakeBoundedTerrain(minX: mapMin, maxX: 100f);
        float startX = mapMin + 5f; // 텔레포트 하면 -100-10 미만 의도

        var (session, caster, map) = SetupMage(startX, facingDir: -1, terrain: terrain);

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        Assert.True(caster.Position.X >= mapMin - 1f,
            $"좌측 경계 미만: Position.X={caster.Position.X} < mapMin={mapMin}");
    }

    // ── 쿨다운 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Teleport_Cooldown_Consumed_AfterCast()
    {
        var (session, caster, map) = SetupMage();

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        long slot = caster.GetLastSkillTick((byte)SkillId.Teleport);
        Assert.Equal(2L, slot);
    }

    [Fact]
    public void Teleport_Cooldown_SecondCastDropped()
    {
        var (session, caster, map) = SetupMage();

        // 첫 발동
        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);
        int castFirst = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(1, castFirst);
        float posAfterFirst = caster.Position.X;

        session.SentPackets.Clear();

        // 쿨다운 미경과(TeleportCooldownTicks=30) 재발동 — tick=3
        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 2));
        map.Tick(3);

        // silent drop → S_SkillCast 없음 + 위치 불변
        int castSecond = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(0, castSecond);
        Assert.Equal(posAfterFirst, caster.Position.X, precision: 3);
    }

    // ── 클래스 게이트 ──────────────────────────────────────────────────────────

    [Fact]
    public void Teleport_Knight_ClassGate_SilentDrop()
    {
        GameMap map = new GameMap(MapId.HuntingGround,
            content: new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>()));
        TestGameSession session = new(map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Knight);
        map.Tick(1);
        session.SentPackets.Clear();
        _consoleCapture.GetStringBuilder().Clear();

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        Assert.Equal(0, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust]", log);
        Assert.Contains("class mismatch", log);
    }

    // ── 순수 이동 — 데미지 없음 ────────────────────────────────────────────────

    [Fact]
    public void Teleport_NoDamage_NoDeferredDamage()
    {
        // 적이 있어도 텔레포트 경로에 S_HitResult 없어야 함 (순수 이동 스킬).
        GameMap map = new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, CombatConstants.TeleportDistance * 0.5f, 0f),
        }));

        TestGameSession session = new(map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Mage);
        map.Tick(1);

        PlayerEntity caster = map.Players[0];
        caster.Position = new Vector2(0f, 0f);
        caster.FacingDir = 1;
        caster.RecordPosition(1, caster.Position);
        session.SentPackets.Clear();

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        // 여러 틱 지나도 S_HitResult 없음
        for (long t = 3; t <= 10; t++)
            map.Tick(t);

        Assert.Equal(0, CountPacketsOfType(session.SentPackets, PacketID.S_HitResult));
    }

    // ── S_SkillCast broadcast ──────────────────────────────────────────────────

    [Fact]
    public void Teleport_SkillCast_Broadcast_SkillIdAndFacing()
    {
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1);

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        int castCount = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.True(castCount >= 1, $"S_SkillCast 없음 (got {castCount})");

        byte[] castPkt = session.SentPackets.First(p => PacketIdOf(p) == PacketID.S_SkillCast);
        S_SkillCast parsed = new S_SkillCast();
        parsed.Read(new ArraySegment<byte>(castPkt));

        Assert.Equal(caster.EntityId, parsed.casterEntityId);
        Assert.Equal((byte)SkillId.Teleport, parsed.skillId);
        Assert.Equal(0, parsed.strikeDelayTicks); // 즉시 이동
        Assert.Equal((byte)1, parsed.facing);     // facingDir=+1 → 1(오른쪽)
    }

    // ── 회귀 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Dash_Thunderbolt_Regression_NotAffected()
    {
        // Teleport 추가 후 Dash(Knight) / Thunderbolt(Mage) 기존 동작 이상 없음.

        // Dash: Knight 세션 → S_SkillCast(Dash) 수신
        GameMap dashMap = new GameMap(MapId.HuntingGround,
            content: new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>()));
        TestGameSession dashSession = new(dashMap);
        dashSession.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        dashSession.BypassHandshake(charClass: (byte)CharacterClass.Knight);
        dashMap.Tick(1);
        PlayerEntity dashCaster = dashMap.Players[0];
        dashCaster.Position = new Vector2(0f, 0f);
        dashCaster.RecordPosition(1, dashCaster.Position);
        dashSession.SentPackets.Clear();

        dashSession.OnRecvPacket(DashPacketBytes(attackerClientTick: 1));
        dashMap.Tick(2);
        Assert.True(CountPacketsOfType(dashSession.SentPackets, PacketID.S_SkillCast) >= 1,
            "Dash 회귀: S_SkillCast 없음");
        byte[] dashPkt = dashSession.SentPackets.First(p => PacketIdOf(p) == PacketID.S_SkillCast);
        S_SkillCast dashParsed = new S_SkillCast();
        dashParsed.Read(new ArraySegment<byte>(dashPkt));
        Assert.Equal((byte)SkillId.Dash, dashParsed.skillId);

        // Thunderbolt: Mage 세션 → S_SkillCast(Thunderbolt) 수신
        GameMap tbMap = new GameMap(MapId.HuntingGround,
            content: new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>()));
        TestGameSession tbSession = new(tbMap);
        tbSession.OnConnected(new IPEndPoint(IPAddress.Loopback, 1));
        tbSession.BypassHandshake(charClass: (byte)CharacterClass.Mage);
        tbMap.Tick(1);
        PlayerEntity tbCaster = tbMap.Players[0];
        tbCaster.Position = new Vector2(0f, 0f);
        tbCaster.RecordPosition(1, tbCaster.Position);
        tbSession.SentPackets.Clear();

        tbSession.OnRecvPacket(ThunderboltPacketBytes(attackerClientTick: 1));
        tbMap.Tick(2);
        Assert.True(CountPacketsOfType(tbSession.SentPackets, PacketID.S_SkillCast) >= 1,
            "Thunderbolt 회귀: S_SkillCast 없음");
        byte[] tbPkt = tbSession.SentPackets.First(p => PacketIdOf(p) == PacketID.S_SkillCast);
        S_SkillCast tbParsed = new S_SkillCast();
        tbParsed.Read(new ArraySegment<byte>(tbPkt));
        Assert.Equal((byte)SkillId.Thunderbolt, tbParsed.skillId);
    }
}
