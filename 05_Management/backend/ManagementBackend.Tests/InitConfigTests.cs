using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;
using Xunit.Abstractions;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// I: `backend-wsl.sh init-config` writes the default configuration with only release.sourceRepository, the work tree
/// that owns the Git objects of the script's checkout (screen-design.md 「백엔드 조정」 B-2). Expected paths are literals.
/// </summary>
public sealed class InitConfigTests
{
    // Building the backend takes far longer; writing one small file does not.
    static readonly TimeSpan NoBuildBound = TimeSpan.FromSeconds(10);

    readonly ITestOutputHelper _output;

    public InitConfigTests(ITestOutputHelper output) => _output = output;

    [LinuxFact(Timeout = 60_000)]
    [SupportedOSPlatform("linux")]
    public async Task I1_WorktreeGitFileWritesTheOriginalCloneAsTheOnlyKey()
    {
        using FakeCheckout checkout = FakeCheckout.Create("i1");
        checkout.WriteGitFile(FakeCheckout.WorktreeGitFile);

        ProcessResult result = await checkout.InitConfigAsync();
        bool written = File.Exists(checkout.ConfigFile);

        Assert.True(result.ExitCode == 0, $"init-config must succeed in a worktree checkout: {result.Describe()}");
        Assert.True(written, $"init-config must write {checkout.ConfigFile}: {result.Describe()}");
        AssertOnlySourceRepository(checkout.ConfigFile, "/mnt/c/Dev/Fake");
        UnixFileMode mode = File.GetUnixFileMode(checkout.ConfigFile);
        Assert.True(mode == (UnixFileMode.UserRead | UnixFileMode.UserWrite), $"{checkout.ConfigFile} must be 0600, is {mode}");
        Assert.True(
            result.Combined.Contains(checkout.ConfigFile, StringComparison.Ordinal),
            $"the output must name {checkout.ConfigFile}: {result.Describe()}");
    }

    [LinuxFact(Timeout = 60_000)]
    public async Task I2_GitDirectoryWritesTheCheckoutItself()
    {
        using FakeCheckout checkout = FakeCheckout.Create("i2");
        await checkout.InitGitRepositoryAsync();

        ProcessResult result = await checkout.InitConfigAsync();
        bool written = File.Exists(checkout.ConfigFile);

        Assert.True(result.ExitCode == 0, $"init-config must succeed in an ordinary clone: {result.Describe()}");
        Assert.True(written, $"init-config must write {checkout.ConfigFile}: {result.Describe()}");
        AssertOnlySourceRepository(checkout.ConfigFile, checkout.Root);
    }

    [LinuxFact(Timeout = 60_000)]
    public async Task I3_ExistingConfigurationKeepsItsContentAndTime()
    {
        using FakeCheckout checkout = FakeCheckout.Create("i3");
        checkout.WriteGitFile(FakeCheckout.WorktreeGitFile);
        Directory.CreateDirectory(Path.GetDirectoryName(checkout.ConfigFile)!);
        byte[] existing = "{ \"listenPort\": 47999, \"release\": { \"sourceRepository\": \"/mnt/c/Existing/Clone\" } }\n"u8.ToArray();
        DateTime existingTime = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.WriteAllBytes(checkout.ConfigFile, existing);
        File.SetLastWriteTimeUtc(checkout.ConfigFile, existingTime);

        ProcessResult result = await checkout.InitConfigAsync();
        byte[] after = File.ReadAllBytes(checkout.ConfigFile);
        DateTime afterTime = File.GetLastWriteTimeUtc(checkout.ConfigFile);

        Assert.True(result.ExitCode == 0, $"init-config must succeed when the file already exists: {result.Describe()}");
        Assert.True(existing.AsSpan().SequenceEqual(after), $"the existing configuration must not change; it now reads {Encoding.UTF8.GetString(after)}");
        Assert.True(afterTime == existingTime, $"the existing configuration's time must not change: {existingTime:O} became {afterTime:O}");
    }

