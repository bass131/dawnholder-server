using System.Diagnostics;
using System.Text;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>Runs a short-lived helper process to completion; a timeout kills its whole tree before failing.</summary>
internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string executable,
        IEnumerable<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        ProcessStartInfo start = new(executable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        ApplyEnvironment(start, environment);
        using Process process = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {executable}.");
        process.StandardInput.Close();
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Fail($"{executable} {string.Join(' ', start.ArgumentList)} did not exit in {timeout}.\n{await stdout}\n{await stderr}");
        }

        return new ProcessResult(process.ExitCode, await stdout, await stderr);
    }

    /// <summary>A null value removes the variable from the inherited environment.</summary>
    public static void ApplyEnvironment(ProcessStartInfo start, IReadOnlyDictionary<string, string?>? environment)
    {
        if (environment == null)
        {
            return;
        }

        foreach ((string name, string? value) in environment)
        {
            if (value == null)
            {
                start.Environment.Remove(name);
            }
            else
            {
                start.Environment[name] = value;
            }
        }
    }
}

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string Combined => StandardOutput + StandardError;

    public string Describe() => $"exit {ExitCode}\n--- stdout ---\n{StandardOutput}\n--- stderr ---\n{StandardError}";
}
