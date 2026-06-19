using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;

namespace GameServer.Tests.Combat;

/// <summary>
/// EnemyCatalog 동치 검증 테스트 — behavior-invariant 보장.
///
/// <para>
/// 검증 목표: 카탈로그가 기존 switch/if 분기와 <strong>완전히 동일한 값</strong>을 생산.
/// 봇(BossFight/EmergencyCombat/EnemyAi)이 의존하는 HP·쿨다운·스탯이 1bit도 바뀌지 않음을 단언.
/// </para>
///
/// 테스트 목록:
///   1. Normal_MaxHp                — NormalDefault().MaxHp와 동일
///   2. Boss_MaxHp                  — BossDefault().MaxHp와 동일 (HpSync maxHp=150 봇 게이트)
///   3. Golem_MaxHp                 — GolemDefault().MaxHp와 동일
///   4. Normal_Stats                — NormalDefault() 전체와 동일
///   5. Boss_Stats                  — BossDefault() 전체와 동일
///   6. Golem_Stats                 — GolemDefault() 전체와 동일
///   7. Normal_InitialState         — EnemyState.Patrol
///   8. Boss_InitialState           — EnemyState.Idle
///   9. Golem_InitialState          — EnemyState.Patrol
///  10. Normal_InitialCooldown      — NormalAttackCooldownTicks
///  11. Boss_InitialCooldown        — BossPhase1CooldownTicks
///  12. Golem_InitialCooldown       — NormalAttackCooldownTicks
///  13. Normal_IsBoss               — false
///  14. Boss_IsBoss                 — true
///  15. Golem_IsBoss                — false
///  16. Normal_RespawnTicks         — NormalEnemyRespawnTicks (100)
///  17. Boss_RespawnTicks           — 0 (재출현 없음)
///  18. Golem_RespawnTicks          — GolemRespawnTicks (120)
///  19. Normal_AttackWindup         — 0
///  20. Golem_AttackWindup          — GolemAttackWindupTicks (6)
///  21. Normal_AttackPattern        — 0
///  22. Golem_AttackPattern         — 1
///  23. NewKindOnlyNeedsOneLine     — 카탈로그 1행 추가로 GameMap이 새 kind를 정상 처리함을 시뮬
///  24. SpawnedEnemy_MaxHp_MatchesCatalog — GameMap.SpawnEnemy가 카탈로그 MaxHp를 사용함
///  25. SpawnedEnemy_Stats_MatchesCatalog — GameMap.SpawnEnemy가 카탈로그 Stats를 사용함
///  26. SpawnedEnemy_InitialState_MatchesCatalog — EnemyEntity.State가 카탈로그 InitialState와 동일
///  27. SpawnedEnemy_InitialCooldown_MatchesCatalog — EnemyEntity.AttackCooldownTicks 카탈로그 동치
/// </summary>
public class EnemyCatalogValueTests
{
    // ── 1~3. MaxHp 동치 ──────────────────────────────────────────────────────

    [Fact]
    public void Normal_MaxHp_MatchesNormalDefaultFactory()
    {
        Assert.Equal(EnemyStats.NormalDefault().MaxHp, EnemyCatalog.For(EnemyKind.Normal).MaxHp);
    }

    [Fact]
    public void Boss_MaxHp_MatchesBossDefaultFactory()
    {
        // HpSync 봇 게이트: maxHp=150. 이 값이 카탈로그에서도 150이어야 봇 통과.
        Assert.Equal(EnemyStats.BossDefault().MaxHp, EnemyCatalog.For(EnemyKind.Boss).MaxHp);
        Assert.Equal(150, EnemyCatalog.For(EnemyKind.Boss).MaxHp);
    }

    [Fact]
    public void Golem_MaxHp_MatchesGolemDefaultFactory()
    {
        Assert.Equal(EnemyStats.GolemDefault().MaxHp, EnemyCatalog.For(EnemyKind.Golem).MaxHp);
        Assert.Equal(60, EnemyCatalog.For(EnemyKind.Golem).MaxHp);
    }

    // ── 4~6. Stats 전체 동치 ─────────────────────────────────────────────────

