using System.Buffers.Binary;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// One connection owns its wire history, sequence and pending request. A reconnect uses a new probe.
sealed class GuildGrowthProbe : ProbeBase
{
    static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(5);
    readonly string _account;
    readonly List<WireEvent> _economy = new();
    readonly List<S_HitResult> _hits = new();
    S_GuildSnapshot? _guild;
    S_InventorySnapshot? _inventory;
    S_PartyUpdate? _party;
    S_EntitySpawn? _boss;
    PendingAction? _pending;
    string? _wireFailure;
    long _sequence;
    uint _personalRevision;
    bool _queryPending;
    int _inviter;
    int _map;
    int _transitions;
    int _clearCount;
    bool _bossDead;
    float _bossX;

    internal GuildGrowthProbe(string account) => _account = account;

    internal string Account => _account;
    internal S_GuildSnapshot Guild { get { lock (Gate) return _guild ?? throw new InvalidOperationException("No guild snapshot."); } }
    internal S_InventorySnapshot Inventory { get { lock (Gate) return _inventory ?? throw new InvalidOperationException("No inventory snapshot."); } }
    internal S_EntitySpawn Boss { get { lock (Gate) return _boss ?? throw new InvalidOperationException("No boss spawn."); } }
    internal int PartyId { get { lock (Gate) return _party?.partyId ?? 0; } }
    internal int ClearCount { get { lock (Gate) return _clearCount; } }
    internal uint PersonalRevision { get { lock (Gate) return _personalRevision; } }
    protected override string AccountName => _account;

    internal void Invite(int peer) => Session!.Send(new C_PartyInvite { targetEntityId = peer }.Write());
    internal void Accept(int inviter) => Session!.Send(new C_PartyRespond { inviterEntityId = inviter, accept = 1 }.Write());
    internal void PrepareQuestGate() => SendCheatCompleteQuestCore();
    internal void ConfirmClear() => Session!.Send(new C_DungeonClearConfirm { reserved = 0 }.Write());

    internal Task WaitForInvite(int inviter, CancellationToken ct)
        => Require(() => _inviter == inviter, "party invitation", ct);

    internal Task WaitForParty(int first, int second, CancellationToken ct)
        => Require(() => _party is { partyId: > 0 } &&
            new[] { _party.member0EntityId, _party.member1EntityId }.Order()
                .SequenceEqual(new[] { first, second }.Order()), "two-member party", ct);

    internal Task WaitForOnline(int count, CancellationToken ct)
        => Require(() => _guild?.onlineMemberCount == count, $"online count {count}", ct);

    internal Task WaitForEntry(CancellationToken ct)
        => Require(() => WaitConnected(TimeSpan.Zero) && WaitHandshake(TimeSpan.Zero) && HandshakeOk &&
            WaitLogin(TimeSpan.Zero) && LoginResult == Shared.GameData.LoginResult.Accepted &&
            WaitEnterMap(TimeSpan.Zero) && LocalEntityId > 0, "handshake/login/select/Town entry", ct);

    internal async Task MoveThroughPortal(float x, int destination, CancellationToken ct)
    {
        await MoveToPortalCore(x, ct);
        int expected;
        lock (Gate) expected = _transitions + 1;
        SendEnterPortalCore(1);
        await Require(() => _transitions == expected && _map == destination &&
            (destination != 2 || _boss != null), $"portal to map {destination}", ct);
    }

    internal async Task ReturnToTown(CancellationToken ct)
    {
        int expected;
        lock (Gate) expected = _transitions + 1;
        ConfirmClear();
        await Require(() => _transitions == expected && _map == 0, "clear confirm to Town", ct);
    }

