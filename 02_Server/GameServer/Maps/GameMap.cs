using System.Collections.Concurrent;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

// 단일 GameMap actor. 단일 thread Tick → lock 없음.
//
// §2.2 컨테이너 + System 분리:
//   GameMap = 상태(_players/_enemies/_pendingJobs/AllocId) + Tick 엔진 + actor 경계.
//   로직은 CombatSystem / EnemyAISystem / RespawnSystem 3개로 추출.
//   Tick에서 System 호출 순서 명문화: physics → CombatSystem(EnqueueJob 경유) → EnemyAISystem → RespawnSystem.
//
// **_enemies invariant**: 살아있는 적만 _enemies에 잔류.
//   사망 시 HandleEnemyDeath가 S_EntityDeath broadcast + RemoveEnemy + (Normal only) EnqueueRespawn.
//   죽음 연출은 클라 VFX 담당 (헌법 #1 — 서버는 확정+제거만).
//
// ARCHITECTURE "Map = Actor": 한 맵의 모든 mutation을 단일 thread에 가두면
// 동시성 버그의 90%가 사라진다. 외부 → EnqueueJob 경유 마샬링.
public class GameMap
{
    readonly List<PlayerEntity> _players = new();
    readonly Dictionary<int, EnemyEntity> _enemies = new();

    // entity id 발급기.
    // (A) GameWorld 경유 생성: _idAllocator = GameWorld.NextEntityId → 전역 풀 (globally-unique).
    // (B) 단독 생성 (테스트 / 미래 확장): null → 로컬 _localNextId (1부터 시작, 테스트 격리 보장).
    readonly Func<int>? _idAllocator;
    int _localNextId = 1;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    // 지형 + 콘텐츠. null terrain = 평지 물리(Physics.Step 2-인자 fallback).
    readonly MapTerrain? _terrain;
    readonly MapContent? _content;

    // Q2: 적 사망 시 호출되는 외부 콜백. GameWorld.MakeMap에서 QuestRegistry.OnKill 연결.
    //   virtual OnEnemyKilled가 이 콜백을 invoke — SpyGameMap override는 base 미호출이므로 미영향(정상).
    readonly Action<int, EnemyEntity>? _onEnemyKilled;

    // System 인스턴스 — tick thread 안에서만 사용 (§1.1 정합).
    readonly CombatSystem _combatSystem = new();
    readonly EnemyAISystem _enemyAISystem = new();
    readonly BossBehaviorSystem _bossBehaviorSystem = new();
    readonly RespawnSystem _respawnSystem = new();
    readonly DeferredDamageSystem _deferredDamageSystem = new();
    readonly SkillSystem _skillSystem = new();

    // Stage Clear 1회 보장 flag.
    bool _stageCleared = false;

    // 보스 초기 스폰 지점 — content에서 Boss 스폰 포인트 캡처(빈 방 리스폰용). Boss 없는 맵은 null.
    readonly EnemySpawnPoint? _bossSpawnPoint;

    // ProcessAttack이 rewind 범위 검증에 사용하는 현재 서버 tick.
    // Tick(long tickNumber) 진입 직후 갱신 — job 처리 *전*에 갱신해야 job 안에서 올바른 tick 읽힘.
    // tick thread invariant 안에서만 읽기/쓰기.
    long _currentTick;

    // wire-format 표현 책임 (§2.2 분리, M7.7 P4a). 시뮬 상태 → 패킷 조립·송신을 위임.
    //   GameMap 참조를 받으므로 this 완성 후(ctor 본문 진입 시점) 안전. byte 동치 계약은 publisher 박제.
    readonly MapPacketPublisher _publisher;

