using System.Reflection;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// Finds the pinned SDK host and the processes under test. The test project records their built paths as assembly
/// metadata (see RecordProcessUnderTestPaths in the .csproj), so the output layout does not matter.
/// </summary>
internal static class BuiltProcesses
{
    public const string BackendAssembly = "Dawnholder.Management.Backend.dll";
    public const string StubAssembly = "GameServerStub.dll";
    public const string NotBuiltMessage = "Management backend is not built";

    /// <summary>The host selected by the entry point (sdk.sh exports it). Tests never guess another SDK.</summary>
    public static string Dotnet
    {
        get
        {
            string? path = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !File.Exists(path))
            {
                Assert.Fail($"DOTNET_HOST_PATH must name the pinned SDK host; got '{path}'. Run the tests through run-tests.sh.");
            }

            return path;
        }
    }

    public static string Backend => Locate(
        BackendAssembly,
        $"{NotBuiltMessage}: no built {BackendAssembly} was recorded. Create 05_Management/backend/ManagementBackend/"
        + "ManagementBackend.csproj with AssemblyName Dawnholder.Management.Backend as backend-design.md describes.");

    public static string Stub => Locate(StubAssembly, $"GameServerStub is not built: no built {StubAssembly} was recorded.");

    /// <summary>Stub source and pinned global.json, copied next to the tests for the temporary release repository.</summary>
    public static string ReleaseSource => Path.Combine(AppContext.BaseDirectory, "ReleaseSource");

    /// <summary>The repository's backend-wsl.sh, copied next to the tests. Tests run copies of it, never the original.</summary>
    public static string BackendEntryScript
    {
        get
        {
            string path = Path.Combine(AppContext.BaseDirectory, "BackendEntry", "backend-wsl.sh");
            Assert.True(File.Exists(path), $"backend-wsl.sh was not copied next to the tests: {path}");
            return path;
        }
    }

    static string Locate(string assemblyFile, string missing)
    {
        string? path = typeof(BuiltProcesses).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == assemblyFile)?.Value;
        if (path == null)
        {
            Assert.Fail(missing);
        }

        if (!File.Exists(path))
        {
            Assert.Fail($"{missing} The recorded path does not exist: {path}");
        }

        return path;
    }
}
