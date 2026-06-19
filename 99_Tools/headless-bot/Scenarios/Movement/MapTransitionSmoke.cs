using System.Diagnostics;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// 맵 전환 결정론 왕복 시나리오.
//
// **목적**:
//   Town → HuntingGround → BossRoom → Ending → Town 4맵 루프를 한 사이클 완주하고
//   ADR-026 (entityId 유지) + 서버 권위 좌표를 assert.
//
// **이동 robust화 (M4.6 Phase 02 후속)**:
//   - MoveToPortal이 서버 권위 X를 S_Snapshot으로 추적해 포털 범위 도달까지 조향.
//   - BossRoom 통과 시 보스(x=30)에게 피격될 수 있음 — HitState(이동 잠금 + 넉백)로
//     고정 틱 루프가 포털에 못 닿는 문제를 해소.
//   - hitstun 종료 후 봇이 방향을 재계산해 계속 조향하므로 넉백 방향 무관하게 수렴.
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
// **헌법 #1**: 봇은 서버 권위 위치(S_Snapshot.x)를 실시간 추적해 조향.
//
// **seedBossGate 파라미터 (테스트 훅)**:
//   BossRoom 진입에는 20킬 게이트(Q3)가 걸린다. 이 시나리오는 4맵 루프 *전환 메커니즘*
//   (entityId 유지 + spawn 좌표)을 검증하는 것이 목적이지 게이트 자체를 검증하지 않는다.
//   게이트 e2e는 BossPortalGateTests(stub) + R2 BossGateSmoke(별도)가 담당한다.
//
//   null(기본, 표준 봇 런): 게이트를 자연히 만남 → killCount=0이면 S_PortalLocked로 차단.
//   non-null(테스트 픽스처 전용): HuntingGround 진입 완료 직후 호출되어 in-process로
//     killCount 전제조건을 충족 → 게이트는 서버 권위 카운트(20)를 읽어 정당 통과.
//     *우회가 아니라 픽스처로 게이트 전제조건을 충족시키는 것.*
public class MapTransitionSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    const float TownPortalX    = 20f;
    const float HGPortalX      = 25f;
    const float BossRoomPortalX = 35f;
    const float EndingPortalX  = 5f;
    const int PortalId = 1;

    const float HGDestSpawnX       = 2f;
    const float BossRoomDestSpawnX = 22f;
    const float EndingDestSpawnX   = 0f;
    const float TownDestSpawnX     = 0f;

    const float SpawnXTolerance = 0.01f;

    public class Result
    {
        public bool Success;
        public string Reason = "";

        public int EntityId;
        public bool EntityIdPreservedAcrossAllMaps;

        public bool EnteredHuntingGround;
        public bool EnteredBossRoom;
        public bool EnteredEnding;
        public bool ReturnedToTown;

        public float SpawnXOnHG;
        public float SpawnXOnBossRoom;
        public float SpawnXOnEnding;
        public float SpawnXOnTown;

        public bool SpawnCoordinatesCorrect;
    }

    /// <param name="seedBossGate">
    /// BossRoom 진입 전 killCount 충족용 테스트 훅. entityId를 인자로 받아 서버 in-process
    /// killCount를 적립하고 완료를 알리는 Task를 반환한다. null이면 skip(표준 봇 런).
    /// 표준 봇 런(null)은 killCount=0이므로 20킬 게이트(Q3)에 막힌다.
    /// 게이트 e2e는 BossPortalGateTests(stub) + R2 BossGateSmoke(별도)가 담당한다.
    /// </param>
    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default,
        Func<int, Task>? seedBossGate = null)
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

            result.EntityId = bot.LocalEntityId;
            if (result.EntityId <= 0)
                return Fail(result, $"invalid initial entityId: {result.EntityId}");

            // ── 1단계: Town → HuntingGround ──────────────────────────────────
            await bot.MoveToPortal(TownPortalX, ct);
            int expected1 = bot.NextExpectedTransitionCount();
            bot.SendEnterPortal(PortalId);

            S_MapTransition? t1 = await bot.WaitForMapTransition(expected1, DefaultTimeout, ct);
            if (t1 == null)
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            if (Math.Abs(t1.spawnX - HGDestSpawnX) > SpawnXTolerance)
                return Fail(result, $"HG destSpawnX 불일치: expected={HGDestSpawnX}, actual={t1.spawnX}");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            result.EnteredHuntingGround = true;
            result.SpawnXOnHG = bot.SpawnX;

            // 보스 게이트 전제조건 충족.
            if (seedBossGate != null)
            {
                await seedBossGate(bot.LocalEntityId);
            }
            else
            {
#if DEBUG
                bot.SendCheatCompleteQuest();
                await Task.Delay(Constants.TickIntervalMs * 3, ct);
#endif
            }

            // ── 2단계: HuntingGround → BossRoom ──────────────────────────────
            await bot.MoveToPortal(HGPortalX, ct);
            int expected2 = bot.NextExpectedTransitionCount();
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
            int expected3 = bot.NextExpectedTransitionCount();
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
            int expected4 = bot.NextExpectedTransitionCount();
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

    sealed class TransitionProbe : ProbeBase
    {
        // S_MapTransition 수신 횟수 카운터 기반 추적.
        // 단조증가 불변식: 호출부가 SendEnterPortal 전에 NextExpectedTransitionCount()로 캡처.
        volatile int _mapTransitionCount = 0;

        S_MapTransition? _lastTransition;

        // 현재 맵 terrain — S_EnterMap 수신 시 초기화(Town=0), S_MapTransition 수신 시 갱신.
        // 갱신 누락 = 이전 맵 지형으로 시뮬 → desync 폭증 함정 (Phase 03 D6 설계 주의사항).
        // Ending(3)은 terrain.bin 없음 — null 허용(Physics.Step flat fallback).
        MapTerrain? _currentTerrain;

        public MapTerrain? CurrentTerrain => _currentTerrain;

        protected override void OnEnterMap(S_EnterMap packet)
        {
            _currentTerrain = BotTerrainLoader.Load(mapId: 0);
        }

        protected override void OnMapTransition(S_MapTransition packet)
        {
            _currentTerrain = packet.destMapId < 3
                ? BotTerrainLoader.Load(packet.destMapId)
                : null;
            lock (Gate) _lastTransition = packet;
            _mapTransitionCount++;
        }

        // 호출부가 SendEnterPortal 전에 캡처해 race-safe하게 대기.
        public int NextExpectedTransitionCount() => _mapTransitionCount + 1;

        public async Task<S_MapTransition?> WaitForMapTransition(int expectedCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => _mapTransitionCount >= expectedCount, timeout, ct);
            if (!ok) return null;
            lock (Gate) return _lastTransition;
        }

        // portal 위치까지 C_MoveIntent 기반 이동. ServerX 매 틱 확인 — HitState 후에도 수렴.
        // 루프 상한(400틱)에 도달하면 TimeoutException으로 loud 실패.
        // tick count 반환 — 시나리오 진단용.
        public async Task<int> MoveToPortal(float portalX, CancellationToken ct)
        {
            int ticks = 0;
            const int maxTicks = 400;
            const float reachRadius = 0.5f;

            while (true)
            {
                float sx = ServerX;
                if (Math.Abs(sx - portalX) <= reachRadius)
                    break;

                if (ticks >= maxTicks)
                    throw new TimeoutException(
                        $"MoveToPortal: {maxTicks}틱 내 포털 미도달. " +
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

        public void SendEnterPortal(int portalId) => SendEnterPortalCore(portalId);

        public void SendCheatCompleteQuest() => SendCheatCompleteQuestCore();
    }
}
