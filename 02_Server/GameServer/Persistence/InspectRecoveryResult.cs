using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>관리자에게 반환할 현재 저장 진단. 게임 입장이나 데이터 수선을 수행하지 않는다.</summary>
public sealed class InspectRecoveryResult : IEquatable<InspectRecoveryResult>
{
    InspectRecoveryResult(
        InspectRecoveryStatus status, PersistenceObservation? current = null, StoredState? stored = null)
    {
        Status = status;
        Current = current;
        Stored = stored;
    }

    public InspectRecoveryStatus Status { get; }
    public PersistenceObservation? Current { get; }
    public StoredState? Stored { get; }

    public static InspectRecoveryResult Inspected(PersistenceObservation current, StoredState stored)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(stored);
        stored.ValidateCurrent(current);
        return new(InspectRecoveryStatus.Inspected, current, stored);
    }

    public static InspectRecoveryResult ReadFailed() => new(InspectRecoveryStatus.ReadFailed);
    public static InspectRecoveryResult NotDispatched() => new(InspectRecoveryStatus.NotDispatched);

    public bool Equals(InspectRecoveryResult? other) =>
        other is not null && Status == other.Status && Equals(Current, other.Current) && Equals(Stored, other.Stored);

    public override bool Equals(object? obj) => obj is InspectRecoveryResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Current, Stored);

    /// <summary>동일 관측의 class/token/presence. missing Progress를 정상 기본값으로 채우지 않는다.</summary>
    public sealed class StoredState : IEquatable<StoredState>
    {
        public StoredState(
            PersistenceIdentity identity, CharacterClass? storedClass,
            PersistenceToken? characterToken, PersistenceToken? progressToken, StoredProgress? progress)
        {
            ArgumentNullException.ThrowIfNull(identity);
            if (storedClass is { } characterClass)
                PersistenceContractGuard.Defined(characterClass, nameof(storedClass));
            if ((storedClass is null) != (characterToken is null))
                throw new ArgumentException("Stored class and character token must be present together.");
            if ((progressToken is null) != (progress is null))
                throw new ArgumentException("Stored progress and its token must be present together.");
            if (characterToken is null && progress is not null)
                throw new ArgumentException("Stored progress cannot exist without a character.");
            Identity = identity;
            StoredClass = storedClass;
            CharacterToken = characterToken;
            ProgressToken = progressToken;
            Progress = progress;
        }

        public PersistenceIdentity Identity { get; }
        public CharacterClass? StoredClass { get; }
        public PersistenceToken? CharacterToken { get; }
        public PersistenceToken? ProgressToken { get; }
        public StoredProgress? Progress { get; }
        public bool CharacterPresent => CharacterToken is not null;
        public bool ProgressPresent => Progress is not null;

        internal void ValidateCurrent(PersistenceObservation current)
        {
            if (!Identity.Equals(current.Authority.Identity) ||
                !Equals(CharacterToken, current.CharacterToken) || !Equals(ProgressToken, current.ProgressToken))
                throw new ArgumentException("Diagnostic state does not match the current observation.", nameof(current));
        }

        internal void ValidateReceipt(OperationReceipt receipt)
        {
            if (receipt.Kind != PersistenceOperationKind.Recover ||
                !Identity.Equals(receipt.Authority.Identity) || StoredClass != receipt.StoredClass ||
                !Equals(CharacterToken, receipt.CharacterToken) || !Equals(ProgressToken, receipt.ProgressToken))
                throw new ArgumentException("Diagnostic history does not match the recovery receipt.", nameof(receipt));
        }

        public bool Equals(StoredState? other) =>
            other is not null && Identity.Equals(other.Identity) && StoredClass == other.StoredClass &&
            Equals(CharacterToken, other.CharacterToken) && Equals(ProgressToken, other.ProgressToken) &&
            Equals(Progress, other.Progress);

        public override bool Equals(object? obj) => obj is StoredState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Identity, StoredClass, CharacterToken, ProgressToken, Progress);
    }

    /// <summary>
    /// 저장된 원 진단값이다. map/HP/unlock를 Town·풀 HP·잠금으로 보정하지 않는다.
    /// 유한 좌표만 요구하며 legacy HP/MaxHp의 관계로 게임 Ready를 판단하지 않는다.
    /// </summary>
    public sealed class StoredProgress : IEquatable<StoredProgress>
    {
        public StoredProgress(int mapId, float x, float y, int hp, int maxHp, bool bossUnlocked)
        {
            PersistenceContractGuard.Finite(x, nameof(x));
            PersistenceContractGuard.Finite(y, nameof(y));
            MapId = mapId;
            X = x;
            Y = y;
            Hp = hp;
            MaxHp = maxHp;
            BossUnlocked = bossUnlocked;
        }

        public int MapId { get; }
        public float X { get; }
        public float Y { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public bool BossUnlocked { get; }

        public bool Equals(StoredProgress? other) =>
            other is not null && MapId == other.MapId && X.Equals(other.X) && Y.Equals(other.Y) &&
            Hp == other.Hp && MaxHp == other.MaxHp && BossUnlocked == other.BossUnlocked;

        public override bool Equals(object? obj) => obj is StoredProgress other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(MapId, X, Y, Hp, MaxHp, BossUnlocked);
    }
}
