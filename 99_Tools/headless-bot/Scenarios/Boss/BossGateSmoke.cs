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
//   [통과 경로] seedBossGate 콜백으로 킬카운트 20 충족 → 재시도 → S_MapTransition 수신.
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

    const float TownPortalX = 20f;
    const int TownPortalId = 1;
    const float HGPortalX = 25f;
    const int HGPortalId = 1;

    const float BossRoomDestSpawnX = 22f;
    const float SpawnXTolerance = 0.01f;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public bool SawPortalLocked;
        public int RequiredCount;
        public int CurrentCount;
        public bool EnteredBossRoom;
        public int LocalEntityId;
    }

    /// <param name="seedBossGate">
    /// BossRoom 진입 전 killCount 충족용 테스트 훅. entityId를 받아 서버 in-process
    /// killCount를 20으로 충족하고 완료를 알리는 Task를 반환한다.
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

            await bot.MoveToPortal(TownPortalX, ct);
            int expected1 = bot.NextExpectedTransitionCount();
            bot.SendEnterPortal(TownPortalId);

            S_MapTransition? t1 = await bot.WaitForMapTransition(expected1, DefaultTimeout, ct);
            if (t1 == null)
                return Fail(result, "S_MapTransition timeout (5s) — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            await bot.MoveToPortal(HGPortalX, ct);
            int expectedBeforeRetry = bot.NextExpectedTransitionCount();
            bot.SendEnterPortal(HGPortalId);

            bool gotLocked = await bot.WaitForPortalLocked(LockedWaitTimeout, ct);

            if (bot.TransitionCount > expectedBeforeRetry - 1)
                return Fail(result, "S_MapTransition arrived with killCount=0 — BossRoom gate not working");

            if (!gotLocked)
                return Fail(result, "S_PortalLocked not received within 2s — gate did not fire");

            result.SawPortalLocked = true;
            result.RequiredCount = bot.LastLockedRequiredCount;
            result.CurrentCount = bot.LastLockedCurrentCount;

            if (result.RequiredCount != 20)
                return Fail(result, $"S_PortalLocked.requiredCount expected=20, actual={result.RequiredCount}");
            if (result.CurrentCount != 0)
                return Fail(result, $"S_PortalLocked.currentCount expected=0, actual={result.CurrentCount}");

            if (seedBossGate == null)
            {
                result.Success = true;
                return result;
            }

            await seedBossGate(bot.LocalEntityId);

            bot.ResetPortalLocked();
            int expectedTransition = bot.NextExpectedTransitionCount();
            bot.SendEnterPortal(HGPortalId);

            S_MapTransition? t2 = await bot.WaitForMapTransition(expectedTransition, TransitionTimeout, ct);
            if (t2 == null)
                return Fail(result, $"S_MapTransition timeout (5s) — HG→BossRoom after seed (killCount=20)");

            if (Math.Abs(t2.spawnX - BossRoomDestSpawnX) > SpawnXTolerance)
                return Fail(result, $"BossRoom destSpawnX 불일치: expected={BossRoomDestSpawnX}, actual={t2.spawnX}");

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

    sealed class GateProbe : ProbeBase
    {
        volatile int _mapTransitionCount = 0;
        S_MapTransition? _lastTransition;
        volatile bool _gotPortalLocked = false;
        int _lastLockedRequired;
        int _lastLockedCurrent;

        public int TransitionCount => _mapTransitionCount;
        public int LastLockedRequiredCount => _lastLockedRequired;
        public int LastLockedCurrentCount => _lastLockedCurrent;

        public int NextExpectedTransitionCount() => _mapTransitionCount + 1;

        public void ResetPortalLocked()
        {
            lock (Gate) _gotPortalLocked = false;
        }

        public async Task MoveToPortal(float portalX, CancellationToken ct)
            => await MoveToPortalCore(portalX, ct);

        public void SendEnterPortal(int portalId) => SendEnterPortalCore(portalId);

        public async Task<bool> WaitForPortalLocked(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _gotPortalLocked, timeout, ct);

        public async Task<S_MapTransition?> WaitForMapTransition(int expectedCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => _mapTransitionCount >= expectedCount, timeout, ct);
            if (!ok) return null;
            lock (Gate) return _lastTransition;
        }

        protected override void OnMapTransition(S_MapTransition packet)
        {
            lock (Gate)
            {
                _lastTransition = packet;
                _mapTransitionCount++;
            }
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            if (id == PacketID.S_PortalLocked)
            {
                S_PortalLocked locked = new();
                locked.Read(buffer);
                lock (Gate)
                {
                    _lastLockedRequired = locked.requiredCount;
                    _lastLockedCurrent = locked.currentCount;
                    _gotPortalLocked = true;
                }
            }
        }
    }
}
