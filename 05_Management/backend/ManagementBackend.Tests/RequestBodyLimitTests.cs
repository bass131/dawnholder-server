using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// The 16 KiB body limit of backend-design.md 「관리 접근 경계」 at its exact edge, with and without Content-Length, and
/// the order of the boundary checks when the client announces a large body and sends only part of it.
/// </summary>
public sealed class RequestBodyLimitTests
{
    const int LimitBytes = 16 * 1024;

    [LinuxFact]
    public async Task BodyOfExactly16KiBPassesTheLimitAndOneMoreByteIs413WithOrWithoutLength()
    {
        await using BackendScenario scenario = BackendScenario.Create("body-edge");
        BackendProcess backend = await scenario.StartBackendAsync();
        string atLimit = CommitBody(LimitBytes);
        string overLimit = CommitBody(LimitBytes + 1);

        ApiResult lengthAtLimit = await backend.SendAsync(HttpMethod.Put, "/api/releases/current", atLimit);
        ApiResult lengthOverLimit = await backend.SendAsync(HttpMethod.Put, "/api/releases/current", overLimit);
        ApiResult chunkedAtLimit = await backend.SendAsync(
            HttpMethod.Put, "/api/releases/current", adjust: request => request.Content = new ChunkedContent(atLimit));
        ApiResult chunkedOverLimit = await backend.SendAsync(
            HttpMethod.Put, "/api/releases/current", adjust: request => request.Content = new ChunkedContent(overLimit));

        Assert.Equal(LimitBytes, Encoding.UTF8.GetByteCount(atLimit));
        // At the limit the body is read and then refused by the commit rule, not by its size.
        ApiAssert.Error(lengthAtLimit, 400, "invalidCommit");
        ApiAssert.Error(chunkedAtLimit, 400, "invalidCommit");
        AssertTooLarge(lengthOverLimit, "Content-Length body of 16 KiB + 1");
        AssertTooLarge(chunkedOverLimit, "chunked body of 16 KiB + 1");
    }

    [LinuxFact]
    public async Task AnnouncedLargeBodyIsAnsweredBeforeTheClientSendsIt()
    {
        await using BackendScenario scenario = BackendScenario.Create("body-early");
        BackendProcess backend = await scenario.StartBackendAsync();
        const long Announced = 100L * 1024 * 1024;
        string host = $"Host: 127.0.0.1:{backend.Port}";

        string withoutSecret = await StatusLineAfterPartialBodyAsync(backend.Port, Announced, host);
        string withOrigin = await StatusLineAfterPartialBodyAsync(backend.Port, Announced, $"{host}\r\nOrigin: http://evil.example");
        string withSecret = await StatusLineAfterPartialBodyAsync(
            backend.Port, Announced, $"{host}\r\nAuthorization: Bearer {backend.Token}");
        ApiResult afterwards = await backend.GetAsync("/api/status");

        Assert.StartsWith("HTTP/1.1 401", withoutSecret, StringComparison.Ordinal);
        Assert.StartsWith("HTTP/1.1 403", withOrigin, StringComparison.Ordinal);
        Assert.StartsWith("HTTP/1.1 413", withSecret, StringComparison.Ordinal);
        Assert.True(afterwards.Code == 200, $"the backend must keep answering: {afterwards.Describe()}");
    }

    static string CommitBody(int bytes)
    {
        const string Prefix = "{\"commit\":\"";
        const string Suffix = "\"}";
        return Prefix + new string('a', bytes - Prefix.Length - Suffix.Length) + Suffix;
    }

    static void AssertTooLarge(ApiResult result, string what)
    {
        // The design fixes the status (413) and the error shape, not the error code.
        Assert.True(result.Code == 413, $"{what}: expected 413, got {result.Describe()}");
        JsonNode? body = result.Json;
        Assert.True(body is JsonObject, $"{what}: expected a JSON error object, got {result.Describe()}");
        Assert.False(string.IsNullOrWhiteSpace(JsonFields.Text(body, "error")), $"{what}: empty error in {result.Describe()}");
        Assert.False(string.IsNullOrWhiteSpace(JsonFields.Text(body, "message")), $"{what}: empty message in {result.Describe()}");
    }

    /// <summary>
    /// Announces <paramref name="announcedLength"/> body bytes, sends only 1 KiB and returns the first response line, so a
    /// backend that waits for the whole body before answering fails the test by timing out.
    /// </summary>
    static async Task<string> StatusLineAfterPartialBodyAsync(int port, long announcedLength, string headers)
    {
        using TcpClient client = new();
        await client.ConnectAsync(IPAddress.Loopback, port);
        NetworkStream stream = client.GetStream();
        string head = $"PUT /api/releases/current HTTP/1.1\r\n{headers}\r\nContent-Type: application/json\r\n"
            + $"Content-Length: {announcedLength}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head));
        await stream.WriteAsync(new byte[1024]);

        using CancellationTokenSource cancel = new(TimeSpan.FromSeconds(10));
        byte[] buffer = new byte[512];
        int read;
        try
        {
            read = await stream.ReadAsync(buffer, cancel.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"no response within 10 s while {announcedLength} announced body bytes were not sent ({headers})");
            throw;
        }

        return Encoding.ASCII.GetString(buffer, 0, read).Split("\r\n")[0];
    }

    /// <summary>A JSON body without Content-Length, so HttpClient sends it with chunked transfer encoding.</summary>
    sealed class ChunkedContent : HttpContent
    {
        readonly byte[] _bytes;

        public ChunkedContent(string json)
        {
            _bytes = Encoding.UTF8.GetBytes(json);
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            for (int offset = 0; offset < _bytes.Length; offset += 4096)
            {
                await stream.WriteAsync(_bytes.AsMemory(offset, Math.Min(4096, _bytes.Length - offset)));
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
