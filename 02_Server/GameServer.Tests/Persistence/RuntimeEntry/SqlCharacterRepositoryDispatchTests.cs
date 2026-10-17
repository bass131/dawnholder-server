using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Independent verification of the SQL repository's dispatch boundary (technical-spec §6 「NotDispatched」: the call was
/// never sent to the DB; §7: a local pre-dispatch failure is not an unknown, and nothing is forced through without
/// budget). A request for another binding, an exhausted budget, a cancelled caller and a connection that cannot even be
/// created all end before any command, so mutations report NotDispatched and the resolver claims nothing (StillUnknown).
/// The connection factory below never returns a connection; tests count how often the repository asked for one.
/// </summary>
public sealed class SqlCharacterRepositoryDispatchTests
{
    static readonly PersistenceOptions Options = new(
        AdmissionTestWorld.PersistedName, "DawnholderRuntimeProbe", "tcp:127.0.0.1,14330", 1,
        AdmissionTestWorld.PersistedAccountId, CharacterId);

    sealed class RefusingConnectionSource
    {
        int _requests;

        public int Requests => Volatile.Read(ref _requests);

        public SqlConnection Create()
        {
            Interlocked.Increment(ref _requests);
            throw new InvalidOperationException("Verification tests never create a SQL connection.");
        }
    }

    sealed record Outcomes(
        ReadAdmissionStatus Read,
        AcquireAndLoadStatus Acquire,
        WriteSafeCheckpointStatus Checkpoint,
        ReleaseStatus Release,
        OperationResolutionStatus Resolve);

    static PersistenceAuthority Held(PersistenceIdentity identity, long fence, long lastSequence)
        => new(identity, PersistenceOwnerKind.Runtime, RuntimeOwnerId, fence, lastSequence);

    static async Task<Outcomes> CallAllFiveAsync(
        SqlCharacterRepository repository, PersistenceIdentity identity, PersistenceDeadline deadline, CancellationToken token)
    {
        AcquireAndLoadRequest acquire = new(identity, OperationA, RuntimeOwnerId, 7, CharacterClass.Knight, Defaults());
        WriteSafeCheckpointRequest checkpoint = new(Held(identity, 8, 0), 1, OperationB, Token(1), Token(2), KnightSafe());
        ReleaseRequest release = new(Held(identity, 8, 1), 2, OperationB);
        ResolveOperationRequest resolve = new(acquire.Operation, ResolveMode.QueryOnly);

        return new Outcomes(
            (await repository.ReadAdmissionAsync(new ReadAdmissionRequest(identity), deadline, token)).Status,
            (await repository.AcquireAndLoadAsync(acquire, deadline, token)).Status,
            (await repository.WriteSafeCheckpointAsync(checkpoint, deadline, token)).Status,
            (await repository.ReleaseAsync(release, deadline, token)).Status,
            (await repository.ResolveOperationAsync(resolve, deadline, token)).Status);
    }

    static readonly Outcomes NeverSent = new(
        ReadAdmissionStatus.NotDispatched,
        AcquireAndLoadStatus.NotDispatched,
        WriteSafeCheckpointStatus.NotDispatched,
        ReleaseStatus.NotDispatched,
        OperationResolutionStatus.StillUnknown);

    [Theory]
    [InlineData("request for another binding")]
    [InlineData("no budget left")]
    [InlineData("caller already cancelled")]
    public async Task RpcThatCannotBeSent_IsNotDispatched_WithoutAskingForAConnection(string reason)
    {
        RefusingConnectionSource connections = new();
        SqlCharacterRepository repository = new(Options, connections.Create);
        ControllableTimeProvider clock = new();
        PersistenceIdentity identity = reason == "request for another binding" ? Identity() : Options.Identity;
        PersistenceDeadline deadline = PersistenceDeadline.FromBudget(
            clock, reason == "no budget left" ? TimeSpan.Zero : TimeSpan.FromSeconds(5));
        CancellationToken token = new(canceled: reason == "caller already cancelled");

        Outcomes outcomes = await Within(CallAllFiveAsync(repository, identity, deadline, token));

        Assert.Equal(NeverSent, outcomes);
        Assert.Equal(0, connections.Requests);
    }

    [Fact]
    public async Task ConnectionThatCannotBeCreated_IsNotDispatched_NeverUnknown()
    {
        RefusingConnectionSource connections = new();
        SqlCharacterRepository repository = new(Options, connections.Create);
        PersistenceDeadline deadline = PersistenceDeadline.FromBudget(new ControllableTimeProvider(), TimeSpan.FromSeconds(5));

        Outcomes outcomes = await Within(CallAllFiveAsync(repository, Options.Identity, deadline, CancellationToken.None));

        Assert.Equal(NeverSent, outcomes);
        Assert.Equal(5, connections.Requests);
    }
}
