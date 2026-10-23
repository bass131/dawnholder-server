using System.Text;
using Dawnholder.Server.GameServer.Accounts;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Catalog;

/// <summary>Only Core preparation can mint this canonical intent. Correlation and budgets are deliberately absent.</summary>
public sealed class CreateDescriptor : IEquatable<CreateDescriptor>
{
    internal CreateDescriptor(AccountContext account, byte slot, string name, string nameKey, CharacterClass characterClass)
    {
        Account = account;
        Slot = slot;
        Name = name;
        NameKey = nameKey;
        Class = characterClass;
    }

    public AccountContext Account { get; }
    public byte Slot { get; }
    public string Name { get; }
    public string NameKey { get; }
    public CharacterClass Class { get; }
    public int PayloadVersion => 1;

    public bool Equals(CreateDescriptor? other) => other is not null && Account == other.Account &&
        Slot == other.Slot && Name == other.Name && NameKey == other.NameKey && Class == other.Class;
    public override bool Equals(object? obj) => obj is CreateDescriptor other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Account, Slot, Name, NameKey, Class);
}

internal static class CatalogValidation
{
    internal static bool Account(AccountContext? account) => account is not null &&
        AccountNameRule.TryNormalize(account.NormalizedAccountName, out string normalized) &&
        normalized == account.NormalizedAccountName && account.AccountId != Guid.Empty &&
        AccountIdResolver.Resolve(normalized) == account.AccountId;

    internal static bool Request(RequestContext? request) => request is not null &&
        request.SessionInstanceId != Guid.Empty && request.Generation >= 0;

    internal static bool Name(string? raw, out string name, out string key)
    {
        name = key = string.Empty;
        if (raw is null || raw.Length is < 2 or > 36) return false;
        try { name = raw.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { return false; }
        if (name.Length is < 2 or > 12 || name.Any(c =>
            !(c is >= '가' and <= '힣' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')))
            return false;
        key = string.Create(name.Length, name, (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
                span[i] = source[i] is >= 'a' and <= 'z' ? (char)(source[i] - ('a' - 'A')) : source[i];
        });
        return true;
    }

    internal static bool Summary(CharacterSummary value) => value.CharacterId != Guid.Empty &&
        value.Slot is >= 1 and <= 3 && value.Class is CharacterClass.Knight or CharacterClass.Mage &&
        Name(value.Name, out string normalized, out _) && normalized == value.Name;
}
