using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
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

    sealed class WhiffProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);

        int _hitResultCount;
        volatile int _lastReceivedServerTick = 0;

        BotSession? _session;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;

        public int HitResultCount { get { lock (_gate) return _hitResultCount; } }

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
        public bool WaitEnterMap(TimeSpan timeout)  => _enterMap.Wait(timeout);

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        // attackerClientTick에 최신 serverTick을 박아야 서버 rewind 범위 검증을 통과한다.
        // 0이면 silent drop되므로 WaitForFirstSnapshot 이후에만 호출한다.
        public void SendAttack(int targetEntityId)
        {
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = _lastReceivedServerTick,
            };
            _session?.Send(attack.Write());
        }

        public void Disconnect() => _session?.Disconnect();

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
                    _enterMap.Set();
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    _lastReceivedServerTick = snapshot.serverTick;
                    break;

                case PacketID.S_HitResult:
                    // 허공 스윙 시 이 패킷이 수신되면 안 된다.
                    lock (_gate) _hitResultCount++;
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
