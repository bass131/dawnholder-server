using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Emergency combat smoke.
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

    // 타겟이 Chase로 봇 근접까지 수렴하길 기다리는 상한.
    // worst case = patrol 반대편 끝에서 aggro 재진입(half-cycle ~4s) + Chase 접근(~7유닛/2u/s ≈ 3.5s).
    static readonly TimeSpan ChaseConvergeTimeout = TimeSpan.FromSeconds(15);

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

    // simulatedLatencyMs — 봇이 C_Attack 송신 시 attackerClientTick에
    // "N ms 전 서버 tick"을 박아 lag 환경을 시뮬. 0 = zero-lag (기본, 회귀 호환).
    // portal 좌표 상수 — 서버 PortalTable.cs와 정합. 변경 시 양쪽 동기화 의무(불일치=전환 실패).
    // Town portal: x=20 → HuntingGround destSpawn x=2.
    // HuntingGround portal: x=25 → BossRoom destSpawn x=22 (BossProbe에서만 사용).
    const float TownPortalX = 20f;
    const int TownPortalId = 1;

    public static async Task<Result> Run(
        string host, int port,
        int simulatedLatencyMs = 0,
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

            // Town → HuntingGround portal 이동 흐름. Town = 빈 맵이라 portal로 HG 이동 후 전투 시작.
            // ADR-026: entityId는 맵 이동 시 유지 — LocalEntityId 변경 X.
            // 서버는 맵 전환 시 S_MapTransition만 발송 (S_EnterMap 재발송 X) — 수신 = 새 맵 진입 완료 (SpawnX 갱신).
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            bool transitioned = await bot.WaitMapTransition(DefaultTimeout, ct);
            if (!transitioned)
                return Fail(result, "S_MapTransition timeout (5s) — Town→HuntingGround portal");

            // S_MapTransition 후 서버가 enemy roster(S_EntitySpawn)를 발송하기까지 짧은 대기.
            // tick thread에서 EnqueueJob으로 처리되므로 최소 1 tick(50ms) 대기 필요.
            await Task.Delay(Constants.TickIntervalMs * 2, ct);

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

            // lag 시뮬 시 _lastReceivedServerTick이 갱신돼야 함.
            // 이동 중 S_Snapshot을 받지 못한 경우를 대비해 명시적 대기.
            // zero-lag(simulatedLatencyMs=0)도 serverTick 추적을 보장하므로 항상 기다림.
            bool gotSnapshot = await bot.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가");

            // 첫 공격 전 타겟 근접 수렴 대기 (M4.4-03 bake 배치에서 표면화된 race 봉합):
            // 스폰 패킷 x = 관측 시점 patrol 위치(stale). 접근 완료 시점에 타겟이 aggro 밖
            // (patrol 반대편)일 수 있어, 즉시 공격하면 AABB 미스 → S_HitResult 없음.
            // 봇이 patrol 범위 안에 서 있으므로 타겟이 aggro → Chase로 알아서 붙어옴 —
            // live S_EntityState 좌표로 공격 AABB 안 진입을 보장한 뒤 공격.
            bool targetNear = await bot.WaitForTargetWithin(
                result.TargetEntityId, PreferredAttackDistance, ChaseConvergeTimeout, ct);
            if (!targetNear)
                return Fail(result,
                    $"target {result.TargetEntityId} did not converge into attack range " +
                    $"({PreferredAttackDistance}) within {ChaseConvergeTimeout.TotalSeconds}s — " +
                    "aggro/Chase 미동작 또는 스폰 배치 변경 의심");

            HitEvent? firstHit = await SendAttackAndWaitForHit(
                bot,
                result.TargetEntityId,
                bot.HitCountFor(result.TargetEntityId),
                DefaultTimeout,
                ct,
                simulatedLatencyMs);

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
                // rate-limit burst: simulatedLatencyMs 적용 — burst 검증은 lag와 무관하게 rate-limit만 검증.
                bot.SendAttack(result.TargetEntityId, simulatedLatencyMs);
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
                    CooldownWait,
                    ct,
                    simulatedLatencyMs);

                if (hit == null)
                {
                    // rate-limit burst 직후/넉백으로 첫 kill 공격이 쿨다운에 걸려 누락될 수 있음 —
                    // hard-fail 대신 재시도(BossFight/BossStageClear 패턴). MaxKillAttempts 상한이 무한루프 방지.
                    await Task.Delay(CooldownWait, ct);
                    continue;
                }

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

            bot.SendAttack(result.TargetEntityId, simulatedLatencyMs);
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
        CancellationToken ct,
        int simulatedLatencyMs = 0)
    {
        bot.SendAttack(targetEntityId, simulatedLatencyMs);
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

    sealed class CombatProbe : ProbeBase
    {
        readonly ManualResetEventSlim _mapTransition = new(false);
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HitEvent> _hits = new();
        readonly List<int> _deaths = new();

        // entityId → 최신 live x (S_EntityState) — 타겟 근접 수렴 대기용.
        readonly Dictionary<int, float> _entityX = new();

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition.IsSet, timeout, ct);

        // portal 위치(portalX)까지 MoveToPortalCore 기반 robust 이동.
        // ServerX 추적 — HG 적 hitstun/넉백에 의한 undershoot 방어.
        public async Task MoveToPortal(float portalX, CancellationToken ct)
            => await MoveToPortalCore(portalX, ct, maxTicks: 160);

        public void SendEnterPortal(int portalId) => SendEnterPortalCore(portalId);

        public async Task<S_EntitySpawn?> WaitForFirstSpawn(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () =>
                {
                    lock (Gate) return _spawns.Count > 0;
                },
                timeout,
                ct);

            if (!ok) return null;
            lock (Gate) return _spawns[0];
        }

        public async Task<int> MoveIntoAttackRange(float targetX, CancellationToken ct)
        {
            float desiredX = targetX > ServerX
                ? targetX - PreferredAttackDistance
                : targetX + PreferredAttackDistance;
            float delta = desiredX - ServerX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;

            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Knight().MoveSpeed * Constants.TickDuration));
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

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

        // 타겟의 live 위치(S_EntityState)가 봇 현재 위치 기준 maxDist 안에 들어올 때까지 대기.
        public async Task<bool> WaitForTargetWithin(int entityId, float maxDist, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(
                () =>
                {
                    lock (Gate)
                        return _entityX.TryGetValue(entityId, out float x)
                            && Math.Abs(x - ServerX) <= maxDist;
                },
                timeout, ct);

        // simulatedLatencyMs 옵션. 0이면 최신 serverTick 그대로 사용 (zero-lag 시뮬).
        // 양수이면 "N ms 전에 본 serverTick"을 보냄 — 서버 rewind 시뮬.
        //   20 TPS = 1 tick 50ms → latencyTicks = latencyMs / 50.
        //   음수 방지 클램프: Math.Max(0, ...).
        public void SendAttack(int targetEntityId, int simulatedLatencyMs = 0)
        {
            int latencyTicks = simulatedLatencyMs / Constants.TickIntervalMs;
            int clientTick = Math.Max(0, LastReceivedServerTick - latencyTicks);
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = clientTick,
            };
            Session?.Send(attack.Write());
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
            lock (Gate)
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
            lock (Gate) return _hits.Count(h => h.TargetEntityId == targetEntityId);
        }

        public int DeathCountFor(int targetEntityId)
        {
            lock (Gate) return _deaths.Count(entityId => entityId == targetEntityId);
        }

        protected override void OnMapTransition(S_MapTransition packet)
        {
            _mapTransition.Set();
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            switch (id)
            {
                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (Gate) _spawns.Add(spawn);
                    break;

                case PacketID.S_HitResult:
                    S_HitResult hit = new();
                    hit.Read(buffer);
                    lock (Gate)
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
                    lock (Gate) _deaths.Add(death.entityId);
                    break;

                case PacketID.S_EntityState:
                    // enemy live 위치 — 타겟 근접 수렴 대기용.
                    S_EntityState entityState = new();
                    entityState.Read(buffer);
                    lock (Gate) _entityX[entityState.entityId] = entityState.x;
                    break;
            }
        }
    }

    sealed record HitEvent(
        int AttackerEntityId,
        int TargetEntityId,
        int Damage,
        int CurrentHp,
        int MaxHp);
}
