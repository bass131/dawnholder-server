using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M3 Phase 07 boss/stage-clear smoke.
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

    public static async Task<Result> Run(
        string host, int port,
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

            int currentHp = result.InitialBossHp;
            int previousHitCount = bot.HitCountFor(result.BossEntityId);
            int deathCountBeforeKill = bot.DeathCountFor(result.BossEntityId);

            for (int attempt = 0; currentHp > 0 && attempt < MaxKillAttempts; attempt++)
            {
                HitEvent? hit = await SendAttackAndWaitForHit(
                    bot,
                    result.BossEntityId,
                    previousHitCount,
                    DefaultTimeout,
                    ct);

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
                bot.SendAttack(result.BossEntityId);
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
        CancellationToken ct)
    {
        bot.SendAttack(targetEntityId);
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
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HitEvent> _hits = new();
        readonly List<int> _deaths = new();
        readonly List<StageClearEvent> _stageClears = new();

        BotSession? _session;
        uint _clientTick;

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

            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (Constants.MoveSpeed * Constants.TickDuration));
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

        public void SendAttack(int targetEntityId)
        {
            C_Attack attack = new() { targetEntityId = targetEntityId };
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
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    SpawnX = enterMap.spawnX;
                    _enterMap.Set();
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (_gate) _spawns.Add(spawn);
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
