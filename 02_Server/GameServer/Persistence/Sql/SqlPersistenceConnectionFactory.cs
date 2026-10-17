using System.Security;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

/// <summary>한 런타임의 고정 pool identity를 소유한다. 호출마다 새 연결을 빌려준다.</summary>
public sealed class SqlPersistenceConnectionFactory : IDisposable
{
    readonly string _connectionString;
    readonly SecureString _password;
    readonly SqlCredential _credential;
    bool _disposed;

    public SqlPersistenceConnectionFactory(PersistenceOptions options, string userName, SecureString password)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(password);
        if (string.IsNullOrWhiteSpace(userName) || userName.Length > 128 || userName.Any(char.IsControl) ||
            password.Length is < 1 or > 128)
            throw new ArgumentException("The runtime credential is invalid.");
        SqlConnectionStringBuilder builder = new()
        {
            DataSource = options.ServerEndpoint,
            InitialCatalog = options.Database,
            IntegratedSecurity = false,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = options.TrustServerCertificate,
            PersistSecurityInfo = options.PersistSecurityInfo,
            MultipleActiveResultSets = options.MultipleActiveResultSets,
            Enlist = options.Enlist,
            ConnectRetryCount = options.ConnectRetryCount,
            ConnectTimeout = options.ConnectTimeoutSeconds,
            Pooling = options.Pooling,
            MinPoolSize = options.MinPoolSize,
            MaxPoolSize = options.MaxPoolSize,
            ApplicationName = options.ApplicationName
        };
        _connectionString = builder.ConnectionString;
        _password = password.Copy();
        _password.MakeReadOnly();
        _credential = new SqlCredential(userName, _password);
    }

    public SqlConnection CreateConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new SqlConnection(_connectionString, _credential);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _password.Dispose();
    }
}
