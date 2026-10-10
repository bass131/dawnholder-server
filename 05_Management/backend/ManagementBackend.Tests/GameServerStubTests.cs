using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// Checks the test tools themselves, without the backend: each stub mode and the temporary release source. The
/// backend tests cannot be edited by the implementer, so these must pass before the backend exists.
/// </summary>
public sealed class GameServerStubTests
{
    static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

    [LinuxFact]
    public async Task NormalMode_ListensWritesScriptedLinesAndExitsZeroOnEnter()
    {
        using ScratchDirectory scratch = new("stub-normal");
        StubBehavior behavior = new StubBehavior(FreePort.Next(), scratch.Create("pids"))
            .WriteLine("stdout", "scripted out")
            .WriteLine("stderr", "scripted err")
            .WriteLine("stdout", "counted", count: 2);
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        await stub.WaitForLineAsync($"stdout: listening on 127.0.0.1:{behavior.Port}", Wait);
        await stub.WaitForLineAsync($"stdout: {StubBehavior.CountedText("counted", 2)}", Wait);
        IReadOnlyList<ListeningSocket> sockets = LinuxProcess.ListeningSocketsOf(stub.Pid);
        stub.SendEnter();
        bool exited = await stub.WaitForExitAsync(Wait);

        Assert.Contains(sockets, socket => socket.Port == behavior.Port);
        Assert.Contains("stdout: scripted out", stub.Lines);
        Assert.Contains("stderr: scripted err", stub.Lines);
        Assert.Contains($"stdout: {StubBehavior.CountedText("counted", 1)}", stub.Lines);
        Assert.Equal([stub.Pid], behavior.StartedPids());
        Assert.True(exited, "stub did not exit after Enter");
        Assert.Equal(0, stub.ExitCode);
    }

    [LinuxFact]
    public async Task NormalMode_ExitsZeroWhenStandardInputCloses()
    {
        using ScratchDirectory scratch = new("stub-eof");
        StubBehavior behavior = new(FreePort.Next(), scratch.Create("pids"));
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        await stub.WaitForLineAsync($"stdout: listening on 127.0.0.1:{behavior.Port}", Wait);
        stub.CloseInput();
        bool exited = await stub.WaitForExitAsync(Wait);

        Assert.True(exited, "stub did not exit when standard input closed");
        Assert.Equal(0, stub.ExitCode);
    }

    [LinuxFact]
    public async Task NormalMode_ExitDelayKeepsProcessAliveAfterEnter()
    {
        using ScratchDirectory scratch = new("stub-exit-delay");
        StubBehavior behavior = new(FreePort.Next(), scratch.Create("pids")) { ExitDelayMilliseconds = 1500 };
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        await stub.WaitForLineAsync($"stdout: listening on 127.0.0.1:{behavior.Port}", Wait);
        stub.SendEnter();
        await stub.WaitForLineAsync("stdout: stop requested", Wait);
        bool aliveDuringDelay = !stub.HasExited;
        bool exited = await stub.WaitForExitAsync(Wait);

        Assert.True(aliveDuringDelay, "stub exited before its exit delay");
        Assert.True(exited, "stub did not exit after its exit delay");
        Assert.Equal(0, stub.ExitCode);
    }

    [LinuxFact]
    public async Task IgnoreEnterMode_KeepsRunningAfterEnterAndClosedInput()
    {
        using ScratchDirectory scratch = new("stub-ignore");
        StubBehavior behavior = new(FreePort.Next(), scratch.Create("pids")) { Mode = StubBehavior.IgnoreEnter };
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        await stub.WaitForLineAsync($"stdout: listening on 127.0.0.1:{behavior.Port}", Wait);
        stub.SendEnter();
        await stub.WaitForLineAsync("stdout: ignoring stop request", Wait);
        stub.CloseInput();
        bool exited = await stub.WaitForExitAsync(TimeSpan.FromSeconds(2));

        Assert.False(exited, "ignore-enter stub exited on Enter or closed input");
    }

    [LinuxFact]
    public async Task CrashMode_ExitsWithConfiguredCodeAfterListening()
    {
        using ScratchDirectory scratch = new("stub-crash");
        StubBehavior behavior = new(FreePort.Next(), scratch.Create("pids"))
        {
            Mode = StubBehavior.Crash,
            ExitCode = 5,
            CrashAfterMilliseconds = 500,
        };
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        await stub.WaitForLineAsync($"stdout: listening on 127.0.0.1:{behavior.Port}", Wait);
        bool exited = await stub.WaitForExitAsync(Wait);

        Assert.True(exited, "crash stub did not exit");
        Assert.Equal(5, stub.ExitCode);
    }

