using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M3 Phase 06 emergency combat smoke.
//
// The server owns enemy spawn, range checks, damage, cooldown, and death. The bot
// only performs the same client-visible flow as Unity: handshake, enter map,
// receive an enemy id, move into server-side range, and send C_Attack intents.
public class EmergencyCombatSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan CooldownWait = TimeSpan.FromMilliseconds(550);
    static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(300);

    const float PreferredAttackDistance = 2.0f;
    const int MaxKillAttempts = 10;
    const int RateLimitBurstCount = 5;
    const int RateLimitBurstIntervalMs = 50;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int TargetEntityId;
        public int InitialHp;
        public int FinalHp;
        public int HitCount;
        public int DeathCount;
        public int MoveIntentsSent;
        public bool SawSpawn;
        public bool SawDeath;
        public bool RateLimitDropped;
        public bool UsedOptionBDeathEquivalent;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        CombatProbe bot = new();

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

            S_EntitySpawn? spawn = await bot.WaitForFirstSpawn(DefaultTimeout, ct);
            if (spawn == null)
                return Fail(result, "S_EntitySpawn timeout (5s)");

            string? spawnFailure = ValidateSpawn(spawn);
            if (spawnFailure != null)
                return Fail(result, spawnFailure);

            result.LocalEntityId = bot.LocalEntityId;
            result.TargetEntityId = spawn.entityId;
            result.InitialHp = spawn.currentHp;
            result.FinalHp = spawn.currentHp;
            result.SawSpawn = true;

            result.MoveIntentsSent = await bot.MoveIntoAttackRange(spawn.x, ct);

            HitEvent? firstHit = await SendAttackAndWaitForHit(
                bot,
                result.TargetEntityId,
                bot.HitCountFor(result.TargetEntityId),
                DefaultTimeout,
                ct);

            if (firstHit == null)
            {
                return Fail(
                    result,
                    "S_HitResult timeout after first C_Attack; bot may still be out of range");
            }

            string? hitFailure = ValidateHit(
                firstHit,
                result.LocalEntityId,
                result.TargetEntityId,
                result.InitialHp);
            if (hitFailure != null)
                return Fail(result, hitFailure);

            result.FinalHp = firstHit.CurrentHp;

            int hitCountBeforeBurst = bot.HitCountFor(result.TargetEntityId);
            int deathCountBeforeBurst = bot.DeathCountFor(result.TargetEntityId);

            for (int i = 0; i < RateLimitBurstCount; i++)
            {
                bot.SendAttack(result.TargetEntityId);
                if (i < RateLimitBurstCount - 1)
                    await Task.Delay(RateLimitBurstIntervalMs, ct);
            }

            await Task.Delay(QuietWindow, ct);

            int hitCountAfterBurst = bot.HitCountFor(result.TargetEntityId);
            int deathCountAfterBurst = bot.DeathCountFor(result.TargetEntityId);
            if (hitCountAfterBurst != hitCountBeforeBurst)
            {
                return Fail(
                    result,
                    $"rate-limit burst produced extra S_HitResult(s): before={hitCountBeforeBurst}, after={hitCountAfterBurst}");
            }
            if (deathCountAfterBurst != deathCountBeforeBurst)
            {
                return Fail(
                    result,
                    $"rate-limit burst produced S_EntityDeath: before={deathCountBeforeBurst}, after={deathCountAfterBurst}");
            }
            result.RateLimitDropped = true;

            await Task.Delay(CooldownWait, ct);

            int currentHp = result.FinalHp;
            int previousHitCount = bot.HitCountFor(result.TargetEntityId);
            int deathCountBeforeKillFlow = bot.DeathCountFor(result.TargetEntityId);

            for (int attempt = 0; currentHp > 0 && attempt < MaxKillAttempts; attempt++)
            {
                HitEvent? hit = await SendAttackAndWaitForHit(
                    bot,
                    result.TargetEntityId,
                    previousHitCount,
                    DefaultTimeout,
                    ct);

                if (hit == null)
                    return Fail(result, $"S_HitResult timeout during kill flow at attempt {attempt + 1}");

                hitFailure = ValidateHit(hit, result.LocalEntityId, result.TargetEntityId, currentHp);
                if (hitFailure != null)
                    return Fail(result, hitFailure);

                currentHp = hit.CurrentHp;
                result.FinalHp = currentHp;
                previousHitCount = bot.HitCountFor(result.TargetEntityId);

                if (currentHp > 0)
                    await Task.Delay(CooldownWait, ct);
            }

            if (currentHp > 0)
                return Fail(result, $"target still alive after {MaxKillAttempts} kill attempts: hp={currentHp}");

            int expectedDeathCount = deathCountBeforeKillFlow + 1;
            bool sawDeath = await bot.WaitForDeathCount(
                result.TargetEntityId,
                expectedDeathCount,
                QuietWindow,
                ct);

            result.SawDeath = sawDeath || bot.DeathCountFor(result.TargetEntityId) > 0;
            result.UsedOptionBDeathEquivalent = !result.SawDeath && currentHp <= 0;

            int deathCountAfterKill = bot.DeathCountFor(result.TargetEntityId);
            await Task.Delay(QuietWindow, ct);
            int deathCountAfterQuiet = bot.DeathCountFor(result.TargetEntityId);
            if (deathCountAfterQuiet != deathCountAfterKill)
            {
                return Fail(
                    result,
                    $"duplicate S_EntityDeath received: before={deathCountAfterKill}, after={deathCountAfterQuiet}");
            }

            int hitCountBeforeDeadRetarget = bot.HitCountFor(result.TargetEntityId);
            int deathCountBeforeDeadRetarget = bot.DeathCountFor(result.TargetEntityId);

            bot.SendAttack(result.TargetEntityId);
            await Task.Delay(QuietWindow, ct);

            int hitCountAfterDeadRetarget = bot.HitCountFor(result.TargetEntityId);
            int deathCountAfterDeadRetarget = bot.DeathCountFor(result.TargetEntityId);
            if (hitCountAfterDeadRetarget != hitCountBeforeDeadRetarget)
            {
                return Fail(
                    result,
                    $"dead target re-attack produced S_HitResult: before={hitCountBeforeDeadRetarget}, after={hitCountAfterDeadRetarget}");
            }
            if (deathCountAfterDeadRetarget != deathCountBeforeDeadRetarget)
            {
                return Fail(
                    result,
                    $"dead target re-attack produced S_EntityDeath: before={deathCountBeforeDeadRetarget}, after={deathCountAfterDeadRetarget}");
            }

            result.HitCount = bot.HitCountFor(result.TargetEntityId);
            result.DeathCount = bot.DeathCountFor(result.TargetEntityId);
            result.Success = true;
            return result;
        }
        finally
        {
            bot.Disconnect();
        }
    }

    static async Task<HitEvent?> SendAttackAndWaitForHit(
        CombatProbe bot,
        int targetEntityId,
        int previousHitCount,
        TimeSpan timeout,
        CancellationToken ct)
    {
        bot.SendAttack(targetEntityId);
        return await bot.WaitForHitCount(targetEntityId, previousHitCount + 1, timeout, ct);
    }

    static string? ValidateSpawn(S_EntitySpawn spawn)
    {
        if (spawn.entityId <= 0)
            return $"invalid S_EntitySpawn.entityId: {spawn.entityId}";

        if (spawn.currentHp <= 0)
            return $"invalid S_EntitySpawn.currentHp: {spawn.currentHp}";

        if (spawn.maxHp <= 0)
            return $"invalid S_EntitySpawn.maxHp: {spawn.maxHp}";

        if (spawn.currentHp > spawn.maxHp)
            return $"S_EntitySpawn currentHp exceeds maxHp: {spawn.currentHp}/{spawn.maxHp}";

        return null;
    }

    static string? ValidateHit(
        HitEvent hit,
        int localEntityId,
        int targetEntityId,
        int previousHp)
    {
        if (hit.AttackerEntityId != localEntityId)
        {
            return
                $"S_HitResult attacker mismatch: expected={localEntityId}, actual={hit.AttackerEntityId}";
        }

        if (hit.TargetEntityId != targetEntityId)
        {
            return
                $"S_HitResult target mismatch: expected={targetEntityId}, actual={hit.TargetEntityId}";
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

    sealed class CombatProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HitEvent> _hits = new();
        readonly List<int> _deaths = new();

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

        public async Task<S_EntitySpawn?> WaitForFirstSpawn(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () =>
                {
                    lock (_gate) return _spawns.Count > 0;
                },
                timeout,
                ct);

            if (!ok) return null;
            lock (_gate) return _spawns[0];
        }

        public async Task<int> MoveIntoAttackRange(float targetX, CancellationToken ct)
        {
            float desiredX = targetX > SpawnX
                ? targetX - PreferredAttackDistance
                : targetX + PreferredAttackDistance;
            float delta = desiredX - SpawnX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;

            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (Constants.MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 80);

            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }

            SendMove(0);
            await Task.Delay(150, ct);
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

        public int HitCountFor(int targetEntityId)
        {
            lock (_gate) return _hits.Count(h => h.TargetEntityId == targetEntityId);
        }

        public int DeathCountFor(int targetEntityId)
        {
            lock (_gate) return _deaths.Count(entityId => entityId == targetEntityId);
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
                    // M4.1 Phase 02 (P0-1/P0-2 봉합): handshake OK 후 즉시 C_CharacterSelect 송신.
                    if (handshake.ok)
                    {
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Warrior };
                        _session?.Send(charSelect.Write());
                    }
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
}
