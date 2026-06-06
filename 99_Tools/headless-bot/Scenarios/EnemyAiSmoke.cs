using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Enemy AI smoke.
//
// 시나리오:
//   1. 서버에 접속 + handshake + Town 진입
//   2. Town portal로 HuntingGround 진입 → 초기 roster의 Normal enemy 수집
//   3. 타겟 선정: 같은 높이(점프 없이 도달) + 현재 aggro 밖(Patrol 관측 보장) 중 가장 가까운 Normal
//   4. 타겟이 Patrol 상태로 도는 것을 S_EntityState로 확인
//   5. 봇이 타겟의 스폰 보고 좌표로 접근 → 서버 |dx| aggro 판정 진입
//   6. S_EntityState에서 state=Chase(2) 전환 확인
//   7. Chase 중 |enemy.x - bot.x| 감소(봇 방향 접근)를 좌표 변화로 검증 (soft)
//
// **검증 전략**:
//   - S_EntityState 패킷(PacketID=19) 수신 후 state 필드 확인
//   - state byte: 0=Idle, 1=Patrol, 2=Chase (EnemyState enum 정합)
//   - 서버 로그는 봇에서 직접 볼 수 없으므로 클라 패킷 수신으로 간접 검증
//
// **좌표 하드코딩 X (M4.4-03)**:
//   적 스폰은 bake 산출 content.bin이 단일 진실(서버 전용) — 봇은 S_EntitySpawn 수신값으로
//   타겟/접근 좌표를 런타임 결정. 재bake로 배치가 바뀌어도 시나리오 수정 불필요.
//   aggro 판정은 서버가 |dx| 단독 사용(EnemyAISystem) — 접근 좌표도 X축만 계산.
public class EnemyAiSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(8);

    const byte StatePatrol = 1;
    const byte StateChase  = 2;

    // HG portal (Town → HG) — 서버 PortalTable.cs와 정합 (포탈 bake 이행은 M4.5+ 이월).
    const float TownPortalX = 20f;
    const int   TownPortalId = 1;

    // 같은 높이 판정 허용 오차 — 점프 없이 접근 가능한 타겟만 고름 (발판 위 적 제외).
    const float SamePlaneTolerance = 1.5f;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int EnemyEntityId;
        public float EnemyInitialX;
        public float EnemyXAtChase;
        public bool SawPatrolState;
        public bool SawChaseState;
        public bool SawXMovement; // Chase 중 |enemy.x - bot.x|가 감소했는지 (방향 무관 접근 검증)
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

            // Town portal → HuntingGround
            await bot.MoveToX(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);

            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            // 서버 tick thread가 다음 맵 job 처리까지 대기
            await Task.Delay(Constants.TickIntervalMs * 3, ct);

            // HG 초기 roster의 Normal enemy 수집 (S_EntitySpawn with kind=0=Normal).
            // 초기 spawn 묶음은 진입 직후 함께 도착 — 첫 마리 수신 후 짧은 유예로 전부 수집.
            IReadOnlyList<S_EntitySpawn> normals = await bot.CollectNormalSpawns(
                DefaultTimeout, TimeSpan.FromMilliseconds(Constants.TickIntervalMs * 4), ct);
            if (normals.Count == 0)
                return Fail(result, "Normal enemy S_EntitySpawn timeout in HuntingGround");

            // 타겟 선정: 같은 높이 + 현재 aggro 밖 중 |dx| 최소.
            // aggro 밖 조건 = Patrol 상태를 먼저 관측할 수 있다는 보장 (진입 즉시 Chase면 4단계 검증 불가).
            float aggroRange = EnemyStats.NormalDefault().AggroRange;
            float botX = bot.BotX;
            float botY = bot.BotY;

            S_EntitySpawn? target = normals
                .Where(s => Math.Abs(s.y - botY) <= SamePlaneTolerance
                         && Math.Abs(s.x - botX) > aggroRange)
                .OrderBy(s => Math.Abs(s.x - botX))
                .FirstOrDefault();
            if (target == null)
                return Fail(result,
                    $"적합한 타겟 없음 — 같은 높이(±{SamePlaneTolerance}) + aggro({aggroRange}) 밖 Normal이 0마리. " +
                    $"수집 {normals.Count}마리: {string.Join(", ", normals.Select(s => $"({s.x:F2},{s.y:F2})"))} " +
                    $"/ bot=({botX:F2},{botY:F2}). 재bake로 스폰 배치가 바뀌었으면 시나리오 전제 재검토.");

            result.EnemyEntityId = target.entityId;
            result.EnemyInitialX = target.x;

            // Patrol 상태 S_EntityState 대기 (enemy가 움직이고 있을 것)
            // SnapshotTickInterval(=2) * 50ms = 100ms 주기로 broadcast됨
            bool gotPatrol = await bot.WaitForEnemyState(
                target.entityId, StatePatrol, DefaultTimeout, ct);
            if (!gotPatrol)
                return Fail(result, $"S_EntityState(Patrol) not received for enemy {target.entityId} within timeout. " +
                                    $"Last known state: {bot.LastStateFor(target.entityId)}");

            result.SawPatrolState = true;

            // 봇이 타겟의 스폰 보고 좌표로 접근. 보고 x = 관측 시점 patrol 위치(센터 아님)지만,
            // patrol 왕복(±PatrolRange)이 매 주기 |dx|<=AggroRange 영역을 통과하므로 Chase 전환 보장.
            await bot.MoveToX(target.x, ct);

            // Chase 전환 대기 — 봇이 aggro 범위 안에 들어간 후 다음 S_EntityState에서 Chase
            bool gotChase = await bot.WaitForEnemyState(
                target.entityId, StateChase, DefaultTimeout, ct);
            if (!gotChase)
                return Fail(result, $"S_EntityState(Chase) not received after aggro entry. " +
                                    $"Last known state: {bot.LastStateFor(target.entityId)}, " +
                                    $"Last known x: {bot.LastXFor(target.entityId):F2}");

            result.SawChaseState = true;
            result.EnemyXAtChase = bot.LastXFor(target.entityId);

            // 접근 검증: Chase 중 적이 봇 방향으로 이동 → |enemy.x - bot.x| 감소 (방향 무관).
            // 첫 Chase 틱이라면 아직 이동량이 작을 수 있으므로 몇 tick 더 관찰 후 비교.
            float chaseDistInitial = Math.Abs(result.EnemyXAtChase - bot.BotX);
            await Task.Delay(500, ct); // 몇 tick 더 관찰

            float enemyCurrentX = bot.LastXFor(target.entityId);
            float chaseDistFinal = Math.Abs(enemyCurrentX - bot.BotX);
            result.SawXMovement = chaseDistFinal < chaseDistInitial;

            if (!result.SawXMovement)
            {
                // 접근이 안 보여도 Chase 상태 전환 자체는 검증 완료이므로 경고 수준
                Console.WriteLine($"[AiSmoke] Warning: enemy did not approach. " +
                                  $"dist atChase={chaseDistInitial:F2}, after={chaseDistFinal:F2}");
                Console.WriteLine($"  This may happen if enemy just transitioned to Chase or already overlaps the bot — not a failure.");
                // 접근은 soft check — Chase 상태 전환이 핵심 검증
            }

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

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;

        // 봇 현재 위치 추정 (서버 권위 S_Snapshot으로 보정됨) — 타겟 선정/접근 거리 계산용.
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

        // 첫 Normal spawn 수신까지 대기 후, grace 동안 추가 수집해 전부 반환.
        // "첫 마리" 단일 반환이 아닌 목록 반환 — 타겟 선정은 호출부 책임 (재bake 내성).
        public async Task<IReadOnlyList<S_EntitySpawn>> CollectNormalSpawns(
            TimeSpan firstTimeout, TimeSpan grace, CancellationToken ct)
        {
            // Normal enemy kind = 0
            const byte NormalKind = 0;
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == NormalKind); },
                firstTimeout, ct);
            if (!ok) return Array.Empty<S_EntitySpawn>();

            await Task.Delay(grace, ct);
            lock (_gate) return _spawns.Where(s => s.entityKind == NormalKind).ToList();
        }

        public async Task<bool> WaitForEnemyState(int entityId, byte targetState, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(
                () => { lock (_gate) return _entityStates.TryGetValue(entityId, out S_EntityState? s) && s.state == targetState; },
                timeout, ct);

        // x 위치까지 C_MoveIntent 기반 이동
        public async Task MoveToX(float targetX, CancellationToken ct)
        {
            float delta = targetX - _botSpawnX;
            if (Math.Abs(delta) < 0.1f) return;

            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (Constants.MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 200);

            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
            // 봇 위치 추정 업데이트 (서버 권위 snapshot이 없으면 추정치)
            _botSpawnX = targetX;
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal packet = new() { portalId = portalId };
            _session?.Send(packet.Write());
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
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Warrior };
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
                    // 새 맵 roster로 교체 — 이전 맵 spawn/state가 타겟 선정에 섞이지 않게 초기화.
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
                    // 봇 위치 업데이트 (자기 자신 snapshot 시)
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
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
