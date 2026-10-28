using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Two parties of two bots each clear their own boss room copy, then return to Town with the clear confirm.
// Requirement: goal 2026-10-10-dungeon-clear-rewards C8 (complete condition 4). Every bot must receive exactly one
// S_DungeonClearResult: Success with the shared reward table, followed by a snapshot one revision after the bot's own
// pre-fight query. None may arrive from the other party's copy. The boss door uses the DEBUG cheat like
// BossStageClearSmoke, so a Release server fails at the boss room gate.
public static class DungeonClearSmoke
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan CooldownWait = TimeSpan.FromMilliseconds(550);
    static readonly TimeSpan QuietWindow = TimeSpan.FromSeconds(1);

    // Map IDs and portal positions mirror the server's MapId and PortalTable, as in InstanceMapIsolationSmoke.
    const int TownMapId = 0;
    const int HuntingGroundMapId = 1;
    const int BossRoomMapId = 2;
    const float TownPortalX = 25f;
    const float HuntingGroundPortalX = 56f;
    const int ForwardPortalId = 1;

    // Attack loop limits from BossStageClearSmoke: reapproach after knockback, retry a missed hit.
    const float ReapproachThreshold = 1.0f;
    const int MaxReapproachTicks = 80;
    const int MaxKillAttempts = 30;

    public static async Task<Result> Run(string host, int port, CancellationToken ct = default)
    {
        ClearProbe[] probes = { new(), new(), new(), new() };
        Result result = new();
        try
        {
            foreach (ClearProbe probe in probes) probe.Connect(host, port);
            foreach (ClearProbe probe in probes)
            {
                if (!probe.WaitConnected(Timeout) || !probe.WaitHandshake(Timeout) ||
                    !probe.HandshakeOk || !probe.WaitEnterMap(Timeout))
                {
                    return Fail(result, "Four bots must complete handshake and enter Town.");
                }
            }
            int[] ids = probes.Select(probe => probe.LocalEntityId).ToArray();
            if (ids.Any(id => id <= 0) || ids.Distinct().Count() != 4)
                return Fail(result, "Entry packets must assign four distinct positive entity IDs.");

            for (int i = 0; i < probes.Length; i += 2)
            {
                probes[i].Invite(ids[i + 1]);
                if (!await probes[i + 1].WaitForInvite(ids[i], ct))
                    return Fail(result, $"Pair {i / 2}: invitation missing.");
                probes[i + 1].Accept(ids[i]);
                if (!await probes[i].WaitForParty(ids[i], ids[i + 1], ct) ||
                    !await probes[i + 1].WaitForParty(ids[i], ids[i + 1], ct))
                {
                    return Fail(result, $"Pair {i / 2}: party membership missing.");
                }
            }
            if (probes[0].PartyId == probes[2].PartyId)
                return Fail(result, "The two pairs must have different party IDs.");
            result.PartiesFormed = 2;

            bool[] hunting = await Task.WhenAll(probes.Select(probe => probe.MoveThroughPortal(TownPortalX, ForwardPortalId, HuntingGroundMapId, ct)));
            if (hunting.Any(ok => !ok)) return Fail(result, "A bot did not reach HuntingGround.");

            // The cheat sets the sender's party progress to the gate threshold, which unlocks both members.
#if DEBUG
            probes[0].SendCheatCompleteQuest();
            probes[2].SendCheatCompleteQuest();
            await Task.Delay(Constants.TickIntervalMs * 3, ct);
#endif

            bool[] bossRooms = await Task.WhenAll(probes.Select(probe => probe.MoveThroughPortal(HuntingGroundPortalX, ForwardPortalId, BossRoomMapId, ct)));
            if (bossRooms.Any(ok => !ok)) return Fail(result, "A bot did not reach its boss room copy (DEBUG server cheat opens the door).");
            int[] bossIds = probes.Select(probe => probe.BossId).ToArray();
            if (bossIds.Any(id => id <= 0) || bossIds[0] != bossIds[1] || bossIds[2] != bossIds[3] || bossIds[0] == bossIds[2])
                return Fail(result, "Each pair must see one boss of its own copy, different from the other pair's boss.");

            uint[] revisions = new uint[probes.Length];
            for (int i = 0; i < probes.Length; i++)
            {
                uint? revision = await probes[i].QueryRevision(ct);
                if (revision == null) return Fail(result, $"Bot {ids[i]}: inventory query before the fight got no snapshot.");
                revisions[i] = revision.Value;
            }

            bool[] killed = await Task.WhenAll(probes[0].KillBoss(ct), probes[2].KillBoss(ct));
            if (killed.Any(ok => !ok)) return Fail(result, "A party leader could not kill its boss.");
            result.ClearedParties = 2;

            for (int i = 0; i < probes.Length; i++)
            {
                if (!await probes[i].WaitForClearResultAndSnapshot(ct))
                    return Fail(result, $"Bot {ids[i]}: no S_DungeonClearResult followed by a snapshot after the boss died.");
                string? rewardFailure = probes[i].CheckReward(revisions[i]);
                if (rewardFailure != null) return Fail(result, $"Bot {ids[i]}: {rewardFailure}");
            }
            result.RewardedBots = 4;

            bool[] returned = await Task.WhenAll(probes.Select(probe => probe.ConfirmAndReturn(ct)));
            if (returned.Any(ok => !ok)) return Fail(result, "A bot did not return to Town after C_DungeonClearConfirm.");
            result.ReturnedBots = 4;

            // A late result from the other party's copy would show up here as a second one.
            await Task.Delay(QuietWindow, ct);
            foreach (ClearProbe probe in probes)
            {
                if (probe.ClearResultCount != 1)
                    return Fail(result, $"Bot {probe.LocalEntityId}: expected exactly one S_DungeonClearResult, got {probe.ClearResultCount}.");
            }
            result.Success = true;
            return result;
        }
        catch (TimeoutException ex)
        {
            return Fail(result, ex.Message);
        }
        finally
        {
            foreach (ClearProbe probe in probes) probe.Disconnect();
        }
    }

    static Result Fail(Result result, string reason)
    {
        result.Reason = reason;
        return result;
    }

    public sealed class Result
    {
        public bool Success;
        public string Reason = "";
        public int PartiesFormed;
        public int ClearedParties;
        public int RewardedBots;
        public int ReturnedBots;
    }

    sealed class ClearProbe : ProbeBase
    {
        readonly List<(PacketID Id, uint Revision)> _economy = new();
        readonly List<S_DungeonClearResult> _clearResults = new();
        S_PartyUpdate? _party;
        int _inviter = -1;
        int _map;
        int _transitions;
        int _bossId;
        float _bossX;
        int _bossHits;
        bool _bossDead;

        internal int PartyId { get { lock (Gate) return _party?.partyId ?? 0; } }

        internal int BossId { get { lock (Gate) return _bossId; } }

        internal int ClearResultCount { get { lock (Gate) return _clearResults.Count; } }

        internal void Invite(int peer) => Session?.Send(new C_PartyInvite { targetEntityId = peer }.Write());

        internal void Accept(int inviter) => Session?.Send(new C_PartyRespond { inviterEntityId = inviter, accept = 1 }.Write());

        internal void SendCheatCompleteQuest() => SendCheatCompleteQuestCore();

        internal Task<bool> WaitForInvite(int inviter, CancellationToken ct)
            => WaitUntil(() => { lock (Gate) return _inviter == inviter; }, Timeout, ct);

        internal Task<bool> WaitForParty(int first, int second, CancellationToken ct)
            => WaitUntil(() =>
            {
                lock (Gate)
                {
                    return _party is { partyId: > 0 } &&
                        new[] { _party.member0EntityId, _party.member1EntityId }.Order().SequenceEqual(new[] { first, second }.Order());
                }
            }, Timeout, ct);

        internal async Task<bool> MoveThroughPortal(float x, int portal, int destination, CancellationToken ct)
        {
            await MoveToPortalCore(x, ct);
            int expected;
            lock (Gate) expected = _transitions + 1;
            SendEnterPortalCore(portal);
            return await WaitUntil(() =>
            {
                lock (Gate) return _transitions == expected && _map == destination && (destination != BossRoomMapId || _bossId > 0);
            }, Timeout, ct);
        }

        internal async Task<uint?> QueryRevision(CancellationToken ct)
        {
            int before;
            lock (Gate) before = _economy.Count;
            Session?.Send(new C_InventoryRequest { reserved = 0 }.Write());
            bool answered = await WaitUntil(() => { lock (Gate) return _economy.Skip(before).Any(e => e.Id == PacketID.S_InventorySnapshot); }, Timeout, ct);
            if (!answered) return null;
            lock (Gate) return _economy.Skip(before).Last(e => e.Id == PacketID.S_InventorySnapshot).Revision;
        }

        // BossStageClearSmoke lines 135-175: reapproach after knockback, attack, retry a missed hit after the cooldown.
        internal async Task<bool> KillBoss(CancellationToken ct)
        {
            for (int attempt = 0; attempt < MaxKillAttempts; attempt++)
            {
                int hitsBefore;
                lock (Gate)
                {
                    if (_bossDead) return true;
                    hitsBefore = _bossHits;
                }
                if (!await EnsureInAttackRange(ct)) return false;
                SendAttack();
                await WaitUntil(() => { lock (Gate) return _bossDead || _bossHits > hitsBefore; }, CooldownWait, ct);
                lock (Gate)
                {
                    if (_bossDead) return true;
                }
                // A missed hit and a landed one both wait out the attack cooldown before the next try.
                await Task.Delay(CooldownWait, ct);
            }
            return await WaitUntil(() => { lock (Gate) return _bossDead; }, Timeout, ct);
        }

        internal Task<bool> WaitForClearResultAndSnapshot(CancellationToken ct)
            => WaitUntil(() =>
            {
                lock (Gate)
                {
                    int index = _economy.FindIndex(e => e.Id == PacketID.S_DungeonClearResult);
                    return index >= 0 && _economy.Skip(index + 1).Any(e => e.Id == PacketID.S_InventorySnapshot);
                }
            }, Timeout, ct);

        // Expected values come from the shared reward table and the bot's own pre-fight query, never from a recomputation.
        internal string? CheckReward(uint revisionBefore)
        {
            lock (Gate)
            {
                if (_clearResults.Count != 1) return $"expected one S_DungeonClearResult, got {_clearResults.Count}";
                S_DungeonClearResult clear = _clearResults[0];
                if (clear.result != (byte)InventoryResult.Success) return $"clear result {clear.result} is not Success";
                if (clear.currency != DungeonClearRewards.Currency) return $"clear gold {clear.currency} differs from the shared table";

                (int ItemId, int Count)[] slots =
                {
                    (clear.reward0ItemId, clear.reward0Count),
                    (clear.reward1ItemId, clear.reward1Count),
                    (clear.reward2ItemId, clear.reward2Count),
                };
                (int ItemId, int Count)[] expected = DungeonClearRewards.Items.Select(item => ((int)item.ItemId, item.Count)).Order().ToArray();
                if (!slots.Where(slot => slot != (0, 0)).Order().SequenceEqual(expected))
                    return "clear items differ from the shared table";

                int index = _economy.FindIndex(e => e.Id == PacketID.S_DungeonClearResult);
                (PacketID Id, uint Revision) next = _economy[index + 1];
                if (next.Id != PacketID.S_InventorySnapshot || next.Revision != revisionBefore + 1)
                    return $"the packet after the clear result must be a snapshot at revision {revisionBefore + 1}, got {next.Id} at {next.Revision}";
                return null;
            }
        }

        internal async Task<bool> ConfirmAndReturn(CancellationToken ct)
        {
            int expected;
            lock (Gate) expected = _transitions + 1;
            Session?.Send(new C_DungeonClearConfirm { reserved = 0 }.Write());
            return await WaitUntil(() => { lock (Gate) return _transitions == expected && _map == TownMapId; }, Timeout, ct);
        }

        protected override void OnMapTransition(S_MapTransition packet)
        {
            lock (Gate)
            {
                _map = packet.destMapId;
                _bossId = 0;
                _transitions++;
            }
        }

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            lock (Gate)
            {
                switch (id)
                {
                    case PacketID.S_PartyInviteRecv:
                        S_PartyInviteRecv invite = new();
                        invite.Read(buffer);
                        _inviter = invite.inviterEntityId;
                        break;
                    case PacketID.S_PartyUpdate:
                        S_PartyUpdate party = new();
                        party.Read(buffer);
                        _party = party;
                        break;
                    case PacketID.S_EntitySpawn:
                        S_EntitySpawn spawn = new();
                        spawn.Read(buffer);
                        if (_map == BossRoomMapId && spawn.entityKind == (byte)EnemyKind.Boss)
                        {
                            _bossId = spawn.entityId;
                            _bossX = spawn.x;
                            _bossDead = false;
                        }
                        break;
                    case PacketID.S_EntityState:
                        S_EntityState state = new();
                        state.Read(buffer);
                        if (state.entityId == _bossId) _bossX = state.x;
                        break;
                    case PacketID.S_HitResult:
                        S_HitResult hit = new();
                        hit.Read(buffer);
                        if (hit.targetEntityId == _bossId && hit.attackerEntityId == LocalEntityId)
                        {
                            _bossHits++;
                            if (hit.currentHp <= 0) _bossDead = true;
                        }
                        break;
                    case PacketID.S_EntityDeath:
                        S_EntityDeath death = new();
                        death.Read(buffer);
                        if (death.entityId == _bossId) _bossDead = true;
                        break;
                    case PacketID.S_InventorySnapshot:
                        S_InventorySnapshot snapshot = new();
                        snapshot.Read(buffer);
                        _economy.Add((id, snapshot.revision));
                        break;
                    case PacketID.S_DungeonClearResult:
                        S_DungeonClearResult clear = new();
                        clear.Read(buffer);
                        _clearResults.Add(clear);
                        _economy.Add((id, 0));
                        break;
                }
            }
        }

        async Task<bool> EnsureInAttackRange(CancellationToken ct)
        {
            for (int tick = 0; tick < MaxReapproachTicks; tick++)
            {
                float playerX = ServerX;
                float bossX;
                lock (Gate) bossX = _bossX;
                if (Math.Abs(playerX - bossX) <= ReapproachThreshold) return true;
                SendMove(bossX >= playerX ? (sbyte)1 : (sbyte)-1);
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            SendMove(0);
            await Task.Delay(Constants.TickIntervalMs, ct);
            lock (Gate) return Math.Abs(ServerX - _bossX) <= ReapproachThreshold;
        }

        void SendAttack()
        {
            int bossId;
            lock (Gate) bossId = _bossId;
            Session?.Send(new C_Attack { targetEntityId = bossId, attackerClientTick = LastReceivedServerTick }.Write());
        }
    }
}
