using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// One real GameWorld driven tick by tick, with a CharacterAdmissionServices attached before any session exists
/// (api-contract v2: Host calls only AttachPersistence and <c>new GameSession(world)</c> consumes the attached
/// services; contract-supplement-1 approves this seam). The store is the recording repository and time is the
/// controllable clock. The persisted account is alice_01; its AccountId is the independent Python UUIDv5 vector
/// pinned in AccountIdResolverTests, not a product computation. Store answers are scripted "what the store reported"
/// values: probe fence 7, acquired fence 8 with sequence 0, released fence 9, tokens 1 and 2, and the stored class
/// each test chooses.
/// </summary>
sealed class AdmissionTestWorld : IDisposable
{
    public const string PersistedName = "alice_01";
    public const string OtherName = "user_123";
    public static readonly Guid PersistedAccountId = new("d0064115-982b-56a6-ae70-528b3cde4a09");
    public static readonly Guid OtherAccountId = new("18be3028-ade2-5ad0-9ffa-0fd51a36f461");
    public static readonly PersistenceIdentity PersistedIdentity = new(1, PersistedAccountId, CharacterId);

    // api-contract v2 「패킷과 공개 결과」: S_CharacterLoadResult is ID 48, six bytes, result then characterClass.
    public const ushort LoadResultId = 48;
    public const byte Success = 0;
    public const byte Busy = 1;
    public const byte RecoveryRequired = 2;
    public const byte Unavailable = 3;
    public const byte NoClass = 255;

    // Literal content values, the same ones CharacterSelectHandlerTests reads from the select log.
    public const int KnightMaxHp = 150;
    public const int MageMaxHp = 80;

    public const long ProbedFence = 7;
    public const long AcquiredFence = 8;
    public const long ReleasedFence = 9;

    internal static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    [ThreadStatic]
    static bool _insideTick;

    long _tick;
    int _takenCalls;

    public AdmissionTestWorld(bool attachPersistence = true)
    {
        World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
        if (!attachPersistence)
            return;
        try
        {
            object selector = RuntimeEntryApi.NewSelector(PersistedName, PersistedAccountId);
            Services = RuntimeEntryApi.NewServices(selector, PersistedIdentity, Repository, Clock);
            RuntimeEntryApi.AttachPersistence(World, Services);
        }
        catch
        {
            World.Stop();
            throw;
        }
    }

    public GameWorld World { get; }

    public object? Services { get; }

    public Repo Repository { get; } = new();

    public ControllableTimeProvider Clock { get; } = new();

    public IEnumerable<PlayerEntity> Players => World.AllLiveMaps.SelectMany(map => map.Players);

    internal static bool InsideTick => _insideTick;

    public void Dispose() => World.Stop();

    public PlayerEntity? PlayerOf(GameSession session)
        => Players.FirstOrDefault(player => ReferenceEquals(player.Owner, session));

    /// <summary>
    /// Runs one world tick on a pool thread through the owner harness's ReturnsPromptly, so a tick that waits for
    /// store I/O fails at the repository fake's existing limit instead of hanging. Sends made inside are marked.
    /// </summary>
    public Task TickAsync() => CharacterPersistenceOwnerHarness.ReturnsPromptly(() =>
    {
        _insideTick = true;
        try
        {
            LifecycleTestWorld.Tick(World, Interlocked.Increment(ref _tick));
        }
        finally
        {
            _insideTick = false;
        }
        return true;
    });

    public async Task TicksAsync(int count)
    {
        for (int i = 0; i < count; i++)
            await TickAsync();
    }

    /// <summary>Ticks until <paramref name="reached"/> holds. The bound is the fake's existing limit, not a contract value.</summary>
    public async Task TickUntilAsync(Func<bool> reached, string what)
    {
        Stopwatch waited = Stopwatch.StartNew();
        while (!reached())
        {
            Assert.True(waited.Elapsed < Repo.Limit, $"{what} was not observed within {Repo.Limit}");
            await TickAsync();
        }
    }

    // Real handshake bytes; the version is whatever both ends currently share.
    public EntrySession Connect()
    {
        EntrySession session = new(World);
        session.OnConnected(Endpoint);
        session.Receive(new C_Handshake { clientVersion = ProtocolVersion.Current });
        return session;
    }

    public async Task<EntrySession> LogInAsync(string accountName)
    {
        EntrySession session = Connect();
        session.Receive(new C_Login { accountName = accountName });
        await TickAsync();
        Assert.True(session.IsLoggedIn, $"fixture: {accountName} was not logged in");
        return session;
    }

    public async Task<EntrySession> EnterAsync(string accountName, CharacterClass menuClass)
    {
        EntrySession session = await LogInAsync(accountName);
        session.Receive(new C_CharacterSelect { characterClass = (byte)menuClass });
        await TickUntilAsync(() => PlayerOf(session) is not null, $"fixture: {accountName} entering the world");
        return session;
    }

    /// <summary>
    /// The persisted account logs in and selects <paramref name="menuClass"/>; the probe is answered Free at fence 7
    /// and the acquire that follows is returned still pending, so the session is in Loading.
    /// </summary>
    public async Task<(EntrySession Session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> Acquire)> LoadingAsync(CharacterClass menuClass)
    {
        EntrySession session = await LogInAsync(PersistedName);
        session.Receive(new C_CharacterSelect { characterClass = (byte)menuClass });
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await NextReadAdmissionAsync();
        Assert.Equal(PersistedIdentity, probe.Request.Identity);
        probe.Complete(FreeProbe());
        return (session, await NextAcquireAsync());
    }

