using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// 연출(스윙) ↔ 명중(데미지) 분리 단위 테스트 4건 (M4.7 Phase 03).
///
/// **검증 대상** (ProcessAttack 재배치 후):
///   1. AirSwing_NoTarget_EntersAttackState_NoHitResult
///      — 타겟 없음(targetEntityId=0) → EnterAttackState 진입 + observer에게 S_PlayerAttack 1회 + 데미지 0
///   2. AirSwing_OutOfRange_EntersAttackState_NoHitResult
///      — 타겟 존재하나 AABB 밖 → 스윙 진입 + S_PlayerAttack 1회 + 데미지 0 + S_HitResult 없음
///   3. Hit_InRange_AttackStateAndDamage
///      — 타겟 AABB 안 → 스윙 진입 + S_HitResult 1회 + 데미지 정상 적용
///      — S_PlayerAttack은 except=attacker.Owner 로 attacker 본인 SentPackets엔 없음 (observer로 검증)
///   4. RateLimit_ThrottlesSwingToo
///      — 쿨다운 내 연타 → 두 번째 스윙도 throttle (EnterAttackState 1회, S_PlayerAttack 1회)
///
/// **테스트 전략**:
///   - attacker 세션 + observer 세션 2개를 같은 맵에 등록.
///   - S_PlayerAttack은 except=attacker.Owner → attacker SentPackets에는 없고 observer SentPackets에 있음.
///   - GameMap.BroadcastToAll(except=attacker.Owner) 기존 동작 재사용.
///   - ActionFsm 상태는 PlayerCombatStates.Attack 타입 비교로 확인 — AttackState 내부 public 노출 없이.
/// </summary>
[Collection("ConsoleSerial")]
public class CombatSwingTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    const int NormalEnemyId = 1;
    const int BossEntityId  = 2;
    const int AttackerEntityId  = 3;
    const int ObserverEntityId  = 4;

    const float NormalX    = 10f;
    const float NormalY    = 0f;
    const int   NormalMaxHp = 30;
    const float BossX      = 30f;
    const float BossY      = 0f;

    // attacker(Knight) + Normal enemy 조합 예상 데미지.
    static readonly int ExpectedDamage = Formulas.ComputeDamage(
        PlayerStats.Knight(), EnemyStats.NormalDefault(), baseDamage: 10);

    // ── TestGameSession ────────────────────────────────────────────────────────

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

        public void BypassHandshake(byte charClass = 0) // 0=Knight
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(charClass);
            EnterGameWorldIfReady();
        }
    }

    // ── 생성자 / Dispose ────────────────────────────────────────────────────────

    public CombatSwingTests()
    {
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

    // ── 헬퍼 ───────────────────────────────────────────────────────────────────

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    /// <summary>
    /// attacker + observer 두 세션을 등록하고 Tick(1) 완료 상태 반환.
    /// - attacker: EntityId=3, observer: EntityId=4
    /// - Tick(1) 이후 두 entity 모두 RecordPosition(1, spawnPos) 완료.
    /// </summary>
    (TestGameSession attacker, TestGameSession observer) SetupTwoSessions()
    {
        TestGameSession attacker = new(_map);
        attacker.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        attacker.BypassHandshake(charClass: 0); // Knight

        TestGameSession observer = new(_map);
        observer.OnConnected(new IPEndPoint(IPAddress.Loopback, 1));
        observer.BypassHandshake(charClass: 0);

        _map.Tick(1); // AddPlayer 람다 처리 → 두 entity 등록
        return (attacker, observer);
    }

    /// <summary>
    /// attacker를 Normal enemy 사거리 안에 배치.
    /// enemy=(10,0), AttackHalfExtent=1.5f → (9,0) → attackBox x[7.5,10.5] ∩ enemy x[9.5,10.5] → hit.
    /// </summary>
    static void PlaceInRange(PlayerEntity player)
        => player.Position = new Vector2(NormalX - 1f, NormalY);

    /// <summary>
    /// attacker를 Normal enemy 사거리 밖에 배치.
    /// (20,0) → attackBox x[18.5,21.5] — enemy(10,0) x[9.5,10.5] → 완전 분리 → miss.
    /// </summary>
    static void PlaceOutOfRange(PlayerEntity player)
        => player.Position = new Vector2(20f, NormalY);

    static ArraySegment<byte> AttackPacketBytes(int targetEntityId, long attackerClientTick)
    {
        C_Attack pkt = new C_Attack
        {
            targetEntityId     = targetEntityId,
            attackerClientTick = (int)attackerClientTick,
        };
        return pkt.Write();
    }

    // ── 테스트 4건 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 1. AirSwing_NoTarget: targetEntityId=0(sentinel) → 허공 스윙.
    ///
    /// **기대값**:
    ///   - attacker ActionFsm → AttackState 진입 (EnterAttackState 호출됨)
    ///   - observer.SentPackets에 S_PlayerAttack 1회 (attacker 제외라 attacker엔 없음)
    ///   - attacker.SentPackets에 S_HitResult 없음 (데미지 0)
    ///   - Normal enemy HP 변화 없음
    /// </summary>
    [Fact]
    public void AirSwing_NoTarget_EntersAttackState_NoHitResult()
    {
        var (attacker, observer) = SetupTwoSessions();
        PlayerEntity? attackerEntity = _map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();

        // targetEntityId=0 sentinel (클라이언트가 "타겟 없음"을 표현하는 값)
        attacker.OnRecvPacket(AttackPacketBytes(targetEntityId: 0, attackerClientTick: 2));
        _map.Tick(2);

        // 스윙 진입: ActionFsm이 AttackState여야 함
        Assert.IsType<Dawnholder.Server.GameServer.Maps.States.AttackState>(
            attackerEntity!.ActionFsm.CurrentState);

        // observer에게 S_PlayerAttack 1회 도달
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_PlayerAttack));

        // attacker 본인엔 S_PlayerAttack 없음 (except=attacker.Owner)
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_PlayerAttack));

        // 데미지 없음
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_HitResult));
        Assert.Equal(0, CountPacketsOfType(observer.SentPackets, PacketID.S_HitResult));
        Assert.Equal(NormalMaxHp, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 2. AirSwing_OutOfRange: 타겟 존재하나 AABB 밖 → 스윙 진입 + 데미지 없음.
    ///
    /// **기대값**:
    ///   - EnterAttackState 진입 (AttackState)
    ///   - S_PlayerAttack observer에게 1회
    ///   - S_HitResult 없음
    ///   - Normal enemy HP 변화 없음
    /// </summary>
    [Fact]
    public void AirSwing_OutOfRange_EntersAttackState_NoHitResult()
    {
        var (attacker, observer) = SetupTwoSessions();
        PlayerEntity? attackerEntity = _map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        PlaceOutOfRange(attackerEntity!); // 사거리 밖

        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();

        attacker.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        _map.Tick(2);

        // 스윙 진입
        Assert.IsType<Dawnholder.Server.GameServer.Maps.States.AttackState>(
            attackerEntity!.ActionFsm.CurrentState);

        // observer에게 S_PlayerAttack 1회
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_PlayerAttack));

        // 데미지 없음
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_HitResult));
        Assert.Equal(0, CountPacketsOfType(observer.SentPackets, PacketID.S_HitResult));
        Assert.Equal(NormalMaxHp, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 3. Hit_InRange: 타겟 AABB 안 → 스윙 진입 + 데미지 적용.
    ///
    /// **기대값**:
    ///   - EnterAttackState 진입
    ///   - observer에게 S_PlayerAttack 1회 (attacker 본인엔 없음)
    ///   - S_HitResult 1회 (전원 broadcast — attacker 포함)
    ///   - Normal enemy HP 감소
    /// </summary>
    [Fact]
    public void Hit_InRange_AttackStateAndDamage()
    {
        var (attacker, observer) = SetupTwoSessions();
        PlayerEntity? attackerEntity = _map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        PlaceInRange(attackerEntity!);

        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();

        attacker.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: 2));
        _map.Tick(2);

        // 스윙 진입
        Assert.IsType<Dawnholder.Server.GameServer.Maps.States.AttackState>(
            attackerEntity!.ActionFsm.CurrentState);

        // S_PlayerAttack: observer에게만 (attacker except)
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_PlayerAttack));
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_PlayerAttack));

        // S_HitResult: 전원에게 (attacker 포함)
        Assert.Equal(1, CountPacketsOfType(attacker.SentPackets, PacketID.S_HitResult));
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_HitResult));

        // 데미지 적용
        Assert.Equal(NormalMaxHp - ExpectedDamage, _map.Enemies[NormalEnemyId].Hp);
    }

    /// <summary>
    /// 4. RateLimit_ThrottlesSwingToo: 쿨다운 내 연타 → 두 번째 스윙도 throttle.
    ///
    /// **시나리오**:
    ///   Tick(2): 첫 번째 공격 → 스윙 + S_PlayerAttack 1회
    ///   Tick(3): 즉시 두 번째 공격 → rate-limit (500ms 미경과) → silent drop
    ///   → S_PlayerAttack 총 1회, ActionFsm은 AttackState(첫 번째 진입 유지)
    ///
    /// **rate-limit이 스윙도 throttle한다**는 것이 핵심 — 스팸 스윙 차단(헌법 #3).
    /// </summary>
    [Fact]
    public void RateLimit_ThrottlesSwingToo()
    {
        var (attacker, observer) = SetupTwoSessions();
        PlayerEntity? attackerEntity = _map.GetPlayer(AttackerEntityId);
        Assert.NotNull(attackerEntity);

        // 허공 스윙으로 rate-limit 영향 순수하게 관찰 (타겟 없음)
        attacker.SentPackets.Clear();
        observer.SentPackets.Clear();

        // 첫 번째 스윙
        attacker.OnRecvPacket(AttackPacketBytes(targetEntityId: 0, attackerClientTick: 2));
        _map.Tick(2);

        // 두 번째 스윙 — 동일 틱 직후라 500ms 미경과
        attacker.OnRecvPacket(AttackPacketBytes(targetEntityId: 0, attackerClientTick: 3));
        _map.Tick(3);

        // S_PlayerAttack은 첫 번째 스윙 1회만
        Assert.Equal(1, CountPacketsOfType(observer.SentPackets, PacketID.S_PlayerAttack));

        // S_HitResult는 없음 (허공)
        Assert.Equal(0, CountPacketsOfType(attacker.SentPackets, PacketID.S_HitResult));
    }
}
