using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// lag compensation rewind 단위 테스트 5건.
///
/// **검증 대상** (ProcessAttack step 4.5 — rewind 범위 검증):
///   1. Rewind_HappyPath — attackerClientTick = currentTick (diff=0) → hit 정합
///   2. Rewind_OutOfRange_4Tick — diff=4 (허용 최대 경계값) → rewind 통과 + hit
///   3. Rewind_BeyondRange_SilentDrop — diff=5 → silent drop (HP 변화 X)
///   4. Rewind_NegativeTick_SilentDrop — attackerClientTick=-1 → silent drop
///   5. Rewind_FutureTick_SilentDrop — attackerClientTick > currentTick → silent drop
///
/// **테스트 전략**:
///   - GameMap 직접 주입 + Send override로 broadcast 캡처 (AttackHandlerTests 패턴 정합).
///   - ring buffer 트레이스: HistorySize=4, head 초기=0.
///     Tick(N) → RecordPosition(N, pos) → slot[head] = (N, pos) → head = (head+1)%4.
///   - ProcessAttack은 Tick 안에서 job으로 실행 (job 처리 → Physics.Step → RecordPosition 순서).
///     따라서 공격 패킷 enqueue 후 Tick(N) 시 처리 순서:
///       ① _currentTick = N
///       ② job 처리 (ProcessAttack 실행 — 이 시점엔 RecordPosition(N,...) 아직 미실행)
///       ③ Physics.Step → RecordPosition(N, pos)
///
/// **race audit**:
///   - ring buffer 갱신(RecordPosition)과 rewind lookup(GetPositionAtTick) 모두
///     tick thread에서만 호출. 단일 스레드 테스트 → race 없음.
///   - network thread는 EnqueueJob 경유 — ring buffer 직접 접근 X (헌법 #5 정합).
/// </summary>
[Collection("ConsoleSerial")]
public class LagCompensationTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // entity id 약속 (AttackHandlerTests 정합).
    const int NormalEnemyId = 1;
    const int BossEntityId = 2;
    const int PlayerEntityId = 3;

    // 옛 MapSpawnTable 값 보존 — inlined (MapSpawnTable 은퇴, M4.4 Phase 03).
    const float NormalX    = 10f;
    const float NormalY    = 0f;
    const int   NormalMaxHp = 30;
    const float BossX      = 30f;
    const float BossY      = 0f;
    const int   BossMaxHp   = 100;

    static readonly int ExpectedDamage = Formulas.ComputeDamage(
        PlayerStats.Knight(), default, baseDamage: 10);

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

    public LagCompensationTests()
    {
        // HuntingGround(Normal enemy id=1) + Boss(id=2) content 주입.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
            new EnemySpawnPoint((byte)EnemyKind.Boss,   BossX,   BossY),
        });
        _map = new GameMap(MapId.HuntingGround, content: content);

        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    // --- 헬퍼 ---

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    /// <summary>
    /// handshake 우회 + Tick(1)로 entity 등록 완료한 세션 반환.
    /// Tick(1) 이후: RecordPosition(1, spawnPos=(0,0)) 박힘 (head=1 이동).
    /// </summary>
    TestGameSession SetupHandshakedSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.BypassHandshake();
        _map.Tick(1); // AddPlayer 람다 처리 → entity 등록 + RecordPosition(1, (0,0))
        return s;
    }

    /// <summary>
    /// Normal enemy 사거리 안 좌표 설정.
    /// enemy=(10,0), AttackHalfExtent=1.5f → player=(9,0)이면:
    ///   attackBox center=(9,0) halfExtent=(1.5,1.5) → x[7.5,10.5]
    ///   enemy.Hitbox center=(10,0) halfExtent=(0.5,0.5) → x[9.5,10.5]
    ///   겹침 → Intersects=true → hit.
    /// </summary>
    static void PlaceInRange(PlayerEntity player)
        => player.Position = new Vector2(NormalX - 1f, NormalY);

    static ArraySegment<byte> AttackPacketBytes(int targetEntityId, long attackerClientTick)
    {
        C_Attack pkt = new C_Attack
        {
            targetEntityId = targetEntityId,
            attackerClientTick = (int)attackerClientTick
        };
        return pkt.Write();
    }

    // --- 테스트 5건 ---

    /// <summary>
    /// 1. Rewind_HappyPath: attackerClientTick = currentTick (diff=0) → hit.
    ///
    /// **tick 흐름**:
    ///   Tick(1): spawn(0,0) + RecordPosition(1,(0,0)), head=1
    ///   PlaceInRange → player.Position=(9,0)
    ///   공격 패킷 enqueue (attackerClientTick=2)
    ///   Tick(2): _currentTick=2 → ① job(ProcessAttack): GetPositionAtTick(2) → 아직 2 미기록
    ///            → fallback = 현재 Position = (9,0) → in-range → hit
    ///          → ② Physics.Step → RecordPosition(2,(9,0))
    ///
    /// **중요**: diff=0이고 Tick(2)의 RecordPosition(2,...) 는 job 처리 *후*에 실행됨.
    ///   따라서 GetPositionAtTick(2)는 찾지 못하고 → 현재 Position fallback → (9,0) → hit.
    /// </summary>
    [Fact]
    public void Rewind_HappyPath_CurrentTickAttack_Hits()
    {
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        s.SentPackets.Clear();

        // attackerClientTick=2 = 이번 tick. diff=0 → rewind 범위 통과.
        s.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        _map.Tick(2);

        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(NormalMaxHp - ExpectedDamage, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 2. Rewind_OutOfRange_4Tick: diff=4 (허용 최대 경계값) → rewind 통과 → hit.
    ///
    /// **ring buffer 트레이스 (HistorySize=4)**:
    ///   Tick(1): slot[0]=(1,(0,0)), head=1
    ///   PlaceInRange → player.Position=(9,0)
    ///   Tick(2): slot[1]=(2,(9,0)), head=2
    ///   Tick(3): slot[2]=(3,(9,0)), head=3
    ///   Tick(4): slot[3]=(4,(9,0)), head=0
    ///   Tick(5): slot[0]=(5,(9,0)), head=1  ← tick 1 덮어씀
    ///   공격 enqueue (attackerClientTick=2)
    ///   Tick(6): ① job: _currentTick=6, diff=6-2=4 ≤ 4 → 통과
    ///                   GetPositionAtTick(2) → slot[1]=(2,(9,0)) → (9,0) → hit
    ///            ② Physics.Step → RecordPosition(6,(9,0))
    /// </summary>
    [Fact]
    public void Rewind_OutOfRange_4Tick_AllowedBoundary_Hits()
    {
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);

        // in-range 위치 설정 후 tick 2~5 흘리기 → slot[1~3,0]에 in-range pos 박힘.
        PlaceInRange(player!);
        _map.Tick(2); // slot[1]=(2,(9,0))
        _map.Tick(3); // slot[2]=(3,(9,0))
        _map.Tick(4); // slot[3]=(4,(9,0))
        _map.Tick(5); // slot[0]=(5,(9,0)) — tick 1 슬롯 덮어씀

        s.SentPackets.Clear();

        // currentTick=6, attackerClientTick=2 → diff=4 = 허용 최대 경계값.
        // GetPositionAtTick(2) = slot[1] = (9,0) → AABB hit.
        s.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        _map.Tick(6);

        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(NormalMaxHp - ExpectedDamage, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 3. Rewind_BeyondRange_SilentDrop: diff=5 (5 tick 전, 200ms 초과) → silent drop.
    ///
    /// currentTick=6, attackerClientTick=1 → diff=5 > 4 → 조건 3번 해당 → return.
    /// </summary>
    [Fact]
    public void Rewind_BeyondRange_SilentDrop()
    {
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        _map.Tick(2);
        _map.Tick(3);
        _map.Tick(4);
        _map.Tick(5);
        s.SentPackets.Clear();

        // diff = 6 - 1 = 5 > 4 → silent drop.
        s.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 1));
        _map.Tick(6);

        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));
        Assert.Equal(NormalMaxHp, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 4. Rewind_NegativeTick_SilentDrop: attackerClientTick=-1 → silent drop.
    ///
    /// int -1 → long -1L → 조건 1번(< 0) → return.
    /// </summary>
    [Fact]
    public void Rewind_NegativeTick_SilentDrop()
    {
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        s.SentPackets.Clear();

        // attackerClientTick = -1 (int PDL 필드) → ProcessAttack에서 long -1L → 음수 → return.
        C_Attack pkt = new C_Attack { targetEntityId = NormalEnemyId, attackerClientTick = -1 };
        s.OnRecvPacket(pkt.Write());
        _map.Tick(2);

        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(NormalMaxHp, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 5. Rewind_FutureTick_SilentDrop: attackerClientTick > currentTick → silent drop.
    ///
    /// currentTick=2, attackerClientTick=999 → 조건 2번(> _currentTick) → return.
    /// </summary>
    [Fact]
    public void Rewind_FutureTick_SilentDrop()
    {
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        s.SentPackets.Clear();

        s.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 999));
        _map.Tick(2);

        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(NormalMaxHp, _map.Enemies[NormalEnemyId].Hp);
    }
}
