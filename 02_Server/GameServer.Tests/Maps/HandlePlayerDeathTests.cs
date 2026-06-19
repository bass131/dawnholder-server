using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// GameMap.HandlePlayerDeath 명시 진입점 직접 검증 (M7.6 P04b — 사망 진입점 대칭 추출 #8).
///
/// 검증 대상 (추출 후 동작 불변 — EnemyStates.ApplyMeleeDamage 인라인 블록과 비트 동일):
///   1. spawn 재배치       — Position == PlayerSpawnPosition
///   2. 풀피 부활          — Hp == Stats.MaxHp
///   3. 물리 상태 리셋     — Velocity == Zero + OnGround == false
///   4. Revive 효과        — ActionFsm.CurrentState == Idle (DeathState 아님)
///   5. HUD HP 송신        — S_PlayerHp(currentHp == maxHp) 1:1 통지
///
/// 테스트 전략:
///   - HandleEnemyDeathKillerTests 패턴 미러 (EnqueueJob 마샬링 → Tick 소비, ConsoleSerial, IDisposable).
///   - 직접 map.HandlePlayerDeath(player) 호출을 EnqueueJob으로 tick thread에 마샬 → Tick으로 소비.
///   - FakeCapturingSession으로 S_PlayerHp broadcast 수집.
///   - BossBehaviorTests:23(BossAttack_PlayerDies_Respawns)의 ActionFsm/spawn 단언 방식 정합.
/// </summary>
[Collection("ConsoleSerial")]
public class HandlePlayerDeathTests : IDisposable
{
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    public HandlePlayerDeathTests()
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

    // ── FakeCapturingSession ───────────────────────────────────────────────────
    // broadcast/1:1 송신을 수집해 S_PlayerHp 통지 검증.
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

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    // BossRoom: PlayerSpawnPosition = (22, 0). 적 없이 플레이어 사망 진입점만 검증.
    (GameMap map, PlayerEntity player, List<byte[]> sink) MakeMapWithPlayer()
    {
        var content = new MapContent(22f, 0f, Array.Empty<EnemySpawnPoint>());
        var map = new GameMap(MapId.BossRoom, content: content);
        var sink = new List<byte[]>();
        // spawn과 다른 위치에 배치 → HandlePlayerDeath가 spawn으로 재배치하는지 검증 가능.
        PlayerEntity player = map.AddPlayer(new FakeCapturingSession(sink), new Vector2(50f, 5f));
        return (map, player, sink);
    }

    [Fact]
    public void HandlePlayerDeath_RepositionsToSpawn()
    {
        var (map, player, _) = MakeMapWithPlayer();
        // 사망 직전 상태 시뮬: HP 0 이하 + spawn 아닌 위치 + 낙하 중.
        player.Hp = 0;
        player.Velocity = new Vector2(3f, -8f);
        player.OnGround = true;

        map.EnqueueJob(() => map.HandlePlayerDeath(player));
        map.Tick(1);

        Assert.Equal(map.PlayerSpawnPosition, player.Position);
    }

    [Fact]
    public void HandlePlayerDeath_RevivesFullHp()
    {
        var (map, player, _) = MakeMapWithPlayer();
        player.Hp = -10; // 음수 HP(과사망)에도 풀피 부활.

        map.EnqueueJob(() => map.HandlePlayerDeath(player));
        map.Tick(1);

        Assert.Equal(player.Stats.MaxHp, player.Hp);
    }

    [Fact]
    public void HandlePlayerDeath_ResetsVelocityAndGround()
    {
        var (map, player, _) = MakeMapWithPlayer();
        player.Hp = 0;
        player.Velocity = new Vector2(3f, -8f);
        player.OnGround = true;

        // Velocity/OnGround는 HandlePlayerDeath가 세팅하지만 *물리 스텝이 소유*하는 transient 상태 —
        // GameMap.Tick의 Physics.Step(step 2)이 같은 틱에서 grounded 스폰을 재판정해 OnGround를 덮음.
        // 실제 게임은 ApplyMeleeDamage가 물리 *후*(EnemyAISystem, step 4) 호출되므로 동작 불변이나,
        // 본 테스트는 EnqueueJob(step 1, 물리 전) 마샬이라 진입점의 직접 출력을 물리가 덮기 전 캡처해야 충실.
        Vector2 capturedVel = new Vector2(float.NaN, float.NaN);
        bool capturedGround = true;
        map.EnqueueJob(() =>
        {
            map.HandlePlayerDeath(player);
            capturedVel = player.Velocity;
            capturedGround = player.OnGround;
        });
        map.Tick(1);

        Assert.Equal(Vector2.Zero, capturedVel);
        Assert.False(capturedGround);
    }

    [Fact]
    public void HandlePlayerDeath_RevivesToIdleState()
    {
        // Revive() → ActionFsm.ChangeState(Idle). DeathState로 남으면 안 됨 (사망 잠금 해제).
        var (map, player, _) = MakeMapWithPlayer();
        player.Hp = 0;

        map.EnqueueJob(() => map.HandlePlayerDeath(player));
        map.Tick(1);

        Assert.Same(PlayerMovementStates.Idle, player.ActionFsm.CurrentState);
        Assert.False(player.ActionFsm.CurrentState is DeathState,
            "부활 후 ActionFsm이 DeathState면 안 됨 — Revive()로 Idle 복귀");
    }

    [Fact]
    public void HandlePlayerDeath_SendsFullHpToHud()
    {
        // 부활 시 S_PlayerHp(currentHp == maxHp) 1:1 송신 — HUD HP 바 복구 통지.
        var (map, player, sink) = MakeMapWithPlayer();
        player.Hp = 0;
        sink.Clear();

        map.EnqueueJob(() => map.HandlePlayerDeath(player));
        map.Tick(1);

        byte[]? raw = sink.LastOrDefault(p => PacketIdOf(p) == PacketID.S_PlayerHp);
        Assert.NotNull(raw);
        S_PlayerHp pkt = new S_PlayerHp();
        pkt.Read(new ArraySegment<byte>(raw!));
        Assert.Equal(player.EntityId, pkt.entityId);
        Assert.Equal(player.MaxHp, pkt.currentHp);
        Assert.Equal(player.MaxHp, pkt.maxHp);
    }
}