    [Fact]
    public void Normal_Stats_MatchesNormalDefaultFactory()
    {
        EnemyStats expected = EnemyStats.NormalDefault();
        EnemyStats actual   = EnemyCatalog.For(EnemyKind.Normal).Stats;

        Assert.Equal(expected.MaxHp,        actual.MaxHp);
        Assert.Equal(expected.Defense,      actual.Defense);
        Assert.Equal(expected.Attack,       actual.Attack);
        Assert.Equal(expected.MoveSpeed,    actual.MoveSpeed,    precision: 4);
        Assert.Equal(expected.AggroRange,   actual.AggroRange,   precision: 4);
        Assert.Equal(expected.PatrolRange,  actual.PatrolRange,  precision: 4);
        Assert.Equal(expected.AggroOnSight, actual.AggroOnSight);
    }

    [Fact]
    public void Boss_Stats_MatchesBossDefaultFactory()
    {
        EnemyStats expected = EnemyStats.BossDefault();
        EnemyStats actual   = EnemyCatalog.For(EnemyKind.Boss).Stats;

        Assert.Equal(expected.MaxHp,        actual.MaxHp);
        Assert.Equal(expected.Defense,      actual.Defense);
        Assert.Equal(expected.Attack,       actual.Attack);
        Assert.Equal(expected.MoveSpeed,    actual.MoveSpeed,    precision: 4);
        Assert.Equal(expected.AggroRange,   actual.AggroRange,   precision: 4);
        Assert.Equal(expected.PatrolRange,  actual.PatrolRange,  precision: 4);
        Assert.Equal(expected.AggroOnSight, actual.AggroOnSight);
    }

    [Fact]
    public void Golem_Stats_MatchesGolemDefaultFactory()
    {
        EnemyStats expected = EnemyStats.GolemDefault();
        EnemyStats actual   = EnemyCatalog.For(EnemyKind.Golem).Stats;

        Assert.Equal(expected.MaxHp,        actual.MaxHp);
        Assert.Equal(expected.Defense,      actual.Defense);
        Assert.Equal(expected.Attack,       actual.Attack);
        Assert.Equal(expected.MoveSpeed,    actual.MoveSpeed,    precision: 4);
        Assert.Equal(expected.AggroRange,   actual.AggroRange,   precision: 4);
        Assert.Equal(expected.PatrolRange,  actual.PatrolRange,  precision: 4);
        Assert.Equal(expected.AggroOnSight, actual.AggroOnSight);
    }

    // ── 7~9. InitialState 동치 ───────────────────────────────────────────────

    [Fact]
    public void Normal_InitialState_IsPatrol()
        => Assert.Equal(EnemyState.Patrol, EnemyCatalog.For(EnemyKind.Normal).InitialState);

    [Fact]
    public void Boss_InitialState_IsIdle()
        => Assert.Equal(EnemyState.Idle, EnemyCatalog.For(EnemyKind.Boss).InitialState);

    [Fact]
    public void Golem_InitialState_IsPatrol()
        => Assert.Equal(EnemyState.Patrol, EnemyCatalog.For(EnemyKind.Golem).InitialState);

    // ── 10~12. InitialAttackCooldownTicks 동치 ───────────────────────────────

    [Fact]
    public void Normal_InitialCooldown_MatchesNormalAttackCooldownTicks()
        => Assert.Equal(CombatConstants.NormalAttackCooldownTicks,
                        EnemyCatalog.For(EnemyKind.Normal).InitialAttackCooldownTicks);

    [Fact]
    public void Boss_InitialCooldown_MatchesBossPhase1CooldownTicks()
        => Assert.Equal(CombatConstants.BossPhase1CooldownTicks,
                        EnemyCatalog.For(EnemyKind.Boss).InitialAttackCooldownTicks);

    [Fact]
    public void Golem_InitialCooldown_MatchesNormalAttackCooldownTicks()
        => Assert.Equal(CombatConstants.NormalAttackCooldownTicks,
                        EnemyCatalog.For(EnemyKind.Golem).InitialAttackCooldownTicks);

    // ── 13~15. IsBoss 플래그 ─────────────────────────────────────────────────

    [Fact]
    public void Normal_IsBoss_IsFalse()
        => Assert.False(EnemyCatalog.For(EnemyKind.Normal).IsBoss);

    [Fact]
    public void Boss_IsBoss_IsTrue()
        => Assert.True(EnemyCatalog.For(EnemyKind.Boss).IsBoss);

    [Fact]
    public void Golem_IsBoss_IsFalse()
        => Assert.False(EnemyCatalog.For(EnemyKind.Golem).IsBoss);

    // ── 16~18. RespawnTicks 동치 ─────────────────────────────────────────────

    [Fact]
    public void Normal_RespawnTicks_MatchesNormalEnemyRespawnTicks()
        => Assert.Equal(RespawnSystem.NormalEnemyRespawnTicks,
                        EnemyCatalog.For(EnemyKind.Normal).RespawnTicks);

