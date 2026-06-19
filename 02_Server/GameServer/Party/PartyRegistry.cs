using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Entities;

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

    // 초대 만료(timeout): 발급 후 이 tick 수가 지나면 Tick 드레인에서 청소.
    //   600 tick = 30초 (20 TPS). 시간 = tick 카운트 — DateTime/Task.Delay 금지(헌법 §5).
    //   PDL/Shared 관심사가 아닌 서버 actor 내부 정책이라 여기 const로 둠.
    public const int InviteTimeoutTicks = 600;

    // S_PartyError reason 코드(PDL byte와 정합, A0).
    //   0=상대없음, 1=이미파티중, 2=자기자신, 3=정원초과.
    public const byte ErrorTargetMissing = 0;
    public const byte ErrorAlreadyInParty = 1;
    public const byte ErrorSelfInvite = 2;
    public const byte ErrorPartyFull = 3;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    // partyId → PartyState. tick thread에서만 읽기/쓰기.
    readonly Dictionary<int, PartyState> _parties = new();

    // entityId → partyId 역방향 인덱스. GetPartyByEntity O(1).
    readonly Dictionary<int, int> _entityToParty = new();

    // pending invite: targetEntityId → (inviterEntityId, 발급 tick). 피초대자 기준 키 = respond 시 O(1) 매칭.
    //   초대자가 보낸 invite를 피초대자가 응답할 때까지 보관. 응답(수락/거절) 시 소비(제거).
    //   **키를 target으로 잡은 이유**: respond 핸들러의 행위자(=피초대자=session._entityId)가 키 →
    //     "이 응답자에게 온 초대가 있나" O(1). 저장된 inviter와 패킷의 inviterEntityId 비교로
    //     A4가 위장(claimed≠pending)/만료(IssuedTick)/race를 거부.
    //   **A4 승격**: 값을 int → PendingInvite(inviter, 발급 tick) 구조체로 승격 — 타임아웃 추가.
    //     append-only 확장(키/매칭 의미 보존, 만료 정보만 부착).
    readonly Dictionary<int, PendingInvite> _pendingInvites = new();

    int _nextPartyId;

    // ── actor 인터페이스 (GameMap.EnqueueJob + Tick 패턴 mirror) ─────────────

    public void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    // GameWorld.OnTick이 매 틱 호출. 단일 thread 보장.
    // currentTick = 만료 판정 기준(시간 = tick 카운트, 헌법 §5).
    public void Tick(long currentTick)
    {
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[PartyRegistry] job 예외: {ex.Message}"); }
        }

        ExpireStaleInvites(currentTick);
    }

    // 발급 후 InviteTimeoutTicks 경과한 보류 초대를 제거. 만료 후 respond는 매칭 실패 → silent.
    //   메모리 누수 방지 + "오래된 초대를 지금 수락" 이상 동작 차단(학습 포인트).
    void ExpireStaleInvites(long currentTick)
    {
        if (_pendingInvites.Count == 0) return; // 흔한 경로 빠른 탈출(매 틱 호출)

        List<int>? expired = null;
        foreach (KeyValuePair<int, PendingInvite> kv in _pendingInvites)
        {
            if (currentTick - kv.Value.IssuedTick >= InviteTimeoutTicks)
                (expired ??= new()).Add(kv.Key);
        }

        if (expired == null) return;
        foreach (int targetId in expired)
            _pendingInvites.Remove(targetId);
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

    /// <summary>
    /// 현재 등록된 모든 파티 순회 — QuestRegistry.ResetAllQuestProgress가 공유 KillCount를
    /// 0으로 리셋할 때 사용(depth-B: KillCount는 PartyState 잔류, 리셋만 Quest 책임).
    /// tick thread invariant — 단일 thread에서만 열거 안전(actor 불변식).
    /// 메서드 형태 = 이 클래스 조회 API(GetParty/GetPartyByEntity)와 일관(StyleCop 순서 정합).
    /// </summary>
    public IEnumerable<PartyState> GetAllParties() => _parties.Values;

    // ── pending invite (초대 → 응답 매칭) ─────────────────────────────────────

    /// <summary>
    /// 초대 기록. targetEntityId(피초대자) 기준으로 (inviterEntityId, 발급 tick)을 보관.
    /// 같은 피초대자에게 새 초대가 오면 덮어쓴다(최신 초대 우선). issuedTick은 만료 판정용.
    /// </summary>
    public void RecordInvite(int inviterEntityId, int targetEntityId, long issuedTick)
        => _pendingInvites[targetEntityId] = new PendingInvite(inviterEntityId, issuedTick);

    /// <summary>
    /// 피초대자(responderEntityId)에게 보류 중인 초대의 발신자를 조회. 없으면(미존재/만료 청소됨) false.
    /// 호출자(SubmitPartyRespond)가 claimedInviter와 inviterEntityId를 비교해 위장 거부.
    /// </summary>
    public bool TryGetPendingInvite(int responderEntityId, out int inviterEntityId)
    {
        if (_pendingInvites.TryGetValue(responderEntityId, out PendingInvite invite))
        {
            inviterEntityId = invite.InviterEntityId;
            return true;
        }
        inviterEntityId = 0;
        return false;
    }

    /// <summary>응답(수락/거절) 처리 후 보류 초대를 소비(제거).</summary>
    public void ConsumeInvite(int responderEntityId)
        => _pendingInvites.Remove(responderEntityId);

    /// <summary>
    /// disconnect 정리: entityId가 얽힌 보류 초대를 양방향 제거.
    ///   - 피초대자 키(entityId): 이 entity가 받은 초대.
    ///   - 발신자 값(InviterEntityId == entityId): 이 entity가 보낸 초대(피초대자가 응답 전).
    /// 끊긴 entity가 유령 초대로 남지 않게 청소. tick thread(EnqueueJob 람다)에서만 호출.
    /// </summary>
    public void RemoveInvitesInvolving(int entityId)
    {
        _pendingInvites.Remove(entityId); // 받은 초대

        List<int>? sentToTargets = null;
        foreach (KeyValuePair<int, PendingInvite> kv in _pendingInvites)
        {
            if (kv.Value.InviterEntityId == entityId)
                (sentToTargets ??= new()).Add(kv.Key);
        }
        if (sentToTargets == null) return;
        foreach (int targetId in sentToTargets)
            _pendingInvites.Remove(targetId);
    }

    // pending invite 1건: 발신자 + 발급 tick. 발급 tick으로 만료 판정.
    readonly record struct PendingInvite(int InviterEntityId, long IssuedTick);
}
