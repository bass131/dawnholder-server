using System.Text.Json;

namespace Dawnholder.Tools.Formatting;

internal sealed record InputFile(string Path, string Sha256, long Bytes, string Category);
internal sealed record InputManifest(int SchemaVersion, string CheckoutSha, string WorkTreeStatus, SdkSelection Sdk, List<InputFile> Files, List<ProjectInputs> Projects)
{
    public static async Task<InputManifest> CreateAsync(string root, SdkSelection sdk, string gitRoot)
    {
        var checkout = (await Processes.RunAsync("git", gitRoot, "rev-parse", "HEAD")).Trim();
        var status = await Processes.RunAsync("git", gitRoot, "status", "--porcelain=v1", "-z", "--untracked-files=all");
        var paths = await GitInputsAsync(gitRoot);
        var files = paths.Select(path =>
        {
            var absolute = InputPaths.Resolve(root, path);
            return new InputFile(path, InputPaths.Hash(absolute), new FileInfo(absolute).Length, Category(path));
        }).ToList();
        foreach (var required in InputPaths.RootInputs.Append(InputPaths.Generated))
            if (!paths.Contains(required, StringComparer.Ordinal)) throw new InvalidDataException($"Missing Git input: {required}");
        var workspace = await WorkspaceInputs.LoadAsync(root, sdk, paths);
        var result = new InputManifest(1, checkout, status, sdk, files, workspace.Projects);
        await result.ValidateAsync(root, sdk, Path.GetFullPath(root) == Path.GetFullPath(gitRoot));
        result.ValidateFiles(gitRoot, sdk);
        await result.ValidateGitAsync(gitRoot);
        return result;
    }

    public static async Task<string[]> GitInputsAsync(string root)
    {
        var tracked = await Processes.RunAsync("git", root, "ls-files", "-z");
        var untracked = await Processes.RunAsync("git", root, "ls-files", "--others", "--exclude-standard", "-z");
        return (tracked + untracked).Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(InputPaths.IsInput).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    public async Task ValidateAsync(string root, SdkSelection sdk, bool git)
    {
        ValidateFiles(root, sdk);
        if (git) await ValidateGitAsync(root);
        var actual = await WorkspaceInputs.LoadAsync(root, sdk, Files.Select(file => file.Path));
        if (JsonSerializer.Serialize(actual.Projects) != JsonSerializer.Serialize(Projects)) throw new InvalidDataException("Compile set or project parse options differ from manifest.");
    }

    public async Task ValidateGitAsync(string root)
    {
        if ((await Processes.RunAsync("git", root, "rev-parse", "HEAD")).Trim() != CheckoutSha || await Processes.RunAsync("git", root, "status", "--porcelain=v1", "-z", "--untracked-files=all") != WorkTreeStatus) throw new InvalidDataException("Git checkout/status changed after capture.");
        if (!Files.Select(file => file.Path).SequenceEqual(await GitInputsAsync(root), StringComparer.Ordinal)) throw new InvalidDataException("Git input set changed after capture.");
    }

    public void ValidateFiles(string root, SdkSelection sdk)
    {
        if (SchemaVersion != 1 || CheckoutSha == null || CheckoutSha.Length != 40 || CheckoutSha.Any(character => !Uri.IsHexDigit(character)) || WorkTreeStatus == null || Files == null || Files.Count == 0 || Projects == null || Projects.Count == 0 || Sdk == null || !ProvenancePath(Sdk.Executable) || !ProvenancePath(Sdk.SdkDirectory) || !Sdk.SdkDirectory.Replace('\\', '/').EndsWith("/" + SdkSelection.Required, StringComparison.Ordinal) || Sdk.Version != SdkSelection.Required || sdk.Version != Sdk.Version || Sdk.FormatterVersion != sdk.FormatterVersion)
            throw new InvalidDataException("Incomplete/unknown manifest or SDK mismatch.");
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Files)
        {
            if (file == null || !InputPaths.IsInput(file.Path) || !unique.Add(file.Path) || file.Sha256 == null || file.Sha256.Length != 64 || file.Sha256.Any(character => !Uri.IsHexDigit(character)) || file.Category != Category(file.Path)) throw new InvalidDataException("Invalid/duplicate/unknown manifest entry.");
            var absolute = InputPaths.Resolve(root, file.Path);
            if (new FileInfo(absolute).Length != file.Bytes || InputPaths.Hash(absolute) != file.Sha256) throw new InvalidDataException($"Input changed before inspection: {file.Path}");
        }
        foreach (var required in InputPaths.RootInputs.Append(InputPaths.Generated))
            if (!Files.Any(file => file.Path == required)) throw new InvalidDataException($"Missing required input: {required}");
        foreach (var optional in InputPaths.OptionalRootInputs)
            if (File.Exists(Path.Combine(root, optional)) && !Files.Any(file => file.Path == optional)) throw new InvalidDataException($"Uncaptured root build input: {optional}");
        ProjectRegistration.Load(root, Files.Select(file => file.Path));
        if (!Files.Select(file => file.Path).SequenceEqual(ObservedInputs(root).Order(StringComparer.Ordinal), StringComparer.Ordinal)) throw new InvalidDataException("Unknown or missing source/configuration inputs in the inspected tree.");
    }

