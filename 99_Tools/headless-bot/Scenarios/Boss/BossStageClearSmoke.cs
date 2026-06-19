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

            // standalone 보스 게이트 충족: C_CheatCommand{cheatType=0} → 서버 DEBUG 치트 경로(DebugCompleteQuest).
            // killCount를 게이트 임계로 즉시 세팅 → HG→BossRoom 포탈 통과.
            // 서버는 #if DEBUG 빌드에서만 처리. standalone 회귀는 DEBUG 빌드 전용.
            // (xUnit은 이 파일을 seedBossGate 파라미터 없이 직접 호출 — 치트는 idempotent(latch)라 무해.)
#if DEBUG
            bot.SendCheatCompleteQuest();
            await Task.Delay(Constants.TickIntervalMs * 3, ct);
#endif

            // 2회차 portal — HuntingGround → BossRoom.
            // BossRoom portal(x=25)까지 이동. HG destSpawn(x=2)에서 시작.
            // robust MoveToPortal(_serverX 추적) — HG 적 hitstun/넉백에 의한 undershoot 방어.
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
                    CooldownWait,
                    ct,
                    simulatedLatencyMs);

                if (hit == null)
                {
                    // 보스 쿨다운/넉백으로 히트가 누락될 수 있음 — hard-fail 대신 재시도(BossFight 패턴).
                    // MaxKillAttempts(20) 상한이 무한루프 방지. 보스 HP 150 ÷ ~15dmg = ~10타라 여유.
                    await Task.Delay(CooldownWait, ct);
                    continue;
                }

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
            return $"S_HitResult attacker mismatch: expected={localEntityId}, actual={hit.AttackerEntityId}";

        if (hit.TargetEntityId != bossEntityId)
            return $"S_HitResult target mismatch: expected={bossEntityId}, actual={hit.TargetEntityId}";

        if (hit.Damage <= 0)
            return $"S_HitResult damage must be positive: {hit.Damage}";

        int expectedHp = previousHp - hit.Damage;
        if (hit.CurrentHp != expectedHp)
            return $"S_HitResult HP mismatch: previous={previousHp}, damage={hit.Damage}, expected={expectedHp}, actual={hit.CurrentHp}";

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

    sealed class BossProbe : ProbeBase
    {
        readonly ManualResetEventSlim _mapTransition1 = new(false);
        readonly ManualResetEventSlim _mapTransition2 = new(false);
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HitEvent> _hits = new();
        readonly List<int> _deaths = new();
        readonly List<StageClearEvent> _stageClears = new();

        // 보스 서버 권위 X — S_EntityState 수신 시 갱신.
        volatile float _bossX = 0f;
        volatile bool _bossXInitialized = false;

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition1.IsSet, timeout, ct);
        public async Task<bool> WaitSecondMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition2.IsSet, timeout, ct);

        public async Task MoveToPortal(float portalX, CancellationToken ct)
            => await MoveToPortalCore(portalX, ct);

        public void SendEnterPortal(int portalId) => SendEnterPortalCore(portalId);
        public void SendCheatCompleteQuest() => SendCheatCompleteQuestCore();

        public async Task<S_EntitySpawn?> WaitForBossSpawn(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (Gate) return _spawns.Any(s => s.entityKind == BossEntityKind); },
                timeout, ct);
            if (!ok) return null;
            lock (Gate) return _spawns.First(s => s.entityKind == BossEntityKind);
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

        // 공격 전 재접근 보장.
        public async Task<bool> EnsureInAttackRange(CancellationToken ct)
        {
            for (int t = 0; t < MaxReapproachTicks; t++)
            {
                float playerX = ServerX;
                float targetX = _bossXInitialized ? _bossX : playerX;
                float dist = Math.Abs(playerX - targetX);
                if (dist <= ReapproachThreshold)
                    return true;

                sbyte dir = (targetX >= playerX) ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }

            SendMove(0);
            await Task.Delay(Constants.TickIntervalMs, ct);
            float finalDist = Math.Abs(ServerX - (_bossXInitialized ? _bossX : 0f));
            return finalDist <= ReapproachThreshold;
        }

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

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

        public async Task<HitEvent?> WaitForHitCount(int targetEntityId, int minCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => HitCountFor(targetEntityId) >= minCount, timeout, ct);
            if (!ok) return null;
            lock (Gate) return _hits.LastOrDefault(h => h.TargetEntityId == targetEntityId);
        }

        public async Task<bool> WaitForDeathCount(int targetEntityId, int minCount, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => DeathCountFor(targetEntityId) >= minCount, timeout, ct);

        public async Task<StageClearEvent?> WaitForStageClearCount(int bossEntityId, int minCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => StageClearCountFor(bossEntityId) >= minCount, timeout, ct);
            if (!ok) return null;
            lock (Gate) return _stageClears.LastOrDefault(s => s.BossEntityId == bossEntityId);
        }

        public int HitCountFor(int targetEntityId)
        {
            lock (Gate) return _hits.Count(h => h.TargetEntityId == targetEntityId);
        }

        public int DeathCountFor(int targetEntityId)
        {
            lock (Gate) return _deaths.Count(entityId => entityId == targetEntityId);
        }

        public int StageClearCountFor(int bossEntityId)
        {
            lock (Gate) return _stageClears.Count(s => s.BossEntityId == bossEntityId);
        }

        protected override void OnMapTransition(S_MapTransition packet)
        {
            if (!_mapTransition1.IsSet) _mapTransition1.Set();
            else _mapTransition2.Set();
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            switch (id)
            {
                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (Gate)
                    {
                        _spawns.Add(spawn);
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
                    lock (Gate)
                        _hits.Add(new HitEvent(hit.attackerEntityId, hit.targetEntityId, hit.damage, hit.currentHp, hit.maxHp));
                    break;

                case PacketID.S_EntityDeath:
                    S_EntityDeath death = new();
                    death.Read(buffer);
                    lock (Gate) _deaths.Add(death.entityId);
                    break;

                case PacketID.S_StageClear:
                    S_StageClear stageClear = new();
                    stageClear.Read(buffer);
                    lock (Gate) _stageClears.Add(new StageClearEvent(stageClear.bossEntityId));
                    break;

                case PacketID.S_EntityState:
                    S_EntityState entityState = new();
                    entityState.Read(buffer);
                    lock (Gate)
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
    }

    sealed record HitEvent(int AttackerEntityId, int TargetEntityId, int Damage, int CurrentHp, int MaxHp);
    sealed record StageClearEvent(int BossEntityId);
}
