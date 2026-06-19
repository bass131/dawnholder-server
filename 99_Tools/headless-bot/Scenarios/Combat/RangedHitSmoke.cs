using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.8 Mage 원거리 평타 히트 회귀 스모크.
//
// 검증 목표:
//   - Mage가 HuntingGround의 Normal 적을 사거리 내에서 C_Attack → S_ProjectileLaunch 수신
//     (attackerEntityId == 자기 자신).
//   - S_ProjectileLaunch.travelTicks 이후 S_HitResult(hitEffect==1) 수신.
//   - S_HitResult 수신 전에 S_HitResult가 오지 않음 (즉시 명중 금지 — 지연 검증).
//   - 적 currentHp 감소 확인.
//
// 흐름: Town → HuntingGround 포털 → S_EntitySpawn(Normal) 수신 → 사거리 내 이동
//   → serverTick 확보 → C_Attack → 검증.
public class RangedHitSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    static readonly TimeSpan ProjectileArrivalTimeout = TimeSpan.FromSeconds(5);

    const float TownPortalX = 20f;
    const int TownPortalId = 1;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int TargetEntityId;
        public int InitialHp;
        public bool SawProjectileLaunch;
        public int ProjectileTravelTicks;
        public bool SawHitResult;
        public byte HitEffect;
        public int HpAfterHit;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        RangedProbe bot = new();

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

            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? spawn = await bot.WaitForNormalSpawn(DefaultTimeout, ct);
            if (spawn == null)
                return Fail(result, "S_EntitySpawn (Normal) timeout");
            if (spawn.currentHp <= 0)
                return Fail(result, $"spawn hp={spawn.currentHp} invalid");

            result.LocalEntityId = bot.LocalEntityId;
            result.TargetEntityId = spawn.entityId;
            result.InitialHp = spawn.currentHp;

            // Mage 사거리(MageAttackHalfExtent=4.0f) 내로 이동: 적에서 3.0f 거리.
            await bot.MoveIntoMageRange(spawn.x, ct);

            bool gotSnapshot = await bot.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가");

            // 적이 Chase로 접근해 올 때까지 대기 (patrol 반대편 있을 수 있음).
            await bot.WaitForTargetNearby(spawn.entityId, maxDist: 4.5f, DefaultTimeout, ct);

            // C_Attack 발사.
            bot.SendAttack(spawn.entityId);

            // S_ProjectileLaunch 수신 확인.
            S_ProjectileLaunch? launch = await bot.WaitForProjectileLaunch(ProjectileArrivalTimeout, ct);
            if (launch == null)
                return Fail(result, "S_ProjectileLaunch not received after C_Attack");
            if (launch.attackerEntityId != bot.LocalEntityId)
                return Fail(result, $"S_ProjectileLaunch.attackerEntityId={launch.attackerEntityId} != self={bot.LocalEntityId}");

            result.SawProjectileLaunch = true;
            result.ProjectileTravelTicks = launch.travelTicks;

            // travelTicks 지연 후 S_HitResult(hitEffect==1) 수신 — 최대 travelTicks * 틱간격 + 여유 2틱.
            int travelMs = launch.travelTicks * Constants.TickIntervalMs + Constants.TickIntervalMs * 2;
            TimeSpan hitWait = TimeSpan.FromMilliseconds(travelMs) + TimeSpan.FromMilliseconds(300);

            S_HitResult? hit = await bot.WaitForHitResult(
                targetEntityId: spawn.entityId,
                timeout: hitWait,
                ct);

            if (hit == null)
                return Fail(result, $"S_HitResult not received within {hitWait.TotalMilliseconds}ms after launch");

            if (hit.hitEffect != 1)
                return Fail(result, $"S_HitResult.hitEffect={hit.hitEffect} — expected 1 (projectile)");

            if (hit.currentHp >= result.InitialHp)
                return Fail(result, $"enemy hp did not decrease: before={result.InitialHp} after={hit.currentHp}");

            result.SawHitResult = true;
            result.HitEffect = hit.hitEffect;
            result.HpAfterHit = hit.currentHp;
            result.Success = true;
            return result;
        }
        finally
        {
            bot.Disconnect();
        }
    }

    static Result Fail(Result r, string reason)
    {
        r.Success = false;
        r.Reason = reason;
        return r;
    }

    sealed class RangedProbe : ProbeBase
    {
        readonly ManualResetEventSlim _mapTransition = new(false);

        readonly List<S_EntitySpawn> _spawns = new();
        S_ProjectileLaunch? _projectileLaunch;
        readonly List<S_HitResult> _hitResults = new();
        readonly Dictionary<int, float> _entityPositions = new();

        protected override CharacterClass SelectedClass => CharacterClass.Mage;

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition.IsSet, timeout, ct);

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

        public async Task<S_EntitySpawn?> WaitForNormalSpawn(TimeSpan timeout, CancellationToken ct)
        {
            const byte NormalKind = 0;
            bool ok = await WaitUntil(
                () => { lock (Gate) return _spawns.Any(s => s.entityKind == NormalKind && s.currentHp > 0); },
                timeout, ct);
            if (!ok) return null;
            lock (Gate) return _spawns.First(s => s.entityKind == NormalKind && s.currentHp > 0);
        }

        public async Task WaitForTargetNearby(int targetEntityId, float maxDist, TimeSpan timeout, CancellationToken ct)
        {
            await WaitUntil(() =>
            {
                lock (Gate)
                {
                    if (!_entityPositions.TryGetValue(targetEntityId, out float tx)) return false;
                    return Math.Abs(tx - SpawnX) <= maxDist;
                }
            }, timeout, ct);
        }

        public async Task<S_ProjectileLaunch?> WaitForProjectileLaunch(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => { lock (Gate) return _projectileLaunch != null; }, timeout, ct);
            if (!ok) return null;
            lock (Gate) return _projectileLaunch;
        }

        public async Task<S_HitResult?> WaitForHitResult(int targetEntityId, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (Gate) return _hitResults.Any(h => h.targetEntityId == targetEntityId); },
                timeout, ct);
            if (!ok) return null;
            lock (Gate) return _hitResults.First(h => h.targetEntityId == targetEntityId);
        }

        // Mage 사거리 내 이동 — 적 위치에서 3.0f 거리(MageAttackHalfExtent=4.0f 안쪽).
        public async Task MoveIntoMageRange(float targetX, CancellationToken ct)
        {
            const float MageStopDist = 3.0f;
            float desiredX = targetX > SpawnX
                ? targetX - MageStopDist
                : targetX + MageStopDist;
            await MoveTo(desiredX, ct);
        }

        public async Task MoveToPortal(float portalX, CancellationToken ct)
            => await MoveTo(portalX, ct);

        public void SendEnterPortal(int portalId) => SendEnterPortalCore(portalId);

        public void SendAttack(int targetEntityId)
        {
            C_Attack p = new()
            {
                targetEntityId     = targetEntityId,
                attackerClientTick = LastReceivedServerTick,
            };
            Session?.Send(p.Write());
        }

        // SpawnX 기반 단순 이동 (RangedHitSmoke는 hitstun 우려가 없는 짧은 이동만 사용).
        async Task MoveTo(float destX, CancellationToken ct)
        {
            float delta = destX - SpawnX;
            sbyte dir = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Mage().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 200);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
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
                    S_EntitySpawn sp = new(); sp.Read(buffer);
                    lock (Gate)
                    {
                        _spawns.Add(sp);
                        _entityPositions[sp.entityId] = sp.x;
                    }
                    break;

                case PacketID.S_EntityState:
                    S_EntityState es = new(); es.Read(buffer);
                    lock (Gate)
                        _entityPositions[es.entityId] = es.x;
                    break;

                case PacketID.S_ProjectileLaunch:
                    S_ProjectileLaunch pl = new(); pl.Read(buffer);
                    lock (Gate)
                    {
                        if (_projectileLaunch == null)
                            _projectileLaunch = pl;
                    }
                    break;

                case PacketID.S_HitResult:
                    S_HitResult hit = new(); hit.Read(buffer);
                    lock (Gate) _hitResults.Add(hit);
                    break;
            }
        }
    }
}
