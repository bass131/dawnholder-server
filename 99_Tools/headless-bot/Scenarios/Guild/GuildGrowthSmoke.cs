using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Public socket journey. Approved numbers are assertions; every reported value comes from packets.
public static class GuildGrowthSmoke
{
    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    public static async Task<Result> Run(string host, int port, CancellationToken ct = default)
    {
        using CancellationTokenSource bounded = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bounded.CancelAfter(TimeSpan.FromSeconds(110));
        ct = bounded.Token;
        GuildGrowthProbe first = new(ProbeBase.CreateAccountName());
        GuildGrowthProbe second = new(ProbeBase.CreateAccountName());
        Result result = new();
        try
        {
            first.Connect(host, port);
            second.Connect(host, port);
            await Task.WhenAll(first.WaitForEntry(ct), second.WaitForEntry(ct));
            Check(first.Account != second.Account && first.LocalEntityId != second.LocalEntityId,
                "Two distinct accounts/entities required.");
            await Task.WhenAll(first.WaitForOnline(2, ct), second.WaitForOnline(2, ct));
            S_GuildSnapshot[] joined = await Task.WhenAll(first.QueryGuild(ct), second.QueryGuild(ct));
            CheckShared(joined, 10, 1);
            Check(joined.All(s => s.memberCount == 2 && s.onlineMemberCount == 2 && s.weaponLevel == 0),
                "Fresh server must contain only the two unenhanced members.");
            Observe(result, $"joined accounts={first.Account},{second.Account} guild={joined[0].guildId} treasury={joined[0].treasury} members={joined[0].memberCount} online={joined[0].onlineMemberCount}");

            first.Invite(second.LocalEntityId);
            await second.WaitForInvite(first.LocalEntityId, ct);
            second.Accept(first.LocalEntityId);
            await Task.WhenAll(first.WaitForParty(first.LocalEntityId, second.LocalEntityId, ct),
                second.WaitForParty(first.LocalEntityId, second.LocalEntityId, ct));
            Check(first.PartyId == second.PartyId && first.PartyId > 0, "Party IDs disagree.");
            Observe(result, $"party={first.PartyId} members={first.LocalEntityId},{second.LocalEntityId}");

            await EnterBossRoom(first, second, ct);
            S_EntitySpawn baselineBoss = first.Boss;
            CheckSameBoss(first, second);
            S_InventorySnapshot[] preFight = await Task.WhenAll(first.QueryInventory(ct), second.QueryInventory(ct));
            S_HitResult beforeFirst = await first.MeasureMelee(ct);
            S_HitResult beforeSecond = await second.MeasureMelee(ct);
            await first.KillBoss(ct);
            GuildGrowthProbe.ClearObservation[] clears = await Task.WhenAll(first.ObserveClear(ct), second.ObserveClear(ct));
            CheckClear(clears[0], first, preFight[0], result);
            CheckClear(clears[1], second, preFight[1], result);
            S_GuildSnapshot[] credited = await Task.WhenAll(first.QueryGuild(ct), second.QueryGuild(ct));
            CheckShared(credited, 30, 1);
            Observe(result, $"clear treasury={joined[0].treasury}->{credited[0].treasury} revision={credited[0].guildRevision}");

            await Task.WhenAll(first.ReturnToTown(ct), second.ReturnToTown(ct));
            first.ConfirmClear();
            second.ConfirmClear();
            await Task.Delay(Constants.TickIntervalMs * 4, ct);
            S_GuildSnapshot[] confirmed = await Task.WhenAll(first.QueryGuild(ct), second.QueryGuild(ct));
            CheckShared(confirmed, 30, 1);
            Check(confirmed.All(s => s.guildRevision == credited[0].guildRevision) &&
                first.ClearCount == 1 && second.ClearCount == 1, "Repeated confirm/query changed credit or clear count.");
            Observe(result, $"Town duplicateConfirm clearCounts={first.ClearCount},{second.ClearCount} treasury={confirmed[0].treasury} revision={confirmed[0].guildRevision}");

            // Arm both connections with the same observed revision before awaiting either answer.
            uint sharedRevision = confirmed[0].guildRevision;
            first.BeginAction(GuildAction.StrongholdUpgrade, sharedRevision);
            second.BeginAction(GuildAction.StrongholdUpgrade, sharedRevision);
            GuildGrowthProbe.ActionObservation[] purchases = await Task.WhenAll(first.FinishAction(ct), second.FinishAction(ct));
            Check(purchases.Count(p => p.Result.result == (byte)GuildResult.Success) == 1 &&
                purchases.Count(p => p.Result.result == (byte)GuildResult.Stale) == 1,
                $"Same-revision purchase results={purchases[0].Result.result},{purchases[1].Result.result}; expected Success/Stale.");
            CheckShared(purchases.Select(p => p.Snapshot).ToArray(), 0, 2);
            Check(purchases.All(p => p.Snapshot.guildRevision == sharedRevision + 1), "Stronghold spent more than one revision.");
            Observe(result, $"stronghold requested={sharedRevision} results={purchases[0].Result.result},{purchases[1].Result.result} treasury={purchases[0].Snapshot.treasury} level={purchases[0].Snapshot.strongholdLevel} unlocked={purchases[0].Snapshot.blacksmithUnlocked}");

            S_InventorySnapshot beforeEnhance = await first.QueryInventory(ct);
            S_InventorySnapshot peerInventory = await second.QueryInventory(ct);
            first.BeginAction(GuildAction.WeaponEnhance, first.PersonalRevision);
            GuildGrowthProbe.ActionObservation enhanced = await first.FinishAction(ct);
            Check(enhanced.Result.result == (byte)GuildResult.Success,
                $"Weapon enhance refused: result={enhanced.Result.result} requested={enhanced.Result.requestedRevision}.");
            S_InventorySnapshot afterEnhance = first.Inventory;
            Check(afterEnhance.currency == beforeEnhance.currency - 20 &&
                GuildGrowthProbe.CountItem(afterEnhance, ItemId.Material) == GuildGrowthProbe.CountItem(beforeEnhance, ItemId.Material) - 1 &&
                afterEnhance.revision == beforeEnhance.revision + 1 && enhanced.Snapshot.weaponLevel == 1 && enhanced.Snapshot.attackBonus == 5,
                "Enhancement did not atomically consume 20 gold/one material for level1/bonus5.");
            S_GuildSnapshot peerGuild = await second.QueryGuild(ct);
            S_InventorySnapshot peerAfter = await second.QueryInventory(ct);
            Check(peerGuild.weaponLevel == 0 && peerGuild.attackBonus == 0 &&
                peerAfter.revision == peerInventory.revision && peerAfter.currency == peerInventory.currency &&
                GuildGrowthProbe.CountItem(peerAfter, ItemId.Material) == GuildGrowthProbe.CountItem(peerInventory, ItemId.Material),
                "Enhancing first account changed the other member's personal state.");
            Observe(result, $"enhance account={first.Account} gold={beforeEnhance.currency}->{afterEnhance.currency} material={GuildGrowthProbe.CountItem(beforeEnhance, ItemId.Material)}->{GuildGrowthProbe.CountItem(afterEnhance, ItemId.Material)} weapon={enhanced.Snapshot.weaponLevel} bonus={enhanced.Snapshot.attackBonus}; peer weapon={peerGuild.weaponLevel} bonus={peerGuild.attackBonus}");

            await EnterBossRoom(first, second, ct);
            CheckSameBoss(first, second);
            S_EntitySpawn nextBoss = first.Boss;
            Check(nextBoss.entityId != baselineBoss.entityId && nextBoss.entityKind == baselineBoss.entityKind &&
                nextBoss.maxHp == baselineBoss.maxHp && nextBoss.currentHp == nextBoss.maxHp,
                "Damage comparison needs a fresh boss of the same kind/maxHP.");
            // EnemyEntity.Stats is immutable and GameMap.SpawnEnemy selects EnemyCatalog.For(kind).
            // The catalog is a target identity check, not a reimplementation of the damage formula.
            int defense = EnemyStats.BossDefault().Defense;
            S_HitResult afterFirst = await first.MeasureMelee(ct);
            S_HitResult afterSecond = await second.MeasureMelee(ct);
            int firstDelta = afterFirst.damage - beforeFirst.damage;
            int secondDelta = afterSecond.damage - beforeSecond.damage;
            Observe(result, $"melee kind={nextBoss.entityKind} catalogDefense={defense} bosses={baselineBoss.entityId}->{nextBoss.entityId} firstDamage={beforeFirst.damage}->{afterFirst.damage} delta={firstDelta}; peerDamage={beforeSecond.damage}->{afterSecond.damage} delta={secondDelta}");
            Check(firstDelta == 5 && secondDelta == 0, $"Observed damage differences are {firstDelta}/{secondDelta}, expected +5/0.");

            S_GuildSnapshot communalBeforeReconnect = await second.QueryGuild(ct);
            string sameAccount = first.Account;
            first.Disconnect();
            Check(first.WaitDisconnected(ConnectTimeout), "First connection did not disconnect.");
            await second.WaitForOnline(1, ct);
            first = new GuildGrowthProbe(sameAccount);
            first.Connect(host, port);
            await first.WaitForEntry(ct);
            await Task.WhenAll(first.WaitForOnline(2, ct), second.WaitForOnline(2, ct));
            S_GuildSnapshot rejoined = await first.QueryGuild(ct);
            S_InventorySnapshot reset = await first.QueryInventory(ct);
            Check(rejoined.guildId == communalBeforeReconnect.guildId &&
                rejoined.guildRevision == communalBeforeReconnect.guildRevision && rejoined.treasury == communalBeforeReconnect.treasury &&
                rejoined.strongholdLevel == 2 && rejoined.blacksmithUnlocked == 1 && rejoined.memberCount == 2 &&
                rejoined.onlineMemberCount == 2 && rejoined.weaponLevel == 0 && rejoined.attackBonus == 0 &&
                reset.currency == 0 && GuildGrowthProbe.CountItem(reset, ItemId.Material) == 0 &&
                GuildGrowthProbe.CountItem(reset, ItemId.CoinPouch) == 0,
                "Same-account reconnect duplicated membership/seed or retained personal economy.");
            Observe(result, $"reconnect account={sameAccount} guild={rejoined.guildId} treasury={rejoined.treasury} revision={rejoined.guildRevision} members={rejoined.memberCount} online={rejoined.onlineMemberCount} weapon={rejoined.weaponLevel} gold={reset.currency}");
            first.CheckHealthy();
            second.CheckHealthy();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Reason = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            first.Disconnect();
            second.Disconnect();
        }
        return result;
    }

