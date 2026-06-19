using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Party;

namespace Dawnholder.Server.GameServer.Quest;

// 퀘스트 전역 actor. GameWorld 소유 — cross-map(보스 해금)이라 특정 맵/세션에 둘 수 없음.
//
// **분리 근거** (M7.6 P01): 퀘스트 진행 상태(보스 해금)가 Party 도메인에 굳어 있으면
//   M8 영속화 시 퀘스트가 파티 테이블로 새는 오염 → 깨끗한 퀘스트 경계 확보.
//
// actor 패턴 = PartyRegistry/GameMap과 동일:
//   외부 thread → EnqueueJob(Action) → Tick()이 순서대로 드레인.
//   단일 thread 직렬화로 race 없음 — lock 사용 금지.
//
// **PartyRegistry 단방향 의존** (사이클 0):
//   생성자 주입(QuestRegistry(PartyRegistry party)). 파티 멤버십/공유 카운트가 필요한
//   지점(GetPartyByEntity, PartyState.KillCount 읽기/쓰기)만 _party 경유 호출.
//   PartyRegistry는 QuestRegistry를 **절대 모름** — 역방향 의존 0.
//
// **동일-스레드 불변식** (★미래 멀티스레드화 재검토 1순위, P01 권고 3):
//   Quest.Tick()과 Party.Tick()은 같은 GameWorld 틱 스레드에서 순차 실행
//   (GameWorld.OnTick: Party.Tick → Quest.Tick). QuestRegistry가 PartyState를
//   *읽고 쓰는*(party.KillCount++ / = target / = 0 변경 포함) 건 이 동일-스레드 보장
//   하에서만 안전하다. *Quest가 Party 소유 데이터를 변경*하는 교차-actor 쓰기라,
//   미래 맵 멀티스레드화 시 가장 먼저 재검토해야 할 결합 지점.
public sealed class QuestRegistry
{
    readonly ConcurrentQueue<Action> _pendingJobs = new();

    // 파티 멤버십/공유 카운트 조회용 단방향 의존. 생성자 주입.
    readonly PartyRegistry _party;

    // 파티 없는 솔로 플레이어의 퀘스트 킬카운트. entityId → count.
    // tick thread에서만 읽기/쓰기 — lock 없음 (actor 불변식).
    readonly Dictionary<int, int> _soloProgress = new();

    // 보스 포탈 영구 해금 latch. 한 번 임계 킬 달성한 entityId 보관 — 이후 리셋(보스 킬)에도 유지.
    // 영호 요청: 퀘스트 달성 후 재그라인드 금지(양방향 포탈 왕복 시 매번 재달성 X). tick thread 전용.
    // 세션 한정(서버 재시작 시 비움) — entityId는 연결마다 신규라 disconnect 잔여는 무해.
    readonly HashSet<int> _bossUnlocked = new();

    public QuestRegistry(PartyRegistry party)
    {
        _party = party ?? throw new ArgumentNullException(nameof(party));
    }

    // ── actor 인터페이스 (PartyRegistry.EnqueueJob + Tick 패턴 mirror) ──────────

    public void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    // GameWorld.OnTick이 매 틱 호출 — Party.Tick **다음에** 호출(퀘스트가 파티 상태를 읽으므로).
    // 단일 thread 보장. currentTick = 시그니처 통일용(현재 퀘스트는 만료 판정 없음).
    public void Tick(long currentTick)
    {
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[QuestRegistry] job 예외: {ex.Message}"); }
        }
    }

    // ── 퀘스트 킬카운트 API (tick thread invariant) ───────────────────────────

    /// <summary>
    /// 적 1킬을 killerEntityId에게 적립. QuestRegistry tick thread 안에서만 직접 호출 가능.
    /// 외부(소켓 thread) 경유 시 GameWorld.MakeMap에서 주입된 onEnemyKilled 콜백이
    /// EnqueueJob으로 마샬링한 후 호출 — 직접 호출 금지.
    ///
    /// 파티 멤버십·공유 카운트는 _party 경유 조회/변경. 동일-스레드 불변식 하에서만 안전.
    /// </summary>
    public void OnKill(int killerEntityId, GameWorld world)
    {
        // tick thread invariant: _soloProgress 직접 접근 + _party 읽기/쓰기 안전(동일 스레드).
        PartyState? party = _party.GetPartyByEntity(killerEntityId);
        if (party != null)
        {
            party.KillCount++;
            if (party.KillCount >= QuestConstants.BossUnlockKillCount)
                foreach (int memberId in party.Members) _bossUnlocked.Add(memberId);

            // 임계 도달 후 표시는 N/target에서 멈춤(초과 누적 숨김) — 해금=영구라 더 셀 의미 없음.
            int shown = Math.Min(party.KillCount, QuestConstants.BossUnlockKillCount);
            foreach (int memberId in party.Members)
                PartyNotifier.SendQuestUpdate(world, memberId, shown, QuestConstants.BossUnlockKillCount);
        }
        else
        {
            _soloProgress.TryGetValue(killerEntityId, out int prev);
            int newCount = prev + 1;
            _soloProgress[killerEntityId] = newCount;
            if (newCount >= QuestConstants.BossUnlockKillCount)
                _bossUnlocked.Add(killerEntityId);

            int shown = Math.Min(newCount, QuestConstants.BossUnlockKillCount);
            PartyNotifier.SendQuestUpdate(world, killerEntityId, shown, QuestConstants.BossUnlockKillCount);
        }
    }

