using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.7 허공 스윙 negative 검증 스모크.
//
// 검증 목표: 타겟 없이 공격(targetEntityId=0)해도 S_HitResult가 발생하지 않는다.
// Town(enemy 없는 빈 맵)에서 허공 스윙을 3~5회 발사하고
// S_HitResult 수신 건수가 0이면 Success.
//
// 의미: "스윙 처리는 하되 데미지(S_HitResult)는 발생하지 않는다"를 negative로 검증.
// S_PlayerAttack broadcast 검증은 RemoteAttackSmoke가 담당하므로 여기선 데미지 0만 본다.
public class WhiffSwingSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan CooldownWait = TimeSpan.FromMilliseconds(550);
    static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(600);

    const int AttackCount = 4;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int AttacksSent;
        public int HitResultCount;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        WhiffProbe bot = new();

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

            // serverTick 확보 — 0이면 서버가 공격을 silent drop한다.
            bool gotSnapshot = await bot.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가");

            // 허공 스윙 AttackCount회 송신. 쿨다운 간격(550ms)을 지켜야 rate-limit에 걸리지 않는다.
            for (int i = 0; i < AttackCount; i++)
            {
                bot.SendAttack(targetEntityId: 0);
                result.AttacksSent++;
                if (i < AttackCount - 1)
                    await Task.Delay(CooldownWait, ct);
            }

            // QuietWindow 대기 후 S_HitResult 수신 여부 확정.
            await Task.Delay(QuietWindow, ct);

            result.HitResultCount = bot.HitResultCount;

            // 허공 스윙은 데미지를 발생시키지 않아야 한다.
            if (result.HitResultCount > 0)
                return Fail(result, $"whiff swing produced S_HitResult — expected 0, got {result.HitResultCount}");

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

    sealed class WhiffProbe : ProbeBase
    {
        int _hitResultCount;

        public int HitResultCount { get { lock (Gate) return _hitResultCount; } }

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

        // attackerClientTick에 최신 serverTick을 박아야 서버 rewind 범위 검증을 통과한다.
        // 0이면 silent drop되므로 WaitForFirstSnapshot 이후에만 호출한다.
        public void SendAttack(int targetEntityId)
        {
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = LastReceivedServerTick,
            };
            Session?.Send(attack.Write());
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            if (id == PacketID.S_HitResult)
            {
                // 허공 스윙 시 이 패킷이 수신되면 안 된다.
                lock (Gate) _hitResultCount++;
            }
        }
    }
}
