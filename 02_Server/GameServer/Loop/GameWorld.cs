using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Loop;

// Phase 02 (M2): 서버 시뮬레이션의 "월드" 최상위.
// TickScheduler → OnTick → GameMap.Tick 콜백 체인.
//
// Phase 03: GameSession이 자기 GameMap을 찾아 AddPlayer 마샬링하려면
// 정적 접근점이 필요 → Instance singleton. 일회 설정(ctor에서) + 외부 set X
// (GameServer/CLAUDE.md "정적 mutable 게임 상태 금지"의 *mutable 금지* 정신 부합).
//
// M4.2 Phase 01: 단일 GameMap → Dictionary<MapId, GameMap> 맵 레지스트리 승격.
// 4맵(Town / HuntingGround / BossRoom / Ending)을 독립 actor로 관리.
// 매 틱 모든 맵 tick (foreach). GetMap(MapId)으로 맵 단건 조회.
// 호환용 Map 프로퍼티 = Town 반환 (플레이어 추적은 Phase 03에서 박힘).
public class GameWorld
{
    public static GameWorld Instance { get; private set; } = null!;

    // M4.2 Phase 01: readonly Dictionary — 외부 set 금지 (헌법: 정적 mutable 게임 상태 금지).
    // 4맵은 ctor에서 1회 생성 + 등록. 이후 추가/제거 X (Phase 03 맵간 이동도 맵 자체 내용 변경이지 레지스트리 변경 아님).
    readonly Dictionary<MapId, GameMap> _maps;

    readonly TickScheduler _scheduler;

    // M4.2 Phase 01: 호환용 프로퍼티. 기존 코드가 GameWorld.Instance?.Map으로 Town 맵을 반환하던 흐름 보존.
    // 플레이어가 "현재 어느 맵에 있는가" 추적은 Phase 03에서 도입.
    // GameSession.GetMap()이 임시로 이 프로퍼티를 통해 Town 맵을 반환하므로 플레이어는 여전히 Town에 spawn.
    public GameMap Map => _maps[MapId.Town];

    public long CurrentTick => _scheduler.CurrentTick;

    // Phase 08 Step 4: 통합 테스트 p99 검증용 (구독자가 OnMetricsSnapshot 받음).
    public TickScheduler Scheduler => _scheduler;

    public GameWorld()
    {
        // 첫 인스턴스 = 공식 singleton. 두 번째 생성은 테스트/오용 신호 → 예외.
        if (Instance != null!)
            throw new InvalidOperationException("GameWorld는 단일 인스턴스만 허용");
        Instance = this;

        // M4.2 Phase 01: 4맵 생성 + 등록.
        //
        // **맵별 콘텐츠 (MapSpawnTable 단일 진실 공급원 — M4.2 Phase 01 모듈화)**:
        //   Town          = 빈 맵 (플레이어 spawn 전용, enemy 0)
        //   HuntingGround = Normal enemy 1마리 (MapSpawnTable.GetSpawnsFor(HuntingGround))
        //   BossRoom      = Boss 1마리 (MapSpawnTable.GetSpawnsFor(BossRoom))
        //   Ending        = 빈 맵 (결과 화면 골격)
        //
        // **헌법 #5 정합**: ctor 동기 코드만. await/Task.Delay/Thread.Sleep 없음.
        // **entity id 풀**: 맵별 독립 (_nextEntityId 맵마다 1부터 시작). 전역 풀 vs 맵별 풀 trade-off는 Phase 03 결정.
        _maps = new Dictionary<MapId, GameMap>
        {
            { MapId.Town,           new GameMap(MapId.Town) },
            { MapId.HuntingGround,  new GameMap(MapId.HuntingGround) },
            { MapId.BossRoom,       new GameMap(MapId.BossRoom) },
            { MapId.Ending,         new GameMap(MapId.Ending) },
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

    // M4.2 Phase 01: MapId로 단건 맵 조회.
    // 없는 MapId를 요청하면 null 반환 (등록 안 된 맵 = 조용한 실패, 호출자가 null 체크 필요).
    // Phase 03에서 GameSession이 "플레이어가 현재 어느 맵"을 알 때 이 메서드로 조회.
    public GameMap? GetMap(MapId id)
        => _maps.TryGetValue(id, out GameMap? map) ? map : null;

    void OnTick(long tickNumber)
    {
        // M4.2 Phase 01: 단일 _map.Tick → 모든 맵 순차 tick.
        // **순서**: Dictionary 열거 순서는 삽입 순서 (C# Dictionary는 CPython 3.7+ 처럼 insertion-ordered X.
        //   단, ctor에서 고정 4개 삽입 + 테스트 환경에서 동일 순서로 일관성 유지됨).
        //   맵 간 tick 순서 의존 없음 — 현 Phase는 맵 간 통신 없어 순서 무관.
        // **헌법 #5 정합**: foreach 동기. await/Task.Delay/Thread.Sleep 없음.
        foreach (GameMap map in _maps.Values)
        {
            map.Tick(tickNumber);
        }
    }
}
