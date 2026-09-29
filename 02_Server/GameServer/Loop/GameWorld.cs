using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Sessions;
using System.Collections.Concurrent;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Loop;

// 서버 시뮬레이션의 "월드" 최상위. TickScheduler → OnTick → GameMap.Tick 콜백 체인.
//
// GameSession이 자기 GameMap을 찾아 AddPlayer 마샬링하려면 정적 접근점이 필요 → Instance singleton.
// 일회 설정(ctor에서) + 외부 set X (헌법 "정적 mutable 게임 상태 금지"의 *mutable 금지* 정신).
//
// Dictionary<MapId, GameMap> 맵 레지스트리: 4맵(Town / HuntingGround / BossRoom / Ending)을
// 독립 actor로 관리. 매 틱 모든 맵 tick (foreach). GetMap(MapId)으로 맵 단건 조회.
public class GameWorld
{
    static GameWorld? _instance;
    readonly ConcurrentQueue<GameSession> _pendingCloses = new();
    readonly ConcurrentQueue<TaskCompletionSource> _cleanupBarriers = new();
    // readonly Dictionary — 외부 set 금지 (헌법: 정적 mutable 게임 상태 금지).
    // 4맵은 ctor에서 1회 생성 + 등록. 이후 추가/제거 X (맵간 이동도 맵 내용 변경이지 레지스트리 변경 아님).
    readonly Dictionary<MapId, GameMap> _maps;

    // 전역 entity id 발급기 (ADR-026).
    //
    // **왜 전역 풀인가?**
    //   맵 간 이동 시 entity id를 유지(재배정 X) → S_MapTransition에 entityId 필드 불필요 (ADR-026).
    //   클라이언트 단순화 (id 교체 로직 0) + 미래 cheat-flag 추적 일관성 확보.
    //
    // **Interlocked.Increment 이유**:
    //   id 발급은 게임 상태 mutation이 아닌 "번호 뽑기" → Interlocked atomic으로 race 없이
    //   globally-unique id 발급. 맵별 게임 로직은 여전히 맵별 단일 thread로 격리됨.
    //   헌법 #5: Interlocked.Increment는 non-blocking lock-free — tick loop 내 허용.
    int _nextEntityId;

    readonly TickScheduler _scheduler;

    // 파티 전역 actor. cross-map이라 특정 맵/세션에 둘 수 없음 — GameWorld 소유.
    // 외부 → PartyRegistry.EnqueueJob → GameWorld.OnTick에서 드레인.
    readonly PartyRegistry _party;

    // Cross-map quest state, connected to copied membership and disband cleanup in the constructor.
    readonly QuestRegistry _quest;

