using System.Text.Json;
using System.Xml.Linq;

namespace Dawnholder.Tools.Formatting;

internal sealed record ProjectRegistration(string[] ProductProjects, string[] IndependentProjects)
{
    public const string IndependentList = "99_Tools/Formatting/independent-projects.json";

    public IEnumerable<string> AllProjects => ProductProjects.Concat(IndependentProjects);

    public static ProjectRegistration Load(string root, IEnumerable<string> expectedFiles)
    {
        var files = expectedFiles.ToHashSet(StringComparer.Ordinal);
        foreach (var required in new[] { "Dawnholder.slnx", IndependentList })
        {
            if (!files.Contains(required)) throw new InvalidDataException($"Missing registration input: {required}");
        }

        var solution = XDocument.Load(InputPaths.Resolve(root, "Dawnholder.slnx"));
        if (solution.Root?.Name != "Solution") throw new InvalidDataException("Expected a Solution registration document.");
        var products = solution.Descendants("Project")
            .Select(element => (string?)element.Attribute("Path") ?? throw new InvalidDataException("Missing project path."))
            .ToArray();
        if (products.Length == 0) throw new InvalidDataException("Product project registration is empty.");

        using var json = JsonDocument.Parse(File.ReadAllText(InputPaths.Resolve(root, IndependentList)));
        if (json.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Independent registration must be an object.");
        var properties = json.RootElement.EnumerateObject().ToArray();
        if (properties.Length != 2 || properties.Count(property => property.Name == "SchemaVersion") != 1 ||
            properties.Count(property => property.Name == "Projects") != 1)
        {
            throw new InvalidDataException("Unknown, missing or duplicate independent registration field.");
        }
        var version = json.RootElement.GetProperty("SchemaVersion");
        var projects = json.RootElement.GetProperty("Projects");
        if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var schema) || schema != 1 ||
            projects.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Unknown independent registration schema.");
        }
        var independent = projects.EnumerateArray().Select(project =>
            project.ValueKind == JsonValueKind.String ? project.GetString()! :
            throw new InvalidDataException("Independent project paths must be strings.")).ToArray();

        // Detect aliases and duplicates before constructing any set; discovery never approves a project.
        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in products.Concat(independent))
        {
            InputPaths.ValidateRelative(path);
            if (path.Any(char.IsControl) || path.IndexOfAny(['$', '*', '?', ';', '"', '<', '>', '|']) >= 0 ||
                !path.EndsWith(".csproj", StringComparison.Ordinal) || !InputPaths.IsInput(path))
            {
                throw new InvalidDataException($"Invalid registered project path: {path}");
            }
            if (!registered.Add(path)) throw new InvalidDataException($"Duplicate/overlapping project registration: {path}");
            if (!files.Contains(path)) throw new InvalidDataException($"Registered project absent from inputs: {path}");
            InputPaths.Resolve(root, path);
        }
        var expected = products.Concat(independent).ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(files.Where(path => path.EndsWith(".csproj", StringComparison.Ordinal))))
        {
            throw new InvalidDataException("Unknown or missing project input outside explicit registration.");
        }
        return new ProjectRegistration(products.Order(StringComparer.Ordinal).ToArray(), independent.Order(StringComparer.Ordinal).ToArray());
    }
}