    internal async Task<S_GuildSnapshot> QueryGuild(CancellationToken ct)
    {
        long before;
        lock (Gate)
        {
            ThrowIfFailed();
            if (_pending != null || _queryPending) throw new InvalidOperationException("Guild request already pending.");
            before = _sequence;
            _queryPending = true;
        }
        Session!.Send(new C_GuildRequest { reserved = 0 }.Write());
        await Require(() => _economy.Any(e => e.Packet is S_GuildSnapshot s &&
            s.snapshotSequence > before && s.changeKind == (byte)GuildChangeKind.Query), "query44", ct);
        lock (Gate)
        {
            _queryPending = false;
            return (S_GuildSnapshot)_economy.Last(e => e.Packet is S_GuildSnapshot s &&
                s.snapshotSequence > before && s.changeKind == (byte)GuildChangeKind.Query).Packet;
        }
    }

    internal async Task<S_InventorySnapshot> QueryInventory(CancellationToken ct)
    {
        int before;
        lock (Gate) before = _economy.Count;
        Session!.Send(new C_InventoryRequest { reserved = 0 }.Write());
        await Require(() => _economy.Skip(before).Any(e => e.Id == PacketID.S_InventorySnapshot), "inventory query", ct);
        return Inventory;
    }

    internal void BeginAction(GuildAction action, uint requestedRevision)
    {
        lock (Gate)
        {
            ThrowIfFailed();
            if (_pending != null || _queryPending) throw new InvalidOperationException("Guild request already pending.");
            _pending = new PendingAction(action, requestedRevision, _economy.Count);
        }
        if (action == GuildAction.StrongholdUpgrade)
            Session!.Send(new C_StrongholdUpgrade { expectedGuildRevision = requestedRevision }.Write());
        else
            Session!.Send(new C_WeaponEnhance { expectedInventoryRevision = requestedRevision }.Write());
    }

    internal async Task<ActionObservation> FinishAction(CancellationToken ct)
    {
        await Require(() => _pending?.Snapshot != null, "47 and its exact target44", ct);
        lock (Gate)
        {
            PendingAction pending = _pending!;
            ActionObservation result = new(pending.Result!, pending.Snapshot!);
            _pending = null;
            return result;
        }
    }

    internal async Task<ClearObservation> ObserveClear(CancellationToken ct)
    {
        await Require(() =>
        {
            int index = _economy.FindIndex(e => e.Id == PacketID.S_DungeonClearResult);
            return index >= 0 && _economy.Count > index + 2;
        }, "39 -> 36 -> beneficiary44", ct);
        lock (Gate)
        {
            int index = _economy.FindIndex(e => e.Id == PacketID.S_DungeonClearResult);
            if (_economy[index + 1].Packet is not S_InventorySnapshot inventory ||
                _economy[index + 2].Packet is not S_GuildSnapshot guild ||
                guild.changeKind != (byte)GuildChangeKind.TreasuryCredit ||
                guild.inventoryRevision != inventory.revision)
            {
                throw new InvalidOperationException("Clear did not produce 39 -> 36 -> own TreasuryCredit44.");
            }
            S_InventorySnapshot before = (S_InventorySnapshot)_economy.Take(index)
                .Last(e => e.Id == PacketID.S_InventorySnapshot).Packet;
            return new ClearObservation((S_DungeonClearResult)_economy[index].Packet, before, inventory, guild);
        }
    }

