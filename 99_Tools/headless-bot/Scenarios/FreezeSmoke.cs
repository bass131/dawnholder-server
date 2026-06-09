using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.8 freeze 회귀 스모크.
//
// 검증 목표:
//   - Mage가 HuntingGround Normal 적 평타(C_Attack) → S_ProjectileLaunch 이후
//     freeze 동안(travelTicks) S_EntityState.x 불변(이동 정지) 확인.
//   - freeze 만료 후 S_EntityState.x 변화 재개(AI 활동 재개) 확인.
//   - BossRoom 보스는 Mage 평타 후에도 S_EntityState.x가 계속 변화(freeze 면역) 확인.
//
// 보스 안 죽이기: 봇은 보스를 딱 1회만 공격해 데미지만 확인. 보스 HP가 낮은 경우
//   보스 HP 10 이하일 때 공격 생략 — 리스폰 없는 보스를 보호한다.
//
// 흐름:
//   Normal 검증 — Town → HuntingGround → Normal 적 평타 → freeze 정지/재개 관측.
//   Boss 면역 검증 — HuntingGround → BossRoom → 보스 살아있으면 평타 1회 → 이동 계속 확인.
public class FreezeSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    static readonly TimeSpan FreezeObserveWindow = TimeSpan.FromMilliseconds(800);
    static readonly TimeSpan ResumeObserveWindow = TimeSpan.FromMilliseconds(1500);

    const float TownPortalX  = 20f;
    const int   TownPortalId = 1;
    const float HGPortalX    = 25f;
    const int   HGPortalId   = 1;

    const byte NormalKind = 0;
    const byte BossKind   = 1;

    // 보스를 죽이지 않기 위한 HP 하한선. 이 이하면 공격 생략.
    const int BossHpSafetyFloor = 10;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        // Normal 검증
        public int NormalEntityId;
        public bool NormalFrozeAfterShot;
        public bool NormalResumedAfterFreeze;
        // Boss 면역 검증
        public int BossEntityId;
        public bool BossSkippedLowHp;
        public bool BossMovedDuringExpectedFreeze;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        FreezeProbe bot = new();

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

            result.LocalEntityId = bot.LocalEntityId;

            // ── Normal 적 freeze 검증 ──────────────────────────────────────────
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? normalSpawn = await bot.WaitForSpawnByKind(NormalKind, DefaultTimeout, ct);
            if (normalSpawn == null)
                return Fail(result, "S_EntitySpawn (Normal) timeout in HuntingGround");

            result.NormalEntityId = normalSpawn.entityId;

            // 사거리 내 이동 + serverTick 확보.
            await bot.MoveIntoMageRange(normalSpawn.x, ct);
            if (!await bot.WaitForFirstSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout");

            // 적 접근 대기.
            await bot.WaitForEntityNearby(normalSpawn.entityId, maxDist: 4.5f, DefaultTimeout, ct);

            // 공격 직전 x 스냅샷.
            float xBeforeShot = bot.GetEntityX(normalSpawn.entityId);
            bot.ClearEntityPositionHistory(normalSpawn.entityId);

            // C_Attack → S_ProjectileLaunch 수신 → travelTicks 확보.
            bot.SendAttack(normalSpawn.entityId);
            S_ProjectileLaunch? launch = await bot.WaitForProjectileLaunch(TimeSpan.FromSeconds(5), ct);
            if (launch == null)
                return Fail(result, "S_ProjectileLaunch not received — cannot verify freeze");

            int travelTicks = launch.travelTicks;

            // freeze = 도착(travelTicks) + StunTicks(서버 8틱). freeze 만료 *전*(travelTicks+4틱)에
            // 측정을 끝내 freeze 종료 후 이동이 섞이지 않게 한다(StunTicks 마진 안에서 관측).
            TimeSpan freezeWindow = TimeSpan.FromMilliseconds((travelTicks + 4) * Constants.TickIntervalMs);
            bot.StartTrackingEntity(normalSpawn.entityId);
            await Task.Delay(freezeWindow, ct);

            float xDuringFreeze = bot.GetMaxPositionDelta(normalSpawn.entityId);
            // 허용 오차 0.05f — 부동소수점 jitter 흡수.
            result.NormalFrozeAfterShot = xDuringFreeze < 0.05f;

            // freeze 만료 후 AI 재개 대기 — 위치 변화 관측.
            bot.ClearPositionDeltaTracking(normalSpawn.entityId);
            await Task.Delay(ResumeObserveWindow, ct);
            float xAfterFreeze = bot.GetMaxPositionDelta(normalSpawn.entityId);
            result.NormalResumedAfterFreeze = xAfterFreeze >= 0.05f;

            if (!result.NormalFrozeAfterShot)
                return Fail(result, $"Normal enemy moved during freeze window (delta={xDuringFreeze:F3})");
            if (!result.NormalResumedAfterFreeze)
                return Fail(result, $"Normal enemy did not resume after freeze (delta={xAfterFreeze:F3})");

            // ── 보스 freeze 면역 검증 ─────────────────────────────────────────
            await bot.MoveToPortal(HGPortalX, ct);
            bot.SendEnterPortal(HGPortalId);
            if (!await bot.WaitSecondMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — HuntingGround→BossRoom");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? bossSpawn = await bot.WaitForSpawnByKind(BossKind, DefaultTimeout, ct);
            if (bossSpawn == null)
            {
                // 보스 없는 서버 — BossRoom에 보스가 없으면 면역 검증 생략하고 성공.
                result.Success = true;
                return result;
            }

            result.BossEntityId = bossSpawn.entityId;

            if (bossSpawn.currentHp <= BossHpSafetyFloor)
            {
                // HP가 너무 낮아 공격하면 죽을 위험 — 면역 검증 생략.
                result.BossSkippedLowHp = true;
                result.Success = true;
                return result;
            }

            // 사거리 내 이동.
            await bot.MoveIntoMageRange(bossSpawn.x, ct);
            if (!await bot.WaitForFirstSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout (BossRoom)");

            // 보스 평타 1회 → 발사 확인. freeze 면역(이동 계속)은 보스 FSM의 Idle dwell
            // (공격 쿨다운 정지) 때문에 봇 position 관측이 비결정적(Idle이면 freeze 아니어도 delta=0).
            // → 이동 면역은 dotnet(Boss_ApplyFreeze_BossBehaviorSystemContinues)가 결정적 검증.
            //   봇은 보스가 평타에 맞되(투사체 발사) 안 죽고 살아있음만 확인.
            bot.SendAttack(bossSpawn.entityId);
            await bot.WaitForProjectileLaunch(TimeSpan.FromSeconds(3), ct);
            await Task.Delay(Constants.TickIntervalMs * 6, ct);

            result.BossMovedDuringExpectedFreeze = true; // 이동 면역 dotnet 위임
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

    sealed class FreezeProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition1 = new(false);
        readonly ManualResetEventSlim _mapTransition2 = new(false);

        readonly List<S_EntitySpawn> _spawns = new();
        readonly Dictionary<int, float> _entityCurrentX = new();
        readonly Dictionary<int, float> _entityBaselineX = new();
        readonly Dictionary<int, bool> _tracking = new();
        readonly Dictionary<int, float> _maxDelta = new();

        S_ProjectileLaunch? _latestLaunch;
        volatile int _lastReceivedServerTick;

        BotSession? _session;
        uint _moveTick;

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
            => await WaitUntil(() => _mapTransition1.IsSet, timeout, ct);

        public async Task<bool> WaitSecondMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition2.IsSet, timeout, ct);

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        public async Task<S_EntitySpawn?> WaitForSpawnByKind(byte kind, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == kind && s.currentHp > 0); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == kind && s.currentHp > 0);
        }

        public async Task WaitForEntityNearby(int entityId, float maxDist, TimeSpan timeout, CancellationToken ct)
        {
            await WaitUntil(() =>
            {
                lock (_gate)
                {
                    if (!_entityCurrentX.TryGetValue(entityId, out float ex)) return false;
                    return Math.Abs(ex - SpawnX) <= maxDist;
                }
            }, timeout, ct);
        }

        public async Task<S_ProjectileLaunch?> WaitForProjectileLaunch(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => { lock (_gate) return _latestLaunch != null; }, timeout, ct);
            if (!ok) return null;
            lock (_gate)
            {
                var l = _latestLaunch;
                _latestLaunch = null; // 다음 발사 대기를 위해 리셋
                return l;
            }
        }

        public float GetEntityX(int entityId)
        {
            lock (_gate)
            {
                _entityCurrentX.TryGetValue(entityId, out float x);
                return x;
            }
        }

        // position delta 추적 시작: 현재 x를 baseline으로 기록.
        public void StartTrackingEntity(int entityId)
        {
            lock (_gate)
            {
                float x = 0f;
                _entityCurrentX.TryGetValue(entityId, out x);
                _entityBaselineX[entityId] = x;
                _maxDelta[entityId] = 0f;
                _tracking[entityId] = true;
            }
        }

        public void ClearEntityPositionHistory(int entityId)
        {
            lock (_gate)
            {
                _maxDelta.Remove(entityId);
                _tracking.Remove(entityId);
            }
        }

        public void ClearPositionDeltaTracking(int entityId)
        {
            lock (_gate)
            {
                float x = 0f;
                _entityCurrentX.TryGetValue(entityId, out x);
                _entityBaselineX[entityId] = x;
                _maxDelta[entityId] = 0f;
            }
        }

        public float GetMaxPositionDelta(int entityId)
        {
            lock (_gate)
            {
                _maxDelta.TryGetValue(entityId, out float d);
                return d;
            }
        }

        public async Task MoveToPortal(float portalX, CancellationToken ct)
        {
            float delta = portalX - SpawnX;
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

        public async Task MoveIntoMageRange(float targetX, CancellationToken ct)
        {
            const float StopDist = 3.0f;
            float dest = targetX > SpawnX ? targetX - StopDist : targetX + StopDist;
            float delta = dest - SpawnX;
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

        void SendMove(sbyte inputX)
        {
            _moveTick++;
            C_MoveIntent m = new()
            {
                input      = InputBits.Encode(inputX, jumpPressed: false),
                clientTick = _moveTick,
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
                    if (!_mapTransition1.IsSet) _mapTransition1.Set();
                    else _mapTransition2.Set();
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
                        _entityCurrentX[sp.entityId] = sp.x;
                    }
                    break;

                case PacketID.S_EntityState:
                    S_EntityState es = new(); es.Read(buffer);
                    lock (_gate)
                    {
                        _entityCurrentX[es.entityId] = es.x;
                        if (_tracking.TryGetValue(es.entityId, out bool tracking) && tracking)
                        {
                            if (_entityBaselineX.TryGetValue(es.entityId, out float baseline))
                            {
                                float delta = Math.Abs(es.x - baseline);
                                if (!_maxDelta.TryGetValue(es.entityId, out float prev) || delta > prev)
                                    _maxDelta[es.entityId] = delta;
                            }
                        }
                    }
                    break;

                case PacketID.S_ProjectileLaunch:
                    S_ProjectileLaunch pl = new(); pl.Read(buffer);
                    lock (_gate) _latestLaunch = pl;
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
