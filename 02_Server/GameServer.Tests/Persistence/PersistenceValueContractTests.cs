using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Value invariants from api-contract.md 「공통 값과 소유」 and technical-spec §3.1·§3.3.
/// Expected values are contract literals; the tests never recompute a product rule.
/// </summary>
public sealed class PersistenceValueContractTests
{
    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)2)]
    [InlineData((byte)3)]
    public void Identity_KeepsEachAccountSlotAndBothGuids_WithValueEquality(byte slotId)
    {
        PersistenceIdentity identity = new(slotId, AccountId, CharacterId);

        Assert.Equal(slotId, identity.SlotId);
        Assert.Equal(AccountId, identity.AccountId);
        Assert.Equal(CharacterId, identity.CharacterId);
        Assert.Equal(new PersistenceIdentity(slotId, AccountId, CharacterId), identity);
        Assert.NotEqual(new PersistenceIdentity(slotId, AccountId, OtherOwnerId), identity);
        Assert.NotEqual(new PersistenceIdentity(slotId, OtherOwnerId, CharacterId), identity);
    }

    [Fact]
    public void Identity_DistinguishesSlotsWithinTheSameAccount()
    {
        Assert.NotEqual(new PersistenceIdentity(1, AccountId, CharacterId), new PersistenceIdentity(2, AccountId, CharacterId));
        Assert.NotEqual(new PersistenceIdentity(2, AccountId, CharacterId), new PersistenceIdentity(3, AccountId, CharacterId));
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)4)]
    [InlineData((byte)255)]
    public void Identity_RejectsSlotsOutsideTheThreeAccountSlots(byte slotId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersistenceIdentity(slotId, AccountId, CharacterId));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Identity_RejectsEmptyGuid_WithoutGeneratingOne(bool emptyAccount, bool emptyCharacter)
    {
        Guid account = emptyAccount ? Guid.Empty : AccountId;
        Guid character = emptyCharacter ? Guid.Empty : CharacterId;

        Assert.ThrowsAny<ArgumentException>(() => new PersistenceIdentity(1, account, character));
    }

    [Fact]
    public void Authority_AcceptsFreeAndHeldShapes()
    {
        PersistenceAuthority free = new(Identity(), PersistenceOwnerKind.Free, null, 7, 0);
        PersistenceAuthority runtime = new(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, 8, 0);
        PersistenceAuthority recovery = new(Identity(), PersistenceOwnerKind.Recovery, RecoveryOwnerId, 9, 2);
        PersistenceAuthority largest = new(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, long.MaxValue, long.MaxValue);

        Assert.Equal(PersistenceOwnerKind.Free, free.OwnerKind);
        Assert.Null(free.OwnerId);
        Assert.Equal(7, free.Fence);
        Assert.Equal(0, free.LastSequence);
        Assert.Equal(RuntimeOwnerId, runtime.OwnerId);
        Assert.Equal(8, runtime.Fence);
        Assert.Equal(PersistenceOwnerKind.Recovery, recovery.OwnerKind);
        Assert.Equal(2, recovery.LastSequence);
        Assert.Equal(long.MaxValue, largest.Fence);
        Assert.Equal(long.MaxValue, largest.LastSequence);
        Assert.Equal(Identity(), runtime.Identity);
    }

    [Fact]
    public void Authority_FreeShape_RejectsOwnerOrNonZeroSequence()
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceAuthority(Identity(), PersistenceOwnerKind.Free, RuntimeOwnerId, 3, 0));
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceAuthority(Identity(), PersistenceOwnerKind.Free, null, 3, 1));
    }

    [Theory]
    [InlineData(PersistenceOwnerKind.Runtime)]
    [InlineData(PersistenceOwnerKind.Recovery)]
    public void Authority_HeldShape_RequiresOwnerAndPositiveFence(PersistenceOwnerKind kind)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceAuthority(Identity(), kind, null, 3, 0));
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceAuthority(Identity(), kind, Guid.Empty, 3, 0));
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceAuthority(Identity(), kind, RuntimeOwnerId, 0, 0));
    }

    [Fact]
    public void Authority_RejectsNegativeCountersUndefinedKindAndNullIdentity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersistenceAuthority(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersistenceAuthority(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, 4, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersistenceAuthority(Identity(), (PersistenceOwnerKind)3, RuntimeOwnerId, 4, 0));
        Assert.Throws<ArgumentNullException>(() => new PersistenceAuthority(null!, PersistenceOwnerKind.Free, null, 0, 0));
    }

    [Theory]
    [InlineData("same")]
    [InlineData("owner")]
    [InlineData("fence")]
    [InlineData("sequence")]
    [InlineData("kind")]
    public void Authority_ComparesOwnerFenceAndSequenceAsOneBundle(string differing)
    {
        PersistenceAuthority baseline = new(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, 5, 2);
        PersistenceAuthority other = differing switch
        {
            "owner" => new(Identity(), PersistenceOwnerKind.Runtime, OtherOwnerId, 5, 2),
            "fence" => new(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, 6, 2),
            "sequence" => new(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, 5, 3),
            "kind" => new(Identity(), PersistenceOwnerKind.Recovery, RuntimeOwnerId, 5, 2),
            _ => new(Identity(), PersistenceOwnerKind.Runtime, RuntimeOwnerId, 5, 2),
        };

        bool equal = baseline.Equals(other);

        Assert.Equal(differing == "same", equal);
    }

    [Fact]
    public void Token_CopiesInput_SoCallerMutationDoesNotLeakIn()
    {
        byte[] source = { 1, 2, 3, 4, 5, 6, 7, 8 };
        PersistenceToken token = new(source);

        source[0] = 0xFF;
        source[7] = 0xEE;

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, token.ToArray());
    }

    [Fact]
    public void Token_ReturnsFreshCopy_SoReturnedArrayMutationDoesNotLeakIn()
    {
        PersistenceToken token = new(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2 });

        byte[] first = token.ToArray();
        first[3] = 0;
        byte[] second = token.ToArray();

        Assert.NotSame(first, second);
        Assert.Equal(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2 }, second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(16)]
    public void Token_RequiresExactlyEightBytes(int length)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PersistenceToken(new byte[length]));
    }

    [Fact]
    public void Token_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new PersistenceToken(null!));
    }

    [Fact]
    public void Token_ZeroIsAValueDistinctFromAbsence_AndEqualityIsByBytes()
    {
        PersistenceToken? absent = null;
        PersistenceToken zero = new(new byte[8]);

        Assert.Null(absent);
        Assert.NotNull(zero);
        Assert.Equal(new byte[8], zero.ToArray());
        Assert.Equal(new PersistenceToken(new byte[8]), zero);
        Assert.Equal(new PersistenceToken(new byte[8]).GetHashCode(), zero.GetHashCode());
        Assert.NotEqual(Token(1), zero);
        Assert.Equal(Token(1), Token(1));
    }

    [Theory]
    [InlineData(CharacterClass.Knight, 150)]
    [InlineData(CharacterClass.Mage, 80)]
    public void SafeCheckpoint_IsTownFullHpLocked(CharacterClass characterClass, int maxHp)
    {
        SafeCheckpoint safe = new(characterClass, -12.5f, 3f, maxHp);

        Assert.Equal(characterClass, safe.CharacterClass);
        Assert.Equal(-12.5f, safe.TownX);
        Assert.Equal(3f, safe.TownY);
        Assert.Equal(maxHp, safe.MaxHp);
        Assert.Equal(maxHp, safe.Hp);
        Assert.False(safe.BossUnlocked);
    }

    [Fact]
    public void SafeCheckpoint_RejectsUndefinedClass()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SafeCheckpoint((CharacterClass)2, 0f, 0f, 150));
    }

    [Theory]
    [InlineData(float.NaN, 0f)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(float.NegativeInfinity, 0f)]
    [InlineData(0f, float.NaN)]
    [InlineData(0f, float.PositiveInfinity)]
    [InlineData(0f, float.NegativeInfinity)]
    public void SafeCheckpoint_RejectsNonFiniteCoordinates(float x, float y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SafeCheckpoint(CharacterClass.Knight, x, y, 150));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void SafeCheckpoint_RejectsNonPositiveMaxHp(int maxHp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SafeCheckpoint(CharacterClass.Knight, 0f, 0f, maxHp));
    }

    [Fact]
    public void SafeCheckpoint_OffersNoRawHpMapQuestOrUnlockInput()
    {
        ConstructorInfo[] constructors = typeof(SafeCheckpoint).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        ConstructorInfo only = Assert.Single(constructors);
        Assert.Equal(
            new[] { typeof(CharacterClass), typeof(float), typeof(float), typeof(int) },
            only.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
    }

    [Fact]
    public void SafeDefaults_KeepsBothClassCandidates()
    {
        SafeDefaults defaults = new(3.5f, -1f, 150, 80);

        Assert.Equal(3.5f, defaults.TownX);
        Assert.Equal(-1f, defaults.TownY);
        Assert.Equal(150, defaults.KnightMaxHp);
        Assert.Equal(80, defaults.MageMaxHp);
    }

    [Theory]
    [InlineData(float.NaN, 0f, 150, 80)]
    [InlineData(0f, float.PositiveInfinity, 150, 80)]
    [InlineData(0f, 0f, 0, 80)]
    [InlineData(0f, 0f, 150, -5)]
    public void SafeDefaults_RejectsNonFiniteOrNonPositiveValues(float x, float y, int knightMaxHp, int mageMaxHp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SafeDefaults(x, y, knightMaxHp, mageMaxHp));
    }

    [Fact]
    public void Deadline_UsesMonotonicTimestamp_NotWallClock()
    {
        ControllableTimeProvider clock = new();
        PersistenceDeadline deadline = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(10), deadline.GetRemaining());
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.FromSeconds(6), deadline.GetRemaining());
        clock.MoveWallClock(TimeSpan.FromHours(3));
        Assert.Equal(TimeSpan.FromSeconds(6), deadline.GetRemaining());
        clock.MoveWallClock(TimeSpan.FromHours(-6));
        Assert.Equal(TimeSpan.FromSeconds(6), deadline.GetRemaining());
        Assert.False(deadline.IsExpired);
    }

    [Fact]
    public void Deadline_ExpiresOnceAtTheAbsolutePoint_AndNeverGoesNegative()
    {
        ControllableTimeProvider clock = new();
        PersistenceDeadline deadline = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(2));

        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.True(deadline.IsExpired);
        Assert.Equal(TimeSpan.Zero, deadline.GetRemaining());
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(deadline.IsExpired);
        Assert.Equal(TimeSpan.Zero, deadline.GetRemaining());
    }

    [Fact]
    public void Deadline_RejectsNegativeOrInfiniteBudgetAndMissingClock()
    {
        ControllableTimeProvider clock = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => PersistenceDeadline.FromBudget(clock, TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PersistenceDeadline.FromBudget(clock, Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentNullException>(() => PersistenceDeadline.FromBudget(null!, TimeSpan.FromSeconds(1)));
    }

    public static TheoryData<Type> ImmutableContractTypes => new()
    {
        typeof(PersistenceIdentity),
        typeof(PersistenceAuthority),
        typeof(PersistenceToken),
        typeof(SafeCheckpoint),
        typeof(SafeDefaults),
        typeof(PersistenceDeadline),
        typeof(OperationIdentity),
        typeof(ReadAdmissionRequest),
        typeof(AcquireAndLoadRequest),
        typeof(WriteSafeCheckpointRequest),
        typeof(ReleaseRequest),
        typeof(ResolveOperationRequest),
        typeof(InspectRecoveryRequest),
        typeof(RecoverAndLoadRequest),
        typeof(ReleaseRecoveryRequest),
        typeof(ResolveRecoveryOperationRequest),
        typeof(PersistenceObservation),
        typeof(OperationReceipt),
        typeof(AdmissionProbe),
        typeof(ReadAdmissionResult),
        typeof(AcquireAndLoadResult),
        typeof(WriteSafeCheckpointResult),
        typeof(ReleaseResult),
        typeof(ResolveOperationResult),
    };

    // Structural guard that complements the alias tests above: a public setter or init accessor
    // would let a caller change an accepted request or a receipt after validation.
    [Theory]
    [MemberData(nameof(ImmutableContractTypes))]
    public void ContractTypes_ExposeNoPublicMutation(Type type)
    {
        string[] settable = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();
        string[] writableFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => !field.IsInitOnly)
            .Select(field => field.Name)
            .ToArray();

        Assert.True(type.IsSealed, $"{type.Name} must be sealed.");
        Assert.Empty(settable);
        Assert.Empty(writableFields);
    }
}
