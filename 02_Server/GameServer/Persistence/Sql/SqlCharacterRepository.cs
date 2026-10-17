using System.Data;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

/// <summary>runtime의 다섯 RPC만 호출한다. 재시도/resolve 정책과 권위의 소유자는 호출한 owner다.</summary>
public sealed class SqlCharacterRepository : ICharacterRepository
{
    readonly PersistenceOptions _options;
    readonly Func<SqlConnection> _connectionFactory;

    public SqlCharacterRepository(PersistenceOptions options, Func<SqlConnection> connectionFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public Task<ReadAdmissionResult> ReadAdmissionAsync(
        ReadAdmissionRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync("dh.ReadAdmission", request.Identity, deadline, cancellationToken,
            command => SqlRuntimeParameters.Identity(command, request.Identity),
            row => ReadAdmissionResult.Probe(row.Admission()),
            ReadAdmissionResult.NotDispatched, ReadAdmissionResult.ReadFailed);
    }

    public Task<AcquireAndLoadResult> AcquireAndLoadAsync(
        AcquireAndLoadRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationIdentity operation = request.Operation;
        return ExecuteAsync("dh.AcquireAndLoad", operation.Identity, deadline, cancellationToken,
            command => SqlRuntimeParameters.Operation(command, operation, resolving: false),
            row =>
            {
                row.ValidateOperation(operation, resolving: false);
                PersistenceObservation current = row.Current();
                return row.Outcome == 1
                    ? AcquireAndLoadResult.Acquired(row.Receipt(operation), current)
                    : AcquireAndLoadResult.Rejected(row.Code);
            }, AcquireAndLoadResult.NotDispatched, AcquireAndLoadResult.AcquireUnknown);
    }

    public Task<WriteSafeCheckpointResult> WriteSafeCheckpointAsync(
        WriteSafeCheckpointRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationIdentity operation = request.Operation;
        return ExecuteAsync("dh.WriteSafeCheckpoint", operation.Identity, deadline, cancellationToken,
            command => SqlRuntimeParameters.Operation(command, operation, resolving: false),
            row =>
            {
                row.ValidateOperation(operation, resolving: false);
                PersistenceObservation current = row.Current();
                if (row.Outcome == 2)
                    return row.Code == PersistenceResultCode.Conflict
                        ? WriteSafeCheckpointResult.Conflict() : WriteSafeCheckpointResult.Rejected(row.Code);
                OperationReceipt receipt = row.Receipt(operation);
                return row.Code == PersistenceResultCode.CheckpointApplied
                    ? WriteSafeCheckpointResult.Durable(receipt, current)
                    : WriteSafeCheckpointResult.CheckpointNoChange(receipt, current);
            }, WriteSafeCheckpointResult.NotDispatched, WriteSafeCheckpointResult.CommitUnknown);
    }

    public Task<ReleaseResult> ReleaseAsync(
        ReleaseRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationIdentity operation = request.Operation;
        return ExecuteAsync("dh.ReleaseRuntime", operation.Identity, deadline, cancellationToken,
            command => SqlRuntimeParameters.Operation(command, operation, resolving: false),
            row =>
            {
                row.ValidateOperation(operation, resolving: false);
                PersistenceObservation current = row.Current();
                return row.Outcome == 1
                    ? ReleaseResult.Released(row.Receipt(operation), current) : ReleaseResult.Rejected(row.Code);
            }, ReleaseResult.NotDispatched, ReleaseResult.ReleaseUnknown);
    }

    public Task<ResolveOperationResult> ResolveOperationAsync(
        ResolveOperationRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationIdentity operation = request.Operation;
        return ExecuteAsync("dh.ResolveRuntimeOperation", operation.Identity, deadline, cancellationToken,
            command =>
            {
                SqlRuntimeParameters.Operation(command, operation, resolving: true);
                SqlRuntimeParameters.Add(command, "@SealIfAbsent", SqlDbType.Int,
                    request.Mode == ResolveMode.SealIfAbsent ? 1 : 0);
            }, row =>
            {
                row.ValidateOperation(operation, resolving: true);
                PersistenceObservation current = row.Current();
                if (row.Status == "StillUnknown")
                    return ResolveOperationResult.StillUnknown();
                if (row.Outcome == 2)
                    return ResolveOperationResult.NotApplied(row.Code, current);
                OperationReceipt receipt = row.Receipt(operation);
                bool sameOwner = receipt.Authority.OwnerKind == current.Authority.OwnerKind &&
                    receipt.Authority.OwnerId == current.Authority.OwnerId && receipt.Authority.Fence == current.Authority.Fence;
                return sameOwner
                    ? ResolveOperationResult.Applied(receipt, current) : ResolveOperationResult.FenceLost(receipt, current);
            }, ResolveOperationResult.StillUnknown, ResolveOperationResult.StillUnknown);
    }

    async Task<T> ExecuteAsync<T>(
        string procedure, PersistenceIdentity identity, PersistenceDeadline deadline, CancellationToken cancellationToken,
        Action<SqlCommand> parameters, Func<SqlPersistenceResult, T> map, Func<T> notDispatched, Func<T> unknown)
    {
        ArgumentNullException.ThrowIfNull(deadline);
        if (!identity.Equals(_options.Identity) || deadline.IsExpired || cancellationToken.IsCancellationRequested)
            return notDispatched();
        bool dispatched = false;
        try
        {
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellation.CancelAfter(deadline.GetRemaining());
            await using SqlConnection connection = _connectionFactory();
            await connection.OpenAsync(cancellation.Token).ConfigureAwait(false);
            await using SqlCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedure;
            parameters(command);
            TimeSpan remaining = deadline.GetRemaining();
            if (remaining <= TimeSpan.Zero || cancellation.IsCancellationRequested)
                return notDispatched();
            command.CommandTimeout = (int)Math.Clamp(Math.Ceiling(remaining.TotalSeconds), 1, _options.CommandTimeoutSeconds);
            int lockTimeout = (int)Math.Min(Math.Floor(remaining.TotalMilliseconds), _options.LockTimeoutMilliseconds);
            if (lockTimeout < 1)
                return notDispatched();
            SqlRuntimeParameters.Add(command, "@LockTimeoutMs", SqlDbType.Int, lockTimeout);
            cancellation.Token.ThrowIfCancellationRequested();
            // From this boundary even a synchronous provider error/cancel cannot prove that SQL did not run.
            dispatched = true;
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellation.Token).ConfigureAwait(false);
            SqlPersistenceResult row = await SqlPersistenceResult.ReadAsync(reader, _options, cancellation.Token)
                .ConfigureAwait(false);
            return map(row);
        }
        catch (Exception)
        {
            // No raw provider/credential/connection-string exception crosses the repository boundary.
            return dispatched ? unknown() : notDispatched();
        }
    }
}
