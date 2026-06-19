using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// M7.7 P4a — GameMap → MapPacketPublisher 추출의 **byte 동치(wire-byte parity) 회귀 가드**.
///
/// **왜 byte 동치인가** (헌법 §2 Protocol is Sacred):
///   publisher 추출은 "순수 리팩토링" — 패킷 ID·필드·순서·수신자·송신 시점이 1bit도 안 바뀌어야 한다.
///   wire byte가 1바이트라도 달라지면 클라/서버 desync = 프로덕션 최악 사례.
///   본 테스트는 publisher가 조립·송신한 byte[]가 *독립적으로 손조립한 기댓값 패킷*의 byte[]와
///   정확히 일치함을 고정 → 추출/이후 리팩토링이 wire를 깨면 commit 시점 검출.
///
/// **테스트 축**:
///   ① 조립 동치: publisher 출력 byte == 동일 필드 독립 조립 byte (필드 매핑 불변)
///   ② 송신 경로: BroadcastToAll(전원) vs Session.Send(1:1) 수신자 집합·순서 보존
///   ③ end-to-end: GameMap.Tick / SendInitialRosterTo 실경로 캡처 == 기댓값 (위임 wiring 회귀)
/// </summary>
public class MapPacketPublisherByteParityTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static GameMap MakeTownMap() => new GameMap(MapId.Town);

    static byte[] Capture(ArraySegment<byte> seg)
    {
        byte[] copy = new byte[seg.Count];
        Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
        return copy;
    }

    static PacketID PacketIdOf(byte[] payload)
        => (PacketID)(ushort)(payload[2] | (payload[3] << 8));

    // ── ① BroadcastSnapshots: 조립 + broadcast byte 동치 ──────────────────────

    /// <summary>
    /// publisher가 broadcast한 S_Snapshot byte[]가, 동일 player 상태로 독립 조립한 S_Snapshot.Write()와
    /// 정확히 일치. 추출 전 GameMap 인라인 조립과 같은 필드 매핑임을 byte 단위로 고정.
    /// </summary>
    [Fact]
    public void BroadcastSnapshots_ProducesIdenticalBytes_ToHandAssembledPacket()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);

        // 결정론적 상태 박제 — Physics.Step 비결정 영향 제거 위해 Tick 호출 없이 publisher 직접 호출.
        PlayerEntity p = map.AddPlayer(session, new Vector2(12.5f, -3.25f));
        p.Velocity = new Vector2(4.0f, -1.5f);
        p.LastClientTick = 77u;

        // S_Snapshot은 SnapshotTickInterval 마다 발송 — interval과 정합하는 tick 번호 사용.
        long tick = Constants.SnapshotTickInterval; // % == 0 보장

        new MapPacketPublisher(map).BroadcastSnapshots(tick);

        byte[] actual = Assert.Single(sink.Where(b => PacketIdOf(b) == PacketID.S_Snapshot));

        // 기댓값: 동일 필드를 독립 조립 (publisher와 같은 매핑이어야 byte 일치).
        var expected = new S_Snapshot
        {
            entityId = p.EntityId,
            x = p.Position.X,
            y = p.Position.Y,
            vx = p.Velocity.X,
            vy = p.Velocity.Y,
            serverTick = (int)tick,
            lastAckedClientTick = p.LastClientTick,
            animState = (byte)p.ActionFsm.AnimState,
        };
        byte[] expectedBytes = Capture(expected.Write());

        Assert.Equal(expectedBytes, actual);
    }

    /// <summary>
    /// player N명이면 N개 snapshot이 per-entity로 발송 — 발송 구조(1 player = 1 패킷) 보존 확인.
    /// </summary>
    [Fact]
    public void BroadcastSnapshots_EmitsOnePacketPerPlayer()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);

        map.AddPlayer(session, new Vector2(0f, 0f));
        map.AddPlayer(session, new Vector2(5f, 0f));
        map.AddPlayer(session, new Vector2(-5f, 0f));

        new MapPacketPublisher(map).BroadcastSnapshots(Constants.SnapshotTickInterval);

        // 세 player가 같은 session을 공유 → BroadcastToAll이 entity별 snapshot을 각 player
        //   (=동일 session)에게 발송하므로 sink엔 N×N=9개가 쌓인다(원본 GameMap.Tick과 동일 패턴).
        //   검증 의도는 "player당 1 snapshot이 per-entity로 조립"이므로 distinct entityId 수로 확인.
        int distinctPlayers = sink
            .Where(b => PacketIdOf(b) == PacketID.S_Snapshot)
            .Select(b => { var s = new S_Snapshot(); s.Read(new ArraySegment<byte>(b)); return s.entityId; })
            .Distinct().Count();
        Assert.Equal(3, distinctPlayers);
    }

    // ── ② SendPlayerHp: 1:1 송신 byte 동치 + floor 보존 ───────────────────────

    [Fact]
    public void SendPlayerHp_ProducesIdenticalBytes_ToHandAssembledPacket()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);

        PlayerEntity p = map.AddPlayer(session, Vector2.Zero);
        p.Hp = 42;

        new MapPacketPublisher(map).SendPlayerHp(p);

        byte[] actual = Assert.Single(sink);

        var expected = new S_PlayerHp
        {
            entityId = p.EntityId,
            currentHp = Math.Max(0, p.Hp),
            maxHp = p.MaxHp,
        };
        Assert.Equal(Capture(expected.Write()), actual);
    }

    /// <summary>
    /// Hp 음수여도 currentHp는 Math.Max(0, Hp) floor — 추출 전 음수 방어 로직 보존.
    /// </summary>
    [Fact]
    public void SendPlayerHp_FloorsNegativeHpToZero()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);

        PlayerEntity p = map.AddPlayer(session, Vector2.Zero);
        p.Hp = -10;

        new MapPacketPublisher(map).SendPlayerHp(p);

        byte[] actual = Assert.Single(sink);
        var decoded = new S_PlayerHp();
        decoded.Read(new ArraySegment<byte>(actual));
        Assert.Equal(0, decoded.currentHp);
    }

    // ── ③ BroadcastEntityDeath / BroadcastStageClear byte 동치 ────────────────

    [Fact]
    public void BroadcastEntityDeath_ProducesIdenticalBytes()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        map.AddPlayer(session, Vector2.Zero);

        new MapPacketPublisher(map).BroadcastEntityDeath(99);

        byte[] actual = Assert.Single(sink);
        var expected = new S_EntityDeath { entityId = 99 };
        Assert.Equal(Capture(expected.Write()), actual);
    }

    [Fact]
    public void BroadcastStageClear_ProducesIdenticalBytes()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        map.AddPlayer(session, Vector2.Zero);

        new MapPacketPublisher(map).BroadcastStageClear(7);

        byte[] actual = Assert.Single(sink);
        var expected = new S_StageClear { bossEntityId = 7 };
        Assert.Equal(Capture(expected.Write()), actual);
    }

    // ── ④ SendInitialRoster: S_PlayerJoin + S_EntitySpawn 발송 순서·byte 동치 ──

    /// <summary>
    /// roster 발송: 기존 player(S_PlayerJoin) 먼저, 그 다음 살아있는 enemy(S_EntitySpawn).
    /// 발송 *순서*와 각 패킷 byte를 동시에 고정 — 추출이 순서/필드를 안 바꿈을 보장.
    /// </summary>
    [Fact]
    public void SendInitialRoster_PreservesOrderAndBytes()
    {
        // HuntingGround content로 Normal enemy 1마리 자동 스폰.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);

        // 기존 player 1명 (roster에 S_PlayerJoin으로 포함). null이 아닌 owner 필요.
        var existingSink = new List<byte[]>();
        var existingSession = new FakeCapturingSession(existingSink);
        PlayerEntity existing = map.AddPlayer(existingSession, new Vector2(3.0f, -1.5f));

        // 신규 진입 세션 — 이쪽이 roster를 1:1로 받음.
        var targetSink = new List<byte[]>();
        var targetSession = new FakeCapturingSession(targetSink);

        map.SendInitialRosterTo(targetSession, new List<PlayerEntity> { existing });

        // 발송 순서: [S_PlayerJoin, S_EntitySpawn].
        Assert.Equal(2, targetSink.Count);
        Assert.Equal(PacketID.S_PlayerJoin, PacketIdOf(targetSink[0]));
        Assert.Equal(PacketID.S_EntitySpawn, PacketIdOf(targetSink[1]));

        // S_PlayerJoin byte 동치.
        var expectedJoin = new S_PlayerJoin
        {
            entityId = existing.EntityId,
            spawnX = existing.Position.X,
            spawnY = existing.Position.Y,
            characterClass = (byte)existing.Stats.Class,
        };
        Assert.Equal(Capture(expectedJoin.Write()), targetSink[0]);

        // S_EntitySpawn byte 동치 (스폰된 Normal enemy).
        EnemyEntity enemy = map.Enemies.Values.Single();
        var expectedSpawn = new S_EntitySpawn
        {
            entityId = enemy.EntityId,
            entityKind = (byte)enemy.Kind,
            x = enemy.X,
            y = enemy.Y,
            currentHp = enemy.Hp,
            maxHp = enemy.MaxHp,
        };
        Assert.Equal(Capture(expectedSpawn.Write()), targetSink[1]);
    }

    /// <summary>
    /// 죽은(IsDead) enemy는 roster에서 skip — 추출 전 `if (enemy.IsDead) continue;` 보존.
    /// </summary>
    [Fact]
    public void SendInitialRoster_SkipsDeadEnemies()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);

        EnemyEntity enemy = map.Enemies.Values.Single();
        enemy.Hp = 0; // IsDead

        var targetSink = new List<byte[]>();
        var targetSession = new FakeCapturingSession(targetSink);

        map.SendInitialRosterTo(targetSession, new List<PlayerEntity>());

        // 죽은 enemy 1마리뿐 + player 0 → 아무것도 안 보냄.
        Assert.Empty(targetSink);
    }

    // ── ⑤ end-to-end: GameMap.Tick 실경로 snapshot byte 회귀 가드 ─────────────

    /// <summary>
    /// 실제 GameMap.Tick(위임 wiring 포함)을 돌려 캡처한 S_Snapshot byte[]가,
    /// 같은 최종 상태로 독립 조립한 기댓값과 일치. 위임 경로(_publisher.BroadcastSnapshots)가
    /// 같은 byte를 같은 수신자에게 보냄을 end-to-end로 고정.
    /// </summary>
    [Fact]
    public void Tick_DelegatesSnapshotBroadcast_WithIdenticalBytes()
    {
        GameMap map = MakeTownMap();
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);

        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.Velocity = Vector2.Zero;
        p.OnGround = true;

        // SnapshotTickInterval과 정합하는 tick에서 broadcast 발생.
        long tick = Constants.SnapshotTickInterval;
        map.Tick(tick);

        byte[] actual = Assert.Single(sink.Where(b => PacketIdOf(b) == PacketID.S_Snapshot));

        // Tick 후 p의 *최종* 상태로 기댓값 조립 (physics가 mutate한 위치/속도 반영).
        var expected = new S_Snapshot
        {
            entityId = p.EntityId,
            x = p.Position.X,
            y = p.Position.Y,
            vx = p.Velocity.X,
            vy = p.Velocity.Y,
            serverTick = (int)tick,
            lastAckedClientTick = p.LastClientTick,
            animState = (byte)p.ActionFsm.AnimState,
        };
        Assert.Equal(Capture(expected.Write()), actual);
    }

    // ── FakeCapturingSession (EnemyAiTests/AnimStateTests 패턴 정합) ──────────

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
