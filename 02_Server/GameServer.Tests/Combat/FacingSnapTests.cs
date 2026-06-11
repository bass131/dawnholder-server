using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// facing 스냅 회귀 안전망 (M4.11 Phase 03 — P1 백로그 회수).
///
/// <b>검증 대상</b>: CombatSystem.ProcessAttack의 facing latch 로직.
///   1. FacingSnap_LiveTarget_RightSide — live target이 오른쪽 → FacingDir=+1 스냅
///   2. FacingSnap_LiveTarget_LeftSide  — live target이 왼쪽  → FacingDir=-1 스냅
///   3. FacingSnap_NoTarget_Sentinel    — targetEntityId=0(허공) → FacingDir 유지
///
/// <b>불변식</b>: P4 후에도 절대 green 유지.
///   facing 스냅은 P4(클라 Predict 고정스텝)와 무관한 서버 권위 로직이다.
///
/// <b>범위 제한</b>: facing latch만 검증. 데미지/히트 판정/HP 변화는 CombatSwingTests 담당.
/// </summary>
[Collection("ConsoleSerial")]
public class FacingSnapTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // EnemySpawnPoint 1마리 → id=1. 플레이어 첫 번째 BypassHandshake → id=2.
    // CombatSwingTests(적 2마리)와 달리 여기는 적 1마리만 스폰 → 플레이어 id=2.
    const int NormalEnemyId    = 1;
    const float EnemyX         = 10f;
    const float EnemyY         = 0f;

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

        public void BypassHandshake(byte charClass = 0)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(charClass);
            EnterGameWorldIfReady();
        }
    }

    // ── 생성자 / Dispose ────────────────────────────────────────────────────────

    public FacingSnapTests()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, EnemyX, EnemyY),
        });
        _map = new GameMap(MapId.HuntingGround, content: content);

        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    // ── 헬퍼 ───────────────────────────────────────────────────────────────────

    TestGameSession SetupAttacker(sbyte initialFacing = 1)
    {
        TestGameSession session = new(_map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake(charClass: 0); // Knight
        _map.Tick(1);
        // Players[0] 직접 접근 — EntityId는 내부 AllocId 순서에 의존하지 않음.
        if (_map.Players.Count > 0) _map.Players[0].FacingDir = initialFacing;
        session.SentPackets.Clear();
        return session;
    }

    static ArraySegment<byte> AttackPacketBytes(int targetEntityId)
        => new C_Attack { targetEntityId = targetEntityId, attackerClientTick = 2 }.Write();

    // ── 불변식 테스트 3건 ──────────────────────────────────────────────────────

    /// <summary>
    /// 1. live target이 오른쪽(target.X > attacker.X) → FacingDir=+1 스냅.
    ///
    /// 설정: attacker x=5(왼쪽), target(Normal enemy) x=10(오른쪽) → 오른쪽 스냅.
    /// 초기 FacingDir=-1로 세팅해 스냅이 실제로 바뀌는지 확인.
    ///
    /// 불변식 — P4 무관.
    /// </summary>
    [Fact]
    public void FacingSnap_LiveTarget_RightSide_SnapsPlusOne()
    {
        TestGameSession session = SetupAttacker(initialFacing: -1); // 초기 왼쪽 방향
        Assert.True(_map.Players.Count > 0, "플레이어 등록 실패");
        PlayerEntity attacker = _map.Players[0];

        // attacker를 enemy 왼쪽에 배치 (enemy.X=10 > attacker.X=5 → 오른쪽 스냅)
        attacker.Position = new Vector2(5f, EnemyY);
        // tick 2는 미기록 → ProcessAttack의 rewind는 현재 Position fallback을 탐 (이 기록 자체는 읽히지 않음 — 의도 명시용).
        attacker.RecordPosition(1, attacker.Position);
        Assert.Equal(-1, attacker.FacingDir); // 전제: 초기값 확인

        session.OnRecvPacket(AttackPacketBytes(NormalEnemyId));
        _map.Tick(2);

        Assert.True(attacker.FacingDir == (sbyte)1,
            $"live target 오른쪽 → FacingDir=+1 스냅 실패 (actual={attacker.FacingDir})");
    }

    /// <summary>
    /// 2. live target이 왼쪽(target.X &lt; attacker.X) → FacingDir=-1 스냅.
    ///
    /// 설정: attacker x=20(오른쪽), target x=10(왼쪽) → 왼쪽 스냅.
    /// 초기 FacingDir=+1로 세팅해 스냅이 실제로 바뀌는지 확인.
    ///
    /// 불변식 — P4 무관.
    /// </summary>
    [Fact]
    public void FacingSnap_LiveTarget_LeftSide_SnapsMinusOne()
    {
        TestGameSession session = SetupAttacker(initialFacing: 1); // 초기 오른쪽 방향
        Assert.True(_map.Players.Count > 0, "플레이어 등록 실패");
        PlayerEntity attacker = _map.Players[0];

        // attacker를 enemy 오른쪽에 배치 (enemy.X=10 < attacker.X=11 → 왼쪽 스냅)
        // AttackHalfExtent=1.5f → x=11이면 attackBox x=[9.5,12.5] ∩ enemy x=[9.5,10.5] → hit
        attacker.Position = new Vector2(EnemyX + 1f, EnemyY); // x=11
        // tick 2는 미기록 → ProcessAttack의 rewind는 현재 Position fallback을 탐 (이 기록 자체는 읽히지 않음 — 의도 명시용).
        attacker.RecordPosition(1, attacker.Position);
        Assert.Equal((sbyte)1, attacker.FacingDir); // 전제: 초기값 확인

        session.OnRecvPacket(AttackPacketBytes(NormalEnemyId));
        _map.Tick(2);

        Assert.True(attacker.FacingDir == (sbyte)-1,
            $"live target 왼쪽 → FacingDir=-1 스냅 실패 (actual={attacker.FacingDir})");
    }

    /// <summary>
    /// 3. 허공 스윙(targetEntityId=0 sentinel) → FacingDir 유지.
    ///
    /// 설정: attacker FacingDir=+1 → targetEntityId=0으로 공격 → FacingDir 변화 없음.
    ///
    /// 근거: CombatSystem.ProcessAttack은 hasLiveTarget=false이면 FacingDir 스냅 분기를 건너뛴다.
    /// 불변식 — P4 무관.
    /// </summary>
    [Fact]
    public void FacingSnap_NoTarget_Sentinel_FacingDirUnchanged()
    {
        TestGameSession session = SetupAttacker(initialFacing: 1);
        Assert.True(_map.Players.Count > 0, "플레이어 등록 실패");
        PlayerEntity attacker = _map.Players[0];

        sbyte facingBefore = attacker.FacingDir;

        // targetEntityId=0 sentinel → 허공 스윙
        session.OnRecvPacket(AttackPacketBytes(targetEntityId: 0));
        _map.Tick(2);

        // FacingDir은 변하지 않아야 함
        Assert.True(attacker.FacingDir == facingBefore,
            $"허공 스윙(sentinel) FacingDir 유지 실패 (before={facingBefore}, actual={attacker.FacingDir})");
    }
}
