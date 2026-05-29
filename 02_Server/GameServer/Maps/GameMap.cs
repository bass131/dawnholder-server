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

    // M4.3 Phase 07: Normal enemy respawn 대기 큐.
    //
    // **설계 결정 — 별도 리스트 vs _enemies 안 IsWaitingRespawn 필드**:
    //   별도 리스트를 선택한 이유: IsDead enemy가 _enemies에 남아있으면 ProcessAttack의
    //   GetEnemyById → IsDead silent drop 경로에 걸려 respawn 대기 중인 적에게
    //   공격 메시지가 계속 들어올 때 O(1) drop은 유지되지만 _enemies 순회 비용이 늘어남.
    //   분리 보관이 "살아있는 적만 _enemies" invariant를 유지 — ProcessAttack/aggro 판정 등
    //   모든 기존 로직이 respawn 대기 entity를 자연스럽게 무시.
    //
    // **Boss 미포함 이유**: Boss는 StageClear 1회 이후 respawn 없음. 이 큐에 넣지 않음.
    //   죽은 Boss는 ProcessAttack의 _enemies.Remove로 완전 소멸 (기존 동작 유지).
    //
    // **헌법 #5**: tick thread invariant — lock 없음 (단일 actor 보장).
    readonly List<EnemyEntity> _respawnQueue = new();

    // M4.3 Phase 07: Normal enemy respawn 대기 틱 수 (tick 기반 타이머 — 헌법 #5 await 금지).
    // 20 TPS 기준 5초 = 100 tick.
    // **설계 결정 — 왜 100tick(5초)인가**:
    //   발표 데모 반복 시연 위해 "금방 다시 나타나는" 것이 필요. 1초(20tick)는 너무 짧아
    //   플레이어가 respawn 충격을 받을 수 있고, 10초(200tick)는 너무 길어 데모 흐름 끊김.
    //   5초는 플레이어가 respawn 위치로 걸어오는 시간과 비슷한 자연스러운 값.
    const int NormalEnemyRespawnTicks = 100; // 5초 @ 20TPS

    // M4.2 Phase 02: entity id 발급기.
    //
    // **두 가지 동작 모드**:
    //   (A) GameWorld 경유 생성: _idAllocator = GameWorld.NextEntityId — 전역 풀에서 발급.
    //       4맵 간 id가 globally-unique (같은 id가 두 맵에 동시 존재 X).
    //       ADR-026: 맵 이동 시 entity id 유지 → S_MapTransition에 entityId 필드 불필요.
    //   (B) 단독 생성 (테스트 / 미래 확장): _idAllocator = null → 로컬 _localNextId 사용 (1부터 시작).
    //       GameMap을 GameWorld 없이 독립 사용 가능 — 테스트 격리 보장.
    //
    // **Func<int> vs GameWorld 직접 참조**:
    //   Func<int>를 주입하면 GameMap이 GameWorld에 직접 의존하지 않음 → 순환 참조 없음.
    //   테스트에서 GameWorld singleton 없이도 GameMap 단독 생성 가능 (테스트 친화적).
    //   Interlocked.Increment는 GameWorld 안에서 처리 — GameMap은 "번호 뽑기 함수"만 받음.
    readonly Func<int>? _idAllocator;

    // (B) 모드 전용 로컬 카운터. (A) 모드에서는 _idAllocator()를 호출하므로 이 필드는 불사용.
    int _localNextId = 1;

    // entity id 발급 단일 경로. (A)/(B) 분기를 여기에만 박음 — SpawnEnemy/AddPlayer는 AllocId() 호출만.
    int AllocId() => _idAllocator != null ? _idAllocator() : _localNextId++;

    readonly ConcurrentQueue<Action> _pendingJobs = new();

    public IReadOnlyList<PlayerEntity> Players => _players;

    // M3 Phase 06 Step 2: 읽기 전용 노출. Step 3에서 EnterGameWorld가 active enemy roster
    // 다발 전송(S_EntitySpawn) 시 순회용 + Step 5 AttackHandler가 target lookup 보조용.
    public IReadOnlyDictionary<int, EnemyEntity> Enemies => _enemies;

    // M4.2 Phase 01 (결정 2 — Spawn 모듈화):
    // 옛 NormalEnemySpawnX/Y/MaxHp · BossSpawnX/Y/MaxHp const 제거.
    // 좌표/HP 정의는 MapSpawnTable.cs로 이동 (단일 진실 공급원).
    // 테스트 참조: GameMapContentTests가 GameMap.const 대신 MapSpawnTable에서 값 확인.

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

    // M4.2 Phase 01: 맵 ID. 어느 맵인지 식별 + GetMap 라우팅 + 로그용.
    // readonly — ctor 이후 변경 X (맵 identity는 불변).
    public MapId MapId { get; }

    // M4.2 Phase 02: 맵에 속한 portal 목록. PortalTable 단일 진실 공급원에서 가져옴.
    // IReadOnlyList — 외부에서 추가/제거 불가 (헌법 #1 Server Authority: portal 정의는 서버 권위).
    // Phase 03에서 C_EnterPortal 핸들러가 portalId로 이 목록을 lookup.
    public IReadOnlyList<Portal> Portals { get; }

    // M4.2 Phase 01 (결정 2 — Spawn 모듈화): ctor switch 분기 제거.
    // MapSpawnTable.GetSpawnsFor(mapId) → spawn 정의 목록을 받아 순서대로 spawn.
    //
    // **변경 전**: ctor 안에 switch(mapId) { HuntingGround: SpawnNormalEnemy(...); BossRoom: SpawnBoss(...); }
    // **변경 후**: foreach(def in MapSpawnTable.GetSpawnsFor(mapId)) SpawnEnemy(def);
    //
    // **이점**:
    //   - ctor에 맵별 로직 없음 — spawn 내용 변경 시 MapSpawnTable만 수정.
    //   - 맵 추가 시 MapSpawnTable에 항목 추가 + GameMap ctor는 변경 없음.
    //   - EnemyKind 분기(Normal/Boss 별도 helper) 통합 → SpawnEnemy(kind, x, y, hp) 단일 경로.
    //
    // **헌법 #5**: ctor는 tick 진입 전 → 동기 코드 OK. await/Task.Delay/Thread.Sleep 없음.
    //
    // M4.2 Phase 02: idAllocator 선택적 주입 (Func<int>? = null).
    // GameWorld 경유 생성 시 GameWorld.NextEntityId 전달 → 전역 풀.
    // 단독 생성 시 null → 로컬 카운터 (테스트 격리 보장).
    public GameMap(MapId mapId = MapId.HuntingGround, Func<int>? idAllocator = null)
    {
        MapId = mapId;
        _idAllocator = idAllocator;

        // M4.2 Phase 02: portal 목록 초기화. PortalTable 단일 진실 공급원.
        Portals = PortalTable.GetPortalsFor(mapId);

        // MapSpawnTable이 단일 진실 공급원 — 맵별 spawn 목록 반환.
        // Town/Ending은 Empty 목록 → foreach 본문 진입 X (빈 맵).
        foreach (EnemySpawnDef def in MapSpawnTable.GetSpawnsFor(mapId))
        {
            SpawnEnemy(def.Kind, def.X, def.Y, def.MaxHp);
        }
    }

    // M4.2 Phase 01 (결정 2 — Spawn 모듈화): SpawnNormalEnemy + SpawnBoss 통합.
    // 옛 두 helper는 kind 인자 하나 차이밖에 없었음 → 통합 SpawnEnemy(kind, x, y, maxHp).
    //
    // M4.3 Phase 07: Normal enemy에 EnemyStats.NormalDefault() 자동 주입.
    // Boss는 default stats (MoveSpeed/AggroRange/PatrolRange = 0) — AI 미적용 (Phase 09).
    //
    // **호출 invariant**: tick thread 또는 ctor에서만 (단일 thread invariant 유지).
    // 헌법 #5 — 동기 코드만, await/Task.Delay/Thread.Sleep 금지.
    //
    // **internal 유지 이유**: 테스트 픽스처가 직접 enemy 구성 가능 (InternalsVisibleTo).
    //   ex. AttackHandlerTests가 임의 맵에 enemy를 추가할 때 호출.
    internal EnemyEntity SpawnEnemy(EnemyKind kind, float x, float y, int maxHp)
    {
        int id = AllocId();
        // Normal enemy는 AI 파라미터 포함 stats 주입. Boss는 default (AI 없음).
        EnemyStats stats = kind == EnemyKind.Normal ? EnemyStats.NormalDefault() : default;
        EnemyEntity e = new EnemyEntity(id, kind, x, y, maxHp, stats);
        _enemies.Add(id, e);
        return e;
    }

    // virtual: 테스트 subclass에서 EnqueueJob 호출 카운트 추적 가능 (Phase 09 rate-limit drop 검증).
    public virtual void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    // tick thread에서만 호출.
    // M4.1 Phase 05 (3단계): stats 옵션 인자 추가. null 시 PlayerEntity ctor가 Warrior() 응급 default 박음.
    public PlayerEntity AddPlayer(GameSession? owner, Vector2 spawnPos, PlayerStats? stats = null)
    {
        PlayerEntity entity = new PlayerEntity(AllocId(), spawnPos, owner, stats);
        _players.Add(entity);
        return entity;
    }

    // M4.2 Phase 03: migration 전용 AddPlayer 오버로드 — 기존 entity id 유지 (ADR-026).
    //
    // **ADR-026 핵심**: 맵 이동 시 player의 entity id를 재배정하지 않는다.
    //   이 메서드가 새 id를 AllocId()로 발급하는 대신 호출자가 제공한 entityId를 그대로 사용.
    //
    // **왜 별도 오버로드인가?**
    //   기존 AddPlayer(owner, spawnPos, stats)에 entityId 옵션을 추가하면
    //   "생성 경로"와 "migration 경로"의 의도 구분이 모호해짐.
    //   오버로드로 명확히 분리 → 코드 읽는 사람이 migration임을 즉시 알 수 있음.
    //
    // **HP 복원**: stats와 별도로 currentHp를 받음. stats.Hp는 *최대* HP 기준이고
    //   migration 시 실제 HP는 전투로 깎인 상태일 수 있음.
    //
    // **호출 invariant**: tick thread에서만 (맵 B의 EnqueueJob 람다 안).
    public PlayerEntity AddPlayerWithId(int entityId, GameSession? owner, Vector2 spawnPos, PlayerStats stats, int currentHp)
    {
        PlayerEntity entity = new PlayerEntity(entityId, spawnPos, owner, stats);
        entity.Hp = currentHp;
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

            // M4.3 Phase 07: Normal enemy respawn 큐 등록.
            // Boss는 StageClear 1회성 → respawn 없음 (위 분기에서 처리 완료).
            // _respawnQueue에 원본 entity 보관 — SpawnX/SpawnY/Stats 재사용.
            // RespawnTicksRemaining 세팅 = tick 카운트다운 시작.
            if (target.Kind == EnemyKind.Normal)
            {
                target.RespawnTicksRemaining = NormalEnemyRespawnTicks;
                _respawnQueue.Add(target);
            }
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
    /// **N² 비용 인지**: 응급 모드 데모(N≤4) 환경에선 무시 가능 (100ms마다 16 패킷 = 160/s).
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

        // 3) Snapshot 브로드캐스트. 매 2 tick(=100ms).
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

        // 4) M4.3 Phase 07: Enemy FSM update 루프 (Normal enemy만 AI 적용).
        //
        // **헌법 #1 (Server Authority)**: enemy 위치/상태 판정은 서버 전담.
        // **헌법 #5 (틱 블로킹 금지)**: 동기 O(N) 처리만. await/Sleep/DB 없음.
        //
        // **2D 사이드스크롤 단순화**: X축 수평 이동만. Y/중력은 이번 scope 밖(적은 지상 고정).
        //
        // **SnapshotTickInterval 활용**: 플레이어 snapshot과 동일 주기(100ms@2tick)로 broadcast.
        //   적이 느리므로 100ms 간격으로도 클라에서 보간 충분. Phase 08에서 조정 예정.
        UpdateEnemies(tickNumber);

        // 5) M4.3 Phase 07: Respawn 처리 (Normal enemy 전용).
        ProcessRespawns(tickNumber);
    }

    /// <summary>
    /// M4.3 Phase 07: Normal enemy AI FSM 1틱 진행.
    ///
    /// **FSM 전이 규칙**:
    ///   Patrol → Chase: 같은 맵 player 중 |dx| <= AggroRange인 가장 가까운 player 발견 시.
    ///   Chase → Patrol: target이 사라지거나 |dx| > AggroRange * 1.5 (de-aggro 히스테리시스).
    ///   Patrol 경계: SpawnX ± PatrolRange 도달 시 PatrolDir 반전.
    ///
    /// **히스테리시스(hysteresis)**: aggro 진입/이탈 거리를 같게 두면 경계에서 Chase↔Patrol가
    ///   1틱마다 토글(flickering). de-aggro 거리를 1.5× 더 크게 두면 안정화.
    ///
    /// **S_EntityState broadcast**: SnapshotTickInterval 마다 모든 Normal enemy 위치/상태 전송.
    ///   100ms 시작 — Phase 08 클라 보간 체감 보고 주기 조정 예정.
    /// </summary>
    void UpdateEnemies(long tickNumber)
    {
        float dt = Constants.TickDuration; // 1틱 시간 (초)
        bool shouldBroadcast = tickNumber % Constants.SnapshotTickInterval == 0;

        foreach (EnemyEntity enemy in _enemies.Values)
        {
            // Boss는 이번 Phase에서 AI 없음 (Idle 고정). Phase 09에서 별도 behavior.
            if (enemy.Kind != EnemyKind.Normal) continue;

            float moveSpeed = enemy.Stats.MoveSpeed;
            float aggroRange = enemy.Stats.AggroRange;
            float patrolRange = enemy.Stats.PatrolRange;

            // --- aggro 판정 (Patrol 및 Chase 상태 모두에서 매 tick 재판정) ---
            // 같은 맵 player 중 |dx| <= AggroRange인 가장 가까운 player를 탐색.
            // "같은 맵"은 이미 _players가 이 맵 소속이므로 별도 필터 불필요.
            PlayerEntity? closest = null;
            float closestDist = float.MaxValue;
            foreach (PlayerEntity p in _players)
            {
                float dx = p.Position.X - enemy.X;
                float absDx = dx < 0 ? -dx : dx;
                if (absDx <= aggroRange && absDx < closestDist)
                {
                    closest = p;
                    closestDist = absDx;
                }
            }

            // --- 상태 전이 ---
            if (enemy.State == EnemyState.Patrol)
            {
                if (closest != null)
                {
                    // aggro 진입 → Chase 전환
                    enemy.State = EnemyState.Chase;
                    enemy.TargetEntityId = closest.EntityId;
                }
            }
            else if (enemy.State == EnemyState.Chase)
            {
                // 타겟 유효성 재확인
                // (1) TargetEntityId가 아직 _players에 있는지 (portal 이동/disconnect 대응)
                // (2) 거리가 de-aggro 임계 초과하지 않는지
                PlayerEntity? target = null;
                if (enemy.TargetEntityId.HasValue)
                {
                    // _players에서 targetId로 찾기 (GetPlayer 사용)
                    target = GetPlayer(enemy.TargetEntityId.Value);
                }

                bool targetLost = target == null;
                bool deAggro = false;
                if (target != null)
                {
                    float dx = target.Position.X - enemy.X;
                    float absDx = dx < 0 ? -dx : dx;
                    deAggro = absDx > aggroRange * 1.5f;
                }

                if (targetLost || deAggro)
                {
                    // de-aggro → Patrol 복귀
                    enemy.State = EnemyState.Patrol;
                    enemy.TargetEntityId = null;
                    target = null;
                }
                else if (closest != null && closest.EntityId != enemy.TargetEntityId)
                {
                    // 더 가까운 target으로 교체 (선택적 최적화 — 현재 target은 이미 범위 안)
                    enemy.TargetEntityId = closest.EntityId;
                    target = closest;
                }
            }

            // --- 이동 처리 ---
            float step = moveSpeed * dt;

            if (enemy.State == EnemyState.Patrol)
            {
                // SpawnX 중심 ±PatrolRange 왕복
                enemy.X += enemy.PatrolDir * step;

                // 경계 clamp + 방향 반전
                float leftBound  = enemy.SpawnX - patrolRange;
                float rightBound = enemy.SpawnX + patrolRange;
                if (enemy.X <= leftBound)
                {
                    enemy.X = leftBound;
                    enemy.PatrolDir = 1;
                }
                else if (enemy.X >= rightBound)
                {
                    enemy.X = rightBound;
                    enemy.PatrolDir = -1;
                }
            }
            else if (enemy.State == EnemyState.Chase && enemy.TargetEntityId.HasValue)
            {
                PlayerEntity? target = GetPlayer(enemy.TargetEntityId.Value);
                if (target != null)
                {
                    float dx = target.Position.X - enemy.X;
                    if (dx > 0f)
                        enemy.X += step;
                    else if (dx < 0f)
                        enemy.X -= step;
                    // dx == 0f 정확히 겹치면 이동 없음 (공격 판정은 ProcessAttack에서)
                }
            }

            // --- S_EntityState broadcast ---
            // SnapshotTickInterval 마다 전원에게 전송.
            // **trade-off**: 매 틱 전체 vs 변경분만 vs SnapshotTickInterval 주기.
            //   현재 = SnapshotTickInterval 주기(100ms)로 전체 Normal enemy broadcast.
            //   매 틱 전체는 20×N 패킷/s — Normal enemy 1~5마리 수준이면 40~100/s으로
            //   데모 환경에서도 부담. SnapshotTickInterval 맞춤으로 player snapshot과 동기.
            //   Phase 08 클라 보간 체감 보고 후 조정 예정.
            if (shouldBroadcast)
            {
                S_EntityState statePacket = new S_EntityState
                {
                    entityId = enemy.EntityId,
                    x = enemy.X,
                    y = enemy.Y,
                    state = (byte)enemy.State,
                };
                BroadcastToAll(statePacket.Write());
            }
        }
    }

    /// <summary>
    /// M4.3 Phase 07: Normal enemy respawn 처리 (tick 카운트다운 기반 — 헌법 #5 정합).
    ///
    /// **tick 카운트다운 패턴**:
    ///   await/Task.Delay/Thread.Sleep 금지 (헌법 #5). 대신 RespawnTicksRemaining 필드를
    ///   매 tick 감소 — 0 도달 시 SpawnX/SpawnY 위치에 새 entity 생성.
    ///
    /// **새 entityId 발급**:
    ///   respawn = 논리적으로 새 적 출현. 기존 entityId는 S_EntityDeath로 이미 클라에서
    ///   despawn됐으므로 재사용 X (헌법 #2 "은퇴 ID 재사용 금지" 정합).
    ///   AllocId()로 새 id 발급 → 클라에게 S_EntitySpawn 전송 (살아있는 적으로 인식).
    ///
    /// **S_EntitySpawn broadcast**:
    ///   respawn 시 전원에게 new S_EntitySpawn 브로드캐스트. 클라는 이를 받아
    ///   새 적 sprite를 생성 (Phase 08 처리).
    /// </summary>
    void ProcessRespawns(long tickNumber)
    {
        // 역방향 순회 — 리스트에서 항목 제거 시 인덱스 어긋남 방지
        for (int i = _respawnQueue.Count - 1; i >= 0; i--)
        {
            EnemyEntity dead = _respawnQueue[i];
            dead.RespawnTicksRemaining--;

            if (dead.RespawnTicksRemaining <= 0)
            {
                _respawnQueue.RemoveAt(i);

                // 새 entity 생성 (새 entityId, SpawnX/SpawnY 위치, 원본 MaxHp + Stats)
                int newId = AllocId();
                EnemyEntity respawned = new EnemyEntity(
                    newId,
                    dead.Kind,
                    dead.SpawnX,
                    dead.SpawnY,
                    dead.MaxHp,
                    dead.Stats);
                _enemies.Add(newId, respawned);

                Console.WriteLine($"[Map] Enemy respawned: newId={newId} at ({respawned.SpawnX},{respawned.SpawnY})");

                // 전원에게 S_EntitySpawn — 클라는 새 적 sprite 생성
                S_EntitySpawn spawnPacket = new S_EntitySpawn
                {
                    entityId = respawned.EntityId,
                    entityKind = (byte)respawned.Kind,
                    x = respawned.X,
                    y = respawned.Y,
                    currentHp = respawned.Hp,
                    maxHp = respawned.MaxHp,
                };
                BroadcastToAll(spawnPacket.Write());
            }
        }
    }
}
