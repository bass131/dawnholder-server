using System.Diagnostics;

namespace Dawnholder.Server.GameServer.Tests.Integration;

// Execute the real CLI; malformed create combinations must stop before connecting.
public sealed class StoredCreateArgumentTests
{
    [Theory]
    [InlineData("other-scenario", "require scenario")]
    [InlineData("legacy", "Invalid Stored create arguments")]
    [InlineData("duplicate-name", "Invalid Stored create arguments")]
    [InlineData("missing-class", "Invalid Stored create arguments")]
    [InlineData("trailing-name", "Invalid Stored create arguments")]
    [InlineData("before-with-new-id", "a different new character ID only in phase after")]
    [InlineData("after-without-new-id", "a different new character ID only in phase after")]
    [InlineData("numeric-class", "new class Knight or Mage")]
    public async Task InvalidCreateArguments_ReturnFailureBeforeNetwork(string fault, string diagnostic)
    {
        List<string> arguments = new()
        {
            "--host", "invalid-host", "--scenario", "PersistenceRestart", "--mode", "Stored",
            "--account", "alice_01", "--phase", "before", "--character-id",
            "53000000-0000-4000-8000-000000000001", "--new-name", "Wcreate", "--new-class", "Knight",
        };
        switch (fault)
        {
            case "other-scenario": arguments[3] = "smoke"; arguments.RemoveRange(4, 2); arguments.RemoveRange(8, 2); break;
            case "legacy": arguments[5] = "Legacy"; arguments.RemoveRange(10, 2); break;
            case "duplicate-name": arguments.AddRange(new[] { "--new-name", "Wsecond" }); break;
            case "missing-class": arguments.RemoveRange(14, 2); break;
            case "trailing-name": arguments.Add("--new-name"); break;
            case "before-with-new-id": arguments.AddRange(new[] { "--new-character-id", "53000000-0000-4000-8000-000000000002" }); break;
            case "after-without-new-id": arguments[9] = "after"; break;
            case "numeric-class": arguments[15] = "0"; break;
        }

        DirectoryInfo output = new(AppContext.BaseDirectory);
        string configuration = output.Parent!.Name;
        DirectoryInfo? repository = output;
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Dawnholder.slnx")))
            repository = repository.Parent;
        Assert.NotNull(repository);
        string dll = Path.Combine(repository.FullName, "99_Tools", "headless-bot", "bin", configuration,
            "net10.0", "Dawnholder.Tools.HeadlessBot.dll");
        Assert.True(File.Exists(dll), dll);
        ProcessStartInfo start = new(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(dll);
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using Process process = new() { StartInfo = start };
        Assert.True(process.Start());
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        string observed = await stdout + await stderr;
        Assert.Equal(1, process.ExitCode);
        Assert.Contains(diagnostic, observed);
        Assert.DoesNotContain("event=list-request", observed);
        Assert.DoesNotContain("createdCharacterId=", observed);
    }
}
