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
}
