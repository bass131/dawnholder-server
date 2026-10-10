using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>One HTTP exchange with the backend; the body stays raw so a non-JSON answer is still visible.</summary>
internal sealed record ApiResult(HttpStatusCode Status, string Body)
{
    public int Code => (int)Status;

    public bool IsSuccess => Code is >= 200 and <= 299;

    public JsonNode? Json
    {
        get
        {
            try
            {
                return JsonNode.Parse(Body);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    public string Describe() => $"HTTP {Code}: {Body}";
}

/// <summary>Error responses are { "error": "&lt;code&gt;", "message": "&lt;Korean text&gt;" } (backend-design.md 「API」).</summary>
internal static class ApiAssert
{
    public static void Success(ApiResult result, string action) =>
        Assert.True(result.IsSuccess, $"{action}: expected 2xx, got {result.Describe()}");

    public static void Error(ApiResult result, int expectedStatus, string expectedError)
    {
        int status = result.Code;
        Assert.True(status == expectedStatus, $"status: expected {expectedStatus}, got {result.Describe()}");

        JsonNode? body = result.Json;
        Assert.True(body is JsonObject, $"error body: expected a JSON object, got {result.Describe()}");
        string error = JsonFields.Text(body, "error");
        Assert.True(error == expectedError, $"error code: expected '{expectedError}', got '{error}' in {result.Describe()}");

        string message = JsonFields.Text(body, "message");
        Assert.False(string.IsNullOrWhiteSpace(message), $"error message is empty in {result.Describe()}");
    }
}
