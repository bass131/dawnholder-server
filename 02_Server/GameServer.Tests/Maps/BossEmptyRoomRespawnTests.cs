using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// 보스 빈 방 재출현(MaybeRespawnBoss) 회귀 안전망.
///
/// 정책: 플레이어 0명 + 보스 부재일 때만 보스 재출현 + IsStageCleared 리셋.
/// 전투 중(플레이어 있음) / 보스 살아있음 / 보스 없는 맵은 재출현 X.
///
/// **픽스처**: BossStateTests / BossStageClearTests 패턴 재사용.
///   - GameMap 직접 주입(content 주입 생성자).
///   - TestGameSession.BypassHandshake()로 핸드셰이크 우회.
///   - HandleEnemyDeath 직접 호출로 보스 처치 시뮬레이션 (tick thread invariant 유지 — Tick 안에서 호출하는 대신 EnqueueJob 경유).
/// </summary>
[Collection("ConsoleSerial")]
public class BossEmptyRoomRespawnTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    const float BossX = 22f;
    const float BossY = 0f;

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

        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0); // Knight
            EnterGameWorldIfReady();
        }
    }

    public BossEmptyRoomRespawnTests()
    {
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    static MapContent MakeBossContent()
        => new MapContent(BossX, BossY, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, BossX, BossY),
        });

    static MapContent MakeNoBossContent()
        => new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 5f, 0f),
        });

    // 보스를 처치 상태로 만드는 헬퍼.
    // HandleEnemyDeath는 tick thread 안에서만 호출 가능하므로 EnqueueJob으로 마샬링.
    static void KillBossViaEnqueue(GameMap map, EnemyEntity boss)
    {
        map.EnqueueJob(() => map.HandleEnemyDeath(boss, killerEntityId: 999));
    }

    // ─── happy: 보스 처치 후 플레이어 0 + 1틱 → 보스 재출현 + flag 리셋 ──────

    [Fact]
    public void EmptyRoom_AfterBossDeath_RespawnsBossAndResetsFlag()
    {
        GameMap map = new GameMap(MapId.BossRoom, content: MakeBossContent());

        // ctor가 보스를 1마리 스폰 → id=1.
        Assert.Single(map.Enemies);
        EnemyEntity boss = map.Enemies.Values.First();
        Assert.Equal(EnemyKind.Boss, boss.Kind);

        // 현실 흐름: 보스는 플레이어가 처치 → kill 시점엔 방에 플레이어가 있음.
        // (플레이어 있는 동안은 MaybeRespawnBoss가 같은 틱 재출현을 막음 — 전투 직후 재등장 금지.)
        PlayerEntity player = map.AddPlayer(owner: null, spawnPos: new Vector2(BossX, BossY));

        // 보스 처치 (플레이어 있는 상태).
        KillBossViaEnqueue(map, boss);
        map.Tick(1); // job 소비 + HandleEnemyDeath → _stageCleared=true, boss 제거. 플레이어 있어 재출현 X.

        Assert.Empty(map.Enemies);
        Assert.True(map.IsStageCleared);

        // 플레이어 퇴장 → 빈 방.
        map.RemovePlayer(player.EntityId);

        // act: 빈 방 틱 → MaybeRespawnBoss 동작.
        map.Tick(2);

        // 보스 재출현 + flag 리셋.
        Assert.Single(map.Enemies);
        EnemyEntity newBoss = map.Enemies.Values.First();
        Assert.Equal(EnemyKind.Boss, newBoss.Kind);
        Assert.Equal(EnemyStats.BossDefault().MaxHp, newBoss.Hp);
        Assert.False(map.IsStageCleared);
    }

    // ─── edge1: 플레이어 있으면 재출현 X ──────────────────────────────────────

    [Fact]
    public void PlayerPresent_AfterBossDeath_DoesNotRespawnBoss()
    {
        GameMap map = new GameMap(MapId.BossRoom, content: MakeBossContent());
        EnemyEntity boss = map.Enemies.Values.First();

        // 플레이어 1명 추가.
        map.AddPlayer(owner: null, spawnPos: new Vector2(BossX, BossY));

        // 보스 처치.
        KillBossViaEnqueue(map, boss);
        map.Tick(1);

        Assert.Empty(map.Enemies);
        Assert.True(map.IsStageCleared);
        Assert.Single(map.Players);

        // act: 플레이어 있는 상태로 틱 → 재출현 X.
        map.Tick(2);
        map.Tick(3);

        Assert.Empty(map.Enemies);
        Assert.True(map.IsStageCleared, "플레이어 있는 동안 flag 리셋 X");
    }

    // ─── edge2: 보스 살아있으면 중복 스폰 X ──────────────────────────────────

    [Fact]
    public void BossAlive_EmptyRoom_DoesNotSpawnDuplicate()
    {
        GameMap map = new GameMap(MapId.BossRoom, content: MakeBossContent());

        // ctor 직후 보스 1마리 존재. 플레이어 0명.
        Assert.Single(map.Enemies);
        Assert.False(map.IsStageCleared);

        // 여러 틱 돌려도 중복 스폰 없음.
        map.Tick(1);
        map.Tick(2);
        map.Tick(3);

        Assert.Single(map.Enemies);
        Assert.Equal(EnemyKind.Boss, map.Enemies.Values.First().Kind);
    }

    // ─── edge3: 보스 없는 맵은 빈 방이어도 보스 안 생김 ─────────────────────

    [Fact]
    public void NoBossContent_EmptyRoom_NoBossSpawned()
    {
        GameMap map = new GameMap(MapId.HuntingGround, content: MakeNoBossContent());

        // Normal enemy 1마리만 있음.
        Assert.Single(map.Enemies);
        EnemyEntity normal = map.Enemies.Values.First();
        Assert.Equal(EnemyKind.Normal, normal.Kind);

        // Normal 처치 → 빈 상태(RespawnSystem 재출현 전 타이밍).
        KillBossViaEnqueue(map, normal);
        map.Tick(1); // HandleEnemyDeath 실행 → Normal 제거 + EnqueueRespawn

        // MaybeRespawnBoss는 _bossSpawnPoint==null 이므로 즉시 return. 보스 스폰 없음.
        map.Tick(2);
        map.Tick(3);

        // Normal은 RespawnSystem이 처리(별도 큐) — 보스(EnemyKind.Boss)는 없어야 함.
        Assert.DoesNotContain(map.Enemies.Values, e => e.Kind == EnemyKind.Boss);
    }
}
