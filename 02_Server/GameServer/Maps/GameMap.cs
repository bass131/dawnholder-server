using System.Collections.Concurrent;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 단일 GameMap actor. 단일 thread Tick → lock 없음.
// Phase 03: IOCP→tick 마샬링 ConcurrentQueue + AddPlayer/RemovePlayer.
// Phase 04: intent 적용 + 매 SnapshotTickInterval(=5) tick마다 S_Snapshot 브로드캐스트.
// M3 Phase 06 Step 2 (응급 전투): `_enemies` Dictionary 분리 보관 +
//   ctor에서 Normal enemy 1마리 spawn(맵 중간 zone 고정 위치). entity id는 player와
//   공유 풀(`_nextEntityId`)에서 발급 — collision 방지 + S_HitResult.targetEntityId 라우팅
//   단순화 (player/enemy 구분 없이 GetById로 찾을 수 있음, Step 3+에서 활용).
//
// ARCHITECTURE "Map = Actor": 한 맵의 모든 mutation을 단일 thread에 가두면
// 동시성 버그의 90%가 사라진다. 외부 → 자기 위치 마샬링.
public class GameMap
{
    readonly List<PlayerEntity> _players = new();
    // M3 Phase 06 Step 2: enemy 보관소. player와 *분리* — broadcast 대상은 players만 (enemy는
    // owner session X). 같은 entity id 풀에서 발급해 id collision 차단.
    readonly Dictionary<int, EnemyEntity> _enemies = new();
    int _nextEntityId = 1;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    public IReadOnlyList<PlayerEntity> Players => _players;

    // M3 Phase 06 Step 2: 읽기 전용 노출. Step 3에서 EnterGameWorld가 active enemy roster
    // 다발 전송(S_EntitySpawn) 시 순회용 + Step 5 AttackHandler가 target lookup 보조용.
    public IReadOnlyDictionary<int, EnemyEntity> Enemies => _enemies;

    // M3 Phase 06 Step 2 (응급 전투): 단일 맵 3-zone trick (좌=마을 / 중=전투 / 우=보스).
    // ground y=0 가정(Physics.cs 정의 정합) + player spawn (0,0)에서 우측으로 충분히 떨어진
    // 위치로 박음. 진짜 zone 경계 좌표는 클라(Phase 08b)에서 시각화 박힘 — 서버는 위치만 정의.
    // `MoveSpeed = 5 units/sec`이므로 (10, 0)은 정상 도보 2초 거리 = 시연 흐름 자연.
    public const float NormalEnemySpawnX = 10f;
    public const float NormalEnemySpawnY = 0f;
    public const int NormalEnemyMaxHp = 30;

    public GameMap()
    {
        // M3 Phase 06 Step 2: 서버 시작 시 Normal enemy 1마리 즉시 spawn.
        // 응급 단순화 — respawn 없음, AI 없음, 고정 위치. Step 3에서 신규 client 접속 시
        // 본 enemy를 S_EntitySpawn으로 다발 전송 (initial roster 패턴, Phase 04 정합).
        //
        // 헌법 #5 (틱 블로킹 금지) 정합: ctor는 tick 진입 전이라 동기 코드 OK. await 없음.
        SpawnNormalEnemy(NormalEnemySpawnX, NormalEnemySpawnY, NormalEnemyMaxHp);
    }

    // M3 Phase 06 Step 2: tick thread (또는 ctor) 에서만 호출 invariant.
    // 헌법 #5 — 동기 코드만, await/Task.Delay/Thread.Sleep 금지.
    EnemyEntity SpawnNormalEnemy(float x, float y, int maxHp)
    {
        int id = _nextEntityId++;
        EnemyEntity e = new EnemyEntity(id, EnemyKind.Normal, x, y, maxHp);
        _enemies.Add(id, e);
        return e;
    }

    // virtual: 테스트 subclass에서 EnqueueJob 호출 카운트 추적 가능 (Phase 09 rate-limit drop 검증).
    public virtual void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

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

    // Phase 10 (M2.5 lifecycle race): owner reference 기반 cleanup.
    // entityId가 아직 -1인 race window에서도 안전 — AddPlayer가 같은 batch에 들어왔으면
    // 그 entity도 owner 일치로 제거됨. tick thread에서만 호출 (단일 thread invariant).
    // 멱등: owner가 없으면 false 반환, 두 번 호출 안전.
    public bool RemovePlayerBySession(GameSession owner)
        => _players.RemoveAll(p => ReferenceEquals(p.Owner, owner)) > 0;

    public PlayerEntity? GetPlayer(int entityId)
        => _players.Find(p => p.EntityId == entityId);

    /// <summary>
    /// M3 Phase 06 Step 3 (netcode 인계 — Step 5 AttackHandler 사전 작업):
    /// enemy entityId 단건 lookup. 없으면 null.
    ///
    /// **사용 의도**: Step 5 AttackHandler에서 target validation 묶음 처리
    /// (`GetEnemyById(id)` → null이면 silent drop, `IsDead`면 idempotent no-op).
    /// `GetPlayer(id)` 시그니처 정합 (둘 다 entityId 단건 lookup, 같은 entity id 풀 사용).
    ///
    /// **호출 invariant**: tick thread에서만 (단일 thread invariant 유지).
    /// </summary>
    internal EnemyEntity? GetEnemyById(int entityId)
        => _enemies.TryGetValue(entityId, out EnemyEntity? e) ? e : null;

    /// <summary>
    /// M3 Phase 04 (헌법 #3 정합 + Phase 10 lifecycle race 패턴 일반화): 같은 맵 전원에게 packet 전송.
    ///
    /// **호출 invariant**: tick thread에서만. EnqueueJob 람다 안 또는 Tick 안에서.
    ///
    /// **skip 규칙**:
    ///   - owner == null (테스트용 entity)
    ///   - owner == except (발신자 자기 자신 제외 — broadcast except self 패턴)
    ///   - owner.IsClosing (Phase 10 lifecycle race 재발 봉합 — disconnect 중인 세션에 Send X)
    ///
    /// **N² 비용 인지**: 응급 모드 데모(N≤4) 환경에선 무시 가능 (250ms마다 16 패킷 = 64/s).
    /// M4+ 다인 환경에선 S_Snapshot 배열 형태 + PDL 도구 확장 필요.
    /// </summary>
    public void BroadcastToAll(ArraySegment<byte> payload, GameSession? except = null)
    {
        foreach (PlayerEntity p in _players)
        {
            if (p.Owner == null) continue;
            if (ReferenceEquals(p.Owner, except)) continue;
            if (p.Owner.IsClosing) continue; // race 안전망
            p.Owner.Send(payload);
        }
    }

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
        //    **M3 Phase 04**: 각 entity별 packet을 *전원에게* broadcast (자기 자신 포함).
        //      - 자기 entity packet: reconcile 진입 (lastAckedClientTick 본인 것만 의미)
        //      - 남 entity packet: remote view 업데이트 (lastAckedClientTick은 무시 권장 — 클라 책임 Phase 05)
        //      N² 비용 인지 (응급 모드 데모 N≤4 환경 무시 가능 / M4+ 배열 형태 PDL 확장)
        if (tickNumber % Constants.SnapshotTickInterval == 0)
        {
            foreach (PlayerEntity p in _players)
            {
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
                BroadcastToAll(pkt.Write()); // 자기 자신 포함 전원
            }
        }
    }
}
