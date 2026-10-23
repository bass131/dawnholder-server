using System.Net;
using System.Security;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Sql;
using Microsoft.Data.SqlClient;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Independent verification (technical-spec §2.1 「저장소 설정과 수명」, §7; api-contract v2 「저장 대상 설정 한 곳」):
/// the runtime connection is TLS with server-certificate validation, carries no ambient identity or transaction, has no
/// driver retry, a fixed Connect Timeout of 5, one bounded pool and a fixed application name. The password travels only
/// in the credential object. Expected values are copied from that text, not read back from PersistenceOptions.
/// SqlConnection objects are only constructed here; no connection is ever opened.
/// </summary>
public sealed class SqlRuntimeConnectionContractTests
{
    const string Endpoint = "tcp:127.0.0.1,14330";
    const string Database = "DawnholderRuntimeProbe";
    const string UserName = "dh_runtime_probe";
    const string PasswordText = "not-a-real-secret-1";

    static PersistenceOptions Options(string database = Database, string endpoint = Endpoint)
        => new(AdmissionTestWorld.PersistedName, database, endpoint, 1, AdmissionTestWorld.PersistedAccountId, CharacterId);

    static SecureString Secret(string text)
    {
        SecureString secret = new();
        foreach (char character in text)
            secret.AppendChar(character);
        secret.MakeReadOnly();
        return secret;
    }

    static string Reveal(SecureString secret) => new NetworkCredential(string.Empty, secret).Password;

