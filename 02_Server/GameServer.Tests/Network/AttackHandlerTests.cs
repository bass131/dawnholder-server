using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// AttackHandler 회귀 안전망:
/// `C_Attack` 수신 → `GameMap.ProcessAttack` 6단계 검증 → `S_HitResult` / `S_EntityDeath` broadcast.
///
/// **검증 invariant** (6건):
///   - Happy: 정상 범위 안 공격 → enemy Hp 감소 + S_HitResult 전원 broadcast (attacker 자기 포함)
///   - OutOfRange: 서버 권위 position `dist² >= range²` → silent drop (no HP change + no broadcast)
///   - RateLimitViolation: 500ms 안 2회 → 1회만 적용, 2회차 silent drop
///   - AuthFailure: handshake 미완 첫 패킷 = C_Attack → first-packet 게이트 Disconnect (헌법 #3)
///   - KillBroadcast: HP 30 + damage 10 × 3 → 3회차에 S_EntityDeath 1회 + _enemies에서 제거
///   - DuplicateDeath: kill 후 추가 공격 → idempotent no-op (HitResult/Death 추가 broadcast X)
///
/// **테스트 전략**:
///   - GameMap 직접 주입(GetMap override) → GameWorld.Instance singleton race 차단
///   - Send override로 broadcast 패킷 캡처 (`SentPackets`) → 회신 byte 검증
///   - Disconnect override로 호출 카운트 추적 (AuthFailure 검증)
///   - `OnConnected`는 base 유지 (handshake 대기) — 우회는 명시 `BypassHandshake()` 호출만
///
/// **rate-limit time 조작 결정**: production code 변경 *0건*. `PlayerEntity.LastAttackTickMs`가
///   *이미 public setter*라 fixture가 직접 `entity.LastAttackTickMs = 0`으로 reset해 cooldown 우회.
///   500ms `Thread.Sleep`은 (a) 느린 테스트 (b) 병렬 flake → 직접 reset 채택.
/// </summary>
[Collection("ConsoleSerial")]
public class AttackHandlerTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // GameMap ctor가 Normal enemy=1 + Boss=2를 박음 → 다음 발급은 player=3.
    // 본 상수는 매직 넘버 회피용.
    const int EnemyEntityId = 1;       // Normal enemy
    const int BossEntityId = 2;        // Boss
    const int PlayerEntityId = 3;

    // 옛 MapSpawnTable 값 보존 — inlined (MapSpawnTable 은퇴, M4.4 Phase 03).
    const float NormalX    = 10f;
    const float NormalY    = 0f;
    const int   NormalMaxHp = 30;
    const float BossX      = 30f;
    const float BossY      = 0f;
    const int   BossMaxHp   = 100;

    // Formulas.ComputeDamage(Warrior, default, BaseDamage=10) = Max(1, 10+15-0) = 25.
    // 두 곳 drift 방지: 이 값은 Warrior factory + EnemyStats default + CombatConstants.BaseDamage 3곳 기반.
    static readonly int ExpectedDamage = Formulas.ComputeDamage(
        PlayerStats.Warrior(), default, baseDamage: 10);

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        // OnConnected는 base 유지 — handshake 대기 상태.
        // Happy/RateLimit/KillBroadcast 테스트는 BypassHandshake로 명시 우회.
        // AuthFailure 테스트는 우회 *안 함* → first-packet 게이트 검증.
        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        // 월드 진입 = handshake + class 선택 양쪽 충족 필요. class 선택도 우회 (Warrior=0).
        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();  // _handshakeCompleted = true
            SetCharacterClass(0);          // HasSelectedClass = true (Warrior)
            EnterGameWorldIfReady();       // 두 조건 충족 → EnterGameWorld() 호출
        }
    }

    public AttackHandlerTests()
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

    // PacketID 헤더(offset 2~3)에서 ID 추출.
    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    // 정상 진입한 세션 + tick 1회 통과 → player entity 등록 완료 상태.
    TestGameSession SetupHandshakedSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.BypassHandshake();   // CompleteHandshakeAndEnter → AddPlayer/EnterMap job enqueue
        _map.Tick(1);          // EnterGameWorld 람다 처리 → _entityId 박힘
        return s;
    }

    // 공격 사거리 안으로 플레이어 좌표 조정.
    // enemy는 (10, 0)에 박혀있고 AttackRangeSquared=9 → distance < 3 필요.
    // (9, 0)이면 distance=1 = 안전 in-range.
    static void PlaceInRange(PlayerEntity player)
        => player.Position = new Vector2(NormalX - 1f, NormalY);

    // 공격 사거리 *밖*: enemy (10, 0)에서 거리 10 → dist² = 100 ≥ 9.
    // spawn 좌표 그대로(0, 0)면 자동 out-of-range지만 명시적 박음 = 의도 표현.
    static void PlaceOutOfRange(PlayerEntity player)
        => player.Position = new Vector2(0f, 0f);

    // zero-lag 시뮬 = attackerClientTick을 현재 서버 tick과 동일하게 설정 → diff=0 → rewind 없음.
    // ProcessAttack의 rewind 범위 검증을 통과하면서 zero-lag와 동일한 결과.
    static ArraySegment<byte> AttackPacketBytes(int targetEntityId, long attackerClientTick = 0)
    {
        C_Attack pkt = new C_Attack { targetEntityId = targetEntityId, attackerClientTick = (int)attackerClientTick };
        return pkt.Write();
    }

    // --- 6건 회귀 ---

    [Fact]
    public void Happy_ValidAttack_BroadcastsHitResult()
    {
        // arrange: handshake 통과 + sin-range 좌표 박음.
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);

        // baseline reset — 이후 attack 회신 패킷만 검증.
        s.SentPackets.Clear();

        // act: C_Attack 송신 → tick으로 ProcessAttack job 처리.
        // attackerClientTick=2 = 이번 tick과 동일(diff=0) → rewind 없음 = 옛 동작 정합.
        s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: 2));
        _map.Tick(2);

        // S_HitResult broadcast 1건 (자기 자신 포함 전원 — except=null) 검증.
        int hitCount = CountPacketsOfType(s.SentPackets, PacketID.S_HitResult);
        Assert.Equal(1, hitCount);

        S_HitResult parsed = new S_HitResult();
        byte[] hitPacket = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_HitResult);
        parsed.Read(new ArraySegment<byte>(hitPacket));
        Assert.Equal(PlayerEntityId, parsed.attackerEntityId);
        Assert.Equal(EnemyEntityId, parsed.targetEntityId);
        // Warrior(Attack=15) + BaseDamage=10 - Defense=0 = 25.
        Assert.Equal(ExpectedDamage, parsed.damage);
        Assert.Equal(NormalMaxHp - ExpectedDamage, parsed.currentHp); // 30 - 25 = 5
        Assert.Equal(NormalMaxHp, parsed.maxHp);                      // 30

        // 권위 상태(enemy.Hp)도 같이 갱신됨 — broadcast와 정합.
        EnemyEntity enemy = _map.Enemies[EnemyEntityId];
        Assert.Equal(NormalMaxHp - ExpectedDamage, enemy.Hp);
        Assert.False(enemy.IsDead);

        // Death broadcast 없어야 — Hp=20 > 0이라 kill 분기 미진입.
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));
        Assert.Equal(0, s.DisconnectCalls);
    }

    [Fact]
    public void OutOfRange_NoHpChange_NoBroadcast()
    {
        // arrange: handshake 통과 + 사거리 밖 좌표.
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceOutOfRange(player!); // distance = 10, AttackRange = 3 → 거리 초과

        s.SentPackets.Clear();

        // act: attackerClientTick=2=현재 tick → rewind 범위 통과. 거리 초과로 step 5 silent drop.
        s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: 2));
        _map.Tick(2);

        // silent drop 이유: 거리 초과 (range check). attackerClientTick=2=현재 tick → rewind 통과.
        // HitResult/Death 둘 다 broadcast 없음 (헌법 #3 fail-closed no-op).
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));

        // enemy Hp 변동 없음 — server 권위 상태 보존 검증.
        EnemyEntity enemy = _map.Enemies[EnemyEntityId];
        Assert.Equal(NormalMaxHp, enemy.Hp);
        Assert.False(enemy.IsDead);
    }

    [Fact]
    public void RateLimitViolation_SecondAttackDropped()
    {
        // arrange: handshake + in-range. 첫 공격 적용 후 LastAttackTickMs 박힘 → 500ms 안 두 번째 attack은 drop.
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        s.SentPackets.Clear();

        // act 1: 첫 attack → 정상 적용 (Hp 30 → 20). attackerClientTick=2=현재 tick.
        s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: 2));
        _map.Tick(2);
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));

        // act 2: 즉시 두 번째 attack — Environment.TickCount64는 단조 증가지만 직전 tick과 같거나 ms 단위 차 → 500ms 안.
        // PlayerEntity.LastAttackTickMs는 *그대로 두고* attack을 한 번 더 보냄 → ProcessAttack step 4 (rate-limit)에서 silent drop.
        // attackerClientTick=3=현재 tick → rewind 범위 통과 (rate-limit이 먼저 잡음).
        s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: 3));
        _map.Tick(3);

        // S_HitResult 여전히 1건만 (두 번째는 silent drop). enemy Hp 추가 감소 X.
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));

        EnemyEntity enemy = _map.Enemies[EnemyEntityId];
        // 첫 hit 후 Hp = 30 - 25 = 5 (한 번만).
        Assert.Equal(NormalMaxHp - ExpectedDamage, enemy.Hp);
    }

    [Fact]
    public void AuthFailure_HandshakeIncomplete_AttackRejected()
    {
        // arrange: OnConnected만 호출 — BypassHandshake 안 함 → _handshakeCompleted=false 상태.
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        // 본 시점 SentPackets = [] (handshake 대기), DisconnectCalls = 0.

        // act: 첫 패킷으로 C_Attack 진입 → first-packet 게이트가 차단해야 함.
        // attackerClientTick은 어떤 값이든 무관 — 게이트 이전에 차단.
        s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: 1));
        _map.Tick(1);

        // first-packet 게이트는 dispatcher 진입 전 차단 → Send X, Disconnect 1회.
        Assert.Empty(s.SentPackets);
        Assert.Equal(1, s.DisconnectCalls);
        Assert.Contains("[Trust] First packet was C_Attack", _consoleCapture.ToString());

        // enemy Hp 변동 없음 — handler 도달 전 차단 검증.
        EnemyEntity enemy = _map.Enemies[EnemyEntityId];
        Assert.Equal(NormalMaxHp, enemy.Hp);
    }

    [Fact]
    public void KillBroadcast_HpZero_BroadcastsDeath_RemovesFromMap()
    {
        // HP 30 + damage 25 → 2회 공격 필요.
        // 1번째: 30 - 25 = 5. 2번째: 5 - 25 = -20 → IsDead. currentHp = -20 (Math.Max는 데미지에만 적용, Hp 자체는 음수 가능).
        // rate-limit 우회: 매 공격 후 `player.LastAttackTickMs = 0`으로 reset (production code 변경 X, public setter).
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        s.SentPackets.Clear();

        // 2회 공격 루프 — 매번 LastAttackTickMs reset으로 cooldown 우회.
        // attackerClientTick=tick과 동일 → diff=0 → rewind 없음.
        int hitsNeeded = (int)Math.Ceiling((double)NormalMaxHp / ExpectedDamage); // 2
        long tick = 2;
        for (int i = 0; i < hitsNeeded; i++)
        {
            player!.LastAttackTickMs = 0; // cooldown 우회 (테스트 hook = public setter 직접 사용)
            s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: tick));
            _map.Tick(tick++);
        }

        // 검증: S_HitResult 2건 (각 -25), S_EntityDeath 1건.
        Assert.Equal(hitsNeeded, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));

        // 마지막 HitResult parsed = currentHp ≤ 0 (Math.Max는 데미지에만, Hp 음수 가능).
        byte[] lastHit = s.SentPackets.Last(p => PacketIdOf(p) == PacketID.S_HitResult);
        S_HitResult parsedHit = new S_HitResult();
        parsedHit.Read(new ArraySegment<byte>(lastHit));
        Assert.True(parsedHit.currentHp <= 0);

        // S_EntityDeath payload = entityId 정합.
        byte[] deathPacket = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_EntityDeath);
        S_EntityDeath parsedDeath = new S_EntityDeath();
        parsedDeath.Read(new ArraySegment<byte>(deathPacket));
        Assert.Equal(EnemyEntityId, parsedDeath.entityId);

        // map에서 제거됨 — 다음 attack은 step 2(GetEnemyById null)에서 자동 잘림.
        Assert.False(_map.Enemies.ContainsKey(EnemyEntityId));
    }

    [Fact]
    public void DuplicateDeath_AttackAfterKill_NoBroadcast()
    {
        // arrange: KillBroadcast 흐름 그대로 → enemy 죽임.
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRange(player!);
        s.SentPackets.Clear();

        // 2회 hit으로 Normal enemy 사망. attackerClientTick=tick → diff=0 → rewind 없음.
        int hitsNeeded = (int)Math.Ceiling((double)NormalMaxHp / ExpectedDamage);
        long tick = 2;
        for (int i = 0; i < hitsNeeded; i++)
        {
            player!.LastAttackTickMs = 0;
            s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: tick));
            _map.Tick(tick++);
        }
        // 사전 검증: 죽었고 map에서 빠짐.
        Assert.False(_map.Enemies.ContainsKey(EnemyEntityId));
        int hitsAfterKill = CountPacketsOfType(s.SentPackets, PacketID.S_HitResult);
        int deathsAfterKill = CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath);
        Assert.Equal(hitsNeeded, hitsAfterKill);
        Assert.Equal(1, deathsAfterKill);

        // act: kill 후 추가 attack → idempotent 검증. cooldown reset해서 *최대한* 통과 시도.
        player!.LastAttackTickMs = 0;
        s.OnRecvPacket(AttackPacketBytes(EnemyEntityId, attackerClientTick: tick));
        _map.Tick(tick++);

        // 검증: HitResult/Death 추가 broadcast 없음 (target lookup step 2에서 silent drop).
        Assert.Equal(hitsAfterKill, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(deathsAfterKill, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));
    }
}
