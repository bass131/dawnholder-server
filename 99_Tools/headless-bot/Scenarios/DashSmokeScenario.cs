using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M4.9 Knight Dash 회귀 스모크.
//
// 검증 목표 (Phase 03 qa 명세 준수):
//   ① S_SkillCast(skillId=Dash=2) 수신 (casterEntityId == 자기 자신).
//   ② 이후 S_Snapshot에서 자기 위치가 전방으로 이동 확인 (시전 직전 X 대비 DashBoxHalfX 이상 증가).
//   ③ 경로 적 있으면 S_HitResult(hitEffect=3) 수신.
//   ③-neg 경로 밖 적(x 거리 2.5~4.0 사이)은 S_HitResult 미수신 — 부정 케이스.
//   ④ 쿨다운 중 재시전 무반응 — S_SkillCast 추가 수신 없음.
//   ⑤ (게이트 회귀) Mage 클래스로 Dash 송신 → S_SkillCast 미수신.
//
// 흐름: Town → HuntingGround 포털 → Normal 적 스폰 대기 → 봇이 적 근처 접근
//   → serverTick 확보 → C_SkillUse(Dash) → 검증.
//
// 클래스 게이트 검증(⑤)은 별도 Mage 봇이 같은 서버에 연결해 Dash 송신 후 S_SkillCast 부재 확인.
// fresh 서버 단독 실행 관례 — 몬스터풀 교차오염 회피 (M4.6 학습).
public class DashSmokeScenario
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    static readonly TimeSpan SkillArrivalTimeout = TimeSpan.FromSeconds(5);

    const float TownPortalX = 20f;
    const int   TownPortalId = 1;
    const byte  NormalKind = 0;
    const byte  DashSkillId = (byte)SkillId.Dash;

    // hitEffect=3 = Dash 타격
    const byte HitEffectDash = 3;

    // Dash 이후 위치 이동 최소 임계.
    // 고정거리 등속: DashSpeed(10) × DashTravelTicks(8) × TickDuration(0.05) = 4.0 unit.
    // 하한 3.5f = 옛 모멘텀 감쇠 모델(~2.43 unit) 회귀 시 FAIL, 새 등속(4.0 unit) 시 PASS.
    // Snapshot 수신 타이밍 여유로 정확히 4.0에 못 미칠 수 있어 3.5f로 설정 (네트워크 노이즈 허용).
    const float MinPositionAdvanceX = 3.5f;

    // 쿨다운 재시전 무반응 검증 대기 (쿨다운 중인 1틱 내). 3틱 여유.
    const int CooldownCheckAfterTicks = 3;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        // ①
        public bool SawSkillCast;
        public byte SkillCastSkillId;
        // ②
        public float PositionBeforeDash;
        public float PositionAfterDash;
        public bool PositionAdvanced;
        // ③ 경로 내 적 타격
        public bool PathEnemyFound;
        public bool SawHitResultDash;
        // ③-neg 경로 밖 적 미타격 (부정 케이스 — x 거리 2.5~4.0 사이)
        public bool OutsidePathEnemyFound;
        public bool OutsidePathEnemyNotHit;
        // ④
        public bool CooldownRejectedRecast;
        // ⑤
        public bool MageClassGateBlocked;
    }

    public static async Task<Result> Run(
        string host, int port,
        CancellationToken ct = default)
    {
        Result result = new();

        // ── Knight 봇 메인 흐름 ────────────────────────────────────────────────
        DashProbe bot = new(CharacterClass.Knight);

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

            result.LocalEntityId = bot.LocalEntityId;

            // Town → HuntingGround
            await bot.MoveToPortal(TownPortalX, ct);
            bot.SendEnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "S_MapTransition timeout — Town→HuntingGround");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // Normal 적 스폰 대기
            if (!await bot.WaitForSpawns(minCount: 1, kind: NormalKind, DefaultTimeout, ct))
                return Fail(result, "S_EntitySpawn (Normal) timeout");

            // 첫 서버 tick 확보
            if (!await bot.WaitForFirstSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout");

            // 적 근처 이동 — 실시간 추적으로 FacingDir=+1(오른쪽) Dash 타격 위치 확보.
            //
            // TrackAndApproachEnemy: 매 iteration GetFirstNormalCurrentX()로 적 현재 위치 갱신,
            //   봇이 항상 적 왼쪽 1 unit에 수렴하도록 이동. AI 이동 중인 적도 추적 가능.
            //
            // DashBox 타격 범위 (FacingDir=+1):
            //   boxOrigin.x = botX + 2.5, 범위 = [botX, botX + 5.0]
            // 봇이 enemyX - 1.0에 있으면 enemyX ∈ [botX, botX + 5.0] → 반드시 타격.
            await bot.TrackAndApproachEnemy(ct);
            // WaitForNextSnapshot: 이동 후 서버 최신 위치 Snapshot 대기.
            if (!await bot.WaitForNextSnapshot(DefaultTimeout, ct))
                return Fail(result, "S_Snapshot timeout (after approach)");

            // 추가 1틱 대기 — AI 이동 등으로 _entityCurrentX 최신화 여유
            await Task.Delay(Constants.TickIntervalMs, ct);
            result.PositionBeforeDash = bot.CurrentX;
            float firstNormalX = bot.GetFirstNormalCurrentX(); // 하드 게이트 오류 메시지용
            result.PathEnemyFound = bot.HasNormalInDashPath();

            // ③ 하드 게이트: 경로 내 적이 없으면 FAIL (스킵-PASS 금지).
            if (!result.PathEnemyFound)
                return Fail(result, $"③ hard gate: no Normal enemy inside server DashBox after approach — botX={result.PositionBeforeDash:F2} facingDir={bot.FacingDir} enemyCurrentX={firstNormalX:F2}. spawn layout changed or enemy moved out of range.");

            // ① C_SkillUse(Dash) 발사
            bot.SendSkillUse(DashSkillId);

            S_SkillCast? cast = await bot.WaitForSkillCast(bot.LocalEntityId, DashSkillId, SkillArrivalTimeout, ct);
            if (cast == null)
                return Fail(result, "S_SkillCast(skillId=Dash) not received after C_SkillUse");

            result.SawSkillCast = true;
            result.SkillCastSkillId = cast.skillId;

            if (cast.skillId != DashSkillId)
                return Fail(result, $"S_SkillCast.skillId expected {DashSkillId} got {cast.skillId}");

            // ② S_Snapshot 위치 전진 확인 — lunge 감쇠 8틱 + 여유 2틱 대기 후 Snapshot 수신.
            // dashWaitMs 대기로 위치가 안정된 뒤 Snapshot을 명시적으로 기다림.
            int dashWaitMs = 10 * Constants.TickIntervalMs;
            await Task.Delay(dashWaitMs, ct);
            // Dash 이후 새 Snapshot이 도착할 때까지 대기 (최대 3틱=150ms 여유).
            if (!await bot.WaitForNextSnapshot(TimeSpan.FromSeconds(1), ct))
                return Fail(result, "Dash 후 Snapshot 미도착 (timeout) — 위치 판정 불가");
            result.PositionAfterDash = bot.CurrentX;
            float advance = result.PositionAfterDash - result.PositionBeforeDash;

            // facing 방향에 따라 advance 부호가 다름 — 절댓값 판정.
            // MinPositionAdvanceX = 3.5f: 고정거리 등속 4.0 unit 기준.
            // 옛 모멘텀 감쇠 모델(~2.43 unit)로 회귀하면 2.43 < 3.5 → FAIL.
            result.PositionAdvanced = Math.Abs(advance) >= MinPositionAdvanceX;

            if (!result.PositionAdvanced)
                return Fail(result, $"Dash position advance too small: before={result.PositionBeforeDash:F2} after={result.PositionAfterDash:F2} advance={advance:F2} min={MinPositionAdvanceX}");

            // ③ 경로 내 적 타격 확인 — 하드 게이트 통과 후 PathEnemyFound는 항상 True.
            // 서버가 S_HitResult(hitEffect=3)를 보내야 Dash 타격 경로가 동작한 것.
            {
                S_HitResult? hr = bot.GetHitResultByEffect(HitEffectDash);
                if (hr == null)
                    return Fail(result, "Path enemy found but no S_HitResult(hitEffect=3) received");
                result.SawHitResultDash = true;
            }

            // ③-neg 경로 밖 적(x 거리 2.5~4.0 사이) 미타격 부정 케이스.
            // 이 케이스가 존재하고 Dash 후 S_HitResult가 도착했다면 서버 DashBoxHalfX가 4.0f로 되돌아간 것.
            // 스폰 배치상 이 범위에 적이 없으면 OutsidePathEnemyFound=false → skip (배치 의존, PASS 유지).
            // skip 시 로그: "[③-neg skip] no outside-path enemy in dist 2.5~4.0 — skipping negative case"
            result.OutsidePathEnemyFound = bot.HasNormalOutsideDashPath();
            if (!result.OutsidePathEnemyFound)
                Console.WriteLine("[③-neg skip] no outside-path enemy in dist 2.5~4.0 — skipping negative case");
            if (result.OutsidePathEnemyFound)
            {
                bool anyOutsideHit = bot.HasDashHitOutsidePath(result.PositionBeforeDash, HitEffectDash);
                result.OutsidePathEnemyNotHit = !anyOutsideHit;
                if (anyOutsideHit)
                    return Fail(result, "Outside-path enemy (dist 2.5~4.0) received S_HitResult(Dash) — DashBoxHalfX regression (was 4.0f)");
            }

            // ④ 쿨다운 중 재시전 무반응 검증
            // 쿨다운=20틱이므로 즉시 재시전은 drop 대상.
            bot.ClearSkillCasts();
            bot.SendSkillUse(DashSkillId);
            await Task.Delay(Constants.TickIntervalMs * CooldownCheckAfterTicks, ct);
            S_SkillCast? recast = bot.GetCachedSkillCast(bot.LocalEntityId, DashSkillId);
            result.CooldownRejectedRecast = recast == null;

            if (!result.CooldownRejectedRecast)
                return Fail(result, "Cooldown recast was NOT silently dropped — server accepted double cast");
        }
        finally
        {
            bot.Disconnect();
        }

        // ⑤ 클래스 게이트 검증: Mage 봇이 Dash 시도 → S_SkillCast 미수신
        DashProbe mageBot = new(CharacterClass.Mage);
        try
        {
            mageBot.Connect(host, port);

            if (!mageBot.WaitConnected(DefaultTimeout))
                return Fail(result, "[gate] mage bot connect timeout");
            if (!mageBot.WaitHandshake(DefaultTimeout))
                return Fail(result, "[gate] mage bot handshake timeout");
            if (!mageBot.HandshakeOk)
                return Fail(result, $"[gate] mage bot handshake rejected: {mageBot.HandshakeReason}");
            if (!mageBot.WaitEnterMap(DefaultTimeout))
                return Fail(result, "[gate] mage bot S_EnterMap timeout");

            // HuntingGround 이동
            await mageBot.MoveToPortal(TownPortalX, ct);
            mageBot.SendEnterPortal(TownPortalId);
            if (!await mageBot.WaitMapTransition(DefaultTimeout, ct))
                return Fail(result, "[gate] mage bot Town→HuntingGround timeout");

            await Task.Delay(Constants.TickIntervalMs * 2, ct);
            await mageBot.WaitForFirstSnapshot(DefaultTimeout, ct);

            // Mage가 Dash(Knight 전용) 시도
            mageBot.SendSkillUse(DashSkillId);
            await Task.Delay(Constants.TickIntervalMs * CooldownCheckAfterTicks, ct);

            S_SkillCast? gateCast = mageBot.GetCachedSkillCast(mageBot.LocalEntityId, DashSkillId);
            result.MageClassGateBlocked = gateCast == null;

            if (!result.MageClassGateBlocked)
                return Fail(result, "Class gate FAILED: Mage received S_SkillCast(Dash) — server did not reject cross-class skill");
        }
        finally
        {
            mageBot.Disconnect();
        }

        result.Success = true;
        return result;
    }

    static Result Fail(Result r, string reason)
    {
        r.Success = false;
        r.Reason = reason;
        return r;
    }

    sealed class DashProbe
    {
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);
        readonly ManualResetEventSlim _mapTransition = new(false);

        readonly List<S_EntitySpawn> _spawns = new();
        readonly List<S_SkillCast> _skillCasts = new();
        readonly List<S_HitResult> _hitResults = new();
        readonly Dictionary<int, float> _entityCurrentX = new();

        volatile int _lastReceivedServerTick;
        volatile int _snapshotGeneration;
        volatile float _currentX;
        // 마지막 비-0 이동 방향. +1=오른쪽, -1=왼쪽. 초기 +1(오른쪽).
        volatile int _lastFacingDir = 1;

        BotSession? _session;
        uint _moveTick;
        readonly CharacterClass _class;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;
        public float SpawnX { get; private set; }
        public float CurrentX => _currentX;
        public int FacingDir => _lastFacingDir;

        public DashProbe(CharacterClass cls) { _class = cls; }

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
                        C_Handshake h = new() { clientVersion = ProtocolVersion.Current };
                        s.Send(h.Write());
                    };
                    s.OnDisconnectedCallback = _ => { };
                    s.OnPacketCallback = HandlePacket;
                    _session = s;
                    return s;
                });
        }

        public bool WaitConnected(TimeSpan t) => _connected.Wait(t);
        public bool WaitHandshake(TimeSpan t) => _handshake.Wait(t);
        public bool WaitEnterMap(TimeSpan t)  => _enterMap.Wait(t);

        public async Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransition.IsSet, timeout, ct);

        public async Task<bool> WaitForFirstSnapshot(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastReceivedServerTick > 0, timeout, ct);

        public async Task<bool> WaitForNextSnapshot(TimeSpan timeout, CancellationToken ct)
        {
            int genBefore = _snapshotGeneration;
            return await WaitUntil(() => _snapshotGeneration > genBefore, timeout, ct);
        }

        public async Task<bool> WaitForSpawns(int minCount, byte kind, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(
                () => { lock (_gate) return _spawns.Count(s => s.entityKind == kind && s.currentHp > 0) >= minCount; },
                timeout, ct);

        // Dash 타격 가능한 Normal 적 X를 반환.
        // 봇 SpawnX 오른쪽 Normal 우선 선택 — 봇이 왼쪽에서 오른쪽(FacingDir=+1)으로 Dash해 타격.
        // 오른쪽 Normal 없으면 왼쪽 Normal 폴백.
        // _entityCurrentX 우선(AI 이동 반영), 없으면 S_EntitySpawn.x 폴백.
        public float GetFirstNormalCurrentX()
        {
            lock (_gate)
            {
                float botSpawnX = SpawnX;
                IEnumerable<S_EntitySpawn> alive = _spawns.Where(s => s.entityKind == 0 && s.currentHp > 0);

                // 오른쪽 Normal 우선
                S_EntitySpawn? sp = alive
                    .Select(s => (spawn: s, x: _entityCurrentX.TryGetValue(s.entityId, out float ex) ? ex : s.x))
                    .Where(t => t.x > botSpawnX)
                    .OrderBy(t => t.x)
                    .Select(t => t.spawn)
                    .FirstOrDefault()
                    ?? alive.FirstOrDefault();

                if (sp == null) return SpawnX;
                return _entityCurrentX.TryGetValue(sp.entityId, out float cur) ? cur : sp.x;
            }
        }

        // 서버 Dash 박스 판정 미러링: boxOrigin = _currentX + DashBoxHalfX * FacingDir, 범위 ± DashBoxHalfX.
        // 서버 02_Server/GameServer/Maps/Systems/SkillSystem.cs ProcessDash 로직과 1:1 대응.
        // DashBoxHalfX 변경 시 여기도 수동 동기화 필수 (봇은 서버 어셈블리 미참조).
        // 적 최신 위치는 _entityCurrentX로 추적 (S_EntitySpawn 초기값 대신 S_EntityState 갱신).
        public bool HasNormalInDashPath()
        {
            const float DashBoxHalfX = 2.5f; // 서버 CombatConstants.DashBoxHalfX와 수동 동기화
            float boxOriginX = _currentX + DashBoxHalfX * _lastFacingDir;
            lock (_gate)
                return _spawns.Any(s =>
                    s.entityKind == 0 &&
                    s.currentHp > 0 &&
                    (_entityCurrentX.TryGetValue(s.entityId, out float ex) ? ex : s.x) is float enemyX &&
                    Math.Abs(enemyX - boxOriginX) <= DashBoxHalfX);
        }

        // 서버 Dash 박스 바로 밖(boxOriginX 기준 dist 2.5~4.0)에 살아있는 Normal 적이 있으면 true.
        // 부정 케이스 검증용 — Dash가 이 범위의 적을 타격하면 DashBoxHalfX가 다시 4.0으로 퇴행한 것.
        // 서버 CombatConstants.DashBoxHalfX / boxOriginX 계산 변경 시 여기도 검토.
        public bool HasNormalOutsideDashPath()
        {
            const float DashBoxHalfX = 2.5f;
            const float OuterBound   = 4.0f;
            float boxOriginX = _currentX + DashBoxHalfX * _lastFacingDir;
            lock (_gate)
                return _spawns.Any(s =>
                    s.entityKind == 0 &&
                    s.currentHp > 0 &&
                    (_entityCurrentX.TryGetValue(s.entityId, out float ex) ? ex : s.x) is float enemyX &&
                    Math.Abs(enemyX - boxOriginX) > DashBoxHalfX &&
                    Math.Abs(enemyX - boxOriginX) <= OuterBound);
        }

        // S_HitResult 버퍼에서 hitEffect == dashHitEffect 이고 타격 대상이 박스 밖(dist 2.5~4.0)이면 true.
        // casterX는 Dash 시전 직전 봇 위치 (PositionBeforeDash) — boxOriginX 계산 기준점.
        // FacingDir은 시전 시점 _lastFacingDir 사용 (호출 시점과 동일 — Dash 직후 호출).
        public bool HasDashHitOutsidePath(float casterX, byte dashHitEffect)
        {
            const float DashBoxHalfX = 2.5f;
            const float OuterBound   = 4.0f;
            float boxOriginX = casterX + DashBoxHalfX * _lastFacingDir;
            lock (_gate)
            {
                foreach (S_HitResult hr in _hitResults)
                {
                    if (hr.hitEffect != dashHitEffect) continue;
                    if (!_entityCurrentX.TryGetValue(hr.targetEntityId, out float hx)) continue;
                    float dist = Math.Abs(hx - boxOriginX);
                    if (dist > DashBoxHalfX && dist <= OuterBound) return true;
                }
                return false;
            }
        }

        public async Task<S_SkillCast?> WaitForSkillCast(int casterEntityId, byte skillId, TimeSpan timeout, CancellationToken ct)
        {
            bool ok = await WaitUntil(
                () => { lock (_gate) return _skillCasts.Any(c => c.casterEntityId == casterEntityId && c.skillId == skillId); },
                timeout, ct);
            if (!ok) return null;
            lock (_gate) return _skillCasts.First(c => c.casterEntityId == casterEntityId && c.skillId == skillId);
        }

        public S_SkillCast? GetCachedSkillCast(int casterEntityId, byte skillId)
        {
            lock (_gate)
                return _skillCasts.FirstOrDefault(c => c.casterEntityId == casterEntityId && c.skillId == skillId);
        }

        public void ClearSkillCasts()
        {
            lock (_gate) _skillCasts.Clear();
        }

        public S_HitResult? GetHitResultByEffect(byte hitEffect)
        {
            lock (_gate)
                return _hitResults.FirstOrDefault(h => h.hitEffect == hitEffect);
        }

        public async Task MoveToPortal(float portalX, CancellationToken ct)
        {
            float delta = portalX - SpawnX;
            sbyte dir = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            float speed = PlayerStats.ForClass(_class).MoveSpeed;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (speed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 200);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
        }

        public async Task MoveNearTarget(float targetX, float approachDist, CancellationToken ct)
        {
            float dest = targetX > _currentX ? targetX - approachDist : targetX + approachDist;
            float delta = dest - _currentX;
            if (Math.Abs(delta) < 0.1f) return;
            sbyte dir = delta >= 0f ? (sbyte)1 : (sbyte)-1;
            float speed = PlayerStats.ForClass(_class).MoveSpeed;
            int ticks = (int)Math.Ceiling(Math.Abs(delta) / (speed * Constants.TickDuration));
            ticks = Math.Clamp(ticks, 0, 200);
            for (int i = 0; i < ticks; i++)
            {
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(150, ct);
        }

        // EnsurePositionedAt: closed-loop 위치 보정.
        //
        // open-loop tick 계산(MoveNearTarget)은 Task.Delay 타이밍 오차 + 서버 물리 추가 틱으로
        // 목적지를 ±2~3 unit 오버슛할 수 있음. 이 메서드는 실시간 _currentX 기반 루프.
        //
        // 종료 조건: |_currentX - destX| ≤ Tolerance(0.3f).
        // 안전 상한: MaxIterations(60) 초과 시 루프 탈출 (hang 방지).
        public async Task EnsurePositionedAt(float destX, CancellationToken ct)
        {
            const float Tolerance = 0.3f;
            const int MaxIterations = 60;

            int iter = 0;
            while (iter < MaxIterations)
            {
                float err = destX - _currentX;
                if (Math.Abs(err) <= Tolerance)
                {
                    SendMove(0);
                    await Task.Delay(150, ct);
                    break;
                }

                sbyte dir = err >= 0f ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
                iter++;
            }
        }

        // TrackAndApproachEnemy: 적의 현재 위치를 실시간으로 추적해 왼쪽 1 unit에 위치.
        // EnsurePositionedAt과 달리 destX를 매 iteration 갱신해 이동하는 적을 추적.
        // 종료 조건: |_currentX - (enemyCurrentX - 1.0)| ≤ Tolerance 또는 최대 100 iteration.
        public async Task TrackAndApproachEnemy(CancellationToken ct)
        {
            const float ApproachOffset = 1.0f; // 적 왼쪽 1 unit
            const float Tolerance = 0.5f;
            const int MaxIterations = 100; // 5초 상한

            int iter = 0;
            while (iter < MaxIterations)
            {
                float enemyX = GetFirstNormalCurrentX();
                float destX = enemyX - ApproachOffset;
                float err = destX - _currentX;

                if (Math.Abs(err) <= Tolerance)
                {
                    SendMove(0);
                    await Task.Delay(150, ct);
                    break;
                }

                sbyte dir = err >= 0f ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
                iter++;
            }
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal p = new() { portalId = portalId };
            _session?.Send(p.Write());
        }

        public void SendSkillUse(byte skillId)
        {
            C_SkillUse p = new()
            {
                skillId            = skillId,
                attackerClientTick = _lastReceivedServerTick,
            };
            _session?.Send(p.Write());
        }

        public void Disconnect() => _session?.Disconnect();

        void SendMove(sbyte inputX)
        {
            if (inputX != 0) _lastFacingDir = inputX;
            _moveTick++;
            C_MoveIntent m = new()
            {
                input      = InputBits.Encode(inputX, jumpPressed: false),
                clientTick = _moveTick,
            };
            _session?.Send(m.Write());
        }

        void HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 4) return;
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2));
            switch ((PacketID)id)
            {
                case PacketID.S_HandshakeResult:
                    S_HandshakeResult hr = new(); hr.Read(buffer);
                    HandshakeOk = hr.ok;
                    HandshakeReason = hr.reason;
                    if (hr.ok)
                    {
                        C_CharacterSelect cs = new() { characterClass = (byte)_class };
                        _session?.Send(cs.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap em = new(); em.Read(buffer);
                    LocalEntityId = em.entityId;
                    SpawnX = em.spawnX;
                    _currentX = em.spawnX;
                    _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition mt = new(); mt.Read(buffer);
                    SpawnX = mt.spawnX;
                    _currentX = mt.spawnX;
                    _mapTransition.Set();
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot sn = new(); sn.Read(buffer);
                    _lastReceivedServerTick = sn.serverTick;
                    if (sn.entityId == LocalEntityId)
                    {
                        SpawnX = sn.x;
                        _currentX = sn.x;
                        Interlocked.Increment(ref _snapshotGeneration);
                    }
                    break;

                case PacketID.S_EntitySpawn:
                    S_EntitySpawn sp = new(); sp.Read(buffer);
                    lock (_gate)
                    {
                        _spawns.Add(sp);
                        _entityCurrentX[sp.entityId] = sp.x;
                    }
                    break;

                case PacketID.S_EntityState:
                    S_EntityState es = new(); es.Read(buffer);
                    lock (_gate) _entityCurrentX[es.entityId] = es.x;
                    break;

                case PacketID.S_SkillCast:
                    S_SkillCast sc = new(); sc.Read(buffer);
                    lock (_gate) _skillCasts.Add(sc);
                    break;

                case PacketID.S_HitResult:
                    S_HitResult hit = new(); hit.Read(buffer);
                    lock (_gate)
                    {
                        _hitResults.Add(hit);
                        // spawn HP 갱신 — 경로 적 생존 여부 추적
                        S_EntitySpawn? existing = _spawns.FirstOrDefault(s => s.entityId == hit.targetEntityId);
                        if (existing != null) existing.currentHp = hit.currentHp;
                    }
                    break;
            }
        }

        static async Task<bool> WaitUntil(Func<bool> pred, TimeSpan timeout, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (pred()) return true;
                await Task.Delay(25, ct);
            }
            return pred();
        }
    }
}
