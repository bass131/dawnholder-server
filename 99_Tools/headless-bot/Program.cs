using System.Net;
using Dawnholder.Client.Net;
using Dawnholder.Tools.HeadlessBot;
using Dawnholder.Tools.HeadlessBot.Scenarios;

// 봇 콘솔 entry. 인자 파싱 → 시나리오 분기 → 결과 출력 + exit code.
//
// 사용 예:
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario M2BasicMovement
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario MultiRosterSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario EmergencyCombatSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario BossStageClearSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario HpSyncSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario RemoteAttackSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario WhiffSwingSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario RangedHitSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario FreezeSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario ThunderboltAoeSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario RangedWhiffSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario DashSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario TeleportSmoke
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario BossGate
//   HeadlessBot --scenario smoke   (단순 connect 검증)

string host = "127.0.0.1";
int port = 7777;
string scenarioName = "smoke";

for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--host": host = args[i + 1]; break;
        case "--port": port = int.Parse(args[i + 1]); break;
        case "--scenario": scenarioName = args[i + 1]; break;
    }
}

Console.WriteLine($"=== HeadlessBot ===");
Console.WriteLine($"target: {host}:{port}  scenario: {scenarioName}");

if (string.Equals(scenarioName, "MultiRosterSmoke", StringComparison.OrdinalIgnoreCase))
{
    MultiRosterSmoke.Result r = await MultiRosterSmoke.Run(host, port);
    Console.WriteLine($"[Bot] MultiRosterSmoke: success={r.Success} " +
                      $"entities=({r.FirstEntityId}, {r.SecondEntityId}, reconnect:{r.ReconnectEntityId})");
    Console.WriteLine($"      firstJoins={r.FirstJoinCount} secondJoins={r.SecondJoinCount} " +
                      $"secondLeaves={r.SecondLeaveCount} reconnectRoster={r.ReconnectRosterCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "EmergencyCombatSmoke", StringComparison.OrdinalIgnoreCase))
{
    EmergencyCombatSmoke.Result r = await EmergencyCombatSmoke.Run(host, port);
    Console.WriteLine($"[Bot] EmergencyCombatSmoke: success={r.Success} " +
                      $"entity={r.LocalEntityId} target={r.TargetEntityId} " +
                      $"hits={r.HitCount} death={r.SawDeath}");
    Console.WriteLine($"      hp: {r.InitialHp} -> {r.FinalHp} " +
                      $"moveIntents={r.MoveIntentsSent} " +
                      $"rateLimitDropped={r.RateLimitDropped} optionB={r.UsedOptionBDeathEquivalent}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "BossStageClearSmoke", StringComparison.OrdinalIgnoreCase))
{
    BossStageClearSmoke.Result r = await BossStageClearSmoke.Run(host, port);
    Console.WriteLine($"[Bot] BossStageClearSmoke: success={r.Success} " +
                      $"entity={r.LocalEntityId} boss={r.BossEntityId} " +
                      $"hits={r.HitCount} stageClear={r.SawStageClear}");
    Console.WriteLine($"      boss hp: {r.InitialBossHp} -> {r.FinalBossHp} " +
                      $"moveIntents={r.MoveIntentsSent} death={r.SawBossDeath} " +
                      $"stageClearCount={r.StageClearCount} duplicateSuppressed={r.DuplicateSuppressed}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "BossFightSmoke", StringComparison.OrdinalIgnoreCase))
{
    BossFightSmoke.Result r = await BossFightSmoke.Run(host, port);
    Console.WriteLine($"[Bot] BossFightSmoke: success={r.Success} " +
                      $"entity={r.LocalEntityId} boss={r.BossEntityId} " +
                      $"hits={r.HitCount} stageClear={r.SawStageClear}");
    Console.WriteLine($"      boss hp: {r.InitialBossHp} -> {r.FinalBossHp} " +
                      $"enemyAttacks={r.EnemyAttackCount} lastDamage={r.LastEnemyAttackDamage} " +
                      $"respawn={r.SawRespawn} respawnCount={r.RespawnCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "HpSyncSmoke", StringComparison.OrdinalIgnoreCase))
{
    HpSyncSmoke.Result r = await HpSyncSmoke.Run(host, port);
    Console.WriteLine($"[Bot] HpSyncSmoke: success={r.Success} " +
                      $"entity={r.LocalEntityId} boss={r.BossEntityId} maxHp={r.MaxHp}");
    Console.WriteLine($"      initialFull={r.SawInitialFull} damage={r.SawDamage} " +
                      $"zero={r.SawZero} reviveFull={r.SawReviveFull} events={r.HpEventCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "RemoteAttackSmoke", StringComparison.OrdinalIgnoreCase))
{
    RemoteAttackSmoke.Result r = await RemoteAttackSmoke.Run(host, port);
    Console.WriteLine($"[Bot] RemoteAttackSmoke: success={r.Success} " +
                      $"botA={r.BotAEntityId} botB={r.BotBEntityId}");
    Console.WriteLine($"      bReceived={r.BReceivedCount} aReceived={r.AReceivedCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "WhiffSwingSmoke", StringComparison.OrdinalIgnoreCase))
{
    WhiffSwingSmoke.Result r = await WhiffSwingSmoke.Run(host, port);
    Console.WriteLine($"[Bot] WhiffSwingSmoke: success={r.Success} " +
                      $"entity={r.LocalEntityId} attacksSent={r.AttacksSent} hitResults={r.HitResultCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "RangedHitSmoke", StringComparison.OrdinalIgnoreCase))
{
    RangedHitSmoke.Result r = await RangedHitSmoke.Run(host, port);
    Console.WriteLine($"[Bot] RangedHitSmoke: success={r.Success} " +
                      $"entity={r.LocalEntityId} target={r.TargetEntityId} initialHp={r.InitialHp}");
    Console.WriteLine($"      projectileLaunch={r.SawProjectileLaunch} travelTicks={r.ProjectileTravelTicks} " +
                      $"hitResult={r.SawHitResult} hitEffect={r.HitEffect} hpAfter={r.HpAfterHit}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "FreezeSmoke", StringComparison.OrdinalIgnoreCase))
{
    FreezeSmoke.Result r = await FreezeSmoke.Run(host, port);
    Console.WriteLine($"[Bot] FreezeSmoke: success={r.Success} entity={r.LocalEntityId}");
    Console.WriteLine($"      normal={r.NormalEntityId} movedDuringObserve={r.NormalMovedDuringObserveWindow}");
    Console.WriteLine($"      boss={r.BossEntityId} bossMovedAfterShot={r.BossMovedAfterShot} " +
                      $"bossSkippedLowHp={r.BossSkippedLowHp}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "ThunderboltAoeSmoke", StringComparison.OrdinalIgnoreCase))
{
    ThunderboltAoeSmoke.Result r = await ThunderboltAoeSmoke.Run(host, port);
    Console.WriteLine($"[Bot] ThunderboltAoeSmoke: success={r.Success} entity={r.LocalEntityId}");
    Console.WriteLine($"      skillCast={r.SawSkillCast} normalTargets={r.NormalTargetCount} " +
                      $"normalHits={r.NormalHitCount} allHpDecreased={r.AllNormalHpDecreased}");
    Console.WriteLine($"      bossAoeAttempted={r.BossAoeAttempted} bossHit={r.BossReceivedHitResult} " +
                      $"bossMoved={r.BossMovedAfterAoe} bossSkippedLowHp={r.BossSkippedLowHp}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "RangedWhiffSmoke", StringComparison.OrdinalIgnoreCase))
{
    RangedWhiffSmoke.Result r = await RangedWhiffSmoke.Run(host, port);
    Console.WriteLine($"[Bot] RangedWhiffSmoke: success={r.Success} entity={r.LocalEntityId} " +
                      $"attacksSent={r.AttacksSent}");
    Console.WriteLine($"      playerAttacks={r.PlayerAttackCount} projectiles={r.ProjectileLaunchCount} " +
                      $"hitResults={r.HitResultCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "DashSmoke", StringComparison.OrdinalIgnoreCase))
{
    DashSmokeScenario.Result r = await DashSmokeScenario.Run(host, port);
    Console.WriteLine($"[Bot] DashSmoke: success={r.Success} entity={r.LocalEntityId}");
    Console.WriteLine($"      skillCast={r.SawSkillCast} skillId={r.SkillCastSkillId}");
    Console.WriteLine($"      position: before={r.PositionBeforeDash:F2} after={r.PositionAfterDash:F2} " +
                      $"advanced={r.PositionAdvanced}");
    Console.WriteLine($"      pathEnemy={r.PathEnemyFound} hitEffect3={r.SawHitResultDash}");
    Console.WriteLine($"      cooldownRejected={r.CooldownRejectedRecast} mageGateBlocked={r.MageClassGateBlocked}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "TeleportSmoke", StringComparison.OrdinalIgnoreCase))
{
    TeleportSmokeScenario.Result r = await TeleportSmokeScenario.Run(host, port);
    Console.WriteLine($"[Bot] TeleportSmoke: success={r.Success} entity={r.LocalEntityId}");
    Console.WriteLine($"      skillCast={r.SawSkillCast}");
    Console.WriteLine($"      position: before={r.PositionBeforeTeleport:F2} " +
                      $"expected≈{r.ExpectedPositionAfterTeleport:F2} actual={r.PositionAfterTeleport:F2} " +
                      $"matches={r.PositionMatchesExpected}");
    Console.WriteLine($"      hitResults={r.HitResultCount}(expect 0) " +
                      $"cooldownRejected={r.CooldownRejectedRecast}");
    Console.WriteLine($"      boundsClamp={r.BoundsClampVerified} boundsTestX={r.BoundsTestPositionAfterTeleport:F2} " +
                      $"knightGateBlocked={r.KnightClassGateBlocked}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "EnemyAiSmoke", StringComparison.OrdinalIgnoreCase))
{
    EnemyAiSmoke.Result r = await EnemyAiSmoke.Run(host, port);
    Console.WriteLine($"[Bot] EnemyAiSmoke: success={r.Success} entity={r.LocalEntityId}");
    Console.WriteLine($"      [A] golem={r.GolemEntityId} x={r.GolemInitialX:F2} " +
                      $"patrol={r.SawGolemPatrol} chaseAfterApproach={r.SawGolemChaseAfterApproach}");
    Console.WriteLine($"      [B] slime={r.SlimeEntityId} x={r.SlimeInitialX:F2} " +
                      $"patrolBeforeHit={r.SawSlimePatrolBeforeHit} " +
                      $"stayedPatrolAfterApproach={r.SlimeStayedPatrolAfterApproach} " +
                      $"chaseAfterHit={r.SawSlimeChaseAfterHit}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "BossGate", StringComparison.OrdinalIgnoreCase))
{
    // standalone 봇 런: seedBossGate=null → 거부 경로(S_PortalLocked) 확인만.
    // 통과 경로(killCount=40 시드 후 재시도)는 xUnit 통합 테스트 전용.
    BossGateSmoke.Result r = await BossGateSmoke.Run(host, port, seedBossGate: null);
    Console.WriteLine($"[Bot] BossGate: success={r.Success} entity={r.LocalEntityId}");
    Console.WriteLine($"      sawPortalLocked={r.SawPortalLocked} " +
                      $"requiredCount={r.RequiredCount} currentCount={r.CurrentCount}");
    Console.WriteLine($"      enteredBossRoom={r.EnteredBossRoom} (standalone=false — xUnit 전용)");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (string.Equals(scenarioName, "MapTransition", StringComparison.OrdinalIgnoreCase))
{
    MapTransitionScenario.Result r = await MapTransitionScenario.Run(host, port);
    Console.WriteLine($"[Bot] MapTransition: success={r.Success} entity={r.EntityId}");
    Console.WriteLine($"      HG={r.EnteredHuntingGround}(spawnX={r.SpawnXOnHG:F2}) " +
                      $"Boss={r.EnteredBossRoom}(spawnX={r.SpawnXOnBossRoom:F2}) " +
                      $"Ending={r.EnteredEnding}(spawnX={r.SpawnXOnEnding:F2}) " +
                      $"Town={r.ReturnedToTown}(spawnX={r.SpawnXOnTown:F2})");
    Console.WriteLine($"      entityIdPreserved={r.EntityIdPreservedAcrossAllMaps} " +
                      $"spawnCoordinatesCorrect={r.SpawnCoordinatesCorrect}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (scenarioName == "M2BasicMovement")
{
    M2BasicMovement.Result r = await M2BasicMovement.Run(host, port);
    Console.WriteLine($"[Bot] M2BasicMovement: success={r.Success} " +
                      $"intents={r.IntentsSent} snapshots={r.SnapshotsReceived}");
    Console.WriteLine($"      bot=({r.BotSimFinal.X:F2},{r.BotSimFinal.Y:F2}) " +
                      $"server=({r.ServerFinal.X:F2},{r.ServerFinal.Y:F2}) " +
                      $"desync=(dx={r.FinalDesyncX:F2}, dy={r.FinalDesyncY:F2})");
    float headroomX = r.BoundX / Math.Max(r.MaxObservedDeltaX, 0.0001f);
    float headroomY = r.BoundY / Math.Max(r.MaxObservedDeltaY, 0.0001f);
    Console.WriteLine($"      maxObsDelta=(x={r.MaxObservedDeltaX:F3}, y={r.MaxObservedDeltaY:F3}) " +
                      $"bound=(x={r.BoundX:F3}, y={r.BoundY:F3}) " +
                      $"headroom=(x={headroomX:F1}x, y={headroomY:F1}x)");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

BotSession? session = null;
ManualResetEventSlim connectedEvent = new ManualResetEventSlim(false);
ManualResetEventSlim disconnectedEvent = new ManualResetEventSlim(false);

Connector connector = new Connector();
connector.Connect(
    new IPEndPoint(IPAddress.Parse(host), port),
    sessionFactory: () =>
    {
        session = new BotSession
        {
            OnConnectedCallback = ep =>
            {
                Console.WriteLine($"[Bot] Connected to {ep}");
                connectedEvent.Set();
            },
            OnDisconnectedCallback = ep =>
            {
                Console.WriteLine($"[Bot] Disconnected from {ep}");
                disconnectedEvent.Set();
            },
            OnPacketCallback = buffer =>
            {
                Console.WriteLine($"[Bot] recv packet: {buffer.Count} bytes");
            }
        };
        return session;
    });

if (!connectedEvent.Wait(TimeSpan.FromSeconds(5)))
{
    Console.WriteLine("[Bot] FAIL: connect timeout (5s)");
    return 1;
}

// 2초간 서버 패킷 받아보고 우아하게 종료.
await Task.Delay(TimeSpan.FromSeconds(2));

session?.Disconnect();
disconnectedEvent.Wait(TimeSpan.FromSeconds(2));

Console.WriteLine("[Bot] Done.");
return 0;
