using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dawnholder.Tools.Formatting;

internal sealed class Arguments
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public Arguments(string[] args)
    {
        if (args.Length == 0) throw new ArgumentException("Command is required.");
        Command = args[0];
        for (var index = 1; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Expected --option.");
            var name = args[index][2..];
            var value = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal) ? args[++index] : "true";
            if (!_values.TryAdd(name, value)) throw new ArgumentException($"Duplicate option: {name}");
        }
        var allowed = new HashSet<string>(["root", "dotnet", "out", "manifest", "after", "git", "git-root", "report", "zero", "files-only"], StringComparer.Ordinal);
        if (_values.Keys.Any(key => !allowed.Contains(key))) throw new ArgumentException("Unknown option.");
    }

    public string Command { get; }
    public bool Has(string name) => _values.ContainsKey(name);
    public string Required(string name) => _values.TryGetValue(name, out var value) && value != "true" ? value : throw new ArgumentException($"Missing --{name}.");
}

internal static class Processes
{
    public static async Task<string> RunAsync(string executable, string root, params string[] arguments)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        info.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await stdout;
        var error = await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} {string.Join(' ', arguments)} exited {process.ExitCode}: {error}{output}");
        return output;
    }
}

internal static class JsonFiles
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };

    public static async Task<T> ReadAsync<T>(string path) => JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), Options) ?? throw new InvalidDataException($"Empty JSON: {path}");
    public static async Task WriteAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, Options) + "\n", new UTF8Encoding(false));
    }
}

internal static class InputPaths
{
    public static readonly string[] Trees = ["02_Server", "04_ClientNet", "98_Shared", "99_Tools"];
    public static readonly string[] RootInputs = ["Dawnholder.slnx", "global.json", "Directory.Build.props", ".editorconfig", ".gitattributes", ".github/workflows/dotnet-tests.yml"];
    public static readonly string[] OptionalRootInputs = ["Directory.Build.targets", "NuGet.config", "nuget.config", "packages.lock.json"];
    public const string Generated = "98_Shared/Protocol/Generated/GenPackets.cs";
    public const string ToolProject = "99_Tools/Formatting/Formatting.csproj";
    public const string TestProject = "99_Tools/Formatting.Tests/Formatting.Tests.csproj";

    public static string Relative(string root, string path)
    {
        var relative = Path.GetRelativePath(root, Path.GetFullPath(path)).Replace('\\', '/');
        ValidateRelative(relative);
        return relative;
    }

    public static void ValidateRelative(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Contains('\\') || relative.Contains(':') || relative.Contains('\0') || relative.StartsWith('/') || relative.Split('/').Any(part => part is "" or "." or ".."))
            throw new InvalidDataException($"Unsafe relative path: {relative}");
    }

    public static string Resolve(string root, string relative)
    {
        ValidateRelative(relative);
        root = Path.GetFullPath(root);
        var result = Path.GetFullPath(Path.Combine(root, relative));
        if (!result.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException($"Path escapes root: {relative}");
        var current = new DirectoryInfo(root);
        if (current.LinkTarget != null) throw new InvalidDataException($"Root is a symbolic link: {root}");
        var components = relative.Split('/');
        for (var index = 0; index < components.Length; index++)
        {
            var component = Path.Combine(root, Path.Combine(components[..(index + 1)]));
            if (File.GetAttributes(component).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException($"Linked input: {relative}");
        }
        if (!File.Exists(result)) throw new FileNotFoundException($"Missing input: {relative}");
        return result;
    }

    public static string Destination(string root, string relative)
    {
        ValidateRelative(relative);
        root = Path.GetFullPath(root);
        if (!Directory.Exists(root) || new DirectoryInfo(root).LinkTarget != null) throw new InvalidDataException("Destination must be an existing real directory.");
        var components = relative.Split('/');
        for (var index = 0; index < components.Length; index++)
        {
            var component = Path.Combine(root, Path.Combine(components[..(index + 1)]));
            if ((File.Exists(component) || Directory.Exists(component)) && File.GetAttributes(component).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException($"Linked destination: {relative}");
        }
        return Path.Combine(root, relative);
    }

    public static bool IsInput(string path)
    {
        ValidateRelative(path);
        if (RootInputs.Contains(path, StringComparer.Ordinal) || OptionalRootInputs.Contains(path, StringComparer.Ordinal)) return true;
        if (!Trees.Any(tree => path.StartsWith(tree + "/", StringComparison.Ordinal))) return false;
        if (path.Split('/').Any(part => part is "bin" or "obj" or "secrets")) return false;
        if (path.StartsWith("99_Tools/Formatting/", StringComparison.Ordinal) || path.StartsWith("99_Tools/Formatting.Tests/", StringComparison.Ordinal)) return true;
        if (path.StartsWith("98_Shared/GameData/Maps/", StringComparison.Ordinal) && path.EndsWith(".bin", StringComparison.Ordinal)) return true;
        return Path.GetExtension(path) is ".cs" or ".csproj" or ".props" or ".targets" || Path.GetFileName(path) is ".editorconfig" or "NuGet.config" or "nuget.config" or "packages.lock.json" || path is "99_Tools/sync-wsl.sh" or "99_Tools/format-check.sh" or "99_Tools/format-check.ps1" or "99_Tools/PacketGenerator/PDL.xml";
    }

    public static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}

internal sealed record SdkSelection(string Executable, string Version, string FormatterVersion, string SdkDirectory)
{
    public const string Required = "10.0.301";

    public static async Task<SdkSelection> VerifyAsync(string executable, string root)
    {
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable)) throw new InvalidOperationException($"SDK executable must exist at an absolute path: {executable}");
        using var pin = JsonDocument.Parse(await File.ReadAllTextAsync(InputPaths.Resolve(root, "global.json")));
        var config = pin.RootElement.GetProperty("sdk");
        if (config.GetProperty("version").GetString() != Required || config.GetProperty("rollForward").GetString() != "disable") throw new InvalidDataException("global.json must pin 10.0.301 with rollForward:disable.");
        var version = (await Processes.RunAsync(executable, root, "--version")).Trim();
        if (version != Required) throw new InvalidOperationException($"Required SDK {Required}; executable {executable}; observed {version}.");
        var formatter = (await Processes.RunAsync(executable, root, "format", "--version")).Trim();
        var sdks = await Processes.RunAsync(executable, root, "--list-sdks");
        var row = sdks.Split('\n').Single(line => line.StartsWith(Required + " [", StringComparison.Ordinal)).Trim();
        var directory = Path.Combine(row[(row.IndexOf('[') + 1)..^1], Required);
        return new SdkSelection(Path.GetFullPath(executable), version, formatter, directory);
    }
}
