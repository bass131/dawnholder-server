using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Dawnholder.Server.GameServer.Tests.Tools;

public sealed class PacketGeneratorExitTests
{
    const string ValidPdl = "<PDL><packet name=\"C_Probe\"><int name=\"value\"/></packet></PDL>";

    [Fact]
    public async Task ValidInput_ExitsZeroAndPreservesBaselineOutput()
    {
        using Fixture fixture = new(ValidPdl);
        Result result = await fixture.Run();

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains("[GEN] Packet Generate Success.", result.Output);
        Assert.Contains("[GEN] --no-manager:", result.Output);
        Assert.DoesNotContain("Packet Generate FAIL!!", result.Output);
        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "02_Server")));
        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "04_ClientNet")));

        // Captured from a3c4e15 for ValidPdl, not calculated from the current generator.
        // Verbatim template checkout and Environment.NewLine may differ between OSes.
        string generated = File.ReadAllText(fixture.GeneratedFile).Replace("\r\n", "\n");
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(generated)));
        Assert.Equal("5725B8CCC663816C8EA816DBCEBB2AF475BE21528CC28F3F110AFB109E630F5B", digest);
    }

    [Fact]
    public async Task OutputDirectoryConflictsWithFile_ExitsNonzeroAndPreservesSentinel()
    {
        using Fixture fixture = new(ValidPdl);
        string generatedDirectory = Path.GetDirectoryName(fixture.GeneratedFile)!;
        Directory.CreateDirectory(Path.GetDirectoryName(generatedDirectory)!);
        byte[] sentinel = [0, 42, 255, 13, 10];
        File.WriteAllBytes(generatedDirectory, sentinel);

        Result result = await fixture.Run();

        Assert.True(result.ExitCode != 0, result.Output);
        Assert.Contains("[GEN] Packet Generate FAIL!!", result.Output);
        Assert.DoesNotContain("Packet Generate Success.", result.Output);
        Assert.False(File.Exists(fixture.GeneratedFile));
        Assert.Equal(sentinel, File.ReadAllBytes(generatedDirectory));
    }

    [Fact]
    public async Task MalformedXml_StillPropagatesParseFailure()
    {
        using Fixture fixture = new("<PDL><packet name=\"C_Broken\">");
        Result result = await fixture.Run();

        Assert.True(result.ExitCode != 0, result.Output);
        Assert.Contains("System.Xml.XmlException", result.Output);
        Assert.DoesNotContain("Packet Generate FAIL!!", result.Output);
        Assert.DoesNotContain("Packet Generate Success.", result.Output);
        Assert.False(File.Exists(fixture.GeneratedFile));
    }

    sealed record Result(int ExitCode, string Output);

    sealed class Fixture : IDisposable
    {
        readonly string _pdl;

        internal Fixture(string contents)
        {
            Root = Path.Combine(Path.GetTempPath(), "Dawnholder-GeneratorTests-" + Guid.NewGuid().ToString("N"));
            _pdl = Path.Combine(Root, "99_Tools", "PacketGenerator", "PDL.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(_pdl)!);
            File.WriteAllText(_pdl, contents);
        }

        internal string Root { get; }
        internal string GeneratedFile => Path.Combine(Root, "98_Shared", "Protocol", "Generated", "GenPackets.cs");

        internal async Task<Result> Run()
        {
            DirectoryInfo output = new(AppContext.BaseDirectory);
            string configuration = output.Parent!.Name;
            DirectoryInfo? repo = output;
            while (repo != null && !File.Exists(Path.Combine(repo.FullName, "Dawnholder.slnx")))
            {
                repo = repo.Parent;
            }

            Assert.NotNull(repo);
            string dll = Path.Combine(repo.FullName, "99_Tools", "PacketGenerator", "bin", configuration,
                "net10.0", "Dawnholder.Tools.PacketGenerator.dll");
            Assert.True(File.Exists(dll), $"Build-only ProjectReference must produce {dll}");
            ProcessStartInfo start = new(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                WorkingDirectory = Root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add(dll);
            start.ArgumentList.Add(_pdl);
            start.ArgumentList.Add("--no-wait");
            using Process process = new() { StartInfo = start };
            Assert.True(process.Start());
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                throw new TimeoutException($"Generator --no-wait exceeded 30s.\n{await stdout}\n{await stderr}");
            }

            return new Result(process.ExitCode, await stdout + await stderr);
        }

        public void Dispose()
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
