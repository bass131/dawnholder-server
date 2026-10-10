using System.Net;
using Dawnholder.Management.Backend;
using Microsoft.AspNetCore.Server.Kestrel.Core;

if (!OperatingSystem.IsLinux())
{
    Console.Error.WriteLine("관리 백엔드는 Linux/WSL에서 실행해야 합니다.");
    return 1;
}

try
{
    BackendSettings settings = BackendSettings.Load(args);
    DateTime startedAt = DateTime.UtcNow;
    // Empty builder avoids environment URL/HostFiltering overrides before our explicit access boundary.
    WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { Args = [] });
    builder.Host.UseConsoleLifetime();
    builder.WebHost.UseKestrel(options =>
    {
        options.Listen(IPAddress.Loopback, settings.ListenPort, listen => listen.Protocols = HttpProtocols.Http1);
        // AccessBoundary bounds buffered/chunked input at 16 KiB. A second transport limit would abort
        // draining an oversized HTTP/1 body and can reset the connection before the client reads our JSON 413.
        options.Limits.MaxRequestBodySize = null;
    });
    builder.Services.AddRouting();
    builder.Services.AddLogging(logging => logging.AddSimpleConsole());
    builder.Services.AddSingleton(settings);
    builder.Services.Configure<HostOptions>(options =>
        options.ShutdownTimeout = TimeSpan.FromSeconds((long)settings.Server.StopTimeoutSeconds + 30));
    builder.Services.AddSingleton<ServerRecordStore>();
    builder.Services.AddSingleton(service => new ReleaseStore(settings, service.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping));
    builder.Services.AddSingleton(service => new LogStore(service.GetRequiredService<ServerRecordStore>(), settings.Logs));
    builder.Services.AddSingleton(service => new ServerSupervisor(settings, service.GetRequiredService<ReleaseStore>(),
        service.GetRequiredService<ServerRecordStore>(), service.GetRequiredService<LogStore>(),
        service.GetRequiredService<IHostApplicationLifetime>(), startedAt));
    builder.Services.AddHostedService(service => service.GetRequiredService<LogStore>());
    builder.Services.AddHostedService(service => service.GetRequiredService<ServerSupervisor>());
    using AccessBoundary access = new(settings.DataDirectory, startedAt);
    await using WebApplication app = builder.Build();
    app.Use(access.InvokeAsync);
    ManagementApi.Map(app);
    await app.StartAsync();
    try
    {
        access.Publish(new Uri(app.Urls.Single()).Port);
        await app.WaitForShutdownAsync();
    }
    finally
    {
        await app.StopAsync();
    }

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"관리 백엔드 시작/종료 오류: {error}");
    return 1;
}
