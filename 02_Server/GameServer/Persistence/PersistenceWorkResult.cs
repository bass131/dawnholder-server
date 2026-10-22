using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

public sealed class PersistenceWorkResult
{
    internal PersistenceWorkResult(PersistenceWorkStatus status, CharacterClass? loadedClass = null,
        PersistenceResultCode? code = null)
    {
        Status = status;
        LoadedClass = status == PersistenceWorkStatus.Acquired ? loadedClass : null;
        Code = code;
    }

    public PersistenceWorkStatus Status { get; }

    /// <summary>Published only after the owner confirms the current acquire receipt and tokens.</summary>
    public CharacterClass? LoadedClass { get; }

    /// <summary>The original rejection for this operation; Status still determines certainty and current ownership.</summary>
    public PersistenceResultCode? Code { get; }
}
