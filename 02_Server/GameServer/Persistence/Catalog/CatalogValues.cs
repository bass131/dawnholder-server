using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Catalog;

public sealed record AccountContext(Guid AccountId, string NormalizedAccountName);
public sealed record RequestContext(Guid SessionInstanceId, long Generation);
public sealed record CharacterSummary(Guid CharacterId, byte Slot, string Name, CharacterClass Class);

public enum CharacterStorageMode { Legacy, Stored }

public sealed record CreatePreparationResult(
    CatalogStatus Status, CreateDescriptor? Descriptor = null, InvalidField InvalidField = InvalidField.None);

/// <summary>Copies list storage; neither repository nor caller can mutate a published result.</summary>
public sealed class CatalogResult
{
    public CatalogResult(CatalogStatus status, RequestContext request,
        CharacterSummary? character = null, IEnumerable<CharacterSummary>? characters = null,
        InvalidField invalidField = InvalidField.None, CatalogStatus? rejection = null, AdmissionLease? lease = null)
    {
        Status = status;
        Request = request;
        Character = character;
        Characters = Array.AsReadOnly(characters?.ToArray() ?? []);
        InvalidField = invalidField;
        Rejection = rejection;
        Lease = lease;
    }

    public CatalogStatus Status { get; }
    public RequestContext Request { get; }
    public CharacterSummary? Character { get; }
    public IReadOnlyList<CharacterSummary> Characters { get; }
    public InvalidField InvalidField { get; }
    public CatalogStatus? Rejection { get; }
    public AdmissionLease? Lease { get; }
}
