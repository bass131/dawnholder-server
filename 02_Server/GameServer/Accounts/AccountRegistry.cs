using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Accounts;

// Socket handlers enqueue validated names; the World tick alone registers accounts and removes
// them during close cleanup. Keeping both mutations on that thread serializes duplicate logins
// with disconnects; Forget removes only the exact session that owns a registration.
internal sealed class AccountRegistry
{
    readonly ConcurrentQueue<Action> _pendingJobs = new();
    readonly Dictionary<string, GameSession> _online = new(StringComparer.Ordinal);

    internal int OnlineCount => _online.Count;

    internal bool TryGetOnlineSession(string normalizedName, out GameSession? session)
        => _online.TryGetValue(normalizedName, out session);

    internal void EnqueueLogin(GameSession session, string normalizedName)
        => _pendingJobs.Enqueue(() => Login(session, normalizedName));

    internal void Tick()
    {
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try
            {
                job();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountRegistry] job failed: {ex}");
            }
        }
    }

    internal void Forget(GameSession session)
    {
        if (session.Account is AccountIdentity account &&
            _online.TryGetValue(account.Name, out GameSession? owner) && ReferenceEquals(owner, session))
        {
            _online.Remove(account.Name);
        }
    }

    void Login(GameSession session, string normalizedName)
    {
        if (session.IsClosing) return;
        if (_online.ContainsKey(normalizedName))
        {
            Console.WriteLine($"[Login] rejected account={normalizedName} result=AlreadyOnline");
            session.RejectLogin(LoginResult.AlreadyOnline);
            return;
        }

        AccountIdentity account = new(normalizedName, AccountIdResolver.Resolve(normalizedName));
        _online.Add(normalizedName, session);
        session.CompleteLogin(account);
        Console.WriteLine($"[Login] accepted account={normalizedName}");
    }
}
