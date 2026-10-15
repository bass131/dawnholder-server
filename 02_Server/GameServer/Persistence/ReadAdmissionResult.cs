namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ReadAdmissionResult : IEquatable<ReadAdmissionResult>
{
    ReadAdmissionResult(ReadAdmissionStatus status, AdmissionProbe? admission = null)
    {
        Status = status;
        Admission = admission;
    }

    public ReadAdmissionStatus Status { get; }
    public AdmissionProbe? Admission { get; }

    public static ReadAdmissionResult Probe(AdmissionProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return new(ReadAdmissionStatus.Probe, probe);
    }

    public static ReadAdmissionResult ReadFailed() => new(ReadAdmissionStatus.ReadFailed);
    public static ReadAdmissionResult NotDispatched() => new(ReadAdmissionStatus.NotDispatched);

    public bool Equals(ReadAdmissionResult? other) =>
        other is not null && Status == other.Status && Equals(Admission, other.Admission);

    public override bool Equals(object? obj) => obj is ReadAdmissionResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Admission);
}
