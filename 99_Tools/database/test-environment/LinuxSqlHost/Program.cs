using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Tools.LinuxSqlHost;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length != 3 || args[2] is not ("ready" or "bootstrap" or "runtime" or "sa-disabled"))
                throw new InvalidDataException();
            HostPlan plan = HostPlan.Read(args[0], args[1]);
            using CredentialEnvelope envelope = await CredentialEnvelope.ReadAsync();
            using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(165));
            SqlLifecycle lifecycle = new(plan, deadline.Token);
            object result = args[2] switch
            {
                "ready" => await lifecycle.ReadyAsync(envelope),
                "bootstrap" => await lifecycle.BootstrapAsync(envelope),
                "runtime" => await lifecycle.RuntimeAsync(envelope),
                "sa-disabled" => await lifecycle.SaDisabledAsync(envelope),
                _ => throw new InvalidDataException()
            };
            Console.WriteLine(JsonSerializer.Serialize(result));
            return 0;
        }
        catch (SqlException exception)
        {
            // Numbers are safe categories; Message/Server/stack/connection strings never cross stdout or stderr.
            Console.WriteLine(JsonSerializer.Serialize(new { Status = "Failed", Kind = "Sql", Code = exception.Number }));
            return 2;
        }
        catch (Exception)
        {
            Console.WriteLine("{\"Status\":\"Failed\",\"Kind\":\"BoundaryOrTimeout\"}");
            return 3;
        }
    }
}

internal sealed class CredentialEnvelope : IDisposable
{
    internal SecureString Admin { get; } = new();
    internal SecureString Runtime { get; } = new();

    public void Dispose()
    {
        Admin.Dispose();
        Runtime.Dispose();
    }

    internal static async Task<CredentialEnvelope> ReadAsync()
    {
        byte[] bytes = new byte[4096];
        CredentialEnvelope envelope = new();
        try
        {
            // WaitAsync bounds even a native stdin read which does not implement cancellation.
            using Stream stdin = Console.OpenStandardInput();
            int used = 0;
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
            while (used < bytes.Length)
            {
                int count = await stdin.ReadAsync(bytes.AsMemory(used, 1), timeout.Token).AsTask().WaitAsync(timeout.Token);
                if (count == 0)
                    throw new InvalidDataException();
                if (bytes[used] == (byte)'\n')
                    break;
                used++;
            }
            if (used == bytes.Length)
                throw new InvalidDataException();
            using JsonDocument document = JsonDocument.Parse(bytes.AsMemory(0, used));
            JsonElement root = document.RootElement;
            if (root.GetProperty("Version").GetInt32() != 1 || root.EnumerateObject().Count() != 3)
                throw new InvalidDataException();
            Copy(root.GetProperty("Admin").GetString(), envelope.Admin);
            Copy(root.GetProperty("Runtime").GetString(), envelope.Runtime);
            return envelope;
        }
        catch
        {
            envelope.Dispose();
            throw;
        }
        finally
        {
            Array.Clear(bytes);
        }
    }

    static void Copy(string? text, SecureString target)
    {
        if (text == null || text.Length > 128 || text.Any(char.IsControl))
            throw new InvalidDataException();
        foreach (char character in text)
            target.AppendChar(character);
        target.MakeReadOnly();
    }
}
