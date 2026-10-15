namespace Dawnholder.Server.GameServer.Persistence;

public sealed class InspectRecoveryRequest : IEquatable<InspectRecoveryRequest>
{
    public InspectRecoveryRequest(PersistenceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
    }

    public PersistenceIdentity Identity { get; }

    public bool Equals(InspectRecoveryRequest? other) => other is not null && Identity.Equals(other.Identity);
    public override bool Equals(object? obj) => obj is InspectRecoveryRequest other && Equals(other);
    public override int GetHashCode() => Identity.GetHashCode();
}
