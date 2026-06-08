using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

/// <summary>
/// 맵 간 player migration 단위 테스트.
///
/// **검증 invariant** (ADR-026 정합):
///   1. entity id 유지 — 맵 이동 후에도 entity id 동일 (ADR-026 핵심)
///   2. state 보존 — HP / PlayerStats(class) 맵 이동 후 유지
///   3. 맵 A에서 제거 — migration 후 맵 A.Players에 없음
///   4. 맵 B에 추가 — migration 후 맵 B.Players에 있음
///   5. S_PlayerLeave broadcast — 맵 A의 다른 플레이어가 receive
///   6. S_PlayerJoin broadcast — 맵 B의 기존 플레이어가 receive
///   7. S_MapTransition — 이동한 본인이 받음 (destMapId, spawnX, spawnY)
///   8. 왕복 state 보존 — A→B→A 후 HP/stats/entityId 동일
///   9. transient drop — migration 중(GetMap null) 도착한 패킷 no-op
///  10. S_PlayerLeave entityId 정합 — 떠난 플레이어의 id로 broadcast
///
/// **테스트 전략**:
///   - TestMigrationSession: GetMap(current) + GetDestMap(dest) override로 GameWorld 없이 두 맵 주입
///   - portal 근접 조건: player 위치를 portal 좌표(±1.5unit) 안으로 수동 세팅
///   - Town portal: portalId=1, position x=20, dest=HuntingGround, destSpawn (2,0)
///   - HuntingGround portal: portalId=1, position x=25, dest=BossRoom, destSpawn (22,0)
/// </summary>
[Collection("ConsoleSerial")]
public class MapMigrationTests : IDisposable
{
    readonly GameMap _mapA; // Town
    readonly GameMap _mapB; // HuntingGround
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // Town portal: portalId=1, position (20, 0), dest=HuntingGround, destSpawn (2, 0)
    const int PortalId = 1;
    static readonly Vector2 TownPortalPos = new Vector2(20f, 0f);   // Town portal 위치
    static readonly Vector2 NearTownPortal = new Vector2(20f - 1.5f, 0f); // 근처 (1.5 unit 안)

    // HuntingGround portal: portalId=1, position (25, 0)
    static readonly Vector2 NearHgPortal = new Vector2(25f - 1.5f, 0f);

    // --- nested session 클래스들 ---

    // disconnect-during-migration 결정론적 재현용 세션.
    // GetDestMap override에서 OnDisconnected를 *동기적으로* 호출 —
    // "맵 A RemovePlayer 완료 직후, 맵 B EnqueueJob 직전" 시점을 정확히 포착.
    // 결과: destMap 람다 실행 시 _closing=1 → AddPlayerWithId skip → ghost 없음.
    class DisconnectOnGetDestSession : GameSession
    {
        GameMap _currentMap;
        readonly GameMap _destMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public DisconnectOnGetDestSession(GameMap currentMap, GameMap destMap)
        {
            _currentMap = currentMap;
            _destMap = destMap;
        }

