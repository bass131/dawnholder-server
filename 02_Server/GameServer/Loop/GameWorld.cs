using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Loop;

// Phase 02 (M2): 서버 시뮬레이션의 "월드" 최상위.
// TickScheduler → OnTick → GameMap.Tick 콜백 체인.
//
// Phase 03: GameSession이 자기 GameMap을 찾아 AddPlayer 마샬링하려면
// 정적 접근점이 필요 → Instance singleton. 일회 설정(ctor에서) + 외부 set X
// (GameServer/CLAUDE.md "정적 mutable 게임 상태 금지"의 *mutable 금지* 정신 부합).
public class GameWorld
{
    public static GameWorld Instance { get; private set; } = null!;

    readonly GameMap _map = new();
    readonly TickScheduler _scheduler;

    public GameMap Map => _map;
    public long CurrentTick => _scheduler.CurrentTick;

    public GameWorld()
    {
        // 첫 인스턴스 = 공식 singleton. 두 번째 생성은 테스트/오용 신호 → 예외.
        if (Instance != null!)
            throw new InvalidOperationException("GameWorld는 단일 인스턴스만 허용");
        Instance = this;

        _scheduler = new TickScheduler(OnTick);
    }

    public void Start() => _scheduler.Start();

    public void Stop()
    {
        _scheduler.Stop();
        // 테스트에서 다시 생성 가능하도록 instance 해제.
        if (Instance == this) Instance = null!;
    }

    void OnTick(long tickNumber)
    {
        _map.Tick(tickNumber);
    }
}
