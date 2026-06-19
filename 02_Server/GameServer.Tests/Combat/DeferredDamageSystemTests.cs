using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// DeferredDamageSystem + FrozenUntilTick + Boss freeze 면역 단위 테스트 (M4.8 Phase 02).
///
/// 검증 대상:
///   1. Deferred_BeforeImpactTick_NoDamage    — impactTick 도달 전 HP 불변
///   2. Deferred_AtImpactTick_DamageApplied   — impactTick 도달 틱에 정확히 1회 데미지 + S_HitResult
///   3. Deferred_KillPath_DeathAndRespawn      — HP≤0 시 S_EntityDeath + Normal respawn 경로
///   4. Freeze_NormalEnemy_XUnchanged          — FrozenUntilTick 동안 X 좌표 불변
///   5. Freeze_NormalEnemy_ResumesAfterExpiry  — 만료 후 이동 재개
///   6. Boss_FreezeIgnored_MovementContinues   — Boss에 ApplyFreeze 호출돼도 BossBehaviorSystem 정상
///   7. Deferred_TargetDeadOnArrival_Skip      — 도착 시 타겟 사망 → skip, 예외 없음
///
/// 테스트 전략:
///   - GameMap 단독 생성 (idAllocator=null) → GameWorld 싱글톤 의존 X.
///   - FakeCapturingSession으로 broadcast 패킷 캡처.
///   - map.EnqueueDeferredDamage 직접 호출 + map.Tick(n)으로 처리.
/// </summary>
[Collection("ConsoleSerial")]
public class DeferredDamageSystemTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    public DeferredDamageSystemTests()
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

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static GameMap MakeHuntingGround() =>
        new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        }));

    static GameMap MakeBossRoom() =>
        new GameMap(MapId.BossRoom, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 30f, 0f),
        }));

    static GameMap MakeGolemMap() =>
        new GameMap(MapId.HuntingGround, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Golem, 10f, 0f),
        }));

    static EnemyEntity FirstEnemy(GameMap map)
    {
        foreach (EnemyEntity e in map.Enemies.Values) return e;
        throw new InvalidOperationException("map has no enemies");
    }

    static List<byte[]> AttachObserver(GameMap map)
    {
        var sink = new List<byte[]>();
        map.AddPlayer(new FakeCapturingSession(sink), new Vector2(5f, 0f));
        return sink;
    }

    static bool IsPacket(byte[] payload, PacketID id)
    {
        if (payload.Length < 4) return false;
        ushort raw = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)raw == id;
    }

    // ── 테스트 1: impactTick 도달 전 HP 불변 ─────────────────────────────────

    [Fact]
    public void Deferred_BeforeImpactTick_NoDamage()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = FirstEnemy(map);
        int originalHp = enemy.Hp;

        // impactTick=5, 현재 tick=1~4 → 데미지 없어야 함
        map.EnqueueDeferredDamage(new DeferredImpact
        {
            AttackerEntityId = 0,
            TargetEntityId   = enemy.EntityId,
            Damage           = 5,
            ImpactTick       = 5,
            HitEffect        = 1,
        });

        for (long t = 1; t <= 4; t++)
            map.Tick(t);

        Assert.Equal(originalHp, enemy.Hp);
        Assert.False(enemy.IsDead);
    }

    // ── 테스트 2: impactTick 도달 틱에 정확히 1회 데미지 ─────────────────────

    [Fact]
    public void Deferred_AtImpactTick_DamageApplied_Once()
    {
        GameMap map = MakeHuntingGround();
        var sink = AttachObserver(map);
        EnemyEntity enemy = FirstEnemy(map);
        int originalHp = enemy.Hp;
        const int damage = 7;

        map.EnqueueDeferredDamage(new DeferredImpact
        {
            AttackerEntityId = 99,
            TargetEntityId   = enemy.EntityId,
            Damage           = damage,
            ImpactTick       = 3,
            HitEffect        = 1,
        });

        // tick=3에서 처리
        map.Tick(1);
        map.Tick(2);
        Assert.Equal(originalHp, enemy.Hp); // 아직 미도달

        sink.Clear();
        map.Tick(3);

        Assert.Equal(originalHp - damage, enemy.Hp);
        int hitCount = sink.Count(p => IsPacket(p, PacketID.S_HitResult));
        Assert.Equal(1, hitCount);

        // 내용 검증
        byte[] hitPkt = sink.First(p => IsPacket(p, PacketID.S_HitResult));
        S_HitResult parsed = new S_HitResult();
        parsed.Read(new ArraySegment<byte>(hitPkt));
        Assert.Equal(99, parsed.attackerEntityId);
        Assert.Equal(enemy.EntityId, parsed.targetEntityId);
        Assert.Equal(damage, parsed.damage);
        Assert.Equal(1, parsed.hitEffect);

        // 재처리 없음 — tick=4에서 다시 적용되면 안 됨
        int hpAfterTick3 = enemy.Hp;
        sink.Clear();
        map.Tick(4);
        Assert.Equal(hpAfterTick3, enemy.Hp);
        Assert.Equal(0, sink.Count(p => IsPacket(p, PacketID.S_HitResult)));
    }

    // ── 테스트 3: HP≤0 시 사망 경로 ─────────────────────────────────────────

    [Fact]
    public void Deferred_KillPath_DeathBroadcast_And_RespawnEnqueued()
    {
        GameMap map = MakeHuntingGround();
        var sink = AttachObserver(map);
        EnemyEntity enemy = FirstEnemy(map);
        int entityId = enemy.EntityId;

        // HP 1 남기고 치명 deferred
        enemy.Hp = 1;
        map.EnqueueDeferredDamage(new DeferredImpact
        {
            AttackerEntityId = 0,
            TargetEntityId   = entityId,
            Damage           = 1,
            ImpactTick       = 2,
            HitEffect        = 0,
        });

        map.Tick(1);
        sink.Clear();
        map.Tick(2);

        // 즉시 제거
        Assert.False(map.Enemies.ContainsKey(entityId));
        // S_EntityDeath broadcast
        int deathCount = sink.Count(p => IsPacket(p, PacketID.S_EntityDeath));
        Assert.Equal(1, deathCount);

        // Normal → respawn 큐 등록 확인: NormalEnemyRespawnTicks(100) 후 재출현
        for (long t = 3; t <= 105; t++)
            map.Tick(t);
        Assert.Single(map.Enemies);
    }

    // ── 테스트 4: Normal 적 frozen 동안 X 좌표 불변 ──────────────────────────

    [Fact]
    public void Freeze_NormalEnemy_XUnchangedWhileFrozen()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = FirstEnemy(map);

        // player를 바로 옆에 둬 Chase 유도 (AggroOnSight=false라 시야 aggro 없음 — Patrol 유지)
        // 대신 FrozenUntilTick을 직접 세팅해 freeze 검증
        map.Tick(1); // FSM 초기화 1틱

        float xBefore = enemy.X;
        // tick=1에서 세팅 → tick=2~4(3틱) frozen, tick=5에서 해제
        enemy.ApplyFreeze(5);

        for (long t = 2; t <= 4; t++)
        {
            map.Tick(t);
            Assert.Equal(xBefore, enemy.X, 0.001f);
        }
    }

    // ── 테스트 5: 만료 후 이동 재개 ──────────────────────────────────────────

    [Fact]
    public void Freeze_NormalEnemy_ResumesAfterExpiry()
    {
        // Golem(선공) 사용 — 이동을 확인하려면 chase 유도가 편함
        GameMap map = MakeGolemMap();
        EnemyEntity enemy = FirstEnemy(map);

        // player를 AggroRange 안에 배치
        map.AddPlayer(null, new Vector2(enemy.X + 2f, 0f));
        map.Tick(1); // Chase 전환
        Assert.Equal(EnemyState.Chase, enemy.State);

        float xAfterChase = enemy.X;

        // freeze 세팅: tick=2 시점에 FrozenUntilTick=3 (tick=2,tick=3 frozen)
        enemy.ApplyFreeze(3);
        map.Tick(2);
        Assert.Equal(xAfterChase, enemy.X, 0.001f); // frozen → 불변

        // tick=3: FrozenUntilTick == tickNumber → 해제 + Fsm.Tick 실행
        map.Tick(3);
        // tick=3에서 해제됐으므로 FrozenUntilTick은 0
        Assert.Equal(0, enemy.FrozenUntilTick);

        // tick=4: 해제 후 정상 이동
        float xAtTick3 = enemy.X;
        map.Tick(4);
        // Chase 상태이므로 X가 변화해야 함 (player 방향으로)
        Assert.NotEqual(xAtTick3, enemy.X);
    }

    // ── 테스트 6: Boss에 ApplyFreeze → BossBehaviorSystem 이동 정상 (면역) ──

    [Fact]
    public void Boss_ApplyFreeze_BossBehaviorSystemContinues()
    {
        GameMap map = MakeBossRoom();
        EnemyEntity boss = FirstEnemy(map);
        Assert.Equal(EnemyKind.Boss, boss.Kind);

        // player를 보스 공격 사거리 밖에 배치 (BossWanderTicks 동안 배회하게)
        map.AddPlayer(null, new Vector2(boss.X + 20f, 0f));

        map.Tick(1); // FSM 초기화
        float xAfterTick1 = boss.X;

        // freeze 세팅 (만료 tick 크게 — 테스트 전 틱 동안 유지)
        boss.ApplyFreeze(1000);

        // 보스 초기 쿨다운(BossPhase1CooldownTicks=40) 소진 + Move 진입 + 배회 이동 확인.
        // 80틱이면 쿨다운 소진(40틱) + Move 배회(BossWanderTicks=20틱)에 충분.
        bool moved = false;
        for (long t = 2; t <= 80; t++)
        {
            map.Tick(t);
            if (Math.Abs(boss.X - xAfterTick1) > 0.001f)
            {
                moved = true;
                break;
            }
        }
        Assert.True(moved, "Boss should still move despite FrozenUntilTick set (Boss is immune to freeze).");
        // FrozenUntilTick 필드는 세팅된 채로 남아있어야 함 (BossBehaviorSystem 가드 없음 → 면역)
        Assert.True(boss.FrozenUntilTick > 0, "Boss FrozenUntilTick should remain set (no guard in BossBehaviorSystem).");
    }

    // ── 테스트 7: 도착 시 타겟 사망/디스폰 → skip ────────────────────────────

    [Fact]
    public void Deferred_TargetDeadOnArrival_Skip_NoException()
    {
        GameMap map = MakeHuntingGround();
        var sink = AttachObserver(map);
        EnemyEntity enemy = FirstEnemy(map);
        int entityId = enemy.EntityId;
        int hpBefore = enemy.Hp;

        map.EnqueueDeferredDamage(new DeferredImpact
        {
            AttackerEntityId = 0,
            TargetEntityId   = entityId,
            Damage           = 5,
            ImpactTick       = 3,
            HitEffect        = 0,
        });

        // 도착 전에 적을 직접 제거 (사망 시뮬)
        map.Tick(1);
        enemy.Hp = 0; // IsDead=true
        map.RemoveEnemy(entityId); // _enemies에서 제거

        sink.Clear();
        // 예외 없이 처리, S_HitResult 없음
        var ex = Record.Exception(() =>
        {
            map.Tick(2);
            map.Tick(3);
        });
        Assert.Null(ex);
        Assert.Equal(0, sink.Count(p => IsPacket(p, PacketID.S_HitResult)));
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
}
