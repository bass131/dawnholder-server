using Dawnholder.Server.GameServer.Accounts;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>동결한 저장 계정 설정을 기존 로그인 identity 규칙과 대조한다.</summary>
public sealed class PersistenceAccountSelector
{
    public PersistenceAccountSelector(string configuredName, Guid accountId)
    {
        if (!AccountNameRule.TryNormalize(configuredName, out string normalized))
            throw new ArgumentException("The persisted account name is invalid.", nameof(configuredName));
        if (accountId == Guid.Empty || AccountIdResolver.Resolve(normalized) != accountId)
            throw new ArgumentException("The persisted account identity does not match its name.", nameof(accountId));
        NormalizedAccountName = normalized;
        AccountId = accountId;
    }

    public string NormalizedAccountName { get; }
    public Guid AccountId { get; }
    public bool IsPersistent(Guid accountId) => accountId == AccountId;
}
