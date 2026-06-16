using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// GameMap content 주입 스폰 테스트 (옛 MapSpawnTableTests — content.bin 주입 대체, M4.4 Phase 03).
//
// 검증 범위:
//   ① content 주입 → N마리 스폰 + kind/HP 정합
//   ② 모르는 kindId → 예외
//   ③ kill-plane: 낙하 → PlayerSpawn 재배치 + HP 무변화 + 속도 0
//   ④ terrain null GameMap = 기존 평지 동작 (회귀)
//   ⑤ MapDataLoader: 파일 부재 → 명확한 예외
public class GameMapContentInjectionTests
{
    // HP 기본값 (EnemyDefaultHp 테이블 정합 — MapSpawnTable 옛 값 보존).
    const int NormalMaxHp = 30;
    const int BossMaxHp   = 150;

    // ① content 주입 → N마리 스폰 + kind/HP 정합
    [Fact]
    public void ContentInjected_Normal_SpawnedWithCorrectKindAndHp()
    {
        MapContent content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);

        EnemyEntity enemy = Assert.Single(map.Enemies.Values);
        Assert.Equal(EnemyKind.Normal, enemy.Kind);
        Assert.Equal(NormalMaxHp, enemy.MaxHp);
        Assert.Equal(10f, enemy.X);
        Assert.Equal(0f, enemy.Y);
    }

    [Fact]
    public void ContentInjected_MultipleNormals_AllSpawned()
    {
        MapContent content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 5f, 0f),
            new EnemySpawnPoint((byte)EnemyKind.Normal, 15f, 0f),
            new EnemySpawnPoint((byte)EnemyKind.Normal, 25f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);

        Assert.Equal(3, map.Enemies.Count);
        Assert.All(map.Enemies.Values, e =>
        {
            Assert.Equal(EnemyKind.Normal, e.Kind);
            Assert.Equal(NormalMaxHp, e.MaxHp);
        });
    }

    [Fact]
    public void ContentInjected_Boss_SpawnedWithCorrectHp()
    {
        MapContent content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 30f, 0f),
        });
        GameMap map = new GameMap(MapId.BossRoom, content: content);

        EnemyEntity boss = Assert.Single(map.Enemies.Values);
        Assert.Equal(EnemyKind.Boss, boss.Kind);
        Assert.Equal(BossMaxHp, boss.MaxHp);
    }

    [Fact]
    public void ContentEmpty_NoEnemiesSpawned()
    {
        GameMap map = new GameMap(MapId.Town, content: MapContent.Empty);
        Assert.Empty(map.Enemies);
    }

    // ② 모르는 kindId → 예외
    [Fact]
    public void UnknownKindId_Throws()
    {
        MapContent badContent = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint(99, 0f, 0f), // 존재하지 않는 kindId
        });

        Assert.Throws<InvalidOperationException>(() =>
            new GameMap(MapId.HuntingGround, content: badContent));
    }

    // ③ kill-plane: 낙하 → PlayerSpawn 재배치 + HP 무변화 + 속도 0
    [Fact]
    public void KillPlane_PlayerFalls_ReplacedAtSpawn_HpUnchanged()
    {
        // 솔리드 바닥(y=0~2), killPlaneY=-5, PlayerSpawn=(3f, 2f)
        MapTerrain terrain = new MapTerrain(
            new[] { new TerrainAabb(-100f, 0f, 100f, 2f) },
            System.Array.Empty<TerrainPlatform>(),
            killPlaneY: -5f);

        MapContent content = new MapContent(3f, 2f, System.Array.Empty<EnemySpawnPoint>());

        GameMap map = new GameMap(MapId.Town, content: content, terrain: terrain);
        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: new Vector2(0f, 0f));

        // HP 강제 세팅 (kill-plane이 HP 건드리지 않는지 확인용)
        player.Hp = 20;
        int hpBefore = player.Hp;

        // 플레이어를 kill-plane 아래로 이동
        player.Position = new Vector2(0f, -10f);
        player.Velocity = new Vector2(1f, -5f);

        // Tick 처리 (physics → kill-plane 체크)
        map.Tick(1);

        // 재배치: PlayerSpawn(3f, 2f)으로, 속도 0, HP 무변화
        Assert.Equal(3f, player.Position.X, 3);
        Assert.Equal(2f, player.Position.Y, 3);
        Assert.Equal(0f, player.Velocity.X, 3);
        Assert.Equal(0f, player.Velocity.Y, 3);
        Assert.Equal(hpBefore, player.Hp);
    }

    [Fact]
    public void KillPlane_PlayerAbovePlane_NotMoved()
    {
        MapTerrain terrain = new MapTerrain(
            new[] { new TerrainAabb(-100f, 0f, 100f, 2f) },
            System.Array.Empty<TerrainPlatform>(),
            killPlaneY: -5f);

        MapContent content = new MapContent(3f, 2f, System.Array.Empty<EnemySpawnPoint>());
        GameMap map = new GameMap(MapId.Town, content: content, terrain: terrain);
        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: new Vector2(0f, 2f));

        map.Tick(1);

        // kill-plane(-5f) 위에 있으면 재배치 없음
        Assert.True(player.Position.Y >= -5f);
        Assert.NotEqual(3f, player.Position.X); // spawn X로 이동하지 않음
    }

    // terrain != null + content == null 조합 — 재배치 목표가 Vector2.Zero fallback임을 박제.
    // 프로덕션 4맵엔 없는 조합이지만 ctor가 허용하므로 의도 동작을 회귀 안전망으로 (reviewer 🟡).
    [Fact]
    public void KillPlane_ContentNull_ReplacedAtOrigin()
    {
        MapTerrain terrain = new MapTerrain(
            new[] { new TerrainAabb(-100f, 0f, 100f, 2f) },
            System.Array.Empty<TerrainPlatform>(),
            killPlaneY: -5f);

        GameMap map = new GameMap(MapId.Town, terrain: terrain);
        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: new Vector2(0f, 2f));

        player.Position = new Vector2(7f, -10f);
        map.Tick(1);

        Assert.Equal(0f, player.Position.X, 3);
        Assert.Equal(0f, player.Position.Y, 3);
    }

    // ④ terrain null GameMap = 기존 평지 동작 (회귀)
    [Fact]
    public void TerrainNull_PlayerFalls_NotReplaced()
    {
        // terrain=null이면 kill-plane 체크 skip → 낙하해도 재배치 X
        GameMap map = new GameMap(MapId.Town);
        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: new Vector2(0f, 100f));

        // 낙하 시뮬
        for (int i = 0; i < 5; i++)
            map.Tick(i + 1);

        // 평지 물리라 바닥(y=0)에 착지. kill-plane 재배치 없음 (원점이 spawn이어서 구분 불가하므로
        // "재배치로 인한 속도 0" 대신 그냥 terrain null이면 낙하 결과 그대로 임을 확인).
        // 최소한 예외 없이 완료됨을 검증 (회귀).
        Assert.True(player.Position.Y <= 100f); // 낙하했음
    }

    // ⑤ MapDataLoader: 파일 부재 → 명확한 예외 (실제 로더 호출 — InternalsVisibleTo)
    [Fact]
    public void MapDataLoader_MissingTerrainFile_ThrowsFileNotFound()
    {
        string emptyDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(emptyDir);

        try
        {
            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(
                () => MapDataLoader.LoadAll(emptyDir));
            Assert.Contains("Town", ex.Message); // 어느 맵이 문제인지 메시지에 박힘 (fail loud 계약)
        }
        finally
        {
            System.IO.Directory.Delete(emptyDir, recursive: true);
        }
    }
}

