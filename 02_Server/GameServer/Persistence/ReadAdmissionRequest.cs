namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ReadAdmissionRequest : IEquatable<ReadAdmissionRequest>
{
    public ReadAdmissionRequest(PersistenceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
    }

    public PersistenceIdentity Identity { get; }

    public bool Equals(ReadAdmissionRequest? other) => other is not null && Identity.Equals(other.Identity);
    public override bool Equals(object? obj) => obj is ReadAdmissionRequest other && Equals(other);
    public override int GetHashCode() => Identity.GetHashCode();
}
