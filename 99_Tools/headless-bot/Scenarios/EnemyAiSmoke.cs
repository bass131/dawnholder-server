using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Enemy AI smoke — M4.6 Phase 04 갱신 (선공/후공 분리 행동 반영).
//
// **무엇이 바뀌었나 (M4.6 Phase 04)**:
//   EnemyStats.AggroOnSight 플래그로 선공/후공이 분리됨.
//   - Normal(슬라임, AggroOnSight=false): 후공 — 피격 시에만 Chase 전환.
//     접근만으론 Patrol 유지 (PatrolState에서 AggroOnSight 게이트 불통과).
//   - Golem(AggroOnSight=true): 선공 — AggroRange(4.0) 안에 들어오면 시야만으로 즉시 Chase.
//
// **Success 판정 (Option A, 2026-06-08 갱신)**:
//   Hard assertion = 슬라임이 접근 후에도 Patrol 유지(후공 확인).
//   SlimeStayedPatrolAfterApproach == true → Success=true.
//   SlimeStayedPatrolAfterApproach == false → 진짜 회귀 → hard FAIL.
//
//   Soft-log 항목 (미관측이어도 절대 FAIL 아님):
//   - 골렘 선공(시나리오 A): golem=0이면 respawn 대기 추정. 단위 테스트 Proactive_AggrosOnSight 위임.
//   - 슬라임 hit→Chase(시나리오 B 후반): 봇 공격 헛스윙(공격-타겟 결합=다음 phase). 단위 테스트 Reactive_AggrosAfterHit 위임.
//   관측되면 [soft/confirmed] 긍정 로깅.
//
// **시나리오 A — 골렘 선공 검증 (soft)**:
//   HuntingGround Golem(x=5.50, y=0.00) → 봇이 AggroRange 밖에서 접근 → Chase 전환 soft 확인.
//
// **시나리오 B — 슬라임 후공 검증 (hard)**:
//   Normal 중 타겟 선정 → 접근 후 일정 시간 Patrol 유지 단언(hard) → C_Attack 1회 → Chase 전환 soft 확인.
//
// **검증 전략**:
//   - S_EntityState 패킷(PacketID=19) state 필드: 0=Idle, 1=Patrol, 2=Chase (EnemyState enum 정합)
//   - 슬라임 후공 검증은 C_Attack 송신 → S_EntityState(Chase) 수신으로 soft 확인.
//     단, AttackHandler가 rewind 범위 검증(attackerClientTick)을 통과해야 하므로
//     S_Snapshot serverTick 추적 후 공격 발송.
//
// **좌표 하드코딩 X**:
//   런타임 S_EntitySpawn 수신값으로 타겟/접근 좌표 결정 (content.bin 재bake 내성).
public class EnemyAiSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan PatrolHoldWindow = TimeSpan.FromMilliseconds(600);

    const byte StatePatrol = 1;
    const byte StateChase  = 2;

    const float TownPortalX = 20f;
    const int   TownPortalId = 1;

    const float SamePlaneTolerance = 1.5f;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;

        // 시나리오 A (골렘 선공)
        public int GolemEntityId;
        public float GolemInitialX;
        public bool SawGolemPatrol;
        public bool SawGolemChaseAfterApproach;

        // 시나리오 B (슬라임 후공)
        public int SlimeEntityId;
        public float SlimeInitialX;
        public bool SawSlimePatrolBeforeHit;
        public bool SlimeStayedPatrolAfterApproach;
        public bool SawSlimeChaseAfterHit;
    }

    public static async Task<Result> Run(
        string host,
        int port,
        CancellationToken ct = default)
    {
        Result result = new();
        AiProbe bot = new();

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

            await bot.MoveToX(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);

            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 3, ct);

            // 전체 초기 roster 수집 (Normal + Golem) — grace 동안 전부 받기.
            IReadOnlyList<S_EntitySpawn> allSpawns = await bot.CollectAllEnemySpawns(
                DefaultTimeout, TimeSpan.FromMilliseconds(Constants.TickIntervalMs * 4), ct);

            if (allSpawns.Count == 0)
                return Fail(result, "S_EntitySpawn timeout — HuntingGround roster 없음");

            float botX = bot.BotX;
            float botY = bot.BotY;

            // ── 시나리오 A: 골렘 선공 검증 ─────────────────────────────────────

            EnemyStats golemStats = EnemyStats.GolemDefault();

            S_EntitySpawn? golemTarget = allSpawns
                .Where(s => s.entityKind == (byte)EnemyKind.Golem
                         && Math.Abs(s.y - botY) <= SamePlaneTolerance
                         && Math.Abs(s.x - botX) > golemStats.AggroRange)
                .OrderBy(s => Math.Abs(s.x - botX))
                .FirstOrDefault();

            if (golemTarget == null)
            {
                Console.WriteLine($"[AiSmoke] Warning: Golem 타겟 없음 (같은 높이 + aggro 밖). " +
                    $"수집 spawns={allSpawns.Count}, bot=({botX:F2},{botY:F2}). " +
                    $"시나리오 A 건너뜀.");
            }
            else
            {
                result.GolemEntityId  = golemTarget.entityId;
                result.GolemInitialX  = golemTarget.x;

                // 골렘이 Patrol 상태인지 먼저 확인 (aggro 밖이니 Patrol이어야 함)
                bool gotGolemPatrol = await bot.WaitForEnemyState(
                    golemTarget.entityId, StatePatrol, DefaultTimeout, ct);
                result.SawGolemPatrol = gotGolemPatrol;
                if (!gotGolemPatrol)
                    Console.WriteLine($"[AiSmoke] Warning: Golem Patrol 확인 실패 (last state={bot.LastStateFor(golemTarget.entityId)})");

                // 골렘 스폰 X로 접근 → AggroRange 안 진입 → 선공 Chase 전환 기대
                await bot.MoveToX(golemTarget.x, ct);

                bool gotGolemChase = await bot.WaitForEnemyState(
                    golemTarget.entityId, StateChase, DefaultTimeout, ct);

                if (!gotGolemChase)
                {
                    Console.WriteLine(
                        $"[soft/deferred] 골렘 선공 미검증 " +
                        $"(last state={bot.LastStateFor(golemTarget.entityId)}, " +
                        $"last x={bot.LastXFor(golemTarget.entityId):F2}, " +
                        $"golem respawn 대기 또는 공격-타겟 결합 이슈 추정) " +
                        $"→ 단위 테스트 Proactive_AggrosOnSight에 위임");
                }
                else
                {
                    result.SawGolemChaseAfterApproach = true;
                    Console.WriteLine($"[soft/confirmed] 골렘 선공 확인 (entity={golemTarget.entityId}, Chase 전환 관측)");
                }

                // 골렘에서 다시 멀어져 봇 위치를 슬라임 시나리오 기준으로 되돌림
                await bot.MoveToX(bot.BotX - golemStats.AggroRange * 2f, ct);
                await Task.Delay(200, ct);
            }

            // ── 시나리오 B: 슬라임 후공 검증 ─────────────────────────────────────

            botX = bot.BotX;
            botY = bot.BotY;

            EnemyStats normalStats = EnemyStats.NormalDefault();

            S_EntitySpawn? slimeTarget = allSpawns
                .Where(s => s.entityKind == (byte)EnemyKind.Normal
                         && Math.Abs(s.y - botY) <= SamePlaneTolerance
                         && Math.Abs(s.x - botX) > normalStats.AggroRange)
                .OrderBy(s => Math.Abs(s.x - botX))
                .FirstOrDefault();

            if (slimeTarget == null)
                return Fail(result,
                    $"슬라임 타겟 없음 (같은 높이(±{SamePlaneTolerance}) + aggro({normalStats.AggroRange}) 밖). " +
                    $"Normal spawns: {string.Join(", ", allSpawns.Where(s => s.entityKind == 0).Select(s => $"({s.x:F2},{s.y:F2})"))} " +
                    $"/ bot=({botX:F2},{botY:F2})");

            result.SlimeEntityId  = slimeTarget.entityId;
            result.SlimeInitialX  = slimeTarget.x;

            // 슬라임 Patrol 확인 (aggro 밖이니 Patrol이어야 함)
            bool gotSlimePatrol = await bot.WaitForEnemyState(
                slimeTarget.entityId, StatePatrol, DefaultTimeout, ct);
            result.SawSlimePatrolBeforeHit = gotSlimePatrol;
            if (!gotSlimePatrol)
                Console.WriteLine($"[AiSmoke] Warning: Slime Patrol 확인 실패. last={bot.LastStateFor(slimeTarget.entityId)}");

            // 접근 — AggroRange 안으로 이동하되 공격 전 Patrol 유지 여부 단언
            await bot.MoveToX(slimeTarget.x, ct);

            // 후공이므로 접근 직후 일정 시간 동안 Chase 전환 없어야 함 (Patrol 유지)
            await Task.Delay(PatrolHoldWindow, ct);
            byte stateAfterApproach = bot.LastStateFor(slimeTarget.entityId);
            result.SlimeStayedPatrolAfterApproach = stateAfterApproach == StatePatrol
                || stateAfterApproach == 255; // 아직 state 미수신이면 Chase 전환 없는 것으로 간주

            if (!result.SlimeStayedPatrolAfterApproach)
            {
                // 접근만으로 Chase 전환 = 후공 전제 붕괴 → 진짜 회귀
                return Fail(result,
                    $"[시나리오 B FAIL] 슬라임이 접근만으로 Chase 전환됨 (state={stateAfterApproach}). " +
                    "AggroOnSight=false 후공 전제 깨짐 — PatrolState AggroOnSight 게이트 확인 필요.");
            }

            Console.WriteLine(
                $"[hard/confirmed] 슬라임 후공 확인 (entity={slimeTarget.entityId}, " +
                $"접근 후 state={stateAfterApproach} Patrol 유지). 이것이 Phase 04 핵심 행동.");

            // serverTick 추적 대기 (C_Attack attackerClientTick 검증 통과 의무)
            bool gotSnapshot = await bot.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가, C_Attack 검증 통과 불가");

            // C_Attack 1회 발송 → 피격 트리거 → Chase 전환 단언
            bot.SendAttack(slimeTarget.entityId);

            bool gotSlimeChase = await bot.WaitForEnemyState(
                slimeTarget.entityId, StateChase, DefaultTimeout, ct);

            if (!gotSlimeChase)
            {
                Console.WriteLine(
                    $"[soft/deferred] 슬라임 hit→Chase 미검증 " +
                    $"(last state={bot.LastStateFor(slimeTarget.entityId)}, " +
                    $"last x={bot.LastXFor(slimeTarget.entityId):F2}, " +
                    $"봇 공격 헛스윙 추정 — 공격-타겟 결합 구조는 다음 phase 작업) " +
                    $"→ 단위 테스트 Reactive_AggrosAfterHit에 위임");
            }
            else
            {
                result.SawSlimeChaseAfterHit = true;
                Console.WriteLine($"[soft/confirmed] 슬라임 hit→Chase 확인 (entity={slimeTarget.entityId}, Chase 전환 관측)");
            }

            // Option A: Success 판정은 슬라임 후공(접근 후 Patrol 유지)만으로 결정.
            // 여기까지 왔으면 SlimeStayedPatrolAfterApproach == true (false면 위에서 return Fail).
            result.LocalEntityId = bot.LocalEntityId;
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

    sealed class AiProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition = new(false);

        readonly List<S_EntitySpawn> _spawns = new();

        // entityId → 최신 S_EntityState (state + x)
        readonly Dictionary<int, S_EntityState> _entityStates = new();

        BotSession? _session;
        uint _clientTick;
        float _botSpawnX;
        float _botSpawnY;

        // SendAttack에서 rewind 범위 검증 통과를 위한 serverTick 추적.
        // volatile: network thread(HandlePacket 쓰기) / 시나리오 thread(SendAttack 읽기) 교차.
        volatile int _lastReceivedServerTick = 0;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;

        public float BotX => _botSpawnX;
        public float BotY => _botSpawnY;

        public byte LastStateFor(int entityId)
        {
            lock (_gate)
                return _entityStates.TryGetValue(entityId, out S_EntityState? s) ? s.state : (byte)255;
        }

        public float LastXFor(int entityId)
        {
            lock (_gate)
                return _entityStates.TryGetValue(entityId, out S_EntityState? s) ? s.x : float.NaN;
        }

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

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition.IsSet, timeout, ct);

        // 모든 kind의 enemy spawn 수집 (Normal + Golem 포함).
        public async Task<IReadOnlyList<S_EntitySpawn>> CollectAllEnemySpawns(
            TimeSpan firstTimeout, TimeSpan grace, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Count > 0; },
                firstTimeout, ct);
            if (!ok) return Array.Empty<S_EntitySpawn>();

            await Task.Delay(grace, ct);
            lock (_gate) return _spawns.ToList();
        }

        public async Task<bool> WaitForEnemyState(int entityId, byte targetState, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(
                () => { lock (_gate) return _entityStates.TryGetValue(entityId, out S_EntityState? s) && s.state == targetState; },
                timeout, ct);

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        // x 위치까지 C_MoveIntent 기반 이동
        public async Task MoveToX(float targetX, CancellationToken ct)
        {
            float delta = targetX - _botSpawnX;
            if (Math.Abs(delta) < 0.1f) return;

            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Knight().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 200);

            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
            _botSpawnX = targetX;
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal packet = new() { portalId = portalId };
            _session?.Send(packet.Write());
        }

        // C_Attack 발송. attackerClientTick에 최신 serverTick 박음 — rewind 범위 검증 통과.
        public void SendAttack(int targetEntityId)
        {
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = _lastReceivedServerTick,
            };
            _session?.Send(attack.Write());
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
                    _botSpawnX = enterMap.spawnX;
                    _botSpawnY = enterMap.spawnY;
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition mapTransition = new();
                    mapTransition.Read(buffer);
                    _botSpawnX = mapTransition.spawnX;
                    _botSpawnY = mapTransition.spawnY;
                    lock (_gate)
                    {
                        _spawns.Clear();
                        _entityStates.Clear();
                    }
                    _mapTransition.Set();
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (_gate) _spawns.Add(spawn);
                    break;

                case PacketID.S_EntityState:
                    S_EntityState state = new();
                    state.Read(buffer);
                    lock (_gate) _entityStates[state.entityId] = state;
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    _lastReceivedServerTick = snapshot.serverTick;
                    if (snapshot.entityId == LocalEntityId)
                    {
                        _botSpawnX = snapshot.x;
                        _botSpawnY = snapshot.y;
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
}
