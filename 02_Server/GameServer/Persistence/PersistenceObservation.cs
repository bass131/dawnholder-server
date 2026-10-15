namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>저장 경계에서 읽은 현재 값의 관측. 이력과 대조하기 전에는 재사용 권한이 아니다.</summary>
public sealed class PersistenceObservation : IEquatable<PersistenceObservation>
{
    public PersistenceObservation(
        PersistenceAuthority authority, PersistenceToken? characterToken, PersistenceToken? progressToken)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (characterToken is null && progressToken is not null)
            throw new ArgumentException("Progress cannot exist without a character token.", nameof(progressToken));
        Authority = authority;
        CharacterToken = characterToken;
        ProgressToken = progressToken;
    }

    public PersistenceAuthority Authority { get; }
    public PersistenceToken? CharacterToken { get; }
    public PersistenceToken? ProgressToken { get; }

    public bool Equals(PersistenceObservation? other) =>
        other is not null && Authority.Equals(other.Authority) &&
        Equals(CharacterToken, other.CharacterToken) && Equals(ProgressToken, other.ProgressToken);

    public override bool Equals(object? obj) => obj is PersistenceObservation other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Authority, CharacterToken, ProgressToken);
}