#if DEBUG
    /// <summary>
    /// [시연 디버그 치트] 호출자의 퀘스트를 즉시 완료 — killCount를 임계로 채우고 보스 영구 해금.
    /// 유일 호출자=GameSession.SubmitCheatCommand(둘 다 #if DEBUG) — Release에는 부재(치트 사슬
    /// 빌드타임 봉합, 헌법 #3 / SN-02). EnqueueJob 경유 호출로 서버 권위 유지.
    /// OnKill 임계 도달 분기와 동형 — 파티면 공유 카운트, 솔로면 _soloProgress.
    /// </summary>
    public void DebugCompleteQuest(int entityId, GameWorld world)
    {
        int target = QuestConstants.BossUnlockKillCount;
        PartyState? party = _party.GetPartyByEntity(entityId);
        if (party != null)
        {
            party.KillCount = target;
            foreach (int memberId in party.Members)
            {
                _bossUnlocked.Add(memberId);
                PartyNotifier.SendQuestUpdate(world, memberId, target, target);
            }
        }
        else
        {
            _soloProgress[entityId] = target;
            _bossUnlocked.Add(entityId);
            PartyNotifier.SendQuestUpdate(world, entityId, target, target);
        }
    }
#endif

    /// <summary>
    /// 모든 퀘스트 진행상황 초기화. 보스 킬 시 GameWorld.MakeMap 콜백이 EnqueueJob 경유로 호출.
    /// MVP 전역 리셋 — 다중 파티 월드로 확장 시 killer 파티/솔로만 리셋하는 정밀화 필요.
    /// 현재 2인 MVP는 파티가 고정 1개이므로 전역 OK.
    /// </summary>
    public void ResetAllQuestProgress()
    {
        // _bossUnlocked는 의도적으로 비우지 않음 — 영구 해금 유지(보스 킬 후 재그라인드 방지, 영호 요청).
        _soloProgress.Clear();
        // 파티 공유 카운트(PartyState.KillCount)는 PartyState에 잔류(depth-B) — _party 경유로 0 리셋.
        foreach (PartyState p in _party.GetAllParties())
            p.KillCount = 0;
    }

    /// <summary>
    /// 보스 게이트용 killCount 통합 조회. 파티면 공유(PartyState.KillCount), 솔로면 _soloProgress.
    ///
    /// tick thread invariant — MapMigration.Execute(맵 tick thread) 안에서 읽힌다.
    /// Q2 OnKill 적립처(파티 KillCount / _soloProgress)와 정확히 정합.
    /// </summary>
    public int GetKillCount(int entityId)
    {
        // 영구 해금 latch: 한 번 임계 달성 시 이후 리셋(보스 킬 등)에도 게이트 통과 유지(영호 요청).
        // 게이트(MapMigration)는 이 값만 보므로 게이트 코드 변경 0 — latch는 서버 권위 OnKill만 세팅.
        if (_bossUnlocked.Contains(entityId)) return QuestConstants.BossUnlockKillCount;

        PartyState? party = _party.GetPartyByEntity(entityId);
        return party != null ? party.KillCount : GetSoloProgress(entityId);
    }

    /// <summary>솔로 진행상황 조회 — 테스트 관측용.</summary>
    internal int GetSoloProgress(int entityId)
        => _soloProgress.TryGetValue(entityId, out int v) ? v : 0;

    /// <summary>보스 포탈 영구 해금 여부 — 테스트 관측용.</summary>
    internal bool IsBossUnlocked(int entityId) => _bossUnlocked.Contains(entityId);
}