    /// <summary>
    /// 맵별 terrain/content 쌍을 주입받는 생성자 — **필수 인자** (default 없음).
    /// <para>
    /// 프로덕션은 MapDataLoader.LoadAll() 산출 주입. 테스트는 인라인 구성 또는 빈 딕셔너리
    /// (디스크/저작 데이터 비종속). 필수로 박은 이유: 주입 누락이 "조용한 빈 월드"로
    /// 굴러가는 경로 차단 (fail loud).
    /// </para>
    /// </summary>
    public GameWorld(IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));

        // 첫 인스턴스 = 공식 singleton. 두 번째 생성은 테스트/오용 신호 → 예외.
        if (Instance != null!)
            throw new InvalidOperationException("GameWorld는 단일 인스턴스만 허용");

        // Both registries are ready before map callbacks can enqueue work.
        _party = new PartyRegistry(OnPartyDisbanded);
        _quest = new QuestRegistry(_party.GetMembershipByEntity);

        // 4맵 생성 + 등록. provider가 있으면 맵별 terrain/content 주입.
        //
        // **생성 순서 결정론적 고정**: Town → HuntingGround → BossRoom → Ending.
        //   ctor 순서가 id 발급 순서를 결정. enemy 1마리당 id 1개 소비.
        _maps = new Dictionary<MapId, GameMap>
        {
            { MapId.Town,          MakeMap(MapId.Town,          provider) },
            { MapId.HuntingGround, MakeMap(MapId.HuntingGround, provider) },
            { MapId.BossRoom,      MakeMap(MapId.BossRoom,      provider) },
            { MapId.Ending,        MakeMap(MapId.Ending,        provider) },
        };

        _scheduler = new TickScheduler(OnTick);
        if (Interlocked.CompareExchange(ref _instance, this, null) != null)
            throw new InvalidOperationException("GameWorld는 단일 인스턴스만 허용");
    }

    public static GameWorld Instance => Volatile.Read(ref _instance)!;

    // 호환용 프로퍼티 — GameWorld.Instance?.Map으로 Town 맵을 반환하던 흐름 보존.
    public GameMap Map => _maps[MapId.Town];

    // 파티 전역 actor 접근점.
    public PartyRegistry Party => _party;

    // 퀘스트 전역 actor 접근점 (M7.6 P01 — Party에서 분리).
    public QuestRegistry Quest => _quest;

    public long CurrentTick => _scheduler.CurrentTick;

    public TickScheduler Scheduler => _scheduler;

    /// <summary>
    /// 전역 entity id 발급. 각 GameMap이 ctor에서 주입받는 Func&lt;int&gt;.
    /// Interlocked.Increment atomic — 두 맵이 동시 호출해도 같은 id 발급 X.
    /// </summary>
    public int NextEntityId() => Interlocked.Increment(ref _nextEntityId);

    public void Start() => _scheduler.Start();

    public void Stop(TimeSpan? timeout = null)
    {
        _scheduler.Stop(timeout);
        // 테스트에서 다시 생성 가능하도록 instance 해제.
        Interlocked.CompareExchange(ref _instance, null, this);
    }

    // 세션 종료 요청 제출을 끝낸 호출자가 틱 밖에서 기다리는 barrier.
    public Task FlushSessionClosuresAsync()
    {
        if (_scheduler.IsTickThread)
            throw new InvalidOperationException("틱에서 cleanup 완료를 기다릴 수 없습니다.");
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _cleanupBarriers.Enqueue(completion);
        return completion.Task;
    }

    // 없는 MapId를 요청하면 null 반환 (등록 안 된 맵 = 조용한 실패, 호출자가 null 체크 필요).
    public GameMap? GetMap(MapId id)
        => _maps.TryGetValue(id, out GameMap? map) ? map : null;

    /// <summary>
    /// entityId를 보유한 맵을 찾아 그 맵의 EnqueueJob 경유로 payload를 송신한다.
    ///
    /// **thread 안전 보장**: 대상 맵의 EnqueueJob(람다)을 통해 그 맵의 tick thread 위에서
    ///   session.Send를 호출한다. 직접 session.Send를 부르면 맵 tick thread를 침범(race) —
    ///   반드시 이 경로를 사용해야 한다 (헌법 §5, Map=Actor 원칙).
    ///
    /// entityId가 어느 맵에도 없으면(로그아웃/전환 중) silent 무시 (예외 X).
    /// 맵 4개 순회 = O(4) 상수 — entityId→MapId 역인덱스 추가 시 동기화 부채(MapMigration 등)가
    ///   늘어나므로 현 규모에서 순회가 더 단순하고 안전하다.
    /// </summary>
    public void SendToEntity(int entityId, ArraySegment<byte> payload)
    {
        foreach (GameMap map in _maps.Values)
        {
            PlayerEntity? player = map.GetPlayer(entityId);
            if (player == null) continue;

            // 대상 맵의 tick thread 위에서 송신 — 직접 호출이면 이 맵(GameWorld) thread가
            // 대상 맵 내부를 침범. EnqueueJob으로 대상 맵 thread에 마샬링.
            GameMap captured = map;
            captured.EnqueueJob(() =>
            {
                PlayerEntity? p = captured.GetPlayer(entityId);
                if (p?.Owner == null || p.Owner.IsClosing) return;
                p.Owner.Send(payload);
            });
            return;
        }
        // entityId 없음 = 오프라인 또는 맵 전환 중 — silent 무시.
    }

    /// <summary>
    /// entityId를 보유한 맵에서 그 플레이어의 CharacterClass를 byte로 조회한다.
    ///
    /// **헌법 #1 (Server Authority)**: 클래스는 서버 권위 PlayerStats에서만 읽는다 — 클라가 보낸 값 X.
    /// 파티 패킷(S_PartyInviteRecv.inviterClass / S_PartyUpdate.memberNClass)을 채울 때
    /// PartyRegistry job(tick thread)이 호출. entityId가 어느 맵에도 없으면 false 반환.
    ///
    /// 맵 4개 순회 = O(4) 상수 — SendToEntity와 동일 패턴(entityId→MapId 역인덱스 추가 시
    /// 동기화 부채가 늘어나므로 현 규모에선 순회가 더 단순·안전).
    /// </summary>
    public bool TryGetEntityClass(int entityId, out byte characterClass)
    {
        foreach (GameMap map in _maps.Values)
        {
            PlayerEntity? player = map.GetPlayer(entityId);
            if (player == null) continue;
            characterClass = (byte)player.Stats.Class;
            return true;
        }
        characterClass = 0;
        return false;
    }

    internal void RequestSessionClose(GameSession session) => _pendingCloses.Enqueue(session);

    internal bool IsActiveSession(GameSession session, int entityId)
        => !session.IsClosing && _maps.Values.Any(map => ReferenceEquals(map.GetPlayer(entityId)?.Owner, session));

    internal bool IsActiveEntity(int entityId)
        => _maps.Values.Any(map => map.GetPlayer(entityId)?.Owner is { IsClosing: false });

