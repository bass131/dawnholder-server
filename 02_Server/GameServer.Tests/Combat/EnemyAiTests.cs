using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Combat;

/// <summary>
/// Enemy AI FSM 단위 테스트.
///
/// **검증 대상**:
///   1. Patrol_BounceAtLeftBoundary  — 좌측 경계 도달 시 PatrolDir 반전 (왼→오)
///   2. Patrol_BounceAtRightBoundary — 우측 경계 도달 시 PatrolDir 반전 (오→왼)
///   3. Aggro_TransitionsToChase     — AggroRange 안에 player 진입 시 Chase 전환 + TargetEntityId 설정
///   4. DeAggro_ReturnsToPatrol      — Chase 중 target이 AggroRange*1.5 벗어나면 Patrol 복귀
///   5. Chase_MovesTowardTarget      — Chase 상태에서 enemy.X가 target 방향으로 이동
///   6. Boss_StaysIdle               — Boss enemy는 Idle 유지 (AI 미적용)
///   7. Respawn_NormalEnemyRespawns  — Normal enemy 사망 후 RespawnTicks 경과 시 재출현
///   8. Respawn_BossNeverRespawns    — Boss 사망 후 respawn 없음 (StageClear 1회성)
///
/// **테스트 전략**:
///   - GameMap 단독 생성(idAllocator=null) → GameWorld 싱글톤 의존 X (테스트 격리).
///   - SpawnEnemy(internal)로 직접 구성.
///   - GameMap.Tick(long) 직접 호출로 FSM 진행 → state 확인.
///   - 플레이어는 null owner AddPlayer로 삽입 (패킷 전송 X — FSM 로직만 검증).
/// </summary>
public class EnemyAiTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// null owner + 지정 위치로 player 추가. broadcast 없음 (owner null skip 정합).
    /// 반환된 PlayerEntity로 위치 조작 가능.
    /// </summary>
    static PlayerEntity AddPlayerAt(GameMap map, float x, float y)
    {
        PlayerEntity p = map.AddPlayer(null, new Vector2(x, y));
        return p;
    }

    /// <summary>
    /// HuntingGround(Normal enemy 1마리) 맵 생성 — content 주입 (MapSpawnTable 은퇴, M4.4 Phase 03).
    /// Normal enemy entityId=1 (로컬 카운터 1부터 시작 — GameWorld 없으므로).
    /// </summary>
    static GameMap MakeHuntingGround()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        return new GameMap(MapId.HuntingGround, content: content);
    }

    static GameMap MakeBossRoom()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 30f, 0f),
        });
        return new GameMap(MapId.BossRoom, content: content);
    }

    // ── 테스트: Patrol 왕복 경계 반전 ──────────────────────────────────────────

    /// <summary>
    /// Patrol 중 좌측 경계(SpawnX - PatrolRange) 도달 시 PatrolDir이 +1(오른쪽)으로 반전.
    ///
    /// 설정:
    ///   SpawnX=10, PatrolRange=4 → leftBound=6.
    ///   PatrolDir=-1(왼쪽)로 강제 설정 후 tick을 충분히 돌려 경계에 도달시킴.
    /// </summary>
    [Fact]
    public void Patrol_BounceAtLeftBoundary()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);
        Assert.Equal(EnemyKind.Normal, enemy!.Kind);

        // 왼쪽 방향으로 설정 + 경계 바로 오른쪽에 배치
        float leftBound = enemy.SpawnX - enemy.Stats.PatrolRange;
        enemy.X = leftBound + 0.01f; // 경계 직전
        enemy.PatrolDir = -1;

        // 1틱 진행 → 경계 도달 또는 초과 → 반전
        map.Tick(1);

        Assert.Equal(EnemyState.Patrol, enemy.State);
        // PatrolDir은 +1로 반전됐어야 함
        Assert.Equal(1, enemy.PatrolDir);
        // X는 leftBound에 clamp됐거나 넘어갔어도 반전 후 오른쪽으로 이동 시작
        Assert.True(enemy.X >= leftBound - 0.001f, $"X={enemy.X} should be >= leftBound={leftBound}");
    }

    /// <summary>
    /// Patrol 중 우측 경계(SpawnX + PatrolRange) 도달 시 PatrolDir이 -1(왼쪽)으로 반전.
    /// </summary>
    [Fact]
    public void Patrol_BounceAtRightBoundary()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        float rightBound = enemy.SpawnX + enemy.Stats.PatrolRange;
        enemy.X = rightBound - 0.01f; // 경계 직전
        enemy.PatrolDir = 1;

        map.Tick(1);

        Assert.Equal(EnemyState.Patrol, enemy.State);
        Assert.Equal(-1, enemy.PatrolDir);
        Assert.True(enemy.X <= rightBound + 0.001f, $"X={enemy.X} should be <= rightBound={rightBound}");
    }

    // ── 테스트: Aggro 진입 → Chase 전환 ───────────────────────────────────────

    /// <summary>
    /// AggroRange 안에 player 진입 시 enemy.State = Chase + TargetEntityId 설정.
    ///
    /// NormalDefault AggroRange=6. enemy.X=10, player.X=13 (|dx|=3 < 6) → aggro 진입.
    /// </summary>
    [Fact]
    public void Aggro_TransitionsToChase()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // enemy를 X=10(SpawnX)에 유지, player를 AggroRange(6) 안 X=13에 배치
        enemy.X = enemy.SpawnX; // 10
        PlayerEntity player = AddPlayerAt(map, enemy.X + 3f, 0f); // |dx|=3 < AggroRange=6

        map.Tick(1);

        Assert.Equal(EnemyState.Chase, enemy!.State);
        Assert.Equal(player.EntityId, enemy.TargetEntityId);
    }

    /// <summary>
    /// AggroRange 밖 player는 aggro 미진입 — Patrol 유지.
    /// |dx| = AggroRange + 1 → threshold 초과.
    /// </summary>
    [Fact]
    public void Aggro_OutOfRange_StaysPatrol()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        float outsideX = enemy.X + enemy.Stats.AggroRange + 1f; // 범위 밖
        AddPlayerAt(map, outsideX, 0f);

        map.Tick(1);

        Assert.Equal(EnemyState.Patrol, enemy!.State);
        Assert.Null(enemy.TargetEntityId);
    }

    // ── 테스트: De-aggro → Patrol 복귀 ───────────────────────────────────────

    /// <summary>
    /// Chase 상태에서 target이 AggroRange*1.5를 벗어나면 Patrol 복귀.
    ///
    /// 설정: enemy를 Chase 상태로 강제 설정 후 target을 AggroRange*1.5 너머로 배치.
    /// tick 1회 후 Patrol 복귀 + TargetEntityId null 검증.
    /// </summary>
    [Fact]
    public void DeAggro_BeyondHysteresis_ReturnsToPatrol()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // player를 de-aggro 임계 밖에 배치
        float deAggroThreshold = enemy.Stats.AggroRange * 1.5f;
        float farAwayX = enemy.X + deAggroThreshold + 1f;
        PlayerEntity player = AddPlayerAt(map, farAwayX, 0f);

        // Chase 상태로 강제 설정 (aggro 진입 없이 de-aggro 경로 직접 검증)
        enemy.State = EnemyState.Chase;
        enemy.TargetEntityId = player.EntityId;

        map.Tick(1);

        Assert.Equal(EnemyState.Patrol, enemy!.State);
        Assert.Null(enemy.TargetEntityId);
    }

    /// <summary>
    /// Chase 상태에서 target이 AggroRange*1.5 이내면 Chase 유지 (히스테리시스 안쪽).
    ///
    /// |dx| = AggroRange*1.2 (< 1.5×AggroRange) → de-aggro 미발동 → Chase 유지.
    /// </summary>
    [Fact]
    public void DeAggro_InsideHysteresis_StaysChase()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // AggroRange*1.2 거리 — de-aggro 임계(1.5×) 안쪽
        float insideX = enemy.X + enemy.Stats.AggroRange * 1.2f;
        PlayerEntity player = AddPlayerAt(map, insideX, 0f);

        enemy.State = EnemyState.Chase;
        enemy.TargetEntityId = player.EntityId;

        map.Tick(1);

        Assert.Equal(EnemyState.Chase, enemy!.State);
        Assert.Equal(player.EntityId, enemy.TargetEntityId);
    }

    // ── 테스트: Chase 이동 방향 ───────────────────────────────────────────────

    /// <summary>
    /// Chase 상태에서 target이 오른쪽에 있으면 enemy.X가 증가 (오른쪽으로 이동).
    /// </summary>
    [Fact]
    public void Chase_MovesRight_WhenTargetIsToRight()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // target을 AggroRange 안쪽 오른쪽에 배치
        float targetX = enemy.X + enemy.Stats.AggroRange * 0.5f;
        PlayerEntity player = AddPlayerAt(map, targetX, 0f);

        enemy.State = EnemyState.Chase;
        enemy.TargetEntityId = player.EntityId;
        float beforeX = enemy.X;

        map.Tick(1);

        // Chase → target 방향(오른쪽)으로 이동했어야 함
        Assert.True(enemy.X > beforeX, $"Expected X to increase: before={beforeX}, after={enemy.X}");
    }

    /// <summary>
    /// Chase 상태에서 target이 왼쪽에 있으면 enemy.X가 감소 (왼쪽으로 이동).
    /// </summary>
    [Fact]
    public void Chase_MovesLeft_WhenTargetIsToLeft()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // target을 AggroRange 안쪽 왼쪽에 배치
        float targetX = enemy.X - enemy.Stats.AggroRange * 0.5f;
        PlayerEntity player = AddPlayerAt(map, targetX, 0f);

        enemy.State = EnemyState.Chase;
        enemy.TargetEntityId = player.EntityId;
        float beforeX = enemy.X;

        map.Tick(1);

        Assert.True(enemy.X < beforeX, $"Expected X to decrease: before={beforeX}, after={enemy.X}");
    }

    // ── 테스트: Boss는 Idle 유지 ───────────────────────────────────────────────

    /// <summary>
    /// Boss는 player가 AggroRange 안에 들어와도 Idle 유지 (Boss는 AI 미적용).
    ///
    /// BossRoom 맵 생성 → Boss spawn 확인 → player를 보스 바로 옆에 배치 → tick → Idle 유지.
    /// </summary>
    [Fact]
    public void Boss_StaysIdle_WhenPlayerNearby()
    {
        // BossRoom 맵 (Boss 1마리 spawn)
        GameMap map = MakeBossRoom();
        EnemyEntity? boss = null;
        foreach (EnemyEntity e in map.Enemies.Values) { boss = e; break; }
        Assert.NotNull(boss);
        Assert.Equal(EnemyKind.Boss, boss!.Kind);
        Assert.Equal(EnemyState.Idle, boss.State);

        // 보스 바로 옆에 player 배치 (AggroRange 개념 없지만 가까이)
        AddPlayerAt(map, boss.X + 1f, 0f);
        float bossXBefore = boss.X;

        map.Tick(1);

        // Boss는 Idle 유지 + 이동 없음
        Assert.Equal(EnemyState.Idle, boss.State);
        Assert.Null(boss.TargetEntityId);
        Assert.Equal(bossXBefore, boss.X); // 위치 변화 없음
    }

    // ── 테스트: Respawn ────────────────────────────────────────────────────────

    /// <summary>
    /// Normal enemy 사망 → NormalEnemyRespawnTicks 경과 후 새 entity 출현.
    ///
    /// 검증:
    ///   - 사망 후 _enemies에서 제거됨.
    ///   - NormalEnemyRespawnTicks tick 진행 후 _enemies.Count == 1 (새 entity).
    ///   - 새 entity는 SpawnX/SpawnY에 배치, HP는 MaxHp.
    ///   - 새 entityId != 옛 entityId (헌법 #2 ID 재사용 금지).
    ///
    /// **NormalEnemyRespawnTicks**: RespawnSystem 내부 const(100). 여기서는 100을 리터럴로 사용
    ///   (const는 internal — 테스트에서 직접 접근 불가).
    ///   값이 바뀌면 이 테스트도 갱신 필요 (의도적 coupling — 정책 값의 변경을 테스트가 잡음).
    /// </summary>
    [Fact]
    public void Respawn_NormalEnemy_ReappearsAfterTicks()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        int originalId = enemy!.EntityId;
        float spawnX = enemy.SpawnX;
        float spawnY = enemy.SpawnY;
        int maxHp = enemy.MaxHp;

        // 사망 처리는 ProcessAttack 경로로만 일어나므로 null-owner player를 enemy 옆에 배치하고
        // 공격해 사망 → respawn 검증. ProcessAttack은 GetPlayer(attackerId) 성공이 전제.
        //   - player null owner → Send 호출 없음 (BroadcastToAll owner null skip).
        //   - rate-limit 우회: player.LastAttackTickMs = 0.
        //   - rewind 범위 검증 우회: tick을 1 이상으로 설정하고 attackerClientTick=1.
        //   - AABB 범위 검증: attacker를 enemy 바로 옆에 배치 (AttackHalfExtent=1.5 → 3×3 박스).

        // 1) null-owner player 추가 (enemy 바로 옆)
        float attackerX = enemy.X + 1f; // AABB 3×3 안에 들어옴 (AttackHalfExtent=1.5)
        PlayerEntity attacker = map.AddPlayer(null, new Vector2(attackerX, 0f));

        // 2) 충분한 damage로 enemy HP 0 만들기 — BaseDamage 여러 번 또는 HP 직접 조작
        // 직접 HP 1로 설정 (다음 attack에서 확실히 사망)
        enemy.Hp = 1;

        // 3) Tick(1) 전 rate-limit 우회
        attacker.LastAttackTickMs = 0;

        // 4) ProcessAttack 직접 호출 (tick thread 동기 — 테스트에서 직접 OK)
        // map.ProcessAttack은 internal — InternalsVisibleTo 허용
        map.ProcessAttack(attacker.EntityId, originalId, attackerClientTick: 1);
        // 이 시점에서 Tick이 아직 안 됐으므로 _currentTick = 0.
        // attackerClientTick=1 > _currentTick=0 → (b) 미래 tick silent drop 발생!
        // → Tick을 먼저 돌려 _currentTick을 1 이상으로 설정.

        // 재설정: Tick(1) 먼저 돌림 → _currentTick=1 → 그 다음 ProcessAttack
        // Tick이 돌면 Physics.Step도 돌지만 AI 루프가 player null owner는 Send 없으므로 OK.
        enemy.Hp = 1; // 다시 1로 (위 ProcessAttack이 실패했으므로 hp는 그대로)
        attacker.LastAttackTickMs = 0;

        // Tick(1) 호출 — _currentTick = 1 세팅 + enemy FSM 진행
        // (이 tick에서 aggro 체크 가능하지만 attack은 아직 없음)
        map.Tick(1);

        // Tick(2) 안에서 ProcessAttack EnqueueJob 방식이 아니라 직접 호출 방식으로 테스트
        // → Tick(1) 이후 _currentTick=1. ProcessAttack(attackerClientTick=1) 호출.
        // rate-limit 재우회
        attacker.LastAttackTickMs = 0;
        // RecordPosition은 Tick(1)에서 이미 완료됨 → GetPositionAtTick(1) 가능.
        // 단 attacker 위치가 enemy AABB 안에 있어야 함.
        // attacker.Position은 Physics.Step으로 이동했을 수 있으므로 재배치.
        attacker.Position = new Vector2(enemy.X + 1f, 0f);
        attacker.RecordPosition(1, attacker.Position); // 수동으로 tick=1 위치 기록

        map.ProcessAttack(attacker.EntityId, originalId, attackerClientTick: 1);

        // enemy가 사망했어야 함 (Hp 1 → 데미지 ≥ 1 → Hp ≤ 0)
        Assert.True(map.Enemies.ContainsKey(originalId) == false,
            $"Enemy should be removed after death. Enemies: {string.Join(",", map.Enemies.Keys)}");

        // respawn 대기 중 (아직 새 entity 없음)
        Assert.Empty(map.Enemies);

        // NormalEnemyRespawnTicks = 100. 100틱 진행.
        // 플레이어는 여전히 맵에 있고 null owner → BroadcastToAll Send 없음.
        const int RespawnTicks = 100;
        for (int i = 2; i <= RespawnTicks + 1; i++)
        {
            map.Tick(i);
        }

        // respawn 완료 — _enemies에 새 entity 1마리
        Assert.Single(map.Enemies);

        EnemyEntity? respawned = null;
        foreach (EnemyEntity e in map.Enemies.Values) { respawned = e; break; }
        Assert.NotNull(respawned);

        // 새 ID (헌법 #2 은퇴 ID 재사용 금지)
        Assert.NotEqual(originalId, respawned!.EntityId);
        // SpawnX/SpawnY 위치
        Assert.Equal(spawnX, respawned.X, precision: 2);
        Assert.Equal(spawnY, respawned.Y, precision: 2);
        // HP 초기화
        Assert.Equal(maxHp, respawned.Hp);
        Assert.Equal(maxHp, respawned.MaxHp);
        // 상태는 Patrol 시작
        Assert.Equal(EnemyState.Patrol, respawned.State);
    }

    /// <summary>
    /// Boss는 사망 후 respawn 없음 (_respawnQueue 미등록).
    ///
    /// BossRoom → Boss 사망(HP 0 직접 조작) → 100tick 경과 → _enemies 여전히 비어있음.
    /// </summary>
    [Fact]
    public void Respawn_Boss_NeverRespawns()
    {
        GameMap map = MakeBossRoom();
        EnemyEntity? boss = null;
        foreach (EnemyEntity e in map.Enemies.Values) { boss = e; break; }
        Assert.NotNull(boss);
        int bossId = boss!.EntityId;

        // player 추가 (ProcessAttack 경로)
        float attackerX = boss.X + 1f;
        PlayerEntity attacker = map.AddPlayer(null, new Vector2(attackerX, 0f));

        // Tick(1) → _currentTick=1
        map.Tick(1);

        // HP 1로 직접 설정 후 ProcessAttack
        boss.Hp = 1;
        attacker.LastAttackTickMs = 0;
        attacker.Position = new Vector2(boss.X + 1f, 0f);
        attacker.RecordPosition(1, attacker.Position);

        map.ProcessAttack(attacker.EntityId, bossId, attackerClientTick: 1);

        Assert.False(map.Enemies.ContainsKey(bossId), "Boss should be removed after death");
        Assert.Empty(map.Enemies);

        // 100틱 이상 진행
        for (int i = 2; i <= 110; i++)
        {
            map.Tick(i);
        }

        // Boss respawn 없음 — 여전히 빈 _enemies
        Assert.Empty(map.Enemies);
        // StageClear flag true (Boss 사망 시 1회 broadcast)
        Assert.True(map.IsStageCleared);
    }

    // ── 테스트: S_EntityState broadcast 검증 ─────────────────────────────────

    /// <summary>
    /// SnapshotTickInterval(=2) 마다 Normal enemy의 S_EntityState가 broadcast됨.
    ///
    /// null-owner player를 사용하면 BroadcastToAll이 owner null skip → Send 안 됨.
    /// 대신 TestSession(Send override)으로 패킷 캡처.
    /// </summary>
    [Fact]
    public void EntityStateBroadcast_OnSnapshotInterval()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // Send 캡처용 minimal TestSession (owner null 대신 실제 Send 캡처)
        var sentPackets = new List<byte[]>();
        var session = new FakeCapturingSession(sentPackets);

        // player 추가 (null owner 대신 fake session)
        PlayerEntity player = map.AddPlayer(session, new Vector2(0f, 0f));
        map.Tick(1); // tick=1 → SnapshotTickInterval=2이면 broadcast X (1%2 != 0)
        // Snapshot 패킷만 있을 수 있음 (tick=1 % 2 != 0이면 아무것도 없음)
        int countBeforeInterval = sentPackets.Count(p => IsEntityStatePacket(p));
        Assert.Equal(0, countBeforeInterval); // tick=1은 broadcast 안 함

        sentPackets.Clear();
        map.Tick(2); // tick=2 → 2%2 == 0 → broadcast

        int entityStateCount = sentPackets.Count(p => IsEntityStatePacket(p));
        Assert.True(entityStateCount >= 1, $"Expected at least 1 S_EntityState packet at tick=2, got {entityStateCount}");

        // 패킷 내용 검증: entityId 정합
        byte[]? statePkt = sentPackets.FirstOrDefault(p => IsEntityStatePacket(p));
        Assert.NotNull(statePkt);
        S_EntityState parsed = new S_EntityState();
        parsed.Read(new ArraySegment<byte>(statePkt!));
        Assert.Equal(enemy!.EntityId, parsed.entityId);
    }

    static bool IsEntityStatePacket(byte[] payload)
    {
        if (payload.Length < 4) return false;
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (Shared.Protocol.PacketID)id == Shared.Protocol.PacketID.S_EntityState;
    }

    // ── FakeCapturingSession ───────────────────────────────────────────────────

    /// <summary>
    /// Send 호출을 캡처하는 최소 세션 mock.
    /// BroadcastToAll이 owner null skip을 하므로, owner를 non-null로 주입할 때 이 클래스 사용.
    /// IsClosing = false 고정 (lifecycle race 없음 — 단위 테스트).
    /// </summary>
    sealed class FakeCapturingSession : Dawnholder.Server.GameServer.Sessions.GameSession
    {
        readonly List<byte[]> _sink;

        public FakeCapturingSession(List<byte[]> sink) { _sink = sink; }

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            _sink.Add(copy);
        }

        protected override Dawnholder.Server.GameServer.Maps.GameMap? GetMap() => null;

        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }
}
