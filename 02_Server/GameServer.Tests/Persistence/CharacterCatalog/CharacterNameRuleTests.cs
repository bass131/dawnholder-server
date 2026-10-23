using Dawnholder.Server.GameServer.Persistence.Catalog;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// API v1 「값과 한도」 Name and 「시험 수용 기준」 (1/2/12/13 characters, decomposed Hangul, surrogates, over 36, spaces and
/// full-width forms): the raw UTF-16 name has at most 36 code units and no malformed surrogate; after NFC it has 2 to 12
/// characters, each in U+AC00..U+D7A3, A..Z, a..z or 0..9; nothing is trimmed. Vectors come from that text and from
/// Unicode canonical composition (a Hangul L+V(+T) jamo run composes to one syllable), not from a product normalizer.
/// Offline observation: a rejected name ends as InvalidInput before any storage call (API v1 「오류 의미와 우선순위」
/// 검사 순서: Core 값 검증 precedes DB work); an accepted name is not InvalidInput and reaches storage.
/// </summary>
public sealed class CharacterNameRuleTests
{
    // 각 = U+1100 U+1161 U+11A8 in decomposed form: three code units that compose to one syllable.
    const string DecomposedGak = "각";
    // 가 = U+1100 U+1161: two code units, one syllable.
    const string DecomposedGa = "가";

    public static TheoryData<string, string> AcceptedNames => new()
    {
        { "가나", "two Hangul syllables (lower bound)" },
        { "ab", "two ASCII letters (lower bound)" },
        { "가나다라마바사아자차카타", "twelve Hangul syllables (upper bound)" },
        { "abcdefghijkl", "twelve ASCII letters (upper bound)" },
        { "Dawn01", "mixed case letters and digits" },
        { "가a1", "Hangul, ASCII letter and digit together" },
        { "12", "digits only are inside the allowed set" },
        { "가힣", "first and last precomposed Hangul syllables U+AC00 and U+D7A3" },
        { string.Concat(Enumerable.Repeat(DecomposedGak, 12)), "36 decomposed code units that compose to 12 syllables" },
        { DecomposedGa + "나", "decomposed 가나 composes to two syllables" },
        // Derived from the text: NFC maps KELVIN SIGN U+212A to K, so the composed name is "Kab".
        { "Kab", "a canonical singleton that NFC maps into A..Z" },
    };

    public static TheoryData<string?, string> RejectedNames => new()
    {
        { null, "missing name" },
        { "", "empty name" },
        { "가", "one Hangul syllable" },
        { "a", "one ASCII letter" },
        { "가나다라마바사아자차카타파", "thirteen Hangul syllables" },
        { "abcdefghijklm", "thirteen ASCII letters" },
        { string.Concat(Enumerable.Repeat(DecomposedGa, 13)), "26 code units but 13 syllables after NFC" },
        { string.Concat(Enumerable.Repeat(DecomposedGak, 12)) + "a", "37 raw code units" },
        { new string('a', 1000), "far over the raw limit" },
        { " 가나", "leading space is not trimmed" },
        { "가나 ", "trailing space is not trimmed" },
        { "가 나", "inner space" },
        { "가\t나", "tab" },
        { "가 나", "no-break space" },
        { "가나　", "ideographic space" },
        { "ＡＢ", "full-width Latin letters stay full-width under NFC" },
        { "ㄱㄴ", "compatibility jamo are outside U+AC00..U+D7A3" },
        { "ᄀᄂ", "leading consonant jamo without a vowel do not compose" },
        { "ÅÅ", "ANGSTROM SIGN composes to U+00C5, which is not ASCII" },
        { "é가나", "non-ASCII Latin letter" },
        { "金銀", "Han characters" },
        { "가_나", "underscore is an account-name character, not a character-name character" },
        { "가-나", "hyphen" },
        { "가@나", "symbol" },
        { "\uD800가나", "lone high surrogate" },
        { "가나\uDC00", "lone low surrogate" },
        { "가\uDE00\uD83D나", "reversed surrogate pair" },
        { "가나\U0001F600", "well-formed emoji outside the allowed set" },
    };

    // Lone surrogates cannot cross the runner's discovery serialization intact, so rows are enumerated at run time.
    [Theory]
    [MemberData(nameof(RejectedNames), DisableDiscoveryEnumeration = true)]
    public async Task RejectedName_IsInvalidInputBeforeAnyStorageCall(string? name, string reason)
    {
        using OfflineCatalog offline = new();
        CatalogCall call = offline.ValidCall() with { Name = name };

        CatalogOutcome outcome = await offline.Catalog.CreateAsync(call);
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.True(outcome.Status == "InvalidInput", $"{reason}: expected InvalidInput, got {outcome.Status}");
        Assert.True(outcome.InvalidField is null or "Name", $"{reason}: InvalidField is {outcome.InvalidField}");
        Assert.True(storageCalls.Count == 0, $"{reason}: storage was called: {string.Join(", ", storageCalls)}");
    }

    [Theory]
    [MemberData(nameof(AcceptedNames))]
    public async Task AcceptedName_IsNotRejectedAndReachesStorage(string name, string reason)
    {
        using OfflineCatalog offline = new();
        CatalogCall call = offline.ValidCall() with { Name = name };

        Task<CatalogResult> created = (Task<CatalogResult>)offline.Catalog.Invoke(CharacterCatalogApi.CreateOperation, call)!;
        // Times out only when the create neither reached storage nor finished.
        await Task.WhenAny(offline.Store.FirstCall, created).WaitAsync(StoreCallRecorder.Limit);
        bool storageReached = offline.Store.FirstCall.IsCompleted;
        string? earlyStatus = created.IsCompletedSuccessfully
            ? (await created).Status.ToString()
            : null;

        Assert.True(earlyStatus != "InvalidInput", $"{reason}: an allowed name was rejected as InvalidInput");
        Assert.True(storageReached, $"{reason}: the create ended as {earlyStatus ?? created.Status.ToString()} without storage");
    }
}
