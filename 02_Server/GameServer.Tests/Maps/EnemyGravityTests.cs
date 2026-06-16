using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// 적 수직 중력 패스(ApplyEnemyGravity) 계약 검증.
///
/// 검증 대상:
///   1. Fall_AbovePlatform_LandsOnSurface — 발판 위 공중 적이 여러 틱 후 발판 면에 착지.
///   2. OnGround_Stays_Grounded — 이미 지면 위 적은 틱 후에도 Y 불변 + OnGround=true.
///   3. Boss_Falls_WithGravity — 보스 엔티티도 동일 중력 적용(Boss kind 포함 순회 확인).
///   4. NoTerrain_FloorClamp — terrain null 맵에서 적 Y&lt;=0 이면 GroundY(=0)에 고정.
///   5. FallDespawn_Normal_RemovedAndRequeued — Normal 낙사 시 _enemies 제거 + 재출현 큐잉.
///   6. FallDespawn_Golem_RemovedAndRequeued — Golem 낙사 시 _enemies 제거 + 재출현 큐잉.
///   7. FallDespawn_Boss_RemovedNoStageClear — Boss 낙사 시 _enemies 제거 + StageClear 미발동.
///   8. FallDespawn_AboveKillPlane_NotRemoved — kill-plane 위 적은 제거되지 않음(회귀 가드).
///
/// 하니스 패턴:
///   - terrain 있는 맵: GameMap ctor에 terrain 직접 주입 (content null → 적 없음 → 수동 SpawnEnemy).
///   - terrain null 맵: GameMap() 기본 생성 + 수동 SpawnEnemy.
///   - Tick() 호출로 전체 시스템 파이프라인 구동 (ApplyEnemyGravity는 private — 통합 via Tick).
/// </summary>
public class EnemyGravityTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    // 단순 발판 하나 있는 지형. 발판 Y=2.0, 지면 solid Y=0~1.
    // 적이 공중(Y=5.0)에 두면 낙하 후 발판 면(Y=2.0)에 착지해야 함.
    static MapTerrain MakeTerrainWithPlatform()
    {
        // 바닥 solid: Y 0~1 (발판에서 내려가도 계속 떨어지지 않도록 하단에 solid 배치)
        var ground = new TerrainAabb(-50f, 0f, 50f, 1f);
        // one-way 발판: Y=3.0, X -50 ~ 50
        var platform = new TerrainPlatform(3f, -50f, 50f);
        return new MapTerrain(new[] { ground }, new[] { platform }, killPlaneY: -10f);
    }

    // terrain 있는 맵 + 지정 위치에 적 1마리 수동 스폰.
    // GameMap content=null → ctor에서 SpawnEnemy 미호출 → 테스트에서 수동 스폰.
    static (GameMap map, EnemyEntity enemy) MakeMapWithEnemy(
        EnemyKind kind, float x, float y, MapTerrain? terrain = null)
    {
        var map = new GameMap(MapId.HuntingGround, terrain: terrain);
        // SpawnEnemy는 internal — 같은 어셈블리(테스트가 GameServer.Tests 네임스페이스이므로 접근 가능).
        var enemy = map.SpawnEnemy(kind, x, y,
            kind == EnemyKind.Boss ? EnemyStats.BossDefault().MaxHp : EnemyStats.NormalDefault().MaxHp);
        return (map, enemy);
    }

    // ── 1. 발판 위 공중 → 낙하 + 착지 (happy) ────────────────────────────────

    /// <summary>
    /// 적이 발판(Y=3.0) 위 공중(Y=5.0)에 배치됐을 때,
    /// 충분한 틱 후 발판 면에 착지하고 OnGround=true 가 돼야 한다.
    /// </summary>
    [Fact]
    public void Fall_AbovePlatform_LandsOnSurface()
    {
        var terrain = MakeTerrainWithPlatform();
        var (map, enemy) = MakeMapWithEnemy(EnemyKind.Normal, x: 0f, y: 5f, terrain: terrain);

        // 초기 상태: 공중(Y=5), OnGround=false, Vy=0.
        Assert.Equal(5f, enemy.Y, precision: 3);
        Assert.False(enemy.OnGround);

        // 중력 = -20 units/s², dt = 0.05s.
        // 5.0 → 3.0 거리 = 2.0 unit 낙하. 자유낙하로 약 7~10 틱 이내 도달.
        // 충분히 여유 있게 40 틱 구동.
        for (int i = 0; i < 40; i++)
            map.Tick(i + 1);

        // 발판 면(Y=3.0) 또는 그 이상 착지 확인.
        Assert.True(enemy.Y >= 2.9f, $"enemy.Y={enemy.Y} — 발판 위 착지 기대(Y>=2.9)");
        Assert.True(enemy.OnGround, "착지 후 OnGround=true 기대");
    }

    // ── 2. 지면 위 유지 (edge) ───────────────────────────────────────────────

    /// <summary>
    /// 이미 바닥 solid 윗면(Y=1.0)에 서 있는 적은
    /// 틱 진행 후에도 Y가 내려가지 않고 OnGround=true 를 유지해야 한다.
    /// </summary>
    [Fact]
    public void OnGround_Stays_Grounded()
    {
        var terrain = MakeTerrainWithPlatform();  // 바닥 solid MaxY = 1.0
        var (map, enemy) = MakeMapWithEnemy(EnemyKind.Normal, x: 0f, y: 1f, terrain: terrain);

        // 첫 틱으로 OnGround 상태를 확정(Vy=0, Y=1.0 → StepWithTerrain이 착지 판정).
        map.Tick(1);
        float yAfterFirst = enemy.Y;

        // 추가 10 틱 — Y 감소 없어야 함.
        for (int i = 2; i <= 11; i++)
            map.Tick(i);

        Assert.True(enemy.OnGround, "지면 위 적 OnGround=true 유지 기대");
        Assert.True(enemy.Y >= yAfterFirst - 0.01f,
            $"지면 위 적 Y={enemy.Y} 감소 금지 (기준 Y={yAfterFirst})");
    }

    // ── 3. 보스도 중력 적용 ───────────────────────────────────────────────────

    /// <summary>
    /// Boss kind 엔티티도 ApplyEnemyGravity 순회에 포함된다.
    /// 보스를 공중(Y=5.0)에 두고 여러 틱 후 Y가 감소해야 한다.
    /// </summary>
    [Fact]
    public void Boss_Falls_WithGravity()
    {
        var terrain = MakeTerrainWithPlatform();
        var (map, boss) = MakeMapWithEnemy(EnemyKind.Boss, x: 0f, y: 5f, terrain: terrain);

        float initialY = boss.Y;

        // 5 틱만 구동해도 중력으로 Y가 줄어들어야 함.
        for (int i = 1; i <= 5; i++)
            map.Tick(i);

        Assert.True(boss.Y < initialY,
            $"보스 Y={boss.Y}가 초기값 {initialY}보다 작아야 함(중력 낙하)");
    }

    // ── 4. terrain null — 평지 clamp ─────────────────────────────────────────

    /// <summary>
    /// terrain null 맵(평지)에서 적을 Y=0 지면에 두면
    /// 중력 패스 후에도 Y=0 에 고정(StepFlat GroundY clamp) + OnGround=true.
    /// </summary>
    [Fact]
    public void NoTerrain_FloorClamp_KeepsEnemyAtGround()
    {
        // terrain 없는 맵 (GameMap 기본 — terrain=null).
        var (map, enemy) = MakeMapWithEnemy(EnemyKind.Normal, x: 0f, y: 0f, terrain: null);

        for (int i = 1; i <= 5; i++)
            map.Tick(i);

        // StepFlat: Y<=0 이면 newY=0, vy=0, onGround=true.
        Assert.Equal(0f, enemy.Y, precision: 3);
        Assert.True(enemy.OnGround, "평지 맵 지면 적 OnGround=true 기대");
    }

    // ── 5~8. 낙사 소멸(DespawnEnemyByFall) ────────────────────────────────────

    // 낙사 테스트 셋업 노트: solid 없는 빈 terrain은 Physics가 StepFlat으로 처리해 Y를 0으로 clamp하므로
    // (적이 killPlaneY 아래로 못 내려감) 낙사 검증이 불가. → MakeTerrainWithPlatform(solid 있음 → StepWithTerrain,
    // Y=0 clamp 없음)을 쓰고, 지면 위(Y=1)에 스폰해 SpawnY를 안전하게 둔 뒤(재출현 루프 방지),
    // enemy.Y를 killPlaneY(-10) 아래로 수동 세팅해 "발판 밖 낙하"를 시뮬한다.

    /// <summary>
    /// Normal 적이 kill-plane 아래로 낙사하면 _enemies에서 제거되고,
    /// RespawnSystem이 재출현 대기 후 새 엔티티를 _enemies에 추가한다.
    /// </summary>
    [Fact]
    public void FallDespawn_Normal_RemovedAndRespawned()
    {
        var terrain = MakeTerrainWithPlatform();  // killPlaneY=-10, ground solid → StepWithTerrain(Y=0 clamp 없음)
        var (map, enemy) = MakeMapWithEnemy(EnemyKind.Normal, x: 0f, y: 1f, terrain: terrain); // 지면 위 → SpawnY=1(재출현 안전)
        int originalId = enemy.EntityId;

        enemy.Y = -11f;  // 발판 밖 낙하 시뮬 — killPlaneY(-10) 아래.
        map.Tick(1);     // ApplyEnemyGravity → kill-plane 체크 → DespawnEnemyByFall 발동.

        Assert.False(map.Enemies.ContainsKey(originalId),
            "낙사한 Normal 적은 _enemies에서 즉시 제거돼야 한다");

        // RespawnSystem이 NormalEnemyRespawnTicks(100 틱, 5초 @20TPS) 후 재출현.
        // 재출현 후 _enemies에 새 엔티티가 등록돼야 한다.
        for (int i = 2; i <= 105; i++)
            map.Tick(i);

        Assert.True(map.Enemies.Count >= 1,
            "Normal 적 낙사 후 RespawnSystem이 재출현시켜야 한다");
    }

    /// <summary>
    /// Golem 적이 kill-plane 아래로 낙사하면 _enemies에서 제거되고,
    /// RespawnSystem이 재출현 대기 후 새 엔티티를 _enemies에 추가한다.
    /// </summary>
    [Fact]
    public void FallDespawn_Golem_RemovedAndRespawned()
    {
        var terrain = MakeTerrainWithPlatform();
        var (map, enemy) = MakeMapWithEnemy(EnemyKind.Golem, x: 0f, y: 1f, terrain: terrain);
        int originalId = enemy.EntityId;

        enemy.Y = -11f;  // killPlaneY(-10) 아래 낙하 시뮬.
        map.Tick(1);

        Assert.False(map.Enemies.ContainsKey(originalId),
            "낙사한 Golem 적은 _enemies에서 즉시 제거돼야 한다");

        // RespawnSystem이 GolemRespawnTicks(120 틱, 6초 @20TPS) 후 재출현.
        for (int i = 2; i <= 125; i++)
            map.Tick(i);

        Assert.True(map.Enemies.Count >= 1,
            "Golem 낙사 후 RespawnSystem이 재출현시켜야 한다");
    }

    /// <summary>
    /// Boss가 kill-plane 아래로 낙사해도 StageClear는 발동하지 않는다.
    /// 재출현 큐잉도 없어야 한다(보스는 MaybeRespawnBoss 별도 경로).
    /// </summary>
    [Fact]
    public void FallDespawn_Boss_RemovedNoStageClear()
    {
        var terrain = MakeTerrainWithPlatform();
        var (map, boss) = MakeMapWithEnemy(EnemyKind.Boss, x: 0f, y: 1f, terrain: terrain);
        int originalId = boss.EntityId;

        boss.Y = -11f;  // killPlaneY(-10) 아래 낙하 시뮬.
        map.Tick(1);

        Assert.False(map.Enemies.ContainsKey(originalId),
            "낙사한 Boss는 _enemies에서 즉시 제거돼야 한다");
        Assert.False(map.IsStageCleared,
            "Boss 낙사는 StageClear를 발동하면 안 된다");
    }

    /// <summary>
    /// kill-plane 위에 있는 적(Y=5)은 낙사 판정에 해당하지 않으므로 제거되지 않아야 한다.
    /// — 회귀 가드: DespawnEnemyByFall이 kill-plane 체크 없이 모든 적을 지우지 않음을 보증.
    /// </summary>
    [Fact]
    public void FallDespawn_AboveKillPlane_NotRemoved()
    {
        var terrain = MakeTerrainWithPlatform(); // killPlaneY=-10, 적은 발판(Y=3) 위
        var (map, enemy) = MakeMapWithEnemy(EnemyKind.Normal, x: 0f, y: 3f, terrain: terrain);
        int originalId = enemy.EntityId;

        // 발판 위에서 10 틱 — 낙사 없음.
        for (int i = 1; i <= 10; i++)
            map.Tick(i);

        Assert.True(map.Enemies.ContainsKey(originalId),
            "kill-plane 위 적은 낙사 소멸 대상이 아니므로 _enemies에 유지돼야 한다");
    }
}
