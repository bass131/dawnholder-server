using System.Collections.Concurrent;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 단일 GameMap actor. 단일 thread Tick → lock 없음.
// Phase 03: IOCP→tick 마샬링 ConcurrentQueue + AddPlayer/RemovePlayer.
// Phase 04: intent 적용 + 매 SnapshotTickInterval(=2) tick마다 S_Snapshot 브로드캐스트.
// M3 Phase 06 Step 2 (응급 전투): `_enemies` Dictionary 분리 보관.
//
// M4.3R Phase 03 (§2.2 컨테이너 + System 분리):
//   CombatSystem / EnemyAISystem / RespawnSystem 3개로 로직 추출.
//   GameMap = 상태(_players/_enemies/_pendingJobs/AllocId) + Tick 엔진 + actor 경계만 잔류.
//   Tick에서 System 호출 순서 명문화: physics → CombatSystem(EnqueueJob 경유) → EnemyAISystem → RespawnSystem.
//
// **살아있는 적만 _enemies** invariant:
//   EnemyEntity는 사망하면 _enemies에서 즉시 Remove (CombatSystem.ProcessAttack 경유).
//   죽은 enemy는 RespawnSystem._respawnQueue에만 보관 → aggro 판정/공격 대상 자동 제외.
//   Boss는 StageClear 후 완전 소멸 (respawn 없음 — _respawnQueue 미등록).
//
// ARCHITECTURE "Map = Actor": 한 맵의 모든 mutation을 단일 thread에 가두면
// 동시성 버그의 90%가 사라진다. 외부 → EnqueueJob 경유 마샬링.
public class GameMap
{
    readonly List<PlayerEntity> _players = new();
    readonly Dictionary<int, EnemyEntity> _enemies = new();

    // M4.2 Phase 02: entity id 발급기.
    // (A) GameWorld 경유 생성: _idAllocator = GameWorld.NextEntityId → 전역 풀 (globally-unique).
    // (B) 단독 생성 (테스트 / 미래 확장): null → 로컬 _localNextId (1부터 시작, 테스트 격리 보장).
    readonly Func<int>? _idAllocator;
    int _localNextId = 1;
    int AllocId() => _idAllocator != null ? _idAllocator() : _localNextId++;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    // M4.3R Phase 03: System 인스턴스 — tick thread 안에서만 사용 (§1.1 정합).
    readonly CombatSystem _combatSystem = new();
    readonly EnemyAISystem _enemyAISystem = new();
    readonly RespawnSystem _respawnSystem = new();

    public IReadOnlyList<PlayerEntity> Players => _players;
    public IReadOnlyDictionary<int, EnemyEntity> Enemies => _enemies;

    // M3 Phase 07: Stage Clear 1회 보장 flag.
    bool _stageCleared = false;

    // M4.1 Phase 06 (4단계): ProcessAttack이 rewind 범위 검증에 사용하는 현재 서버 tick.
    // Tick(long tickNumber) 진입 직후 갱신 — job 처리 *전*에 갱신해야 job 안에서 올바른 tick 읽힘.
    // tick thread invariant 안에서만 읽기/쓰기.
    long _currentTick;

    /// <summary>
    /// CombatSystem이 rewind 범위 검증에 사용하는 현재 서버 tick.
    /// tick thread invariant 안에서만 유효 (§1.1).
    /// </summary>
    internal long CurrentTick => _currentTick;

    /// <summary>
    /// M3 Phase 07: Stage Clear 상태 read-only 노출. 단위 테스트 + Phase 09 리허설 진단용.
    /// flag 자체는 *서버 권위* — 외부에서 강제 set 불가 (헌법 #1).
    /// </summary>
    public bool IsStageCleared => _stageCleared;

    // M4.2 Phase 01: 맵 ID.
    public MapId MapId { get; }

    // M4.2 Phase 02: 맵에 속한 portal 목록. PortalTable 단일 진실 공급원.
    public IReadOnlyList<Portal> Portals { get; }

