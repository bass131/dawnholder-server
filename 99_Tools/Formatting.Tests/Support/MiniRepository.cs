using System.Text;

namespace Dawnholder.Tools.Formatting.Tests.Support;

/// <summary>
/// A disposable Git repository shaped like the product layout: eight projects, the pinned
/// global.json and the protected generated source. Product sources are never used as inputs.
/// </summary>
internal sealed class MiniRepository : IDisposable
{
    public const string GeneratedPath = "98_Shared/Protocol/Generated/GenPackets.cs";
    public const string RegionsPath = "02_Server/Beta/Regions.cs";

    public static readonly string[] ProjectPaths =
    [
        "02_Server/Alpha/Alpha.csproj",
        "02_Server/Beta/Beta.csproj",
        "02_Server/Gamma/Gamma.csproj",
        "04_ClientNet/ClientNet.csproj",
        "98_Shared/Shared.csproj",
        "99_Tools/Delta/Delta.csproj",
        "99_Tools/Epsilon/Epsilon.csproj",
        "99_Tools/Zeta/Zeta.csproj",
    ];

    private static readonly UTF8Encoding Utf8 = new(false);

    private MiniRepository(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public static MiniRepository Create(string purpose, bool restore = true)
    {
        var repository = new MiniRepository(TestEnvironment.NewTemporaryDirectory(purpose));
        repository.WriteLayout();
        repository.Git("init", "-q");
        repository.Commit("initial");
        if (restore) repository.Restore();
        return repository;
    }

    /// <summary>A Debug/Release gate plus a region no product configuration activates.</summary>
    public static string RegionsSource(string debugLine, string neverLine) =>
        "namespace Mini.Beta;\n\npublic static class Regions\n{\n    public static void Run(bool registered)\n    {\n#if DEBUG\n" + debugLine + "\n#else\n        Check(registered, \"release\");\n#endif\n#if DAWNHOLDER_NEVER_DEFINED\n" + neverLine + "\n#endif\n    }\n\n    private static void Check(bool value, string message)\n    {\n    }\n}\n";

    public string PathOf(string relative) => Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    public void Write(string relative, string text) => WriteBytes(relative, Utf8.GetBytes(text));

    public void WriteBytes(string relative, byte[] bytes)
    {
        var path = PathOf(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    public string Read(string relative) => File.ReadAllText(PathOf(relative), Utf8);

    public void Commit(string message)
    {
        Git("add", "-A");
        Git("commit", "-q", "-m", message);
    }

    public string Git(params string[] arguments)
    {
        var result = TestEnvironment.Run("git", Root, arguments, TestEnvironment.IsolatedGit());
        if (result.ExitCode != 0) throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {result.Combined}");
        return result.StandardOutput;
    }

    public void Restore()
    {
        var result = TestEnvironment.Run(TestEnvironment.Dotnet, Root, ["restore", "Dawnholder.slnx", "--nologo"]);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Mini repository restore failed: {result.Combined}");
    }

    /// <summary>Copies tracked inputs (not bin/obj/.git) into a new directory, keeping bytes.</summary>
    public string CopyInputsTo(string purpose)
    {
        var destination = TestEnvironment.NewTemporaryDirectory(purpose);
        foreach (var relative in Git("ls-files", "-z").Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var target = Path.Combine(destination, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(PathOf(relative), target);
        }

        return destination;
    }

    public ProcessResult Cli(params string[] arguments) => TestEnvironment.RunCli(Root, arguments);

    public string CreateManifest(string? root = null)
    {
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("manifest"), "input-manifest.json");
        var result = TestEnvironment.RunCli(Root, "manifest", "--root", root ?? Root, "--git-root", Root, "--dotnet", TestEnvironment.Dotnet, "--out", output);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Baseline manifest creation failed: {result.Combined}");
        return output;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Build servers may still hold a file; the directory is unique and test-owned.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string ProjectText()
    {
        // Debug/Release keep the SDK defaults (DEBUG vs RELEASE symbols). No package references.
        return "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <Nullable>enable</Nullable>\n    <LangVersion>latest</LangVersion>\n  </PropertyGroup>\n</Project>\n";
    }

    private void WriteLayout()
    {
        var solution = new StringBuilder("<Solution>\n");
        foreach (var project in ProjectPaths) solution.Append("  <Project Path=\"").Append(project).Append("\" />\n");
        solution.Append("</Solution>\n");
        Write("Dawnholder.slnx", solution.ToString());
        File.Copy(Path.Combine(TestEnvironment.RepositoryRoot, "global.json"), PathOf("global.json"));
        Write("Directory.Build.props", "<Project>\n</Project>\n");
        Write(".editorconfig", "root = true\n\n[*.cs]\nend_of_line = lf\ncharset = utf-8\nindent_style = space\nindent_size = 4\ninsert_final_newline = true\ntrim_trailing_whitespace = true\n");
        Write(".gitattributes", "* -text\n");
        Write(".gitignore", "bin/\nobj/\n");
        Write(".github/workflows/dotnet-tests.yml", "name: mini\n");
        foreach (var project in ProjectPaths)
        {
            Write(project, ProjectText());
            var directory = Path.GetDirectoryName(project)!.Replace('\\', '/');
            var name = Path.GetFileNameWithoutExtension(project);
            Write($"{directory}/{name}Source.cs", $"namespace Mini.{name};\n\npublic static class {name}Source\n{{\n    public static int Value() => 1;\n}}\n");
        }

        Write(GeneratedPath, "namespace Mini.Generated;\n\npublic static class GenPackets\n{\n    public const string Name = \"generated\";\n}\n");
        Write(RegionsPath, RegionsSource("        Check(registered, \"debug\");", "        var  hidden = 1;"));
    }
}
