using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// BossRoom 포탈 잠금 게이트(Q3) e2e 스모크.
//
// **검증 시나리오 2개**:
//   [거부 경로] killCount=0 → HG→BossRoom 포탈 시도 → S_PortalLocked 수신
//              (S_MapTransition 오지 않음) → 차단 확인.
//   [통과 경로] seedBossGate 콜백으로 킬카운트 40 충족 → 재시도 → S_MapTransition 수신.
//
// **standalone vs xUnit 분리**:
//   seedBossGate=null (standalone, Program.cs): 거부 경로만 검증. 통과는 xUnit 전용.
//   seedBossGate=Func (xUnit): 거부→시드→재시도(통과) 전체 플로우 검증.
//
// **portal 좌표 상수** (서버 PortalTable.cs와 정합 — 변경 시 양쪽 동기화 의무):
//   Town x=20 → HuntingGround destSpawn x=2.
//   HuntingGround x=25 → BossRoom destSpawn x=22.
public class BossGateSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan LockedWaitTimeout = TimeSpan.FromSeconds(2);
    static readonly TimeSpan TransitionTimeout = TimeSpan.FromSeconds(5);

    // portal 좌표 상수 — 서버 PortalTable.cs와 정합.
    const float TownPortalX = 20f;
    const int TownPortalId = 1;
    const float HGPortalX = 25f;
    const int HGPortalId = 1;

    // HG→BossRoom 진입 시 기대 destSpawnX (BossStageClearSmoke 상수와 정합).
    const float BossRoomDestSpawnX = 22f;
    const float SpawnXTolerance = 0.01f;

    public class Result
    {
        public bool Success;
        public string Reason = "";

        // 거부 경로 결과
        public bool SawPortalLocked;
        public int RequiredCount;
        public int CurrentCount;

        // 통과 경로 결과 (seedBossGate 제공 시만 유효)
        public bool EnteredBossRoom;

        public int LocalEntityId;
    }

    /// <param name="seedBossGate">
    /// BossRoom 진입 전 killCount 충족용 테스트 훅. entityId를 받아 서버 in-process
    /// killCount를 40으로 충족하고 완료를 알리는 Task를 반환한다.
    /// null이면 거부 경로만 검증하고 종료(standalone 봇 런).
    /// </param>
    public static async Task<Result> Run(
        string host, int port,
        Func<int, Task>? seedBossGate = null,
        CancellationToken ct = default)
    {
        Result result = new();
        GateProbe bot = new();

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

            result.LocalEntityId = bot.LocalEntityId;

            // ── 1단계: Town → HuntingGround (게이트 없음, 통과) ──────────────
            await bot.MoveToPortal(TownPortalX, ct);
            int expected1 = bot.NextExpectedTransitionCount();
            bot.SendEnterPortal(TownPortalId);

            S_MapTransition? t1 = await bot.WaitForMapTransition(expected1, DefaultTimeout, ct);
            if (t1 == null)
                return Fail(result, "S_MapTransition timeout (5s) — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // ── 2단계: HG → BossRoom 거부 경로 (killCount=0) ─────────────────
            // S_MapTransition이 오면 안 됨. S_PortalLocked만 와야 함.
            await bot.MoveToPortal(HGPortalX, ct);
            int expectedBeforeRetry = bot.NextExpectedTransitionCount(); // race 안전 캡처
            bot.SendEnterPortal(HGPortalId);

            // S_PortalLocked 대기 (최대 2s). S_MapTransition이 도착하면 게이트가 안 걸린 것.
            bool gotLocked = await bot.WaitForPortalLocked(LockedWaitTimeout, ct);

            // 거부 경로에서 맵 전환이 발생했다면 게이트 결함.
            if (bot.TransitionCount > expectedBeforeRetry - 1)
            {
                return Fail(result, "S_MapTransition arrived with killCount=0 — BossRoom gate not working");
            }

            if (!gotLocked)
                return Fail(result, "S_PortalLocked not received within 2s — gate did not fire");

            result.SawPortalLocked = true;
            result.RequiredCount = bot.LastLockedRequiredCount;
            result.CurrentCount = bot.LastLockedCurrentCount;

            if (result.RequiredCount != 40)
                return Fail(result, $"S_PortalLocked.requiredCount expected=40, actual={result.RequiredCount}");

            if (result.CurrentCount != 0)
                return Fail(result, $"S_PortalLocked.currentCount expected=0, actual={result.CurrentCount}");

            // ── standalone 봇 런: 거부 경로까지만 검증하고 성공 반환 ──────────
            if (seedBossGate == null)
            {
                result.Success = true;
                return result;
            }

            // ── 3단계: killCount 시드 (xUnit 테스트 전용) ────────────────────
            // in-process로 PartyRegistry에 40킬 적립 → 다음 틱에 게이트 통과 준비.
            // 우회 X — 게이트는 서버 권위 카운트를 실제 검사 (헌법 §1, §3 정합).
            await seedBossGate(bot.LocalEntityId);

            // ── 4단계: HG → BossRoom 재시도 (통과 경로) ─────────────────────
            // PortalLocked 플래그 리셋 + 전환 카운터 캡처(race 안전).
            bot.ResetPortalLocked();
            int expectedTransition = bot.NextExpectedTransitionCount();
            bot.SendEnterPortal(HGPortalId);

            S_MapTransition? t2 = await bot.WaitForMapTransition(expectedTransition, TransitionTimeout, ct);
            if (t2 == null)
                return Fail(result, $"S_MapTransition timeout (5s) — HG→BossRoom after seed (killCount=40)");

            if (Math.Abs(t2.spawnX - BossRoomDestSpawnX) > SpawnXTolerance)
                return Fail(result,
                    $"BossRoom destSpawnX 불일치: expected={BossRoomDestSpawnX}, actual={t2.spawnX}");

            result.EnteredBossRoom = true;
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

    sealed class GateProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);

        // S_MapTransition 카운터 — NextExpectedTransitionCount / WaitForMapTransition 패턴.
        // (MapTransitionScenario race-safe 설계와 동일.)
        volatile int _mapTransitionCount = 0;
        S_MapTransition? _lastTransition;

        // S_PortalLocked 수신 여부 + 마지막 값.
        volatile bool _gotPortalLocked = false;
        int _lastLockedRequired;
        int _lastLockedCurrent;

        // 서버 권위 X — S_Snapshot으로 추적 (MoveToPortal robust화).
        volatile float _serverX;

        BotSession? _session;
        uint _clientTick;

        const int MoveToPortalMaxTicks = 400;
        const float PortalReachRadius = 0.5f;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;
        public float SpawnX { get; private set; }

        public int TransitionCount => _mapTransitionCount;
        public bool GotPortalLocked => _gotPortalLocked;
        public int LastLockedRequiredCount => _lastLockedRequired;
        public int LastLockedCurrentCount => _lastLockedCurrent;

        public int NextExpectedTransitionCount() => _mapTransitionCount + 1;

        public void ResetPortalLocked()
        {
            lock (_gate) _gotPortalLocked = false;
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

        public async Task<bool> WaitForPortalLocked(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _gotPortalLocked, timeout, ct);

        public async Task<S_MapTransition?> WaitForMapTransition(int expectedCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => _mapTransitionCount >= expectedCount, timeout, ct);
            if (!ok) return null;
            lock (_gate) return _lastTransition;
        }

        // portal 위치까지 서버 권위 X(_serverX) 기반 이동.
        // MapTransitionScenario와 동일한 robust MoveToPortal 패턴.
        public async Task<int> MoveToPortal(float portalX, CancellationToken ct)
        {
            int ticks = 0;
            while (true)
            {
                float sx = _serverX;
                if (Math.Abs(sx - portalX) <= PortalReachRadius)
                    break;

                if (ticks >= MoveToPortalMaxTicks)
                    throw new TimeoutException(
                        $"MoveToPortal: {MoveToPortalMaxTicks}틱 내 포털 미도달. " +
                        $"portalX={portalX}, serverX={sx}");

                sbyte dir = sx < portalX ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
                ticks++;
            }
            SendMove(0);
            await Task.Delay(150, ct);
            return ticks + 1;
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
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Knight };
                        _session?.Send(charSelect.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    SpawnX = enterMap.spawnX;
                    _serverX = enterMap.spawnX;
                    if (!_enterMap.IsSet) _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition transition = new();
                    transition.Read(buffer);
                    SpawnX = transition.spawnX;
                    _serverX = transition.spawnX;
                    lock (_gate)
                    {
                        _lastTransition = transition;
                        _mapTransitionCount++;
                    }
                    break;

                case PacketID.S_PortalLocked:
                    S_PortalLocked locked = new();
                    locked.Read(buffer);
                    lock (_gate)
                    {
                        _lastLockedRequired = locked.requiredCount;
                        _lastLockedCurrent = locked.currentCount;
                        _gotPortalLocked = true;
                    }
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    if (snapshot.entityId == LocalEntityId)
                        _serverX = snapshot.x;
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