    public GameMap(MapId mapId = MapId.HuntingGround, Func<int>? idAllocator = null,
                   MapTerrain? terrain = null, MapContent? content = null,
                   Action<int, EnemyEntity>? onEnemyKilled = null)
    {
        MapId = mapId;
        _idAllocator = idAllocator;
        _terrain = terrain;
        _content = content;
        _onEnemyKilled = onEnemyKilled;
        Portals = PortalTable.GetPortalsFor(mapId);
        _publisher = new MapPacketPublisher(this);

        if (content != null)
        {
            foreach (EnemySpawnPoint sp in content.Enemies)
            {
                // kindId 범위 검증 — 알 수 없는 kindId = 저작 오류 → fail loud.
                if (!Enum.IsDefined(typeof(EnemyKind), sp.KindId))
                {
                    throw new InvalidOperationException(
                        $"[GameMap:{mapId}] 알 수 없는 kindId={sp.KindId} in content.bin. " +
                        "EnemyKind enum을 확인하세요.");
                }

                EnemyKind kind = (EnemyKind)sp.KindId;
                // HP 단일 출처 = Formulas factory MaxHp.
                int maxHp = kind switch
                {
                    EnemyKind.Normal => EnemyStats.NormalDefault().MaxHp,
                    EnemyKind.Boss   => EnemyStats.BossDefault().MaxHp,
                    EnemyKind.Golem  => EnemyStats.GolemDefault().MaxHp,
                    _                => 0,
                };
                SpawnEnemy(kind, sp.X, sp.Y, maxHp);
                if (kind == EnemyKind.Boss) _bossSpawnPoint = sp;
            }
        }
    }

    public IReadOnlyList<PlayerEntity> Players => _players;
    public IReadOnlyDictionary<int, EnemyEntity> Enemies => _enemies;

    /// <summary>
    /// Stage Clear 상태 read-only 노출.
    /// flag 자체는 *서버 권위* — 외부에서 강제 set 불가 (헌법 #1).
    /// </summary>
    public bool IsStageCleared => _stageCleared;

    public MapId MapId { get; }

    // 맵에 속한 portal 목록. PortalTable 단일 진실 공급원.
    public IReadOnlyList<Portal> Portals { get; }

    // 플레이어 스폰 좌표 — content가 있으면 content 기준, 없으면 원점 fallback.
    internal Vector2 PlayerSpawnPosition =>
        _content != null
            ? new Vector2(_content.PlayerSpawnX, _content.PlayerSpawnY)
            : Vector2.Zero;

    // 맵 X축 경계 (minX, maxX). Teleport 착지 clamp 및 서버 권위 범위 검증(헌법 §3)에 사용.
    // terrain이 있으면 Solids 전체의 MinX/MaxX를 합산 — 텔레포트가 솔리드 바깥으로 나가는 것을 차단.
    // terrain이 null이면 float.MinValue/MaxValue (평지 테스트 맵 — 경계 없음).
    internal (float MinX, float MaxX) MapBoundsX
    {
        get
        {
            if (_terrain == null || _terrain.Solids.Length == 0)
                return (float.MinValue, float.MaxValue);
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (TerrainAabb s in _terrain.Solids)
            {
                if (s.MinX < min) min = s.MinX;
                if (s.MaxX > max) max = s.MaxX;
            }
            return (min, max);
        }
    }

    /// <summary>
    /// CombatSystem이 rewind 범위 검증에 사용하는 현재 서버 tick.
    /// tick thread invariant 안에서만 유효 (§1.1).
    /// </summary>
    internal long CurrentTick => _currentTick;

    // virtual: 테스트 subclass에서 EnqueueJob 호출 카운트 추적 가능.
    public virtual void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    public PlayerEntity AddPlayer(GameSession? owner, Vector2 spawnPos, PlayerStats? stats = null)
    {
        PlayerEntity entity = new PlayerEntity(AllocId(), spawnPos, owner, stats);
        _players.Add(entity);
        return entity;
    }

