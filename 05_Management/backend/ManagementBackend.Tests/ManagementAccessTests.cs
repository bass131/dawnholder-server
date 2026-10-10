using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>A: same-PC address, per-start secret and request limits (backend-design.md 「관리 접근 경계」).</summary>
public sealed class ManagementAccessTests
{
    [LinuxFact]
    public async Task A1_ListensOnlyOn127001()
    {
        await using BackendScenario scenario = BackendScenario.Create("a1");
        BackendProcess backend = await scenario.StartBackendAsync();

        IReadOnlyList<ListeningSocket> sockets = LinuxProcess.ListeningSocketsOf(backend.Pid);
        string observed = string.Join(", ", sockets.Select(socket => $"{socket.Address}:{socket.Port}"));

        Assert.True(sockets.Count > 0, "the backend process has no listening TCP socket");
        Assert.True(sockets.All(socket => socket.Address.Equals(IPAddress.Loopback)), $"listening on more than 127.0.0.1: {observed}");
        Assert.True(sockets.Any(socket => socket.Port == backend.Port), $"connection.json port {backend.Port} not among {observed}");
    }

    [LinuxFact]
    public async Task A2_MissingOrWrongSecretIsUnauthorized()
    {
        await using BackendScenario scenario = BackendScenario.Create("a2");
        BackendProcess backend = await scenario.StartBackendAsync();
        string token = backend.Token;
        string wrongToken = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        ApiResult missing = await backend.SendAsync(HttpMethod.Get, "/api/status", adjust: request => request.Headers.Authorization = null);
        ApiResult wrong = await backend.SendAsync(
            HttpMethod.Get, "/api/status", adjust: request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongToken));
        ApiResult otherScheme = await backend.SendAsync(
            HttpMethod.Get, "/api/status", adjust: request => request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token));
        ApiResult startWithoutSecret = await backend.SendAsync(
            HttpMethod.Post, "/api/server/start", adjust: request => request.Headers.Authorization = null);
        ApiResult valid = await backend.GetAsync("/api/status");

        ApiAssert.Error(missing, 401, "unauthorized");
        ApiAssert.Error(wrong, 401, "unauthorized");
        ApiAssert.Error(otherScheme, 401, "unauthorized");
        ApiAssert.Error(startWithoutSecret, 401, "unauthorized");
        Assert.True(valid.Code == 200, $"the correct secret must be accepted: {valid.Describe()}");
    }

    [LinuxFact]
    public async Task A3_OriginHeaderIsForbiddenBeforeAuthentication()
    {
        await using BackendScenario scenario = BackendScenario.Create("a3");
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult crossSite = await backend.SendAsync(
            HttpMethod.Get, "/api/status", adjust: request => request.Headers.Add("Origin", "http://evil.example"));
        ApiResult sameAddress = await backend.SendAsync(
            HttpMethod.Get, "/api/status", adjust: request => request.Headers.Add("Origin", $"http://127.0.0.1:{backend.Port}"));
        ApiResult withoutSecret = await backend.SendAsync(
            HttpMethod.Get,
            "/api/status",
            adjust: request =>
            {
                request.Headers.Authorization = null;
                request.Headers.Add("Origin", "http://evil.example");
            });

        ApiAssert.Error(crossSite, 403, "forbidden");
        ApiAssert.Error(sameAddress, 403, "forbidden");
        ApiAssert.Error(withoutSecret, 403, "forbidden");
    }

    [LinuxFact]
    public async Task A4_UnexpectedHostIsForbiddenBeforeAuthentication()
    {
        await using BackendScenario scenario = BackendScenario.Create("a4");
        BackendProcess backend = await scenario.StartBackendAsync();
        int port = backend.Port;
        string[] badHosts = ["evil.example", $"evil.example:{port}", "127.0.0.1", "127.0.0.1:1", $"192.168.0.10:{port}"];

        foreach (string host in badHosts)
        {
            ApiResult result = await backend.SendAsync(HttpMethod.Get, "/api/status", adjust: request => request.Headers.Host = host);
            Assert.True(result.Code == 403, $"Host '{host}': expected 403, got {result.Describe()}");
            ApiAssert.Error(result, 403, "forbidden");
        }

        ApiResult withoutSecret = await backend.SendAsync(
            HttpMethod.Get,
            "/api/status",
            adjust: request =>
            {
                request.Headers.Authorization = null;
                request.Headers.Host = "evil.example";
            });
        ApiResult localhost = await backend.SendAsync(HttpMethod.Get, "/api/status", adjust: request => request.Headers.Host = $"localhost:{port}");
        ApiResult loopback = await backend.SendAsync(HttpMethod.Get, "/api/status", adjust: request => request.Headers.Host = $"127.0.0.1:{port}");

        ApiAssert.Error(withoutSecret, 403, "forbidden");
        Assert.True(localhost.Code == 200, $"Host localhost:{port} must be accepted: {localhost.Describe()}");
        Assert.True(loopback.Code == 200, $"Host 127.0.0.1:{port} must be accepted: {loopback.Describe()}");
    }

    [LinuxFact]
    public async Task A5_BodyOver16KiBIsRejectedWith413()
    {
        await using BackendScenario scenario = BackendScenario.Create("a5");
        BackendProcess backend = await scenario.StartBackendAsync();
        string oversized = JsonSerializer.Serialize(new { commit = new string('a', 20_000) });
        string small = JsonSerializer.Serialize(new { commit = new string('a', 1_000) });

        ApiResult tooLarge = await backend.SendAsync(HttpMethod.Put, "/api/releases/current", oversized);
        ApiResult control = await backend.SendAsync(HttpMethod.Put, "/api/releases/current", small);

        Assert.True(oversized.Length > 16 * 1024, "the oversized body must exceed 16 KiB");
        Assert.True(tooLarge.Code == 413, $"body of {oversized.Length} bytes: expected 413, got {tooLarge.Describe()}");
        ApiAssert.Error(control, 400, "invalidCommit");
    }

    [LinuxFact]
    [SupportedOSPlatform("linux")]
    public async Task A6_ConnectionFileAppearsAfterListeningIsPrivateAndRemovedOnShutdown()
    {
        await using BackendScenario scenario = BackendScenario.Create("a6");
        BackendProcess backend = await scenario.StartBackendAsync();
        string file = backend.ConnectionFile;

        // StartBackendAsync only accepts a connection.json whose pid is this backend process, so "pid" is checked there.
        JsonNode connection = JsonFields.ReadFile(file);
        UnixFileMode mode = File.GetUnixFileMode(file);
        long port = JsonFields.Number(connection, "port");
        string token = JsonFields.Text(connection, "token");
        IReadOnlyList<ListeningSocket> sockets = LinuxProcess.ListeningSocketsOf(backend.Pid);
        int exitCode = await backend.TerminateAsync(TimeSpan.FromSeconds(30));
        bool removed = !File.Exists(file);
        BackendProcess restarted = await scenario.StartBackendAsync();

        Assert.True(backend.ListeningWhenConnectionFileAppeared, "connection.json existed before the port accepted connections");
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, mode);
        Assert.True(sockets.Any(socket => socket.Port == port), $"connection.json port {port} is not where the backend listens");
        Assert.True(token.Length >= 32, $"token '{token}' is too short for a 32-byte secret");
        JsonFields.Time(connection, "startedAt");
        Assert.True(removed, $"connection.json was not removed after SIGTERM (backend exit {exitCode})");
        Assert.NotEqual(token, restarted.Token);
    }
}
