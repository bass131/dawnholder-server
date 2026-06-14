using System.Collections.Concurrent;

namespace Dawnholder.Server.GameServer.Party;

// 파티 전역 actor. GameWorld 소유 — cross-map이라 특정 맵/세션에 둘 수 없음.
//
// actor 패턴 = GameMap과 동일:
//   외부 thread → EnqueueJob(Action) → Tick()이 순서대로 드레인.
//   단일 thread 직렬화로 race 없음 — lock 사용 금지.
//
// PartyId 채번 = Interlocked.Increment (GameWorld.NextEntityId 패턴).
//   파티와 entity가 같은 전역 풀을 공유하지 않아도 되므로 별도 카운터 사용.
//   필요하면 GameWorld.NextEntityId 주입으로 교체 가능(생성자 overload 추가).
public sealed class PartyRegistry
{
    // 정원 2 고정 — PDL member0/member1 슬롯과 정합.
    public const int MaxPartySize = 2;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    // partyId → PartyState. tick thread에서만 읽기/쓰기.
    readonly Dictionary<int, PartyState> _parties = new();

    // entityId → partyId 역방향 인덱스. GetPartyByEntity O(1).
    readonly Dictionary<int, int> _entityToParty = new();

    // pending invite: targetEntityId → inviterEntityId. 피초대자 기준 키 = respond 시 O(1) 매칭.
    //   초대자가 보낸 invite를 피초대자가 응답할 때까지 보관. 응답(수락/거절) 시 소비(제거).
    //   **키를 target으로 잡은 이유**: respond 핸들러의 행위자(=피초대자=session._entityId)가 키 →
    //     "이 응답자에게 온 초대가 있나" O(1). 저장된 inviter와 패킷의 inviterEntityId 비교로
    //     A4가 위장/만료/race 검증을 확장(이번 happy엔 매칭 존재만 확인).
    //   **A4 확장 여지**: 값을 (inviterEntityId, 발급 tick) 구조체로 승격하면 타임아웃 추가 가능.
    //     이번엔 int 값으로 단순 유지 — append-only 확장 구조.
    readonly Dictionary<int, int> _pendingInvites = new();

    int _nextPartyId;

    // ── actor 인터페이스 (GameMap.EnqueueJob + Tick 패턴 mirror) ─────────────

    public void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    // GameWorld.OnTick이 매 틱 호출. 단일 thread 보장.
    public void Tick()
    {
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[PartyRegistry] job 예외: {ex.Message}"); }
        }
    }

    // ── 파티 조작 API (tick thread 안에서만 직접 호출 가능) ──────────────────

    // 외부 thread에서 호출 시 EnqueueJob 경유 필수.
    // 테스트는 Tick() 드레인 후 결과 확인.

    /// <summary>
    /// 2인 파티 생성. 생성 즉시 Members에 initiator + member 2명 등록.
    /// 이미 파티가 있는 경우 → null 반환(거부). 핸들러 단계(A3)에서 체크.
    /// </summary>
    public PartyState? CreateParty(int initiatorEntityId, int memberEntityId)
    {
        if (_entityToParty.ContainsKey(initiatorEntityId)) return null;
        if (_entityToParty.ContainsKey(memberEntityId)) return null;

        int id = Interlocked.Increment(ref _nextPartyId);
        PartyState party = new PartyState(id, initiatorEntityId);
        party.Members.Add(initiatorEntityId);
        party.Members.Add(memberEntityId);

        _parties[id] = party;
        _entityToParty[initiatorEntityId] = id;
        _entityToParty[memberEntityId] = id;

        return party;
    }

    /// <summary>
    /// 기존 파티에 멤버 추가. 정원(2) 초과 시 false 반환.
    /// </summary>
    public bool AddMember(int partyId, int entityId)
    {
        if (!_parties.TryGetValue(partyId, out PartyState? party)) return false;
        if (party.Members.Count >= MaxPartySize) return false;
        if (_entityToParty.ContainsKey(entityId)) return false;

        party.Members.Add(entityId);
        _entityToParty[entityId] = partyId;
        return true;
    }

    /// <summary>파티 해산. 멤버 역방향 인덱스도 제거.</summary>
    public bool Disband(int partyId)
    {
        if (!_parties.TryGetValue(partyId, out PartyState? party)) return false;

        foreach (int memberId in party.Members)
            _entityToParty.Remove(memberId);

        _parties.Remove(partyId);
        return true;
    }

    /// <summary>partyId → PartyState 조회.</summary>
    public PartyState? GetParty(int partyId)
        => _parties.TryGetValue(partyId, out PartyState? p) ? p : null;

    /// <summary>entityId → 소속 PartyState 역방향 조회. O(1).</summary>
    public PartyState? GetPartyByEntity(int entityId)
    {
        if (!_entityToParty.TryGetValue(entityId, out int partyId)) return null;
        return GetParty(partyId);
    }

    // ── pending invite (초대 → 응답 매칭) ─────────────────────────────────────

    /// <summary>
    /// 초대 기록. targetEntityId(피초대자) 기준으로 inviterEntityId를 보관.
    /// 같은 피초대자에게 새 초대가 오면 덮어쓴다(최신 초대 우선 — happy 단순화, 거절/race는 A4).
    /// </summary>
    public void RecordInvite(int inviterEntityId, int targetEntityId)
        => _pendingInvites[targetEntityId] = inviterEntityId;

    /// <summary>
    /// 피초대자(responderEntityId)에게 보류 중인 초대의 발신자를 조회. 없으면 false.
    /// A4가 inviter 일치/만료 검증을 여기에 확장. 이번엔 존재 여부만.
    /// </summary>
    public bool TryGetPendingInvite(int responderEntityId, out int inviterEntityId)
        => _pendingInvites.TryGetValue(responderEntityId, out inviterEntityId);

    /// <summary>응답(수락/거절) 처리 후 보류 초대를 소비(제거).</summary>
    public void ConsumeInvite(int responderEntityId)
        => _pendingInvites.Remove(responderEntityId);
}
