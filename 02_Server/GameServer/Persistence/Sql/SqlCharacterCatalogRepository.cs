using System.Data;
using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Microsoft.Data.SqlClient;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

/// <summary>Execute-only catalog RPC with explicit types, bounded budgets and complete terminal-response validation.</summary>
public sealed class SqlCharacterCatalogRepository : ICharacterCatalogRepository
{
    static readonly string[] Columns =
        ["Status", "CharacterId", "Slot", "Name", "Class", "Characters", "Rejection", "MigrationManifest", "SchemaVersion", "DatabaseName"];
    readonly PersistenceOptions _options;
    readonly Func<SqlConnection> _connections;

    public SqlCharacterCatalogRepository(PersistenceOptions options, Func<SqlConnection> connectionFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connections = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<bool> PreflightAsync(PersistenceDeadline deadline, CancellationToken token)
        => (await ExecuteAsync("Preflight", null, null, null, null, null, deadline, token).ConfigureAwait(false)).Status == CatalogStatus.Listed;

    public Task<CatalogStorageResult> ListAsync(AccountContext account, PersistenceDeadline deadline, CancellationToken token)
        => ExecuteAsync("List", account, null, null, null, null, deadline, token);

    public Task<CatalogStorageResult> CreateAsync(CreateDescriptor descriptor, Guid operationId,
        SafeDefaults defaults, PersistenceDeadline deadline, CancellationToken token)
        => ExecuteAsync("Create", descriptor.Account, descriptor, operationId, null, defaults, deadline, token);

    public Task<CatalogStorageResult> ResolveCreateAsync(CreateDescriptor descriptor, Guid operationId,
        PersistenceDeadline deadline, CancellationToken token)
        => ExecuteAsync("Resolve", descriptor.Account, descriptor, operationId, null, null, deadline, token);

    public Task<CatalogStorageResult> ReadSelectionAsync(AccountContext account, Guid characterId,
        PersistenceDeadline deadline, CancellationToken token)
        => ExecuteAsync("Select", account, null, null, characterId, null, deadline, token);

    async Task<CatalogStorageResult> ExecuteAsync(string action, AccountContext? account, CreateDescriptor? descriptor,
        Guid? operationId, Guid? characterId, SafeDefaults? defaults, PersistenceDeadline deadline, CancellationToken token)
    {
        bool dispatched = false;
        if (deadline.IsExpired || token.IsCancellationRequested)
            return new(action == "Resolve" ? CatalogStatus.StillUnknown : CatalogStatus.CancelledBeforeDispatch);
        try
        {
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            cancellation.CancelAfter(deadline.GetRemaining());
            await using SqlConnection connection = _connections();
            await connection.OpenAsync(cancellation.Token).ConfigureAwait(false);
            await using SqlCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dh.CharacterCatalog";
            Add(command, "@Action", SqlDbType.VarChar, action, 16);
            if (account is not null)
            {
                Add(command, "@AccountId", SqlDbType.UniqueIdentifier, account.AccountId);
                Add(command, "@AccountName", SqlDbType.VarChar, account.NormalizedAccountName, 16);
            }
            if (characterId.HasValue) Add(command, "@CharacterId", SqlDbType.UniqueIdentifier, characterId.Value);
            if (operationId.HasValue) Add(command, "@OperationId", SqlDbType.UniqueIdentifier, operationId.Value);
            if (descriptor is not null)
            {
                Add(command, "@Slot", SqlDbType.TinyInt, descriptor.Slot);
                Add(command, "@Name", SqlDbType.NVarChar, descriptor.Name, 12);
                Add(command, "@NameKey", SqlDbType.NVarChar, descriptor.NameKey, 12);
                Add(command, "@Class", SqlDbType.TinyInt, (byte)descriptor.Class);
            }
            if (defaults is not null)
            {
                Add(command, "@TownX", SqlDbType.Real, defaults.TownX);
                Add(command, "@TownY", SqlDbType.Real, defaults.TownY);
                Add(command, "@KnightMaxHp", SqlDbType.Int, defaults.KnightMaxHp);
                Add(command, "@MageMaxHp", SqlDbType.Int, defaults.MageMaxHp);
            }
            TimeSpan remaining = deadline.GetRemaining();
            int locks = (int)Math.Min(_options.LockTimeoutMilliseconds, Math.Floor(remaining.TotalMilliseconds));
            if (locks < 1 || cancellation.IsCancellationRequested)
                return new(action == "Resolve" ? CatalogStatus.StillUnknown : CatalogStatus.CancelledBeforeDispatch);
            command.CommandTimeout = (int)Math.Clamp(Math.Ceiling(remaining.TotalSeconds), 1, _options.CommandTimeoutSeconds);
            Add(command, "@LockTimeoutMs", SqlDbType.Int, locks);
            dispatched = true;
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellation.Token).ConfigureAwait(false);
            object[] row = await ReadRowAsync(reader, cancellation.Token).ConfigureAwait(false);
            return Parse(row, action);
        }
        catch (SqlException exception) when (exception.Number == 51023)
        {
            return new(CatalogStatus.SchemaMismatch);
        }
        catch (Exception)
        {
            return new(action == "Resolve" ? CatalogStatus.StillUnknown :
                action == "Create" && dispatched ? CatalogStatus.OutcomeUnknown : CatalogStatus.Unavailable);
        }
    }