#if DEBUG
    // Called within the session's existing guarded quest job; do not enqueue a second job.
    internal void CompleteQuestForDebug(int entityId)
        => QuestNotifier.Send(this, _quest.DebugCompleteQuest(entityId));
#endif

    void DrainSessionCloses()
    {
        while (_pendingCloses.TryDequeue(out GameSession? session))
        {
            int entityId = session.EntityId;
            List<(GameMap Map, int EntityId)> removed = new();
            // 모든 맵은 같은 월드 틱에서 실행된다. 소켓의 라우팅 힌트를 신뢰하지 않는다.
            foreach (GameMap map in _maps.Values)
            {
                foreach (PlayerEntity player in map.Players)
                {
                    if (ReferenceEquals(player.Owner, session))
                        removed.Add((map, player.EntityId));
                }
                map.RemovePlayerBySession(session);
            }
            if (entityId >= 0) PartyFlow.CleanupOnDisconnect(this, entityId);
            session.CompleteWorldLeave();
            foreach (var entry in removed)
                entry.Map.BroadcastToAll(new S_PlayerLeave { entityId = entry.EntityId }.Write(), except: session);
        }
    }

    void OnPartyDisbanded(int partyId)
        => _quest.EnqueueJob(() => _quest.ForgetPartyProgress(partyId));

    GameMap MakeMap(MapId id,
        IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> provider)
    {
        // 킬 콜백: Boss 킬 → 전역 리셋, 그 외 → OnKill 적립.
        //   EnqueueJob 마샬링: solo/party 진행 변경과 통보를 기존 Quest job 안에서 처리.
        //   맵 Tick과 Quest.Tick은 같은 틱 스레드에서 순차 실행(GameWorld.OnTick).
        //   미래 맵 멀티스레드화 대비 방어적 — 현재는 0~1틱 지연만 발생.
        Action<int, EnemyEntity> onKill = (killerId, target) =>
            _quest.EnqueueJob(() =>
            {
                if (EnemyCatalog.For(target.Kind).IsBoss)
                    _quest.ResetAllQuestProgress();
                else
                    QuestNotifier.Send(this, _quest.OnKill(killerId));
            });

        if (provider.TryGetValue(id, out var pair))
            return new GameMap(id, NextEntityId, pair.Terrain, pair.Content, onKill);
        return new GameMap(id, NextEntityId, onEnemyKilled: onKill);
    }

    void OnTick(long tickNumber)
    {
        List<TaskCompletionSource> barriers = new();
        while (_cleanupBarriers.TryDequeue(out TaskCompletionSource? barrier)) barriers.Add(barrier);
        try
        {
            DrainSessionCloses();
            foreach (GameMap map in _maps.Values)
                map.Tick(tickNumber);
            DrainSessionCloses();

            Party.Tick(tickNumber);
            Quest.Tick(tickNumber);
            foreach (TaskCompletionSource completion in barriers) completion.TrySetResult();
        }
        catch (Exception ex)
        {
            foreach (TaskCompletionSource completion in barriers) completion.TrySetException(ex);
            throw;
        }
    }
}
