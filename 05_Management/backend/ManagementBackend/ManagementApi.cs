using System.Text.Json;

namespace Dawnholder.Management.Backend;

internal static class ManagementApi
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/status", (ServerSupervisor supervisor) => Results.Json(supervisor.Status()));
        app.MapPost("/api/server/start", async (ServerSupervisor supervisor) => Results.Json(await supervisor.StartServerAsync()));
        app.MapPost("/api/server/stop", async (ServerSupervisor supervisor) => Results.Json(await supervisor.StopServerAsync()));
        app.MapPost("/api/server/force-stop", async (ServerSupervisor supervisor) => Results.Json(await supervisor.ForceStopAsync()));
        app.MapGet("/api/logs", (HttpRequest request, LogStore logs, ServerRecordStore records) => Results.Json(logs.Query(LogQuery.Parse(request.Query, records))));
        app.MapGet("/api/releases", (ReleaseStore releases) => Results.Json(releases.List()));
        app.MapPost("/api/releases", async (HttpRequest request, ReleaseStore releases) =>
            Results.Json(await releases.BuildAsync(await CommitAsync(request), request.HttpContext.RequestAborted)));
        app.MapPut("/api/releases/current", async (HttpRequest request, ReleaseStore releases) =>
            Results.Json(await releases.SelectAsync(await CommitAsync(request))));
        app.MapFallback(() => Results.Json(new { error = "notFound", message = "관리 API 경로를 찾을 수 없습니다." }, statusCode: 404));
    }

    private static async Task<string> CommitAsync(HttpRequest request)
    {
        try
        {
            using JsonDocument body = await JsonDocument.ParseAsync(request.Body, cancellationToken: request.HttpContext.RequestAborted);
            if (body.RootElement.ValueKind != JsonValueKind.Object
                || !body.RootElement.TryGetProperty("commit", out JsonElement commit) || commit.ValueKind != JsonValueKind.String)
            {
                return ReleaseStore.ValidateCommit(null);
            }
            return ReleaseStore.ValidateCommit(commit.GetString());
        }
        catch (JsonException)
        {
            throw new ApiFailure(400, "invalidCommit", "commit을 담은 JSON 객체가 필요합니다.");
        }
    }
}
