using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

/// <summary>dh.EmitPersistenceResult의 한 terminal row와 마지막 completion을 함께 요구한다.</summary>
internal sealed class SqlPersistenceResult
{
    static readonly string[] Columns =
    [
        "TransportVersion", "Status", "OperationId", "Kind", "Outcome", "ResultCode", "ResultSnapshot", "IsReplay",
        "RecordedUtc", "DatabaseName", "ProductVersion", "SchemaVersion", "PayloadVersion", "SnapshotVersion",
        "MigrationManifest", "CurrentSlotId", "CurrentAccountId", "CurrentCharacterId", "CurrentOwnerKind",
        "CurrentOwnerId", "CurrentFence", "CurrentLastSequence", "CurrentAccountPresent", "CurrentCharacterPresent",
        "CurrentClass", "CurrentCharacterVersion", "CurrentProgressPresent", "CurrentProgressVersion", "CurrentStoredProgress"
    ];
    readonly Dictionary<string, object> _values;

    SqlPersistenceResult(Dictionary<string, object> values, PersistenceOptions options, PersistenceIdentity expectedIdentity)
    {
        _values = values;
        if (Required<int>("TransportVersion") != 1 || Required<int>("PayloadVersion") != 1 ||
            Required<int>("SnapshotVersion") != 1 || Required<int>("SchemaVersion") != SqlPersistenceDeployment.SchemaVersion ||
            Required<string>("DatabaseName") != options.Database ||
            !Version.TryParse(Required<string>("ProductVersion"), out _) || Required<int>("CurrentSlotId") is < 1 or > 3)
            throw new InvalidDataException("Incompatible persistence transport.");
        PersistenceIdentity identity = new((byte)Required<int>("CurrentSlotId"),
            Required<Guid>("CurrentAccountId"), Required<Guid>("CurrentCharacterId"));
        if (!identity.Equals(expectedIdentity))
            throw new InvalidDataException("Persistence binding mismatch.");
        Authority = new PersistenceAuthority(identity, (PersistenceOwnerKind)Required<byte>("CurrentOwnerKind"),
            Optional<Guid>("CurrentOwnerId"), Required<long>("CurrentFence"), Required<long>("CurrentLastSequence"));
        Status = Required<string>("Status");
        _ = Required<bool>("IsReplay");
    }

    internal string Status { get; }
    internal PersistenceAuthority Authority { get; }
    internal PersistenceResultCode Code => (PersistenceResultCode)Required<short>("ResultCode");
    internal byte Outcome => Required<byte>("Outcome");

    internal static async Task<SqlPersistenceResult> ReadAsync(
        SqlDataReader reader, PersistenceOptions options, CancellationToken cancellationToken, PersistenceIdentity? expectedIdentity = null)
    {
        Dictionary<string, object>? row = null;
        bool malformed = false;
        int resultSets = 0;
        do
        {
            resultSets++;
            bool schemaMatches = reader.FieldCount == Columns.Length &&
                Enumerable.Range(0, reader.FieldCount).All(index => reader.GetName(index) == Columns[index]);
            if (!schemaMatches)
                malformed = true;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (row is not null || !schemaMatches)
                {
                    malformed = true;
                    continue;
                }
                row = new Dictionary<string, object>(StringComparer.Ordinal);
                for (int index = 0; index < Columns.Length; index++)
                    row.Add(Columns[index], reader.GetValue(index));
            }
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
        // Exceptions from trailing DONE/error tokens reach the repository before any result can become success.
        if (malformed || resultSets != 1 || row is null)
            throw new InvalidDataException("Incomplete persistence result.");
        return new SqlPersistenceResult(row, options, expectedIdentity ?? options.Identity);
    }

