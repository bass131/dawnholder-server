using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.7 HP 동기화 회귀 스모크.
//
// 검증 목표: 서버가 보스 피격 → S_PlayerHp(21) 흐름으로
//   ① 초기 풀HP → ② 감소 → ③ 사망(0) → ④ 부활(풀HP) 순서를 관측한다.
//
// 봇은 보스를 절대 공격하지 않는다.
// 이유: 보스는 리스폰이 없어(EnqueueRespawn은 Normal 몬스터만) 봇이 보스를 죽이면
// 피격 소스가 사라져 사망→부활 관측이 불가능해진다.
public class HpSyncSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    static readonly TimeSpan DeathReviveTimeout = TimeSpan.FromSeconds(40);

    const byte BossEntityKind = 1;
    const float PreferredAttackDistance = 1.5f;
    const float ReapproachThreshold = 1.0f;
    const int MaxReapproachTicks = 30;

    const float TownPortalX = 20f;
    const int TownPortalId = 1;
    const float HGPortalX = 25f;
    const int HGPortalId = 1;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int BossEntityId;
        public int MaxHp;
        public bool SawInitialFull;
        public bool SawDamage;
        public bool SawZero;
        public bool SawReviveFull;
        public int HpEventCount;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        HpProbe bot = new();

        try
        {
            bot.Connect(host, port);

            if (!bot.WaitConnected(DefaultTimeout))
                return Fail(result, "connect timeout");

            if (!bot.WaitHandshake(DefaultTimeout))
                return Fail(result, "S_HandshakeResult timeout");

            if (!bot.HandshakeOk)
                return Fail(result, $"handshake rejected: {bot.HandshakeReason}");

            if (!bot.WaitEnterMap(DefaultTimeout))
                return Fail(result, "S_EnterMap timeout");

            // Town → HuntingGround
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // standalone 보스 게이트 충족: C_CheatCommand{cheatType=0} → 서버 DEBUG 치트(DebugCompleteQuest).
            // killCount를 게이트 임계로 즉시 세팅 → HG→BossRoom 포탈 통과.
            // 서버는 #if DEBUG 빌드에서만 처리. standalone 회귀는 DEBUG 빌드 전용.
#if DEBUG
            bot.SendCheatCompleteQuest();
            await Task.Delay(Constants.TickIntervalMs * 3, ct);
#endif

            // HuntingGround → BossRoom
            // robust MoveToPortal(_serverX 추적) — HG 적 hitstun/넉백에 의한 undershoot 방어.
            await bot.MoveToPortal(HGPortalX, ct);
            bot.SendEnterPortal(HGPortalId);
            if (!await bot.WaitSecondMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — HuntingGround→BossRoom");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? bossSpawn = await bot.WaitForBossSpawn(DefaultTimeout, ct);
            if (bossSpawn == null)
                return Fail(result, "Boss S_EntitySpawn timeout");

            if (bossSpawn.entityId <= 0 || bossSpawn.entityKind != BossEntityKind || bossSpawn.currentHp <= 0)
                return Fail(result, $"invalid boss spawn: entityId={bossSpawn.entityId} kind={bossSpawn.entityKind} hp={bossSpawn.currentHp}");

            result.LocalEntityId = bot.LocalEntityId;
            result.BossEntityId = bossSpawn.entityId;

            // 보스 범위 안으로 이동해 피격 소스를 확보한다.
            await bot.MoveIntoBossRange(bossSpawn.x, ct);

            // 검증 4단계: S_PlayerHp 이벤트 흐름을 순서대로 관측한다.
            // ① SawInitialFull: 맵 진입 직후 currentHp == maxHp
            // ② SawDamage:      0 < currentHp < maxHp (보스 피격 감소)
            // ③ SawZero:        currentHp == 0 (사망 floor)
            // ④ SawReviveFull:  ③ 이후 다시 currentHp == maxHp (부활)
            bool complete = await bot.WaitForDeathReviveCycle(
                result.BossEntityId,
                DeathReviveTimeout,
                ct);

            result.MaxHp = bot.ObservedMaxHp;
            result.SawInitialFull = bot.SawInitialFull;
            result.SawDamage = bot.SawDamage;
            result.SawZero = bot.SawZero;
            result.SawReviveFull = bot.SawReviveFull;
            result.HpEventCount = bot.HpEventCount;

            if (!complete)
            {
                string missing = "";
                if (!result.SawInitialFull) missing += " SawInitialFull";
                if (!result.SawDamage)      missing += " SawDamage";
                if (!result.SawZero)        missing += " SawZero";
                if (!result.SawReviveFull)  missing += " SawReviveFull";
                return Fail(result, $"death-revive cycle incomplete within {DeathReviveTimeout.TotalSeconds}s — missing:{missing}");
            }

            result.Success = true;
            return result;
        }
        finally
        {
            bot.Disconnect();
        }
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class HpProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition1 = new(false);
        readonly ManualResetEventSlim _mapTransition2 = new(false);
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HpEvent> _hpEvents = new();

        BotSession? _session;
        uint _clientTick;
        volatile float _bossX = 0f;
        volatile bool _bossXInitialized = false;

        // 4단계 관측 상태 — HandlePacket(네트워크 스레드)에서 갱신, lock 보호.
        bool _sawInitialFull;
        bool _sawDamage;
        bool _sawZero;
        bool _sawReviveFull;
        int _observedMaxHp;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;
        public float SpawnX { get; private set; }

        public bool SawInitialFull  { get { lock (_gate) return _sawInitialFull; } }
        public bool SawDamage       { get { lock (_gate) return _sawDamage; } }
        public bool SawZero         { get { lock (_gate) return _sawZero; } }
        public bool SawReviveFull   { get { lock (_gate) return _sawReviveFull; } }
        public int  ObservedMaxHp   { get { lock (_gate) return _observedMaxHp; } }
        public int  HpEventCount    { get { lock (_gate) return _hpEvents.Count; } }

        public void Connect(string host, int port)
        {
            _connector.Connect(
                new IPEndPoint(IPAddress.Parse(host), port),
                sessionFactory: () =>
                {
                    BotSession s = new();
                    s.OnConnectedCallback = _ =>
                    {
                        _connected.Set();
                        C_Handshake handshake = new() { clientVersion = ProtocolVersion.Current };
                        s.Send(handshake.Write());
                    };
                    s.OnDisconnectedCallback = _ => { };
                    s.OnPacketCallback = HandlePacket;
                    _session = s;
                    return s;
                });
        }

        public bool WaitConnected(TimeSpan timeout) => _connected.Wait(timeout);
        public bool WaitHandshake(TimeSpan timeout) => _handshake.Wait(timeout);
        public bool WaitEnterMap(TimeSpan timeout)  => _enterMap.Wait(timeout);

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition1.IsSet, timeout, ct);

        public async Task<bool> WaitSecondMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition2.IsSet, timeout, ct);

        public async Task<S_EntitySpawn?> WaitForBossSpawn(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == BossEntityKind); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == BossEntityKind);
        }

        // 4단계가 모두 true가 되는 순간 즉시 반환.
        public async Task<bool> WaitForDeathReviveCycle(int bossEntityId, TimeSpan timeout, CancellationToken ct)
        {
            // 재접근 루프와 WaitUntil 병렬 실행:
            // 타임아웃 내에서 주기적으로 재접근을 시도하며 조건을 폴링한다.
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                lock (_gate)
                {
                    if (_sawInitialFull && _sawDamage && _sawZero && _sawReviveFull)
                        return true;
                }

                // 넉백(~1.26유닛)으로 보스 범위 밖으로 밀려났을 수 있으므로 주기적 재접근.
                await EnsureInAttackRangeOnce(ct);
                await Task.Delay(25, ct);
            }

            lock (_gate)
                return _sawInitialFull && _sawDamage && _sawZero && _sawReviveFull;
        }

        // portal 위치까지 서버 권위 X(_serverX) 기반 robust 이동.
        // 매 틱 방향 재계산 — HG 적 hitstun/넉백에 의한 undershoot 방어.
        const int MoveToPortalMaxTicks = 400;
        const float PortalReachRadius = 0.5f;
        volatile float _serverX = 0f;

        public async Task MoveToPortal(float portalX, CancellationToken ct)
        {
            int ticks = 0;
            while (true)
            {
                float sx = _serverX;
                if (Math.Abs(sx - portalX) <= PortalReachRadius)
                    break;

                if (ticks >= MoveToPortalMaxTicks)
                    throw new TimeoutException(
                        $"MoveToPortal: {MoveToPortalMaxTicks}틱 내 포털 미도달. " +
                        $"portalX={portalX}, serverX={sx}");

                sbyte dir = sx < portalX ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
                ticks++;
            }
            SendMove(0);
            await Task.Delay(150, ct);
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal packet = new() { portalId = portalId };
            _session?.Send(packet.Write());
        }

        // standalone 게이트 충족용 DEBUG 치트. cheatType=0 = DebugCompleteQuest.
        public void SendCheatCompleteQuest()
        {
            C_CheatCommand cheat = new() { cheatType = 0 };
            _session?.Send(cheat.Write());
        }

        public async Task MoveIntoBossRange(float bossX, CancellationToken ct)
        {
            float desiredX = bossX > SpawnX
                ? bossX - PreferredAttackDistance
                : bossX + PreferredAttackDistance;
            float delta = desiredX - SpawnX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Knight().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 160);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(250, ct);
        }

        // 1회 재접근 시도 (WaitForDeathReviveCycle 루프 안에서 호출).
        async Task EnsureInAttackRangeOnce(CancellationToken ct)
        {
            if (!_bossXInitialized) return;
            float dist = Math.Abs(SpawnX - _bossX);
            if (dist <= ReapproachThreshold) return;

            sbyte dir = (_bossX >= SpawnX) ? (sbyte)1 : (sbyte)-1;
            for (int t = 0; t < MaxReapproachTicks; t++)
            {
                float d = Math.Abs(SpawnX - _bossX);
                if (d <= ReapproachThreshold) break;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
        }

        public void Disconnect() => _session?.Disconnect();

        void SendMove(sbyte inputX)
        {
            _clientTick++;
            C_MoveIntent move = new()
            {
                input = InputBits.Encode(inputX, jumpPressed: false),
                clientTick = _clientTick,
            };
            _session?.Send(move.Write());
        }

        void HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 4) return;

            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2));
            switch ((PacketID)id)
            {
                case PacketID.S_HandshakeResult:
                    S_HandshakeResult handshake = new();
                    handshake.Read(buffer);
                    HandshakeOk = handshake.ok;
                    HandshakeReason = handshake.reason;
                    if (handshake.ok)
                    {
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Knight };
                        _session?.Send(charSelect.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    SpawnX = enterMap.spawnX;
                    _serverX = enterMap.spawnX; // robust MoveToPortal 초기화.
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition mapTransition = new();
                    mapTransition.Read(buffer);
                    SpawnX = mapTransition.spawnX;
                    _serverX = mapTransition.spawnX; // 새 맵 spawn 좌표로 재동기화.
                    if (!_mapTransition1.IsSet) _mapTransition1.Set();
                    else _mapTransition2.Set();
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (_gate)
                    {
                        _spawns.Add(spawn);
                        if (spawn.entityKind == BossEntityKind)
                        {
                            _bossX = spawn.x;
                            _bossXInitialized = true;
                        }
                    }
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    if (snapshot.entityId == LocalEntityId)
                    {
                        SpawnX = snapshot.x;
                        _serverX = snapshot.x; // robust MoveToPortal 실시간 추적.
                    }
                    break;

                case PacketID.S_EntityState:
                    S_EntityState entityState = new();
                    entityState.Read(buffer);
                    lock (_gate)
                    {
                        bool isBoss = _spawns.Any(s => s.entityId == entityState.entityId && s.entityKind == BossEntityKind);
                        if (isBoss)
                        {
                            _bossX = entityState.x;
                            _bossXInitialized = true;
                        }
                    }
                    break;

                case PacketID.S_PlayerHp:
                    // 자기 자신의 HP 이벤트만 수집한다.
                    // 서버 동작: 보스가 플레이어를 때리면 HP를 감소시키고 S_PlayerHp를 송신.
                    // 사망 시 currentHp=0, 그 직후 부활 시 currentHp=maxHp로 다시 송신.
                    S_PlayerHp playerHp = new();
                    playerHp.Read(buffer);
                    if (playerHp.entityId != LocalEntityId) break;

                    lock (_gate)
                    {
                        _hpEvents.Add(new HpEvent(playerHp.currentHp, playerHp.maxHp));

                        if (playerHp.maxHp > 0 && _observedMaxHp == 0)
                            _observedMaxHp = playerHp.maxHp;

                        // 4단계 상태기계: 순서 보장을 위해 이전 단계 완료 여부를 확인한다.
                        if (!_sawInitialFull)
                        {
                            if (playerHp.maxHp > 0 && playerHp.currentHp == playerHp.maxHp)
                                _sawInitialFull = true;
                        }
                        else if (!_sawDamage)
                        {
                            if (playerHp.currentHp > 0 && playerHp.currentHp < playerHp.maxHp)
                                _sawDamage = true;
                        }
                        else if (!_sawZero)
                        {
                            // SawDamage 이후 HP가 0이면 사망 floor.
                            // (currentHp가 곧바로 0으로 뛰어도 관측한다.)
                            if (playerHp.currentHp == 0)
                                _sawZero = true;
                            else if (playerHp.currentHp < playerHp.maxHp)
                            {
                                // 계속 감소 중 — SawDamage 유지, SawZero 아직.
                            }
                        }
                        else if (!_sawReviveFull)
                        {
                            // SawZero 이후 다시 currentHp == maxHp이면 부활.
                            if (playerHp.maxHp > 0 && playerHp.currentHp == playerHp.maxHp)
                                _sawReviveFull = true;
                        }
                    }
                    break;
            }
        }

        static async Task<bool> WaitUntil(Func<bool> predicate, TimeSpan timeout, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (predicate()) return true;
                await Task.Delay(25, ct);
            }
            return predicate();
        }
    }

    sealed record HpEvent(int CurrentHp, int MaxHp);
}