    internal async Task<S_HitResult> MeasureMelee(CancellationToken ct)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            await ApproachBoss(ct);
            int before;
            int bossId;
            lock (Gate)
            {
                if (_bossDead) throw new InvalidOperationException("Cannot measure damage on a dead boss.");
                before = _hits.Count;
                bossId = _boss!.entityId;
            }
            Session!.Send(new C_Attack { targetEntityId = bossId, attackerClientTick = LastReceivedServerTick }.Write());
            bool hit = await WaitUntil(() =>
            {
                lock (Gate)
                {
                    ThrowIfFailed();
                    return _hits.Count > before;
                }
            }, TimeSpan.FromMilliseconds(550), ct);
            await Task.Delay(550, ct);
            if (hit)
            {
                lock (Gate)
                {
                    S_HitResult observed = _hits[before];
                    if (observed.hitEffect != (byte)HitEffect.Melee || observed.damage <= 0)
                        throw new InvalidOperationException($"Unexpected hit effect/damage {observed.hitEffect}/{observed.damage}.");
                    return observed;
                }
            }
        }
        throw new TimeoutException($"{Account}: no own melee Damage after eight attacks.");
    }

    internal async Task KillBoss(CancellationToken ct)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            lock (Gate)
            {
                if (_bossDead) return;
            }
            await MeasureMelee(ct);
        }
        throw new TimeoutException("Boss did not die after bounded real attacks.");
    }

    internal void CheckHealthy()
    {
        lock (Gate) ThrowIfFailed();
    }

    internal static int CountItem(S_InventorySnapshot inventory, ItemId item)
    {
        (int Id, int Count)[] slots =
        {
            (inventory.slot0ItemId, inventory.slot0Count), (inventory.slot1ItemId, inventory.slot1Count),
            (inventory.slot2ItemId, inventory.slot2Count), (inventory.slot3ItemId, inventory.slot3Count),
            (inventory.slot4ItemId, inventory.slot4Count), (inventory.slot5ItemId, inventory.slot5Count),
            (inventory.slot6ItemId, inventory.slot6Count), (inventory.slot7ItemId, inventory.slot7Count),
        };
        return slots.Where(slot => slot.Id == (int)item).Sum(slot => slot.Count);
    }

    protected override void OnMapTransition(S_MapTransition packet)
    {
        lock (Gate)
        {
            _map = packet.destMapId;
            _transitions++;
            _boss = null;
            _bossDead = false;
            _hits.Clear();
        }
    }

    protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
    {
        lock (Gate)
        {
            try
            {
                switch (id)
                {
                    case PacketID.S_GuildSnapshot:
                        ReadGuild(buffer);
                        break;
                    case PacketID.S_GuildActionResult:
                        ReadAction(buffer);
                        break;
                    case PacketID.S_InventorySnapshot:
                        S_InventorySnapshot inventory = new();
                        inventory.Read(buffer);
                        _inventory = inventory;
                        _personalRevision = Math.Max(_personalRevision, inventory.revision);
                        _economy.Add(new WireEvent(id, inventory));
                        Console.WriteLine($"[Guild wire] account={Account} id=36 rev={inventory.revision} gold={inventory.currency} material={CountItem(inventory, ItemId.Material)} pouch={CountItem(inventory, ItemId.CoinPouch)}");
                        break;
                    case PacketID.S_DungeonClearResult:
                        S_DungeonClearResult clear = new();
                        clear.Read(buffer);
                        _clearCount++;
                        _economy.Add(new WireEvent(id, clear));
                        Console.WriteLine($"[Guild wire] account={Account} id=39 result={clear.result} gold={clear.currency} rewards={clear.reward0ItemId}:{clear.reward0Count},{clear.reward1ItemId}:{clear.reward1Count},{clear.reward2ItemId}:{clear.reward2Count}");
                        break;
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
                        if (_map == 2 && spawn.entityKind == (byte)EnemyKind.Boss)
                        {
                            _boss = spawn;
                            _bossX = spawn.x;
                            _bossDead = false;
                        }
                        break;
                    case PacketID.S_EntityState:
                        S_EntityState state = new();
                        state.Read(buffer);
                        if (state.entityId == _boss?.entityId) _bossX = state.x;
                        break;
                    case PacketID.S_HitResult:
                        S_HitResult hit = new();
                        hit.Read(buffer);
                        if (hit.targetEntityId == _boss?.entityId && hit.attackerEntityId == LocalEntityId)
                        {
                            _hits.Add(hit);
                            Console.WriteLine($"[Guild wire] account={Account} attacker={hit.attackerEntityId} boss={hit.targetEntityId} damage={hit.damage} hp={hit.currentHp}/{hit.maxHp} effect={hit.hitEffect}");
                        }
                        break;
                    case PacketID.S_EntityDeath:
                        S_EntityDeath death = new();
                        death.Read(buffer);
                        if (death.entityId == _boss?.entityId) _bossDead = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _wireFailure ??= $"{id}: {ex.Message}";
            }
        }
    }

    async Task Require(Func<bool> predicate, string description, CancellationToken ct)
    {
        bool arrived = await WaitUntil(() =>
        {
            lock (Gate)
            {
                ThrowIfFailed();
                return predicate();
            }
        }, ReplyTimeout, ct);
        if (!arrived) throw new TimeoutException($"{Account}: timeout waiting for {description}.");
    }

    async Task ApproachBoss(CancellationToken ct)
    {
        for (int tick = 0; tick < 80; tick++)
        {
            float x;
            lock (Gate)
            {
                ThrowIfFailed();
                x = _bossX;
            }
            if (Math.Abs(ServerX - x) <= 1f)
            {
                SendMove(0);
                await Task.Delay(Constants.TickIntervalMs * 2, ct);
                return;
            }
            SendMove(ServerX < x ? (sbyte)1 : (sbyte)-1);
            await Task.Delay(Constants.TickIntervalMs, ct);
        }
        SendMove(0);
        throw new TimeoutException($"{Account}: boss approach failed at x={ServerX}.");
    }

    void ReadGuild(ArraySegment<byte> buffer)
    {
        // Generated Read trusts its variable string length. Validate the full frame first.
        ValidateHeader(buffer, PacketID.S_GuildSnapshot, 66, exact: false);
        ushort nameBytes = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(64, 2));
        if (nameBytes < 2 || nameBytes > 32 || nameBytes % 2 != 0 || buffer.Count != 66 + nameBytes)
            throw new InvalidOperationException($"Invalid guild name/frame length {nameBytes}/{buffer.Count}.");
        S_GuildSnapshot snapshot = new();
        snapshot.Read(buffer);
        ValidateGuild(snapshot);
        if (snapshot.snapshotSequence <= _sequence)
            throw new InvalidOperationException($"Non-increasing guild sequence {_sequence}->{snapshot.snapshotSequence}.");
        _sequence = snapshot.snapshotSequence;
        // Keep an older personal revision from replacing the already observed personal state.
        // The raw event still participates in sequence/action correlation; a query refreshes the view.
        if (snapshot.inventoryRevision >= _personalRevision) _guild = snapshot;
        _personalRevision = Math.Max(_personalRevision, snapshot.inventoryRevision);
        _economy.Add(new WireEvent(PacketID.S_GuildSnapshot, snapshot));
        Console.WriteLine($"[Guild wire] account={Account} id=44 seq={snapshot.snapshotSequence} kind={snapshot.changeKind} guild={snapshot.guildId} rev={snapshot.guildRevision} treasury={snapshot.treasury} level={snapshot.strongholdLevel} online={snapshot.onlineMemberCount}/{snapshot.memberCount} personalRev={snapshot.inventoryRevision} weapon={snapshot.weaponLevel} bonus={snapshot.attackBonus}");

        // A broadcast may update display state but only this result's exact target completes the action.
        if (_pending?.Result is not { } result || result.snapshotSequence != snapshot.snapshotSequence) return;
        if (result.guildRevision != snapshot.guildRevision || result.inventoryRevision != snapshot.inventoryRevision)
            throw new InvalidOperationException("47/44 revisions disagree.");
        GuildChangeKind expectedKind = result.result == (byte)GuildResult.Success
            ? (_pending.Action == GuildAction.StrongholdUpgrade ? GuildChangeKind.StrongholdUpgrade : GuildChangeKind.WeaponEnhance)
            : GuildChangeKind.Query;
        if (snapshot.changeKind != (byte)expectedKind) throw new InvalidOperationException("47/44 change kind disagrees.");
        int resultIndex = _economy.FindIndex(_pending.Start, e => ReferenceEquals(e.Packet, result));
        bool needsInventory = _pending.Action == GuildAction.WeaponEnhance &&
            (result.result == (byte)GuildResult.Success || result.result == (byte)GuildResult.Stale);
        int expectedDistance = needsInventory ? 3 : 2;
        if (_economy.Count - resultIndex != expectedDistance ||
            (needsInventory && (_economy[resultIndex + 1].Packet is not S_InventorySnapshot inventory ||
                inventory.revision != result.inventoryRevision)))
        {
            throw new InvalidOperationException("Action response order must be 47 -> [36] -> target44.");
        }
        _pending.Snapshot = snapshot;
    }

    void ReadAction(ArraySegment<byte> buffer)
    {
        ValidateHeader(buffer, PacketID.S_GuildActionResult, 26, exact: true);
        S_GuildActionResult result = new();
        result.Read(buffer);
        if (result.action < 1 || result.action > 2 || result.result > (byte)GuildResult.Unavailable ||
            result.snapshotSequence <= _sequence || _pending == null || _pending.Result != null ||
            result.action != (byte)_pending.Action || result.requestedRevision != _pending.Revision)
        {
            throw new InvalidOperationException($"Unexpected47 action={result.action} result={result.result} requested={result.requestedRevision} target={result.snapshotSequence}.");
        }
        _pending.Result = result;
        _economy.Add(new WireEvent(PacketID.S_GuildActionResult, result));
        Console.WriteLine($"[Guild wire] account={Account} id=47 action={result.action} result={result.result} requested={result.requestedRevision} target={result.snapshotSequence}");
    }

    void ThrowIfFailed()
    {
        if (_wireFailure != null) throw new InvalidOperationException($"{Account}: {_wireFailure}");
        if (WaitDisconnected(TimeSpan.Zero)) throw new InvalidOperationException($"{Account}: disconnected while awaiting observations.");
    }

    static void ValidateHeader(ArraySegment<byte> buffer, PacketID id, int length, bool exact)
    {
        if (buffer.Count < length || (exact && buffer.Count != length) ||
            BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2)) != buffer.Count ||
            BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2)) != (ushort)id)
        {
            throw new InvalidOperationException($"Invalid {id} header/size {buffer.Count}.");
        }
    }

    static void ValidateGuild(S_GuildSnapshot s)
    {
        if (s.snapshotSequence <= 0 || s.changeKind > (byte)GuildChangeKind.Presence || s.guildId == 0 ||
            s.treasury < 0 || s.treasury > 1_000_000_000 || s.strongholdLevel < 1 || s.strongholdLevel > 2 ||
            s.maxStrongholdLevel != 2 || s.blacksmithUnlocked != (s.strongholdLevel == 2 ? 1 : 0) ||
            s.memberCount < 1 || s.onlineMemberCount < 0 || s.onlineMemberCount > s.memberCount ||
            s.weaponLevel > 1 || s.maxWeaponLevel != 1 || s.attackBonus != (s.weaponLevel == 1 ? 5 : 0) ||
            s.strongholdCost != (s.strongholdLevel == 2 ? 0 : 30) ||
            s.weaponCurrencyCost != (s.weaponLevel == 1 ? 0 : 20) || s.weaponMaterialId != 1 ||
            s.weaponMaterialCount != (s.weaponLevel == 1 ? 0 : 1) ||
            s.strongholdAvailability > (byte)GuildResult.Unavailable || s.weaponAvailability > (byte)GuildResult.Unavailable)
        {
            throw new InvalidOperationException("Guild snapshot violates v20 enum/range/field relations.");
        }
    }

    internal sealed record ActionObservation(S_GuildActionResult Result, S_GuildSnapshot Snapshot);
    internal sealed record ClearObservation(S_DungeonClearResult Result, S_InventorySnapshot Before,
        S_InventorySnapshot After, S_GuildSnapshot Guild);
    sealed record WireEvent(PacketID Id, object Packet);

    sealed class PendingAction
    {
        internal PendingAction(GuildAction action, uint revision, int start)
        {
            Action = action;
            Revision = revision;
            Start = start;
        }

        internal GuildAction Action { get; }
        internal uint Revision { get; }
        internal int Start { get; }
        internal S_GuildActionResult? Result { get; set; }
        internal S_GuildSnapshot? Snapshot { get; set; }
    }
}
