namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>순서나 시간으로 해석하지 않는 8byte token. 부재는 이 타입의 null이다.</summary>
public sealed class PersistenceToken : IEquatable<PersistenceToken>
{
    readonly byte[] _bytes;

    public PersistenceToken(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length != 8)
            throw new ArgumentException("A persistence token must contain exactly eight bytes.", nameof(bytes));
        _bytes = (byte[])bytes.Clone();
    }

    public byte[] ToArray() => (byte[])_bytes.Clone();

    public bool Equals(PersistenceToken? other) =>
        other is not null && _bytes.AsSpan().SequenceEqual(other._bytes);

    public override bool Equals(object? obj) => obj is PersistenceToken other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (byte value in _bytes)
            hash.Add(value);
        return hash.ToHashCode();
    }
}
