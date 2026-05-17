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

        // 2) Phase 07: Physics.Step (Shared 단일 출처, 헌법 #1)에 위임.
        //    옛 Phase 04 단순 dx 코드 → jump + 중력 + ground clamp 통합.
        //    jumpPressed는 *에지* (D4 (a)) — 적용 후 즉시 false reset로 같은 tick 재점프 안전망.
        //    cheat가 매 frame jumpPressed=true 보내도 Physics.Step의 OnGround 검사로 무한 점프 차단.
        foreach (PlayerEntity p in _players)
        {
            PhysicsInput input = new PhysicsInput(
                p.PendingInputX, p.PendingJumpPressed, Constants.TickDuration);
            PhysicsState before = new PhysicsState(p.Position, p.Velocity, p.OnGround);
            PhysicsState after = Physics.Step(before, input);
            p.Position = after.Position;
            p.Velocity = after.Velocity;
            p.OnGround = after.OnGround;
            p.PendingInputX = 0;
            p.PendingJumpPressed = false;
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
                    vx = p.Velocity.X, // Phase 07 Step 3: 실제 velocity (prediction reconcile 정합)
                    vy = p.Velocity.Y,
                    serverTick = (int)tickNumber,
                    lastAckedClientTick = p.LastClientTick
                };
                // 본 Phase는 본인 1명만 — unicast. 다인은 M3+에서 broadcast로 확장.
                p.Owner.Send(pkt.Write());
            }
        }
    }
}
