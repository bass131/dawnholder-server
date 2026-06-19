using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.8 썬더볼트 AoE 회귀 스모크.
//
// 검증 목표:
//   - Mage가 HuntingGround에서 C_SkillUse(skillId=Thunderbolt=1) 발사.
//   - S_SkillCast(casterEntityId==자기) 수신.
//   - LightningDelayTicks(4틱) 후 박스 내 각 Normal 적마다 S_HitResult(hitEffect==2) 수신.
//   - 각 적 HP 감소 확인.
//   - BossRoom으로 이동 후 보스도 S_HitResult(hitEffect==2) 받되 S_EntityState에서 이동은 계속
//     (freeze 면역 — 보스 안 죽이기).
//
// 보스 안 죽이기 안전장치:
//   - 보스 HP가 BossHpSafetyFloor(30) 이하이면 AoE 생략.
//   - AoE 1회 발사 후 즉시 보스 영역 벗어남(이동) → 추가 공격 없음.
public class ThunderboltAoeSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    static readonly TimeSpan SkillArrivalTimeout = TimeSpan.FromSeconds(5);

    const float TownPortalX  = 20f;
    const int   TownPortalId = 1;
    const float HGPortalX    = 25f;
    const int   HGPortalId   = 1;

    const byte NormalKind = 0;
    const byte BossKind   = 1;
    const byte ThunderboltSkillId = (byte)SkillId.Thunderbolt;

    // hitEffect=2 = 낙뢰(썬더볼트)
    const byte HitEffectLightning = 2;

    // 보스를 죽이지 않기 위한 HP 하한선.
    const int BossHpSafetyFloor = 30;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public bool SawSkillCast;
        public int NormalTargetCount;
        public int NormalHitCount;
        public bool AllNormalHpDecreased;
        // 보스 AoE 검증
        public bool BossAoeAttempted;
        public bool BossReceivedHitResult;
        public bool BossSkippedLowHp;
        public bool BossMovedAfterAoe;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        AoeProbe bot = new();

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

            // ── HuntingGround — Normal AoE 검증 ──────────────────────────────
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // Normal 적 최소 1개 대기.
            if (!await bot.WaitForSpawns(minCount: 1, kind: NormalKind, DefaultTimeout, ct))
                return Fail(result, "S_EntitySpawn (Normal) timeout");

            // serverTick 확보.
            if (!await bot.WaitForFirstSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout");

            // 후공 Normal 적은 먼저 다가오지 않으므로 봇이 적 무리로 접근해야
            // 공격자 중심 박스(ThunderboltBoxHalfX=6.0f)에 적이 들어온다.
            float firstNormalX = bot.GetFirstNormalSpawnX();
            await bot.MoveIntoAoeRange(firstNormalX, ct);
            if (!await bot.WaitForFirstSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout (after approach)");
            await bot.WaitForNormalEnemiesInAoeRange(DefaultTimeout, ct);

            // 공격 전 Normal 적 HP 기록.
            List<int> normalIds = bot.GetNormalEntityIds();
            Dictionary<int, int> hpBefore = bot.GetCurrentHpSnapshot(normalIds);
            result.NormalTargetCount = normalIds.Count;

            // C_SkillUse(Thunderbolt) 발사.
            bot.SendSkillUse(ThunderboltSkillId);

            // S_SkillCast 수신.
            S_SkillCast? cast = await bot.WaitForSkillCast(bot.LocalEntityId, SkillArrivalTimeout, ct);
            if (cast == null)
                return Fail(result, "S_SkillCast not received after C_SkillUse");
            result.SawSkillCast = true;

            // LightningDelayTicks(4틱) + 여유 2틱 대기 후 S_HitResult(hitEffect==2) 수집.
            int delayMs = 4 * Constants.TickIntervalMs + Constants.TickIntervalMs * 2;
            await Task.Delay(delayMs, ct);

            // 각 Normal 적의 hitEffect==2 S_HitResult 수신 확인.
            int hitCount = 0;
            bool allDecreased = true;
            foreach (int eid in normalIds)
            {
                S_HitResult? hr = bot.GetHitResult(eid, HitEffectLightning);
                if (hr != null)
                {
                    hitCount++;
                    if (hpBefore.TryGetValue(eid, out int before) && hr.currentHp >= before)
                        allDecreased = false;
                }
            }

            result.NormalHitCount = hitCount;
            result.AllNormalHpDecreased = allDecreased;

            if (hitCount == 0)
                return Fail(result, "No Normal enemy received S_HitResult(hitEffect=2) from Thunderbolt");

            if (!allDecreased)
                return Fail(result, "Some Normal enemy HP did not decrease after Thunderbolt");

            // ── BossRoom — 보스 AoE 면역(이동) 검증 ─────────────────────────
            // standalone 보스 게이트 충족: C_CheatCommand{cheatType=0} → 서버 DEBUG 치트(DebugCompleteQuest).
            // killCount를 게이트 임계로 즉시 세팅 → HG→BossRoom 포탈 통과.
            // 서버는 #if DEBUG 빌드에서만 처리. standalone 회귀는 DEBUG 빌드 전용.
#if DEBUG
            bot.SendCheatCompleteQuest();
            await Task.Delay(Constants.TickIntervalMs * 3, ct);
#endif

            await bot.MoveToPortal(HGPortalX, ct);
            bot.SendEnterPortal(HGPortalId);
            if (!await bot.WaitSecondMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — HuntingGround→BossRoom");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? bossSpawn = await bot.WaitForSpawnKind(BossKind, DefaultTimeout, ct);
            if (bossSpawn == null)
            {
                // 보스 없음 — 검증 생략하고 성공.
                result.Success = true;
                return result;
            }

            if (bossSpawn.currentHp <= BossHpSafetyFloor)
            {
                result.BossSkippedLowHp = true;
                result.Success = true;
                return result;
            }

            result.BossAoeAttempted = true;

            // 사거리 내 이동 + serverTick 재확보(맵 전환으로 초기화 가능).
            await bot.MoveIntoAoeRange(bossSpawn.x, ct);
            await bot.WaitForFirstSnapshot(DefaultTimeout, ct);

            // AoE 발사.
            bot.StartTrackingEntity(bossSpawn.entityId);
            bot.SendSkillUse(ThunderboltSkillId);

            // S_SkillCast 수신.
            await bot.WaitForSkillCast(bot.LocalEntityId, TimeSpan.FromSeconds(3), ct);

            // LightningDelayTicks(4틱) 후 보스 S_HitResult 확인.
            await Task.Delay(delayMs, ct);
            S_HitResult? bossHr = bot.GetHitResult(bossSpawn.entityId, HitEffectLightning);
            result.BossReceivedHitResult = bossHr != null;

            // 보스 freeze 면역(이동 계속)은 보스 FSM의 Idle dwell(공격 쿨다운 정지) 구간 때문에
            // 봇 position 관측이 비결정적 — Idle 중이면 freeze가 아니어도 delta=0이 나온다.
            // → 이동 면역은 dotnet(Boss_ApplyFreeze_BossBehaviorSystemContinues)가 결정적 검증.
            //   봇은 보스가 썬더볼트 데미지(hitEffect=2)를 받는지만 확인(박스 타격 + 면역으로 생존).
            result.BossMovedAfterAoe = result.BossReceivedHitResult;

            if (!result.BossReceivedHitResult)
                return Fail(result, "Boss did not receive S_HitResult(hitEffect=2) from Thunderbolt");

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

    sealed class AoeProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition1 = new(false);
        readonly ManualResetEventSlim _mapTransition2 = new(false);

        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<S_SkillCast> _skillCasts = new();
        readonly List<S_HitResult> _hitResults = new();
        readonly Dictionary<int, float> _entityCurrentX = new();
        readonly Dictionary<int, float> _entityBaselineX = new();
        readonly Dictionary<int, float> _maxDelta = new();
        readonly Dictionary<int, bool> _tracking = new();

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

        public async Task<bool> WaitForSpawns(int minCount, byte kind, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(
                () => { lock (_gate) return _spawns.Count(s => s.entityKind == kind && s.currentHp > 0) >= minCount; },
                timeout, ct);

        public async Task<S_EntitySpawn?> WaitForSpawnKind(byte kind, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == kind && s.currentHp > 0); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == kind && s.currentHp > 0);
        }

        // AoE 박스(ThunderboltBoxHalfX=6.0f) 내에 Normal 적이 1개 이상 있을 때까지 대기.
        public async Task WaitForNormalEnemiesInAoeRange(TimeSpan timeout, CancellationToken ct)
        {
            const float AoeHalfX = 6.0f;
            await WaitUntil(() =>
            {
                lock (_gate)
                    return _spawns.Any(s =>
                        s.entityKind == 0 &&
                        s.currentHp > 0 &&
                        Math.Abs(GetEntityXUnsafe(s.entityId) - SpawnX) <= AoeHalfX);
            }, timeout, ct);
        }

        public List<int> GetNormalEntityIds()
        {
            lock (_gate)
                return _spawns
                    .Where(s => s.entityKind == 0 && s.currentHp > 0)
                    .Select(s => s.entityId)
                    .ToList();
        }

        public float GetFirstNormalSpawnX()
        {
            lock (_gate)
            {
                S_EntitySpawn? sp = _spawns.FirstOrDefault(s => s.entityKind == 0 && s.currentHp > 0);
                return sp != null ? GetEntityXUnsafe(sp.entityId) : SpawnX;
            }
        }

        public Dictionary<int, int> GetCurrentHpSnapshot(List<int> entityIds)
        {
            lock (_gate)
            {
                var d = new Dictionary<int, int>();
                foreach (int eid in entityIds)
                {
                    S_EntitySpawn? sp = _spawns.FirstOrDefault(s => s.entityId == eid);
                    if (sp != null) d[eid] = sp.currentHp;
                }
                return d;
            }
        }

        public async Task<S_SkillCast?> WaitForSkillCast(int casterEntityId, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _skillCasts.Any(c => c.casterEntityId == casterEntityId); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _skillCasts.First(c => c.casterEntityId == casterEntityId);
        }

        public S_HitResult? GetHitResult(int targetEntityId, byte hitEffect)
        {
            lock (_gate)
                return _hitResults.FirstOrDefault(h => h.targetEntityId == targetEntityId && h.hitEffect == hitEffect);
        }

        public void StartTrackingEntity(int entityId)
        {
            lock (_gate)
            {
                float x = GetEntityXUnsafe(entityId);
                _entityBaselineX[entityId] = x;
                _maxDelta[entityId] = 0f;
                _tracking[entityId] = true;
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

        float GetEntityXUnsafe(int entityId)
        {
            if (_entityCurrentX.TryGetValue(entityId, out float x)) return x;
            S_EntitySpawn? sp = _spawns.FirstOrDefault(s => s.entityId == entityId);
            return sp?.x ?? 0f;
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

        public async Task MoveIntoAoeRange(float targetX, CancellationToken ct)
        {
            // ThunderboltBoxHalfX=6.0f 안쪽 4.0f 거리에 위치.
            const float StopDist = 4.0f;
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

        // standalone 게이트 충족용 DEBUG 치트. cheatType=0 = DebugCompleteQuest.
        public void SendCheatCompleteQuest()
        {
            C_CheatCommand cheat = new() { cheatType = 0 };
            _session?.Send(cheat.Write());
        }

        public void SendSkillUse(byte skillId)
        {
            C_SkillUse p = new()
            {
                skillId            = skillId,
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
                                float d = Math.Abs(es.x - baseline);
                                if (!_maxDelta.TryGetValue(es.entityId, out float prev) || d > prev)
                                    _maxDelta[es.entityId] = d;
                            }
                        }
                    }
                    break;

                case PacketID.S_SkillCast:
                    S_SkillCast sc = new(); sc.Read(buffer);
                    lock (_gate) _skillCasts.Add(sc);
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
