using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Tests.Persistence;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuLifetimeProbe;

namespace Dawnholder.Server.GameServer.Tests.Network;

// server-request-contract-v1.md §2. No test-local pending/high-water/generation implementation.
// The reflection driver asserts product presence; all state transitions execute on that object.
public sealed class CharacterMenuRequestLifetimeTests
{
    [Fact]
    public void ProductApi_HasTheFixedBeginTicketAndLifetimeOperations()
        => _ = new CharacterMenuLifetimeProbe();

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void ValidBegin_PreservesIdKindPositiveGenerationAndExactDeadline(ushort id)
    {
        CharacterMenuLifetimeProbe lifetime = new();
        PersistenceDeadline deadline = Budget(new());
        object ticket = Started(lifetime.Begin(0x12345678, id, deadline));
        Assert.Equal(0x12345678u, Read<uint>(ticket, "RequestId"));
        Assert.Equal(id, Read<ushort>(ticket, "RequestPacketId"));
        Assert.True(Read<long>(ticket, "Generation") > 0);
        Assert.Same(deadline, Read<PersistenceDeadline>(ticket, "Deadline"));
        Assert.True(lifetime.IsCurrent(ticket));
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)48)]
    [InlineData((ushort)50)]
    [InlineData((ushort)52)]
    [InlineData((ushort)54)]
    [InlineData(ushort.MaxValue)]
    public void UnsupportedKind_IsIgnoredWithoutAdvancingHighWater(ushort id)
    {
        CharacterMenuLifetimeProbe lifetime = new();
        PersistenceDeadline deadline = Budget(new());
        WithoutTicket("Ignored", lifetime.Begin(uint.MaxValue, id, deadline));
        object ticket = Started(lifetime.Begin(1, 49, deadline));
        Assert.True(lifetime.IsCurrent(ticket));
        WithoutTicket("Ignored", lifetime.Begin(uint.MaxValue, id, deadline));
        Assert.True(lifetime.IsCurrent(ticket));
        Assert.True(lifetime.Complete(ticket));
        Assert.True(lifetime.IsCurrent(Started(lifetime.Begin(2, 53, deadline))));
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void ZeroId_IsIgnoredWithAndWithoutPending(ushort id)
    {
        CharacterMenuLifetimeProbe lifetime = new();
        PersistenceDeadline deadline = Budget(new());
        WithoutTicket("Ignored", lifetime.Begin(0, id, deadline));
        object first = Started(lifetime.Begin(1, 49, deadline));
        WithoutTicket("Ignored", lifetime.Begin(0, id, deadline));
        Assert.True(lifetime.IsCurrent(first));
        Assert.True(lifetime.Complete(first));
        Assert.True(lifetime.IsCurrent(Started(lifetime.Begin(2, id, deadline))));
    }

    [Fact]
    public void NullDeadline_ThrowsAndLeavesHighWaterAndOriginalPendingUntouched()
    {
        CharacterMenuLifetimeProbe lifetime = new();
        Assert.Throws<ArgumentNullException>(() => lifetime.Begin(uint.MaxValue, 49, null));
        PersistenceDeadline deadline = Budget(new());
        object first = Started(lifetime.Begin(1, 49, deadline));
        Assert.Throws<ArgumentNullException>(() => lifetime.Begin(uint.MaxValue, 51, null));
        Assert.True(lifetime.IsCurrent(first));
        Assert.Same(deadline, Read<PersistenceDeadline>(first, "Deadline"));
        Assert.True(lifetime.Complete(first));
        Assert.True(lifetime.IsCurrent(Started(lifetime.Begin(2, 53, deadline))));
    }

    [Theory]
    [InlineData((ushort)49, (ushort)51)]
    [InlineData((ushort)51, (ushort)53)]
    [InlineData((ushort)53, (ushort)49)]
    public void HigherPendingRequest_IsConsumedWithoutReplacingTheOriginalTicket(ushort firstKind, ushort rejectedKind)
    {
        CharacterMenuLifetimeProbe lifetime = new();
        ControllableTimeProvider clock = new();
        PersistenceDeadline originalDeadline = Budget(clock);
        object original = Started(lifetime.Begin(10, firstKind, originalDeadline));
        long generation = Read<long>(original, "Generation");
        clock.Advance(TimeSpan.FromSeconds(2));
        PersistenceDeadline laterDeadline = Budget(clock);
        WithoutTicket("InProgress", lifetime.Begin(30, rejectedKind, laterDeadline));
        WithoutTicket("Ignored", lifetime.Begin(30, firstKind, laterDeadline));
        WithoutTicket("Ignored", lifetime.Begin(20, 53, laterDeadline));
        WithoutTicket("Ignored", lifetime.Begin(10, rejectedKind, laterDeadline));
        Assert.True(lifetime.IsCurrent(original));
        Assert.Equal(10u, Read<uint>(original, "RequestId"));
        Assert.Equal(firstKind, Read<ushort>(original, "RequestPacketId"));
        Assert.Equal(generation, Read<long>(original, "Generation"));
        Assert.Same(originalDeadline, Read<PersistenceDeadline>(original, "Deadline"));
        Assert.Equal(TimeSpan.FromSeconds(3), originalDeadline.GetRemaining());
        Assert.True(lifetime.Complete(original));
        Assert.False(lifetime.Complete(original));
        WithoutTicket("Ignored", lifetime.Begin(30, rejectedKind, laterDeadline));
        WithoutTicket("Ignored", lifetime.Begin(29, firstKind, laterDeadline));
        object next = Started(lifetime.Begin(31, rejectedKind, laterDeadline));
        Assert.NotSame(original, next);
        Assert.True(Read<long>(next, "Generation") > generation);
        Assert.Equal(31u, Read<uint>(next, "RequestId"));
        Assert.Same(laterDeadline, Read<PersistenceDeadline>(next, "Deadline"));
        Assert.False(lifetime.IsCurrent(original));
        Assert.False(lifetime.Complete(original));
        Assert.True(lifetime.IsCurrent(next));
    }

    [Fact]
    public void CompletedIdsRemainIgnored_AndOnlyHigherIdsCreateNewGenerations()
    {
        CharacterMenuLifetimeProbe lifetime = new();
        PersistenceDeadline deadline = Budget(new());
        object first = Started(lifetime.Begin(100, 49, deadline));
        long firstGeneration = Read<long>(first, "Generation");
        Assert.True(lifetime.Complete(first));
        Assert.False(lifetime.IsCurrent(first));
        foreach (ushort kind in new ushort[] { 49, 51, 53 })
        {
            WithoutTicket("Ignored", lifetime.Begin(100, kind, deadline));
            WithoutTicket("Ignored", lifetime.Begin(99, kind, deadline));
        }
        object next = Started(lifetime.Begin(101, 51, deadline));
        Assert.True(Read<long>(next, "Generation") > firstGeneration);
        Assert.False(lifetime.Complete(first));
        Assert.True(lifetime.IsCurrent(next));
        Assert.True(lifetime.Complete(next));
        Assert.False(lifetime.Complete(next));
    }

    [Fact]
    public void AnotherLifetimeTicketWithSameNumbersCannotCompleteOrReplacePending()
    {
        CharacterMenuLifetimeProbe first = new();
        CharacterMenuLifetimeProbe second = new();
        PersistenceDeadline deadline = Budget(new());
        object firstTicket = Started(first.Begin(1, 49, deadline));
        object secondTicket = Started(second.Begin(1, 49, deadline));
        Assert.NotSame(firstTicket, secondTicket);
        Assert.Equal(Read<uint>(firstTicket, "RequestId"), Read<uint>(secondTicket, "RequestId"));
        Assert.Equal(Read<long>(firstTicket, "Generation"), Read<long>(secondTicket, "Generation"));
        Assert.False(first.IsCurrent(secondTicket));
        Assert.False(first.Complete(secondTicket));
        Assert.False(second.IsCurrent(firstTicket));
        Assert.False(second.Complete(firstTicket));
        Assert.True(first.IsCurrent(firstTicket));
        Assert.True(second.IsCurrent(secondTicket));
        Assert.True(first.Complete(firstTicket));
        Assert.True(second.IsCurrent(secondTicket));
    }

    [Fact]
    public void CloseIsPermanentAndIdempotent_LateCompletionCannotAffectAnotherLifetime()
    {
        CharacterMenuLifetimeProbe old = new();
        PersistenceDeadline deadline = Budget(new());
        object oldTicket = Started(old.Begin(1, 49, deadline));
        old.Close();
        old.Close();
        Assert.False(old.IsCurrent(oldTicket));
        Assert.False(old.Complete(oldTicket));
        WithoutTicket("Closed", old.Begin(2, 51, deadline));
        WithoutTicket("Closed", old.Begin(uint.MaxValue, 53, deadline));
        CharacterMenuLifetimeProbe fresh = new();
        object freshTicket = Started(fresh.Begin(1, 49, deadline));
        Assert.False(fresh.IsCurrent(oldTicket));
        Assert.False(fresh.Complete(oldTicket));
        Assert.False(old.Complete(freshTicket));
        old.Close();
        Assert.True(fresh.IsCurrent(freshTicket));
        Assert.True(fresh.Complete(freshTicket));
    }

    [Fact]
    public void CloseWithoutPendingStillCannotBeReopened()
    {
        CharacterMenuLifetimeProbe lifetime = new();
        lifetime.Close();
        lifetime.Close();
        WithoutTicket("Closed", lifetime.Begin(1, 49, Budget(new())));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConsumingUIntMaxValueNeverWraps(bool consumedByPendingRejection)
    {
        CharacterMenuLifetimeProbe lifetime = new();
        PersistenceDeadline deadline = Budget(new());
        object ticket;
        if (consumedByPendingRejection)
        {
            ticket = Started(lifetime.Begin(uint.MaxValue - 1, 49, deadline));
            WithoutTicket("InProgress", lifetime.Begin(uint.MaxValue, 51, deadline));
            Assert.True(lifetime.IsCurrent(ticket));
        }
        else
        {
            ticket = Started(lifetime.Begin(uint.MaxValue, 53, deadline));
        }
        Assert.True(lifetime.Complete(ticket));
        WithoutTicket("Ignored", lifetime.Begin(uint.MaxValue, 49, deadline));
        WithoutTicket("Ignored", lifetime.Begin(1, 51, deadline));
        WithoutTicket("Ignored", lifetime.Begin(uint.MaxValue - 1, 53, deadline));
    }

    [Fact]
    public void OriginalDeadlineAgesThroughRejectionAndCompletion_ExpiryDoesNotAutoComplete()
    {
        CharacterMenuLifetimeProbe lifetime = new();
        ControllableTimeProvider clock = new();
        PersistenceDeadline original = Budget(clock);
        object ticket = Started(lifetime.Begin(1, 49, original));
        Assert.Equal(TimeSpan.FromSeconds(5), original.GetRemaining());
        clock.Advance(TimeSpan.FromSeconds(4));
        clock.MoveWallClock(TimeSpan.FromDays(-10));
        WithoutTicket("InProgress", lifetime.Begin(2, 51, Budget(clock)));
        Assert.Same(original, Read<PersistenceDeadline>(ticket, "Deadline"));
        Assert.Equal(TimeSpan.FromSeconds(1), original.GetRemaining());
        Assert.False(original.IsExpired);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(original.IsExpired);
        Assert.Equal(TimeSpan.Zero, original.GetRemaining());
        Assert.True(lifetime.IsCurrent(ticket), "timeout settlement belongs to the owner, not the lifetime clock");
        WithoutTicket("InProgress", lifetime.Begin(3, 53, Budget(clock)));
        Assert.True(lifetime.IsCurrent(ticket));
        Assert.Same(original, Read<PersistenceDeadline>(ticket, "Deadline"));
        Assert.True(lifetime.Complete(ticket));
        Assert.Same(original, Read<PersistenceDeadline>(ticket, "Deadline"));
        Assert.Equal(TimeSpan.Zero, original.GetRemaining());
        Assert.False(lifetime.Complete(ticket));
        WithoutTicket("Ignored", lifetime.Begin(3, 49, Budget(clock)));
        object next = Started(lifetime.Begin(4, 51, original));
        Assert.Same(original, Read<PersistenceDeadline>(next, "Deadline"));
        Assert.Equal(TimeSpan.Zero, Read<PersistenceDeadline>(next, "Deadline").GetRemaining());
        Assert.True(lifetime.IsCurrent(next));
    }

    static PersistenceDeadline Budget(ControllableTimeProvider clock)
        => PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));

    static object Started(BeginObservation result)
    {
        Assert.Equal("Started", result.Status);
        Assert.NotNull(result.Ticket);
        return result.Ticket!;
    }

    static void WithoutTicket(string expectedStatus, BeginObservation result)
    {
        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.Ticket);
    }
}