    internal AdmissionProbe Admission()
    {
        if (Status is not ("AdmissionFree" or "AdmissionHeld") ||
            (Status == "AdmissionFree") != (Authority.OwnerKind == PersistenceOwnerKind.Free) ||
            Required<bool>("IsReplay"))
            throw new InvalidDataException("Invalid admission observation.");
        foreach (string field in new[] { "OperationId", "Kind", "Outcome", "ResultCode", "ResultSnapshot", "RecordedUtc",
            "CurrentAccountPresent", "CurrentCharacterPresent", "CurrentClass", "CurrentCharacterVersion",
            "CurrentProgressPresent", "CurrentProgressVersion", "CurrentStoredProgress" })
            Null(field);
        SqlPersistenceDeployment.ValidateManifest(Required<string>("MigrationManifest"));
        return new AdmissionProbe(Authority, schemaCompatible: true, bindingMatches: true);
    }

    internal PersistenceObservation Current()
    {
        bool account = Required<bool>("CurrentAccountPresent");
        bool character = Required<bool>("CurrentCharacterPresent");
        bool progress = Required<bool>("CurrentProgressPresent");
        PersistenceToken? characterToken = Token("CurrentCharacterVersion");
        PersistenceToken? progressToken = Token("CurrentProgressVersion");
        if (character != (characterToken is not null) || progress != (progressToken is not null) ||
            character && !account || progress && !character)
            throw new InvalidDataException("Inconsistent current presence.");
        if (character)
            _ = SqlPersistenceSnapshot.CharacterClassValue(Required<byte>("CurrentClass"));
        else
            Null("CurrentClass");
        if (progress)
        {
            string json = Required<string>("CurrentStoredProgress");
            if (json.Length > 2048)
                throw new InvalidDataException("Invalid current progress.");
            using JsonDocument document = JsonDocument.Parse(json);
            SqlPersistenceSnapshot.ValidateProgress(document.RootElement, safeProjection: false);
        }
        else
            Null("CurrentStoredProgress");
        return new PersistenceObservation(Authority, characterToken, progressToken);
    }

    internal void ValidateOperation(OperationIdentity operation, bool resolving)
    {
        Null("MigrationManifest");
        if (Required<Guid>("OperationId") != operation.OperationId || Required<int>("Kind") != (int)operation.Kind)
            throw new InvalidDataException("Operation result identity mismatch.");
        if (resolving && Status == "StillUnknown")
        {
            foreach (string field in new[] { "Outcome", "ResultCode", "ResultSnapshot", "RecordedUtc" })
                Null(field);
            if (Required<bool>("IsReplay"))
                throw new InvalidDataException("Invalid unresolved result.");
            return;
        }
        if (Status != "Terminal")
            throw new InvalidDataException("No terminal operation proof.");
        _ = Required<DateTime>("RecordedUtc");
        if (Outcome == 1)
        {
            bool allowed = operation.Kind switch
            {
                PersistenceOperationKind.Acquire => Code == PersistenceResultCode.Acquired,
                PersistenceOperationKind.Checkpoint => Code is PersistenceResultCode.CheckpointApplied or
                    PersistenceResultCode.CheckpointNoChange,
                PersistenceOperationKind.ReleaseRuntime => Code == PersistenceResultCode.Released,
                _ => false
            };
            if (!allowed)
                throw new InvalidDataException("Invalid applied operation code.");
        }
        else if (Outcome == 2)
        {
            Null("ResultSnapshot");
            PersistenceContractGuard.NotApplied(Code);
            if (operation.Kind != PersistenceOperationKind.Checkpoint && Code == PersistenceResultCode.Conflict ||
                operation.Kind == PersistenceOperationKind.ReleaseRuntime && Code == PersistenceResultCode.InvalidClass)
                throw new InvalidDataException("Invalid rejection code for operation.");
        }
        else
            throw new InvalidDataException("Unknown operation outcome.");
    }

    internal OperationReceipt Receipt(OperationIdentity operation) =>
        SqlPersistenceSnapshot.Parse(Required<string>("ResultSnapshot"), operation, Code);

    T Required<T>(string name) => _values[name] is T value
        ? value : throw new InvalidDataException("Invalid persistence field type.");

    T? Optional<T>(string name) where T : struct => _values[name] is DBNull ? null : Required<T>(name);

    PersistenceToken? Token(string name) => _values[name] is DBNull
        ? null : new PersistenceToken(Required<byte[]>(name));

    void Null(string name)
    {
        if (_values[name] is not DBNull)
            throw new InvalidDataException("Unexpected persistence field.");
    }
}