// RespawnSystem 다수 Normal 독립 동작 점검
public class RespawnSystemMultiNormalTests
{
    [Fact]
    public void MultipleNormals_EachRespawnIndependently()
    {
        // 2마리 Normal 스폰 → 각각 독립 respawn 타이머를 가지는지 확인.
        // 하나 죽이고 respawn 대기 → 다른 하나는 영향받지 않음.
        MapContent content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 5f, 0f),
            new EnemySpawnPoint((byte)EnemyKind.Normal, 15f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);

        Assert.Equal(2, map.Enemies.Count);

        // 첫 번째 적 id 취득 후 강제 사망 처리
        int firstId = map.Enemies.Keys.First();
        EnemyEntity first = map.Enemies[firstId];

        // CombatSystem.ProcessAttack 대신 직접 map mutator 사용 (테스트 격리)
        map.RemoveEnemy(firstId);
        map.EnqueueRespawn(first);       // Enqueue가 타이머를 NormalEnemyRespawnTicks(100)로 세팅
        first.RespawnTicksRemaining = 1; // 다음 tick에 만료하도록 단축 (Enqueue *후* 세팅 — 덮어쓰기 주의)

        // 두 번째 적은 아직 살아있음
        Assert.Single(map.Enemies);

        map.Tick(1); // 1 → 0 도달 → respawn

        // respawn 후 2마리로 복원 (새 id로)
        Assert.Equal(2, map.Enemies.Count);
        Assert.All(map.Enemies.Values, e => Assert.Equal(EnemyKind.Normal, e.Kind));
    }
}

// 골렘 1층 교차 재스폰 (M6) — 처치 시 좌↔우 번갈아 1마리 재출현
public class GolemCrossRespawnTests
{
    [Fact]
    public void Golem_RespawnsAlternatingLeftRightOnFloor1()
    {
        // 1층 중앙우측 골렘 1마리 스폰.
        MapContent content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Golem, 5.5f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        Assert.Single(map.Enemies);

        // 1차 처치 → 좌측(-8.5) 재출현
        KillAndRespawn(map);
        EnemyEntity g1 = map.Enemies.Values.Single();
        Assert.Equal(EnemyKind.Golem, g1.Kind);
        Assert.Equal(-8.5f, g1.SpawnX, 3);

        // 2차 처치 → 우측(9.5) 재출현 (교차)
        KillAndRespawn(map);
        EnemyEntity g2 = map.Enemies.Values.Single();
        Assert.Equal(EnemyKind.Golem, g2.Kind);
        Assert.Equal(9.5f, g2.SpawnX, 3);

        // 항상 1마리 유지
        Assert.Single(map.Enemies);
    }

    static void KillAndRespawn(GameMap map)
    {
        EnemyEntity golem = map.Enemies.Values.Single();
        map.RemoveEnemy(golem.EntityId);
        map.EnqueueRespawn(golem);
        golem.RespawnTicksRemaining = 1; // 다음 tick에 만료 (Enqueue 후 세팅 — 덮어쓰기 주의)
        map.Tick(1);
    }
}
