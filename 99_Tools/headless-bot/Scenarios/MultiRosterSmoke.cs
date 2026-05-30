using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
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

        // Server join/roster jobs can arrive in the same tick batch as EnterMap.
        await Task.Delay(50, ct);
        return null;
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class BotProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly List<int> _joins = new();
        readonly List<int> _leaves = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        BotSession? _session;

        public string Name { get; }
        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int EntityId { get; private set; } = -1;

        public int JoinCount { get { lock (_gate) return _joins.Count; } }
        public int LeaveCount { get { lock (_gate) return _leaves.Count; } }

        public BotProbe(string name) => Name = name;

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

        public async Task<bool> WaitForJoin(int entityId, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => HasJoin(entityId), timeout, ct);

        public async Task<bool> WaitForLeave(int entityId, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => HasLeave(entityId), timeout, ct);

        public bool HasJoin(int entityId)
        {
            lock (_gate) return _joins.Contains(entityId);
        }

        bool HasLeave(int entityId)
        {
            lock (_gate) return _leaves.Contains(entityId);
        }

        public void Disconnect() => _session?.Disconnect();

        void HandlePacket(ArraySegment<byte> buffer)
        {
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
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    EntityId = enterMap.entityId;
                    _enterMap.Set();
                    break;

                case PacketID.S_PlayerJoin:
                    S_PlayerJoin join = new();
                    join.Read(buffer);
                    lock (_gate) _joins.Add(join.entityId);
                    break;

                case PacketID.S_PlayerLeave:
                    S_PlayerLeave leave = new();
                    leave.Read(buffer);
                    lock (_gate) _leaves.Add(leave.entityId);
                    break;
            }
        }

        static async Task<bool> WaitUntil(
            Func<bool> predicate, TimeSpan timeout, CancellationToken ct)
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
