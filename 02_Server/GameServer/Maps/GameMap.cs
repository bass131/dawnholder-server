using System.Collections.Concurrent;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

// EnemyKind → (MaxHp) 기본값 테이블. content.bin은 위치+kind만 담고 HP는 서버 권위 코드 결정.
// append-only: 새 종류 추가 시 이 배열에 항목 추가 (EnemyKind.cs 값과 index 정합 유지).
// kindId 범위 검증은 GameMap ctor 단일 지점 (reviewer 🟡 — 중복 검증 단일화).
file static class EnemyDefaultHp
{
    // index = (int)EnemyKind. GolemDefault().MaxHp와 일치 의무 — drift 방지는 테스트가 잡음.
    internal static readonly int[] ByKind = { 30, 100, 60 }; // Normal=30, Boss=100, Golem=60

    internal static int For(EnemyKind kind) => ByKind[(int)kind];
}

// 단일 GameMap actor. 단일 thread Tick → lock 없음.
//
// §2.2 컨테이너 + System 분리:
//   GameMap = 상태(_players/_enemies/_pendingJobs/AllocId) + Tick 엔진 + actor 경계.
//   로직은 CombatSystem / EnemyAISystem / RespawnSystem 3개로 추출.
//   Tick에서 System 호출 순서 명문화: physics → CombatSystem(EnqueueJob 경유) → EnemyAISystem → RespawnSystem.
//
// **_enemies invariant**: 살아있는 적만 _enemies에 잔류.
//   사망 시 CombatSystem이 즉시 S_EntityDeath broadcast + RemoveEnemy + (Normal only) EnqueueRespawn.
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
    int AllocId() => _idAllocator != null ? _idAllocator() : _localNextId++;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    // 지형 + 콘텐츠. null terrain = 평지 물리(Physics.Step 2-인자 fallback).
    readonly MapTerrain? _terrain;
    readonly MapContent? _content;

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

    // System 인스턴스 — tick thread 안에서만 사용 (§1.1 정합).
    readonly CombatSystem _combatSystem = new();
    readonly EnemyAISystem _enemyAISystem = new();
    readonly BossBehaviorSystem _bossBehaviorSystem = new();
    readonly RespawnSystem _respawnSystem = new();
    readonly DeferredDamageSystem _deferredDamageSystem = new();
    readonly SkillSystem _skillSystem = new();

    public IReadOnlyList<PlayerEntity> Players => _players;
    public IReadOnlyDictionary<int, EnemyEntity> Enemies => _enemies;

    // Stage Clear 1회 보장 flag.
    bool _stageCleared = false;

    // ProcessAttack이 rewind 범위 검증에 사용하는 현재 서버 tick.
    // Tick(long tickNumber) 진입 직후 갱신 — job 처리 *전*에 갱신해야 job 안에서 올바른 tick 읽힘.
    // tick thread invariant 안에서만 읽기/쓰기.
    long _currentTick;

    /// <summary>
    /// CombatSystem이 rewind 범위 검증에 사용하는 현재 서버 tick.
    /// tick thread invariant 안에서만 유효 (§1.1).
    /// </summary>
    internal long CurrentTick => _currentTick;

    /// <summary>
    /// Stage Clear 상태 read-only 노출.
    /// flag 자체는 *서버 권위* — 외부에서 강제 set 불가 (헌법 #1).
    /// </summary>
    public bool IsStageCleared => _stageCleared;

    public MapId MapId { get; }

    // 맵에 속한 portal 목록. PortalTable 단일 진실 공급원.
    public IReadOnlyList<Portal> Portals { get; }

    public GameMap(MapId mapId = MapId.HuntingGround, Func<int>? idAllocator = null,
                   MapTerrain? terrain = null, MapContent? content = null)
    {
        MapId = mapId;
        _idAllocator = idAllocator;
        _terrain = terrain;
        _content = content;
        Portals = PortalTable.GetPortalsFor(mapId);

        if (content != null)
        {
            foreach (EnemySpawnPoint sp in content.Enemies)
            {
                // kindId 범위 검증 — 알 수 없는 kindId = 저작 오류 → fail loud.
                if (sp.KindId >= EnemyDefaultHp.ByKind.Length)
                    throw new InvalidOperationException(
                        $"[GameMap:{mapId}] 알 수 없는 kindId={sp.KindId} in content.bin. " +
                        "EnemyKind enum과 EnemyDefaultHp 테이블을 확인하세요.");

                EnemyKind kind = (EnemyKind)sp.KindId;
                int maxHp = EnemyDefaultHp.For(kind);
                SpawnEnemy(kind, sp.X, sp.Y, maxHp);
            }
        }
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

    internal EnemyEntity? GetEnemyById(int entityId)
        => _enemies.TryGetValue(entityId, out EnemyEntity? e) ? e : null;

    // ── AnimState 계산 ───────────────────────────────────────────────────────

    /// <summary>
    /// 플레이어의 현재 시각 애니메이션 상태를 계산. 서버 권위 (헌법 #1).
    ///
    /// ActionFsm이 단일 출처 — Death/Hit/Attack/Jump/Walk/Idle 우선순위는
    /// FSM 전이 규칙으로 보장. 이 메서드는 FSM 현재 상태의 AnimState를 반환한다.
    ///
    /// **tick thread invariant**: GameMap.Tick 안에서만 호출 (단일 thread).
    /// </summary>
    static byte ComputePlayerAnimState(PlayerEntity p)
        => (byte)p.ActionFsm.AnimState;

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
    internal void SendPlayerHp(PlayerEntity p)
    {
        if (p.Owner == null || p.Owner.IsClosing) return;
        S_PlayerHp pkt = new S_PlayerHp
        {
            entityId  = p.EntityId,
            currentHp = Math.Max(0, p.Hp),
            maxHp     = p.MaxHp,
        };
        p.Owner.Send(pkt.Write());
    }

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

    // ── ProcessSkill (썬더볼트 AoE — P4) ──────────────────────────────────────

    /// <summary>
    /// tick thread 안에서 스킬 1건 처리. SkillSystem.ProcessThunderbolt에 위임.
    ///
    /// **호출 invariant**: tick thread에서만. GameSession.SubmitSkillUse가 EnqueueJob 람다로 박음.
    /// </summary>
    internal void ProcessSkill(int casterEntityId, byte skillId, long attackerClientTick)
        => _skillSystem.ProcessSkill(this, casterEntityId, skillId, attackerClientTick);

    // ── Tick 엔진 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// TickScheduler가 매 50ms마다 호출. 단일 thread.
    ///
    /// System 호출 순서 (§2.2 명문화):
    ///   1. physics (PlayerEntity Physics.Step + RecordPosition)
    ///   2. CombatSystem (EnqueueJob 경유 attack job 처리)
    ///   3. EnemyAISystem (Normal/Golem FSM)
    ///   4. BossBehaviorSystem (Boss 패턴 FSM + 데미지 판정)
    ///   5. DeferredDamageSystem (impactTick 도달 데미지 + 사망 처리)
    ///   6. RespawnSystem
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

            // KnockbackVx(피격) + AttackLungeVx(근접 공격 전방 lunge)를 ExternalVelX로 전달.
            //   둘은 상호배타 State(Hit vs Attack)라 합 = 활성값. 0이면 기존 이동과 동일.
            PhysicsInput input = new PhysicsInput(inputX, jumpPressed, Constants.TickDuration, p.KnockbackVx + p.AttackLungeVx);
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

        // 3) Snapshot 브로드캐스트. 매 2 tick(=100ms).
        if (tickNumber % Constants.SnapshotTickInterval == 0)
        {
            foreach (PlayerEntity p in _players)
            {
                // 플레이어 animState 계산 (헌법 #1 — 서버 권위 결정).
                // latch 감소는 physics 루프(2단계)에서 매 tick 처리됨 — 여기선 계산·주입만.
                byte animState = ComputePlayerAnimState(p);

                S_Snapshot pkt = new S_Snapshot
                {
                    entityId = p.EntityId,
                    x = p.Position.X,
                    y = p.Position.Y,
                    vx = p.Velocity.X,
                    vy = p.Velocity.Y,
                    serverTick = (int)tickNumber,
                    lastAckedClientTick = p.LastClientTick,
                    animState = animState
                };
                BroadcastToAll(pkt.Write());
            }
        }

        // 4) EnemyAISystem: Normal/Golem FSM 1틱 (aggro·Patrol↔Chase·이동·S_EntityState broadcast).
        _enemyAISystem.Update(this, tickNumber);

        // 5) BossBehaviorSystem: Boss FSM 1틱 (쿨다운→telegraph→데미지판정→리셋, latch, broadcast).
        _bossBehaviorSystem.Update(this, tickNumber);

        // 6) DeferredDamageSystem: impactTick 도달 항목 HP 적용 + S_HitResult broadcast + 사망 처리.
        _deferredDamageSystem.Process(this, tickNumber);

        // 7) RespawnSystem: Normal enemy respawn 카운트다운 + 재출현.
        _respawnSystem.Process(this, tickNumber);
    }
}
