using Microsoft.AspNetCore.Server.Kestrel.Core;
using TelegramBot;
using static ReportProvider.ReportProvider;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

var config = builder.Configuration;
var appOptions = config.GetSection(AppOptions.Name).Get<AppOptions>() ?? throw new InvalidOperationException();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddGrpc();
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<AppOptions>(config.GetSection(AppOptions.Name));
builder.Services.AddSingleton<CheckHandler>();
builder.Services.AddGrpcClient<ReportProviderClient>(o => o.Address = new Uri(appOptions.ReportProviderUri));

var app = builder.Build();

app.MapGrpcService<HealthCheck>();

await app.RunAsync();