    public GameMap(MapId mapId = MapId.HuntingGround, Func<int>? idAllocator = null)
    {
        MapId = mapId;
        _idAllocator = idAllocator;
        Portals = PortalTable.GetPortalsFor(mapId);

        foreach (EnemySpawnDef def in MapSpawnTable.GetSpawnsFor(mapId))
        {
            SpawnEnemy(def.Kind, def.X, def.Y, def.MaxHp);
        }
    }

    // **호출 invariant**: tick thread 또는 ctor에서만 (단일 thread invariant 유지).
    // **internal 유지 이유**: 테스트 픽스처가 직접 enemy 구성 가능 (InternalsVisibleTo).
    //
    // stats 오버라이드 규칙:
    //   - stats == null (기본) → kind==Normal이면 EnemyStats.NormalDefault() 자동 주입, Boss이면 default.
    //   - stats != null → 그대로 사용 (RespawnSystem이 원본 stats 유지 목적으로 전달).
    internal EnemyEntity SpawnEnemy(EnemyKind kind, float x, float y, int maxHp, EnemyStats? stats = null)
    {
        int id = AllocId();
        EnemyStats resolvedStats = stats ?? (kind == EnemyKind.Normal ? EnemyStats.NormalDefault() : default);
        EnemyEntity e = new EnemyEntity(id, kind, x, y, maxHp, resolvedStats);
        _enemies.Add(id, e);
        return e;
    }

    // virtual: 테스트 subclass에서 EnqueueJob 호출 카운트 추적 가능.
    public virtual void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    public PlayerEntity AddPlayer(GameSession? owner, Vector2 spawnPos, PlayerStats? stats = null)
    {
        PlayerEntity entity = new PlayerEntity(AllocId(), spawnPos, owner, stats);
        _players.Add(entity);
        return entity;
    }

    // M4.2 Phase 03: migration 전용 AddPlayer 오버로드 — 기존 entity id 유지 (ADR-026).
    public PlayerEntity AddPlayerWithId(int entityId, GameSession? owner, Vector2 spawnPos, PlayerStats stats, int currentHp)
    {
        PlayerEntity entity = new PlayerEntity(entityId, spawnPos, owner, stats);
        entity.Hp = currentHp;
        _players.Add(entity);
        return entity;
    }

    public bool RemovePlayer(int entityId)
        => _players.RemoveAll(p => p.EntityId == entityId) > 0;

    // Phase 10 (M2.5 lifecycle race): owner reference 기반 cleanup.
    public bool RemovePlayerBySession(GameSession owner)
        => _players.RemoveAll(p => ReferenceEquals(p.Owner, owner)) > 0;

    public PlayerEntity? GetPlayer(int entityId)
        => _players.Find(p => p.EntityId == entityId);

    internal EnemyEntity? GetEnemyById(int entityId)
        => _enemies.TryGetValue(entityId, out EnemyEntity? e) ? e : null;

    // ── CombatSystem용 internal mutator (§0.3 최소 surface) ──────────────────

    /// <summary>
    /// Stage Clear flag를 true로 설정. CombatSystem이 Boss 사망 시 1회만 호출.
    /// 외부에서 직접 set 불가 (헌법 #1 Server Authority).
    /// </summary>
    internal void SetStageCleared() => _stageCleared = true;

    /// <summary>
    /// 살아있는 적 목록에서 제거. CombatSystem이 enemy 사망 처리 후 호출.
    /// "살아있는 적만 _enemies" invariant 유지 책임 — 이 mutator 1곳만.
    /// </summary>
    internal void RemoveEnemy(int entityId) => _enemies.Remove(entityId);

    /// <summary>
    /// RespawnSystem에 죽은 enemy 등록. CombatSystem이 Normal enemy 사망 후 호출.
    /// RespawnSystem._respawnQueue 직접 접근 대신 이 경유로 단일화.
    /// </summary>
    internal void EnqueueRespawn(EnemyEntity dead) => _respawnSystem.Enqueue(dead);

    // ── Broadcast ────────────────────────────────────────────────────────────