    private static bool ProvenancePath(string? path) => !string.IsNullOrWhiteSpace(path) && (path.StartsWith('/') || path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\') && !path.Replace('\\', '/').Split('/').Any(part => part is "." or "..");

    private static IEnumerable<string> ObservedInputs(string root)
    {
        foreach (var path in InputPaths.RootInputs.Concat(InputPaths.OptionalRootInputs))
            if (File.Exists(Path.Combine(root, path))) yield return path;
        foreach (var tree in InputPaths.Trees)
        {
            var pending = new Stack<string>();
            pending.Push(Path.Combine(root, tree));
            while (pending.TryPop(out var directory))
            {
                if (File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException($"Linked source directory: {directory}");
                foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
                {
                    var attributes = File.GetAttributes(entry);
                    if (attributes.HasFlag(FileAttributes.Directory))
                    {
                        if (Path.GetFileName(entry) is not ("bin" or "obj" or "secrets")) pending.Push(entry);
                    }
                    else
                    {
                        var relative = InputPaths.Relative(root, entry);
                        if (InputPaths.IsInput(relative) && !InputPaths.RootInputs.Contains(relative, StringComparer.Ordinal))
                        {
                            if (attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException($"Linked source file: {entry}");
                            yield return relative;
                        }
                    }
                }
            }
        }
    }

    private static string Category(string path) => path == InputPaths.Generated ? "generated-source" : path.StartsWith("99_Tools/Formatting.Tests/Fixtures/", StringComparison.Ordinal) ? "fixture" : path.EndsWith(".cs", StringComparison.Ordinal) ? path.StartsWith("99_Tools/Formatting", StringComparison.Ordinal) ? "tool-source" : "handwritten-source" : "build-or-policy-input";
}

internal static class FormatterReport
{
    public static void Validate(string root, string report, InputManifest manifest, bool zero)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(report));
        if (json.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Formatter report must be an array.");
        var files = manifest.Files.ToDictionary(file => file.Path, StringComparer.Ordinal);
        foreach (var change in json.RootElement.EnumerateArray())
        {
            var name = change.GetProperty("FilePath").GetString() ?? throw new InvalidDataException("Missing report path.");
            var relative = Path.IsPathFullyQualified(name) ? InputPaths.Relative(root, name) : name.Replace('\\', '/');
            if (!files.TryGetValue(relative, out var input) || input.Category is not ("handwritten-source" or "tool-source")) throw new InvalidDataException($"Unexpected formatter path: {name}");
        }
        if (zero && json.RootElement.GetArrayLength() != 0) throw new InvalidDataException($"Formatter reported {json.RootElement.GetArrayLength()} files.");
    }
}
