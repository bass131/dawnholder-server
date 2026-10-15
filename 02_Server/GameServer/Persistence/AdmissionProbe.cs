namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>schema/binding과 authority의 읽기 관측이며 acquire 또는 입장 성공이 아니다.</summary>
public sealed class AdmissionProbe : IEquatable<AdmissionProbe>
{
    public AdmissionProbe(PersistenceAuthority observedAuthority, bool schemaCompatible, bool bindingMatches)
    {
        ArgumentNullException.ThrowIfNull(observedAuthority);
        ObservedAuthority = observedAuthority;
        SchemaCompatible = schemaCompatible;
        BindingMatches = bindingMatches;
    }

    public PersistenceAuthority ObservedAuthority { get; }
    public bool SchemaCompatible { get; }
    public bool BindingMatches { get; }

    public bool Equals(AdmissionProbe? other) =>
        other is not null && ObservedAuthority.Equals(other.ObservedAuthority) &&
        SchemaCompatible == other.SchemaCompatible && BindingMatches == other.BindingMatches;

    public override bool Equals(object? obj) => obj is AdmissionProbe other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ObservedAuthority, SchemaCompatible, BindingMatches);
}
