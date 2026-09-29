using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Party;

namespace Dawnholder.Server.GameServer.Quest;

// Owns solo/party progress and entity-lifetime unlocks. All state access stays on the world tick thread.
// GameWorld drains Party before Quest; membership is resolved when each quest job executes.
public sealed class QuestRegistry
{
    readonly ConcurrentQueue<Action> _pendingJobs = new();
    readonly Func<int, PartyMembership?> _lookupMembership;
    readonly Dictionary<int, int> _soloProgress = new();
    readonly Dictionary<int, int> _partyProgress = new();
    readonly HashSet<int> _bossUnlocked = new();

    public QuestRegistry(Func<int, PartyMembership?> lookupMembership)
    {
        _lookupMembership = lookupMembership ?? throw new ArgumentNullException(nameof(lookupMembership));
    }

    internal int TrackedPartyProgressCount => _partyProgress.Count;

    public void EnqueueJob(Action job) => _pendingJobs.Enqueue(job);

    public void Tick(long currentTick)
    {
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try { job(); }
            catch (Exception ex) { Console.WriteLine($"[QuestRegistry] job 예외: {ex.Message}"); }
        }
    }

    /// <summary>Credit one kill on the tick thread and return captured notifications in member order.</summary>
    public IReadOnlyList<QuestProgressUpdate> OnKill(int killerEntityId)
    {
        PartyMembership? membership = _lookupMembership(killerEntityId);
        int count;
        if (membership != null)
        {
            count = GetPartyProgress(membership.PartyId) + 1;
            _partyProgress[membership.PartyId] = count;
        }
        else
        {
            count = GetSoloProgress(killerEntityId) + 1;
            _soloProgress[killerEntityId] = count;
        }

        return CaptureUpdates(membership?.MemberIds ?? new[] { killerEntityId }, count);
    }

#if DEBUG
    /// <summary>Set current solo/party progress to the threshold. The caller remains a guarded quest job.</summary>
    public IReadOnlyList<QuestProgressUpdate> DebugCompleteQuest(int entityId)
    {
        int target = QuestConstants.BossUnlockKillCount;
        PartyMembership? membership = _lookupMembership(entityId);
        if (membership != null)
            _partyProgress[membership.PartyId] = target;
        else
            _soloProgress[entityId] = target;

        return CaptureUpdates(membership?.MemberIds ?? new[] { entityId }, target);
    }
#endif

    /// <summary>Boss death resets all raw progress without changing unlocks or emitting notifications.</summary>
    public void ResetAllQuestProgress()
    {
        _soloProgress.Clear();
        _partyProgress.Clear();
    }

    /// <summary>Queued by the world after a successful disband; solo progress and unlocks are retained.</summary>
    public void ForgetPartyProgress(int partyId) => _partyProgress.Remove(partyId);

    /// <summary>Boss gate query on the world tick thread. An existing entity unlock takes precedence.</summary>
    public int GetKillCount(int entityId)
    {
        if (_bossUnlocked.Contains(entityId)) return QuestConstants.BossUnlockKillCount;

        PartyMembership? membership = _lookupMembership(entityId);
        return membership != null ? GetPartyProgress(membership.PartyId) : GetSoloProgress(entityId);
    }

    internal int GetSoloProgress(int entityId)
        => _soloProgress.TryGetValue(entityId, out int count) ? count : 0;

    internal int GetPartyProgress(int partyId)
        => _partyProgress.TryGetValue(partyId, out int count) ? count : 0;

    internal bool IsBossUnlocked(int entityId) => _bossUnlocked.Contains(entityId);

    IReadOnlyList<QuestProgressUpdate> CaptureUpdates(IReadOnlyList<int> recipients, int count)
    {
        int target = QuestConstants.BossUnlockKillCount;
        int shown = Math.Min(count, target);
        QuestProgressUpdate[] updates = new QuestProgressUpdate[recipients.Count];
        for (int i = 0; i < recipients.Count; i++)
        {
            int recipient = recipients[i];
            if (count >= target) _bossUnlocked.Add(recipient);
            updates[i] = new QuestProgressUpdate(recipient, shown, target);
        }

        return Array.AsReadOnly(updates);
    }
}
