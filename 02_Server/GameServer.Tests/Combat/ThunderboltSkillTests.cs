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
/// 썬더볼트 스킬 시스템 단위 테스트 (M4.8 Phase 04).
///
/// 검증 대상:
///   [핸들러 3종]
///   1. SkillHandler_Happy        — handshake 완료 + Thunderbolt → SubmitSkillUse 호출
///   2. SkillHandler_InvalidSkillId — skillId=0(None) → silent drop (cheat-flag 로그)
///   3. SkillHandler_AuthFailure  — handshake 미완료 → silent drop (cheat-flag 로그)
///
///   [시스템 5종]
///   4. BoxScan_TargetsInBox_AllEnqueued  — 박스 내 N개 전원 deferred enqueue, 박스 밖 0
///   5. LightningDelay_DamageAndHitEffect — 각 적 LightningDelayTicks 후 데미지 + S_HitResult(hitEffect=2)
///   6. Boss_DamageApplied_NoFreeze       — Boss 데미지 O / FrozenUntilTick 세팅 X (M4.15 P03: 썬더볼트 stun 제거, 모든 적 freeze 없음)
///   7. Cooldown_SecondCastDropped        — ThunderboltCooldownTicks 미경과 재발동 silent drop
///   8. EmptyBox_SkillCastOnly_DeferredZero — 타격 적 0개: S_SkillCast 1회, deferred 0
/// </summary>
[Collection("ConsoleSerial")]
public class ThunderboltSkillTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // GameMap ctor id 발급: Normal=1, Boss=2 → caster=3, observer=4.
    const int NormalEnemyId = 1;
    const int BossEnemyId   = 2;
    const int CasterEntityId = 3;

    const float NormalX    = 3f;   // 박스 내 (caster x=0, ThunderboltBoxHalfX=13.0 → 범위 [-13,13])
    const float NormalY    = 0f;
    const float BossX      = 4f;   // 박스 내
    const float BossY      = 0f;
    const float OutsideX   = 20f;  // 박스 밖 (halfX=13, origin=0 → x=20은 범위 밖)

    public ThunderboltSkillTests()
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
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        public void BypassHandshake(byte charClass = 1) // 기본 Mage
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(charClass);
            EnterGameWorldIfReady();
        }
    }

    // ── 헬퍼 ───────────────────────────────────────────────────────────────────

    static GameMap MakeMapWithNormalAndBoss() => new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
    {
        new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
        new EnemySpawnPoint((byte)EnemyKind.Boss,   BossX,   BossY),
    }));

    static GameMap MakeMapWithOutsideEnemy() => new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
    {
        new EnemySpawnPoint((byte)EnemyKind.Normal, OutsideX, 0f),
    }));

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    static ArraySegment<byte> SkillPacketBytes(byte skillId, int attackerClientTick)
    {
        C_SkillUse pkt = new C_SkillUse { skillId = skillId, attackerClientTick = attackerClientTick };
        return pkt.Write();
    }

    /// <summary>
    /// Mage 세션 + optional observer를 등록하고 Tick(1) 완료.
    /// caster 위치를 origin(0,0)으로 세팅 — 박스 중심이 0,0.
    /// </summary>
    (TestGameSession caster, List<byte[]> observerSink, GameMap map) SetupMageSession(GameMap? map = null)
    {
        map ??= MakeMapWithNormalAndBoss();

        TestGameSession caster = new(map);
        caster.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        caster.BypassHandshake(charClass: 1); // Mage

        // observer: broadcast 수신용. 별도 SentPackets.
        var observerSink = new List<byte[]>();
        TestGameSession observer = new(map);
        observer.OnConnected(new IPEndPoint(IPAddress.Loopback, 1));
        observer.BypassHandshake(charClass: 0); // Knight(observer)

        map.Tick(1); // AddPlayer 람다 처리 → entity 등록

        // caster 위치를 원점에 배치 — 박스 중심 = 원점.
        // NormalX=3, BossX=4 → 원점 기준 ThunderboltBoxHalfX=13 박스 안에 들어옴.
        PlayerEntity? casterEntity = map.GetPlayer(CasterEntityId);
        Assert.NotNull(casterEntity);
        casterEntity!.Position = new Vector2(0f, 0f);
        casterEntity.RecordPosition(1, casterEntity.Position);

        // observer SentPackets를 직접 연결해 관찰.
        // (observer의 SentPackets는 TestGameSession 필드이므로 캡처 후 반환)
        return (caster, observer.SentPackets, map);
    }

    // ── 핸들러 3종 ─────────────────────────────────────────────────────────────

    [Fact]
    public void SkillHandler_Happy_SubmitsSkillUse()
    {
        // arrange: handshake 완료 + Mage 선택
        var (caster, _, map) = SetupMageSession();
        caster.SentPackets.Clear();

        // act: C_SkillUse(Thunderbolt) 발송 → Tick으로 ProcessSkill 처리
        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(2);

        // S_SkillCast broadcast 1건 이상 수신 = 성공 처리 (caster + observer 포함 전원)
        // caster.SentPackets에서 S_SkillCast 검증
        int castCount = CountPacketsOfType(caster.SentPackets, PacketID.S_SkillCast);
        Assert.True(castCount >= 1, $"S_SkillCast 패킷이 없음 (got {castCount})");

        S_SkillCast parsed = new S_SkillCast();
        byte[] castPkt = caster.SentPackets.First(p => PacketIdOf(p) == PacketID.S_SkillCast);
        parsed.Read(new ArraySegment<byte>(castPkt));
        Assert.Equal(CasterEntityId, parsed.casterEntityId);
        Assert.Equal((byte)1, parsed.skillId);       // Thunderbolt=1
        Assert.Equal(CombatConstants.LightningDelayTicks, parsed.strikeDelayTicks);
    }

    [Fact]
    public void SkillHandler_InvalidSkillId_SilentDrop()
    {
        // arrange: handshake 완료
        var (caster, _, map) = SetupMageSession();
        caster.SentPackets.Clear();

        // act: skillId=0(None) 발송 → silent drop
        caster.OnRecvPacket(SkillPacketBytes(skillId: 0, attackerClientTick: 1));
        map.Tick(2);

        // S_SkillCast 없음 + cheat 로그
        Assert.Equal(0, CountPacketsOfType(caster.SentPackets, PacketID.S_SkillCast));
        Assert.Contains("[Trust] C_SkillUse unknown skillId=0", _consoleCapture.ToString());
    }

    [Fact]
    public void SkillHandler_AuthFailure_HandshakeIncomplete_SilentDrop()
    {
        // arrange: handshake 미완료
        GameMap map = MakeMapWithNormalAndBoss();
        TestGameSession caster = new(map);
        caster.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        // BypassHandshake 안 함

        // act: 첫 패킷으로 C_SkillUse → first-packet 게이트가 차단
        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(1);

        // first-packet 게이트: C_SkillUse는 C_Handshake 아니므로 Disconnect
        Assert.Equal(1, caster.DisconnectCalls);
        Assert.Contains("[Trust] First packet was C_SkillUse", _consoleCapture.ToString());
    }

    // ── 시스템 테스트 ───────────────────────────────────────────────────────────

    [Fact]
    public void BoxScan_TargetsInBox_AllEnqueued_OutsideExcluded()
    {
        // Normal(3,0) + Boss(4,0)는 박스 [-13,13]x[-1.5,1.5] 안. outside(20,0)은 밖.
        // 3개 enemy 맵: id=1(Normal inside), id=2(Boss inside), id=3(Normal outside).
        // SetupMageSession이 player 2명 추가 → caster=4, observer=5. CasterEntityId=3은 틀림.
        // 이 테스트는 별도로 직접 세션을 생성해서 id를 동적으로 구함.
        GameMap map = new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),  // id=1 (박스 안)
            new EnemySpawnPoint((byte)EnemyKind.Boss,   BossX,   BossY),    // id=2 (박스 안)
            new EnemySpawnPoint((byte)EnemyKind.Normal, OutsideX, 0f),      // id=3 (박스 밖)
        }));

        // caster만 등록 (id=4)
        TestGameSession caster = new(map);
        caster.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        caster.BypassHandshake(charClass: 1);
        map.Tick(1);

        Assert.Single(map.Players);
        PlayerEntity casterEntity = map.Players[0];
        casterEntity.Position = new Vector2(0f, 0f);
        casterEntity.RecordPosition(1, casterEntity.Position);

        caster.SentPackets.Clear();

        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(2); // ProcessSkill 처리 → deferred enqueue

        // impactTick 도달 → S_HitResult
        long impactTick = 2 + CombatConstants.LightningDelayTicks;
        for (long t = 3; t <= impactTick; t++)
            map.Tick(t);

        // 박스 안 2개(Normal + Boss)에 S_HitResult(hitEffect=2). OutsideNormal은 0.
        int hitsTotal = CountPacketsOfType(caster.SentPackets, PacketID.S_HitResult);
        Assert.Equal(2, hitsTotal);

        // outside(id=3)는 HP 불변
        EnemyEntity? outside = map.GetEnemyById(3);
        if (outside != null)
            Assert.Equal(outside.MaxHp, outside.Hp); // 박스 밖이라 미적용
    }

    [Fact]
    public void LightningDelay_DamageApplied_HitEffectIs2()
    {
        // Normal 적 1개만 박스 내 배치. LightningDelayTicks 후 S_HitResult(hitEffect=2) 확인.
        GameMap map = new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
        }));

        var (caster, observerSink, _) = SetupMageSession(map);
        EnemyEntity normal = map.GetEnemyById(NormalEnemyId)!;
        int originalHp = normal.Hp;
        caster.SentPackets.Clear();

        // tick=2에서 ProcessSkill 처리
        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(2);

        // impactTick 도달 전 — HP 불변
        for (long t = 3; t < 2 + CombatConstants.LightningDelayTicks; t++)
        {
            map.Tick(t);
            Assert.Equal(originalHp, normal.Hp);
        }

        // impactTick 도달 → 데미지 적용
        map.Tick(2 + CombatConstants.LightningDelayTicks);

        Assert.True(normal.Hp < originalHp, "썬더볼트 데미지가 적용돼야 함");
        int hitCount = CountPacketsOfType(caster.SentPackets, PacketID.S_HitResult);
        Assert.True(hitCount >= 1, "S_HitResult가 없음");

        // hitEffect=2 검증
        byte[] hitPkt = caster.SentPackets.Last(p => PacketIdOf(p) == PacketID.S_HitResult);
        S_HitResult parsed = new S_HitResult();
        parsed.Read(new ArraySegment<byte>(hitPkt));
        Assert.Equal((byte)2, parsed.hitEffect);
    }

    [Fact]
    public void Boss_DamageApplied_FrozenUntilTickNotSet()
    {
        // Boss만 박스 안에 배치
        GameMap map = new GameMap(MapId.BossRoom, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, BossX, BossY),
        }));

        TestGameSession caster = new(map);
        caster.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        caster.BypassHandshake(charClass: 1);
        map.Tick(1);

        // caster 위치를 원점에 세팅 — Boss(4,0) 박스 [-6,6] 안에 들어옴
        PlayerEntity? casterEntity = map.GetPlayer(map.Players[0].EntityId);
        Assert.NotNull(casterEntity);
        casterEntity!.Position = new Vector2(0f, 0f);
        casterEntity.RecordPosition(1, casterEntity.Position);

        EnemyEntity boss = map.Enemies.Values.First();
        long freezeBefore = boss.FrozenUntilTick;
        int hpBefore = boss.Hp;
        caster.SentPackets.Clear();

        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(2);

        // impactTick까지 진행
        for (long t = 3; t <= 2 + CombatConstants.LightningDelayTicks; t++)
            map.Tick(t);

        // Boss: 데미지 적용 O
        Assert.True(boss.Hp < hpBefore, "Boss에 썬더볼트 데미지가 적용돼야 함");
        // Boss: freeze 세팅 X (M4.15 P03 — 썬더볼트 ApplyFreeze 호출 제거. 모든 적에 freeze 없음).
        Assert.Equal(0L, boss.FrozenUntilTick);
    }

    [Fact]
    public void Cooldown_SecondCastDropped_BelowCooldownTicks()
    {
        var (caster, _, map) = SetupMageSession();
        caster.SentPackets.Clear();

        // 첫 번째 발동
        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(2);
        int castCountFirst = CountPacketsOfType(caster.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(1, castCountFirst);

        caster.SentPackets.Clear();

        // 쿨다운 미경과 상태에서 재발동 — tick=3(ThunderboltCooldownTicks=40 미경과)
        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 2));
        map.Tick(3);

        // silent drop → S_SkillCast 없음
        int castCountSecond = CountPacketsOfType(caster.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(0, castCountSecond);
    }

    [Fact]
    public void EmptyBox_SkillCastBroadcast_NoDeferredDamage()
    {
        // 박스 밖 적만 있는 맵
        GameMap map = MakeMapWithOutsideEnemy();
        var (caster, _, _) = SetupMageSession(map);
        EnemyEntity enemy = map.Enemies.Values.First();
        int hpBefore = enemy.Hp;
        caster.SentPackets.Clear();

        caster.OnRecvPacket(SkillPacketBytes(skillId: 1, attackerClientTick: 1));
        map.Tick(2);

        // S_SkillCast 1회 (캐스팅 모션)
        int castCount = CountPacketsOfType(caster.SentPackets, PacketID.S_SkillCast);
        Assert.Equal(1, castCount);

        // S_HitResult 없음 — deferred 0 (빈 박스)
        // impactTick 지나도 확인
        for (long t = 3; t <= 2 + CombatConstants.LightningDelayTicks + 2; t++)
            map.Tick(t);

        int hitCount = CountPacketsOfType(caster.SentPackets, PacketID.S_HitResult);
        Assert.Equal(0, hitCount);

        // 적 HP 불변
        Assert.Equal(hpBefore, enemy.Hp);
    }
}
