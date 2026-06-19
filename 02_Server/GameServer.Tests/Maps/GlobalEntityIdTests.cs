using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 전역 entity id 풀 단위 테스트 (ADR-026 핵심).
//
// **검증 범위** (ADR-026 정합):
//   - GameWorld.NextEntityId()가 단조 증가하며 globally-unique id 발급
//   - 4맵 생성 후 AddPlayer가 전역 발급기에서 id 받음 (맵별 1 재시작 X)
//   - SpawnEnemy도 전역 발급기에서 id 받음
//   - 로컬 모드(idAllocator=null): 단독 GameMap 생성 시 1부터 시작 (테스트 격리 보존)
//
// **싱글톤 관리**: GameWorld는 싱글톤 — [Collection("GlobalEntityIdTests")]으로 순차 실행.
// Stop() 호출 시 Instance = null 해제 → 다음 테스트 GameWorld 생성 가능.
[Collection("GlobalEntityIdTests")]
public class GlobalEntityIdTests : IDisposable
{
    readonly GameWorld _world;

    public GlobalEntityIdTests()
    {
        // HuntingGround Normal + BossRoom Boss content 주입 (옛 MapSpawnTable 값 보존).
        // 생성 순서 결정론: Town→HG→BR→Ending → HG Normal=id1, BR Boss=id2.
        var provider = new Dictionary<MapId, (MapTerrain? Terrain, MapContent? Content)>
        {
            [MapId.Town]          = (null, MapContent.Empty),
            [MapId.HuntingGround] = (null, new MapContent(0f, 0f, new[] { new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f) })),
            [MapId.BossRoom]      = (null, new MapContent(0f, 0f, new[] { new EnemySpawnPoint((byte)EnemyKind.Boss,   30f, 0f) })),
            [MapId.Ending]        = (null, MapContent.Empty),
        };
        _world = new GameWorld(provider);
    }

    public void Dispose()
    {
        _world.Stop(); // Instance = null 해제
    }

    // --- GameWorld.NextEntityId 단조 증가 ---

    [Fact]
    public void NextEntityId_IsMonotonicallyIncreasing()
    {
        // NextEntityId()를 연속 호출하면 항상 이전 값보다 큰 값이 나와야 함.
        // Interlocked.Increment(ref _nextEntityId) = post-increment → 1, 2, 3, ...
        int a = _world.NextEntityId();
        int b = _world.NextEntityId();
        int c = _world.NextEntityId();

        Assert.True(a < b, $"a={a} must be < b={b}");
        Assert.True(b < c, $"b={b} must be < c={c}");
    }

    [Fact]
    public void NextEntityId_NeverReturns_Zero()
    {
        // Interlocked.Increment(ref _nextEntityId) where _nextEntityId starts at 0
        // → 첫 호출 = 1 (0+1). 0은 "미배정" 센티널 값 — id로 사용하면 lookup 혼란.
        int first = _world.NextEntityId();
        Assert.True(first > 0, $"first id must be > 0, got {first}");
    }

    // --- 전역 풀: 맵을 넘나들어도 unique ---

    [Fact]
    public void AllMaps_EnemyIds_AreGloballyUnique()
    {
        // GameWorld ctor에서 4맵 생성 시 enemy가 전역 풀에서 id 받음.
        // HuntingGround Normal=id??, BossRoom Boss=id?? — 서로 다른 id 보장.
        //
        // 구체적 기대값: Town(enemy 0개) → HuntingGround Normal = 1, BossRoom Boss = 2.
        // (GameWorld ctor 주석 "생성 순서 결정론적 고정" 참조)
        GameMap hg = _world.GetMap(MapId.HuntingGround)!;
        GameMap br = _world.GetMap(MapId.BossRoom)!;

        int normalId = hg.Enemies.Keys.Single();   // Normal enemy id
        int bossId = br.Enemies.Keys.Single();     // Boss enemy id

        // globally-unique: 두 맵의 enemy id가 달라야 함.
        Assert.NotEqual(normalId, bossId);

        // 단조 증가 순서: HuntingGround가 먼저 생성 → Normal id < Boss id.
        Assert.True(normalId < bossId,
            $"Normal(id={normalId}) must be spawned before Boss(id={bossId}) — creation order guarantee");
    }

    [Fact]
    public void AddPlayer_Town_Gets_GlobalId_AfterEnemies()
    {
        // GameWorld 4맵 생성 후 Town.AddPlayer() 호출 → 전역 풀에서 id 받음.
        // enemy들(HuntingGround Normal=1, BossRoom Boss=2) 이후 발급 → player id > 2.
        //
        // Town 맵은 tick thread를 직접 사용하지 않고 직접 AddPlayer (테스트 직접 접근).
        GameMap town = _world.GetMap(MapId.Town)!;
        PlayerEntity player = town.AddPlayer(owner: null, spawnPos: Vector2.Zero);

        // HuntingGround Normal=1, BossRoom Boss=2 이후 → player=3.
        // 만약 Town에 enemy가 없고 Ending에도 없으면 첫 플레이어 id = 3.
        Assert.Equal(3, player.EntityId);
    }

    [Fact]
    public void AddPlayer_TwoMaps_NoDuplicateId()
    {
        // 서로 다른 맵에 AddPlayer → id가 겹치지 않아야 함.
        GameMap town = _world.GetMap(MapId.Town)!;
        GameMap ending = _world.GetMap(MapId.Ending)!;

        PlayerEntity p1 = town.AddPlayer(owner: null, spawnPos: Vector2.Zero);
        PlayerEntity p2 = ending.AddPlayer(owner: null, spawnPos: Vector2.Zero);

        Assert.NotEqual(p1.EntityId, p2.EntityId);
        // 단조 증가: 먼저 추가한 p1 < p2.
        Assert.True(p1.EntityId < p2.EntityId,
            $"p1.EntityId={p1.EntityId} must be < p2.EntityId={p2.EntityId}");
    }

    [Fact]
    public void SpawnEnemy_AfterAddPlayer_GetsNextGlobalId()
    {
        // AddPlayer 후 SpawnEnemy도 같은 전역 풀에서 id 받음.
        // → entity id 공간에서 player와 enemy가 섞여도 id 충돌 없음.
        GameMap town = _world.GetMap(MapId.Town)!;
        GameMap hg   = _world.GetMap(MapId.HuntingGround)!;

        int normalEnemyId = hg.Enemies.Keys.Single(); // 전역 풀에서 이미 발급된 id

        PlayerEntity player = town.AddPlayer(owner: null, spawnPos: Vector2.Zero);

        // SpawnEnemy를 Town에 직접 추가 (InternalsVisibleTo 허용).
        EnemyEntity extra = town.SpawnEnemy(
            EnemyKind.Normal, x: 0f, y: 0f, maxHp: 10);

        // player id ≠ normalEnemyId ≠ extra id — 전역 풀 unique 보장.
        Assert.NotEqual(player.EntityId, normalEnemyId);
        Assert.NotEqual(extra.EntityId, normalEnemyId);
        Assert.NotEqual(extra.EntityId, player.EntityId);
    }
}

