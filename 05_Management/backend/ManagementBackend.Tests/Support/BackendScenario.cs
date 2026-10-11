using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// Owns everything one test starts: its scratch directory, backend processes, stub processes, lock holders and other
/// listeners. Disposal cleans all of them up, also after a failed assertion, and only ever signals processes the test
/// started (stubs are matched by pid file and command line).
/// </summary>
internal sealed class BackendScenario : IAsyncDisposable
{
    static readonly TimeSpan BuildTimeout = TimeSpan.FromSeconds(330);

    // Long enough for a backend that ignores its configuration to start and write connection.json.
    static readonly TimeSpan RefusalTimeout = TimeSpan.FromSeconds(30);

    readonly ScratchDirectory _scratch;
    readonly PortReservation? _serverPortReservation;
    readonly List<BackendProcess> _backends = [];
    readonly List<Process> _helpers = [];
    readonly List<StubProcess> _otherStubs = [];

    BackendScenario(string purpose, PortReservation? serverPortReservation)
    {
        _scratch = new ScratchDirectory(purpose);
        _serverPortReservation = serverPortReservation;
        Home = _scratch.Create("home");
        Stub = new StubBehavior(serverPortReservation?.Port ?? FreePort.Next(), _scratch.Create("stub-pids"));
        Settings = new BackendSettings(
            Path.Combine(_scratch.Root, "data"),
            Path.Combine(_scratch.Create("locks"), "server.lock"),
            Stub);
    }

    public string Root => _scratch.Root;

    /// <summary>HOME of the backend process, so default paths never reach the real config, lock or data directory.</summary>
    public string Home { get; }

    public string ConfigPath => Path.Combine(_scratch.Root, "backend.json");

    public BackendSettings Settings { get; }

    public StubBehavior Stub { get; }

    public string DataDirectory => Settings.DataDirectory;

    /// <summary>The default dataDirectory of backend-design.md 「설정」 under this scenario's HOME.</summary>
    public string DefaultDataDirectory => Path.Combine(Home, ".local", "share", "dawnholder", "management");

    public ReleaseSourceRepository? Source { get; private set; }

    /// <summary>
    /// Fails right away when the backend is not built, so before implementation every backend test fails for that one
    /// reason and does not leave half-made fixtures behind.
    /// </summary>
    public static BackendScenario Create(string purpose)
    {
        _ = BuiltProcesses.Backend;
        return new BackendScenario(purpose, null);
    }

    /// <summary>
    /// Like <see cref="Create"/>, but the server port stays bound by the test until disposal, so no other process that
    /// asks the kernel for a free port gets it meanwhile. Only for tests that never start the stub on that port.
    /// </summary>
    public static BackendScenario CreateWithReservedServerPort(string purpose)
    {
        _ = BuiltProcesses.Backend;
        return new BackendScenario(purpose, PortReservation.Take());
    }

    /// <summary>
    /// Environment of the backend process. Build-server switches keep its dotnet publish from leaving MSBuild or
    /// compiler server processes behind after the test run; URL variables would compete with listenPort.
    /// </summary>
    public Dictionary<string, string?> BackendEnvironment() => new(StringComparer.Ordinal)
    {
        ["HOME"] = Home,
        ["MSBUILDDISABLENODEREUSE"] = "1",
        ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0",
        ["UseSharedCompilation"] = "false",
        ["ASPNETCORE_URLS"] = null,
        ["ASPNETCORE_HTTP_PORTS"] = null,
        ["ASPNETCORE_HTTPS_PORTS"] = null,
        ["DOTNET_URLS"] = null,
    };

    public async Task<BackendProcess> StartBackendAsync()
    {
        WriteConfig(Settings.ToJson());
        BackendProcess backend = await BackendProcess.StartAsync(ConfigPath, DataDirectory, Root, BackendEnvironment());
        _backends.Add(backend);
        return backend;
    }

    /// <summary>Writes <paramref name="config"/> and runs a backend that should refuse it and exit.</summary>
    public Task<ProcessResult> RunBackendToExitAsync(JsonObject config, Dictionary<string, string?> environment)
    {
        WriteConfig(config);
        return BackendProcess.RunToExitAsync(ConfigPath, Root, environment, TimeSpan.FromSeconds(60));
    }

    /// <summary>Writes <paramref name="config"/> and runs a backend that should refuse it without starting.</summary>
    public Task<RefusalRun> RunBackendExpectingRefusalAsync(JsonObject config)
    {
        WriteConfig(config);
        return RunBackendExpectingRefusalAsync(ConfigPath);
    }

