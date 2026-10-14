using Dawnholder.Server.GameServer.Accounts;

namespace Dawnholder.Server.GameServer.Tests.Accounts;

// Goal T2 (completion condition 5). Expected IDs come from an independent RFC 9562 v5 implementation, run once
// before the resolver existed: wsl -d Ubuntu -- python3 -c 'import uuid; print(uuid.uuid5(
// uuid.UUID("d05083ec-6067-4d21-9ce9-271da508bbfd"), "<name>"))' (Python 3.14.4, raw output kept with the goal's
// evidence). A literal cannot change when the server restarts, so equality also covers "same ID after restart".
public sealed class AccountIdResolverTests
{
    [Theory]
    [InlineData("abc", "137b1c41-ed4a-5aaf-8252-731482bcabe4")]              // 3 characters
    [InlineData("abcdefghijklmnop", "71115165-620a-5a03-98d3-eacdd8a61f30")] // 16 characters
    [InlineData("user_123", "18be3028-ade2-5ad0-9ffa-0fd51a36f461")]         // underscore and digits
    [InlineData("alice_01", "d0064115-982b-56a6-ae70-528b3cde4a09")]
    public void T2_Resolve_MatchesTheIndependentV5Value(string normalizedName, string expected)
    {
        Guid id = AccountIdResolver.Resolve(normalizedName);

        Assert.Equal(expected, id.ToString("D"));
    }

    [Fact]
    public void T2_SameName_AlwaysResolvesToTheSameNonEmptyId()
    {
        Guid first = AccountIdResolver.Resolve("alice_01");
        Guid second = AccountIdResolver.Resolve("alice_01");

        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void T2_DifferentNames_ResolveToDifferentIds()
    {
        string[] names = { "abc", "abcdefghijklmnop", "user_123", "alice_01", "bob_02" };

        Guid[] ids = names.Select(AccountIdResolver.Resolve).ToArray();

        Assert.Equal(names.Length, ids.Distinct().Count());
    }
}
