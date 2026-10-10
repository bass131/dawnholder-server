using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// contains limits of backend-design.md 「로그 조회」 (0~5 words, each 1~64 characters) counted in characters, so a
/// Korean word of 64 characters (192 UTF-8 bytes) is accepted and a single bad word refuses the whole query.
/// </summary>
public sealed class LogQueryWordLimitTests
{
    static readonly TimeSpan LogWait = TimeSpan.FromSeconds(20);

    [LinuxFact]
    public async Task KoreanWordsAreCountedInCharactersAndOneBadWordRefusesTheQuery()
    {
        await using BackendScenario scenario = BackendScenario.Create("contains-korean");
        scenario.Stub.WriteLine("stdout", "서버 경고: 저장 지연");
        scenario.Stub.WriteLine("stdout", "평범한 줄");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        await BackendScenario.StartServerAsync(backend);
        await LogQuery.WaitForTextAsync(backend, "limit=100", "평범한 줄", LogWait);
        string word64 = new('가', 64);
        string word65 = new('가', 65);

        ApiResult longest = await backend.GetAsync($"/api/logs?contains={Escape(word64)}");
        ApiResult tooLong = await backend.GetAsync($"/api/logs?contains={Escape(word65)}");
        ApiResult fiveWithOneTooLong = await backend.GetAsync(
            "/api/logs?" + string.Join('&', new[] { "a", "b", "c", "d", word65 }.Select(word => $"contains={Escape(word)}")));
        ApiResult oneEmpty = await backend.GetAsync("/api/logs?contains=a&contains=");
        JsonNode warning = await LogQuery.GetAsync(backend, $"contains={Escape("경고")}&contains={Escape("없는낱말")}");

        Assert.True(longest.Code == 200, $"a 64-character Korean word is inside 1~64 characters: {longest.Describe()}");
        ApiAssert.Error(tooLong, 400, "invalidQuery");
        ApiAssert.Error(fiveWithOneTooLong, 400, "invalidQuery");
        ApiAssert.Error(oneEmpty, 400, "invalidQuery");
        IReadOnlyList<string> texts = LogQuery.Texts(warning);
        Assert.Contains("서버 경고: 저장 지연", texts);
        Assert.DoesNotContain("평범한 줄", texts);
    }

    static string Escape(string word) => Uri.EscapeDataString(word);
}
