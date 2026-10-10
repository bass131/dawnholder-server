using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Dawnholder.Management.Backend;

// Owns only the connection credential and the HTTP access boundary, never server state.
internal sealed class AccessBoundary(string dataDirectory, DateTime startedAt) : IDisposable
{
    public const int MaximumBodyBytes = 16 * 1024;

    private readonly string _token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    private readonly string _path = Path.Combine(dataDirectory, "connection.json");
    private int _port;
    private bool _published;

    public void Publish(int port)
    {
        _port = port;
        JsonFiles.Write(_path, new Connection(port, _token, Environment.ProcessId, startedAt), privateFile: true);
        _published = true;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            string? host = context.Request.Host.Value;
            int port = _port == 0 ? context.Connection.LocalPort : _port;
            if (context.Connection.RemoteIpAddress is not { } address || !IPAddress.IsLoopback(address)
                || context.Request.Headers.ContainsKey("Origin")
                || (host != $"127.0.0.1:{port}" && host != $"localhost:{port}"))
            {
                throw new ApiFailure(403, "forbidden", "이 주소에서는 관리 요청을 허용하지 않습니다.");
            }

            byte[] supplied = SHA256.HashData(Encoding.UTF8.GetBytes(context.Request.Headers.Authorization.ToString()));
            byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes("Bearer " + _token));
            if (!CryptographicOperations.FixedTimeEquals(supplied, expected))
                throw new ApiFailure(401, "unauthorized", "관리 연결 비밀값이 맞지 않습니다.");

            if (context.Request.ContentLength > MaximumBodyBytes)
                throw new ApiFailure(413, "bodyTooLarge", "요청 본문은 16 KiB 이하여야 합니다.");

            // Bound chunked bodies too, including endpoints which do not deserialize a body.
            using MemoryStream body = new();
            byte[] buffer = new byte[4096];
            int read;
            while ((read = await context.Request.Body.ReadAsync(buffer, context.RequestAborted)) != 0)
            {
                if (body.Length + read > MaximumBodyBytes)
                    throw new ApiFailure(413, "bodyTooLarge", "요청 본문은 16 KiB 이하여야 합니다.");
                body.Write(buffer, 0, read);
            }

            body.Position = 0;
            Stream original = context.Request.Body;
            try
            {
                context.Request.Body = body;
                await next(context);
            }
            finally
            {
                context.Request.Body = original;
            }
        }
        catch (ApiFailure error)
        {
            await ErrorAsync(context, error.StatusCode, error.Code, error.Message);
        }
        catch (BadHttpRequestException error)
        {
            await ErrorAsync(context, error.StatusCode, "invalidRequest", "올바른 요청 본문이 필요합니다.");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Request cancellation must not turn an owned background server into an orphan.
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            await ErrorAsync(context, 500, "operationFailed", "관리 작업에 실패했습니다. 백엔드 로그를 확인하세요.");
        }
    }

    public void Dispose()
    {
        if (!_published) return;
        try
        {
            if (File.Exists(_path) && JsonFiles.Read<Connection>(_path).Token == _token) File.Delete(_path);
        }
        catch (Exception error) when (error is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"연결 파일 정리 실패: {error.Message}");
        }
    }

    private static Task ErrorAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { error = code, message });
    }

    private sealed record Connection(int Port, string Token, int Pid, DateTime StartedAt);
}
