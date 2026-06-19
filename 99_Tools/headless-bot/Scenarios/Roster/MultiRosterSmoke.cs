using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// 멀티 접속 골격(roster join/leave) 회귀 시나리오.
//
// 사용 패킷: C_Handshake, S_HandshakeResult, S_EnterMap, S_PlayerJoin, S_PlayerLeave.
public class MultiRosterSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int FirstEntityId;
        public int SecondEntityId;
        public int ReconnectEntityId;
        public int FirstJoinCount;
        public int SecondJoinCount;
        public int SecondLeaveCount;
        public int ReconnectRosterCount;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();

        BotProbe first = new("first");
        BotProbe second = new("second");
        BotProbe reconnect = new("reconnect");

        try
        {
            string? failure = await ConnectAndEnter(first, host, port, ct);
            if (failure != null) return Fail(result, failure);

            failure = await ConnectAndEnter(second, host, port, ct);
            if (failure != null) return Fail(result, failure);

            if (!await first.WaitForJoin(second.EntityId, DefaultTimeout, ct))
                return Fail(result, $"first bot did not receive S_PlayerJoin for second entity {second.EntityId}");

            if (!await second.WaitForJoin(first.EntityId, DefaultTimeout, ct))
                return Fail(result, $"second bot did not receive initial roster entry for first entity {first.EntityId}");

            first.Disconnect();
            if (!await second.WaitForLeave(first.EntityId, DefaultTimeout, ct))
                return Fail(result, $"second bot did not receive S_PlayerLeave for first entity {first.EntityId}");

            failure = await ConnectAndEnter(reconnect, host, port, ct);
            if (failure != null) return Fail(result, failure);

            if (!await second.WaitForJoin(reconnect.EntityId, DefaultTimeout, ct))
                return Fail(result, $"second bot did not receive S_PlayerJoin for reconnect entity {reconnect.EntityId}");

            if (!await reconnect.WaitForJoin(second.EntityId, DefaultTimeout, ct))
                return Fail(result, $"reconnect bot did not receive initial roster entry for second entity {second.EntityId}");

            if (reconnect.HasJoin(first.EntityId))
                return Fail(result, $"reconnect bot received stale roster entry for disconnected entity {first.EntityId}");

            result.FirstEntityId = first.EntityId;
            result.SecondEntityId = second.EntityId;
            result.ReconnectEntityId = reconnect.EntityId;
            result.FirstJoinCount = first.JoinCount;
            result.SecondJoinCount = second.JoinCount;
            result.SecondLeaveCount = second.LeaveCount;
            result.ReconnectRosterCount = reconnect.JoinCount;
            result.Success = true;
            return result;
        }
        finally
        {
            first.Disconnect();
            second.Disconnect();
            reconnect.Disconnect();
        }
    }

    static async Task<string?> ConnectAndEnter(
        BotProbe bot, string host, int port, CancellationToken ct)
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

        await Task.Delay(50, ct);
        return null;
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class BotProbe : ProbeBase
    {
        readonly List<int> _joins = new();
        readonly List<int> _leaves = new();

        public string Name { get; }
        public int EntityId => LocalEntityId;

        public int JoinCount { get { lock (Gate) return _joins.Count; } }
        public int LeaveCount { get { lock (Gate) return _leaves.Count; } }

        public BotProbe(string name) => Name = name;

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            switch (id)
            {
                case PacketID.S_PlayerJoin:
                    S_PlayerJoin join = new();
                    join.Read(buffer);
                    lock (Gate) _joins.Add(join.entityId);
                    break;

                case PacketID.S_PlayerLeave:
                    S_PlayerLeave leave = new();
                    leave.Read(buffer);
                    lock (Gate) _leaves.Add(leave.entityId);
                    break;
            }
        }

        public async Task<bool> WaitForJoin(int entityId, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => HasJoin(entityId), timeout, ct);

        public async Task<bool> WaitForLeave(int entityId, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => HasLeave(entityId), timeout, ct);

        public bool HasJoin(int entityId)
        {
            lock (Gate) return _joins.Contains(entityId);
        }

        bool HasLeave(int entityId)
        {
            lock (Gate) return _leaves.Contains(entityId);
        }
    }
}