    /// <summary>
    /// Runs a backend with `--config <paramref name="configPath"/>` that should refuse to start. One that starts anyway
    /// is killed as soon as it writes connection.json in the configured or in the default data directory.
    /// </summary>
    public Task<RefusalRun> RunBackendExpectingRefusalAsync(string configPath) =>
        BackendProcess.RunExpectingRefusalAsync(
            configPath,
            Root,
            BackendEnvironment(),
            [Path.Combine(DataDirectory, "connection.json"), Path.Combine(DefaultDataDirectory, "connection.json")],
            RefusalTimeout);

    public async Task<ReleaseSourceRepository> CreateReleaseSourceAsync()
    {
        Source = await ReleaseSourceRepository.CreateAsync(_scratch.Create("source"));
        Settings.SourceRepository = Source.Root;
        return Source;
    }

    /// <summary>Builds <paramref name="commit"/> through the API and makes it the current release.</summary>
    public async Task InstallReleaseAsync(BackendProcess backend, string commit)
    {
        await BuildReleaseAsync(backend, commit);
        ApiAssert.Success(await backend.PutAsync("/api/releases/current", new { commit }), $"PUT /api/releases/current {commit}");
    }

    public async Task BuildReleaseAsync(BackendProcess backend, string commit) =>
        ApiAssert.Success(await PostReleaseAsync(backend, commit), $"POST /api/releases {commit}");

    public Task<ApiResult> PostReleaseAsync(BackendProcess backend, string commit) =>
        backend.PostAsync("/api/releases", new { commit }, BuildTimeout);

    /// <summary>Creates the release source, starts the backend and installs the first commit as the current release.</summary>
    public async Task<BackendProcess> StartWithReleaseAsync()
    {
        ReleaseSourceRepository source = await CreateReleaseSourceAsync();
        BackendProcess backend = await StartBackendAsync();
        await InstallReleaseAsync(backend, source.FirstCommit);
        return backend;
    }

    /// <summary>Starts the game server and returns the status that shows it running.</summary>
    public static async Task<JsonNode> StartServerAsync(BackendProcess backend)
    {
        ApiResult start = await backend.PostAsync("/api/server/start");
        ApiAssert.Success(start, "POST /api/server/start");
        JsonNode status = await backend.StatusAsync();
        Assert.True(JsonFields.Text(status, "server.state") == "running", $"after start: {JsonFields.Show(status)}");
        return status;
    }

    /// <summary>Asks for a normal stop and waits until the backend reports the server stopped.</summary>
    public static async Task<JsonNode> StopServerAsync(BackendProcess backend)
    {
        ApiAssert.Success(await backend.PostAsync("/api/server/stop"), "POST /api/server/stop");
        return await backend.WaitForStatusAsync(
            status => JsonFields.Text(status, "server.state") == "stopped",
            TimeSpan.FromSeconds(20),
            "server.state stopped");
    }

    /// <summary>Another process takes the server port lock and keeps it until the scenario ends.</summary>
    public async Task<Process> HoldPortLockAsync()
    {
        Process holder = await PortLock.HoldAsync(Settings.PortLockFile);
        _helpers.Add(holder);
        return holder;
    }

    /// <summary>A GameServerStub started by the test itself listens on the server port, as another runtime would.</summary>
    public async Task<StubProcess> StartOtherListenerAsync()
    {
        StubBehavior other = new(Stub.Port, _scratch.Create("other-listener-pids"));
        StubProcess listener = StubProcess.Start(BuiltProcesses.Stub, other);
        _otherStubs.Add(listener);
        await listener.WaitForLineAsync($"stdout: listening on 127.0.0.1:{Stub.Port}", TimeSpan.FromSeconds(30));
        return listener;
    }

    public async ValueTask DisposeAsync()
    {
        for (int index = _backends.Count - 1; index >= 0; index--)
        {
            await _backends[index].DisposeAsync();
        }

        foreach (int pid in Stub.StartedPids())
        {
            if (LinuxProcess.IsAlive(pid) && LinuxProcess.CommandLine(pid)?.Contains(BuiltProcesses.StubAssembly, StringComparison.Ordinal) == true)
            {
                LinuxProcess.Signal(pid, LinuxProcess.SigKill);
                await LinuxProcess.WaitForExitAsync(pid, TimeSpan.FromSeconds(10));
            }
        }

        foreach (StubProcess listener in _otherStubs)
        {
            listener.Dispose();
        }

        foreach (Process helper in _helpers)
        {
            if (!helper.HasExited)
            {
                helper.Kill(entireProcessTree: true);
                await helper.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }

            helper.Dispose();
        }

        _serverPortReservation?.Dispose();
        _scratch.Dispose();
    }

    void WriteConfig(JsonObject config) => File.WriteAllText(ConfigPath, config.ToJsonString());
}