    /// <summary>
    /// 같은 맵 전원에게 packet 전송.
    ///
    /// **호출 invariant**: tick thread에서만 (EnqueueJob 람다 안 또는 Tick 안).
    /// **skip 규칙**:
    ///   - owner == null (테스트용 entity)
    ///   - owner == except (발신자 자기 자신 제외)
    ///   - owner.IsClosing (Phase 10 lifecycle race 재발 봉합)
    /// </summary>
    public void BroadcastToAll(ArraySegment<byte> payload, GameSession? except = null)
    {
        foreach (PlayerEntity p in _players)
        {
            if (p.Owner == null) continue;
            if (ReferenceEquals(p.Owner, except)) continue;
            if (p.Owner.IsClosing) continue;
            p.Owner.Send(payload);
        }
    }

    // ── ProcessAttack (internal 래퍼 — 기존 테스트 인터페이스 보존) ───────────

    /// <summary>
    /// tick thread 안에서 attack 1건 처리. CombatSystem에 위임.
    ///
    /// **인터페이스 보존**: 테스트가 이 메서드를 직접 호출하므로 시그니처 유지.
    ///   실제 로직은 CombatSystem.ProcessAttack(map, ...) — §2.2 컨테이너+System 구조.
    ///
    /// **호출 invariant**: tick thread에서만. GameSession.SubmitAttack이 EnqueueJob 람다로 박음.
    /// </summary>
    internal void ProcessAttack(int attackerEntityId, int targetEntityId, long attackerClientTick)
        => _combatSystem.ProcessAttack(this, attackerEntityId, targetEntityId, attackerClientTick);

    // ── Tick 엔진 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// TickScheduler가 매 50ms마다 호출. 단일 thread.
    ///
    /// System 호출 순서 (§2.2 명문화):
    ///   1. physics (PlayerEntity Physics.Step + RecordPosition)
    ///   2. CombatSystem (EnqueueJob 경유 attack job 처리)
    ///   3. EnemyAISystem (UpdateEnemies → EnemyAISystem.Update)
    ///   4. RespawnSystem (ProcessRespawns → RespawnSystem.Process)
    ///
    /// physics가 1순위인 이유: 이 틱의 player 최종 위치를 RecordPosition으로 기록한 뒤
    ///   CombatSystem이 rewind lookup을 해야 하기 때문 — 단, job은 _currentTick 갱신 후 physics 전에 처리.
    ///   (헌법 #5: tick 안 await/Sleep/DB 금지)
    /// </summary>
    public void Tick(long tickNumber)
    {
        // _currentTick 갱신 — job 처리 *전*에 박아야
        // job(ProcessAttack 람다) 안에서 올바른 tick으로 rewind 범위 검증 가능.
        _currentTick = tickNumber;

        // 1) 외부 thread가 push한 job들 처리 (AddPlayer/RemovePlayer/SubmitAttack 등).
        //    CombatSystem 처리는 여기서 일어남 (SubmitAttack → EnqueueJob 람다).
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[Map] job 예외: {ex.Message}"); }
        }

        // 2) Physics.Step + RecordPosition (플레이어 이동).
        //    헌법 #1: 서버 권위 이동. 클라 prediction은 서버 snapshot으로 reconcile.
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
            // Physics.Step 완료 후 위치 기록 — "그 tick에 실제로 있던 위치" (M4.1 Phase 06).
            p.RecordPosition(tickNumber, p.Position);
        }

        // 3) Snapshot 브로드캐스트. 매 2 tick(=100ms).
        if (tickNumber % Constants.SnapshotTickInterval == 0)
        {
            foreach (PlayerEntity p in _players)
            {
                S_Snapshot pkt = new S_Snapshot
                {
                    entityId = p.EntityId,
                    x = p.Position.X,
                    y = p.Position.Y,
                    vx = p.Velocity.X,
                    vy = p.Velocity.Y,
                    serverTick = (int)tickNumber,
                    lastAckedClientTick = p.LastClientTick
                };
                BroadcastToAll(pkt.Write());
            }
        }

        // 4) EnemyAISystem: Normal enemy FSM 1틱 (aggro·Patrol↔Chase·이동·S_EntityState broadcast).
        _enemyAISystem.Update(this, tickNumber);

        // 5) RespawnSystem: Normal enemy respawn 카운트다운 + 재출현.
        _respawnSystem.Process(this, tickNumber);
    }
}
