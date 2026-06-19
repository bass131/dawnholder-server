using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;

namespace GameServer.Tests.Combat;

/// <summary>
/// M4.5-02 골렘 추가 검증 테스트.
///
/// 검증 대상:
///   1. GolemDefault 스탯 5개 + PatrolRange &lt; AggroRange 불변식 + EnemyDefaultHp drift 방지
///   2. 골렘 AI: 초기 State=Patrol (Idle 아님), aggro 진입 시 Chase 전환
///   3. Boss Idle 고정 회귀: 분기 정정 후에도 Boss는 Idle 유지
/// </summary>
public class GolemTests
{
    // ── 1. GolemDefault 스탯 단언 ─────────────────────────────────────────────

    [Fact]
    public void GolemDefault_Stats_AreCorrect()
    {
        EnemyStats s = EnemyStats.GolemDefault();

        Assert.Equal(60, s.MaxHp);
        Assert.Equal(5, s.Defense);
        Assert.Equal(1.2f, s.MoveSpeed, precision: 4);
        Assert.Equal(4.0f, s.AggroRange, precision: 4);
        Assert.Equal(2.5f, s.PatrolRange, precision: 4);
        Assert.True(s.AggroOnSight, "Golem should be AggroOnSight=true (선공)");
    }

    [Fact]
    public void GolemDefault_PatrolRange_LessThan_AggroRange()
    {
        EnemyStats s = EnemyStats.GolemDefault();

        Assert.True(s.PatrolRange < s.AggroRange,
            $"PatrolRange({s.PatrolRange}) must be < AggroRange({s.AggroRange})");
    }

    [Fact]
    public void EnemyDefaultHp_Golem_MatchesGolemDefaultMaxHp()
    {
        // EnemyDefaultHp.ByKind[(int)Golem] == GolemDefault().MaxHp — drift 방지
        int golemIndex = (int)EnemyKind.Golem;
        int tableHp = GameMapHpTableAccessor.GetByKind(golemIndex);
        int statsHp = EnemyStats.GolemDefault().MaxHp;

        Assert.Equal(statsHp, tableHp);
    }

    // ── 2. 골렘 AI: 초기 Patrol + Chase 전환 ──────────────────────────────────

    [Fact]
    public void Golem_InitialState_IsPatrol()
    {
        GameMap map = MakeGolemMap();
        EnemyEntity golem = GetFirstEnemy(map);

        Assert.Equal(EnemyKind.Golem, golem.Kind);
        Assert.Equal(EnemyState.Patrol, golem.State);
    }

    [Fact]
    public void Golem_AggroRange_TransitionsToChase()
    {
        GameMap map = MakeGolemMap();
        EnemyEntity golem = GetFirstEnemy(map);

        // GolemDefault AggroRange=4.0. |dx|=3 < 4 → aggro 진입
        PlayerEntity player = map.AddPlayer(null, new Vector2(golem.X + 3f, 0f));

        map.Tick(1);

        Assert.Equal(EnemyState.Chase, golem.State);
        Assert.Equal(player.EntityId, golem.TargetEntityId);
    }

    [Fact]
    public void Golem_OutsideAggroRange_StaysPatrol()
    {
        GameMap map = MakeGolemMap();
        EnemyEntity golem = GetFirstEnemy(map);

        // AggroRange=4.0, player를 5.0 거리에 배치 → 범위 밖
        map.AddPlayer(null, new Vector2(golem.X + 5f, 0f));

        map.Tick(1);

        Assert.Equal(EnemyState.Patrol, golem.State);
        Assert.Null(golem.TargetEntityId);
    }

    // ── 3. Boss Idle 고정 회귀 ────────────────────────────────────────────────

    [Fact]
    public void Boss_StaysIdle_AfterGolemBranchFix()
    {
        // Boss가 "Kind == Boss → skip AI" 분기 정정 후에도 Idle 유지임을 검증
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 30f, 0f),
        });
        GameMap map = new GameMap(MapId.BossRoom, content: content);
        EnemyEntity boss = GetFirstEnemy(map);

        Assert.Equal(EnemyKind.Boss, boss.Kind);
        Assert.Equal(EnemyState.Idle, boss.State);

        // 보스 바로 옆에 player 배치
        map.AddPlayer(null, new Vector2(boss.X + 1f, 0f));
        float bossXBefore = boss.X;

        map.Tick(1);

        Assert.Equal(EnemyState.Idle, boss.State);
        Assert.Null(boss.TargetEntityId);
        Assert.Equal(bossXBefore, boss.X);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

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
}

/// <summary>
/// EnemyDefaultHp.ByKind는 file-scoped internal — InternalsVisibleTo로 테스트에서 접근 불가.
/// GameMap ctor이 kindId 범위 검증에 EnemyDefaultHp.ByKind.Length를 쓰므로,
/// 골렘을 포함한 content를 주입해 GameMap이 throw하지 않으면 ByKind에 Golem 행이 있음을 간접 증명.
/// MaxHp는 SpawnEnemy → EnemyDefaultHp.For(kind) 경로로 EnemyEntity.MaxHp에 박힘 — 직접 읽음.
/// </summary>
file static class GameMapHpTableAccessor
{
    internal static int GetByKind(int kindIndex)
    {
        // Golem(=2) content를 주입했을 때 throw하지 않으면 배열 길이 >= 3.
        // SpawnEnemy가 EnemyDefaultHp.For(kind)로 maxHp를 결정하므로 EnemyEntity.MaxHp로 읽어냄.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)kindIndex, 0f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        int hp = 0;
        foreach (EnemyEntity e in map.Enemies.Values) { hp = e.MaxHp; break; }
        return hp;
    }
}
