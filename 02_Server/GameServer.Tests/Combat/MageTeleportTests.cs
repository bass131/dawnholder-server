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

    static ArraySegment<byte> TeleportPacketBytes(int attackerClientTick = 1, byte verticalDir = 0)
    {
        C_SkillUse pkt = new C_SkillUse
        {
            skillId = (byte)SkillId.Teleport,
            attackerClientTick = attackerClientTick,
            verticalDir = verticalDir,
        };
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

    // 지형 인식 수직 텔레포트 검증용 — 지정 Y에 TerrainPlatform 삽입.
    // platformY: 발판 표면 Y (= destY 기대값). 플레이어 X가 발판 X 범위에 포함되도록 넓게 생성.
    // floors: 별도 solid floor 없음(플레이어는 공중 배치 — 물리 낙하 간섭 배제).
    static MapTerrain MakeTerrainWithPlatforms(params float[] platformYValues)
    {
        TerrainPlatform[] platforms = new TerrainPlatform[platformYValues.Length];
        for (int i = 0; i < platformYValues.Length; i++)
            platforms[i] = new TerrainPlatform(platformYValues[i], minX: -200f, maxX: 200f);
        return new MapTerrain(Array.Empty<TerrainAabb>(), platforms);
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
        float startX = mapMax - CombatConstants.TeleportDistance * 0.5f; // 텔레포트 시 절반 거리만큼 벽 초과 의도 → clamp 검증 (거리 상대값)

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
        float startX = mapMin + CombatConstants.TeleportDistance * 0.5f; // 텔레포트 시 절반 거리만큼 벽 초과 의도 → clamp 검증 (거리 상대값)

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

    // ── 지형 인식 수직 텔레포트 (M4.15 P09) ──────────────────────────────────────

    // 발판 표면 Y 픽스처 상수 — 기대 destY 정확 검증용 단일 진실.
    // 플레이어 시작 Y=0 기준, TeleportVerticalRange(3.0) 이내로 배치(경계 fragile 회피 — 2.5).
    const float UpperPlatformY = 2.5f;  // 위 발판 (거리 2.5 ≤ 사거리 3.0)
    const float LowerPlatformY = -2.5f; // 아래 발판 (거리 2.5 ≤ 사거리 3.0)

    [Fact]
    public void Teleport_Vertical_Up_SnapsToUpperPlatform_XUnchanged()
    {
        // verticalDir=1(위): 사거리 안 위 발판 → 발판 표면 Y로 정확 snap.
        //   발판 위 배치라 중력 중립(OnGround=true, 낙하 없음) — Execute 직후 Position이 정확값.
        //   Execute → RecordPosition → BroadcastToAll 순서 이후 map.Tick이 물리를 1회 돌리지만
        //   발판(Platform.Y=5f) 위에 있으면 중력이 표면에 재snap → 정확값 유지.
        MapTerrain terrain = MakeTerrainWithPlatforms(UpperPlatformY);
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1, terrain: terrain);
        caster.Position = new Vector2(0f, 0f);
        caster.RecordPosition(1, caster.Position);
        session.SentPackets.Clear();
        float startX = caster.Position.X;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 1));
        map.Tick(2);

        Assert.Equal(UpperPlatformY, caster.Position.Y, precision: 3);
        Assert.Equal(startX, caster.Position.X, precision: 3);
    }

    [Fact]
    public void Teleport_Vertical_Down_SnapsToLowerPlatform_XUnchanged()
    {
        // verticalDir=2(아래): 사거리 안 아래 발판 → 발판 표면 Y로 정확 snap. X 불변.
        MapTerrain terrain = MakeTerrainWithPlatforms(LowerPlatformY);
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1, terrain: terrain);
        caster.Position = new Vector2(0f, 0f);
        caster.RecordPosition(1, caster.Position);
        session.SentPackets.Clear();
        float startX = caster.Position.X;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 2));
        map.Tick(2);

        Assert.Equal(LowerPlatformY, caster.Position.Y, precision: 3);
        Assert.Equal(startX, caster.Position.X, precision: 3);
    }

    [Fact]
    public void Teleport_Vertical_Up_OutOfRange_PositionUnchanged()
    {
        // 위 발판이 사거리 밖(TeleportVerticalRange 초과) → 이동 없음. (상수 상대값이라 튜닝에 자동 정합.)
        float farPlatformY = CombatConstants.TeleportVerticalRange + 1f; // 사거리 밖
        MapTerrain terrain = MakeTerrainWithPlatforms(farPlatformY);
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1, terrain: terrain);
        caster.Position = new Vector2(0f, 0f);
        caster.RecordPosition(1, caster.Position);
        session.SentPackets.Clear();
        float startX = caster.Position.X;
        float startY = caster.Position.Y;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 1));
        map.Tick(2);

        // 이동 없음: X 정확 불변, Y는 중력 낙하로 미세 감소할 수 있으나 startY 기준 ±0.2 이내.
        Assert.Equal(startX, caster.Position.X, precision: 3);
        Assert.True(MathF.Abs(caster.Position.Y - startY) < 0.2f,
            $"이동 없어야 함(사거리 밖): startY={startY} Position.Y={caster.Position.Y}");
    }

    [Fact]
    public void Teleport_Vertical_NoPlatform_PositionUnchanged()
    {
        // 발판 없는 terrain(빈 MapTerrain) → 이동 없음. 이펙트(S_SkillCast) 1회 보장.
        MapTerrain emptyTerrain = new MapTerrain(Array.Empty<TerrainAabb>(), Array.Empty<TerrainPlatform>());
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1, terrain: emptyTerrain);
        caster.Position = new Vector2(0f, 0f);
        caster.RecordPosition(1, caster.Position);
        session.SentPackets.Clear();
        float startX = caster.Position.X;
        float startY = caster.Position.Y;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 1));
        map.Tick(2);

        // 이동 없음
        Assert.Equal(startX, caster.Position.X, precision: 3);
        Assert.True(MathF.Abs(caster.Position.Y - startY) < 0.2f,
            $"이동 없어야 함(발판 없음): startY={startY} Position.Y={caster.Position.Y}");
        // 이펙트 신호(S_SkillCast) 무조건 1회 — early-return 회귀 차단.
        Assert.Equal(1, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
    }

    [Fact]
    public void Teleport_Vertical_NoPlatform_BroadcastsSkillCastAndPositionUnchanged()
    {
        // 아래 이동 불가(발판 없음) → Position 불변 + S_SkillCast 1회 동시 Assert.
        // early-return이 있으면 broadcast가 빠짐 — 회귀 방지 전용 케이스.
        MapTerrain emptyTerrain = new MapTerrain(Array.Empty<TerrainAabb>(), Array.Empty<TerrainPlatform>());
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1, terrain: emptyTerrain);
        caster.Position = new Vector2(0f, 0f);
        caster.RecordPosition(1, caster.Position);
        session.SentPackets.Clear();
        Vector2 startPos = caster.Position;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 2));
        map.Tick(2);

        // Position 불변 (X 정확, Y 중력 허용 ±0.2)
        Assert.Equal(startPos.X, caster.Position.X, precision: 3);
        Assert.True(MathF.Abs(caster.Position.Y - startPos.Y) < 0.2f,
            $"아래 이동 불가 시 Y 변화 없어야 함: startY={startPos.Y} pos={caster.Position.Y}");
        // S_SkillCast 1회 무조건 broadcast (이펙트 신호)
        Assert.Equal(1, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
    }

    [Fact]
    public void Teleport_Horizontal_VerticalDirZero_MovesXOnly()
    {
        // verticalDir=0(수평): 기존 거동 — X축 FacingDir 방향, Y 유지.
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1);
        float startY = caster.Position.Y;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 0));
        map.Tick(2);

        Assert.Equal(CombatConstants.TeleportDistance, caster.Position.X, precision: 3); // 0 + 거리
        Assert.Equal(startY, caster.Position.Y, precision: 3); // 수평 이동은 Y 불변
    }

    [Fact]
    public void Teleport_Distance_IsThreePointFive()
    {
        // 수평 거리 축소(15→5→3.5, 영호 Play 튜닝) 반영 회귀.
        Assert.Equal(3.5f, CombatConstants.TeleportDistance, precision: 3);

        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1);
        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: 0));
        map.Tick(2);
        Assert.Equal(3.5f, caster.Position.X, precision: 3);
    }

    // ── whitelist 경계 (헌법 §3) ────────────────────────────────────────────────

    [Theory]
    [InlineData((byte)3)]    // 경계값 3 — off-by-one cheat (허용 집합 {0,1,2} 바로 밖). 필수 케이스.
    [InlineData((byte)99)]
    [InlineData((byte)255)]  // byte 최대값
    public void Teleport_VerticalDir_OutOfWhitelist_NormalizesToHorizontal(byte cheatVerticalDir)
    {
        // {0,1,2} 밖의 값은 전부 0(수평)으로 정규화 — SkillUseHandler whitelist 술어 검증.
        //   수평으로 정규화됐다면 X가 FacingDir 방향으로 이동하고 Y는 불변이어야 함.
        var (session, caster, map) = SetupMage(startX: 0f, facingDir: 1);
        float startY = caster.Position.Y;

        session.OnRecvPacket(TeleportPacketBytes(attackerClientTick: 1, verticalDir: cheatVerticalDir));
        map.Tick(2);

        // 수평 거동: X = +거리, Y 불변.
        Assert.Equal(CombatConstants.TeleportDistance, caster.Position.X, precision: 3);
        Assert.Equal(startY, caster.Position.Y, precision: 3);
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
