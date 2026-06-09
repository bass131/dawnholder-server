using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// Mage 평타 원거리 단위 테스트 4건 (M4.8 Phase 03).
///
/// 검증 대상:
///   1. Mage_Hit_InRange — 사거리 내 명중: S_ProjectileLaunch 1회 + deferred enqueue + freeze 세팅, 즉시 S_HitResult 없음
///   2. Mage_Hit_ImpactTick_DamageApplied — travelTicks 후 DeferredDamageSystem이 HP 적용 + S_HitResult(hitEffect=1)
///   3. Mage_Miss_OutOfRange — 사거리 밖: S_PlayerAttack만, S_ProjectileLaunch 없음, 데미지 0
///   4. Knight_ImmediateHit_HitEffect0 — Knight 즉시 데미지(hitEffect=0), 음수 currentHp 계약 유지 + rate-limit 차단 유지
///
/// 테스트 전략:
///   - attacker(Mage) + observer 2세션을 같은 맵에 등록. observer가 broadcast 수신.
///   - TestGameSession.BypassHandshake(charClass) 으로 class 설정.
///   - S_ProjectileLaunch / S_HitResult는 PacketID 헤더(offset 2~3)로 식별.
///   - travelTicks = clamp(round(|dx| / ProjectileSpeedPerTick), Min, Max) 직접 산출 후 비교.
/// </summary>
[Collection("ConsoleSerial")]
public class MageRangedCombatTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // GameMap ctor가 id=1(Normal), id=2(Boss) 발급 → player id=3(attacker), id=4(observer).
    const int NormalEnemyId     = 1;
    const int AttackerEntityId  = 3;
    const int ObserverEntityId  = 4;

    const float NormalX    = 10f;
    const float NormalY    = 0f;
    const int   NormalMaxHp = 30;
    const float BossX      = 30f;
    const float BossY      = 0f;

    public MageRangedCombatTests()
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

    static GameMap MakeMap() => new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
    {
        new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
        new EnemySpawnPoint((byte)EnemyKind.Boss,   BossX,   BossY),
    }));

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    /// <summary>
    /// attacker(Mage, charClass=1) + observer(Knight, charClass=0)를 등록하고 Tick(1) 완료.
    /// </summary>
    (TestGameSession attacker, TestGameSession observer, GameMap map) SetupMageSession()
    {
        GameMap map = MakeMap();

        TestGameSession attacker = new(map);
        attacker.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        attacker.BypassHandshake(charClass: 1); // Mage

        TestGameSession observer = new(map);
        observer.OnConnected(new IPEndPoint(IPAddress.Loopback, 1));
        observer.BypassHandshake(charClass: 0); // Knight(observer)

        map.Tick(1); // AddPlayer 람다 처리 → entity 등록 + RecordPosition
        return (attacker, observer, map);
    }

    static ArraySegment<byte> AttackPacketBytes(int targetEntityId, long attackerClientTick)
    {
        C_Attack pkt = new C_Attack
        {
            targetEntityId     = targetEntityId,
            attackerClientTick = (int)attackerClientTick,
        };
        return pkt.Write();
    }

    /// <summary>
    /// travelTicks = clamp(round(|dx| / speed), min, max) 직접 산출 (테스트 기준값).
    /// </summary>
    static int ExpectedTravelTicks(float attackerX, float enemyX)
    {
        float dx = Math.Abs(enemyX - attackerX);
        int raw = (int)Math.Round(dx / CombatConstants.ProjectileSpeedPerTick);
        return Math.Clamp(raw, CombatConstants.MinTravelTicks, CombatConstants.MaxTravelTicks);
    }

    // ── 테스트 1: Mage 사거리 내 명중 → S_ProjectileLaunch + deferred + freeze, 즉시 데미지 없음 ──

    [Fact]
    public void Mage_Hit_InRange_ProjectileLaunchAndDeferredAndFreeze_NoImmediateDamage()
    {
        var (attacker, observer, map) = SetupMageSession();
        PlayerEntity? attackerEntity = map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        // Mage(MageAttackHalfExtent=4.0f) + enemy=(10,0) → 사거리 내:
        // attacker를 (7,0)으로 배치 → attackBox x[3,11] ∩ enemy x[9.5,10.5] = hit.
        attackerEntity!.Position = new Vector2(7f, NormalY);

        EnemyEntity enemy = map.Enemies[NormalEnemyId];
        int hpBefore = enemy.Hp;

        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();

        attacker.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        map.Tick(2);

        // S_ProjectileLaunch: 전원(attacker 포함)에게 broadcast
        Assert.Equal(1, CountPacketsOfType(attacker.SentPackets, PacketID.S_ProjectileLaunch));
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_ProjectileLaunch));

        // S_ProjectileLaunch 내용 검증
        byte[] launchPkt = attacker.SentPackets.First(p => PacketIdOf(p) == PacketID.S_ProjectileLaunch);
        S_ProjectileLaunch parsed = new S_ProjectileLaunch();
        parsed.Read(new ArraySegment<byte>(launchPkt));
        Assert.Equal(AttackerEntityId, parsed.attackerEntityId);
        Assert.Equal(NormalEnemyId, parsed.targetEntityId);
        Assert.Equal((byte)0, parsed.projectileType); // 0=Mage 평타
        int expectedTravelTicks = ExpectedTravelTicks(7f, NormalX);
        Assert.Equal(expectedTravelTicks, parsed.travelTicks);

        // 즉시 S_HitResult 없음 — DeferredDamageSystem이 도착 시 발송
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_HitResult));
        Assert.Equal(0, CountPacketsOfType(observer.SentPackets, PacketID.S_HitResult));

        // 즉시 HP 변화 없음
        Assert.Equal(hpBefore, enemy.Hp);

        // freeze 세팅 확인 (FrozenUntilTick > 0)
        Assert.True(enemy.FrozenUntilTick > 0, "투사체 발사 후 freeze가 세팅돼야 함");
        Assert.Equal(2L + expectedTravelTicks + CombatConstants.StunTicks, enemy.FrozenUntilTick);
    }

    // ── 테스트 2: travelTicks 후 DeferredDamageSystem → HP 적용 + S_HitResult(hitEffect=1) ──

    [Fact]
    public void Mage_Hit_AtImpactTick_DamageApplied_HitEffect1()
    {
        var (attacker, observer, map) = SetupMageSession();
        PlayerEntity? attackerEntity = map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        attackerEntity!.Position = new Vector2(7f, NormalY);

        EnemyEntity enemy = map.Enemies[NormalEnemyId];
        int hpBefore = enemy.Hp;
        int expectedTravelTicks = ExpectedTravelTicks(7f, NormalX);

        attacker.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        map.Tick(2); // 발사 틱

        // travelTicks-1 틱 동안 데미지 없음
        for (long t = 3; t < 2 + expectedTravelTicks; t++)
            map.Tick(t);
        Assert.Equal(hpBefore, enemy.Hp);

        // 도착 틱: 데미지 적용
        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();
        map.Tick(2 + expectedTravelTicks);

        // HP 감소
        int expectedDamage = Formulas.ComputeDamage(
            PlayerStats.Mage(), EnemyStats.NormalDefault(), baseDamage: 10);
        Assert.Equal(hpBefore - expectedDamage, enemy.Hp);

        // S_HitResult(hitEffect=1) broadcast
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_HitResult));
        byte[] hitPkt = observer.SentPackets.First(p => PacketIdOf(p) == PacketID.S_HitResult);
        S_HitResult parsedHit = new S_HitResult();
        parsedHit.Read(new ArraySegment<byte>(hitPkt));
        Assert.Equal((byte)1, parsedHit.hitEffect); // 1 = 투사체 도착
        Assert.Equal(AttackerEntityId, parsedHit.attackerEntityId);
        Assert.Equal(NormalEnemyId, parsedHit.targetEntityId);
    }

    // ── 테스트 3: 사거리 밖 Mage → S_PlayerAttack만, 투사체 없음, 데미지 0 ──────────────

    [Fact]
    public void Mage_Miss_OutOfRange_OnlySwing_NoProjectile_NoDamage()
    {
        var (attacker, observer, map) = SetupMageSession();
        PlayerEntity? attackerEntity = map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        // (0,0)에서 enemy=(10,0) → |dx|=10 > MageAttackHalfExtent(4)+halfEnemy(0.5) → miss
        attackerEntity!.Position = new Vector2(0f, NormalY);

        EnemyEntity enemy = map.Enemies[NormalEnemyId];
        int hpBefore = enemy.Hp;

        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();

        attacker.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        map.Tick(2);

        // S_PlayerAttack: observer에게 1회(스윙 연출 유지)
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_PlayerAttack));

        // S_ProjectileLaunch 없음
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_ProjectileLaunch));
        Assert.Equal(0, CountPacketsOfType(observer.SentPackets, PacketID.S_ProjectileLaunch));

        // S_HitResult 없음 (즉시 + 도착 모두)
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_HitResult));

        // HP 불변
        Assert.Equal(hpBefore, enemy.Hp);

        // freeze 없음
        Assert.Equal(0L, enemy.FrozenUntilTick);
    }

    // ── 테스트 4: Knight 즉시 데미지(hitEffect=0) + rate-limit 차단 유지 ──────────────────

    [Fact]
    public void Knight_ImmediateHit_HitEffect0_NegativeHpAllowed_RateLimit()
    {
        GameMap map = MakeMap();

        TestGameSession knightSession = new(map);
        knightSession.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        knightSession.BypassHandshake(charClass: 0); // Knight

        TestGameSession observer = new(map);
        observer.OnConnected(new IPEndPoint(IPAddress.Loopback, 1));
        observer.BypassHandshake(charClass: 0);

        map.Tick(1);

        PlayerEntity? knight = map.GetPlayer(AttackerEntityId);
        Assert.NotNull(knight);
        // Knight AttackHalfExtent=1.5f → (9,0)에서 enemy=(10,0) → hit
        knight!.Position = new Vector2(NormalX - 1f, NormalY);

        EnemyEntity enemy = map.Enemies[NormalEnemyId];

        knightSession.SentPackets.Clear();
        observer.SentPackets.Clear();

        knightSession.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        map.Tick(2);

        // 즉시 S_HitResult — DeferredDamageSystem 아님
        Assert.Equal(1, CountPacketsOfType(knightSession.SentPackets, PacketID.S_HitResult));

        byte[] hitPkt = knightSession.SentPackets.First(p => PacketIdOf(p) == PacketID.S_HitResult);
        S_HitResult parsedHit = new S_HitResult();
        parsedHit.Read(new ArraySegment<byte>(hitPkt));
        Assert.Equal((byte)0, parsedHit.hitEffect); // 0 = 근접

        // 음수 currentHp 계약: Knight hit이 치명타라도 raw 음수 그대로(LagSim 봇 계약)
        // NormalMaxHp=30, ExpectedDamage=25 → Hp=5 > 0이므로 이 테스트에선 양수
        int expectedDmg = Formulas.ComputeDamage(PlayerStats.Knight(), EnemyStats.NormalDefault(), baseDamage: 10);
        Assert.Equal(NormalMaxHp - expectedDmg, parsedHit.currentHp);

        // S_ProjectileLaunch 없음 (Knight는 즉시 경로)
        Assert.Equal(0, CountPacketsOfType(knightSession.SentPackets, PacketID.S_ProjectileLaunch));

        // rate-limit 차단: 동일 틱 직후 재공격 → silent drop
        knightSession.SentPackets.Clear();
        observer.SentPackets.Clear();
        knightSession.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 3));
        map.Tick(3);

        // 두 번째 스윙도 rate-limit으로 drop
        Assert.Equal(0, CountPacketsOfType(observer.SentPackets, PacketID.S_PlayerAttack));
        Assert.Equal(0, CountPacketsOfType(knightSession.SentPackets, PacketID.S_HitResult));
    }
}
