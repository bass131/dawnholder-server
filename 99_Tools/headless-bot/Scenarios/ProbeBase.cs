using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// 공통 핸드셰이크 + 연결 인프라 베이스.
//
// 모든 Probe가 반복 구현하던 다음 4종을 집중 관리한다:
//   1. Connector + _connected/_handshake/_login/_disconnected/_enterMap ManualResetEventSlim
//   2. Connect() — OnConnected 시 C_Handshake 자동 송신
//   3. S_HandshakeResult → C_Login → Accepted 뒤 C_CharacterSelect 송신
//   4. S_EnterMap 처리 — LocalEntityId + SpawnX + _serverX 초기화
//   5. WaitUntil 헬퍼 (25ms 폴링)
//
// 각 Probe는 ProbeBase를 sealed로 상속하고 추가 필드와 HandleExtraPacket을 override한다.
// NS: 동일 어셈블리이므로 폴더 이동과 무관하게 참조 정상.
abstract class ProbeBase
{
    static readonly string RunName = Guid.NewGuid().ToString("N").Substring(0, 4);
    static int _nextAccount;
    protected readonly object Gate = new();
    readonly string _accountName = CreateAccountName();
    readonly Connector _connector = new();
    readonly ManualResetEventSlim _connected = new(false);
    readonly ManualResetEventSlim _handshake = new(false);
    readonly ManualResetEventSlim _login = new(false);
    readonly ManualResetEventSlim _disconnected = new(false);
    readonly ManualResetEventSlim _enterMap = new(false);
    bool _closed;

    protected BotSession? Session;
    protected uint ClientTick;

    // HandlePacket에서 갱신, scenario thread에서 읽음 (volatile).
    protected volatile float ServerX = 0f;
    protected volatile int LastReceivedServerTick = 0;

    public bool HandshakeOk { get; private set; }
    public string HandshakeReason { get; private set; } = "";
    public LoginResult? LoginResult { get; private set; }
    public int LocalEntityId { get; private set; } = -1;
    public float SpawnX { get; protected set; }

    // 서브클래스가 캐릭터 클래스를 지정. 기본값 Knight.
    protected virtual CharacterClass SelectedClass => CharacterClass.Knight;
    protected virtual string AccountName => _accountName;
    protected virtual bool SelectClassAfterLogin => true;
    protected bool IsDisconnected => _disconnected.IsSet;

    internal static string CreateAccountName()
    {
        string name = $"bot_{RunName}_{Interlocked.Increment(ref _nextAccount)}";
        if (!AccountNameRule.TryNormalize(name, out string normalized))
            throw new InvalidOperationException("Bot account sequence exceeded the name limit.");
        return normalized;
    }

    public void Connect(string host, int port)
    {
        _connector.Connect(
            new IPEndPoint(IPAddress.Parse(host), port),
            sessionFactory: CreateSession);
    }

    public bool WaitConnected(TimeSpan timeout) => _connected.Wait(timeout);
    public bool WaitHandshake(TimeSpan timeout) => _handshake.Wait(timeout);
    public bool WaitLogin(TimeSpan timeout) => _login.Wait(timeout);
    public bool WaitDisconnected(TimeSpan timeout) => _disconnected.Wait(timeout);
    public bool WaitEnterMap(TimeSpan timeout) => _enterMap.Wait(timeout);

    public void Disconnect()
    {
        BotSession? session;
        lock (Gate)
        {
            _closed = true;
            session = Session;
        }
        session?.Disconnect();
    }

    protected async Task<bool> ConnectWithCancellationAsync(string host, int port, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using IConnectionAttempt attempt = _connector.BeginConnect(new IPEndPoint(IPAddress.Parse(host), port));
        using CancellationTokenRegistration cancel = timeout.Token.Register(attempt.Cancel);
        var outcome = await attempt.Completion;
        ct.ThrowIfCancellationRequested();
        if (outcome.Status != ConnectionStatus.Success || !attempt.TryTakeConnectedSocket(out Socket? socket))
            return false;
        try
        {
            EndPoint endPoint = socket!.RemoteEndPoint!;
            BotSession session = CreateSession();
            session.Start(socket);
            socket = null; // Start owns the socket, including a session closed before activation.
            if (!session.IsDisconnected) session.OnConnected(endPoint);
            return !session.IsDisconnected;
        }
        finally
        {
            socket?.Dispose();
        }
    }

    protected void SendMove(sbyte inputX, bool jump = false)
    {
        ClientTick++;
        C_MoveIntent move = new()
        {
            input = InputBits.Encode(inputX, jumpPressed: jump),
            clientTick = ClientTick,
        };
        Session?.Send(move.Write());
    }

    protected void SendEnterPortalCore(int portalId)
    {
        C_EnterPortal packet = new() { portalId = portalId };
        Session?.Send(packet.Write());
    }