    /// <summary>Ticks until a store call the test has not taken yet arrives, then takes it.</summary>
    public async Task<object> NextCallAsync(string what)
    {
        int expected = _takenCalls + 1;
        await TickUntilAsync(() => Repository.Calls.Count >= expected, what);
        _takenCalls = expected;
        return await Repository.NextAnyAsync();
    }

    public async Task<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>> NextReadAdmissionAsync()
        => Assert.IsType<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>>(await NextCallAsync("ReadAdmission call"));

    public async Task<Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult>> NextAcquireAsync()
        => Assert.IsType<Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult>>(await NextCallAsync("AcquireAndLoad call"));

    public async Task<Repo.PendingCall<ReleaseRequest, ReleaseResult>> NextReleaseAsync()
        => Assert.IsType<Repo.PendingCall<ReleaseRequest, ReleaseResult>>(await NextCallAsync("Release call"));

    public async Task<Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult>> NextResolveAsync()
        => Assert.IsType<Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult>>(await NextCallAsync("ResolveOperation call"));

    public static PersistenceAuthority FreeAuthority(long fence)
        => new(PersistedIdentity, PersistenceOwnerKind.Free, null, fence, 0);

    public static PersistenceAuthority HeldAuthority(PersistenceOwnerKind kind, Guid ownerId, long fence)
        => new(PersistedIdentity, kind, ownerId, fence, 0);

    public static ReadAdmissionResult FreeProbe()
        => ReadAdmissionResult.Probe(new AdmissionProbe(FreeAuthority(ProbedFence), schemaCompatible: true, bindingMatches: true));

    public static ReadAdmissionResult HeldProbe(PersistenceOwnerKind kind, Guid ownerId)
        => ReadAdmissionResult.Probe(new AdmissionProbe(HeldAuthority(kind, ownerId, ProbedFence), schemaCompatible: true, bindingMatches: true));

    /// <summary>The store's Acquired answer to <paramref name="request"/>: its owner holds fence 8 at sequence 0.</summary>
    public static AcquireAndLoadResult Acquired(AcquireAndLoadRequest request, CharacterClass storedClass, int storedMaxHp)
    {
        PersistenceAuthority held = HeldAuthority(PersistenceOwnerKind.Runtime, request.Operation.OwnerId, AcquiredFence);
        OperationReceipt receipt = new(
            request.Operation.OperationId, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired,
            held, storedClass, new SafeCheckpoint(storedClass, 3.5f, 0f, storedMaxHp), Token(1), Token(2));
        return AcquireAndLoadResult.Acquired(receipt, Observed(held, Token(1), Token(2)));
    }

    /// <summary>The store's conditional release answer: authority becomes Free at fence 9.</summary>
    public static ReleaseResult Released(ReleaseRequest request)
    {
        PersistenceAuthority free = FreeAuthority(ReleasedFence);
        OperationReceipt receipt = new(
            request.Operation.OperationId, PersistenceOperationKind.ReleaseRuntime, PersistenceResultCode.Released,
            free, null, null, null, null);
        return ReleaseResult.Released(receipt, Observed(free, Token(1), Token(2)));
    }

    /// <summary>The single S_CharacterLoadResult sent so far, decoded from raw bytes [6,0][48,0][result][class].</summary>
    public static (byte Result, byte LoadedClass) SingleLoadResult(EntrySession session)
    {
        SentPacket packet = Assert.Single(session.Sent(), sent => sent.Id == LoadResultId);
        Assert.Equal(6, packet.Bytes.Length);
        Assert.Equal(6, BinaryPrimitives.ReadUInt16LittleEndian(packet.Bytes.AsSpan(0, 2)));
        return (packet.Bytes[4], packet.Bytes[5]);
    }

    public static int FirstIndexOf(EntrySession session, ushort id)
        => Array.FindIndex(session.Sent(), sent => sent.Id == id);
}

/// <summary>Records every packet the server sends to this connection and whether the send ran inside a world tick.</summary>
sealed class EntrySession : GameSession
{
    readonly List<SentPacket> _sent = new();

    internal EntrySession(GameWorld world)
        : base(world)
    {
    }

    public override void Send(ArraySegment<byte> packet)
    {
        // The tick, the store continuation and the test thread may all send.
        lock (_sent)
            _sent.Add(new SentPacket(packet.ToArray(), AdmissionTestWorld.InsideTick));
    }

    // Normal and abrupt transport closes both reach the session through OnDisconnected.
    public override void Disconnect() => OnDisconnected(AdmissionTestWorld.Endpoint);

    internal void Receive(IPacket packet) => OnRecvPacket(packet.Write());

    internal int Mark
    {
        get
        {
            lock (_sent)
                return _sent.Count;
        }
    }

    internal SentPacket[] Sent(int fromMark = 0)
    {
        lock (_sent)
            return _sent.Skip(fromMark).ToArray();
    }

    internal ushort[] IdsSince(int mark) => Sent(mark).Select(sent => sent.Id).ToArray();
}

readonly record struct SentPacket(byte[] Bytes, bool InsideTick)
{
    public ushort Id => BinaryPrimitives.ReadUInt16LittleEndian(Bytes.AsSpan(2, 2));
}
