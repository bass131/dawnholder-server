using System.Diagnostics;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// A GameServerStub started directly by a test: for the stub's own contract checks, and as "another process" that
/// already listens on the server port. Disposing it kills it.
/// </summary>
internal sealed class StubProcess : IDisposable
{
    readonly Process _process;
    readonly List<string> _lines = [];

    StubProcess(Process process)
    {
        _process = process;
    }

    public int Pid => _process.Id;

    public bool HasExited => _process.HasExited;

    public int ExitCode => _process.ExitCode;

    public IReadOnlyList<string> Lines
    {
        get
        {
            lock (_lines)
            {
                return [.. _lines];
            }
        }
    }

    public static StubProcess Start(string stubAssembly, StubBehavior behavior)
    {
        ProcessStartInfo start = new(BuiltProcesses.Dotnet)
        {
            WorkingDirectory = Path.GetDirectoryName(stubAssembly)!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(stubAssembly);
        foreach ((string name, string value) in behavior.ToEnvironment())
        {
            start.Environment[name] = value;
        }

        Process process = new() { StartInfo = start };
        StubProcess stub = new(process);
        process.OutputDataReceived += (_, line) => stub.Append("stdout", line.Data);
        process.ErrorDataReceived += (_, line) => stub.Append("stderr", line.Data);
        Assert.True(process.Start(), "stub did not start");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return stub;
    }

    /// <summary>Lines are recorded as "stdout: text" or "stderr: text".</summary>
    public async Task WaitForLineAsync(string line, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (!Lines.Contains(line))
        {
            Assert.True(DateTime.UtcNow < deadline, $"stub did not print '{line}' within {timeout}; saw:\n{string.Join('\n', Lines)}");
            await Task.Delay(20);
        }
    }

    public async Task<bool> WaitForExitAsync(TimeSpan timeout)
    {
        try
        {
            await _process.WaitForExitAsync().WaitAsync(timeout);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public void SendEnter() => _process.StandardInput.WriteLine();

    public void CloseInput() => _process.StandardInput.Close();

    public void Dispose()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }

        _process.Dispose();
    }

    void Append(string stream, string? data)
    {
        if (data == null)
        {
            return;
        }

        lock (_lines)
        {
            _lines.Add($"{stream}: {data}");
        }
    }
}
