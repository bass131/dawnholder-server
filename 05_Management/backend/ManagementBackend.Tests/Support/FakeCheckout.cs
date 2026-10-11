namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// A throwaway checkout for `backend-wsl.sh init-config` (screen-design.md 「백엔드 조정」 B-2): only a copy of the
/// repository's backend-wsl.sh in 05_Management/backend/ and the .git the test writes. The copy runs with HOME set to a
/// temporary folder, so the real configuration file, WSL copy and lock are never touched. Disposal deletes everything.
/// </summary>
internal sealed class FakeCheckout : IDisposable
{
    /// <summary>A worktree's .git file as Git writes it on Windows; the clone that owns the objects is C:/Dev/Fake.</summary>
    public const string WorktreeGitFile = "gitdir: C:/Dev/Fake/.git/worktrees/w\n";

    static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(20);

    readonly ScratchDirectory _scratch;

    FakeCheckout(string purpose)
    {
        _scratch = new ScratchDirectory(purpose);
        Root = _scratch.Create("checkout");
        Home = _scratch.Create("home");
        string backend = _scratch.Create(Path.Combine("checkout", "05_Management", "backend"));
        File.Copy(BuiltProcesses.BackendEntryScript, Path.Combine(backend, "backend-wsl.sh"));
    }

    public string Root { get; }

    public string Home { get; }

    /// <summary>The default configuration file of backend-design.md 「설정」 under the temporary HOME.</summary>
    public string ConfigFile => Path.Combine(Home, ".config", "dawnholder", "management", "backend.json");

    /// <summary>Where backend-wsl.sh keeps the WSL copy and its lock under the temporary HOME.</summary>
    public string BackendCache => Path.Combine(Home, ".cache", "dawnholder", "management");

    string GitPath => Path.Combine(Root, ".git");

    string Script => Path.Combine(Root, "05_Management", "backend", "backend-wsl.sh");

    public static FakeCheckout Create(string purpose) => new(purpose);

    public void WriteGitFile(string content)
    {
        RemoveGit();
        File.WriteAllText(GitPath, content);
    }

    /// <summary>Makes the checkout an ordinary clone: a real .git directory from `git init`.</summary>
    public async Task InitGitRepositoryAsync()
    {
        RemoveGit();
        ProcessResult init = await ProcessRunner.RunAsync(
            "git",
            ["-c", "init.defaultBranch=main", "init", "--quiet", Root],
            _scratch.Root,
            RunTimeout,
            new Dictionary<string, string?> { ["HOME"] = Home });
        Assert.True(init.ExitCode == 0, $"git init in the fake checkout failed: {init.Describe()}");
    }

    public void RemoveGit()
    {
        if (File.Exists(GitPath))
        {
            File.Delete(GitPath);
        }
        else if (Directory.Exists(GitPath))
        {
            Directory.Delete(GitPath, recursive: true);
        }
    }

    /// <summary>Runs `bash &lt;copy&gt;/backend-wsl.sh init-config` from outside the checkout with the temporary HOME.</summary>
    public Task<ProcessResult> InitConfigAsync() =>
        ProcessRunner.RunAsync("bash", [Script, "init-config"], _scratch.Root, RunTimeout, new Dictionary<string, string?> { ["HOME"] = Home });

    public void Dispose() => _scratch.Dispose();
}