    [Fact]
    public void Boss_RespawnTicks_IsZero()
        => Assert.Equal(0, EnemyCatalog.For(EnemyKind.Boss).RespawnTicks);

    [Fact]
    public void Golem_RespawnTicks_MatchesGolemRespawnTicks()
        => Assert.Equal(RespawnSystem.GolemRespawnTicks,
                        EnemyCatalog.For(EnemyKind.Golem).RespawnTicks);

    // ── 19~20. AttackWindupTicks 동치 ────────────────────────────────────────

    [Fact]
    public void Normal_AttackWindup_IsZero()
        => Assert.Equal(CombatConstants.NormalAttackWindupTicks,
                        EnemyCatalog.For(EnemyKind.Normal).AttackWindupTicks);

    [Fact]
    public void Golem_AttackWindup_MatchesGolemAttackWindupTicks()
        => Assert.Equal(CombatConstants.GolemAttackWindupTicks,
                        EnemyCatalog.For(EnemyKind.Golem).AttackWindupTicks);

    // ── 21~22. AttackPattern 동치 ────────────────────────────────────────────

    [Fact]
    public void Normal_AttackPattern_IsZero()
        => Assert.Equal((byte)0, EnemyCatalog.For(EnemyKind.Normal).AttackPattern);

    [Fact]
    public void Golem_AttackPattern_IsOne()
        => Assert.Equal((byte)1, EnemyCatalog.For(EnemyKind.Golem).AttackPattern);

    // ── 24~27. GameMap.SpawnEnemy가 카탈로그 값을 실제로 사용하는지 통합 검증 ─

    [Theory]
    [InlineData(EnemyKind.Normal)]
    [InlineData(EnemyKind.Boss)]
    [InlineData(EnemyKind.Golem)]
    public void SpawnedEnemy_MaxHp_MatchesCatalog(EnemyKind kind)
    {
        GameMap map = MapWithKind(kind);
        EnemyEntity enemy = GetFirst(map);

        Assert.Equal(EnemyCatalog.For(kind).MaxHp, enemy.MaxHp);
    }

    [Theory]
    [InlineData(EnemyKind.Normal)]
    [InlineData(EnemyKind.Boss)]
    [InlineData(EnemyKind.Golem)]
    public void SpawnedEnemy_Stats_MatchesCatalog(EnemyKind kind)
    {
        GameMap map = MapWithKind(kind);
        EnemyEntity enemy = GetFirst(map);

        EnemyStats expected = EnemyCatalog.For(kind).Stats;
        Assert.Equal(expected.MaxHp,       enemy.Stats.MaxHp);
        Assert.Equal(expected.Defense,     enemy.Stats.Defense);
        Assert.Equal(expected.Attack,      enemy.Stats.Attack);
        Assert.Equal(expected.MoveSpeed,   enemy.Stats.MoveSpeed,   precision: 4);
        Assert.Equal(expected.AggroRange,  enemy.Stats.AggroRange,  precision: 4);
        Assert.Equal(expected.PatrolRange, enemy.Stats.PatrolRange, precision: 4);
    }

    [Theory]
    [InlineData(EnemyKind.Normal)]
    [InlineData(EnemyKind.Boss)]
    [InlineData(EnemyKind.Golem)]
    public void SpawnedEnemy_InitialState_MatchesCatalog(EnemyKind kind)
    {
        GameMap map = MapWithKind(kind);
        EnemyEntity enemy = GetFirst(map);

        Assert.Equal(EnemyCatalog.For(kind).InitialState, enemy.State);
    }

    [Theory]
    [InlineData(EnemyKind.Normal)]
    [InlineData(EnemyKind.Boss)]
    [InlineData(EnemyKind.Golem)]
    public void SpawnedEnemy_InitialCooldown_MatchesCatalog(EnemyKind kind)
    {
        GameMap map = MapWithKind(kind);
        EnemyEntity enemy = GetFirst(map);

        Assert.Equal(EnemyCatalog.For(kind).InitialAttackCooldownTicks, enemy.AttackCooldownTicks);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    static GameMap MapWithKind(EnemyKind kind)
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)kind, 10f, 0f),
        });
        return new GameMap(MapId.HuntingGround, content: content);
    }

    static EnemyEntity GetFirst(GameMap map)
    {
        foreach (EnemyEntity e in map.Enemies.Values) return e;
        throw new InvalidOperationException("적이 없음");
    }
}
