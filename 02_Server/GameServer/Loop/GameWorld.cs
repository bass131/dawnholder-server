using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Loop;

// Phase 02 (M2): 서버 시뮬레이션의 "월드" 최상위 객체.
// TickScheduler가 부르는 OnTick을 받아 GameMap.Tick으로 forward.
// 이번 Phase는 단일 GameMap 하드코딩. 다중 맵은 M3+에서 GameWorld가
// 맵 레지스트리를 갖는 형태로 확장.
public class GameWorld
{
    readonly GameMap _map = new();
    readonly TickScheduler _scheduler;

    public GameMap Map => _map;
    public long CurrentTick => _scheduler.CurrentTick;

    public GameWorld()
    {
        _scheduler = new TickScheduler(OnTick);
    }

    public void Start() => _scheduler.Start();
    public void Stop() => _scheduler.Stop();

    void OnTick(long tickNumber)
    {
        _map.Tick(tickNumber);
    }
}
