using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

public static class InstanceMapIsolationSmoke
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task<Result> Run(string host, int port, CancellationToken ct = default)
    {
        IsolationProbe[] probes = { new(), new(), new(), new() };
        Result result = new();
        try
        {
            foreach (IsolationProbe probe in probes) probe.Connect(host, port);
            foreach (IsolationProbe probe in probes)
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
                if (probes[i].PartyId != probes[i + 1].PartyId)
                    return Fail(result, $"Pair {i / 2}: party IDs disagree.");
            }
            if (probes[0].PartyId == probes[2].PartyId)
                return Fail(result, "The two pairs must have different party IDs.");
            result.PartiesFormed = 2;

            bool[] entered = await Task.WhenAll(probes.Select(probe => probe.MoveThroughPortal(20f, 1, 1, ct)));
            if (entered.Any(ok => !ok)) return Fail(result, "A bot did not reach HuntingGround.");
            for (int i = 0; i < probes.Length; i++)
            {
                if (!await probes[i].WaitForPeerAndSnapshots(ids[i ^ 1], ct))
                    return Fail(result, $"Bot {ids[i]}: party peer or live snapshots missing.");
            }
            for (int i = 0; i < probes.Length; i++)
            {
                if (!probes[i].SawOnlyPeer(ids[i ^ 1]))
                    return Fail(result, $"Bot {ids[i]}: player roster crossed the party boundary.");
            }
            int[][] enemyIds = probes.Select(probe => probe.EnemyIds()).ToArray();
            if (enemyIds.Any(enemies => enemies.Length == 0) ||
                !enemyIds[0].SequenceEqual(enemyIds[1]) || !enemyIds[2].SequenceEqual(enemyIds[3]) ||
                enemyIds[0].Intersect(enemyIds[2]).Any())
            {
                return Fail(result, "Enemy spawn packets must match within each party and be disjoint between parties.");
            }
            result.IsolatedBots = 4;

            bool[] returned = await Task.WhenAll(probes.Select(probe => probe.MoveThroughPortal(5f, 2, 0, ct)));
            if (returned.Any(ok => !ok)) return Fail(result, "A bot did not return to Town.");
            foreach (IsolationProbe probe in probes)
            {
                if (!await probe.WaitForTownRoster(ids, ct))
                    return Fail(result, $"Bot {probe.LocalEntityId}: reunited Town roster missing.");
                if (!probe.SawOnlyPeer(ids[Array.IndexOf(ids, probe.LocalEntityId) ^ 1]))
                    return Fail(result, "A foreign player was observed before leaving HuntingGround.");
            }
            result.ReturnedBots = 4;
            result.Success = true;
            return result;
        }
        catch (TimeoutException ex)
        {
            return Fail(result, ex.Message);
        }
        finally
        {
            foreach (IsolationProbe probe in probes) probe.Disconnect();
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
        public int IsolatedBots;
        public int ReturnedBots;
    }

    sealed class IsolationProbe : ProbeBase
    {
        readonly HashSet<int> _players = new();
        readonly HashSet<int> _seenInHuntingGround = new();
        readonly HashSet<int> _enemies = new();
        S_PartyUpdate? _party;
        int _inviter = -1;
        int _map;
        int _transitions;
        int _snapshots;

        internal int PartyId { get { lock (Gate) return _party?.partyId ?? 0; } }

        internal void Invite(int peer) => Session?.Send(new C_PartyInvite { targetEntityId = peer }.Write());
        internal void Accept(int inviter) => Session?.Send(new C_PartyRespond { inviterEntityId = inviter, accept = 1 }.Write());

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
                lock (Gate) return _transitions == expected && _map == destination;
            }, Timeout, ct);
        }

        internal Task<bool> WaitForPeerAndSnapshots(int peer, CancellationToken ct)
            => WaitUntil(() => { lock (Gate) return _map == 1 && _players.Contains(peer) && _snapshots >= 10; }, Timeout, ct);

        internal Task<bool> WaitForTownRoster(int[] ids, CancellationToken ct)
            => WaitUntil(() =>
            {
                lock (Gate) return _map == 0 && _snapshots >= 2 && _players.SetEquals(ids.Where(id => id != LocalEntityId));
            }, Timeout, ct);

        internal bool SawOnlyPeer(int peer)
        {
            lock (Gate) return _seenInHuntingGround.SetEquals(new[] { peer });
        }

        internal int[] EnemyIds()
        {
            lock (Gate) return _enemies.Order().ToArray();
        }

        protected override void OnMapTransition(S_MapTransition packet)
        {
            lock (Gate)
            {
                _map = packet.destMapId;
                _players.Clear();
                _enemies.Clear();
                _snapshots = 0;
                _transitions++;
            }
        }

        protected override void OnSnapshot(S_Snapshot packet)
        {
            lock (Gate) _snapshots++;
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
                    case PacketID.S_PlayerJoin:
                        S_PlayerJoin join = new();
                        join.Read(buffer);
                        _players.Add(join.entityId);
                        if (_map == 1) _seenInHuntingGround.Add(join.entityId);
                        break;
                    case PacketID.S_PlayerLeave:
                        S_PlayerLeave leave = new();
                        leave.Read(buffer);
                        _players.Remove(leave.entityId);
                        break;
                    case PacketID.S_EntitySpawn:
                        S_EntitySpawn spawn = new();
                        spawn.Read(buffer);
                        _enemies.Add(spawn.entityId);
                        break;
                }
            }
        }
    }
}
