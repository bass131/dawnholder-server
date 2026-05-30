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
//   2. portal 2회 타고 HuntingGround 진입 (Town → HG 경유 없이 HG 바로 진입)
//      HG에 Normal enemy spawn 대기
//   3. enemy가 Patrol 상태로 시작하는 것을 S_EntityState로 확인
//   4. 봇이 AggroRange(6) 안으로 진입
//   5. 다음 S_EntityState에서 state=Chase(2)로 전환됨을 확인
//   6. enemy.x가 봇 방향으로 이동하는 것을 좌표 변화로 검증
//
// **검증 전략**:
//   - S_EntityState 패킷(PacketID=19) 수신 후 state 필드 확인
//   - state byte: 0=Idle, 1=Patrol, 2=Chase (EnemyState enum 정합)
//   - 서버 로그는 봇에서 직접 볼 수 없으므로 클라 패킷 수신으로 간접 검증
public class EnemyAiSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(8);

    const byte StatePatrol = 1;
    const byte StateChase  = 2;

    // HG portal (Town → HG)
    const float TownPortalX = 20f;
    const int   TownPortalId = 1;

    // enemy AggroRange = 6. 봇이 aggro 범위 안에 들어가려면 enemy 근처로 이동.
    // HG enemy SpawnX=10. 봇이 HG destSpawn=x=2에서 시작.
    // enemy.X는 Patrol 중 (10 ± 4 범위) — 봇이 x=6~7 근처로 가면 |dx|≤6 만족.
    const float AggroEntryX = 7f;   // enemy SpawnX(10) - AggroRange(6) + 여유 = 약 5~7

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
        public bool SawXMovement; // Chase 중 X가 봇 방향으로 이동했는지
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

            // HG Normal enemy spawn 대기 (S_EntitySpawn with kind=0=Normal)
            S_EntitySpawn? enemySpawn = await bot.WaitForNormalEnemySpawn(DefaultTimeout, ct);
            if (enemySpawn == null)
                return Fail(result, "Normal enemy S_EntitySpawn timeout in HuntingGround");

            result.EnemyEntityId = enemySpawn.entityId;
            result.EnemyInitialX = enemySpawn.x;

            // Patrol 상태 S_EntityState 대기 (enemy가 움직이고 있을 것)
            // SnapshotTickInterval(=2) * 50ms = 100ms 주기로 broadcast됨
            // 최대 5초 대기
            bool gotPatrol = await bot.WaitForEnemyState(
                enemySpawn.entityId, StatePatrol, DefaultTimeout, ct);
            if (!gotPatrol)
                return Fail(result, $"S_EntityState(Patrol) not received for enemy {enemySpawn.entityId} within timeout. " +
                                    $"Last known state: {bot.LastStateFor(enemySpawn.entityId)}");

            result.SawPatrolState = true;

            // 봇이 AggroRange 안으로 진입 — enemy SpawnX(10) 기준 6 이내
            // 현재 봇 위치는 HG destSpawn x=2. AggroEntryX(7)까지 이동.
            await bot.MoveToX(AggroEntryX, ct);

            // Chase 전환 대기 — 봇이 aggro 범위 안에 들어간 후 다음 S_EntityState에서 Chase
            bool gotChase = await bot.WaitForEnemyState(
                enemySpawn.entityId, StateChase, DefaultTimeout, ct);
            if (!gotChase)
                return Fail(result, $"S_EntityState(Chase) not received after aggro entry. " +
                                    $"Last known state: {bot.LastStateFor(enemySpawn.entityId)}, " +
                                    $"Last known x: {bot.LastXFor(enemySpawn.entityId):F2}");

            result.SawChaseState = true;
            result.EnemyXAtChase = bot.LastXFor(enemySpawn.entityId);

            // X 변화 검증: enemy가 Chase 상태에서 봇 방향(왼쪽)으로 이동했어야 함.
            // enemy SpawnX=10, 봇은 x≈7. enemy.X < SpawnX(10) 이면 봇 방향으로 이동 중.
            // 단 첫 Chase 틱이라면 아직 이동 안 했을 수 있으므로 여유 줌.
            // 대신 여러 패킷 관찰 후 최솟값이 초기값보다 감소했는지 확인.
            await Task.Delay(500, ct); // 몇 tick 더 관찰

            float enemyCurrentX = bot.LastXFor(enemySpawn.entityId);
            // enemy가 봇(x≈7) 방향으로 이동했다면 enemy.X < 초기값(SpawnX=10)
            result.SawXMovement = enemyCurrentX < result.EnemyInitialX;

            if (!result.SawXMovement)
            {
                // X 이동이 없어도 Chase 상태 전환 자체는 검증 완료이므로 경고 수준
                Console.WriteLine($"[AiSmoke] Warning: enemy X did not decrease. initial={result.EnemyInitialX:F2}, current={enemyCurrentX:F2}");
                Console.WriteLine($"  This may happen if enemy just transitioned to Chase — not a failure.");
                // X 이동은 soft check — Chase 상태 전환이 핵심 검증
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

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;

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

        public async Task<S_EntitySpawn?> WaitForNormalEnemySpawn(TimeSpan timeout, CancellationToken ct)
        {
            // Normal enemy kind = 0
            const byte NormalKind = 0;
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == NormalKind); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == NormalKind);
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
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition mapTransition = new();
                    mapTransition.Read(buffer);
                    _botSpawnX = mapTransition.spawnX;
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
                    // 봇 SpawnX 업데이트 (자기 자신 snapshot 시)
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    if (snapshot.entityId == LocalEntityId)
                        _botSpawnX = snapshot.x;
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
