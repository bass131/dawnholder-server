using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

public sealed class PersistenceWorkResult
{
    internal PersistenceWorkResult(PersistenceWorkStatus status, CharacterClass? loadedClass = null)
    {
        Status = status;
        LoadedClass = status == PersistenceWorkStatus.Acquired ? loadedClass : null;
    }

    public PersistenceWorkStatus Status { get; }

    /// <summary>Published only after the owner confirms the current acquire receipt and tokens.</summary>
    public CharacterClass? LoadedClass { get; }
}
