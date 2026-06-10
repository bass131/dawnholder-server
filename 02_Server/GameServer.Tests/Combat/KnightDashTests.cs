using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// Knight Dash 스킬 단위 테스트 (M4.9 Phase 03).
///
/// 검증 대상:
///   1. Dash_Knight_ExternalVelX_Applied       — Dash 시전 후 AttackLungeVx 부여 확인
///   2. Dash_Knight_PathTarget_DamageHitEffect3 — 경로 적 데미지 + S_HitResult.hitEffect==3
///   3. Dash_Knight_Cooldown_SecondCastDropped  — 쿨다운 미경과 재발동 silent drop
///   4. Dash_Mage_ClassGate_Regression          — Mage가 Dash 시전 시 drop (Phase 02 게이트 정합)
/// </summary>
[Collection("ConsoleSerial")]
public class KnightDashTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    public KnightDashTests()
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

    static ArraySegment<byte> DashPacketBytes(int attackerClientTick = 1)
    {
        C_SkillUse pkt = new C_SkillUse { skillId = (byte)SkillId.Dash, attackerClientTick = attackerClientTick };
        return pkt.Write();
    }

    // Knight 세션 setup: handshake 완료 + Tick(1) → entity 등록. 적 1마리 전방에 배치.
    // 적 위치: caster x=0, FacingDir=+1(오른쪽), boxOrigin.x = DashBoxHalfX(4.0) → 적은 x=4에 배치.
    (TestGameSession session, PlayerEntity caster, EnemyEntity enemy, GameMap map) SetupKnight()
    {
        float enemyX = CombatConstants.DashBoxHalfX; // 박스 center = caster.x + DashBoxHalfX (facing=+1)
        GameMap map = new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, enemyX, 0f),
        }));

        TestGameSession session = new(map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Knight);
        map.Tick(1);

        PlayerEntity caster = map.Players[0];
        caster.Position = new Vector2(0f, 0f);
        caster.FacingDir = 1; // 오른쪽
        caster.RecordPosition(1, caster.Position);

        EnemyEntity enemy = map.Enemies.Values.First();
        session.SentPackets.Clear();
        _consoleCapture.GetStringBuilder().Clear();

        return (session, caster, enemy, map);
    }

    // ── 테스트 4종 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Dash_Knight_ExternalVelX_Applied()
    {
        // Dash 시전 후 AttackLungeVx에 DashLungeInitialVx * FacingDir이 세팅되는지 확인.
        // 동시에 S_SkillCast(skillId=Dash) broadcast도 확인 — 연출 신호 정합.
        var (session, caster, _, map) = SetupKnight();

        session.OnRecvPacket(DashPacketBytes(attackerClientTick: 1));

        // ProcessDash는 EnqueueJob 경유로 Tick 안에서 실행.
        // Tick 전: AttackLungeVx = 0.
        Assert.Equal(0f, caster.AttackLungeVx);

        map.Tick(2);

        // Tick 후: AttackLungeVx = DashLungeInitialVx * FacingDir(+1) = DashLungeInitialVx.
        // 단, AttackState.Tick이 같은 Tick 안에서 1회 감쇠를 적용하므로 엄밀히는 조금 작아짐.
        // 검증: lunge가 0보다 유의미하게 크다 (감쇠 후에도 첫 틱이라 충분히 큼).
        Assert.True(caster.AttackLungeVx > CombatConstants.DashLungeInitialVx * 0.5f,
            $"AttackLungeVx={caster.AttackLungeVx} 가 너무 작음 — Dash lunge 미적용 의심");

        // S_SkillCast(skillId=Dash) 확인
        int castCount = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.True(castCount >= 1, $"S_SkillCast 없음 (got {castCount})");

        byte[] castPkt = session.SentPackets.First(p => PacketIdOf(p) == PacketID.S_SkillCast);
        S_SkillCast parsed = new S_SkillCast();
        parsed.Read(new ArraySegment<byte>(castPkt));
        Assert.Equal(caster.EntityId, parsed.casterEntityId);
        Assert.Equal((byte)SkillId.Dash, parsed.skillId);
        Assert.Equal(0, parsed.strikeDelayTicks); // 즉시 적용
    }

    [Fact]
    public void Dash_Knight_PathTarget_DamageAndHitEffect3()
    {
        // 전방 경로 적에게 데미지 + S_HitResult.hitEffect==3 확인.
        var (session, caster, enemy, map) = SetupKnight();
        int hpBefore = enemy.Hp;

        session.OnRecvPacket(DashPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        // 적 HP 감소
        Assert.True(enemy.Hp < hpBefore, $"경로 적 데미지 미적용 (hp={enemy.Hp}, before={hpBefore})");

        // S_HitResult 확인 + hitEffect==3
        int hitCount = CountPacketsOfType(session.SentPackets, PacketID.S_HitResult);
        Assert.True(hitCount >= 1, "S_HitResult 없음");

        byte[] hitPkt = session.SentPackets.Last(p => PacketIdOf(p) == PacketID.S_HitResult);
        S_HitResult parsedHit = new S_HitResult();
        parsedHit.Read(new ArraySegment<byte>(hitPkt));
        Assert.Equal((byte)3, parsedHit.hitEffect);
        Assert.Equal(caster.EntityId, parsedHit.attackerEntityId);
        Assert.Equal(enemy.EntityId, parsedHit.targetEntityId);
    }

    [Fact]
    public void Dash_Knight_Cooldown_SecondCastDropped()
    {
        // 첫 Dash 성공 → 쿨다운 소비 → 쿨다운 미경과 재발동 silent drop.
        var (session, caster, _, map) = SetupKnight();

        // 첫 발동
        session.OnRecvPacket(DashPacketBytes(attackerClientTick: 1));
        map.Tick(2);
        int castCountFirst = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(1, castCountFirst);

        session.SentPackets.Clear();

        // 쿨다운 미경과(DashCooldownTicks=20) 상태에서 재발동 — tick=3
        session.OnRecvPacket(DashPacketBytes(attackerClientTick: 2));
        map.Tick(3);

        // silent drop → S_SkillCast 없음
        int castCountSecond = CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(0, castCountSecond);

        // 쿨다운 슬롯이 tick=2로 세팅됐는지 확인 (첫 성공 시 소비)
        long dashSlot = caster.GetLastSkillTick((byte)SkillId.Dash);
        Assert.Equal(2L, dashSlot);
    }

    [Fact]
    public void Dash_Mage_ClassGate_Regression()
    {
        // Phase 02 게이트 회귀: Mage가 Dash 시전 시 drop + cheat-flag 로그.
        GameMap map = new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>()));
        TestGameSession session = new(map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Mage);
        map.Tick(1);
        session.SentPackets.Clear();
        _consoleCapture.GetStringBuilder().Clear();

        session.OnRecvPacket(DashPacketBytes(attackerClientTick: 1));
        map.Tick(2);

        // S_SkillCast 없음 + cheat-flag 로그
        Assert.Equal(0, CountPacketsOfType(session.SentPackets, PacketID.S_SkillCast));
        string log = _consoleCapture.ToString();
        Assert.Contains("[Trust]", log);
        Assert.Contains("class mismatch", log);
    }
}
