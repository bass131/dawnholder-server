using System.Text;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// A temporary Git repository holding the GameServerStub source and the pinned global.json, standing in for
/// release.sourceRepository. It lives under TMPDIR, outside the Dawnholder checkout, so the root analyzers do not apply.
/// </summary>
internal sealed class ReleaseSourceRepository
{
    public const string FirstMarker = "release-a";

    // Isolated from the user's Git configuration; identity is passed with -c only.
    static readonly Dictionary<string, string?> GitEnvironment = new(StringComparer.Ordinal)
    {
        ["GIT_CONFIG_NOSYSTEM"] = "1",
        ["GIT_CONFIG_GLOBAL"] = "/dev/null",
    };

    ReleaseSourceRepository(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public string FirstCommit { get; private set; } = string.Empty;

    /// <summary>Creates the repository with one commit whose stub prints "release-marker: release-a".</summary>
    public static async Task<ReleaseSourceRepository> CreateAsync(string root)
    {
        CopyDirectory(BuiltProcesses.ReleaseSource, root);
        ReleaseSourceRepository repository = new(root);
        await repository.GitAsync("init", "-q", "-b", "main");
        repository.FirstCommit = await repository.CommitMarkerAsync(FirstMarker);
        return repository;
    }

    /// <summary>Commits a new release-marker.txt; the stub built from it prints "release-marker: &lt;marker&gt;".</summary>
    public async Task<string> CommitMarkerAsync(string marker)
    {
        File.WriteAllText(Path.Combine(Root, "GameServerStub", "release-marker.txt"), marker + "\n");
        return await CommitAllAsync($"stub {marker}");
    }

    /// <summary>
    /// Commits a source file that does not compile, so publishing that commit fails. A following commit removes it
    /// again, so the working tree and later commits stay buildable.
    /// </summary>
    public async Task<string> CommitBrokenBuildAsync()
    {
        File.WriteAllText(Path.Combine(Root, "GameServerStub", "BrokenBuild.cs"), "this is not C#\n");
        string commit = await CommitAllAsync("stub that does not build");
        await GitAsync("rm", "-q", "GameServerStub/BrokenBuild.cs");
        await CommitAllAsync("stub builds again");
        return commit;
    }

    /// <summary>Git status including ignored files, HEAD and every working-tree file with its size and write time.</summary>
    public async Task<string> WorkingTreeSnapshotAsync()
    {
        StringBuilder snapshot = new();
        snapshot.AppendLine(await GitAsync("rev-parse", "HEAD"));
        snapshot.AppendLine(await GitAsync("status", "--porcelain=v1", "--ignored", "--untracked-files=all"));
        foreach (string file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string relative = Path.GetRelativePath(Root, file);
            if (relative.StartsWith(".git" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                continue;
            }

            FileInfo info = new(file);
            snapshot.AppendLine($"{relative} {info.Length} {info.LastWriteTimeUtc.Ticks}");
        }

        return snapshot.ToString();
    }

    static void CopyDirectory(string source, string destination)
    {
        Assert.True(Directory.Exists(source), $"release source was not copied next to the tests: {source}");
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    async Task<string> CommitAllAsync(string message)
    {
        await GitAsync("add", "-A");
        await GitAsync(
            "-c", "user.name=Management Backend Tests",
            "-c", "user.email=management-backend-tests@localhost",
            "-c", "commit.gpgsign=false",
            "commit", "-q", "-m", message);
        return (await GitAsync("rev-parse", "HEAD")).Trim();
    }

    async Task<string> GitAsync(params string[] arguments)
    {
        ProcessResult result = await ProcessRunner.RunAsync("git", arguments, Root, TimeSpan.FromSeconds(30), GitEnvironment);
        Assert.True(result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Describe()}");
        return result.StandardOutput;
    }
}
