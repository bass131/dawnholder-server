using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// HandleEnemyDeath killerEntityId 전파 검증 (M5 Phase Q1).
///
/// 검증 대상:
///   1. MeleeAction_KillPath_KillerEntityId  — 평타 즉시 사망 시 killerEntityId = attacker.EntityId
///   2. DashAction_KillPath_KillerEntityId   — Dash 충돌 사망 시 killerEntityId = caster.EntityId
///   3. DeferredDamage_KillPath_KillerEntityId — 지연 데미지 사망 시 killerEntityId = impact.AttackerEntityId
///   4. KillSequence_OrderPreserved          — OnEnemyKilled는 기존 시퀀스(S_EntityDeath/RemoveEnemy) 완료 *후* 호출됨
///
/// 테스트 전략:
///   - SpyGameMap: GameMap을 override해 OnEnemyKilled 호출을 캡처.
///   - FakeCapturingSession: broadcast 패킷을 수집해 S_EntityDeath 순서 확인.
///   - BossStageClearTests / KnightDashTests 패턴 정합.
/// </summary>
[Collection("ConsoleSerial")]
public class HandleEnemyDeathKillerTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    public HandleEnemyDeathKillerTests()
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

    // ── SpyGameMap ─────────────────────────────────────────────────────────────

    // GameMap 서브클래스로 OnEnemyKilled 훅을 spy. Q2가 이 훅을 override해 PartyRegistry를 연결한다.
    sealed class SpyGameMap : GameMap
    {
        public int? CapturedKillerId { get; private set; }
        public EnemyEntity? CapturedTarget { get; private set; }
        public int CallCount { get; private set; }

        // _enemies 상태(Remove 완료)를 훅에서 검증하기 위해 캡처 시점의 적 존재 여부 저장.
        public bool EnemyAlreadyRemovedWhenHookCalled { get; private set; }

        public SpyGameMap(MapId mapId, MapContent content)
            : base(mapId, content: content) { }

        protected override void OnEnemyKilled(int killerEntityId, EnemyEntity target)
        {
            CapturedKillerId = killerEntityId;
            CapturedTarget   = target;
            CallCount++;
            // 훅 호출 시점에 이미 _enemies에서 제거됐어야 함(시퀀스 순서 계약).
            EnemyAlreadyRemovedWhenHookCalled = !Enemies.ContainsKey(target.EntityId);
        }
    }

    // ── FakeCapturingSession ───────────────────────────────────────────────────

    sealed class FakeCapturingSession : GameSession
    {
        readonly List<byte[]> _sink;
        public FakeCapturingSession(List<byte[]> sink) { _sink = sink; }

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            _sink.Add(copy);
        }

        protected override GameMap? GetMap() => null;
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }

    // SpyGameMap을 GetMap으로 주입하는 TestGameSession.
    sealed class TestGameSession : GameSession
    {
        readonly SpyGameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();

        public TestGameSession(SpyGameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }

        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }

        public void BypassHandshake(byte charClass = (byte)CharacterClass.Knight)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(charClass);
            EnterGameWorldIfReady();
        }
    }

    // ── 공통 헬퍼 ─────────────────────────────────────────────────────────────

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static ArraySegment<byte> AttackPacketBytes(int targetEntityId, long clientTick = 1)
    {
        C_Attack pkt = new C_Attack { targetEntityId = targetEntityId, attackerClientTick = (int)clientTick };
        return pkt.Write();
    }

    static ArraySegment<byte> DashPacketBytes(long clientTick = 1, byte facing = 1)
    {
        C_SkillUse pkt = new C_SkillUse
        {
            skillId            = (byte)SkillId.Dash,
            attackerClientTick = (int)clientTick,
            facing             = facing,
        };
        return pkt.Write();
    }

    // Normal enemy 1마리 + 관찰 세션 붙이기.
    (SpyGameMap map, List<byte[]> sink) MakeHuntingGroundWithObserver(float enemyX = 10f)
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, enemyX, 0f),
        });
        var map = new SpyGameMap(MapId.HuntingGround, content);
        var sink = new List<byte[]>();
        map.AddPlayer(new FakeCapturingSession(sink), new Vector2(5f, 0f));
        return (map, sink);
    }

    // ── 테스트 1: MeleeAction 즉시 평타 경로 ─────────────────────────────────

    [Fact]
    public void MeleeAction_KillPath_KillerEntityId_IsAttackerEntityId()
    {
        // arrange: Knight 세션 + Normal enemy를 사거리 안에 배치. HP를 1로 만들어 다음 타에 킬.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 1f, 0f),
        });
        var map = new SpyGameMap(MapId.HuntingGround, content);
        var session = new TestGameSession(map);
        session.OnConnected(new IPEndPoint(System.Net.IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Knight);
        map.Tick(1);

        PlayerEntity player = map.Players[0];
        player.Position = new Vector2(0f, 0f);
        player.RecordPosition(1, player.Position);
        int attackerEntityId = player.EntityId;

        EnemyEntity enemy = map.Enemies.Values.First();
        int enemyEntityId = enemy.EntityId;
        enemy.Hp = 1; // 다음 평타 한 방으로 킬

        session.SentPackets.Clear();

        // act: 평타 패킷 → Tick 처리
        session.OnRecvPacket(AttackPacketBytes(enemyEntityId, clientTick: 1));
        map.Tick(2);

        // assert: OnEnemyKilled 1회 + killerEntityId == attackerEntityId
        Assert.Equal(1, map.CallCount);
        Assert.Equal(attackerEntityId, map.CapturedKillerId);
        Assert.Equal(enemyEntityId, map.CapturedTarget!.EntityId);
    }

    // ── 테스트 2: DashAction 충돌 경로 ───────────────────────────────────────

    [Fact]
    public void DashAction_KillPath_KillerEntityId_IsCasterEntityId()
    {
        // arrange: Knight(Dash 가능) + Normal enemy를 대쉬 경로 상에 배치. HP=1.
        // Dash 박스: caster.x=0, facing=+1 → AABB 오른쪽. DashBoxHalfX=2.5f 안에 적을 세팅.
        float enemyX = CombatConstants.DashBoxHalfX; // 박스 중심점 일치
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, enemyX, 0f),
        });
        var map = new SpyGameMap(MapId.HuntingGround, content);
        var session = new TestGameSession(map);
        session.OnConnected(new IPEndPoint(System.Net.IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: (byte)CharacterClass.Knight);
        map.Tick(1);

        PlayerEntity caster = map.Players[0];
        caster.Position = new Vector2(0f, 0f);
        caster.FacingDir = 1;
        caster.RecordPosition(1, caster.Position);
        int casterEntityId = caster.EntityId;

        EnemyEntity enemy = map.Enemies.Values.First();
        int enemyEntityId = enemy.EntityId;
        enemy.Hp = 1; // 대쉬 1회로 킬

        session.SentPackets.Clear();

        // act: Dash 패킷 → Tick 처리
        session.OnRecvPacket(DashPacketBytes(clientTick: 1, facing: 1));
        map.Tick(2);

        // assert: OnEnemyKilled 1회 + killerEntityId == casterEntityId
        Assert.Equal(1, map.CallCount);
        Assert.Equal(casterEntityId, map.CapturedKillerId);
        Assert.Equal(enemyEntityId, map.CapturedTarget!.EntityId);
    }

    // ── 테스트 3: DeferredDamageSystem 지연 경로 (썬더볼트 AoE 포함) ─────────

    [Fact]
    public void DeferredDamage_KillPath_KillerEntityId_IsAttackerEntityId()
    {
        // arrange: Normal enemy HP=1. DeferredImpact.AttackerEntityId=99 → killerEntityId=99.
        var (map, _) = MakeHuntingGroundWithObserver();
        EnemyEntity enemy = map.Enemies.Values.First();
        int enemyEntityId = enemy.EntityId;
        const int expectedKillerId = 99;

        enemy.Hp = 1;

        map.EnqueueDeferredDamage(new DeferredImpact
        {
            AttackerEntityId = expectedKillerId,
            TargetEntityId   = enemyEntityId,
            Damage           = 1,
            ImpactTick       = 3,
            HitEffect        = 1,
        });

        map.Tick(1);
        map.Tick(2);
        Assert.Equal(0, map.CallCount); // impactTick 미도달 → 훅 미호출

        map.Tick(3); // impactTick 도달 → 사망 처리 → 훅 호출

        // assert: OnEnemyKilled 1회 + killerEntityId == AttackerEntityId
        Assert.Equal(1, map.CallCount);
        Assert.Equal(expectedKillerId, map.CapturedKillerId);
        Assert.Equal(enemyEntityId, map.CapturedTarget!.EntityId);
    }

    // ── 테스트 4: 시퀀스 순서 보존 — OnEnemyKilled는 RemoveEnemy 이후 호출 ─────

    [Fact]
    public void KillSequence_OnEnemyKilled_CalledAfterRemoveEnemy()
    {
        // 기존 순서 계약(순서 계약 BossStageClearTests 정합):
        //   S_EntityDeath broadcast → (Boss) StageClear → RemoveEnemy → (Normal) EnqueueRespawn → OnEnemyKilled
        // SpyGameMap이 훅 호출 시점에 _enemies.ContainsKey(target) 검사 → false면 순서 보존 확인.
        var (map, sink) = MakeHuntingGroundWithObserver();
        EnemyEntity enemy = map.Enemies.Values.First();
        int enemyEntityId = enemy.EntityId;

        enemy.Hp = 1;

        map.EnqueueDeferredDamage(new DeferredImpact
        {
            AttackerEntityId = 42,
            TargetEntityId   = enemyEntityId,
            Damage           = 1,
            ImpactTick       = 2,
            HitEffect        = 0,
        });

        sink.Clear();
        map.Tick(2);

        // 순서 보존: 훅 호출 시점에 이미 _enemies에서 제거됨
        Assert.True(map.EnemyAlreadyRemovedWhenHookCalled,
            "OnEnemyKilled가 RemoveEnemy *이전에* 호출됨 — 순서 계약 위반");

        // S_EntityDeath가 broadcast됐음 (기존 사망 처리 회귀 0)
        int deathCount = sink.Count(p => PacketIdOf(p) == PacketID.S_EntityDeath);
        Assert.Equal(1, deathCount);
    }
}
