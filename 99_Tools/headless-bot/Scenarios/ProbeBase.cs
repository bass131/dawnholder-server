using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
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
            sessionFactory: () =>
            {
                BotSession s = new();
                s.OnConnectedCallback = _ =>
                {
                    _connected.Set();
                    C_Handshake handshake = new() { clientVersion = ProtocolVersion.Current };
                    s.Send(handshake.Write());
                };
                s.OnDisconnectedCallback = _ => _disconnected.Set();
                s.OnPacketCallback = HandlePacket;
                Session = s;
                return s;
            });
    }

    public bool WaitConnected(TimeSpan timeout) => _connected.Wait(timeout);
    public bool WaitHandshake(TimeSpan timeout) => _handshake.Wait(timeout);
    public bool WaitLogin(TimeSpan timeout) => _login.Wait(timeout);
    public bool WaitDisconnected(TimeSpan timeout) => _disconnected.Wait(timeout);
    public bool WaitEnterMap(TimeSpan timeout) => _enterMap.Wait(timeout);

    public void Disconnect() => Session?.Disconnect();

    protected void SendMove(sbyte inputX)
    {
        ClientTick++;
        C_MoveIntent move = new()
        {
            input = InputBits.Encode(inputX, jumpPressed: false),
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
    protected async Task MoveToPortalCore(float portalX, CancellationToken ct,
        int maxTicks = 400, float reachRadius = 0.5f)
    {
        int ticks = 0;
        while (true)
        {
            float sx = ServerX;
            if (Math.Abs(sx - portalX) <= reachRadius)
                break;

            if (ticks >= maxTicks)
                throw new TimeoutException(
                    $"MoveToPortal: {maxTicks}틱 내 포털 미도달. portalX={portalX}, serverX={sx}");

            sbyte dir = sx < portalX ? (sbyte)1 : (sbyte)-1;
            SendMove(dir);
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
                    Session?.Send(new C_Login { accountName = AccountName }.Write());
                }
                _handshake.Set();
                break;

            case PacketID.S_LoginResult:
                S_LoginResult login = new();
                login.Read(buffer);
                LoginResult = (LoginResult)login.result;
                if (LoginResult == Shared.GameData.LoginResult.Accepted)
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
    protected virtual void OnEnterMap(S_EnterMap packet) { }
    protected virtual void OnMapTransition(S_MapTransition packet) { }

    // S_Snapshot은 자기 자신의 것만 호출 (entityId 필터 후).
    protected virtual void OnSnapshot(S_Snapshot packet) { }

    // S_HandshakeResult / S_LoginResult / S_EnterMap / S_MapTransition / S_Snapshot 외 모든 패킷.
    protected virtual void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer) { }
}
