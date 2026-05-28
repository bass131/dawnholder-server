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

    // M4.2 Phase 02: 전역 entity id 발급기 (ADR-026).
    //
    // **왜 전역 풀인가?**
    //   맵 간 이동 시 entity id를 유지(재배정 X) → S_MapTransition에 entityId 필드 불필요 (ADR-026).
    //   클라이언트 단순화 (id 교체 로직 0) + 미래 cheat-flag 추적 일관성 확보.
    //
    // **Interlocked.Increment 이유**:
    //   id 발급은 게임 상태 mutation이 아님 — "번호 뽑기"일 뿐 (ADR-026 명시).
    //   GameMap tick thread가 AllocId()를 호출하는 시점이 맵마다 다를 수 있으나
    //   Interlocked가 atomic 보장 → race 없이 globally-unique id 발급.
    //   맵별 모든 게임 로직(이동/전투/spawn)은 여전히 맵별 단일 thread로 격리됨.
    //
    // **헌법 #5**: Interlocked.Increment는 non-blocking lock-free — tick loop 내 허용.
    int _nextEntityId;

    /// <summary>
    /// M4.2 Phase 02: 전역 entity id 발급. 각 GameMap이 ctor에서 주입받는 Func&lt;int&gt;.
    ///
    /// <para>
    /// 단조 증가 보장: Interlocked.Increment(post-increment) → 1, 2, 3, ...
    /// 멀티스레드 안전: Interlocked.Increment는 atomic — 두 맵이 동시 호출해도 같은 id 발급 X.
    /// </para>
    /// </summary>
    public int NextEntityId() => Interlocked.Increment(ref _nextEntityId);

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
        // M4.2 Phase 02: idAllocator = NextEntityId 주입 → 전역 풀 (ADR-026).
        //
        // **생성 순서 결정론적 고정**: Town → HuntingGround → BossRoom → Ending.
        //   ctor 순서가 id 발급 순서를 결정. Town=빈맵(enemy 0) → id 소비 0.
        //   HuntingGround: Normal enemy 1마리 → id=1 소비.
        //   BossRoom: Boss 1마리 → id=2 소비.
        //   Ending=빈맵(enemy 0) → id 소비 0.
        //   → 플레이어가 Town 진입 시 AddPlayer → id=3 (첫 번째 player id).
        //
        // **테스트 회귀 분석**:
        //   GameMapContentTests: new GameMap(MapId.HuntingGround) 단독 생성 (idAllocator=null)
        //     → 로컬 카운터 1부터 시작 → Normal enemy id=1 기대값 *변경 없음*.
        //   AttackHandlerTests/BossStageClearTests: new GameMap(MapId.HuntingGround) 단독 생성
        //     → EnemyEntityId=1, BossEntityId=2, PlayerEntityId=3 *변경 없음*.
        //   GameWorldRegistryTests: GameWorld 경유 → 전역 풀 적용.
        //     맵 등록 수(4) + MapId 조회만 검증 — id 숫자 직접 검증 없음 → 회귀 없음.
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