    static async Task EnterBossRoom(GuildGrowthProbe first, GuildGrowthProbe second, CancellationToken ct)
    {
        await Task.WhenAll(first.MoveThroughPortal(20f, 1, ct), second.MoveThroughPortal(20f, 1, ct));
#if DEBUG
        // Only the quest gate is prepared. Boss damage, death, rewards and credit remain real server behavior.
        first.PrepareQuestGate();
        await Task.Delay(Constants.TickIntervalMs * 3, ct);
#endif
        await Task.WhenAll(first.MoveThroughPortal(25f, 2, ct), second.MoveThroughPortal(25f, 2, ct));
    }

    static void CheckSameBoss(GuildGrowthProbe first, GuildGrowthProbe second)
    {
        Check(first.Boss.entityId > 0 && first.Boss.entityId == second.Boss.entityId &&
            first.Boss.entityKind == (byte)EnemyKind.Boss && second.Boss.entityKind == (byte)EnemyKind.Boss,
            "Both party members must observe the same boss room copy.");
    }

    static void CheckShared(S_GuildSnapshot[] snapshots, int treasury, byte level)
    {
        Check(snapshots.Select(s => s.guildId).Distinct().Count() == 1 &&
            snapshots.Select(s => s.guildRevision).Distinct().Count() == 1 &&
            snapshots.All(s => s.treasury == treasury && s.strongholdLevel == level &&
                s.blacksmithUnlocked == (level == 2 ? 1 : 0)),
            $"Shared state mismatch: {string.Join("; ", snapshots.Select(s => $"guild={s.guildId}/rev={s.guildRevision}/gold={s.treasury}/level={s.strongholdLevel}"))}.");
    }

