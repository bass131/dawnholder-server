using System.Collections.Concurrent;
using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 단일 GameMap actor. 단일 thread Tick → lock 없음.
// Phase 03: IOCP→tick 마샬링 ConcurrentQueue + AddPlayer/RemovePlayer.
// Phase 04: intent 적용 + 매 SnapshotTickInterval(=5) tick마다 S_Snapshot 브로드캐스트.
//
// ARCHITECTURE "Map = Actor": 한 맵의 모든 mutation을 단일 thread에 가두면
// 동시성 버그의 90%가 사라진다. 외부 → 자기 위치 마샬링.
public class GameMap
{
    readonly List<PlayerEntity> _players = new();
    int _nextEntityId = 1;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    public IReadOnlyList<PlayerEntity> Players => _players;

    public void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    // tick thread에서만 호출.
    public PlayerEntity AddPlayer(GameSession? owner, Vector2 spawnPos)
    {
        PlayerEntity entity = new PlayerEntity(_nextEntityId++, spawnPos, owner);
        _players.Add(entity);
        return entity;
    }

    // tick thread에서만 호출.
    public bool RemovePlayer(int entityId)
        => _players.RemoveAll(p => p.EntityId == entityId) > 0;

    public PlayerEntity? GetPlayer(int entityId)
        => _players.Find(p => p.EntityId == entityId);

    /// <summary>
    /// TickScheduler가 매 50ms마다 호출. 단일 thread.
    /// </summary>
    public void Tick(long tickNumber)
    {
        // 1) 외부 thread가 push한 job들 처리 (AddPlayer/RemovePlayer/SetPendingInputX 등).
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[Map] job 예외: {ex.Message}"); }
        }

        // 2) 각 player의 pending intent를 적용 후 0으로 리셋.
        //    클라가 계속 누르면 매 tick 새 intent가 도착해 덮어씀.
        //    *prediction 없음* — snapshot이 도착해야 클라 화면이 움직임 (Phase 04 의도).
        foreach (PlayerEntity p in _players)
        {
            if (p.PendingInputX != 0)
            {
                float dx = p.PendingInputX * Constants.MoveSpeed * Constants.TickDuration;
                p.Position = new Vector2(p.Position.X + dx, p.Position.Y);
            }
            p.PendingInputX = 0;
        }

        // 3) Snapshot 브로드캐스트. 매 5 tick(=250ms).
        //    헌법 #3 (Trust Boundary): 좌표는 *서버가 정한 것만* 전송. 클라 보고 받지 않음.
        if (tickNumber % Constants.SnapshotTickInterval == 0)
        {
            foreach (PlayerEntity p in _players)
            {
                if (p.Owner == null) continue;
                S_Snapshot pkt = new S_Snapshot
                {
                    entityId = p.EntityId,
                    x = p.Position.X,
                    y = p.Position.Y,
                    serverTick = (int)tickNumber,
                    lastAckedClientTick = p.LastClientTick
                };
                // 본 Phase는 본인 1명만 — unicast. 다인은 M3+에서 broadcast로 확장.
                p.Owner.Send(pkt.Write());
            }
        }
    }
}
