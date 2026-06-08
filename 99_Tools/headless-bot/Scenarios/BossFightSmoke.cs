using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Boss fight smoke — BossStageClearSmoke 확장.
//
// BossStageClearSmoke와 차이점:
//   - BossProbe에 S_EnemyAttack(ID 20) 핸들러 추가.
//   - 보스 범위 안에 머물며 S_EnemyAttack 수신 + 자기 HP 감소 관측.
//   - 보스에게 맞아 죽어도 리스폰 후 계속 공격 가능 → BossKill + S_StageClear까지 PASS.
//   - 기존 BossStageClearSmoke는 무변경 (회귀 0).
public class BossFightSmoke
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30); // 보스 쿨다운(40틱=2s) 감안 넉넉히
    static readonly TimeSpan CooldownWait = TimeSpan.FromMilliseconds(550);
    static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(500);

    const byte BossEntityKind = 1;
    const float PreferredAttackDistance = 1.5f; // 보스 범위(±2.5f) 안에 머물기 위해 타이트하게
    // 재접근 임계값: AttackHalfExtent=1.5보다 여유를 두어 넉백(~1.26) 후에도 확실히 재진입.
    const float ReapproachThreshold = 1.0f;
    const int MaxReapproachTicks = 30;
    const int MaxKillAttempts = 40;
    const int DeadTargetRetargetCount = 3;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int BossEntityId;
        public int InitialBossHp;
        public int FinalBossHp;
        public int HitCount;
        public int StageClearCount;
        public bool SawStageClear;
        public int EnemyAttackCount;      // S_EnemyAttack 수신 횟수
        public int LastEnemyAttackDamage; // 마지막 S_EnemyAttack damage 값
        public bool SawRespawn;           // 보스에게 맞아 리스폰 발생 여부
        public int RespawnCount;          // 리스폰 횟수
    }

    // portal 좌표 상수 — PortalTable.cs와 정합.
    const float TownPortalX = 20f;
    const int TownPortalId = 1;
    const float HGPortalX = 25f;
    const int HGPortalId = 1;

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();
        FightProbe bot = new();

        try
        {
            bot.Connect(host, port);

            if (!bot.WaitConnected(DefaultTimeout))
                return Fail(result, "connect timeout");

            if (!bot.WaitHandshake(DefaultTimeout))
                return Fail(result, "S_HandshakeResult timeout");

            if (!bot.HandshakeOk)
                return Fail(result, $"handshake rejected: {bot.HandshakeReason}");

            if (!bot.WaitEnterMap(DefaultTimeout))
                return Fail(result, "S_EnterMap timeout");

            // Town → HuntingGround
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // HuntingGround → BossRoom
            await bot.MoveToPortal(HGPortalX, ct);
            bot.SendEnterPortal(HGPortalId);
            if (!await bot.WaitSecondMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — HuntingGround→BossRoom");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            S_EntitySpawn? bossSpawn = await bot.WaitForBossSpawn(DefaultTimeout, ct);
            if (bossSpawn == null)
                return Fail(result, "Boss S_EntitySpawn timeout");

            if (bossSpawn.entityId <= 0 || bossSpawn.entityKind != BossEntityKind || bossSpawn.currentHp <= 0)
                return Fail(result, $"invalid boss spawn: entityId={bossSpawn.entityId} kind={bossSpawn.entityKind} hp={bossSpawn.currentHp}");

            result.LocalEntityId = bot.LocalEntityId;
            result.BossEntityId = bossSpawn.entityId;
            result.InitialBossHp = bossSpawn.currentHp;
            result.FinalBossHp = bossSpawn.currentHp;

            // 보스 범위 안으로 이동 (±BossAttackHalfExtent=2.5f)
            await bot.MoveIntoBossRange(bossSpawn.x, ct);

            bool gotSnapshot = await bot.WaitForFirstSnapshot(DefaultTimeout, ct);
            if (!gotSnapshot)
                return Fail(result, "S_Snapshot timeout — serverTick 추적 불가");

            int currentHp = result.InitialBossHp;
            int prevHitCount = bot.HitCountFor(result.BossEntityId);

            for (int attempt = 0; currentHp > 0 && attempt < MaxKillAttempts; attempt++)
            {
                // 넉백(~1.26유닛)으로 공격 범위(1.5) 밖으로 밀려났을 수 있으므로 공격 전 재접근.
                bool inRange = await bot.EnsureInAttackRange(ct);
                if (!inRange)
                    return Fail(result, $"reapproach timeout at attempt {attempt + 1} — bot could not close to boss");

                bot.SendAttack(result.BossEntityId);
                HitEvent? hit = await bot.WaitForHitCount(result.BossEntityId, prevHitCount + 1, CooldownWait, ct);

                if (hit == null)
                {
                    // 보스 쿨다운 중일 수 있음 — 리스폰 여부 확인 후 계속
                    // (hit이 null이면 rate-limit drop이거나 아직 반응 전 — 다음 attempt로)
                    await Task.Delay(CooldownWait, ct);
                    continue;
                }

                currentHp = hit.CurrentHp;
                result.FinalBossHp = currentHp;
                prevHitCount = bot.HitCountFor(result.BossEntityId);

                if (currentHp > 0)
                    await Task.Delay(CooldownWait, ct);
            }

            if (currentHp > 0)
                return Fail(result, $"boss still alive after {MaxKillAttempts} attempts: hp={currentHp}");

            // S_StageClear 수신 대기
            StageClearEvent? stageClear = await bot.WaitForStageClearCount(
                result.BossEntityId, 1, DefaultTimeout, ct);

            if (stageClear == null)
                return Fail(result, "S_StageClear timeout after boss death");

            if (stageClear.BossEntityId != result.BossEntityId)
                return Fail(result, $"S_StageClear bossEntityId mismatch: expected={result.BossEntityId}, actual={stageClear.BossEntityId}");

            result.SawStageClear = true;
            result.StageClearCount = bot.StageClearCountFor(result.BossEntityId);
            result.HitCount = bot.HitCountFor(result.BossEntityId);

            result.EnemyAttackCount = bot.EnemyAttackCount;
            result.LastEnemyAttackDamage = bot.LastEnemyAttackDamage;
            result.SawRespawn = bot.RespawnCount > 0;
            result.RespawnCount = bot.RespawnCount;

            // 시나리오 핵심: 보스에게 맞아 HP 감소를 *관측*해야 PASS (Phase 04 완료 조건).
            if (result.EnemyAttackCount == 0)
                return Fail(result, "no S_EnemyAttack observed — boss never hit the bot");

            result.Success = true;
            return result;
        }
        finally
        {
            bot.Disconnect();
        }
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class FightProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition1 = new(false);
        readonly ManualResetEventSlim _mapTransition2 = new(false);
        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<HitEvent> _hits = new();
        readonly List<StageClearEvent> _stageClears = new();
        readonly List<EnemyAttackEvent> _enemyAttacks = new();

        BotSession? _session;
        uint _clientTick;
        volatile int _lastReceivedServerTick = 0;

        // 봇 서버 권위 X — S_Snapshot(entityId==LocalEntityId) 수신 시 갱신.
        volatile float _serverX = 0f;

        // 보스 서버 권위 X — S_EntityState 수신 시 갱신.
        volatile float _bossX = 0f;
        volatile bool _bossXInitialized = false;

        // 리스폰 추적: S_EnemyAttack.targetCurrentHp ≤ 0 (사망) 관측 후
        // 다음 공격에서 HP 양수면 리스폰으로 판정.
        bool _sawDeath;
        int _respawnCount = 0;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;
        public float SpawnX { get; private set; }

        public int EnemyAttackCount { get { lock (_gate) return _enemyAttacks.Count; } }
        public int LastEnemyAttackDamage { get { lock (_gate) return _enemyAttacks.Count > 0 ? _enemyAttacks[^1].Damage : 0; } }
        public int RespawnCount => _respawnCount;

        public void Connect(string host, int port)
        {
            _connector.Connect(
                new IPEndPoint(IPAddress.Parse(host), port),
                sessionFactory: () =>
                {
                    BotSession s = new();
                    s.OnConnectedCallback = _ =>
                    {
                        _connected.Set();
                        C_Handshake handshake = new() { clientVersion = ProtocolVersion.Current };
                        s.Send(handshake.Write());
                    };
                    s.OnDisconnectedCallback = _ => { };
                    s.OnPacketCallback = HandlePacket;
                    _session = s;
                    return s;
                });
        }

        public bool WaitConnected(TimeSpan timeout) => _connected.Wait(timeout);
        public bool WaitHandshake(TimeSpan timeout) => _handshake.Wait(timeout);
        public bool WaitEnterMap(TimeSpan timeout) => _enterMap.Wait(timeout);

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition1.IsSet, timeout, ct);
        public async Task<bool> WaitSecondMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition2.IsSet, timeout, ct);

        public async Task MoveToPortal(float portalX, CancellationToken ct)
        {
            float delta = portalX - SpawnX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Warrior().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 160);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal packet = new() { portalId = portalId };
            _session?.Send(packet.Write());
        }

        public async Task<S_EntitySpawn?> WaitForBossSpawn(TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _spawns.Any(s => s.entityKind == BossEntityKind); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _spawns.First(s => s.entityKind == BossEntityKind);
        }

        // 보스 범위(±BossAttackHalfExtent=2.5f) 안으로 이동.
        public async Task MoveIntoBossRange(float bossX, CancellationToken ct)
        {
            float desiredX = bossX > SpawnX
                ? bossX - PreferredAttackDistance
                : bossX + PreferredAttackDistance;
            float delta = desiredX - SpawnX;
            sbyte direction = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (PlayerStats.Warrior().MoveSpeed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 160);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(direction);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(250, ct);
        }

        // 공격 전 재접근 보장. 넉백으로 범위 밖에 있으면 보스 쪽으로 조향해 ReapproachThreshold 안에 들어온다.
        public async Task<bool> EnsureInAttackRange(CancellationToken ct)
        {
            for (int t = 0; t < MaxReapproachTicks; t++)
            {
                float playerX = _serverX;
                float targetX = _bossXInitialized ? _bossX : playerX;
                float dist = Math.Abs(playerX - targetX);
                if (dist <= ReapproachThreshold)
                    return true;

                sbyte dir = (targetX >= playerX) ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }

            SendMove(0);
            await Task.Delay(Constants.TickIntervalMs, ct);
            float finalDist = Math.Abs(_serverX - (_bossXInitialized ? _bossX : 0f));
            return finalDist <= ReapproachThreshold;
        }

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        public void SendAttack(int targetEntityId)
        {
            C_Attack attack = new()
            {
                targetEntityId = targetEntityId,
                attackerClientTick = _lastReceivedServerTick,
            };
            _session?.Send(attack.Write());
        }

        public async Task<HitEvent?> WaitForHitCount(int targetEntityId, int minCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => HitCountFor(targetEntityId) >= minCount, timeout, ct);
            if (!ok) return null;
            lock (_gate) return _hits.LastOrDefault(h => h.TargetEntityId == targetEntityId);
        }

        public async Task<StageClearEvent?> WaitForStageClearCount(int bossEntityId, int minCount, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(() => StageClearCountFor(bossEntityId) >= minCount, timeout, ct);
            if (!ok) return null;
            lock (_gate) return _stageClears.LastOrDefault(s => s.BossEntityId == bossEntityId);
        }

        public int HitCountFor(int targetEntityId)
        {
            lock (_gate) return _hits.Count(h => h.TargetEntityId == targetEntityId);
        }

        public int StageClearCountFor(int bossEntityId)
        {
            lock (_gate) return _stageClears.Count(s => s.BossEntityId == bossEntityId);
        }

        public void Disconnect() => _session?.Disconnect();

        void SendMove(sbyte inputX)
        {
            _clientTick++;
            C_MoveIntent move = new()
            {
                input = InputBits.Encode(inputX, jumpPressed: false),
                clientTick = _clientTick,
            };
            _session?.Send(move.Write());
        }

        void HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 4) return;

            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2));
            switch ((PacketID)id)
            {
                case PacketID.S_HandshakeResult:
                    S_HandshakeResult handshake = new();
                    handshake.Read(buffer);
                    HandshakeOk = handshake.ok;
                    HandshakeReason = handshake.reason;
                    if (handshake.ok)
                    {
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Warrior };
                        _session?.Send(charSelect.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    SpawnX = enterMap.spawnX;
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition mapTransition = new();
                    mapTransition.Read(buffer);
                    SpawnX = mapTransition.spawnX;
                    if (!_mapTransition1.IsSet) _mapTransition1.Set();
                    else _mapTransition2.Set();
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn spawn = new();
                    spawn.Read(buffer);
                    lock (_gate)
                    {
                        _spawns.Add(spawn);
                        // 보스 스폰 시 초기 좌표 캐시 — S_EntityState 수신 전에도 재접근 기준으로 활용.
                        if (spawn.entityKind == BossEntityKind)
                        {
                            _bossX = spawn.x;
                            _bossXInitialized = true;
                        }
                    }
                    break;

                case PacketID.S_HitResult:
                    S_HitResult hit = new();
                    hit.Read(buffer);
                    lock (_gate)
                        _hits.Add(new HitEvent(hit.attackerEntityId, hit.targetEntityId, hit.damage, hit.currentHp, hit.maxHp));
                    break;

                case PacketID.S_EntityDeath:
                    // 보스 사망 — 별도 추적 불필요 (StageClear로 판단)
                    break;

                case PacketID.S_StageClear:
                    S_StageClear stageClear = new();
                    stageClear.Read(buffer);
                    lock (_gate) _stageClears.Add(new StageClearEvent(stageClear.bossEntityId));
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    _lastReceivedServerTick = snapshot.serverTick;
                    // 자기 자신 snapshot → 봇 서버 권위 위치 갱신 (재접근 헬퍼 기준 좌표).
                    if (snapshot.entityId == LocalEntityId)
                        _serverX = snapshot.x;
                    break;

                case PacketID.S_EntityState:
                    // 보스 live 위치 — 재접근 헬퍼가 최신 보스 좌표로 방향 계산.
                    S_EntityState entityState = new();
                    entityState.Read(buffer);
                    lock (_gate)
                    {
                        bool isBoss = _spawns.Any(s => s.entityId == entityState.entityId && s.entityKind == BossEntityKind);
                        if (isBoss)
                        {
                            _bossX = entityState.x;
                            _bossXInitialized = true;
                        }
                    }
                    break;

                case PacketID.S_EnemyAttack:
                    // 보스→플레이어 공격 수신. 자기 자신이 타겟인 경우만 카운트.
                    S_EnemyAttack enemyAttack = new();
                    enemyAttack.Read(buffer);
                    if (enemyAttack.targetId == LocalEntityId)
                    {
                        lock (_gate)
                        {
                            _enemyAttacks.Add(new EnemyAttackEvent(
                                enemyAttack.attackerId,
                                enemyAttack.targetId,
                                enemyAttack.damage,
                                enemyAttack.targetCurrentHp,
                                enemyAttack.attackPattern));

                            if (enemyAttack.targetCurrentHp <= 0)
                            {
                                _sawDeath = true;
                            }
                            else if (_sawDeath)
                            {
                                _respawnCount++;
                                _sawDeath = false;
                            }
                        }
                    }
                    break;
            }
        }

        static async Task<bool> WaitUntil(Func<bool> predicate, TimeSpan timeout, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (predicate()) return true;
                await Task.Delay(25, ct);
            }
            return predicate();
        }
    }

    sealed record HitEvent(int AttackerEntityId, int TargetEntityId, int Damage, int CurrentHp, int MaxHp);
    sealed record StageClearEvent(int BossEntityId);
    sealed record EnemyAttackEvent(int AttackerId, int TargetId, int Damage, int TargetCurrentHp, byte AttackPattern);
}
