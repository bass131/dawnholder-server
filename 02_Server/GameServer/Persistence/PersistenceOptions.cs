using System.Globalization;
using System.Text.Json.Serialization;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>공개 설정과 고정 보안/예산 계약. 자격증명은 이 값에 포함하지 않는다.</summary>
public sealed class PersistenceOptions
{
    [JsonConstructor]
    public PersistenceOptions(
        string persistedAccountName, string database, string serverEndpoint,
        byte slotId, Guid accountId, Guid characterId)
    {
        PersistenceAccountSelector selector = new(persistedAccountName, accountId);
        if (string.IsNullOrWhiteSpace(database) || database.Length > 128 ||
            database != database.Trim() || database.Any(char.IsControl))
            throw new ArgumentException("An explicit database name is required.", nameof(database));
        if (string.IsNullOrWhiteSpace(serverEndpoint) || serverEndpoint.Length > 255 ||
            !serverEndpoint.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase) ||
            serverEndpoint.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is ';' or '=' or '\\'))
            throw new ArgumentException("An explicit TCP endpoint and port are required.", nameof(serverEndpoint));
        int separator = serverEndpoint.LastIndexOf(',');
        if (separator <= 4 || serverEndpoint.AsSpan(4, separator - 4).Contains(',') ||
            !int.TryParse(serverEndpoint.AsSpan(separator + 1), NumberStyles.None,
                CultureInfo.InvariantCulture, out int port) || port is < 1 or > 65535)
            throw new ArgumentException("An explicit TCP endpoint and port are required.", nameof(serverEndpoint));
        Identity = new PersistenceIdentity(slotId, accountId, characterId);
        PersistedAccountName = selector.NormalizedAccountName;
        Database = database;
        ServerEndpoint = serverEndpoint;
    }

    public string PersistedAccountName { get; }
    public string Database { get; }
    public string ServerEndpoint { get; }
    public byte SlotId => Identity.SlotId;
    public Guid AccountId => Identity.AccountId;
    public Guid CharacterId => Identity.CharacterId;
    [JsonIgnore]
    public PersistenceIdentity Identity { get; }
    public int ConnectTimeoutSeconds => 5;
    public int CommandTimeoutSeconds => 5;
    public int LockTimeoutMilliseconds => 2000;
    public bool TrustServerCertificate => false;
    public bool PersistSecurityInfo => false;
    public bool MultipleActiveResultSets => false;
    public bool Enlist => false;
    public int ConnectRetryCount => 0;
    public bool Pooling => true;
    public int MinPoolSize => 0;
    public int MaxPoolSize => 4;
    public string ApplicationName => "Dawnholder.Persistence";
}