// --- 로컬 모드 테스트 (GameWorld 불필요 — 독립 실행) ---

// GameWorld singleton 없이 독립 생성 GameMap의 로컬 카운터 동작 검증.
// 이 테스트들은 기존 AttackHandlerTests/BossStageClearTests가 의존하는
// "단독 GameMap = id 1부터" 동작이 유지됨을 회귀 안전망으로 확인.
public class LocalEntityIdTests
{
    static MapContent NormalContent() => new MapContent(0f, 0f, new[]
    {
        new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
    });

    [Fact]
    public void StandaloneGameMap_Enemy_StartsAtOne()
    {
        // content 주입 GameMap (idAllocator=null) → 로컬 카운터 1부터.
        // AttackHandlerTests.EnemyEntityId=1 기대값의 근거.
        GameMap map = new GameMap(MapId.HuntingGround, content: NormalContent());
        EnemyEntity enemy = map.Enemies.Values.Single();
        Assert.Equal(1, enemy.EntityId);
    }

    [Fact]
    public void StandaloneGameMap_AddPlayer_AfterEnemy_Gets_IncrementalId()
    {
        // HuntingGround Normal(id=1) + Boss 수동 spawn(id=2) 후 AddPlayer → id=3.
        // AttackHandlerTests.PlayerEntityId=3 기대값의 근거.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        // id=1 (Normal, ctor에서 content 주입 spawn)

        map.SpawnEnemy(EnemyKind.Boss, 30f, 0f, 100);
        // id=2 (Boss, 수동 spawn)

        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: Vector2.Zero);
        // id=3 (player, 로컬 카운터)

        Assert.Equal(3, player.EntityId);
    }

    [Fact]
    public void CustomAllocator_Used_When_Injected()
    {
        // idAllocator 주입 시 로컬 카운터 무시 + 주입된 함수 호출됨 검증.
        // GameWorld가 NextEntityId를 주입하는 것과 같은 메커니즘.
        int counter = 100; // 100부터 시작하는 커스텀 발급기
        Func<int> allocator = () => ++counter;

        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, allocator, content: content);

        // content 주입 Normal enemy 1마리 spawn → allocator() 1회 호출 → id=101.
        EnemyEntity enemy = map.Enemies.Values.Single();
        Assert.Equal(101, enemy.EntityId);

        // AddPlayer 추가 → allocator() 또 호출 → id=102.
        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: Vector2.Zero);
        Assert.Equal(102, player.EntityId);
    }
}

// xUnit Collection: GameWorld 싱글톤 1개 허용 — 순차 실행.
[CollectionDefinition("GlobalEntityIdTests", DisableParallelization = true)]
public class GlobalEntityIdTestsCollection { }
