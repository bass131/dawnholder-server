using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Accounts;

// Goal T1 (completion condition 6, confirmed default 1): 3..16 characters of a-z, 0-9 and '_'. ASCII A-Z is
// lowered to the same account, nothing else is mapped, and spaces are never trimmed. Every row is a concrete
// input with its expected output; no rule code is repeated here.
public sealed class AccountNameRuleTests
{
    [Fact]
    public void T1_LengthBounds_AreThreeAndSixteen()
    {
        Assert.Equal(3, AccountNameRule.MinLength);
        Assert.Equal(16, AccountNameRule.MaxLength);
    }

    [Theory]
    [InlineData("abc", "abc")]                           // 3 characters, the lower bound
    [InlineData("abcdefghijklmnop", "abcdefghijklmnop")] // 16 characters, the upper bound
    [InlineData("user_123", "user_123")]
    [InlineData("007", "007")]
    [InlineData("_9_", "_9_")]
    [InlineData("ABC", "abc")]
    [InlineData("Alice_01", "alice_01")]
    [InlineData("ABCDEFGHIJKLMNOP", "abcdefghijklmnop")]
    public void T1_ValidName_PassesInLowercase(string raw, string expected)
    {
        bool accepted = AccountNameRule.TryNormalize(raw, out string normalized);

        Assert.True(accepted, $"'{raw}' was rejected");
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]                // 2 characters
    [InlineData("abcdefghijklmnopq")] // 17 characters
    [InlineData("ABCDEFGHIJKLMNOPQ")] // 17 characters after lowering
    [InlineData(" abc")]              // spaces are not trimmed
    [InlineData("abc ")]
    [InlineData("ab cd")]
    [InlineData("한글이름")]
    [InlineData("abc한")]
    [InlineData("with-hyphen")]
    [InlineData("dot.name")]
    [InlineData("ｆｕｌｌ")]             // full-width letters
    public void T1_NameOutsideTheRule_IsRejected(string? raw)
    {
        bool accepted = AccountNameRule.TryNormalize(raw, out _);

        Assert.False(accepted, $"'{raw}' was accepted");
    }

    // Control characters are built from code points so test names stay printable.
    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x1F)]
    [InlineData(0x7F)]
    public void T1_NameWithAControlCharacter_IsRejected(int codePoint)
    {
        string raw = "abc" + (char)codePoint + "def";

        bool accepted = AccountNameRule.TryNormalize(raw, out _);

        Assert.False(accepted, $"control character U+{codePoint:X4} was accepted");
    }

    // Only ASCII A-Z is lowered. KELVIN SIGN and capital I with dot lower to ASCII letters under invariant
    // casing, so a rule that lowered everything first would let them through as "kelvin" or "istanbul".
    [Theory]
    [InlineData(0x212A, "elvin")]
    [InlineData(0x0130, "stanbul")]
    public void T1_NonAsciiUppercase_IsNotFoldedIntoAnAsciiName(int codePoint, string rest)
    {
        string raw = (char)codePoint + rest;

        bool accepted = AccountNameRule.TryNormalize(raw, out _);

        Assert.False(accepted, $"U+{codePoint:X4}{rest} was accepted");
    }
}
