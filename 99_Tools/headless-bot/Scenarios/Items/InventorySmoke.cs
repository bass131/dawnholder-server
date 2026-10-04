using System.Buffers.Binary;
using System.Diagnostics;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// Server-authoritative economy on a fresh GameServer, observed end to end by one Knight bot.
// Flow: new connection → empty snapshot → walk to HuntingGround → kill the real Golem with real
// melee attacks (no cheat, no direct grant) → reward snapshot → use the owned CoinPouch → exact
// currency/pouch change → resend the same use bytes after completion → Stale, state unchanged.
// Economy packets are decoded with the fixed wire table of the PR1 acceptance (offsets in Wire),
// and every expected value is a literal from that table. A timeout is a failure, never a success.
public class InventorySmoke
{
    const int TownPortalId = 1;
    const float TownPortalX = 20f;
    const byte GolemKind = 2;
    const int CoinPouchId = 2;
    static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(3);
    static readonly TimeSpan KillTimeout = TimeSpan.FromSeconds(40);
    static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(600);

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int LocalEntityId;
        public int GolemEntityId;
        public int AttacksSent;
        public List<string> Observations = new();
    }

    public static async Task<Result> Run(string host, int port, CancellationToken ct = default)
    {
        Result result = new();
        EconomyProbe bot = new();
        try
        {
            bot.Connect(host, port);
            if (!bot.WaitConnected(StepTimeout)) return Fail(result, "connect timeout");
            if (!bot.WaitHandshake(StepTimeout)) return Fail(result, "S_HandshakeResult timeout");
            if (!bot.HandshakeOk) return Fail(result, $"handshake rejected: {bot.HandshakeReason}");
            if (!bot.WaitEnterMap(StepTimeout)) return Fail(result, "S_EnterMap timeout");
            if (!await bot.WaitForServerTick(StepTimeout, ct)) return Fail(result, "S_Snapshot timeout");
            result.LocalEntityId = bot.LocalEntityId;

            // 1) A new connection reads the empty authoritative state.
            byte[]? first = await bot.Request(Wire.InventoryRequest(), replies: 1, ReplyTimeout, ct);
            if (first == null) return Fail(result, "initial snapshot timeout");
            result.Observations.Add("initial " + Wire.Describe(first));
            string? initialError = Wire.CheckSnapshot(first, revision: 0, currency: 0);
            if (initialError != null) return Fail(result, "initial snapshot: " + initialError);

            // 2) Real kill: walk through the Town portal, approach the HuntingGround Golem, melee it.
            await bot.MoveToPortal(TownPortalX, ct);
            bot.EnterPortal(TownPortalId);
            if (!await bot.WaitMapTransition(StepTimeout, ct)) return Fail(result, "S_MapTransition timeout");
            int? golem = await bot.WaitForLiveSpawn(GolemKind, StepTimeout, ct);
            if (golem == null) return Fail(result, "Golem S_EntitySpawn timeout");
            result.GolemEntityId = golem.Value;
            int economyBeforeKill = bot.EconomyCount;
            bool killed = await bot.MeleeUntilDead(golem.Value, KillTimeout, ct);
            result.AttacksSent = bot.AttacksSent;
            result.Observations.Add($"golem={golem.Value} killed={killed} attacks={bot.AttacksSent} hits={bot.HitsOn(golem.Value)}");
            if (!killed) return Fail(result, "Golem was not killed within the kill timeout");

            // 3) The kill reward arrives as the server's push: pouch 1 + currency 10 at revision 1.
            byte[]? reward = await bot.WaitEconomy(economyBeforeKill + 1, ReplyTimeout, ct);
            if (reward == null) return Fail(result, "reward snapshot timeout after the real kill");
            result.Observations.Add("reward " + Wire.Describe(reward));
            string? rewardError = Wire.CheckSnapshot(reward, revision: 1, currency: 10, (CoinPouchId, 1));
            if (rewardError != null) return Fail(result, "reward snapshot: " + rewardError);
            bot.StopMoving();

            // 4) Use the owned pouch: Success, then pouch 0 and currency 60 at revision 2.
            byte[] use = Wire.ItemUse(CoinPouchId, expectedRevision: 1);
            byte[][]? used = await bot.RequestAll(use, replies: 2, ReplyTimeout, ct);
            if (used == null) return Fail(result, "use replies timeout");
            result.Observations.Add("use " + Wire.Describe(used[0]) + " then " + Wire.Describe(used[1]));
            string? useError = Wire.CheckUseResult(used[0], Wire.Success, CoinPouchId, revision: 2)
                ?? Wire.CheckSnapshot(used[1], revision: 2, currency: 60);
            if (useError != null) return Fail(result, "use: " + useError);

            // 5) The same bytes after completion are a replay: Stale and the unchanged state.
            byte[][]? replayed = await bot.RequestAll(use, replies: 2, ReplyTimeout, ct);
            if (replayed == null) return Fail(result, "replay replies timeout");
            result.Observations.Add("replay " + Wire.Describe(replayed[0]) + " then " + Wire.Describe(replayed[1]));
            string? replayError = Wire.CheckUseResult(replayed[0], Wire.Stale, CoinPouchId, revision: 2)
                ?? Wire.CheckSnapshot(replayed[1], revision: 2, currency: 60);
            if (replayError != null) return Fail(result, "replay: " + replayError);

            // 6) A final query and a quiet window: nothing was granted or consumed twice.
            byte[]? final = await bot.Request(Wire.InventoryRequest(), replies: 1, ReplyTimeout, ct);
            if (final == null) return Fail(result, "final snapshot timeout");
            result.Observations.Add("final " + Wire.Describe(final));
            string? finalError = Wire.CheckSnapshot(final, revision: 2, currency: 60);
            if (finalError != null) return Fail(result, "final snapshot: " + finalError);
            await Task.Delay(QuietWindow, ct);
            result.Observations.Add($"economy packets total={bot.EconomyCount}");
            if (bot.EconomyCount != 7) return Fail(result, $"expected 7 economy packets in total, saw {bot.EconomyCount}");

            result.Success = true;
            return result;
        }
        catch (TimeoutException ex)
        {
            return Fail(result, ex.Message);
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

    sealed class EconomyProbe : ProbeBase
    {
        // Knight melee reach: the attack box half-width is 1.5 (CombatConstants.KnightAttackHalfX).
        const float MeleeDistance = 1.0f;
        static readonly TimeSpan AttackInterval = TimeSpan.FromMilliseconds((Constants.AttackCooldownTicks + 2) * Constants.TickIntervalMs);

        readonly ManualResetEventSlim _mapTransition = new(false);
        readonly List<byte[]> _economy = new();
        readonly List<(int EntityId, byte Kind, int Hp)> _spawns = new();
        readonly Dictionary<int, float> _enemyX = new();
        readonly HashSet<int> _dead = new();
        readonly List<int> _hitTargets = new();

        public int AttacksSent { get; private set; }

        public int EconomyCount
        {
            get
            {
                lock (Gate) return _economy.Count;
            }
        }

        public Task<bool> WaitForServerTick(TimeSpan timeout, CancellationToken ct)
            => WaitUntil(() => LastReceivedServerTick > 0, timeout, ct);

        public Task<bool> WaitMapTransition(TimeSpan timeout, CancellationToken ct)
            => WaitUntil(() => _mapTransition.IsSet, timeout, ct);

        public Task MoveToPortal(float portalX, CancellationToken ct) => MoveToPortalCore(portalX, ct);

        public void EnterPortal(int portalId) => SendEnterPortalCore(portalId);

        public void StopMoving() => SendMove(0);

        public int HitsOn(int entityId)
        {
            lock (Gate) return _hitTargets.Count(target => target == entityId);
        }

        public async Task<int?> WaitForLiveSpawn(byte kind, TimeSpan timeout, CancellationToken ct)
        {
            bool found = await WaitUntil(() => FindSpawn(kind) != null, timeout, ct);
            return found ? FindSpawn(kind) : null;
        }

        // Walks toward the target's latest server position and swings once per cooldown when close.
        public async Task<bool> MeleeUntilDead(int target, TimeSpan timeout, CancellationToken ct)
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            Stopwatch sinceAttack = Stopwatch.StartNew();
            bool attacked = false;
            while (elapsed.Elapsed < timeout)
            {
                float targetX;
                lock (Gate)
                {
                    if (_dead.Contains(target)) return true;
                    targetX = _enemyX[target];
                }
                float gap = targetX - ServerX;
                if (Math.Abs(gap) > MeleeDistance)
                {
                    SendMove(gap > 0 ? (sbyte)1 : (sbyte)-1);
                }
                else
                {
                    SendMove(0);
                    if (!attacked || sinceAttack.Elapsed >= AttackInterval)
                    {
                        Session?.Send(new C_Attack { targetEntityId = target, attackerClientTick = LastReceivedServerTick }.Write());
                        AttacksSent++;
                        attacked = true;
                        sinceAttack.Restart();
                    }
                }
                await Task.Delay(Constants.TickIntervalMs, ct);
            }
            lock (Gate) return _dead.Contains(target);
        }

        public async Task<byte[]?> WaitEconomy(int count, TimeSpan timeout, CancellationToken ct)
        {
            bool arrived = await WaitUntil(() => EconomyCount >= count, timeout, ct);
            if (!arrived) return null;
            lock (Gate) return _economy[count - 1];
        }

        public async Task<byte[]?> Request(byte[] packet, int replies, TimeSpan timeout, CancellationToken ct)
        {
            byte[][]? all = await RequestAll(packet, replies, timeout, ct);
            return all?[^1];
        }

        // Sends a copy of the bytes and waits for exactly the next `replies` economy packets.
        public async Task<byte[][]?> RequestAll(byte[] packet, int replies, TimeSpan timeout, CancellationToken ct)
        {
            int mark = EconomyCount;
            Session?.Send(new ArraySegment<byte>(packet.ToArray()));
            bool arrived = await WaitUntil(() => EconomyCount >= mark + replies, timeout, ct);
            if (!arrived) return null;
            lock (Gate) return _economy.Skip(mark).Take(replies).ToArray();
        }

        protected override void OnMapTransition(S_MapTransition packet) => _mapTransition.Set();

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            byte[] bytes = buffer.ToArray();
            ushort rawId = Wire.IdOf(bytes);
            lock (Gate)
            {
                if (rawId is Wire.InventorySnapshotId or Wire.ItemUseResultId)
                {
                    _economy.Add(bytes);
                    return;
                }
                switch (id)
                {
                    case PacketID.S_EntitySpawn:
                        S_EntitySpawn spawn = new();
                        spawn.Read(buffer);
                        _spawns.Add((spawn.entityId, spawn.entityKind, spawn.currentHp));
                        _enemyX[spawn.entityId] = spawn.x;
                        _dead.Remove(spawn.entityId);
                        break;
                    case PacketID.S_EntityState:
                        S_EntityState state = new();
                        state.Read(buffer);
                        _enemyX[state.entityId] = state.x;
                        break;
                    case PacketID.S_HitResult:
                        S_HitResult hit = new();
                        hit.Read(buffer);
                        _hitTargets.Add(hit.targetEntityId);
                        break;
                    case PacketID.S_EntityDeath:
                        S_EntityDeath death = new();
                        death.Read(buffer);
                        _dead.Add(death.entityId);
                        break;
                }
            }
        }

        int? FindSpawn(byte kind)
        {
            lock (Gate)
            {
                foreach ((int entityId, byte spawnKind, int hp) in _spawns)
                {
                    if (spawnKind == kind && hp > 0 && !_dead.Contains(entityId)) return entityId;
                }
                return null;
            }
        }
    }

    // Fixed wire table of the PR1 acceptance: LittleEndian, 4-byte header (ushort size including the
    // header, ushort id); 36 snapshot = uint revision, int currency, 8 × (int itemId, int count);
    // 38 result = byte result, int itemId, uint revision.
    static class Wire
    {
        internal const ushort InventoryRequestId = 35;
        internal const ushort InventorySnapshotId = 36;
        internal const ushort ItemUseId = 37;
        internal const ushort ItemUseResultId = 38;
        internal const byte Success = 0;
        internal const byte Stale = 1;
        const int SnapshotLength = 76;
        const int UseResultLength = 13;
        const int SlotCount = 8;

        internal static byte[] InventoryRequest() => Frame(InventoryRequestId, new byte[] { 0 });

        internal static byte[] ItemUse(int itemId, uint expectedRevision)
        {
            byte[] payload = new byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), itemId);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), expectedRevision);
            return Frame(ItemUseId, payload);
        }

        internal static ushort IdOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));

        internal static string? CheckSnapshot(byte[] packet, uint revision, int currency, params (int ItemId, int Count)[] occupied)
        {
            if (IdOf(packet) != InventorySnapshotId) return $"expected snapshot, got id {IdOf(packet)}";
            if (packet.Length != SnapshotLength) return $"snapshot length {packet.Length}";
            uint actualRevision = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4, 4));
            int actualCurrency = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8, 4));
            if (actualRevision != revision) return $"revision {actualRevision}, expected {revision}";
            if (actualCurrency != currency) return $"currency {actualCurrency}, expected {currency}";
            for (int i = 0; i < SlotCount; i++)
            {
                (int ItemId, int Count) expected = i < occupied.Length ? occupied[i] : (0, 0);
                (int ItemId, int Count) actual = Slot(packet, i);
                if (actual != expected) return $"slot{i} {actual}, expected {expected}";
            }
            return null;
        }

        internal static string? CheckUseResult(byte[] packet, byte result, int itemId, uint revision)
        {
            if (IdOf(packet) != ItemUseResultId) return $"expected use result, got id {IdOf(packet)}";
            if (packet.Length != UseResultLength) return $"use result length {packet.Length}";
            int actualItemId = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(5, 4));
            uint actualRevision = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(9, 4));
            if (packet[4] != result) return $"result {packet[4]}, expected {result}";
            if (actualItemId != itemId) return $"itemId {actualItemId}, expected {itemId}";
            if (actualRevision != revision) return $"revision {actualRevision}, expected {revision}";
            return null;
        }

        internal static string Describe(byte[] packet)
        {
            if (IdOf(packet) == InventorySnapshotId && packet.Length == SnapshotLength)
            {
                uint revision = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4, 4));
                int currency = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8, 4));
                string slots = string.Join(" ", Enumerable.Range(0, SlotCount).Select(i => Slot(packet, i))
                    .Select(slot => $"{slot.ItemId}/{slot.Count}"));
                return $"snapshot(len={packet.Length} rev={revision} currency={currency} slots={slots})";
            }
            if (IdOf(packet) == ItemUseResultId && packet.Length == UseResultLength)
            {
                return $"useResult(len={packet.Length} result={packet[4]} " +
                       $"itemId={BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(5, 4))} " +
                       $"rev={BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(9, 4))})";
            }
            return $"packet(id={IdOf(packet)} len={packet.Length})";
        }

        static (int ItemId, int Count) Slot(byte[] packet, int index)
        {
            int offset = 12 + (index * 8);
            return (BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset + 4, 4)));
        }

        static byte[] Frame(ushort id, byte[] payload)
        {
            byte[] packet = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), id);
            payload.CopyTo(packet, 4);
            return packet;
        }
    }
}
