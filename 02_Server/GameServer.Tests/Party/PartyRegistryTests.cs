using Dawnholder.Server.GameServer.Party;

namespace Dawnholder.Server.GameServer.Tests.Party;

// PartyRegistry actor 단위 테스트.
//
// 검증 범위:
//   - CreateParty: 멤버 2명 + 리더 정확
//   - 정원 2 invariant: AddMember로 3번째 추가 거부
//   - PartyId 단조 증가 (Interlocked.Increment)
//   - Disband 후 lookup 실패
//   - entityId → party 역방향 lookup 정확
//   - actor 패턴: EnqueueJob 후 Tick 드레인 전에는 반영 X, 드레인 후 반영
//
// 모든 직접 API 호출(CreateParty/Disband 등)은 tick thread에서 호출한다고 가정.
// actor EnqueueJob 테스트만 Tick 드레인 경유.
public class PartyRegistryTests
{
    readonly PartyRegistry _registry = new();

    // ── CreateParty ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateParty_Returns_PartyState_With_Two_Members_And_Leader()
    {
        PartyState? party = _registry.CreateParty(initiatorEntityId: 10, memberEntityId: 20);

        Assert.NotNull(party);
        Assert.Equal(10, party!.LeaderEntityId);
        Assert.Equal(2, party.Members.Count);
        Assert.Contains(10, party.Members);
        Assert.Contains(20, party.Members);
    }

    [Fact]
    public void CreateParty_Returns_NonNull_PartyId()
    {
        PartyState? party = _registry.CreateParty(1, 2);

        Assert.NotNull(party);
        Assert.True(party!.PartyId > 0);
    }

    // ── PartyId 단조 증가 ────────────────────────────────────────────────────

    [Fact]
    public void PartyId_IsMonotonicallyIncreasing()
    {
        PartyState? a = _registry.CreateParty(1, 2);
        PartyState? b = _registry.CreateParty(3, 4);
        PartyState? c = _registry.CreateParty(5, 6);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotNull(c);
        Assert.True(a!.PartyId < b!.PartyId, $"a.PartyId={a.PartyId} must be < b.PartyId={b.PartyId}");
        Assert.True(b.PartyId < c!.PartyId, $"b.PartyId={b.PartyId} must be < c.PartyId={c.PartyId}");
    }

    // ── 정원 2 invariant ────────────────────────────────────────────────────

    [Fact]
    public void AddMember_ThirdMember_IsRejected()
    {
        // CreateParty로 이미 2명(10, 20). 3번째(30) 추가 시도.
        PartyState? party = _registry.CreateParty(10, 20);
        Assert.NotNull(party);

        bool result = _registry.AddMember(party!.PartyId, entityId: 30);

        Assert.False(result);
        Assert.Equal(2, party.Members.Count);
    }

    [Fact]
    public void AddMember_AlreadyInParty_IsRejected()
    {
        // entity 10이 이미 다른 파티에 속해 있으면 AddMember 거부.
        _registry.CreateParty(10, 20);  // entity 10, 20 → party A

        // 새 파티(30, 40) 생성 후 10 추가 시도 → 거부.
        PartyState? partyB = _registry.CreateParty(30, 40);
        Assert.NotNull(partyB);

        bool result = _registry.AddMember(partyB!.PartyId, entityId: 10);

        Assert.False(result);
    }

    // ── Disband ───────────────────────────────────────────────────────────

    [Fact]
    public void Disband_RemovesParty_And_LookupFails()
    {
        PartyState? party = _registry.CreateParty(10, 20);
        Assert.NotNull(party);

        bool disbanded = _registry.Disband(party!.PartyId);

        Assert.True(disbanded);
        Assert.Null(_registry.GetParty(party.PartyId));
    }

    [Fact]
    public void Disband_Clears_EntityIndex()
    {
        PartyState? party = _registry.CreateParty(10, 20);
        Assert.NotNull(party);

        _registry.Disband(party!.PartyId);

        // 해산 후 entityId 역방향 lookup도 null이어야 함.
        Assert.Null(_registry.GetPartyByEntity(10));
        Assert.Null(_registry.GetPartyByEntity(20));
    }

    [Fact]
    public void Disband_NonExistentParty_ReturnsFalse()
    {
        bool result = _registry.Disband(partyId: 9999);
        Assert.False(result);
    }

    // ── entityId → party lookup ─────────────────────────────────────────

    [Fact]
    public void GetPartyByEntity_Returns_Correct_Party()
    {
        PartyState? party = _registry.CreateParty(10, 20);
        Assert.NotNull(party);

        PartyState? foundByLeader = _registry.GetPartyByEntity(10);
        PartyState? foundByMember = _registry.GetPartyByEntity(20);

        Assert.NotNull(foundByLeader);
        Assert.NotNull(foundByMember);
        Assert.Equal(party!.PartyId, foundByLeader!.PartyId);
        Assert.Equal(party.PartyId, foundByMember!.PartyId);
    }

    [Fact]
    public void GetPartyByEntity_UnknownEntity_ReturnsNull()
    {
        PartyState? result = _registry.GetPartyByEntity(entityId: 9999);
        Assert.Null(result);
    }

    // ── CreateParty — 이미 파티 중인 entity 거부 ─────────────────────────

    [Fact]
    public void CreateParty_WithExistingPartyMember_ReturnsNull()
    {
        _registry.CreateParty(10, 20); // entity 10, 20 → 파티 보유

        // entity 10이 initiator로 새 파티 시도 → 거부.
        PartyState? duplicate = _registry.CreateParty(10, 30);
        Assert.Null(duplicate);
    }

    // ── actor 패턴: EnqueueJob → Tick 드레인 후 반영 ─────────────────────

    [Fact]
    public void EnqueueJob_Mutation_NotVisible_Before_Tick()
    {
        // EnqueueJob 후 Tick 전에는 파티가 존재하지 않아야 함.
        PartyState? captured = null;

        _registry.EnqueueJob(() =>
        {
            captured = _registry.CreateParty(10, 20);
        });

        // Tick 드레인 전: job이 아직 실행되지 않음.
        Assert.Null(captured);
    }

    [Fact]
    public void EnqueueJob_Mutation_IsVisible_After_Tick()
    {
        PartyState? captured = null;

        _registry.EnqueueJob(() =>
        {
            captured = _registry.CreateParty(10, 20);
        });

        _registry.Tick(currentTick: 1); // job 드레인

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Members.Count);
        // GetPartyByEntity도 드레인 후 정확해야 함.
        Assert.NotNull(_registry.GetPartyByEntity(10));
        Assert.NotNull(_registry.GetPartyByEntity(20));
    }

    [Fact]
    public void MultipleEnqueuedJobs_ExecuteInOrder_After_Tick()
    {
        List<int> order = new();

        _registry.EnqueueJob(() => order.Add(1));
        _registry.EnqueueJob(() => order.Add(2));
        _registry.EnqueueJob(() => order.Add(3));

        _registry.Tick(currentTick: 1);

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }
}
