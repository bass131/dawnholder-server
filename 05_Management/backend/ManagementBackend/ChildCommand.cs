using System.Diagnostics;
using System.Text;

namespace Dawnholder.Management.Backend;

// A finite external command owns its entire process tree until it exits or is cancelled.
internal static class ChildCommand
{
    public static async Task<CommandResult> RunAsync(
        string executable, IEnumerable<string> arguments, string directory,
        CancellationToken cancellationToken, Action<string>? output = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProcessStartInfo start = new(executable)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.Environment["UseSharedCompilation"] = "false";
        using Process process = Process.Start(start) ?? throw new IOException($"실행 실패: {executable}");
        process.StandardInput.Close();
        Task<string> stdout = DrainAsync(process.StandardOutput, output);
        Task<string> stderr = DrainAsync(process.StandardError, output);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch
        {
            // Only this command's descendants are killed; no name-based or global build-server shutdown.
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            throw;
        }

        return new CommandResult(process.ExitCode, await stdout, await stderr);
    }

    private static async Task<string> DrainAsync(StreamReader reader, Action<string>? output)
    {
        StringBuilder tail = new();
        char[] buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer)) > 0)
        {
            string text = new(buffer, 0, count);
            output?.Invoke(text);
            tail.Append(text);
            if (tail.Length > 65536) tail.Remove(0, tail.Length - 65536);
        }

        return tail.ToString();
    }
}

internal sealed record CommandResult(int ExitCode, string Output, string Error);
