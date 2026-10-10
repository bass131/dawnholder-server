using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>H: settings are validated at start (backend-design.md 「설정」).</summary>
public sealed class BackendConfigurationTests
{
    /// <summary>
    /// Each case breaks one key of an otherwise valid configuration. The expected text is the key name the design asks
    /// the backend to print in its reason.
    /// </summary>
    [LinuxTheory]
    [InlineData("relativeDataDirectory", "dataDirectory")]
    [InlineData("listenPortOutOfRange", "listenPort")]
    [InlineData("serverPortOutOfRange", "port")]
    [InlineData("relativePortLockFile", "portLockFile")]
    [InlineData("zeroStartTimeout", "startTimeoutSeconds")]
    [InlineData("negativeStopTimeout", "stopTimeoutSeconds")]
    [InlineData("relativeSourceRepository", "sourceRepository")]
    [InlineData("zeroBuildTimeout", "buildTimeoutSeconds")]
    [InlineData("zeroRetentionDays", "retentionDays")]
    [InlineData("zeroRetentionBytes", "retentionBytes")]
    [InlineData("zeroRetentionInterval", "retentionIntervalSeconds")]
    [InlineData("noDotnetPath", "dotnetPath")]
    public async Task H1_InvalidSettingExitsNonzeroNamingTheKey(string breakage, string key)
    {
        await using BackendScenario scenario = BackendScenario.Create($"h1-{breakage}");
        JsonObject config = scenario.Settings.ToJson();
        Dictionary<string, string?> environment = scenario.BackendEnvironment();
        Break(config, environment, breakage);

        ProcessResult result = await scenario.RunBackendToExitAsync(config, environment);
        bool connectionFileWritten = File.Exists(Path.Combine(scenario.DataDirectory, "connection.json"));

        Assert.True(result.ExitCode != 0, $"invalid {key} must stop the backend with a nonzero code: {result.Describe()}");
        Assert.True(result.Combined.Contains(key, StringComparison.Ordinal), $"the reason must name '{key}': {result.Describe()}");
        Assert.False(connectionFileWritten, "an invalid configuration must not write connection.json");
    }

    static void Break(JsonObject config, Dictionary<string, string?> environment, string breakage)
    {
        JsonObject server = config["server"]!.AsObject();
        JsonObject release = config["release"]!.AsObject();
        JsonObject logs = config["logs"]!.AsObject();
        switch (breakage)
        {
            case "relativeDataDirectory":
                config["dataDirectory"] = "relative/data";
                break;
            case "listenPortOutOfRange":
                config["listenPort"] = 70000;
                break;
            case "serverPortOutOfRange":
                server["port"] = 70000;
                break;
            case "relativePortLockFile":
                server["portLockFile"] = "locks/server.lock";
                break;
            case "zeroStartTimeout":
                server["startTimeoutSeconds"] = 0;
                break;
            case "negativeStopTimeout":
                server["stopTimeoutSeconds"] = -1;
                break;
            case "relativeSourceRepository":
                release["sourceRepository"] = "relative/repo";
                break;
            case "zeroBuildTimeout":
                release["buildTimeoutSeconds"] = 0;
                break;
            case "zeroRetentionDays":
                logs["retentionDays"] = 0;
                break;
            case "zeroRetentionBytes":
                logs["retentionBytes"] = 0;
                break;
            case "zeroRetentionInterval":
                logs["retentionIntervalSeconds"] = 0;
                break;
            case "noDotnetPath":
                // server.dotnetPath falls back to DOTNET_HOST_PATH; without both it is a setting error.
                server.Remove("dotnetPath");
                environment["DOTNET_HOST_PATH"] = null;
                break;
            default:
                Assert.Fail($"unknown breakage {breakage}");
                break;
        }
    }
}
