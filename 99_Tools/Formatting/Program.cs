using Dawnholder.Tools.Formatting;

try
{
    var arguments = new Arguments(args);
    var root = Path.GetFullPath(arguments.Required("root"));
    var sdk = await SdkSelection.VerifyAsync(arguments.Required("dotnet"), root);
    var command = arguments.Command;
    switch (command)
    {
        case "projects":
            {
                string[] inputs;
                if (arguments.Has("manifest"))
                {
                    var manifest = await JsonFiles.ReadAsync<InputManifest>(arguments.Required("manifest"));
                    manifest.ValidateFiles(root, sdk);
                    inputs = manifest.Files.Select(file => file.Path).ToArray();
                }
                else inputs = await InputManifest.GitInputsAsync(arguments.Has("git-root") ? arguments.Required("git-root") : root);
                var registration = ProjectRegistration.Load(root, inputs);
                await JsonFiles.WriteAsync(arguments.Required("out"), new { registration.ProductProjects, registration.IndependentProjects });
                break;
            }
        case "snapshot":
            {
                var destination = Path.GetFullPath(arguments.Required("after"));
                if (!Directory.Exists(destination) || Directory.EnumerateFileSystemEntries(destination).Any()) throw new InvalidDataException("Snapshot destination must be an existing empty directory.");
                InputManifest? manifest = null;
                if (arguments.Has("manifest"))
                {
                    manifest = await JsonFiles.ReadAsync<InputManifest>(arguments.Required("manifest"));
                    manifest.ValidateFiles(root, sdk);
                }
                var inputs = manifest?.Files.Select(file => file.Path).ToArray() ?? await InputManifest.GitInputsAsync(root);
                var copied = new List<InputFile>();
                foreach (var relative in inputs)
                {
                    var source = InputPaths.Resolve(root, relative);
                    var hash = InputPaths.Hash(source);
                    var target = InputPaths.Destination(destination, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(source, target);
                    if (InputPaths.Hash(target) != hash || InputPaths.Hash(source) != hash) throw new InvalidDataException($"Source changed during snapshot: {relative}");
                    copied.Add(new InputFile(relative, hash, new FileInfo(source).Length, "snapshot-input"));
                }
                manifest?.ValidateFiles(destination, sdk);
                await JsonFiles.WriteAsync(arguments.Required("out"), copied);
                break;
            }
        case "manifest":
            {
                var manifest = await InputManifest.CreateAsync(root, sdk, arguments.Has("git-root") ? arguments.Required("git-root") : root);
                await JsonFiles.WriteAsync(arguments.Required("out"), manifest);
                break;
            }
        case "validate":
            {
                var manifest = await JsonFiles.ReadAsync<InputManifest>(arguments.Required("manifest"));
                if (arguments.Has("files-only"))
                {
                    manifest.ValidateFiles(root, sdk);
                    if (arguments.Has("git")) await manifest.ValidateGitAsync(root);
                }
                else await manifest.ValidateAsync(root, sdk, arguments.Has("git"));
                Console.WriteLine($"Validated {manifest.Files.Count} inputs, checkout {manifest.CheckoutSha}, SDK {sdk.Version}.");
                break;
            }
        case "sync-inputs":
            {
                var manifest = await JsonFiles.ReadAsync<InputManifest>(arguments.Required("manifest"));
                manifest.ValidateFiles(root, sdk);
                var destination = Path.GetFullPath(arguments.Required("after"));
                foreach (var file in manifest.Files.Where(file => InputPaths.RootInputs.Contains(file.Path, StringComparer.Ordinal) || InputPaths.OptionalRootInputs.Contains(file.Path, StringComparer.Ordinal)))
                {
                    var path = InputPaths.Destination(destination, file.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.Copy(InputPaths.Resolve(root, file.Path), path, true);
                }
                manifest.ValidateFiles(destination, sdk);
                break;
            }
        case "compare":
            {
                var manifest = await JsonFiles.ReadAsync<InputManifest>(arguments.Required("manifest"));
                await manifest.ValidateAsync(root, sdk, arguments.Has("git"));
                var after = Path.GetFullPath(arguments.Required("after"));
                var result = await Preservation.CompareAsync(root, after, manifest, sdk);
                await JsonFiles.WriteAsync(arguments.Required("out"), result);
                Console.WriteLine($"Preserved {result.Files.Count} files across actual Debug/Release parse options.");
                break;
            }
        case "check-report":
            {
                var manifest = await JsonFiles.ReadAsync<InputManifest>(arguments.Required("manifest"));
                await manifest.ValidateAsync(root, sdk, false);
                FormatterReport.Validate(root, arguments.Required("report"), manifest, arguments.Has("zero"));
                break;
            }
        default:
            throw new InvalidOperationException("Expected projects, snapshot, manifest, validate, sync-inputs, compare, or check-report.");
    }
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception}");
    return 1;
}
