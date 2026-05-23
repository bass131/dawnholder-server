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

    // M3 Phase 07 (보스 + Stage Clear): 우측 zone 보스 placeholder.
    // 3-zone 좌표 약속 = 좌 마을 (x<0) / 중 전투 (Normal=10) / 우 보스 (Boss=30). player spawn=0 정합.
    // HP 100 = damage 10 × 10회 사망. 본 마감엔 보스 전용 데미지 공식 + 페이즈 — M4 backlog.
    // AI 없음, 패시브 dummy (Phase 06 Normal과 동일 모델 — `EnemyKind.Boss` 분기만 다름).
    public const float BossSpawnX = 30f;
    public const float BossSpawnY = 0f;
    public const int BossMaxHp = 100;

    // M3 Phase 07: Stage Clear 1회 보장 flag. tick thread invariant 안에서만 읽기/쓰기.
    // **헌법 #1 (Server Authority)**: 클라가 stage clear 자체 판정 X — 서버가 본 flag로 1회 broadcast.
    // **idempotent 약속**: 보스 HP 0 후 추가 attack 도착해도 (a) target.Hp<=0 + _enemies.Remove로
    // step 2/3 silent drop (b) 본 flag true면 broadcast 분기 미진입 — 이중 안전망.
    bool _stageCleared = false;

    // M4.1 Phase 06 (4단계): ProcessAttack이 rewind 범위 검증에 사용하는 현재 서버 tick.
    // Tick(long tickNumber) 진입 직후 박힘 — job 처리 *전*에 갱신해야 job 안에서 올바른 tick 읽힘.
    // tick thread invariant 안에서만 읽기/쓰기 (lock 불필요).
    long _currentTick;

    /// <summary>
    /// M3 Phase 07: Stage Clear 상태 read-only 노출. 단위 테스트 + Phase 09 리허설 진단용.
    /// flag 자체는 *서버 권위* — 외부에서 강제 set 불가 (헌법 #1).
    /// </summary>
    public bool IsStageCleared => _stageCleared;

    public GameMap()
    {
        // M3 Phase 06 Step 2: 서버 시작 시 Normal enemy 1마리 즉시 spawn.
        // 응급 단순화 — respawn 없음, AI 없음, 고정 위치. Step 3에서 신규 client 접속 시
        // 본 enemy를 S_EntitySpawn으로 다발 전송 (initial roster 패턴, Phase 04 정합).
        //
        // 헌법 #5 (틱 블로킹 금지) 정합: ctor는 tick 진입 전이라 동기 코드 OK. await 없음.
        SpawnNormalEnemy(NormalEnemySpawnX, NormalEnemySpawnY, NormalEnemyMaxHp);

        // M3 Phase 07: 서버 시작 시 Boss 1마리 즉시 spawn (우측 zone). Normal과 같은 entity id 풀
        // 공유 (`_nextEntityId++`) → S_HitResult.targetEntityId 라우팅 단순화 (Step 5 ProcessAttack에서
        // GetEnemyById 한 번에 lookup). 별 BossEntity 모델 분리 X (Codex β 권장 — combat 로직 재사용,
        // StageClear trigger만 EnemyKind.Boss 분기).
        SpawnBoss(BossSpawnX, BossSpawnY, BossMaxHp);
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

    // M3 Phase 07: tick thread (또는 ctor)에서만 호출 invariant.
    // Normal과 분리 helper로 박은 이유 = 호출처 명확화 (`SpawnBoss(30, 0, 100)`가 `SpawnEnemy(Boss, 30, 0, 100)`보다
    // 의도 표현 명확). 본 마감 시 통합 SpawnEnemy(kind, ...)로 합치는 게 정석이지만 응급 = 명시 helper 2개로 유지.
    EnemyEntity SpawnBoss(float x, float y, int maxHp)
    {
        int id = _nextEntityId++;
        EnemyEntity e = new EnemyEntity(id, EnemyKind.Boss, x, y, maxHp);
        _enemies.Add(id, e);
        return e;
    }

    // virtual: 테스트 subclass에서 EnqueueJob 호출 카운트 추적 가능 (Phase 09 rate-limit drop 검증).
    public virtual void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    // tick thread에서만 호출.
    // M4.1 Phase 05 (3단계): stats 옵션 인자 추가. null 시 PlayerEntity ctor가 Warrior() 응급 default 박음.
    public PlayerEntity AddPlayer(GameSession? owner, Vector2 spawnPos, PlayerStats? stats = null)
    {
        PlayerEntity entity = new PlayerEntity(_nextEntityId++, spawnPos, owner, stats);
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
    /// M4.1 Phase 06 (4·5단계 통합 개정): tick thread 안에서 attack 1건 처리.
    /// lag compensation rewind (attacker 위치 4-tick 이내 rewinding) + AABB precision hitbox.
    ///
    /// **호출 invariant**: tick thread에서만. `GameSession.SubmitAttack`이 `EnqueueJob` 람다로 박음.
    ///
    /// **검증 순서 (헌법 #3 Trust Boundary — fail-closed silent drop)**:
    ///   1. attacker player 존재 — 없으면 silent drop.
    ///   2. target enemy 존재 — null이면 silent drop (PvP 미지원 + 죽은 target 자동 차단).
    ///   3. target alive — IsDead면 silent drop (idempotent).
    ///   4. rate-limit 500ms — AttackCooldownMs 안 재공격 silent drop.
    ///   4.5. rewind 범위 검증 (M4.1 Phase 06 신설, 헌법 #3 정합):
    ///       - attackerClientTick &lt; 0 → silent drop (음수 = 미초기화/조작).
    ///       - attackerClientTick > _currentTick → silent drop (미래 tick = 클라 조작).
    ///       - _currentTick - attackerClientTick > 4 → silent drop (200ms 초과 = cheat 후보).
    ///       - 통과 → attacker.GetPositionAtTick(attackerClientTick)으로 rewind 위치 획득.
    ///   5. AABB precision hitbox — attacker.GetAttackHitbox(rewindedPos).Intersects(target.Hitbox).
    ///      옛 `dist² &lt; AttackRangeSquared` 교체. 클라 좌표 직접 사용 X (헌법 #1/#3 정합).
    ///
    /// **통과 시 처리**:
    ///   - attacker.LastAttackTickMs 갱신 + Formulas.ComputeDamage 데미지 계산 + target.Hp 감소.
    ///   - S_HitResult broadcast 전원 (except=null — attacker 자기 포함).
    ///   - Hp ≤ 0 → S_EntityDeath broadcast + Boss 시 S_StageClear 1회 + _enemies.Remove.
    /// </summary>
    internal void ProcessAttack(int attackerEntityId, int targetEntityId, long attackerClientTick)
    {
        // 1) attacker player exists
        PlayerEntity? attacker = GetPlayer(attackerEntityId);
        if (attacker == null) return;

        // 2) target enemy exists (player id 던지면 자동 silent drop — PvP 미지원 응급 약속)
        EnemyEntity? target = GetEnemyById(targetEntityId);
        if (target == null) return;

        // 3) target alive (idempotent — kill broadcast 후 후속 attack no-op)
        if (target.IsDead) return;

        // 4) rate-limit 500ms silent drop
        long now = Environment.TickCount64;
        if (now - attacker.LastAttackTickMs < CombatConstants.AttackCooldownMs) return;

        // 4.5) M4.1 Phase 06: rewind 범위 검증 (헌법 #3 Trust Boundary — 3분기 silent drop)
        //
        // **왜 여기(rate-limit 후, range 전)인가**:
        //   rate-limit 통과 후 범위 검증 전에 끊어야 cheat가 rate-limit 우회 후
        //   무한 rewind를 시도하는 것을 막을 수 있음.
        //
        // **3분기**:
        //   (a) 음수 tick — 초기화 안 된 클라 or 조작. 헌법 #3 fail-closed.
        //   (b) 미래 tick — 클라가 아직 오지 않은 tick 보냄 = 조작.
        //   (c) 5tick 이상 전 — 200ms 초과 lag = cheat 후보 (또는 비정상 lag).
        //       4 tick = 200ms가 허용 최대 (Phase 06 설계 결정, 4-slot ring buffer 깊이와 정합).
        if (attackerClientTick < 0) return;                               // (a) 음수
        if (attackerClientTick > _currentTick) return;                    // (b) 미래
        if (_currentTick - attackerClientTick > 4) return;               // (c) 200ms 초과

        // rewind: attacker가 공격 버튼을 눌렀을 당시 tick의 서버 저장 위치로 되돌림.
        // target은 현재 위치 사용 (target rewind는 M4.3 backlog).
        Vector2 rewindedPos = attacker.GetPositionAtTick(attackerClientTick);

        // 5) AABB precision hitbox (옛 dist² < range² 교체)
        // attacker 공격 박스(3×3 unit, rewindedPos 중심) vs target 피격 박스(1×1 unit).
        AABB attackBox = GetAttackHitbox(rewindedPos);
        if (!attackBox.Intersects(target.Hitbox)) return;

        // 통과 → 권위 mutation 진입
        // M4.1 Phase 05 (3단계): 옛 고정 BaseDamage 빼기 → Formulas.ComputeDamage 위임.
        // CombatConstants.BaseDamage는 *보존* — Formulas의 baseDamage 입력으로 활용 (제거 X, 의도적 보존).
        // **헌법 #1 (Server Authority)**: 데미지 계산은 서버만. 클라는 S_HitResult.damage 수신 후 표시만.
        attacker.LastAttackTickMs = now;
        int damage = Formulas.ComputeDamage(attacker.Stats, target.Stats, CombatConstants.BaseDamage);
        target.Hp -= damage;

        S_HitResult hit = new S_HitResult
        {
            attackerEntityId = attacker.EntityId,
            targetEntityId = target.EntityId,
            damage = damage,
            currentHp = target.Hp,
            maxHp = target.MaxHp,
        };
        BroadcastToAll(hit.Write()); // 전원 (attacker 자기 포함) — except=null

        // death 처리. IsDead 체크는 step 3의 *진입 검사*가 흡수하지만 (이미 dead면 여기 도달 X),
        // damage 적용 후 Hp ≤ 0 박힌 첫 시점은 본 분기 1회 — `_enemies.Remove`로 map에서 빼면
        // 다음 attack job은 step 2(GetEnemyById null)에서 잘림 = idempotent 보장.
        if (target.Hp <= 0)
        {
            S_EntityDeath death = new S_EntityDeath { entityId = target.EntityId };
            BroadcastToAll(death.Write());

            // M3 Phase 07 (보스 + Stage Clear): Boss 사망 시 S_StageClear 1회 broadcast.
            //
            // **순서 약속** (PDL.xml 본문 박힘): S_EntityDeath → S_StageClear (lifecycle → game event).
            //   클라가 entity despawn 처리 후 stage clear UI 띄우면 자연스러운 흐름.
            //
            // **`_stageCleared` flag 1회 보장 — 이중 안전망**:
            //   (a) HP 0 후 추가 attack 도착 시 _enemies.Remove로 step 2(GetEnemyById null) silent drop —
            //       본 분기 자체에 도달 안 함.
            //   (b) 만약 보스 2마리 spawn 시나리오가 미래에 도입돼도 본 flag로 *첫 보스 사망*만 stage clear
            //       broadcast (M4 backlog 다중 보스 시 본 로직 재검토 필요).
            //
            // **헌법 #1 (Server Authority)**: 클라는 본 패킷 수신 시점이 권위 stage clear 신호.
            //   클라가 자체 보스 HP 추적해서 "0이니까 clear"라고 자체 판정 X — 본 broadcast가 단일 진실.
            //
            // **broadcast 대상**: 전원 (`except: null`) — attacker 자기도 포함. 같은 맵 전원 stage clear UI.
            if (target.Kind == EnemyKind.Boss && !_stageCleared)
            {
                _stageCleared = true;
                S_StageClear stageClear = new S_StageClear { bossEntityId = target.EntityId };
                BroadcastToAll(stageClear.Write());
            }

            _enemies.Remove(target.EntityId);
        }
    }

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
    /// M4.1 Phase 06 (5단계): attacker 위치 중심으로 공격 AABB 박스를 생성.
    /// ProcessAttack이 rewindedPos로 호출 → 그 tick 기준 박스 생성.
    ///
    /// **static 설계 이유**: 위치만 달라지는 순수 함수 (GameMap 상태 의존 X).
    ///   AttackHalfExtent = 1.5f → 전체 3×3 unit (CombatConstants.AttackRange 정합).
    /// </summary>
    static AABB GetAttackHitbox(Vector2 origin)
        => new AABB(origin, new Vector2(CombatConstants.AttackHalfExtent, CombatConstants.AttackHalfExtent));

    /// <summary>
    /// TickScheduler가 매 50ms마다 호출. 단일 thread.
    /// </summary>
    public void Tick(long tickNumber)
    {
        // M4.1 Phase 06 (4단계): _currentTick 갱신 — job 처리 *전*에 박아야
        // job(ProcessAttack 람다) 안에서 올바른 tick으로 rewind 범위 검증 가능.
        _currentTick = tickNumber;

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
        //
        // M4.1 Phase 06 (1단계): Physics.Step 완료 직후 RecordPosition 호출.
        //   "그 tick에 실제로 있던 위치"를 기록해야 rewind가 정확.
        //   Step *전* 위치 기록은 이동 반영 전 snapshot → rewind 시 1tick 느린 위치 반환 = 오류.
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
            // M4.1 Phase 06 (1단계): Physics.Step 완료 후 위치 기록.
            p.RecordPosition(tickNumber, p.Position);
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
