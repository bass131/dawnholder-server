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
    Console.WriteLine($"      normal={r.NormalEntityId} froze={r.NormalFrozeAfterShot} resumed={r.NormalResumedAfterFreeze}");
    Console.WriteLine($"      boss={r.BossEntityId} bossMovedDuringExpectedFreeze={r.BossMovedDuringExpectedFreeze} " +
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

if (scenarioName == "M2BasicMovement")
{
    M2BasicMovement.Result r = await M2BasicMovement.Run(host, port);
    Console.WriteLine($"[Bot] M2BasicMovement: success={r.Success} " +
                      $"intents={r.IntentsSent} snapshots={r.SnapshotsReceived}");
    Console.WriteLine($"      bot=({r.BotSimFinal.X:F2},{r.BotSimFinal.Y:F2}) " +
                      $"server=({r.ServerFinal.X:F2},{r.ServerFinal.Y:F2}) " +
                      $"desync=(dx={r.FinalDesyncX:F2}, dy={r.FinalDesyncY:F2})");
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
