using Dawnholder.Server.GameServer.Maps;

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
    public static GameWorld Instance { get; private set; } = null!;

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

    /// <summary>
    /// 전역 entity id 발급. 각 GameMap이 ctor에서 주입받는 Func&lt;int&gt;.
    /// Interlocked.Increment atomic — 두 맵이 동시 호출해도 같은 id 발급 X.
    /// </summary>
    public int NextEntityId() => Interlocked.Increment(ref _nextEntityId);

    readonly TickScheduler _scheduler;

    // 호환용 프로퍼티 — GameWorld.Instance?.Map으로 Town 맵을 반환하던 흐름 보존.
    public GameMap Map => _maps[MapId.Town];

    public long CurrentTick => _scheduler.CurrentTick;

    public TickScheduler Scheduler => _scheduler;

    public GameWorld()
    {
        // 첫 인스턴스 = 공식 singleton. 두 번째 생성은 테스트/오용 신호 → 예외.
        if (Instance != null!)
            throw new InvalidOperationException("GameWorld는 단일 인스턴스만 허용");
        Instance = this;

        // 4맵 생성 + 등록. 맵별 콘텐츠는 MapSpawnTable 단일 진실 공급원이 결정.
        // idAllocator = NextEntityId 주입 → 전역 풀 (ADR-026).
        //
        // **생성 순서 결정론적 고정**: Town → HuntingGround → BossRoom → Ending.
        //   ctor 순서가 id 발급 순서를 결정. enemy 1마리당 id 1개 소비 →
        //   HuntingGround Normal=1, BossRoom Boss=2, 첫 player(Town 진입)=3.
        _maps = new Dictionary<MapId, GameMap>
        {
            { MapId.Town,           new GameMap(MapId.Town,          NextEntityId) },
            { MapId.HuntingGround,  new GameMap(MapId.HuntingGround, NextEntityId) },
            { MapId.BossRoom,       new GameMap(MapId.BossRoom,      NextEntityId) },
            { MapId.Ending,         new GameMap(MapId.Ending,        NextEntityId) },
        };

        _scheduler = new TickScheduler(OnTick);
    }

    public void Start() => _scheduler.Start();

    public void Stop()
    {
        _scheduler.Stop();
        // 테스트에서 다시 생성 가능하도록 instance 해제.
        if (Instance == this) Instance = null!;
    }

    // 없는 MapId를 요청하면 null 반환 (등록 안 된 맵 = 조용한 실패, 호출자가 null 체크 필요).
    public GameMap? GetMap(MapId id)
        => _maps.TryGetValue(id, out GameMap? map) ? map : null;

    void OnTick(long tickNumber)
    {
        // 모든 맵 순차 tick. 맵 간 tick 순서 의존 없음 (맵 간 통신 없어 순서 무관).
        foreach (GameMap map in _maps.Values)
        {
            map.Tick(tickNumber);
        }
    }
}
