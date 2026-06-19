using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// 일반몹(Normal/Golem) 공격 로직 단위 테스트 (M5 Phase 17 C1).
///
/// 검증 대상:
///   1. Normal/Golem이 사거리 안 플레이어에게 데미지 적용 (HP 감소)
///   2. 사거리 밖 플레이어에게는 데미지·broadcast 없음
///   3. 공격 시 S_EnemyAttack(ID=20) broadcast 발생 — 신규 패킷 0
///   4. 무적 게이트: IsInvulnerable(tick)이면 데미지·broadcast skip
///   5. 쿨다운: 1회 공격 후 NormalAttackCooldownTicks 동안 재공격 없음
///   6. 쿨다운 만료 후 재공격 가능
///   7. 보스 동작 회귀 없음 — ApplyMeleeDamage 헬퍼 추출 후 보스 데미지 수식 동일
/// </summary>
public class EnemyAttackTests
{
    // ── 헬퍼 ───────────────────────────────────────────────────────────────────

    static GameMap MakeNormalMap()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        return new GameMap(MapId.HuntingGround, content: content);
    }

    static GameMap MakeGolemMap()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Golem, 10f, 0f),
        });
        return new GameMap(MapId.HuntingGround, content: content);
    }

    static EnemyEntity GetFirstEnemy(GameMap map)
    {
        EnemyEntity? e = null;
        foreach (EnemyEntity enemy in map.Enemies.Values) { e = enemy; break; }
        Assert.NotNull(e);
        return e!;
    }

    /// <summary>
    /// S_EnemyAttack 패킷 캡처용 세션. BroadcastToAll이 owner null을 skip하므로
    /// non-null 세션으로 player를 추가해야 broadcast를 수신할 수 있음.
    /// </summary>
    sealed class CapturingSession : GameSession
    {
        public List<byte[]> Sent { get; } = new();

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            Sent.Add(copy);
        }

        protected override GameMap? GetMap() => null;
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }

    static bool IsEnemyAttackPacket(byte[] payload)
    {
        if (payload.Length < 4) return false;
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id == PacketID.S_EnemyAttack;
    }

    /// <summary>
    /// enemy를 Chase 상태로 직접 세팅하고 쿨다운을 0으로 리셋.
    /// ctor 초기 쿨다운(30틱)이 세팅되어 있어 그대로 두면 공격 트리거 안 됨.
    /// </summary>
    static void ForceChaseAndReadyAttack(EnemyEntity enemy, PlayerEntity target)
    {
        enemy.Fsm!.ChangeState(EnemyStates.Chase, enemy);
        enemy.TargetEntityId = target.EntityId;
        enemy.AttackCooldownTicks = 0;
    }

    // ── 1. 사거리 안 → 데미지 적용 (Normal) ────────────────────────────────────

    /// <summary>
    /// Normal enemy가 사거리(NormalAttackTriggerRange=1.5) 안 플레이어에게 데미지를 준다.
    ///
    /// 기대 데미지 = Formulas.ComputeDamage(NormalDefault(), Knight(), NormalBaseDamage)
    ///            = Max(1, 4 + 5 - 0) = 9.
    /// </summary>
    [Fact]
    public void Normal_InRange_DealsDamage()
    {
        GameMap map = MakeNormalMap();
        EnemyEntity enemy = GetFirstEnemy(map);

        // 사거리 안에 player 배치 (|dx|=1.0 < NormalAttackTriggerRange=1.5)
        PlayerEntity player = map.AddPlayer(null, new Vector2(enemy.X + 1.0f, 0f));
        int hpBefore = player.Hp;

        ForceChaseAndReadyAttack(enemy, player);
        map.Tick(1);

        // 공격이 발생했으면 HP가 감소해야 함
        Assert.True(player.Hp < hpBefore,
            $"Expected HP to decrease. Before={hpBefore}, After={player.Hp}");
    }

    // ── 2. 사거리 밖 → 데미지 없음 (Normal) ───────────────────────────────────

    /// <summary>
    /// Normal enemy가 사거리(NormalAttackTriggerRange=1.5) 밖 플레이어에게 데미지를 주지 않는다.
    ///
    /// |dx|=3.0 > 1.5 → 공격 트리거 미발동 → HP 불변.
    /// </summary>
    [Fact]
    public void Normal_OutOfRange_NoDamage()
    {
        GameMap map = MakeNormalMap();
        EnemyEntity enemy = GetFirstEnemy(map);

        // 사거리 밖 player (|dx|=3.0 > 1.5)
        PlayerEntity player = map.AddPlayer(null, new Vector2(enemy.X + 3.0f, 0f));
        int hpBefore = player.Hp;

        ForceChaseAndReadyAttack(enemy, player);
        // 쿨다운 0이지만 사거리 밖이라 Attack 전환 안 됨
        map.Tick(1);

        Assert.Equal(hpBefore, player.Hp);
    }

    // ── 3. 공격 시 S_EnemyAttack broadcast 발생 ─────────────────────────────────

    /// <summary>
    /// Normal enemy 공격 시 S_EnemyAttack(PacketID=20) broadcast가 발생한다.
    /// 신규 패킷 0 — 기존 S_EnemyAttack을 Normal/Golem도 재사용함을 검증.
    /// </summary>
    [Fact]
    public void Normal_InRange_BroadcastsEnemyAttackPacket()
    {
        GameMap map = MakeNormalMap();
        EnemyEntity enemy = GetFirstEnemy(map);

        var session = new CapturingSession();
        PlayerEntity player = map.AddPlayer(session, new Vector2(enemy.X + 1.0f, 0f));

        ForceChaseAndReadyAttack(enemy, player);
        map.Tick(1);

        int attackPacketCount = session.Sent.Count(IsEnemyAttackPacket);
        Assert.True(attackPacketCount >= 1,
            $"Expected at least 1 S_EnemyAttack packet, got {attackPacketCount}");
    }

    // ── 4. 무적 게이트 ────────────────────────────────────────────────────────

    /// <summary>
    /// IsInvulnerable(tick)이 true인 플레이어에게는 데미지·broadcast가 발생하지 않는다.
    /// (대쉬 i-frame 등 무적 상태 보호 — 헌법 #1 서버 판정.)
    /// </summary>
    [Fact]
    public void Normal_InvulnerablePlayer_SkipsDamageAndBroadcast()
    {
        GameMap map = MakeNormalMap();
        EnemyEntity enemy = GetFirstEnemy(map);

        var session = new CapturingSession();
        PlayerEntity player = map.AddPlayer(session, new Vector2(enemy.X + 1.0f, 0f));
        int hpBefore = player.Hp;

        // 현재 tick(=1)에 무적 세팅: InvulnUntilTick >= 1
        player.InvulnUntilTick = 100; // tick 100까지 무적

        ForceChaseAndReadyAttack(enemy, player);
        map.Tick(1);

        Assert.Equal(hpBefore, player.Hp);
        Assert.Equal(0, session.Sent.Count(IsEnemyAttackPacket));
    }

    // ── 5. 쿨다운: 1회 공격 후 재공격 없음 ──────────────────────────────────────

    /// <summary>
    /// 1회 공격 후 쿨다운 만료 직전까지 재공격 없음.
    ///
    /// 타이밍: tick=1에서 공격 발생(쿨다운 30 세팅 → EnemyAISystem이 29로 감소).
    ///   tick 2~29: 쿨다운 28→1로 감소. tick 30 직전까지 쿨다운 > 0 → 재공격 없음.
    ///   tick=30에서 쿨다운 1→0으로 감소 직후 ChaseState.Tick이 다시 공격 트리거 가능.
    ///   따라서 tick 2~28 구간(쿨다운 완전 만료 전) 진행 후 HP 변화 없음을 확인.
    /// </summary>
    [Fact]
    public void Normal_AfterAttack_CooldownPreventsReattack()
    {
        GameMap map = MakeNormalMap();
        EnemyEntity enemy = GetFirstEnemy(map);

        PlayerEntity player = map.AddPlayer(null, new Vector2(enemy.X + 1.0f, 0f));

        ForceChaseAndReadyAttack(enemy, player);
        map.Tick(1); // 첫 공격 발생 (쿨다운 30 → 29)
        int hpAfterFirstAttack = player.Hp;

        // 쿨다운 아직 > 0인 구간 (tick 2~29: 쿨다운 28~1) — 재공격 없어야 함
        for (long t = 2; t <= 29; t++)
            map.Tick(t);

        Assert.Equal(hpAfterFirstAttack, player.Hp);
    }

    // ── 6. 쿨다운 만료 후 재공격 가능 ───────────────────────────────────────────

    /// <summary>
    /// NormalAttackCooldownTicks 만료 후 플레이어가 사거리 안에 있으면 재공격한다.
    ///
    /// 시나리오: 공격(tick=1, 쿨다운→29) → 쿨다운 감소(tick 2~30) → tick=31에 쿨다운 0 → 재공격.
    /// 충분한 틱(40틱) 진행 후 HP 추가 감소 확인.
    /// </summary>
    [Fact]
    public void Normal_AfterCooldown_ReattacksInRange()
    {
        GameMap map = MakeNormalMap();
        EnemyEntity enemy = GetFirstEnemy(map);

        PlayerEntity player = map.AddPlayer(null, new Vector2(enemy.X + 1.0f, 0f));
        player.Hp = player.Stats.MaxHp; // HP 확보

        ForceChaseAndReadyAttack(enemy, player);
        map.Tick(1); // 첫 공격 발생 (쿨다운 30 세팅 → EnemyAISystem이 즉시 29로 감소)
        int hpAfterFirst = player.Hp;

        // 쿨다운 만료 충분한 틱(40틱) 진행 — 31틱 지점에서 쿨다운 0 도달, 재공격 발생
        // player X는 고정(이동 안 함) → 사거리 안 유지
        for (long t = 2; t <= 40; t++)
            map.Tick(t);

        Assert.True(player.Hp < hpAfterFirst,
            $"Expected HP to decrease again after cooldown. AfterFirst={hpAfterFirst}, After40Ticks={player.Hp}");
    }

    // ── 7. Golem 공격 — 스탯 차이 검증 ──────────────────────────────────────────

    /// <summary>
    /// Golem이 사거리 안 플레이어에게 데미지를 준다 — windup(휘두르기) 경과 후에.
    ///
    /// 기대 데미지 = Formulas.ComputeDamage(GolemDefault(), Knight(), NormalBaseDamage)
    ///            = Max(1, 4 + 8 - 0) = 12. (Golem.Defense=5는 enemy.Defense라 player→enemy 방향에만 적용.)
    ///
    /// **버그 2(M6) 회귀 안전망**: 골렘은 GolemAttackWindupTicks(6) 동안 데미지가 나오면 안 되고
    ///   windup 경과 후 타격. swing 애니가 진행되는 동안 hit이 떨어지지 않게 보장.
    /// </summary>
    [Fact]
    public void Golem_InRange_DealsDamageAfterWindup()
    {
        GameMap map = MakeGolemMap();
        EnemyEntity golem = GetFirstEnemy(map);

        PlayerEntity player = map.AddPlayer(null, new Vector2(golem.X + 1.0f, 0f));
        int hpBefore = player.Hp;

        ForceChaseAndReadyAttack(golem, player);

        // Tick 1: Chase → Attack 전환 + Enter(windup 세팅). 이 틱엔 데미지 없음.
        map.Tick(1);
        Assert.Equal(hpBefore, player.Hp);

        // windup 진행 중(Tick 2..6: windup 6→1) — 아직 타격 전, HP 불변이어야 함.
        for (long t = 2; t <= CombatConstants.GolemAttackWindupTicks; t++)
            map.Tick(t);
        Assert.Equal(hpBefore, player.Hp);

        // windup 0 도달 틱(Tick 7)에 타격 → HP 감소.
        map.Tick(CombatConstants.GolemAttackWindupTicks + 1);
        Assert.True(player.Hp < hpBefore,
            $"Expected HP to decrease after windup. Before={hpBefore}, After={player.Hp}");
    }

    // ── 8. 보스 데미지 회귀 — ApplyMeleeDamage 헬퍼 추출 후 보스 동작 불변 ────────

    /// <summary>
    /// BossStates.ApplyBossAttack이 EnemyStates.ApplyMeleeDamage 헬퍼로 교체된 후에도
    /// 보스 데미지 수식이 동일함을 검증 (헬퍼 추출 정합 증명).
    ///
    /// 기대 데미지 = Formulas.ComputeDamage(BossDefault(), Knight(), BossBaseDamage)
    ///            = Max(1, 8 + 12 - 5) = 15.
    /// </summary>
    [Fact]
    public void Boss_ApplyMeleeDamage_DamageFormula_Unchanged()
    {
        // BossRoom 맵 — boss만 포함
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 0f, 0f),
        });
        GameMap map = new GameMap(MapId.BossRoom, content: content);
        EnemyEntity boss = GetFirstEnemy(map);

        PlayerEntity player = map.AddPlayer(null, new Vector2(0f, 0f));

        int expectedDamage = Formulas.ComputeDamage(
            EnemyStats.BossDefault(), PlayerStats.Knight(), CombatConstants.BossBaseDamage);

        // ApplyBossAttack 직접 호출 (BossAttackState.Enter 경로와 동일)
        BossStates.ApplyBossAttack(map, boss);

        int actualDamage = player.Stats.MaxHp - player.Hp;
        Assert.Equal(expectedDamage, actualDamage);
    }
}
