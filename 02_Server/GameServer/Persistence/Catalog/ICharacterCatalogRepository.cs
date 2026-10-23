namespace Dawnholder.Server.GameServer.Persistence.Catalog;

/// <summary>Storage facts only. The facade owns request correlation, pending work and admission lifetimes.</summary>
public interface ICharacterCatalogRepository
{
    Task<CatalogStorageResult> ListAsync(AccountContext account, PersistenceDeadline deadline, CancellationToken token);
    Task<CatalogStorageResult> CreateAsync(CreateDescriptor descriptor, Guid operationId,
        SafeDefaults defaults, PersistenceDeadline deadline, CancellationToken token);
    Task<CatalogStorageResult> ResolveCreateAsync(CreateDescriptor descriptor, Guid operationId,
        PersistenceDeadline deadline, CancellationToken token);
    Task<CatalogStorageResult> ReadSelectionAsync(AccountContext account, Guid characterId,
        PersistenceDeadline deadline, CancellationToken token);
}

public sealed class CatalogStorageResult
{
    public CatalogStorageResult(CatalogStatus status, CharacterSummary? character = null,
        IEnumerable<CharacterSummary>? characters = null, CatalogStatus? rejection = null)
    {
        Status = status;
        Character = character;
        Characters = Array.AsReadOnly(characters?.ToArray() ?? []);
        Rejection = rejection;
    }

    public CatalogStatus Status { get; }
    public CharacterSummary? Character { get; }
    public IReadOnlyList<CharacterSummary> Characters { get; }
    public CatalogStatus? Rejection { get; }
}
