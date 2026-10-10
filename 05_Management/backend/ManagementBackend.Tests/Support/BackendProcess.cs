using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// One running backend, started as `dotnet Dawnholder.Management.Backend.dll --config &lt;file&gt;` and reached only
/// through the port and token of its connection.json. Disposing it shuts it down and kills it if that fails.
/// </summary>
internal sealed class BackendProcess : IAsyncDisposable
{
    static readonly TimeSpan ConnectionFileTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);

    readonly Process _process;
    readonly StringBuilder _output = new();
    readonly HttpClient _http = new(new SocketsHttpHandler { UseProxy = false }) { Timeout = Timeout.InfiniteTimeSpan };

    BackendProcess(Process process, string dataDirectory)
    {
        _process = process;
        DataDirectory = dataDirectory;
    }

    public int Pid => _process.Id;

    public string DataDirectory { get; }

    public string ConnectionFile => Path.Combine(DataDirectory, "connection.json");

    /// <summary>Parsed connection.json of this process (its pid matched).</summary>
    public JsonNode Connection { get; private set; } = new JsonObject();

    public int Port => (int)JsonFields.Number(Connection, "port");

    public string Token => JsonFields.Text(Connection, "token");

    /// <summary>Whether a TCP connect succeeded right after connection.json first appeared (it must follow listening).</summary>
    public bool ListeningWhenConnectionFileAppeared { get; private set; }

    public bool HasExited => _process.HasExited;

    public string Output
    {
        get
        {
            lock (_output)
            {
                return _output.ToString();
            }
        }
    }

    public static async Task<BackendProcess> StartAsync(
        string configPath,
        string dataDirectory,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment)
    {
        Process process = new() { StartInfo = CreateStartInfo(configPath, workingDirectory, environment) };
        BackendProcess backend = new(process, dataDirectory);
        process.OutputDataReceived += (_, line) => backend.Append(line.Data);
        process.ErrorDataReceived += (_, line) => backend.Append(line.Data);
        Assert.True(process.Start(), "backend process did not start");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await backend.WaitForConnectionFileAsync();
        }
        catch
        {
            await backend.DisposeAsync();
            throw;
        }

        return backend;
    }

    /// <summary>Runs a backend that is expected to refuse its configuration and exit by itself.</summary>
    public static Task<ProcessResult> RunToExitAsync(
        string configPath,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment,
        TimeSpan timeout) =>
        ProcessRunner.RunAsync(
            BuiltProcesses.Dotnet,
            [BuiltProcesses.Backend, "--config", configPath],
            workingDirectory,
            timeout,
            environment);

    /// <summary>
    /// Runs a backend that should refuse its configuration and exit by itself. A backend that starts anyway would run
    /// until stopped, so the wait also ends when one of <paramref name="connectionFiles"/> appears or at
    /// <paramref name="timeout"/>, and the process is killed then. Nothing outlives the call.
    /// </summary>
    public static async Task<RefusalRun> RunExpectingRefusalAsync(
        string configPath,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment,
        IReadOnlyList<string> connectionFiles,
        TimeSpan timeout)
    {
        using Process process = new() { StartInfo = CreateStartInfo(configPath, workingDirectory, environment) };
        StringBuilder output = new();
        process.OutputDataReceived += (_, line) => AppendTo(output, line.Data);
        process.ErrorDataReceived += (_, line) => AppendTo(output, line.Data);
        Stopwatch watch = Stopwatch.StartNew();
        Assert.True(process.Start(), "backend process did not start");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        while (!process.HasExited && watch.Elapsed < timeout && !connectionFiles.Any(File.Exists))
        {
            await Task.Delay(20);
        }

        bool exitedByItself = process.HasExited;
        if (!exitedByItself)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        process.WaitForExit(); // drains the asynchronous output readers before reporting
        watch.Stop();
        lock (output)
        {
            return new RefusalRun(exitedByItself, process.ExitCode, output.ToString(), [.. connectionFiles.Where(File.Exists)], watch.Elapsed);
        }
    }

    public Task<ApiResult> GetAsync(string path) => SendAsync(HttpMethod.Get, path);

    public Task<ApiResult> PostAsync(string path, object? body = null, TimeSpan? timeout = null) =>
        SendAsync(HttpMethod.Post, path, body == null ? null : JsonSerializer.Serialize(body), timeout: timeout);

    public Task<ApiResult> PutAsync(string path, object body) => SendAsync(HttpMethod.Put, path, JsonSerializer.Serialize(body));

    /// <summary>
    /// Sends one request. By default it carries the bearer token and a Host of 127.0.0.1:&lt;port&gt;;
    /// <paramref name="adjust"/> may change or remove headers for the access-boundary tests.
    /// </summary>
    public async Task<ApiResult> SendAsync(
        HttpMethod method,
        string path,
        string? jsonBody = null,
        Action<HttpRequestMessage>? adjust = null,
        TimeSpan? timeout = null)
    {
        using HttpRequestMessage request = new(method, $"http://127.0.0.1:{Port}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        if (jsonBody != null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        adjust?.Invoke(request);
        using CancellationTokenSource cancel = new(timeout ?? DefaultRequestTimeout);
        try
        {
            using HttpResponseMessage response = await _http.SendAsync(request, cancel.Token);
            return new ApiResult(response.StatusCode, await response.Content.ReadAsStringAsync(cancel.Token));
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
        {
            Assert.Fail($"{method} {path} failed without an HTTP answer: {error.Message}\n--- backend output ---\n{Output}");
            throw;
        }
    }

    public async Task<JsonNode> StatusAsync()
    {
        ApiResult result = await GetAsync("/api/status");
        Assert.True(result.Code == 200, $"GET /api/status: {result.Describe()}");
        return result.Json!;
    }

    /// <summary>Polls GET /api/status until <paramref name="condition"/> holds; fails with the last status.</summary>
    public async Task<JsonNode> WaitForStatusAsync(Func<JsonNode, bool> condition, TimeSpan timeout, string what)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            JsonNode status = await StatusAsync();
            if (condition(status))
            {
                return status;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"timed out after {timeout} waiting for {what}; last status {JsonFields.Show(status)}");
            }

            await Task.Delay(100);
        }
    }

    /// <summary>Sends SIGTERM (the backend's normal shutdown) and waits for the process to exit.</summary>
    public async Task<int> TerminateAsync(TimeSpan timeout)
    {
        LinuxProcess.Signal(Pid, LinuxProcess.SigTerm);
        try
        {
            await _process.WaitForExitAsync().WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"backend did not exit within {timeout} after SIGTERM\n--- backend output ---\n{Output}");
        }

        return _process.ExitCode;
    }

    /// <summary>Simulates a backend crash: SIGKILL gives it no chance to close records or delete connection.json.</summary>
    public async Task KillAsync()
    {
        LinuxProcess.Signal(Pid, LinuxProcess.SigKill);
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            LinuxProcess.Signal(Pid, LinuxProcess.SigTerm);
            try
            {
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        _http.Dispose();
        _process.Dispose();
    }

    static ProcessStartInfo CreateStartInfo(
        string configPath,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment)
    {
        ProcessStartInfo start = new(BuiltProcesses.Dotnet)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(BuiltProcesses.Backend);
        start.ArgumentList.Add("--config");
        start.ArgumentList.Add(configPath);
        ProcessRunner.ApplyEnvironment(start, environment);
        return start;
    }

    static void AppendTo(StringBuilder output, string? line)
    {
        if (line == null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }

    void Append(string? line) => AppendTo(_output, line);

    /// <summary>
    /// Waits for a connection.json written by this process. A file left by a killed backend has another pid and is
    /// ignored. The first sighting is followed at once by a TCP connect to check the file was written after listening.
    /// </summary>
    async Task WaitForConnectionFileAsync()
    {
        DateTime deadline = DateTime.UtcNow + ConnectionFileTimeout;
        while (true)
        {
            if (_process.HasExited)
            {
                _process.WaitForExit(); // drains the asynchronous output readers before reporting
                Assert.Fail($"backend exited with {_process.ExitCode} before writing connection.json\n{Output}");
            }

            JsonNode? connection = TryReadConnectionFile();
            if (connection is JsonObject && connection["pid"] is JsonValue pid
                && pid.GetValueKind() == JsonValueKind.Number && pid.GetValue<long>() == Pid)
            {
                Connection = connection;
                ListeningWhenConnectionFileAppeared = await CanConnectAsync(Port);
                return;
            }

            Assert.True(DateTime.UtcNow < deadline, $"no connection.json with pid {Pid} within {ConnectionFileTimeout}\n{Output}");
            await Task.Delay(20);
        }
    }

    JsonNode? TryReadConnectionFile()
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(ConnectionFile));
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    static async Task<bool> CanConnectAsync(int port)
    {
        using TcpClient client = new();
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromSeconds(2));
            return true;
        }
        catch (Exception error) when (error is SocketException or TimeoutException)
        {
            return false;
        }
    }
}

/// <summary>
/// A backend run that should have refused its configuration. <see cref="ExitedByItself"/> is false when it had to be
/// killed; <see cref="ConnectionFiles"/> lists the watched connection.json files that existed afterwards.
/// </summary>
internal sealed record RefusalRun(bool ExitedByItself, int ExitCode, string Output, IReadOnlyList<string> ConnectionFiles, TimeSpan Elapsed)
{
    public string Describe() =>
        $"exited by itself: {ExitedByItself}, exit {ExitCode} after {Elapsed.TotalSeconds:F1} s, "
        + $"connection.json written: [{string.Join(", ", ConnectionFiles)}]\n--- backend output ---\n{Output}";
}
