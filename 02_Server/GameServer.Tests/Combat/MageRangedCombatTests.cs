using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// Mage 평타 원거리 단위 테스트 (M4.8 Phase 03 + M4.15 Phase 02 X/Y 분리 갱신).
///
/// 검증 대상:
///   1. Mage_Hit_InRange — 사거리 내 명중: S_ProjectileLaunch 1회 + deferred enqueue, 즉시 S_HitResult 없음, freeze 없음 (M4.15 P03)
///   2. Mage_Hit_ImpactTick_DamageApplied — travelTicks 후 DeferredDamageSystem이 HP 적용 + S_HitResult(hitEffect=1)
///   3. Mage_Miss_OutOfRange — 사거리 밖: S_PlayerAttack만, S_ProjectileLaunch 없음, 데미지 0
///   4. Knight_ImmediateHit_HitEffect0 — Knight 즉시 데미지(hitEffect=0), 음수 currentHp 계약 유지 + rate-limit 차단 유지
///   5. Mage_Hit_SameLayer_Miss_UpperLayer — Y범위 회귀: 같은 Y 적은 hit, 층간격 초과(Y=3.0) 적은 miss (Phase 02 핵심)
///
/// 테스트 전략:
///   - attacker(Mage) + observer 2세션을 같은 맵에 등록. observer가 broadcast 수신.
///   - TestGameSession.BypassHandshake(charClass) 으로 class 설정.
///   - S_ProjectileLaunch / S_HitResult는 PacketID 헤더(offset 2~3)로 식별.
///   - travelTicks = clamp(round(|dx| / ProjectileSpeedPerTick), Min, Max) 직접 산출 후 비교.
///
/// M4.15 Phase 02 박스 상수:
///   MageAttackHalfX=11.0f, MageAttackHalfY=1.0f (X/Y 분리).
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
    /// travelTicks = max(MinTravelTicks, ceil(2D-dist / speed)) 직접 산출 (테스트 기준값).
    /// Phase 04: round+clamp(Min,Max) → ceil+max(Min) + 2D distance 로 교체.
    /// </summary>
    static int ExpectedTravelTicks(float attackerX, float enemyX, float attackerY = 0f, float enemyY = 0f)
    {
        float dx = enemyX - attackerX;
        float dy = enemyY - attackerY;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return Math.Max(
            CombatConstants.MinTravelTicks,
            (int)Math.Ceiling(dist / CombatConstants.ProjectileSpeedPerTick));
    }

    // ── 테스트 1: Mage 사거리 내 명중 → S_ProjectileLaunch + deferred, 즉시 데미지 없음, freeze 없음 ──

    [Fact]
    public void Mage_Hit_InRange_ProjectileLaunchAndDeferred_NoImmediateDamage_NoFreeze()
    {
        var (attacker, observer, map) = SetupMageSession();
        PlayerEntity? attackerEntity = map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        // Mage(MageAttackHalfX=11.0f) + enemy=(10,0) → 사거리 내:
        // attacker를 (7,0)으로 배치 → attackBox x[-4,18] ∩ enemy x[9.5,10.5] = hit.
        // Y: attackBox y[-1,1] ∩ enemy y[-0.5,0.5] = hit (같은 층).
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

        // freeze 없음 확인 (M4.15 P03 — 에너지볼트는 stun 없음, 인프라만 보존)
        Assert.Equal(0L, enemy.FrozenUntilTick);
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

        // attacker=(-3,0)에서 enemy=(10,0) → attackBox x[-14,8] ∩ enemy x[9.5,10.5]:
        //   boxMaxX=8 < enemyMinX=9.5 → X miss(사거리 밖).
        // (옛: attacker=(0,0) → MageAttackHalfX=11이면 attackBox x[-11,11] ∩ enemy x[9.5,10.5] = hit이 돼 갱신)
        attackerEntity!.Position = new Vector2(-3f, NormalY);

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
        // Knight KnightAttackHalfX=1.5f, KnightAttackHalfY=1.0f → (9,0)에서 enemy=(10,0):
        //   attackBox x[7.5,10.5] ∩ enemy x[9.5,10.5] = hit(X).
        //   attackBox y[-1,1] ∩ enemy y[-0.5,0.5] = hit(Y). → 명중.
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

    // ── 테스트 6: travelTicks 단조증가 — 상한 폭증 없음 (Phase 04 acceptance) ─────────────

    /// <summary>
    /// 옛 MaxTravelTicks(10) 상한이 제거된 뒤, 거리가 멀수록 travelTicks가 단조증가(비감소)함을 검증.
    /// 사거리 내(dist ≤ MageAttackHalfX=11) 여러 거리를 체크 — 상한 clamp로 속도 폭증이 재현되지 않음.
    /// </summary>
    [Fact]
    public void Mage_TravelTicks_MonotonicallyIncreasing_NoUpperBoundSpike()
    {
        // 2D 거리 2, 4, 6, 8, 10 (사거리 내, 적 Y=attacker Y=0)
        float[] distances = [2f, 4f, 6f, 8f, 10f];
        int prev = -1;
        foreach (float d in distances)
        {
            // enemy X = attacker X + d, 동일 Y
            int ticks = ExpectedTravelTicks(0f, d, 0f, 0f);
            Assert.True(ticks >= prev,
                $"dist={d}: travelTicks={ticks} < prev={prev} — 단조 감소 발생");
            prev = ticks;
        }

        // 거리 10일 때 travelTicks = ceil(10/2)=5 — 옛 상한(10)이 없으므로 clamp 영향 0
        int far = ExpectedTravelTicks(0f, 10f, 0f, 0f);
        Assert.Equal(5, far);
    }

    // ── 테스트 5: Y범위 회귀 — 같은 층 hit, 층간격 초과(Y=3.0) miss (Phase 02 핵심 acceptance) ──

    /// <summary>
    /// Mage 평타가 같은 Y의 적은 hit, Y가 MageAttackHalfY(1.0f)를 초과한 적은 miss.
    ///
    /// **시나리오**:
    ///   - attacker=(0, 0), enemy_same_layer=(3, 0): attackBox y[-1,1] ∩ enemy y[-0.5,0.5] = hit.
    ///   - attacker=(0, 0), enemy_upper=(3, 3.0): |dy|=3.0, sumHalfY=1.0+0.5=1.5 → 3.0 > 1.5 → miss.
    ///   Y=3.0은 층간격(사이드스크롤 플랫폼 기준 위층 적) 대표값.
    ///
    /// **Phase 02 acceptance criterion**: MageAttackHalfY=1.0f가 위층 오판정을 제거함.
    /// 옛 정사각(half=8.0)에서는 위층(Y=3)도 ±8 범위 안이라 hit — Phase 02에서 miss로 전환됨을 검증.
    /// </summary>
    [Fact]
    public void Mage_SameLayer_Hit_UpperLayer_Miss_YRangeRegression()
    {
        // 맵: NormalEnemy 2개(같은 층 + 위층) + Boss(별 위치).
        // enemy_same: x=3, y=0 (attacker와 같은 층)
        // enemy_upper: x=3, y=3.0 (층간격 초과, hit이면 Phase 02 실패)
        const float EnemySameX  = 3f;
        const float EnemySameY  = 0f;
        const float EnemyUpperX = 3f;
        const float EnemyUpperY = 3.0f; // MageAttackHalfY(1.0) + HitboxHalfExtent(0.5) = 1.5 < 3.0 → miss

        GameMap map = new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, EnemySameX,  EnemySameY),  // id=1: 같은 층
            new EnemySpawnPoint((byte)EnemyKind.Normal, EnemyUpperX, EnemyUpperY), // id=2: 위층(층간격 초과)
            new EnemySpawnPoint((byte)EnemyKind.Boss,   BossX,       BossY),       // id=3: 무관
        }));

        TestGameSession attacker = new(map);
        attacker.OnConnected(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        attacker.BypassHandshake(charClass: 1); // Mage

        TestGameSession observer = new(map);
        observer.OnConnected(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 1));
        observer.BypassHandshake(charClass: 0);

        map.Tick(1);

        // caster 원점에 세팅. 이 맵은 enemy 3개(id 1,2,3)이므로 첫 player는 id=4.
        // map.Players[0]으로 동적 조회 (AttackerEntityId=3 상수는 enemy 2개 맵 기준).
        Assert.NotEmpty(map.Players);
        PlayerEntity casterEntity = map.Players[0];
        casterEntity.Position = new Vector2(0f, 0f);
        casterEntity.RecordPosition(1, casterEntity.Position);

        EnemyEntity enemySame  = map.Enemies[1]; // 같은 층
        EnemyEntity enemyUpper = map.Enemies[2]; // 위층
        int hpSameBefore  = enemySame.Hp;
        int hpUpperBefore = enemyUpper.Hp;

        attacker.SentPackets.Clear();

        // 같은 층 적 공격 → hit 검증
        attacker.OnRecvPacket(AttackPacketBytes(enemySame.EntityId, attackerClientTick: 2));
        map.Tick(2);

        // S_ProjectileLaunch: 같은 층 hit이면 발사됨
        Assert.Equal(1, CountPacketsOfType(attacker.SentPackets, PacketID.S_ProjectileLaunch));
        // 즉시 HP 불변 (deferred)
        Assert.Equal(hpSameBefore, enemySame.Hp);
        Assert.Equal(hpUpperBefore, enemyUpper.Hp);

        // 위층 적 — ResolveImpactTargets는 타겟 힌트와 무관하게 박스 ∩ 전 적을 검사하지 않음.
        // MeleeAction은 ctx.TargetEntityId(같은 층 적) 기준으로 attackBox를 생성해 교차 체크.
        // 위층 적이 박스 밖인지 직접 AABB로 검증 (서버 권위 판정 로직 보존 확인).
        AABB mageBox = CombatSystem.GetAttackHitbox(new Vector2(0f, 0f), CharacterClass.Mage);
        AABB upperHitbox = new AABB(new Vector2(EnemyUpperX, EnemyUpperY),
            new Vector2(CombatConstants.HitboxHalfExtent, CombatConstants.HitboxHalfExtent));
        AABB sameHitbox  = new AABB(new Vector2(EnemySameX, EnemySameY),
            new Vector2(CombatConstants.HitboxHalfExtent, CombatConstants.HitboxHalfExtent));

        // Phase 02 acceptance: 같은 층 = hit, 위층(Y=3.0) = miss
        Assert.True(mageBox.Intersects(sameHitbox),
            $"같은 층 적이 miss — MageAttackHalfY({CombatConstants.MageAttackHalfY}) 부족. box={mageBox}");
        Assert.False(mageBox.Intersects(upperHitbox),
            $"위층(Y={EnemyUpperY}) 적이 hit — MageAttackHalfY({CombatConstants.MageAttackHalfY}) 과다. box={mageBox}");
    }
}
