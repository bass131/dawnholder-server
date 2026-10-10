using System.Text.Json;

namespace Dawnholder.Management.Backend;

internal static class JsonFiles
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // Configuration and persisted contracts use exact camelCase names and JSON numbers.
        PropertyNameCaseInsensitive = false,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
    };

    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new IOException($"비어 있는 기록: {path}");

    public static void Write<T>(string path, T value, bool privateFile = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            FileStreamOptions options = new() { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (privateFile && OperatingSystem.IsLinux())
            {
                // Set mode during creation: chmod after writing would expose the bearer token briefly.
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (FileStream stream = new(temporary, options))
            {
                JsonSerializer.Serialize(stream, value, Options);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