    protected void SendCheatCompleteQuestCore()
    {
        C_CheatCommand cheat = new() { cheatType = 0 };
        Session?.Send(cheat.Write());
    }

    // 서버 권위 X 기반 포털 이동. 매 틱 방향 재계산 — hitstun/넉백 후에도 수렴.
    // 1칸 단(사냥터 봉인 제단, 보스방 문턱·출구 단)에 막혀 X가 멈추면 한 틱 점프를 섞어 넘는다.
    protected async Task MoveToPortalCore(float portalX, CancellationToken ct,
        int maxTicks = 400, float reachRadius = 0.5f)
    {
        const int StallTicksBeforeJump = 3;
        int ticks = 0;
        int stalledTicks = 0;
        float previousX = ServerX;
        while (true)
        {
            float sx = ServerX;
            if (Math.Abs(sx - portalX) <= reachRadius)
                break;

            if (ticks >= maxTicks)
                throw new TimeoutException(
                    $"MoveToPortal: {maxTicks}틱 내 포털 미도달. portalX={portalX}, serverX={sx}");

            stalledTicks = Math.Abs(sx - previousX) < 0.01f ? stalledTicks + 1 : 0;
            previousX = sx;
            bool jump = stalledTicks >= StallTicksBeforeJump;
            if (jump)
                stalledTicks = 0;

            sbyte dir = sx < portalX ? (sbyte)1 : (sbyte)-1;
            SendMove(dir, jump);
            await Task.Delay(Constants.TickIntervalMs, ct);
            ticks++;
        }
        SendMove(0);
        await Task.Delay(150, ct);
    }

    protected static async Task<bool> WaitUntil(Func<bool> predicate, TimeSpan timeout, CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (predicate()) return true;
            await Task.Delay(25, ct);
        }
        return predicate();
    }

    BotSession CreateSession()
    {
        BotSession session = new();
        session.OnConnectedCallback = _ =>
        {
            lock (Gate)
            {
                if (_closed) { session.Disconnect(); return; }
                _connected.Set();
                session.Send(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            }
        };
        session.OnDisconnectedCallback = _ => _disconnected.Set();
        session.OnPacketCallback = buffer =>
        {
            lock (Gate)
            {
                if (!_closed) HandlePacket(buffer);
            }
        };
        lock (Gate)
        {
            Session = session;
            // A timed-out/cancelled probe must also close a legacy connector that succeeds later.
            if (_closed) session.Disconnect();
        }
        return session;
    }

    void HandlePacket(ArraySegment<byte> buffer)
    {
        if (!ValidatePacket(buffer)) return;
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
                    Session?.Send(new C_Login { accountName = AccountName }.Write());
                }
                _handshake.Set();
                break;

            case PacketID.S_LoginResult:
                S_LoginResult login = new();
                login.Read(buffer);
                LoginResult = (LoginResult)login.result;
                if (LoginResult == Shared.GameData.LoginResult.Accepted && SelectClassAfterLogin)
                {
                    Session?.Send(new C_CharacterSelect { characterClass = (byte)SelectedClass }.Write());
                }
                _login.Set();
                break;

            case PacketID.S_EnterMap:
                S_EnterMap enterMap = new();
                enterMap.Read(buffer);
                LocalEntityId = enterMap.entityId;
                SpawnX = enterMap.spawnX;
                ServerX = enterMap.spawnX;
                OnEnterMap(enterMap);
                if (!_enterMap.IsSet) _enterMap.Set();
                break;

            case PacketID.S_MapTransition:
                S_MapTransition mapTransition = new();
                mapTransition.Read(buffer);
                SpawnX = mapTransition.spawnX;
                ServerX = mapTransition.spawnX;
                OnMapTransition(mapTransition);
                break;

            case PacketID.S_Snapshot:
                S_Snapshot snapshot = new();
                snapshot.Read(buffer);
                LastReceivedServerTick = snapshot.serverTick;
                if (snapshot.entityId == LocalEntityId)
                {
                    ServerX = snapshot.x;
                    OnSnapshot(snapshot);
                }
                break;

            default:
                HandleExtraPacket((PacketID)id, buffer);
                break;
        }
    }

    // 서브클래스 확장 포인트 — 기본 구현은 no-op.
    protected virtual bool ValidatePacket(ArraySegment<byte> buffer) => true;
    protected virtual void OnEnterMap(S_EnterMap packet) { }
    protected virtual void OnMapTransition(S_MapTransition packet) { }

    // S_Snapshot은 자기 자신의 것만 호출 (entityId 필터 후).
    protected virtual void OnSnapshot(S_Snapshot packet) { }

    // S_HandshakeResult / S_LoginResult / S_EnterMap / S_MapTransition / S_Snapshot 외 모든 패킷.
    protected virtual void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer) { }
}