    [LinuxFact]
    public async Task ExitBeforeListenMode_ExitsWithConfiguredCodeWithoutListening()
    {
        using ScratchDirectory scratch = new("stub-early-exit");
        StubBehavior behavior = new(FreePort.Next(), scratch.Create("pids")) { Mode = StubBehavior.ExitBeforeListen, ExitCode = 4 };
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        bool exited = await stub.WaitForExitAsync(Wait);

        Assert.True(exited, "exit-before-listen stub did not exit");
        Assert.Equal(4, stub.ExitCode);
        Assert.DoesNotContain(stub.Lines, line => line.Contains("listening on", StringComparison.Ordinal));
    }

    [LinuxFact]
    public async Task NoListenMode_KeepsRunningWithoutListeningOrReadingInput()
    {
        using ScratchDirectory scratch = new("stub-no-listen");
        StubBehavior behavior = new(FreePort.Next(), scratch.Create("pids")) { Mode = StubBehavior.NoListen };
        using StubProcess stub = StubProcess.Start(BuiltProcesses.Stub, behavior);

        await stub.WaitForLineAsync($"stdout: GameServerStub mode=NoListen pid={stub.Pid}", Wait);
        stub.SendEnter();
        bool exited = await stub.WaitForExitAsync(TimeSpan.FromSeconds(2));
        IReadOnlyList<ListeningSocket> sockets = LinuxProcess.ListeningSocketsOf(stub.Pid);

        Assert.False(exited, "no-listen stub exited");
        Assert.Empty(sockets);
    }

    /// <summary>
    /// The release settings given to the backend (BackendSettings) must describe a buildable project: archive the
    /// commit, publish it with the pinned SDK, and find the entry assembly and marker. A broken commit must not build.
    /// </summary>
    [LinuxFact]
    public async Task ReleaseSource_ArchivedCommitPublishesAndBrokenCommitFails()
    {
        using ScratchDirectory scratch = new("stub-release-source");
        ReleaseSourceRepository source = await ReleaseSourceRepository.CreateAsync(scratch.Create("source"));
        string broken = await source.CommitBrokenBuildAsync();
        BackendSettings settings = new(scratch.Create("data"), Path.Combine(scratch.Root, "lock"), new StubBehavior(1, scratch.Root));

        ProcessResult good = await PublishAsync(scratch.Create("good"), source, source.FirstCommit, settings);
        ProcessResult bad = await PublishAsync(scratch.Create("bad"), source, broken, settings);
        ProcessResult sdk = await ProcessRunner.RunAsync(
            BuiltProcesses.Dotnet, ["--version"], Path.Combine(scratch.Root, "good"), TimeSpan.FromSeconds(60));

        Assert.True(good.ExitCode == 0, good.Describe());
        Assert.True(File.Exists(Path.Combine(scratch.Root, "good", "out", settings.EntryAssembly)), "entry assembly missing");
        Assert.Equal(
            ReleaseSourceRepository.FirstMarker,
            File.ReadAllText(Path.Combine(scratch.Root, "good", "out", "release-marker.txt")).Trim());
        Assert.True(bad.ExitCode != 0, bad.Describe());
        Assert.Equal("10.0.301", sdk.StandardOutput.Trim());
    }

    static async Task<ProcessResult> PublishAsync(string work, ReleaseSourceRepository source, string commit, BackendSettings settings)
    {
        string archive = Path.Combine(work, "source.tar");
        ProcessResult archived = await ProcessRunner.RunAsync(
            "git",
            ["archive", "--format=tar", "-o", archive, commit, "--", .. settings.ArchivePaths],
            source.Root,
            TimeSpan.FromSeconds(30),
            new Dictionary<string, string?> { ["GIT_CONFIG_NOSYSTEM"] = "1", ["GIT_CONFIG_GLOBAL"] = "/dev/null" });
        Assert.True(archived.ExitCode == 0, archived.Describe());
        ProcessResult extracted = await ProcessRunner.RunAsync("tar", ["-xf", archive], work, TimeSpan.FromSeconds(30));
        Assert.True(extracted.ExitCode == 0, extracted.Describe());
        return await ProcessRunner.RunAsync(
            BuiltProcesses.Dotnet,
            ["publish", settings.ProjectPath, "-c", "Release", "-o", Path.Combine(work, "out"), "--disable-build-servers", "--nologo"],
            work,
            TimeSpan.FromSeconds(300));
    }
}