    /// <summary>
    /// The refusal alone cannot tell "the .git gives no source" from "init-config is an unknown command", so the same
    /// copy then runs once more with a worktree .git file and has to succeed.
    /// </summary>
    [LinuxTheory(Timeout = 90_000)]
    [InlineData("noGit")]
    [InlineData("submoduleGitFile")]
    [InlineData("separateGitDirectoryFile")]
    public async Task I4_UndeterminableSourceIsRefusedWithAReasonAndNoFile(string gitState)
    {
        using FakeCheckout checkout = FakeCheckout.Create($"i4-{gitState}");
        switch (gitState)
        {
            case "noGit":
                checkout.RemoveGit();
                break;
            case "submoduleGitFile":
                checkout.WriteGitFile("gitdir: C:/Dev/Fake/.git/modules/sub\n");
                break;
            case "separateGitDirectoryFile":
                checkout.WriteGitFile("gitdir: C:/Dev/Fake/.git\n");
                break;
            default:
                Assert.Fail($"unknown git state {gitState}");
                break;
        }

        ProcessResult refused = await checkout.InitConfigAsync();
        bool writtenByRefusal = File.Exists(checkout.ConfigFile);
        checkout.WriteGitFile(FakeCheckout.WorktreeGitFile);
        ProcessResult control = await checkout.InitConfigAsync();

        Assert.True(refused.ExitCode != 0, $"init-config must fail when the source cannot be determined ({gitState}): {refused.Describe()}");
        Assert.False(string.IsNullOrWhiteSpace(refused.Combined), $"the refusal must print its reason ({gitState}): {refused.Describe()}");
        Assert.False(writtenByRefusal, $"a refused init-config must not write {checkout.ConfigFile} ({gitState})");
        Assert.True(
            control.ExitCode == 0 && File.Exists(checkout.ConfigFile),
            $"control: the same init-config with a worktree .git must succeed, or the refusal above says nothing about {gitState}: {control.Describe()}");
    }

    [LinuxFact(Timeout = 60_000)]
    public async Task I5_InitConfigNeitherCopiesNorBuilds()
    {
        using FakeCheckout checkout = FakeCheckout.Create("i5");
        checkout.WriteGitFile(FakeCheckout.WorktreeGitFile);

        Stopwatch watch = Stopwatch.StartNew();
        ProcessResult result = await checkout.InitConfigAsync();
        watch.Stop();
        bool written = File.Exists(checkout.ConfigFile);
        bool cacheCreated = Directory.Exists(checkout.BackendCache);
        _output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"I5 init-config exit={result.ExitCode} elapsed_seconds={watch.Elapsed.TotalSeconds:F3} config_written={written} backend_cache_exists={cacheCreated}"));

        Assert.True(result.ExitCode == 0, $"init-config must succeed in a worktree checkout: {result.Describe()}");
        Assert.True(written, $"init-config must write {checkout.ConfigFile}: {result.Describe()}");
        Assert.False(cacheCreated, $"init-config must not create the WSL copy or its lock under {checkout.BackendCache}");
        Assert.True(
            watch.Elapsed < NoBuildBound,
            $"init-config took {watch.Elapsed.TotalSeconds:F1} s; without a copy or build it ends well within {NoBuildBound.TotalSeconds} s");
    }

    /// <summary>The file is exactly { "release": { "sourceRepository": <paramref name="expected"/> } }.</summary>
    static void AssertOnlySourceRepository(string configFile, string expected)
    {
        string text = File.ReadAllText(configFile);
        JsonNode? root = JsonNode.Parse(text);
        Assert.True(root is JsonObject, $"{configFile} must hold a JSON object: {text}");
        string[] rootKeys = [.. ((JsonObject)root!).Select(property => property.Key)];
        Assert.True(rootKeys.SequenceEqual(["release"]), $"the only top-level key must be release, found [{string.Join(", ", rootKeys)}] in {text}");
        JsonNode? release = root["release"];
        Assert.True(release is JsonObject, $"release must be an object: {text}");
        string[] releaseKeys = [.. ((JsonObject)release!).Select(property => property.Key)];
        Assert.True(
            releaseKeys.SequenceEqual(["sourceRepository"]),
            $"the only release key must be sourceRepository, found [{string.Join(", ", releaseKeys)}] in {text}");
        Assert.Equal(expected, JsonFields.Text(root, "release.sourceRepository"));
    }
}
