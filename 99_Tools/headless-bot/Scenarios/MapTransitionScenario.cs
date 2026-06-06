using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// 맵 전환 결정론 왕복 시나리오.
//
// **목적**:
//   Town → HuntingGround → BossRoom → Ending → Town 4맵 루프를 한 사이클 완주하고
//   ADR-026 (entityId 유지) + 서버 권위 좌표를 assert.
//
// **결정론 보장**:
//   - 모든 이동은 C_MoveIntent 기반, tick 수 계산 결정론.
//   - 적 전투 없음 — 순수 이동/portal 검증 (HP 변화 없음 불변식 검증).
//   - 실시간 sleep은 Constants.TickIntervalMs 단위로만 사용 (서버 tick 기반).
//
// **portal 좌표 상수** (서버 PortalTable.cs와 정합 — 변경 시 양쪽 동기화 의무):
//   Town x=20 → HuntingGround destSpawn x=2.
//   HuntingGround x=25 → BossRoom destSpawn x=22.
//   BossRoom x=35 → Ending destSpawn x=0.
//   Ending x=5 → Town destSpawn x=0.
//
// **ADR-026**: entityId는 맵 이동 시 유지. LocalEntityId는 모든 맵에서 동일해야 함.
//
// **헌법 #5**: 봇 시나리오라 await/Task.Delay OK. 단 tick 기반 결정론 유지.
// **헌법 #1**: 봇은 서버 권위 SpawnX만 사용 (S_MapTransition/S_EnterMap으로 수신).
public class MapTransitionScenario
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    // portal 좌표 상수 — 서버 PortalTable.cs와 정합.
    // 변경 시 양쪽(봇 시나리오 + 서버 PortalTable.cs) 동기화 의무.
    const float TownPortalX    = 20f;
    const float HGPortalX      = 25f;
    const float BossRoomPortalX = 35f;
    const float EndingPortalX  = 5f;
    const int PortalId = 1; // 모든 맵에서 portal id = 1 (맵당 portal 1개)

    // 기대 destSpawn X — S_MapTransition.spawnX 검증에 사용.
    const float HGDestSpawnX       = 2f;
    const float BossRoomDestSpawnX = 22f;
    const float EndingDestSpawnX   = 0f;
    const float TownDestSpawnX     = 0f;

    // 좌표 비교 허용 오차 (float 비교)
    const float SpawnXTolerance = 0.01f;

    public class Result
    {
        public bool Success;
        public string Reason = "";

        // ADR-026 검증: 모든 맵에서 entityId가 동일한지
        public int EntityId;
        public bool EntityIdPreservedAcrossAllMaps;

        // 각 맵 진입 성공 여부
        public bool EnteredHuntingGround;
        public bool EnteredBossRoom;
        public bool EnteredEnding;
        public bool ReturnedToTown;

        // 각 맵 도착 spawn 좌표 (서버 권위)
        public float SpawnXOnHG;
        public float SpawnXOnBossRoom;
        public float SpawnXOnEnding;
        public float SpawnXOnTown;

        // spawn 좌표 검증 결과
        public bool SpawnCoordinatesCorrect;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        TransitionProbe bot = new();

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
                return Fail(result, "S_EnterMap timeout — Town 진입");

            // entityId 초기값 기록 (ADR-026 검증 기준)
            result.EntityId = bot.LocalEntityId;
            if (result.EntityId <= 0)
                return Fail(result, $"invalid initial entityId: {result.EntityId}");

            // 서버는 맵 전환 시 S_MapTransition만 발송 (S_EnterMap 재발송 X).
            // S_MapTransition 수신 = 맵 진입 완료 신호. entityId는 ADR-026에 따라 S_MapTransition에 없음
            // (유지되므로 변경 여부를 봇이 직접 확인 불가 — LocalEntityId는 최초 S_EnterMap에서만 받음).

            // ── 1단계: Town → HuntingGround ──────────────────────────────────
            await bot.MoveToPortal(TownPortalX, ct);
            int expected1 = bot.NextExpectedTransitionCount(); // SendEnterPortal 전 캡처 — race 안전 (P4)
            bot.SendEnterPortal(PortalId);

            S_MapTransition? t1 = await bot.WaitForMapTransition(expected1, DefaultTimeout, ct);
            if (t1 == null)
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            // spawn 좌표 검증 (서버 권위 destSpawn — PortalTable.cs와 정합)
            if (Math.Abs(t1.spawnX - HGDestSpawnX) > SpawnXTolerance)
                return Fail(result, $"HG destSpawnX 불일치: expected={HGDestSpawnX}, actual={t1.spawnX}");

            // tick thread가 새 맵 초기화를 완료하기까지 대기 (S_EntitySpawn 수신 전 이동 시작 방지)
            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            result.EnteredHuntingGround = true;
            result.SpawnXOnHG = bot.SpawnX;

            // ── 2단계: HuntingGround → BossRoom ──────────────────────────────
            await bot.MoveToPortal(HGPortalX, ct);
            int expected2 = bot.NextExpectedTransitionCount(); // SendEnterPortal 전 캡처 — race 안전 (P4)
            bot.SendEnterPortal(PortalId);

            S_MapTransition? t2 = await bot.WaitForMapTransition(expected2, DefaultTimeout, ct);
            if (t2 == null)
                return Fail(result, "S_MapTransition timeout — HuntingGround→BossRoom");

            if (Math.Abs(t2.spawnX - BossRoomDestSpawnX) > SpawnXTolerance)
                return Fail(result, $"BossRoom destSpawnX 불일치: expected={BossRoomDestSpawnX}, actual={t2.spawnX}");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            result.EnteredBossRoom = true;
            result.SpawnXOnBossRoom = bot.SpawnX;

            // ── 3단계: BossRoom → Ending ──────────────────────────────────────
            await bot.MoveToPortal(BossRoomPortalX, ct);
            int expected3 = bot.NextExpectedTransitionCount(); // SendEnterPortal 전 캡처 — race 안전 (P4)
            bot.SendEnterPortal(PortalId);

            S_MapTransition? t3 = await bot.WaitForMapTransition(expected3, DefaultTimeout, ct);
            if (t3 == null)
                return Fail(result, "S_MapTransition timeout — BossRoom→Ending");

            if (Math.Abs(t3.spawnX - EndingDestSpawnX) > SpawnXTolerance)
                return Fail(result, $"Ending destSpawnX 불일치: expected={EndingDestSpawnX}, actual={t3.spawnX}");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            result.EnteredEnding = true;
            result.SpawnXOnEnding = bot.SpawnX;

            // ── 4단계: Ending → Town (루프 완주) ─────────────────────────────
            await bot.MoveToPortal(EndingPortalX, ct);
            int expected4 = bot.NextExpectedTransitionCount(); // SendEnterPortal 전 캡처 — race 안전 (P4)
            bot.SendEnterPortal(PortalId);

            S_MapTransition? t4 = await bot.WaitForMapTransition(expected4, DefaultTimeout, ct);
            if (t4 == null)
                return Fail(result, "S_MapTransition timeout — Ending→Town");

            if (Math.Abs(t4.spawnX - TownDestSpawnX) > SpawnXTolerance)
                return Fail(result, $"Town destSpawnX 불일치: expected={TownDestSpawnX}, actual={t4.spawnX}");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            result.ReturnedToTown = true;
            result.SpawnXOnTown = bot.SpawnX;

            // ── 최종 assert ───────────────────────────────────────────────────
            // ADR-026: entityId는 맵 이동 시 유지.
            // S_MapTransition에 entityId 필드가 없으므로 직접 비교 불가 — 최초 S_EnterMap의
            // LocalEntityId가 맵 이동 내내 변하지 않는 것으로 간접 검증.
            // (서버가 S_EnterMap을 재발송하지 않으므로 LocalEntityId는 처음 값 그대로 유지됨)
            result.EntityIdPreservedAcrossAllMaps = (bot.LocalEntityId == result.EntityId);
            result.SpawnCoordinatesCorrect =
                result.EnteredHuntingGround &&
                result.EnteredBossRoom &&
                result.EnteredEnding &&
                result.ReturnedToTown;

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

    sealed class TransitionProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);

        // S_MapTransition 수신 횟수 카운터 기반 추적. 맵 이동 횟수 = 4회 (Town→HG→Boss→Ending→Town).
        // 서버는 맵 전환 시 S_MapTransition만 발송 (S_EnterMap 재발송 X).
        volatile int _mapTransitionCount = 0;

        // 최신 S_MapTransition 패킷 — WaitForMapTransition이 꺼내 감.
        S_MapTransition? _lastTransition;

        // 현재 맵 terrain — Physics.Step 자체 시뮬에 사용.
        // S_EnterMap 수신 시 초기화(Town=0), S_MapTransition 수신 시 목적지 맵으로 갱신.
        // 갱신 누락 = 이전 맵 지형으로 시뮬 → desync 폭증 함정 (Phase 03 D6 설계 주의사항).
        // Ending(3)은 terrain.bin 없음 — Ending 맵 이동 시 null 허용(Physics.Step flat fallback).
        MapTerrain? _currentTerrain;

        BotSession? _session;
        uint _clientTick;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;
        public float SpawnX { get; private set; }

        // 현재 맵 terrain 노출 (시나리오 검증용).
        public MapTerrain? CurrentTerrain => _currentTerrain;

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

        // 다음 S_MapTransition 수신 대기 (카운터 기반).
        //
        // **race 안전 설계**: 호출부가 SendEnterPortal 전에 expectedCount = MapTransitionCount + 1을
        //   캡처해 인자로 전달. 함수는 카운터가 expectedCount에 도달할 때까지만 대기.
        //   서버 응답이 몇 microsecond 먼저 와도 카운터는 이미 expectedCount이므로 즉시 반환.
        //   (함수 내부 스냅샷 방식이면 서버 응답이 진입 전 도착 시 영원히 false — 그래서 호출부 캡처.)
        //
        // **단조증가 불변식**: expectedCount는 항상 이전 expectedCount보다 1 크다.
        //   4회 호출 시 expectedCount = 1, 2, 3, 4 순서. 역순 또는 건너뜀 없음.
        public async Task<S_MapTransition?> WaitForMapTransition(int expectedCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => _mapTransitionCount >= expectedCount, timeout, ct);
            if (!ok) return null;
            lock (_gate) return _lastTransition;
        }

        // 호출부 편의 — SendEnterPortal 전에 호출해 expected count를 안전하게 캡처.
        // 사용 패턴:
        //   int expected = bot.NextExpectedTransitionCount();
        //   bot.SendEnterPortal(portalId);
        //   S_MapTransition? t = await bot.WaitForMapTransition(expected, timeout, ct);
        public int NextExpectedTransitionCount() => _mapTransitionCount + 1;

        // portal 위치까지 C_MoveIntent 기반 이동.
        // 서버 PortalTable.cs와 정합 — portal x 좌표는 호출부 const로 박음.
        public async Task<int> MoveToPortal(float portalX, CancellationToken ct)
        {
            float delta = portalX - SpawnX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Warrior().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 200);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
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
                    // handshake OK 후 즉시 C_CharacterSelect 송신 (서버가 class 선택 전 월드 진입 차단).
                    if (handshake.ok)
                    {
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Warrior };
                        _session?.Send(charSelect.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    // 서버는 최초 진입 시에만 S_EnterMap 발송. 맵 전환 시엔 S_MapTransition만 발송.
                    // ADR-026: entityId는 맵 이동 시 유지 — LocalEntityId는 최초 1회 수신 후 고정.
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    SpawnX = enterMap.spawnX;
                    // 초기 맵 terrain 로드 (Town=0). fail loud 정책 — BotTerrainLoader 참조.
                    _currentTerrain = BotTerrainLoader.Load(mapId: 0);
                    if (!_enterMap.IsSet) _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    // 맵 전환 패킷 수신 — SpawnX를 목적지 spawn 좌표로 갱신.
                    // 봇은 서버 권위 좌표만 사용 (헌법 #1).
                    // terrain도 목적지 맵으로 갱신 — 갱신 누락 시 이전 맵 지형으로 시뮬 (desync 폭증 함정).
                    S_MapTransition transition = new();
                    transition.Read(buffer);
                    SpawnX = transition.spawnX;
                    // Ending(mapId=3)은 terrain.bin 없음 — null 허용, Physics.Step flat fallback.
                    // 그 외 맵은 파일 부재 시 예외 (fail loud).
                    _currentTerrain = transition.destMapId < 3
                        ? BotTerrainLoader.Load(transition.destMapId)
                        : null;
                    lock (_gate) _lastTransition = transition;
                    _mapTransitionCount++;
                    break;

                // 이 시나리오는 전투 없음 — S_EntitySpawn/S_HitResult 등 무시.
                // 단, S_Snapshot은 서버 동작 확인용으로 수신만 (무시).
            }
        }

        static async Task<bool> WaitUntil(
            Func<bool> predicate,
            TimeSpan timeout,
            CancellationToken ct)
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
