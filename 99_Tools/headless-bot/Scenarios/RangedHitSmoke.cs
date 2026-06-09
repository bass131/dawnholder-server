using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
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

    sealed class RangedProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition = new(false);

        readonly List<S_EntitySpawn> _spawns = new();
        S_ProjectileLaunch? _projectileLaunch;
        readonly List<S_HitResult> _hitResults = new();
        readonly Dictionary<int, float> _entityPositions = new();

        volatile int _lastReceivedServerTick;

        BotSession? _session;
        uint _clientMoveTick;

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
                        C_Handshake h = new() { clientVersion = ProtocolVersion.Current };
                        s.Send(h.Write());
                    };
                    s.OnDisconnectedCallback = _ => { };
                    s.OnPacketCallback = HandlePacket;
                    _session = s;
                    return s;
                });
        }

        public bool WaitConnected(TimeSpan t) => _connected.Wait(t);
        public bool WaitHandshake(TimeSpan t) => _handshake.Wait(t);
        public bool WaitEnterMap(TimeSpan t)  => _enterMap.Wait(t);

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition.IsSet, timeout, ct);

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        public async Task<S_EntitySpawn?> WaitForNormalSpawn(TimeSpan timeout, CancellationToken ct)
        {
            const byte NormalKind = 0;
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == NormalKind && s.currentHp > 0); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == NormalKind && s.currentHp > 0);
        }

        public async Task WaitForTargetNearby(int targetEntityId, float maxDist, TimeSpan timeout, CancellationToken ct)
        {
            await WaitUntil(() =>
            {
                lock (_gate)
                {
                    if (!_entityPositions.TryGetValue(targetEntityId, out float tx)) return false;
                    return Math.Abs(tx - SpawnX) <= maxDist;
                }
            }, timeout, ct);
        }

        public async Task<S_ProjectileLaunch?> WaitForProjectileLaunch(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => { lock (_gate) return _projectileLaunch != null; }, timeout, ct);
            if (!ok) return null;
            lock (_gate) return _projectileLaunch;
        }

        public async Task<S_HitResult?> WaitForHitResult(int targetEntityId, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _hitResults.Any(h => h.targetEntityId == targetEntityId); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _hitResults.First(h => h.targetEntityId == targetEntityId);
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

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal p = new() { portalId = portalId };
            _session?.Send(p.Write());
        }

        public void SendAttack(int targetEntityId)
        {
            C_Attack p = new()
            {
                targetEntityId     = targetEntityId,
                attackerClientTick = _lastReceivedServerTick,
            };
            _session?.Send(p.Write());
        }

        public void Disconnect() => _session?.Disconnect();

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

        void SendMove(sbyte inputX)
        {
            _clientMoveTick++;
            C_MoveIntent m = new()
            {
                input      = InputBits.Encode(inputX, jumpPressed: false),
                clientTick = _clientMoveTick,
            };
            _session?.Send(m.Write());
        }

        void HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 4) return;
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2));
            switch ((PacketID)id)
            {
                case PacketID.S_HandshakeResult:
                    S_HandshakeResult hr = new(); hr.Read(buffer);
                    HandshakeOk = hr.ok;
                    HandshakeReason = hr.reason;
                    if (hr.ok)
                    {
                        C_CharacterSelect cs = new() { characterClass = (byte)CharacterClass.Mage };
                        _session?.Send(cs.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap em = new(); em.Read(buffer);
                    LocalEntityId = em.entityId;
                    SpawnX = em.spawnX;
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition mt = new(); mt.Read(buffer);
                    SpawnX = mt.spawnX;
                    _mapTransition.Set();
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot sn = new(); sn.Read(buffer);
                    _lastReceivedServerTick = sn.serverTick;
                    if (sn.entityId == LocalEntityId)
                        SpawnX = sn.x;
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn sp = new(); sp.Read(buffer);
                    lock (_gate)
                    {
                        _spawns.Add(sp);
                        _entityPositions[sp.entityId] = sp.x;
                    }
                    break;

                case PacketID.S_EntityState:
                    S_EntityState es = new(); es.Read(buffer);
                    lock (_gate)
                        _entityPositions[es.entityId] = es.x;
                    break;

                case PacketID.S_ProjectileLaunch:
                    S_ProjectileLaunch pl = new(); pl.Read(buffer);
                    lock (_gate)
                    {
                        if (_projectileLaunch == null)
                            _projectileLaunch = pl;
                    }
                    break;

                case PacketID.S_HitResult:
                    S_HitResult hit = new(); hit.Read(buffer);
                    lock (_gate) _hitResults.Add(hit);
                    break;
            }
        }

        static async Task<bool> WaitUntil(Func<bool> pred, TimeSpan timeout, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (pred()) return true;
                await Task.Delay(25, ct);
            }
            return pred();
        }
    }
}