    static void CheckClear(GuildGrowthProbe.ClearObservation clear, GuildGrowthProbe probe,
        S_InventorySnapshot preFight, Result result)
    {
        (int Id, int Count)[] rewards =
        {
            (clear.Result.reward0ItemId, clear.Result.reward0Count),
            (clear.Result.reward1ItemId, clear.Result.reward1Count),
            (clear.Result.reward2ItemId, clear.Result.reward2Count),
        };
        Check(clear.Result.result == (byte)InventoryResult.Success && clear.Result.currency == 50 &&
            rewards.Where(r => r != (0, 0)).Order().SequenceEqual(new[] { (1, 1), (2, 1) }) &&
            clear.After.currency - clear.Before.currency == 50 && clear.After.revision == clear.Before.revision + 1 &&
            GuildGrowthProbe.CountItem(clear.After, ItemId.Material) - GuildGrowthProbe.CountItem(clear.Before, ItemId.Material) == 1 &&
            GuildGrowthProbe.CountItem(clear.After, ItemId.CoinPouch) - GuildGrowthProbe.CountItem(clear.Before, ItemId.CoinPouch) == 1 &&
            probe.ClearCount == 1,
            $"{probe.Account}: invalid clear reward result={clear.Result.result} gold={clear.Result.currency}.");
        Observe(result, $"clear account={probe.Account} order=39/36/44 result={clear.Result.result} rewardGold={clear.Result.currency} gold={clear.Before.currency}->{clear.After.currency} material={GuildGrowthProbe.CountItem(clear.Before, ItemId.Material)}->{GuildGrowthProbe.CountItem(clear.After, ItemId.Material)} pouch={GuildGrowthProbe.CountItem(clear.Before, ItemId.CoinPouch)}->{GuildGrowthProbe.CountItem(clear.After, ItemId.CoinPouch)} ordinaryKillGold={clear.Before.currency - preFight.currency} personalRev={clear.Before.revision}->{clear.After.revision} creditSeq={clear.Guild.snapshotSequence}");
    }

    static void Check(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }

    static void Observe(Result result, string observation)
    {
        result.Observations.Add(observation);
        Console.WriteLine($"[Guild observation] {observation}");
    }

    public sealed class Result
    {
        public bool Success;
        public string Reason = "";
        public List<string> Observations { get; } = new();
    }
}
