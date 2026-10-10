using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// Reads dotted paths ("server.state") from API responses and record files. A missing property fails with the path and
/// the whole document, so a contract mismatch reads differently from a wrong value.
/// </summary>
internal static class JsonFields
{
    /// <summary>The value at <paramref name="path"/>; the property must exist but its value may be null.</summary>
    public static JsonNode? Field(JsonNode? root, string path)
    {
        JsonNode? node = root;
        foreach (string name in path.Split('.'))
        {
            Assert.True(node is JsonObject, $"'{path}': expected an object before '{name}' in {Show(root)}");
            JsonObject parent = (JsonObject)node!;
            Assert.True(parent.ContainsKey(name), $"'{path}': property '{name}' is missing in {Show(root)}");
            node = parent[name];
        }

        return node;
    }

    public static bool IsNull(JsonNode? root, string path) => Field(root, path) == null;

    public static string Text(JsonNode? root, string path)
    {
        JsonNode? node = Field(root, path);
        Assert.True(
            node is JsonValue value && value.GetValueKind() == JsonValueKind.String,
            $"'{path}': expected a string, got {Show(node)} in {Show(root)}");
        return node!.GetValue<string>();
    }

    public static long Number(JsonNode? root, string path)
    {
        JsonNode? node = Field(root, path);
        Assert.True(
            node is JsonValue value && value.GetValueKind() == JsonValueKind.Number,
            $"'{path}': expected a number, got {Show(node)} in {Show(root)}");
        return node!.GetValue<long>();
    }

    public static bool Flag(JsonNode? root, string path)
    {
        JsonNode? node = Field(root, path);
        Assert.True(
            node is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
            $"'{path}': expected a boolean, got {Show(node)} in {Show(root)}");
        return node!.GetValue<bool>();
    }

    /// <summary>A UTC ISO 8601 time ending in Z, as every API time is (backend-design.md 「API」).</summary>
    public static DateTimeOffset Time(JsonNode? root, string path)
    {
        string text = Text(root, path);
        Assert.True(text.EndsWith('Z'), $"'{path}': expected a UTC time ending in Z, got '{text}'");
        Assert.True(
            DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset time),
            $"'{path}': '{text}' is not an ISO 8601 time");
        return time;
    }

    public static JsonArray Array(JsonNode? root, string path)
    {
        JsonNode? node = Field(root, path);
        Assert.True(node is JsonArray, $"'{path}': expected an array, got {Show(node)} in {Show(root)}");
        return (JsonArray)node!;
    }

    public static JsonNode ReadFile(string path)
    {
        Assert.True(File.Exists(path), $"expected file {path}");
        JsonNode? node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return node;
    }

    public static string Show(JsonNode? node) => node?.ToJsonString() ?? "null";
}