        protected override GameMap? GetMap() => _currentMap;
        protected override GameMap? GetDestMap(MapId destMapId)
        {
            // tick thread에서 호출되는 이 시점 = 맵 A RemovePlayer 완료 직후.
            // OnDisconnected를 직접 호출해 _closing=1로 세팅.
            // socket thread가 아닌 tick thread에서 호출하지만, 테스트는 단일 스레드 —
            // Interlocked.Exchange가 동기적으로 1을 박으므로 결정론적.
            OnDisconnected(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
            return _destMap;
        }

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0);
            EnterGameWorldIfReady();
        }
    }

    // 두 맵 모두 주입받는 migration 전용 TestGameSession
    class TestMigrationSession : GameSession
    {
        GameMap _currentMap;
        GameMap _destMapOverride;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestMigrationSession(GameMap currentMap, GameMap destMap)
        {
            _currentMap = currentMap;
            _destMapOverride = destMap;
        }

        protected override GameMap? GetMap() => _currentMap;
        protected override GameMap? GetDestMap(MapId destMapId) => _destMapOverride;

        // 왕복 테스트에서 현재 맵/목적지 맵 교체용
        public void SetCurrentMap(GameMap map) => _currentMap = map;
        public void SetDestMap(GameMap map) => _destMapOverride = map;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0); // Knight
            EnterGameWorldIfReady();
        }
    }

    // 관찰자 세션 (맵의 다른 플레이어 — broadcast 검증용)
    class ObserverSession : GameSession
    {
        readonly GameMap _map;
        public List<byte[]> SentPackets { get; } = new();

        public ObserverSession(GameMap map) { _map = map; }
        protected override GameMap? GetMap() => _map;
        protected override GameMap? GetDestMap(MapId _) => null;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }

        // OnConnected에서 자동 handshake + class 선택 → EnterGameWorld
        public override void OnConnected(EndPoint endPoint)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0);
            EnterGameWorldIfReady();
        }
    }

    // migration 중 상태 시뮬 전용 세션 (GetMap null 제어)
    class TransientTestSession : GameSession
    {
        readonly GameMap _map;
        public List<byte[]> SentPackets { get; } = new();
        bool _forceNullMap;

        public TransientTestSession(GameMap map) { _map = map; }
        protected override GameMap? GetMap() => _forceNullMap ? null : _map;
        protected override GameMap? GetDestMap(MapId _) => null;

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
            SetCharacterClass(0);
            EnterGameWorldIfReady();
        }

        // migration 중 시뮬: GetMap() null 반환
        public void SimulateMigrating() => _forceNullMap = true;
        public void SimulateMigrationComplete() => _forceNullMap = false;
    }

    // --- 픽스처 ---

    public MapMigrationTests()
    {
        _mapA = new GameMap(MapId.Town);
        _mapB = new GameMap(MapId.HuntingGround);

        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    static IPEndPoint Ep() => new IPEndPoint(IPAddress.Loopback, 0);

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    // player 세팅 + 맵 A 진입 완료 상태 반환
    TestMigrationSession SetupMigratingSession()
    {
        TestMigrationSession s = new(_mapA, _mapB);
        s.OnConnected(Ep());
        s.BypassHandshake();
        _mapA.Tick(1); // AddPlayer 람다 → entity 등록
        return s;
    }

    // portal 근처로 이동 + C_EnterPortal 전송 + 양쪽 맵 tick
    static void TriggerMigration(TestMigrationSession session, GameMap mapA, GameMap mapB,
                                  Vector2 nearPortalPos, int tickA, int tickB)
    {
        PlayerEntity? player = mapA.Players.FirstOrDefault(p => p.Owner == session);
        if (player != null) player.Position = nearPortalPos;

        C_EnterPortal pkt = new C_EnterPortal { portalId = PortalId };
        session.OnRecvPacket(pkt.Write());

        mapA.Tick(tickA); // 검증 + RemovePlayer + mapB.EnqueueJob
        mapB.Tick(tickB); // AddPlayerWithId + S_MapTransition + S_PlayerJoin broadcast
    }

    // --- 1. entity id 유지 (ADR-026 핵심) ---

    [Fact]
    public void EntityId_Preserved_After_Migration()
    {
        TestMigrationSession s = SetupMigratingSession();

        PlayerEntity? playerBefore = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerBefore);
        int originalEntityId = playerBefore!.EntityId;

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        PlayerEntity? playerAfter = _mapB.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerAfter);

        // ADR-026: entity id 재배정 X
        Assert.Equal(originalEntityId, playerAfter!.EntityId);
    }

    // --- 2. HP state 보존 ---

    [Fact]
    public void HP_Preserved_After_Migration()
    {
        TestMigrationSession s = SetupMigratingSession();

        PlayerEntity? playerBefore = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerBefore);

        int reducedHp = 50; // 전투로 깎인 HP 시뮬
        playerBefore!.Hp = reducedHp;

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        PlayerEntity? playerAfter = _mapB.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerAfter);

        Assert.Equal(reducedHp, playerAfter!.Hp); // HP 그대로 (리셋 X)
    }

    // --- 3. PlayerStats 보존 ---

    [Fact]
    public void Stats_Preserved_After_Migration()
    {
        TestMigrationSession s = SetupMigratingSession();

        PlayerEntity? playerBefore = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerBefore);
        PlayerStats statsBefore = playerBefore!.Stats;

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        PlayerEntity? playerAfter = _mapB.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerAfter);

        Assert.Equal(statsBefore.Class, playerAfter!.Stats.Class);
        Assert.Equal(statsBefore.Attack, playerAfter.Stats.Attack);
        Assert.Equal(statsBefore.Defense, playerAfter.Stats.Defense);
    }

    // --- 4. 맵 A에서 제거 ---

    [Fact]
    public void MapA_HasNoPlayer_AfterMigration()
    {
        TestMigrationSession s = SetupMigratingSession();
        Assert.Single(_mapA.Players);

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        Assert.Empty(_mapA.Players);
    }

    // --- 5. 맵 B에 추가 ---

    [Fact]
    public void MapB_HasPlayer_AfterMigration()
    {
        TestMigrationSession s = SetupMigratingSession();
        Assert.Empty(_mapB.Players);

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        Assert.Single(_mapB.Players);
    }

    // --- 6. S_MapTransition 수신 ---

    [Fact]
    public void Player_Receives_S_MapTransition()
    {
        TestMigrationSession s = SetupMigratingSession();
        s.SentPackets.Clear();

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        bool hasTransition = s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_MapTransition);
        Assert.True(hasTransition, "이동한 플레이어가 S_MapTransition을 받아야 함");

        byte[] pkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_MapTransition);
        S_MapTransition parsed = new S_MapTransition();
        parsed.Read(new ArraySegment<byte>(pkt));

        // Town portal → HuntingGround (id=1) spawn (2, 0) — PortalTable 정합
        Assert.Equal((byte)MapId.HuntingGround, parsed.destMapId);
        Assert.Equal(2f, parsed.spawnX);
        Assert.Equal(0f, parsed.spawnY);
    }

    // --- 7. 맵 A의 다른 플레이어가 S_PlayerLeave 수신 ---

    [Fact]
    public void MapA_OtherPlayer_Receives_S_PlayerLeave()
    {
        // observer 먼저 입장
        ObserverSession observer = new(_mapA);
        observer.OnConnected(Ep());
        _mapA.Tick(1); // observer AddPlayer 처리

        // migration할 세션 입장
        TestMigrationSession s = new(_mapA, _mapB);
        s.OnConnected(Ep());
        s.BypassHandshake();
        _mapA.Tick(2); // s AddPlayer 처리

        int observerBaseline = observer.SentPackets.Count;
        s.SentPackets.Clear();

        // portal 근처로 이동 + 전송
        PlayerEntity? player = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = NearTownPortal;

        C_EnterPortal pkt = new C_EnterPortal { portalId = PortalId };
        s.OnRecvPacket(pkt.Write());
        _mapA.Tick(3);
        _mapB.Tick(3);

        List<byte[]> newPackets = observer.SentPackets.Skip(observerBaseline).ToList();
        bool hasLeave = newPackets.Any(p => PacketIdOf(p) == PacketID.S_PlayerLeave);
        Assert.True(hasLeave, "맵 A의 다른 플레이어가 S_PlayerLeave를 받아야 함");
    }

    // --- 8. 맵 B의 기존 플레이어가 S_PlayerJoin 수신 ---

    [Fact]
    public void MapB_ExistingPlayer_Receives_S_PlayerJoin()
    {
        // observerB: 맵 B에 이미 있던 플레이어
        ObserverSession observerB = new(_mapB);
        observerB.OnConnected(Ep());
        _mapB.Tick(1); // observerB AddPlayer 처리

        TestMigrationSession s = SetupMigratingSession();
        int observerBBaseline = observerB.SentPackets.Count;
        s.SentPackets.Clear();

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        List<byte[]> newPackets = observerB.SentPackets.Skip(observerBBaseline).ToList();
        bool hasJoin = newPackets.Any(p => PacketIdOf(p) == PacketID.S_PlayerJoin);
        Assert.True(hasJoin, "맵 B의 기존 플레이어가 S_PlayerJoin을 받아야 함");
    }

    // --- 9. 왕복 state 보존 (A→B→A) ---

    [Fact]
    public void RoundTrip_A_to_B_to_A_StatePreserved()
    {
        // 1차: Town → HuntingGround
        TestMigrationSession s = new(_mapA, _mapB);
        s.OnConnected(Ep());
        s.BypassHandshake();
        _mapA.Tick(1);

        PlayerEntity? playerBeforeFirst = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerBeforeFirst);
        int hp = 70;
        playerBeforeFirst!.Hp = hp;
        int entityId = playerBeforeFirst.EntityId;

        TriggerMigration(s, _mapA, _mapB, NearTownPortal, tickA: 2, tickB: 2);

        // 맵 B에 도착 — entity id + HP 확인
        PlayerEntity? playerInB = _mapB.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerInB);
        Assert.Equal(entityId, playerInB!.EntityId);
        Assert.Equal(hp, playerInB.Hp);

        // 2차: HuntingGround → 맵 A로 역방향 (SetDestMap으로 A 지정)
        s.SetCurrentMap(_mapB);
        s.SetDestMap(_mapA);

        TriggerMigration(s, _mapB, _mapA, NearHgPortal, tickA: 3, tickB: 3);

        // 맵 A에 돌아옴
        PlayerEntity? playerBack = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(playerBack);

        // ADR-026: entity id 동일
        Assert.Equal(entityId, playerBack!.EntityId);
        // state 보존: 전투로 깎인 HP 유지 (리셋 X)
        Assert.Equal(hp, playerBack.Hp);
    }

    // --- 10. transient drop: migration 중 도착한 패킷 no-op ---

    [Fact]
    public void TransientDrop_MoveDuringMigration_IsNoOp()
    {
        // **설계 의도**: migration 중(_migrating=1) GetMap() null 반환 → SubmitMoveIntent no-op.
        // TransientTestSession이 _forceNullMap으로 migration 상태를 시뮬.
        // 결과: MoveIntent가 도착해도 map.EnqueueJob이 실행되지 않음 → 위치 변화 없음.

        GameMap testMap = new GameMap(MapId.Town);
        TransientTestSession ts = new(testMap);
        ts.OnConnected(Ep());
        ts.BypassHandshake();
        testMap.Tick(1); // entity 등록

        // migration 중 시뮬 (GetMap → null)
        ts.SimulateMigrating();

        // move intent 전송 → GetMap() null → SubmitMoveIntent 내부 no-op
        C_MoveIntent movePkt = new C_MoveIntent { input = 1, clientTick = 5 }; // right=1
        ts.OnRecvPacket(movePkt.Write());
        testMap.Tick(2); // 처리 — PendingInputX = 0 유지 → 위치 변화 없음

        PlayerEntity? player = testMap.Players.FirstOrDefault(p => p.Owner == ts);
        Assert.NotNull(player);
        // spawn (0, 0)에서 x 이동이 없었음 — migration 중 drop 확인
        Assert.Equal(0f, player!.Position.X);

        ts.SimulateMigrationComplete();
    }

    // --- 12. transient drop: migration 중 공격 패킷 no-op ---

    [Fact]
    public void TransientDrop_AttackDuringMigration_IsNoOp()
    {
        // **설계 의도**: migration 중(_migrating=1) GetMap() null → SubmitAttack no-op.
        // TransientTestSession.SimulateMigrating()으로 GetMap null 강제 → C_Attack 전송.
        // 결과: map.EnqueueJob이 실행되지 않음 → ProcessAttack 호출 없음.
        //
        // **왜 "처리 안 됨"을 검증하나?**
        //   ProcessAttack은 enemy를 공격한다. 맵에 enemy가 없으면 no-op과 구별 불가.
        //   대신 SendPackets에 S_HitResult가 없음을 확인 — 공격 처리됐으면 반드시 발송됨.

        GameMap testMap = new GameMap(MapId.Town);
        TransientTestSession ts = new(testMap);
        ts.OnConnected(Ep());
        ts.BypassHandshake();
        testMap.Tick(1); // entity 등록

        ts.SentPackets.Clear(); // baseline

        // migration 중 시뮬
        ts.SimulateMigrating();

        // C_Attack 전송 → GetMap() null → SubmitAttack 내부 early return
        C_Attack attackPkt = new C_Attack { targetEntityId = 9999, attackerClientTick = 10 };
        ts.OnRecvPacket(attackPkt.Write());
        testMap.Tick(2); // tick 처리

        // S_HitResult / S_EntityDeath가 없어야 함 (공격이 처리됐으면 둘 중 하나가 발송됨)
        bool anyHitResult = ts.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_HitResult);
        bool anyEntityDeath = ts.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_EntityDeath);
        Assert.False(anyHitResult, "migration 중 공격 처리 시 S_HitResult가 발송되면 안 됨");
        Assert.False(anyEntityDeath, "migration 중 공격 처리 시 S_EntityDeath가 발송되면 안 됨");

        ts.SimulateMigrationComplete();
    }

    // --- 13. 이동 중 disconnect — ghost entity 없음 ---

    [Fact]
    public void DisconnectDuringMigration_NoGhostEntity()
    {
        // **설계 의도**: 맵 A RemovePlayer 직후 ~ 맵 B AddPlayerWithId 직전에 OnDisconnected 도착.
        //   DisconnectOnGetDestSession.GetDestMap()이 이 "중간 시점"을 결정론적으로 재현:
        //   → GetDestMap override 안에서 OnDisconnected()를 직접 호출 (_closing=1 박힘)
        //   → 그 후 destMap 람다 실행 시 _closing=1 확인 → AddPlayerWithId skip
        //   → 결과: 양쪽 맵 모두 Players empty (ghost entity 없음).
        //
        // **왜 GetDestMap hook이 결정론적인가?**
        //   migration lambda 순서: _migrating=1 → RemovePlayer(mapA) → GetDestMap() → destMap.EnqueueJob.
        //   GetDestMap 호출 = RemovePlayer 완료 직후가 보장됨.
        //   테스트는 단일 스레드 — Interlocked.Exchange가 동기적으로 _closing=1 박음.
        //   실행 후 destMap.Tick → destMap 람다 실행 → _closing=1 확인 → skip.

        GameMap mapA = new GameMap(MapId.Town);
        GameMap mapB = new GameMap(MapId.HuntingGround);

        DisconnectOnGetDestSession s = new(mapA, mapB);
        s.OnConnected(Ep());
        s.BypassHandshake();
        mapA.Tick(1); // entity 등록

        Assert.Single(mapA.Players);
        Assert.Empty(mapB.Players);

        // portal 근처로 이동 + EnterPortal 전송
        PlayerEntity? player = mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        player!.Position = NearTownPortal;

        C_EnterPortal pkt = new C_EnterPortal { portalId = PortalId };
        s.OnRecvPacket(pkt.Write()); // → mapA.EnqueueJob(migration lambda)

        mapA.Tick(2);
        // mapA tick: migration lambda 실행
        //   _migrating=1 → RemovePlayer(mapA) → GetDestMap() → OnDisconnected() (_closing=1) → destMap.EnqueueJob(inner lambda)

        mapB.Tick(2);
        // mapB tick: inner lambda 실행
        //   _closing=1 확인 → AddPlayerWithId skip → mapB.Players 여전히 empty

        // ghost entity 없음: 양쪽 맵 모두 empty
        Assert.Empty(mapA.Players); // 맵 A: RemovePlayer됨
        Assert.Empty(mapB.Players); // 맵 B: AddPlayerWithId skip (ghost 없음)
    }

    // --- 11. S_PlayerLeave entityId 정합 ---

    [Fact]
    public void S_PlayerLeave_Contains_CorrectEntityId()
    {
        // observer와 migration 세션이 같은 맵에 있을 때 PlayerLeave.entityId가 맞는지
        ObserverSession observer = new(_mapA);
        observer.OnConnected(Ep());
        _mapA.Tick(1);

        TestMigrationSession s = new(_mapA, _mapB);
        s.OnConnected(Ep());
        s.BypassHandshake();
        _mapA.Tick(2);

        // s의 entity id 캡처
        PlayerEntity? player = _mapA.Players.FirstOrDefault(p => p.Owner == s);
        Assert.NotNull(player);
        int sEntityId = player!.EntityId;

        int observerBaseline = observer.SentPackets.Count;

        player.Position = NearTownPortal;
        C_EnterPortal pkt = new C_EnterPortal { portalId = PortalId };
        s.OnRecvPacket(pkt.Write());
        _mapA.Tick(3);
        _mapB.Tick(3);

        List<byte[]> newPackets = observer.SentPackets.Skip(observerBaseline).ToList();
        byte[]? leaveBytes = newPackets.FirstOrDefault(p => PacketIdOf(p) == PacketID.S_PlayerLeave);
        Assert.NotNull(leaveBytes);

        S_PlayerLeave parsed = new S_PlayerLeave();
        parsed.Read(new ArraySegment<byte>(leaveBytes!));
        Assert.Equal(sEntityId, parsed.entityId);
    }
}