    // migration 전용 AddPlayer 오버로드 — 기존 entity id 유지 (ADR-026).
    public PlayerEntity AddPlayerWithId(int entityId, GameSession? owner, Vector2 spawnPos, PlayerStats stats, int currentHp)
    {
        PlayerEntity entity = new PlayerEntity(entityId, spawnPos, owner, stats);
        entity.Hp = currentHp;
        _players.Add(entity);
        return entity;
    }

    public bool RemovePlayer(int entityId)
        => _players.RemoveAll(p => p.EntityId == entityId) > 0;

    // owner reference 기반 cleanup.
    public bool RemovePlayerBySession(GameSession owner)
        => _players.RemoveAll(p => ReferenceEquals(p.Owner, owner)) > 0;

    public PlayerEntity? GetPlayer(int entityId)
        => _players.Find(p => p.EntityId == entityId);

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

    // ── Tick 엔진 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// TickScheduler가 매 50ms마다 호출. 단일 thread.
    ///
    /// System 호출 순서 (§2.2 명문화):
    ///   1. physics (PlayerEntity Physics.Step + RecordPosition)
    ///   2. CombatSystem (EnqueueJob 경유 attack job 처리)
    ///   3. EnemyAISystem (Normal/Golem FSM — X 이동)
    ///   4. BossBehaviorSystem (Boss 패턴 FSM — X 이동)
    ///   5. ApplyEnemyGravity (모든 적 수직 중력 패스 — Y/Vy/OnGround 갱신)
    ///   6. DeferredDamageSystem (impactTick 도달 데미지 + 사망 처리)
    ///   7. RespawnSystem
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
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[Map] job 예외: {ex.Message}"); }
        }

        // 2) Physics.Step + RecordPosition (플레이어 이동).
        //    헌법 #1: 서버 권위 이동. 클라 prediction은 서버 snapshot으로 reconcile.
        //
        //    틱당 정확히 Physics.Step 1회 불변식: 물리 시간 = 벽시계 시간 (50ms/tick).
        //    큐에 N개 쌓여도 이 틱에는 1개만 소비 — 멀티 드레인 금지.
        //    큐 비면(starvation) neutral (0, false) 적용 — 세계는 계속 흐름(중력/마찰).
        foreach (PlayerEntity p in _players)
        {
            // death-guard: IsDead인데 DeathState가 아니면 즉시 전이.
            // BossBehaviorSystem.ApplyBossAttack이 Hp를 0으로 내린 직후에도 안전하게 잡힘.
            if (p.IsDead && p.ActionFsm.CurrentState is not DeathState)
                p.ActionFsm.ChangeState(PlayerCombatStates.Death, p);

            bool hasInput = p.TryDequeueInput(out PlayerEntity.InputCommand cmd);
            sbyte inputX = hasInput ? cmd.InputX : (sbyte)0;
            bool rawJump = hasInput && cmd.JumpPressed;

            // movement-gate: LocksMovement=true인 State(Attack/Hit/Death)면 이동 입력 무효.
            bool locked = p.ActionFsm.CurrentState.LocksMovement;
            if (locked)
            {
                inputX = 0;
                rawJump = false;
            }

            bool jumpPressed = p.ResolveJump(rawJump); // jump buffer: 공중 입력 → 착지 틱 발사

            // ExternalImpulseVx: 대쉬/lunge(AttackState) + 넉백(HitState) 통합 단일 필드.
            //   두 State는 상호배타라 항상 하나만 활성. 0이면 기존 이동과 동일.
            PhysicsInput input = new PhysicsInput(inputX, jumpPressed, Constants.TickDuration, p.ExternalImpulseVx);
            PhysicsState before = new PhysicsState(p.Position, p.Velocity, p.OnGround);
            MoveParams move = new MoveParams(p.Stats.MoveSpeed, p.Stats.JumpVel);
            PhysicsState after = Physics.Step(before, input, _terrain, move);
            p.Position = after.Position;
            p.Velocity = after.Velocity;
            p.OnGround = after.OnGround;

            // kill-plane: 낙하로 맵 밖 벗어나면 PlayerSpawn 재배치. HP 무변화 (낙사 데미지 M4.5 이월).
            // terrain null이면 체크 skip (평지 맵은 낙사 없음).
            if (_terrain != null && p.Position.Y < _terrain.KillPlaneY)
            {
                Vector2 spawn = PlayerSpawnPosition;
                p.Position = spawn;
                p.Velocity = Vector2.Zero;
                p.OnGround = false;
            }

            // 이동 방향 갱신 — inputX가 0이 아닐 때만. 0이면 마지막 방향 유지.
            // FacingDir은 S_PlayerAttack.facing 직렬화에 사용 (공격 연출 방향 결정).
            if (inputX != 0)
                p.FacingDir = inputX > 0 ? (sbyte)1 : (sbyte)-1;

            // ack = 적용 시점 clientTick. 빈 틱(starvation)은 불변 — 클라 reconcile 정합.
            if (hasInput)
                p.LastClientTick = cmd.ClientTick;

            // Physics.Step 완료 후 위치 기록 — "그 tick에 실제로 있던 위치".
            p.RecordPosition(tickNumber, p.Position);

            // ActionFsm Tick: 전투 State(Attack/Hit)의 카운터 감소 + 이동 State 전환 판정을 통합 처리.
            // Attack/HitState는 내부에서 StateTicksRemaining을 감소시키고 0이면 ResolveGrounded 반환.
            // 이동 State(Idle/Move/Jump)는 물리 상태(OnGround/Velocity)를 보고 전환.
            p.ActionFsm.Tick(p);
        }

        // 3) Snapshot 브로드캐스트. 매 1 tick(=50ms, 20Hz).
        //    조립·송신은 MapPacketPublisher 위임 (§2.2 표현 분리). animState 계산도 publisher 내부.
        if (tickNumber % Constants.SnapshotTickInterval == 0)
            _publisher.BroadcastSnapshots(tickNumber);

        // 4) EnemyAISystem: Normal/Golem FSM 1틱 (aggro·Patrol↔Chase·이동·S_EntityState broadcast).
        _enemyAISystem.Update(this, tickNumber);

        // 5) BossBehaviorSystem: Boss FSM 1틱 (쿨다운→telegraph→데미지판정→리셋, latch, broadcast).
        _bossBehaviorSystem.Update(this, tickNumber);

        // 6) 적 수직 물리: FSM이 X를 세팅한 뒤 수직 중력 패스 적용.
        //    inputX=0 → Physics.Step이 X를 변경하지 않아 FSM 세팅 X 보존.
        //    terrain==null 이면 Physics.Step이 자동으로 StepFlat(Y<=0 clamp) 으로 위임.
        ApplyEnemyGravity();

        // 7) DeferredDamageSystem: impactTick 도달 항목 HP 적용 + S_HitResult broadcast + 사망 처리.
        _deferredDamageSystem.Process(this, tickNumber);

        // 8) RespawnSystem: Normal enemy respawn 카운트다운 + 재출현.
        _respawnSystem.Process(this, tickNumber);

        // 9) 보스 방 빈 상태 리스폰: 플레이어 0 + 보스 부재면 재출현(영호 지시). 빈 방에서만 리셋.
        MaybeRespawnBoss();
    }

    // ── 지형 쿼리 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 수직 텔레포트 목적지 발판을 찾는다 (지형 인식 M4.15 P09).
    ///
    /// 후보: Solids[].MaxY + Platforms[].Y 중 x ∈ [MinX-eps, MaxX+eps].
    ///   up=true  : surfaceY &gt; currentY+eps 중 가장 낮은 것(가장 가까운 위).
    ///   up=false : surfaceY &lt; currentY-eps 중 가장 높은 것(가장 가까운 아래).
    ///   가장 가까운 발판이 maxRange 이내면 destY=surfaceY → true.
    ///   발판 없거나 사거리 밖이면 destY=currentY → false.
    ///
    /// **헌법 §5 정합**: span 순회만, alloc 0.
    /// **_terrain==null(평지 맵)**: 발판 없음 → false.
    /// </summary>
    internal bool TryFindVerticalTeleportTarget(
        float x, float currentY, bool up, float maxRange, out float destY)
    {
        destY = currentY;
        if (_terrain == null)
            return false;

        const float eps = 0.0001f; // Terrain.cs GroundEpsilon 재사용

        float best = up ? float.MaxValue : float.MinValue;
        bool found = false;

        foreach (TerrainAabb s in _terrain.Solids)
        {
            if (x < s.MinX - eps || x > s.MaxX + eps)
                continue;
            float surfaceY = s.MaxY;
            if (up)
            {
                if (surfaceY > currentY + eps && surfaceY < best)
                {
                    best  = surfaceY;
                    found = true;
                }
            }
            else
            {
                if (surfaceY < currentY - eps && surfaceY > best)
                {
                    best  = surfaceY;
                    found = true;
                }
            }
        }

        foreach (TerrainPlatform p in _terrain.Platforms)
        {
            if (x < p.MinX - eps || x > p.MaxX + eps)
                continue;
            float surfaceY = p.Y;
            if (up)
            {
                if (surfaceY > currentY + eps && surfaceY < best)
                {
                    best  = surfaceY;
                    found = true;
                }
            }
            else
            {
                if (surfaceY < currentY - eps && surfaceY > best)
                {
                    best  = surfaceY;
                    found = true;
                }
            }
        }

        if (!found || MathF.Abs(best - currentY) > maxRange)
            return false;

        destY = best;
        return true;
    }

    // **호출 invariant**: tick thread 또는 ctor에서만 (단일 thread invariant 유지).
    //
    // stats 오버라이드 규칙:
    //   - stats == null (기본) → kind==Normal이면 EnemyStats.NormalDefault() 자동 주입, Boss이면 default.
    //   - stats != null → 그대로 사용 (RespawnSystem이 원본 stats 유지 목적으로 전달).
    internal EnemyEntity SpawnEnemy(EnemyKind kind, float x, float y, int maxHp, EnemyStats? stats = null)
    {
        int id = AllocId();
        // kind별 stats 결정. stats != null이면 RespawnSystem이 원본 유지 목적으로 전달한 것 — 그대로 사용.
        // default 분기 = fail-safe (알 수 없는 미래 종류 — Defense/MaxHp/Speed 모두 0, 동작하되 허약).
        EnemyStats resolvedStats = stats ?? kind switch
        {
            EnemyKind.Normal => EnemyStats.NormalDefault(),
            EnemyKind.Golem  => EnemyStats.GolemDefault(),
            EnemyKind.Boss   => EnemyStats.BossDefault(),
            _                => default,
        };
        EnemyEntity e = new EnemyEntity(id, kind, x, y, maxHp, resolvedStats);
        _enemies.Add(id, e);
        e.OwningMap = this;
        // Fsm은 OwningMap 세팅 후 생성 — kind별 초기 State (Boss=BossStates.Idle, 그외=EnemyStates.Patrol).
        e.Fsm = new StateMachine<EnemyEntity>(
            kind == EnemyKind.Boss ? BossStates.Idle : EnemyStates.Patrol, e);
        return e;
    }

    internal EnemyEntity? GetEnemyById(int entityId)
        => _enemies.TryGetValue(entityId, out EnemyEntity? e) ? e : null;

    // ── CombatSystem용 internal mutator (§0.3 최소 surface) ──────────────────

    /// <summary>
    /// 살아있는 적 목록에서 제거. HandleEnemyDeath가 사망 처리 시 호출 (+테스트).
    /// "살아있는 적만 _enemies" invariant 유지 책임 — 이 mutator 1곳만.
    /// </summary>
    internal void RemoveEnemy(int entityId) => _enemies.Remove(entityId);

    /// <summary>
    /// RespawnSystem에 죽은 enemy 등록. HandleEnemyDeath가 Normal enemy 사망 시 호출 (+테스트).
    /// RespawnSystem._respawnQueue 직접 접근 대신 이 경유로 단일화.
    /// </summary>
    internal void EnqueueRespawn(EnemyEntity dead) => _respawnSystem.Enqueue(dead);

    /// <summary>
    /// 사망 처리 시퀀스(S_EntityDeath broadcast → StageClear → RemoveEnemy → EnqueueRespawn) 완료 후 호출되는 훅.
    /// 기본 구현은 생성자 주입 콜백(_onEnemyKilled)을 invoke — GameWorld.MakeMap이 PartyRegistry.OnKill을 연결.
    /// virtual 유지: SpyGameMap override는 base 미호출 → 콜백 미실행(정상 — 테스트 spy 격리).
    /// tick thread invariant 안에서만 호출.
    /// </summary>
    protected virtual void OnEnemyKilled(int killerEntityId, EnemyEntity target)
        => _onEnemyKilled?.Invoke(killerEntityId, target);

    /// <summary>
    /// 적 사망 후처리: S_EntityDeath broadcast → (Boss) StageClear → 제거 → (Normal) respawn 큐잉.
    /// CombatSystem(즉시) / DeferredDamageSystem(지연) / SkillSystem(Dash) 3 경로 공통 — DRY 단일 출처.
    /// HP 게이트(target.Hp &lt;= 0)와 S_HitResult 송신은 호출처에 남는다 — 적용 타이밍이 경로마다 다르므로.
    ///
    /// **순서 계약(BossStageClearTests)**: S_EntityDeath → S_StageClear 순서 보존 필수.
    /// **tick thread invariant**: GameMap.Tick 안에서만 호출.
    /// </summary>
    internal void HandleEnemyDeath(EnemyEntity target, int killerEntityId)
    {
        _publisher.BroadcastEntityDeath(target.EntityId);

        if (target.Kind == EnemyKind.Boss && !IsStageCleared)
        {
            SetStageCleared();
            _publisher.BroadcastStageClear(target.EntityId);
        }
        RemoveEnemy(target.EntityId);
        // Normal(슬라임)은 원위치 재스폰, Golem은 1층 좌↔우 교차 재스폰(RespawnSystem이 위치 결정). Boss는 1회성.
        if (target.Kind == EnemyKind.Normal || target.Kind == EnemyKind.Golem)
            EnqueueRespawn(target);

        OnEnemyKilled(killerEntityId, target);
    }

    /// <summary>
    /// 플레이어 사망 후처리: PlayerSpawn 재배치 + 풀피 부활 + HUD HP 송신.
    /// HandleEnemyDeath와 대칭 — 사망 처리는 권위 맵 소유자의 책임(State 폴더 아님).
    /// 현재 호출 경로: EnemyStates.ApplyMeleeDamage(적/보스 근접 치사타). M8 영속화 훅 단일 후보.
    /// tick thread invariant: GameMap.Tick(EnemyAISystem/BossBehaviorSystem) 안에서만.
    /// </summary>
    internal void HandlePlayerDeath(PlayerEntity player)
    {
        Vector2 spawn = PlayerSpawnPosition;
        player.Position = spawn;
        player.Velocity = Vector2.Zero;
        player.OnGround = false;
        player.Hp = player.Stats.MaxHp;
        player.Revive();
        SendPlayerHp(player);
    }

    /// <summary>
    /// DeferredDamageSystem에 지연 데미지 1건 등록. P3(평타)/P4(썬더볼트)가 호출.
    /// impactTick = CurrentTick + delayTicks — 호출자가 계산 후 전달.
    /// tick thread invariant: EnqueueJob 람다 안 또는 Tick 안에서만 호출.
    /// </summary>
    internal void EnqueueDeferredDamage(DeferredImpact impact) => _deferredDamageSystem.Enqueue(impact);

    // ── Broadcast / 1:1 송신 ─────────────────────────────────────────────────

    /// <summary>
    /// 플레이어 본인에게만 S_PlayerHp 1:1 송신.
    ///
    /// **호출 invariant**: tick thread에서만. player.Hp가 mutate되는 *모든 지점*에서 동반 호출이 규율
    ///   (진입 EnterGameWorld / 피격·부활 ApplyBossAttack / 맵 전환 MapMigration). 누락 시 HUD 표시 갭.
    /// **논블로킹 보장**: Owner.Send는 Session.Send → lock + queue enqueue (헌법 #5).
    /// currentHp는 Math.Max(0, p.Hp) floor — 음수 방어 (표시 전용, 사망 lifecycle은 S_EntityDeath 채널).
    /// entityId 필드는 미래 원격/파티 HP 바 확장 대비 — 이번 마일스톤은 본인에게만 송신.
    /// </summary>
    internal void SendPlayerHp(PlayerEntity p) => _publisher.SendPlayerHp(p);

    /// <summary>
    /// 새로 진입한 세션에게 이 맵의 현재 roster를 1:1 Send — 기존 player(S_PlayerJoin) + 살아있는 enemy(S_EntitySpawn).
    /// EnterGameWorld(최초 진입) / MapMigration(맵 이동) 두 경로 공통 — DRY 단일 출처.
    ///
    /// existingPlayers: AddPlayer *전에* 호출부가 찍은 snapshot(자기 자신 제외 — self에게 self PlayerJoin 보내는 것 방지).
    ///   snapshot 순서 의존성은 호출부 책임 — 전송 루프만 여기로.
    /// closing-skip: Owner null(유효 연결 없음) + IsClosing(닫히는 중) 둘 다 skip — BroadcastToAll 정책 정합.
    /// **§2 wire**: S_PlayerJoin/S_EntitySpawn 필드·순서는 통합 전 원본과 byte 단위 동일.
    /// </summary>
    internal void SendInitialRosterTo(GameSession target, List<PlayerEntity> existingPlayers)
        => _publisher.SendInitialRoster(target, existingPlayers);

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

    // ── ProcessSkill (썬더볼트 AoE — P4) ──────────────────────────────────────

    /// <summary>
    /// tick thread 안에서 스킬 1건 처리. SkillSystem.ProcessThunderbolt에 위임.
    ///
    /// **호출 invariant**: tick thread에서만. GameSession.SubmitSkillUse가 EnqueueJob 람다로 박음.
    /// </summary>
    internal void ProcessSkill(int casterEntityId, byte skillId, long attackerClientTick, sbyte facing, byte verticalDir)
        => _skillSystem.ProcessSkill(this, casterEntityId, skillId, attackerClientTick, facing, verticalDir);

    /// <summary>
    /// 보스 방이 비고(플레이어 0) 보스도 없으면 보스를 재출현 — 다음 입장자가 fresh 보스를 만남(영호 지시).
    /// 빈 방에서만 리셋 → 전투 중/직후 갑작스런 재등장 없음. StageClear flag도 함께 리셋.
    /// broadcast 불필요: 플레이어 0명일 때만 실행 → 수신자 0. 입장자는 SendInitialRosterTo로 받음.
    /// tick thread invariant: Tick 안에서만 호출. 헌법 #5 정합(await/sleep 없음).
    /// </summary>
    private void MaybeRespawnBoss()
    {
        if (_bossSpawnPoint is not { } bsp) return;          // 보스 없는 맵 → 매 틱 최저비용 early-return
        if (_players.Count != 0) return;                     // 누군가 있으면 대기(전투 중/직후 리스폰 금지)
        foreach (EnemyEntity e in _enemies.Values)
            if (e.Kind == EnemyKind.Boss) return;            // 이미 보스 존재 → 중복 방지
        EnemyEntity boss = SpawnEnemy(EnemyKind.Boss, bsp.X, bsp.Y, EnemyStats.BossDefault().MaxHp);
        _stageCleared = false;
        Console.WriteLine($"[Map:{MapId}] 빈 방 보스 재출현: id={boss.EntityId}");
    }

    /// <summary>
    /// 살아 있는 모든 적(Normal/Golem/Boss)에 수직 중력 패스 적용.
    ///
    /// FSM(EnemyAISystem/BossBehaviorSystem)이 X를 세팅한 *뒤*에 호출 —
    /// inputX=0으로 Physics.Step을 호출하면 X 변화 없이 Y/Vy/OnGround만 갱신된다.
    ///
    /// terrain==null(평지 맵)이면 Physics.Step이 StepFlat으로 위임:
    ///   Y&lt;=0 이면 clamp+onGround=true — 지면 아래로 꺼지지 않음.
    ///
    /// moveParams의 MoveSpeed/JumpVel은 inputX=0+jumpPressed=false라 사실상 미사용이지만
    /// Physics.Step 시그니처 충족을 위해 실제 적 스탯 기반 값을 전달한다.
    ///
    /// 헌법 #5: async/await/Thread.Sleep 없음 — 순수 동기 연산.
    /// </summary>
    private void ApplyEnemyGravity()
    {
        PhysicsInput gravityInput = new PhysicsInput((sbyte)0, false, Constants.TickDuration);
        List<EnemyEntity>? fallen = null; // 낙사 대상 — 순회 중 _enemies 수정 금지, collect-then-remove
        foreach (EnemyEntity enemy in _enemies.Values)
        {
            MoveParams move = new MoveParams(enemy.Stats.MoveSpeed, 0f);
            PhysicsState before = new PhysicsState(
                new Vector2(enemy.X, enemy.Y),
                new Vector2(0f, enemy.Vy),
                enemy.OnGround);
            PhysicsState after = Physics.Step(before, gravityInput, _terrain, move);
            enemy.Y        = after.Position.Y;
            enemy.Vy       = after.Velocity.Y;
            enemy.OnGround = after.OnGround;

            if (_terrain != null && enemy.Y < _terrain.KillPlaneY)
            {
                fallen ??= new List<EnemyEntity>();
                fallen.Add(enemy);
            }
        }

        if (fallen != null)
        {
            foreach (EnemyEntity enemy in fallen)
                DespawnEnemyByFall(enemy);
        }
    }

    /// <summary>
    /// kill-plane 아래로 낙사한 적을 소멸시킨다.
    ///
    /// HandleEnemyDeath와의 차이:
    ///   - killer 없음 → OnEnemyKilled 호출 X (파티 킬 크레딧 오발동 방지).
    ///   - Boss → StageClear 발동 X (보스방에 낭떠러지 없으므로 실질 비발생, 안전망 차원).
    ///   - Normal/Golem → EnqueueRespawn 호출로 사냥터 인구 유지.
    ///
    /// 헌법 §5: async/await/Thread.Sleep 없음 — 순수 동기.
    /// </summary>
    private void DespawnEnemyByFall(EnemyEntity enemy)
    {
        _publisher.BroadcastEntityDeath(enemy.EntityId);
        RemoveEnemy(enemy.EntityId);
        if (enemy.Kind == EnemyKind.Normal || enemy.Kind == EnemyKind.Golem)
            EnqueueRespawn(enemy);
    }

    /// <summary>
    /// Stage Clear flag를 true로 설정. HandleEnemyDeath가 Boss 사망 시 1회만 호출.
    /// 외부에서 직접 set 불가 (헌법 #1 Server Authority).
    /// </summary>
    private void SetStageCleared() => _stageCleared = true;

    private int AllocId() => _idAllocator != null ? _idAllocator() : _localNextId++;
}