    CatalogStorageResult Parse(object[] row, string action)
    {
        if (row[8] is not int schema || schema != SqlPersistenceDeployment.SchemaVersion ||
            row[9] is not string database || database != _options.Database)
            throw new InvalidDataException("Catalog deployment mismatch.");
        if (row[0] is not string statusText || !Enum.TryParse(statusText, out CatalogStatus status) ||
            status.ToString() != statusText || !Allowed(action, status))
            throw new InvalidDataException("Invalid catalog status.");
        if (action == "Preflight")
        {
            SqlPersistenceDeployment.ValidateManifest(row[7] as string ?? throw new InvalidDataException());
        }
        else if (row[7] is not DBNull) throw new InvalidDataException("Unexpected deployment manifest.");
        CharacterSummary? character = null;
        bool hasCharacter = status is CatalogStatus.Created or CatalogStatus.Applied or CatalogStatus.Selected;
        if (hasCharacter)
        {
            if (row[1] is not Guid id || row[2] is not byte slot || row[3] is not string name || row[4] is not byte characterClass)
                throw new InvalidDataException("Incomplete character summary.");
            character = new(id, slot, name, (CharacterClass)characterClass);
            if (!CatalogValidation.Summary(character)) throw new InvalidDataException("Invalid character summary.");
        }
        else if (row.Skip(1).Take(4).Any(v => v is not DBNull)) throw new InvalidDataException("Unexpected character disclosure.");
        List<CharacterSummary> characters = [];
        if (action == "List" && status == CatalogStatus.Listed)
        {
            try
            {
                // The SQL scalar FOR JSON subquery returns NULL for an account with no characters.
                string json = row[5] is DBNull ? "[]" : row[5] as string
                    ?? throw new InvalidDataException("Invalid catalog list.");
                if (json.Length > 4096) throw new InvalidDataException("Invalid catalog list.");
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() > 3)
                    throw new InvalidDataException("Invalid catalog count.");
                foreach (JsonElement value in document.RootElement.EnumerateArray())
                {
                    SqlPersistenceSnapshot.Keys(value, "characterId", "slot", "name", "class");
                    CharacterSummary summary = new(value.GetProperty("characterId").GetGuid(), value.GetProperty("slot").GetByte(),
                        value.GetProperty("name").GetString()!, (CharacterClass)value.GetProperty("class").GetByte());
                    if (!CatalogValidation.Summary(summary)) throw new InvalidDataException("Invalid listed character.");
                    characters.Add(summary);
                }
                if (characters.Select(c => c.Slot).Distinct().Count() != characters.Count ||
                    characters.Select(c => c.CharacterId).Distinct().Count() != characters.Count)
                    throw new InvalidDataException("Duplicate character list.");
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException or
                InvalidOperationException or FormatException)
            {
                // A received list can be corrupt without a transport failure; mutation outcomes remain uncertain elsewhere.
                return new(CatalogStatus.IntegrityFailure);
            }
        }
        else if (row[5] is not DBNull) throw new InvalidDataException("Unexpected character list.");
        CatalogStatus? rejection = null;
        if (status == CatalogStatus.NotApplied)
        {
            if (row[6] is not string code || !Enum.TryParse(code, out CatalogStatus parsed) ||
                parsed.ToString() != code || parsed is not (CatalogStatus.SlotsFull or CatalogStatus.SlotOccupied or
                    CatalogStatus.NameTaken or CatalogStatus.CancelledBeforeDispatch))
                throw new InvalidDataException("Invalid create rejection.");
            rejection = parsed;
        }
        else if (row[6] is not DBNull) throw new InvalidDataException("Unexpected rejection.");
        return new(status, character, characters, rejection);
    }

    static bool Allowed(string action, CatalogStatus status) => status == CatalogStatus.IntegrityFailure || (action switch
    {
        "Preflight" or "List" => status == CatalogStatus.Listed,
        "Select" => status is CatalogStatus.Selected or CatalogStatus.NotOwned or CatalogStatus.CharacterNotFound,
        "Resolve" => status is CatalogStatus.Applied or CatalogStatus.NotApplied or CatalogStatus.OperationConflict,
        "Create" => status is CatalogStatus.Created or CatalogStatus.SlotsFull or CatalogStatus.SlotOccupied or
            CatalogStatus.NameTaken or CatalogStatus.CancelledBeforeDispatch or CatalogStatus.OperationConflict,
        _ => false
    });

    static async Task<object[]> ReadRowAsync(SqlDataReader reader, CancellationToken token)
    {
        if (reader.FieldCount != Columns.Length || !Enumerable.Range(0, Columns.Length).All(i => reader.GetName(i) == Columns[i]))
            throw new InvalidDataException("Invalid catalog transport.");
        if (!await reader.ReadAsync(token).ConfigureAwait(false)) throw new InvalidDataException("Missing catalog result.");
        object[] values = new object[Columns.Length];
        reader.GetValues(values);
        if (await reader.ReadAsync(token).ConfigureAwait(false) || await reader.NextResultAsync(token).ConfigureAwait(false))
            throw new InvalidDataException("Unexpected catalog response.");
        return values;
    }

    static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
        => SqlRuntimeParameters.Add(command, name, type, value, size);
}
