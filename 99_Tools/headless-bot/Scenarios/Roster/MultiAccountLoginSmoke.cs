using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

public static class MultiAccountLoginSmoke
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task<Result> Run(string host, int port, CancellationToken ct = default)
    {
        string firstName = ProbeBase.CreateAccountName();
        Probe first = new(firstName);
        Probe second = new(ProbeBase.CreateAccountName());
        Probe duplicate = new(firstName);
        Probe reconnect = new(firstName);
        Probe invalid = new("bad-name");
        try
        {
            first.Connect(host, port);
            second.Connect(host, port);
            if (!Entered(first) || !Entered(second)) return new(false, "Two distinct accounts did not enter.");
            if (first.LocalEntityId == second.LocalEntityId) return new(false, "Distinct accounts share an entity.");

            int snapshotsBefore = first.SnapshotCount;
            duplicate.Connect(host, port);
            if (!duplicate.WaitLogin(Timeout) || duplicate.LoginResult != LoginResult.AlreadyOnline)
                return new(false, "Duplicate account did not receive AlreadyOnline.");
            if (!duplicate.WaitDisconnected(Timeout)) return new(false, "Duplicate connection stayed open.");
            if (!await first.WaitSnapshotsAfter(snapshotsBefore, ct))
                return new(false, "Original connection stopped receiving snapshots.");

            first.Disconnect();
            if (!await second.WaitLeave(first.LocalEntityId, ct))
                return new(false, "Other client did not observe the original player's departure.");
            reconnect.Connect(host, port);
            if (!Entered(reconnect)) return new(false, "Released account could not enter again.");

            invalid.Connect(host, port);
            if (!invalid.WaitLogin(Timeout) || invalid.LoginResult != LoginResult.InvalidName)
                return new(false, "Invalid name did not receive InvalidName.");
            if (!invalid.WaitDisconnected(Timeout)) return new(false, "Invalid-name connection stayed open.");

            return new(true, "Distinct accounts, duplicate rejection, continuing snapshots and relogin observed.");
        }
        finally
        {
            first.Disconnect();
            second.Disconnect();
            duplicate.Disconnect();
            reconnect.Disconnect();
            invalid.Disconnect();
        }
    }

    static bool Entered(Probe probe)
        => probe.WaitConnected(Timeout) && probe.WaitHandshake(Timeout) && probe.HandshakeOk &&
           probe.WaitLogin(Timeout) && probe.LoginResult == LoginResult.Accepted && probe.WaitEnterMap(Timeout);

    public sealed record Result(bool Success, string Reason);

    sealed class Probe : ProbeBase
    {
        readonly string _name;
        readonly HashSet<int> _departed = new();
        int _snapshotCount;

        internal Probe(string name) => _name = name;

        internal int SnapshotCount => Volatile.Read(ref _snapshotCount);

        protected override string AccountName => _name;

        internal Task<bool> WaitSnapshotsAfter(int previous, CancellationToken ct)
            => WaitUntil(() => SnapshotCount > previous, Timeout, ct);

        internal Task<bool> WaitLeave(int entityId, CancellationToken ct)
            => WaitUntil(() => { lock (Gate) return _departed.Contains(entityId); }, Timeout, ct);

        protected override void OnSnapshot(S_Snapshot packet) => Interlocked.Increment(ref _snapshotCount);

        protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
        {
            if (id != PacketID.S_PlayerLeave) return;
            S_PlayerLeave leave = new();
            leave.Read(buffer);
            lock (Gate) _departed.Add(leave.entityId);
        }
    }
}
