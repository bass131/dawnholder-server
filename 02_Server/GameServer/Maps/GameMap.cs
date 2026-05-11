using System.Collections.Concurrent;
using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 단일 GameMap actor. 모든 PlayerEntity가 이 안에 살고,
// Tick() 호출은 단일 thread에서만 — *lock 없음*이 actor 패턴의 핵심.
//
// Phase 03 (M2) 확장: IOCP 스레드(GameSession.OnConnected/Disconnected)에서
// AddPlayer/RemovePlayer를 직접 호출하면 tick thread와 경합. 대신
// ConcurrentQueue<Action>으로 마샬링 → Tick 시작에 drain한다.
//
// ServerCore의 JobQueue는 *첫 Push 스레드가 flush*하는 패턴이라 IOCP→tick 마샬링
// 용도엔 부적합(IOCP 스레드가 그대로 실행). 따라서 GameMap 전용 큐를 둠.
public class GameMap
{
    readonly List<PlayerEntity> _players = new();
    int _nextEntityId = 1;

    // 외부(IOCP 등) 스레드 → tick thread 마샬링 큐.
    readonly ConcurrentQueue<Action> _pendingJobs = new();

    public IReadOnlyList<PlayerEntity> Players => _players;

    /// <summary>
    /// 외부 스레드에서 호출. job은 다음 Tick 시작에 tick thread에서 실행됨.
    /// GameMap의 모든 mutation은 이 경로로만 들어와야 한다 (actor 패턴 강제).
    /// </summary>
    public void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    /// <summary>
    /// tick thread에서만 호출. job 안에서 AddPlayer/RemovePlayer 같은
    /// mutation을 안전하게 수행.
    /// </summary>
    public PlayerEntity AddPlayer(GameSession? owner, Vector2 spawnPos)
    {
        PlayerEntity entity = new PlayerEntity(_nextEntityId++, spawnPos, owner);
        _players.Add(entity);
        return entity;
    }

    /// <summary>
    /// tick thread에서만 호출.
    /// </summary>
    public bool RemovePlayer(int entityId)
        => _players.RemoveAll(p => p.EntityId == entityId) > 0;

    /// <summary>
    /// TickScheduler가 매 50ms마다 호출. 단일 thread.
    /// </summary>
    public void Tick(long tickNumber)
    {
        // 외부 스레드가 push한 job들을 tick thread에서 처리.
        // 한 tick에 너무 많은 job이 쌓이면 tick duration 폭증 가능 — Phase 02 메트릭이 잡아냄.
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex)
            {
                Console.WriteLine($"[Map] pending job 예외: {ex.Message}");
            }
        }

        // Phase 04+: 각 player의 _pendingIntent를 적용해 Position 갱신.
    }
}
