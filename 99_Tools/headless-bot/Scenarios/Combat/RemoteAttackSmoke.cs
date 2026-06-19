using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.7 공격 broadcast 회귀 스모크 (2봇).
//
// 검증 목표: S_PlayerAttack(22) broadcast 규칙
//   - botB는 botA의 공격 이벤트를 1건 이상 수신한다 (서버가 공격자 외 전원에게 broadcast).
//   - botA는 자기 자신의 공격 이벤트를 0건 수신한다 (except: attacker.Owner 규칙).
//
// 두 봇 모두 Town(같은 맵)에 머물러 같은 맵 broadcast 범위 안에 있도록 한다.
// Town은 enemy가 없으므로 허공 스윙(targetEntityId=0)으로 공격을 발생시킨다.
public class RemoteAttackSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(600);

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int BotAEntityId;
        public int BotBEntityId;
        public int BReceivedCount;
        public int AReceivedCount;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();

        AttackProbe botA = new("botA");
        AttackProbe botB = new("botB");

        try
        {
            // 두 봇을 순차 연결 — 같은 Town 맵에 진입한다.
            string? failureA = await ConnectAndEnter(botA, host, port, ct);
            if (failureA != null) return Fail(result, failureA);

            string? failureB = await ConnectAndEnter(botB, host, port, ct);
            if (failureB != null) return Fail(result, failureB);

            result.BotAEntityId = botA.LocalEntityId;
            result.BotBEntityId = botB.LocalEntityId;

            // botA의 serverTick 확보 — C_Attack.attackerClientTick에 박아야
            // 서버 rewind 범위 검증(diff ≤ 4)을 통과한다. 0이면 silent drop된다.
            bool gotSnapshot = await botA.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "botA: S_Snapshot timeout — serverTick 추적 불가");

            // botA가 허공 스윙을 1회 발사한다 (targetEntityId=0, Town은 enemy 없음).
            botA.SendAttack(targetEntityId: 0);

            // QuietWindow 대기 후 수신 카운트 확정.
            await Task.Delay(QuietWindow, ct);

            result.BReceivedCount = botB.AttackCountFrom(botA.LocalEntityId);
            result.AReceivedCount = botA.AttackCountFrom(botA.LocalEntityId);

            // botB는 botA 공격 이벤트를 1건 이상 수신해야 한다.
            if (result.BReceivedCount < 1)
                return Fail(result, $"botB did not receive S_PlayerAttack from botA (count={result.BReceivedCount})");

            // botA는 자기 자신의 공격 이벤트를 수신하면 안 된다.
            if (result.AReceivedCount > 0)
                return Fail(result, $"botA received own S_PlayerAttack — except rule broken (count={result.AReceivedCount})");

            result.Success = true;
            return result;
        }
        finally
        {
            botA.Disconnect();
            botB.Disconnect();
        }
    }

    static async Task<string?> ConnectAndEnter(
        AttackProbe bot, string host, int port, CancellationToken ct)
    {
        bot.Connect(host, port);

        if (!bot.WaitConnected(DefaultTimeout))
            return $"{bot.Name}: connect timeout";

        if (!bot.WaitHandshake(DefaultTimeout))
            return $"{bot.Name}: S_HandshakeResult timeout";

        if (!bot.HandshakeOk)
            return $"{bot.Name}: handshake rejected: {bot.HandshakeReason}";

        if (!bot.WaitEnterMap(DefaultTimeout))
            return $"{bot.Name}: S_EnterMap timeout";

        // 진입 직후 roster/spawn 패킷 처리 대기.
        await Task.Delay(50, ct);
        return null;
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class AttackProbe : ProbeBase
    {
        // S_PlayerAttack 수신 목록 — attackerEntityId별로 카운트한다.
        readonly List<int> _receivedAttackerIds = new();

        public string Name { get; }

        public AttackProbe(string name) => Name = name;

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

        // attackerClientTick에 최신 serverTick을 박아야 서버 rewind 범위 검증을 통과한다.
        public void SendAttack(int targetEntityId)
        {
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = LastReceivedServerTick,
            };
            Session?.Send(attack.Write());
        }

        // 특정 attackerEntityId로부터 수신한 S_PlayerAttack 건수.
        public int AttackCountFrom(int attackerEntityId)
        {
            lock (Gate)
                return _receivedAttackerIds.Count(id => id == attackerEntityId);
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            if (id == PacketID.S_PlayerAttack)
            {
                // 서버가 공격자 외 전원에게 broadcast하는 이벤트.
                // attackerEntityId를 기록해 검증에 활용한다.
                S_PlayerAttack playerAttack = new();
                playerAttack.Read(buffer);
                lock (Gate)
                    _receivedAttackerIds.Add(playerAttack.attackerEntityId);
            }
        }
    }
}