    [Fact]
    public void ConnectionString_CarriesTheFixedTransportSecurityAndPoolContract()
    {
        using SecureString password = Secret(PasswordText);
        using SqlPersistenceConnectionFactory factory = new(Options(), UserName, password);
        using SqlConnection connection = factory.CreateConnection();
        SqlConnectionStringBuilder actual = new(connection.ConnectionString);

        Assert.Equal(Endpoint, actual.DataSource);
        Assert.Equal(Database, actual.InitialCatalog);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, actual.Encrypt);
        Assert.False(actual.TrustServerCertificate);
        Assert.False(actual.IntegratedSecurity);
        Assert.False(actual.PersistSecurityInfo);
        Assert.False(actual.MultipleActiveResultSets);
        Assert.False(actual.Enlist);
        Assert.Equal(0, actual.ConnectRetryCount);
        Assert.Equal(5, actual.ConnectTimeout);
        Assert.True(actual.Pooling);
        Assert.Equal(0, actual.MinPoolSize);
        Assert.Equal(4, actual.MaxPoolSize);
        Assert.Equal("Dawnholder.Persistence", actual.ApplicationName);
    }

    [Fact]
    public void Password_StaysOutOfTheConnectionString_AndTravelsOnlyInTheCredential()
    {
        using SecureString password = Secret(PasswordText);
        using SqlPersistenceConnectionFactory factory = new(Options(), UserName, password);
        using SqlConnection connection = factory.CreateConnection();
        SqlConnectionStringBuilder actual = new(connection.ConnectionString);

        bool passwordInConnectionString = connection.ConnectionString.Contains(PasswordText, StringComparison.Ordinal);
        Assert.False(passwordInConnectionString);
        Assert.Equal(string.Empty, actual.UserID);
        Assert.Equal(string.Empty, actual.Password);
        Assert.NotNull(connection.Credential);
        Assert.Equal(UserName, connection.Credential.UserId);
        Assert.Equal(PasswordText, Reveal(connection.Credential.Password));
    }

    [Fact]
    public void Factory_KeepsItsOwnPassword_AfterTheCallerDisposesTheirs()
    {
        SecureString callerPassword = Secret(PasswordText);
        using SqlPersistenceConnectionFactory factory = new(Options(), UserName, callerPassword);

        callerPassword.Dispose();
        using SqlConnection connection = factory.CreateConnection();

        Assert.Equal(PasswordText, Reveal(connection.Credential.Password));
    }

    [Fact]
    public void DisposedFactory_LendsNoFurtherConnection()
    {
        using SecureString password = Secret(PasswordText);
        SqlPersistenceConnectionFactory factory = new(Options(), UserName, password);

        factory.Dispose();

        Assert.Throws<ObjectDisposedException>(() => factory.CreateConnection());
    }

    [Fact]
    public void ProfileDatabaseValue_CannotRewriteTheSecurityKeywords()
    {
        const string hostile = "Runtime;Encrypt=False;TrustServerCertificate=True;Integrated Security=True";
        using SecureString password = Secret(PasswordText);
        using SqlPersistenceConnectionFactory factory = new(Options(database: hostile), UserName, password);
        using SqlConnection connection = factory.CreateConnection();
        SqlConnectionStringBuilder actual = new(connection.ConnectionString);

        Assert.Equal(hostile, actual.InitialCatalog);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, actual.Encrypt);
        Assert.False(actual.TrustServerCertificate);
        Assert.False(actual.IntegratedSecurity);
    }

    [Theory]
    [InlineData("approved-host,1433")]
    [InlineData("tcp:approved-host")]
    [InlineData(@"tcp:approved-host\SQLEXPRESS,1433")]
    [InlineData("tcp:,1433")]
    [InlineData("tcp:approved-host,0")]
    [InlineData("tcp:approved-host,65536")]
    [InlineData("tcp:approved-host,14 33")]
    [InlineData("tcp:approved-host;Encrypt=False,1433")]
    [InlineData("tcp:first,second,1433")]
    [InlineData("np:approved-host")]
    [InlineData("")]
    public void EndpointWithoutOneExplicitTcpHostAndPort_IsRefusedAtConfiguration(string endpoint)
    {
        // §2.1: an explicit endpoint; no SQL Browser discovery and no fallback to another port.
        Assert.ThrowsAny<ArgumentException>(() => Options(endpoint: endpoint));
    }

    [Theory]
    [InlineData("tcp:approved-host,1433")]
    [InlineData("tcp:127.0.0.1,14330")]
    public void ExplicitTcpHostAndPort_IsKeptExactly(string endpoint)
    {
        PersistenceOptions options = Options(endpoint: endpoint);

        Assert.Equal(endpoint, options.ServerEndpoint);
        Assert.Equal(AdmissionTestWorld.PersistedName, options.PersistedAccountName);
    }

    [Fact]
    public void BindingThatDoesNotIdentifyTheConfiguredAccount_IsRefusedAtConfiguration()
    {
        // The compatibility constructor binds values; the catalog profile's startup gate is a separate contract.
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceOptions(
            AdmissionTestWorld.PersistedName, Database, Endpoint, 1, AdmissionTestWorld.OtherAccountId, CharacterId));
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)2)]
    [InlineData((byte)3)]
    public void CompatibilityBinding_KeepsEachAccountSlotAndValidatedIdentity(byte slotId)
    {
        PersistenceOptions options = new(
            AdmissionTestWorld.PersistedName, Database, Endpoint, slotId, AdmissionTestWorld.PersistedAccountId, CharacterId);

        Assert.Equal(slotId, options.SlotId);
        Assert.Equal(AdmissionTestWorld.PersistedAccountId, options.AccountId);
        Assert.Equal(CharacterId, options.CharacterId);
        Assert.Equal(AdmissionTestWorld.PersistedName, options.PersistedAccountName);
        Assert.Equal(Database, options.Database);
        Assert.Equal(Endpoint, options.ServerEndpoint);
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)4)]
    [InlineData((byte)255)]
    public void CompatibilityBinding_RejectsSlotsOutsideTheThreeAccountSlots(byte slotId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceOptions(
            AdmissionTestWorld.PersistedName, Database, Endpoint, slotId, AdmissionTestWorld.PersistedAccountId, CharacterId));
    }

    [Fact]
    public void CompatibilityBinding_RejectsEmptyAccountId()
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceOptions(
            AdmissionTestWorld.PersistedName, Database, Endpoint, 1, Guid.Empty, CharacterId));
    }

    [Fact]
    public void CompatibilityBinding_RejectsEmptyCharacterId()
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceOptions(
            AdmissionTestWorld.PersistedName, Database, Endpoint, 1, AdmissionTestWorld.PersistedAccountId, Guid.Empty));
    }

    [Fact]
    public void CompatibilityBinding_RejectsDatabaseNamesWithLeadingWhitespace()
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceOptions(
            AdmissionTestWorld.PersistedName, " " + Database, Endpoint, 1, AdmissionTestWorld.PersistedAccountId, CharacterId));
    }
}
