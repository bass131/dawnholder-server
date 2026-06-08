using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Boss/stage-clear smoke.
//
// This intentionally drives the public client protocol only. The bot moves into
// boss range with C_MoveIntent, attacks with C_Attack, then verifies server-owned
// death and S_StageClear idempotency.
public class BossStageClearSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan CooldownWait = TimeSpan.FromMilliseconds(550);
    static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(500);

    const byte BossEntityKind = 1;
    const float PreferredAttackDistance = 2.0f;
    // 재접근 임계값: AttackHalfExtent=1.5보다 여유를 두어 넉백(~1.26) 후에도 확실히 재진입.
    const float ReapproachThreshold = 1.0f;
    const int MaxReapproachTicks = 30;
    const int MaxKillAttempts = 20;
    const int DeadTargetRetargetCount = 3;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int BossEntityId;
        public int InitialBossHp;
        public int FinalBossHp;
        public int HitCount;
        public int DeathCount;
        public int StageClearCount;
        public int MoveIntentsSent;
        public bool SawBossSpawn;
        public bool SawBossDeath;
        public bool SawStageClear;
        public bool DuplicateSuppressed;
        public bool UsedOptionBDeathEquivalent;
    }

    // portal 좌표 상수 — 서버 PortalTable.cs와 정합. 변경 시 양쪽 동기화 의무(불일치=전환 실패).
    // Town portal: x=20 → HuntingGround destSpawn x=2.
    // HuntingGround portal: x=25 → BossRoom destSpawn x=22.
    const float TownPortalX = 20f;
    const int TownPortalId = 1;
    const float HGPortalX = 25f;
    const int HGPortalId = 1;

    public static async Task<Result> Run(
        string host, int port,
        int simulatedLatencyMs = 0,
        CancellationToken ct = default)
    {
        Result result = new();
        BossProbe bot = new();

        try
        {
            bot.Connect(host, port);

            if (!bot.WaitConnected(DefaultTimeout))
                return Fail(result, "connect timeout (5s)");

            if (!bot.WaitHandshake(DefaultTimeout))
                return Fail(result, "S_HandshakeResult timeout (5s)");

            if (!bot.HandshakeOk)
                return Fail(result, $"handshake rejected: {bot.HandshakeReason}");

            if (!bot.WaitEnterMap(DefaultTimeout))
                return Fail(result, "S_EnterMap timeout (5s)");

            // 1회차 portal — Town → HuntingGround. Town = 빈 맵이라 HG 경유 후 BossRoom으로.
            // ADR-026: entityId는 맵 이동 시 유지. 서버는 맵 전환 시 S_MapTransition만 발송 (S_EnterMap 재발송 X).
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout (5s) — Town→HuntingGround portal");

            // S_MapTransition 후 서버 tick thread가 다음 맵 job을 처리하기까지 대기.
            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // 2회차 portal — HuntingGround → BossRoom.
            // BossRoom portal(x=25)까지 이동. HG destSpawn(x=2)에서 시작.
            await bot.MoveToPortal(HGPortalX, ct);
            bot.SendEnterPortal(HGPortalId);
            if (!await bot.WaitSecondMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout (5s) — HuntingGround→BossRoom portal");

            // S_MapTransition 후 서버 tick thread 처리 대기.
            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? bossSpawn = await bot.WaitForBossSpawn(DefaultTimeout, ct);
            if (bossSpawn == null)
                return Fail(result, "Boss S_EntitySpawn timeout (5s)");

            string? spawnFailure = ValidateBossSpawn(bossSpawn);
            if (spawnFailure != null)
                return Fail(result, spawnFailure);

            result.LocalEntityId = bot.LocalEntityId;
            result.BossEntityId = bossSpawn.entityId;
            result.InitialBossHp = bossSpawn.currentHp;
            result.FinalBossHp = bossSpawn.currentHp;
            result.SawBossSpawn = true;

            result.MoveIntentsSent = await bot.MoveIntoAttackRange(bossSpawn.x, ct);

            // lag 시뮬 시 serverTick 추적 보장.
            bool gotSnapshot = await bot.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가");

            int currentHp = result.InitialBossHp;
            int previousHitCount = bot.HitCountFor(result.BossEntityId);
            int deathCountBeforeKill = bot.DeathCountFor(result.BossEntityId);

            for (int attempt = 0; currentHp > 0 && attempt < MaxKillAttempts; attempt++)
            {
                // 넉백(~1.26유닛)으로 공격 범위(1.5) 밖으로 밀려났을 수 있으므로 공격 전 재접근.
                bool inRange = await bot.EnsureInAttackRange(ct);
                if (!inRange)
                    return Fail(result, $"reapproach timeout at attempt {attempt + 1} — bot could not close to boss");

                HitEvent? hit = await SendAttackAndWaitForHit(
                    bot,
                    result.BossEntityId,
                    previousHitCount,
                    DefaultTimeout,
                    ct,
                    simulatedLatencyMs);

                if (hit == null)
                    return Fail(result, $"S_HitResult timeout during boss kill at attempt {attempt + 1}");

                string? hitFailure = ValidateHit(
                    hit,
                    result.LocalEntityId,
                    result.BossEntityId,
                    currentHp);
                if (hitFailure != null)
                    return Fail(result, hitFailure);

                currentHp = hit.CurrentHp;
                result.FinalBossHp = currentHp;
                previousHitCount = bot.HitCountFor(result.BossEntityId);

                if (currentHp > 0 && bot.StageClearCountFor(result.BossEntityId) > 0)
                    return Fail(result, "S_StageClear arrived before boss death-equivalent HP reached 0");

                if (currentHp > 0)
                    await Task.Delay(CooldownWait, ct);
            }

            if (currentHp > 0)
                return Fail(result, $"boss still alive after {MaxKillAttempts} attacks: hp={currentHp}");

            int expectedDeathCount = deathCountBeforeKill + 1;
            bool sawDeath = await bot.WaitForDeathCount(
                result.BossEntityId,
                expectedDeathCount,
                QuietWindow,
                ct);

            result.SawBossDeath = sawDeath || bot.DeathCountFor(result.BossEntityId) > 0;
            result.UsedOptionBDeathEquivalent = !result.SawBossDeath && currentHp <= 0;

            StageClearEvent? stageClear = await bot.WaitForStageClearCount(
                result.BossEntityId,
                1,
                DefaultTimeout,
                ct);

            if (stageClear == null)
                return Fail(result, "S_StageClear timeout after boss death");

            if (stageClear.BossEntityId != result.BossEntityId)
            {
                return Fail(
                    result,
                    $"S_StageClear boss mismatch: expected={result.BossEntityId}, actual={stageClear.BossEntityId}");
            }

            result.SawStageClear = true;

            int hitCountAfterClear = bot.HitCountFor(result.BossEntityId);
            int deathCountAfterClear = bot.DeathCountFor(result.BossEntityId);
            int stageClearCountAfterClear = bot.StageClearCountFor(result.BossEntityId);

            for (int i = 0; i < DeadTargetRetargetCount; i++)
            {
                bot.SendAttack(result.BossEntityId, simulatedLatencyMs);
                await Task.Delay(50, ct);
            }

            await Task.Delay(QuietWindow, ct);

            int hitCountAfterRetarget = bot.HitCountFor(result.BossEntityId);
            int deathCountAfterRetarget = bot.DeathCountFor(result.BossEntityId);
            int stageClearCountAfterRetarget = bot.StageClearCountFor(result.BossEntityId);

            if (hitCountAfterRetarget != hitCountAfterClear)
            {
                return Fail(
                    result,
                    $"dead boss re-attack produced S_HitResult: before={hitCountAfterClear}, after={hitCountAfterRetarget}");
            }
            if (deathCountAfterRetarget != deathCountAfterClear)
            {
                return Fail(
                    result,
                    $"dead boss re-attack produced S_EntityDeath: before={deathCountAfterClear}, after={deathCountAfterRetarget}");
            }
            if (stageClearCountAfterRetarget != stageClearCountAfterClear)
            {
                return Fail(
                    result,
                    $"dead boss re-attack produced duplicate S_StageClear: before={stageClearCountAfterClear}, after={stageClearCountAfterRetarget}");
            }

            result.HitCount = bot.HitCountFor(result.BossEntityId);
            result.DeathCount = bot.DeathCountFor(result.BossEntityId);
            result.StageClearCount = bot.StageClearCountFor(result.BossEntityId);
            result.DuplicateSuppressed = true;
            result.Success = true;
            return result;
        }
        finally
        {
            bot.Disconnect();
        }
    }

    static async Task<HitEvent?> SendAttackAndWaitForHit(
        BossProbe bot,
        int targetEntityId,
        int previousHitCount,
        TimeSpan timeout,
        CancellationToken ct,
        int simulatedLatencyMs = 0)
    {
        bot.SendAttack(targetEntityId, simulatedLatencyMs);
        return await bot.WaitForHitCount(targetEntityId, previousHitCount + 1, timeout, ct);
    }

    static string? ValidateBossSpawn(S_EntitySpawn spawn)
    {
        if (spawn.entityId <= 0)
            return $"invalid boss entityId: {spawn.entityId}";

        if (spawn.entityKind != BossEntityKind)
            return $"invalid boss entityKind: expected={BossEntityKind}, actual={spawn.entityKind}";

        if (spawn.currentHp <= 0)
            return $"invalid boss currentHp: {spawn.currentHp}";

        if (spawn.maxHp <= 0)
            return $"invalid boss maxHp: {spawn.maxHp}";

        if (spawn.currentHp > spawn.maxHp)
            return $"boss currentHp exceeds maxHp: {spawn.currentHp}/{spawn.maxHp}";

        return null;
    }

    static string? ValidateHit(
        HitEvent hit,
        int localEntityId,
        int bossEntityId,
        int previousHp)
    {
        if (hit.AttackerEntityId != localEntityId)
        {
            return
                $"S_HitResult attacker mismatch: expected={localEntityId}, actual={hit.AttackerEntityId}";
        }

        if (hit.TargetEntityId != bossEntityId)
        {
            return
                $"S_HitResult target mismatch: expected={bossEntityId}, actual={hit.TargetEntityId}";
        }

        if (hit.Damage <= 0)
            return $"S_HitResult damage must be positive: {hit.Damage}";

        int expectedHp = previousHp - hit.Damage;
        if (hit.CurrentHp != expectedHp)
        {
            return
                $"S_HitResult HP mismatch: previous={previousHp}, damage={hit.Damage}, expected={expectedHp}, actual={hit.CurrentHp}";
        }

        if (hit.CurrentHp > hit.MaxHp)
            return $"S_HitResult currentHp exceeds maxHp: {hit.CurrentHp}/{hit.MaxHp}";

        if (hit.MaxHp <= 0)
            return $"S_HitResult maxHp must be positive: {hit.MaxHp}";

        return null;
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class BossProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        // 1회차/2회차 S_MapTransition 각각 관리.
        // 서버는 맵 전환 시 S_MapTransition만 발송 (S_EnterMap 재발송 X).
        readonly ManualResetEventSlim _mapTransition1 = new(false);
        readonly ManualResetEventSlim _mapTransition2 = new(false);
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HitEvent> _hits = new();
        readonly List<int> _deaths = new();
        readonly List<StageClearEvent> _stageClears = new();

        BotSession? _session;
        uint _clientTick;

        // 서버 tick 추적 — S_Snapshot 수신 시 갱신.
        // C_Attack.attackerClientTick에 박아야 서버 rewind 범위 검증(diff ≤ 4)을 통과.
        // volatile: network thread(HandlePacket)에서 쓰고 메인 시나리오 thread(SendAttack)에서 읽음.
        volatile int _lastReceivedServerTick = 0;

        // 봇 서버 권위 X — S_Snapshot(entityId==LocalEntityId) 수신 시 갱신.
        // 넉백 후 재접근 헬퍼가 실제 서버 위치 기준으로 방향/틱을 계산하기 위해 사용.
        volatile float _serverX = 0f;

        // 보스 서버 권위 X — S_EntityState 수신 시 갱신.
        // 초기 MoveIntoAttackRange의 스폰 좌표(stale 가능)를 보완해 재접근에 활용.
        volatile float _bossX = 0f;
        volatile bool _bossXInitialized = false;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;
        public float SpawnX { get; private set; }

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
        public bool WaitEnterMap(TimeSpan timeout) => _enterMap.Wait(timeout);

        // 서버는 맵 전환 시 S_MapTransition만 발송 — 이 이벤트 수신으로 맵 진입 완료 판정.
        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition1.IsSet, timeout, ct);
        public async Task<bool> WaitSecondMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition2.IsSet, timeout, ct);

        // portal 위치까지 C_MoveIntent 기반 이동. portal x 좌표는 호출부 const(PortalTable.cs와 정합).
        public async Task<int> MoveToPortal(float portalX, CancellationToken ct)
        {
            float delta = portalX - SpawnX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Knight().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 160);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
            return ticks + 1;
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal packet = new() { portalId = portalId };
            _session?.Send(packet.Write());
        }

        public async Task<S_EntitySpawn?> WaitForBossSpawn(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () =>
                {
                    lock (_gate) return _spawns.Any(s => s.entityKind == BossEntityKind);
                },
                timeout,
                ct);

            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == BossEntityKind);
        }

        public async Task<int> MoveIntoAttackRange(float targetX, CancellationToken ct)
        {
            float desiredX = targetX > SpawnX
                ? targetX - PreferredAttackDistance
                : targetX + PreferredAttackDistance;
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
            return ticks + 1;
        }

        // 공격 전 재접근 보장. 넉백으로 범위 밖에 있으면 보스 쪽으로 조향해 ReapproachThreshold 안에 들어온다.
        // HitState 이동잠금(8틱) 중에 SendMove를 보내도 서버가 무시 → 서버 위치가 수렴하면 자연히 범위 내.
        // 상한 MaxReapproachTicks 초과 시 false 반환 (호출부가 실패 처리).
        public async Task<bool> EnsureInAttackRange(CancellationToken ct)
        {
            for (int t = 0; t < MaxReapproachTicks; t++)
            {
                float playerX = _serverX;
                float targetX = _bossXInitialized ? _bossX : playerX;
                float dist = Math.Abs(playerX - targetX);
                if (dist <= ReapproachThreshold)
                    return true;

                sbyte dir = (targetX >= playerX) ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }

            SendMove(0);
            // 마지막 한 번 더 체크 (마지막 이동이 서버에서 반영되기까지 짧은 대기).
            await Task.Delay(Constants.TickIntervalMs, ct);
            float finalDist = Math.Abs(_serverX - (_bossXInitialized ? _bossX : 0f));
            return finalDist <= ReapproachThreshold;
        }

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        public void SendAttack(int targetEntityId, int simulatedLatencyMs = 0)
        {
            int latencyTicks = simulatedLatencyMs / Constants.TickIntervalMs;
            int clientTick = Math.Max(0, _lastReceivedServerTick - latencyTicks);
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = clientTick,
            };
            _session?.Send(attack.Write());
        }

        public async Task<HitEvent?> WaitForHitCount(
            int targetEntityId,
            int minCount,
            TimeSpan timeout,
            CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => HitCountFor(targetEntityId) >= minCount,
                timeout,
                ct);

            if (!ok) return null;
            lock (_gate)
                return _hits.LastOrDefault(h => h.TargetEntityId == targetEntityId);
        }

        public async Task<bool> WaitForDeathCount(
            int targetEntityId,
            int minCount,
            TimeSpan timeout,
            CancellationToken ct)
            => await WaitUntil(() => DeathCountFor(targetEntityId) >= minCount, timeout, ct);

        public async Task<StageClearEvent?> WaitForStageClearCount(
            int bossEntityId,
            int minCount,
            TimeSpan timeout,
            CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => StageClearCountFor(bossEntityId) >= minCount,
                timeout,
                ct);

            if (!ok) return null;
            lock (_gate)
                return _stageClears.LastOrDefault(s => s.BossEntityId == bossEntityId);
        }

        public int HitCountFor(int targetEntityId)
        {
            lock (_gate) return _hits.Count(h => h.TargetEntityId == targetEntityId);
        }

        public int DeathCountFor(int targetEntityId)
        {
            lock (_gate) return _deaths.Count(entityId => entityId == targetEntityId);
        }

        public int StageClearCountFor(int bossEntityId)
        {
            lock (_gate) return _stageClears.Count(s => s.BossEntityId == bossEntityId);
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
                    // handshake OK 후 즉시 C_CharacterSelect 송신.
                    // 서버가 class 선택 없이 월드 진입을 차단하므로 S_EnterMap은 이 패킷 후에야 옴.
                    if (handshake.ok)
                    {
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Knight };
                        _session?.Send(charSelect.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    // 서버는 최초 진입 시에만 S_EnterMap 발송. 맵 전환 시엔 S_MapTransition만 발송.
                    // ADR-026: entityId는 맵 이동 시 유지 — S_EnterMap은 최초 1회만 수신.
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    SpawnX = enterMap.spawnX;
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    // SpawnX를 목적지 spawn 좌표로 갱신. 봇은 서버 권위 좌표만 사용 (헌법 #1).
                    S_MapTransition mapTransition = new();
                    mapTransition.Read(buffer);
                    SpawnX = mapTransition.spawnX;
                    if (!_mapTransition1.IsSet)
                        _mapTransition1.Set();
                    else
                        _mapTransition2.Set();
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (_gate)
                    {
                        _spawns.Add(spawn);
                        // 보스 스폰 시 초기 좌표 캐시 — S_EntityState 수신 전에도 재접근 기준으로 활용.
                        if (spawn.entityKind == BossEntityKind)
                        {
                            _bossX = spawn.x;
                            _bossXInitialized = true;
                        }
                    }
                    break;

                case PacketID.S_HitResult:
                    S_HitResult hit = new();
                    hit.Read(buffer);
                    lock (_gate)
                    {
                        _hits.Add(new HitEvent(
                            hit.attackerEntityId,
                            hit.targetEntityId,
                            hit.damage,
                            hit.currentHp,
                            hit.maxHp));
                    }
                    break;

                case PacketID.S_EntityDeath:
                    S_EntityDeath death = new();
                    death.Read(buffer);
                    lock (_gate) _deaths.Add(death.entityId);
                    break;

                case PacketID.S_StageClear:
                    S_StageClear stageClear = new();
                    stageClear.Read(buffer);
                    lock (_gate)
                        _stageClears.Add(new StageClearEvent(stageClear.bossEntityId));
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    _lastReceivedServerTick = snapshot.serverTick;
                    // 자기 자신 snapshot → 봇 서버 권위 위치 갱신 (재접근 헬퍼 기준 좌표).
                    if (snapshot.entityId == LocalEntityId)
                        _serverX = snapshot.x;
                    break;

                case PacketID.S_EntityState:
                    // 보스 live 위치 — 재접근 헬퍼가 최신 보스 좌표로 방향 계산.
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
            }
        }

        static async Task<bool> WaitUntil(
            Func<bool> predicate,
            TimeSpan timeout,
            CancellationToken ct)
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

    sealed record HitEvent(
        int AttackerEntityId,
        int TargetEntityId,
        int Damage,
        int CurrentHp,
        int MaxHp);

    sealed record StageClearEvent(int BossEntityId);
}
