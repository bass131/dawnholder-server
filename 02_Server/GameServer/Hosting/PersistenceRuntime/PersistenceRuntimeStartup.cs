using System.Text.Json;
using System.Text.Json.Serialization;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Admission;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Dawnholder.Server.GameServer.Persistence.Sql;

namespace Dawnholder.Server.GameServer.Hosting.PersistenceRuntime;

/// <summary>명시한 profile의 설정/credential/preflight가 확인된 뒤 admission을 구성한다.</summary>
internal sealed class PersistenceRuntimeStartup : IDisposable
{
    readonly SqlPersistenceConnectionFactory _factory;

    PersistenceRuntimeStartup(SqlPersistenceConnectionFactory factory, CharacterAdmissionServices services)
    {
        _factory = factory;
        Services = services;
    }

    internal CharacterAdmissionServices Services { get; }

    internal static async Task<PersistenceRuntimeStartup?> CreateAsync(string[] args, CancellationToken cancellationToken,
        SafeDefaults? defaults = null)
    {
        if (args.Length == 0)
            return null;
        if (args.Length != 2 || args[0] != "--persistence-profile" || string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("Select one public persistence profile.");
        ArgumentNullException.ThrowIfNull(defaults);
        PersistenceOptions options = await ReadOptionsAsync(args[1], cancellationToken).ConfigureAwait(false);
        using CancellationTokenSource secretBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        secretBudget.CancelAfter(TimeSpan.FromSeconds(5));
        using PersistenceRuntimeCredential credential = await PersistenceRuntimeCredential.ReadAsync(
            Console.OpenStandardInput(), secretBudget.Token).ConfigureAwait(false);
        SqlPersistenceConnectionFactory factory = credential.CreateFactory(options);
        try
        {
            SqlCharacterRepository repository = new(options, factory.CreateConnection);
            SqlCharacterCatalogRepository catalogRepository = new(options, factory.CreateConnection);
            PersistenceDeadline deadline = PersistenceDeadline.FromBudget(TimeProvider.System, TimeSpan.FromSeconds(5));
            if (!await catalogRepository.PreflightAsync(deadline, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Persistence preflight failed.");
            CharacterCatalogService catalog = new(catalogRepository, repository, defaults, TimeProvider.System);
            CharacterAdmissionServices services = new(catalog, TimeProvider.System);
            return new PersistenceRuntimeStartup(factory, services);
        }
        catch
        {
            factory.Dispose();
            throw;
        }
    }

    static async Task<PersistenceOptions> ReadOptionsAsync(string path, CancellationToken cancellationToken)
    {
        byte[] bytes = new byte[16385];
        await using FileStream file = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        int length = 0;
        while (length < bytes.Length)
        {
            int read = await file.ReadAsync(bytes.AsMemory(length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;
            length += read;
        }
        if (length == 0 || length == bytes.Length)
            throw new InvalidDataException("Persistence profile is invalid.");
        using JsonDocument document = JsonDocument.Parse(bytes.AsMemory(0, length));
        SqlPersistenceSnapshot.Keys(document.RootElement,
            "profileVersion", "database", "serverEndpoint");
        JsonSerializerOptions serializer = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters = true
        };
        return document.RootElement.Deserialize<PersistenceOptions>(serializer)
            ?? throw new InvalidDataException("Persistence profile is invalid.");
    }

    public void Dispose() => _factory.Dispose();
}
